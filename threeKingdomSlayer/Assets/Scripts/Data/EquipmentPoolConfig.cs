using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **装备奖励池配置**（2026-11-25）——影之武装 / 饰品的本局掉落池。
///
/// 与「属性池」分开两套规则（`design/gameplay-progression-loop.md` §3.3–§3.4）：
///   · **装备**：每个变体单独配「池内档位 + 档内权重」；内部圈数阶段决定各档概率；
///   · **属性**：见 <see cref="AttributePoolConfig"/>，品质概率全局统一，不看圈、不用档。
///
/// ⚠ 玩家看到的品质 / 等级（<see cref="UpgradeDefinition.quality"/> / <see cref="UpgradeDefinition.visibleLevel"/>）
///   与这里的**池内档位是两组独立数据**：同为「传说 Lv.1」的两件装备可以落在不同档，
///   同为 Lv.1 也不会因此自动同档。不要从品质 / 等级反推档位。
///
/// ⚠ 没有共享库存、没有个数限制：抽到 / 已装备都不减少池里的数量，后续仍能再出现同款。
/// </summary>
[CreateAssetMenu(fileName = "EquipmentPoolConfig", menuName = "一夫当关/装备奖励池配置")]
public class EquipmentPoolConfig : ScriptableObject
{
    [Header("装备变体（每个品质 / 等级变体一行）")]
    [Tooltip("每行 = 一个具体变体（一个 UpgradeDefinition 资产）。\n" +
             "· 同一件装备的不同品质 / 等级 **各占一行**（各自一个资产、共用 kindId）；\n" +
             "· **档位** = 类自走棋「费用」，独立于玩家可见品质 / 等级；\n" +
             "· **权重** = 同档内这个变体的相对出现权重（0 = 本次不抽它，但仍参与配置校验）。\n" +
             "⚠ 地刺 / 击退波 / 聚拢波 / 反伤这四项机制暂不入装备池（配了会报配置错误）。")]
    public List<EquipmentVariantEntry> variants = new List<EquipmentVariantEntry>();

    [Header("内部圈数阶段 → 各池内档位权重")]
    [Tooltip("按列表顺序取**第一个命中**的阶段（minCircle ≤ 内部圈数 ≤ maxCircle；maxCircle=0 表示不设上限）。\n" +
             "每个阶段只配「各档的相对权重」，不要求加起来等于 1；系统按正权重归一化。\n" +
             "⚠ 正式分段与概率由用户填写；这里不写死任何数值。")]
    public List<EquipmentTierStage> stages = new List<EquipmentTierStage>();

    /// <summary>匹配某个内部圈数的阶段；没有命中返回 null（由抽取器报配置错误）。</summary>
    public EquipmentTierStage FindStage(int circle)
    {
        if (stages == null) return null;
        for (int i = 0; i < stages.Count; i++)
        {
            var stage = stages[i];
            if (stage == null) continue;
            if (circle < stage.minCircle) continue;
            if (stage.maxCircle > 0 && circle > stage.maxCircle) continue;
            return stage;
        }
        return null;
    }
}

/// <summary>装备池里的一行：一个具体变体 + 它的池内档位 / 档内权重。</summary>
[System.Serializable]
public class EquipmentVariantEntry
{
    [Tooltip("具体变体资产（影之武装 / 占槽饰品）。同一件装备的不同品质 / 等级各填一行。")]
    public UpgradeDefinition variant;

    [Tooltip("**池内档位**（类自走棋费用，≥1）。与玩家看到的品质 / 等级无关。")]
    [Min(1)] public int tier = 1;

    [Tooltip("同档内的相对出现权重（0 = 不抽它）。")]
    [Min(0)] public int weight = 100;
}

/// <summary>内部圈数的一个阶段：命中后按 <see cref="tierWeights"/> 抽档位。</summary>
[System.Serializable]
public class EquipmentTierStage
{
    [Tooltip("备注（只给自己看，例如「前期 / 中期 / 后期」）。")]
    public string label = "";

    [Tooltip("阶段起始内部圈数（含）。")]
    [Min(1)] public int minCircle = 1;

    [Tooltip("阶段结束内部圈数（含）。**0 = 不设上限**。")]
    [Min(0)] public int maxCircle = 0;

    [Tooltip("本阶段各档位的相对权重。只写用到的档；权重必须 > 0 才有概率。")]
    public List<EquipmentTierWeight> tierWeights = new List<EquipmentTierWeight>();
}

/// <summary>一个档位在某阶段里的相对权重。</summary>
[System.Serializable]
public class EquipmentTierWeight
{
    [Tooltip("池内档位（对应装备池行的 tier）。")]
    [Min(1)] public int tier = 1;

    [Tooltip("相对权重（>0 才会被抽到）。")]
    [Min(0f)] public float weight = 1f;
}
