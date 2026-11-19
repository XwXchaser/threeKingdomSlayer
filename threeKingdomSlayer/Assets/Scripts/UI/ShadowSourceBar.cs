using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// **暗源条**（影之武装资源，2026-11-16）。
///
/// 口径（用户定）：
///   · 暗源与**大招能量并存**（暗源管主动技能，大招能量管大招）—— 不合并、不抢位置；
///   · 显示**数量与充能**：一条横条 + 数字，见底变红。
///
/// 位置：**主动技能栏（<see cref="FrontItemBar"/>）正下方**（与它同区，因为它服务于那些技能）。
/// 整条 HUD 在**运行时创建**（不进场景、不改 prefab）⇒ 不用动 Battle.scene。
///
/// 获取规则在 <see cref="PlayerState"/>（只靠命中 / 击杀 → 防御姿态不回）。
/// </summary>
public class ShadowSourceBar : MonoBehaviour
{
    public static ShadowSourceBar Instance { get; private set; }

    [Header("外观")]
    public Color barColor = new Color(0.16f, 0.13f, 0.22f, 0.85f);
    public Color fillColor = new Color(0.72f, 0.58f, 0.95f, 1f);
    public Color emptyColor = new Color(0.95f, 0.35f, 0.32f, 1f);
    public float barHeight = 14f;
    public float barWidth = 420f;

    private Image _fill;
    private TextMeshProUGUI _text;
    private RectTransform _rect;
    private bool _placed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("ShadowSourceBar");
        go.AddComponent<ShadowSourceBar>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUi();
        var ps = PlayerState.Instance;
        if (ps != null) ps.OnShadowSourceChanged += Refresh;
    }

    private void OnDestroy()
    {
        var ps = PlayerState.Instance;
        if (ps != null) ps.OnShadowSourceChanged -= Refresh;
        if (Instance == this) Instance = null;
    }

    private void BuildUi()
    {
        var canvasGo = new GameObject("ShadowSourceCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 96;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(UIResolutionHelper.ReferenceWidth, UIResolutionHelper.ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = canvasGo.GetComponent<RectTransform>();

        var bgGo = new GameObject("Bar", typeof(RectTransform));
        bgGo.transform.SetParent(root, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = barColor;
        bg.raycastTarget = false;
        _rect = bgGo.GetComponent<RectTransform>();

        var fillGo = new GameObject("Fill", typeof(RectTransform));
        fillGo.transform.SetParent(_rect, false);
        _fill = fillGo.AddComponent<Image>();
        _fill.color = fillColor;
        _fill.raycastTarget = false;
        _fill.type = Image.Type.Filled;
        _fill.fillMethod = Image.FillMethod.Horizontal;
        _fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        var fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(_rect, false);
        _text = textGo.AddComponent<TextMeshProUGUI>();
        _text.font = UiFontUtility.ResolveCjkFont();
        _text.fontSize = 22;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.white;
        _text.raycastTarget = false;
        _text.enableWordWrapping = false;
        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(4f, 0f);
        textRect.offsetMax = new Vector2(-4f, 0f);
    }

    private void Update()
    {
        if (!_placed) TryPlace();
        Refresh();
    }

    /// <summary>把条放到主动技能栏正下方（找不到就退化为屏幕底部居中）。</summary>
    private void TryPlace()
    {
        var bar = FrontItemBar.Instance;
        if (bar != null)
        {
            var barRt = bar.GetComponent<RectTransform>();
            if (barRt != null && barRt.parent != null)
            {
                var parent = barRt.parent as RectTransform;
                if (parent != null)
                {
                    // 挂到主动技能栏的同一个父节点 → 位置随 HUD 布局走
                    _rect.SetParent(parent, false);
                    _rect.anchorMin = barRt.anchorMin;
                    _rect.anchorMax = barRt.anchorMax;
                    _rect.pivot = new Vector2(0.5f, 1f);
                    _rect.sizeDelta = new Vector2(barWidth, barHeight);
                    float dy = -(barRt.sizeDelta.y * (1f - barRt.pivot.y) + 10f);
                    _rect.anchoredPosition = new Vector2(barRt.anchoredPosition.x, barRt.anchoredPosition.y + dy);
                    _placed = true;
                    return;
                }
            }
        }

        // 退路：屏幕底部居中
        _rect.SetParent(_rect.parent, false);
        _rect.anchorMin = new Vector2(0.5f, 0f);
        _rect.anchorMax = new Vector2(0.5f, 0f);
        _rect.pivot = new Vector2(0.5f, 0f);
        _rect.sizeDelta = new Vector2(barWidth, barHeight);
        _rect.anchoredPosition = new Vector2(0f, 180f);
        _placed = true;
    }

    private void Refresh()
    {
        var ps = PlayerState.Instance;
        if (ps == null || _fill == null) return;
        float max = Mathf.Max(1f, ps.ShadowSourceMax);
        float cur = Mathf.Clamp(ps.ShadowSource, 0f, max);
        _fill.fillAmount = cur / max;
        bool empty = cur <= 0.01f;
        _fill.color = empty ? emptyColor : fillColor;
        if (_text != null) _text.text = $"暗源 {Mathf.FloorToInt(cur)}/{Mathf.FloorToInt(max)}";
    }
}
