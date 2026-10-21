using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 路线战斗宿主：一场「节点战斗」的开关与状态迁移（开打 / 旅行 / 等奖励 / 节点推进）。
///
/// 从 <c>StageController</c> 拆出。职责边界：
///   • 本类只管「这一场节点战斗处于什么状态」以及状态迁移时该做什么
///     （停波、清场、重置连击/冷却、开BGM、起波次、触发战斗开始被动）；
///   • 关卡生命周期（结算、通关、返回落地页）与事件广播仍归 <c>StageController</c>；
///   • 需要的环境（当前关卡配置、状态读写、波次/敌人/玩家/对象池）通过
///     <see cref="IContext"/> 拿，不直接依赖 <c>StageController</c>。
///
/// 状态位（路线战斗是否开启、是否在等奖励）随逻辑一起搬到本类，
/// 避免「战斗开关」散落在关卡门面里。
/// </summary>
public sealed class StageRouteBattleHost
{
    /// <summary>宿主需要的环境（由 StageController 实现）。</summary>
    public interface IContext
    {
        /// <summary>当前关卡配置（节点切换时会变）。</summary>
        StageConfig StageConfig { get; set; }

        /// <summary>当前关卡状态（只读）。</summary>
        StageState CurrentState { get; }

        /// <summary>设置关卡状态。</summary>
        void SetStageState(StageState state);

        /// <summary>波次生成器。</summary>
        WaveSpawner WaveSpawner { get; }

        /// <summary>敌人管理器。</summary>
        EnemyManager EnemyManager { get; }

        /// <summary>玩家状态。</summary>
        PlayerState PlayerState { get; }

        /// <summary>敌人对象池。</summary>
        EnemyPool EnemyPool { get; }
    }

    private readonly IContext _ctx;

    public StageRouteBattleHost(IContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>路线战斗是否已开启（节点战斗开始 → 结算前）。</summary>
    public bool RouteBattleRunning { get; private set; }

    /// <summary>是否正在等待奖励选择（三选一）。</summary>
    public bool RewardWaiting { get; private set; }

    /// <summary>是否处于「等待奖励」阶段。</summary>
    public bool IsRouteRewardWaiting => RewardWaiting;

    /// <summary>是否处于路线战斗进行中（战斗已开、未在等奖励、关卡在进行中）。</summary>
    public bool IsRouteCombatActive => RouteBattleRunning
        && _ctx.CurrentState == StageState.InProgress
        && !RewardWaiting;

    /// <summary>开始一场节点战斗：开战斗位、换当前关卡配置、清场、开BGM、起波次。</summary>
    public void StartRouteBattle(StageConfig config)
    {
        if (config == null) return;
        Debug.Log($"[RouteDiag] StartRouteBattle frame={Time.frameCount} nodeConfig={config.name} routeCombatBefore={IsRouteCombatActive} burnStates=" + EnemyStatusBurn.CountActive());
        RouteBattleRunning = true;
        RewardWaiting = false;
        _ctx.StageConfig = config;
        StopCombatSystemsForNodeTransition();
        PrewarmEnemyPools();
        _ctx.SetStageState(StageState.InProgress);
        AudioManager.Instance?.PlayDefaultBGM();
        _ctx.WaveSpawner?.StartWaveSpawning();
        Debug.Log($"[RouteDiag] StartRouteBattle after wave spawn frame={Time.frameCount} routeCombat={IsRouteCombatActive} enemies={AttackSystem.Instance?.columnManager?.GetAllEnemies()?.Count}");
        TimedPassiveModule.Instance?.TriggerPendingCombatStartEffects();
        SpikeTrapController.Instance?.SetRouteVisibility(true);
    }

    /// <summary>进入节点间旅行：停战斗、准备非战斗被动、隐藏尖刺、状态回 Starting。</summary>
    public void SetRouteTravelState()
    {
        Debug.Log($"[RouteDiag] SetRouteTravelState frame={Time.frameCount} routeCombat={IsRouteCombatActive} burnStates=" + EnemyStatusBurn.CountActive());
        RewardWaiting = false;
        StopCombatSystemsForNodeTransition();
        TimedPassiveModule.Instance?.PrepareForNonCombat();
        SpikeTrapController.Instance?.SetRouteVisibility(false);
        _ctx.SetStageState(StageState.Starting);
    }

    /// <summary>进入等待奖励：不再生成新波次，并取消正在播放的大招表现。</summary>
    public void SetRouteRewardWaitState()
    {
        RewardWaiting = true;
        // 奖励等待阶段：不再生成新波次，并取消正在播放的大招表现；
        // 保留死亡动画与延迟奖励回调直到玩家离开本节点。
        _ctx.WaveSpawner?.StopSpawning();
        UltimateSystem.Instance?.CancelUltimate();
    }

    /// <summary>旅行前停止战斗表现（仅对旧版路线模式有效）。</summary>
    public void StopCombatForRouteTravel()
    {
        StopCombatSystemsForNodeTransition();
        _ctx.SetStageState(StageState.Starting);
    }

    /// <summary>重置路线战斗状态位（重开关卡 / 重开时用）。</summary>
    public void ResetRouteBattleState()
    {
        RouteBattleRunning = false;
        RewardWaiting = false;
    }

    /// <summary>节点切换时停止一切战斗系统并重置连击/冷却等。</summary>
    public void StopCombatSystemsForNodeTransition()
    {
        _ctx.WaveSpawner?.StopSpawning();
        _ctx.EnemyManager?.ClearAllEnemies();
        ClearEnemyProjectiles();
        ComboManager.Instance?.ResetCombo();
        HealthPotionManager.Instance?.ResetForNewStage();
        _ctx.PlayerState?.ResetCooldownsForNodeTransition();
        ActiveSkillInventory.Instance?.ResetCooldowns();
        TimedPassiveModule.Instance?.ResetTimers();
    }

    /// <summary>预热当前关卡会用到的敌人对象池。</summary>
    public void PrewarmEnemyPools()
    {
        var pool = _ctx.EnemyPool;
        var config = _ctx.StageConfig;
        if (config == null || pool == null) return;

        var enemyIds = new HashSet<int>();
        foreach (var wave in config.waves)
            CollectEnemyIds(wave, enemyIds);

        foreach (int enemyId in enemyIds)
            pool.PrewarmPool(enemyId, pool.defaultPoolSize);
    }

    private static void CollectEnemyIds(WaveConfig wave, HashSet<int> enemyIds)
    {
        if (wave == null || wave.rows == null)
            return;

        foreach (var row in wave.rows)
        {
            if (row == null || row.enemyIds == null)
                continue;

            foreach (int enemyId in row.enemyIds)
            {
                if (enemyId > 0 && enemyId != RowConfig.RhythmGateMarker)
                    enemyIds.Add(enemyId);
            }
        }
    }

    /// <summary>清掉场上所有敌方投射物。</summary>
    public static void ClearEnemyProjectiles()
    {
        var projectiles = Resources.FindObjectsOfTypeAll<EnemyProjectile>();
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i] == null) continue;
            if (projectiles[i].gameObject.scene.IsValid())
            {
                projectiles[i].gameObject.SetActive(false);
                UnityEngine.Object.Destroy(projectiles[i].gameObject);
            }
        }
    }
}
