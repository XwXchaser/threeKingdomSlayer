using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **状态容器**：谁持有谁挂一个，统一驱动其上状态效果的每帧结算与查询。
///
/// 宿主无关：敌人（<c>Enemy.StatusHost</c>）、玩家（<c>PlayerState.StatusHost</c>）
/// 用的都是这一个容器——状态类不需要知道自己挂在谁身上。
///
/// 只收「可施加 / 可消耗 / 可叠加」的状态；敌人的**常驻机制**（韧性、格挡护盾、描边、
/// 受击反馈、移动、共享血量）不进这里，它们是长期存在的能力而不是会被施加清除的状态。
/// </summary>
public sealed class StatusContainer
{
    private readonly List<IStatusEffect> _effects = new List<IStatusEffect>();

    /// <summary>登记一个状态效果（同一 id 后登记者覆盖先登记者）。</summary>
    public void Add(IStatusEffect effect)
    {
        if (effect == null) return;
        for (int i = 0; i < _effects.Count; i++)
        {
            if (_effects[i] != null && _effects[i].StatusId == effect.StatusId) { _effects[i] = effect; return; }
        }
        _effects.Add(effect);
    }

    /// <summary>移除一个状态效果。</summary>
    public void Remove(IStatusEffect effect)
    {
        if (effect == null) return;
        for (int i = _effects.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_effects[i], effect)) _effects.RemoveAt(i);
    }

    /// <summary>取某个状态效果（没有则 null）。</summary>
    public IStatusEffect Get(string statusId)
    {
        if (string.IsNullOrEmpty(statusId)) return null;
        for (int i = 0; i < _effects.Count; i++)
            if (_effects[i] != null && _effects[i].StatusId == statusId) return _effects[i];
        return null;
    }

    /// <summary>已登记的状态效果数量。</summary>
    public int Count => _effects.Count;

    /// <summary>已登记的状态效果实例（只读）。</summary>
    public IReadOnlyList<IStatusEffect> Instances => _effects;

    /// <summary>统一驱动每帧结算。</summary>
    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            var effect = _effects[i];
            if (effect == null) continue;
            try { effect.Tick(deltaTime); }
            catch (System.Exception e) { Debug.LogError("[Status] " + effect.StatusId + " Tick 失败: " + e); }
        }
    }

    /// <summary>清空全部状态效果（宿主回收 / 开新局时调用）。</summary>
    public void ClearAll()
    {
        for (int i = 0; i < _effects.Count; i++)
        {
            var effect = _effects[i];
            if (effect == null) continue;
            try { effect.Clear(); }
            catch (System.Exception e) { Debug.LogError("[Status] " + effect.StatusId + " Clear 失败: " + e); }
        }
        _effects.Clear();
    }
}
