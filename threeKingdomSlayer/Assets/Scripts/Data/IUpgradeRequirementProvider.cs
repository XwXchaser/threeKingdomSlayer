/// <summary>升级池的种类（条件源可按种类分别处理）。</summary>
public enum UpgradePoolKind
{
    /// <summary>常规升级池（等级提升的三选一）。</summary>
    Regular = 0,
    /// <summary>道具池（Boss 锦囊）。</summary>
    Item = 1,
    /// <summary>主动技能池（V2 统一池 / Boss 锦囊）。</summary>
    ActiveSkill = 2,
}

/// <summary>
/// 升级**入池的附加条件源**（扩展点）。
///
/// 背景：目前升级入池只有一条硬编码规则——「需要另一个升级达到 N 级」
/// （<c>UpgradeDefinition.prerequisites</c>）。但局外成长（天赋树）已经存在，
/// 后续会出现「某些升级只有解锁了对应天赋才进池」的需求。
///
/// 约定：
///   • <see cref="AppliesTo"/> 返回 true = 本条件源对这条升级生效（按升级定义上的声明判断）；
///   • <see cref="IsSatisfied"/> 返回 true = 条件满足（可以入池）。
///   • 多个条件源是**与**的关系：凡是适用的条件源都必须满足。
///
/// 已有实现：<c>TalentUpgradeRequirementProvider</c>（按天赋解锁入池）。
/// </summary>
public interface IUpgradeRequirementProvider
{
    /// <summary>本条件源是否对这条升级生效。</summary>
    bool AppliesTo(UpgradeDefinition def, UpgradePoolKind pool);

    /// <summary>条件是否满足（true = 可以入池）。</summary>
    bool IsSatisfied(UpgradeDefinition def, UpgradePoolKind pool);
}
