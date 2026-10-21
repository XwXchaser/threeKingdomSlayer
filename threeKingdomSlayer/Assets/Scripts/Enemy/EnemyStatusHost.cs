using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人身上的**一个状态效果实例**（扩展点）。
///
/// 这是「新状态效果」的统一落点：写一个类实现本接口，让它在敌人身上注册，
/// 由 <see cref="EnemyStatusHost"/> 统一驱动每帧结算、由 <see cref="EnemyStatusRegistry"/>
/// 统一提供查询（血条状态、机制判定都能问同一个接口）。
///
/// 已有先例：<c>EnemyFreeze</c>（冰冻）就是这样一个实例。
///
/// 约定：
///   • 实例**自己持有状态**（字段放在实现类里），不由外部管理；
///   • <see cref="Tick"/> 只放「每帧都要跑」的部分；
///     依赖敌人状态机的部分（如「冻结中才结算」）仍由状态机调用；
///   • <see cref="Clear"/> 用于敌人回收 / 死亡时清干净。
/// </summary>
public interface IEnemyStatusInstance
{
    /// <summary>状态 id（如 frozen）。</summary>
    string StatusId { get; }

    /// <summary>当前是否处于该状态。</summary>
    bool IsActive { get; }

    /// <summary>进度（0~1，血条用）。</summary>
    float GetProgress();

    /// <summary>层数（无层数概念的返回 0）。</summary>
    int GetStacks();

    /// <summary>同屏多个状态时的优先级（大的排前面）。</summary>
    int GetPriority();

    /// <summary>每帧结算（由宿主调用）。</summary>
    void Tick(float deltaTime);

    /// <summary>清空（敌人回收 / 死亡时调用）。</summary>
    void Clear();
}

/// <summary>
/// 敌人的**状态效果宿主**：持有该敌人身上的状态效果实例，并统一驱动它们的每帧结算。
///
/// 由 <c>Enemy</c> 懒加载创建；<c>Enemy.Update</c> 调 <see cref="Tick"/>。
/// </summary>
public sealed class EnemyStatusHost
{
    private readonly Enemy _owner;
    private readonly List<IEnemyStatusInstance> _instances = new List<IEnemyStatusInstance>();

    public EnemyStatusHost(Enemy owner)
    {
        _owner = owner;
    }

    /// <summary>该宿主的拥有者。</summary>
    public Enemy Owner => _owner;

    /// <summary>登记一个状态效果实例（同一 id 后登记者覆盖先登记者）。</summary>
    public void Add(IEnemyStatusInstance instance)
    {
        if (instance == null) return;
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i] != null && _instances[i].StatusId == instance.StatusId) { _instances[i] = instance; return; }
        }
        _instances.Add(instance);
    }

    /// <summary>移除一个状态效果实例。</summary>
    public void Remove(IEnemyStatusInstance instance)
    {
        if (instance == null) return;
        for (int i = _instances.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_instances[i], instance)) _instances.RemoveAt(i);
    }

    /// <summary>取某个状态效果实例（没有则 null）。</summary>
    public IEnemyStatusInstance Get(string statusId)
    {
        if (string.IsNullOrEmpty(statusId)) return null;
        for (int i = 0; i < _instances.Count; i++)
            if (_instances[i] != null && _instances[i].StatusId == statusId) return _instances[i];
        return null;
    }

    /// <summary>该宿主上已登记的状态效果数量。</summary>
    public int Count => _instances.Count;

    /// <summary>该宿主上已登记的状态效果实例（只读）。</summary>
    public System.Collections.Generic.IReadOnlyList<IEnemyStatusInstance> Instances => _instances;

    /// <summary>统一驱动每帧结算。</summary>
    public void Tick(float deltaTime)
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            if (instance == null) continue;
            try { instance.Tick(deltaTime); }
            catch (System.Exception e) { Debug.LogError("[EnemyStatus] " + instance.StatusId + " Tick 失败: " + e); }
        }
    }

    /// <summary>清空全部状态效果（敌人回收 / 死亡时调用）。</summary>
    public void ClearAll()
    {
        for (int i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            if (instance == null) continue;
            try { instance.Clear(); }
            catch (System.Exception e) { Debug.LogError("[EnemyStatus] " + instance.StatusId + " Clear 失败: " + e); }
        }
        _instances.Clear();
    }
}
