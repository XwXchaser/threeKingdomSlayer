using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 补齐规则枚举
/// </summary>
public enum FillUpRule
{
    [Tooltip("逐列补齐：每列独立，前排敌人死亡后同列后方敌人立即前移")]
    PerColumn,
    [Tooltip("逐排补齐：整排敌人全部阵亡后，下一排敌人才会集体前移")]
    PerRow
}

/// <summary>
/// 一排敌人的配置
/// 每排的敌人数量由 enemyIds 长度决定，每个敌人ID对应一个站位
/// enemyIds[0]=列0(最左), enemyIds[1]=列1, ... enemyIds[4]=列4(最右)
/// 敌人实际占用的列数由其 EnemyConfig.occupySlots 决定
/// </summary>
[Serializable]
public class RowConfig
{
    public const int RhythmGateMarker = 999;

    [Tooltip("该排的敌人ID列表，每个ID对应一个站位。0=空槽；任意位置填999则整排为节奏门，不生成敌人")]
    public int[] enemyIds = new int[5]; // 长度决定该排有多少个敌人站位

    public bool IsRhythmGate
    {
        get
        {
            if (enemyIds == null)
                return false;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                if (enemyIds[i] == RhythmGateMarker)
                    return true;
            }

            return false;
        }
    }

    public bool HasConfiguredEnemies
    {
        get
        {
            if (IsRhythmGate || enemyIds == null)
                return false;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                if (enemyIds[i] > 0)
                    return true;
            }

            return false;
        }
    }

    public bool HasMixedRhythmGateContent
    {
        get
        {
            if (!IsRhythmGate || enemyIds == null)
                return false;

            for (int i = 0; i < enemyIds.Length; i++)
            {
                int enemyId = enemyIds[i];
                if (enemyId != 0 && enemyId != RhythmGateMarker)
                    return true;
            }

            return false;
        }
    }
}

/// <summary>
/// 波次配置
/// </summary>
[Serializable]
public class WaveConfig
{
    public int waveId;
    public bool isBossWave;
    [Tooltip("此波包含的所有排，按顺序从远到近生成")]
    public List<RowConfig> rows = new List<RowConfig>();

    [Header("波次敌人强化")]
    [Tooltip("血量倍率，1.0 = 100% = 不变")]
    public float healthMultiplier = 1f;

    [Tooltip("Boss 专属血量倍率（仅对 isBoss=true 的敌人生效），0 表示使用通用 healthMultiplier")]
    public float bossHealthMultiplier = 0f;

    [Tooltip("攻击速度倍率，>1 敌人攻击更频繁")]
    public float attackSpeedMultiplier = 1f;

    [Tooltip("伤害倍率，>1 玩家承伤更高")]
    public float damageMultiplier = 1f;

    [Tooltip("敌人颜色叠加，白色(1,1,1) = 不变")]
    public Color waveTintColor = Color.white;

    [Header("补齐延迟（本波次独立配置）")]
    [Tooltip("启用动态补齐：存活敌人越少，补齐延迟越短（用于后期割草加速）")]
    public bool enableDynamicRush = false;
    [Tooltip("补齐移动基础延迟（秒），敌人数量 >=10 时使用此值")]
    public float rushMoveDelay = 0.2f;
    [Tooltip("补齐移动最低延迟（秒），敌人数量 →0 时趋近此值")]
    public float rushMoveDelayMin = 0.02f;
}

/// <summary>
/// 关卡配置 - ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewStageConfig", menuName = "一夫当关/关卡配置")]
public class StageConfig : ScriptableObject
{
    [Header("关卡信息")]
    public int stageId;
    public string stageName = "第一关";
    
    [Header("波次配置")]
    public List<WaveConfig> waves = new List<WaveConfig>(); // 按顺序执行
    
    [Header("总击杀奖励")]
    [Tooltip("累计击杀里程碑，每个阈值可配置多条奖励（铜钱/回血/升级）")]
    public List<KillMilestoneEntry> killMilestones = new List<KillMilestoneEntry>();
    
    [Header("通关奖励")]
    public int clearCoinReward = 100;
    
    [Header("阵型与可见性（多关卡共享）")]
    [Tooltip("阵型配置资产。若未设置则使用代码默认值")]
    public FormationConfig formationConfig;

    [Header("补齐规则")]
    [Tooltip("选择补齐规则：逐列补齐（每列独立前移）或逐排补齐（整排清空后集体前移）")]
    public FillUpRule fillUpRule = FillUpRule.PerColumn;

    [Header("补齐移动配置（每个关卡可不同）")]
    [Tooltip("连续补齐移动间的延迟（秒）。用于实现'快移动+停顿'的效果")]
    public float rushMoveDelay = 0.2f;

    /// <summary>
    /// 自动计算本关卡所有波次中非零 enemyId 的总数（即总需击杀敌人数量）
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int count = 0;
        foreach (var wave in waves)
        {
            if (wave == null || wave.rows == null) continue;
            foreach (var row in wave.rows)
            {
                if (row == null || row.enemyIds == null) continue;
                foreach (var eid in row.enemyIds)
                    if (eid > 0 && eid != RowConfig.RhythmGateMarker) count++;
            }
        }
        return count;
    }
}
