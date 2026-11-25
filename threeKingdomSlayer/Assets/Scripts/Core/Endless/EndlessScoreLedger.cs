using System.Collections.Generic;

/// <summary>
/// **无限模式分数账本**（2026-11-16 新增，2026-11-24 简化为「只按击杀计分」）。
///
/// ## 规则（用户 2026-11-24 定）
/// · **一个敌人值多少分是固定的**：`EndlessEnemyThreat.score`（10/15/20/25，Boss 220–300）；
/// · **击杀当场加分**：`AddKill` 立刻累加总分并抛 `OnScoreChanged` ⇒ HUD 数字立刻跳；
/// · **没有波次奖励分、没有圈里程碑分**：
///   旧版那套「基础 100 + 快枪 + 残血 + 连击 + 无伤 + 无破防」是上一轮自行设计的占位规则，
///   用户明确表示不了解、也不需要 ⇒ 整体删除。以后要加新计分规则，**先写设计文档再实现**，
///   不要在波末偷偷结一笔账。
/// · **击杀分不随层数放大**：同一敌人永远同分（旧版 `1 + (层-1)/2` 已删）。
///
/// ## 结算
/// 只有一处：**局结束**（阵亡 / 提前退出共用），面板显示本局总分 + 历史最高分。
/// 中途没有结算画面，也不打断玩家。
/// </summary>
public sealed class EndlessScoreLedger
{
    /// <summary>累计击杀分（目前 = 总分，保留分开是为了明细展示）。</summary>
    public int KillScore { get; private set; }

    /// <summary>本局总分（当前只有击杀分）。</summary>
    public int TotalScore { get; private set; }

    /// <summary>总分变化通知（新总分）。击杀与重开都会发。</summary>
    public System.Action<int> OnScoreChanged;

    /// <summary>击杀得分：按敌种固定分，**当场**累加。</summary>
    public void AddKill(int enemyId)
    {
        int gained = EndlessEnemyThreat.Get(enemyId).score;
        if (gained <= 0) return;

        KillScore += gained;
        TotalScore += gained;
        OnScoreChanged?.Invoke(TotalScore);
    }

    /// <summary>重开一局（总分清零）。</summary>
    public void Reset()
    {
        KillScore = 0;
        TotalScore = 0;
        OnScoreChanged?.Invoke(0);
    }

    /// <summary>结算明细（给结算面板 / 调试用）。</summary>
    public List<(string label, int value)> Breakdown()
    {
        return new List<(string, int)>
        {
            ("击杀分", KillScore),
            ("总分", TotalScore),
        };
    }

    public override string ToString() => $"[EndlessScore] 总分 {TotalScore}（击杀 {KillScore}）";
}
