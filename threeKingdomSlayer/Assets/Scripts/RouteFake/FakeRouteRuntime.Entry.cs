using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
    /// FakeRouteRuntime 的**落地页两段式**：PrepareForEntry（只准备表现层）→ 玩家点「开始关卡」→ RunPrepared，
    /// 以及起始节点的解析（含 checkpoint 恢复）。
    ///
    /// 按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class FakeRouteRuntime : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════
    // 落地页（就绪态）两段式：PrepareForEntry → 玩家点「开始关卡」→ RunPrepared
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 【落地页】只准备、不跑流程：加载路线场景表现 + 把视觉定位到起始节点（含 checkpoint 恢复）。
    /// 准备完成后 Phase = Ready；任何时刻调 <see cref="RunPrepared"/> 才真正开始跑节点流程。
    /// 与 <see cref="Begin"/> 互斥：会顶掉正在跑的流程。
    /// </summary>
    public void PrepareForEntry(FakeRouteStageConfig config)
    {
        if (_flow != null) { StopCoroutine(_flow); _flow = null; }
        routeConfig = config;
        if (ResolveBattleHost() == null || routeConfig == null)
        {
            Debug.LogError("[FakeRoute] PrepareForEntry: 配置或StageController为空");
            return;
        }
        // 让宿主也知道「当前路线」：结算（CompleteFakeRouteStage）、战败重来
        // （RestartStageCoroutine）、重来按钮都靠这个字段判断「现在跑的是卷轴路线」。
        // 落地页自举时它是 null（没有 MainMenu 传参），不补的话结算会直接被 early-return 掉：
        // 没有 Victory 状态 / 没有通关铜钱 / 没有记录已通关。
        ResolveBattleHost();
        _battleHost.RouteConfig = routeConfig;
        AcquireExclusiveDriving();
        movementPresenter = presenterBehaviour as IRouteMotionPresenter;

        string validationError;
        if (!ValidateConfiguration(out validationError))
        {
            Debug.LogError("[FakeRoute] PrepareForEntry 配置校验失败: " + validationError);
            SetPhase(FakeRoutePhase.None);
            return;
        }
        if (movementPresenter == null || !movementPresenter.TryValidate(routeConfig, out validationError))
        {
            Debug.LogError("[FakeRoute] PrepareForEntry 表现层不可用: " + (movementPresenter == null ? "未绑定 IRouteMotionPresenter" : validationError));
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
        _prepared = false;
        _preparedStartNode = null;
        SetPhase(FakeRoutePhase.Ready);
        _flow = StartCoroutine(PrepareSceneOnly());
    }

    private IEnumerator PrepareSceneOnly()
    {
        yield return null;
        _preparedStartNode = ResolveStartNode();
        if (_preparedStartNode == null)
        {
            Debug.LogError("[FakeRoute] PrepareForEntry: startNode 为空");
            SetPhase(FakeRoutePhase.None);
            yield break;
        }

        SetGameplayInput(false);
        yield return movementPresenter.Prepare(routeConfig, () => Phase != FakeRoutePhase.Defeated);
        if (!movementPresenter.IsSceneReady)
        {
            Debug.LogError("[FakeRoute] PrepareForEntry: 路线表现层未就绪（检查 ScrollRoutePresenter 的 routeSceneName / battleCamera）");
            SetPhase(FakeRoutePhase.None);
            yield break;
        }

        // 不播开场、不进节点：只把视觉定位到起始节点锚点。
        yield return movementPresenter.PlayOpening(null, _preparedStartNode, () => Phase != FakeRoutePhase.Defeated);
        if (Phase == FakeRoutePhase.Defeated) yield break;

        _prepared = true;
        SetPhase(FakeRoutePhase.Ready);
        Debug.Log("[FakeRoute] 落地页就绪：关卡=" + routeConfig.stageName +
                  " 起始节点=" + (_preparedStartNode ? _preparedStartNode.nodeId : "null") +
                  "（等玩家点开始关卡）");
    }

    /// <summary>【落地页】玩家点「开始关卡」：从已准备的起始节点开始跑节点流程。</summary>
    public void RunPrepared()
    {
        if (!_prepared || routeConfig == null || _preparedStartNode == null)
        {
            Debug.LogWarning("[FakeRoute] RunPrepared 被忽略：尚未准备就绪（Phase=" + Phase + "）");
            return;
        }
        if (_flow != null) StopCoroutine(_flow);
        _flow = StartCoroutine(RunPreparedFlow());
    }

    private IEnumerator RunPreparedFlow()
    {
        var startNode = _preparedStartNode;
        _prepared = false;
        Debug.Log("[FakeRoute] 开始关卡：从节点 " + (startNode ? startNode.nodeId : "null") + " 起跑");

        // 开场表现（如果有）：openingTravel 为空时表现层直接停在 startNode 锚点。
        SetGameplayInput(false);
        yield return movementPresenter.PlayOpening(routeConfig.openingTravel, startNode, () => Phase != FakeRoutePhase.Defeated);
        if (Phase == FakeRoutePhase.Defeated) yield break;
        yield return EnterNode(startNode);
    }

    /// <summary>
    /// 解析起始节点（含 checkpoint 恢复）。Begin 与 PrepareForEntry 共用。
    ///
    /// 是否接存档点的判定：
    ///   ① 老路径（<see cref="Begin"/>）：只看 FakeRouteLaunch.StartFromCheckpoint，行为不变。
    ///   ② 落地页自举（<see cref="IsHubEntry"/>）：**无条件**尝试恢复快照。
    ///      落地页语义就是「进游戏 = 当前进度」，而 StartFromCheckpoint 是 static、
    ///      进程冷启动时默认 false；只靠它会让二次启动回到 route.startNode 并丢掉血量/升级。
    ///      快照在通关时已被 FinishRoute 清掉，所以「有快照」= 该关没打完，可以安全恢复。
    /// </summary>
    private FakeRouteNodeConfig ResolveStartNode()
    {
        if (routeConfig == null) return null;
        var startNode = useTestStartNode && testStartNode != null ? testStartNode : routeConfig.startNode;
        bool tryRestoreCheckpoint = !useTestStartNode && (FakeRouteLaunch.StartFromCheckpoint || IsHubEntry);
        if (tryRestoreCheckpoint)
        {
            var snapshot = SaveManager.GetFakeRouteSnapshot(routeConfig.routeId, routeConfig.stageId);
            if (snapshot != null && RestoreCheckpoint(snapshot, out var restoredNode))
            {
                startNode = restoredNode;
                _restoredFromCheckpoint = true;
                _restoredCheckpointNodeId = snapshot.checkpointNodeId;
                Debug.Log("[FakeRoute] restored checkpoint node=" + snapshot.checkpointNodeId);
            }
            else
            {
                Debug.Log("[FakeRoute] no valid fake route checkpoint, starting from startNode");
            }
        }
        return startNode;
    }

    private IEnumerator BeginRouteNextFrame()
    {
        yield return null;
        var startNode = useTestStartNode && testStartNode != null ? testStartNode : routeConfig.startNode;
        if (!useTestStartNode && FakeRouteLaunch.StartFromCheckpoint)
        {
            var snapshot = SaveManager.GetFakeRouteSnapshot(routeConfig.routeId, routeConfig.stageId);
            if (snapshot != null && RestoreCheckpoint(snapshot, out var restoredNode))
            {
                startNode = restoredNode;
                _restoredFromCheckpoint = true;
                _restoredCheckpointNodeId = snapshot.checkpointNodeId;
                Debug.Log("[FakeRoute] restored checkpoint node=" + snapshot.checkpointNodeId);
            }
            else
            {
                Debug.Log("[FakeRoute] no valid fake route checkpoint, starting from startNode");
            }
        }
        if (startNode == null)
        {
            Debug.LogError("[FakeRoute] startNode为空");
            SetPhase(FakeRoutePhase.None);
            yield break;
        }
        // 表现层准备：加载路线场景并校验锚点，必须在播放开场、旅行之前完成。
        SetGameplayInput(false);
        yield return movementPresenter.Prepare(routeConfig, () => Phase != FakeRoutePhase.Defeated);
        if (!movementPresenter.IsSceneReady)
        {
            Debug.LogError("[FakeRoute] 路线表现层未就绪，流程中止（检查 ScrollRoutePresenter 的 routeSceneName / battleCamera）");
            SetPhase(FakeRoutePhase.None);
            yield break;
        }
        // 开场表现：openingTravel 为空时不播开场，表现层直接把视觉定位到 startNode 锚点。
        yield return movementPresenter.PlayOpening(routeConfig.openingTravel, startNode, () => Phase != FakeRoutePhase.Defeated);
        if (Phase == FakeRoutePhase.Defeated) yield break;
        yield return EnterNode(startNode);
    }
}