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
