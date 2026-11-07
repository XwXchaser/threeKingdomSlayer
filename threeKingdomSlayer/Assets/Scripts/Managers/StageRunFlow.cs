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
        // 「解锁招式后首次进关」的引导不在这里弹：
        // 它现在是**教学拍**（由 TutorialStageDirector 在开波时拍出来，和教点戳同一条机制）。
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
            var fakeRuntime = FakeRouteRuntime.Instance;
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
        var killRewardManager = KillRewardManager.Instance;
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
        var fakeRuntime = FakeRouteRuntime.Instance;
        if (fakeRuntime != null)
            fakeRuntime.HandleStageDefeat();
    }

    /// <summary>返回落地页：重载 Battle 场景，FakeRouteRuntime 会重新自举回 Ready。结算铜钱始终先做。</summary>
    public void GoToMainMenu()
    {
        AudioManager.Instance?.StopBGM();
        CleanupRouteStage(true);
        _ctx.SettleCoins();

        // 2026-10-27：**不再**置 StartFromCheckpoint。
        // 退出关卡后重新进入 = 从**关卡开头**开始（与 FakeRouteRuntime.ResolveStartNode 的注释意图一致）。
        // 之前这里置了位，而那个静态开关又永不复位 → 之后每次进关都从上一个节点续跑
        // （用户反馈「重进游戏直接落在 N3/E0，关卡前段被跳过」）。
        // 明确要从存档点续玩的地方（如「重来」）自己置位，并确保只消费一次。

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
        _ctx.RunCoroutine(FlushChoicesAfterExpSettled(waveIndex, false));
    }

    /// <summary>
    /// **等经验宝石落地之后再 flush 待处理的三选一**（2026-11-07 修：弹窗晚一波）。
    ///
    /// 根因：**击杀不给经验** —— 经验是「击杀 → 经验宝石飞到经验条 → 落地才结算」
    ///（<c>EnemyManager.OnEnemyDied</c> 只负责生成宝石）。于是：
    ///   本波最后一只怪死 → 波末 flush（此刻经验还没到，队列是空的）→ 下一波开打
    ///   → 宝石落地 → 升级事件发生 → 升级三选一被**顺延到下一波末**才弹。
    /// 实测：教学 E0 的三选一因此晚一波弹出 → 玩家拿到三选一时「用技能打断」那一拍已经过去了。
    ///
    /// 做法：波末先等宝石飞完（<c>ExpGemManager.IsCollecting</c>），再 flush → 升级事件已经入队 ✓。
    /// </summary>
    private System.Collections.IEnumerator FlushChoicesAfterExpSettled(int waveIndex, bool isLastWave)
    {
        float timeout = 3f;
        while (ExpGemManager.Instance != null && ExpGemManager.Instance.IsCollecting && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
        {
            // 波末统一结算待处理的选择（2026-11-05 用户定）：
            // 升级不再当场弹窗打断战斗，而是攒到本波清空时一起给。
            ucm.FlushPendingChoices();
            if (ucm.IsChoosing)
            {
                if (isLastWave)
                {
                    Debug.Log("[StageController] 关卡结束前还有待处理选择 → 等选完再结算");
                    ucm.OnAllChoicesDone += CompleteStageAfterChoices;
                }
                else
                {
                    Debug.Log($"[StageController] 第{waveIndex + 1}波清空，有待处理选择，暂缓下一波");
                    ucm.OnAllChoicesDone += OnChoicesDoneSpawnNextWave;
                }
                yield break;
            }
        }

        if (isLastWave)
        {
            CompleteStageNow();
            yield break;
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
        var cm = ColumnManager.Instance;
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

        // 关卡结算前：如果还有待处理的三选一（波末弹的升级三选一），**先让玩家选完再结算**。
        // 否则升级会在结算之后才生效（等于白送），这一级的成长也接不上。
        // ⚠️ 同样要**等经验宝石落地**：否则最后一波的经验到得太晚 → 升级三选一被推到结算之后。
        _ctx.RunCoroutine(FlushChoicesAfterExpSettled(-1, true));
    }

    /// <summary>选择完成 → 继续结算（避免结算与弹窗撞车）。</summary>
    private void CompleteStageAfterChoices()
    {
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null) ucm.OnAllChoicesDone -= CompleteStageAfterChoices;
        CompleteStageNow();
    }

    /// <summary>真正的结算（胜利 / 铜钱 / 存档）。</summary>
    private void CompleteStageNow()
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
            // 聚财（天赋）：铜钱获取 +30%
            _ctx.PlayerState?.AddCoins(Mathf.RoundToInt(stageConfig.clearCoinReward * TalentCapabilities.CoinGainMultiplier));
        }
        if (stageConfig != null)
            SaveManager.MarkStageCleared(stageConfig.stageId);
        _ctx.SettleCoins();

        _ctx.RaiseStageVictory();
    }

    /// <summary>玩家阵亡 → 关卡失败</summary>
    public void OnPlayerDefeated()
    {
        // 路线通知必须先于状态检查：PlayerState 会先把自身与 StageController 同步为 Defeat，
        // 若在此之前按状态拦截，路线会永远停在 Battle，且「重来」无法从存档点接回。
        // HandleStageDefeat 重复调用无害（递增 generation、停止协程），重复调用无副作用。
        var fakeRuntime = FakeRouteRuntime.Instance;
        if (fakeRuntime != null)
        {
            fakeRuntime.HandleStageDefeat();
        }

        if (_ctx.CurrentState == StageState.Defeat || _ctx.CurrentState == StageState.Victory) return;

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
        // 聚财（天赋）：铜钱获取 +30%
        _ctx.PlayerState?.AddCoins(Mathf.RoundToInt(enemy.coinReward * TalentCapabilities.CoinGainMultiplier));
    }
}
