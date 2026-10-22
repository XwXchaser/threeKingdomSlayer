using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个天赋节点。用普通可序列化类而不是独立资产：
/// 节点只在天赋树资产里被引用一次，做成内联数据可以避免几十个零散 .asset。
/// </summary>
[Serializable]
public sealed class TalentNodeConfig
{
    [Tooltip("唯一 id，写进存档。改名会让老档丢失该节点，别乱改")]
    public string nodeId = "";
    [Tooltip("节点标题（按钮上显示）")]
    public string displayName = "";
    [Tooltip("节点说明（点开后在底部说明栏显示）")]
    [TextArea(2, 4)] public string description = "";

    [Tooltip("解锁花费的铜钱。0 = 免费（仅用于框架验证）")]
    [Min(0)] public int coinCost = 100;

    [Header("解锁载荷 ✅（可同时给多个 ✅ —— 不再有「解锁类型」枚举 ✅）")]
    [Tooltip("**解锁招式** ✅ —— 解锁后该招式才可出招 ✅（留空 = 不解锁招式 ✅）")]
    public AttackSkillConfig moveConfig;
    [Tooltip("**点亮机械开关** ✅ —— 开关 id ✅（后续系统用 `TalentTreeState.IsFlagUnlocked` ✅ 读 ✅）（留空 = 不点开关 ✅）")]
    public string flagId = "";

    [Header("前置关系")]
    [Tooltip("**前置节点的 nodeId 列表** ✅（可多个 ✅ → 支持分叉 ✅）。\n"
           + "留空 = 沿用旧行为 ✅（前置 = 列表里的上一个节点 ✅）→ 老资产不用改 ✅。\n"
           + "存档存的是 nodeId ✅ → 改这里**不会毁老存档** ✅ ✅。")]
    public List<string> prerequisiteIds = new List<string>();

    [Header("排版（美术稿下来后按设计稿填）")]
    [Tooltip("节点中心在面板里的位置，单位=设计像素（1080×1920 口径），原点为面板中心，+Y 向上")]
    public Vector2 treePosition = Vector2.zero;
    [Tooltip("节点图标；为空时用程序化圆形占位")]
    public Sprite icon;

    /// <summary>
    /// 这个节点**带没带解锁载荷** ✅ —— 两个载荷都空 ✅ = 无效 ✅（自检会报错 ✅ ✅）。
    /// </summary>
    public bool HasValidUnlockPayload =>
        moveConfig != null || !string.IsNullOrEmpty(flagId);

    /// <summary>解锁了招式 ✅（不再靠枚举判断 ✅ ✅）。</summary>
    public bool UnlocksMove => moveConfig != null;

    /// <summary>点亮了机械开关 ✅。</summary>
    public bool UnlocksFlag => !string.IsNullOrEmpty(flagId);
}

/// <summary>
/// 一条天赋树。
/// **前置关系 = 显式的 nodeId 列表** ✅（`TalentNodeConfig.prerequisiteIds` ✅）→ **支持分叉 ✅** ✅。
/// 兼容旧写法 ✅：某节点的 `prerequisiteIds` 为空 ✅ → 前置 = 列表里的**上一个**节点 ✅（原来的单向链 ✅ ✅）✅
/// → **老资产不用改也能用** ✅ ✅ ✅。
/// </summary>
[CreateAssetMenu(fileName = "TalentTree", menuName = "一夫当关/天赋树")]
public class TalentTreeConfig : ScriptableObject
{
    [Tooltip("树 id，写进存档做区分")]
    public string treeId = "main";
    [Tooltip("节点列表。**前置关系看每个节点的「前置关系」字段 ✅**（prerequisiteIds ✅）——\n"
           + "列表顺序只影响**旧写法 ✅**（前置 = 上一项 ✅）与面板里的排布顺序 ✅。")]
    public List<TalentNodeConfig> nodes = new List<TalentNodeConfig>();

    public TalentNodeConfig GetNode(int index) =>
        nodes != null && index >= 0 && index < nodes.Count ? nodes[index] : null;

    public int IndexOf(TalentNodeConfig node) => node == null || nodes == null ? -1 : nodes.IndexOf(node);

    /// <summary>前置节点：根节点返回 null。</summary>
    public TalentNodeConfig GetPrerequisite(TalentNodeConfig node)
    {
        var list = GetPrerequisites(node);
        return list.Count > 0 ? list[0] : null;
    }

    /// <summary>
    /// 节点的**全部前置** ✅ —— 优先用显式的 `prerequisiteIds` ✅；
    /// 没填 ✅ 则退回旧行为 ✅（前置 = 列表里的上一项 ✅）✅ → **老资产零改动可用** ✅ ✅。
    /// </summary>
    public List<TalentNodeConfig> GetPrerequisites(TalentNodeConfig node)
    {
        var result = new List<TalentNodeConfig>();
        if (node == null) return result;
        if (node.prerequisiteIds != null && node.prerequisiteIds.Count > 0)
        {
            foreach (var id in node.prerequisiteIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var p = FindById(id);
                if (p != null) result.Add(p);
            }
            return result;
        }
        int i = IndexOf(node);
        if (i > 0 && nodes != null && nodes[i - 1] != null) result.Add(nodes[i - 1]);
        return result;
    }

    /// <summary>
    /// **自检** ✅：前置 id 不存在 ✅ / 自指 ✅ / 环 ✅ / 重复 ✅。
    /// → 改完数据一键验 ✅ ✅（工具菜单「校验天赋树 ✅」✅）。
    /// </summary>
    public List<string> Validate()
    {
        var issues = new List<string>();
        if (nodes == null) return issues;
        var seen = new HashSet<string>();
        foreach (var n in nodes)
        {
            if (n == null) { issues.Add("有空节点 ✅ —— 删掉它 ✅"); continue; }
            if (string.IsNullOrEmpty(n.nodeId)) { issues.Add("有节点没填 nodeId ✅"); continue; }
            if (!seen.Add(n.nodeId)) issues.Add($"nodeId 重复：{n.nodeId} ✅");
            if (n.prerequisiteIds == null) continue;
            foreach (var id in n.prerequisiteIds)
            {
                if (string.IsNullOrEmpty(id)) { issues.Add($"{n.nodeId} 有空的前置 id ✅"); continue; }
                if (id == n.nodeId) { issues.Add($"{n.nodeId} 把自己当前置 ✅（环 ✅）"); continue; }
                if (FindById(id) == null) issues.Add($"{n.nodeId} 的前置 {id} 不存在 ✅");
            }
        }
        // 环检测 ✅（沿前置回溯 ✅）
        foreach (var n in nodes)
        {
            if (n == null || string.IsNullOrEmpty(n.nodeId)) continue;
            var path = new HashSet<string>();
            var cur = n;
            int guard = 0;
            while (cur != null && guard++ < 256)
            {
                if (!path.Add(cur.nodeId)) { issues.Add($"{n.nodeId} 的前置形成环 ✅"); break; }
                var ps = GetPrerequisites(cur);
                if (ps.Count == 0) break;
                cur = ps[0];
            }
        }
        return issues;
    }

    public TalentNodeConfig FindById(string nodeId)
    {
        if (nodes == null || string.IsNullOrEmpty(nodeId)) return null;
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i] != null && nodes[i].nodeId == nodeId) return nodes[i];
        return null;
    }

    private void OnValidate()
    {
        if (nodes == null) return;
        var seen = new HashSet<string>();
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n == null) continue;
            if (string.IsNullOrEmpty(n.nodeId))
                n.nodeId = "node_" + i;
            else if (!seen.Add(n.nodeId))
                n.nodeId = n.nodeId + "_" + i;
        }
    }
}
