using System;

/// <summary>
/// 路线流程对「关卡宿主」的唯一要求（端口接口 / Ports &amp; Adapters）。
///
/// 依赖方向：由**使用方**（<see cref="FakeRouteRuntime"/>，路线层）声明自己需要什么，
/// 由**实现方**（<c>StageController</c>，应用层）去实现。这样路线层不再直接认识
/// `StageController` 这个全局门面，后续换成别的宿主（测试替身、另一种关卡类型）
/// 也不需要改路线流程。
///
/// 构成 = 三个角色接口的合计：
/// <list type="bullet">
/// <item><see cref="IRunFlow"/>：这一局的流程（路线配置、开始/重开/退出、战败策略）。</item>
/// <item><see cref="ICombatStaging"/>：一场战斗的布场与开打。</item>
/// <item><see cref="IRewardGate"/>：奖励门控与结算。</item>
/// </list>
///
/// 历史：这里原先是一个自成一体的宽端口 <c>IRouteBattleHost</c>，成员与
/// <c>IStageFlowControl</c> / <c>IStageBattleState</c> 有重叠。2026-10-22 起
/// 宽端口拆成上面三个窄角色，本接口只做合成——路线层仍然只持有一个字段，
/// 但每个能力都归到了唯一的角色里。
/// </summary>
public interface IRouteStageHost : IRunFlow, ICombatStaging, IRewardGate
{
}
