using System;
using UnityEngine;

/// <summary>
/// 补给物件发奖的**表现端口**：内核（Combat）只负责算清奖励，**什么时候真正入账**交给表现层决定。
///
/// 为什么要端口：内核层（Locus.Combat）不能反向引用 UI 层（Locus.UI）——
/// 而「金币飞过去再入账」这件事天然住在 UI 层。
/// 于是内核把「发奖回调」交给端口，UI 播完动画再调用它；UI 不在时端口立即回调（不吞奖励）。
/// </summary>
public struct SupplyRewardInfo
{
    /// <summary>物件的世界坐标（金币从这儿飞出去）。</summary>
    public Vector3 worldPosition;

    /// <summary>物件所在的层（决定奖励入账去处）。</summary>
    public SupplyLayer layer;

    /// <summary>奖励数量（金币）。</summary>
    public int coins;

    /// <summary>物件 id（调试 / 去重）。</summary>
    public string propId;

    /// <summary>真正发奖的回调（幂等：重复调用只会生效一次）。</summary>
    public Action grant;
}

/// <summary>补给发奖的表现端口（装配点：UI 层订阅）。</summary>
public static class SupplyRewardPort
{
    /// <summary>有订阅者时由订阅者决定何时发奖；没有订阅者时立即发奖。</summary>
    public static event Action<SupplyRewardInfo> RewardGranted;

    public static void Raise(SupplyRewardInfo info)
    {
        var handler = RewardGranted;
        if (handler == null)
        {
            info.grant?.Invoke();
            return;
        }
        handler(info);
    }
}
