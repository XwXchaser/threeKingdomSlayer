using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 列的**只读查询层**。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 2）。职责边界：
///   • 本类**只读**：不改任何列/排状态，不创建移动，不决定补齐；
///   • 它是战斗侧（<c>AttackSystem</c> 31 处、QTE 选目标、效果范围判定）的主要取数入口，
///     独立出来后，这层「只读」的契约就与其他调度逻辑物理分开，后续改调度不会误伤取数。
///
/// 取列数组用委托传入，避免 <c>ColumnManager.InitializeColumns()</c> 重建数组后拿到旧引用。
/// </summary>
public sealed class ColumnQueries
{
    private readonly Func<Column[]> _columnsProvider;
    private readonly List<Enemy> _rangeQueryList = new List<Enemy>();

    public ColumnQueries(Func<Column[]> columnsProvider)
    {
        _columnsProvider = columnsProvider;
    }

    private Column[] Columns => _columnsProvider != null ? _columnsProvider() : null;

    private int ColumnCount => Columns != null ? Columns.Length : 0;

    private bool IsValidColumn(int columnIndex)
    {
        var cols = Columns;
        return cols != null && columnIndex >= 0 && columnIndex < cols.Length;
    }

    /// <summary>
    /// 获取指定列
    /// </summary>
    public Column GetColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return Columns[columnIndex];
    }

    /// <summary>
    /// 获取指定列的最前排敌人
    /// </summary>
    public Enemy GetFrontEnemy(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return Columns[columnIndex].GetFrontEnemy();
    }

    /// <summary>
    /// 获取指定列指定排的敌人
    /// </summary>
    public Enemy GetEnemyAt(int columnIndex, int rowIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return Columns[columnIndex].GetEnemyAtRow(rowIndex);
    }

    /// <summary>
    /// 获取指定列的所有敌人
    /// </summary>
    public List<Enemy> GetEnemiesInColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;
        return Columns[columnIndex].enemies;
    }

    /// <summary>
    /// Returns the living InCombat Boss whose horizontal footprint covers the requested column.
    /// Bosses remain stored in their center column; coverage only changes attack targeting.
    /// </summary>
    public Enemy GetCombatBossCoveringColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return null;

        var cols = Columns;
        for (int centerColumn = 0; centerColumn < ColumnCount; centerColumn++)
        {
            var enemies = cols[centerColumn].enemies;
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || !enemy.isBoss || enemy.state == EnemyState.Dead || enemy.bossState != BossState.InCombat)
                    continue;

                int footprint = Mathf.Clamp(enemy.occupySlots, 1, ColumnCount);
                int left = Mathf.Clamp(enemy.columnIndex - footprint / 2, 0, ColumnCount - footprint);
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
        var cols = Columns;
        if (cols == null) return allEnemies;
        for (int i = 0; i < cols.Length; i++)
        {
            allEnemies.AddRange(cols[i].enemies);
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

        Column column = Columns[columnIndex];
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
        var cols = Columns;
        if (cols == null) return _rangeQueryList;
        for (int i = 0; i < cols.Length; i++)
        {
            var col = cols[i];
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
        var cols = Columns;
        if (cols == null) return result;
        for (int i = 0; i < cols.Length; i++)
        {
            Column column = cols[i];
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
        return Columns[columnIndex].IsEmpty;
    }

    /// <summary>
    /// 是否所有列为空
    /// </summary>
    public bool AreAllColumnsEmpty()
    {
        var cols = Columns;
        if (cols == null) return true;
        for (int i = 0; i < cols.Length; i++)
        {
            if (!cols[i].IsEmpty) return false;
        }
        return true;
    }

    /// <summary>
    /// 获取指定列的敌人数量
    /// </summary>
    public int GetColumnEnemyCount(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return 0;
        return Columns[columnIndex].EnemyCount;
    }

    /// <summary>
    /// 获取所有列的敌人总数
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int total = 0;
        var cols = Columns;
        if (cols == null) return total;
        for (int i = 0; i < cols.Length; i++)
        {
            total += cols[i].EnemyCount;
        }
        return total;
    }
}
