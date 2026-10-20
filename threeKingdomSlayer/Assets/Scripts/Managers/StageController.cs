using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡流程控制器 - 单例
/// 管理关卡开始、进行中、胜利、失败状态
/// 协调 WaveSpawner、EnemyManager、PlayerState 之间的交互
/// </summary>
public class StageController : MonoBehaviour
{
    public static StageController Instance { get; private set; }

    [Header("关卡配置")]
    public StageConfig stageConfig;
    [Tooltip("Disable only when an external flow explicitly starts combat.")]
    public bool autoStartStage = true;
    public RouteStageConfig routeStageConfig;
    public RouteStageConfigV2 routeStageConfigV2;
    [Tooltip("卷轴/假移动路线关卡配置。为空时该流程不参与。")]
    public FakeRouteStageConfig fakeRouteStageConfig;

    [Header("组件引用")]
    public WaveSpawner waveSpawner;
    public EnemyManager enemyManager;
    public PlayerState playerState;
    public EnemyPool enemyPool;

    [Header("场景引用")]
    // MainMenu 已于 D4 删除；落地页（回 Ready）就靠重载 battleSceneName。
    public string battleSceneName = "Battle";

    // 运行时状态
    private StageState currentState = StageState.None;
    private bool _coinsSettled;
    private bool _routeRunInitialized;
    private bool _routeBattleRuntime;
    private bool _routeStageSettled;
    private bool _routeRewardWaiting;

    // 事件
    public System.Action<StageState> OnStageStateChanged;
    public System.Action OnStageVictory;
    public System.Action OnStageDefeat;

    [Tooltip("脚本化演出用（教学关初见杀）：置 true 时玩家阵亡仍会进 Defeat 状态与事件，但不接战败面板")]
    public bool suppressDefeatUI;
    [Tooltip("教学关：战败后不允许「重来」，只保留回主菜单（教学流程不重跑）")]
    public bool blockRestartOnDefeat;
    public System.Action OnCombatNodeCleared;
    public System.Action OnRouteBattleCompleted;

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 【架构收口】关卡配置只由运行时注入，不再读任何跨场景 Pending* 静态入口：
        //   • 落地页：FakeRouteRuntime.PrepareForEntry 写 fakeRouteStageConfig
        //   • 战斗：StartRouteBattle(stageConfig) 注入当前节点配置
        // 原来的四条分支（MainMenu 传参 / RouteV2 传参 / 线性关卡 / 旧路线）自 D4 删 MainMenu 起
        // 已无人写入，只是每次 Awake 把自己读出来再置 null，2026-10-20 批次 0 一并删除。
        // 注意：Hub 自举依赖此处保持 null，FakeRouteRuntime.Start 才能走「读存档自举」分支。

        if (fakeRouteStageConfig != null)
        {
            Debug.Log($"[StageController] 加载卷轴路线关卡: {fakeRouteStageConfig.stageName} (stageId={fakeRouteStageConfig.stageId})");
        }
        else if (routeStageConfig != null)
        {
            Debug.Log($"[StageController] 加载路线关卡: {routeStageConfig.stageName} (stageId={routeStageConfig.stageId})");
        }
        else if (stageConfig != null)
        {
            Debug.Log($"[StageController] 加载线性关卡: {stageConfig.stageName} (stageId={stageConfig.stageId})");
        }
        else
        {
            Debug.LogWarning("[StageController] 未设置线性或路线关卡配置");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // 自动查找组件引用
        if (waveSpawner == null) waveSpawner = FindObjectOfType<WaveSpawner>();
        if (enemyManager == null) enemyManager = FindObjectOfType<EnemyManager>();
        if (playerState == null) playerState = FindObjectOfType<PlayerState>();
        if (enemyPool == null) enemyPool = FindObjectOfType<EnemyPool>();

        // 注册事件
        if (waveSpawner != null)
        {
            waveSpawner.OnAllWavesCompleted += OnAllWavesCleared;
            waveSpawner.OnWaveCompleted += OnWaveCleared;
        }
        if (enemyManager != null)
        {
            enemyManager.OnAnyEnemyDied += OnEnemyDied;
        }
        if (playerState != null)
        {
            playerState.OnPlayerDied += OnPlayerDefeated;
            playerState.OnStageStateChanged += OnPlayerStageStateChanged;
        }

        // 域重载或上一局暂停状态可能保留 timeScale=0；必须在安排首局启动前恢复，
        // 否则 Invoke 的缩放时间永远不会到达，关卡会停在 None。
        Time.timeScale = 1f;

        // 自动开始关卡：协程使用 unscaled 时间，避免其他 Start 在同帧暂停时让启动永久卡住。
        Debug.Log("[StageController] 场景加载完成，自动开始关卡");
        if (autoStartStage) StartCoroutine(StartStageNextFrame());
    }

    private IEnumerator StartStageNextFrame()
    {
        yield return null;
        Time.timeScale = 1f;
        StartStage();
    }

    #region 关卡流程

    /// <summary>
    /// 开始关卡
    /// </summary>
    public void StartStage()
    {
        if (fakeRouteStageConfig != null)
        {
            _routeStageSettled = false;
            playerState?.ResetPlayer();
            UltimateSystem.Instance?.ResetEnergy();
            enemyManager?.ClearAllEnemies();
            var fakeRuntime = FindObjectOfType<FakeRouteRuntime>();
            if (fakeRuntime == null)
            {
                Debug.LogError("[StageController] 卷轴路线运行时缺失（FakeRouteRuntime）");
                return;
            }
            fakeRuntime.Begin(fakeRouteStageConfig);
            return;
        }

        if (routeStageConfigV2 != null)
        {
            _routeStageSettled = false;
            var checkpoint = RouteStageV2Launch.StartFromCheckpoint ? SaveManager.GetRouteStageSnapshot(routeStageConfigV2.stageId) : null;
            if (checkpoint == null)
            {
                Debug.Log("[RouteV2] no checkpoint: resetting full player state");
                playerState?.ResetPlayer();
                Debug.Log("[RouteV2] no checkpoint reset result health=" + (playerState != null ? playerState.currentHealth.ToString("F1") : "NULL") + " level=" + (playerState != null ? playerState.currentLevel.ToString() : "NULL") + " upgrades=" + (playerState != null ? playerState.acquiredUpgrades.Count.ToString() : "NULL"));
            }
            UltimateSystem.Instance?.ResetEnergy();
            enemyManager?.ClearAllEnemies();
            var v2Runtime = FindObjectOfType<RouteStageRuntimeV2>();
            if (v2Runtime == null)
            {
                Debug.LogError("[StageController] V2路线运行时组件缺失");
                return;
            }
            v2Runtime.Begin(routeStageConfigV2);
            return;
        }

        if (routeStageConfig != null)
        {
            var routeController = RouteProgressionController.Instance;
            string routeError = string.Empty;
            if (routeController == null || !routeController.TryInitialize(routeStageConfig, out routeError))
            {
                Debug.LogError("[StageController] 路线配置无效: " + routeError);
                return;
            }
            return;
        }

        if (stageConfig == null)
        {
            Debug.LogError("[StageController] 当前节点StageConfig未赋值，无法开始关卡");
            return;
        }

        Debug.Log($"[StageController] 开始战斗节点: {stageConfig.stageName}");

        // 重置所有状态
        playerState?.ResetPlayer();
        UltimateSystem.Instance?.ResetEnergy();
        enemyManager?.ClearAllEnemies();

        // 重置击杀奖励管理器
        var killRewardManager = FindObjectOfType<KillRewardManager>();
        killRewardManager?.ResetRewards();

        // 重置连击管理器
        ComboManager.Instance?.ResetCombo();

        // 重置血包掉落
        HealthPotionManager.Instance?.ResetForNewStage();

        // 预创建对象池
        PrewarmEnemyPools();

        // 设置关卡状态为进行中
        SetState(StageState.InProgress);

        // 播放 BGM
        AudioManager.Instance?.PlayDefaultBGM();

        // 开始生成波次
        waveSpawner?.StartWaveSpawning();
    }

    /// <summary>
    /// 预创建敌人对象池
    /// </summary>
    private void PrewarmEnemyPools()
    {
        if (stageConfig == null || enemyPool == null) return;

        var enemyIds = new HashSet<int>();
        foreach (var wave in stageConfig.waves)
            CollectEnemyIds(wave, enemyIds);

        foreach (int enemyId in enemyIds)
            enemyPool.PrewarmPool(enemyId, enemyPool.defaultPoolSize);
    }

    private void CollectEnemyIds(WaveConfig wave, HashSet<int> enemyIds)
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

    /// <summary>
    /// 设置关卡状态
    /// </summary>
    private void SetState(StageState newState)
    {
        if (currentState == newState)
        {
            // 状态没变也必须把玩家侧状态同步回来：
            // StartStage 开头会 ResetPlayer() 把 playerState 打回 Starting，
            // 如果这里直接 return，playerState 就永远停在 Starting，
            // 后果是攻击/主动技能/道具全部被“非 InProgress”的门挡住（教学关就是这么没反应的）。
            playerState?.SetStageState(newState);
            return;
        }

        currentState = newState;
        OnStageStateChanged?.Invoke(newState);
        playerState?.SetStageState(newState);
    }

    #endregion

    #region 事件回调

    /// <summary>
    /// 单波清空 → 若有待处理的三选一（升级/道具），等待选择完成再生成下一波
    /// </summary>
    private void OnWaveCleared(int waveIndex)
    {
        if (routeStageConfig != null && RouteProgressionController.Instance != null)
        {
            Debug.Log("[StageController] 路线节点波次清空，路线控制器负责后续流程");
            return;
        }
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null && ucm.IsChoosing)
        {
            Debug.Log($"[StageController] 第{waveIndex + 1}波清空，有待处理选择，暂缓下一波");
            ucm.OnAllChoicesDone += OnChoicesDoneSpawnNextWave;
            return;
        }

        Debug.Log($"[StageController] 第{waveIndex + 1}波清空，自动开始下一波");
        waveSpawner?.SpawnNextWave();
    }

    private void OnChoicesDoneSpawnNextWave()
    {
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
            ucm.OnAllChoicesDone -= OnChoicesDoneSpawnNextWave;

        // Existing scheduler-owned orders resume themselves when choices close.
        var cm = FindObjectOfType<ColumnManager>();
        if (cm != null)
        {
            DebugLog.Info("[BOSS_ADVANCE] StageController 调用TriggerAllBossFillForward");
            cm.TriggerAllBossFillForward();
        }

        Debug.Log("[StageController] 所有选择完成，开始下一波");
        waveSpawner?.SpawnNextWave();
    }

    /// <summary>
    /// 所有波次已清空 → 关卡胜利
    /// </summary>
    private void OnAllWavesCleared()
    {
        if (currentState != StageState.InProgress) return;

        if (_routeBattleRuntime)
        {
            OnRouteBattleCompleted?.Invoke();
            return;
        }

        // 路线模式下，当前节点清空交给路线控制器；只有终点节点清空才是整关胜利。
        if (routeStageConfig != null && RouteProgressionController.Instance != null
            && RouteProgressionController.Instance.CurrentNode != routeStageConfig.finalNode)
        {
            Debug.Log("[StageDiag] OnAllWavesCleared route=" + (routeStageConfig != null ? routeStageConfig.name : "NULL") + " routeInstance=" + (RouteProgressionController.Instance != null ? RouteProgressionController.Instance.name + "#" + RouteProgressionController.Instance.GetInstanceID() : "NULL") + " currentNode=" + (RouteProgressionController.Instance != null && RouteProgressionController.Instance.CurrentNode != null ? RouteProgressionController.Instance.CurrentNode.name : "NULL") + " final=" + (routeStageConfig != null && routeStageConfig.finalNode != null ? routeStageConfig.finalNode.name : "NULL"));
            Debug.Log("[StageController] 当前路线节点已清空，等待路线控制器推进");
            OnCombatNodeCleared?.Invoke();
            return;
        }

        Debug.Log("[StageController] 最终战斗节点已清空，关卡胜利！");
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);
        ClearEnemyProjectiles();

        if (stageConfig != null)
        {
            playerState?.AddCoins(stageConfig.clearCoinReward);
        }
        if (routeStageConfig != null)
            playerState?.AddCoins(routeStageConfig.clearCoinReward);

        if (routeStageConfig != null)
            SaveManager.MarkStageCleared(routeStageConfig.stageId);
        else if (stageConfig != null)
            SaveManager.MarkStageCleared(stageConfig.stageId);
        SettleCoins();

        OnStageVictory?.Invoke();
    }

    /// <summary>
    /// 玩家阵亡 → 关卡失败
    /// </summary>
    private void OnPlayerDefeated()
    {
        if (currentState == StageState.Defeat || currentState == StageState.Victory) return;

        var fakeRuntime = FindObjectOfType<FakeRouteRuntime>();
        if (fakeRuntime != null)
        {
            fakeRuntime.HandleStageDefeat();
        }
        else
        {
            var v2Runtime = FindObjectOfType<RouteStageRuntimeV2>();
            if (v2Runtime != null)
                v2Runtime.HandleStageDefeat();
        }

        // 取消待处理的选择完成回调（玩家已死，不再生成下一波）
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
            ucm.OnAllChoicesDone -= OnChoicesDoneSpawnNextWave;

        Debug.Log("[StageController] 玩家阵亡，关卡失败");
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Defeat);
        ClearEnemyProjectiles();
        OnStageDefeat?.Invoke();
    }

    private void ClearEnemyProjectiles()
    {
        var projectiles = Resources.FindObjectsOfTypeAll<EnemyProjectile>();
        for (int i = 0; i < projectiles.Length; i++)
        {
            if (projectiles[i].gameObject.scene.IsValid())
            {
                projectiles[i].gameObject.SetActive(false);
                Destroy(projectiles[i].gameObject);
            }
        }
    }

    /// <summary>
    /// 玩家状态变化
    /// </summary>
    private void OnPlayerStageStateChanged(StageState state)
    {
        // 同步状态
        if (currentState != state)
        {
            currentState = state;
        }
    }

    /// <summary>
    /// 敌人死亡回调 - 增加击杀数和铜钱
    /// </summary>
    private void OnEnemyDied(Enemy enemy)
    {
        playerState?.AddKill();
        playerState?.AddCoins(enemy.coinReward);
    }

    #endregion

    #region 场景管理

    /// <summary>
    /// 重新开始关卡
    /// </summary>
    public void RestartStage()
    {
        StartCoroutine(RestartStageCoroutine());
    }

    public void CleanupRouteStage(bool unloadScene)
    {
        var fakeRuntime = FindObjectOfType<FakeRouteRuntime>();
        if (fakeRuntime != null)
            fakeRuntime.HandleStageDefeat();

        var runtimeV2 = FindObjectOfType<RouteStageRuntimeV2>();
        if (runtimeV2 != null)
            runtimeV2.CleanupRouteStage(unloadScene);
    }

    private IEnumerator RestartStageCoroutine()
    {
        if (fakeRouteStageConfig != null)
        {
            // 卷轴路线重开：清场 → 重置玩家 → 重新 Begin；是否从存档点恢复由 FakeRouteLaunch.StartFromCheckpoint 决定。
            enemyManager?.ClearAllEnemies();
            enemyPool?.ClearAllPools();
            playerState?.ResetPlayer();
            yield return null;
            StartStage();
            yield break;
        }

        Debug.Log("[RouteV2] RestartStage begin checkpointMode=" + RouteStageV2Launch.StartFromCheckpoint + " health=" + (playerState != null ? playerState.currentHealth.ToString("F1") : "NULL") + " level=" + (playerState != null ? playerState.currentLevel.ToString() : "NULL") + " upgrades=" + (playerState != null ? playerState.acquiredUpgrades.Count.ToString() : "NULL"));
        enemyManager?.ClearAllEnemies();
        enemyPool?.ClearAllPools();
        if (RouteStageV2Launch.StartFromCheckpoint)
        {
            playerState?.ResetPlayer();
            Debug.Log("[RouteV2] cleared death runtime state before checkpoint restore");
        }
        CleanupRouteStage(true);

        while (routeStageConfigV2 != null)
        {
            var routeScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(routeStageConfigV2.routeSceneName);
            if (!routeScene.IsValid() || !routeScene.isLoaded)
                break;
            yield return null;
        }

        // 等待一帧确保清理完成
        yield return null;

        StartStage();
    }

    /// <summary>
    /// 返回落地页：重载 Battle 场景，FakeRouteRuntime 会重新自举回 Ready。结算铜钱始终先做。
    /// （MainMenu 已随 D4 删除，不再有「回主菜单」分支。）
    /// </summary>
    public void GoToMainMenu()
    {
        AudioManager.Instance?.StopBGM();
        CleanupRouteStage(true);
        SettleCoins();

        // 回落地页要接上次进度：中途退出 → 用 checkpoint 节点；已通关（snapshot 已被 FinishRoute 清掉）→ 回起点。
        FakeRouteLaunch.StartFromCheckpoint = true;

        if (string.IsNullOrEmpty(battleSceneName))
        {
            Debug.LogError("[StageController] battleSceneName 为空，无法回落地页");
            return;
        }

        // 路线场景（YJunctionSample）是 additive 加载的，LoadScene 不会自动卸载它；
        // 新的 ScrollRoutePresenter.EnsureRoute 会直接复用已加载的那个场景并重新初始化。
        UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
    }

    /// <summary>
    /// 结算本局铜钱到存档（幂等：同一局只结算一次）
    /// </summary>
    private void SettleCoins()
    {
        if (_coinsSettled) return;
        if (playerState == null) return;

        int sessionCoins = playerState.coinCount;
        if (sessionCoins <= 0) return;

        int total = SaveManager.GetCoins() + sessionCoins;
        SaveManager.SetCoins(total);
        _coinsSettled = true;

        Debug.Log($"[StageController] 铜钱已结算: +{sessionCoins}, 总铜钱: {total}");
    }

    /// <summary>
    /// 进入战斗场景
    /// </summary>
    public void GoToBattleScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(battleSceneName);
    }

    /// <summary>卷轴/假移动路线整关结算：整关奖励 + 标记通关 + 铜钱结算，只执行一次。</summary>
    public void CompleteFakeRouteStage(int clearReward)
    {
        Debug.Log($"[Tutorial] 结算入口: config={(fakeRouteStageConfig != null ? fakeRouteStageConfig.stageName : "NULL")} 已结算={_routeStageSettled} 奖励={clearReward} 玩家={(playerState != null)}");
        if (fakeRouteStageConfig == null || _routeStageSettled) return;
        _routeStageSettled = true;
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);
        ClearEnemyProjectiles();
        if (clearReward > 0) playerState?.AddCoins(clearReward);
        SaveManager.MarkStageCleared(fakeRouteStageConfig.stageId);
        SettleCoins();
        OnStageVictory?.Invoke();
    }

    public void CompleteRouteStage(int clearReward)
    {
        if (!_routeBattleRuntime || _routeStageSettled) return;
        _routeStageSettled = true;
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);
        ClearEnemyProjectiles();
        if (clearReward > 0) playerState?.AddCoins(clearReward);
        if (routeStageConfigV2 != null)
            SaveManager.MarkStageCleared(routeStageConfigV2.stageId);
        SettleCoins();
        OnStageVictory?.Invoke();
    }

    public void SetCurrentNodeBattleConfig(StageConfig config)
    {
        stageConfig = config;
    }

    public void SetRouteTravelState()
    {
        Debug.Log($"[RouteDiag] SetRouteTravelState frame={Time.frameCount} routeCombat={IsRouteCombatActive} burnStates=" + (UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetBurnStateCountForDiagnostics().ToString() : "NULL"));
        _routeRewardWaiting = false;
        StopCombatSystemsForNodeTransition();
        TimedPassiveModule.Instance?.PrepareForNonCombat();
        SpikeTrapController.Instance?.SetRouteVisibility(false);
        SetState(StageState.Starting);
    }

    public void SetRouteRewardWaitState()
    {
        _routeRewardWaiting = true;
        // 奖励等待阶段：不再生成新波次，并取消正在播放的大招表现；
        // 保留死亡动画与延迟奖励回调直到玩家离开本节点。
        waveSpawner?.StopSpawning();
        UltimateSystem.Instance?.CancelUltimate();
    }

    public bool IsRouteRewardWaiting => _routeRewardWaiting;
    public bool IsRouteCombatActive => _routeBattleRuntime
        && currentState == StageState.InProgress
        && !_routeRewardWaiting;

    public void StartRouteBattle(StageConfig config)
    {
        if (config == null) return;
        Debug.Log($"[RouteDiag] StartRouteBattle frame={Time.frameCount} nodeConfig={config.name} routeCombatBefore={IsRouteCombatActive} burnStates=" + (UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetBurnStateCountForDiagnostics().ToString() : "NULL"));
        _routeBattleRuntime = true;
        _routeRewardWaiting = false;
        routeStageConfig = null;
        stageConfig = config;
        StopCombatSystemsForNodeTransition();
        PrewarmEnemyPools();
        SetState(StageState.InProgress);
        AudioManager.Instance?.PlayDefaultBGM();
        waveSpawner?.StartWaveSpawning();
        Debug.Log($"[RouteDiag] StartRouteBattle after wave spawn frame={Time.frameCount} routeCombat={IsRouteCombatActive} enemies={AttackSystem.Instance?.columnManager?.GetAllEnemies()?.Count}");
        TimedPassiveModule.Instance?.TriggerPendingCombatStartEffects();
        SpikeTrapController.Instance?.SetRouteVisibility(true);
    }

    public void StopCombatForRouteTravel()
    {
        if (routeStageConfig == null) return;
        StopCombatSystemsForNodeTransition();
        SetState(StageState.Starting);
    }

    public void StartCurrentRouteNode(bool resetPlayer = false)
    {
        if (routeStageConfig == null || stageConfig == null) return;
        StopCombatSystemsForNodeTransition();
        StartNodeCombat(resetPlayer);
    }

    private void StopCombatSystemsForNodeTransition()
    {
        waveSpawner?.StopSpawning();
        enemyManager?.ClearAllEnemies();
        ClearEnemyProjectiles();
        ComboManager.Instance?.ResetCombo();
        HealthPotionManager.Instance?.ResetForNewStage();
        playerState?.ResetCooldownsForNodeTransition();
        ActiveSkillInventory.Instance?.ResetCooldowns();
        TimedPassiveModule.Instance?.ResetTimers();
    }

    private void StartNodeCombat(bool resetPlayer)
    {
        if (stageConfig == null) return;
        if (resetPlayer) playerState?.ResetPlayer();
        UltimateSystem.Instance?.ResetEnergy();
        var killRewardManager = FindObjectOfType<KillRewardManager>();
        killRewardManager?.ResetRewards();
        PrewarmEnemyPools();
        SetState(StageState.InProgress);
        AudioManager.Instance?.PlayDefaultBGM();
        waveSpawner?.StartWaveSpawning();
    }



    public bool IsRouteMode => routeStageConfig != null;

    public RouteStageConfig CurrentRouteStageConfig => routeStageConfig;

    public void CompleteCurrentCombatNode()
    {
        if (routeStageConfig == null || CurrentState != StageState.InProgress) return;
        OnCombatNodeCleared?.Invoke();
    }

    #endregion

    #region 查询

    /// <summary>
    /// 获取当前关卡状态
    /// </summary>
    public StageState CurrentState => currentState;

    /// <summary>
    /// 关卡是否正在进行
    /// </summary>
    public bool IsStageInProgress => currentState == StageState.InProgress;

    /// <summary>
    /// 关卡是否胜利
    /// </summary>
    public bool IsStageVictory => currentState == StageState.Victory;

    /// <summary>
    /// 关卡是否失败
    /// </summary>
    public bool IsStageDefeat => currentState == StageState.Defeat;

    /// <summary>
    /// 获取指定列和排的X轴偏移量（梯形/扇形阵型）
    /// 使用缓存避免每帧遍历所有敌人，每0.2秒刷新一次
    ///
    /// BUG FIX: 使用 maxVisibleRows 作为固定的最大排数基准，而非动态的 cachedMaxRow。
    /// 之前使用动态 cachedMaxRow 导致当某列敌人减少时，maxRow 变小，
    /// 阵型公式中的 normalizedRow 变大，所有列向中间收缩。
    /// 现在使用固定的 maxVisibleRows，确保阵型位置始终稳定。
    /// </summary>
    public float GetFormationOffset(int columnIndex, int rowIndex)
    {
        var fc = stageConfig?.formationConfig;
        if (fc == null) return (columnIndex - 2) * 2.0f;
        int maxRow = Mathf.Max(fc.maxVisibleRows - 1, 0);

        return RowFormation.GetColumnOffsetX(
            rowIndex, columnIndex, maxRow,
            fc.manualRowHalfWidths,
            fc.formationPreset,
            fc.formationMaxSpread,
            fc.formationMinSpread,
            fc.formationPowerCurve
        );
    }

    /// <summary>
    /// 获取排间距
    /// </summary>
    public float GetRowSpacing()
    {
        return stageConfig?.formationConfig?.rowSpacing ?? 2.5f;
    }

    public float GetFormationOffsetZ()
    {
        return stageConfig?.formationConfig?.formationOffsetZ ?? 0f;
    }

    public int GetMaxVisibleRows()
    {
        return stageConfig?.formationConfig?.maxVisibleRows ?? 5;
    }

    public float[] GetRowAlphaFactors()
    {
        return stageConfig?.formationConfig?.rowAlphaFactors;
    }

    /// <summary>
    /// 获取补齐移动延迟（连续补齐移动间的停顿时间）
    /// 优先使用当前波次配置：若启用动态补齐，则根据场上存活敌人数量在 [delayMin, delayMax] 间线性插值。
    /// 未启用动态补齐时使用波次静态延迟；找不到波次配置时回退到全局默认值。
    /// </summary>
    public float GetRushMoveDelay()
    {
        if (stageConfig == null) return 0.2f;

        WaveConfig waveCfg = null;
        int waveIdx = waveSpawner != null ? waveSpawner.CurrentWaveIndex : -1;
        if (waveIdx >= 0 && waveIdx < stageConfig.waves.Count)
            waveCfg = stageConfig.waves[waveIdx];

        if (waveCfg != null)
        {
            if (waveCfg.enableDynamicRush)
            {
                int alive = enemyManager != null ? enemyManager.AliveEnemyCount : 10;
                float t = Mathf.Clamp01(alive / 10f);
                return Mathf.Lerp(waveCfg.rushMoveDelayMin, waveCfg.rushMoveDelay, t);
            }
            return waveCfg.rushMoveDelay;
        }

        return stageConfig.rushMoveDelay;
    }

    /// <summary>
    /// 获取当前关卡的补齐规则
    /// </summary>
    public FillUpRule GetFillUpRule()
    {
        return stageConfig?.fillUpRule ?? FillUpRule.PerColumn;
    }

    #endregion
}
