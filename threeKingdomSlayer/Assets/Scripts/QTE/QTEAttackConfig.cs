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
    [Tooltip("QTE 攻击期间播放的精灵帧动画（按顺序播放一次）")]
    public Sprite[] qteAnimationFrames;
    [Tooltip("精灵动画播放速率（帧/秒）")]
    public float qteAnimationFPS = 12f;
    [Tooltip("动画播放到 QTE 起始的延迟（秒）")]
    public float animationLeadTime = 0.3f;

    [Header("飞行物（可选）")]
    [Tooltip("飞行物 prefab（如箭矢、能量弹），为空则无飞行物")]
    public GameObject projectilePrefab;
    [Tooltip("飞行物从 BOSS 位置飞向目标的时长（秒）")]
    public float projectileFlightTime = 0.8f;
    [Tooltip("飞行物目标位置的 Z 轴偏移（相对于第一排敌人位置）")]
    public float projectileTargetZ = -2f;

    [Header("冷却")]
    [Tooltip("QTE 攻击结束后的冷却时间（秒）")]
    public float cooldownAfterQTE = 3f;

    /// <summary>
    /// 本次 QTE 攻击的总持续时间（最晚出现的 QTE 的 delay + warning + judgeWindow）
    /// </summary>
    public float TotalDuration
    {
        get
        {
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
}
