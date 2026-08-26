using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FakeRoutePhase
{
    None,
    EnteringNode,
    Battle,
    WaitingReward,
    ChoosingRoute,
    FakeMoving,
    Completed,
    Defeated
}

public sealed class FakeRouteRuntime : MonoBehaviour
{
    [SerializeField] private FakeRouteStageConfig routeConfig;
    [SerializeField] private StageController stageController;
    [SerializeField] private FakeMovementPresenter movementPresenter;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool useTestStartNode;
    [SerializeField] private FakeRouteNodeConfig testStartNode;
    [SerializeField] private bool showDebugChoicePanel = true;

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
    }

    private IEnumerator Start()
    {
        if (!autoStart) yield break;
        yield return null;
        Begin(routeConfig);
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
        string validationError;
        if (!ValidateConfiguration(out validationError))
        {
            Debug.LogError("[FakeRoute] 配置校验失败: " + validationError);
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
        if (node.savePoint && (!_restoredFromCheckpoint || node.nodeId != _restoredCheckpointNodeId))
            SaveCheckpoint();
        yield return RunBattleEntries();
        if (Phase == FakeRoutePhase.Defeated) yield break;

        if (node.isFinalNode)
        {
            yield return FinishRoute();
            yield break;
        }

        SetPhase(FakeRoutePhase.ChoosingRoute);
    }

    private IEnumerator RunBattleEntries()
    {
        if (_currentNode.battleEntries == null || _currentNode.battleEntries.Count == 0)
            yield break;

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
        }
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
                SetPhase(FakeRoutePhase.FakeMoving);
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
            yield return movementPresenter.Play(choice, () => generation == _generation && Phase == FakeRoutePhase.FakeMoving);
        else
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0f, choice.placeholderDuration);
            while (elapsed < duration && generation == _generation && Phase == FakeRoutePhase.FakeMoving)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        if (generation != _generation || Phase == FakeRoutePhase.Defeated) yield break;
        yield return EnterNode(choice.targetNode);
    }

    public bool ValidateConfiguration(out string error)
    {
        error = string.Empty;
        if (routeConfig == null) { error = "routeConfig为空"; return false; }
        if (string.IsNullOrEmpty(routeConfig.routeId)) { error = "routeId为空"; return false; }
        if (routeConfig.startNode == null) { error = "startNode为空"; return false; }
        if (routeConfig.nodes == null || routeConfig.nodes.Count == 0) { error = "nodes为空"; return false; }
        var nodeIds = new HashSet<string>();
        for (int i = 0; i < routeConfig.nodes.Count; i++)
        {
            var node = routeConfig.nodes[i];
            if (node == null) { error = "nodes包含空节点"; return false; }
            if (string.IsNullOrEmpty(node.nodeId) || !nodeIds.Add(node.nodeId)) { error = "nodeId为空或重复: " + node.nodeId; return false; }
            if (node.battleEntries == null) { error = "节点battleEntries为空: " + node.nodeId; return false; }
            for (int b = 0; b < node.battleEntries.Count; b++)
                if (node.battleEntries[b] != null && node.battleEntries[b].battleConfig == null) { error = "BattleEntry配置为空: " + node.nodeId + "#" + b; return false; }
            var choiceIds = new HashSet<string>();
            if (node.outgoingChoices == null) { error = "节点出口列表为空: " + node.nodeId; return false; }
            for (int c = 0; c < node.outgoingChoices.Count; c++)
            {
                var choice = node.outgoingChoices[c];
                if (choice == null || string.IsNullOrEmpty(choice.choiceId) || !choiceIds.Add(choice.choiceId) || choice.targetNode == null || !ContainsNode(choice.targetNode))
                { error = "路线选项无效: " + node.nodeId + "#" + c; return false; }
            }
            if (node.isFinalNode && node.outgoingChoices.Count > 0) { error = "终点节点不能有出口: " + node.nodeId; return false; }
            if (!node.isFinalNode && node.outgoingChoices.Count == 0) { error = "非终点节��没有出口: " + node.nodeId; return false; }
        }
        if (!ContainsNode(routeConfig.startNode)) { error = "startNode不在nodes列表中"; return false; }
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
            currentKillCount = PlayerState.Instance.killCount,
            currentCoinCount = PlayerState.Instance.coinCount,
            ultimateEnergy = UltimateSystem.Instance != null ? UltimateSystem.Instance.CurrentEnergy : 0
        };
        snapshot.choiceHistory.AddRange(_choiceHistory);
        foreach (var node in _visitedNodes)
        {
            if (node == null) continue;
            var state = new FakeRouteNodeSaveState { nodeId = node.nodeId, visited = true };
            if (_completedBattleEntries.TryGetValue(node, out var completed))
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
                    snapshot.activeSkills.Add(new FakeRouteActiveSkillSaveState { upgradeId = entry.definition.upgradeId, level = entry.level });
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
        player.killCount = snapshot.currentKillCount;
        player.coinCount = snapshot.currentCoinCount;
        player.acquiredUpgrades.Clear();
        _completedBattleEntries.Clear();
        _visitedNodes.Clear();
        _choiceHistory.Clear();
        if (snapshot.choiceHistory != null)
            _choiceHistory.AddRange(snapshot.choiceHistory);
        if (snapshot.nodeStates != null)
        {
            for (int i = 0; i < snapshot.nodeStates.Count; i++)
            {
                var savedNode = snapshot.nodeStates[i];
                var node = FindNodeById(savedNode != null ? savedNode.nodeId : null);
                if (node == null) continue;
                if (savedNode.visited) _visitedNodes.Add(node);
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

    private void OnGUI()
    {
        if (!showDebugChoicePanel || Phase != FakeRoutePhase.ChoosingRoute || _currentNode == null) return;
        GUILayout.BeginArea(new Rect(20f, 20f, 420f, 280f), GUI.skin.box);
        GUILayout.Label("Fake Route Node: " + _currentNode.displayName);
        if (_currentNode.outgoingChoices != null)
        {
            for (int i = 0; i < _currentNode.outgoingChoices.Count; i++)
            {
                var choice = _currentNode.outgoingChoices[i];
                if (choice == null || choice.targetNode == null) continue;
                if (GUILayout.Button(choice.displayName + " -> " + choice.targetNode.displayName, GUILayout.Height(45f)))
                    TrySelectChoice(choice.choiceId);
            }
        }
        GUILayout.EndArea();
    }
}
