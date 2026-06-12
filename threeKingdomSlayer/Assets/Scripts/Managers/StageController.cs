using System.Collections;
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

    /// <summary>
    /// 待加载的关卡配置（MainMenu选关后设置，Battle场景 Awake 时读取并清空）
    /// </summary>
    public static StageConfig PendingStageConfig;

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

    // 事件
    public System.Action<StageState> OnStageStateChanged;
    public System.Action OnStageVictory;
    public System.Action OnStageDefeat;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 从 MainMenu 传入的关卡配置（静态变量跨场景传递）
        if (PendingStageConfig != null)
        {
            stageConfig = PendingStageConfig;
            PendingStageConfig = null;
        }

        if (stageConfig != null)
        {
            Debug.Log($"[StageController] 加载关卡: {stageConfig.stageName} (stageId={stageConfig.stageId})");
        }
        else
        {
            Debug.LogWarning("[StageController] stageConfig 未设置");
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

        // 自动开始关卡（Battle场景加载后直接进入战斗）
        // 注意：延迟一帧执行，确保所有组件都已初始化完毕
        // 例如 EnemyPool 在 Awake 中注册预制体，StartStage 中 PrewarmEnemyPools 需要用到
        Debug.Log("[StageController] 场景加载完成，自动开始关卡");
        Invoke(nameof(StartStage), 0.1f);
    }

    #region 关卡流程

    /// <summary>
    /// 开始关卡
    /// </summary>
    public void StartStage()
    {
        if (stageConfig == null)
        {
            Debug.LogError("[StageController] stageConfig 未赋值，无法开始关卡");
            return;
        }

        Debug.Log($"[StageController] 开始关卡: {stageConfig.stageName}");

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

        // 遍历所有波次，收集所有用到的敌人ID
        foreach (var wave in stageConfig.waves)
        {
            foreach (var row in wave.rows)
            {
                foreach (int enemyId in row.enemyIds)
                {
                    if (enemyId > 0)
                    {
                        enemyPool.PrewarmPool(enemyId, enemyPool.defaultPoolSize);
                    }
                }
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

        // 先触发补齐移动（Boss死亡时跳过了rush chain，现在补偿）
        var cm = FindObjectOfType<ColumnManager>();
        if (cm != null)
        {
            FillUpRule rule = GetFillUpRule();
            if (rule == FillUpRule.PerRow)
                cm.RowBasedFillUp();
            else
                for (int i = 0; i < cm.columnCount; i++)
                    cm.TriggerFillForward(i);
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

        Debug.Log("[StageController] 所有波次已清空，关卡胜利！");
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Victory);

        // 发放通关奖励
        if (stageConfig != null)
        {
            playerState?.AddCoins(stageConfig.clearCoinReward);
        }

        // 标记通关 + 结算铜钱
        if (stageConfig != null)
        {
            SaveManager.MarkStageCleared(stageConfig.stageId);
        }
        SettleCoins();

        OnStageVictory?.Invoke();
    }

    /// <summary>
    /// 玩家阵亡 → 关卡失败
    /// </summary>
    private void OnPlayerDefeated()
    {
        if (currentState == StageState.Defeat || currentState == StageState.Victory) return;

        // 取消待处理的选择完成回调（玩家已死，不再生成下一波）
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null)
            ucm.OnAllChoicesDone -= OnChoicesDoneSpawnNextWave;

        Debug.Log("[StageController] 玩家阵亡，关卡失败");
        AudioManager.Instance?.StopBGM();
        SetState(StageState.Defeat);
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

    private IEnumerator RestartStageCoroutine()
    {
        // 清理所有敌人
        enemyManager?.ClearAllEnemies();
        enemyPool?.ClearAllPools();

        // 等待一帧确保清理完成
        yield return null;

        // 重新开始
        StartStage();
    }

    /// <summary>
    /// 返回主菜单（结算铜钱后跳转）
    /// </summary>
    public void GoToMainMenu()
    {
        AudioManager.Instance?.StopBGM();
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
