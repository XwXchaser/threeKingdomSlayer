using UnityEngine;
using DG.Tweening;

public sealed class CameraFeedbackController : MonoBehaviour
{
    public static CameraFeedbackController Instance { get; private set; }

    [Header("镜头反馈")]
    [SerializeField] private float standardDuration = 0.07f;
    [SerializeField] private float standardIntensity = 0.012f;
    [SerializeField] private float heavyDuration = 0.11f;
    [SerializeField] private float heavyIntensity = 0.035f;
    [SerializeField] private float playerDamageDuration = 0.15f;
    [SerializeField] private float playerDamageIntensity = 0.07f;
    [SerializeField] private float requestCooldown = 0.04f;
    [SerializeField] private int standardVibrato = 10;
    [SerializeField] private int heavyVibrato = 14;
    [SerializeField] private RectTransform worldBackground;

    private Sequence _feedbackSequence;
    private float _lastRequestTime = -10f;
    private HitFeedbackStrength _activeStrength = HitFeedbackStrength.None;
    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _backgroundBaseLocalPosition;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
        CacheWorldBackground();
    }

    private void CacheWorldBackground()
    {
        if (worldBackground == null)
        {
            var canvasRoot = GameObject.Find("Canvas");
            var background = canvasRoot != null ? canvasRoot.transform.Find("background") : null;
            worldBackground = background as RectTransform;
        }

        if (worldBackground != null)
            _backgroundBaseLocalPosition = worldBackground.localPosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (_feedbackSequence != null && _feedbackSequence.IsActive())
            _feedbackSequence.Kill(false);
        _feedbackSequence = null;
        if (worldBackground != null)
            worldBackground.localPosition = _backgroundBaseLocalPosition;
    }

    public void RequestHit(HitFeedbackContext context)
    {
        if (context.strength < HitFeedbackStrength.Standard)
            return;
        if (context.source == HitFeedbackSource.Dot)
            return;
        if (Time.unscaledTime - _lastRequestTime < requestCooldown
            && context.strength <= _activeStrength)
            return;

        _lastRequestTime = Time.unscaledTime;
        bool heavy = context.strength == HitFeedbackStrength.Heavy;
        Vector3 direction = GetImpactDirection(context.worldPosition);
        PlayFeedback(heavy ? heavyDuration : standardDuration,
            heavy ? heavyIntensity : standardIntensity,
            heavy ? heavyVibrato : standardVibrato,
            direction);
        _activeStrength = context.strength;
    }

    public void RequestPlayerDamage()
    {
        _lastRequestTime = Time.unscaledTime;
        PlayFeedback(playerDamageDuration, playerDamageIntensity, heavyVibrato, Vector3.zero);
        _activeStrength = HitFeedbackStrength.Heavy;
    }

    private void LateUpdate()
    {
        if (worldBackground == null)
            return;

        Vector3 cameraDelta = transform.localPosition - _baseLocalPosition;
        worldBackground.localPosition = _backgroundBaseLocalPosition + new Vector3(
            cameraDelta.x * 135f,
            cameraDelta.y * 135f,
            0f);
    }

    private Vector3 GetImpactDirection(Vector3 worldPosition)
    {
        Vector3 viewport = Camera.main != null
            ? Camera.main.WorldToViewportPoint(worldPosition)
            : new Vector3(0.5f, 0.5f, 0f);
        float horizontal = Mathf.Clamp((viewport.x - 0.5f) * 2f, -1f, 1f);
        float vertical = Mathf.Clamp((viewport.y - 0.5f) * 2f, -1f, 1f);
        return new Vector3(-horizontal, -vertical, 0f);
    }

    private void PlayFeedback(float duration, float intensity, int vibrato, Vector3 direction)
    {
        _feedbackSequence?.Kill(false);
        transform.localPosition = _baseLocalPosition;
        transform.localRotation = _baseLocalRotation;

        Vector3 offset = direction.sqrMagnitude > 0.0001f
            ? direction.normalized * intensity
            : new Vector3(intensity * 0.35f, -intensity * 0.2f, 0f);
        _feedbackSequence = DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, true);
        _feedbackSequence.Append(transform.DOLocalMove(_baseLocalPosition + offset, duration * 0.35f).SetEase(Ease.OutQuad));
        _feedbackSequence.Append(transform.DOLocalMove(_baseLocalPosition, duration * 0.65f).SetEase(Ease.OutCubic));
        _feedbackSequence.Join(transform.DOShakeRotation(duration, new Vector3(0f, 0f, intensity * 80f), vibrato, 90f, false).SetEase(Ease.OutQuad));
        _feedbackSequence.OnComplete(() =>
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            if (worldBackground != null)
                worldBackground.localPosition = _backgroundBaseLocalPosition;
            _feedbackSequence = null;
            _activeStrength = HitFeedbackStrength.None;
        });
        _feedbackSequence.OnKill(() =>
        {
            transform.localPosition = _baseLocalPosition;
            transform.localRotation = _baseLocalRotation;
            if (worldBackground != null)
                worldBackground.localPosition = _backgroundBaseLocalPosition;
            _feedbackSequence = null;
            _activeStrength = HitFeedbackStrength.None;
        });
    }

    private void StopFeedback()
    {
        _feedbackSequence?.Kill(false);
        _feedbackSequence = null;
        _activeStrength = HitFeedbackStrength.None;
        transform.localPosition = _baseLocalPosition;
        transform.localRotation = _baseLocalRotation;
        if (worldBackground != null)
            worldBackground.localPosition = _backgroundBaseLocalPosition;
    }
}
