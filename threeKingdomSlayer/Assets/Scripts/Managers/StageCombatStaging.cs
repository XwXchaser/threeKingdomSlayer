using UnityEngine;

/// <summary>
/// **一场战斗的布场与开打**的实现（<see cref="ICombatStaging"/> 角色）。
///
/// 从 <c>StageController</c> 拆出。职责边界：
///   • 本类管「某个节点战斗怎么开、怎么停、怎么切场景」，
///     真正的开打/旅行/等奖励状态机在 <c>StageRouteBattleHost</c>（本类持有它）；
///   • 「这一局怎么开始/重开/退出」归 <see cref="StageRunFlow"/>，
///     「奖励门控与结算」归 <see cref="StageRewardGate"/>；
///   • 需要的环境通过 <see cref="IStageRoleContext"/> 拿，不直接依赖 <c>StageController</c>。
/// </summary>
public sealed class StageCombatStaging : ICombatStaging
{
    private readonly IStageRoleContext _ctx;
    private readonly StageRouteBattleHost _host;

    public StageCombatStaging(IStageRoleContext ctx, StageRouteBattleHost host)
    {
        _ctx = ctx;
        _host = host;
    }

    // ───────── ICombatStaging ─────────

    public bool AutoStartStage
    {
        get => _ctx.AutoStartStage;
        set => _ctx.AutoStartStage = value;
    }

    public bool IsRouteCombatActive => _host.IsRouteCombatActive;

    public event System.Action RouteBattleCompleted
    {
        add => _ctx.RouteBattleCompleted += value;
        remove => _ctx.RouteBattleCompleted -= value;
    }

    /// <summary>装上当前节点的战斗配置。</summary>
    public void SetCurrentNodeBattleConfig(StageConfig config)
    {
        _ctx.StageConfig = config;
    }

    /// <summary>开始某节点的一场战斗。</summary>
    public void StartRouteBattle(StageConfig config)
    {
        if (config == null) return;
        _host.StartRouteBattle(config);
    }

    /// <summary>开始当前路线节点（可顺带重置玩家）。</summary>
    public void StartCurrentRouteNode(bool resetPlayer = false)
    {
        if (_ctx.StageConfig == null) return;
        _host.StopCombatSystemsForNodeTransition();
        StartNodeCombat(resetPlayer);
    }

    /// <summary>停止战斗（进入节点间旅行前）。</summary>
    public void StopCombatForRouteTravel()
    {
        _host.StopCombatForRouteTravel();
    }

    /// <summary>中止当前这场战斗（开发者跳过战斗用 ✅；不动关卡状态 ✅）。</summary>
    public void AbortRouteBattle()
    {
        _host.AbortRouteBattle();
    }

    /// <summary>进入节点间旅行状态。</summary>
    public void SetRouteTravelState()
    {
        _host.SetRouteTravelState();
    }

    /// <summary>回到战斗场景（重新装载 Battle 场景）。</summary>
    public void GoToBattleScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(_ctx.BattleSceneName);
    }

    // ───────── 内部 ─────────

    private void StartNodeCombat(bool resetPlayer)
    {
        if (_ctx.StageConfig == null) return;
        if (resetPlayer) _ctx.PlayerState?.ResetPlayer();
        UltimateSystem.Instance?.ResetEnergy();
        var killRewardManager = UnityEngine.Object.FindObjectOfType<KillRewardManager>();
        killRewardManager?.ResetRewards();
        _host.PrewarmEnemyPools();
        _ctx.SetStageState(StageState.InProgress);
        AudioManager.Instance?.PlayDefaultBGM();
        _ctx.WaveSpawner?.StartWaveSpawning();
    }
}
