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

    // ── 已应用升级追踪 (upgradeId → level) ──
    private Dictionary<string, int> _appliedUpgrades = new Dictionary<string, int>();

    // ── 行为执行器注册表 (effectType → executor) ──
    private Dictionary<string, IEffectExecutor> _executors = new Dictionary<string, IEffectExecutor>();

    // ── 升级事件 ──
    public System.Action<UpgradeDefinition, int> OnUpgradeApplied; // (def, newLevel)

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
            if (ItemInventory.Instance != null)
                ItemInventory.Instance.AddItem(def);
            else
                Debug.LogWarning("[UpgradeEffectManager] ItemInventory 未找到，道具无法添加");

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

    /// <summary>叠加伤害倍率（供道具型测试等直接调用）</summary>
    public void AddDamageBonus(float amount)
    {
        _damageMultiplier += amount;
        Debug.Log($"[UpgradeEffectManager] 伤害加成 +{amount:P0} → 当前倍率 {_damageMultiplier:P0}");
    }
    public float GetAttackSpeedMultiplier() => _attackSpeedMultiplier;
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
                        ? cfg.intervalSeconds.ToString("F1") + "秒"
                        : cfg.triggerThreshold + "次攻击";
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
                        ? cfg.intervalSeconds.ToString("F1") + "秒"
                        : cfg.triggerThreshold + "次攻击";
                    desc = desc.Replace("{0}", triggerStr);
                    desc = desc.Replace("{1}", cfg.rowCount.ToString());
                    desc = desc.Replace("{2}", cfg.arrowCount.ToString());
                    desc = desc.Replace("{3}", cfg.damage.ToString());
                }
            }
            else if (def.effectType == "passive_timed_cyclone")
            {
                if (def.cycloneLevels != null && nextLevel <= def.cycloneLevels.Count)
                {
                    var cfg = def.cycloneLevels[nextLevel - 1];
                    desc = desc.Replace("{0}", cfg.intervalSeconds.ToString("F1") + "秒");
                    desc = desc.Replace("{1}", cfg.enemyCount.ToString());
                    desc = desc.Replace("{2}", cfg.knockupDuration.ToString("F1") + "秒");
                }
            }
            else if (def.effectType == "passive_return_wave")
            {
                var cfg = (def.returnWaveLevels != null && nextLevel <= def.returnWaveLevels.Count)
                    ? def.returnWaveLevels[nextLevel - 1]
                    : new ReturnWaveLevelConfig { triggerThreshold = def.intValue, damageRatio = def.floatValue };
                string triggerStr = isTimed
                    ? cfg.intervalSeconds.ToString("F1") + "秒"
                    : cfg.triggerThreshold + "次攻击";
                desc = desc.Replace("{0}", triggerStr);
                desc = desc.Replace("{1}", (cfg.damageRatio * 100f).ToString("F0"));
            }
            else if (def.effectType == "passive_chain_bounce")
            {
                var cfg = (def.chainBounceLevels != null && nextLevel <= def.chainBounceLevels.Count)
                    ? def.chainBounceLevels[nextLevel - 1]
                    : new ChainBounceLevelConfig { triggerThreshold = def.intValue, maxBounces = def.secondaryIntValue, damageRatio = def.floatValue };
                string triggerStr = isTimed
                    ? cfg.intervalSeconds.ToString("F1") + "秒"
                    : cfg.triggerThreshold + "次攻击";
                desc = desc.Replace("{0}", triggerStr);
                desc = desc.Replace("{1}", cfg.maxBounces.ToString());
                desc = desc.Replace("{2}", (cfg.damageRatio * 100f).ToString("F0"));
            }
            else
            {
                def.GetPhantomConfig(nextLevel, out int triggerParam, out var steps);
                float interval = def.phantomLevels != null && nextLevel <= def.phantomLevels.Count
                    ? def.phantomLevels[nextLevel - 1].intervalSeconds : -1f;
                string triggerStr = isTimed && interval > 0f
                    ? interval.ToString("F1") + "秒"
                    : triggerParam + "次攻击";
                desc = desc.Replace("{0}", triggerStr);
                desc = desc.Replace("{1}", (steps?.Count ?? 0).ToString());
                if (steps != null && steps.Count > 0)
                    desc = desc.Replace("{2}", (steps[0].damageRatio * 100f).ToString("F0"));
            }
        }
        else if (def.effectType == "stab_range_boost" || def.effectType == "sweep_range_boost")
        {
            desc = desc.Replace("{0}", (def.intValue * nextLevel).ToString());
            desc = desc.Replace("{1}", (def.secondaryIntValue * nextLevel).ToString());
        }
        else if (def.effectType == "push_wave" || def.effectType == "convergence_wave")
        {
            // {0}=intValue*level (格数/排数), {1}=floatValue*100 (百分比)
            desc = desc.Replace("{0}", (def.intValue * nextLevel).ToString());
            desc = desc.Replace("{1}", (def.floatValue * 100f).ToString("F0"));
        }
        else
        {
            float nextValue = def.floatValue * nextLevel;
            int nextIntValue = def.intValue * nextLevel;
            desc = desc.Replace("{0}", (nextValue * 100f).ToString("F0"));
            desc = desc.Replace("{1}", nextIntValue.ToString());
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

        // 清空被动攻击模块
        PassiveTriggerModule.Instance?.ResetAll();
        TimedPassiveModule.Instance?.ResetAll();

        // 清空道具库存
        ItemInventory.Instance?.ClearAll();
    }

    // ── 内部 ──

    private void ApplyNumericEffect(UpgradeDefinition def, int level)
    {
        switch (def.effectType)
        {
            case "damage_multiplier":
                _damageMultiplier += def.floatValue;
                break;
            case "attack_speed":
                _attackSpeedMultiplier += def.floatValue;
                break;
            case "move_speed":
                _moveSpeedMultiplier += def.floatValue;
                break;
            case "exp_multiplier":
                _expMultiplier += def.floatValue;
                break;
            case "stab_range_boost":
                _stabRangeBonus += def.intValue;
                _stabDamagePenalty += def.secondaryIntValue * 0.01f;
                break;
            case "sweep_range_boost":
                _sweepRangeBonus += def.intValue;
                _sweepDamagePenalty += def.secondaryIntValue * 0.01f;
                break;
            case "push_wave":
                _pushWaveDistance += def.intValue;
                break;
            case "convergence_wave":
                _convergenceStep += def.intValue;
                if (def.floatValue > 0f) _convergenceDamagePercent = def.floatValue;
                break;
            case "unlock_attack":
                // 数值叠加由注册的 UnlockAttackExecutor 处理
                break;
        }
    }
}
