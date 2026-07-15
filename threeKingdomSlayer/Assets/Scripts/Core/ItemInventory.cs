using System.Collections.Generic;
using UnityEngine;

/// <summary>道具库存：按槽位保存道具份数，可在开局前选择是否允许同类堆叠。</summary>
public class ItemInventory : MonoBehaviour
{
    public sealed class ItemEntry
    {
        public int id;
        public UpgradeDefinition definition;
        public int remainingUses;
        public bool isPotion;
        public string GestureId => isPotion ? HealthPotionGestureId : definition != null ? definition.gestureId : null;
    }

    public const string HealthPotionGestureId = "health_potion";
    public static ItemInventory Instance { get; private set; }

    [Header("局外能力测试")]
    [Tooltip("仅在开始对局前配置。开启后，同类道具合并到同一槽位。")]
    [SerializeField] private bool allowSameTypeStacking;

    private readonly List<ItemEntry> _entries = new List<ItemEntry>();
    private int _nextEntryId = 1;

    public System.Action OnInventoryChanged;
    public IReadOnlyList<ItemEntry> Entries => _entries;
    public bool AllowSameTypeStacking => allowSameTypeStacking;
    public int Capacity => PlayerState.Instance != null && PlayerState.Instance.heroConfig != null
        ? Mathf.Max(0, PlayerState.Instance.heroConfig.itemSlotCount)
        : 2;
    public bool HasFreeSlot => _entries.Count < Capacity;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public void SetSameTypeStackingForNextRun(bool enabled)
    {
        if (_entries.Count > 0) { Debug.LogWarning("[ItemInventory] 对局内不支持切换同类堆叠规则"); return; }
        allowSameTypeStacking = enabled;
    }

    public bool CanAdd(UpgradeDefinition def)
    {
        if (def == null || string.IsNullOrEmpty(def.gestureId)) return false;
        return HasFreeSlot || (allowSameTypeStacking && FindFirst(def.gestureId) != null);
    }

    public bool AddItem(UpgradeDefinition def)
    {
        if (!CanAdd(def)) { Debug.LogWarning($"[ItemInventory] 道具栏已满，无法获得 {def?.displayName}"); return false; }
        var entry = allowSameTypeStacking ? FindFirst(def.gestureId) : null;
        if (entry != null) entry.remainingUses = MergeUses(entry.remainingUses, def.useCount);
        else { entry = new ItemEntry { id = _nextEntryId++, definition = def, remainingUses = def.useCount }; _entries.Add(entry); }
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool CanAddPotion() => HasFreeSlot || FindFirst(HealthPotionGestureId) != null;

    public bool AddPotion(UpgradeDefinition definition, int maxStack)
    {
        var entry = FindFirst(HealthPotionGestureId);
        if (entry == null)
        {
            if (!HasFreeSlot) return false;
            entry = new ItemEntry { id = _nextEntryId++, definition = definition, isPotion = true };
            _entries.Add(entry);
        }
        if (entry.remainingUses >= maxStack) return false;
        entry.remainingUses++;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryConsume(string gestureId) { var entry = FindFirst(gestureId); return entry != null && TryConsumeEntry(entry.id); }

    public bool TryConsumeEntry(int entryId)
    {
        var entry = FindById(entryId);
        if (entry == null || entry.remainingUses == 0) return false;
        if (entry.remainingUses < 0) return true;
        entry.remainingUses--;
        if (entry.remainingUses <= 0) _entries.Remove(entry);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string gestureId) => FindFirst(gestureId) != null;

    public int GetRemainingUses(string gestureId)
    {
        int total = 0;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.GestureId != gestureId) continue;
            if (entry.remainingUses < 0) return -1;
            total += entry.remainingUses;
        }
        return total;
    }

    public UpgradeDefinition GetDefinition(string gestureId) => FindFirst(gestureId)?.definition;
    public ItemEntry GetEntry(int entryId) => FindById(entryId);

    public void ClearAll() { _entries.Clear(); _nextEntryId = 1; OnInventoryChanged?.Invoke(); }

    private ItemEntry FindFirst(string gestureId)
    {
        for (int i = 0; i < _entries.Count; i++) if (_entries[i].GestureId == gestureId) return _entries[i];
        return null;
    }

    private ItemEntry FindById(int entryId)
    {
        for (int i = 0; i < _entries.Count; i++) if (_entries[i].id == entryId) return _entries[i];
        return null;
    }

    private static int MergeUses(int current, int added) => current < 0 || added < 0 ? -1 : current + added;
}
