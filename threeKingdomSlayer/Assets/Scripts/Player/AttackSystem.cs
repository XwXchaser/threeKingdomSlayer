using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 攻击系统
/// 实现6种攻击类型：戳击、斩击、穿刺、横扫、挑飞、招架
/// 所有攻击参数从 HeroConfig 的技能配置中读取
/// </summary>
public class AttackSystem : MonoBehaviour
{
    public static AttackSystem Instance { get; private set; }

    /// <summary>攻击执行事件（仅 Slash/Sweep/Stab/Pierce/Launch 五种有效攻击触发）</summary>
    public System.Action<AttackType, int, bool> OnAttackPerformed; // attackType, targetColumn, slashLeftToRight

    /// <summary>Stab攻击最后一个命中的敌人（供被动效果查询目标）</summary>
    public Enemy LastStabTargetEnemy { get; private set; }

    [Header("组件引用")]
    public ColumnManager columnManager;
    public PlayerState playerState;

    [Header("招架远程飞行物")]
    [Tooltip("招架时扫描飞行物的范围半径")]
    public float parryProjectileRange = 4f;

    [Header("幻影攻击")]
    [Tooltip("幻影攻击使用的紫色透明材质")]
    [SerializeField] private Material _phantomMaterial;

    [Header("被动效果Prefab")]
    [Tooltip("折返波使用的视觉Prefab")]
    [SerializeField] private GameObject _returnWavePrefab;
    [Tooltip("连锁弹射闪电连线使用的视觉Prefab")]
    [SerializeField] private GameObject _chainBounceVisualPrefab;
    [Tooltip("弹射连线用的Chain预制体（回退方案）")]
    [SerializeField] private GameObject _chainBouncePrefab;

    [Header("Stab动画帧")]
    [Tooltip("戳击旋转帧1（stab_rotate1，从左往右朝向）")]
    [SerializeField] private Sprite _stabRotate1Sprite;
    [Tooltip("戳击旋转帧2（stab_rotate2，从左往右朝向）")]
    [SerializeField] private Sprite _stabRotate2Sprite;

    [Header("攻击冷却模式")]
    [Tooltip("勾选→动作锁定模式（攻击动画期间锁定所有攻击输入）。\n取消→独立CD模式（每招独立冷却，可交替连打）。\n独立CD模式保留作为未来可能的奖励效果（如技能移除动作硬直）。")]
    public bool useActionBasedCooldown = false;
    private float _actionLockTimer;

    /// <summary>
    /// 当前是否处于攻击动作播放中（动作锁定计时器未结束）
    /// </summary>
    public bool IsActionPlaying => _actionLockTimer > 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (_actionLockTimer > 0f)
            _actionLockTimer -= Time.deltaTime;
    }

    private void Start()
    {
        if (columnManager == null)
            columnManager = FindObjectOfType<ColumnManager>();
        if (playerState == null)
            playerState = FindObjectOfType<PlayerState>();
    }

    /// <summary>
    /// 尝试执行攻击
    /// BUG FIX: 只有实际命中至少一个敌人时，才触发冷却和消耗
    /// </summary>
    public bool TryExecuteAttack(AttackType attackType, int targetColumn = -1, bool slashLeftToRight = true)
    {
        if (playerState == null) return false;
        if (playerState.stageState != StageState.InProgress) return false;

        // 冷却检查：动作锁定模式 → 全局锁（时长=cooldown，特效timeScale同步拉伸）
        if (useActionBasedCooldown)
        {
            if (_actionLockTimer > 0f) return false;
        }
        else
        {
            if (!playerState.IsAttackReady(attackType))
            {
                Debug.Log($"[AttackSystem] {attackType} 冷却中");
                return false;
            }
        }

        bool hitAny = false;
        switch (attackType)
        {
            case AttackType.Stab:   hitAny = ExecuteStab(targetColumn); break;
            case AttackType.Slash:  hitAny = ExecuteSlash(slashLeftToRight); break;
            case AttackType.Pierce: hitAny = ExecutePierce(targetColumn); break;
            case AttackType.Sweep:  hitAny = ExecuteSweep(); break;
            case AttackType.Launch: hitAny = ExecuteLaunch(); break;
            case AttackType.Parry:  hitAny = ExecuteParry(); break;
        }

        if (hitAny)
        {
            // 触发冷却：动作锁定模式 → 全局锁=cooldown/攻速（特效timeScale同步拉伸）
            if (useActionBasedCooldown)
            {
                var cfg = GetConfig(attackType);
                float cooldown = cfg != null ? cfg.cooldown : 0.3f;
                float speedMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetAttackSpeedMultiplier() : 1f;
                _actionLockTimer = cooldown / Mathf.Max(speedMult, 0.01f);
            }
            else
            {
                playerState.StartCooldown(attackType);
            }
            UltimateSystem.Instance?.AddEnergyForAttack(attackType);
            // 攻击震动已暂时关闭（安卓端不合适），后续在其他功能情景中重新启用
            // Handheld.Vibrate();

            // 仅五种有效攻击类型触发被动计数（排除 Parry 和 Ultimate）
            if (attackType != AttackType.Parry && attackType != AttackType.Ultimate)
                OnAttackPerformed?.Invoke(attackType, targetColumn, slashLeftToRight);

            return true;
        }

        Debug.Log($"[AttackSystem] {attackType} 未命中任何敌人，不消耗冷却");
        return false;
    }

    #region 攻击类型实现

    private AttackSkillConfig GetConfig(AttackType type)
    {
        return playerState?.heroConfig?.GetSkillConfig(type);
    }

    private bool ExecuteStab(int columnIndex)
    {
        var cfg = GetConfig(AttackType.Stab);
        if (cfg == null || columnIndex < 0 || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetStabPierceDamagePenalty();
        int effectiveRows = GetEffectiveRangeRows(cfg);
        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, effectiveRows);
        // Stab 只命中应战 Boss（排除未应战 Boss 导致 wave 位置错误）
        targets = targets.FindAll(e => !e.isBoss || e.bossState == BossState.InCombat);
        LastStabTargetEnemy = targets.Count > 0 ? targets[0] : null;
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            wavePos.y = targets[0].transform.position.y + cfg.stabSpawnYOffset;
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                prefab: cfg.attackWavePrefab, zOffset: cfg.stabSpawnZOffset,
                targetDuration: GetVisualTargetDuration(cfg));
            AudioManager.Instance?.PostEvent("Player_Attack");
        }

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{finalDmg} 目标数:{targets.Count}");
        if (targets.Count > 0) ApplyStabPushWave(targets);
        return targets.Count > 0;
    }

    private bool ExecuteSlash(bool leftToRight)
    {
        var cfg = GetConfig(AttackType.Slash);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetSweepDamagePenalty();
        int effectiveRows = GetEffectiveSweepRangeRows(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(effectiveRows);
        if (targets.Count > 0)
        {
            Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
            Vector3 wavePos = new Vector3(0, playerPos.y + cfg.slashSpawnYOffset, playerPos.z + cfg.slashSpawnZOffset);
            bool hasTargets = targets.Count > 0;
            SweepEffect.Create(wavePos, cfg.damageType, finalDmg, targets, leftToRight,
                cfg.slashSweepHalfWidth, cfg.slashSweepAngle, cfg.slashSweepDuration,
                prefab: cfg.attackWavePrefab,
                onAllHit: hasTargets ? () => ApplySlashDirectionalPush(targets, leftToRight) : null,
                targetDuration: GetVisualTargetDuration(cfg),
                rotateSprite1: _stabRotate1Sprite, rotateSprite2: _stabRotate2Sprite);
            AudioManager.Instance?.PostEvent("Player_Attack");
        }

        Debug.Log($"[AttackSystem] 斩击 方向:{(leftToRight ? "L→R" : "R→L")} 伤害:{finalDmg} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecutePierce(int columnIndex)
    {
        var cfg = GetConfig(AttackType.Pierce);
        if (cfg == null || columnIndex < 0 || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetStabPierceDamagePenalty();
        int effectiveRows = GetEffectiveRangeRows(cfg);
        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, effectiveRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            StartCoroutine(ReleaseChargeShockwaves());
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab,
                targetDuration: GetVisualTargetDuration(cfg));
        }

        Debug.Log($"[AttackSystem] 穿刺 列{columnIndex} 伤害:{finalDmg} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteSweep()
    {
        var cfg = GetConfig(AttackType.Sweep);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetSweepDamagePenalty();
        int effectiveRows = GetEffectiveSweepRangeRows(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(effectiveRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            StartCoroutine(ReleaseChargeShockwaves());
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab,
                targetDuration: GetVisualTargetDuration(cfg));
        }

        Debug.Log($"[AttackSystem] 横扫 伤害:{finalDmg} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteLaunch()
    {
        var cfg = GetConfig(AttackType.Launch);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);

            StartCoroutine(ReleaseChargeShockwaves());

            // 概率击飞 Buff：每次攻击时判定一次（对所有目标生效）
            bool probLaunchActive = playerState != null && playerState.HasBuff(BuffType.ProbabilityLaunch);

            // Launch 攻击不使用 Stab.prefab 作为视觉（避免与 Pierce 混淆），
            // prefab=null 时 AttackWave 用纯色 Quad，不影响伤害和击飞逻辑
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                onHit: (enemy) =>
                {
                    bool canLaunch = enemy.CanBeLaunched(cfg.poiseDamage);
                    // 概率击飞：非 CanBeLaunched 时按概率强制进入 Stun 后再 Launch
                    if (!canLaunch && probLaunchActive)
                    {
                        // 默认 30% 概率触发
                        if (Random.value < 0.3f)
                        {
                            enemy.Stun(cfg.launchDuration * 0.5f);
                            canLaunch = true;
                        }
                    }

                    if (canLaunch)
                        enemy.Launch();
                },
                prefab: null,
                alphaOverride: 0f,
                canInterruptCFrame: true,
                targetDuration: GetVisualTargetDuration(cfg));
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 击飞时间:{cfg.launchDuration}s 目标数:{targets.Count}");

        Vector3 playerLaunchPos = playerState != null ? playerState.transform.position : transform.position;
        PlayLaunchVisual(cfg, playerLaunchPos, GetVisualTargetDuration(cfg));

        return targets.Count > 0;
    }

    private bool ExecuteParry()
    {
        var cfg = GetConfig(AttackType.Parry);
        if (cfg == null) return false;

        // 优先：扫描附近的远程飞行物并反弹
        var projectiles = FindObjectsOfType<EnemyProjectile>();
        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        bool deflectedAny = false;
        foreach (var p in projectiles)
        {
            if (p == null || p.isQTEProjectile) continue;
            float dist = Vector3.Distance(p.GetWorldPosition(), playerPos);
            if (dist <= parryProjectileRange)
            {
                p.Deflect();
                deflectedAny = true;
                Debug.Log($"[AttackSystem] 招架反弹飞行物: dist={dist:F1}");
            }
        }
        if (deflectedAny)
        {
            AudioManager.Instance?.PostEvent("Player_Parry");
            PlayParryVisual(cfg, playerPos, GetVisualTargetDuration(cfg));
            return true;
        }

        // 无飞行物在范围内 → 对敌人执行招架伤害
        if (columnManager == null) return false;
        float finalDmg = GetFinalDamage(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count == 0) return false;

        foreach (var enemy in targets)
        {
            // BUG FIX: 先削韧再扣血。TakeDamage 的打断逻辑会 CancelAttack → state=Idle，
            // 导致 TakePoiseDamage 的 state==Attacking 检查失败，Boss 永远无法被招架破势。
            enemy.TakePoiseDamage(cfg.poiseDamage);
            enemy.TakeDamage(finalDmg, cfg.damageType, canInterruptCFrame: true, isParryInterrupt: true);
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 目标数:{targets.Count}");
        AudioManager.Instance?.PostEvent("Player_Parry");

        PlayParryVisual(cfg, playerPos, GetVisualTargetDuration(cfg));

        return true;
    }

    /// <summary>
    /// 挑飞视觉特效：使用专用 stab_rotate 精灵创建独立视觉，
    /// 以 (35,90,zStart) 朝向做纯 Z 轴旋转至 (35,90,zEnd)，
    /// 表现枪头从低往高上挑的攻击动作。
    /// </summary>
    private void PlayLaunchVisual(AttackSkillConfig cfg, Vector3 playerPos, float targetDuration = -1f)
    {
        if (_stabRotate1Sprite == null) return;

        Vector3 spawnPos = new Vector3(playerPos.x + cfg.launchSpawnXOffset, playerPos.y + cfg.launchSpawnYOffset, playerPos.z + cfg.launchSpawnZOffset);

        float variance = Mathf.Clamp(cfg.launchAngleVariance, 0f, 30f);
        float zStart = 140f + Random.Range(-variance, variance);
        float zEnd = zStart - cfg.launchFlickAngle;
        float duration = Mathf.Max(cfg.launchFlickDuration, 0.1f);

        Vector3 startEuler = new Vector3(35f, 90f, zStart);
        Vector3 endEuler = new Vector3(35f, 90f, zEnd);

        var obj = new GameObject("Launch_Visual");
        obj.transform.position = spawnPos;
        obj.transform.rotation = Quaternion.Euler(startEuler);

        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = _stabRotate1Sprite;
        sr.sortingOrder = 10;
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;
        sr.material = mat;

        // 根据精灵尺寸计算初始 scale
        float basePixelsPerUnit = _stabRotate1Sprite.pixelsPerUnit;
        float basePixelSize = Mathf.Max(_stabRotate1Sprite.rect.width, _stabRotate1Sprite.rect.height);
        float baseWorldSize = basePixelSize / basePixelsPerUnit;
        float targetWorldSize = 5f;
        float scale = baseWorldSize > 0.001f ? targetWorldSize / baseWorldSize : 1f;
        Vector3 targetScale = Vector3.one * scale;
        obj.transform.localScale = Vector3.zero;

        var scaleIn = obj.transform.DOScale(targetScale, 0.05f).SetEase(Ease.OutQuad);

        var seq = DOTween.Sequence();
        seq.SetTarget(obj.transform);
        seq.SetUpdate(true);

        var rotate = obj.transform.DORotate(endEuler, duration, RotateMode.Fast).SetEase(Ease.InOutQuad);
        seq.Append(rotate);
        var moveUp = obj.transform.DOMoveY(spawnPos.y + cfg.launchRiseHeight, duration).SetEase(Ease.OutQuad);
        seq.Join(moveUp);

        // Stab rotate 双帧动画：rotate1 → rotate2
        if (_stabRotate2Sprite != null)
        {
            float frameT = duration / 2f;
            seq.Insert(0, DOTween.Sequence()
                .AppendCallback(() => sr.sprite = _stabRotate1Sprite)
                .AppendInterval(frameT)
                .AppendCallback(() => sr.sprite = _stabRotate2Sprite));
        }

        seq.AppendInterval(0.03f);
        seq.Append(mat.DOFade(0f, 0.15f).SetEase(Ease.InQuad));

        // 特效时长拉伸/压缩以匹配cooldown（限制最大缩放防极端值）
        if (targetDuration > 0f)
        {
            float naturalDuration = duration + 0.03f + 0.15f;
            if (naturalDuration > 0f)
            {
                float ts = Mathf.Clamp(naturalDuration / targetDuration, 0.1f, 10f);
                seq.timeScale = ts;
                scaleIn.timeScale = ts;
            }
        }

        bool completed = false;
        seq.OnKill(() =>
        {
            if (!completed)
            {
                scaleIn.Kill();
                Destroy(obj);
            }
        });

        seq.OnComplete(() =>
        {
            completed = true;
            scaleIn.Kill();
            Destroy(obj);
        });
    }

    /// <summary>
    /// 招架视觉特效：Stab prefab 以 (54,270,zStart) 朝向做纯 Z 轴旋转至 (54,270,zEnd)，
    /// 表现枪尾从下往上挑的格挡动作。zStart 每次有随机偏差。
    /// 生成位置以玩家为基准 + Inspector 偏移参数，而非跟随敌人。
    /// </summary>
    private void PlayParryVisual(AttackSkillConfig cfg, Vector3 playerPos, float targetDuration = -1f)
    {
        Vector3 spawnPos = new Vector3(playerPos.x + cfg.parrySpawnXOffset, playerPos.y + cfg.parrySpawnYOffset, playerPos.z + cfg.parrySpawnZOffset);

        // Z 起始角 45° ± 随机偏移，终点 = 起点 + sweepAngle
        float variance = Mathf.Clamp(cfg.parryAngleVariance, 0f, 30f);
        float zStart = 45f + Random.Range(-variance, variance);
        float zEnd = zStart + cfg.parrySweepAngle;
        float duration = Mathf.Max(cfg.parrySweepDuration, 0.1f);

        Vector3 startEuler = new Vector3(54f, 270f, zStart);
        Vector3 endEuler = new Vector3(54f, 270f, zEnd);

        // 实例化 prefab 或 Quad fallback
        GameObject obj;
        Material mat;
        Color parryColor = Color.white;

        if (cfg.attackWavePrefab != null)
        {
            obj = Instantiate(cfg.attackWavePrefab, spawnPos, Quaternion.Euler(startEuler));
            obj.name = "Parry_Visual";
            Renderer r = obj.GetComponentInChildren<Renderer>();
            if (r != null) { mat = r.material; mat.color = parryColor; }
            else { mat = null; }
        }
        else
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.name = "Parry_Visual";
            obj.transform.position = spawnPos;
            obj.transform.rotation = Quaternion.Euler(startEuler);
            obj.transform.localScale = Vector3.zero;
            mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = parryColor;
            obj.GetComponent<Renderer>().material = mat;
        }

        Vector3 targetScale = obj.transform.localScale == Vector3.zero
            ? new Vector3(5f, 1.5f, 1f)
            : obj.transform.localScale;

        // DOTween 序列：scale-in → 纯 Z 轴旋转（枪尾上挑）→ 淡出销毁
        var scaleIn = obj.transform.DOScale(targetScale, 0.05f).SetEase(Ease.OutQuad);

        var seq = DOTween.Sequence();
        seq.SetTarget(obj.transform);
        seq.SetUpdate(true);

        var rotate = obj.transform.DORotate(endEuler, duration, RotateMode.Fast).SetEase(Ease.InOutQuad);
        seq.Append(rotate);

        seq.AppendInterval(0.03f);
        if (mat != null)
            seq.Append(mat.DOFade(0f, 0.15f).SetEase(Ease.InQuad));

        // 特效时长拉伸/压缩以匹配cooldown（限制最大缩放防极端值）
        if (targetDuration > 0f)
        {
            float naturalDuration = seq.Duration();
            if (naturalDuration > 0f)
            {
                float ts = Mathf.Clamp(naturalDuration / targetDuration, 0.1f, 10f);
                seq.timeScale = ts;
                scaleIn.timeScale = ts;
            }
        }

        bool completed = false;
        seq.OnKill(() =>
        {
            if (!completed)
            {
                scaleIn.Kill();
                Destroy(obj);
            }
        });

        seq.OnComplete(() =>
        {
            completed = true;
            scaleIn.Kill();
            Destroy(obj);
        });
    }

    #endregion

    #region 工具方法

    private Vector3 GetWavePosition(List<Enemy> targets, int targetColumn)
    {
        if (targets.Count == 0)
            return new Vector3(0, 1.5f, -10f);

        Vector3 pos = targets[0].transform.position;

        if (targetColumn < 0)
            pos.x = 0f;

        pos.y = targets[0].transform.position.y + 1.5f;
        pos.z += 0.5f;

        return pos;
    }

    /// <summary>获取最终伤害（基础伤害 × 升级倍率）</summary>
    private float GetFinalDamage(AttackSkillConfig cfg)
    {
        if (cfg == null) return 0f;
        float mult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f;
        return cfg.damage * mult;
    }

    /// <summary>释放蓄力冲击波（蓄力攻击时调用，必须在伤害前）</summary>
    private System.Collections.IEnumerator ReleaseChargeShockwaves()
    {
        if (playerState == null || !playerState.IsCharging) yield break;
        if (TimedPassiveModule.Instance == null) yield break;
        if (columnManager == null) yield break;

        var results = TimedPassiveModule.Instance.ConsumeAllShockwaves();
        if (results.Count == 0) yield break;

        var sweepCfg = GetConfig(AttackType.Sweep);
        GameObject wavePrefab = sweepCfg?.attackWavePrefab;

        foreach (var r in results)
        {
            int wavesPerTick = r.config.shockwaveCount;
            int rows = r.config.rangeRows;
            var targets = columnManager.GetAllEnemiesInRange(rows);
            Vector3 wavePos = GetWavePosition(targets, -1);

            for (int layer = 0; layer < r.layers; layer++)
            {
                float damageMult = 1f + layer * r.config.stackDamageBonus;
                int dmg = Mathf.RoundToInt(r.config.baseDamage * damageMult);
                for (int w = 0; w < wavesPerTick; w++)
                {
                    AttackWave.Create(wavePos, DamageType.Sweep, dmg, targets,
                        prefab: wavePrefab);
                    if (r.config.waveDelay > 0f)
                        yield return new WaitForSeconds(r.config.waveDelay);
                }
            }
            Debug.Log($"[AttackSystem] 蓄力冲击波释放: {r.upgradeId} {r.layers}层×{wavesPerTick}波 rows={rows} baseDmg={r.config.baseDamage} delay={r.config.waveDelay}");
        }
    }

    /// <summary>获取有效攻击排数（含延长等加成）</summary>
    private int GetEffectiveRangeRows(AttackSkillConfig cfg)
    {
        int bonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetStabRangeBonus() : 0;
        return cfg.rangeRows + bonus;
    }

    /// <summary>获取有效横扫范围（含波长加成）</summary>
    private int GetEffectiveSweepRangeRows(AttackSkillConfig cfg)
    {
        int bonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetSweepRangeBonus() : 0;
        return cfg.rangeRows + bonus;
    }

    /// <summary>获取戳击/穿刺伤害惩罚倍率（延长副作用）</summary>
    private float GetStabPierceDamagePenalty()
    {
        if (UpgradeEffectManager.Instance != null)
            return 1f - UpgradeEffectManager.Instance.GetStabDamagePenalty();
        return 1f;
    }

    /// <summary>获取横扫/斩击伤害惩罚倍率（波长副作用）</summary>
    private float GetSweepDamagePenalty()
    {
        if (UpgradeEffectManager.Instance != null)
            return 1f - UpgradeEffectManager.Instance.GetSweepDamagePenalty();
        return 1f;
    }

    /// <summary>Stab 击退波</summary>
    /// <remarks>
    /// BOSS 免疫位移（ApplyPushWave 内部过滤 isBoss），因此仅在有敌人被实际推动时才执行列填充。
    /// 无条件调用 PostDisplacementFillUp 会导致 BOSS 被意外压缩（ResetMovementState → Idle），
    /// 若此时 BOSS 处于 QTEAttacking 状态将中止 QTE。
    /// </remarks>
    private void ApplyStabPushWave(List<Enemy> targets)
    {
        if (UpgradeEffectManager.Instance == null || columnManager == null) return;
        int pushDist = UpgradeEffectManager.Instance.GetPushWaveDistance();
        if (pushDist <= 0) return;

        Debug.Log($"[Displacement] Stab PushWave dist={pushDist} targets={targets.Count}");
        Debug.Log(columnManager.DumpColumns());

        bool anyPushed = columnManager.ApplyPushWave(targets, pushDist, canInterruptCFrame: false);
        // 仅在实际有敌人被推动时才执行列填充（BOSS 免疫位移，无推动则无需填充）
        if (anyPushed)
            columnManager.PostDisplacementFillUp();

        Debug.Log($"[Displacement] after Stab PushWave:");
        Debug.Log(columnManager.DumpColumns());
    }

    /// <summary>Slash 方向推</summary>
    /// <remarks>
    /// BOSS 免疫位移（ApplyDirectionalPush 内部过滤 isBoss），仅在有敌人被实际推动时才执行列填充。
    /// 同 ApplyStabPushWave 的 BOSS 保护逻辑。
    /// </remarks>
    private void ApplySlashDirectionalPush(List<Enemy> targets, bool leftToRight)
    {
        if (UpgradeEffectManager.Instance == null || columnManager == null) return;
        int step = UpgradeEffectManager.Instance.GetDirectionalPushStep();
        if (step <= 0) return;

        Debug.Log($"[Displacement] Slash DirectionalPush step={step} dir={(leftToRight ? "L→R" : "R→L")} targets={targets.Count}");
        Debug.Log(columnManager.DumpColumns());

        var movedEnemies = new List<Enemy>();
        bool anyMoved = columnManager.ApplyDirectionalPush(targets, step, leftToRight, canInterruptCFrame: false, movedEnemies: movedEnemies);

        // 仅在实际有敌人被推动时才执行列填充（BOSS 免疫位移）
        if (anyMoved)
            columnManager.PostDisplacementFillUp();

        // 位移旋转表现（仅实际发生位移的敌人，放 FillUp 后避免 StartMoving 的 DOTween.Kill 误杀 tilt）
        foreach (var enemy in movedEnemies)
        {
            enemy.ApplyDisplacementTilt(leftToRight);
        }

        Debug.Log($"[Displacement] after Slash DirectionalPush:");
        Debug.Log(columnManager.DumpColumns());
    }

    public float GetAttackDamage(AttackType attackType)
    {
        var cfg = GetConfig(attackType);
        return cfg != null ? GetFinalDamage(cfg) : 0f;
    }

    /// <summary>
    /// 执行幻影攻击 — 被动触发，继承原攻击类型，按比例缩放伤害和透明度。
    /// 不消耗冷却、不加能量、不计入攻击计数器。
    /// </summary>
    public bool ExecutePhantomAttack(AttackType attackType, int targetColumn, bool slashLeftToRight,
        float damageRatio, float alpha)
    {
        if (playerState == null || columnManager == null) return false;
        if (playerState.stageState != StageState.InProgress) return false;

        var cfg = GetConfig(attackType);
        if (cfg == null) return false;

        float finalDmg = GetFinalDamage(cfg) * damageRatio;
        Color phantomColor = new Color(0.3f, 0.5f, 1f); // 幻影伤害数字颜色：蓝色

        switch (attackType)
        {
            case AttackType.Stab:
            {
                if (targetColumn < 0) return false;
                int effectiveRows = GetEffectiveRangeRows(cfg);
                var targets = columnManager.GetEnemiesInRange(targetColumn, effectiveRows);
                targets = targets.FindAll(e => !e.isBoss || e.bossState == BossState.InCombat);
                finalDmg *= GetStabPierceDamagePenalty();
                if (targets.Count > 0)
                {
                    Vector3 wavePos = GetWavePosition(targets, targetColumn);
                    wavePos.y = targets[0].transform.position.y + cfg.stabSpawnYOffset;
                    AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                        prefab: cfg.attackWavePrefab, zOffset: cfg.stabSpawnZOffset, alphaOverride: alpha,
                        damageNumberColor: phantomColor, materialOverride: _phantomMaterial);
                }
                return targets.Count > 0;
            }
            case AttackType.Slash:
            {
                int effectiveRows = GetEffectiveSweepRangeRows(cfg);
                var targets = columnManager.GetAllEnemiesInRange(effectiveRows);
                finalDmg *= GetSweepDamagePenalty();
                if (targets.Count > 0)
                {
                    Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
                    Vector3 wavePos = new Vector3(0, playerPos.y + cfg.slashSpawnYOffset, playerPos.z + cfg.slashSpawnZOffset);
                    SweepEffect.Create(wavePos, cfg.damageType, finalDmg, targets, slashLeftToRight,
                        cfg.slashSweepHalfWidth, cfg.slashSweepAngle, cfg.slashSweepDuration,
                        prefab: cfg.attackWavePrefab, alphaOverride: alpha,
                        damageNumberColor: phantomColor, materialOverride: _phantomMaterial);
                }
                return targets.Count > 0;
            }
            case AttackType.Pierce:
            {
                if (targetColumn < 0) return false;
                int effectiveRows = GetEffectiveRangeRows(cfg);
                var targets = columnManager.GetEnemiesInRange(targetColumn, effectiveRows);
                finalDmg *= GetStabPierceDamagePenalty();
                if (targets.Count > 0)
                {
                    Vector3 wavePos = GetWavePosition(targets, targetColumn);
                    AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                        prefab: cfg.attackWavePrefab, alphaOverride: alpha,
                        damageNumberColor: phantomColor, materialOverride: _phantomMaterial);
                }
                return targets.Count > 0;
            }
            case AttackType.Sweep:
            {
                int effectiveRows = GetEffectiveSweepRangeRows(cfg);
                var targets = columnManager.GetAllEnemiesInRange(effectiveRows);
                finalDmg *= GetSweepDamagePenalty();
                if (targets.Count > 0)
                {
                    Vector3 wavePos = GetWavePosition(targets, -1);
                    AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                        prefab: cfg.attackWavePrefab, alphaOverride: alpha,
                        damageNumberColor: phantomColor, materialOverride: _phantomMaterial);
                }
                return targets.Count > 0;
            }
            case AttackType.Launch:
            {
                var targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
                if (targets.Count > 0)
                {
                    Vector3 wavePos = GetWavePosition(targets, -1);
                    bool probLaunchActive = playerState != null && playerState.HasBuff(BuffType.ProbabilityLaunch);
                    AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                        onHit: (enemy) =>
                        {
                            bool canLaunch = enemy.CanBeLaunched(cfg.poiseDamage);
                            if (!canLaunch && probLaunchActive && Random.value < 0.3f)
                            {
                                enemy.Stun(cfg.launchDuration * 0.5f);
                                canLaunch = true;
                            }
                            if (canLaunch)
                                enemy.Launch();
                        },
                        prefab: cfg.attackWavePrefab, alphaOverride: alpha,
                        damageNumberColor: phantomColor,
                        canInterruptCFrame: true, materialOverride: _phantomMaterial);
                }
                return targets.Count > 0;
            }
            default:
                return false;
        }
    }

    /// <summary>
    /// 折返波 — 被动触发（return_wave）。多列回旋镖：以目标列为中心 ±1列，
    /// 飞出 rangeRows 排 → 折返飞回，每段命中范围内所有敌人。
    /// </summary>
    public bool ExecuteReturnWave(AttackType attackType, int targetColumn, bool slashLeftToRight,
        float damageRatio, int rangeRows)
    {
        if (playerState == null || columnManager == null) return false;
        if (playerState.stageState != StageState.InProgress) return false;
        if (targetColumn < 0) return false;

        var cfg = GetConfig(attackType);
        if (cfg == null) return false;

        // 多列收集：targetColumn ± 1（钳位到有效列范围）
        List<Enemy> targets = new List<Enemy>();
        int colMin = Mathf.Max(0, targetColumn - 1);
        int colMax = Mathf.Min(columnManager.columnCount - 1, targetColumn + 1);
        for (int col = colMin; col <= colMax; col++)
        {
            var colTargets = columnManager.GetEnemiesInRange(col, rangeRows);
            foreach (var e in colTargets)
            {
                if (!e.isBoss || e.bossState == BossState.InCombat)
                    targets.Add(e);
            }
        }

        if (targets.Count == 0) return false;

        float finalDmg = GetFinalDamage(cfg) * GetStabPierceDamagePenalty();
        Vector3 wavePos = GetWavePosition(targets, targetColumn);
        Color waveColor = new Color(0.2f, 0.7f, 1f); // 青蓝色回旋镖
        float alpha = 0.85f;

        GameObject wavePrefab = _returnWavePrefab != null ? _returnWavePrefab : cfg.attackWavePrefab;
        wavePos.y = targets[0].transform.position.y + cfg.stabSpawnYOffset;

        AttackWave.CreateReturnWave(wavePos, cfg.damageType, finalDmg, targets, damageRatio,
            prefab: wavePrefab, zOffset: cfg.stabSpawnZOffset,
            alphaOverride: alpha, damageNumberColor: waveColor, colorOverride: waveColor);

        Debug.Log($"[AttackSystem] 回旋镖: type={attackType} dmg={finalDmg} returnRatio={damageRatio} cols=[{colMin},{colMax}] rangeRows={rangeRows} targets={targets.Count}");
        return true;
    }

    /// <summary>
    /// 连锁弹射 — 被动触发（chain_bounce）。Pierce命中后弹射至同行最近敌人，最多 maxBounces 次。
    /// </summary>
    public bool ExecuteChainBounce(AttackType attackType, int targetColumn,
        float damageRetention, int maxBounces)
    {
        if (playerState == null || columnManager == null || targetColumn < 0) return false;
        if (playerState.stageState != StageState.InProgress) return false;

        var cfg = GetConfig(attackType);
        if (cfg == null) return false;

        int effectiveRows = GetEffectiveRangeRows(cfg);
        var initialTargets = columnManager.GetEnemiesInRange(targetColumn, effectiveRows);
        initialTargets = initialTargets.FindAll(e => !e.isBoss || e.bossState == BossState.InCombat);
        if (initialTargets.Count == 0) return false;

        float baseDamage = GetFinalDamage(cfg) * GetStabPierceDamagePenalty();
        int totalBounces = 0;

        foreach (var startEnemy in initialTargets)
        {
            if (startEnemy == null || startEnemy.state == EnemyState.Dead) continue;

            Enemy current = startEnemy;
            float bounceDamage = baseDamage * damageRetention;

            for (int i = 0; i < maxBounces; i++)
            {
                Enemy next = FindNearestSameRowEnemy(current, targetColumn);
                if (next == null) break;

                // 直接造成弹射伤害
                next.TakeDamage(bounceDamage, cfg.damageType);

                // 连锁闪电视觉：LineRenderer连接 current → next
                StartCoroutine(CreateChainVisual(current, next));

                bounceDamage *= damageRetention;
                current = next;
                totalBounces++;
            }
        }

        Debug.Log($"[AttackSystem] 连锁弹射: col={targetColumn} retention={damageRetention} maxBounces={maxBounces} actual={totalBounces}");
        return totalBounces > 0;
    }

    /// <summary>
    /// 连锁弹射闪电连线视觉 — 使用 Chain 预制体连接两个敌人，紫色调 + 0.35s 渐隐
    /// </summary>
    private System.Collections.IEnumerator CreateChainVisual(Enemy from, Enemy to)
    {
        if (from == null || to == null) yield break;

        GameObject visualPrefab = _chainBounceVisualPrefab != null ? _chainBounceVisualPrefab : _chainBouncePrefab;
        if (visualPrefab == null) yield break;

        Vector3 fromPos = from.transform.position + Vector3.up * 0.5f;
        Vector3 toPos = to.transform.position + Vector3.up * 0.5f;
        Vector3 mid = (fromPos + toPos) * 0.5f;
        float dist = Vector3.Distance(fromPos, toPos);
        if (dist < 0.01f) yield break;

        var chainGo = Object.Instantiate(visualPrefab, mid, visualPrefab.transform.rotation);
        chainGo.name = "ChainBounce";
        var sr = chainGo.GetComponent<SpriteRenderer>();
        if (sr == null) { Destroy(chainGo); yield break; }

        // 闪电色调
        sr.color = new Color(0.6f, 0.2f, 1f, 1f);
        sr.sortingOrder = 100;

        // 保持原始大小和旋转，仅放置在两敌人中间

        // 渐隐
        float duration = 0.35f;
        float elapsed = 0f;
        while (elapsed < duration && sr != null)
        {
            elapsed += Time.deltaTime;
            float a = 1f - elapsed / duration;
            sr.color = new Color(0.6f, 0.2f, 1f, a);
            yield return null;
        }

        if (chainGo != null) Destroy(chainGo);
    }

    /// <summary>
    /// 寻找同行（同 rowIndex）不同列中最近的敌人（按列距离）
    /// </summary>
    private Enemy FindNearestSameRowEnemy(Enemy source, int excludeColumn)
    {
        if (columnManager == null || source == null) return null;

        int sourceRow = source.rowIndex;
        Enemy best = null;
        int bestDist = int.MaxValue;

        for (int col = 0; col < columnManager.columnCount; col++)
        {
            if (col == excludeColumn) continue;
            if (col == source.columnIndex) continue;

            Enemy candidate = columnManager.GetColumn(col)?.GetEnemyAtRow(sourceRow);
            if (candidate == null || candidate.state == EnemyState.Dead) continue;
            if (candidate.isBoss && candidate.bossState != BossState.InCombat) continue;

            int dist = Mathf.Abs(col - source.columnIndex);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// 强制执行 Stab（绕过冷却），直接指定最终伤害值，供狂怒大招等调用
    /// </summary>
    public bool ForceExecuteStab(int columnIndex, float damage)
    {
        if (playerState == null || columnManager == null || columnIndex < 0) return false;
        if (playerState.stageState != StageState.InProgress) return false;

        var cfg = GetConfig(AttackType.Stab);
        if (cfg == null) return false;

        int effectiveRows = GetEffectiveRangeRows(cfg);
        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, effectiveRows);
        targets = targets.FindAll(e => !e.isBoss || e.bossState == BossState.InCombat);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            wavePos.y = targets[0].transform.position.y + cfg.stabSpawnYOffset;
            AttackWave.Create(wavePos, cfg.damageType, damage * GetStabPierceDamagePenalty(), targets,
                prefab: cfg.attackWavePrefab, zOffset: cfg.stabSpawnZOffset);
        }

        Debug.Log($"[AttackSystem] 强制Stab 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    #endregion

    #region 解锁攻击注册表

    private Dictionary<string, AttackSkillConfig> _unlockedAttacks = new Dictionary<string, AttackSkillConfig>();
    private Dictionary<string, int> _unlockedAttackLevels = new Dictionary<string, int>();
    private Dictionary<string, float> _unlockedFloatValues = new Dictionary<string, float>();

    /// <summary>注册解锁的攻击技能（由 UnlockAttackExecutor 调用）</summary>
    public void RegisterUnlockedAttack(string unlockId, AttackSkillConfig config, int level, float floatValue)
    {
        _unlockedAttacks[unlockId] = config;
        _unlockedAttackLevels[unlockId] = level;
        _unlockedFloatValues[unlockId] = floatValue;
        Debug.Log($"[AttackSystem] 注册解锁攻击: {unlockId} Lv.{level} damage={config.damage} floatValue={floatValue}");
    }

    /// <summary>更新解锁攻击等级</summary>
    public void UpdateUnlockedAttackLevel(string unlockId, int level)
    {
        if (_unlockedAttackLevels.ContainsKey(unlockId))
            _unlockedAttackLevels[unlockId] = level;
    }

    /// <summary>尝试执行解锁攻击</summary>
    public bool TryExecuteUnlockedAttack(string unlockId, int targetColumn = -1)
    {
        if (playerState == null || playerState.stageState != StageState.InProgress) return false;
        if (!_unlockedAttacks.TryGetValue(unlockId, out var cfg)) return false;
        if (!_unlockedAttackLevels.TryGetValue(unlockId, out int level)) return false;

        // 解锁攻击伤害 = baseAttackConfig.damage + floatValue × (level - 1)
        float baseDmg = cfg.damage;
        float bonusPerLevel = _unlockedFloatValues.TryGetValue(unlockId, out float fv) ? fv : 0f;
        float finalDmg = (baseDmg + bonusPerLevel * (level - 1))
            * (UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f);

        List<Enemy> targets;
        if (targetColumn >= 0)
            targets = columnManager.GetEnemiesInRange(targetColumn, cfg.rangeRows);
        else
            targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);

        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, targetColumn);
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 解锁攻击 {unlockId} Lv.{level} 伤害:{finalDmg} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>获取解锁攻击的最终伤害值（供 UI 显示）</summary>
    public float GetUnlockedAttackDamage(string unlockId)
    {
        if (!_unlockedAttacks.TryGetValue(unlockId, out var cfg)) return 0f;
        if (!_unlockedAttackLevels.TryGetValue(unlockId, out int level)) return 0f;
        float bonusPerLevel = _unlockedFloatValues.TryGetValue(unlockId, out float fv2) ? fv2 : 0f;
        return cfg.damage + bonusPerLevel * (level - 1);
    }

    public void ResetUnlockedAttacks()
    {
        _unlockedAttacks.Clear();
        _unlockedAttackLevels.Clear();
        _unlockedFloatValues.Clear();
    }

    /// <summary>
    /// 获取特效目标时长（秒）。action-based模式下=cooldown/攻速，旧模式返回-1（自然时长）。
    /// 特效通过timeScale拉伸/压缩匹配此时长，确保视觉与锁定同步结束。
    /// </summary>
    private float GetVisualTargetDuration(AttackSkillConfig cfg)
    {
        if (!useActionBasedCooldown || cfg == null) return -1f;
        float speedMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetAttackSpeedMultiplier() : 1f;
        return cfg.cooldown / Mathf.Max(speedMult, 0.01f);
    }

    #endregion
}
