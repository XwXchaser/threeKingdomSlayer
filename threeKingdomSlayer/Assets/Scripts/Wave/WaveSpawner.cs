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
    private float _nextSnapshotTime;

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

    private void Update()
    {
        if (Time.unscaledTime >= _nextSnapshotTime)
        {
            _nextSnapshotTime = Time.unscaledTime + 5f;
            int waveAlive = AttackWave.AliveCount;
            int sweepAlive = SweepEffect.AliveCount;
            if (waveAlive > 0 || sweepAlive > 0)
                Debug.Log($"[EffectLeak] AttackWave alive={waveAlive}, SweepEffect alive={sweepAlive}, frame={Time.frameCount}");
        }
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

        // 清理上一波残留特效，避免与 BOSS 入场动画重叠
        CleanupLingeringEffects();

        // 通知波次开始
        OnWaveStarted?.Invoke(currentWaveIndex);
        PlayerState.Instance?.SetCurrentWave(currentWaveIndex + 1);

        Debug.Log($"[WaveSpawner] 开始生成第 {currentWaveIndex + 1} 波，共 {wave.rows.Count} 排敌人");

        // 生成该波的所有排
        FillUpRule fillRule = ResolvedStageConfig?.fillUpRule ?? FillUpRule.PerColumn;
        if (fillRule == FillUpRule.PerRow)
        {
            // PerRow: 每排使用顺序递增的排号，确保 rowIndex 正确对应配置中的排位
            int rowIdx = 0;
            foreach (RowConfig row in wave.rows)
            {
                SpawnRow(row, fillRule, rowIdx++);
            }
        }
        else
        {
            foreach (RowConfig row in wave.rows)
            {
                SpawnRow(row, fillRule);
            }
        }

        isSpawning = false;

        // 波次生成完成后，创建共享血量组
        // 注意：必须在补齐前创建。UpdateMovement 中的解散检查已跳过正在补齐的成员，
        // 所以组会安全度过补齐期，补齐完成后所有成员在同一排自然不会被解散。
        CreateSharedHealthGroups();

        // 波次生成完成后，PerRow 检测清空排并压缩
        if (fillRule == FillUpRule.PerRow)
            columnManager.RowBasedFillUp();

        // 启动波次行军：跨列整排推进，敌人从生成排向 row=0 前进
        columnManager.StartWaveMarch();

        // Boss 独立补齐：波次行军跳过 Boss，需单独触发
        columnManager.TriggerAllBossFillForward();

        // 启动协程等待当前波次所有敌人死亡
        StartCoroutine(WaitForWaveClearAndNotify());
    }

    /// <summary>
    /// 等待当前波次清空，然后触发完成事件
    /// </summary>
    private IEnumerator WaitForWaveClearAndNotify()
    {
        // BUG FIX: 捕获当前波次索引为局部变量，避免 OnWaveCompleted 回调同步调用
        // SpawnNextWave() 修改 currentWaveIndex 后，旧协程的 final check 读到新值导致误判胜利
        int capturedWaveIndex = currentWaveIndex;

        // 等待直到所有敌人都死亡
        while (enemyManager != null && !enemyManager.IsAllEnemiesDead)
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 波次已清空
        isWaveComplete = true;
        OnWaveCompleted?.Invoke(capturedWaveIndex);

        Debug.Log($"[WaveSpawner] 第 {capturedWaveIndex + 1} 波已清空");

        // 检查是否所有波次已完成（最后一波清空后触发胜利）
        // 必须用 capturedWaveIndex 而非 currentWaveIndex
        if (capturedWaveIndex >= ResolvedStageConfig.waves.Count - 1)
        {
            isAllWavesCompleted = true;
            OnAllWavesCompleted?.Invoke();
        }

        // 注意：不自动生成下一波，等待 StageController 在玩家点击"继续"后调用 SpawnNextWave()
    }

    /// <summary>
    /// 生成一排敌人
    /// enemyIds[i] 直接对应第 i 列（0=最左, 4=最右）。填 0 表示该列为空。
    /// 该排所有敌人共享相同的排索引（rowIndex）。
    /// </summary>
    /// <param name="explicitRowIndex">PerRow 模式下的显式排号（>=0 时直接使用，无需计算）</param>
    private void SpawnRow(RowConfig row, FillUpRule fillRule = FillUpRule.PerColumn, int explicitRowIndex = -1)
    {
        if (row.enemyIds == null || row.enemyIds.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] RowConfig.enemyIds 为空，跳过");
            return;
        }

        // 该排所有敌人共享相同的排索引
        int rowIndex;
        if (explicitRowIndex >= 0)
        {
            // PerRow 模式：推后2排，由 RowBasedFillUp 触发逐步前移
            rowIndex = explicitRowIndex + 2;
        }
        else
        {
            // PerColumn 模式：基于最大列数量计算后排位置
            rowIndex = 0;
            if (columnManager != null)
            {
                for (int c = 0; c < 5; c++)
                {
                    int count = columnManager.GetColumnEnemyCount(c);
                    if (count > rowIndex)
                        rowIndex = count;
                }
            }
            rowIndex += 2; // 从后排出场，通过 TriggerFillForward 前移
        }

        for (int i = 0; i < row.enemyIds.Length; i++)
        {
            int enemyId = row.enemyIds[i];
            if (enemyId <= 0) continue;

            // 从对象池获取敌人
            Enemy enemy = enemyPool?.GetEnemy(enemyId);
            if (enemy == null)
            {
                Debug.LogWarning($"[WaveSpawner] 无法获取敌人ID {enemyId}，跳过");
                continue;
            }

            // 直接使用数组索引作为列号：enemyIds[0]=列0(最左), enemyIds[4]=列4(最右)
            Debug.Log($"[WaveSpawner] 生成敌人 id={enemyId} → 列{i} 排{rowIndex} (fillRule={fillRule})");
            enemy.Initialize(i, rowIndex);

            // 应用波次敌人强化（血量倍率 + 颜色叠加）
            WaveConfig currentWave = ResolvedStageConfig?.waves[currentWaveIndex];
            if (currentWave != null)
                enemy.ApplyWaveScaling(currentWave.healthMultiplier, currentWave.waveTintColor,
                    currentWave.attackSpeedMultiplier, currentWave.damageMultiplier);

            // 注册到管理器
            enemyManager?.RegisterEnemy(enemy);
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

    /// <summary>
    /// 清理上一波残留特效（AttackWave + DamageNumber），避免与 BOSS 入场动画重叠
    /// </summary>
    public void CleanupLingeringEffects()
    {
        // 销毁所有活跃的攻击波特效
        var activeWaves = FindObjectsOfType<AttackWave>();
        foreach (var w in activeWaves)
        {
            if (w != null && w.gameObject != null)
            {
                float survival = Time.unscaledTime - w.CreationTime;
                Debug.Log($"[WaveSpawner] Cleanup lingering AttackWave: name={w.gameObject.name}, id={w.gameObject.GetInstanceID()}, damageType={w.DamageType}, survival={survival:F2}s, frame={Time.frameCount}");
                Destroy(w.gameObject);
            }
        }
        if (activeWaves.Length > 0)
            Debug.Log($"[WaveSpawner] 清理了 {activeWaves.Length} 个残留攻击波");

        // 销毁所有活跃的斩击特效
        var activeSweeps = FindObjectsOfType<SweepEffect>();
        foreach (var s in activeSweeps)
        {
            if (s != null && s.gameObject != null)
            {
                float survival = Time.unscaledTime - s.CreationTime;
                Debug.Log($"[WaveSpawner] Cleanup lingering SweepEffect: name={s.gameObject.name}, id={s.gameObject.GetInstanceID()}, damageType={s.DamageType}, survival={survival:F2}s, frame={Time.frameCount}");
                Destroy(s.gameObject);
            }
        }
        if (activeSweeps.Length > 0)
            Debug.Log($"[WaveSpawner] 清理了 {activeSweeps.Length} 个残留斩击");

        // 回收所有活跃的伤害跳字
        var activeDNs = FindObjectsOfType<DamageNumber>();
        int cleaned = 0;
        foreach (var dn in activeDNs)
        {
            if (dn != null && dn.gameObject != null && dn.gameObject.activeSelf && dn.OnReturnToPool != null)
            {
                dn.ResetNumber();
                cleaned++;
            }
        }
        if (cleaned > 0)
            Debug.Log($"[WaveSpawner] 清理了 {cleaned} 个残留伤害跳字");
    }

    #region 共享血量组

    /// <summary>
    /// 扫描当前所有存活敌人，为同行相邻同ID且 shareHealthWithAdjacent 的敌人创建共享血量组
    /// </summary>
    private void CreateSharedHealthGroups()
    {
        if (enemyManager == null) return;

        var allEnemies = enemyManager.GetAllAliveEnemies();
        if (allEnemies.Count == 0) return;

        // 按 rowIndex 分组
        var rows = new Dictionary<int, List<Enemy>>();
        foreach (var enemy in allEnemies)
        {
            if (enemy == null || !enemy.shareHealthWithAdjacent) continue;
            if (enemy.sharedHealthGroup != null) continue;

            if (!rows.ContainsKey(enemy.rowIndex))
                rows[enemy.rowIndex] = new List<Enemy>();
            rows[enemy.rowIndex].Add(enemy);
        }

        foreach (var kvp in rows)
        {
            var rowEnemies = kvp.Value;
            rowEnemies.Sort((a, b) => a.columnIndex.CompareTo(b.columnIndex));

            int i = 0;
            while (i < rowEnemies.Count)
            {
                int start = i;
                int enemyId = rowEnemies[i].enemyId;
                i++;

                while (i < rowEnemies.Count
                    && rowEnemies[i].enemyId == enemyId
                    && rowEnemies[i].columnIndex == rowEnemies[i - 1].columnIndex + 1)
                {
                    i++;
                }

                int count = i - start;
                if (count >= 2)
                {
                    var group = new SharedHealthGroup(
                        enemyManager.sharedHealthChainPrefab,
                        enemyManager.chainScale,
                        enemyManager.chainYOffset);

                    for (int j = start; j < i; j++)
                        group.AddMember(rowEnemies[j]);

                    group.SpawnChains();
                    enemyManager.RegisterGroup(group);

                    Debug.Log($"[WaveSpawner] 创建共享血量组: enemyId={enemyId}, row={kvp.Key}, members={count}, poolHP={group.currentHealth:F0}/{group.maxHealth:F0}");
                }
            }
        }
    }

    #endregion
}
