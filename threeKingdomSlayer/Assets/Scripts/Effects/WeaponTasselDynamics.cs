using UnityEngine;

/// <summary>Small, explicitly wired spring chain evaluated after weapon motion and deformation.</summary>
/// <remarks>
/// ★ 架构 ⑤（2026-11-14）：积分核心已抽到 <see cref="SpringBoneChain"/>，
/// 本类只保留「锚点怎么接」与「怎么画」：
///   · 静止朝向（rest pose）—— <c>_restRotations</c> / <c>_restDirections</c>；
///   · 瞬移重置 —— <c>ResetSimulation</c> + <c>teleportLengthRatio</c>；
///   · 枪杆避让（碰撞）—— <c>shaftClearanceRatio</c>；
///   · 角度上限 —— <c>maxBendDegrees</c>。
/// ★ 序列化字段**一个都没动**（bones / tip / weaponTip / weaponTail 是预制体上的引用，
///   搬字段或改继承关系有丢引用的风险）⇒ 共用求解器用**组合**接入。
/// </remarks>
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
    private Vector3 _lastAnchor;
    private bool _initialized;
    private bool _sampleValid;
    private bool _motionPaused;

    /// <summary>共用求解器（架构 ⑤）：积分核心在 <see cref="SpringBoneChain"/>，这里只接「红缨的锚点」。</summary>
    private readonly TasselSolver _solver = new TasselSolver();

    public float WeaponLength => weaponTip != null && weaponTail != null
        ? Vector3.Distance(weaponTip.position, weaponTail.position) : 0f;

    /// <summary>
    /// 调快回弹、减小摆幅（持枪姿态用）：红缨是速度驱动的弹簧骨链，
    /// 动作后会继续摆动 → 枪头附近的视觉角度每次都不同。持枪时收敛它，让枪尖读数稳定。
    /// </summary>
    public void SetSettle(float newDamping, float newMaxBendDegrees)
    {
        if (newDamping > 0f) damping = newDamping;
        if (newMaxBendDegrees > 0f) maxBendDegrees = newMaxBendDegrees;
    }

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
        for (int i = 0; i < count; i++)
        {
            Transform end = i + 1 < count ? bones[i + 1] : tip;
            _restRotations[i] = bones[i].localRotation;
            _restDirections[i] = bones[i].InverseTransformPoint(end.position);
        }
        _solver.Initialize(count);
        _initialized = true;
        return true;
    }

    public void ResetSimulation()
    {
        if (!Initialize()) return;
        for (int i = 0; i < bones.Length; i++) bones[i].localRotation = _restRotations[i];
        var positions = new Vector3[bones.Length];
        for (int i = 0; i < bones.Length; i++)
            positions[i] = (i + 1 < bones.Length ? bones[i + 1] : tip).position;
        _solver.ResetTo(positions);
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

        // ★ 架构 ⑤：手感参数 → 共用求解器（序列化字段仍是唯一真值来源）
        _solver.Owner = this;
        _solver.Stiffness = stiffness;
        _solver.Damping = damping;
        _solver.GravityScale = gravityScale;
        _solver.MaxSpeedRatio = 12f;
        _solver.Substeps = Mathf.Clamp(Mathf.CeilToInt(deltaTime / (1f / 120f)), 1, 12);
        _solver.Step(deltaTime);
    }

    private void OnDisable()
    {
        if (_initialized)
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].localRotation = _restRotations[i];
        _sampleValid = false;
        _motionPaused = false;
    }

    /// <summary>
    /// 红缨专用的求解器适配层：把「红缨的锚点」接进共用积分核心。
    /// 只做「怎么接」，不做物理（物理在 <see cref="SpringBoneChain"/>）。
    /// </summary>
    private sealed class TasselSolver : SpringBoneChain
    {
        /// <summary>宿主组件（求解器要读它的骨骼与静止姿态）。</summary>
        public WeaponTasselDynamics Owner;

        // 每节的临时量（求解器逐节调用，立即用掉）
        private Vector3 _restDirection;
        private Vector3 _nodeTarget;
        private Vector3 _nodeOrigin;
        private float _segmentLength;

        protected override void PrepareNode(int index)
        {
            // ★ 红缨专有：把骨骼复位到**静止姿态**（localRotation），
            //   好让「父级驱动出来的世界位置」是干净的（父的旋转在上一节就已更新）。
            Owner.bones[index].localRotation = Owner._restRotations[index];
        }

        protected override bool ComputeNode(int index, out Vector3 origin, out Vector3 target)
        {
            Transform bone = Owner.bones[index];
            origin = bone.position;
            _nodeOrigin = origin;
            Vector3 restVector = bone.TransformVector(Owner._restDirections[index]);
            _segmentLength = restVector.magnitude;
            if (_segmentLength < 0.000001f)
            {
                target = origin;
                return false;
            }
            _restDirection = restVector / _segmentLength;
            target = origin + restVector;
            _nodeTarget = target;
            return true;
        }

        protected override Vector3 FallbackDirection(int index, Vector3 origin, Vector3 target) => _restDirection;

        protected override bool ClampBend(int index, ref Vector3 direction)
        {
            direction = Vector3.RotateTowards(_restDirection, direction,
                Owner.maxBendDegrees * Mathf.Deg2Rad, 0f);
            return true;
        }

        protected override float NodeLength(int index) => _segmentLength;

        protected override float MaxSpeed(int index)
            => Mathf.Max(Owner.WeaponLength, 0.01f) * MaxSpeedRatio;

        protected override bool RefineNode(int index, ref Vector3 position, ref Vector3 direction)
        {
            // ★ 红缨专有：枪杆避让（红缨不能穿进枪杆）。
            Transform wTip = Owner.weaponTip;
            Transform wTail = Owner.weaponTail;
            if (wTip == null || wTail == null || Owner.shaftClearanceRatio <= 0f) return true;

            float length = Mathf.Max(Owner.WeaponLength, 0.01f);
            Vector3 axis = wTip.position - wTail.position;
            float axisLengthSquared = axis.sqrMagnitude;
            float clearance = length * Owner.shaftClearanceRatio;
            for (int iteration = 0; iteration < 10 && axisLengthSquared > 0.000001f; iteration++)
            {
                float along = Mathf.Clamp01(Vector3.Dot(position - wTail.position, axis) / axisLengthSquared);
                Vector3 nearest = wTail.position + axis * along;
                Vector3 radial = position - nearest;
                if (radial.sqrMagnitude >= clearance * clearance) break;
                if (radial.sqrMagnitude < 0.00000001f)
                    radial = Vector3.ProjectOnPlane(_nodeTarget - nearest, axis);
                if (radial.sqrMagnitude < 0.00000001f)
                    radial = Vector3.Cross(axis, Vector3.right);
                if (radial.sqrMagnitude < 0.00000001f)
                    radial = Vector3.Cross(axis, Vector3.up);
                position = nearest + radial.normalized * clearance;
                direction = (position - _nodeOrigin).normalized;
                position = _nodeOrigin + direction * _segmentLength;
            }
            return true;
        }

        protected override void ApplyNode(int index, Vector3 position, Vector3 direction)
        {
            Transform bone = Owner.bones[index];
            bone.rotation = Quaternion.FromToRotation(_restDirection, direction) * bone.rotation;
        }
    }
}
