using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillInventory : MonoBehaviour
{
    /// <summary>没有武将配置时的主动槽缺省值（与 <c>HeroConfig.activeSkillSlotCount</c> 的基础值一致 = 2）。</summary>
    private const int DefaultCapacity = 2;

    /// <summary>
    /// 关卡战斗状态（只读 + 状态事件）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageBattleState _stageBattleState;

    private IStageBattleState StageBattleState => _stageBattleState ??= StageRoles.BattleState;
    public sealed class SkillEntry
    {
        public int id;
        public ActiveSkillDefinition definition;
        public int level;
        public float cooldownRemaining;
        public float cooldownDuration;
    }

    public static ActiveSkillInventory Instance
    {
        get
        {
            if (_instance == null)
                _instance = Object.FindObjectOfType<ActiveSkillInventory>();
            return _instance;
        }
        private set => _instance = value;
    }
    private static ActiveSkillInventory _instance;

    [Header("规则版本（仅开局前切换）")]
    [SerializeField] private ItemRuleVersion _ruleVersion = ItemRuleVersion.V1_LimitedItem;

    [Header("默认技能（开局赠送，调试用）")]
    [Tooltip("开启后：每次进入战斗时，若库存里没有这些技能则自动补上（不覆盖已有等级）。")]
    [SerializeField] private bool _grantDefaultSkills = true;
    [Tooltip("默认赠送的主动技能（V2 规则下生效）。")]
    [SerializeField] private List<ActiveSkillDefinition> _defaultSkills = new List<ActiveSkillDefinition>();

    private readonly List<SkillEntry> _entries = new List<SkillEntry>();
    private int _nextEntryId = 1;
    private ItemRuleVersion _lockedRuleVersion;
    private bool _versionLocked;
    private StageState _lastStageState = StageState.None;

    public System.Action OnSkillsChanged;
    public System.Action OnCooldownsChanged;

    public IReadOnlyList<SkillEntry> Entries => _entries;
    public ItemRuleVersion RuleVersion => _versionLocked ? _lockedRuleVersion : _ruleVersion;
    public bool UsesActiveSkills => RuleVersion == ItemRuleVersion.V2_ActiveSkill;
    /// <summary>
    /// 主动技能槽上限（基础 2，来自 <c>HeroConfig.activeSkillSlotCount</c>；巧思天赋再 +N）。
    /// 老资产 / 漏改资产的字段是 0 → 按 1 兜底，避免一个技能都放不下。
    /// </summary>
    public int Capacity => PlayerState.Instance != null && PlayerState.Instance.heroConfig != null
        ? Mathf.Max(1, PlayerState.Instance.heroConfig.activeSkillSlotCount) + TalentCapabilities.ExtraActiveSlot
        : DefaultCapacity;
    public bool IsFull => _entries.Count >= Capacity;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LockRuleVersion();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (PlayerState.Instance == null) return;

        // 战斗开始的那一刻补一次默认技能（存档恢复路径会在别处再补一次，这里兼颗无快照的新玩家）。
        var currentState = PlayerState.Instance.stageState;
        if (currentState != _lastStageState)
        {
            _lastStageState = currentState;
            if (currentState == StageState.InProgress)
                EnsureDefaultSkills();
        }

        if (PlayerState.Instance.stageState != StageState.InProgress) return;
        if (!UsesActiveSkills) return;

        bool changed = false;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.cooldownRemaining <= 0f) continue;
            entry.cooldownRemaining = Mathf.Max(0f, entry.cooldownRemaining - Time.deltaTime);
            changed = true;
        }

        if (changed)
            OnCooldownsChanged?.Invoke();
    }

    public void LockRuleVersion()
    {
        if (_versionLocked) return;
        _lockedRuleVersion = _ruleVersion;
        _versionLocked = true;
    }

    /// <summary>
    /// 补齐默认赠送的主动技能（V2 规则下）。
    /// 幂等：已持有的不动（不降级、不重置冷却）；缺失的按 lv1 补上。
    /// 快照恢复会先 <c>ResetAll()</c> 清空库存，所以恢复路径需要在本方法之后再调一次。
    /// </summary>
    public void EnsureDefaultSkills()
    {
        if (!_grantDefaultSkills || !UsesActiveSkills) return;
        if (_defaultSkills == null || _defaultSkills.Count == 0) return;

        for (int i = 0; i < _defaultSkills.Count; i++)
        {
            var def = _defaultSkills[i];
            if (def == null) continue;
            if (FindByUpgradeId(def.upgradeId) != null) continue;
            AcquireOrUpgrade(def, out _);
        }
    }

    public bool CanAcquire(ActiveSkillDefinition definition)
    {
        if (!UsesActiveSkills || definition == null) return false;
        var existing = FindByUpgradeId(definition.upgradeId);
        int cap = UpgradeEffectManager.Instance != null
            ? UpgradeEffectManager.Instance.GetEffectiveMaxLevel(definition)
            : Mathf.Max(1, definition.maxLevel);
        if (existing != null)
            return existing.level < cap;
        return !IsFull;
    }

    public bool AcquireOrUpgrade(ActiveSkillDefinition definition, out int newLevel)
    {
        newLevel = 0;
        if (!CanAcquire(definition)) return false;

        var entry = FindByUpgradeId(definition.upgradeId);
        if (entry == null)
        {
            entry = new SkillEntry
            {
                id = _nextEntryId++,
                definition = definition,
                level = 1,
                cooldownRemaining = 0f,
                cooldownDuration = 0f
            };
            _entries.Add(entry);
        }
        else
        {
            entry.level++;
            entry.definition = definition;
        }

        newLevel = entry.level;
        OnSkillsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// **换装：把指定槽位的影之武装换成另一件**（2026-11-24 用户定：影之武装本局可替换）。
    ///
    /// 语义：**槽位不动，内容换掉**，等级从 Lv.1 重新开始（换掉的技能本身不留等级）。
    /// 不做的事：不碰其它槽位、不碰被动、不在这里写存档（解锁资格才是存档的，本局持有不存）。
    /// ⚠ 调用时机应该在**波末三选一**（此时没有技能在施放）；不提供中途打断当前技能的保证。
    /// </summary>
    /// <returns>换成功返回 true；槽位不存在 / 目标已在其它槽 / 同一件 / 规则不是 V2 返回 false。</returns>
    public bool Replace(int entryId, ActiveSkillDefinition definition)
    {
        // 旧 F2 入口：固定 Lv.1。新变体规则走 ReplaceVariant（按可见等级、按种类查重）。
        return ReplaceVariant(entryId, definition, 1);
    }

    /// <summary>按**同款身份**（<see cref="UpgradeDefinition.KindId"/>）查已装备的影之武装。</summary>
    public SkillEntry FindByKind(string kindId)
    {
        if (string.IsNullOrEmpty(kindId)) return null;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.definition != null && entry.definition.KindId == kindId) return entry;
        }
        return null;
    }

    /// <summary>该种类的影之武装是否已在某个槽里（不同品质 / 等级算同款）。</summary>
    public bool IsKindEquipped(string kindId) => FindByKind(kindId) != null;

    /// <summary>
    /// **空槽装备一个变体**（2026-11-25）：等级 = 变体的可见等级，不做 +1 / 不合成。
    /// 已有同款、槽位已满、非 V2 规则都返回 false（调用方应先判定「同款替换 / 满槽换装」）。
    /// </summary>
    public bool TryEquipVariant(ActiveSkillDefinition variant, int level, out SkillEntry entry)
    {
        entry = null;
        if (!UsesActiveSkills || variant == null) return false;
        if (FindByKind(variant.KindId) != null) return false;
        if (IsFull) return false;

        entry = new SkillEntry
        {
            id = _nextEntryId++,
            definition = variant,
            level = Mathf.Max(1, level),
            cooldownRemaining = 0f,
            cooldownDuration = 0f
        };
        _entries.Add(entry);
        SyncPlayerState(null, variant, entry.level);
        OnSkillsChanged?.Invoke();
        OnCooldownsChanged?.Invoke();
        Debug.Log($"[ActiveSkillInventory] 装备 {variant.displayName}（{variant.quality} Lv.{entry.level}，kind={variant.KindId}）");
        return true;
    }

    /// <summary>
    /// **把指定槽换成新变体**（同款换品质 / 等级，或异款替换）：槽位不动，等级取新变体的可见等级。
    /// 同款已在另一个槽时拒绝（不允许同一件影之武装占两格）。
    /// </summary>
    public bool ReplaceVariant(int entryId, ActiveSkillDefinition variant, int level)
    {
        if (!UsesActiveSkills || variant == null) return false;

        var entry = FindById(entryId);
        if (entry == null) return false;

        // 目标同款已经在**别的**槽里 ⇒ 拒绝（否则同一件技能会占两格，冷却与 HUD 都会重复）
        var sameKind = FindByKind(variant.KindId);
        if (sameKind != null && sameKind != entry) return false;

        level = Mathf.Max(1, level);
        var oldDefinition = entry.definition;
        if (oldDefinition == variant && entry.level == level)
            return true;   // 幂等：同一变体同一级，重复点不算失败

        entry.definition = variant;
        entry.level = level;
        entry.cooldownRemaining = 0f;
        entry.cooldownDuration = 0f;

        SyncPlayerState(oldDefinition, variant, level);
        OnSkillsChanged?.Invoke();
        OnCooldownsChanged?.Invoke();
        Debug.Log($"[ActiveSkillInventory] 槽 {entryId} 换装 → {variant.displayName}（{variant.quality} Lv.{level}）");
        return true;
    }

    /// <summary>把**当前持有的同款**换成新变体（同款直接替换原槽）。</summary>
    public bool ReplaceKindVariant(ActiveSkillDefinition variant, int level)
    {
        if (variant == null) return false;
        var entry = FindByKind(variant.KindId);
        return entry != null && ReplaceVariant(entry.id, variant, level);
    }

    /// <summary>移除一个主动槽位（换装流程里用于清掉被替下的那一件），同时清掉玩家持有记录。</summary>
    public bool Remove(int entryId)
    {
        var entry = FindById(entryId);
        if (entry == null) return false;

        _entries.Remove(entry);
        RemoveFromPlayerState(entry.definition);
        OnSkillsChanged?.Invoke();
        OnCooldownsChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 同步玩家持有记录（`PlayerState.acquiredUpgrades` 是 HUD / 快照的读取源）。
    /// 换装时必须把**旧变体**那条换成**新变体**，否则同等级换装会留下旧记录。
    /// </summary>
    private void SyncPlayerState(UpgradeDefinition oldDefinition, UpgradeDefinition newDefinition, int level)
    {
        var ps = PlayerState.Instance;
        if (ps == null || newDefinition == null) return;

        if (oldDefinition == newDefinition)
        {
            for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
            {
                if (ps.acquiredUpgrades[i].definition != newDefinition) continue;
                ps.acquiredUpgrades[i].currentLevel = level;
                return;
            }
        }
        else
        {
            RemoveFromPlayerState(oldDefinition);
        }

        for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
        {
            if (ps.acquiredUpgrades[i].definition != newDefinition) continue;
            ps.acquiredUpgrades[i].currentLevel = level;
            return;
        }
        ps.acquiredUpgrades.Add(new UpgradeAcquired { definition = newDefinition, currentLevel = level });
    }

    private static void RemoveFromPlayerState(UpgradeDefinition definition)
    {
        if (definition == null) return;
        var ps = PlayerState.Instance;
        if (ps == null) return;
        for (int i = ps.acquiredUpgrades.Count - 1; i >= 0; i--)
        {
            var acquired = ps.acquiredUpgrades[i];
            if (acquired != null && acquired.definition == definition)
                ps.acquiredUpgrades.RemoveAt(i);
        }
    }

    public bool TryActivate(int entryId)
    {
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress) return false;
        if (StageBattleState != null && StageBattleState.IsRouteRewardWaiting) return false;
        if (!UsesActiveSkills) return false;
        // 时停（timeScale=0）时默认**不能**放技能；教学「用技能打断」那一拍例外
        //（那一拍就是要玩家在时停里点技能，见 GameplayInputGate.AllowSkillActivationWhileFrozen）。
        if (Time.timeScale <= 0f && !GameplayInputGate.AllowSkillActivationWhileFrozen) return false;
        // 台词在播时会挡住技能释放（对话层把交互锁了）。
        // 教学「用技能打断」那一拍**就是要玩家在冻结里点技能**，
        // 而那一拍的说明台词是**同时**弹出来的 → 不能让它把技能槽挡掉
        // （否则玩家点技能槽没反应 → 这一拍只能干等 30 秒超时放行）。
        if (!GameplayInputGate.AllowSkillActivationWhileBlocked &&
            CombatPresenters.Dialogue != null && CombatPresenters.Dialogue.IsInteractionBlocked) return false;

        var entry = FindById(entryId);
        if (entry == null)
        {
            Debug.LogWarning($"[ActiveSkillInventory] TryActivate failed: entry {entryId} not found");
            return false;
        }
        if (entry.cooldownRemaining > 0f)
        {
            Debug.Log($"[ActiveSkillInventory] TryActivate failed: {entry.definition?.displayName} on cooldown ({entry.cooldownRemaining:F1}s remaining)");
            return false;
        }
        if (ActiveSkillRunner.Instance == null)
        {
            Debug.LogWarning("[ActiveSkillInventory] TryActivate failed: ActiveSkillRunner.Instance null");
            return false;
        }
        if (!ActiveSkillRunner.Instance.TryActivate(entry.definition, entry.level))
        {
            Debug.LogWarning($"[ActiveSkillInventory] TryActivate failed: ActiveSkillRunner.TryActivate returned false for {entry.definition?.displayName} Lv.{entry.level}");
            return false;
        }

        float cd = entry.definition.GetCooldown(entry.level);
        float reduction = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetActiveSkillCDReduction() : 0f;
        entry.cooldownDuration = cd * (1f - Mathf.Min(reduction, 1f));
        entry.cooldownRemaining = entry.cooldownDuration;
        OnCooldownsChanged?.Invoke();
        Debug.Log($"[ActiveSkillInventory] Activated {entry.definition?.displayName} Lv.{entry.level}, cooldown={entry.cooldownDuration}s");
        return true;
    }

    public SkillEntry GetEntry(int entryId) => FindById(entryId);

    public SkillEntry GetEntry(string upgradeId) => FindByUpgradeId(upgradeId);

    public int GetLevel(string upgradeId)
    {
        var entry = FindByUpgradeId(upgradeId);
        return entry != null ? entry.level : 0;
    }

    public bool HasSkill(string upgradeId) => FindByUpgradeId(upgradeId) != null;

    public void ResetCooldowns()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null || entry.definition == null) continue;
            entry.cooldownRemaining = 0f;
            entry.cooldownDuration = 0f;
        }
        OnCooldownsChanged?.Invoke();
    }

    public void ResetAll()
    {
        _entries.Clear();
        _nextEntryId = 1;
        ActiveSkillRunner.Instance?.ResetAll();
        OnSkillsChanged?.Invoke();
        OnCooldownsChanged?.Invoke();
    }

    private SkillEntry FindById(int entryId)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].id == entryId)
                return _entries[i];
        return null;
    }

    private SkillEntry FindByUpgradeId(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].definition != null && _entries[i].definition.upgradeId == upgradeId)
                return _entries[i];
        return null;
    }
}
