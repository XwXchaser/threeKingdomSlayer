using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 英雄 HUD — 独立 Prefab，每个武将可拥有不同的布局和素材。
/// 挂载到英雄 HUD Prefab 根节点，BattleHUD 在战斗开始时通过 HeroConfig.heroHUDPrefab 实例化。
/// 所有字段按需拖拽，不需要的可以留空。
/// </summary>
public class HeroHUD : MonoBehaviour
{
    [Header("生命值")]
    public Slider healthSlider;
    public TMP_Text healthText;

    [Header("复活次数")]
    public TMP_Text reviveText;

    [Header("冷却指示器 (FillMethod=Horizontal)")]
    public Image stabCooldownImage;
    public Image slashCooldownImage;
    public Image pierceCooldownImage;
    public Image sweepCooldownImage;
    public Image launchCooldownImage;
    public Image parryCooldownImage;

    [Header("冷却充能指示器 (Radial 填充)")]
    public Image stabChargeFill;
    public Image slashChargeFill;
    public Image pierceChargeFill;
    public Image sweepChargeFill;
    public Image launchChargeFill;
    public Image parryChargeFill;

    [Header("QTE 老虎机")]
    [Tooltip("QTE 判定框 RectTransform（含 RectMask2D 裁剪）")]
    public RectTransform qteFrameRect;
    [Tooltip("QTE 指示器生成区域（QTEFrame 下的空节点）")]
    public RectTransform qteIndicatorArea;

    // 血量条默认颜色缓存
    private Color _healthBarDefaultColor;
    private bool _healthBarColorSaved;

    #region 公共接口

    public void SetHealth(float current, float max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    public void SetReviveCount(int count)
    {
        if (reviveText != null)
        {
            reviveText.text = $"复活: {count}";
            reviveText.gameObject.SetActive(count > 0);
        }
    }

    public void SetHealthBarColor(Color color)
    {
        if (healthSlider != null && healthSlider.fillRect != null)
        {
            var img = healthSlider.fillRect.GetComponent<Image>();
            if (img != null)
            {
                if (!_healthBarColorSaved)
                {
                    _healthBarDefaultColor = img.color;
                    _healthBarColorSaved = true;
                }
                img.color = color;
            }
        }
    }

    public void ResetHealthBarColor()
    {
        if (_healthBarColorSaved)
            SetHealthBarColor(_healthBarDefaultColor);
    }

    /// <summary>
    /// 更新冷却指示器 fillAmount（0=可用, 1=冷却中）
    /// </summary>
    public void SetCooldown(AttackType type, float progress)
    {
        var img = GetCooldownImage(type);
        if (img != null)
        {
            img.fillAmount = progress;
            img.color = progress > 0f ? Color.red : Color.green;
        }
    }

    /// <summary>
    /// 更新冷却充能 Radial 填充（0→1, 技能就绪时=1）
    /// </summary>
    public void SetChargeFill(AttackType type, float fillAmount)
    {
        var img = GetChargeFillImage(type);
        if (img != null)
            img.fillAmount = fillAmount;
    }

    #endregion

    #region 内部

    private Image GetCooldownImage(AttackType type)
    {
        switch (type)
        {
            case AttackType.Stab:   return stabCooldownImage;
            case AttackType.Slash:  return slashCooldownImage;
            case AttackType.Pierce: return pierceCooldownImage;
            case AttackType.Sweep:  return sweepCooldownImage;
            case AttackType.Launch: return launchCooldownImage;
            case AttackType.Parry:  return parryCooldownImage;
            default: return null;
        }
    }

    private Image GetChargeFillImage(AttackType type)
    {
        switch (type)
        {
            case AttackType.Stab:   return stabChargeFill;
            case AttackType.Slash:  return slashChargeFill;
            case AttackType.Pierce: return pierceChargeFill;
            case AttackType.Sweep:  return sweepChargeFill;
            case AttackType.Launch: return launchChargeFill;
            case AttackType.Parry:  return parryChargeFill;
            default: return null;
        }
    }

    #endregion
}
