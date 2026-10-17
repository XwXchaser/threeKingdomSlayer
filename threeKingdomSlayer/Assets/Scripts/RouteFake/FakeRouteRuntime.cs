using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FakeRoutePhase
{
    None,
    EnteringNode,
    Battle,
    WaitingReward,
    RouteChoiceTransition,
    ChoosingRoute,
    FakeMoving,
    Completed,
    Defeated
}

public sealed class FakeRouteRuntime : MonoBehaviour
{
    [SerializeField] private FakeRouteStageConfig routeConfig;
    [SerializeField] private StageController stageController;
    [SerializeField] private MonoBehaviour presenterBehaviour;
    private IRouteMotionPresenter movementPresenter;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool useTestStartNode;
    [SerializeField] private RouteChoicePanel choicePanelPrefab;

    [SerializeField] private FakeRouteNodeConfig testStartNode;

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
        movementPresenter = presenterBehaviour as IRouteMotionPresenter;
        AcquireExclusiveDriving();
    }

    /// <summary>
    /// 取得独占驱动权：关闭 StageController 的自动开战并停用场景内的 RouteStageRuntimeV2，
    /// 保证同一时刻只有一个路线驱动在运行。幂等，可重复调用。
    /// </summary>
    private void AcquireExclusiveDriving()
    {
        if (stageController == null) stageController = StageController.Instance;
        if (stageController != null) stageController.autoStartStage = false;
        var runtimes = FindObjectsOfType<RouteStageRuntimeV2>();
        for (int i = 0; i < runtimes.Length; i++)
        {
            if (runtimes[i] == null) continue;
            runtimes[i].StopAllCoroutines();
            runtimes[i].enabled = false;
        }
    }

    private IEnumerator Start()
    {
        if (!autoStart) yield break;
        yield return null;
        // 只有本关被显式启动（MainMenu 选关/继续、或失败重开）时才自走：
        // 直接播放 Battle 场景（StageController 未拿到路线配置）时不启动，避免抢占其他测试环境。
        if (stageController == null) stageController = StageController.Instance;
        if (stageController == null || stageController.fakeRouteStageConfig == null) yield break;
        Begin(stageController.fakeRouteStageConfig);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (stageController != null)
            stageController.OnRouteBattleCompleted -= NotifyBattleCompleted;
    }

    public void Begin(FakeRouteStageConfig config)
    {
        if (_flow != null) StopCoroutine(_flow);
        routeConfig = config;
        if (stageController == null) stageController = StageController.Instance;
        if (stageController == null || routeConfig == null)
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
        stageController.OnRouteBattleCompleted -= NotifyBattleCompleted;
        stageController.OnRouteBattleCompleted += NotifyBattleCompleted;
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

    private IEnumerator EnterNode(FakeRouteNodeConfig node)
    {
        if (node == null || !ContainsNode(node))
        {
            Debug.LogError("[FakeRoute] 目标节点无效");
            SetPhase(FakeRoutePhase.None);
            yield break;
        }

        _currentNode = node;
        _visitedNodes.Add(node);
        SetPhase(FakeRoutePhase.EnteringNode);
        movementPresenter?.ApplyNodeVisuals(node);
        yield return PlayEntryDialogue(node.entryDialogue);
        if (Phase == FakeRoutePhase.Defeated) yield break;
        if (node.savePoint && (!_restoredFromCheckpoint || node.nodeId != _restoredCheckpointNodeId))
            SaveCheckpoint();
        yield return RunBattleEntries();
        if (Phase == FakeRoutePhase.Defeated) yield break;

        if (node.isFinalNode)
        {
            if (HasUncompletedBattleEntry(node))
            {
                Debug.LogError("[FakeRoute] 终点节点仍有未完成BattleEntry: " + node.nodeId);
                SetPhase(FakeRoutePhase.None);
                yield break;
            }
            yield return FinishRoute();
            yield break;
        }

        if (node.outgoingChoices == null || node.outgoingChoices.Count == 0)
        {
            Debug.LogError("[FakeRoute] 非终点节点没有出口: " + node.nodeId);
            SetPhase(FakeRoutePhase.None);
            yield break;
        }

        SetPhase(FakeRoutePhase.ChoosingRoute);
        ShowRouteChoice();
    }

    private IEnumerator PlayPostBattleSequence(FakeRouteNodeConfig node)
    {
        if (node == null || (node.postBattleDialogue == null && node.postBattlePresentation == null)) yield break;
        int generation = _generation;
        RouteChoicePanel.HideCurrent();
        SetGameplayInput(false);
        yield return PlayEntryDialogue(node.postBattleDialogue);
        if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;
        if (node.postBattlePresentation == null || movementPresenter == null) yield break;

        SetPhase(FakeRoutePhase.EnteringNode);
        SetGameplayInput(false);
        stageController?.SetRouteTravelState();
        yield return movementPresenter.PlayNodePresentation(node.postBattlePresentation,
            () => generation == _generation && Phase != FakeRoutePhase.Defeated);
        if (generation == _generation && Phase != FakeRoutePhase.Defeated)
        {
            // A held video frame is the node's new environment (e.g. dawn).
            // Only presentations without frame retention should restore a static background.
            movementPresenter.ApplyNodeVisuals(node);
            Debug.Log("[FakeRoute] post-battle presentation completed node=" + node.nodeId);
        }
    }

    private IEnumerator PlayEntryDialogue(DialogueEventData dialogue)
    {
        if (dialogue == null || DialogueManager.Instance == null)
            yield break;

        bool queued = DialogueManager.Instance.QueueEventAndNotify(dialogue);
        if (!queued)
            yield break;

        yield return null;
        while (DialogueManager.Instance.IsBusy && Phase != FakeRoutePhase.Defeated)
            yield return null;
    }

    private IEnumerator RunBattleEntries()
    {
        if (_currentNode.battleEntries == null || _currentNode.battleEntries.Count == 0)
            yield break;

        bool anyExecuted = false;
        for (_battleIndex = 0; _battleIndex < _currentNode.battleEntries.Count; _battleIndex++)
        {
            var entry = _currentNode.battleEntries[_battleIndex];
            if (entry == null || entry.battleConfig == null) continue;
            if (entry.conditionEnabled) continue;
            if (IsBattleEntryCompleted(_currentNode, _battleIndex)) continue;

            int generation = _generation;
            _battleActive = true;
            _battleCleared = false;
            SetPhase(FakeRoutePhase.Battle);
            movementPresenter?.ApplyNodeVisuals(_currentNode);
            stageController.StartRouteBattle(entry.battleConfig);
            SetGameplayInput(true);

            while (_battleActive && !_battleCleared && generation == _generation && Phase != FakeRoutePhase.Defeated)
                yield return null;
            if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;

            SetGameplayInput(false);
            SetPhase(FakeRoutePhase.WaitingReward);
            stageController.SetRouteRewardWaitState();
            while (IsRewardBlocking() && generation == _generation && Phase != FakeRoutePhase.Defeated)
                yield return null;
            if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;

            MarkBattleEntryCompleted(_currentNode, _battleIndex);
            _battleActive = false;
            anyExecuted = true;
            Debug.Log("[FakeRoute] battle entry completed node=" + _currentNode.nodeId + " index=" + _battleIndex);
        }

        // 禁止回访，因此本节点本次进入的全部 Entry 必须一次跑完，然后统一播放战后演出。
        if (anyExecuted)
            yield return PlayPostBattleSequence(_currentNode);
    }

    private bool IsRewardBlocking()
    {
        return (UpgradeChoiceManager.Instance != null && UpgradeChoiceManager.Instance.IsChoosing)
            || ItemDiscardPopup.IsShowing
            || (ExpGemManager.Instance != null && ExpGemManager.Instance.IsCollecting);
    }

    public bool TrySelectChoice(string choiceId)
    {
        if (Phase != FakeRoutePhase.ChoosingRoute || _currentNode == null || string.IsNullOrEmpty(choiceId)) return false;
        if (_currentNode.outgoingChoices == null) return false;
        for (int i = 0; i < _currentNode.outgoingChoices.Count; i++)
        {
            var choice = _currentNode.outgoingChoices[i];
            if (choice != null && choice.choiceId == choiceId)
            {
                if (choice.targetNode == null || !ContainsNode(choice.targetNode)) return false;
                if (_visitedNodes.Contains(choice.targetNode))
                {
                    Debug.LogWarning("[FakeRoute] 禁止回访：目标节点已访问 " + choice.targetNode.nodeId);
                    return false;
                }
                SetPhase(FakeRoutePhase.FakeMoving);
                RouteChoicePanel.HideCurrent();
                _choiceHistory.Add(new FakeRouteChoiceSaveState
                {
                    sourceNodeId = _currentNode.nodeId,
                    choiceId = choice.choiceId,
                    targetNodeId = choice.targetNode.nodeId
                });
                SetGameplayInput(false);
                stageController.SetRouteTravelState();
                _flow = StartCoroutine(PlayPlaceholderAndEnter(choice, _generation));
                return true;
            }
        }
        return false;
    }

    private IEnumerator PlayPlaceholderAndEnter(FakeRouteChoiceConfig choice, int generation)
    {
        if (movementPresenter != null)
            yield return movementPresenter.PlayChoiceTravel(choice, _currentNode, choice.targetNode, () => generation == _generation && Phase == FakeRoutePhase.FakeMoving);
        else
        {
            // 没有表现层时无法播放旅行：报告并直接提交目标节点，不静默改变节点语义。
            Debug.LogError("[FakeRoute] 表现层缺失，无法播放出口旅行: " + (choice != null ? choice.choiceId : "null"));
        }
        if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;
        yield return EnterNode(choice.targetNode);
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

    public void SaveCheckpoint()
    {
        if (routeConfig == null || _currentNode == null || PlayerState.Instance == null) return;
        var snapshot = new FakeRouteStageSaveSnapshot
        {
            snapshotVersion = 1,
            routeArchitectureId = "fake-route-v1",
            routeId = routeConfig.routeId,
            stageId = routeConfig.stageId,
            configurationVersion = routeConfig.configurationVersion,
            checkpointNodeId = _currentNode.nodeId,
            currentHealth = PlayerState.Instance.currentHealth,
            currentRevives = PlayerState.Instance.currentRevives,
            currentLevel = PlayerState.Instance.currentLevel,
            currentExp = PlayerState.Instance.currentExp,
            killCount = PlayerState.Instance.killCount,
            coinCount = PlayerState.Instance.coinCount,
            ultimateEnergy = UltimateSystem.Instance != null ? UltimateSystem.Instance.CurrentEnergy : 0
        };
        snapshot.choiceHistory.Clear();
        snapshot.choiceHistory.AddRange(_choiceHistory);
        // 路线推进记录：到过的节点 + 每个节点已完成的 BattleEntry。
        snapshot.visitedNodeIds.Clear();
        snapshot.completedNodeIds.Clear();
        foreach (var node in _visitedNodes)
        {
            if (node == null) continue;
            if (!snapshot.visitedNodeIds.Contains(node.nodeId)) snapshot.visitedNodeIds.Add(node.nodeId);
            if (IsNodeContentCompleted(node) && !snapshot.completedNodeIds.Contains(node.nodeId))
                snapshot.completedNodeIds.Add(node.nodeId);
            if (!_completedBattleEntries.TryGetValue(node, out var completed) || completed.Count == 0) continue;
            var state = new FakeRouteNodeBattleSaveState { nodeId = node.nodeId };
            state.completedEntryIndices.AddRange(completed);
            snapshot.nodeStates.Add(state);
        }
        foreach (var upgrade in PlayerState.Instance.acquiredUpgrades)
        {
            if (upgrade != null && upgrade.definition != null)
                snapshot.upgrades.Add(new RouteUpgradeSaveState { upgradeId = upgrade.definition.upgradeId, level = upgrade.currentLevel });
        }
        if (ActiveSkillInventory.Instance != null)
        {
            foreach (var entry in ActiveSkillInventory.Instance.Entries)
            {
                if (entry != null && entry.definition != null)
                    snapshot.activeSkills.Add(new RouteActiveSkillSaveState { upgradeId = entry.definition.upgradeId, level = entry.level });
            }
        }
        SaveManager.SaveFakeRouteSnapshot(snapshot);
        SaveManager.SetActiveFakeRouteStage(routeConfig.stageId);
        Debug.Log("[FakeRoute] checkpoint saved node=" + _currentNode.nodeId);
    }

    private bool RestoreCheckpoint(FakeRouteStageSaveSnapshot snapshot, out FakeRouteNodeConfig restoredNode)
    {
        restoredNode = FindNodeById(snapshot != null ? snapshot.checkpointNodeId : null);
        if (snapshot == null || restoredNode == null || snapshot.snapshotVersion != 1 || snapshot.routeArchitectureId != "fake-route-v1" || snapshot.routeId != routeConfig.routeId || snapshot.stageId != routeConfig.stageId || snapshot.configurationVersion != routeConfig.configurationVersion)
            return false;

        var player = PlayerState.Instance;
        if (player == null) return false;
        player.currentHealth = snapshot.currentHealth;
        player.currentRevives = snapshot.currentRevives;
        player.currentLevel = snapshot.currentLevel;
        player.currentExp = snapshot.currentExp;
        player.killCount = snapshot.killCount;
        player.coinCount = snapshot.coinCount;
        player.acquiredUpgrades.Clear();
        _completedBattleEntries.Clear();
        _visitedNodes.Clear();
        _choiceHistory.Clear();
        if (snapshot.choiceHistory != null)
            _choiceHistory.AddRange(snapshot.choiceHistory);
        if (snapshot.visitedNodeIds != null)
        {
            for (int i = 0; i < snapshot.visitedNodeIds.Count; i++)
            {
                var node = FindNodeById(snapshot.visitedNodeIds[i]);
                if (node != null) _visitedNodes.Add(node);
            }
        }
        if (snapshot.nodeStates != null)
        {
            for (int i = 0; i < snapshot.nodeStates.Count; i++)
            {
                var savedNode = snapshot.nodeStates[i];
                var node = FindNodeById(savedNode != null ? savedNode.nodeId : null);
                if (node == null) continue;
                _visitedNodes.Add(node);
                if (savedNode.completedEntryIndices != null)
                    _completedBattleEntries[node] = new HashSet<int>(savedNode.completedEntryIndices);
            }
        }
        if (!ValidateChoiceHistory(snapshot.choiceHistory)) return false;
        TimedPassiveModule.Instance?.SetSuppressImmediateEffects(true);
        if (snapshot.upgrades != null && UpgradeEffectManager.Instance != null)
        {
            for (int i = 0; i < snapshot.upgrades.Count; i++)
            {
                var savedUpgrade = snapshot.upgrades[i];
                var definition = FindUpgradeDefinition(savedUpgrade.upgradeId);
                if (definition == null) continue;
                for (int level = 0; level < savedUpgrade.level; level++)
                    UpgradeEffectManager.Instance.ApplyUpgrade(definition);
            }
        }
        if (snapshot.activeSkills != null && ActiveSkillInventory.Instance != null)
        {
            for (int i = 0; i < snapshot.activeSkills.Count; i++)
            {
                var savedSkill = snapshot.activeSkills[i];
                var definition = FindActiveSkillDefinition(savedSkill.upgradeId);
                if (definition == null) continue;
                for (int level = 0; level < savedSkill.level; level++)
                    ActiveSkillInventory.Instance.AcquireOrUpgrade(definition, out _);
            }
            ActiveSkillInventory.Instance.ResetCooldowns();
        }
        TimedPassiveModule.Instance?.SetSuppressImmediateEffects(false);
        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.SetEnergyForRouteRestore(snapshot.ultimateEnergy);
        float maxHealth = player.heroConfig != null ? player.heroConfig.maxHealth : 100f;
        int requiredExp = player.GetExpRequiredForNextLevel();
        player.OnHealthChanged?.Invoke(player.currentHealth, maxHealth);
        player.OnReviveCountChanged?.Invoke(player.currentRevives);
        player.OnLevelChanged?.Invoke(player.currentLevel);
        player.OnExpChanged?.Invoke(player.currentExp, requiredExp > 0 ? requiredExp : player.currentExp);
        return true;
    }

    private FakeRouteNodeConfig FindNodeById(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || routeConfig == null || routeConfig.nodes == null) return null;
        for (int i = 0; i < routeConfig.nodes.Count; i++)
            if (routeConfig.nodes[i] != null && routeConfig.nodes[i].nodeId == nodeId)
                return routeConfig.nodes[i];
        return null;
    }

    private bool ValidateChoiceHistory(List<FakeRouteChoiceSaveState> history)
    {
        if (history == null) return true;
        for (int i = 0; i < history.Count; i++)
        {
            var item = history[i];
            var source = FindNodeById(item != null ? item.sourceNodeId : null);
            var target = FindNodeById(item != null ? item.targetNodeId : null);
            if (source == null || target == null || source.outgoingChoices == null) return false;
            bool matched = false;
            for (int c = 0; c < source.outgoingChoices.Count; c++)
            {
                var choice = source.outgoingChoices[c];
                if (choice != null && choice.choiceId == item.choiceId && choice.targetNode == target) { matched = true; break; }
            }
            if (!matched) return false;
        }
        return true;
    }

    private ActiveSkillDefinition FindActiveSkillDefinition(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;
        var all = Resources.FindObjectsOfTypeAll<ActiveSkillDefinition>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].upgradeId == upgradeId)
                return all[i];
        return null;
    }

    private UpgradeDefinition FindUpgradeDefinition(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;
        var all = Resources.FindObjectsOfTypeAll<UpgradeDefinition>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].upgradeId == upgradeId)
                return all[i];
        return null;
    }

    public void HandleStageDefeat()
    {
        _generation++;
        _battleActive = false;
        _battleCleared = false;
        SetGameplayInput(false);
        SetPhase(FakeRoutePhase.Defeated);
        if (_flow != null)
        {
            StopCoroutine(_flow);
            _flow = null;
        }
    }

    private IEnumerator FinishRoute()
    {
        if (_routeSettled) yield break;
        _routeSettled = true;
        SetGameplayInput(false);
        SetPhase(FakeRoutePhase.Completed);
        yield return new WaitForSecondsRealtime(0.5f);
        SaveManager.ClearFakeRouteSnapshot(routeConfig.routeId, routeConfig.stageId);
        stageController.CompleteFakeRouteStage(routeConfig.clearCoinReward);
    }

    private void SetPhase(FakeRoutePhase phase)
    {
        Phase = phase;
        if (phase != FakeRoutePhase.Battle) SetGameplayInput(false);
    }

    private void SetGameplayInput(bool enabled)
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.gameplayInputEnabled = enabled
                && PlayerState.Instance != null
                && PlayerState.Instance.stageState == StageState.InProgress;
            if (!enabled) InputManager.Instance.CancelCurrentGesture();
        }
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
        if (node == null || node.battleEntries == null) return true;
        for (int i = 0; i < node.battleEntries.Count; i++)
        {
            var entry = node.battleEntries[i];
            if (entry == null || entry.battleConfig == null || entry.conditionEnabled) continue;
            if (!IsBattleEntryCompleted(node, i)) return false;
        }
        return true;
    }

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

    private void ShowRouteChoice()
    {
        if (_currentNode == null || _currentNode.outgoingChoices == null) return;
        var options = new List<RouteChoiceOption>();
        for (int i = 0; i < _currentNode.outgoingChoices.Count; i++)
        {
            var choice = _currentNode.outgoingChoices[i];
            if (choice == null || choice.targetNode == null) continue;
            options.Add(new RouteChoiceOption
            {
                id = choice.choiceId,
                label = choice.displayName + " → " + choice.targetNode.displayName,
                layout = choice.layout
            });
        }
        RouteChoicePanel.Show(choicePanelPrefab, _currentNode.displayName, options, id => TrySelectChoice(id));
    }

}
