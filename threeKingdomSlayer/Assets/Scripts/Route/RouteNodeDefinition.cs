using System;
using System.Collections.Generic;
using UnityEngine;

public enum RouteNodeType
{
    Combat,
    Junction
}

[Serializable]
public sealed class RouteEdgeDefinition
{
    public RouteDirection direction;
    public RouteNodeDefinition destination;
}

[CreateAssetMenu(fileName = "NewRouteNodeDefinition", menuName = "一夫当关/路线节点配置")]
public sealed class RouteNodeDefinition : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public RouteNodeType nodeType = RouteNodeType.Combat;
    public StageConfig battleConfig;
    public RouteNodeDefinition completionJunction;
    public int fixedCoinReward;
    public List<RouteEdgeDefinition> outgoingEdges = new List<RouteEdgeDefinition>();

    public bool IsCombat => nodeType == RouteNodeType.Combat;

    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(nodeId)) { error = "节点ID为空"; return false; }
        if (IsCombat && battleConfig == null) { error = "战斗节点缺少StageConfig"; return false; }
        if (!IsCombat && battleConfig != null) { error = "非战斗节点不应引用StageConfig"; return false; }
        if (completionJunction != null && completionJunction.nodeType != RouteNodeType.Junction) { error = "completionJunction必须是Junction节点"; return false; }
        if (outgoingEdges == null) { error = "出口列表为空"; return false; }
        var directions = new HashSet<RouteDirection>();
        foreach (var edge in outgoingEdges)
        {
            if (edge == null || edge.destination == null) { error = "出口目标为空"; return false; }
            if (!directions.Add(edge.direction)) { error = "同一方向配置多个出口"; return false; }
            if (edge.destination == this) { error = "节点不能连接自身"; return false; }
        }
        error = string.Empty;
        return true;
    }
}
