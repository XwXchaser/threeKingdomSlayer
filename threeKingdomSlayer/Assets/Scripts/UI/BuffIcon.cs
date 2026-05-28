using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BuffDisplayPanel 中的单个图标 — 数值型/被动型/道具型通用。
/// 道具型可点击触发消耗。
/// </summary>
public class BuffIcon : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private Button _button;

    public string UpgradeId { get; private set; }
    public string GestureId { get; private set; }
    public UpgradeCategory Category { get; private set; }
    public Sprite IconSprite => _iconImage != null ? _iconImage.sprite : null;
    public string BadgeText => _badgeText != null ? _badgeText.text : "";

    public System.Action<BuffIcon> OnClicked;

    public void Setup(Sprite icon, string upgradeId, UpgradeCategory category, string gestureId)
    {
        UpgradeId = upgradeId;
        Category = category;
        GestureId = gestureId;

        if (_iconImage != null && icon != null)
            _iconImage.sprite = icon;

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            if (category == UpgradeCategory.Item)
            {
                _button.interactable = true;
                _button.onClick.AddListener(() => OnClicked?.Invoke(this));
            }
            else
            {
                _button.interactable = false;
            }
        }
    }

    public void SetBadge(string text)
    {
        if (_badgeText != null)
            _badgeText.text = text;
    }

    /// <summary>清空图标数据并隐藏</summary>
    public void ResetSlot()
    {
        UpgradeId = null;
        GestureId = null;
        Category = UpgradeCategory.Numeric;
        if (_iconImage != null) _iconImage.sprite = null;
        if (_badgeText != null) _badgeText.text = "";
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.interactable = false;
        }
        OnClicked = null;
        gameObject.SetActive(false);
    }
}
