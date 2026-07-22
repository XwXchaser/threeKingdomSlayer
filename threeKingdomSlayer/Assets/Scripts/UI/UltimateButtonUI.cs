using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 大招按钮UI控制器
/// 未充满时半透明显示，垂直方向从底部向上填充表现充能进度。
/// 充能满时高亮且可交互，点击触发大招。
/// </summary>
public class UltimateButtonUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("大招按钮（Button 组件）")]
    public Button ultimateButton;
    [Tooltip("充能填充 Image（Image.fillMethod = Vertical，从底部向上）")]
    public Image fillImage;
    [Tooltip("按钮图标/背景 Image（用于控制透明度）")]
    public Image buttonIconImage;
    [Tooltip("充能数值文本（当前/上限）")]
    public TMP_Text energyText;

    [Header("视觉配置")]
    [Tooltip("未充满时的透明度")]
    [Range(0f, 1f)] public float inactiveAlpha = 0.4f;
    [Tooltip("充满时的高亮颜色叠加")]
    public Color readyColor = Color.white;
    [Tooltip("未充满时是否整体变暗。头像合并大招按钮时关闭，保持头像和底图常驻可见")]
    public bool dimWhenInactive = true;

    [Header("充满特效")]
    public GameObject readyEffectRoot;
    public Image readyEffectImage;
    public UIReadyFireEffect readyEffectEmitter;
    public Sprite readyFireStartSprite;
    public Sprite[] readyFireLoopSprites;
    public float readyFireFps = 10f;

    private CanvasGroup canvasGroup;
    private CanvasGroup readyEffectCanvasGroup;
    private UltimateSystem subscribedUltimateSystem;
    private Color iconOriginalColor;
    private UIReadyVerticalPulse fillReadyPulse;
    private bool _isReady;

    private void OnValidate()
    {
        ConfigureReadyEffectImage();
    }

    private void OnEnable()
    {
        TryBindUltimateSystem();
    }

    private void Start()
    {
        if (ultimateButton == null)
            ultimateButton = GetComponent<Button>();
        if (fillImage == null)
        {
            var fillTrans = transform.Find("Fill");
            if (fillTrans != null)
                fillImage = fillTrans.GetComponent<Image>();
        }
        if (buttonIconImage == null)
            buttonIconImage = GetComponent<Image>();
        if (energyText == null)
        {
            var textTrans = transform.Find("EnergyText");
            if (textTrans != null)
                energyText = textTrans.GetComponent<TMP_Text>();
        }
        if (readyEffectRoot == null)
        {
            var effectTrans = transform.Find("ReadyFireEffect");
            if (effectTrans != null)
                readyEffectRoot = effectTrans.gameObject;
        }
        if (readyEffectImage == null && readyEffectRoot != null)
            readyEffectImage = readyEffectRoot.GetComponent<Image>();
        if (readyEffectEmitter == null && readyEffectRoot != null)
            readyEffectEmitter = readyEffectRoot.GetComponent<UIReadyFireEffect>();
        ConfigureReadyEffectImage();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && ultimateButton != null)
            canvasGroup = ultimateButton.gameObject.AddComponent<CanvasGroup>();

        if (buttonIconImage != null)
            iconOriginalColor = buttonIconImage.color;

        TryBindUltimateSystem();
        SyncFromUltimateSystem();

        if (ultimateButton != null)
        {
            ultimateButton.onClick.AddListener(OnButtonClick);
            ultimateButton.interactable = false;
        }

        // 初始设置 fillImage 为 Vertical fill
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Vertical;
            fillImage.fillOrigin = 0; // Bottom
            fillImage.fillAmount = 0f;
            fillReadyPulse = fillImage.GetComponent<UIReadyVerticalPulse>();
        }

        SyncFromUltimateSystem();
    }

    private void Update()
    {
        TryBindUltimateSystem();
    }

    public void ApplyReadyEffectSkin(Sprite startSprite, Sprite[] loopSprites, float fps)
    {
        readyFireStartSprite = startSprite;
        readyFireLoopSprites = loopSprites;
        readyFireFps = fps > 0f ? fps : readyFireFps;

        if (readyEffectImage != null)
            readyEffectImage.sprite = readyFireStartSprite != null ? readyFireStartSprite : GetFirstLoopSprite();
        readyEffectEmitter?.ApplySprites(readyFireStartSprite, readyFireLoopSprites, readyFireFps);
    }

    private void OnDisable()
    {
        UnbindUltimateSystem();
    }

    private void OnDestroy()
    {
        UnbindUltimateSystem();

        if (ultimateButton != null)
        {
            ultimateButton.onClick.RemoveListener(OnButtonClick);
        }
    }

    private void OnEnergyChanged(float percent)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = percent;
        }

        if (energyText != null && UltimateSystem.Instance != null)
        {
            energyText.text = $"{UltimateSystem.Instance.CurrentEnergy}/{UltimateSystem.Instance.maxUltimateEnergy}";
        }

        bool ready = UltimateSystem.Instance != null && UltimateSystem.Instance.IsReady;
        UpdateVisualState(ready);
        if (ready)
        {
            ShowReadyEffect();
        }
        else
        {
            HideReadyEffect();
            fillReadyPulse?.SetPlaying(false);
        }
    }

    private void OnReady()
    {
        _isReady = true;
        fillReadyPulse?.SetPlaying(true);
        UpdateVisualState(true);
        ShowReadyEffect();
    }

    private void OnActivated()
    {
        _isReady = false;
        fillReadyPulse?.SetPlaying(false);
        UpdateVisualState(false);
        HideReadyEffect();
    }

    private void UpdateVisualState(bool ready)
    {
        if (dimWhenInactive)
        {
            float targetAlpha = ready ? 1f : inactiveAlpha;

            if (canvasGroup != null)
                canvasGroup.alpha = targetAlpha;

            if (buttonIconImage != null)
                buttonIconImage.color = ready ? readyColor : iconOriginalColor * targetAlpha;
        }

        if (ultimateButton != null)
            ultimateButton.interactable = ready;
    }

    private void TryBindUltimateSystem()
    {
        if (subscribedUltimateSystem != null || UltimateSystem.Instance == null)
            return;

        subscribedUltimateSystem = UltimateSystem.Instance;
        subscribedUltimateSystem.OnEnergyChanged += OnEnergyChanged;
        subscribedUltimateSystem.OnUltimateReady += OnReady;
        subscribedUltimateSystem.OnUltimateActivated += OnActivated;
        SyncFromUltimateSystem();
    }

    private void UnbindUltimateSystem()
    {
        if (subscribedUltimateSystem == null)
            return;

        subscribedUltimateSystem.OnEnergyChanged -= OnEnergyChanged;
        subscribedUltimateSystem.OnUltimateReady -= OnReady;
        subscribedUltimateSystem.OnUltimateActivated -= OnActivated;
        subscribedUltimateSystem = null;
    }

    private void SyncFromUltimateSystem()
    {
        if (UltimateSystem.Instance != null)
            OnEnergyChanged(UltimateSystem.Instance.EnergyPercent);
        else
            OnEnergyChanged(0f);
    }

    private void ConfigureReadyEffectImage()
    {
        ConfigureReadyEffectTarget(readyEffectRoot, readyEffectImage, ref readyEffectCanvasGroup, ref readyEffectEmitter);
    }

    private void ConfigureReadyEffectTarget(GameObject root, Image image, ref CanvasGroup targetCanvasGroup, ref UIReadyFireEffect emitter)
    {
        if (root != null)
        {
            root.SetActive(true);
            targetCanvasGroup = root.GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null)
                targetCanvasGroup = root.AddComponent<CanvasGroup>();
            if (emitter == null)
                emitter = root.GetComponent<UIReadyFireEffect>();
            if (emitter == null)
                emitter = root.AddComponent<UIReadyFireEffect>();
            emitter.ApplySprites(readyFireStartSprite, readyFireLoopSprites, readyFireFps);
            emitter.SetVisible(false);
        }

        if (image == null)
            return;

        image.enabled = true;
        image.raycastTarget = false;
        image.maskable = false;
        if (image.sprite == null)
            image.sprite = readyFireStartSprite != null ? readyFireStartSprite : GetFirstLoopSprite();
    }

    private void HideReadyEffect()
    {
        readyEffectEmitter?.Stop(true);
        if (readyEffectCanvasGroup != null)
            readyEffectCanvasGroup.alpha = 0f;
    }

    private void ShowReadyEffect()
    {
        if (readyEffectRoot == null || readyEffectImage == null)
            return;

        ConfigureReadyEffectImage();
        readyEffectEmitter?.Play();
        if (readyEffectCanvasGroup != null)
            readyEffectCanvasGroup.alpha = 1f;
        if (readyEffectImage != null)
            readyEffectImage.sprite = readyFireStartSprite != null ? readyFireStartSprite : GetFirstLoopSprite();
    }

    private Sprite GetFirstLoopSprite()
    {
        return readyFireLoopSprites != null && readyFireLoopSprites.Length > 0 ? readyFireLoopSprites[0] : null;
    }

    private void OnButtonClick()
    {
        if (Time.timeScale == 0f || (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractionBlocked)) return;
        UltimateSystem.Instance?.ActivateUltimate();
    }
}
