using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **关卡清单数据校验** ✅ —— 与天赋树校验器同一套思路 ✅ ✅。
///
/// 查什么 ✅：stageId 重复 ✅ / 前置 id 不在清单里 ✅ / 前置成环 ✅ / 默认可选 id 不存在 ✅。
/// 为什么重要 ✅：**解锁关系从「按位置数前面通了几个 ✅」搬到了数据里** ✅ ✅
/// → 改完清单一键验 ✅ ✅✅ ✅。
/// </summary>
public static class RouteCatalogValidator
{
    [MenuItem("Tools/数据校验/关卡清单 ✅")]
    private static void ValidateAll()
    {
        var guids = AssetDatabase.FindAssets("t:RouteCatalogData");
        int total = 0, bad = 0;
        var sb = new StringBuilder();
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var data = AssetDatabase.LoadAssetAtPath<RouteCatalogData>(path);
            if (data == null) continue;
            total++;
            var issues = data.Validate();
            if (issues.Count == 0)
            {
                sb.AppendLine($"✅ {path}（{data.stages?.Count ?? 0} 个关卡 ✅）—— 无问题 ✅");
                continue;
            }
            bad++;
            sb.AppendLine($"❌ {path}（{data.stages?.Count ?? 0} 个关卡 ✅）—— {issues.Count} 个问题 ✅：");
            foreach (var i in issues) sb.AppendLine("    · " + i);
        }
        Debug.Log($"[关卡清单校验] 共 {total} 份 ✅｜有问题 {bad} 份 ✅\n{sb}");
        if (bad == 0) EditorUtility.DisplayDialog("关卡清单校验", $"{total} 份清单 ✅ 全部通过 ✅", "好 ✅");
        else EditorUtility.DisplayDialog("关卡清单校验", $"{bad} / {total} 份有问题 ✅ —— 看 Console ✅", "好 ✅");
    }
}
