using UnityEngine;

/// <summary>
/// 大招技能配置 - ScriptableObject
/// 独立于普通攻击技能的配置体系。每个大招是一个独立的 .asset。
/// 公共字段（cooldown/energyCost/damage）所有大招共用；
/// 类型专用字段（如 berserkDuration）仅特定大招使用。
/// </summary>
[CreateAssetMenu(fileName = "UltimateSkillConfig", menuName = "一夫当关/大招技能配置")]
public class UltimateSkillConfig : ScriptableObject
{
    [Header("基础")]
    [Tooltip("技能编号，唯一标识")]
    public int id;
    [Tooltip("冷却时间（秒）。填 5 = 每 5 秒可发动一次")]
    public float cooldown = 5f;
    [Tooltip("消耗能量值")]
    public int energyCost = 100;

    [Header("伤害（可选）")]
    [Tooltip("基础伤害。无伤害的大招填 0")]
    public float damage = 100f;
    public DamageType damageType = DamageType.Stab;

    [Header("狂怒参数（仅 Berserk 类型大招使用）")]
    [Tooltip("持续时间（秒）")]
    public float berserkDuration = 5f;
    [Tooltip("自动 Stab 间隔（秒）")]
    public float berserkStabCooldown = 0.5f;
    [Tooltip("伤害倍率")]
    public float berserkDamageMultiplier = 1.5f;
}
