using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;


/// <summary>
/// 升级效果管理器 - 单例
///
/// 管理三选一升级奖励的永久数值累积（damage_multiplier / attack_speed 等）
/// 和行为型效果的 IEffectExecutor 注册表（on_attack_trigger / unlock_attack 等）。
///
/// 数值型效果累积在此处，由 AttackSystem 在计算伤害时查询 GetDamageMultiplier()。
/// 行为型效果通过 RegisterExecutor 注册执行器，ApplyUpgrade 时自动分发。
/// </summary>
public partial class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier, IUpgradeStatSink
{

    /// <summary>
    /// 关卡战斗状态（只读 + 状态事件）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageBattleState _stageBattleState;

    private IStageBattleState StageBattleState => _stageBattleState ??= StageRoles.BattleState;
    public static UpgradeEffectManager Instance { get; private set; }

    // ── 数值累积 ──
    /// <summary>属性 id：攻击力（伤害）。分层乘算模型的唯一入口键。</summary>
    public const string StatAtk = "atk";
    /// <summary>属性 id：生命上限（转化器的合法源属性：只被加法影响，不参与转化）。</summary>
    public const string StatMaxHealth = "max_health";

    /// <summary>
    /// **属性分层乘算模型**（2026-11-16 新增）：伤害的加法层 / 乘法层 / 固定值层 / 转化器都走这里。
    /// 迁移期等价：没有固定值与乘区时 = 旧的 <c>_damageMultiplier + _buffDamageBonus</c>。
    /// </summary>
    private readonly StatLayerModel _statLayers = new StatLayerModel();
    private float _attackSpeedMultiplier = 1f;
    private float _moveSpeedMultiplier = 1f;
    private float _expMultiplier = 1f;
    private float _buffAttackSpeedBonus;
    private float _buffMoveSpeedBonus;
    private float _buffExpBonus;
    private float _itemDropRateBonus; // 道具掉落率加成（小数，0.1=+10%）
    private float _itemDamageBonus;   // 道具伤害加成（小数，0.15=+15%）
    private int _attackRangeBonus;
    private float _attackDamagePenalty;
    private int _pushWaveDistance;
    private int _convergenceStep;
    private float _convergenceDamagePercent = 0.1f;
    private float _chargeDamageReduction;
    private UpgradeDefinition _chargeHitShockwaveDefinition;
    private int _chargeHitShockwaveLevel;
    private int _chargeHitShockwaveHitCount;
    private float _activeSkillCDReduction;

    // ── 角色属性（2026-11-19 新增；statId 见 UpgradeStatIds）──
    private float _maxHealthBonus;          // 生命上限加成（绝对值）
    private float _damageReductionBonus;    // 常驻减伤（小数，0.1 = 减伤 10%）
    private float _guardBreakThresholdBonus;// 破防条加宽（绝对值）
    private float _critRate;                // 暴击率（小数）
    private float _critDamage;              // 暴击伤害倍率加成（小数，0.5 = ×1.5）

    // 反伤盾（由 charge_reflect_shield 管理）
    // 反伤盾的状态已迁至玩家侧状态 ReflectShieldStatus（2026-10-22），本类只保留薄转发。

    // DoT（灼烧 / 疾病）的状态已迁至敌人侧状态 EnemyStatusBurn / EnemyStatusDisease
    // （2026-10-22，批次 C），本类不再持有任何 DoT 状态。

    // ── 已应用升级追踪 (upgradeId → level) ──
    private Dictionary<string, int> _appliedUpgrades = new Dictionary<string, int>();

    // ── 行为执行器注册表 (effectType → executor) ──
    private Dictionary<string, IEffectExecutor> _executors = new Dictionary<string, IEffectExecutor>();

    // ── 数值效果应用器注册表 (effectType → applier) ──
    // 扩展点：新增数值型升级 = 写一个 IUpgradeNumericApplier 并注册，不必改 ApplyUpgrade。
    private Dictionary<string, IUpgradeNumericApplier> _numericAppliers = new Dictionary<string, IUpgradeNumericApplier>();

    // ── 描述格式化器注册表（自定义优先，内置兼底）──
    // 扩展点：新增升级类型的文案 = 写一个 IUpgradeDescriptionFormatter 并注册，不必改格式化链。
    private List<IUpgradeDescriptionFormatter> _descriptionFormatters = new List<IUpgradeDescriptionFormatter>();
    private IUpgradeDescriptionFormatter _defaultDescriptionFormatter;

    // ── 升级事件 ──
    public System.Action<UpgradeDefinition, int> OnUpgradeApplied; // (def, newLevel)
    public System.Action OnUpgradesReset;
    public System.Action OnReflectShieldConsumed; // 反伤盾被消耗时触发（供 ThornArmorEffect）
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 局外天赋可以抬高某条升级的**等级上限**（软锁）。
        // 口径：节点 statId = "upgrade_level_cap"、statAdd = 绝对上限、targetId = 目标 upgradeId。
        // 见 design/weapon-system-design.md §4.2。
        RegisterLevelCapProvider(def => TalentStatService.GetUpgradeLevelCap(def != null ? def.upgradeId : null));
    }

    private void Start()
    {
        RegisterBuffAppliers();
        RegisterDefaultAppliers();
    }

    /// <summary>
    /// 注册**数值型升级效果应用器**（扩展点）。
    /// 同 effectType 后注册者覆盖先注册者；不注册的内置 effectType 走默认实现。
    /// </summary>
    public void RegisterNumericApplier(string effectType, IUpgradeNumericApplier applier)
    {
        if (string.IsNullOrEmpty(effectType) || applier == null) return;
        _numericAppliers[effectType] = applier;
    }

    /// <summary>
    /// 注册**数值型升级效果应用器**（扩展点，委托版）。
    /// 同 effectType 后注册者覆盖先注册者；不注册的内置 effectType 走默认实现。
    /// </summary>
    public void RegisterNumericApplier(string effectType, System.Action<UpgradeDefinition, int, NumericLevelConfig> apply)
    {
        if (string.IsNullOrEmpty(effectType) || apply == null) return;
        RegisterNumericApplier(effectType, new DelegateUpgradeNumericApplier(apply));
    }

    /// <summary>
    /// 注册**升级描述格式化器**（扩展点）。
    /// 自定义格式化器优先于内置兼底生效；多个自定义按注册顺序尝试。
    /// </summary>
    public void RegisterDescriptionFormatter(IUpgradeDescriptionFormatter formatter)
    {
        if (formatter == null) return;
        _descriptionFormatters.Add(formatter);
    }

    /// <summary>
    /// 注册**升级描述格式化器**（扩展点，委托版）。
    /// </summary>
    public void RegisterDescriptionFormatter(
        System.Func<UpgradeDefinition, int, bool> canFormat,
        System.Func<UpgradeDefinition, string, int, string> format)
    {
        RegisterDescriptionFormatter(new DelegateUpgradeDescriptionFormatter(canFormat, format));
    }

    public void RegisterBuffAppliers()
    {
        if (BuffManager.Instance == null) return;
        BuffManager.Instance.RegisterApplier("atk", this);
        BuffManager.Instance.RegisterApplier("attack_speed", this);
        BuffManager.Instance.RegisterApplier("move_speed", this);
        BuffManager.Instance.RegisterApplier("exp", this);
    }

    private void OnDestroy()
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.UnregisterApplier("atk", this);
            BuffManager.Instance.UnregisterApplier("attack_speed", this);
            BuffManager.Instance.UnregisterApplier("move_speed", this);
            BuffManager.Instance.UnregisterApplier("exp", this);
        }
        if (Instance == this)
            Instance = null;
    }

    // 反伤盾的 CD 由玩家侧状态容器统一驱动（PlayerState.Update → StatusHost.Tick）。
    // DoT（灼烧 / 疾病）由敌人侧状态自己结算（批次 C）。




    /// <summary>获取指定升级的当前等级（0=未获得）</summary>
    public int GetUpgradeLevel(string upgradeId)
    {
        return _appliedUpgrades.TryGetValue(upgradeId, out int lv) ? lv : 0;
    }

    /// <summary>当前已获得的被动技能数量（Numeric + AttackPassive + TimedPassive）</summary>
    public int PassiveUpgradeCount => _appliedUpgrades.Count;

    /// <summary>
    /// **占被动槽的升级数**（2026-11-16 修正）。
    ///
    /// ⚠ 之前槽位判定用的是 <see cref="PassiveUpgradeCount"/>（= **所有**升级数）⇒ **数值类升级也占被动槽**
    ///   → 被动槽只有 2 时，玩家拿满 2 个数值升级后，抽池的“被动槽已满”过滤会把
    ///   **所有没拿过的条目全排掉** ⇒ 玩家再也拿不到任何升级。
    ///   （用户 2026-11-16 反馈：“玩家也获取不到数值提升”）
    ///
    /// ⇒ 修正口径（与“属性 / 装备分成两段”的设计一致）：
    ///   · **数值类（Numeric）= 属性，不占槽**，只受自己的等级上限约束；
    ///   · **机制型被动（AttackPassive / TimedPassive）占被动槽**；
    ///   · 主动技能有自己的槽（不计入这里）。
    /// </summary>
    public int PassiveSlotUserCount => _passiveSlotUsers;

    /// <summary>占被动槽的升级计数（只数机制型被动）。</summary>
    private int _passiveSlotUsers;

    // ── 运行时等级上限（有效上限）──

    private readonly List<System.Func<UpgradeDefinition, int>> _levelCapProviders = new List<System.Func<UpgradeDefinition, int>>();

    /// <summary>
    /// 注册「天赋解锁的等级上限」来源（**扩展点**）。
    /// 返回值 = 该升级允许到的最高等级（0 / 负数 = 没有放宽）。
    /// 当前项目还没有「解锁等级上限」的天赋节点 → 没有注册方 → 天赋项恒为 0。
    /// </summary>
    public void RegisterLevelCapProvider(System.Func<UpgradeDefinition, int> provider)
    {
        if (provider == null) return;
        _levelCapProviders.Add(provider);
    }

    /// <summary>
    /// 有效等级上限 = clamp(基础上限 ∪ 天赋上限, 资产硬上限, 等级数据行数)。
    ///
    /// | 项 | 来源 | 作用 |
    /// |---|---|---|
    /// | 资产硬上限 | <c>UpgradeDefinition.maxLevel</c> | 天赋**不能突破**它（数据只有这么多行） |
    /// | 基础上限 | <c>UpgradeDefinition.defaultLevelCap</c>（0 = 用 maxLevel） | **软锁**：不局外解锁就升不上去 |
    /// | 天赋上限 | 局外天赋节点（statId=upgrade_level_cap） | 唯一的抬升途径 |
    /// | 数据上限 | <c>UpgradeDefinition.GetAvailableLevelCount()</c> | 行数不够就夹住（0 = 不夹） |
    /// </summary>
    public int GetEffectiveMaxLevel(UpgradeDefinition def)
    {
        if (def == null) return 1;

        int assetCap = Mathf.Max(1, def.maxLevel);
        int baseCap = def.defaultLevelCap > 0 ? def.defaultLevelCap : assetCap;

        int talentCap = 0;
        for (int i = 0; i < _levelCapProviders.Count; i++)
        {
            int v = _levelCapProviders[i](def);
            if (v > talentCap) talentCap = v;
        }

        // 天赋可以抬高软锁，但不能突破资产硬上限
        int cap = Mathf.Clamp(Mathf.Max(baseCap, talentCap), 1, assetCap);

        int dataRows = def.GetAvailableLevelCount();
        if (dataRows > 0 && dataRows < cap) cap = dataRows;

        return Mathf.Max(1, cap);
    }

    // ── 数值查询接口 ──

    /// <summary>
    /// 伤害倍率（**不含固定值加成**）= (1 + 加法层百分比) × Π(1 + 乘法层乘区)。
    /// 迁移期与旧实现等价（旧实现只有一个加法标量）。
    /// </summary>
    public float GetDamageMultiplier() => Mathf.Max(0f, _statLayers.EvaluateMultiplier(StatAtk));

    /// <summary>
    /// **统一伤害出口**（2026-11-16）：基础伤害 → 固定值加成 → 加法百分比 → 转化器 → 乘法乘区。
    /// 改造前各处写的是 <c>base * GetDamageMultiplier()</c>；现在统一走这里，
    /// 后续「固定伤害加成 / 乘区 / 转化器」只需要在这一个函数里生效。
    /// </summary>
    public float ComputeDamage(float baseDamage)
    {
        return _statLayers.Evaluate(StatAtk, baseDamage, ResolveStatSource);
    }

    /// <summary>固定值伤害加成（+20 攻击这种 flat，不吃百分比）。</summary>
    public void AddFlatDamageBonus(float amount) => _statLayers.AddFlat(StatAtk, amount);

    /// <summary>乘法乘区（0.2 = ×1.2，与加法层独立相乘）。</summary>
    public void AddMultiplicativeDamageBonus(float amount) => _statLayers.AddMultiplicative(StatAtk, amount);

    /// <summary>注册属性转化器（例：每 100 点生命 → +20 攻击）。</summary>
    public void AddStatConverter(StatConverter converter) => _statLayers.AddConverter(converter);

    /// <summary>
    /// 转化器的源属性解析：只暴露**不参与转化**的汇点属性（当前只有生命上限）。
    /// 返回值必须是不含转化器产出的值 ⇒ 结构上不可能成环。
    /// </summary>
    private static float ResolveStatSource(string statId)
    {
        if (string.IsNullOrEmpty(statId)) return 0f;
        if (statId == StatMaxHealth)
        {
            var ps = PlayerState.Instance;
            if (ps != null) return ps.MaxHealth;
        }
        return 0f;
    }
    public float GetAttackSpeedMultiplier() => Mathf.Max(0.01f, _attackSpeedMultiplier + _buffAttackSpeedBonus);
    public float GetMoveSpeedMultiplier() => Mathf.Max(0f, _moveSpeedMultiplier + _buffMoveSpeedBonus);
    public float GetExpMultiplier() => Mathf.Max(0f, _expMultiplier + _buffExpBonus);

    // ── 角色属性的读点（2026-11-19）──

    /// <summary>局内生命上限加成（绝对值）。</summary>
    public float GetMaxHealthBonus() => _maxHealthBonus;

    /// <summary>常驻减伤（小数，0.1 = 减伤 10%）。</summary>
    public float GetDamageReduction() => Mathf.Clamp01(_damageReductionBonus);

    /// <summary>破防条加宽（绝对值）。</summary>
    public float GetGuardBreakThresholdBonus() => _guardBreakThresholdBonus;

    /// <summary>暴击率（小数，0.05 = 5%）。</summary>
    public float GetCritRate() => Mathf.Clamp01(_critRate);

    /// <summary>暴击伤害倍率加成（小数，0.5 = 暴击时伤害 ×1.5）。</summary>
    public float GetCritDamage() => Mathf.Max(0f, _critDamage);

    /// <summary>
    /// **暴击判定**（2026-11-19 新增）。
    ///
    /// 由**伤害施加点**调用（每次命中判一次）。
    ///
    /// **暴击基础倍率 = ×1.5**（暴击的定义）；属性「暴击伤害」在这个基础上**再加**百分比
    /// （满级 +50% ⇒ 最高 ×2.0）。
    /// 暴击率 = 0 时直接返回原伤害，不摇随机数。
    /// </summary>
    /// <param name="damage">已算好倍率层的伤害。</param>
    /// <param name="crit">是否暴击。</param>
    /// <returns>暴击后的伤害。</returns>
    public float ApplyCrit(float damage, out bool crit)
    {
        crit = false;
        if (damage <= 0f) return damage;
        float rate = GetCritRate();
        if (rate <= 0f) return damage;
        if (UnityEngine.Random.value >= rate) return damage;
        crit = true;
        return damage * (CritBaseMultiplier + GetCritDamage());
    }

    /// <summary>暴击的**基础倍率**（×1.5）。属性「暴击伤害」在它之上再加。</summary>
    public const float CritBaseMultiplier = 1.5f;

    public void ApplyModifier(StatModifier modifier)
    {
        ApplyBuffModifier(modifier, 1f);
    }

    public void RemoveModifier(StatModifier modifier)
    {
        ApplyBuffModifier(modifier, -1f);
    }

    private void ApplyBuffModifier(StatModifier modifier, float direction)
    {
        if (modifier == null) return;
        float amount = modifier.value * direction;
        switch (modifier.statId)
        {
            case "atk":
                _statLayers.AddAdditive(StatAtk, amount);
                break;
            case "attack_speed":
                _buffAttackSpeedBonus += amount;
                break;
            case "move_speed":
                _buffMoveSpeedBonus += amount;
                break;
            case "exp":
                _buffExpBonus += amount;
                break;
            default:
                Debug.LogWarning($"[UpgradeEffectManager] 不支持的 Buff statId: {modifier.statId}");
                break;
        }
    }

    public void RegisterChargeHitShockwaveHit()
    {
        if (_chargeHitShockwaveDefinition != null && _chargeHitShockwaveLevel > 0)
            _chargeHitShockwaveHitCount++;
    }

    public float GetChargeHitShockwaveBonusPercent()
    {
        if (!TryGetChargeHitShockwaveConfig(out var cfg)) return 0f;
        return _chargeHitShockwaveHitCount * cfg.damageBonusPerHit;
    }

    public bool ConsumeChargeHitShockwave(out ChargeHitShockwaveLevelConfig config, out float damageBonusPercent)
    {
        if (!TryGetChargeHitShockwaveConfig(out config))
        {
            damageBonusPercent = 0f;
            return false;
        }
        damageBonusPercent = _chargeHitShockwaveHitCount * config.damageBonusPerHit;
        _chargeHitShockwaveHitCount = 0;
        return true;
    }

    private bool TryGetChargeHitShockwaveConfig(out ChargeHitShockwaveLevelConfig config)
    {
        if (_chargeHitShockwaveDefinition != null && _chargeHitShockwaveLevel > 0 &&
            _chargeHitShockwaveDefinition.chargeHitShockwaveLevels != null &&
            _chargeHitShockwaveLevel <= _chargeHitShockwaveDefinition.chargeHitShockwaveLevels.Count)
        {
            config = _chargeHitShockwaveDefinition.chargeHitShockwaveLevels[_chargeHitShockwaveLevel - 1];
            return true;
        }
        config = default;
        return false;
    }

    /// <summary>叠加伤害倍率（供道具型测试等直接调用）</summary>
    public void AddDamageBonus(float amount)
    {
        _statLayers.AddAdditive(StatAtk, amount);
        Debug.Log($"[UpgradeEffectManager] 伤害加成 +{amount:P0} → 当前倍率 {GetDamageMultiplier():P0}");
    }
    /// <summary>攻速累计加成百分比（如 1.45 → 45）</summary>
    public float GetAttackSpeedBonusPercent() => (GetAttackSpeedMultiplier() - 1f) * 100f;
    public float GetItemDropRateBonus() => _itemDropRateBonus;
    public float GetItemDamageBonus() => _itemDamageBonus;
    public int GetAttackRangeBonus() => _attackRangeBonus;
    public float GetAttackDamagePenalty() => _attackDamagePenalty;
    // 保留旧接口兼容（后续清理）
    public int GetStabRangeBonus() => _attackRangeBonus;
    public float GetStabDamagePenalty() => _attackDamagePenalty;
    public int GetSweepRangeBonus() => _attackRangeBonus;
    public float GetSweepDamagePenalty() => _attackDamagePenalty;
    public int GetPushWaveDistance() => _pushWaveDistance;
    public int GetConvergenceStep() => _convergenceStep;
    public float GetConvergenceDamagePercent() => _convergenceDamagePercent;
    public float GetActiveSkillCDReduction() => _activeSkillCDReduction;

    /// <summary>蓄力减伤比例（0~1），仅在玩家处于蓄力状态时由 PlayerState 查询</summary>
    public float GetChargeDamageReduction() => _chargeDamageReduction;

    /// <summary>反伤盾当前剩余值（0=无盾）</summary>
    public int GetReflectShieldAmount() => Shield != null ? Shield.Amount : 0;

    /// <summary>是否持有反伤盾</summary>
    public bool GetHasReflectShield() => Shield != null && Shield.HasShield;

    /// <summary>反伤倍率加成（0~1）</summary>
    public float GetBonusReflectPercent() => Shield != null ? Shield.BonusPercent : 0f;

    /// <summary>反伤盾冷却进度，返回 (fill 0→1, 剩余秒数)。未获得升级返回 (-1, 0)</summary>
    public (float fill, float remaining) GetReflectShieldCooldown()
    {
        return Shield != null ? Shield.GetCooldown() : (-1f, 0f);
    }

    /// <summary>
    /// 玩家的反伤护盾（玩家侧状态，2026-10-22 从本类迁出）。
    /// 护盾是玩家身上的状态，不该寄居在局内 Build 层里。
    /// </summary>
    private ReflectShieldStatus Shield
    {
        get
        {
            var player = PlayerState.Instance;
            if (player == null) return null;
            var status = player.StatusHost.Get(ReflectShieldStatus.Id) as ReflectShieldStatus;
            if (status == null)
            {
                status = new ReflectShieldStatus();
                status.OnConsumed += () => OnReflectShieldConsumed?.Invoke();
                player.StatusHost.Add(status);
            }
            return status;
        }
    }

    /// <summary>重置玩家侧的反伤护盾状态（开新局）。</summary>
    private void ResetReflectShieldState()
    {
        var player = PlayerState.Instance;
        if (player == null) return;
        var status = player.StatusHost.Get(ReflectShieldStatus.Id) as ReflectShieldStatus;
        status?.ResetAll();
    }

    /// <summary>通知蓄力状态变化（由 PlayerState 调用）</summary>
    public void SetCharging(bool charging)
    {
        if (Shield != null) Shield.SetCharging(charging);
    }

    /// <summary>尝试授予护盾。CD 就绪且进入蓄力时调用。返回授予的护盾值，0 表示未就绪</summary>
    public int TryGrantShield()
    {
        return Shield != null ? Shield.TryGrant() : 0;
    }

    /// <summary>吸收伤害并返回反伤值。返回 0 表示无护盾</summary>
    public float AbsorbDamage(float damage)
    {
        if (Shield == null) return 0f;
        return Shield.Absorb(damage);
    }

    /// <summary>清空护盾（离开蓄力时调用）</summary>
    public void ClearShield()
    {
        if (Shield != null) Shield.Clear();
    }

    #region Debug Setters

    public void DebugSetPushWave(int distance) => _pushWaveDistance = distance;
    public void DebugSetConvergenceWave(int step, float damagePercent)
    {
        _convergenceStep = step;
        if (damagePercent > 0f) _convergenceDamagePercent = damagePercent;
    }
    public void DebugSetDirectionalPush(int step) { /* 已移除：Slash 左右位移改为招式资产配置驱动（slashDirectionalPushRows） */ }

    #endregion


    /// <summary>重置所有升级效果（新对局开始时调用）</summary>
    public void ResetAll()
    {
        // 清理行为型效果
        foreach (var kv in _appliedUpgrades)
        {
            foreach (var exec in _executors.Values)
                exec.Remove(kv.Key);
        }

        _appliedUpgrades.Clear();
        _passiveSlotUsers = 0;
        _statLayers.Clear();
        _attackSpeedMultiplier = 1f;
        _moveSpeedMultiplier = 1f;
        _expMultiplier = 1f;
        _buffAttackSpeedBonus = 0f;
        _buffMoveSpeedBonus = 0f;
        _buffExpBonus = 0f;
        _itemDropRateBonus = 0f;
        _itemDamageBonus = 0f;
        _attackRangeBonus = 0;
        _attackDamagePenalty = 0f;
        _pushWaveDistance = 0;
        _convergenceStep = 0;
        _convergenceDamagePercent = 0.1f;
        _chargeDamageReduction = 0f;
        _chargeHitShockwaveDefinition = null;
        _chargeHitShockwaveLevel = 0;
        _chargeHitShockwaveHitCount = 0;
        _activeSkillCDReduction = 0f;
        _maxHealthBonus = 0f;
        _damageReductionBonus = 0f;
        _guardBreakThresholdBonus = 0f;
        _critRate = 0f;
        _critDamage = 0f;

        // 反伤盾：状态已迁至玩家侧，重置走玩家状态容器
        ResetReflectShieldState();
        Shield?.SetCharging(false);

        // DoT（灼烧 / 疾病）状态在敌人侧，回池时随敌人一起清掉，这里不需要处理。

        CycloneItemController.Instance?.ResetAll();
        SpikeTrapController.Instance?.ResetAll();

        // 清空被动攻击模块
        PassiveTriggerModule.Instance?.ResetAll();
        TimedPassiveModule.Instance?.ResetAll();

        // 清空 V1 道具与 V2 主动技能运行态
        ItemInventory.Instance?.ClearAll();
        ActiveSkillInventory.Instance?.ResetAll();
        OnUpgradesReset?.Invoke();
    }
}
