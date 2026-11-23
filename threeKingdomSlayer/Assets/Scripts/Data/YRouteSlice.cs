using UnityEngine;

/// <summary>
/// **路线切片**（2026-11-19，最小实验用）。
///
/// 一条路线 = **有序的切片序列**；场景 = 切片按顺序**拼接**。
/// ⇒ 「无限卷轴」不是代码特性，而是**数据**：某个切片的出口接回前面的切片 ⇒ 序列成环。
///
/// <para><b>为什么是切片</b>：
/// 现有工程里已经有「段 prefab」和「节点」两类内容，但它们没有共同的归属单位。
/// 切片把「这一片的地面/景物/节点/敌人」收在一起 ⇒ 拼接时整片一起摆，接缝天然对齐。</para>
///
/// <para><b>坐标约定</b>：切片内容全部活在 <b>flat（展开态）空间</b>——
/// 段 prefab 的子物件 <c>localPosition</c> 就是「横向偏移 x + 沿路距离 z」，
/// 弯是 shader 绕相机旋转做出来的（不是 3D 几何）。
/// ⇒ 拼接 = 沿 flat z 依次排开 ⇒ <b>不需要任何复制/平移/周期特判</b>。</para>
/// </summary>
[CreateAssetMenu(fileName = "YRouteSlice", menuName = "一夫当关/Y 路线切片")]
public sealed class YRouteSlice : ScriptableObject
{
    [Tooltip("切片 id（唯一）")]
    public string sliceId = "";

    [Tooltip("切片显示名")]
    public string displayName = "";

    [Tooltip("本片沿路长度（米）。切片按顺序首尾相接 ⇒ 前一片的结束就是本片的开始。")]
    public float length = 90f;

    [Tooltip("本片包含的段 prefab（景物 / 地表小块）。它们自带 flat 坐标（x=横向, z=沿路）。")]
    public GameObject[] segmentPrefabs = System.Array.Empty<GameObject>();

    [Tooltip("本片的节点（战斗配置 + 敌人 + 锚点）。节点就在这一片里。")]
    public FakeRouteNodeConfig node;

    [Tooltip("节点在本片内的沿路位置（米）。")]
    public float nodeDistance = 0f;

    [Tooltip("本片地面（flat 地面小块）。留空 = 用场景里那块常驻地形。")]
    public GameObject groundPrefab;
}

/// <summary>
/// **切片序列**（2026-11-19，最小实验用）。
///
/// 切片的有序列表 + 起点。<b>序列成环 ⇒ 无限卷轴</b>（出口可以指回前面的切片）。
/// </summary>
[CreateAssetMenu(fileName = "YRouteSliceSequence", menuName = "一夫当关/Y 路线切片序列")]
public sealed class YRouteSliceSequence : ScriptableObject
{
    [Tooltip("切片序列（按顺序拼接）")]
    public YRouteSlice[] slices = System.Array.Empty<YRouteSlice>();

    [Tooltip("起始切片 id（留空 = 用第一个）")]
    public string startSliceId = "";

    /// <summary>所有切片的沿路总长（米）—— 一圈的 flat 长度。</summary>
    public float TotalLength
    {
        get
        {
            float sum = 0f;
            if (slices == null) return 0f;
            for (int i = 0; i < slices.Length; i++)
                if (slices[i] != null) sum += slices[i].length;
            return sum;
        }
    }

    /// <summary>按 id 找切片；找不到返回 null。</summary>
    public YRouteSlice FindById(string sliceId)
    {
        if (slices == null || string.IsNullOrEmpty(sliceId)) return null;
        for (int i = 0; i < slices.Length; i++)
            if (slices[i] != null && slices[i].sliceId == sliceId) return slices[i];
        return null;
    }

    /// <summary>起始切片。</summary>
    public YRouteSlice StartSlice()
    {
        if (slices == null || slices.Length == 0) return null;
        YRouteSlice byId = FindById(startSliceId);
        return byId != null ? byId : slices[0];
    }
}
