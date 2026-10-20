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
    public float TravelAngle => angle;

    public const float JunctionDistance = 190f;
    public const float CurveRadius = 28.1595573f;
    public const float ExitTailDistance = 113f;

    public static float GetRouteLength(float degrees)
    {
        return JunctionDistance + CurveRadius * Mathf.Max(0f, degrees) * Mathf.Deg2Rad + ExitTailDistance;
    }

    public static Vector3 Evaluate(float distance, float degrees, bool right, out float heading)
    {
        // The N1 -> E0 -> J1 lane reaches the fork before either branch turns.
        const float junction=JunctionDistance, radius=CurveRadius;
        float a=degrees*Mathf.Deg2Rad, sign=right?1:-1;
        if(distance<=junction){heading=0;return new Vector3(0,0,distance);}
        float curve=Mathf.Min(distance-junction,radius*a);
        float t=curve/radius;
        heading=sign*t*Mathf.Rad2Deg;
        Vector3 p=new Vector3(sign*radius*(1-Mathf.Cos(t)),0,junction+radius*Mathf.Sin(t));
        float tail=Mathf.Max(0,distance-junction-radius*a);
        return p+new Vector3(sign*Mathf.Sin(a),0,Mathf.Cos(a))*tail;
    }
    public float Length => GetRouteLength(turnAngle);
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
        // Branch world Z is shorter than traveled distance. The authored route distance is authoritative.
        normalizedProgress = match.worldDistance / Mathf.Max(0.1f, Length);
        return true;
    }
    void OnEnable(){Camera.onPreCull+=Before;Camera.onPostRender+=After;}
    void OnDisable(){Camera.onPreCull-=Before;Camera.onPostRender-=After;Restore();}
    void Update()
    {
        if(Application.isPlaying&&animate&&battleHost==null)progress=Mathf.Min(1f,progress+Time.deltaTime/duration);
        position=Evaluate(progress*Length,turnAngle,right,out angle);
        if(Application.isPlaying&&animate&&progress>=1f)animate=false;
        if(travelMarker){travelMarker.localPosition=position+Vector3.up*.1f;travelMarker.localRotation=Quaternion.Euler(0,angle,0);}
    }
    void Before(Camera cam)
    {
        Restore();
        Shader.SetGlobalFloat("_YSAngle",turnAngle*Mathf.Deg2Rad);
        Shader.SetGlobalFloat("_YSViewDistance",Length);
        if(cam!=viewCamera)return;
        position=Evaluate(progress*Length,turnAngle,right,out angle);
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
