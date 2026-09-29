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
            errors += ValidateBinding(route);
            if (route.startNode != null && !route.nodes.Contains(route.startNode)) { Debug.LogError("[FakeRoute] 起点不在nodes中: " + path); errors++; }
            var nodeIds = new System.Collections.Generic.HashSet<string>();
            for (int n = 0; n < route.nodes.Count; n++)
            {
                var node = route.nodes[n];
                if (node == null) { Debug.LogError("[FakeRoute] 空节点: " + path + "#" + n); errors++; continue; }
                if (string.IsNullOrEmpty(node.nodeId) || !nodeIds.Add(node.nodeId)) { Debug.LogError("[FakeRoute] nodeId为空或重复: " + path + " / " + node.nodeId); errors++; }
                if (node.isFinalNode && node.outgoingChoices != null && node.outgoingChoices.Count > 0) { Debug.LogError("[FakeRoute] 终点存在出口: " + node.nodeId); errors++; }
                if (node.isFinalNode && (node.battleEntries == null || node.battleEntries.Count != 1)) { Debug.LogError("[FakeRoute] 终点必须恰好有一个BattleEntry: " + node.nodeId); errors++; }
                if (!node.isFinalNode && (node.outgoingChoices == null || node.outgoingChoices.Count == 0)) { Debug.LogError("[FakeRoute] 非终点没有出口: " + node.nodeId); errors++; }
                errors += ValidateBinding(node);
                var choiceIds = new System.Collections.Generic.HashSet<string>();
                if (node.outgoingChoices != null)
                    foreach (var choice in node.outgoingChoices)
                    {
                        if (choice == null || string.IsNullOrEmpty(choice.choiceId) || !choiceIds.Add(choice.choiceId)) { Debug.LogError("[FakeRoute] 空/重复出口: " + node.nodeId); errors++; continue; }
                        if (choice.targetNode == null || !route.nodes.Contains(choice.targetNode)) { Debug.LogError("[FakeRoute] 出口目标缺失或未注册: " + node.nodeId); errors++; }
                        if (choice.presentation != null && choice.presentation.mode == FakeRoutePresentationMode.Video && choice.presentation.videoClip == null) { Debug.LogError("[FakeRoute] 视频引用缺失: " + node.nodeId); errors++; }
                    }
                if (node.battleEntries == null) continue;
                for (int b = 0; b < node.battleEntries.Count; b++)
                    if (node.battleEntries[b] == null || node.battleEntries[b].battleConfig == null) { Debug.LogError("[FakeRoute] BattleEntry为空: " + node.nodeId + "#" + b); errors++; }
            }
            Debug.Log("[FakeRoute] " + path + (errors == 0 ? " validation passed" : " validation completed with errors=" + errors));
        }
        if (guids.Length == 0) Debug.LogWarning("[FakeRoute] 未找到 FakeRouteStageConfig 资产");
    }

    private static int ValidateBinding(ScriptableObject asset)
    {
        var script = new SerializedObject(asset).FindProperty("m_Script").objectReferenceValue as MonoScript;
        if (script != null && script.GetClass() == asset.GetType()) return 0;
        Debug.LogError("[FakeRoute] 脚本绑定缺失或类型不匹配，禁止继续部署: " + AssetDatabase.GetAssetPath(asset));
        return 1;
    }

    [MenuItem("Tools/Fake Route/Reimport And Validate Assets")]
    public static void ReimportAndValidateAssets()
    {
        // Do not save potentially broken objects before checking their disk representation.
        foreach (var path in AssetDatabase.GetAllAssetPaths())
            if (path.StartsWith("Assets/RouteData/", System.StringComparison.Ordinal) && path.EndsWith(".asset", System.StringComparison.Ordinal))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    Debug.LogError("[FakeRoute] 重新导入后资产无法加载: " + path);
            }
        ValidateAssets();
    }
}
