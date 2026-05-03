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

    [Header("透明度渐变配置")]
    [Tooltip("每排的透明度系数，索引0=最前排")]
    public float[] rowAlphaFactors = new float[] { 1.0f, 0.8f, 0.6f, 0.4f, 0.2f };

    [Header("可见排数限制")]
    public int maxVisibleRows = 5;

    [Header("排阵型配置（梯形/扇形内收）")]
    [Tooltip("方案A：预设表。若设置则优先使用预设表，否则使用方案B公式计算")]
    public RowFormationPreset formationPreset;
    [Tooltip("最前排（rowIndex=0）的半宽。例如4.0表示最前排最左列X=-4.0，最右列X=+4.0")]
    public float formationMaxSpread = 4.0f;
    [Tooltip("最后排的半宽。例如0.5表示最后排最左列X=-0.5，最右列X=+0.5")]
    public float formationMinSpread = 0.5f;
    [Tooltip("内收曲线指数。1.0=线性，>1.0=后排更快收拢，<1.0=前排更快收拢")]
    public float formationPowerCurve = 1.2f;
    [Tooltip("排间距（Z轴，世界单位）")]
    public float rowSpacing = 2.5f;
    [Tooltip("阵型整体Z轴偏移（正值=远离摄像机，负值=靠近摄像机）")]
    public float formationOffsetZ = 0f;

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

        if (stageConfig != null)
        {
            rowAlphaFactors = stageConfig.rowAlphaFactors;
            maxVisibleRows = stageConfig.maxVisibleRows;
            formationPreset = stageConfig.formationPreset;
            formationMaxSpread = stageConfig.formationMaxSpread;
            formationMinSpread = stageConfig.formationMinSpread;
            formationPowerCurve = stageConfig.formationPowerCurve;
            rowSpacing = stageConfig.rowSpacing;
            formationOffsetZ = stageConfig.formationOffsetZ;
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
        enemyManager?.ClearAllEnemies();

        // 预创建对象池
        PrewarmEnemyPools();

        // 设置关卡状态为进行中
        SetState(StageState.InProgress);

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
    /// 所有波次已清空 → 关卡胜利
    /// </summary>
    private void OnAllWavesCleared()
    {
        if (currentState != StageState.InProgress) return;

        Debug.Log("[StageController] 所有波次已清空，关卡胜利！");
        SetState(StageState.Victory);

        // 发放通关奖励
        if (stageConfig != null)
        {
            playerState?.AddCoins(stageConfig.clearCoinReward);
        }

        OnStageVictory?.Invoke();
    }

    /// <summary>
    /// 玩家阵亡 → 关卡失败
    /// </summary>
    private void OnPlayerDefeated()
    {
        if (currentState == StageState.Defeat || currentState == StageState.Victory) return;

        Debug.Log("[StageController] 玩家阵亡，关卡失败");
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
        if (enemy?.config == null) return;

        playerState?.AddKill();
        playerState?.AddCoins(enemy.config.coinReward);
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
    /// 返回主菜单
    /// </summary>
    public void GoToMainMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(mainMenuSceneName);
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
        // BUG FIX: 使用固定的 maxVisibleRows 作为最大排数基准
        // 这样即使某列敌人减少，阵型位置也不会变化
        // 使用 maxVisibleRows - 1 作为最大排索引，确保阵型始终按最大可见排数计算
        int maxRow = Mathf.Max(maxVisibleRows - 1, 0);

        return RowFormation.GetColumnOffsetX(
            rowIndex, columnIndex, maxRow,
            formationPreset,
            formationMaxSpread,
            formationMinSpread,
            formationPowerCurve
        );
    }

    /// <summary>
    /// 获取排间距
    /// </summary>
    public float GetRowSpacing()
    {
        return rowSpacing;
    }

    /// <summary>
    /// 获取阵型整体Z轴偏移
    /// </summary>
    public float GetFormationOffsetZ()
    {
        return formationOffsetZ;
    }

    /// <summary>
    /// 获取补齐移动时长（秒）
    /// 敌人死亡后，后方所有敌人同时使用此固定时长补齐到前一排
    /// </summary>
    public float GetRushMoveDuration()
    {
        if (stageConfig != null)
        {
            return stageConfig.rushMoveDuration;
        }
        return 0.5f; // 默认值
    }

    /// <summary>
    /// 获取最大可见排数
    /// </summary>
    public int GetMaxVisibleRows()
    {
        return maxVisibleRows;
    }

    #endregion
}
