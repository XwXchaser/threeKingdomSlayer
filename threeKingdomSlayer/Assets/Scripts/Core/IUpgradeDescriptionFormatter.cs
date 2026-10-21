/// <summary>
/// **升级描述格式化器**（扩展点）。
///
/// 升级卡片的文案里有一批占位符（<c>{0} {1} {2}</c> …），每种升级类型的填法不同。
/// 以前这些填法是一条 200 多行的 if-else 链（<c>FormatDescriptionForLevel</c>），
/// 新增一种升级类型就必须去改那条链——那是「新机制必撞硬编码分派」的典型点。
///
/// 现在：自定义格式化器通过 <c>UpgradeEffectManager.RegisterDescriptionFormatter(formatter)</c> 注册，
/// **优先于**内置兜底生效；内置兜底（<c>DefaultUpgradeDescriptionFormatter</c>）保持既有全部类型的文案不变。
/// </summary>
public interface IUpgradeDescriptionFormatter
{
    /// <summary>本格式化器是否处理这条升级（按类型 / effectType 自行判断）。</summary>
    bool CanFormat(UpgradeDefinition def, int nextLevel);

    /// <summary>返回填好占位符的描述文本。</summary>
    /// <param name="def">升级定义。</param>
    /// <param name="desc">原始描述模板（已合并 extraDescriptionTemplate）。</param>
    /// <param name="nextLevel">目标等级（1-based）。</param>
    string Format(UpgradeDefinition def, string desc, int nextLevel);
}
