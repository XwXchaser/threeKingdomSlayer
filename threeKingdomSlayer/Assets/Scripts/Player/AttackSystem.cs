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

    [Header("攻击波预制体（可选，null 则用占位 Quad）")]
    public GameObject stabWavePrefab;
    public GameObject slashWavePrefab;
    public GameObject pierceWavePrefab;
    public GameObject sweepWavePrefab;
    public GameObject launchWavePrefab;

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
    /// 如果攻击未命中任何敌人（例如点击了没有敌人的列），不消耗冷却
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

        // 执行攻击，获取是否命中至少一个敌人
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

        // BUG FIX: 只有实际命中至少一个敌人时，才触发冷却
        // 如果攻击未命中任何敌人（例如点击了没有敌人的列），不消耗冷却
        if (hitAny)
        {
            playerState.StartCooldown(attackType);
            return true;
        }

        Debug.Log($"[AttackSystem] {attackType} 未命中任何敌人，不消耗冷却");
        return false;
    }

    #region 攻击类型实现

    /// <summary>
    /// 戳击：点击任意列 → 对该列前N排造成伤害
    /// </summary>
    /// <returns>是否命中至少一个敌人</returns>
    private bool ExecuteStab(int columnIndex)
    {
        if (columnIndex < 0 || columnManager == null || playerState?.heroConfig == null) return false;

        float damage = playerState.heroConfig.stabDamage;
        int rangeRows = playerState.heroConfig.stabRangeRows;

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            AttackWave.Create(wavePos, DamageType.Stab, damage, targets, prefab: stabWavePrefab);
        }

        Debug.Log($"[AttackSystem] 戳击 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>
    /// 斩击：划动屏幕 → 对所有列前N排造成伤害
    /// </summary>
    /// <returns>是否命中至少一个敌人</returns>
    private bool ExecuteSlash()
    {
        if (columnManager == null || playerState?.heroConfig == null) return false;

        float damage = playerState.heroConfig.slashDamage;
        int rangeRows = playerState.heroConfig.slashRangeRows;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            AttackWave.Create(wavePos, DamageType.Slash, damage, targets, prefab: slashWavePrefab);
        }

        Debug.Log($"[AttackSystem] 斩击 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>
    /// 穿刺：长按某列后松开 → 对该列造成高额伤害
    /// </summary>
    /// <returns>是否命中至少一个敌人</returns>
    private bool ExecutePierce(int columnIndex)
    {
        if (columnIndex < 0 || columnManager == null || playerState?.heroConfig == null) return false;

        float damage = playerState.heroConfig.pierceDamage;
        int rangeRows = playerState.heroConfig.pierceRangeRows;

        List<Enemy> targets = columnManager.GetEnemiesInRange(columnIndex, rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, columnIndex);
            AttackWave.Create(wavePos, DamageType.Pierce, damage, targets, prefab: pierceWavePrefab);
        }

        Debug.Log($"[AttackSystem] 穿刺 列{columnIndex} 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>
    /// 横扫：从屏幕一侧长按后划向另一侧松开 → 对所有列造成伤害
    /// </summary>
    /// <returns>是否命中至少一个敌人</returns>
    private bool ExecuteSweep()
    {
        if (columnManager == null || playerState?.heroConfig == null) return false;

        float damage = playerState.heroConfig.sweepDamage;
        int rangeRows = playerState.heroConfig.sweepRangeRows;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            AttackWave.Create(wavePos, DamageType.Sweep, damage, targets, prefab: sweepWavePrefab);
        }

        Debug.Log($"[AttackSystem] 横扫 伤害:{damage} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>
    /// 挑飞：在屏幕中间区域向上滑动 → 对所有列造成挑飞伤害+架势伤害
    /// </summary>
    /// <returns>是否命中至少一个敌人</returns>
    private bool ExecuteLaunch()
    {
        if (columnManager == null || playerState?.heroConfig == null) return false;

        float damage = playerState.heroConfig.launchDamage;
        float poiseDamage = playerState.heroConfig.launchPoiseDamage;
        int rangeRows = playerState.heroConfig.launchRangeRows;
        float duration = playerState.heroConfig.launchDuration;

        List<Enemy> targets = columnManager.GetAllEnemiesInRange(rangeRows);
        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, -1);
            AttackWave.Create(wavePos, DamageType.Launch, damage, targets,
                onHit: (enemy) =>
                {
                    enemy.TakePoiseDamage(poiseDamage);
                    enemy.Launch(duration);
                },
                prefab: launchWavePrefab);
        }

        Debug.Log($"[AttackSystem] 挑飞 伤害:{damage} 架势伤害:{poiseDamage} 击飞时间:{duration}s 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>
    /// 招架：在红光提示时反方向划动 → 招架BOSS攻击
    /// </summary>
    /// <returns>是否命中至少一个敌人</returns>
    private bool ExecuteParry()
    {
        if (playerState?.heroConfig == null) return false;

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
            // BUG FIX: 招架造成的是血量伤害，不应使用 DamageType.Poise（架势倍率）。
            // DamageType.Poise 会应用 EnemyConfig.poiseDamageMultiplier，如果该值<1
            // 会导致招架伤害低于预期。
            // 使用 DamageType.Stab 应用普通伤害倍率。
            enemy.TakeDamage(damage, DamageType.Stab);
            enemy.TakePoiseDamage(poiseDamage);
        }

        Debug.Log($"[AttackSystem] 招架 伤害:{damage} (type=Stab) 架势伤害:{poiseDamage} 目标数:{frontEnemies.Count}");
        return frontEnemies.Count > 0;
    }

    #endregion

    #region 工具方法

    /// <summary>
    /// 计算攻击波的生成位置
    /// 单列攻击：置于该列的前排敌人前方；多列攻击：置于屏幕中央
    /// </summary>
    private Vector3 GetWavePosition(List<Enemy> targets, int targetColumn)
    {
        if (targets.Count == 0)
            return new Vector3(0, 1.5f, -10f);

        // 取第一个（最前排）目标的位置
        Vector3 pos = targets[0].transform.position;

        // X: 单列攻击用该列的 X，多列攻击居中
        if (targetColumn < 0)
            pos.x = 0f;

        // Y: 敌人胸口高度（pivot 通常在地面，+1.5f 到躯干位置）
        pos.y = targets[0].transform.position.y + 1.5f;

        // Z: 稍微靠前（更接近玩家），让波看起来是从玩家方向飞来的
        pos.z += 0.5f;

        return pos;
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
