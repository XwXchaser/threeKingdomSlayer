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
    [Tooltip("奖励类型：数值buff | 道具 | 被动攻击")]
    public UpgradeCategory category;
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
    [Tooltip("每级浮点加成 — 填此处：damage_multiplier/attack_speed/move_speed/exp_multiplier（0.05 = 每级+5%）")]
    public float floatValue;
    [Tooltip("每级整数加成 — 填此处：stab_range_boost 范围排数 / on_attack_trigger 触发次数 / unlock_attack 等")]
    public int intValue;
    [Tooltip("每级第二整数加成 — 仅 stab_range_boost 填此处（伤害惩罚%，5 = -5%/级）")]
    public int secondaryIntValue;
    [Tooltip("字符串参数（如on_kill_chance的掉落类型）")]
    public string stringValue;

    [Header("攻击解锁（仅 unlock_attack）")]
    [Tooltip("基础攻击技能配置骨架")]
    public AttackSkillConfig baseAttackConfig;

    [Header("UI 图标")]
    [Tooltip("所有类型的升级均需配置图标，供 BuffDisplayPanel 显示")]
    public Sprite icon;

    [Header("被动攻击型（category=Passive 时生效）")]
    [Tooltip("触发阈值（每X次攻击触发一次效果）")]
    public int triggerParam;
    [Tooltip("幻影攻击列表（多段幻影依次执行），每段配置伤害比例与透明度")]
    public List<PhantomStep> phantomSteps = new List<PhantomStep>();

    [Header("道具型（category=Item 时生效）")]
    [Tooltip("获得后可使用的次数，-1=无限次")]
    public int useCount = 1;
    [Tooltip("触发手势: circle(画圈) | long_press_swipe_down(长按下滑)")]
    public string gestureId;

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

/// <summary>
/// 升级奖励类型：数值buff型 | 道具型 | 被动攻击型
/// </summary>
public enum UpgradeCategory
{
    Numeric,   // 数值buff型：伤害/攻速/移速/经验倍率等永久加成
    Item,      // 道具型：手势触发的一次性/限次道具（大旋风、落雷等）
    Passive    // 被动攻击型：每N次攻击自动触发效果
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Legendary
}

/// <summary>
/// 幻影攻击配置段 — 被动攻击型每段幻影的伤害比例和透明度
/// </summary>
[System.Serializable]
public struct PhantomStep
{
    [Tooltip("伤害比例（0.3=30%）")]
    public float damageRatio;
    [Tooltip("透明度（0.6=60%）")]
    public float alpha;
}
