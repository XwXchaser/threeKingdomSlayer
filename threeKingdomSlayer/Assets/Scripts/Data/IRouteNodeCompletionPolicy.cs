/// <summary>
/// **节点完成策略**（扩展点）。
///
/// 背景：数据模型上早就写了两个策略字段——
/// <c>FakeRouteNodeConfig.completionPolicy</c>（战斗+奖励 / 出口确认 / 敌人全灭 / 立即）
/// 与 <c>terminalPolicy</c>（终点如何收尾），
/// 但运行时一直用**固定规则**（战斗条目跑完 = 完成；<c>isFinalNode</c> = 收尾整关），
/// 两个字段长期没有消费点（见 design/gameplay-pillars.md）。
///
/// 现在判定走注册表：新增一种「节点怎么算完成 / 要不要收尾整关」=
/// 写一个实现并 <c>FakeRouteRuntime.RegisterNodeCompletionPolicy(...)</c> 注册，
/// **不用改** <c>FakeRouteRuntime</c> 本体。
///
/// 内置实现（<c>DefaultRouteNodeCompletionPolicy</c>）保持今天的规则不变；
/// 想让某个节点走不同语义（例如长坂坡的 EV / J1 声明的「出口确认」），
/// 注册一个只接管那些节点的策略即可。
/// </summary>
public interface IRouteNodeCompletionPolicy
{
    /// <summary>本策略是否负责该节点（按 completionPolicy / terminalPolicy / nodeId 自行判断）。</summary>
    bool CanHandle(FakeRouteNodeConfig node);

    /// <summary>该节点的内容是否已完成（可以进入出口选择或整关收尾）。</summary>
    bool IsContentCompleted(FakeRouteNodeConfig node, IRouteNodeProgressView progress);

    /// <summary>该节点完成后是否直接收尾整条路线（终点语义）。</summary>
    bool ShouldFinishRoute(FakeRouteNodeConfig node);
}
