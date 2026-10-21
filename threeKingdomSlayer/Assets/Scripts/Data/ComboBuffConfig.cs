using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个连击 Buff 触发配置
/// </summary>
[Serializable]
public class ComboBuffTrigger
{
    /// <summary>触发所需连击数</summary>
    public int comboThreshold;
    /// <summary>Buff 标识（相同 buffId 的不同阈值叠加为同一 Buff）</summary>
    public string buffId;
    /// <summary>此档位的单个属性修正</summary>
    public StatModifier modifier;
}

/// <summary>
/// 连击 Buff 配置 - ScriptableObject
/// 策划在 Inspector 中配置连击阈值、Buff 效果和持续时间。
/// </summary>
[CreateAssetMenu(fileName = "ComboBuffConfig", menuName = "一夫当关/连击Buff配置")]
public class ComboBuffConfig : ScriptableObject
{
    [Header("连击重置")]
    [Tooltip("基础重置窗口（高连击时 ComboManager 动态压缩，参见 GetEffectiveResetDelay）")]
    public float resetDelay = 3f;

    [Header("计数模式")]
    [Tooltip("按攻击次数还是按命中敌人数量增加连击")]
    public HitIncrementMode hitIncrementMode = HitIncrementMode.PerHit;

    [Header("触发列表")]
    [Tooltip("连击阈值 → Buff 效果映射，按阈值升序排列")]
    public List<ComboBuffTrigger> triggers = new List<ComboBuffTrigger>();
}
