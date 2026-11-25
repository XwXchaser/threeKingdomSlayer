using System.Collections.Generic;

/// <summary>
/// **装备解锁分片**：已**永久解锁**的影之武装 / 饰品 id
/// （<c>SaveData.unlockedActiveSkills</c> / <c>SaveData.unlockedAccessories</c>）。
///
/// 与 <see cref="ManualSaveSection"/> 同构：唯一权威是存档里的这份列表，查询不缓存副本。
///
/// ⚠ 只记「**解锁资格**」，不记「本局持有」：
///   · 永久解锁 = 之后的对局里这件装备可以出现在装备宝箱候选里（不是立刻装备）；
///   · 本局持有 = <c>ActiveSkillInventory</c> / <c>PlayerState.acquiredUpgrades</c>，本局结束即清空。
///
/// ⚠ 列表里存的是 <c>UpgradeDefinition.upgradeId</c>，两种装备各存一份，**不混在一个列表**：
/// 主动技能走主动槽，饰品走被动槽，混存会让槽位判定读错池。
/// </summary>
public sealed class EquipmentUnlockSaveSection : ISaveSection
{
    public string Id => "equipment_unlock";

    public void Migrate(SaveData data)
    {
        // 老档没有这两个字段，JsonUtility 会给 null，必须补空列表否则查询空引用
        if (data.unlockedActiveSkills == null) data.unlockedActiveSkills = new List<string>();
        if (data.unlockedAccessories == null) data.unlockedAccessories = new List<string>();
    }

    /// <summary>已解锁的影之武装 id 列表（只读视图 ✅）。</summary>
    public static List<string> GetUnlockedActiveSkills(SaveData data)
    {
        if (data.unlockedActiveSkills == null) data.unlockedActiveSkills = new List<string>();
        return data.unlockedActiveSkills;
    }

    /// <summary>已解锁的饰品 id 列表（只读视图 ✅）。</summary>
    public static List<string> GetUnlockedAccessories(SaveData data)
    {
        if (data.unlockedAccessories == null) data.unlockedAccessories = new List<string>();
        return data.unlockedAccessories;
    }

    /// <summary>某件影之武装是否已永久解锁。</summary>
    public static bool IsActiveSkillUnlocked(SaveData data, string upgradeId)
    {
        if (data == null || string.IsNullOrEmpty(upgradeId)) return false;
        return GetUnlockedActiveSkills(data).Contains(upgradeId);
    }

    /// <summary>某件饰品是否已永久解锁。</summary>
    public static bool IsAccessoryUnlocked(SaveData data, string upgradeId)
    {
        if (data == null || string.IsNullOrEmpty(upgradeId)) return false;
        return GetUnlockedAccessories(data).Contains(upgradeId);
    }

    /// <summary>永久解锁一件影之武装（重复调用无副作用 ✅）。</summary>
    public static void UnlockActiveSkill(SaveData data, string upgradeId)
    {
        if (data == null || string.IsNullOrEmpty(upgradeId)) return;
        var unlocked = GetUnlockedActiveSkills(data);
        if (!unlocked.Contains(upgradeId)) unlocked.Add(upgradeId);
    }

    /// <summary>永久解锁一件饰品（重复调用无副作用 ✅）。</summary>
    public static void UnlockAccessory(SaveData data, string upgradeId)
    {
        if (data == null || string.IsNullOrEmpty(upgradeId)) return;
        var unlocked = GetUnlockedAccessories(data);
        if (!unlocked.Contains(upgradeId)) unlocked.Add(upgradeId);
    }

    /// <summary>清空两边的解锁记录（重置用）。</summary>
    public static void Clear(SaveData data)
    {
        if (data == null) return;
        GetUnlockedActiveSkills(data).Clear();
        GetUnlockedAccessories(data).Clear();
    }
}
