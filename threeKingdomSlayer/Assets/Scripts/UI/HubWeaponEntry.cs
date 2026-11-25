using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **落地页「武器」入口**（2026-11-24，简易版）。
///
/// 为什么不改场景：这一版先用**运行时克隆**落地页上已有的「天赋」卡片做一个同级入口
/// （`HubTalentEntry`），位置放在它左边一格；等 UI 布局与美术定稿时再进编辑器正式摆位。
///
/// 入口只在**配了主武器目录**时出现（`HeroConfig.weaponCatalog`），否则点了也没内容可看。
/// </summary>
public class HubWeaponEntry : MonoBehaviour
{
    [Header("交互")]
    public Button button;

    [Header("文案")]
    public TMP_Text label;
    public string labelText = "武器";

    /// <summary>在落地页准备一个入口（幂等：已经有了就什么都不做）。</summary>
    public static void EnsureCreated()
    {
        var existing = Object.FindObjectsOfType<HubWeaponEntry>(true);
        if (existing != null && existing.Length > 0) return;

        var source = Object.FindObjectOfType<HubTalentEntry>(true);
        if (source == null) return;                                  // 不在落地页

        if (WeaponService.Catalog == null) return;                   // 没配主武器目录 → 不出入口

        var parent = source.transform.parent;
        if (parent == null) return;

        var clone = Object.Instantiate(source.gameObject, parent);
        clone.name = "HubWeaponEntry";
        clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        var srcRt = (RectTransform)source.transform;
        var rt = (RectTransform)clone.transform;
        // 放在「天赋」卡左边一格（同为 (1,0) 锚点，右对齐；x 往左让一个卡宽 + 间距）
        rt.anchoredPosition = srcRt.anchoredPosition + new Vector2(-(srcRt.sizeDelta.x + 12f), 0f);

        // 克隆来的卡片带 HubTalentEntry：会抢着把按钮接到天赋页，先拆掉
        var old = clone.GetComponent<HubTalentEntry>();
        if (old != null) Object.Destroy(old);

        var entry = clone.AddComponent<HubWeaponEntry>();
        entry.button = clone.GetComponent<Button>();
        entry.label = clone.GetComponentInChildren<TMP_Text>(true);
        entry.ApplyLabel();
        entry.Bind();
    }

    private void OnEnable()
    {
        Bind();
        ApplyLabel();
    }

    public void ApplyLabel()
    {
        if (label != null) label.text = labelText;
    }

    public void Bind()
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        WeaponLoadoutPanel.GetOrCreate().Toggle();
    }
}
