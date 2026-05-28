using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 屏幕左侧 Buff 图标面板 — 槽位制。
///
/// Column A（数值型 + 被动型）：持久，永不删除，再次获得仅更新角标。
/// Column B（道具型）：可消耗，消耗后槽位清空、后续槽位前移补位。
///
/// 在 Inspector 中将 BuffIcon 实例拖入 _columnASlots / _columnBSlots，
/// 槽位按列表顺序分配（0 → N），代码不创建/销毁实例。
/// </summary>
public class BuffDisplayPanel : MonoBehaviour
{
    [SerializeField] private List<BuffIcon> _columnASlots = new List<BuffIcon>();
    [SerializeField] private List<BuffIcon> _columnBSlots = new List<BuffIcon>();

    [Header("底框精灵")]
    [Tooltip("index 0 = Lv.1, index 4 = Lv.5")]
    [SerializeField] private Sprite[] _levelFrames = new Sprite[5];
    [Tooltip("道具型统一底框")]
    [SerializeField] private Sprite _skillFrame;

    // upgradeId → slot（ColumnA）
    private Dictionary<string, BuffIcon> _upgradeIcons = new Dictionary<string, BuffIcon>();
    // gestureId → slot（ColumnB）
    private Dictionary<string, BuffIcon> _itemIcons = new Dictionary<string, BuffIcon>();

    private CanvasGroup _canvasGroup;
    private bool _firstUpgradeReceived;
    private int _columnBUsedCount;

    private void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;

        foreach (var slot in _columnASlots)
            if (slot != null) slot.gameObject.SetActive(false);
        foreach (var slot in _columnBSlots)
            if (slot != null) slot.gameObject.SetActive(false);

        if (UpgradeEffectManager.Instance != null)
            UpgradeEffectManager.Instance.OnUpgradeApplied += OnUpgradeApplied;
        if (ItemInventory.Instance != null)
            ItemInventory.Instance.OnItemChanged += OnItemChanged;
        if (PassiveTriggerModule.Instance != null)
            PassiveTriggerModule.Instance.OnPassiveRegistered += OnPassiveRegistered;
    }

    private void OnDestroy()
    {
        if (UpgradeEffectManager.Instance != null)
            UpgradeEffectManager.Instance.OnUpgradeApplied -= OnUpgradeApplied;
        if (ItemInventory.Instance != null)
            ItemInventory.Instance.OnItemChanged -= OnItemChanged;
        if (PassiveTriggerModule.Instance != null)
            PassiveTriggerModule.Instance.OnPassiveRegistered -= OnPassiveRegistered;
    }

    // ── 槽位分配 ──

    private BuffIcon ClaimColumnASlot()
    {
        foreach (var slot in _columnASlots)
            if (slot != null && !slot.gameObject.activeSelf)
                return slot;
        Debug.LogWarning("[BuffDisplayPanel] ColumnA 槽位已满");
        return null;
    }

    private BuffIcon ClaimColumnBSlot()
    {
        if (_columnBUsedCount >= _columnBSlots.Count)
        {
            Debug.LogWarning("[BuffDisplayPanel] ColumnB 槽位已满");
            return null;
        }
        return _columnBSlots[_columnBUsedCount++];
    }

    /// <summary>ColumnB 槽位前移补位：将 removedIndex 之后的已用槽依次前移</summary>
    private void CompactColumnB(int removedIndex)
    {
        for (int i = removedIndex; i < _columnBUsedCount - 1; i++)
        {
            var from = _columnBSlots[i + 1];
            var to = _columnBSlots[i];
            if (from == null || to == null) continue;

            to.Setup(from.IconSprite, from.UpgradeId, from.Category, from.GestureId);
            to.SetBadge(from.BadgeText);
            to.SetFrame(_skillFrame);
            to.OnClicked -= OnItemIconClicked;
            to.OnClicked += OnItemIconClicked;
            to.gameObject.SetActive(true);

            if (!string.IsNullOrEmpty(from.GestureId))
                _itemIcons[from.GestureId] = to;

            from.ResetSlot();
        }

        var last = _columnBSlots[_columnBUsedCount - 1];
        if (last != null) last.ResetSlot();
        _columnBUsedCount--;
    }

    // ── 事件回调 ──

    private void OnUpgradeApplied(UpgradeDefinition def, int newLevel)
    {
        if (!_firstUpgradeReceived)
        {
            _firstUpgradeReceived = true;
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
        }

        if (def.category == UpgradeCategory.Item)
        {
            if (!_itemIcons.TryGetValue(def.gestureId, out var icon))
            {
                icon = ClaimColumnBSlot();
                if (icon == null) return;

                icon.Setup(def.icon, def.upgradeId, UpgradeCategory.Item, def.gestureId);
                icon.SetFrame(_skillFrame);
                icon.OnClicked += OnItemIconClicked;
                icon.gameObject.SetActive(true);
                _itemIcons[def.gestureId] = icon;
            }

            int uses = ItemInventory.Instance != null ? ItemInventory.Instance.GetRemainingUses(def.gestureId) : def.useCount;
            icon.SetBadge(uses == -1 ? "∞" : uses.ToString());
        }
        else
        {
            if (!_upgradeIcons.TryGetValue(def.upgradeId, out var icon))
            {
                icon = ClaimColumnASlot();
                if (icon == null) return;

                icon.Setup(def.icon, def.upgradeId, def.category, null);
                icon.gameObject.SetActive(true);
                _upgradeIcons[def.upgradeId] = icon;
                icon.SetFrame(GetLevelFrame(newLevel));
            }

            icon.SetFrame(GetLevelFrame(newLevel));

            if (def.category == UpgradeCategory.Passive)
            {
                def.GetPhantomConfig(newLevel, out int threshold, out _);
                icon.SetBadge(threshold.ToString());
            }
            else
            {
                icon.SetBadge($"Lv.{newLevel}");
            }
        }
    }

    private void OnItemChanged(string gestureId, int remainingUses, bool wasRemoved)
    {
        if (!_itemIcons.TryGetValue(gestureId, out var icon)) return;

        if (wasRemoved)
        {
            icon.OnClicked -= OnItemIconClicked;
            int index = _columnBSlots.IndexOf(icon);
            if (index >= 0)
            {
                icon.ResetSlot();
                _itemIcons.Remove(gestureId);
                CompactColumnB(index);
            }
        }
        else
        {
            icon.SetBadge(remainingUses == -1 ? "∞" : remainingUses.ToString());
        }
    }

    private void OnPassiveRegistered(string upgradeId, int threshold)
    {
        if (_upgradeIcons.TryGetValue(upgradeId, out var icon))
            icon.SetBadge(threshold.ToString());
    }

    private Sprite GetLevelFrame(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, _levelFrames.Length - 1);
        return _levelFrames[index];
    }

    // ── 道具点击 ──

    private void OnItemIconClicked(BuffIcon icon)
    {
        string gestureId = icon.GestureId;
        if (string.IsNullOrEmpty(gestureId)) return;
        if (ItemInventory.Instance == null) return;

        var def = ItemInventory.Instance.GetDefinition(gestureId);
        if (def == null) return;

        if (!ItemInventory.Instance.TryConsume(gestureId)) return;

        if (gestureId == "circle")
        {
            if (WhirlwindController.Instance != null)
                WhirlwindController.Instance.Activate(def);
        }
        else if (gestureId == "long_press_swipe_down")
        {
            if (InputManager.Instance != null)
                InputManager.Instance.ExecuteLightning(def);
        }
        else if (gestureId == "damage_boost")
        {
            if (UpgradeEffectManager.Instance != null)
                UpgradeEffectManager.Instance.AddDamageBonus(def.floatValue);
        }
    }
}
