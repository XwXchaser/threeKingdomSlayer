using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级奖励池配置 - ScriptableObject
/// 控制三选一弹窗中各稀有度的出现权重以及每个稀有度池中的候选升级。
/// </summary>
[CreateAssetMenu(fileName = "UpgradePoolConfig", menuName = "一夫当关/升级奖励池配置")]
public class UpgradePoolConfig : ScriptableObject
{
    [Header("稀有度出现权重")]
    [Tooltip("普通稀有度出现权重")]
    public float commonWeight = 0.8f;
    [Tooltip("稀有出现权重")]
    public float rareWeight = 0.15f;
    [Tooltip("传说出现权重")]
    public float legendaryWeight = 0.05f;

    [Header("普通池")]
    public List<WeightedUpgrade> commonPool;

    [Header("稀有池")]
    public List<WeightedUpgrade> rarePool;

    [Header("传说池")]
    public List<WeightedUpgrade> legendaryPool;
}

[System.Serializable]
public class WeightedUpgrade
{
    [Tooltip("升级定义")]
    public UpgradeDefinition upgrade;
    [Tooltip("权重（越高越容易被抽中）")]
    public int weight = 1;
}
