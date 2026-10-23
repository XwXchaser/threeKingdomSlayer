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
        if (attackType != ExpectedAttackFor(_beatTarget)) return;
        DebugLog.Info($"[Tutorial] 手势拍完成：{attackType} col={column}");
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
        DebugLog.Info("[Tutorial] 教学关已完成并记档");
    }

    // ── 对话视图缓存 ✅ ──
    // 原来同一帧可能反复 Find 同一个视图 ✅（7 处 ✅）→ 缓存一次 ✅ ✅；
    // 视图被销毁时 Unity 的「假 null」✅ 会让它自动重新解析 ✅ ✅（不缓存已销毁对象 ✅ ✅）。
    private BattleDialogueView _dialogueView;

    private BattleDialogueView ResolveDialogueView()
    {
        if (_dialogueView == null) _dialogueView = FindObjectOfType<BattleDialogueView>();
        return _dialogueView;
    }
}
