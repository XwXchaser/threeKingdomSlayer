using UnityEngine;

/// <summary>链的形状与手感参数（由技能资产里的分级配置映射过来）。</summary>
[System.Serializable]
public struct ChainLinkSettings
{
    [Tooltip("链节数量（= 骨骼数）。越多越细腻，也越贵。")]
    public int SegmentCount;
    [Tooltip("链节在世界里的长度（单位）。")]
    public float SegmentLength;
    [Tooltip("链节显示缩放（链环固定尺寸，靠间距读出链节）。")]
    public float LinkScale;
    [Tooltip("弹簧硬度（越大越快贴住目标）。")]
    public float Stiffness;
    [Tooltip("阻尼（越大越快停）。")]
    public float Damping;
    [Tooltip("重力下垂比例。")]
    public float GravityScale;
}

/// <summary>
/// **铁链骨链**（追命锁链的表现层 · 占位实现）。
///
/// ★ 设计口径（用户 2026-11-14：「锁链麻烦些，因为要有铁链的表现，你需要想想如何安排骨骼和动画」）：
///
///   **骨骼**：链子 = 一串 **N 节骨骼**，每节一个位置点 + 一个朝向（上一节 → 本节）。
///   链尾固定在**玩家手**，链头挂**镰刀**（占位期 = 本杆长枪）。
///   骨骼**不在美术资产里**，而是运行时按程序生成 ⇒ 以后换成正式链子（骨骼蒙皮 / 逐节模型）
///   只需要换“每节怎么画”，**求解器不变**。
///
///   **动画（三个状态，与 `WeaponTasselDynamics` 的弹簧骨链同一套思路）**：
///   ① <b>甩出 Throw</b>：链头（枪）飞向目标，链身靠弹簧**拖在后面** ⇒ 读成“甩链”；
///   ② <b>绷紧 Taut</b>：链身被拉直（总长约束），目标被**拽**向玩家；
///   ③ <b>回收 Return</b>：链头飞回玩家，链身继续甩尾。
///
///   求解：距离约束（每节长度固定）+ 位置弹簧（软硬可调）+ 重力下垂。
///   位置在 <c>LateUpdate</c> 里求解 ⇒ 与武器/角色动画同帧，不会抖。
///
/// ★ 架构 ⑤（2026-11-14）：积分核心已抽到 <see cref="SpringBoneChain"/>（与红缨共用），
///   本类只保留「锚点怎么接」与「怎么画」：
///   · 距离约束（每节长度固定）、链尾钉在玩家手、链头由外部驱动。
///
/// 用法：<c>ChainLinkRig.Create(...)</c> 生成 → <c>SetHeadTarget(pos)</c> 驱动链头 →
/// <c>SetTaut(bool)</c> 切状态 → <c>SetVisible(bool)</c> 显隐。
/// </summary>
[DefaultExecutionOrder(900)]
[DisallowMultipleComponent]
public sealed class ChainLinkRig : MonoBehaviour
{
    [Header("链节")]
    [Tooltip("链节贴图（占位用 Enemy2/chain.png；正式链子换成逐节模型时留空即可）。")]
    [SerializeField] private Sprite linkSprite;
    [Tooltip("链节在世界里的长度（单位）。")]
    [SerializeField] private float segmentLength = 0.95f;
    [Tooltip("链节显示缩放。")]
    [SerializeField] private float linkScale = 3.4f;

    [Header("解算")]
    [Tooltip("弹簧硬度（越大越硬、越快贴住目标）。\n★ 实测：链头飞得很快，硬度不够就追不上（会差 2~4 个单位，断开）。900 能在绷紧阶段拉直。")]
    [SerializeField] private float stiffness = 900f;
    [Tooltip("阻尼（越大越快停）。\n★ 与硬度配套：临界阻尼 = 2·√硬度 ≈ 60 ⇒ 40 略欠阻尼（保留一点甩劲）。")]
    [SerializeField] private float damping = 40f;
    [Tooltip("重力下垂比例。")]
    [SerializeField] private float gravityScale = 0.22f;

    private SpriteRenderer[] _renderers;
    private Transform[] _links;
    private Vector3 _tailAnchor;
    private Vector3 _headTarget;
    private bool _initialized;
    private bool _taut;
    private bool _visible = true;

    /// <summary>共用求解器（架构 ⑤）：积分核心在 <see cref="SpringBoneChain"/>，这里只接「链的锚点」。</summary>
    private readonly ChainSolver _solver = new ChainSolver();

    /// <summary>链头（镰刀 / 长枪占位）的位置由外部驱动。</summary>
    public Vector3 HeadTarget => _headTarget;

    /// <summary>链尾（玩家手）位置。</summary>
    public Vector3 TailAnchor => _tailAnchor;

    /// <summary>链是否绷紧（绷紧时链身拉直、不下垂）。</summary>
    public bool IsTaut => _taut;

    /// <summary>
    /// 生成一条链。返回 null 表示生成失败（没有链节贴图也不影响逻辑，只是不可见）。
    /// </summary>
    public static ChainLinkRig Create(Transform parent, Sprite linkSprite, ChainLinkSettings settings)
    {
        if (parent == null) return null;
        var go = new GameObject("ChainLinkRig");
        go.transform.SetParent(parent, false);
        var rig = go.AddComponent<ChainLinkRig>();
        rig.linkSprite = linkSprite;
        rig.segmentLength = Mathf.Max(0.05f, settings.SegmentLength);
        rig.linkScale = Mathf.Max(0.05f, settings.LinkScale);
        rig.stiffness = Mathf.Max(0f, settings.Stiffness);
        rig.damping = Mathf.Max(0f, settings.Damping);
        rig.gravityScale = Mathf.Clamp01(settings.GravityScale);
        rig.Initialize(Mathf.Clamp(settings.SegmentCount, 3, 24));
        return rig;
    }

    private void Initialize(int segmentCount)
    {
        if (_initialized) return;
        _initialized = true;
        int n = segmentCount;
        _links = new Transform[n];
        _renderers = new SpriteRenderer[n];

        // ★ 链节贴图要**裁出链节本体**：chain.png 的链节只占 256² 画布中下的一小块，
        //   直接用整张图 ⇒ 每个链节只有几个像素大，链子等于没画。
        Sprite usable = CropLinkSprite(linkSprite);

        for (int i = 0; i < n; i++)
        {
            var link = new GameObject($"Link{i:00}");
            link.transform.SetParent(transform, false);
            _links[i] = link.transform;
            if (usable != null)
            {
                var sr = link.AddComponent<SpriteRenderer>();
                sr.sprite = usable;
                sr.sortingLayerName = "Default";
                sr.sortingOrder = 100;   // ★ 链身要压在敌人精灵之上
                _renderers[i] = sr;
            }
        }

        _solver.Owner = this;
        _solver.Initialize(n);
    }

    /// <summary>把链节从大画布里裁出来（只保留链节本体），否则链节小到看不见。</summary>
    private static Sprite CropLinkSprite(Sprite src)
    {
        if (src == null) return null;
        var tex = src.texture;
        if (tex == null) return src;
        // chain.png：链节大致在画布的 x 20%~78%、y 18%~58%（y 向上）
        float w = tex.width * 0.58f;
        float h = tex.height * 0.40f;
        float x = tex.width * 0.20f;
        float y = tex.height * 0.18f;
        var rect = new Rect(x, y, w, h);
        var cropped = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect);
        cropped.name = src.name + "_LinkCrop";
        return cropped;
    }

    private void Awake() { /* ⚠ 不能在这里 Initialize：AddComponent 会立刻触发 Awake，
                             *   那时 linkSprite 还没赋值 ⇒ 会把链身初始化成空。
                             *   初始化改由 Create() 在赋值之后触发。 */ }

    /// <summary>设置链尾（玩家手）与链头初始位置。</summary>
    public void Setup(Vector3 tail, Vector3 head)
    {
        if (!_initialized) return;
        _tailAnchor = tail;
        _headTarget = head;
        int n = _links.Length;
        var positions = new Vector3[n];
        for (int i = 0; i < n; i++)
            positions[i] = Vector3.Lerp(tail, head, (float)i / Mathf.Max(1, n - 1));
        _solver.ResetTo(positions);
        for (int i = 0; i < n; i++)
            if (_links[i] != null) _links[i].position = positions[i];
    }

    /// <summary>更新链尾（玩家手）位置。</summary>
    public void SetTail(Vector3 tail) { _tailAnchor = tail; }

    /// <summary>更新链头目标（枪口 / 镰刀）。</summary>
    public void SetHeadTarget(Vector3 head) { _headTarget = head; }

    /// <summary>绷紧（拉直）/ 松弛（下垂甩动）。</summary>
    public void SetTaut(bool taut) { _taut = taut; }

    public void SetVisible(bool visible)
    {
        if (_visible == visible) return;
        _visible = visible;
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null) _renderers[i].enabled = visible;
    }

    private void LateUpdate()
    {
        if (!_initialized) return;
        Simulate(Time.deltaTime);
    }

    /// <summary>链求解：距离约束 + 弹簧 + 重力（积分核心在共用的 <see cref="SpringBoneChain"/>）。</summary>
    public void Simulate(float deltaTime)
    {
        if (!_initialized || _links == null || _links.Length < 2) return;
        if (deltaTime <= 0f) return;

        _solver.Owner = this;
        _solver.Stiffness = stiffness;
        _solver.Damping = damping;
        _solver.GravityScale = _taut ? 0f : gravityScale;   // 绷紧时不垂
        _solver.MaxSpeedRatio = 80f;
        // 与红缨同一套时间分辨率（120Hz 子步进）⇒ 两条链的手感参数语义一致
        _solver.Substeps = Mathf.Clamp(Mathf.CeilToInt(deltaTime / (1f / 120f)), 1, 12);
        _solver.Step(deltaTime);

        // 写回骨骼：位置取中点、朝向指向下一节
        int n = _links.Length;
        for (int i = 0; i < n; i++)
        {
            if (_links[i] == null) continue;
            Vector3 a = _solver.GetPosition(i);
            Vector3 b = (i + 1 < n) ? _solver.GetPosition(i + 1) : _headTarget;
            _links[i].position = (a + b) * 0.5f;
            Vector3 dir = b - a;
            if (dir.sqrMagnitude > 0.000001f)
                _links[i].rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            if (_renderers[i] != null && _renderers[i].sprite != null)
            {
                // ★ 链环**固定尺寸**、不按节长拉伸 ⇒ 读成“一节一节的链环”；
                //   拉伸会把链环拉成一条黑杠（实测）。
                float s = linkScale * 0.5f;
                _renderers[i].transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    /// <summary>
    /// 铁链专用的求解器适配层：把「链的锚点」接进共用积分核心。
    /// 只做「怎么接」，不做物理（物理在 <see cref="SpringBoneChain"/>）。
    /// </summary>
    private sealed class ChainSolver : SpringBoneChain
    {
        /// <summary>宿主组件（求解器要读它的链尾 / 链头）。</summary>
        public ChainLinkRig Owner;

        protected override void BeforeStep()
        {
            // 链尾钉在玩家手
            if (Count > 0) _positions[0] = Owner._tailAnchor;
        }

        protected override bool ComputeNode(int index, out Vector3 origin, out Vector3 target)
        {
            int n = Count;
            if (index == 0)
            {
                // 链尾：钉死，不参与弹簧求解
                origin = Owner._tailAnchor;
                target = origin;
                return false;
            }
            if (index == n - 1)
            {
                // 链头：由外部驱动（锁镰位置）—— 不做硬长度约束，
                //   这样链身才能伸长去够远处的锁镰（甩出阶段链是直的）。
                origin = _positions[index - 1];
                target = Owner._headTarget;
                return true;
            }
            // 中间节：距离约束（每节长度固定）
            //   ★ 方向取「**链头方向**」（当前节 → 链头），而不是「当前朝向」。
            //     用当前朝向的话，链一上来塌在一点（钩子还在手上）就永远拉不开
            //     —— 那正是之前「链身像几块碎片」的根因。
            origin = _positions[index - 1];
            Vector3 dir = Owner._headTarget - origin;
            if (dir.sqrMagnitude <= 0.000001f) dir = Owner._headTarget - Owner._tailAnchor;
            if (dir.sqrMagnitude <= 0.000001f) dir = Vector3.down;
            else dir.Normalize();
            target = origin + dir * Owner.segmentLength;
            return true;
        }

        protected override Vector3 FallbackDirection(int index, Vector3 origin, Vector3 target)
            => target - origin;

        /// <summary>链用**软**距离约束（目标是「固定节长」的那个点，但落点仍可被弹簧拉过）⇒ 链能伸长。</summary>
        protected override float NodeLength(int index) => 0f;

        protected override float MaxSpeed(int index) => MaxSpeedRatio * Owner.segmentLength;

        /// <summary>
        /// 链的**写回**不逐节做：每一节画的是「本节 → 下一节」的**中点**（一个链环跨在两节之间）
        /// ⇒ 统一在 <see cref="ChainLinkRig.Simulate"/> 里一次写完，这里什么都不做。
        /// </summary>
        protected override void ApplyNode(int index, Vector3 position, Vector3 direction) { }
    }
}
