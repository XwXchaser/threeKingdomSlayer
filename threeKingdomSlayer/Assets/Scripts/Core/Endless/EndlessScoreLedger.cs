using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **无限模式分数账本**（2026-11-16 新增）。
///
/// 现状：全项目 grep `score` **零命中** —— 分数系统是从零设计的。
///
/// ## 分数来源（都可从现有系统取数，不需要新埋点太深）
/// | 来源 | 取数 |
/// |---|---|
/// | 击杀分 | 敌种基础分（`EndlessEnemyThreat`）× 圈数难度系数 |
/// | 波次通过分 | 剩余时间 + 剩余血量 |
/// | 连击分 | `ComboManager` 已有计数 |
/// | 无伤 / 无破防加成 | 战斗状态 |
/// | 圈里程碑 | 无限模式运行时 |
///
/// ## 三层结算（与 `design/endless-mode-design.md` §4.2 对应）
/// 波结算（轻，不弹窗） → 节点结算（中） → 圈结算（重，玩家决策点）。
///
/// ⚠ 全部系数是**占位值**，等实机标定（数值工作慢慢打磨）。
/// </summary>
public sealed class EndlessScoreLedger
{
    /// <summary>波结算输入。</summary>
    public struct WaveSettlementInput
    {
        /// <summary>本波用掉的时间（秒）。</summary>
        public float elapsedSeconds;
        /// <summary>本波的目标时长（秒），用于算"快枪"奖励。</summary>
        public float targetSeconds;
        /// <summary>本波结束时剩余血量比例（0~1）。</summary>
        public float healthRatio;
        /// <summary>本波达到的最高连击。</summary>
        public int maxCombo;
        /// <summary>本波是否无伤。</summary>
        public bool noHitTaken;
        /// <summary>本波是否无人死亡（未被破防）。</summary>
        public bool noBreakTaken;
    }

    // ── 分项累计 ──
    public int KillScore { get; private set; }
    public int WaveScore { get; private set; }
    public int CircleScore { get; private set; }
    public int ComboPeak { get; private set; }

    /// <summary>总分（= 击杀 + 波次 + 圈 + 连击峰值奖励）。</summary>
    public int TotalScore { get; private set; }

    /// <summary>当前圈数（结算时用于难度系数）。</summary>
    public int Circle { get; private set; } = 1;

    // ── 系数（占位值，等标定）──
    /// <summary>每点连击的分数。</summary>
    public int scorePerCombo = 2;
    /// <summary>波次基础通过分。</summary>
    public int waveBaseScore = 100;
    /// <summary>剩余血量比例的加成系数（满血 = ×(1 + ratio × k)）。</summary>
    public float healthBonusCoefficient = 0.5f;
    /// <summary>快枪加成系数（提前完成比例 → 百分比加成）。</summary>
    public float speedBonusCoefficient = 0.3f;
    /// <summary>无伤加成（固定分）。</summary>
    public int noHitBonus = 150;
    /// <summary>无破防加成（固定分）。</summary>
    public int noBreakBonus = 80;
    /// <summary>圈里程碑基础分（每圈递增）。</summary>
    public int circleBaseBonus = 500;
    /// <summary>圈里程碑每圈增量。</summary>
    public int circleBonusStep = 250;

    /// <summary>进入新的一圈（重置本圈统计，总分保留）。</summary>
    public void BeginCircle(int circle)
    {
        Circle = Mathf.Max(1, circle);
    }

    /// <summary>击杀得分。</summary>
    public void AddKill(int enemyId)
    {
        var entry = EndlessEnemyThreat.Get(enemyId);
        int gained = entry.score * CircleDifficultyMultiplier();
        KillScore += gained;
        TotalScore += gained;
    }

    /// <summary>记录连击峰值（波结算时结算一次）。</summary>
    public void AddComboPeak(int combo)
    {
        if (combo > ComboPeak) ComboPeak = combo;
    }

    /// <summary>
    /// 波次结算：返回本次获得的波次分。
    /// 组成 = 基础通过分 + 快枪加成 + 残血加成 + 连击分 + 无伤 + 无破防。
    /// </summary>
    public int SettleWave(WaveSettlementInput input)
    {
        int gained = waveBaseScore;

        // 快枪：提前完成目标时长 → 按比例加成
        if (input.targetSeconds > 0f && input.elapsedSeconds > 0f)
        {
            float ratio = 1f - Mathf.Clamp01(input.elapsedSeconds / input.targetSeconds);
            if (ratio > 0f) gained += Mathf.RoundToInt(waveBaseScore * speedBonusCoefficient * ratio);
        }

        // 残血奖励：越残血分越高（鼓励险胜，但别让它压过击杀分）
        float health = Mathf.Clamp01(input.healthRatio);
        gained += Mathf.RoundToInt(waveBaseScore * healthBonusCoefficient * health);

        // 连击分
        if (input.maxCombo > 0) gained += input.maxCombo * scorePerCombo;

        // 无伤 / 无破防
        if (input.noHitTaken) gained += noHitBonus;
        if (input.noBreakTaken) gained += noBreakBonus;

        WaveScore += gained;
        TotalScore += gained;
        return gained;
    }

    /// <summary>圈结算：返回本次获得的圈里程碑分。</summary>
    public int SettleCircle()
    {
        int gained = circleBaseBonus + circleBonusStep * Mathf.Max(0, Circle - 1);
        CircleScore += gained;
        TotalScore += gained;
        return gained;
    }

    /// <summary>圈数难度系数（击杀分随圈数放大）。</summary>
    public int CircleDifficultyMultiplier()
    {
        return 1 + Mathf.Max(0, Circle - 1) / 2;
    }

    /// <summary>重开一局（总分清零）。</summary>
    public void Reset()
    {
        KillScore = 0;
        WaveScore = 0;
        CircleScore = 0;
        ComboPeak = 0;
        TotalScore = 0;
        Circle = 1;
    }

    /// <summary>结算明细（给结算面板用）。</summary>
    public List<(string label, int value)> Breakdown()
    {
        return new List<(string, int)>
        {
            ("击杀分", KillScore),
            ("波次分", WaveScore),
            ("圈里程碑", CircleScore),
            ("总分", TotalScore),
        };
    }

    public override string ToString()
    {
        return $"[EndlessScore] 总分 {TotalScore}（击杀 {KillScore} / 波次 {WaveScore} / 圈 {CircleScore}）圈 {Circle}";
    }
}
