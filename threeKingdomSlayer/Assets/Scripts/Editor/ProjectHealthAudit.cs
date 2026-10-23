using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 项目体检（只读）。
///
/// 把 plan/project-audit-20261024.md 里手工跑过的扫描固化成一个菜单：
///   Tools/项目体检/运行全部检查
///
/// 检查项：
///   1. 脚本规模（.cs 数量、按顶层目录统计）
///   2. 旧架构残留（Legacy 目录 / 程序集 / 类型名）
///   3. 单例（static Instance）与 Find* 调用的分布和热点
///   4. 孤儿审计：配置类 ScriptableObject 资产，按 GUID 与按名字两种方式查引用
///   5. TODO / FIXME / HACK / 临时方案 标记
///
/// 不修改任何资产；报告写入 Library/Locus/tmp/project-health-audit.txt（Library 下，不进版本库）。
/// </summary>
public static class ProjectHealthAudit
{
    private const string ReportPath = "Library/Locus/tmp/project-health-audit.txt";
    private const string ScriptsRoot = "Assets/Scripts";

    private static readonly Regex SingletonRegex =
        new Regex(@"static\s+[\w<>]+\s+Instance\b", RegexOptions.Compiled);

    private static readonly Regex FindRegex =
        new Regex(@"FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|GameObject\.Find\(|Resources\.FindObjectsOfTypeAll",
            RegexOptions.Compiled);

    private static readonly Regex MarkerRegex =
        new Regex(@"\b(TODO|FIXME|HACK)\b|临时方案", RegexOptions.Compiled);

    [MenuItem("Tools/项目体检/运行全部检查")]
    public static void RunAll()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# 项目体检 {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        try
        {
            var csFiles = Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories)
                .Select(p => p.Replace('\\', '/'))
                .Where(p => !p.EndsWith("/ProjectHealthAudit.cs"))
                .ToArray();

            ScriptScale(csFiles, sb);
            LegacyLeftovers(csFiles, sb);
            SingletonAndFind(csFiles, sb);
            MarkersScan(csFiles, sb);
            OrphanAudit(sb);
        }
        catch (System.Exception e)
        {
            sb.AppendLine("!! 体检异常中断: " + e);
        }

        WriteReport(sb.ToString());
        Debug.Log($"[项目体检] 完成，报告已写入 {ReportPath}\n{sb}");
    }

    // ---------- 1. 规模 ----------
    private static void ScriptScale(string[] csFiles, StringBuilder sb)
    {
        long bytes = csFiles.Sum(f => new FileInfo(f).Length);
        sb.AppendLine($"## 1. 脚本规模");
        sb.AppendLine($"- .cs 文件数：{csFiles.Length}（{bytes / 1024} KB）");

        var byDir = csFiles
            .GroupBy(f => f.Substring(ScriptsRoot.Length + 1).Split('/')[0])
            .OrderByDescending(g => g.Count());
        foreach (var g in byDir) sb.AppendLine($"  - {g.Key}: {g.Count()}");

        var largest = csFiles.OrderByDescending(f => new FileInfo(f).Length).Take(5);
        sb.AppendLine("- 最大的 5 个文件：");
        foreach (var f in largest) sb.AppendLine($"  - {f} ({new FileInfo(f).Length / 1024} KB)");
        sb.AppendLine();
    }

    // ---------- 2. 旧架构残留 ----------
    private static void LegacyLeftovers(string[] csFiles, StringBuilder sb)
    {
        sb.AppendLine("## 2. 旧架构残留");
        var legacyDirs = Directory.GetDirectories(ScriptsRoot, "Legacy", SearchOption.AllDirectories);
        var legacyAsmdefs = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories)
            .Where(p => Path.GetFileName(p).IndexOf("Legacy", System.StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        sb.AppendLine($"- Legacy 目录：{(legacyDirs.Length == 0 ? "无" : string.Join(", ", legacyDirs))}");
        sb.AppendLine($"- Legacy 程序集：{(legacyAsmdefs.Length == 0 ? "无" : string.Join(", ", legacyAsmdefs))}");

        // 引用 Locus.Legacy 的 asmdef
        var refs = Directory.GetFiles("Assets", "*.asmdef", SearchOption.AllDirectories)
            .Where(p => File.ReadAllText(p).Contains("\"Locus.Legacy\""))
            .ToArray();
        sb.AppendLine($"- 仍引用 Locus.Legacy 的 asmdef：{(refs.Length == 0 ? "无" : string.Join(", ", refs))}");

        // 空目录（git 不跟踪，但 Unity 会留 .meta）
        var emptyDirs = Directory.GetDirectories(ScriptsRoot, "*", SearchOption.AllDirectories)
            .Where(d => Directory.GetFileSystemEntries(d).All(e => e.EndsWith(".meta")) && Directory.GetFiles(d).Length == 0)
            .Select(d => d.Replace('\\', '/'))
            .ToArray();
        sb.AppendLine($"- 空目录（仅剩 .meta）：{(emptyDirs.Length == 0 ? "无" : string.Join(", ", emptyDirs))}");
        sb.AppendLine();
    }

    // ---------- 3. 单例与 Find* ----------
    private static void SingletonAndFind(string[] csFiles, StringBuilder sb)
    {
        var singletonFiles = new List<string>();
        var findHits = new Dictionary<string, int>();
        int findTotal = 0;

        foreach (var f in csFiles)
        {
            string text = File.ReadAllText(f);
            if (SingletonRegex.IsMatch(text)) singletonFiles.Add(f);

            int n = FindRegex.Matches(text).Count;
            if (n > 0)
            {
                findHits[f] = n;
                findTotal += n;
            }
        }

        sb.AppendLine("## 3. 单例与 Find*");
        sb.AppendLine($"- 含 static Instance 的文件：{singletonFiles.Count}");
        sb.AppendLine($"- Find* 调用总数：{findTotal}（分布在 {findHits.Count} 个文件）");
        sb.AppendLine("- Find* 热点（前 10）：");
        foreach (var kv in findHits.OrderByDescending(kv => kv.Value).Take(10))
            sb.AppendLine($"  - {kv.Key}: {kv.Value}");
        sb.AppendLine();
    }

    // ---------- 4. TODO / 临时方案 ----------
    private static void MarkersScan(string[] csFiles, StringBuilder sb)
    {
        sb.AppendLine("## 4. TODO / FIXME / HACK / 临时方案");
        int count = 0;
        foreach (var f in csFiles)
        {
            var lines = File.ReadAllLines(f);
            for (int i = 0; i < lines.Length; i++)
            {
                if (!MarkerRegex.IsMatch(lines[i])) continue;
                count++;
                sb.AppendLine($"  - {f}:{i + 1}  {lines[i].Trim()}");
            }
        }
        sb.AppendLine($"- 共 {count} 处");
        sb.AppendLine();
    }

    // ---------- 5. 孤儿审计 ----------
    // 配置类 ScriptableObject 资产：只要按 GUID 或按文件名在任何 .unity/.prefab/.asset 里出现过就算被引用。
    private static void OrphanAudit(StringBuilder sb)
    {
        sb.AppendLine("## 5. 配置类资产孤儿审计（GUID 文本扫描，结果为候选，需人工确认）");

        var configAssets = AssetDatabase.FindAssets("t:ScriptableObject")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith("Assets/") && !p.StartsWith("Assets/Scripts/") && !p.Contains("/ThirdParty/"))
            .ToArray();

        // 候选引用源：场景、预制体、其它资产的文本内容
        var sourceFiles = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".unity") || p.EndsWith(".prefab") || p.EndsWith(".asset"))
            .Select(p => p.Replace('\\', '/'))
            .ToArray();

        var sourceTexts = new Dictionary<string, string>();
        foreach (var s in sourceFiles) sourceTexts[s] = File.ReadAllText(s);

        int orphan = 0;
        var orphanList = new List<string>();
        foreach (var asset in configAssets)
        {
            string guid = AssetDatabase.AssetPathToGUID(asset);
            string name = Path.GetFileNameWithoutExtension(asset);
            bool referenced = sourceTexts.Values.Any(t => t.Contains(guid));
            // 名字只用于兜底：同名字段/脚本引用不算证据，因此名字命中仅作为「可能被引用」提示
            bool nameHit = sourceTexts.Values.Any(t => t.Contains(name));

            if (!referenced)
            {
                orphan++;
                orphanList.Add(asset + (nameHit ? "  [名字命中，需人工确认]" : "  [GUID 与名字均未命中]"));
            }
        }

        sb.AppendLine($"- 扫描配置类资产：{configAssets.Length}，GUID 文本无命中：{orphan}");
        sb.AppendLine("- 说明：GUID 文本无命中 ≠ 确定孤儿。字段可能由 Inspector 在其它资产中以值形式持有，或通过代码动态加载；需结合 unity_code_usages 与人工判断。");
        foreach (var o in orphanList.Take(50)) sb.AppendLine("  - " + o);
        if (orphanList.Count > 50) sb.AppendLine($"  ... 另有 {orphanList.Count - 50} 个");
        sb.AppendLine();
    }

    private static void WriteReport(string content)
    {
        string dir = Path.GetDirectoryName(ReportPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(ReportPath, content, new UTF8Encoding(false));
    }
}
