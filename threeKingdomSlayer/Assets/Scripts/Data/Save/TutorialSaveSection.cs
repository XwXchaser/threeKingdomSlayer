using System.Collections.Generic;

/// <summary>
/// **教学分片**：教学关是否完成 + 已播过的教学对话 id（<c>SaveData.tutorialCompleted</c> /
/// <c>completedTutorialDialogueIds</c>）。
/// </summary>
public sealed class TutorialSaveSection : ISaveSection
{
    public string Id => "tutorial";

    public void Migrate(SaveData data)
    {
        if (data.completedTutorialDialogueIds == null)
            data.completedTutorialDialogueIds = new List<string>();
        if (data.taughtTutorialBeats == null)
            data.taughtTutorialBeats = new List<string>();
    }

    /// <summary>教学关是否已完成（= 教学关被打通关）。</summary>
    public static bool IsCompleted(SaveData data) => data.tutorialCompleted;

    /// <summary>标记教学关已完成（已存档则不重复写 ✅）。</summary>
    public static void MarkCompleted(SaveData data)
    {
        if (data.tutorialCompleted) return;
        data.tutorialCompleted = true;
    }

    /// <summary>某条教学对话是否已播过。</summary>
    public static bool IsDialogueCompleted(SaveData data, string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return false;
        return GetCompletedDialogues(data).Contains(dialogueId);
    }

    /// <summary>标记一条教学对话已播过。</summary>
    public static void MarkDialogueCompleted(SaveData data, string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId)) return;
        var list = GetCompletedDialogues(data);
        if (!list.Contains(dialogueId)) list.Add(dialogueId);
    }

    /// <summary>已播过的教学对话 id 列表。</summary>
    public static List<string> GetCompletedDialogues(SaveData data)
    {
        if (data.completedTutorialDialogueIds == null)
            data.completedTutorialDialogueIds = new List<string>();
        return data.completedTutorialDialogueIds;
    }

    // ── 每一拍的「已教过」标记（2026-11-05 用户口径）──────────────────────
    //
    // 「我已完成点戳的教程，就应该在存档里标记完成，**而不是根据关卡通过来判断**」
    // → **每教完一拍就写**；下次进教学关（哪怕没通关）也**不再教这一拍**。
    //   引导拍（招式解锁）另有存档位：<c>MoveGuideSaveSection.taughtMoveGuides</c>。

    /// <summary>某一拍是否**已经教过**（存档标记）。</summary>
    public static bool IsBeatTaught(SaveData data, string beatId)
    {
        if (data == null || string.IsNullOrEmpty(beatId)) return false;
        return GetTaughtBeats(data).Contains(beatId);
    }

    /// <summary>标记某一拍已教完。返回 true = 本次真的写入了新标记。</summary>
    public static bool MarkBeatTaught(SaveData data, string beatId)
    {
        if (data == null || string.IsNullOrEmpty(beatId)) return false;
        var list = GetTaughtBeats(data);
        if (list.Contains(beatId)) return false;
        list.Add(beatId);
        return true;
    }

    private static List<string> GetTaughtBeats(SaveData data)
    {
        if (data.taughtTutorialBeats == null) data.taughtTutorialBeats = new List<string>();
        return data.taughtTutorialBeats;
    }
}
