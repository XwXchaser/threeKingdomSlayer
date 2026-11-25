using System.Collections.Generic;
using System.Text;

/// <summary>
/// **奖励抽取的配置报告**（2026-11-25）。
///
/// 口径（用户 2026-11-25 拍定）：
///   · 同次去重导致某个原本有效的档位 / 品质抽空 = **配置错误** ⇒ 整组奖励作废，不发放任何一张；
///   · 一个候选都没有（配置缺货 / 去重后全空）= **报错跳过本次**（两类奖励统一，不再发铜钱补偿）；
///   · 候选 1~2 张仍按真实数量显示，不补卡、不伪造；
///   · **禁止静默改概率 / 加保底 / 发补偿 / 造装备**。
/// </summary>
public sealed class RewardDrawReport
{
    /// <summary>是否出现配置错误（出现 ⇒ 本次奖励整组作废）。</summary>
    public bool HasError => errors.Count > 0;

    /// <summary>配置错误明细（每条都带具体对象 / 条目）。</summary>
    public readonly List<string> errors = new List<string>();

    /// <summary>不致命但需要让配置者看到的提醒（例如候选种类少于请求张数）。</summary>
    public readonly List<string> warnings = new List<string>();

    /// <summary>本次请求的候选张数。</summary>
    public int requested;

    /// <summary>实际产出的候选张数。</summary>
    public int produced;

    /// <summary>本次锁定的内部圈数（仅装备池有意义；属性池恒为 0）。</summary>
    public int circle;

    /// <summary>命中的装备阶段备注（属性池为空）。</summary>
    public string stageLabel = "";

    public void Error(string message)
    {
        if (!string.IsNullOrEmpty(message)) errors.Add(message);
    }

    public void Warn(string message)
    {
        if (!string.IsNullOrEmpty(message)) warnings.Add(message);
    }

    /// <summary>拼成一条可直接进 Console 的说明（不省略关键对象）。</summary>
    public string Describe(string title)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(title).Append("] 配置问题：请求 ").Append(requested)
          .Append(" 张，实际 ").Append(produced).Append(" 张");
        if (circle > 0) sb.Append("，内部圈数 ").Append(circle);
        if (!string.IsNullOrEmpty(stageLabel)) sb.Append('（').Append(stageLabel).Append('）');
        sb.Append('。');
        for (int i = 0; i < errors.Count; i++)
            sb.Append('\n').Append("  · ").Append(errors[i]);
        for (int i = 0; i < warnings.Count; i++)
            sb.Append('\n').Append("  ⚠ ").Append(warnings[i]);
        return sb.ToString();
    }
}
