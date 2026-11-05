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

    /// <summary>
    /// **天赋页教学是否已教过**（2026-11-04）。
    /// 第一次打开天赋页会硬性教「怎么解锁天赋」→ 玩家**真的解锁了一个节点**才算教完。
    /// </summary>
    public static bool IsTalentPanelTaught(SaveData data)
    {
        return data != null && data.taughtTalentPanel;
    }

    /// <summary>标记「天赋页教学已教过」（重复调用无副作用 ✅）。</summary>
    public static void MarkTalentPanelTaught(SaveData data)
    {
        if (data == null) return;
        data.taughtTalentPanel = true;
    }

    /// <summary>
    /// **「点开始关卡」的主菜单引导**是否已教过（2026-11-04）。
    /// 玩家点过一次（或正式进入战斗）后 → 同一段教程里不再重复出现。
    /// </summary>
    public static bool IsStartStageTaught(SaveData data)
    {
        return data != null && data.taughtStartStage;
    }

    /// <summary>标记「开始关卡引导已教过」（重复调用无副作用 ✅）。</summary>
    public static void MarkStartStageTaught(SaveData data)
    {
        if (data == null) return;
        data.taughtStartStage = true;
    }
}
