/// <summary>
/// 道具弃置弹窗的**结果**（纯数据）。
///
/// 2026-10-22（asmdef 分层第四批）：原本是 <c>ItemDiscardPopup.Result</c>（嵌在界面组件里），
/// 下沉到契约层 —— 内核通过 <c>IItemDiscardPrompt</c> 拿结果，不该看见界面层的嵌套类型。
/// </summary>
public struct ItemDiscardResult
{
    /// <summary>true = 丢弃新道具（玩家没弃掉旧的，只是没要新的）。</summary>
    public bool DiscardNew { get; set; }

    /// <summary>被弃掉的旧条目 id（<see cref="DiscardNew"/> 为 false 时有效）。</summary>
    public int EntryId { get; set; }

    public static ItemDiscardResult NewItem() => new ItemDiscardResult { DiscardNew = true, EntryId = -1 };

    public static ItemDiscardResult Existing(int entryId) => new ItemDiscardResult { DiscardNew = false, EntryId = entryId };
}
