using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档**服务**（读写 PlayerPrefs 里的 JSON 存档）。
///
/// 2026-10-22（asmdef 分层第二批）：从 <c>Assets/Scripts/Core/SaveManager.cs</c> 移到服务层
/// （<c>Locus.Services</c>）—— 存档模型（<c>SaveData</c> 等）已下沉到数据层，
/// 本类只负责「读 / 写 / 迁移」这一件事，不该待在业务目录里被业务代码反向依赖。
/// </summary>
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

    public static FakeRouteStageSaveSnapshot GetFakeRouteSnapshot(string routeId, int stageId)
    {
        var data = Load();
        if (data.fakeRouteSnapshots == null) return null;
        for (int i = 0; i < data.fakeRouteSnapshots.Count; i++)
        {
            var snapshot = data.fakeRouteSnapshots[i];
            if (snapshot != null && snapshot.routeId == routeId && snapshot.stageId == stageId
                && snapshot.routeArchitectureId == "fake-route-v1") return snapshot;
        }
        return null;
    }

    public static void SaveFakeRouteSnapshot(FakeRouteStageSaveSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrEmpty(snapshot.routeId)) return;
        var data = Load();
        if (data.fakeRouteSnapshots == null) data.fakeRouteSnapshots = new List<FakeRouteStageSaveSnapshot>();
        for (int i = data.fakeRouteSnapshots.Count - 1; i >= 0; i--)
        {
            var old = data.fakeRouteSnapshots[i];
            if (old == null || (old.routeId == snapshot.routeId && old.stageId == snapshot.stageId))
                data.fakeRouteSnapshots.RemoveAt(i);
        }
        data.fakeRouteSnapshots.Add(snapshot);
        Save(data);
    }

    public static void ClearFakeRouteSnapshot(string routeId, int stageId)
    {
        var data = Load();
        if (data.fakeRouteSnapshots == null) return;
        bool changed = false;
        for (int i = data.fakeRouteSnapshots.Count - 1; i >= 0; i--)
        {
            var snapshot = data.fakeRouteSnapshots[i];
            if (snapshot != null && snapshot.routeId == routeId && snapshot.stageId == stageId)
            {
                data.fakeRouteSnapshots.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) Save(data);
    }

    public static void ClearFakeRouteSnapshots()
    {
        var data = Load();
        if (data.fakeRouteSnapshots == null || data.fakeRouteSnapshots.Count == 0) return;
        data.fakeRouteSnapshots.Clear();
        Save(data);
    }

    /// <summary>记录当前正在进行的假移动/卷轴路线关卡（供“继续游戏”使用）。</summary>
    public static void SetActiveFakeRouteStage(int stageId)
    {
        var data = Load();
        if (data.activeFakeRouteStageId != stageId)
        {
            data.activeFakeRouteStageId = stageId;
            Save(data);
        }
    }

    public static int GetActiveFakeRouteStageId()
    {
        return Load().activeFakeRouteStageId;
    }

    public static void ClearActiveFakeRouteStage()
    {
        var data = Load();
        if (data.activeFakeRouteStageId != -1)
        {
            data.activeFakeRouteStageId = -1;
            Save(data);
        }
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
