/// <summary>
/// Buff 类型枚举。
///
/// 2026-10-22（asmdef 分层第一批）：从 <c>Player/PlayerState.cs</c> 抽出到数据层
/// —— 纯枚举，敌人侧（击飞判定）与玩家侧都要用，放在 <c>Locus.Data</c> 才能被下层程序集引用。
/// </summary>
public enum BuffType
{
    ForceLaunch,       // 强制击飞：CanBeLaunched 始终返回 true
    ProbabilityLaunch  // 概率击飞：攻击时按概率强制击飞
}
