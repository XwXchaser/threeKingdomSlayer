/// <summary>
/// 路线出口的**可选性判定**（扩展点）。
///
/// 内置规则：禁止回访已访问过的节点。
/// 新增规则（需要某个天赋 / 需要多少铜钱 / 战斗评级达到 X / 必须在某个时间窗内……）
/// = 写一个实现并 <c>FakeRouteRuntime.RegisterChoiceValidator(...)</c> 注册。
///
/// 判定不通过时会给出**原因**，原因会显示在按钮副标题上（玩家能看到为什么点不了），
/// 点击也会被拒绝。
/// </summary>
public interface IRouteChoiceValidator
{
    /// <summary>该出口当前是否可选。</summary>
    /// <param name="choice">出口配置。</param>
    /// <param name="fromNode">出口所在节点。</param>
    /// <param name="reason">不可选的原因（可选时为 null / 空）。</param>
    bool IsSelectable(FakeRouteChoiceConfig choice, FakeRouteNodeConfig fromNode, out string reason);
}
