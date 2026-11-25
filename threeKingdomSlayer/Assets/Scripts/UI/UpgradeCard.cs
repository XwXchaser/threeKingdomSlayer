using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 三选一选项卡片 — 显示单个升级选项的名称、描述、稀有度背景
///
/// **选择方式：点按确认**（2026-11-16 用户定：从长按改回点按）。
///   按一下这张卡 → 立即选中并确认（走 Unity 标准 Button 的 onClick）。
///   ⚠ 之前是**长按确认**（2026-11-05 用户定“不要点两下”）——已按 2026-11-16 的口径改回点按。
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [Header("UI 组件")]
    public Image backgroundImage;
    public Image iconFrameImage;
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI cooldownText;
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
    private bool _showAsFixedVariant;

    public UpgradeDefinition Definition => _upgradeDef;

    /// <summary>
    /// 填充卡片。
    /// </summary>
    /// <param name="showAsFixedVariant">
    /// true = **装备变体**（2026-11-25）：显示「品质 Lv.可见等级」与**该等级**的实际数值，
    /// 不做「当前 → 下一级」对比（同款选到是替换，不是 +1）。
    /// false = 属性 / V1：保持既有「当前 → 下一级」预览。
    /// </param>
    public void Setup(UpgradeDefinition def, Action<UpgradeCard> onClicked = null, bool showAsFixedVariant = false)
    {
        _upgradeDef = def;
        _onClicked = onClicked;
        _showAsFixedVariant = showAsFixedVariant;
        if (!_hasBasePosition)
        {
            _baseAnchoredPosition = ((RectTransform)transform).anchoredPosition;
            _baseIconFrameSprite = iconFrameImage != null ? iconFrameImage.sprite : null;
            _hasBasePosition = true;
        }
        SetSelected(false);
        Color qualityColor = UpgradeQualityStyle.Color(def.quality);
        if (nameText != null)
        {
            nameText.text = def.displayName;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = qualityColor;   // 品质靠颜色 + 等级文本一起读，不改数值
        }
        if (descriptionText != null)
        {
            if (_showAsFixedVariant)
                descriptionText.text = UpgradeEffectManager.Instance != null
                    ? UpgradeEffectManager.Instance.GetUpgradeDescriptionAtLevel(def, Mathf.Max(1, def.visibleLevel))
                    : def.descriptionTemplate;
            else
                descriptionText.text = UpgradeEffectManager.Instance != null
                    ? UpgradeEffectManager.Instance.GetUpgradePreviewDescription(def)
                    : def.descriptionTemplate;
        }

        int currentLevel = GetCurrentLevel(def);
        int maxLevel = UpgradeEffectManager.Instance != null
            ? UpgradeEffectManager.Instance.GetEffectiveMaxLevel(def)
            : Mathf.Max(1, def != null ? def.maxLevel : 1);
        if (levelStatusText != null)
        {
            if (_showAsFixedVariant)
            {
                // 装备变体：品质 + 可见等级（选中就是这一件，不会变成 +1）
                levelStatusText.text = UpgradeQualityStyle.QualityAndLevel(def.quality, Mathf.Max(1, def.visibleLevel));
            }
            else if (currentLevel <= 0)
            {
                levelStatusText.text = UpgradeQualityStyle.Name(def.quality) + " · 新获得";
            }
            else if (currentLevel >= maxLevel)
            {
                levelStatusText.text = UpgradeQualityStyle.Name(def.quality) + " · Lv.Max";
            }
            else if (currentLevel + 1 >= maxLevel)
            {
                levelStatusText.text = UpgradeQualityStyle.Name(def.quality) + $" · Lv.{currentLevel} → Lv.Max";
            }
            else
            {
                levelStatusText.text = UpgradeQualityStyle.Name(def.quality) + $" · Lv.{currentLevel} → Lv.{currentLevel + 1}";
            }
            levelStatusText.color = qualityColor;
        }

        bool isActiveSkill = def.category == UpgradeCategory.ActiveSkill;
        if (cooldownText != null)
        {
            float nextCooldown = _showAsFixedVariant
                ? GetDisplayCooldown(def, Mathf.Max(1, def.visibleLevel))
                : GetDisplayCooldown(def, currentLevel + 1);
            bool hasCooldown = nextCooldown > 0f;
            cooldownText.gameObject.SetActive(hasCooldown);
            if (hasCooldown)
            {
                if (_showAsFixedVariant)
                {
                    cooldownText.text = $"冷却时间 <b><color=#F5C542>{nextCooldown:0}</color></b> 秒";
                }
                else
                {
                    float currentCooldown = currentLevel > 0 ? GetDisplayCooldown(def, currentLevel) : 0f;
                    string cooldownValue = currentLevel > 0 && !Mathf.Approximately(currentCooldown, nextCooldown)
                        ? $"{currentCooldown:0} → <b><color=#F5C542>{nextCooldown:0}</color></b>"
                        : $"<b><color=#F5C542>{nextCooldown:0}</color></b>";
                    cooldownText.text = $"冷却时间 {cooldownValue} 秒";
                }
            }
        }
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
            // **点按确认**：走 Unity 标准 Button 的 onClick（按下+抬起都落在卡上 = 一次点按）。
            // · 不可选的卡 button.interactable = false → 不会触发（见 SetSelectable）
            // · 弹窗把 timeScale 压成 0 不影响 UI 事件
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnTapped);
            button.interactable = true;      // 默认可选（教学会再按需要关掉某些卡）
        }
        SetSelectable(true);
    }

    /// <summary>点按这张卡 → 立即确认。</summary>
    private void OnTapped()
    {
        if (button != null && !button.interactable) return;
        SetSelected(true);
        Debug.Log($"[UpgradeCard] 点按确认 frame={Time.frameCount} upgradeId={_upgradeDef?.upgradeId}");
        _onClicked?.Invoke(this);
    }

    /// <summary>
    /// 设成不可选（教学的三选1 里只让玩家选指定的那张）：
    /// 按钮不可点 + 整张卡变暗，玩家一眼看出「这张现在不能选」。
    /// </summary>
    public void SetSelectable(bool selectable)
    {
        if (button != null) button.interactable = selectable;
        float a = selectable ? 1f : 0.4f;
        if (backgroundImage != null)
        {
            var c = backgroundImage.color; c.a = a; backgroundImage.color = c;
        }
        if (iconImage != null)
        {
            var c = iconImage.color; c.a = a; iconImage.color = c;
        }
        if (iconFrameImage != null)
        {
            var c = iconFrameImage.color; c.a = a; iconFrameImage.color = c;
        }
        if (nameText != null)
        {
            var c = nameText.color; c.a = a; nameText.color = c;
        }
        if (descriptionText != null)
        {
            var c = descriptionText.color; c.a = a; descriptionText.color = c;
        }
    }

    private static float GetDisplayCooldown(UpgradeDefinition def, int level)
    {
        if (def is ActiveSkillDefinition activeSkill)
            return activeSkill.GetCooldown(level);

        return def.category == UpgradeCategory.TimedPassive
            ? def.GetTriggerInterval(level)
            : 0f;
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

    // ── 长按确认 ────────────────────────────────────────────────────────

    /// <summary>当前这张卡是否可选（教学会把非推荐卡置为不可选）。</summary>
    private bool IsSelectable => button == null || button.interactable;
}
