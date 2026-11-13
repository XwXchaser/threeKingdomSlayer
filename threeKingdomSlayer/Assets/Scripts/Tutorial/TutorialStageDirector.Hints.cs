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
        EndIntro();                          // 开场被打断 → 也要解冻/解锁（幂等）
        SetTeachingDim(false);               // 压暗也要撤（拍没开成就被打断的情况，EndBeat 里会提前 return）
        RestoreDialogueAutoAdvance();        // 台词自动推进恢复（幂等）
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
        // ★ 等级球教学引导：**第一拍开始就收掉**（避免与教学拍自己的手势示范撞在一起）
        if (levelOrbGuide != null) levelOrbGuide.Hide();

        if (step.delay > 0f) yield return new WaitForSecondsRealtime(step.delay);

        // 登场拍：一开波就冻结敌人并暂停把台词播完（骑兵这类先见人的场面）
        if (step.lineAtWaveStart) yield return PlayWaveIntroLine();

        // 只播登场台词、不教手势的波次（上挑已经教过，不再重复）
        if (step.kind == GestureHintKind.None)
        {
            Debug.Log("[Tutorial] 本波只播登场台词，不教手势");
            SetTeachingDim(false);   // 没有手势 → 文本读完就撤压暗（否则会一直暗着）
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

        // ⚠️ **防御键从这一刻就静音**（2026-11-05 修 / 2026-11-08 改口径）：
        //   这一拍玩家要出招 → 防御键一旦被按下就会抢走持枪姿态 →
        //   `AttackSystem` 的硬闸「防御中，不能出招」会把这一记招打不出来 → 这一拍永远收不了。
        //   旧实现是**开拍那一刻**才静音 → 前面这段（等条件 + 台词弹出 + 等敌人收招，最长几秒）
        //   是**没静音的空窗**：玩家在这段里按了防御键 → 开拍时虽然把枪收回去了，
        //   但玩家的第一下点击已经错位 → 表现为「点了没反应、这一拍过不去」。
        //   → 静音提前到「条件满足」这一刻；拍结束/被打断时统一恢复（见 Hints.cs 的收拍路径）。
        //   口径：除「教按防御键」那一拍外全静音（见 BeatShouldMuteDefense）。
        if (BeatShouldMuteDefense(step.kind)) SetDefenseMuteBeat(true);

        // ★ 连招蓄力拍（指向突刺教学）：**提前选好会打霸体招的目标**。
        //   选晚了不行：台词期间玩家/机器人可能把它打死 → 只剩别的敌人 → 这一拍教的东西就不对了。
        Enemy comboTarget = null;
        if (step.kind == GestureHintKind.ComboCharge)
        {
            comboTarget = FindCAttackTarget();
            if (comboTarget == null) comboTarget = FindTargetEnemy();
            Debug.Log("[Tutorial] 连招蓄力拍：教学目标=" + (comboTarget == null ? "无" : ("敌人#" + comboTarget.enemyId + "（会打霸体招）row=" + comboTarget.rowIndex)));
        }

        // 敌人站定后才推台词：此时台词和手势引导一起出现，玩家看得见该做什么
        // ★ 统一入口：教学拍走「开场两段字幕 + 点继续」（第一段 = 开场铺垫，第二段 = 原本那行台词）；
        //   暂停点 = **原本那行台词出现的那一刻**（就在这个位置，不是波次开始）；两段之间不留间隔。
        //
        // ★ **连招蓄力拍（指向突刺教学）例外**：它的第一句台词挪到「敌人抬手定格」那一刻再播
        //   （ComboChargeRoutine 内 → 那里弹 beforeDialogue）。
        //   这里只保留**压暗**：PushBeatDialogue 在没有台词时会 early-out（Beats.cs:277），
        //   压暗得自己设 —— 否则整拍不压暗（那是可见变化，不是本次要改的东西）。
        //   ⚠️ 引导拍期间 _pendingWaveDialogue 本来就被置空（TutorialStageDirector.cs:920-921），
        //   所以这里置空 fallback 不会吞掉任何波次台词。
        bool deferBeforeDialogue = step.kind == GestureHintKind.ComboCharge;
        if (deferBeforeDialogue) SetTeachingDim(true);
        yield return PushBeatDialogue(deferBeforeDialogue ? null : step.beforeDialogue);

        // 开始教学拍：冻结敌人（不让它们行动）、只压暗场景、只接受目标手势
        var target = ToGestureTarget(step.kind);
        if (gestureHint == null || target == TutorialGestureTarget.None)
        {
            Debug.Log($"[Tutorial] 手势拍无法开始：hint={(gestureHint != null)} target={target}");
            SetDefenseMuteBeat(false);      // 拍没开成 → 防御键静音要收回（否则永久静音）
            SetTeachingDim(false);          // 拍没开成 → 压暗也要撤
            _hintRoutine = null;
            yield break;
        }

        // 等条件期间可能已经阵亡/通关：不要再开拍，否则示范会盖在战败/胜利面板上一直循环
        if (_stageState != null && !_stageState.IsStageInProgress)
        {
            Debug.Log("[Tutorial] 舞台已结束，取消手势拍");
            SetDefenseMuteBeat(false);
            SetTeachingDim(false);
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
        // **这一拍要灰掉防御键**（2026-11-08 用户口径：除「教按防御键」那一拍外全静音）。
        //   机制：`AttackSystem` 有硬闸「防御中，不能出招」→ 玩家在这一拍里按住防御，
        //   招式就永远打不出来 → 这一拍收不了 → **教程卡死**。
        //   （教学拍期间敌人是冻住的 → 静音防御键不会让玩家挨打。）
        //   例外见 BeatShouldMuteDefense：只有「教按防御键」那一拍不静音。
        if (BeatShouldMuteDefense(step.kind)) SetDefenseMuteBeat(true);
        BeginBeat(target, TeachAttackFor(step), requiredHits: step.requiredHits,
            // ★ 连招蓄力拍**不能按住敌人**：目标必须能**起新的霸体攻击**，否则
            //   `isAttackAnimating` 永远 false → 冻结条件永远不成立
            //   → 表现就是「敌人只是不进攻击，攻击动作根本没被冻住」（2026-11-08 用户实测）。
            //   其它敌人由 routine 里的 SetEnemiesHoldExcept(target) 按住（玩家不至于挨打）。
            holdEnemies: step.kind != GestureHintKind.ComboCharge);
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
            // ★ **阶段号必须在这里初始化成 1**（2026-11-08 实测踩到）：
            //   `OnBeatEnemyDamaged` 按 `_comboChargePhase` 分流（1=点一下、2=蓄力松手），
            //   阶段号还是 0 时那一记命中会**掉进通用分支**「打中就算完成」→ 这一拍瞬间收掉
            //   （玩家只看到拍闪一下就没了，还被写成「已教过」→ 之后再也不出现）。
            _comboChargePhase = 1;
            yield return ComboChargeRoutine(step, from, to, playSeconds, comboTarget);
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

    /// <summary>某个敌人的可见本体中心（屏幕坐标）。</summary>
    private bool TryGetEnemyScreenPosition(Enemy enemy, out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var cam = Camera.main;
        if (cam == null || enemy == null) return false;
        Vector3 world = GetEnemyBodyCenter(enemy);
        screenPos = cam.WorldToScreenPoint(world);
        return screenPos.x > 0f && screenPos.x < Screen.width && screenPos.y > 0f;
    }

    /// <summary>
    /// 连招蓄力拍（指向突刺教学）—— **两段式 + 打断**（口径同「海浪技能教学」「格挡教学」）：
    ///   阶段 1：点一下（**必须命中目标**）
    ///   阶段 2：按住蓄力（**手指按在敌人位置 = 瞄准**）→ 等目标**抡起霸体招** → **冻结敌人** →
    ///           玩家松手 → 派生招式**必须打断**它
    ///
    /// 2026-11-08 用户定：
    ///   • 流程与海浪/格挡一致：**等敌人抬手**（敌人停在**霸体前摇的最后一帧**，静止等待玩家完成动作）；
    ///   • **冻结敌人的动作**（目标停住 + 后面的敌人不动、不出招）；
    ///   • **必须打断**（只是打中不算）；手指不在敌人身上松手 → 武器退回收蓄力漂浮位、要重新蓄力
    ///     → 所以要教「瞄准」；
    ///   • 等待抬手期间玩家**不能出招**（闸门 ComboHold：此时他正在 stabC2 连段里）；
    ///   • **强制教学**：不提供跳过兜底 → 但**每一段都有超时重试**，绝不能把整局卡死。
    ///
    /// ⚠️ **为什么只冻敌人、不冻整个游戏**：整局 timeScale=0 会把玩家的**连段**也冻住
    ///   （戳击视觉门 + 接续窗口都走缩放时间 → 永不过期 → 第二下/松手出不来）→ 这一拍直接死等。
    ///   所以：**世界照常走，只把敌人的动作停住**（敌人的攻击 tween 挂起 + 全场按住）。
    /// </summary>
    private System.Collections.IEnumerator ComboChargeRoutine(TutorialGestureStep step, Vector2 from, Vector2 to, float playSeconds, Enemy presetTarget)
    {
        var input = InputManager.Instance;
        var machine = UnityEngine.Object.FindObjectOfType<PlayerMoveStateMachine>();
        float demoTimer = 0f;

        // ★ 教学目标 = **会打霸体招（C 技）**的敌人（1011 这类）—— 这一拍要教的是
        //   「用指向突刺**打断**他的霸体攻击」（2026-11-08 用户定）。
        var target = presetTarget;
        if (target == null || target.state == EnemyState.Dead) target = FindCAttackTarget();
        if (target == null) target = FindTargetEnemy();
        _beatTargetEnemy = target;
        // 其它敌人按住（目标保持自由：它要抡起霸体招，玩家才有东西可打断）
        if (target != null) SetEnemiesHoldExcept(target);

        // ═══ 阶段 0：**敌人抬手到顶 → 挂起（定格）** ═══
        //   只挂敌人、**不动 Time.timeScale** → 世界照常跑：
        //   玩家在定格期间照样能点 C1、蓄力、松手；窗口/蓄力计时/戳击视觉门全部正常。
        //   冻结时机**只跟敌人有关**，与玩家是否已经点过 C1 完全无关（用户多次强调）。
        //   判据与霸体防御拍 Beats.cs:420 一致：isAttackAnimating && isCFrame && 剩余 <= 1.5s
        bool frozenAtWindup = false;
        float freezeDbgT = 0f;
        while (_beatActive && !frozenAtWindup)
        {
            if (target == null || target.state == EnemyState.Dead)
            {
                var nextT = FindCAttackTarget();
                if (nextT == null) nextT = FindTargetEnemy();
                if (nextT != null && nextT != target)
                {
                    target = nextT;
                    _beatTargetEnemy = nextT;
                    SetEnemiesHoldExcept(nextT);
                    if (TryGetEnemyScreenPosition(target, out var tp2)) { from = tp2; to = tp2; }
                    Debug.Log("[Tutorial] 连招蓄力拍：原目标已不在 → 换目标继续等抬手");
                }
                yield return null;
                continue;
            }

            bool cAtk = target.isAttackAnimating && target.isCFrame;
            float tEnd = cAtk ? target.TimeToAttackEnd() : -1f;
            if (Time.unscaledTime - freezeDbgT > 1f)
            {
                freezeDbgT = Time.unscaledTime;
                Debug.Log("[Tutorial][连招拍] WAIT 抡招=" + target.isAttackAnimating + " C技=" + target.isCFrame
                    + " 剩余=" + (target.isAttackAnimating ? target.TimeToAttackEnd().ToString("F2") : "-")
                    + "s 阈值=" + comboWindupFreezeSeconds.ToString("F2") + "s 目标=" + target.enemyId + "/" + target.state);
            }
            if (cAtk && tEnd >= 0f && tEnd <= comboWindupFreezeSeconds)
            {
                FreezeAllEnemiesForTutorial();
                frozenAtWindup = true;
                _comboTargetWasAttacking = true;
                // ★ **文本①挪到这里**（2026-11-09）：敌人抡招到顶、敌人定格这一刻才弹
                //   「让我教你点新招式，先戳一下敌人」——原本它在拍开始就弹（那时敌人还没抬手）。
                if (step.beforeDialogue != null)
                    DialogueManager.Instance?.QueueEventAndNotify(step.beforeDialogue, forceReplay: true);
                Debug.Log("[Tutorial] 连招蓄力拍：敌人抬起到顶（剩余 " + tEnd.ToString("F2") + "s）→ 敌人定格（世界照常跑）");
            }
            yield return null;
        }

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
                // 目标死了 → 换一个**会打 C 技**的活目标（示范也跟着挪过去）
                if (target == null || target.state == EnemyState.Dead)
                {
                    var next = FindCAttackTarget();
                    if (next != null)
                    {
                        target = next;
                        _beatTargetEnemy = next;
                        Vector2 nFrom = from, nTo = to;
                        if (TryGetEnemyScreenPosition(target, out var tp))
                        {
                            nTo = tp;
                            nFrom = tp;
                        }
                        from = nFrom; to = nTo;
                        Debug.Log("[Tutorial] 连招蓄力拍：原目标已不在 → 换目标继续等");
                    }
                }
                demoTimer += Time.unscaledDeltaTime;
                if (demoTimer >= playSeconds) { demoTimer = 0f; PlayGestureAt(GestureHintKind.Tap, from, to); }
                yield return null;
            }
            if (!_beatActive) break;

            // ── 阶段 2：等接续窗口打开 → 冻结招式推进（窗口一直开着）
            if (input != null) input.tutorialGestureTarget = TutorialGestureTarget.ComboHold;
            // **阶段 2（蓄力）期间防御键静音**（2026-11-05 用户定）：
            //   这一阶段玩家的持枪姿态被冻在蓄势位 → 按防御键会抢走姿态 → 蓄力/连段断掉 → 这一拍收不了。
            //   ⚠️ 「按防御会打断蓄力」这一层已由**整拍静音**接手（见上方 BeatShouldMuteDefense）→ 这里不再单独静音阶段 2。
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

            // ── 示范「按住蓄力」→ 玩家按住（手指按在目标上 = 瞄准）→ 等目标抡招 → 冻结 → 松手打断
            demoTimer = 0f;
            PlayGestureAt(GestureHintKind.Hold, from, to);
            float phase2Time = 0f;
            bool frozen = false;
            float chargeDbgT = 0f;
            while (_beatActive && _comboChargePhase == 2)
            {
                // 探针：蓄力为什么涨不上去（每秒一行）
                if (Time.unscaledTime - chargeDbgT > 1f)
                {
                    chargeDbgT = Time.unscaledTime;
                    var imD = InputManager.Instance;
                    Debug.Log("[Tutorial][连招拍] CHARGE 等级=" + (imD != null ? imD.CurrentChargeLevel : -1)
                        + " 连段蓄力=" + (imD != null && imD.comboChargeActive)
                        + " 长按=" + (imD != null && imD.IsLongPressForDebug)
                        + " 指针按下=" + (imD != null && imD.IsPointerDown)
                        + " 状态机活跃=" + (machine != null && machine.IsMoveActive)
                        + " 时钟冻=" + (machine != null && machine.Frozen)
                        + " ts=" + Time.timeScale.ToString("F2")
                        + " 阶段=" + _comboChargePhase);
                }

                // **把枪一直保持在蓄势位**（2026-11-04 用户反馈：「长枪还是没收住，会缩回去」）：
                //   枪体停在回收段中途是游戏自己的「连段蓄力保持」表现（AttackSystem.BeginComboChargeHold），
                //   但它平时跟着「手指按住」走 → 玩家还没按住时枪就自己收回去了。
                //   这里**每帧重新声明一次** → 枪一直举着；玩家中途松手（没蓄满）也还是举着 → 可以再蓄一次。
                if (atkSystem != null) atkSystem.BeginComboChargeHold();

                // 目标没了（被打死）→ 解冻 → 回阶段 1 重来
                if (target == null || target.state == EnemyState.Dead)
                {
                    UnfreezeAllEnemiesForTutorial();
                    frozen = false;
                    if (machine != null) machine.NotifyExternalReset();
                    _comboChargePhase = 1;
                    break;
                }

                // ★ **打空了 → 提示重打这一段连招（拍不结束）**
                //   派生招式照常放出去；这里按「这一招打的列」判断有没有朝目标打。
                //   ⚠️ 不能靠「状态机空闲」判空挥：阶段 2 里状态机时钟是冻着的 → 那个条件永远不成立。
                if (_comboChargeAttackFired)
                {
                    int targetCol = target != null ? target.columnIndex : -1;
                    bool hitTarget = target != null && _beatLastAttackColumn == targetCol;
                    if (!hitTarget)
                    {
                        Debug.Log("[Tutorial] 连招蓄力拍：这一下没朝目标打出去（列=" + _beatLastAttackColumn
                            + " 目标列=" + targetCol + "）→ 回到阶段 1 重打这一段连招");
                        // ★ **不弹任何文本**（2026-11-09 用户定）：流程回到阶段 1（重新点戳），
                        //   此刻弹「长按蓄力」是错位的 → 只靠手势示范 + 日志提示。
                        if (machine != null) machine.NotifyExternalReset();
                        _comboChargePhase = 1;
                        break;
                    }
                }

                phase2Time += Time.unscaledDeltaTime;

                demoTimer += Time.unscaledDeltaTime;
                if (demoTimer >= playSeconds) { demoTimer = 0f; PlayGestureAt(GestureHintKind.Hold, from, to); }
                yield return null;
            }
            if (_beatActive && _comboChargePhase == 1) continue;   // 空挥兜底 → 回到阶段 1 再来一次
            break;
        }

        UnfreezeAllEnemiesForTutorial();   // 收尾：教学冻结必须解除（否则全场敌人僵住）
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

    private void FreezeAllEnemiesForTutorial()
    {
        var alive = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (alive != null)
        {
            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                if (e == null) continue;
                e.PushSuspend(Enemy.SuspendSource.Tutorial);
            }
        }
        SetEnemiesHold(true);
    }

    private void UnfreezeAllEnemiesForTutorial()
    {
        var alive = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (alive != null)
        {
            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                if (e == null) continue;
                e.PopSuspend(Enemy.SuspendSource.Tutorial);
            }
        }
        SetEnemiesHold(false);
    }

    /// <summary>
    /// 场上还有没有**玩家够得着**的活目标（第 0~1 排）。
    /// （玩家的普通戳击打不到更远的排 → 没有可打目标时这一拍就永远收不了 → 教学卡死。）
    /// </summary>
    private bool HasReachableTarget()
    {
        var alive = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (alive == null) return false;
        for (int i = 0; i < alive.Count; i++)
        {
            var e = alive[i];
            if (e == null || e.state == EnemyState.Dead) continue;
            if (e.rowIndex <= 1) return true;
        }
        return false;
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
        // **防御键静音**（2026-11-08 用户口径：除「教按防御键」那一拍外全静音）。
        //   放在这里 = **所有拍型都覆盖**（招式拍 / 霸体防御拍 / 技能打断拍 / 时停招架拍 …），
        //   收拍时由 `EndBeat` 统一恢复（见 Hints.cs）。
        //   招式拍那条路径还会**提前**到「条件满足」那一刻静音（BUG-009 的修法）。
        SetDefenseMuteBeat(_currentStep != null && BeatShouldMuteDefense(_currentStep.kind));
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
        // 教学文本的压暗（黑幕）：手势做完 → 撤（用户 2026-11-06 定：「一直压到手势做完」）
        SetTeachingDim(false);
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
        // ★ 连招蓄力拍的两个「枪停在蓄势位」表现也要收回：否则拍被打断/提前结束时
        //   长枪会**一直悬停在空中**（2026-11-08 用户实测）。两者都是幂等的。
        AttackSystem.Instance?.EndComboChargeHold();
        var psEnd = PlayerState.Instance;
        if (psEnd != null) psEnd.freezeWeaponStance = false;
        UnfreezeWorldForTutorial();   // ★ 教学冻结（指向突刺拍）必须解除 —— 否则整局停在时停
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
        // 连招蓄力拍的阶段号也要收回（拍被打断/收尾时清干净 → 下一拍不会带着旧阶段号开跑）
        _comboChargePhase = 0;

        // 收拍后的补充台词：**每一拍都要能播**。
        //   原来只在 RegisterBeatHit 里播 → 而那要求「这一拍是靠出招收拍的」
        //   → 「不靠出招收拍」的拍（霸体防御拍 / 技能打断拍）挂了 afterDialogue 也**永远播不出来**
        //   （2026-11-04 实测：技能打断拍那句「用技能打断它」从来没出现过）。
        if (playAfterDialogue && _currentStep != null && _currentStep.afterDialogue != null)
        {
            DialogueManager.Instance?.QueueEventAndNotify(_currentStep.afterDialogue, forceReplay: true);
            DebugLog.Info("[Tutorial] 补播收尾台词：" + _currentStep.afterDialogue.name);
        }

        // ★ 开场两段字幕的第二段文本**留到玩家做出手势**（需求：不自动消失）→ 拍收尾时才清。
        //   文本由上面的 view.Skip() 清掉；这里把「台词自动推进」恢复回去。
        RestoreDialogueAutoAdvance();
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
