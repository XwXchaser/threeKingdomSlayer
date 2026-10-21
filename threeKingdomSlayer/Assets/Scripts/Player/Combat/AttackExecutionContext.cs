using System;

/// <summary>
/// 一次**招式执行**的上下文（传给招式执行器）。
///
/// 2026-10-22 新增：招式实现从 <c>AttackSystem</c> 的 switch 分派改成注册表，
/// 新增招式 = 注册一个执行器并拿到这份上下文。
/// </summary>
public struct AttackExecutionContext
{
    /// <summary>招式系统（执行器可调用它的公开能力，如取配置、播表现）。</summary>
    public AttackSystem system;

    /// <summary>目标列（-1 = 不限；戳击/穿刺用）。</summary>
    public int targetColumn;

    /// <summary>横扫方向：true = 从左到右（斩击用）。</summary>
    public bool slashLeftToRight;

    /// <summary>斩击的视觉倾斜（斩击用）。</summary>
    public float slashVisualTilt;

    /// <summary>本次是否为蓄力招式。</summary>
    public bool charged;
}
