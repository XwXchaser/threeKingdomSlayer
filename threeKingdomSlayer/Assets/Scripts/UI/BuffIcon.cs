using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// BuffDisplayPanel 中的单个图标 — 数值型/被动型/道具型通用。
/// 道具型可点击触发消耗。
/// </summary>
public class BuffIcon : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _frameImage;
    [SerializeField] private Button _button;

    [Header("右上角精灵数字显示")]
    [SerializeField] private SpriteNumberDisplay _spriteNumberDisplay;

    [Header("底部角标精灵数字")]
    [SerializeField] private SpriteNumberDisplay _badgeNumberDisplay;

    [Header("冷却显示（计时被动专用）")]
    [SerializeField] private Image _cooldownDim;
    [SerializeField] private Image _cooldownFill;

    [Header("道具就绪光效")]
    [SerializeField] private float _readyGlowSpeed = 1.2f;
    [SerializeField, Range(0f, 1f)] private float _readyGlowMinAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _readyGlowMaxAlpha = 0.95f;
    [SerializeField] private float _readyGlowPadding = 10f;

    private Image _readyGlow;
    private Outline _readyGlowOutline;
    private bool _showReadyGlow;

    [Header("主动技能按钮")]
    [SerializeField] private Image _activeButtonFrame;
    [SerializeField] private RectTransform _activeButtonFace;
    [SerializeField] private float _activeButtonPressedOffset = 8f;
    [SerializeField] private float _activeButtonReturnDuration = 0.12f;

    private Vector2 _activeButtonRestPosition;
    private bool _activeButtonPositionCached;
    private bool _activeButtonPressed;
    private Tween _activeButtonTween;

    public string UpgradeId { get; private set; }
    public string GestureId { get; private set; }
    public UpgradeCategory Category { get; private set; }
    public Sprite IconSprite => _iconImage != null ? _iconImage.sprite : null;
    public int BadgeNumber { get; private set; }

    public System.Action<BuffIcon> OnClicked;

    public void Setup(Sprite icon, string upgradeId, UpgradeCategory category, string gestureId)
    {
        UpgradeId = upgradeId;
        Category = category;
        GestureId = gestureId;

        if (_iconImage != null)
        {
            _iconImage.sprite = icon;
            _iconImage.enabled = icon != null;
            if (icon != null)
                SyncCooldownSprite(icon);
        }

        bool isActive = category == UpgradeCategory.ActiveSkill;
        ApplyActiveButtonStyle(isActive);

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            if (category == UpgradeCategory.Item || category == UpgradeCategory.ActiveSkill)
            {
                _button.interactable = true;
                _button.onClick.AddListener(() => OnClicked?.Invoke(this));
                if (_iconImage != null) _iconImage.raycastTarget = true;
                SetReadyGlow(icon != null);
            }
            else
            {
                _button.interactable = false;
                SetReadyGlow(false);
            }
        }
    }

    private void ApplyActiveButtonStyle(bool isActiveSkill)
    {
        if (_frameImage != null)
            _frameImage.gameObject.SetActive(!isActiveSkill);
        if (_activeButtonFrame != null)
            _activeButtonFrame.gameObject.SetActive(isActiveSkill);
        if (_activeButtonFace != null)
            _activeButtonFace.gameObject.SetActive(isActiveSkill);
    }

    /// <summary>强制切换槽位样式（供 FrontItemBar 在初始化空槽位时调用）</summary>
    public void SetActiveSlotStyle(bool isActiveSkill)
    {
        ApplyActiveButtonStyle(isActiveSkill);
    }

    /// <summary>设置底部角标数字（道具次数/血包数量）</summary>
    public void SetBadgeNumber(int value)
    {
        BadgeNumber = value;
        if (value < 0)
            _badgeNumberDisplay?.Clear();
        else
            _badgeNumberDisplay?.ShowNumber(value);
    }

    /// <summary>清除底部角标</summary>
    public void ClearBadgeNumber()
    {
        BadgeNumber = 0;
        _badgeNumberDisplay?.Clear();
    }

    /// <summary>右上角显示百分比数字（精灵）</summary>
    public void SetPercentNumber(int value)
    {
        _spriteNumberDisplay?.ShowPercent(value);
    }

    /// <summary>右上角显示倒计时秒数（精灵）</summary>
    public void SetCountdownNumber(int seconds)
    {
        _spriteNumberDisplay?.ShowCountdown(seconds);
    }

    /// <summary>右上角显示纯数字（精灵）</summary>
    public void SetTopRightNumber(int value)
    {
        _spriteNumberDisplay?.ShowNumber(value);
    }

    /// <summary>清除右上角数字显示</summary>
    public void ClearTopRightNumber()
    {
        _spriteNumberDisplay?.Clear();
    }

    private void Update()
    {
        if (!_showReadyGlow || _readyGlow == null) return;
        float wave = Mathf.Sin(Time.unscaledTime * _readyGlowSpeed * Mathf.PI * 2f) * 0.5f + 0.5f;
        var color = _readyGlow.color;
        color.a = Mathf.Lerp(_readyGlowMinAlpha, _readyGlowMaxAlpha, wave);
        _readyGlow.color = color;
        if (_readyGlowOutline != null)
        {
            var outlineColor = _readyGlowOutline.effectColor;
            outlineColor.a = color.a;
            _readyGlowOutline.effectColor = outlineColor;
        }
    }

    private void SetReadyGlow(bool visible)
    {
        _showReadyGlow = visible;
        if (!visible)
        {
            if (_readyGlow != null) _readyGlow.gameObject.SetActive(false);
            return;
        }

        if (_iconImage == null || _iconImage.sprite == null) return;
        if (_readyGlow == null)
        {
            var go = new GameObject("ReadyGlow", typeof(RectTransform), typeof(Image));
            _readyGlow = go.GetComponent<Image>();
            _readyGlow.raycastTarget = false;
            _readyGlow.maskable = false;
            _readyGlowOutline = go.AddComponent<Outline>();
            _readyGlowOutline.effectColor = Color.white;
            _readyGlowOutline.effectDistance = new Vector2(4f, -4f);
            _readyGlowOutline.useGraphicAlpha = true;
        }

        Transform glowParent = Category == UpgradeCategory.ActiveSkill && _activeButtonFace != null
            ? _activeButtonFace
            : transform;
        if (_readyGlow.transform.parent != glowParent)
            _readyGlow.transform.SetParent(glowParent, false);

        var glowRect = _readyGlow.rectTransform;
        if (_iconImage != null)
        {
            var iconRect = _iconImage.rectTransform;
            glowRect.anchorMin = iconRect.anchorMin;
            glowRect.anchorMax = iconRect.anchorMax;
            glowRect.pivot = iconRect.pivot;
            glowRect.anchoredPosition = iconRect.anchoredPosition;
            glowRect.sizeDelta = iconRect.sizeDelta;
        }
        _readyGlow.transform.SetSiblingIndex(GetReadyGlowSiblingIndex());

        _readyGlow.sprite = _iconImage.sprite;
        _readyGlow.type = _iconImage.type;
        _readyGlow.preserveAspect = _iconImage.preserveAspect;
        _readyGlow.color = new Color(1f, 1f, 1f, _readyGlowMinAlpha);
        _readyGlow.transform.SetSiblingIndex(GetReadyGlowSiblingIndex());
        _readyGlow.gameObject.SetActive(true);
    }

    private int GetReadyGlowSiblingIndex()
    {
        if (Category == UpgradeCategory.ActiveSkill && _iconImage != null)
            return _iconImage.transform.GetSiblingIndex();

        int numberIndex = transform.childCount;
        if (_spriteNumberDisplay != null)
            numberIndex = Mathf.Min(numberIndex, _spriteNumberDisplay.transform.GetSiblingIndex());
        if (_badgeNumberDisplay != null)
            numberIndex = Mathf.Min(numberIndex, _badgeNumberDisplay.transform.GetSiblingIndex());
        return Mathf.Max(0, numberIndex - 1);
    }

    public void SetFrame(Sprite sprite)
    {
        if (_frameImage != null)
        {
            _frameImage.sprite = sprite;
            _frameImage.enabled = sprite != null;
        }
        if (_showReadyGlow)
            SetReadyGlow(_iconImage != null && _iconImage.sprite != null);
    }

    /// <summary>设置半透明状态（用于未持有的道具槽位显示）</summary>
    public void SetDimmed(bool dimmed)
    {
        if (_iconImage != null)
            _iconImage.color = dimmed ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        if (_button != null)
            _button.interactable = !dimmed;
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
            _button.interactable = interactable;
        if (Category == UpgradeCategory.Item || Category == UpgradeCategory.ActiveSkill)
            SetReadyGlow(interactable && _iconImage != null && _iconImage.sprite != null);
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
        if (Category == UpgradeCategory.Item || Category == UpgradeCategory.ActiveSkill)
            SetReadyGlow(!visible);
    }

    /// <summary>同步冷却蒙层精灵（图标变更时调用）</summary>
    public void SyncCooldownSprite(Sprite sprite)
    {
        if (_cooldownDim != null) _cooldownDim.sprite = sprite;
        if (_cooldownFill != null) _cooldownFill.sprite = sprite;
    }

    public void ShowEmpty(Sprite frame)
    {
        UpgradeId = null;
        GestureId = null;
        Category = UpgradeCategory.Item;
        ApplyActiveButtonStyle(false);
        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
            _iconImage.color = Color.white;
            _iconImage.raycastTarget = false;
        }
        SetFrame(frame);
        SetReadyGlow(false);
        ClearBadgeNumber();
        ClearTopRightNumber();
        SetCooldown(0f, null, false);
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.interactable = false;
        }
        OnClicked = null;
        gameObject.SetActive(true);
    }

    public void SetActiveButtonPressed(bool pressed)
    {
        if (_activeButtonFace == null) return;
        if (_activeButtonPressed == pressed) return;
        _activeButtonPressed = pressed;

        if (!_activeButtonPositionCached)
        {
            _activeButtonRestPosition = _activeButtonFace.anchoredPosition;
            _activeButtonPositionCached = true;
        }

        _activeButtonTween?.Kill();
        if (pressed)
        {
            _activeButtonFace.anchoredPosition = _activeButtonRestPosition + new Vector2(0f, -_activeButtonPressedOffset);
        }
        else
        {
            _activeButtonTween = _activeButtonFace.DOAnchorPos(_activeButtonRestPosition, _activeButtonReturnDuration)
                .SetEase(Ease.OutBack);
        }
    }

    /// <summary>清空图标数据并隐藏</summary>
    public void ResetSlot()
    {
        UpgradeId = null;
        GestureId = null;
        Category = UpgradeCategory.Numeric;
        if (_iconImage != null)
        {
            _iconImage.sprite = null;
            _iconImage.enabled = false;
        }
        if (_frameImage != null) { _frameImage.sprite = null; _frameImage.enabled = false; }
        SetReadyGlow(false);
        if (_badgeNumberDisplay != null) _badgeNumberDisplay.Clear();
        ClearTopRightNumber();
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
