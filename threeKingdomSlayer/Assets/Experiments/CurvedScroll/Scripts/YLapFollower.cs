using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **按圈摆放的常驻物件**（无限卷轴）。
///
/// 场景里有几类东西不随段 prefab 装载，却必须跟着圈走：
///   · **地面**（<c>Terrain - one mesh, fork painted in terrain</c>）
///   · **节点连接片**（<c>Route Connections</c>）
///   · **节点遭遇摆位**（<c>Encounter Authoring</c>）与**远景地标**（<c>N1 Far Landmarks</c>）
///
/// 它们原本只静态摆一次 ⇒ 第二圈就缺物件。挂上本组件后：
///   · <see cref="copyInsteadOfMove"/> = false ⇒ **搬家**：用本圈的<b>刚体帧</b>摆放（位置 + 朝向）。
///   · <see cref="copyInsteadOfMove"/> = true  ⇒ **复制**：每圈一份，原件只当模板。
///     地面这类物件必须复制：一块地形的 mesh 只覆盖本圈一段，复制到前后圈，接缝后面才不会露洞。
///
/// ★ 第 0 圈帧恒为恒等；**无限卷轴关闭时本组件完全不动作**（第一关保持原样，原件保持可见）。
/// ⚠ 只能挂在**与 <see cref="YScrollSample"/> 同一父级**的物件上（帧在路线根空间里）。
/// </summary>
[DisallowMultipleComponent]
public sealed class YLapFollower : MonoBehaviour
{
    [Tooltip("留空则自动取父级上的 YScrollSample。")]
    public YScrollSample route;
    [Tooltip("留空则自动取 YScrollSample 同物体上的 YRouteStreamingWindow。")]
    public YRouteStreamingWindow window;

    [Tooltip("★ 复制而不是搬家。\n" +
             "地面这类「一块 mesh 只覆盖本圈一段」的物件必须开这个，否则接缝后面会露洞。\n" +
             "景物 / 节点布景这类真实物件保持关闭（搬家即可）。")]
    public bool copyInsteadOfMove = false;

    [Tooltip("复制模式下同时保留前后各几圈的副本；具体半径由地面或景物的可见范围决定。")]
    [Min(0)] public int copyLapRadius = 0;

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private bool _captured;
    private bool _originHidden;

    /// <summary>本组件负责搬家的渲染器，以及它们作者态的包围盒（离场时还原）。</summary>
    private Renderer[] _ownedRenderers;
    private Bounds[] _authoredBounds;

    private readonly Dictionary<int, GameObject> _copies = new Dictionary<int, GameObject>();

    /// <summary>地面副本各自的圈号与分支；其它渲染器不写入这些属性。</summary>
    private static readonly int LapIndexId = Shader.PropertyToID("_YSLapIndex");
    private static readonly int LapBranchId = Shader.PropertyToID("_YSLapBranchSign");

    /// <summary>可复用的 MaterialPropertyBlock（只写本组件负责的两个属性，不碰其它系统的值）。</summary>
    /// ⚠ 不能在字段初始化器里 `new`：Unity 禁止在 MonoBehaviour 构造期创建原生对象
    ///   （`Instantiate` 一个带本组件的物体时会直接抛 UnityException）。延迟创建。
    private MaterialPropertyBlock _lapBlock;
    private MaterialPropertyBlock LapBlock => _lapBlock ??= new MaterialPropertyBlock();

    private void Awake()
    {
        if (route == null) route = GetComponentInParent<YScrollSample>();
        if (window == null && route != null) window = route.GetComponent<YRouteStreamingWindow>();
        Capture();
    }

    private void OnEnable()
    {
        if (route != null)
        {
            route.OnLapChanged += OnLapChanged;
            route.OnLapFramesChanged += OnLapFramesChanged;
        }
        InflateCullingBounds();
    }

    private void OnDisable()
    {
        if (route != null)
        {
            route.OnLapChanged -= OnLapChanged;
            route.OnLapFramesChanged -= OnLapFramesChanged;
        }
        RestoreCullingBounds();
    }

    /// <summary>
    /// 玩家确定本圈支路后，刷新已缓存副本的圈帧和地面参数。
    /// 已确定的历史圈帧不变；未来预览帧可能变化。
    /// </summary>
    private void OnLapFramesChanged()
    {
        if (route == null || !route.infiniteScroll) return;
        foreach (var pair in _copies)
        {
            var copy = pair.Value;
            if (copy == null) continue;
            PlaceCopy(copy.transform, pair.Key);
            ApplyLapBlock(copy, pair.Key);
            if (window != null) window.SetTrackedDistance(copy.GetComponentsInChildren<Renderer>(true), pair.Key);
        }
    }

    /// <summary>
    /// **关掉 Unity 的视锥剔除**。
    ///
    /// 这套滚动是「假滚动」：画面由 shader 弯折，物件的**真实 transform** 会随圈数越搬越远
    /// （第 3 圈已在 1000 米外），而相机本体始终在原点附近 ⇒ Unity 按真实包围盒判断「在不在镜头里」，
    /// 圈 ≥ 1 就把整组当成镜头外**整批剔除** ⇒ 屏幕上什么都没有（偏偏 <c>Renderer.enabled</c> 还是 true）。
    ///
    /// 修法与本场景既有约定一致（<see cref="YScrollSample"/> 在 Before 里对 <c>scenery</c> 就是这么干的）：
    /// 把包围盒撑到 10000 米。
    ///
    /// **为什么放在自己身上**：搬家的执行者顺带声明「我的物体不能按真实位置剔除」——
    /// 不需要任何序列化名单 ⇒ 场景零膨胀、新增物件自动生效、组件关掉就还原。
    /// 只跑一次，不是每帧。
    /// </summary>
    private void InflateCullingBounds()
    {
        if (_ownedRenderers == null)
        {
            _ownedRenderers = GetComponentsInChildren<Renderer>(true);
            _authoredBounds = new Bounds[_ownedRenderers.Length];
        }
        for (int i = 0; i < _ownedRenderers.Length; i++)
        {
            var renderer = _ownedRenderers[i];
            if (renderer == null) continue;
            _authoredBounds[i] = renderer.localBounds;
            renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }
    }

    private void RestoreCullingBounds()
    {
        if (_ownedRenderers == null) return;
        for (int i = 0; i < _ownedRenderers.Length; i++)
        {
            var renderer = _ownedRenderers[i];
            if (renderer != null) renderer.localBounds = _authoredBounds[i];
        }
    }

    private void Capture()
    {
        _baseLocalPosition = transform.localPosition;
        _baseLocalRotation = transform.localRotation;
        _captured = true;
    }

    /// <summary>
    /// 圈变了 ⇒ ① 世界摆放 ② **同步窗口里的沿路距离**
    /// （否则会被当成「还在上一圈」而淡出 ⇒ 第二圈看不见）。
    /// </summary>
    private void OnLapChanged(int lap)
    {
        if (route == null) return;
        if (window == null) window = route.GetComponent<YRouteStreamingWindow>();
        if (!route.infiniteScroll) return;

        // ★ 顺序很关键：**先套本圈刚体帧，再按当前位置反算沿路距离**。
        //   OnLapChanged 是在 YScrollSample.Update 里触发的，而本组件的摆放原本只在 LateUpdate 做
        //   ⇒ 触发这一刻 transform 还是**上一圈**的世界位置 ⇒ 反算出来的沿路距离全部塌成 0
        //   （实测 386 / 422 / 391 会变成 380 / 380 / 380）⇒ 圈一起的距离账全错，物件会被提前淡出。
        ApplyLapPlacement(lap);
        if (window == null) return;
        // ★ 按**当前圈**重算（幂等），不用增量：圈号可能一下跳很多圈，增量漏一次就永久错位。
        window.SetTrackedDistance(GetComponentsInChildren<Renderer>(true), lap);
    }

    /// <summary>把本圈刚体帧套到 transform 上（搬家模式）。复制模式不动原件（原件只当模板）。</summary>
    private void ApplyLapPlacement(int lap)
    {
        if (copyInsteadOfMove) return;
        // ★ **刚体帧**：转向是「地图在转」⇒ 本圈物件必须带着累积旋转一起摆。
        var frame = route.GetLapFrame(lap);
        transform.localPosition = frame.Apply(_baseLocalPosition);
        transform.localRotation = frame.Rotation * _baseLocalRotation;
    }

    private void LateUpdate()
    {
        if (!_captured) Capture();
        if (route == null) return;

        // ★ 无限卷轴关闭 ⇒ **完全不动**，并恢复原件可见（第一关保持原样）
        if (!route.infiniteScroll)
        {
            if (_originHidden) SetOriginVisible(true);
            SetCopiesVisible(false);
            return;
        }

        int lap = route.CurrentLap;

        if (copyInsteadOfMove)
        {
            if (!_originHidden) SetOriginVisible(false);
            EnsureCopies(lap);
            return;
        }

        // ★ **刚体帧**：转向是「地图在转」⇒ 本圈物件必须带着累积旋转一起摆。
        ApplyLapPlacement(lap);
    }

    /// <summary>原件可见性（复制模式下原件只当模板）。副本是 runtime 生成的，不会被误关。</summary>
    private void SetOriginVisible(bool visible)
    {
        _originHidden = !visible;
        var renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;
            if (IsUnderAnyCopy(r.transform)) continue;
            r.enabled = visible;
        }
    }

    private bool IsUnderAnyCopy(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            if (p == transform) return false;
            foreach (var pair in _copies)
                if (pair.Value != null && pair.Value.transform == p) return true;
        }
        return false;
    }

    private void SetCopiesVisible(bool visible)
    {
        foreach (var pair in _copies)
            if (pair.Value != null && pair.Value.activeSelf != visible) pair.Value.SetActive(visible);
    }

    /// <summary>保证 [lap - radius, lap + radius] 各有一份副本，范围外的关掉（不销毁，避免卡顿）。</summary>
    private void EnsureCopies(int lap)
    {
        int from = Mathf.Max(0, lap - copyLapRadius);
        int to = lap + copyLapRadius;

        foreach (var pair in _copies)
        {
            bool wanted = pair.Key >= from && pair.Key <= to;
            if (pair.Value != null && pair.Value.activeSelf != wanted) pair.Value.SetActive(wanted);
        }

        for (int k = from; k <= to; k++)
        {
            if (_copies.TryGetValue(k, out var existing) && existing != null)
            {
                if (!existing.activeSelf) existing.SetActive(true);
                continue;
            }
            _copies[k] = CreateCopy(k);
        }
    }

    /// <summary>按第 <paramref name="lap"/> 圈的刚体帧摆一份副本（创建与重摆共用）。</summary>
    private void PlaceCopy(Transform target, int lap)
    {
        if (target == null || route == null) return;
        var frame = route.GetLapFrame(lap);
        target.localPosition = frame.Apply(_baseLocalPosition);
        target.localRotation = frame.Rotation * _baseLocalRotation;
    }

    /// <summary>
    /// 写入地面副本的圈号与本圈支路，并保留该渲染器上其它系统的属性。
    /// 本圈起点由本地 z=0 界定，终点使用本圈支路的实际落点和切线。
    /// </summary>
    private void ApplyLapBlock(GameObject target, int lap)
    {
        if (target == null || route == null) return;
        float branchSign = route.BranchOfLap(lap) ? 1f : -1f;
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null) continue;
            var material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty(LapIndexId)) continue;
            // ★ 先取回既有 block 再合入：别把别人（可能有）的属性抹掉。
            var block = LapBlock;
            renderer.GetPropertyBlock(block);
            block.SetFloat(LapIndexId, lap);
            block.SetFloat(LapBranchId, branchSign);
            renderer.SetPropertyBlock(block);
        }
    }

    private GameObject CreateCopy(int lap)
    {
        var copy = Instantiate(gameObject, transform.parent, false);
        copy.name = gameObject.name + " (Slice c" + lap + ")";
        PlaceCopy(copy.transform, lap);

        // 副本上的 Follower 不要再管它
        var follower = copy.GetComponent<YLapFollower>();
        if (follower != null) follower.enabled = false;

        // 副本的渲染器一律启用（原件可能因为「当模板」被隐藏）
        // ★ 包围盒也要重新撑：副本上的 Follower 刚被关掉，它的 OnDisable 会把包围盒**还原回去**，
        //   而副本是摆在「第 k 圈」坐标上的（第 3 圈已在 1000 米外）⇒ 不撑就会被视锥剔除
        //   （实测：地形副本 lap0/lap1 isVisible=1，lap2 掉成 0）。
        var renderers = copy.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].enabled = true;
            renderers[i].localBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);
        }
        ApplyLapBlock(copy, lap);
        return copy;
    }
}
