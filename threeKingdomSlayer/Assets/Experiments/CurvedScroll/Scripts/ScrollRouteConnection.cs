using UnityEngine;

/// <summary>Editable Scene connection. Phase1 only: data and visualization; runtime travel is not wired.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class ScrollRouteConnection : MonoBehaviour
{
    public ScrollWorldRouteRules rules;
    public FakeRouteNodeConfig sourceNode;
    public FakeRouteChoiceConfig choice;
    public FakeRouteNodeConfig targetNode;
    public ScrollRouteDirection direction = ScrollRouteDirection.Forward;
    [Min(0.1f)] public float roadWidth = 5f;
    public Transform[] pathPoints = System.Array.Empty<Transform>();
    public Transform sourceMarker;
    public Transform targetMarker;
    public Transform contentRoot;
    public bool drawPath = true;
    public bool drawArrows = true;
    public Color pathColor = new Color(0.9f, 0.65f, 0.1f, 0.95f);
    public bool showRoadWidth = true;
    public bool showTargetDirection = true;

    public float ExpectedAngle => rules != null ? rules.GetAngle(direction) : 0f;
    public string ConnectionId => sourceNode != null && targetNode != null
        ? sourceNode.nodeId + " -> " + targetNode.nodeId
        : name;

    void OnValidate()
    {
        if (rules != null && roadWidth <= 0f) roadWidth = rules.defaultRoadWidth;
        if (choice != null)
        {
            if (sourceNode == null) sourceNode = FindSourceForChoice(choice);
            if (targetNode == null) targetNode = choice.targetNode;
        }
        gameObject.name = ConnectionId + " [" + direction + "]";
    }

    FakeRouteNodeConfig FindSourceForChoice(FakeRouteChoiceConfig targetChoice)
    {
        var all = Resources.FindObjectsOfTypeAll<FakeRouteNodeConfig>();
        foreach (var node in all)
        {
            if (node == null || node.outgoingChoices == null) continue;
            if (node.outgoingChoices.Contains(targetChoice)) return node;
        }
        return null;
    }

    public Vector3 StartPosition => sourceMarker != null ? sourceMarker.position : GetPoint(0);
    public Vector3 EndPosition => targetMarker != null ? targetMarker.position : GetPoint(-1);
    public Vector3 GetPoint(int index)
    {
        if (pathPoints == null || pathPoints.Length == 0) return transform.position;
        if (index < 0) index = pathPoints.Length - 1;
        index = Mathf.Clamp(index, 0, pathPoints.Length - 1);
        return pathPoints[index] != null ? pathPoints[index].position : transform.position;
    }
    public Vector3 GetTangentAtStart()
    {
        if (pathPoints == null || pathPoints.Length < 2) return transform.forward;
        return (GetPoint(1) - GetPoint(0)).normalized;
    }
    public Vector3 GetTangentAtEnd()
    {
        if (pathPoints == null || pathPoints.Length < 2) return transform.forward;
        return (GetPoint(-1) - GetPoint(pathPoints.Length - 2)).normalized;
    }

    void OnDrawGizmos()
    {
        if (!drawPath || pathPoints == null || pathPoints.Length == 0) return;
        Gizmos.color = pathColor;
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null) continue;
            Gizmos.DrawWireSphere(pathPoints[i].position, 0.25f);
            if (i == 0) Gizmos.DrawLine(pathPoints[i].position, pathPoints[i].position + GetTangentAtStart() * 2f);
            if (i > 0 && pathPoints[i - 1] != null)
            {
                Gizmos.DrawLine(pathPoints[i - 1].position, pathPoints[i].position);
                if (drawArrows) DrawArrow(pathPoints[i - 1].position, pathPoints[i].position);
            }
        }
#if UNITY_EDITOR
        UnityEditor.Handles.color = pathColor;
        var labelPoint = GetPoint(0) + Vector3.up * 0.8f;
        UnityEditor.Handles.Label(labelPoint, ConnectionId + "  " + direction + "  angle=" + ExpectedAngle.ToString("F1") + "°");
        if (showRoadWidth)
        {
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                if (pathPoints[i] == null || pathPoints[i + 1] == null) continue;
                Vector3 tangent = (pathPoints[i + 1].position - pathPoints[i].position).normalized;
                tangent.y = 0f; tangent.Normalize();
                Vector3 side = new Vector3(tangent.z, 0f, -tangent.x) * roadWidth * 0.5f;
                UnityEditor.Handles.DrawLine(pathPoints[i].position - side, pathPoints[i].position + side);
            }
        }
        if (showTargetDirection)
        {
            var end = GetPoint(-1);
            var tangent = GetTangentAtEnd();
            UnityEditor.Handles.ArrowHandleCap(0, end, Quaternion.LookRotation(tangent), 1.2f, EventType.Repaint);
        }
#endif
    }
    void DrawArrow(Vector3 from, Vector3 to)
    {
        var d = to - from;
        if (d.sqrMagnitude < 0.001f) return;
#if UNITY_EDITOR
        UnityEditor.Handles.ArrowHandleCap(0, Vector3.Lerp(from, to, 0.55f), Quaternion.LookRotation(d), 0.5f, EventType.Repaint);
#endif
    }
}
