using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class ActiveSkillRunner : MonoBehaviour
{
    public static ActiveSkillRunner Instance
    {
        get
        {
            if (_instance == null)
                _instance = Object.FindObjectOfType<ActiveSkillRunner>();
            return _instance;
        }
        private set => _instance = value;
    }
    private static ActiveSkillRunner _instance;

    [Header("特效预制体（未配置时回退使用 TimedPassiveModule）")]
    [SerializeField] private GameObject _fireEffectPrefab;
    [Tooltip("冰雾特效预制体。为空时回退用 _fireEffectPrefab 并乘上冰蓝色占位")]
    [SerializeField] private GameObject _iceEffectPrefab;
    [SerializeField] private GameObject _arrowEffectPrefab;
    [SerializeField] private GameObject _cycloneEffectPrefab;
    [SerializeField] private WaveManager _waveManager;

    [Header("追命锁链表现")]
    [Tooltip("铁链链节贴图（**Inspector 引用**，不走 Resources / 字符串查找）。\n" +
             "占位可用 Assets/Sprites/Enemy/Enemy2/chain.png；留空 = 只显示锁镰、不显示链身。")]
    [SerializeField] private Sprite _chainLinkSprite;

    private readonly Dictionary<string, int> _chargeAttackShockwaveLayers = new Dictionary<string, int>();
    private ActiveSkillDefinition _armedDiseaseDefinition;
    private int _armedDiseaseLevel;
    private int _armedDiseaseLayers;

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
            Instance = null;
    }

    public bool TryActivate(ActiveSkillDefinition definition, int level)
    {
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress) return false;
        if (definition == null || level <= 0) return false;
        // 输入被拦下时（破防硬直、路线转场、三选一面板……）不能释放主动技能
        if (GameplayInputGate.IsBlocked && !GameplayInputGate.AllowSkillActivationWhileBlocked) return false;

        // **暗源消耗**（2026-11-16）：暗源只靠进攻获取 → 防御回血型玩法放不出技能。
        // 先查够不够（不够直接拒绝），释放**成功之后**才扣 → 失败不误扣。
        float cost = definition.GetShadowCost(level);
        if (cost > 0f && !PlayerState.Instance.HasShadowSource(cost))
        {
            Debug.Log($"[ActiveSkillRunner] 暗源不足：{definition.displayName} 需要 {cost:F0}，当前 {PlayerState.Instance.ShadowSource:F0}");
            return false;
        }

        bool activated = ActivateByType(definition, level);
        if (activated && cost > 0f) PlayerState.Instance.TrySpendShadowSource(cost);
        return activated;
    }

    /// <summary>按效果类型分发（从 TryActivate 里抽出来，便于“成功后扣暗源”）。</summary>
    private bool ActivateByType(ActiveSkillDefinition definition, int level)
    {
        switch (definition.activeEffectType)
        {
            case ActiveSkillEffectType.FireAoe:
                return ActivateFire(definition, level);
            case ActiveSkillEffectType.FireLine:
                return ActivateFireLine(definition, level);
            case ActiveSkillEffectType.ArrowRain:
                return ActivateArrow(definition, level);
            case ActiveSkillEffectType.Cyclone:
                return ActivateCyclone(definition, level);
            case ActiveSkillEffectType.ChargeAttackShockwave:
                return ActivateChargeAttackShockwave(definition, level);
            case ActiveSkillEffectType.Wave:
                return ActivateWave(definition, level);
            case ActiveSkillEffectType.Disease:
                return ActivateDisease(definition, level);
            case ActiveSkillEffectType.IceMist:
                return ActivateIceMist(definition, level);
            case ActiveSkillEffectType.AxeSmash:
                return ActivateAxeSmash(definition, level);
            case ActiveSkillEffectType.ChainPull:
                return ActivateChainPull(definition, level);
            default:
                return false;
        }
    }

    private bool ActivateIceMist(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedAoeLevels == null || level > definition.timedAoeLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateIceMist failed: timedAoeLevels null or level {level} > count {definition.timedAoeLevels?.Count}");
            return false;
        }
        var cfg = definition.timedAoeLevels[level - 1];
        var prefab = _iceEffectPrefab != null ? _iceEffectPrefab : _fireEffectPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[ActiveSkillRunner] ActivateIceMist failed: 没有可用的喷流特效 prefab（_iceEffectPrefab / _fireEffectPrefab 均为空）");
            return false;
        }
        if (cfg.columns == null || cfg.columns.Count == 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateIceMist failed: columns null or empty (level={level})");
            return false;
        }

        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        if (effect == null)
        {
            Debug.LogError("[ActiveSkillRunner] ActivateIceMist failed: ShootFireEffect component missing on prefab");
            Destroy(instance);
            return false;
        }

        // 占位表现：没有专属冰系特效时，把火焰素材乘上冰蓝色
        if (_iceEffectPrefab == null)
            effect.particleTint = new Color(0.55f, 0.85f, 1f, 1f);

        float chancePerHit = cfg.iceFreezeChancePerHit;
        effect.onSweepHit = enemy =>
        {
            if (enemy == null) return;
            enemy.ApplyIceHit(chancePerHit);
        };

        effect.PlaySweep(cfg.columns, cfg.damage, cfg.rangeRows, 0f, 0, 0f, DamageType.Ice);
        PlayerElementalBuffs.GrantIce(ElementalScope.SingleColumnHeavy);
        Debug.Log($"[ActiveSkillRunner] ActivateIceMist: columns=[{string.Join(",", cfg.columns)}] rows={cfg.rangeRows} dmg={cfg.damage} chancePerHit={chancePerHit}");
        return true;
    }

    private bool ActivateFire(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedAoeLevels == null || level > definition.timedAoeLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFire failed: timedAoeLevels null or level {level} > count {definition.timedAoeLevels?.Count}");
            return false;
        }
        var cfg = definition.timedAoeLevels[level - 1];
        var prefab = _fireEffectPrefab != null ? _fireEffectPrefab : TimedPassiveModule.Instance?.fireEffectPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[ActiveSkillRunner] ActivateFire failed: fireEffectPrefab is null");
            return false;
        }
        if (cfg.columns == null || cfg.columns.Count == 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFire failed: columns null or empty (level={level})");
            return false;
        }

        Debug.Log($"[ActiveSkillRunner] ActivateFire: spawning effect, columns=[{string.Join(",", cfg.columns)}], damage={cfg.damage}");
        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        if (effect == null)
        {
            Debug.LogError("[ActiveSkillRunner] ActivateFire failed: ShootFireEffect component missing on prefab");
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.columns, cfg.damage);
        return true;
    }

    private bool ActivateFireLine(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedAoeLevels == null || level > definition.timedAoeLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFireLine failed: timedAoeLevels null or level {level} > count {definition.timedAoeLevels?.Count}");
            return false;
        }
        var cfg = definition.timedAoeLevels[level - 1];
        var prefab = _fireEffectPrefab != null ? _fireEffectPrefab : TimedPassiveModule.Instance?.fireEffectPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[ActiveSkillRunner] ActivateFireLine failed: fireEffectPrefab is null");
            return false;
        }
        if (cfg.columns == null || cfg.columns.Count == 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFireLine failed: columns null or empty (level={level})");
            return false;
        }

        Debug.Log($"[ActiveSkillRunner] ActivateFireLine: spawning effect, columns=[{string.Join(",", cfg.columns)}], damage={cfg.damage}, critIfBurning={level >= 3}");
        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        if (effect == null)
        {
            Debug.LogError("[ActiveSkillRunner] ActivateFireLine failed: ShootFireEffect component missing on prefab");
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.columns, cfg.damage, -1, 0, 0f, level >= 3);
        return true;
    }

    private bool ActivateArrow(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedArrowLevels == null || level > definition.timedArrowLevels.Count)
            return false;
        var cfg = definition.timedArrowLevels[level - 1];
        var prefab = _arrowEffectPrefab != null ? _arrowEffectPrefab : TimedPassiveModule.Instance?.arrowEffectPrefab;
        if (prefab == null) return false;

        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<TimedArrowEffect>();
        if (effect == null)
        {
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.rowCount, cfg.arrowCount, cfg.damage);
        return true;
    }

    private bool ActivateCyclone(ActiveSkillDefinition definition, int level)
    {
        if (definition.waveLevels == null || level > definition.waveLevels.Count)
            return false;
        var cfg = definition.waveLevels[level - 1];
        var columnManager = AttackSystem.Instance?.columnManager;
        var prefab = _cycloneEffectPrefab != null ? _cycloneEffectPrefab : TimedPassiveModule.Instance?.cycloneEffectPrefab;
        if (columnManager == null || prefab == null || cfg.rangeRows <= 0)
            return false;

        float zoneDuration = cfg.cycloneDuration > 0f ? cfg.cycloneDuration : 2f;

        void SpawnZone(int row)
        {
            var instance = Instantiate(prefab);
            var zone = instance.GetComponent<CycloneZone>();
            if (zone == null)
            {
                Destroy(instance);
                return;
            }
            zone.Setup(columnManager, row, cfg.damage, cfg.landingDamage,
                cfg.bossPoiseDamagePercent, 1.2f, zoneDuration);
        }

        for (int row = 0; row < cfg.rangeRows; row++)
            SpawnZone(row);

        // Boss 固定在第二排，若 rangeRows 覆盖不到则额外创建 Boss 排 Zone
        for (int col = 0; col < columnManager.columnCount; col++)
        {
            var boss = columnManager.GetCombatBossCoveringColumn(col);
            if (boss != null && boss.rowIndex >= cfg.rangeRows)
            {
                SpawnZone(boss.rowIndex);
                break; // 一个 Boss 排 Zone 即可覆盖所有列
            }
        }

        return true;
    }

    private bool ActivateWave(ActiveSkillDefinition definition, int level)
    {
        if (definition.waveLevels == null || level > definition.waveLevels.Count)
            return false;

        var cfg = definition.waveLevels[level - 1];
        var waveManager = _waveManager != null ? _waveManager : WaveManager.Instance;
        if (waveManager == null || cfg.rangeRows <= 0 || cfg.damage < 0)
            return false;

        waveManager.TriggerWave(0, cfg.rangeRows - 1, cfg.damage, cfg.bossPoiseDamagePercent);
        return true;
    }

    /// <summary>
    /// **抡斧劈砍**（REQ-008 · 参考影之刃零「巨凶斧」：破坏型 / 高破盾能力）。
    ///
    /// 特点：**无视盾牌减伤 + 必定破盾** —— 正面砍盾卫也能全额结算并直接破盾。
    /// 这是巨斧相对其它招式唯一不可替代的价值（其余伤害用枪突/横斩也能打）。
    ///
    /// ★ **表现（占位）**：用户 2026-11-14 指示「斧头暂时用这杆长枪代替」⇒
    ///   另起一个**长枪实例**（不动玩家自己的姿态武器），**抡起 → 劈下**，命中帧结算伤害。
    ///   正式版换成大斧模型/动作时，只需换这一个实例与两段轨迹。
    /// </summary>
    private bool ActivateAxeSmash(ActiveSkillDefinition definition, int level)
    {
        if (definition.axeSmashLevels == null || level > definition.axeSmashLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateAxeSmash failed: levels missing for Lv.{level}");
            return false;
        }

        var cfg = definition.axeSmashLevels[level - 1];
        var columnManager = AttackSystem.Instance != null ? AttackSystem.Instance.columnManager : null;
        if (columnManager == null || cfg.rangeRows <= 0 || cfg.damage <= 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateAxeSmash failed: rows={cfg.rangeRows} damage={cfg.damage}");
            return false;
        }

        var targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets == null || targets.Count == 0)
        {
            Debug.Log("[ActiveSkillRunner] 抡斧劈砍：前方没有敌人");
            return false;
        }

        StartCoroutine(RunAxeSmash(cfg, targets));
        return true;
    }

    /// <summary>
    /// 抡斧劈砍的表现参数在 <see cref="AxeSmashPose"/>（单一来源，编辑器调姿工具共用）。
    /// </summary>

    /// <summary>抡斧的表现与结算：抡起 → 劈下 → **命中帧结算** → 停顿 → 收势。</summary>
    private System.Collections.IEnumerator RunAxeSmash(AxeSmashLevelConfig cfg, List<Enemy> targets)
    {
        var player = PlayerState.Instance;
        if (player == null) yield break;

        // ── 落点：命中的敌人里**最靠前的那一排**（rowIndex 最小）的胸口 ──
        //   用敌人自己的 transform 取位置（排位 z = Enemies 根节点 + (maxRows-1-row)*(-rowSpacing)），
        //   不写死常量 ⇒ rangeRows 变化、阵型换预设都自动跟着走。
        Vector3 impactPoint = Vector3.zero;
        bool hasTarget = false;
        int bestRow = int.MaxValue;
        for (int i = 0; i < targets.Count; i++)
        {
            var e = targets[i];
            if (e == null) continue;
            if (!hasTarget || e.rowIndex < bestRow)
            {
                hasTarget = true;
                bestRow = e.rowIndex;
                impactPoint = e.transform.position;
            }
        }
        if (!hasTarget) yield break;
        // 胸口高度 + 往相机挪一点（3D 斧头要压在 2D 敌人前面，否则会被精灵盖住）
        impactPoint = AxeSmashPose.ImpactPoint(impactPoint);

        GameObject axe = null;
        MeshWeaponVisual axeVisual = null;
        var weaponPrefab = cfg.weaponPrefab != null ? cfg.weaponPrefab : ResolvePlayerWeaponPrefab();
        Vector3 tipLocal = Vector3.zero;      // 刃尖锚点（Prefab 局部）
        if (weaponPrefab != null)
        {
            axe = Instantiate(weaponPrefab);
            axe.name = "Axe3D";
            var vis = axe.GetComponentInChildren<MeshWeaponVisual>();
            axeVisual = vis;
            if (vis != null) vis.SetSorting(100);   // ★ 技能表现必须在敌人精灵**之上**（否则被人群挡住）
            // ★ 用 Prefab 自带的 Tip 锚点反解握把位置：刃口必然落在目标胸口，
            //   不写死网格轴向（模型朝向改了也不用重调）。
            //   实例刚生成、根节点在原点且无旋转缩放 ⇒ 此时世界坐标 = Prefab 局部坐标。
            tipLocal = vis != null ? vis.TipPosition : Vector3.zero;
            axe.transform.localScale = Vector3.one * AxeSmashPose.RuntimeScale;
        }

        // ★ 三帧位姿：美术 2026-11-15 在 Battle 场景里摆定，原样照搬（Start 起手 → Apex 抡到最高 → End 砸下＝命中帧）。
        //   **绝对坐标**：本技能是“对排的范围伤害”，伤害按排结算，武器表现不锁任何敌人的位置。
        Vector3 startGrip = AxeSmashPose.StartGrip();
        Quaternion startRot = AxeSmashPose.StartRot;
        Vector3 apexGrip = AxeSmashPose.ApexGrip();
        Quaternion apexRot = AxeSmashPose.ApexRot;
        Vector3 endGrip = AxeSmashPose.EndGrip();
        Quaternion endRot = AxeSmashPose.EndRot;

        if (axe != null)
        {
            // ① 蓄势：把斧举起来。**ease-out** ⇒ 到位前的后半段明显变慢（蓄势感由“减速到位”表达，不抖动）
            float t = 0f;
            while (t < AxeSmashPose.WindUpSeconds)
            {
                t += Time.deltaTime;
                float kPos = AxeSmashPose.EaseWindUpPos(t / AxeSmashPose.WindUpSeconds);
                float kRot = AxeSmashPose.EaseWindUpRot(t / AxeSmashPose.WindUpSeconds);
                axe.transform.position = Vector3.Lerp(startGrip, apexGrip, kPos);
                axe.transform.rotation = Quaternion.Slerp(startRot, apexRot, kRot);
                yield return null;
            }
            axe.transform.position = apexGrip;
            axe.transform.rotation = apexRot;

            // ② 绷住：最高点停一拍（张力 —— 这一拍就是“蓄势”的观感来源）
            t = 0f;
            while (t < AxeSmashPose.ApexHoldSeconds) { t += Time.deltaTime; yield return null; }

            // ③ 劈下：**要快** —— 出手即有速度，末端刃口甩下去砸中
            //    ★ 挥空音（金属风声）跟出手一起播
            if (cfg.swingClip != null) AudioManager.Instance?.PlaySfx(cfg.swingClip, 1f);
            else AudioManager.Instance?.PlayHeavySwing();
            t = 0f;
            while (t < AxeSmashPose.ChopSeconds)
            {
                t += Time.deltaTime;
                float kPos = AxeSmashPose.EaseChopPos(t / AxeSmashPose.ChopSeconds);
                float kRot = AxeSmashPose.EaseChopRot(t / AxeSmashPose.ChopSeconds);
                axe.transform.position = Vector3.Lerp(apexGrip, endGrip, kPos);
                axe.transform.rotation = Quaternion.Slerp(apexRot, endRot, kRot);
                yield return null;
            }
            axe.transform.position = endGrip;
            axe.transform.rotation = endRot;
        }

        // ③ 命中帧：刃口在目标胸口 → 结算（无视盾牌减伤 + 必定破盾）
        //    ★ 命中感四件套：斧身**轻微**高光（peak 0.35，不用全白剪影）＋ 命中音 ＋ 镜头推近 ＋ 命中点对齐
        //      （目标侧已有的：敌人局部卡肉 0.14s ＋ 像素特效 ＋ 震屏 ＋ 伤害数字）
        if (axeVisual != null) axeVisual.PlayFlash(0.06f, 0.35f);
        if (cfg.impactClip != null) AudioManager.Instance?.PlaySfx(cfg.impactClip, 1f);
        else AudioManager.Instance?.PlayHeavyHit();
        // ★ 音效第二层：低频“闷”响（PBZ 演出语法④：极短金属交击 ＋ 一记低频闷响）
        AudioManager.Instance?.PlayHeavyThud();
        CameraFeedbackController.Instance?.RequestImpactPush(
            AxeSmashPose.ImpactCameraPushDegrees, AxeSmashPose.ImpactCameraPushHold);
        // ★ 重武器镜头：被“顶开”一下（低频单次大位移），与普通命中的高频小抖区分开
        CameraFeedbackController.Instance?.RequestImpactSway(impactPoint);

        int hit = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            var enemy = targets[i];
            if (enemy == null) continue;
            // ★ 每个敌人用**自己的**胸口位置做命中点（范围伤害：各排各算，数字/特效不落在同一点）
            Vector3 hitPoint = enemy.transform.position + new Vector3(0f, AxeSmashPose.ImpactHeightOffset, -AxeSmashPose.DepthInFrontOfRow);
            enemy.TakeDamage(cfg.damage, DamageType.Slash, countsForCombo: true,
                feedbackStrength: HitFeedbackStrength.Heavy, isHeavyAttack: true,
                ignoreShieldBlock: cfg.ignoreShieldBlock, poiseDamage: cfg.poiseDamage,
                impactPosition: hitPoint);
            hit++;
        }
        Debug.Log($"[ActiveSkillRunner] 抡斧劈砍: 命中 {hit} 个 rows={cfg.rangeRows} damage={cfg.damage} 无视盾减伤={cfg.ignoreShieldBlock} 削韧={cfg.poiseDamage} 落点={impactPoint}");

        // ★ 砸地裂纹特效：**放在伤害结算之后**（表现层的异常绝不能挡住伤害）
        //    位置＝斧刃实际入地点：取斧 mesh 最低点（局部坐标常量）投影到地面 ⇒ 美术改三帧姿态也会自动跟随
        if (cfg.effectPrefab != null && axe != null)
        {
            Vector3 lowestWorld = axe.transform.TransformPoint(AxeSmashPose.AxeLowestVertexRootLocal);
            Vector3 contact = new Vector3(lowestWorld.x, 0f, lowestWorld.z);
            var crack = Instantiate(cfg.effectPrefab);
            crack.transform.position = contact;
            var crackFx = crack.GetComponent<GroundCrackEffect>();
            if (crackFx != null) crackFx.Play(contact, AxeSmashPose.GroundCrackDiameter);
            else Destroy(crack);
        }

        if (axe != null)
        {
            // ④ 命中停顿：斧头压在目标上不动（让打击感立住）
            float t = 0f;
            while (t < AxeSmashPose.HitStopSeconds) { t += Time.deltaTime; yield return null; }

            // ⑤ 渐变消失：**End 就是最终位置**（不再往下沉 / 不再往前钻），原地淡出
            //    ★ 2026-11-16 用户口径：End 之后不该还有位移，之前那版“收势”会把斧头继续推进 0.7 单位
            //      并多转 26°，看着像沉到地里；现在改为原地淡出（MeshWeaponVisual.SetAlpha 会切到淡出材质）。
            if (axeVisual != null)
            {
                float fade = AxeSmashPose.FollowThroughSeconds;
                var fadeTween = GameplayPause.Bind(DOTween.To(
                    () => axeVisual.Alpha,
                    v => axeVisual.SetAlpha(v),
                    0f, fade).SetEase(Ease.InQuad));
                float elapsed = 0f;
                while (elapsed < fade)
                {
                    elapsed += Time.deltaTime;
                    if (fadeTween == null || !fadeTween.IsActive()) break;   // 被打断（对象销毁等）就别空转
                    yield return null;
                }
                if (axeVisual != null) axeVisual.SetAlpha(0f);
            }
            Destroy(axe);
        }
    }

    /// <summary>
    /// **追命锁链**（REQ-009 · 参考影之刃零「猎人的追命锁镰」：钩索勾住远处的锚点）。
    ///
    /// 锁定**最远**（rowIndex 最大）且**最弱**（当前血量最低）的敌人，把它拉到**本列最前的空格**。
    /// ⇒ 这是**排序工具**（把躲在后排的脆皮 / 远程 / 阴兵拽到前排解决），不是伤害工具。
    /// ⚠ 落点**不跨列** ⇒ 不会与其它敌人重叠，也不走补齐通道（`design/immutable-constraints.md`）。
    ///
    /// ★ **表现（占位）**：用户 2026-11-14 指示「锁链暂时用这杆长枪代替」⇒
    ///   ① **锁镰 = 长枪实例**（飞向目标）；② **铁链 = 弹簧骨链**（`ChainLinkRig`，程序生成骨链）；
    ///   ③ 三段动画：**甩出 → 绷紧（拽人）→ 回收**（见 `ChainLinkRig` 的设计注释）。
    /// </summary>
    private bool ActivateChainPull(ActiveSkillDefinition definition, int level)
    {
        if (definition.chainPullLevels == null || level > definition.chainPullLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateChainPull failed: levels missing for Lv.{level}");
            return false;
        }

        var cfg = definition.chainPullLevels[level - 1];
        var columnManager = AttackSystem.Instance != null ? AttackSystem.Instance.columnManager : null;
        var player = PlayerState.Instance;
        if (columnManager == null || player == null) return false;

        // 目标：最远（rowIndex 最大）→ 同排再取最弱（当前血量最低）
        var all = columnManager.GetEnemiesByRowLimit(999);
        Enemy target = null;
        int bestRow = -1;
        float bestHp = float.MaxValue;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null) continue;
            if (cfg.maxRange > 0f && Vector3.Distance(e.transform.position, player.transform.position) > cfg.maxRange) continue;
            if (e.rowIndex < bestRow) continue;
            if (e.rowIndex == bestRow && e.currentHealth >= bestHp) continue;
            target = e;
            bestRow = e.rowIndex;
            bestHp = e.currentHealth;
        }
        if (target == null)
        {
            Debug.Log("[ActiveSkillRunner] 追命锁链：没有可锁定的目标");
            return false;
        }

        // 落点：本列**最前的空格**（不跨列 ⇒ 不重叠）
        int col = target.columnIndex;
        int targetRow = -1;
        for (int r = 0; r < 64; r++)
        {
            if (columnManager.GetEnemyAt(col, r) == null) { targetRow = r; break; }
        }
        if (targetRow < 0 || targetRow >= target.rowIndex)
        {
            Debug.Log($"[ActiveSkillRunner] 追命锁链：{target.DebugTag} 已在最前（或本列无空位），不拉");
            return false;
        }

        StartCoroutine(RunChainPull(cfg, target, targetRow, bestRow, col));
        return true;
    }

    /// <summary>锁链的表现与结算：甩出 → 绷紧拽人 → 回收。</summary>
    private System.Collections.IEnumerator RunChainPull(ChainPullLevelConfig cfg, Enemy target, int targetRow, int oldRow, int col)
    {
        var player = PlayerState.Instance;
        if (player == null) yield break;

        Vector3 hand = player.transform.position + new Vector3(0f, 1.2f, -0.4f);
        Vector3 targetPos = target.transform.position;

        // ① 锁镰（占位：长枪实例）
        GameObject hook = null;
        var weaponPrefab = cfg.weaponPrefab != null ? cfg.weaponPrefab : ResolvePlayerWeaponPrefab();
        if (weaponPrefab != null)
        {
            hook = Instantiate(weaponPrefab);
            hook.name = "ChainHookPlaceholder";
            var vis = hook.GetComponentInChildren<MeshWeaponVisual>();
            if (vis != null) vis.SetSorting(100);   // ★ 同上：锁镰要压在敌人之上
            hook.transform.localScale = Vector3.one * 0.9f;
            // ★ 3D 模型在透视相机下靠**深度**排序，sortingOrder 压不过 2D 精灵
            //   ⇒ 把锁镰拉到**更靠近相机**的 z（相机在 z=-10，z 越小越靠前）。
            hook.transform.position = hand + new Vector3(0f, 0f, -1.2f);
            hook.transform.rotation = Quaternion.LookRotation((targetPos - hand).normalized, Vector3.up);
        }

        // ② 铁链骨链（程序生成的骨链；正式链子换“每节怎么画”即可，解算器不变）
        //    ★ 架构 ⑥：链的形状与手感参数**全部来自技能资产**（原来散在代码里当魔法数字）
        var chainSettings = new ChainLinkSettings
        {
            SegmentCount = cfg.chainSegmentCount,
            SegmentLength = cfg.chainSegmentLength,
            LinkScale = cfg.chainLinkScale,
            Stiffness = cfg.chainStiffness,
            Damping = cfg.chainDamping,
            GravityScale = cfg.chainGravityScale,
        };
        var rig = ChainLinkRig.Create(player.transform,
            cfg.chainLinkSprite != null ? cfg.chainLinkSprite : _chainLinkSprite, chainSettings);
        if (rig != null) rig.Setup(hand, hand);

        // ③ 甩出：链头（枪）飞向目标
        float throwTime = Mathf.Max(0.05f, cfg.chainThrowSeconds);
        float t = 0f;
        while (t < throwTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / throwTime);
            Vector3 pos = Vector3.Lerp(hand, targetPos, k) + new Vector3(0f, 0f, -1.2f);
            if (hook != null) hook.transform.position = pos;
            if (rig != null) { rig.SetHeadTarget(pos); rig.SetTail(hand); }
            yield return null;
        }

        // ④ 命中：链绷紧 ⇒ 目标被拽向最前排
        if (rig != null) rig.SetTaut(true);
        Vector3 from = target.transform.position;
        int startRow = target.rowIndex;
        target.SetRowIndex(targetRow);      // 逻辑位置立即更新（射程 / AI 判定即刻生效）
        Vector3 to = target.transform.position;
        target.transform.position = from;   // 视觉从原位开始，被链子拽过去
        float pullTime = Mathf.Max(0.08f, cfg.pullSeconds);
        t = 0f;
        while (t < pullTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pullTime);
            target.transform.position = Vector3.Lerp(from, to, k * (2f - k));   // ease-out：先快后慢
            if (hook != null) hook.transform.position = Vector3.Lerp(targetPos, to, k);
            if (rig != null) rig.SetHeadTarget(hook != null ? hook.transform.position : target.transform.position);
            yield return null;
        }

        if (cfg.damage > 0)
            target.TakeDamage(cfg.damage, DamageType.Stab, countsForCombo: true,
                feedbackStrength: HitFeedbackStrength.Light);

        // ⑤ 回收：链头飞回玩家
        if (rig != null) rig.SetTaut(false);
        Vector3 backFrom = hook != null ? hook.transform.position : to;
        float returnTime = Mathf.Max(0.05f, cfg.chainReturnSeconds);
        t = 0f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / returnTime);
            Vector3 pos = Vector3.Lerp(backFrom, hand, k);
            if (hook != null) hook.transform.position = pos;
            if (rig != null) rig.SetHeadTarget(pos);
            yield return null;
        }

        if (rig != null) Destroy(rig.gameObject);
        if (hook != null) Destroy(hook);

        Debug.Log($"[ActiveSkillRunner] 追命锁链: {target.DebugTag} row {startRow}→{targetRow} col={col}");
    }

    /// <summary>取玩家的长枪预制体（占位武器：斧头 / 锁镰都用它）。</summary>
    private static GameObject ResolvePlayerWeaponPrefab()
    {
        var hero = PlayerState.Instance != null ? PlayerState.Instance.heroConfig : null;
        var parry = hero != null ? hero.GetSkillConfig(AttackType.Parry) : null;
        return parry != null ? parry.attackWavePrefab : null;
    }

    private bool ActivateDisease(ActiveSkillDefinition definition, int level)
    {
        if (definition.diseaseLevels == null || level > definition.diseaseLevels.Count)
            return false;

        var config = definition.diseaseLevels[level - 1];
        if (config.totalDamage <= 0 || config.durationSeconds <= 0)
            return false;

        _armedDiseaseDefinition = definition;
        _armedDiseaseLevel = level;
        _armedDiseaseLayers++;
        Debug.Log($"[ActiveSkillRunner] 染病已武装: {definition.displayName} Lv.{level}, layers={_armedDiseaseLayers}");
        return true;
    }

    public bool ConsumeArmedDisease(Enemy enemy)
    {
        if (_armedDiseaseDefinition == null || enemy == null)
            return false;

        var config = _armedDiseaseDefinition.diseaseLevels[_armedDiseaseLevel - 1];
        EnemyStatusDisease.ApplyTo(enemy, config.totalDamage, config.durationSeconds, _armedDiseaseLayers, smartSpread: config.smartSpread);
        Debug.Log($"[ActiveSkillRunner] 染病附着: {enemy.DebugTag}, totalDamage={config.totalDamage}, duration={config.durationSeconds}s, layers={_armedDiseaseLayers}");
        _armedDiseaseDefinition = null;
        _armedDiseaseLevel = 0;
        _armedDiseaseLayers = 0;
        return true;
    }

    private bool ActivateChargeAttackShockwave(ActiveSkillDefinition definition, int level)
    {
        if (definition.chargeAttackShockwaveLevels == null || level > definition.chargeAttackShockwaveLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateChargeAttackShockwave failed: levels missing for Lv.{level}");
            return false;
        }

        var cfg = definition.chargeAttackShockwaveLevels[level - 1];
        if (cfg.rangeRows <= 0 || cfg.damage < 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateChargeAttackShockwave failed: invalid rows={cfg.rangeRows} damage={cfg.damage}");
            return false;
        }

        if (!_chargeAttackShockwaveLayers.ContainsKey(definition.upgradeId))
            _chargeAttackShockwaveLayers[definition.upgradeId] = 0;
        _chargeAttackShockwaveLayers[definition.upgradeId]++;
        Debug.Log($"[ActiveSkillRunner] 主动冲击波充能+1: {definition.displayName} 当前 {_chargeAttackShockwaveLayers[definition.upgradeId]} 层");
        return true;
    }

    public List<ActiveChargeAttackShockwaveConsumeResult> ConsumeAllChargeAttackShockwaves()
    {
        var results = new List<ActiveChargeAttackShockwaveConsumeResult>();
        foreach (var pair in _chargeAttackShockwaveLayers)
        {
            if (pair.Value <= 0) continue;
            var entry = ActiveSkillInventory.Instance?.GetEntry(pair.Key);
            if (entry == null || entry.definition == null || entry.definition.chargeAttackShockwaveLevels == null || entry.level > entry.definition.chargeAttackShockwaveLevels.Count)
                continue;

            results.Add(new ActiveChargeAttackShockwaveConsumeResult
            {
                upgradeId = pair.Key,
                layers = pair.Value,
                config = entry.definition.chargeAttackShockwaveLevels[entry.level - 1]
            });
        }

        for (int i = 0; i < results.Count; i++)
            _chargeAttackShockwaveLayers[results[i].upgradeId] = 0;
        return results;
    }

    public void ResetAll()
    {
        _chargeAttackShockwaveLayers.Clear();
        _armedDiseaseDefinition = null;
        _armedDiseaseLevel = 0;
        _armedDiseaseLayers = 0;
    }
}

public struct ActiveChargeAttackShockwaveConsumeResult
{
    public string upgradeId;
    public int layers;
    public ChargeAttackShockwaveLevelConfig config;
}

