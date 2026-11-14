using UnityEngine;

/// <summary>
/// **共用弹簧骨链求解器**（架构 ⑤ · 2026-11-14）。
///
/// 以前「铁链」（<c>ChainLinkRig</c>）与「红缨」（<c>WeaponTasselDynamics</c>）各写了一套弹簧骨链：
/// 概念相同（每节用弹簧拉向目标 + 阻尼 + 重力），但数学表述不同 ⇒ 两套参数、两套手感，
/// 调一个参数要改两处代码。
///
/// 本类把**共用的积分核心**抽出来，两边各自只保留「锚点怎么接」与「怎么画」：
/// <list type="bullet">
///   <item><b>共用</b>：速度积分（弹簧 + 重力）、指数阻尼、径向速度剔除、速度上限、子步进。</item>
///   <item><b>不共用</b>（各自实现，别混进来）：
///     · 红缨：<b>静止朝向</b>（rest pose）、<b>瞬移重置</b>、枪杆避让（碰撞）、角度上限；
///     · 铁链：<b>距离约束</b>（每节长度固定）、链尾钉在玩家手、链头由外部驱动。</item>
/// </list>
///
/// 求解顺序（每个子步、每节）：
/// <c>准备 → 取约束点/目标 → 速度积分 → 方向求解 → 长度约束 → 弯曲上限 → 额外修正 → 回写 → 更新速度</c>。
///
/// ★ 为什么是**普通类**而不是 MonoBehaviour 基类：
///   红缨挂在玩家的长枪预制体上、字段（bones/tip/weaponTip/weaponTail）是**序列化引用**。
///   改类继承关系 / 搬字段有丢引用的风险（已验收的资产）⇒ 这里用**组合**：
///   两个组件各自持有一个求解器实例，序列化字段一个都不动。
/// </summary>
public abstract class SpringBoneChain
{
    /// <summary>节点数（= 骨骼数）。</summary>
    public int Count { get; protected set; }

    protected Vector3[] _positions;
    protected Vector3[] _velocities;

    // ── 共用手感参数（两处链共用同一套语义）────────────────────────────────
    /// <summary>弹簧硬度：越大越快贴住目标。</summary>
    public float Stiffness = 100f;
    /// <summary>阻尼：越大越快停。</summary>
    public float Damping = 12f;
    /// <summary>重力下垂比例（乘 <see cref="Physics.gravity"/>）。</summary>
    public float GravityScale = 0.12f;
    /// <summary>速度上限（相对尺度；子类可换算成自己的长度单位）。</summary>
    public float MaxSpeedRatio = 12f;
    /// <summary>子步进数：1 = 不细分。越大越稳，也越贵。</summary>
    public int Substeps = 1;

    /// <summary>初始化节点数组（位置可给初值，速度清零）。</summary>
    public void Initialize(int count, Vector3[] initialPositions = null)
    {
        Count = Mathf.Max(0, count);
        _positions = new Vector3[Count];
        _velocities = new Vector3[Count];
        if (initialPositions != null)
            for (int i = 0; i < Count && i < initialPositions.Length; i++) _positions[i] = initialPositions[i];
    }

    /// <summary>把节点位置复位到给定值（速度清零）。</summary>
    public void ResetTo(Vector3[] positions)
    {
        if (_positions == null) return;
        for (int i = 0; i < _positions.Length; i++)
        {
            if (positions != null && i < positions.Length) _positions[i] = positions[i];
            _velocities[i] = Vector3.zero;
        }
    }

    /// <summary>读某一节的当前求解位置（世界坐标）。</summary>
    public Vector3 GetPosition(int index)
        => (_positions != null && index >= 0 && index < _positions.Length) ? _positions[index] : Vector3.zero;

    /// <summary>推进一帧（内部按 <see cref="Substeps"/> 细分）。</summary>
    public void Step(float deltaTime)
    {
        if (_positions == null || Count <= 0 || deltaTime <= 0f) return;
        BeforeStep();
        int steps = Mathf.Clamp(Substeps, 1, 12);
        float dt = deltaTime / steps;
        float drag = Mathf.Exp(-Damping * dt);
        for (int s = 0; s < steps; s++)
            for (int i = 0; i < Count; i++)
                IntegrateNode(i, dt, drag);
        AfterStep();
    }

    // ── 子类钩子 ─────────────────────────────────────────────────────────

    /// <summary>每子步开始前（子类：把锚点/钉死点更新到位）。</summary>
    protected virtual void BeforeStep() { }

    /// <summary>每子步结束后（子类：收尾）。</summary>
    protected virtual void AfterStep() { }

    /// <summary>每子步、每节的准备（红缨：复位到静止姿态，好让父级驱动生效）。</summary>
    protected virtual void PrepareNode(int index) { }

    /// <summary>
    /// 取本节的「约束作用点」<paramref name="origin"/> 与「弹簧目标点」<paramref name="target"/>（世界坐标）。
    /// 返回 false = 本节不参与求解。
    /// </summary>
    protected abstract bool ComputeNode(int index, out Vector3 origin, out Vector3 target);

    /// <summary>方向退化时的兜底朝向。</summary>
    protected virtual Vector3 FallbackDirection(int index, Vector3 origin, Vector3 target)
        => target - origin;

    /// <summary>弯曲上限（红缨：按静止朝向夹角）。</summary>
    protected virtual bool ClampBend(int index, ref Vector3 direction) => false;

    /// <summary>额外修正（红缨：枪杆避让碰撞）。返回 false = 放弃本节。</summary>
    protected virtual bool RefineNode(int index, ref Vector3 position, ref Vector3 direction) => true;

    /// <summary>本节到约束点的固定长度（&lt;= 0 = 不做硬长度约束，只用弹簧目标）。</summary>
    protected virtual float NodeLength(int index) => 0f;

    /// <summary>速度上限（红缨：按整杆枪长换算；铁链：按节长）。</summary>
    protected virtual float MaxSpeed(int index) => MaxSpeedRatio;

    /// <summary>把算好的位置写回表现层。</summary>
    protected abstract void ApplyNode(int index, Vector3 position, Vector3 direction);

    // ── 核心积分 ─────────────────────────────────────────────────────────

    private void IntegrateNode(int index, float dt, float drag)
    {
        PrepareNode(index);

        if (!ComputeNode(index, out Vector3 origin, out Vector3 target)) return;

        Vector3 previous = _positions[index];
        Vector3 velocity = _velocities[index] * drag;
        velocity += ((target - previous) * Stiffness + Physics.gravity * GravityScale) * dt;
        Vector3 candidate = previous + velocity * dt;

        Vector3 direction = candidate - origin;
        if (direction.sqrMagnitude > 0.00000001f) direction.Normalize();
        else direction = FallbackDirection(index, origin, target);

        ClampBend(index, ref direction);

        Vector3 position = candidate;
        float length = NodeLength(index);
        if (length > 0f) position = origin + direction * length;

        if (!RefineNode(index, ref position, ref direction)) return;

        // 速度由「实际落点」反推；剔除沿节方向的径向分量（那是约束拉伸出来的，不是甩动）
        velocity = (position - previous) / dt;
        velocity -= Vector3.Dot(velocity, direction) * direction;
        _velocities[index] = Vector3.ClampMagnitude(velocity, MaxSpeed(index));
        _positions[index] = position;

        ApplyNode(index, position, direction);
    }
}
