using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays active combo buffs next to the hero portrait.
/// </summary>
public class ComboBuffHUD : MonoBehaviour
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private SpriteNumberDisplay _bonusDisplay;
    [SerializeField] private Sprite _damageBuffIcon;

    private ComboManager _comboManager;
    private CanvasGroup _canvasGroup;
    private bool _isShowing;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void OnEnable()
    {
        TrySubscribe();
        Refresh();
    }

    private void Update()
    {
        if (_comboManager == null)
            TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void TrySubscribe()
    {
        if (_comboManager != null || ComboManager.Instance == null) return;

        _comboManager = ComboManager.Instance;
        _comboManager.OnComboTrigger += OnComboChanged;
        _comboManager.OnComboUpdated += OnComboUpdated;
        Refresh();
    }

    private void Unsubscribe()
    {
        if (_comboManager == null) return;

        _comboManager.OnComboTrigger -= OnComboChanged;
        _comboManager.OnComboUpdated -= OnComboUpdated;
        _comboManager = null;
    }

    private void OnComboChanged(string _)
    {
        Refresh();
    }

    private void OnComboUpdated(int combo)
    {
        if (combo == 0)
            SetVisible(false);
    }

    private void Refresh()
    {
        var buffs = BuffManager.Instance != null ? BuffManager.Instance.ActiveBuffs : null;
        if (buffs == null)
        {
            SetVisible(false);
            return;
        }

        for (int i = 0; i < buffs.Count; i++)
        {
            var buff = buffs[i];
            if (buff.buffId != "combo_damage") continue;

            int bonusPercent = GetBonusPercent(buff.modifiers);
            if (bonusPercent <= 0) continue;

            if (_iconImage != null)
                _iconImage.sprite = _damageBuffIcon;
            _bonusDisplay?.ShowSignedPercent(bonusPercent);
            SetVisible(true);
            return;
        }

        SetVisible(false);
    }

    private static int GetBonusPercent(List<StatModifier> modifiers)
    {
        if (modifiers == null) return 0;

        float bonus = 0f;
        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (modifier == null || modifier.statId != "atk") continue;

            bonus += modifier.type == StatModifierType.Multiply ? modifier.value * 100f : modifier.value;
        }
        return Mathf.RoundToInt(bonus);
    }

    private void SetVisible(bool visible)
    {
        _isShowing = visible;
        if (!visible)
            _bonusDisplay?.Clear();
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }
    }
}
