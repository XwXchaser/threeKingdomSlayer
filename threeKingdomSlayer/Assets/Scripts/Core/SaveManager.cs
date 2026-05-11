using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档数据 — JSON序列化到 Application.persistentDataPath
/// </summary>
[Serializable]
public class SaveData
{
    public List<int> clearedStageIds = new List<int>();
    public int coinCount;
    public bool tutorialCompleted;
}

/// <summary>
/// 存档管理器 — 静态工具类
/// </summary>
public static class SaveManager
{
    private const string SaveKey = "player_save";
    private static SaveData _cache;

    public static bool HasSave => PlayerPrefs.HasKey(SaveKey);

    /// <summary>
    /// 加载存档（带缓存）
    /// </summary>
    public static SaveData Load()
    {
        if (_cache != null) return _cache;
        if (!HasSave) { _cache = new SaveData(); return _cache; }

        string json = PlayerPrefs.GetString(SaveKey);
        _cache = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
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

    /// <summary>
    /// 标记关卡已通关
    /// </summary>
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
        data.coinCount = amount;
        Save(data);
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
