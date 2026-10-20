using System.Collections.Generic;
using UnityEngine;

/// <summary>逻辑排的种类：活人排 / 普通空排占位符 / 999 节奏门。</summary>
public enum LogicalWaveRowKind
{
    Live,
    Empty,
    RhythmGate
}

/// <summary>逻辑排队列里的一个排节点。</summary>
public sealed class LogicalWaveRow
{
    public LogicalWaveRowKind kind;
    public int initialRow;
    public int currentRow;
    public bool removed;
}

/// <summary>
/// 逻辑排队列：波次的「逻辑排」结构与纯数据操作。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 1）。职责边界：
///   • 本类只管**队列本身**：活人排 / 普通空排占位符 / 999 节奏门、门的开关状态、
///     运行时排号重排、敌人与逻辑排的归属；
///   • **不**决定何时补齐、**不**派单、**不**移动敌人（那是调度器与位移类的职责）；
///   • 规则依据见 design/percolumn-fillup-rules.md（普通补齐只由整排清空触发、
///     空排占位符随队列前移、999 门冻结后方区段）。
///
/// 纯数据 + 纯操作，不做场景查找，可在没有 Unity 场景的情况下测。
/// </summary>
public sealed class LogicalRowQueue
{
    private readonly List<LogicalWaveRow> _rows = new List<LogicalWaveRow>();
    private readonly Dictionary<int, LogicalWaveRow> _rowsByInitialRow = new Dictionary<int, LogicalWaveRow>();
    private readonly Dictionary<Enemy, LogicalWaveRow> _enemyRows = new Dictionary<Enemy, LogicalWaveRow>();
    private readonly HashSet<int> _openedRhythmGateRows = new HashSet<int>();
    private bool _dirty;
    private int _layoutGeneration;
    private bool _useRowMarchPlanner;

    /// <summary>布局代数（每次重建/清空 +1），用于订单的 generation 校验。</summary>
    public int LayoutGeneration => _layoutGeneration;

    /// <summary>是否使用「逻辑排队列」规划（PerRow）；false 表示遗留扫描模式。</summary>
    public bool UseRowMarchPlanner => _useRowMarchPlanner;

    /// <summary>是否有待处理的布局变化（活人排被清空 / 骑兵下马等）。</summary>
    public bool IsDirty => _dirty;

    /// <summary>按初始顺序排列的逻辑排。</summary>
    public IReadOnlyList<LogicalWaveRow> Rows => _rows;

    /// <summary>敌人 → 逻辑排归属。</summary>
    public IEnumerable<KeyValuePair<Enemy, LogicalWaveRow>> EnemyRows => _enemyRows;

    /// <summary>按波次配置重建队列（普通空排占位符与 999 门都保留来源语义）。</summary>
    public void Configure(IList<RowConfig> rows, int rowOffset)
    {
        _layoutGeneration++;
        _useRowMarchPlanner = true;
        _rows.Clear();
        _rowsByInitialRow.Clear();
        _enemyRows.Clear();
        _openedRhythmGateRows.Clear();
        _dirty = false;

        if (rows == null)
            return;

        for (int i = 0; i < rows.Count; i++)
        {
            int runtimeRow = i + rowOffset;
            var row = rows[i];
            var logicalRow = new LogicalWaveRow
            {
                kind = row != null && row.IsRhythmGate
                    ? LogicalWaveRowKind.RhythmGate
                    : row != null && row.HasConfiguredEnemies
                        ? LogicalWaveRowKind.Live
                        : LogicalWaveRowKind.Empty,
                initialRow = runtimeRow,
                currentRow = runtimeRow
            };
            _rows.Add(logicalRow);
            _rowsByInitialRow[runtimeRow] = logicalRow;
        }
    }

    /// <summary>清空队列（回到遗留扫描模式）。</summary>
    public void Clear()
    {
        _layoutGeneration++;
        _useRowMarchPlanner = false;
        _rows.Clear();
        _rowsByInitialRow.Clear();
        _enemyRows.Clear();
        _openedRhythmGateRows.Clear();
        _dirty = false;
    }

    /// <summary>该运行时排是不是一道 999 节奏门。</summary>
    public bool IsRhythmGateRow(int row)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            var logicalRow = _rows[i];
            if (!logicalRow.removed && logicalRow.kind == LogicalWaveRowKind.RhythmGate && logicalRow.currentRow == row)
                return true;
        }
        return false;
    }

    /// <summary>该运行时排上的门是否已开启（前方没有活人排即开启）。</summary>
    public bool IsRhythmGateOpen(int row)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            var gate = _rows[i];
            if (gate.removed || gate.kind != LogicalWaveRowKind.RhythmGate || gate.currentRow != row)
                continue;
            return IsRhythmGateOpen(gate);
        }
        return true;
    }

    private bool IsRhythmGateOpen(LogicalWaveRow gate)
    {
        if (_openedRhythmGateRows.Contains(gate.initialRow))
            return true;

        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (row == gate)
                break;
            if (!row.removed && row.kind == LogicalWaveRowKind.Live)
                return false;
        }

        _openedRhythmGateRows.Add(gate.initialRow);
        return true;
    }

    /// <summary>登记敌人所属的逻辑排（按它生成时的配置排号）。</summary>
    public void RegisterEnemy(Enemy enemy, int configuredRow)
    {
        if (enemy == null || !_rowsByInitialRow.TryGetValue(configuredRow, out var logicalRow))
            return;
        _enemyRows[enemy] = logicalRow;
    }

    /// <summary>移除敌人的逻辑排归属。</summary>
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy != null)
            _enemyRows.Remove(enemy);
    }

    /// <summary>取敌人所属的逻辑排。</summary>
    public bool TryGetRowForEnemy(Enemy enemy, out LogicalWaveRow row)
    {
        row = null;
        return enemy != null && _enemyRows.TryGetValue(enemy, out row);
    }

    /// <summary>该敌人是否登记在队列里。</summary>
    public bool HasRowFor(Enemy enemy)
    {
        return enemy != null && _enemyRows.ContainsKey(enemy);
    }

    /// <summary>该逻辑排里是否还有活着的敌人。</summary>
    public bool HasLivingMembers(LogicalWaveRow logicalRow)
    {
        foreach (var pair in _enemyRows)
        {
            var enemy = pair.Key;
            if (pair.Value == logicalRow && enemy != null && enemy.state != EnemyState.Dead)
                return true;
        }
        return false;
    }

    /// <summary>该活人排被清空：从队列移除并标记待重排。</summary>
    public void MarkRowRemoved(LogicalWaveRow row)
    {
        if (row == null) return;
        row.removed = true;
        _dirty = true;
    }

    /// <summary>标记布局有变化（等待调度器重排）。</summary>
    public void MarkDirty()
    {
        _dirty = true;
    }

    /// <summary>消费掉待重排标记。</summary>
    public void ConsumeDirty()
    {
        _dirty = false;
    }

    /// <summary>
    /// 重排运行时排号：活人排依次前移；普通空排占位符在前方还有活人排时保留，
    /// 只有前导空排（前方已无活人排）才被移除；999 门在开启后被移除。
    /// </summary>
    public void ReassignRuntimeRows()
    {
        int nextRuntimeRow = 0;
        bool hasLiveBefore = false;
        for (int i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            if (row.removed)
                continue;

            if (row.kind == LogicalWaveRowKind.RhythmGate)
            {
                if (!IsRhythmGateOpen(row))
                {
                    nextRuntimeRow = row.currentRow + 1;
                    hasLiveBefore = false;
                    continue;
                }

                row.removed = true;
                continue;
            }

            if (row.kind == LogicalWaveRowKind.Empty && !hasLiveBefore)
            {
                row.removed = true;
                continue;
            }

            row.currentRow = nextRuntimeRow++;
            if (row.kind == LogicalWaveRowKind.Live)
                hasLiveBefore = true;
        }
    }
}
