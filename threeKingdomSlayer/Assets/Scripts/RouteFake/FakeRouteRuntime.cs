using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class FakeRouteRuntime : MonoBehaviour, IRouteNodeProgressView, IRouteFlow
{
    [SerializeField] private FakeRouteStageConfig routeConfig;
    [SerializeField] private StageController stageController;
    [SerializeField] private MonoBehaviour presenterBehaviour;
    private IRouteMotionPresenter movementPresenter;
    /// <summary>路线战斗宿主：只通过端口接口调用，不直接碰 StageController 的具体成员。</summary>
    private IRouteStageHost _battleHost;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool useTestStartNode;
    [SerializeField] private RouteChoicePanel choicePanelPrefab;

    [SerializeField] private FakeRouteNodeConfig testStartNode;

    // ── 节点完成策略注册表（扩展点）──
    // 新增一种「节点怎么算完成 / 要不要收尾整关」= 写一个 IRouteNodeCompletionPolicy 并注册，
    // 不用改本类；内置兼底保持改造前的固定规则。
    private readonly List<IRouteNodeCompletionPolicy> _nodeCompletionPolicies = new List<IRouteNodeCompletionPolicy>();
    // 字段初始化（不用 Awake）：任何时候拿到这个对象都有一份可用的兼底策略。
    private IRouteNodeCompletionPolicy _defaultNodeCompletionPolicy = new DefaultRouteNodeCompletionPolicy();

    private readonly Dictionary<FakeRouteNodeConfig, HashSet<int>> _completedBattleEntries = new Dictionary<FakeRouteNodeConfig, HashSet<int>>();
    private readonly HashSet<FakeRouteNodeConfig> _visitedNodes = new HashSet<FakeRouteNodeConfig>();
    private readonly List<FakeRouteChoiceSaveState> _choiceHistory = new List<FakeRouteChoiceSaveState>();
    private FakeRouteNodeConfig _currentNode;
    private int _battleIndex;
    private bool _battleActive;
    private bool _battleCleared;
    private bool _routeSettled;
    private bool _restoredFromCheckpoint;
    private string _restoredCheckpointNodeId;
    private int _generation;
    private Coroutine _flow;

    /// <summary>就绪态下等待玩家点「开始关卡」的起始节点（含 checkpoint 恢复结果）。</summary>
    private FakeRouteNodeConfig _preparedStartNode;
    private bool _prepared;

    /// <summary>已准备完毕（表现层就位、随时可 RunPrepared）。</summary>
    public bool IsPrepared => _prepared;

    /// <summary>
    /// true = 本次是 Battle 场景自己当落地页自举起来的（没有 MainMenu 传参）。
    /// 结算/退到菜单时据此决定「回落地页」还是「回 MainMenu」。
    /// </summary>
    public bool IsHubEntry { get; private set; }

    public static FakeRouteRuntime Instance { get; private set; }
    public FakeRouteStageConfig RouteConfig => routeConfig;
    public FakeRouteNodeConfig CurrentNode => _currentNode;
    public FakeRoutePhase Phase { get; private set; }
    public string RouteId => routeConfig != null ? routeConfig.routeId : string.Empty;
    public int StageId => routeConfig != null ? routeConfig.stageId : -1;
    public int ConfigurationVersion => routeConfig != null ? routeConfig.configurationVersion : -1;
    public bool IsChoosing => Phase == FakeRoutePhase.ChoosingRoute;
    public bool IsFakeMoving => Phase == FakeRoutePhase.FakeMoving;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 登记到路线驱动装配点（2026-10-23）：界面 / 教学只认 IRouteFlow 端口，
        // 不再点名流程层的具体类。
        RouteFlowHub.Current = this;
        // 登记道具破坏通知的接收方：战斗内核里的道具被破坏时经端口通知，
        // 不再点名流程类（2026-10-22 asmdef 分层第八批）。
        PropDestroyNotifiers.Current = this;
        movementPresenter = presenterBehaviour as IRouteMotionPresenter;
        AcquireExclusiveDriving();
    }

    /// <summary>
    /// 取得独占驱动权：关闭 StageController 的自动开战，保证同一时刻只有一个路线驱动在运行。
    /// 幂等，可重复调用。
    ///
    /// 2026-10-20：原先这里还会遍历停用场景内的 <c>RouteStageRuntimeV2</c>（靠运行期暴力
    /// 保证“只有一个驱动”）。Battle 场景里那个 V2 组件已删除，V2 实现整体收进
    /// Assets/Scripts/Legacy/RouteV2（只剩试玩场景在用），这段竞争逻辑随之移除。
    /// </summary>
    private void AcquireExclusiveDriving()
    {
        var host = ResolveBattleHost();
        if (host != null) host.AutoStartStage = false;
    }

    /// <summary>解析路线战斗宿主（StageController 实现 IRouteStageHost），幂等。</summary>
    private IRouteStageHost ResolveBattleHost()
    {
        if (_battleHost == null && stageController != null) _battleHost = stageController as IRouteStageHost;
        if (_battleHost == null) _battleHost = StageController.Instance as IRouteStageHost;
        return _battleHost;
    }

    private IEnumerator Start()
    {
        if (!autoStart) yield break;
        yield return null;
        if (ResolveBattleHost() == null) yield break;

        // 落地页自举：读存档「当前进度」→ 只准备表现层，等玩家点「开始关卡」。
        // （历史 ① 传统路径依赖 FakeRouteLaunch.PendingConfig，MainMenu 删除后无人写入，
        //    2026-10-20 批次 0 已移除；Begin() 本身保留，战败重来仍走它。）
        var activeRoute = ResolveActiveRouteFromSave();
        if (activeRoute == null)
        {
            Debug.LogWarning("[FakeRoute] 落地页自举失败：找不到可用的路线配置（检查 StageConfigManager 是否在本场景）");
            yield break;
        }
        Debug.Log("[FakeRoute] 落地页自举：当前关卡 = " + activeRoute.stageName);
        IsHubEntry = true;
        PrepareForEntry(activeRoute);
    }

    /// <summary>
    /// 从存档解析「玩家当前关卡」：
    /// SaveManager.GetActiveFakeRouteStageId() → 在 StageConfigManager.GetSelectableRoutes() 里找。
    /// 存档 id 失效（比如关卡被删）时回退到可选列表第一条。
    /// </summary>
    private FakeRouteStageConfig ResolveActiveRouteFromSave()
    {
        var catalog = RouteCatalog.Resolve();
        if (catalog == null) return null;

        var routes = catalog.GetSelectableRoutes();
        if (routes.Count == 0) return null;

        int activeId = SaveManager.GetActiveFakeRouteStageId();
        for (int i = 0; i < routes.Count; i++)
            if (routes[i] != null && routes[i].stageId == activeId) return routes[i];

        Debug.LogWarning("[FakeRoute] 存档当前关卡 id=" + activeId + " 不在可选列表里，回退到第一条: " + routes[0].stageName);
        return routes[0];
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (ReferenceEquals(RouteFlowHub.Current, this)) RouteFlowHub.Clear();
        if (_battleHost != null)
            _battleHost.RouteBattleCompleted -= NotifyBattleCompleted;
    }

    public void Begin(FakeRouteStageConfig config)
    {
        // 显式 Begin = 由 MainMenu 选关/继续启动的老路径，结算后回 MainMenu。
        IsHubEntry = false;
        if (_flow != null) StopCoroutine(_flow);
        routeConfig = config;
        if (ResolveBattleHost() == null || routeConfig == null)
        {
            Debug.LogError("[FakeRoute] 配置或StageController为空");
            return;
        }
        AcquireExclusiveDriving();
        movementPresenter = presenterBehaviour as IRouteMotionPresenter;

        string validationError;
        if (!ValidateConfiguration(out validationError))
        {
            Debug.LogError("[FakeRoute] 配置校验失败: " + validationError);
            SetPhase(FakeRoutePhase.None);
            return;
        }
        if (movementPresenter == null || !movementPresenter.TryValidate(routeConfig, out validationError))
        {
            Debug.LogError("[FakeRoute] 表现层不可用: " + (movementPresenter == null ? "未绑定 IRouteMotionPresenter" : validationError));
            SetPhase(FakeRoutePhase.None);
            return;
        }
        ResolveBattleHost();
        _battleHost.RouteBattleCompleted -= NotifyBattleCompleted;
        _battleHost.RouteBattleCompleted += NotifyBattleCompleted;
        _generation++;
        _routeSettled = false;
        _completedBattleEntries.Clear();
        _visitedNodes.Clear();
        _choiceHistory.Clear();
        _restoredFromCheckpoint = false;
        _restoredCheckpointNodeId = null;
        _currentNode = null;
        _battleActive = false;
        SetPhase(FakeRoutePhase.EnteringNode);
        _flow = StartCoroutine(BeginRouteNextFrame());
    }




    public bool ValidateConfiguration(out string error)
    {
        if (!FakeRouteGraphValidator.TryValidate(routeConfig, out error)) return false;
        // 本流程禁止回访，因此终点节点必须恰好有一条 BattleEntry（与视频分支一致）。
        for (int i = 0; i < routeConfig.nodes.Count; i++)
        {
            var node = routeConfig.nodes[i];
            if (node != null && node.isFinalNode && (node.battleEntries == null || node.battleEntries.Count != 1))
            {
                error = "终点节点必须恰好有一条 BattleEntry: " + node.nodeId;
                return false;
            }
        }
        error = string.Empty;
        return true;
    }

    public void NotifyBattleCompleted()
    {
        if (!_battleActive || Phase != FakeRoutePhase.Battle) return;
        _battleCleared = true;
    }



    private void SetPhase(FakeRoutePhase phase)
    {
        Phase = phase;
        if (phase != FakeRoutePhase.Battle) SetGameplayInput(false);
    }

    private void SetGameplayInput(bool enabled)
    {
        // 旧实现：直接写全局布尔（后写者覆盖前写者）。
        // 现在路线层只管自己那一份拦截：别的模块（结算面板 / 教学门槛）要拦输入时，
        // 不会被这里「解锁」顺带放掉。
        bool allow = enabled
            && PlayerState.Instance != null
            && PlayerState.Instance.stageState == StageState.InProgress;
        if (allow) GameplayInputGate.Release(this);
        else GameplayInputGate.RequestBlock(this, InputBlockReason.RouteTravel);
        if (!enabled && InputManager.Instance != null) InputManager.Instance.CancelCurrentGesture();
    }

    private bool ContainsNode(FakeRouteNodeConfig node)
    {
        if (routeConfig == null || routeConfig.nodes == null) return false;
        for (int i = 0; i < routeConfig.nodes.Count; i++)
            if (routeConfig.nodes[i] == node) return true;
        return false;
    }

    /// <summary>本节点本次要执行的 BattleEntry 是否已全部完成（无有效条目视为已完成）。</summary>
    private bool IsNodeContentCompleted(FakeRouteNodeConfig node)
    {
        // 扩展点：判定走「节点完成策略」注册表（自定义优先，内置兼底）。
        var policy = ResolveNodeCompletionPolicy(node);
        if (policy == null) return true;
        return policy.IsContentCompleted(node, this);
    }

    /// <summary>该节点完成后是否直接收尾整条路线（终点语义）。</summary>
    private bool ShouldFinishRouteAtNode(FakeRouteNodeConfig node)
    {
        var policy = ResolveNodeCompletionPolicy(node);
        return policy != null && policy.ShouldFinishRoute(node);
    }

    /// <summary>解析该节点适用的完成策略：自定义优先，都没有命中则用内置兼底。</summary>
    private IRouteNodeCompletionPolicy ResolveNodeCompletionPolicy(FakeRouteNodeConfig node)
    {
        for (int i = 0; i < _nodeCompletionPolicies.Count; i++)
        {
            var policy = _nodeCompletionPolicies[i];
            if (policy != null && policy.CanHandle(node)) return policy;
        }
        return _defaultNodeCompletionPolicy;
    }

    /// <summary>
    /// 注册**节点完成策略**（扩展点）。
    /// 自定义策略优先于内置兼底；多个自定义按注册顺序尝试，第一个 CanHandle 为真的接管该节点。
    /// </summary>
    public void RegisterNodeCompletionPolicy(IRouteNodeCompletionPolicy policy)
    {
        if (policy == null) return;
        _nodeCompletionPolicies.Add(policy);
    }

    /// <summary>
    /// 注册**节点完成策略**（扩展点，委托版）。
    /// </summary>
    public void RegisterNodeCompletionPolicy(
        System.Func<FakeRouteNodeConfig, bool> canHandle,
        System.Func<FakeRouteNodeConfig, IRouteNodeProgressView, bool> isContentCompleted,
        System.Func<FakeRouteNodeConfig, bool> shouldFinishRoute)
    {
        RegisterNodeCompletionPolicy(new DelegateRouteNodeCompletionPolicy(canHandle, isContentCompleted, shouldFinishRoute));
    }

    // ── IRouteNodeProgressView 实现（给完成策略看的节点进度只读视图）──

    int IRouteNodeProgressView.GetBattleEntryCount(FakeRouteNodeConfig node)
    {
        return node != null && node.battleEntries != null ? node.battleEntries.Count : 0;
    }

    bool IRouteNodeProgressView.IsBattleEntryCompleted(FakeRouteNodeConfig node, int index)
    {
        return node != null && IsBattleEntryCompleted(node, index);
    }

    bool IRouteNodeProgressView.HasUncompletedBattleEntry(FakeRouteNodeConfig node)
    {
        return HasUncompletedBattleEntry(node);
    }

    bool IRouteNodeProgressView.HasPendingReward => IsRewardBlocking();

    private bool HasUncompletedBattleEntry(FakeRouteNodeConfig node)
    {
        if (node == null || node.battleEntries == null) return false;
        for (int i = 0; i < node.battleEntries.Count; i++)
        {
            var entry = node.battleEntries[i];
            if (entry != null && entry.battleConfig != null && !entry.conditionEnabled && !IsBattleEntryCompleted(node, i))
                return true;
        }
        return false;
    }

    private bool IsBattleEntryCompleted(FakeRouteNodeConfig node, int index)
    {
        return _completedBattleEntries.TryGetValue(node, out var completed) && completed.Contains(index);
    }

    private void MarkBattleEntryCompleted(FakeRouteNodeConfig node, int index)
    {
        if (!_completedBattleEntries.TryGetValue(node, out var completed))
        {
            completed = new HashSet<int>();
            _completedBattleEntries.Add(node, completed);
        }
        completed.Add(index);
    }


}
