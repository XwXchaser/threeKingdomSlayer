using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 铜钱计数器UI — 显示本局获得铜钱总数 + 获得时飘字
/// 结构：一个精灵图片 + 两个数字（总数 + 飘字）
/// </summary>
public class CoinCounterUI : MonoBehaviour
{
    [Header("组件")]
    public UnityEngine.UI.Image coinIcon;
    public TMP_Text totalText;
    [Tooltip("空GameObject，控制飘字起始位置")]
    public Transform floatTextAnchor;

    [Header("飘字样式")]
    [SerializeField] private Color floatTextColor = new Color(1f, 0.85f, 0.2f); // 金色
    [SerializeField] private float floatTextFontSize = 28f;
    [SerializeField] private float floatUpDistance = 60f;
    [SerializeField] private float floatDuration = 0.7f;

    [Header("CoinIcon 跳动效果")]
    [SerializeField] private float iconPunchScale = 0.25f;
    [SerializeField] private float iconPunchDuration = 0.3f;

    [Header("TotalText 跳动效果")]
    [SerializeField] private float totalPunchScale = 0.25f;
    [SerializeField] private float totalPunchDuration = 0.3f;

    private RectTransform iconRect;
    private RectTransform totalTextRect;

    private void Start()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnCoinGained += OnCoinGained;

        if (coinIcon != null)
            iconRect = coinIcon.GetComponent<RectTransform>();
        if (totalText != null)
            totalTextRect = totalText.GetComponent<RectTransform>();

        // 初始显示（本局铜钱从0开始）
        if (totalText != null)
            totalText.text = (PlayerState.Instance != null ? PlayerState.Instance.coinCount : 0).ToString();
    }

    private void OnDestroy()
    {
        if (PlayerState.Instance != null)
            PlayerState.Instance.OnCoinGained -= OnCoinGained;
    }

    private void OnCoinGained(int amount, int total)
    {
        // 更新总数显示（total 即为本局铜钱总数）
        if (totalText != null)
            totalText.text = total.ToString();

        // Icon 跳动
        if (iconRect != null)
        {
            iconRect.DOKill();
            iconRect.localScale = Vector3.one;
            iconRect.DOPunchScale(Vector3.one * iconPunchScale, iconPunchDuration, 2, 0.5f);
        }

        // 总数跳动
        if (totalTextRect != null)
        {
            totalTextRect.DOKill();
            totalTextRect.localScale = Vector3.one;
            totalTextRect.DOPunchScale(Vector3.one * totalPunchScale, totalPunchDuration, 2, 0.5f);
        }

        SpawnFloatText(amount);
    }

    private void SpawnFloatText(int amount)
    {
        if (totalText == null) return;

        var go = new GameObject("CoinFloat", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = $"+{amount}";
        text.fontSize = floatTextFontSize;
        text.color = floatTextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        if (totalText.font != null)
            text.font = totalText.font;

        var rect = go.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100f, 40f);

        // 用 floatTextAnchor 定位，未设置则 fallback 到 totalText 右上
        if (floatTextAnchor != null)
            rect.localPosition = floatTextAnchor.localPosition;
        else if (totalTextRect != null)
            rect.localPosition = totalTextRect.localPosition + new Vector3(30f, 25f, 0f);

        var startPos = rect.localPosition;
        var seq = DOTween.Sequence();
        seq.Join(rect.DOLocalMove(startPos + new Vector3(0f, floatUpDistance, 0f), floatDuration).SetEase(Ease.OutQuad));
        seq.Join(text.DOFade(0f, floatDuration).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(go));
    }
}
