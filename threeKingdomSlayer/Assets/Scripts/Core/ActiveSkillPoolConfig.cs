using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActiveSkillPoolConfig", menuName = "一夫当关/主动技能奖励池配置")]
public class ActiveSkillPoolConfig : ScriptableObject
{
    [Header("普通升级三选一权重")]
    public float commonWeight = 0.8f;
    public float rareWeight = 0.15f;
    public float legendaryWeight = 0.05f;

    [Header("主动技能池")]
    public List<WeightedActiveSkill> commonPool = new List<WeightedActiveSkill>();
    public List<WeightedActiveSkill> rarePool = new List<WeightedActiveSkill>();
    public List<WeightedActiveSkill> legendaryPool = new List<WeightedActiveSkill>();
}

[System.Serializable]
public class WeightedActiveSkill
{
    public ActiveSkillDefinition skill;
    [Min(1)] public int weight = 1;
}
