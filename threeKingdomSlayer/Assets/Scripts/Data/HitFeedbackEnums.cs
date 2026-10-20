/// <summary>
/// 打击反馈来源枚举。
///
/// 【为什么单独成文件】原先与 <c>HitFeedbackContext</c> 一起声明在
/// <c>Assets/Scripts/Core/HitFeedbackManager.cs</c>，而 Context 持有 <c>Enemy</c> 引用；
/// 结果任何配置只要用到这两个枚举（例如 AttackSkillConfig 的反馈强度字段）就被迫
/// 引用整个打击反馈运行时。2026-10-20 批次 1 把纯枚举提到数据层。
/// </summary>
public enum HitFeedbackSource
{
    BasicAttack,
    ActiveSkill,
    Passive,
    Item,
    Phantom,
    Dot,
    Ultimate,
    Displacement,
    SpikeTrap,
    Collision
}

/// <summary>打击反馈强度枚举——与 <see cref="HitFeedbackSource"/> 一起提到数据层。</summary>
public enum HitFeedbackStrength
{
    None,
    Light,
    Standard,
    Heavy
}
