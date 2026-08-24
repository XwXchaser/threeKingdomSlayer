using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRouteStageConfigV2", menuName = "一夫当关/路线关卡配置 V2")]
public sealed class RouteStageConfigV2 : ScriptableObject
{
    public int stageId;
    public string stageName = "路线关卡";
    public string routeSceneName;
    public RouteNodeConfigV2 startNode;
    public List<RouteNodeConfigV2> combatNodes = new List<RouteNodeConfigV2>();
    public int clearCoinReward = 100;
}

[CreateAssetMenu(fileName = "NewRouteNodeConfigV2", menuName = "一夫当关/路线节点配置 V2")]
public sealed class RouteNodeConfigV2 : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public List<RouteBattleEntryV2> battleEntries = new List<RouteBattleEntryV2>();
    public bool isFinalNode;
    public bool savePoint;
    public List<RouteConnectionV2> outgoingConnections = new List<RouteConnectionV2>();
}

[Serializable]
public sealed class RouteBattleEntryV2
{
    public StageConfig battleConfig;
    public bool conditionEnabled;
}

[Serializable]
public sealed class RouteConnectionV2
{
    public string choiceSlot;
    public RouteNodeConfigV2 targetNode;
}
