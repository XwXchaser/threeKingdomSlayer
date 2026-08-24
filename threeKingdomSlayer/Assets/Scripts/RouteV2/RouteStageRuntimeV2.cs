using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class RouteStageRuntimeV2 : MonoBehaviour
{
    [SerializeField] private RouteStageConfigV2 routeConfig;
    [SerializeField] private RouteStageTargetsV2 battleTargets;
    [SerializeField] private StageController stageController;
    [SerializeField] private float moveDuration = 1.5f;
    [SerializeField] private float rotateDuration = 0.75f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool useTestStartNode;
    [SerializeField] private RouteNodeConfigV2 testStartNode;

    private RouteStageSceneEntryV2 _sceneEntry;
    private RouteNodeConfigV2 _currentNode;
    private RouteCombatNodeEntryV2 _currentEntry;
    private RouteBattleEntryV2 _currentBattle;
    private int _battleIndex;
    private bool _running;
    private bool _choosing;
    private bool _battleActive;
    private bool _finalizing;
    private bool _battleEntryCompleted;
    private bool _battleCleared;
    private bool _restoredFromCheckpoint;

    private readonly Dictionary<RouteNodeConfigV2, HashSet<int>> _completedBattleEntries = new Dictionary<RouteNodeConfigV2, HashSet<int>>();

    public RouteNodeConfigV2 CurrentNode => _currentNode;
    public bool IsChoosing => _choosing;

    private IEnumerator Start()
    {
        if (!autoStart) yield break;
        yield return null;
        if (stageController == null) stageController = StageController.Instance;
        if (routeConfig == null || battleTargets == null || stageController == null)
        {
            Debug.LogError("[RouteV2] 配置、BattleStageTargets或StageController为空");
            yield break;
        }
        stageController.OnRouteBattleCompleted += NotifyBattleCompleted;
        yield return StartRoute();
    }

    private void OnDestroy()
    {
        if (stageController != null)
            stageController.OnRouteBattleCompleted -= NotifyBattleCompleted;
    }

    public void Begin(RouteStageConfigV2 config)
    {
        routeConfig = config;
        Debug.Log("[RouteV2] Begin mode=" + (RouteStageV2Launch.StartFromCheckpoint ? "checkpoint" : "new game") + " stage=" + (config != null ? config.stageId.ToString() : "NULL"));
        if (stageController == null) stageController = StageController.Instance;
        if (battleTargets == null) battleTargets = FindObjectOfType<RouteStageTargetsV2>();
        if (stageController != null) stageController.OnRouteBattleCompleted += NotifyBattleCompleted;
        if (!isActiveAndEnabled) return;
        StartCoroutine(StartRoute());
    }

    private void SetGameplayInput(bool enabled)
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.gameplayInputEnabled = enabled && PlayerState.Instance != null && PlayerState.Instance.stageState == StageState.InProgress;
            if (!enabled)
                InputManager.Instance.CancelCurrentGesture();
        }
    }

    private void AlignAnchorToTarget(Transform anchor, Transform target)
    {
        if (anchor == null || target == null) return;
        var root = _sceneEntry.routeStageRoot;
        Vector3 anchorLocalPosition = root.InverseTransformPoint(anchor.position);
        Quaternion anchorLocalRotation = Quaternion.Inverse(root.rotation) * anchor.rotation;
        Quaternion targetRootRotation = target.rotation * Quaternion.Inverse(anchorLocalRotation);
        Vector3 targetRootPosition = target.position - targetRootRotation * anchorLocalPosition;
        root.SetPositionAndRotation(targetRootPosition, targetRootRotation);
    }

    private IEnumerator StartRoute()
    {
        if (_running) yield break;
        if (routeConfig == null)
        {
            Debug.LogError("[RouteV2] RouteStageConfigV2为空");
            yield break;
        }
        if (battleTargets == null)
            battleTargets = FindObjectOfType<RouteStageTargetsV2>();
        if (stageController == null)
            stageController = StageController.Instance;
        if (battleTargets == null || battleTargets.initialHeadTarget == null || battleTargets.combatTarget == null || battleTargets.tailTarget == null || stageController == null)
        {
            Debug.LogError("[RouteV2] BattleStageTargets或StageController未完成接线");
            yield break;
        }
        _running = true;
        var existingRouteScene = SceneManager.GetSceneByName(routeConfig.routeSceneName);
        if (existingRouteScene.IsValid() && existingRouteScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(existingRouteScene);
            yield return null;
        }
        var load = SceneManager.LoadSceneAsync(routeConfig.routeSceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError("[RouteV2] 路线场景加载请求失败: " + routeConfig.routeSceneName);
            yield break;
        }
        while (!load.isDone) yield return null;
        var routeScene = SceneManager.GetSceneByName(routeConfig.routeSceneName);
        _sceneEntry = null;
        if (routeScene.IsValid() && routeScene.isLoaded)
        {
            var roots = routeScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && _sceneEntry == null; i++)
                _sceneEntry = roots[i].GetComponentInChildren<RouteStageSceneEntryV2>(true);
        }
        if (_sceneEntry == null || _sceneEntry.routeStageRoot == null)
        {
            Debug.LogError("[RouteV2] RouteStageSceneEntry或RouteStageRoot缺失");
            yield break;
        }
        var startNode = useTestStartNode && testStartNode != null ? testStartNode : routeConfig.startNode;
        _completedBattleEntries.Clear();
        _restoredFromCheckpoint = false;
        if (!useTestStartNode)
        {
            var snapshot = RouteStageV2Launch.StartFromCheckpoint ? SaveManager.GetRouteStageSnapshot(routeConfig.stageId) : null;
            Debug.Log("[RouteV2] checkpoint lookup mode=" + (RouteStageV2Launch.StartFromCheckpoint ? "enabled" : "disabled") + " found=" + (snapshot != null));
            if (snapshot != null)
            {
                var savedNode = FindNodeById(snapshot.currentNodeId);
                if (savedNode != null)
                {
                    startNode = savedNode;
                    _restoredFromCheckpoint = true;
                    RestoreRouteCheckpoint(snapshot);
                }
                else
                {
                    Debug.LogError("[RouteV2] checkpoint node missing, reset to route start: " + snapshot.currentNodeId);
                    PlayerState.Instance?.ResetPlayer();
                }
            }
        }
        if (startNode == null || !_sceneEntry.TryGetNode(startNode, out _currentEntry))
        {
            Debug.LogError("[RouteV2] 起始节点没有对应场景绑定");
            yield break;
        }
        _currentNode = startNode;
        Debug.Log("[RouteV2] start node selected=" + _currentNode.nodeId + " source=" + (RouteStageV2Launch.StartFromCheckpoint ? "checkpoint-or-start" : "routeConfig.startNode"));
        if (_currentNode.savePoint && !_restoredFromCheckpoint)
        {
            Debug.Log("[RouteV2] savePoint Head reached node=" + _currentNode.nodeId);
            SaveRouteCheckpoint();
        }
        AlignAnchorToTarget(_currentEntry.headJunction, battleTargets.initialHeadTarget);
        SetGameplayInput(false);
        yield return MoveAnchorToTarget(_currentEntry.combatArea, battleTargets.combatTarget, _currentEntry.headJunction, _currentEntry.headToCombatPath);
        yield return RunBattleEntries();
        SetGameplayInput(false);
        stageController.SetRouteTravelState();
        yield return MoveAnchorToTarget(_currentEntry.tailJunction, battleTargets.tailTarget, _currentEntry.combatArea, _currentEntry.combatToTailPath);
        _running = false;
        _choosing = true;
        SetGameplayInput(false);
    }

    private RouteNodeConfigV2 FindNodeById(string nodeId)
    {
        if (routeConfig == null || routeConfig.combatNodes == null) return null;
        for (int i = 0; i < routeConfig.combatNodes.Count; i++)
        {
            var node = routeConfig.combatNodes[i];
            if (node != null && node.nodeId == nodeId) return node;
        }
        return null;
    }

    private void RestoreRouteCheckpoint(RouteStageSaveSnapshot snapshot)
    {
        var player = PlayerState.Instance;
        player?.ResetPlayer();
        UpgradeEffectManager.Instance?.ResetAll();
        ActiveSkillInventory.Instance?.ResetAll();
        ItemInventory.Instance?.ClearAll();
        if (player != null)
        {
            player.acquiredUpgrades.Clear();
            player.currentHealth = snapshot.currentHealth;
            player.currentRevives = snapshot.currentRevives;
            player.currentLevel = snapshot.currentLevel;
            player.currentExp = snapshot.currentExp;
            float maxHealth = player.heroConfig != null ? player.heroConfig.maxHealth : 100f;
            float requiredExp = player.GetExpRequiredForNextLevel();
            if (requiredExp < 0f) requiredExp = player.currentExp;
            player.OnExpChanged?.Invoke(player.currentExp, requiredExp);
            player.OnLevelChanged?.Invoke(player.currentLevel);
            player.OnHealthChanged?.Invoke(player.currentHealth, maxHealth);
            player.OnReviveCountChanged?.Invoke(player.currentRevives);
        }
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
        TimedPassiveModule.Instance?.SetSuppressImmediateEffects(false);
        if (snapshot.nodeStates == null) return;
        for (int i = 0; i < snapshot.nodeStates.Count; i++)
        {
            var savedNode = snapshot.nodeStates[i];
            var node = FindNodeById(savedNode.nodeId);
            if (node == null || savedNode.completedEntryIndices == null) continue;
            _completedBattleEntries[node] = new HashSet<int>(savedNode.completedEntryIndices);
        }
        Debug.Log("[RouteV2] checkpoint restored node=" + snapshot.currentNodeId + " upgrades=" + (player != null ? player.acquiredUpgrades.Count.ToString() : "NULL") + " savedUpgrades=" + (snapshot.upgrades != null ? snapshot.upgrades.Count.ToString() : "NULL"));
    }

    private UpgradeDefinition FindUpgradeDefinition(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;
        var all = Resources.FindObjectsOfTypeAll<UpgradeDefinition>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].upgradeId == upgradeId) return all[i];
        return null;
    }

    private IEnumerator RunBattleEntries()
    {
        if (_currentNode.battleEntries == null) yield break;
        for (_battleIndex = 0; _battleIndex < _currentNode.battleEntries.Count; _battleIndex++)
        {
            _currentBattle = _currentNode.battleEntries[_battleIndex];
            if (_currentBattle == null || _currentBattle.battleConfig == null)
            {
                Debug.LogWarning("[RouteV2] skip invalid BattleEntry node=" + _currentNode.nodeId + " index=" + _battleIndex);
                continue;
            }
            if (_currentBattle.conditionEnabled)
            {
                Debug.Log("[RouteV2] skip BattleEntry by condition node=" + _currentNode.nodeId + " index=" + _battleIndex);
                continue;
            }
            if (IsBattleEntryCompleted(_currentNode, _battleIndex))
            {
                Debug.Log("[RouteV2] skip completed BattleEntry node=" + _currentNode.nodeId + " index=" + _battleIndex);
                continue;
            }
            _battleActive = true;
            _battleCleared = false;
            _battleEntryCompleted = false;
            Debug.Log("[RouteV2] Start battle node=" + _currentNode.name + " index=" + _battleIndex + " config=" + _currentBattle.battleConfig.name);
            stageController.StartRouteBattle(_currentBattle.battleConfig);
            SetGameplayInput(true);
            while (!_battleCleared) yield return null;
            SetGameplayInput(false);
            stageController.SetRouteRewardWaitState();
            while ((UpgradeChoiceManager.Instance != null && UpgradeChoiceManager.Instance.IsChoosing)
                || ItemDiscardPopup.IsShowing
                || (ExpGemManager.Instance != null && ExpGemManager.Instance.IsCollecting))
                yield return null;
            _battleEntryCompleted = true;
            Debug.Log("[RouteV2] Battle rewards completed node=" + _currentNode.name + " index=" + _battleIndex);
            while (!_battleEntryCompleted) yield return null;
            Debug.Log("[RouteV2] Battle completed node=" + _currentNode.name + " index=" + _battleIndex);
            MarkBattleEntryCompleted(_currentNode, _battleIndex);
            _battleActive = false;
        }
    }

    private bool IsBattleEntryCompleted(RouteNodeConfigV2 node, int index)
    {
        return _completedBattleEntries.TryGetValue(node, out var completed) && completed.Contains(index);
    }

    private void MarkBattleEntryCompleted(RouteNodeConfigV2 node, int index)
    {
        if (!_completedBattleEntries.TryGetValue(node, out var completed))
        {
            completed = new HashSet<int>();
            _completedBattleEntries.Add(node, completed);
        }
        completed.Add(index);
    }

    public void SaveRouteCheckpoint()
    {
        if (routeConfig == null || _currentNode == null || PlayerState.Instance == null) return;
        var snapshot = new RouteStageSaveSnapshot
        {
            stageId = routeConfig.stageId,
            currentNodeId = _currentNode.nodeId,
            currentHealth = PlayerState.Instance.currentHealth,
            currentRevives = PlayerState.Instance.currentRevives,
            currentLevel = PlayerState.Instance.currentLevel,
            currentExp = PlayerState.Instance.currentExp
        };
        foreach (var upgrade in PlayerState.Instance.acquiredUpgrades)
        {
            if (upgrade != null && upgrade.definition != null)
                snapshot.upgrades.Add(new RouteUpgradeSaveState { upgradeId = upgrade.definition.upgradeId, level = upgrade.currentLevel });
        }
        foreach (var pair in _completedBattleEntries)
        {
            var state = new RouteNodeBattleSaveState { nodeId = pair.Key.nodeId };
            state.completedEntryIndices.AddRange(pair.Value);
            snapshot.nodeStates.Add(state);
        }
        SaveManager.SaveRouteStageSnapshot(snapshot);
        Debug.Log("[RouteV2] checkpoint saved node=" + _currentNode.nodeId);
    }

    public void CleanupRouteStage(bool unloadScene)
    {
        StopAllCoroutines();
        _running = false;
        _choosing = false;
        _battleActive = false;
        _battleCleared = false;
        _battleEntryCompleted = false;
        _currentNode = null;
        _currentEntry = null;
        _completedBattleEntries.Clear();
        SetGameplayInput(false);
        if (!unloadScene || routeConfig == null) return;
        var routeScene = SceneManager.GetSceneByName(routeConfig.routeSceneName);
        if (routeScene.IsValid() && routeScene.isLoaded)
            SceneManager.UnloadSceneAsync(routeScene);
        _sceneEntry = null;
    }

    public void HandleStageDefeat()
    {
        CleanupRouteStage(true);
    }

    public void NotifyBattleCompleted()
    {
        if (!_battleActive || _battleCleared) return;
        _battleCleared = true;
        Debug.Log("[RouteV2] Battle cleared, waiting for reward choices node=" + _currentNode.nodeId + " index=" + _battleIndex);
    }

    private IEnumerator MoveAnchorToTarget(Transform anchor, Transform target, Transform rotationPivot, Transform[] pathPoints = null)
    {
        if (anchor == null || target == null) yield break;
        var root = _sceneEntry.routeStageRoot;
        Vector3 anchorLocalPosition = root.InverseTransformPoint(anchor.position);
        Vector3 targetPosition = target.position;
        Quaternion anchorLocalRotation = Quaternion.Inverse(root.rotation) * anchor.rotation;
        Quaternion targetRootRotation = target.rotation * Quaternion.Inverse(anchorLocalRotation);
        Vector3 targetRootPosition = targetPosition - targetRootRotation * anchorLocalPosition;
        Quaternion startRotation = root.rotation;
        Vector3 startPosition = root.position;
        Vector3 pivotWorld = rotationPivot != null ? rotationPivot.position : anchor.position;
        float elapsed = 0f;
        while (elapsed < rotateDuration && Quaternion.Angle(root.rotation, targetRootRotation) > 0.05f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, rotateDuration));
            Quaternion rotation = Quaternion.Slerp(startRotation, targetRootRotation, t);
            root.rotation = rotation;
            root.position = pivotWorld + rotation * Quaternion.Inverse(startRotation) * (startPosition - pivotWorld);
            yield return null;
        }
        root.rotation = targetRootRotation;
        Vector3 finalPosition = targetRootPosition;
        elapsed = 0f;
        Vector3 moveStart = root.position;
        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            root.position = Vector3.Lerp(moveStart, finalPosition, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, moveDuration)));
            yield return null;
        }
        root.SetPositionAndRotation(finalPosition, targetRootRotation);
    }

    private Vector3[] SamplePath(Transform root, Transform[] pathPoints, Vector3 sourceAnchorWorld)
    {
        if (pathPoints == null || pathPoints.Length < 2) return null;
        var result = new Vector3[pathPoints.Length];
        Vector3 sourcePathWorld = pathPoints[0] != null ? pathPoints[0].position : sourceAnchorWorld;
        Vector3 offset = root.position - sourcePathWorld;
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null) return null;
            result[i] = pathPoints[i].position + offset;
        }
        return result;
    }

    private void OnGUI()
    {
        if (!_choosing || _currentNode == null || _currentNode.isFinalNode || _currentNode.outgoingConnections == null) return;
        SetGameplayInput(false);
        GUILayout.BeginArea(new Rect(20f, 20f, 360f, 240f), GUI.skin.box);
        GUILayout.Label("Tail: " + _currentNode.displayName);
        for (int i = 0; i < _currentNode.outgoingConnections.Count; i++)
        {
            var connection = _currentNode.outgoingConnections[i];
            if (connection == null || connection.targetNode == null) continue;
            if (GUILayout.Button(connection.choiceSlot + " -> " + connection.targetNode.displayName, GUILayout.Height(40f)))
            {
                Debug.Log("[RouteV2] route choice clicked: " + _currentNode.nodeId + " -> " + connection.targetNode.nodeId);
                stageController.SetRouteTravelState();
                StartCoroutine(TravelTo(connection));
            }
        }
        GUILayout.EndArea();
    }

    private IEnumerator FinishFinalNode()
    {
        if (_finalizing) yield break;
        _finalizing = true;
        _choosing = false;
        SetGameplayInput(false);
        Debug.Log("[RouteV2] Final node reached, completing route stage");
        yield return new WaitForSecondsRealtime(0.5f);
        stageController.CompleteRouteStage(routeConfig.clearCoinReward);
    }

    private IEnumerator TravelTo(RouteConnectionV2 connection)
    {
        if (!_choosing || connection == null || connection.targetNode == null) yield break;
        _choosing = false;
        SetGameplayInput(false);
        if (!_sceneEntry.TryGetConnection(_currentNode, connection.targetNode, out var binding))
        {
            Debug.LogError("[RouteV2] connection binding missing: " + _currentNode.nodeId + " -> " + connection.targetNode.nodeId);
            _choosing = true;
            SetGameplayInput(false);
            yield break;
        }
        Debug.Log("[RouteV2] traveling: " + _currentNode.nodeId + " -> " + connection.targetNode.nodeId + " binding=" + binding.name);
        if (!_sceneEntry.TryGetNode(connection.targetNode, out var targetEntry))
        {
            Debug.LogError("[RouteV2] target node entry missing: " + connection.targetNode.nodeId);
            _choosing = true;
            SetGameplayInput(false);
            yield break;
        }
        Debug.Log("[RouteV2] target entry found: " + targetEntry.name);
        yield return MoveAnchorToTarget(binding.targetHead, battleTargets.initialHeadTarget, binding.rotationPivot, binding.travelPath);
        Debug.Log("[RouteV2] connection arrived: " + _currentNode.nodeId + " -> " + connection.targetNode.nodeId);
        _currentNode = connection.targetNode;
        _currentEntry = targetEntry;
        if (_currentNode.savePoint && !_restoredFromCheckpoint)
        {
            Debug.Log("[RouteV2] savePoint Head reached node=" + _currentNode.nodeId);
            SaveRouteCheckpoint();
        }
        yield return MoveAnchorToTarget(_currentEntry.combatArea, battleTargets.combatTarget, _currentEntry.headJunction, _currentEntry.headToCombatPath);
        yield return RunBattleEntries();
        SetGameplayInput(false);
        yield return MoveAnchorToTarget(_currentEntry.tailJunction, battleTargets.tailTarget, _currentEntry.combatArea, _currentEntry.combatToTailPath);
        if (_currentNode.isFinalNode)
        {
            yield return FinishFinalNode();
            yield break;
        }
        _choosing = true;
        SetGameplayInput(false);
    }
}