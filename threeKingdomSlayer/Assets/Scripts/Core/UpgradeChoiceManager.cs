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
///
/// 【2026-10-22 解耦】「什么能进池、怎么抽」已拆到 <see cref="UpgradePoolSampler"/>；
/// 本类只管流程。两把扩展点：
///   • 入池条件：<see cref="IUpgradeRequirementProvider"/>（如按天赋解锁入池）；
///   • 选中后的额外效果：<see cref="IUpgradeSelectionEffect"/>。
/// </summary>
public class UpgradeChoiceManager : MonoBehaviour
{

    /// <summary>
    /// 关卡战斗状态（只读 + 状态事件）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageBattleState _stageBattleState;

    private IStageBattleState StageBattleState => _stageBattleState ??= StageController.Instance;
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

    [Tooltip("教学关用：非空时三选一固定显示这些选项（按顺序），不走随机池；只填 1 个就是强制选择")]
    public List<UpgradeDefinition> forcedChoices = new List<UpgradeDefinition>();

    [Tooltip("教学关等脚本化关卡用：置 true 时不弹三选一、不暂停（否则升级会把下一波卡住）")]
    public bool suppressChoices;

    // ── 运行时状态 ──
    private bool _isChoosing;
    private int _pendingLevelUps;
    private int _pendingItemChoices;
    private bool _isShowingItemChoice;
    private List<UpgradeDefinition> _currentChoices;
    private UpgradeChoicePopup _activePopup;

    /// <summary>抽池器（候选收集 + 抽取）。入池条件源注册在它上面。</summary>
    private UpgradePoolSampler _sampler;

    /// <summary>选中后的额外效果（扩展点）。</summary>
    private readonly List<IUpgradeSelectionEffect> _selectionEffects = new List<IUpgradeSelectionEffect>();

    // 排队：按触发时间先后
    private enum PendingType { LevelUp, Item }
    private Queue<PendingType> _pendingQueue = new Queue<PendingType>();

    // ── 事件（供 UI 订阅）──
    public System.Action<List<UpgradeDefinition>> OnChoicesReady;
    public System.Action<UpgradeDefinition> OnChoiceSelected;
    public System.Action OnAllChoicesDone;

    public bool IsChoosing => _isChoosing;
    public int PendingLevelUps => _pendingLevelUps;

    /// <summary>抽池器（调试 / 工具用；注册条件源也走它）。惰性创建：Awake 之前也能用。</summary>
    public UpgradePoolSampler PoolSampler
    {
        get
        {
            if (_sampler == null) _sampler = CreateSampler();
            return _sampler;
        }
    }

    /// <summary>创建抽池器（依赖注入：等级查询 / 被动上限 / 主动技能可获得性）。</summary>
    private UpgradePoolSampler CreateSampler()
    {
        var sampler = new UpgradePoolSampler(
            GetCurrentLevel,
            () => UpgradeEffectManager.Instance != null && UpgradeEffectManager.Instance.PassiveUpgradeCount >= 6,
            def => ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.CanAcquire(def));
        // 内置条件源：按天赋解锁入池（升级定义填了 requiredTalentIds 才生效）。
        sampler.AddRequirementProvider(new TalentUpgradeRequirementProvider());
        return sampler;
    }

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
        _sampler = CreateSampler();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── 扩展点注册 ──

    /// <summary>
    /// 注册**入池条件源**（扩展点）。例如「某条升级需要先解锁某个天赋才进池」。
    /// 多个条件源是「与」的关系：凡是适用的条件源都必须满足。
    /// </summary>
    public void RegisterRequirementProvider(IUpgradeRequirementProvider provider)
    {
        if (provider == null) return;
        PoolSampler.AddRequirementProvider(provider);
    }

    /// <summary>
    /// 注册**选中后的额外效果**（扩展点）。在升级本身的效果应用之后调用一次。
    /// </summary>
    public void RegisterSelectionEffect(IUpgradeSelectionEffect effect)
    {
        if (effect == null) return;
        _selectionEffects.Add(effect);
    }

    /// <summary>
    /// 注册**选中后的额外效果**（扩展点，委托版）。
    /// </summary>
    public void RegisterSelectionEffect(
        System.Func<UpgradeDefinition, bool> appliesTo,
        System.Action<UpgradeDefinition> onSelected)
    {
        RegisterSelectionEffect(new DelegateUpgradeSelectionEffect(appliesTo, onSelected));
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
        // 教学关等脚本化关卡：不弹三选一、不暂停（否则波次会被卡住）
        if (suppressChoices) return;

        // 通关/战败后不再弹出
        if (StageBattleState != null && !StageBattleState.IsStageInProgress)
            return;

        _pendingItemChoices++;
        _pendingQueue.Enqueue(PendingType.Item);
        if (!_isChoosing)
            StartChoiceFlow();
    }

    private void StartChoiceFlow()
    {
        // 教学关等脚本化关卡：吞掉本次选择，不暂停
        if (suppressChoices)
        {
            _pendingLevelUps = 0;
            _pendingItemChoices = 0;
            _pendingQueue.Clear();
            return;
        }

        // 通关后不再弹出
        if (StageBattleState != null && !StageBattleState.IsStageInProgress)
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
            _currentChoices = forcedChoices != null && forcedChoices.Count > 0
                ? new List<UpgradeDefinition>(forcedChoices)
                : DrawChoices();
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
        ApplySelectionEffects(selected);

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

    /// <summary>执行选中后的额外效果（扩展点）。在升级本身的效果应用之后。</summary>
    private void ApplySelectionEffects(UpgradeDefinition selected)
    {
        for (int i = 0; i < _selectionEffects.Count; i++)
        {
            var effect = _selectionEffects[i];
            if (effect == null || !effect.AppliesTo(selected)) continue;
            try
            {
                effect.OnSelected(selected);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[UpgradeChoiceManager] 选中额外效果执行失败: " + e);
            }
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

        if (pauseGameDuringChoice && !(CombatPresenters.ItemDiscard != null && CombatPresenters.ItemDiscard.IsShowing))
        {
            Time.timeScale = 1f;
            if (InputManager.Instance != null)
                InputManager.Instance.blockInputFrames = 2;
        }
        OnAllChoicesDone?.Invoke();
    }

    // ── 随机抽取（实现见 UpgradePoolSampler）──
    
        private List<UpgradeDefinition> DrawChoices()
        {
            bool v2 = UsesActiveSkillRules && activeSkillPoolConfig != null;
            if (!v2 && poolConfig == null)
            {
                Debug.LogWarning("[UpgradeChoiceManager] poolConfig 未配置");
                return new List<UpgradeDefinition>();
            }
    
            var candidates = CollectEligible();
            if (candidates.Count == 0)
                return new List<UpgradeDefinition>();
    
            return _sampler.DrawByRarity(candidates, choiceCount, RollRarity);
        }
    
        /// <summary>收集所有满足前置条件且未满级的候选项；V2 从 activeSkillPoolConfig 读取全池。</summary>
        private List<UpgradePoolSampler.Candidate> CollectEligible()
        {
            var candidates = new List<UpgradePoolSampler.Candidate>();
            if (UsesActiveSkillRules && activeSkillPoolConfig != null)
            {
                _sampler.CollectUnified(candidates, activeSkillPoolConfig.commonPool, UpgradeRarity.Common);
                _sampler.CollectUnified(candidates, activeSkillPoolConfig.rarePool, UpgradeRarity.Rare);
                _sampler.CollectUnified(candidates, activeSkillPoolConfig.legendaryPool, UpgradeRarity.Legendary);
            }
            else
            {
                _sampler.CollectRegular(candidates, poolConfig.commonPool, UpgradeRarity.Common);
                _sampler.CollectRegular(candidates, poolConfig.rarePool, UpgradeRarity.Rare);
                _sampler.CollectRegular(candidates, poolConfig.legendaryPool, UpgradeRarity.Legendary);
            }
            return candidates;
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
    
            return _sampler.DrawByRarity(candidates, choiceCount, RollItemRarity);
        }
    
        private List<UpgradePoolSampler.Candidate> CollectItemEligible()
        {
            var candidates = new List<UpgradePoolSampler.Candidate>();
            _sampler.CollectItem(candidates, itemPoolConfig.commonPool, UpgradeRarity.Common);
            _sampler.CollectItem(candidates, itemPoolConfig.rarePool, UpgradeRarity.Rare);
            _sampler.CollectItem(candidates, itemPoolConfig.legendaryPool, UpgradeRarity.Legendary);
            return candidates;
        }
    
        private UpgradeRarity RollItemRarity()
        {
            float total = itemPoolConfig.commonWeight + itemPoolConfig.rareWeight + itemPoolConfig.legendaryWeight;
            float roll = Random.value * total;
            if (roll < itemPoolConfig.commonWeight) return UpgradeRarity.Common;
            if (roll < itemPoolConfig.commonWeight + itemPoolConfig.rareWeight) return UpgradeRarity.Rare;
            return UpgradeRarity.Legendary;
        }
    
        private List<UpgradeDefinition> DrawBossActiveSkillChoices()
        {
            var candidates = new List<UpgradePoolSampler.Candidate>();
            CollectActiveSkills(candidates, includeCommon: false);
            return _sampler.DrawByWeight(candidates, choiceCount);
        }
    
        private void CollectActiveSkills(List<UpgradePoolSampler.Candidate> result, bool includeCommon)
        {
            if (activeSkillPoolConfig == null || ActiveSkillInventory.Instance == null) return;
            if (includeCommon)
                _sampler.CollectActiveSkills(result, activeSkillPoolConfig.commonPool, UpgradeRarity.Common);
            _sampler.CollectActiveSkills(result, activeSkillPoolConfig.rarePool, UpgradeRarity.Rare);
            _sampler.CollectActiveSkills(result, activeSkillPoolConfig.legendaryPool, UpgradeRarity.Legendary);
        }
    
        /// <summary>按稀有度权重随机选择稀有度</summary>
        private UpgradeRarity RollRarity()
        {
            bool v2 = UsesActiveSkillRules && activeSkillPoolConfig != null;
            float commonW = v2 ? activeSkillPoolConfig.commonWeight : poolConfig.commonWeight;
            float rareW = v2 ? activeSkillPoolConfig.rareWeight : poolConfig.rareWeight;
            float legendaryW = v2 ? activeSkillPoolConfig.legendaryWeight : poolConfig.legendaryWeight;
            float total = commonW + rareW + legendaryW;
            float roll = Random.value * total;
            if (roll < commonW) return UpgradeRarity.Common;
            if (roll < commonW + rareW) return UpgradeRarity.Rare;
            return UpgradeRarity.Legendary;
        }
    }
