using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    public Image healthBottomImage;
    public Image healthFrameImage;
    [Tooltip("护盾状态下替换血条 Fill 的精灵（留空则不改动）")]
    public Sprite shieldFillSprite;

    [Header("大招头像")]
    public Image ultimateBaseImage;
    public Image ultimateFillImage;
    public Image portraitImage;
    public UltimateButtonUI ultimateButtonUI;

    [Header("扩展UI")]
    public Transform extraUIRoot;

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
    public HeroHUDFlipCard flipCard;
    public RectTransform qteFrameRect;
    [Tooltip("QTE 指示器生成区域（QTEFrame 下的空节点）")]
    public RectTransform qteIndicatorArea;

    [Header("对话")]
    public HeroHUDDialogue dialogue;
    public Sprite PortraitSprite => portraitImage != null ? portraitImage.sprite : null;

    /// <summary>
    /// 触发对话看板翻转，显示台词。战斗事件（Boss出场、阶段切换等）调用此方法。
    /// </summary>
    public void ShowDialogue(DialogueData data)
    {
        if (data == null) return;
        Debug.LogWarning("[HeroHUD] 旧 DialogueData 已弃用，请改用 DialogueManager.Trigger(eventId)");
    }

    [Header("经验条")]
    public Slider expSlider;
    public TMP_Text expLevelText;

    [Header("关卡进度")]
    [Tooltip("关卡进度条组件（与 QTEFrame 互斥显示）")]
    public StageProgressBar stageProgressBar;

    // 血量条默认颜色缓存
    private Color _healthBarDefaultColor;
    private bool _healthBarColorSaved;
    private Sprite _defaultFillSprite;
    private bool _defaultFillSpriteCached;
    private Canvas _hudForegroundCanvas;
    private readonly List<GameObject> _extraUIInstances = new List<GameObject>();

    #region 公共接口

    private void OnEnable()
    {
        EnsurePortraitForegroundLayer();
    }

    private void Start()
    {
        EnsurePortraitForegroundLayer();
    }

    private void Update()
    {
        if (_hudForegroundCanvas != null)
            _hudForegroundCanvas.enabled = true;
    }

    private void EnsurePortraitForegroundLayer()
    {
        var hudRoot = transform.Find("HudCard");
        if (hudRoot == null) return;

        _hudForegroundCanvas = hudRoot.GetComponent<Canvas>();
        if (_hudForegroundCanvas == null)
            _hudForegroundCanvas = hudRoot.gameObject.AddComponent<Canvas>();

        _hudForegroundCanvas.overrideSorting = true;
        _hudForegroundCanvas.sortingOrder = 21;

        if (hudRoot.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            hudRoot.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    #endregion

    #region 公共接口

    public void ApplySkin(HeroHUDSkin skin)
    {
        ClearExtraUI();
        if (skin == null) return;

        SetImageSprite(healthBottomImage, skin.healthBottomSprite);
        SetImageSprite(GetHealthFillImage(), skin.healthFillSprite);
        SetImageSprite(healthFrameImage, skin.healthFrameSprite);
        if (skin.shieldFillSprite != null)
        {
            shieldFillSprite = skin.shieldFillSprite;
            _defaultFillSpriteCached = false;
        }

        SetImageSprite(ultimateBaseImage, skin.ultimateBaseSprite);
        SetImageSprite(ultimateFillImage, skin.ultimateFillSprite);
        SetImageSprite(portraitImage, skin.portraitSprite);
        if (ultimateButtonUI != null)
            ultimateButtonUI.ApplyReadyEffectSkin(skin.readyFireStartSprite, skin.readyFireLoopSprites, skin.readyFireFps);

        SetSkillSprites(stabCooldownImage, stabChargeFill, skin.stabIcon, skin.stabChargeSprite);
        SetSkillSprites(slashCooldownImage, slashChargeFill, skin.slashIcon, skin.slashChargeSprite);
        SetSkillSprites(pierceCooldownImage, pierceChargeFill, skin.pierceIcon, skin.pierceChargeSprite);
        SetSkillSprites(sweepCooldownImage, sweepChargeFill, skin.sweepIcon, skin.sweepChargeSprite);
        SetSkillSprites(launchCooldownImage, launchChargeFill, skin.launchIcon, skin.launchChargeSprite);
        SetSkillSprites(parryCooldownImage, parryChargeFill, skin.parryIcon, skin.parryChargeSprite);

        var parent = extraUIRoot != null ? extraUIRoot : transform;
        if (skin.extraUIPrefabs != null)
        {
            for (int i = 0; i < skin.extraUIPrefabs.Length; i++)
            {
                var prefab = skin.extraUIPrefabs[i];
                if (prefab == null) continue;
                _extraUIInstances.Add(Instantiate(prefab, parent));
            }
        }
    }

    public void SetHealth(float current, float max, int shieldAmount = 0)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null)
        {
            if (shieldAmount > 0)
                healthText.text = $"{Mathf.CeilToInt(current)}+({shieldAmount})/{Mathf.CeilToInt(max)}";
            else
                healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
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
    /// 切换血条 Fill 精灵为护盾版本 / 恢复默认
    /// </summary>
    public void SetShieldFillActive(bool active)
    {
        if (healthSlider == null || healthSlider.fillRect == null || shieldFillSprite == null) return;
        var img = healthSlider.fillRect.GetComponent<Image>();
        if (img == null) return;
        if (!_defaultFillSpriteCached)
        {
            _defaultFillSprite = img.sprite;
            _defaultFillSpriteCached = true;
        }
        img.sprite = active ? shieldFillSprite : _defaultFillSprite;
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

    private Image GetHealthFillImage()
    {
        if (healthSlider == null || healthSlider.fillRect == null) return null;
        return healthSlider.fillRect.GetComponent<Image>();
    }

    private void SetImageSprite(Image image, Sprite sprite)
    {
        if (image != null && sprite != null)
            image.sprite = sprite;
    }

    private void SetSkillSprites(Image icon, Image chargeFill, Sprite iconSprite, Sprite chargeSprite)
    {
        SetImageSprite(icon, iconSprite);
        SetImageSprite(chargeFill, chargeSprite != null ? chargeSprite : iconSprite);
    }

    private void ClearExtraUI()
    {
        for (int i = 0; i < _extraUIInstances.Count; i++)
        {
            if (_extraUIInstances[i] != null)
                Destroy(_extraUIInstances[i]);
        }
        _extraUIInstances.Clear();
    }

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
