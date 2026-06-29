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
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button button;

    private UpgradeDefinition _upgradeDef;

    public void Setup(UpgradeDefinition def)
    {
        _upgradeDef = def;
        if (nameText != null)
        {
            string name = def.displayName;
            int currentLevel = UpgradeEffectManager.Instance != null
                ? UpgradeEffectManager.Instance.GetUpgradeLevel(def.upgradeId) : 0;
            int nextLevel = currentLevel + 1;

            string levelSuffix;
            if (currentLevel == 0)
                levelSuffix = "新获得";
            else if (nextLevel == def.maxLevel)
                levelSuffix = $"Lv.{currentLevel} → Lv.MAX";
            else
                levelSuffix = $"Lv.{currentLevel} → Lv.{nextLevel}";

            nameText.text = $"{name}  {levelSuffix}";
        }
        if (descriptionText != null)
            descriptionText.text = UpgradeEffectManager.Instance != null
                ? UpgradeEffectManager.Instance.GetDescription(def)
                : def.descriptionTemplate;
        if (iconImage != null && def.icon != null)
            iconImage.sprite = def.icon;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        Debug.Log($"[UpgradeCard] OnClicked frame={Time.frameCount} timeScale={Time.timeScale} upgradeId={_upgradeDef?.upgradeId}");
        if (_upgradeDef != null && UpgradeChoiceManager.Instance != null)
            UpgradeChoiceManager.Instance.ConfirmChoice(_upgradeDef);
    }

}
