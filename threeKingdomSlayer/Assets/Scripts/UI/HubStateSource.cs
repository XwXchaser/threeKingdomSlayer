using System;
using UnityEngine;

/// <summary>
/// 落地页 / 界面状态的**唯一计算源**。
///
/// 背景：原来 <see cref="BattleStartStageButton"/>、<see cref="BattleHubTabBar"/>、
/// <see cref="HubHudModeSwitch"/> 三个组件各自每帧读 <c>FakeRouteRuntime.IsPrepared</c> +
/// <c>SceneFlowManager.HasOverlay</c>。同一份状态在三个地方各算一遍，出现显示异常时
/// 只能去三个组件里逐个猜是谁算错了；谁改了状态也不可追踪。
///
/// 现在：只有本组件每帧读一次权威状态，变化时抛 <see cref="Changed"/>，
/// 订阅者只在变化时更新一次 UI。订阅者不再拥有"什么时候该刷新"的判断权。
///
/// 职责边界（对应 design/architecture-decoupling-analysis.md §4.3）：
///   • 本组件只回答"现在的界面状态是什么"，不碰任何 UI 对象；
///   • 订阅者只把状态映射成自己的显隐，不反向修改状态。
/// </summary>
public sealed class HubStateSource : MonoBehaviour
{
    public static HubStateSource Instance { get; private set; }

    [Tooltip("留空则自动找场景里的路线驱动（实现 IRouteFlow 的组件）")]
    public MonoBehaviour routeRuntime;

    /// <summary>落地页就绪态（已准备好，可以直接开始关卡）。</summary>
    public bool IsPrepared { get; private set; }

    /// <summary>叠在上层的界面场景名（null/空 = 战斗界面）。</summary>
    public string Overlay { get; private set; }

    /// <summary>是否有界面叠在战斗界面之上（选关 / 天赋）。</summary>
    public bool IsOverlayOpen => !string.IsNullOrEmpty(Overlay);

    /// <summary>IsPrepared / Overlay 任一变化时触发；初始化后的第一帧必定触发一次。</summary>
    public event Action Changed;

    private bool _primed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        var routeFlow = routeRuntime as IRouteFlow ?? RouteFlowHub.Current;
        bool prepared = routeFlow != null && routeFlow.IsPrepared;
        var flow = SceneFlowManager.Instance;
        string overlay = flow != null ? flow.CurrentOverlay : null;

        // 第一帧强制广播一次：订阅者可能在 Enable 阶段就已经挂在场景里，
        // 若初始值恰好等于默认值（false / null），不强制广播它们就永远收不到首次通知。
        bool changed = !_primed || prepared != IsPrepared || overlay != Overlay;
        if (!changed) return;

        _primed = true;
        IsPrepared = prepared;
        Overlay = overlay;
        Changed?.Invoke();
    }
}
