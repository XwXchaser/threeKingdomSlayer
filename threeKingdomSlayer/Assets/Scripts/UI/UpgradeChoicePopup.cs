using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 三选一弹窗 — prefab 中预置 3 张卡片，手动调整布局。
/// 代码只负责填充内容 + 淡入淡出动画。
/// 由 UpgradeChoiceManager 动态 Instantiate / Destroy 管理生命周期。
/// </summary>
public class UpgradeChoicePopup : MonoBehaviour
{
    [Header("预置卡片")]
    public UpgradeCard card1;
    public UpgradeCard card2;
    public UpgradeCard card3;

    [Header("动画")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.15f;

    private UpgradeCard[] _cards;

    private void Awake()
    {
        _cards = new[] { card1, card2, card3 };
    }

    public void ShowChoices(List<UpgradeDefinition> choices)
    {
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < choices.Count && choices[i] != null)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].Setup(choices[i]);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);
            }
        }
        FadeIn();
    }

    public void Dismiss(Action onDone)
    {
        FadeOut(() =>
        {
            onDone?.Invoke();
            Destroy(gameObject);
        });
    }

    private void FadeIn()
    {
        if (canvasGroup == null) return;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
    }

    private void FadeOut(Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }
        canvasGroup.blocksRaycasts = false;
        var tw = canvasGroup.DOFade(0f, fadeOutDuration).SetUpdate(true);
        if (onComplete != null)
            tw.OnComplete(() => onComplete());
    }
}
