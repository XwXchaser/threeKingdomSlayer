using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验宝石管理器 — 单例
///
/// 敌人死亡 → SpawnGem(世界坐标, 经验值)
/// 宝石飞向经验条收集点，到达后 → PlayerState.AddExp
/// 飞行中宝石越多，所有宝石飞行速度越快（防积压）。
/// </summary>
public class ExpGemManager : MonoBehaviour
{
    public static ExpGemManager Instance { get; private set; }

    [Header("宝石预制体")]
    public GameObject gemPrefab;

    [Header("父容器（运行时由BattleHUD设置，宝石生成为其子对象）")]
    public RectTransform gemParent;

    [Header("经验条（运行时由BattleHUD设置，宝石飞向其Fill右端）")]
    public Slider expSlider;

    [Header("备用收集点（世界坐标，expSlider 为空时使用）")]
    public Transform fallbackCollectPoint;

    [Header("飞行参数")]
    [Tooltip("屏幕空间基础速度（像素/秒，参考分辨率1080x1920）")]
    public float baseSpeed = 800f;
    [Tooltip("每多一颗飞行中的宝石，速度增加的倍率")]
    public float speedPerExtraGem = 0.15f;
    [Tooltip("最大速度倍率上限")]
    public float maxSpeedMultiplier = 5f;

    // 飞行中的宝石
    private List<ExpGem> _flyingGems = new List<ExpGem>();

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
        // 不再需要 _cachedCamera，屏幕空间 UI 不需要坐标转换
    }

    // Update 已移除：宝石目标在生成时锁定为 Fill 右端，不再每帧更新
    // 避免升级时 Fill 重置导致飞行中的宝石改变目标

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>获取经验条 Fill 右端的屏幕坐标（生成时锁定，不受后续 Fill 变化影响）</summary>
    private Vector3 GetFillEndScreenPosition()
    {
        if (expSlider != null && expSlider.fillRect != null)
        {
            Vector3[] corners = new Vector3[4];
            expSlider.fillRect.GetWorldCorners(corners);
            return (corners[2] + corners[3]) * 0.5f; // 右边缘中点，已是屏幕坐标
        }
        if (fallbackCollectPoint != null && Camera.main != null)
            return Camera.main.WorldToScreenPoint(fallbackCollectPoint.position);
        return Vector3.zero;
    }

    /// <summary>在指定世界坐标生成经验宝石，可覆盖精灵</summary>
    public void SpawnGem(Vector3 worldPos, float expAmount, Sprite overrideSprite = null)
    {
        if (gemPrefab == null || gemParent == null)
        {
            Debug.LogWarning("[ExpGemManager] gemPrefab 或 gemParent 未配置");
            return;
        }

        var go = Instantiate(gemPrefab, gemParent);
        var gem = go.GetComponent<ExpGem>();
        if (gem == null)
        {
            gem = go.AddComponent<ExpGem>();
        }

        // 确保宝石渲染在所有 UI 最上层
        go.transform.SetAsLastSibling();

        // 世界生成坐标 → 屏幕坐标
        var rt = go.GetComponent<RectTransform>();
        if (rt != null && Camera.main != null)
            rt.position = Camera.main.WorldToScreenPoint(worldPos);

        // 关闭射线检测，避免阻挡 UI 点击
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;

        gem.expAmount = expAmount;
        gem.targetPosition = GetFillEndScreenPosition(); // 屏幕坐标，生成时锁定

        if (overrideSprite != null)
        {
            gem.SetVisual(overrideSprite, Color.white);
        }

        _flyingGems.Add(gem);

        UpdateAllSpeeds();
    }

    /// <summary>宝石到达收集点</summary>
    public void OnGemArrived(ExpGem gem)
    {
        _flyingGems.Remove(gem);
        PlayerState.Instance?.AddExp(gem.expAmount);
        UpdateAllSpeeds();
    }

    /// <summary>根据飞行中宝石数量更新所有宝石速度</summary>
    private void UpdateAllSpeeds()
    {
        int count = _flyingGems.Count;
        float multiplier = Mathf.Min(1f + (count - 1) * speedPerExtraGem, maxSpeedMultiplier);
        // 至少 1 颗时不加速
        if (count <= 1) multiplier = 1f;

        float speed = baseSpeed * multiplier;
        for (int i = 0; i < _flyingGems.Count; i++)
        {
            if (_flyingGems[i] != null)
                _flyingGems[i].speed = speed;
        }
    }

    /// <summary>清除所有飞行中的宝石（新对局重置）</summary>
    public void ClearAll()
    {
        for (int i = _flyingGems.Count - 1; i >= 0; i--)
        {
            if (_flyingGems[i] != null)
                Destroy(_flyingGems[i].gameObject);
        }
        _flyingGems.Clear();
    }
}
