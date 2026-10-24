using System;
using UnityEngine;

/// <summary>补给物件所在的层：决定「什么时候能被打」与「奖励发到哪里」。</summary>
public enum SupplyLayer
{
    /// <summary>局内层：关卡态可交互；奖励进局内资源（局内铜钱等）；状态记路线快照。</summary>
    InRun = 0,

    /// <summary>局外层：就绪态（主菜单 / 落地页）可交互；奖励进存档金币；交互后渐变消失并销毁。</summary>
    OutOfRun = 1,
}

/// <summary>
/// 局外补给点位（固定点位）。
///
/// 固定点位而不是随机生成，是为了不和场景里已有的摆件撞在一起、也不挡住路中线；
/// 位置一律写「路线根节点下的局部坐标」，与 Y 场景的 authored 坐标同一套。
/// </summary>
[Serializable]
public class SupplySpawnPoint
{
    [Tooltip("点位标识（存档 / 调试用）")]
    public string id = "hub_point_01";

    [Tooltip("点位在路线根节点下的局部坐标（与 Y 场景 authored 坐标同一套）")]
    public Vector3 localPosition = Vector3.zero;

    [Tooltip("这一轮里该点位开始出现的偏移（秒）")]
    public float roundOffsetSeconds = 0f;

    [Tooltip("出现后存活多久（秒）；到期自动消失（不结算）")]
    public float lifetimeSeconds = 45f;

    [Tooltip("击碎后给的局外金币（存档货币）")]
    public int coinReward = 100;

    [Tooltip("生成用的预制体（只允许 Inspector 拖入，不做字符串查找）")]
    public GameObject propPrefab;
}

/// <summary>
/// 局外补给生成计划（一轮的点位表）。
///
/// 「一轮」= 服务器倒计时的一个周期。接真后端时由服务端下发轮次与存活表；
/// 本地模拟阶段用本地时钟推进，规则完全一致（见 <c>LocalSupplySpawnSchedule</c>）。
/// </summary>
[CreateAssetMenu(menuName = "补给/局外生成计划", fileName = "SupplySpawnPlan")]
public class SupplySpawnPlan : ScriptableObject
{
    [Tooltip("一轮的周期（秒）。局外 = 服务器倒计时的一轮")]
    public float roundSeconds = 60f;

    [Tooltip("本轮要出现的固定点位")]
    public SupplySpawnPoint[] points = new SupplySpawnPoint[0];
}
