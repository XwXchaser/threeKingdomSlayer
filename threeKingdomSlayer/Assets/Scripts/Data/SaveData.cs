using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档数据 — JSON序列化到 Application.persistentDataPath
/// 道具系统：props 列表管理所有道具（铜钱/钥匙等），coinCount 保留向后兼容
///
/// 2026-10-22（asmdef 分层第二批）：从 <c>Assets/Scripts/Core/SaveManager.cs</c> 拆到数据层
/// —— 存档**数据模型**是纯数据（序列化用），服务（<c>SaveManager</c>）才是行为；
/// 两者分开后，服务层不依赖任何战斗 / 流程代码。
/// </summary>
[Serializable]
public class SaveData
{
    public List<int> clearedStageIds = new List<int>();
    public List<PropData> props = new List<PropData>();
    [Obsolete("使用 props.GetPropAmount(PropType.Coin) 替代")]
    public int coinCount;
    public bool tutorialCompleted;
    public List<string> completedTutorialDialogueIds = new List<string>();
    public List<RouteStageSaveSnapshot> routeStageSnapshots = new List<RouteStageSaveSnapshot>();
    public List<FakeRouteStageSaveSnapshot> fakeRouteSnapshots = new List<FakeRouteStageSaveSnapshot>();
    public int activeFakeRouteStageId = -1;
    [Tooltip("已解锁的天赋节点 id（TalentNodeConfig.nodeId）。旧档缺失时 MigrateIfNeeded 会补空列表")]
    public List<string> unlockedTalents = new List<string>();

    /// <summary>
    /// 获取铜钱数量（优先从 props 列表读取，兼容旧存档的 coinCount 字段）
    /// </summary>
    public int GetCoinCount()
    {
        int fromProps = props.GetPropAmount(PropType.Coin);
        return fromProps > 0 || props.Count > 0 ? fromProps : coinCount;
    }

    /// <summary>
    /// 迁移旧存档：将过时的 coinCount 字段合并到 props 列表
    /// </summary>
    public void MigrateIfNeeded()
    {
        if (coinCount > 0 && props.GetPropAmount(PropType.Coin) == 0)
        {
            props.SetPropAmount(PropType.Coin, coinCount);
            coinCount = 0;
        }
        // 老档没有天赋字段，JsonUtility 会给 null，必须补上否则所有天赋查询空引用
        if (unlockedTalents == null) unlockedTalents = new List<string>();
    }
}

/// <summary>
/// 单个关卡的存档快照（线性关卡路线用）。
/// </summary>
[Serializable]
public sealed class RouteStageSaveSnapshot
{
    public int stageId;
    public string currentNodeId;
    public float currentHealth;
    public int currentRevives;
    public int currentLevel;
    public float currentExp;
    public List<RouteNodeBattleSaveState> nodeStates = new List<RouteNodeBattleSaveState>();
    public List<RouteUpgradeSaveState> upgrades = new List<RouteUpgradeSaveState>();
}

/// <summary>
/// 节点战斗进度（已完成的入场序号）。
/// </summary>
[Serializable]
public sealed class RouteNodeBattleSaveState
{
    public string nodeId;
    public List<int> completedEntryIndices = new List<int>();
}

/// <summary>
/// 已应用的升级及其等级。
/// </summary>
[Serializable]
public sealed class RouteUpgradeSaveState
{
    public string upgradeId;
    public int level;
}
