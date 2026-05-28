using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个击杀展示条目：到达指定击杀数时显示对应图片
/// 与关卡配置解耦，全局生效
/// </summary>
[Serializable]
public class KillDisplayEntry
{
    [Tooltip("触发击杀数阈值")]
    public int killThreshold;
    [Tooltip("显示的精灵图片")]
    public Sprite displaySprite;
    [Tooltip("总显示时长（秒），含出现+停留+消失")]
    public float displayDuration = 2f;
    [Tooltip("图片在 Canvas 中的大小")]
    public Vector2 displaySize = new Vector2(200f, 200f);
    [Tooltip("图片在 Canvas 中的锚点位置")]
    public Vector2 displayPosition = new Vector2(0f, 0f);
}

/// <summary>
/// 全局击杀展示配置 - ScriptableObject
/// 与关卡无关，策划在此配置全局击杀阈值展示
/// </summary>
[CreateAssetMenu(fileName = "GlobalKillDisplayConfig", menuName = "一夫当关/全局击杀展示配置")]
public class GlobalKillDisplayConfig : ScriptableObject
{
    [Tooltip("击杀展示条目列表")]
    public List<KillDisplayEntry> entries = new List<KillDisplayEntry>();
}
