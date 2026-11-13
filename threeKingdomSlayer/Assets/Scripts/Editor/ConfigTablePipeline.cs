using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 配置总表（Excel 配表）管线的数据模型。
///
/// 一行 = 一个「可配项」；一格 = 一个字段。资产路径 + 组件 + 属性路径 = 稳定定位三元组，
/// 导出、填表、回写三步都靠它对齐，不依赖行号（行序变了也不会串行）。
/// </summary>
[Serializable]
public sealed class ConfigTableCell
{
    /// <summary>Excel 列名。</summary>
    public string Column;
    /// <summary>字段所在组件类型（空 = 资产自身，例如 ScriptableObject）。</summary>
    public string Component;
    /// <summary>SerializedProperty 属性路径，例如 maxHealth / lines.Array.data[0].text。</summary>
    public string Property;
    /// <summary>导出时的原值（回写时用它和表里的值做差异比对）。</summary>
    public string Baseline;
    /// <summary>这一格是否允许在 Excel 里改。</summary>
    public bool Editable;
    /// <summary>字段所在对象在场景里的层级路径（空 = 资产自身；场景对象才需要）。</summary>
    public string ObjectPath = "";
}

[Serializable]
public sealed class ConfigTableRow
{
    /// <summary>稳定业务键（资产名 / upgradeId / 事件ID+行号），回写时用它匹配行。</summary>
    public string Key;
    /// <summary>显示名。</summary>
    public string Label;
    /// <summary>资产路径（prefab 或 .asset）。</summary>
    public string AssetPath;
    /// <summary>默认组件类型（行内大部分格子在这个组件上）。</summary>
    public string Component;
    public List<ConfigTableCell> Cells = new List<ConfigTableCell>();
}

[Serializable]
public sealed class ConfigTableDomain
{
    /// <summary>Excel sheet 名（≤31 字符）。</summary>
    public string Sheet;
    /// <summary>这一页是什么、怎么填。</summary>
    public string Title;
    /// <summary>wide = 宽表（行=资产，列=字段）；long = 长表（行=一条文案）。</summary>
    public string Shape = "wide";
    /// <summary>列顺序（身份列由管线固定加在最前面）。</summary>
    public List<string> Columns = new List<string>();
    public List<ConfigTableRow> Rows = new List<ConfigTableRow>();
}

[Serializable]
public sealed class ConfigTableDump
{
    public string generatedAt;
    public string projectVersion;
    public List<ConfigTableDomain> Domains = new List<ConfigTableDomain>();
}

[Serializable]
public sealed class ConfigTableEdit
{
    public string assetPath;
    public string component;
    public string property;
    public string value;
    public string rowKey;
    public string column;
    /// <summary>场景对象的层级路径（空 = 资产自身）。</summary>
    public string objectPath = "";
}

[Serializable]
public sealed class ConfigTableEdits
{
    public List<ConfigTableEdit> Edits = new List<ConfigTableEdit>();
}

/// <summary>
/// 配置总表管线：Unity 资产 → JSON dump → Excel 工作簿 → JSON edits → 写回 Unity 资产。
///
/// 为什么走 JSON 中转而不是 Editor 直接写 xlsx：
///   Unity 侧没有 xlsx 写库（openpyxl 在 python 侧），且「写回资产」必须走 SerializedProperty
///   （不能手改 YAML）。于是拆成两段：
///     ① Editor 导出 dump（含原值基线） → python 生成工作簿 → 用户填 → python 比对基线产出 edits
///     ② Editor 读 edits → SerializedProperty 写回 → 打印变更报告
///
/// 差异比对用「导出时的基线」而不是「另开一列新值」：
///   表里所见即所得，用户直接改值列；没改的格子不会产生 edits。
/// </summary>
public static class ConfigTablePipeline
{
    public const string TmpDir = "Library/Locus/tmp";
    public const string DumpPath = TmpDir + "/config_table_dump.json";
    public const string EditsPath = TmpDir + "/config_table_edits.json";

    // ── 导出 ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/三国杀戮/配置总表/① 导出 dump（配表用）", false, 200)]
    public static void ExportDump()
    {
        var dump = new ConfigTableDump
        {
            generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            projectVersion = Application.version
        };
        dump.Domains.AddRange(ConfigTableDomains.BuildAll());

        Directory.CreateDirectory(TmpDir);
        File.WriteAllText(DumpPath, JsonUtility.ToJson(dump, true), new UTF8Encoding(false));

        int rows = 0, cells = 0;
        foreach (var d in dump.Domains)
        {
            rows += d.Rows.Count;
            foreach (var r in d.Rows) cells += r.Cells.Count;
        }
        Debug.Log($"[ConfigTable] dump 已导出：{DumpPath}\n" +
                  $"域 {dump.Domains.Count} 个 / 行 {rows} / 单元格 {cells}\n" +
                  $"下一步：python Tools/build_config_workbook.py");
    }

    // ── 回写 ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/三国杀戮/配置总表/③ 应用 Excel 回填", false, 202)]
    public static void ApplyEdits()
    {
        if (!File.Exists(EditsPath))
        {
            Debug.LogWarning($"[ConfigTable] 找不到 {EditsPath}\n" +
                             $"下一步：python Tools/apply_config_workbook.py");
            return;
        }

        ConfigTableEdits edits;
        try
        {
            edits = JsonUtility.FromJson<ConfigTableEdits>(File.ReadAllText(EditsPath, Encoding.UTF8));
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfigTable] edits json 解析失败：{e.Message}");
            return;
        }
        if (edits == null || edits.Edits == null || edits.Edits.Count == 0)
        {
            Debug.Log("[ConfigTable] 没有需要写回的改动。");
            return;
        }

        var touched = new List<UnityEngine.Object>();
        var dirtyScenes = new List<string>();
        int ok = 0, fail = 0;
        var failures = new List<string>();

        foreach (var e in edits.Edits)
        {
            var target = ResolveTarget(e.assetPath, e.component, e.objectPath, out var openedScene);
            if (openedScene.IsValid() && !dirtyScenes.Contains(openedScene.path))
                dirtyScenes.Add(openedScene.path);
            if (target == null)
            {
                fail++;
                failures.Add($"{e.assetPath} / {e.component}：找不到目标");
                continue;
            }

            var so = new SerializedObject(target);
            var prop = so.FindProperty(e.property);
            if (prop == null)
            {
                fail++;
                failures.Add($"{e.assetPath} / {e.property}：属性路径找不到");
                continue;
            }

            string err;
            if (!TrySetValue(prop, e.value, out err))
            {
                fail++;
                failures.Add($"{e.assetPath} / {e.property} = \"{e.value}\"：{err}");
                continue;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            if (!touched.Contains(target)) touched.Add(target);
            string scenePath = System.IO.Path.GetExtension(e.assetPath).ToLowerInvariant() == ".unity"
                           || System.IO.Path.GetExtension(e.assetPath).ToLowerInvariant() == ".scene"
                ? e.assetPath : null;
            if (scenePath != null && !dirtyScenes.Contains(scenePath))
                dirtyScenes.Add(scenePath);
            ok++;
        }

        foreach (var t in touched)
        {
            string path = AssetDatabase.GetAssetPath(t);
            if (string.IsNullOrEmpty(path)) continue;
            if (path.EndsWith(".unity")) continue; // 场景不自动保存：保存会连带写入 GUID 迁移结果，diff 很大
            // 组件的 GetAssetPath 返回的是 prefab 路径；保存时用路径下的根资产（prefab 根 GameObject）
            var root = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (root != null) AssetDatabase.SaveAssetIfDirty(root);
        }
        AssetDatabase.Refresh();

        Debug.Log($"[ConfigTable] 回写完成：成功 {ok} 项，失败 {fail} 项，涉及资产 {touched.Count} 个。");
        foreach (var f in failures) Debug.LogWarning("[ConfigTable] " + f);
        foreach (var s in dirtyScenes)
            Debug.Log($"[ConfigTable] 场景已改脏（**未自动保存**）：{s}\n" +
                      "请自行确认后保存场景。提醒：当前工程正在做 GUID 格式迁移，保存场景会把迁移结果一起写进文件，git diff 会很大。");
        if (ok > 0) Debug.Log("[ConfigTable] 建议：手上有 Play Mode 的话，跑一局确认数值生效。");
    }

    /// <summary>按「资产路径 + 组件」找到要写回的 SerializedObject 宿主。</summary>
    /// <param name="openedScene">如果为了定位而临时打开了场景，这里返回它（调用方负责保留，不要关掉）。</param>
    private static UnityEngine.Object ResolveTarget(string assetPath, string componentType, string objectPath,
                                                      out UnityEngine.SceneManagement.Scene openedScene)
    {
        openedScene = default;

        string ext = System.IO.Path.GetExtension(assetPath).ToLowerInvariant();
        if (ext == ".unity" || ext == ".scene")
        {
            if (string.IsNullOrEmpty(objectPath)) return null;

            // 先在已加载的场景里找
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                var loaded = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!loaded.IsValid()) continue;
                var hit = FindByPathInScene(loaded, objectPath);
                if (hit != null) return hit.gameObject.GetComponent(componentType);
            }

            // 没加载 → 临时 additively 打开（**保持打开**：改了得让人能保存）
            var prevActive = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var opened = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                assetPath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
            openedScene = opened;
            // 打开后别抢活动场景（可能别人正在用）
            if (prevActive.IsValid() && prevActive.isLoaded)
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(prevActive);
            var hit2 = FindByPathInScene(opened, objectPath);
            if (hit2 == null) return null;
            return hit2.gameObject.GetComponent(componentType);
        }

        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (asset == null) return null;
        if (string.IsNullOrEmpty(componentType)) return asset;

        var go = asset as GameObject;
        if (go == null) return null;
        return go.GetComponent(componentType);
    }

    /// <summary>在场景里按 "根/子/孙" 路径找对象（支持 inactive）。</summary>
    private static Transform FindByPathInScene(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        string[] parts = path.Split('/');
        foreach (var root in scene.GetRootGameObjects())
        {
            Transform cur = root.transform;
            if (cur.name != parts[0]) continue;
            bool ok = true;
            for (int i = 1; i < parts.Length && ok; i++)
            {
                Transform next = null;
                foreach (Transform child in cur)
                    if (child.name == parts[i]) { next = child; break; }
                if (next == null) ok = false; else cur = next;
            }
            if (ok) return cur;
        }
        return null;
    }

    /// <summary>把表里的字符串写进 SerializedProperty（按属性类型解析）。</summary>
    private static bool TrySetValue(SerializedProperty prop, string raw, out string error)
    {
        error = null;
        string v = (raw ?? "").Trim();

        switch (prop.propertyType)
        {
            case SerializedPropertyType.Float:
                float f;
                if (!float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                {
                    error = "不是数字";
                    return false;
                }
                prop.floatValue = f;
                return true;

            case SerializedPropertyType.Integer:
            case SerializedPropertyType.Enum:
                int i;
                if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
                {
                    if (prop.propertyType == SerializedPropertyType.Enum)
                    {
                        if (i < 0 || i >= prop.enumDisplayNames.Length)
                        {
                            error = "枚举下标越界";
                            return false;
                        }
                        prop.enumValueIndex = i;
                    }
                    else
                    {
                        prop.intValue = i;
                    }
                    return true;
                }
                if (prop.propertyType == SerializedPropertyType.Enum)
                {
                    string[] names = prop.enumDisplayNames;
                    for (int k = 0; k < names.Length; k++)
                    {
                        if (string.Equals(names[k], v, StringComparison.OrdinalIgnoreCase))
                        {
                            prop.enumValueIndex = k;
                            return true;
                        }
                    }
                    error = "枚举名不在可选值里（可选：" + string.Join("/", names) + "）";
                    return false;
                }
                error = "不是整数";
                return false;

            case SerializedPropertyType.Boolean:
                bool b;
                if (bool.TryParse(v, out b)) { prop.boolValue = b; return true; }
                if (v == "1") { prop.boolValue = true; return true; }
                if (v == "0") { prop.boolValue = false; return true; }
                if (v == "是" || v == "有") { prop.boolValue = true; return true; }
                if (v == "否" || v == "无") { prop.boolValue = false; return true; }
                error = "不是布尔值（填 true/false、1/0、是/否）";
                return false;

            case SerializedPropertyType.String:
                prop.stringValue = raw ?? "";
                return true;

            default:
                error = "暂不支持的属性类型：" + prop.propertyType;
                return false;
        }
    }

    // ── 读值工具（给各域定义用） ────────────────────────────────────────────

    /// <summary>读一个字段并生成单元格（值取自组件实例上的真实序列化值）。</summary>
    public static ConfigTableCell Cell(UnityEngine.Object host, string componentType, string property,
                                       string column, bool editable)
    {
        var cell = new ConfigTableCell
        {
            Column = column,
            Component = componentType,
            Property = property,
            Editable = editable
        };
        cell.Baseline = ReadValue(host, property);
        return cell;
    }

    /// <summary>读取字段当前值（统一按字符串输出，便于和 Excel 单元格对齐）。</summary>
    public static string ReadValue(UnityEngine.Object host, string property)
    {
        if (host == null) return "";
        var so = new SerializedObject(host);
        var prop = so.FindProperty(property);
        if (prop == null) return "";
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Float: return prop.floatValue.ToString("R", CultureInfo.InvariantCulture);
            case SerializedPropertyType.Integer: return prop.intValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Enum:
                int idx = prop.enumValueIndex;
                return (idx >= 0 && idx < prop.enumDisplayNames.Length) ? prop.enumDisplayNames[idx] : idx.ToString();
            case SerializedPropertyType.Boolean: return prop.boolValue ? "true" : "false";
            case SerializedPropertyType.String: return prop.stringValue ?? "";
            default: return "";
        }
    }
}
