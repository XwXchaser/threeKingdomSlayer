using System.Collections.Generic;

/// <summary>
/// **招式引导分片**：已经教过的招式引导 id 列表（<c>SaveData.taughtMoveGuides</c>）。
///
/// 与 <see cref="TalentSaveSection"/> 同构：唯一权威是存档里的这份列表 ✅。
/// 语义：**「这个招式的解锁引导已经教过一次」** —— 教过就不再弹 ✅（同一存档只教一次 ✅）。
/// </summary>
public sealed class MoveGuideSaveSection : ISaveSection
{
    public string Id => "moveguide";

    public void Migrate(SaveData data)
    {
        // 老档没有这个字段，JsonUtility 会给 null → 必须补空列表否则查询空引用
        if (data.taughtMoveGuides == null) data.taughtMoveGuides = new List<string>();
    }

    /// <summary>已教过的引导 id 列表（只读视图 ✅）。</summary>
    public static List<string> GetTaught(SaveData data)
    {
        if (data.taughtMoveGuides == null) data.taughtMoveGuides = new List<string>();
        return data.taughtMoveGuides;
    }

    /// <summary>某个引导是否已经教过。</summary>
    public static bool IsTaught(SaveData data, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return false;
        return GetTaught(data).Contains(nodeId);
    }

    /// <summary>标记为已教过（重复调用无副作用 ✅）。</summary>
    public static void MarkTaught(SaveData data, string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        var taught = GetTaught(data);
        if (!taught.Contains(nodeId)) taught.Add(nodeId);
    }
}
