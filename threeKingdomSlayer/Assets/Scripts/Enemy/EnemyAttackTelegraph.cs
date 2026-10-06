using DG.Tweening;
using UnityEngine;

/// <summary>
/// 敌人攻击前的通用视觉预警。
/// 组件挂在敌人 Prefab 的 AttackTelegraphAnchor 上，Transform 位置由各敌人 Prefab 单独调整。
/// 正式预警由 Inspector 直接引用的三张透明 Sprite 按序播放；Unity 不运行时绘制图案。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyAttackTelegraph : MonoBehaviour
{
    public const int FrameCount = 3;

    [Header("预警视觉")]
    [Tooltip("正式三帧预警 Sprite：棱形点、75%展开十字星、十字星加外侧圆环。")]
    [SerializeField] private Sprite[] telegraphFrames = new Sprite[FrameCount];
    [Tooltip("Edit Mode 下显示的预览帧，0=棱形点，1=展开十字星，2=十字星加外侧圆环。")]
    [SerializeField, Range(0, FrameCount - 1)] private int previewFrame;
    [Tooltip("Edit Mode 下是否显示当前预览帧；关闭后隐藏预警 Sprite。")]
    [SerializeField] private bool showPreview;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.86f, 0.24f, 1f);
    [SerializeField, Range(0.05f, 1f)] private float startScaleMultiplier = 0.28f;
    [SerializeField, Range(0.5f, 2f)] private float peakScaleMultiplier = 1.08f;
    [SerializeField, Range(0f, 2f)] private float peakAlpha = 1f;

    [Header("帧节奏")]
    [SerializeField, Range(0.05f, 0.8f)] private float frame1DurationRatio = 0.25f;
    [SerializeField, Range(0.05f, 0.8f)] private float frame2DurationRatio = 0.30f;

    [Header("层级")]
    [Tooltip("相对敌人主体 SpriteRenderer 的排序层级偏移；1 = 敌人之上一级。")]
    [SerializeField] private int sortingOrderOffset = 1;

    [Header("音效")]
    [SerializeField] private bool playSound = true;

    private SpriteRenderer _renderer;
    private Enemy _enemy;
    private SpriteRenderer _enemyRenderer;
    private Sequence _sequence;
    private Vector3 _baseLocalScale;
    private Color _baseRendererColor;
    private float _animatedAlpha;
    private float _parentAlpha = 1f;
    private bool _initialized;

    public bool IsPlaying => _sequence != null && _sequence.IsActive();

    public void SetParentAlpha(float alpha)
    {
        _parentAlpha = Mathf.Clamp01(alpha);
        ApplyAlpha();
    }

    private void SetAnimatedAlpha(float alpha)
    {
        _animatedAlpha = Mathf.Clamp01(alpha);
        ApplyAlpha();
    }

    private void ApplyAlpha()
    {
        if (_renderer == null)
            return;

        Color color = _baseRendererColor;
        color.a = _animatedAlpha * _parentAlpha;
        _renderer.color = color;
    }

    private void Awake()
    {
        EnsureInitialized();
        if (Application.isPlaying)
            HideVisual();
        else
            ApplyPreview();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        if (!Application.isPlaying)
            ApplyPreview();
    }

    private void OnValidate()
    {
        previewFrame = Mathf.Clamp(previewFrame, 0, FrameCount - 1);
        frame1DurationRatio = Mathf.Clamp01(frame1DurationRatio);
        frame2DurationRatio = Mathf.Clamp01(frame2DurationRatio);
        EnsureInitialized();
        if (!Application.isPlaying)
            ApplyPreview();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            ApplyPreview();
    }

    private void EnsureInitialized()
    {
        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        if (_enemy == null)
            _enemy = GetComponentInParent<Enemy>();
        if (_enemyRenderer == null && _enemy != null)
            _enemyRenderer = _enemy.GetComponent<SpriteRenderer>();
        if (!_initialized)
        {
            _baseLocalScale = transform.localScale;
            _baseRendererColor = telegraphColor;
            _parentAlpha = 1f;
            _animatedAlpha = 0f;
            _initialized = true;
        }
        RefreshSorting();
    }

    private Sprite GetFrame(int index)
    {
        return telegraphFrames != null && index >= 0 && index < telegraphFrames.Length
            ? telegraphFrames[index]
            : null;
    }

    private void ApplyPreview()
    {
        EnsureInitialized();
        if (_renderer == null)
            return;

        _renderer.sprite = GetFrame(previewFrame);
        _renderer.color = telegraphColor;
        _renderer.enabled = showPreview && _renderer.sprite != null;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            StopWarning();
        else if (_renderer != null)
            _renderer.enabled = false;
    }

    public void BeginWarning(float duration)
    {
        if (_renderer == null)
            return;

        duration = Mathf.Max(0f, duration);
        if (duration <= 0.001f || GetFrame(0) == null || GetFrame(1) == null || GetFrame(2) == null)
        {
            StopWarning();
            return;
        }

        StopWarning();
        RefreshSorting();

        _renderer.enabled = true;
        _renderer.sprite = GetFrame(0);
        _animatedAlpha = 0f;
        ApplyAlpha();
        transform.localScale = _baseLocalScale * startScaleMultiplier;

        if (playSound)
            AudioManager.Instance?.PostEvent("Enemy_AttackTelegraph");

        float frame1Duration = duration * frame1DurationRatio;
        float frame2Duration = duration * frame2DurationRatio;
        float frame3Duration = Mathf.Max(0.001f, duration - frame1Duration - frame2Duration);
        float appearDuration = Mathf.Max(0.001f, frame1Duration * 0.8f);
        float fadeDuration = Mathf.Max(0.001f, frame3Duration * 0.25f);
        float frame3HoldDuration = Mathf.Max(0.001f, frame3Duration - fadeDuration);

        _sequence = DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, false);
        _sequence.Append(transform.DOScale(_baseLocalScale * peakScaleMultiplier, appearDuration)
            .SetEase(Ease.OutBack));
        _sequence.Join(DOTween.To(() => _animatedAlpha, SetAnimatedAlpha,
            Mathf.Clamp01(peakAlpha), appearDuration).SetEase(Ease.OutQuad));
        _sequence.AppendCallback(() => SetFrame(1));
        _sequence.AppendInterval(Mathf.Max(0.001f, frame2Duration - appearDuration));
        _sequence.AppendCallback(() => SetFrame(2));
        _sequence.AppendInterval(frame3HoldDuration);
        _sequence.Append(transform.DOScale(_baseLocalScale * 0.12f, fadeDuration)
            .SetEase(Ease.InQuad));
        _sequence.Join(DOTween.To(() => _animatedAlpha, SetAnimatedAlpha,
            0f, fadeDuration).SetEase(Ease.InQuad));
        _sequence.OnComplete(HideVisual);
        _sequence.OnKill(HideVisual);
    }

    private void SetFrame(int index)
    {
        if (_renderer != null)
            _renderer.sprite = GetFrame(index);
    }

    public void StopWarning()
    {
        Sequence sequence = _sequence;
        _sequence = null;
        if (sequence != null && sequence.IsActive())
            sequence.Kill(false);
        else
            HideVisual();
    }

    private void RefreshSorting()
    {
        if (_renderer == null)
            return;

        if (_enemy == null)
            _enemy = GetComponentInParent<Enemy>();
        if (_enemyRenderer == null && _enemy != null)
            _enemyRenderer = _enemy.GetComponent<SpriteRenderer>();

        if (_enemyRenderer != null)
        {
            _renderer.sortingLayerID = _enemyRenderer.sortingLayerID;
            _renderer.sortingOrder = _enemyRenderer.sortingOrder + sortingOrderOffset;
        }
        else
        {
            _renderer.sortingLayerName = "Default";
            _renderer.sortingOrder = sortingOrderOffset;
        }
    }

    private void HideVisual()
    {
        if (_sequence != null && _sequence.IsActive())
            return;

        if (_renderer != null)
        {
            _renderer.enabled = false;
            _animatedAlpha = 0f;
            ApplyAlpha();
        }
        transform.localScale = _baseLocalScale;
    }
}
