using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 破冰顶开的结果（刀 5 从 <c>ColumnManager.BreakIcePushResult</c> 提为顶层类型）。
/// </summary>
public struct BreakIcePushResult
{
    public bool moved;
    public Enemy blockedBy;
    public int fromRow;
    public int toRow;
}

/// <summary>
/// 位移类：**精确击退回位** + **位移效果**（破冰顶开 / 击退 / 横向位移 / 聚拢）。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 5）。职责边界：
///   • 本类持有**位移相关的全部状态**：回位事务表、回位 generation/orderId、回位抑制标志，
///     以及击退 / 横推 / 聚拢用的复用容器（避免每帧 new 分配导致 GC）；
///   • 规则（design/immutable-constraints.md）：
///     击退**只移动被击退者**、必须回**精确原槽**；横向位移**只改 column**、不发布行拓扑变化；
///     聚拢用中心槽位裁决；BOSS 不参与击退，且其所在排是「墙」；
///   • 依赖宿主（<c>ColumnManager</c>）的部分通过**构造委托**注入：列数组、骑兵槽位、
///     可见排数、释放敌人/槽位、中止与恢复补齐。
///
/// 事件订阅（<c>OnRushMoveComplete</c>）与退订都在本类内部成对完成，
/// 避免宿主侧退订不掉（委托目标变了）。
/// </summary>
public sealed class DisplacementMover
{
    // ── 构造注入的宿主能力 ──
    private readonly Func<Column[]> _columnsProvider;
    private readonly Func<int, int, Enemy, bool> _isCavalrySlotReserved;
    private readonly Func<int> _getMaxVisibleRows;
    private readonly Action<Enemy> _releaseCavalrySlot;
    private readonly Action<Enemy, int> _releaseEnemyFromSchedulers;
    private readonly Action<Enemy, int> _releaseEnemyFromMovementSchedulers;
    private readonly Action<bool> _abortWaveMarch;
    private readonly Action _resumePausedWaveAfterPushReturns;
    private readonly Action _resetPausedWaveState;

    /// <summary>一次「被击退 → 回精确原槽」的事务。</summary>
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

    // 位移复用容器（避免每帧 new 分配导致 GC）
    private readonly List<Enemy> _pushWorkList = new List<Enemy>();
    private readonly HashSet<Enemy> _pushHitSet = new HashSet<Enemy>();
    private readonly Dictionary<int, List<Enemy>> _pushByColumn = new Dictionary<int, List<Enemy>>();
    private readonly Dictionary<int, List<Enemy>> _rowEnemies = new Dictionary<int, List<Enemy>>();

    public DisplacementMover(
        Func<Column[]> columnsProvider,
        Func<int, int, Enemy, bool> isCavalrySlotReserved,
        Func<int> getMaxVisibleRows,
        Action<Enemy> releaseCavalrySlot,
        Action<Enemy, int> releaseEnemyFromSchedulers,
        Action<Enemy, int> releaseEnemyFromMovementSchedulers,
        Action<bool> abortWaveMarch,
        Action resumePausedWaveAfterPushReturns,
        Action resetPausedWaveState)
    {
        _columnsProvider = columnsProvider;
        _isCavalrySlotReserved = isCavalrySlotReserved;
        _getMaxVisibleRows = getMaxVisibleRows;
        _releaseCavalrySlot = releaseCavalrySlot;
        _releaseEnemyFromSchedulers = releaseEnemyFromSchedulers;
        _releaseEnemyFromMovementSchedulers = releaseEnemyFromMovementSchedulers;
        _abortWaveMarch = abortWaveMarch;
        _resumePausedWaveAfterPushReturns = resumePausedWaveAfterPushReturns;
        _resetPausedWaveState = resetPausedWaveState;
    }

    private Column[] Columns => _columnsProvider != null ? _columnsProvider() : null;

    private int ColumnCount => Columns != null ? Columns.Length : 0;

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < ColumnCount;
    }

    // ───────────────────────── 精确击退回位 ─────────────────────────

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

    public void RegisterPushReturn(Enemy enemy, int originalColumn, int originalRow, int displacedRow, float delaySeconds = -1f, bool abortWaveMarch = true)
    {
        float delay = delaySeconds >= 0f ? delaySeconds : pushReturnDelay;
        if (_pushReturnTransactions.TryGetValue(enemy, out var transaction))
        {
            enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
            enemy.CancelRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation, resetActiveMovement: true);

            transaction.displacedColumn = enemy.columnIndex;
            transaction.displacedRow = displacedRow;
            transaction.generation = NextPushReturnGeneration();
            transaction.orderId = NextPushReturnOrderId();
            transaction.returnDueTime = Time.time + delay;
            transaction.scheduled = true;
            transaction.lastBlockedRow = int.MinValue;
            DebugLog.Info($"[PushReturn] reschedule {enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) target=({originalColumn},{displacedRow}) gen={transaction.generation} order={transaction.orderId}");
            return;
        }

        if (abortWaveMarch && _pushReturnTransactions.Count == 0)
        {
            if (_abortWaveMarch != null)
                _abortWaveMarch(true);
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
            returnDueTime = Time.time + delay,
            scheduled = true
        };
        _pushReturnTransactions.Add(enemy, transaction);
        DebugLog.Info($"[PushReturn] register {enemy.DebugTag} origin=({originalColumn},{originalRow}) target=({originalColumn},{displacedRow}) gen={generation} order={orderId}");
    }

    public void UpdatePushReturns()
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
        var column = Columns[transaction.originalColumn];
        if (column.IsRowOccupied(nextRow, enemy)
            || (_isCavalrySlotReserved != null && _isCavalrySlotReserved(transaction.originalColumn, nextRow, enemy)))
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
        if (_resumePausedWaveAfterPushReturns != null)
            _resumePausedWaveAfterPushReturns();
    }

    private void LogPushReturnBlocked(PushReturnTransaction transaction, int blockedRow)
    {
        if (transaction.lastBlockedRow == blockedRow)
            return;

        transaction.lastBlockedRow = blockedRow;
        DebugLog.Info($"[PushReturn] blocked {transaction.enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) current=({transaction.enemy.columnIndex},{transaction.enemy.rowIndex}) blockedRow={blockedRow} gen={transaction.generation} order={transaction.orderId}");
    }

    public void CancelPushReturn(Enemy enemy, string reason)
    {
        if (enemy == null || !_pushReturnTransactions.TryGetValue(enemy, out var transaction))
            return;

        string logReason = enemy.state == EnemyState.Dead ? "cancel-dead" : reason;
        enemy.OnRushMoveComplete -= OnPushReturnStepComplete;
        enemy.CancelRushMoveOrder(RushMoveOrderOwner.PushReturn, transaction.generation, resetActiveMovement: true);
        _pushReturnTransactions.Remove(enemy);
        DebugLog.Info($"[PushReturn] {logReason} {enemy.DebugTag} origin=({transaction.originalColumn},{transaction.originalRow}) gen={transaction.generation} order={transaction.orderId}");
        if (!_suppressPushReturnResume && _resumePausedWaveAfterPushReturns != null)
            _resumePausedWaveAfterPushReturns();
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

    /// <summary>当前是否有挂起的回位事务（补齐调度据此暂缓）。</summary>
    public bool HasPushReturnTransactions => _pushReturnTransactions.Count > 0;

    /// <summary>该敌人是否处于回位事务中。</summary>
    public bool HasPushReturnTransaction(Enemy enemy)
    {
        return enemy != null && _pushReturnTransactions.ContainsKey(enemy);
    }

    public void CancelAllPushReturns()
    {
        if (_resetPausedWaveState != null)
            _resetPausedWaveState();
        _suppressPushReturnResume = true;
        _pushReturnWorkList.Clear();
        foreach (var transaction in _pushReturnTransactions.Values)
            _pushReturnWorkList.Add(transaction);
        for (int i = 0; i < _pushReturnWorkList.Count; i++)
            CancelPushReturn(_pushReturnWorkList[i].enemy, "cancel-reset");
        _pushReturnWorkList.Clear();
        _suppressPushReturnResume = false;
    }

    // ───────────────────────── 位移效果 ─────────────────────────

    /// <summary>
    /// 破冰推动（方案 A）：逐排向后推进到最远合法排，被占则停在阻挡者前一排。
    /// 不在这里登记回位，回位由调用方（Enemy）在破冰动画结束时用 RegisterBreakIceReturn 登记。
    /// </summary>
    public BreakIcePushResult ApplyBreakIcePush(Enemy frozen, int maxRows)
    {
        var result = new BreakIcePushResult { moved = false, blockedBy = null, fromRow = -1, toRow = -1 };
        if (frozen == null || frozen.state == EnemyState.Dead) return result;
        if (frozen.isBoss || frozen.IsMountedCavalry || frozen.freezePushImmune) return result;

        int col = frozen.columnIndex;
        if (!IsValidColumn(col) || maxRows <= 0) return result;

        int visibleMaxRow = (_getMaxVisibleRows != null ? _getMaxVisibleRows() : 5) - 1;
        int allowed = Mathf.Min(maxRows, visibleMaxRow - frozen.rowIndex);
        if (allowed <= 0) return result;

        var column = Columns[col];
        int fromRow = frozen.rowIndex;
        int targetRow = fromRow;

        for (int step = 1; step <= allowed; step++)
        {
            int probe = fromRow + step;
            var occupant = column.GetEnemyAtRow(probe);
            if (occupant != null && occupant != frozen)
            {
                // 本次破冰的其他冻结目标不算阻挡者：只停在其前方，不结算碰撞伤害（设计 §5.7）
                if (IceBreakRuntime.IsBreakSetMember(occupant))
                {
                    result.blockedBy = null;
                    break;
                }
                result.blockedBy = occupant;
                break;
            }
            targetRow = probe;
        }

        result.fromRow = fromRow;
        result.toRow = targetRow;
        if (targetRow == fromRow) return result;
        if (column.IsRowOccupied(targetRow, frozen)) return result;

        Vector3 fromWorldPosition = frozen.transform.position;
        // 只停掉它自己的在途移动；不走 ReleaseEnemyFromSchedulers，
        // 否则会打断波次行军链、取消回位事务（会造成“被推后不再补齐回来”）。
        if (frozen.IsRushMovementActive)
            frozen.CancelRushMoveOrder(resetActiveMovement: true);
        column.RemoveEnemySilent(frozen);
        frozen.SetRowIndex(targetRow);
        column.InsertEnemySorted(frozen);
        result.moved = true;
        frozen.PlayBreakIcePushMove(fromWorldPosition);

        frozen.RecheckAttackRange();
        SpikeTrapController.Instance?.CheckAndTrigger(frozen);
        DebugLog.Info($"[BreakIcePush] {frozen.DebugTag} col={col} row {fromRow}→{targetRow} blockedBy={(result.blockedBy != null ? result.blockedBy.DebugTag : "none")}");
        return result;
    }

    /// <summary>破冰后的延迟回位（走现有回位交易，原槽被占时按既有重试逻辑等待）。
    /// 注意：不中止波次行军（abortWaveMarch=false）。
    /// 否则每次破冰都会把在途的补齐步骤挂起并重播，造成“补齐规则被破坏”的观感。
    /// 目标本身因带回位交易会被行军计划自动跳过。</summary>
    public void RegisterBreakIceReturn(Enemy enemy, int originalColumn, int originalRow, int displacedRow, float delaySeconds)
    {
        if (enemy == null || enemy.state == EnemyState.Dead) return;
        RegisterPushReturn(enemy, originalColumn, originalRow, displacedRow, delaySeconds, abortWaveMarch: false);
        NotifyPushReturnTopologyChanged();
        DebugLog.Info($"[BreakIcePush] 登记延迟回位 {enemy.DebugTag} origin=({originalColumn},{originalRow}) displacedRow={displacedRow} delay={delaySeconds:F2}s");
    }

    /// <summary>
    /// 将敌人移动到目标列的指定排（保留 row 位置），更新世界坐标。
    /// 若目标位置已被占据则返回 false（调用方需处理冲突）。
    /// 不触发补齐链。
    /// </summary>
    public bool MoveEnemyToColumnAtRow(Enemy enemy, int targetCol, int targetRow)
    {
        if (!IsValidColumn(targetCol)) return false;
        // 骑乘骑兵免疫位移（横推/聚拢等）。
        if (enemy.IsMountedCavalry) return false;
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

        if (Columns[targetCol].IsRowOccupied(targetRow, enemy)
            || (_isCavalrySlotReserved != null && _isCavalrySlotReserved(targetCol, targetRow, enemy)))
        {
            DebugLog.Info($"[ColumnManager] MoveEnemyToColumnAtRow blocked: {enemy.DebugTag} → col={targetCol} row={targetRow} occupied");
            return false;
        }

        if (_releaseCavalrySlot != null)
            _releaseCavalrySlot(enemy);
        if (_releaseEnemyFromSchedulers != null)
            _releaseEnemyFromSchedulers(enemy, srcCol);
        Columns[srcCol].RemoveEnemySilent(enemy);
        enemy.columnIndex = targetCol;
        enemy.SetRowIndex(targetRow);
        Columns[targetCol].InsertEnemySorted(enemy);

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

        if (_releaseEnemyFromSchedulers != null)
            _releaseEnemyFromSchedulers(enemy, srcCol);
        Columns[srcCol].RemoveEnemySilent(enemy);
        int newRow = Columns[targetCol].enemies.Count;
        enemy.columnIndex = targetCol;
        enemy.SetRowIndex(newRow);
        Columns[targetCol].AddEnemy(enemy);

        DebugLog.Info($"[ColumnManager] MoveEnemyToColumnEnd: {enemy.DebugTag} col {srcCol}→{targetCol}, newRow={newRow}");
    }

    /// <summary>
    /// 获取指定列中BOSS的rowIndex，无BOSS返回 -1。
    /// </summary>
    public int GetBossRowInColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return -1;
        foreach (var e in Columns[columnIndex].enemies)
        {
            if (e.isBoss && e.state != EnemyState.Dead)
                return e.rowIndex;
        }
        return -1;
    }

    /// <summary>
    /// 击退栈式阻塞检测：检查某列被击中的敌人能否向后推移。
    /// 规则1：若 [maxHitRow+1, maxHitRow+pushAmount] 区间内存在非 hit 敌人 → 整列阻塞。
    /// 规则2：每个命中敌人的目标排 (rowIndex + pushAmount) 不能被非 hit 敌人占据 → 否则重叠。
    /// BOSS 不参与判断（免疫击退，不阻塞击退）。
    /// </summary>
    /// <returns>true = 可以击退</returns>
    public bool CanPushColumn(int columnIndex, int pushAmount, HashSet<Enemy> hitEnemies)
    {
        if (!IsValidColumn(columnIndex)) return false;
        if (pushAmount <= 0) return false;

        var colEnemies = Columns[columnIndex].enemies;
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
            if (_isCavalrySlotReserved != null && _isCavalrySlotReserved(columnIndex, destRow, e)) return false;
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
        if (_isCavalrySlotReserved != null && _isCavalrySlotReserved(columnIndex, destinationRow, enemy)) return false;
        int bossRow = GetBossRowInColumn(columnIndex);
        if (bossRow >= 0 && destinationRow >= bossRow)
            return false;

        var occupant = Columns[columnIndex].GetEnemyAtRow(destinationRow);
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

        var col = Columns[columnIndex];
        _pushWorkList.Clear();

        for (int i = 0; i < columnHitEnemies.Count; i++)
        {
            var enemy = columnHitEnemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.columnIndex != columnIndex || enemy.IsMountedCavalry)
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
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.columnIndex != columnIndex || enemy.IsMountedCavalry)
                continue;

            bool hadExistingReturn = _pushReturnTransactions.TryGetValue(enemy, out var existing);
            int firstOriginColumn = hadExistingReturn ? existing.originalColumn : columnIndex;
            int firstOriginRow = hadExistingReturn ? existing.originalRow : enemy.rowIndex;
            RegisterPushReturn(enemy, firstOriginColumn, firstOriginRow, enemy.rowIndex + pushAmount);
            if (_releaseEnemyFromMovementSchedulers != null)
                _releaseEnemyFromMovementSchedulers(enemy, columnIndex);
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
            // 骑乘骑兵免疫击退
            if (e.IsMountedCavalry) continue;
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
}
