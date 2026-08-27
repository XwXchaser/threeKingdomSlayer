using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteStageConfig", menuName = "一夫当关/假移动路线关卡配置")]
public sealed class FakeRouteStageConfig : ScriptableObject
{
    public string routeId;
    public int stageId;
    public int configurationVersion = 1;
    public string stageName = "假移动路线关卡";
    public FakeRouteNodeConfig startNode;
    public List<FakeRouteNodeConfig> nodes = new List<FakeRouteNodeConfig>();
    public int clearCoinReward = 100;
}

[CreateAssetMenu(fileName = "NewFakeRouteNodeConfig", menuName = "一夫当关/假移动路线节点配置")]
public sealed class FakeRouteNodeConfig : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public List<FakeRouteBattleEntry> battleEntries = new List<FakeRouteBattleEntry>();
    public bool isFinalNode;
    public bool savePoint;
    public FakeRoutePresentation battleBackground;
    public FakeRoutePresentation routeChoiceTransition;
    public FakeRoutePresentation routeChoiceBackground;
    public List<FakeRouteChoiceConfig> outgoingChoices = new List<FakeRouteChoiceConfig>();
}

[Serializable]
public sealed class FakeRouteBattleEntry
{
    public StageConfig battleConfig;
    public bool conditionEnabled;
}

[Serializable]
public sealed class FakeRouteChoiceConfig
{
    public string choiceId;
    public string displayName;
    public FakeRouteNodeConfig targetNode;
    public FakeRoutePresentation presentation;
    public float placeholderDuration = 1f;
}
