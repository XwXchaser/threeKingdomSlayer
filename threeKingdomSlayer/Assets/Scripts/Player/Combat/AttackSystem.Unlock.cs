using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

/// <summary>
    /// AttackSystem 的**解锁攻击注册表**：招式解锁状态的登记与查询。
    ///
    /// 按机制从 <c>AttackSystem.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 AttackSystem.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class AttackSystem : MonoBehaviour
{
    #region 解锁攻击注册表

    private Dictionary<string, AttackSkillConfig> _unlockedAttacks = new Dictionary<string, AttackSkillConfig>();
    private Dictionary<string, int> _unlockedAttackLevels = new Dictionary<string, int>();
    private Dictionary<string, float> _unlockedFloatValues = new Dictionary<string, float>();

    /// <summary>注册解锁的攻击技能（由 UnlockAttackExecutor 调用）</summary>
    public void RegisterUnlockedAttack(string unlockId, AttackSkillConfig config, int level, float floatValue)
    {
        _unlockedAttacks[unlockId] = config;
        _unlockedAttackLevels[unlockId] = level;
        _unlockedFloatValues[unlockId] = floatValue;
        Debug.Log($"[AttackSystem] 注册解锁攻击: {unlockId} Lv.{level} damage={config.damage} floatValue={floatValue}");
    }

    /// <summary>更新解锁攻击等级</summary>
    public void UpdateUnlockedAttackLevel(string unlockId, int level)
    {
        if (_unlockedAttackLevels.ContainsKey(unlockId))
            _unlockedAttackLevels[unlockId] = level;
    }

    /// <summary>尝试执行解锁攻击</summary>
    public bool TryExecuteUnlockedAttack(string unlockId, int targetColumn = -1)
    {
        if (playerState == null || playerState.stageState != StageState.InProgress) return false;
        if (!_unlockedAttacks.TryGetValue(unlockId, out var cfg)) return false;
        if (!_unlockedAttackLevels.TryGetValue(unlockId, out int level)) return false;

        // 解锁攻击伤害 = baseAttackConfig.damage + floatValue × (level - 1)
        float baseDmg = cfg.damage;
        float bonusPerLevel = _unlockedFloatValues.TryGetValue(unlockId, out float fv) ? fv : 0f;
        float finalDmg = (baseDmg + bonusPerLevel * (level - 1))
            * (UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f);

        List<Enemy> targets;
        if (targetColumn >= 0)
            targets = columnManager.GetEnemiesInRange(targetColumn, cfg.rangeRows);
        else
            targets = columnManager.GetAllEnemiesInRange(cfg.rangeRows);

        if (targets.Count > 0)
        {
            Vector3 wavePos = GetWavePosition(targets, targetColumn);
            AttackWave.Create(wavePos, cfg.damageType, finalDmg, targets, prefab: cfg.attackWavePrefab,
                hitReactionDirection: cfg.hitReactionDirection);
        }

        Debug.Log($"[AttackSystem] 解锁攻击 {unlockId} Lv.{level} 伤害:{finalDmg} 目标数:{targets.Count}");
        return targets.Count > 0;
    }

    /// <summary>获取解锁攻击的最终伤害值（供 UI 显示）</summary>
    public float GetUnlockedAttackDamage(string unlockId)
    {
        if (!_unlockedAttacks.TryGetValue(unlockId, out var cfg)) return 0f;
        if (!_unlockedAttackLevels.TryGetValue(unlockId, out int level)) return 0f;
        float bonusPerLevel = _unlockedFloatValues.TryGetValue(unlockId, out float fv2) ? fv2 : 0f;
        return cfg.damage + bonusPerLevel * (level - 1);
    }

    public void ResetUnlockedAttacks()
    {
        _unlockedAttacks.Clear();
        _unlockedAttackLevels.Clear();
        _unlockedFloatValues.Clear();
    }

    private float GetAttackDuration(AttackSkillConfig cfg)
    {
        if (cfg == null) return 0.5f;
        float speedMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetAttackSpeedMultiplier() : 1f;
        return cfg.actionDuration / Mathf.Max(speedMult, 0.01f);
    }

    /// <summary>
    /// 招架空放（没格挡到任何攻击）时的动作锁时长：比格挡成功长得多，让招架不能连发。
    /// 数值走招式资产（AttackSkillConfig.parryWhiffDuration，默认 0.8s），随攻速缩放。
    /// </summary>
    private float GetParryWhiffDuration(AttackSkillConfig cfg)
    {
        if (cfg == null) return 0.8f;
        float speedMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetAttackSpeedMultiplier() : 1f;
        return cfg.parryWhiffDuration / Mathf.Max(speedMult, 0.01f);
    }

    private float GetVisualTargetDuration(AttackSkillConfig cfg)
    {
        if (!useActionBasedCooldown || cfg == null) return -1f;
        float speedMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetAttackSpeedMultiplier() : 1f;
        return cfg.cooldown / Mathf.Max(speedMult, 0.01f);
    }

    #endregion
}