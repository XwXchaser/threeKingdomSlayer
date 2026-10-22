using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

/// <summary>
    /// AttackSystem 的**工具方法**：坐标换算、目标选取、冷却与手势辅助逻辑。
    ///
    /// 按机制从 <c>AttackSystem.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 AttackSystem.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class AttackSystem : MonoBehaviour
{
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

    private bool _attackIsCharged;
    private int _currentChargeLevel;
    private AttackSkillConfig _currentConfigOverride;

    /// <summary>
    /// 本次攻击是否可打断骑兵冲锋（仅用于敌人侧判定，不参与玩家输入窗口/手势判定）。
    /// 只有两个来源，都不靠代码里的攻击类型白名单：
    ///   1) 攻击资产勾选了 AttackSkillConfig.interruptsCavalryCharge（把某种攻击视为“蓄力/重攻击”时在数据里声明）；
    ///   2) 本次输入为蓄力（InputManager.isCharged，经 TryExecuteAttack(..., chargedAttack) 传入），
    ///      用于斩击这种普通/蓄力共用的动作。
    /// 将来新增“重攻击”类攻击：勾选该资产，或在新的输入分支传入 chargedAttack，不需要改骑兵代码。
    /// </summary>
    private bool InterruptsCavalryCharge(AttackSkillConfig cfg)
    {
        if (cfg != null && cfg.interruptsCavalryCharge) return true;
        return _attackIsCharged;
    }

    /// <summary>获取最终伤害（基础伤害 × 升级倍率）</summary>
    /// <summary>按蓄力等级取伤害倍率；未配置等级表时不影响任何数值</summary>
    private float GetChargeLevelMultiplier(AttackSkillConfig cfg)
    {
        if (_currentChargeLevel < 1 || cfg == null) return 1f;
        var table = cfg.chargeLevelDamageMultipliers;
        if (table == null || table.Count == 0) return 1f;
        return table[Mathf.Clamp(_currentChargeLevel - 1, 0, table.Count - 1)];
    }

    private float GetFinalDamage(AttackSkillConfig cfg)
    {
        if (cfg == null) return 0f;
        float mult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f;
        return cfg.damage * mult;
    }

    /// <summary>释放蓄力受击增伤冲击波，每次蓄力攻击释放后清空累计。</summary>
    private void ReleaseChargeHitShockwave()
    {
        if (playerState == null || !playerState.IsCharging) return;
        var uem = UpgradeEffectManager.Instance;
        if (uem == null || columnManager == null) return;
        if (!uem.ConsumeChargeHitShockwave(out var cfg, out float bonusPercent)) return;

        var targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count == 0) return;
        var slashCfg = GetConfig(AttackType.Slash);
        GameObject prefab = slashCfg != null ? slashCfg.attackWavePrefab : null;
        Vector3 wavePos = GetWavePosition(targets, -1);
        int damage = Mathf.RoundToInt(cfg.baseDamage * (1f + bonusPercent));

        for (int i = 0; i < cfg.shockwaveCount; i++)
            AttackWave.Create(wavePos, DamageType.Slash, damage, targets, prefab: prefab,
                interruptsCavalryCharge: true);

        Debug.Log($"[AttackSystem] 受击冲击波释放: {cfg.shockwaveCount}波 rows={cfg.rangeRows} damage={damage} bonus={bonusPercent:P0}");
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
                        prefab: wavePrefab, interruptsCavalryCharge: true);
                    if (r.config.waveDelay > 0f)
                        yield return new WaitForSeconds(r.config.waveDelay);
                }
            }
            Debug.Log($"[AttackSystem] 蓄力冲击波释放: {r.upgradeId} {r.layers}层×{wavesPerTick}波 rows={rows} baseDmg={r.config.baseDamage} delay={r.config.waveDelay}");
        }
    }

    /// <summary>释放主动冲击波：玩家点击叠加层数，下一次蓄力攻击时每层释放一道</summary>
    private System.Collections.IEnumerator ReleaseChargeAttackShockwaves()
    {
        if (playerState == null || !playerState.IsCharging) yield break;
        if (ActiveSkillRunner.Instance == null || columnManager == null) yield break;

        var results = ActiveSkillRunner.Instance.ConsumeAllChargeAttackShockwaves();
        if (results.Count == 0) yield break;

        var sweepCfg = GetConfig(AttackType.Sweep);
        GameObject wavePrefab = sweepCfg?.attackWavePrefab;

        foreach (var r in results)
        {
            var targets = columnManager.GetAllEnemiesInRange(r.config.rangeRows);
            Vector3 wavePos = GetWavePosition(targets, -1);

            for (int layer = 0; layer < r.layers; layer++)
            {
                AttackWave.Create(wavePos, DamageType.Sweep, r.config.damage, targets,
                    prefab: wavePrefab, interruptsCavalryCharge: true);
                if (layer + 1 < r.layers)
                    yield return null;
            }

            Debug.Log($"[AttackSystem] 主动冲击波释放: {r.upgradeId} {r.layers}层 rows={r.config.rangeRows} damage={r.config.damage}");
        }
    }

    /// <summary>获取有效攻击排数（含拔苗助长等加成）</summary>
    private int GetEffectiveRangeRows(AttackSkillConfig cfg)
    {
        int bonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetAttackRangeBonus() : 0;
        return cfg.rangeRows + bonus;
    }

    /// <summary>
    /// 本次命中实际生效的击退排数（与攻击类型无关，任何招式都能用）。
    /// 规则（用户已定）：招式自身 pushBackRows 大于 0 才生效，并叠加局内升级「push_wave」的加成；
    /// 招式没配击退时恒为 0——升级不会凭空给招式加击退（旧行为是升级直接作用于戳击）。
    /// </summary>
    private int GetEffectivePushBack(AttackSkillConfig cfg)
    {
        int movePush = cfg != null ? Mathf.Max(0, cfg.pushBackRows) : 0;
        if (movePush <= 0) return 0;

        int upgradeBonus = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetPushWaveDistance() : 0;
        return movePush + Mathf.Max(0, upgradeBonus);
    }

    /// <summary>
    /// 招式自带的命中击退（公共入口，与攻击类型无关）：按招式配置推命中目标并记录被推者。
    /// 调用方需在所有命中结束后调用 <c>columnManager.PostDisplacementFillUp(pushedTargets)</c> 收尾。
    /// 位移全程走既有 ApplyPushWave 通道：不重叠、只影响被命中者、Boss 与骑乘骑兵免疫、最后精确回原槽。
    /// 当前只有 Stab 路径接了此处（ExecuteStab）；其它招式（穿刺/横扫/挑飞的 AttackWave 路径）需要时再接。
    /// </summary>
    public bool ApplyMovePushBack(AttackSkillConfig cfg, Enemy enemy, List<Enemy> pushedTargets)
    {
        if (enemy == null || columnManager == null) return false;

        // 破冰推动已接管本次位移：同一目标不再叠加招式自带的普通击退
        if (IceBreakSession.IsBrokenThisAttack(enemy)) return false;

        int pushDist = GetEffectivePushBack(cfg);
        if (pushDist <= 0) return false;

        _movePushBuffer.Clear();
        _movePushBuffer.Add(enemy);
        return columnManager.ApplyPushWave(_movePushBuffer, pushDist, canInterruptCFrame: false, pushedEnemies: pushedTargets);
    }

    /// <summary>获取攻击范围惩罚倍率（拔苗助长副作用）</summary>
    private float GetAttackRangeDamagePenalty()
    {
        if (UpgradeEffectManager.Instance != null)
            return 1f - UpgradeEffectManager.Instance.GetAttackDamagePenalty();
        return 1f;
    }

    /// <summary>Slash horizontal directional push. It changes only hit enemies' columns and never enters backward-push return/fill.</summary>
    private void ApplySlashDirectionalPush(List<Enemy> targets, bool leftToRight)
    {
        if (UpgradeEffectManager.Instance == null || columnManager == null) return;
        int step = UpgradeEffectManager.Instance.GetDirectionalPushStep();
        if (step <= 0) return;

        Debug.Log($"[Displacement] Slash DirectionalPush step={step} dir={(leftToRight ? "L→R" : "R→L")} targets={targets.Count}");
        Debug.Log(columnManager.DumpColumns());

        columnManager.ApplyDirectionalPush(targets, step, leftToRight, canInterruptCFrame: false);

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
                finalDmg *= GetAttackRangeDamagePenalty();
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
                int effectiveRows = GetEffectiveRangeRows(cfg);
                var targets = columnManager.GetAllEnemiesInRange(effectiveRows);
                finalDmg *= GetAttackRangeDamagePenalty();
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
                finalDmg *= GetAttackRangeDamagePenalty();
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
                int effectiveRows = GetEffectiveRangeRows(cfg);
                var targets = columnManager.GetAllEnemiesInRange(effectiveRows);
                finalDmg *= GetAttackRangeDamagePenalty();
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
                    bool probLaunchActive = playerState != null && playerState.HasBuff(BuffIds.ProbabilityLaunch);
                    AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets,
                        onHit: (enemy) =>
                        {
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

        float finalDmg = GetFinalDamage(cfg) * GetAttackRangeDamagePenalty();
        Vector3 wavePos = GetWavePosition(targets, targetColumn);
        Color waveColor = new Color(0.2f, 0.7f, 1f); // 青蓝色回旋镖
        float alpha = 0.85f;

        GameObject wavePrefab = _returnWavePrefab != null ? _returnWavePrefab : cfg.attackWavePrefab;
        wavePos.y = targets[0].transform.position.y + cfg.stabSpawnYOffset;

        AttackWave.CreateReturnWave(wavePos, cfg.damageType, finalDmg, targets, damageRatio,
            prefab: wavePrefab, zOffset: cfg.stabSpawnZOffset,
            alphaOverride: alpha, damageNumberColor: waveColor, colorOverride: waveColor,
            hitReactionDirection: cfg.hitReactionDirection);

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

        float baseDamage = GetFinalDamage(cfg) * GetAttackRangeDamagePenalty();
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
                next.TakeDamage(bounceDamage, cfg.damageType,
                    feedbackSource: HitFeedbackSource.Passive, feedbackStrength: HitFeedbackStrength.Light,
                    hitReactionDirection: cfg.hitReactionDirection);

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
            AttackWave.Create(wavePos, cfg.damageType, damage * GetAttackRangeDamagePenalty(), targets,
                prefab: cfg.attackWavePrefab, zOffset: cfg.stabSpawnZOffset,
                hitReactionDirection: cfg.hitReactionDirection);
        }

        Debug.Log($"[AttackSystem] 强制Stab 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    #endregion
}