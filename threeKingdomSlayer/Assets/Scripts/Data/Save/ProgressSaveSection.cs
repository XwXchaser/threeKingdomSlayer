using System.Collections.Generic;

/// <summary>
/// **进度分片**：已通关的关卡 id 列表（<c>SaveData.clearedStageIds</c>）。
/// 关卡解锁顺序由它决定（已通关数 + 1 ✅）。
/// </summary>
public sealed class ProgressSaveSection : ISaveSection
{
    public string Id => "progress";

    public void Migrate(SaveData data)
    {
        if (data.clearedStageIds == null) data.clearedStageIds = new List<int>();
    }

    /// <summary>标记关卡通关（重复调用无副作用 ✅）。</summary>
    public static void MarkCleared(SaveData data, int stageId)
    {
        var list = GetCleared(data);
        if (!list.Contains(stageId)) list.Add(stageId);
    }

    /// <summary>关卡是否已通关。</summary>
    public static bool IsCleared(SaveData data, int stageId) => GetCleared(data).Contains(stageId);

    /// <summary>已通关的关卡 id 列表。</summary>
    public static List<int> GetCleared(SaveData data)
    {
        if (data.clearedStageIds == null) data.clearedStageIds = new List<int>();
        return data.clearedStageIds;
    }

    /// <summary>下一个可用关卡 id（最大已通关 id + 1，没有则 1 ✅）。</summary>
    public static int GetNextAvailableStageId(SaveData data)
    {
        int maxCleared = 0;
        var cleared = GetCleared(data);
        for (int i = 0; i < cleared.Count; i++)
            if (cleared[i] > maxCleared) maxCleared = cleared[i];
        return maxCleared + 1;
    }
}
