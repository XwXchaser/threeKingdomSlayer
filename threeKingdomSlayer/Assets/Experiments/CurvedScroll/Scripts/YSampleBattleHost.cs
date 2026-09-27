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
        foreach (var routeRuntime in FindObjectsOfType<RouteStageRuntimeV2>()) routeRuntime.enabled = false;
        var stage = FindObjectOfType<StageController>();
        if (stage == null) { Debug.LogError("[YSampleBattleHost] StageController not found after Battle load", this); yield break; }
        if (battleConfig == null) { Debug.LogError("[YSampleBattleHost] battleConfig is not assigned", this); yield break; }
        stage.StartRouteBattle(battleConfig);
        Debug.Log("[YSampleBattleHost] Battle started at Y route arrival: " + battleConfig.name, this);
    }
}
