using System;
using DG.Tweening;
using UnityEngine;

/// <summary>Rigid 3D lift, authored in the battle camera frame. Never drives ribbon bones.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshWeaponVisual))]
public sealed class MeshSpearLaunchMotion : MonoBehaviour
{
    // Previous tip-driven prototype values are retained for serialized compatibility only.
    [SerializeField, HideInInspector] private Vector3 readyTip;
    [SerializeField, HideInInspector] private Vector3 lowTip;
    [SerializeField, HideInInspector] private Vector3 impactTip;
    [SerializeField, HideInInspector] private Vector3 apexTip;
    [Header("普通Launch：相机空间枪尾支点")]
    [SerializeField] private Vector3 readyPivot = new Vector3(-0.05f, -4.60f, 0.20f);
    [SerializeField] private Vector3 lowPivot = new Vector3(-0.06f, -5.10f, 0.20f);
    [SerializeField] private Vector3 impactPivot = new Vector3(0.70f, -4.90f, 0.40f);
    [SerializeField] private Vector3 apexPivot = new Vector3(1.72f, -4.60f, 0.50f);
    // Retain the first prototype's serialized data while replacing its direction solver.
    [SerializeField, HideInInspector] private Vector3 gripGuide = new Vector3(1f, -2.5f, 3.8f);
    [SerializeField, HideInInspector] private Vector3 gripLift = new Vector3(-0.15f, 0.25f, 0.45f);
    [Header("枪轴姿态：相机空间枪尾指向枪尖")]
    // Screen-plane angle sweeps ~100deg (ready) -> ~70deg after a clockwise cock -> up to ~120deg at the apex.
    [SerializeField] private Vector3 readyAxisCamera = new Vector3(-0.052f, 0.295f, 1f);
    [SerializeField] private Vector3 lowAxisCamera = new Vector3(0.154f, 0.423f, 1f);
    [SerializeField] private Vector3 impactAxisCamera = new Vector3(0.02f, 0.60f, 0.8f);
    [SerializeField] private Vector3 apexAxisCamera = new Vector3(-0.22f, 0.62f, 0.62f);
    [SerializeField, Min(0f)] private float maxAimOffset = 2f;
    [Header("3D挑飞：时长（再乘现有Launch播放倍率）")]
    [SerializeField, Min(0.01f)] private float windupSeconds = 0.15f;
    [Tooltip("蓄势到位后到上挑发力之间的停顿，用于制造预压。")]
    [SerializeField, Min(0f)] private float anticipationSeconds = 0.05f;
    [Tooltip("上挑段总时长。要沉重感就拉长，要爆发感就缩短；参考原版 Launch 为 0.15s。")]
    [SerializeField, Min(0.01f)] private float liftSeconds = 0.20f;
    [Tooltip("命中后的停顿：让质量压在接触点上。")]
    [SerializeField, Min(0f)] private float holdSeconds = 0.07f;
    [Tooltip("收势时长。重物停不下来，拉长才能读出重量。")]
    [SerializeField, Min(0.01f)] private float recoverySeconds = 0.23f;
    [Tooltip("命中点在推进段中的位置。原版 Launch 为 0.48；越大则蓄力段越长。")]
    [SerializeField, Range(0.1f, 0.9f)] private float impactProgress = 0.52f;
    [Tooltip("旋转相对于位移的滞后比例（同原版 Launch 的 0.14）：位移先走、旋转随后追上。")]
    [SerializeField, Range(0f, 0.5f)] private float rotationLagRatio = 0.14f;

    [Header("受控形变（只沿枪轴拉伸，不弯曲；需外弯必须重绑定杆骨）")]
    [Tooltip("满速时长枪额外拉长的比例。枪尾固定在支点上，只有枪尖前伸。")]
    [SerializeField, Range(0f, 0.25f)] private float maxStretch = 0.10f;
    [Tooltip("命中瞬间额外加入的拉伸冲量，停顿后回弹。")]
    [SerializeField, Range(0f, 0.25f)] private float impactStretch = 0.06f;
    [Tooltip("开始拉伸的枪尖速度（世界单位/秒）。取高于拉伸自身造成的回流速度，避免自激。")]
    [SerializeField, Min(0f)] private float stretchMinSpeed = 15f;
    [SerializeField, Min(0.01f)] private float stretchFullSpeed = 60f;
    [SerializeField, Min(1f)] private float stretchStiffness = 120f;
    [SerializeField, Min(0f)] private float stretchDamping = 16f;
    [Tooltip("弹簧允许的压缩下限：回弹时会短暂短于原长。")]
    [SerializeField, Range(0f, 0.1f)] private float maxSquash = 0.03f;

    public float Duration => windupSeconds + anticipationSeconds + liftSeconds + holdSeconds + recoverySeconds;
    private Sequence _sequence;
    private MeshWeaponVisual _visual;
    private Action _onImpact;
    private bool _impactInvoked;
    private bool _destroying;
    private Vector3 _origin;
    private Quaternion _frame;
    private Quaternion _initialRotation;
    private Vector3 _initialAxis;
    private Transform _pivot;
    private Vector3 _windupStartPosition;
    private Quaternion _windupStartRotation;
    private float _aimOffset;
    private Vector3 _baseScale = Vector3.one;
    private float _restLength;
    private Vector3 _lastTipPosition;
    private float _stretch = 1f;
    private float _stretchVelocity;
    private float _impactKick;
    private bool _stretchSampleValid;

    public void Begin(Vector3 playerPosition, bool preservePose, float durationScale, Action onImpact)
    {
        _visual = GetComponent<MeshWeaponVisual>();
        _onImpact = onImpact;
        var camera = Camera.main;
        _frame = camera != null ? camera.transform.rotation : Quaternion.identity;
        _origin = camera != null ? camera.transform.position : playerPosition + Vector3.back * 10f;
        Vector3 right = _frame * Vector3.right;
        _origin += right * Vector3.Dot(playerPosition - _origin, right);
        _initialRotation = transform.rotation;
        _initialAxis = (_visual.TipPosition - _visual.TailPosition).normalized;
        _aimOffset = preservePose
            ? Mathf.Clamp(Vector3.Dot(_visual.TipPosition - _origin, right), -maxAimOffset, maxAimOffset) : 0f;
        // Deformation baseline: rest length and rest scale must be captured before any pose is applied.
        _baseScale = transform.localScale;
        _restLength = _visual.Length;
        _lastTipPosition = _visual.TipPosition;
        _stretch = 1f;
        _stretchVelocity = 0f;
        _impactKick = 0f;
        _stretchSampleValid = true;
        // Exactly like C4's lever hierarchy, but use the actual tail anchor.
        _pivot = new GameObject("OrdinaryLaunch_TailPivot").transform;
        _pivot.SetPositionAndRotation(_visual.TailPosition, _initialRotation);
        transform.SetParent(_pivot, true);
        _visual.SetAlpha(1f);
        _visual.SetSorting(2);
        foreach (var collider in GetComponentsInChildren<Collider>()) Destroy(collider);

        if (!preservePose)
            PlacePivot(Point(readyPivot), RotationForAxis(readyAxisCamera));
        _windupStartPosition = _pivot.position;
        _windupStartRotation = _pivot.rotation;

        float scale = Mathf.Max(0.01f, durationScale);
        _sequence = DOTween.Sequence().SetTarget(this).SetUpdate(UpdateType.Normal, false);
        // Weight: the cock fights the mass, so it starts slow and settles into place (not a fast snap).
        _sequence.Append(DOTween.To(() => 0f, SampleWindup, 1f, windupSeconds * scale).SetEase(Ease.InOutQuad));
        if (anticipationSeconds > 0f) _sequence.AppendInterval(anticipationSeconds * scale);
        _sequence.Append(DOTween.To(() => 0f, SampleLift, 1f, liftSeconds * scale).SetEase(Ease.Linear));
        _sequence.InsertCallback((windupSeconds + anticipationSeconds + liftSeconds * impactProgress) * scale, InvokeImpact);
        _sequence.AppendInterval(holdSeconds * scale);
        _sequence.Append(DOTween.To(() => 0f, SampleRecovery, 1f, recoverySeconds * scale).SetEase(Ease.Linear));
        _sequence.OnComplete(() => { _destroying = true; Destroy(gameObject); });
        _sequence.OnKill(() => { if (!_destroying) Destroy(gameObject); });
    }

    private Vector3 Point(Vector3 cameraPoint)
    {
        cameraPoint.x += _aimOffset;
        return _origin + _frame * cameraPoint;
    }

    private Quaternion RotationForAxis(Vector3 cameraAxis)
    {
        Vector3 direction = _frame * cameraAxis.normalized;
        return Quaternion.FromToRotation(_initialAxis, direction) * _initialRotation;
    }

    private void PlacePivot(Vector3 position, Quaternion rotation)
    {
        // Never correct the tip after rotation: it must sweep the full lever radius.
        _pivot.SetPositionAndRotation(position, rotation);
    }

    private void SampleWindup(float progress)
    {
        float t = Mathf.SmoothStep(0f, 1f, progress);
        Vector3 position = Vector3.Lerp(_windupStartPosition, Point(lowPivot), t);
        Quaternion rotation = Quaternion.Slerp(_windupStartRotation, RotationForAxis(lowAxisCamera), t);
        PlacePivot(position, rotation);
    }

    private void SampleLift(float progress)
    {
        Vector3 position;
        if (progress <= impactProgress)
        {
            float t = progress / impactProgress;
            position = Vector3.Lerp(Point(lowPivot), Point(impactPivot), t * t);
        }
        else
        {
            float t = (progress - impactProgress) / (1f - impactProgress);
            position = Vector3.Lerp(Point(impactPivot), Point(apexPivot), 1f - (1f - t) * (1f - t));
        }
        // Only the tail support translates. The weapon's local offset remains constant.
        // Rotation lags the displacement, then whips through the impact (same trick as the legacy Launch).
        Quaternion rotation;
        if (progress <= impactProgress)
        {
            float delayed = Mathf.Clamp01((progress / impactProgress - rotationLagRatio) / (1f - rotationLagRatio));
            float eased = delayed * delayed * (3f - 2f * delayed);
            rotation = Quaternion.Slerp(RotationForAxis(lowAxisCamera), RotationForAxis(impactAxisCamera), eased);
        }
        else
        {
            float t = (progress - impactProgress) / (1f - impactProgress);
            rotation = Quaternion.Slerp(RotationForAxis(impactAxisCamera), RotationForAxis(apexAxisCamera), 1f - (1f - t) * (1f - t));
        }
        PlacePivot(position, rotation);
    }

    private void SampleRecovery(float progress)
    {
        float t = Mathf.SmoothStep(0f, 1f, progress);
        Vector3 start = Point(apexPivot);
        Vector3 control = Point(apexPivot + new Vector3(0.05f, -0.1f, -0.4f));
        Vector3 end = Point(readyPivot);
        Vector3 position = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * end;
        PlacePivot(position, Quaternion.Slerp(RotationForAxis(apexAxisCamera), RotationForAxis(readyAxisCamera), t));
        _visual.SetAlpha(1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, progress)));
    }

    private void InvokeImpact()
    {
        if (_impactInvoked) return;
        _impactInvoked = true;
        // A short extra stretch right at contact, then the spring pulls it back (rebound).
        _impactKick = impactStretch;
        _onImpact?.Invoke();
    }

    private void LateUpdate()
    {
        if (_visual == null || _pivot == null) return;
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f) return;

        Vector3 tip = _visual.TipPosition;
        float speed = _stretchSampleValid ? (tip - _lastTipPosition).magnitude / deltaTime : 0f;
        _lastTipPosition = tip;
        _stretchSampleValid = true;
        // A long frame is not real speed; ignore it instead of injecting a false stretch.
        if (deltaTime > 0.1f) speed = 0f;

        float driven = maxStretch * Mathf.Clamp01(Mathf.InverseLerp(stretchMinSpeed, stretchFullSpeed, speed));
        _impactKick = Mathf.MoveTowards(_impactKick, 0f, deltaTime * Mathf.Max(impactStretch, 0.01f) * 8f);
        float target = 1f + driven + _impactKick;

        _stretchVelocity += ((target - _stretch) * stretchStiffness - _stretchVelocity * stretchDamping) * deltaTime;
        _stretch += _stretchVelocity * deltaTime;
        _stretch = Mathf.Clamp(_stretch, 1f - maxSquash, 1f + maxStretch + impactStretch);
        ApplyStretch();
    }

    /// <summary>
    /// Stretch only along the shaft. The tail is measured before/after and compensated so it stays on the
    /// lever pivot: the hand position and the contact relationship stay intact, only the tip reaches further.
    /// </summary>
    private void ApplyStretch()
    {
        Vector3 tailBefore = _visual.TailPosition;
        Vector3 scale = _baseScale;
        scale.y *= _stretch;
        transform.localScale = scale;
        transform.position += tailBefore - _visual.TailPosition;
    }

    private void OnDestroy()
    {
        _destroying = true;
        _sequence?.Kill(false);
        _sequence = null;
        _onImpact = null;
        if (_pivot != null) Destroy(_pivot.gameObject);
    }
}
