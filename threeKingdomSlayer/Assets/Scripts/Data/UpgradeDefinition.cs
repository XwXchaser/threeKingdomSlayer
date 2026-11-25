using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
    [Tooltip("各等级首次新增的机制说明。index 0=Lv1；留空表示该等级无新增机制。升级预览中新增文本以金色显示。")]
    [TextArea(1, 3)]
    public List<string> levelFeatureDescriptions = new List<string>();

    [Header("稀有度与等级")]
    public UpgradeRarity rarity = UpgradeRarity.Common;
    [Tooltip("最高等级（1-10）")]
    public int maxLevel = 10;
    [Tooltip("**基础上限（软锁）**：0 = 用 maxLevel。\n" +
             ">0 时本条升级默认只能升到这一级，**只能靠局外天赋解锁**（节点 statId=\"upgrade_level_cap\"）才能抬高。\n" +
             "资产硬上限仍是 maxLevel —— 天赋不能突破它（等级数据只有这么多行）。")]
    public int defaultLevelCap = 0;
    [Tooltip("**品质档**（普通 / 精良 / 传说）。与稀有度是两个独立维度。\n" +
             "同一条升级在同一档内靠等级涨数值；跨档由不同条目或不同卡片实例表达。\n" +
             "部分内容只有单一品质（例如「一次性复活」类饰品只有精良品质）。")]
    public UpgradeQuality quality = UpgradeQuality.Common;

    [Tooltip("**同款身份（kindId）**——2026-11-25：同一件装备的不同品质 / 等级变体共用这个 id。\n" +
             "• **留空 = 用 upgradeId**（现有资产不用改，天然各自一款）；\n" +
             "• 复制出「传说 染病」变体时：新资产给新的 upgradeId，kindId 填原件的 upgradeId；\n" +
             "• 用途：同次三选一去重、同款不能同时装备两份、永久解锁按种类判定；\n" +
             "• **属性**也用它做「同一升级内容」的去重（同次不出普通 / 传说两张生命上限）。\n" +
             "⚠ 主动旋风与被动旋风是不同种类，不要因为它们同名就填同一个 kindId。")]
    public string kindId = "";

    [Tooltip("**玩家可见等级**（2026-11-25）——**只对装备（影之武装 / 饰品）生效**。\n" +
             "装备变体被选中时按这个等级直接生效（不 +1、不继承旧等级、不合成）。\n" +
             "• 默认 1；填 3 = 卡片显示「传说 Lv.3」，实际读取本资产**第 3 行**效果数据；\n" +
             "• 属性（statId 非空）**不使用**这个字段，仍按既有「每选一次升一级」累积。")]
    public int visibleLevel = 1;
    [Tooltip("**初始即解锁**（2026-11-24）——**只对装备池**（影之武装 / 饰品）生效。\n" +
             "• 勾上 = 不写存档也视为已解锁（demo 期现有内容全部勾上）；\n" +
             "• 取消 = 只能靠精英 / Boss / 探索宝箱**永久解锁**（写入 SaveData.unlockedActiveSkills / unlockedAccessories）。\n" +
             "属性 / 秘籍不受影响（它们不走这套解锁）。")]
    public bool unlockedByDefault = true;

    [Header("效果")]
    [Tooltip("效果类型: damage_multiplier | attack_speed | stab_range_boost | sweep_range_boost | push_wave | convergence_wave | spike_trap | charge_damage_reduction | charge_reflect_shield | charge_shockwave | on_attack_trigger | on_kill_chance | unlock_attack | passive_phantom_weapon | passive_return_wave | passive_chain_bounce | passive_timed_aoe | passive_timed_arrow | passive_timed_cyclone | max_health | attack_power | damage_reduction | guard_break_threshold | crit_rate | crit_damage")]
    public string effectType;

    [Header("角色属性（statId）")]
    [Tooltip("**这条升级是不是「玩家可持有的属性」**（2026-11-19）。\n" +
             "• **填了 = 属性** ⇒ 进**属性池**（来源：经验升级）。玩家身上能挂着、会随成长变强的数值。\n" +
             "• **留空 = 机制 / 主动技能** ⇒ 进**装备池**（来源：精英敌人掉落）。\n" +
             "取值见 `UpgradeStatIds`（max_health / attack_power / damage_percent / attack_speed /\n" +
             "crit_rate / crit_damage / damage_reduction / guard_break_threshold / exp_bonus /\n" +
             "cooldown_reduction / item_drop_rate / item_damage_bonus / charge_damage_reduction / attack_range）。\n" +
             "⚠ 分类口径**写在资产上**，不要在代码里按 upgradeId 写名单。")]
    public string statId = "";

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

    [Header("主动海浪（ActiveSkillEffectType=Wave）")]
    [Tooltip("按等级配置主动海浪效果。index 0 = Lv1")]
    public List<ActiveWaveLevelConfig> waveLevels = new List<ActiveWaveLevelConfig>();

    [Header("受击冲击波（effectType=charge_hit_shockwave）")]
    [Tooltip("按等级配置蓄力受击增伤冲击波。index 0 = Lv1")]
    public List<ChargeHitShockwaveLevelConfig> chargeHitShockwaveLevels = new List<ChargeHitShockwaveLevelConfig>();

    [Header("主动疾病（ActiveSkillEffectType=Disease）")]
    [Tooltip("按等级配置主动疾病效果。index 0 = Lv1")]
    public List<DiseaseLevelConfig> diseaseLevels = new List<DiseaseLevelConfig>();

    [Header("抡斧劈砍（ActiveSkillEffectType=AxeSmash）")]
    [Tooltip("按等级配置抡斧。index 0 = Lv1")]
    public List<AxeSmashLevelConfig> axeSmashLevels = new List<AxeSmashLevelConfig>();

    [Header("追命锁链（ActiveSkillEffectType=ChainPull）")]
    [Tooltip("按等级配置锁链拉人。index 0 = Lv1")]
    public List<ChainPullLevelConfig> chainPullLevels = new List<ChainPullLevelConfig>();

    // ═══════════════════════════════════════════════
    // 旧版兼容字段（Inspector 隐藏，保留序列化数据）
    // ═══════════════════════════════════════════════

    [HideInInspector] public int triggerParam;
    [HideInInspector] public List<PhantomStep> phantomSteps = new List<PhantomStep>();

    // ── 辅助方法 ──

    /// <summary>
    /// **同款身份**（2026-11-25）：同一件装备 / 同一条升级内容的不同品质变体共用它。
    /// 留空 <see cref="kindId"/> 时退回 <see cref="upgradeId"/> —— 现有资产不需要改数据。
    /// </summary>
    public string KindId => string.IsNullOrEmpty(kindId) ? upgradeId : kindId;

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

    /// <summary>
    /// 该定义**实际有多少行等级数据**（数据上限）。
    /// 0 = 这个定义没有等级数据（走旧版字段 / 固定值兜底）→ **不夹上限**。
    ///
    /// 为什么要这个：<see cref="GetTriggerInterval"/> / <see cref="GetTriggerThreshold"/> 越界会返回 **-1**
    /// → 触发模块直接判定「不适用」→ **该被动不注册 = 直接失效**。
    /// 夹住数据上限后，这两个函数永远不会走到越界分支。
    /// </summary>
    public virtual int GetAvailableLevelCount()
    {
        switch (effectType)
        {
            case "passive_timed_aoe": return LevelCount(timedAoeLevels);
            case "passive_timed_arrow": return LevelCount(timedArrowLevels);
            case "passive_phantom_weapon": return LevelCount(phantomLevels);
            case "passive_return_wave": return LevelCount(returnWaveLevels);
            case "passive_chain_bounce": return LevelCount(chainBounceLevels);
            case "passive_timed_cyclone": return LevelCount(cycloneLevels);
            case "passive_arrow_volley": return LevelCount(arrowVolleyLevels);
            case "charge_shockwave": return LevelCount(chargeShockwaveLevels);
            case "charge_hit_shockwave": return LevelCount(chargeHitShockwaveLevels);
            case "charge_reflect_shield": return LevelCount(reflectShieldLevels);
            case "active_wave": return LevelCount(waveLevels);
            case "active_disease": return LevelCount(diseaseLevels);
            default: return LevelCount(numericLevels);
        }
    }

    private static int LevelCount<T>(List<T> list) => list != null ? list.Count : 0;

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

    [Header("天赋条件（2026-10-22）")]
    [Tooltip("需要先解锁这些天赋节点（节点 id）才会进入抽取池；空 = 不受天赋限制")]
    public List<string> requiredTalentIds = new List<string>();

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
    ActiveSkill,   // V2 主动技能：永久占槽、可升级、使用后进入独立冷却
    Manual         // 秘籍：局外永久解锁物（掉落拾取即生效，不占局内槽位、不走 ItemInventory）
}

public enum UpgradeRarity
{
    Common,
    Rare,
    Legendary
}

/// <summary>
/// **品质档**（2026-11-16，与稀有度是两个独立维度）。
/// 普通 / 精良 / 传说：同一条升级在同一档内靠等级涨数值；跨档 = 不同的条目或不同的卡片实例。
/// </summary>
public enum UpgradeQuality
{
    Common = 0,    // 普通
    Fine = 1,      // 精良
    Legendary = 2  // 传说
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
    [Tooltip("前方射程排数（扇扫模式使用）")]
    [Min(1)] public int rangeRows;
    [Tooltip("每次伤害")]
    public int damage;
    [Tooltip("影响的列索引列表: 1=col1, 2=col2, 3=col3")]
    public List<int> columns;
    [Header("灼烧")]
    [Tooltip("每次火焰附加的灼烧总伤害")]
    [FormerlySerializedAs("burnDamagePerSecond")]
    public int burnTotalDamage;
    [Tooltip("灼烧持续时间（秒）")]
    public float burnDurationSeconds;

    [Header("冰系")]
    [Tooltip("每次冰伤害叠加的冻结概率（0~1）。火系保持 0")]
    [Range(0f, 1f)] public float iceFreezeChancePerHit;
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
    [Tooltip("落地伤害绝对值（0=未解锁）")]
    public int landingDamage;
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

/// <summary>主动海浪每级配置</summary>
[System.Serializable]
public struct ActiveWaveLevelConfig
{
    [Tooltip("海浪覆盖前方排数")]
    public int rangeRows;
    [Tooltip("每名敌人伤害")]
    public int damage;
    [Tooltip("未眩晕 Boss 受到的最大架势百分比伤害（0.08=8%）")]
    [Range(0f, 1f)] public float bossPoiseDamagePercent;
    [Tooltip("落地伤害绝对值（0=未解锁），仅 Cyclone 使用")]
    public int landingDamage;
    [Tooltip("旋风区域持续时间（秒），仅 Cyclone 使用")]
    [Min(0.01f)] public float cycloneDuration;
}

/// <summary>主动疾病每级配置</summary>
[System.Serializable]
public struct DiseaseLevelConfig
{
    [Tooltip("疾病完整持续时间内的总伤害")]
    public int totalDamage;
    [Tooltip("疾病持续时间（秒）")]
    public int durationSeconds;
    [Tooltip("智能传播（仅1个敌人，优先两侧）")]
    public bool smartSpread;
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

/// <summary>抡斧劈砍配置段（REQ-008 · 参考影之刃零「巨凶斧」：破坏型 / 高破盾）。</summary>
[System.Serializable]
public struct AxeSmashLevelConfig
{
    [Tooltip("每个敌人受到的伤害")]
    public int damage;
    [Tooltip("覆盖前方排数（从第 0 排起）")]
    public int rangeRows;
    [Tooltip("无视盾牌减伤且必定破盾")]
    public bool ignoreShieldBlock;
    [Tooltip("附加破防值（削韧）")]
    public int poiseDamage;
    [Tooltip("特效预制体（留空则用刀光占位）")]
    public GameObject effectPrefab;
    [Tooltip("**抡起来的武器模型**（3D）。留空 = 用玩家这杆长枪占位。\n" +
             "规范见 design/weapon-3d-art-spec-axe-and-chain.md。")]
    public GameObject weaponPrefab;
    [Tooltip("**抡出时的挥空音**（金属风声）。留空 = 回退到 AudioManager 的重武器挥空音。")]
    public AudioClip swingClip;
    [Tooltip("**命中音**（重斧砸实的声音）。留空 = 回退到 AudioManager 的重击命中音。")]
    public AudioClip impactClip;
}

/// <summary>追命锁链配置段（REQ-009 · 参考影之刃零「猎人的追命锁镰」：钩索勾住远处）。</summary>
[System.Serializable]
public struct ChainPullLevelConfig
{
    [Tooltip("命中伤害（伤害不是这个技能的重点，重点是拉人）")]
    public int damage;
    [Tooltip("拉拽时长（秒）")]
    public float pullSeconds;
    [Tooltip("只拉这个距离以内的敌人（世界单位；0 = 不限）")]
    public float maxRange;
    [Tooltip("特效预制体（留空则用箭矢占位）")]
    public GameObject effectPrefab;
    [Tooltip("**锁镰模型**（3D，带铁链）。留空 = 用玩家这杆长枪占位。\n" +
             "规范见 design/weapon-3d-art-spec-axe-and-chain.md。")]
    public GameObject weaponPrefab;
    [Tooltip("**链节贴图**（2D，一个链环）。留空 = 用 Inspector 上的默认链节贴图。")]
    public Sprite chainLinkSprite;

    // ── 链的表现参数（架构 ⑥ · 2026-11-14：原来这些是散在代码里的魔法数字）────────
    [Tooltip("链节数量（= 骨骼数）。")]
    public int chainSegmentCount;
    [Tooltip("链节在世界里的长度（单位）。")]
    public float chainSegmentLength;
    [Tooltip("链环显示缩放（链环固定尺寸，靠间距读出链节）。")]
    public float chainLinkScale;
    [Tooltip("链的弹簧硬度（越大越快贴住目标）。")]
    public float chainStiffness;
    [Tooltip("链的阻尼（越大越快停）。")]
    public float chainDamping;
    [Tooltip("链的重力下垂比例。")]
    public float chainGravityScale;
    [Tooltip("甩出时长（秒）：链头飞向目标的时长。")]
    public float chainThrowSeconds;
    [Tooltip("回收时长（秒）：链头飞回玩家的时长。")]
    public float chainReturnSeconds;
}
