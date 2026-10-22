using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// **表化这个组** ✅：把场景里手摆的一组布景接进表驱动流水线 ✅。
///
/// 一条命令做完 ✅（原来要 8 步手工 ✅ + 2 个坑 ✅）：
///   ① 导出摆放表 ✅ → ② 导入规格资产 ✅ → ③ 生成段 prefab ✅
///   → ④ 场景里换成该 prefab 的实例 ✅（**删旧的 ✅ —— 必须传 GameObject ✅**）→ ⑤ 外部引用重指向 ✅
///   → ⑥ 三方对齐校验 ✅ → ⑦ 画面逐像素校验 ✅
///
/// 安全边界 ✅：只动**这一组** ✅（别的组 ✅ / 功能件 ✅ / 地形 ✅ 一概不碰 ✅）；
/// 旧组是**删**了换成 prefab 实例 ✅ —— 万一不对 ✅，`git checkout` 场景文件即可回退 ✅。
/// </summary>
public sealed class YSceneTableizeWindow : EditorWindow
{
    private GameObject _source;               // 场景里要表化的那一组 ✅
    private string _nodeId = "";              // 表 / 规格 / 段 prefab 的命名 ✅
    private bool _tableOnly = false;          // 只导出表 ✅（不动场景 ✅）
    private Vector2 _scroll;
    private readonly List<string> _log = new List<string>();

    private const string PlacementFolder = "Assets/Experiments/CurvedScroll/Authoring/Placement";
    private const string SegmentFolder = "Assets/Experiments/CurvedScroll/Authoring/Segments";
    private const string SpecFolder = "Assets/RouteData/YSceneSpecs";

    [MenuItem("Tools/Y 场景规格/⑩ 表化这个组…")]
    private static void Open()
    {
        var w = GetWindow<YSceneTableizeWindow>();
        w.titleContent = new GUIContent("Y 表化这个组");
        w._source = Selection.activeGameObject;
        w._nodeId = w._source != null ? w._source.name : "";
        w.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("把场景里手摆的一组布景接进表驱动流水线 ✅", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            _source = (GameObject)EditorGUILayout.ObjectField("场景里的那一组", _source, typeof(GameObject), true);
            if (GUILayout.Button("用当前选中 ✅", GUILayout.Width(110)))
            {
                _source = Selection.activeGameObject;
                _nodeId = _source != null ? _source.name : _nodeId;
            }
        }
        _nodeId = EditorGUILayout.TextField("表 / 规格 / 段的命名", _nodeId);
        _tableOnly = EditorGUILayout.ToggleLeft("只导出表 ✅（不动场景 ✅）", _tableOnly);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("开始表化 ✅", GUILayout.Height(28))) DoTableize();
            if (GUILayout.Button("清空日志 ✅", GUILayout.Height(28))) _log.Clear();
        }

        EditorGUILayout.Space();
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var line in _log) EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    private void Log(string s)
    {
        _log.Add(s);
        Debug.Log("[Y 表化] " + s);
    }

    private void DoTableize()
    {
        _log.Clear();
        Tableize(_source, _nodeId, _tableOnly, true, _log);
    }

    /// <summary>
    /// 表化的全部逻辑 ✅（**UI 只做壳 ✅** —— 这样能被脚本直接验收 ✅）。
    /// <paramref name="askConfirm"/> = false ✅ → 不弹确认框 ✅（自动化用 ✅）。
    /// </summary>
    internal static void Tableize(GameObject source, string nodeId, bool tableOnly, bool askConfirm, List<string> log)
    {
        void Log(string s) { log.Add(s); Debug.Log("[Y 表化] " + s); }

        if (source == null) { Log("❌ 先指定场景里的那一组 ✅"); return; }
        string node = string.IsNullOrEmpty(nodeId) ? source.name : nodeId.Trim();
        if (node.Contains("/")) { Log("❌ 命名里不能带 '/' ✅"); return; }

        string tablePath = $"{PlacementFolder}/{node}.json";
        string specPath = $"{SpecFolder}/{node}.asset";
        string segPath = $"{SegmentFolder}/{node}.prefab";

        // ① 导出表 ✅
        int exported = YScenePlacementImporter.ExportPlacementTable(source, tablePath, false, node, false);
        Log($"① 导出表 {exported} 条 → {tablePath} ✅");
        if (exported == 0) { Log("❌ 导出 0 条 ✅ —— 选中的组里没有可搬的物件 ✅"); return; }

        if (tableOnly) { Log("（只导出表模式 ✅ —— 到此为止 ✅）"); return; }

        // ② 规格（同名重建 ✅ —— 旧的那个口径可能已经陈旧 ✅）
        if (askConfirm)
        {
            bool go = EditorUtility.DisplayDialog("表化这一组？",
                $"要替换这三样 ✅：\n\n· 表 ✅ {tablePath}\n· 规格 ✅ {specPath}\n· 段 prefab ✅ {segPath}\n\n"
                + $"并且**删掉场景里的旧组** ✅（换成该 prefab 的实例 ✅）—— 万一不对 ✅ 可以 git checkout 场景文件 ✅ 回退 ✅。\n\n继续？",
                "开始 ✅", "取消 ✅");
            if (!go) { Log("已取消 ✅"); return; }
        }

        if (AssetDatabase.LoadAssetAtPath<YSceneSpec>(specPath) != null) AssetDatabase.DeleteAsset(specPath);
        var spec = YScenePlacementImporter.Import(tablePath);
        if (spec == null) { Log("❌ 规格导入失败 ✅"); return; }
        Log($"② 规格 → {specPath} ✅");

        // ③ 段 prefab ✅
        int count = 0;
        var pf = YSceneSpecTools.GenerateToPrefab(spec, segPath, out count);
        if (pf == null) { Log("❌ 段 prefab 生成失败 ✅"); return; }
        Log($"③ 段 prefab {count} 个对象 → {segPath} ✅");
        AssetDatabase.SaveAssets();

        // ④ 换实例 ✅（先记外部引用 ✅ —— 换完就找不到了 ✅）
        var refs = FindExternalReferences(source.transform);
        Log($"外部引用 {refs.Count} 处 ✅");

        var scene = source.scene;
        var parent = source.transform.parent;
        var lp = source.transform.localPosition;
        var lr = source.transform.localEulerAngles;
        var ls = source.transform.localScale;
        string oldName = source.name;

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(pf, scene);
        inst.transform.SetParent(parent, false);
        inst.transform.localPosition = lp;
        inst.transform.localEulerAngles = lr;
        inst.transform.localScale = ls;
        inst.name = oldName;                       // 名字不变 ✅（场景里别的东西按名字找它 ✅）

        // ⑤ 外部引用重指向 ✅（按**相对路径**映射到新实例下的同名件 ✅）
        int repointed = 0;
        foreach (var r in refs)
        {
            var prop = r.so.FindProperty(r.path);
            if (prop == null) continue;
            var mapped = MapToInstance(r.value, source.transform, inst.transform);
            if (mapped == null) continue;
            prop.objectReferenceValue = mapped;
            repointed++;
        }
        if (repointed > 0)
        {
            foreach (var r in refs) r.so.ApplyModifiedProperties();
            Log($"⑤ 外部引用重指向 {repointed} 处 ✅");
        }

        // 删旧的 ✅ —— **必须传 GameObject ✅**（传 Transform 只会毁组件 ❗ ❗）
        UnityEngine.Object.DestroyImmediate(source.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Log($"④ 场景里换成 prefab 实例 ✅「{oldName} ✅」并已存盘 ✅");

        // ⑥⑦ 验收 ✅
        bool chainOk = YSceneSpecTools.RunChainCheck(tablePath, specPath, segPath, out string chainReport);
        Log($"⑥ 三方对齐（表→规格→段 prefab ✅）：{(chainOk ? "一致 ✅" : "不一致 ❌")}");
        if (!chainOk) foreach (var l in chainReport.Split('\n')) Log("   " + l);

        bool pixelOk = YSceneSpecTools.RunPixelCheck(inst, tablePath, specPath, segPath, out string pixelReport);
        Log($"⑦ 画面逐像素 ✅：{(pixelOk ? "完全一致 ✅" : "有差异 ❌")}");
        foreach (var l in pixelReport.Split('\n')) Log("   " + l);
    }

    // ───────────────────────── 引用处理 ─────────────────────────

    private struct RefHit
    {
        public SerializedObject so;
        public string path;
        public Transform value;      // 原先指向的那个 Transform ✅
    }

    /// <summary>全场景扫一遍：谁引用了这一组里的东西 ✅（组内互相引用不算 ✅ —— 换 prefab 实例后它们是实例内部的 ✅）。</summary>
    private static List<RefHit> FindExternalReferences(Transform target)
    {
        var hits = new List<RefHit>();
        foreach (var go in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
        {
            if (go == null) continue;
            if (go.transform == target || go.transform.IsChildOf(target)) continue;   // 组内 ✅
            foreach (var comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                var it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var obj = it.objectReferenceValue as Object;
                    if (obj == null) continue;
                    var tr = obj as Transform;
                    if (tr == null) { var c = obj as Component; if (c != null) tr = c.transform; }
                    if (tr == null) continue;
                    if (tr != target && !tr.IsChildOf(target)) continue;              // 指向组根或组内某件 ✅
                    hits.Add(new RefHit { so = so, path = it.propertyPath, value = tr });
                }
            }
        }
        return hits;
    }

    /// <summary>把「旧组里的某件」按**相对路径**映射到「新实例下的同名件」✅。</summary>
    private static Transform MapToInstance(Transform oldValue, Transform oldRoot, Transform newRoot)
    {
        if (oldValue == oldRoot) return newRoot;
        // 走层级 ✅（同名同层级 ✅）
        var stack = new System.Collections.Generic.List<string>();
        var t = oldValue;
        while (t != null && t != oldRoot) { stack.Insert(0, t.name); t = t.parent; }
        var cur = newRoot;
        foreach (var n in stack)
        {
            if (cur == null) return null;
            Transform next = null;
            for (int i = 0; i < cur.childCount; i++) if (cur.GetChild(i).name == n) { next = cur.GetChild(i); break; }
            cur = next;
        }
        return cur;
    }
}
