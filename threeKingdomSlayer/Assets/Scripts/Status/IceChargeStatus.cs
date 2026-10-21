using System;
using UnityEngine;

/// <summary>
/// **玩家的破冰预备**（玩家侧状态）：不设时限、挂到被消耗为止、可并存。
///
/// 2026-10-22 从静态类 <c>PlayerElementalBuffs</c> 的字段迁出：它是**玩家身上的状态**，
/// 不该是一份游离的静态数据。现在它挂在玩家的状态容器上，语义与迁移前完全一致。
///
/// 作用域与招式资产的 ColumnMode 配合做合规判定（单列重攻击 / 任意重攻击）。
/// </summary>
public sealed class IceChargeStatus : IStatusEffect
{
    public const string Id = "ice_charge";

    private bool _ready;
    private ElementalScope _scope = ElementalScope.SingleColumnHeavy;

    /// <summary>状态变化（获得 / 消费 / 清空）。HUD 订阅这里刷新图标。</summary>
    public event Action OnChanged;

    public string StatusId => Id;

    public bool IsActive => _ready;

    public float GetProgress() => _ready ? 1f : 0f;

    public int GetStacks() => 0;

    public int GetPriority() => 50;

    /// <summary>当前作用域。</summary>
    public ElementalScope Scope => _scope;

    /// <summary>获得破冰预备（已存在时刷新作用域）。</summary>
    public void Grant(ElementalScope scope = ElementalScope.SingleColumnHeavy)
    {
        bool wasReady = _ready;
        _ready = true;
        _scope = scope;
        DebugLog.Info($"[ElementalBuff] 获得破冰预备 scope={scope}{(wasReady ? "（已存在，刷新作用域）" : "")}");
        OnChanged?.Invoke();
    }

    /// <summary>作用域匹配则消费并返回 true。一次攻击只在首次实际命中时调用。</summary>
    public bool TryConsume(ColumnMode columnMode, bool isHeavy)
    {
        if (!_ready) return false;
        if (!isHeavy) return false;
        if (_scope == ElementalScope.SingleColumnHeavy && columnMode != ColumnMode.SingleColumn) return false;

        _ready = false;
        DebugLog.Info($"[ElementalBuff] 消费破冰预备 (columnMode={columnMode}, scope={_scope})");
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>清空（开新局时调用）。</summary>
    public void Clear()
    {
        bool changed = _ready;
        _ready = false;
        _scope = ElementalScope.SingleColumnHeavy;
        if (changed) OnChanged?.Invoke();
    }

    /// <summary>没有时限，不做每帧结算。</summary>
    public void Tick(float deltaTime) { }
}
