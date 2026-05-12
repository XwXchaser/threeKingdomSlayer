using UnityEngine;

/// <summary>
/// 攻击技能配置 - ScriptableObject
/// 每个技能配置是一个独立的 .asset 文件，包含伤害、范围、冷却等所有参数。
/// 策划可在 Inspector 中拖拽不同的技能资产来装配武将的攻击组合。
/// </summary>
[CreateAssetMenu(fileName = "AttackSkillConfig", menuName = "一夫当关/攻击技能配置")]
public class AttackSkillConfig : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("技能编号，唯一标识")]
    public int id;
    public AttackType attackType;
    public DamageType damageType = DamageType.Stab;

    [Header("伤害")]
    [Tooltip("基础伤害")]
    public float damage = 30f;
    [Tooltip("架势伤害（Launch/Parry 使用）")]
    public float poiseDamage = 0f;

    [Header("范围与冷却")]
    [Tooltip("影响排数")]
    public int rangeRows = 1;
    [Tooltip("冷却时间（秒）")]
    public float cooldown = 0.5f;

    [Header("挑飞特殊参数")]
    [Tooltip("挑飞持续时间（秒），仅 Launch 有效")]
    public float launchDuration = 2f;

    [Header("特效")]
    [Tooltip("攻击波预制体（可为空，使用默认 Quad）")]
    public GameObject attackWavePrefab;

    [Header("大招")]
    [Tooltip("命中时获得能量（非大招技能有效）")]
    public int ultimateEnergyGain = 10;
}
