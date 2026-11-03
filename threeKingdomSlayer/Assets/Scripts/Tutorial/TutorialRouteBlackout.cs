using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **节点移动黑幕**（2026-11-01，教学关）。
///
/// 背景：教学关走卷轴路线，节点之间有一段「假移动」的过场 —— 之前**没有任何遮罩**，
/// 画面是硬切过去的（用户 11-01 提出「节点移动这段缺少黑幕效果」）。
///
/// **黑幕 = 半透明压暗**（默认 0.55），**不是**全屏纯黑：画面在转场时仍然看得见，
/// 不会黑屏一闪把玩家晃到。
///
/// 为什么不用现有的 <see cref="TutorialSceneDim"/>：它是「只压暗路线场景（背景/地形/植被）」
/// 的实现，靠改材质与精灵颜色，不是全屏遮罩，而且它的设计明确写了「不用全屏 UI 遮罩 ——
/// 那会把角色也遮住」。节点移动时画面里没有角色在打，这时用一层半透明黑幕正好。
///
/// 触发：只**监听**路线阶段，不改路线系统本体：
///   • <see cref="FakeRoutePhase.RouteChoiceTransition"/> / <see cref="FakeRoutePhase.FakeMoving"/> → 淡入黑幕；
///   • <see cref="FakeRoutePhase.ChoosingRoute"/> / <see cref="FakeRoutePhase.Battle"/> → 淡出（玩家要看路牌/要打）。
/// 淡入淡出用非缩放时间（timeScale=0 时也要能走完）。
/// </summary>
[DisallowMultipleComponent]
public class TutorialRouteBlackout : MonoBehaviour
{
    [Header("外观")]
    [Tooltip("黑幕 Image（场景里配好；留空则自动在本物体下建一个全屏 Image）")]
    public Image curtain;

    [Tooltip("黑幕不透明度（**半透明压暗**，不是全屏纯黑）")]
    [Range(0f, 1f)] public float maxAlpha = 0.55f;

    [Tooltip("淡入/淡出时长（秒，不受 timeScale 影响）")]
    public float fadeSeconds = 0.12f;

    [Header("作用范围")]
    [Tooltip("只对这条路线生效（留空 = 任何路线都遮）。教学关 = Tutorial_Route")]
    public FakeRouteStageConfig routeConfig;

    [Tooltip("只在教学关生效：离开教学路线时立刻恢复（不进黑）")]
    public bool tutorialOnly = true;

    private CanvasGroup _group;
    private float _alpha;
    private float _target;
    private readonly List<RouteChoiceButton> _hookedButtons = new List<RouteChoiceButton>();
    private bool _pressedCover;

    private void Awake()
    {
        EnsureCurtain();
        _group = curtain.GetComponent<CanvasGroup>();
        if (_group == null) _group = curtain.gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;      // 黑幕不拦点击（路牌按钮要点得到）
        _group.interactable = false;
        curtain.color = new Color(0f, 0f, 0f, maxAlpha);
    }

    private void OnDisable()
    {
        _target = 0f;
        _alpha = 0f;
        Apply();
    }

    private void Update()
    {
        // **遮罩由「按下路牌按钮」触发**（2026-11-01 用户反馈：「为什么移动节点还是在移动时有黑色遮罩，
        // 而不是点击按键时有黑色遮罩」→ 遮罩要跟着玩家的点击出现，而不是跟着路线阶段）。
        HookRouteChoiceButtons();

        // 节点开始移动 → **收掉遮罩**（移动过程要看得见，不遮）
        var rt = RouteFlowHub.Current;
        if (_pressedCover && rt != null && rt.Phase == FakeRoutePhase.FakeMoving) _pressedCover = false;

        _target = (_pressedCover || ShouldBeBlack()) ? 1f : 0f;
        if (!Mathf.Approximately(_alpha, _target))
        {
            float step = fadeSeconds <= 0.001f ? 1f : Time.unscaledDeltaTime / fadeSeconds;
            _alpha = Mathf.MoveTowards(_alpha, _target, step);
            Apply();
        }
    }

    /// <summary>
    /// 当前是否应该黑着。
    ///
    /// **只盖住「点完按钮 → 开始移动」这一小段**：
    /// 节点真正开始移动（FakeMoving）时遮罩就收掉 —— 移动过程要看得见（用户反馈：「节点移动时依然有黑幕」）。
    /// </summary>
    private bool ShouldBeBlack()
    {
        var runtime = RouteFlowHub.Current;
        if (runtime == null) return false;
        if (routeConfig != null && runtime.RouteConfig != routeConfig) return false;
        if (tutorialOnly && tutorialRouteConfig != null && runtime.RouteConfig != tutorialRouteConfig) return false;

        return runtime.Phase == FakeRoutePhase.RouteChoiceTransition;
    }

    /// <summary>
    /// 给路牌按钮挂一个「按下即遮罩」的监听（按钮是路线系统运行时生成的，所以要现找现挂）。
    /// </summary>
    private void HookRouteChoiceButtons()
    {
        var runtime = RouteFlowHub.Current;
        if (runtime == null) return;
        if (runtime.Phase != FakeRoutePhase.ChoosingRoute)
        {
            if (_hookedButtons.Count > 0) _hookedButtons.Clear();
            return;
        }

        var buttons = FindObjectsOfType<RouteChoiceButton>();
        if (buttons == null) return;
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null || _hookedButtons.Contains(btn)) continue;
            _hookedButtons.Add(btn);
            // 挂在**可点的那个 Button** 的物体上（路牌的可点区域可能是子物体）
            var target = btn.Button != null ? btn.Button.gameObject : btn.gameObject;
            var trigger = target.GetComponent<TutorialBlackoutPointerTrigger>();
            if (trigger == null) trigger = target.AddComponent<TutorialBlackoutPointerTrigger>();
            trigger.blackout = this;
        }
    }

    /// <summary>玩家按下路牌 → 立刻开始遮罩（不等路线阶段切换）。</summary>
    public void ShowNow()
    {
        _pressedCover = true;
        _target = 1f;
    }

    /// <summary>教学路线配置（可留空 = 不限定教学路线）。</summary>
    public FakeRouteStageConfig tutorialRouteConfig;

    private void Apply()
    {
        if (_group == null) return;
        _group.alpha = Mathf.Clamp01(_alpha) * maxAlpha;
    }

    /// <summary>没有配 curtain 就自己建一个全屏黑 Image。</summary>
    private void EnsureCurtain()
    {
        if (curtain != null) return;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;

        var go = new GameObject("Tutorial Blackout Curtain", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        curtain = img;
        // 置顶：黑幕要盖住一切
        go.transform.SetAsLastSibling();
    }
}
