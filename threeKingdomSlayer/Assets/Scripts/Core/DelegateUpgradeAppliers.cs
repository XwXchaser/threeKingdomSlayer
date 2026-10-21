using System;

/// <summary>
/// 委托版数值型效果应用器（<see cref="IUpgradeNumericApplier"/> 的适配器）。
///
/// 让调用方不必为一个数值效果专门写一个类：
/// <code>
/// UpgradeEffectManager.Instance.RegisterNumericApplier(
///     "crit_rate", (def, level, cfg) => CritService.Instance.AddRate(cfg.floatValue));
/// </code>
/// </summary>
public sealed class DelegateUpgradeNumericApplier : IUpgradeNumericApplier
{
    private readonly Action<UpgradeDefinition, int, NumericLevelConfig> _apply;

    public DelegateUpgradeNumericApplier(Action<UpgradeDefinition, int, NumericLevelConfig> apply)
    {
        _apply = apply;
    }

    /// <inheritdoc/>
    public void Apply(UpgradeDefinition def, int level, NumericLevelConfig cfg)
    {
        _apply?.Invoke(def, level, cfg);
    }
}

/// <summary>
/// 委托版描述格式化器（<see cref="IUpgradeDescriptionFormatter"/> 的适配器）。
///
/// 让调用方不必为一种新升级类型专门写一个类：
/// <code>
/// UpgradeEffectManager.Instance.RegisterDescriptionFormatter(
///     (def, level) =&gt; def.effectType == "crit_rate",
///     (def, desc, level) =&gt; desc.Replace("{0}", CritService.Instance.GetPercent(level).ToString()));
/// </code>
/// </summary>
public sealed class DelegateUpgradeDescriptionFormatter : IUpgradeDescriptionFormatter
{
    private readonly Func<UpgradeDefinition, int, bool> _canFormat;
    private readonly Func<UpgradeDefinition, string, int, string> _format;

    public DelegateUpgradeDescriptionFormatter(
        Func<UpgradeDefinition, int, bool> canFormat,
        Func<UpgradeDefinition, string, int, string> format)
    {
        _canFormat = canFormat;
        _format = format;
    }

    /// <inheritdoc/>
    public bool CanFormat(UpgradeDefinition def, int nextLevel)
    {
        return _canFormat == null || _canFormat(def, nextLevel);
    }

    /// <inheritdoc/>
    public string Format(UpgradeDefinition def, string desc, int nextLevel)
    {
        return _format == null ? desc : _format(def, desc, nextLevel);
    }
}
