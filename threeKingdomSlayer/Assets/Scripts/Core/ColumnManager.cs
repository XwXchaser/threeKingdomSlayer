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
    private readonly Dictionary<Enemy, int> _convOriginalRows = new Dictionary<Enemy, int>();
    private readonly List<(Enemy enemy, int targetCol, int targetRow)> _convTargets = new List<(Enemy, int, int)>();
    private readonly Dictionary<(int col, int row), List<Enemy>> _convGroups = new Dictionary<(int, int), List<Enemy>>();
    private readonly Dictionary<int, List<Enemy>> _rowEnemies = new Dictionary<int, List<Enemy>>();

    // GC 优化：范围查询复用列表 + RowBasedFillUp 复用 HashSet
    private readonly List<Enemy> _rangeQueryList = new List<Enemy>();
    private readonly HashSet<int> _occupiedRowsSet = new HashSet<int>();

    // 波次行军状态（规则1/2）
    private bool _isWaveMarching = false;
    private int _currentWaveSourceRow = -1;
    private readonly HashSet<Enemy> _pendingWaveEnemies = new HashSet<Enemy>();

    // 击退后紧凑链计数器
    private int _compactionColumnsRemaining = 0;
    private int _compactionGeneration = 0;
    private bool _isCompactionPending = false;
    private bool _isCompactionActive = false;

    // 击退后 RowBasedFillUp 完成到紧凑链启动之间的延迟（秒），
    // 让敌人停留在击退后的位置一段时间再 Rush 补齐。
    private const float compactionStartDelay = 0.35f;

    /// <summary>
    /// 列结构变化事件（RemoveEnemy / UpdateEnemyRow 后触发）
    /// Boss 用此事件检测前排是否清空以恢复推进
    /// </summary>
    public System.Action OnColumnsModified;

    private void Awake()
    {
        InitializeColumns();
    }

    /// <summary>
    /// 初始化5列
    /// </summary>
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

        FillUpRule rule = StageController.Instance?.GetFillUpRule() ?? FillUpRule.PerColumn;
        columns[columnIndex].RemoveEnemy(enemy, skipChain: true);

        if (_pendingWaveEnemies.Remove(enemy))
        {
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            if (_pendingWaveEnemies.Count == 0)
            {
                _isWaveMarching = false;
                _currentWaveSourceRow = -1;
            }
        }

        columns[columnIndex].ResumeRushMoveChain();

        if (rule == FillUpRule.PerRow)
        {
            // PerRow：逐排补齐（数据模型压缩），再启动跨列整排行军（规则1/2）
            RowBasedFillUp();
            StartWaveMarch();
        }
        else
        {
            // PerColumn：仅移除敌人，保留缺口。整排空出后由 StartWaveMarch 统一推进（规则2）。
            StartWaveMarch();
        }
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
        for (int i = 0; i < columnCount; i++)
        {
            columns[i].enemies.Clear();
        }
    }

    /// <summary>
    /// 清空指定列
    /// </summary>
    public void ClearColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return;
        columns[columnIndex].enemies.Clear();
    }

    #endregion

    #region 更新敌人位置

    /// <summary>
    /// 更新敌人在列中的排索引（前进后调用）。
    /// PerColumn: 直接触发波次行军检查。PerRow: 触发 RowBasedFillUp。
    /// </summary>
    public void UpdateEnemyRow(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        FillUpRule rule = StageController.Instance?.GetFillUpRule() ?? FillUpRule.PerColumn;
        if (rule == FillUpRule.PerRow)
        {
            DebugLog.Info($"[ColumnManager] UpdateEnemyRow (PerRow): col={columnIndex}, {enemy.DebugTag}, 触发 RowBasedFillUp");
            RowBasedFillUp();
            OnColumnsModified?.Invoke();
            return;
        }

        // PerColumn: 自然移动后只通知观察者
        DebugLog.Info($"[ColumnManager] UpdateEnemyRow (PerColumn): col={columnIndex}, {enemy.DebugTag}");
        OnColumnsModified?.Invoke();
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
    /// 跨列波次行军：找到最前排的空排，将该排后的整排敌人一起前移一排。
    /// 所有列同排敌人同步移动，保持阵型。完成后级联检查下一排。
    /// </summary>
    public void StartWaveMarch()
    {
        if (_isWaveMarching || _isCompactionPending || _isCompactionActive) return;

        int maxRow = GetMaxOccupiedRow();
        for (int r = 0; r < maxRow; r++)
        {
            if (IsRowFullyVacated(r) && !IsRowFullyVacated(r + 1))
            {
                BeginWaveStep(r + 1, r);
                return;
            }
        }
    }

    private void BeginWaveStep(int sourceRow, int targetRow)
    {
        _pendingWaveEnemies.Clear();

        int target = Mathf.Max(0, targetRow);

        for (int c = 0; c < columnCount; c++)
        {
            foreach (var e in columns[c].enemies)
            {
                if (e == null || e.state == EnemyState.Dead) continue;
                if (e.isBoss) continue;
                if (e.state == EnemyState.Launched || e.state == EnemyState.Stunned) continue;
                if (e.rowIndex != sourceRow) continue;

                e.targetRow = target;
                e.pendingRushMove = true;
                // 攻击动作不可打断：攻击完成回调会调用 TryStartRushMove。
                // 冷却或其他可移动状态仍按原逻辑重置并立即补齐。
                if (!e.isAttackAnimating)
                    e.ResetMovementState();
                e.OnRushMoveComplete += OnWaveEnemyRushComplete;
                _pendingWaveEnemies.Add(e);
            }
        }

        // 该排仅有 Boss 或无可行军敌人：不启动行军，让调用方决定下一步
        if (_pendingWaveEnemies.Count == 0)
        {
            DebugLog.Info($"[ColumnManager] BeginWaveStep: sourceRow={sourceRow} 无可行军敌人，跳过");
            return;
        }

        _isWaveMarching = true;
        _currentWaveSourceRow = sourceRow;

        // 所有同排敌人同时启动 rush move
        foreach (var e in _pendingWaveEnemies)
        {
            e.TryStartRushMove();
        }
    }

    private void OnWaveEnemyRushComplete(Enemy enemy)
    {
        enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
        _pendingWaveEnemies.Remove(enemy);

        if (_pendingWaveEnemies.Count == 0)
        {
            _isWaveMarching = false;
            int justVacated = _currentWaveSourceRow;
            _currentWaveSourceRow = -1;

            // 级联：当前排前移后，该排空出，若后排有非Boss敌人则继续行军
            if (!IsRowFullyVacated(justVacated + 1))
            {
                BeginWaveStep(justVacated + 1, justVacated);
                // BeginWaveStep 可能因该排仅有 Boss 而跳过；若未启动则走 StartWaveMarch 继续级联
                if (!_isWaveMarching)
                    StartWaveMarch();
            }
            else
            {
                // 后排已空，检查是否还有更远的空排需要处理
                StartWaveMarch();
            }
        }
    }

    /// <summary>
    /// 中止波次行军：取消所有待处理敌人的 rush 订阅，清除状态。
    /// 用于击退开始前保存现场。
    /// </summary>
    public void AbortWaveMarch()
    {
        CancelInvoke(nameof(StartWaveMarch));
        CancelInvoke(nameof(StartAllCompactionChains));
        foreach (var e in _pendingWaveEnemies)
        {
            e.OnRushMoveComplete -= OnWaveEnemyRushComplete;
        }
        _pendingWaveEnemies.Clear();
        _isWaveMarching = false;
        _currentWaveSourceRow = -1;
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

    private void CancelCompactionChains()
    {
        CancelInvoke(nameof(StartAllCompactionChains));
        _compactionGeneration++;
        _compactionColumnsRemaining = 0;
        _isCompactionPending = false;
        _isCompactionActive = false;
        for (int c = 0; c < columnCount; c++)
            columns[c].CancelRushMoveChain();
    }

    private void OnCompactionChainComplete(int generation)
    {
        if (generation != _compactionGeneration || !_isCompactionActive)
            return;

        _compactionColumnsRemaining--;
        if (_compactionColumnsRemaining <= 0)
        {
            _compactionColumnsRemaining = 0;
            _isCompactionActive = false;
            OnColumnsModified?.Invoke();
            StartWaveMarch();
        }
    }

    private void StartAllCompactionChains()
    {
        if (!_isCompactionPending)
            return;

        _isCompactionPending = false;
        _isCompactionActive = true;
        _compactionColumnsRemaining = 0;
        int generation = _compactionGeneration;

        for (int c = 0; c < columnCount; c++)
        {
            if (columns[c].HasPendingRushEnemies())
            {
                _compactionColumnsRemaining++;
                columns[c].StartRushMoveChain(c, () => OnCompactionChainComplete(generation));
            }
        }

        if (_compactionColumnsRemaining == 0)
        {
            _isCompactionActive = false;
            OnColumnsModified?.Invoke();
            StartWaveMarch();
        }
    }

    #endregion

    #region 逐排补齐（Row-Based Fill-Up）

    /// <summary>
    /// 逐排补齐：扫描所有列中存活敌人的 rowIndex（非 Dead），
    /// 找出已完全清空的行，然后将各列敌人向清空行压缩。
    /// 在 PerRow 模式下，任何列结构变化后都应调用此方法。
    ///
    /// 注意：使用 enemy.rowIndex 而非列表位置判断排归属。
    /// RemoveEnemy(skipChain=true) 移除阵亡敌人后列表位置会变化，
    /// 但存活敌人保留原有 rowIndex，列表位置不再反映真实排号。
    /// </summary>
    public void RowBasedFillUp(int? pushedToRow = null)
    {
        // 1. 收集所有存活（非 Dead）敌人所在的排号
        int maxRow = 0;
        _occupiedRowsSet.Clear();
        var occupiedRows = _occupiedRowsSet;
        for (int c = 0; c < columnCount; c++)
        {
            foreach (var e in columns[c].enemies)
            {
                if (e == null) continue;
                if (e.state == EnemyState.Dead) continue;
                // Launched 敌人仍占据其排位置——击飞≠空位，后排不应因此前移
                int r = e.rowIndex;
                if (r > maxRow) maxRow = r;
                occupiedRows.Add(r);
            }
        }

        // 2. 确定哪些排已完全清空（跨所有列无存活敌人）
        bool[] clearRows = new bool[maxRow + 1];
        for (int r = 0; r <= maxRow; r++)
        {
            clearRows[r] = !occupiedRows.Contains(r);
            if (clearRows[r])
                DebugLog.Info($"[ColumnManager] RowBasedFillUp: 第{r}排已清空");
        }

        // 3. 各列按 clearRows 压缩，传入 pushedToRow 防止击退被补齐抵消
        for (int c = 0; c < columnCount; c++)
        {
            columns[c].CompactByClearRows(clearRows, pushedToRow);
        }
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

    /// <summary>
    /// 对单列执行击退：将该列中被击中的敌人向后移动 pushAmount 排。
    /// 预条件：已通过 CanPushColumn 检查。
    /// </summary>
    public void ExecutePush(int columnIndex, int pushAmount, List<Enemy> columnHitEnemies)
    {
        if (!IsValidColumn(columnIndex) || columnHitEnemies.Count == 0) return;

        var col = columns[columnIndex];
        int bossRow = GetBossRowInColumn(columnIndex);

        // 从列表中移除所有被击中的敌人
        foreach (var e in columnHitEnemies)
            col.RemoveEnemySilent(e);

        // 更新 rowIndex，钳制上限为 bossRow-1（BOSS排是不可逾越的墙壁）
        _pushWorkList.Clear();
        var pushedEnemies = _pushWorkList;
        foreach (var e in columnHitEnemies)
        {
            int oldRow = e.rowIndex;
            int newRow = oldRow + pushAmount;
            if (bossRow >= 0 && newRow >= bossRow)
            {
                newRow = bossRow - 1;
                DebugLog.Info($"[Displacement] ExecutePush {e.DebugTag}: row {oldRow}→{newRow} (clamped by boss wall at row {bossRow})");
            }
            else
            {
                DebugLog.Info($"[Displacement] ExecutePush {e.DebugTag}: row {oldRow}→{newRow}");
            }
            e.SetRowIndex(newRow);
            pushedEnemies.Add(e);
        }

        // 按 rowIndex 升序重新插入
        pushedEnemies.Sort((a, b) => a.rowIndex.CompareTo(b.rowIndex));
        foreach (var e in pushedEnemies)
            col.InsertEnemySorted(e);

        DebugLog.Info($"[ColumnManager] ExecutePush: col={columnIndex}, pushed={pushedEnemies.Count} enemies by {pushAmount} rows");

        // 地刺检测：Push 后检查被推敌人是否踩中
        foreach (var e in pushedEnemies)
            SpikeTrapController.Instance?.CheckAndTrigger(e);

        // 被击退后重新检查攻击范围：若超出范围则取消攻击并冲回前线
        foreach (var e in pushedEnemies)
        {
            e.RecheckAttackRange();
        }

        OnColumnsModified?.Invoke();
    }

    /// <summary>
    /// 击退波完整逻辑：逐列检查栈式阻塞 → 执行击退。
    /// BOSS 免疫击退。
    /// 返回 true 表示至少有一列成功执行了击退。
    /// </summary>
    public bool ApplyPushWave(List<Enemy> hitEnemies, int pushAmount, bool canInterruptCFrame = false)
    {
        if (hitEnemies == null || hitEnemies.Count == 0) return false;

        DebugLog.Info($"[Displacement] ApplyPushWave: pushAmount={pushAmount}, hitEnemies count={hitEnemies.Count}");
        foreach (var e in hitEnemies)
            DebugLog.Info($"  hitEnemy: {e.DebugTag} col={e.columnIndex} row={e.rowIndex} state={e.state} isBoss={e.isBoss}");

        _pushHitSet.Clear();
        _pushHitSet.UnionWith(hitEnemies);
        var hitSet = _pushHitSet;

        _pushByColumn.Clear();
        var byColumn = _pushByColumn;

        foreach (var e in hitEnemies)
        {
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
            if (canPush)
            {
                ExecutePush(col, pushAmount, kv.Value);
                anyPushed = true;
            }
        }
        return anyPushed;
    }

    /// <summary>
    /// 方向推（Slash 专属）：将击中敌人按行分组，朝 slash 方向推移 step 列。
    /// 同行多敌人自动分散到不同列，不重叠。
    /// </summary>
    public bool ApplyDirectionalPush(List<Enemy> hitEnemies, int step, bool pushRight, bool canInterruptCFrame = false)
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
                    if (MoveEnemyToColumnAtRow(enemy, tryCol, row))
                    {
                        anyMoved = true;
                        break;
                    }
                    tryCol += dir;
                }
            }
        }

        if (anyMoved) OnColumnsModified?.Invoke();
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

        OnColumnsModified?.Invoke();
    }

    #endregion

    /// <summary>
    /// 位移效果完成后触发补齐：中止波次行军 → 逐列紧凑 → 启动链式补齐 → 链结束后自动 StartWaveMarch。
    /// pushedToRow: 位移目标排（被推入的排），该排敌人不参与紧凑，避免击退被补齐抵消。
    /// </summary>
    public void PostDisplacementFillUp(int? pushedToRow = null)
    {
        DebugLog.Info($"[Displacement] PostDisplacementFillUp pushedToRow={pushedToRow?.ToString() ?? "null"}");

        AbortWaveMarch();
        CancelCompactionChains();

        RowBasedFillUp(pushedToRow);

        _isCompactionPending = true;
        Invoke(nameof(StartAllCompactionChains), compactionStartDelay);
    }

    /// <summary>
    /// 逐列紧凑所有列：每列存活敌人向前紧凑，Boss 作为墙壁不可逾越。
    /// rangeStart/rangeEnd 限定紧凑范围，默认 -1 表示全列。
    /// </summary>
    public void CompactAllColumns(int rangeStart = -1, int rangeEnd = -1)
    {
        for (int c = 0; c < columnCount; c++)
        {
            int bossRow = GetBossRowInColumn(c);
            columns[c].CompactColumn(bossRow, rangeStart, rangeEnd);
        }
        OnColumnsModified?.Invoke();
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
