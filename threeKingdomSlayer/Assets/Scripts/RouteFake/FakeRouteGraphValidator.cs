using System.Collections.Generic;
using UnityEngine;

/// <summary>Validates the logical route before a runtime can start. No scene or world-space assumptions.</summary>
public static class FakeRouteGraphValidator
{
    public static bool TryValidate(FakeRouteStageConfig config, out string error)
    {
        error = string.Empty;
        if (config == null) { error = "路线配置为空"; return false; }
        if (string.IsNullOrEmpty(config.routeId)) { error = "routeId为空"; return false; }
        if (config.configurationVersion <= 0) { error = "configurationVersion必须大于0"; return false; }
        if (config.startNode == null) { error = "startNode为空"; return false; }
        if (config.nodes == null || config.nodes.Count == 0) { error = "nodes为空"; return false; }
        var nodes = new HashSet<FakeRouteNodeConfig>();
        var ids = new HashSet<string>();
        for (int i = 0; i < config.nodes.Count; i++)
        {
            var node = config.nodes[i];
            if (node == null) { error = "nodes包含空引用"; return false; }
            if (!nodes.Add(node)) { error = "nodes包含重复资产引用: " + node.name; return false; }
            if (string.IsNullOrEmpty(node.nodeId) || !ids.Add(node.nodeId)) { error = "nodeId为空或重复: " + node.name; return false; }
            if (node == config.startNode && node.terminalPolicy != FakeRouteTerminalPolicy.None)
            { error = "startNode不能是终点节点"; return false; }
            if (node.outgoingChoices == null) continue;
            var choiceIds = new HashSet<string>();
            for (int j = 0; j < node.outgoingChoices.Count; j++)
            {
                var choice = node.outgoingChoices[j];
                if (choice == null || choice.targetNode == null) { error = node.name + "存在无效出口"; return false; }
                if (string.IsNullOrEmpty(choice.choiceId) || !choiceIds.Add(choice.choiceId)) { error = node.name + "出口choiceId为空或重复"; return false; }
                if (!ContainsNode(config.nodes, choice.targetNode)) { error = "出口目标不在nodes列表: " + choice.targetNode.name; return false; }
                if (choice.targetNode == node) { error = "禁止节点自环: " + node.nodeId; return false; }
                if (choice.presentation == null) { error = "出口缺少旅行表现: " + choice.choiceId; return false; }
            }
            if (node.terminalPolicy != FakeRouteTerminalPolicy.None && node.outgoingChoices.Count > 0)
            { error = "终点节点不能有出口: " + node.nodeId; return false; }
        }
        if (!ContainsNode(config.nodes, config.startNode)) { error = "startNode不在nodes列表"; return false; }
        // A route may branch, but it must not make any node reachable from itself.
        var visiting = new HashSet<FakeRouteNodeConfig>();
        var visited = new HashSet<FakeRouteNodeConfig>();
        if (HasCycle(config.startNode, visiting, visited, out var cycleNode))
        { error = "从startNode可达路径存在回环: " + cycleNode.nodeId; return false; }
        return true;
    }

    static bool ContainsNode(List<FakeRouteNodeConfig> nodes, FakeRouteNodeConfig target)
    {
        if (nodes == null || target == null) return false;
        for (int i = 0; i < nodes.Count; i++) if (nodes[i] == target) return true;
        return false;
    }

    static bool HasCycle(FakeRouteNodeConfig node, HashSet<FakeRouteNodeConfig> visiting, HashSet<FakeRouteNodeConfig> visited, out FakeRouteNodeConfig cycle)
    {
        cycle = null;
        if (!visiting.Add(node)) { cycle = node; return true; }
        if (node.outgoingChoices != null)
            for (int i = 0; i < node.outgoingChoices.Count; i++)
            {
                var target = node.outgoingChoices[i]?.targetNode;
                if (target == null || visited.Contains(target)) continue;
                if (HasCycle(target, visiting, visited, out cycle)) return true;
            }
        visiting.Remove(node); visited.Add(node); return false;
    }
}
