using UnityEngine;

/// <summary>Small, explicitly wired spring chain evaluated after weapon motion and deformation.</summary>
[DefaultExecutionOrder(1000)]
[DisallowMultipleComponent]
public sealed class WeaponTasselDynamics : MonoBehaviour
{
    [Tooltip("Ordered deform bones, starting at the fixed ribbon attachment.")]
    [SerializeField] private Transform[] bones;
    [SerializeField] private Transform tip;
    [SerializeField] private Transform weaponTip;
    [SerializeField] private Transform weaponTail;
    [SerializeField, Min(0f)] private float stiffness = 100f;
    [SerializeField, Min(0f)] private float damping = 12f;
    [SerializeField, Range(0f, 1f)] private float gravityScale = 0.12f;
    [SerializeField, Range(1f, 85f)] private float maxBendDegrees = 38f;
    [Tooltip("Simple shaft clearance, including ribbon thickness, as a fraction of weapon length.")]
    [SerializeField, Range(0f, 0.1f)] private float shaftClearanceRatio = 0.035f;
    [Tooltip("Reset after an attachment jump exceeding this fraction of weapon length.")]
    [SerializeField, Min(0.1f)] private float teleportLengthRatio = 0.75f;

    private Quaternion[] _restRotations;
    private Vector3[] _restDirections;
    private Vector3[] _positions;
    private Vector3[] _velocities;
    private Vector3 _lastAnchor;
    private bool _initialized;
    private bool _sampleValid;
    private bool _motionPaused;

    public float WeaponLength => weaponTip != null && weaponTail != null
        ? Vector3.Distance(weaponTip.position, weaponTail.position) : 0f;

    private void Awake() { Initialize(); }
    private void OnEnable() { _sampleValid = false; }

    private bool Initialize()
    {
        if (_initialized) return true;
        if (bones == null || bones.Length == 0 || tip == null) return false;
        for (int i = 0; i < bones.Length; i++)
            if (bones[i] == null || (i > 0 && bones[i].parent != bones[i - 1])) return false;
        if (tip.parent != bones[bones.Length - 1]) return false;

        int count = bones.Length;
        _restRotations = new Quaternion[count];
        _restDirections = new Vector3[count];
        _positions = new Vector3[count];
        _velocities = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Transform end = i + 1 < count ? bones[i + 1] : tip;
            _restRotations[i] = bones[i].localRotation;
            _restDirections[i] = bones[i].InverseTransformPoint(end.position);
        }
        _initialized = true;
        return true;
    }

    public void ResetSimulation()
    {
        if (!Initialize()) return;
        for (int i = 0; i < bones.Length; i++) bones[i].localRotation = _restRotations[i];
        for (int i = 0; i < bones.Length; i++)
        {
            _positions[i] = (i + 1 < bones.Length ? bones[i + 1] : tip).position;
            _velocities[i] = Vector3.zero;
        }
        _lastAnchor = bones[0].position;
        _sampleValid = true;
    }

    public void SetMotionPaused(bool paused)
    {
        _motionPaused = paused;
    }

    private void LateUpdate()
    {
        if (!Initialize()) return;
        if (!_sampleValid) ResetSimulation();
        float dt = Time.deltaTime;
        if (_motionPaused || dt <= 0f) return;
        Simulate(dt);
    }

    // Also used by the bounded editor verification with explicit sample times.
    public void Simulate(float deltaTime)
    {
        if (!Initialize() || deltaTime <= 0f) return;
        if (!_sampleValid) ResetSimulation();
        float length = Mathf.Max(WeaponLength, 0.01f);
        Vector3 anchor = bones[0].position;
        if (deltaTime > 0.1f || (anchor - _lastAnchor).sqrMagnitude >
            length * length * teleportLengthRatio * teleportLengthRatio)
        {
            ResetSimulation();
            return;
        }
        _lastAnchor = anchor;
        int steps = Mathf.Clamp(Mathf.CeilToInt(deltaTime / (1f / 120f)), 1, 12);
        float dt = deltaTime / steps;
        float drag = Mathf.Exp(-damping * dt);
        for (int step = 0; step < steps; step++)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                bone.localRotation = _restRotations[i];
                Vector3 origin = bone.position;
                Vector3 restVector = bone.TransformVector(_restDirections[i]);
                float segmentLength = restVector.magnitude;
                if (segmentLength < 0.000001f) continue;
                Vector3 restDirection = restVector / segmentLength;
                Vector3 target = origin + restVector;
                Vector3 previous = _positions[i];
                Vector3 velocity = _velocities[i] * drag;
                velocity += ((target - previous) * stiffness + Physics.gravity * gravityScale) * dt;
                Vector3 candidate = previous + velocity * dt - origin;
                Vector3 direction = candidate.sqrMagnitude > 0.00000001f
                    ? candidate.normalized : restDirection;
                direction = Vector3.RotateTowards(restDirection, direction,
                    maxBendDegrees * Mathf.Deg2Rad, 0f);
                Vector3 position = origin + direction * segmentLength;
                // Alternate shaft and length constraints; collision takes priority at the bend limit.
                if (weaponTip != null && weaponTail != null && shaftClearanceRatio > 0f)
                {
                    Vector3 axis = weaponTip.position - weaponTail.position;
                    float axisLengthSquared = axis.sqrMagnitude;
                    float clearance = length * shaftClearanceRatio;
                    for (int iteration = 0; iteration < 10 && axisLengthSquared > 0.000001f; iteration++)
                    {
                        float along = Mathf.Clamp01(Vector3.Dot(position - weaponTail.position, axis) / axisLengthSquared);
                        Vector3 nearest = weaponTail.position + axis * along;
                        Vector3 radial = position - nearest;
                        if (radial.sqrMagnitude >= clearance * clearance) break;
                        if (radial.sqrMagnitude < 0.00000001f)
                            radial = Vector3.ProjectOnPlane(target - nearest, axis);
                        if (radial.sqrMagnitude < 0.00000001f)
                            radial = Vector3.Cross(axis, Vector3.right);
                        if (radial.sqrMagnitude < 0.00000001f)
                            radial = Vector3.Cross(axis, Vector3.up);
                        position = nearest + radial.normalized * clearance;
                        direction = (position - origin).normalized;
                        position = origin + direction * segmentLength;
                    }
                }
                velocity = (position - previous) / dt;
                // Remove radial velocity generated by segment constraints.
                velocity -= Vector3.Dot(velocity, direction) * direction;
                _velocities[i] = Vector3.ClampMagnitude(velocity, length * 12f);
                _positions[i] = position;
                bone.rotation = Quaternion.FromToRotation(restDirection, direction) * bone.rotation;
            }
        }
    }

    private void OnDisable()
    {
        if (_initialized)
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].localRotation = _restRotations[i];
        _sampleValid = false;
        _motionPaused = false;
    }
}
