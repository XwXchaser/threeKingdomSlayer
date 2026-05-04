using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用对象池 - 按敌人ID分池管理
/// 支持预创建和动态扩容
///
/// 【自动注册方案】
/// 将敌人预制体放入 Resources/EnemyPrefabs/ 文件夹下，
/// 预制体文件名格式为：Enemy_{enemyId}.prefab
/// 例如：Enemy_1.prefab、Enemy_2.prefab
/// 系统会在 Awake() 时自动扫描并注册，无需手动调用 RegisterPrefab()
///
/// 如果不想使用自动注册，也可以手动调用 RegisterPrefab() 注册
/// </summary>
public class EnemyPool : MonoBehaviour
{
    public static EnemyPool Instance { get; private set; }

    [Header("池设置")]
    public Transform poolRoot; // 对象池根节点（可选，用于存放未激活的敌人）
    public Transform enemiesRoot; // 敌人运行时父节点（可选，用于场景中整体移动敌人位置）
    public int defaultPoolSize = 20; // 每个池默认预创建数量

    [Header("自动注册设置")]
    [Tooltip("是否在 Awake 时自动从 Resources/EnemyPrefabs/ 加载预制体")]
    public bool autoLoadFromResources = true;
    [Tooltip("Resources 中存放敌人预制体的文件夹路径（相对于 Resources 根目录）")]
    public string resourcesPrefabPath = "EnemyPrefabs";

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

        // 自动从 Resources 加载预制体
        if (autoLoadFromResources)
        {
            AutoRegisterPrefabsFromResources();
        }
    }

    /// <summary>
    /// 从 Resources 文件夹自动加载并注册敌人预制体
    /// 预制体文件名格式：Enemy_{enemyId}.prefab
    /// 例如：Enemy_1.prefab → enemyId = 1
    ///       Enemy_2.prefab → enemyId = 2
    ///
    /// 【回退方案】
    /// 1. 优先从 Resources/EnemyPrefabs/ 加载
    /// 2. 如果为空，尝试从项目 Prefabs/ 目录加载（通过 Resources.Load 无法直接访问 Prefabs 目录）
    /// 3. 如果仍然没有，则动态创建临时 Cube 作为敌人（确保至少能看到敌人出现）
    /// </summary>
    private void AutoRegisterPrefabsFromResources()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>(resourcesPrefabPath);
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.Log($"[EnemyPool] Resources/{resourcesPrefabPath}/ 中未找到任何预制体，尝试创建临时敌人");

            // 回退方案：动态创建临时 Cube 作为敌人
            // 这样即使没有预制体，也能看到敌人出现
            CreateFallbackEnemyPrefab(101); // 对应 Enemy_Skeleton.asset 的 enemyId
            return;
        }

        foreach (GameObject prefab in prefabs)
        {
            // 从文件名解析 enemyId
            // 格式：Enemy_{enemyId} 或 Enemy_{enemyId}_任意后缀
            string name = prefab.name;
            int enemyId = ParseEnemyIdFromName(name);
            if (enemyId > 0)
            {
                RegisterPrefab(enemyId, prefab);
                Debug.Log($"[EnemyPool] 自动注册敌人预制体: {name} → enemyId={enemyId}");
            }
            else
            {
                Debug.LogWarning($"[EnemyPool] 无法从文件名解析 enemyId: {name}，跳过注册。" +
                    $"请确保文件名格式为 Enemy_{{enemyId}}，例如 Enemy_1");
            }
        }
    }

    /// <summary>
    /// 创建临时敌人预制体（回退方案）
    /// 当 Resources/EnemyPrefabs/ 中没有预制体时，动态创建 Cube 作为敌人
    /// 这样即使没有配置预制体，也能看到敌人出现并测试游戏逻辑
    /// </summary>
    private void CreateFallbackEnemyPrefab(int enemyId)
    {
        // 创建临时 GameObject
        GameObject tempPrefab = new GameObject($"Enemy_{enemyId}_Temp");
        
        // 添加 Cube 网格和渲染器，使其可见
        MeshFilter meshFilter = tempPrefab.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        MeshRenderer meshRenderer = tempPrefab.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
        meshRenderer.sharedMaterial.color = Color.red; // 临时敌人用红色区分
        
        // 添加 Enemy 组件
        tempPrefab.AddComponent<Enemy>();
        
        // 注册到对象池
        RegisterPrefab(enemyId, tempPrefab);
        Debug.Log($"[EnemyPool] 已创建临时敌人预制体 (enemyId={enemyId})，请尽快将预制体放入 Resources/EnemyPrefabs/ 目录");
    }

    /// <summary>
    /// 从文件名解析敌人ID
    /// 支持格式：Enemy_1、Enemy_1_v2、Enemy_1_弓箭手 等
    /// </summary>
    private int ParseEnemyIdFromName(string name)
    {
        // 查找 "Enemy_" 前缀
        const string prefix = "Enemy_";
        if (!name.StartsWith(prefix)) return -1;

        string afterPrefix = name.Substring(prefix.Length);
        // 提取数字部分（直到遇到非数字字符）
        string numberStr = "";
        foreach (char c in afterPrefix)
        {
            if (char.IsDigit(c))
            {
                numberStr += c;
            }
            else
            {
                break; // 遇到非数字字符就停止
            }
        }

        if (int.TryParse(numberStr, out int id))
        {
            return id;
        }
        return -1;
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
        // 从对象池取出时，挂载到 enemiesRoot 下（如果设置了的话）
        // 这样敌人会相对于 enemiesRoot 定位，调整 enemiesRoot 的 Position 即可整体移动
        if (enemiesRoot != null)
        {
            pooled.transform.SetParent(enemiesRoot);
        }
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
        // 回收时挂回 poolRoot（隐藏起来），下次激活时 CreateNewEnemy 会重新挂到 enemiesRoot
        enemy.transform.SetParent(poolRoot);

        if (!pools.ContainsKey(enemyId))
        {
            pools[enemyId] = new Queue<Enemy>();
        }
        pools[enemyId].Enqueue(enemy);
    }

    /// <summary>
    /// 创建新的敌人实例
    /// 如果设置了 enemiesRoot，则挂载到 enemiesRoot 下（用于场景中整体移动敌人位置）
    /// 否则挂载到 poolRoot 下
    /// </summary>
    private Enemy CreateNewEnemy(GameObject prefab)
    {
        // 激活的敌人挂载到 enemiesRoot，未激活的挂载到 poolRoot
        Transform parent = enemiesRoot != null ? enemiesRoot : poolRoot;
        GameObject go = Instantiate(prefab, parent);
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
