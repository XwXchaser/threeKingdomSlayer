using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 三选一弹窗 — 管理升级选项卡片的显示/隐藏、动画
///
/// 监听 UpgradeChoiceManager.OnChoicesReady / OnChoiceSelected / OnAllChoicesDone
/// 在独立 Canvas 上竖向堆叠选项卡片，支持连续升级时刷新内容。
/// </summary>
public class UpgradeChoicePopup : MonoBehaviour
{
    [Header("布局")]
    public Transform cardsParent;
    public GameObject cardPrefab;
    [Tooltip("卡片竖向间距")]
    public float cardSpacing = 20f;

    [Header("动画")]
    public CanvasGroup canvasGroup;
    [Tooltip("弹窗打开淡入时长")]
    public float fadeInDuration = 0.2f;
    [Tooltip("弹窗关闭淡出时长")]
    public float fadeOutDuration = 0.15f;

    private List<UpgradeCard> _spawnedCards = new List<UpgradeCard>();

    private void Start()
    {
        if (UpgradeChoiceManager.Instance != null)
        {
            UpgradeChoiceManager.Instance.OnChoicesReady += ShowChoices;
            UpgradeChoiceManager.Instance.OnAllChoicesDone += Hide;
        }

        // 初始隐藏
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (UpgradeChoiceManager.Instance != null)
        {
            UpgradeChoiceManager.Instance.OnChoicesReady -= ShowChoices;
            UpgradeChoiceManager.Instance.OnAllChoicesDone -= Hide;
        }
    }

    private void ShowChoices(List<UpgradeDefinition> choices)
    {
        // 清理旧卡片
        for (int i = 0; i < _spawnedCards.Count; i++)
            Destroy(_spawnedCards[i].gameObject);
        _spawnedCards.Clear();

        // 生成新卡片
        for (int i = 0; i < choices.Count; i++)
        {
            var go = Instantiate(cardPrefab, cardsParent);
            var card = go.GetComponent<UpgradeCard>();
            if (card != null)
            {
                card.Setup(choices[i]);
                _spawnedCards.Add(card);
            }
        }

        // 淡入动画（使用 Time.unscaledDeltaTime，因为游戏可能已暂停）
        FadeIn();
    }

    private void Hide()
    {
        FadeOut();
    }

    private void FadeIn()
    {
        if (canvasGroup == null) return;
        canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
    }

    private void FadeOut()
    {
        if (canvasGroup == null) return;
        canvasGroup.DOFade(0f, fadeOutDuration).SetUpdate(true);
    }
}
