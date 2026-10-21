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

    private IStageBattleState StageBattleState => _stageBattleState ??= StageController.Instance;
    public static UpgradeEffectManager Instance { get; private set; }

    // ── 数值累积 ──
    private float _damageMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;
    private float _moveSpeedMultiplier = 1f;
    private float _expMultiplier = 1f;
    private float _buffDamageBonus;
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
    private int _directionalPushStep;
    private float _chargeDamageReduction;
    private UpgradeDefinition _chargeHitShockwaveDefinition;
    private int _chargeHitShockwaveLevel;
    private int _chargeHitShockwaveHitCount;
    private float _activeSkillCDReduction;

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

    // ── 数值查询接口 ──

    public float GetDamageMultiplier() => Mathf.Max(0f, _damageMultiplier + _buffDamageBonus);
    public float GetAttackSpeedMultiplier() => Mathf.Max(0.01f, _attackSpeedMultiplier + _buffAttackSpeedBonus);
    public float GetMoveSpeedMultiplier() => Mathf.Max(0f, _moveSpeedMultiplier + _buffMoveSpeedBonus);
    public float GetExpMultiplier() => Mathf.Max(0f, _expMultiplier + _buffExpBonus);

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
                _buffDamageBonus += amount;
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
        _damageMultiplier += amount;
        Debug.Log($"[UpgradeEffectManager] 伤害加成 +{amount:P0} → 当前倍率 {_damageMultiplier:P0}");
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
    public int GetDirectionalPushStep() => _directionalPushStep;
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
    public void DebugSetDirectionalPush(int step) => _directionalPushStep = step;

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
        _damageMultiplier = 1f;
        _attackSpeedMultiplier = 1f;
        _moveSpeedMultiplier = 1f;
        _expMultiplier = 1f;
        _buffDamageBonus = 0f;
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
        _directionalPushStep = 0;
        _chargeDamageReduction = 0f;
        _chargeHitShockwaveDefinition = null;
        _chargeHitShockwaveLevel = 0;
        _chargeHitShockwaveHitCount = 0;
        _activeSkillCDReduction = 0f;

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
