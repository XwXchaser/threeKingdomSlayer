using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public sealed class RouteChoiceButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup visualGroup;
    [SerializeField] private float guideMoveDistance = 10f;
    [SerializeField] private float guideCycleDuration = 0.9f;
    [SerializeField] private float breathingAlpha = 0.12f;
    [SerializeField] private float breathingScale = 0.025f;
    /// <summary>
    /// 是否让引导呼吸把按钮**变暗**（透明度呼吸）。
    ///
    /// ⚠️ **默认 false**（2026-11-09 用户反馈：「点移动按钮还是会变暗」）：
    ///   按钮的底板是 targetGraphic，ColorTint 的 pressed 色（C8C8C8）会让**白色底板**变灰，
    ///   叠在透明度呼吸（1↔0.88）上就是「按一下整块暗掉」→ 看着像被别的东西盖住。
    ///   现在：按下不变暗（pressed=normal）+ 呼吸不再动透明度 → 按钮**恒亮**，只保留位置/缩放的引导。
    /// </summary>
    [SerializeField] private bool breatheAlpha = false;
    [NonSerialized] public Text fallbackText;

    /// <summary>本按钮绑定的路线选项 id（= RouteChoiceOption.id，例：J1_N2）。</summary>
    public string OptionId => _id;

    /// <summary>可点的那个 Button（教学要挂在**它**的物体上才能收到按下事件）。</summary>
    public Button Button => button;

    private string _id;
    private Action<string> _onSelected;
    /// <summary>是否已经点过（防重复点击；**不动 interactable**，否则按钮会变暗）。</summary>
    private bool _consumed;
    private RectTransform _rectTransform;
    private RectTransform _iconRectTransform;
    private Vector2 _baseAnchoredPosition;
    private Vector3 _baseLocalScale;
    private LayoutElement _layoutElement;
    private Sequence _visualSequence;
    private Coroutine _visualStartCoroutine;

    public void Bind(RouteChoiceOption option, Action<string> onSelected)
    {
        _id = option.id;
        _onSelected = onSelected;
        _consumed = false;
        if (labelText != null) labelText.text = option.label;
        if (descriptionText != null)
        {
            descriptionText.text = option.description;
            descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(option.description));
        }
        if (iconImage != null)
        {
            iconImage.sprite = option.icon;
            iconImage.gameObject.SetActive(option.icon != null);
        }
        if (fallbackText != null) fallbackText.text = option.label;
        if (button == null) button = GetComponent<Button>();
        EnsureVisualSetup();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(Select);
            // 锁定的出口不可点（原因已显示在副标题上）。
            button.interactable = !option.locked;
            // **按下不变暗**（2026-11-09 用户反馈）：ColorTint 的 pressed/disabled 色会让底板变灰，
            // 看着像「按钮被什么盖住了」。引导感由呼吸位移/缩放提供，不靠变色。
            var colors = button.colors;
            colors.pressedColor = colors.normalColor;
            colors.disabledColor = colors.normalColor;
            button.colors = colors;
        }
        StartVisualLoopNextFrame();
    }

    private void StartVisualLoopNextFrame()
    {
        _visualSequence?.Kill();
        if (_visualStartCoroutine != null)
            StopCoroutine(_visualStartCoroutine);
        _visualStartCoroutine = StartCoroutine(StartVisualLoopCoroutine());
    }

    private System.Collections.IEnumerator StartVisualLoopCoroutine()
    {
        yield return null;
        StartVisualLoop();
    }

    private void Awake()
    {
        EnsureVisualSetup();
    }

    private void EnsureVisualSetup()
    {
        if (_rectTransform == null)
            _rectTransform = transform as RectTransform;
        if (_iconRectTransform == null && iconImage != null)
            _iconRectTransform = iconImage.rectTransform;
        if (visualGroup == null)
        {
            visualGroup = GetComponent<CanvasGroup>();
            if (visualGroup == null)
                visualGroup = gameObject.AddComponent<CanvasGroup>();
        }
        if (_layoutElement == null)
        {
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
                _layoutElement = gameObject.AddComponent<LayoutElement>();
        }
    }

    private void OnDestroy()
    {
        _visualSequence?.Kill();
    }

    private void StartVisualLoop()
    {
        EnsureVisualSetup();
        if (_rectTransform == null || visualGroup == null) return;
        Canvas.ForceUpdateCanvases();
        _visualSequence?.Kill();
        _baseAnchoredPosition = _rectTransform.anchoredPosition;
        _baseLocalScale = _rectTransform.localScale;
        visualGroup.alpha = 1f;

        float cycleDuration = Mathf.Max(0.05f, guideCycleDuration);
        Vector2 guideDirection = GetGuideDirection();
        Vector2 guideTarget = _baseAnchoredPosition + guideDirection * guideMoveDistance;
        _visualSequence = DOTween.Sequence().SetTarget(this).SetUpdate(true);
        _visualSequence.Append(_rectTransform.DOAnchorPos(guideTarget, cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.Append(_rectTransform.DOAnchorPos(_baseAnchoredPosition, cycleDuration).SetEase(Ease.InOutSine));
        if (breatheAlpha)
            _visualSequence.Join(visualGroup.DOFade(1f - breathingAlpha, cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.Join(_rectTransform.DOScale(_baseLocalScale * (1f + breathingScale), cycleDuration).SetEase(Ease.InOutSine));
        _visualSequence.SetLoops(-1, LoopType.Restart);
    }

    private Vector2 GetGuideDirection()
    {
        if (_iconRectTransform == null || _rectTransform.parent == null)
            return Vector2.up;

        Vector3 worldDirection = _iconRectTransform.TransformDirection(Vector3.up);
        Vector3 parentDirection = _rectTransform.parent.InverseTransformDirection(worldDirection);
        Vector2 direction = new Vector2(parentDirection.x, parentDirection.y);
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;
    }

    public void ApplyLayout(RouteChoiceLayout layout, int index, int count)
    {
        EnsureVisualSetup();
        if (_rectTransform == null) return;
        Vector2 position = GetDefaultPosition(index, count);
        float rotation = 0f;
        if (layout != null && layout.overrideDefault)
        {
            position = layout.anchoredPosition;
            rotation = layout.rotation;
        }
        _rectTransform.anchoredPosition = position;
        _rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
    }

    private static Vector2 GetDefaultPosition(int index, int count)
    {
        if (count <= 1) return Vector2.zero;
        float spacing = 160f;
        return new Vector2((index - (count - 1) * 0.5f) * spacing, 0f);
    }

    private void Select()
    {
        // 防重复点击：**不再用 `button.interactable = false`** ——
        //   Button 的 Color Tint 过渡会把目标 Image 立刻切到 **disabled 色**（变暗 + 半透明），
        //   而路牌面板在选完之后**不会立刻隐藏**（要等旅行开始才 HideCurrent）→
        //   玩家会看到「点一下下一节点按钮，它就暗一下」（2026-11-08 用户反馈）。
        //   改用自己的 _consumed 标志挡重复点击 → 视觉完全不动。
        if (_consumed) return;
        _consumed = true;
        _visualSequence?.Kill();
        ResetVisualToBase();
        _onSelected?.Invoke(_id);
    }

    /// <summary>把按钮外观恢复到基准状态（位置 / 缩放 / 透明度）。</summary>
    private void ResetVisualToBase()
    {
        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _baseAnchoredPosition;
            _rectTransform.localScale = _baseLocalScale;
        }
        if (visualGroup != null) visualGroup.alpha = 1f;
    }
}
