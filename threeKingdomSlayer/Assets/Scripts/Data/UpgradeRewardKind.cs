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
/// 奖励分类判定（2026-11-17；**2026-11-19 改成按语义**）。
///
/// **单一真源**：属性 / 装备 的划分只在这里定义一次，
/// 抽池器（<c>UpgradePoolSampler</c>）、三选一管理器（<c>UpgradeChoiceManager</c>）
/// 都问这里，不再各自写一遍 category 判断。
///
/// ⚠ **分类口径 = 资产上的 <c>UpgradeDefinition.statId</c>**（2026-11-19）：
///   · <c>statId</c> **非空** ⇒ **属性**（玩家可持有的数值，走经验升级的属性池）
///   · <c>statId</c> **为空** 且不是主动技能 / 道具 ⇒ **装备**（机制效果，走精英掉落的装备池）
///
/// 旧口径（「占不占槽位」= <c>category == Numeric</c> 就是属性）已废弃：
/// <c>category = Numeric</c> ≠「角色属性」——地刺 / 击退波 / 聚拢波 / 反伤这些**机制**
/// 也是 Numeric，旧口径把它们当成了「角色属性成长」塞进了属性池。
/// </summary>
public static class UpgradeRewardClassifier
{
    /// <summary>
    /// 是「**属性**」吗：玩家可持有的数值（资产上填了 <c>statId</c>）→ 由**经验升级**发放。
    /// </summary>
    public static bool IsAttribute(UpgradeDefinition def)
    {
        return def != null && !string.IsNullOrEmpty(def.statId);
    }

    /// <summary>
    /// 是「**饰品**」吗：机制效果（资产上**没填** <c>statId</c>，且不是主动技能 / 道具）
    /// → 由**精英敌人掉落**发放。
    ///
    /// ⚠ 这里**不再按「占不占被动槽」过滤**：机制条目（地刺 / 击退波 / 聚拢波 / 反伤）
    /// 本来就不占被动槽，但它们也不该进属性池 ⇒ 归到装备线，槽位判定另算。
    /// </summary>
    public static bool IsAccessory(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (def.category == UpgradeCategory.ActiveSkill) return false;   // 影之武装单独一个子池
        if (def.category == UpgradeCategory.Item) return false;
        return string.IsNullOrEmpty(def.statId);
    }

    /// <summary>
    /// 是「**影之武装**」吗：主动技能（占主动槽）→ 由**精英敌人掉落**发放。
    /// </summary>
    public static bool IsShadowArmament(UpgradeDefinition def)
    {
        return def != null && def.category == UpgradeCategory.ActiveSkill;
    }

    /// <summary>
    /// 是「**装备 / 饰品**」吗（占槽的一切）→ 由**精英敌人掉落**发放。
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
        if (IsAttribute(def)) return UpgradeRewardKind.Attribute;
        return UpgradeRewardKind.Equipment;
    }
}
