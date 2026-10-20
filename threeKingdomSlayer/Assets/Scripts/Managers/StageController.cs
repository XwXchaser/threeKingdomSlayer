using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡流程控制器 - 单例
/// 管理关卡开始、进行中、胜利、失败状态
/// 协调 WaveSpawner、EnemyManager、PlayerState 之间的交互
/// </summary>
public class StageController : MonoBehaviour, IRouteBattleHost, IStageFormation, IStageFlowControl, IStageBattleState,
    StageRouteBattleHost.IContext
{
    public static StageController Instance { get; private set; }

    [Header("关卡配置")]
    public StageConfig stageConfig;
    [Tooltip("Disable only when an external flow explicitly starts combat.")]
    public bool autoStartStage = true;
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
    private bool _routeStageSettled;

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

    // ───────── IRouteBattleHost 显式实现 ─────────
    // 路线层（FakeRouteRuntime）只依赖它自己声明的端口；这三个成员在 StageController 上
    // 原本是 public 字段，用显式实现包一层，字段与 Inspector 序列化保持不变。
    // 四个方法（StartRouteBattle / SetRouteRewardWaitState / SetRouteTravelState /
    // CompleteFakeRouteStage）签名已匹配，隐式实现接口。
    bool IRouteBattleHost.AutoStartStage
    {
        get => autoStartStage;
        set => autoStartStage = value;
    }

    FakeRouteStageConfig IRouteBattleHost.RouteConfig
    {
        get => fakeRouteStageConfig;
        set => fakeRouteStageConfig = value;
    }

    event System.Action IRouteBattleHost.RouteBattleCompleted
    {
        add => OnRouteBattleCompleted += value;
        remove => OnRouteBattleCompleted -= value;
    }

    // ───────── IStageFormation / IStageFlowControl 显式实现 ─────────
    // 原本是 public 字段的成员用显式实现包一层，字段与 Inspector 序列化保持不变；
    // GetFormationOffset / GetRowSpacing / GetFormationOffsetZ / GetMaxVisibleRows /
    // GoToMainMenu 签名已匹配，走隐式实现。
    StageConfig IStageFormation.StageConfig => stageConfig;

    /// <summary>路线战斗宿主：节点战斗的开打/旅行/等奖励状态与迁移逻辑已拆出。</summary>
    private StageRouteBattleHost _routeHost;

    /// <summary>阵型计算：纯计算，已拆出（见 StageFormation）。</summary>
    private StageFormation _formation = new StageFormation();

    // ───────── StageRouteBattleHost.IContext 实现 ─────────
    StageConfig StageRouteBattleHost.IContext.StageConfig
    {
        get => stageConfig;
        set => stageConfig = value;
    }

    StageState StageRouteBattleHost.IContext.CurrentState => currentState;

    void StageRouteBattleHost.IContext.SetStageState(StageState state) => SetState(state);

    WaveSpawner StageRouteBattleHost.IContext.WaveSpawner => waveSpawner;

    EnemyManager StageRouteBattleHost.IContext.EnemyManager => enemyManager;

    PlayerState StageRouteBattleHost.IContext.PlayerState => playerState;

    EnemyPool StageRouteBattleHost.IContext.EnemyPool => enemyPool;

    StageConfig IStageFlowControl.StageConfig => stageConfig;

    bool IStageFlowControl.IsStageInProgress => IsStageInProgress;

    bool IStageFlowControl.BlockRestartOnDefeat
    {
        get => blockRestartOnDefeat;
        set => blockRestartOnDefeat = value;
    }

    bool IStageFlowControl.SuppressDefeatUI => suppressDefeatUI;

    FakeRouteStageConfig IStageFlowControl.RouteConfig => fakeRouteStageConfig;

    // ───────── IStageBattleState 显式实现 ─────────
    // OnStageStateChanged / OnStageVictory 在 StageController 上是 public Action 字段，
    // 用显式实现包成事件：字段与 Inspector 序列化不变，订阅者走接口。
    bool IStageBattleState.IsRouteCombatActive => IsRouteCombatActive;

    bool IStageBattleState.IsRouteRewardWaiting => IsRouteRewardWaiting;

    bool IStageBattleState.IsStageInProgress => IsStageInProgress;

    StageConfig IStageBattleState.StageConfig => stageConfig;

    event System.Action<StageState> IStageBattleState.StageStateChanged
    {
        add => OnStageStateChanged += value;
        remove => OnStageStateChanged -= value;
    }

    event System.Action IStageBattleState.StageVictory
    {
        add => OnStageVictory += value;
        remove => OnStageVictory -= value;
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
        _routeHost = new StageRouteBattleHost(this);
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
        _routeHost.PrewarmEnemyPools();

        // 设置关卡状态为进行中
        SetState(StageState.InProgress);

        // 播放 BGM
        AudioManager.Instance?.PlayDefaultBGM();

        // 开始生成波次
        waveSpawner?.StartWaveSpawning();
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

        if (_routeHost.RouteBattleRunning)
        {
            OnRouteBattleCompleted?.Invoke();
            return;
        }

        Debug.Log("[StageController] 最终战斗节点已清空，关卡胜利！");
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);
        StageRouteBattleHost.ClearEnemyProjectiles();

        if (stageConfig != null)
        {
            playerState?.AddCoins(stageConfig.clearCoinReward);
        }
        if (stageConfig != null)
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

        // 取消待处理的选择完成回调（玩家已死，不再生成下一波）
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
            ucm.OnAllChoicesDone -= OnChoicesDoneSpawnNextWave;

        Debug.Log("[StageController] 玩家阵亡，关卡失败");
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Defeat);
        StageRouteBattleHost.ClearEnemyProjectiles();
        OnStageDefeat?.Invoke();
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

        // 兜底：没有卷轴路线配置时按普通关卡重开。
        enemyManager?.ClearAllEnemies();
        enemyPool?.ClearAllPools();
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
    // ───────── 遗留路线（Legacy）用的入口 ─────────
    // 这些方法只被 Assets/Scripts/Legacy 里的 V1/V2 实现调用，现役流程不经过。
    // 刻意不引用任何 Legacy 类型（V1/V2 的配置类型），这样 Legacy 目录才能保持「孤岛」。

    public void SetCurrentNodeBattleConfig(StageConfig config)
    {
        stageConfig = config;
    }

    public void StopCombatForRouteTravel()
    {
        _routeHost.StopCombatForRouteTravel();
    }

    public void StartCurrentRouteNode(bool resetPlayer = false)
    {
        if (stageConfig == null) return;
        _routeHost.StopCombatSystemsForNodeTransition();
        StartNodeCombat(resetPlayer);
    }

    public void CompleteCurrentCombatNode()
    {
        if (CurrentState != StageState.InProgress) return;
        OnCombatNodeCleared?.Invoke();
    }

    /// <summary>旧版（RouteV2）路线结算。stageId 由调用方给出，避免引用 V2 的配置类型。</summary>
    public void CompleteRouteStage(int stageId, int clearReward)
    {
        if (!_routeHost.RouteBattleRunning || _routeStageSettled) return;
        _routeStageSettled = true;
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);
        StageRouteBattleHost.ClearEnemyProjectiles();
        if (clearReward > 0) playerState?.AddCoins(clearReward);
        if (stageId > 0) SaveManager.MarkStageCleared(stageId);
        SettleCoins();
        OnStageVictory?.Invoke();
    }

    public void CompleteFakeRouteStage(int clearReward)
    {
        Debug.Log($"[Tutorial] 结算入口: config={(fakeRouteStageConfig != null ? fakeRouteStageConfig.stageName : "NULL")} 已结算={_routeStageSettled} 奖励={clearReward} 玩家={(playerState != null)}");
        if (fakeRouteStageConfig == null || _routeStageSettled) return;
        _routeStageSettled = true;
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);
        StageRouteBattleHost.ClearEnemyProjectiles();
        if (clearReward > 0) playerState?.AddCoins(clearReward);
        SaveManager.MarkStageCleared(fakeRouteStageConfig.stageId);
        SettleCoins();
        OnStageVictory?.Invoke();
    }



    public void SetRouteTravelState()
    {
        _routeHost.SetRouteTravelState();
    }

    public void SetRouteRewardWaitState()
    {
        _routeHost.SetRouteRewardWaitState();
    }

    public bool IsRouteRewardWaiting => _routeHost.IsRouteRewardWaiting;
    public bool IsRouteCombatActive => _routeHost.IsRouteCombatActive;

    public void StartRouteBattle(StageConfig config)
    {
        if (config == null) return;
        _routeHost.StartRouteBattle(config);
    }




    private void StartNodeCombat(bool resetPlayer)
    {
        if (stageConfig == null) return;
        if (resetPlayer) playerState?.ResetPlayer();
        UltimateSystem.Instance?.ResetEnergy();
        var killRewardManager = FindObjectOfType<KillRewardManager>();
        killRewardManager?.ResetRewards();
        _routeHost.PrewarmEnemyPools();
        SetState(StageState.InProgress);
        AudioManager.Instance?.PlayDefaultBGM();
        waveSpawner?.StartWaveSpawning();
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
        return _formation.GetFormationOffset(stageConfig, columnIndex, rowIndex);
    }

    /// <summary>
    /// 获取排间距
    /// </summary>
    public float GetRowSpacing()
    {
        return _formation.GetRowSpacing(stageConfig);
    }

    public float GetFormationOffsetZ()
    {
        return _formation.GetFormationOffsetZ(stageConfig);
    }

    public int GetMaxVisibleRows()
    {
        return _formation.GetMaxVisibleRows(stageConfig);
    }

    public float[] GetRowAlphaFactors()
    {
        return _formation.GetRowAlphaFactors(stageConfig);
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
        return _formation.GetFillUpRule(stageConfig);
    }

    #endregion
}
