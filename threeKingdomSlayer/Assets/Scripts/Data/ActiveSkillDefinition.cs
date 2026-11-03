using System.Collections.Generic;
using UnityEngine;

public enum ItemRuleVersion
{
    V1_LimitedItem,
    V2_ActiveSkill
}

public enum ActiveSkillEffectType
{
    FireAoe,
    ArrowRain,
    Cyclone,
    FireLine,
    ChargeAttackShockwave,
    Wave,
    Disease,
    IceMist
}

[CreateAssetMenu(fileName = "ActiveSkillDefinition", menuName = "一夫当关/主动技能定义")]
public class ActiveSkillDefinition : UpgradeDefinition
{
    [Header("主动技能")]
    public ActiveSkillEffectType activeEffectType;
    [Tooltip("按等级配置冷却时间。index 0 = Lv1")]
    public List<float> cooldownLevels = new List<float>();

    public float GetCooldown(int level)
    {
        if (cooldownLevels == null || cooldownLevels.Count == 0)
            return 0f;
        return Mathf.Max(0f, cooldownLevels[Mathf.Clamp(level - 1, 0, cooldownLevels.Count - 1)]);
    }

    /// <summary>主动技能的等级数据行数 = 冷却表行数（每级一条）。</summary>
    public override int GetAvailableLevelCount() => cooldownLevels != null ? cooldownLevels.Count : 0;
}
