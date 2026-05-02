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
    /// </summary>
    public void AddEnemy(Enemy enemy)
    {
        enemy.SetColumnPosition(columnIndex, enemies.Count);
        enemies.Add(enemy);
    }

    /// <summary>
    /// 移除指定敌人（通常是最前排死亡）
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
                enemies[i].SetRowIndex(i);
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
