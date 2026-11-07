using System.Collections;
using UnityEngine;

/// <summary>
/// 教学关导演的**两种新拍型**（2026-10-26）：
///   ① <see cref="FreezeParryRoutine"/> —— 时停招架拍：敌人出伤窗口打开时**冻结整个游戏**，
///      玩家在冻结中上划 → 同帧解冻并派发招架 → 这一击被格开（敌人攻击被取消，不结算伤害）。
///   ② <see cref="DefendHoldRoutine"/> —— 防御拍：109 被按住不动，玩家「上划后按住」进入防御，
///      教学临时锁定防御 → 放 109 冲锋 → 伤害被吸收（HP 不变）→ 109 回退淡出离场（不计击杀）。
///
/// 输入怎么进来（2026-10-26 修正）：
///   冻结时 <c>Time.timeScale = 0</c>，而 <c>InputManager</c> 默认在 timeScale=0 时不处理任何输入
///   → 教学用 <c>InputManager.allowInputWhileTimeFrozen</c> 让**游戏自己的手势通道**在冻结中继续工作。
///   （早前版本另写了一套原始输入轮询，触摸抬手那一帧拿不到 Ended 相位 → 划动永远识别不到、
///     且识别不到就永久冻结 → 玩家“完全动不了”。现在统一走游戏自己的识别，阈值/方向/抬手全一致。）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    [Header("时停招架拍")]
    [Tooltip("冻结后等玩家手势的保险上限（秒，unscaled）。\n" +
             "正常情况下玩家很快就会上划；这个上限只为防“永远识别不到 → 永久冻结”把游戏卡死。")]
    public float freezeTimeoutSeconds = 20f;

    [Tooltip("「用技能打断霸体」这一拍的上限（秒，unscaled）：玩家一直没点技能就收拍放行，不能把这一波卡死")]
    public float skillBeatTimeoutSeconds = 30f;

    [Tooltip("技能打断拍：**等 FrontItemBar 把技能图标刷进注册表**的上限（秒）。\n" +
             "（2026-11-02：旧写法等满 60 秒才跳过整拍 → 教学不发生、玩家站着挨打；\n" +
             "  现在超过这个时间就用「按槽位序号取位置」的兜底，教学照旧进行。）")]
    public float skillSlotWaitSeconds = 2f;

    [Tooltip("技能打断拍：**等玩家把三选一选完**的上限（秒）。\n" +
             "这一拍必须排在选卡之后（用户期望：「玩家点击选择3选1奖励后 → 敌人进 C 技 → 暂停 → 手势 → 点技能」）；\n" +
             "旧顺序是拍先开、三选一弹窗后到 → 冻结画面被弹窗整个盖住 → 教学等于没发生（2026-11-02 实测：22.38s 冻结 / 22.40s 弹窗）。")]
    public float skillChoiceWaitSeconds = 90f;

    [Tooltip("技能打断拍：玩家**已经放出技能**之后，最多再等多久看这一击有没有被打断（秒）。\n" +
             "到期就收拍 —— 教学不能把玩家卡在「只能防御和技能」的状态里\n" +
             "（2026-11-02 用户反馈：「玩家点击技能后也无法回到正常流程攻击敌人」）。")]
    public float skillResultWaitSeconds = 3f;

    [Tooltip("技能打断拍：敌人的 C 技**只剩多少秒**时才冻结并开始教学（与霸体防御拍同一口径：武器已举到位、还没落）。\n" +
             "调大 = 更早暂停（抬手没做完）；调小 = 更晚（可能刀已经落下）。")]
    public float skillPromptToEndSeconds = 1.5f;

    [Tooltip("霸体防御拍：敌人的攻击**只剩多少秒**时才冻结并开始教学（= 等它把抬手动作做完）。\n" +
             "1012 的 C 技实测：攻击 tween 全长 3.13s、出伤（霸体解除）在剩 1.15s 时 → 取 1.5 = 武器已举到位、刀还没落。\n" +
             "调大 = 更早暂停（抬手没做完）；调小 = 更晚（可能刀已经落下）。")]
    public float defendPromptToEndSeconds = 1.5f;

    /// <summary>时停拍：这一拍把整个游戏冻结（timeScale=0）。</summary>
    private bool _frozenByTutorial;
    /// <summary>防御拍：玩家已进入防御姿态（教学已锁定）。</summary>
    private bool _defendLocked;
    /// <summary>这一拍是否「出招即收拍」（防御拍为 false：成功判据是进入防御姿态）。</summary>
    private bool _beatEndsOnAttack = true;
    /// <summary>当前防御拍的配置（决定成功后是否让目标敌人离场）。</summary>
    private TutorialGestureStep _defendStep;
    /// <summary>时停拍：招架必须**真的格到了**才算这一拍完成（格不到就继续等玩家再试）。</summary>
    private bool _requireParryBlocked;

    // ── ① 时停招架拍 ──────────────────────────────────────────

    private IEnumerator FreezeParryRoutine(TutorialGestureStep step)
    {
        if (step.delay > 0f) yield return new WaitForSecondsRealtime(step.delay);
        if (step.lineAtWaveStart) yield return PlayWaveIntroLine();

        // 1) 等目标敌人站定
        var target = FindTargetEnemy();
        float waited = 0f;
        float limit = hintWaitForEnemySeconds + 25f;
        while (waited < limit)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) { _hintRoutine = null; yield break; }
            target = FindTargetEnemy();
            if (target != null) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (target == null) { _hintRoutine = null; yield break; }

        // 2) 推台词（forceReplay）
        yield return PushWaveDialogue();

        // 3) 等它起手 → **出伤窗口一打开就冻结**
        //    冻结后敌人的攻击 tween 停住 → 窗口永远开着 → 玩家什么时候上划都必然格开。
        //    （若在窗口之前冻结，timeToStrike 被冻在窗口外 → 招架永远判不中 → 死锁。）
        bool frozen = false;
        float frozenAt = 0f;
        Vector2 demoFrom = step.anchorToPlayer ? PlayerScreenPoint() : ScreenPoint(step.from01);
        Vector2 demoTo = step.anchorToPlayer ? demoFrom + new Vector2(0f, 260f) : ScreenPoint(step.to01);
        waited = 0f;
        while (waited < limit)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) break;
            if (target == null || target.state == EnemyState.Dead) break;
            if (frozen && !_beatActive) break;      // 玩家已出招 → 拍已收

            if (!frozen)
            {
                // 冻结点 = **命中帧那一刻**（武器已经挥到位），而不是出伤窗口开启（0.15s 之前）。
                // 冻结在窗口开头的话，玩家看到的是「敌人还在抬手」→ 感受上就是「招架打断了抬手帧」；
                // 冻结在命中帧上，玩家看到的是「这一击正要打到我」→ 感受是「格开了这一击」。
                // 冻结后窗口永远开着（tween 停住），所以玩家什么时候划都仍然算格开。
                float toStrike = target.TimeToStrike();
                if (target.isAttackAnimating && toStrike >= 0f && toStrike <= 0.03f)
                {
                    Time.timeScale = 0f;
                    _frozenByTutorial = true;
                    frozen = true;
                    frozenAt = Time.unscaledTime;

                    // 冻结时 Time.deltaTime = 0 → 上一拍留下的动作锁永远走不完 → 招架发不出来
                    AttackSystem.Instance?.ClearActionLock();

                    // 压暗：只压暗**场景**（角色/敌人/长枪保持原亮度）。
                    // 不用提示层的全屏遮罩 —— 那会把角色也遮住（与「遮罩只遮场景」的设计相反）。
                    SetSceneDim(true);
                    // 开拍：闸门只放行"不蓄力竖滑"（= 招架）；这一拍教的就是招架 → 允许发出
                    _currentTeachAttack = AttackType.Parry;
                    _requireParryBlocked = true;     // 格不到不算完成 → 玩家可以再试（教学不设失败）
                    BeginBeat(TutorialGestureTarget.ParryVertical, AttackType.Parry, holdEnemies: false);

                    var input = InputManager.Instance;
                    if (input != null) input.allowInputWhileTimeFrozen = true;   // 冻结中仍处理玩家输入
                    // **手势示范**（自己循环，玩家任何时候都能看到要在哪儿划）
                    PlayGestureAt(step.kind, demoFrom, demoTo);
                    Debug.Log("[Tutorial] 时停招架拍：已冻结（timeScale=0），等待玩家上划招架");
                }
            }
            else
            {
                // 每帧压住：DialogueManager.Finish() 会把 timeScale 顶回 1
                if (Time.timeScale != 0f) Time.timeScale = 0f;
                // 防卡死保险：玩家一直没做出手势 → 放行并结束这一拍（不让游戏永远冻着）
                if (Time.unscaledTime - frozenAt > freezeTimeoutSeconds)
                {
                    Debug.LogWarning("[Tutorial] 时停招架拍：等待手势超时，放行并结束这一拍");
                    EndBeat();
                    break;
                }
            }

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        // 4) 收场（EndBeat 已解冻；这里只兜底）
        if (frozen && _beatActive) EndBeat();
        _requireParryBlocked = false;
        _hintRoutine = null;
    }

    // ── ② 防御拍 ────────────────────────────────────────────

    private IEnumerator DefendHoldRoutine(TutorialGestureStep step)
    {
        if (step.delay > 0f) yield return new WaitForSecondsRealtime(step.delay);
        if (step.lineAtWaveStart) yield return PlayWaveIntroLine();

        // 1) 等目标敌人（109）站定
        var target = FindTargetEnemy();
        float waited = 0f;
        float limit = hintWaitForEnemySeconds + 25f;
        while (waited < limit)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) { _hintRoutine = null; yield break; }
            target = FindTargetEnemy();
            if (target != null) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (target == null) { _hintRoutine = null; yield break; }

        // 2) 推台词
        yield return PushWaveDialogue();

        // 3) 按住敌人（tutorialHold → 冲锋计时不走），等玩家进入防御再放行
        target.tutorialHold = true;

        _defendLocked = false;
        _beatEndsOnAttack = false;
        _currentTeachAttack = null;                    // 防御键不教任何招式
        _defendStep = step;
        // **闸门不再限手势**：这一拍教的是「按防御键」，玩家仍可自由点击/划动攻击。
        BeginBeat(TutorialGestureTarget.None, null, holdEnemies: true);

        var ps = PlayerState.Instance;
        if (ps != null && ps.Guard != null) ps.Guard.OnDamageAbsorbed += OnGuardAbsorbed;

        Vector2 from = step.anchorToPlayer ? PlayerScreenPoint() : ScreenPoint(step.from01);
        Vector2 to = step.anchorToPlayer ? from + new Vector2(0f, 260f) : ScreenPoint(step.to01);

        // 防御键拍：提示手去**点那个按钮**（不是划动）→ 落点用按钮的屏幕坐标。
        if (step.kind == GestureHintKind.DefendButton && TryGetDefendButtonScreenPoint(out var btnPos))
        {
            from = btnPos;
            to = btnPos;
        }

        PlayGestureAt(step.kind, from, to);   // 示范自循环，不需要反复重播

        while (_beatActive)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) { EndBeat(); break; }

            // 成功判据：玩家进入防御姿态 → 放开敌人，让它出手
            // （2026-10-29：防御键改成 **hold** 语义 —— 玩家按住按钮本身就是“按住”，
            //   不再需要 forcePointerHeld 去强行锁住手指）
            var p = PlayerState.Instance;
            if (!_defendLocked && p != null && p.WeaponStance != null && p.WeaponStance.IsDefending)
            {
                _defendLocked = true;
                if (target != null) target.tutorialHold = false;
                Debug.Log("[Tutorial] 防御拍：玩家已进入防御 → 放开敌人出手");
            }

            yield return new WaitForSecondsRealtime(0.25f);
        }

        // 4) 收场：解除订阅
        if (ps != null && ps.Guard != null) ps.Guard.OnDamageAbsorbed -= OnGuardAbsorbed;
        _beatEndsOnAttack = true;
        _defendLocked = false;
        // 教学拍结束 → 收起防御（把枪放下来）
        var ps2 = PlayerState.Instance;
        if (ps2 != null) ps2.SetDefend(false);
        _hintRoutine = null;
    }

    /// <summary>防御按键的屏幕坐标（提示手去点它）。</summary>
    private static bool TryGetDefendButtonScreenPoint(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var button = DefenseButton.Instance;
        if (button == null || button.Button == null) return false;
        var rect = button.Button.GetComponent<RectTransform>();
        if (rect == null) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
        return true;
    }

    /// <summary>防御吸收了伤害 → 防御成功：解除锁定，按配置决定是否让目标敌人离场。</summary>
    private void OnGuardAbsorbed(float damage)
    {
        if (!_defendLocked) return;
        _defendLocked = false;
        Debug.Log("[Tutorial] 防御拍：成功吸收伤害 " + damage.ToString("F0") + " → 收拍");

        // 防御成功 = 玩家**真的做到了** → 这一拍算教完（写存档标记的依据）
        _beatCompleted = true;
        EndBeat();

        // 不离场（despawnTargetOnSuccess = false）→ 敌人继续打，玩家继续用防御处理
        if (_defendStep != null && !_defendStep.despawnTargetOnSuccess) return;

        var target = FindTargetEnemy();
        if (target == null) return;
        var cavalry = target.GetComponent<CavalryEnemy>();
        if (cavalry != null) cavalry.RequestTutorialExit();
        else EnemyManager.Instance?.DespawnEnemy(target);
    }

    // ── 共用小工具 ───────────────────────────────────────────

    /// <summary>推这一拍的台词并等它真的显示出来（forceReplay：教学台词是固定节拍）。</summary>
    private IEnumerator PushWaveDialogue()
    {
        // 走统一入口：教学拍会在这里变成「开场两段字幕 + 点击继续」
        yield return PushBeatDialogue(null);
    }

    /// <summary>
    /// 推这一拍的台词（**所有拍型共用**）。
    ///
    /// · 教学拍且配了开场第一段 → 走「两段字幕 + 点击继续」（第一段 = 开场铺垫，第二段 = 原本那行台词）
    /// · 否则 → 原本的单句台词（forceReplay）
    ///
    /// ⚠️ 所有拍型共用这个入口 → **各种拍（点一下 / 连点 / 防御 / 霸体时停 / 技能打断）
    ///   的开场两段都发生在「原本那行台词出现的那一刻」**，早晚一致。
    /// </summary>
    /// <param name="fallbackDialogue">这一拍自带的台词（没有波次台词时用它）。</param>
    private IEnumerator PushBeatDialogue(DialogueEventData fallbackDialogue)
    {
        var waveDialogue = _pendingWaveDialogue;
        _pendingWaveDialogue = null;
        var beatDialogue = waveDialogue != null ? waveDialogue : fallbackDialogue;

        if (TryGetIntroLine(_currentStep, out var introLine))
        {
            yield return PlayTutorialIntro(introLine, beatDialogue);
            yield break;
        }

        if (beatDialogue == null) yield break;
        // 教学文本 → 压暗画面（黑幕）；压到这一拍的手势做完（EndBeat）才撤
        SetTeachingDim(true);
        DialogueManager.Instance?.QueueEventAndNotify(beatDialogue, forceReplay: true);

        var view = ResolveDialogueView();
        float waitShow = 0f;
        while (view != null && !view.IsShowing && waitShow < 1.5f)
        {
            waitShow += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    /// <summary>玩家的屏幕位置（招架/防御是玩家侧动作 → 示范贴玩家而不是贴敌人）。</summary>
    private static Vector2 PlayerScreenPoint()
    {
        var cam = Camera.main;
        var ps = PlayerState.Instance;
        if (cam == null || ps == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.35f);
        Vector3 sp = cam.WorldToScreenPoint(ps.transform.position + Vector3.up * 1.2f);
        return new Vector2(sp.x, sp.y);
    }

    // ── ③ 霸体时停防御拍（2026-11-01，节点二内容1）────────────────────────
    //
    // 需求：敌人抡起**霸体技**（C 技前摇）时**暂停**（冻结整个游戏），
    //       教玩家按住防御键格挡这一击。
    //
    // 两个与既有拍型不同的地方：
    //  1. **冻结点 = C 技起手瞬间**（isCFrame 刚为 true），不是命中帧 ——
    //     命中帧时敌人已经挥下来了，玩家来不及反应（招架时停拍冻结在命中帧是因为那里才「像格开」）。
    //  2. **冻结期间要用非缩放时间推进玩家持枪姿态**：
    //     PlayerState.Update 传给 WeaponStance.Tick 的是 Time.deltaTime（受 timeScale 影响），
    //     冻结时 deltaTime=0 → 防御位永远进不去 → 玩家按了也没用（“完全动不了”）。
    //     所以本拍每帧额外用 unscaledDeltaTime 驱动一次 Tick（PlayerState 传的 0 不会重复推进）。
    private IEnumerator DefendAtCAttackRoutine(TutorialGestureStep step)
    {
        if (step.delay > 0f) yield return new WaitForSecondsRealtime(step.delay);
        if (step.lineAtWaveStart) yield return PlayWaveIntroLine();

        // 1) 等目标敌人站定
        var target = FindTargetEnemy();
        float waited = 0f;
        float limit = hintWaitForEnemySeconds + 25f;
        while (waited < limit)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) { _hintRoutine = null; yield break; }
            target = FindTargetEnemy();
            if (target != null) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (target == null) { _hintRoutine = null; yield break; }

        // 2) 推台词
        yield return PushWaveDialogue();

        // 3) 等敌人起 C 技 → 冻结 → 教按住防御键 → 解冻 → 这一击落下来被吸收
        var ps = PlayerState.Instance;
        if (ps != null && ps.Guard != null) ps.Guard.OnDamageAbsorbed += OnGuardAbsorbed;

        bool frozen = false;
        bool beatStarted = false;
        bool awaitingStrike = false;
        float frozenAt = 0f;

        // 2.5) **拍一开始就禁掉出招**：这一拍的敌人是「霸体教学对象」，
        //      玩家抢在它抬手之前把它打死 = 格挡教学永远教不到
        //      （2026-11-02 实测：E0 第一波的 1012 只有 150 血，自动流程 1.6 秒就点没了，
        //        霸体技还没起手人就没了 → 整拍被跳过）。
        //      台词已经说了「它会出红技、按住防御键」，所以这段“只能看不能打”是合理的。
        if (!_beatActive)
        {
            BeginBeat(TutorialGestureTarget.BlockAll, null, holdEnemies: false);
            beatStarted = true;
            _beatEndsOnAttack = false;
            // 压暗留到真正冻结的那一刻：别在敌人抬手时就把画面压黑（用户要看抬手）
            SetSceneDim(false);
        }
        while (waited < limit)
        {
            // 注意：拍是在「敌人起 C 技」那一刻才 BeginBeat 的，
            // 所以**不能用 _beatActive 当循环条件**（那时它还没置位 → 循环会秒退）。
            // 只有「已经开了拍、又被收掉」才算结束。
            if (beatStarted && !_beatActive) break;
            if (_stageState != null && !_stageState.IsStageInProgress) break;
            if (target == null || target.state == EnemyState.Dead)
            {
                // 目标没了（被抢杀 / 被打断送走）→ 换下一只继续教，别把这一拍丢掉
                var next = FindTargetEnemy();
                if (next != null)
                {
                    DebugLog.Info("[Tutorial] 霸体防御拍：原目标已不在 → 换目标继续等霸体技");
                    target = next;
                }
                else
                {
                    waited += Time.unscaledDeltaTime;
                    yield return null;
                    continue;
                }
            }

            if (!frozen)
            {
                if (awaitingStrike)
                {
                    // 玩家已经进入防御位、已解冻 → 等这一击落下来
                    // 收拍条件：① 被吸收（OnGuardAbsorbed 回调）；② 这一击没能打中玩家
                    //            （敌人的攻击已经结束 —— 无论是被格开还是别的原因）
                    if (!target.isAttackAnimating)
                    {
                        awaitingStrike = false;   // 这一击打完了 → 等下一个 C 技起手再冻结
                    }
                }
                else
                {
                    // ② 等敌人把**抬手动作做完**再冻结（2026-11-02 用户：「不能一标红就开教学，意义不明」）。
                    //    判据用攻击剩余时长：剩余不多 = 武器已经举到位、刀还没落 → 这时暂停，
                    //    玩家看到的是「敌人举着刀停住」，而不是「刚抬手就被冻住」。
                    //    实测 1012 的 C 技：攻击 tween 全长 3.13s，出伤（霸体解除）在剩 1.15s 时。
                    bool cAttack = target.isAttackAnimating && target.isCFrame;
                    float toAttackEnd = cAttack ? target.TimeToAttackEnd() : -1f;
                    if (cAttack && toAttackEnd >= 0f && toAttackEnd <= defendPromptToEndSeconds)
                    {
                        // —— 冻结 ——
                        Time.timeScale = 0f;
                        _frozenByTutorial = true;
                        frozen = true;
                        frozenAt = Time.unscaledTime;
                        AttackSystem.Instance?.ClearActionLock();

                        SetSceneDim(true);
                        _currentTeachAttack = null;
                        _beatEndsOnAttack = false;              // 成功判据 = 吸收伤害 / 这一击没打中
                        _defendLocked = false;
                        _defendStep = step;

                        var input = InputManager.Instance;
                        if (input != null) input.allowInputWhileTimeFrozen = true;

                        // 提示手落在防御键上
                        if (TryGetDefendButtonScreenPoint(out var btnPos))
                            PlayGestureAt(GestureHintKind.DefendButton, btnPos, btnPos);
                        DebugLog.Info("[Tutorial] 霸体防御拍：抬手完成（剩余 " + toAttackEnd.ToString("F2") + "s）→ 已锁住动作，等待玩家按住防御键");
                    }
                }
            }
            else
            {
                if (Time.timeScale != 0f) Time.timeScale = 0f;

                // 冻结期间推进持枪姿态（否则防御位进不去）
                DriveStanceUnscaled();

                // 玩家进入防御位 → 解冻，让敌人这一击落下来
                var p = PlayerState.Instance;
                if (!_defendLocked && p != null && p.WeaponStance != null && p.WeaponStance.IsDefending)
                {
                    _defendLocked = true;
                    _frozenByTutorial = false;
                    Time.timeScale = 1f;
                    frozen = false;
                    awaitingStrike = true;
                    DebugLog.Info("[Tutorial] 霸体防御拍：玩家已进入防御 → 解冻，等敌人这一击打下来");
                }

                // 防卡死保险：玩家一直没按防御键 → 放行并结束这一拍
                if (Time.unscaledTime - frozenAt > freezeTimeoutSeconds)
                {
                    DebugLog.Warning("[Tutorial] 霸体防御拍：等待防御超时，放行并结束这一拍");
                    EndBeat();
                    break;
                }
            }

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (ps != null && ps.Guard != null) ps.Guard.OnDamageAbsorbed -= OnGuardAbsorbed;
        _beatEndsOnAttack = true;
        _defendLocked = false;
        if (frozen) { _frozenByTutorial = false; Time.timeScale = 1f; }
        // 拍在开头就开了 → 收尾统一收掉（否则「禁止出招」会一直留着，玩家永远出不了招）
        if (_beatActive) EndBeat();

        // 内容1 收完：玩家接下来要把这只霸体敌人打死 → 那一杀的经验给足 → 升级 → 弹技能三选1
        // （三选1 由**经验升级**触发，不在这里自己弹）
        DebugLog.Info("[Tutorial] 内容1 完成：打死这只敌人 → 升级 → 弹技能三选1");

        _hintRoutine = null;
    }

    // ── ④ 技能打断拍（2026-11-01，节点二内容2）────────────────────────────
    //
    // 需求：教玩家用**主动技能**打断敌人的霸体攻击。
    //
    // 成功判据用 Enemy.attackInterrupted（只有真的被打断才置位）——
    // 不能用「敌人不再攻击」当判据：C 技自然打完也是不再攻击，会误判成功。
    // 另接受 IsSuspended（冰雾冻结把攻击 tween 挂起）——那也算「用技能把这一下压住了」。
    //
    // 这一拍**会冻结**：敌人抡起霸体技的瞬间锁住它的动作（timeScale=0），
    // 玩家从容地点技能槽 → 技能进入冷却 → 解冻 → 技能效果结算 → 敌人的霸体攻击被打断。
    // （不冻结的话玩家来不及点，这一击就糊脸上了 —— 用户：「完全无法在合适的时候暂停教玩家」）
    private IEnumerator SkillSlotInterruptRoutine(TutorialGestureStep step)
    {
        if (step.delay > 0f) yield return new WaitForSecondsRealtime(step.delay);
        if (step.lineAtWaveStart) yield return PlayWaveIntroLine();

        // 1) **先等玩家把三选一选完**（2026-11-02 用户期望的流程：
        //    「玩家点击选择3选1奖励后 → 敌人进入C技攻击 → 像防御教程那样暂停 →
        //      触发手势引导让玩家点击技能释放 → 然后变回正常流程」）。
        //
        //    旧顺序的毛病：拍在**波次开始**就开，而三选一弹窗恰好也是这一刻弹出来
        //    （实测：22.38s 冻结 / 22.40s 弹窗）→ 冻结画面和手势提示整个被弹窗盖住，
        //    玩家根本没看到「点技能」这一步；等弹窗关掉，拍已经快结束了 → 教学等于没有。
        //
        //    所以这里：**先等选卡完成，不开拍、不拦出招**（这一步是玩家自己的选择），
        //    选完之后再正式开拍。
        float readyWaited = 0f;
        while (ChosenSkill == null && readyWaited < skillChoiceWaitSeconds)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) { SkillBeatProbe("READY-WAIT stage-not-in-progress", null); _hintRoutine = null; yield break; }
            readyWaited += Time.unscaledDeltaTime;
            yield return null;
        }
        SkillBeatProbe("① 等玩家选完技能：等了 " + readyWaited.ToString("F1") + "s，选中=" + (ChosenSkill != null ? ChosenSkill.upgradeId : "(未选)"), null);
        if (ChosenSkill == null)
            DebugLog.Warning("[Tutorial] 技能打断拍：玩家还没选技能（等 " + skillChoiceWaitSeconds + "s 超时），继续");
        // 2) 等目标敌人站定
        var target = FindTargetEnemy();
        float waited = 0f;
        float limit = hintWaitForEnemySeconds + 25f;
        while (waited < limit)
        {
            if (_stageState != null && !_stageState.IsStageInProgress) { _hintRoutine = null; yield break; }
            target = FindTargetEnemy();
            if (target != null) break;
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        if (target == null) { ResetSkillBeatFlags(); _hintRoutine = null; yield break; }

        // 3) **拍一开始就禁掉出招**（对齐霸体防御拍）：
        //    等待期间玩家照打 → 教学目标被打死 → 这一拍静默结束，玩家永远学不到
        //    （2026-11-02 实测：等待期间场上敌人被打光，"存活敌人 0 只"）。
        //    台词已经交代了「用技能打断」，这段“只能看不能打”是合理的。
        bool beatStarted = false;
        if (!_beatActive)
        {
            BeginBeat(TutorialGestureTarget.BlockAll, null, holdEnemies: false);
            beatStarted = true;
            _beatEndsOnAttack = false;
            // 压暗留到真正冻结的那一刻：别在敌人抬手时就把画面压黑（用户要看抬手）
            SetSceneDim(false);
        }
        // 这一拍要玩家能真的点出技能：拦下输入时也允许释放技能
        // （否则三选1 之后那个空档里点技能没反应 —— 用户 2026-11-01 反馈）
        SkillBeatActive = true;
        GameplayInputGate.AllowSkillActivationWhileBlocked = true;
        // ⚠️ **这一拍不要让防御键一直静音**（2026-11-02 用户反馈「为什么我会受击」）：
        //    等敌人抡招的这几秒里，玩家出招被封（教学需要）但**必须能防御** → 只能站着挨打。
        //    三选1 阶段的静音在这里解除（防御拍 E0 第 1 波已经教过，防御键此时本来就该是开放的）。
        SetDefenseMuteChoice(false);

        bool frozen = false;
        bool skillUsed = false;
        bool freezeHappened = false;
        float frozenAt = 0f;
        float skillDeadline = 0f;
        SkillBeatProbe("② 开拍（暂时禁出招，等它抡霸体招）", target);

        // 4) 等技能槽出现（玩家选完就有）——**不再硬失败**：
        //    旧写法等不到就 `yield break` 跳过整拍 → 内容2 永远教不到
        //    （2026-11-02 实测：三选一恰好在这一拍开始时才弹，旧逻辑 15s 就超时跳过）。
        string wantedId = !string.IsNullOrEmpty(step.skillUpgradeId)
            ? step.skillUpgradeId
            : (ChosenSkill != null ? ChosenSkill.upgradeId : null);
        DebugLog.Info("[Tutorial] 技能打断拍：等玩家选完技能 → 已选 " + (wantedId ?? "(未指定)"));
        Vector2 slotPos = Vector2.zero;
        float waitStart = Time.unscaledTime;
        bool slotReady = TryGetSkillSlotScreenPoint(wantedId, out slotPos);
        if (!slotReady)
        {
            // 最多等 2 秒让 FrontItemBar 把图标刷出来（**不能像旧写法那样等满 60 秒**：
            // 那会把整拍堵死 → 玩家站着挨打、技能教学从头到尾没发生）。
            float slotWait = 0f;
            while (slotWait < 2f)
            {
                slotWait += Time.unscaledDeltaTime;
                yield return null;
                if (TryGetSkillSlotScreenPoint(wantedId, out slotPos)) { slotReady = true; break; }
            }
        }
        if (!slotReady)
        {
            // **兜底：按槽位序号取**（不依赖图标注册表）——
            //   教学不能因为「图标还没刷出来」就整段不教：直接按槽位序号指过去。
            int slotIndex = 0;
            var inv = ActiveSkillInventory.Instance;
            if (inv != null && !string.IsNullOrEmpty(wantedId))
            {
                for (int i = 0; i < inv.Entries.Count; i++)
                {
                    var e = inv.Entries[i];
                    if (e != null && e.definition != null && e.definition.upgradeId == wantedId) { slotIndex = i; break; }
                }
            }
            var bar = FrontItemBar.ResolveForSkills();
            slotReady = bar != null && bar.TryGetActiveSkillSlotScreenPointByIndex(slotIndex, out slotPos);
            SkillBeatProbe("技能槽定位：" + (slotReady ? "序号兜底成功 → 槽#" + slotIndex : "兜底也失败")
                + " | " + (bar != null ? bar.DebugActiveSkillSlotState() : "无 FrontItemBar"), target);
        }
        if (!slotReady)
        {
            // 实在定位不到技能槽也**不跳过这一拍**：
            //   照样冻结 + 台词 + 等玩家点技能（只是没有手势指引）——
            //   教学不能因为「找不到图标」就整段消失（那正是 2026-11-02 用户遇到的情况：
            //   玩家站着挨打、技能教学从头到尾没出现）。
            DebugLog.Warning("[Tutorial] 技能打断拍：技能槽定位失败，仍会冻结并提示玩家点技能");
        }
        bool gestureOnSlot = slotReady;
        // 技能槽图标（冻结时做高亮脉冲）
        var skillBar = FrontItemBar.ResolveForSkills();
        _skillSlotIcon = skillBar != null ? skillBar.FindActiveSkillSlotIcon(wantedId) : null;

        // 4) 台词：按玩家**实际选中的技能**给对应的一条（“用你刚选的【海浪】打断它”）
        //    （放在等槽位之后：这样才知道该念哪一条）
        var skillDialogue = SkillDialogueFor(ChosenSkill);
        // 开场两段字幕：第一段 = 开场铺垫，第二段 = 原本这里要念的那条（技能台词）
        if (TryGetIntroLine(_currentStep, out var introLine))
        {
            yield return PlayTutorialIntro(introLine, skillDialogue);
        }
        else if (skillDialogue != null)
        {
            SetTeachingDim(true);   // 教学文本 → 压暗（黑幕）
            DialogueManager.Instance?.QueueEventAndNotify(skillDialogue, forceReplay: true);
        }

        // 5) 等敌人抡起霸体技 → **抬手做完后冻结** → 提示点技能槽 → 玩家点技能 → 打断
        DebugLog.Info("[Tutorial] 技能打断拍：等敌人抡起霸体技（抬起到顶就暂停，提示你去点技能槽）");
        float attackWaited = 0f;
        float attackLimit = hintWaitForEnemySeconds + 25f;
        float dbgT = 0f;
        while (attackWaited < attackLimit)
        {
            // ⚠️ 这里必须 `break` 而不是 `yield break`：
            //    走到这一步时，「循环点技能槽」的示范（PlayTapLoop）**可能已经在跑**了。
            //    yield break 会直接跳过下面的收尾 → 那个无限协程会一直画下去
            //    → 表现为「拍结束了光圈和手还赖在屏幕上」（2026-11-02 用户反馈的残留）。
            if (_stageState != null && !_stageState.IsStageInProgress) { SkillBeatProbe("✗ 等攻击时舞台已不在进行中 → 收尾", target); break; }
            if (beatStarted && !_beatActive) { SkillBeatProbe("✗ 拍被收掉了（BeatActive=false）→ 收尾", target); break; }     // 拍被收掉了

            if (target == null || target.state == EnemyState.Dead)
            {
                // 目标没了 → 换下一只继续等（对齐霸体防御拍）
                var next = FindTargetEnemy();
                if (next != null)
                {
                    DebugLog.Info("[Tutorial] 技能打断拍：原目标已不在 → 换目标继续等霸体技");
                    target = next;
                }
                else
                {
                    attackWaited += Time.unscaledDeltaTime;
                    yield return null;
                    continue;
                }
            }

            // 玩家是否已经放出技能？（技能进入冷却 = 释放成功）
            // ⚠️ 只认**冻结之后**的释放：玩家在冻结前手痒先点了技能（旧写法会把这一下当成
            //    「教学已完成」→ 后面敌人再抡招就不冻结了 → 玩家看不到这段教学，
            //    2026-11-02 用户反馈：「获得3选1技能后下一次敌人攻击没有冻结并提示我用技能打断」）。
            if (!skillUsed && freezeHappened && IsSkillOnCooldown(wantedId))
            {
                skillUsed = true;
                skillDeadline = Time.unscaledTime + skillResultWaitSeconds;
                if (frozen)
                {
                    // 解冻 → 技能效果结算 → 敌人的霸体攻击被打断
                    _frozenByTutorial = false;
                    Time.timeScale = 1f;
                    frozen = false;
                    GameplayInputGate.AllowSkillActivationWhileFrozen = false;
                    DebugLog.Info("[Tutorial] 技能打断拍：玩家已释放技能 → 解冻，等技能生效");
                }
                else
                {
                    DebugLog.Info("[Tutorial] 技能打断拍：检测到技能已释放");
                }
            }

            if (!frozen && !skillUsed)
            {
                // 等敌人把**抬手动作做完**再冻结（与霸体防御拍同一口径）：
                // 判据 = C 技前摇的剩余时长（武器已举到位、还没落）。
                // 冻结判据：**敌人的下一次攻击**（不挑是不是 C 技）+ 抬起到顶那一瞬。
                //
                // ⚠️ 2026-11-02 用户反馈「获得3选1技能后下一次敌人攻击没有冻结」的根因：
                //    1012 的攻击是**序列轮换**的（Enemy.Attack.cs: _currentAttackStep 轮流取
                //    attackSequence 里的招式，isCFrame = isCAttack）→ 普通攻击和 C 技交替。
                //    旧判据要求 `isCFrame`（= 霸体招）→ 敌人的「下一次攻击」如果是普通攻击，
                //    这一拍就永远等不到 → 教学静默消失（实测：用户那次会话 1012 连续两次
                //    普通攻击，一次都没冻上；日志里连一行「技能打断拍」都没有）。
                //    → 现在**任何一次攻击**的抬起到顶都会冻结（教学要表达的是
                //      「他抡招的瞬间用技能打断」），C 技来了同样会冻。
                bool anyAttack = target != null && target.isAttackAnimating;
                bool cAttack = anyAttack && target.isCFrame;
                float toAttackEnd = anyAttack ? target.TimeToAttackEnd() : -1f;
                // ⚠️ **技能必须在冷却完毕**才能冻结：
                //    玩家提前把技能放了（冷却中）→ 这时候冻结 → 他点了技能槽也没反应 →
                //    教学卡死 30 秒。宁可在等技能好的时候不冻结。
                bool skillReady = !IsSkillOnCooldown(wantedId);
                if (anyAttack && toAttackEnd >= 0f && toAttackEnd <= skillPromptToEndSeconds && skillReady)
                if (Time.unscaledTime - dbgT > 1f)
                {
                    dbgT = Time.unscaledTime;
                    SkillBeatProbe("WAIT-C 敌人=" + (target == null ? "null" : target.gameObject.name + "/" + target.state)
                        + " 抡招=" + (target != null && target.isAttackAnimating)
                        + " C技=" + (target != null && target.isCFrame)
                        + " 剩余=" + toAttackEnd.ToString("F2") + "s"
                        + " 阈值=" + skillPromptToEndSeconds.ToString("F2") + "s"
                        + " 技能可用=" + (!IsSkillOnCooldown(wantedId)), target);
                }
                if (cAttack && toAttackEnd >= 0f && toAttackEnd <= skillPromptToEndSeconds)
                {
                    // —— 冻结：敌人的抬手动作被定住，玩家可以从容点技能 ——
                    Time.timeScale = 0f;
                    _frozenByTutorial = true;
                    frozen = true;
                    frozenAt = Time.unscaledTime;
                    AttackSystem.Instance?.ClearActionLock();

                    SetSceneDim(true);
                    _currentTeachAttack = null;
                    _beatEndsOnAttack = false;

                    // **时停期间要能真的点出技能**：
                    //   ActiveSkillInventory 默认禁止 timeScale=0 时放技能 → 不打开这一拍就永远教不成
                    //   （2026-11-02 实测：驱动每 2s 点一次技能，全部被 timeScale 闸门吃掉）。
                    GameplayInputGate.AllowSkillActivationWhileFrozen = true;

                    // 冻结前的旧台词先收掉：这一拍要在冻结时换一句新的（点技能图标），
                    //   不收的话两句会叠在一起。
                    var dlgView = ResolveDialogueView();
                    if (dlgView != null && dlgView.IsShowing) dlgView.Skip();

                    // **冻结瞬间把这一拍的台词推出来**（和手势示范同时出现）：
                    //   原来这一拍到冻结时**一句台词都没有** —— 玩家看到的是
                    //   「敌人举着刀停住 + 手指落在技能槽上」，但没有任何文字告诉他该干什么。
                    //   ⚠️ 冻结时 Time.timeScale = 0 → DialogueManager.IsBlockedByOtherUI()
                    //      会把队列里的台词当「被别的 UI 挡住」拦下来 →
                    //      必须临时开 AllowDialogueWhilePaused（教学专用开关）才推得上去。
                    if (step.freezeDialogue != null)
                    {
                        DialogueManager.AllowDialogueWhilePaused = true;
                        SetTeachingDim(true);   // 教学文本 → 压暗（黑幕）
                        DialogueManager.Instance?.QueueEventAndNotify(step.freezeDialogue, forceReplay: true);
                        var freezeView = ResolveDialogueView();
                        float waitShow = 0f;
                        while (freezeView != null && !freezeView.IsShowing && waitShow < 1.5f)
                        {
                            waitShow += Time.unscaledDeltaTime;
                            yield return null;
                        }
                        // 已经显示出来了 → 开关收回去（没显示出来就留着，等这一拍收尾统一收）
                        if (freezeView != null && freezeView.IsShowing) DialogueManager.AllowDialogueWhilePaused = false;
                        DebugLog.Info("[Tutorial] 技能打断拍：冻结台词：" + step.freezeDialogue.name);
                    }

                    var input = InputManager.Instance;
                    if (input != null) input.allowInputWhileTimeFrozen = true;

                    // 提示手落在技能槽上（循环点）；定位不到槽位就不画手势（教学本身仍然进行）
                    if (gestureOnSlot && gestureHint != null) gestureHint.PlayTapLoop(slotPos);
                    freezeHappened = true;
                    SkillBeatProbe("③ 已冻结：敌人抬起到顶（" + (cAttack ? "C技" : "普通招") + "）→ 点技能槽打断它", target);
                    DebugLog.Info("[Tutorial] 技能打断拍：敌人抬手完成（剩余 " + toAttackEnd.ToString("F2")
                        + "s，" + (cAttack ? "C技" : "普通招") + "）→ 已锁住动作，等待玩家点技能槽");
                }
            }
            else if (frozen)
            {
                if (Time.timeScale != 0f) Time.timeScale = 0f;

                // **技能槽高亮**：冻结期间让那个图标脉冲一下 → 玩家一眼看到该点哪里
                //   （手势提示 + 高亮，两个都要有：用户 2026-11-02 反馈「没有手势指引和高亮提示」）。
                if (_skillSlotIcon != null)
                {
                    var iconRect = _skillSlotIcon.transform as RectTransform;
                    if (iconRect != null)
                    {
                        float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
                        float sc = 1f + 0.12f * k;
                        iconRect.localScale = new Vector3(sc, sc, 1f);
                    }
                }

                if (Time.unscaledTime - frozenAt > skillBeatTimeoutSeconds)
                {
                    DebugLog.Warning("[Tutorial] 技能打断拍：等待超时，放行");
                    _frozenByTutorial = false;
                    Time.timeScale = 1f;
                    frozen = false;
                    GameplayInputGate.AllowSkillActivationWhileFrozen = false;
                    EndBeat();
                    break;
                }
            }

            // 成功判据：**玩家确实放了技能** 且 敌人的这一击被压住 ——
            //   • attackInterrupted = 真打断（旋风击飞 / 海浪击退 → CancelAttack）
            //   • IsSuspended       = 被挂起（冰系技能 / 技能命中）
            // ⚠️ 两个前提缺一不可：
            //   ① 必须先 `skillUsed`（技能真的放出去了）—— 否则会被**任何一次命中**的卡肉
            //      触发（ApplyHitStop → PushSuspend(HitStop)）→ 拍瞬间判定成功收拍
            //      → 玩家根本没机会点技能按钮 → 这一段教学等于没有
            //      （2026-11-02 实测日志：打断=False 挂起=True → 收拍）。
            //   ② 技能放出去之后最多再等 skillResultWaitSeconds；
            //      **没打断也要收拍** —— 教学不能把玩家卡在「只能防御和技能」的状态里
            //      （2026-11-02 用户反馈：「玩家点击技能后也无法回到正常流程攻击敌人」）。
            bool skillLanded = skillUsed && (target.attackInterrupted || target.IsSuspended);
            bool skillTimeout = skillUsed && Time.unscaledTime > skillDeadline;
            if (skillLanded || skillTimeout)
            {
                DebugLog.Info("[Tutorial] 技能打断拍：收拍（打断=" + target.attackInterrupted
                    + " 挂起=" + target.IsSuspended + " 卡肉=" + target.IsHitStopped
                    + " 已放技能=" + skillUsed + (skillTimeout ? " 未打断=超时收拍" : "") + "）");
                _beatCompleted = skillLanded;
                if (frozen) { _frozenByTutorial = false; Time.timeScale = 1f; frozen = false; }
                if (beatStarted && _beatActive) EndBeat();
                break;
            }

            attackWaited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (frozen) { _frozenByTutorial = false; Time.timeScale = 1f; }
        ResetSkillBeatFlags();
        ResetSkillSlotHighlight();
        // 收拍（EndBeat 会把「循环点技能槽」的示范收掉 → 光圈/手不会残留）
        if (beatStarted && _beatActive) EndBeat();
        // 收尾：恢复技能释放 + 恢复防御键（这一拍教完了）
        SkillBeatActive = false;
        GameplayInputGate.AllowSkillActivationWhileBlocked = false;
        SetDefenseMuteChoice(false);
        _hintRoutine = null;
    }

    /// <summary>技能打断拍的阶段日志（写 Editor.log，方便玩家/开发在日志里核对这一拍走到哪）。</summary>
    private static void SkillBeatProbe(string phase, Enemy target)
    {
        string line = "[Tutorial][技能拍] " + phase
            + " | t=" + Time.unscaledTime.ToString("F1")
            + " ts=" + Time.timeScale.ToString("F2")
            + " 拍=" + (TutorialStageDirector.Instance != null && TutorialStageDirector.Instance.BeatActive ? "开" : "关")
            + " 敌人hp=" + (target == null ? "-" : target.currentHealth.ToString("F0"))
            + " 打断=" + (target != null && target.attackInterrupted)
            + " 挂起=" + (target != null && target.IsSuspended);
        Debug.Log(line);
    }

    /// <summary>
    /// 把「教学技能拍」打开的**全局开关**全部收回。
    ///
    /// ⚠️ 这些是 static（进程级）→ 漏收回就会漏到正式关：
    ///   • AllowSkillActivationWhileBlocked → 技能不再受输入门/对话层拦截
    ///   • AllowSkillActivationWhileFrozen → timeScale=0 时也能放技能
    ///   • AllowDialogueWhilePaused        → 冻结时台词能推上屏
    /// 调用点：**拍正常收尾 / 拍被 StopHint 打断 / 导演被禁用** —— 缺一个都会漏。
    /// </summary>
    private void ResetSkillBeatFlags()
    {
        GameplayInputGate.AllowSkillActivationWhileBlocked = false;
        GameplayInputGate.AllowSkillActivationWhileFrozen = false;
        DialogueManager.AllowDialogueWhilePaused = false;
    }

    /// <summary>冻结时高亮的技能槽图标（脉冲缩放）。</summary>
    private BuffIcon _skillSlotIcon;

    /// <summary>把技能槽图标的高亮恢复原样（收拍时调用）。</summary>
    private void ResetSkillSlotHighlight()
    {
        if (_skillSlotIcon == null) return;
        var iconRect = _skillSlotIcon.transform as RectTransform;
        if (iconRect != null) iconRect.localScale = Vector3.one;
        _skillSlotIcon = null;
    }

    /// <summary>某个主动技能是不是已经在冷却中（= 刚被释放过）。</summary>
    private static bool IsSkillOnCooldown(string upgradeId)
    {
        var inv = ActiveSkillInventory.Instance;
        if (inv == null || !inv.UsesActiveSkills) return false;
        var entries = inv.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.definition == null) continue;
            if (!string.IsNullOrEmpty(upgradeId) && e.definition.upgradeId != upgradeId) continue;
            if (e.cooldownRemaining > 0f) return true;
        }
        return false;
    }

    /// <summary>冻结期间用非缩放时间推进玩家持枪姿态（让防御位进得去）。</summary>
    private static void DriveStanceUnscaled()
    {
        var ps = PlayerState.Instance;
        if (ps == null || ps.WeaponStance == null) return;
        ps.WeaponStance.Tick(Time.unscaledDeltaTime, ps.DefendHeld);
    }

    /// <summary>
    /// 主动技能槽的屏幕坐标。
    /// upgradeId 留空时**优先指能打断霸体的技能**（旋风 / 海浪）——
    /// 技能槽里第一个是默认送的技能，指它会教错。
    /// </summary>
    private static bool TryGetSkillSlotScreenPoint(string upgradeId, out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        // ⚠️ 场景里有**多个** FrontItemBar（只有一个配了槽位，根节点上还有个空的）→ 逐个试，
        //    别只认 Instance（2026-11-02：Instance 指向空的那个 → 永远查不到技能槽 → 没有手势指引）。
        var bars = UnityEngine.Object.FindObjectsOfType<FrontItemBar>(true);
        for (int i = 0; i < bars.Length; i++)
        {
            var bar = bars[i];
            if (bar == null) continue;
            if (!string.IsNullOrEmpty(upgradeId))
            {
                if (bar.TryGetActiveSkillSlotScreenPoint(upgradeId, out screenPos)) return true;
                continue;
            }
            foreach (var id in new[] { "active_cyclone", "active_wave" })
                if (bar.TryGetActiveSkillSlotScreenPoint(id, out screenPos)) return true;
            if (bar.TryGetActiveSkillSlotScreenPoint(null, out screenPos)) return true;
        }
        return false;
    }
}
