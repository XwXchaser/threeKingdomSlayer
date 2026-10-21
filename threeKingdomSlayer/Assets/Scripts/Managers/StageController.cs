using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡流程控制器 - 单例
/// 管理关卡开始、进行中、胜利、失败状态
/// 协调 WaveSpawner、EnemyManager、PlayerState 之间的交互
///
/// 【架构】本类现在是**薄宿主**：Inspector 引用、关卡状态、事件广播、协程宿主都在这里；
/// 「这一局怎么跑」「一场战斗怎么开」「奖励怎么结算」三块逻辑分别拆到
/// <see cref="StageRunFlow"/> / <see cref="StageCombatStaging"/> / <see cref="StageRewardGate"/>，
/// 它们通过 <see cref="IStageRoleContext"/> 拿环境，不认识本门面。
/// 对外仍然实现 5 个角色接口（阵型 / 战斗状态 / 一局流程 / 战斗布场 / 奖励门控），
/// 消费者一行都不用改。
/// </summary>
public class StageController : MonoBehaviour, IStageFormation, IStageBattleState,
    IRunFlow, ICombatStaging, IRewardGate, IRouteStageHost, IStageRoleContext,
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

    // ───────── ICombatStaging / IRunFlow 显式实现 ─────────
    // 路线层（FakeRouteRuntime）只依赖它自己声明的端口（IRouteStageHost = 三个角色的合成）；
    // 这三个成员在 StageController 上原本是 public 字段，用显式实现包一层，
    // 字段与 Inspector 序列化保持不变。
    // 方法（StartRouteBattle / SetRouteTravelState / SetRouteRewardWaitState /
    // CompleteFakeRouteStage / CompleteRouteStage / SetCurrentNodeBattleConfig /
    // StartCurrentRouteNode / StopCombatForRouteTravel / GoToBattleScene）签名已匹配，隐式实现接口。
    bool ICombatStaging.AutoStartStage
    {
        get => autoStartStage;
        set => autoStartStage = value;
    }

    FakeRouteStageConfig IRunFlow.RouteConfig
    {
        get => fakeRouteStageConfig;
        set => fakeRouteStageConfig = value;
    }

    event System.Action ICombatStaging.RouteBattleCompleted
    {
        add => OnRouteBattleCompleted += value;
        remove => OnRouteBattleCompleted -= value;
    }

    // ───────── IStageFormation 显式实现 ─────────
    // 原本是 public 字段的成员用显式实现包一层，字段与 Inspector 序列化保持不变；
    // GetFormationOffset / GetRowSpacing / GetFormationOffsetZ / GetMaxVisibleRows /
    // GetRushMoveDelay / GetRowAlphaFactors 签名已匹配，走隐式实现。
    StageConfig IStageFormation.StageConfig => stageConfig;

    // ───────── 三个角色的实现 ─────────
    /// <summary>路线战斗宿主：节点战斗的开打/旅行/等奖励状态与迁移逻辑已拆出。</summary>
    private StageRouteBattleHost _routeHost;

    /// <summary>一局流程：开始 / 重开 / 退出 / 波次推进 / 胜负判定。</summary>
    private StageRunFlow _runFlow;

    /// <summary>一场战斗的布场与开打。</summary>
    private StageCombatStaging _staging;

    /// <summary>奖励门控与结算。</summary>
    private StageRewardGate _rewardGate;

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

    StageConfig IRunFlow.StageConfig => stageConfig;

    bool IRunFlow.BlockRestartOnDefeat
    {
        get => blockRestartOnDefeat;
        set => blockRestartOnDefeat = value;
    }

    bool IRunFlow.SuppressDefeatUI => suppressDefeatUI;

    event System.Action IRunFlow.StageDefeat
    {
        add => OnStageDefeat += value;
        remove => OnStageDefeat -= value;
    }

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

    // ───────── IRewardGate 显式实现 ─────────
    // OnCombatNodeCleared 在 StageController 上是 public Action 字段，用显式实现包成事件：
    // 字段与 Inspector 序列化不变，订阅者走接口。
    event System.Action IRewardGate.CombatNodeCleared
    {
        add => OnCombatNodeCleared += value;
        remove => OnCombatNodeCleared -= value;
    }

    // ───────── IStageRoleContext 实现（给三个角色类提供环境）─────────
    FakeRouteStageConfig IStageRoleContext.RouteConfig
    {
        get => fakeRouteStageConfig;
        set => fakeRouteStageConfig = value;
    }

    bool IStageRoleContext.AutoStartStage
    {
        get => autoStartStage;
        set => autoStartStage = value;
    }

    bool IStageRoleContext.BlockRestartOnDefeat
    {
        get => blockRestartOnDefeat;
        set => blockRestartOnDefeat = value;
    }

    bool IStageRoleContext.SuppressDefeatUI => suppressDefeatUI;

    string IStageRoleContext.BattleSceneName => battleSceneName;

    bool IStageRoleContext.RouteStageSettled
    {
        get => _routeStageSettled;
        set => _routeStageSettled = value;
    }

    bool IStageRoleContext.IsRouteBattleRunning => _routeHost.RouteBattleRunning;

    bool IStageRoleContext.IsRouteRewardWaiting => _routeHost.IsRouteRewardWaiting;

    void IStageRoleContext.PrewarmEnemyPools() => _routeHost.PrewarmEnemyPools();

    void IStageRoleContext.SettleCoins() => SettleCoins();

    void IStageRoleContext.RunCoroutine(IEnumerator routine) => StartCoroutine(routine);

    void IStageRoleContext.SyncStageStateSilently(StageState state)
    {
        if (currentState != state) currentState = state;
    }

    void IStageRoleContext.RaiseStageStateChanged(StageState state) => OnStageStateChanged?.Invoke(state);

    void IStageRoleContext.RaiseStageVictory() => OnStageVictory?.Invoke();

    void IStageRoleContext.RaiseStageDefeat() => OnStageDefeat?.Invoke();

    void IStageRoleContext.RaiseCombatNodeCleared() => OnCombatNodeCleared?.Invoke();

    void IStageRoleContext.RaiseRouteBattleCompleted() => OnRouteBattleCompleted?.Invoke();

    event System.Action IStageRoleContext.StageDefeat
    {
        add => OnStageDefeat += value;
        remove => OnStageDefeat -= value;
    }

    event System.Action IStageRoleContext.CombatNodeCleared
    {
        add => OnCombatNodeCleared += value;
        remove => OnCombatNodeCleared -= value;
    }

    event System.Action IStageRoleContext.RouteBattleCompleted
    {
        add => OnRouteBattleCompleted += value;
        remove => OnRouteBattleCompleted -= value;
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;
        _routeHost = new StageRouteBattleHost(this);
        _runFlow = new StageRunFlow(this);
        _staging = new StageCombatStaging(this, _routeHost);
        _rewardGate = new StageRewardGate(this, _routeHost);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 登记到角色契约装配点：内核只通过 IStageBattleState / IStageFormation 取本类，
        // 不再点名流程类（2026-10-22 asmdef 分层第八批）。
        StageRoles.BattleState = this;
        StageRoles.Formation = this;

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

        // 注册事件（回调实现都在 StageRunFlow 里）
        if (waveSpawner != null)
        {
            waveSpawner.OnAllWavesCompleted += _runFlow.OnAllWavesCleared;
            waveSpawner.OnWaveCompleted += _runFlow.OnWaveCleared;
        }
        if (enemyManager != null)
        {
            enemyManager.OnAnyEnemyDied += _runFlow.OnEnemyDied;
        }
        if (playerState != null)
        {
            playerState.OnPlayerDied += _runFlow.OnPlayerDefeated;
            playerState.OnStageStateChanged += _runFlow.OnPlayerStageStateChanged;
        }

        // 域重载或上一局暂停状态可能保留 timeScale=0；必须在安排首局启动前恢复，
        // 否则 Invoke 的缩放时间永远不会到达，关卡会停在 None。
        Time.timeScale = 1f;

        // 自动开始关卡：协程使用 unscaled 时间，避免其他 Start 在同帧暂停时让启动永久卡住。
        Debug.Log("[StageController] 场景加载完成，自动开始关卡");
        if (autoStartStage) StartCoroutine(_runFlow.StartStageNextFrame());
    }

    #region 关卡流程（薄转发：实现见 StageRunFlow）

    /// <summary>
    /// 开始关卡
    /// </summary>
    public void StartStage() => _runFlow.StartStage();



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

    #region 场景管理

    /// <summary>
    /// 重新开始关卡
    /// </summary>
    public void RestartStage() => _runFlow.RestartStage();

    public void CleanupRouteStage(bool unloadScene) => _runFlow.CleanupRouteStage(unloadScene);

    /// <summary>
    /// 返回落地页：重载 Battle 场景，FakeRouteRuntime 会重新自举回 Ready。结算铜钱始终先做。
    /// （MainMenu 已随 D4 删除，不再有「回主菜单」分支。）
    /// </summary>
    public void GoToMainMenu() => _runFlow.GoToMainMenu();

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
    public void GoToBattleScene() => _staging.GoToBattleScene();

    /// <summary>卷轴/假移动路线整关结算：整关奖励 + 标记通关 + 铜钱结算，只执行一次。</summary>
    // ───────── 遗留路线（Legacy）用的入口 ─────────
    // 这些方法只被 Assets/Scripts/Legacy 里的 V1/V2 实现调用，现役流程不经过。
    // 刻意不引用任何 Legacy 类型（V1/V2 的配置类型），这样 Legacy 目录才能保持「孤岛」。

    public void SetCurrentNodeBattleConfig(StageConfig config) => _staging.SetCurrentNodeBattleConfig(config);

    public void StopCombatForRouteTravel() => _staging.StopCombatForRouteTravel();

    public void StartCurrentRouteNode(bool resetPlayer = false) => _staging.StartCurrentRouteNode(resetPlayer);

    public void CompleteCurrentCombatNode() => _rewardGate.CompleteCurrentCombatNode();

    /// <summary>旧版（RouteV2）路线结算。stageId 由调用方给出，避免引用 V2 的配置类型。</summary>
    public void CompleteRouteStage(int stageId, int clearReward) => _rewardGate.CompleteRouteStage(stageId, clearReward);

    public void CompleteFakeRouteStage(int clearReward) => _rewardGate.CompleteFakeRouteStage(clearReward);

    public void SetRouteTravelState() => _staging.SetRouteTravelState();

    public void SetRouteRewardWaitState() => _rewardGate.SetRouteRewardWaitState();

    public bool IsRouteRewardWaiting => _routeHost.IsRouteRewardWaiting;
    public bool IsRouteCombatActive => _routeHost.IsRouteCombatActive;

    public void StartRouteBattle(StageConfig config) => _staging.StartRouteBattle(config);






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

    #endregion
}
