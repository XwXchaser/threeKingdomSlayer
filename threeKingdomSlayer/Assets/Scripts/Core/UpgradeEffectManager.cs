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
public partial class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier
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

    // DoT系统：伤害配置均表示完整持续时间内的总伤害。
    private const float BurnTickInterval = 1f;
    private const float DiseaseTickInterval = 0.5f;

    private readonly Dictionary<Enemy, DotState> _burnStates = new Dictionary<Enemy, DotState>();
    private readonly Dictionary<Enemy, DiseaseState> _diseaseStates = new Dictionary<Enemy, DiseaseState>();
    private readonly List<Enemy> _dotRemovalList = new List<Enemy>();
    private readonly List<KeyValuePair<Enemy, DotState>> _burnUpdateBuffer = new List<KeyValuePair<Enemy, DotState>>();
    private readonly List<KeyValuePair<Enemy, DiseaseState>> _diseaseUpdateBuffer = new List<KeyValuePair<Enemy, DiseaseState>>();
    private readonly Dictionary<Enemy, DiseaseBubbleEffect> _diseaseVisuals = new Dictionary<Enemy, DiseaseBubbleEffect>();

    private void StopDiseaseVisual(Enemy enemy)
    {
        if (enemy == null || !_diseaseVisuals.TryGetValue(enemy, out var visual))
            return;
        _diseaseVisuals.Remove(enemy);
        if (visual != null)
            visual.StopEmission();
    }

    private void EnsureDiseaseVisual(Enemy enemy)
    {
        if (enemy == null)
            return;
        if (_diseaseVisuals.TryGetValue(enemy, out var existing) && existing != null)
        {
            existing.StartEmission();
            return;
        }

        var visualObject = new GameObject("DiseaseBubbleEffect");
        visualObject.transform.SetParent(enemy.transform, false);
        var visual = visualObject.AddComponent<DiseaseBubbleEffect>();
        visual.StartEmission();
        _diseaseVisuals[enemy] = visual;
    }


    // 反伤盾（由 charge_reflect_shield 管理）
    private int _reflectShieldAmount;        // 当前护盾剩余值，0 = 无护盾
    private int _reflectShieldMaxAmount;     // 护盾最大值
    private float _reflectShieldCooldown;    // CD 计时器
    private float _reflectShieldInterval;    // CD 间隔
    private float _bonusReflectPercent;      // 反伤倍率加成（0~1）
    private bool _reflectShieldReady;        // CD 冷却完毕，等待进入蓄力
    private bool _isCharging;

    // ── 已应用升级追踪 (upgradeId → level) ──
    private Dictionary<string, int> _appliedUpgrades = new Dictionary<string, int>();

    // ── 行为执行器注册表 (effectType → executor) ──
    private Dictionary<string, IEffectExecutor> _executors = new Dictionary<string, IEffectExecutor>();

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

    private void Update()
    {
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress) return;
        // 反伤盾 CD 计时器：蓄力期间暂停，非蓄力且未就绪时跑
        TickBurnDamage();

        if (_reflectShieldInterval <= 0f) return;
        if (_reflectShieldReady) return;
        if (_isCharging) return;
        _reflectShieldCooldown -= Time.deltaTime;
        if (_reflectShieldCooldown <= 0f)
        {
            _reflectShieldReady = true;
            _reflectShieldCooldown = 0f;
        }
    }




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
    public int GetReflectShieldAmount() => _reflectShieldAmount;

    /// <summary>是否持有反伤盾</summary>
    public bool GetHasReflectShield() => _reflectShieldAmount > 0;

    /// <summary>反伤倍率加成（0~1）</summary>
    public float GetBonusReflectPercent() => _bonusReflectPercent;

    /// <summary>反伤盾冷却进度，返回 (fill 0→1, 剩余秒数)。未获得升级返回 (-1, 0)</summary>
    public (float fill, float remaining) GetReflectShieldCooldown()
    {
        if (_reflectShieldInterval <= 0f) return (-1f, 0f);
        if (_reflectShieldReady) return (1f, 0f); // CD 就绪
        float remaining = Mathf.Max(_reflectShieldCooldown, 0f);
        float fill = 1f - (remaining / _reflectShieldInterval);
        return (fill, remaining);
    }

    /// <summary>通知蓄力状态变化（由 PlayerState 调用）</summary>
    public void SetCharging(bool charging)
    {
        _isCharging = charging;
    }

    /// <summary>尝试授予护盾。CD 就绪且进入蓄力时调用。返回授予的护盾值，0 表示未就绪</summary>
    public int TryGrantShield()
    {
        if (!_reflectShieldReady || _reflectShieldMaxAmount <= 0) return 0;
        _reflectShieldReady = false;
        _reflectShieldAmount = _reflectShieldMaxAmount;
        _reflectShieldCooldown = _reflectShieldInterval;
        return _reflectShieldAmount;
    }

    /// <summary>吸收伤害并返回反伤值。返回 0 表示无护盾</summary>
    public float AbsorbDamage(float damage)
    {
        if (_reflectShieldAmount <= 0) return 0f;
        int absorbed = Mathf.Min(_reflectShieldAmount, Mathf.CeilToInt(damage));
        _reflectShieldAmount -= absorbed;
        float reflect = damage * (1f + _bonusReflectPercent);
        if (_reflectShieldAmount <= 0)
        {
            _reflectShieldAmount = 0;
            OnReflectShieldConsumed?.Invoke();
        }
        return reflect;
    }

    /// <summary>清空护盾（离开蓄力时调用）</summary>
    public void ClearShield()
    {
        if (_reflectShieldAmount > 0)
        {
            _reflectShieldAmount = 0;
            OnReflectShieldConsumed?.Invoke();
        }
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

    private static NumericLevelConfig GetDisplayNumericConfig(UpgradeDefinition def, int level)
    {
        return def.effectType == "attack_range_boost"
            ? def.GetNumericConfig(level)
            : GetCumulativeNumericConfig(def, level);
    }


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

        _reflectShieldAmount = 0;
        _reflectShieldMaxAmount = 0;
        _reflectShieldCooldown = 0f;
        _reflectShieldInterval = 0f;
        _bonusReflectPercent = 0f;
        _reflectShieldReady = false;
        _isCharging = false;

        _burnStates.Clear();
        _dotRemovalList.Clear();
        _burnUpdateBuffer.Clear();
        _diseaseUpdateBuffer.Clear();
        foreach (var enemy in _diseaseStates.Keys)
        {
            if (enemy != null)
                enemy.OnDying -= HandleDiseasedEnemyDeath;
        }
        _diseaseStates.Clear();
        _diseaseVisuals.Clear();

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
