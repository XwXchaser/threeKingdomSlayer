using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击计数触发模块 — 单例
///
/// 监听 AttackSystem.OnAttackPerformed，为每个已注册的升级维护独立计数器。
/// 计数器到达阈值时（由 UpgradeDefinition.triggerMode=AttackCount 决定），
/// 根据 effectType 分发到自包含的效果执行器。
///
/// 效果执行不再依赖攻击上下文（_lastAttackType 等已移除），
/// 每个效果从自身每级配置读取所需的全部参数。
/// </summary>
public class PassiveTriggerModule : MonoBehaviour
{
    public static PassiveTriggerModule Instance { get; private set; }

    [Header("特效预制体")]
    [Tooltip("喷火特效 prefab（effectType=passive_timed_aoe 被攻击计数触发时使用）")]
    public GameObject fireEffectPrefab;
    [Tooltip("箭雨特效 prefab（effectType=passive_timed_arrow 被攻击计数触发时使用）")]
    public GameObject arrowEffectPrefab;

    [Header("测试开关")]
    [Tooltip("开启后所有效果每次攻击都触发（忽略配表阈值）")]
    [SerializeField] private bool _forceTriggerEveryAttack;

    private class PassiveState
    {
        public UpgradeDefinition definition;
        public int level;
        public int currentCount;
        public int threshold;
    }

    private Dictionary<string, PassiveState> _states = new Dictionary<string, PassiveState>();

    public System.Action<string, int> OnPassiveRegistered;   // upgradeId, threshold
    public System.Action<string> OnPassiveTriggered;         // upgradeId

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

    private void Start()
    {
        if (AttackSystem.Instance != null)
            AttackSystem.Instance.OnAttackPerformed += OnAttackPerformed;
    }

    private void OnAttackPerformed(AttackType attackType, int targetColumn, bool slashLeftToRight)
    {
        foreach (var kv in _states)
        {
            var state = kv.Value;
            state.currentCount++;

            if (state.currentCount >= state.threshold)
            {
                state.currentCount = 0;
                DispatchEffect(state);
            }
        }
    }

    /// <summary>根据 effectType 分发到对应效果执行器</summary>
    private void DispatchEffect(PassiveState state)
    {
        switch (state.definition.effectType)
        {
            case "passive_phantom_weapon":
                StartCoroutine(ExecutePhantoms(state));
                break;
            case "passive_return_wave":
                StartCoroutine(ExecuteReturnWave(state));
                break;
            case "passive_chain_bounce":
                StartCoroutine(ExecuteChainBounce(state));
                break;
            case "passive_timed_aoe":
                ExecuteFire(state);
                break;
            case "passive_timed_arrow":
                ExecuteArrow(state);
                break;
            default:
                Debug.LogWarning($"[PassiveTriggerModule] 未知 effectType: {state.definition.effectType}");
                break;
        }
    }

    // ══════════════════════════════════════════
    // 注册 / 注销
    // ══════════════════════════════════════════

    public void Register(UpgradeDefinition def, int level)
    {
        if (def == null) return;

        int threshold = _forceTriggerEveryAttack ? 1 : def.GetTriggerThreshold(level);
        if (threshold <= 0)
        {
            Debug.LogWarning($"[PassiveTriggerModule] {def.upgradeId} Lv.{level} GetTriggerThreshold 返回 {threshold}，使用 intValue={def.intValue} 兜底");
            threshold = def.intValue > 0 ? def.intValue : 4;
        }

        if (_states.TryGetValue(def.upgradeId, out var existing))
        {
            existing.threshold = threshold;
            existing.level = level;
            existing.definition = def;
        }
        else
        {
            _states[def.upgradeId] = new PassiveState
            {
                definition = def,
                level = level,
                currentCount = 0,
                threshold = threshold
            };
        }

        OnPassiveRegistered?.Invoke(def.upgradeId, threshold);
        Debug.Log($"[PassiveTriggerModule] 注册: {def.displayName} Lv.{level} threshold={threshold} effectType={def.effectType}");
    }

    public void Unregister(string upgradeId)
    {
        _states.Remove(upgradeId);
    }

    public void ResetAll()
    {
        _states.Clear();
    }

    // ══════════════════════════════════════════
    // 效果执行器（自包含，不依赖攻击上下文）
    // ══════════════════════════════════════════

    private IEnumerator ExecutePhantoms(PassiveState state)
    {
        if (AttackSystem.Instance == null)
        {
            Debug.LogWarning("[PassiveTriggerModule] AttackSystem.Instance is null");
            yield break;
        }

        var def = state.definition;
        def.GetPhantomEffectConfig(state.level, out var attackType, out var targetColumn, out var steps);

        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning($"[PassiveTriggerModule] {def.displayName} phantomSteps 为空");
            yield break;
        }

        // slashLeftToRight 根据列位置推断（col 1-2 向右，col 3 向左）
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

            bool hit = AttackSystem.Instance.ExecutePhantomAttack(
                attackType, targetColumn, slashLeftToRight,
                step.damageRatio, step.alpha);

            if (!hit)
                Debug.LogWarning($"[PassiveTriggerModule] 幻影未命中 (type={attackType} col={targetColumn} ratio={step.damageRatio})");
        }

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 幻影触发: {def.displayName} steps={steps.Count} type={attackType} col={targetColumn}");
    }

    private IEnumerator ExecuteReturnWave(PassiveState state)
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

        bool hit = AttackSystem.Instance.ExecuteReturnWave(
            AttackType.Pierce, column, slashLeftToRight,
            damageRatio, rangeRows);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 折返波触发: {def.displayName} col={column} rows={rangeRows} ratio={damageRatio} hit={hit}");
    }

    private IEnumerator ExecuteChainBounce(PassiveState state)
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

        bool hit = AttackSystem.Instance.ExecuteChainBounce(
            AttackType.Pierce, column, damageRatio, maxBounces);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 连锁弹射触发: {def.displayName} col={column} maxBounces={maxBounces} ratio={damageRatio} hit={hit}");
    }

    private void ExecuteFire(PassiveState state)
    {
        var def = state.definition;
        if (def.timedAoeLevels == null || state.level > def.timedAoeLevels.Count) return;
        var cfg = def.timedAoeLevels[state.level - 1];
        if (cfg.columns == null || cfg.columns.Count == 0 || fireEffectPrefab == null) return;

        var instance = Instantiate(fireEffectPrefab);
        instance.GetComponent<ShootFireEffect>().Play(cfg.columns, cfg.damage);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 喷火触发: {def.displayName} damage={cfg.damage} cols=[{string.Join(",", cfg.columns)}]");
    }

    private void ExecuteArrow(PassiveState state)
    {
        var def = state.definition;
        if (def.timedArrowLevels == null || state.level > def.timedArrowLevels.Count) return;
        var cfg = def.timedArrowLevels[state.level - 1];
        if (arrowEffectPrefab == null) return;

        var instance = Instantiate(arrowEffectPrefab);
        instance.GetComponent<TimedArrowEffect>().Play(cfg.rowCount, cfg.arrowCount, cfg.damage);

        OnPassiveTriggered?.Invoke(def.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 箭雨触发: {def.displayName} rows={cfg.rowCount} arrows={cfg.arrowCount} damage={cfg.damage}");
    }
}
