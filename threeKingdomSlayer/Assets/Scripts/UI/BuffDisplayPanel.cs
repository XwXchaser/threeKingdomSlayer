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
    // entryId → slot（ColumnB）
    private readonly Dictionary<int, BuffIcon> _itemIcons = new Dictionary<int, BuffIcon>();
    private readonly Dictionary<int, BuffIcon> _itemSlotAssignments = new Dictionary<int, BuffIcon>();
    private readonly Dictionary<BuffIcon, int> _iconEntryIds = new Dictionary<BuffIcon, int>();

    private CanvasGroup _canvasGroup;

    private void Start()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        // 血包槽位始终可见
        _canvasGroup.alpha = 1f;
        _canvasGroup.blocksRaycasts = true;

        foreach (var slot in _columnASlots)
            if (slot != null) slot.gameObject.SetActive(false);
        foreach (var slot in _columnBSlots)
            if (slot != null) slot.gameObject.SetActive(false);

        InitializeItemSlots();

        if (UpgradeEffectManager.Instance != null)
            UpgradeEffectManager.Instance.OnUpgradeApplied += OnUpgradeApplied;
        if (ItemInventory.Instance != null)
        {
            ItemInventory.Instance.OnInventoryChanged += RefreshItemSlots;
            RefreshItemSlots();
        }
        if (PassiveTriggerModule.Instance != null)
            PassiveTriggerModule.Instance.OnPassiveRegistered += OnPassiveRegistered;
    }

    private void OnDestroy()
    {
        if (UpgradeEffectManager.Instance != null)
            UpgradeEffectManager.Instance.OnUpgradeApplied -= OnUpgradeApplied;
        if (ItemInventory.Instance != null)
            ItemInventory.Instance.OnInventoryChanged -= RefreshItemSlots;
        if (PassiveTriggerModule.Instance != null)
            PassiveTriggerModule.Instance.OnPassiveRegistered -= OnPassiveRegistered;
    }

    private void InitializeItemSlots()
    {
        int capacity = ItemInventory.Instance != null ? ItemInventory.Instance.Capacity : 2;
        for (int i = 0; i < _columnBSlots.Count; i++)
        {
            var slot = _columnBSlots[i];
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
        _itemIcons.Clear();
        _iconEntryIds.Clear();
        int capacity = ItemInventory.Instance != null ? ItemInventory.Instance.Capacity : 0;
        var entries = ItemInventory.Instance != null ? ItemInventory.Instance.Entries : null;
        bool potionTargetAssigned = false;
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
        for (int i = 0; i < _columnBSlots.Count; i++)
        {
            var slot = _columnBSlots[i];
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
            if (!_itemSlotAssignments.TryGetValue(entry.id, out var slot) || slot == null || !_columnBSlots.Contains(slot))
            {
                slot = null;
                for (int j = 0; j < capacity && j < _columnBSlots.Count; j++)
                {
                    var candidate = _columnBSlots[j];
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

            if (entry.isPotion && HealthPotionManager.Instance != null)
            {
                HealthPotionManager.Instance.targetSlot = slot.GetComponent<RectTransform>();
                potionTargetAssigned = true;
            }
        }

        if (!potionTargetAssigned && HealthPotionManager.Instance != null)
        {
            for (int i = 0; i < capacity && i < _columnBSlots.Count; i++)
            {
                var slot = _columnBSlots[i];
                if (slot != null && !occupiedSlots.Contains(slot))
                {
                    HealthPotionManager.Instance.targetSlot = slot.GetComponent<RectTransform>();
                    break;
                }
            }
        }
    }

    private BuffIcon ClaimColumnASlot()
    {
        foreach (var slot in _columnASlots)
            if (slot != null && !slot.gameObject.activeSelf)
                return slot;
        Debug.LogWarning("[BuffDisplayPanel] ColumnA 槽位已满");
        return null;
    }

    // ── 事件回调 ──

    private void OnUpgradeApplied(UpgradeDefinition def, int newLevel)
    {
        if (def.category == UpgradeCategory.Item)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            RefreshItemSlots();
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
            RefreshNumericValue(icon, def, UpgradeEffectManager.Instance);
        }
    }

    private void RefreshNumericValue(BuffIcon icon, UpgradeDefinition definition, UpgradeEffectManager uem)
    {
        if (icon == null || definition == null || uem == null) return;

        switch (definition.effectType)
        {
            case "attack_speed":
                icon.SetPercentNumber(Mathf.RoundToInt(uem.GetAttackSpeedBonusPercent()));
                break;
            case "damage_multiplier":
                icon.SetPercentNumber(Mathf.RoundToInt((uem.GetDamageMultiplier() - 1f) * 100f));
                break;
            case "exp_multiplier":
                icon.SetPercentNumber(Mathf.RoundToInt((uem.GetExpMultiplier() - 1f) * 100f));
                break;
            case "item_drop_rate":
                icon.SetPercentNumber(Mathf.RoundToInt(uem.GetItemDropRateBonus() * 100f));
                break;
            case "item_damage_bonus":
                icon.SetPercentNumber(Mathf.RoundToInt(uem.GetItemDamageBonus() * 100f));
                break;
            case "charge_damage_reduction":
                icon.SetPercentNumber(Mathf.RoundToInt(uem.GetChargeDamageReduction() * 100f));
                break;
            case "stab_range_boost":
                icon.SetTopRightNumber(uem.GetStabRangeBonus());
                break;
            case "sweep_range_boost":
                icon.SetTopRightNumber(uem.GetSweepRangeBonus());
                break;
            case "push_wave":
                icon.SetTopRightNumber(uem.GetPushWaveDistance());
                break;
            case "convergence_wave":
                icon.SetTopRightNumber(uem.GetConvergenceStep());
                break;
            case "charge_reflect_shield":
                icon.ClearTopRightNumber();
                break;
            default:
                icon.ClearTopRightNumber();
                break;
        }
    }

    private void OnPassiveRegistered(string upgradeId, int threshold)
    {
        if (_upgradeIcons.TryGetValue(upgradeId, out var icon) && threshold > 0)
        {
            icon.SetCooldown(0f, null, true);
            icon.SetTopRightNumber(threshold);
        }
    }

    private Sprite GetLevelFrame(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, _levelFrames.Length - 1);
        return _levelFrames[index];
    }

    // ── 道具点击 ──

        // ── 计时被动冷却更新 ──

    private void Update()
    {
        var timedModule = TimedPassiveModule.Instance;
        if (timedModule != null)
        {
            foreach (var upgradeId in timedModule.RegisteredUpgradeIds)
            {
                if (!_upgradeIcons.TryGetValue(upgradeId, out var icon)) continue;

                float timer = timedModule.GetTimer(upgradeId);
                float interval = timedModule.GetInterval(upgradeId);
                if (interval <= 0f) continue;

                float fill = 1f - (timer / interval);
                icon.SetCooldown(fill, null, true);
                icon.SetCountdownNumber(Mathf.CeilToInt(timer));
            }
        }

        var cycloneItem = CycloneItemController.Instance;
        if (cycloneItem != null)
        {
            foreach (var pair in _itemIcons)
            {
                var entry = ItemInventory.Instance != null ? ItemInventory.Instance.GetEntry(pair.Key) : null;
                if (entry == null || entry.GestureId != "item_cyclone") continue;
                var cycloneItemIcon = pair.Value;
                bool coolingDown = cycloneItem.IsOnCooldown;
                cycloneItemIcon.SetCooldown(cycloneItem.CooldownFill, null, coolingDown);
                cycloneItemIcon.SetInteractable(!coolingDown);
                if (coolingDown) cycloneItemIcon.SetCountdownNumber(Mathf.CeilToInt(cycloneItem.CooldownRemaining));
                else cycloneItemIcon.ClearTopRightNumber();
            }
        }

        // 反伤盾：显示护盾值 / CD 倒计时
        var uem = UpgradeEffectManager.Instance;
        if (uem != null && _upgradeIcons.TryGetValue("charge_reflect_shield", out var shieldIcon))
        {
            var (fill, remaining) = uem.GetReflectShieldCooldown();
            if (fill >= 0f)
            {
                if (uem.GetHasReflectShield())
                {
                    // 护盾存在：清除右上角数字（血量栏已显示护盾值）
                    shieldIcon.SetCooldown(0f, null, false);
                    shieldIcon.ClearTopRightNumber();
                }
                else
                {
                    // CD 中：显示填充环和倒计时
                    shieldIcon.SetCooldown(fill, null, remaining > 0f);
                    if (remaining > 0f)
                        shieldIcon.SetCountdownNumber(Mathf.CeilToInt(remaining));
                    else
                        shieldIcon.ClearTopRightNumber();
                }
            }
        }

        // 受击冲击波：右上角实时显示本次蓄力累计增伤
        if (uem != null && _upgradeIcons.TryGetValue("charge_hit_shockwave", out var hitWaveIcon))
        {
            int percent = Mathf.RoundToInt(uem.GetChargeHitShockwaveBonusPercent() * 100f);
            hitWaveIcon.SetPercentNumber(percent);
        }

        // 攻击计数被动：显示距下一次触发的剩余攻击次数。
        var ptm = PassiveTriggerModule.Instance;
        if (ptm != null)
        {
            foreach (var upgradeId in ptm.RegisteredUpgradeIds)
            {
                if (!_upgradeIcons.TryGetValue(upgradeId, out var icon)) continue;
                int threshold = ptm.GetThreshold(upgradeId);
                if (threshold <= 0) continue;
                int current = ptm.GetCurrentCount(upgradeId);
                icon.SetCooldown((float)current / threshold, null, true);
                icon.SetTopRightNumber(Mathf.Max(0, threshold - current));
            }
        }
    }

    private void OnItemIconClicked(BuffIcon icon)
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractionBlocked) return;
        var qte = FindObjectOfType<QTEController>();
        if (qte != null && qte.IsStrictInputActive) return;
        if (!_iconEntryIds.TryGetValue(icon, out int entryId)) return;
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
