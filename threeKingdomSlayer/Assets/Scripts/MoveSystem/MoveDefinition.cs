using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个招式节点。
/// 招式的数值与表现全部来自现有 AttackSkillConfig；本资产只补「节点身份 + 接续关系」，
/// 因此同一个 AttackSkillConfig 可以被插入任意位置复用。
/// </summary>
[CreateAssetMenu(fileName = "MoveDefinition", menuName = "一夫当关/招式节点")]
public class MoveDefinition : ScriptableObject
{
    [Header("身份")]
    [Tooltip("节点标识，用于日志与解锁集合")]
    public string moveId;

    [Tooltip("显示名（调试面板用）")]
    public string displayName;

    [Header("招式内容")]
    [Tooltip("复用现有招式配置资产：伤害、范围、时长、出手视觉、飞行物都由它决定")]
    public AttackSkillConfig config;

    [Header("接续窗口")]
    [Tooltip("覆盖本节点的接续窗口。关闭时按招式类型派生默认窗口（见设计文档 3.7）")]
    public bool overrideWindow = false;

    [Range(0f, 1f)] public float windowStart01 = 0f;

    [Range(0f, 1f)] public float windowEnd01 = 1f;

    [Header("接续规则")]
    [Tooltip("允许用进入本节点的手势重复本节点（Stab→Stab→Stab）。关闭即为无双式串尾终止")]
    public bool repeatSelf = true;

    [Tooltip("后继输入。未配置的输入在本节点内不响应")]
    public List<MoveEdge> edges = new List<MoveEdge>();

    /// <summary>本节点对应的攻击类型；决定默认窗口与底层执行分支</summary>
    public AttackType MoveAttackType => config != null ? config.attackType : AttackType.Stab;

    /// <summary>本节点展示名；未配置时回落到资产名</summary>
    public string ResolvedName
    {
        get
        {
            if (!string.IsNullOrEmpty(displayName)) return displayName;
            if (!string.IsNullOrEmpty(moveId)) return moveId;
            return name;
        }
    }
}
