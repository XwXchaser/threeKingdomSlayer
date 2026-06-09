using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定时触发模块 — 单例
///
/// 管理计时器驱动的被动技能。计时器到期时（由 UpgradeDefinition.triggerMode=Timed 决定），
/// 根据 effectType 分发到自包含的效果执行器。
///
/// 所有效果类型均由本模块统一分发，不限于火焰/箭雨。
/// </summary>
public class TimedPassiveModule : MonoBehaviour
{
    public static TimedPassiveModule Instance { get; private set; }

    [Header("特效预制体")]
    public GameObject fireEffectPrefab;
    public GameObject arrowEffectPrefab;

    private class TimedState
    {
        public UpgradeDefinition definition;
        public int level;
        public float timer;
    }

    private Dictionary<string, TimedState> _states = new Dictionary<string, TimedState>();

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
            _states[def.upgradeId] = new TimedState
            {
                definition = def,
                level = level,
                timer = interval
            };
        }

        Debug.Log($"[TimedPassiveModule] 注册 {def.displayName} Lv.{level} effectType={def.effectType} interval={interval}s");
    }

    public void Unregister(string upgradeId)
    {
        _states.Remove(upgradeId);
    }

    public void ResetAll()
    {
        _states.Clear();
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
        foreach (var kv in _states)
        {
            var state = kv.Value;
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
            default:
                Debug.LogWarning($"[TimedPassiveModule] 未知 effectType: {state.definition.effectType}");
                break;
        }
    }

    private void SpawnFire(TimedState state)
    {
        if (state.definition.timedAoeLevels == null || state.level > state.definition.timedAoeLevels.Count) return;
        var cfg = state.definition.timedAoeLevels[state.level - 1];
        if (cfg.columns == null || cfg.columns.Count == 0 || fireEffectPrefab == null) return;

        var instance = Instantiate(fireEffectPrefab);
        instance.GetComponent<ShootFireEffect>().Play(cfg.columns, cfg.damage);
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
}