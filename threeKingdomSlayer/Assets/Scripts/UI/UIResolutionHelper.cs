using UnityEngine;

/// <summary>
/// UI 分辨率适配工具。基于参考分辨率 1080×1920 提供缩放系数，
/// 窄屏设备上 UI 元素自动等比缩小，防止水平方向溢出。
/// </summary>
public static class UIResolutionHelper
{
    public const float ReferenceWidth = 1080f;
    public const float ReferenceHeight = 1920f;

    /// <summary>
    /// 当前 UI 缩放系数。参考分辨率下 = 1，窄屏时 < 1。
    /// 公式：当前宽高比 / 参考宽高比，clamp 到 ≤1。
    /// 保证以 1080×1920 为基准设计的绝对像素值在窄屏上等比缩小。
    /// </summary>
    public static float UIScale
    {
        get
        {
            float curAspect = (float)Screen.width / Screen.height;
            float refAspect = ReferenceWidth / ReferenceHeight;
            return Mathf.Min(1f, curAspect / refAspect);
        }
    }
}
