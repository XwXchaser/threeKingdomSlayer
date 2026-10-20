using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActiveSkillPoolConfig", menuName = "一夫当关/主动技能奖励池配置")]
public class ActiveSkillPoolConfig : ScriptableObject
{
    [Header("稀有度出现权重")]
    public float commonWeight = 0.60f;
    public float rareWeight = 0.35f;
    public float legendaryWeight = 0.05f;

    [Header("普通池")]
    public List<WeightedUpgrade> commonPool = new List<WeightedUpgrade>();

    [Header("稀有池")]
    public List<WeightedUpgrade> rarePool = new List<WeightedUpgrade>();

    [Header("传说池")]
    public List<WeightedUpgrade> legendaryPool = new List<WeightedUpgrade>();
}
