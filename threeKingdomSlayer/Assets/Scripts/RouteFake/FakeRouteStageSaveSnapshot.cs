using System;
using System.Collections.Generic;

[Serializable]
public sealed class FakeRouteStageSaveSnapshot
{
    public int snapshotVersion = 1;
    public string routeArchitectureId = "fake-route-v1";
    public string routeId;
    public int stageId;
    public int configurationVersion;
    public string checkpointNodeId;
    public int ultimateEnergy;
    public int killCount;
    public int coinCount;
    public List<FakeRouteChoiceSaveState> choiceHistory = new List<FakeRouteChoiceSaveState>();
    public List<string> visitedNodeIds = new List<string>();
    public List<string> completedNodeIds = new List<string>();
    public List<string> routeChoiceHistory = new List<string>();
    public List<FakeRouteNodeBattleSaveState> nodeStates = new List<FakeRouteNodeBattleSaveState>();
    public float currentHealth;
    public int currentRevives;
    public int currentLevel;
    public float currentExp;
    public List<RouteUpgradeSaveState> upgrades = new List<RouteUpgradeSaveState>();
    public List<RouteActiveSkillSaveState> activeSkills = new List<RouteActiveSkillSaveState>();
    public List<RouteItemSaveState> items = new List<RouteItemSaveState>();
    // 可破坏补给：已破坏状态 + 已获取内容（旧档缺失时为 null，按“全部未破坏”处理）
    public List<BreakableProp.PropSaveEntry> propStates = new List<BreakableProp.PropSaveEntry>();
}

[Serializable]
public sealed class FakeRouteNodeBattleSaveState
{
    public string nodeId;
    public List<int> completedEntryIndices = new List<int>();
}

[Serializable]
public sealed class RouteActiveSkillSaveState
{
    public string upgradeId;
    public int level;
}

[Serializable]
public sealed class RouteItemSaveState
{
    public string gestureId;
    public string definitionId;
    public int remainingUses;
    public bool isPotion;
}

/// <summary>一条路线推进记录：从哪个节点、选了哪个出口、到了哪个节点。</summary>
[Serializable]
public sealed class FakeRouteChoiceSaveState
{
    public string sourceNodeId;
    public string choiceId;
    public string targetNodeId;
}
