using UnityEngine;

/// <summary>
/// Speed-driven shaft bend for the 3D weapon.
/// The bend comes from the tip's lateral motion relative to the shaft: the shaft trails behind, then springs back.
/// It only rotates the shaft chain that was added in the shaft re-rig; the blade stays rigid to the top shaft bone,
/// so its volume and the contact point are preserved. The ribbon hangs off the top shaft bone and follows for free.
/// </summary>
[DisallowMultipleComponent]
public sealed class MeshWeaponShaftBend : MonoBehaviour
{
    [Tooltip("Shaft chain from the base toward the blade. Blade stays rigid to the last entry.")]
    [SerializeField] private Transform[] chain;
    [SerializeField] private MeshWeaponVisual visual;
    [Tooltip("Total bend across the whole chain, in degrees, at full lateral speed. Keep small: this reads as weight, not rubber.")]
    [SerializeField, Range(0f, 40f)] private float maxTotalBendDegrees = 14f;
    [Tooltip("Tip lateral speed (world units/s) that reaches the full bend.")]
    [SerializeField, Min(0.1f)] private float fullLateralSpeed = 45f;
    [Tooltip("Bend spring: higher follows the motion harder, lower lags more.")]
    [SerializeField, Min(1f)] private float stiffness = 120f;
    [SerializeField, Min(0f)] private float damping = 16f;
    [Tooltip("Frame steps longer than this are treated as a hitch and skipped.")]
    [SerializeField, Min(0.02f)] private float hitchSeconds = 0.1f;

    private Quaternion[] _restRotations;
    private Vector3 _bendLocal;
    private Vector3 _bendVelocity;
    private Vector3 _lastTipPosition;
    private bool _initialized;
    private bool _sampleValid;

    private void Awake() { Initialize(); }
    private void OnEnable() { _sampleValid = false; }
    private void OnDisable() { ApplyRest(); }

    private bool Initialize()
    {
        if (_initialized) return true;
        if (chain == null || chain.Length == 0) return false;
        for (int i = 0; i < chain.Length; i++)
        {
            if (chain[i] == null) return false;
            if (i > 0 && chain[i].parent != chain[i - 1]) return false;
        }
        if (visual == null) visual = GetComponent<MeshWeaponVisual>();
        if (visual == null) return false;
        _restRotations = new Quaternion[chain.Length];
        for (int i = 0; i < chain.Length; i++) _restRotations[i] = chain[i].localRotation;
        _initialized = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!Initialize()) return;
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || deltaTime > hitchSeconds)
        {
            _sampleValid = false;
            return;
        }

        Vector3 tip = visual.TipPosition;
        if (!_sampleValid)
        {
            _lastTipPosition = tip;
            _sampleValid = true;
            return;
        }

        Vector3 velocity = (tip - _lastTipPosition) / deltaTime;
        _lastTipPosition = tip;

        Vector3 shaft = visual.TipPosition - visual.TailPosition;
        if (shaft.sqrMagnitude < 0.000001f) return;
        shaft.Normalize();

        // Only the sideways part of the motion bends the shaft; thrusting along its own axis does not.
        Vector3 lateral = velocity - Vector3.Dot(velocity, shaft) * shaft;
        Vector3 target = -transform.InverseTransformDirection(lateral) / Mathf.Max(fullLateralSpeed, 0.001f);
        if (target.magnitude > 1f) target.Normalize();

        _bendVelocity += ((target - _bendLocal) * stiffness - _bendVelocity * damping) * deltaTime;
        _bendLocal += _bendVelocity * deltaTime;
        if (_bendLocal.magnitude > 1f) _bendLocal.Normalize();

        ApplyBend(shaft);
    }

    private void ApplyBend(Vector3 shaftWorld)
    {
        Vector3 bendWorld = transform.TransformDirection(_bendLocal);
        float magnitude = Mathf.Clamp01(_bendLocal.magnitude);
        if (magnitude <= 0.0001f || bendWorld.sqrMagnitude < 0.0000001f)
        {
            ApplyRest();
            return;
        }

        Vector3 axisWorld = Vector3.Cross(shaftWorld, bendWorld);
        if (axisWorld.sqrMagnitude < 0.0000001f)
        {
            ApplyRest();
            return;
        }
        axisWorld.Normalize();

        float perBone = maxTotalBendDegrees * magnitude / chain.Length;
        for (int i = 0; i < chain.Length; i++)
        {
            Transform bone = chain[i];
            Vector3 axisLocal = bone.parent != null
                ? bone.parent.InverseTransformDirection(axisWorld)
                : axisWorld;
            bone.localRotation = Quaternion.AngleAxis(perBone, axisLocal) * _restRotations[i];
        }
    }

    private void ApplyRest()
    {
        if (!_initialized) return;
        for (int i = 0; i < chain.Length; i++)
            if (chain[i] != null) chain[i].localRotation = _restRotations[i];
    }
}
