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
    IceMist,
    /// <summary>抡斧劈砍（REQ-008）：无视盾牌减伤 + 必定破盾。</summary>
    AxeSmash,
    /// <summary>追命锁链（REQ-009）：把**最远、最弱**的敌人拉到最前排（排序工具，不是伤害工具）。</summary>
    ChainPull
}

[CreateAssetMenu(fileName = "ActiveSkillDefinition", menuName = "一夫当关/主动技能定义")]
public class ActiveSkillDefinition : UpgradeDefinition
{
    [Header("主动技能")]
    public ActiveSkillEffectType activeEffectType;
    [Tooltip("按等级配置冷却时间。index 0 = Lv1")]
    public List<float> cooldownLevels = new List<float>();

    [Header("暗源（释放消耗，2026-11-16）")]
    [Tooltip("**释放该技能消耗的暗源**（逐等级；index 0 = Lv1）。\n" +
             "暗源**只靠进攻获取**（命中 / 击杀）→ 防御姿态不回 → 防止“按住防御纯放技能”。\n" +
             "空列表或全 0 = 不消耗（现有技能保持原样）。")]
    public List<float> shadowCostLevels = new List<float>();

    /// <summary>该等级释放一次消耗的暗源（0 = 不消耗）。</summary>
    public float GetShadowCost(int level)
    {
        if (shadowCostLevels == null || shadowCostLevels.Count == 0) return 0f;
        int idx = Mathf.Clamp(level - 1, 0, shadowCostLevels.Count - 1);
        return Mathf.Max(0f, shadowCostLevels[idx]);
    }

    public float GetCooldown(int level)
    {
        if (cooldownLevels == null || cooldownLevels.Count == 0)
            return 0f;
        return Mathf.Max(0f, cooldownLevels[Mathf.Clamp(level - 1, 0, cooldownLevels.Count - 1)]);
    }

    /// <summary>主动技能的等级数据行数 = 冷却表行数（每级一条）。</summary>
    public override int GetAvailableLevelCount() => cooldownLevels != null ? cooldownLevels.Count : 0;
}
