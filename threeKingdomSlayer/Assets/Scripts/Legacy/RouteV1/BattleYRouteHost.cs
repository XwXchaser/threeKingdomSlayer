using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Runs the Inspector-authored N1 -> E0 -> J1 route and its preview branches.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class BattleYRouteHost : MonoBehaviour
{
    public string routeSceneName = "Assets/Experiments/CurvedScroll/YJunctionSample.unity";
    public bool autoLoadRoute = true;
    [Header("Logical nodes and branch directions")]
    public FakeRouteStageConfig routeConfig;
    public FakeRouteChoiceConfig leftBranchChoice;
    public FakeRouteChoiceConfig rightBranchChoice;
    public StageController stage;
    public Camera battleCamera;
    public float groundHeight = -1.8f;
    public float enemyFootGroundY = -1.8f;
    public bool applyEnemyVisualOffset = false;
    public float openingDuration = 3f;
    [Tooltip("Fallback travel curve when a connection does not supply its own curve.")]
    public AnimationCurve travelProgressCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.1f, 0.1f, 1.6f, 1.6f),
        new Keyframe(0.4f, 0.65f, 1f, 1f),
        new Keyframe(0.8f, 0.97f, 0.22f, 0.22f),
        new Keyframe(1f, 1f, 0f, 0f));

    public string Phase { get; private set; }
    public FakeRouteNodeConfig CurrentNode { get; private set; }
    public bool IsWaitingForExit { get; private set; }
    public bool showRouteChoice { get; private set; }

    readonly HashSet<int> groundedEnemies = new HashSet<int>();
    readonly HashSet<FakeRouteNodeConfig> visited = new HashSet<FakeRouteNodeConfig>();
    YScrollSample route;
    bool cleared, choiceLocked;

    void Awake()
    {
        if (!stage) stage = FindObjectOfType<StageController>();
        if (stage) stage.autoStartStage = false;
        foreach (var runtime in FindObjectsOfType<RouteStageRuntimeV2>())
        {
            runtime.StopAllCoroutines();
            runtime.enabled = false;
        }
        var old = GetComponent<BattleYVisualBridge>();
        if (old) old.enabled = false;
    }

    IEnumerator Start()
    {
        Phase = "Loading";
        if (!autoLoadRoute) yield break;
        yield return null;
        var scene = SceneManager.GetSceneByPath(routeSceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(routeSceneName, LoadSceneMode.Additive);
            if (op == null) { Phase = "Load failed"; yield break; }
            yield return op;
            scene = SceneManager.GetSceneByPath(routeSceneName);
        }
        foreach (var root in scene.GetRootGameObjects())
        {
            route = root.GetComponentInChildren<YScrollSample>(true);
            if (route) break;
        }
        if (!ValidateBindings(out var error))
        {
            Phase = "Missing bindings";
            Debug.LogError("[YRoute] " + error, this);
            yield break;
        }
        foreach (var root in scene.GetRootGameObjects())
            foreach (var camera in root.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
        var reverse = route.GetComponent<YSampleBattleHost>();
        if (reverse) { reverse.StopAllCoroutines(); reverse.enabled = false; }
        route.battleHost = this;
        route.viewCamera = battleCamera;
        route.animate = false;
        route.progress = 0f;
        route.right = false;
        SetAllBranchVisuals(false);
        SetBranchGroup(route.junctionVisuals, false);
        var position = route.transform.position;
        position.y = groundHeight;
        route.transform.position = position;
        foreach (var t in route.GetComponentsInChildren<Transform>(true))
            if (t.name == "Display Enemies - no combat") t.gameObject.SetActive(false);
        stage.OnRouteBattleCompleted += OnCleared;
        stage.playerState.ResetPlayer();
        stage.SetRouteTravelState();
        SetInput(false);
        route.TryGetNodeProgress(routeConfig.startNode, out var start);
        Phase = "Opening travel";
        yield return MoveTo(start, openingDuration, travelProgressCurve);
        yield return EnterNode(routeConfig.startNode);
    }

    bool ValidateBindings(out string error)
    {
        error = "Route, StageController, player or camera binding is missing";
        if (!route || !stage || !stage.playerState || !stage.waveSpawner || !battleCamera) return false;
        if (!FakeRouteGraphValidator.TryValidate(routeConfig, out error)) return false;
        if (!leftBranchChoice || !rightBranchChoice || leftBranchChoice == rightBranchChoice)
        {
            error = "Left/right choices must be assigned separately";
            return false;
        }
        bool hasBranch = false;
        foreach (var node in routeConfig.nodes)
        {
            if (!route.TryGetNodeProgress(node, out _))
            {
                error = "Missing or ambiguous scene node anchor: " + node.nodeId;
                return false;
            }
            if (node.dialogues.Count != 0 || node.rewards.Count != 0)
            {
                error = "This scroll preview only supports battle content and exit confirmation: " + node.nodeId;
                return false;
            }
            foreach (var entry in node.battleEntries)
                if (entry == null || !entry.battleConfig || entry.conditionEnabled)
                {
                    error = "Invalid or conditional battle entry: " + node.nodeId;
                    return false;
                }
            if (node.outgoingChoices.Count > 1)
            {
                if (node.outgoingChoices.Count != 2 || !node.outgoingChoices.Contains(leftBranchChoice)
                    || !node.outgoingChoices.Contains(rightBranchChoice))
                {
                    error = "Branch exits must match the Inspector left/right choices: " + node.nodeId;
                    return false;
                }
                hasBranch = true;
            }
            if (node.terminalPolicy == FakeRouteTerminalPolicy.None && node.outgoingChoices.Count == 0)
            {
                error = "Nonterminal node has no exit: " + node.nodeId;
                return false;
            }
        }
        if (!hasBranch) { error = "No branch node references the configured left/right exits"; return false; }
        error = string.Empty;
        return true;
    }

    IEnumerator EnterNode(FakeRouteNodeConfig node)
    {
        CurrentNode = node;
        visited.Add(node);
        choiceLocked = false;
        IsWaitingForExit = false;
        showRouteChoice = false;
        SetInput(false);
        foreach (var entry in node.battleEntries)
        {
            yield return Combat(entry.battleConfig);
            if (Phase == "Defeated") yield break;
        }
        if (node.terminalPolicy != FakeRouteTerminalPolicy.None)
        {
            stage.SetRouteTravelState();
            Phase = "Completed (test only)";
            yield break;
        }
        // Keep reward callbacks alive until the player confirms departure.
        IsWaitingForExit = true;
        showRouteChoice = node.outgoingChoices.Count > 1;
        Phase = showRouteChoice ? "Choose route" : "Confirm exit";
        if (showRouteChoice) SetBranchGroup(route.junctionVisuals, true);
    }

    void OnCleared() { cleared = true; }

    bool Blocking() => Time.timeScale <= 0f
        || (DialogueManager.Instance && DialogueManager.Instance.IsInteractionBlocked)
        || (UpgradeChoiceManager.Instance && UpgradeChoiceManager.Instance.IsChoosing)
        || ItemDiscardPopup.IsShowing
        || (ExpGemManager.Instance && ExpGemManager.Instance.IsCollecting);

    IEnumerator Combat(StageConfig config)
    {
        cleared = false;
        stage.waveSpawner.stageConfig = config;
        Phase = "Battle";
        stage.StartRouteBattle(config);
        SetInput(true);
        while (!cleared && stage.CurrentState != StageState.Defeat) yield return null;
        SetInput(false);
        if (stage.CurrentState == StageState.Defeat) { Phase = "Defeated"; yield break; }
        Phase = "Waiting rewards";
        stage.SetRouteRewardWaitState();
        // Death animation callbacks can create late rewards; do not clean them up before they run.
        var deaths = FindObjectsOfType<Enemy>();
        int settledFrames = 0;
        while (settledFrames < 2)
        {
            yield return null;
            bool waiting = Blocking();
            foreach (var enemy in deaths)
                if (enemy && enemy.gameObject.activeInHierarchy && enemy.state == EnemyState.Dead) waiting = true;
            settledFrames = waiting ? 0 : settledFrames + 1;
            if (stage.CurrentState == StageState.Defeat) { Phase = "Defeated"; yield break; }
        }
    }

    public bool ConfirmSingleExit()
    {
        if (!CurrentNode || CurrentNode.outgoingChoices.Count != 1) return false;
        return Choose(CurrentNode.outgoingChoices[0]);
    }

    public void ChooseBranch(bool right)
    {
        Choose(right ? rightBranchChoice : leftBranchChoice);
    }

    public bool Choose(FakeRouteChoiceConfig choice)
    {
        if (!IsWaitingForExit || choiceLocked || Blocking() || !CurrentNode || !choice
            || !CurrentNode.outgoingChoices.Contains(choice) || visited.Contains(choice.targetNode)
            || !route.TryGetNodeProgress(choice.targetNode, out var end) || end < route.progress)
            return false;
        choiceLocked = true;
        IsWaitingForExit = false;
        showRouteChoice = false;
        if (choice == leftBranchChoice || choice == rightBranchChoice)
        {
            route.right = choice == rightBranchChoice;
            SetAllBranchVisuals(false);
            SetBranchGroup(route.right ? route.rightBranchVisuals : route.leftBranchVisuals, true);
            SetBranchGroup(route.right ? route.rightBranchEncounter : route.leftBranchEncounter, true);
        }
        SetInput(false);
        stage.SetRouteTravelState();
        Phase = CurrentNode.nodeId + " travel";
        StartCoroutine(Travel(choice, end));
        return true;
    }

    IEnumerator Travel(FakeRouteChoiceConfig choice, float end)
    {
        yield return MoveTo(end, choice.presentation.duration, choice.presentation.speedCurve);
        yield return EnterNode(choice.targetNode);
    }

    IEnumerator MoveTo(float end, float seconds, AnimationCurve curve)
    {
        float origin = route.progress, elapsed = 0f, traveled = 0f;
        float duration = Mathf.Max(0.1f, seconds);
        while (elapsed < duration)
        {
            if (Blocking()) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float sample = curve != null && curve.length >= 2
                ? curve.Evaluate(time) : Mathf.SmoothStep(0f, 1f, time);
            traveled = Mathf.Max(traveled, Mathf.Clamp01(sample));
            route.progress = Mathf.Lerp(origin, end, traveled);
            yield return null;
        }
        route.progress = end;
    }

    // A legacy isolated sample callback must never start a second battle.
    public void StartBranchBattle(bool right) { }

    void SetAllBranchVisuals(bool active)
    {
        if (!route) return;
        SetBranchGroup(route.leftBranchVisuals, active);
        SetBranchGroup(route.leftBranchEncounter, active);
        SetBranchGroup(route.rightBranchVisuals, active);
        SetBranchGroup(route.rightBranchEncounter, active);
    }

    void SetBranchGroup(Transform group, bool active)
    {
        if (!group) return;
        group.gameObject.SetActive(active);
        foreach (var child in group.GetComponentsInChildren<Transform>(true))
            if (child != group) child.gameObject.SetActive(active && child.name != "Display Enemies - no combat");
    }

    void SetInput(bool value)
    {
        if (!InputManager.Instance) return;
        InputManager.Instance.gameplayInputEnabled = value;
        if (!value) InputManager.Instance.CancelCurrentGesture();
    }

    void OnDestroy() { if (stage) stage.OnRouteBattleCompleted -= OnCleared; }

    void Update()
    {
        if (!applyEnemyVisualOffset || !Application.isPlaying || stage == null || stage.CurrentState != StageState.InProgress) return;
        foreach (var enemy in FindObjectsOfType<Enemy>())
        {
            if (!enemy || groundedEnemies.Contains(enemy.GetInstanceID())) continue;
            var renderer = enemy.GetComponentInChildren<SpriteRenderer>();
            if (!renderer) continue;
            float localBottom = renderer.bounds.min.y - enemy.transform.position.y;
            enemy.visualYOffset += enemyFootGroundY - (enemy.transform.position.y + localBottom);
            groundedEnemies.Add(enemy.GetInstanceID());
        }
    }

    void OnGUI()
    {
        if (!Application.isPlaying) return;
        var old = GUI.matrix;
        var enabled = GUI.enabled;
        float scale = Mathf.Max(1f, Screen.width / 540f);
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        GUILayout.BeginArea(new Rect(12, 12, Screen.width / scale - 24, 150), GUI.skin.box);
        GUILayout.Label("Y路线 / " + (CurrentNode ? CurrentNode.displayName : "") + " / " + Phase);
        if (Phase == "Waiting rewards") GUILayout.Label("请先完成奖励选择与经验收集");
        GUI.enabled = enabled && !Blocking();
        if (IsWaitingForExit && CurrentNode)
        {
            if (CurrentNode.outgoingChoices.Count == 1)
            {
                if (GUILayout.Button(CurrentNode.outgoingChoices[0].displayName, GUILayout.Height(60))) ConfirmSingleExit();
            }
            else if (showRouteChoice)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(leftBranchChoice.displayName, GUILayout.Height(60))) ChooseBranch(false);
                if (GUILayout.Button(rightBranchChoice.displayName, GUILayout.Height(60))) ChooseBranch(true);
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndArea();
        GUI.enabled = enabled;
        GUI.matrix = old;
    }
}
