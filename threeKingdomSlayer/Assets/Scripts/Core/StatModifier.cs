using System;
using System.Collections.Generic;

/// <summary>
/// 连击计数模式
/// </summary>
public enum HitIncrementMode
{
    /// <summary>每次攻击波命中 +1 连击</summary>
    PerHit,
    /// <summary>每个被命中的敌人 +1 连击</summary>
    PerEnemy
}

/// <summary>
/// 属性修正类型
/// </summary>
public enum StatModifierType
{
    /// <summary>加法：base + value</summary>
    Add,
    /// <summary>乘法：base * (1 + value)</summary>
    Multiply
}

/// <summary>
/// 单个属性修正
/// </summary>
[Serializable]
public class StatModifier
{
    /// <summary>属性标识（如 "atk", "def", "spd"）</summary>
    public string statId;
    public StatModifierType type;
    public float value;
}

/// <summary>
/// 运行时激活的 Buff 实例
/// </summary>
public class ActiveBuff
{
    /// <summary>Buff 标识，同 buffId 只刷新不叠加</summary>
    public string buffId;
    /// <summary>效果结束时间（Time.time + duration）</summary>
    public float endTime;
    /// <summary>此 Buff 携带的属性修正列表</summary>
    public List<StatModifier> modifiers;
}
