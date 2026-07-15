using System.Collections.Generic;
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
public class UpgradeEffectManager : MonoBehaviour
{
    public static UpgradeEffectManager Instance { get; private set; }

    // ── 数值累积 ──
    private float _damageMultiplier = 1f;
    private float _attackSpeedMultiplier = 1f;
    private float _moveSpeedMultiplier = 1f;
    private float _expMultiplier = 1f;
    private int _stabRangeBonus;
    private float _stabDamagePenalty;
    private int _sweepRangeBonus;
    private float _sweepDamagePenalty;
    private int _pushWaveDistance;
    private int _convergenceStep;
    private float _convergenceDamagePercent = 0.1f;
    private int _directionalPushStep;
    private float _chargeDamageReduction;
    private UpgradeDefinition _chargeHitShockwaveDefinition;
    private int _chargeHitShockwaveLevel;
    private int _chargeHitShockwaveHitCount;

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        // 反伤盾 CD 计时器：蓄力期间暂停，非蓄力且未就绪时跑
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

    /// <summary>注册行为型效果执行器</summary>
    public void RegisterExecutor(string effectType, IEffectExecutor executor)
    {
        _executors[effectType] = executor;
    }

    /// <summary>注销行为型效果执行器</summary>
    public void UnregisterExecutor(string effectType)
    {
        _executors.Remove(effectType);
    }

    /// <summary>
    /// 应用升级奖励 — 由 UpgradeChoiceManager 在玩家选择后调用
    /// </summary>
    public void ApplyUpgrade(UpgradeDefinition def)
    {
        // 道具型：路由到 ItemInventory
        if (!string.IsNullOrEmpty(def.gestureId))
        {
            if (ItemInventory.Instance == null)
            {
                Debug.LogWarning("[UpgradeEffectManager] ItemInventory 未找到，道具无法添加");
                return;
            }
            if (!ItemInventory.Instance.AddItem(def)) return;

            SyncToPlayerState(def, 0); // level=0 标记为道具型（PlayerState 中不追踪等级）
            OnUpgradeApplied?.Invoke(def, 0);
            return;
        }

        // 被动攻击型：根据 category 路由到对应的触发模块
        if (def.category == UpgradeCategory.AttackPassive || def.category == UpgradeCategory.TimedPassive)
        {
            int currentLevel = _appliedUpgrades.TryGetValue(def.upgradeId, out int lv) ? lv : 0;
            int newLevel = currentLevel + 1;
            if (newLevel > def.maxLevel)
            {
                Debug.LogWarning($"[UpgradeEffectManager] {def.upgradeId} 已达最大等级 {def.maxLevel}，跳过");
                return;
            }
            _appliedUpgrades[def.upgradeId] = newLevel;
            SyncToPlayerState(def, newLevel);

            if (def.category == UpgradeCategory.TimedPassive)
            {
                if (TimedPassiveModule.Instance != null)
                    TimedPassiveModule.Instance.Register(def, newLevel);
                else
                    Debug.LogWarning("[UpgradeEffectManager] TimedPassiveModule 未找到");
            }
            else if (def.effectType == "charge_hit_shockwave")
            {
                _chargeHitShockwaveDefinition = def;
                _chargeHitShockwaveLevel = newLevel;
            }
            else
            {
                if (PassiveTriggerModule.Instance != null)
                    PassiveTriggerModule.Instance.Register(def, newLevel);
                else
                    Debug.LogWarning("[UpgradeEffectManager] PassiveTriggerModule 未找到");
            }

            OnUpgradeApplied?.Invoke(def, newLevel);
            Debug.Log($"[UpgradeEffectManager] 应用被动 {def.displayName} Lv.{newLevel} category={def.category} effectType={def.effectType}");
            return;
        }

        int currentNumericLevel = _appliedUpgrades.TryGetValue(def.upgradeId, out int ln) ? ln : 0;
        int newNumericLevel = currentNumericLevel + 1;

        if (newNumericLevel > def.maxLevel)
        {
            Debug.LogWarning($"[UpgradeEffectManager] {def.upgradeId} 已达最大等级 {def.maxLevel}，跳过");
            return;
        }

        _appliedUpgrades[def.upgradeId] = newNumericLevel;
        ApplyNumericEffect(def, newNumericLevel);

        // 同步到 PlayerState（保持两份记录一致）
        SyncToPlayerState(def, newNumericLevel);

        if (_executors.TryGetValue(def.effectType, out var executor))
            executor.Execute(def, newNumericLevel);

        OnUpgradeApplied?.Invoke(def, newNumericLevel);

        Debug.Log($"[UpgradeEffectManager] 应用 {def.displayName} Lv.{newNumericLevel} (effectType={def.effectType})");
    }

    /// <summary>获取指定升级的当前等级（0=未获得）</summary>
    public int GetUpgradeLevel(string upgradeId)
    {
        return _appliedUpgrades.TryGetValue(upgradeId, out int lv) ? lv : 0;
    }

    // ── 数值查询接口 ──

    public float GetDamageMultiplier() => _damageMultiplier;

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
    public float GetAttackSpeedMultiplier() => _attackSpeedMultiplier;
    /// <summary>攻速累计加成百分比（如 1.45 → 45）</summary>
    public float GetAttackSpeedBonusPercent() => (_attackSpeedMultiplier - 1f) * 100f;
    public float GetMoveSpeedMultiplier() => _moveSpeedMultiplier;
    public float GetExpMultiplier() => _expMultiplier;
    public int GetStabRangeBonus() => _stabRangeBonus;
    public float GetStabDamagePenalty() => _stabDamagePenalty;
    public int GetSweepRangeBonus() => _sweepRangeBonus;
    public float GetSweepDamagePenalty() => _sweepDamagePenalty;
    public int GetPushWaveDistance() => _pushWaveDistance;
    public int GetConvergenceStep() => _convergenceStep;
    public float GetConvergenceDamagePercent() => _convergenceDamagePercent;
    public int GetDirectionalPushStep() => _directionalPushStep;

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

    /// <summary>
    /// 根据描述模板和当前等级生成效果文本
    /// </summary>
    public string GetDescription(UpgradeDefinition def)
    {
        int level = _appliedUpgrades.TryGetValue(def.upgradeId, out int lv) ? lv : 0;
        int nextLevel = level + 1;

        string desc = def.descriptionTemplate;

        if (def.category == UpgradeCategory.AttackPassive || def.category == UpgradeCategory.TimedPassive)
        {
            bool isTimed = def.category == UpgradeCategory.TimedPassive;
            // 被动：从每级配置读取
            if (def.effectType == "passive_timed_aoe")
            {
                if (def.timedAoeLevels != null && nextLevel <= def.timedAoeLevels.Count)
                {
                    var cfg = def.timedAoeLevels[nextLevel - 1];
                    string triggerStr = isTimed
                        ? cfg.intervalSeconds.ToString("F1")
                        : cfg.triggerThreshold.ToString();
                    desc = desc.Replace("{0}", triggerStr);
                    desc = desc.Replace("{1}", cfg.damage.ToString());
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
                desc = desc.Replace("{0}", swCfg.intervalSeconds.ToString("F1"));
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
        else if (def.effectType == "stab_range_boost" || def.effectType == "sweep_range_boost")
        {
            var cfg = def.GetNumericConfig(nextLevel);
            desc = desc.Replace("{0}", cfg.intValue.ToString());
            desc = desc.Replace("{1}", cfg.secondaryIntValue.ToString());
        }
        else if (def.effectType == "push_wave" || def.effectType == "convergence_wave")
        {
            var cfg = def.GetNumericConfig(nextLevel);
            desc = desc.Replace("{0}", cfg.intValue.ToString());
            desc = desc.Replace("{1}", (cfg.floatValue * 100f).ToString("F0"));
        }
        else if (def.effectType == "charge_damage_reduction")
        {
            var cfg = def.GetNumericConfig(nextLevel);
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
        else
        {
            var cfg = def.GetNumericConfig(nextLevel);
            desc = desc.Replace("{0}", (cfg.floatValue * 100f).ToString("F0"));
            desc = desc.Replace("{1}", cfg.intValue.ToString());
        }

        return desc;
    }

    private void SyncToPlayerState(UpgradeDefinition def, int level)
    {
        var ps = PlayerState.Instance;
        if (ps == null) return;

        for (int i = 0; i < ps.acquiredUpgrades.Count; i++)
        {
            if (ps.acquiredUpgrades[i].definition == def)
            {
                ps.acquiredUpgrades[i].currentLevel = level;
                return;
            }
        }
        ps.acquiredUpgrades.Add(new UpgradeAcquired { definition = def, currentLevel = level });
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
        _stabRangeBonus = 0;
        _stabDamagePenalty = 0f;
        _sweepRangeBonus = 0;
        _sweepDamagePenalty = 0f;
        _pushWaveDistance = 0;
        _convergenceStep = 0;
        _convergenceDamagePercent = 0.1f;
        _directionalPushStep = 0;
        _chargeDamageReduction = 0f;
        _chargeHitShockwaveDefinition = null;
        _chargeHitShockwaveLevel = 0;
        _chargeHitShockwaveHitCount = 0;

        _reflectShieldAmount = 0;
        _reflectShieldMaxAmount = 0;
        _reflectShieldCooldown = 0f;
        _reflectShieldInterval = 0f;
        _bonusReflectPercent = 0f;
        _reflectShieldReady = false;
        _isCharging = false;

        CycloneItemController.Instance?.ResetAll();
        SpikeTrapController.Instance?.ResetAll();

        // 清空被动攻击模块
        PassiveTriggerModule.Instance?.ResetAll();
        TimedPassiveModule.Instance?.ResetAll();

        // 清空道具库存
        ItemInventory.Instance?.ClearAll();
    }

    // ── 内部 ──

    private void ApplyNumericEffect(UpgradeDefinition def, int level)
    {
        var cfg = def.GetNumericConfig(level);

        switch (def.effectType)
        {
            case "damage_multiplier":
                _damageMultiplier += cfg.floatValue;
                break;
            case "attack_speed":
                _attackSpeedMultiplier += cfg.floatValue;
                break;
            case "move_speed":
                _moveSpeedMultiplier += cfg.floatValue;
                break;
            case "exp_multiplier":
                _expMultiplier += cfg.floatValue;
                break;
            case "stab_range_boost":
                _stabRangeBonus += cfg.intValue;
                _stabDamagePenalty += cfg.secondaryIntValue * 0.01f;
                break;
            case "sweep_range_boost":
                _sweepRangeBonus += cfg.intValue;
                _sweepDamagePenalty += cfg.secondaryIntValue * 0.01f;
                break;
            case "push_wave":
                _pushWaveDistance += cfg.intValue;
                break;
            case "convergence_wave":
                _convergenceStep += cfg.intValue;
                if (cfg.floatValue > 0f) _convergenceDamagePercent = cfg.floatValue;
                break;
            case "unlock_attack":
                break;
            case "charge_damage_reduction":
                _chargeDamageReduction += cfg.floatValue;
                break;
            case "charge_reflect_shield":
                if (def.reflectShieldLevels != null && level <= def.reflectShieldLevels.Count)
                {
                    var rsCfg = def.reflectShieldLevels[level - 1];
                    _reflectShieldInterval = rsCfg.intervalSeconds;
                    _reflectShieldMaxAmount = rsCfg.shieldAmount;
                    if (rsCfg.enableBonus)
                        _bonusReflectPercent = rsCfg.bonusReflectPercent / 100f;
                    _reflectShieldReady = true; // 首次获得/升级立即就绪
                    _reflectShieldCooldown = 0f;
                }
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
        }
    }
}
