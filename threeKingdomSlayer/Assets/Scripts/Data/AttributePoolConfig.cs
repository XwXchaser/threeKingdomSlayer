using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **属性奖励池配置**（2026-11-25）——经验升级带来的本局属性成长。
///
/// 与装备池是**两套规则**（`design/gameplay-progression-loop.md` §3.3–§3.4）：
///   · 属性品质概率**全局统一**：不随无尽内部圈数变化，不使用装备的池内档位；
///   · 同一次三选一按**升级内容身份**（<see cref="UpgradeDefinition.KindId"/>）去重：
///     普通生命上限与传说生命上限不会同时出现在一组候选里；
///   · 不同次仍可继续获得同一属性 ⇒ 按既有「每选一次升一级」的增量语义累积，不占槽、不被换装删除。
///
/// ⚠ 品质对应的实际数值 = 该变体资产自己的等级表（例如「传说 生命上限」是一份独立资产，
///   把 <c>kindId</c> 填成普通生命上限的 id、<c>quality</c> 填传说，再填它自己的增量）。
/// </summary>
[CreateAssetMenu(fileName = "AttributePoolConfig", menuName = "一夫当关/属性奖励池配置")]
public class AttributePoolConfig : ScriptableObject
{
    [Header("属性变体（每个品质变体一行）")]
    [Tooltip("每行 = 一个具体属性变体资产。\n" +
             "· 同一内容的普通 / 精良 / 传说各一份资产，共用 kindId；\n" +
             "· **权重** = 该变体在自己品质内的相对出现权重；\n" +
             "· 资格过滤沿用既有规则：前置条件 / 满级（按本局等级）。")]
    public List<WeightedUpgrade> variants = new List<WeightedUpgrade>();

    [Header("品质出现权重（全局统一，不随内部圈数）")]
    [Tooltip("每次抽一张卡之前先按这份权重摇品质，再在该品质的候选里按变体权重抽。\n" +
             "· 权重 > 0 的品质必须有可用变体，否则报配置错误；\n" +
             "· 权重 = 0 表示本次不抽该品质（合法配置，不报错）；\n" +
             "· 正式概率由用户填写，代码里不写死数值。")]
    public List<UpgradeQualityWeight> qualityWeights = new List<UpgradeQualityWeight>();
}

/// <summary>某个品质的全局出现权重。</summary>
[System.Serializable]
public class UpgradeQualityWeight
{
    [Tooltip("品质档：普通 / 精良 / 传说。")]
    public UpgradeQuality quality = UpgradeQuality.Common;

    [Tooltip("相对权重（0 = 不抽该品质）。")]
    [Min(0f)] public float weight = 0f;
}
