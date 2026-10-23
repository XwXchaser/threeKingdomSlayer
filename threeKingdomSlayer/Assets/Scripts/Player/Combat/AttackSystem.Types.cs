using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

/// <summary>
    /// AttackSystem 的**攻击类型实现**：每种招式（Stab / Slash / Pierce / Sweep / Launch / Parry）
    /// 的出招流程、命中判定与表现触发。
    ///
    /// 按机制从 <c>AttackSystem.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 AttackSystem.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class AttackSystem : MonoBehaviour
{
    #region 攻击类型实现

    private float GetStabRayYaw(int columnIndex, float baseLength)
    {
        if (useStabRayAngleOverrides && stabRayInwardAngleOverrides != null && columnIndex < stabRayInwardAngleOverrides.Length)
            return stabRayInwardAngleOverrides[columnIndex];

        float columnOffset = StageFormation != null
            ? StageFormation.GetFormationOffset(columnIndex, 0)
            : (columnIndex - 2) * 2f;
        return Mathf.Atan2(columnOffset, baseLength * 2f) * Mathf.Rad2Deg;
    }

    private float GetStabVisualStartXOffset(int columnIndex)
    {
        if (stabVisualStartXOffsets != null && columnIndex >= 0 && columnIndex < stabVisualStartXOffsets.Length)
            return stabVisualStartXOffsets[columnIndex];
        return 0f;
    }

    /// <summary>本招式的配置：招式表解析出的招式资产优先，否则按攻击类型取武将默认配置</summary>
    private AttackSkillConfig GetConfig(AttackType type)
    {
        if (_currentConfigOverride != null && _currentConfigOverride.attackType == type)
            return _currentConfigOverride;
        return playerState?.heroConfig?.GetSkillConfig(type);
    }

    private bool ExecuteStab(int columnIndex)
    {
        var cfg = GetConfig(AttackType.Stab);
        if (cfg == null || columnIndex < 0 || columnManager == null || cfg.attackWavePrefab == null) return false;
        _lastStabColumn = columnIndex;

        float finalDmg = GetFinalDamage(cfg) * GetAttackRangeDamagePenalty();
        // 打断标记必须在发起时固化：穿刺等伤害在特效回调里延迟结算。
        bool chargeInterrupt = InterruptsCavalryCharge(cfg);
        int effectiveRows = GetEffectiveRangeRows(cfg);
        int visualRangeRows = effectiveRows;
        Enemy coveredBoss = columnManager.GetCombatBossCoveringColumn(columnIndex);
        if (coveredBoss != null)
            visualRangeRows = Mathf.Max(visualRangeRows, coveredBoss.rowIndex + 1);
        var targetColumn = columnManager.GetColumn(columnIndex);
        if (targetColumn != null)
        {
            for (int i = 0; i < targetColumn.enemies.Count; i++)
            {
                var enemy = targetColumn.enemies[i];
                if (enemy != null && enemy.isBoss && enemy.bossState == BossState.InCombat)
                {
                    visualRangeRows = Mathf.Max(visualRangeRows, enemy.rowIndex + 1);
                    break;
                }
            }
        }
        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        float spacing = StageFormation != null ? StageFormation.GetRowSpacing() : 2.5f;
        Vector3 startPosition = new Vector3(playerPos.x, playerPos.y + cfg.stabSpawnYOffset, -5.5f);
        float yaw = GetStabRayYaw(columnIndex, spacing);
        float baseLength = spacing * 2f;
        float rayLength = baseLength + (visualRangeRows - 1) * spacing;

        // 枪尾位置自由：只要求枪尖能命中本列。
        // 射线方向由「自由度很高的枪尾 → 本列目标点」决定，因此自然产生复合角度的斜向刺入，而不是单一轴旋转。
        Vector3 baseAimDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 targetPosition = startPosition + baseAimDirection * rayLength;
        Vector3 tailPosition = startPosition
            + Vector3.right * cfg.stabTailOffsetRight
            + Vector3.up * cfg.stabTailOffsetUp
            + Vector3.forward * cfg.stabTailOffsetForward;
        Vector3 rayDirection = (targetPosition - tailPosition).normalized;
        Vector3 rayOrigin = tailPosition;
        var hitTargets = new List<Enemy>();
        var pushedTargets = new List<Enemy>();

        // 连段交接：上一段还没结束的戳击交出当前姿态后被立刻结束，避免两把枪重叠
        StabStartPose startPose = StabStartPose.None;
        if (_lastStabEffect != null)
        {
            startPose = _lastStabEffect.CapturePose();
            _lastStabEffect.HandOff();
            _lastStabEffect = null;
        }
        else if (_lastStabPose.valid && Time.time - _lastStabPoseTime <= StabPoseHandoffWindow)
        {
            // 上一段特效已被外部清理（例如路线转场清特效），仍沿用其末态，避免连段断裂
            startPose = _lastStabPose;
        }

        LastStabTargetEnemy = null;
        _lastStabEffect = StabSweepEffect.Create(cfg.attackWavePrefab, _stabSpeedSprite, rayOrigin, targetPosition, columnIndex, effectiveRows, visualRangeRows,
            finalDmg, cfg.damageType, columnManager, coveredBoss,
            enemy =>
            {
                if (LastStabTargetEnemy == null)
                    LastStabTargetEnemy = enemy;
                hitTargets.Add(enemy);
                // 位移与伤害同帧结算（招式自带击退 + 升级加成，见 ApplyMovePushBack）
                ApplyMovePushBack(cfg, enemy, pushedTargets);
            },
            enemy => ActiveSkillRunner.Instance?.ConsumeArmedDisease(enemy) ?? false,
            () =>
            {
                AudioManager.Instance?.PostEvent("Stab_Hit");
                UltimateSystem.Instance?.AddEnergyForAttack(AttackType.Stab);
                OnAttackPerformed?.Invoke(AttackType.Stab, columnIndex, true);
            },
            () =>
            {
                if (pushedTargets.Count > 0)
                    columnManager.PostDisplacementFillUp(pushedTargets);
                // Bug #2 修复：Stab 的 hit 回调是异步的（射线穿过才触发），
                // 如果在这里立刻清 _currentConfigOverride，hit 回调里 GetConfig(Stab) 会退回
                // 默认 Zhangfei_Stab.pushBackRows=0 → pushDist=0 → 不尝试推退。
                // 等射线播完才清；下一帧攻击的 line 297 会自动覆盖。
                _currentConfigOverride = null;
            },
            cfg.stabVisualReachOffset,
            GetStabVisualStartXOffset(columnIndex),
            cfg.stabVisualTargetRandomRadius,
            baseLength,
            GetAttackDuration(cfg), chargeInterrupt, cfg.hitReactionDirection,
            false, StabMotionParams.FromConfig(cfg), startPose, _chargeStabVisual,
            enableWeaponAfterimage: true);
        AudioManager.Instance?.PostEvent("Player_Attack");

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{finalDmg} 射程:{effectiveRows} 视觉射程:{visualRangeRows}");
        return true;
    }

    private bool ExecuteSlash(bool leftToRight, float visualTilt)
    {
        var cfg = GetConfig(AttackType.Slash);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetAttackRangeDamagePenalty();
        bool chargeInterrupt = InterruptsCavalryCharge(cfg);
        int effectiveRows = GetEffectiveRangeRows(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(effectiveRows);
        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        Vector3 wavePos = new Vector3(0, playerPos.y + cfg.slashSpawnYOffset, playerPos.z + cfg.slashSpawnZOffset);
        var hitTargets = new List<Enemy>();

        Debug.Log($"[SlashTilt] AttackSystem leftToRight={leftToRight} tilt={visualTilt:F2} movementTilt={visualTilt:F2}");
        SweepEffect.Create(wavePos, cfg.damageType, finalDmg, targets, leftToRight,
            cfg.slashSweepHalfWidth, cfg.slashSweepAngle, cfg.slashSweepDuration,
            prefab: cfg.attackWavePrefab,
            onHit: enemy => hitTargets.Add(enemy),
            onFirstHit: () =>
            {
                AudioManager.Instance?.PostEvent("Slash_Hit");
                UltimateSystem.Instance?.AddEnergyForAttack(AttackType.Slash);
                OnAttackPerformed?.Invoke(AttackType.Slash, -1, leftToRight);
            },
            onAllHit: () => ApplySlashDirectionalPush(hitTargets, leftToRight),
            targetDuration: GetAttackDuration(cfg),
            hitReactionDirection: leftToRight ? HitReactionDirection.Left : HitReactionDirection.Right,
            rotateSprite1: _stabRotate1Sprite, rotateSprite2: _stabRotate2Sprite,
            visualPathTilt: visualTilt,
            useEnhancedSlashMotion: true,
            interruptsCavalryCharge: chargeInterrupt,
            enableWeaponAfterimage: true);
        AudioManager.Instance?.PostEvent("Player_Attack");

        Debug.Log($"[AttackSystem] 斩击 方向:{(leftToRight ? "L→R" : "R→L")} 伤害:{finalDmg} 目标数:{targets.Count}");
        return true;
    }

    public bool TryGetPierceIndicatorPath(int columnIndex, List<Vector3> pathPoints,
        out int effectiveRows)
    {
        effectiveRows = 0;
        if (pathPoints == null)
            return false;
        pathPoints.Clear();

        AttackSkillConfig cfg = GetConfig(AttackType.Pierce);
        if (cfg == null || columnIndex < 0 || columnIndex >= 5)
            return false;

        effectiveRows = GetEffectiveRangeRows(cfg);
        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        float rowSpacing = StageFormation != null ? StageFormation.GetRowSpacing() : 2.5f;
        float formationOffsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
        int visibleRows = StageFormation != null ? StageFormation.GetMaxVisibleRows() : 5;
        int targetRow = Mathf.Clamp(effectiveRows, 1, visibleRows) - 1;
        float enemyRootZ = GetEnemyRootWorldZ();
        float y = playerPos.y + cfg.stabSpawnYOffset;
        float startX = StageFormation != null
            ? StageFormation.GetFormationOffset(columnIndex, 0)
            : (columnIndex - 2) * 2f;
        pathPoints.Add(new Vector3(startX, y, -5.5f));

        for (int row = 0; row <= targetRow; row++)
        {
            float x = StageFormation != null
                ? StageFormation.GetFormationOffset(columnIndex, row)
                : startX;
            float z = enemyRootZ + (visibleRows - 1 - row) * (-rowSpacing) + formationOffsetZ;
            pathPoints.Add(new Vector3(x, y, z));
        }

        if (TryGetPierceVisualPath(columnIndex, out _, out Vector3 visualEndPosition, out _))
            pathPoints.Add(visualEndPosition);

        return true;
    }

    public bool TryGetPierceVisualPath(int columnIndex, out Vector3 startPosition,
        out Vector3 endPosition, out int effectiveRows)
    {
        startPosition = default;
        endPosition = default;
        effectiveRows = 0;

        AttackSkillConfig cfg = GetConfig(AttackType.Pierce);
        if (cfg == null || columnIndex < 0 || columnIndex >= 5)
            return false;

        effectiveRows = GetEffectiveRangeRows(cfg);
        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        float columnX = StageFormation != null
            ? StageFormation.GetFormationOffset(columnIndex, 0)
            : (columnIndex - 2) * 2f;
        float rowSpacing = StageFormation != null ? StageFormation.GetRowSpacing() : 2.5f;
        float formationOffsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
        int visibleRows = StageFormation != null ? StageFormation.GetMaxVisibleRows() : 5;
        int targetRow = Mathf.Clamp(effectiveRows, 1, visibleRows) - 1;
        float targetRowZ = (visibleRows - 1 - targetRow) * (-rowSpacing) + formationOffsetZ;
        float endX = StageFormation != null
            ? StageFormation.GetFormationOffset(columnIndex, targetRow)
            : columnX;

        startPosition = new Vector3(columnX, playerPos.y + cfg.stabSpawnYOffset, -5.5f);
        endPosition = new Vector3(endX, startPosition.y, GetEnemyRootWorldZ() + targetRowZ + 0.8f);
        return true;
    }

    private bool ExecutePierce(int columnIndex)
    {
        var cfg = GetConfig(AttackType.Pierce);
        if (cfg == null || columnIndex < 0 || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetAttackRangeDamagePenalty();
        bool chargeInterrupt = InterruptsCavalryCharge(cfg);
        int effectiveRows = GetEffectiveRangeRows(cfg);
        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, effectiveRows);
        if (!TryGetPierceVisualPath(columnIndex, out Vector3 releasePosition,
                out Vector3 visualEndPosition, out _))
            return false;
        Vector3 wavePos = targets.Count > 0
            ? GetWavePosition(targets, columnIndex)
            : releasePosition;
        Vector3 launchOrigin = releasePosition;
        Vector3 visualDirection = (visualEndPosition - launchOrigin).normalized;
        Quaternion projectileRotation = Quaternion.FromToRotation(Vector3.up, visualDirection);
        Vector3 projectileScale = cfg.attackWavePrefab != null
            ? cfg.attackWavePrefab.transform.localScale
            : Vector3.one;
        Sprite releaseSprite = cfg.attackWavePrefab != null
            ? cfg.attackWavePrefab.GetComponentInChildren<SpriteRenderer>()?.sprite
            : null;
        ReleaseChargeHitShockwave();
        StartCoroutine(ReleaseChargeShockwaves());
        StartCoroutine(ReleaseChargeAttackShockwaves());
        Debug.Log($"[PierceTrace] attack col={columnIndex} targets={targets.Count} wave={wavePos} launchOrigin={launchOrigin} visualEnd={visualEndPosition} endZ={visualEndPosition.z:F2}");
        PierceReleaseVisual.Create(releaseSprite, _chargeStabVisual, releasePosition,
            projectileRotation, projectileScale, launchOrigin, projectileRotation,
            projectileScale, GetAttackDuration(cfg) * pierceTimeScale, releaseVisual =>
        {
            List<Enemy> aliveTargets = FilterAliveTargets(targets);

            if (releaseVisual == null)
            {
                if (aliveTargets.Count > 0)
                    AttackWave.Create(wavePos, cfg.damageType, finalDmg, aliveTargets,
                        onHit: _ => AudioManager.Instance?.PostEvent("Stab_Hit"),
                        prefab: cfg.attackWavePrefab,
                        interruptsCavalryCharge: chargeInterrupt,
                        hitReactionDirection: cfg.hitReactionDirection);
                return;
            }

            GameObject projectileObject = releaseVisual.ProjectileTransform.gameObject;
            Vector3 flightStart = releaseVisual.FlightStartPosition;
            releaseVisual.TransferToProjectile(flightStart, projectileRotation, projectileScale);
            AttackWave.CreatePierceFromVisual(projectileObject, flightStart, finalDmg,
                aliveTargets, visualEndPosition,
                onHit: _ => AudioManager.Instance?.PostEvent("Stab_Hit"), timeScale: pierceTimeScale,
                interruptsCavalryCharge: chargeInterrupt,
                hitReactionDirection: cfg.hitReactionDirection);
        }, weaponPrefab: cfg.attackWavePrefab);

        Debug.Log($"[AttackSystem] 穿刺 列{columnIndex} 伤害:{finalDmg} 目标数:{targets.Count}");
        return true;
    }

    private bool ExecuteSweep()
    {
        var cfg = GetConfig(AttackType.Sweep);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg) * GetAttackRangeDamagePenalty();
        bool chargeInterrupt = InterruptsCavalryCharge(cfg);
        int effectiveRows = GetEffectiveRangeRows(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(effectiveRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            ReleaseChargeHitShockwave();
            StartCoroutine(ReleaseChargeShockwaves());
            StartCoroutine(ReleaseChargeAttackShockwaves());
            AttackReleaseTimeline.Create(AttackType.Sweep, GetAttackDuration(cfg), () =>
            {
                List<Enemy> aliveTargets = FilterAliveTargets(targets);
                if (aliveTargets.Count == 0)
                    return;
                AttackWave.Create(wavePos, cfg.damageType, finalDmg, aliveTargets,
                    prefab: cfg.attackWavePrefab,
                    interruptsCavalryCharge: chargeInterrupt,
                    hitReactionDirection: cfg.hitReactionDirection);
            });
        }

        Debug.Log($"[AttackSystem] 横扫 伤害:{finalDmg} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private static List<Enemy> FilterAliveTargets(List<Enemy> source)
    {
        var aliveTargets = new List<Enemy>(source.Count);
        for (int i = 0; i < source.Count; i++)
        {
            Enemy enemy = source[i];
            if (enemy != null && enemy.state != EnemyState.Dead)
                aliveTargets.Add(enemy);
        }
        return aliveTargets;
    }

    private static float GetEnemyRootWorldZ()
    {
        var columnManager = Instance != null ? Instance.columnManager : null;
        var enemies = columnManager != null ? columnManager.GetAllEnemies() : null;
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.transform.parent != null)
                    return enemy.transform.parent.position.z;
            }
        }

        var pool = EnemyPool.Instance != null ? EnemyPool.Instance : FindObjectOfType<EnemyPool>();
        if (pool == null) return 0f;
        Transform enemyRoot = pool.enemiesRoot != null ? pool.enemiesRoot : pool.poolRoot;
        return enemyRoot != null ? enemyRoot.position.z : 0f;
    }

    private bool ExecuteLaunch()
    {
        var cfg = GetConfig(AttackType.Launch);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg);
        bool chargeInterrupt = InterruptsCavalryCharge(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        Vector3 playerLaunchPos = playerState != null ? playerState.transform.position : transform.position;
        // 连段收尾：先铺一层 slash 的扫掠表现（只做观感，不参与判定）
        if (cfg.launchSweepMode) PlaySweepPresentation(cfg, playerLaunchPos);
        // 释放格挡（局外解锁项）：与扫掠表现同一时刻一次性结算
        ApplyReleaseParryEffect(cfg);
        if (targets.Count > 0)
        {
            ReleaseChargeHitShockwave();
            StartCoroutine(ReleaseChargeShockwaves());

            bool probLaunchActive = playerState != null && playerState.HasBuff(BuffIds.ProbabilityLaunch);
            Vector3 wavePos = GetWavePosition(targets, -1);
            PlayLaunchVisual(cfg, playerLaunchPos, () =>
            {
                bool launchHitSoundPlayed = false;
                AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                    onHit: enemy =>
                    {
                        if (!launchHitSoundPlayed)
                        {
                            launchHitSoundPlayed = true;
                            AudioManager.Instance?.PostEvent("Launch_Hit");
                        }

                        bool canLaunch = enemy.CanBeLaunched(cfg.poiseDamage);
                        if (!canLaunch && probLaunchActive && Random.value < playerState.LaunchProbability)
                        {
                            enemy.Stun(cfg.launchDuration * 0.5f);
                            canLaunch = true;
                        }
                        if (canLaunch)
                        {
                            enemy.OnPlayerLaunchHit();
                            enemy.Launch();
                        }
                    },
                    prefab: null,
                    alphaOverride: 0f,
                    canInterruptCFrame: true,
                    interruptsCavalryCharge: chargeInterrupt,
                    hitReactionDirection: cfg.hitReactionDirection,
                    suppressHitReaction: true);
            });
        }
        else
        {
            PlayLaunchVisual(cfg, playerLaunchPos, null);
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 击飞时间:{cfg.launchDuration}s 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteParry()
    {
        var cfg = GetConfig(AttackType.Parry);
        if (cfg == null) return false;

        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        bool deflectedAny = DeflectProjectilesNear(playerPos, parryProjectileRange) > 0;
        if (deflectedAny)
        {
            AudioManager.Instance?.PostEvent("Player_Parry");
            PlayParryVisual(cfg, playerPos);
            return true;
        }

        // 无飞行物在范围内 → 对敌人执行招架伤害
        if (columnManager == null) return false;
        float finalDmg = GetFinalDamage(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count == 0) return false;

        foreach (var enemy in targets)
        {
            enemy.TakePoiseDamage(cfg.poiseDamage);
            // 冲锋暂不可被 Parry 打断：格挡只造成架势/伤害。
            enemy.TakeDamage(finalDmg, cfg.damageType, canInterruptCFrame: true, isParryInterrupt: true,
                feedbackStrength: HitFeedbackStrength.Heavy);
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 目标数:{targets.Count}");
        AudioManager.Instance?.PostEvent("Player_Parry");
        PlayParryVisual(cfg, playerPos);
        return true;
    }

    /// <summary>
    /// 反弹玩家周围的敌人飞行物（QTE 飞行物除外）。返回反弹数量。
    /// 招架与「释放格挡」共用同一条通道，避免两处各写一份扫描逻辑。
    /// </summary>
    private int DeflectProjectilesNear(Vector3 playerPos, float range)
    {
        var projectiles = SceneRegistry<EnemyProjectile>.Snapshot();
        int deflected = 0;
        foreach (var p in projectiles)
        {
            if (p == null || p.isQTEProjectile) continue;
            float dist = Vector3.Distance(p.GetWorldPosition(), playerPos);
            if (dist > range) continue;

            p.Deflect();
            deflected++;
            Debug.Log($"[AttackSystem] 反弹飞行物: dist={dist:F1} range={range:F1}");
        }
        return deflected;
    }

    /// <summary>
    /// 释放格挡（Parry 式附加效果：仅机制，不播 Parry 视觉与音效）。
    /// 释放瞬间一次性结算：反弹范围内飞行物 + 打断正在攻击的敌人。
    /// 效果由招式资产声明（releaseParryEnabled / 半径 / 排数），是否已解锁由 finisherParryUnlocked 决定。
    /// 注意：本效果与「是否命中敌人」无关，空挥释放也会生效（飞行物在途时仍能被挡下）。
    /// </summary>
    private void ApplyReleaseParryEffect(AttackSkillConfig cfg)
    {
        if (cfg == null || !cfg.releaseParryEnabled) return;
        // 局外解锁门：未解锁时不产生任何效果（当前由编辑器开关代替解锁）
        if (!finisherParryUnlocked) return;

        Vector3 playerPos = playerState != null ? playerState.transform.position : transform.position;
        int deflected = DeflectProjectilesNear(playerPos, cfg.releaseParryProjectileRange);
        int interrupted = InterruptAttackingEnemies(cfg.releaseParryRangeRows);

        Debug.Log($"[AttackSystem] 释放格挡（{cfg.name}）：反弹飞行物 {deflected} 个，打断攻击敌人 {interrupted} 个");
    }

    /// <summary>打断正在攻击的敌人（Parry 门控，不对其造成任何伤害）。返回打断数量。</summary>
    private int InterruptAttackingEnemies(int rangeRows)
    {
        if (columnManager == null) return 0;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(Mathf.Max(1, rangeRows));
        int interrupted = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            Enemy enemy = targets[i];
            if (enemy == null) continue;
            if (enemy.TryInterruptAttackByParry()) interrupted++;
        }
        return interrupted;
    }

    /// <summary>
    /// 挑飞视觉特效：使用专用 stab_rotate 精灵创建独立视觉，
    /// 以 (35,90,zStart) 朝向做纯 Z 轴旋转至 (35,90,zEnd)，
    /// 表现枪头从低往高上挑的攻击动作。
    /// </summary>
    private static readonly List<Enemy> NoSweepTargets = new List<Enemy>();

    /// <summary>
    /// 连段收尾的扫击表现层：只借 slash 的扫掠观感（扇形/拖尾/方向/斜度），
    /// 目标为空 → 不命中、不伤害、不掉血；伤害与击飞仍由挑飞的 AttackWave 结算。
    /// </summary>
    private void PlaySweepPresentation(AttackSkillConfig cfg, Vector3 playerPos)
    {
        Vector3 wavePos = new Vector3(0f, playerPos.y + cfg.slashSpawnYOffset,
            playerPos.z + cfg.slashSpawnZOffset);
        bool leftToRight = cfg.slashSweepDirection == SlashSweepDirection.LeftToRight;

        SweepEffect.Create(wavePos, cfg.damageType, 0f, NoSweepTargets, leftToRight,
            cfg.slashSweepHalfWidth, cfg.slashSweepAngle, cfg.slashSweepDuration,
            prefab: cfg.attackWavePrefab,
            // 扫掠总时长用 slashSweepDuration（比节点动作锁短），让它一下子掍完，而不是拖满整个收尾
            targetDuration: cfg.slashSweepDuration,
            // 终结技只保留一层很淡的拖尾（slashSweepAlpha），不再读成一次 slash
            alphaOverride: cfg.slashSweepAlpha,
            rotateSprite1: _stabRotate1Sprite, rotateSprite2: _stabRotate2Sprite,
            movementTilt: cfg.slashMovementTiltDegrees,
            visualPathTilt: cfg.slashOverrideVisualTilt ? cfg.slashVisualTiltDegrees : 0f,
            useEnhancedSlashMotion: true, suppressHitReaction: true);
    }

    private void PlayLaunchVisual(AttackSkillConfig cfg, Vector3 playerPos, System.Action onImpact)
    {
        // 连段终结技：从蓄势位的枪体原地起手（中心/旋转/缩放全部沿用），并把那把枪交掉（避免两把枪）
        bool hasHoldPose = false;
        Vector3 holdCenter = default;
        Quaternion holdRotation = default;
        Vector3 holdScale = default;
        GameObject transferredWeapon = null;
        StabSweepEffect heldStab = _lastStabEffect;
        if (heldStab != null && heldStab.IsChargeHeld
            && heldStab.TryGetChargeHoldPose(out holdCenter, out holdRotation, out holdScale))
        {
            hasHoldPose = true;
            bool meshWeapon = cfg.attackWavePrefab != null
                && cfg.attackWavePrefab.GetComponent<MeshWeaponVisual>() != null;
            if (meshWeapon) transferredWeapon = heldStab.TakeHeldMeshVisual();
            if (transferredWeapon == null) heldStab.ReleaseAfterChargeHold();
            _lastStabEffect = null;
        }

        LaunchVisualEffect.Create(_launchSprite1, _launchSprite2, _launchSprite3, cfg, playerPos,
            _chargeStabVisual, LaunchVisualEffect.ObservationScale, onImpact,
            hasHoldPose, holdCenter, holdRotation, holdScale, cfg.launchSkipWindup && hasHoldPose, transferredWeapon);
    }

    /// <summary>
    /// 招架视觉由独立效果组件负责；战斗判定与敌人状态流程保持不变。
    /// </summary>
    private void PlayParryVisual(AttackSkillConfig cfg, Vector3 playerPos)
    {
        ParryVisualEffect.Create(cfg.attackWavePrefab, playerPos, cfg);
    }

    #endregion
}