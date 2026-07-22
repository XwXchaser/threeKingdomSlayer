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

    [Header("道具掉落")]
    [Tooltip("击杀掉落道具池配置")]
    public DropItemPoolConfig dropItemPoolConfig;

    [Header("共享血量")]
    [Tooltip("共享血量敌人之间的连接特效 Prefab")]
    public GameObject sharedHealthChainPrefab;
    [Tooltip("铁链缩放")]
    public Vector3 chainScale = Vector3.one;
    [Tooltip("铁链Y轴偏移")]
    public float chainYOffset = 0f;

    // 所有存活敌人的列表（用于遍历）
    private List<Enemy> allAliveEnemies = new List<Enemy>();

    // 活跃的共享血量组
    private List<SharedHealthGroup> activeGroups = new List<SharedHealthGroup>();

    // 事件
    public System.Action<Enemy> OnAnyEnemyDied;
    public System.Action OnAllEnemiesDied;
    public System.Action<Enemy> OnBossEngaged;
    /// <summary>新敌人注册时触发（用于外部系统订阅敌人事件）</summary>
    public System.Action<Enemy> OnEnemyRegistered;

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

        // Boss: 订阅分阶段推进事件 + 初始化分阶段推进
        if (enemy.isBoss)
        {
            enemy.OnBossEngaged += HandleBossEngaged;
            enemy.StartBossPhaseAdvance();
        }

        // 添加到列管理
        columnManager?.AddEnemyToColumn(enemy.columnIndex, enemy);

        // 通知外部系统有新敌人
        OnEnemyRegistered?.Invoke(enemy);
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

        enemy.OnDeath -= OnEnemyDied;
        enemy.OnBossEngaged -= HandleBossEngaged;

        // Boss: 推迟存活列表移除，等锦囊三选一完成后再触发波次结束检测
        // 非Boss: 立即移除并检测
        if (!enemy.isBoss)
        {
            allAliveEnemies.Remove(enemy);
        }

        // 从列中移除
        // Boss: 跳过补齐链，等三选一完成后再触发补齐移动（由StageController处理）
        if (enemy.isBoss)
            columnManager?.RemoveEnemyFromColumn(enemy.columnIndex, enemy, skipChain: true);
        else
            columnManager?.RemoveEnemyFromColumn(enemy.columnIndex, enemy);

        // 触发事件（必须在 ReturnEnemy 之前）
        OnAnyEnemyDied?.Invoke(enemy);

        // 击杀充能：按 HeroConfig 的普通敌人/Boss数值结算。
        UltimateSystem.Instance?.AddEnergyForKill(enemy.isBoss);

        // 经验值流转：击杀 → 生成经验宝石飞向经验条
        if (ExpGemManager.Instance != null)
        {
            float expMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetExpMultiplier() : 1f;
            ExpGemManager.Instance.SpawnGem(enemy.transform.position, enemy.expReward * expMult, enemy.gemSprite);
        }
        else
        {
            float expMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetExpMultiplier() : 1f;
            PlayerState.Instance?.AddExp(enemy.expReward * expMult); // 回退：直接加经验
        }

        // 道具掉落判定
        TryDropItem(enemy);

        // 注意：不再在此处调用 EnemyPool.Instance?.ReturnEnemy(enemy)
        // 对象回收由 Enemy.DeathBounceAndFall() 协程在死亡动画播完后处理

        // 检查是否所有敌人都死亡（Boss 尚未移除，此检查对 Boss 波次无效）
        if (allAliveEnemies.Count == 0)
        {
            OnAllEnemiesDied?.Invoke();
        }

        // Boss死亡 → 等待死亡动画播完 → 掉落锦囊（触发物品三选一）
        if (enemy.isBoss && UpgradeChoiceManager.Instance != null)
        {
            enemy.OnDeathAnimComplete += HandleBossDeathAnimComplete;
        }
    }

    /// <summary>击杀掉落道具判定</summary>
    private void TryDropItem(Enemy enemy)
    {
        if (dropItemPoolConfig == null || dropItemPoolConfig.pool.Count == 0) return;
        if (ItemInventory.Instance == null) return;

        float dropChance = dropItemPoolConfig.baseDropChance;
        var uem = UpgradeEffectManager.Instance;
        if (uem != null) dropChance *= (1f + uem.GetItemDropRateBonus());

        if (Random.value >= dropChance) return;

        // 加权随机抽取
        float totalWeight = 0f;
        for (int i = 0; i < dropItemPoolConfig.pool.Count; i++)
            totalWeight += dropItemPoolConfig.pool[i].weight;
        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        UpgradeDefinition picked = null;
        for (int i = 0; i < dropItemPoolConfig.pool.Count; i++)
        {
            cumulative += dropItemPoolConfig.pool[i].weight;
            if (roll <= cumulative) { picked = dropItemPoolConfig.pool[i].item; break; }
        }
        if (picked == null) return;

        // 尝试添加，满则弹出弃置弹窗
        if (ItemInventory.Instance.CanAdd(picked))
        {
            ItemInventory.Instance.AddItem(picked);
        }
        else
        {
            var entries = new List<ItemInventory.ItemEntry>(ItemInventory.Instance.Entries);
            ItemDiscardPopup.Show(entries, picked, discardIndex =>
            {
                if (discardIndex >= 0)
                {
                    ItemInventory.Instance.DiscardEntry(discardIndex);
                    ItemInventory.Instance.AddItem(picked);
                }
            });
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
        PlayerState.Instance?.TakeDamage(damage, enemy);
    }

    #endregion

    /// <summary>
    /// Boss 到达应战排：通知 BattleHUD 创建血条
    /// </summary>
    private void HandleBossEngaged(Enemy boss)
    {
        OnBossEngaged?.Invoke(boss);
    }

    /// <summary>
    /// Boss死亡动画播放完毕 → 掉落锦囊（触发物品三选一）
    /// </summary>
    private void HandleBossDeathAnimComplete(Enemy boss)
    {
        boss.OnDeathAnimComplete -= HandleBossDeathAnimComplete;

        // 推迟的存活列表移除：此时锦囊即将弹出，再触发波次结束检测
        allAliveEnemies.Remove(boss);

        // 清理残留特效，避免三选一弹窗时仍有攻击波/跳字
        WaveSpawner.Instance?.CleanupLingeringEffects();

        if (UpgradeChoiceManager.Instance != null)
            UpgradeChoiceManager.Instance.TriggerItemChoice();

        // 锦囊入队后检查波次是否全清（若全清且无后续波次，在 TriggerItemChoice 中已判通关拦截）
        if (allAliveEnemies.Count == 0)
        {
            OnAllEnemiesDied?.Invoke();
        }
    }

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

    #region 共享血量组

    public void RegisterGroup(SharedHealthGroup group)
    {
        if (group != null && !activeGroups.Contains(group))
            activeGroups.Add(group);
    }

    public void RemoveGroup(SharedHealthGroup group)
    {
        if (group != null)
            activeGroups.Remove(group);
    }

    private void LateUpdate()
    {
        // 更新铁链位置
        for (int i = activeGroups.Count - 1; i >= 0; i--)
        {
            var g = activeGroups[i];
            if (g == null || g.members.Count < 2)
            {
                activeGroups.RemoveAt(i);
                continue;
            }
            g.UpdateAllChainPositions();
        }
    }

    #endregion

    #region 清理

    /// <summary>
    /// 清除所有敌人（关卡切换时调用）
    /// </summary>
    public void ClearAllEnemies()
    {
        // 清理所有共享血量组
        var groupsToClear = new List<SharedHealthGroup>(activeGroups);
        foreach (var g in groupsToClear)
        {
            if (g != null)
            {
                // 清除成员的组引用，避免 Disband/KillAll 内部重复清理
                foreach (var m in g.members)
                {
                    if (m != null) m.sharedHealthGroup = null;
                }
                g.members.Clear();
                // 销毁铁链
                foreach (var chain in g.chainObjects)
                {
                    if (chain != null) Destroy(chain);
                }
                g.chainObjects.Clear();
            }
        }
        activeGroups.Clear();

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
