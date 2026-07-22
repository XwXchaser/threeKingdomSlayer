using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 道具栏满弃置弹窗 — 运行时动态创建。
/// 展示当前道具 + 待添加道具，玩家点击选择要丢弃的道具后确认。
/// </summary>
public class ItemDiscardPopup : MonoBehaviour
{
    public readonly struct Result
    {
        public bool DiscardNew { get; }
        public int EntryId { get; }

        private Result(bool discardNew, int entryId)
        {
            DiscardNew = discardNew;
            EntryId = entryId;
        }

        public static Result NewItem() => new Result(true, -1);
        public static Result Existing(int entryId) => new Result(false, entryId);
    }

    public static ItemDiscardPopup Instance { get; private set; }
    public static bool IsShowing => Instance != null;

    private CanvasGroup _canvasGroup;
    private Action<Result> _onComplete;
    private UpgradeCard _selectedCard;
    private bool _completed;

    private readonly List<UpgradeCard> _cards = new List<UpgradeCard>(6);
    private readonly Dictionary<UpgradeCard, Outline> _selectionOutlines = new Dictionary<UpgradeCard, Outline>();

    private const float FadeDuration = 0.2f;
    private const string OuterFramePath = "UI/ItemDiscard/item_discard_popup_outer_frame_v2";
    private const string DiscardCardPath = "UI/ItemDiscard/item_discard_card";
    private const string NewItemCardPath = "UI/ItemDiscard/item_get_card";

    public static void Show(List<ItemInventory.ItemEntry> entries, UpgradeDefinition newItem, Action<Result> onComplete)
    {
        if (Instance != null)
        {
            onComplete?.Invoke(Result.NewItem());
            return;
        }

        var choiceManager = UpgradeChoiceManager.Instance;
        if (choiceManager == null || choiceManager.discardPopupPrefab == null)
        {
            Debug.LogWarning("[ItemDiscardPopup] discardPopupPrefab 未配置，丢弃新道具");
            onComplete?.Invoke(Result.NewItem());
            return;
        }

        var go = Instantiate(choiceManager.discardPopupPrefab);
        var popup = go.AddComponent<ItemDiscardPopup>();
        Instance = popup;
        popup.BuildUI(entries, newItem, onComplete);
    }

    private void BuildUI(List<ItemInventory.ItemEntry> entries, UpgradeDefinition newItem, Action<Result> onComplete)
    {
        _onComplete = onComplete;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        Time.timeScale = 0f;

        var choicePopup = GetComponent<UpgradeChoicePopup>();
        var template = choicePopup != null ? choicePopup.card1 : null;
        if (template == null)
        {
            Complete(Result.NewItem());
            return;
        }

        var content = template.transform.parent as RectTransform;
        if (content == null)
        {
            Complete(Result.NewItem());
            return;
        }

        ApplyDiscardFrame();
        DisableLegacyCards(choicePopup, template);
        template.gameObject.SetActive(false);

        if (newItem != null)
        {
            var card = CreateCard(template, content);
            SetupCard(card, newItem, "新获得", true, Result.NewItem());
        }

        int existingCount = Mathf.Min(entries.Count, 5);
        for (int i = 0; i < existingCount; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.definition == null) continue;
            var card = CreateCard(template, content);
            string count = entry.remainingUses < 0 ? "∞" : entry.remainingUses.ToString();
            SetupCard(card, entry.definition, $"持有 ×{count}", false, Result.Existing(entry.id));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        _canvasGroup.DOFade(1f, FadeDuration).SetUpdate(true);
    }

    private static void DisableLegacyCards(UpgradeChoicePopup choicePopup, UpgradeCard template)
    {
        if (choicePopup == null) return;
        if (choicePopup.card2 != null && choicePopup.card2 != template)
            choicePopup.card2.gameObject.SetActive(false);
        if (choicePopup.card3 != null && choicePopup.card3 != template)
            choicePopup.card3.gameObject.SetActive(false);
    }

    private UpgradeCard CreateCard(UpgradeCard template, RectTransform content)
    {
        var card = Instantiate(template, content);
        card.name = $"DiscardOption{_cards.Count + 1}";
        card.gameObject.SetActive(true);
        card.transform.localScale = Vector3.one;
        _cards.Add(card);
        return card;
    }

    private void ApplyDiscardFrame()
    {
        var outerSprite = Resources.Load<Sprite>(OuterFramePath);
        var frameImage = transform.Find("FrameBg")?.GetComponent<Image>();
        if (outerSprite != null && frameImage != null)
        {
            frameImage.sprite = outerSprite;
            frameImage.type = Image.Type.Simple;
            frameImage.preserveAspect = true;
        }
    }

    private void SetupCard(UpgradeCard card, UpgradeDefinition definition, string label, bool isNewItem, Result result)
    {
        card.Setup(definition);
        var cardSprite = Resources.Load<Sprite>(isNewItem ? NewItemCardPath : DiscardCardPath);
        if (cardSprite != null && card.backgroundImage != null)
        {
            card.backgroundImage.sprite = cardSprite;
            card.backgroundImage.type = Image.Type.Simple;
            card.backgroundImage.preserveAspect = false;
            card.backgroundImage.color = Color.white;
        }
        if (card.nameText != null)
            card.nameText.text = $"{label} · {definition.displayName}";
        if (card.descriptionText != null)
            card.descriptionText.text = GetItemDescription(definition);
        if (card.iconImage != null)
        {
            card.iconImage.sprite = definition.icon;
            card.iconImage.enabled = definition.icon != null;
        }

        var outline = card.GetComponent<Outline>();
        if (outline == null)
            outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.78f, 0.2f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = true;
        outline.enabled = false;
        _selectionOutlines[card] = outline;

        if (card.button != null)
        {
            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() => OnCardClicked(card, result));
        }
    }

    private void OnCardClicked(UpgradeCard card, Result result)
    {
        if (_selectedCard == card)
        {
            Complete(result);
            return;
        }

        if (_selectedCard != null && _selectionOutlines.TryGetValue(_selectedCard, out var previousOutline))
            previousOutline.enabled = false;

        _selectedCard = card;
        if (_selectionOutlines.TryGetValue(card, out var outline))
            outline.enabled = true;
    }

    private static string GetItemDescription(UpgradeDefinition definition)
    {
        var description = definition.descriptionTemplate;
        var runner = ItemEffectRunner.Instance;
        if (runner == null) return description;

        switch (definition.gestureId)
        {
            case "arrow_rain":
                return description.Replace("{0}", runner.arrowRows.ToString())
                    .Replace("{1}", (runner.arrowDamage * runner.arrowWaves * runner.arrowsPerWave).ToString());
            case "fire_snake":
                return description.Replace("{0}", runner.fireRows.ToString());
            case "phantom_weapon_item":
                return description.Replace("{0}", runner.phantomDuration.ToString("F1"));
            default:
                return UpgradeEffectManager.Instance != null
                    ? UpgradeEffectManager.Instance.GetDescription(definition)
                    : description;
        }
    }

    private void Complete(Result result)
    {
        if (_completed) return;
        _completed = true;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.DOFade(0f, FadeDuration).SetUpdate(true).OnComplete(() =>
        {
            var callback = _onComplete;
            _onComplete = null;
            Instance = null;
            Time.timeScale = 1f;
            if (InputManager.Instance != null)
                InputManager.Instance.blockInputFrames = 2;
            Destroy(gameObject);
            callback?.Invoke(result);
        });
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            if (!_completed)
                Time.timeScale = 1f;
        }
    }
}
