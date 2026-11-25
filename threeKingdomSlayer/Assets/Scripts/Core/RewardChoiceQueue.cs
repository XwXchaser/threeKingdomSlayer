using System.Collections.Generic;

/// <summary>波末奖励队列里的选择类型。</summary>
public enum RewardChoiceType
{
    /// <summary>属性 / 机制（经验升级）。</summary>
    LevelUp = 0,
    /// <summary>装备（精英 / Boss / 场景破坏物）。</summary>
    Equipment = 1,
    /// <summary>道具（V1 Boss 锦囊）。</summary>
    Item = 2,
}

/// <summary>队列中的一项：类型 + 该装备选项**掉落时**的内部圈数（属性 / 道具为 0）。</summary>
public struct RewardChoiceEntry
{
    public RewardChoiceType type;
    public int circle;
}

/// <summary>
/// **波末奖励队列**（2026-11-25）——按类型优先，而不是单纯按入队时间：
///   1. **所有 Equipment**（装备）先出；
///   2. 再出 **LevelUp**（属性 / 经验升级）；
///   3. 最后出 **Item**（仅 V1）。
///
/// 同类型内部保持**到达顺序**（先入队的先出）。
/// 这样即使装备箱在属性升级之后才到达，也会先弹装备，符合用户口径：
/// 「所有装备选择都排在所有属性升级之前」。
///
/// 圈数随装备项一起入队：抽取时用**掉落时**的圈数，而不是当前圈数（跨圈迟到箱不会套错档位）。
/// </summary>
public sealed class RewardChoiceQueue
{
    private readonly List<RewardChoiceEntry> _entries = new List<RewardChoiceEntry>();

    public int Count => _entries.Count;

    public void Clear() => _entries.Clear();

    public void Add(RewardChoiceType type, int circle = 0)
    {
        _entries.Add(new RewardChoiceEntry { type = type, circle = circle });
    }

    /// <summary>
    /// 取下一个：Equipment → LevelUp → Item；同类型取最早入队的那一项。
    /// 队列空返回 false。
    /// </summary>
    public bool TryDequeueNext(out RewardChoiceEntry entry)
    {
        int index = FirstIndexOf(RewardChoiceType.Equipment);
        if (index < 0) index = FirstIndexOf(RewardChoiceType.LevelUp);
        if (index < 0) index = FirstIndexOf(RewardChoiceType.Item);
        if (index < 0)
        {
            entry = default;
            return false;
        }

        entry = _entries[index];
        _entries.RemoveAt(index);
        return true;
    }

    private int FirstIndexOf(RewardChoiceType type)
    {
        for (int i = 0; i < _entries.Count; i++)
            if (_entries[i].type == type) return i;
        return -1;
    }
}
