using UnityEngine;

/// <summary>
/// 玩家输入手势的语义分类（与攻击类型解耦）。
/// P1 用「手势 + 是否蓄满」两维表达，可无歧义地还原改造前 InputManager 的输入分支。
/// </summary>
public enum MoveGesture
{
    Tap,             // 点击：未滑动、未蓄满
    Hold,            // 长按蓄满后抬手：未滑动
    SwipeVertical,   // 竖滑（方向与垂直轴夹角小于阈值）
    SwipeHorizontal, // 横滑（方向与水平轴夹角小于阈值）
    SwipeDiagonal    // 斜滑（兜底）
}

/// <summary>
/// 一次已识别的手势输入，由 InputManager 投递给招式状态机。
/// 字段取值与改造前直接调用 AttackSystem.TryExecuteAttack 的实参一一对应。
/// </summary>
public struct GestureInput
{
    public MoveGesture gesture;
    public bool charged;
    public int targetColumn;
    public bool slashLeftToRight;
    public float slashVisualTilt;
    public float timestamp;
}

public static class MoveGestureDefaults
{
    /// <summary>
    /// P1 等价映射：还原改造前 InputManager 的手势 → 攻击类型分支。
    ///
    ///   Tap                          → Stab
    ///   Hold                         → Pierce
    ///   SwipeVertical  蓄满          → Launch
    ///   SwipeVertical  未蓄满        → Parry
    ///   SwipeHorizontal 蓄满         → Sweep
    ///   SwipeHorizontal 未蓄满       → Slash
    ///   SwipeDiagonal  任意          → Slash
    ///
    /// 「未蓄满的横滑/斜滑落到 Slash」对应改造前按住期间快速划动的分支（未蓄满时非竖滑一律 Slash）。
    /// </summary>
    public static AttackType ResolveAttackType(MoveGesture gesture, bool charged)
    {
        switch (gesture)
        {
            case MoveGesture.Tap:
                return AttackType.Stab;
            case MoveGesture.Hold:
                return AttackType.Pierce;
            case MoveGesture.SwipeVertical:
                return charged ? AttackType.Launch : AttackType.Parry;
            case MoveGesture.SwipeHorizontal:
                return charged ? AttackType.Sweep : AttackType.Slash;
            default:
                return AttackType.Slash;
        }
    }

    /// <summary>按斜滑/横滑/竖滑阈值给滑动方向分类，阈值与 InputManager 使用同一组参数。</summary>
    public static MoveGesture ClassifySwipe(Vector2 direction, float verticalThreshold, float horizontalThreshold)
    {
        if (Vector2.Angle(direction, Vector2.up) < verticalThreshold)
            return MoveGesture.SwipeVertical;
        if (Vector2.Angle(direction, Vector2.right) < horizontalThreshold)
            return MoveGesture.SwipeHorizontal;
        return MoveGesture.SwipeDiagonal;
    }
}
