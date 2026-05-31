using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 被动攻击型模块 — 单例
///
/// 监听 AttackSystem.OnAttackPerformed，为每个已注册的被动升级维护独立计数器。
/// 计数器到达阈值时，按 effectType 分发到对应执行器：
///   - passive_phantom_weapon: 依次执行 phantomSteps 中配置的幻影攻击
///   - passive_return_wave:  触发折返波（当前攻击的波到达终点后折返再次命中）
///   - passive_chain_bounce: 触发连锁弹射（命中敌人后弹射至同行最近敌人）
/// </summary>
public class PassiveTriggerModule : MonoBehaviour
{
    public static PassiveTriggerModule Instance { get; private set; }

    /// <summary>被动子类型标识，与 UpgradeDefinition.effectType 对齐</summary>
    public enum PassiveKind
    {
        Phantom,     // passive_phantom_weapon — 通用幻影攻击
        ReturnWave,  // passive_return_wave — 折返波
        ChainBounce  // passive_chain_bounce — 连锁弹射
    }

    private class PassiveState
    {
        public UpgradeDefinition definition;
        public int currentCount;
        public int threshold;
        public List<PhantomStep> phantomSteps;
        public PassiveKind kind;
        public float damageRatio; // return_wave: 折返伤害比例; chain_bounce: 弹射伤害保留比例
        public int maxBounces;    // chain_bounce 最大弹射次数
    }

    private Dictionary<string, PassiveState> _states = new Dictionary<string, PassiveState>();

    // 攻击上下文 — 由 OnAttackPerformed 暂存，供效果执行时使用
    private AttackType _lastAttackType;
    private int _lastTargetColumn;
    private bool _lastSlashLeftToRight;

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
        _lastAttackType = attackType;
        _lastTargetColumn = targetColumn;
        _lastSlashLeftToRight = slashLeftToRight;

        foreach (var kv in _states)
        {
            var state = kv.Value;
            state.currentCount++;

            if (state.currentCount >= state.threshold)
            {
                state.currentCount = 0;

                switch (state.kind)
                {
                    case PassiveKind.ReturnWave:
                        StartCoroutine(ExecuteReturnWave(state));
                        break;
                    case PassiveKind.ChainBounce:
                        StartCoroutine(ExecuteChainBounce(state));
                        break;
                    default:
                        StartCoroutine(ExecutePhantoms(state));
                        break;
                }
            }
        }
    }

    private System.Collections.IEnumerator ExecutePhantoms(PassiveState state)
    {
        if (AttackSystem.Instance == null)
        {
            Debug.LogWarning("[PassiveTriggerModule] AttackSystem.Instance is null, 无法执行幻影");
            yield break;
        }
        if (state.phantomSteps == null || state.phantomSteps.Count == 0)
        {
            Debug.LogWarning($"[PassiveTriggerModule] {state.definition.displayName} phantomSteps 为空");
            yield break;
        }

        for (int i = 0; i < state.phantomSteps.Count; i++)
        {
            var step = state.phantomSteps[i];

            // 首段之前的延迟
            if (i == 0 && step.delaySeconds > 0f)
                yield return new WaitForSeconds(step.delaySeconds);
            // 段间延迟
            if (i > 0)
            {
                float delay = step.delaySeconds > 0f ? step.delaySeconds : 0.15f;
                yield return new WaitForSeconds(delay);
            }

            if (AttackSystem.Instance == null) yield break;

            bool hit = AttackSystem.Instance.ExecutePhantomAttack(
                _lastAttackType, _lastTargetColumn, _lastSlashLeftToRight,
                step.damageRatio, step.alpha);
            if (!hit)
                Debug.LogWarning($"[PassiveTriggerModule] 幻影攻击未命中 (type={_lastAttackType} col={_lastTargetColumn} ratio={step.damageRatio})");
        }

        OnPassiveTriggered?.Invoke(state.definition.upgradeId);
        Debug.Log($"[PassiveTriggerModule] {state.definition.displayName} 触发, 幻影段数={state.phantomSteps.Count}, 攻击类型={_lastAttackType}");
    }

    /// <summary>注册被动升级（由 UpgradeEffectManager 调用）</summary>
    public void Register(UpgradeDefinition def, int level)
    {
        if (def == null) return;

        PassiveKind kind = ResolveKind(def.effectType);
        int triggerParam;
        List<PhantomStep> steps = null;
        float damageRatio = def.floatValue;
        int maxBounces = def.secondaryIntValue > 0 ? def.secondaryIntValue : 3;

        if (kind == PassiveKind.Phantom)
        {
            def.GetPhantomConfig(level, out triggerParam, out steps);
        }
        else
        {
            // return_wave / chain_bounce: 阈值来自 intValue
            triggerParam = def.intValue > 0 ? def.intValue : (kind == PassiveKind.ReturnWave ? 4 : 6);
        }

        if (_states.TryGetValue(def.upgradeId, out var existing))
        {
            existing.threshold = triggerParam;
            existing.phantomSteps = steps;
            existing.definition = def;
            existing.kind = kind;
            existing.damageRatio = damageRatio;
            existing.maxBounces = maxBounces;
        }
        else
        {
            _states[def.upgradeId] = new PassiveState
            {
                definition = def,
                currentCount = 0,
                threshold = triggerParam,
                phantomSteps = steps,
                kind = kind,
                damageRatio = damageRatio,
                maxBounces = maxBounces
            };
        }

        OnPassiveRegistered?.Invoke(def.upgradeId, triggerParam);
        Debug.Log($"[PassiveTriggerModule] 注册被动: {def.displayName} Lv.{level} kind={kind} threshold={triggerParam}");
    }

    private static PassiveKind ResolveKind(string effectType)
    {
        switch (effectType)
        {
            case "passive_return_wave":  return PassiveKind.ReturnWave;
            case "passive_chain_bounce": return PassiveKind.ChainBounce;
            default:                     return PassiveKind.Phantom;
        }
    }

    /// <summary>折返波：在当前攻击的波到达终点后，折返再次命中路径上所有敌人</summary>
    private System.Collections.IEnumerator ExecuteReturnWave(PassiveState state)
    {
        if (AttackSystem.Instance == null) yield break;

        // 折返波仅对 Pierce / Sweep 有效（列/排攻击波可折返）
        if (_lastAttackType != AttackType.Pierce && _lastAttackType != AttackType.Sweep)
        {
            // 非折返兼容类型 → 退化为普通幻影攻击
            AttackSystem.Instance.ExecutePhantomAttack(
                _lastAttackType, _lastTargetColumn, _lastSlashLeftToRight,
                state.damageRatio, 0.6f);
            yield break;
        }

        // Phase 5 将实现真正的折返：当前波到达终点 → 掉头 → 再次命中
        // Phase 2 占位：先打出一次幻影攻击作为基础效果
        bool hit = AttackSystem.Instance.ExecuteReturnWave(
            _lastAttackType, _lastTargetColumn, _lastSlashLeftToRight,
            state.damageRatio);

        OnPassiveTriggered?.Invoke(state.definition.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 折返波触发: {state.definition.displayName} type={_lastAttackType} ratio={state.damageRatio} hit={hit}");
    }

    /// <summary>连锁弹射：命中敌人后弹射至同行最近敌人，最多弹射 N 次，每次递减伤害</summary>
    private System.Collections.IEnumerator ExecuteChainBounce(PassiveState state)
    {
        if (AttackSystem.Instance == null) yield break;

        // 连锁弹射仅对 Pierce 有效（列攻击才能弹射至同行其他列）
        if (_lastAttackType != AttackType.Pierce)
        {
            // 非 Pierce → 退化为普通幻影攻击
            AttackSystem.Instance.ExecutePhantomAttack(
                _lastAttackType, _lastTargetColumn, _lastSlashLeftToRight,
                state.damageRatio, 0.6f);
            yield break;
        }

        // Phase 5 将实现真正的弹射：找到同行最近敌人 → 发射弹射波 → 链式传递
        // Phase 2 占位：先打出一次幻影攻击作为基础效果
        bool hit = AttackSystem.Instance.ExecuteChainBounce(
            _lastAttackType, _lastTargetColumn,
            state.damageRatio, state.maxBounces);

        OnPassiveTriggered?.Invoke(state.definition.upgradeId);
        Debug.Log($"[PassiveTriggerModule] 连锁弹射触发: {state.definition.displayName} col={_lastTargetColumn} ratio={state.damageRatio} maxBounces={state.maxBounces} hit={hit}");
    }

    /// <summary>注销被动升级</summary>
    public void Unregister(string upgradeId)
    {
        _states.Remove(upgradeId);
    }

    /// <summary>重置所有被动状态（新对局）</summary>
    public void ResetAll()
    {
        _states.Clear();
    }
}
