using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 三选一弹窗 — prefab 中预置 3 张卡片，手动调整布局。
/// 代码只负责填充内容 + 淡入淡出动画。
/// 由 UpgradeChoiceManager 动态 Instantiate / Destroy 管理生命周期。
/// </summary>
public class UpgradeChoicePopup : MonoBehaviour, IUpgradeChoiceView, IUpgradeChoiceKindView, IEquipmentReplacePrompt
{
    [Header("预置卡片")]
    public UpgradeCard card1;
    public UpgradeCard card2;
    public UpgradeCard card3;

    [Header("标题（区分奖励来源）")]
    [Tooltip("标题文本（2026-11-17：属性强化 = 经验升级；装备掉落 = 精英敌人）")]
    public TextMeshProUGUI titleText;
    [Tooltip("属性（经验升级）的标题文案")]
    public string attributeTitle = "属性强化";
    [Tooltip("装备 / 饰品（精英敌人掉落）的标题文案")]
    public string equipmentTitle = "装备掉落";
    [Tooltip("道具（Boss 锦囊，V1）的标题文案")]
    public string itemTitle = "战利品";
    [Tooltip("属性标题颜色")]
    public Color attributeTitleColor = new Color(0.91f, 0.89f, 0.82f, 1f);
    [Tooltip("装备标题颜色")]
    public Color equipmentTitleColor = new Color(0.96f, 0.77f, 0.26f, 1f);
    [Tooltip("道具标题颜色")]
    public Color itemTitleColor = new Color(0.85f, 0.85f, 0.85f, 1f);

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
    /// <summary>本次弹窗的奖励类别（装备卡片要按「固定变体」渲染）。</summary>
    private UpgradeRewardKind _kind = UpgradeRewardKind.Attribute;
    /// <summary>满槽换装时下方弹出的旧装备名单（运行时创建，不改 prefab）。</summary>
    private EquipmentReplaceListView _replaceList;
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
        _replaceList = null;
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
        bool fixedVariant = _kind == UpgradeRewardKind.Equipment;
        for (int i = 0; i < _cards.Length; i++)
        {
            if (i < choices.Count && choices[i] != null)
            {
                _cards[i].gameObject.SetActive(true);
                _cards[i].Setup(choices[i], OnCardTapped, fixedVariant);
            }
            else
            {
                _cards[i].gameObject.SetActive(false);
            }
        }
        FadeIn();
    }

    /// <summary>
    /// **标题区分奖励来源**（2026-11-17）：
    ///   · 属性强化 → 来自**经验升级**
    ///   · 装备掉落 → 来自**精英敌人**
    /// 玩家一眼能看出这一轮拿到的卡是从哪条线来的。
    /// </summary>
    public void SetChoiceKind(UpgradeRewardKind kind)
    {
        _kind = kind;
        if (titleText == null) return;
        switch (kind)
        {
            case UpgradeRewardKind.Equipment:
                titleText.text = equipmentTitle;
                titleText.color = equipmentTitleColor;
                break;
            case UpgradeRewardKind.Item:
                titleText.text = itemTitle;
                titleText.color = itemTitleColor;
                break;
            default:
                titleText.text = attributeTitle;
                titleText.color = attributeTitleColor;
                break;
        }
    }

    /// <summary>
    /// **点按确认某张卡**（2026-11-16 用户定：从长按改回点按）。
    /// 卡自己走 Unity 标准 Button 的 onClick（按下+抬起 = 一次点按）。
    /// </summary>
    private void OnCardTapped(UpgradeCard card)
    {
        if (card == null) return;
        if (IsShowing) return;   // 正在等下方旧装备：不再接受新卡点击
        UpgradeChoiceManager.Instance?.ConfirmChoice(card.Definition);
    }

    // ── IEquipmentReplacePrompt（满槽异款：下方选旧装备）──

    /// <summary>当前是否在等玩家点下方的旧装备。</summary>
    public bool IsShowing => _replaceList != null;

    /// <summary>在候选卡下方展示同类别已装备名单（只展示、不推进队列）。</summary>
    public void ShowReplaceTargets(UpgradeDefinition newVariant, List<EquippedItem> equipped, Action<UpgradeDefinition> onPicked)
    {
        HideReplaceTargets();

        var content = card1 != null ? card1.transform.parent as RectTransform : null;
        var parent = content != null ? content : transform as RectTransform;
        if (parent == null)
        {
            Debug.LogError("[UpgradeChoicePopup] 找不到 Content 节点，无法展示替换列表");
            return;
        }

        var font = titleText != null ? titleText.font : TMP_Settings.defaultFontAsset;
        _replaceList = EquipmentReplaceListView.Create(parent, font, newVariant, equipped, onPicked);

        // 名单开着时，上方卡片不再可点（选新装备那一步已经完成）
        if (_cards != null)
        {
            for (int i = 0; i < _cards.Length; i++)
            {
                var card = _cards[i];
                if (card != null && card.gameObject.activeSelf) card.SetSelectable(false);
            }
        }
    }

    /// <summary>收起名单（替换成功 / 本次奖励结束时调用）。</summary>
    public void HideReplaceTargets()
    {
        if (_replaceList != null)
        {
            Destroy(_replaceList.gameObject);
            _replaceList = null;
        }
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
