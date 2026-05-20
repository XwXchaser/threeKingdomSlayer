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
    public void RemoveEnemyFromColumn(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        FillUpRule rule = StageController.Instance?.GetFillUpRule() ?? FillUpRule.PerColumn;
        if (rule == FillUpRule.PerRow)
        {
            // 逐排补齐：仅移除敌人，不触发逐列链，由 RowBasedFillUp 统一处理
            columns[columnIndex].RemoveEnemy(enemy, skipChain: true);
            RowBasedFillUp();
        }
        else
        {
            columns[columnIndex].RemoveEnemy(enemy);
        }
        OnColumnsModified?.Invoke();
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
    /// 更新敌人在列中的排索引（前进后调用）
    /// 仅在非补齐移动（自然移动、眩晕/挑飞后恢复）完成后调用。
    /// 补齐移动（死亡触发）由 Column.RemoveEnemy() 独立处理。
    ///
    /// 击飞敌人保留在列中（可被攻击），不参与补齐，不阻塞后方敌人填充前方空位。
    /// Dead 敌人跳过并清理。存活敌人紧凑排列，链式触发补齐。
    /// </summary>
    public void UpdateEnemyRow(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        FillUpRule rule = StageController.Instance?.GetFillUpRule() ?? FillUpRule.PerColumn;
        if (rule == FillUpRule.PerRow)
        {
            // 逐排补齐：自然移动后不触发逐列链，由 RowBasedFillUp 统一处理
            Debug.Log($"[ColumnManager] UpdateEnemyRow (PerRow): col={columnIndex}, {enemy.DebugTag}, 触发 RowBasedFillUp");
            RowBasedFillUp();
            OnColumnsModified?.Invoke();
            return;
        }

        Column column = columns[columnIndex];
        int currentIndex = column.enemies.IndexOf(enemy);
        Debug.Log($"[ColumnManager] UpdateEnemyRow: col={columnIndex}, {enemy.DebugTag}, currentIndex={currentIndex}, count={column.enemies.Count}");
        if (currentIndex > 0)
        {
            // 将敌人前移一位
            column.enemies.RemoveAt(currentIndex);
            column.enemies.Insert(currentIndex - 1, enemy);
            Debug.Log($"[ColumnManager] 重排列顺序：{enemy.DebugTag}, from={currentIndex}→{currentIndex - 1}");

            // 紧凑排列：Launched 保留原位不参与补齐，Dead 跳过并清理，存活敌人向前补齐
            int writeIdx = currentIndex;
            bool anyPendingRush = false;
            for (int i = currentIndex; i < column.enemies.Count; i++)
            {
                Enemy e = column.enemies[i];
                if (e.state == EnemyState.Dead)
                {
                    Debug.Log($"[ColumnManager] 跳过 Dead 敌人: {e.DebugTag}, col={columnIndex}, row={e.rowIndex}");
                    continue;
                }
                if (e.state == EnemyState.Launched)
                {
                    if (i != writeIdx) column.enemies[writeIdx] = e;
                    e.targetRow = writeIdx;
                    Debug.Log($"[ColumnManager] 保留 Launched 敌人（不参与补齐）: {e.DebugTag}, col={columnIndex}, row={e.rowIndex}, listPos={writeIdx}");
                    writeIdx++;
                    continue;
                }

                if (i != writeIdx) column.enemies[writeIdx] = e;
                e.targetRow = writeIdx;
                e.ResetMovementState();
                // Boss 在 Approaching 阶段不参与列内补齐，由分阶段推进系统控制
                if (!(e.isBoss && e.bossState == BossState.Approaching))
                {
                    e.pendingRushMove = true;
                    anyPendingRush = true;
                }
                Debug.Log($"[ColumnManager] 标记补齐移动: {e.DebugTag}, col={columnIndex}, curRow={e.rowIndex}, targetRow={writeIdx}");
                writeIdx++;
            }

            // 移除 Dead 敌人留下的空洞
            if (writeIdx < column.enemies.Count)
                column.enemies.RemoveRange(writeIdx, column.enemies.Count - writeIdx);

            // 链式触发：从第一个 pendingRushMove 的存活敌人开始
            if (anyPendingRush)
            {
                for (int i = currentIndex; i < column.enemies.Count; i++)
                {
                    if (column.enemies[i].pendingRushMove)
                    {
                        column.enemies[i].OnRushMoveComplete += OnColumnManagerRushComplete;
                        column.enemies[i].TryStartRushMove();
                        Debug.Log($"[ColumnManager] 启动链式补齐: {column.enemies[i].DebugTag}, col={columnIndex}");
                        break;
                    }
                }
            }

            OnColumnsModified?.Invoke();
        }
    }

    /// <summary>
    /// ColumnManager 补齐链回调：当前敌人补齐完成后，启动下一个 pendingRushMove 的存活敌人
    /// </summary>
    private void OnColumnManagerRushComplete(Enemy completed)
    {
        completed.OnRushMoveComplete -= OnColumnManagerRushComplete;
        Column column = columns[completed.columnIndex];
        int idx = column.enemies.IndexOf(completed);
        for (int i = idx + 1; i < column.enemies.Count; i++)
        {
            if (column.enemies[i].pendingRushMove)
            {
                column.enemies[i].OnRushMoveComplete += OnColumnManagerRushComplete;
                column.enemies[i].TryStartRushMove();
                return;
            }
        }
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
        List<Enemy> result = new List<Enemy>();
        for (int i = 0; i < columnCount; i++)
        {
            result.AddRange(GetEnemiesInRange(i, rangeRows));
        }
        return result;
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

    #region 逐排补齐（Row-Based Fill-Up）

    /// <summary>
    /// 逐排补齐：扫描所有列中存活敌人的 rowIndex（非 Dead、非 Launched），
    /// 找出已完全清空的行，然后将各列敌人向清空行压缩。
    /// 在 PerRow 模式下，任何列结构变化后都应调用此方法。
    ///
    /// 注意：使用 enemy.rowIndex 而非列表位置判断排归属。
    /// RemoveEnemy(skipChain=true) 移除阵亡敌人后列表位置会变化，
    /// 但存活敌人保留原有 rowIndex，列表位置不再反映真实排号。
    /// </summary>
    public void RowBasedFillUp()
    {
        // 1. 收集所有存活（非 Dead、非 Launched）敌人所在的排号
        int maxRow = 0;
        var occupiedRows = new System.Collections.Generic.HashSet<int>();
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
                Debug.Log($"[ColumnManager] RowBasedFillUp: 第{r}排已清空");
        }

        // 3. 各列按 clearRows 压缩
        for (int c = 0; c < columnCount; c++)
        {
            columns[c].CompactByClearRows(clearRows);
        }
    }

    #endregion

    #region 工具方法

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < columnCount;
    }

    #endregion
}
