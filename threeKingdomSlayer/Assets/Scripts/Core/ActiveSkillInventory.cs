using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillInventory : MonoBehaviour
{

    /// <summary>
    /// 关卡战斗状态（只读 + 状态事件）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageBattleState _stageBattleState;

    private IStageBattleState StageBattleState => _stageBattleState ??= StageController.Instance;
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
    public int Capacity => PlayerState.Instance != null && PlayerState.Instance.heroConfig != null
        ? Mathf.Max(0, PlayerState.Instance.heroConfig.itemSlotCount)
        : 2;
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
        if (existing != null)
            return existing.level < definition.maxLevel;
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

    public bool TryActivate(int entryId)
    {
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress) return false;
        if (StageBattleState != null && StageBattleState.IsRouteRewardWaiting) return false;
        if (!UsesActiveSkills) return false;
        if (Time.timeScale <= 0f) return false;
        if (CombatPresenters.Dialogue != null && CombatPresenters.Dialogue.IsInteractionBlocked) return false;

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
