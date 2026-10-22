using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **摆放表 → 场景规格** 导入器（2026-10-23）。
///
/// 场景自动化的中间格式：参考图由 agent 分析后写成一份**摆放表** ✅，
/// 本工具把它翻译成 <see cref="YSceneSpec"/> ✅（再交给生成器落位 ✅）。
///
/// 摆放表格式（JSON ✅）：
/// <code>
/// {
///   "nodeId": "J1",
///   "groupOrigin": [0, 0, 190],          // 该组在场景里的原点（沿路坐标 ✅）
///   "backgroundMaterial": "YSampleScenery_E0Route",   // 可选 ✅
///   "items": [
///     { "id": "J1_A01_DoubleArrowSignpost_v1", "at": [0.0, 190.0], "layer": "Central Signpost and Divider" },
///     { "id": "J1_StoneRoad_Center_v1",      "at": [0.0, 190.0], "layer": "Ground" }
///   ]
/// }
/// </code>
///
/// 约定：
///   • <c>at</c> = **沿路坐标** [x 横向, z 沿路米数] ✅（绝对值 ✅，相对 <c>groupOrigin</c> 换算成局部 ✅）；
///   • <c>id</c> = 美术资产文件名（不含扩展名 ✅）→ 在美术目录里搜 ✅；
///   • <c>layer</c> = 分组名 ✅（生成后就是场景里的层级 ✅）。
/// </summary>
public static class YScenePlacementImporter
{
    [Serializable]
    private class PlacementItem
    {
        public string id;
        public float[] at;
        public string layer;
        public float scale = 1f;
        public int sortBias = 0;
    }

    [Serializable]
    private class PlacementTable
    {
        public string nodeId;
        public float[] groupOrigin;
        public string backgroundMaterial;
        public string spriteSearchFolders;
        public List<PlacementItem> items = new List<PlacementItem>();
    }

    private static readonly string[] DefaultSearchFolders =
    {
        "Assets/Experiments/CurvedScroll/Art",
        "Assets/Sprites",
    };

    [MenuItem("Tools/Y 场景/摆放表 → 场景规格（导入）")]
    private static void ImportMenu()
    {
        var path = EditorUtility.OpenFilePanel("选摆放表 JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;
        var spec = Import(path);
        if (spec == null) return;
        Selection.activeObject = spec;
        EditorGUIUtility.PingObject(spec);
        Debug.Log($"[Y 场景] 摆放表已导入 → {AssetDatabase.GetAssetPath(spec)}");
    }

    /// <summary>把一份摆放表翻译成规格资产（返回 null 表示失败）。</summary>
    public static YSceneSpec Import(string jsonPath)
    {
        string json;
        try { json = File.ReadAllText(jsonPath); }
        catch (System.Exception e) { Debug.LogError($"[Y 场景] 读不了摆放表：{e.Message}"); return null; }

        PlacementTable table;
        try { table = JsonUtility.FromJson<PlacementTable>(json); }
        catch (System.Exception e) { Debug.LogError($"[Y 场景] 摆放表 JSON 解析失败：{e.Message}"); return null; }
        if (table == null || table.items == null) { Debug.LogError("[Y 场景] 摆放表内容为空"); return null; }

        string nodeId = string.IsNullOrEmpty(table.nodeId) ? Path.GetFileNameWithoutExtension(jsonPath) : table.nodeId;
        Vector3 origin = table.groupOrigin != null && table.groupOrigin.Length >= 3
            ? new Vector3(table.groupOrigin[0], table.groupOrigin[1], table.groupOrigin[2])
            : Vector3.zero;

        var spec = ScriptableObject.CreateInstance<YSceneSpec>();
        spec.routeId = nodeId;

        // 分组 → 条目 ✅
        var byLayer = new Dictionary<string, List<YPropEntry>>();
        var order = new List<string>();
        int missing = 0;
        foreach (var item in table.items)
        {
            if (item == null || string.IsNullOrEmpty(item.id)) continue;
            var sprite = FindSprite(item.id, table.spriteSearchFolders);
            if (sprite == null) { missing++; Debug.LogWarning($"[Y 场景] 找不到美术资产：{item.id}"); continue; }

            var layer = string.IsNullOrEmpty(item.layer) ? "Props" : item.layer;
            if (!byLayer.TryGetValue(layer, out var list)) { list = new List<YPropEntry>(); byLayer[layer] = list; order.Add(layer); }

            var e = new YPropEntry
            {
                name = item.id,
                path = layer + "/" + item.id,
                sprite = sprite,
                material = FindMaterial(table.backgroundMaterial),
                localPosition = new Vector3(
                    (item.at != null && item.at.Length > 0 ? item.at[0] : 0f) - origin.x,
                    0f,
                    (item.at != null && item.at.Length > 1 ? item.at[1] : 0f) - origin.z),
                localScale = Vector3.one * (item.scale <= 0f ? 1f : item.scale),
                sortBias = item.sortBias,
                renderKind = YRenderKind.Sprite,
                pivotMode = YPivotMode.Foot,
            };
            list.Add(e);
        }

        var groups = new List<YSceneGroup>();
        foreach (var layer in order)
        {
            var list = byLayer[layer];
            int baseline = 0;
            foreach (var e in list) { e.sortBias -= baseline; }
            groups.Add(new YSceneGroup { path = layer, sortBaseline = baseline, entries = list.ToArray() });
        }

        var node = new YNodeSceneSpec
        {
            nodeId = nodeId,
            anchorDistance = 0f,
            groups = groups.ToArray(),
        };
        spec.nodeSpecs = new[] { node };

        // 落盘 ✅
        const string folder = "Assets/RouteData/YSceneSpecs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }
        string specPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{nodeId}.asset");
        AssetDatabase.CreateAsset(spec, specPath);
        AssetDatabase.SaveAssets();
        if (missing > 0) Debug.LogWarning($"[Y 场景] 有 {missing} 件美术资产没找到，规格里缺这些 ✅");
        return spec;
    }

    private static Sprite FindSprite(string id, string overrideFolders)
    {
        string[] folders = !string.IsNullOrEmpty(overrideFolders)
            ? overrideFolders.Split(';')
            : DefaultSearchFolders;
        foreach (var folder in folders)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder)) continue;
            foreach (var guid in AssetDatabase.FindAssets(id + " t:Sprite", new string[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.Contains("/" + id + ".")) continue;
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) return s;
            }
        }
        return null;
    }

    private static Material FindMaterial(string materialName)
    {
        if (string.IsNullOrEmpty(materialName)) return null;
        foreach (var guid in AssetDatabase.FindAssets(materialName + " t:Material"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (!p.Contains("/" + materialName + ".")) continue;
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (m != null) return m;
        }
        return null;
    }
}
