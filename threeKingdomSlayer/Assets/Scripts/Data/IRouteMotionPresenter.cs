using System;
using System.Collections;

/// <summary>
/// 路线表现层接口：运行器只负责提交阶段与目标节点，表现层只负责播放并在结束时回调一次。
/// 卷轴实现见 Assets/Scripts/Core/ScrollRoutePresenter.cs。
/// </summary>
public interface IRouteMotionPresenter
{
    /// <summary>当前是否正在播放表现。</summary>
    bool IsPlaying { get; }

    /// <summary>表现层是否已完成准备（路线场景已加载且锚点已绑定）。</summary>
    bool IsSceneReady { get; }

    /// <summary>配置级校验（不要求表现资源已加载）。</summary>
    bool TryValidate(FakeRouteStageConfig config, out string error);

    /// <summary>加载并绑定路线表现资源，并校验配置中的每个节点都能找到锚点；幂等，可重复调用。</summary>
    IEnumerator Prepare(FakeRouteStageConfig config, Func<bool> canContinue);

    /// <summary>
    /// 开场表现。presentation 为空表示不播开场：实现应把视觉状态直接定位到 startNode 锚点后返回。
    /// </summary>
    IEnumerator PlayOpening(FakeRouteTravelPresentation presentation, FakeRouteNodeConfig startNode, Func<bool> canContinue);

    /// <summary>出口旅行表现。from 为当前节点，to 为该出口的目标节点。</summary>
    IEnumerator PlayChoiceTravel(FakeRouteChoiceConfig choice, FakeRouteNodeConfig from, FakeRouteNodeConfig to, Func<bool> canContinue);

    /// <summary>节点内演出（战后演出等）。presentation 为空时立即返回。</summary>
    IEnumerator PlayNodePresentation(FakeRoutePresentation presentation, Func<bool> canContinue);

    /// <summary>进入节点后同步视觉状态（分支车道、锚点、随行表现开关）。</summary>
    void ApplyNodeVisuals(FakeRouteNodeConfig node);

    /// <summary>跳过当前表现阶段：只完成表现本身，不跳过战斗、奖励或条件求值。</summary>
    void Skip();
}
