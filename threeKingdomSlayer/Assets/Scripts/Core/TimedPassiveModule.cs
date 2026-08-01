using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定时触发模块 — 单例
///
/// 管理计时器驱动的被动技能。计时器到期时，根据 effectType 分发到自包含的效果执行器。
///
/// 所有效果类型均由本模块统一分发，不限于火焰/箭雨。
/// </summary>
public class TimedPassiveModule : MonoBehaviour
{
    public static TimedPassiveModule Instance { get; private set; }

    [Header("特效预制体")]
    public GameObject fireEffectPrefab;
    public GameObject arrowEffectPrefab;
    public GameObject cycloneEffectPrefab;

    [Header("蛇形喷射")]
    [Tooltip("相对火焰Prefab起点的Z偏移。Prefab基础起点为-2，设置-4时最终起点为-6。")]
    public float fireSweepStartZOffset = -4f;

    private class TimedState
    {
        public UpgradeDefinition definition;
        public int level;
        public float timer;
    }

    private Dictionary<string, TimedState> _states = new Dictionary<string, TimedState>();

    // 蓄力冲击波：每计时器 tick 积攒层数（不立即生成），蓄力攻击时统一释放
    private Dictionary<string, int> _shockwaveLayers = new Dictionary<string, int>();

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

    /// <summary>注册/更新定时被动（由 UpgradeEffectManager 调用）</summary>
    public void Register(UpgradeDefinition def, int level)
    {
        float interval = GetIntervalForLevel(def, level);
        if (interval <= 0f)
        {
            Debug.LogWarning($"[TimedPassiveModule] {def.upgradeId} Lv.{level} 配置异常，interval<=0");
            return;
        }

        if (_states.TryGetValue(def.upgradeId, out var existing))
        {
            existing.level = level;
            existing.definition = def;
        }
        else
        {
            var newState = new TimedState
            {
                definition = def,
                level = level,
                timer = interval
            };
            _states[def.upgradeId] = newState;

            // 队列型效果首次获得时授予一层；其他定时效果立即释放一次。
            if (def.effectType == "charge_shockwave")
            {
                _shockwaveLayers[def.upgradeId] = 1;
                Debug.Log($"[TimedPassiveModule] {def.displayName} 首次获得，立即授予 1 层");
            }
            else
            {
                SpawnEffect(newState);
                Debug.Log($"[TimedPassiveModule] {def.displayName} 首次获得，立即触发 1 次");
            }
        }

        Debug.Log($"[TimedPassiveModule] 注册 {def.displayName} Lv.{level} effectType={def.effectType} interval={interval}s");
    }

    public void Unregister(string upgradeId)
    {
        _states.Remove(upgradeId);
        _shockwaveLayers.Remove(upgradeId);
    }

    public void ResetAll()
    {
        _states.Clear();
        _shockwaveLayers.Clear();
    }

    // ── 冷却显示 API ──

    /// <summary>获取所有已注册的计时被动 upgradeId（供 UI 遍历）</summary>
    public IEnumerable<string> RegisteredUpgradeIds
    {
        get
        {
            foreach (var kv in _states)
                yield return kv.Key;
        }
    }

    /// <summary>获取当前剩余时间（秒），-1=未注册</summary>
    public float GetTimer(string upgradeId)
    {
        return _states.TryGetValue(upgradeId, out var s) ? s.timer : -1f;
    }

    /// <summary>获取总间隔时间（秒），-1=未注册</summary>
    public float GetInterval(string upgradeId)
    {
        if (!_states.TryGetValue(upgradeId, out var s)) return -1f;
        return GetIntervalForLevel(s.definition, s.level);
    }

    // ── Update ──

    private void Update()
    {
        bool isCharging = PlayerState.Instance != null && PlayerState.Instance.IsCharging;

        foreach (var kv in _states)
        {
            var state = kv.Value;

            // 蓄力冲击波：仅在 layers==0 或蓄力时走 timer
            if (state.definition.effectType == "charge_shockwave")
            {
                int layers = _shockwaveLayers.TryGetValue(kv.Key, out int l) ? l : 0;
                bool shouldTick = layers == 0 || isCharging;
                if (!shouldTick) continue;
            }

            state.timer -= Time.deltaTime;
            if (state.timer <= 0f)
            {
                state.timer = GetIntervalForLevel(state.definition, state.level);
                SpawnEffect(state);
            }
        }
    }

    private void SpawnEffect(TimedState state)
    {
        switch (state.definition.effectType)
        {
            case "passive_timed_aoe":
                SpawnFire(state);
                break;
            case "passive_timed_arrow":
                SpawnArrow(state);
                break;
            case "passive_phantom_weapon":
                StartCoroutine(SpawnPhantom(state));
                break;
            case "passive_return_wave":
                StartCoroutine(SpawnReturnWave(state));
                break;
            case "passive_chain_bounce":
                StartCoroutine(SpawnChainBounce(state));
                break;
            case "passive_timed_cyclone":
                SpawnCyclone(state);
                break;
            case "charge_shockwave":
                AccumulateShockwave(state);
                break;
            default:
                Debug.LogWarning($"[TimedPassiveModule] 未知 effectType: {state.definition.effectType}");
                break;
        }
    }

    private void SpawnFire(TimedState state)
    {
        if (state.definition.timedAoeLevels == null || state.level > state.definition.timedAoeLevels.Count) return;
        var cfg = state.definition.timedAoeLevels[state.level - 1];
        if (fireEffectPrefab == null) return;

        // 使用资产配置的列数，不再硬编码全5列
        var cols = cfg.columns != null && cfg.columns.Count > 0
            ? cfg.columns
            : new List<int> { 0, 1, 2, 3, 4 };
        var instance = Instantiate(fireEffectPrefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        effect.PlaySweep(cols, cfg.damage, cfg.rangeRows, fireSweepStartZOffset, cfg.burnTotalDamage, cfg.burnDurationSeconds);
    }

    private void SpawnArrow(TimedState state)
    {
        if (state.definition.timedArrowLevels == null || state.level > state.definition.timedArrowLevels.Count) return;
        var cfg = state.definition.timedArrowLevels[state.level - 1];
        if (arrowEffectPrefab == null) return;

        var instance = Instantiate(arrowEffectPrefab);
        instance.GetComponent<TimedArrowEffect>().Play(cfg.rowCount, cfg.arrowCount, cfg.damage);
    }

    private static float GetIntervalForLevel(UpgradeDefinition def, int level)
    {
        return def.GetTriggerInterval(level);
    }

    // ══════════════════════════════════════════
    // 幻影 / 折返波 / 连锁弹射 — 定时触发版本
    // ══════════════════════════════════════════

    private System.Collections.IEnumerator SpawnPhantom(TimedState state)
    {
        if (AttackSystem.Instance == null) yield break;

        var def = state.definition;
        def.GetPhantomEffectConfig(state.level, out var attackType, out var targetColumn, out var steps);

        if (steps == null || steps.Count == 0) yield break;

        bool slashLeftToRight = targetColumn <= 2;

        for (int i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (i == 0 && step.delaySeconds > 0f)
                yield return new WaitForSeconds(step.delaySeconds);
            if (i > 0)
            {
                float delay = step.delaySeconds > 0f ? step.delaySeconds : 0.15f;
                yield return new WaitForSeconds(delay);
            }

            if (AttackSystem.Instance == null) yield break;
            AttackSystem.Instance.ExecutePhantomAttack(
                attackType, targetColumn, slashLeftToRight,
                step.damageRatio, step.alpha);
        }

        Debug.Log($"[TimedPassiveModule] 幻影触发: {def.displayName} steps={steps.Count} type={attackType} col={targetColumn}");
    }

    private System.Collections.IEnumerator SpawnReturnWave(TimedState state)
    {
        if (AttackSystem.Instance == null) yield break;

        var def = state.definition;
        int level = state.level;
        var waveCfg = (def.returnWaveLevels != null && level <= def.returnWaveLevels.Count)
            ? def.returnWaveLevels[level - 1]
            : new ReturnWaveLevelConfig { column = 2, rangeRows = 2, damageRatio = def.floatValue };

        int column = waveCfg.column > 0 ? waveCfg.column : 2;
        int rangeRows = waveCfg.rangeRows > 0 ? waveCfg.rangeRows : 2;
        float damageRatio = waveCfg.damageRatio > 0f ? waveCfg.damageRatio : def.floatValue;
        bool slashLeftToRight = column <= 2;

        AttackSystem.Instance.ExecuteReturnWave(
            AttackType.Pierce, column, slashLeftToRight,
            damageRatio, rangeRows);

        Debug.Log($"[TimedPassiveModule] 折返波触发: {def.displayName} col={column} rows={rangeRows} ratio={damageRatio}");
    }

    private System.Collections.IEnumerator SpawnChainBounce(TimedState state)
    {
        if (AttackSystem.Instance == null) yield break;

        var def = state.definition;
        int level = state.level;
        var bounceCfg = (def.chainBounceLevels != null && level <= def.chainBounceLevels.Count)
            ? def.chainBounceLevels[level - 1]
            : new ChainBounceLevelConfig { column = 2, maxBounces = 3, damageRatio = def.floatValue };

        int column = bounceCfg.column > 0 ? bounceCfg.column : 2;
        int maxBounces = bounceCfg.maxBounces > 0 ? bounceCfg.maxBounces : 3;
        float damageRatio = bounceCfg.damageRatio > 0f ? bounceCfg.damageRatio : def.floatValue;

        AttackSystem.Instance.ExecuteChainBounce(
            AttackType.Pierce, column, damageRatio, maxBounces);

        Debug.Log($"[TimedPassiveModule] 连锁弹射触发: {def.displayName} col={column} maxBounces={maxBounces} ratio={damageRatio}");
    }

    // ══════════════════════════════════════════
    // 旋风 — 定时触发版本
    // ══════════════════════════════════════════

    private void SpawnCyclone(TimedState state)
    {
        var def = state.definition;
        if (def.cycloneLevels == null || state.level > def.cycloneLevels.Count) return;
        var cfg = def.cycloneLevels[state.level - 1];
        if (cycloneEffectPrefab == null) return;

        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null) return;

        // 收集所有可击飞的敌人
        var candidates = new System.Collections.Generic.List<Enemy>();
        foreach (var enemy in cm.GetAllEnemies())
        {
            if (enemy != null && enemy.state != EnemyState.Dead &&
                (enemy.state == EnemyState.Launched || enemy.CanBeLaunched(float.MaxValue)))
                candidates.Add(enemy);
        }

        if (candidates.Count == 0) return;

        // 随机选取 cfg.enemyCount 个
        int pickCount = Mathf.Min(cfg.enemyCount, candidates.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int r = Random.Range(i, candidates.Count);
            var temp = candidates[i];
            candidates[i] = candidates[r];
            candidates[r] = temp;
        }

        for (int i = 0; i < pickCount; i++)
        {
            var enemy = candidates[i];
            var instance = Instantiate(cycloneEffectPrefab);
            var fx = instance.GetComponent<CycloneEffect>();
            if (fx != null)
                fx.Setup(enemy, cfg.damage, cfg.landingDamage, cfg.knockupDuration);
            else
                Debug.LogWarning("[TimedPassiveModule] CycloneEffect prefab 缺少 CycloneEffect 组件");
        }

        Debug.Log($"[TimedPassiveModule] 旋风触发: {def.displayName} 选取 {pickCount}/{candidates.Count} 个敌人");
    }

    // ══════════════════════════════════════════
    // 蓄力冲击波 — 队列型（timer→层数，攻击时释放）
    // ══════════════════════════════════════════

    private void AccumulateShockwave(TimedState state)
    {
        string id = state.definition.upgradeId;
        if (!_shockwaveLayers.ContainsKey(id))
            _shockwaveLayers[id] = 0;
        _shockwaveLayers[id] += 1;
        Debug.Log($"[TimedPassiveModule] 冲击波层数+1: {state.definition.displayName} 当前 {_shockwaveLayers[id]} 层");
    }

    /// <summary>消耗所有蓄力冲击波层数，返回 (upgradeId, 总层数, 每级配置)</summary>
    public List<ShockwaveConsumeResult> ConsumeAllShockwaves()
    {
        var results = new List<ShockwaveConsumeResult>();
        foreach (var kv in _states)
        {
            if (kv.Value.definition.effectType != "charge_shockwave") continue;
            if (!_shockwaveLayers.TryGetValue(kv.Key, out int layers) || layers <= 0) continue;

            var def = kv.Value.definition;
            int level = kv.Value.level;
            if (def.chargeShockwaveLevels == null || level > def.chargeShockwaveLevels.Count) continue;

            var cfg = def.chargeShockwaveLevels[level - 1];
            results.Add(new ShockwaveConsumeResult
            {
                upgradeId = kv.Key,
                layers = layers,
                config = cfg
            });
            _shockwaveLayers.Remove(kv.Key);
        }
        return results;
    }
}

/// <summary>蓄力冲击波消耗结果</summary>
public struct ShockwaveConsumeResult
{
    public string upgradeId;
    public int layers;
    public ChargeShockwaveLevelConfig config;
}