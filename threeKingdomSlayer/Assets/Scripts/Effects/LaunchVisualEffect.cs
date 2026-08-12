using System;
using DG.Tweening;
using UnityEngine;

public sealed class LaunchVisualEffect : MonoBehaviour
{
    public const float ObservationScale = 1.5f;

    private const float ImpactThrustRatio = 0.48f;
    private const float RecoveryHoldDuration = 0.025f;
    private const float RecoveryRetractDuration = 0.085f;
    private const float RecoveryPathEndRatio = 0.35f;

    public static float GetObservationDuration(AttackSkillConfig config)
    {
        if (config == null)
            return 0f;
        return (Mathf.Max(config.launchFlickDuration, 0.1f)
            + RecoveryHoldDuration + RecoveryRetractDuration) * ObservationScale;
    }

    private Sequence _sequence;
    private WeaponMotionBlurController _motionBlur;
    private Action _onImpact;
    private bool _impactInvoked;
    private bool _completed;

    public static void Create(Sprite launchSprite1, Sprite launchSprite2, Sprite launchSprite3,
        AttackSkillConfig config, Vector3 playerPosition, ChargeStabVisual chargeVisual,
        float durationScale, Action onImpact)
    {
        if (launchSprite1 == null)
        {
            onImpact?.Invoke();
            return;
        }

        var root = new GameObject("Launch_Visual");
        root.AddComponent<LaunchVisualEffect>().Initialize(launchSprite1, launchSprite2, launchSprite3,
            config, playerPosition, chargeVisual, Mathf.Max(durationScale, 0.01f), onImpact);
    }

    private void Initialize(Sprite launchSprite1, Sprite launchSprite2, Sprite launchSprite3,
        AttackSkillConfig config, Vector3 playerPosition, ChargeStabVisual chargeVisual,
        float durationScale, Action onImpact)
    {
        _onImpact = onImpact;

        float variance = Mathf.Clamp(config.launchAngleVariance, 0f, 30f);
        float zStart = 140f + UnityEngine.Random.Range(-variance, variance);
        float motionDuration = Mathf.Max(config.launchFlickDuration, 0.1f) * durationScale;

        Vector3 spawnPosition = new Vector3(
            playerPosition.x + config.launchSpawnXOffset,
            playerPosition.y + config.launchSpawnYOffset,
            playerPosition.z + config.launchSpawnZOffset);
        Quaternion startRotation = Quaternion.Euler(35f, 90f, zStart);

        Vector3 chargePosition = default;
        Quaternion chargeRotation = default;
        Vector3 chargeScale = default;
        bool useChargePose = chargeVisual != null
            && chargeVisual.TryGetCurrentVisualPose(out chargePosition, out chargeRotation, out chargeScale);

        Vector3 targetScale;
        if (useChargePose)
        {
            spawnPosition = chargePosition;
            startRotation = chargeRotation;
            targetScale = chargeScale;
            chargeVisual.SuppressFadeAndDestroy();
        }
        else
        {
            float basePixelsPerUnit = launchSprite1.pixelsPerUnit;
            float basePixelSize = Mathf.Max(launchSprite1.rect.width, launchSprite1.rect.height);
            float baseWorldSize = basePixelSize / basePixelsPerUnit;
            float scale = baseWorldSize > 0.001f ? 5f / baseWorldSize : 1f;
            targetScale = Vector3.one * scale;
        }

        float sideRatio = 0f;
        if (useChargePose)
        {
            float horizontalRange = chargeVisual != null ? chargeVisual.halfWidth : 3f;
            if (horizontalRange > 0.001f)
                sideRatio = Mathf.Clamp((chargePosition.x - playerPosition.x) / horizontalRange, -1f, 1f);
        }
        float sideTilt = sideRatio * config.launchSideTilt;
        float randomTiltMagnitude = UnityEngine.Random.Range(variance * 0.55f, variance);
        float randomTilt = randomTiltMagnitude * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
        float poseTilt = sideTilt + randomTilt;
        float flickAngle = Mathf.Clamp(config.launchFlickAngle, 45f, 70f);

        Camera mainCamera = Camera.main;
        Vector3 cameraUp = mainCamera != null ? mainCamera.transform.up : Vector3.up;
        Vector3 cameraForward = mainCamera != null ? mainCamera.transform.forward : Vector3.forward;
        Vector3 cameraDown = -cameraUp;
        Vector3 cameraRight = mainCamera != null ? mainCamera.transform.right : Vector3.right;

        float spriteHeight = launchSprite1.rect.height / launchSprite1.pixelsPerUnit;
        float halfLength = spriteHeight * targetScale.y * 0.5f;
        Vector3 gunUp = startRotation * Vector3.up;
        Vector3 gunTail = spawnPosition - gunUp * halfLength;
        float pivotFromTail = halfLength * 0.40f;
        Vector3 pivotPosition = gunTail + gunUp * pivotFromTail;
        float pivotArmLength = halfLength - pivotFromTail;

        Quaternion windupRotation = startRotation * Quaternion.Euler(32f, 0f,
            sideTilt * 0.45f + randomTilt * 0.12f);
        Quaternion apexRotation = startRotation * Quaternion.Euler(-flickAngle, 0f, poseTilt);

        float riseDistance = Mathf.Clamp(config.launchRiseHeight * 0.49f, 0.40f, 0.56f);
        float sideOffset = sideRatio * 0.12f;
        Vector3 windupBack = -cameraRight * Mathf.Sign(Mathf.Abs(sideRatio) > 0.01f ? sideRatio : 1f) * 0.10f;
        Vector3 windupPosition = pivotPosition + cameraDown * config.launchWindupDistance
            + windupBack - cameraRight * sideOffset;
        Vector3 apexPosition = pivotPosition + cameraUp * riseDistance + cameraForward * 0.18f
            + cameraRight * (sideRatio * 0.18f);
        Vector3 impactPosition = Vector3.Lerp(windupPosition, apexPosition, ImpactThrustRatio);
        Vector3 preImpactControl = Vector3.Lerp(windupPosition, impactPosition, 0.42f)
            + cameraDown * 0.10f
            + cameraForward * 0.16f
            + cameraRight * (sideRatio * 0.14f);
        Vector3 postImpactControl = impactPosition
            + (impactPosition - preImpactControl) * 0.45f;
        Quaternion impactRotation = Quaternion.Slerp(windupRotation, apexRotation, 0.56f);

        transform.position = pivotPosition;
        transform.rotation = Quaternion.identity;

        var pivot = new GameObject("Launch_Pivot").transform;
        pivot.SetParent(transform, false);
        pivot.localRotation = startRotation;

        var weapon = new GameObject("Launch_Weapon").transform;
        weapon.SetParent(pivot, false);
        weapon.localPosition = Vector3.up * pivotArmLength;
        weapon.localScale = targetScale;

        var renderer = weapon.gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = launchSprite1;
        renderer.color = Color.white;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 2;
        _motionBlur = new WeaponMotionBlurController(renderer, 1.2f, 0.09f, 56f);

        float windupDuration = Mathf.Clamp(config.launchWindupDuration, 0.06f,
            config.launchFlickDuration * 0.56f) * durationScale;
        float windupPause = Mathf.Min(0.02f, config.launchFlickDuration * 0.08f) * durationScale;
        float thrustDuration = Mathf.Max(motionDuration - windupDuration - windupPause, 0.01f);
        float recoveryHoldDuration = RecoveryHoldDuration * durationScale;
        float retractDuration = RecoveryRetractDuration * durationScale;
        float thrustStart = windupDuration + windupPause;

        _sequence = DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, false);
        float windupProgress = 0f;
        Tween windupTween = DOTween.To(
            () => windupProgress,
            value =>
            {
                windupProgress = value;
                transform.position = Vector3.LerpUnclamped(pivotPosition, windupPosition, value);
                pivot.localRotation = Quaternion.SlerpUnclamped(startRotation, windupRotation, value);
            },
            1f,
            windupDuration).SetEase(Ease.OutQuad);
        _sequence.Append(windupTween);
        _sequence.AppendInterval(windupPause);

        float thrustProgress = 0f;
        Tween thrustTween = DOTween.To(
            () => thrustProgress,
            value =>
            {
                thrustProgress = value;
                transform.position = EvaluateLaunchPath(value, windupPosition, preImpactControl,
                    impactPosition, postImpactControl, apexPosition);
                pivot.localRotation = EvaluateLaunchRotation(value, windupRotation,
                    impactRotation, apexRotation);
                _motionBlur?.UpdateMotionWorld(weapon.position, weapon.rotation,
                    cameraUp, 1.35f, 20f, Time.deltaTime);
            },
            1f,
            thrustDuration).SetEase(Ease.Linear);
        thrustTween.OnStart(() => _motionBlur?.SetStrength(36f));
        _sequence.Append(thrustTween);

        _sequence.InsertCallback(thrustStart, () =>
        {
            if (launchSprite2 != null)
                renderer.sprite = launchSprite2;
            _motionBlur?.SetStrength(38f);
        });
        _sequence.InsertCallback(thrustStart + thrustDuration * 0.55f, () =>
        {
            if (launchSprite3 != null)
                renderer.sprite = launchSprite3;
            _motionBlur?.SetStrength(30f);
        });
        _sequence.InsertCallback(thrustStart + thrustDuration * ImpactThrustRatio, InvokeImpact);

        // 上挑完成后仅保持姿态，不再继续上移或增加上挑角度
        _sequence.AppendInterval(recoveryHoldDuration);
        _sequence.AppendCallback(() => _motionBlur?.SetStrength(4f));

        // 收招：反向采样同一条上挑弧线，旋转略快于位移以表现主动回手
        float recoveryProgress = 0f;
        Tween recoveryTween = DOTween.To(
            () => recoveryProgress,
            value =>
            {
                recoveryProgress = value;
                float pathProgress = Mathf.Lerp(1f, RecoveryPathEndRatio, value);
                float rotationProgress = Mathf.Lerp(1f, RecoveryPathEndRatio,
                    Mathf.Clamp01(value * 1.18f));
                transform.position = EvaluateLaunchPath(pathProgress, windupPosition, preImpactControl,
                    impactPosition, postImpactControl, apexPosition);
                pivot.localRotation = EvaluateLaunchRotation(rotationProgress, windupRotation,
                    impactRotation, apexRotation);
                _motionBlur?.UpdateMotionWorld(weapon.position, weapon.rotation,
                    cameraDown, 0.45f, 6f, Time.deltaTime);
            },
            1f,
            retractDuration).SetEase(Ease.OutCubic);
        _sequence.Append(recoveryTween);
        _sequence.Join(DOTween.Sequence()
            .AppendInterval(retractDuration * 0.35f)
            .Append(renderer.DOFade(0f, retractDuration * 0.65f).SetEase(Ease.InQuad)));
        _sequence.Join(DOTween.To(() => 4f, value => _motionBlur?.SetStrength(value), 0f, retractDuration)
            .SetEase(Ease.InQuad));

        _sequence.OnKill(() =>
        {
            if (!_completed)
                Destroy(gameObject);
        });
        _sequence.OnComplete(() =>
        {
            _completed = true;
            Destroy(gameObject);
        });
    }

    private static Vector3 EvaluateLaunchPath(float progress, Vector3 start,
        Vector3 preImpactControl, Vector3 impact, Vector3 postImpactControl, Vector3 apex)
    {
        progress = Mathf.Clamp01(progress);
        if (progress <= ImpactThrustRatio)
        {
            float localProgress = progress / ImpactThrustRatio;
            float easedProgress = localProgress * localProgress;
            return EvaluateQuadratic(start, preImpactControl, impact, easedProgress);
        }

        float postProgress = (progress - ImpactThrustRatio) / (1f - ImpactThrustRatio);
        float easedPostProgress = 1f - Mathf.Pow(1f - postProgress, 2f);
        return EvaluateQuadratic(impact, postImpactControl, apex, easedPostProgress);
    }

    private static Quaternion EvaluateLaunchRotation(float progress, Quaternion start,
        Quaternion impact, Quaternion apex)
    {
        progress = Mathf.Clamp01(progress);
        const float rotationImpactRatio = 0.56f;
        if (progress <= ImpactThrustRatio)
        {
            float localProgress = progress / ImpactThrustRatio;
            float delayedProgress = Mathf.Clamp01((localProgress - 0.14f) / 0.86f);
            delayedProgress = delayedProgress * delayedProgress * (3f - 2f * delayedProgress);
            return Quaternion.SlerpUnclamped(start, impact, delayedProgress);
        }

        float postProgress = (progress - ImpactThrustRatio) / (1f - ImpactThrustRatio);
        float easedPostProgress = 1f - Mathf.Pow(1f - postProgress, 2f);
        Quaternion impactPose = Quaternion.SlerpUnclamped(start, apex, rotationImpactRatio);
        return Quaternion.SlerpUnclamped(impactPose, apex, easedPostProgress);
    }

    private static Vector3 EvaluateQuadratic(Vector3 start, Vector3 control, Vector3 end, float progress)
    {
        float inverse = 1f - progress;
        return inverse * inverse * start + 2f * inverse * progress * control + progress * progress * end;
    }

    private void InvokeImpact()
    {
        if (_impactInvoked)
            return;
        _impactInvoked = true;
        _onImpact?.Invoke();
    }

    private void OnDestroy()
    {
        _sequence?.Kill(false);
        _sequence = null;
        _motionBlur?.Dispose();
        _motionBlur = null;
        _onImpact = null;
    }
}
