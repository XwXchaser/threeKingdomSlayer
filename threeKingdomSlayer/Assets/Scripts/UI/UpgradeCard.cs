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
    public Image iconFrameImage;
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI levelStatusText;
    public Image passiveTagImage;
    public Image activeTagImage;
    public Sprite activeSkillFrameSprite;
    public Button button;
    [Header("选中状态")]
    [SerializeField] private Image selectedGlowImage;
    [SerializeField] private float selectedLift = 16f;

    private UpgradeDefinition _upgradeDef;
    private Action<UpgradeCard> _onClicked;
    private Vector2 _baseAnchoredPosition;
    private Sprite _baseIconFrameSprite;
    private bool _hasBasePosition;

    public UpgradeDefinition Definition => _upgradeDef;

    public void Setup(UpgradeDefinition def, Action<UpgradeCard> onClicked = null)
    {
        _upgradeDef = def;
        _onClicked = onClicked;
        if (!_hasBasePosition)
        {
            _baseAnchoredPosition = ((RectTransform)transform).anchoredPosition;
            _baseIconFrameSprite = iconFrameImage != null ? iconFrameImage.sprite : null;
            _hasBasePosition = true;
        }
        SetSelected(false);
        if (nameText != null)
        {
            nameText.text = def.displayName;
            nameText.alignment = TextAlignmentOptions.Center;
        }
        if (descriptionText != null)
            descriptionText.text = UpgradeEffectManager.Instance != null
                ? UpgradeEffectManager.Instance.GetDescription(def)
                : def.descriptionTemplate;

        int currentLevel = GetCurrentLevel(def);
        if (levelStatusText != null)
        {
            if (currentLevel <= 0)
                levelStatusText.text = "新获得";
            else if (currentLevel >= def.maxLevel)
                levelStatusText.text = "Lv.Max";
            else if (currentLevel + 1 >= def.maxLevel)
                levelStatusText.text = $"Lv.{currentLevel} → Lv.Max";
            else
                levelStatusText.text = $"Lv.{currentLevel} → Lv.{currentLevel + 1}";
        }

        bool isActiveSkill = def.category == UpgradeCategory.ActiveSkill;
        if (activeTagImage != null)
            activeTagImage.gameObject.SetActive(isActiveSkill);
        if (passiveTagImage != null)
            passiveTagImage.gameObject.SetActive(!isActiveSkill && def.category != UpgradeCategory.Item);
        if (iconFrameImage != null)
            iconFrameImage.sprite = isActiveSkill && activeSkillFrameSprite != null
                ? activeSkillFrameSprite
                : _baseIconFrameSprite;

        if (iconImage != null && def.icon != null)
            iconImage.sprite = def.icon;
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }
    }

    private static int GetCurrentLevel(UpgradeDefinition def)
    {
        if (def == null) return 0;
        if (def.category == UpgradeCategory.ActiveSkill)
            return ActiveSkillInventory.Instance != null ? ActiveSkillInventory.Instance.GetLevel(def.upgradeId) : 0;
        return UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetUpgradeLevel(def.upgradeId) : 0;
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
