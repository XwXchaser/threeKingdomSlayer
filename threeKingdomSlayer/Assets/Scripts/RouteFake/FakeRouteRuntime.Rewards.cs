using System.Collections.Generic;
using UnityEngine;

/// <summary>
///    FakeRouteRuntime 的**节点奖励**：发放 <c>FakeRouteNodeConfig.rewards</c>。
///
///    按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-11-03 C-2，partial 分文件）。
///    partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
///
///    为什么现在才发：这个字段从加进来就没人消费过（配了不发）——
///    现在节点内容跑完就发，口径与击杀里程碑共用 <see cref="KillRewardUtility"/>。
///
///    幂等性：检查点快照会连**铜钱**一起回滚（<c>RestoreCheckpoint</c> 写回 coinCount），
///    所以「死亡后重打这个节点」不会双发铜钱；秘籍本身按存档判定，重复发放无副作用。
/// </summary>
public sealed partial class FakeRouteRuntime : MonoBehaviour
{
    /// <summary>节点内容跑完后发放节点奖励（有配置的节点才会进来）。</summary>
    private void GrantNodeRewards(FakeRouteNodeConfig node)
    {
        if (node == null || node.rewards == null || node.rewards.Count == 0) return;

        List<string> lines = KillRewardUtility.Apply(node.rewards);
        if (lines.Count == 0) return;

        string detail = string.Join("  ", lines.ToArray());
        Debug.Log($"[FakeRoute] 节点奖励 node={node.nodeId}: {detail}");

        // 提示复用局内奖励弹窗端口（没挂 UI 就只有日志，不阻塞流程）。
        CombatPresenters.InRunReward?.ShowReward($"节点奖励 · {node.displayName}", detail);
    }

    /// <summary>
    /// 战斗节点通关奖：付该场战斗自己的 <c>StageConfig.clearCoinReward</c>。
    ///
    /// 为什么在这里付而不在 <c>StageRunFlow.OnAllWavesCleared</c>：
    ///   <c>WaveSpawner</c> 在一场战斗结束时会把 <c>OnAllWavesCompleted</c> **触发两次**
    ///   （NotifyWaveCleared 一次 + SpawnNextWave 溢出分支一次）→ 那里付会**付两次**（实测 N1 +60 付了两遍）。
    ///   非路线分支靠「状态已切 Victory → 第二次被状态检查拦下」侥幸躲过，路线分支没有这道闸。
    ///   这里每个 BattleEntry 只走一次 → 只付一次。
    ///
    /// 聚财（天赋）：铜钱获取 +30%。
    /// </summary>
    private static void PayBattleEntryClearReward(StageConfig battleConfig)
    {
        if (battleConfig == null || battleConfig.clearCoinReward <= 0) return;
        var ps = PlayerState.Instance;
        if (ps == null) return;

        int amount = Mathf.RoundToInt(battleConfig.clearCoinReward * TalentCapabilities.CoinGainMultiplier);
        if (amount <= 0) return;
        ps.AddCoins(amount);
        Debug.Log($"[FakeRoute] 战斗节点通关奖：{battleConfig.stageName} +{amount} 铜钱");
    }
}
