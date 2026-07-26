using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// QTE 显示管理器 — 挂载到 Canvas
/// 简化 QTE 动画：指示器入场 → 即进入判定窗口 → 结果退场
/// 已移除填充/ghost 预警机制，指示器出现即判定
/// </summary>
public class QTEDisplay : MonoBehaviour
{
    [Header("框架")]
    [Tooltip("QTE 判定框 RectTransform（含 RectMask2D），运行时由 BattleHUD 注入")]
    public RectTransform qteFrameRect;
    [Tooltip("QTE 指示器生成区域（QTEFrame 下的空节点），运行时由 BattleHUD 注入")]
    public RectTransform qteIndicatorArea;

    [Header("动画参数")]
    [Tooltip("入场动画时长（秒）")]
    public float slideInDuration = 0.15f;
    [Tooltip("退场动画时长（秒）")]
    public float slideOutDuration = 0.3f;
    [Tooltip("入场起始偏移（像素，frame 上方）")]
    public float slideInOffsetY = 200f;

    [Header("默认指示器")]
    [Tooltip("QTE 指示器默认 prefab（当 QTEConfig 未指定时使用）")]
    public GameObject defaultClickIndicatorPrefab;
    public GameObject defaultSwipeIndicatorPrefab;

    [Header("结果特效")]
    [Tooltip("成功特效 prefab")]
    public GameObject successEffectPrefab;
    [Tooltip("失败特效 prefab")]
    public GameObject failureEffectPrefab;
    [Tooltip("结果特效持续时间")]
    public float resultEffectDuration = 0.5f;

    [Header("结果反馈")]
    [Tooltip("QTE 成功结果图")]
    public Sprite successResultSprite;
    [Tooltip("QTE 失败结果图")]
    public Sprite failureResultSprite;
    [Tooltip("结果图显示总时长（秒）")]
    [SerializeField] private float resultFeedbackDuration = 0.42f;
    [Tooltip("结果图在 QTE 图案右侧的偏移（像素）")]
    [SerializeField] private float resultFeedbackOffsetX = 175f;

    [Header("严格模式提示")]
    [SerializeField] private TMP_Text _strictModePrompt;
    [SerializeField] private float _strictModePromptDuration = 0.45f;
    private Tween _strictModePromptTween;

    private class IndicatorState
    {
        public GameObject gameObject;
        public Image indicatorImage;
        public Tween animationTween;
        public bool landed;           // 指示器已落位（入场完成）
    }

    private class DyingIndicator
    {
        public GameObject gameObject;
        public Tween moveTween;
        public Tween fadeTween;
    }

    private List<IndicatorState> _activeStates = new List<IndicatorState>();
    private List<DyingIndicator> _dyingIndicators = new List<DyingIndicator>();
    private RectTransform _fallbackParent;
    private float _uiScale;
    private GameObject _resultFeedback;
    private Tween _resultFeedbackTween;

    private void Awake()
    {
        _uiScale = UIResolutionHelper.UIScale;
        slideInOffsetY *= _uiScale;

        var canvas = GetComponent<Canvas>();
        if (canvas != null)
            _fallbackParent = canvas.GetComponent<RectTransform>();
    }

    private RectTransform GetIndicatorParent()
    {
        EnsureHeroHudReferences();
        if (qteIndicatorArea != null) return qteIndicatorArea;
        return _fallbackParent;
    }

    private void EnsureHeroHudReferences()
    {
        if (qteFrameRect != null && qteIndicatorArea != null) return;

        var heroHud = UnityEngine.Object.FindObjectOfType<HeroHUD>(true);
        if (heroHud == null) return;

        qteFrameRect = heroHud.qteFrameRect;
        qteIndicatorArea = heroHud.qteIndicatorArea;
        if (_strictModePrompt == null)
            _strictModePrompt = heroHud.strictModePrompt;
    }

    // ═══════════════════════════════════════════
    //  公开 API
    // ═══════════════════════════════════════════

    public void ShowStrictModePrompt()
    {
        EnsureHeroHudReferences();
        if (_strictModePrompt == null) return;

        _strictModePromptTween?.Kill();
        _strictModePrompt.gameObject.SetActive(true);
        _strictModePrompt.alpha = 1f;
        _strictModePromptTween = _strictModePrompt.DOFade(0f, _strictModePromptDuration)
            .SetUpdate(true)
            .OnComplete(() => _strictModePrompt.gameObject.SetActive(false));
    }

    /// <summary>
    /// 生成 QTE 指示器：入场动画后即进入判定状态（不再有填充预警）
    /// </summary>
    public GameObject StartQTEIndicator(QTEConfig config)
    {
        EnsureHeroHudReferences();

        GameObject prefab = config.qteIndicatorPrefab != null
            ? config.qteIndicatorPrefab
            : GetDefaultPrefab(config.qteType);

        if (prefab == null) return null;

        var parent = GetIndicatorParent();
        GameObject indicator = Instantiate(prefab, parent);
        var rt = indicator.GetComponent<RectTransform>();

        var state = new IndicatorState();
        state.gameObject = indicator;

        state.indicatorImage = indicator.GetComponent<Image>();
        if (state.indicatorImage == null)
            state.indicatorImage = indicator.GetComponentInChildren<Image>();

        // 指示器居中于 frame
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.sizeDelta = config.indicatorSize;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        // 指示器 Image：完全可见，raycastTarget
        if (state.indicatorImage != null)
        {
            state.indicatorImage.color = new Color(1f, 1f, 1f, 1f);
            state.indicatorImage.raycastTarget = true;
            if (state.indicatorImage.type == Image.Type.Filled)
            {
                state.indicatorImage.fillAmount = 1f;
                state.indicatorImage.type = Image.Type.Simple;
            }
        }

        // 入场：从上方滑入
        float targetY = rt != null ? rt.anchoredPosition.y : 0f;
        if (rt != null)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, slideInOffsetY);

        if (rt != null && slideInDuration > 0f)
        {
            state.animationTween = rt.DOAnchorPosY(targetY, slideInDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .OnComplete(() => state.landed = true);
        }
        else
        {
            state.landed = true;
        }

        _activeStates.Add(state);
        Debug.Log($"[QTE_DIAG] StartQTEIndicator: id={indicator.GetInstanceID()}, type={config.qteType}, activeCount={_activeStates.Count}");
        return indicator;
    }

    /// <summary>
    /// 是否已落位（入场动画完成，可以开始判定）
    /// </summary>
    public bool HasLanded(GameObject indicator)
    {
        var state = FindState(indicator);
        return state != null && state.landed;
    }

    /// <summary>
    /// 错误输入反馈：当前指示器短暂染红，随后由 ResolveIndicator 退场。
    /// </summary>
    public void FlashIndicatorFailure(GameObject indicator)
    {
        var state = FindState(indicator);
        if (state == null || state.indicatorImage == null) return;

        state.indicatorImage.DOKill();
        state.indicatorImage.color = Color.white;
        state.indicatorImage.DOColor(new Color(1f, 0.22f, 0.22f, 1f), 0.08f)
            .SetUpdate(true);
    }

    /// <summary>
    /// 判定结果退场动画
    /// </summary>
    public void ResolveIndicator(GameObject indicator, bool success)
    {
        var state = FindState(indicator);
        if (state == null) return;

        if (state.animationTween != null && state.animationTween.IsActive())
            state.animationTween.Kill();
        indicator.transform.DOKill(true);

        var effectParent = GetIndicatorParent();
        if (success && successEffectPrefab != null)
        {
            var e = Instantiate(successEffectPrefab, indicator.transform.position, Quaternion.identity, effectParent);
            StartCoroutine(DestroyAfterUnscaled(e, resultEffectDuration));
        }
        else if (!success && failureEffectPrefab != null)
        {
            var e = Instantiate(failureEffectPrefab, indicator.transform.position, Quaternion.identity, effectParent);
            StartCoroutine(DestroyAfterUnscaled(e, resultEffectDuration));
        }

        SlideOutAndDestroy(state);
    }

    /// <summary>
    /// 清除所有活跃指示器（AbortQTE 时调用）
    /// </summary>
    public void ClearAllIndicators()
    {
        _resultFeedbackTween?.Kill();
        _resultFeedbackTween = null;
        if (_resultFeedback != null)
            Destroy(_resultFeedback);
        _resultFeedback = null;

        Debug.Log($"[QTE_DIAG] ClearAllIndicators: activeCount={_activeStates.Count}, dyingCount={_dyingIndicators.Count}");

        foreach (var state in _activeStates)
        {
            if (state == null) continue;
            if (state.gameObject != null)
            {
                if (state.animationTween != null && state.animationTween.IsActive())
                    state.animationTween.Kill();
                state.gameObject.transform.DOKill();
                Destroy(state.gameObject);
            }
        }
        _activeStates.Clear();

        for (int i = _dyingIndicators.Count - 1; i >= 0; i--)
        {
            var di = _dyingIndicators[i];
            if (di.moveTween != null && di.moveTween.IsActive())
                di.moveTween.Kill();
            if (di.fadeTween != null && di.fadeTween.IsActive())
                di.fadeTween.Kill();
            if (di.gameObject != null)
                Destroy(di.gameObject);
        }
        _dyingIndicators.Clear();
    }

    // ═══════════════════════════════════════════
    //  旧 API（保持兼容）
    // ═══════════════════════════════════════════

    public GameObject SpawnIndicator(QTEConfig config)
    {
        return StartQTEIndicator(config);
    }

    public void ShowQTEResult(GameObject indicator, bool success)
    {
        ShowResultFeedback(success);
        ResolveIndicator(indicator, success);
    }

    public void ShowResultFeedback(bool success)
    {
        var sprite = success ? successResultSprite : failureResultSprite;
        if (sprite == null) return;

        _resultFeedbackTween?.Kill();
        if (_resultFeedback != null)
            Destroy(_resultFeedback);

        var parent = GetIndicatorParent();
        if (parent == null) return;

        _resultFeedback = new GameObject("QTE_ResultFeedback", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        _resultFeedback.transform.SetParent(parent, false);

        var rt = _resultFeedback.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float displayScale = _uiScale > 0.01f ? _uiScale : 1f;
        rt.anchoredPosition = new Vector2(resultFeedbackOffsetX * displayScale, 0f);
        rt.sizeDelta = new Vector2(510f * displayScale, 255f * displayScale);
        rt.localScale = Vector3.one;

        var image = _resultFeedback.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        var group = _resultFeedback.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        float holdDuration = Mathf.Max(0f, resultFeedbackDuration - 0.20f);
        _resultFeedbackTween = DOTween.Sequence()
            .SetUpdate(true)
            .Append(group.DOFade(1f, 0.08f))
            .Join(rt.DOScale(1f, 0.08f).SetEase(Ease.OutBack))
            .AppendInterval(holdDuration)
            .Append(group.DOFade(0f, 0.12f))
            .OnComplete(() =>
            {
                if (_resultFeedback != null)
                    Destroy(_resultFeedback);
                _resultFeedback = null;
                _resultFeedbackTween = null;
            });
    }

    // ═══════════════════════════════════════════
    //  内部
    // ═══════════════════════════════════════════

    private void SlideOutAndDestroy(IndicatorState state)
    {
        _activeStates.Remove(state);
        var go = state.gameObject;
        if (go == null) return;

        Debug.Log($"[QTE_DIAG] SlideOutAndDestroy: id={go.GetInstanceID()}, activeRemaining={_activeStates.Count}, dyingCount={_dyingIndicators.Count}");

        var rt = go.GetComponent<RectTransform>();
        float endY = -(150f * _uiScale * 0.5f + slideInOffsetY);
        if (qteFrameRect != null)
            endY = -(qteFrameRect.rect.height * 0.5f + slideInOffsetY);

        var canvasGroup = go.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = go.AddComponent<CanvasGroup>();

        var di = new DyingIndicator { gameObject = go };
        _dyingIndicators.Add(di);

        if (rt != null)
            di.moveTween = rt.DOAnchorPosY(endY, slideOutDuration).SetEase(Ease.InCubic).SetUpdate(true);
        di.fadeTween = canvasGroup.DOFade(0f, slideOutDuration).SetUpdate(true).OnComplete(() =>
        {
            _dyingIndicators.Remove(di);
            if (go != null)
            {
                Debug.Log($"[QTE_DIAG] SlideOutComplete: id={go.GetInstanceID()}, dyingRemaining={_dyingIndicators.Count}");
                Destroy(go);
            }
        });
    }

    private IndicatorState FindState(GameObject indicator)
    {
        for (int i = _activeStates.Count - 1; i >= 0; i--)
        {
            if (_activeStates[i] != null && _activeStates[i].gameObject == indicator)
                return _activeStates[i];
        }
        return null;
    }

    private GameObject GetDefaultPrefab(QTEType type)
    {
        switch (type)
        {
            case QTEType.Click: return defaultClickIndicatorPrefab;
            case QTEType.Swipe: return defaultSwipeIndicatorPrefab;
        }
        return null;
    }

    private System.Collections.IEnumerator DestroyAfterUnscaled(GameObject obj, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (obj != null) Destroy(obj);
    }
}
