/// <summary>
/// 升级效果可写入的**玩家数值面**（扩展点）。
///
/// 数值型升级通过它改动玩家的实际数值。新增一种数值型升级时，
/// 实现 <see cref="IUpgradeNumericApplier"/> 并注册即可，
/// **不需要**去改 <c>UpgradeEffectManager</c> 的私有字段或分派逻辑。
///
/// 约定：
///   • <c>Add*</c> = 累加（可叠加多级），<c>Set*</c> = 覆盖（该类型的语义是赋值）。
///   • 百分比类字段在管理器内部统一换算成「小数」，实现方按同一约定传入即可。
/// </summary>
public interface IUpgradeStatSink
{
    /// <summary>累加伤害倍率。</summary>
    void AddDamageMultiplier(float value);

    /// <summary>累加攻击速度倍率。</summary>
    void AddAttackSpeedMultiplier(float value);

    /// <summary>累加移动速度倍率。</summary>
    void AddMoveSpeedMultiplier(float value);

    /// <summary>累加经验倍率。</summary>
    void AddExpMultiplier(float value);

    /// <summary>累加道具掉落率加成。</summary>
    void AddItemDropRateBonus(float value);

    /// <summary>累加道具伤害加成。</summary>
    void AddItemDamageBonus(float value);

    /// <summary>累加攻击射程加成。</summary>
    void AddAttackRangeBonus(int value);

    /// <summary>累加攻击伤害惩罚（传入的是小数，0.1 = 10%）。</summary>
    void AddAttackDamagePenalty(float value);

    /// <summary>覆盖攻击射程加成（不是累加）。</summary>
    void SetAttackRangeBonus(int value);

    /// <summary>覆盖攻击伤害惩罚（不是累加，传入的是小数）。</summary>
    void SetAttackDamagePenalty(float value);

    /// <summary>累加推波距离。</summary>
    void AddPushWaveDistance(int value);

    /// <summary>累加聚拢波步数。</summary>
    void AddConvergenceStep(int value);

    /// <summary>覆盖聚拢波伤害百分比（0.1 = 10%）。</summary>
    void SetConvergenceDamagePercent(float value);

    /// <summary>累加蓄力减伤（传入的是小数，0.2 = 20%）。</summary>
    void AddChargeDamageReduction(float value);

    /// <summary>累加主动技能冷却缩减（传入的是小数，0.1 = 10%）。</summary>
    void AddActiveSkillCDReduction(float value);

    /// <summary>按升级定义写入反射护盾的该级配置（等级从 1 起）。</summary>
    void ApplyReflectShieldLevel(UpgradeDefinition def, int level);
}
