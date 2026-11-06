using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手势提示 —— 教学关导演的**手势提示**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    private void StartHint(int waveIndex, bool stopFirst = true)
    {
        if (stopFirst) StopHint();
        if (gestureHint == null || _stageState == null || _stageState.StageConfig == null || gestureSteps == null)
        {
            Debug.Log($"[Tutorial] 手势未启动：hint={(gestureHint != null)} stage={(_stageState != null)} cfg={(_stageState != null && _stageState.StageConfig != null)} steps={(gestureSteps != null)}");
            return;
        }
        var battle = _stageState.StageConfig;
        for (int i = 0; i < gestureSteps.Length; i++)
        {
            var step = gestureSteps[i];
            if (step == null || step.waveIndex != waveIndex) continue;
            if (step.battle != null && step.battle != battle) continue;
            Debug.Log($"[Tutorial] 命中手势步骤 {i} wave={waveIndex} kind={step.kind} pause={step.pauseUntilPerformed}");
            _currentStep = step;
            // **这一拍已经教过（存档标记）→ 跳过**（2026-11-05 用户口径：
            //   「我已完成点戳的教程，就应该在存档里标记完成，而不是根据关卡通过来判断」）。
            //   ⚠️ 跳过时必须把「这一拍教出来的东西」补上（招式白名单 / 防御键开放），
            //      否则教学关里玩家打不出招 → 教学关直接卡住。
            if (IsBeatAlreadyTaught(step))
            {
                ApplyBeatOutcome(step);
                Debug.Log($"[Tutorial] 手势步骤 {i} wave={waveIndex} kind={step.kind} **已教过（存档标记）→ 跳过**");
                return;
            }
            // 连点教学（TapCombo）：**光圈常驻**在场上（玩家一直看得见「点这里」）
            if (gestureHint != null) gestureHint.keepRingVisible = (step.kind == GestureHintKind.TapCombo);
            // 教到「防御按键」这一拍 → 防御键从这一刻起才开放给玩家
            if (step.kind == GestureHintKind.DefendButton) _defendTaught = true;
            // 路线出口（负数波次）不走暂停门槛：那一拍没有战斗，只需把光标停在箭头上
            if (waveIndex < 0)
            {
                _hintRoutine = StartCoroutine(RouteHintRoutine(step));
                _routeHintRoutine = _hintRoutine;
            }
            else if (step.freezeOnCAttack)
            {
                // 霸体时停防御拍（2026-11-01）：敌人抡起霸体技时冻结 → 教按住防御键 → 解冻 → 这一击被吸收
                _hintRoutine = StartCoroutine(DefendAtCAttackRoutine(step));
            }
            else if (step.kind == GestureHintKind.SkillSlot)
            {
                // 技能打断拍（2026-11-01）：提示点技能槽 → 技能释放 → 敌人的霸体攻击被打断
                _hintRoutine = StartCoroutine(SkillSlotInterruptRoutine(step));
            }
            else if (step.freezeOnGesture)
            {
                // 时停拍（招架）：冻结整个游戏，玩家在冻结中做手势
                _hintRoutine = StartCoroutine(FreezeParryRoutine(step));
            }
            else if (step.holdAfterGesture)
            {
                // 防御拍：上划后按住 → 扛住 109 冲锋
                _hintRoutine = StartCoroutine(DefendHoldRoutine(step));
            }
            else if (step.kind == GestureHintKind.DefendButton)
            {
                // 防御按键拍（2026-10-29）：点一下防御键 → 举枪 → 落防御位 → 扛住这一击
                _hintRoutine = StartCoroutine(DefendHoldRoutine(step));
            }
            else
            {
                _hintRoutine = StartCoroutine(HintRoutine(step));
            }
            return;
        }
        Debug.Log($"[Tutorial] 没有匹配的手势步骤 wave={waveIndex} battle={battle.name}");
    }

    private void StopHint()
    {
        if (_waveRoutine != null)
        {
            StopCoroutine(_waveRoutine);
            _waveRoutine = null;
        }
        if (_hintRoutine != null)
        {
            // 【诊断】这一拍是被谁掐掉的？堆栈直接写进 Editor.log
            Debug.LogWarning("[Tutorial] StopHint：中断当前手势拍\n" + System.Environment.StackTrace);
            StopCoroutine(_hintRoutine);
            _hintRoutine = null;
        }
        _routeHintRoutine = null;
        _routeHintActive = false;
        // 登场台词的暂停如果被打断（转场/阵亡），必须把时间恢复，否则回主菜单也是卡住的
        if (_introPaused)
        {
            _introPaused = false;
            Time.timeScale = 1f;
        }
        // 教学拍被打断（波次结束/关场景）时必须把敌人、压暗、手势过滤全部恢复，否则整局卡死
        EndBeat(playAfterDialogue: false);   // 被打断的拍不补播收尾台词
        ResetSkillBeatFlags();               // 技能拍的全局开关（static）必须收回，否则漏到正式关
        SetDefenseMuteChoice(false);         // 静音也必须收回（拍被打断 → 这一拍的教学窗口结束了）
        SetDefenseMuteBeat(false);          // 需要出招的拍的静音同样收回
        // 正在门槛里被打断时必须走 Cancel：它会恢复 timeScale 和游戏输入
        if (gestureHint != null)
        {
            if (gestureHint.IsGating) gestureHint.Cancel();
            else gestureHint.Hide();
        }
        // 时停拍被打断：必须解冻，否则整局卡在 timeScale=0
        if (_frozenByTutorial)
        {
            _frozenByTutorial = false;
            Time.timeScale = 1f;
        }
        var view = ResolveDialogueView();
        if (view != null) view.PointerAdvanceEnabled = true;
    }

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
            if (gestureHint != null) gestureHint.PlayTap(pos);
            yield return new WaitForSecondsRealtime(playSeconds);
            if (!_routeHintActive) break;
            SafeHideGestureHint();
            if (step.gap > 0f) yield return new WaitForSecondsRealtime(step.gap);
            if (TryGetRouteChoiceScreenPosition(out var latest)) pos = latest;
        }

        SafeHideGestureHint();
        _routeHintActive = false;
        _routeHintRoutine = null;
        _hintRoutine = null;
    }

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
                if (_stageState != null && !_stageState.IsStageInProgress)
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

        // ⚠️ **防御键从这一刻就静音**（2026-11-05 修）：
        //   这一拍需要玩家出招 → 防御键一旦被按下就会抢走持枪姿态 →
        //   `AttackSystem` 的硬闸「防御中，不能出招」会把这一记招打不出来 → 这一拍永远收不了。
        //   旧实现是**开拍那一刻**才静音 → 前面这段（等条件 + 台词弹出 + 等敌人收招，最长几秒）
        //   是**没静音的空窗**：玩家在这段里按了防御键 → 开拍时虽然把枪收回去了，
        //   但玩家的第一下点击已经错位 → 表现为「点了没反应、这一拍过不去」。
        //   → 静音提前到「条件满足」这一刻；拍结束/被打断时统一恢复（见 Hints.cs 的收拍路径）。
        if (BeatNeedsAttack(step.kind)) SetDefenseMuteBeat(true);

        // 敌人站定后才推台词：此时台词和手势引导一起出现，玩家看得见该做什么
        BattleDialogueView view = null;
        // 台词来源：**波次台词**（教学关）或 **这一拍自带的 beforeDialogue**（招式解锁引导的一行文字）
        var beatDialogue = _pendingWaveDialogue != null ? _pendingWaveDialogue : step.beforeDialogue;
        if (beatDialogue != null)
        {
            _pendingWaveDialogue = null;
            // 强制重播：教学台词是教学关的固定节拍，不能因为上一次通关就永久不再播
            DialogueManager.Instance?.QueueEventAndNotify(beatDialogue, forceReplay: true);

            // 等对话真的显示出来再弹手势；否则手势出现时屏幕上一个字也没有
            view = ResolveDialogueView();
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
            SetDefenseMuteBeat(false);      // 拍没开成 → 防御键静音要收回（否则永久静音）
            _hintRoutine = null;
            yield break;
        }

        // 等条件期间可能已经阵亡/通关：不要再开拍，否则示范会盖在战败/胜利面板上一直循环
        if (_stageState != null && !_stageState.IsStageInProgress)
        {
            Debug.Log("[Tutorial] 舞台已结束，取消手势拍");
            SetDefenseMuteBeat(false);
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

        // 示范位置：**每轮重新取活着的目标敌人**（2026-11-04 用户反馈：
        // 「已击杀敌人但是光圈没有消失」—— 示范位置是拍开始时取的固定屏幕坐标，
        // 敌人被打死后光圈就停在尸体上。目标换了就把示范挪过去；没有活目标就收掉提示层。
        Enemy demoTarget = null;
        Vector2 from = ScreenPoint(step.from01);
        Vector2 to = ScreenPoint(step.to01);

        // 这一拍是「招式打出去就算数」的拍 → 打开出招信号。
        // ⚠️ 必须在这里显式打开：教学关的**防御拍 / 技能拍**会把 `_beatEndsOnAttack` 关成 false
        //    （它们的完成判据不是出招），而**从来没人复位** → 之后所有招式拍都收不了拍
        //    （正式关的「连招蓄力」教学拍就永远停在阶段 2 → 整条「教学→天赋→招式教学」链路卡死，
    //      2026-11-04 实测才发现）。
        _beatEndsOnAttack = true;
        // **这一拍需要玩家出招 → 防御键静音**（2026-11-05 用户反馈「按防御会导致教程卡死」）。
        //   机制：`AttackSystem` 有硬闸「防御中，不能出招」→ 玩家在这一拍里按住防御，
        //   招式就永远打不出来 → 这一拍收不了 → **教程卡死**。
        //   （教学拍期间敌人是冻住的 → 静音防御键不会让玩家挨打。）
        //   例外见 BeatNeedsAttack：防御拍（教按防御键）与技能拍（点技能槽）不静音。
        if (BeatNeedsAttack(step.kind)) SetDefenseMuteBeat(true);
        BeginBeat(target, TeachAttackFor(step), requiredHits: step.requiredHits);
        Debug.Log($"[Tutorial] 手势拍开始 wave={step.waveIndex} kind={step.kind} target={target} 敌人={CountAliveEnemies()}" +
                  $" | 台词={(step.beforeDialogue != null ? "与示范同时:" + step.beforeDialogue.name : (step.afterDialogue != null ? "收拍后:" + step.afterDialogue.name : "无"))}" +
                  $" | 示范={step.from01}→{step.to01}");

        float playSeconds = step.playSeconds > 0f ? step.playSeconds : 1.1f;
        // 蓄力上挑的示范本身要播「按住 1.2s + 上划 1.1s」，播放时长必须跟它对齐，
        // 否则会在示范没播完时就重开一轮，看起来像教程被“多调了一次”
        if (step.kind == GestureHintKind.ChargedSwipe) playSeconds = Mathf.Max(playSeconds, 2.5f);
        // 连点教学：提示手势自己循环（单次点按会在播完后把整个提示层收起来 → 只剩光圈没有手指）
        bool loopingGesture = step.kind == GestureHintKind.TapCombo;
        RefreshDemoTarget(step, ref demoTarget, ref from, ref to);
        AssertDemoDirectionMatches(step);

        // 连招蓄力拍：两段式（点一下 → 在窗口里按住蓄满 → 松手），单独一套流程
        if (step.kind == GestureHintKind.ComboCharge)
        {
            yield return ComboChargeRoutine(step, from, to, playSeconds);
            yield break;
        }

        if (loopingGesture) PlayGestureAt(step.kind, from, to);
        while (_beatActive)
        {
            if (_stageState != null && !_stageState.IsStageInProgress)
            {
                Debug.Log("[Tutorial] 教学拍期间舞台结束，结束手势拍");
                EndBeat();
                break;
            }
            // 目标换了（被打死了）→ 示范挪到新目标上（循环手势要重播一遍才会跟过去）
            bool targetChanged = RefreshDemoTarget(step, ref demoTarget, ref from, ref to);
            if (targetChanged && loopingGesture) PlayGestureAt(step.kind, from, to);
            if (!loopingGesture) PlayGestureAt(step.kind, from, to);
            yield return new WaitForSecondsRealtime(playSeconds);
            if (!_beatActive) break;
            if (!loopingGesture) gestureHint.Hide();
            if (step.gap > 0f) yield return new WaitForSecondsRealtime(step.gap);
        }

        EndBeat();
        _hintRoutine = null;
    }

    /// <summary>
    /// **自检：示范的滑动方向必须和这一拍要求的手势一致。**
    ///
    /// 2026-11-04 用户实测踩过：引导拍没设 from01/to01 → 用了类默认值
    /// (0.5,0.5)→(0.5,0.72) = **向上挑**，而闸门要求的是**横划** →
    /// 示范教上挑、判定要横划 → 玩家照着做根本过不了，而且一眼看得出手势是错的。
    /// 这条自检对**所有**拍生效（教学关的拍也查）→ 方向写错会直接报 Error，不会再无声溜过去。
    /// </summary>
    private void AssertDemoDirectionMatches(TutorialGestureStep step)
    {
        if (step == null) return;
        if (step.kind != GestureHintKind.Swipe && step.kind != GestureHintKind.ChargedSwipe) return;
        var delta = step.to01 - step.from01;
        var target = ToGestureTarget(step.kind);
        bool wantHorizontal = target == TutorialGestureTarget.SwipeHorizontal;
        bool isHorizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
        if (wantHorizontal == isHorizontal) return;
        Debug.LogError($"[Tutorial] 手势示范方向与这一拍要求不一致！kind={step.kind} 目标={target}" +
                       $" 示范 {step.from01} → {step.to01}（delta={delta}）要求={(wantHorizontal ? "横划" : "竖划")}" +
                       $" → 玩家会照着错的方向做");
    }

    /// <summary>
    /// **连招蓄力拍**（2026-11-04）：教「先点一记戳，再在接续窗口里按住蓄满 → 松手 = 派生招式」。
    ///
    /// 两段**独立**输入（和真实连招一致）：
    ///   阶段 1：闸门 = Tap → 玩家点一下（松手）→ 一记戳真的打出去并打中敌人。
    ///   阶段 2：**等接续窗口打开** → **冻结招式状态机的时钟**（窗口一直开着，玩家不用抢那 0.2 秒）
    ///            → 闸门 = ComboHold（只接受「蓄满后松手」）→ 玩家按住 → 蓄满 → 松手（松在敌人身上）
    ///            → 派生招式真的打出去并打中敌人 → 收拍。
    ///
    /// ⚠️ 冻结的是**招式状态机的时钟**（<see cref="PlayerMoveStateMachine.SetFrozen"/>），**不是全局 timeScale**：
    ///    全局冻结会让 <c>InputManager.IsPointerDown</c>（要求 timeScale&gt;0）判为未按下 → 连招蓄力直接断。
    /// ⚠️ 阶段 2 的闸门不能用 <c>BlockAll</c>：教学闸门在手势路径的**第 1 道**，
    ///    排在「连招释放」那道前面 → BlockAll 会把连招的松手一起 drop 掉（所以单独开了 ComboHold）。
    /// </summary>
    private System.Collections.IEnumerator ComboChargeRoutine(TutorialGestureStep step, Vector2 from, Vector2 to, float playSeconds)
    {
        var input = InputManager.Instance;
        var machine = UnityEngine.Object.FindObjectOfType<PlayerMoveStateMachine>();
        float demoTimer = 0f;

        while (_beatActive)
        {
            // ── 阶段 1：示范「点一下」→ 玩家点出一记戳（命中由 OnBeatEnemyDamaged 推进阶段）
            // **先把招式状态机打回中立**（2026-11-04 实测）：
            // 「等敌人到位」的那几秒里玩家/机器人可以自由点 → 已经连点成 Jab1→Jab2→Jab3，
            // 而「指向突刺」的连段边只挂在 **Jab1** 上 → 从 Jab3 起就永远没有出路了 → 这一拍收不了。
            // 回到中立后，玩家的那一记戳会从 Jab1 起 → 才能接上蓄满松手。
            if (machine != null) machine.NotifyExternalReset();
            // **把枪冻在举着的位置**（不冻的话枪会在蓄力期间自己收回 → 教学流程对不上）
            var ps = PlayerState.Instance;
            if (ps != null) ps.freezeWeaponStance = true;
            _comboChargePhase = 1;
            if (input != null) input.tutorialGestureTarget = TutorialGestureTarget.Tap;
            demoTimer = 0f;
            PlayGestureAt(GestureHintKind.Tap, from, to);
            while (_beatActive && _comboChargePhase == 1)
            {
                demoTimer += Time.unscaledDeltaTime;
                if (demoTimer >= playSeconds) { demoTimer = 0f; PlayGestureAt(GestureHintKind.Tap, from, to); }
                yield return null;
            }
            if (!_beatActive) break;

            // ── 阶段 2：等接续窗口打开 → 冻结招式推进（窗口一直开着）
            if (input != null) input.tutorialGestureTarget = TutorialGestureTarget.ComboHold;
            // **阶段 2（蓄力）期间防御键静音**（2026-11-05 用户定）：
            //   这一阶段玩家的持枪姿态被冻在蓄势位 → 按防御键会抢走姿态 → 蓄力/连段断掉 → 这一拍收不了。
            //   ⚠️ 「按防御会打断蓄力」这一层已由**整拍静音**接手（见上方 BeatNeedsAttack）→ 这里不再单独静音阶段 2。
            SetDefenseMuteBeat(true);
            float winWait = 0f;
            bool windowOk = false;
            while (_beatActive && winWait < 3f)
            {
                if (machine == null) { windowOk = true; break; }
                if (!machine.IsMoveActive) break;                     // 这一记戳打得太晚 → 窗口已经错过
                if (machine.IsWindowOpenFor(MoveGesture.Hold, 1)) { windowOk = true; break; }
                winWait += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!_beatActive) break;
            if (!windowOk)
            {
                // 接续窗口没赶上 → 回到阶段 1 让玩家**再点一记**（不能干等，否则这一拍就废了）
                // （回到阶段 1 → 防御键要恢复：玩家这时可以防御，只是不能再蓄力）
                SetDefenseMuteBeat(false);
                Debug.Log("[Tutorial] 连招蓄力拍：接续窗口没赶上 → 回阶段 1（再点一记）");
                continue;
            }
            if (machine != null)
            {
                machine.SetFrozen(true);
                Debug.Log("[Tutorial] 连招蓄力拍：接续窗口已打开 → 冻结招式推进（玩家可以从容蓄力）");
            }
            // **把长枪拉回蓄势位**（2026-11-04 用户反馈：「长枪提前收回」）：
            // 玩家还没按住时，枪不能自己收回去 → 用游戏自己的「连段蓄力保持」表现把枪定在蓄势姿势。
            var atkSystem = AttackSystem.Instance;
            if (atkSystem != null) atkSystem.BeginComboChargeHold();
            // 阶段 2 的台词：「按住蓄满 → 松手」
            if (step.comboPhase2Dialogue != null)
                DialogueManager.Instance?.QueueEventAndNotify(step.comboPhase2Dialogue, forceReplay: true);

            // ── 示范「按住蓄力」→ 玩家按住 → 蓄满 → 松手 → 派生招式打中敌人
            demoTimer = 0f;
            PlayGestureAt(GestureHintKind.Hold, from, to);
            while (_beatActive && _comboChargePhase == 2)
            {
                // **把枪一直保持在蓄势位**（2026-11-04 用户反馈：「长枪还是没收住，会缩回去」）：
                //   枪体停在回收段中途是游戏自己的「连段蓄力保持」表现（AttackSystem.BeginComboChargeHold），
                //   但它平时跟着「手指按住」走 → 玩家还没按住时枪就自己收回去了。
                //   这里**每帧重新声明一次** → 枪一直举着；玩家中途松手（没蓄满）也还是举着 → 可以再蓄一次。
                if (atkSystem != null) atkSystem.BeginComboChargeHold();
                demoTimer += Time.unscaledDeltaTime;
                if (demoTimer >= playSeconds) { demoTimer = 0f; PlayGestureAt(GestureHintKind.Hold, from, to); }
                yield return null;
            }
            break;
        }

        if (machine != null) machine.SetFrozen(false);
        var atkEnd = AttackSystem.Instance;
        if (atkEnd != null) atkEnd.EndComboChargeHold();
        var psEnd = PlayerState.Instance;
        if (psEnd != null) psEnd.freezeWeaponStance = false;
        SetDefenseMuteBeat(false);      // 蓄力阶段结束（或这一拍被中止）→ 恢复防御键
        _comboChargePhase = 0;
        EndBeat();
        _hintRoutine = null;
    }

    /// <summary>
    /// 重新取「活着的目标敌人」并把示范位置贴到它身上。
    /// 返回「目标是否换了」—— 换了的话，循环手势需要重播一遍才会跟过去。
    /// 没有活目标时收掉提示层（光圈不能停在尸体上）。
    /// </summary>
    private bool RefreshDemoTarget(TutorialGestureStep step, ref Enemy lastTarget, ref Vector2 from, ref Vector2 to)
    {
        var enemy = FindTargetEnemy();
        bool changed = enemy != lastTarget;
        var baseFrom = ScreenPoint(step.from01);
        var baseTo = ScreenPoint(step.to01);
        if (enemy != null && TryGetTargetEnemyScreenPosition(out var enemyScreen))
        {
            to = enemyScreen + (baseTo - baseFrom);
            from = enemyScreen;
        }
        lastTarget = enemy;
        if (changed && enemy == null)
        {
            Debug.Log("[Tutorial] 教学目标已无 → 收掉手势提示层");
            SafeHideGestureHint();
        }
        return changed;
    }

    private void BeginBeat(TutorialGestureTarget target, AttackType? teachAttack, bool holdEnemies = true, int requiredHits = 1)
    {
        _beatActive = true;
        _beatTarget = target;
        _currentTeachAttack = teachAttack;
        _beatCompleted = false;
        _beatHitCount = 0;
        _beatRequiredHits = Mathf.Max(1, requiredHits);
        _beatAttackFired = false;
        // 招架拍**不能冻敌人**（敌人必须真的挥下来才能被格开）→ holdEnemies=false
        if (holdEnemies) SetEnemiesHold(true);
        SetSceneDim(true);
        var input = InputManager.Instance;
        if (input != null)
        {
            input.tutorialGestureTarget = target;
            input.OnAttackExecuted -= OnTutorialAttackExecuted;
            input.OnAttackExecuted += OnTutorialAttackExecuted;
        }
        // 教学进度：把白名单刷成「已教完的 + 当前这一拍要教的」
        ApplyAttackGate(teachAttack);
        // 「攻击成功」要确认真的打中敌人 → 监听场上敌人的受击
        WatchEnemiesForDamage();
    }

    /// <summary>监听场上敌人的受击（用来确认「攻击成功」= 真的打中了人）。</summary>
    private void WatchEnemiesForDamage()
    {
        UnwatchEnemiesForDamage();
        var alive = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (alive == null) return;
        for (int i = 0; i < alive.Count; i++)
        {
            var e = alive[i];
            if (e == null) continue;
            e.OnDamageTaken += OnBeatEnemyDamaged;
            _beatDamageWatchers.Add(e);
        }
    }

    private void UnwatchEnemiesForDamage()
    {
        for (int i = 0; i < _beatDamageWatchers.Count; i++)
        {
            var e = _beatDamageWatchers[i];
            if (e != null) e.OnDamageTaken -= OnBeatEnemyDamaged;
        }
        _beatDamageWatchers.Clear();
    }

    /// <param name="playAfterDialogue">
    /// false = 这一拍是**被打断**的（波次结束 / 切场景 / StopHint）→ 不补播收尾台词。
    /// </param>
    private void EndBeat(bool playAfterDialogue = true)
    {
        if (!_beatActive) return;
        _beatActive = false;
        UnwatchEnemiesForDamage();
        _beatAttackFired = false;
        var input = InputManager.Instance;
        if (input != null)
        {
            input.OnAttackExecuted -= OnTutorialAttackExecuted;
            input.tutorialGestureTarget = TutorialGestureTarget.None;
            input.allowInputWhileTimeFrozen = false;   // 时停结束 → 恢复正常输入规则
        }
        // 时停：解冻（必须在恢复输入之前，否则玩家会在冻结态里继续出招）
        if (_frozenByTutorial)
        {
            _frozenByTutorial = false;
            Time.timeScale = 1f;
        }
        SetEnemiesHold(false);
        SetSceneDim(false);
        SafeHideGestureHint();
        // 这一拍结束 → 「需要出招的拍」的防御键静音随之解除（否则两拍之间防御键一直是死的）
        SetDefenseMuteBeat(false);
        // 做完立刻收掉台词：玩家要能马上看清自己打出了什么
        var view = ResolveDialogueView();
        if (view != null && view.IsShowing) view.Skip();
        // 这一拍真的教完了 → 该招式转为"已教"（之后不再锁）
        if (_beatCompleted && _currentTeachAttack.HasValue)
        {
            _taughtAttacks.Add(_currentTeachAttack.Value);
            ApplyAttackGate(null);
        }
        // 招式解锁引导：**真的打出去**才算教完 → 写存档「已教过」（没打出去下次还会教）
        if (_beatCompleted)
        {
            MarkGuideTaughtIfNeeded();
            // 教学关的手势拍也一样：**每教完一拍就写存档标记**（不看教学关通不通）
            MarkTutorialBeatTaughtIfNeeded();
        }
        _beatCompleted = false;

        // 收拍后的补充台词：**每一拍都要能播**。
        //   原来只在 RegisterBeatHit 里播 → 而那要求「这一拍是靠出招收拍的」
        //   → 「不靠出招收拍」的拍（霸体防御拍 / 技能打断拍）挂了 afterDialogue 也**永远播不出来**
        //   （2026-11-04 实测：技能打断拍那句「用技能打断它」从来没出现过）。
        if (playAfterDialogue && _currentStep != null && _currentStep.afterDialogue != null)
        {
            DialogueManager.Instance?.QueueEventAndNotify(_currentStep.afterDialogue, forceReplay: true);
            DebugLog.Info("[Tutorial] 补播收尾台词：" + _currentStep.afterDialogue.name);
        }
    }

    private void PlayGestureAt(GestureHintKind kind, Vector2 from, Vector2 to)
    {
        if (gestureHint == null) return;
        switch (kind)
        {
            case GestureHintKind.Tap: gestureHint.PlayTap(from); break;
            // 连点教学：必须用**循环**点按（单次点按播完会把整个提示层收起来 → 只剩光圈、没有手指）
            case GestureHintKind.TapCombo: gestureHint.PlayTapLoop(from); break;
            case GestureHintKind.Hold: gestureHint.PlayHold(from); break;
            case GestureHintKind.Swipe: gestureHint.PlaySwipe(from, to); break;
            case GestureHintKind.ChargedSwipe: gestureHint.PlayChargedSwipe(from, to); break;
            case GestureHintKind.Parry: gestureHint.PlayParry(from, to); break;
            case GestureHintKind.ParryHold: gestureHint.PlayParryHold(from, to); break;
            case GestureHintKind.DefendButton: gestureHint.PlayPressHoldLoop(from); break;   // 防御键：按住（hold）
        }
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
            case GestureHintKind.TapCombo: gestureHint.PlayTapLoop(from); break;
            case GestureHintKind.Hold: gestureHint.PlayHold(from); break;
            case GestureHintKind.Swipe: gestureHint.PlaySwipe(from, to); break;
            case GestureHintKind.ChargedSwipe: gestureHint.PlayChargedSwipe(from, to); break;
            case GestureHintKind.Parry: gestureHint.PlayParry(from, to); break;
            case GestureHintKind.ParryHold: gestureHint.PlayParryHold(from, to); break;
            case GestureHintKind.DefendButton: gestureHint.PlayPressHoldLoop(from); break;
        }
    }
}
