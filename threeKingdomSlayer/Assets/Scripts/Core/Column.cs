using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单列敌人管理
/// 维护一列中的敌人列表，index 0 = 最前排（靠近玩家）
/// </summary>
[System.Serializable]
public class Column
{
    public int columnIndex; // 0~4
    public List<Enemy> enemies = new List<Enemy>(); // index 0 = 最前排

    public Column(int index)
    {
        columnIndex = index;
        enemies = new List<Enemy>();
    }

    /// <summary>
    /// 获取最前排的敌人
    /// </summary>
    public Enemy GetFrontEnemy()
    {
        if (enemies.Count > 0)
            return enemies[0];
        return null;
    }

    /// <summary>
    /// 获取指定排的敌人（0=最前排）
    /// </summary>
    public Enemy GetEnemyAtRow(int rowIndex)
    {
        if (rowIndex >= 0 && rowIndex < enemies.Count)
            return enemies[rowIndex];
        return null;
    }

    /// <summary>
    /// 在队列末尾添加敌人
    /// 注意：enemy 的 rowIndex 应在调用此方法前由调用方设置好
    /// 此方法仅将敌人加入列表，不再覆盖 rowIndex
    /// </summary>
    public void AddEnemy(Enemy enemy)
    {
        // BUG FIX: 不再覆盖 enemy 的 rowIndex
        // 调用方（如 WaveSpawner.SpawnRow）已通过 enemy.Initialize() 设置了正确的 rowIndex
        // 这里只设置 columnIndex 以确保列索引正确
        enemy.columnIndex = columnIndex;
        enemies.Add(enemy);
    }

    /// <summary>
    /// 移除指定敌人（通常是最前排死亡）
    /// 移除敌人后，后方所有敌人标记 pendingRushMove = true，
    /// 更新排索引 SetRowIndex(i+1)，然后链式触发补齐移动。
    ///
    /// 链式补齐改进：
    ///   - SetRowIndex(i+1)：后方敌人排索引设为新列表位置+1，
    ///     补齐移动完成后 rowIndex-- = 列表位置，不会与其他敌人重合。
    ///   - 链式触发：第一个敌人补齐移动完全完成(moveProgress>=1.0)时，
    ///     通过 OnRushMoveComplete 事件启动下一个敌人。
    ///     必须等待前一敌人完全补齐完毕，后一敌人才能开始补齐。
    ///
    /// Problem 3 修复（补齐延迟）：
    ///   - 补齐移动完成后若还需继续前进，启动延迟计时器，
    ///     延迟结束后再开始下一次补齐移动。
    /// </summary>
    public void RemoveEnemy(Enemy enemy)
    {
        int index = enemies.IndexOf(enemy);
        if (index >= 0)
        {
            enemies.RemoveAt(index);
            int colIndex = enemy.columnIndex;
            Debug.Log($"[Column] RemoveEnemy: column={colIndex}, deadIndex={index}, remaining={enemies.Count}");

            // BUG FIX: 恢复 SetRowIndex(i+1) 调用。
            // 每次 RemoveEnemy 重新设置后方敌人的排索引为列表位置+1，
            // 这样补齐移动完成后 rowIndex-- = 列表位置，不会因连续多次补齐导致重合。
            // 例如：位置0排索引1→移动→rowIndex=0；位置1排索引2→移动→rowIndex=1
            // 各敌人最终排索引与列表位置一致，不会发生重合。
            for (int i = index; i < enemies.Count; i++)
            {
                Enemy backEnemy = enemies[i];
                // 设置排索引为新列表位置+1（一次补齐移动后降为列表位置）
                backEnemy.SetRowIndex(i + 1);
                // BUG FIX: Problem 4 - 设置目标排位置为列表位置
                // 当 rowIndex <= targetRow 时，停止延迟循环中的继续补齐
                backEnemy.targetRow = i;
                // 重置移动状态（重置 state=Idle 使 StartMoving 能通过保护检查）
                backEnemy.ResetMovementState();
                // 标记需要向前补齐（链式触发）
                backEnemy.pendingRushMove = true;
                Debug.Log($"[Column] 标记补齐移动: enemyId={backEnemy.config?.enemyId}, col={colIndex}, newRow={i + 1}, targetRow={i}, pending={backEnemy.pendingRushMove}");
            }

            // 只启动第一个敌人的补齐移动，后续通过链式触发
            if (index < enemies.Count)
            {
                Enemy firstToMove = enemies[index];
                firstToMove.OnRushMoveComplete += OnColumnRushMoveComplete;
                firstToMove.TryStartRushMove();
                Debug.Log($"[Column] 启动链式补齐: enemyId={firstToMove.config?.enemyId}, col={colIndex}, row={firstToMove.rowIndex}");
            }
        }
    }

    /// <summary>
    /// 链式补齐回调：当前敌人补齐完成后，启动下一个敌人
    /// </summary>
    private void OnColumnRushMoveComplete(Enemy enemy)
    {
        // 取消订阅当前敌人的完成事件
        enemy.OnRushMoveComplete -= OnColumnRushMoveComplete;

        // 查找下一个需要补齐的敌人
        int idx = enemies.IndexOf(enemy);
        int nextIdx = idx + 1;
        if (nextIdx >= 0 && nextIdx < enemies.Count)
        {
            Enemy nextEnemy = enemies[nextIdx];
            if (nextEnemy.pendingRushMove)
            {
                nextEnemy.OnRushMoveComplete += OnColumnRushMoveComplete;
                nextEnemy.TryStartRushMove();
                Debug.Log($"[Column] 链式触发下一个: enemyId={nextEnemy.config?.enemyId}, col={columnIndex}, row={nextEnemy.rowIndex}");
            }
        }
    }

    /// <summary>
    /// 获取该列敌人总数
    /// </summary>
    public int EnemyCount => enemies.Count;

    /// <summary>
    /// 该列是否为空
    /// </summary>
    public bool IsEmpty => enemies.Count == 0;
}
