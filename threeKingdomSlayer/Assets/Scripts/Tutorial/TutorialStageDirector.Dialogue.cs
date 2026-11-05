using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 台词 —— 教学关导演的**台词**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
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
        var view = ResolveDialogueView();
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

    private void OnTutorialAttackExecuted(AttackType attackType, int column)
    {
        if (!_beatActive) return;
        // 防御拍的成功判据是「进入防御姿态」，不是出招 → 出招不收拍
        if (!_beatEndsOnAttack) return;
        if (attackType != ExpectedAttackFor(_beatTarget)) return;

        // 时停招架拍：必须**真的格到了**才算这一拍完成；格不到就继续等玩家再试（教学不设失败）
        if (_requireParryBlocked && attackType == AttackType.Parry)
        {
            var atk = AttackSystem.Instance;
            bool blocked = atk != null && atk.LastParryBlocked;
            if (!blocked)
            {
                Debug.LogWarning("[Tutorial] 这一下没格到 → 不算，继续等玩家再划");
                // 空放招架的 0.9s 后摇在冻结中不走（deltaTime=0）→ 清掉，否则玩家再也划不出来
                atk?.ClearActionLock();
                return;
            }
            // 招架不打伤害 → 它的「成功」就是格挡本身，不走命中判定
            RegisterBeatHit(attackType, column);
            return;
        }

        // 连招蓄力拍的阶段 1：**第一记点出来就换闸门**（ComboHold）→ 玩家点第二下会被拦掉。
        // 不换的话玩家/机器人会连点成 Jab1→Jab2→Jab3，而「指向突刺」的连段边只挂在 **Jab1** 上
        // → 连点三下之后就再也没有出路了（2026-11-04 实测：机器人在这一拍里连点三次，直接把连招打没了）。
        if (_comboChargePhase == 1)
        {
            var inputMgr = InputManager.Instance;
            if (inputMgr != null) inputMgr.tutorialGestureTarget = TutorialGestureTarget.ComboHold;
        }

        // 「攻击成功」= 招式打出去**并且打中敌人**（2026-11-04 用户定）。
        // 这里只记「刚打出了这一招」，**真正的命中**由敌人受击事件确认 → **空挥不收拍**。
        _beatAttackFired = true;
        _beatLastAttackType = attackType;
        _beatLastAttackColumn = column;
    }

    /// <summary>敌人受击 → 确认「攻击成功」（这一拍刚打出的招式真的打中了）。</summary>
    private void OnBeatEnemyDamaged(Enemy enemy)
    {
        if (!_beatActive || !_beatEndsOnAttack) return;
        // 「这一下是不是这一拍教的招式」：**用招式状态机的当前节点**判，
        // 不能只靠 <c>OnAttackExecuted</c> 事件 ——
        // **连段/缓冲触发的执行不会派发那个事件**（2026-11-04 实测：「指向突刺」明明打出来了，
        // 这一拍却收不了 → 「教学→天赋→招式教学」整条链路卡死）。
        if (!IsBeatAttackExecutingNow()) return;

        // 连招蓄力拍：**两段命中分开算** ——
        //   阶段 1 的第一记戳只推进阶段（不算收拍）；阶段 2 的派生招式才收拍。
        if (_comboChargePhase == 1) { _comboChargePhase = 2; return; }
        if (_comboChargePhase == 2) { _comboChargePhase = 3; }

        RegisterBeatHit(_beatLastAttackType, _beatLastAttackColumn);
    }

    /// <summary>
    /// 此刻状态机里正在执行的招式，是不是这一拍要教的那一招。
    /// （状态机在下一个输入到来之前会一直停在当前节点 → 受击那一刻看到的就是刚打出去的那一招 ✓）
    /// </summary>
    private bool IsBeatAttackExecutingNow()
    {
        var machine = _moveMachine != null ? _moveMachine : UnityEngine.Object.FindObjectOfType<PlayerMoveStateMachine>();
        if (machine == null) return false;
        var move = machine.CurrentMove;
        if (move == null) return false;
        return move.attackType == ExpectedAttackFor(_beatTarget);
    }

    /// <summary>记一次「攻击成功」（命中），并按 requiredHits 判定这一拍是否完成。</summary>
    private void RegisterBeatHit(AttackType attackType, int column)
    {
        // TapCombo：要打满 requiredHits 次才算完成（连点三下 → 三连 stab）
        _beatHitCount++;
        if (_beatHitCount < _beatRequiredHits)
        {
            DebugLog.Info($"[Tutorial] 手势拍进度：{_beatHitCount}/{_beatRequiredHits}（{attackType}）");
            return;
        }

        DebugLog.Info($"[Tutorial] 手势拍完成：{attackType} col={column}（第 {_beatHitCount} 次）");
        _beatCompleted = true;
        // 收尾台词统一由 EndBeat 补播（每一拍都能播 → 不靠出招收拍的拍也有台词）
        EndBeat();
    }

    private void OnStageVictory()
    {
        if (_stageState == null || tutorialRouteConfig == null) return;
        var runtime = RouteFlowHub.Current;
        if (runtime == null || runtime.RouteConfig != tutorialRouteConfig) return;

        var data = SaveManager.Load();
        if (TutorialSaveSection.IsCompleted(data)) return;
        TutorialSaveSection.MarkCompleted(data);
        SaveManager.Save(data);
        _tutorialCompletedCache = true;   // 缓存刷新：之后防御键在正式关一直开放
        DebugLog.Info("[Tutorial] 教学关已完成并记档");
    }

    // ── 对话视图缓存 ✅ ──
    // 原来同一帧可能反复 Find 同一个视图 ✅（7 处 ✅）→ 缓存一次 ✅ ✅；
    // 视图被销毁时 Unity 的「假 null」✅ 会让它自动重新解析 ✅ ✅（不缓存已销毁对象 ✅ ✅）。
    private BattleDialogueView _dialogueView;

    private BattleDialogueView ResolveDialogueView()
    {
        if (_dialogueView == null) _dialogueView = BattleDialogueView.Instance;
        return _dialogueView;
    }
}
