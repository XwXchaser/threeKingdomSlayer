using UnityEngine;

/// <summary>
/// 路线物件跟随器：把「玩法物件」（可破坏补给之类）放进 Y 场景的滚动世界里。
///
/// <para><b>为什么需要它</b>：Y 场景的滚动是**着色器**做的（<c>CurvedScroll/Y Sample Scenery</c>
/// 等，见 <c>Assets/Experiments/CurvedScroll/Shaders/YScrollScenery.shader</c>）——
/// 顶点里用 <c>_YSPose</c>（旅行点）把物件锚点旋转平移，于是**相机不动、世界在动</c>。
/// 任何不挂滚动材质的物件都会因此被钉在相机正前方（历史上那只木箱就是这样：
/// 落地页、每个路线节点都挡在路中央，打碎后残骸还跟着镜头跑）。</para>
///
/// <para><b>本组件做什么</b>：在 CPU 侧复刻同一套锚点投影（与 shader 的
/// <c>q = anchor.xz - _YSPose.xz</c> → 按航向旋转 → <c>TransformPoint</c> 一致），
/// 把物件放到正确的世界位置：<b>transform 保持真实</b>，于是命中判定、距离判定、
/// 描边高亮都能直接用世界坐标，不必再猜。</para>
///
/// <para><b>可见性归属</b>：本组件<b>独占</b> <c>renderer.enabled</c>（远离相机时隐藏），
/// <c>YRouteStreamingWindow</c> 会跳过带本组件的渲染器。两套系统抢同一个
/// <c>renderer.enabled</c> 是「已破坏的物件复活成残骸」的历史成因，必须保持单一所有者。</para>
///
/// <para><b>编辑期</b>：只在 Play 模式跑投影（编辑期保持 authored 位置，避免每帧改脏场景）。
/// authored 位置 = 进度 0 时的投影位置，二者在起点一致。</para>
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RoutePropFollower : MonoBehaviour
{
    [Tooltip("留空则自动取父级链上的 YScrollSample")]
    public YScrollSample route;

    [Tooltip("物件在路线根节点下的 authored 锚点（局部坐标）。运行时位置由投影算出，不读 transform。")]
    public Vector3 localAnchor = Vector3.zero;

    [Tooltip("朝向是否跟随路线航向（滚动 shader 本身不旋转精灵，只平移锚点）")]
    public bool followHeading;

    [Tooltip("绕 Y 轴的额外朝向偏移（度）")]
    public float yawOffset;

    [Header("可见性（按投影后的路线坐标判定）")]
    [Tooltip("投影后 z 低于该值 = 已到相机身后 → 隐藏")]
    public float hideBehindZ = -13f;

    [Tooltip("投影后 z 高于该值 = 太远 → 隐藏")]
    public float hideBeyondZ = 82f;

    private SpriteRenderer _renderer;
    private bool _resolved;

    /// <summary>投影后的路线坐标 z（相对旅行点）：负 = 相机身后，越大越远。</summary>
    public float ProjectedZ { get; private set; }

    /// <summary>投影后的世界坐标（最近一次计算）。</summary>
    public Vector3 ProjectedWorldPosition { get; private set; }

    private void Resolve()
    {
        if (_resolved) return;
        _resolved = true;
        if (route == null) route = GetComponentInParent<YScrollSample>();
        _renderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _resolved = false;
        Resolve();
    }

    private void OnValidate()
    {
        _resolved = false;
        Resolve();
    }

    private void LateUpdate()
    {
        Resolve();
        if (route == null) return;
        if (!Application.isPlaying) return; // 编辑期保持 authored 位置，不改脏场景
        ApplyProjection();
    }

    /// <summary>按滚动 shader 的同一套数学算位置并落地（与 YScrollScenery.shader:11-19 对齐）。</summary>
    public void ApplyProjection()
    {
        if (route == null) return;

        Vector3 pose = route.TravelPosition;   // 路线根局部空间里的旅行点
        // ★ <see cref="YScrollSample.TravelAngle"/> 是**度**（YScrollSample 内部一律用度，喂给 shader 前才转弧度）。
        //   这里必须先转成弧度再喂给 Sin/Cos，否则航向 ≠ 0 时（走过 J1 弯、或第 2 圈起航向就带上了
        //   本圈净转角）会把「度」当弧度算 ⇒ 物件横向偏出去几十米（实测 E0 宝箱偏 23 米）。
        float angle = route.TravelAngle * Mathf.Deg2Rad;
        float s = Mathf.Sin(angle);
        float c = Mathf.Cos(angle);

        // ★ **按圈摆放**：无限卷轴时同一个补给点每圈都会再出现一次。
        //   localAnchor 是**第 0 圈**的局部坐标 ⇒ 先套本圈的刚体帧再投影，否则第二圈起
        //   物件会全部落在起点附近（玩家走过去就是“补给没了”）。第 0 圈帧为恒等 ⇒ 旧表现不变。
        Vector3 anchor = route.GetLapFrame(route.CurrentLap).Apply(localAnchor);

        float qx = anchor.x - pose.x;
        float qz = anchor.z - pose.z;
        float ax = c * qx - s * qz;
        float az = s * qx + c * qz;
        float ay = anchor.y;

        // 远景下沉：与 shader 的 anchor.y -= .009*d*d*smoothstep(0,6,d) 保持一致。
        // 注意：**不能直接用 Mathf.SmoothStep(0,6,d)** —— Unity 的 Mathf.SmoothStep 是
        // “把 t 夹到 [0,1] 再在 from→to 之间插值”，d>to 时会返回 to(=6)；
        // 而 GLSL 的 smoothstep(0,6,d) 返回 1。这里手写 GLSL 语义。
        float d = Mathf.Max(0f, az - 18f);
        float t = Mathf.Clamp01(d / 6f);
        float smooth = t * t * (3f - 2f * t);
        ay -= 0.009f * d * d * smooth;

        ProjectedZ = az;
        Vector3 world = route.transform.TransformPoint(new Vector3(ax, ay, az));
        ProjectedWorldPosition = world;
        transform.position = world;

        if (followHeading)
            transform.rotation = route.transform.rotation * Quaternion.Euler(0f, angle * Mathf.Rad2Deg + yawOffset, 0f);
        else
            // ★ 不跟随航向 ⇒ **必须显式把世界旋转钉回路线根朝向**，不能让它继承父链。
            //   父链是节点锚点（`E0 Encounter` → `Encounter Authoring`），而锚点每圈被圈框架转 −45°
            //   ⇒ 圈 ≥ 1 时**只有这些补给/宝箱在自转**（shader 弯折的景物是故意不转的：
            //   它把精灵的局部顶点偏移加在旋转后的锚点上 ⇒ 画面永远朝行进方向）
            //   ⇒ 同一件箱子每圈朝向都不一样（实测世界旋转 0° → 315° → 270°，看上去就是“形象变形”）。
            //   钉回路线根朝向 ⇒ 与第 0 圈逐位一致，圈数再多也不变。
            transform.rotation = route.transform.rotation * Quaternion.Euler(0f, yawOffset, 0f);

        if (_renderer != null)
            _renderer.enabled = az > hideBehindZ && az < hideBeyondZ;
    }

    private void OnDrawGizmosSelected()
    {
        if (route == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(route.transform.TransformPoint(localAnchor), 0.3f);
        Gizmos.DrawLine(route.transform.TransformPoint(localAnchor), transform.position);
    }
}
