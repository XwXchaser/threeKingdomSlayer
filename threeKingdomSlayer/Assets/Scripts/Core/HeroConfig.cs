using UnityEngine;

/// <summary>
/// 武将配置 - ScriptableObject
/// 不同武将拥有不同的属性数值
/// </summary>
[CreateAssetMenu(fileName = "NewHeroConfig", menuName = "一夫当关/武将配置")]
public class HeroConfig : ScriptableObject
{
    [Header("基本信息")]
    public string heroName = "赵云";
    public int heroId;
    
    [Header("基础属性")]
    public float maxHealth = 500f;
    public int reviveCount = 3;
    public float reviveHealthPercent = 0.5f; // 复活时回复生命值百分比
    
    [Header("戳击属性")]
    public float stabDamage = 30f;
    public int stabRangeRows = 1;       // 影响多少排
    public float stabCooldown = 0.3f;   // 冷却时间（秒）
    
    [Header("斩击属性")]
    public float slashDamage = 20f;
    public int slashRangeRows = 2;      // 影响多少排
    public float slashCooldown = 0.8f;
    
    [Header("穿刺属性")]
    public float pierceDamage = 80f;
    public int pierceRangeRows = 5;     // 影响多少排
    public float pierceCooldown = 1.5f;
    
    [Header("横扫属性")]
    public float sweepDamage = 40f;
    public int sweepRangeRows = 3;      // 影响多少排
    public float sweepCooldown = 1.2f;
    
    [Header("挑飞属性")]
    public float launchDamage = 25f;
    public int launchRangeRows = 2;     // 影响多少排
    public float launchCooldown = 1.0f;
    public float launchPoiseDamage = 30f; // 挑飞架势伤害
    
    [Header("招架属性")]
    public float parryDamage = 15f;
    public float parryPoiseDamage = 40f;
    
    [Header("伤害加成")]
    [Range(0f, 2f)]
    public float damageBonusPercent = 0f; // 百分比伤害加成（0=无加成）
}
