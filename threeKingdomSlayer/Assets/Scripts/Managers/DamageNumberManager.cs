using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害数字管理器（单例 + 对象池）
/// 管理 DamageNumber 对象的生成、回收和复用
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Header("对象池设置")]
    [SerializeField] private GameObject damageNumberPrefab;
    [SerializeField] private int initialPoolSize = 20;

    [Header("位置偏移")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 2.5f, 0f); // 敌人头顶正上方

    private Queue<DamageNumber> pool = new Queue<DamageNumber>();
    private Transform poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 创建对象池根节点
        GameObject root = new GameObject("DamageNumberPool");
        root.transform.SetParent(transform);
        poolRoot = root.transform;

        // 预创建初始池
        if (damageNumberPrefab != null)
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewDamageNumber();
            }
        }
        else
        {
            Debug.LogError("[DamageNumberManager] damageNumberPrefab 未设置！");
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
    /// 创建一个新的伤害数字对象并加入池
    /// </summary>
    private DamageNumber CreateNewDamageNumber()
    {
        GameObject go = Instantiate(damageNumberPrefab, poolRoot);
        go.SetActive(false);
        DamageNumber dn = go.GetComponent<DamageNumber>();
        if (dn == null)
        {
            dn = go.AddComponent<DamageNumber>();
        }
        dn.OnReturnToPool = ReturnDamageNumber;
        pool.Enqueue(dn);
        return dn;
    }

    /// <summary>
    /// 从对象池获取一个伤害数字对象，如果没有可用则新建
    /// </summary>
    private DamageNumber GetDamageNumber()
    {
        if (pool.Count == 0)
        {
            CreateNewDamageNumber();
        }
        return pool.Dequeue();
    }

    /// <summary>
    /// 回收伤害数字对象到池
    /// </summary>
    private void ReturnDamageNumber(DamageNumber dn)
    {
        if (dn == null) return;
        dn.ResetNumber();
        dn.transform.SetParent(poolRoot);
        pool.Enqueue(dn);
    }

    /// <summary>
    /// 在指定敌人位置右侧显示伤害数字
    /// </summary>
    /// <param name="enemyWorldPos">敌人的世界坐标位置</param>
    /// <param name="damage">伤害数值</param>
    public void Spawn(Vector3 enemyWorldPos, float damage, Color? colorOverride = null)
    {
        if (damageNumberPrefab == null) return;

        DamageNumber dn = GetDamageNumber();

        // 位置：敌人位置 + 右侧偏上偏移
        Vector3 spawnPos = enemyWorldPos + positionOffset;

        // 添加随机水平偏移（±0.3f），避免多个伤害数字完全重叠
        spawnPos.x += Random.Range(-0.3f, 0.3f);

        dn.Show(spawnPos, damage, colorOverride);
    }
}
