using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡配置管理器 — 挂载在 MainMenu 场景的 GameObject 上，Inspector 中拖入 StageConfig 并排序
/// 列表顺序决定关卡解锁顺序。此为关卡配置的唯一来源，不再自动扫描 Resources 文件夹
/// </summary>
public class StageConfigManager : MonoBehaviour
{
    public static StageConfigManager Instance { get; private set; }

    [Tooltip("关卡配置列表（按 Inspector 顺序排列）。拖入 StageConfig 资产并排序")]
    public List<StageConfig> stages = new List<StageConfig>();

    [Tooltip("V2场景化路线关卡配置列表。每个配置对应一张RouteStage场景")]
    public List<RouteStageConfigV2> routeStagesV2 = new List<RouteStageConfigV2>();

    [Tooltip("假移动/卷轴路线关卡配置列表。每个配置对应一条纯逻辑路线（卷轴表现）")]
    public List<FakeRouteStageConfig> fakeRouteStages = new List<FakeRouteStageConfig>();

    [Tooltip("教学关路线配置（教学路线，不混进 fakeRouteStages——那个列表的第 0 项被当“正式关”用）")]
    public FakeRouteStageConfig tutorialStage;

    /// <summary>
    /// 玩家在「选关页」里可选的全部路线：教学关 + 正式路线。
    /// 选关页只列这个，不列节点（点哪条都从该路线的 startNode 开始）。
    /// </summary>
    public List<FakeRouteStageConfig> GetSelectableRoutes()
    {
        var result = new List<FakeRouteStageConfig>();
        if (tutorialStage != null) result.Add(tutorialStage);
        for (int i = 0; i < fakeRouteStages.Count; i++)
            if (fakeRouteStages[i] != null) result.Add(fakeRouteStages[i]);
        return result;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Android 默认 30 FPS，显式设为目标帧率
        Application.targetFrameRate = 60;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public List<StageConfig> GetStages() => stages;

    public StageConfig GetStageById(int stageId)
    {
        foreach (var s in stages)
            if (s != null && s.stageId == stageId)
                return s;
        return null;
    }
}
