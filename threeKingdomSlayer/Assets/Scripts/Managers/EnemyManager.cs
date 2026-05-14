using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人管理器 - 单例
/// 管理所有存活的敌人，处理敌人死亡事件，协调列管理
/// </summary>
public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [Header("列管理")]
    public ColumnManager columnManager;

    // 所有存活敌人的列表（用于遍历）
    private List<Enemy> allAliveEnemies = new List<Enemy>();

    // 事件
    public System.Action<Enemy> OnAnyEnemyDied;
    public System.Action OnAllEnemiesDied;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        if (columnManager == null)
        {
            columnManager = FindObjectOfType<ColumnManager>();
        }
    }

    #region 敌人注册

    /// <summary>
    /// 注册新生成的敌人
    /// </summary>
    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        allAliveEnemies.Add(enemy);
        enemy.OnDeath += OnEnemyDied;

        // 添加到列管理
        columnManager?.AddEnemyToColumn(enemy.columnIndex, enemy);
    }

    /// <summary>
    /// 批量注册敌人
    /// </summary>
    public void RegisterEnemies(List<Enemy> enemies)
    {
        foreach (var enemy in enemies)
        {
            RegisterEnemy(enemy);
        }
    }

    #endregion

    #region 敌人事件处理

    /// <summary>
    /// 敌人死亡回调
    /// 注意：闪白效果现在由 Enemy.Die() 中的协程处理（使用材质实例 + 延迟触发死亡事件），
    /// 因此此处直接回收敌人到对象池即可。
    /// </summary>
    private void OnEnemyDied(Enemy enemy)
    {
        if (enemy == null) return;

        allAliveEnemies.Remove(enemy);
        enemy.OnDeath -= OnEnemyDied;

        // 从列中移除
        columnManager?.RemoveEnemyFromColumn(enemy.columnIndex, enemy);

        // 触发事件（必须在 ReturnEnemy 之前）
        OnAnyEnemyDied?.Invoke(enemy);

        // 注意：不再在此处调用 EnemyPool.Instance?.ReturnEnemy(enemy)
        // 对象回收由 Enemy.DeathBounceAndFall() 协程在死亡动画播完后处理

        // 检查是否所有敌人都死亡
        if (allAliveEnemies.Count == 0)
        {
            OnAllEnemiesDied?.Invoke();
        }
    }

    /// <summary>
    /// 敌人前进了一排（由Enemy调用）
    /// </summary>
    public void OnEnemyMovedForward(Enemy enemy)
    {
        if (enemy == null) return;
        columnManager?.UpdateEnemyRow(enemy.columnIndex, enemy);
    }

    /// <summary>
    /// 敌人攻击玩家（由Enemy调用）
    /// </summary>
    public void OnEnemyAttackPlayer(Enemy enemy)
    {
        if (enemy == null) return;
        float damage = enemy.attackDamage;
        PlayerState.Instance?.TakeDamage(damage);
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 获取所有存活敌人
    /// </summary>
    public List<Enemy> GetAllAliveEnemies()
    {
        return allAliveEnemies;
    }

    /// <summary>
    /// 获取指定列的所有敌人
    /// </summary>
    public List<Enemy> GetEnemiesInColumn(int columnIndex)
    {
        return columnManager?.GetColumn(columnIndex)?.enemies;
    }

    /// <summary>
    /// 获取指定列的最前排敌人
    /// </summary>
    public Enemy GetFrontEnemyInColumn(int columnIndex)
    {
        return columnManager?.GetColumn(columnIndex)?.GetFrontEnemy();
    }

    /// <summary>
    /// 获取存活敌人总数
    /// </summary>
    public int AliveEnemyCount => allAliveEnemies.Count;

    /// <summary>
    /// 是否所有敌人都已死亡
    /// </summary>
    public bool IsAllEnemiesDead => allAliveEnemies.Count == 0;

    #endregion

    #region 清理

    /// <summary>
    /// 清除所有敌人（关卡切换时调用）
    /// </summary>
    public void ClearAllEnemies()
    {
        // 复制列表以避免迭代时修改
        var enemiesToClear = new List<Enemy>(allAliveEnemies);
        foreach (var enemy in enemiesToClear)
        {
            if (enemy != null)
            {
                enemy.OnDeath -= OnEnemyDied;
                EnemyPool.Instance?.ReturnEnemy(enemy);
            }
        }
        allAliveEnemies.Clear();
        columnManager?.ClearAllColumns();
    }

    #endregion
}
