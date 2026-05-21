using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 击杀奖励管理器
/// 监听 PlayerState.OnKillCountChanged，从当前 StageConfig 读取里程碑配置并发放奖励
/// </summary>
public class KillRewardManager : MonoBehaviour
{
    /// <summary>已领取的里程碑阈值集合</summary>
    private HashSet<int> _earnedThresholds = new HashSet<int>();

    /// <summary>里程碑达成事件：(阈值, 奖励列表)</summary>
    public event System.Action<int, List<KillRewardEntry>> OnMilestoneReached;

    private List<KillMilestoneEntry> _milestones;

    private void Start()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged += OnKillCountChanged;
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged -= OnKillCountChanged;
    }

    /// <summary>
    /// 重置已领取记录并重新加载当前关卡配置（新关卡开始时调用）
    /// </summary>
    public void ResetRewards()
    {
        _earnedThresholds.Clear();
        var sc = StageController.Instance;
        _milestones = (sc != null && sc.stageConfig != null) ? sc.stageConfig.killMilestones : null;
    }

    /// <summary>获取当前关卡里程碑（供 UI 读取进度）</summary>
    public List<KillMilestoneEntry> CurrentMilestones => _milestones;

    /// <summary>获取已领取的里程碑阈值集合（供暂停面板显示）</summary>
    public HashSet<int> GetEarnedThresholds() => _earnedThresholds;

    private void OnKillCountChanged(int killCount)
    {
        if (_milestones == null || _milestones.Count == 0) return;

        foreach (var entry in _milestones)
        {
            if (killCount >= entry.killThreshold && _earnedThresholds.Add(entry.killThreshold))
            {
                GrantReward(entry);
                OnMilestoneReached?.Invoke(entry.killThreshold, entry.rewards);
            }
        }
    }

    private void GrantReward(KillMilestoneEntry entry)
    {
        var ps = PlayerState.Instance;
        if (ps == null || entry.rewards == null) return;

        foreach (var r in entry.rewards)
        {
            switch (r.rewardType)
            {
                case KillRewardType.Coin:
                    ps.AddCoins(r.rewardAmount);
                    break;

                case KillRewardType.Heal:
                    float maxHp = ps.heroConfig != null ? ps.heroConfig.maxHealth : 100f;
                    ps.currentHealth = Mathf.Min(ps.currentHealth + r.rewardAmount, maxHp);
                    ps.OnHealthChanged?.Invoke(ps.currentHealth, maxHp);
                    break;

                case KillRewardType.RandomUpgrade:
                    Debug.Log($"[KillRewardManager] RandomUpgrade 奖励（第三期待实现）：x{r.rewardAmount}");
                    break;
            }
        }
    }
}
