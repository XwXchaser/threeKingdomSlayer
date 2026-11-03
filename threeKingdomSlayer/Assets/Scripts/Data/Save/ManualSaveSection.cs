using System.Collections.Generic;

/// <summary>
/// **秘籍分片**：已获得的秘籍 id 列表（<c>SaveData.unlockedManuals</c>）。
///
/// 与 <see cref="TalentSaveSection"/> 同构：唯一权威是存档里的这份列表，查询不缓存副本。
/// </summary>
public sealed class ManualSaveSection : ISaveSection
{
    public string Id => "manual";

    public void Migrate(SaveData data)
    {
        // 老档没有秘籍字段，JsonUtility 会给 null，必须补空列表否则查询空引用
        if (data.unlockedManuals == null) data.unlockedManuals = new List<string>();
    }

    /// <summary>已获得的秘籍 id 列表（只读视图 ✅）。</summary>
    public static List<string> GetUnlocked(SaveData data)
    {
        if (data.unlockedManuals == null) data.unlockedManuals = new List<string>();
        return data.unlockedManuals;
    }

    /// <summary>某个秘籍是否已获得。</summary>
    public static bool IsUnlocked(SaveData data, string manualId)
    {
        if (data == null || string.IsNullOrEmpty(manualId)) return false;
        return GetUnlocked(data).Contains(manualId);
    }

    /// <summary>获得一个秘籍（重复调用无副作用 ✅）。</summary>
    public static void Unlock(SaveData data, string manualId)
    {
        if (data == null || string.IsNullOrEmpty(manualId)) return;
        var unlocked = GetUnlocked(data);
        if (!unlocked.Contains(manualId)) unlocked.Add(manualId);
    }

    /// <summary>清空已获得列表（重置用）。</summary>
    public static void Clear(SaveData data)
    {
        if (data == null) return;
        GetUnlocked(data).Clear();
    }
}
