using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V3 正面面板上的横向道具栏 — 仅 ColumnB（可消耗道具）。
/// 与 BuffDisplayPanel 共享 ItemInventory 数据源，架构镜像复制。
/// </summary>
public class FrontItemBar : MonoBehaviour
{
    [SerializeField] private List<BuffIcon> _itemSlots = new List<BuffIcon>();
    [SerializeField] private Sprite _skillFrame;
    [Tooltip("相邻道具槽位的 Canvas 设计单位间距")]
    [SerializeField, Min(0f)] private float _slotSpacing = 10f;

    private readonly Dictionary<int, BuffIcon> _itemIcons = new Dictionary<int, BuffIcon>();
    private readonly Dictionary<int, BuffIcon> _itemSlotAssignments = new Dictionary<int, BuffIcon>();
    private readonly Dictionary<BuffIcon, int> _iconEntryIds = new Dictionary<BuffIcon, int>();

    private CanvasGroup _canvasGroup;
    private bool _qteDimmed;
    private QTEController _qteController;
    private System.Action _onQteTriggered;
    private System.Action _onQteFinished;
    private float _appliedSlotSpacing = float.NaN;

    public bool HasAvailableSlot => _itemSlots.Exists(s => s != null && !s.gameObject.activeSelf);

    private void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        foreach (var slot in _itemSlots)
            if (slot != null) slot.gameObject.SetActive(false);

        InitializeItemSlots();

        if (ItemInventory.Instance != null)
            ItemInventory.Instance.OnInventoryChanged += RefreshItemSlots;
        if (ActiveSkillInventory.Instance != null)
        {
            ActiveSkillInventory.Instance.OnSkillsChanged += RefreshItemSlots;
            ActiveSkillInventory.Instance.OnCooldownsChanged += RefreshActiveSkillCooldowns;
        }
        RefreshItemSlots();

        if (ActiveSkillInventory.Instance == null || !ActiveSkillInventory.Instance.UsesActiveSkills)
            TryAssignPotionTarget();
    }

    private void Update()
    {
        if (!Mathf.Approximately(_appliedSlotSpacing, _slotSpacing))
            LayoutSlots();

        if (_qteController != null) return;
        _qteController = Object.FindObjectOfType<QTEController>();
        if (_qteController == null) return;

        _onQteTriggered = () => SetQTEInputLocked(true);
        _onQteFinished = () => SetQTEInputLocked(false);
        _qteController.OnQTETriggered += _onQteTriggered;
        _qteController.OnQTEAttackFinished += _onQteFinished;
    }

    private void OnDestroy()
    {
        if (ItemInventory.Instance != null)
            ItemInventory.Instance.OnInventoryChanged -= RefreshItemSlots;
        if (ActiveSkillInventory.Instance != null)
        {
            ActiveSkillInventory.Instance.OnSkillsChanged -= RefreshItemSlots;
            ActiveSkillInventory.Instance.OnCooldownsChanged -= RefreshActiveSkillCooldowns;
        }
        if (_qteController != null)
        {
            if (_onQteTriggered != null)
                _qteController.OnQTETriggered -= _onQteTriggered;
            if (_onQteFinished != null)
                _qteController.OnQTEAttackFinished -= _onQteFinished;
        }
    }

    private void LayoutSlots()
    {
        int count = _itemSlots.Count;
        if (count == 0) return;

        float slotWidth = _itemSlots[0] != null ? _itemSlots[0].GetComponent<RectTransform>().rect.width : 0f;
        float totalWidth = count * slotWidth + (count - 1) * _slotSpacing;
        float startX = -totalWidth * 0.5f + slotWidth * 0.5f;
        for (int i = 0; i < count; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;
            var position = slot.GetComponent<RectTransform>().anchoredPosition;
            position.x = startX + i * (slotWidth + _slotSpacing);
            slot.GetComponent<RectTransform>().anchoredPosition = position;
        }
        _appliedSlotSpacing = _slotSpacing;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            LayoutSlots();
    }

    private void InitializeItemSlots()
    {
        LayoutSlots();
        int capacity = ItemInventory.Instance != null ? ItemInventory.Instance.Capacity : 2;
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;
            slot.OnClicked -= OnItemIconClicked;
            if (i < capacity)
            {
                slot.ShowEmpty(_skillFrame);
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.ResetSlot();
            }
        }
    }

    private void RefreshItemSlots()
    {
        if (ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.UsesActiveSkills)
        {
            RefreshActiveSkillSlots();
            return;
        }

        _itemIcons.Clear();
        _iconEntryIds.Clear();
        int capacity = ItemInventory.Instance != null ? ItemInventory.Instance.Capacity : 0;
        var entries = ItemInventory.Instance != null ? ItemInventory.Instance.Entries : null;

        var activeEntryIds = new HashSet<int>();
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
                activeEntryIds.Add(entries[i].id);
        }

        var staleAssignments = new List<int>();
        foreach (var pair in _itemSlotAssignments)
        {
            if (!activeEntryIds.Contains(pair.Key))
                staleAssignments.Add(pair.Key);
        }
        foreach (int entryId in staleAssignments)
            _itemSlotAssignments.Remove(entryId);

        var occupiedSlots = new HashSet<BuffIcon>();
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;
            slot.OnClicked -= OnItemIconClicked;
            slot.ShowEmpty(_skillFrame);
            if (i >= capacity)
                slot.ResetSlot();
        }

        if (entries == null) return;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!_itemSlotAssignments.TryGetValue(entry.id, out var slot) || slot == null || !_itemSlots.Contains(slot))
            {
                slot = null;
                for (int j = 0; j < capacity && j < _itemSlots.Count; j++)
                {
                    var candidate = _itemSlots[j];
                    if (candidate != null && !occupiedSlots.Contains(candidate))
                    {
                        slot = candidate;
                        break;
                    }
                }
                if (slot == null) continue;
                _itemSlotAssignments[entry.id] = slot;
            }

            occupiedSlots.Add(slot);
            var def = entry.definition;
            slot.Setup(def != null ? def.icon : null, def != null ? def.upgradeId : null,
                UpgradeCategory.Item, entry.GestureId);
            slot.SetFrame(_skillFrame);
            if (entry.remainingUses > 1)
                slot.SetBadgeNumber(entry.remainingUses);
            else
                slot.ClearBadgeNumber();
            slot.OnClicked += OnItemIconClicked;
            slot.gameObject.SetActive(true);
            _itemIcons[entry.id] = slot;
            _iconEntryIds[slot] = entry.id;
        }
    }

    private void RefreshActiveSkillSlots()
    {
        _itemIcons.Clear();
        _iconEntryIds.Clear();
        _itemSlotAssignments.Clear();

        var inventory = ActiveSkillInventory.Instance;
        int capacity = inventory != null ? inventory.Capacity : 0;
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;
            slot.OnClicked -= OnItemIconClicked;
            if (i < capacity)
            {
                slot.ShowEmpty(_skillFrame);
                slot.gameObject.SetActive(true);
            }
            else
            {
                slot.ResetSlot();
            }
        }

        if (inventory == null) return;
        var entries = inventory.Entries;
        for (int i = 0; i < entries.Count && i < capacity && i < _itemSlots.Count; i++)
        {
            var entry = entries[i];
            var slot = _itemSlots[i];
            if (slot == null || entry.definition == null) continue;
            slot.Setup(entry.definition.icon, entry.definition.upgradeId, UpgradeCategory.ActiveSkill, entry.definition.upgradeId);
            slot.SetFrame(_skillFrame);
            slot.ClearBadgeNumber();
            slot.OnClicked += OnItemIconClicked;
            slot.gameObject.SetActive(true);
            _itemIcons[entry.id] = slot;
            _iconEntryIds[slot] = entry.id;
        }
        RefreshActiveSkillCooldowns();
    }

    private void RefreshActiveSkillCooldowns()
    {
        var inventory = ActiveSkillInventory.Instance;
        if (inventory == null || !inventory.UsesActiveSkills) return;
        foreach (var pair in _itemIcons)
        {
            var entry = inventory.GetEntry(pair.Key);
            if (entry == null) continue;
            bool coolingDown = entry.cooldownRemaining > 0f;
            float fill = entry.cooldownDuration > 0f
                ? 1f - Mathf.Clamp01(entry.cooldownRemaining / entry.cooldownDuration)
                : 0f;
            pair.Value.SetCooldown(fill, null, coolingDown);
            pair.Value.SetInteractable(!coolingDown && !_qteDimmed);
            if (coolingDown)
                pair.Value.SetCountdownNumber(Mathf.CeilToInt(entry.cooldownRemaining));
            else
                pair.Value.ClearTopRightNumber();
        }
    }

    public void SetQTEInputLocked(bool locked)
    {
        _qteDimmed = locked;
        if (_canvasGroup == null) return;
        _canvasGroup.alpha = locked ? 0.4f : 1f;
        _canvasGroup.blocksRaycasts = !locked;
        _canvasGroup.interactable = !locked;
        RefreshActiveSkillCooldowns();
    }

    public void TryAssignPotionTarget()
    {
        if (HealthPotionManager.Instance == null || !HealthPotionManager.Instance.IsEnabledForCurrentRules) return;
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            if (slot != null && slot.gameObject.activeSelf)
            {
                HealthPotionManager.Instance.targetSlot = slot.GetComponent<RectTransform>();
                return;
            }
        }
    }

    private void OnItemIconClicked(BuffIcon icon)
    {
        if (_qteDimmed) return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractionBlocked) return;
        if (!_iconEntryIds.TryGetValue(icon, out int entryId)) return;

        if (ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.UsesActiveSkills)
        {
            ActiveSkillInventory.Instance.TryActivate(entryId);
            return;
        }

        var entry = ItemInventory.Instance.GetEntry(entryId);
        if (entry == null) return;
        string gestureId = entry.GestureId;
        var def = entry.definition;
        if (entry.isPotion)
        {
            HealthPotionManager.Instance?.TryUsePotion();
            return;
        }
        if (def == null) return;

        if (gestureId == "item_cyclone")
        {
            if (CycloneItemController.Instance != null &&
                CycloneItemController.Instance.TryActivate(def))
                ItemInventory.Instance.TryConsumeEntry(entryId);
            return;
        }

        if (!ItemInventory.Instance.TryConsumeEntry(entryId)) return;

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
        else if (gestureId == "wave")
        {
            if (WaveManager.Instance != null)
                WaveManager.Instance.TriggerWave(def.intValue, def.secondaryIntValue, Mathf.RoundToInt(def.floatValue));
        }
        else if (gestureId == "arrow_rain" || gestureId == "fire_snake" || gestureId == "phantom_weapon_item")
        {
            if (ItemEffectRunner.Instance != null)
                ItemEffectRunner.Instance.TryActivate(def);
        }
    }
}
