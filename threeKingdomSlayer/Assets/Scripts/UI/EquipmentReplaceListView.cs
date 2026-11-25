using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **满槽换装的“旧装备名单”**（2026-11-25）——三选一下方的一排可点旧装备。
///
/// 规则（用户 2026-11-25 定）：
///   · 只在「没有同款 + 该类满槽」时出现；点上方新装备后，点这里的一件旧装备完成替换；
///   · 2026-11-25 用户确认：提供**跳过**（放弃本次装备奖励、保留原装备）；
///   · 上方候选卡仍可点，用于**切换**要替换成的新装备；
///   · 只列**同类别**（影之武装或饰品）的已装备装备。
///
/// 实现：运行时搭在弹窗 Content 下方（不改 prefab），用程序化圆角底板撑住临时造型，
/// 后续美术到位可整块替换，不影响流程。
/// </summary>
public class EquipmentReplaceListView : MonoBehaviour
{
    private const float PanelWidth = 860f;
    // 高度 170：右上「跳过」按钮底边在 128，旧装备 chip 顶边在 120，留 8px 间隙；
    // 5 件旧装备（天赋扩槽后的上限）时按钮不会压住最后一件。
    private const float PanelHeight = 170f;
    private const float ChipWidth = 160f;
    private const float ChipHeight = 108f;
    private const float ChipGap = 12f;

    private Action<UpgradeDefinition> _onPicked;

    /// <summary>在 <paramref name="parent"/> 下方创建名单（父节点应为弹窗的 Content）。</summary>
    public static EquipmentReplaceListView Create(
        RectTransform parent,
        TMP_FontAsset font,
        UpgradeDefinition newVariant,
        List<EquippedItem> items,
        Action<UpgradeDefinition> onPicked,
        Action onSkip = null)
    {
        var go = new GameObject("EquipmentReplaceList", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -12f);
        rt.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        var view = go.AddComponent<EquipmentReplaceListView>();
        view.Build(font, newVariant, items, onPicked, onSkip);
        return view;
    }

    private void Build(TMP_FontAsset font, UpgradeDefinition newVariant, List<EquippedItem> items,
        Action<UpgradeDefinition> onPicked, Action onSkip)
    {
        _onPicked = onPicked;

        var background = gameObject.AddComponent<Image>();
        background.sprite = ProceduralUiSprites.RoundedPlate(18, 2, 0.94f, 1f);
        background.type = Image.Type.Sliced;
        background.color = new Color(0.05f, 0.06f, 0.08f, 0.96f);
        background.raycastTarget = true;

        // 标题让出右上角给「跳过」按钮。
        var title = CreateText(transform as RectTransform, font, 22f, Color.white);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(-55f, -10f);
        titleRect.sizeDelta = new Vector2(-150f, 34f);
        string newName = newVariant != null ? newVariant.displayName : "新装备";
        title.text = "槽位已满：选下方旧装备，用「" + newName + "」替换";
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.96f, 0.77f, 0.26f, 1f);

        BuildSkipButton(font, onSkip);

        int count = items != null ? items.Count : 0;
        float totalWidth = count * ChipWidth + Mathf.Max(0, count - 1) * ChipGap;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var item = items[i];
            if (item.definition == null) continue;
            float x = startX + i * (ChipWidth + ChipGap) + ChipWidth * 0.5f;
            BuildChip(font, item, x);
        }
    }

    /// <summary>右上角「跳过」：放弃本次装备奖励，保留原装备（不发放补偿）。</summary>
    private void BuildSkipButton(TMP_FontAsset font, Action onSkip)
    {
        var go = new GameObject("SkipButton", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -8f);
        rt.sizeDelta = new Vector2(104f, 34f);

        var background = go.AddComponent<Image>();
        background.sprite = ProceduralUiSprites.RoundedPlate(10, 2, 0.95f, 1f);
        background.type = Image.Type.Sliced;
        background.color = new Color(0.24f, 0.14f, 0.10f, 0.98f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = background;
        if (onSkip != null) button.onClick.AddListener(() => onSkip());

        var text = CreateText(rt, font, 22f, new Color(0.96f, 0.77f, 0.26f, 1f));
        text.text = "跳过";
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;
    }

    private void BuildChip(TMP_FontAsset font, EquippedItem item, float x)
    {
        var go = new GameObject("OldEquip", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(x, 12f);
        rt.sizeDelta = new Vector2(ChipWidth, ChipHeight);

        var background = go.AddComponent<Image>();
        background.sprite = ProceduralUiSprites.RoundedPlate(14, 2, 0.9f, 1f);
        background.type = Image.Type.Sliced;
        background.color = new Color(0.15f, 0.13f, 0.11f, 0.98f);

        var button = go.AddComponent<Button>();
        button.targetGraphic = background;
        var captured = item.definition;
        button.onClick.AddListener(() => _onPicked?.Invoke(captured));

        // 图标（左上）
        var iconGo = new GameObject("Icon", typeof(RectTransform));
        var iconRect = (RectTransform)iconGo.transform;
        iconRect.SetParent(rt, false);
        iconRect.anchorMin = new Vector2(0f, 1f);
        iconRect.anchorMax = new Vector2(0f, 1f);
        iconRect.pivot = new Vector2(0f, 1f);
        iconRect.anchoredPosition = new Vector2(8f, -8f);
        iconRect.sizeDelta = new Vector2(52f, 52f);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = item.definition.icon;
        icon.enabled = item.definition.icon != null;
        icon.raycastTarget = false;

        // 名称（图标右侧）
        var name = CreateText(rt, font, 22f, UpgradeQualityStyle.Color(item.definition.quality));
        var nameRect = name.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(66f, -10f);
        nameRect.sizeDelta = new Vector2(-72f, 32f);
        name.text = item.definition.displayName;
        name.alignment = TextAlignmentOptions.Left;

        // 品质 + 等级（名称下方）
        var level = CreateText(rt, font, 20f, new Color(0.82f, 0.82f, 0.82f, 1f));
        var levelRect = level.rectTransform;
        levelRect.anchorMin = new Vector2(0f, 0f);
        levelRect.anchorMax = new Vector2(1f, 0f);
        levelRect.pivot = new Vector2(0.5f, 0f);
        levelRect.anchoredPosition = new Vector2(0f, 8f);
        levelRect.sizeDelta = new Vector2(-12f, 26f);
        level.text = UpgradeQualityStyle.QualityAndLevel(item.definition.quality, Mathf.Max(1, item.level));
        level.alignment = TextAlignmentOptions.Center;
    }

    private static TextMeshProUGUI CreateText(RectTransform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private void OnDestroy()
    {
        _onPicked = null;
    }
}
