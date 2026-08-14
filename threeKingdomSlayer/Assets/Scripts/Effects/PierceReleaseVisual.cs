using System;
using DG.Tweening;
using UnityEngine;

public sealed class PierceReleaseVisual : MonoBehaviour
{
    private Sequence _sequence;
    private Action<PierceReleaseVisual> _onRelease;
    private PierceAxialSpinVisual _spinVisual;
    private bool _released;
    private bool _transferred;

    public Transform ProjectileTransform => transform;
    public Vector3 FlightStartPosition { get; private set; }

    public static void Create(Sprite sprite, ChargeStabVisual chargeVisual, Vector3 fallbackPosition,
        Quaternion fallbackRotation, Vector3 fallbackScale, Vector3 projectileStartPosition,
        Quaternion projectileRotation, Vector3 projectileScale, float duration,
        Action<PierceReleaseVisual> onRelease)
    {
        if (onRelease == null)
            return;
        if (sprite == null)
        {
            onRelease(null);
            return;
        }

        var root = new GameObject("Pierce_ReleaseVisual");
        root.AddComponent<PierceReleaseVisual>().Initialize(sprite, chargeVisual, fallbackPosition,
            fallbackRotation, fallbackScale, projectileStartPosition, projectileRotation,
            projectileScale, Mathf.Max(duration, 0.01f), onRelease);
    }

    private void Initialize(Sprite sprite, ChargeStabVisual chargeVisual, Vector3 fallbackPosition,
        Quaternion fallbackRotation, Vector3 fallbackScale, Vector3 projectileStartPosition,
        Quaternion projectileRotation, Vector3 projectileScale, float duration,
        Action<PierceReleaseVisual> onRelease)
    {
        _onRelease = onRelease;

        Vector3 startPosition = fallbackPosition;
        Quaternion startRotation = fallbackRotation;
        Vector3 startScale = fallbackScale;
        if (chargeVisual != null
            && chargeVisual.TryGetCurrentVisualPose(out Vector3 chargePosition,
                out Quaternion chargeRotation, out Vector3 chargeScale))
        {
            startPosition = chargePosition;
            startRotation = chargeRotation;
            startScale = chargeScale;
            chargeVisual.SuppressFadeAndDestroy();
        }

        transform.SetPositionAndRotation(startPosition, startRotation);
        transform.localScale = startScale;

        var renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = Color.white;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 2;
        _spinVisual = PierceAxialSpinVisual.Create(transform, renderer);

        // 射出方向（固定飞行轴）。蓄力→弹射轨迹严格沿此轴直线。
        Vector3 flightAxis = (projectileRotation * Vector3.up).normalized;

        float orientDuration = duration * 0.20f;
        float pullDuration = duration * 0.50f;
        float holdDuration = duration * 0.05f;
        float releaseDuration = duration * 0.12f;

        float pullBackDistance = 3.0f;
        // 飞行起点：x 对齐目标列，y/z 保持蓄力位置（不往上跳、不前移）。
        Vector3 flightStart = new Vector3(projectileStartPosition.x, startPosition.y, startPosition.z);
        FlightStartPosition = flightStart;
        // 摆正目标 = 飞行起点。
        Vector3 orientPosition = flightStart;
        // 后收点：沿飞行轴反向抽枪。
        Vector3 pullbackPosition = orientPosition - flightAxis * pullBackDistance;
        // 拉弓压缩：明显压短蓄势（长度 0.70）。
        Vector3 compressionScale = Vector3.Scale(startScale, new Vector3(0.96f, 0.70f, 0.96f));

        Debug.Log($"[PierceTrace] start={startPosition} orient={orientPosition} pullback={pullbackPosition} flightStart={flightStart} axis={flightAxis} pullDistance={pullBackDistance:F2} duration={duration:F2}");

        _spinVisual?.SetSpinProfile(orientDuration + pullDuration, 30f, 220f);

        _sequence = DOTween.Sequence().SetTarget(transform).SetUpdate(UpdateType.Normal, false);

        // 1. 摆正：x/y 对齐飞行线 + 旋转到飞行轴（z 不前移）。
        _sequence.Append(transform.DOMove(orientPosition, orientDuration).SetEase(Ease.OutQuad));
        _sequence.Join(transform.DORotateQuaternion(projectileRotation, orientDuration).SetEase(Ease.OutQuad));

        // 2. 拉弓后收：沿飞行轴反向直线抽枪 + 明显压缩蓄势。
        _sequence.Append(transform.DOMove(pullbackPosition, pullDuration).SetEase(Ease.InOutSine));
        _sequence.Join(transform.DOScale(compressionScale, pullDuration).SetEase(Ease.InQuad));

        // 3. 满弓停顿：制造蓄势张力。
        _sequence.AppendInterval(holdDuration);

        // 4. 射出：松手瞬间爆发冲出枪口，拉伸回弹，衔接飞行段衰减。
        _sequence.AppendCallback(() => _spinVisual?.SetSpinProfile(releaseDuration, 280f, 760f));
        _sequence.Append(transform.DOMove(flightStart, releaseDuration).SetEase(Ease.OutExpo));
        _sequence.Join(transform.DOScale(projectileScale, releaseDuration).SetEase(Ease.OutBack, 2.5f));

        _sequence.AppendCallback(Release);
        _sequence.OnKill(() =>
        {
            if (!_transferred)
                Destroy(gameObject);
        });
        _sequence.OnComplete(() =>
        {
            if (!_transferred)
                Destroy(gameObject);
        });
    }

    public void TransferToProjectile(Vector3 projectilePosition,
        Quaternion projectileRotation, Vector3 projectileScale)
    {
        if (_transferred)
            return;

        _transferred = true;
        _sequence?.Kill(false);
        _sequence = null;
        transform.SetPositionAndRotation(projectilePosition, projectileRotation);
        transform.localScale = projectileScale;
        ApplyProjectileSorting();
        Vector3 flightDirection = projectileRotation * Vector3.up;
        _spinVisual?.SetSpinProfile(1f, 420f, 1800f);
        _spinVisual?.EnableFlightBlur(flightDirection);
        Destroy(this);
    }

    private void ApplyProjectileSorting()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = "Default";
            renderers[i].sortingOrder = 2;
        }
    }

    private void Release()
    {
        if (_released)
            return;
        _released = true;
        Action<PierceReleaseVisual> callback = _onRelease;
        _onRelease = null;
        callback?.Invoke(this);
    }

    private void OnDestroy()
    {
        _sequence?.Kill(false);
        _sequence = null;
        _onRelease = null;
    }
}
