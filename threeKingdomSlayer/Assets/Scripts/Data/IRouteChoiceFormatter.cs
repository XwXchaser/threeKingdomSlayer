/// <summary>
/// 路线出口的**呈现**（扩展点）。
///
/// 出口按钮上显示什么（标题 / 副标题 / 图标 / 位置）由这里决定。
/// 内置实现保持现状：标题 = 「出口名 → 目标节点名」。
///
/// 新增一种分支形态（带目标预览、带消耗、带风险提示、带锁定徽标……）
/// = 写一个实现并 <c>FakeRouteRuntime.RegisterChoiceFormatter(...)</c> 注册，
/// 不用改运行时与面板。
///
/// 约定：**第一个 CanHandle 为真的格式化器完全接管这条出口的呈现**（内置兼底不会自动跑）。
/// 想在默认文案上做追加时，自己先调一次内置实现：
/// <code>
/// public void Format(FakeRouteChoiceConfig choice, FakeRouteNodeConfig from, RouteChoiceOption option)
/// {
///     new DefaultRouteChoiceFormatter().Format(choice, from, option);
///     option.label = "⚠ " + option.label;
/// }
/// </code>
/// </summary>
public interface IRouteChoiceFormatter
{
    /// <summary>本格式化器是否负责这条出口（按出口类型 / 目标节点自行判断）。</summary>
    bool CanFormat(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode);

    /// <summary>把出口填进选项（就地修改 <paramref name="option"/>）。</summary>
    void Format(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode, RouteChoiceOption option);
}
