using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级奖励定义 - ScriptableObject
///
/// 架构原则：效果为主，触发为辅。
/// - 效果（effectType + 每级效果参数）是 SO 的身份核心，始终在 Inspector 中可见。
/// - 触发方式由 category（AttackPassive / TimedPassive）决定，Inspector 按 category 显示对应触发字段。
/// - 所有字段始终序列化，切换 category 不会丢失数据。
/// </summary>
[CreateAssetMenu(fileName = "UpgradeDefinition", menuName = "一夫当关/升级奖励定义")]
public class UpgradeDefinition : ScriptableObject
{
    [Header("标识")]
    [Tooltip("奖励类型：数值buff | 道具 | 被动攻击")]
    public UpgradeCategory category;
    [Tooltip("唯一标识符")]
    public string upgradeId;

    [Header("显示")]
    [Tooltip("奖励名称，如「神力」")]
    public string displayName;
    [Tooltip("效果描述模板，{0}=数值，如「造成伤害提升 {0}%」")]
    public string descriptionTemplate;
    [Tooltip("补充效果描述；支持与主描述相同的 {0}、{1} 占位符。留空则不显示。")]
    [TextArea(1, 3)]
    public string extraDescriptionTemplate;

    [Header("稀有度与等级")]
    public UpgradeRarity rarity = UpgradeRarity.Common;
    [Tooltip("最高等级（1-10）")]
    public int maxLevel = 10;

    [Header("效果")]
    [Tooltip("效果类型: damage_multiplier | attack_speed | stab_range_boost | sweep_range_boost | push_wave | convergence_wave | spike_trap | charge_damage_reduction | charge_reflect_shield | charge_shockwave | on_attack_trigger | on_kill_chance | unlock_attack | passive_phantom_weapon | passive_return_wave | passive_chain_bounce | passive_timed_aoe | passive_timed_arrow | passive_timed_cyclone")]
    public string effectType;

    [Header("数值型 — 每级效果配置（effectType 匹配时使用，index 0 = Lv1）")]
    [Tooltip("按等级配置数值效果。index 0 = Lv1。每级数值独立填写，无需一致。")]
    public List<NumericLevelConfig> numericLevels = new List<NumericLevelConfig>();

    [Header("旧版兼容（迁移前保留，numericLevels 为空时回退使用）")]
    [Tooltip("每级浮点加成 — 仅数值型使用。已弃用，请使用 numericLevels")]
    public float floatValue;
    [Tooltip("每级整数加成 — 仅数值型使用。已弃用，请使用 numericLevels")]
    public int intValue;
    [Tooltip("每级第二整数加成 — 仅数值型使用。已弃用，请使用 numericLevels")]
    public int secondaryIntValue;
    [Tooltip("字符串参数（如on_kill_chance的掉落类型）")]
    public string stringValue;

    [Header("攻击解锁（仅 unlock_attack）")]
    [Tooltip("基础攻击技能配置骨架")]
    public AttackSkillConfig baseAttackConfig;

    [Header("UI 图标")]
    [Tooltip("所有类型的升级均需配置图标，供 BuffDisplayPanel 显示")]
    public Sprite icon;

    // ═══════════════════════════════════════════════
    // 被动攻击型 — 效果每级配置（始终可见，不随触发选项卡切换）
    // ═══════════════════════════════════════════════

    [Header("幻影武器（effectType=passive_phantom_weapon）")]
    [Tooltip("按等级配置幻影效果。index 0 = Lv1")]
    public List<PhantomLevelConfig> phantomLevels = new List<PhantomLevelConfig>();

    [Header("喷火（effectType=passive_timed_aoe）")]
    [Tooltip("按等级配置喷火效果。index 0 = Lv1")]
    public List<TimedAoeLevelConfig> timedAoeLevels = new List<TimedAoeLevelConfig>();

    [Header("箭雨（effectType=passive_timed_arrow）")]
    [Tooltip("按等级配置箭雨效果。index 0 = Lv1")]
    public List<TimedArrowLevelConfig> timedArrowLevels = new List<TimedArrowLevelConfig>();

    [Header("折返波（effectType=passive_return_wave）")]
    [Tooltip("按等级配置折返波效果。index 0 = Lv1")]
    public List<ReturnWaveLevelConfig> returnWaveLevels = new List<ReturnWaveLevelConfig>();

    [Header("连锁弹射（effectType=passive_chain_bounce）")]
    [Tooltip("按等级配置连锁弹射效果。index 0 = Lv1")]
    public List<ChainBounceLevelConfig> chainBounceLevels = new List<ChainBounceLevelConfig>();

    [Header("旋风（effectType=passive_timed_cyclone）")]
    [Tooltip("按等级配置旋风效果。index 0 = Lv1")]
    public List<CycloneLevelConfig> cycloneLevels = new List<CycloneLevelConfig>();

    [Header("箭矢齐射（effectType=passive_arrow_volley）")]
    [Tooltip("按等级配置箭矢齐射效果。index 0 = Lv1")]
    public List<ArrowVolleyLevelConfig> arrowVolleyLevels = new List<ArrowVolleyLevelConfig>();

    [Header("反伤盾（effectType=charge_reflect_shield）")]
    [Tooltip("按等级配置反伤盾效果。index 0 = Lv1")]
    public List<ReflectShieldLevelConfig> reflectShieldLevels = new List<ReflectShieldLevelConfig>();

    [Header("冲击波（effectType=charge_shockwave）")]
    [Tooltip("按等级配置冲击波效果。index 0 = Lv1")]
    public List<ChargeShockwaveLevelConfig> chargeShockwaveLevels = new List<ChargeShockwaveLevelConfig>();

    [Header("蓄力攻击冲击波（effectType=charge_attack_shockwave）")]
    [Tooltip("按等级配置蓄力攻击冲击波效果。index 0 = Lv1")]
    public List<ChargeAttackShockwaveLevelConfig> chargeAttackShockwaveLevels = new List<ChargeAttackShockwaveLevelConfig>();

    [Header("受击冲击波（effectType=charge_hit_shockwave）")]
    [Tooltip("按等级配置蓄力受击增伤冲击波。index 0 = Lv1")]
    public List<ChargeHitShockwaveLevelConfig> chargeHitShockwaveLevels = new List<ChargeHitShockwaveLevelConfig>();

    // ═══════════════════════════════════════════════
    // 旧版兼容字段（Inspector 隐藏，保留序列化数据）
    // ═══════════════════════════════════════════════

    [HideInInspector] public int triggerParam;
    [HideInInspector] public List<PhantomStep> phantomSteps = new List<PhantomStep>();

    // ── 辅助方法 ──

    /// <summary>获取指定等级的数值配置。优先 numericLevels，回退到旧版字段 × level。</summary>
    public NumericLevelConfig GetNumericConfig(int level)
    {
        if (numericLevels != null && numericLevels.Count >= level)
            return numericLevels[level - 1];
        // 回退：旧版字段 × level
        return new NumericLevelConfig
        {
            floatValue = this.floatValue * level,
            intValue = this.intValue * level,
            secondaryIntValue = this.secondaryIntValue * level
        };
    }

    // ═══════════════════════════════════════════════
    // 道具型
    // ═══════════════════════════════════════════════

    [Header("道具型（category=Item 时生效）")]
    [Tooltip("获得后可使用的次数，-1=无限次")]
    public int useCount = 1;
    [Tooltip("道具动作标识，用于库存和执行分发")]
    public string gestureId;

    [Header("持续旋风道具（effectType=item_cyclone）")]
    public CycloneItemConfig cycloneItemConfig;

    [Header("前置条件")]
    [Tooltip("需要其他选项达到指定等级后才会进入抽取池")]
    public List<UpgradePrerequisite> prerequisites;

    // ── 辅助方法 ──

    /// <summary>获取指定等级的幻影配置。优先使用 phantomLevels，回退到旧字段。</summary>
    public void GetPhantomConfig(int level, out int triggerParam, out List<PhantomStep> steps)
    {
        if (phantomLevels != null && phantomLevels.Count >= level)
        {
            var cfg = phantomLevels[level - 1];
            triggerParam = cfg.triggerParam;
            steps = cfg.phantomSteps;
        }
        else
        {
            triggerParam = this.triggerParam;
            steps = this.phantomSteps;
        }
    }

    /// <summary>获取指定等级的效果配置（效果自包含，不依赖攻击上下文）</summary>
    public void GetPhantomEffectConfig(int level, out AttackType attackType, out int targetColumn, out List<PhantomStep> steps)
    {
        if (phantomLevels != null && phantomLevels.Count >= level)
        {
            var cfg = phantomLevels[level - 1];
            attackType = cfg.attackType;
            targetColumn = cfg.targetColumn;
            steps = cfg.phantomSteps;
        }
        else
        {
            attackType = AttackType.Pierce;
            targetColumn = 2;
            steps = this.phantomSteps;
        }
    }

    /// <summary>获取指定等级的定时触发间隔（秒），-1=不适用</summary>
    public float GetTriggerInterval(int level)
    {
        switch (effectType)
        {
            case "passive_timed_aoe":
                if (timedAoeLevels != null && level <= timedAoeLevels.Count)
                    return timedAoeLevels[level - 1].intervalSeconds;
                break;
            case "passive_timed_arrow":
                if (timedArrowLevels != null && level <= timedArrowLevels.Count)
                    return timedArrowLevels[level - 1].intervalSeconds;
                break;
            case "passive_phantom_weapon":
                if (phantomLevels != null && level <= phantomLevels.Count)
                    return phantomLevels[level - 1].intervalSeconds;
                break;
            case "passive_return_wave":
                if (returnWaveLevels != null && level <= returnWaveLevels.Count)
                    return returnWaveLevels[level - 1].intervalSeconds;
                break;
            case "passive_chain_bounce":
                if (chainBounceLevels != null && level <= chainBounceLevels.Count)
                    return chainBounceLevels[level - 1].intervalSeconds;
                break;
            case "passive_timed_cyclone":
                if (cycloneLevels != null && level <= cycloneLevels.Count)
                    return cycloneLevels[level - 1].intervalSeconds;
                break;
            case "charge_shockwave":
                if (chargeShockwaveLevels != null && level <= chargeShockwaveLevels.Count)
                    return chargeShockwaveLevels[level - 1].intervalSeconds;
                break;
        }
        return -1f;
    }

    /// <summary>获取指定等级的攻击计数阈值，-1=不适用</summary>
    public int GetTriggerThreshold(int level)
    {
        switch (effectType)
        {
            case "passive_phantom_weapon":
                if (phantomLevels != null && level <= phantomLevels.Count)
                    return phantomLevels[level - 1].triggerParam;
                break;
            case "passive_return_wave":
                if (returnWaveLevels != null && level <= returnWaveLevels.Count)
                    return returnWaveLevels[level - 1].triggerThreshold;
                break;
            case "passive_chain_bounce":
                if (chainBounceLevels != null && level <= chainBounceLevels.Count)
                    return chainBounceLevels[level - 1].triggerThreshold;
                break;
            case "passive_timed_aoe":
                if (timedAoeLevels != null && level <= timedAoeLevels.Count)
                    return timedAoeLevels[level - 1].triggerThreshold;
                break;
            case "passive_timed_arrow":
                if (timedArrowLevels != null && level <= timedArrowLevels.Count)
                    return timedArrowLevels[level - 1].triggerThreshold;
                break;
            case "passive_arrow_volley":
                if (arrowVolleyLevels != null && level <= arrowVolleyLevels.Count)
                    return arrowVolleyLevels[level - 1].triggerThreshold;
                break;
        }
        return -1;
    }
}

// ═══════════════════════════════════════════════
// 枚举定义
// ═══════════════════════════════════════════════

/// <summary>升级奖励类型</summary>
public enum UpgradeCategory
{
    Numeric,       // 数值buff型：伤害/攻速/移速/经验倍率等永久加成
    Item,          // V1 道具型：手势触发的一次性/限次道具
    AttackPassive, // 攻击计数被动：每 N 次攻击触发
    TimedPassive,  // 定时被动：每 N 秒触发
    ActiveSkill    // V2 主动技能：永久占槽、可升级、使用后进入独立冷却
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Legendary
}

// ═══════════════════════════════════════════════
// 每级效果配置 struct（触发参数 + 效果参数各自独立）
// ═══════════════════════════════════════════════

/// <summary>幻影武器每级配置</summary>
[System.Serializable]
public struct PhantomLevelConfig
{
    [Header("触发参数")]
    [Tooltip("定时触发间隔（秒）— triggerMode=Timed 时有效")]
    public float intervalSeconds;
    [Tooltip("攻击计数阈值（每X次）— triggerMode=AttackCount 时有效")]
    public int triggerParam;

    [Header("效果参数")]
    [Tooltip("幻影攻击类型")]
    public AttackType attackType;
    [Tooltip("目标列（1=col1, 2=col2, 3=col3）")]
    public int targetColumn;
    [Tooltip("该等级的幻影攻击段数列表")]
    public List<PhantomStep> phantomSteps;
}

/// <summary>喷火每级配置</summary>
[System.Serializable]
public struct TimedAoeLevelConfig
{
    [Header("触发参数")]
    [Tooltip("定时触发间隔（秒）— triggerMode=Timed 时有效")]
    public float intervalSeconds;
    [Tooltip("攻击计数阈值（每X次）— triggerMode=AttackCount 时有效")]
    public int triggerThreshold;

    [Header("效果参数")]
    [Tooltip("每次伤害")]
    public int damage;
    [Tooltip("影响的列索引列表: 1=col1, 2=col2, 3=col3")]
    public List<int> columns;
    [Header("灼烧")]
    [Tooltip("灼烧每秒伤害，0=不启用灼烧")]
    public int burnDamagePerSecond;
    [Tooltip("灼烧持续时间（秒）")]
    public float burnDurationSeconds;
}

/// <summary>箭雨每级配置</summary>
[System.Serializable]
public struct TimedArrowLevelConfig
{
    [Header("触发参数")]
    [Tooltip("定时触发间隔（秒）— triggerMode=Timed 时有效")]
    public float intervalSeconds;
    [Tooltip("攻击计数阈值（每X次）— triggerMode=AttackCount 时有效")]
    public int triggerThreshold;

    [Header("效果参数")]
    [Tooltip("正前方排数")]
    public int rowCount;
    [Tooltip("每个敌人被射箭矢数")]
    public int arrowCount;
    [Tooltip("每箭伤害")]
    public int damage;
}

/// <summary>折返波每级配置</summary>
[System.Serializable]
public struct ReturnWaveLevelConfig
{
    [Header("触发参数")]
    [Tooltip("定时触发间隔（秒）— triggerMode=Timed 时有效")]
    public float intervalSeconds;
    [Tooltip("攻击计数阈值（每X次）— triggerMode=AttackCount 时有效")]
    public int triggerThreshold;

    [Header("效果参数")]
    [Tooltip("目标列（1=col1, 2=col2, 3=col3）")]
    public int column;
    [Tooltip("波覆盖排数")]
    public int rangeRows;
    [Tooltip("折返伤害比例（0.5=50%）")]
    public float damageRatio;
}

/// <summary>连锁弹射每级配置</summary>
[System.Serializable]
public struct ChainBounceLevelConfig
{
    [Header("触发参数")]
    [Tooltip("定时触发间隔（秒）— triggerMode=Timed 时有效")]
    public float intervalSeconds;
    [Tooltip("攻击计数阈值（每X次）— triggerMode=AttackCount 时有效")]
    public int triggerThreshold;

    [Header("效果参数")]
    [Tooltip("起始列（1=col1, 2=col2, 3=col3）")]
    public int column;
    [Tooltip("最大弹射次数")]
    public int maxBounces;
    [Tooltip("每次弹射伤害保留比例（0.8=80%）")]
    public float damageRatio;
}

/// <summary>旋风每级配置</summary>
[System.Serializable]
public struct CycloneLevelConfig
{
    [Header("触发参数")]
    [Tooltip("定时触发间隔（秒）")]
    public float intervalSeconds;

    [Header("效果参数")]
    [Tooltip("随机选取敌人数")]
    public int enemyCount;
    [Tooltip("击飞持续秒数")]
    public float knockupDuration;
    [Tooltip("击飞伤害")]
    public int damage;
    [Tooltip("落地伤害百分比（0=未解锁, 0.5=50%）")]
    public float landingDamagePercent;
}

/// <summary>持续旋风道具配置</summary>
[System.Serializable]
public struct CycloneItemConfig
{
    [Tooltip("道具生效的总持续时间（秒）")]
    [Min(0.01f)] public float durationSeconds;
    [Tooltip("重新查询前排敌人的间隔（秒）")]
    [Min(0.01f)] public float intervalSeconds;
    [Tooltip("每次使用后的冷却时间（秒）")]
    [Min(0f)] public float cooldownSeconds;
    [Tooltip("影响玩家前方排数")]
    [Min(1)] public int rowCount;

    [Header("局外成长扩展（当前默认无伤害）")]
    [Tooltip("旋风生成时的伤害")]
    [Min(0)] public int initialDamage;
    [Tooltip("落地伤害相对初始伤害的比例（0.5=50%）")]
    [Min(0f)] public float landingDamagePercent;
}

/// <summary>数值型每级配置</summary>
[System.Serializable]
public struct NumericLevelConfig
{
    [Tooltip("浮点值（伤害倍率增量、攻速增量、百分比等）")]
    public float floatValue;
    [Tooltip("整数值（排数、列数、格数等）")]
    public int intValue;
    [Tooltip("第二整数值（伤害惩罚、col等）")]
    public int secondaryIntValue;
}

/// <summary>箭矢齐射每级配置</summary>
[System.Serializable]
public struct ArrowVolleyLevelConfig
{
    [Header("触发参数")]
    [Tooltip("攻击计数阈值（每X次）")]
    public int triggerThreshold;

    [Header("效果参数")]
    [Tooltip("瞄准最近的敌人数")]
    public int targetCount;
    [Tooltip("每敌发射箭矢数")]
    public int arrowCount;
    [Tooltip("每箭基础伤害（走数值管线）")]
    public int baseDamage;
}

/// <summary>反伤盾每级配置</summary>
[System.Serializable]
public struct ReflectShieldLevelConfig
{
    [Header("触发参数")]
    [Tooltip("获得护盾的间隔时间（秒）。CD 冷却完毕后，进入蓄力时获得护盾")]
    public float intervalSeconds;

    [Header("效果参数")]
    [Tooltip("护盾值（反伤总量）")]
    public int shieldAmount;

    [Header("额外效果")]
    [Tooltip("勾选后启用额外反伤倍率加成")]
    public bool enableBonus;
    [Tooltip("反伤伤害加成百分比（10=10%），实际反伤 = 吸收伤害 × (1 + bonus/100)")]
    public float bonusReflectPercent;
}

/// <summary>冲击波每级配置</summary>
[System.Serializable]
public struct ChargeShockwaveLevelConfig
{
    [Header("触发参数")]
    [Tooltip("攒波间隔时间（秒）")]
    public float intervalSeconds;

    [Header("效果参数")]
    [Tooltip("每次攒的波数")]
    public int shockwaveCount;
    [Tooltip("冲击波覆盖排数")]
    public int rangeRows;
    [Tooltip("每段基础伤害")]
    public int baseDamage;
    [Tooltip("每层伤害加成（0.15=15%）")]
    public float stackDamageBonus;
    [Tooltip("每道冲击波之间的延迟（秒），防止同时打出")]
    public float waveDelay;
}

/// <summary>主动蓄力冲击波每级配置</summary>
[System.Serializable]
public struct ChargeAttackShockwaveLevelConfig
{
    [Tooltip("冲击波覆盖排数")]
    public int rangeRows;
    [Tooltip("每道冲击波伤害")]
    public int damage;
}

/// <summary>蓄力受击增伤冲击波每级配置</summary>
[System.Serializable]
public struct ChargeHitShockwaveLevelConfig
{
    [Tooltip("蓄力攻击附带的冲击波数量")]
    public int shockwaveCount;
    [Tooltip("每道冲击波基础伤害")]
    public int baseDamage;
    [Tooltip("冲击波覆盖排数")]
    public int rangeRows;
    [Tooltip("蓄力期间每次实际掉血增加的伤害比例（0.15=15%）")]
    public float damageBonusPerHit;
}

// ═══════════════════════════════════════════════
// 其他辅助类型
// ═══════════════════════════════════════════════

[System.Serializable]
public class UpgradePrerequisite
{
    [Tooltip("需要的前置升级")]
    public UpgradeDefinition requiredUpgrade;
    [Tooltip("前置升级最低等级")]
    public int requiredLevel = 1;
}

/// <summary>幻影攻击配置段 — 伤害比例、透明度、延迟</summary>
[System.Serializable]
public struct PhantomStep
{
    [Tooltip("伤害比例（0.3=30%）")]
    public float damageRatio;
    [Tooltip("透明度（0.6=60%）")]
    public float alpha;
    [Tooltip("该段幻影延迟（秒），0=无延迟")]
    public float delaySeconds;
}
