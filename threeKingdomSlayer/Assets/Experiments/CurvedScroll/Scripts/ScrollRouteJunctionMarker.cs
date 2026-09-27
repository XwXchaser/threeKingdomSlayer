using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRouteJunctionMarker : MonoBehaviour
{
    public string label;
    public ScrollRouteConnection[] connections = System.Array.Empty<ScrollRouteConnection>();
    public bool merge;
    public float radius = 2f;
    public Color color = new Color(1f, 0.8f, 0.1f, 0.8f);
    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, radius);
        if (connections != null)
            foreach (var c in connections)
                if (c != null) Gizmos.DrawLine(transform.position, c.GetPoint(0));
#if UNITY_EDITOR
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(transform.position + Vector3.up, (merge ? "MERGE " : "BRANCH ") + label);
#endif
    }
}
