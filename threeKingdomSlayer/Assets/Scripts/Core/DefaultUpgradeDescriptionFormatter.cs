using UnityEngine;
    
    /// <summary>
    /// **内置的升级描述格式化器**（默认兜底）。
    ///
    /// 承载项目里已有的全部升级类型的占位符填法（主动技能 / 被动 / 数值型 …），
    /// 文案与改造前**逐字一致**（原为 UpgradeEffectManager 里 200 多行的 if-else 链）。
    ///
    /// 它是「兜底」：自定义的 <see cref="IUpgradeDescriptionFormatter"/> 优先生效；
    /// 走不到自定义时由本类处理，保证既有文案不变。
    /// </summary>
    public sealed class DefaultUpgradeDescriptionFormatter : IUpgradeDescriptionFormatter
    {
    /// <summary>默认实现处理所有类型（永远兜底）。</summary>
    public bool CanFormat(UpgradeDefinition def, int nextLevel) => true;

    /// <summary>累加到指定等级的数值配置（用于文案里的累计值）。</summary>
    private static NumericLevelConfig GetCumulativeNumericConfig(UpgradeDefinition def, int level)
    {
        var total = new NumericLevelConfig();
        for (int i = 1; i <= level; i++)
        {
            var config = def.GetNumericConfig(i);
            total.floatValue += config.floatValue;
            total.intValue += config.intValue;
            total.secondaryIntValue += config.secondaryIntValue;
        }
        return total;
    }

    /// <summary>文案用的数值配置（射程类是覆盖语义，其余是累计语义）。</summary>
    private static NumericLevelConfig GetDisplayNumericConfig(UpgradeDefinition def, int level)
    {
        return def.effectType == "attack_range_boost"
            ? def.GetNumericConfig(level)
            : GetCumulativeNumericConfig(def, level);
    }

    
        /// <inheritdoc/>
        public string Format(UpgradeDefinition def, string desc, int nextLevel)
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
    