using System;

/// <summary>
/// **内置的出口呈现**（默认兼底）：标题 = 「出口名 → 目标节点名」，其余留空。
///
/// 与 <c>DefaultRouteNodeCompletionPolicy</c> 同一范式：内置实现承载既有行为，
/// 自定义的 <see cref="IRouteChoiceFormatter"/> 优先生效。
/// </summary>
public sealed class DefaultRouteChoiceFormatter : IRouteChoiceFormatter
{
    /// <summary>兼底实现处理所有出口。</summary>
    public bool CanFormat(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode) => true;

    /// <inheritdoc/>
    public void Format(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode, RouteChoiceOption option)
    {
        if (option == null || choice == null) return;
        option.label = choice.displayName + " → " + (choice.targetNode != null ? choice.targetNode.displayName : "");
    }
}

/// <summary>
/// 委托版出口呈现（<see cref="IRouteChoiceFormatter"/> 的适配器）。
///
/// 注册时可以直接传 lambda：
/// <code>
/// runtime.RegisterChoiceFormatter(
///     (choice, from) =&gt; choice.choiceId.StartsWith("risk"),
///     (choice, from, option) =&gt; option.label = "⚠ " + choice.displayName);
/// </code>
/// </summary>
public sealed class DelegateRouteChoiceFormatter : IRouteChoiceFormatter
{
    private readonly Func<FakeRouteChoiceConfig, FakeRouteNodeConfig, bool> _canFormat;
    private readonly Action<FakeRouteChoiceConfig, FakeRouteNodeConfig, RouteChoiceOption> _format;

    public DelegateRouteChoiceFormatter(
        Func<FakeRouteChoiceConfig, FakeRouteNodeConfig, bool> canFormat,
        Action<FakeRouteChoiceConfig, FakeRouteNodeConfig, RouteChoiceOption> format)
    {
        _canFormat = canFormat;
        _format = format;
    }

    /// <inheritdoc/>
    public bool CanFormat(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode)
    {
        return _canFormat == null || _canFormat(choice, fromNode);
    }

    /// <inheritdoc/>
    public void Format(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode, RouteChoiceOption option)
    {
        _format?.Invoke(choice, fromNode, option);
    }
}

/// <summary>
/// 委托版出口可选性判定（<see cref="IRouteChoiceValidator"/> 的适配器）。
/// </summary>
public sealed class DelegateRouteChoiceValidator : IRouteChoiceValidator
{
    private readonly Func<FakeRouteChoiceConfig, FakeRouteNodeConfig, string> _check;

    /// <param name="check">返回 null / 空 = 可选；返回非空字符串 = 不可选（该字符串是原因）。</param>
    public DelegateRouteChoiceValidator(Func<FakeRouteChoiceConfig, FakeRouteNodeConfig, string> check)
    {
        _check = check;
    }

    /// <inheritdoc/>
    public bool IsSelectable(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode, out string reason)
    {
        reason = _check != null ? _check(choice, fromNode) : null;
        return string.IsNullOrEmpty(reason);
    }
}
