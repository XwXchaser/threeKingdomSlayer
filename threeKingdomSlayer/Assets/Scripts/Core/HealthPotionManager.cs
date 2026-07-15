using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 血包掉落管理器 — 单例
///
/// 放回抽卡概率模型：基础掉率 + 每次击杀递增 → 掉落时重置为有效基础掉率。
/// 有效基础掉率 = baseDropRate + DropRateBonus（DropRateBonus 预留全局/局内成长）。
/// 掉落时生成飞行宝石，飞向 BuffDisplayPanel 的血包槽位。
/// </summary>
public class HealthPotionManager : MonoBehaviour
{
    public static HealthPotionManager Instance { get; private set; }

    [Header("掉落概率")]
    [Range(0f, 1f)]
    [Tooltip("基础掉率（%）")]
    public float baseDropRate = 0.05f;
    [Range(0f, 1f)]
    [Tooltip("每次击杀递增的掉率（%）")]
    public float dropRateIncrement = 0.02f;

    [Header("血包属性")]
    [Tooltip("最大持有数")]
    public int maxStack = 3;
    [Tooltip("每次回复生命值")]
    public float healAmount = 50f;

    [Header("UI 引用")]
    [Tooltip("血包 UpgradeDefinition（图标、显示名等）")]
    public UpgradeDefinition potionDefinition;
    [Tooltip("飞行宝石的父容器 Canvas RectTransform（由 BattleHUD 运行时注入）")]
    public RectTransform gemParent;
    [Tooltip("血包槽位的 RectTransform（由 BuffDisplayPanel 运行时注入，用于飞行目标位置和尺寸）")]
    public RectTransform targetSlot;
    [Tooltip("宝石飞行速度（屏幕空间像素/秒）")]
    public float gemSpeed = 1200f;

    // 预留：将来全局/局内成长影响基础掉率
    [System.NonSerialized] public float DropRateBonus;

    public float EffectiveBaseRate => baseDropRate + DropRateBonus;

    private float _currentDropRate;
    private int _potionCount;

    public int PotionCount => ItemInventory.Instance != null
        ? ItemInventory.Instance.GetRemainingUses(ItemInventory.HealthPotionGestureId)
        : 0;

    /// <summary>血包数量变化事件</summary>
    public System.Action<int> OnPotionCountChanged;

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
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAnyEnemyDied += OnEnemyDied;
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnAnyEnemyDied -= OnEnemyDied;
        if (Instance == this)
            Instance = null;
    }

    /// <summary>新对局重置：掉率回到有效基础值，血包数量清零</summary>
    public void ResetForNewStage()
    {
        _currentDropRate = EffectiveBaseRate;
        _potionCount = 0;
        OnPotionCountChanged?.Invoke(0);
    }

    /// <summary>尝试使用一个血包。成功返回 true。</summary>
    public bool TryUsePotion()
    {
        if (PotionCount <= 0) return false;
        if (PlayerState.Instance == null || ItemInventory.Instance == null) return false;
        if (!ItemInventory.Instance.TryConsume(ItemInventory.HealthPotionGestureId)) return false;

        _potionCount = PotionCount;
        OnPotionCountChanged?.Invoke(_potionCount);

        PlayerState.Instance.Heal(healAmount);
        return true;
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (enemy == null) return;
        if (PotionCount >= maxStack) return; // 已达上限，不投骰
        if (ItemInventory.Instance == null || !ItemInventory.Instance.CanAddPotion()) return;
        if (gemParent == null) return;

        float roll = Random.value;
        if (roll < _currentDropRate)
        {
            // 掉落成功：重置掉率到有效基础值
            _currentDropRate = EffectiveBaseRate;
            SpawnFlyingGem(enemy.transform.position);
        }
        else
        {
            // 未掉落：递增掉率
            _currentDropRate = Mathf.Min(_currentDropRate + dropRateIncrement, 1f);
        }
    }

    private void SpawnFlyingGem(Vector3 worldPos)
    {
        // 复用 ExpGem prefab，设置自定义回调
        var expGemManager = ExpGemManager.Instance;
        if (expGemManager == null || expGemManager.gemPrefab == null) return;

        var go = Instantiate(expGemManager.gemPrefab, gemParent);
        go.transform.SetAsLastSibling();

        var gem = go.GetComponent<ExpGem>();
        if (gem == null)
            gem = go.AddComponent<ExpGem>();

        // 设置宝石精灵为包子
        if (potionDefinition != null && potionDefinition.icon != null)
            gem.SetVisual(potionDefinition.icon, Color.white);

        // 初始位置：敌人世界坐标 → 屏幕坐标
        var rt = go.GetComponent<RectTransform>();
        if (rt != null && Camera.main != null)
            rt.position = Camera.main.WorldToScreenPoint(worldPos);

        // 匹配目标槽位尺寸
        if (targetSlot != null)
            rt.sizeDelta = targetSlot.sizeDelta;

        // 关闭射线检测
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        // 目标位置：槽位屏幕坐标
        Vector3 targetScreenPos;
        if (targetSlot != null)
            targetScreenPos = targetSlot.position;
        else
            targetScreenPos = rt.position + Vector3.up * 200f; // fallback

        gem.expAmount = 0f; // 不使用经验值
        gem.speed = gemSpeed;
        gem.targetPosition = targetScreenPos;
        gem.onArrived = OnFlyingGemArrived;
    }

    private void OnFlyingGemArrived(ExpGem gem)
    {
        if (gem != null)
            Destroy(gem.gameObject);

        if (ItemInventory.Instance != null && ItemInventory.Instance.AddPotion(potionDefinition, maxStack))
        {
            _potionCount = PotionCount;
            OnPotionCountChanged?.Invoke(_potionCount);
        }
    }
}
