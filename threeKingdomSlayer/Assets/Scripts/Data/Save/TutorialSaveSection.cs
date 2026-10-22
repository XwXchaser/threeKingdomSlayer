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
    }

    /// <summary>教学关是否已完成。</summary>
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
}
