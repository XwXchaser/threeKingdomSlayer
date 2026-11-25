using System.Collections.Generic;

/// <summary>
/// **主武器与招式分片**（2026-11-24）：装备的主武器 + 已解锁招式。
///
/// 两个字段各管一件事：
///   · `equippedWeaponId` —— 现在是哪把武器（局外切，下一局生效）；
///   · `unlockedWeaponMoves` —— 已永久解锁的招式 id。
///
/// ⚠ 招式**不做纯数值升级**（用户 2026-11-24 定）：解锁一个节点就是“多一招”，
/// 不做“某招 +10% 伤害”那种等级。以后要成长，走招式树里新增的招式/派生招式节点。
///
/// ⚠ 与天赋树**无关**：招式不再由 `talent_move_*` 节点售卖（那些节点已随 Moves 树移出）。
/// </summary>
public sealed class WeaponSaveSection : ISaveSection
{
    public string Id => "weapon";

    public void Migrate(SaveData data)
    {
        if (data.equippedWeaponId == null) data.equippedWeaponId = "";
        if (data.unlockedWeaponMoves == null) data.unlockedWeaponMoves = new List<string>();

        // 旧档可能会留下空 id（手改档 / 早期版本），清掉避免脏数据
        for (int i = data.unlockedWeaponMoves.Count - 1; i >= 0; i--)
            if (string.IsNullOrEmpty(data.unlockedWeaponMoves[i]))
                data.unlockedWeaponMoves.RemoveAt(i);
    }

    public static string GetEquippedWeaponId(SaveData data)
    {
        return data != null && data.equippedWeaponId != null ? data.equippedWeaponId : "";
    }

    public static void SetEquippedWeaponId(SaveData data, string weaponId)
    {
        if (data == null) return;
        data.equippedWeaponId = weaponId ?? "";
    }

    public static bool IsMoveUnlocked(SaveData data, string moveId)
    {
        if (data == null || string.IsNullOrEmpty(moveId)) return false;
        if (data.unlockedWeaponMoves == null) return false;
        return data.unlockedWeaponMoves.Contains(moveId);
    }

    /// <summary>解锁一个招式（重复调用无副作用）。</summary>
    public static void UnlockMove(SaveData data, string moveId)
    {
        if (data == null || string.IsNullOrEmpty(moveId)) return;
        if (data.unlockedWeaponMoves == null) data.unlockedWeaponMoves = new List<string>();
        if (!data.unlockedWeaponMoves.Contains(moveId)) data.unlockedWeaponMoves.Add(moveId);
    }

    /// <summary>清空武器与招式记录（重置用）。</summary>
    public static void Clear(SaveData data)
    {
        if (data == null) return;
        data.equippedWeaponId = "";
        if (data.unlockedWeaponMoves != null) data.unlockedWeaponMoves.Clear();
    }
}
