using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class RouteChoiceOption
{
    public string id;
    public string label;
    public string description;
    public Sprite icon;
    public RouteChoiceLayout layout;
}

public sealed class RouteChoicePanel : MonoBehaviour
{
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RouteChoiceButton buttonPrefab;
    [SerializeField] private Sprite defaultIcon;

    private readonly List<RouteChoiceButton> _buttons = new List<RouteChoiceButton>();
    private Action<string> _onSelected;

    public static RouteChoicePanel Show(RouteChoicePanel panelPrefab, string title, IList<RouteChoiceOption> options, Action<string> onSelected)
    {
        HideCurrent();
        RouteChoicePanel panel = panelPrefab != null
            ? Instantiate(panelPrefab, FindOverlayCanvas().transform)
            : CreateFallbackPanel();
        panel._onSelected = onSelected;
        panel.gameObject.SetActive(true);
        panel.Rebuild(title, options);
        return panel;
    }

    public static void HideCurrent()
    {
        var panels = FindObjectsOfType<RouteChoicePanel>(true);
        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null)
                Destroy(panels[i].gameObject);
    }

    private void Rebuild(string title, IList<RouteChoiceOption> options)
    {
        if (titleText != null) titleText.text = title;
        if (choiceContainer == null) choiceContainer = transform;
        for (int i = 0; i < _buttons.Count; i++)
            if (_buttons[i] != null) Destroy(_buttons[i].gameObject);
        _buttons.Clear();

        if (options == null) return;
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            if (option == null) continue;
            var button = buttonPrefab != null ? Instantiate(buttonPrefab, choiceContainer) : CreateFallbackButton(choiceContainer);
            if (option.icon == null) option.icon = defaultIcon;
            button.Bind(option, HandleSelected);
            button.ApplyLayout(option.layout, i, options.Count);
            _buttons.Add(button);
        }
    }

    private void HandleSelected(string id)
    {
        _onSelected?.Invoke(id);
    }

    private static RouteChoicePanel CreateFallbackPanel()
    {
        var root = new GameObject("RouteChoicePanel_Test", typeof(RectTransform), typeof(RouteChoicePanel));
        root.transform.SetParent(FindOverlayCanvas().transform, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.3f);
        rect.anchorMax = new Vector2(0.85f, 0.7f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var container = new GameObject("ChoiceContainer", typeof(RectTransform));
        container.transform.SetParent(root.transform, false);
        var containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.15f, 0.15f);
        containerRect.anchorMax = new Vector2(0.85f, 0.85f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;
        root.GetComponent<RouteChoicePanel>().choiceContainer = container.transform;
        return root.GetComponent<RouteChoicePanel>();
    }

    private static RouteChoiceButton CreateFallbackButton(Transform parent)
    {
        var root = new GameObject("RouteChoiceButton_Test", typeof(RectTransform), typeof(Image), typeof(Button), typeof(RouteChoiceButton));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = Color.white;
        root.transform.localScale = Vector3.one * 0.5f;
        root.transform.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-25f, 25f));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(260f, 80f);
        rootRect.anchoredPosition = new Vector2(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-100f, 100f));
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(root.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 4f);
        textRect.offsetMax = new Vector2(-12f, -4f);
        var text = textObject.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        root.GetComponent<RouteChoiceButton>().fallbackText = text;
        return root.GetComponent<RouteChoiceButton>();
    }

    private static Canvas FindOverlayCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i].isRootCanvas && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay && canvases[i].name == "BattleHUD(Canvas)")
                return canvases[i];
        for (int i = 0; i < canvases.Length; i++)
            if (canvases[i].isRootCanvas && canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                return canvases[i];
        throw new InvalidOperationException("RouteChoicePanel requires a ScreenSpaceOverlay Canvas.");
    }
}
