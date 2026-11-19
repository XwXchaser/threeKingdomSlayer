/// <summary>
/// **局内奖励的两条来源线**（2026-11-17 用户定口径）。
///
/// 用户原话：“当前主要是局内角色属性成长和装备饰品掉落混一起了。
/// 属性成长应该跟经验，装备饰品应该跟精英敌人。”
///
/// | 类别 | 内容 | 来源 | 槽位 |
/// |---|---|---|---|
/// | **属性** | 纯数值（不占槽、可无限叠） | **经验升级** | 无 |
/// | **装备 / 饰品** | 影之武装（主动）/ 饰品（机制型被动） | **精英敌人掉落** | 主动槽 / 被动槽 |
///
/// 分类口径 = **占不占槽位**（与 <c>UpgradeEffectManager</c> 的槽位模型一致）：
///   · <c>Numeric</c> → 不占槽 → **属性**
///   · <c>ActiveSkill</c> → 占主动槽 → **影之武装**
///   · <c>AttackPassive / TimedPassive</c> → 占被动槽 → **饰品**
/// </summary>
public enum UpgradeRewardKind
{
    /// <summary>属性（纯数值）：由**经验升级**发放，不占槽、抽到就叠。</summary>
    Attribute = 0,

    /// <summary>装备 / 饰品（占槽）：由**精英敌人掉落**发放。</summary>
    Equipment = 1,

    /// <summary>道具（V1 的 Boss 锦囊）。V2 规则下不再单独发放。</summary>
    Item = 2,
}

/// <summary>
/// 奖励分类判定（2026-11-17）。
///
/// **单一真源**：属性 / 装备 的划分只在这里定义一次，
/// 抽池器（<c>UpgradePoolSampler</c>）、三选一管理器（<c>UpgradeChoiceManager</c>）
/// 都问这里，不再各自写一遍 category 判断。
/// </summary>
public static class UpgradeRewardClassifier
{
    /// <summary>
    /// 是「**属性**」吗：纯数值、不占槽、可无限叠 → 由经验升级发放。
    /// </summary>
    public static bool IsAttribute(UpgradeDefinition def)
    {
        return def != null && def.category == UpgradeCategory.Numeric;
    }

    /// <summary>
    /// 是「**饰品**」吗：机制型被动（占被动槽）→ 由精英敌人掉落。
    /// </summary>
    public static bool IsAccessory(UpgradeDefinition def)
    {
        if (def == null) return false;
        return def.category == UpgradeCategory.AttackPassive || def.category == UpgradeCategory.TimedPassive;
    }

    /// <summary>
    /// 是「**影之武装**」吗：主动技能（占主动槽）→ 由精英敌人掉落。
    /// </summary>
    public static bool IsShadowArmament(UpgradeDefinition def)
    {
        return def != null && def.category == UpgradeCategory.ActiveSkill;
    }

    /// <summary>
    /// 是「**装备 / 饰品**」吗（占槽的一切）→ 由精英敌人掉落。
    /// </summary>
    public static bool IsEquipment(UpgradeDefinition def)
    {
        return IsShadowArmament(def) || IsAccessory(def);
    }

    /// <summary>这条奖励属于哪一类（用于 UI 标题区分）。</summary>
    public static UpgradeRewardKind KindOf(UpgradeDefinition def)
    {
        if (def == null) return UpgradeRewardKind.Attribute;
        if (def.category == UpgradeCategory.Item) return UpgradeRewardKind.Item;
        if (IsEquipment(def)) return UpgradeRewardKind.Equipment;
        return UpgradeRewardKind.Attribute;
    }
}
