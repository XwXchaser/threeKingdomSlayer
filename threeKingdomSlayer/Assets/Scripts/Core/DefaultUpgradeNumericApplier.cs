/// <summary>
/// **内置的数值型升级效果应用器**（默认实现）。
///
/// 它承载了项目里已有的全部数值型 effectType（伤害倍率 / 攻速 / 移速 / 经验 / 道具 / 射程 /
/// 推波 / 聚拢 / 蓄力减伤 / 反射护盾 / 尖刺陷阱 / 冷却缩减 …）。
///
/// 这些 effectType 是在 <c>UpgradeEffectManager.Awake</c> 里被注册进
/// <c>RegisterNumericApplier</c> 注册表的；同名的 effectType 后注册者覆盖先注册者，
/// 因此**新增数值型升级不必再改这个类**——写自己的应用器注册即可。
/// </summary>
public sealed class DefaultUpgradeNumericApplier : IUpgradeNumericApplier
{
    /// <summary>本类覆盖的 effectType（注册时逐个登记到管理器）。</summary>
    public static readonly string[] EffectTypes =
    {
        "damage_multiplier",
        "attack_speed",
        "move_speed",
        "exp_multiplier",
        "item_drop_rate",
        "item_damage_bonus",
        "stab_range_boost",
        "sweep_range_boost",
        "attack_range_boost",
        "push_wave",
        "convergence_wave",
        "unlock_attack",
        "charge_damage_reduction",
        "charge_reflect_shield",
        "spike_trap",
        "cooldown_reduction",
        // 角色属性（2026-11-19；statId 见 UpgradeStatIds）
        "max_health",
        "attack_power",
        "damage_reduction",
        "guard_break_threshold",
        "crit_rate",
        "crit_damage",
    };

    private readonly IUpgradeStatSink _sink;

    public DefaultUpgradeNumericApplier(IUpgradeStatSink sink)
    {
        _sink = sink;
    }

    /// <inheritdoc/>
    public void Apply(UpgradeDefinition def, int level, NumericLevelConfig cfg)
    {
        switch (def.effectType)
        {
            case "damage_multiplier":
                _sink.AddDamageMultiplier(cfg.floatValue);
                break;
            case "attack_speed":
                _sink.AddAttackSpeedMultiplier(cfg.floatValue);
                break;
            case "move_speed":
                _sink.AddMoveSpeedMultiplier(cfg.floatValue);
                break;
            case "exp_multiplier":
                _sink.AddExpMultiplier(cfg.floatValue);
                break;
            case "item_drop_rate":
                _sink.AddItemDropRateBonus(cfg.floatValue);
                break;
            case "item_damage_bonus":
                _sink.AddItemDamageBonus(cfg.floatValue);
                break;
            case "stab_range_boost":
            case "sweep_range_boost":
                _sink.AddAttackRangeBonus(cfg.intValue);
                _sink.AddAttackDamagePenalty(cfg.secondaryIntValue * 0.01f);
                break;
            case "attack_range_boost":
                _sink.SetAttackRangeBonus(cfg.intValue);
                _sink.SetAttackDamagePenalty(cfg.secondaryIntValue * 0.01f);
                break;
            case "push_wave":
                _sink.AddPushWaveDistance(cfg.intValue);
                break;
            case "convergence_wave":
                _sink.AddConvergenceStep(cfg.intValue);
                if (cfg.floatValue > 0f) _sink.SetConvergenceDamagePercent(cfg.floatValue);
                break;
            case "unlock_attack":
                break;
            case "charge_damage_reduction":
                _sink.AddChargeDamageReduction(cfg.floatValue);
                break;
            case "charge_reflect_shield":
                _sink.ApplyReflectShieldLevel(def, level);
                break;
            case "spike_trap":
                if (SpikeTrapController.Instance != null)
                {
                    if (level == 1)
                        SpikeTrapController.Instance.Initialize(cfg.intValue, cfg.secondaryIntValue, cfg.floatValue);
                    else
                        SpikeTrapController.Instance.SetDamage(cfg.floatValue);
                }
                break;
            case "cooldown_reduction":
                _sink.AddActiveSkillCDReduction(cfg.floatValue);
                break;

            // ── 角色属性（2026-11-19）──
            //   口径：**绝对值走 intValue**（生命上限 / 攻击力 / 破防条宽度），
            //        **百分比走 floatValue**（减伤 / 暴击率 / 暴击伤害）。
            case "max_health":
                _sink.AddMaxHealthBonus(cfg.intValue);
                break;
            case "attack_power":
                _sink.AddAttackPowerBonus(cfg.intValue);
                break;
            case "damage_reduction":
                _sink.AddDamageReduction(cfg.floatValue);
                break;
            case "guard_break_threshold":
                _sink.AddGuardBreakThresholdBonus(cfg.intValue);
                break;
            case "crit_rate":
                _sink.AddCritRate(cfg.floatValue);
                break;
            case "crit_damage":
                _sink.AddCritDamage(cfg.floatValue);
                break;
        }
    }
}
