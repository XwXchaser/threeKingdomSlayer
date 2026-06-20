using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QTE 攻击中单个 QTE 判断的出现时机
/// </summary>
[Serializable]
public struct QTESlot
{
    [Tooltip("QTE 配置")]
    public QTEConfig config;
    [Tooltip("延迟出现时间（秒）：QTE 攻击开始后，此 QTE 相对于攻击开始的延迟")]
    public float delay;
}

/// <summary>
/// 一次 QTE 攻击的完整配置
/// 可包含多个 QTE 判断（如 3 次点击 + 1 次划动）
/// </summary>
[CreateAssetMenu(fileName = "QTEAttackConfig", menuName = "QTE/QTE Attack Config")]
public class QTEAttackConfig : ScriptableObject
{
    [Header("QTE 队列")]
    [Tooltip("本次 QTE 攻击包含的 QTE 判断列表，按出现顺序排列")]
    public List<QTESlot> qteSlots = new List<QTESlot>();

    [Header("BOSS 演出")]
    [Tooltip("QTE 攻击期间播放的 AnimationClip（单段模式）")]
    public AnimationClip qteAnimationClip;
    [Tooltip("动画总时长（秒），0 = 自动根据 QTE 槽位计算")]
    public float animationDuration;
    [Tooltip("动画播放到 QTE 起始的延迟（秒）")]
    public float animationLeadTime = 0.3f;

    [Header("BOSS 演出（多段模式，设置后优先于单段 qteAnimationClip）")]
    [Tooltip("QTE 开始阶段播放的动画（单次）")]
    public AnimationClip animationStartClip;
    [Tooltip("QTE 判定阶段循环播放的动画")]
    public AnimationClip animationLoopClip;
    [Tooltip("QTE 结束阶段播放的动画（单次）")]
    public AnimationClip animationEndClip;

    [Header("飞行物（可选）")]
    [Tooltip("飞行物 prefab（如箭矢、能量弹），为空则无飞行物")]
    public GameObject projectilePrefab;
    [Tooltip("飞行物从 BOSS 位置飞向目标的时长（秒）")]
    public float projectileFlightTime = 0.8f;
    [Tooltip("飞行物目标位置的 Z 轴偏移（相对于第一排敌人位置）")]
    public float projectileTargetZ = -2f;

    [Header("箭矢波（可选，用于多段防御型 QTE）")]
    [Tooltip("箭矢 prefab（需要 EnemyProjectile 组件），为空则不生成箭矢波")]
    public GameObject arrowPrefab;
    [Tooltip("每个 QTE slot 射出的箭矢数量")]
    public int arrowsPerWave = 5;
    [Tooltip("箭矢抛物线最高点高度")]
    public float arrowArcHeight = 3f;
    [Tooltip("箭矢生成位置的 Z 偏移（相对于 row5 位置）")]
    public float arrowSpawnOffsetZ = 1f;
    [Tooltip("箭矢水平散布范围（X 轴）")]
    public float arrowSpreadX = 3f;
    [Tooltip("箭矢飞行时间（秒），必须与 QTE warningDuration 对齐")]
    public float arrowFlightTime = 1.5f;

    [Header("QTE 模式")]
    [Tooltip("防御型 QTE：成功不造成 poise 伤害，箭矢命中才是威胁")]
    public bool isDefensiveQTE;
    [Tooltip("QTE 阶段固定时长（秒），>0 时到期强制结束，不等待全部 slot resolve")]
    public float fixedQteDuration;

    [Header("打断")]
    [Tooltip("QTE 攻击是否可被 stun 打断")]
    public bool interruptibleOnStun = true;

    [Header("冷却")]
    [Tooltip("QTE 攻击结束后的冷却时间（秒）")]
    public float cooldownAfterQTE = 3f;

    /// <summary>
    /// 是否使用多段动画模式（start/loop/end）
    /// </summary>
    public bool UseMultiPhaseAnimation => animationStartClip != null && animationLoopClip != null && animationEndClip != null;

    /// <summary>
    /// 本次 QTE 攻击的总持续时间（最晚出现的 QTE 的 delay + warning + judgeWindow）
    /// 如果设置了 fixedQteDuration > 0，则使用固定时长
    /// </summary>
    public float TotalDuration
    {
        get
        {
            if (fixedQteDuration > 0f) return fixedQteDuration;
            float maxEnd = 0f;
            foreach (var slot in qteSlots)
            {
                if (slot.config != null)
                {
                    float end = slot.delay + slot.config.warningDuration + slot.config.judgeWindow;
                    if (end > maxEnd) maxEnd = end;
                }
            }
            return maxEnd;
        }
    }

    /// <summary>
    /// QTE 阶段开始到结束的总时间（fixedQteDuration 或自动计算）
    /// </summary>
    public float QTEPhaseDuration => fixedQteDuration > 0f ? fixedQteDuration : TotalDuration;
}
