using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次行军（普通补齐）的**状态、查询与流程**。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 4a 状态+查询 / 刀 4b 流程）。
/// 职责边界：
///   • 本类持有补齐调度的**全部状态**：本次订单的 generation、计划/生效的目标行与目标槽、
///     准备中/待行军的敌人集合、暂停快照；
///   • 只读查询（某排是否已空、目标槽是否被占、订单能否开始、能否进入某排）也在此；
///   • **流程**：何时发起补齐、如何派单、行军步进、击退回位期间如何暂停与恢复；
///   • 依赖宿主（<c>ColumnManager</c>）的部分通过**构造委托**注入：逻辑排队列、入场是否占用、
///     击退回位事务、骑兵槽位查询、取消已排程的补齐。
///
/// 规则依据：design/percolumn-fillup-rules.md（普通补齐只由整排清空触发）与
/// design/immutable-constraints.md 第 6 条（订单完成必须校验 owner + generation）。
/// </summary>
public sealed class WaveMarchScheduler
{
    private readonly Func<Column[]> _columnsProvider;
    private readonly Func<int, int, Enemy, bool> _isCavalrySlotReserved;
    private readonly LogicalRowQueue _queue;
    private readonly Func<bool> _isSpawnEntryBusy;
    private readonly Func<bool> _hasPushReturnTransactions;
    private readonly Func<Enemy, bool> _hasPushReturnTransaction;
    private readonly Func<int, int, Enemy> _getCavalryReservationOwner;
    private readonly Action _cancelScheduledWaveMarch;

    // ── 订单与目标 ──
    /// <summary>本次行军的 generation（订单校验用，敌人侧按它拒绝过期订单）。</summary>
    public int waveGeneration;

    /// <summary>计划中的目标行（敌人 → 目标排）。</summary>
    public readonly Dictionary<Enemy, int> plannedWaveTargetRows = new Dictionary<Enemy, int>();

    /// <summary>已生效的目标行（敌人 → 目标排）。</summary>
    public readonly Dictionary<Enemy, int> activeWaveTargetRows = new Dictionary<Enemy, int>();

    /// <summary>计划中已占用的目标槽。</summary>
    public readonly HashSet<long> plannedWaveTargetSlots = new HashSet<long>();

    /// <summary>已生效的目标槽（列 << 32 | 排）。</summary>
    public readonly HashSet<long> activeWaveTargetSlots = new HashSet<long>();

    // ── 批次成员 ──
    /// <summary>已派单、正在准备起步的敌人。</summary>
    public readonly List<Enemy> preparingWaveEnemies = new List<Enemy>();

    /// <summary>已放行、等待起步的敌人。</summary>
    public readonly HashSet<Enemy> pendingWaveEnemies = new HashSet<Enemy>();

    // ── 暂停快照（击退回位期间保留） ──
    /// <summary>是否有被暂停的行军步骤。</summary>
    public bool hasPausedWaveStep;

    /// <summary>暂停的源排。</summary>
    public int pausedWaveSourceRow = -1;

    /// <summary>暂停的目标排。</summary>
    public int pausedWaveTargetRow = -1;

    /// <summary>暂停步骤涉及的敌人。</summary>
    public readonly List<Enemy> pausedWaveEnemies = new List<Enemy>();

    /// <summary>暂停步骤里每个敌人的目标行。</summary>
    public readonly Dictionary<Enemy, int> pausedWaveTargetRows = new Dictionary<Enemy, int>();

    // ── 当前步骤描述 ──
    /// <summary>当前步骤的源排。</summary>
    public int currentWaveSourceRow = -1;

    /// <summary>当前步骤的目标排。</summary>
    public int currentWaveTargetRow = -1;

    /// <summary>当前步骤是否为连续跑（ContinuousEmptyRun）。</summary>
    public bool currentWaveContinuousRun;

    /// <summary>是否正在行军中。</summary>
    public bool isWaveMarching;

    /// <summary>是否正在准备行军（已派单未起步）。</summary>
    public bool isWavePreparing;

    /// <summary>击退回位期间挂起的补齐请求（回位全部结束后恢复）。</summary>
    private bool _waveMarchRequestedWhilePushReturn;

    public WaveMarchScheduler(
        Func<Column[]> columnsProvider,
        Func<int, int, Enemy, bool> isCavalrySlotReserved,
        LogicalRowQueue queue,
        Func<bool> isSpawnEntryBusy,
        Func<bool> hasPushReturnTransactions,
        Func<Enemy, bool> hasPushReturnTransaction,
        Func<int, int, Enemy> getCavalryReservationOwner,
        Action cancelScheduledWaveMarch)
    {
        _columnsProvider = columnsProvider;
        _isCavalrySlotReserved = isCavalrySlotReserved;
        _queue = queue;
        _isSpawnEntryBusy = isSpawnEntryBusy;
        _hasPushReturnTransactions = hasPushReturnTransactions;
        _hasPushReturnTransaction = hasPushReturnTransaction;
        _getCavalryReservationOwner = getCavalryReservationOwner;
        _cancelScheduledWaveMarch = cancelScheduledWaveMarch;
    }

    private Column[] Columns => _columnsProvider != null ? _columnsProvider() : null;

    private int ColumnCount => Columns != null ? Columns.Length : 0;

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < ColumnCount;
    }

    /// <summary>该排是否已经没有活人（普通补齐的唯一触发判据）。</summary>
    public bool IsRowFullyVacated(int row)
    {
        var cols = Columns;
        if (cols == null) return true;
        for (int c = 0; c < cols.Length; c++)
        {
            foreach (var e in cols[c].enemies)
            {
                if (e == null) continue;
                if (e.state == EnemyState.Dead) continue;
                if (e.rowIndex == row) return false;
            }
        }
        return true;
    }

    /// <summary>当前最靠后的排号。</summary>
    public int GetMaxOccupiedRow()
    {
        var cols = Columns;
        if (cols == null) return 0;
        int max = 0;
        for (int c = 0; c < cols.Length; c++)
        {
            foreach (var e in cols[c].enemies)
            {
                if (e == null || e.state == EnemyState.Dead) continue;
                if (e.rowIndex > max) max = e.rowIndex;
            }
        }
        return max;
    }

    /// <summary>该槽位是否已被本次行军预定（或已被别人占用）。</summary>
    public bool IsTargetReserved(Enemy enemy, int column, int row)
    {
        if (enemy != null && activeWaveTargetRows.TryGetValue(enemy, out int ownTargetRow) && ownTargetRow == row)
            return false;
        return activeWaveTargetSlots.Contains(((long)column << 32) ^ (uint)row);
    }

    /// <summary>该敌人能否开始执行本次行军订单（generation 校验 + 待行军集合）。</summary>
    public bool CanStartOrder(Enemy enemy, int generation)
    {
        return !isWavePreparing
            && generation == waveGeneration
            && pendingWaveEnemies.Contains(enemy);
    }

    /// <summary>连续行军时该敌人能否进入目标排（目标排被占 / 骑兵槽位被占则不行）。</summary>
    public bool CanEnterRow(Enemy enemy, int column, int row)
    {
        var cols = Columns;
        if (cols == null || column < 0 || column >= cols.Length)
            return false;
        if (_isCavalrySlotReserved != null && _isCavalrySlotReserved(column, row, enemy))
            return false;

        var occupant = cols[column].GetEnemyAtRow(row);
        if (occupant == null || occupant == enemy || occupant.state == EnemyState.Dead)
            return true;

        return occupant.IsRushMoveOrder(RushMoveOrderOwner.WaveMarch, waveGeneration)
            && activeWaveTargetRows.TryGetValue(occupant, out int occupantTargetRow)
            && occupantTargetRow < row;
    }

    // ─────────────────────────── 流程（刀 4b 搬入） ───────────────────────────

    /// <summary>
    /// 某活人排被清空：把它从逻辑排队列摘掉，并请求一次布局重排。
    /// </summary>
    public void RequestLogicalLayoutReflow(LogicalWaveRow clearedRow)
    {
        if (clearedRow == null || clearedRow.removed || clearedRow.kind != LogicalWaveRowKind.Live)
            return;

        _queue.MarkRowRemoved(clearedRow);
        StartPendingLogicalLayoutReflow();
    }

    /// <summary>该逻辑排里是否还有活着的敌人。</summary>
    public bool HasLivingMembers(LogicalWaveRow logicalRow)
    {
        return _queue.HasLivingMembers(logicalRow);
    }

    /// <summary>
    /// 若队列有待重排的布局变化，且当前没有入场 / 击退回位 / 行军占用，则发起一次补齐。
    /// </summary>
    public void StartPendingLogicalLayoutReflow()
    {
        if (!_queue.UseRowMarchPlanner || !_queue.IsDirty)
            return;
        if (_isSpawnEntryBusy != null && _isSpawnEntryBusy())
            return;
        if (_hasPushReturnTransactions != null && _hasPushReturnTransactions())
        {
            _waveMarchRequestedWhilePushReturn = true;
            return;
        }
        if (isWaveMarching || isWavePreparing)
            return;

        if (!TryBuildLogicalWaveMarchPlan())
        {
            _queue.ConsumeDirty();
            return;
        }

        _queue.ConsumeDirty();
        BeginWaveStep();
    }

    /// <summary>
    /// Cross-column wave march. Legacy non-layout stages may still request a topology scan.
    /// PerRow stages consume only logical layout changes caused by full row deaths.
    /// </summary>
    public void StartWaveMarch()
    {
        // 补齐只有一条路径：整排清空 → 逻辑排队列差分 → 波次行军。
        // 旧的「按列找空位前压」分支已随 FillUpRule 一起退役（plan/percolumn-retirement-20261020.md 刀 6）。
        StartPendingLogicalLayoutReflow();
    }

    private bool TryBuildLogicalWaveMarchPlan()
    {
        plannedWaveTargetRows.Clear();
        plannedWaveTargetSlots.Clear();

        _queue.ReassignRuntimeRows();

        foreach (var pair in _queue.EnemyRows)
        {
            var enemy = pair.Key;
            var logicalRow = pair.Value;
            if (enemy == null || enemy.isBoss || enemy.state == EnemyState.Dead || logicalRow == null || logicalRow.removed)
                continue;
            if (enemy.GetComponent<CavalryEnemy>()?.IsMounted == true)
                continue;
            if (_hasPushReturnTransaction != null && _hasPushReturnTransaction(enemy)) continue;
            if (enemy.rowIndex <= logicalRow.currentRow)
                continue;
            // 「豁免补齐」：该敌人不参与波次行军——不派单、不预留目标槽（原地下不动）。
            // 注意这是**设计意图的排除**（敌人自己声明不参与），不是按状态过滤；
            // 按状态过滤会让敌人永远拿不到本该有的订单（历史缺陷：105 远程敌人永远不补齐）。
            if (enemy.ignoreFillUpOrders)
            {
                DebugLog.Info("[FillUp] 豁免补齐（不参与波次行军）: " + enemy.DebugTag);
                continue;
            }

            long slotKey = ((long)enemy.columnIndex << 32) ^ (uint)logicalRow.currentRow;
            if (!plannedWaveTargetSlots.Add(slotKey))
                continue;
            plannedWaveTargetRows[enemy] = logicalRow.currentRow;
        }

        return plannedWaveTargetRows.Count > 0;
    }

    private void BeginWaveStep()
    {
        waveGeneration++;
        if (waveGeneration <= 0) waveGeneration = 1;
        currentWaveSourceRow = -1;
        currentWaveTargetRow = -1;
        currentWaveContinuousRun = true;
        preparingWaveEnemies.Clear();
        pendingWaveEnemies.Clear();
        activeWaveTargetRows.Clear();
        activeWaveTargetSlots.Clear();

        foreach (var pair in plannedWaveTargetRows)
        {
            var enemy = pair.Key;
            if (enemy == null || enemy.state == EnemyState.Dead)
                continue;
            var reservationOwner = _getCavalryReservationOwner != null
                ? _getCavalryReservationOwner(enemy.columnIndex, pair.Value)
                : null;
            if (reservationOwner != null && reservationOwner != enemy)
                reservationOwner.GetComponent<CavalryEnemy>()?.AbortForExternalOrder("WaveMarch目标优先");
            if (!enemy.AssignRushMoveOrder(RushMoveOrderOwner.WaveMarch, waveGeneration, pair.Value, RushMoveMode.ContinuousEmptyRun))
                continue;

            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            enemy.OnRushMoveComplete += OnWaveEnemyRushComplete;
            preparingWaveEnemies.Add(enemy);
            activeWaveTargetRows[enemy] = pair.Value;
            activeWaveTargetSlots.Add(((long)enemy.columnIndex << 32) ^ (uint)pair.Value);
        }

        plannedWaveTargetRows.Clear();
        plannedWaveTargetSlots.Clear();
        if (preparingWaveEnemies.Count == 0)
            return;

        isWavePreparing = true;
    }

    private void BeginWaveStep(int sourceRow, int targetRow, IList<Enemy> restrictedEnemies = null, bool continuousRun = false)
    {
        waveGeneration++;
        if (waveGeneration <= 0) waveGeneration = 1;
        currentWaveSourceRow = sourceRow;
        currentWaveTargetRow = Mathf.Max(0, targetRow);
        currentWaveContinuousRun = continuousRun;
        preparingWaveEnemies.Clear();
        pendingWaveEnemies.Clear();

        var cols = Columns;
        for (int c = 0; c < ColumnCount; c++)
        {
            foreach (var enemy in cols[c].enemies)
            {
                if (enemy == null || enemy.isBoss || enemy.state == EnemyState.Dead || enemy.rowIndex != sourceRow)
                    continue;
                if (enemy.GetComponent<CavalryEnemy>()?.IsMounted == true)
                    continue;
                if (restrictedEnemies != null && !restrictedEnemies.Contains(enemy))
                    continue;
                // 「豁免补齐」：不参与波次行军（与规划层一致，见 TryBuildLogicalWaveMarchPlan 注释）
                if (enemy.ignoreFillUpOrders)
                    continue;

                if (_hasPushReturnTransaction != null && _hasPushReturnTransaction(enemy))
                    continue;

                var reservationOwner = _getCavalryReservationOwner != null
                    ? _getCavalryReservationOwner(enemy.columnIndex, currentWaveTargetRow)
                    : null;
                if (reservationOwner != null && reservationOwner != enemy)
                    reservationOwner.GetComponent<CavalryEnemy>()?.AbortForExternalOrder("WaveMarch目标优先");
                if (!enemy.AssignRushMoveOrder(RushMoveOrderOwner.WaveMarch, waveGeneration, currentWaveTargetRow,
                    continuousRun ? RushMoveMode.ContinuousEmptyRun : RushMoveMode.Step))
                    continue;

                enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
                enemy.OnRushMoveComplete += OnWaveEnemyRushComplete;
                preparingWaveEnemies.Add(enemy);
            }
        }

        if (preparingWaveEnemies.Count == 0)
        {
            Debug.Log($"[WaveMarch] BeginWaveStep srcRow={sourceRow} tgtRow={targetRow}: NO enemies eligible (all boss/dead/rush-order-rejected)");
            currentWaveSourceRow = -1;
            currentWaveTargetRow = -1;
            return;
        }

        isWavePreparing = true;
    }

    private void StartPreparedWaveStep()
    {
        int generation = waveGeneration;
        isWavePreparing = false;
        isWaveMarching = true;
        pendingWaveEnemies.Clear();

        for (int i = 0; i < preparingWaveEnemies.Count; i++)
        {
            var enemy = preparingWaveEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead)
                continue;
            if (!enemy.IsRushMoveOrder(RushMoveOrderOwner.WaveMarch, generation))
                continue;
            pendingWaveEnemies.Add(enemy);
        }
        preparingWaveEnemies.Clear();

        foreach (var enemy in pendingWaveEnemies)
            enemy.TryStartRushMove();

        if (pendingWaveEnemies.Count == 0)
            CompleteWaveStep(generation);
    }

    private void OnWaveEnemyRushComplete(Enemy enemy, RushMoveOrderOwner owner, int generation)
    {
        if (owner != RushMoveOrderOwner.WaveMarch || generation != waveGeneration)
            return;

        enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
        pendingWaveEnemies.Remove(enemy);
        if (pendingWaveEnemies.Count == 0)
            CompleteWaveStep(generation);
    }

    private void CompleteWaveStep(int generation)
    {
        if (generation != waveGeneration) return;

        isWaveMarching = false;
        isWavePreparing = false;
        currentWaveContinuousRun = false;
        currentWaveSourceRow = -1;
        currentWaveTargetRow = -1;
        pendingWaveEnemies.Clear();
        preparingWaveEnemies.Clear();
        activeWaveTargetRows.Clear();
        activeWaveTargetSlots.Clear();
        StartPendingLogicalLayoutReflow();
        if (!_queue.UseRowMarchPlanner)
            StartWaveMarch();
    }

    /// <summary>
    /// 每帧推进「已派单、未起步」的补齐批次。
    /// </summary>
    public void TickPreparing()
    {
        if (!isWavePreparing) return;

        for (int i = preparingWaveEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = preparingWaveEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || !enemy.IsRushMoveOrder(RushMoveOrderOwner.WaveMarch, waveGeneration))
            {
                if (enemy != null)
                    enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
                preparingWaveEnemies.RemoveAt(i);
                continue;
            }
            if (!enemy.IsRushMoveReady)
                return;
        }

        if (preparingWaveEnemies.Count == 0)
        {
            isWavePreparing = false;
            currentWaveSourceRow = -1;
            currentWaveTargetRow = -1;
            StartPendingLogicalLayoutReflow();
            return;
        }

        StartPreparedWaveStep();
    }

    /// <summary>
    /// Captures the exact current wave order before the first push transaction so temporary holes
    /// cannot be reinterpreted. After all returns finish, only the captured members/target are revalidated.
    /// </summary>
    private void CapturePausedWaveStep()
    {
        if (!isWaveMarching && !isWavePreparing)
            return;

        hasPausedWaveStep = true;
        pausedWaveSourceRow = currentWaveSourceRow;
        pausedWaveTargetRow = currentWaveTargetRow;
        pausedWaveEnemies.Clear();
        pausedWaveTargetRows.Clear();

        for (int i = 0; i < preparingWaveEnemies.Count; i++)
        {
            var enemy = preparingWaveEnemies[i];
            if (enemy != null && !pausedWaveEnemies.Contains(enemy))
                pausedWaveEnemies.Add(enemy);
        }
        foreach (var enemy in pendingWaveEnemies)
        {
            if (enemy != null && !pausedWaveEnemies.Contains(enemy))
                pausedWaveEnemies.Add(enemy);
        }

        for (int i = 0; i < pausedWaveEnemies.Count; i++)
        {
            var enemy = pausedWaveEnemies[i];
            if (activeWaveTargetRows.TryGetValue(enemy, out int targetRow))
                pausedWaveTargetRows[enemy] = targetRow;
        }
    }

    /// <summary>中止当前补齐批次（重开关卡 / 移走敌人 / 击退回位前）。</summary>
    public void AbortWaveMarch(bool preserveForPushReturn = false)
    {
        if (preserveForPushReturn)
            CapturePausedWaveStep();

        if (_cancelScheduledWaveMarch != null)
            _cancelScheduledWaveMarch();
        waveGeneration++;

        for (int i = 0; i < preparingWaveEnemies.Count; i++)
        {
            var enemy = preparingWaveEnemies[i];
            if (enemy == null) continue;
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            enemy.CancelRushMoveOrder(resetActiveMovement: true);
        }
        foreach (var enemy in pendingWaveEnemies)
        {
            if (enemy == null) continue;
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            enemy.CancelRushMoveOrder(resetActiveMovement: true);
        }

        preparingWaveEnemies.Clear();
        pendingWaveEnemies.Clear();
        plannedWaveTargetRows.Clear();
        plannedWaveTargetSlots.Clear();
        activeWaveTargetRows.Clear();
        activeWaveTargetSlots.Clear();
        if (_queue.UseRowMarchPlanner)
            _queue.MarkDirty();
        isWavePreparing = false;
        isWaveMarching = false;
        currentWaveSourceRow = -1;
        currentWaveTargetRow = -1;
    }

    /// <summary>
    /// 敌人被移出列 / 死亡时，从补齐批次里摘除它。
    /// 若批次因此清空，必须补发一次全局补齐扫描（否则补齐链会永久暂停）。
    /// </summary>
    public void ReleaseWaveMember(Enemy enemy, int sourceColumn)
    {
        if (enemy == null) return;

        bool wasWaveMember = pendingWaveEnemies.Remove(enemy);
        if (activeWaveTargetRows.Remove(enemy, out int activeTargetRow))
            activeWaveTargetSlots.Remove(((long)sourceColumn << 32) ^ (uint)activeTargetRow);
        if (preparingWaveEnemies.Remove(enemy))
            wasWaveMember = true;
        if (wasWaveMember)
            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;

        if (IsValidColumn(sourceColumn))
            Columns[sourceColumn].ReleaseRushMoveOwnership(enemy);

        enemy.CancelRushMoveOrder(resetActiveMovement: true);

        if (wasWaveMember && pendingWaveEnemies.Count == 0 && preparingWaveEnemies.Count == 0)
        {
            isWavePreparing = false;
            isWaveMarching = false;
            currentWaveSourceRow = -1;
            currentWaveTargetRow = -1;
            activeWaveTargetRows.Clear();
            activeWaveTargetSlots.Clear();
            if (_queue.UseRowMarchPlanner)
            {
                _queue.MarkDirty();
                StartWaveMarch();
                return;
            }
            // 修复（WaveMarch死锁）：WaveMarch 步进成员被位移（横推/击退/移除）打断且集合清空后，
            // 必须补发一次全局补齐扫描。此前只复位 flag 不重扫，导致补齐链永久暂停：
            // 横推清空集合后无人重扫；随后 Stab 击退触发 RegisterPushReturn 时因 flag 已 false
            // 而 AbortWaveMarch 不保存 paused，PushReturn 完成后无从恢复 → 永久死锁。
            // StartWaveMarch 自带 PushReturn 感知：有挂起事务时 deferred 并置
            // _waveMarchRequestedWhilePushReturn=true，由 TryResumePausedWaveAfterPushReturns 恢复。
            StartWaveMarch();
        }
    }

    private void ResumePausedWavePlan()
    {
        waveGeneration++;
        if (waveGeneration <= 0) waveGeneration = 1;
        currentWaveSourceRow = -1;
        currentWaveTargetRow = -1;
        currentWaveContinuousRun = true;
        preparingWaveEnemies.Clear();
        pendingWaveEnemies.Clear();
        activeWaveTargetRows.Clear();
        activeWaveTargetSlots.Clear();

        foreach (var pair in pausedWaveTargetRows)
        {
            var enemy = pair.Key;
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.rowIndex <= pair.Value)
                continue;
            if (!enemy.AssignRushMoveOrder(RushMoveOrderOwner.WaveMarch, waveGeneration, pair.Value, RushMoveMode.ContinuousEmptyRun))
                continue;

            enemy.OnRushMoveComplete -= OnWaveEnemyRushComplete;
            enemy.OnRushMoveComplete += OnWaveEnemyRushComplete;
            preparingWaveEnemies.Add(enemy);
            activeWaveTargetRows[enemy] = pair.Value;
            activeWaveTargetSlots.Add(((long)enemy.columnIndex << 32) ^ (uint)pair.Value);
        }

        pausedWaveTargetRows.Clear();
        pausedWaveEnemies.Clear();
        if (preparingWaveEnemies.Count > 0)
            isWavePreparing = true;
    }

    /// <summary>击退回位全部结束后，恢复被暂停的补齐批次。</summary>
    public void TryResumePausedWaveAfterPushReturns()
    {
        if (_hasPushReturnTransactions != null && _hasPushReturnTransactions())
            return;

        if (hasPausedWaveStep)
        {
            int sourceRow = pausedWaveSourceRow;
            int targetRow = pausedWaveTargetRow;
            hasPausedWaveStep = false;
            pausedWaveSourceRow = -1;
            pausedWaveTargetRow = -1;

            if (pausedWaveTargetRows.Count > 0)
                ResumePausedWavePlan();
            else
            {
                BeginWaveStep(sourceRow, targetRow, pausedWaveEnemies);
                pausedWaveEnemies.Clear();
                pausedWaveTargetRows.Clear();
            }

            if (isWavePreparing)
                return;

            StartPendingLogicalLayoutReflow();
        }

        if (_waveMarchRequestedWhilePushReturn)
        {
            _waveMarchRequestedWhilePushReturn = false;
            if (_queue.UseRowMarchPlanner)
                StartPendingLogicalLayoutReflow();
            else
                StartWaveMarch();
        }
    }

    /// <summary>清空暂停快照与挂起请求（重开关卡 / 取消全部击退回位时）。</summary>
    public void ResetPausedState()
    {
        _waveMarchRequestedWhilePushReturn = false;
        hasPausedWaveStep = false;
        pausedWaveSourceRow = -1;
        pausedWaveTargetRow = -1;
        pausedWaveEnemies.Clear();
        pausedWaveTargetRows.Clear();
    }
}
