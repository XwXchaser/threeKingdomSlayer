using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 击杀奖励类型
/// </summary>
public enum KillRewardType
{
    [Tooltip("铜钱")]
    Coin,
    [Tooltip("回血")]
    Heal,
    [Tooltip("随机升级（第三期实现）")]
    RandomUpgrade
}

/// <summary>
/// 单条奖励条目（类型 + 数值）
/// </summary>
[Serializable]
public class KillRewardEntry
{
    [Tooltip("奖励类型")]
    public KillRewardType rewardType;
    [Tooltip("奖励数值（铜钱数或回血量）")]
    public int rewardAmount;
}

/// <summary>
/// 单个击杀里程碑条目（1个阈值 → N个奖励）
/// </summary>
[Serializable]
public class KillMilestoneEntry
{
    [Tooltip("累计击杀数阈值")]
    public int killThreshold;
    [Tooltip("奖励列表（可配置多条，如同时奖励铜钱+回血）")]
    public List<KillRewardEntry> rewards = new List<KillRewardEntry>();
}

/// <summary>
/// 击杀里程碑工具方法
/// </summary>
public static class KillMilestoneUtils
{
    /// <summary>
    /// 获取大于当前击杀数的最小阈值，无则返回 -1
    /// </summary>
    public static int GetNextThreshold(this List<KillMilestoneEntry> milestones, int currentKills)
    {
        int best = -1;
        foreach (var m in milestones)
        {
            if (m.killThreshold > currentKills && (best == -1 || m.killThreshold < best))
                best = m.killThreshold;
        }
        return best;
    }
}
