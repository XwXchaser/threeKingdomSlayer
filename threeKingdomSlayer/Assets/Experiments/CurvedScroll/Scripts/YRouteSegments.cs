using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个可搬运的路线段：从场景里搬出为 prefab，按沿路米数在节点停留期装回。
/// </summary>
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
///
/// ★ **按圈复制**（2026-11-19 无限循环）：路径的**形状每圈重复、位置逐圈递进**
///   （见 <see cref="YScrollSample.GetLapOffset"/>），所以同一批段 prefab 在第 k 圈
///   要再出现一份，摆在「本圈位置 + k × 圈偏移」处。
///   ⇒ 走到本圈末尾 → 下一圈同一批段自动接上 → **地图可以一直往前展开**。
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

    /// <summary>运行时展开的段（同一份 prefab 在不同圈各有一份实例）。</summary>
    private sealed class LiveSegment
    {
        public YRouteSegment source;
        public int lap;
        public float startDistance;
        public float endDistance;
        public GameObject instance;
    }

    private readonly List<LiveSegment> _live = new List<LiveSegment>();
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
            for (int i = 0; i < _live.Count; i++) if (_live[i] != null && _live[i].instance != null) n++;
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
        for (int i = _live.Count - 1; i >= 0; i--) Unload(_live[i]);
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

        // ★ 圈长 = 当前的一圈 flat 长度（无限卷轴开启时 = 切片序列总长；关闭时 = 路径形状长度）
        float lap = route != null ? Mathf.Max(0.1f, route.Length) : 1f;

        // ① 已加载的：出了窗口就卸
        for (int i = _live.Count - 1; i >= 0; i--)
        {
            var live = _live[i];
            if (live == null || live.instance == null) { _live.RemoveAt(i); continue; }
            if (position - live.endDistance > behind) Unload(live);
        }

        // ② 该加载的：**按圈展开**（同一批段 prefab 在每圈各有一份）
        //    ⚠ 圈号范围要**按每个段自己算**（2026-11-19 修）：
        //       用全局 position 反推会 off-by-one —— 段在第 k 圈的副本区间是
        //       [startDistance + k×一圈, endDistance + k×一圈]，
        //       落在窗口内的 k 取决于**这个段自己的** start/end，不是 position 所在的那一圈。
        for (int i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            if (segment == null || segment.prefab == null) continue;
            int kMin = route != null && route.infiniteScroll
                ? Mathf.Max(0, Mathf.FloorToInt((position - behind - segment.endDistance) / lap))
                : 0;
            int kMax = route != null && route.infiniteScroll
                ? Mathf.FloorToInt((limit - segment.startDistance) / lap)
                : 0;
            for (int l = kMin; l <= kMax; l++)
            {
                float start = segment.startDistance + l * lap;
                float end = segment.endDistance + l * lap;
                if (start > limit) continue;
                if (end < position - behind) continue;
                if (budget <= 0) break;
                if (IsLive(segment, l)) continue;
                Load(segment, l, start, end);
                budget--;
            }
        }

        if (preloading && budget == startBudget) preloadTarget = float.NegativeInfinity;
    }

    bool IsLive(YRouteSegment segment, int lap)
    {
        for (int i = 0; i < _live.Count; i++)
        {
            var live = _live[i];
            if (live != null && live.source == segment && live.lap == lap && live.instance != null) return true;
        }
        return false;
    }

    void Load(YRouteSegment segment, int lap, float start, float end)
    {
        var instance = Instantiate(segment.prefab, transform, false);
        instance.name = segment.prefab.name + " (Runtime)";
        // ★ **先注册再套帧**：注册时实例还在「未套帧」的原始局部位置，
        //   而窗口需要的就是那个**局部**沿路距离。
        //   ⚠ 反过来做会踩坑：刚设完根 transform 的 localPosition，
        //   同一帧里读子 Renderer.transform.position 可能拿到**旧值**
        //   ⇒ 所有子物件都被登记到距离 0 ⇒ 窗口判定全错。
        var live = new LiveSegment
        {
            source = segment,
            lap = lap,
            startDistance = start,
            endDistance = end,
            instance = instance
        };
        _live.Add(live);
        segment.instance = instance;   // 兼容旧字段
        if (window != null) window.RegisterSegment(instance, lap);

        // ★ 按圈摆放 = **刚体帧**（位置 + 朝向），不是只沿 +z 平移：
        //   转向是「地图在转」⇒ 第 k 圈的内容必须带着本圈累积的旋转一起摆
        //   ⇒ 接缝处内容与相机同时跨一步 ⇒ 位置/朝向都连续。
        if (route != null && lap != 0)
        {
            var frame = route.GetLapFrame(lap);
            var t = instance.transform;
            t.localPosition = frame.Apply(t.localPosition);
            t.localRotation = frame.Rotation * t.localRotation;
        }
        if (logSegments)
            Debug.Log($"[YSegment] load '{segment.prefab.name}' lap={lap} range=[{start:F0},{end:F0}]");
    }

    void Unload(LiveSegment live)
    {
        if (live == null || live.instance == null) return;
        if (window != null) window.UnregisterSegment(live.instance);
        if (logSegments) Debug.Log($"[YSegment] unload '{live.source?.prefab?.name}' lap={live.lap}");
        Destroy(live.instance);
        live.instance = null;
        if (live.source != null && live.source.instance == live.instance) live.source.instance = null;
        _live.Remove(live);
    }
}
