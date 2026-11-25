using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **装备三选一抽取器**（2026-11-25）——影之武装与饰品**统一一套候选**。
///
/// 与旧的「主动 / 饰品交替保底」彻底不同：
///   ① 锁定本次奖励所处的**内部圈数阶段**（由 <see cref="IEndlessCircleSource"/> 注入，Combat 不引用 Flow）；
///   ② 按该阶段的**档位权重**抽一个池内档；
///   ③ 从该档的合规变体里按**档内权重**抽一个；
///   ④ 同一次候选按**种类身份**（<see cref="UpgradeDefinition.KindId"/>）去重 —— 不出现同款的不同品质 / 等级。
///
/// 不保底：允许三张全是影之武装、或全是饰品；不按类型交替，也不因为槽位满 / 同款已装备而过滤。
/// 没有库存：抽到 / 已装备都不减少池内数量，后续仍能再出现同款。
///
/// ⚠ 抽到「已有同款」不是过滤条件：它可以用来替换原槽（规则见 <see cref="EquipmentLoadout"/>）。
/// ⚠ 抽到「该类满槽且没有同款」也不过滤：由玩家在候选下方点旧装备替换。
/// </summary>
public sealed class EquipmentChoiceDrawer
{
    private readonly EquipmentPoolConfig _config;
    private readonly Func<UpgradeDefinition, bool> _isUnlocked;
    private readonly Func<float> _random01;

    public EquipmentChoiceDrawer(
        EquipmentPoolConfig config,
        Func<UpgradeDefinition, bool> isUnlocked,
        Func<float> random01 = null)
    {
        _config = config;
        _isUnlocked = isUnlocked;
        _random01 = random01 ?? (() => UnityEngine.Random.value);
    }

    /// <summary>
    /// 抽一次装备候选。**出现任何配置错误都返回空列表**（整组作废，不发放部分奖励）。
    /// </summary>
    public List<UpgradeDefinition> Draw(int circle, int count, RewardDrawReport report)
    {
        report ??= new RewardDrawReport();
        report.requested = Mathf.Max(1, count);
        report.circle = circle;
        report.produced = 0;

        var results = new List<UpgradeDefinition>();
        if (_config == null)
        {
            report.Error("装备池未配置（UpgradeChoiceManager.equipmentPoolConfig 为空）");
            return results;
        }
        if (_config.variants == null || _config.variants.Count == 0)
        {
            report.Error(_config.name + ": 装备池没有任何变体");
            return results;
        }

        // ── ① 逐条校验（不静默跳过坏条目：坏一条就整池不可抽）──
        var entries = new List<EquipmentVariantEntry>();
        var seenIds = new HashSet<string>();
        var seenVariants = new HashSet<string>();
        int invalid = 0;
        for (int i = 0; i < _config.variants.Count; i++)
        {
            var entry = _config.variants[i];
            if (entry == null)
            {
                report.Error(_config.name + ": 第 " + (i + 1) + " 行是空行");
                invalid++;
                continue;
            }
            string problem = EquipmentVariantRules.Validate(entry.variant);
            if (problem != null)
            {
                report.Error(problem);
                invalid++;
                continue;
            }
            if (entry.weight < 0)
            {
                report.Error(entry.variant.upgradeId + ": 档内权重不能为负（" + entry.weight + "）");
                invalid++;
                continue;
            }
            if (entry.tier < 1)
            {
                report.Error(entry.variant.upgradeId + ": 池内档位必须 >= 1（" + entry.tier + "）");
                invalid++;
                continue;
            }
            if (!seenIds.Add(entry.variant.upgradeId))
            {
                report.Error(entry.variant.upgradeId + ": upgradeId 在装备池里重复（存档 / 快照按它恢复，必须唯一）");
                invalid++;
                continue;
            }
            string variantKey = entry.variant.KindId + "|" + entry.variant.quality + "|" + entry.variant.visibleLevel;
            if (!seenVariants.Add(variantKey))
            {
                report.Error(entry.variant.upgradeId + ": 同款同品质同等级（" + entry.variant.KindId + " / "
                    + entry.variant.quality + " / Lv." + entry.variant.visibleLevel + "）出现多次，卡片身份无法区分");
                invalid++;
                continue;
            }
            entries.Add(entry);
        }
        if (invalid > 0) return results;

        // ── ② 锁定内部圈数阶段 ──
        if (_config.stages != null)
        {
            for (int i = 0; i < _config.stages.Count; i++)
            {
                var s = _config.stages[i];
                if (s == null) continue;
                if (s.minCircle < 1)
                    report.Error(_config.name + ": 第 " + (i + 1) + " 个阶段的 minCircle 必须 >= 1（" + s.minCircle + "）");
                if (s.maxCircle > 0 && s.maxCircle < s.minCircle)
                    report.Error(_config.name + ": 阶段「" + (string.IsNullOrEmpty(s.label) ? (i + 1).ToString() : s.label)
                        + "」的 maxCircle（" + s.maxCircle + "）小于 minCircle（" + s.minCircle + "）");
            }
        }
        if (report.HasError) return results;

        var stage = _config.FindStage(circle);
        if (stage == null)
        {
            report.Error(_config.name + ": 内部圈数 " + circle + " 没有命中任何阶段（请检查 stages 的 min/max）");
            return results;
        }
        report.stageLabel = string.IsNullOrEmpty(stage.label)
            ? stage.minCircle + "-" + (stage.maxCircle > 0 ? stage.maxCircle.ToString() : "∞")
            : stage.label;

        var tierWeights = new List<EquipmentTierWeight>();
        float totalTierWeight = 0f;
        if (stage.tierWeights != null)
        {
            for (int i = 0; i < stage.tierWeights.Count; i++)
            {
                var tw = stage.tierWeights[i];
                if (tw == null) continue;
                if (tw.weight < 0f)
                {
                    report.Error("阶段「" + report.stageLabel + "」：档位 " + tw.tier + " 的权重为负（" + tw.weight + "）");
                    continue;
                }
                if (tw.weight <= 0f) continue;
                tierWeights.Add(tw);
                totalTierWeight += tw.weight;
            }
        }
        if (report.HasError) return results;
        if (tierWeights.Count == 0 || totalTierWeight <= 0f)
        {
            report.Error("阶段「" + report.stageLabel + "」：没有任何正权重的档位");
            return results;
        }

        // ── ③ 每个正权重档位都必须有可用变体（解锁过滤后）──
        for (int i = 0; i < tierWeights.Count; i++)
        {
            int tier = tierWeights[i].tier;
            int usable = 0;
            for (int j = 0; j < entries.Count; j++)
            {
                if (entries[j].tier != tier || entries[j].weight <= 0) continue;
                if (!IsUnlocked(entries[j].variant)) continue;
                usable++;
            }
            if (usable == 0)
                report.Error("阶段「" + report.stageLabel + "」：档位 " + tier
                    + " 有权重但没有可用变体（未配置 / 权重为 0 / 未解锁）");
        }
        if (report.HasError) return results;

        int distinctKinds = CountDistinctKinds(entries);
        if (distinctKinds < report.requested)
            report.Warn("可用种类只有 " + distinctKinds + " 种，少于请求的 " + report.requested
                + " 张 —— 本次按真实数量出牌（不补卡）");

        // ── ④ 抽档 → 档内加权 → 同次按种类去重 ──
        var usedKinds = new HashSet<string>();
        for (int pick = 0; pick < report.requested; pick++)
        {
            var available = new List<EquipmentVariantEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.weight <= 0) continue;
                if (usedKinds.Contains(e.variant.KindId)) continue;
                if (!IsUnlocked(e.variant)) continue;
                available.Add(e);
            }
            if (available.Count == 0) break;

            int tier = RollTier(tierWeights, totalTierWeight);
            var bucket = new List<EquipmentVariantEntry>();
            for (int i = 0; i < available.Count; i++)
                if (available[i].tier == tier) bucket.Add(available[i]);

            if (bucket.Count == 0)
            {
                // 用户 2026-11-25 拍定：同次去重把原本有效的档抽空 = 配置错误 ⇒ 整组作废。
                report.Error("阶段「" + report.stageLabel + "」：抽到档位 " + tier
                    + "，但该档的变体都已在本次出现过（同次去重耗尽）");
                results.Clear();
                report.produced = 0;
                return results;
            }

            var picked = WeightedPick(bucket);
            results.Add(picked.variant);
            usedKinds.Add(picked.variant.KindId);
        }

        report.produced = results.Count;
        if (results.Count == 0)
            report.Error("没有任何可用装备变体（配置与解锁检查通过，但候选为 0）");
        return results;
    }

    private bool IsUnlocked(UpgradeDefinition def)
    {
        return _isUnlocked == null || _isUnlocked(def);
    }

    private static int CountDistinctKinds(List<EquipmentVariantEntry> entries)
    {
        var kinds = new HashSet<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].weight <= 0) continue;
            kinds.Add(entries[i].variant.KindId);
        }
        return kinds.Count;
    }

    private int RollTier(List<EquipmentTierWeight> tierWeights, float totalWeight)
    {
        float roll = Mathf.Clamp(_random01(), 0f, 0.999999f) * totalWeight;
        float cumulative = 0f;
        for (int i = 0; i < tierWeights.Count; i++)
        {
            cumulative += tierWeights[i].weight;
            if (roll < cumulative) return tierWeights[i].tier;
        }
        return tierWeights[tierWeights.Count - 1].tier;
    }

    private EquipmentVariantEntry WeightedPick(List<EquipmentVariantEntry> bucket)
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
