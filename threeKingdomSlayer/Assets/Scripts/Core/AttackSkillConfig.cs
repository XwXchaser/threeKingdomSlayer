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
    [Tooltip("冷却时间（秒）— 旧模式：独立技能CD")]
    public float cooldown = 0.5f;
    [Tooltip("攻击动作时长（秒）— 新模式：动作锁定。攻速会缩放此值。应与该攻击的动画/特效总时长匹配，让玩家感知到'因为还在挥刀所以不能做其他动作'")]
    public float actionDuration = 0.3f;

    [Header("挑飞特殊参数")]
    [Tooltip("挑飞持续时间（秒），仅 Launch 有效")]
    public float launchDuration = 2f;

    [Header("特效")]
    [Tooltip("攻击波预制体（可为空，使用默认 Quad）")]
    public GameObject attackWavePrefab;

    [Header("戳击偏移")]
    [Tooltip("生成位置 Y 偏移（相对敌人中心）")]
    public float stabSpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对敌人中心）")]
    public float stabSpawnZOffset = 0.5f;
    [Tooltip("仅戳刺视觉沿攻击方向额外前伸的世界距离，不影响命中、伤害或射程")]
    public float stabVisualReachOffset = 0.5f;

    [Header("斩击扇形扫掠")]
    [Tooltip("扫掠半宽（X 轴范围，默认 5）")]
    public float slashSweepHalfWidth = 5f;
    [Tooltip("扇形旋转角度（度，默认 50）")]
    public float slashSweepAngle = 50f;
    [Tooltip("扫掠持续时长（秒，默认 0.25）")]
    public float slashSweepDuration = 0.25f;
    [Tooltip("生成位置 Y 偏移（相对敌人中心）")]
    public float slashSpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对敌人中心）")]
    public float slashSpawnZOffset = 0.5f;

    [Header("招架扫掠")]
    [Tooltip("Z 轴旋转幅度（度，默认 100）。枪尾从起始角扫到起始角+此值")]
    [Range(30f, 180f)]
    public float parrySweepAngle = 100f;
    [Tooltip("扫掠持续时长（秒，默认 0.25）")]
    [Range(0.1f, 0.5f)]
    public float parrySweepDuration = 0.25f;
    [Tooltip("生成位置 X 偏移（相对玩家位置）")]
    public float parrySpawnXOffset = 0f;
    [Tooltip("生成位置 Y 偏移（相对玩家位置）")]
    public float parrySpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对玩家位置）")]
    public float parrySpawnZOffset = 0f;
    [Tooltip("每次招架 Z 起始角随机偏移范围（度，默认 15）")]
    [Range(0f, 30f)]
    public float parryAngleVariance = 15f;

    [Header("挑飞上挑")]
    [Tooltip("Z 轴旋转幅度（度，默认 90）。枪头从低到高上挑")]
    [Range(30f, 180f)]
    public float launchFlickAngle = 90f;
    [Tooltip("上挑持续时长（秒，默认 0.20）")]
    [Range(0.1f, 0.5f)]
    public float launchFlickDuration = 0.20f;
    [Tooltip("生成位置 X 偏移（相对玩家位置）")]
    public float launchSpawnXOffset = 0f;
    [Tooltip("生成位置 Y 偏移（相对玩家位置）")]
    public float launchSpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对玩家位置）")]
    public float launchSpawnZOffset = 0f;
    [Tooltip("每次上挑 Z 起始角随机偏移范围（度，默认 15）")]
    [Range(0f, 30f)]
    public float launchAngleVariance = 15f;
    [Tooltip("上挑时世界 Y 轴上升高度（默认 1.0）")]
    public float launchRiseHeight = 1.0f;

    [Header("大招")]
    [Tooltip("命中时获得能量（非大招技能有效）")]
    public int ultimateEnergyGain = 10;
}
