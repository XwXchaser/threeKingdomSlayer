using System.Collections;
using UnityEngine;

/// <summary>
/// 教学拍的**开场两段字幕**（2026-11-05 用户定 · 2026-11-06 按用户口述重做）。
///
/// 需求原文（2026-11-06 用户口述，逐字）：
/// **「就位，暂停，播放第一段文本同时启用按键(不等待时间)；
///   玩家点击按键；
///   出现第二段文本和对应手势，直到玩家做出对应手势前文本和教学各项内容都不会消失；
///   完成教学，退出 回到正常游戏；
///   不存在任何计时器直接跳过，从来没有这样的需求。」**
///
/// ```
/// [波次开始] → 敌人进场 → **就位**（敌人站定 = 原本那行台词出现的时刻）
///   ① 暂停：冻场 + 锁输入 + 挡住其它 UI（只有左上角暂停键可按）
///   ② 播【第一段文本】**同时**亮出「点击继续」按键（不等任何计时器，文本不会自己消失）
///   ③ 玩家点击按键 → 第一段收掉
///   ④ 出现【第二段文本】**与对应手势示范**（同时出现；两者都留到玩家做出手势）
///   ⑤ 做出手势 → 教学完成 → **退出暂停** → 回到正常游戏
/// ```
///
/// ⚠️ **没有任何计时器**：第一段不会自动消失、第二段不会按秒消失；唯一的推进力是玩家的动作。
/// ⚠️ **正式关不走这条**：`TutorialStageDirector` 是 Battle.scene 的常驻组件，
///   正式关同样会触发 `OnWaveStarted` → 这里用 `n1Battle / e0Battle` 判路线挡掉。
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    /// <summary>开场两段字幕是否正在进行（供自动化驱动 / 调试读取）。</summary>
    public bool IntroActive { get; private set; }

    /// <summary>
    /// 这一拍要不要走开场两段字幕。
    ///
    /// 只有**教学拍**走（招式解锁引导拍 `guideNodeId` 非空的不走）；
    /// 只有教学关的波次走；没有配第一段文本就不走。
    /// </summary>
    private bool TryGetIntroLine(TutorialGestureStep step, out DialogueEventData line)
    {
        line = null;
        if (step == null) return false;
        if (step.waveIndex < 0) return false;                                   // 路线出口不是波次
        if (!string.IsNullOrEmpty(step.guideNodeId)) return false;              // 招式解锁引导拍不走
        if (_stageState == null || _stageState.StageConfig == null) return false;

        var config = _stageState.StageConfig;
        if (config != n1Battle && config != e0Battle) return false;             // 正式关不走

        var arr = config == e0Battle ? e0IntroDialogues : n1IntroDialogues;
        if (arr == null || step.waveIndex >= arr.Length) return false;
        line = arr[step.waveIndex];
        return line != null;
    }

    /// <summary>
    /// 开场两段字幕：第一段（+按键）→ 玩家点击 → 第二段。
    ///
    /// ⚠️ **暂停的范围 = 第一段 + 等点击**：
    ///   玩家点完按钮就**恢复可操作**（冻场会让招式状态机的时钟不走 → 手势打不出来），
    ///   然后第二段文本 + 手势示范一起出现，两者留到玩家做出手势。
    /// </summary>
    private IEnumerator PlayTutorialIntro(DialogueEventData firstLine, DialogueEventData secondLine)
    {
        if (firstLine == null && secondLine == null) yield break;

        IntroActive = true;
        // ① 冻结 + 锁输入 + 挡住别的 UI（只有暂停键和「点击继续」能点）
        SetEnemiesHold(true);
        _introPaused = true;
        Time.timeScale = 0f;
        GameplayInputGate.RequestBlock(this, InputBlockReason.TutorialIntro);
        DialogueManager.AllowDialogueWhilePaused = true;   // 冻结中台词队列要能播
        SetIntroClickBlock(true);
        DisableHudInteraction();
        // 文本不会自己消失：第一段等玩家点按钮、第二段等玩家做出手势
        SetDialogueAutoAdvance(false);
        // 开场期间**点气泡不推进台词**（唯一推进力是「点击继续」→ 否则玩家一点就把第一段点没了）
        var introView = ResolveDialogueView();
        if (introView != null) introView.PointerAdvanceEnabled = false;

        // ② 第一段文本 **同时** 亮出「点击继续」按键（不等任何计时器）
        //    教学文本出现 → 压暗画面（黑幕）；压到手势做完（EndBeat）才撤
        SetTeachingDim(true);
        if (firstLine != null)
        {
            var view = ResolveDialogueView();
            // ⚠️ 先把**上一条台词**收掉（上一拍/引导拍的尾巴可能还留在屏上）
            //   → 否则「等文本上屏」会误判成“已经显示了”（实测：按钮先亮、文本后到）。
            if (view != null && view.IsShowing) view.Skip();
            DialogueManager.Instance?.QueueEventAndNotify(firstLine, forceReplay: true);
            float shown = 0f;
            while (view != null && !view.IsShowing && shown < 2f)
            {
                shown += Time.unscaledDeltaTime;
                yield return null;
            }

            // 等玩家点击（**没有超时**：不存在任何计时器直接跳过）
            bool clicked = false;
            if (continueButton != null)
            {
                continueButton.Show(() => { clicked = true; });
                while (!clicked)
                {
                    KeepFrozen();
                    yield return null;
                }
                continueButton.Hide();
            }
            else
            {
                Debug.LogWarning("[Tutorial] 场景里没有「点击继续」按钮 → 跳过等待");
            }
            // 第一段收掉（第二段要接上）
            if (view != null && view.IsShowing) view.Skip();
        }

        // ③ 玩家点完了 → **恢复可操作**（冻场会让招式时钟不走 → 手势打不出来）
        EndIntro();

        // ④ 第二段文本上屏（**不自动消失**）→ 接下来的手势示范由 HintRoutine 接着开，
        //    两者一起留到玩家做出手势（EndBeat 会清掉文本）。
        if (secondLine != null)
        {
            DialogueManager.Instance?.QueueEventAndNotify(secondLine, forceReplay: true);
            var view = ResolveDialogueView();
            float shown = 0f;
            while (view != null && !view.IsShowing && shown < 2f)
            {
                shown += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    /// <summary>
    /// 开场收尾（玩家点完「点击继续」时调）：解冻、解锁、收遮罩、恢复 HUD。
    ///
    /// ⚠️ **顺序很重要**：先解冻，再收别的；且每一步都包 try/catch
    ///   —— 任何一步抛异常都不能把游戏留在 timeScale=0 的冻场里（那看起来就是「卡死」）。
    /// 幂等：被打断时也会调它。
    /// </summary>
    private void EndIntro()
    {
        if (!IntroActive) return;
        IntroActive = false;
        _introPaused = false;
        // 只解自己的冻（别的系统冻的时间我们不接管）
        if (Time.timeScale == 0f) Time.timeScale = 1f;

        SafeStep("收引导期点击遮罩", () => SetIntroClickBlock(false));
        SafeStep("收「点击继续」按钮", () => { if (continueButton != null) continueButton.Hide(); });
        SafeStep("恢复 HUD 交互", RestoreHudInteraction);
        SafeStep("解冻敌人", () => SetEnemiesHold(false));
        SafeStep("释放输入闸门", () => GameplayInputGate.Release(this));
        SafeStep("恢复对话许可", () => DialogueManager.AllowDialogueWhilePaused = false);
        Debug.Log("[Tutorial] 开场结束，恢复可操作");
    }

    /// <summary>
    /// 引导期把**战斗 HUD 那一块**失能（道具栏 / 技能槽 / 防御键 / 英雄头像……）。
    ///
    /// ⚠️ **失能范围只能是 HudCard**（战斗 HUD 本体），**不能是 BattleHUD 根 CanvasGroup**：
    ///   根 CanvasGroup 是「暂停键」和「点击继续」按钮的祖先 →
    ///   一旦把它置为 interactable=false，`Selectable.IsInteractable()` 会连带变 false
    ///   → **暂停键和「点击继续」都会点不动**（2026-11-06 实测踩到）。
    ///   HudCard 里的东西（防御键 / 道具栏 / 技能槽 / 头像）才是引导期要锁的。
    /// </summary>
    private void DisableHudInteraction()
    {
        if (hudCanvasGroup == null) return;
        _hudInteractableBackup = hudCanvasGroup.interactable;
        _hudBlocksRaycastsBackup = hudCanvasGroup.blocksRaycasts;
        hudCanvasGroup.interactable = false;
        hudCanvasGroup.blocksRaycasts = false;
    }

    private void RestoreHudInteraction()
    {
        if (hudCanvasGroup == null) return;
        hudCanvasGroup.interactable = _hudInteractableBackup;
        hudCanvasGroup.blocksRaycasts = _hudBlocksRaycastsBackup;
    }

    private bool _hudInteractableBackup = true;
    private bool _hudBlocksRaycastsBackup = true;

    /// <summary>台词是否自动推进（开场期间关掉 → 文本留在屏上等玩家动作）。</summary>
    private void SetDialogueAutoAdvance(bool enabled)
    {
        var view = ResolveDialogueView();
        if (view != null) view.autoAdvanceEnabled = enabled;
    }

    /// <summary>恢复台词自动推进（幂等）。</summary>
    private void RestoreDialogueAutoAdvance()
    {
        SetDialogueAutoAdvance(true);
        // 开场期间关掉的“点气泡推进”也在拍收尾时恢复
        var v = ResolveDialogueView();
        if (v != null) v.PointerAdvanceEnabled = true;
    }

    /// <summary>保持冻结（暂停菜单的「继续游戏」会把 timeScale 写回 1 → 每帧重新断言）。</summary>
    private static void KeepFrozen()
    {
        if (Time.timeScale != 0f) Time.timeScale = 0f;
    }
}
