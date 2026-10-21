/// <summary>
/// 节点进度**只读视图**（给节点完成策略用）。
///
/// 完成策略要判断「这个节点还剩什么没做」时读这个视图，
/// 而不是去摸路线运行时的内部状态（_completedBattleEntries 等）。
/// </summary>
public interface IRouteNodeProgressView
{
    /// <summary>该节点的战斗条目总数（无条目 = 0）。</summary>
    int GetBattleEntryCount(FakeRouteNodeConfig node);

    /// <summary>该节点的某个战斗条目是否已完成。</summary>
    bool IsBattleEntryCompleted(FakeRouteNodeConfig node, int index);

    /// <summary>该节点是否还有未完成的战斗条目。</summary>
    bool HasUncompletedBattleEntry(FakeRouteNodeConfig node);

    /// <summary>是否还有奖励在等玩家（三选一 / 道具弃置 / 拾取中）。</summary>
    bool HasPendingReward { get; }
}
