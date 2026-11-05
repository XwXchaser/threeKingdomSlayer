using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// V3 正面面板上的横向道具栏 — 仅 ColumnB（可消耗道具）。
/// 与 BuffDisplayPanel 共享 ItemInventory 数据源，架构镜像复制。
/// </summary>
public class FrontItemBar : MonoBehaviour
{
    /// <summary>单例（教学关需要定位技能槽的屏幕坐标）。</summary>
    public static FrontItemBar Instance { get; private set; }
    [SerializeField] private List<BuffIcon> _itemSlots = new List<BuffIcon>();
    [SerializeField] private Sprite _skillFrame;
    [Tooltip("相邻道具槽位的 Canvas 设计单位间距")]
    [SerializeField, Min(0f)] private float _slotSpacing = 10f;

    private readonly Dictionary<int, BuffIcon> _itemIcons = new Dictionary<int, BuffIcon>();
    private readonly Dictionary<int, BuffIcon> _itemSlotAssignments = new Dictionary<int, BuffIcon>();
    private readonly Dictionary<BuffIcon, int> _iconEntryIds = new Dictionary<BuffIcon, int>();
    private readonly Dictionary<int, bool> _cooldownStates = new Dictionary<int, bool>();

    private CanvasGroup _canvasGroup;
    private bool _qteDimmed;
    private QTEController _qteController;
    private System.Action _onQteTriggered;
    private System.Action _onQteFinished;
    private float _appliedSlotSpacing = float.NaN;

    public bool HasAvailableSlot => _itemSlots.Exists(s => s != null && !s.gameObject.activeSelf);

    /// <summary>
    /// **教学用**：取某个主动技能槽的屏幕坐标（<paramref name="upgradeId"/> 留空 = 第一个主动技能槽）。
    ///
    /// 教学「用技能打断霸体」这一拍要把提示手落在技能槽上 → 需要知道槽位在屏幕上的位置。
    /// 返回 false = 现在没有可用的主动技能槽（还没拿到技能 / 槽位没刷新）。
    /// </summary>
    public bool TryGetActiveSkillSlotScreenPoint(string upgradeId, out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var inventory = ActiveSkillInventory.Instance;
        if (inventory == null || !inventory.UsesActiveSkills) return false;

        var entries = inventory.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.definition == null) continue;
            if (!string.IsNullOrEmpty(upgradeId) && entry.definition.upgradeId != upgradeId) continue;
            if (!_itemIcons.TryGetValue(entry.id, out var slot) || slot == null) continue;

            var rect = slot.GetComponent<RectTransform>();
            if (rect == null) return false;
            var canvas = rect.GetComponentInParent<Canvas>();
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            return true;
        }
        return false;
    }

    /// <summary>
    /// 按**槽位序号**取主动技能槽的屏幕坐标（**不依赖 _itemIcons 注册表**）。
    ///
    /// 为什么要有这个（2026-11-02）：教学「用技能打断」那一拍要指技能槽，
    /// 旧写法用 <see cref="TryGetActiveSkillSlotScreenPoint"/>（靠 _itemIcons 里的 entry.id 反查）——
    /// 一旦图标还没刷进注册表（玩家刚拿到技能、FrontItemBar 还没重建槽位），
    /// 查找就永远失败 → 那一拍在「等技能槽」上卡住 → 教学不发生、玩家全程挨打。
    /// 这个接口直接按槽位 GameObject 取位置，图标没刷新也能指对地方。
    /// </summary>
    public bool TryGetActiveSkillSlotScreenPointByIndex(int slotIndex, out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        if (slotIndex < 0 || slotIndex >= _itemSlots.Count) return false;
        var slot = _itemSlots[slotIndex];
        if (slot == null || !slot.gameObject.activeInHierarchy) return false;
        var rect = slot.GetComponent<RectTransform>();
        if (rect == null) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
        return true;
    }

    /// <summary>
    /// 调试：当前主动技能槽的注册情况（给教学拍排查「等不到技能槽」用）。
    /// </summary>
    public string DebugActiveSkillSlotState()
    {
        var inv = ActiveSkillInventory.Instance;
        var sb = new System.Text.StringBuilder();
        sb.Append("entries=" + (inv != null ? inv.Entries.Count : -1)
            + " 图标已注册=" + _itemIcons.Count
            + " 槽位数=" + _itemSlots.Count
            + " capacity=" + (inv != null ? inv.Capacity : -1)
            + " 用主动技能规则=" + (inv != null && inv.UsesActiveSkills));
        if (inv != null)
        {
            for (int i = 0; i < inv.Entries.Count; i++)
            {
                var e = inv.Entries[i];
                sb.Append(" | #" + i + " "
                    + (e != null && e.definition != null ? e.definition.upgradeId : "?")
                    + "(id=" + (e != null ? e.id : -1)
                    + ",已注册=" + (e != null && _itemIcons.ContainsKey(e.id)) + ")");
            }
        }
        return sb.ToString();
    }

    private void Awake()
    {
        // ⚠️ 场景里可能有**两个** FrontItemBar（Battle HUD 的那个才配了槽位，根节点上还有一个空的），
        //    而 Instance 是“先 Awake 的赢” → 之前拿到的是那个空的 → 技能槽查找全查空表
        //    → 「用技能打断」那一拍找不到技能槽 → 没有手势指引（2026-11-02 用户反馈）。
        //    现在：**有槽位的那个优先当 Instance**。
        if (Instance == null || (Instance._itemSlots.Count == 0 && _itemSlots.Count > 0))
            Instance = this;
    }

    /// <summary>
    /// 找到**真正带技能槽**的那个 FrontItemBar（场景里有多个时用这个，别只认 Instance）。
    /// </summary>
    public static FrontItemBar ResolveForSkills()
    {
        var bars = FindObjectsOfType<FrontItemBar>(true);
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] != null && bars[i]._itemSlots.Count > 0) return bars[i];
        }
        return Instance;
    }

    /// <summary>
    /// 按 upgradeId 找到技能槽的图标（找不到返回 null）。
    /// </summary>
    public BuffIcon FindActiveSkillSlotIcon(string upgradeId)
    {
        var inventory = ActiveSkillInventory.Instance;
        if (inventory == null || !inventory.UsesActiveSkills) return null;
        var entries = inventory.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || entry.definition == null) continue;
            if (!string.IsNullOrEmpty(upgradeId) && entry.definition.upgradeId != upgradeId) continue;
            if (_itemIcons.TryGetValue(entry.id, out var slot) && slot != null) return slot;
        }
        return null;
    }

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
        if (Instance == this) Instance = null;
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

    /// <summary>重新排布槽位（按获得顺序左对齐）。改造工具会在改完尺寸后主动调一次。</summary>
    public void LayoutSlots()
    {
        int count = _itemSlots.Count;
        if (count == 0) return;

        // 用 sizeDelta 而不是 rect.width：rect 要等布局跑完才有值，Edit Mode / 首帧可能是 0
        float slotWidth = _itemSlots[0] != null ? _itemSlots[0].GetComponent<RectTransform>().sizeDelta.x : 0f;
        if (slotWidth <= 0f) slotWidth = 104f;
        // 左对齐：从最左槽开始向右排 —— “新获得就从左往右排序出现”。
        // 槽位是 FrontItemBar 本地坐标（该物体 pivot 在左下角），所以 x=0 即左边缘。
        float startX = slotWidth * 0.5f;
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
        // 初始全部隐藏 —— “新获得就出现”，空槽不占位。
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;
            slot.OnClicked -= OnItemIconClicked;
            slot.ResetSlot();
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
        _itemSlotAssignments.Clear();

        int capacity = ItemInventory.Instance != null ? ItemInventory.Instance.Capacity : 0;
        var entries = ItemInventory.Instance != null ? ItemInventory.Instance.Entries : null;

        // 先全部清空（未占用的槽隐藏）
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            if (slot == null) continue;
            slot.OnClicked -= OnItemIconClicked;
            slot.ResetSlot();
        }

        if (entries == null) return;

        // 按**获得顺序紧凑排列**：entries[i] → 第 i 个槽（不再锁死槽位）
        int used = 0;
        for (int i = 0; i < entries.Count && used < capacity && used < _itemSlots.Count; i++)
        {
            var entry = entries[i];
            var slot = _itemSlots[used];
            if (slot == null) continue;
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
            used++;
        }
        LayoutSlots();
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
            slot.ResetSlot();
        }

        if (inventory == null) return;
        var entries = inventory.Entries;
        int used = 0;
        for (int i = 0; i < entries.Count && used < capacity && used < _itemSlots.Count; i++)
        {
            var entry = entries[i];
            var slot = _itemSlots[used];
            if (slot == null || entry.definition == null) continue;
            slot.Setup(entry.definition.icon, entry.definition.upgradeId, UpgradeCategory.ActiveSkill, entry.definition.upgradeId);
            slot.SetFrame(_skillFrame);
            slot.ClearBadgeNumber();
            slot.OnClicked += OnItemIconClicked;
            slot.gameObject.SetActive(true);
            _itemIcons[entry.id] = slot;
            _iconEntryIds[slot] = entry.id;
            used++;
        }
        RefreshActiveSkillCooldowns();
        LayoutSlots();
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

            bool hadPrev = _cooldownStates.TryGetValue(pair.Key, out bool wasCoolingDown);
            if (!hadPrev || wasCoolingDown != coolingDown)
            {
                _cooldownStates[pair.Key] = coolingDown;
                pair.Value.SetActiveButtonPressed(coolingDown);
            }
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
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsInteractionBlocked)
        {
            // 教学「用技能打断」那一拍：冻结着等玩家点技能槽 →
            //   那一拍的说明台词是同时弹出来的 → 不能让它把点击吞掉
            //   （见 ActiveSkillInventory.TryActivate 的同一处说明）。
            bool skillBeat = ActiveSkillInventory.Instance != null
                             && ActiveSkillInventory.Instance.UsesActiveSkills
                             && GameplayInputGate.AllowSkillActivationWhileBlocked;
            if (!skillBeat) return;
        }
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
