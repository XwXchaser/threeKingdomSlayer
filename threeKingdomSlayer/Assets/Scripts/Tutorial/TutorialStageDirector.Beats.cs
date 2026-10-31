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

    /// <summary>推这一波的台词并等它真的显示出来（forceReplay：教学台词是固定节拍）。</summary>
    private IEnumerator PushWaveDialogue()
    {
        if (_pendingWaveDialogue == null) yield break;
        DialogueManager.Instance?.QueueEventAndNotify(_pendingWaveDialogue, forceReplay: true);
        _pendingWaveDialogue = null;

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
}
