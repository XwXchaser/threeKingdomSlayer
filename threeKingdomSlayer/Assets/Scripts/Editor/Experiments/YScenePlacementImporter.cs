using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **摆放表 ⇄ 场景规格**（2026-10-23）。
///
/// 摆放表 = 自动搭场景的**输入格式** ✅（agent 手写 ✅，或从现成 prefab 反推 ✅）。
///
/// **两种坐标空间** ✅（表级字段 <c>space</c> 决定 ✅）：
///   • <c>"route"</c>（默认 ✅，手写用 ✅）：<c>at</c> = **沿路坐标** [x 横向, z 沿路米数] ✅；
///     物件挂在 <c>layer</c> 桶下 ✅（桶 = 节点根下的一个容器 ✅，桶内坐标即沿路坐标 ✅）；
///   • <c>"local"</c>（导出器用 ✅，无损 ✅）：<c>at</c> = **相对父容器**的局部变换 ✅，
///     <c>layer</c> = 父容器的相对路径 ✅（空 = 直接挂在节点根下 ✅）。
///
/// 格式（JSON ✅）：
/// <code>
/// {
///   "nodeId": "N3",
///   "space": "local",
///   "origin": [0, 0, 266],                       // 节点根在场景里的位置 ✅
///   "rootRotation": [0, 315, 0],                 // 节点根朝向 ✅
///   "items": [
///     { "id": "N3_Grass_L_00", "layer": "Roadside Trees Grass and Rocks", "kind": "sprite",
///       "at": [-4.5, -0.04, 4.0], "rotation": [0,0,0], "scale": [0.48,0.48,0.48],
///       "spriteName": "N3_Grass_L_00", "asset": "Assets/.../N3_Grass_L_00.png",
///       "material": "YSampleScenery_E0Route", "materialPath": "Assets/.../YSampleScenery_E0Route.mat",
///       "sortingOrder": 0 }
///   ]
/// }
/// </code>
///
/// 约定：
///   • <c>kind</c> = sprite ✅（2D 贴图 ✅）/ mesh ✅（3D 面片 ✅）/ group ✅（只带变换的容器 ✅）；
///   • <c>layer</c> = 父容器路径 ✅（= 场景里的层级 ✅）；
///   • <c>asset</c> = 资产路径 ✅（贴图/网格 ✅）→ 贴图按 <c>spriteName</c> 取子精灵 ✅（多精灵图集 ✅）；
///
/// 工具：
///   • <c>Tools/Y 场景/摆放表 → 场景规格（导入）</c> ✅
///   • <c>Tools/Y 场景/场景/预制体 → 摆放表（导出）</c> ✅（用现成场景反推摆放表 ✅）
/// </summary>
public static class YScenePlacementImporter
{
    [Serializable]
    private class PlacementItem
    {
        public string id;
        public string kind = "sprite";
        public string layer = "";
        public string spriteName;
        public string meshName;
        public string asset;
        public string material;
        public string materialPath;
        public float[] at;
        public float[] rotation;
        public float[] scale;
        public int sortingOrder = 0;
        public bool active = true;
        /// <summary>渲染器被关掉 ✅（例：旗子的精灵关掉 ✅、只留风动网格 ✅）—— 不记就会被动点亮 ✅。</summary>
        public bool rendererDisabled = false;
    }

    [Serializable]
    private class PlacementSkipped
    {
        public string element;      // 参考图上的元素（读图结果 ✅）
        public string reason;       // 为什么没搭（素材缺 ✅ / 非本节点素材集 ✅）
        public string note;         // 补充（图上位置 ✅、将来要补时的提示 ✅）
    }

    [Serializable]
    private class PlacementTable
    {
        public string nodeId;
        public string space = "route";
        public float[] origin;
        public float[] rootRotation;
        public string backgroundMaterial;      // 整表默认材质 ✅（手写表用 ✅）
        public string spriteSearchFolders;
        public float roadHalfWidth;            // 路面半宽 ✅（米 ✅）→ 沿街构件不得侵入 ✅（0 = 不检查 ✅）
        public List<PlacementSkipped> skipped = new List<PlacementSkipped>();
        public List<PlacementItem> items = new List<PlacementItem>();
    }

    private static readonly string[] DefaultSearchFolders =
    {
        "Assets/Experiments/CurvedScroll/Art",
        "Assets/Experiments/CurvedScroll/Authoring",
        "Assets/Sprites",
    };

    // ───────────────────────── 菜单 ─────────────────────────

    [MenuItem("Tools/Y 场景/摆放表 → 场景规格（导入）")]
    private static void ImportMenu()
    {
        string path = EditorUtility.OpenFilePanel("选摆放表 JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(path)) return;
        var spec = Import(path);
        if (spec == null) return;
        Selection.activeObject = spec;
        EditorGUIUtility.PingObject(spec);
        Debug.Log($"[Y 场景] 摆放表已导入 → {AssetDatabase.GetAssetPath(spec)}");
    }

    [MenuItem("Tools/Y 场景/场景/预制体 → 摆放表（导出）")]
    private static void ExportMenu()
    {
        var sel = Selection.activeGameObject;
        if (sel == null) { Debug.LogError("[Y 场景] 先选中要导出的 prefab 或场景根"); return; }
        string path = EditorUtility.SaveFilePanel("存摆放表", Application.dataPath, sel.name + "_placement", "json");
        if (string.IsNullOrEmpty(path)) return;
        int n = ExportPlacementTable(sel, path);
        Debug.Log($"[Y 场景] 已导出 {n} 条摆放 → {path}");
    }

    // ───────────────────────── 导出 ─────────────────────────

    /// <summary>
    /// 把一个 prefab / 场景根导出成摆放表 JSON（用现成场景反推 ✅）。
    ///
    /// <paramref name="includeInactive"/> = false（默认 ✅）→ **跳过作者态关掉的物件** ✅
    /// （= 旧残留 ✅：运行时没人会再打开它们 ✅ —— <c>YRouteStreamingWindow</c> 只切 <c>Renderer.enabled</c> ✅）。
    /// 「拿现成场景当参考重搭一遍」时**不要**把这些带上 ✅。
    ///
    /// <paramref name="nodeIdOverride"/>：写进表里的节点 id ✅（默认用根物件名 ✅）。
    /// 对齐路线节点就传它 ✅（例：<c>"EV"</c> ✅ —— 要与 <c>FakeRouteNodeConfig.nodeId</c> 一致 ✅）。
    ///
    /// <paramref name="localRoot"/> = true → 表里的原点/朝向 = 单位变换 ✅：
    /// 内容坐标已经相对根物件了 ✅ → 产出的 prefab **不自带位置** ✅（由路线系统放到节点锚点上 ✅）。
    /// </summary>
    public static int ExportPlacementTable(GameObject root, string jsonPath, bool includeInactive = false,
        string nodeIdOverride = null, bool localRoot = false)
    {
        if (root == null) return 0;
        var table = new PlacementTable
        {
            nodeId = string.IsNullOrEmpty(nodeIdOverride) ? root.name : nodeIdOverride,
            space = "local",                       // 局部坐标 = 无损 ✅
            // localRoot：prefab 不自带位置 ✅（原点=原点 ✅，朝向=朝向 ✅）
            origin = localRoot ? new float[] { 0f, 0f, 0f } : Vec(root.transform.localPosition),
            rootRotation = localRoot ? new float[] { 0f, 0f, 0f } : Vec(root.transform.localEulerAngles),
        };

        int skipped = 0;
        // **全部变换节点**都进表 ✅（容器也要 ✅——容器带位置但自己没渲染器 ✅）
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root.transform) continue;
            // 作者态关掉的 = 旧残留 ✅ → 默认不搬 ✅
            // （连它的子树一起不搬 ✅：父级关着 → 子级在层级上也是不激活的 ✅）
            if (!includeInactive && !IsEffectivelyActive(t, root.transform)) { skipped++; continue; }
            var item = new PlacementItem
            {
                id = t.name,
                // 父容器路径（相对节点根 ✅，空 = 直接挂在节点根下 ✅）
                layer = RelativeParentPath(t, root.transform),
                // **局部变换**（相对父容器 ✅）——不做世界坐标换算 ✅
                // （世界换算会引入 ~1e-5 的浮点误差 ✅，局部值本身是精确的 ✅）
                at = Vec(t.localPosition),
                rotation = Vec(t.localEulerAngles),
                scale = Vec(t.localScale),
                active = t.gameObject.activeSelf,
            };

            var sr = t.GetComponent<SpriteRenderer>();
            var mr = t.GetComponent<MeshRenderer>();
            if (sr != null)
            {
                item.kind = "sprite";
                item.sortingOrder = sr.sortingOrder;
                item.rendererDisabled = !sr.enabled;      // 作者态关掉的渲染器 ✅ 要带走 ✅
                if (sr.sprite != null)
                {
                    item.spriteName = sr.sprite.name;
                    item.asset = AssetDatabase.GetAssetPath(sr.sprite);
                }
                CaptureMaterial(item, sr.sharedMaterial);
            }
            else if (mr != null)
            {
                item.kind = "mesh";
                item.sortingOrder = mr.sortingOrder;
                item.rendererDisabled = !mr.enabled;      // 同上 ✅
                var mf = t.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    item.meshName = mf.sharedMesh.name;
                    item.asset = AssetDatabase.GetAssetPath(mf.sharedMesh);
                }
                CaptureMaterial(item, mr.sharedMaterial);
            }
            else
            {
                item.kind = "group";             // 只带变换的容器 ✅（嵌套 prefab 实例 ✅）
            }
            table.items.Add(item);
        }

        try
        {
            File.WriteAllText(jsonPath, JsonUtility.ToJson(table, true));
            if (skipped > 0) Debug.Log($"[Y 场景] 导出跳过 {skipped} 个不激活物件（旧残留 ✅）");
            return table.items.Count;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Y 场景] 写摆放表失败：{e.Message}");
            return 0;
        }
    }

    /// <summary>物件是否**实际可见**（自身与所有祖先都激活 ✅，根除外 ✅）。</summary>
    private static bool IsEffectivelyActive(Transform t, Transform root)
    {
        var cur = t;
        while (cur != null && cur != root)
        {
            if (!cur.gameObject.activeSelf) return false;
            cur = cur.parent;
        }
        return true;
    }

    private static void CaptureMaterial(PlacementItem item, Material mat)
    {
        if (mat == null) return;
        item.material = mat.name;
        item.materialPath = AssetDatabase.GetAssetPath(mat);
    }

    private static float[] Vec(Vector3 v) => new float[] { v.x, v.y, v.z };

    /// <summary>物件的**父容器**路径（相对节点根 ✅；空 = 直接挂在节点根下 ✅）。</summary>
    private static string RelativeParentPath(Transform t, Transform root)
    {
        var parts = new List<string>();
        var cur = t.parent;
        while (cur != null && cur != root) { parts.Insert(0, cur.name); cur = cur.parent; }
        return string.Join("/", parts);
    }

    // ───────────────────────── 导入 ─────────────────────────

    /// <summary>把一份摆放表翻译成规格资产（返回 null 表示失败）。</summary>
    public static YSceneSpec Import(string jsonPath)
    {
        if (!File.Exists(jsonPath)) { Debug.LogError($"[Y 场景] 找不到摆放表：{jsonPath}"); return null; }
        PlacementTable table;
        try { table = JsonUtility.FromJson<PlacementTable>(File.ReadAllText(jsonPath)); }
        catch (System.Exception e) { Debug.LogError($"[Y 场景] 摆放表解析失败：{e.Message}"); return null; }
        if (table == null || table.items == null || table.items.Count == 0)
        {
            Debug.LogError("[Y 场景] 摆放表是空的");
            return null;
        }

        string nodeId = string.IsNullOrEmpty(table.nodeId) ? "YNode" : table.nodeId;
        bool localSpace = table.space == "local";
        Vector3 origin = ReadVec(table.origin);
        Vector3 rootEuler = ReadVec(table.rootRotation);
        Quaternion rootRot = Quaternion.Euler(rootEuler);

        var spec = ScriptableObject.CreateInstance<YSceneSpec>();
        spec.routeId = "changbanpo";

        var byLayer = new Dictionary<string, List<YPropEntry>>();
        var order = new List<string>();
        int missing = 0;
        int maxSorting = 0;

        foreach (var item in table.items)
        {
            if (item == null || string.IsNullOrEmpty(item.id)) continue;
            string layer = item.layer ?? "";
            if (!byLayer.TryGetValue(layer, out var list)) { list = new List<YPropEntry>(); byLayer[layer] = list; order.Add(layer); }

            // 坐标 → 局部 ✅（route 空间 = 沿路绝对 ✅；local 空间 = 已是局部 ✅）
            Vector3 at = ReadAt(item.at);
            Vector3 localPos, localEuler, localScale;
            if (localSpace)
            {
                localPos = at;
            }
            else
            {
                // 沿路绝对坐标 → 节点根的局部 ✅（根有朝向时要转回来 ✅）
                localPos = Quaternion.Inverse(rootRot) * (at - origin);
            }
            localEuler = ReadVec(item.rotation);
            Vector3 scl = item.scale != null && item.scale.Length >= 3 ? ReadVec(item.scale) : Vector3.one;

            var e = new YPropEntry
            {
                name = item.id,
                path = (layer.Length > 0 ? layer + "/" : "") + item.id,   // 完整相对路径 ✅
                localPosition = localPos,
                localEulerAngles = localEuler,
                localScale = scl,
                material = FindMaterial(item.materialPath, item.material) ?? FindMaterial(null, table.backgroundMaterial),
                sortBias = item.sortingOrder,
                renderKind = YRenderKind.Sprite,
                pivotMode = YPivotMode.Foot,
                active = item.active,
                rendererDisabled = item.rendererDisabled,
            };

            if (item.kind == "mesh")
            {
                var mesh = FindAssetByName<Mesh>(item.asset, item.meshName);
                if (mesh == null) { missing++; Debug.LogWarning($"[Y 场景] 找不到网格：{item.asset} / {item.meshName}"); }
                e.renderKind = YRenderKind.Solid3D;
                e.mesh = mesh;
                e.pivotMode = YPivotMode.Center;
            }
            else if (item.kind == "group")
            {
                // 只带变换的容器 ✅（不加工渲染器 ✅）
                e.sprite = null;
                e.containerOnly = true;
            }
            else
            {
                var sprite = FindAssetByName<Sprite>(item.asset, item.spriteName);
                if (sprite == null) sprite = FindSprite(item.id, table.spriteSearchFolders);
                if (sprite == null) { missing++; Debug.LogWarning($"[Y 场景] 找不到美术资产：{item.id}（{item.asset}）"); }
                e.sprite = sprite;
            }

            if (e.material == null) missing++;
            maxSorting = Mathf.Max(maxSorting, item.sortingOrder);
            list.Add(e);
        }

        // 组 = 父容器路径 ✅（组基准 = 组内最小 sortingOrder ✅，条目用相对偏移 ✅）
        var groups = new List<YSceneGroup>();
        foreach (var layer in order)
        {
            var list = byLayer[layer];
            int baseline = list.Min(x => x.sortBias);
            foreach (var e in list) e.sortBias -= baseline;
            groups.Add(new YSceneGroup { path = layer, sortBaseline = baseline, entries = list.ToArray() });
        }

        var node = new YNodeSceneSpec
        {
            nodeId = nodeId,
            anchorDistance = 0f,
            groups = groups.ToArray(),
            // 节点根的变换 ✅（表里带了才写 ✅——否则保留场景里已有的摆位 ✅）
            localPosition = origin,
            localEulerAngles = rootEuler,
            localScale = Vector3.one,
            applyRootTransform = true,
        };
        spec.nodeSpecs = new[] { node };
        spec.renderWorldBaseline = Mathf.Max(0, maxSorting);

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
        if (missing > 0) Debug.LogWarning($"[Y 场景] 有 {missing} 件资产没找到，规格里缺这些 ✅");

        // 沿街构件不得侵入道路 ✅（路面半宽由表给 ✅ —— 权威值来自 ScrollRouteRoadSurface.width ✅）
        if (table.roadHalfWidth > 0f)
        {
            var intruded = new List<string>();
            foreach (var item in table.items)
            {
                if (item == null || item.kind == "mesh" || item.kind == "group") continue;
                var sprite = FindAssetByName<Sprite>(item.asset, item.spriteName);
                if (sprite == null) continue;
                float scale = item.scale != null && item.scale.Length >= 3 ? item.scale[0] : 1f;
                float x = item.at != null && item.at.Length >= 3 ? item.at[0] : 0f;
                // 精灵相对枢轴的左右延伸 ✅（visible bounds ✅ + 枢轴偏移 ✅）
                float half = sprite.bounds.size.x * 0.5f * scale;
                float pivotOffset = (sprite.pivot.x / Mathf.Max(1f, sprite.rect.size.x) - 0.5f) * sprite.bounds.size.x * scale;
                float left = x + (-half - pivotOffset);
                float right = x + (half - pivotOffset);
                float inner = Mathf.Abs(left) < Mathf.Abs(right) ? left : right;   // 更靠路中心的那侧 ✅
                if (Mathf.Abs(inner) < table.roadHalfWidth)
                    intruded.Add($"{item.id}（内边缘 {inner:0.00}m < 路面半宽 {table.roadHalfWidth:0.0}m ✅）");
            }
            if (intruded.Count > 0)
                Debug.LogWarning($"[Y 场景] 有 {intruded.Count} 件沿街构件侵入道路 ✅（路面半宽 {table.roadHalfWidth}m ✅）：\n    · " + string.Join("\n    · ", intruded));
            else
                Debug.Log($"[Y 场景] 沿街构件不侵入道路 ✅（路面半宽 {table.roadHalfWidth}m ✅）");
        }
        // 缺口清单 ✅：参考图上有、但没搭的元素 ✅（不生成 ✅、不借用别处素材 ✅ —— 记下来再说 ✅）
        if (table.skipped != null && table.skipped.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[Y 场景] 摆放表记录了 {table.skipped.Count} 个「参考图上有、没搭」的元素 ✅：");
            foreach (var sk in table.skipped)
                if (sk != null) sb.AppendLine($"    · {sk.element} —— {sk.reason}" + (string.IsNullOrEmpty(sk.note) ? "" : "（" + sk.note + "）"));
            Debug.Log(sb.ToString());
        }
        return spec;
    }

    private static Vector3 ReadVec(float[] a, Vector3 fallback = default)
    {
        if (a == null || a.Length < 3) return fallback;
        return new Vector3(a[0], a[1], a[2]);
    }

    /// <summary>读 <c>at</c> ✅：3 个数 = [x, y, z] ✅；2 个数 = [x, z]（y = 0 ✅）。</summary>
    private static Vector3 ReadAt(float[] a)
    {
        if (a == null || a.Length == 0) return Vector3.zero;
        if (a.Length == 1) return new Vector3(a[0], 0f, 0f);
        if (a.Length == 2) return new Vector3(a[0], 0f, a[1]);
        return new Vector3(a[0], a[1], a[2]);
    }

    // ───────────────────────── 资产查找 ─────────────────────────

    /// <summary>按路径找资产 ✅；路径里有多个子资产时按名字取 ✅（多精灵图集 ✅）。</summary>
    private static T FindAssetByName<T>(string assetPath, string name) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(assetPath)) return null;
        var direct = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (direct != null && (string.IsNullOrEmpty(name) || direct.name == name)) return direct;
        var all = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (all == null) return null;
        T fallback = null;
        foreach (var o in all)
        {
            if (!(o is T)) continue;
            var typed = (T)o;
            if (!string.IsNullOrEmpty(name) && typed.name == name) return typed;
            if (fallback == null) fallback = typed;
        }
        return fallback;
    }

    private static Sprite FindSprite(string id, string overrideFolders)
    {
        string[] folders = !string.IsNullOrEmpty(overrideFolders)
            ? overrideFolders.Split(';', ',')
            : DefaultSearchFolders;
        foreach (var folder in folders)
        {
            if (string.IsNullOrEmpty(folder)) continue;
            foreach (var guid in AssetDatabase.FindAssets(id + " t:Sprite", new[] { folder }))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!p.Contains("/" + id + ".")) continue;
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
                if (s != null) return s;
            }
        }
        return null;
    }

    private static Material FindMaterial(string materialPath, string materialName)
    {
        if (!string.IsNullOrEmpty(materialPath))
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (m != null) return m;
        }
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
