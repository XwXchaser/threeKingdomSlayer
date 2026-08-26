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
    public RouteStageConfig routeStageConfig;
    public RouteStageConfigV2 routeStageConfigV2;
    public FakeRouteStageConfig fakeRouteStageConfig;

    /// <summary>
    /// 待加载的假移动路线关卡配置（MainMenu选关后设置）
    /// </summary>
    public static FakeRouteStageConfig PendingFakeRouteStageConfig;

    /// <summary>
    /// 待加载的线性关卡配置（MainMenu选关后设置）
    /// </summary>
    public static StageConfig PendingStageConfig;

    /// <summary>
    /// 待加载的路线关卡配置（MainMenu选关后设置）
    /// </summary>
    public static RouteStageConfig PendingRouteStageConfig;

    [Header("组件引用")]
    public WaveSpawner waveSpawner;
    public EnemyManager enemyManager;
    public PlayerState playerState;
    public EnemyPool enemyPool;

    [Header("场景引用")]
    public string mainMenuSceneName = "MainMenu";
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

        // 从 MainMenu 传入的路线或线性关卡配置
        if (FakeRouteLaunch.PendingConfig != null)
        {
            fakeRouteStageConfig = FakeRouteLaunch.PendingConfig;
            Debug.Log("[FakeRoute] launch stage=" + fakeRouteStageConfig.stageId);
            FakeRouteLaunch.PendingConfig = null;
            routeStageConfigV2 = null;
            routeStageConfig = null;
            stageConfig = null;
        }
        else if (RouteStageV2Launch.PendingConfig != null)
        {
            routeStageConfigV2 = RouteStageV2Launch.PendingConfig;
            Debug.Log("[RouteV2] launch mode=" + (RouteStageV2Launch.StartFromCheckpoint ? "checkpoint" : "new game") + " stage=" + routeStageConfigV2.stageId);
            RouteStageV2Launch.PendingConfig = null;
            routeStageConfig = null;
            stageConfig = null;
        }
        else if (PendingRouteStageConfig != null)
        {
            routeStageConfig = PendingRouteStageConfig;
            PendingRouteStageConfig = null;
            routeStageConfigV2 = null;
            stageConfig = null;
        }
        else if (PendingStageConfig != null)
        {
            stageConfig = PendingStageConfig;
            PendingStageConfig = null;
            routeStageConfig = null;
            routeStageConfigV2 = null;
            fakeRouteStageConfig = null;
        }

        if (fakeRouteStageConfig != null)
        {
            Debug.Log($"[StageController] 加载假移动路线关卡: {fakeRouteStageConfig.stageName} (stageId={fakeRouteStageConfig.stageId})");
        }
        else if (routeStageConfig != null)
        {
            Debug.Log($"[StageController] 加载旧路线关卡: {routeStageConfig.stageName} (stageId={routeStageConfig.stageId})");
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
        StartCoroutine(StartStageNextFrame());
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
                Debug.LogError("[StageController] 假移动路线运行时组件缺失");
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
        if (currentState == newState) return; // BUG FIX: 防止重复设置导致循环

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

        var fakeRuntime = FindObjectOfType<FakeRouteRuntime>();
        if (_routeBattleRuntime && fakeRuntime != null)
        {
            OnRouteBattleCompleted?.Invoke();
            return;
        }

        var runtimeV2 = FindObjectOfType<RouteStageRuntimeV2>();
        if (_routeBattleRuntime && runtimeV2 != null)
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
            fakeRuntime.HandleStageDefeat();

        var v2Runtime = FindObjectOfType<RouteStageRuntimeV2>();
        if (v2Runtime != null)
            v2Runtime.HandleStageDefeat();

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
    /// 返回主菜单（结算铜钱后跳转）
    /// </summary>
    public void GoToMainMenu()
    {
        AudioManager.Instance?.StopBGM();
        CleanupRouteStage(true);
        SettleCoins();
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
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

    public void CompleteFakeRouteStage(int clearReward)
    {
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
        TimedPassiveModule.Instance?.PrepareForNonCombat();
        SetState(StageState.Starting);
    }

    public void SetRouteRewardWaitState()
    {
        _routeRewardWaiting = true;
        StopCombatSystemsForNodeTransition();
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
