using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// QTE 显示管理器 — 挂载到 Canvas
/// 老虎机风格 QTE 动画：图案下滑入场 → 填充 0→1 → 放大闪白 → 判定 → 下滑退场
/// </summary>
public class QTEDisplay : MonoBehaviour
{
    [Header("老虎机框架")]
    [Tooltip("QTE 判定框 RectTransform（含 RectMask2D），运行时由 BattleHUD 注入")]
    public RectTransform qteFrameRect;
    [Tooltip("QTE 指示器生成区域（QTEFrame 下的空节点），运行时由 BattleHUD 注入")]
    public RectTransform qteIndicatorArea;

    [Header("老虎机动画参数")]
    [Tooltip("入场下滑时长（秒）")]
    public float slideInDuration = 0.25f;
    [Tooltip("退场下滑时长（秒）")]
    public float slideOutDuration = 0.3f;
    [Tooltip("入场起始偏移（像素，frame 上方）")]
    public float slideInOffsetY = 200f;
    [Tooltip("放大闪白总时长（秒）")]
    public float flashDuration = 0.3f;
    [Tooltip("放大闪白峰值 scale")]
    public float flashScale = 1.3f;

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

    /// <summary>
    /// 活跃指示器状态
    /// </summary>
    private class IndicatorState
    {
        public GameObject gameObject;
        public Image fillImage;        // 填充层 Image（Filled 类型，上层）
        public Image ghostImage;       // 底图层 Image（Simple 类型，半透明）
        public Sequence animationSeq;
        public bool fillComplete;
    }

    private class DyingIndicator
    {
        public GameObject gameObject;
        public Sequence sequence;
    }

    private List<IndicatorState> _activeStates = new List<IndicatorState>();
    private List<DyingIndicator> _dyingIndicators = new List<DyingIndicator>();
    private RectTransform _fallbackParent;
    private float _uiScale;

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
        if (qteIndicatorArea != null) return qteIndicatorArea;
        return _fallbackParent;
    }

    // ═══════════════════════════════════════════
    //  新 API — 老虎机动画
    // ═══════════════════════════════════════════

    /// <summary>
    /// 启动 QTE 指示器老虎机动画（替代旧 SpawnIndicator）
    /// 序列：半透明底图下滑入场 → 填充层 0→1 覆盖 → 放大闪白 → 等待判定 → 下滑退场
    /// </summary>
    public GameObject StartQTEIndicator(QTEConfig config)
    {
        GameObject prefab = config.qteIndicatorPrefab != null
            ? config.qteIndicatorPrefab
            : GetDefaultPrefab(config.qteType);

        if (prefab == null) return null;

        var parent = GetIndicatorParent();
        GameObject indicator = Instantiate(prefab, parent);
        var rt = indicator.GetComponent<RectTransform>();

        var state = new IndicatorState();
        state.gameObject = indicator;

        // 获取 prefab 上的 Image 作为填充层
        state.fillImage = indicator.GetComponent<Image>();
        if (state.fillImage == null)
            state.fillImage = indicator.GetComponentInChildren<Image>();

        // 设置指示器尺寸、位置和缩放
        float posX = 0f, posY = 0f;
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.sizeDelta = config.indicatorSize;

            float frameW = qteFrameRect != null ? qteFrameRect.rect.width : 600f * _uiScale;
            float frameH = qteFrameRect != null ? qteFrameRect.rect.height : 150f * _uiScale;
            posX = (config.screenPosition.x - 0.5f) * frameW;
            posY = (config.screenPosition.y - 0.5f) * frameH;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(posX, posY);
        }

        // ── 创建底图层（ghost）──
        // 与填充层同 sprite，Type=Simple 始终满，alpha≈0.7 半透明
        if (state.fillImage != null && state.fillImage.sprite != null)
        {
            var ghostGo = new GameObject("Ghost", typeof(RectTransform), typeof(Image));
            ghostGo.transform.SetParent(indicator.transform, false);
            var ghostRt = ghostGo.GetComponent<RectTransform>();
            ghostRt.anchorMin = Vector2.zero;
            ghostRt.anchorMax = Vector2.one;
            ghostRt.sizeDelta = Vector2.zero;
            ghostRt.anchoredPosition = Vector2.zero;
            var ghostImg = ghostGo.GetComponent<Image>();
            ghostImg.sprite = state.fillImage.sprite;
            ghostImg.type = Image.Type.Simple;
            ghostImg.preserveAspect = state.fillImage.preserveAspect;
            ghostImg.color = new Color(1f, 1f, 1f, 0.7f);
            ghostImg.raycastTarget = false;
            state.ghostImage = ghostImg;
        }

        // 填充层：fillAmount 立即归零，Type 保持 Filled
        if (state.fillImage != null)
        {
            state.fillImage.fillAmount = 0f;
            state.fillImage.color = new Color(1f, 1f, 1f, 1f);
            state.fillImage.raycastTarget = true;
        }

        // 初始位置：frame 上方（X不变，Y上移）
        float targetY = rt != null ? rt.anchoredPosition.y : 0f;
        if (rt != null)
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, slideInOffsetY);

        // ── 构建老虎机 Sequence ──
        var seq = DOTween.Sequence();
        seq.SetId($"qte_{indicator.GetInstanceID()}");
        seq.SetAutoKill(false);

        // 1. 下滑入场（仅位移，无 fade：底图半透明始终可见，填充层从 0 开始不可见）
        if (rt != null)
            seq.Append(rt.DOAnchorPosY(targetY, slideInDuration).SetEase(Ease.OutCubic));

        // 2. 填充 0→1（填充层覆盖在半透明底图上）
        if (state.fillImage != null && config.warningDuration > 0f)
        {
            seq.Append(state.fillImage.DOFillAmount(1f, config.warningDuration).SetEase(Ease.Linear));
            seq.AppendCallback(() => state.fillComplete = true);
        }
        else
        {
            seq.AppendCallback(() => state.fillComplete = true);
        }

        // 3. 放大闪白（进入判定窗口）
        if (state.fillImage != null && flashDuration > 0f)
        {
            seq.Append(indicator.transform.DOPunchScale(
                Vector3.one * (flashScale - 1f), flashDuration, 1, 0f));
        }

        state.animationSeq = seq;
        seq.SetUpdate(true); // timeScale-independent
        _activeStates.Add(state);
        Debug.Log($"[QTE_DIAG] StartQTEIndicator: id={indicator.GetInstanceID()}, type={config.qteType}, activeCount={_activeStates.Count}, dyingCount={_dyingIndicators.Count}");
        return indicator;
    }

    /// <summary>
    /// 判定窗口开始 — 若填充尚未完成，Kill sequence 后手动填满并触发放大闪白
    /// </summary>
    public void OnJudgmentStart(GameObject indicator)
    {
        var state = FindState(indicator);
        if (state == null || state.fillComplete) return;

        state.fillComplete = true;

        // Kill 动画 sequence，避免与手动 flash 冲突
        if (state.animationSeq != null && state.animationSeq.IsActive())
            state.animationSeq.Kill();

        if (state.fillImage != null)
        {
            DOTween.Kill(state.fillImage);
            state.fillImage.fillAmount = 1f;
        }

        // 放大闪白
        if (flashDuration > 0f)
        {
            indicator.transform.localScale = Vector3.one;
            indicator.transform.DOPunchScale(
                Vector3.one * (flashScale - 1f), flashDuration, 1, 0f);
        }
    }

    /// <summary>
    /// 判定结果退场动画（替代旧 ShowQTEResult）
    /// </summary>
    public void ResolveIndicator(GameObject indicator, bool success)
    {
        var state = FindState(indicator);
        if (state == null) return;

        if (state.animationSeq != null && state.animationSeq.IsActive())
            state.animationSeq.Kill();
        indicator.transform.DOKill(true);

        // 结果特效（使用 unscaledTime 自毁协程，确保 timeScale=0 时也能清理）
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
    /// 提前取消（提早输入）— 无特效直接下滑消失
    /// </summary>
    public void CancelIndicatorEarly(GameObject indicator)
    {
        var state = FindState(indicator);
        if (state == null) return;

        if (state.animationSeq != null && state.animationSeq.IsActive())
            state.animationSeq.Kill();
        indicator.transform.DOKill(true);

        SlideOutAndDestroy(state);
    }

    /// <summary>
    /// 清除所有活跃指示器（AbortQTE 时调用）
    /// </summary>
    public void ClearAllIndicators()
    {
        Debug.Log($"[QTE_DIAG] ClearAllIndicators: activeCount={_activeStates.Count}, dyingCount={_dyingIndicators.Count}");

        // 清理活跃指示器
        foreach (var state in _activeStates)
        {
            if (state == null) continue;
            if (state.gameObject != null)
            {
                if (state.animationSeq != null && state.animationSeq.IsActive())
                    state.animationSeq.Kill();
                state.gameObject.transform.DOKill();
                Destroy(state.gameObject);
            }
        }
        _activeStates.Clear();

        // 清理正在下滑退场的指示器（提前失败时已从 _activeStates 移除但动画尚未完成）
        for (int i = _dyingIndicators.Count - 1; i >= 0; i--)
        {
            var di = _dyingIndicators[i];
            if (di.sequence != null && di.sequence.IsActive())
                di.sequence.Kill();
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
        ResolveIndicator(indicator, success);
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

        var cg = go.AddComponent<CanvasGroup>();
        var seq = DOTween.Sequence();
        seq.SetId($"qte_out_{go.GetInstanceID()}");
        seq.SetUpdate(true); // timeScale-independent: 升级弹窗暂停时也能完成清理
        if (rt != null)
            seq.Join(rt.DOAnchorPosY(endY, slideOutDuration).SetEase(Ease.InCubic));
        seq.Join(cg.DOFade(0f, slideOutDuration));

        var di = new DyingIndicator { gameObject = go, sequence = seq };
        _dyingIndicators.Add(di);

        seq.OnComplete(() =>
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
