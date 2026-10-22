using System.Collections.Generic;

/// <summary>
/// **天赋分片**：已解锁的天赋节点 id 列表（<c>SaveData.unlockedTalents</c>）。
///
/// 唯一权威是存档里的这份列表 ✅ —— 天赋树运行时状态（<c>TalentTreeState</c>）只是它的视图 ✅。
/// </summary>
public sealed class TalentSaveSection : ISaveSection
{
    public string Id => "talent";

    public void Migrate(SaveData data)
    {
        // 老档没有天赋字段，JsonUtility 会给 null，必须补上否则所有天赋查询空引用
        if (data.unlockedTalents == null) data.unlockedTalents = new List<string>();
    }

    /// <summary>已解锁的节点 id 列表（只读视图 ✅）。</summary>
    public static List<string> GetUnlocked(SaveData data)
    {
        if (data.unlockedTalents == null) data.unlockedTalents = new List<string>();
        return data.unlockedTalents;
    }

    /// <summary>某个节点是否已解锁。</summary>
    public static bool IsUnlocked(SaveData data, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        return GetUnlocked(data).Contains(nodeId);
    }

    /// <summary>解锁一个节点（重复调用无副作用 ✅）。</summary>
    public static void Unlock(SaveData data, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        var unlocked = GetUnlocked(data);
        if (!unlocked.Contains(nodeId)) unlocked.Add(nodeId);
    }

    /// <summary>清空已解锁列表（重置天赋用）。</summary>
    public static void Clear(SaveData data)
    {
        GetUnlocked(data).Clear();
    }
}
