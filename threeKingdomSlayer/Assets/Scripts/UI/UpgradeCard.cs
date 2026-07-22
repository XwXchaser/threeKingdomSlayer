using System;
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
    [Header("选中状态")]
    [SerializeField] private Image selectedGlowImage;
    [SerializeField] private float selectedLift = 16f;

    private UpgradeDefinition _upgradeDef;
    private Action<UpgradeCard> _onClicked;
    private Vector2 _baseAnchoredPosition;
    private bool _hasBasePosition;

    public UpgradeDefinition Definition => _upgradeDef;

    public void Setup(UpgradeDefinition def, Action<UpgradeCard> onClicked = null)
    {
        _upgradeDef = def;
        _onClicked = onClicked;
        if (!_hasBasePosition)
        {
            _baseAnchoredPosition = ((RectTransform)transform).anchoredPosition;
            _hasBasePosition = true;
        }
        SetSelected(false);
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
                levelSuffix = $"Lv.{currentLevel} → MAX";
            else
                levelSuffix = $"Lv.{currentLevel} → {nextLevel}";

            nameText.text = $"{name}\n<size=70%>{levelSuffix}</size>";
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

    public void SetSelectedGlow(Image glow)
    {
        selectedGlowImage = glow;
        selectedGlowImage.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedGlowImage != null)
            selectedGlowImage.gameObject.SetActive(selected);
        if (_hasBasePosition)
            ((RectTransform)transform).anchoredPosition = _baseAnchoredPosition + (selected ? Vector2.up * selectedLift : Vector2.zero);
    }

    private void OnClicked()
    {
        Debug.Log($"[UpgradeCard] OnClicked frame={Time.frameCount} timeScale={Time.timeScale} upgradeId={_upgradeDef?.upgradeId}");
        _onClicked?.Invoke(this);
    }

}
