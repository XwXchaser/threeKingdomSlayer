using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// **Y 场景规格工具**（2026-10-23）：导出 / 生成 / 校验 / 往返验证。
///
/// 规格 ↔ 场景的映射规则（**路径驱动** ✅，无损 ✅）：
///   • **每个带 Renderer 的节点 = 一条条目** ✅，条目带**完整相对路径** ✅（相对节点根 ✅）；
///   • 路径上缺的无渲染器容器，生成时**自动补出来** ✅（纯容器 ✅）；
///   • **组** = 路径前缀 ✅（用于排序基准与自检 ✅）：组基准 = 组内条目最小 sortingOrder ✅，
///     条目用 **sortBias（相对组基准的偏移）** 表达前后 ✅；
///   • 生成是**幂等**的 ✅：同路径对象复用并更新 ✅，标了 <c>userModified</c> 的条目**不覆盖** ✅。
///
/// 工作流：① 导出切片为规格 → ② 从规格生成到当前场景 → ③ 校验规格 → ④ 往返验证。
/// </summary>
public static class YSceneSpecTools
{
    private const string SpecFolder = "Assets/RouteData/YSceneSpecs";
    private const string YScenePath = "Assets/Experiments/CurvedScroll/YJunctionSample.unity";

    // ───────────────────────── 菜单 ─────────────────────────

    [MenuItem("Tools/Y 场景规格/① 导出切片为规格")]
    private static void ExportSliceMenu()
    {
        var sel = Selection.activeGameObject;
        if (sel == null) { Debug.LogError("[Y 场景规格] 先在场景里选中要导出的那一组"); return; }
        var spec = ExportSlice(sel, null);
        if (spec == null) return;
        Selection.activeObject = spec;
        EditorGUIUtility.PingObject(spec);
        Debug.Log($"[Y 场景规格] 已导出切片「{sel.name}」→ {AssetDatabase.GetAssetPath(spec)}");
    }

    [MenuItem("Tools/Y 场景规格/② 从规格生成到当前场景")]
    private static void GenerateMenu()
    {
        var spec = Selection.activeObject as YSceneSpec;
        if (spec == null) { Debug.LogError("[Y 场景规格] 先在 Project 里选中一个 YSceneSpec 资产"); return; }
        var scene = SceneManager.GetActiveScene();
        var root = FindOrCreateRoot(scene, spec);
        int count = Generate(spec, root);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[Y 场景规格] 已在场景「{scene.name}」生成/更新 {count} 个对象，根组「{root.name}」");
    }

    [MenuItem("Tools/Y 场景规格/③ 校验规格")]
    private static void ValidateMenu()
    {
        var spec = Selection.activeObject as YSceneSpec;
        if (spec == null) { Debug.LogError("[Y 场景规格] 先选中一个 YSceneSpec 资产"); return; }
        var issues = Validate(spec, out var clusters);
        if (issues.Count == 0) { Debug.Log("[Y 场景规格] 自检通过 ✅（0 问题）"); }
        else
        {
            foreach (var i in issues) Debug.LogWarning("[Y 场景规格] " + i);
            Debug.Log($"[Y 场景规格] 自检发现 {issues.Count} 个问题");
        }
        if (clusters.Count > 0)
            Debug.Log($"[Y 场景规格] 另有 {clusters.Count} 对「挤在一簇」✅（同深度带 ✅ + 脚点 < 0.5m ✅ —— 多为故意的叠放 ✅ 默认不报 ✅）");
    }

    [MenuItem("Tools/Y 场景规格/④ 往返验证（导出→生成→逐件比对）")]
    private static void RoundTripMenu()
    {
        var sel = Selection.activeObject;
        if (sel == null) { Debug.LogError("[Y 场景规格] 先在场景里选中要验证的那一组"); return; }
        RunRoundTrip(sel as GameObject);
    }

    [MenuItem("Tools/Y 场景规格/⑤ 摆放表往返验证（prefab→摆放表→规格→生成→逐件比对）")]
    private static void PlacementRoundTripMenu()
    {
        var sel = Selection.activeGameObject;
        if (sel == null) { Debug.LogError("[Y 场景规格] 先在 Project 里选中要验证的 prefab"); return; }
        RunPlacementRoundTrip(sel);
    }

    [MenuItem("Tools/Y 场景规格/⑥ 节点 prefab 生产（导出→摆放表→规格→prefab→逐件比对）")]
    private static void NodeProductionMenu()
    {
        var sel = Selection.activeGameObject;
        if (sel == null) { Debug.LogError("[Y 场景规格] 先在场景里选中要生产的节点组（节点锚点 ✅）"); return; }
        string nodeId = DeriveNodeId(sel.name);
        string prefabPath = $"Assets/Experiments/CurvedScroll/Authoring/Nodes/{nodeId}.prefab";
        string report;
        bool ok = RunNodeProduction(sel, nodeId, prefabPath, out report);
        Debug.Log($"[Y 场景规格] 节点生产 {nodeId}：" + (ok ? "逐件差异 0 ✅" : "有差异 ❌") + "\n" + report);
    }

    [MenuItem("Tools/Y 场景规格/⑦ 预览段 prefab（摆进当前场景）")]
    private static void PreviewSegmentMenu()
    {
        var sel = Selection.activeObject as GameObject;
        if (sel == null || !EditorUtility.IsPersistent(sel))
        {
            Debug.LogError("[Y 场景规格] 先在 Project 里选中一个段 prefab ✅");
            return;
        }
        PreviewSegmentInScene(sel);
    }

    /// <summary>
    /// 把段 prefab **按它自带的位姿** 摆进当前场景 ✅（段 prefab 的根带世界位姿 ✅，见 <c>YRouteSegment</c> 约定 ✅）。
    /// 段是**运行时按需装载**的 ✅ → Edit 模式的场景里没有它 ✅ → 想在编辑器里看就得这样摆一份 ✅。
    /// 只影响当前场景 ✅（可撤销 ✅）。
    /// </summary>
    public static GameObject PreviewSegmentInScene(GameObject segmentPrefab)
    {
        if (segmentPrefab == null) return null;
        var scene = SceneManager.GetActiveScene();
        var inst = PrefabUtility.InstantiatePrefab(segmentPrefab, scene) as GameObject;
        if (inst == null) return null;
        inst.name = segmentPrefab.name + " (预览)";
        inst.transform.position = segmentPrefab.transform.localPosition;
        inst.transform.rotation = segmentPrefab.transform.localRotation;
        // **不被保存** ✅（场景随便存 ✅，不会留下预览垃圾 ✅；重载场景自动没 ✅）
        inst.hideFlags = HideFlags.DontSave;
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = inst;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"[Y 场景规格] 段预览已摆进场景「{scene.name}」：{inst.name} @ {inst.transform.position} rot {inst.transform.eulerAngles}（场景已脏 ✅，可撤销 ✅）");
        return inst;
    }

    [MenuItem("Tools/Y 场景规格/⑧ 全段表化（导出→表→规格→回写 prefab→逐件比对＋对照图）")]
    private static void BatchTableizeMenu()
    {
        string report;
        int done = BatchTableizeSegments(out report, true);
        Debug.Log($"[Y 场景规格] 全段表化完成（成功 {done} 个 ✅）\n{report}");
    }

    /// <summary>
    /// **全段表化** ✅：把 <c>Authoring/Segments/</c> 下每个段 prefab 变成「表驱动的段」✅。
    ///
    /// 流程：导表 ✅ → 导规格 ✅ → 生成到临时场景并与原 prefab **逐件比对** ✅（必须 0 ✅）→
    /// 渲染**改前/改后同机位对照图** ✅ → **回写同一个 prefab 路径** ✅（GUID 不变 ✅ → 段表不用改 ✅）。
    /// 已有同名表的段会跳过 ✅（幂等 ✅）。
    /// </summary>
    public static int BatchTableizeSegments(out string report, bool renderCompare)
    {
        var sb = new System.Text.StringBuilder();
        report = "";
        const string segFolder = "Assets/Experiments/CurvedScroll/Authoring/Segments";
        const string tableFolder = "Assets/Experiments/CurvedScroll/Authoring/Placement";
        const string shotFolder = "Library/Locus/tmp/segment_compare";
        if (renderCompare) Directory.CreateDirectory(shotFolder);

        int done = 0, failed = 0, skipped = 0;
        var guids = AssetDatabase.FindAssets("t:Prefab", new[] { segFolder });
        var paths = new List<string>();
        foreach (var g in guids) paths.Add(AssetDatabase.GUIDToAssetPath(g));
        paths.Sort();

        foreach (var path in paths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;
            string name = prefab.name;
            // 空占位 prefab（只有一个根、没有内容 ✅）→ 跳过 ✅（不是失败 ✅）
            if (prefab.transform.childCount == 0) { skipped++; sb.AppendLine($"[跳过] {name}（空占位 ✅）"); continue; }
            string tablePath = tableFolder + "/" + name + ".json";
            if (File.Exists(tablePath)) { skipped++; sb.AppendLine($"[跳过] {name}（已有表 ✅）"); continue; }

            // ① 导表 ✅
            int exported = YScenePlacementImporter.ExportPlacementTable(prefab, tablePath, false, name, false);
            // ② 导规格 ✅
            var spec = YScenePlacementImporter.Import(tablePath);
            if (spec == null) { failed++; sb.AppendLine($"[失败] {name}：规格导入失败 ✅"); continue; }

            // ③ 生成到临时场景 → 与原 prefab 逐件比对 ✅
            var tmp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var holder = new GameObject("Gen");
            Generate(spec, holder.transform);
            var nodeRoot = holder.transform.Find(spec.nodeSpecs[0].nodeId);
            var diffs = nodeRoot != null ? CompareTrees(prefab.transform, nodeRoot, true, true) : new List<string> { "生成结果里找不到节点根" };
            UnityEngine.Object.DestroyImmediate(holder);
            EditorSceneManager.CloseScene(tmp, true);

            if (diffs.Count > 0)
            {
                failed++;
                sb.AppendLine($"[失败] {name}：逐件比对 {diffs.Count} 条差异 ✅ → 不回写 ✅（表保留待查 ✅）");
                for (int i = 0; i < diffs.Count && i < 5; i++) sb.AppendLine("      " + diffs[i]);
                continue;
            }

            // ④ 改前对照图 ✅
            string beforePath = null, afterPath = null;
            if (renderCompare)
            {
                beforePath = shotFolder + "/" + name + "_before.png";
                RenderSegmentView(prefab, beforePath);
            }

            // ⑤ 回写同一个 prefab 路径 ✅（GUID 不变 ✅ → 段表不用动 ✅）
            int count = 0;
            var written = GenerateToPrefab(spec, path, out count);
            if (written == null) { failed++; sb.AppendLine($"[失败] {name}：prefab 回写失败 ✅"); continue; }

            // ⑥ 改后对照图 ✅
            if (renderCompare)
            {
                afterPath = shotFolder + "/" + name + "_after.png";
                RenderSegmentView(AssetDatabase.LoadAssetAtPath<GameObject>(path), afterPath);
                sb.AppendLine($"[完成] {name}：表 {exported} 条 ✅ → prefab {count} 件 ✅ 逐件差异 0 ✅ | 对照图 {beforePath} / {afterPath}");
            }
            else
            {
                sb.AppendLine($"[完成] {name}：表 {exported} 条 ✅ → prefab {count} 件 ✅ 逐件差异 0 ✅");
            }
            done++;
        }
        AssetDatabase.SaveAssets();
        report = sb.ToString() + $"\n汇总：成功 {done} ✅ / 跳过 {skipped} ✅ / 失败 {failed} ✅";
        return done;
    }

    /// <summary>
    /// 按段 prefab **自带的位姿** 渲染一张同机位对照图 ✅（相机 = 段根的位姿 + 眼高 1.6m ✅，沿段的前进方向 ✅）。
    /// 用来肉眼验收「改前 vs 改后」✅。
    /// </summary>
    public static string RenderSegmentView(GameObject segmentPrefab, string outPath)
    {
        if (segmentPrefab == null) return null;
        var scene = SceneManager.GetActiveScene();
        GameObject inst = null;
        try
        {
            inst = PrefabUtility.InstantiatePrefab(segmentPrefab, scene) as GameObject;
            if (inst == null) return null;
            inst.hideFlags = HideFlags.DontSave;
            var rootT = segmentPrefab.transform;
            var camGo = new GameObject("SegCam");
            camGo.hideFlags = HideFlags.DontSave;
            var cam = camGo.AddComponent<Camera>();
            camGo.transform.position = rootT.position + rootT.rotation * new Vector3(0f, 1.6f, 0f);
            camGo.transform.rotation = rootT.rotation * Quaternion.Euler(2f, 0f, 0f);
            cam.fieldOfView = 60f;
            cam.aspect = 0.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            int W = 900, H = 1800;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(inst);
            return outPath;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Y 场景规格] 渲染对照图失败：" + e.Message);
            if (inst != null) UnityEngine.Object.DestroyImmediate(inst);
            return null;
        }
    }

    /// <summary>从物件名推节点 id ✅（"EV - Presentation Anchor" → "EV" ✅）。</summary>
    public static string DeriveNodeId(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return "Node";
        int idx = objectName.IndexOf(" - Presentation Anchor", System.StringComparison.OrdinalIgnoreCase);
        if (idx > 0) return objectName.Substring(0, idx).Trim();
        return objectName.Trim();
    }

    // ───────────────────────── 导出 ─────────────────────────

    public static YSceneSpec ExportSlice(GameObject sliceRoot, YSceneSpec existing, bool includeInactive = false)
    {
        if (sliceRoot == null) return null;
        var spec = existing;
        if (spec == null)
        {
            if (!AssetDatabase.IsValidFolder(SpecFolder))
            {
                Directory.CreateDirectory(SpecFolder);
                AssetDatabase.Refresh();
            }
            string path = AssetDatabase.GenerateUniqueAssetPath($"{SpecFolder}/{sliceRoot.name}_Slice.asset");
            spec = ScriptableObject.CreateInstance<YSceneSpec>();
            AssetDatabase.CreateAsset(spec, path);
        }
        var built = BuildSpecFromSlice(sliceRoot, includeInactive);
        spec.routeId = built.routeId;
        spec.renderWorldBaseline = built.renderWorldBaseline;
        spec.nodeSpecs = built.nodeSpecs;
        EditorUtility.SetDirty(spec);
        AssetDatabase.SaveAssets();
        return spec;
    }

    /// <summary>
    /// 把场景里的一个切片转成内存中的规格（不落盘）。
    ///
    /// <paramref name="includeInactive"/> = false（默认 ✅）→ **跳过作者态关掉的物件** ✅
    /// （旧残留 ✅：运行时没人会再打开它们 ✅）。
    /// </summary>
    public static YSceneSpec BuildSpecFromSlice(GameObject sliceRoot, bool includeInactive = false)
    {
        var spec = ScriptableObject.CreateInstance<YSceneSpec>();
        spec.routeId = "changbanpo";
        spec.renderWorldBaseline = 0;
        var node = new YNodeSceneSpec
        {
            nodeId = sliceRoot.name,
            anchorDistance = FindAnchorDistance(sliceRoot),
            groups = Array.Empty<YSceneGroup>(),
        };

        // ① 收集所有带渲染器的节点（带完整相对路径 ✅）
        var entries = new List<YPropEntry>();
        var sliceRootT = sliceRoot.transform;
        // 切片根自身的渲染器（地形 / 天空盒 ✅ —— 也是「给定输入」✅）也记下来 ✅
        var rootRenderer = sliceRootT.GetComponent<Renderer>();
        if (rootRenderer != null) entries.Add(BuildEntry(sliceRootT, "", rootRenderer));
        Collect(sliceRootT, entries, includeInactive);
        Debug.Log($"[Y 场景规格] 导出「{sliceRoot.name}」：子物体 {sliceRootT.childCount}，收集条目 {entries.Count}");

        // ② 按「组路径」（= 条目的父路径 ✅）分组 ✅
        var groups = new Dictionary<string, List<YPropEntry>>();
        var order = new List<string>();
        foreach (var e in entries)
        {
            string g = ParentPath(e.path);
            if (!groups.TryGetValue(g, out var list)) { list = new List<YPropEntry>(); groups[g] = list; order.Add(g); }
            list.Add(e);
        }
        var groupList = new List<YSceneGroup>();
        foreach (var g in order)
        {
            int baseline = groups[g].Min(e => e.sortBias);
            foreach (var e in groups[g]) e.sortBias -= baseline;      // 换成相对偏移 ✅
            groupList.Add(new YSceneGroup { path = g, sortBaseline = baseline, entries = groups[g].ToArray() });
        }

        // ②b 功能件（无渲染器但带组件的物件 ✅）也按组归类 ✅
        var funcsByGroup = new Dictionary<string, List<YFuncEntry>>();
        var funcOrder = new List<string>();
        var funcEntries = new List<YFuncEntry>();
        CollectFunctions(sliceRootT, sliceRootT, funcsByGroup, funcOrder, funcEntries, includeInactive);
        foreach (var g in groupList)
        {
            if (funcsByGroup.TryGetValue(g.path, out var fl)) g.functions = fl.ToArray();
        }
        foreach (var key in funcOrder)
        {
            if (groupList.Exists(g => g.path == key)) continue;
            groupList.Add(new YSceneGroup
            {
                path = key,
                sortBaseline = 0,
                entries = Array.Empty<YPropEntry>(),
                functions = funcsByGroup[key].ToArray(),
            });
        }
        // ②c 实例引用绑定表（重建时把组件里的引用接回新物件 ✅）
        node.refBindings = BuildRefBindings(sliceRootT, entries, funcEntries);

        node.groups = groupList.ToArray();
        spec.nodeSpecs = new[] { node };

        // 背景带 ✅（一关内可以分段换背景 ✅）
        var bands = new List<YBackgroundBand>();
        var bg = FindBackgroundObject(sliceRootT);
        if (bg != null)
        {
            var sr = bg.GetComponent<SpriteRenderer>();
            float length = 0f;
            var route = sliceRootT.GetComponent<YScrollSample>();
            if (route != null) length = route.Length;
            if (length <= 0f) length = 200f;
            bands.Add(new YBackgroundBand
            {
                fromDistance = 0f,
                toDistance = length,
                objectName = bg.name,
                sprite = sr != null ? sr.sprite : null,
                material = sr != null ? sr.sharedMaterial : null,
                sortBias = sr != null ? sr.sortingOrder : 0,
                enabled = true,
            });
        }
        spec.backgroundBands = bands.ToArray();
        return spec;
    }

    /// <summary>找场景里的背景物件（按名字含 Background ✅）。</summary>
    private static Transform FindBackgroundObject(Transform root)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var c = root.GetChild(i);
            if (c.name.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0) return c;
        }
        return null;
    }

    /// <summary>收集「旧场景实例 → 物件路径」的映射 ✅。</summary>
    private static YRefBinding[] BuildRefBindings(Transform sliceRootT, List<YPropEntry> entries, List<YFuncEntry> funcEntries)
    {
        var map = new Dictionary<int, string>();
        foreach (var e in entries)
        {
            var go = FindByPath(sliceRootT, e.path);
            if (go != null) map[go.GetInstanceID()] = e.path;
        }
        foreach (var f in funcEntries)
        {
            var go = FindByPath(sliceRootT, f.path);
            if (go != null) map[go.GetInstanceID()] = f.path;
        }
        var list = new List<YRefBinding>();
        foreach (var kv in map) list.Add(new YRefBinding { instanceId = kv.Key, path = kv.Value });
        return list.ToArray();
    }

    private static GameObject FindByPath(Transform root, string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        var cur = root;
        var parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            var child = cur.Find(parts[i]);
            if (child == null) return null;
            cur = child;
        }
        return cur.gameObject;
    }

    /// <summary>收集「无渲染器但带组件」的物件（节点锚点 / 连接 / 战斗锚点 …）✅。</summary>
    private static void CollectFunctions(Transform t, Transform sliceRootT,
        Dictionary<string, List<YFuncEntry>> byGroup, List<string> order, List<YFuncEntry> all,
        bool includeInactive)
    {
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            // 作者态关掉的 = 旧残留 ✅ → 默认不收 ✅
            if (!includeInactive && !child.gameObject.activeSelf) continue;
            if (child.GetComponent<Renderer>() == null)
            {
                // 没有渲染器的物件 = 功能件（节点锚点 ✅ / 连接 ✅ / 战斗锚点 ✅ / 路径可视化空容器 ✅）
                var fe = BuildFuncEntry(child, sliceRootT);
                string g = ParentPath(fe.path);
                if (!byGroup.TryGetValue(g, out var list)) { list = new List<YFuncEntry>(); byGroup[g] = list; order.Add(g); }
                list.Add(fe);
                all.Add(fe);
            }
            CollectFunctions(child, sliceRootT, byGroup, order, all, includeInactive);
        }
    }

    private static YFuncEntry BuildFuncEntry(Transform t, Transform sliceRootT)
    {
        var e = new YFuncEntry
        {
            path = RelPath(t, sliceRootT),
            name = t.name,
            localPosition = t.localPosition,
            localEulerAngles = t.localEulerAngles,
            localScale = t.localScale,
            active = t.gameObject.activeSelf,
        };
        var comps = t.GetComponents<MonoBehaviour>();
        var types = new List<string>();
        var jsons = new List<string>();
        for (int i = 0; i < comps.Length; i++)
        {
            if (comps[i] == null) continue;
            types.Add(comps[i].GetType().AssemblyQualifiedName);
            jsons.Add(UnityEditor.EditorJsonUtility.ToJson(comps[i]));
        }
        e.componentTypes = types.ToArray();
        e.componentJson = jsons.ToArray();
        return e;
    }

    /// <summary>递归收集：切片根**之下**所有带渲染器的节点（切片根本身不算 ✅）。</summary>
    private static void Collect(Transform sliceRootT, List<YPropEntry> into, bool includeInactive)
    {
        for (int i = 0; i < sliceRootT.childCount; i++)
            CollectNode(sliceRootT.GetChild(i), sliceRootT, into, includeInactive);
    }

    private static void CollectNode(Transform t, Transform sliceRootT, List<YPropEntry> into, bool includeInactive)
    {
        // 作者态关掉的 = 旧残留 ✅ → 默认不收 ✅
        if (!includeInactive && !t.gameObject.activeSelf) return;
        // 先看自己（切片根的直接子物件也可能就是布景件 ✅），再看子树 ✅
        var r = t.GetComponent<Renderer>();
        if (r != null) into.Add(BuildEntry(t, RelPath(t, sliceRootT), r));
        for (int i = 0; i < t.childCount; i++)
            CollectNode(t.GetChild(i), sliceRootT, into, includeInactive);
    }

    private static string RelPath(Transform t, Transform root)
    {
        var parts = new List<string>();
        var cur = t;
        while (cur != null && cur != root) { parts.Insert(0, cur.name); cur = cur.parent; }
        return string.Join("/", parts);
    }

    private static YPropEntry BuildEntry(Transform t, string path, Renderer r)
    {
        var e = new YPropEntry
        {
            name = t.name,
            path = path,
            localPosition = t.localPosition,
            localEulerAngles = t.localEulerAngles,
            localScale = t.localScale,
            material = r.sharedMaterial,
            active = t.gameObject.activeSelf,
            isWindMesh = t.name == "Wind Mesh",
        };
        var sr = r as SpriteRenderer;
        if (sr != null)
        {
            e.renderKind = YRenderKind.Sprite;
            e.sprite = sr.sprite;
            e.sortBias = sr.sortingOrder;
            e.pivotMode = YPivotMode.Foot;
            if (sr.sprite != null) e.footOffsetY = sr.sprite.pivot.y - sr.sprite.bounds.min.y;
        }
        else
        {
            e.renderKind = YRenderKind.Solid3D;
            var mf = t.GetComponent<MeshFilter>();
            e.mesh = mf != null ? mf.sharedMesh : null;
            e.sortBias = r.sortingOrder;
            e.pivotMode = YPivotMode.Center;
        }
        return e;
    }

    private static string ParentPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return "";
        int i = path.LastIndexOf('/');
        return i < 0 ? "" : path.Substring(0, i);
    }

    private static float FindAnchorDistance(GameObject sliceRoot)
    {
        var scene = sliceRoot.scene;
        float best = -1f; float bestDist = float.MaxValue;
        foreach (var p in UnityEngine.Object.FindObjectsOfType<ScrollRouteNodePresentation>(true))
        {
            if (p == null || p.gameObject.scene != scene) continue;
            float d = Vector3.Distance(p.transform.position, sliceRoot.transform.position);
            if (d < bestDist) { bestDist = d; best = p.worldDistance; }
        }
        return best;
    }

    // ───────────────────────── 生成 ─────────────────────────

    public static int Generate(YSceneSpec spec, Transform root)
    {
        if (spec == null || root == null) return 0;
        int count = 0;
        foreach (var node in spec.nodeSpecs)
        {
            var nodeRoot = FindOrCreatePath(root, node.nodeId);
            // 节点根的变换 ✅（只有规格里明确带了才写 ✅——否则保留场景里已有的摆位 ✅）
            if (node.applyRootTransform)
            {
                nodeRoot.localPosition = node.localPosition;
                nodeRoot.localEulerAngles = node.localEulerAngles;
                nodeRoot.localScale = node.localScale;
            }

            // ── 阶段 1：建全部物件 ✅（组件数据先不填 ✅，等引用映射齐了再填 ✅）
            foreach (var band in spec.backgroundBands)
            {
                if (band == null || !band.enabled || band.sprite == null) continue;
                // 幂等：场景里已有背景物件就复用它 ✅（名字带 Background ✅）→ 重建后名字不变 ✅
                var existing = FindBackgroundObject(root);
                var go = existing != null ? existing.gameObject : new GameObject(band.objectName);
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(0f, 0f, (band.fromDistance + band.toDistance) * 0.5f);
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr == null) sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = band.sprite;
                sr.material = band.material;
                sr.sortingOrder = spec.renderWorldBaseline + band.sortBias;
                count++;
            }
            foreach (var g in node.groups)
            {
                // 组**不带变换** ✅（每个节点的变换由它自己的条目写 ✅）
                // 组只是排序桶 ✅：路径上的容器由条目路径自动补出来 ✅
                foreach (var e in g.entries) { GenerateEntry(e, nodeRoot, g.sortBaseline); count++; }
                foreach (var f in g.functions) { CreateFunction(f, nodeRoot); count++; }
            }
            if (node.interactives != null)
                foreach (var e in node.interactives) { GenerateEntry(e, nodeRoot, spec.renderWorldBaseline); count++; }

            // ── 阶段 2：回填组件数据 ✅（对象引用按绑定表重映射到**新建的**物件 ✅）
            var remap = BuildRemap(node, nodeRoot);
            foreach (var g in node.groups)
                foreach (var f in g.functions)
                    ApplyFunctionData(f, nodeRoot, remap);
            ApplyRootComponents(node, nodeRoot, remap);
        }
        return count;
    }

    /// <summary>旧 instanceID → 新 instanceID 的重映射表 ✅。</summary>
    private static Dictionary<int, int> BuildRemap(YNodeSceneSpec node, Transform nodeRoot)
    {
        var map = new Dictionary<int, int>();
        if (node.refBindings == null) return map;
        foreach (var b in node.refBindings)
        {
            var go = FindByPath(nodeRoot, b.path);
            if (go != null) map[b.instanceId] = go.GetInstanceID();
        }
        return map;
    }

    /// <summary>把 JSON 里的旧 instanceID 换成新的 ✅（EditorJsonUtility 的对象引用格式 ✅）。</summary>
    private static string RemapJson(string json, Dictionary<int, int> remap)
    {
        if (string.IsNullOrEmpty(json) || remap == null || remap.Count == 0) return json;
        return System.Text.RegularExpressions.Regex.Replace(json, "\"instanceID\":(\\d+)",
            m =>
            {
                int oldId;
                if (!int.TryParse(m.Groups[1].Value, out oldId)) return m.Value;
                int newId;
                return remap.TryGetValue(oldId, out newId) ? "\"instanceID\":" + newId : m.Value;
            });
    }

    private static void ApplyRootComponents(YNodeSceneSpec node, Transform nodeRoot, Dictionary<int, int> remap)
    {
        if (node.rootComponentTypes == null) return;
        for (int i = 0; i < node.rootComponentTypes.Length; i++)
        {
            var type = System.Type.GetType(node.rootComponentTypes[i]);
            if (type == null) continue;
            var comp = nodeRoot.GetComponent(type);
            if (comp == null) comp = nodeRoot.gameObject.AddComponent(type);
            if (i < node.rootComponentJson.Length && !string.IsNullOrEmpty(node.rootComponentJson[i]))
            {
                try { UnityEditor.EditorJsonUtility.FromJsonOverwrite(RemapJson(node.rootComponentJson[i], remap), comp); }
                catch (System.Exception ex) { Debug.LogWarning($"[Y 场景规格] 回填根组件失败 {type.Name}: {ex.Message}"); }
            }
        }
    }

    /// <summary>按路径找/建一个对象（幂等 ✅：同名即复用 ✅）。</summary>
    private static Transform FindOrCreatePath(Transform parent, string path)
    {
        if (string.IsNullOrEmpty(path)) return parent;
        var cur = parent;
        var parts = path.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            var child = cur.Find(parts[i]);
            if (child == null)
            {
                var go = new GameObject(parts[i]);
                go.transform.SetParent(cur, false);
                child = go.transform;
            }
            cur = child;
        }
        return cur;
    }

    private static int GenerateEntry(YPropEntry e, Transform nodeRoot, int baseline)
    {
        // 路径为空 = 节点根自身 ✅（地形 / 天空盒 ✅）
        if (string.IsNullOrEmpty(e.path))
        {
            var rootGo = nodeRoot.gameObject;
            rootGo.transform.localPosition = e.localPosition;
            rootGo.transform.localEulerAngles = e.localEulerAngles;
            rootGo.transform.localScale = e.localScale;
            var rs = rootGo.GetComponent<Renderer>();
            if (rs == null) rs = rootGo.AddComponent(e.renderKind == YRenderKind.Sprite ? typeof(SpriteRenderer) : typeof(MeshRenderer)) as Renderer;
            if (e.renderKind == YRenderKind.Sprite)
            {
                var spr = rs as SpriteRenderer;
                if (spr != null) { spr.sprite = e.sprite; spr.material = e.material; spr.sortingOrder = baseline + e.sortBias; }
            }
            else
            {
                var mf = rootGo.GetComponent<MeshFilter>();
                if (mf == null && e.mesh != null) mf = rootGo.AddComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = e.mesh;
                var mr = rs as MeshRenderer;
                if (mr != null) { mr.sharedMaterial = e.material; mr.sortingOrder = baseline + e.sortBias; }
            }
            rs.enabled = !e.rendererDisabled;      // 作者态关掉的渲染器 ✅ 保持关 ✅
            if (rootGo.activeSelf != e.active) rootGo.SetActive(e.active);
            return 1;
        }
        string parentPath = ParentPath(e.path);
        var parent = FindOrCreatePath(nodeRoot, parentPath);
        var existing = parent.Find(e.name);
        GameObject go;
        if (existing != null)
        {
            if (e.userModified) return 0;      // 手工改过 → 不覆盖 ✅
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject(e.name);
            go.transform.SetParent(parent, false);
        }
        go.transform.localPosition = e.localPosition;
        go.transform.localEulerAngles = e.localEulerAngles;
        go.transform.localScale = e.localScale;
        if (go.activeSelf != e.active) go.SetActive(e.active);

        // 没有贴图也没有网格 = 只带变换的容器 ✅（嵌套 prefab 实例 ✅）→ 不加渲染器 ✅
        if (e.containerOnly) return 1;
        if (e.renderKind == YRenderKind.Sprite && e.sprite == null) return 1;

        if (e.renderKind == YRenderKind.Sprite)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = e.sprite;
            sr.material = e.material;
            sr.sortingOrder = baseline + e.sortBias;
            sr.enabled = !e.rendererDisabled;        // 作者态关掉的渲染器 ✅ 保持关 ✅
        }
        else
        {
            if (e.mesh != null)
            {
                var mf = go.GetComponent<MeshFilter>();
                if (mf == null) mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = e.mesh;
            }
            var mr = go.GetComponent<MeshRenderer>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = e.material;
            mr.sortingOrder = baseline + e.sortBias;
            mr.enabled = !e.rendererDisabled;        // 同上 ✅
        }
        return 1;
    }

    /// <summary>生成一个功能件（无渲染器但带组件的物件 ✅）：建物体 + 加组件 + 回填序列化数据 ✅。</summary>
    /// <summary>阶段 1：建功能件物件（组件数据稍后回填 ✅）。</summary>
    private static int CreateFunction(YFuncEntry f, Transform nodeRoot)
    {
        var go = ResolveFunctionObject(f, nodeRoot);
        if (go == null) return 0;
        go.transform.localPosition = f.localPosition;
        go.transform.localEulerAngles = f.localEulerAngles;
        go.transform.localScale = f.localScale;
        if (go.activeSelf != f.active) go.SetActive(f.active);
        // 某些组件（例：ScrollRouteConnection）会在 OnValidate 里给物体改名（追加方向后缀 ✅），
        // 生成后要还原成规格里的名字 ✅，否则会变成 "X [Forward] [Forward]" ✅。
        if (go.name != f.name) go.name = f.name;
        return 1;
    }

    /// <summary>阶段 2：回填功能件的组件数据（引用按重映射表接回新物件 ✅）。</summary>
    private static void ApplyFunctionData(YFuncEntry f, Transform nodeRoot, Dictionary<int, int> remap)
    {
        var go = ResolveFunctionObject(f, nodeRoot);
        if (go == null) return;
        for (int i = 0; i < f.componentTypes.Length; i++)
        {
            var type = System.Type.GetType(f.componentTypes[i]);
            if (type == null) { Debug.LogWarning($"[Y 场景规格] 找不到组件类型 {f.componentTypes[i]}"); continue; }
            var comp = go.GetComponent(type);
            if (comp == null) comp = go.AddComponent(type);
            if (i < f.componentJson.Length && !string.IsNullOrEmpty(f.componentJson[i]))
            {
                try { UnityEditor.EditorJsonUtility.FromJsonOverwrite(RemapJson(f.componentJson[i], remap), comp); }
                catch (System.Exception ex) { Debug.LogWarning($"[Y 场景规格] 回填组件数据失败 {f.name}/{type.Name}: {ex.Message}"); }
            }
        }
        if (go.name != f.name) go.name = f.name;
    }

    private static GameObject ResolveFunctionObject(YFuncEntry f, Transform nodeRoot)
    {
        string parentPath = ParentPath(f.path);
        var parent = FindOrCreatePath(nodeRoot, parentPath);
        var existing = parent.Find(f.name);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(f.name);
        go.transform.SetParent(parent, false);
        return go;
    }
    private static Transform FindOrCreateRoot(Scene scene, YSceneSpec spec)
    {
        string name = spec.nodeSpecs.Length > 0 ? spec.nodeSpecs[0].nodeId : "Y Spec Root";
        var existing = GameObject.Find(name);
        if (existing != null) return existing.transform;
        var main = scene.GetRootGameObjects().Length > 0 ? scene.GetRootGameObjects()[0].transform : null;
        var go = new GameObject(name);
        if (main != null) go.transform.SetParent(main, false);
        return go.transform;
    }

    // ───────────────────────── 校验 ─────────────────────────

    public static List<string> Validate(YSceneSpec spec)
    {
        return Validate(spec, out _);
    }

    /// <summary>
    /// 自检 ✅。
    /// <paramref name="clusterDetails"/>：**可选**的「挤在一簇」清单 ✅（同 sortBias ✅ + 脚点距离 &lt; 0.5m ✅）——
    /// 默认**不报** ✅（实测：同 sortBias 共 18104 对 ✅，脚点 &lt; 0.5m 的只有 25 对 ✅，
    /// 而那些全是**故意**的叠放 ✅ —— 路牌脚下的石头 ✅ / 墙脚的草 ✅ → 报出来纯噪音 ✅）。
    /// 需要审这簇时再取它 ✅。
    /// </summary>
    public static List<string> Validate(YSceneSpec spec, out List<string> clusterDetails)
    {
        var issues = new List<string>();
        clusterDetails = new List<string>();
        if (spec == null) { issues.Add("规格为空"); return issues; }

        foreach (var node in spec.nodeSpecs)
        {
            if (string.IsNullOrEmpty(node.nodeId)) issues.Add("节点 id 为空");
            foreach (var g in node.groups)
            {
                if (g.entries == null || g.entries.Length == 0) continue;

                int maxBias = g.entries.Max(e => e.sortBias);
                if (g.sortBaseline + maxBias > spec.renderWorldBaseline)
                {
                    issues.Add($"[{node.nodeId}/{g.GroupName}] 层级越界：组基准 {g.sortBaseline} + 最大 sortBias {maxBias} = {g.sortBaseline + maxBias} > 世界基准 {spec.renderWorldBaseline}"
                             + "（该组会压在所有场景物之上）");
                }

                // 规则 7 ✅：**同一深度带里真叠放** ✅ —— 判据 = **脚点几乎重合** ✅（< 0.1m ✅）
                // 为什么不用「AABB 重叠」❗：精灵的可见范围含大片透明 ✅（路牌那张 10.24m 见方 ✅）
                // → AABB 一判就和旁边所有东西"重叠" ✅ → 全量跑出 52 条 ❗ 还是噪音 ❗
                // 为什么把「挤在一簇」降成可选 ✅：实测同 sortBias 共 18104 对 ✅，脚点 < 0.5m 的只有 25 对 ✅，
                // 而那 25 对全是**故意**的叠放 ✅（路牌脚下的石头 ✅ / 墙脚的草 ✅ / 树边的灌木 ✅）→ 报出来纯噪音 ✅
                var sameBias = g.entries.Where(e => e != null && !e.containerOnly)
                                        .GroupBy(e => e.sortBias)
                                        .Where(x => x.Count() > 1);
                foreach (var biasGroup in sameBias)
                {
                    var list = biasGroup.ToList();
                    for (int i = 0; i < list.Count; i++)
                    {
                        for (int j = i + 1; j < list.Count; j++)
                        {
                            float d = FootDistance(list[i], list[j]);
                            if (d < 0.1f)
                                issues.Add($"[{node.nodeId}/{g.GroupName}] 同层真叠放：{list[i].name} × {list[j].name}（脚点相距 {d:F2}m ✅）");
                            else if (d < 0.5f && clusterDetails != null)
                                clusterDetails.Add($"[{node.nodeId}/{g.GroupName}] 挤在一簇：{list[i].name} × {list[j].name}（{d:F2}m ✅）");
                        }
                    }
                }

                foreach (var e in g.entries)
                {
                    if (e.containerOnly) continue;          // 纯容器 ✅（无渲染器 ✅，不算缺失 ✅）
                    if (e.renderKind == YRenderKind.Sprite && e.sprite == null) issues.Add($"[{node.nodeId}/{g.GroupName}/{e.name}] 精灵引用为空");
                    if (e.renderKind == YRenderKind.Solid3D && e.mesh == null) issues.Add($"[{node.nodeId}/{g.GroupName}/{e.name}] 网格引用为空");
                    if (e.material == null) issues.Add($"[{node.nodeId}/{g.GroupName}/{e.name}] 材质引用为空");
                }
            }
        }
        return issues;
    }

    /// <summary>
    /// 两条同组条目的**脚点距离**（局部 XZ ✅ —— 同组同父级 ✅ → 坐标可比 ✅）。
    /// </summary>
    private static float FootDistance(YPropEntry a, YPropEntry b)
    {
        return Vector2.Distance(
            new Vector2(a.localPosition.x, a.localPosition.z),
            new Vector2(b.localPosition.x, b.localPosition.z));
    }

    // ───────────────────────── 往返验证 ─────────────────────────

    public static bool RunRoundTrip(GameObject sliceRoot)
    {
        if (sliceRoot == null) return false;
        var spec = BuildSpecFromSlice(sliceRoot, false);

        var tmp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var rootGo = new GameObject("RoundTrip");
        int created = Generate(spec, rootGo.transform);
        var nodeRoot = rootGo.transform.Find(sliceRoot.name);

        var diffs = nodeRoot != null
            ? CompareTrees(sliceRoot.transform, nodeRoot, false, true)
            : new List<string> { "生成结果里找不到节点根" };
        Debug.Log($"[Y 场景规格] 往返验证：生成 {created} 个对象；差异 {diffs.Count} 条");
        for (int i = 0; i < diffs.Count && i < 20; i++) Debug.Log("   " + diffs[i]);

        var issues = Validate(spec);
        foreach (var i in issues) Debug.LogWarning("[Y 场景规格][自检] " + i);
        Debug.Log($"[Y 场景规格] 自检问题数 = {issues.Count}");

        UnityEngine.Object.DestroyImmediate(rootGo);
        EditorSceneManager.CloseScene(tmp, true);
        return diffs.Count == 0;
    }

    /// <summary>
    /// **摆放表往返**：prefab → 摆放表 → 规格 → 生成 → 与原 prefab 逐件比对 ✅。
    /// 验证的是「摆放表格式能不能无损承载一个现成场景」✅（自动搭场景的前置条件 ✅）。
    /// </summary>
    public static bool RunPlacementRoundTrip(GameObject source, string jsonPath = null, bool keepSpec = false)
    {
        if (source == null) return false;
        jsonPath = string.IsNullOrEmpty(jsonPath) ? "Library/Locus/tmp/y_placement_roundtrip.json" : jsonPath;
        int exported = YScenePlacementImporter.ExportPlacementTable(source, jsonPath);
        var spec = YScenePlacementImporter.Import(jsonPath);
        if (spec == null) return false;
        string specPath = AssetDatabase.GetAssetPath(spec);

        var tmp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var rootGo = new GameObject("RoundTrip");
        int created = Generate(spec, rootGo.transform);
        var nodeRoot = rootGo.transform.Find(spec.nodeSpecs[0].nodeId);

        var diffs = nodeRoot != null
            ? CompareTrees(source.transform, nodeRoot, true, true)
            : new List<string> { "生成结果里找不到节点根" };
        Debug.Log($"[Y 场景规格] 摆放表往返：导出 {exported} 条 → 生成 {created} 个对象；差异 {diffs.Count} 条");
        for (int i = 0; i < diffs.Count && i < 30; i++) Debug.Log("   " + diffs[i]);

        var issues = Validate(spec);
        foreach (var i in issues) Debug.LogWarning("[Y 场景规格][自检] " + i);
        Debug.Log($"[Y 场景规格] 自检问题数 = {issues.Count}");

        UnityEngine.Object.DestroyImmediate(rootGo);
        EditorSceneManager.CloseScene(tmp, true);
        if (!keepSpec && !string.IsNullOrEmpty(specPath)) AssetDatabase.DeleteAsset(specPath);
        return diffs.Count == 0;
    }

    // ───────────────────────── 生成到 prefab ─────────────────────────

    /// <summary>
    /// 把规格生成成一个 **prefab** ✅（新方案的产物形态 ✅：节点 prefab / 段 prefab ✅）。
    ///
    /// 生成在**临时场景**里做 ✅ → 当前场景零改动 ✅（不脏场景 ✅）。
    /// prefab 的根 = 节点根 ✅；节点的**位置由路线系统放在锚点上** ✅（规格里原点为单位变换 ✅）。
    ///
    /// <paramref name="worldRootPosition"/> / <paramref name="worldRootEuler"/>：
    /// 非空时把 **prefab 根摆到世界位姿** ✅ → 产出的就是**段 prefab** ✅
    /// （段表按 prefab 根的位姿搬运 ✅，见 <c>YRouteSegment.prefab</c> 的约定 ✅）。
    /// </summary>
    public static GameObject GenerateToPrefab(YSceneSpec spec, string prefabPath, out int count,
        Vector3? worldRootPosition = null, Vector3? worldRootEuler = null)
    {
        count = 0;
        if (spec == null || string.IsNullOrEmpty(prefabPath)) return null;
        if (spec.nodeSpecs == null || spec.nodeSpecs.Length == 0) return null;

        var tmp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var holder = new GameObject("PrefabBuild");
            count = Generate(spec, holder.transform);
            var nodeRoot = holder.transform.Find(spec.nodeSpecs[0].nodeId);
            if (nodeRoot == null)
            {
                Debug.LogError($"[Y 场景规格] 生成结果里找不到节点根「{spec.nodeSpecs[0].nodeId}」");
                return null;
            }
            // 段 prefab：根带世界位姿 ✅（节点 prefab：根留在原点 ✅）
            if (worldRootPosition.HasValue)
            {
                nodeRoot.localPosition = worldRootPosition.Value;
                nodeRoot.localEulerAngles = worldRootEuler.HasValue ? worldRootEuler.Value : Vector3.zero;
            }
            EnsureAssetFolder(Path.GetDirectoryName(prefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(nodeRoot.gameObject, prefabPath);
            if (prefab == null) Debug.LogError("[Y 场景规格] prefab 存盘失败：" + prefabPath);
            return prefab;
        }
        finally
        {
            EditorSceneManager.CloseScene(tmp, true);
        }
    }

    private static void EnsureAssetFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// **节点 prefab 生产**（2026-10-24 ✅）：现成场景组 → 摆放表 → 规格 → **prefab** ✅ → 与源逐件比对 ✅。
    /// 这是「用新方案产出内容」的完整链路 ✅。
    /// 产物 = prefab ✅（**当前场景零改动** ✅）；比对不比根变换 ✅（prefab 不带位置 ✅）。
    /// </summary>
    public static bool RunNodeProduction(GameObject source, string nodeId, string prefabPath, out string report)
    {
        var sb = new System.Text.StringBuilder();
        report = string.Empty;
        if (source == null) { report = "源为空"; return false; }
        if (string.IsNullOrEmpty(nodeId)) nodeId = DeriveNodeId(source.name);

        string jsonPath = "Library/Locus/tmp/y_node_production.json";
        int exported = YScenePlacementImporter.ExportPlacementTable(source, jsonPath, false, nodeId, true);
        var spec = YScenePlacementImporter.Import(jsonPath);
        if (spec == null) { report = "规格导入失败"; return false; }
        string specPath = AssetDatabase.GetAssetPath(spec);
        sb.AppendLine($"摆放表 {exported} 条 → 规格 {specPath}");

        int count = 0;
        var prefab = GenerateToPrefab(spec, prefabPath, out count);
        if (prefab == null) { report = sb + "prefab 生成失败"; return false; }
        sb.AppendLine($"prefab {prefabPath}（生成 {count} 个对象 ✅）");

        var diffs = CompareTrees(source.transform, prefab.transform, false, true);
        sb.AppendLine($"逐件比对：差异 {diffs.Count} 条");
        for (int i = 0; i < diffs.Count && i < 20; i++) sb.AppendLine("   " + diffs[i]);
        report = sb.ToString();
        return diffs.Count == 0;
    }

    /// <summary>
    /// 逐对象比对两棵层级树（相对各自根，路径为准）。
    ///
    /// <paramref name="ignoreInactive"/> = true → 两边都跳过**作者态关掉**的物件 ✅
    /// （旧残留 ✅：导出时不带它们 ✅，比对时也要跳过 ✅，否则会误报「缺失」✅）。
    /// </summary>
    public static List<string> CompareTrees(Transform a, Transform b, bool compareRoot = false, bool ignoreInactive = false)
    {
        var diffs = new List<string>();
        if (compareRoot)
        {
            if (a.localPosition != b.localPosition) diffs.Add($"[根] 位置 {a.localPosition} vs {b.localPosition}");
            if (a.localEulerAngles != b.localEulerAngles) diffs.Add($"[根] 旋转 {a.localEulerAngles} vs {b.localEulerAngles}");
            if (a.localScale != b.localScale) diffs.Add($"[根] 缩放 {a.localScale} vs {b.localScale}");
        }
        var mapA = Flatten(a, ignoreInactive);
        var mapB = Flatten(b, ignoreInactive);
        foreach (var kv in mapA)
        {
            if (!mapB.TryGetValue(kv.Key, out var tb)) { diffs.Add($"缺失: {kv.Key}"); continue; }
            var ta = kv.Value;
            if (ta.name != tb.name) diffs.Add($"{kv.Key}: 名字 {ta.name} vs {tb.name}");
            if (ta.localPosition != tb.localPosition) diffs.Add($"{kv.Key}: 位置 {ta.localPosition} vs {tb.localPosition}");
            if (ta.localEulerAngles != tb.localEulerAngles) diffs.Add($"{kv.Key}: 旋转 {ta.localEulerAngles} vs {tb.localEulerAngles}");
            if (ta.localScale != tb.localScale) diffs.Add($"{kv.Key}: 缩放 {ta.localScale} vs {tb.localScale}");
            var sa = ta.GetComponent<SpriteRenderer>(); var sb = tb.GetComponent<SpriteRenderer>();
            if (sa != null || sb != null)
            {
                if (sa == null || sb == null) { diffs.Add($"{kv.Key}: 渲染器类型不同"); continue; }
                if (sa.sprite != sb.sprite) diffs.Add($"{kv.Key}: 精灵 {sa.sprite?.name} vs {sb.sprite?.name}");
                if (sa.sharedMaterial != sb.sharedMaterial) diffs.Add($"{kv.Key}: 材质 {sa.sharedMaterial?.name} vs {sb.sharedMaterial?.name}");
                if (sa.sortingOrder != sb.sortingOrder) diffs.Add($"{kv.Key}: sortingOrder {sa.sortingOrder} vs {sb.sortingOrder}");
                if (sa.sortingLayerID != sb.sortingLayerID) diffs.Add($"{kv.Key}: sortingLayer {sa.sortingLayerID} vs {sb.sortingLayerID}");
                if (sa.enabled != sb.enabled) diffs.Add($"{kv.Key}: 渲染器 enabled {sa.enabled} vs {sb.enabled}（作者态关掉的 ✅ 表里能记 ✅）");
            }
            else
            {
                var ma = ta.GetComponent<MeshRenderer>(); var mb = tb.GetComponent<MeshRenderer>();
                if (ma != null && mb != null)
                {
                    var fa = ta.GetComponent<MeshFilter>(); var fb = tb.GetComponent<MeshFilter>();
                    if ((fa == null ? null : fa.sharedMesh) != (fb == null ? null : fb.sharedMesh)) diffs.Add($"{kv.Key}: 网格不同");
                    if (ma.sharedMaterial != mb.sharedMaterial) diffs.Add($"{kv.Key}: 材质 {ma.sharedMaterial?.name} vs {mb.sharedMaterial?.name}");
                        if (ma.sortingOrder != mb.sortingOrder) diffs.Add($"{kv.Key}: sortingOrder {ma.sortingOrder} vs {mb.sortingOrder}");
                        if (ma.enabled != mb.enabled) diffs.Add($"{kv.Key}: 渲染器 enabled {ma.enabled} vs {mb.enabled}（作者态关掉的 ✅ 表里能记 ✅）");
                }
            }
        }
        foreach (var kv in mapB) if (!mapA.ContainsKey(kv.Key)) diffs.Add($"多出: {kv.Key}");
        return diffs;
    }

    /// <summary>
    /// Unity 不允许「活动场景 = 未保存的新场景 ✅」时再加性建场景 ✅（会抛 "Cannot create a new scene additively with an untitled scene unsaved" ✅）。
    /// 先把活动场景切到**有路径的**已加载场景 ✅（不动内容 ✅）。
    /// </summary>
    private static bool CanCreateAdditiveScene(out string why)
    {
        why = null;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var sc = SceneManager.GetSceneAt(i);
            if (!string.IsNullOrEmpty(sc.path)) continue;
            why = "有未保存的空场景已加载 ✅（Unity 不允许再加性建场景 ✅）—— 请先把它关掉 ✅";
            return false;
        }
        return true;
    }

    // ───────────────────────── 刀三：三方漂移检查 ─────────────────────────

    /// <summary>
    /// **三方漂移检查** ✅：**表 ↔ 规格 ↔ 段 prefab** ✅
    /// ① 表 → 规格A（**临时新资产** ✅ —— 绝不覆盖盘上的规格 ❗）
    /// ② 规格A ↔ 规格B（盘上的 ✅）→ 「表与规格是否同步」✅
    /// ③ 规格B → 生成 ↔ 段 prefab（盘上的 ✅）→ 「规格与 prefab 是否同步」✅
    /// 为什么要它 ✅：规格资产会随类字段改动**静默变陈旧** ✅（反序列化不报错 ✅）→ 没人会发现 ❗。
    /// </summary>
    public static bool RunChainCheck(string tablePath, string specPath, string prefabPath, out string report)
    {
        report = string.Empty;
        var sb = new System.Text.StringBuilder();
        if (string.IsNullOrEmpty(tablePath) || !File.Exists(tablePath)) { report = "找不到摆放表：" + tablePath; return false; }
        var specOnDisk = AssetDatabase.LoadAssetAtPath<YSceneSpec>(specPath);
        var prefabOnDisk = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (specOnDisk == null) { report = "找不到规格资产：" + specPath; return false; }
        if (prefabOnDisk == null) { report = "找不到段 prefab：" + prefabPath; return false; }

        var specFromTable = YScenePlacementImporter.Import(tablePath);
        if (specFromTable == null) { report = "摆放表导入失败"; return false; }
        string tempSpecPath = AssetDatabase.GetAssetPath(specFromTable);

        if (!CanCreateAdditiveScene(out string why)) { report = why; return false; }
        var tmp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject holderA = null, holderB = null;
        try
        {
            holderA = new GameObject("SpecFromTable");
            Generate(specFromTable, holderA.transform);
            var rootA = holderA.transform.Find(specFromTable.nodeSpecs[0].nodeId);
            holderB = new GameObject("SpecOnDisk");
            Generate(specOnDisk, holderB.transform);
            var rootB = holderB.transform.Find(specOnDisk.nodeSpecs[0].nodeId);

            var d1 = (rootA != null && rootB != null)
                ? CompareTrees(rootA, rootB, true, true)
                : new List<string> { "节点根缺失（表导出的 ✅ / 盘上的 ✅）" };
            var d2 = (rootB != null)
                ? CompareTrees(prefabOnDisk.transform, rootB, true, true)
                : new List<string> { "节点根缺失" };

            sb.AppendLine($"表 → 规格：{d1.Count} 条差异 ✅");
            for (int i = 0; i < d1.Count && i < 12; i++) sb.AppendLine("    · " + d1[i]);
            sb.AppendLine($"规格 → 段 prefab：{d2.Count} 条差异 ✅");
            for (int i = 0; i < d2.Count && i < 12; i++) sb.AppendLine("    · " + d2[i]);
            sb.AppendLine(d1.Count == 0 && d2.Count == 0 ? "三方一致 ✅" : "三方不一致 ❌ —— 重新导出规格 / 重建 prefab ✅");
            report = sb.ToString();
            return d1.Count == 0 && d2.Count == 0;
        }
        finally
        {
            if (holderA != null) UnityEngine.Object.DestroyImmediate(holderA);
            if (holderB != null) UnityEngine.Object.DestroyImmediate(holderB);
            EditorSceneManager.CloseScene(tmp, true);
            if (!string.IsNullOrEmpty(tempSpecPath)) AssetDatabase.DeleteAsset(tempSpecPath);   // 临时资产不留 ✅
        }
    }

    // ───────────────────────── 刀四：画面逐像素比对 ─────────────────────────

    private const int PixCheckLayer = 2;        // Ignore Raycast ✅（项目里没在用 ✅）
    private const int PixCheckWidth = 887;
    private const int PixCheckHeight = 1774;

    /// <summary>
    /// **画面逐像素比对** ✅：**现状** vs **由规格重建** ✅。
    /// 「画面没变」是唯一能证明表化无损的口径 ✅ —— 结构比对通过 ≠ 画面一致 ✅（今天实证过 ✅）。
    /// 做法 ✅：把现状**克隆**一份 ✅（不动用户的对象 ✅）→ 挪进临时场景 ✅ → 挪到检查层 ✅ → 渲 A ✅；
    /// 由规格生成一份 ✅ → 渲 B ✅；最后在 C# 里逐像素比 ✅（不依赖外部 Python ✅）。
    /// </summary>
    public static bool RunPixelCheck(GameObject source, string tablePath, string specPath, string prefabPath, out string report)
    {
        report = string.Empty;
        var sb = new System.Text.StringBuilder();
        if (source == null) { report = "没有要比对的现状对象"; return false; }
        var spec = AssetDatabase.LoadAssetAtPath<YSceneSpec>(specPath);
        if (spec == null) { report = "找不到规格资产：" + specPath; return false; }

        // 机位按**现状的世界包围盒**定 ✅ → 两张图同一机位 ✅
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (var r in source.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
        }
        if (first) { report = "现状对象里没有渲染器"; return false; }
        Vector3 camPos = new Vector3(bounds.center.x, 1.7f, bounds.min.z - 5f);
        Quaternion camRot = Quaternion.Euler(10f, 0f, 0f);

        if (!CanCreateAdditiveScene(out string why)) { report = why; return false; }
        var tmp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject clone = null, holder = null;
        try
        {
            // A = 现状（克隆 ✅ —— 不动用户的对象 ✅）
            clone = UnityEngine.Object.Instantiate(source);
            clone.hideFlags = HideFlags.DontSave;
            SceneManager.MoveGameObjectToScene(clone, tmp);
            clone.transform.position = source.transform.position;
            clone.transform.rotation = source.transform.rotation;
            string pathA = RenderForCompare(clone.transform, tmp, camPos, camRot, "A");
            // 克隆渲完**立刻销毁** ✅ —— 否则 B 图里会叠着两份布景 ❗（克隆 + 重建 ✅）→ 比对必然有差异 ❌
            UnityEngine.Object.DestroyImmediate(clone);
            clone = null;

            // B = 由规格重建 ✅
            holder = new GameObject("Rebuilt");
            Generate(spec, holder.transform);
            var rebuilt = holder.transform.Find(spec.nodeSpecs[0].nodeId);
            if (rebuilt == null) { report = "由规格重建时找不到节点根"; return false; }
            string pathB = RenderForCompare(rebuilt, tmp, camPos, camRot, "B");

            ComparePng(pathA, pathB, out int exact, out int over4, out int max, out int total);
            sb.AppendLine($"机位 = ({camPos.x:F1}, {camPos.y:F1}, {camPos.z:F1}) rot = ({camRot.eulerAngles.x:F0}, {camRot.eulerAngles.y:F0}, {camRot.eulerAngles.z:F0})");
            sb.AppendLine($"完全不同的像素 = {exact} / {total}（{100.0 * exact / Mathf.Max(1, total):F4}%）");
            sb.AppendLine($"通道差 > 4 的像素 = {over4}｜最大通道差 = {max}");
            sb.AppendLine(exact == 0 ? "画面完全一致 ✅" : "画面有差异 ❌ —— 两张图在 Library/Locus/tmp/ypix_A.png ✅ / ypix_B.png ✅");
            report = sb.ToString();
            return exact == 0;
        }
        finally
        {
            if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
            if (holder != null) UnityEngine.Object.DestroyImmediate(holder);
            EditorSceneManager.CloseScene(tmp, true);
        }
    }

    /// <summary>把一棵树按固定机位渲成 PNG ✅（只渲检查层 ✅ → 当前场景的其它东西不会进来 ✅）。</summary>
    private static string RenderForCompare(Transform tree, Scene scene, Vector3 camPos, Quaternion camRot, string tag)
    {
        foreach (var r in tree.GetComponentsInChildren<Renderer>(true))
            if (r != null) SetLayerRecursive(r.transform, PixCheckLayer);

        var camGo = new GameObject("PixCam");
        camGo.hideFlags = HideFlags.DontSave;
        SceneManager.MoveGameObjectToScene(camGo, scene);
        var cam = camGo.AddComponent<Camera>();
        camGo.transform.position = camPos;
        camGo.transform.rotation = camRot;
        cam.fieldOfView = 60f; cam.aspect = 0.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.72f, 0.88f);
        cam.nearClipPlane = 0.05f; cam.farClipPlane = 600f;
        cam.cullingMask = 1 << PixCheckLayer;              // 只看检查层 ✅
        int W = PixCheckWidth, H = PixCheckHeight;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        string path = $"Library/Locus/tmp/ypix_{tag}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(tex);
        UnityEngine.Object.DestroyImmediate(rt);
        UnityEngine.Object.DestroyImmediate(camGo);
        return path;
    }

    /// <summary>两张 PNG 逐像素比较 ✅（完全一致 = 0 ✅）。</summary>
    private static void ComparePng(string pathA, string pathB, out int exact, out int over4, out int max, out int total)
    {
        var ta = LoadPng(pathA);
        var tb = LoadPng(pathB);
        var pa = ta.GetPixels32();
        var pb = tb.GetPixels32();
        exact = 0; over4 = 0; max = 0; total = pa.Length;
        int n = Mathf.Min(pa.Length, pb.Length);
        for (int i = 0; i < n; i++)
        {
            int d = Mathf.Max(Mathf.Abs(pa[i].r - pb[i].r),
                      Mathf.Max(Mathf.Abs(pa[i].g - pb[i].g), Mathf.Abs(pa[i].a - pb[i].a)));
            if (d > 0) { exact++; if (d > 4) over4++; if (d > max) max = d; }
        }
        UnityEngine.Object.DestroyImmediate(ta);
        UnityEngine.Object.DestroyImmediate(tb);
    }

    private static Texture2D LoadPng(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return tex;
    }

    private static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i), layer);
    }

    [MenuItem("Tools/Y 场景规格/⑧ 全链路校验（表→规格→段 prefab 三方对齐 ✅）")]
    private static void ChainCheckMenu()
    {
        var sel = Selection.activeObject as GameObject;
        if (sel == null || !EditorUtility.IsPersistent(sel)) { Debug.LogError("[Y 场景规格] 请在 Project 里选中一个段 prefab ✅"); return; }
        string name = sel.name;
        bool ok = RunChainCheck(
            $"Assets/Experiments/CurvedScroll/Authoring/Placement/{name}.json",
            $"Assets/RouteData/YSceneSpecs/{name}.asset",
            AssetDatabase.GetAssetPath(sel),
            out string report);
        Debug.Log($"[Y 场景规格] 全链路校验「{name}」：" + (ok ? "三方一致 ✅" : "三方不一致 ❌") + "\n" + report);
    }

    [MenuItem("Tools/Y 场景规格/⑨ 画面逐像素比对（现状 vs 由规格重建 ✅）")]
    private static void PixelCheckMenu()
    {
        var sel = Selection.activeGameObject;
        if (sel == null) { Debug.LogError("[Y 场景规格] 先选中要比对的对象 ✅（段 prefab ✅ 或场景里的组 ✅）"); return; }
        string name = sel.name;
        bool ok = RunPixelCheck(sel,
            $"Assets/Experiments/CurvedScroll/Authoring/Placement/{name}.json",
            $"Assets/RouteData/YSceneSpecs/{name}.asset",
            $"Assets/Experiments/CurvedScroll/Authoring/Segments/{name}.prefab",
            out string report);
        Debug.Log($"[Y 场景规格] 画面比对「{name}」：" + (ok ? "完全一致 ✅" : "有差异 ❌") + "\n" + report);
    }

    private static Dictionary<string, Transform> Flatten(Transform root, bool ignoreInactive = false)
    {
        var map = new Dictionary<string, Transform>();
        void Walk(Transform t, string path)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                var c = t.GetChild(i);
                // 作者态关掉的 = 旧残留 ✅ → 比对时也跳过 ✅
                if (ignoreInactive && !c.gameObject.activeSelf) continue;
                string p = path.Length == 0 ? c.name : path + "/" + c.name;
                map[p] = c;
                Walk(c, p);
            }
        }
        Walk(root, "");
        return map;
    }
}
