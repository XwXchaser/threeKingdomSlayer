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

            // BUG FIX（v1 - 过滤Dead敌人）: 清除所有处于 Dead 状态的敌人（同时死亡，协程同时完成时，
            // 后方敌人 OnDeath 事件尚未处理）。这些敌人的死亡协程（FlashThenRelease）已完成，state 为 Dead，
            // 它们的 OnDeath 事件会在后续被触发（EnemyManager.OnEnemyDied 会处理它们）。
            // 如果不清理它们：TryStartRushMove() 会因 state==Dead 返回 false，导致链式补齐永久中断。
            //
            // BUG FIX（v2 - 移除 SetRowIndex 瞬移）: 不再调用 SetRowIndex() 设置 rowIndex，
            // 因为 SetRowIndex() 立即调用 UpdateWorldPosition()，将敌人从当前位置瞬时跳转到目标位置。
            // 改为让敌人保持当前 rowIndex，利用 targetRow 控制逐步前进（多步补齐）。
            // UpdateMovement() 中已有的逻辑：移动完成后检查 rowIndex <= targetRow，
            // 若未到达则延迟后继续移动，从当前位置一步步前进到目标位置，无瞬移。
            int aliveCount = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e.state == EnemyState.Dead)
                {
                    Debug.Log($"[Column] 跳过 Dead 敌人（稍后由其自身 OnDeath 处理）: enemyId={e.config?.enemyId}, col={colIndex}, row={e.rowIndex}");
                    continue;
                }

                // 将存活敌人紧凑排列到列表前面
                if (i != aliveCount)
                {
                    enemies[aliveCount] = e;
                }

                // BUG FIX（移除 SetRowIndex 瞬移）: 
                // 不再调用 SetRowIndex() 设置排索引，因为 SetRowIndex() 会立即调用 UpdateWorldPosition()
                // 将敌人从当前位置（如 row=2）瞬时跳转到目标位置（row=1），造成"瞬移"视觉。
                // 替代方案：敌人保持当前 rowIndex，利用 targetRow 控制前进距离。
                // UpdateMovement() 中已有的逻辑：移动完成后检查 rowIndex <= targetRow，
                // 若未到达则延迟后继续移动（多步补齐），从当前位置一步步前进到目标位置。
                e.targetRow = aliveCount;
                // 重置移动状态（重置 state=Idle 使 StartMoving 能通过保护检查）
                e.ResetMovementState();
                // 标记需要向前补齐（链式触发）
                e.pendingRushMove = true;
                Debug.Log($"[Column] 标记补齐移动: enemyId={e.config?.enemyId}, col={colIndex}, curRow={e.rowIndex}, targetRow={aliveCount}, pending={e.pendingRushMove}");

                aliveCount++;
            }

            // 移除列表末尾的"空洞"（被跳过的 Dead 敌人留下的空位）
            if (aliveCount < enemies.Count)
            {
                enemies.RemoveRange(aliveCount, enemies.Count - aliveCount);
            }

            // 只启动第一个存活敌人的补齐移动，后续通过链式触发
            if (aliveCount > 0)
            {
                Enemy firstToMove = enemies[0];
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
