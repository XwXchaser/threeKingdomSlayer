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

        columns[columnIndex].RemoveEnemy(enemy);
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
    /// 链式触发补齐移动，必须等待前一敌人完全补齐完毕，后一敌人才能开始补齐。
    /// 更新后方所有敌人的行索引后，只启动第一个敌人，后续通过 OnRushMoveComplete 逐个触发。
    /// </summary>
    public void UpdateEnemyRow(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        Column column = columns[columnIndex];
        int currentIndex = column.enemies.IndexOf(enemy);
        Debug.Log($"[ColumnManager] UpdateEnemyRow: col={columnIndex}, enemyId={enemy.config?.enemyId}, currentIndex={currentIndex}, count={column.enemies.Count}");
        if (currentIndex > 0)
        {
            // 将敌人前移一位
            column.enemies.RemoveAt(currentIndex);
            column.enemies.Insert(currentIndex - 1, enemy);
            Debug.Log($"[ColumnManager] 重排列顺序：enemyId={enemy.config?.enemyId}, from={currentIndex}→{currentIndex - 1}");

            // BUG FIX（v1 - 过滤Dead敌人）: 跳过 Dead 状态的敌人（可能在死亡协程窗口期内，后续由 RemoveEnemy 处理）。
            // 从 currentIndex 开始扫描，将存活敌人紧凑排列并清除 Dead 敌人。
            // BUG FIX（v2 - 移除 SetRowIndex 瞬移）: 不再调用 SetRowIndex()，
            // 敌人保持当前 rowIndex，利用 targetRow 控制逐步前进（多步补齐）。
            int writeIdx = currentIndex;
            for (int i = currentIndex; i < column.enemies.Count; i++)
            {
                Enemy e = column.enemies[i];
                if (e.state == EnemyState.Dead)
                {
                    Debug.Log($"[ColumnManager] 跳过 Dead 敌人（稍后由其自身 OnDeath 处理）: enemyId={e.config?.enemyId}, col={columnIndex}, row={e.rowIndex}");
                    continue;
                }

                // 紧凑排列存活敌人
                if (i != writeIdx)
                {
                    column.enemies[writeIdx] = e;
                }

                Debug.Log($"[ColumnManager] 更新后方敌人: enemyId={e.config?.enemyId}, curRow={e.rowIndex}, writeIdx={writeIdx}");
                e.ResetMovementState();
                // BUG FIX（移除 SetRowIndex 瞬移）: 不再调用 SetRowIndex()，
                // 敌人保持当前 rowIndex，利用 targetRow 控制多步补齐。
                // 从当前位置一步步前进到目标位置，无瞬移。
                e.targetRow = writeIdx;
                e.pendingRushMove = true;

                writeIdx++;
            }

            // 移除 Dead 敌人留下的空洞
            if (writeIdx < column.enemies.Count)
            {
                column.enemies.RemoveRange(writeIdx, column.enemies.Count - writeIdx);
            }

            // 只启动第一个存活敌人的补齐移动，后续通过链式触发
            if (writeIdx > currentIndex)
            {
                Enemy firstToMove = column.enemies[currentIndex];
                System.Action<Enemy> handler = null;
                handler = (completedEnemy) =>
                {
                    completedEnemy.OnRushMoveComplete -= handler;
                    int idx = column.enemies.IndexOf(completedEnemy);
                    int nextIdx = idx + 1;
                    if (nextIdx >= 0 && nextIdx < column.enemies.Count)
                    {
                        Enemy nextEnemy = column.enemies[nextIdx];
                        if (nextEnemy.pendingRushMove)
                        {
                            nextEnemy.OnRushMoveComplete += handler;
                            nextEnemy.TryStartRushMove();
                        }
                    }
                };
                firstToMove.OnRushMoveComplete += handler;
                firstToMove.TryStartRushMove();
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
    /// 获取指定列前N排的所有敌人
    /// </summary>
    public List<Enemy> GetEnemiesInRange(int columnIndex, int rangeRows)
    {
        List<Enemy> result = new List<Enemy>();
        if (!IsValidColumn(columnIndex)) return result;

        Column column = columns[columnIndex];
        int count = Mathf.Min(rangeRows, column.enemies.Count);
        for (int i = 0; i < count; i++)
        {
            result.Add(column.enemies[i]);
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
    /// 获取所有列中排索引小于等于指定值的敌人
    /// </summary>
    public List<Enemy> GetEnemiesByRowLimit(int maxRowIndex)
    {
        List<Enemy> result = new List<Enemy>();
        for (int i = 0; i < columnCount; i++)
        {
            Column column = columns[i];
            for (int j = 0; j < column.enemies.Count && j <= maxRowIndex; j++)
            {
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

    #region 工具方法

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < columnCount;
    }

    #endregion
}
