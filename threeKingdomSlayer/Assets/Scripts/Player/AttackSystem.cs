using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击系统
/// 实现6种攻击类型：戳击、斩击、穿刺、横扫、挑飞、招架
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
    /// </summary>
    public bool TryExecuteAttack(AttackType attackType, int targetColumn = -1)
    {
        if (playerState == null) return false;
        if (playerState.stageState != StageState.InProgress) return false;

        // 检查冷却
        if (!playerState.IsAttackReady(attackType))
        {
            Debug.Log($"[AttackSystem] {attackType} 冷却中");
            return false;
        }

        // 执行攻击
        switch (attackType)
        {
            case AttackType.Stab:   ExecuteStab(targetColumn); break;
            case AttackType.Slash:  ExecuteSlash(); break;
            case AttackType.Pierce: ExecutePierce(targetColumn); break;
            case AttackType.Sweep:  ExecuteSweep(); break;
            case AttackType.Launch: ExecuteLaunch(); break;
            case AttackType.Parry:  ExecuteParry(); break;
        }

        // 触发冷却
        playerState.StartCooldown(attackType);
        return true;
    }

    #region 攻击类型实现

    /// <summary>
    /// 戳击：点击任意列 → 对该列前N排造成伤害
    /// </summary>
    private void ExecuteStab(int columnIndex)
    {
        if (columnIndex < 0 || columnManager == null || playerState?.heroConfig == null) return;

        float damage = playerState.heroConfig.stabDamage;
        int rangeRows = playerState.heroConfig.stabRangeRows;

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, rangeRows);
        ApplyDamageToTargets(targets, damage, DamageType.Stab);

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
    }

    /// <summary>
    /// 斩击：划动屏幕 → 对所有列前N排造成伤害
    /// </summary>
    private void ExecuteSlash()
    {
        if (columnManager == null || playerState?.heroConfig == null) return;

        float damage = playerState.heroConfig.slashDamage;
        int rangeRows = playerState.heroConfig.slashRangeRows;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(rangeRows);
        ApplyDamageToTargets(targets, damage, DamageType.Slash);

        Debug.Log($"[AttackSystem] 斩击 伤害:{damage} 目标数:{targets.Count}");
    }

    /// <summary>
    /// 穿刺：长按某列后松开 → 对该列造成高额伤害
    /// </summary>
    private void ExecutePierce(int columnIndex)
    {
        if (columnIndex < 0 || columnManager == null || playerState?.heroConfig == null) return;

        float damage = playerState.heroConfig.pierceDamage;
        int rangeRows = playerState.heroConfig.pierceRangeRows;

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, rangeRows);
        ApplyDamageToTargets(targets, damage, DamageType.Pierce);

        Debug.Log($"[AttackSystem] 穿刺 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
    }

    /// <summary>
    /// 横扫：从屏幕一侧长按后划向另一侧松开 → 对所有列造成伤害
    /// </summary>
    private void ExecuteSweep()
    {
        if (columnManager == null || playerState?.heroConfig == null) return;

        float damage = playerState.heroConfig.sweepDamage;
        int rangeRows = playerState.heroConfig.sweepRangeRows;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(rangeRows);
        ApplyDamageToTargets(targets, damage, DamageType.Sweep);

        Debug.Log($"[AttackSystem] 横扫 伤害:{damage} 目标数:{targets.Count}");
    }

    /// <summary>
    /// 挑飞：在屏幕中间区域向上滑动 → 对所有列造成挑飞伤害+架势伤害
    /// </summary>
    private void ExecuteLaunch()
    {
        if (columnManager == null || playerState?.heroConfig == null) return;

        float damage = playerState.heroConfig.launchDamage;
        float poiseDamage = playerState.heroConfig.launchPoiseDamage;
        int rangeRows = playerState.heroConfig.launchRangeRows;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(rangeRows);
        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.state == EnemyState.Dead) continue;
            enemy.TakeDamage(damage, DamageType.Launch);
            enemy.TakePoiseDamage(poiseDamage);
            enemy.Launch(playerState.heroConfig.launchCooldown); // 使用挑飞持续时间
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{damage} 架势伤害:{poiseDamage} 目标数:{targets.Count}");
    }

    /// <summary>
    /// 招架：在红光提示时反方向划动 → 招架BOSS攻击
    /// </summary>
    private void ExecuteParry()
    {
        if (playerState?.heroConfig == null) return;

        float damage = playerState.heroConfig.parryDamage;
        float poiseDamage = playerState.heroConfig.parryPoiseDamage;

        // 招架只对当前正在攻击的BOSS敌人有效
        // 获取所有列最前排的敌人，对其造成招架伤害
        List<Enemy> frontEnemies = new List<Enemy>();
        for (int i = 0; i < 5; i++)
        {
            Enemy front = columnManager?.GetFrontEnemy(i);
            if (front != null && front.state != EnemyState.Dead)
            {
                frontEnemies.Add(front);
            }
        }

        foreach (var enemy in frontEnemies)
        {
            enemy.TakeDamage(damage, DamageType.Poise);
            enemy.TakePoiseDamage(poiseDamage);
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{damage} 架势伤害:{poiseDamage} 目标数:{frontEnemies.Count}");
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 对目标列表应用伤害
    /// </summary>
    private void ApplyDamageToTargets(List<Enemy> targets, float damage, DamageType damageType)
    {
        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.state == EnemyState.Dead) continue;
            enemy.TakeDamage(damage, damageType);
        }
    }

    /// <summary>
    /// 获取攻击的伤害值（用于UI显示）
    /// </summary>
    public float GetAttackDamage(AttackType attackType)
    {
        if (playerState?.heroConfig == null) return 0f;
        return attackType switch
        {
            AttackType.Stab => playerState.heroConfig.stabDamage,
            AttackType.Slash => playerState.heroConfig.slashDamage,
            AttackType.Pierce => playerState.heroConfig.pierceDamage,
            AttackType.Sweep => playerState.heroConfig.sweepDamage,
            AttackType.Launch => playerState.heroConfig.launchDamage,
            AttackType.Parry => playerState.heroConfig.parryDamage,
            _ => 0f
        };
    }

    #endregion
}
