using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 蓄力指示器：跟随手指/鼠标移动，蓄力时 Radial360 填充，蓄满后叠加旋转环
/// 监听 InputManager 的蓄力事件和 PlayerState 的死亡事件
/// </summary>
public class ChargeIndicatorController : MonoBehaviour
{
    [Header("指示器根节点")]
    public RectTransform indicatorRoot;

    [Header("精灵图片1 — Radial360 填充")]
    public Image chargeFillImage;

    [Header("精灵图片2 — 蓄力完成后旋转")]
    public Image chargeSpinImage;

    [Header("出现阈值")]
    [Tooltip("蓄力进度达到此比例时指示器才出现 (0~1)")]
    [Range(0f, 1f)]
    public float appearThreshold = 0.3f;

    [Header("旋转速度 (度/秒)")]
    public float spinSpeed = 180f;

    private Canvas parentCanvas;
    private bool isActive;
    private bool isCharged;
    private bool hasAppeared;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (indicatorRoot != null)
            indicatorRoot.gameObject.SetActive(false);

        if (chargeFillImage != null)
        {
            chargeFillImage.type = Image.Type.Filled;
            chargeFillImage.fillMethod = Image.FillMethod.Radial360;
            chargeFillImage.fillOrigin = (int)Image.Origin360.Top;
            chargeFillImage.fillAmount = 0f;
        }

        if (chargeSpinImage != null)
            chargeSpinImage.gameObject.SetActive(false);

        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan += OnChargeBegan;
            InputManager.Instance.OnChargeUpdated += OnChargeUpdated;
            InputManager.Instance.OnChargeEnded += OnChargeEnded;
        }

        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied += OnChargeEnded;
    }

    private void Update()
    {
        if (isActive && isCharged && chargeSpinImage != null)
            chargeSpinImage.rectTransform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan -= OnChargeBegan;
            InputManager.Instance.OnChargeUpdated -= OnChargeUpdated;
            InputManager.Instance.OnChargeEnded -= OnChargeEnded;
        }
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnPlayerDied -= OnChargeEnded;
    }

    private void OnChargeBegan(Vector2 screenPos)
    {
        isActive = true;
        isCharged = false;
        hasAppeared = false;
        // 指示器先不显示，等进度 >= appearThreshold 再出现
    }

    private void OnChargeUpdated(Vector2 screenPos, float progress)
    {
        if (!isActive) return;

        if (progress >= appearThreshold)
        {
            if (!hasAppeared)
            {
                hasAppeared = true;
                if (indicatorRoot != null) indicatorRoot.gameObject.SetActive(true);
                if (chargeFillImage != null) chargeFillImage.fillAmount = 0f;
                if (chargeSpinImage != null) chargeSpinImage.gameObject.SetActive(false);
            }

            UpdatePosition(screenPos);

            // 将 progress (appearThreshold→1) 映射到 fillAmount (0→1)
            float fillAmount = Mathf.Clamp01((progress - appearThreshold) / (1f - appearThreshold));
            if (chargeFillImage != null)
                chargeFillImage.fillAmount = fillAmount;

            if (fillAmount >= 1f && !isCharged)
            {
                isCharged = true;
                if (chargeSpinImage != null) chargeSpinImage.gameObject.SetActive(true);
            }
        }
    }

    private void OnChargeEnded()
    {
        isActive = false;
        isCharged = false;
        hasAppeared = false;
        if (indicatorRoot != null) indicatorRoot.gameObject.SetActive(false);
        if (chargeSpinImage != null) chargeSpinImage.gameObject.SetActive(false);
    }

    private void UpdatePosition(Vector2 screenPos)
    {
        if (parentCanvas == null || indicatorRoot == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)parentCanvas.transform,
            screenPos,
            parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main,
            out Vector2 localPos);
        indicatorRoot.localPosition = localPos;
    }
}
