using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 卷轴路线表现层：把运行器提交的「阶段 + 目标节点」翻译为 YScrollSample 的 progress 位移与分支车道。
/// 严格不修改 Player / Enemy / Battle Camera 坐标，也不修改 YScrollSample 的 Evaluate / Length / turnAngle 语义。
/// 本类对应旧预演驱动 Assets/Scripts/Core/BattleYRouteHost.cs 的表现部分（旧文件保留到清理轮再删）。
/// </summary>
public sealed class ScrollRoutePresenter : MonoBehaviour, IRouteMotionPresenter
{
    [Header("路线场景")]
    public string routeSceneName = "Assets/Experiments/CurvedScroll/YJunctionSample.unity";
    [Tooltip("Battle 主相机：卷轴投影使用的视图相机。Y 场景自带相机会在准备阶段被禁用。")]
    public Camera battleCamera;
    [Tooltip("卷轴路线根节点的地面高度（与旧驱动保持一致）。")]
    public float groundHeight = -1.8f;
    [Tooltip("出口/开场未提供曲线时使用的兜底进度曲线。")]
    public AnimationCurve fallbackProgressCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(0.1f, 0.1f, 1.6f, 1.6f),
        new Keyframe(0.4f, 0.65f, 1f, 1f),
        new Keyframe(0.8f, 0.97f, 0.22f, 0.22f),
        new Keyframe(1f, 1f, 0f, 0f));
    [Tooltip("默认出口排布的水平间距，必须与 RouteChoiceButton 的默认排布一致。")]
    public float defaultChoiceSpacing = 160f;

    public bool IsPlaying { get; private set; }
    public bool IsSceneReady => route != null;
    public YScrollSample Route => route;

    YScrollSample route;
    bool skipRequested;

    // ---------------------------------------------------------------- 接口实现

    public bool TryValidate(FakeRouteStageConfig config, out string error)
    {
        error = string.Empty;
        if (!battleCamera) { error = "ScrollRoutePresenter 未绑定 battleCamera"; return false; }
        if (config == null || config.nodes == null) { error = "路线配置为空"; return false; }
        return true;
    }

    /// <summary>加载/绑定路线场景，并校验配置中的每个节点都能找到唯一锚点；幂等。</summary>
    public IEnumerator Prepare(FakeRouteStageConfig config, Func<bool> canContinue)
    {
        yield return EnsureRoute();
        if (route == null) yield break;
        if (config == null || config.nodes == null) yield break;
        for (int i = 0; i < config.nodes.Count; i++)
        {
            var node = config.nodes[i];
            if (node == null) continue;
            if (!route.TryGetNodeProgress(node, out _))
                Debug.LogError("[ScrollRoute] 路线场景缺少节点锚点（ScrollRouteNodePresentation）: " + node.nodeId);
        }
    }

    public IEnumerator PlayOpening(FakeRouteTravelPresentation presentation, FakeRouteNodeConfig startNode, Func<bool> canContinue)
    {
        yield return EnsureRoute();
        if (route == null) yield break;

        if (!route.TryGetNodeProgress(startNode, out float start))
        {
            Debug.LogError("[ScrollRoute] 缺少 startNode 锚点: " + (startNode ? startNode.nodeId : "null"));
            yield break;
        }

        if (presentation == null)
        {
            // 不播开场：直接把视觉状态定位到 startNode 锚点（N1 锚点距离为 0 时等同于原地）。
            route.progress = start;
            route.animate = false;
            yield break;
        }

        yield return MoveTo(start, presentation.duration, presentation.speedCurve);
    }

    public IEnumerator PlayChoiceTravel(FakeRouteChoiceConfig choice, FakeRouteNodeConfig from, FakeRouteNodeConfig to, Func<bool> canContinue)
    {
        yield return EnsureRoute();
        if (route == null) yield break;
        if (!route.TryGetNodeProgress(to, out float end))
        {
            Debug.LogError("[ScrollRoute] 缺少目标节点锚点: " + (to ? to.nodeId : "null"));
            yield break;
        }

        ApplyLane(choice, from);
        float duration = choice != null && choice.presentation != null ? choice.presentation.duration : 2f;
        AnimationCurve curve = choice != null && choice.presentation != null ? choice.presentation.speedCurve : null;
        yield return MoveTo(end, duration, curve);
    }

    public IEnumerator PlayNodePresentation(FakeRoutePresentation presentation, Func<bool> canContinue)
    {
        if (presentation == null) yield break;
        // 卷轴表现层只实现「时长节拍」：静态图 / 视频 / 黑屏过场未接入（需要时另开任务）。
        if (presentation.mode != FakeRoutePresentationMode.StaticImage)
            Debug.LogWarning("[ScrollRoute] 卷轴表现层暂不支持该演出模式，仅按时长停留: " + presentation.mode);
        IsPlaying = true;
        skipRequested = false;
        float remain = Mathf.Max(0f, presentation.duration);
        while (remain > 0f && !skipRequested && canContinue())
        {
            if (Time.timeScale <= 0f) { yield return null; continue; }
            remain -= Time.unscaledDeltaTime;
            yield return null;
        }
        IsPlaying = false;
        skipRequested = false;
    }

    public void ApplyNodeVisuals(FakeRouteNodeConfig node)
    {
        if (route == null) return;
        route.animate = false;
    }

    public bool TryGetActiveScene(out Scene scene)
    {
        scene = default;
        if (route == null) return false;
        scene = route.gameObject.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    public void Skip()
    {
        if (IsPlaying) skipRequested = true;
    }

    // ---------------------------------------------------------------- 卷轴控制

    IEnumerator EnsureRoute()
    {
        if (route != null) yield break;
        var scene = SceneManager.GetSceneByPath(routeSceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(routeSceneName, LoadSceneMode.Additive);
            if (op == null) { Debug.LogError("[ScrollRoute] 路线场景加载失败: " + routeSceneName); yield break; }
            yield return op;
            scene = SceneManager.GetSceneByPath(routeSceneName);
        }
        if (!scene.IsValid() || !scene.isLoaded) { Debug.LogError("[ScrollRoute] 路线场景未加载: " + routeSceneName); yield break; }

        foreach (var root in scene.GetRootGameObjects())
        {
            route = root.GetComponentInChildren<YScrollSample>(true);
            if (route) break;
        }
        if (route == null) { Debug.LogError("[ScrollRoute] 路线场景缺少 YScrollSample: " + routeSceneName); yield break; }

        // Y 场景自带相机不参与渲染；投影使用 Battle 主相机。
        foreach (var root in scene.GetRootGameObjects())
            foreach (var cam in root.GetComponentsInChildren<Camera>(true)) cam.enabled = false;

        var legacyHost = route.GetComponent<YSampleBattleHost>();
        if (legacyHost) { legacyHost.StopAllCoroutines(); legacyHost.enabled = false; }

        route.viewCamera = battleCamera;
        route.animate = false;
        var position = route.transform.position;
        position.y = groundHeight;
        route.transform.position = position;

        SetAllBranchVisuals(false);
        SetBranchGroup(route.junctionVisuals, false);
        foreach (var t in route.GetComponentsInChildren<Transform>(true))
            if (t.name == "Display Enemies - no combat") t.gameObject.SetActive(false);
    }

    IEnumerator MoveTo(float end, float seconds, AnimationCurve curve)
    {
        if (route == null) yield break;
        float origin = route.progress;
        float duration = Mathf.Max(0.1f, seconds);
        float elapsed = 0f, traveled = 0f;
        IsPlaying = true;
        skipRequested = false;
        while (elapsed < duration)
        {
            if (skipRequested) break;
            if (Blocking()) { yield return null; continue; }
            elapsed += Time.deltaTime;
            float time = Mathf.Clamp01(elapsed / duration);
            AnimationCurve use = curve != null && curve.length >= 2 ? curve : fallbackProgressCurve;
            float sample = use != null && use.length >= 2 ? use.Evaluate(time) : Mathf.SmoothStep(0f, 1f, time);
            traveled = Mathf.Max(traveled, Mathf.Clamp01(sample));
            route.progress = Mathf.Lerp(origin, end, traveled);
            yield return null;
        }
        route.progress = end;
        route.animate = false;
        IsPlaying = false;
        skipRequested = false;
    }

    static bool Blocking()
    {
        return Time.timeScale <= 0f
            || (CombatPresenters.Dialogue != null && CombatPresenters.Dialogue.IsInteractionBlocked)
            || (UpgradeChoiceManager.Instance && UpgradeChoiceManager.Instance.IsChoosing)
            || (CombatPresenters.ItemDiscard != null && CombatPresenters.ItemDiscard.IsShowing)
            || (ExpGemManager.Instance && ExpGemManager.Instance.IsCollecting);
    }

    // ---------------------------------------------------------------- 分支车道与视觉组

    /// <summary>
    /// 出口所在车道：优先读出口的 layout（overrideDefault 时用 anchoredPosition.x），
    /// 否则按默认排布 (index-(n-1)/2)*spacing 推断；只有一个出口时保持当前车道不变（宽路阶段车道无意义）。
    /// </summary>
    void ApplyLane(FakeRouteChoiceConfig choice, FakeRouteNodeConfig from)
    {
        if (route == null) return;
        int count = from != null && from.outgoingChoices != null ? from.outgoingChoices.Count : 1;
        if (count <= 1) return;

        int index = from.outgoingChoices.IndexOf(choice);
        if (index < 0) index = 0;
        float x = choice != null && choice.layout != null && choice.layout.overrideDefault
            ? choice.layout.anchoredPosition.x
            : (index - (count - 1) * 0.5f) * defaultChoiceSpacing;
        bool right = x >= 0f;

        route.right = right;
        SetAllBranchVisuals(false);
        SetBranchGroup(right ? route.rightBranchVisuals : route.leftBranchVisuals, true);
        SetBranchGroup(right ? route.rightBranchEncounter : route.leftBranchEncounter, true);
    }

    void SetAllBranchVisuals(bool active)
    {
        if (route == null) return;
        SetBranchGroup(route.leftBranchVisuals, active);
        SetBranchGroup(route.leftBranchEncounter, active);
        SetBranchGroup(route.rightBranchVisuals, active);
        SetBranchGroup(route.rightBranchEncounter, active);
    }

    static void SetBranchGroup(Transform group, bool active)
    {
        if (!group) return;
        group.gameObject.SetActive(active);
        foreach (var child in group.GetComponentsInChildren<Transform>(true))
            if (child != group) child.gameObject.SetActive(active && child.name != "Display Enemies - no combat");
    }
}
