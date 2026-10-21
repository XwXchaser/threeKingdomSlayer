/// <summary>
/// 效果执行器接口 — 处理升级奖励中的行为型效果
/// 如 on_attack_trigger、on_kill_chance、unlock_attack 等
///
/// 2026-10-22（asmdef 分层第三批）：从 <c>Assets/Scripts/Core/IEffectExecutor.cs</c> 下沉到契约层
/// —— 这是**局内 Build 的扩展点**，新行为型升级 = 实现它并注册，不必改管理器本体。
/// </summary>
public interface IEffectExecutor
{
    /// <summary>应用效果</summary>
    /// <param name="def">升级定义</param>
    /// <param name="level">当前等级（1-based）</param>
    void Execute(UpgradeDefinition def, int level);

    /// <summary>移除效果（新对局重置时）</summary>
    void Remove(string upgradeId);
}
