using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **饰品槽（换装）入口**（2026-11-24）：本局内已装备的饰品可以插拔、替换。
///
/// 口径（`design/gameplay-progression-loop.md` §3.4）：
///   · 饰品 = **占被动槽**的机制型被动（`AttackPassive` / `TimedPassive`）；
///   · 换掉饰品**不移除**玩家已获得的成长属性（属性是 `Numeric`，根本不在这套里）；
///   · 槽位基础 2 格，后面由局外成长扩（`TalentCapabilities.ExtraPassiveSlot`）。
///
/// 为什么不做一份独立的「已装备列表」：
///   唯一权威是 `PlayerState.acquiredUpgrades`（UI 的饰品排也是读它），
///   再多存一份就会两边打架。这里只提供**增 / 换 / 减 + 槽位判定**，
///   真正落地仍然走 <see cref="UpgradeEffectManager"/>（它才是效果与槽位计数的持有者）。
///
/// ⚠ 只用于**占槽**饰品。机制型条目（地刺 / 击退波…）不占槽，不能在这里换装。
/// </summary>
public static class AccessoryLoadout
{
    /// <summary>没有武将配置时的被动槽缺省值（与 `HeroConfig.passiveSlotCount` 基础值一致）。</summary>
    public const int DefaultCapacity = 2;

    /// <summary>被动槽上限（基础值 + 局外天赋）。</summary>
    public static int Capacity
    {
        get
        {
            var hero = PlayerState.Instance != null ? PlayerState.Instance.heroConfig : null;
            int baseCap = hero != null ? Mathf.Max(1, hero.passiveSlotCount) : DefaultCapacity;
            return baseCap + TalentCapabilities.ExtraPassiveSlot;
        }
    }

    /// <summary>当前占用的槽位数（只数机制型被动）。</summary>
    public static int UsedSlots => UpgradeEffectManager.Instance != null
        ? UpgradeEffectManager.Instance.PassiveSlotUserCount
        : 0;

    public static bool IsFull => UsedSlots >= Capacity;

    /// <summary>这件定义能不能走换装（= 占被动槽的饰品）。</summary>
    public static bool IsSwappable(UpgradeDefinition def)
    {
        if (def == null) return false;
        return def.category == UpgradeCategory.AttackPassive || def.category == UpgradeCategory.TimedPassive;
    }

    /// <summary>当前装备的饰品（按 `PlayerState.acquiredUpgrades` 的持有顺序）。</summary>
    public static List<UpgradeDefinition> Equipped()
    {
        var result = new List<UpgradeDefinition>();
        var items = EquippedItems();
        for (int i = 0; i < items.Count; i++) result.Add(items[i].definition);
        return result;
    }

    /// <summary>当前装备的饰品 + 各自等级（满槽换装列表用）。</summary>
    public static List<EquippedItem> EquippedItems()
    {
        var result = new List<EquippedItem>();
        var ps = PlayerState.Instance;
        if (ps == null) return result;
        for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
        {
            var u = ps.acquiredUpgrades[i];
            if (u == null || u.definition == null) continue;
            if (!IsSwappable(u.definition)) continue;
            result.Add(new EquippedItem(u.definition, u.currentLevel));
        }
        return result;
    }

    /// <summary>装上一件饰品（有空槽、且没有同款才成功）。等级 = 变体的可见等级。</summary>
    public static bool TryEquip(UpgradeDefinition def)
    {
        if (!IsSwappable(def)) return false;
        if (IsFull) return false;
        if (IsKindEquipped(def.KindId)) return false;

        var uem = UpgradeEffectManager.Instance;
        if (uem == null) return false;
        if (!uem.ApplyUpgradeAtLevel(def, LevelOf(def))) return false;
        return IsKindEquipped(def.KindId);
    }

    /// <summary>
    /// **把一件换成另一件**：拆下旧的 → 按可见等级装上新的。
    /// 新的一件装不上时**回滚**旧的那件（保持原等级），不留下“拆了没装”的中间态。
    /// </summary>
    public static bool TryReplace(UpgradeDefinition oldDef, UpgradeDefinition newDef)
    {
        if (!IsSwappable(oldDef) || !IsSwappable(newDef)) return false;
        if (!IsKindEquipped(oldDef.KindId)) return false;
        if (IsKindEquipped(newDef.KindId) && newDef.KindId != oldDef.KindId) return false;   // 新的已经在别的槽

        var uem = UpgradeEffectManager.Instance;
        if (uem == null) return false;

        // 同一个变体（同池同品质同等级）再次被抽到：已装备且等级一致 = 幂等成功；
        // 等级不一致 = 按新变体的可见等级重新登记（不拆旧件）。
        // ⚠ 旧实现直接 return false → 三选一被误判为换装失败并整组作废（2026-11-25 实测）。
        if (ReferenceEquals(oldDef, newDef))
        {
            int targetLevel = LevelOf(newDef);
            if (uem.GetUpgradeLevel(oldDef.upgradeId) == targetLevel) return true;
            return uem.ApplyUpgradeAtLevel(newDef, targetLevel);
        }

        int oldLevel = uem.GetUpgradeLevel(oldDef.upgradeId);
        if (!uem.RemoveUpgrade(oldDef)) return false;

        if (uem.ApplyUpgradeAtLevel(newDef, LevelOf(newDef))) return true;

        // 回滚：把旧的那件按原等级重新登记，避免“换装失败但旧的已经没了”。
        if (oldLevel > 0 && uem.ApplyUpgradeAtLevel(oldDef, oldLevel))
            Debug.LogWarning($"[AccessoryLoadout] 换装到 {newDef.upgradeId} 失败，已回滚 {oldDef.upgradeId}（Lv.{oldLevel}）");
        else
            Debug.LogError($"[AccessoryLoadout] 换装到 {newDef.upgradeId} 失败，且回滚 {oldDef.upgradeId} 也失败");
        return false;
    }

    /// <summary>拆下一件饰品（换装失败时的回滚 / 玩家主动卸下）。</summary>
    public static bool TryRemove(UpgradeDefinition def)
    {
        if (!IsSwappable(def)) return false;
        var uem = UpgradeEffectManager.Instance;
        return uem != null && uem.RemoveUpgrade(def);
    }

    /// <summary>这个变体装备后实际生效的等级（装备变体的 visibleLevel；非法值按 1 兜底）。</summary>
    public static int LevelOf(UpgradeDefinition def)
    {
        return def != null ? Mathf.Max(1, def.visibleLevel) : 1;
    }

    public static bool IsEquipped(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return false;
        var ps = PlayerState.Instance;
        if (ps == null) return false;
        for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
        {
            var u = ps.acquiredUpgrades[i];
            if (u != null && u.definition != null && u.definition.upgradeId == upgradeId) return true;
        }
        return false;
    }

    /// <summary>该**种类**的饰品是否已在身上（不同品质 / 等级算同款）。</summary>
    public static bool IsKindEquipped(string kindId)
    {
        if (string.IsNullOrEmpty(kindId)) return false;
        var ps = PlayerState.Instance;
        if (ps == null) return false;
        for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
        {
            var u = ps.acquiredUpgrades[i];
            if (u == null || u.definition == null) continue;
            if (!IsSwappable(u.definition)) continue;
            if (u.definition.KindId == kindId) return true;
        }
        return false;
    }
}
