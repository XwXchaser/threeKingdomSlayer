/// <summary>
/// 效果执行器接口 — 处理升级奖励中的行为型效果
/// 如 on_attack_trigger、on_kill_chance、unlock_attack 等
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
