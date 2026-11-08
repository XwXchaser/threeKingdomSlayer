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
    public float fadeInDuration = 0.28f;
    public float fadeOutDuration = 0.15f;
    [Tooltip("弹出时的起始缩放（1 = 不缩放）")]
    public float popStartScale = 0.6f;
    [Tooltip("弹出动画时长（秒；不受 timeScale 影响 —— 弹窗期间游戏是暂停的）")]
    public float popDuration = 0.4f;
    [Tooltip("弹出时的上浮距离（画布单位）")]
    public float popRise = 60f;

    private UpgradeCard[] _cards;
    private Tween _fadeTween;
    private readonly List<Tween> _scaleTweens = new List<Tween>();
    private readonly List<Tween> _moveTweens = new List<Tween>();
    /// <summary>弹窗根的原始缩放（弹出动画结束要回到它）。</summary>
    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        _cards = new[] { card1, card2, card3 };
        var rt = transform as RectTransform;
        if (rt != null) _baseScale = rt.localScale;
    }

    /// <summary>
    /// 弹窗的**视觉子物体**（框体 + 内容）：缩放动画作用在它们身上。
    /// </summary>
    private List<RectTransform> GetVisualRoots()
    {
        var result = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var c = transform.GetChild(i) as RectTransform;
            if (c != null) result.Add(c);
        }
        return result;
    }

    private void OnDestroy()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
            _fadeTween.Kill(false);
        KillTweens(_scaleTweens);
        KillTweens(_moveTweens);
        if (canvasGroup != null)
            canvasGroup.DOKill(false);
    }

    private static void KillTweens(List<Tween> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].IsActive()) list[i].Kill(false);
        }
        list.Clear();
    }

    public void ShowChoices(List<UpgradeDefinition> choices)
    {
        if (_cards == null)
            _cards = new[] { card1, card2, card3 };
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < choices.Count && choices[i] != null)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].Setup(choices[i], OnCardHeld);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);
            }
        }
        FadeIn();
    }

    /// <summary>
    /// **长按确认某张卡**（2026-11-05 用户定：长按选择，而不是点两下）。
    /// 卡自己按住计时（<see cref="UpgradeCard.holdConfirmSeconds"/>），满了才回调到这里。
    /// </summary>
    private void OnCardHeld(UpgradeCard card)
    {
        if (card == null) return;
        UpgradeChoiceManager.Instance?.ConfirmChoice(card.Definition);
    }

    /// <summary>
    /// 教学用：取某个候选卡在屏幕上的位置（把提示手落在推荐的那张卡上）。
    /// 找不到返回 false。
    /// </summary>
    public bool TryGetChoiceScreenPoint(UpgradeDefinition def, out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        if (def == null || _cards == null) return false;
        for (int i = 0; i < _cards.Length; i++)
        {
            var card = _cards[i];
            if (card == null || card.Definition != def) continue;
            var rect = card.GetComponent<RectTransform>();
            if (rect == null) return false;
            var canvas = rect.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 教学用：把某个候选卡**设成不可选**（教学只让玩家选指定的那张）。
    /// </summary>
    public void SetChoiceSelectable(UpgradeDefinition def, bool selectable)
    {
        if (def == null || _cards == null) return;
        for (int i = 0; i < _cards.Length; i++)
        {
            var card = _cards[i];
            if (card == null || card.Definition != def) continue;
            card.SetSelectable(selectable);
            return;
        }
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
        canvasGroup.alpha = 0f;

        // ★ **弹出窗口的动态效果**（2026-11-07 用户定：「框体**变大打开**」，
        //   不要「等比立刻显示」）→ 从 0.6 倍**长大**到 1 倍，配一段淡入，整体比之前长一截。
        //   ⚠️ **不能缩放根节点**：弹窗根上挂着 Canvas + CanvasScaler → CanvasScaler 每帧改写根的
        //   localScale → 根上的缩放补间会被**立刻覆盖** → 看起来就是「等比立刻显示」（2026-11-07 实测）。
        //   → 改成缩放根下面的**视觉子物体**（框体 + 内容），它们不受 CanvasScaler 影响。
        //   ⚠️ 必须 `SetUpdate(true)`：弹窗期间游戏是暂停的（timeScale=0）→ 用缩放时间才动得了。
        KillTweens(_scaleTweens);
        KillTweens(_moveTweens);

        var visuals = GetVisualRoots();
        var baseScales = new List<Vector3>();
        for (int i = 0; i < visuals.Count; i++)
        {
            var v = visuals[i];
            if (v == null) continue;
            baseScales.Add(v.localScale);
            v.localScale = v.localScale * popStartScale;
            v.anchoredPosition = v.anchoredPosition - new Vector2(0f, popRise);
        }
        for (int i = 0; i < visuals.Count; i++)
        {
            var v = visuals[i];
            if (v == null) continue;
            Vector3 targetScale = baseScales[i];
            Vector2 targetPos = v.anchoredPosition + new Vector2(0f, popRise);
            _scaleTweens.Add(v.DOScale(targetScale, popDuration).SetUpdate(true).SetEase(Ease.OutCubic));
            // anchoredPosition 没有现成的扩展（UI 模块未引入）→ 用 DOTween.To 自己做插值
            _moveTweens.Add(DOTween.To(() => v.anchoredPosition, p => v.anchoredPosition = p, targetPos, popDuration)
                .SetUpdate(true).SetEase(Ease.OutCubic));
        }

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
