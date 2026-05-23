using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级奖励定义 - ScriptableObject
/// 每个三选一奖励选项一个 .asset 文件，包含显示名、效果类型、每级数值、前置条件等。
/// 策划可在 Inspector 中配置。
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "一夫当关/升级奖励定义")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("标识")]
    [Tooltip("唯一标识符")]
    public string upgradeId;

    [Header("显示")]
    [Tooltip("奖励名称，如「神力」")]
    public string displayName;
    [Tooltip("效果描述模板，{0}=数值，如「造成伤害提升 {0}%」")]
    public string descriptionTemplate;

    [Header("稀有度与等级")]
    public UpgradeRarity rarity = UpgradeRarity.Common;
    [Tooltip("最高等级（1-10）")]
    public int maxLevel = 10;

    [Header("效果")]
    [Tooltip("效果类型: damage_multiplier | attack_speed | on_attack_trigger | on_kill_chance | unlock_attack")]
    public string effectType;
    [Tooltip("每级浮点叠加值（如0.1=每级+10%）")]
    public float floatValue;
    [Tooltip("整数参数（如on_attack_trigger的触发次数）")]
    public int intValue;
    [Tooltip("字符串参数（如on_kill_chance的掉落类型）")]
    public string stringValue;

    [Header("攻击解锁（仅 unlock_attack）")]
    [Tooltip("基础攻击技能配置骨架")]
    public AttackSkillConfig baseAttackConfig;

    [Header("前置条件")]
    [Tooltip("需要其他选项达到指定等级后才会进入抽取池")]
    public List<UpgradePrerequisite> prerequisites;
}

[System.Serializable]
public class UpgradePrerequisite
{
    [Tooltip("需要的前置升级")]
    public UpgradeDefinition requiredUpgrade;
    [Tooltip("前置升级最低等级")]
    public int requiredLevel = 1;
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Legendary
}
