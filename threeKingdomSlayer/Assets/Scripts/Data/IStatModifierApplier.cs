/// <summary>
/// 属性修正应用器接口 — 实现者能接收 StatModifier 的 Apply/Remove
///
/// 2026-10-22（asmdef 分层第三批）：从 <c>Assets/Scripts/Core/BuffManager.cs</c> 抽到契约层
/// —— 这是「局内 Build」的扩展点之一，住在契约层才能被任何层实现 / 引用。
/// </summary>
public interface IStatModifierApplier
{
    void ApplyModifier(StatModifier modifier);
    void RemoveModifier(StatModifier modifier);
}
