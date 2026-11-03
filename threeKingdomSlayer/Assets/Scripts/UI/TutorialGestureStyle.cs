using UnityEngine;

/// <summary>
/// **手势提示的统一样式**（2026-11-01）。
///
/// 为什么要有这个：之前「教学手势提示」和「常态点击引导」各写了一套
///   · 尺寸（手 220 / 圈 240 vs 另一套）
///   · 锚点（pivot 在指尖 vs 在掌心）
///   · 旋转补偿（绕指尖转 vs 绕掌心转 + 位置补偿）
/// → 同一个提示在两个地方长得不一样，而且手指和光圈对不齐
///   （用户 2026-11-01 反馈：「引导手指和光圈位置不匹配…就没有个统一的设置吗」）。
///
/// 这里的约定（两边共用）：
///   • **指尖 = 定位锚点**：手的位置由指尖决定 → 指尖落在目标点（光圈心）上；
///   • **掌心 = 旋转支点**：按压动画绕**掌心**转，**掌心在按压过程中不动**；
///   • 常态引导 = 基础尺寸 × <see cref="PersistentScale"/>（比教学大一圈，远处也看得见）。
///
/// ⚠️ 2026-11-02 修正：之前两边都在按压时**逐帧把指尖钉回目标点**
///    （<see cref="RotatedTipOffset"/> 的反向补偿），而「绕掌心转 + 钉死指尖」
///    在数学上等价于「**绕指尖转**」→ 掌心被迫绕着指尖走，视觉上就是
///    「整只手以指尖为圆心在转」，不像点击（用户反馈：「实际是绕着手指在旋转，而不是掌心」）。
///    现在**不再做逐帧补偿**：按压只改旋转/缩放（绕掌心），指尖只在静止姿态参与定位。
///    代价：按压的那一瞬间指尖会沿弧线离开圈心，
///    最大距离 = 2 × |掌心→指尖| × sin(θ/2)（θ=8° → 约 25 画布单位，光圈半径的 14%）。
/// </summary>
public static class TutorialGestureStyle
{
    /// <summary>手的基准尺寸（**教学提示实际用的尺寸**：308）。</summary>
    public const float BaseHandSize = 308f;

    /// <summary>光圈的基准直径（教学提示实际用的尺寸：240）。</summary>
    public const float BaseRingSize = 240f;

    /// <summary>常态引导相对教学的放大倍数（远处也要看得清）。</summary>
    public const float PersistentScale = 2f;

    /// <summary>指尖在图里的位置（0~1，左下原点）。</summary>
    public static readonly Vector2 TipUV = new Vector2(0.273f, 0.801f);

    /// <summary>掌心在图里的位置（0~1，左下原点）—— 旋转支点。</summary>
    public static readonly Vector2 PalmUV = new Vector2(0.48f, 0.48f);

    /// <summary>按压时的倾角（度）。</summary>
    public const float PressAngle = 8f;

    /// <summary>常态引导：手的尺寸。</summary>
    public static float PersistentHandSize => BaseHandSize * PersistentScale;

    /// <summary>常态引导：光圈直径。</summary>
    public static float PersistentRingSize => BaseRingSize * PersistentScale;

    /// <summary>
    /// 手的 RectTransform pivot —— 设在**掌心**（旋转支点）。
    /// </summary>
    public static Vector2 HandPivot => PalmUV;

    /// <summary>
    /// 手指图与光圈图的**实测**几何（2026-11-02 用 alpha 包围盒量出来的）：
    ///   • 手指图 `tutorial_gesture_finger_ui.png`（256×256）：alpha 内容 x=62..193 / y=49..207；
    ///     最上一行只有 6px 宽 → **指尖在 (0.273, 0.801)**；手掌主体在 y≈0.42~0.55 → **掌心 (0.48, 0.48)**。
    ///   • 光圈图 `tutorial_gesture_hold_ring_ui.png`（256×256）：内容 x=19..236 / y=21..234（占 85%），
    ///     内容重心 UV (0.498, 0.489) ≈ pivot 中心 → **光圈是居中的**，可直接用 pivot 对准目标。
    /// </summary>
    public static Vector2 HandPositionOffset(float size)
    {
        return new Vector2((PalmUV.x - TipUV.x) * size, (PalmUV.y - TipUV.y) * size);
    }

    /// <summary>指尖相对 pivot（掌心）的偏移（未旋转、未缩放）。</summary>
    public static Vector2 TipOffset(float size)
    {
        return new Vector2((TipUV.x - PalmUV.x) * size, (TipUV.y - PalmUV.y) * size);
    }
}
