using System.Collections.Generic;
using UnityEngine;

public sealed class RouteWorldChannelPoint : MonoBehaviour
{
    public string channelId;
    public RouteWorldNodeBinding sourceNode;
    public RouteWorldNodeBinding targetNode;
    public RouteDirection direction;
    public Transform sourceHead;
    public Transform turnPivot;
    public Transform targetTail;
    public Transform[] pathPoints = System.Array.Empty<Transform>();

    public bool TryValidate(out string error)
    {
        if (sourceNode == null || targetNode == null) { error = "节点引用不完整"; return false; }
        if (sourceHead == null || targetTail == null) { error = "通道首尾缺失"; return false; }
        if (pathPoints == null || pathPoints.Length < 2) { error = "路径点少于2个"; return false; }
        for (int i = 0; i < pathPoints.Length; i++)
            if (pathPoints[i] == null) { error = "路径包含空点"; return false; }
        if (Vector3.Distance(sourceHead.position, pathPoints[0].position) > 0.01f) { error = "首点未连接SourceHead"; return false; }
        if (Vector3.Distance(targetTail.position, pathPoints[pathPoints.Length - 1].position) > 0.01f) { error = "末点未连接TargetTail"; return false; }
        if (turnPivot != null && turnPivot.root != transform.root) { error = "TurnPivot不在RouteWorldRoot下"; return false; }
        error = string.Empty;
        return true;
    }
}

public sealed class RouteWorldGraph : MonoBehaviour
{
    public Transform routeWorldRoot;
    public RouteWorldNodeBinding[] nodes = System.Array.Empty<RouteWorldNodeBinding>();
    public RouteWorldChannelPoint[] channels = System.Array.Empty<RouteWorldChannelPoint>();

    public bool TryGetIncomingDirection(RouteWorldNodeBinding node, RouteWorldChannelPoint exclude, out Vector3 direction)
    {
        direction = Vector3.forward;
        if (node == null || channels == null) return false;
        foreach (var channel in channels)
        {
            if (channel == null || channel == exclude || channel.targetNode != node || channel.sourceNode == null) continue;
            Vector3 delta = node.transform.localPosition - channel.sourceNode.transform.localPosition;
            if (delta.sqrMagnitude > 0.0001f)
            {
                direction = delta.normalized;
                return true;
            }
        }
        return false;
    }

    public bool TryValidate(out string error)
    {
        if (routeWorldRoot == null) { error = "RouteWorldRoot为空"; return false; }
        var nodeSet = new HashSet<RouteWorldNodeBinding>(nodes);
        foreach (var node in nodes)
        {
            if (node == null || node.transform.root != routeWorldRoot.root) { error = "节点不在RouteWorldRoot下"; return false; }
            if (node.nodeDefinition == null) { error = "场景节点缺少RouteNodeDefinition"; return false; }
            if (node.nodeType == RouteWorldNodeType.Combat && (node.headJunction == null || node.tailJunction == null)) { error = "Combat节点缺少头尾Junction"; return false; }
            if (node.nodeType == RouteWorldNodeType.Junction && node.junctionAnchor == null) { error = "Junction节点缺少JunctionAnchor"; return false; }
            if (node.nodeType == RouteWorldNodeType.Combat && (node.headJunction.GetComponent<RouteWorldAnchor>() == null || node.tailJunction.GetComponent<RouteWorldAnchor>() == null)) { error = "Combat头尾缺少RouteWorldAnchor组件"; return false; }
            if (node.nodeType == RouteWorldNodeType.Junction && node.junctionAnchor.GetComponent<RouteWorldAnchor>() == null) { error = "Junction缺少RouteWorldAnchor组件"; return false; }
        }
        foreach (var channel in channels)
        {
            if (channel == null || channel.transform.root != routeWorldRoot.root) { error = "通道不在RouteWorldRoot下"; return false; }
            if (!nodeSet.Contains(channel.sourceNode) || !nodeSet.Contains(channel.targetNode)) { error = "通道节点不在图中"; return false; }
            if (!channel.TryValidate(out error)) return false;
        }
        error = string.Empty;
        return true;
    }
}
