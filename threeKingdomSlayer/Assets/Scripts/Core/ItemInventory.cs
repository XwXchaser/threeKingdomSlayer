using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道具库存 — 单例
///
/// 管理道具型升级奖励（gestureId != null）的获得、消耗、次数追踪。
/// gestureId 作为唯一键：一个手势只对应一种道具，多次获得同一道具叠加使用次数。
/// 
/// useCount=-1 表示无限次，TryConsume 永远返回 true 但不扣减。
/// </summary>
public class ItemInventory : MonoBehaviour
{
    public static ItemInventory Instance { get; private set; }

    private Dictionary<string, ItemStock> _items = new Dictionary<string, ItemStock>();

    /// <summary>道具变更事件：(gestureId, remainingUses, wasRemoved)</summary>
    public System.Action<string, int, bool> OnItemChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>添加/叠加道具（由 UpgradeEffectManager.ApplyUpgrade 调用）</summary>
    public void AddItem(UpgradeDefinition def)
    {
        if (_items.TryGetValue(def.gestureId, out var stock))
        {
            // 无限次道具保持无限
            if (stock.remainingUses >= 0 && def.useCount >= 0)
                stock.remainingUses += def.useCount;
            else
                stock.remainingUses = -1; // 任一方无限 → 无限

            _items[def.gestureId] = stock;
        }
        else
        {
            _items[def.gestureId] = new ItemStock { definition = def, remainingUses = def.useCount };
        }

        OnItemChanged?.Invoke(def.gestureId, _items[def.gestureId].remainingUses, false);
        Debug.Log($"[ItemInventory] 获得 {def.displayName} gestureId={def.gestureId} uses={_items[def.gestureId].remainingUses}");
    }

    /// <summary>尝试消耗一次道具。无限次(-1)永远返回true但不扣减。</summary>
    public bool TryConsume(string gestureId)
    {
        if (!_items.TryGetValue(gestureId, out var stock))
            return false;

        if (stock.remainingUses == -1)
        {
            OnItemChanged?.Invoke(gestureId, -1, false);
            return true;
        }

        if (stock.remainingUses <= 0)
            return false;

        stock.remainingUses--;
        bool removed = stock.remainingUses <= 0;

        if (removed)
            _items.Remove(gestureId);
        else
            _items[gestureId] = stock;

        OnItemChanged?.Invoke(gestureId, removed ? 0 : stock.remainingUses, removed);
        Debug.Log($"[ItemInventory] 消耗 {gestureId} remaining={stock.remainingUses} removed={removed}");
        return true;
    }

    /// <summary>检查是否拥有指定手势的道具</summary>
    public bool HasItem(string gestureId)
    {
        return _items.TryGetValue(gestureId, out var s) && s.remainingUses != 0;
    }

    /// <summary>获取道具剩余次数（-1=无限，0=不存在）</summary>
    public int GetRemainingUses(string gestureId)
    {
        return _items.TryGetValue(gestureId, out var s) ? s.remainingUses : 0;
    }

    /// <summary>获取道具定义（供 Whirlwind/Lightning 执行器读取参数）</summary>
    public UpgradeDefinition GetDefinition(string gestureId)
    {
        return _items.TryGetValue(gestureId, out var s) ? s.definition : null;
    }

    /// <summary>清空所有道具（新对局重置）</summary>
    public void ClearAll()
    {
        foreach (var kv in _items)
            OnItemChanged?.Invoke(kv.Key, 0, true);
        _items.Clear();
    }

    private struct ItemStock
    {
        public UpgradeDefinition definition;
        public int remainingUses;
    }
}
