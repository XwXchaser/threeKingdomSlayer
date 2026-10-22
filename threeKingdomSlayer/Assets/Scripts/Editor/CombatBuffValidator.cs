using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **战斗状态定义校验** ✅ —— 与天赋树 ✅ / 关卡清单 ✅ 同一套思路 ✅ ✅。
///
/// 查什么 ✅：buffId 为空 ✅ / 重复 ✅ / 不在 `BuffIds` 词表里 ✅（拼错 id ✅ 最容易踩 ✅ ✅）/ 概率越界 ✅。
/// </summary>
public static class CombatBuffValidator
{
    [MenuItem("Tools/数据校验/战斗状态定义 ✅")]
    private static void ValidateAll()
    {
        var guids = AssetDatabase.FindAssets("t:CombatBuffDefinition");
        int total = 0, bad = 0;
        var sb = new StringBuilder();
        var known = new System.Collections.Generic.HashSet<string> { BuffIds.ForceLaunch, BuffIds.ProbabilityLaunch };
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var def = AssetDatabase.LoadAssetAtPath<CombatBuffDefinition>(path);
            if (def == null) continue;
            total++;
            var issues = new System.Collections.Generic.List<string>();
            if (!def.IsValid) issues.Add("buffId 为空 ✅");
            else
            {
                if (!seen.Add(def.buffId)) issues.Add($"buffId 重复：{def.buffId} ✅");
                if (!known.Contains(def.buffId)) issues.Add($"buffId「{def.buffId}」不在 BuffIds 词表里 ✅（代码里没人认它 ✅ —— 是不是拼错了 ✅ ✅）");
            }
            if (def.launchProbability < 0f || def.launchProbability > 1f) issues.Add("击飞概率越界 ✅");
            if (issues.Count == 0) { sb.AppendLine($"✅ {path}（{def.buffId} ✅）—— 无问题 ✅"); continue; }
            bad++;
            sb.AppendLine($"❌ {path} —— {issues.Count} 个问题 ✅：");
            foreach (var i in issues) sb.AppendLine("    · " + i);
        }
        Debug.Log($"[战斗状态校验] 共 {total} 条 ✅｜有问题 {bad} 条 ✅\n{sb}");
        if (bad == 0) EditorUtility.DisplayDialog("战斗状态校验", $"{total} 条状态定义 ✅ 全部通过 ✅", "好 ✅");
        else EditorUtility.DisplayDialog("战斗状态校验", $"{bad} / {total} 条有问题 ✅ —— 看 Console ✅", "好 ✅");
    }
}
