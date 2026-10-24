using System.Collections.Generic;

/// <summary>
/// **局外补给分片**：记录已被领取过的局外补给实例 id（轮次 + 点位）。
///
/// 为什么要存档：局外补给按「服务器倒计时轮次」刷新，玩家离开再回来时，
/// 同一个实例不能重复发奖励（否则可以反复进出主菜单刷金币）。
///
/// 旧档没有这个字段 → 迁移时补空列表（当作「没领过」）✅。
/// </summary>
public sealed class SupplySaveSection : ISaveSection
{
    public string Id => "supply";

    public void Migrate(SaveData data)
    {
        if (data.claimedSupplyIds == null) data.claimedSupplyIds = new List<string>();
    }

    /// <summary>该实例是否已经领过奖励。</summary>
    public static bool IsClaimed(SaveData data, string instanceId)
    {
        if (data == null || data.claimedSupplyIds == null || string.IsNullOrEmpty(instanceId)) return false;
        return data.claimedSupplyIds.Contains(instanceId);
    }

    /// <summary>标记该实例已领取。</summary>
    public static void MarkClaimed(SaveData data, string instanceId)
    {
        if (data == null) return;
        if (data.claimedSupplyIds == null) data.claimedSupplyIds = new List<string>();
        if (string.IsNullOrEmpty(instanceId)) return;
        if (!data.claimedSupplyIds.Contains(instanceId)) data.claimedSupplyIds.Add(instanceId);
        Trim(data);
    }

    /// <summary>只保留最近若干条（领取记录是“防重复”的窗口，不是永久历史）。</summary>
    private static void Trim(SaveData data)
    {
        const int keep = 64;
        var ids = data.claimedSupplyIds;
        if (ids == null || ids.Count <= keep) return;
        ids.RemoveRange(0, ids.Count - keep);
    }
}
