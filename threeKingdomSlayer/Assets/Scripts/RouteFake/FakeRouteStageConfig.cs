using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteStageConfig", menuName = "一夫当关/假移动路线关卡")]
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
