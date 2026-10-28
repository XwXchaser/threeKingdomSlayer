using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 英雄 HUD — 独立 Prefab，每个武将可拥有不同的布局和素材。
/// 挂载到英雄 HUD Prefab 根节点，BattleHUD 在战斗开始时通过 HeroConfig.heroHUDPrefab 实例化。
/// 所有字段按需拖拽，不需要的可以留空。
/// </summary>
public class HeroHUD : MonoBehaviour, IQteHud
{
    public static HeroHUD Instance { get; private set; }
    [Header("生命值（通栏 · 两侧向中间收缩）")]
    [Tooltip("左半块填充：Filled / Horizontal / fillOrigin=Right（贴中心，向左收缩）")]
    public Image healthFillLeft;
    [Tooltip("右半块填充：Filled / Horizontal / fillOrigin=Left（贴中心，向右收缩）")]
    public Image healthFillRight;
    public TMP_Text healthText;
    public Image healthBottomImage;
    public Image healthFrameImage;
    [Tooltip("护盾状态下替换血条 Fill 的精灵（留空则不改动）")]
    public Sprite shieldFillSprite;

    [Header("等级球")]
    [Tooltip("球内液体层：Filled / Vertical / fillOrigin=Bottom（自下而上「水上涨」）")]
    public Image levelOrbFill;
    [Tooltip("等级数字（只显示等级，不显示具体经验值）")]
    public TMP_Text levelOrbText;
    [Tooltip("等级球本体 —— 经验宝石的飞行目标")]
    public RectTransform levelOrbTarget;

    [Header("大招头像")]
    public Image ultimateBaseImage;
    public Image ultimateFillImage;
    public Image portraitImage;
    public UltimateButtonUI ultimateButtonUI;

    [Header("扩展UI")]
    public Transform extraUIRoot;

    [Header("复活次数")]
    public TMP_Text reviveText;

    [Header("QTE 老虎机")]
    [Tooltip("QTE 判定框 RectTransform（含 RectMask2D 裁剪）")]
    public HeroHUDFlipCard flipCard;
    public RectTransform qteFrameRect;
    [Tooltip("QTE 指示器生成区域（QTEFrame 下的空节点）")]
    public RectTransform qteIndicatorArea;
    [Tooltip("严格 QTE 模式进入提示")]
    public TMP_Text strictModePrompt;

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
        if (Instance == null) Instance = this;
        EnsurePortraitForegroundLayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // 登记到呈现层装配点：QTE 演出只通过 IQteHud 端口用英雄 HUD
        //（2026-10-22 asmdef 分层第七批）。
        CombatPresenters.QteHud = this;
        EnsurePortraitForegroundLayer();
    }

    // ── IQteHud 端口 ──

    /// <summary>QTE 框（指示器用它定位）。</summary>
    public RectTransform QteFrameRect => qteFrameRect;

    /// <summary>QTE 指示器挂点。</summary>
    public RectTransform QteIndicatorArea => qteIndicatorArea;

    /// <summary>
    /// 显示「严格模式」提示并淡出。
    /// 表现（TMP 文字 + 淡出）由界面层负责 ✅ —— QTE 侧只说「要显示、显示多久」✅。
    /// </summary>
    public void ShowStrictModePrompt(float durationSeconds)
    {
        if (strictModePrompt == null) return;
        _strictModePromptTween?.Kill();
        strictModePrompt.gameObject.SetActive(true);
        strictModePrompt.alpha = 1f;
        _strictModePromptTween = strictModePrompt.DOFade(0f, durationSeconds)
            .SetUpdate(true)
            .OnComplete(() => strictModePrompt.gameObject.SetActive(false));
    }

    /// <summary>「严格模式」提示的淡出 Tween（避免与上一次淡出叠加）。</summary>
    private Tween _strictModePromptTween;

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
        SetImageSprite(healthFillLeft, skin.healthFillSprite);
        SetImageSprite(healthFillRight, skin.healthFillSprite);
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
        // 血条「两侧向中间收缩」：左右两块镜像填充，fillAmount 同步 = hp/max
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;
        if (healthFillLeft != null) healthFillLeft.fillAmount = ratio;
        if (healthFillRight != null) healthFillRight.fillAmount = ratio;
        HealthInitialized = true;
        if (healthText != null)
        {
            if (shieldAmount > 0)
                healthText.text = $"{Mathf.CeilToInt(current)}+({shieldAmount})/{Mathf.CeilToInt(max)}";
            else
                healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
        }
    }

    /// <summary>等级球：只显示等级与「水上涨」的经验比例，不显示具体经验值。</summary>
    public void SetLevelExp(float currentExp, float requiredExp)
    {
        if (levelOrbFill == null) return;
        float ratio = requiredExp > 0f ? Mathf.Clamp01(currentExp / requiredExp) : 0f;
        levelOrbFill.fillAmount = ratio;
    }

    /// <summary>等级球的等级数字。</summary>
    public void SetLevelText(int level)
    {
        if (levelOrbText == null) return;
        levelOrbText.text = $"{level}";
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
        var left = healthFillLeft;
        var right = healthFillRight;
        if (left == null && right == null) return;
        if (!_healthBarColorSaved)
        {
            _healthBarDefaultColor = left != null ? left.color : right.color;
            _healthBarColorSaved = true;
        }
        if (left != null) left.color = color;
        if (right != null) right.color = color;
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
        if (shieldFillSprite == null) return;
        var left = healthFillLeft;
        var right = healthFillRight;
        if (left == null && right == null) return;
        if (!_defaultFillSpriteCached)
        {
            _defaultFillSprite = left != null ? left.sprite : right.sprite;
            _defaultFillSpriteCached = true;
        }
        var sprite = active ? shieldFillSprite : _defaultFillSprite;
        if (left != null) left.sprite = sprite;
        if (right != null) right.sprite = sprite;
    }

    #endregion

    #region 内部

    private Image GetHealthFillImage()
    {
        return healthFillLeft;
    }

    /// <summary>血量是否已经收到过真实数值（用于 HUD 初始化判定）。</summary>
    public bool HealthInitialized { get; private set; }

    private void SetImageSprite(Image image, Sprite sprite)
    {
        if (image != null && sprite != null)
            image.sprite = sprite;
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

    #endregion
}
