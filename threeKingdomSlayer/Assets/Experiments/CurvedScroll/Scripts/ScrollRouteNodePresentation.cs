using UnityEngine;

/// <summary>Editable node arrival/background anchor. Phase1/2 authoring data only.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRouteNodePresentation : MonoBehaviour
{
    public FakeRouteNodeConfig node;
    public float worldDistance;
    public Transform battleAnchor;
    public Transform backgroundAnchor;
    public bool showLabel = true;
    public Color color = Color.cyan;
    void OnValidate()
    {
        gameObject.name = (node != null ? node.nodeId : "Node") + " - Presentation Anchor";
        if (worldDistance > 0f) transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, worldDistance);
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
