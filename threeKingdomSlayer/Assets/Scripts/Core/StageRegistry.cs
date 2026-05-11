using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡注册表 — ScriptableObject，存放所有关卡配置的有序列表
/// 放在 Resources 下，运行时通过 StageRegistry.Instance 访问
/// </summary>
[CreateAssetMenu(fileName = "StageRegistry", menuName = "一夫当关/关卡注册表")]
public class StageRegistry : ScriptableObject
{
    private static StageRegistry _instance;

    public static StageRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<StageRegistry>("StageRegistry");
            return _instance;
        }
    }

    [Tooltip("按顺序排列的关卡配置列表，顺序决定关卡解锁顺序")]
    public List<StageConfig> stages = new List<StageConfig>();

    /// <summary>
    /// 按 stageId 查找关卡配置
    /// </summary>
    public StageConfig GetStageById(int stageId)
    {
        foreach (var s in stages)
            if (s != null && s.stageId == stageId)
                return s;
        return null;
    }

    /// <summary>
    /// 获取所有关卡配置（按列表顺序）
    /// </summary>
    public List<StageConfig> GetAllStages()
    {
        return stages;
    }
}
