using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级池采样器：从池配置收集**候选**并按权重 / 稀有度抽成若干选项。
///
/// 从 <c>UpgradeChoiceManager</c> 拆出（2026-10-22）。三选一管理器只管
/// 「触发 → 暂停 → 弹窗 → 确认 → 恢复」这条流程；「什么能进池、怎么抽」全在这里。
/// 新增入池条件（按天赋解锁等）只需注册一个 <see cref="IUpgradeRequirementProvider"/>，
/// 不用改管理器。
///
/// 依赖注入（不直接摸单例）：当前等级 / 被动上限 / 主动技能可获得性 三个查询由调用方提供，
/// 这样采样器可以脱离场景单独测。
/// </summary>
public sealed class UpgradePoolSampler
{
    /// <summary>候选条目（一条升级 + 权重 + 稀有度）。</summary>
    public struct Candidate
    {
        public UpgradeDefinition upgrade;
        public int weight;
        public UpgradeRarity rarity;
    }

    private readonly Func<UpgradeDefinition, int> _getLevel;
    private readonly Func<bool> _passiveCapReached;
    private readonly Func<ActiveSkillDefinition, bool> _canAcquireActiveSkill;
    private readonly List<IUpgradeRequirementProvider> _requirementProviders = new List<IUpgradeRequirementProvider>();

    public UpgradePoolSampler(
        Func<UpgradeDefinition, int> getLevel,
        Func<bool> passiveCapReached,
        Func<ActiveSkillDefinition, bool> canAcquireActiveSkill)
    {
        _getLevel = getLevel;
        _passiveCapReached = passiveCapReached;
        _canAcquireActiveSkill = canAcquireActiveSkill;
    }

    // ───────── 扩展点：入池条件源 ─────────

    /// <summary>注册入池条件源（扩展点）。多个条件源是「与」的关系。</summary>
    public void AddRequirementProvider(IUpgradeRequirementProvider provider)
    {
        if (provider == null) return;
        _requirementProviders.Add(provider);
    }

    /// <summary>所有适用的条件源都满足 → 该升级可以入池。</summary>
    public bool RequirementsMet(UpgradeDefinition def, UpgradePoolKind pool)
    {
        for (int i = 0; i < _requirementProviders.Count; i++)
        {
            var provider = _requirementProviders[i];
            if (provider == null || !provider.AppliesTo(def, pool)) continue;
            if (!provider.IsSatisfied(def, pool)) return false;
        }
        return true;
    }

    // ───────── 收集候选 ─────────

    /// <summary>常规池（V1）：排除道具与主动技能；检查前置条件、附加条件、满级、被动上限。</summary>
    public void CollectRegular(List<Candidate> result, List<WeightedUpgrade> pool, UpgradeRarity rarity)
    {
        if (result == null || pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var wu = pool[i];
            if (wu == null || wu.upgrade == null) continue;
            // V1 Item 与 V2 ActiveSkill 均由各自独立池处理
            if (wu.upgrade.category == UpgradeCategory.Item || wu.upgrade.category == UpgradeCategory.ActiveSkill) continue;
            if (!PrerequisitesMet(wu.upgrade)) continue;
            if (!RequirementsMet(wu.upgrade, UpgradePoolKind.Regular)) continue;
            int currentLevel = GetLevel(wu.upgrade);
            if (currentLevel >= wu.upgrade.maxLevel) continue;
            // 被动技能上限 6 个：已满时只允许升级已有技能，不允许获得新技能
            if (currentLevel == 0 && IsPassiveCapReached()) continue;
            result.Add(new Candidate { upgrade = wu.upgrade, weight = wu.weight, rarity = rarity });
        }
    }

    /// <summary>V2 统一池：主动技能走可获得性，其余走前置条件 + 槽位检查。</summary>
    public void CollectUnified(List<Candidate> result, List<WeightedUpgrade> pool, UpgradeRarity rarity)
    {
        if (result == null || pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var wu = pool[i];
            if (wu == null || wu.upgrade == null) continue;
            if (wu.upgrade.category == UpgradeCategory.Item) continue;

            if (wu.upgrade.category == UpgradeCategory.ActiveSkill)
            {
                if (!CanAcquireActiveSkill(wu.upgrade as ActiveSkillDefinition)) continue;
            }
            else
            {
                if (!PrerequisitesMet(wu.upgrade)) continue;
                if (!RequirementsMet(wu.upgrade, UpgradePoolKind.Regular)) continue;
                int currentLevel = GetLevel(wu.upgrade);
                if (currentLevel >= wu.upgrade.maxLevel) continue;
                if (currentLevel == 0 && IsPassiveCapReached()) continue;
            }

            result.Add(new Candidate { upgrade = wu.upgrade, weight = Mathf.Max(1, wu.weight), rarity = rarity });
        }
    }

    /// <summary>道具池：检查前置条件、附加条件与满级。</summary>
    public void CollectItem(List<Candidate> result, List<WeightedUpgrade> pool, UpgradeRarity rarity)
    {
        if (result == null || pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var wu = pool[i];
            if (wu == null || wu.upgrade == null) continue;
            if (!PrerequisitesMet(wu.upgrade)) continue;
            if (!RequirementsMet(wu.upgrade, UpgradePoolKind.Item)) continue;
            if (GetLevel(wu.upgrade) >= wu.upgrade.maxLevel) continue;
            result.Add(new Candidate { upgrade = wu.upgrade, weight = wu.weight, rarity = rarity });
        }
    }

    /// <summary>主动技能池（V2）：只看主动技能的可获得性。</summary>
    public void CollectActiveSkills(List<Candidate> result, List<WeightedUpgrade> pool, UpgradeRarity rarity)
    {
        if (result == null || pool == null) return;
        for (int i = 0; i < pool.Count; i++)
        {
            var wu = pool[i];
            if (wu == null || wu.upgrade == null || wu.upgrade.category != UpgradeCategory.ActiveSkill) continue;
            var skill = wu.upgrade as ActiveSkillDefinition;
            if (skill == null) continue;
            if (!CanAcquireActiveSkill(skill)) continue;
            result.Add(new Candidate { upgrade = skill, weight = Mathf.Max(1, wu.weight), rarity = rarity });
        }
    }

    // ───────── 抽取 ─────────

    /// <summary>
    /// 按稀有度权重抽：每次先摇一个稀有度，在该稀有度的候选里按权重挑；
    /// 该稀有度不够就降级到所有剩余候选。
    /// </summary>
    public List<UpgradeDefinition> DrawByRarity(List<Candidate> candidates, int choiceCount, Func<UpgradeRarity> rollRarity)
    {
        var results = new List<UpgradeDefinition>();
        if (candidates == null || candidates.Count == 0) return results;

        var usedIds = new HashSet<string>();
        for (int i = 0; i < choiceCount && usedIds.Count < candidates.Count; i++)
        {
            UpgradeRarity targetRarity = rollRarity != null ? rollRarity() : UpgradeRarity.Common;

            // 同一稀有度中未选中的候选项
            var rarityCandidates = new List<Candidate>();
            for (int j = 0; j < candidates.Count; j++)
            {
                var c = candidates[j];
                if (c.rarity == targetRarity && !usedIds.Contains(c.upgrade.upgradeId))
                    rarityCandidates.Add(c);
            }

            // 稀有度池不够 → 降级到所有剩余候选项
            if (rarityCandidates.Count == 0)
            {
                for (int j = 0; j < candidates.Count; j++)
                {
                    var c = candidates[j];
                    if (!usedIds.Contains(c.upgrade.upgradeId))
                        rarityCandidates.Add(c);
                }
            }

            if (rarityCandidates.Count == 0) break;

            var picked = WeightedPick(rarityCandidates);
            results.Add(picked.upgrade);
            usedIds.Add(picked.upgrade.upgradeId);
        }
        return results;
    }

    /// <summary>纯按权重抽（不看稀有度）——Boss 锦囊的主动技能池用这个。</summary>
    public List<UpgradeDefinition> DrawByWeight(List<Candidate> candidates, int choiceCount)
    {
        var results = new List<UpgradeDefinition>();
        if (candidates == null || candidates.Count == 0) return results;

        var usedIds = new HashSet<string>();
        for (int i = 0; i < choiceCount && usedIds.Count < candidates.Count; i++)
        {
            var remaining = new List<Candidate>();
            for (int j = 0; j < candidates.Count; j++)
                if (!usedIds.Contains(candidates[j].upgrade.upgradeId))
                    remaining.Add(candidates[j]);
            if (remaining.Count == 0) break;
            var picked = WeightedPick(remaining);
            results.Add(picked.upgrade);
            usedIds.Add(picked.upgrade.upgradeId);
        }
        return results;
    }

    /// <summary>按权重随机抽取一个</summary>
    private static Candidate WeightedPick(List<Candidate> candidates)
    {
        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
            totalWeight += candidates[i].weight;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidates[i].weight;
            if (roll < cumulative)
                return candidates[i];
        }
        return candidates[candidates.Count - 1];
    }

    // ───────── 判定 ─────────

    /// <summary>前置条件：需要其他升级达到指定等级。</summary>
    private bool PrerequisitesMet(UpgradeDefinition def)
    {
        if (def == null) return false;
        if (def.prerequisites == null || def.prerequisites.Count == 0) return true;
        for (int i = 0; i < def.prerequisites.Count; i++)
        {
            var prereq = def.prerequisites[i];
            if (prereq == null || prereq.requiredUpgrade == null) continue;
            if (GetLevel(prereq.requiredUpgrade) < prereq.requiredLevel)
                return false;
        }
        return true;
    }

    private int GetLevel(UpgradeDefinition def)
    {
        return _getLevel != null ? _getLevel(def) : 0;
    }

    private bool IsPassiveCapReached()
    {
        return _passiveCapReached != null && _passiveCapReached();
    }

    private bool CanAcquireActiveSkill(ActiveSkillDefinition def)
    {
        return _canAcquireActiveSkill != null && _canAcquireActiveSkill(def);
    }
}
