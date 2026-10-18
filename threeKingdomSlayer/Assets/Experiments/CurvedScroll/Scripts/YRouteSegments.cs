using System.Collections.Generic;
using UnityEngine;

/// <summary>一个可搬运的路线段：从场景里搬出为 prefab，按沿路米数在节点停留期装回。</summary>
[System.Serializable]
public sealed class YRouteSegment
{
    [Tooltip("段 prefab（根 transform 的 localPosition 保持搬运前的值）。")]
    public GameObject prefab;
    [Tooltip("段起点沿路米数。")]
    public float startDistance;
    [Tooltip("段终点沿路米数。")]
    public float endDistance;
    [HideInInspector] public GameObject instance;
}

/// <summary>
/// 路线段的按需装载：玩家在节点停留时用有限预算把前方段实例化，离开后销毁。
/// 只在节点停留期装载（避免 3 秒移动期间出现实例化卡顿），并在进度跳变（检查点恢复）时兜底。
/// </summary>
[RequireComponent(typeof(YScrollSample))]
[DisallowMultipleComponent]
public sealed class YRouteSegments : MonoBehaviour
{
    public YScrollSample route;
    public YRouteStreamingWindow window;
    public List<YRouteSegment> segments = new List<YRouteSegment>();

    [Header("预载")]
    [Tooltip("停留时向前预载的距离（米）。<=0 表示自动：最大节点间距 + 渲染窗口 + 10。")]
    public float dwellPreloadLead = 0f;
    [Tooltip("节点停留期每帧最多实例化的段根数。")]
    [Min(1)] public int dwellBudgetPerFrame = 8;
    [Tooltip("非停留期（兜底）每帧最多实例化的段根数。")]
    [Min(1)] public int fallbackBudgetPerFrame = 1;
    [Tooltip("进度静止多少帧判定为节点停留。")]
    [Min(1)] public int dwellFrames = 12;

    [Header("调试")]
    public bool logSegments;

    float lastPosition = float.NaN;
    int stillFrames;
    float preloadTarget = float.NegativeInfinity;
    float autoLead;

    /// <summary>最大节点间距 + 渲染窗口 + 10：保证走完一整段路都不用临时装载。</summary>
    float ComputeAutoLead()
    {
        float renderAhead = window != null ? window.renderAhead : 105f;
        float maxGap = 0f;
        if (route != null && route.nodePresentations != null)
        {
            var distances = new List<float>();
            foreach (var p in route.nodePresentations) if (p != null) distances.Add(p.worldDistance);
            distances.Sort();
            for (int i = 1; i < distances.Count; i++) maxGap = Mathf.Max(maxGap, distances[i] - distances[i - 1]);
        }
        if (maxGap <= 0f) maxGap = 100f;
        return maxGap + renderAhead + 10f;
    }

    public int LoadedCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < segments.Count; i++) if (segments[i] != null && segments[i].instance != null) n++;
            return n;
        }
    }

    void Awake()
    {
        if (route == null) route = GetComponent<YScrollSample>();
        if (window == null) window = GetComponent<YRouteStreamingWindow>();
        autoLead = ComputeAutoLead();
    }

    void Update()
    {
        if (!Application.isPlaying || route == null) return;
        float position = route.progress * Mathf.Max(0.1f, route.Length);
        TrackDwell(position);
        Drive(position);
        lastPosition = position;
    }

    void OnDisable()
    {
        for (int i = 0; i < segments.Count; i++) Unload(segments[i]);
        preloadTarget = float.NegativeInfinity;
    }

    void TrackDwell(float position)
    {
        if (!float.IsNaN(lastPosition) && Mathf.Abs(position - lastPosition) < 0.05f) stillFrames++;
        else stillFrames = 0;
        if (stillFrames == dwellFrames)
            preloadTarget = position + EffectiveLead();
    }

    float EffectiveLead() => dwellPreloadLead > 0f ? dwellPreloadLead : autoLead;

    /// <summary>显式预载（供流程在节点停留时调用）。幂等。</summary>
    public void PreloadAround(float position)
    {
        preloadTarget = Mathf.Max(preloadTarget, position + EffectiveLead());
    }

    void Drive(float position)
    {
        float renderAhead = window != null ? window.renderAhead : 105f;
        float behind = window != null ? window.residentBehind : 30f;
        bool preloading = position <= preloadTarget;
        int budget = preloading ? dwellBudgetPerFrame : fallbackBudgetPerFrame;
        int startBudget = budget;
        // 预载时按绝对目标距离装载；否则只补渲染窗口内的缺口（检查点跳变兜底）
        float limit = preloading ? Mathf.Max(preloadTarget, position + renderAhead) : position + renderAhead;

        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment == null || segment.prefab == null) continue;
            if (segment.instance != null)
            {
                if (position - segment.endDistance > behind) Unload(segment);
                continue;
            }
            if (budget <= 0) continue;
            if (position <= segment.endDistance + behind && segment.startDistance <= limit)
            {
                Load(segment, position);
                budget--;
            }
        }

        if (preloading && budget == startBudget) preloadTarget = float.NegativeInfinity;
    }

    void Load(YRouteSegment segment, float position)
    {
        var instance = Instantiate(segment.prefab, transform, false);
        instance.name = segment.prefab.name + " (Runtime)";
        segment.instance = instance;
        if (window != null) window.RegisterSegment(instance);
        if (logSegments)
            Debug.Log($"[YSegment] load '{segment.prefab.name}' at pos={position:F1} range=[{segment.startDistance:F0},{segment.endDistance:F0}]");
    }

    void Unload(YRouteSegment segment)
    {
        if (segment == null || segment.instance == null) return;
        if (window != null) window.UnregisterSegment(segment.instance);
        if (logSegments) Debug.Log($"[YSegment] unload '{segment.prefab.name}'");
        Destroy(segment.instance);
        segment.instance = null;
    }
}
