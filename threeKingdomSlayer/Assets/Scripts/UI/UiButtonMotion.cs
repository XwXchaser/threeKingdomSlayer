using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// **按钮动效**（落地页用，可复用到任何 UI 按钮）：
///   • 常态：呼吸缩放 + 上下微浮，循环播放；
///   • 按下：立刻压扁 + 下沉，松手回弹（带一点过冲）。
///
/// 实现约定（踩过的坑都在这里）：
///   • 缩放与位置**只由 DOTween 驱动**，常态 tween 与按下 tween **互斥**：
///     按下时先 Kill 常态 tween，松手的回弹 tween 结束后再恢复常态，
///     否则两个 tween 抢同一个属性 → 抖动 / 卡在中间值。
///   • 所有 tween 都 <c>SetLink(目标)</c>：目标被销毁时自动 Kill，
///     不会在回调里访问已销毁的 Transform（见 memory/dotween-destroyed-transform）。
///   • 目标默认是按钮自身；若按钮根节点上有布局逻辑，把视觉件放到子节点并指定 <see cref="target"/>。
///   • 用 <see cref="IPointerDownHandler"/> 而不是 <c>onClick</c>：
///     按下到松手之间就要有反馈，不能等点击完成。
/// </summary>
[RequireComponent(typeof(Button))]
public class UiButtonMotion : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("作用对象")]
    [Tooltip("缩放/浮动作用的对象，留空 = 按钮自身")]
    public RectTransform target;

    [Header("常态")]
    public bool idleEnabled = true;
    [Tooltip("呼吸峰值缩放（1 = 不缩放）")]
    public float idleScale = 1.03f;
    [Tooltip("一次呼吸的周期（秒）")]
    public float idlePeriod = 1.6f;
    [Tooltip("上下浮动的幅度（像素）")]
    public float idleBob = 3f;

    [Header("按下")]
    [Tooltip("按下时的缩放")]
    public float pressScale = 0.93f;
    [Tooltip("按下时下沉的距离（像素）")]
    public float pressOffset = 4f;
    [Tooltip("按下压扁的时长（秒）")]
    public float pressTime = 0.06f;
    [Tooltip("松手回弹的时长（秒）")]
    public float releaseTime = 0.2f;

    private Button _button;
    private RectTransform _rt;
    private Vector3 _baseScale;
    private Vector2 _basePos;
    private Tween _idleScaleTween;
    private Tween _idlePosTween;
    private Tween _pressScaleTween;
    private Tween _pressPosTween;
    private bool _pressed;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _rt = target != null ? target : transform as RectTransform;
        if (_rt == null) return;
        _baseScale = _rt.localScale;
        _basePos = _rt.anchoredPosition;
    }

    private void OnEnable()
    {
        if (_rt == null) return;
        StartIdle();
    }

    private void OnDisable()
    {
        KillAll();
        if (_rt == null) return;
        _rt.localScale = _baseScale;
        _rt.anchoredPosition = _basePos;
        _pressed = false;
    }

    // ─────────────────────────── 常态 ───────────────────────────

    private void StartIdle()
    {
        if (!idleEnabled || _rt == null) return;
        KillIdle();
        float half = Mathf.Max(0.05f, idlePeriod * 0.5f);

        if (idleScale > 0.001f)
        {
            _idleScaleTween = _rt.DOScale(_baseScale * idleScale, half)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(_rt.gameObject);
        }

        if (idleBob > 0.001f)
        {
            _idlePosTween = _rt.DOAnchorPos(_basePos + new Vector2(0f, idleBob), half)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(_rt.gameObject);
        }
    }

    private void KillIdle()
    {
        if (_idleScaleTween != null) { _idleScaleTween.Kill(); _idleScaleTween = null; }
        if (_idlePosTween != null) { _idlePosTween.Kill(); _idlePosTween = null; }
    }

    private void KillAll()
    {
        KillIdle();
        if (_pressScaleTween != null) { _pressScaleTween.Kill(); _pressScaleTween = null; }
        if (_pressPosTween != null) { _pressPosTween.Kill(); _pressPosTween = null; }
    }

    // ─────────────────────────── 按下 ───────────────────────────

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_rt == null) return;
        if (_button != null && !_button.interactable) return;   // 禁用按钮不给反馈
        _pressed = true;

        KillIdle();                                            // 常态 tween 让位给按下 tween
        if (_pressScaleTween != null) _pressScaleTween.Kill();
        if (_pressPosTween != null) _pressPosTween.Kill();

        _pressScaleTween = _rt.DOScale(_baseScale * pressScale, pressTime)
            .SetEase(Ease.OutQuad).SetLink(_rt.gameObject);
        _pressPosTween = _rt.DOAnchorPos(_basePos + new Vector2(0f, -pressOffset), pressTime)
            .SetEase(Ease.OutQuad).SetLink(_rt.gameObject);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_pressed || _rt == null) return;
        _pressed = false;

        if (_pressScaleTween != null) { _pressScaleTween.Kill(); _pressScaleTween = null; }
        if (_pressPosTween != null) { _pressPosTween.Kill(); _pressPosTween = null; }

        var scaleBack = _rt.DOScale(_baseScale, releaseTime)
            .SetEase(Ease.OutBack).SetLink(_rt.gameObject);
        _rt.DOAnchorPos(_basePos, releaseTime)
            .SetEase(Ease.OutCubic)
            .SetLink(_rt.gameObject)
            .OnComplete(StartIdle);                            // 回弹结束后再恢复常态，避免抢属性
    }
}
