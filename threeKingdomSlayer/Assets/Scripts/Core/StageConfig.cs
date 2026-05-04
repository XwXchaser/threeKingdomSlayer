using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一排敌人的配置
/// 每排的敌人数量由 enemyIds 长度决定，每个敌人ID对应一个站位
/// 敌人实际占用的列数由其 occupySlots 决定
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
    
    [Header("透明度渐变配置")]
    [Tooltip("每排的透明度系数，索引0=最前排。例如 [1.0, 0.8, 0.6, 0.4, 0.2] 表示前5排从100%到20%")]
    public float[] rowAlphaFactors = new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };
    
    [Header("可见排数限制")]
    [Tooltip("玩家能看到的最大排数，超出此排数的敌人完全透明")]
    public int maxVisibleRows = 5;

    [Header("补齐移动配置")]
    [Tooltip("连续补齐移动间的延迟（秒）。补齐移动完成后若仍需继续前进，等待此延迟后再开始下一次补齐移动。用于实现'快移动+停顿'的效果")]
    public float rushMoveDelay = 0.2f;

    [Header("排阵型配置（梯形/扇形内收）")]
    [Tooltip("方案A：预设表。若设置则优先使用预设表，否则使用方案B公式计算")]
    public RowFormationPreset formationPreset;

    [Header("方案B：手动每排宽度（优先级最高）")]
    [Tooltip("手动指定每一排的半宽值，数组索引=排索引（0=最前排）。\n例如 [4.0, 3.5, 3.0, 2.5, 2.0] 表示第0排半宽4.0、第1排半宽3.5...\n设置此数组后，方案A预设表和方案C公式均被忽略。\n若数组长度不足（如只有3个值但排索引=4），则对超出部分使用最后一位的值。")]
    public float[] manualRowHalfWidths;

    [Header("方案C：公式参数（仅当未设置预设表和手动数组时生效）")]
    [Tooltip("最前排（rowIndex=0）的半宽。例如4.0表示最前排最左列X=-4.0，最右列X=+4.0")]
    public float formationMaxSpread = 4.0f;
    [Tooltip("最后排的半宽。例如0.5表示最后排最左列X=-0.5，最右列X=+0.5")]
    public float formationMinSpread = 0.5f;
    [Tooltip("内收曲线指数。1.0=线性，>1.0=后排更快收拢，<1.0=前排更快收拢")]
    public float formationPowerCurve = 1.2f;
    [Tooltip("排间距（Z轴，世界单位）")]
    public float rowSpacing = 2.5f;
    [Tooltip("阵型整体Z轴偏移（正值=远离摄像机，负值=靠近摄像机）。例如设为10，则最前排敌人Z=10，远离摄像机")]
    public float formationOffsetZ = 0f;
}
