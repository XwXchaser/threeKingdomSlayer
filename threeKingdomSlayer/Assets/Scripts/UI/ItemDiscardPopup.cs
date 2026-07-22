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
    public static ItemDiscardPopup Instance { get; private set; }
    public static bool IsShowing => Instance != null;

    private CanvasGroup _canvasGroup;
    private Action<int> _onComplete;
    private bool _completed;

    private const float FadeDuration = 0.2f;
    private const string OuterFramePath = "UI/ItemDiscard/item_discard_popup_outer_frame_v2";
    private const string DiscardCardPath = "UI/ItemDiscard/item_discard_card";
    private const string NewItemCardPath = "UI/ItemDiscard/item_get_card";

    /// <summary>回调 -1 表示丢弃新获得的道具；0 及以上表示丢弃对应库存索引。</summary>
    public static void Show(List<ItemInventory.ItemEntry> entries, UpgradeDefinition newItem, Action<int> onComplete)
    {
        if (Instance != null)
        {
            onComplete?.Invoke(-1);
            return;
        }

        var choiceManager = UpgradeChoiceManager.Instance;
        if (choiceManager == null || choiceManager.discardPopupPrefab == null)
        {
            Debug.LogWarning("[ItemDiscardPopup] discardPopupPrefab 未配置，丢弃新道具");
            onComplete?.Invoke(-1);
            return;
        }

        var go = Instantiate(choiceManager.discardPopupPrefab);
        var popup = go.AddComponent<ItemDiscardPopup>();
        Instance = popup;
        popup.BuildUI(entries, newItem, onComplete);
    }

    private void BuildUI(List<ItemInventory.ItemEntry> entries, UpgradeDefinition newItem, Action<int> onComplete)
    {
        _onComplete = onComplete;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        Time.timeScale = 0f;

        var choicePopup = GetComponent<UpgradeChoicePopup>();
        if (choicePopup == null)
        {
            Complete(-1);
            return;
        }

        var cards = new[] { choicePopup.card1, choicePopup.card2, choicePopup.card3 };
        ApplyDiscardFrame();
        AddTitle(cards[0] != null ? cards[0].transform.parent : transform);
        SetupCard(cards[0], newItem, "新获得", -1, true);

        for (int i = 1; i < cards.Length; i++)
        {
            int entryIndex = i - 1;
            if (entryIndex < entries.Count)
                SetupCard(cards[i], entries[entryIndex].definition, "点击弃置", entryIndex, false);
            else if (cards[i] != null)
                cards[i].gameObject.SetActive(false);
        }

        _canvasGroup.DOFade(1f, FadeDuration).SetUpdate(true);
    }

    private void AddTitle(Transform content)
    {
        var titleGo = CreateUIObject("DiscardTitle", content);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, 42f);
        titleRect.sizeDelta = new Vector2(720f, 64f);
        var title = titleGo.AddComponent<TextMeshProUGUI>();
        title.text = "请选择弃置的道具";
        title.fontSize = 30f;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;
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

    private void SetupCard(UpgradeCard card, UpgradeDefinition definition, string label, int discardIndex, bool isNewItem)
    {
        if (card == null || definition == null) return;

        card.gameObject.SetActive(true);
        card.Setup(definition);
        SetUpgradeCardChromeVisible(card.transform, false);
        var cardSprite = Resources.Load<Sprite>(isNewItem ? NewItemCardPath : DiscardCardPath);
        if (cardSprite != null && card.backgroundImage != null)
        {
            card.backgroundImage.sprite = cardSprite;
            card.backgroundImage.type = Image.Type.Simple;
            card.backgroundImage.preserveAspect = true;
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
        if (card.button != null)
        {
            card.button.onClick.RemoveAllListeners();
            card.button.onClick.AddListener(() => Complete(discardIndex));
        }
    }

    private static void SetUpgradeCardChromeVisible(Transform card, bool visible)
    {
        var namePlate = card.Find("NamePlate");
        if (namePlate != null) namePlate.gameObject.SetActive(visible);
        var descriptionBox = card.Find("DescriptionBox");
        if (descriptionBox != null) descriptionBox.gameObject.SetActive(visible);
        var selectedGlow = card.Find("SelectedGlow");
        if (selectedGlow != null) selectedGlow.gameObject.SetActive(false);
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

    private void Complete(int discardIndex)
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
            callback?.Invoke(discardIndex);
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

    // ── UI 工具方法 ──

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateTMPro(string name, Transform parent, string text, int fontSize)
    {
        var go = CreateUIObject(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        return tmp;
    }

}
