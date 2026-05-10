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

    private CanvasGroup canvasGroup;
    private Color iconOriginalColor;

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

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && ultimateButton != null)
            canvasGroup = ultimateButton.gameObject.AddComponent<CanvasGroup>();

        if (buttonIconImage != null)
            iconOriginalColor = buttonIconImage.color;

        // 订阅事件
        if (UltimateSystem.Instance != null)
        {
            UltimateSystem.Instance.OnEnergyChanged += OnEnergyChanged;
            UltimateSystem.Instance.OnUltimateReady += OnReady;
            UltimateSystem.Instance.OnUltimateActivated += OnActivated;
        }

        // 初始状态
        OnEnergyChanged(0f);

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
        }
    }

    private void OnDestroy()
    {
        if (UltimateSystem.Instance != null)
        {
            UltimateSystem.Instance.OnEnergyChanged -= OnEnergyChanged;
            UltimateSystem.Instance.OnUltimateReady -= OnReady;
            UltimateSystem.Instance.OnUltimateActivated -= OnActivated;
        }

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

        // 未充满时半透明
        bool ready = UltimateSystem.Instance != null && UltimateSystem.Instance.IsReady;
        UpdateVisualState(ready);
    }

    private void OnReady()
    {
        UpdateVisualState(true);
    }

    private void OnActivated()
    {
        UpdateVisualState(false);
    }

    private void UpdateVisualState(bool ready)
    {
        float targetAlpha = ready ? 1f : inactiveAlpha;

        if (canvasGroup != null)
            canvasGroup.alpha = targetAlpha;

        if (buttonIconImage != null)
        {
            buttonIconImage.color = ready ? readyColor : iconOriginalColor * inactiveAlpha;
        }

        if (ultimateButton != null)
            ultimateButton.interactable = ready;
    }

    private void OnButtonClick()
    {
        UltimateSystem.Instance?.ActivateUltimate();
    }
}
