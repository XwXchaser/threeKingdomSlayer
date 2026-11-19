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

    private IStageBattleState StageBattleState => _stageBattleState ??= StageRoles.BattleState;
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
    [Tooltip("波次结束时才弹三选一（2026-11-05 用户定）：升级不再当场打断战斗，\n选择攒到本波清空时一起给。关掉则恢复“升级立刻弹”。")]
    public bool deferChoicesToWaveEnd = true;

    [Tooltip("教学关用：非空时三选一固定显示这些选项（按顺序），不走随机池；只填 1 个就是强制选择")]
    public List<UpgradeDefinition> forcedChoices = new List<UpgradeDefinition>();

    [Tooltip("教学关等脚本化关卡用：置 true 时不弹三选一、不暂停（否则升级会把下一波卡住）")]
    public bool suppressChoices;

    [Header("满槽铜钱补偿")]
    [Tooltip("三选一没有候选时（槽位满 / 全部满级）改发的铜钱基数")]
    [SerializeField] private int _fullSlotCoinBase = 30;
    [Tooltip("每级额外铜钱：补偿 = 基数 + 每级 × 当前等级")]
    [SerializeField] private int _fullSlotCoinPerLevel = 5;

    // ── 运行时状态 ──
    private bool _isChoosing;
    /// <summary>「这次的 timeScale=0 是我干的」→ 销毁时要把时间还回去（否则重载后的场景被冻住）。</summary>
    private bool _pausedByUs;
    private int _pendingLevelUps;
    private int _pendingItemChoices;
    /// <summary>待处理的**装备**选择次数（影之武装 / 饰品；精英敌人掉落带来）。</summary>
    private int _pendingEquipmentChoices;
    private bool _isShowingItemChoice;
    /// <summary>当前这次选择是不是“装备类”（用于 UI 标题区分）。</summary>
    private bool _isShowingEquipmentChoice;
    private List<UpgradeDefinition> _currentChoices;
    private IUpgradeChoiceView _activePopup;

    /// <summary>抽池器（候选收集 + 抽取）。入池条件源注册在它上面。</summary>
    private UpgradePoolSampler _sampler;

    /// <summary>选中后的额外效果（扩展点）。</summary>
    private readonly List<IUpgradeSelectionEffect> _selectionEffects = new List<IUpgradeSelectionEffect>();

    // 排队：按触发时间先后
    /// <summary>
    /// 待处理选择的类型（2026-11-16 拆成三类）：
    ///   LevelUp   = **属性 / 机制**（经验升级带来；**不含主动技能**）
    ///   Equipment = **装备**（影之武装 / 饰品；**精英敌人掉落**带来）
    ///   Item      = 道具（Boss 锦囊）
    /// </summary>
    private enum PendingType { LevelUp, Equipment, Item }
    private Queue<PendingType> _pendingQueue = new Queue<PendingType>();

    // ── 事件（供 UI 订阅）──
    public System.Action<List<UpgradeDefinition>> OnChoicesReady;
    public System.Action<UpgradeDefinition> OnChoiceSelected;
    public System.Action OnAllChoicesDone;

    public bool IsChoosing => _isChoosing;

    /// <summary>当前是否有**排队中**、还没弹出来的选择（升级三选一 / Boss 锦囊）。</summary>
    public bool HasPendingChoices => _pendingQueue.Count > 0 || _pendingLevelUps > 0 || _pendingItemChoices > 0;

    /// <summary>当前弹出的三选一窗口（教学要指着推荐的那张卡）。</summary>
    public IUpgradeChoiceView ActivePopup => _activePopup;
    public int PendingLevelUps => _pendingLevelUps;

    /// <summary>
    /// 当前**待选取**的三选一数量（升级三选一 + Boss 锦囊三选一）。
    ///
    /// ⚠️ **包含已经在弹的那一个**（2026-11-07 用户反馈：「等级球实际显示的是能获得的 3 选 1 数量 - 1」）：
    ///   弹出时 <see cref="ShowNextChoice"/> 会把计数**先减掉**（那一项已经被取出来放进弹窗了），
    ///   所以只看队列计数会**少 1** → 这里把「正在弹的那一个」也算回去。
    /// HUD 的等级球数字用它。
    /// </summary>
    public int PendingChoiceCount => _pendingLevelUps + _pendingItemChoices + _pendingEquipmentChoices + (_isChoosing ? 1 : 0);

    /// <summary>抽池器（调试 / 工具用；注册条件源也走它）。惰性创建：Awake 之前也能用。</summary>
    public UpgradePoolSampler PoolSampler
    {
        get
        {
            if (_sampler == null) _sampler = CreateSampler();
            return _sampler;
        }
    }

    /// <summary>被动槽上限的缺省值（没有武将配置时用，与 <c>HeroConfig.passiveSlotCount</c> 的基础值一致 = 2）。</summary>
    private const int DefaultPassiveSlotCount = 2;

    /// <summary>
    /// 被动槽上限（基础 2，来自 <c>HeroConfig.passiveSlotCount</c>；博学天赋再 +N）。
    /// 老资产 / 漏改资产的字段是 0 → 按 1 兜底；没有武将配置时用基础值。
    /// </summary>
    private int PassiveSlotCapacity
    {
        get
        {
            var hero = PlayerState.Instance != null ? PlayerState.Instance.heroConfig : null;
            int baseCap = hero != null ? Mathf.Max(1, hero.passiveSlotCount) : DefaultPassiveSlotCount;
            return baseCap + TalentCapabilities.ExtraPassiveSlot;
        }
    }

    /// <summary>创建抽池器（依赖注入：等级查询 / 被动上限 / 主动技能可获得性）。</summary>
    private UpgradePoolSampler CreateSampler()
    {
        var sampler = new UpgradePoolSampler(
            GetCurrentLevel,
            () => UpgradeEffectManager.Instance != null && UpgradeEffectManager.Instance.PassiveSlotUserCount >= PassiveSlotCapacity,
            def => ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.CanAcquire(def),
            def => UpgradeEffectManager.Instance != null
                ? UpgradeEffectManager.Instance.GetEffectiveMaxLevel(def)
                : Mathf.Max(1, def != null ? def.maxLevel : 1));
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
        // ⚠️ **面板开着的时候退出会话**（教学关退出回主菜单 → Battle 场景整场景重载）
        //   → 这里 timeScale 还是 0（是本组件自己暂停的）→ **重载后的主菜单被冻住**、点不动。
        //   本组件把游戏暂停了，就得负责在销毁时把时间还回去。
        //   只还「自己暂停的那一次」→ 别人的暂停（教学的冻结等）不归我管。
        if (_pausedByUs) Time.timeScale = 1f;
        _pausedByUs = false;
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

    /// <summary>
    /// **主动发放 N 次三选一**（2026-11-16，无尽模式开局给"起步 build"用）。
    /// 与升级同一条队列（默认波末统一弹，不打断战斗）。
    /// </summary>
    public void GrantChoices(int count)
    {
        if (count <= 0) return;
        for (int i = 0; i < count; i++)
        {
            _pendingLevelUps++;
            _pendingQueue.Enqueue(PendingType.LevelUp);
        }
        if (deferChoicesToWaveEnd) return;
        if (!_isChoosing) StartChoiceFlow();
    }

    // ── 升级触发 ──

    private void OnPlayerLevelUp(int newLevel)
    {
        _pendingLevelUps++;
        _pendingQueue.Enqueue(PendingType.LevelUp);

        // 波末弹（默认）：升级只入队，不当场打断战斗。
        // 波次清空时由 <see cref="FlushPendingChoices"/> 统一弹出。
        if (deferChoicesToWaveEnd) return;

        if (!_isChoosing)
            StartChoiceFlow();
    }

    /// <summary>
    /// **把待处理的选择弹出来**（波次清空时调用）。
    ///
    /// 升级三选一默认不再当场弹（<see cref="deferChoicesToWaveEnd"/>），
    /// 而是攒到波末一起给 —— 避免战斗中途被弹窗打断（体验断档）。
    /// 没有待处理的选择时什么都不做。
    /// </summary>
    public void FlushPendingChoices()
    {
        if (_isChoosing) return;
        if (_pendingQueue.Count == 0 && _pendingLevelUps <= 0 && _pendingItemChoices <= 0 && _pendingEquipmentChoices <= 0) return;
        StartChoiceFlow();
    }

    private static bool UsesActiveSkillRules => ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.UsesActiveSkills;

    /// <summary>
    /// Boss死亡掉落锦囊，触发物品三选一。
    /// 由 EnemyManager 调用。
    ///
    /// ⚠️ **V2（主动技能）规则下直接返回**（2026-11-17 用户定）：
    /// 「装备 / 饰品应该跟**精英敌人**」⇒ Boss 的战利品就是**精英掉落**那一段（100% 掉）。
    /// 再单独弹一次锦囊 ⇒ Boss 弹两次、内容还和精英掉落撞车（锦囊里发的正是主动技能）。
    /// ⇒ V2 下锦囊并入精英掉落；V1（旧道具规则）保持原样。
    /// </summary>
    public void TriggerItemChoice()
    {
        // V2：装备的唯一来源是精英敌人掉落 → 不再单独弹 Boss 锦囊
        if (UsesActiveSkillRules) return;

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
            _pendingEquipmentChoices = 0;
            _pendingQueue.Clear();
            return;
        }

        // 通关后不再弹出
        if (StageBattleState != null && !StageBattleState.IsStageInProgress)
        {
            _pendingLevelUps = 0;
            _pendingItemChoices = 0;
            _pendingEquipmentChoices = 0;
            _pendingQueue.Clear();
            return;
        }

        _isChoosing = true;
        if (pauseGameDuringChoice)
        {
            Time.timeScale = 0f;
            _pausedByUs = true;   // 记住「这次暂停是我干的」→ 销毁时要还回去
        }
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
            else if (_pendingEquipmentChoices > 0)
                _pendingQueue.Enqueue(PendingType.Equipment);
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
        _isShowingEquipmentChoice = (nextType == PendingType.Equipment);

        if (_isShowingItemChoice)
        {
            _pendingItemChoices--;
            _currentChoices = DrawItemChoices();
        }
        else if (_isShowingEquipmentChoice)
        {
            // 装备三选一（影之武装 / 饰品）：**只从主动技能池抽**，不与属性/机制混
            _pendingEquipmentChoices--;
            _currentChoices = DrawEquipmentChoices();
        }
        else
        {
            // 属性 / 机制三选一（经验升级带来）：**不抽主动技能**（2026-11-16 拆分）
            _pendingLevelUps--;
            _currentChoices = forcedChoices != null && forcedChoices.Count > 0
                ? new List<UpgradeDefinition>(forcedChoices)
                : DrawChoices();
        }

        if (_currentChoices == null || _currentChoices.Count == 0)
        {
            // 池空不再是「静默跳过」：把这次没得选的升级折算成局内铜钱（R8 / A3）。
            // 只对**升级三选一**生效；Boss 锦囊（Item）池空仍按原样跳过。
            if (!_isShowingItemChoice && !_isShowingEquipmentChoice) GrantFullSlotCoinCompensation();

            if (_pendingQueue.Count > 0 || _pendingLevelUps > 0 || _pendingItemChoices > 0 || _pendingEquipmentChoices > 0)
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
            // 只认端口，不认界面组件类型（2026-10-22 asmdef 分层第五批）。
            _activePopup = go.GetComponent<IUpgradeChoiceView>();
            if (_activePopup == null)
            {
                Debug.LogError("[UpgradeChoiceManager] popupPrefab 上找不到 IUpgradeChoiceView 实现");
                return;
            }
        }

        _activePopup.ShowChoices(_currentChoices);
        NotifyChoiceKind();
        OnChoicesReady?.Invoke(_currentChoices);
    }

    /// <summary>
    /// 告诉弹窗「这次弹的是哪一类奖励」（标题区分：属性强化 / 装备掉落）。
    /// 弹窗没实现 <see cref="IUpgradeChoiceKindView"/> 时静默跳过。
    /// </summary>
    private void NotifyChoiceKind()
    {
        var kindView = _activePopup as IUpgradeChoiceKindView;
        if (kindView == null) return;
        kindView.SetChoiceKind(_isShowingItemChoice ? UpgradeRewardKind.Item
            : _isShowingEquipmentChoice ? UpgradeRewardKind.Equipment
            : UpgradeRewardKind.Attribute);
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

    /// <summary>
    /// 满槽 / 全部满级时的**铜钱补偿**（R8）：把这次「没得选」的升级折算成局内铜钱。
    /// 铜钱走 <c>PlayerState.AddCoins</c> → 局内 session → <c>StageController.SettleCoins()</c>
    /// → 存档 → **天赋树可花**（局外解锁力）。
    /// 提示复用 <c>KillRewardUI</c> 的奖励弹窗形态（经 <see cref="CombatPresenters.InRunReward"/> 端口）；
    /// 界面上没有这个实现时**安全降级**——<c>CoinCounterUI</c> 仍会飘「+N」。
    /// </summary>
    private void GrantFullSlotCoinCompensation()
    {
        var ps = PlayerState.Instance;
        if (ps == null) return;

        int level = Mathf.Max(0, ps.currentLevel);
        int amount = _fullSlotCoinBase + _fullSlotCoinPerLevel * level;
        if (amount <= 0) return;

        ps.AddCoins(amount);

        bool slotFull = IsUpgradeSlotFull();
        string title = slotFull ? "槽位已满，改为发放铜钱" : "升级已满级，改为发放铜钱";
        var popup = CombatPresenters.InRunReward;
        if (popup != null) popup.ShowReward(title, $"+{amount} 铜钱");

        Debug.Log($"[UpgradeChoiceManager] 没有可选项 → 发放铜钱补偿 +{amount}（等级 {level}，槽位满={slotFull}）");
    }

    /// <summary>槽位是否真的满了（用于区分「槽位满」与「全部满级」两种提示文案）。</summary>
    private bool IsUpgradeSlotFull()
    {
        var uem = UpgradeEffectManager.Instance;
        if (uem != null && uem.PassiveSlotUserCount >= PassiveSlotCapacity) return true;
        var asi = ActiveSkillInventory.Instance;
        return asi != null && asi.UsesActiveSkills && asi.IsFull;
    }

    /// <summary>
    /// 本次弹窗的**选项数**：基础 <see cref="choiceCount"/> + 运筹天赋的 1 个额外选项（三选一 → 四选一）。
    /// </summary>
    private int EffectiveChoiceCount => Mathf.Max(1, choiceCount + TalentCapabilities.ChoiceCountBonus);

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
            _pausedByUs = false;
            if (PlayerInputHub.Current != null)
                PlayerInputHub.Current.BlockInputForFrames(2);
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

            // ★ 属性成长 = **纯数值**（2026-11-17 用户口径）：
            //   「属性成长应该跟经验」⇒ 经验升级带来的三选一**只出属性**，不出装备 / 饰品。
            var candidates = CollectAttributeCandidates();
            if (candidates.Count == 0)
                return new List<UpgradeDefinition>();

            return _sampler.DrawByRarity(candidates, EffectiveChoiceCount, RollRarity);
        }

        /// <summary>
        /// **属性候选**（经验升级这一段）。
        /// V2：只收纯数值（不占槽、可无限叠）。
        /// V1（旧道具规则）：保持旧口径不变（除主动技能外全收）——V1 没有"装备 / 饰品"这条线。
        /// </summary>
        private List<UpgradePoolSampler.Candidate> CollectAttributeCandidates()
        {
            var candidates = new List<UpgradePoolSampler.Candidate>();
            if (UsesActiveSkillRules && activeSkillPoolConfig != null)
            {
                _sampler.CollectAttributes(candidates, activeSkillPoolConfig.commonPool, UpgradeRarity.Common);
                _sampler.CollectAttributes(candidates, activeSkillPoolConfig.rarePool, UpgradeRarity.Rare);
                _sampler.CollectAttributes(candidates, activeSkillPoolConfig.legendaryPool, UpgradeRarity.Legendary);
            }
            else
            {
                _sampler.CollectRegular(candidates, poolConfig.commonPool, UpgradeRarity.Common);
                _sampler.CollectRegular(candidates, poolConfig.rarePool, UpgradeRarity.Rare);
                _sampler.CollectRegular(candidates, poolConfig.legendaryPool, UpgradeRarity.Legendary);
            }
            return candidates;
        }

        /// <summary>
        /// **装备三选一**（影之武装 + 饰品）：只由**精英敌人掉落**触发
        /// （见 <see cref="GrantEquipmentChoices"/>）。
        ///
        /// 两个子池**交替**出牌，保证每次都能看到"武器 + 饰品"的组合：
        ///   · **影之武装** = 主动技能（占主动槽）
        ///   · **饰品**     = 机制型被动（占被动槽）
        /// 子池内部按稀有度权重抽；某个子池没货就退回另一个。
        /// </summary>
        private List<UpgradeDefinition> DrawEquipmentChoices()
        {
            var shadowArmaments = new List<UpgradePoolSampler.Candidate>();
            var accessories = new List<UpgradePoolSampler.Candidate>();
            CollectActiveSkills(shadowArmaments, includeCommon: true);
            CollectAccessories(accessories);

            int count = EffectiveChoiceCount;
            var results = new List<UpgradeDefinition>();
            var usedIds = new HashSet<string>();
            // 每次弹窗随机决定先出武器还是先出饰品（避免每张牌的顺序都一样）
            bool firstIsShadowArmament = Random.value < 0.5f;

            for (int i = 0; i < count; i++)
            {
                bool preferShadowArmament = ((i % 2 == 0) == firstIsShadowArmament);
                var primary = preferShadowArmament ? shadowArmaments : accessories;
                var fallback = preferShadowArmament ? accessories : shadowArmaments;
                if (CountAvailable(primary, usedIds) == 0) primary = fallback;
                if (CountAvailable(primary, usedIds) == 0) break;

                var picked = _sampler.DrawOneByRarity(primary, usedIds, RollRarity);
                if (picked == null) break;
                results.Add(picked);
                usedIds.Add(picked.upgradeId);
            }
            return results;
        }

        /// <summary>子池里还有多少条没被选过。</summary>
        private static int CountAvailable(List<UpgradePoolSampler.Candidate> candidates, HashSet<string> usedIds)
        {
            if (candidates == null) return 0;
            int n = 0;
            for (int i = 0; i < candidates.Count; i++)
                if (!usedIds.Contains(candidates[i].upgrade.upgradeId)) n++;
            return n;
        }

        /// <summary>
        /// **发放 N 次装备选择**（2026-11-17：影之武装 / 饰品改为**精英敌人掉落**，不再混在升级三选一里）。
        /// 由 EnemyManager 在精英死亡时调用。
        ///
        /// ⚠️ 只在 V2（主动技能）规则下生效：V1 没有"影之武装"这条线。
        /// </summary>
        public void GrantEquipmentChoices(int count)
        {
            if (!UsesActiveSkillRules) return;
            if (suppressChoices) return;
            if (StageBattleState != null && !StageBattleState.IsStageInProgress) return;
            if (count <= 0) return;
            for (int i = 0; i < count; i++)
            {
                _pendingEquipmentChoices++;
                _pendingQueue.Enqueue(PendingType.Equipment);
            }
            // ★ **波末统一弹**（2026-11-17 用户定：“波次结束后才选升级成长和武器”）：
            //   精英掉落的装备选择与升级三选一走**同一条队列、同一个时机** ⇒
            //   不再在精英死亡的那一瞬间当场弹窗打断战斗。
            if (deferChoicesToWaveEnd) return;
            if (!_isChoosing) StartChoiceFlow();
        }
    
        /// <summary>收集**饰品**候选（机制型被动，占被动槽）。</summary>
        private void CollectAccessories(List<UpgradePoolSampler.Candidate> result)
        {
            if (activeSkillPoolConfig == null) return;
            _sampler.CollectAccessories(result, activeSkillPoolConfig.commonPool, UpgradeRarity.Common);
            _sampler.CollectAccessories(result, activeSkillPoolConfig.rarePool, UpgradeRarity.Rare);
            _sampler.CollectAccessories(result, activeSkillPoolConfig.legendaryPool, UpgradeRarity.Legendary);
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
