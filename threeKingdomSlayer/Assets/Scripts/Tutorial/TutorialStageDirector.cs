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
    [Header("教学战斗与台词")]
    [Tooltip("教学 N1（教点戳 / 斩击）")]
    public StageConfig n1Battle;
    [Tooltip("按波次（0 起）对应的台词")]
    public DialogueEventData[] n1Dialogues = new DialogueEventData[0];
    [Tooltip("教学 E0（1021 破盾、109 冲锋）")]
    public StageConfig e0Battle;
    [Tooltip("按波次（0 起）对应的台词")]
    public DialogueEventData[] e0Dialogues = new DialogueEventData[0];

    [Header("教学台词：路线移动")]
    [Tooltip("路线出现出口箭头时播放，教玩家点箭头走到下一个节点")]
    public DialogueEventData choiceDialogue;

    [Header("教学升级")]
    [Tooltip("教学期固定给的这个技能（冰雾）。留空则不给")]
    public UpgradeDefinition tutorialIceSkill;

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
    private Coroutine _hintRoutine;
    private DialogueEventData _pendingWaveDialogue;
    private bool _openingPlayed;
    private bool _routeHintActive;
    private bool _beatActive;
    private bool _introPaused;
    private TutorialGestureTarget _beatTarget = TutorialGestureTarget.None;
    private TutorialSceneDim _sceneDim;
    private bool _sceneDimBound;

    private void Start()
    {
        _stageState = StageController.Instance as IStageBattleState;
        _stageFlow = StageController.Instance as IRunFlow;
        _waveSpawner = FindObjectOfType<WaveSpawner>();
        if (_waveSpawner != null) _waveSpawner.OnWaveStarted += OnWaveStarted;
        if (_stageState != null) _stageState.StageVictory += OnStageVictory;
    }

    private void OnDisable()
    {
        if (_waveSpawner != null) _waveSpawner.OnWaveStarted -= OnWaveStarted;
        if (_stageState != null) _stageState.StageVictory -= OnStageVictory;
        StopHint();
        if (UpgradeChoiceManager.Instance != null)
        {
            UpgradeChoiceManager.Instance.suppressChoices = false;
            UpgradeChoiceManager.Instance.forcedChoices.Clear();
        }
    }

    /// <summary>路线出口（箭头）出现时推一条教学台词：没人告诉过玩家点箭头才能走。</summary>
    private void Update()
    {
        // 开幕台词：教学路线一就绪就播一次
        if (!_openingPlayed && openingDialogue != null && tutorialRouteConfig != null)
        {
            var route = RouteFlowHub.Current;
            if (route != null && route.RouteConfig == tutorialRouteConfig && route.Phase != FakeRoutePhase.None)
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
            StopHint();
            // 路线台词没读完也要收：它虽不暂停，但会挡住路线推进（Blocking 认对话）
            var liveView = ResolveDialogueView();
            if (liveView != null && liveView.IsShowing) liveView.Skip();
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

    private void OnWaveStarted(int waveIndex)
    {
        Debug.Log($"[Tutorial] 波次开始 wave={waveIndex} stage={(_stageState != null && _stageState.StageConfig != null ? _stageState.StageConfig.name : "NULL")} hint={(gestureHint != null)} steps={(gestureSteps != null ? gestureSteps.Length : -1)}");
        if (_stageState == null || _stageState.StageConfig == null) return;
        var config = _stageState.StageConfig;
        var dialogues = config == n1Battle ? n1Dialogues : (config == e0Battle ? e0Dialogues : null);
        if (dialogues == null) return; // 不是教学战斗

        // 新一波开始先把上一波没读完的台词收掉：否则手势门槛会用上一波的文字暂停在那里
        var staleView = ResolveDialogueView();
        if (staleView != null && staleView.IsShowing) staleView.Skip();

        ApplyTutorialRules(config);

        // 台词不在这里推：入场期间 timeScale=0 会让敌人走不动，也看不到。
        // 交给 HintRoutine，等敌人站定后再推，这样台词和手势引导同时出现。
        _pendingWaveDialogue = (waveIndex >= 0 && waveIndex < dialogues.Length) ? dialogues[waveIndex] : null;

        StartHint(waveIndex);
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
    /// <summary>不教手势：这一波只播登场台词（例如 109 骑兵来索命），上挑已经在前一波教过</summary>
    None
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

    [Tooltip("重复次数；<=0 表示一直重复到本波结束")]
    public int loops = 0;
    [Tooltip("两次之间的间隔秒数")]
    public float gap = 0.6f;
}
