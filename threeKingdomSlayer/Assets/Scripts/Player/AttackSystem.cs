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
        }

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{finalDmg} 目标数:{targets.Count}");
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
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab);
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
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab);
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
                prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 击飞时间:{cfg.launchDuration}s 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteParry()
    {
        var cfg = GetConfig(AttackType.Parry);
        if (cfg == null || columnManager == null) return false;

        float finalDmg = GetFinalDamage(cfg);
        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count == 0) return false;

        foreach (var enemy in targets)
        {
            bool canInterrupt = enemy.state == EnemyState.Attacking
                && enemy.isAttackAnimating
                && !enemy.isAttackDrawPhase;

            if (enemy.isBoss)
            {
                // Boss: parry 无条件打断攻击，打断成功才造成 Poise 伤害
                if (canInterrupt)
                {
                    enemy.CancelAttack();
                    enemy.TakePoiseDamage(cfg.poiseDamage);
                }
                else
                {
                    enemy.TakeDamage(finalDmg, cfg.damageType);
                }
            }
            else if (canInterrupt && cfg.poiseDamage >= enemy.maxPoise)
            {
                enemy.CancelAttack();
            }
            else
            {
                // 非Boss敌人不施加架势伤害（没有眩晕设计），仅造成伤害
                enemy.TakeDamage(finalDmg, cfg.damageType);
                enemy.CheckParryStunThresholds();
            }
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{finalDmg} 架势伤害:{cfg.poiseDamage} 目标数:{targets.Count}");
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
                        damageNumberColor: phantomColor);
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
                        damageNumberColor: phantomColor);
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
                        damageNumberColor: phantomColor);
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
                        damageNumberColor: phantomColor);
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
                        damageNumberColor: phantomColor);
                }
                return targets.Count > 0;
            }
            default:
                return false;
        }
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
