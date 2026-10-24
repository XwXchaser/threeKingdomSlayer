using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 补给物件的「金币爆散」表现：从物件位置撒出多枚金币图标，收敛到金币计数器后**才真正入账**。
///
/// 手感参照经验宝石（<c>ExpGem</c> / <c>ExpGemManager</c>）的掉落收集：
///   物件碎 → 一把铜钱飞出去 → 落进计数器 → 数字跳动 + 飘字。
///
/// 关键约束：**不能吞奖励**——播不了（没有画布/目标/相机）时立刻发奖；
/// 另有兜底计时器，动画异常时也会在超时后发奖。
/// </summary>
public sealed class SupplyCoinBurst : MonoBehaviour
{
    [Header("图标")]
    [Tooltip("金币图标（留空则用 HUD 里战斗金币显示的图标）")]
    public Sprite coinSprite;

    [Tooltip("图标父容器（留空则用 HUD 画布）")]
    public RectTransform iconParent;

    [Header("表现")]
    [Tooltip("金币图标数量")]
    public int iconCount = 12;

    [Tooltip("全部图标播完的总时长（秒）")]
    public float totalDuration = 0.6f;

    [Tooltip("单枚图标的飞行时长（秒）")]
    public float flightDuration = 0.32f;

    [Tooltip("起飞位置的随机散布（像素）")]
    public float startSpread = 46f;

    [Tooltip("抛物线高度（像素）")]
    public float arcHeight = 110f;

    [Tooltip("图标尺寸（像素）")]
    public float iconSize = 40f;

    [Tooltip("左右散布的额外随机（像素）")]
    public float arcSpreadX = 40f;

    private int _pending;
    private System.Action _pendingGrant;
    private float _safetyTimer;

    private void OnEnable()
    {
        SupplyRewardPort.RewardGranted += OnRewardGranted;
    }

    private void OnDisable()
    {
        SupplyRewardPort.RewardGranted -= OnRewardGranted;
        // 退场时把没发出去的奖补上，别让奖励卡住
        Grant();
    }

    private void OnRewardGranted(SupplyRewardInfo info)
    {
        Play(info.worldPosition, info.grant);
    }

    /// <summary>播一次金币爆散；播完（或播不了）后调用 onCollected 发奖。</summary>
    public void Play(Vector3 worldPosition, System.Action onCollected)
    {
        var parent = ResolveParent();
        var target = ResolveTarget();
        var sprite = ResolveSprite();
        var cam = Camera.main;
        Vector3 start = cam != null ? cam.WorldToScreenPoint(worldPosition) : Vector3.zero;

        if (parent == null || sprite == null || cam == null || start.z <= 0f || target == Vector3.zero)
        {
            onCollected?.Invoke();
            return;
        }

        int count = Mathf.Max(1, iconCount);
        _pending = count;
        _pendingGrant = onCollected;
        _safetyTimer = totalDuration + 0.5f;

        // 错峰：让最后一枚刚好在总时长内落地
        float stagger = count > 1 ? Mathf.Max(0f, totalDuration - flightDuration) / (count - 1) : 0f;
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("SupplyCoinIcon", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.color = Color.white;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(iconSize, iconSize);
            rect.pivot = new Vector2(0.5f, 0.5f);
            go.transform.SetAsLastSibling();

            Vector2 spread = Random.insideUnitCircle * startSpread;
            var icon = go.AddComponent<SupplyCoinIcon>();
            icon.startPosition = start + new Vector3(spread.x, spread.y, 0f);
            icon.targetPosition = target;
            icon.duration = flightDuration;
            icon.delay = i * stagger;
            icon.arcOffset = new Vector2(Random.Range(-arcSpreadX, arcSpreadX), arcHeight);
            icon.onArrive = OnIconArrived;
            // 先摆到起点：错峰等待期间不要停在画布中心
            rect.position = icon.startPosition;
            var c = img.color;
            c.a = 0f;
            img.color = c;
        }
    }

    private void OnIconArrived()
    {
        _pending--;
        if (_pending > 0) return;
        Grant();
    }

    private void Update()
    {
        if (_pendingGrant == null) return;
        _safetyTimer -= Time.unscaledDeltaTime;
        if (_safetyTimer <= 0f) Grant();
    }

    private void Grant()
    {
        var g = _pendingGrant;
        _pendingGrant = null;
        _pending = 0;
        g?.Invoke();
    }

    private RectTransform ResolveParent()
    {
        if (iconParent != null) return iconParent;
        var counter = FindObjectOfType<CoinCounterUI>();
        if (counter != null) return counter.transform.parent as RectTransform;
        var canvas = GetComponentInParent<Canvas>();
        return canvas != null ? canvas.transform as RectTransform : null;
    }

    private Sprite ResolveSprite()
    {
        if (coinSprite != null) return coinSprite;
        var counter = FindObjectOfType<CoinCounterUI>();
        if (counter != null && counter.coinIcon != null) return counter.coinIcon.sprite;
        var hub = FindObjectOfType<HubCoinDisplay>();
        if (hub != null && hub.iconSprite != null) return hub.iconSprite;
        return null;
    }

    private Vector3 ResolveTarget()
    {
        var counter = FindObjectOfType<CoinCounterUI>();
        if (counter != null && counter.coinIcon != null)
            return counter.coinIcon.GetComponent<RectTransform>().position;
        var hub = FindObjectOfType<HubCoinDisplay>();
        if (hub != null)
        {
            foreach (var img in hub.GetComponentsInChildren<Image>(true))
                return img.GetComponent<RectTransform>().position;
        }
        return Vector3.zero;
    }
}
