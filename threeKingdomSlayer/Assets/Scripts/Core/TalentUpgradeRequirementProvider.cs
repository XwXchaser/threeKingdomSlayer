using System.Collections.Generic;

/// <summary>
/// **按天赋解锁入池**的条件源（<see cref="IUpgradeRequirementProvider"/> 的内置实现）。
///
/// 升级定义里填了 <c>requiredTalentIds</c>（需要已解锁的天赋节点 id）时生效；
/// 只要有其中任意一个天赋没解锁，这条升级就不进池。
///
/// 天赋状态以 <see cref="TalentTreeState"/> 为准（唯一权威是存档里的 unlockedTalents）。
/// </summary>
public sealed class TalentUpgradeRequirementProvider : IUpgradeRequirementProvider
{
    /// <inheritdoc/>
    public bool AppliesTo(UpgradeDefinition def, UpgradePoolKind pool)
    {
        return def != null && def.requiredTalentIds != null && def.requiredTalentIds.Count > 0;
    }

    /// <inheritdoc/>
    public bool IsSatisfied(UpgradeDefinition def, UpgradePoolKind pool)
    {
        if (def == null || def.requiredTalentIds == null) return true;
        for (int i = 0; i < def.requiredTalentIds.Count; i++)
        {
            var talentId = def.requiredTalentIds[i];
            if (string.IsNullOrEmpty(talentId)) continue;
            if (!TalentTreeState.IsUnlocked(talentId)) return false;
        }
        return true;
    }
}
