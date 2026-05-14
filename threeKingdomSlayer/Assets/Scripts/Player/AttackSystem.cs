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
    public bool TryExecuteAttack(AttackType attackType, int targetColumn = -1)
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
            case AttackType.Slash:  hitAny = ExecuteSlash(); break;
            case AttackType.Pierce: hitAny = ExecutePierce(targetColumn); break;
            case AttackType.Sweep:  hitAny = ExecuteSweep(); break;
            case AttackType.Launch: hitAny = ExecuteLaunch(); break;
            case AttackType.Parry:  hitAny = ExecuteParry(); break;
        }

        if (hitAny)
        {
            playerState.StartCooldown(attackType);
            UltimateSystem.Instance?.AddEnergyForAttack(attackType);
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

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            AttackWave.Create(wavePos, cfg.damageType, cfg.damage, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{cfg.damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteSlash()
    {
        var cfg = GetConfig(AttackType.Slash);
        if (cfg == null || columnManager == null) return false;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            AttackWave.Create(wavePos, cfg.damageType, cfg.damage, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 斩击 伤害:{cfg.damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecutePierce(int columnIndex)
    {
        var cfg = GetConfig(AttackType.Pierce);
        if (cfg == null || columnIndex < 0 || columnManager == null) return false;

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            AttackWave.Create(wavePos, cfg.damageType, cfg.damage, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 穿刺 列{columnIndex} 伤害:{cfg.damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteSweep()
    {
        var cfg = GetConfig(AttackType.Sweep);
        if (cfg == null || columnManager == null) return false;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            AttackWave.Create(wavePos, cfg.damageType, cfg.damage, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 横扫 伤害:{cfg.damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteLaunch()
    {
        var cfg = GetConfig(AttackType.Launch);
        if (cfg == null || columnManager == null) return false;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            AttackWave.Create(wavePos, cfg.damageType, cfg.damage, targets,
                onHit: (enemy) =>
                {
                    bool broken = enemy.TakePoiseDamage(cfg.poiseDamage);
                    if (broken)
                        enemy.Launch(cfg.launchDuration);
                },
                prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{cfg.damage} 架势伤害:{cfg.poiseDamage} 击飞时间:{cfg.launchDuration}s 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    private bool ExecuteParry()
    {
        var cfg = GetConfig(AttackType.Parry);
        if (cfg == null || columnManager == null) return false;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);
        if (targets.Count == 0) return false;

        foreach (var enemy in targets)
        {
            if (enemy.state == EnemyState.Attacking && enemy.isAttackAnimating && !enemy.isAttackDrawPhase
                && cfg.poiseDamage >= enemy.maxPoise)
            {
                enemy.CancelAttack();
            }
            else
            {
                enemy.TakeDamage(cfg.damage, cfg.damageType);
                enemy.TakePoiseDamage(cfg.poiseDamage);
                enemy.CheckParryStunThresholds();
            }
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{cfg.damage} 架势伤害:{cfg.poiseDamage} 目标数:{targets.Count}");
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

    public float GetAttackDamage(AttackType attackType)
    {
        var cfg = GetConfig(attackType);
        return cfg != null ? cfg.damage : 0f;
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

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, cfg.rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            AttackWave.Create(wavePos, cfg.damageType, damage, targets, prefab: cfg.attackWavePrefab);
        }

        Debug.Log($"[AttackSystem] 强制Stab 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    #endregion
}
