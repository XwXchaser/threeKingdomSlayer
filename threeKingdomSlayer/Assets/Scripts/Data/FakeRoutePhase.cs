/// <summary>
/// 路线阶段（2026-10-23 从 <c>Assets/Scripts/RouteFake/FakeRouteRuntime.cs</c> 提到契约层）。
///
/// 为什么下沉：<see cref="IRouteFlow"/> 端口住在 <c>Locus.Data</c> ✅，
/// 而界面层与教学只认这个端口 ✅ —— 端口的返回类型就必须在下层 ✅，
/// 否则「接口住在数据层、枚举住在流程层」会把流程层重新拉回上层依赖 ❌。
/// </summary>
public enum FakeRoutePhase
{
    None,
    /// <summary>落地页就绪态：表现层已加载、视觉已定位到起始节点，但尚未开始跑流程。</summary>
    Ready,
    EnteringNode,
    Battle,
    WaitingReward,
    RouteChoiceTransition,
    ChoosingRoute,
    FakeMoving,
    Completed,
    Defeated
}
