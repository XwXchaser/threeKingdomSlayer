using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 被动攻击型模块 — 单例
///
/// 监听 AttackSystem.OnAttackPerformed，为每个已注册的被动升级维护独立计数器。
/// 计数器到达阈值时，依次执行 upgradeDef.phantomSteps 中配置的幻影攻击。
///
/// 框架支持多段幻影：phantomSteps 可包含多个 PhantomStep，依次释放（为后续等级扩展预留）。
/// 当前 Lv1 配置为单段：damageRatio=0.3, alpha=0.6。
/// </summary>
public class PassiveTriggerModule : MonoBehaviour
{
    public static PassiveTriggerModule Instance { get; private set; }

    private class PassiveState
    {
        public UpgradeDefinition definition;
        public int currentCount;
        public int threshold;
        public List<PhantomStep> phantomSteps;
    }

    private Dictionary<string, PassiveState> _states = new Dictionary<string, PassiveState>();

    // 攻击上下文 — 由 OnAttackPerformed 暂存，供幻影执行时使用
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
                ExecutePhantoms(state);
            }
        }
    }

    private void ExecutePhantoms(PassiveState state)
    {
        if (AttackSystem.Instance == null)
        {
            Debug.LogWarning("[PassiveTriggerModule] AttackSystem.Instance is null, 无法执行幻影");
            return;
        }
        if (state.phantomSteps == null || state.phantomSteps.Count == 0)
        {
            Debug.LogWarning($"[PassiveTriggerModule] {state.definition.displayName} phantomSteps 为空");
            return;
        }

        foreach (var step in state.phantomSteps)
        {
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
    public void Register(UpgradeDefinition def)
    {
        if (def == null) return;

        if (_states.TryGetValue(def.upgradeId, out var existing))
        {
            // 已存在则更新阈值和幻影配置（等级提升）
            existing.threshold = def.triggerParam;
            existing.phantomSteps = def.phantomSteps;
            existing.definition = def;
        }
        else
        {
            _states[def.upgradeId] = new PassiveState
            {
                definition = def,
                currentCount = 0,
                threshold = def.triggerParam,
                phantomSteps = def.phantomSteps
            };
        }

        OnPassiveRegistered?.Invoke(def.upgradeId, def.triggerParam);
        Debug.Log($"[PassiveTriggerModule] 注册被动: {def.displayName} threshold={def.triggerParam} steps={def.phantomSteps?.Count ?? 0}");
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
