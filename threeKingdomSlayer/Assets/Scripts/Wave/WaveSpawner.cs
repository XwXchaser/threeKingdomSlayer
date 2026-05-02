using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次生成器
/// 实现波次生成逻辑，清空条件，生成方式
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("配置")]
    public StageConfig stageConfig;
    public EnemyPool enemyPool;
    public ColumnManager columnManager;
    public EnemyManager enemyManager;

    [Header("生成参数")]
    public Transform spawnRoot; // 生成根节点（可选）

    // 运行时状态
    private int currentWaveIndex = -1;
    private bool isSpawning;
    private bool isWaveComplete;

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
    /// 开始生成波次
    /// </summary>
    public void StartWaveSpawning()
    {
        if (stageConfig == null || stageConfig.waves.Count == 0)
        {
            Debug.LogError("[WaveSpawner] 关卡配置为空或没有波次配置");
            return;
        }

        currentWaveIndex = -1;
        StartCoroutine(SpawnNextWave());
    }

    /// <summary>
    /// 生成下一波
    /// </summary>
    private IEnumerator SpawnNextWave()
    {
        currentWaveIndex++;

        // 所有波次已完成
        if (currentWaveIndex >= stageConfig.waves.Count)
        {
            OnAllWavesCompleted?.Invoke();
            yield break;
        }

        WaveConfig wave = stageConfig.waves[currentWaveIndex];
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

        // 等待当前波次所有敌人死亡
        yield return StartCoroutine(WaitForWaveClear());

        // 波次已清空
        isWaveComplete = true;
        OnWaveCompleted?.Invoke(currentWaveIndex);

        Debug.Log($"[WaveSpawner] 第 {currentWaveIndex + 1} 波已清空");

        // 等待延迟后生成下一波
        float delay = wave.nextWaveDelay > 0 ? wave.nextWaveDelay : 3f;
        yield return new WaitForSeconds(delay);

        // 生成下一波
        StartCoroutine(SpawnNextWave());
    }

    /// <summary>
    /// 生成一排敌人（5列）
    /// </summary>
    private void SpawnRow(RowConfig row)
    {
        if (row.enemyIds == null || row.enemyIds.Length != 5)
        {
            Debug.LogWarning("[WaveSpawner] RowConfig.enemyIds 长度不为5，跳过");
            return;
        }

        for (int col = 0; col < 5; col++)
        {
            int enemyId = row.enemyIds[col];
            if (enemyId <= 0) continue; // 0或负数表示该列无敌人

            // 从对象池获取敌人
            Enemy enemy = enemyPool?.GetEnemy(enemyId);
            if (enemy == null)
            {
                Debug.LogWarning($"[WaveSpawner] 无法获取敌人ID {enemyId}，跳过");
                continue;
            }

            // 获取该列当前敌人数量作为排索引
            int rowIndex = columnManager?.GetColumnEnemyCount(col) ?? 0;

            // 初始化敌人
            EnemyConfig config = GetEnemyConfig(enemyId);
            if (config == null)
            {
                Debug.LogWarning($"[WaveSpawner] 未找到敌人ID {enemyId} 的配置");
                enemyPool.ReturnEnemy(enemy);
                continue;
            }

            enemy.Initialize(config, col, rowIndex);

            // 注册到管理器
            enemyManager?.RegisterEnemy(enemy);
        }
    }

    /// <summary>
    /// 等待当前波次所有敌人死亡
    /// </summary>
    private IEnumerator WaitForWaveClear()
    {
        // 等待直到所有敌人都死亡
        while (enemyManager != null && !enemyManager.IsAllEnemiesDead)
        {
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 获取敌人配置（从Resources或引用）
    /// 实际项目中应从配置管理器获取
    /// </summary>
    private EnemyConfig GetEnemyConfig(int enemyId)
    {
        // 简单实现：从Resources加载
        // 实际项目中应使用配置管理器缓存
        EnemyConfig[] configs = Resources.LoadAll<EnemyConfig>("");
        foreach (var cfg in configs)
        {
            if (cfg.enemyId == enemyId)
                return cfg;
        }
        return null;
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
    /// 获取总波次数
    /// </summary>
    public int TotalWaves => stageConfig != null ? stageConfig.waves.Count : 0;
}
