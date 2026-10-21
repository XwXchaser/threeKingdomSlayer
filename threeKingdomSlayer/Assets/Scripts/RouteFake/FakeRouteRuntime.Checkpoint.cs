using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
    /// FakeRouteRuntime 的**检查点**：写快照、读快照、选择历史校验、升级/主动技能/道具的恢复。
    ///
    /// 按机制从 <c>FakeRouteRuntime.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 FakeRouteRuntime.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class FakeRouteRuntime : MonoBehaviour, IPropDestroyNotifier
{
    public void SaveCheckpoint()
    {
        if (routeConfig == null || _currentNode == null || PlayerState.Instance == null) return;
        var snapshot = new FakeRouteStageSaveSnapshot
        {
            snapshotVersion = 1,
            routeArchitectureId = "fake-route-v1",
            routeId = routeConfig.routeId,
            stageId = routeConfig.stageId,
            configurationVersion = routeConfig.configurationVersion,
            checkpointNodeId = _currentNode.nodeId,
            currentHealth = PlayerState.Instance.currentHealth,
            maxHealthAtSave = PlayerState.Instance.heroConfig != null ? PlayerState.Instance.heroConfig.maxHealth : 100f,
            currentRevives = PlayerState.Instance.currentRevives,
            currentLevel = PlayerState.Instance.currentLevel,
            currentExp = PlayerState.Instance.currentExp,
            killCount = PlayerState.Instance.killCount,
            coinCount = PlayerState.Instance.coinCount,
            ultimateEnergy = UltimateSystem.Instance != null ? UltimateSystem.Instance.CurrentEnergy : 0
        };
        snapshot.choiceHistory.Clear();
        snapshot.choiceHistory.AddRange(_choiceHistory);
        // 路线推进记录：到过的节点 + 每个节点已完成的 BattleEntry。
        snapshot.visitedNodeIds.Clear();
        snapshot.completedNodeIds.Clear();
        foreach (var node in _visitedNodes)
        {
            if (node == null) continue;
            if (!snapshot.visitedNodeIds.Contains(node.nodeId)) snapshot.visitedNodeIds.Add(node.nodeId);
            if (IsNodeContentCompleted(node) && !snapshot.completedNodeIds.Contains(node.nodeId))
                snapshot.completedNodeIds.Add(node.nodeId);
            if (!_completedBattleEntries.TryGetValue(node, out var completed) || completed.Count == 0) continue;
            var state = new FakeRouteNodeBattleSaveState { nodeId = node.nodeId };
            state.completedEntryIndices.AddRange(completed);
            snapshot.nodeStates.Add(state);
        }
        foreach (var upgrade in PlayerState.Instance.acquiredUpgrades)
        {
            if (upgrade != null && upgrade.definition != null)
                snapshot.upgrades.Add(new RouteUpgradeSaveState { upgradeId = upgrade.definition.upgradeId, level = upgrade.currentLevel });
        }
        if (ActiveSkillInventory.Instance != null)
        {
            foreach (var entry in ActiveSkillInventory.Instance.Entries)
            {
                if (entry != null && entry.definition != null)
                    snapshot.activeSkills.Add(new RouteActiveSkillSaveState { upgradeId = entry.definition.upgradeId, level = entry.level });
            }
        }
        snapshot.propStates.Clear();
        snapshot.propStates.AddRange(BreakableProp.ExportStates());
        SaveManager.SaveFakeRouteSnapshot(snapshot);
        SaveManager.SetActiveFakeRouteStage(routeConfig.stageId);
        Debug.Log("[FakeRoute] checkpoint saved node=" + _currentNode.nodeId);
    }

    /// <summary>
    /// 道具被破坏后就地更新已存在的快照（只改道具记录，不改变检查点节点）。
    /// 没有快照时不创建，避免把未开始的节点当成检查点。
    /// </summary>
    public void NotifyPropDestroyed()
    {
        if (routeConfig == null) return;
        var snapshot = SaveManager.GetFakeRouteSnapshot(routeConfig.routeId, routeConfig.stageId);
        if (snapshot == null) return;
        snapshot.propStates = new List<PropSaveEntry>(BreakableProp.ExportStates());
        SaveManager.SaveFakeRouteSnapshot(snapshot);
    }

    private bool RestoreCheckpoint(FakeRouteStageSaveSnapshot snapshot, out FakeRouteNodeConfig restoredNode)
    {
        restoredNode = FindNodeById(snapshot != null ? snapshot.checkpointNodeId : null);
        if (snapshot == null || restoredNode == null || snapshot.snapshotVersion != 1 || snapshot.routeArchitectureId != "fake-route-v1" || snapshot.routeId != routeConfig.routeId || snapshot.stageId != routeConfig.stageId || snapshot.configurationVersion != routeConfig.configurationVersion)
            return false;

        var player = PlayerState.Instance;
        if (player == null) return false;
        // 血量按**比例**恢复：快照存的是绝对值 ✅，换英雄 / 改配置后绝对值会失真 ✅
        // （曾经出现过「快照血量 9999 / 最大血量 100」这种脏数据 ✅ —— 直接赋值会把血条撑爆 ✅）。
        float maxHealthNow = player.heroConfig != null ? player.heroConfig.maxHealth : 100f;
        float maxHealthAtSave = snapshot.maxHealthAtSave > 0f ? snapshot.maxHealthAtSave : maxHealthNow;
        float restoredHealth = maxHealthNow * (snapshot.currentHealth / maxHealthAtSave);
        player.currentHealth = Mathf.Clamp(restoredHealth, 0f, maxHealthNow);
        player.currentRevives = snapshot.currentRevives;
        player.currentLevel = snapshot.currentLevel;
        player.currentExp = snapshot.currentExp;
        player.killCount = snapshot.killCount;
        player.coinCount = snapshot.coinCount;
        BreakableProp.ImportStates(snapshot.propStates);
        player.acquiredUpgrades.Clear();
        _completedBattleEntries.Clear();
        _visitedNodes.Clear();
        _choiceHistory.Clear();
        if (snapshot.choiceHistory != null)
            _choiceHistory.AddRange(snapshot.choiceHistory);
        if (snapshot.visitedNodeIds != null)
        {
            for (int i = 0; i < snapshot.visitedNodeIds.Count; i++)
            {
                var node = FindNodeById(snapshot.visitedNodeIds[i]);
                if (node != null) _visitedNodes.Add(node);
            }
        }
        if (snapshot.nodeStates != null)
        {
            for (int i = 0; i < snapshot.nodeStates.Count; i++)
            {
                var savedNode = snapshot.nodeStates[i];
                var node = FindNodeById(savedNode != null ? savedNode.nodeId : null);
                if (node == null) continue;
                _visitedNodes.Add(node);
                if (savedNode.completedEntryIndices != null)
                    _completedBattleEntries[node] = new HashSet<int>(savedNode.completedEntryIndices);
            }
        }
        if (!ValidateChoiceHistory(snapshot.choiceHistory)) return false;
        TimedPassiveModule.Instance?.SetSuppressImmediateEffects(true);
        if (snapshot.upgrades != null && UpgradeEffectManager.Instance != null)
        {
            for (int i = 0; i < snapshot.upgrades.Count; i++)
            {
                var savedUpgrade = snapshot.upgrades[i];
                var definition = FindUpgradeDefinition(savedUpgrade.upgradeId);
                if (definition == null) continue;
                for (int level = 0; level < savedUpgrade.level; level++)
                    UpgradeEffectManager.Instance.ApplyUpgrade(definition);
            }
        }
        if (snapshot.activeSkills != null && ActiveSkillInventory.Instance != null)
        {
            for (int i = 0; i < snapshot.activeSkills.Count; i++)
            {
                var savedSkill = snapshot.activeSkills[i];
                var definition = FindActiveSkillDefinition(savedSkill.upgradeId);
                if (definition == null) continue;
                for (int level = 0; level < savedSkill.level; level++)
                    ActiveSkillInventory.Instance.AcquireOrUpgrade(definition, out _);
            }
            ActiveSkillInventory.Instance.ResetCooldowns();
        }
        // 快照恢复会先 ResetAll() 清空库存；恢复完成后再补一次默认赠送技能（幂等）。
        ActiveSkillInventory.Instance?.EnsureDefaultSkills();
        TimedPassiveModule.Instance?.SetSuppressImmediateEffects(false);
        if (UltimateSystem.Instance != null)
            UltimateSystem.Instance.SetEnergyForRouteRestore(snapshot.ultimateEnergy);
        float maxHealth = player.heroConfig != null ? player.heroConfig.maxHealth : 100f;
        int requiredExp = player.GetExpRequiredForNextLevel();
        player.OnHealthChanged?.Invoke(player.currentHealth, maxHealth);
        player.OnReviveCountChanged?.Invoke(player.currentRevives);
        player.OnLevelChanged?.Invoke(player.currentLevel);
        player.OnExpChanged?.Invoke(player.currentExp, requiredExp > 0 ? requiredExp : player.currentExp);
        return true;
    }

    private FakeRouteNodeConfig FindNodeById(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || routeConfig == null || routeConfig.nodes == null) return null;
        for (int i = 0; i < routeConfig.nodes.Count; i++)
            if (routeConfig.nodes[i] != null && routeConfig.nodes[i].nodeId == nodeId)
                return routeConfig.nodes[i];
        return null;
    }

    private bool ValidateChoiceHistory(List<FakeRouteChoiceSaveState> history)
    {
        if (history == null) return true;
        for (int i = 0; i < history.Count; i++)
        {
            var item = history[i];
            var source = FindNodeById(item != null ? item.sourceNodeId : null);
            var target = FindNodeById(item != null ? item.targetNodeId : null);
            if (source == null || target == null || source.outgoingChoices == null) return false;
            bool matched = false;
            for (int c = 0; c < source.outgoingChoices.Count; c++)
            {
                var choice = source.outgoingChoices[c];
                if (choice != null && choice.choiceId == item.choiceId && choice.targetNode == target) { matched = true; break; }
            }
            if (!matched) return false;
        }
        return true;
    }

    private ActiveSkillDefinition FindActiveSkillDefinition(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;
        var all = Resources.FindObjectsOfTypeAll<ActiveSkillDefinition>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].upgradeId == upgradeId)
                return all[i];
        return null;
    }

    private UpgradeDefinition FindUpgradeDefinition(string upgradeId)
    {
        if (string.IsNullOrEmpty(upgradeId)) return null;
        var all = Resources.FindObjectsOfTypeAll<UpgradeDefinition>();
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].upgradeId == upgradeId)
                return all[i];
        return null;
    }
}