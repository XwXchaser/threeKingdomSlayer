using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **奖励发放的唯一实现**（2026-11-03 C-2 / C-5）。
///
/// 两处消费方共用这一份，避免口径漂移：
///   • 击杀里程碑 → <see cref="KillRewardManager"/>；
///   • 路线节点奖励 → <c>FakeRouteRuntime</c>（<c>FakeRouteNodeConfig.rewards</c>，这个字段以前没人消费）。
///
/// 口径：
///   • 铜钱一律乘「聚财」天赋倍率（<see cref="TalentCapabilities.CoinGainMultiplier"/>）；
///   • 回血按当前血量上限截断；
///   • 秘籍走存档判定，**已拥有就跳过**（幂等 ✅，重复发放不会给第二份）。
///
/// 不做提示：提示由调用方决定（节点奖励弹窗 / 里程碑弹窗各走各的端口）。
/// </summary>
public static class KillRewardUtility
{
    /// <summary>
    /// 发放一组奖励条目。
    /// </summary>
    /// <param name="rewards">奖励条目（可为 null / 空）。</param>
    /// <returns>可展示的文案行（每条一行）；没有实际发放时返回空列表。</returns>
    public static List<string> Apply(List<KillRewardEntry> rewards)
    {
        var lines = new List<string>();
        if (rewards == null || rewards.Count == 0) return lines;

        var ps = PlayerState.Instance;
        for (int i = 0; i < rewards.Count; i++)
        {
            var r = rewards[i];
            if (r == null) continue;

            switch (r.rewardType)
            {
                case KillRewardType.Coin:
                {
                    if (ps == null) break;
                    int amount = Mathf.RoundToInt(r.rewardAmount * TalentCapabilities.CoinGainMultiplier);
                    if (amount == 0) break;
                    ps.AddCoins(amount);
                    lines.Add($"+{amount} 铜钱");
                    break;
                }

                case KillRewardType.Heal:
                {
                    if (ps == null) break;
                    float maxHp = ps.heroConfig != null ? ps.heroConfig.maxHealth : 100f;
                    float before = ps.currentHealth;
                    ps.currentHealth = Mathf.Min(ps.currentHealth + r.rewardAmount, maxHp);
                    int healed = Mathf.RoundToInt(ps.currentHealth - before);
                    ps.OnHealthChanged?.Invoke(ps.currentHealth, maxHp);
                    if (healed > 0) lines.Add($"+{healed} 生命");
                    break;
                }

                case KillRewardType.Manual:
                {
                    if (r.manual == null)
                    {
                        Debug.LogError("[KillRewardUtility] Manual 奖励没有填秘籍资产，跳过");
                        break;
                    }
                    bool granted = ManualDropSystem.Grant(r.manual);
                    lines.Add(granted
                        ? $"获得秘籍「{r.manual.displayName}」"
                        : $"已拥有秘籍「{r.manual.displayName}」");
                    break;
                }

                case KillRewardType.RandomUpgrade:
                    Debug.Log($"[KillRewardUtility] RandomUpgrade 奖励（第三期待实现）：x{r.rewardAmount}");
                    break;
            }
        }

        return lines;
    }
}
