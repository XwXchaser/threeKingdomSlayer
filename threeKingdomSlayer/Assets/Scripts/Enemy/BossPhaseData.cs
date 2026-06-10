using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BOSS 阶段配置 ScriptableObject
/// 每个 .asset 文件代表 BOSS 的一个阶段，包含攻击序列、QTE数据、霸体初始值等
/// </summary>
[CreateAssetMenu(menuName = "Game/BossPhaseData")]
public class BossPhaseData : ScriptableObject
{
    [Header("阶段标识")]
    public int phaseIndex;
    public string phaseName;

    [Header("触发条件")]
    [Range(0f, 1f)] public float triggerHealthPercent;  // 血量低于此百分比触发转阶段

    [Header("攻击配置")]
    public List<AttackStep> attackSequence;
    public BossQTEData qteData;

    [Header("行动调度")]
    [Tooltip("普通攻击权重")]
    public float normalAttackWeight = 1f;
    [Tooltip("C技权重")]
    public float cAttackWeight = 1f;
    [Tooltip("QTE权重 (0 = 该阶段无QTE)")]
    public float qteWeight = 0.5f;
    [Tooltip("行动间冷却 [min, max]（秒）")]
    public Vector2 actionInterval = new Vector2(0.3f, 1f);
    [Tooltip("QTE后强制冷却（秒）")]
    public float postQTECooldown = 5f;

    [Header("霸体")]
    public bool isSuperArmor;

    [Header("弱点倍率")]
    public float stabDamageMultiplier = 1f;
    public float slashDamageMultiplier = 1f;
    public float pierceDamageMultiplier = 1f;
    public float sweepDamageMultiplier = 1f;
    public float launchDamageMultiplier = 1f;
    public float poiseDamageMultiplier = 1f;

    [Header("转阶段动画")]
    public string transitionTriggerName;
    public float transitionDuration = 1.5f;
}
