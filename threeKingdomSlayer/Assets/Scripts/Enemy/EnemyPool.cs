using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池 - 按敌人ID分池管理
/// 支持预创建和动态扩容
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Header("池设置")]
    public Transform poolRoot; // 对象池根节点（可选）
    public int defaultPoolSize = 20; // 每个池默认预创建数量

    // 按敌人ID分池：Dictionary<enemyId, Queue<Enemy>>
    private Dictionary<int, Queue<Enemy>> pools = new Dictionary<int, Queue<Enemy>>();

    // 敌人预制体注册表：Dictionary<enemyId, GameObject>
    private Dictionary<int, GameObject> enemyPrefabs = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (poolRoot == null)
        {
            poolRoot = transform;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 注册敌人预制体
    /// </summary>
    public void RegisterPrefab(int enemyId, GameObject prefab)
    {
        if (!enemyPrefabs.ContainsKey(enemyId))
        {
            enemyPrefabs[enemyId] = prefab;
        }
    }

    /// <summary>
    /// 预创建指定ID的敌人池
    /// </summary>
    public void PrewarmPool(int enemyId, int count)
    {
        if (!enemyPrefabs.ContainsKey(enemyId))
        {
            Debug.LogError($"[EnemyPool] 未注册敌人ID {enemyId} 的预制体");
            return;
        }

        if (!pools.ContainsKey(enemyId))
        {
            pools[enemyId] = new Queue<Enemy>();
        }

        GameObject prefab = enemyPrefabs[enemyId];
        for (int i = 0; i < count; i++)
        {
            Enemy enemy = CreateNewEnemy(prefab);
            enemy.ResetEnemy();
            pools[enemyId].Enqueue(enemy);
        }
    }

    /// <summary>
    /// 从对象池获取一个敌人
    /// </summary>
    public Enemy GetEnemy(int enemyId)
    {
        // 如果池不存在，先创建
        if (!pools.ContainsKey(enemyId))
        {
            pools[enemyId] = new Queue<Enemy>();
        }

        // 如果池为空且有预制体，动态扩容
        if (pools[enemyId].Count == 0)
        {
            if (!enemyPrefabs.ContainsKey(enemyId))
            {
                Debug.LogError($"[EnemyPool] 未注册敌人ID {enemyId} 的预制体");
                return null;
            }
            Enemy enemy = CreateNewEnemy(enemyPrefabs[enemyId]);
            enemy.ResetEnemy();
            return enemy;
        }

        Enemy pooled = pools[enemyId].Dequeue();
        pooled.gameObject.SetActive(true);
        return pooled;
    }

    /// <summary>
    /// 回收敌人到对象池
    /// </summary>
    public void ReturnEnemy(Enemy enemy)
    {
        if (enemy == null) return;

        // 必须在ResetEnemy()之前读取enemyId，因为ResetEnemy会将config置null
        int enemyId = enemy.config != null ? enemy.config.enemyId : -1;

        enemy.ResetEnemy();
        enemy.transform.SetParent(poolRoot);

        if (!pools.ContainsKey(enemyId))
        {
            pools[enemyId] = new Queue<Enemy>();
        }
        pools[enemyId].Enqueue(enemy);
    }

    /// <summary>
    /// 创建新的敌人实例
    /// </summary>
    private Enemy CreateNewEnemy(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, poolRoot);
        Enemy enemy = go.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = go.AddComponent<Enemy>();
        }
        return enemy;
    }

    /// <summary>
    /// 清空所有池
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var kvp in pools)
        {
            foreach (var enemy in kvp.Value)
            {
                if (enemy != null && enemy.gameObject != null)
                {
                    Destroy(enemy.gameObject);
                }
            }
        }
        pools.Clear();
    }
}
