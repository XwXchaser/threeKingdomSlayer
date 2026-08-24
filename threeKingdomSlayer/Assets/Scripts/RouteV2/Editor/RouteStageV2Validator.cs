using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class RouteStageV2Validator
{
    [MenuItem("Tools/Route V2/Validate Active RouteStage")]
    public static void ValidateActive()
    {
        var scene = SceneManager.GetActiveScene();
        var entry = Object.FindObjectOfType<RouteStageSceneEntryV2>();
        if (entry == null)
        {
            Debug.LogError("[RouteV2] RouteStageSceneEntryV2 missing in active scene " + scene.path);
            return;
        }

        int errors = 0;
        var configByNode = new Dictionary<RouteNodeConfigV2, RouteCombatNodeEntryV2>();
        var sceneByConfig = new Dictionary<RouteNodeConfigV2, RouteCombatNodeEntryV2>();
        if (entry.routeStageRoot == null) { Debug.LogError("[RouteV2] routeStageRoot missing"); errors++; }
        if (entry.nodes == null || entry.nodes.Length == 0) { Debug.LogError("[RouteV2] no CombatNode entries"); errors++; }
        if (entry.nodes != null)
        {
            for (int i = 0; i < entry.nodes.Length; i++)
            {
                var node = entry.nodes[i];
                if (node == null) { Debug.LogError("[RouteV2] node entry " + i + " is null"); errors++; continue; }
                if (node.nodeConfig == null) { Debug.LogError("[RouteV2] node " + node.name + " config missing"); errors++; }
                else if (sceneByConfig.ContainsKey(node.nodeConfig)) { Debug.LogError("[RouteV2] duplicate scene binding for node config: " + node.nodeConfig.nodeId); errors++; }
                else sceneByConfig.Add(node.nodeConfig, node);
                if (node.GetComponent<RouteCombatNodeEntryV2>() == null) { Debug.LogError("[RouteV2] node " + node.name + " RouteCombatNodeEntryV2 missing or broken"); errors++; }
                if (node.headJunction == null || node.combatArea == null || node.tailJunction == null) { Debug.LogError("[RouteV2] node " + node.name + " Head/Combat/Tail incomplete"); errors++; }
                ValidatePath(node.name + " HeadToCombat", node.headToCombatPath, node.headJunction, node.combatArea, ref errors);
                ValidatePath(node.name + " CombatToTail", node.combatToTailPath, node.combatArea, node.tailJunction, ref errors);
            }
        }

        if (entry.connections != null)
        {
            for (int i = 0; i < entry.connections.Length; i++)
            {
                var connection = entry.connections[i];
                if (connection == null) { Debug.LogError("[RouteV2] connection entry " + i + " is null"); errors++; continue; }
                if (connection.sourceNode == null || connection.targetNode == null) { Debug.LogError("[RouteV2] connection " + connection.name + " node config incomplete"); errors++; }
                if (connection.sourceNode != null && connection.targetNode != null)
                {
                    var key = connection.sourceNode.nodeId + "->" + connection.targetNode.nodeId;
                    if (configByNode.ContainsKey(connection.sourceNode)) { }
                    else configByNode.Add(connection.sourceNode, null);
                    if (sceneByConfig.ContainsKey(connection.sourceNode) == false) { Debug.LogError("[RouteV2] connection source node has no scene binding: " + key); errors++; }
                    if (sceneByConfig.ContainsKey(connection.targetNode) == false) { Debug.LogError("[RouteV2] connection target node has no scene binding: " + key); errors++; }
                    var targetConfig = connection.targetNode;
                    if (!targetConfig.outgoingConnections.Contains(null)) { }
                }
                if (connection.GetComponent<RouteConnectionSceneBindingV2>() == null) { Debug.LogError("[RouteV2] connection " + connection.name + " RouteConnectionSceneBindingV2 missing or broken"); errors++; }
                ValidatePath(connection.name, connection.travelPath, connection.sourceTail, connection.targetHead, ref errors);
                if (connection.rotationPivot == null) { Debug.LogError("[RouteV2] connection " + connection.name + " rotationPivot missing"); errors++; }
            }

            for (int i = 0; i < entry.connections.Length; i++)
            {
                var first = entry.connections[i];
                if (first == null || first.targetHead == null) continue;
                for (int j = i + 1; j < entry.connections.Length; j++)
                {
                    var second = entry.connections[j];
                    if (second == null || second.targetHead == null || first.targetHead != second.targetHead) continue;
                    if (first.targetNode != second.targetNode)
                    {
                        Debug.LogError("[RouteV2] connections share a target Head but targetNode differs: " + first.name + " / " + second.name);
                        errors++;
                    }
                    if (Quaternion.Angle(first.targetHead.rotation, second.targetHead.rotation) > 0.05f)
                    {
                        Debug.LogError("[RouteV2] shared target Head rotation mismatch: " + first.name + " / " + second.name);
                        errors++;
                    }
                }
            }
        }

        var routeConfig = FindRouteConfigForScene(scene);
        if (routeConfig != null)
        {
            if (routeConfig.startNode == null || !sceneByConfig.ContainsKey(routeConfig.startNode)) { Debug.LogError("[RouteV2] startNode is not bound in active scene"); errors++; }
            if (routeConfig.combatNodes == null || routeConfig.combatNodes.Count == 0) { Debug.LogError("[RouteV2] route config combatNodes is empty"); errors++; }
            else
            {
                for (int i = 0; i < routeConfig.combatNodes.Count; i++)
                {
                    var config = routeConfig.combatNodes[i];
                    if (config == null) { Debug.LogError("[RouteV2] route config combatNodes contains null at " + i); errors++; continue; }
                    if (!sceneByConfig.ContainsKey(config)) { Debug.LogError("[RouteV2] route config node has no scene binding: " + config.nodeId); errors++; }
                }
            }
        }
        Debug.Log(errors == 0 ? "[RouteV2] RouteStage validation passed: " + scene.path : "[RouteV2] RouteStage validation errors=" + errors + ": " + scene.path);
    }

    private static RouteStageConfigV2 FindRouteConfigForScene(Scene scene)
    {
        var guids = AssetDatabase.FindAssets("t:RouteStageConfigV2");
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var config = AssetDatabase.LoadAssetAtPath<RouteStageConfigV2>(path);
            if (config != null && config.routeSceneName == scene.name)
                return config;
        }
        return null;
    }

    private static void ValidatePath(string label, Transform[] path, Transform source, Transform target, ref int errors)
    {
        if (source == null || target == null) { Debug.LogError("[RouteV2] " + label + " source/target missing"); errors++; return; }
        if (path == null || path.Length < 2) { Debug.LogError("[RouteV2] " + label + " needs at least 2 points"); errors++; return; }
        for (int i = 0; i < path.Length; i++)
        {
            if (path[i] == null) { Debug.LogError("[RouteV2] " + label + " point " + i + " is null"); errors++; continue; }
            if (path[i].root != source.root) Debug.LogWarning("[RouteV2] " + label + " point " + i + " is outside source root");
        }
        if (Vector3.Distance(path[0].position, source.position) > 0.05f) { Debug.LogError("[RouteV2] " + label + " first point is not connected to source"); errors++; }
        if (Vector3.Distance(path[path.Length - 1].position, target.position) > 0.05f) { Debug.LogError("[RouteV2] " + label + " last point is not connected to target"); errors++; }
    }
}
