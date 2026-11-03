using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 击杀奖励管理器
/// 监听 PlayerState.OnKillCountChanged，从当前 StageConfig 读取里程碑配置并发放奖励
/// </summary>
public class KillRewardManager : MonoBehaviour
{
    public static KillRewardManager Instance { get; private set; }
    /// <summary>
    /// 关卡战斗状态（只读）：取当前关卡配置的击杀里程碑。
    /// 历史上直接摸 StageController 门面，现只依赖角色接口（2026-10-20 批次 1）。
    /// </summary>
    private IStageBattleState _stageBattleState;

    private IStageBattleState StageBattleState => _stageBattleState ??= StageRoles.BattleState;
    /// <summary>已领取的里程碑阈值集合</summary>
    private HashSet<int> _earnedThresholds = new HashSet<int>();

    /// <summary>里程碑达成事件：(阈值, 奖励列表)</summary>
    public event System.Action<int, List<KillRewardEntry>> OnMilestoneReached;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged += OnKillCountChanged;
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnKillCountChanged -= OnKillCountChanged;
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 重置已领取记录（新关卡开始时调用）
    /// </summary>
    public void ResetRewards()
    {
        _earnedThresholds.Clear();
    }

    /// <summary>
    /// 当前关卡的击杀里程碑（**实时读**）。
    ///
    /// ⚠️ 为什么不能开局缓存：<c>ResetRewards()</c> 在关卡开始时调，
    /// 那一刻 <c>StageConfig</c> 可能还没挂上 → 缓存会一直是 null → **里程碑永远不发**
    /// （2026-11-03 D1 实测发现：正式关配了 killMilestones 也不结算）。
    /// </summary>
    public List<KillMilestoneEntry> CurrentMilestones
    {
        get
        {
            var sc = StageBattleState;
            return (sc != null && sc.StageConfig != null) ? sc.StageConfig.killMilestones : null;
        }
    }

    /// <summary>获取已领取的里程碑阈值集合（供暂停面板显示）</summary>
    public HashSet<int> GetEarnedThresholds() => _earnedThresholds;

    private void OnKillCountChanged(int killCount)
    {
        var milestones = CurrentMilestones;
        if (milestones == null || milestones.Count == 0) return;

        foreach (var entry in milestones)
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
        if (entry == null || entry.rewards == null) return;
        // 发放口径统一到 KillRewardUtility（与路线节点奖励共用一份，2026-11-03 C-2）。
        KillRewardUtility.Apply(entry.rewards);
    }
}
