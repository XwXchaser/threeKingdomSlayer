using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// DisplacementMover 的**击退回位**：被推走的敌人按原路逐段返回的事务（登记/推进/取消）。
///
/// 按机制从 <c>DisplacementMover.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 DisplacementMover.cs **同类型**，行为完全一致（本刀只搬不改，位移三原则的判定逻辑原样保留）。
/// </summary>

public sealed partial class DisplacementMover
{
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
}