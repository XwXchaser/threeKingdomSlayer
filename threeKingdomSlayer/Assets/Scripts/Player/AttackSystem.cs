using System.Collections.Generic;
using UnityEngine;

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

        if (!playerState.IsAttackReady(attackType))
        {
            Debug.Log($"[AttackSystem] {attackType} 冷却中");
            return false;
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
            playerState.StartCooldown(attackType);
            UltimateSystem.Instance?.AddEnergyForAttack(attackType);

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
        // Stab 严格按 rangeRows 过滤，且只命中应战 Boss（排除未应战 Boss 导致 wave 位置错误）
        targets = targets.FindAll(e => e.rowIndex < effectiveRows && (!e.isBoss || e.bossState == BossState.InCombat));
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            wavePos.y = targets[0].transform.position.y + cfg.stabSpawnYOffset;
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                prefab: cfg.attackWavePrefab, zOffset: cfg.stabSpawnZOffset);
            WwiseAudioManager.Instance?.PostEvent("Player_Attack");
        }

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{finalDmg} 目标数:{targets.Count}");
        if (targets.Count > 0) ApplyDisplacementEffects(targets, AttackType.Stab, canInterruptCFrame: false);
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
            Vector3 wavePos = GetWavePosition(targets, -1);
            wavePos.y = targets[0].transform.position.y + cfg.slashSpawnYOffset;
            wavePos.z = targets[0].transform.position.z + cfg.slashSpawnZOffset;
            SweepEffect.Create(wavePos, cfg.damageType, finalDmg, targets, leftToRight,
                cfg.slashSweepHalfWidth, cfg.slashSweepAngle, cfg.slashSweepDuration,
                prefab: cfg.attackWavePrefab);
            WwiseAudioManager.Instance?.PostEvent("Player_Attack");
        }

        Debug.Log($"[AttackSystem] 斩击 方向:{(leftToRight ? "L→R" : "R→L")} 伤害:{finalDmg} 目标数:{targets.Count}");
        if (targets.Count > 0) ApplyDisplacementEffects(targets, AttackType.Slash, canInterruptCFrame: false);
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
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 穿刺 列{columnIndex} 伤害:{finalDmg} 目标数:{targets.Count}");
        if (targets.Count > 0) ApplyDisplacementEffects(targets, AttackType.Pierce, canInterruptCFrame: false);
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
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 横扫 伤害:{finalDmg} 目标数:{targets.Count}");
        if (targets.Count > 0) ApplyDisplacementEffects(targets, AttackType.Sweep, canInterruptCFrame: false);
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

            // 概率击飞 Buff：每次攻击时判定一次（对所有目标生效）
            bool probLaunchActive = playerState != null && playerState.HasBuff(BuffType.ProbabilityLaunch);

            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                onHit: (enemy) =>
                {
                    enemy.TakePoiseDamage(cfg.poiseDamage);

                    bool canLaunch = enemy.CanBeLaunched();
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
                prefab: cfg.attackWavePrefab,
                canInterruptCFrame: true);
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 击飞时间:{cfg.launchDuration}s 目标数:{targets.Count}");
        if (targets.Count > 0) ApplyDisplacementEffects(targets, AttackType.Launch, canInterruptCFrame: true);
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
            if (p == null) continue;
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
            WwiseAudioManager.Instance?.PostEvent("Player_Parry");
            return true;
        }

        // 无飞行物在范围内 → 对敌人执行招架伤害
        if (columnManager == null) return false;
        float finalDmg = GetFinalDamage(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count == 0) return false;

        foreach (var enemy in targets)
        {
            // TakeDamage 内部处理打断逻辑（canInterruptCFrame=true 可打断C技霸体）
            enemy.TakeDamage(finalDmg, cfg.damageType, canInterruptCFrame: true);
            if (enemy.isBoss)
                enemy.TakePoiseDamage(cfg.poiseDamage);
            enemy.CheckParryStunThresholds();
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 目标数:{targets.Count}");
        WwiseAudioManager.Instance?.PostEvent("Player_Parry");
        return true;
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

    /// <summary>
    /// 在成功攻击后应用位移效果（push_wave / convergence_wave）
    /// </summary>
    private void ApplyDisplacementEffects(List<Enemy> targets, AttackType attackType, bool canInterruptCFrame)
    {
        if (UpgradeEffectManager.Instance == null || columnManager == null) return;

        int pushDist = UpgradeEffectManager.Instance.GetPushWaveDistance();
        int convergence = UpgradeEffectManager.Instance.GetConvergenceStep();

        Debug.Log($"[Displacement] === START atkType={attackType} push={pushDist} conv={convergence} targets={targets.Count} canInterruptCFrame={canInterruptCFrame} ===");
        Debug.Log(columnManager.DumpColumns());

        if (pushDist > 0)
        {
            columnManager.ApplyPushWave(targets, pushDist, canInterruptCFrame);
            Debug.Log($"[Displacement] after PUSH:");
            Debug.Log(columnManager.DumpColumns());
        }

        if (convergence > 0)
        {
            float dmgPct = UpgradeEffectManager.Instance.GetConvergenceDamagePercent();
            columnManager.ApplyConvergenceWave(targets, convergence, dmgPct, canInterruptCFrame);
            Debug.Log($"[Displacement] after CONVERGENCE:");
            Debug.Log(columnManager.DumpColumns());
        }

        columnManager.PostDisplacementFillUp();
        Debug.Log($"[Displacement] after FILLUP:");
        Debug.Log(columnManager.DumpColumns());
        Debug.Log($"[Displacement] === END ===");
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
                targets = targets.FindAll(e => e.rowIndex < effectiveRows && (!e.isBoss || e.bossState == BossState.InCombat));
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
                    Vector3 wavePos = GetWavePosition(targets, -1);
                    wavePos.y = targets[0].transform.position.y + cfg.slashSpawnYOffset;
                    wavePos.z = targets[0].transform.position.z + cfg.slashSpawnZOffset;
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
                            enemy.TakePoiseDamage(cfg.poiseDamage * damageRatio);
                            bool canLaunch = enemy.CanBeLaunched();
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
                if (e.rowIndex < rangeRows && (!e.isBoss || e.bossState == BossState.InCombat))
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
        initialTargets = initialTargets.FindAll(e => e.rowIndex < effectiveRows && (!e.isBoss || e.bossState == BossState.InCombat));
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
        targets = targets.FindAll(e => e.rowIndex < effectiveRows && (!e.isBoss || e.bossState == BossState.InCombat));
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

    #endregion
}
