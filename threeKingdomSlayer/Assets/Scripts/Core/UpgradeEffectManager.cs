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
public class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier
{
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

    public struct DotState
    {
        public int damagePerTick;
        public int totalTicks;
        public int ticksRemaining;
        public float tickTimer;
    }

    public struct DiseaseState
    {
        public int totalDamagePerLayer;
        public int durationSeconds;
        public int damagePerTick;
        public int layers;
        public int totalTicks;
        public int ticksRemaining;
        public float tickTimer;
        public bool smartSpread;
    }

    public struct DotStatus
    {
        public bool isBurning;
        public float burnProgress;
        public bool isDiseased;
        public float diseaseProgress;
        public int diseaseLayers;
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

    private static bool IsDotPaused(Enemy enemy)
    {
        return enemy != null && enemy.isBoss &&
            (enemy.bossState != BossState.InCombat || enemy.state == EnemyState.QTEAttacking || enemy.isPhaseTransitioning);
    }

    private void TickBurnDamage()
    {
        TickBurnStates();
        TickDiseaseStates();
    }

    private void TickBurnStates()
    {
        if (_burnStates.Count == 0) return;

        _burnUpdateBuffer.Clear();
        var keys = new Enemy[_burnStates.Count];
        _burnStates.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            var enemy = keys[i];
            if (enemy != null && _burnStates.TryGetValue(enemy, out var state))
                _burnUpdateBuffer.Add(new KeyValuePair<Enemy, DotState>(enemy, state));
        }

        _dotRemovalList.Clear();
        float dt = Time.deltaTime;
        for (int i = 0; i < _burnUpdateBuffer.Count; i++)
        {
            var enemy = _burnUpdateBuffer[i].Key;
            var state = _burnUpdateBuffer[i].Value;
            if (enemy == null || enemy.state == EnemyState.Dead)
            {
                _dotRemovalList.Add(enemy);
                continue;
            }

            if (IsDotPaused(enemy))
            {
                _burnStates[enemy] = state;
                continue;
            }

            state.tickTimer -= dt;
            if (state.tickTimer <= 0f)
            {
                enemy.TakeDamage(state.damagePerTick, DamageType.Pierce, Color.red, countsForCombo: false, canInterruptAttack: false, triggerHitAnimation: false, ignoreDamageModifiers: true);
                state.ticksRemaining--;
                state.tickTimer += BurnTickInterval;
            }

            if (enemy.state == EnemyState.Dead || state.ticksRemaining <= 0)
                _dotRemovalList.Add(enemy);
            else
                _burnStates[enemy] = state;
        }

        for (int i = 0; i < _dotRemovalList.Count; i++)
            _burnStates.Remove(_dotRemovalList[i]);
    }

    private void TickDiseaseStates()
    {
        if (_diseaseStates.Count == 0) return;

        _diseaseUpdateBuffer.Clear();
        var keys = new Enemy[_diseaseStates.Count];
        _diseaseStates.Keys.CopyTo(keys, 0);
        for (int i = 0; i < keys.Length; i++)
        {
            var enemy = keys[i];
            if (enemy != null && _diseaseStates.TryGetValue(enemy, out var state))
                _diseaseUpdateBuffer.Add(new KeyValuePair<Enemy, DiseaseState>(enemy, state));
        }

        _dotRemovalList.Clear();
        float dt = Time.deltaTime;
        for (int i = 0; i < _diseaseUpdateBuffer.Count; i++)
        {
            var enemy = _diseaseUpdateBuffer[i].Key;
            var state = _diseaseUpdateBuffer[i].Value;
            if (enemy == null || enemy.state == EnemyState.Dead)
            {
                _dotRemovalList.Add(enemy);
                continue;
            }

            if (IsDotPaused(enemy))
            {
                _diseaseStates[enemy] = state;
                continue;
            }

            state.tickTimer -= dt;
            if (state.tickTimer <= 0f)
            {
                int tickDmg = state.damagePerTick * state.layers;
                DebugLog.Info($"[Disease] Tick: {enemy.DebugTag} dmg={tickDmg} remaining={state.ticksRemaining}");
                enemy.TakeDamage(tickDmg, DamageType.Pierce, new Color(0.72f, 0.28f, 0.9f), countsForCombo: false, canInterruptAttack: false, triggerHitAnimation: false, ignoreDamageModifiers: true);
                state.ticksRemaining--;
                state.tickTimer += DiseaseTickInterval;
            }

            if (enemy.state == EnemyState.Dead || state.ticksRemaining <= 0)
                _dotRemovalList.Add(enemy);
            else
                _diseaseStates[enemy] = state;
        }

        for (int i = 0; i < _dotRemovalList.Count; i++)
        {
            var enemy = _dotRemovalList[i];
            _diseaseStates.Remove(enemy);
            if (enemy != null)
                enemy.OnDying -= HandleDiseasedEnemyDeath;
        }
    }

    /// <summary>施加灼烧，总伤害在持续时间内按固定跳字间隔结算。</summary>
    public void ApplyBurn(Enemy enemy, int totalDamage, float burnDuration)
    {
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(burnDuration / BurnTickInterval));
        int damagePerTick = totalDamage / tickCount;
        if (enemy == null || damagePerTick <= 0 || burnDuration <= 0f) return;

        if (_burnStates.TryGetValue(enemy, out var existing))
        {
            existing.totalTicks = tickCount;
            existing.ticksRemaining = tickCount;
            existing.damagePerTick = Mathf.Max(existing.damagePerTick, damagePerTick);
            existing.tickTimer = BurnTickInterval;
            _burnStates[enemy] = existing;
            return;
        }

        _burnStates[enemy] = new DotState
        {
            damagePerTick = damagePerTick,
            totalTicks = tickCount,
            ticksRemaining = tickCount,
            tickTimer = BurnTickInterval
        };
    }

    public void ApplyDisease(Enemy enemy, int totalDamage, int durationSeconds, int layers = 1, bool addToExistingLayers = true, bool smartSpread = false)
    {
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(durationSeconds / DiseaseTickInterval));
        int damagePerTick = Mathf.Max(1, totalDamage / tickCount);
        if (enemy == null || enemy.state == EnemyState.Dead || durationSeconds <= 0 || layers <= 0) return;

        if (_diseaseStates.TryGetValue(enemy, out var existing))
        {
            existing.layers = addToExistingLayers
                ? existing.layers + layers
                : Mathf.Max(existing.layers, layers);
            existing.totalDamagePerLayer = Mathf.Max(existing.totalDamagePerLayer, totalDamage);
            existing.durationSeconds = Mathf.Max(existing.durationSeconds, durationSeconds);
            existing.damagePerTick = Mathf.Max(existing.damagePerTick, damagePerTick);
            existing.totalTicks = tickCount;
            existing.ticksRemaining = tickCount;
            existing.tickTimer = DiseaseTickInterval;
            existing.smartSpread = existing.smartSpread || smartSpread;
            _diseaseStates[enemy] = existing;
            return;
        }

        _diseaseStates[enemy] = new DiseaseState
        {
            totalDamagePerLayer = totalDamage,
            durationSeconds = durationSeconds,
            damagePerTick = damagePerTick,
            layers = layers,
            totalTicks = tickCount,
            ticksRemaining = tickCount,
            tickTimer = DiseaseTickInterval,
            smartSpread = smartSpread
        };
        enemy.OnDying += HandleDiseasedEnemyDeath;
        DebugLog.Info($"[Disease] ApplyDisease: {enemy.DebugTag} totalDmg={totalDamage} ticks={tickCount} dmgPerTick={damagePerTick} layers={layers} smartSpread={smartSpread}");
    }

    private void HandleDiseasedEnemyDeath(Enemy enemy)
    {
        if (enemy == null || !_diseaseStates.TryGetValue(enemy, out var state)) return;

        int column = enemy.columnIndex;
        int row = enemy.rowIndex;
        _diseaseStates.Remove(enemy);
        enemy.OnDying -= HandleDiseasedEnemyDeath;

        if (enemy.isBoss) return;

        var columnManager = AttackSystem.Instance?.columnManager;
        if (columnManager == null) return;

        if (state.smartSpread)
        {
            Enemy target = SelectDiseaseSpreadTarget(columnManager, column, row);
            if (target != null)
                ApplyDisease(target, state.totalDamagePerLayer, state.durationSeconds, state.layers, addToExistingLayers: false, smartSpread: true);
        }
        else
        {
            // 全相邻传播：左、右、后
            SpreadToIfValid(columnManager.GetEnemyAt(column - 1, row), state);
            SpreadToIfValid(columnManager.GetEnemyAt(column + 1, row), state);
            SpreadToIfValid(columnManager.GetEnemyAt(column, row + 1), state);
        }
    }

    private void SpreadToIfValid(Enemy target, DiseaseState state)
    {
        if (target != null && !target.isBoss && target.state != EnemyState.Dead)
            ApplyDisease(target, state.totalDamagePerLayer, state.durationSeconds, state.layers, addToExistingLayers: false, smartSpread: state.smartSpread);
    }

    private static Enemy SelectDiseaseSpreadTarget(ColumnManager columnManager, int column, int row)
    {
        // 优先同行（左右），其次后排；同行内选最近，后排内选最近
        Enemy closestSameRow = null;
        int closestSameRowDist = int.MaxValue;
        Enemy closestOtherRow = null;
        int closestOtherRowDist = int.MaxValue;
        var allEnemies = columnManager.GetAllEnemies();
        if (allEnemies == null) { DebugLog.Info("[DiseaseSpread] GetAllEnemies返回null"); return null; }

        DebugLog.Info($"[DiseaseSpread] 扫描{allEnemies.Count}个敌人，源位置(col={column},row={row})");
        for (int i = 0; i < allEnemies.Count; i++)
        {
            var e = allEnemies[i];
            if (e == null) continue;
            if (e.isBoss || e.state == EnemyState.Dead) continue;
            if (e.columnIndex == column && e.rowIndex == row) continue;
            int colDist = Mathf.Abs(e.columnIndex - column);
            int rowDist = Mathf.Abs(e.rowIndex - row);
            int dist = colDist + rowDist;
            bool sameRow = e.rowIndex == row;
            DebugLog.Info($"[DiseaseSpread] 候选: {e.DebugTag} col={e.columnIndex} row={e.rowIndex} dist={dist} sameRow={sameRow}");
            if (sameRow)
            {
                if (dist < closestSameRowDist) { closestSameRowDist = dist; closestSameRow = e; }
            }
            else
            {
                if (dist < closestOtherRowDist) { closestOtherRowDist = dist; closestOtherRow = e; }
            }
        }
        var result = closestSameRow ?? closestOtherRow;
        DebugLog.Info($"[DiseaseSpread] 结果: {(result != null ? result.DebugTag : "null")} sameRow={closestSameRow != null} pickedSameRowDist={closestSameRowDist} pickedOtherRowDist={closestOtherRowDist}");
        return result;
    }

    public DotStatus GetDotStatus(Enemy enemy)
    {
        if (enemy == null || enemy.state == EnemyState.Dead)
            return default;

        var status = new DotStatus();
        if (_burnStates.TryGetValue(enemy, out var burn))
        {
            status.isBurning = burn.ticksRemaining > 0;
            status.burnProgress = status.isBurning
                ? Mathf.Clamp01((burn.ticksRemaining - 1 + Mathf.Clamp01(burn.tickTimer / BurnTickInterval)) / Mathf.Max(1f, burn.totalTicks))
                : 0f;
        }

        if (_diseaseStates.TryGetValue(enemy, out var disease))
        {
            status.isDiseased = disease.ticksRemaining > 0;
            status.diseaseProgress = status.isDiseased
                ? Mathf.Clamp01((disease.ticksRemaining - 1 + Mathf.Clamp01(disease.tickTimer / DiseaseTickInterval)) / Mathf.Max(1f, disease.totalTicks))
                : 0f;
            status.diseaseLayers = status.isDiseased ? disease.layers : 0;
        }

        return status;
    }

    /// <summary>检查敌人是否处于灼烧状态</summary>
    public bool IsBurning(Enemy enemy)
    {
        return enemy != null && _burnStates.TryGetValue(enemy, out var state) && state.ticksRemaining > 0;
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
        if (def == null) return;

        if (def.category == UpgradeCategory.ActiveSkill)
        {
            if (!(def is ActiveSkillDefinition activeSkill))
            {
                Debug.LogWarning($"[UpgradeEffectManager] {def.name} 标记为 ActiveSkill，但资产类型不是 ActiveSkillDefinition");
                return;
            }
            if (ActiveSkillInventory.Instance == null || !ActiveSkillInventory.Instance.UsesActiveSkills)
                return;
            if (!ActiveSkillInventory.Instance.AcquireOrUpgrade(activeSkill, out int activeSkillLevel))
                return;

            SyncToPlayerState(def, activeSkillLevel);
            OnUpgradeApplied?.Invoke(def, activeSkillLevel);
            Debug.Log($"[UpgradeEffectManager] 获得主动技能 {def.displayName} Lv.{activeSkillLevel}");
            return;
        }

        // V1 道具型：路由到 ItemInventory
        if (def.category == UpgradeCategory.Item && !string.IsNullOrEmpty(def.gestureId))
        {
            if (ItemInventory.Instance == null)
            {
                Debug.LogWarning("[UpgradeEffectManager] ItemInventory 未找到，道具无法添加");
                return;
            }
            if (!ItemInventory.Instance.CanAdd(def))
            {
                // 道具栏满 → 弹出弃置弹窗
                var entries = new List<ItemInventory.ItemEntry>(ItemInventory.Instance.Entries);
                ItemDiscardPopup.Show(entries, def, result =>
                {
                    if (!result.DiscardNew && ItemInventory.Instance.DiscardEntryById(result.EntryId))
                    {
                        ItemInventory.Instance.AddItem(def);
                        SyncToPlayerState(def, 0);
                        OnUpgradeApplied?.Invoke(def, 0);
                    }
                });
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

        CycloneItemController.Instance?.ResetAll();
        SpikeTrapController.Instance?.ResetAll();

        // 清空被动攻击模块
        PassiveTriggerModule.Instance?.ResetAll();
        TimedPassiveModule.Instance?.ResetAll();

        // 清空 V1 道具与 V2 主动技能运行态
        ItemInventory.Instance?.ClearAll();
        ActiveSkillInventory.Instance?.ResetAll();
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
            case "item_drop_rate":
                _itemDropRateBonus += cfg.floatValue;
                break;
            case "item_damage_bonus":
                _itemDamageBonus += cfg.floatValue;
                break;
            case "stab_range_boost":
            case "sweep_range_boost":
                _attackRangeBonus += cfg.intValue;
                _attackDamagePenalty += cfg.secondaryIntValue * 0.01f;
                break;
            case "attack_range_boost":
                _attackRangeBonus = cfg.intValue;
                _attackDamagePenalty = cfg.secondaryIntValue * 0.01f;
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
            case "cooldown_reduction":
                _activeSkillCDReduction += cfg.floatValue;
                break;
        }
    }
}
