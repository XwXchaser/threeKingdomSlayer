using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **经验升级（属性）抽取器**（2026-11-25）。
///
/// 规则（`design/gameplay-progression-loop.md` §3.3 / P0 计划 §1.4）：
///   · 品质出现概率**全局统一**，不随无尽内部圈数、不使用装备的池内档位；
///   · 同一次三选一按**升级内容身份**（<see cref="UpgradeDefinition.KindId"/>）去重；
///     「普通 生命上限」与「传说 生命上限」不同时出现；
///   · 不同次仍可继续获得同一属性 —— 这里只负责抽，实际累积沿用既有
///     「每选一次升一级、按该级增量叠加」的语义（见 <see cref="UpgradeEffectManager.ApplyUpgrade"/>）。
///
/// 资格过滤沿用既有口径：前置条件 / 天赋条件 / 本局等级未达有效上限。
/// ⚠ 满级过滤是**属性**的既有语义；装备不适用（装备没有「抽到同款就升级」）。
/// </summary>
public sealed class AttributeChoiceDrawer
{
    private readonly AttributePoolConfig _config;
    private readonly Func<UpgradeDefinition, int> _getLevel;
    private readonly Func<UpgradeDefinition, int> _getEffectiveMaxLevel;
    private readonly Func<UpgradeDefinition, bool> _prerequisitesMet;
    private readonly Func<UpgradeDefinition, bool> _requirementsMet;
    private readonly Func<float> _random01;

    public AttributeChoiceDrawer(
        AttributePoolConfig config,
        Func<UpgradeDefinition, int> getLevel,
        Func<UpgradeDefinition, int> getEffectiveMaxLevel,
        Func<UpgradeDefinition, bool> prerequisitesMet,
        Func<UpgradeDefinition, bool> requirementsMet,
        Func<float> random01 = null)
    {
        _config = config;
        _getLevel = getLevel ?? (_ => 0);
        _getEffectiveMaxLevel = getEffectiveMaxLevel ?? (def => Mathf.Max(1, def != null ? def.maxLevel : 1));
        _prerequisitesMet = prerequisitesMet ?? (_ => true);
        _requirementsMet = requirementsMet ?? (_ => true);
        _random01 = random01 ?? (() => UnityEngine.Random.value);
    }

    /// <summary>
    /// 抽一次属性候选。**出现任何配置错误都返回空列表**（整组作废）。
    /// </summary>
    public List<UpgradeDefinition> Draw(int count, RewardDrawReport report)
    {
        report ??= new RewardDrawReport();
        report.requested = Mathf.Max(1, count);
        report.circle = 0;
        report.stageLabel = "";
        report.produced = 0;

        var results = new List<UpgradeDefinition>();
        if (_config == null)
        {
            report.Error("属性池未配置（UpgradeChoiceManager.attributePoolConfig 为空）");
            return results;
        }
        if (_config.variants == null || _config.variants.Count == 0)
        {
            report.Error(_config.name + ": 属性池没有任何变体");
            return results;
        }

        // ── ① 逐条校验 ──
        var entries = new List<WeightedUpgrade>();
        var seenIds = new HashSet<string>();
        var seenVariants = new HashSet<string>();
        int invalid = 0;
        for (int i = 0; i < _config.variants.Count; i++)
        {
            var entry = _config.variants[i];
            if (entry == null || entry.upgrade == null)
            {
                report.Error(_config.name + ": 第 " + (i + 1) + " 行是空行");
                invalid++;
                continue;
            }
            var def = entry.upgrade;
            if (!UpgradeRewardClassifier.IsAttribute(def))
            {
                report.Error(def.upgradeId + ": 不是属性（属性池只收资产上填了 statId 的条目）");
                invalid++;
                continue;
            }
            if (def.category == UpgradeCategory.Item || def.category == UpgradeCategory.ActiveSkill)
            {
                report.Error(def.upgradeId + ": category=" + def.category + " 不能进属性池");
                invalid++;
                continue;
            }
            if (entry.weight < 0)
            {
                report.Error(def.upgradeId + ": 权重不能为负（" + entry.weight + "）");
                invalid++;
                continue;
            }
            if (!seenIds.Add(def.upgradeId))
            {
                report.Error(def.upgradeId + ": upgradeId 在属性池里重复（存档 / 快照按它恢复，必须唯一）");
                invalid++;
                continue;
            }
            string variantKey = def.KindId + "|" + def.quality;
            if (!seenVariants.Add(variantKey))
            {
                report.Error(def.upgradeId + ": 同款同品质（" + def.KindId + " / " + def.quality
                    + "）出现多次，无法判断「同一升级内容」的身份");
                invalid++;
                continue;
            }
            entries.Add(entry);
        }
        if (invalid > 0) return results;

        // ── ② 全局品质权重 ──
        var qualityWeights = new List<UpgradeQualityWeight>();
        float totalQualityWeight = 0f;
        if (_config.qualityWeights != null)
        {
            for (int i = 0; i < _config.qualityWeights.Count; i++)
            {
                var qw = _config.qualityWeights[i];
                if (qw == null) continue;
                if (qw.weight < 0f)
                {
                    report.Error(_config.name + ": 品质 " + qw.quality + " 的权重为负（" + qw.weight + "）");
                    continue;
                }
                if (qw.weight <= 0f) continue;
                qualityWeights.Add(qw);
                totalQualityWeight += qw.weight;
            }
        }
        if (report.HasError) return results;
        if (qualityWeights.Count == 0 || totalQualityWeight <= 0f)
        {
            report.Error(_config.name + ": 没有任何正权重的品质（经验升级不会使用装备档位，必须在这里配）");
            return results;
        }

        // ── ③ 当前可用候选（前置 / 天赋 / 等级上限）──
        var eligible = new List<WeightedUpgrade>();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry.weight <= 0) continue;   // 0 权重 = 本次不抽它（合法配置）
            var def = entry.upgrade;
            if (!_prerequisitesMet(def)) continue;
            if (!_requirementsMet(def)) continue;
            int level = _getLevel(def);
            int max = Mathf.Max(1, _getEffectiveMaxLevel(def));
            if (level >= max) continue;
            eligible.Add(entry);
        }

        for (int i = 0; i < qualityWeights.Count; i++)
        {
            var quality = qualityWeights[i].quality;
            int usable = 0;
            for (int j = 0; j < eligible.Count; j++)
                if (eligible[j].upgrade.quality == quality) usable++;
            if (usable == 0)
                report.Error(_config.name + ": 品质 " + quality
                    + " 有权重但当前没有可用候选（未配置该品质变体 / 已满级 / 前置未满足）");
        }
        if (report.HasError) return results;

        var eligibleKinds = new HashSet<string>();
        for (int i = 0; i < eligible.Count; i++) eligibleKinds.Add(eligible[i].upgrade.KindId);
        if (eligibleKinds.Count == 0)
        {
            report.Error(_config.name + ": 当前没有任何可用的属性变体");
            return results;
        }
        if (eligibleKinds.Count < report.requested)
            report.Warn("可用属性种类只有 " + eligibleKinds.Count + " 种，少于请求的 " + report.requested
                + " 张 —— 本次按真实数量出牌（不补卡）");

        // ── ④ 摇品质 → 品质内加权 → 同次按内容去重 ──
        var usedKinds = new HashSet<string>();
        for (int pick = 0; pick < report.requested; pick++)
        {
            var available = new List<WeightedUpgrade>();
            for (int i = 0; i < eligible.Count; i++)
            {
                var entry = eligible[i];
                if (usedKinds.Contains(entry.upgrade.KindId)) continue;
                available.Add(entry);
            }
            if (available.Count == 0) break;

            var quality = RollQuality(qualityWeights, totalQualityWeight);
            var bucket = new List<WeightedUpgrade>();
            for (int i = 0; i < available.Count; i++)
                if (available[i].upgrade.quality == quality) bucket.Add(available[i]);

            if (bucket.Count == 0)
            {
                // 用户 2026-11-25 拍定：同次去重把原本有效的品质抽空 = 配置错误 ⇒ 整组作废。
                report.Error("品质 " + quality
                    + "：抽到它，但该品质的候选都已在本次出现过（同次去重耗尽）");
                results.Clear();
                report.produced = 0;
                return results;
            }

            var picked = WeightedPick(bucket);
            results.Add(picked.upgrade);
            usedKinds.Add(picked.upgrade.KindId);
        }

        report.produced = results.Count;
        if (results.Count == 0)
            report.Error(_config.name + ": 没有任何可用属性变体");
        return results;
    }

    private UpgradeQuality RollQuality(List<UpgradeQualityWeight> weights, float totalWeight)
    {
        float roll = Mathf.Clamp(_random01(), 0f, 0.999999f) * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            cumulative += weights[i].weight;
            if (roll < cumulative) return weights[i].quality;
        }
        return weights[weights.Count - 1].quality;
    }

    private WeightedUpgrade WeightedPick(List<WeightedUpgrade> bucket)
    {
        int total = 0;
        for (int i = 0; i < bucket.Count; i++) total += Mathf.Max(1, bucket[i].weight);
        int roll = Mathf.Clamp((int)(_random01() * total), 0, total - 1);
        int cumulative = 0;
        for (int i = 0; i < bucket.Count; i++)
        {
            cumulative += Mathf.Max(1, bucket[i].weight);
            if (roll < cumulative) return bucket[i];
        }
        return bucket[bucket.Count - 1];
    }
}
