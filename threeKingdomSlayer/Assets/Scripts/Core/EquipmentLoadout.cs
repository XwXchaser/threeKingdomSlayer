using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **装备换装入口**（2026-11-25）——影之武装 / 饰品的本局装备、同款替换与满槽换装。
///
/// 规则（`design/gameplay-progression-loop.md` §3.4 / P0 计划 §1.3）：
///   · 没有同款且有空槽 → 直接装备；
///   · **已有同款**（即使还有空槽）→ 点候选后直接替换原槽；品质 / 等级 / 效果以新变体为准，
///     不 +1、不继承、不取最高；
///   · 没有同款且该类满槽 → 返回 <see cref="EquipResult.NeedsReplacement"/>，
///     由 UI 在候选下方列出同类别已装备名单，玩家点旧装备后调 <see cref="TryReplace"/>；
///   · 同一件装备不能同时占两个槽（按 <see cref="UpgradeDefinition.KindId"/> 判定）；
///   · 替换不动本局成长属性（属性是 Numeric，根本不在这里）。
///
/// 为什么单独一层：主动（<see cref="ActiveSkillInventory"/>）与被动（<see cref="UpgradeEffectManager"/>）
/// 两套运行态的“换装”语义不同，调用方（三选一流程 / UI）不该关心这些差别。
/// </summary>
public static class EquipmentLoadout
{
    public enum EquipResult
    {
        /// <summary>已装上（含「同款直接替换原槽」）。</summary>
        Equipped,
        /// <summary>该类已满且没有同款 → 需要玩家在下方点一件旧装备替换（尚未改动任何状态）。</summary>
        NeedsReplacement,
        /// <summary>失败（配置 / 运行态异常），调用方应报错，不得当作成功消费奖励。</summary>
        Failed,
    }

    /// <summary>这个变体是不是可换装的装备（影之武装 / 占槽饰品）。</summary>
    public static bool IsEquippable(UpgradeDefinition def)
    {
        return EquipmentVariantRules.IsEquippableCategory(def)
            && EquipmentVariantRules.Validate(def) == null;
    }

    /// <summary>该变体是否已在身上（按种类）。</summary>
    public static bool IsEquippedKind(UpgradeDefinition def)
    {
        if (def == null) return false;
        return def.category == UpgradeCategory.ActiveSkill
            ? ActiveSkillInventory.Instance != null && ActiveSkillInventory.Instance.IsKindEquipped(def.KindId)
            : AccessoryLoadout.IsKindEquipped(def.KindId);
    }

    /// <summary>该变体所在类别的槽位是否已满。</summary>
    public static bool IsCategoryFull(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (def.category == UpgradeCategory.ActiveSkill)
            return ActiveSkillInventory.Instance == null || ActiveSkillInventory.Instance.IsFull;
        return AccessoryLoadout.IsFull;
    }

    /// <summary>
    /// 当前装备的、与 <paramref name="def"/> **同类别**的装备（满槽换装时展示的旧装备名单）。
    /// </summary>
    public static List<EquippedItem> GetReplacementCandidates(UpgradeDefinition def)
    {
        var result = new List<EquippedItem>();
        if (def == null) return result;

        if (def.category == UpgradeCategory.ActiveSkill)
        {
            var inventory = ActiveSkillInventory.Instance;
            if (inventory == null) return result;
            var entries = inventory.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.definition == null) continue;
                result.Add(new EquippedItem(entry.definition, entry.level));
            }
            return result;
        }

        return AccessoryLoadout.EquippedItems();
    }

    /// <summary>
    /// 尝试装上一个变体：
    ///   · 同款已在身上 → 直接替换原槽；
    ///   · 有空槽 → 直接装备；
    ///   · 满槽且无同款 → <see cref="EquipResult.NeedsReplacement"/>（不改状态）。
    /// </summary>
    public static EquipResult TryEquip(UpgradeDefinition variant, out string error)
    {
        error = null;
        if (variant == null)
        {
            error = "变体为空";
            return EquipResult.Failed;
        }
        string problem = EquipmentVariantRules.Validate(variant);
        if (problem != null)
        {
            error = problem;
            return EquipResult.Failed;
        }

        var sameKind = FindEquippedByKind(variant.KindId, variant.category);
        if (sameKind != null)
        {
            return TryReplace(sameKind, variant, out error)
                ? EquipResult.Equipped
                : EquipResult.Failed;
        }

        if (variant.category == UpgradeCategory.ActiveSkill)
        {
            var inventory = ActiveSkillInventory.Instance;
            if (inventory == null)
            {
                error = "ActiveSkillInventory 未找到";
                return EquipResult.Failed;
            }
            if (inventory.IsFull) return EquipResult.NeedsReplacement;
            return inventory.TryEquipVariant(variant as ActiveSkillDefinition,
                Mathf.Max(1, variant.visibleLevel), out _)
                ? EquipResult.Equipped
                : Fail("主动技能装备失败", out error);
        }

        if (AccessoryLoadout.IsFull) return EquipResult.NeedsReplacement;
        return AccessoryLoadout.TryEquip(variant)
            ? EquipResult.Equipped
            : Fail("饰品装备失败", out error);
    }

    /// <summary>
    /// 用 <paramref name="newVariant"/> 替换 <paramref name="oldDefinition"/> 所在的槽。
    /// 先拆后装；装上失败会回滚旧的那件（被动）。主动技能是原地换内容，失败不改状态。
    /// </summary>
    public static bool TryReplace(UpgradeDefinition oldDefinition, UpgradeDefinition newVariant, out string error)
    {
        error = null;
        if (oldDefinition == null || newVariant == null)
        {
            error = "换装对象为空";
            return false;
        }
        string problem = EquipmentVariantRules.Validate(newVariant);
        if (problem != null)
        {
            error = problem;
            return false;
        }
        // 影之武装 = 主动槽；饰品 = 被动槽。**AttackPassive 与 TimedPassive 都是饰品，可互换**
        // （2026-11-25 实测：替换列表里点旧饰品时被“类别不一致”误拒）。
        if (!SameSlotCategory(oldDefinition.category, newVariant.category))
        {
            error = $"类别不一致：{oldDefinition.upgradeId}（{oldDefinition.category}）→ {newVariant.upgradeId}（{newVariant.category}）";
            return false;
        }

        // 新变体的同款若已在**另一个**槽里，不能换（同一件不能占两格）
        var sameKind = FindEquippedByKind(newVariant.KindId, newVariant.category);
        if (sameKind != null && sameKind != oldDefinition)
        {
            error = $"{newVariant.upgradeId}: 同款已在另一个槽（{sameKind.upgradeId}）";
            return false;
        }

        if (newVariant.category == UpgradeCategory.ActiveSkill)
        {
            var inventory = ActiveSkillInventory.Instance;
            if (inventory == null)
            {
                error = "ActiveSkillInventory 未找到";
                return false;
            }
            var entry = inventory.FindByKind(oldDefinition.KindId);
            if (entry == null)
            {
                error = $"要替换的影之武装不在槽里（{oldDefinition.upgradeId}）";
                return false;
            }
            if (!inventory.ReplaceVariant(entry.id, newVariant as ActiveSkillDefinition,
                    Mathf.Max(1, newVariant.visibleLevel)))
            {
                error = "主动技能换装失败（槽位或同款冲突）";
                return false;
            }
            return true;
        }

        if (!AccessoryLoadout.IsKindEquipped(oldDefinition.KindId))
        {
            error = $"要替换的饰品不在身上（{oldDefinition.upgradeId}）";
            return false;
        }
        if (!AccessoryLoadout.TryReplace(oldDefinition, newVariant))
        {
            error = "饰品换装失败（拆卸或登记失败，已尝试回滚）";
            return false;
        }
        return true;
    }

    /// <summary>两个 category 是否属于同一个装备栏（主动 / 饰品）。</summary>
    private static bool SameSlotCategory(UpgradeCategory a, UpgradeCategory b)
    {
        if (a == b) return true;
        bool aPassive = a == UpgradeCategory.AttackPassive || a == UpgradeCategory.TimedPassive;
        bool bPassive = b == UpgradeCategory.AttackPassive || b == UpgradeCategory.TimedPassive;
        return aPassive && bPassive;
    }

    /// <summary>
    /// 某个变体的同款当前装备在哪一件上（没有返回 null）。
    /// **必须带类别**：主动旋风与被动旋风是不同种类，不能因为同名就跨类别合并。
    /// </summary>
    public static UpgradeDefinition FindEquippedByKind(string kindId, UpgradeCategory category)
    {
        if (string.IsNullOrEmpty(kindId)) return null;

        if (category == UpgradeCategory.ActiveSkill)
        {
            var inventory = ActiveSkillInventory.Instance;
            if (inventory == null) return null;
            var entry = inventory.FindByKind(kindId);
            return entry != null ? entry.definition : null;
        }

        if (category != UpgradeCategory.AttackPassive && category != UpgradeCategory.TimedPassive) return null;

        var ps = PlayerState.Instance;
        if (ps == null) return null;
        for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
        {
            var u = ps.acquiredUpgrades[i];
            if (u == null || u.definition == null) continue;
            if (!AccessoryLoadout.IsSwappable(u.definition)) continue;
            if (u.definition.KindId == kindId) return u.definition;
        }
        return null;
    }

    /// <summary>某件装备当前生效的等级（用于 UI 对比旧装备）。</summary>
    public static int GetEquippedLevel(UpgradeDefinition def)
    {
        if (def == null) return 0;
        if (def.category == UpgradeCategory.ActiveSkill)
        {
            var inventory = ActiveSkillInventory.Instance;
            return inventory != null ? inventory.GetLevel(def.upgradeId) : 0;
        }
        var uem = UpgradeEffectManager.Instance;
        return uem != null ? uem.GetUpgradeLevel(def.upgradeId) : 0;
    }

    private static EquipResult Fail(string message, out string error)
    {
        error = message;
        return EquipResult.Failed;
    }
}
