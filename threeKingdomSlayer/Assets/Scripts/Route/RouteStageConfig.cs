using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRouteStageConfig", menuName = "一夫当关/路线关卡配置")]
public sealed class RouteStageConfig : ScriptableObject
{
    public int stageId;
    public string stageName = "路线关卡";
    public RouteNodeDefinition startNode;
    public RouteNodeDefinition finalNode;
    public List<RouteNodeDefinition> nodes = new List<RouteNodeDefinition>();
    public int clearCoinReward = 100;

    public bool TryValidate(out string error)
    {
        if (startNode == null) { error = "路线起点为空"; return false; }
        if (finalNode == null) { error = "路线终点为空"; return false; }
        if (nodes == null || nodes.Count == 0) { error = "路线节点列表为空"; return false; }
        var nodeSet = new HashSet<RouteNodeDefinition>(nodes);
        if (!nodeSet.Contains(startNode) || !nodeSet.Contains(finalNode)) { error = "起点或终点不在节点列表中"; return false; }
        foreach (var node in nodes)
        {
            if (node == null) { error = "节点列表包含空节点"; return false; }
            string nodeError;
            if (!node.TryValidate(out nodeError)) { error = nodeError; return false; }
            foreach (var edge in node.outgoingEdges)
                if (!nodeSet.Contains(edge.destination)) { error = "出口目标不在节点列表中"; return false; }
        }
        if (finalNode.outgoingEdges.Count != 0) { error = "终点不能配置出口"; return false; }
        if (!CanReachAllNodesFromStart(nodeSet)) { error = "存在从起点不可达的节点"; return false; }
        if (!CanReachFinal(startNode, new HashSet<RouteNodeDefinition>())) { error = "存在无法到达终点的路线"; return false; }
        error = string.Empty;
        return true;
    }

    private bool CanReachAllNodesFromStart(HashSet<RouteNodeDefinition> nodeSet)
    {
        var visited = new HashSet<RouteNodeDefinition>();
        var pending = new List<RouteNodeDefinition> { startNode };
        while (pending.Count > 0)
        {
            int last = pending.Count - 1;
            var node = pending[last];
            pending.RemoveAt(last);
            if (!visited.Add(node)) continue;
            foreach (var edge in node.outgoingEdges) pending.Add(edge.destination);
        }
        return visited.Count == nodeSet.Count;
    }

    private bool CanReachFinal(RouteNodeDefinition node, HashSet<RouteNodeDefinition> visited)
    {
        if (node == finalNode) return true;
        if (!visited.Add(node)) return false;
        foreach (var edge in node.outgoingEdges)
            if (CanReachFinal(edge.destination, visited)) return true;
        return false;
    }
}
