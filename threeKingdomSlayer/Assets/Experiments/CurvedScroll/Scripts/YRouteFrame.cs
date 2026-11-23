using UnityEngine;

/// <summary>
/// **路线帧**：内容在「第 k 圈」时的刚体摆放（位置 + 朝向）。
///
/// <para><b>为什么需要它</b>：卷轴的「转向」不是玩家在转，而是**地图在转**——
/// 内容全部活在展开态（flat）空间里，转弯由 shader 绕相机旋转做出来。
/// 既然地图在转，那么「第 k 圈」的内容就不能只沿 +z 平移（那是「地图不转」的模型，
/// 要求每圈净转角为 0），而必须**带着本圈累积的旋转一起摆**。</para>
///
/// <para><b>不变量</b>：<see cref="Identity"/> 是第一圈。第一圈用恒等帧摆放
/// ⇒ 旧路线、旧场景表现<b>逐像素不变</b>，无限循环只在 k ≥ 1 时起作用。</para>
///
/// <para><b>坐标</b>：位置/朝向都在路线根（<see cref="YScrollSample"/> 所在物体）的局部空间，
/// 与内容 prefab 的 <c>localPosition</c> 同源。</para>
/// </summary>
[System.Serializable]
public struct YRouteFrame
{
    /// <summary>本圈内容原点在路线空间的位置。</summary>
    public Vector3 position;

    /// <summary>本圈内容在路线空间的朝向（度，绕 Y）。</summary>
    public float heading;

    public static readonly YRouteFrame Identity = new YRouteFrame { position = Vector3.zero, heading = 0f };

    public Quaternion Rotation => Quaternion.Euler(0f, heading, 0f);

    public bool IsIdentity => position == Vector3.zero && Mathf.Approximately(heading, 0f);

    /// <summary>内容局部坐标 → 路线空间。</summary>
    public Vector3 Apply(Vector3 local) => IsIdentity ? local : position + Rotation * local;

    /// <summary>路线空间 → 内容局部坐标（<see cref="Apply"/> 的逆）。</summary>
    public Vector3 Inverse(Vector3 route) => IsIdentity ? route : Quaternion.Euler(0f, -heading, 0f) * (route - position);

    /// <summary>本帧之后再叠加一帧（子帧在自己的局部空间里）。</summary>
    public YRouteFrame Compose(YRouteFrame child) => new YRouteFrame
    {
        position = Apply(child.position),
        heading = heading + child.heading
    };

    public override string ToString() => $"pos={position} heading={heading:F2}°";
}
