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
    /// 移除敌人后，后方所有敌人使用 ResetMovementState() + StartMoving() 向前补齐一排。
    /// 先重置移动状态（state=Idle, moveProgress=0），
    /// 再更新排索引（SetRowIndex），
    /// 最后调用 StartMoving() 开始向更前一排移动。
    ///
    /// BUG FIX: 使用 ResetMovementState() + StartMoving() 的组合，
    /// 而非 StartRushMoving()。StartMoving() 中的 state==Moving 保护检查
    /// 确保正在移动中的敌人不会被重置进度。
    /// ResetMovementState() 重置 state=Idle，使 StartMoving() 能通过保护检查。
    /// </summary>
    public void RemoveEnemy(Enemy enemy)
    {
        int index = enemies.IndexOf(enemy);
        if (index >= 0)
        {
            enemies.RemoveAt(index);
            // 更新后方所有敌人的排索引
            for (int i = index; i < enemies.Count; i++)
            {
                Enemy backEnemy = enemies[i];
                // 先重置移动状态（state=Idle, moveProgress=0），
                // 使 StartMoving() 能通过 state==Moving 保护检查
                backEnemy.ResetMovementState();
                // 再更新排索引（内部调用 UpdateWorldPosition() 更新位置）
                backEnemy.SetRowIndex(i);
                // 最后调用 StartMoving() 开始向更前一排移动
                backEnemy.StartMoving();
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
