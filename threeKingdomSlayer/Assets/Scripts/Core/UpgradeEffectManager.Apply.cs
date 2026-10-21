using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
    /// UpgradeEffectManager 的**升级应用与派发**：ApplyUpgrade（按 category 路由）、行为型执行器注册表、
    /// 数值型效果应用、玩家状态同步。
    ///
    /// 按机制从 <c>UpgradeEffectManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 UpgradeEffectManager.cs **同类型**，行为完全一致。
    /// 扩展点：新的局内升级机制走「注册表」，不要往 ApplyUpgrade 的分派里加分支。
    /// </summary>

public partial class UpgradeEffectManager : MonoBehaviour, IStatModifierApplier
{
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

    // ── 内部 ──

    private void ApplyNumericEffect(UpgradeDefinition def, int level)
        {
            var cfg = def.GetNumericConfig(level);
    
            // 扩展点：数值型效果走注册表；未注册的 effectType 不做任何处理
            //（与改造前 switch 无匹配分支的语义一致）。
            if (_numericAppliers.TryGetValue(def.effectType, out var applier))
                applier.Apply(def, level, cfg);
        }
    
        /// <summary>注册内置的默认应用器（数值型 effectType + 描述兼底）。</summary>
        private void RegisterDefaultAppliers()
        {
            var defaultNumeric = new DefaultUpgradeNumericApplier(this);
            for (int i = 0; i < DefaultUpgradeNumericApplier.EffectTypes.Length; i++)
                _numericAppliers[DefaultUpgradeNumericApplier.EffectTypes[i]] = defaultNumeric;
    
            _defaultDescriptionFormatter = new DefaultUpgradeDescriptionFormatter();
        }
    
        // ── IUpgradeStatSink 实现（数值型升级通过它写玩家数值）──
    
        void IUpgradeStatSink.AddDamageMultiplier(float value) => _damageMultiplier += value;
    
        void IUpgradeStatSink.AddAttackSpeedMultiplier(float value) => _attackSpeedMultiplier += value;
    
        void IUpgradeStatSink.AddMoveSpeedMultiplier(float value) => _moveSpeedMultiplier += value;
    
        void IUpgradeStatSink.AddExpMultiplier(float value) => _expMultiplier += value;
    
        void IUpgradeStatSink.AddItemDropRateBonus(float value) => _itemDropRateBonus += value;
    
        void IUpgradeStatSink.AddItemDamageBonus(float value) => _itemDamageBonus += value;
    
        void IUpgradeStatSink.AddAttackRangeBonus(int value) => _attackRangeBonus += value;
    
        void IUpgradeStatSink.AddAttackDamagePenalty(float value) => _attackDamagePenalty += value;
    
        void IUpgradeStatSink.SetAttackRangeBonus(int value) => _attackRangeBonus = value;
    
        void IUpgradeStatSink.SetAttackDamagePenalty(float value) => _attackDamagePenalty = value;
    
        void IUpgradeStatSink.AddPushWaveDistance(int value) => _pushWaveDistance += value;
    
        void IUpgradeStatSink.AddConvergenceStep(int value) => _convergenceStep += value;
    
        void IUpgradeStatSink.SetConvergenceDamagePercent(float value) => _convergenceDamagePercent = value;
    
        void IUpgradeStatSink.AddChargeDamageReduction(float value) => _chargeDamageReduction += value;
    
        void IUpgradeStatSink.AddActiveSkillCDReduction(float value) => _activeSkillCDReduction += value;
    
        void IUpgradeStatSink.ApplyReflectShieldLevel(UpgradeDefinition def, int level)
        {
            if (def.reflectShieldLevels == null || level > def.reflectShieldLevels.Count) return;
            var rsCfg = def.reflectShieldLevels[level - 1];
            // 护盾是玩家侧状态：写入玩家状态容器里的护盾状态。
            var shield = ResolveReflectShieldStatus();
            if (shield == null) return;
            float bonusPercent = rsCfg.enableBonus ? rsCfg.bonusReflectPercent / 100f : 0f;
            shield.Configure(rsCfg.shieldAmount, rsCfg.intervalSeconds, bonusPercent);
        }

        /// <summary>取玩家侧的反伤护盾状态（没有则建一个并登记到玩家状态容器）。</summary>
        private ReflectShieldStatus ResolveReflectShieldStatus()
        {
            var player = PlayerState.Instance;
            if (player == null) return null;
            var status = player.StatusHost.Get(ReflectShieldStatus.Id) as ReflectShieldStatus;
            if (status == null)
            {
                status = new ReflectShieldStatus();
                status.OnConsumed += () => OnReflectShieldConsumed?.Invoke();
                player.StatusHost.Add(status);
            }
            return status;
        }
}