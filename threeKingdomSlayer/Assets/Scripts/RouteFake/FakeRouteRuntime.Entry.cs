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
    /// 进入教学路线时把**这一趟教学**的进度清零（从头开始）。
    ///
    /// 清两样：
    ///   ① 存档里的「已教过的教学拍」标记（否则重进会直接跳过那些拍 → 玩家没真学过）
    ///   ② 教学路线的存档点（checkpoint）→ 否则会从中途的节点继续
    /// </summary>
    private static void ResetTutorialProgressOnEntry(FakeRouteStageConfig config)
    {
        var catalog = RouteCatalog.Resolve();
        var tutorial = catalog != null ? catalog.tutorialStage : null;
        if (tutorial == null || config != tutorial) return;

        var data = SaveManager.Load();
        if (data == null) return;
        if (data.tutorialCompleted) return;              // 已经整条通关 → 不清（那是“教学已完成”的存档）

        int taught = data.taughtTutorialBeats != null ? data.taughtTutorialBeats.Count : 0;
        TutorialSaveSection.ClearTutorialProgress(data);
        SaveManager.Save(data);
        SaveManager.ClearFakeRouteSnapshot(config.routeId, config.stageId);
        if (taught > 0) Debug.Log("[FakeRoute] 进入教学关 → 教学进度清零（从头开始）");
    }

    /// <summary>
    /// 教学路线（<c>RouteCatalog.tutorialStage</c>）。
    /// </summary>
    private static FakeRouteStageConfig FindTutorialRoute()
    {
        var catalog = RouteCatalog.Resolve();
        return catalog != null ? catalog.tutorialStage : null;
    }

    /// <summary>
    /// **教学还没打完 → 强制走教学路线**（不能从主菜单/选关页绕过教学）。
    ///
    /// 判据：<c>SaveData.tutorialCompleted</c>（由教学导演在**整条教学路线跑完**时置位）。
    /// </summary>
    private static FakeRouteStageConfig ForceTutorialRouteIfTutorialIncomplete(FakeRouteStageConfig requested)
    {
        var data = SaveManager.Load();
        if (data == null || TutorialSaveSection.IsCompleted(data)) return requested;

        var tutorial = FindTutorialRoute();
        if (tutorial == null)
        {
            Debug.LogWarning("[FakeRoute] 教学未完成，但场景里没有教学路线配置（RouteCatalog.tutorialStage）→ 按请求的路线走");
            return requested;
        }
        if (requested == tutorial) return requested;

        Debug.Log("[FakeRoute] 教学未完成 → 强制进入教学路线：" + tutorial.stageName
                  + "（原请求：" + (requested != null ? requested.stageName : "null") + "）");
        return tutorial;
    }

    /// <summary>
    /// 【落地页】只准备、不跑流程：加载路线场景表现 + 把视觉定位到起始节点（含 checkpoint 恢复）。
    /// 准备完成后 Phase = Ready；任何时刻调 <see cref="RunPrepared"/> 才真正开始跑节点流程。
    /// 与 <see cref="Begin"/> 互斥：会顶掉正在跑的流程。
    /// </summary>
    public void PrepareForEntry(FakeRouteStageConfig config)
    {
        // ★ **教学没打完 → 强制走教学路线**（2026-11-06 用户反馈：「在主菜单不点教学引导直接点开始游戏」）。
        //   之前这里只看存档里的「当前关卡」→ 教学没打完也能直接进正式关（这条是所有入口的总咽喉：
        //   主菜单「开始关卡」/ 选关页切关 / 落地页自举都走它）。
        config = ForceTutorialRouteIfTutorialIncomplete(config);

        // ★ **进教学关 → 教学进度清零**（2026-11-06 用户：「如果玩家中途退出，不应该从头开始这段教程吗？」）。
        //   教学进度是**这一趟教学内**的事：中途退出 / 掉线 / 换设备 → 下次从头开始。
        //   （只有整条教学路线通关才写「教学完成」→ 那是另一件事，见 TutorialStageDirector。）
        ResetTutorialProgressOnEntry(config);

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
        // 就绪态（落地页 / 主菜单）：局外补给物件可交互，局内补给不可交互
        BreakableProp.HubState = true;
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
        // 关卡态：局内补给物件可交互，局外补给（落地页专属）不再响应
        BreakableProp.HubState = false;
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
    /// 是否接存档点的判定（2026-10-22 改，BUG-005）：
    ///   • **只认显式的「续玩」标记** <see cref="FakeRouteLaunch.StartFromCheckpoint"/>；
    ///   • **落地页自举（<see cref="IsHubEntry"/>）不再恢复检查点** ——
    ///     重新进入游戏从**关卡的初始节点**开始（routeConfig.startNode），
    ///     而不是从上次退出的节点续跑。
    ///     原因：① 玩家反馈「重进游戏直接落在 N3」是 bug（关卡前段内容被跳过）；
    ///     ② 检查点里还存着血量 / 等级 / 升级这些「上一次的状态」，
    ///     换个关卡配置或换个英雄就会变成坏数据（血量对不上）——
    ///     初始节点开局 = 状态干净，不会有这类脏数据。
    ///     想续玩走显式入口（设置 <see cref="FakeRouteLaunch.StartFromCheckpoint"/>）。
    /// </summary>
    private FakeRouteNodeConfig ResolveStartNode()
    {
        if (routeConfig == null) return null;
        var startNode = useTestStartNode && testStartNode != null ? testStartNode : routeConfig.startNode;
        // **一次性消费**：读走即清（否则退出一次关卡后，本会话后续每次进入都从存档点续跑）
        bool tryRestoreCheckpoint = !useTestStartNode && FakeRouteLaunch.ConsumeStartFromCheckpoint();
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
        if (!useTestStartNode && FakeRouteLaunch.ConsumeStartFromCheckpoint())
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