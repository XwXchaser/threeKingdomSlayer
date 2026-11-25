using UnityEngine;

/// <summary>
/// **主武器 / 招式服务**（2026-11-24）：装备判定、招式解锁与升级、伤害加成。
///
/// 数据来源全部走 Inspector 引用链（不做 Resources.Load / 字符串查表）：
/// `PlayerState.heroConfig.weaponCatalog` → `MainWeaponConfig` → `WeaponMoveEntry`。
///
/// 三条口径：
///   · **装备**：只存 `SaveData.equippedWeaponId`；本局内不切武器，下一次进关由
///     <see cref="ApplyEquippedHeroConfig"/> 把 `PlayerState.heroConfig` 指到该武器的配置；
///   · **招式解锁**：`unlockedByDefault` 或存档 `unlockedWeaponMoves`；
///   · **招式树**：每条 entry 可挂前置（`prerequisiteIds`），节点只放招式/派生招式，**不做纯数值升级**。
///
/// ⚠ 招式**不再**由天赋树售卖：`TalentTree_Moves` 的 4 个节点已移出，
/// `HeroConfig.talentTree` 不再参与招式门判定（改由 <see cref="IsMoveUnlocked"/>）。
/// </summary>
public static class WeaponService
{
    /// <summary>武器目录（来自当前武将配置）。</summary>
    public static MainWeaponCatalog Catalog
    {
        get
        {
            var hero = PlayerState.Instance != null ? PlayerState.Instance.heroConfig : null;
            return hero != null ? hero.weaponCatalog : null;
        }
    }

    /// <summary>当前装备的武器（存档 id → 资产；失效则回落目录默认）。</summary>
    public static MainWeaponConfig Equipped
    {
        get
        {
            var catalog = Catalog;
            if (catalog == null) return null;

            var data = SaveManager.Load();
            string id = WeaponSaveSection.GetEquippedWeaponId(data);
            var byId = catalog.Find(id);
            return byId != null ? byId : catalog.Default;
        }
    }

    /// <summary>这把武器能不能装备（要有战斗配置）。</summary>
    public static bool IsWeaponUsable(MainWeaponConfig weapon)
    {
        return weapon != null && weapon.heroConfig != null;
    }

    /// <summary>装备一把武器（只写存档，下一次进关生效）。</summary>
    public static bool TryEquip(MainWeaponConfig weapon)
    {
        if (!IsWeaponUsable(weapon)) return false;

        var data = SaveManager.Load();
        if (data == null) return false;
        WeaponSaveSection.SetEquippedWeaponId(data, weapon.EffectiveWeaponId);
        SaveManager.Save(data);
        DebugLog.Info($"[Weapon] 装备主武器 {weapon.displayName}（下一次进关生效）");
        return true;
    }

    /// <summary>
    /// **把存档里装备的武器套到玩家身上**（进关初始化时调用一次）。
    /// 不在这里改血量 / 重置状态 —— 只换配置引用，数值由既有初始化流程读取。
    /// </summary>
    public static void ApplyEquippedHeroConfig(PlayerState player)
    {
        if (player == null) return;
        var weapon = Equipped;
        if (weapon == null || weapon.heroConfig == null) return;
        if (player.heroConfig == weapon.heroConfig) return;

        player.heroConfig = weapon.heroConfig;
        DebugLog.Info($"[Weapon] 应用主武器配置 {weapon.displayName} → {weapon.heroConfig.name}");
    }

    /// <summary>这条招式是否已解锁。找不到归属武器时视为**未锁**（避免配置缺失把招式全锁死）。</summary>
    public static bool IsMoveUnlocked(AttackSkillConfig move)
    {
        var entry = FindEntry(move);
        if (entry == null) return true;
        return IsEntryUnlocked(entry);
    }

    /// <summary>\n    /// 直接按 entry 判定是否解锁（**不需要目录 / PlayerState** 能解析）：
    /// 引导收集这类编辑期 / 早期时序场景用这个，避免拿不到 PlayerState 时误判成已解锁。
    /// </summary>
    public static bool IsEntryUnlocked(WeaponMoveEntry entry)
    {
        if (entry == null) return true;
        if (entry.unlockedByDefault) return true;
        return WeaponSaveSection.IsMoveUnlocked(SaveManager.Load(), entry.EffectiveMoveId);
    }

    /// <summary>前置招式是否全部解锁（招式树）。配错 id 不卡人。</summary>
    public static bool ArePrerequisitesMet(MainWeaponConfig weapon, WeaponMoveEntry entry)
    {
        if (entry == null) return false;
        if (entry.prerequisiteIds == null || entry.prerequisiteIds.Count == 0) return true;

        for (int i = 0; i < entry.prerequisiteIds.Count; i++)
        {
            string reqId = entry.prerequisiteIds[i];
            if (string.IsNullOrEmpty(reqId)) continue;
            var req = weapon != null ? weapon.FindEntryById(reqId) : null;
            if (req == null) continue;
            if (!IsEntryUnlocked(req)) return false;
        }
        return true;
    }

    /// <summary>花铜钱解锁一个招式（要求前置已全部解锁）。</summary>
    public static WeaponMoveActionResult TryUnlockMove(MainWeaponConfig weapon, WeaponMoveEntry entry)
    {
        if (entry == null || entry.move == null) return WeaponMoveActionResult.Invalid;
        if (IsEntryUnlocked(entry)) return WeaponMoveActionResult.AlreadyUnlocked;
        if (!ArePrerequisitesMet(weapon, entry)) return WeaponMoveActionResult.PrerequisiteLocked;

        var data = SaveManager.Load();
        if (data == null) return WeaponMoveActionResult.Invalid;

        int cost = Mathf.Max(0, entry.unlockCoinCost);
        if (SaveManager.GetCoins() < cost) return WeaponMoveActionResult.NotEnoughCoins;
        if (cost > 0) SaveManager.SetCoins(SaveManager.GetCoins() - cost);

        WeaponSaveSection.UnlockMove(data, entry.EffectiveMoveId);
        SaveManager.Save(data);
        DebugLog.Info($"[Weapon] 解锁招式 {entry.displayName}（{entry.EffectiveMoveId}）花费 {cost}");
        return WeaponMoveActionResult.Success;
    }

    /// <summary>按招式资产找它所属的 entry（找不到 = 不归任何武器管）。</summary>
    public static WeaponMoveEntry FindEntry(AttackSkillConfig move)
    {
        if (move == null) return null;
        var catalog = Catalog;
        if (catalog == null || catalog.weapons == null) return null;

        for (int i = 0; i < catalog.weapons.Count; i++)
        {
            var weapon = catalog.weapons[i];
            if (weapon == null) continue;
            var entry = weapon.FindEntry(move);
            if (entry != null) return entry;
        }
        return null;
    }
}
