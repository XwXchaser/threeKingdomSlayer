using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **路线清单**（2026-10-23，配置语义拆分）：与「线性关卡清单」分开。
///
/// 背景：<c>StageConfigManager</c> 一个类里同时装着两种东西 ——
///   ① <c>stages</c>：线性关卡（走 StageConfig 的关卡表 ✅）
///   ② <c>fakeRouteStages</c> + <c>tutorialStage</c>：卷轴路线配置 ✅
/// 两者消费方不同（选关页 / 路线运行器 ✅）、解锁规则也不同 ✅，混在一起会越滚越大 ✅。
///
/// 拆开之后：
///   • <c>StageConfigManager</c> 只管线性关卡 ✅；
///   • 本类只管路线清单 ✅，并提供「玩家能选的路线 = 教学关 + 正式路线」✅。
///
/// 本类是**纯数据持有者**（不跑流程 ✅）→ 住在流程层 ✅。
/// </summary>
public class RouteCatalog : MonoBehaviour
{
    public static RouteCatalog Instance { get; private set; }

    [Tooltip("卷轴路线关卡配置列表。每个配置对应一条纯逻辑路线（卷轴表现）")]
    public List<FakeRouteStageConfig> fakeRouteStages = new List<FakeRouteStageConfig>();

    [Tooltip("教学关路线配置（教学路线，不混进 fakeRouteStages——那个列表的第 0 项被当“正式关”用）")]
    public FakeRouteStageConfig tutorialStage;

    [Tooltip("**关卡清单资产** ✅ —— 选关列表 ✅ + 解锁关系 ✅ 都在里面 ✅。\n"
           + "留空 = 退回旧行为 ✅（用下面的列表 ✅ + 按位置解锁 ✅）。\n"
           + "有了它 ✅ → **加关卡 = 改这个资产 ✅ ✅**（不用改场景 ✅ ✅）。")]
    public RouteCatalogData catalogData;

    /// <summary>
    /// 取当前场景的路线清单（优先单例 ✅，找不到就扫场景 ✅）。
    /// </summary>
    public static RouteCatalog Resolve()
    {
        if (Instance != null) return Instance;
        return FindObjectOfType<RouteCatalog>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 玩家在「选关页」里可选的全部路线 ✅。
    /// 优先用**清单资产** ✅（`catalogData` ✅ —— 解锁关系也在里面 ✅ ✅）；
    /// 没有资产 ✅ → 退回旧的「教学关 + 列表 ✅」（解锁走旧规则 ✅ ✅）。
    /// </summary>
    public List<FakeRouteStageConfig> GetSelectableRoutes()
    {
        var result = new List<FakeRouteStageConfig>();
        if (catalogData != null)
        {
            var fromAsset = catalogData.GetStages();
            if (fromAsset.Count > 0) return fromAsset;
        }
        if (tutorialStage != null) result.Add(tutorialStage);
        for (int i = 0; i < fakeRouteStages.Count; i++)
            if (fakeRouteStages[i] != null) result.Add(fakeRouteStages[i]);
        return result;
    }
}
