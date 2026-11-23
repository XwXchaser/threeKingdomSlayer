using UnityEngine;

/// <summary>Editable node arrival/background anchor. Phase1/2 authoring data only.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRouteNodePresentation : MonoBehaviour
{
    public FakeRouteNodeConfig node;
    public float worldDistance;
    public ScrollRouteDirection direction = ScrollRouteDirection.Forward;
    public Transform battleAnchor;
    public Transform backgroundAnchor;
    public bool showLabel = true;
    public Color color = Color.cyan;
    void OnValidate()
    {
        SyncTransformFromDistance();
    }

    /// <summary>
    /// **按圈重定位**：锚点（及其子物体 = 节点布景）用本圈的**刚体帧**摆放。
    ///
    /// ★ 第 0 圈 frame 为恒等 ⇒ 与旧表现逐像素一致。
    /// </summary>
    public void SyncTransformForLap(int lap)
    {
        var route = GetComponentInParent<YScrollSample>();
        if (route == null) return;

        float heading;
        float distance = worldDistance;
        var local = YScrollSample.Evaluate(distance, route.turnAngle,
            direction == ScrollRouteDirection.Right, out heading);
        var rotation = Quaternion.Euler(0f, heading, 0f);

        if (route.infiniteScroll)
        {
            // ★ **刚体帧**：地图在转（转向由 shader 绕相机旋转做出来）
            //   ⇒ 锚点也必须带着本圈累积的旋转一起摆，不能只沿 +z 平移。
            var frame = route.GetLapFrame(lap);
            transform.localPosition = frame.Apply(local);
            transform.localRotation = frame.Rotation * rotation;
            return;
        }

        transform.position = route.transform.TransformPoint(local);
        transform.rotation = route.transform.rotation * rotation;
    }

    public void SyncTransformFromDistance()
    {
        SyncTransformForLap(0);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, 0.55f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 3f);
        if (battleAnchor)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(battleAnchor.position, new Vector3(6f, 0.1f, 12f));
            Gizmos.DrawLine(battleAnchor.position, battleAnchor.position + battleAnchor.forward * 3f);
        }
#if UNITY_EDITOR
        if (showLabel) UnityEditor.Handles.Label(transform.position + Vector3.up, (node != null ? node.displayName : name) + "  d=" + worldDistance.ToString("F0"));
#endif
    }
}
