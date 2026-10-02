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

    /// <summary>
    /// 刺出/回收期间额外绕「屏幕平面法线」的旋转：参考 Parry 的大幅扫转（220°），
    /// 但支点在枪尾（见 launchPivotFromTailRatio）—— 两者组合把动作读成「上挑」。
    /// 幅度 0 = 与改造前完全一致。
    /// </summary>
    private Quaternion SweepRollRotation(float progress)
    {
        float degrees = _sweepRollDegrees * Mathf.Clamp01(progress);
        if (Mathf.Approximately(degrees, 0f)) return Quaternion.identity;
        return Quaternion.Euler(0f, 0f, degrees);
    }

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
    private bool _sweepMode;
    /// <summary>刺出/回收期间额外绕屏幕平面法线的旋转幅度（度），来自招式资产（0 = 与改造前一致）</summary>
    private float _sweepRollDegrees;

    public static void Create(Sprite launchSprite1, Sprite launchSprite2, Sprite launchSprite3,
        AttackSkillConfig config, Vector3 playerPosition, ChargeStabVisual chargeVisual,
        float durationScale, Action onImpact,
        bool hasExplicitPose = false, Vector3 explicitPosition = default, Quaternion explicitRotation = default,
        Vector3 explicitScale = default, bool skipWindup = false)
    {
        if (launchSprite1 == null)
        {
            onImpact?.Invoke();
            return;
        }

        var root = new GameObject("Launch_Visual");
        root.AddComponent<LaunchVisualEffect>().Initialize(launchSprite1, launchSprite2, launchSprite3,
            config, playerPosition, chargeVisual, Mathf.Max(durationScale, 0.01f), onImpact,
            hasExplicitPose, explicitPosition, explicitRotation, explicitScale, skipWindup);
    }

    private void Initialize(Sprite launchSprite1, Sprite launchSprite2, Sprite launchSprite3,
        AttackSkillConfig config, Vector3 playerPosition, ChargeStabVisual chargeVisual,
        float durationScale, Action onImpact,
        bool hasExplicitPose, Vector3 explicitPosition, Quaternion explicitRotation,
        Vector3 explicitScale, bool skipWindup)
    {
        _onImpact = onImpact;
        _sweepMode = config.launchSweepMode;
        _sweepRollDegrees = config.launchSweepRollDegrees;

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
        bool hasStartPose = false;
        if (hasExplicitPose)
        {
            // 连段终结技：从上一段保持中的枪体姿态原地起手（位置=枪体中心、缩放=同一世界缩放，不位移）
            spawnPosition = explicitPosition;
            startRotation = explicitRotation;
            targetScale = explicitScale != Vector3.zero ? explicitScale : ComputeDefaultScale(launchSprite1);
            hasStartPose = true;
        }
        else if (useChargePose)
        {
            spawnPosition = chargePosition;
            startRotation = chargeRotation;
            targetScale = chargeScale;
            chargeVisual.SuppressFadeAndDestroy();
            hasStartPose = true;
        }
        else
        {
            targetScale = ComputeDefaultScale(launchSprite1);
        }

        float sideRatio = 0f;
        if (hasStartPose)
        {
            float horizontalRange = chargeVisual != null ? chargeVisual.halfWidth : 3f;
            if (horizontalRange > 0.001f)
                sideRatio = Mathf.Clamp((spawnPosition.x - playerPosition.x) / horizontalRange, -1f, 1f);
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
        // 支点距枪尾的比例：0 = 正好在枪尾（上挑读感最强，用户要求）；旧值 0.4 ≈ 距枪尾 20% 枪长
        float pivotFromTail = halfLength * Mathf.Clamp(config.launchPivotFromTailRatio, 0f, 1f);
        Vector3 pivotPosition = gunTail + gunUp * pivotFromTail;
        float pivotArmLength = halfLength - pivotFromTail;

        Quaternion windupRotation = skipWindup
            ? startRotation
            : startRotation * Quaternion.Euler(32f, 0f, sideTilt * 0.45f + randomTilt * 0.12f);
        Quaternion apexRotation = startRotation * Quaternion.Euler(-flickAngle, 0f, poseTilt);
        // 左上扫击表现：终点枪尖方向直接在相机平面里给出（不受挑飞角度钳制影响）
        if (config.launchSweepMode)
        {
            Vector3 sweepAxis = cameraUp * config.launchSweepUp
                + cameraRight * (-config.launchSweepLeft)
                + cameraForward * config.launchSweepForward;
            if (sweepAxis.sqrMagnitude > 0.0001f)
                apexRotation = Quaternion.LookRotation(sweepAxis.normalized, cameraUp) * Quaternion.Euler(90f, 0f, 0f);
        }

        float riseDistance = Mathf.Clamp(config.launchRiseHeight * 0.49f, 0.40f, 0.56f);
        float sideOffset = sideRatio * 0.12f;
        Vector3 windupBack = -cameraRight * Mathf.Sign(Mathf.Abs(sideRatio) > 0.01f ? sideRatio : 1f) * 0.10f;
        Vector3 windupPosition = pivotPosition + cameraDown * config.launchWindupDistance
            + windupBack - cameraRight * sideOffset;
        // 连段终结技：预备已由蓄力拉回完成，直接从蓄势位起上挑
        if (skipWindup) windupPosition = pivotPosition;
        Vector3 apexPosition = pivotPosition + cameraUp * riseDistance + cameraForward * 0.18f
            + cameraRight * (sideRatio * 0.18f);
        if (config.launchSweepMode)
            apexPosition = pivotPosition + cameraUp * config.launchSweepRise
                + cameraRight * (-config.launchSweepLeftShift);
        // 只在「挑出」过程中前移：起手位（= 蓄势姿）保持不动，挑出终点沿镜头前方进入画面纵深，避免枪体贴着镜头
        if (config.launchForwardShift > 0f)
            apexPosition += cameraForward * config.launchForwardShift;
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

        float windupDuration = skipWindup ? 0f : Mathf.Clamp(config.launchWindupDuration, 0.06f,
            config.launchFlickDuration * 0.56f) * durationScale;
        float windupPause = skipWindup ? 0f : Mathf.Min(0.02f, config.launchFlickDuration * 0.08f) * durationScale;
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
                pivot.localRotation = EvaluateRotation(value, windupRotation,
                    impactRotation, apexRotation) * SweepRollRotation(value);
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
                pivot.localRotation = EvaluateRotation(rotationProgress, windupRotation,
                    impactRotation, apexRotation) * SweepRollRotation(rotationProgress);
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

    private static Vector3 ComputeDefaultScale(Sprite sprite)
    {
        if (sprite == null) return Vector3.one;
        float basePixelsPerUnit = sprite.pixelsPerUnit;
        float basePixelSize = Mathf.Max(sprite.rect.width, sprite.rect.height);
        float baseWorldSize = basePixelSize / basePixelsPerUnit;
        float scale = baseWorldSize > 0.001f ? 5f / baseWorldSize : 1f;
        return Vector3.one * scale;
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

    /// <summary>
    /// 旋转曲线：挑飞用「位移先走、旋转短暂滞后后追上」的错峰；扫击模式改用与位移同一条曲线，一甩到底。
    /// </summary>
    private Quaternion EvaluateRotation(float progress, Quaternion start, Quaternion impact, Quaternion apex)
    {
        if (!_sweepMode)
            return EvaluateLaunchRotation(progress, start, impact, apex);

        float p = Mathf.Clamp01(progress);
        if (p <= ImpactThrustRatio)
        {
            float localProgress = p / ImpactThrustRatio;
            return Quaternion.SlerpUnclamped(start, impact, localProgress * localProgress);
        }

        float postProgress = (p - ImpactThrustRatio) / (1f - ImpactThrustRatio);
        float easedPostProgress = 1f - Mathf.Pow(1f - postProgress, 2f);
        return Quaternion.SlerpUnclamped(impact, apex, easedPostProgress);
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
