using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 三选一选项卡片 — 显示单个升级选项的名称、描述、稀有度背景
/// 点击时通知 UpgradeChoiceManager.ConfirmChoice
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [Header("UI 组件")]
    public Image backgroundImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button button;

    [Header("稀有度背景色（占位，后续替换为图片）")]
    public Color commonColor = Color.gray;
    public Color rareColor = Color.blue;
    public Color legendaryColor = new Color(1f, 0.84f, 0f);

    private UpgradeDefinition _upgradeDef;

    public void Setup(UpgradeDefinition def)
    {
        _upgradeDef = def;
        if (nameText != null)
            nameText.text = def.displayName;
        if (descriptionText != null)
            descriptionText.text = UpgradeEffectManager.Instance != null
                ? UpgradeEffectManager.Instance.GetDescription(def)
                : def.descriptionTemplate;
        if (backgroundImage != null)
            backgroundImage.color = GetRarityColor(def.rarity);
        if (button != null)
            button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        Debug.Log($"[UpgradeCard] OnClicked frame={Time.frameCount} timeScale={Time.timeScale} upgradeId={_upgradeDef?.upgradeId}");
        if (_upgradeDef != null && UpgradeChoiceManager.Instance != null)
            UpgradeChoiceManager.Instance.ConfirmChoice(_upgradeDef);
    }

    private Color GetRarityColor(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Rare: return rareColor;
            case UpgradeRarity.Legendary: return legendaryColor;
            default: return commonColor;
        }
    }
}
