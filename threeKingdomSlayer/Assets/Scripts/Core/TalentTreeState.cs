using System.Collections.Generic;

/// <summary>天赋解锁结果，UI 直接拿来做提示。</summary>
public enum TalentUnlockResult
{
    Success = 0,
    AlreadyUnlocked = 1,
    /// <summary>前置节点还没解锁</summary>
    Locked = 2,
    NotEnoughCoins = 3,
    Invalid = 4
}

/// <summary>
/// 天赋树的运行时状态：唯一权威是 SaveData.unlockedTalents（节点 id 列表）。
/// 不缓存副本，所有查询都从存档读，避免“存档改了但界面还是旧的”。
/// 铜钱统一走 SaveManager，不在这里另存一份。
/// </summary>
public static class TalentTreeState
{
    public static event System.Action OnChanged;

    private static List<string> Unlocked => TalentSaveSection.GetUnlocked(SaveManager.Load());

    public static bool IsUnlocked(string nodeId)
    {
        return TalentSaveSection.IsUnlocked(SaveManager.Load(), nodeId);
    }

    public static bool IsUnlocked(TalentNodeConfig node) => node != null && IsUnlocked(node.nodeId);

    /// <summary>可以点了：自己没解锁、前置已解锁（根节点恒满足）。</summary>
    public static bool IsAvailable(TalentTreeConfig tree, TalentNodeConfig node)
    {
        if (tree == null || node == null || IsUnlocked(node)) return false;
        var prereq = tree.GetPrerequisite(node);
        return prereq == null || IsUnlocked(prereq);
    }

    public static bool HasEnoughCoins(TalentNodeConfig node) =>
        node != null && SaveManager.GetCoins() >= node.coinCost;

    public static TalentUnlockResult TryUnlock(TalentTreeConfig tree, TalentNodeConfig node)
    {
        if (tree == null || node == null || string.IsNullOrEmpty(node.nodeId) || !node.HasValidUnlockPayload)
            return TalentUnlockResult.Invalid;
        if (IsUnlocked(node)) return TalentUnlockResult.AlreadyUnlocked;

        var prereq = tree.GetPrerequisite(node);
        if (prereq != null && !IsUnlocked(prereq)) return TalentUnlockResult.Locked;
        if (!HasEnoughCoins(node)) return TalentUnlockResult.NotEnoughCoins;

        int coins = SaveManager.GetCoins();
        if (node.coinCost > 0)
            SaveManager.SetCoins(coins - node.coinCost);

        var data = SaveManager.Load();
        TalentSaveSection.Unlock(data, node.nodeId);
        SaveManager.Save(data);

        DebugLog.Info($"[Talent] 解锁 {node.nodeId}({node.displayName}) 花费 {node.coinCost}，剩余铜钱 {SaveManager.GetCoins()}");
        OnChanged?.Invoke();
        return TalentUnlockResult.Success;
    }

    /// <summary>机械开关查询（破阵/追击/运筹…）。flag 只看是否被任何已解锁节点点亮。</summary>
    public static bool IsFlagUnlocked(TalentTreeConfig tree, string flagId)
    {
        if (tree == null || tree.nodes == null || string.IsNullOrEmpty(flagId)) return false;
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var n = tree.nodes[i];
            if (n == null || n.unlockKind != TalentUnlockKind.Flag || n.flagId != flagId) continue;
            if (IsUnlocked(n)) return true;
        }
        return false;
    }

    /// <summary>招式是否已被天赋解锁（C2/C4 这类首轮锁定的招式靠这里判定）。</summary>
    public static bool IsMoveUnlocked(TalentTreeConfig tree, AttackSkillConfig move)
    {
        if (tree == null || move == null || tree.nodes == null) return false;
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var n = tree.nodes[i];
            if (n == null || n.unlockKind != TalentUnlockKind.MoveUnlock || n.moveConfig != move) continue;
            if (IsUnlocked(n)) return true;
        }
        return false;
    }

    /// <summary>
    /// 该招式是否「配了解锁节点、但还没解锁」。
    /// 没有对应节点的招式一律视为不受天赋管（返回 false），避免配置缺失把招式全锁死。
    /// </summary>
    public static bool IsMoveGatedAndLocked(TalentTreeConfig tree, AttackSkillConfig move)
    {
        if (tree == null || move == null || tree.nodes == null) return false;
        for (int i = 0; i < tree.nodes.Count; i++)
        {
            var n = tree.nodes[i];
            if (n == null || n.unlockKind != TalentUnlockKind.MoveUnlock || n.moveConfig != move) continue;
            return !IsUnlocked(n);
        }
        return false;
    }

    public static int UnlockedCount(TalentTreeConfig tree)
    {
        if (tree == null || tree.nodes == null) return 0;
        int count = 0;
        for (int i = 0; i < tree.nodes.Count; i++)
            if (IsUnlocked(tree.nodes[i])) count++;
        return count;
    }

    /// <summary>清空全部天赋（删档时用）。</summary>
    public static void ResetAll()
    {
        var data = SaveManager.Load();
        TalentSaveSection.Clear(data);
        SaveManager.Save(data);
        OnChanged?.Invoke();
    }
}

