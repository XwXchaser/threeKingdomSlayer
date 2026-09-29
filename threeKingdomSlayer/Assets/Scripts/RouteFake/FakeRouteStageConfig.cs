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
    public FakeRoutePresentation openingPresentation;
    public FakeRouteNodeConfig startNode;
    public List<FakeRouteNodeConfig> nodes = new List<FakeRouteNodeConfig>();
    public int clearCoinReward = 100;
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
    public RouteChoiceLayout layout = new RouteChoiceLayout();
    public FakeRoutePresentation presentation;
    public float placeholderDuration = 1f;
}
