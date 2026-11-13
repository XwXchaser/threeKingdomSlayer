using UnityEngine;

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
/// 用法：<c>ChainLinkRig.Create(...)</c> 生成 → <c>SetHeadTarget(pos)</c> 驱动链头 →
/// <c>SetTaut(bool)</c> 切状态 → <c>SetVisible(bool)</c> 显隐。
/// </summary>
[DefaultExecutionOrder(900)]
[DisallowMultipleComponent]
public sealed class ChainLinkRig : MonoBehaviour
{
    [Header("链节")]
    [Tooltip("链节数量（= 骨骼数）。越多越细腻，也越贵。")]
    [Range(3, 24)] public int segmentCount = 9;
    [Tooltip("链节贴图（占位用 Enemy2/chain.png；正式链子换成逐节模型时留空即可）。")]
    [SerializeField] private Sprite linkSprite;
    [Tooltip("链节在世界里的长度（单位）。")]
    [Range(0.1f, 3f)] public float segmentLength = 0.95f;
    [Tooltip("链节显示缩放。")]
    [Range(0.05f, 6f)] public float linkScale = 3.4f;

    [Header("解算")]
    [Tooltip("弹簧硬度（越大越硬、越快贴住目标）。")]
    [Range(10f, 800f)] public float stiffness = 260f;
    [Tooltip("阻尼（越大越快停）。")]
    [Range(1f, 60f)] public float damping = 16f;
    [Tooltip("重力下垂比例。")]
    [Range(0f, 1f)] public float gravityScale = 0.22f;

    private SpriteRenderer[] _renderers;
    private Transform[] _links;
    private Vector3[] _positions;
    private Vector3[] _velocities;
    private Vector3 _tailAnchor;
    private Vector3 _headTarget;
    private bool _initialized;
    private bool _taut;
    private bool _visible = true;

    /// <summary>链头（镰刀 / 长枪占位）的位置由外部驱动。</summary>
    public Vector3 HeadTarget => _headTarget;

    /// <summary>链尾（玩家手）位置。</summary>
    public Vector3 TailAnchor => _tailAnchor;

    /// <summary>链是否绷紧（绷紧时链身拉直、不下垂）。</summary>
    public bool IsTaut => _taut;

    /// <summary>
    /// 生成一条链。返回 null 表示生成失败（没有链节贴图也不影响逻辑，只是不可见）。
    /// </summary>
    public static ChainLinkRig Create(Transform parent, Sprite linkSprite, int segments, float segmentLength)
    {
        if (parent == null) return null;
        var go = new GameObject("ChainLinkRig");
        go.transform.SetParent(parent, false);
        var rig = go.AddComponent<ChainLinkRig>();
        rig.linkSprite = linkSprite;
        rig.segmentCount = Mathf.Clamp(segments, 3, 24);
        rig.segmentLength = Mathf.Max(0.05f, segmentLength);
        rig.Initialize();
        return rig;
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        int n = segmentCount;
        _links = new Transform[n];
        _renderers = new SpriteRenderer[n];
        _positions = new Vector3[n];
        _velocities = new Vector3[n];

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
                             *   初始化改由 Create()/Setup() 在赋值之后触发。 */ }

    /// <summary>设置链尾（玩家手）与链头初始位置。</summary>
    public void Setup(Vector3 tail, Vector3 head)
    {
        Initialize();
        _tailAnchor = tail;
        _headTarget = head;
        for (int i = 0; i < _links.Length; i++)
        {
            _positions[i] = Vector3.Lerp(tail, head, (float)i / Mathf.Max(1, _links.Length - 1));
            _velocities[i] = Vector3.zero;
            if (_links[i] != null) _links[i].position = _positions[i];
        }
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

    /// <summary>链求解：距离约束 + 弹簧 + 重力。</summary>
    public void Simulate(float deltaTime)
    {
        Initialize();
        if (deltaTime <= 0f || _links == null || _links.Length < 2) return;

        int n = _links.Length;

        // ① 链头：由外部驱动（弹道由技能脚本给），链头带一点跟随惯性
        Vector3 head = _headTarget;
        _positions[n - 1] = Vector3.Lerp(_positions[n - 1], head, 1f - Mathf.Exp(-stiffness * 0.25f * deltaTime));
        _velocities[n - 1] = Vector3.zero;

        // ② 从链尾往链头推：每节被上一节拉向“固定长度”的位置
        for (int i = 1; i < n; i++)
        {
            Vector3 prev = _positions[i - 1];
            Vector3 dir = _positions[i] - prev;
            float len = dir.magnitude;
            Vector3 desired = prev + (len > 0.0001f ? dir / len : Vector3.down) * segmentLength;
            _positions[i] = Vector3.Lerp(_positions[i], desired, 1f - Mathf.Exp(-stiffness * deltaTime));
        }

        // ③ 链尾固定在玩家手
        _positions[0] = _tailAnchor;

        // ④ 松弛时：重力下垂 + 速度阻尼（绷紧时不垂）
        if (!_taut && gravityScale > 0f)
        {
            for (int i = 1; i < n - 1; i++)
                _positions[i] += Vector3.down * (gravityScale * segmentLength * 12f * deltaTime * deltaTime);
        }

        // ⑤ 速度用于阻尼（近似：位置差）
        for (int i = 1; i < n; i++)
        {
            Vector3 vel = (_positions[i] - _positions[i - 1]) * damping * deltaTime;
            if (i < n - 1) _positions[i] -= vel * 0.02f;
        }

        // ⑥ 写回骨骼：位置取中点、朝向指向下一节
        for (int i = 0; i < n; i++)
        {
            if (_links[i] == null) continue;
            Vector3 a = _positions[i];
            Vector3 b = (i + 1 < n) ? _positions[i + 1] : _headTarget;
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
}
