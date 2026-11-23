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
    float pathBuiltLength;
    readonly List<Renderer> tracked = new List<Renderer>();
    readonly List<float> trackedDistance = new List<float>();
    readonly List<int> trackedLap = new List<int>();
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

    void AddEntry(Renderer renderer, int lap = 0)
    {
        EnsurePath();
        var p = renderer.transform.position;
        float cycle = route != null ? Mathf.Max(1f, route.Length) : 0f;
        // ★ 注册时实例还在**未套帧**的原始位置 ⇒ 世界 XZ 就是局部沿路坐标，
        //   直接投影即可；第 lap 圈的整体偏移只影响标量距离（刚体帧保弧长）。
        tracked.Add(renderer);
        trackedLap.Add(lap);
        trackedDistance.Add(ProjectDistance(new Vector2(p.x, p.z)) + lap * cycle);
        authoredEnabled.Add(renderer.enabled);
        appliedEnabled.Add(true);
    }

    /// <summary>按需实例化的段装载后调用：加入距离表和 scenery 的 bounds 表，并按当前窗口立即生效。</summary>
    /// <param name="lap">
    /// **圈号**（2026-11-19 无限循环）：同一批段 prefab 在第 k 圈会再装一份，
    /// 摆在「本圈位置 + k × 圈偏移」处 ⇒ 它的沿路距离要加 k × 一圈长度，
    /// 否则窗口会拿本圈的数字去比第 k 圈的世界位置 ⇒ 段会被错误地淡出/激活。
    /// </param>
    public void RegisterSegment(GameObject segmentRoot, int lap = 0)
    {
        if (segmentRoot == null) return;
        var renderers = segmentRoot.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            if (IsFollowerOwned(renderer)) continue;
            if (tracked.Contains(renderer)) continue;
            AddEntry(renderer, lap);
            registeredScenery.Add(renderer);
        }
        SyncScenery();
        ApplyWindow(float.IsNaN(lastPosition) ? CurrentPosition() : lastPosition);
    }

    /// <summary>
    /// 把指定一组 renderer 的**沿路距离**整体平移（2026-11-19 无限循环）。
    ///
    /// 为什么需要：<see cref="YLapFollower"/> 把静态布景按圈平移了（世界位置变了），
    /// 但窗口判定用的是**采集时的沿路距离** ⇒ 不同步的话，圈一变这些物件就掉出窗口被**淡出**。
    /// </summary>
    public void ShiftTrackedDistance(System.Collections.Generic.IEnumerable<Renderer> renderers, float amount)
    {
        if (renderers == null || Mathf.Approximately(amount, 0f)) return;
        for (int i = 0; i < tracked.Count; i++)
        {
            var r = tracked[i];
            if (r == null) continue;
            foreach (var target in renderers)
            {
                if (r != target) continue;
                trackedDistance[i] += amount;
                break;
            }
        }
    }

    /// <summary>
    /// **直接设定**一组 renderer 的沿路距离（幂等），不用增量。
    ///
    /// 为什么：圈号可能**跳变**（检查点、直接改 progress、一次跨多圈），增量就要求
    /// 每一跳恰好收到一次事件 ⇒ 漏一次就永久错位（表现为「第二圈某个节点整片物件消失」）。
    /// 这里按物件**当前位置**反算，错了也能自己纠正。
    /// </summary>
    public void SetTrackedDistance(System.Collections.Generic.IEnumerable<Renderer> renderers, int lap)
    {
        if (renderers == null || route == null) return;
        float cycle = Mathf.Max(1f, route.Length);
        var frame = route.GetLapFrame(lap);
        foreach (var target in renderers)
        {
            if (target == null) continue;
            int idx = tracked.IndexOf(target);
            if (idx < 0) continue;
            Vector3 local = frame.Inverse(target.transform.position);
            trackedDistance[idx] = ProjectDistance(new Vector2(local.x, local.z)) + lap * cycle;
            if (idx < trackedLap.Count) trackedLap[idx] = lap;
        }
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
            if (i < trackedLap.Count) trackedLap.RemoveAt(i);
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
        pathBuiltLength = length;
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

    /// <summary>
    /// ★ 采样表是按「当时的 Length」建的。无限卷轴开启后 Length 从地图长度变成一圈长度，
    /// 不重建的话所有超过地图末端的内容都会被投影到地图末端（= 反推过来说它们“在身后”）
    /// ⇒ 最后一段布景会在相机走到那里之前就被窗口淡出（表现为“到那儿什么都没有”）。
    /// </summary>
    void EnsurePath()
    {
        if (pathSamples != null && Mathf.Abs(pathBuiltLength - route.Length) < 0.01f) return;
        BuildPath();
        RebuildDistances();
    }

    /// <summary>采样表重建后，按新路径重算所有已登记物件的沿路距离。</summary>
    void RebuildDistances()
    {
        if (route == null) return;
        float cycle = Mathf.Max(1f, route.Length);
        for (int i = 0; i < tracked.Count && i < trackedDistance.Count; i++)
        {
            var r = tracked[i];
            if (r == null) continue;
            int lap = i < trackedLap.Count ? trackedLap[i] : 0;
            Vector3 local = route.GetLapFrame(lap).Inverse(r.transform.position);
            trackedDistance[i] = ProjectDistance(new Vector2(local.x, local.z)) + lap * cycle;
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
