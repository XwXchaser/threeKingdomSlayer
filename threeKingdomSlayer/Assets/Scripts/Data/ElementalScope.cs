/// <summary>
/// 元素一次性 BUFF 的作用域。与招式资产的 ColumnMode 配合做合规判定。
///
/// 2026-10-22（asmdef 分层第一批）：从 <c>Core/ElementalBuffController.cs</c> 抽出到数据层
/// —— 纯枚举，玩家侧（破冰会话）与招式资产都要用。
/// </summary>
public enum ElementalScope
{
    /// <summary>只对单列重攻击生效（初始版）</summary>
    SingleColumnHeavy = 0,
    /// <summary>任意重攻击生效（局外解锁版）</summary>
    AnyHeavy = 1
}
