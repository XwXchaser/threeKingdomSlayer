/// <summary>
/// **角色属性的 statId 词表**（2026-11-19）。
///
/// 这是「哪些升级算**玩家可持有的属性**」的**单一真源**：
///
/// | 层 | 谁在用 | 判据 |
/// |---|---|---|
/// | 资产 | <see cref="UpgradeDefinition.statId"/> | **填了 = 角色属性**；留空 = 机制 / 主动技能 |
/// | 抽池 | <see cref="UpgradeRewardClassifier.IsAttribute"/> | <c>statId</c> 非空 ⇒ 进**属性池**（经验升级） |
/// | 抽池 | <see cref="UpgradeRewardClassifier.IsAccessory"/> | <c>statId</c> 为空且非主动技能 ⇒ 进**装备池**（精英掉落） |
///
/// **为什么不再用 <c>category</c> 兼表**（2026-11-19 用户指出）：
/// 旧的分类口径是「占不占槽位」⇒ <c>category == Numeric</c> 就是属性。
/// 但 <c>category = Numeric</c> ≠「角色属性」——地刺 / 击退波 / 聚拢波 / 反伤这些**机制**
/// 也是 Numeric，于是被当成「角色属性成长」塞进了属性池。
///
/// 现在属性与装备**按语义分**：属性 = 玩家身上能挂着、会随成长变强的数值；
/// 装备 = 机制效果（占不占槽另说）与影之武装。
///
/// ⚠ **这层语义要落在资产上**（<c>UpgradeDefinition.statId</c>，Inspector 可填），
/// 不要在代码里按 upgradeId / effectType 写名单 —— 否则又变成"名单不许代码编"的反面。
/// </summary>
public static class UpgradeStatIds
{
    // ── 生命 / 防御 ──

    /// <summary>生命上限 +N（固定值）。</summary>
    public const string MaxHealth = "max_health";

    /// <summary>防御力：常驻减伤百分比（0.1 = 减伤 10%）。</summary>
    public const string DamageReduction = "damage_reduction";

    /// <summary>破防条加宽：防御姿态的破防阈值 +N（越宽越难破防）。</summary>
    public const string GuardBreakThreshold = "guard_break_threshold";

    // ── 攻击 ──

    /// <summary>攻击力：所有招式伤害的**固定值**加成（先加，再乘倍率层）。</summary>
    public const string AttackPower = "attack_power";

    /// <summary>伤害百分比（加法层）：造成伤害 +X%。</summary>
    public const string DamagePercent = "damage_percent";

    /// <summary>攻击速度倍率（0.15 = +15%）。</summary>
    public const string AttackSpeed = "attack_speed";

    /// <summary>暴击率（0.05 = 5%）。</summary>
    public const string CritRate = "crit_rate";

    /// <summary>暴击伤害倍率加成（0.5 = 暴击时伤害 ×1.5）。</summary>
    public const string CritDamage = "crit_damage";

    /// <summary>攻击射程 +N（格）。</summary>
    public const string AttackRange = "attack_range";

    // ── 节奏 / 收益 ──

    /// <summary>主动技能冷却缩减（0.12 = −12%）。</summary>
    public const string CooldownReduction = "cooldown_reduction";

    /// <summary>经验获取加成（0.05 = +5%）。</summary>
    public const string ExpBonus = "exp_bonus";

    /// <summary>道具掉落率加成（0.1 = +10%）。</summary>
    public const string ItemDropRate = "item_drop_rate";

    /// <summary>道具伤害加成（0.15 = +15%）。</summary>
    public const string ItemDamageBonus = "item_damage_bonus";

    /// <summary>蓄力期间减伤（0.2 = 减伤 20%）。</summary>
    public const string ChargeDamageReduction = "charge_damage_reduction";
}
