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

    [Header("常规池（V1）")]
    public UpgradePoolConfig poolConfig;
    [Header("物品池（Boss锦囊，仅V1）")]
    public ItemPoolConfig itemPoolConfig;
    [Header("属性池（V2 经验升级：品质概率全局统一）")]
    [Tooltip("经验升级三选一的候选来源。品质概率全局统一，不随内部圈数、不使用装备档位。")]
    public AttributePoolConfig attributePoolConfig;
    [Header("装备池（V2 精英 / Boss 掉落：变体独立档位 + 内部圈数阶段）")]
    [Tooltip("影之武装与饰品统一一套候选；不保底、不按类型交替、不受槽位 / 同款过滤。")]
    public EquipmentPoolConfig equipmentPoolConfig;
    [Header("旧主动技能池（已退役）")]
    [Tooltip("2026-11-25 起 V2 不再使用：属性走 attributePoolConfig，装备走 equipmentPoolConfig。\n" +
             "保留引用只为不丢已有Inspector绑定（TutorialSetupTool 等编辑器工具仍在写它）。")]
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

    /// <summary>满槽异款：正在等玩家点下方旧装备 / 跳过。
    /// 此期间不推进队列；上方候选卡仍可点，用于切换到另一件新装备。</summary>
    private bool _awaitingReplacePick;
    /// <summary>满槽异款时玩家刚点的新变体（等下方选中旧装备后使用）。</summary>
    private UpgradeDefinition _pendingReplacementVariant;

    /// <summary>内部圈数来源（无尽关注册；非无尽关为空 → 装备档位按第 1 圈处理）。</summary>
    private IEndlessCircleSource _circleSource;
    private bool _circleFallbackLogged;

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
    private readonly RewardChoiceQueue _pendingQueue = new RewardChoiceQueue();

    // ── 事件（供 UI 订阅）──
    public System.Action<List<UpgradeDefinition>> OnChoicesReady;
    public System.Action<UpgradeDefinition> OnChoiceSelected;
    public System.Action OnAllChoicesDone;

    public bool IsChoosing => _isChoosing;

    /// <summary>当前是否有**排队中**、还没弹出来的选择（属性 / 装备 / Boss 锦囊）。</summary>
    public bool HasPendingChoices => _pendingQueue.Count > 0 || _pendingLevelUps > 0
        || _pendingItemChoices > 0 || _pendingEquipmentChoices > 0;

    /// <summary>当前弹出的三选一窗口（教学要指着推荐的那张卡）。</summary>
    public IUpgradeChoiceView ActivePopup => _activePopup;
    public int PendingLevelUps => _pendingLevelUps;

    /// <summary>
    /// 当前**待选取**的三选一数量（属性升级 + 装备 + Boss 锦囊）。
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

    // ── 无尽内部圈数注入（Data 层契约，Combat 不引用 Flow）──

    /// <summary>无尽关开局注册自己（<see cref="IEndlessCircleSource"/>）；只影响装备各档概率。</summary>
    public void SetCircleSource(IEndlessCircleSource source)
    {
        _circleSource = source;
        _circleFallbackLogged = false;
    }

    /// <summary>无尽关结束 / 销毁时注销（只注销自己，避免误清别人的注册）。</summary>
    public void ClearCircleSource(IEndlessCircleSource source)
    {
        if (ReferenceEquals(_circleSource, source)) _circleSource = null;
    }

    /// <summary>当前内部圈数（供掉落时盖章；无来源时按 1；不进 HUD / 不计分）。</summary>
    public int CurrentCircleForDrop => _circleSource != null ? Mathf.Max(1, _circleSource.Circle) : 1;

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
            _pendingQueue.Add(RewardChoiceType.LevelUp);
        }
        if (deferChoicesToWaveEnd) return;
        if (!_isChoosing) StartChoiceFlow();
    }

    // ── 升级触发 ──

    private void OnPlayerLevelUp(int newLevel)
    {
        _pendingLevelUps++;
        _pendingQueue.Add(RewardChoiceType.LevelUp);

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
        _pendingQueue.Add(RewardChoiceType.Item);
        if (!_isChoosing)
            StartChoiceFlow();
    }

    private void StartChoiceFlow()
    {
        // 教学关等脚本化关卡：吞掉本次选择，不暂停
        if (suppressChoices)
        {
            AbandonAllPending();
            return;
        }

        // 通关后不再弹出
        if (StageBattleState != null && !StageBattleState.IsStageInProgress)
        {
            AbandonAllPending();
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
        // 从队列取下一个待处理的类型（Equipment 优先于 LevelUp；同类型按到达顺序）
        if (_pendingQueue.Count == 0)
        {
            // 安全回退：无队列但有计数器时（按优先级补一个；后续会继续补）
            if (_pendingEquipmentChoices > 0)
                _pendingQueue.Add(RewardChoiceType.Equipment, CurrentCircleForDrop);
            else if (_pendingLevelUps > 0)
                _pendingQueue.Add(RewardChoiceType.LevelUp);
            else if (_pendingItemChoices > 0)
                _pendingQueue.Add(RewardChoiceType.Item);
            else
            {
                FinishAllChoices();
                return;
            }
        }

        if (!_pendingQueue.TryDequeueNext(out var nextChoice))
        {
            FinishAllChoices();
            return;
        }
        var nextType = nextChoice.type;
        _isShowingItemChoice = (nextType == RewardChoiceType.Item);
        _isShowingEquipmentChoice = (nextType == RewardChoiceType.Equipment);

        RewardDrawReport drawReport = null;
        if (_isShowingItemChoice)
        {
            _pendingItemChoices--;
            _currentChoices = DrawItemChoices();
        }
        else if (_isShowingEquipmentChoice)
        {
            _pendingEquipmentChoices--;
            _currentChoices = DrawEquipmentChoices(nextChoice.circle, out drawReport);
        }
        else
        {
            _pendingLevelUps--;
            _currentChoices = forcedChoices != null && forcedChoices.Count > 0
                ? new List<UpgradeDefinition>(forcedChoices)
                : DrawChoices(out drawReport);
        }

        if (drawReport != null)
            LogDrawReport(drawReport, _isShowingEquipmentChoice ? "装备" : "属性");

        if (_currentChoices == null || _currentChoices.Count == 0)
        {
            // 2026-11-25 用户拍定：零候选 **报错跳过**，不再发铜钱补偿、不伪造候选。
            // 但“候选都已经是当前已装备的完全一致变体”属于**状态性缺货**（isShortage）：
            // 不报配置错误、不整组作废，只记录警告并跳过本次。
            if (!_isShowingItemChoice && (drawReport == null || !drawReport.HasError) && (drawReport == null || !drawReport.isShortage))
                Debug.LogError("[UpgradeChoiceManager] 本次"
                    + (_isShowingEquipmentChoice ? "装备" : "属性")
                    + "没有任何候选，按配置错误跳过本次奖励（不发放补偿）");

            AdvanceAfterChoice();
            return;
        }

        // 动态生成弹窗（不在场景中预置）
        if (_activePopup == null)
        {
            if (popupPrefab == null)
            {
                Debug.LogError("[UpgradeChoiceManager] popupPrefab 未配置 → 本次奖励无法展示，收尾避免卡死");
                AbandonAllPending();
                FinishAllChoices();
                return;
            }
            var go = Instantiate(popupPrefab);
            // 只认端口，不认界面组件类型（2026-10-22 asmdef 分层第五批）。
            _activePopup = go.GetComponent<IUpgradeChoiceView>();
            if (_activePopup == null)
            {
                Debug.LogError("[UpgradeChoiceManager] popupPrefab 上找不到 IUpgradeChoiceView 实现 → 收尾避免卡死");
                AbandonAllPending();
                FinishAllChoices();
                return;
            }
        }

        // 上一次可能停着「下方选旧装备」的名单（异常 / 连发奖励）→ 先收掉再展示新的一组。
        (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();

        // 先告诉弹窗“这是哪一类”，卡片才能按装备（固定品质 / 等级）或属性（当前 → 下一级）渲染。
        NotifyChoiceKind();
        _activePopup.ShowChoices(_currentChoices);
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
        Debug.Log($"[UpgradeChoiceManager] ConfirmChoice frame={Time.frameCount} timeScale={Time.timeScale} upgradeId={selected?.upgradeId} isItem={_isShowingItemChoice} isEquipment={_isShowingEquipmentChoice} pendingLevelUps={_pendingLevelUps} pendingItems={_pendingItemChoices} pendingEquipment={_pendingEquipmentChoices} queue={_pendingQueue.Count}");
        if (!_isChoosing || _currentChoices == null) return;
        if (!_currentChoices.Contains(selected)) return;

        // 正在等下方旧装备时，玩家仍可点上方的其他候选卡 → 收掉旧名单，按新卡重新处理。
        if (_awaitingReplacePick)
        {
            _awaitingReplacePick = false;
            _pendingReplacementVariant = null;
            (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();
        }

        if (_isShowingEquipmentChoice)
        {
            ResolveEquipmentChoice(selected);
            return;
        }

        // 属性 / V1 道具：沿用既有 ApplyUpgrade（属性是“每选一次升一级”的累积语义）
        UpgradeEffectManager.Instance.ApplyUpgrade(selected);
        OnChoiceSelected?.Invoke(selected);
        ApplySelectionEffects(selected);
        AdvanceAfterChoice();
    }

    /// <summary>
    /// **装备选择**（2026-11-25）：
    ///   · 同款已在身上 → 直接替换原槽（不 +1、不继承旧等级）；
    ///   · 空槽 → 直接装备；
    ///   · 满槽且无同款 → 在下方列出同类别旧装备，等玩家点一件替换（不推进队列）。
    /// 失败不当作成功：不触发选择事件、不消费奖励。
    /// </summary>
    private void ResolveEquipmentChoice(UpgradeDefinition selected)
    {
        var result = EquipmentLoadout.TryEquip(selected, out string error);
        if (result == EquipmentLoadout.EquipResult.Equipped)
        {
            OnChoiceSelected?.Invoke(selected);
            ApplySelectionEffects(selected);
            AdvanceAfterChoice();
            return;
        }

        if (result == EquipmentLoadout.EquipResult.NeedsReplacement)
        {
            var prompt = _activePopup as IEquipmentReplacePrompt;
            var candidates = EquipmentLoadout.GetReplacementCandidates(selected);
            if (prompt == null || candidates.Count == 0)
            {
                AbortChoice("装备三选一：需要选择替换槽位，但弹窗不支持替换列表 / 没有可替换装备", error);
                return;
            }
            _awaitingReplacePick = true;
            _pendingReplacementVariant = selected;
            prompt.ShowReplaceTargets(selected, candidates, OnReplacementPicked, OnReplacementSkipped);
            return;
        }

        AbortChoice("装备三选一：装上失败", error);
    }

    /// <summary>玩家在下方点了「跳过」：放弃本次装备奖励，保留原装备，推进队列。</summary>
    private void OnReplacementSkipped()
    {
        if (!_awaitingReplacePick) return;
        _awaitingReplacePick = false;
        _pendingReplacementVariant = null;
        (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();
        Debug.Log("[UpgradeChoiceManager] 装备选择已跳过（保留原装备，不发放补偿）");
        AdvanceAfterChoice();
    }

    /// <summary>玩家在下方点了要换掉的旧装备（仅在 <see cref="_awaitingReplacePick"/> 时生效）。</summary>
    private void OnReplacementPicked(UpgradeDefinition oldDefinition)
    {
        if (!_awaitingReplacePick) return;

        var newVariant = _pendingReplacementVariant;
        _awaitingReplacePick = false;
        _pendingReplacementVariant = null;
        (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();

        if (!EquipmentLoadout.TryReplace(oldDefinition, newVariant, out string error))
        {
            AbortChoice("装备三选一：替换失败", error);
            return;
        }

        OnChoiceSelected?.Invoke(newVariant);
        ApplySelectionEffects(newVariant);
        AdvanceAfterChoice();
    }

    /// <summary>
    /// **本次奖励作废**（配置 / 运行态异常）：不发放、不补偿、不伪造候选，报错后跳过，避免波末卡死。
    /// （用户 2026-11-25 定：同次去重耗尽 / 零候选都按配置错误报出，不得静默降级。）
    /// </summary>
    private void AbortChoice(string context, string error)
    {
        Debug.LogError("[UpgradeChoiceManager] " + context + "：本次奖励作废（不发放、不补偿）"
            + (string.IsNullOrEmpty(error) ? "" : " — " + error));
        _awaitingReplacePick = false;
        _pendingReplacementVariant = null;
        (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();
        AdvanceAfterChoice();
    }

    /// <summary>清空所有排队项与换装中间态（教学吞掉 / 关卡结束 / 弹窗不可用时）。</summary>
    private void AbandonAllPending()
    {
        _pendingLevelUps = 0;
        _pendingItemChoices = 0;
        _pendingEquipmentChoices = 0;
        _pendingQueue.Clear();
        _awaitingReplacePick = false;
        _pendingReplacementVariant = null;
        (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();
    }

    /// <summary>当前这次选完之后：还有排队项就继续，否则收尾（恢复时间 / 关弹窗）。</summary>
    private void AdvanceAfterChoice()
    {
        if (_pendingQueue.Count > 0 || _pendingLevelUps > 0 || _pendingItemChoices > 0 || _pendingEquipmentChoices > 0)
        {
            Debug.Log($"[UpgradeChoiceManager] 还有待处理选择，刷新弹窗 (queue={_pendingQueue.Count} lv={_pendingLevelUps} item={_pendingItemChoices} equip={_pendingEquipmentChoices})");
            ShowNextChoice();
        }
        else
        {
            FinishAllChoices();
        }
    }

    private static void LogDrawReport(RewardDrawReport report, string label)
    {
        if (report == null) return;
        if (report.HasError)
        {
            Debug.LogError(report.Describe("UpgradeChoiceManager/" + label));
            return;
        }
        if (report.warnings.Count > 0)
            Debug.LogWarning(report.Describe("UpgradeChoiceManager/" + label));
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
        _isShowingEquipmentChoice = false;
        _awaitingReplacePick = false;
        _pendingReplacementVariant = null;
        _pendingLevelUps = 0;
        _pendingItemChoices = 0;
        _pendingEquipmentChoices = 0;
        _pendingQueue.Clear();

        (_activePopup as IEquipmentReplacePrompt)?.HideReplaceTargets();

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
    
        /// <summary>
        /// 属性三选一（V2 经验升级）/ V1 常规池。
        ///
        /// V2（2026-11-25）：品质概率**全局统一**、同次按升级内容去重；
        ///   不使用装备的池内档位（<see cref="AttributeChoiceDrawer"/>）。
        /// V1：保持旧口径（除主动技能外全收）。
        /// </summary>
        private List<UpgradeDefinition> DrawChoices(out RewardDrawReport report)
        {
            report = null;
            if (UsesActiveSkillRules)
            {
                report = new RewardDrawReport();
                return DrawAttributeChoices(report);
            }

            if (poolConfig == null)
            {
                Debug.LogWarning("[UpgradeChoiceManager] poolConfig 未配置");
                return new List<UpgradeDefinition>();
            }

            var candidates = new List<UpgradePoolSampler.Candidate>();
            _sampler.CollectRegular(candidates, poolConfig.commonPool, UpgradeRarity.Common);
            _sampler.CollectRegular(candidates, poolConfig.rarePool, UpgradeRarity.Rare);
            _sampler.CollectRegular(candidates, poolConfig.legendaryPool, UpgradeRarity.Legendary);
            return _sampler.DrawByRarity(candidates, EffectiveChoiceCount, RollRarity);
        }

        /// <summary>V2 属性抽取：全局品质权重 + 同次按 kindId 去重。</summary>
        private List<UpgradeDefinition> DrawAttributeChoices(RewardDrawReport report)
        {
            var drawer = new AttributeChoiceDrawer(
                attributePoolConfig,
                GetCurrentLevel,
                def => UpgradeEffectManager.Instance != null
                    ? UpgradeEffectManager.Instance.GetEffectiveMaxLevel(def)
                    : Mathf.Max(1, def != null ? def.maxLevel : 1),
                _sampler.PrerequisitesMet,
                def => _sampler.RequirementsMet(def, UpgradePoolKind.Regular));
            return drawer.Draw(EffectiveChoiceCount, report);
        }

        /// <summary>
        /// **装备三选一**（2026-11-25）：影之武装与饰品**统一一套候选**。
        /// 内部圈数 → 档位概率 → 档内权重 → 同次按种类去重；不保底、不交替、不受槽位 / 同款过滤。
        /// </summary>
        private List<UpgradeDefinition> DrawEquipmentChoices(int circle, out RewardDrawReport report)
        {
            report = new RewardDrawReport();
            if (_circleSource == null && !_circleFallbackLogged)
            {
                _circleFallbackLogged = true;
                Debug.Log("[UpgradeChoiceManager] 没有无尽圈数来源（非无尽关）→ 装备档位按第 1 圈处理");
            }

            var drawer = new EquipmentChoiceDrawer(
                equipmentPoolConfig,
                EquipmentUnlockService.IsUnlocked,
                EquipmentLoadout.IsEquippedExact);
            return drawer.Draw(Mathf.Max(1, circle), EffectiveChoiceCount, report);
        }

        /// <summary>
        /// **发放 N 次装备选择**（2026-11-17：影之武装 / 饰品改为**精英敌人掉落**，不再混在升级三选一里）。
        /// 由 EnemyManager 在精英死亡时调用。
        ///
        /// ⚠️ 只在 V2（主动技能）规则下生效：V1 没有"影之武装"这条线。
        /// </summary>
        public void GrantEquipmentChoices(int count)
            => GrantEquipmentChoices(count, CurrentCircleForDrop);

        /// <summary>
        /// 发放 N 次装备选择，并记录**掉落时**的内部圈数（2026-11-25）。
        /// 跨圈迟到时抽档仍用这个圈数，而不是弹出时的当前圈数。
        /// </summary>
        public void GrantEquipmentChoices(int count, int circle)
        {
            if (!UsesActiveSkillRules) return;
            if (suppressChoices) return;
            if (StageBattleState != null && !StageBattleState.IsStageInProgress) return;
            if (count <= 0) return;
            int stampedCircle = Mathf.Max(1, circle);
            for (int i = 0; i < count; i++)
            {
                _pendingEquipmentChoices++;
                _pendingQueue.Add(RewardChoiceType.Equipment, stampedCircle);
            }
            // ★ **波末统一弹**（2026-11-17 用户定：“波次结束后才选升级成长和武器”）：
            //   装备与升级走同一个波末时机；队列按 **Equipment 优先于 LevelUp** 出牌（2026-11-25）。
            //   不再在精英死亡的那一瞬间当场弹窗打断战斗。
            if (deferChoicesToWaveEnd) return;
            if (!_isChoosing) StartChoiceFlow();
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
    
        /// <summary>V1 常规池的稀有度权重（V2 属性走全局品质权重，不经过这里）。</summary>
        private UpgradeRarity RollRarity()
        {
            if (poolConfig == null) return UpgradeRarity.Common;
            float commonW = poolConfig.commonWeight;
            float rareW = poolConfig.rareWeight;
            float legendaryW = poolConfig.legendaryWeight;
            float total = commonW + rareW + legendaryW;
            float roll = Random.value * total;
            if (roll < commonW) return UpgradeRarity.Common;
            if (roll < commonW + rareW) return UpgradeRarity.Rare;
            return UpgradeRarity.Legendary;
        }
    }
