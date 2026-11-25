using UnityEngine;

/// <summary>
/// **装备变体的合法性规则**（2026-11-25）——单一真源。
///
/// 抽取器、运行时换装、Editor 校验菜单都问这里，避免出现三套判定：
///   · 什么算「可装备变体」；
///   · 哪些机制条目**暂不入装备池**（用户明确要求：保留资产，不删、不伪装成可拆饰品）；
///   · 变体的可见等级 / 同款身份 / 等级数据是否自洽。
/// </summary>
public static class EquipmentVariantRules
{
    /// <summary>四项暂不入装备池的机制（用户 2026-11-24 定）。保留资产与 GUID，只做池与换装的排除。</summary>
    public static readonly string[] ExcludedMechanicIds =
    {
        "spike_trap",              // 地刺
        "push_wave",               // 击退波
        "convergence_wave",        // 聚拢波
        "charge_reflect_shield",   // 反伤
    };

    /// <summary>是不是用户明确排除的机制条目。</summary>
    public static bool IsExcludedMechanic(UpgradeDefinition def)
    {
        if (def == null) return false;
        string id = def.upgradeId;
        if (string.IsNullOrEmpty(id)) return false;
        for (int i = 0; i < ExcludedMechanicIds.Length; i++)
            if (ExcludedMechanicIds[i] == id) return true;
        // 复制出来的变体沿用原 upgradeId 前缀时也一并排除（例如 push_wave_fine）。
        // 只按「变体的 kindId」判定也可以，但这里两种 id 都查，避免换了 upgradeId 就漏网。
        string kind = def.KindId;
        for (int i = 0; i < ExcludedMechanicIds.Length; i++)
            if (ExcludedMechanicIds[i] == kind) return true;
        return false;
    }

    /// <summary>
    /// 这个变体能不能走「本局装备 / 换装」。
    /// 只有**占槽**的两类可以：影之武装（ActiveSkill）与占槽饰品（AttackPassive / TimedPassive）。
    /// 机制型 Numeric（地刺 / 击退波 / 聚拢波 / 反伤等）效果是层叠相加、没有可逆减法 ⇒ 不支持拆卸。
    /// </summary>
    public static bool IsEquippableCategory(UpgradeDefinition def)
    {
        if (def == null) return false;
        switch (def.category)
        {
            case UpgradeCategory.ActiveSkill:
            case UpgradeCategory.AttackPassive:
            case UpgradeCategory.TimedPassive:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 校验一个装备变体。返回 null = 合法；否则返回面向配置者的具体问题。
    /// </summary>
    public static string Validate(UpgradeDefinition def)
    {
        if (def == null) return "变体为空";
        if (string.IsNullOrEmpty(def.upgradeId)) return $"{def.name}: 缺少 upgradeId";
        if (IsExcludedMechanic(def))
            return $"{def.upgradeId}: 属于暂不入装备池的机制（地刺 / 击退波 / 聚拢波 / 反伤）";
        if (!UpgradeRewardClassifier.IsEquipment(def))
            return $"{def.upgradeId}: 不是装备（属性 / 道具 / 秘籍不能进装备池）";
        if (!IsEquippableCategory(def))
            return $"{def.upgradeId}: 该 category（{def.category}）不占槽，没有可逆的换装实现，不能进装备池";
        if (string.IsNullOrEmpty(def.KindId))
            return $"{def.upgradeId}: 缺少同款身份（kindId 与 upgradeId 都为空）";

        if (def.visibleLevel < 1)
            return $"{def.upgradeId}: 可见等级必须 ≥ 1（当前 {def.visibleLevel}）";

        int rows = def.GetAvailableLevelCount();
        if (rows > 0 && def.visibleLevel > rows)
            return $"{def.upgradeId}: 可见等级 Lv.{def.visibleLevel} 超出该变体的等级数据行数 {rows}（第 {def.visibleLevel} 行不存在）";
        if (rows <= 0 && def.visibleLevel > Mathf.Max(1, def.maxLevel))
            return $"{def.upgradeId}: 没有逐级数据时，可见等级 Lv.{def.visibleLevel} 不能超过 maxLevel（{Mathf.Max(1, def.maxLevel)}）";

        return null;
    }
}
