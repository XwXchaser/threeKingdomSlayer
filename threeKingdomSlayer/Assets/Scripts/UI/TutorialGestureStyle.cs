using TMPro;
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

    // ─────────────────────────────────────────────────────────────────────────
    // 提示文案（2026-11-07 用户反馈：「对话黑框严重阻挡了功能UI，
    // 应该只显示白色文字，去除底框且注意排布问题」）
    //
    // 规则：
    //   • **没有底板** → 只有白字（原来的深色圆角底板 820×96 会盖住一整条功能 UI）
    //   • **尺寸按内容收缩** → 不再是固定的一大块
    //   • **摆位要躲开要教学的东西** → 不遮目标、也不出图层（屏幕 / 面板）
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>提示文案的宽度上限（画布单位；参考分辨率 1080 宽）。</summary>
    public const float MaxLabelWidth = 760f;

    /// <summary>
    /// 落地页**底部功能 UI 带**的高度（屏幕像素）：任务栏 + 路线标题 + 天赋卡。
    /// 提示文案不许进这条带子 → 否则白字会压在路线标题上（实测）。
    /// </summary>
    public const float BottomHudBandPx = 440f;

    /// <summary>
    /// 按**内容**收缩文案尺寸 → 返回实际尺寸。
    /// 单行放得下就单行；放不下就**换行**（不是溢出）→ 超出上限宽度的长句不会压到别的 UI 上。
    /// </summary>
    public static Vector2 FitLabelToContent(TextMeshProUGUI text, float maxWidth, float minWidth = 120f)
    {
        if (text == null) return Vector2.zero;
        string content = text.text != null ? text.text : string.Empty;

        // ① 先按“无限宽”量一次 → 放得下就是单行
        text.enableWordWrapping = false;
        var single = text.GetPreferredValues(content, 100000f, 100000f);
        bool oneLine = single.x + 12f <= maxWidth;

        float w, h;
        if (oneLine)
        {
            w = Mathf.Clamp(single.x + 12f, minWidth, maxWidth);
            h = single.y + 10f;
        }
        else
        {
            // ② 放不下 → 按上限宽度换行，再量一次
            text.enableWordWrapping = true;
            var wrapped = text.GetPreferredValues(content, maxWidth - 12f, 10000f);
            w = maxWidth;
            h = wrapped.y + 10f;
        }

        var rt = text.rectTransform;
        rt.sizeDelta = new Vector2(w, h);
        return new Vector2(w, h);
    }

    /// <summary>把中心点夹回图层范围内（不出屏幕 / 不出面板）。</summary>
    public static Vector2 ClampToLayer(RectTransform layer, Vector2 center, Vector2 size, float margin = 8f)
    {
        if (layer == null) return center;
        Rect b = layer.rect;
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        float minX = b.xMin + margin + hx;
        float maxX = b.xMax - margin - hx;
        float minY = b.yMin + margin + hy;
        float maxY = b.yMax - margin - hy;
        float x = maxX < minX ? b.center.x : Mathf.Clamp(center.x, minX, maxX);
        float y = maxY < minY ? b.center.y : Mathf.Clamp(center.y, minY, maxY);
        return new Vector2(x, y);
    }

    /// <summary>
    /// 把文案摆在**目标旁边**：不遮目标（那是要教学的东西）、不出图层。
    /// 候选顺序：上 → 下 → 右 → 左；都不合格就取第一个并夹回范围内。
    /// </summary>
    public static Vector2 PlaceLabelClearOf(RectTransform layer, RectTransform target, Vector2 labelSize, float gap = 20f, float bottomKeepOut = 0f)
    {
        if (layer == null) return Vector2.zero;

        Vector2 tc, ts;
        TargetBounds(layer, target, out tc, out ts);

        float hx = labelSize.x * 0.5f;
        float hy = labelSize.y * 0.5f;
        float minY = layer.rect.yMin + bottomKeepOut + hy;
        Vector2[] candidates =
        {
            new Vector2(tc.x, tc.y + ts.y * 0.5f + gap + hy),   // 上
            new Vector2(tc.x, tc.y - ts.y * 0.5f - gap - hy),   // 下
            new Vector2(tc.x + ts.x * 0.5f + gap + hx, tc.y),   // 右
            new Vector2(tc.x - ts.x * 0.5f - gap - hx, tc.y),   // 左
        };

        Rect b = layer.rect;
        for (int i = 0; i < candidates.Length; i++)
        {
            Vector2 c = candidates[i];
            if (c.y - hy < minY) continue;                       // 不许压到底部功能 UI 带
            if (!Inside(c, labelSize, b, 8f)) continue;
            if (Overlaps(c, labelSize, tc, ts, 8f)) continue;
            return c;
        }
        // 都不行 → 取第一个，并夹回范围内（同时抬出底部带）
        Vector2 fallback = candidates[0];
        if (fallback.y - hy < minY) fallback.y = minY;
        return ClampToLayer(layer, fallback, labelSize, 8f);
    }

    /// <summary>目标在图层坐标系里的包围盒（中心 + 尺寸）。</summary>
    private static void TargetBounds(RectTransform layer, RectTransform target, out Vector2 center, out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;
        if (target == null) return;
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = layer.InverseTransformPoint(corners[i]);
            min = Vector2.Min(min, new Vector2(p.x, p.y));
            max = Vector2.Max(max, new Vector2(p.x, p.y));
        }
        center = (min + max) * 0.5f;
        size = max - min;
    }

    private static bool Inside(Vector2 center, Vector2 size, Rect bounds, float margin)
    {
        float hx = size.x * 0.5f;
        float hy = size.y * 0.5f;
        return center.x - hx >= bounds.xMin + margin && center.x + hx <= bounds.xMax - margin
            && center.y - hy >= bounds.yMin + margin && center.y + hy <= bounds.yMax - margin;
    }

    private static bool Overlaps(Vector2 center, Vector2 size, Vector2 otherCenter, Vector2 otherSize, float margin)
    {
        float hx = size.x * 0.5f + margin;
        float hy = size.y * 0.5f + margin;
        float ox = otherSize.x * 0.5f + margin;
        float oy = otherSize.y * 0.5f + margin;
        return Mathf.Abs(center.x - otherCenter.x) < hx + ox && Mathf.Abs(center.y - otherCenter.y) < hy + oy;
    }


}
