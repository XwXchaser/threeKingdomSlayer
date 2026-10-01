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
    /// <summary>蓄力等级（0 = 未蓄力；>=1 为蓄力招式等级），由 InputManager 按按住时长判定</summary>
    public int chargeLevel;
    public int targetColumn;
    public bool slashLeftToRight;
    public float slashVisualTilt;
    public float timestamp;
}

public static class MoveGestureDefaults
{
    /// <summary>
    /// 手势 → 攻击类型：分叉轴是「是否蓄力」，方向只在蓄力时用于区分是哪一种蓄力招式。
    ///
    ///   Tap                              → Stab
    ///   Hold（长按不移动）                  → Pierce
    ///   竖滑 + 蓄力                        → Launch
    ///   竖滑 + 未蓄力                      → Parry
    ///   横滑 + 蓄力                        → Sweep
    ///   横滑 + 未蓄力                      → Slash
    ///   斜滑（无论是否蓄力）                  → Slash
    ///
    /// 注意：方向不是分叉轴。「未蓄力的横滑/斜滑都落到 Slash」是设计本身，不得改成按方向分叉。
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
