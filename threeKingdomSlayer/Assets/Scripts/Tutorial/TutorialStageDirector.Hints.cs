using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 手势提示 —— 教学关导演的**手势提示**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    private void StartHint(int waveIndex)
    {
        StopHint();
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
            // 教到「防御按键」这一拍 → 防御键从这一刻起才开放给玩家
            if (step.kind == GestureHintKind.DefendButton) _defendTaught = true;
            // 路线出口（负数波次）不走暂停门槛：那一拍没有战斗，只需把光标停在箭头上
            if (waveIndex < 0)
            {
                _hintRoutine = StartCoroutine(RouteHintRoutine(step));
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

        // 敌人站定后才推台词：此时台词和手势引导一起出现，玩家看得见该做什么
        BattleDialogueView view = null;
        if (_pendingWaveDialogue != null)
        {
            // 强制重播：教学台词是教学关的固定节拍，不能因为上一次通关就永久不再播
            DialogueManager.Instance?.QueueEventAndNotify(_pendingWaveDialogue, forceReplay: true);
            _pendingWaveDialogue = null;

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
            _hintRoutine = null;
            yield break;
        }

        // 等条件期间可能已经阵亡/通关：不要再开拍，否则示范会盖在战败/胜利面板上一直循环
        if (_stageState != null && !_stageState.IsStageInProgress)
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

        BeginBeat(target, AttackTypeFor(step.kind));
        Debug.Log($"[Tutorial] 手势拍开始 wave={step.waveIndex} kind={step.kind} target={target} 敌人={CountAliveEnemies()}");

        float playSeconds = step.playSeconds > 0f ? step.playSeconds : 1.1f;
        // 蓄力上挑的示范本身要播「按住 1.2s + 上划 1.1s」，播放时长必须跟它对齐，
        // 否则会在示范没播完时就重开一轮，看起来像教程被“多调了一次”
        if (step.kind == GestureHintKind.ChargedSwipe) playSeconds = Mathf.Max(playSeconds, 2.5f);
        while (_beatActive)
        {
            if (_stageState != null && !_stageState.IsStageInProgress)
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

    private void BeginBeat(TutorialGestureTarget target, AttackType? teachAttack, bool holdEnemies = true)
    {
        _beatActive = true;
        _beatTarget = target;
        _currentTeachAttack = teachAttack;
        _beatCompleted = false;
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
        gestureHint?.Hide();
        // 做完立刻收掉台词：玩家要能马上看清自己打出了什么
        var view = ResolveDialogueView();
        if (view != null && view.IsShowing) view.Skip();
        // 这一拍真的教完了 → 该招式转为"已教"（之后不再锁）
        if (_beatCompleted && _currentTeachAttack.HasValue)
        {
            _taughtAttacks.Add(_currentTeachAttack.Value);
            ApplyAttackGate(null);
        }
        _beatCompleted = false;
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
            case GestureHintKind.Hold: gestureHint.PlayHold(from); break;
            case GestureHintKind.Swipe: gestureHint.PlaySwipe(from, to); break;
            case GestureHintKind.ChargedSwipe: gestureHint.PlayChargedSwipe(from, to); break;
            case GestureHintKind.Parry: gestureHint.PlayParry(from, to); break;
            case GestureHintKind.ParryHold: gestureHint.PlayParryHold(from, to); break;
            case GestureHintKind.DefendButton: gestureHint.PlayPressHoldLoop(from); break;
        }
    }
}
