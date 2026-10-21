using UnityEngine;

/// <summary>
/// **奖励门控与结算**的实现（<see cref="IRewardGate"/> 角色）。
///
/// 从 <c>StageController</c> 拆出。职责边界：
///   • 本类管「现在是不是在等奖励」「这一局怎么结算」；
///   • 「一场战斗怎么开、怎么停」归 <see cref="StageCombatStaging"/>，
///     「这一局怎么开始/重开/退出」归 <see cref="StageRunFlow"/>；
///   • 需要的环境通过 <see cref="IStageRoleContext"/> 拿，不直接依赖 <c>StageController</c>。
/// </summary>
public sealed class StageRewardGate : IRewardGate
{
    private readonly IStageRoleContext _ctx;
    private readonly StageRouteBattleHost _host;

    public StageRewardGate(IStageRoleContext ctx, StageRouteBattleHost host)
    {
        _ctx = ctx;
        _host = host;
    }

    // ───────── IRewardGate ─────────

    public bool IsRouteRewardWaiting => _host.IsRouteRewardWaiting;

    public event System.Action CombatNodeCleared
    {
        add => _ctx.CombatNodeCleared += value;
        remove => _ctx.CombatNodeCleared -= value;
    }

    /// <summary>进入「等待奖励选择」状态（此时输入禁用、不算战斗）。</summary>
    public void SetRouteRewardWaitState()
    {
        _host.SetRouteRewardWaitState();
    }

    /// <summary>当前战斗节点完成。</summary>
    public void CompleteCurrentCombatNode()
    {
        if (_ctx.CurrentState != StageState.InProgress) return;
        _ctx.RaiseCombatNodeCleared();
    }

    /// <summary>旧版（RouteV2）路线结算。stageId 由调用方给出，避免引用 V2 的配置类型。</summary>
    public void CompleteRouteStage(int stageId, int clearReward)
    {
        if (!_host.RouteBattleRunning || _ctx.RouteStageSettled) return;
        _ctx.RouteStageSettled = true;
        AudioManager.Instance?.StopBGM();
        _ctx.SetStageState(StageState.Victory);
        StageRouteBattleHost.ClearEnemyProjectiles();
        if (clearReward > 0) _ctx.PlayerState?.AddCoins(clearReward);
        if (stageId > 0) SaveManager.MarkStageCleared(stageId);
        _ctx.SettleCoins();
        _ctx.RaiseStageVictory();
    }

    /// <summary>卷轴/假移动路线整关结算：整关奖励 + 标记通关 + 铜钱结算，只执行一次。</summary>
    public void CompleteFakeRouteStage(int clearReward)
    {
        var routeConfig = _ctx.RouteConfig;
        Debug.Log($"[Tutorial] 结算入口: config={(routeConfig != null ? routeConfig.stageName : "NULL")} 已结算={_ctx.RouteStageSettled} 奖励={clearReward} 玩家={(_ctx.PlayerState != null)}");
        if (routeConfig == null || _ctx.RouteStageSettled) return;
        _ctx.RouteStageSettled = true;
        AudioManager.Instance?.StopBGM();
        _ctx.SetStageState(StageState.Victory);
        StageRouteBattleHost.ClearEnemyProjectiles();
        if (clearReward > 0) _ctx.PlayerState?.AddCoins(clearReward);
        SaveManager.MarkStageCleared(routeConfig.stageId);
        _ctx.SettleCoins();
        _ctx.RaiseStageVictory();
    }
}
