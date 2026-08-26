using UnityEditor;
using UnityEngine;

public static class FakeRouteValidator
{
    [MenuItem("Tools/Fake Route/Validate Assets")]
    public static void ValidateAssets()
    {
        var guids = AssetDatabase.FindAssets("t:FakeRouteStageConfig");
        int errors = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var route = AssetDatabase.LoadAssetAtPath<FakeRouteStageConfig>(path);
            if (route == null) continue;
            if (string.IsNullOrEmpty(route.routeId)) { Debug.LogError("[FakeRoute] routeId为空: " + path); errors++; }
            if (route.startNode == null) { Debug.LogError("[FakeRoute] startNode为空: " + path); errors++; }
            if (route.nodes == null || route.nodes.Count == 0) { Debug.LogError("[FakeRoute] nodes为空: " + path); errors++; continue; }
            var nodeIds = new System.Collections.Generic.HashSet<string>();
            for (int n = 0; n < route.nodes.Count; n++)
            {
                var node = route.nodes[n];
                if (node == null) { Debug.LogError("[FakeRoute] 空节点: " + path + "#" + n); errors++; continue; }
                if (string.IsNullOrEmpty(node.nodeId) || !nodeIds.Add(node.nodeId)) { Debug.LogError("[FakeRoute] nodeId为空或重复: " + path + " / " + node.nodeId); errors++; }
                if (node.isFinalNode && node.outgoingChoices != null && node.outgoingChoices.Count > 0) { Debug.LogError("[FakeRoute] 终点存在出口: " + node.nodeId); errors++; }
                if (!node.isFinalNode && (node.outgoingChoices == null || node.outgoingChoices.Count == 0)) { Debug.LogError("[FakeRoute] 非终点没有出口: " + node.nodeId); errors++; }
                if (node.battleEntries == null) continue;
                for (int b = 0; b < node.battleEntries.Count; b++)
                    if (node.battleEntries[b] == null || node.battleEntries[b].battleConfig == null) { Debug.LogError("[FakeRoute] BattleEntry为空: " + node.nodeId + "#" + b); errors++; }
            }
            Debug.Log("[FakeRoute] " + path + (errors == 0 ? " validation passed" : " validation completed with errors=" + errors));
        }
        if (guids.Length == 0) Debug.LogWarning("[FakeRoute] 未找到 FakeRouteStageConfig 资产");
    }
}
