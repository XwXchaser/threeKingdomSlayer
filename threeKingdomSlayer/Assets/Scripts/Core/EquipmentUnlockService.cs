/// <summary>
/// **装备永久解锁判定**（2026-11-24）：影之武装 / 饰品有没有资格出现在装备宝箱候选里。
///
/// 两类来源，二者取或：
///   ① 资产上勾了 `unlockedByDefault`（初始即解锁，demo 期现有内容全部勾上）；
///   ② 存档 `SaveData.unlockedActiveSkills` / `unlockedAccessories` 里有这条 id
///      （靠精英 / Boss / 探索宝箱永久解锁，见 <see cref="EquipmentUnlockSaveSection"/>）。
///
/// ⚠ **这不是「本局持有」**：
///   · 解锁 = 可以出现在候选里（不保证抽到、不自动装备）；
///   · 持有 = `ActiveSkillInventory` / `PlayerState.acquiredUpgrades`，本局结束即清空。
///
/// ⚠ 属性（Numeric，填了 statId）与秘籍（Manual）**不走这套解锁** —— 查询一律返回 true，
/// 因为它们的来源是局内经验 / 剧情掉落，不该被装备解锁卡住。
/// </summary>
public static class EquipmentUnlockService
{
    /// <summary>这条定义是不是「装备池」里的东西（影之武装 / 饰品）。
    ///
    /// 口径不在这里自定义：直接复用 <see cref="UpgradeRewardClassifier.IsEquipment"/>，
    /// 否则「什么算装备」会出现两套判定（奖励分类一套、解锁一套）而互相打架。</summary>
    public static bool IsEquipment(UpgradeDefinition def)
    {
        return UpgradeRewardClassifier.IsEquipment(def);
    }

    /// <summary>这件装备是否已永久解锁（含「初始即解锁」）。</summary>
    public static bool IsUnlocked(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (!IsEquipment(def)) return true;          // 属性 / 秘籍不归这套管
        if (def.unlockedByDefault) return true;

        var data = SaveManager.Load();
        if (data == null) return false;

        // 2026-11-25：解锁按**种类**（kindId）判定 —— 解锁「染病」后，后续宝箱可以抽到
        // 它配置允许的任意变体（普通 / 传说），不是只解锁某一张资产。
        // 现有资产的 kindId 留空 ⇒ 回退 upgradeId ⇒ 旧存档的 id 仍然命中。
        string kindId = def.KindId;
        return def.category == UpgradeCategory.ActiveSkill
            ? EquipmentUnlockSaveSection.IsActiveSkillUnlocked(data, kindId)
            : EquipmentUnlockSaveSection.IsAccessoryUnlocked(data, kindId);
    }

    /// <summary>
    /// 永久解锁一件装备并立刻落盘（幂等：已解锁返回 false）。
    /// </summary>
    public static bool Grant(UpgradeDefinition def)
    {
        if (!IsEquipment(def)) return false;
        string id = def.KindId;   // 解锁资格按种类存
        if (string.IsNullOrEmpty(id)) return false;

        var data = SaveManager.Load();
        if (data == null) return false;
        if (EquipmentUnlockSaveSection.IsActiveSkillUnlocked(data, id)
            || EquipmentUnlockSaveSection.IsAccessoryUnlocked(data, id))
            return false;

        if (def.category == UpgradeCategory.ActiveSkill)
            EquipmentUnlockSaveSection.UnlockActiveSkill(data, id);
        else
            EquipmentUnlockSaveSection.UnlockAccessory(data, id);

        SaveManager.Save(data);
        DebugLog.Info($"[EquipmentUnlock] 永久解锁 {def.displayName}（id={id}）");
        return true;
    }
}
