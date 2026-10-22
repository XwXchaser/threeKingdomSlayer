using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **关卡清单（选关数据）** ✅ —— 玩家在选关页能看到的全部关卡 ✅ + 它们之间的**解锁关系** ✅。
///
/// 为什么要有它 ✅：清单原本住在**场景里的一个 MonoBehaviour** ✅ → 加一条关卡要**改场景** ✅ ❗
/// → 增量内容被绑死在场景上 ✅ ❗。现在清单是**资产** ✅ → **加关卡 = 加一条资产 ✅ ✅**。
///
/// 解锁规则（全部写在数据里 ✅ ✅）：
///   ① `alwaysUnlockedStageIds` ✅ —— 总是可选的关卡 ✅（教学关 ✅ / 第一个正式关 ✅）
///   ② `FakeRouteStageConfig.prerequisiteStageIds` ✅ —— 其余关卡要求**所有前置都通关** ✅（可多个 ✅ → 支持分叉 ✅）
/// → 「按位置数前面通了几个 ✅」那种写法已退役 ✅ ❗（插一条关卡会连锁改 ✅ ❗）。
/// </summary>
[CreateAssetMenu(fileName = "RouteCatalog", menuName = "一夫当关/关卡清单")]
public sealed class RouteCatalogData : ScriptableObject
{
    [Tooltip("选关页要列的关卡 ✅（顺序 = 显示顺序 ✅）")]
    public List<FakeRouteStageConfig> stages = new List<FakeRouteStageConfig>();

    [Tooltip("**总是可选**的关卡 stageId ✅（教学关 ✅ / 第一个正式关 ✅ 放这里 ✅）")]
    public List<int> alwaysUnlockedStageIds = new List<int>();

    /// <summary>取清单里的关卡 ✅（跳过空引用 ✅）。</summary>
    public List<FakeRouteStageConfig> GetStages()
    {
        var result = new List<FakeRouteStageConfig>();
        if (stages == null) return result;
        for (int i = 0; i < stages.Count; i++)
            if (stages[i] != null) result.Add(stages[i]);
        return result;
    }

    public bool IsAlwaysUnlocked(int stageId) =>
        alwaysUnlockedStageIds != null && alwaysUnlockedStageIds.Contains(stageId);

    /// <summary>
    /// 这个关卡**是否已解锁** ✅ —— 全部规则在数据里 ✅ ✅：
    /// 总是可选 ✅ → 否则要求**所有前置都通关** ✅ ✅。
    /// </summary>
    public bool IsUnlocked(FakeRouteStageConfig stage, System.Func<int, bool> isCleared)
    {
        if (stage == null) return false;
        if (IsAlwaysUnlocked(stage.stageId)) return true;
        if (stage.prerequisiteStageIds == null || stage.prerequisiteStageIds.Count == 0) return false;
        for (int i = 0; i < stage.prerequisiteStageIds.Count; i++)
            if (isCleared == null || !isCleared(stage.prerequisiteStageIds[i])) return false;
        return true;
    }

    /// <summary>
    /// **自检** ✅：关卡 id 重复 ✅ / 前置 id 不存在 ✅ / 前置成环 ✅ / 默认可选 id 不存在 ✅。
    /// → 改完清单一键验 ✅ ✅（工具菜单「校验关卡清单 ✅」✅）。
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();
        if (stages == null) return issues;
        var seen = new HashSet<int>();
        foreach (var s in stages)
        {
            if (s == null) { issues.Add("清单里有空关卡 ✅"); continue; }
            if (!seen.Add(s.stageId)) issues.Add($"stageId 重复：{s.stageId} ✅");
            if (s.prerequisiteStageIds == null) continue;
            foreach (var pid in s.prerequisiteStageIds)
            {
                if (pid == s.stageId) { issues.Add($"{s.stageName}({s.stageId}) 把自己当前置 ✅（环 ✅）"); continue; }
                bool found = false;
                for (int i = 0; i < stages.Count; i++)
                    if (stages[i] != null && stages[i].stageId == pid) { found = true; break; }
                if (!found) issues.Add($"{s.stageName}({s.stageId}) 的前置 {pid} 不在清单里 ✅");
            }
        }
        if (alwaysUnlockedStageIds != null)
            foreach (var id in alwaysUnlockedStageIds)
            {
                bool found = false;
                for (int i = 0; i < stages.Count; i++)
                    if (stages[i] != null && stages[i].stageId == id) { found = true; break; }
                if (!found) issues.Add($"「默认可选」里的 {id} 不在清单里 ✅");
            }
        // 环检测 ✅
        foreach (var s in stages)
        {
            if (s == null) continue;
            var path = new HashSet<int>();
            var cur = s;
            int guard = 0;
            while (cur != null && guard++ < 256)
            {
                if (!path.Add(cur.stageId)) { issues.Add($"{s.stageName}({s.stageId}) 的前置成环 ✅"); break; }
                if (cur.prerequisiteStageIds == null || cur.prerequisiteStageIds.Count == 0) break;
                int nextId = cur.prerequisiteStageIds[0];
                FakeRouteStageConfig next = null;
                for (int i = 0; i < stages.Count; i++)
                    if (stages[i] != null && stages[i].stageId == nextId) { next = stages[i]; break; }
                cur = next;
            }
        }
        return issues;
    }
}
