using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **招式解锁引导**（2026-11-04）—— 教学关导演的一部分。
///
/// 口径：**解锁后第一次进关**时，把「已解锁但还没教过」的招式当成**一拍拍出来**，
/// 走的**就是教学拍那条机制**：示范循环 → 玩家真的把招式打出去 → 打出去才算教完 → 收拍后补一行台词。
///
/// 为什么不再用另一套表现（2026-11-04 的教训）：
///   之前把引导做成了「独立的手势门槛」（<c>TutorialGestureHint.Gate</c>：暂停整个游戏 + 拦输入 + 原始输入自判），
///   结果和教学拍**不一致**：
///     • 引导跑在战斗开始之前 → 玩家划完那一下**打不出去** → 学不到「这一划会横扫一排」；
///     • 它和教学拍抢同一个提示层 → 教学拍的手势收不掉（手反复冒出来）。
///   而教学拍本身**根本不用 Gate**（它就是「示范循环 + 招式真的打出去」）→ 交接 §3.2 的描述是错的。
/// → 统一走教学拍：同一套视觉、同一套判定、同一套收尾。
/// </summary>
public partial class TutorialStageDirector
{
    /// <summary>已经播过的解锁引导节点（避免同一节点反复排队）。</summary>
    private readonly HashSet<string> _playedGuideNodeIds = new HashSet<string>();

    /// <summary>
    /// 收集「已解锁但还没教过」的招式 → 变成教学拍（排在教学关自己的拍**前面**）。
    /// 与关卡节点无关：正式关也会播（数据驱动 → 只看天赋节点配没配手势）。
    /// </summary>
    private List<TutorialGestureStep> BuildMoveGuideSteps(StageConfig config, int waveIndex)
    {
        var result = new List<TutorialGestureStep>();
        var ps = PlayerState.Instance;
        var hero = ps != null ? ps.heroConfig : null;
        if (hero == null) return result;

        var pending = MoveGuideService.CollectPending(hero.talentTree, hero.talentCatalog);
        for (int i = 0; i < pending.Count; i++)
        {
            var req = pending[i];
            if (req == null || string.IsNullOrEmpty(req.nodeId)) continue;
            if (_playedGuideNodeIds.Contains(req.nodeId)) continue;
            var kind = KindForMoveGuide(req.gesture);
            if (kind == GestureHintKind.None) continue;

            // 示范轨迹：**必须和这一拍要求的手势方向一致**。
            // 2026-11-04 用户实测踩过：引导拍没设 from01/to01 → 用了类默认值
            // (0.5,0.5)→(0.5,0.72) = **向上挑**，而闸门要求的是**横划** →
            // 示范教上挑、判定要横划，玩家照着做根本过不了。
            Vector2 from01, to01;
            GuideDemoPath(req.gesture, out from01, out to01);

            result.Add(new TutorialGestureStep
            {
                battle = config,
                waveIndex = waveIndex,
                kind = kind,
                delay = 0.2f,
                playSeconds = 1.2f,
                pauseUntilPerformed = false,   // 教学拍不是门槛：靠「招式真的打出去」收拍
                from01 = from01,
                to01 = to01,
                beforeDialogue = req.dialogue, // **和手势示范同时出现**（不是收拍后补播）
                comboPhase2Dialogue = req.phase2Dialogue, // 连招蓄力拍：阶段 2 的台词
                comboPhase3Dialogue = req.phase3Dialogue, // 连招蓄力拍：阶段 3（瞄准）的台词
                guideNodeId = req.nodeId,
                guideAttackType = req.move != null ? (int)req.move.attackType : -1,
            });
        }
        return result;
    }

    /// <summary>
    /// 引导拍的示范轨迹（屏幕归一化 0..1，左下角为原点）。
    /// **横划 = 同一 y、x 从左到右**；竖划 = 同一 x、y 从下往上。
    /// </summary>
    private static void GuideDemoPath(MoveGuideGesture gesture, out Vector2 from01, out Vector2 to01)
    {
        if (gesture == MoveGuideGesture.SwipeVertical)
        {
            from01 = new Vector2(0.5f, 0.25f);
            to01 = new Vector2(0.5f, 0.75f);
            return;
        }
        if (gesture == MoveGuideGesture.Tap || gesture == MoveGuideGesture.Hold || gesture == MoveGuideGesture.ComboCharge)
        {
            // 连招蓄力拍（ComboCharge）也走这一条：它是「点一下 + 按住」，
            // 不能拿横划的轨迹当示范（2026-11-08：那一拍本来教的就是点/蓄，不是划）。
            from01 = new Vector2(0.5f, 0.5f);
            to01 = new Vector2(0.5f, 0.72f);
            return;
        }
        // 横划（默认）：同一条水平线上从左到右
        from01 = new Vector2(0.2f, 0.5f);
        to01 = new Vector2(0.8f, 0.5f);
    }

    /// <summary>天赋节点配的手势 → 教学拍的拍型。</summary>
    private static GestureHintKind KindForMoveGuide(MoveGuideGesture gesture)
    {
        switch (gesture)
        {
            case MoveGuideGesture.Tap: return GestureHintKind.Tap;
            case MoveGuideGesture.Hold: return GestureHintKind.Hold;
            case MoveGuideGesture.ComboCharge: return GestureHintKind.ComboCharge;
            case MoveGuideGesture.SwipeHorizontal:
            case MoveGuideGesture.SwipeVertical: return GestureHintKind.Swipe;
            default: return GestureHintKind.None;
        }
    }

    /// <summary>
    /// 这一拍完成后 → 标记「已教过」（**只有招式真的打出去才算**，没打出去下次还会教）。
    /// </summary>
    private void MarkGuideTaughtIfNeeded()
    {
        var step = _currentStep;
        if (step == null || string.IsNullOrEmpty(step.guideNodeId)) return;
        if (!_playedGuideNodeIds.Add(step.guideNodeId)) return;
        MoveGuideService.MarkTaught(step.guideNodeId);
        Debug.Log($"[Tutorial] 招式解锁引导完成：{step.guideNodeId}");
    }
}
