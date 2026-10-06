using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Single flow owner: opening -> combat -> rewards -> branch choice -> travel -> combat.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class BattleYRouteHost : MonoBehaviour
{
    public string routeSceneName = "Assets/Experiments/CurvedScroll/YJunctionSample.unity";
    public StageConfig leftBattleConfig;
    public StageConfig rightBattleConfig;
    public bool autoLoadRoute = true;
    public bool driveRouteInPlay;
    public bool startRightBranch;
    public bool showRouteChoice;
    public bool useSampleBattle = true;
    public StageController stage;
    public Camera battleCamera;
    public StageConfig openingBattleConfig;
    public float groundHeight = -1.8f;
    public float enemyFootGroundY = -1.8f;
    public bool applyEnemyVisualOffset = false;
    public float openingDistance = 18f;
    public float openingDuration = 8f;
    public float branchDuration = 8f;
    [Tooltip("Normalized time to traveled distance. Short acceleration, fast travel, long deceleration; endpoints are fixed by the mover.")]
    public AnimationCurve travelProgressCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.1f, 0.1f, 1.6f, 1.6f),
        new Keyframe(0.4f, 0.65f, 1f, 1f),
        new Keyframe(0.8f, 0.97f, 0.22f, 0.22f),
        new Keyframe(1f, 1f, 0f, 0f));
    public string Phase { get; private set; }
    readonly System.Collections.Generic.HashSet<int> groundedEnemies = new System.Collections.Generic.HashSet<int>();
    YScrollSample route;
    bool cleared, choiceLocked;
    void Awake()
    {
        if (!stage) stage=FindObjectOfType<StageController>();
        if(stage) stage.autoStartStage=false;
        // Stop legacy route owners before their Start methods schedule travel/spawning.
        foreach(var runtime in FindObjectsOfType<RouteStageRuntimeV2>()){runtime.StopAllCoroutines();runtime.enabled=false;}
        var old=GetComponent<BattleYVisualBridge>();if(old)old.enabled=false;
    }
    IEnumerator Start()
    {
        Phase="Loading";showRouteChoice=false;
        if(!autoLoadRoute)yield break;
        yield return null;
        var scene=SceneManager.GetSceneByPath(routeSceneName);
        if(!scene.IsValid()||!scene.isLoaded){var op=SceneManager.LoadSceneAsync(routeSceneName,LoadSceneMode.Additive);if(op==null){Phase="Load failed";yield break;}yield return op;scene=SceneManager.GetSceneByPath(routeSceneName);}
        foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<YScrollSample>(true);if(found){route=found;break;}}
        if(!route||!stage||!battleCamera||!openingBattleConfig||!leftBattleConfig||!rightBattleConfig){Phase="Missing bindings";Debug.LogError("[YRoute] Missing host bindings",this);yield break;}
        foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<Camera>(true))c.enabled=false;
        var reverse=route.GetComponent<YSampleBattleHost>();if(reverse){reverse.StopAllCoroutines();reverse.enabled=false;}
        route.battleHost=this;route.viewCamera=battleCamera;route.animate=false;route.progress=0;route.right=false;
        var p=route.transform.position;p.y=groundHeight;route.transform.position=p;
        foreach(var t in route.GetComponentsInChildren<Transform>(true))if(t.name=="Display Enemies - no combat")t.gameObject.SetActive(false);
        stage.OnRouteBattleCompleted+=OnCleared;
        stage.playerState.ResetPlayer();stage.SetRouteTravelState();SetInput(false);
        Phase="Opening travel";
        yield return MoveTo(openingDistance/route.Length,openingDuration);
        yield return Combat(openingBattleConfig);
        if(Phase=="Defeated")yield break;
        Phase="Choose route";showRouteChoice=true;
    }
    void OnCleared(){cleared=true;}
    bool Blocking()=> (UpgradeChoiceManager.Instance&&UpgradeChoiceManager.Instance.IsChoosing)||ItemDiscardPopup.IsShowing||(ExpGemManager.Instance&&ExpGemManager.Instance.IsCollecting);
    IEnumerator Combat(StageConfig config)
    {
        cleared=false;stage.waveSpawner.stageConfig=config;Phase="Battle";
        stage.StartRouteBattle(config);SetInput(true);
        while(!cleared&&stage.CurrentState!=StageState.Defeat)yield return null;
        SetInput(false);if(stage.CurrentState==StageState.Defeat){Phase="Defeated";yield break;}
        Phase="Waiting rewards";stage.SetRouteRewardWaitState();yield return null;
        while(Blocking())yield return null;
        stage.SetRouteTravelState();
    }
    IEnumerator MoveTo(float end,float seconds)
    {
        float origin = route.progress, elapsed = 0f, traveled = 0f;
        float duration = Mathf.Max(0.1f, seconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            float sample = travelProgressCurve != null && travelProgressCurve.length >= 2
                ? travelProgressCurve.Evaluate(time)
                : Mathf.SmoothStep(0f, 1f, time);
            // User tuning cannot reverse travel or overshoot the destination.
            traveled = Mathf.Max(traveled, Mathf.Clamp01(sample));
            route.progress = Mathf.Lerp(origin, end, traveled);
            yield return null;
        }
        route.progress = end;
    }
    public void ChooseBranch(bool right)
    {
        if(Phase!="Choose route"||choiceLocked||Blocking())return;
        choiceLocked=true;showRouteChoice=false;route.right=right;Phase="Branch travel";
        StartCoroutine(RunBranch(right));
    }
    IEnumerator RunBranch(bool right)
    {
        yield return MoveTo(1,branchDuration);
        yield return Combat(right?rightBattleConfig:leftBattleConfig);
        if(Phase!="Defeated")Phase="Completed (test only)";
    }
    // Legacy sample callback cannot start a second battle; this host alone owns arrival.
    public void StartBranchBattle(bool right) { }
    void SetInput(bool value){if(InputManager.Instance){InputManager.Instance.gameplayInputEnabled=value;if(!value)InputManager.Instance.CancelCurrentGesture();}}
    void OnDestroy(){if(stage)stage.OnRouteBattleCompleted-=OnCleared;}
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
        if(!Application.isPlaying)return;
        var old=GUI.matrix;float scale=Mathf.Max(1,Screen.width/540f);GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        GUILayout.BeginArea(new Rect(12,12,Screen.width/scale-24,130),GUI.skin.box);
        GUILayout.Label("Y路线 / "+Phase);
        if(Phase=="Choose route"){GUILayout.BeginHorizontal();if(GUILayout.Button("左路：山谷",GUILayout.Height(60)))ChooseBranch(false);if(GUILayout.Button("右路：营地",GUILayout.Height(60)))ChooseBranch(true);GUILayout.EndHorizontal();}
        GUILayout.EndArea();GUI.matrix=old;
    }
}
