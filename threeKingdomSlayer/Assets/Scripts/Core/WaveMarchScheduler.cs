using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次行军（普通补齐）的**状态与查询**。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 4a）。职责边界：
///   • 本类持有补齐调度的**全部状态**：本次订单的 generation、计划/生效的目标行与目标槽、
///     准备中/待行军的敌人集合、暂停快照；
///   • 只读查询（某排是否已空、目标槽是否被占、订单能否开始、能否进入某排）也在此；
///   • **流程**（什么时候发起补齐、如何派单）暂留 <c>ColumnManager</c>，刀 4b 再搬。
///
/// 规则依据：design/percolumn-fillup-rules.md（普通补齐只由整排清空触发）与
/// design/immutable-constraints.md 第 6 条（订单完成必须校验 owner + generation）。
/// </summary>
public sealed class WaveMarchScheduler
{
    private readonly Func<Column[]> _columnsProvider;
    private readonly Func<int, int, Enemy, bool> _isCavalrySlotReserved;

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

    public WaveMarchScheduler(
        Func<Column[]> columnsProvider,
        Func<int, int, Enemy, bool> isCavalrySlotReserved)
    {
        _columnsProvider = columnsProvider;
        _isCavalrySlotReserved = isCavalrySlotReserved;
    }

    private Column[] Columns => _columnsProvider != null ? _columnsProvider() : null;

    private int ColumnCount => Columns != null ? Columns.Length : 0;

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
}
