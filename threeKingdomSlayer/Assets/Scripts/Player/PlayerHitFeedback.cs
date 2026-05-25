using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 玩家受击反馈：全屏边框闪烁 + 镜头抖动
/// 挂载在 Player GameObject 上，监听 PlayerState.OnHealthChanged 检测受伤
/// </summary>
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("全屏边框")]
    [SerializeField] private Image hittedImage;
    [SerializeField] private float hittedDuration = 0.3f;
    [SerializeField] private float hittedFadeDuration = 0.1f;

    [Header("镜头抖动")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeIntensity = 0.3f;
    [SerializeField] private int shakeVibrato = 20;

    private float _lastHealth;
    private Tween _hittedTween;

    private void Start()
    {
        if (hittedImage != null)
        {
            hittedImage.raycastTarget = false;
            hittedImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (PlayerState.Instance != null)
        {
            _lastHealth = PlayerState.Instance.currentHealth;
            PlayerState.Instance.OnHealthChanged += OnHealthChanged;
        }
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnHealthChanged -= OnHealthChanged;

        _hittedTween?.Kill();
    }

    private void OnHealthChanged(float current, float max)
    {
        if (current < _lastHealth)
        {
            TriggerHitFeedback();
        }
        _lastHealth = current;
    }

    private void TriggerHitFeedback()
    {
        ShowHittedOverlay();
        ShakeCamera();
    }

    private void ShowHittedOverlay()
    {
        if (hittedImage == null) return;

        _hittedTween?.Kill();
        hittedImage.color = Color.white;
        _hittedTween = hittedImage.DOFade(0f, hittedFadeDuration)
            .SetDelay(hittedDuration)
            .SetEase(Ease.OutCubic);
    }

    private void ShakeCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        cam.transform.DOKill(true);
        cam.transform.DOShakePosition(shakeDuration, shakeIntensity, shakeVibrato);
    }
}
