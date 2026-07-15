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

        for (int i = 0; i < _columnBSlots.Count; i++)
        {
            var slot = _columnBSlots[i];
            if (slot == null) continue;
            slot.OnClicked -= OnItemIconClicked;
            if (i >= capacity)
            {
                slot.ResetSlot();
                continue;
            }
            if (entries == null || i >= entries.Count)
            {
                slot.ShowEmpty(_skillFrame);
                if (!potionTargetAssigned && HealthPotionManager.Instance != null)
                {
                    HealthPotionManager.Instance.targetSlot = slot.GetComponent<RectTransform>();
                    potionTargetAssigned = true;
                }
                continue;
            }

            var entry = entries[i];
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

            if (def.category != UpgradeCategory.AttackPassive && def.category != UpgradeCategory.TimedPassive)
            {
                var uem = UpgradeEffectManager.Instance;
                // 攻速：右上角显示累计百分比
                if (def.effectType == "attack_speed" && uem != null)
                {
                    int pct = Mathf.RoundToInt(uem.GetAttackSpeedBonusPercent());
                    icon.SetPercentNumber(pct);
                }
                // 蓄力减伤：右上角显示百分比
                else if (def.effectType == "charge_damage_reduction")
                {
                    int pct = Mathf.RoundToInt(uem.GetChargeDamageReduction() * 100f);
                    icon.SetPercentNumber(pct);
                }
                // 反伤盾：右上角显示护盾值
                else if (def.effectType == "charge_reflect_shield")
                {
                    // 初始不显示数字，由 Update 动态更新
                }
            }
        }
    }

    private void OnPassiveRegistered(string upgradeId, int threshold)
    {
        if (_upgradeIcons.TryGetValue(upgradeId, out var icon))
        {
            // 不再显示等级和攻击计数
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

        // 箭矢齐射：显示触发剩余攻击次数
        var ptm = PassiveTriggerModule.Instance;
        if (ptm != null && _upgradeIcons.TryGetValue("arrow_volley", out var volleyIcon))
        {
            int threshold = ptm.GetThreshold("arrow_volley");
            int current = ptm.GetCurrentCount("arrow_volley");
            if (threshold > 0)
            {
                int remaining = threshold - current;
                float fill = (float)current / threshold;
                volleyIcon.SetCooldown(fill, null, true);
                volleyIcon.SetTopRightNumber(remaining);
            }
        }
    }

    private void OnItemIconClicked(BuffIcon icon)
    {
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
    }
}
