using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **等级球教学引导**（2026-11-07 用户需求：「需要添加等级球的教学引导 —— 告诉玩家等级球在哪、显示什么信息」）。
///
/// 表现：**手 + 光圈**指着等级球（战斗 HUD 左下角），旁边一句白字。
///   • 等级球 = 战斗 HUD 左下角的小球：
///       - 球里的**液面** = 经验进度（离下一级还差多少）
///       - 球里的**数字** = **当前待选取的三选一数量**（0 时不显示数字）
///   • 白字、无底板（与其它教学引导同一套观感）。
///
/// 由 <c>TutorialStageDirector</c> 在**教学关开场**叫起，在**第一拍开始**时收掉
/// （避免与教学拍自己的手势示范撞在一起）。
///
/// ⚠️ 本组件自己建一个高优先级 Canvas（跟 <c>HubTutorialGuide</c> 同一套路），
/// 否则会被战斗 HUD / 教学遮罩盖住。
/// </summary>
public class LevelOrbGuide : MonoBehaviour
{
    [Header("素材")]
    [Tooltip("引导手贴图（留空 = 不画手）")]
    public Sprite handSprite;
    [Tooltip("引导光圈贴图（留空 = 程序化圆环）")]
    public Sprite ringSprite;

    [Header("文案")]
    [Tooltip("引导文字（白字；也可在《教学字幕总表》里改）")]
    [TextArea(2, 4)] public string guideText = "等级球：液面是经验进度，数字是等你选的三选一数量";

    [Header("样式")]
    [Tooltip("手的世界尺寸（画布单位）")]
    public float handSize = 190f;
    [Tooltip("光圈的世界尺寸（画布单位）")]
    public float ringSize = 150f;
    [Tooltip("文字距离等级球的距离（画布单位）")]
    public float textGap = 130f;
    [Tooltip("文字相对等级球的纵向偏移（画布单位）")]
    public float textRise = 90f;
    [Tooltip("图层排序（要压在战斗 HUD 与教学遮罩之上）")]
    public int sortingOrder = 140;

    private RectTransform _layer;
    private Image _hand;
    private Image _ring;
    private TextMeshProUGUI _label;

    /// <summary>是否正在显示。</summary>
    public bool IsShowing => _layer != null && _layer.gameObject.activeInHierarchy;

    /// <summary>叫起引导（指向等级球）。</summary>
    public void Show()
    {
        EnsureBuilt();
        if (_layer == null) return;
        _layer.gameObject.SetActive(true);
        Refresh();
    }

    /// <summary>收掉引导。</summary>
    public void Hide()
    {
        if (_layer == null) return;
        _layer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsShowing) return;
        Refresh();
    }

    // ── 构建 ────────────────────────────────────────────────────────────────

    private void EnsureBuilt()
    {
        if (_layer != null && _layer.gameObject != null) return;   // 场景重载后重建

        // ★ 自建**根画布**（不能挂在普通 Transform 下 → 那样 RectTransform 尺寸为 0 → 什么都画不出来）
        var layerGo = new GameObject("LevelOrbGuideCanvas", typeof(RectTransform));
        var lr = (RectTransform)layerGo.transform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

        // 根画布尺寸 = 屏幕；排序压在战斗 HUD(21) / 教学遮罩(121) 之上
        var canvas = layerGo.AddComponent<Canvas>();
        // ⚠️ 必须显式指定 Overlay：运行时新建的根画布有时会落到 WorldSpace
        //   （WorldSpace 下画布尺寸不会被屏幕驱动 → rect 为 0 → 什么都画不出来）
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        var scaler = layerGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _layer = lr;

        _ring = MakeImage("Ring", new Color(1f, 0.86f, 0.4f, 0.9f),
            ringSprite != null ? ringSprite : ProceduralUiSprites.Ring(128, 12, 1f));
        _hand = MakeImage("Hand", Color.white, handSprite);
        _label = MakeText("Label");

        layerGo.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_layer != null) Destroy(_layer.gameObject);
        _layer = null;
    }

    private Image MakeImage(string name, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_layer, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = false;
        if (sprite == null) img.enabled = false;
        return img;
    }

    private TextMeshProUGUI MakeText(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_layer, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        var hud = HeroHUD.Instance;
        if (hud != null && hud.levelOrbText != null && hud.levelOrbText.font != null)
            txt.font = hud.levelOrbText.font;          // 跟战斗 HUD 同一个字体
        else if (TMP_Settings.defaultFontAsset != null)
            txt.font = TMP_Settings.defaultFontAsset;
        txt.raycastTarget = false;
        txt.fontSize = 34f;
        txt.alignment = TextAlignmentOptions.Left;
        txt.color = Color.white;
        txt.enableWordWrapping = false;
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0f, 0.5f);
        r.sizeDelta = new Vector2(760f, 96f);
        r.anchoredPosition = Vector2.zero;
        return txt;
    }

    // ── 每帧刷新（等级球会动） ─────────────────────────────────────────────

    private void Refresh()
    {
        var hud = HeroHUD.Instance;
        var orb = hud != null ? hud.levelOrbTarget as RectTransform : null;
        if (orb == null || _layer == null) return;

        Vector3 world = orb.TransformPoint(orb.rect.center);
        Vector2 local = _layer.InverseTransformPoint(world);

        if (_ring != null && _ring.enabled)
        {
            var rr = _ring.rectTransform;
            rr.sizeDelta = new Vector2(ringSize, ringSize);
            rr.pivot = Vector2.one * 0.5f;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = local;
            rr.SetAsLastSibling();
        }

        if (_hand != null)
        {
            if (_hand.sprite != null)
            {
                var hr = _hand.rectTransform;
                hr.sizeDelta = new Vector2(handSize, handSize);
                hr.pivot = TutorialGestureStyle.HandPivot;       // 支点在掌心
                hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);
                // 指尖钉在等级球上：手心位置 = 目标 - 掌心→指尖偏移（旋转后）
                Vector3 tipOffset = new Vector3(
                    (TutorialGestureStyle.TipUV.x - hr.pivot.x) * handSize,
                    (TutorialGestureStyle.TipUV.y - hr.pivot.y) * handSize, 0f);
                tipOffset = hr.localRotation * tipOffset;
                tipOffset = new Vector3(tipOffset.x * hr.localScale.x, tipOffset.y * hr.localScale.y, 0f);
                hr.anchoredPosition = new Vector2(local.x - tipOffset.x, local.y - tipOffset.y);
                hr.SetAsLastSibling();
            }
            else _hand.gameObject.SetActive(false);
        }

        if (_label != null)
        {
            if (_label.text != guideText) _label.text = guideText ?? string.Empty;
            var lr = _label.rectTransform;
            lr.anchoredPosition = new Vector2(local.x + textGap, local.y + textRise);
            // 夹在图层内（别顶出屏幕）
            Rect b = _layer.rect;
            float halfW = lr.sizeDelta.x * lr.pivot.x;
            float x = Mathf.Clamp(lr.anchoredPosition.x, b.xMin + halfW + 8f, b.xMax - (lr.sizeDelta.x * (1f - lr.pivot.x)) - 8f);
            float y = Mathf.Clamp(lr.anchoredPosition.y, b.yMin + 8f + lr.sizeDelta.y * 0.5f, b.yMax - 8f - lr.sizeDelta.y * 0.5f);
            lr.anchoredPosition = new Vector2(x, y);
            lr.SetAsLastSibling();
        }
    }
}
