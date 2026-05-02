using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一排敌人的配置（5列）
/// </summary>
[Serializable]
public class RowConfig
{
    public int[] enemyIds = new int[5]; // 长度必须为5，每列一个敌人ID
}

/// <summary>
/// 波次配置
/// </summary>
[Serializable]
public class WaveConfig
{
    public int waveId;
    public float nextWaveDelay = 3f;       // 本波清完后延迟多久出下一波
    public bool isBossWave;
    public List<RowConfig> rows = new List<RowConfig>(); // 此波包含的所有排
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

    [Header("排阵型配置（梯形/扇形内收）")]
    [Tooltip("方案A：预设表。若设置则优先使用预设表，否则使用方案B公式计算")]
    public RowFormationPreset formationPreset;

    [Header("方案B：公式参数（仅当未设置预设表时生效）")]
    [Tooltip("最前排（rowIndex=0）的半宽。例如4.0表示最前排最左列X=-4.0，最右列X=+4.0")]
    public float formationMaxSpread = 4.0f;
    [Tooltip("最后排的半宽。例如0.5表示最后排最左列X=-0.5，最右列X=+0.5")]
    public float formationMinSpread = 0.5f;
    [Tooltip("内收曲线指数。1.0=线性，>1.0=后排更快收拢，<1.0=前排更快收拢")]
    public float formationPowerCurve = 1.2f;
    [Tooltip("排间距（Z轴，世界单位）")]
    public float rowSpacing = 2.5f;
}
