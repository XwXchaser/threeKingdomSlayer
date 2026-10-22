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
}
