using UnityEngine;

/// <summary>
/// 敌人配置 - ScriptableObject
/// 策划可在Unity编辑器中创建和配置不同类型的敌人
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyConfig", menuName = "一夫当关/敌人配置")]
public class EnemyConfig : ScriptableObject
{
    [Header("基础属性")]
    public string enemyName = "骷髅兵";
    public int enemyId;
    
    [Header("战斗属性")]
    public float maxHealth = 100f;
    public int occupySlots = 1;          // 站位数（1~5）
    public float attackSpeed = 1f;       // 每秒攻击次数
    public float attackDamage = 10f;     // 攻击力
    public float attackRange = 1f;       // 攻击距离（多少排）
    public float moveSpeed = 1f;         // 前进速度（秒/排）
    
    [Header("架势系统")]
    public float maxPoise = 50f;         // 最大架势值
    public float stunDuration = 1.5f;    // 眩晕时间（秒）
    public float launchDuration = 2f;    // 击飞时间（秒）
    
    [Header("奖励")]
    public int coinReward = 10;          // 击败后掉落铜钱
    
    [Header("BOSS")]
    public bool isBoss = false;
    
    [Header("弱点系统")]
    public float stabDamageMultiplier = 1f;      // 戳击伤害倍率
    public float slashDamageMultiplier = 1f;     // 斩击伤害倍率
    public float pierceDamageMultiplier = 1f;    // 穿刺伤害倍率
    public float sweepDamageMultiplier = 1f;     // 横扫伤害倍率
    public float launchDamageMultiplier = 1f;    // 挑飞伤害倍率
    public float poiseDamageMultiplier = 1f;     // 架势伤害倍率
}
