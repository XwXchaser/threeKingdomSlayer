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
        return RunSnapshotSaveSection.GetRouteStageSnapshot(Load(), stageId);
    }

    public static void SaveRouteStageSnapshot(RouteStageSaveSnapshot snapshot)
    {
        var data = Load();
        RunSnapshotSaveSection.SaveRouteStageSnapshot(data, snapshot);
        Save(data);
    }

    public static FakeRouteStageSaveSnapshot GetFakeRouteSnapshot(string routeId, int stageId)
    {
        return RunSnapshotSaveSection.GetFakeRouteSnapshot(Load(), routeId, stageId);
    }

    public static void SaveFakeRouteSnapshot(FakeRouteStageSaveSnapshot snapshot)
    {
        if (snapshot == null || string.IsNullOrEmpty(snapshot.routeId)) return;
        var data = Load();
        RunSnapshotSaveSection.SaveFakeRouteSnapshot(data, snapshot);
        Save(data);
    }

    public static void ClearFakeRouteSnapshot(string routeId, int stageId)
    {
        var data = Load();
        if (RunSnapshotSaveSection.ClearFakeRouteSnapshot(data, routeId, stageId)) Save(data);
    }

    public static void ClearFakeRouteSnapshots()
    {
        var data = Load();
        if (RunSnapshotSaveSection.ClearAllFakeRouteSnapshots(data)) Save(data);
    }

    /// <summary>记录当前正在进行的假移动/卷轴路线关卡（供“继续游戏”使用）。</summary>
    public static void SetActiveFakeRouteStage(int stageId)
    {
        var data = Load();
        if (data.activeFakeRouteStageId != stageId)
        {
            RunSnapshotSaveSection.SetActiveFakeRouteStage(data, stageId);
            Save(data);
        }
    }

    public static int GetActiveFakeRouteStageId()
    {
        return RunSnapshotSaveSection.GetActiveFakeRouteStageId(Load());
    }

    public static void ClearActiveFakeRouteStage()
    {
        var data = Load();
        if (data.activeFakeRouteStageId != -1)
        {
            RunSnapshotSaveSection.ClearActiveFakeRouteStage(data);
            Save(data);
        }
    }

    public static void ClearRouteStageSnapshot(int stageId)
    {
        var data = Load();
        if (RunSnapshotSaveSection.ClearRouteStageSnapshot(data, stageId)) Save(data);
    }

    public static void MarkStageCleared(int stageId)
    {
        var data = Load();
        if (!ProgressSaveSection.IsCleared(data, stageId))
        {
            ProgressSaveSection.MarkCleared(data, stageId);
            Save(data);
        }
    }

    /// <summary>
    /// 设置铜钱数
    /// </summary>
    public static void SetCoins(int amount)
    {
        var data = Load();
        PropSaveSection.SetCoins(data, amount);
        Save(data);
    }

    /// <summary>
    /// 获取铜钱数
    /// </summary>
    public static int GetCoins()
    {
        return PropSaveSection.GetCoins(Load());
    }

    /// <summary>
    /// 增加铜钱（返回增加后的总量）
    /// </summary>
    public static int AddCoins(int delta)
    {
        var data = Load();
        int total = PropSaveSection.AddCoins(data, delta);
        Save(data);
        return total;
    }

    /// <summary>
    /// 获取指定道具数量
    /// </summary>
    public static int GetProp(PropType type)
    {
        return PropSaveSection.GetProp(Load(), type);
    }

    /// <summary>
    /// 设置指定道具数量
    /// </summary>
    public static void SetProp(PropType type, int amount)
    {
        var data = Load();
        PropSaveSection.SetProp(data, type, amount);
        Save(data);
    }

    /// <summary>
    /// 增加指定道具数量（返回增加后的总量）
    /// </summary>
    public static int AddProp(PropType type, int delta)
    {
        var data = Load();
        int total = PropSaveSection.AddProp(data, type, delta);
        Save(data);
        return total;
    }

    /// <summary>
    /// 获取下一关可用关卡ID（最大已通关ID + 1，若没有则返回1）
    /// </summary>
    public static int GetNextAvailableStageId()
    {
        return ProgressSaveSection.GetNextAvailableStageId(Load());
    }

    /// <summary>
    /// 检查关卡是否已通关
    /// </summary>
    public static bool IsStageCleared(int stageId)
    {
        return ProgressSaveSection.IsCleared(Load(), stageId);
    }
}
