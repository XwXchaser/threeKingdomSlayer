using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFakeRouteStageConfig", menuName = "一夫当关/假移动路线关卡")]
public sealed class FakeRouteStageConfig : ScriptableObject
{
    public string routeId;
    public int stageId;
    public int configurationVersion = 1;
    public string stageName = "假移动路线关卡";
    [Tooltip("开场表现；留空表示不播开场，直接进入 startNode。")]
    public FakeRouteTravelPresentation openingTravel;
    public FakeRouteNodeConfig startNode;
    public List<FakeRouteNodeConfig> nodes = new List<FakeRouteNodeConfig>();
    public int clearCoinReward = 100;

    [Header("解锁条件")]
    [Tooltip("**前置关卡的 stageId 列表** ✅（可多个 ✅ → 支持分叉 ✅）。\n"
           + "空 = 不设前置 ✅（是否默认可选由**清单里的「默认可选关卡」**决定 ✅ ✅）。\n"
           + "注意：解锁关系写在**数据**里 ✅ → 中间插一条关卡不会连锁改 ✅ ✅ ✅。")]
    public List<int> prerequisiteStageIds = new List<int>();
}
