using System;
using UnityEngine;

/// <summary>
/// 招式转移边上的一条输入：在某段接续窗口内接受某手势，转移到后继招式。
/// 窗口以内层（边）为准；未显式指定时使用节点/默认派生窗口。
/// </summary>
[Serializable]
public class MoveEdge
{
    [Tooltip("接受的手势")]
    public MoveGesture gesture;

    [Tooltip("是否覆盖窗口。关闭时使用节点或按招式类型派生的默认窗口")]
    public bool overrideWindow = false;

    [Tooltip("接续窗口起点（占招式序列的归一化比例 0~1）")]
    [Range(0f, 1f)] public float windowStart01 = 0f;

    [Tooltip("接续窗口终点（占招式序列的归一化比例 0~1）")]
    [Range(0f, 1f)] public float windowEnd01 = 1f;

    [Tooltip("后继招式。留空表示该输入不转移节点（等同于无匹配边）")]
    public MoveDefinition next;
}
