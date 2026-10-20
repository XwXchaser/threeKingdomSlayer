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
public class TutorialStageDirector : MonoBehaviour
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

    private StageController _stage;
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
        _stage = StageController.Instance;
        _waveSpawner = FindObjectOfType<WaveSpawner>();
        if (_waveSpawner != null) _waveSpawner.OnWaveStarted += OnWaveStarted;
        if (_stage != null) _stage.OnStageVictory += OnStageVictory;
    }

    private void OnDisable()
    {
        if (_waveSpawner != null) _waveSpawner.OnWaveStarted -= OnWaveStarted;
        if (_stage != null) _stage.OnStageVictory -= OnStageVictory;
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
            var route = FakeRouteRuntime.Instance;
            if (route != null && route.RouteConfig == tutorialRouteConfig && route.Phase != FakeRoutePhase.None)
            {
                _openingPlayed = true;
                // 教学关战败不重来：只保留回主菜单（否则要把整个教学流程再跑一遍）
                var stageNow = StageController.Instance != null ? StageController.Instance : _stage;
                if (stageNow != null) stageNow.blockRestartOnDefeat = true;
                DialogueManager.Instance?.QueueEventAndNotify(openingDialogue, forceReplay: true);
                DebugLog.Info("[Tutorial] 开幕台词");
            }
        }

        var routeRuntime = FakeRouteRuntime.Instance;
        bool onTutorialRoute = routeRuntime != null && tutorialRouteConfig != null && routeRuntime.RouteConfig == tutorialRouteConfig;

        // 出口光标：选完路（或离开选择阶段/离开教学路线）就收起来。必须放在下面的「已推过」早退之前。
        if (_routeHintActive && (!onTutorialRoute || routeRuntime.Phase != FakeRoutePhase.ChoosingRoute))
        {
            StopHint();
            // 路线台词没读完也要收：它虽不暂停，但会挡住路线推进（Blocking 认对话）
            var liveView = FindObjectOfType<BattleDialogueView>();
            if (liveView != null && liveView.IsShowing) liveView.Skip();
        }

        if (_choiceDialogueShown || choiceDialogue == null || !onTutorialRoute) return;
        if (routeRuntime.Phase != FakeRoutePhase.ChoosingRoute) return;
        _choiceDialogueShown = true;

        // 出口这一拍不暂停：先把上一波还没读完的台词收掉，再推不拦人的路线台词
        var activeView = FindObjectOfType<BattleDialogueView>();
        if (activeView != null && activeView.IsShowing) activeView.Skip();
        DialogueManager.Instance?.QueueEventAndNotify(choiceDialogue, forceReplay: true);
        StartHint(-2);
    }

    private void OnWaveStarted(int waveIndex)
    {
        Debug.Log($"[Tutorial] 波次开始 wave={waveIndex} stage={(_stage != null && _stage.stageConfig != null ? _stage.stageConfig.name : "NULL")} hint={(gestureHint != null)} steps={(gestureSteps != null ? gestureSteps.Length : -1)}");
        if (_stage == null || _stage.stageConfig == null) return;
        var config = _stage.stageConfig;
        var dialogues = config == n1Battle ? n1Dialogues : (config == e0Battle ? e0Dialogues : null);
        if (dialogues == null) return; // 不是教学战斗

        // 新一波开始先把上一波没读完的台词收掉：否则手势门槛会用上一波的文字暂停在那里
        var staleView = FindObjectOfType<BattleDialogueView>();
        if (staleView != null && staleView.IsShowing) staleView.Skip();

        ApplyTutorialRules(config);

        // 台词不在这里推：入场期间 timeScale=0 会让敌人走不动，也看不到。
        // 交给 HintRoutine，等敌人站定后再推，这样台词和手势引导同时出现。
        _pendingWaveDialogue = (waveIndex >= 0 && waveIndex < dialogues.Length) ? dialogues[waveIndex] : null;

        StartHint(waveIndex);
    }

    /// <summary>教学期规则：固定升级候选、禁用非上挑的蓄力招式。每波开始重设一次，避免上一场的状态残留。</summary>
    private void ApplyTutorialRules(StageConfig config)
    {
        var choices = UpgradeChoiceManager.Instance;
        if (choices == null) return;
        if (config == n1Battle)
        {
            // N1 给一次升级，固定冰雾
            choices.suppressChoices = false;
            choices.forcedChoices = tutorialIceSkill != null
                ? new List<UpgradeDefinition> { tutorialIceSkill }
                : new List<UpgradeDefinition>();
        }
        else
        {
            // E0 是破盾与冲锋教学，不放三选一，避免随机技能盖掉冰雾
            choices.forcedChoices = new List<UpgradeDefinition>();
            choices.suppressChoices = true;
        }
    }

    /// <summary>按「当前战斗配置 + 波次」找步骤并开始手势提示；没有配置就不做。</summary>
    private void StartHint(int waveIndex)
    {
        StopHint();
        if (gestureHint == null || _stage == null || _stage.stageConfig == null || gestureSteps == null)
        {
            Debug.Log($"[Tutorial] 手势未启动：hint={(gestureHint != null)} stage={(_stage != null)} cfg={(_stage != null && _stage.stageConfig != null)} steps={(gestureSteps != null)}");
            return;
        }
        var battle = _stage.stageConfig;
        for (int i = 0; i < gestureSteps.Length; i++)
        {
            var step = gestureSteps[i];
            if (step == null || step.waveIndex != waveIndex) continue;
            if (step.battle != null && step.battle != battle) continue;
            Debug.Log($"[Tutorial] 命中手势步骤 {i} wave={waveIndex} kind={step.kind} pause={step.pauseUntilPerformed}");
            // 路线出口（负数波次）不走暂停门槛：那一拍没有战斗，只需把光标停在箭头上
            _hintRoutine = waveIndex < 0 ? StartCoroutine(RouteHintRoutine(step)) : StartCoroutine(HintRoutine(step));
            return;
        }
        Debug.Log($"[Tutorial] 没有匹配的手势步骤 wave={waveIndex} battle={battle.name}");
    }

    private void StopHint()
    {
        if (_hintRoutine != null) { StopCoroutine(_hintRoutine); _hintRoutine = null; }
        _routeHintActive = false;
        // 登场台词的暂停如果被打断（转场/阵亡），必须把时间恢复，否则回主菜单也是卡住的
        if (_introPaused)
        {
            _introPaused = false;
            Time.timeScale = 1f;
        }
        // 教学拍被打断（波次结束/关场景）时必须把敌人、压暗、手势过滤全部恢复，否则整局卡死
        EndBeat();
        // 正在门槛里被打断时必须走 Cancel：它会恢复 timeScale 和游戏输入
        if (gestureHint != null)
        {
            if (gestureHint.IsGating) gestureHint.Cancel();
            else gestureHint.Hide();
        }
        var view = FindObjectOfType<BattleDialogueView>();
        if (view != null) view.PointerAdvanceEnabled = true;
    }

    /// <summary>
    /// 路线出口的手势：不暂停、不遮罩，只在真实的路线按钮上循环一个点按光标，直到玩家选完路。
    /// 从前这里走暂停门槛，把玩家点箭头的那一下吃掉又重放成一次戳击，路线就永远选不动。
    /// </summary>
    private IEnumerator RouteHintRoutine(TutorialGestureStep step)
    {
        _routeHintActive = true;
        Vector2 pos = ScreenPoint(step.from01);

        // 等路线按钮真的生成出来，把光标放到箭头上（硬编码坐标会点偏）
        float waited = 0f;
        while (waited < 3f)
        {
            if (TryGetRouteChoiceScreenPosition(out var routePos)) { pos = routePos; break; }
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        float playSeconds = step.playSeconds > 0f ? step.playSeconds : 1.1f;
        while (_routeHintActive)
        {
            gestureHint?.PlayTap(pos);
            yield return new WaitForSecondsRealtime(playSeconds);
            if (!_routeHintActive) break;
            gestureHint?.Hide();
            if (step.gap > 0f) yield return new WaitForSecondsRealtime(step.gap);
            if (TryGetRouteChoiceScreenPosition(out var latest)) pos = latest;
        }

        gestureHint?.Hide();
        _hintRoutine = null;
    }

    /// <summary>当前路线按钮（箭头）在屏幕上的位置。</summary>
    private static bool TryGetRouteChoiceScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var buttons = FindObjectsOfType<RouteChoiceButton>();
        if (buttons == null || buttons.Length == 0) return false;
        var rect = buttons[0].transform as RectTransform;
        if (rect == null) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
        return true;
    }

    /// <summary>
    /// 登场台词：先冻结敌人（避免一出场就冲锋），展示台词并暂停，玩家点完再放行。
    /// 台词本身是 nonBlocking 资产，这里的暂停由教学导演掌控。
    /// </summary>
    private IEnumerator PlayWaveIntroLine()
    {
        var line = _pendingWaveDialogue;
        _pendingWaveDialogue = null;
        SetEnemiesHold(true);
        if (line == null)
        {
            SetEnemiesHold(false);
            yield break;
        }

        DialogueManager.Instance?.QueueEventAndNotify(line, forceReplay: true);
        var view = FindObjectOfType<BattleDialogueView>();
        float waitShow = 0f;
        while (view != null && !view.IsShowing && waitShow < 1.5f)
        {
            waitShow += Time.unscaledDeltaTime;
            yield return null;
        }
        if (view != null) view.PointerAdvanceEnabled = true;

        var dialogue = DialogueManager.Instance;
        float limit = 12f;
        while (dialogue != null && (dialogue.IsPlaying || dialogue.IsBusy))
        {
            if (Time.timeScale != 0f) Time.timeScale = 0f;
            _introPaused = true;
            limit -= Time.unscaledDeltaTime;
            if (limit <= 0f) break;
            yield return null;
        }
        Time.timeScale = 1f;
        _introPaused = false;
        SetEnemiesHold(false);
        Debug.Log("[Tutorial] 登场台词结束，放行");
    }

    /// <summary>
    /// 教学手势拍：敌人冻结、场景压暗（角色/长枪/敌人保持亮），循环示范手势；
    /// 玩家用**真实输入**做出目标招式后立刻收场 —— 不暂停、不重放、不遮住角色。
    /// </summary>
    private IEnumerator HintRoutine(TutorialGestureStep step)
    {
        if (step.delay > 0f) yield return new WaitForSecondsRealtime(step.delay);

        // 登场拍：一开波就冻结敌人并暂停把台词播完（骑兵这类先见人的场面）
        if (step.lineAtWaveStart) yield return PlayWaveIntroLine();

        // 只播登场台词、不教手势的波次（上挑已经教过，不再重复）
        if (step.kind == GestureHintKind.None)
        {
            Debug.Log("[Tutorial] 本波只播登场台词，不教手势");
            _hintRoutine = null;
            yield break;
        }

        // 等敌人站定（第 0 排、不在移动），并且血量降到门槛以下才出手势
        if (step.waveIndex >= 0)
        {
            // 登场台词拍（骑兵这类）不要求血量：台词一结束、敌人到前排就开教，
            // 否则骑兵在 1 排/0 排之间来回，等血量会把玩家捉在场上很久
            float hpThreshold = step.lineAtWaveStart ? 1f : step.triggerHpPercent;
            float waited = 0f;
            float limit = hintWaitForEnemySeconds + (hpThreshold < 0.999f ? 25f : 0f);
            while (true)
            {
                if (_stage != null && !_stage.IsStageInProgress)
                {
                    Debug.Log("[Tutorial] 等待期间舞台已结束，取消这一拍");
                    _hintRoutine = null;
                    yield break;
                }
                var enemy = FindTargetEnemy();
                bool hpOk = enemy != null && enemy.maxHealth > 0f &&
                            enemy.currentHealth / enemy.maxHealth <= hpThreshold;
                if (enemy != null && hpOk) break;
                waited += Time.unscaledDeltaTime;
                if (waited >= limit)
                {
                    Debug.Log($"[Tutorial] 等待超时（敌人={(enemy != null)} 血比={(enemy != null && enemy.maxHealth > 0f ? (enemy.currentHealth / enemy.maxHealth).ToString("P0") : "-")}），按当前状态开始");
                    break;
                }
                yield return null;
            }
            Debug.Log($"[Tutorial] 教学条件满足，等待 {waited:F1} 秒");
        }

        // 敌人站定后才推台词：此时台词和手势引导一起出现，玩家看得见该做什么
        BattleDialogueView view = null;
        if (_pendingWaveDialogue != null)
        {
            // 强制重播：教学台词是教学关的固定节拍，不能因为上一次通关就永久不再播
            DialogueManager.Instance?.QueueEventAndNotify(_pendingWaveDialogue, forceReplay: true);
            _pendingWaveDialogue = null;

            // 等对话真的显示出来再弹手势；否则手势出现时屏幕上一个字也没有
            view = FindObjectOfType<BattleDialogueView>();
            float waitShow = 0f;
            while (view != null && !view.IsShowing && waitShow < 1.5f)
            {
                waitShow += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 开始教学拍：冻结敌人（不让它们行动）、只压暗场景、只接受目标手势
        var target = ToGestureTarget(step.kind);
        if (gestureHint == null || target == TutorialGestureTarget.None)
        {
            Debug.Log($"[Tutorial] 手势拍无法开始：hint={(gestureHint != null)} target={target}");
            _hintRoutine = null;
            yield break;
        }

        // 等条件期间可能已经阵亡/通关：不要再开拍，否则示范会盖在战败/胜利面板上一直循环
        if (_stage != null && !_stage.IsStageInProgress)
        {
            Debug.Log("[Tutorial] 舞台已结束，取消手势拍");
            _hintRoutine = null;
            yield break;
        }

        // 目标敌人正在挥的那一刀先让它收完（最多 2 秒），避免冻在半空
        var preBeat = FindTargetEnemy();
        float preWaited = 0f;
        while (preBeat != null && preBeat.isAttackAnimating && preWaited < 2f)
        {
            preWaited += Time.unscaledDeltaTime;
            yield return null;
        }

        // 示范位置：有敌人就贴在目标敌人身上（手指/光圈就在要打的地方）
        Vector2 from = ScreenPoint(step.from01);
        Vector2 to = ScreenPoint(step.to01);
        if (TryGetTargetEnemyScreenPosition(out var enemyScreen))
        {
            to = enemyScreen + (to - from);
            from = enemyScreen;
        }

        BeginBeat(target);
        Debug.Log($"[Tutorial] 手势拍开始 wave={step.waveIndex} kind={step.kind} target={target} 敌人={CountAliveEnemies()}");

        float playSeconds = step.playSeconds > 0f ? step.playSeconds : 1.1f;
        // 蓄力上挑的示范本身要播「按住 1.2s + 上划 1.1s」，播放时长必须跟它对齐，
        // 否则会在示范没播完时就重开一轮，看起来像教程被“多调了一次”
        if (step.kind == GestureHintKind.ChargedSwipe) playSeconds = Mathf.Max(playSeconds, 2.5f);
        while (_beatActive)
        {
            if (_stage != null && !_stage.IsStageInProgress)
            {
                Debug.Log("[Tutorial] 教学拍期间舞台结束，结束手势拍");
                EndBeat();
                break;
            }
            PlayGestureAt(step.kind, from, to);
            yield return new WaitForSecondsRealtime(playSeconds);
            if (!_beatActive) break;
            gestureHint.Hide();
            if (step.gap > 0f) yield return new WaitForSecondsRealtime(step.gap);
        }

        EndBeat();
        _hintRoutine = null;
    }

    // ---------------------------------------------------------------- 教学手势拍

    private void BeginBeat(TutorialGestureTarget target)
    {
        _beatActive = true;
        _beatTarget = target;
        SetEnemiesHold(true);
        SetSceneDim(true);
        var input = InputManager.Instance;
        if (input != null)
        {
            input.tutorialGestureTarget = target;
            input.OnAttackExecuted -= OnTutorialAttackExecuted;
            input.OnAttackExecuted += OnTutorialAttackExecuted;
        }
    }

    private void EndBeat()
    {
        if (!_beatActive) return;
        _beatActive = false;
        var input = InputManager.Instance;
        if (input != null)
        {
            input.OnAttackExecuted -= OnTutorialAttackExecuted;
            input.tutorialGestureTarget = TutorialGestureTarget.None;
        }
        SetEnemiesHold(false);
        SetSceneDim(false);
        gestureHint?.Hide();
        // 做完立刻收掉台词：玩家要能马上看清自己打出了什么
        var view = FindObjectOfType<BattleDialogueView>();
        if (view != null && view.IsShowing) view.Skip();
    }

    /// <summary>真实出招判定：只有这一拍要的招式才算完成，其它输入被 InputManager 挡在外面。</summary>
    private void OnTutorialAttackExecuted(AttackType attackType, int column)
    {
        if (!_beatActive) return;
        if (attackType != ExpectedAttackFor(_beatTarget)) return;
        DebugLog.Info($"[Tutorial] 手势拍完成：{attackType} col={column}");
        EndBeat();
    }

    private static TutorialGestureTarget ToGestureTarget(GestureHintKind kind)
    {
        switch (kind)
        {
            case GestureHintKind.Tap: return TutorialGestureTarget.Tap;
            case GestureHintKind.Swipe: return TutorialGestureTarget.SwipeHorizontal;
            case GestureHintKind.ChargedSwipe: return TutorialGestureTarget.ChargedVertical;
            default: return TutorialGestureTarget.None;
        }
    }

    private static AttackType ExpectedAttackFor(TutorialGestureTarget target)
    {
        switch (target)
        {
            case TutorialGestureTarget.SwipeHorizontal: return AttackType.Slash;
            case TutorialGestureTarget.ChargedVertical: return AttackType.Launch;
            default: return AttackType.Stab;
        }
    }

    private static int CountAliveEnemies()
        => EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies().Count : 0;

    /// <summary>教学拍：让当前所有存活敌人不进攻、不移动（受击与动画照常）。</summary>
    private static void SetEnemiesHold(bool hold)
    {
        if (EnemyManager.Instance == null) return;
        var alive = EnemyManager.Instance.GetAllAliveEnemies();
        for (int i = 0; i < alive.Count; i++)
            if (alive[i] != null) alive[i].tutorialHold = hold;
    }

    /// <summary>只压暗路线场景（背景/地形/植被），不动角色与敌人。</summary>
    private void SetSceneDim(bool dim)
    {
        if (_sceneDim == null) _sceneDim = GetComponent<TutorialSceneDim>();
        if (_sceneDim == null) _sceneDim = gameObject.AddComponent<TutorialSceneDim>();
        if (_sceneDim == null) return;
        _sceneDim.dimBrightness = sceneDimBrightness;

        if (!_sceneDimBound && dim)
        {
            var presenter = FindObjectOfType<ScrollRoutePresenter>();
            var route = presenter != null ? presenter.Route : null;
            if (route == null)
            {
                Debug.Log("[Tutorial] 场景压暗：路线场景未就绪，跳过压暗");
                return;
            }
            _sceneDim.Bind(route.gameObject.scene);
            _sceneDimBound = true;
        }
        _sceneDim.FadeTo(dim);
    }

    private void PlayGestureAt(GestureHintKind kind, Vector2 from, Vector2 to)
    {
        if (gestureHint == null) return;
        switch (kind)
        {
            case GestureHintKind.Tap: gestureHint.PlayTap(from); break;
            case GestureHintKind.Hold: gestureHint.PlayHold(from); break;
            case GestureHintKind.Swipe: gestureHint.PlaySwipe(from, to); break;
            case GestureHintKind.ChargedSwipe: gestureHint.PlayChargedSwipe(from, to); break;
        }
    }

    /// <summary>找到引导指向的敌人：第 0 排、已站定（不在移动）、优先第 2 列。</summary>
    private Enemy FindTargetEnemy()
    {
        if (EnemyManager.Instance == null) return null;
        Enemy best = null;
        var alive = EnemyManager.Instance.GetAllAliveEnemies();
        for (int i = 0; i < alive.Count; i++)
        {
            var e = alive[i];
            if (e == null || e.state == EnemyState.Dead || e.rowIndex != 0 || e.state == EnemyState.Moving) continue;
            if (best == null) { best = e; continue; }
            if (e.columnIndex == 2) best = e;
            else if (best.columnIndex != 2 && e.columnIndex < best.columnIndex) best = e;
        }
        return best;
    }

    /// <summary>引导指向的敌人所在列；没有敌人返回 -1。</summary>
    private int TargetColumn
    {
        get
        {
            var e = FindTargetEnemy();
            return e != null ? e.columnIndex : -1;
        }
    }

    /// <summary>取目标敌人的屏幕坐标；没有敌人就返回 false。</summary>
    private bool TryGetTargetEnemyScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var cam = Camera.main;
        var best = FindTargetEnemy();
        if (cam == null || best == null) return false;
        screenPos = cam.WorldToScreenPoint(best.transform.position);
        return screenPos.x > 0f && screenPos.x < Screen.width && screenPos.y > 0f;
    }

    private static Vector2 ScreenPoint(Vector2 normalized) =>
        new Vector2(normalized.x * Screen.width, normalized.y * Screen.height);

    private void PlayGesture(TutorialGestureStep step)
    {
        if (gestureHint == null) return;
        Vector2 from = ScreenPoint(step.from01);
        Vector2 to = ScreenPoint(step.to01);
        switch (step.kind)
        {
            case GestureHintKind.Tap: gestureHint.PlayTap(from); break;
            case GestureHintKind.Hold: gestureHint.PlayHold(from); break;
            case GestureHintKind.Swipe: gestureHint.PlaySwipe(from, to); break;
            case GestureHintKind.ChargedSwipe: gestureHint.PlayChargedSwipe(from, to); break;
        }
    }

    private void OnStageVictory()
    {
        if (_stage == null || tutorialRouteConfig == null) return;
        var runtime = FakeRouteRuntime.Instance;
        if (runtime == null || runtime.RouteConfig != tutorialRouteConfig) return;

        var data = SaveManager.Load();
        if (data.tutorialCompleted) return;
        data.tutorialCompleted = true;
        SaveManager.Save(data);
        DebugLog.Info("[Tutorial] 教学关已完成并记档");
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


