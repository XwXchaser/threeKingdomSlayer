/// <summary>
/// 攻击类型枚举。
///
/// 【为什么单独成文件】原先声明在 <c>Assets/Scripts/Player/PlayerState.cs</c> 末尾，结果任何配置
/// （UpgradeDefinition / AttackSkillConfig / MoveGesture …）只要用到 AttackType，就被迫引用
/// PlayerState 这个运行时大类 → 配置层无法与运行时分层。2026-10-20 批次 1 提到这里，
/// 属于纯枚举搬家：枚举值不变，序列化按 int，资产引用不受影响。
/// </summary>
public enum AttackType
{
    Stab,     // 戳击
    Slash,    // 斩击
    Pierce,   // 穿刺
    Sweep,    // 横扫
    Launch,   // 挑飞
    Parry,    // 招架
    Ultimate  // 大招
}

/// <summary>
/// 伤害类型枚举。
///
/// 【为什么单独成文件】原先声明在 <c>Assets/Scripts/Enemy/Enemy.cs</c>（5224 行的上帝类）末尾，
/// 造成「配置要用伤害类型 → 必须引用整个 Enemy」的隐式依赖。搬家同样不动枚举值。
/// </summary>
public enum DamageType
{
    Stab,        // 戳击
    Slash,       // 斩击
    Pierce,      // 穿刺
    Sweep,       // 横扫
    Launch,      // 挑飞
    Poise,       // 架势伤害
    Convergence, // 聚拢冲突伤害
    Ice,         // 冰系伤害
    Collision    // 破冰推动被阻挡时的碰撞伤害
}
