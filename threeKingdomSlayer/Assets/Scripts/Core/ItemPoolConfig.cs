using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品奖励池配置 — 独立的 ScriptableObject
///
/// 与 UpgradePoolConfig 结构相同（三层稀有度 + 权重），
/// 但仅包含 category == Item 的升级定义。
/// Boss 死亡掉落锦囊时从此池抽取三选一。
/// </summary>
[CreateAssetMenu(fileName = "ItemPoolConfig", menuName = "一夫当关/物品奖励池配置")]
public class ItemPoolConfig : ScriptableObject
{
    [Header("稀有度出现权重")]
    [Tooltip("普通稀有度出现权重")]
    public float commonWeight = 0.7f;
    [Tooltip("稀有出现权重")]
    public float rareWeight = 0.2f;
    [Tooltip("传说出现权重")]
    public float legendaryWeight = 0.1f;

    [Header("普通池")]
    public List<WeightedUpgrade> commonPool;

    [Header("稀有池")]
    public List<WeightedUpgrade> rarePool;

    [Header("传说池")]
    public List<WeightedUpgrade> legendaryPool;
}
