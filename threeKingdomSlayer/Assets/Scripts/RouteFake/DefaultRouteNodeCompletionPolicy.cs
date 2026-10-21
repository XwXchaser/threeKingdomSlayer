using System;

/// <summary>
/// **内置的节点完成策略**（默认兼底）：保持改造前的固定规则不变。
///
/// 规则：
///   • 内容完成 = 本节点所有「有效」战斗条目都已完成（条目为 null / 无战斗配置 / 条件未开的条目跳过）；
///   • 收尾整关 = <c>node.isFinalNode</c>。
///
/// 它是兼底：自定义策略（<see cref="IRouteNodeCompletionPolicy"/>）优先于它生效。
/// </summary>
public sealed class DefaultRouteNodeCompletionPolicy : IRouteNodeCompletionPolicy
{
    /// <summary>兼底实现处理所有节点。</summary>
    public bool CanHandle(FakeRouteNodeConfig node) => true;

    /// <inheritdoc/>
    public bool IsContentCompleted(FakeRouteNodeConfig node, IRouteNodeProgressView progress)
    {
        if (node == null || node.battleEntries == null) return true;
        if (progress == null)
        {
            // 没有进度视图时退化为「按条目配置判断」（与旧的静态规则一致）。
            for (int i = 0; i < node.battleEntries.Count; i++)
            {
                var entry = node.battleEntries[i];
                if (entry == null || entry.battleConfig == null || entry.conditionEnabled) continue;
                return false;
            }
            return true;
        }

        int count = progress.GetBattleEntryCount(node);
        for (int i = 0; i < count; i++)
        {
            var entry = node.battleEntries[i];
            if (entry == null || entry.battleConfig == null || entry.conditionEnabled) continue;
            if (!progress.IsBattleEntryCompleted(node, i)) return false;
        }
        return true;
    }

    /// <inheritdoc/>
    public bool ShouldFinishRoute(FakeRouteNodeConfig node) => node != null && node.isFinalNode;
}

/// <summary>
/// 委托版节点完成策略（<see cref="IRouteNodeCompletionPolicy"/> 的适配器）。
///
/// 让调用方不必为一个节点语义专门写一个类：
/// <code>
/// runtime.RegisterNodeCompletionPolicy(
///     node =&gt; node.completionPolicy == FakeRouteNodeCompletionPolicy.ChoiceConfirmation,
///     (node, progress) =&gt; progress.HasUncompletedBattleEntry(node) == false,
///     node =&gt; node.isFinalNode);
/// </code>
/// </summary>
public sealed class DelegateRouteNodeCompletionPolicy : IRouteNodeCompletionPolicy
{
    private readonly Func<FakeRouteNodeConfig, bool> _canHandle;
    private readonly Func<FakeRouteNodeConfig, IRouteNodeProgressView, bool> _isContentCompleted;
    private readonly Func<FakeRouteNodeConfig, bool> _shouldFinishRoute;

    public DelegateRouteNodeCompletionPolicy(
        Func<FakeRouteNodeConfig, bool> canHandle,
        Func<FakeRouteNodeConfig, IRouteNodeProgressView, bool> isContentCompleted,
        Func<FakeRouteNodeConfig, bool> shouldFinishRoute)
    {
        _canHandle = canHandle;
        _isContentCompleted = isContentCompleted;
        _shouldFinishRoute = shouldFinishRoute;
    }

    /// <inheritdoc/>
    public bool CanHandle(FakeRouteNodeConfig node)
    {
        return _canHandle == null || _canHandle(node);
    }

    /// <inheritdoc/>
    public bool IsContentCompleted(FakeRouteNodeConfig node, IRouteNodeProgressView progress)
    {
        return _isContentCompleted == null || _isContentCompleted(node, progress);
    }

    /// <inheritdoc/>
    public bool ShouldFinishRoute(FakeRouteNodeConfig node)
    {
        return _shouldFinishRoute != null && _shouldFinishRoute(node);
    }
}
