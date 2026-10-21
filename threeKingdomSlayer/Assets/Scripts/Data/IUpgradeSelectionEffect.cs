using System;

/// <summary>
/// 选中升级后的**额外效果**（扩展点）。
///
/// 与已有的 <see cref="IEffectExecutor"/> 分工：
///   • <c>IEffectExecutor</c> = **持续型**行为效果（每次攻击触发、击杀触发……）；
///   • 本接口 = **一次性**效果，只在玩家选中这条升级的那一刻发生一次。
///
/// 例子：选中时额外回血 / 额外给一个道具 / 记录统计 / 与某个天赋联动……
/// </summary>
public interface IUpgradeSelectionEffect
{
    /// <summary>本效果是否对这条升级生效。</summary>
    bool AppliesTo(UpgradeDefinition def);

    /// <summary>执行（在升级本身的效果应用之后调用）。</summary>
    void OnSelected(UpgradeDefinition def);
}

/// <summary>
/// 委托版选中效果（<see cref="IUpgradeSelectionEffect"/> 的适配器）。
///
/// 注册时可以直接传 lambda，不必为一个效果专门写一个类：
/// <code>
/// manager.RegisterSelectionEffect(
///     def =&gt; def.effectType == "crit_rate",
///     def =&gt; CritService.Instance.Grant(def.upgradeId));
/// </code>
/// </summary>
public sealed class DelegateUpgradeSelectionEffect : IUpgradeSelectionEffect
{
    private readonly Func<UpgradeDefinition, bool> _appliesTo;
    private readonly Action<UpgradeDefinition> _onSelected;

    public DelegateUpgradeSelectionEffect(
        Func<UpgradeDefinition, bool> appliesTo,
        Action<UpgradeDefinition> onSelected)
    {
        _appliesTo = appliesTo;
        _onSelected = onSelected;
    }

    /// <inheritdoc/>
    public bool AppliesTo(UpgradeDefinition def)
    {
        return _appliesTo == null || _appliesTo(def);
    }

    /// <inheritdoc/>
    public void OnSelected(UpgradeDefinition def)
    {
        _onSelected?.Invoke(def);
    }
}
