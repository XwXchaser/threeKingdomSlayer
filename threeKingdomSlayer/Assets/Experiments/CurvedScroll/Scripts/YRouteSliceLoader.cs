using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// **切片序列实验驱动**（2026-11-19，最小实验）。
///
/// <para><b>核心约定</b>：环回时所有内容一律沿 <b>flat z</b> 推进
/// （段 prefab 的子物件本来就住在 flat 空间：x=横向, z=沿路），
/// <b>不碰 3D 曲线</b> ⇒ 拼接天然对齐、不会歪、不会跳变、**不会倒退**。</para>
///
/// <para><b>交互</b>：屏幕右上角有一个**看得见能点的按钮**「卷轴实验 开/关」。
/// 默认关 ⇒ 第一关完全不受影响。</para>
///
/// <para><b>不动现有运行时</b>：战斗、出口面板、路线流程仍走 <c>FakeRouteRuntime</c>。
/// 本实验只验证「切片拼接 + 环回重摆 ⇒ 第二圈画面与第一圈一致」。</para>
/// </summary>
[DisallowMultipleComponent]
public sealed class YRouteSliceLoader : MonoBehaviour
{
    [Tooltip("切片序列（成环 = 无限卷轴）")]
    public YRouteSliceSequence sequence;

    [Tooltip("留空则自动取场景里真正的那个 YScrollSample")]
    public YScrollSample route;

    [Header("实验驱动")]
    [Tooltip("自动沿路前进")]
    public bool autoAdvance = true;
    [Tooltip("前进速度（米/秒）")]
    public float metersPerSecond = 10f;

    [Header("面板")]
    public Vector2 buttonAnchoredPosition = new Vector2(-24f, -150f);

    /// <summary>实验是否开启（按钮可切）。</summary>
    public static bool ExperimentEnabled = false;

    /// <summary>相机在 flat 空间的沿路位置（米）。</summary>
    public float FlatZ { get; private set; }

    /// <summary>当前是第几圈（= floor(flatZ / 一圈长度)）。</summary>
    public int CurrentCycle { get; private set; }

    private Button _button;
    private TextMeshProUGUI _label;
    private bool _applied;
    private int _nextRouteSearchFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("YRouteSliceLoader");
        go.AddComponent<YRouteSliceLoader>();
    }

    private void Awake()
    {
        BuildButton();
    }

    private void Update()
    {
        if (ExperimentEnabled && !_applied) ApplyOn();
        if (!ExperimentEnabled && _applied) ApplyOff();

        if (route == null && Time.frameCount >= _nextRouteSearchFrame)
        {
            // ⚠ 必须挑**真正的**场景 YScrollSample（带节点锚点那个）。
            //   每帧 FindObjectsOfType 太贵：找不到就隔 30 帧再找（Battle 落地页里没有这个组件）。
            _nextRouteSearchFrame = Time.frameCount + 30;
            var samples = Object.FindObjectsOfType<YScrollSample>();
            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] == null) continue;
                if (samples[i].nodePresentations == null || samples[i].nodePresentations.Length == 0) continue;
                route = samples[i];
                break;
            }
        }
        if (!ExperimentEnabled || route == null || sequence == null) return;

        float cycle = Mathf.Max(1f, route.Length);
        if (autoAdvance) route.progress += (metersPerSecond * Time.deltaTime) / cycle;
        FlatZ = route.progress * cycle;
        CurrentCycle = Mathf.FloorToInt(FlatZ / cycle);
    }

    private void ApplyOn()
    {
        if (route == null) return;
        if (sequence != null)
        {
            float mapLength = YScrollSample.GetRouteLength(route.turnAngle);
            float total = sequence.TotalLength;
            // ★ 一圈长度必须 = **一圈内容的长度**。切片序列铺不满一整圈时不能拿它覆盖，
            //   否则周期比内容短 ⇒ 节点和相机按两个周期走 ⇒ 接缝必错。
            if (total >= mapLength - 0.5f) route.flatCycleLength = total;
            else
            {
                Debug.LogWarning("[SliceLoader] 切片序列只有 " + total.ToString("F0") + " 米，不足一整圈（"
                                 + mapLength.ToString("F0") + " 米）⇒ 用场景里配置的一圈长度（当前 "
                                 + route.flatCycleLength.ToString("F0") + " 米）。");
            }
        }
        // ★ 没挂切片序列也必须能开：一圈长度用场景里配的值（没配则用地图长度）。
        //   ⚠ 以前 sequence 为空就直接 return ⇒ 按钮是个空转的，正式关里永远开不了。
        if (route.flatCycleLength <= 0f) route.flatCycleLength = YScrollSample.GetRouteLength(route.turnAngle);
        route.infiniteScroll = true;
        route.RefreshNodeAnchors();
        _applied = true;
        Debug.Log("[SliceLoader] 无限卷轴已开：一圈=" + route.Length.ToString("F0") + " 米");
        RefreshLabel();
    }

    private void ApplyOff()
    {
        if (route != null) { route.infiniteScroll = false; route.flatCycleLength = 0f; }
        _applied = false;
        Debug.Log("[SliceLoader] 实验关闭，恢复原状");
        RefreshLabel();
    }

    private void BuildButton()
    {
        var canvasGo = new GameObject("SliceToggleCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var btnGo = new GameObject("SliceToggleButton", typeof(RectTransform));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var rect = (RectTransform)btnGo.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = buttonAnchoredPosition;
        rect.sizeDelta = new Vector2(280f, 84f);

        var img = btnGo.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.55f);
        _button = btnGo.AddComponent<Button>();
        _button.targetGraphic = img;
        _button.onClick.AddListener(OnToggle);

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(btnGo.transform, false);
        var lrect = (RectTransform)labelGo.transform;
        lrect.anchorMin = Vector2.zero; lrect.anchorMax = Vector2.one;
        lrect.offsetMin = Vector2.zero; lrect.offsetMax = Vector2.zero;
        _label = labelGo.AddComponent<TextMeshProUGUI>();
        _label.alignment = TextAlignmentOptions.Center;
        _label.fontSize = 34f;
        _label.color = Color.white;
        _label.font = UiFontUtility.ResolveCjkFont();
        _label.enableWordWrapping = false;
        _label.overflowMode = TextOverflowModes.Overflow;
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (_label == null) return;
        _label.text = ExperimentEnabled ? "卷轴实验 开" : "卷轴实验 关";
    }

    private void OnToggle()
    {
        ExperimentEnabled = !ExperimentEnabled;
        RefreshLabel();
        Debug.Log("[SliceLoader] 实验开关 = " + ExperimentEnabled);
    }
}
