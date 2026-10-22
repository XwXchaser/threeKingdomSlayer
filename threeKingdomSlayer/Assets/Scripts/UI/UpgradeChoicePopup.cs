using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 三选一弹窗 — prefab 中预置 3 张卡片，手动调整布局。
/// 代码只负责填充内容 + 淡入淡出动画。
/// 由 UpgradeChoiceManager 动态 Instantiate / Destroy 管理生命周期。
/// </summary>
public class UpgradeChoicePopup : MonoBehaviour, IUpgradeChoiceView
{
    [Header("预置卡片")]
    public UpgradeCard card1;
    public UpgradeCard card2;
    public UpgradeCard card3;

    [Header("弃置弹窗素材（道具栏满时弹出的弃置窗用）")]
    [Tooltip("弃置弹窗外框（Inspector 引用，替代运行时按路径加载）")]
    public Sprite discardOuterFrame;
    [Tooltip("弃置弹窗里「已有道具」卡片的底图")]
    public Sprite discardCardBackground;
    [Tooltip("弃置弹窗里「新获得道具」卡片的底图")]
    public Sprite newItemCardBackground;

    [Header("动画")]
    public CanvasGroup canvasGroup;
    public float fadeInDuration = 0.2f;
    public float fadeOutDuration = 0.15f;

    private UpgradeCard[] _cards;
    private UpgradeCard _selectedCard;
    private Tween _fadeTween;

    private void Awake()
    {
        _cards = new[] { card1, card2, card3 };
    }

    private void OnDestroy()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
            _fadeTween.Kill(false);
        if (canvasGroup != null)
            canvasGroup.DOKill(false);
    }

    public void ShowChoices(List<UpgradeDefinition> choices)
    {
        if (_cards == null)
            _cards = new[] { card1, card2, card3 };
        _selectedCard = null;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < choices.Count && choices[i] != null)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].Setup(choices[i], OnCardClicked);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);
            }
        }
        FadeIn();
    }

    private void OnCardClicked(UpgradeCard card)
    {
        if (card == null) return;
        if (_selectedCard == card)
        {
            UpgradeChoiceManager.Instance?.ConfirmChoice(card.Definition);
            return;
        }

        if (_selectedCard != null)
            _selectedCard.SetSelected(false);
        _selectedCard = card;
        _selectedCard.SetSelected(true);
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
        _fadeTween?.Kill(false);
        canvasGroup.blocksRaycasts = true;
        _fadeTween = canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
    }

    private void FadeOut(Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }
        canvasGroup.blocksRaycasts = false;
        _fadeTween?.Kill(false);
        var tw = canvasGroup.DOFade(0f, fadeOutDuration).SetUpdate(true);
        _fadeTween = tw;
        if (onComplete != null)
            tw.OnComplete(() => onComplete());
    }
}
