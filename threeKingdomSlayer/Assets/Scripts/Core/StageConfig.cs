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
    [Tooltip("该排的敌人ID列表，每个ID对应一个站位。敌人实际占用的列数由 EnemyConfig.occupySlots 决定")]
    public int[] enemyIds = new int[5]; // 长度决定该排有多少个敌人站位
}

/// <summary>
/// 波次配置
/// 每个关卡通常只有1个波次，波次之间播放剧情演出
/// </summary>
[Serializable]
public class WaveConfig
{
    public int waveId;
    public bool isBossWave;
    [Tooltip("此波包含的所有排，按顺序从远到近生成")]
    public List<RowConfig> rows = new List<RowConfig>();
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
    
    [Header("连杀奖励")]
    public List<int> killStreakThresholds = new List<int> { 10, 25, 50, 100 }; // 连杀奖励触发阈值
    
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
}
