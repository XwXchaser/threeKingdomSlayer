using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **天赋树数据校验** ✅ —— 把「加一条天赋 ✅」的风险挡在编辑器里 ✅ ✅。
///
/// 查什么 ✅：前置 id 不存在 ✅ / 自指 ✅ / 环 ✅ / nodeId 重复 ✅ / 空节点 ✅。
/// 为什么重要 ✅：**前置关系从「列表上一项 ✅」改成了显式 id ✅** ✅
/// → 写得对不对不再靠肉眼 ✅ ✅ → 改完一键验 ✅ ✅✅。
/// </summary>
public static class TalentTreeValidator
{
    [MenuItem("Tools/数据校验/天赋树 ✅")]
    private static void ValidateAll()
    {
        var guids = AssetDatabase.FindAssets("t:TalentTreeConfig");
        int total = 0, bad = 0;
        var sb = new StringBuilder();
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var tree = AssetDatabase.LoadAssetAtPath<TalentTreeConfig>(path);
            if (tree == null) continue;
            total++;
            var issues = tree.Validate();
            if (issues.Count == 0)
            {
                sb.AppendLine($"✅ {path}（{tree.nodes?.Count ?? 0} 个节点 ✅）—— 无问题 ✅");
                continue;
            }
            bad++;
            sb.AppendLine($"❌ {path}（{tree.nodes?.Count ?? 0} 个节点 ✅）—— {issues.Count} 个问题 ✅：");
            foreach (var i in issues) sb.AppendLine("    · " + i);
        }
        Debug.Log($"[天赋树校验] 共 {total} 棵 ✅｜有问题 {bad} 棵 ✅\n{sb}");
        if (bad == 0) EditorUtility.DisplayDialog("天赋树校验", $"{total} 棵天赋树 ✅ 全部通过 ✅", "好 ✅");
        else EditorUtility.DisplayDialog("天赋树校验", $"{bad} / {total} 棵有问题 ✅ —— 看 Console ✅", "好 ✅");
    }

    private const int RowsPerPage = 8;

    /// <summary>
    /// 三分类目录校验：跨资产 id 唯一 / 位置派生 id / 自动切页可行性 / 页数预览。
    /// 这几条是「三页 × 自动切页」新增的约束，单棵树的 Validate 看不到。
    /// </summary>
    [MenuItem("Tools/数据校验/天赋目录（三分类）")]
    private static void ValidateCatalog()
    {
        var guids = AssetDatabase.FindAssets("t:TalentCatalog");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("天赋目录校验", "项目里没有 TalentCatalog 资产", "好");
            return;
        }

        var sb = new StringBuilder();
        int bad = 0;
        foreach (var g in guids)
        {
            string cpath = AssetDatabase.GUIDToAssetPath(g);
            var cat = AssetDatabase.LoadAssetAtPath<TalentCatalog>(cpath);
            if (cat == null) continue;

            sb.AppendLine($"== {cpath}");
            var ids = new Dictionary<string, string>();   // id -> 来源
            int issuesHere = 0;

            for (int c = 0; c < TalentCatalog.CategoryCount; c++)
            {
                var tree = cat.Get(c);
                if (tree == null)
                {
                    sb.AppendLine($"   [{c}] （留空）");
                    continue;
                }

                // 自动切页可行性
                var problems = new List<string>();
                var pages = TalentPaginator.Split(tree, RowsPerPage, problems);
                sb.AppendLine($"   [{c}] {tree.name}：{tree.nodes.Count} 节点 → {pages.Count} 页（rowsPerPage={RowsPerPage}）");
                foreach (var p in pages)
                    sb.AppendLine($"        · 第 {p.index + 1} 页：行 {p.rowStart + 1}-{p.rowEnd + 1}，{p.nodes.Count} 节点");
                foreach (var pr in problems)
                {
                    sb.AppendLine("        ! " + pr);
                    issuesHere++;
                }

                foreach (var n in tree.nodes)
                {
                    if (n == null || string.IsNullOrEmpty(n.nodeId)) continue;
                    // 位置派生 id（OnValidate 曾经自动生成 "node_0" 这种，挪顺序即换身份）
                    if (System.Text.RegularExpressions.Regex.IsMatch(n.nodeId, @"^node_\d+$"))
                    {
                        sb.AppendLine($"        ! {n.nodeId} 是位置派生 id（node_数字），必须改成语义 id");
                        issuesHere++;
                    }
                    if (ids.TryGetValue(n.nodeId, out var owner))
                    {
                        sb.AppendLine($"        ! nodeId 重复：{n.nodeId}（{owner} 与分类 {c}）");
                        issuesHere++;
                    }
                    else ids[n.nodeId] = $"分类 {c}";
                }
            }

            sb.AppendLine(issuesHere == 0 ? "   ✅ 无问题" : $"   ❌ {issuesHere} 个问题");
            bad += issuesHere;
        }

        Debug.Log($"[天赋目录校验]\n{sb}");
        EditorUtility.DisplayDialog("天赋目录校验", bad == 0 ? "全部通过（详情见 Console）" : $"{bad} 个问题（详情见 Console）", "好");
    }
}
