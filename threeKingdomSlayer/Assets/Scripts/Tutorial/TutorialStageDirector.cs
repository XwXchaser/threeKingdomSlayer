using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 教学关导演（Battle.scene 常驻；不是教学战斗时自动静默）。
///
/// 教学关走正式卷轴路线（FakeRouteRuntime）：节点进入、旅行、奖励等待、整关结算全部由路线系统负责。
/// 本组件只补路线系统管不了的两件事：
/// 1) 按波推台词（走对话数据库的 WaveStart 匹配会漏第一波，实测过）；
/// 2) 教学期升级固定给冰雾（正式游玩才是随机三选一）。
///
/// 这里**不碰蓄力**：上挑（Launch）本身要求蓄力，禁掉蓄力会把上挑一起禁死，
/// 1021 的破盾教学就做不出来；C4 终结技由天赋节点锁着，不需要再禁一次。
/// 玩家阵亡也不改判定：死了就是失败，弹失败面板，按检查点重来。
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    /// <summary>单例（常驻点击特效要判断「教学拍是不是在进行中」）。</summary>
    public static TutorialStageDirector Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        // 注册**世界侧压暗后端**（统一遮罩系统）：
        //   UI 侧由 UiMask 负责，背景侧由 TutorialSceneDim 负责 → 两者由同一条规则发起。
        if (_sceneDim == null) _sceneDim = GetComponent<TutorialSceneDim>();
        if (_sceneDim == null) _sceneDim = gameObject.AddComponent<TutorialSceneDim>();
        if (_sceneDim != null) MaskService.WorldDim = _sceneDim;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        // 导演没了 → 它申请的遮罩与世界压暗都要收
        MaskService.Clear(this);
        if (MaskService.WorldDim == _sceneDim) MaskService.WorldDim = null;
    }
    [Header("教学战斗与台词")]
    [Tooltip("教学 N1（教点戳 / 连点三下）")]
    public StageConfig n1Battle;
    [Tooltip("按波次（0 起）对应的台词")]
    public DialogueEventData[] n1Dialogues = new DialogueEventData[0];
    [Tooltip("教学 E0（1021 破盾、109 冲锋）")]
    public StageConfig e0Battle;
    [Tooltip("按波次（0 起）对应的台词")]
    public DialogueEventData[] e0Dialogues = new DialogueEventData[0];

    [Header("教学开场流程")]
    [Tooltip("教学 N1 各波的**第一段字幕**（0 起）；留空 = 这一波不走开场")]
    public DialogueEventData[] n1IntroDialogues = new DialogueEventData[0];
    [Tooltip("教学 E0 各波的**第一段字幕**（0 起）；留空 = 这一波不走开场")]
    public DialogueEventData[] e0IntroDialogues = new DialogueEventData[0];
    [Tooltip("开场流程的「点击继续」按钮（场景里的实例；自带 Canvas+GraphicRaycaster，点得到）")]
    public TutorialContinueButton continueButton;
    [Tooltip("引导期点击遮罩的宿主层（TutorialIntroOverlay，order=121；挡住战场等，HUD 由 hudCanvasGroup 整块失能）")]
    public RectTransform introMaskHost;
    [Tooltip("**等级球教学引导**（战斗 HUD 左下角；第一波叫起、第一拍开始时收掉）")]
    public LevelOrbGuide levelOrbGuide;
    /// <summary>等级球教学引导是否已经叫起过（只叫一次）。</summary>
    private bool _orbGuideShown;

    [Tooltip("**指向突刺教学**：敌人抡招「抬手到顶」多少秒内冻结（口径同技能打断拍）")]
    public float comboWindupFreezeSeconds = 0.35f;
    [Tooltip("战斗 HUD 根上的 CanvasGroup（引导期整块失能：道具栏 / 技能槽 / 防御键 / 头像都不可点）")]
    public CanvasGroup hudCanvasGroup;
    // ⚠️ 这里**没有**“超时自动跳过”的开关：需求明确「不存在任何计时器直接跳过」。
    //   「点击继续」只能由玩家点击推进；拍做完才退出暂停。

    [Header("教学台词：路线移动")]
    [Tooltip("路线出现出口箭头时播放，教玩家点箭头走到下一个节点")]
    public DialogueEventData choiceDialogue;

    [Header("教学升级")]
    [Tooltip("教学期固定给的这个技能（冰雾）。留空则不给")]
    public UpgradeDefinition tutorialIceSkill;

    [Tooltip("**玩家全程唯一一次三选1** 的候选（节点二内容1 之后、经验升级时弹）。\n" +
             "至少要有一个能打断敌人霸体的技能（旋风 / 海浪）。")]
    public List<UpgradeDefinition> tutorialChoicePool = new List<UpgradeDefinition>();

    [Tooltip("三选1 里**推荐玩家选的那张**（教学会指着它，并念出它的名字）。")]
    public UpgradeDefinition recommendedChoice;

    [Tooltip("教学三选1 **只允许选推荐的那张**：其余候选变灰且不可点。\n" +
             "（教学的内容2 要教「用这个技能打断霸体」，让玩家选到别的就教不下去了）")]
    public bool forceRecommendedChoiceOnly = true;

    [Header("教学台词：技能与三选1")]
    [Tooltip("三选1 弹出时的教学对话（**用对话系统说**，不说另起一块字幕）")]
    public DialogueEventData choiceTeachingDialogue;

    [Tooltip("三选1 弹出时的第一句：先讲清经验→升级→三选1")]
    public string choiceExpLine = "击杀敌人获得经验，经验满了就会升级，升级会弹出三选1奖励。";

    [Tooltip("三选1 弹出时的第二句：让玩家听引导选指定的技能（{0} = 技能名）")]
    public string choicePickLine = "听我的，选【{0}】——它能把霸体敌人推回去，打断他的招。";

    [Tooltip("内容2 用【旋风】时的台词")]
    public DialogueEventData skillDialogueCyclone;

    [Tooltip("内容2 用【海浪】时的台词")]
    public DialogueEventData skillDialogueWave;

    [Tooltip("内容2 用其它技能时的台词（兼底）")]
    public DialogueEventData skillDialogueGeneric;

    [Header("教学路线")]
    [Tooltip("教学路线配置：整关结算时用它标记教学已完成")]
    public FakeRouteStageConfig tutorialRouteConfig;

    [Header("手势提示")]
    [Tooltip("手势提示预制体（Battle.scene 里的实例）")]
    public TutorialGestureHint gestureHint;
    [Tooltip("按「战斗配置 + 波次」匹配的手势提示步骤；waveIndex=-2 表示路线出口（点箭头）那一刻")]
    public TutorialGestureStep[] gestureSteps = new TutorialGestureStep[0];
    [Tooltip("教学台词每行自动停留的秒数（不拦玩家；真正的门槛是手势）")]
    public float tutorialLineSeconds = 2.2f;
    [Tooltip("教学关开幕台词：进入教学路线时播一次")]
    public DialogueEventData openingDialogue;
    [Tooltip("手势引导最多等敌人跑到前排的秒数；超时就直接在敌人当前位置开始")]
    public float hintWaitForEnemySeconds = 8f;

    [Header("教学压暗")]
    [Tooltip("手势拍时场景（背景/路面/植被/墙体）保留的亮度；1 = 不压暗。角色、长枪、敌人始终不压暗")]
    [Range(0.1f, 1f)] public float sceneDimBrightness = 0.42f;

    /// <summary>关卡战斗状态（只读 + 状态事件）。</summary>
    private IStageBattleState _stageState;

    /// <summary>一局流程（战败是否允许重来等开关）。</summary>
    private IRunFlow _stageFlow;
    private WaveSpawner _waveSpawner;
    private bool _choiceDialogueShown;
    /// <summary>教学是否已经教到「防御按键」那一拍（教到之前防御键锁住）。</summary>
    private bool _defendTaught;
    private Coroutine _hintRoutine;
    /// <summary>波次开始的节拍协程（引导拍 → 教学拍）：与教学拍分开管。</summary>
    private Coroutine _waveRoutine;
    /// <summary>路线出口光标（wave=-2）那个协程 —— **与教学拍分开管**，
    /// 免得清理路线光标时把正在跑的教学拍一起 StopCoroutine 掉（2026-11-02 的坑）。</summary>
    private Coroutine _routeHintRoutine;
    private DialogueEventData _pendingWaveDialogue;
    private bool _openingPlayed;
    private bool _routeHintActive;
    private bool _beatActive;
    private bool _introPaused;
    /// <summary>
    /// **玩家在教学三选1 里选中的技能**（内容2 要指着它的槽位教）。
    /// 选完之后才确定，所以是运行时记录的。
    /// </summary>
    private UpgradeDefinition _chosenSkill;
    /// <summary>三选1 是否已经给过（教学全程只给一次）。</summary>
    private bool _choiceGiven;
    /// <summary>当前这一拍的配置（收拍后可能要补播 afterDialogue）。</summary>
    private TutorialGestureStep _currentStep;

    /// <summary>当前正在进行的教学拍（null = 没在教学）。供调试/自动化驱动读取。</summary>
    public TutorialGestureStep CurrentStep => _currentStep;

    /// <summary>这一拍是否正在等玩家动作。</summary>
    public bool BeatActive => _beatActive;

    /// <summary>
    /// 「指着三选一卡片的那只手」是否正在展示（常驻点击特效/手势提示层靠它判断要不要收摊）。
    /// </summary>
    public bool ChoiceHintActive { get; private set; }

    /// <summary>「指着路线出口箭头的那只手」是否正在展示。</summary>
    public bool RouteHintActive => _routeHintActive;
    private TutorialGestureTarget _beatTarget = TutorialGestureTarget.None;
    private TutorialSceneDim _sceneDim;

    private void Start()
    {
        _stageState = StageController.Instance as IStageBattleState;
        _stageFlow = StageController.Instance as IRunFlow;
        _waveSpawner = WaveSpawner.Instance;
        if (_waveSpawner != null) _waveSpawner.OnWaveStarted += OnWaveStarted;
        var choiceMgr = UpgradeChoiceManager.Instance;
        if (choiceMgr != null)
        {
            choiceMgr.OnChoicesReady += OnChoicesReady;
            choiceMgr.OnChoiceSelected += OnChoiceSelected;
        }
        if (_stageState != null) _stageState.StageVictory += OnStageVictory;
    }

    private void OnDisable()
    {
        // 【诊断】导演被禁用 → 教学拍会被收掉；把调用方写进日志
        Debug.LogWarning("[Tutorial] OnDisable：教学导演被禁用 → 收拍\n" + System.Environment.StackTrace);

        // ⚠️ **顺序很重要**：先回收全局/静态状态，最后才收拍。
        //   收拍会碰场景对象（提示层 / 对话层）—— 场景重载时它们可能**已经被销毁**，
        //   一旦抛异常，后面的清理就全被跳过 → 全局状态漏到主菜单 / 正式关。
        //   （2026-11-05 实测：教学关退出回主菜单 → Battle 场景整场景重载 →
        //     提示层先被销毁 → EndBeat → gestureHint.Hide() → MissingReferenceException →
        //     OnDisable 后半段（招式白名单 / 三选一开关 / 输入闸门）全没跑。）
        SafeStep("回收全局状态", ReleaseGlobalTutorialState);
        SafeStep("退订事件", UnsubscribeEvents);
        SafeStep("收拍", StopHint);
    }

    /// <summary>
    /// **教学占用过的全局 / 静态状态** —— 全部在这里回收（**唯一出口**）。
    ///
    /// 为什么必须集中成一处：
    ///   这些状态**活得比场景长**（static / 单例），漏回收就会跟着玩家进主菜单和正式关：
    ///   • <see cref="TutorialAttackGate"/>（static 白名单）→ 正式关里玩家放不出没教过的招式
    ///   • <c>GameplayInputGate.AllowSkillActivationWhileBlocked</c> / <c>...WhileFrozen</c>（static）
    ///   • <c>DialogueManager.AllowDialogueWhilePaused</c>（static）
    ///   • <c>InputManager.tutorialGestureTarget</c> / <c>allowInputWhileTimeFrozen</c>
    ///   • <c>Time.timeScale</c>（冻结态 / 登场台词暂停态退出时可能还是 0 → 主菜单被冻住）
    ///
    /// 幂等：可重复调用（OnDisable 与 OnDestroy 都会调）。
    /// </summary>
    private void ReleaseGlobalTutorialState()
    {
        // ① 招式白名单（static）—— 不清的话正式关里玩家放不出没教过的招式
        TutorialAttackGate.Clear();
        _attackGateOn = false;
        _taughtAttacks.Clear();

        // ② 技能拍 / 冻结 / 对话层的 static 开关
        ResetSkillBeatFlags();

        // ③ 输入闸门（手势过滤 + 冻结时仍吃输入）
        var input = InputManager.Instance;
        if (input != null)
        {
            input.tutorialGestureTarget = TutorialGestureTarget.None;
            input.allowInputWhileTimeFrozen = false;
        }

        // ④ 教学专用的三选一开关（强制池）
        var choices = UpgradeChoiceManager.Instance;
        if (choices != null)
        {
            choices.suppressChoices = false;
            choices.forcedChoices.Clear();
        }

        // ⑤ 时间缩放：**只恢复自己冻的**（别人的暂停不归我管）
        if (_frozenByTutorial || _introPaused) Time.timeScale = 1f;
        _frozenByTutorial = false;
        _introPaused = false;

        // ⑥ 输入闸门：开场流程（TutorialIntro）持有的那份锁必须还回去。
        //   ⚠️ owner 规则只在**对象被销毁**时自动回收；导演被「禁用但没销毁」时
        //   那份锁会留着 → 玩家进主菜单/正式关后点不动 → 所以这里显式 Release。
        GameplayInputGate.Release(this);
        IntroActive = false;
        // 开场流程的「点击继续」按钮也是场景对象：导演被禁用时它会留在屏上 → 收掉
        if (continueButton != null) continueButton.Hide();
        // 教学遮罩（点击遮罩 + 背景压暗）也要收 → 否则离开教学后画面还暗着 / 点击还被挡
        MaskService.Clear(this);
    }

    /// <summary>退订（可重复调用，重复退订无害）。</summary>
    private void UnsubscribeEvents()
    {
        if (_waveSpawner != null) _waveSpawner.OnWaveStarted -= OnWaveStarted;
        var choiceMgr = UpgradeChoiceManager.Instance;
        if (choiceMgr != null)
        {
            choiceMgr.OnChoicesReady -= OnChoicesReady;
            choiceMgr.OnChoiceSelected -= OnChoiceSelected;
        }
        if (_stageState != null) _stageState.StageVictory -= OnStageVictory;
    }

    /// <summary>
    /// 清理步骤包一层 try/catch：**任何一步抛异常都不能挡住后面的步骤**。
    /// 清理的职责就是「把状态还回去」—— 一步失败就意味着有状态漏了，
    /// 而漏掉的状态会跟着玩家进主菜单 / 正式关（比报错更难查）。
    /// </summary>
    private static void SafeStep(string label, System.Action action)
    {
        try { action(); }
        catch (System.Exception e)
        {
            Debug.LogError("[Tutorial] 清理步骤失败（" + label + "）：" + e);
        }
    }

    /// <summary>
    /// 安全地收起手势提示层。
    ///
    /// ⚠️ **不能用 <c>gestureHint?.Hide()</c>**：<c>?.</c> 做的是**引用 null** 判断，
    /// 而 Unity 的「假 null」（对象已被 Destroy、引用还在）它挡不住 →
    /// 场景重载时提示层先被销毁，这里照样调用 → 内部 <c>StopCoroutine</c> 抛
    /// MissingReferenceException → 异常顺着 OnDisable 往上冒 → 后面的清理全被跳过
    /// （2026-11-05 实测：教学关退出回主菜单时必崩）。
    /// </summary>
    private void SafeHideGestureHint()
    {
        // Unity 语义：对象已销毁时 `== null` 为 true
        if (gestureHint == null) return;
        try { gestureHint.Hide(); }
        catch (System.Exception e) { Debug.LogError("[Tutorial] 收起手势提示层失败：" + e.Message); }
    }

    /// <summary>
    /// **不在教学路线上时**，把**教学专用**的三选一开关还原（forcedChoices / suppressChoices）。
    ///
    /// 背景：这两个开关原本只在 <c>OnDisable</c> 里清。但教学关 → 正式关是**同一场景内切节点**，
    /// Director 不会被禁用 / 销毁 → <c>OnDisable</c> 不跑 → 教学用的强制三选一池
    /// （旋风 / 海浪）**残留到正式关**，把正式关的三选一锁死。
    ///
    /// 判定用「当前路线 != 教学路线」，与 <see cref="RefreshDefenseButtonAvailability"/> 同一口径；
    /// 路线未就绪（route == null）时**不动**，避免教学刚开始就误清。
    /// </summary>
    private void ReleaseTutorialOnlyChoicesIfOffTutorialRoute()
    {
        var choices = UpgradeChoiceManager.Instance;
        if (choices == null) return;

        var route = RouteFlowHub.Current;
        bool definitelyOffTutorial = route != null && tutorialRouteConfig != null
                                    && route.RouteConfig != tutorialRouteConfig;
        if (!definitelyOffTutorial) return;

        bool hadForced = choices.forcedChoices != null && choices.forcedChoices.Count > 0;
        bool suppressed = choices.suppressChoices;
        if (!hadForced && !suppressed) return;

        choices.suppressChoices = false;
        choices.forcedChoices.Clear();
        Debug.Log("[Tutorial] 已离开教学路线 → 还原教学专用三选一开关（forcedChoices 已清空 / suppressChoices=false）");
    }

    /// <summary>
    /// **不在教学路线上时**，把**教学专用的防御键静音**还原（压暗 + 按下无效）。
    ///
    /// 背景（2026-11-05 用户反馈「正式关3选1选择后无法按防御按键」）：
    ///   静音是教学关「3选1 → 技能拍」那段用的，但**正式关也会弹3选1** → 一旦被静音，
    ///   正式关里没人会解除（技能拍只在教学关跑）→ 防御键整局都是死的。
    ///
    /// 所以除了调用点判路线，这里再挂一道**每帧兜底**：只要不在教学路线上，静音一律还原。
    /// （与 <see cref="ReleaseTutorialOnlyChoicesIfOffTutorialRoute"/> 同一口径：路由未就绪时不动。）
    /// </summary>
    /// <summary>
    /// 路线选择期（等玩家选路牌）的教学锁定：**战斗 HUD 整块失能**。
    ///
    /// 需求（2026-11-06 用户）：「非战斗的教学部分……依然可以点击其他 UI，没有强制玩家跟教学走」
    /// → 这段时间只允许点**路牌按钮**（幕布不拦射线）+ 左上角**暂停键**
    /// （暂停层自己挂了 `ignoreParentGroups` 的 CanvasGroup → 不受 HUD 失能影响；
    ///   路牌面板同理，自己挂了 `ignoreParentGroups` → 不会被 HUD 失能连带）。
    /// </summary>
    private void UpdateRouteTeachingLock()
    {
        var route = RouteFlowHub.Current as FakeRouteRuntime;
        bool shouldLock = route != null && route.Phase == FakeRoutePhase.ChoosingRoute && IsOnTutorialRoute();
        if (shouldLock == _routeTeachingLocked) return;
        _routeTeachingLocked = shouldLock;

        if (shouldLock)
        {
            if (hudCanvasGroup != null)
            {
                _routeHudInteractableBackup = hudCanvasGroup.interactable;
                _routeHudBlocksRaycastsBackup = hudCanvasGroup.blocksRaycasts;
                hudCanvasGroup.interactable = false;
                hudCanvasGroup.blocksRaycasts = false;
            }
            Debug.Log("[Tutorial] 路线选择教学：HUD 已锁定（只能点路牌 / 暂停键）");
        }
        else if (hudCanvasGroup != null)
        {
            hudCanvasGroup.interactable = _routeHudInteractableBackup;
            hudCanvasGroup.blocksRaycasts = _routeHudBlocksRaycastsBackup;
        }
    }

    private bool _routeTeachingLocked;
    private bool _routeHudInteractableBackup = true;
    private bool _routeHudBlocksRaycastsBackup = true;

    private void ReleaseTutorialOnlyDefenseMuteIfOffTutorialRoute()
    {
        if (!_defenseMutedByChoice) return;
        var route = RouteFlowHub.Current;
        bool definitelyOffTutorial = route != null && tutorialRouteConfig != null
                                    && route.RouteConfig != tutorialRouteConfig;
        if (!definitelyOffTutorial) return;
        SetDefenseMuteChoice(false);
        Debug.Log("[Tutorial] 已离开教学路线 → 还原防御键（取消静音）");
    }

    /// <summary>
    /// 防御键的开放时机：
    ///   • 教学关：**教到那一拍之前锁住**（锁住 = 不可见 + 不吃射线 → 既不会被点到，也不吞手势）；
    ///   • 正式关 / 教学已完成：一直开放。
    /// </summary>
    private void RefreshDefenseButtonAvailability()
    {
        var button = DefenseButton.Instance;
        if (button == null) return;

        // 教学路线里：**教到防御之前一律锁住**（看不见也点不到）。
        // 不看存档的 tutorialCompleted —— 就算之前通关过，只要这一局还在教学路线上，
        // 没教到就不给看（2026-11-01 用户反馈：「还没教格挡你给格挡按键干嘛」）。
        bool shouldLock = false;
        if (!_defendTaught)
        {
            var route = RouteFlowHub.Current;
            bool onTutorialRoute = route != null && tutorialRouteConfig != null && route.RouteConfig == tutorialRouteConfig;
            shouldLock = onTutorialRoute;
        }

        if (button.Available == shouldLock) button.SetAvailable(!shouldLock);
    }

    /// <summary>
    /// 教学拍在存档里的**稳定 id**（形态 <c>kind@waveIndex</c>）。
    /// 引导拍（招式解锁）另有存档位（<c>MoveGuideSaveSection.taughtMoveGuides</c>）→ 这里返回 null。
    /// </summary>
    private static string BeatSaveId(TutorialGestureStep step)
    {
        if (step == null) return null;
        if (!string.IsNullOrEmpty(step.guideNodeId)) return null;
        return step.kind + "@" + step.waveIndex;
    }

    /// <summary>这一拍是否**已经教过**（存档标记）。</summary>
    private static bool IsBeatAlreadyTaught(TutorialGestureStep step)
    {
        string id = BeatSaveId(step);
        if (string.IsNullOrEmpty(id)) return false;
        return TutorialSaveSection.IsBeatTaught(SaveManager.Load(), id);
    }

    /// <summary>
    /// 把「这一拍教出来的东西」记到当前状态（跳过已教过的拍时用来补齐）。
    /// </summary>
    private void ApplyBeatOutcome(TutorialGestureStep step)
    {
        if (step == null) return;
        var teach = TeachAttackFor(step);
        if (teach.HasValue && _taughtAttacks.Add(teach.Value)) ApplyAttackGate(null);
        if (step.kind == GestureHintKind.DefendButton) _defendTaught = true;
    }

    /// <summary>
    /// 这一拍真的教完了 → **写存档标记**（每教完一拍就记，不看教学关通不通）。
    /// </summary>
    private void MarkTutorialBeatTaughtIfNeeded()
    {
        MarkTutorialBeatTaughtById(BeatSaveId(_currentStep));
    }

    /// <summary>按 id 写入「已教过」标记。</summary>
    private void MarkTutorialBeatTaughtById(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        var data = SaveManager.Load();
        if (!TutorialSaveSection.MarkBeatTaught(data, id)) return;
        SaveManager.Save(data);
        Debug.Log($"[Tutorial] 已教完「{id}」→ 写入存档（下次不再教）");
    }

    /// <summary>3选1 教学的存档 id（它不是手势拍，单独一个口径）。</summary>
    private const string ChoiceTeachId = "Choice3@0";

    /// <summary>3选1 教学是否**已经教过**（存档标记）。</summary>
    private static bool IsChoiceTeachDone()
    {
        return TutorialSaveSection.IsBeatTaught(SaveManager.Load(), ChoiceTeachId);
    }

    /// <summary>教学是否已完成（缓存：只读一次，通关时置 true）。</summary>
    private bool? _tutorialCompletedCache;
    private bool TutorialCompleted
    {
        get
        {
            if (_tutorialCompletedCache == null)
            {
                var data = SaveManager.Load();
                _tutorialCompletedCache = data != null && TutorialSaveSection.IsCompleted(data);
            }
            return _tutorialCompletedCache.Value;
        }
    }

    /// <summary>路线出口（箭头）出现时推一条教学台词：没人告诉过玩家点箭头才能走。</summary>
    private void Update()
    {
        // 教学关才限制招式（还没教到的不许发出）
        UpdateAttackGate();

        // 防御键：教学关在教到之前**不给用** ——
        // 它是个可交互 UI，按输入规则（IsPointerOverUIAt：命中可交互 UI → 这次输入不算手势）
        // 会把玩家点敌人的手势吞掉 → 开局的点击提示点过去就落在按钮上。
        RefreshDefenseButtonAvailability();

        // 【教学专用状态复位】离开教学路线后必须还原「教学专用的三选一」开关。
        // forcedChoices / suppressChoices 是教学关强制的三选一池（旋风 / 海浪），
        // 原本只在 OnDisable 里清 —— 但教学关 → 正式关是**同一场景内切节点**，
        // Director 不会 OnDisable → 这两个开关**残留到正式关**，
        // 于是正式关的三选一被锁死在教学池里（表现为「到该弹的时候选不了 / 卡住」）。
        ReleaseTutorialOnlyChoicesIfOffTutorialRoute();
        ReleaseTutorialOnlyDefenseMuteIfOffTutorialRoute();

        // 路线选择期（等玩家选路牌）的教学锁定：其它 UI 不可点 → 强制玩家跟教学走。
        UpdateRouteTeachingLock();

        // 教学**整条路线**跑完才算完成（置位时机错了 → 玩家会在主菜单被直接放进正式关）。
        UpdateTutorialRouteCompletion();

        // 开幕台词：教学路线一就绪就播一次
        if (!_openingPlayed && openingDialogue != null && tutorialRouteConfig != null)
        {
            var route = RouteFlowHub.Current;
            // 只在**真正进关卡**时播（2026-10-26：原来只要路线不是 None 就播 → 落地页（Ready）也播，
            // 于是玩家还没点「开始关卡」就被一句阻塞台词 + 遮罩糊在脸上）
            if (route != null && route.RouteConfig == tutorialRouteConfig &&
                (route.Phase == FakeRoutePhase.EnteringNode || route.Phase == FakeRoutePhase.Battle))
            {
                _openingPlayed = true;
                // 教学关战败不重来：只保留回主菜单（否则要把整个教学流程再跑一遍）
                if (_stageFlow != null) _stageFlow.BlockRestartOnDefeat = true;
                DialogueManager.Instance?.QueueEventAndNotify(openingDialogue, forceReplay: true);
                DebugLog.Info("[Tutorial] 开幕台词");
            }
        }

        var routeRuntime = RouteFlowHub.Current;
        bool onTutorialRoute = routeRuntime != null && tutorialRouteConfig != null && routeRuntime.RouteConfig == tutorialRouteConfig;

        // 出口光标：选完路（或离开选择阶段/离开教学路线）就收起来。必须放在下面的「已推过」早退之前。
        if (_routeHintActive && (!onTutorialRoute || routeRuntime.Phase != FakeRoutePhase.ChoosingRoute))
        {
            // ⚠️ 这里**只能收路线光标自己**，不能调 StopHint()：
            //    StopHint() 会把「当前正在跑的教学拍」一起 StopCoroutine 掉
            //    （2026-11-02：用户那次「用技能打断」那一拍在开拍的同一帧被这里掐掉
            //      → 冻结代码根本没执行 → 玩家站着挨打、技能教学完全不发生）。
            _routeHintActive = false;
            if (_routeHintRoutine != null)
            {
                StopCoroutine(_routeHintRoutine);
                _routeHintRoutine = null;
                if (_hintRoutine == null) _hintRoutine = null;
            }
            // 路线台词没读完也要收：它虽不暂停，但会挡住路线推进（Blocking 认对话）
            var liveView = ResolveDialogueView();
            if (liveView != null && liveView.IsShowing) liveView.Skip();
        }

        // ── 手势提示防残留（2026-11-02）────────────────────────────
        // 循环示范（连点 / 按住防御 / 招架来回划）是**无限协程**：只要有一条路径没走到收尾，
        // 它就会一直画下去 —— 表现为「拍结束了光圈还在」「教学关的残留跟着流进正式关」。
        // 这里用两条兜底规则把提示层收干净：
        //   ① 三选一弹窗已经关了 → 收掉指着卡片的那只手；
        //   ② 不在教学路线上（落地页 / 正式关）→ 一律收掉。
        if (ChoiceHintActive)
        {
            var choiceMgr = UpgradeChoiceManager.Instance;
            if (choiceMgr == null || !choiceMgr.IsChoosing)
            {
                ChoiceHintActive = false;
                SafeHideGestureHint();
            }
        }
        // ⚠️ ② 必须排除「**正在进行的教学拍**」（2026-11-04）：
        //   「招式解锁引导拍」也在正式关跑 → 这条规则会把它的示范**每帧杀掉** →
        //   示范只闪 1 帧（实测每 ~5.6s 闪一次）→ 玩家根本看不到手势。
        if (!onTutorialRoute && !_beatActive && gestureHint != null && gestureHint.IsShowing)
        {
            gestureHint.Hide();
        }

        if (_choiceDialogueShown || choiceDialogue == null || !onTutorialRoute) return;
        if (routeRuntime.Phase != FakeRoutePhase.ChoosingRoute) return;
        _choiceDialogueShown = true;

        // 出口这一拍不暂停：先把上一波还没读完的台词收掉，再推不拦人的路线台词
        var activeView = ResolveDialogueView();
        if (activeView != null && activeView.IsShowing) activeView.Skip();
        DialogueManager.Instance?.QueueEventAndNotify(choiceDialogue, forceReplay: true);
        StartHint(-2);
    }

    /// <summary>
    /// 三选1 弹出来了 → 补一句教学台词（“这里可以选一个技能”）。
    ///
    /// 三选1 本身是**经验升级**给的（击杀节点二第一波敌人 → 升级 → 三选1），
    /// 这里只负责把「怎么用」讲清楚，不负责发奖。
    /// </summary>
    private void OnChoicesReady(System.Collections.Generic.List<UpgradeDefinition> choices)
    {
        DebugLog.Info("[Tutorial] 三选1 回调：候选 " + (choices != null ? choices.Count : 0) + "，_choiceGiven=" + _choiceGiven);
        // **正式关不教**（2026-11-04 用户定：「你见过教两次同样操作的教学吗？」）。
        // 三选1 教学只属于教学关：正式关升级弹三选1 时 —— 不念台词、不指卡片、
        // 也不「只让玩家选推荐的那张」（那是教学关的规则，不能带进正式关）。
        if (!IsOnTutorialRoute())
        {
            DebugLog.Info("[Tutorial] 不在教学路线 → 三选1 不教");
            return;
        }
        if (_choiceGiven) return;
        if (choices == null || choices.Count == 0) return;
        _choiceGiven = true;

        // **3选1 教学已经教过（存档标记）→ 不再教**（2026-11-05 用户口径：每教完一个教程就标记完成）。
        //   ⚠️ 只是**不教**（不念台词、不指卡片、不限制只能选推荐的那张），
        //      三选一本身**照常弹** —— 玩家要拿到技能。
        if (IsChoiceTeachDone())
        {
            Debug.Log("[Tutorial] 3选1 教学已教过（存档标记）→ 不再教（但仍然正常弹三选1）");
            return;
        }
        DebugLog.Info("[Tutorial] 三选1 弹出（" + choices.Count + " 个候选）→ 教学台词");

        // 引导选哪个：提示手落在**推荐的那张卡**上（纯视觉，不拦点击）
        var recommended = recommendedChoice != null && choices.Contains(recommendedChoice)
            ? recommendedChoice
            : (tutorialChoicePool != null && tutorialChoicePool.Count > 0 ? tutorialChoicePool[0] : null);

        // 教学：**只让玩家选推荐的那张** —— 其余候选变灰 + 不可点
        if (forceRecommendedChoiceOnly && recommended != null)
        {
            var mgr0 = UpgradeChoiceManager.Instance;
            var popup0 = mgr0 != null ? mgr0.ActivePopup as UpgradeChoicePopup : null;
            if (popup0 != null)
            {
                for (int i = 0; i < choices.Count; i++)
                {
                    if (choices[i] == null) continue;
                    popup0.SetChoiceSelectable(choices[i], choices[i] == recommended);
                }
            }
        }

        if (recommended != null) ShowChoiceHint(recommended);

        // **3选1弹窗期间：防御键静音**（2026-11-05 用户定）。
        // 弹窗一打开就静音 → 玩家在这段里按防御键不会举枪，
        // 也就不会把技能槽点不动 / 长枪卡在空中（2026-11-01 那个老问题）。
        SetDefenseMuteChoice(true);

        // 教学对话：**用对话系统**说「选哪个」（三选一弹窗会把游戏暂停 →
        // 对话默认被“被别的 UI 挡住”拦下不播，所以这一拍开个口）。
        if (choiceTeachingDialogue != null)
        {
            DialogueManager.AllowDialogueWhilePaused = true;
            DialogueManager.Instance?.QueueEventAndNotify(choiceTeachingDialogue, forceReplay: true);
        }
    }

    /// <summary>
    /// 当前是否在**教学路线**上（教学关专属的行为都要过这一关）。
    /// 教学导演是常驻组件（挂在 Battle.scene）→ 正式关里它也在，
    /// 所以教学行为必须自己判路线，不能靠「只跑一次」的标志位。
    /// </summary>
    private bool IsOnTutorialRoute()
    {
        var routeRuntime = RouteFlowHub.Current as FakeRouteRuntime;
        return routeRuntime != null && tutorialRouteConfig != null
               && routeRuntime.RouteConfig == tutorialRouteConfig;
    }

    /// <summary>把提示手落在三选一的推荐卡上。</summary>
    private void ShowChoiceHint(UpgradeDefinition recommended)
    {
        var mgr = UpgradeChoiceManager.Instance;
        if (mgr == null || gestureHint == null) return;
        var popup = mgr.ActivePopup as UpgradeChoicePopup;
        if (popup == null) return;
        if (!popup.TryGetChoiceScreenPoint(recommended, out var pos)) return;
        DebugLog.Info("[Tutorial] 三选一引导：指向 " + recommended.displayName);
        ChoiceHintActive = true;
        // 2026-11-05：三选一改成**长按选择** → 提示也必须是“按住”，而不是“点一下”
        gestureHint.PlayPressHoldLoop(pos);
    }

    /// <summary>
    /// 玩家选完了 → 记下选的是哪个技能（内容2 要指着它的槽位教）。
    /// </summary>
    /// <summary>
    /// 教学期间**暂时静音**防御键（可见但压暗、按下去没反应）。
    ///
    /// 用在哪两处（2026-11-05 用户定）：
    ///   ① **3选1弹窗 → 玩家点技能按钮**这段：弹窗一打开就静音。
    ///      （旧实现是“选完之后”才锁 → **弹窗期间防御键是开的** →
    ///        玩家这时按防御键 → 举枪 → 技能槽点不动 / 长枪卡在空中 —— 2026-11-01 那个老问题）
    ///   ② **招式解锁引导拍**（指向突刺的连招蓄力）：那一拍玩家在蓄力，
    ///      按防御键会抢走持枪姿态 → 这一拍收不了。
    ///
    /// ⚠️ 与「教到格挡之前锁住」是**两件不同的事**：
    ///    那个是**隐藏**（<c>SetAvailable(false)</c>，2026-10-01 用户定）；
    ///    这个是**压暗可见但无效**（<c>SetMuted(true)</c>）。
    ///    → 所以静音的恢复**不依赖** <c>_defendTaught</c>（它在正式关一直是 false）。
    /// </summary>
    /// <summary>
    /// 防御键的**静音**分两个来源（可同时存在），合并后一起应用：
    ///   • <see cref="_defenseMutedByChoice"/> = 教学关「3选1 → 技能拍」这段（**教学专用**，离开教学路线必须还原）
    ///   • <see cref="_defenseMutedByBeat"/>  = **需要出招的教学拍**（点戳 / 连点三下 / 连招蓄力）
    ///     —— 任何关卡都可能有（教学关的拍 + 招式解锁引导拍）→ 拍结束即恢复
    /// </summary>
    private void ApplyDefenseMute()
    {
        var button = DefenseButton.Instance;
        bool muted = _defenseMutedByChoice || _defenseMutedByBeat;
        // **需要出招的教学拍**：静音但**不吃射线** → 玩家点在防御键那一块也能出招
        //   （否则这一点被按钮吞掉 → 招式打不出来 → 这一拍永远收不了）。
        // **三选一面板那段**：仍然吃射线（玩家点防御键不该顺手打出一记攻击）。
        bool swallowClicks = !_defenseMutedByBeat || _defenseMutedByChoice;
        if (button != null) button.SetMuted(muted, swallowClicks);
        if (muted)
        {
            var ps0 = PlayerState.Instance;
            if (ps0 != null) ps0.SetDefend(false);      // 长枪收回去（别卡在空中）
        }
    }

    /// <summary>教学关「3选1 → 技能拍」这段的静音（教学专用）。</summary>
    private void SetDefenseMuteChoice(bool muted)
    {
        _defenseMutedByChoice = muted;
        ApplyDefenseMute();
    }

    /// <summary>需要出招的教学拍期间的静音（任何关卡）。</summary>
    private void SetDefenseMuteBeat(bool muted)
    {
        _defenseMutedByBeat = muted;
        ApplyDefenseMute();
    }

    /// <summary>
    /// **这一拍需要玩家出招**（点戳 / 连点三下 / 连招蓄力）。
    ///
    /// 这类拍期间**必须静音防御键**：`AttackSystem` 有硬闸「防御中，不能出招」
    /// → 玩家按住防御 → 招式打不出来 → **这一拍永远收不了 → 教程卡死**（2026-11-05 用户反馈）。
    ///
    /// 例外（不静音）：
    ///   • <see cref="GestureHintKind.DefendButton"/> = 这一拍就是教玩家按防御键 → 必须可用
    ///   • <see cref="GestureHintKind.SkillSlot"/> = 玩家点技能槽，不靠出招；
    ///     而且等敌人抡招时玩家要能防御（2026-11-02 反馈「为什么我会受击」）
    /// </summary>
    private static bool BeatNeedsAttack(GestureHintKind kind)
    {
        return kind == GestureHintKind.Tap
            || kind == GestureHintKind.TapCombo
            || kind == GestureHintKind.ComboCharge;
    }

    /// <summary>教学关「3选1 → 技能拍」这段的静音（教学专用，离开教学路线会自愈）。</summary>
    private bool _defenseMutedByChoice;

    /// <summary>需要出招的教学拍期间的防御键静音（任何关卡都可能有，拍结束即恢复）。</summary>
    private bool _defenseMutedByBeat;

    /// <summary>内容2 的技能拍是否正在进行（那一拍允许在输入被拦下时也能释放技能）。</summary>
    public bool SkillBeatActive { get; private set; }

    private void OnChoiceSelected(UpgradeDefinition selected)
    {
        if (selected == null) return;
        _chosenSkill = selected;
        DebugLog.Info("[Tutorial] 玩家选中技能：" + selected.displayName);
        // 选完了 → 收掉指着卡片的提示手
        ChoiceHintActive = false;
        SafeHideGestureHint();
        DialogueManager.AllowDialogueWhilePaused = false;
        // 三选1 → 内容2 技能拍 之间**继续静音**防御键（弹窗阶段的静音在这里延续，
        // 到技能拍开拍时（Beats.cs）才恢复 —— 那几秒玩家要能防御，见 2026-11-02 反馈「为什么我会受击」）。
        // 三选1 → 内容2 技能拍 之间**锁住防御键**：
        // 否则玩家在这个空档按下防御 → 长枪举起来（又收不回），同时技能键被输入拦截点不动。
        //
        // ⚠️ **必须判路线**（2026-11-05 用户反馈「正式关3选1选择后无法按防御按键」）：
        //   这个静音是**教学专用**（教学关的3选1 → 技能拍那段），而正式关也会弹3选1 →
        //   不判路线就会把正式关的防御键也静音，而且**永远没人解除**（技能拍只在教学关跑）。
        if (IsOnTutorialRoute()) SetDefenseMuteChoice(true);

        // 玩家**真的选了** → 3选1 教学算教完 → 写存档标记（下次不再教这一段）
        MarkTutorialBeatTaughtById(ChoiceTeachId);
    }

    /// <summary>玩家在教学三选1 里选中的技能（没选 = null）。</summary>
    public UpgradeDefinition ChosenSkill => _chosenSkill;

    /// <summary>内容2 的台词：按玩家选中的技能给对应的一条。</summary>
    public DialogueEventData SkillDialogueFor(UpgradeDefinition skill)
    {
        if (skill == null) return skillDialogueGeneric;
        if (skill.upgradeId == "active_cyclone") return skillDialogueCyclone != null ? skillDialogueCyclone : skillDialogueGeneric;
        if (skill.upgradeId == "active_wave") return skillDialogueWave != null ? skillDialogueWave : skillDialogueGeneric;
        return skillDialogueGeneric;
    }

    private void OnWaveStarted(int waveIndex)
    {
        Debug.Log($"[Tutorial] 波次开始 wave={waveIndex} stage={(_stageState != null && _stageState.StageConfig != null ? _stageState.StageConfig.name : "NULL")} hint={(gestureHint != null)} steps={(gestureSteps != null ? gestureSteps.Length : -1)}");
        // 收掉上一波的拍（原来是在 StartHint 里做的）
        StopHint();
        _waveRoutine = StartCoroutine(OnWaveStartedRoutine(waveIndex));
    }

    /// <summary>
    /// 波次开始的节拍：**先播招式解锁引导拍**（与关卡无关），再播教学关自己的拍。
    /// </summary>
    private System.Collections.IEnumerator OnWaveStartedRoutine(int waveIndex)
    {
        if (_stageState == null || _stageState.StageConfig == null) yield break;
        var config = _stageState.StageConfig;
        var dialogues = config == n1Battle ? n1Dialogues : (config == e0Battle ? e0Dialogues : null);
        bool isTutorialBattle = dialogues != null;

        if (isTutorialBattle)
        {
            // 一次教学流程的开始（教学 N1 的第一波）→ 已教招式作废，从头学
            if (config == n1Battle && waveIndex == 0)
            {
                _taughtAttacks.Clear();
                ApplyAttackGate(null);
                _choiceGiven = false;
                _chosenSkill = null;
                // ★ **等级球教学引导**（2026-11-07 用户需求）：告诉玩家等级球在哪、显示什么信息。
                //   这里才叫起：**战斗 HUD（等级球）已经存在**（落地页没有 HUD，叫了也看不见）；
                //   → 第一拍开始时收掉（见 TutorialStageDirector.Hints.cs）。
                if (!_orbGuideShown)
                {
                    _orbGuideShown = true;
                    if (levelOrbGuide != null) levelOrbGuide.Show();
                }
            }
            Debug.Log("[Tutorial] 波次开始 wave=" + waveIndex + " 招式白名单=" + TutorialAttackGate.Describe());

            // 新一波开始先把上一波没读完的台词收掉：否则手势门槛会用上一波的文字暂停在那里
            var staleView = ResolveDialogueView();
            if (staleView != null && staleView.IsShowing) staleView.Skip();

            ApplyTutorialRules(config);

            // 台词不在这里推：入场期间 timeScale=0 会让敌人走不动，也看不到。
            // 交给 HintRoutine，等敌人站定后再推，这样台词和手势引导同时出现。
            _pendingWaveDialogue = (waveIndex >= 0 && waveIndex < dialogues.Length) ? dialogues[waveIndex] : null;
        }

        // 招式解锁引导拍：与关卡节点无关（正式关也会播），排在教学关自己的拍**前面**。
        var guideSteps = BuildMoveGuideSteps(config, waveIndex);
        for (int i = 0; i < guideSteps.Count; i++)
        {
            var pendingDialogue = _pendingWaveDialogue;
            _pendingWaveDialogue = null;      // 引导拍不推本波台词（那是教学拍自己的）
            _currentStep = guideSteps[i];
            // 「光圈常驻」是**连点拍**的规则（StartHint 里按拍型设的）；引导拍不走 StartHint
            // → 这里要自己复位，否则上一拍残留的 true 会让引导拍也留个光圈。
            if (gestureHint != null) gestureHint.keepRingVisible = false;
            yield return HintRoutine(guideSteps[i]);
            _pendingWaveDialogue = pendingDialogue;
        }

        if (!isTutorialBattle) yield break;   // 不是教学战斗 → 没有自己的拍
        StartHint(waveIndex, stopFirst: false);
    }
}

public enum GestureHintKind
{
    /// <summary>点一下</summary>
    Tap,
    /// <summary>按住蓄力（光圈收缩）</summary>
    Hold,
    /// <summary>普通滑动</summary>
    Swipe,
    /// <summary>按住蓄力后滑出去（蓄力上挑）</summary>
    ChargedSwipe,
    /// <summary>不教手势：这一波只播登场台词</summary>
    None,
    /// <summary>竖直上划（招架）：**时停拍** —— 冻结整个游戏，玩家在冻结中做手势</summary>
    Parry,
    /// <summary>上划后按住（防御）：上划一次并保持按住 → 落防御位</summary>
    ParryHold,
    /// <summary>
    /// **防御按键**（2026-10-29 用户定）：点一下底排中间的防御键 → 长枪举起 → 落防御位。
    /// 不做任何招架判定（敌人不会被打断）；防御期免伤、只涨破防值。
    /// </summary>
    DefendButton,
    /// <summary>
    /// **连点三下**（2026-11-01 用户定，节点一内容2）：连点 N 次触发一次连招（三连 stab）。
    /// 与 <see cref="Tap"/> 的差别只是「要打几次才算完成」——由 <c>TutorialGestureStep.requiredHits</c> 决定。
    /// </summary>
    TapCombo,
    /// <summary>
    /// **用技能打断**（2026-11-01 用户定，节点二内容2）：提示手落在主动技能槽上，
    /// 玩家点一下技能槽 → 技能释放 → 目标敌人的霸体攻击被打断（以「敌人不再处于攻击态」为成功判据）。
    /// 这一拍里**禁止玩家出招**（闸门 = BlockAll），避免敌人被点死而没有打断机会。
    /// </summary>
    SkillSlot,
    /// <summary>
    /// **连招蓄力**（2026-11-04）：两段独立输入 ——
    /// ① **点一下**（像点戳教学那样）打出第一段；
    /// ② 在**接续窗口打开时冻结**招式推进，玩家**按住蓄满 → 松手** → 打出派生招式。
    /// </summary>
    ComboCharge
}

[System.Serializable]
public class TutorialGestureStep
{
    [Tooltip("只对这一份战斗配置生效；留空 = 任意教学战斗")]
    public StageConfig battle;
    [Tooltip("波次索引（0 起）；-2 表示路线出口出现箭头的那一刻")]
    public int waveIndex = 0;
    public GestureHintKind kind = GestureHintKind.Tap;
    [Tooltip("屏幕归一化坐标 0..1：手指起点")]
    public Vector2 from01 = new Vector2(0.5f, 0.5f);
    [Tooltip("屏幕归一化坐标 0..1：滑动终点（Tap / Hold 忽略）")]
    public Vector2 to01 = new Vector2(0.5f, 0.72f);
    [Tooltip("台词播完后再延迟多少秒出现")]
    public float delay = 0.2f;
    [Tooltip("一次动作占用的秒数（等它播完再判定下一次）")]
    public float playSeconds = 1.2f;
    [Tooltip("暂停游戏，玩家真的做对这个手势才继续（推荐开着）")]
    public bool pauseUntilPerformed = true;
    [Tooltip("登场台词：这一波开波就冻结敌人并暂停把台词播完，再进入教学拍（骑兵登场这类需要先看人的场面）")]
    public bool lineAtWaveStart;
    [Tooltip("等目标敌人血量降到这个比例再开始教（1 = 开局就教）。例如 0.8 = 掉到 80% 才教")]
    [Range(0.05f, 1f)] public float triggerHpPercent = 1f;

    [Tooltip("**时停拍**：把整个游戏冻结（timeScale=0），玩家在冻结中做手势。\n" +
             "用于招架 —— 必须冻在敌人的出伤窗口内（窗口开了才冻 → 冻结期间窗口永远开着）。")]
    public bool freezeOnGesture;

    [Tooltip("示范贴在**玩家**身上而不是目标敌人（招架/防御都是玩家侧动作）")]
    public bool anchorToPlayer;

    [Tooltip("成功判据 = 玩家进入**防御姿态**（而不是出招）；用于「上划后按住」这一拍")]
    public bool holdAfterGesture;

    [Tooltip("成功后让目标敌人离场（109 回退淡出，不计击杀）")]
    public bool despawnTargetOnSuccess;

    [Tooltip("重复次数；<=0 表示一直重复到本波结束")]
    public int loops = 0;
    [Tooltip("两次之间的间隔秒数")]
    public float gap = 0.6f;

    [Tooltip("这一拍要打几次才算完成（1 = 打一次就完成）。TapCombo 用它做「连点三下」")]
    public int requiredHits = 1;

    [Tooltip("**在敌人的霸体攻击（C 技前摇）起手时冻结整个游戏**：\n" +
             "用于「敌人正在抡霸体招 → 暂停教玩家按住防御键格挡这一击」。\n" +
             "冻结点 = C 技起手瞬间（不是命中帧）——冻结在命中帧的话敌人已经挥下来了，玩家来不及反应。\n" +
             "冻结期间玩家用**非缩放时间**推进持枪姿态（否则 timeScale=0 时防御位永远进不去）。")]
    public bool freezeOnCAttack;

    [Tooltip("等目标敌人**起 C 技（霸体前摇）**再开始这一拍（不冻结）。\n" +
             "用于「教玩家用技能打断霸体」——必须在敌人正在抡招时提示，否则没有可打断的东西。")]
    public bool waitForCAttack;

    [Tooltip("SkillSlot 拍：要指向哪个技能（升级 id，如 active_cyclone）。留空 = 指向第一个主动技能槽")]
    public string skillUpgradeId = "";

    [Tooltip("这一拍**完成后**补播的台词（非阻塞，不暂停）。\n" +
             "例：连点三下完成后补一句「多次攻击可以打断敌人的攻击」。")]
    public DialogueEventData afterDialogue;

    [Tooltip("这一拍**开始时**推的台词（**和手势示范同时出现**）。\n" +
             "教学关的波次台词走 `TutorialStageDirector` 的波次台词通道；\n" +
             "「招式解锁引导」的一行文字走这里（它要和示范同时出现，不是收拍后补播）。")]
    public DialogueEventData beforeDialogue;

    [Tooltip("**冻结瞬间**推的台词（和冻结时出现的手势示范同时出现）。\n" +
             "用于「用技能打断」这一拍：敌人抬手到顶 → 冻结 → 台词 + 手势落在技能槽上。\n" +
             "⚠️ 别用 afterDialogue 干这件事：那一拍是「靠放技能收拍」的，\n" +
             "   而 afterDialogue 只在「靠出招收拍」的路径上播（RegisterBeatHit）→ 永远播不出来。")]
    public DialogueEventData freezeDialogue;

    [Tooltip("**连招蓄力拍**：阶段 2（窗口打开、冻结之后）的台词 ——「按住蓄满 → 松手」。\n" +
             "阶段 1 的台词走 <see cref=\"beforeDialogue\"/>（先点一下）。")]
    public DialogueEventData comboPhase2Dialogue;

    [Tooltip("**连招蓄力拍**：阶段 3 的台词 ——「瞄准」：敌人抡招抬手被**冻结**的那一刻播。\n" +
             "教玩家把**手指按在敌人身上**再松手（否则武器退回收蓄力漂浮位，要重新蓄力）。")]
    public DialogueEventData comboPhase3Dialogue;

    // ── 招式解锁引导（2026-11-04）──────────────────────────────────
    // 「解锁了但还没教过的招式」当成**一拍拍出来**（和教学拍同一条机制），
    // 而不是另起一套表现 → 玩家看到/学到的和教学关里学点戳完全一致。
    [Tooltip("招式解锁引导：天赋节点 id。这一拍**真的打出去了**才写存档「已教过」（空 = 普通教学拍）")]
    public string guideNodeId = "";

    [Tooltip("招式解锁引导：要教的招式（-1 = 按 kind 推断）")]
    public int guideAttackType = -1;
}
