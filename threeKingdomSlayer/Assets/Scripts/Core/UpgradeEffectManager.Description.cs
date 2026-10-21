using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
    /// UpgradeEffectManager 的**升级描述文本**：占位符填充与逐级文案。
    ///
    /// 按机制从 <c>UpgradeEffectManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 UpgradeEffectManager.cs **同类型**，行为完全一致。
    /// 扩展点：新的升级类型注册自己的描述格式化器，不要往格式化链里加分支。
    /// </summary>

public partial class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier
{

    /// <summary>
    /// 根据描述模板和当前等级生成效果文本
    /// </summary>
    public string GetDescription(UpgradeDefinition def)
    {
        int level = GetDisplayLevel(def);
        return AppendFeatureDescriptions(GetDescriptionForLevel(def, level + 1), def, level + 1, false, level);
    }

    public string GetUpgradePreviewDescription(UpgradeDefinition def)
    {
        int currentLevel = GetDisplayLevel(def);
        int nextLevel = currentLevel + 1;
        string nextDescription = GetDescriptionForLevel(def, nextLevel);
        if (currentLevel <= 0)
            return AppendFeatureDescriptions(nextDescription, def, nextLevel, false, 0);

        string currentDescription = GetDescriptionForLevel(def, currentLevel);
        string comparison = BuildDescriptionComparison(currentDescription, nextDescription);
        return AppendFeatureDescriptions(comparison, def, nextLevel, true, currentLevel);
    }

    private int GetDisplayLevel(UpgradeDefinition def)
    {
        return def.category == UpgradeCategory.ActiveSkill && ActiveSkillInventory.Instance != null
            ? ActiveSkillInventory.Instance.GetLevel(def.upgradeId)
            : (_appliedUpgrades.TryGetValue(def.upgradeId, out int level) ? level : 0);
    }

    private static string AppendFeatureDescriptions(string description, UpgradeDefinition def, int level, bool highlightNew, int currentLevel)
    {
        if (def.levelFeatureDescriptions == null) return description;

        int upperBound = Mathf.Min(level, def.levelFeatureDescriptions.Count);
        for (int i = 0; i < upperBound; i++)
        {
            string feature = def.levelFeatureDescriptions[i];
            if (string.IsNullOrEmpty(feature)) continue;

            bool isNewFeature = highlightNew && i >= currentLevel;
            description += "\n" + (isNewFeature ? "<color=#F5C542>" + feature + "</color>" : feature);
        }
        return description;
    }

    private static string BuildDescriptionComparison(string currentDescription, string nextDescription)
    {
        var currentNumbers = Regex.Matches(currentDescription, @"\d+(?:\.\d+)?");
        var nextNumbers = Regex.Matches(nextDescription, @"\d+(?:\.\d+)?");
        if (currentNumbers.Count == 0 || currentNumbers.Count != nextNumbers.Count)
            return nextDescription;

        int numberIndex = 0;
        return Regex.Replace(nextDescription, @"\d+(?:\.\d+)?", match =>
        {
            string currentValue = currentNumbers[numberIndex].Value;
            string nextValue = match.Value;
            numberIndex++;
            return currentValue != nextValue
                ? currentValue + " → <color=#F5C542>" + nextValue + "</color>"
                : nextValue;
        });
    }

    private string GetDescriptionForLevel(UpgradeDefinition def, int nextLevel)
    {
        string desc = string.IsNullOrEmpty(def.extraDescriptionTemplate)
            ? def.descriptionTemplate
            : def.descriptionTemplate + "\n" + def.extraDescriptionTemplate;
        return FormatDescriptionForLevel(def, desc, nextLevel);
    }

    private string FormatDescriptionForLevel(UpgradeDefinition def, string desc, int nextLevel)
    {
        if (def.category == UpgradeCategory.ActiveSkill && def is ActiveSkillDefinition activeSkill)
        {
            if (activeSkill.activeEffectType == ActiveSkillEffectType.FireAoe && activeSkill.timedAoeLevels != null && nextLevel <= activeSkill.timedAoeLevels.Count)
            {
                var cfg = activeSkill.timedAoeLevels[nextLevel - 1];
                desc = desc.Replace("{0}", activeSkill.GetCooldown(nextLevel).ToString("F1"));
                desc = desc.Replace("{1}", cfg.damage.ToString());
                desc = desc.Replace("{2}", cfg.columns != null ? cfg.columns.Count.ToString() : "0");
            }
            else if (activeSkill.activeEffectType == ActiveSkillEffectType.FireLine && activeSkill.timedAoeLevels != null && nextLevel <= activeSkill.timedAoeLevels.Count)
            {
                var cfg = activeSkill.timedAoeLevels[nextLevel - 1];
                desc = desc.Replace("{0}", activeSkill.GetCooldown(nextLevel).ToString("F1"));
                desc = desc.Replace("{1}", cfg.damage.ToString());
                desc = desc.Replace("{2}", cfg.columns != null ? cfg.columns.Count.ToString() : "0");
            }
            else if (activeSkill.activeEffectType == ActiveSkillEffectType.ArrowRain && activeSkill.timedArrowLevels != null && nextLevel <= activeSkill.timedArrowLevels.Count)
            {
                var cfg = activeSkill.timedArrowLevels[nextLevel - 1];
                desc = desc.Replace("{0}", activeSkill.GetCooldown(nextLevel).ToString("F1"));
                desc = desc.Replace("{1}", cfg.rowCount.ToString());
                desc = desc.Replace("{2}", cfg.arrowCount.ToString());
                desc = desc.Replace("{3}", cfg.damage.ToString());
            }
            else if (activeSkill.activeEffectType == ActiveSkillEffectType.Cyclone && activeSkill.waveLevels != null && nextLevel <= activeSkill.waveLevels.Count)
            {
                var cfg = activeSkill.waveLevels[nextLevel - 1];
                desc = desc.Replace("{0}", cfg.rangeRows.ToString());
                desc = desc.Replace("{1}", activeSkill.GetCooldown(nextLevel).ToString("F1"));
                desc = desc.Replace("{2}", (cfg.bossPoiseDamagePercent * 100f).ToString("0"));
                desc = desc.Replace("{3}", cfg.landingDamage.ToString());
            }
            else if (activeSkill.activeEffectType == ActiveSkillEffectType.ChargeAttackShockwave && activeSkill.chargeAttackShockwaveLevels != null && nextLevel <= activeSkill.chargeAttackShockwaveLevels.Count)
            {
                var cfg = activeSkill.chargeAttackShockwaveLevels[nextLevel - 1];
                desc = desc.Replace("{0}", cfg.rangeRows.ToString());
                desc = desc.Replace("{1}", cfg.damage.ToString());
                desc = desc.Replace("{2}", activeSkill.GetCooldown(nextLevel).ToString("F1"));
            }
            else if (activeSkill.activeEffectType == ActiveSkillEffectType.Disease && activeSkill.diseaseLevels != null && nextLevel <= activeSkill.diseaseLevels.Count)
            {
                var cfg = activeSkill.diseaseLevels[nextLevel - 1];
                desc = desc.Replace("{0}", cfg.totalDamage.ToString());
            }
            else if (activeSkill.activeEffectType == ActiveSkillEffectType.Wave && activeSkill.waveLevels != null && nextLevel <= activeSkill.waveLevels.Count)
            {
                var cfg = activeSkill.waveLevels[nextLevel - 1];
                desc = desc.Replace("{0}", cfg.rangeRows.ToString());
                desc = desc.Replace("{1}", activeSkill.GetCooldown(nextLevel).ToString("F1"));
                desc = desc.Replace("{2}", (cfg.bossPoiseDamagePercent * 100f).ToString("0"));
            }
        }
        else if (def.category == UpgradeCategory.AttackPassive || def.category == UpgradeCategory.TimedPassive)
        {
            bool isTimed = def.category == UpgradeCategory.TimedPassive;
            // 被动：从每级配置读取
            if (def.effectType == "passive_timed_aoe")
            {
                if (def.timedAoeLevels != null && nextLevel <= def.timedAoeLevels.Count)
                {
                    var cfg = def.timedAoeLevels[nextLevel - 1];
                    desc = desc.Replace("{1}", cfg.damage.ToString());
                    desc = desc.Replace("{2}", cfg.burnTotalDamage.ToString());
                    desc = desc.Replace("{3}", cfg.burnDurationSeconds.ToString("F1"));
                    desc = desc.Replace("{4}", cfg.rangeRows.ToString());
                }
            }
            else if (def.effectType == "passive_timed_arrow")
            {
                if (def.timedArrowLevels != null && nextLevel <= def.timedArrowLevels.Count)
                {
                    var cfg = def.timedArrowLevels[nextLevel - 1];
                    string triggerStr = isTimed
                        ? cfg.intervalSeconds.ToString("F1")
                        : cfg.triggerThreshold.ToString();
                    desc = desc.Replace("{0}", triggerStr);
                    desc = desc.Replace("{1}", cfg.rowCount.ToString());
                    desc = desc.Replace("{2}", cfg.arrowCount.ToString());
                    desc = desc.Replace("{3}", Mathf.Max(1, cfg.damage / 4).ToString());
                }
            }
            else if (def.effectType == "passive_timed_cyclone")
            {
                if (def.cycloneLevels != null && nextLevel <= def.cycloneLevels.Count)
                {
                    var cfg = def.cycloneLevels[nextLevel - 1];
                    desc = desc.Replace("{0}", cfg.intervalSeconds.ToString("F1"));
                    desc = desc.Replace("{1}", cfg.enemyCount.ToString());
                    desc = desc.Replace("{2}", cfg.knockupDuration.ToString("F1"));
                }
            }
            else if (def.effectType == "passive_return_wave")
            {
                var cfg = (def.returnWaveLevels != null && nextLevel <= def.returnWaveLevels.Count)
                    ? def.returnWaveLevels[nextLevel - 1]
                    : new ReturnWaveLevelConfig { triggerThreshold = def.intValue, damageRatio = def.floatValue };
                string triggerStr = isTimed
                    ? cfg.intervalSeconds.ToString("F1")
                    : cfg.triggerThreshold.ToString();
                desc = desc.Replace("{0}", triggerStr);
                desc = desc.Replace("{1}", (cfg.damageRatio * 100f).ToString("F0"));
            }
            else if (def.effectType == "passive_arrow_volley")
            {
                var cfg = (def.arrowVolleyLevels != null && nextLevel <= def.arrowVolleyLevels.Count)
                    ? def.arrowVolleyLevels[nextLevel - 1]
                    : new ArrowVolleyLevelConfig { triggerThreshold = def.intValue, targetCount = def.secondaryIntValue, arrowCount = 3 };
                desc = desc.Replace("{0}", cfg.triggerThreshold.ToString());
                desc = desc.Replace("{1}", cfg.targetCount.ToString());
                desc = desc.Replace("{2}", cfg.arrowCount.ToString());
            }
            else if (def.effectType == "passive_chain_bounce")
            {
                var cfg = (def.chainBounceLevels != null && nextLevel <= def.chainBounceLevels.Count)
                    ? def.chainBounceLevels[nextLevel - 1]
                    : new ChainBounceLevelConfig { triggerThreshold = def.intValue, maxBounces = def.secondaryIntValue, damageRatio = def.floatValue };
                string triggerStr = isTimed
                    ? cfg.intervalSeconds.ToString("F1")
                    : cfg.triggerThreshold.ToString();
                desc = desc.Replace("{0}", triggerStr);
                desc = desc.Replace("{1}", cfg.maxBounces.ToString());
                desc = desc.Replace("{2}", (cfg.damageRatio * 100f).ToString("F0"));
            }
            else if (def.effectType == "charge_shockwave")
            {
                var swCfg = def.chargeShockwaveLevels != null && nextLevel <= def.chargeShockwaveLevels.Count
                    ? def.chargeShockwaveLevels[nextLevel - 1]
                    : new ChargeShockwaveLevelConfig();
                desc = desc.Replace("{1}", swCfg.shockwaveCount.ToString());
                desc = desc.Replace("{2}", swCfg.rangeRows.ToString());
                desc = desc.Replace("{3}", swCfg.baseDamage.ToString());
                desc = desc.Replace("{4}", (swCfg.stackDamageBonus * 100f).ToString("F0"));
            }
            else if (def.effectType == "charge_hit_shockwave")
            {
                var cfg = def.chargeHitShockwaveLevels != null && nextLevel <= def.chargeHitShockwaveLevels.Count
                    ? def.chargeHitShockwaveLevels[nextLevel - 1]
                    : new ChargeHitShockwaveLevelConfig();
                desc = desc.Replace("{0}", cfg.shockwaveCount.ToString());
                desc = desc.Replace("{1}", cfg.baseDamage.ToString());
                desc = desc.Replace("{2}", cfg.rangeRows.ToString());
                desc = desc.Replace("{3}", (cfg.damageBonusPerHit * 100f).ToString("F0"));
            }
            else
            {
                def.GetPhantomConfig(nextLevel, out int triggerParam, out var steps);
                float interval = def.phantomLevels != null && nextLevel <= def.phantomLevels.Count
                    ? def.phantomLevels[nextLevel - 1].intervalSeconds : -1f;
                string triggerStr = isTimed && interval > 0f
                    ? interval.ToString("F1")
                    : triggerParam.ToString();
                desc = desc.Replace("{0}", triggerStr);
                desc = desc.Replace("{1}", (steps?.Count ?? 0).ToString());
                if (steps != null && steps.Count > 0)
                    desc = desc.Replace("{2}", (steps[0].damageRatio * 100f).ToString("F0"));
            }
        }
        else if (def.effectType == "item_cyclone")
        {
            var cfg = def.cycloneItemConfig;
            desc = desc.Replace("{0}", cfg.durationSeconds.ToString("F1"));
            desc = desc.Replace("{1}", cfg.intervalSeconds.ToString("F1"));
            desc = desc.Replace("{2}", cfg.rowCount.ToString());
        }
        else if (def.effectType == "stab_range_boost" || def.effectType == "sweep_range_boost" || def.effectType == "attack_range_boost")
        {
            var cfg = GetDisplayNumericConfig(def, nextLevel);
            desc = desc.Replace("{0}", cfg.intValue.ToString());
            desc = desc.Replace("{1}", cfg.secondaryIntValue.ToString());
        }
        else if (def.effectType == "push_wave" || def.effectType == "convergence_wave")
        {
            var cfg = GetDisplayNumericConfig(def, nextLevel);
            desc = desc.Replace("{0}", cfg.intValue.ToString());
            desc = desc.Replace("{1}", (cfg.floatValue * 100f).ToString("F0"));
        }
        else if (def.effectType == "charge_damage_reduction")
        {
            var cfg = GetDisplayNumericConfig(def, nextLevel);
            desc = desc.Replace("{0}", (cfg.floatValue * 100f).ToString("F0"));
        }
        else if (def.effectType == "spike_trap")
        {
            var cfg = def.GetNumericConfig(nextLevel);
            desc = desc.Replace("{0}", cfg.floatValue.ToString("F0"));
            desc = desc.Replace("{1}", cfg.intValue.ToString());
            desc = desc.Replace("{2}", cfg.secondaryIntValue.ToString());
        }
        else if (def.effectType == "charge_reflect_shield")
        {
            var rsCfg = def.reflectShieldLevels != null && nextLevel <= def.reflectShieldLevels.Count
                ? def.reflectShieldLevels[nextLevel - 1]
                : new ReflectShieldLevelConfig();
            desc = desc.Replace("{0}", rsCfg.intervalSeconds.ToString("F1"));
            desc = desc.Replace("{1}", rsCfg.shieldAmount.ToString());
            if (rsCfg.enableBonus)
                desc = desc.Replace("{2}", $"，反伤伤害+{rsCfg.bonusReflectPercent.ToString("F0")}%");
            else
                desc = desc.Replace("{2}", "");
        }
        else if (def.effectType == "heal")
        {
            var cfg = def.GetNumericConfig(nextLevel);
            desc = desc.Replace("{0}", cfg.floatValue.ToString("F0"));
        }
        else
        {
            var cfg = def.GetNumericConfig(nextLevel);
            desc = desc.Replace("{0}", (cfg.floatValue * 100f).ToString("F0"));
            desc = desc.Replace("{1}", cfg.intValue.ToString());
        }

        return desc;
    }
}