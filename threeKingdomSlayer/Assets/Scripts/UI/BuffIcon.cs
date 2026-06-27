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
    [SerializeField] private Image _frameImage;
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private Button _button;

    [Header("右上角百分比（数值型累计加成）")]
    [SerializeField] private TextMeshProUGUI _percentText;

    [Header("冷却显示（计时被动专用）")]
    [SerializeField] private Image _cooldownDim;
    [SerializeField] private Image _cooldownFill;
    [SerializeField] private TextMeshProUGUI _countdownText;

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
        {
            _iconImage.sprite = icon;
            SyncCooldownSprite(icon);
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            if (category == UpgradeCategory.Item)
            {
                _button.interactable = true;
                _button.onClick.AddListener(() => OnClicked?.Invoke(this));
                if (_iconImage != null) _iconImage.raycastTarget = true;
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

    /// <summary>设置右上角百分比文本（null/空则隐藏）</summary>
    public void SetPercentText(string text)
    {
        if (_percentText == null) return;
        _percentText.text = text;
        _percentText.gameObject.SetActive(!string.IsNullOrEmpty(text));
    }

    public void SetFrame(Sprite sprite)
    {
        if (_frameImage != null)
        {
            _frameImage.sprite = sprite;
            _frameImage.enabled = sprite != null;
        }
    }

    /// <summary>设置半透明状态（用于未持有的道具槽位显示）</summary>
    public void SetDimmed(bool dimmed)
    {
        if (_iconImage != null)
            _iconImage.color = dimmed ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        if (_button != null)
            _button.interactable = !dimmed;
    }

    /// <summary>设置冷却显示</summary>
    /// <param name="fillAmount">填充量 0=就绪 1=满冷却</param>
    /// <param name="countdown">倒计时文本，null/空则隐藏</param>
    /// <param name="visible">是否显示冷却层</param>
    public void SetCooldown(float fillAmount, string countdown, bool visible)
    {
        if (_cooldownDim != null)
            _cooldownDim.gameObject.SetActive(visible);
        if (_cooldownFill != null)
        {
            _cooldownFill.gameObject.SetActive(visible);
            if (visible) _cooldownFill.fillAmount = fillAmount;
        }
        if (_countdownText != null)
        {
            _countdownText.gameObject.SetActive(visible && !string.IsNullOrEmpty(countdown));
            if (!string.IsNullOrEmpty(countdown))
                _countdownText.text = countdown;
        }
    }

    /// <summary>同步冷却蒙层精灵（图标变更时调用）</summary>
    public void SyncCooldownSprite(Sprite sprite)
    {
        if (_cooldownDim != null) _cooldownDim.sprite = sprite;
        if (_cooldownFill != null) _cooldownFill.sprite = sprite;
    }

    /// <summary>清空图标数据并隐藏</summary>
    public void ResetSlot()
    {
        UpgradeId = null;
        GestureId = null;
        Category = UpgradeCategory.Numeric;
        if (_iconImage != null) _iconImage.sprite = null;
        if (_frameImage != null) { _frameImage.sprite = null; _frameImage.enabled = false; }
        if (_badgeText != null) _badgeText.text = "";
        SetPercentText(null);
        SetCooldown(0f, null, false);
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.interactable = false;
        }
        OnClicked = null;
        gameObject.SetActive(false);
    }
}
