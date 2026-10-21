/// <summary>
/// **数值型升级效果的应用器**（扩展点）。
///
/// 与行为型的 <see cref="IEffectExecutor"/> 平行：行为型升级通过
/// <c>UpgradeEffectManager.RegisterExecutor(effectType, executor)</c> 注册，
/// 数值型升级通过 <c>RegisterNumericApplier(effectType, applier)</c> 注册。
///
/// 新增一种数值型升级的完整步骤：
/// <list type="number">
/// <item>写一个实现本接口的类（数值改动走 <see cref="IUpgradeStatSink"/>）；</item>
/// <item>在 <c>UpgradeEffectManager.Awake</c> 之后调用 <c>RegisterNumericApplier("你的 effectType", applier)</c>。</item>
/// </list>
/// 之后**不需要**再改 <c>ApplyUpgrade</c> / <c>ApplyNumericEffect</c>。
/// </summary>
public interface IUpgradeNumericApplier
{
    /// <summary>把这一级的数值效果写入玩家数值。</summary>
    /// <param name="def">升级定义（可读每级配置、反射护盾档位等）。</param>
    /// <param name="level">当前等级（1-based）。</param>
    /// <param name="cfg">该级的数值配置。</param>
    void Apply(UpgradeDefinition def, int level, NumericLevelConfig cfg);
}
