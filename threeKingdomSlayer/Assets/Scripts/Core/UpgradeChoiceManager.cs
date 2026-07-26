using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 三选一升级选择管理器 - 单例
///
/// 监听 PlayerState.OnLevelUp，管理暂停/弹窗/连续选择的完整流程：
///   1. 经验达标 → OnLevelUp 触发
///   2. 暂停游戏 (Time.timeScale=0)
///   3. 从 UpgradePoolConfig 随机抽取 choiceCount 个选项
///   4. 通知 UI 显示（OnChoicesReady）
///   5. 玩家选择后 → 应用效果 → 检查是否还有待处理的升级
///   6. 有则刷新弹窗（仍暂停），无则恢复游戏
/// </summary>
public class UpgradeChoiceManager : MonoBehaviour
{
    public static UpgradeChoiceManager Instance { get; private set; }

    [Header("常规池")]
    public UpgradePoolConfig poolConfig;
    [Header("物品池（Boss锦囊，仅V1）")]
    public ItemPoolConfig itemPoolConfig;
    [Header("主动技能池（V2）")]
    public ActiveSkillPoolConfig activeSkillPoolConfig;
    [Tooltip("三选一升级弹窗 Prefab（运行时动态生成/销毁）")]
    public GameObject popupPrefab;
    [Tooltip("道具栏满时的弃置弹窗 Prefab（运行时动态生成/销毁）")]
    public GameObject discardPopupPrefab;
    [Tooltip("选项数量（默认 3，可扩展为 4 选 1 / 5 选 1）")]
    public int choiceCount = 3;
    [Tooltip("弹窗期间是否暂停游戏")]
    public bool pauseGameDuringChoice = true;

    // ── 运行时状态 ──
    private bool _isChoosing;
    private int _pendingLevelUps;
    private int _pendingItemChoices;
    private bool _isShowingItemChoice;
    private List<UpgradeDefinition> _currentChoices;
    private UpgradeChoicePopup _activePopup;

    // 排队：按触发时间先后
    private enum PendingType { LevelUp, Item }
    private Queue<PendingType> _pendingQueue = new Queue<PendingType>();

    // ── 事件（供 UI 订阅）──
    public System.Action<List<UpgradeDefinition>> OnChoicesReady;
    public System.Action<UpgradeDefinition> OnChoiceSelected;
    public System.Action OnAllChoicesDone;

    public bool IsChoosing => _isChoosing;
    public int PendingLevelUps => _pendingLevelUps;

    private int GetCurrentLevel(UpgradeDefinition definition)
    {
        if (definition == null) return 0;
        if (definition.category == UpgradeCategory.ActiveSkill)
            return ActiveSkillInventory.Instance != null ? ActiveSkillInventory.Instance.GetLevel(definition.upgradeId) : 0;
        return UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetUpgradeLevel(definition.upgradeId) : 0;
    }

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

    private void Start()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnLevelUp += OnPlayerLevelUp;
    }

    // ── 升级触发 ──

    private void OnPlayerLevelUp(int newLevel)
    {
        _pendingLevelUps++;
        _pendingQueue.Enqueue(PendingType.LevelUp);
        if (!_isChoosing)
            StartChoiceFlow();
    }

    private static bool UsesActiveSkillRules => ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.UsesActiveSkills;

    /// <summary>
    /// Boss死亡掉落锦囊，触发物品三选一。
    /// 由 EnemyManager 调用。
    /// </summary>
    public void TriggerItemChoice()
    {
        // 通关/战败后不再弹出
        if (StageController.Instance != null && !StageController.Instance.IsStageInProgress)
            return;

        _pendingItemChoices++;
        _pendingQueue.Enqueue(PendingType.Item);
        if (!_isChoosing)
            StartChoiceFlow();
    }

    private void StartChoiceFlow()
    {
        // 通关后不再弹出
        if (StageController.Instance != null && !StageController.Instance.IsStageInProgress)
        {
            _pendingLevelUps = 0;
            _pendingItemChoices = 0;
            _pendingQueue.Clear();
            return;
        }

        _isChoosing = true;
        if (pauseGameDuringChoice)
            Time.timeScale = 0f;
        ShowNextChoice();
    }

    private void ShowNextChoice()
    {
        // 从队列取下一个待处理的类型
        if (_pendingQueue.Count == 0)
        {
            // 安全回退：无队列但有计数器时
            if (_pendingLevelUps > 0)
                _pendingQueue.Enqueue(PendingType.LevelUp);
            else if (_pendingItemChoices > 0)
                _pendingQueue.Enqueue(PendingType.Item);
            else
            {
                FinishAllChoices();
                return;
            }
        }

        var nextType = _pendingQueue.Dequeue();
        _isShowingItemChoice = (nextType == PendingType.Item);

        if (_isShowingItemChoice)
        {
            _pendingItemChoices--;
            _currentChoices = UsesActiveSkillRules ? DrawBossActiveSkillChoices() : DrawItemChoices();
        }
        else
        {
            _pendingLevelUps--;
            _currentChoices = DrawChoices();
            if (UsesActiveSkillRules && (_currentChoices == null || _currentChoices.Count == 0))
                _currentChoices = DrawBossActiveSkillChoices();
        }

        if (_currentChoices == null || _currentChoices.Count == 0)
        {
            if (_pendingQueue.Count > 0 || _pendingLevelUps > 0 || _pendingItemChoices > 0)
                ShowNextChoice();
            else
                FinishAllChoices();
            return;
        }

        // 动态生成弹窗（不在场景中预置）
        if (_activePopup == null)
        {
            if (popupPrefab == null)
            {
                Debug.LogError("[UpgradeChoiceManager] popupPrefab 未配置");
                return;
            }
            var go = Instantiate(popupPrefab);
            _activePopup = go.GetComponent<UpgradeChoicePopup>();
        }

        _activePopup.ShowChoices(_currentChoices);
        OnChoicesReady?.Invoke(_currentChoices);
    }

    // ── 玩家确认选择（由 UI 按钮调用）──

    public void ConfirmChoice(UpgradeDefinition selected)
    {
        Debug.Log($"[UpgradeChoiceManager] ConfirmChoice frame={Time.frameCount} timeScale={Time.timeScale} upgradeId={selected?.upgradeId} isItem={_isShowingItemChoice} pendingLevelUps={_pendingLevelUps} pendingItems={_pendingItemChoices} queue={_pendingQueue.Count}");
        if (!_isChoosing || _currentChoices == null) return;
        if (!_currentChoices.Contains(selected)) return;

        UpgradeEffectManager.Instance.ApplyUpgrade(selected);
        OnChoiceSelected?.Invoke(selected);

        // 检查是否还有待处理的选择
        if (_pendingQueue.Count > 0 || _pendingLevelUps > 0 || _pendingItemChoices > 0)
        {
            Debug.Log($"[UpgradeChoiceManager] 还有待处理选择，刷新弹窗 (queue={_pendingQueue.Count} lv={_pendingLevelUps} item={_pendingItemChoices})");
            ShowNextChoice();
        }
        else
        {
            FinishAllChoices();
        }
    }

    private void FinishAllChoices()
    {
        Debug.Log($"[UpgradeChoiceManager] 全部选择完成，恢复 timeScale=1 (frame={Time.frameCount})");
        _isChoosing = false;
        _currentChoices = null;
        _isShowingItemChoice = false;
        _pendingLevelUps = 0;
        _pendingItemChoices = 0;
        _pendingQueue.Clear();

        if (_activePopup != null)
        {
            var popup = _activePopup;
            _activePopup = null;
            popup.Dismiss(() => { });
        }

        if (pauseGameDuringChoice && !ItemDiscardPopup.IsShowing)
        {
            Time.timeScale = 1f;
            if (InputManager.Instance != null)
                InputManager.Instance.blockInputFrames = 2;
        }
        OnAllChoicesDone?.Invoke();
    }

    // ── 随机抽取 ──

    private List<UpgradeDefinition> DrawChoices()
    {
        if (poolConfig == null)
        {
            Debug.LogWarning("[UpgradeChoiceManager] poolConfig 未配置");
            return new List<UpgradeDefinition>();
        }

        var candidates = CollectEligible();
        if (candidates.Count == 0)
            return new List<UpgradeDefinition>();

        var results = new List<UpgradeDefinition>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < choiceCount && usedIds.Count < candidates.Count; i++)
        {
            UpgradeRarity targetRarity = RollRarity();

            // 同一稀有度中未选中的候选项
            var rarityCandidates = new List<EligibleEntry>();
            for (int j = 0; j < candidates.Count; j++)
            {
                var c = candidates[j];
                if (c.rarity == targetRarity && !usedIds.Contains(c.upgrade.upgradeId))
                    rarityCandidates.Add(c);
            }

            // 稀有度池不够 → 降级到所有剩余候选项
            if (rarityCandidates.Count == 0)
            {
                for (int j = 0; j < candidates.Count; j++)
                {
                    var c = candidates[j];
                    if (!usedIds.Contains(c.upgrade.upgradeId))
                        rarityCandidates.Add(c);
                }
            }

            if (rarityCandidates.Count == 0) break;

            var picked = WeightedPick(rarityCandidates);
            results.Add(picked.upgrade);
            usedIds.Add(picked.upgrade.upgradeId);
        }

        return results;
    }

    /// <summary>收集所有满足前置条件且未满级的候选项；V2 同时合并主动技能池。</summary>
    private List<EligibleEntry> CollectEligible()
    {
        var candidates = new List<EligibleEntry>();
        CollectFromPool(candidates, poolConfig.commonPool, UpgradeRarity.Common);
        CollectFromPool(candidates, poolConfig.rarePool, UpgradeRarity.Rare);
        CollectFromPool(candidates, poolConfig.legendaryPool, UpgradeRarity.Legendary);
        if (UsesActiveSkillRules)
            CollectActiveSkills(candidates, includeCommon: true);
        return candidates;
    }

    private void CollectFromPool(List<EligibleEntry> result, List<WeightedUpgrade> pool, UpgradeRarity rarity)
    {
        if (pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var wu = pool[i];
            if (wu.upgrade == null) continue;
            // V1 Item 与 V2 ActiveSkill 均由各自独立池处理
            if (wu.upgrade.category == UpgradeCategory.Item || wu.upgrade.category == UpgradeCategory.ActiveSkill) continue;
            if (!PrerequisitesMet(wu.upgrade)) continue;
            if (GetCurrentLevel(wu.upgrade) >= wu.upgrade.maxLevel) continue;
            result.Add(new EligibleEntry { upgrade = wu.upgrade, weight = wu.weight, rarity = rarity });
        }
    }

    /// <summary>从物品池抽取候选项</summary>
    private List<UpgradeDefinition> DrawItemChoices()
    {
        if (itemPoolConfig == null)
        {
            Debug.LogWarning("[UpgradeChoiceManager] itemPoolConfig 未配置");
            return new List<UpgradeDefinition>();
        }

        var candidates = CollectItemEligible();
        if (candidates.Count == 0)
        {
            Debug.LogWarning("[UpgradeChoiceManager] 物品池无可用候选项");
            return new List<UpgradeDefinition>();
        }

        var results = new List<UpgradeDefinition>();
        var usedIds = new HashSet<string>();

        for (int i = 0; i < choiceCount && usedIds.Count < candidates.Count; i++)
        {
            UpgradeRarity targetRarity = RollItemRarity();

            var rarityCandidates = new List<EligibleEntry>();
            for (int j = 0; j < candidates.Count; j++)
            {
                var c = candidates[j];
                if (c.rarity == targetRarity && !usedIds.Contains(c.upgrade.upgradeId))
                    rarityCandidates.Add(c);
            }

            if (rarityCandidates.Count == 0)
            {
                for (int j = 0; j < candidates.Count; j++)
                {
                    var c = candidates[j];
                    if (!usedIds.Contains(c.upgrade.upgradeId))
                        rarityCandidates.Add(c);
                }
            }

            if (rarityCandidates.Count == 0) break;

            var picked = WeightedPick(rarityCandidates);
            results.Add(picked.upgrade);
            usedIds.Add(picked.upgrade.upgradeId);
        }

        return results;
    }

    private List<EligibleEntry> CollectItemEligible()
    {
        var candidates = new List<EligibleEntry>();
        CollectFromItemPool(candidates, itemPoolConfig.commonPool, UpgradeRarity.Common);
        CollectFromItemPool(candidates, itemPoolConfig.rarePool, UpgradeRarity.Rare);
        CollectFromItemPool(candidates, itemPoolConfig.legendaryPool, UpgradeRarity.Legendary);
        return candidates;
    }

    private void CollectFromItemPool(List<EligibleEntry> result, List<WeightedUpgrade> pool, UpgradeRarity rarity)
    {
        if (pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var wu = pool[i];
            if (wu.upgrade == null) continue;
            if (!PrerequisitesMet(wu.upgrade)) continue;
            if (GetCurrentLevel(wu.upgrade) >= wu.upgrade.maxLevel) continue;
            result.Add(new EligibleEntry { upgrade = wu.upgrade, weight = wu.weight, rarity = rarity });
        }
    }

    private UpgradeRarity RollItemRarity()
    {
        float total = itemPoolConfig.commonWeight + itemPoolConfig.rareWeight + itemPoolConfig.legendaryWeight;
        float roll = Random.value * total;
        if (roll < itemPoolConfig.commonWeight) return UpgradeRarity.Common;
        if (roll < itemPoolConfig.commonWeight + itemPoolConfig.rareWeight) return UpgradeRarity.Rare;
        return UpgradeRarity.Legendary;
    }

    private bool PrerequisitesMet(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (def.prerequisites == null || def.prerequisites.Count == 0) return true;
        for (int i = 0; i < def.prerequisites.Count; i++)
        {
            var prereq = def.prerequisites[i];
            if (prereq.requiredUpgrade == null) continue;
            if (GetCurrentLevel(prereq.requiredUpgrade) < prereq.requiredLevel)
                return false;
        }
        return true;
    }

    private List<UpgradeDefinition> DrawBossActiveSkillChoices()
    {
        var candidates = new List<EligibleEntry>();
        CollectActiveSkills(candidates, includeCommon: false);
        return DrawFromEligible(candidates);
    }

    private void CollectActiveSkills(List<EligibleEntry> result, bool includeCommon)
    {
        if (activeSkillPoolConfig == null || ActiveSkillInventory.Instance == null) return;
        if (includeCommon)
            CollectFromActiveSkillPool(result, activeSkillPoolConfig.commonPool, UpgradeRarity.Common);
        CollectFromActiveSkillPool(result, activeSkillPoolConfig.rarePool, UpgradeRarity.Rare);
        CollectFromActiveSkillPool(result, activeSkillPoolConfig.legendaryPool, UpgradeRarity.Legendary);
    }

    private void CollectFromActiveSkillPool(List<EligibleEntry> result, List<WeightedActiveSkill> pool, UpgradeRarity rarity)
    {
        if (pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var weighted = pool[i];
            var skill = weighted.skill;
            if (skill == null || skill.category != UpgradeCategory.ActiveSkill) continue;
            var inventory = ActiveSkillInventory.Instance;
            if (inventory == null || !inventory.CanAcquire(skill)) continue;
            result.Add(new EligibleEntry { upgrade = skill, weight = Mathf.Max(1, weighted.weight), rarity = rarity });
        }
    }

    private List<UpgradeDefinition> DrawFromEligible(List<EligibleEntry> candidates)
    {
        var results = new List<UpgradeDefinition>();
        if (candidates == null || candidates.Count == 0) return results;

        var usedIds = new HashSet<string>();
        for (int i = 0; i < choiceCount && usedIds.Count < candidates.Count; i++)
        {
            var remaining = new List<EligibleEntry>();
            for (int j = 0; j < candidates.Count; j++)
                if (!usedIds.Contains(candidates[j].upgrade.upgradeId))
                    remaining.Add(candidates[j]);
            if (remaining.Count == 0) break;
            var picked = WeightedPick(remaining);
            results.Add(picked.upgrade);
            usedIds.Add(picked.upgrade.upgradeId);
        }
        return results;
    }

    /// <summary>按稀有度权重随机选择稀有度</summary>
    private UpgradeRarity RollRarity()
    {
        float total = poolConfig.commonWeight + poolConfig.rareWeight + poolConfig.legendaryWeight;
        float roll = Random.value * total;
        if (roll < poolConfig.commonWeight) return UpgradeRarity.Common;
        if (roll < poolConfig.commonWeight + poolConfig.rareWeight) return UpgradeRarity.Rare;
        return UpgradeRarity.Legendary;
    }

    /// <summary>按权重随机抽取一个</summary>
    private static EligibleEntry WeightedPick(List<EligibleEntry> candidates)
    {
        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
            totalWeight += candidates[i].weight;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidates[i].weight;
            if (roll < cumulative)
                return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    private struct EligibleEntry
    {
        public UpgradeDefinition upgrade;
        public int weight;
        public UpgradeRarity rarity;
    }
}
