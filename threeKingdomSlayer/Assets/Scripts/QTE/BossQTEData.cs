using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BOSS 的 QTE 数据配置
/// 包含 QTE 攻击的顺序列表和冷却参数
/// 挂载到 BOSS 的 QTEController 上
/// </summary>
[CreateAssetMenu(fileName = "BossQTEData", menuName = "QTE/Boss QTE Data")]
public class BossQTEData : ScriptableObject
{
    [Header("QTE 攻击列表")]
    [Tooltip("BOSS 的 QTE 攻击列表，按配置顺序循环执行")]
    public List<QTEAttackConfig> qteAttacks = new List<QTEAttackConfig>();

    [Header("循环设置")]
    [Tooltip("是否循环执行 QTE 攻击列表（false 则执行完一轮后停止）")]
    public bool loopAttacks = true;

    [Header("冷却设置")]
    [Tooltip("首次 QTE 攻击的冷却时间（秒），BOSS 进入应战后开始计时")]
    public float firstQTECooldown = 5f;
    [Tooltip("QTE 攻击之间的基础冷却时间（秒）")]
    public float baseQTECooldown = 5f;
}
