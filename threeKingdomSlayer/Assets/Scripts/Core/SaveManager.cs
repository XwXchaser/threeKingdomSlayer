using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档数据 — JSON序列化到 Application.persistentDataPath
/// 道具系统：props 列表管理所有道具（铜钱/钥匙等），coinCount 保留向后兼容
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
    }
}

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

[Serializable]
public sealed class RouteNodeBattleSaveState
{
    public string nodeId;
    public List<int> completedEntryIndices = new List<int>();
}

[Serializable]
public sealed class RouteUpgradeSaveState
{
    public string upgradeId;
    public int level;
}

public static class SaveManager
{
    private const string SaveKey = "player_save";
    private static SaveData _cache;

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

    /// <summary>
    /// 加载存档（带缓存，自动迁移旧格式）
    /// </summary>
    public static SaveData Load()
    {
        if (_cache != null) return _cache;
        if (!HasSave) { _cache = new SaveData(); return _cache; }

        string json = PlayerPrefs.GetString(SaveKey);
        _cache = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        _cache.MigrateIfNeeded();
        return _cache;
    }

    /// <summary>
    /// 保存存档
    /// </summary>
    public static void Save(SaveData data)
    {
        _cache = data;
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 保存当前缓存
    /// </summary>
    public static void Save()
    {
        if (_cache == null) return;
        Save(_cache);
    }

    /// <summary>
    /// 删除存档
    /// </summary>
    public static void Delete()
    {
        _cache = null;
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }

    public static RouteStageSaveSnapshot GetRouteStageSnapshot(int stageId)
    {
        var data = Load();
        for (int i = 0; i < data.routeStageSnapshots.Count; i++)
            if (data.routeStageSnapshots[i] != null && data.routeStageSnapshots[i].stageId == stageId)
                return data.routeStageSnapshots[i];
        return null;
    }

    public static void SaveRouteStageSnapshot(RouteStageSaveSnapshot snapshot)
    {
        var data = Load();
        for (int i = data.routeStageSnapshots.Count - 1; i >= 0; i--)
            if (data.routeStageSnapshots[i] == null || data.routeStageSnapshots[i].stageId == snapshot.stageId)
                data.routeStageSnapshots.RemoveAt(i);
        data.routeStageSnapshots.Add(snapshot);
        Save(data);
    }

    public static void ClearRouteStageSnapshot(int stageId)
    {
        var data = Load();
        bool changed = false;
        for (int i = data.routeStageSnapshots.Count - 1; i >= 0; i--)
        {
            if (data.routeStageSnapshots[i] != null && data.routeStageSnapshots[i].stageId == stageId)
            {
                data.routeStageSnapshots.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) Save(data);
    }

    public static void MarkStageCleared(int stageId)
    {
        var data = Load();
        if (!data.clearedStageIds.Contains(stageId))
        {
            data.clearedStageIds.Add(stageId);
            Save(data);
        }
    }

    /// <summary>
    /// 设置铜钱数
    /// </summary>
    public static void SetCoins(int amount)
    {
        var data = Load();
        data.props.SetPropAmount(PropType.Coin, amount);
        Save(data);
    }

    /// <summary>
    /// 获取铜钱数
    /// </summary>
    public static int GetCoins()
    {
        return Load().GetCoinCount();
    }

    /// <summary>
    /// 增加铜钱（返回增加后的总量）
    /// </summary>
    public static int AddCoins(int delta)
    {
        var data = Load();
        int total = data.props.AddPropAmount(PropType.Coin, delta);
        Save(data);
        return total;
    }

    /// <summary>
    /// 获取指定道具数量
    /// </summary>
    public static int GetProp(PropType type)
    {
        return Load().props.GetPropAmount(type);
    }

    /// <summary>
    /// 设置指定道具数量
    /// </summary>
    public static void SetProp(PropType type, int amount)
    {
        var data = Load();
        data.props.SetPropAmount(type, amount);
        Save(data);
    }

    /// <summary>
    /// 增加指定道具数量（返回增加后的总量）
    /// </summary>
    public static int AddProp(PropType type, int delta)
    {
        var data = Load();
        int total = data.props.AddPropAmount(type, delta);
        Save(data);
        return total;
    }

    /// <summary>
    /// 获取下一关可用关卡ID（最大已通关ID + 1，若没有则返回1）
    /// </summary>
    public static int GetNextAvailableStageId()
    {
        var data = Load();
        int maxCleared = 0;
        foreach (int id in data.clearedStageIds)
            if (id > maxCleared) maxCleared = id;
        return maxCleared + 1;
    }

    /// <summary>
    /// 检查关卡是否已通关
    /// </summary>
    public static bool IsStageCleared(int stageId)
    {
        return Load().clearedStageIds.Contains(stageId);
    }
}
