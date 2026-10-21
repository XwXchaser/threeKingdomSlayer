/// <summary>
/// **命中反应处理器**（扩展点）。
///
/// 位置：<c>Enemy.TakeDamage</c> 里、伤害结算之前的固定一个点
/// （<c>Assets/Scripts/Enemy/Enemy.Combat.cs</c>），以及一次攻击的开始 / 结束
/// （<c>AttackSystem</c> 开、<c>PlayerState</c> 关）。
///
/// 用途：实现「命中那一刻发生的额外反应」——元素反应（冰 / 火 / 雷）、破甲、处决、反击……
///
/// 现状：冰系反应已拆开——判定在玩家侧会话（<c>IceBreakSession</c>）、
/// 反应在敌人侧冻结状态（<c>EnemyFreeze.TryBreak</c>）；新增一种元素/反应要么改它们，
/// 要么注册一个处理器（本接口）。内置的冰系本身也是一个已注册的处理器
/// （<c>IceBreakReactionHandler</c>），所以两者优先级一致、可替换。
///
/// 约定：
///   • 处理器可以按 <c>ref damage</c> 修改本次伤害（例如重击解冻翻倍）；
///   • 返回 true = 本次反应确实生效了（仅用于诊断与统计，不阻断其它处理器）。
/// </summary>
public interface IDamageReactionHandler
{
    /// <summary>反应 id（诊断用）。</summary>
    string ReactionId { get; }

    /// <summary>本次攻击开始（可开启自己的会话状态，如「本次攻击已破冰的敌人」）。</summary>
    void OnAttackBegin(AttackSkillConfig cfg, float attackDuration);

    /// <summary>本次攻击结束（清理会话状态）。</summary>
    void OnAttackEnd();

    /// <summary>命中时结算这次反应。</summary>
    /// <param name="target">被命中的敌人。</param>
    /// <param name="damageType">伤害类型。</param>
    /// <param name="feedbackSource">伤害来源（区分玩家攻击本体与 DoT / 位移 / 地刺等）。</param>
    /// <param name="damage">本次伤害（可修改）。</param>
    /// <returns>true = 本次反应生效了。</returns>
    bool TryResolve(Enemy target, DamageType damageType, HitFeedbackSource feedbackSource, ref float damage);
}
