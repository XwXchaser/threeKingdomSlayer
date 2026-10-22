using System;

/// <summary>
/// **路线驱动端口**：流程层对外的唯一入口（2026-10-23，asmdef 分层第十四批）。
///
/// 背景：落地页 / 界面 / 教学都直接点名 <c>FakeRouteRuntime</c>（流程层的具体类），
/// 于是「界面 → 流程具体实现」这条依赖没有边界，流程层一改名/换实现，所有调用点都要动。
/// 改成端口 + 装配点之后：调用方只认接口 ✅，实现换谁都行 ✅。
///
/// 端口只暴露**调用方真正用到的**那几件事（不把整个运行器塞进接口）：
///   • <see cref="Phase"/>：路线阶段（就绪 / 进节点 / 战斗中 / 选路中 / 已完成 …）
///   • <see cref="RouteConfig"/>：当前路线配置
///   • <see cref="IsPrepared"/>：落地页是否已就绪
///   • <see cref="PrepareForEntry"/> / <see cref="RunPrepared"/>：落地页两段流程
/// </summary>
public interface IRouteFlow
{
    /// <summary>当前路线阶段。</summary>
    FakeRoutePhase Phase { get; }

    /// <summary>当前路线配置（未绑定时为 null）。</summary>
    FakeRouteStageConfig RouteConfig { get; }

    /// <summary>落地页是否已准备好（可以点「开始关卡」）。</summary>
    bool IsPrepared { get; }

    /// <summary>准备落地页（切到指定路线）。</summary>
    void PrepareForEntry(FakeRouteStageConfig config);

    /// <summary>正式开始跑这条路线（落地页「开始关卡」）。</summary>
    void RunPrepared();
}

/// <summary>
/// **路线驱动装配点**：流程层登记自己，上层（界面 / 教学）按接口取用。
///
/// 规则同 <c>CombatPresenters</c>：**上层登记、下层使用** ✅；取不到时按「没有路线」安全降级 ✅。
/// </summary>
public static class RouteFlowHub
{
    /// <summary>当前场景里的路线驱动（没有则为 null）。</summary>
    public static IRouteFlow Current { get; set; }

    /// <summary>清空登记（场景重载 / 运行器销毁时用）。</summary>
    public static void Clear()
    {
        Current = null;
    }
}
