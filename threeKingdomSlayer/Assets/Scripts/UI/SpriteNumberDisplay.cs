using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右上角精灵数字化显示 — 替代 TMP 文本，用数字精灵水平排列。
/// 配合 HorizontalLayoutGroup + ContentSizeFitter 自动布局。
/// </summary>
public class SpriteNumberDisplay : MonoBehaviour
{
    [Header("数字精灵（索引 0-9）")]
    [SerializeField] private Sprite[] _digitSprites = new Sprite[10];

    [Header("符号精灵")]
    [SerializeField] private Sprite _percentSprite;
    [SerializeField] private Sprite _plusSprite;

    [Header("显示比例（整体缩放）")]
    [SerializeField] private float _displayScale = 1f;

    [Header("数字固定尺寸（宽, 高）")]
    [SerializeField] private Vector2 _digitSize = new Vector2(16f, 20f);

    [Header("数字间距")]
    [SerializeField] private float _spacing = 1f;

    private readonly List<Image> _digitPool = new List<Image>();
    private int _activeCount;

    private void Awake()
    {
        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ApplyConfig(layout);
    }

    private void ApplyConfig(HorizontalLayoutGroup layout)
    {
        layout.spacing = _spacing;
        transform.localScale = new Vector3(_displayScale, _displayScale, 1f);
    }

    private void OnValidate()
    {
        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout != null) ApplyConfig(layout);
    }

    /// <summary>显示百分比：value + %符号</summary>
    public void ShowPercent(int value)
    {
        var digits = GetDigits(value);
        int needed = digits.Count + 1;
        EnsurePool(needed);
        for (int i = 0; i < digits.Count; i++)
            SetDigit(i, digits[i]);
        SetPercentSymbol(digits.Count);
        _activeCount = needed;
        HideExcess();
    }

    /// <summary>显示带正号的百分比：+value%</summary>
    public void ShowSignedPercent(int value)
    {
        var digits = GetDigits(Mathf.Abs(value));
        int needed = digits.Count + 2;
        EnsurePool(needed);
        SetPlusSymbol(0);
        for (int i = 0; i < digits.Count; i++)
            SetDigit(i + 1, digits[i]);
        SetPercentSymbol(digits.Count + 1);
        _activeCount = needed;
        HideExcess();
    }

    /// <summary>显示倒计时（整数秒，最小值 0）</summary>
    public void ShowCountdown(int seconds)
    {
        var digits = GetDigits(Mathf.Max(0, seconds));
        EnsurePool(digits.Count);
        for (int i = 0; i < digits.Count; i++)
            SetDigit(i, digits[i]);
        _activeCount = digits.Count;
        HideExcess();
    }

    /// <summary>显示纯数字</summary>
    public void ShowNumber(int value)
    {
        var digits = GetDigits(value);
        EnsurePool(digits.Count);
        for (int i = 0; i < digits.Count; i++)
            SetDigit(i, digits[i]);
        _activeCount = digits.Count;
        HideExcess();
    }

    /// <summary>清除所有数字显示</summary>
    public void Clear()
    {
        for (int i = 0; i < _digitPool.Count; i++)
            _digitPool[i].gameObject.SetActive(false);
        _activeCount = 0;
    }

    private void EnsurePool(int needed)
    {
        while (_digitPool.Count < needed)
            CreateDigit();
    }

    private void CreateDigit()
    {
        var go = new GameObject("Digit", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(transform, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        var le = go.GetComponent<LayoutElement>();
        le.preferredWidth = _digitSize.x;
        le.preferredHeight = _digitSize.y;
        _digitPool.Add(img);
    }

    private void SetDigit(int index, int digit)
    {
        if (digit >= 0 && digit <= 9 && _digitSprites[digit] != null)
            _digitPool[index].sprite = _digitSprites[digit];
        _digitPool[index].gameObject.SetActive(true);
    }

    private void SetPlusSymbol(int index)
    {
        if (_plusSprite != null)
            _digitPool[index].sprite = _plusSprite;
        _digitPool[index].gameObject.SetActive(true);
    }

    private void SetPercentSymbol(int index)
    {
        if (_percentSprite != null)
            _digitPool[index].sprite = _percentSprite;
        _digitPool[index].gameObject.SetActive(true);
    }

    private void HideExcess()
    {
        for (int i = _activeCount; i < _digitPool.Count; i++)
            _digitPool[i].gameObject.SetActive(false);
    }

    private static List<int> GetDigits(int value)
    {
        var list = new List<int>();
        if (value <= 0) { list.Add(0); return list; }
        while (value > 0) { list.Insert(0, value % 10); value /= 10; }
        return list;
    }
}
