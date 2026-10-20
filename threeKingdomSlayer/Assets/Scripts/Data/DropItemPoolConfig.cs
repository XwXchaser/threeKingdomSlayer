using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>击杀掉落道具池配置</summary>
[CreateAssetMenu(fileName = "DropItemPoolConfig", menuName = "ThreeKingdom/Drop Item Pool Config")]
public class DropItemPoolConfig : ScriptableObject
{
    [Header("掉落概率")]
    [Tooltip("基础掉落概率 (0-1)，例如 0.1 = 10%")]
    [Range(0f, 1f)]
    public float baseDropChance = 0.1f;

    [Header("掉落池")]
    [Tooltip("击杀时按权重随机抽取的道具列表")]
    public List<WeightedDropItem> pool = new List<WeightedDropItem>();
}

[Serializable]
public class WeightedDropItem
{
    [Tooltip("道具定义（必须有 gestureId）")]
    public UpgradeDefinition item;
    [Tooltip("权重，越高越容易抽中")]
    public float weight = 10f;
}
