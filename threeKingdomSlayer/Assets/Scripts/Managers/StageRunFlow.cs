using System.Collections;
using UnityEngine;

/// <summary>
/// **一局流程**的实现（<see cref="IRunFlow"/> 角色）。
///
/// 从 <c>StageController</c> 拆出。职责边界：
///   • 本类管「这一局怎么开始 / 怎么重开 / 怎么退出 / 现在是什么状态」，
///     以及关卡内部的波次推进与胜负判定；
///   • 「一场节点战斗怎么开、怎么停」归 <see cref="StageCombatStaging"/>，
///     「奖励门控与结算」归 <see cref="StageRewardGate"/>；
///   • 需要的环境（当前配置、状态读写、波次·敌人·玩家·对象池、事件广播）通过
///     <see cref="IStageRoleContext"/> 拿，不直接依赖 <c>StageController</c>。
/// </summary>
public sealed class StageRunFlow : IRunFlow
{
    private readonly IStageRoleContext _ctx;

    public StageRunFlow(IStageRoleContext ctx)
    {
        _ctx = ctx;
    }

    // ───────── IRunFlow：状态 ─────────

    public StageState CurrentState => _ctx.CurrentState;

    public bool IsStageInProgress => _ctx.CurrentState == StageState.InProgress;

    public bool IsStageVictory => _ctx.CurrentState == StageState.Victory;

    public bool IsStageDefeat => _ctx.CurrentState == StageState.Defeat;

    public bool BlockRestartOnDefeat
    {
        get => _ctx.BlockRestartOnDefeat;
        set => _ctx.BlockRestartOnDefeat = value;
    }

    public bool SuppressDefeatUI => _ctx.SuppressDefeatUI;

    public FakeRouteStageConfig RouteConfig
    {
        get => _ctx.RouteConfig;
        set => _ctx.RouteConfig = value;
    }

    public StageConfig StageConfig => _ctx.StageConfig;

    public event System.Action StageDefeat
    {
        add => _ctx.StageDefeat += value;
        remove => _ctx.StageDefeat -= value;
    }

    // ───────── 生命周期 ─────────

    /// <summary>首帧之后再开打（协程用 unscaled 时间，避免同帧暂停把启动卡住）。</summary>
    public IEnumerator StartStageNextFrame()
    {
        yield return null;
        Time.timeScale = 1f;
        StartStage();
    }

    /// <summary>开始关卡。</summary>
    public void StartStage()
    {
        var routeConfig = _ctx.RouteConfig;
        if (routeConfig != null)
        {
            _ctx.RouteStageSettled = false;
            _ctx.PlayerState?.ResetPlayer();
            UltimateSystem.Instance?.ResetEnergy();
            _ctx.EnemyManager?.ClearAllEnemies();
            var fakeRuntime = UnityEngine.Object.FindObjectOfType<FakeRouteRuntime>();
            if (fakeRuntime == null)
            {
                Debug.LogError("[StageController] 卷轴路线运行时缺失（FakeRouteRuntime）");
                return;
            }
            fakeRuntime.Begin(routeConfig);
            return;
        }

        var stageConfig = _ctx.StageConfig;
        if (stageConfig == null)
        {
            Debug.LogError("[StageController] 当前节点StageConfig未赋值，无法开始关卡");
            return;
        }

        Debug.Log($"[StageController] 开始战斗节点: {stageConfig.stageName}");

        // 重置所有状态
        _ctx.PlayerState?.ResetPlayer();
        UltimateSystem.Instance?.ResetEnergy();
        _ctx.EnemyManager?.ClearAllEnemies();

        // 重置击杀奖励管理器
        var killRewardManager = UnityEngine.Object.FindObjectOfType<KillRewardManager>();
        killRewardManager?.ResetRewards();

        // 重置连击管理器
        ComboManager.Instance?.ResetCombo();

        // 重置血包掉落
        HealthPotionManager.Instance?.ResetForNewStage();

        // 预创建对象池
        _ctx.PrewarmEnemyPools();

        // 设置关卡状态为进行中
        _ctx.SetStageState(StageState.InProgress);

        // 播放 BGM
        AudioManager.Instance?.PlayDefaultBGM();

        // 开始生成波次
        _ctx.WaveSpawner?.StartWaveSpawning();
    }

    /// <summary>重新开始关卡（战败后的「重来」）。</summary>
    public void RestartStage()
    {
        _ctx.RunCoroutine(RestartStageCoroutine());
    }

    private IEnumerator RestartStageCoroutine()
    {
        if (_ctx.RouteConfig != null)
        {
            // 卷轴路线重开：清场 → 重置玩家 → 重新 Begin；是否从存档点恢复由 FakeRouteLaunch.StartFromCheckpoint 决定。
            _ctx.EnemyManager?.ClearAllEnemies();
            _ctx.EnemyPool?.ClearAllPools();
            _ctx.PlayerState?.ResetPlayer();
            yield return null;
            StartStage();
            yield break;
        }

        // 兜底：没有卷轴路线配置时按普通关卡重开。
        _ctx.EnemyManager?.ClearAllEnemies();
        _ctx.EnemyPool?.ClearAllPools();
        yield return null;
        StartStage();
    }

    /// <summary>收尾当前路线战斗（战败 / 退出时清理）。</summary>
    public void CleanupRouteStage(bool unloadScene)
    {
        var fakeRuntime = UnityEngine.Object.FindObjectOfType<FakeRouteRuntime>();
        if (fakeRuntime != null)
            fakeRuntime.HandleStageDefeat();
    }

    /// <summary>返回落地页：重载 Battle 场景，FakeRouteRuntime 会重新自举回 Ready。结算铜钱始终先做。</summary>
    public void GoToMainMenu()
    {
        AudioManager.Instance?.StopBGM();
        CleanupRouteStage(true);
        _ctx.SettleCoins();

        // 回落地页要接上次进度：中途退出 → 用 checkpoint 节点；已通关（snapshot 已被 FinishRoute 清掉）→ 回起点。
        FakeRouteLaunch.StartFromCheckpoint = true;

        string battleSceneName = _ctx.BattleSceneName;
        if (string.IsNullOrEmpty(battleSceneName))
        {
            Debug.LogError("[StageController] battleSceneName 为空，无法回落地页");
            return;
        }

        // 路线场景（YJunctionSample）是 additive 加载的，LoadScene 不会自动卸载它；
        // 新的 ScrollRoutePresenter.EnsureRoute 会直接复用已加载的那个场景并重新初始化。
        UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
    }

    // ───────── 关卡内的事件回调 ─────────

    /// <summary>单波清空 → 若有待处理的三选一（升级/道具），等待选择完成再生成下一波</summary>
    public void OnWaveCleared(int waveIndex)
    {
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null && ucm.IsChoosing)
        {
            Debug.Log($"[StageController] 第{waveIndex + 1}波清空，有待处理选择，暂缓下一波");
            ucm.OnAllChoicesDone += OnChoicesDoneSpawnNextWave;
            return;
        }

        Debug.Log($"[StageController] 第{waveIndex + 1}波清空，自动开始下一波");
        _ctx.WaveSpawner?.SpawnNextWave();
    }

    private void OnChoicesDoneSpawnNextWave()
    {
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
            ucm.OnAllChoicesDone -= OnChoicesDoneSpawnNextWave;

        // Existing scheduler-owned orders resume themselves when choices close.
        var cm = UnityEngine.Object.FindObjectOfType<ColumnManager>();
        if (cm != null)
        {
            DebugLog.Info("[BOSS_ADVANCE] StageController 调用TriggerAllBossFillForward");
            cm.TriggerAllBossFillForward();
        }

        Debug.Log("[StageController] 所有选择完成，开始下一波");
        _ctx.WaveSpawner?.SpawnNextWave();
    }

    /// <summary>所有波次已清空 → 关卡胜利</summary>
    public void OnAllWavesCleared()
    {
        if (_ctx.CurrentState != StageState.InProgress) return;

        if (_ctx.IsRouteBattleRunning)
        {
            _ctx.RaiseRouteBattleCompleted();
            return;
        }

        Debug.Log("[StageController] 最终战斗节点已清空，关卡胜利！");
        AudioManager.Instance?.StopBGM();
        _ctx.SetStageState(StageState.Victory);
        StageRouteBattleHost.ClearEnemyProjectiles();

        var stageConfig = _ctx.StageConfig;
        if (stageConfig != null)
        {
            _ctx.PlayerState?.AddCoins(stageConfig.clearCoinReward);
        }
        if (stageConfig != null)
            SaveManager.MarkStageCleared(stageConfig.stageId);
        _ctx.SettleCoins();

        _ctx.RaiseStageVictory();
    }

    /// <summary>玩家阵亡 → 关卡失败</summary>
    public void OnPlayerDefeated()
    {
        if (_ctx.CurrentState == StageState.Defeat || _ctx.CurrentState == StageState.Victory) return;

        var fakeRuntime = UnityEngine.Object.FindObjectOfType<FakeRouteRuntime>();
        if (fakeRuntime != null)
        {
            fakeRuntime.HandleStageDefeat();
        }

        // 取消待处理的选择完成回调（玩家已死，不再生成下一波）
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
            ucm.OnAllChoicesDone -= OnChoicesDoneSpawnNextWave;

        Debug.Log("[StageController] 玩家阵亡，关卡失败");
        AudioManager.Instance?.StopBGM();
        _ctx.SetStageState(StageState.Defeat);
        StageRouteBattleHost.ClearEnemyProjectiles();
        _ctx.RaiseStageDefeat();
    }

    /// <summary>玩家状态变化（同步门面的关卡状态，不广播事件）。</summary>
    public void OnPlayerStageStateChanged(StageState state)
    {
        _ctx.SyncStageStateSilently(state);
    }

    /// <summary>敌人死亡回调 - 增加击杀数和铜钱</summary>
    public void OnEnemyDied(Enemy enemy)
    {
        _ctx.PlayerState?.AddKill();
        _ctx.PlayerState?.AddCoins(enemy.coinReward);
    }
}
