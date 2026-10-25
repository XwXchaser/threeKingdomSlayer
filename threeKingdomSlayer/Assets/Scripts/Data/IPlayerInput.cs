using System;
using UnityEngine;

/// <summary>
/// **玩家输入端口**：战斗内核看到的「输入状态 + 输入事件」。
///
/// 刻意**不含手势识别逻辑** ✅ —— 识别层（怎么把手指动作分类成 Tap / Hold / 竖滑…）
/// 留在上层的 <c>InputManager</c> ✅；内核只关心「蓄到几级」「按住没有」「出招了没有」✅。
/// 这样以后**加新手势不会牵动内核** ✅（扩展路径见
/// <c>design/input-gesture-pipeline.md</c> ✅）。
///
/// 2026-10-22（asmdef 分层第九批）：内核对 <c>InputManager</c> 的 34 处直接引用改走本端口 ✅。
/// </summary>
public interface IPlayerInput
{
    /// <summary>当前蓄力等级（0 = 未蓄力；>=1 为蓄力招式等级）。</summary>
    int CurrentChargeLevel { get; }

    /// <summary>指针是否处于「已按下且未被输入门拦下」的状态。</summary>
    bool IsPointerDown { get; }

    /// <summary>
    /// 指针是否**物理按下**（不受输入门影响）。
    /// 防御姿态用它判断“玩家是否还在按住”（破防硬直时输入门拦下，但手指仍按着）。
    /// </summary>
    bool PointerHeld { get; }

    /// <summary>按住时长（秒）。</summary>
    float HoldDurationSeconds { get; }

    /// <summary>当前指针位置对应的戳击目标列（未落在有效列上时返回 -1）。</summary>
    int CurrentPointerStabColumn { get; }

    /// <summary>本次按住是否被判为「连段蓄力」。</summary>
    bool ComboChargeActive { get; set; }

    /// <summary>教学期开关：只允许竖滑蓄力（上挑）。</summary>
    bool RestrictChargedToVertical { get; }

    /// <summary>最小蓄力时间（秒）。</summary>
    float MinChargeTime { get; }

    /// <summary>长按判定时间（秒）。</summary>
    float LongPressDuration { get; }

    /// <summary>技能输入是否放行（狂化大招期间会关掉）。</summary>
    bool SkillInputEnabled { get; set; }

    /// <summary>屏蔽若干帧输入（防止 UI 弹窗的点击误触发攻击）。</summary>
    void BlockInputForFrames(int frames);

    /// <summary>把屏幕坐标换算成穿刺目标列（未落在有效列上时返回 -1）。</summary>
    int GetPierceColumnFromScreenPosition(Vector2 screenPos);

    /// <summary>蓄力开始（参数：屏幕坐标）。</summary>
    event Action<Vector2> OnChargeBegan;

    /// <summary>蓄力进度更新（参数：屏幕坐标、进度 0~1）。</summary>
    event Action<Vector2, float> OnChargeUpdated;

    /// <summary>蓄力结束。</summary>
    event Action OnChargeEnded;

    /// <summary>招式已执行（参数：攻击类型、目标列）。</summary>
    event Action<AttackType, int> OnAttackExecuted;
}

/// <summary>
/// **输入端口的持有者**：上层（<c>InputManager</c>）在启动时登记自己 ✅，
/// 内核从这里取端口 ✅ —— 依赖方向始终朝下 ✅（与呈现层装配点同一套路 ✅）。
/// </summary>
public static class PlayerInputHub
{
    /// <summary>当前的输入端口（没有输入源时为 null，内核按「无输入」安全降级）。</summary>
    public static IPlayerInput Current { get; set; }
}
