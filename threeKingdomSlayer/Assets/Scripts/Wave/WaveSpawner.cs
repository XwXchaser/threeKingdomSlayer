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
    public StageConfig stageConfig;
    public EnemyPool enemyPool;
    public ColumnManager columnManager;
    public EnemyManager enemyManager;

    [Header("敌人配置（Inspector 拖拽赋值）")]
    [Tooltip("将 EnemyConfig ScriptableObject 拖拽到这里，系统会自动按 enemyId 索引。如果不赋值，会尝试从 Resources/EnemyConfigs/ 加载")]
    public List<EnemyConfig> enemyConfigs = new List<EnemyConfig>();

    [Header("生成参数")]
    public Transform spawnRoot; // 生成根节点（可选）

    // 运行时状态
    private int currentWaveIndex = -1;
    private bool isSpawning;
    private bool isWaveComplete;
    private bool isAllWavesCompleted;

    // 敌人配置缓存（避免每帧 Resources.LoadAll）
    private static Dictionary<int, EnemyConfig> enemyConfigCache = new Dictionary<int, EnemyConfig>();

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

        // BUG FIX: 清空静态缓存，避免场景重载（重新开始/返回主菜单）时缓存了旧的配置。
        // 静态缓存在场景切换后仍然存在，如果 StageConfig 更换了不同的 EnemyConfig，
        // ContainsKey 检查会导致新配置无法覆盖旧缓存，使用过期的配置数据。
        enemyConfigCache.Clear();
        Debug.Log($"[WaveSpawner] 清空敌人配置缓存 (count={enemyConfigCache.Count})");

        // 初始化敌人配置缓存：优先使用 Inspector 拖拽的配置
        // 这样策划可以直接在 Inspector 中拖拽 EnemyConfig 赋值，无需放入 Resources 文件夹
        if (enemyConfigs != null && enemyConfigs.Count > 0)
        {
            foreach (var cfg in enemyConfigs)
            {
                if (cfg != null)
                {
                    enemyConfigCache[cfg.enemyId] = cfg;
                    Debug.Log($"[WaveSpawner] 从 Inspector 加载敌人配置: {cfg.enemyName} (enemyId={cfg.enemyId})");
                }
            }
        }
    }

    /// <summary>
    /// 开始生成波次（关卡开始时调用）
    /// </summary>
    public void StartWaveSpawning()
    {
        if (stageConfig == null || stageConfig.waves.Count == 0)
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
        if (currentWaveIndex >= stageConfig.waves.Count)
        {
            isAllWavesCompleted = true;
            OnAllWavesCompleted?.Invoke();
            return;
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
            EnemyConfig config = GetEnemyConfig(enemyId);
            int slots = (config != null) ? Mathf.Clamp(config.occupySlots, 1, 5) : 1;
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

            // 初始化敌人
            EnemyConfig config = GetEnemyConfig(enemyId);
            if (config == null)
            {
                Debug.LogWarning($"[WaveSpawner] 未找到敌人ID {enemyId} 的配置");
                enemyPool.ReturnEnemy(enemy);
                currentCol += slots;
                continue;
            }

            enemy.Initialize(config, currentCol, rowIndex);

            // 注册到管理器
            enemyManager?.RegisterEnemy(enemy);

            currentCol += slots;
        }
    }

    /// <summary>
    /// 获取敌人配置（带缓存）
    /// 首次加载后缓存到字典，避免重复 Resources.LoadAll
    /// </summary>
    private EnemyConfig GetEnemyConfig(int enemyId)
    {
        // 先从缓存查找
        if (enemyConfigCache.TryGetValue(enemyId, out EnemyConfig cached))
        {
            return cached;
        }

        // 缓存未命中，从Resources加载并缓存
        EnemyConfig[] configs = Resources.LoadAll<EnemyConfig>("");
        foreach (var cfg in configs)
        {
            if (!enemyConfigCache.ContainsKey(cfg.enemyId))
            {
                enemyConfigCache[cfg.enemyId] = cfg;
            }
        }

        // 再次从缓存查找
        if (enemyConfigCache.TryGetValue(enemyId, out EnemyConfig result))
        {
            return result;
        }

        Debug.LogError($"[WaveSpawner] 未找到敌人ID {enemyId} 的配置，请确认已创建对应的 EnemyConfig ScriptableObject 并放置在 Resources 文件夹中");
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
    /// 是否所有波次已完成
    /// </summary>
    public bool IsAllWavesCompleted => isAllWavesCompleted;

    /// <summary>
    /// 获取总波次数
    /// </summary>
    public int TotalWaves => stageConfig != null ? stageConfig.waves.Count : 0;
}
