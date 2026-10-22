using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Optional isolated host for the Y proof. Loads the real Battle scene only after route arrival.</summary>
[DisallowMultipleComponent]
public sealed class YSampleBattleHost : MonoBehaviour
{
    public YScrollSample route;
    public StageConfig battleConfig;
    public string battleSceneName = "Battle";
    public bool loadAtArrival = true;
    public bool disableSampleCameraOnBattle = true;
    bool triggered;
    Scene battleScene;
    void Reset() { route = GetComponent<YScrollSample>(); }
    void Awake() { if (!route) route = GetComponent<YScrollSample>(); }
    void Update()
    {
        if (!Application.isPlaying || !loadAtArrival || triggered || !route || route.progress < 0.999f) return;
        triggered = true;
        StartCoroutine(LoadBattleAndStart());
    }
    IEnumerator LoadBattleAndStart()
    {
        route.animate = false;
        if (disableSampleCameraOnBattle && route.viewCamera) route.viewCamera.enabled = false;
        var op = SceneManager.LoadSceneAsync(battleSceneName, LoadSceneMode.Additive);
        if (op == null) { Debug.LogError("[YSampleBattleHost] Battle scene load failed: " + battleSceneName, this); yield break; }
        while (!op.isDone) yield return null;
        battleScene = SceneManager.GetSceneByName(battleSceneName);
        // 2026-10-23：原先这里会关掉场景里的旧版路线驱动（RouteStageRuntimeV2）——
        // 但 Battle 场景早已不再挂那个组件（遗留实现已收进 Assets/Scripts/Legacy），
        // 这行是死代码；留着还会让 Y 分叉程序集反向依赖遗留程序集（成环），故删除。
        var stage = FindObjectOfType<StageController>();
        if (stage == null) { Debug.LogError("[YSampleBattleHost] StageController not found after Battle load", this); yield break; }
        if (battleConfig == null) { Debug.LogError("[YSampleBattleHost] battleConfig is not assigned", this); yield break; }
        stage.StartRouteBattle(battleConfig);
        Debug.Log("[YSampleBattleHost] Battle started at Y route arrival: " + battleConfig.name, this);
    }
}
