using System;
using UnityEngine;

/// <summary>
/// **玩家的反伤护盾**（玩家侧状态）。
///
/// 2026-10-22 从 <c>UpgradeEffectManager</c> 迁出：护盾是**玩家身上的状态**，
/// 之前却寄居在局内 Build 层（升级效果管理器）里，由玩家在用、由升级写入——归属错位。
/// 现在它是一个挂在玩家状态容器上的独立状态。
///
/// 语义与迁移前完全一致：
///   • CD 就绪且进入蓄力时授予（满额 = 上限值）；
///   • 受击时吸收伤害并按「伤害 × (1 + 反伤加成)」反伤；
///   • 护盾清空（离开蓄力）会触发 OnConsumed（供 ThornArmorEffect）；
///   • CD 在蓄力期间暂停。
/// </summary>
public sealed class ReflectShieldStatus : IStatusEffect
{
    public const string Id = "reflect_shield";

    private int _amount;        // 当前剩余护盾值
    private int _maxAmount;     // 一次授予的上限值
    private float _cooldown;     // 剩余 CD
    private float _interval;     // CD 总长
    private float _bonusPercent; // 反伤加成（0~1）
    private bool _ready;         // CD 是否就绪
    private bool _charging;      // 蓄力中（CD 暂停）

    /// <summary>护盾被消耗完或被清空时触发（供 ThornArmorEffect）。</summary>
    public event Action OnConsumed;

    public string StatusId => Id;

    public bool IsActive => _amount > 0;

    public float GetProgress() => _interval > 0f ? Mathf.Clamp01(1f - _cooldown / _interval) : 1f;

    public int GetStacks() => 0;

    public int GetPriority() => 40;

    /// <summary>当前剩余值（0 = 无盾）。</summary>
    public int Amount => _amount;

    /// <summary>是否持有护盾。</summary>
    public bool HasShield => _amount > 0;

    /// <summary>反伤倍率加成（0~1）。</summary>
    public float BonusPercent => _bonusPercent;

    /// <summary>是否处于蓄力（CD 暂停）。</summary>
    public bool IsCharging => _charging;

    /// <summary>写入升级的数值配置（上限值 / CD / 反伤加成），并立即就绪。</summary>
    public void Configure(int maxAmount, float intervalSeconds, float bonusPercent)
    {
        _maxAmount = maxAmount;
        _interval = intervalSeconds;
        _bonusPercent = bonusPercent;
        _ready = true;
        _cooldown = 0f;
    }

    /// <summary>设置蓄力状态（蓄力期间 CD 暂停）。</summary>
    public void SetCharging(bool charging) => _charging = charging;

    /// <summary>尝试授予护盾（CD 就绪时）。返回授予值，0 = 未就绪。</summary>
    public int TryGrant()
    {
        if (!_ready || _maxAmount <= 0) return 0;
        _ready = false;
        _amount = _maxAmount;
        _cooldown = _interval;
        return _amount;
    }

    /// <summary>吸收伤害并返回反伤值。返回 0 表示无护盾。</summary>
    public float Absorb(float damage)
    {
        if (_amount <= 0) return 0f;
        int absorbed = Mathf.Min(_amount, Mathf.CeilToInt(damage));
        _amount -= absorbed;
        float reflect = damage * (1f + _bonusPercent);
        if (_amount <= 0)
        {
            _amount = 0;
            OnConsumed?.Invoke();
        }
        return reflect;
    }

    /// <summary>清空护盾（离开蓄力时调用）。</summary>
    public void Clear()
    {
        if (_amount > 0)
        {
            _amount = 0;
            OnConsumed?.Invoke();
        }
    }

    /// <summary>CD 推进（蓄力期间暂停）。</summary>
    public void Tick(float deltaTime)
    {
        if (_interval <= 0f) return;
        if (_ready) return;
        if (_charging) return;
        _cooldown -= deltaTime;
        if (_cooldown <= 0f)
        {
            _ready = true;
            _cooldown = 0f;
        }
    }

    /// <summary>反伤盾冷却进度，返回 (fill 0→1, 剩余秒数)。未获得升级返回 (-1, 0)。</summary>
    public (float fill, float remaining) GetCooldown()
    {
        if (_interval <= 0f) return (-1f, 0f);
        if (_ready) return (1f, 0f);
        float remaining = Mathf.Max(_cooldown, 0f);
        float fill = 1f - (remaining / _interval);
        return (fill, remaining);
    }

    /// <summary>开新局时重置全部（不触发 OnConsumed，与迁移前一致）。</summary>
    public void ResetAll()
    {
        _amount = 0;
        _maxAmount = 0;
        _cooldown = 0f;
        _interval = 0f;
        _bonusPercent = 0f;
        _ready = false;
        _charging = false;
    }
}
