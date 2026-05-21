using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道具类型 — 铜钱为通用货币，后续可扩展钥匙/宝石等
/// </summary>
public enum PropType
{
    Coin = 0,   // 铜钱（通用货币）
    // 后续扩展: Key, Gem, ...
}

/// <summary>
/// 道具数据 — 类型 + 数量
/// </summary>
[Serializable]
public class PropData
{
    public PropType type;
    public int amount;
}

/// <summary>
/// 道具系统工具类 — 用于 SaveData 中管理道具列表
/// </summary>
public static class PropUtils
{
    /// <summary>
    /// 从道具列表中获取指定类型的数量
    /// </summary>
    public static int GetPropAmount(this List<PropData> props, PropType type)
    {
        if (props == null) return 0;
        foreach (var p in props)
            if (p.type == type) return p.amount;
        return 0;
    }

    /// <summary>
    /// 设置道具列表中指定类型的数量
    /// </summary>
    public static void SetPropAmount(this List<PropData> props, PropType type, int amount)
    {
        foreach (var p in props)
        {
            if (p.type == type)
            {
                p.amount = amount;
                return;
            }
        }
        props.Add(new PropData { type = type, amount = amount });
    }

    /// <summary>
    /// 增加道具数量（返回增加后的总量）
    /// </summary>
    public static int AddPropAmount(this List<PropData> props, PropType type, int delta)
    {
        foreach (var p in props)
        {
            if (p.type == type)
            {
                p.amount += delta;
                return p.amount;
            }
        }
        props.Add(new PropData { type = type, amount = delta });
        return delta;
    }
}
