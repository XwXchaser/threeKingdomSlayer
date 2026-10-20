using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 天赋节点解锁后产生什么。刻意只做“机械开关”，不做纯数值伤害提升（见 design/out-of-game-growth-system.md 设计约束）。
/// </summary>
public enum TalentUnlockKind
{
    [Tooltip("解锁一个招式/连段收尾（C2 = Zhangfei_StabR2，C4 = Zhangfei_LaunchFinisher）")]
    MoveUnlock = 0,
    [Tooltip("点亮一个机械开关（破阵/追击/运筹…），由后续系统按 flagId 读取")]
    Flag = 1
}

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

    [Header("解锁内容")]
    public TalentUnlockKind unlockKind = TalentUnlockKind.Flag;
    [Tooltip("unlockKind = MoveUnlock 时必填：解锁后该招式才可出招")]
    public AttackSkillConfig moveConfig;
    [Tooltip("unlockKind = Flag 时必填：机械开关 id，后续系统用 TalentTreeState.IsFlagUnlocked 判定")]
    public string flagId = "";

    [Header("排版（美术稿下来后按设计稿填）")]
    [Tooltip("节点中心在面板里的位置，单位=设计像素（1080×1920 口径），原点为面板中心，+Y 向上")]
    public Vector2 treePosition = Vector2.zero;
    [Tooltip("节点图标；为空时用程序化圆形占位")]
    public Sprite icon;

    public bool HasValidUnlockPayload =>
        unlockKind == TalentUnlockKind.MoveUnlock
            ? moveConfig != null
            : !string.IsNullOrEmpty(flagId);
}

/// <summary>
/// 一条天赋树。当前按 nodes 列表顺序做单向链：nodes[0] 是根（无前置），
/// nodes[i] 的前置是 nodes[i-1]。用户口径是“暂时一路点到尾”，所以不做分叉；
/// 将来要分叉时再给 TalentNodeConfig 加 prerequisiteNodeIds。
/// </summary>
[CreateAssetMenu(fileName = "TalentTree", menuName = "一夫当关/天赋树")]
public class TalentTreeConfig : ScriptableObject
{
    [Tooltip("树 id，写进存档做区分")]
    public string treeId = "main";
    [Tooltip("按解锁顺序排列：第一个是根节点，后面每个的前置是前一个")]
    public List<TalentNodeConfig> nodes = new List<TalentNodeConfig>();

    public TalentNodeConfig GetNode(int index) =>
        nodes != null && index >= 0 && index < nodes.Count ? nodes[index] : null;

    public int IndexOf(TalentNodeConfig node) => node == null || nodes == null ? -1 : nodes.IndexOf(node);

    /// <summary>前置节点：根节点返回 null。</summary>
    public TalentNodeConfig GetPrerequisite(TalentNodeConfig node)
    {
        int i = IndexOf(node);
        return i <= 0 ? null : nodes[i - 1];
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
            if (!n.HasValidUnlockPayload && n.unlockKind == TalentUnlockKind.MoveUnlock && n.moveConfig == null)
                n.unlockKind = TalentUnlockKind.Flag;
        }
    }
}
