using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// QTE 显示管理器 — 挂载到 Canvas
/// 管理 QTE 视觉 prefab 的生成/销毁、预警动画、成功/失败特效
/// </summary>
public class QTEDisplay : MonoBehaviour
{
    [Header("指示器设置")]
    [Tooltip("QTE 指示器的父节点（Canvas 下的 RectTransform）")]
    public RectTransform indicatorParent;
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

    // 活跃的指示器
    private List<GameObject> _activeIndicators = new List<GameObject>();

    private void Awake()
    {
        if (indicatorParent == null)
        {
            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                indicatorParent = canvas.GetComponent<RectTransform>();
            }
        }
    }

    /// <summary>
    /// 生成 QTE 指示器
    /// </summary>
    public GameObject SpawnIndicator(QTEConfig config)
    {
        GameObject prefab = config.qteIndicatorPrefab != null
            ? config.qteIndicatorPrefab
            : GetDefaultPrefab(config.qteType);

        if (prefab == null) return null;

        GameObject indicator = Instantiate(prefab, indicatorParent);
        var rt = indicator.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = config.screenPosition;
            rt.anchorMax = config.screenPosition;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = config.indicatorSize;
        }

        // 预警动画：缩放脉冲
        if (config.warningDuration > 0f)
        {
            indicator.transform.localScale = Vector3.zero;
            indicator.transform.DOScale(1f, config.warningDuration).SetEase(Ease.OutBack);
        }

        _activeIndicators.Add(indicator);
        return indicator;
    }

    /// <summary>
    /// 显示 QTE 判定结果特效
    /// </summary>
    public void ShowQTEResult(GameObject indicator, bool success)
    {
        if (success && successEffectPrefab != null)
        {
            var effect = Instantiate(successEffectPrefab, indicator.transform.position, Quaternion.identity, indicatorParent);
            Destroy(effect, resultEffectDuration);
        }
        else if (!success && failureEffectPrefab != null)
        {
            var effect = Instantiate(failureEffectPrefab, indicator.transform.position, Quaternion.identity, indicatorParent);
            Destroy(effect, resultEffectDuration);
        }

        // 指示器缩小消失
        indicator.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                _activeIndicators.Remove(indicator);
                Destroy(indicator);
            });
    }

    /// <summary>
    /// 清除所有活跃的 QTE 指示器
    /// </summary>
    public void ClearAllIndicators()
    {
        foreach (var indicator in _activeIndicators)
        {
            if (indicator != null)
            {
                indicator.transform.DOKill();
                Destroy(indicator);
            }
        }
        _activeIndicators.Clear();
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
}
