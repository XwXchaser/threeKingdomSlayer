using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次生成器
/// 实现波次生成逻辑，清空条件，生成方式
/// 每个关卡通常只有1个波次，波次之间播放剧情演出，玩家手动点击"继续"按钮触发下一波
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("配置")]
    [Tooltip("关卡配置。留空则运行时从 StageController 自动获取")]
    public StageConfig stageConfig;
    public EnemyPool enemyPool;
    public ColumnManager columnManager;
    public EnemyManager enemyManager;

    /// <summary>
    /// 运行时解析关卡配置：优先用自身序列化字段，留空则从 StageController 读取
    /// </summary>
    private StageConfig ResolvedStageConfig
    {
        get
        {
            if (stageConfig != null) return stageConfig;
            return StageController.Instance != null ? StageController.Instance.stageConfig : null;
        }
    }

    [Header("生成参数")]
    public Transform spawnRoot; // 生成根节点（可选）

    // 运行时状态
    private int currentWaveIndex = -1;
    private bool isSpawning;
    private bool isWaveComplete;
    private bool isAllWavesCompleted;

    // 事件
    public System.Action<int> OnWaveStarted;       // waveIndex
    public System.Action<int> OnWaveCompleted;     // waveIndex
    public System.Action OnAllWavesCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        if (enemyPool == null) enemyPool = FindObjectOfType<EnemyPool>();
        if (columnManager == null) columnManager = FindObjectOfType<ColumnManager>();
        if (enemyManager == null) enemyManager = FindObjectOfType<EnemyManager>();
    }

    /// <summary>
    /// 开始生成波次（关卡开始时调用）
    /// </summary>
    public void StartWaveSpawning()
    {
        var cfg = ResolvedStageConfig;
        if (cfg == null || cfg.waves.Count == 0)
        {
            Debug.LogError("[WaveSpawner] 关卡配置为空或没有波次配置");
            return;
        }

        currentWaveIndex = -1;
        isAllWavesCompleted = false;
        SpawnNextWave();
    }

    /// <summary>
    /// 生成下一波（由 StageController 在玩家点击"继续"后调用）
    /// </summary>
    public void SpawnNextWave()
    {
        if (isAllWavesCompleted) return;

        currentWaveIndex++;

        // 所有波次已完成
        if (currentWaveIndex >= ResolvedStageConfig.waves.Count)
        {
            isAllWavesCompleted = true;
            OnAllWavesCompleted?.Invoke();
            return;
        }

        WaveConfig wave = ResolvedStageConfig.waves[currentWaveIndex];
        isSpawning = true;
        isWaveComplete = false;

        // 通知波次开始
        OnWaveStarted?.Invoke(currentWaveIndex);
        PlayerState.Instance?.SetCurrentWave(currentWaveIndex + 1);

        Debug.Log($"[WaveSpawner] 开始生成第 {currentWaveIndex + 1} 波，共 {wave.rows.Count} 排敌人");

        // 生成该波的所有排
        foreach (RowConfig row in wave.rows)
        {
            SpawnRow(row);
        }

        isSpawning = false;

        // 波次生成完成后，触发所有列的初始前移补齐
        // 普通波次敌人从后排 spawn（rowIndex+2），需要逐步前进填补前方空位
        for (int c = 0; c < 5; c++)
        {
            var col = columnManager.GetColumn(c);
            if (col != null && col.enemies.Count > 0)
                col.TriggerFillForward();
        }

        // 启动协程等待当前波次所有敌人死亡
        StartCoroutine(WaitForWaveClearAndNotify());
    }

    /// <summary>
    /// 等待当前波次清空，然后触发完成事件
    /// </summary>
    private IEnumerator WaitForWaveClearAndNotify()
    {
        // 等待直到所有敌人都死亡
        while (enemyManager != null && !enemyManager.IsAllEnemiesDead)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 波次已清空
        isWaveComplete = true;
        OnWaveCompleted?.Invoke(currentWaveIndex);

        Debug.Log($"[WaveSpawner] 第 {currentWaveIndex + 1} 波已清空");

        // 检查是否所有波次已完成（最后一波清空后触发胜利）
        if (currentWaveIndex >= ResolvedStageConfig.waves.Count - 1)
        {
            isAllWavesCompleted = true;
            OnAllWavesCompleted?.Invoke();
        }

        // 注意：不自动生成下一波，等待 StageController 在玩家点击"继续"后调用 SpawnNextWave()
    }

    /// <summary>
    /// 生成一排敌人
    /// 根据 enemyIds 长度决定该排有多少个敌人站位
    /// 每个敌人根据其 occupySlots 占用对应数量的列
    /// 该排所有敌人共享相同的排索引（rowIndex）
    /// </summary>
    private void SpawnRow(RowConfig row)
    {
        if (row.enemyIds == null || row.enemyIds.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] RowConfig.enemyIds 为空，跳过");
            return;
        }

        // 计算该排所有敌人的总占位数，确定起始列偏移
        int totalSlots = 0;
        int[] slotCounts = new int[row.enemyIds.Length];
        for (int i = 0; i < row.enemyIds.Length; i++)
        {
            int enemyId = row.enemyIds[i];
            if (enemyId <= 0)
            {
                slotCounts[i] = 0;
                continue;
            }
            int slots = enemyPool != null ? enemyPool.GetEnemyOccupySlots(enemyId) : 1;
            slotCounts[i] = slots;
            totalSlots += slots;
        }

        // 计算起始列偏移，使敌人居中排列
        // 例如 totalSlots=5 时起始列=0，totalSlots=3 时起始列=1
        int startColumn = (5 - totalSlots) / 2;
        if (startColumn < 0) startColumn = 0;

        // BUG FIX: 该排所有敌人共享相同的排索引
        // 排索引 = 当前波次中已生成的总排数（即所有列中最大的敌人数量）
        // 这样确保同一排的敌人在不同列中拥有相同的 rowIndex
        int rowIndex = 0;
        if (columnManager != null)
        {
            for (int c = 0; c < 5; c++)
            {
                int count = columnManager.GetColumnEnemyCount(c);
                if (count > rowIndex)
                    rowIndex = count;
            }
        }

        // 普通波次：敌人从第3排（rowIndex+2）出场，营造压迫前进感
        // BOSS波次：直接从最前排出场，立刻交战
        WaveConfig currentWave = ResolvedStageConfig.waves[currentWaveIndex];
        if (!currentWave.isBossWave)
            rowIndex += 2;

        int currentCol = startColumn;
        for (int i = 0; i < row.enemyIds.Length; i++)
        {
            int enemyId = row.enemyIds[i];
            if (enemyId <= 0) continue;

            int slots = slotCounts[i];

            // 从对象池获取敌人
            Enemy enemy = enemyPool?.GetEnemy(enemyId);
            if (enemy == null)
            {
                Debug.LogWarning($"[WaveSpawner] 无法获取敌人ID {enemyId}，跳过");
                currentCol += slots;
                continue;
            }

            enemy.Initialize(currentCol, rowIndex);

            // 注册到管理器
            enemyManager?.RegisterEnemy(enemy);

            currentCol += slots;
        }
    }

    /// <summary>
    /// 获取当前波次索引
    /// </summary>
    public int CurrentWaveIndex => currentWaveIndex;

    /// <summary>
    /// 是否正在生成
    /// </summary>
    public bool IsSpawning => isSpawning;

    /// <summary>
    /// 当前波次是否已完成
    /// </summary>
    public bool IsWaveComplete => isWaveComplete;

    /// <summary>
    /// 是否所有波次已完成
    /// </summary>
    public bool IsAllWavesCompleted => isAllWavesCompleted;

    /// <summary>
    /// 获取总波次数
    /// </summary>
    public int TotalWaves => ResolvedStageConfig != null ? ResolvedStageConfig.waves.Count : 0;
}
