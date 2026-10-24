using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 按沿路米数做距离窗口激活：只有 [pos - residentBehind, pos + residentAhead] 内的布景参与渲染。
/// 只切换 Renderer.enabled，不改任何物体的世界坐标、层级或作者态 activeSelf。
/// 窗口参数来源见 plan/scroll-on-demand-loading-plan.md：residentAhead = max(淡出距离 + 40, 最大节点间距 + 20)。
/// </summary>
[RequireComponent(typeof(YScrollSample))]
[DisallowMultipleComponent]
public sealed class YRouteStreamingWindow : MonoBehaviour
{
    [Tooltip("留空则自动取同物体上的 YScrollSample。")]
    public YScrollSample route;

    [Header("常驻内容（永不参与窗口）")]
    [Tooltip("地形、天空盒、远景地标、节点锚点与纯逻辑载体。")]
    public Transform[] alwaysResidentRoots = new Transform[0];

    [Header("渲染窗口（米）")]
    [Tooltip("只影响 Renderer.enabled。实测安全边界约 [-20, +92]，默认留余量。")]
    public bool enableWindow = true;
    public float renderAhead = 105f;
    public float renderBehind = 28f;

    [Header("常驻窗口（米）")]
    [Tooltip("必须覆盖一次节点间移动（最大间距 + 20），供按需实例化的段使用。必须 >= 渲染窗口。")]
    public float residentAhead = 120f;
    public float residentBehind = 30f;

    [Header("调试")]
    public bool logWindow;

    const float CoarseStep = 4f;
    const float RefineStep = 0.2f;
    const float ApplyThreshold = 0.5f;

    Vector2[] pathSamples;
    int samplesPerBranch;
    readonly List<Renderer> tracked = new List<Renderer>();
    readonly List<float> trackedDistance = new List<float>();
    readonly List<bool> authoredEnabled = new List<bool>();
    readonly List<bool> appliedEnabled = new List<bool>();
    readonly List<Renderer> registeredScenery = new List<Renderer>();
    bool restored = true;
    float lastPosition = float.NaN;

    public int SubmittedCount { get; private set; }
    public float WindowStart { get; private set; }
    public float WindowEnd { get; private set; }
    public float ResidentStart => WindowStart - Mathf.Max(0f, residentBehind - renderBehind);
    public float ResidentEnd => WindowEnd + Mathf.Max(0f, residentAhead - renderAhead);
    public int TrackedCount => tracked.Count;
    public bool Ready => tracked.Count > 0;

    void Awake()
    {
        if (route == null) route = GetComponent<YScrollSample>();
        Sanitize();
        if (!Application.isPlaying) return;
        BuildTable();
    }

    /// <summary>序列化默认值可能因字段新增而为 0，这里做一次保守回退，避免窗口被误关成空集。</summary>
    void Sanitize()
    {
        if (renderAhead <= 0f) renderAhead = Mathf.Max(residentAhead, 105f);
        if (renderBehind <= 0f) renderBehind = Mathf.Max(residentBehind, 28f);
        residentAhead = Mathf.Max(residentAhead, renderAhead);
        residentBehind = Mathf.Max(residentBehind, renderBehind);
    }

    void Update()
    {
        if (!Application.isPlaying || !Ready) return;
        if (!enableWindow)
        {
            if (!restored) RestoreAll();
            return;
        }
        float position = CurrentPosition();
        if (!float.IsNaN(lastPosition) && Mathf.Abs(position - lastPosition) < ApplyThreshold) return;
        lastPosition = position;
        ApplyWindow(position);
    }

    void OnDisable() { RestoreAll(); }

    void OnDestroy() { RestoreAll(); }

    float CurrentPosition() => route.progress * Mathf.Max(0.1f, route.Length);

    void BuildTable()
    {
        BuildPath();
        tracked.Clear();
        trackedDistance.Clear();
        authoredEnabled.Clear();
        appliedEnabled.Clear();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (IsAlwaysResident(renderer.transform)) continue;
            // 玩法物件（可破坏补给等）由 RoutePropFollower 独占可见性：它按投影后的路线坐标
            // 自行隐藏/显示。两套系统同时写 renderer.enabled 会让「已破坏的物件复活成残骸」。
            if (IsFollowerOwned(renderer)) continue;
            AddEntry(renderer);
        }
        restored = false;
        lastPosition = CurrentPosition();
        ApplyWindow(lastPosition);
        if (logWindow)
            Debug.Log($"[YStream] table built: tracked={tracked.Count} submitted={SubmittedCount} pos={lastPosition:F1}");
    }

    void AddEntry(Renderer renderer)
    {
        var p = renderer.transform.position;
        tracked.Add(renderer);
        trackedDistance.Add(ProjectDistance(new Vector2(p.x, p.z)));
        authoredEnabled.Add(renderer.enabled);
        appliedEnabled.Add(true);
    }

    /// <summary>按需实例化的段装载后调用：加入距离表和 scenery 的 bounds 表，并按当前窗口立即生效。</summary>
    public void RegisterSegment(GameObject segmentRoot)
    {
        if (segmentRoot == null) return;
        var renderers = segmentRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (IsFollowerOwned(renderer)) continue;
            if (tracked.Contains(renderer)) continue;
            AddEntry(renderer);
            registeredScenery.Add(renderer);
        }
        SyncScenery();
        ApplyWindow(float.IsNaN(lastPosition) ? CurrentPosition() : lastPosition);
    }

    /// <summary>段卸载前调用：从距离表与 scenery 的 bounds 表里移除。</summary>
    public void UnregisterSegment(GameObject segmentRoot)
    {
        if (segmentRoot == null) return;
        var renderers = segmentRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = tracked.Count - 1; i >= 0; i--)
        {
            if (!renderers.Contains(tracked[i])) continue;
            tracked.RemoveAt(i);
            trackedDistance.RemoveAt(i);
            authoredEnabled.RemoveAt(i);
            appliedEnabled.RemoveAt(i);
        }
        // 引用还活着的时候直接从 scenery 里摘掉，避免 Destroy 后留下空引用
        if (route != null && route.scenery != null)
        {
            var scenery = new List<Renderer>(route.scenery);
            foreach (var renderer in renderers) scenery.Remove(renderer);
            route.scenery = scenery.ToArray();
        }
        foreach (var renderer in renderers) registeredScenery.Remove(renderer);
    }

    void SyncScenery()
    {
        if (route == null) return;
        var fixedScenery = route.scenery == null ? new List<Renderer>() : new List<Renderer>(route.scenery);
        fixedScenery.RemoveAll(r => r == null || registeredScenery.Contains(r));
        fixedScenery.AddRange(registeredScenery);
        route.scenery = fixedScenery.ToArray();
    }

    void BuildPath()
    {
        float length = route.Length;
        samplesPerBranch = Mathf.CeilToInt(length / 0.5f) + 1;
        pathSamples = new Vector2[samplesPerBranch * 2];
        for (int side = 0; side < 2; side++)
        {
            bool right = side == 1;
            for (int i = 0; i < samplesPerBranch; i++)
            {
                float heading;
                var p = YScrollSample.Evaluate(i * 0.5f, route.turnAngle, right, out heading);
                pathSamples[side * samplesPerBranch + i] = new Vector2(p.x, p.z);
            }
        }
    }

    /// <summary>把世界 XZ 投影到路径上，返回沿路米数。左右两条分支各算一次取更近者。</summary>
    float ProjectDistance(Vector2 worldXZ)
    {
        float length = route.Length;
        float bestDistance = 0f;
        float bestSqr = float.MaxValue;
        for (int side = 0; side < 2; side++)
        {
            int offset = side * samplesPerBranch;
            for (float d = 0f; d <= length; d += CoarseStep)
            {
                int index = Mathf.Min(samplesPerBranch - 1, Mathf.RoundToInt(d / 0.5f));
                float sqr = (pathSamples[offset + index] - worldXZ).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; bestDistance = index * 0.5f; }
            }
            float from = Mathf.Max(0f, bestDistance - CoarseStep);
            float to = Mathf.Min(length, bestDistance + CoarseStep);
            for (float d = from; d <= to; d += RefineStep)
            {
                int index = Mathf.Min(samplesPerBranch - 1, Mathf.RoundToInt(d / 0.5f));
                float sqr = (pathSamples[offset + index] - worldXZ).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; bestDistance = index * 0.5f; }
            }
        }
        return bestDistance;
    }

    bool IsAlwaysResident(Transform target)
    {
        for (int i = 0; i < alwaysResidentRoots.Length; i++)
        {
            var root = alwaysResidentRoots[i];
            if (root == null) continue;
            if (target == root || target.IsChildOf(root)) return true;
        }
        return false;
    }

    /// <summary>渲染器是否由 RoutePropFollower 独占可见性（是则本窗口不碰它）。</summary>
    static bool IsFollowerOwned(Renderer renderer)
    {
        if (renderer == null) return false;
        return renderer.GetComponentInParent<RoutePropFollower>() != null;
    }

    void ApplyWindow(float position)
    {
        WindowStart = position - renderBehind;
        WindowEnd = position + renderAhead;
        int submitted = 0;
        for (int i = 0; i < tracked.Count; i++)
        {
            var renderer = tracked[i];
            if (renderer == null) continue;
            float distance = trackedDistance[i];
            bool inside = distance >= WindowStart && distance <= WindowEnd;
            bool want = authoredEnabled[i] && inside;
            if (appliedEnabled[i] != want)
            {
                renderer.enabled = want;
                appliedEnabled[i] = want;
            }
            if (want && renderer.gameObject.activeInHierarchy) submitted++;
        }
        SubmittedCount = submitted;
        restored = false;
        if (logWindow) Debug.Log($"[YStream] pos={position:F1} render=[{WindowStart:F0},{WindowEnd:F0}] resident=[{ResidentStart:F0},{ResidentEnd:F0}] submitted={SubmittedCount}/{tracked.Count}");
    }

    void RestoreAll()
    {
        if (tracked.Count == 0) return;
        for (int i = 0; i < tracked.Count; i++)
        {
            var renderer = tracked[i];
            if (renderer == null) continue;
            if (renderer.enabled != authoredEnabled[i])
            {
                renderer.enabled = authoredEnabled[i];
                appliedEnabled[i] = authoredEnabled[i];
            }
        }
        restored = true;
    }

#if UNITY_EDITOR
    /// <summary>作者态在运行时被修改后，重新采集一次基线（仅编辑器调试用）。</summary>
    public void RebuildTable()
    {
        if (!Application.isPlaying) return;
        RestoreAll();
        BuildTable();
    }
#endif
}
