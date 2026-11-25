using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **装备宝箱管理器**（2026-11-17）。
///
/// 掉落链：精英 / Boss 死亡 → 这里生成一个宝箱 → 宝箱从敌人位置飞向玩家 → 拾取 →
/// 入队一次「装备三选一」（**波末统一弹**，与升级三选一同一队列）。
///
/// 为什么要有这个中间物件（而不是直接给卡）：
/// 用户要求「精英敌人才掉**宝箱**，再拾取」—— 战场上要**看得见**战利品从哪来。
/// 玩家位置固定（不打横向位移），所以宝箱**主动飞向玩家**（与经验宝石同款交互）。
/// </summary>
public class EquipmentChestManager : MonoBehaviour
{
    public static EquipmentChestManager Instance { get; private set; }

    [Header("宝箱预制体")]
    public GameObject chestPrefab;

    [Header("父容器（运行时由 BattleHUD 设置，宝箱生成为其子对象）")]
    public RectTransform chestParent;

    [Header("收集点：玩家（留空则回落到屏幕中心下方）")]
    public Transform playerCollectPoint;

    [Header("飞行参数")]
    [Tooltip("屏幕空间飞行速度（像素/秒，参考分辨率 1080x1920）")]
    public float flySpeed = 900f;

    [Tooltip("宝箱显示尺寸（像素）")]
    public float chestSize = 110f;

    private readonly List<EquipmentChest> _flyingChests = new List<EquipmentChest>();

    /// <summary>
    /// 是否还有**敌人掉落的装备箱**在飞（2026-11-25）。
    /// 波末结算要等它们到位并入队；**场景破坏物不算**（玩家可打可不打，不能阻塞波末）。
    /// 查询时顺手剔除已被销毁 / 场景清掉的箱子，避免永久等待。
    /// </summary>
    public bool HasFlyingChests
    {
        get
        {
            PruneDestroyed();
            return _flyingChests.Count > 0;
        }
    }

    /// <summary>箱子被外部销毁 / 未拾取时从等待列表剔除（不发放奖励）。</summary>
    public void ForgetChest(EquipmentChest chest)
    {
        if (chest != null) _flyingChests.Remove(chest);
    }

    private void PruneDestroyed()
    {
        for (int i = _flyingChests.Count - 1; i >= 0; i--)
            if (_flyingChests[i] == null) _flyingChests.RemoveAt(i);
    }

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
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 在敌人死亡的世界坐标处掉一个宝箱。
    /// </summary>
    public void SpawnChest(Vector3 worldPos)
    {
        if (chestPrefab == null)
        {
            Debug.LogWarning("[EquipmentChestManager] chestPrefab 未配置 → 直接给一次装备三选一（兜底）");
            UpgradeChoiceManager.Instance?.GrantEquipmentChoices(1);
            return;
        }

        RectTransform parent = chestParent;
        if (parent == null)
        {
            // 没有 HUD 容器 → 退而求其次：找个 Canvas
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null) parent = canvas.transform as RectTransform;
        }
        if (parent == null)
        {
            Debug.LogWarning("[EquipmentChestManager] 找不到父容器 → 直接给一次装备三选一（兜底）");
            UpgradeChoiceManager.Instance?.GrantEquipmentChoices(1);
            return;
        }

        var go = Instantiate(chestPrefab, parent);
        var chest = go.GetComponent<EquipmentChest>();
        if (chest == null) chest = go.AddComponent<EquipmentChest>();

        // 掉落时盖章：抽档用这个圈数，而不是选择窗口弹出时的当前圈数。
        chest.circleAtDrop = UpgradeChoiceManager.Instance != null
            ? UpgradeChoiceManager.Instance.CurrentCircleForDrop
            : 1;

        go.transform.SetAsLastSibling();

        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(chestSize, chestSize);
            if (Camera.main != null) rt.position = Camera.main.WorldToScreenPoint(worldPos);
        }

        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        chest.speed = flySpeed;
        chest.targetPosition = GetCollectScreenPosition();
        _flyingChests.Add(chest);
    }

    /// <summary>收集点（屏幕坐标）：玩家的位置 → 没有就取屏幕中下方。</summary>
    private Vector3 GetCollectScreenPosition()
    {
        if (playerCollectPoint != null && Camera.main != null)
            return Camera.main.WorldToScreenPoint(playerCollectPoint.position);

        // 屏幕中下方（1080x1920 参考分辨率）
        return new Vector3(Screen.width * 0.5f, Screen.height * 0.32f, 0f);
    }

    /// <summary>宝箱到达玩家 → 拾取 → 入队一次装备三选一（波末弹）。</summary>
    public void OnChestArrived(EquipmentChest chest)
    {
        if (chest != null) _flyingChests.Remove(chest);
        int circle = chest != null ? chest.circleAtDrop : 1;
        UpgradeChoiceManager.Instance?.GrantEquipmentChoices(1, circle);
    }

    /// <summary>清除所有飞行中的宝箱（新对局重置）。</summary>
    public void ClearAll()
    {
        for (int i = _flyingChests.Count - 1; i >= 0; i--)
        {
            if (_flyingChests[i] != null) Destroy(_flyingChests[i].gameObject);
        }
        _flyingChests.Clear();
    }
}
