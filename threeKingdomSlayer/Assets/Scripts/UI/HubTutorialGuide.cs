using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **主菜单教学引导**（2026-11-04）。
///
/// 教学链的收尾两环（用户反馈：「教学关结束后，主菜单没有强制引导我进天赋界面」/「天赋教学结束后回到主菜单没有开始关卡的引导」）：
///
///   ① 教学关打完、且**天赋页还没教过** → 主菜单**强制**引导玩家去「天赋」界面解锁招式
///      （不引导的话玩家就停在主菜单，主线断在这儿）。
///   ② 天赋页教学完成 → 回主菜单 → 引导玩家点「开始关卡」。
///
/// 表现：**手 + 光圈 + 一行文字**（运行时自绘，和天赋面板里的教学同一套观感）。
/// 只提示、不拦路：玩家不点也能继续（教学关专属的强制感靠「教程期间不弹别的界面」实现）。
/// </summary>
public class HubTutorialGuide : MonoBehaviour
{
    [Header("素材")]
    [Tooltip("引导手的贴图（留空 = 不画手）")]
    public Sprite handSprite;
    [Tooltip("引导光圈的贴图（留空 = 程序化圆环）")]
    public Sprite ringSprite;
    [Tooltip("文字字体（留空 = 用 TMP 默认字体）")]
    public TMP_FontAsset font;

    [Header("文案")]
    [TextArea(2, 3)] public string talentText = "点这里去解锁新招式";
    [TextArea(2, 3)] public string stageText = "点「开始关卡」进入战斗";

    private RectTransform _layer;
    private Image _hand;
    private Image _ring;
    private TextMeshProUGUI _label;
    private RectTransform _labelBg;
    private float _animTime;
    private string _currentText;

    private void Update()
    {
        var mode = ResolveMode();
        if (mode == Mode.None) { Hide(); MaskService.Clear(this); return; }
        EnsureUI();
        var anchor = ResolveAnchor(mode);
        if (anchor == null) { Hide(); MaskService.Clear(this); return; }
        Show(anchor, mode == Mode.Talent ? talentText : stageText);
        Animate();

        // ★ **聚光遮罩**（统一遮罩系统）：一整块遮罩铺满引导层；字幕板与要点的那个按钮都在它上面
        //   （按钮由规则里的 Exempt 提到引导层里 → 不被压暗、也能点到）。
        var maskTarget = ResolveMaskTarget(mode);
        var rule = new MaskRule
        {
            Host = _layer,
            Alpha = MaskDimAlpha,
            BlockClicks = true,
            FadeSeconds = MaskFadeSeconds
        };
        rule.Exempt.Add(maskTarget != null ? maskTarget : anchor);
        MaskService.Apply(this, rule);
    }

    /// <summary>聚光遮罩的压暗强度。</summary>
    private const float MaskDimAlpha = 0.45f;
    private const float MaskFadeSeconds = 0.12f;

    /// <summary>遮罩开洞的目标 = **可点的那个按钮**（不是它的图片）→ 整张按钮亮着。</summary>
    private static RectTransform ResolveMaskTarget(Mode mode)
    {
        if (mode == Mode.Talent)
        {
            var entry = FindObjectOfType<HubTalentEntry>();
            var btn = entry != null ? entry.GetComponent<UnityEngine.UI.Button>() : null;
            if (btn != null) return btn.transform as RectTransform;
            return entry != null ? entry.transform as RectTransform : null;
        }
        var start = FindObjectOfType<BattleStartStageButton>();
        var startBtn = start != null ? start.GetComponent<UnityEngine.UI.Button>() : null;
        if (startBtn != null) return startBtn.transform as RectTransform;
        return start != null ? start.transform as RectTransform : null;
    }

    private enum Mode { None, Talent, Stage }

    /// <summary>现在该引导哪一步。</summary>
    private Mode ResolveMode()
    {
        var data = SaveManager.Load();
        if (data == null || !data.tutorialCompleted) return Mode.None;   // 教学关还没打完
        // 只在**落地页**（路线 Ready）才引导 → 进了战斗就不再指「开始关卡」
        var route = RouteFlowHub.Current as FakeRouteRuntime;
        bool atLanding = route == null || route.Phase == FakeRoutePhase.Ready;
        if (!atLanding)
        {
            // 玩家已经点了开始关卡（进了战斗）→ 标记「这段引导已完成」→ 同一教程里不再出现
            if (!TalentSaveSection.IsStartStageTaught(data) && data.taughtTalentPanel)
            {
                TalentSaveSection.MarkStartStageTaught(data);
                SaveManager.Save(data);
            }
            return Mode.None;
        }
        // 天赋页开着 → 那一段由天赋页自己的教学接管（指 C2 节点）→ 落地页这条收起来
        if (IsTalentPanelOpen()) return Mode.None;
        // 天赋页还没教过 → 先去天赋；教过了 → 去开打
        if (!data.taughtTalentPanel) return Mode.Talent;
        return TalentSaveSection.IsStartStageTaught(data) ? Mode.None : Mode.Stage;
    }

    /// <summary>天赋页（overlay）是否开着 —— 开着的���候由**天赋页自己的教学**接管（指 C2 节点），
    /// 落地页这条引导要收起来（否则那只手会浮在天赋面板上面，指着一个被面板挡住的东西）。</summary>
    private static bool IsTalentPanelOpen()
    {
        var panel = Object.FindObjectOfType<TalentTreePanel>();
        return panel != null && panel.gameObject.activeInHierarchy;
    }

    /// <summary>要指向的那个 UI 元素的**视觉中心**（按钮容器 ≠ 按钮图案）。</summary>
    private RectTransform ResolveAnchor(Mode mode)
    {
        RectTransform target = null;
        if (mode == Mode.Talent)
        {
            var entry = FindObjectOfType<HubTalentEntry>();
            if (entry == null || !entry.gameObject.activeInHierarchy) return null;
            target = entry.transform as RectTransform;      // 指整张卡（它的视觉中心）
        }
        else
        {
            var btn = FindObjectOfType<BattleStartStageButton>();
            if (btn == null || !btn.gameObject.activeInHierarchy) return null;
            target = btn.transform as RectTransform;
        }
        // 容器本身常常是「全屏的按钮根」→ 要指它里面的**图案**（例如「开始关卡」的红字）
        if (target != null)
        {
            var img = target.GetComponent<UnityEngine.UI.Image>();
            if (img == null)
            {
                var child = target.GetComponentInChildren<UnityEngine.UI.Image>(true);
                if (child != null) target = child.rectTransform;
            }
        }
        return target;
    }

    private void EnsureUI()
    {
        if (_layer != null) return;
        var entry = FindObjectOfType<HubTalentEntry>();
        var btn = FindObjectOfType<BattleStartStageButton>();
        var host = (entry != null ? entry.transform : (btn != null ? btn.transform : transform)) as RectTransform;
        if (host == null) return;
        var canvas = host.GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var layerGo = new GameObject("HubTutorialGuide", typeof(RectTransform));
        layerGo.transform.SetParent(canvas.transform, false);
        var lr = (RectTransform)layerGo.transform;
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
        // **自建一个高优先级的 Canvas**：
        //   ① Hub 的底部 tab 栏在另一个 canvas（order 50）上 → 挂在 HubCanvas 下会被盖住（截图确认）；
        //   ② 挂在别人的 Canvas 下时 TMP 文字可能不渲染（字是空的）。
        var own = layerGo.AddComponent<Canvas>();
        own.overrideSorting = true;
        own.sortingOrder = 60;
        // **必须有 GraphicRaycaster**：引导层的**点击遮罩**靠它接射线
        //   （没有它 → 遮罩存在但收不到点击 → 等于没有遮罩）。
        //   手 / 光圈 / 文字的 `raycastTarget` 都是 false → 它们不会抢点击 ✓。
        if (layerGo.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            layerGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var scaler = layerGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2160f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        _layer = lr;

        // ★ **遮罩要最先创建**（2026-11-06 实测）：运行时新建的 Graphic 如果排在别的图形之后，
        //   会进不了 canvas 的渲染批次 → 半透明压暗**画不出来**（不透明色却正常）。
        //   → 遮罩面板必须是本 canvas 的**第一个图形** → 字幕板 / 光圈 / 手 都在它上面。
        //   （统一遮罩系统 UiMask.Create() 自己就是第一个子物体 ✓）

        _ring = MakeImage("GuideRing", new Color(1f, 0.86f, 0.4f, 0.9f),
            ringSprite != null ? ringSprite : ProceduralUiSprites.Ring(128, 12, 1f));
        _hand = MakeImage("GuideHand", Color.white, handSprite);
        _label = MakeText("GuideLabel");
        _labelBg = _label != null ? _label.transform.parent as RectTransform : null;

        // ★ **不再用“高亮材质”**（2026-11-06 用户：「本质上来说确实不需要高亮材质，
        //   而是让对应按钮显示在遮罩上层即可」）→ 手与光圈**不画**了：
        //   高亮效果 = **遮罩在目标按钮处开洞** → 按钮原样亮着，其余压暗。
        if (_ring != null) _ring.gameObject.SetActive(false);
        if (_hand != null) _hand.gameObject.SetActive(false);
    }

    private Image MakeImage(string name, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(_layer, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI MakeText(string name)
    {
        // 底板（深色）→ 白字在浅色背景上也看得清
        var bgGo = new GameObject(name + "Bg", typeof(RectTransform));
        bgGo.transform.SetParent(_layer, false);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.04f, 0.88f);
        bg.sprite = ProceduralUiSprites.RoundedPlate(18, 3, 1f, 1f);
        bg.type = Image.Type.Sliced;
        bg.raycastTarget = false;
        var br = (RectTransform)bgGo.transform;
        br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
        br.pivot = new Vector2(0.5f, 0.5f);
        br.sizeDelta = new Vector2(820f, 96f);
        br.anchoredPosition = new Vector2(0f, 0f);

        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(bgGo.transform, false);
        var txt = go.AddComponent<TextMeshProUGUI>();
        // 字体必须给（不设的话 TMP 什么都不画 —— 2026-11-04 截图发现主菜单引导的字是空的）
        if (font != null) txt.font = font;
        else if (TMP_Settings.defaultFontAsset != null) txt.font = TMP_Settings.defaultFontAsset;
        txt.raycastTarget = false;
        txt.fontSize = 34f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(1f, 0.94f, 0.82f, 1f);
        txt.enableWordWrapping = true;
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(860f, 96f);
        r.anchoredPosition = Vector2.zero;
        return txt;
    }

    /// <summary>
    /// 目标 UI 元素的**视觉中心**（世界坐标）。
    /// ⚠️ 不能用 `rectTransform.position` —— 那是 **pivot** 的位置（很多 UI 的 pivot 在角上，比如「开始关卡」按钮的 pivot 在**顶边中点**）
    /// → 指针会偏一整块（实测偏 129px，2026-11-04 用户反馈「不在对应按钮的正中心」）。
    /// </summary>
    private static Vector3 VisualCenter(RectTransform rt)
    {
        if (rt == null) return Vector3.zero;
        return rt.TransformPoint(rt.rect.center);
    }

    private void Show(RectTransform anchor, string text)
    {
        if (_layer == null) return;
        _layer.gameObject.SetActive(true);
        // Hide() 不再关 canvas（避免渲染批次失效）→ 这里只把文字显示出来
        if (_label != null) _label.gameObject.SetActive(true);
        if (_labelBg != null) _labelBg.gameObject.SetActive(true);
        if (_currentText != text) { _currentText = text; if (_label != null) _label.text = text; }

        Vector3 world = VisualCenter(anchor);
        _targetWorld = world;
        float handSize = 190f;
        float ringSize = 150f;

        // 屏幕安全区（引导层是全屏 Canvas，原点在中心）
        float halfW = Screen.width * 0.5f;
        float halfH = Screen.height * 0.5f;
        float margin = ringSize * 0.5f + 10f;
        Vector3 local = _layer.InverseTransformPoint(world);
        // 目标在屏幕边上时，把**指尖**夹回安全区 → 否则手只露出一半（截图确认）
        local.x = Mathf.Clamp(local.x, -halfW + margin, halfW - margin);
        local.y = Mathf.Clamp(local.y, -halfH + margin, halfH - margin);
        _targetLocal = local;

        if (_ring != null)
        {
            var rr = _ring.rectTransform;
            rr.sizeDelta = new Vector2(ringSize, ringSize);
            rr.pivot = Vector2.one * 0.5f;
            rr.anchorMin = rr.anchorMax = new Vector2(0.5f, 0.5f);
            rr.anchoredPosition = local;
        }
        PlaceHandOnTarget(handSize);

        // 文案：**靠近手势，但既不遮手势、也不遮它指的按钮**
        if (_labelBg != null)
        {
            Vector2 lblPos = TutorialGestureStyle.PlaceLabelNear(_layer, _layer.TransformPoint(local), new Vector2(700f, 96f), handSize);
            _labelBg.anchoredPosition = lblPos;
        }
    }

    /// <summary>
    /// 把手的**指尖**钉在目标上（位置随按压动画的反算）。
    /// 按压动画会绕**掌心**旋转 → 不反算的话指尖会被甩离目标（实测偏 44,-68px）。
    /// </summary>
    private void PlaceHandOnTarget(float handSize)
    {
        if (_hand == null || _hand.sprite == null) return;
        var hr = _hand.rectTransform;
        hr.sizeDelta = new Vector2(handSize, handSize);
        hr.pivot = TutorialGestureStyle.HandPivot;
        hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0.5f);

        // 指尖相对 pivot（掌心）的偏移 → 旋转+缩放后 → 抵消掉
        Vector3 tipOffset = new Vector3(
            (TutorialGestureStyle.TipUV.x - hr.pivot.x) * handSize,
            (TutorialGestureStyle.TipUV.y - hr.pivot.y) * handSize, 0f);
        tipOffset = hr.localRotation * tipOffset;
        tipOffset = new Vector3(tipOffset.x * hr.localScale.x, tipOffset.y * hr.localScale.y, 0f);
        hr.anchoredPosition = _targetLocal - tipOffset;
    }

    private Vector3 _targetWorld;
    private Vector3 _targetLocal;

    private void Animate()
    {
        if (_hand == null && _ring == null) return;
        _animTime += Time.unscaledDeltaTime;
        float cycle = 1.1f;
        float phase = (_animTime % cycle) / cycle;
        float press = phase < 0.35f
            ? Mathf.SmoothStep(0f, 1f, phase / 0.35f)
            : 1f - Mathf.SmoothStep(0f, 1f, (phase - 0.35f) / 0.65f);
        if (_hand != null)
        {
            _hand.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f * press);   // 绕掌心压下去
            _hand.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.94f, press);
            PlaceHandOnTarget(190f);   // 旋转会把指尖甩离目标 → 每帧反算位置把它钉回去
        }
        if (_ring != null)
        {
            float breathe = 1f + Mathf.Sin(_animTime * 3.2f) * 0.08f;
            // ⚠️ 缩放是**倍率**，不能再乘一次尺寸（乘两次 → 光圈变成 150×150 像素的巨物 → 看着像没有）
            _ring.rectTransform.localScale = Vector3.one * breathe;
            _ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _animTime * 30f);
        }
    }

    private void Hide()
    {
        _currentText = null;
        // ★ **不关引导层 canvas**（只隐藏里面的字/图标）：
        //   反复 SetActive(false/true) 会让这个 canvas 的渲染批次失效
        //   → 之后新建的半透明 UI（聚光遮罩）就**画不出来**（实测 2026-11-06）。
        if (_layer == null) return;
        if (_label != null) _label.gameObject.SetActive(false);
        if (_labelBg != null) _labelBg.gameObject.SetActive(false);
        if (_ring != null) _ring.gameObject.SetActive(false);
        if (_hand != null) _hand.gameObject.SetActive(false);
    }
}
