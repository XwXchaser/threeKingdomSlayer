using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武将配置 - ScriptableObject
/// 不同武将拥有不同的属性数值和技能组合
/// </summary>
[CreateAssetMenu(fileName = "NewHeroConfig", menuName = "一夫当关/武将配置")]
public class HeroConfig : ScriptableObject
{
    [Header("基本信息")]
    public string heroName = "赵云";
    public int heroId;

    [Header("UI")]
    [Tooltip("英雄 HUD 皮肤（推荐）：同一套场景HUD布局，根据武将替换素材与扩展UI")]
    public HeroHUDSkin hudSkin;
    [Tooltip("英雄 HUD Prefab（旧方案/兜底）：未配置场景HUD时使用")]
    public GameObject heroHUDPrefab;

    [Header("基础属性")]
    public float maxHealth = 500f;
    public int reviveCount = 3;
    public float reviveHealthPercent = 0.5f;
    [Min(0)] public int itemSlotCount = 2;

    [Header("技能配置")]
    [Tooltip("装配的技能列表。每个 AttackType 对应一个技能配置，策划可拖拽不同的 .asset 来定制武将。")]
    public List<AttackSkillConfig> skillConfigs = new List<AttackSkillConfig>();

    [Header("大招配置")]
    [Tooltip("大招技能配置（独立于普通技能体系）")]
    public UltimateSkillConfig ultimateSkillConfig;

    [Header("伤害加成")]
    [Range(0f, 2f)]
    public float damageBonusPercent = 0f;

    /// <summary>
    /// 根据攻击类型查找技能配置，未找到返回 null
    /// </summary>
    public AttackSkillConfig GetSkillConfig(AttackType attackType)
    {
        for (int i = 0; i < skillConfigs.Count; i++)
        {
            if (skillConfigs[i] != null && skillConfigs[i].attackType == attackType)
                return skillConfigs[i];
        }
        return null;
    }
}
