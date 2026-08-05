using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 列管理器
/// 管理5列敌人，提供添加/移除/查询接口
/// 实现补齐逻辑：当某列前排死亡，后排自动前移
/// </summary>
public class ColumnManager : MonoBehaviour
{
    [Header("列配置")]
    public int columnCount = 5; // 默认5列

    // 5列敌人列表
    private Column[] columns;

    // 位移系统复用容器（避免每帧 new 分配导致 GC）
    private readonly List<Enemy> _pushWorkList = new List<Enemy>();
    private readonly HashSet<Enemy> _pushHitSet = new HashSet<Enemy>();
    private readonly Dictionary<int, List<Enemy>> _pushByColumn = new Dictionary<int, List<Enemy>>();
    private readonly Dictionary<int, List<Enemy>> _rowEnemies = new Dictionary<int, List<Enemy>>();

    // GC 优化：范围查询复用列表
    private readonly List<Enemy> _rangeQueryList = new List<Enemy>();

    // Wave march has a preparing barrier and one exact generation per row step.
    private bool _isWaveMarching;
    private bool _isWavePreparing;
    private int _currentWaveSourceRow = -1;
    private int _currentWaveTargetRow = -1;
    private int _waveGeneration;
    private bool _waveMarchRequestedWhilePushReturn;
    private bool _topologyChangedWhilePushReturn;
    private bool _hasPausedWaveStep;
    private int _pausedWaveSourceRow = -1;
    private int _pausedWaveTargetRow = -1;
    private readonly List<Enemy> _pausedWaveEnemies = new List<Enemy>();
    private readonly List<Enemy> _preparingWaveEnemies = new List<Enemy>();
    private readonly HashSet<Enemy> _pendingWaveEnemies = new HashSet<Enemy>();

    private sealed class PushReturnTransaction
    {
        public Enemy enemy;
        public int originalColumn;
        public int originalRow;
        public int displacedColumn;
        public int displacedRow;
        public int generation;
        public int orderId;
        public float returnDueTime;
        public bool scheduled;
        public int lastBlockedRow = int.MinValue;
    }

    private const float pushReturnDelay = 0.35f;
    private bool _suppressPushReturnResume;
    private readonly Dictionary<Enemy, PushReturnTransaction> _pushReturnTransactions = new Dictionary<Enemy, PushReturnTransaction>();
    private readonly List<PushReturnTransaction> _pushReturnWorkList = new List<PushReturnTransaction>();
    private int _pushReturnGeneration;
    private int _pushReturnOrderId;

    /// <summary>
    /// 列结构变化事件（RemoveEnemy / UpdateEnemyRow 后触发）
    /// Boss 用此事件检测前排是否清空以恢复推进
    /// </summary>
    public System.Action OnColumnsModified;

    private void Awake()
    {
        InitializeColumns();
    }

    private void Update()
    {
        UpdatePushReturns();

        if (!_isWavePreparing) return;

        for (int i = _preparingWaveEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = _preparingWaveEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || !enemy.IsRushMoveOrder(RushMoveOrderOwner.WaveMarch, _waveGeneration))
            {
                if (enemy != null)
                    enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
                _preparingWaveEnemies.RemoveAt(i);
                continue;
            }
            if (!enemy.IsRushMoveReady)
                return;
        }

        if (_preparingWaveEnemies.Count == 0)
        {
            _isWavePreparing = false;
            _currentWaveSourceRow = -1;
            _currentWaveTargetRow = -1;
            StartWaveMarch();
            return;
        }

        StartPreparedWaveStep();
    }

    private void InitializeColumns()
    {
        columns = new Column[columnCount];
        for (int i = 0; i < columnCount; i++)
        {
            columns[i] = new Column(i);
        }
    }

    #region 添加敌人

    /// <summary>
    /// 向指定列添加敌人（追加到队列末尾）
    /// </summary>
    public void AddEnemyToColumn(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        columns[columnIndex].AddEnemy(enemy);
    }

    /// <summary>
    /// 批量向指定列添加敌人
    /// </summary>
    public void AddEnemiesToColumn(int columnIndex, List<Enemy> enemies)
    {
        if (!IsValidColumn(columnIndex)) return;

        foreach (var enemy in enemies)
        {
            columns[columnIndex].AddEnemy(enemy);
        }
    }

    /// <summary>
    /// 向所有列添加敌人（用于波次生成）
    /// 传入长度为5的列表，每个元素是对应列的敌人列表
    /// </summary>
    public void AddEnemiesToAllColumns(List<Enemy>[] columnEnemies)
    {
        for (int i = 0; i < columnCount && i < columnEnemies.Length; i++)
        {
            if (columnEnemies[i] != null)
            {
                AddEnemiesToColumn(i, columnEnemies[i]);
            }
        }
    }

    #endregion

    #region 移除敌人

    /// <summary>
    /// 从指定列移除敌人
    /// </summary>
    public void RemoveEnemyFromColumn(int columnIndex, Enemy enemy, bool skipChain = false)
    {
        if (!IsValidColumn(columnIndex)) return;

        columns[columnIndex].RemoveEnemy(enemy, skipChain: true);
        ReleaseEnemyFromSchedulers(enemy, columnIndex);
        columns[columnIndex].ResumeRushMoveChain();

        // Ordinary death only changes topology; wave march decides whether a cross-column row may advance.
        Debug.Log($"[WaveMarch] RemoveEnemyFromColumn col={columnIndex} enemy={enemy.DebugTag} → StartWaveMarch");
        StartWaveMarch();
        NotifyPushReturnTopologyChanged();
        OnColumnsModified?.Invoke();
    }

    /// <summary>
    /// 触发补齐前移（Boss死亡延迟补齐用）。
    /// 委托给 Column.TriggerFillForward，由调用方自行控制。
    /// </summary>
    public void TriggerFillForward(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return;
        columns[columnIndex].TriggerFillForward();
        OnColumnsModified?.Invoke();
    }

    /// <summary>
    /// 扫描所有列，触发 Boss 独立补齐（StartWaveMarch 跳过 Boss，需单独调用）。
    /// 波次生成后和选择完成后调用。
    /// </summary>
    public void TriggerAllBossFillForward()
    {
        for (int i = 0; i < columnCount; i++)
            columns[i].TriggerBossFillForward();
    }

    /// <summary>
    /// 清空所有列
    /// </summary>
    public void ClearAllColumns()
    {
        AbortWaveMarch();
        CancelAllPushReturns();
        _waveMarchRequestedWhilePushReturn = false;
        _topologyChangedWhilePushReturn = false;
        _hasPausedWaveStep = false;
        _pausedWaveSourceRow = -1;
        _pausedWaveTargetRow = -1;
        _pausedWaveEnemies.Clear();
        for (int i = 0; i < columnCount; i++)
            columns[i].enemies.Clear();
    }

    /// <summary>
    /// 清空指定列
    /// </summary>
    public void ClearColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return;
        for (int i = columns[columnIndex].enemies.Count - 1; i >= 0; i--)
            CancelPushReturn(columns[columnIndex].enemies[i], "cancel-reset");
        columns[columnIndex].enemies.Clear();
    }

    #endregion

    #region 更新敌人位置

    /// <summary>
    /// A normal movement/landing topology change only requests a wave rescan.
    /// </summary>
    public void UpdateEnemyRow(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        NotifyPushReturnTopologyChanged();
        OnColumnsModified?.Invoke();
        StartWaveMarch();
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 获取指定列
    /// </summary>
    public Column GetColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return columns[columnIndex];
    }

    /// <summary>
    /// 获取指定列的最前排敌人
    /// </summary>
    public Enemy GetFrontEnemy(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return columns[columnIndex].GetFrontEnemy();
    }

    /// <summary>
    /// 获取指定列指定排的敌人
    /// </summary>
    public Enemy GetEnemyAt(int columnIndex, int rowIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return columns[columnIndex].GetEnemyAtRow(rowIndex);
    }

    /// <summary>
    /// 获取指定列的所有敌人
    /// </summary>
    public List<Enemy> GetEnemiesInColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return columns[columnIndex].enemies;
    }

    /// <summary>
    /// Returns the living InCombat Boss whose horizontal footprint covers the requested column.
    /// Bosses remain stored in their center column; coverage only changes attack targeting.
    /// </summary>
    public Enemy GetCombatBossCoveringColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;

        for (int centerColumn = 0; centerColumn < columnCount; centerColumn++)
        {
            var enemies = columns[centerColumn].enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isBoss || enemy.state == EnemyState.Dead || enemy.bossState != BossState.InCombat)
                    continue;

                int footprint = Mathf.Clamp(enemy.occupySlots, 1, columnCount);
                int left = Mathf.Clamp(enemy.columnIndex - footprint / 2, 0, columnCount - footprint);
                int right = left + footprint - 1;
                if (columnIndex >= left && columnIndex <= right)
                    return enemy;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取所有列的所有敌人（用于遍历攻击）
    /// </summary>
    public List<Enemy> GetAllEnemies()
    {
        List<Enemy> allEnemies = new List<Enemy>();
        for (int i = 0; i < columnCount; i++)
        {
            allEnemies.AddRange(columns[i].enemies);
        }
        return allEnemies;
    }

    /// <summary>
    /// 获取指定列中 rowIndex 小于 rangeRows 的所有敌人（按排索引而非列表位置过滤）
    /// Boss 敌人始终包含在内，不受 rangeRows 限制
    /// </summary>
    public List<Enemy> GetEnemiesInRange(int columnIndex, int rangeRows)
    {
        List<Enemy> result = new List<Enemy>();
        if (!IsValidColumn(columnIndex)) return result;

        Column column = columns[columnIndex];
        for (int i = 0; i < column.enemies.Count; i++)
        {
            var e = column.enemies[i];
            if (e.rowIndex < rangeRows || (e.isBoss && e.bossState == BossState.InCombat))
                result.Add(e);
        }
        return result;
    }

    /// <summary>
    /// 获取所有列前N排的所有敌人（用于斩击/横扫等范围攻击）
    /// </summary>
    public List<Enemy> GetAllEnemiesInRange(int rangeRows)
    {
        // 复用列表，内联查询避免 GetEnemiesInRange 的中间分配
        _rangeQueryList.Clear();
        for (int i = 0; i < columnCount; i++)
        {
            if (!IsValidColumn(i)) continue;
            var col = columns[i];
            for (int j = 0; j < col.enemies.Count; j++)
            {
                var e = col.enemies[j];
                if (e.rowIndex < rangeRows || (e.isBoss && e.bossState == BossState.InCombat))
                    _rangeQueryList.Add(e);
            }
        }
        return _rangeQueryList;
    }

    /// <summary>
    /// 获取所有列中 rowIndex 小于等于 maxRowIndex 的敌人（按排索引而非列表位置过滤）
    /// </summary>
    public List<Enemy> GetEnemiesByRowLimit(int maxRowIndex)
    {
        List<Enemy> result = new List<Enemy>();
        for (int i = 0; i < columnCount; i++)
        {
            Column column = columns[i];
            for (int j = 0; j < column.enemies.Count; j++)
            {
                if (column.enemies[j].rowIndex <= maxRowIndex)
                    result.Add(column.enemies[j]);
            }
        }
        return result;
    }

    /// <summary>
    /// 指定列是否为空
    /// </summary>
    public bool IsColumnEmpty(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return true;
        return columns[columnIndex].IsEmpty;
    }

    /// <summary>
    /// 是否所有列为空
    /// </summary>
    public bool AreAllColumnsEmpty()
    {
        for (int i = 0; i < columnCount; i++)
        {
            if (!columns[i].IsEmpty) return false;
        }
        return true;
    }

    /// <summary>
    /// 获取指定列的敌人数量
    /// </summary>
    public int GetColumnEnemyCount(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return 0;
        return columns[columnIndex].EnemyCount;
    }

    /// <summary>
    /// 获取所有列的敌人总数
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int total = 0;
        for (int i = 0; i < columnCount; i++)
        {
            total += columns[i].EnemyCount;
        }
        return total;
    }

    #endregion

    #region 波次行军（规则1/2）

    /// <summary>
    /// Cross-column wave march. The source row is prepared first; Update waits for every
    /// eligible member, then starts all one-row moves in the same frame.
    /// </summary>
    public void StartWaveMarch()
    {
        if (_pushReturnTransactions.Count > 0)
        {
            _waveMarchRequestedWhilePushReturn = true;
            _topologyChangedWhilePushReturn = true;
            Debug.Log($"[WaveMarch] StartWaveMarch deferred: pushReturnTx={_pushReturnTransactions.Count}");
            return;
        }
        if (_isWaveMarching || _isWavePreparing)
        {
            Debug.Log($"[WaveMarch] StartWaveMarch blocked: _isWaveMarching={_isWaveMarching} _isWavePreparing={_isWavePreparing} srcRow={_currentWaveSourceRow} tgtRow={_currentWaveTargetRow}");
            return;
        }

        int maxRow = GetMaxOccupiedRow();
        for (int r = 0; r < maxRow; r++)
        {
            if (IsRowFullyVacated(r) && !IsRowFullyVacated(r + 1))
            {
                Debug.Log($"[WaveMarch] StartWaveMarch → BeginWaveStep srcRow={r + 1} tgtRow={r}");
                BeginWaveStep(r + 1, r);
                return;
            }
        }
    }

    private void BeginWaveStep(int sourceRow, int targetRow, IList<Enemy> restrictedEnemies = null)
    {
        _waveGeneration++;
        if (_waveGeneration <= 0) _waveGeneration = 1;
        _currentWaveSourceRow = sourceRow;
        _currentWaveTargetRow = Mathf.Max(0, targetRow);
        _preparingWaveEnemies.Clear();
        _pendingWaveEnemies.Clear();

        for (int c = 0; c < columnCount; c++)
        {
            foreach (var enemy in columns[c].enemies)
            {
                if (enemy == null || enemy.isBoss || enemy.state == EnemyState.Dead || enemy.rowIndex != sourceRow)
                    continue;
                if (restrictedEnemies != null && !restrictedEnemies.Contains(enemy))
                    continue;

                if (_pushReturnTransactions.ContainsKey(enemy))
                    continue;

                if (!enemy.AssignRushMoveOrder(RushMoveOrderOwner.WaveMarch, _waveGeneration, _currentWaveTargetRow))
                    continue;

                enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
                enemy.OnRushMoveComplete += OnWaveEnemyRushComplete;
                _preparingWaveEnemies.Add(enemy);
            }
        }

        if (_preparingWaveEnemies.Count == 0)
        {
            Debug.Log($"[WaveMarch] BeginWaveStep srcRow={sourceRow} tgtRow={targetRow}: NO enemies eligible (all boss/dead/rush-order-rejected)");
            _currentWaveSourceRow = -1;
            _currentWaveTargetRow = -1;
            return;
        }

        _isWavePreparing = true;
    }

    private void StartPreparedWaveStep()
    {
        int generation = _waveGeneration;
        _isWavePreparing = false;
        _isWaveMarching = true;
        _pendingWaveEnemies.Clear();

        for (int i = 0; i < _preparingWaveEnemies.Count; i++)
        {
            var enemy = _preparingWaveEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead)
                continue;
            if (!enemy.IsRushMoveOrder(RushMoveOrderOwner.WaveMarch, generation))
                continue;
            _pendingWaveEnemies.Add(enemy);
        }
        _preparingWaveEnemies.Clear();

        foreach (var enemy in _pendingWaveEnemies)
            enemy.TryStartRushMove();

        if (_pendingWaveEnemies.Count == 0)
            CompleteWaveStep(generation);
    }

    private void OnWaveEnemyRushComplete(Enemy enemy, RushMoveOrderOwner owner, int generation)
    {
        if (owner != RushMoveOrderOwner.WaveMarch || generation != _waveGeneration)
            return;

        enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
        _pendingWaveEnemies.Remove(enemy);
        if (_pendingWaveEnemies.Count == 0)
            CompleteWaveStep(generation);
    }

    private void CompleteWaveStep(int generation)
    {
        if (generation != _waveGeneration) return;

        _isWaveMarching = false;
        _isWavePreparing = false;
        _currentWaveSourceRow = -1;
        _currentWaveTargetRow = -1;
        _pendingWaveEnemies.Clear();
        _preparingWaveEnemies.Clear();
        Debug.Log($"[WaveMarch] CompleteWaveStep gen={generation} → chain StartWaveMarch");
        StartWaveMarch();
    }

    /// <summary>
    /// Captures the exact current wave order before the first push transaction so temporary holes
    /// cannot be reinterpreted. After all returns finish, only the captured members/target are revalidated.
    /// </summary>
    private void CapturePausedWaveStep()
    {
        if (!_isWaveMarching && !_isWavePreparing)
            return;

        _hasPausedWaveStep = true;
        _pausedWaveSourceRow = _currentWaveSourceRow;
        _pausedWaveTargetRow = _currentWaveTargetRow;
        _pausedWaveEnemies.Clear();

        for (int i = 0; i < _preparingWaveEnemies.Count; i++)
        {
            var enemy = _preparingWaveEnemies[i];
            if (enemy != null && !_pausedWaveEnemies.Contains(enemy))
                _pausedWaveEnemies.Add(enemy);
        }
        foreach (var enemy in _pendingWaveEnemies)
        {
            if (enemy != null && !_pausedWaveEnemies.Contains(enemy))
                _pausedWaveEnemies.Add(enemy);
        }
    }

    private void AbortWaveMarch(bool preserveForPushReturn = false)
    {
        if (preserveForPushReturn)
            CapturePausedWaveStep();

        CancelInvoke(nameof(StartWaveMarch));
        _waveGeneration++;

        for (int i = 0; i < _preparingWaveEnemies.Count; i++)
        {
            var enemy = _preparingWaveEnemies[i];
            if (enemy == null) continue;
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            enemy.CancelRushMoveOrder(resetActiveMovement: true);
        }
        foreach (var enemy in _pendingWaveEnemies)
        {
            if (enemy == null) continue;
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            enemy.CancelRushMoveOrder(resetActiveMovement: true);
        }

        _preparingWaveEnemies.Clear();
        _pendingWaveEnemies.Clear();
        _isWavePreparing = false;
        _isWaveMarching = false;
        _currentWaveSourceRow = -1;
        _currentWaveTargetRow = -1;
    }

    private void ReleaseEnemyFromSchedulers(Enemy enemy, int sourceColumn)
    {
        if (enemy == null) return;

        ReleaseEnemyFromMovementSchedulers(enemy, sourceColumn);
        CancelPushReturn(enemy, enemy.state == EnemyState.Dead ? "cancel-dead" : "cancel-removed");
    }

    private void ReleaseEnemyFromMovementSchedulers(Enemy enemy, int sourceColumn)
    {
        if (enemy == null) return;

        bool wasWaveMember = _pendingWaveEnemies.Remove(enemy);
        if (_preparingWaveEnemies.Remove(enemy))
            wasWaveMember = true;
        if (wasWaveMember)
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;

        if (IsValidColumn(sourceColumn))
            columns[sourceColumn].ReleaseRushMoveOwnership(enemy);

        enemy.CancelRushMoveOrder(resetActiveMovement: true);

        if (wasWaveMember && _pendingWaveEnemies.Count == 0 && _preparingWaveEnemies.Count == 0)
        {
            _isWavePreparing = false;
            _isWaveMarching = false;
            _currentWaveSourceRow = -1;
            _currentWaveTargetRow = -1;
            // 修复（WaveMarch死锁）：WaveMarch 步进成员被位移（横推/击退/移除）打断且集合清空后，
            // 必须补发一次全局补齐扫描。此前只复位 flag 不重扫，导致补齐链永久暂停：
            // 横推清空集合后无人重扫；随后 Stab 击退触发 RegisterPushReturn 时因 flag 已 false
            // 而 AbortWaveMarch 不保存 paused，PushReturn 完成后无从恢复 → 永久死锁。
            // StartWaveMarch 自带 PushReturn 感知：有挂起事务时 deferred 并置
            // _waveMarchRequestedWhilePushReturn=true，由 TryResumePausedWaveAfterPushReturns 恢复。
            StartWaveMarch();
        }
    }

    /// <summary>
    /// 检查指定排是否在所有列均无存活（非 Dead）的敌人。
    /// Launched 敌人仍占据排位——击飞不等于空出。
    /// </summary>
    public bool IsRowFullyVacated(int row)
    {
        for (int c = 0; c < columnCount; c++)
        {
            foreach (var e in columns[c].enemies)
            {
                if (e == null) continue;
                if (e.state == EnemyState.Dead) continue;
                if (e.rowIndex == row) return false;
            }
        }
        return true;
    }

    private int GetMaxOccupiedRow()
    {
        int max = 0;
        for (int c = 0; c < columnCount; c++)
        {
            foreach (var e in columns[c].enemies)
            {
                if (e == null || e.state == EnemyState.Dead) continue;
                if (e.rowIndex > max) max = e.rowIndex;
            }
        }
        return max;
    }

    #endregion

    #region 精确击退回位

    /// <summary>
    /// Exact-slot backward-push returns. Only registered pushed enemies are considered;
    /// no scan may create a return for an unrelated enemy.
    /// </summary>
    private int NextPushReturnGeneration()
    {
        _pushReturnGeneration++;
        if (_pushReturnGeneration <= 0) _pushReturnGeneration = 1;
        return _pushReturnGeneration;
    }

    private int NextPushReturnOrderId()
    {
        _pushReturnOrderId++;
        if (_pushReturnOrderId <= 0) _pushReturnOrderId = 1;
        return _pushReturnOrderId;
    }

    private void RegisterPushReturn(Enemy enemy, int originalColumn, int originalRow, int displacedRow)
    {
        if (_pushReturnTransactions.TryGetValue(enemy, out var transaction))
        {
            enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
            enemy.CancelRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation, resetActiveMovement: true);

            transaction.displacedColumn = enemy.columnIndex;
            transaction.displacedRow = displacedRow;
            transaction.generation = NextPushReturnGeneration();
            transaction.orderId = NextPushReturnOrderId();
            transaction.returnDueTime = Time.time + pushReturnDelay;
            transaction.scheduled = true;
            transaction.lastBlockedRow = int.MinValue;
            DebugLog.Info($"[PushReturn] reschedule {enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) target=({originalColumn},{displacedRow}) gen={transaction.generation} order={transaction.orderId}");
            return;
        }

        if (_pushReturnTransactions.Count == 0)
        {
            AbortWaveMarch(preserveForPushReturn: true);
            _topologyChangedWhilePushReturn = false;
        }

        int generation = NextPushReturnGeneration();
        int orderId = NextPushReturnOrderId();

        transaction = new PushReturnTransaction
        {
            enemy = enemy,
            originalColumn = originalColumn,
            originalRow = originalRow,
            displacedColumn = originalColumn,
            displacedRow = displacedRow,
            generation = generation,
            orderId = orderId,
            returnDueTime = Time.time + pushReturnDelay,
            scheduled = true
        };
        _pushReturnTransactions.Add(enemy, transaction);
        DebugLog.Info($"[PushReturn] register {enemy.DebugTag} origin=({originalColumn},{originalRow}) target=({originalColumn},{displacedRow}) gen={transaction.generation} order={transaction.orderId}");
    }

    private void UpdatePushReturns()
    {
        if (_pushReturnTransactions.Count == 0)
            return;

        _pushReturnWorkList.Clear();
        foreach (var transaction in _pushReturnTransactions.Values)
            _pushReturnWorkList.Add(transaction);

        for (int i = 0; i < _pushReturnWorkList.Count; i++)
        {
            var transaction = _pushReturnWorkList[i];
            var enemy = transaction.enemy;
            if (enemy == null || enemy.state == EnemyState.Dead || !enemy.gameObject.activeInHierarchy)
            {
                CancelPushReturn(enemy, "cancel-dead");
                continue;
            }
            if (!transaction.scheduled || Time.time < transaction.returnDueTime)
                continue;
            if (enemy.IsRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation))
                continue;
            if (enemy.HasRushMoveOrder)
                continue;

            TrySchedulePushReturnStep(transaction);
        }
    }

    private void TrySchedulePushReturnStep(PushReturnTransaction transaction)
    {
        var enemy = transaction.enemy;
        if (enemy.columnIndex != transaction.originalColumn)
        {
            LogPushReturnBlocked(transaction, enemy.rowIndex);
            return;
        }

        if (enemy.rowIndex == transaction.originalRow)
        {
            CompletePushReturn(transaction);
            return;
        }

        if (enemy.rowIndex < transaction.originalRow)
        {
            LogPushReturnBlocked(transaction, enemy.rowIndex);
            return;
        }

        int nextRow = enemy.rowIndex - 1;
        var column = columns[transaction.originalColumn];
        if (column.IsRowOccupied(nextRow, enemy))
        {
            LogPushReturnBlocked(transaction, nextRow);
            return;
        }

        if (!enemy.AssignRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation, nextRow))
        {
            LogPushReturnBlocked(transaction, nextRow);
            return;
        }

        enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
        enemy.OnRushMoveComplete += OnPushReturnStepComplete;
        var result = enemy.TryStartRushMove();
        if (result == RushMoveStartResult.Rejected)
        {
            enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
            enemy.CancelRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation, resetActiveMovement: true);
            LogPushReturnBlocked(transaction, nextRow);
            return;
        }

        transaction.lastBlockedRow = int.MinValue;
        DebugLog.Info($"[PushReturn] step {enemy.DebugTag} row={enemy.rowIndex}→{nextRow} origin=({transaction.originalColumn},{transaction.originalRow}) gen={transaction.generation} order={transaction.orderId}");
    }

    private void OnPushReturnStepComplete(Enemy enemy, RushMoveOrderOwner owner, int generation)
    {
        if (owner != RushMoveOrderOwner.PushReturn)
            return;

        enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
        if (!_pushReturnTransactions.TryGetValue(enemy, out var transaction) || transaction.generation != generation)
            return;

        if (enemy.columnIndex == transaction.originalColumn && enemy.rowIndex == transaction.originalRow)
        {
            CompletePushReturn(transaction);
            return;
        }

        transaction.lastBlockedRow = int.MinValue;
    }

    private void CompletePushReturn(PushReturnTransaction transaction)
    {
        var enemy = transaction.enemy;
        if (enemy != null)
        {
            enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
            enemy.RecheckAttackRange();
            DebugLog.Info($"[PushReturn] complete {enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) gen={transaction.generation} order={transaction.orderId}");
        }
        _pushReturnTransactions.Remove(enemy);
        TryResumePausedWaveAfterPushReturns();
    }

    private void TryResumePausedWaveAfterPushReturns()
    {
        if (_pushReturnTransactions.Count > 0)
            return;

        if (_hasPausedWaveStep)
        {
            int sourceRow = _pausedWaveSourceRow;
            int targetRow = _pausedWaveTargetRow;
            _hasPausedWaveStep = false;
            _pausedWaveSourceRow = -1;
            _pausedWaveTargetRow = -1;

            BeginWaveStep(sourceRow, targetRow, _pausedWaveEnemies);
            _pausedWaveEnemies.Clear();
            if (_isWavePreparing)
                return;

            // The captured members may all have died or become invalid while the push return
            // transaction was active. Re-scan the current topology instead of dropping the chain.
            Debug.Log($"[WaveMarch] Resume paused step produced no preparing members → rescan current topology");
            StartWaveMarch();
        }

        if (_waveMarchRequestedWhilePushReturn)
        {
            bool mayRescan = _topologyChangedWhilePushReturn;
            _waveMarchRequestedWhilePushReturn = false;
            _topologyChangedWhilePushReturn = false;
            if (mayRescan)
                StartWaveMarch();
        }
    }

    private void LogPushReturnBlocked(PushReturnTransaction transaction, int blockedRow)
    {
        if (transaction.lastBlockedRow == blockedRow)
            return;

        transaction.lastBlockedRow = blockedRow;
        DebugLog.Info($"[PushReturn] blocked {transaction.enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) current=({transaction.enemy.columnIndex},{transaction.enemy.rowIndex}) blockedRow={blockedRow} gen={transaction.generation} order={transaction.orderId}");
    }

    private void CancelPushReturn(Enemy enemy, string reason)
    {
        if (enemy == null || !_pushReturnTransactions.TryGetValue(enemy, out var transaction))
            return;

        string logReason = enemy.state == EnemyState.Dead ? "cancel-dead" : reason;
        enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
        enemy.CancelRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation, resetActiveMovement: true);
        _pushReturnTransactions.Remove(enemy);
        DebugLog.Info($"[PushReturn] {logReason} {enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) gen={transaction.generation} order={transaction.orderId}");
        if (!_suppressPushReturnResume)
            TryResumePausedWaveAfterPushReturns();
    }

    public void CancelPushReturnForEnemy(Enemy enemy, string reason = "cancel-reset")
    {
        CancelPushReturn(enemy, reason);
    }

    public void NotifyPushReturnTopologyChanged()
    {
        foreach (var transaction in _pushReturnTransactions.Values)
            transaction.lastBlockedRow = int.MinValue;
    }

    private void CancelAllPushReturns()
    {
        _waveMarchRequestedWhilePushReturn = false;
        _topologyChangedWhilePushReturn = false;
        _hasPausedWaveStep = false;
        _pausedWaveSourceRow = -1;
        _pausedWaveTargetRow = -1;
        _pausedWaveEnemies.Clear();
        _suppressPushReturnResume = true;
        _pushReturnWorkList.Clear();
        foreach (var transaction in _pushReturnTransactions.Values)
            _pushReturnWorkList.Add(transaction);
        for (int i = 0; i < _pushReturnWorkList.Count; i++)
            CancelPushReturn(_pushReturnWorkList[i].enemy, "cancel-reset");
        _pushReturnWorkList.Clear();
        _suppressPushReturnResume = false;
    }

    /// <summary>
    /// Legacy row-based compaction entry. It is intentionally inert; ordinary fill is scheduled by StartWaveMarch.
    /// </summary>
    public void RowBasedFillUp(ISet<Enemy> protectedEnemies = null)
    {
        DebugLog.Warning("[ColumnManager] RowBasedFillUp ignored: legacy compaction is disabled");
    }

    #endregion

    #region 位移效果（击退/聚拢）

    /// <summary>
    /// 将敌人移动到目标列的指定排（保留 row 位置），更新世界坐标。
    /// 若目标位置已被占据则返回 false（调用方需处理冲突）。
    /// 不触发补齐链。
    /// </summary>
    public bool MoveEnemyToColumnAtRow(Enemy enemy, int targetCol, int targetRow)
    {
        if (!IsValidColumn(targetCol)) return false;
        int srcCol = enemy.columnIndex;

        // BOSS墙壁：目标列若存在BOSS，目标排不得超过BOSS排
        int bossRow = GetBossRowInColumn(targetCol);
        if (bossRow >= 0 && targetRow >= bossRow)
        {
            targetRow = bossRow - 1;
            if (targetRow < 0)
            {
                DebugLog.Info($"[ColumnManager] MoveEnemyToColumnAtRow blocked (boss wall full): {enemy.DebugTag} → col={targetCol}, bossRow={bossRow}");
                return false;
            }
            DebugLog.Info($"[ColumnManager] MoveEnemyToColumnAtRow clamped by boss wall: {enemy.DebugTag} → col={targetCol} row={targetRow} (bossRow={bossRow})");
        }

        if (columns[targetCol].IsRowOccupied(targetRow, enemy))
        {
            DebugLog.Info($"[ColumnManager] MoveEnemyToColumnAtRow blocked: {enemy.DebugTag} → col={targetCol} row={targetRow} occupied");
            return false;
        }

        ReleaseEnemyFromSchedulers(enemy, srcCol);
        columns[srcCol].RemoveEnemySilent(enemy);
        enemy.columnIndex = targetCol;
        enemy.SetRowIndex(targetRow);
        columns[targetCol].InsertEnemySorted(enemy);

        DebugLog.Info($"[ColumnManager] MoveEnemyToColumnAtRow: {enemy.DebugTag} col {srcCol}→{targetCol}, row={targetRow}");

        // 地刺检测：跨列/跨排移动后检查是否踩中
        SpikeTrapController.Instance?.CheckAndTrigger(enemy);

        return true;
    }

    /// <summary>
    /// 将敌人追加到目标列末尾，更新世界坐标。
    /// </summary>
    public void MoveEnemyToColumnEnd(Enemy enemy, int targetCol)
    {
        if (!IsValidColumn(targetCol)) return;
        int srcCol = enemy.columnIndex;
        if (srcCol == targetCol) return;

        ReleaseEnemyFromSchedulers(enemy, srcCol);
        columns[srcCol].RemoveEnemySilent(enemy);
        int newRow = columns[targetCol].enemies.Count;
        enemy.columnIndex = targetCol;
        enemy.SetRowIndex(newRow);
        columns[targetCol].AddEnemy(enemy);

        DebugLog.Info($"[ColumnManager] MoveEnemyToColumnEnd: {enemy.DebugTag} col {srcCol}→{targetCol}, newRow={newRow}");
    }

    /// <summary>
    /// 击退栈式阻塞检测：检查某列被击中的敌人能否向后推移。
    /// 规则1：若 [maxHitRow+1, maxHitRow+pushAmount] 区间内存在非 hit 敌人 → 整列阻塞。
    /// 规则2：每个命中敌人的目标排 (rowIndex + pushAmount) 不能被非 hit 敌人占据 → 否则重叠。
    /// BOSS 不参与判断（免疫击退，不阻塞击退）。
    /// </summary>
    /// <summary>
    /// 获取指定列中BOSS的rowIndex，无BOSS返回 -1。
    /// </summary>
    private int GetBossRowInColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return -1;
        foreach (var e in columns[columnIndex].enemies)
        {
            if (e.isBoss && e.state != EnemyState.Dead)
                return e.rowIndex;
        }
        return -1;
    }

    /// <returns>true = 可以击退</returns>
    public bool CanPushColumn(int columnIndex, int pushAmount, HashSet<Enemy> hitEnemies)
    {
        if (!IsValidColumn(columnIndex)) return false;
        if (pushAmount <= 0) return false;

        var colEnemies = columns[columnIndex].enemies;
        int maxHitRow = -1;

        for (int i = 0; i < colEnemies.Count; i++)
        {
            var e = colEnemies[i];
            if (e.isBoss) continue;
            if (e.state == EnemyState.Dead) continue;
            if (!hitEnemies.Contains(e)) continue;
            if (e.rowIndex > maxHitRow) maxHitRow = e.rowIndex;
        }

        if (maxHitRow < 0) return false;

        // 规则1：最深命中敌人身后的区间不能有阻塞者
        for (int r = maxHitRow + 1; r <= maxHitRow + pushAmount; r++)
        {
            for (int i = 0; i < colEnemies.Count; i++)
            {
                var e = colEnemies[i];
                if (e.isBoss) continue;
                if (e.state == EnemyState.Dead) continue;
                if (e.rowIndex == r && !hitEnemies.Contains(e))
                {
                    DebugLog.Info($"[ColumnManager] Push blocked (tail): col={columnIndex}, blocker={e.DebugTag} at row={r}");
                    return false;
                }
            }
        }

        // 规则2：每个命中敌人的目标排不能被非命中敌人占据（防止 InsertEnemySorted 产生同排重叠）
        for (int i = 0; i < colEnemies.Count; i++)
        {
            var e = colEnemies[i];
            if (e.isBoss) continue;
            if (e.state == EnemyState.Dead) continue;
            if (!hitEnemies.Contains(e)) continue;
            int destRow = e.rowIndex + pushAmount;
            for (int j = 0; j < colEnemies.Count; j++)
            {
                var other = colEnemies[j];
                if (other.isBoss) continue;
                if (other.state == EnemyState.Dead) continue;
                if (hitEnemies.Contains(other)) continue;
                if (other.rowIndex == destRow)
                {
                    DebugLog.Info($"[ColumnManager] Push blocked (overlap): col={columnIndex}, {e.DebugTag}→row{destRow} occupied by {other.DebugTag}");
                    return false;
                }
            }
        }

        // 规则3：BOSS排作为墙壁，敌人不能被击退到BOSS所在排或之后
        int bossRow = GetBossRowInColumn(columnIndex);
        if (bossRow >= 0)
        {
            for (int i = 0; i < colEnemies.Count; i++)
            {
                var e = colEnemies[i];
                if (e.isBoss) continue;
                if (e.state == EnemyState.Dead) continue;
                if (!hitEnemies.Contains(e)) continue;
                int destRow = e.rowIndex + pushAmount;
                if (destRow >= bossRow)
                {
                    DebugLog.Info($"[ColumnManager] Push blocked (boss wall): col={columnIndex}, {e.DebugTag}→row{destRow} >= bossRow={bossRow}");
                    return false;
                }
            }
        }

        return true;
    }

    private bool CanPushEnemyFromCurrentRow(int columnIndex, Enemy enemy, int pushAmount, List<Enemy> columnHitEnemies)
    {
        if (!_pushReturnTransactions.TryGetValue(enemy, out var existing))
            return true;
        if (existing.originalColumn != columnIndex)
            return false;

        int destinationRow = enemy.rowIndex + pushAmount;
        int bossRow = GetBossRowInColumn(columnIndex);
        if (bossRow >= 0 && destinationRow >= bossRow)
            return false;

        var occupant = columns[columnIndex].GetEnemyAtRow(destinationRow);
        return occupant == null || occupant == enemy || columnHitEnemies.Contains(occupant);
    }

    /// <summary>
    /// 对单列执行击退：将该列中被击中的敌人向后移动 pushAmount 排。
    /// 预条件：已通过 CanPushColumn 检查。
    /// </summary>
    public bool ExecutePush(int columnIndex, int pushAmount, List<Enemy> columnHitEnemies)
    {
        if (!IsValidColumn(columnIndex) || columnHitEnemies == null || columnHitEnemies.Count == 0)
            return false;
        if (pushAmount <= 0)
            return false;

        var col = columns[columnIndex];
        _pushWorkList.Clear();

        for (int i = 0; i < columnHitEnemies.Count; i++)
        {
            var enemy = columnHitEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.columnIndex != columnIndex)
                continue;

            if (!CanPushEnemyFromCurrentRow(columnIndex, enemy, pushAmount, columnHitEnemies))
            {
                DebugLog.Info($"[PushReturn] blocked {enemy.DebugTag} current=({columnIndex},{enemy.rowIndex}) additionalPush={pushAmount} existingReturnPreserved");
                return false;
            }
        }

        for (int i = 0; i < columnHitEnemies.Count; i++)
        {
            var enemy = columnHitEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.columnIndex != columnIndex)
                continue;

            bool hadExistingReturn = _pushReturnTransactions.TryGetValue(enemy, out var existing);
            int firstOriginColumn = hadExistingReturn ? existing.originalColumn : columnIndex;
            int firstOriginRow = hadExistingReturn ? existing.originalRow : enemy.rowIndex;
            RegisterPushReturn(enemy, firstOriginColumn, firstOriginRow, enemy.rowIndex + pushAmount);
            ReleaseEnemyFromMovementSchedulers(enemy, columnIndex);
            _pushWorkList.Add(enemy);
        }

        if (_pushWorkList.Count == 0)
            return false;

        // Stack push must be evaluated from back to front so already-hit enemies may occupy each other's source slots.
        _pushWorkList.Sort((a, b) => b.rowIndex.CompareTo(a.rowIndex));
        int movedCount = 0;
        for (int i = 0; i < _pushWorkList.Count; i++)
        {
            var enemy = _pushWorkList[i];
            int oldRow = enemy.rowIndex;
            int newRow = oldRow + pushAmount;
            var occupant = col.GetEnemyAtRow(newRow);
            if (occupant != null && occupant != enemy)
            {
                DebugLog.Info($"[PushReturn] blocked {enemy.DebugTag} target=({columnIndex},{newRow}) occupied={occupant.DebugTag}");
                CancelPushReturn(enemy, "cancel-blocked-push");
                continue;
            }
            col.RemoveEnemySilent(enemy);
            enemy.SetRowIndex(newRow);
            col.InsertEnemySorted(enemy);
            movedCount++;

            if (_pushReturnTransactions.TryGetValue(enemy, out var transaction))
            {
                transaction.displacedColumn = columnIndex;
                transaction.displacedRow = newRow;
            }

            DebugLog.Info($"[Displacement] ExecutePush {enemy.DebugTag}: row {oldRow}→{newRow}");
        }

        for (int i = 0; i < _pushWorkList.Count; i++)
        {
            var enemy = _pushWorkList[i];
            if (!_pushReturnTransactions.ContainsKey(enemy))
                continue;
            SpikeTrapController.Instance?.CheckAndTrigger(enemy);
            enemy.RecheckAttackRange();
        }

        DebugLog.Info($"[ColumnManager] ExecutePush: col={columnIndex}, pushed={movedCount} enemies by {pushAmount} rows");
        return movedCount > 0;
    }

    /// <summary>
    /// 击退波完整逻辑：逐列检查栈式阻塞 → 执行击退。
    /// BOSS 免疫击退。
    /// 返回 true 表示至少有一列成功执行了击退。
    /// </summary>
    public bool ApplyPushWave(List<Enemy> hitEnemies, int pushAmount, bool canInterruptCFrame = false, List<Enemy> pushedEnemies = null)
    {
        if (hitEnemies == null || hitEnemies.Count == 0) return false;
        if (pushAmount <= 0) return false;

        DebugLog.Info($"[Displacement] ApplyPushWave: pushAmount={pushAmount}, hitEnemies count={hitEnemies.Count}");
        foreach (var e in hitEnemies)
        {
            if (e == null) continue;
            DebugLog.Info($"  hitEnemy: {e.DebugTag} col={e.columnIndex} row={e.rowIndex} state={e.state} isBoss={e.isBoss}");
        }

        _pushHitSet.Clear();
        _pushHitSet.UnionWith(hitEnemies);
        var hitSet = _pushHitSet;

        _pushByColumn.Clear();
        var byColumn = _pushByColumn;

        foreach (var e in hitEnemies)
        {
            if (e == null) continue;
            if (e.isBoss) continue;
            if (e.state == EnemyState.Dead) continue;
            if (e.isCFrame && !canInterruptCFrame) continue;
            if (!byColumn.ContainsKey(e.columnIndex))
            {
                var newList = new List<Enemy>();
                byColumn[e.columnIndex] = newList;
                newList.Add(e);
            }
            else
            {
                byColumn[e.columnIndex].Add(e);
            }
        }

        bool anyPushed = false;
        foreach (var kv in byColumn)
        {
            int col = kv.Key;
            bool canPush = CanPushColumn(col, pushAmount, hitSet);
            DebugLog.Info($"[Displacement] PushWave col={col}: canPush={canPush}, hitCount={kv.Value.Count}");
            if (canPush && ExecutePush(col, pushAmount, kv.Value))
            {
                if (pushedEnemies != null)
                {
                    for (int i = 0; i < kv.Value.Count; i++)
                    {
                        var enemy = kv.Value[i];
                        if (enemy != null && _pushReturnTransactions.ContainsKey(enemy))
                            pushedEnemies.Add(enemy);
                    }
                }
                anyPushed = true;
            }
        }
        return anyPushed;
    }

    /// <summary>
    /// 方向推（Slash 专属）：将击中敌人按行分组，朝 slash 方向推移 step 列。
    /// 同行多敌人自动分散到不同列，不重叠。
    /// </summary>
    public bool ApplyDirectionalPush(List<Enemy> hitEnemies, int step, bool pushRight, bool canInterruptCFrame = false, List<Enemy> movedEnemies = null)
    {
        if (hitEnemies == null || hitEnemies.Count == 0) return false;
        if (step <= 0) return false;

        DebugLog.Info($"[Displacement] ApplyDirectionalPush: step={step} pushRight={pushRight} hitCount={hitEnemies.Count}");

        _rowEnemies.Clear();
        foreach (var e in hitEnemies)
        {
            if (e.state == EnemyState.Dead || e.isBoss) continue;
            if (e.isCFrame && !canInterruptCFrame) continue;
            if (!_rowEnemies.ContainsKey(e.rowIndex))
                _rowEnemies[e.rowIndex] = new List<Enemy>();
            _rowEnemies[e.rowIndex].Add(e);
        }

        bool anyMoved = false;
        int dir = pushRight ? 1 : -1;

        foreach (var kv in _rowEnemies)
        {
            int row = kv.Key;
            var enemies = kv.Value;

            // 沿推方向排序：推右→最右优先，推左→最左优先
            if (pushRight)
                enemies.Sort((a, b) => b.columnIndex.CompareTo(a.columnIndex));
            else
                enemies.Sort((a, b) => a.columnIndex.CompareTo(b.columnIndex));

            foreach (var enemy in enemies)
            {
                int idealCol = pushRight
                    ? Mathf.Min(enemy.columnIndex + step, 4)
                    : Mathf.Max(enemy.columnIndex - step, 0);

                // 尝试理想列，被占则沿推方向找下一个可用列
                int tryCol = idealCol;
                while (tryCol >= 0 && tryCol <= 4)
                {
                    bool changesColumn = tryCol != enemy.columnIndex;
                    if (MoveEnemyToColumnAtRow(enemy, tryCol, row))
                    {
                        if (changesColumn)
                            movedEnemies?.Add(enemy);
                        anyMoved = true;
                        break;
                    }
                    tryCol += dir;
                }
            }
        }

        // Horizontal displacement changes only hit enemies' columns. It does not publish row-topology changes.
        return anyMoved;
    }

    /// <summary>
    /// 聚拢波完整逻辑：将击中敌人向 col=2 移动 step 列。
    /// 冲突裁决：仅多敌人争同一位置时 → 各承受 convergenceDamagePercent% HP 伤害 → 重新分配到 col=1/col=3 末尾。
    /// 按行分组，从 col=2 向外分配槽位，始终朝中心聚拢不越界。
    /// convergenceDamagePercent 保留签名兼容，新算法无冲突故不施加伤害。
    /// </summary>
    public void ApplyConvergenceWave(List<Enemy> hitEnemies, int step, float convergenceDamagePercent, bool canInterruptCFrame = false)
    {
        if (hitEnemies == null || hitEnemies.Count == 0) return;
        if (step <= 0) return;

        DebugLog.Info($"[Displacement] ApplyConvergenceWave: step={step} hitCount={hitEnemies.Count}");

        _rowEnemies.Clear();
        foreach (var e in hitEnemies)
        {
            if (e.state == EnemyState.Dead || e.isBoss) continue;
            if (e.isCFrame && !canInterruptCFrame) continue;
            if (!_rowEnemies.ContainsKey(e.rowIndex))
                _rowEnemies[e.rowIndex] = new List<Enemy>();
            _rowEnemies[e.rowIndex].Add(e);
        }

        // 槽位优先级：从中心 col=2 向外
        int[] prioritySlots = { 2, 1, 3, 0, 4 };

        foreach (var kv in _rowEnemies)
        {
            int row = kv.Key;
            var enemies = kv.Value;
            int N = enemies.Count;

            // 取 N 个最靠近中心的槽位
            var slots = new List<int>(N);
            for (int i = 0; i < N && i < prioritySlots.Length; i++)
                slots.Add(prioritySlots[i]);

            // 按距 col=2 由近到远排序（近者优先分配近槽）
            enemies.Sort((a, b) => Mathf.Abs(a.columnIndex - 2).CompareTo(Mathf.Abs(b.columnIndex - 2)));

            for (int i = 0; i < enemies.Count && i < slots.Count; i++)
            {
                var enemy = enemies[i];
                int slot = slots[i];
                int dist = Mathf.Abs(enemy.columnIndex - slot);
                if (dist > 0 && dist <= step)
                {
                    DebugLog.Info($"[Displacement] Conv MOVE {enemy.DebugTag} col {enemy.columnIndex}→{slot} row={row}");
                    MoveEnemyToColumnAtRow(enemy, slot, row);
                }
            }
        }

        // Convergence is horizontal-only and must not publish row-topology changes.
    }

    #endregion

    /// <summary>
    /// Arms exact-slot returns for enemies that were actually pushed backward.
    /// Complete only after this same enemy reaches its exact first origin; no other enemy is moved.
    /// </summary>
    public void PostDisplacementFillUp(IEnumerable<Enemy> pushedEnemies)
    {
        if (pushedEnemies == null) return;

        foreach (var enemy in pushedEnemies)
        {
            if (enemy == null || enemy.state == EnemyState.Dead)
                continue;
            if (_pushReturnTransactions.TryGetValue(enemy, out var transaction))
                transaction.scheduled = true;
        }
    }

    /// <summary>
    /// Legacy no-argument entry retained for API compatibility. It cannot create return orders
    /// because exact origins are registered only by ExecutePush.
    /// </summary>
    public void PostDisplacementFillUp()
    {
    }

    /// <summary>
    /// Legacy manual compaction entry retained for compatibility. It is intentionally inert;
    /// normal fill is owned exclusively by StartWaveMarch.
    /// </summary>
    public void CompactAllColumns(int rangeStart = -1, int rangeEnd = -1)
    {
        DebugLog.Warning("[ColumnManager] CompactAllColumns ignored: legacy displacement compaction is disabled");
    }

    /// <summary>
    /// 调试用：导出所有列中敌人的 (col, row, name)
    /// </summary>
    public string DumpColumns()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[DumpColumns]\n");
        for (int c = 0; c < columnCount; c++)
        {
            sb.Append($"  col={c}: ");
            var col = columns[c];
            if (col.enemies.Count == 0)
            {
                sb.Append("(empty)\n");
                continue;
            }
            for (int i = 0; i < col.enemies.Count; i++)
            {
                var e = col.enemies[i];
                sb.Append($"[{e.rowIndex}]{e.name}");
                if (i < col.enemies.Count - 1) sb.Append(", ");
            }
            sb.Append("\n");
        }
        return sb.ToString();
    }

    #region 工具方法

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < columnCount;
    }

    #endregion
}
