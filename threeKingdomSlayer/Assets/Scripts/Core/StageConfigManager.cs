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
