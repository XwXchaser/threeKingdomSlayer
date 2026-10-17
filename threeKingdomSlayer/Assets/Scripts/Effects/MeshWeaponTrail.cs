using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Short, speed-driven motion trail for the 3D weapon.
/// It follows explicit rigid anchors only: the skinned mesh is never sampled, so there is no BakeMesh cost.
/// Emissive only above a speed threshold, and it clears itself on disable so nothing is left dragging.
/// </summary>
[DisallowMultipleComponent]
public sealed class MeshWeaponTrail : MonoBehaviour
{
    [Tooltip("Rigid sample points on the weapon, e.g. the tip anchor. Ribbon bones must not be used.")]
    [SerializeField] private Transform[] anchors;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private Color headColor = new Color(1f, 0.95f, 0.86f, 0.85f);
    [SerializeField] private Color tailColor = new Color(1f, 0.95f, 0.86f, 0f);
    [Tooltip("Head width in world units, as a fraction of weapon length.")]
    [SerializeField, Range(0.005f, 0.2f)] private float headWidthOfLength = 0.045f;
    [Tooltip("Tail width relative to the head width.")]
    [SerializeField, Range(0f, 1f)] private float tailWidthRatio = 0.08f;
    [Tooltip("Seconds a sample survives. Short values read as speed rather than as a ribbon.")]
    [SerializeField, Range(0.02f, 0.4f)] private float trailSeconds = 0.08f;
    [Tooltip("Sampled world speed (units/s) below which the trail stops emitting.")]
    [SerializeField, Min(0f)] private float minSpeed = 12f;
    [Tooltip("Sampled world speed (units/s) that reaches full width and opacity.")]
    [SerializeField, Min(0.01f)] private float fullSpeed = 75f;
    [Tooltip("Width and opacity floor while the trail is emitting.")]
    [SerializeField, Range(0f, 1f)] private float minIntensity = 0.35f;
    [Tooltip("Frame steps longer than this are treated as a hitch: samples reset instead of drawing a stray segment.")]
    [SerializeField, Min(0.02f)] private float hitchSeconds = 0.1f;
    [Tooltip("Suppress emission while the sampled point travels backward along the shaft (e.g. a stab pull-back, which would otherwise read as drag). 0 = never suppress.")]
    [SerializeField, Range(0f, 1f)] private float retractSuppression = 0.15f;
    [SerializeField] private string sortingLayer = "Default";
    [SerializeField] private int sortingOrder = 3;

    private TrailRenderer[] _trails;
    private Vector3[] _lastPositions;
    private MeshWeaponVisual _visual;
    private float _length;
    private int _alphaStep = -1;
    private bool _initialized;
    private bool _sampleValid;

    private void Awake() { Initialize(); }

    private void OnEnable()
    {
        if (_initialized) ClearAll();
        _sampleValid = false;
    }

    private void OnDisable() { ClearAll(); }

    private bool Initialize()
    {
        if (_initialized) return true;
        if (anchors == null || anchors.Length == 0) return false;

        var visual = GetComponent<MeshWeaponVisual>();
        _visual = visual;
        _length = visual != null ? visual.Length : 0f;
        if (_length <= 0.0001f) _length = 10f;

        // Counteract the weapon scale so widths stay in world units whatever the prefab scale becomes.
        Vector3 scale = transform.lossyScale;
        var holder = new GameObject("MeshWeaponTrailHolder");
        holder.transform.SetParent(transform, false);
        holder.transform.localPosition = Vector3.zero;
        holder.transform.localRotation = Quaternion.identity;
        holder.transform.localScale = new Vector3(
            1f / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            1f / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            1f / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));

        _trails = new TrailRenderer[anchors.Length];
        _lastPositions = new Vector3[anchors.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            if (anchors[i] == null) continue;
            // Rigid offset from the weapon root, so it stays correct through every attack pose.
            Vector3 rootLocal = transform.InverseTransformPoint(anchors[i].position);
            var sample = new GameObject("TrailSample_" + i);
            sample.transform.SetParent(holder.transform, false);
            sample.transform.localPosition = Vector3.Scale(rootLocal, scale);
            sample.transform.localRotation = Quaternion.identity;

            var trail = sample.AddComponent<TrailRenderer>();
            trail.sharedMaterial = trailMaterial;
            trail.time = trailSeconds;
            trail.minVertexDistance = 0.05f;
            trail.autodestruct = false;
            trail.emitting = false;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCapVertices = 0;
            trail.numCornerVertices = 0;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.lightProbeUsage = LightProbeUsage.Off;
            trail.reflectionProbeUsage = ReflectionProbeUsage.Off;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, tailWidthRatio);
            trail.widthMultiplier = headWidthOfLength * _length;
            trail.startColor = headColor;
            trail.endColor = tailColor;
            trail.sortingLayerName = sortingLayer;
            trail.sortingOrder = sortingOrder;
            _trails[i] = trail;
            _lastPositions[i] = sample.transform.position;
        }
        _initialized = _trails[0] != null;
        return _initialized;
    }

    private void LateUpdate()
    {
        if (!Initialize()) return;

        float deltaTime = Time.deltaTime;
        // Paused or hit-stopped: stop adding samples rather than growing a frozen ribbon.
        if (deltaTime <= 0f || deltaTime > hitchSeconds)
        {
            SetEmitting(false);
            _sampleValid = false;
            return;
        }

        if (!_sampleValid)
        {
            ResetSamples();
            _sampleValid = true;
            SetEmitting(false);
            return;
        }

        float peak = 0f;
        bool reversed = false;
        Vector3 axis = AxisDirection();
        for (int i = 0; i < _trails.Length; i++)
        {
            TrailRenderer trail = _trails[i];
            if (trail == null) continue;
            Vector3 position = trail.transform.position;
            Vector3 velocity = (position - _lastPositions[i]) / deltaTime;
            _lastPositions[i] = position;
            float speed = velocity.magnitude;
            float intensity = Mathf.Clamp01(Mathf.InverseLerp(minSpeed, fullSpeed, speed));
            // Travelling back along the shaft is a pull-back, not a swing.
            if (retractSuppression > 0f
                && Vector3.Dot(velocity, axis) < -retractSuppression * speed)
            {
                reversed = true;
                intensity = 0f;
            }
            if (intensity > peak) peak = intensity;
        }

        if (reversed)
        {
            // Drop the recorded ribbon instead of folding it back over itself, which reads as drag.
            SetEmitting(false);
            for (int i = 0; i < _trails.Length; i++)
                if (_trails[i] != null) _trails[i].Clear();
            _alphaStep = -1;
            return;
        }

        SetEmitting(peak > 0.001f);
        if (peak <= 0.001f) return;

        float widthScale = Mathf.Lerp(minIntensity, 1f, peak);
        for (int i = 0; i < _trails.Length; i++)
            if (_trails[i] != null)
                _trails[i].widthMultiplier = headWidthOfLength * _length * widthScale;

        int step = Mathf.RoundToInt(peak * 16f);
        if (step == _alphaStep) return;
        _alphaStep = step;
        float alphaScale = Mathf.Lerp(minIntensity, 1f, peak);
        for (int i = 0; i < _trails.Length; i++)
        {
            TrailRenderer trail = _trails[i];
            if (trail == null) continue;
            Color head = headColor;
            head.a *= alphaScale;
            trail.startColor = head;
            trail.endColor = tailColor;
        }
    }

    private Vector3 AxisDirection()
    {
        if (_visual == null) return Vector3.zero;
        Vector3 direction = _visual.TipPosition - _visual.TailPosition;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
    }

    private void SetEmitting(bool emitting)
    {
        if (_trails == null) return;
        for (int i = 0; i < _trails.Length; i++)
            if (_trails[i] != null && _trails[i].emitting != emitting)
                _trails[i].emitting = emitting;
    }

    private void ResetSamples()
    {
        if (_trails == null) return;
        for (int i = 0; i < _trails.Length; i++)
            if (_trails[i] != null)
            {
                _lastPositions[i] = _trails[i].transform.position;
                _trails[i].Clear();
            }
        _alphaStep = -1;
    }

    private void ClearAll()
    {
        _sampleValid = false;
        if (_trails == null) return;
        for (int i = 0; i < _trails.Length; i++)
            if (_trails[i] != null)
            {
                _trails[i].emitting = false;
                _trails[i].Clear();
            }
    }
}
