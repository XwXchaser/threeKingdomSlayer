using UnityEngine;

/// <summary>Isolated Y junction proof. Authored transforms remain flat; projection is camera scoped.</summary>
[ExecuteAlways]
public sealed class YScrollSample : MonoBehaviour
{
    public Camera viewCamera;
    public Renderer[] scenery = new Renderer[0];
    public Transform travelMarker;
    public bool right;
    [Range(0,1)] public float progress;
    [Range(15,65)] public float turnAngle = 45;
    public bool animate;
    /// <summary>
    /// **无限循环模式**（2026-11-19）：为 true 时，自动前进不会在 <c>progress == 1</c> 处停住
    /// （卷轴一直往前展开）。无尽模式用它。
    /// </summary>
    public bool endlessLoop = true;
    [Min(1)] public float duration = 10;
    /// <summary>
    /// 旧版路线驱动（已收进 Assets/Scripts/Legacy）。这里只把它当作「动画由谁接管」的标记：
    /// 旧驱动不在时本脚本自己推进 progress。类型用 Component，避免运行时代码依赖 Legacy 程序集。
    /// </summary>
    public Component battleHost;
    [Header("Route visual groups")]
    public Transform leftBranchVisuals;
    public Transform rightBranchVisuals;
    public Transform leftBranchEncounter;
    public Transform rightBranchEncounter;
    public Transform junctionVisuals;
    [Header("Inspector-authored logical node anchors")]
    public ScrollRouteNodePresentation[] nodePresentations = System.Array.Empty<ScrollRouteNodePresentation>();
    Bounds[] originalBounds;
    float angle;
    Vector3 position;
    public Vector3 TravelPosition => position;
    /// <summary>路线朝向（**度**）。喂给三角函数前要先转弧度。</summary>
    public float TravelAngle => angle;

    public const float JunctionDistance = 190f;
    public const float CurveRadius = 28.1595573f;
    /// <summary>分叉后的尾段长度（米）。**113 = 原值**（第一圈的几何一点不变）。</summary>
    public const float ExitTailDistance = 113f;
    public static float GetRouteLength(float degrees)
    {
        return JunctionDistance + CurveRadius * Mathf.Max(0f, degrees) * Mathf.Deg2Rad
             + ExitTailDistance;
    }

    // ── 圈框架（新框架的唯一新增概念，见 YRouteFrame）────────────────────────
    //   旧实现是「每圈沿 +z 平移」⇒ 要求每圈净转角为 0；而这条路转 45° 不回正
    //   ⇒ 接缝必然回摆。新实现改成「每圈继承上一圈的刚体变换」⇒ 地图真的在转，
    //   人一直在直走 ⇒ 接缝处位置、朝向同时跨一步 ⇒ 连续。

    /// <summary>
    /// **沿路距离 → 位置**（距离**无上限**）。
    ///
    /// ★ 2026-11-19：**不再取模、不再平移** —— 尾段沿原方向**无限延伸**。
    ///   这是“卷轴连贯”的前提：位置是沿路距离的**连续函数**，
    ///   所以内容按沿路距离重复摆上去时，**接缝天然对齐、不会跳变**。
    ///   （旧的“每圈平移一段”做法会让沿路长度 ≠ 世界位移 ⇒ 每圈必然错位一下。）
    /// </summary>
    public static Vector3 Evaluate(float distance, float degrees, bool right, out float heading)
    {
        return EvaluateLap(distance, degrees, right, out heading);
    }

    /// <summary>一圈之内（沿路 0..尾段末端）的形状位置；**尾段无限延伸**。</summary>
    private static Vector3 EvaluateLap(float distance, float degrees, bool right, out float heading)
    {
        // The N1 -> E0 -> J1 lane reaches the fork before either branch turns.
        const float junction=JunctionDistance, radius=CurveRadius;
        float a=degrees*Mathf.Deg2Rad, sign=right?1:-1;
        if(distance<=junction){heading=0;return new Vector3(0,0,distance);}
        float arcLen=radius*a;
        float curve=Mathf.Min(distance-junction,arcLen);
        float t=curve/radius;
        // ★ 朝向：弧上**渐进转**（t 随弧长增长），到尾段自然饱和在 ±a ⇒ 连续无突变。
        //   ⚠ 不要在这里无条件改成尾段方向 —— 那会把 J1 弧上的转向变成“瞬间 45°”（2026-11-19 修）。
        heading=sign*t*Mathf.Rad2Deg;
        Vector3 p=new Vector3(sign*radius*(1-Mathf.Cos(t)),0,junction+radius*Mathf.Sin(t));
        // 尾段：沿切线方向**无限延伸**（距离无上限 ⇒ 位置是沿路距离的连续函数）
        float tail=Mathf.Max(0,distance-junction-arcLen);
        Vector3 dir=new Vector3(sign*Mathf.Sin(a),0,Mathf.Cos(a));
        return p+dir*tail;
    }

    /// <summary>
    /// **无限卷轴开关**。
    ///
    /// ★ **默认关** ⇒ 第一关完全保持原样（不复制内容、不旋转、不改归一化）。
    ///
    /// 打开后：沿路距离无上限增长，内容按 <see cref="flatCycleLength"/> 一圈一圈重复，
    /// 每圈用 <see cref="GetLapFrame"/> 的**刚体帧**摆放（位置 + 朝向）。
    /// ⇒ 圈边界上内容与相机同时跨一步、同时转一度 ⇒ **接缝连续、不跳、不倒退**。
    /// </summary>
    public bool infiniteScroll = false;

    /// <summary>
    /// **一圈的长度**（米）= 一圈内容的总长（切片序列里所有切片长度之和）。
    /// </summary>
    public float flatCycleLength = 0f;

    /// <summary>
    /// **进度归一化用的长度**。
    /// 关闭时 = 路径形状长度（第一关原样）；打开时 = 一圈长度（<see cref="flatCycleLength"/>）。
    /// </summary>
    public float Length => (infiniteScroll && flatCycleLength > 0f) ? flatCycleLength : GetRouteLength(turnAngle);

    /// <summary>第 k 圈的摆放帧缓存（按需往后长）。</summary>
    private readonly System.Collections.Generic.List<YRouteFrame> lapFrames = new System.Collections.Generic.List<YRouteFrame>();
    private float frameCacheCycle = -1f;
    private float frameCacheAngle = float.NaN;
    private bool frameCacheRight;

    /// <summary>一圈走下来，内容相对上一圈的**增量帧**（位置 = 本圈末端的局部落点，朝向 = 本圈净转角）。</summary>
    private YRouteFrame lapStep;

    /// <summary>
    /// 第 <paramref name="lap"/> 圈的摆放帧。**第 0 圈恒为 <see cref="YRouteFrame.Identity"/>**
    /// ⇒ 旧路线、旧场景表现不受影响。
    /// </summary>
    public YRouteFrame GetLapFrame(int lap)
    {
        EnsureLapFrames();
        if (lap < 0) lap = 0;
        while (lapFrames.Count <= lap)
            lapFrames.Add(lapFrames[lapFrames.Count - 1].Compose(lapStep));
        return lapFrames[lap];
    }

    void EnsureLapFrames()
    {
        float cycle = Mathf.Max(0.1f, flatCycleLength > 0f ? flatCycleLength : GetRouteLength(turnAngle));
        if (lapFrames.Count > 0 && Mathf.Approximately(frameCacheCycle, cycle)
            && Mathf.Approximately(frameCacheAngle, turnAngle) && frameCacheRight == right) return;

        frameCacheCycle = cycle;
        frameCacheAngle = turnAngle;
        frameCacheRight = right;

        // 增量帧 = 本圈内容末端 → 下一圈内容原点：
        //   位置 = 本圈末端在局部空间的落点（相机开到那里时脚下就是它）
        //   朝向 = 本圈净转角（右路 +turnAngle / 左路 −turnAngle）
        float startHeading, endHeading;
        var start = Evaluate(0f, turnAngle, right, out startHeading);
        var end = Evaluate(cycle, turnAngle, right, out endHeading);
        lapStep = new YRouteFrame
        {
            position = end - start,
            heading = endHeading - startHeading
        };

        lapFrames.Clear();
        lapFrames.Add(YRouteFrame.Identity);
    }

    /// <summary>沿路距离属于第几圈（无限卷轴关闭时恒为 0）。</summary>
    public int LapOf(float distance)
    {
        if (!infiniteScroll || flatCycleLength <= 0f) return 0;
        return Mathf.FloorToInt(distance / flatCycleLength);
    }

    /// <summary>
    /// 沿路距离（无上限）→ 路线空间位置与相机朝向。
    ///
    /// ★ <c>position = frame_k · Evaluate(local)</c>，<c>朝向 = 局部朝向 + frame_k.heading</c>。
    ///   第 0 圈 frame 为恒等 ⇒ 与旧实现逐像素一致。
    /// </summary>
    public void EvaluateRoute(float distance, out Vector3 routePosition, out float heading, out int lap)
    {
        if (infiniteScroll && flatCycleLength > 0f)
        {
            float cycle = flatCycleLength;
            lap = Mathf.FloorToInt(distance / cycle);
            float local = distance - lap * cycle;
            var frame = GetLapFrame(lap);
            routePosition = frame.Apply(Evaluate(local, turnAngle, right, out float localAngle));
            heading = localAngle + frame.heading;
            return;
        }
        lap = 0;
        routePosition = Evaluate(distance, turnAngle, right, out heading);
    }

    /// <summary>
    /// **当前是第几圈**（0 起）。
    ///
    /// ★ 2026-11-19 无限循环：节点锚点、节点布景（锚点的子物体）、节点连接片
    ///   都要**跟着圈走**，否则第二圈就缺物件。
    /// </summary>
    public int CurrentLap { get; private set; }

    /// <summary>圈号变了（事件）：跟随者用它重新定位。</summary>
    public event System.Action<int> OnLapChanged;

    /// <summary>
    /// 取某个节点**在哪条支路上**（按它自己的锚点方向）。
    ///
    /// ★ 这是「走哪条路」的唯一权威（2026-11-19）：
    ///   旧口径是「from.outgoingChoices 的第几个出口」⇒ 分叉点的节点资产里
    ///   只挂了一条路时（另一条挂在路线的 extraChoices 上），计数 ≤ 1 就放弃判定
    ///   ⇒ 永远落在左路 ⇒ **走右路也渲染成左边的村庄**。
    ///   改成看**目标节点自己的锚点方向**（Left / Right / Forward）就与出口顺序无关了。
    ///
    /// 匹配规则与 <see cref="TryGetNodeProgress"/> 一致：先按节点资产精确匹配，
    /// 匹配不到再按 nodeId 匹配（教学关与正式关同 id 同位）。
    /// </summary>
    public bool TryGetNodeDirection(FakeRouteNodeConfig node, out ScrollRouteDirection direction)
    {
        direction = ScrollRouteDirection.Forward;
        if (!node || nodePresentations == null) return false;

        ScrollRouteNodePresentation match = null;
        foreach (var presentation in nodePresentations)
        {
            if (!presentation || !presentation.node) continue;
            if (presentation.node != node) continue;
            if (match) return false;   // 同一节点匹配到多处 = 歧义
            match = presentation;
        }
        if (!match && !string.IsNullOrEmpty(node.nodeId))
        {
            foreach (var presentation in nodePresentations)
            {
                if (!presentation || !presentation.node || presentation.node.nodeId != node.nodeId) continue;
                if (match) return false;
                match = presentation;
            }
        }
        if (!match) return false;
        direction = match.direction;
        return true;
    }
    public bool TryGetNodeProgress(FakeRouteNodeConfig node, out float normalizedProgress)
    {
        normalizedProgress = 0f;
        if (!node || nodePresentations == null) return false;
        ScrollRouteNodePresentation match = null;
        foreach (var presentation in nodePresentations)
        {
            if (!presentation || presentation.node != node) continue;
            if (match) return false;
            match = presentation;
        }
        // 兜底：场景锚点绑定的是具体节点资产。另有一份配置在同一条路上复用相同 nodeId 的节点时
        // （教学关 N1/E0 与正式关卡同位），允许按 nodeId 解析到同一个锚点；同 id 匹配到多处仍视为歧义。
        if (!match && !string.IsNullOrEmpty(node.nodeId))
        {
            foreach (var presentation in nodePresentations)
            {
                if (!presentation || !presentation.node || presentation.node.nodeId != node.nodeId) continue;
                if (match) return false;
                match = presentation;
            }
        }
        // 0.2 m tolerance: EV/N3 anchors are authored in metres and the N3 anchor
        // legitimately sits 0.116455 m past the computed end of the route.
        if (!match || float.IsNaN(match.worldDistance) || match.worldDistance < 0f
            || match.worldDistance > Length + 0.2f) return false;

        // ★ **周期化**：目标一律取「当前沿路距离**之后**最近的那一次出现」⇒ 永不倒退。
        //   一圈长度用 <see cref="Length"/>（无限卷轴时 = 一圈内容长度）。
        //   ⚠ 这里以前写死 445，与数据（切片序列总长）不一致 ⇒ 节点与相机按两个周期走。
        float lap = Mathf.Max(0.1f, Length);
        float w = match.worldDistance;
        float current = progress * Length;
        float k = Mathf.FloorToInt((current - w) / lap);
        if (k * lap + w < current - 0.001f) k += 1f;   // 取第一个 ≥ current 的出现
        float target = k * lap + w;
        // 归一化用 Length（progress 可能 >1 ⇒ 相机继续沿路往前走，不会倒退）
        normalizedProgress = target / Mathf.Max(0.1f, Length);
        return true;
    }
    void OnEnable(){Camera.onPreCull+=Before;Camera.onPostRender+=After;}
    void OnDisable(){Camera.onPreCull-=Before;Camera.onPostRender-=After;Restore();}
    void Update()
    {
        if(Application.isPlaying&&animate&&battleHost==null)
        {
            // ★ 无限卷轴时 progress **不封顶**（一圈 = 1.0，第二圈 = 2.0…）
            progress+=Time.deltaTime/duration;
            if(!endlessLoop)progress=Mathf.Min(1f,progress);
        }
        ComputePosition();
        if(Application.isPlaying&&animate&&progress>=1f&&!endlessLoop){progress=1f;animate=false;}
        if(travelMarker){travelMarker.localPosition=position+Vector3.up*.1f;travelMarker.localRotation=Quaternion.Euler(0,angle,0);}

        // ★ 圈号变化 → 节点锚点重新定位（锚点的子物体 = 节点布景，跟着一起走）
        int lap = LapOf(progress * Length);
        if (lap != CurrentLap)
        {
            CurrentLap = lap;
            RefreshNodeAnchors();
            OnLapChanged?.Invoke(lap);
        }
    }

    /// <summary>把所有节点锚点搬到**当前圈**的位置（锚点的子物体跟着走 ⇒ 节点布景不丢）。</summary>
    public void RefreshNodeAnchors()
    {
        if (nodePresentations == null) return;
        for (int i = 0; i < nodePresentations.Length; i++)
        {
            var p = nodePresentations[i];
            if (p != null) p.SyncTransformForLap(CurrentLap);
        }
    }
    /// <summary>
    /// **相机位置**（唯一真源：<c>Update</c> 与 <c>Before</c> 都用它，否则 shader 拿到的弯曲中心会和内容错位）。
    ///
    /// ★ 无限卷轴时相机也走同一个**圈框架**（<see cref="EvaluateRoute"/>）
    ///   ⇒ 相机与内容同源 ⇒ 接缝处两边同时跨一步、同时转一度 ⇒ 不会错位。
    /// </summary>
    void ComputePosition()
    {
        EvaluateRoute(progress * Length, out position, out angle, out _);
    }

    void Before(Camera cam)
    {
        Restore();
        Shader.SetGlobalFloat("_YSAngle",turnAngle*Mathf.Deg2Rad);
        Shader.SetGlobalFloat("_YSViewDistance",Length);
        // 一圈长度：地面副本按它把自己裁到「本圈那一段」（0 = 不裁，旧行为）
        Shader.SetGlobalFloat("_YSCycleLength", (infiniteScroll && flatCycleLength > 0f) ? flatCycleLength : 0f);
        if(cam!=viewCamera)return;
        ComputePosition();
        Shader.SetGlobalMatrix("_YSToLocal",transform.worldToLocalMatrix);
        Shader.SetGlobalMatrix("_YSToWorld",transform.localToWorldMatrix);
        Shader.SetGlobalVector("_YSPose",new Vector4(position.x,position.y,position.z,angle*Mathf.Deg2Rad));
        Shader.SetGlobalFloat("_YSEnabled",1);
        if(originalBounds==null||originalBounds.Length!=scenery.Length)originalBounds=new Bounds[scenery.Length];
        for(int i=0;i<scenery.Length;i++)if(scenery[i]){
            originalBounds[i]=scenery[i].localBounds;
            scenery[i].localBounds=new Bounds(Vector3.zero,Vector3.one*10000);
        }
        boundsActive=true;
    }
    bool boundsActive;
    void After(Camera cam){Restore();}
    void Restore(){Shader.SetGlobalFloat("_YSEnabled",0);if(!boundsActive)return;for(int i=0;i<scenery.Length;i++)if(scenery[i])scenery[i].localBounds=originalBounds[i];boundsActive=false;}
    void OnDrawGizmos()
    {
        Shader.SetGlobalFloat("_YSAngle",turnAngle*Mathf.Deg2Rad);
        Shader.SetGlobalFloat("_YSViewDistance",Length);
        for(int side=0;side<2;side++){
            Gizmos.color=side==0?Color.cyan:Color.yellow;
            Vector3 prev=transform.position;
            for(int i=1;i<=100;i++){
                float h;var p=transform.TransformPoint(Evaluate(Length*i/100f,turnAngle,side==1,out h));Gizmos.DrawLine(prev,p);prev=p;
            }
        }
    }
}
