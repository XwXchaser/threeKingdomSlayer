using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 连击特效显示 — "连"字 + 动态数字，统一填充 + 命中缩放动画
/// </summary>
public class ComboDisplayUI : MonoBehaviour
{
    [Header("连字")]
    public Image lianFillImage;
    public Image lianStaticImage;

    [Header("数字素材")]
    [Tooltip("0-9 填充面 Sprite")]
    public Sprite[] digitFillSprites;
    [Tooltip("0-9 底面 Sprite（alpha 后缀）")]
    public Sprite[] digitAlphaSprites;

    [Header("数字预制体")]
    [Tooltip("DigitSlot 预制体，含 FillImage + StaticImage 子节点")]
    public GameObject digitPrefab;
    [Tooltip("数字的父容器（水平排列）")]
    public Transform digitParent;

    [Header("缩放动画")]
    public float scaleAmplitude = 1.3f;
    public float scaleDuration = 0.15f;

    [Header("间距适配")]
    [Tooltip("参考分辨率宽度")]
    [SerializeField] private float _referenceWidth = 1080f;
    [Tooltip("'连'字与第一位数字在参考分辨率下的间距（px）")]
    [SerializeField] private float _referenceGap = 41f;

    private List<DigitSlot> _digitPool = new List<DigitSlot>();
    private int _lastCombo;
    private bool _visible;
    private Coroutine _scaleRoutine;
    private Vector3 _baseScale;
    private RectTransform _lianRect;
    private RectTransform _digitParentRect;
    private RectTransform _parentRect;
    private float _lastParentWidth;

    private class DigitSlot
    {
        public GameObject go;
        public Image fillImage;
        public Image staticImage;
    }

    private void Awake()
    {
        _baseScale = transform.localScale;
        _parentRect = transform as RectTransform;
        _lianRect = (lianStaticImage != null ? lianStaticImage : lianFillImage)?.GetComponent<RectTransform>();
        _digitParentRect = digitParent as RectTransform;
        SetVisible(false);
    }

    private void Start()
    {
        if (ComboManager.Instance != null)
            ComboManager.Instance.OnComboUpdated += OnComboUpdated;
        AdjustSpacing();
    }

    private void OnDestroy()
    {
        if (ComboManager.Instance != null)
            ComboManager.Instance.OnComboUpdated -= OnComboUpdated;
    }

    private void Update()
    {
        // 分辨率变化时重新调整间距
        if (_parentRect != null && _parentRect.rect.width != _lastParentWidth)
            AdjustSpacing();

        var cm = ComboManager.Instance;
        if (cm == null || !_visible) return;

        ApplyFillAmounts(cm.ComboResetProgress);
    }

    private void OnComboUpdated(int combo)
    {
        if (combo > 0)
        {
            if (combo > _lastCombo)
            {
                RebuildDigits(combo);
                SetVisible(true);
                ApplyFillAmounts(1f);
                PlayScaleAnimation();
            }
        }
        else
        {
            SetVisible(false);
        }

        _lastCombo = combo;
    }

    private void RebuildDigits(int combo)
    {
        var digits = GetDigits(combo);
        int needed = digits.Count;

        while (_digitPool.Count < needed)
        {
            var go = Instantiate(digitPrefab, digitParent);
            var slot = new DigitSlot
            {
                go = go,
                staticImage = go.transform.Find("StaticImage")?.GetComponent<Image>(),
                fillImage = go.transform.Find("FillImage")?.GetComponent<Image>()
            };
            if (slot.fillImage != null)
            {
                slot.fillImage.type = Image.Type.Filled;
                slot.fillImage.fillMethod = Image.FillMethod.Horizontal;
                slot.fillImage.fillOrigin = 0;
            }
            _digitPool.Add(slot);
        }

        for (int i = 0; i < _digitPool.Count; i++)
        {
            var slot = _digitPool[i];
            bool active = i < needed;
            slot.go.SetActive(active);
            if (active)
            {
                int d = digits[i];
                if (slot.fillImage != null && d < digitFillSprites.Length)
                    slot.fillImage.sprite = digitFillSprites[d];
                if (slot.staticImage != null && d < digitAlphaSprites.Length)
                    slot.staticImage.sprite = digitAlphaSprites[d];
            }
        }

        // 强制重建布局，确保 DigitParent 的 ContentSizeFitter 即时生效
        LayoutRebuilder.ForceRebuildLayoutImmediate(digitParent as RectTransform);
    }

    private void ApplyFillAmounts(float progress)
    {
        int digitCount = 0;
        for (int i = 0; i < _digitPool.Count; i++)
            if (_digitPool[i].go.activeSelf)
                digitCount++;

        int N = 1 + digitCount;

        if (lianFillImage != null)
            lianFillImage.fillAmount = Mathf.Clamp01(N * progress - 0);

        int di = 0;
        for (int i = 0; i < _digitPool.Count; i++)
        {
            if (!_digitPool[i].go.activeSelf) continue;
            if (_digitPool[i].fillImage != null)
                _digitPool[i].fillImage.fillAmount = Mathf.Clamp01(N * progress - (di + 1));
            di++;
        }
    }

    private void SetVisible(bool visible)
    {
        _visible = visible;
        float a = visible ? 1f : 0f;
        SetImageAlpha(lianFillImage, a);
        SetImageAlpha(lianStaticImage, a);
        foreach (var slot in _digitPool)
        {
            if (slot.go.activeSelf)
            {
                SetImageAlpha(slot.fillImage, a);
                SetImageAlpha(slot.staticImage, a);
            }
        }
    }

    private static void SetImageAlpha(Image img, float a)
    {
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    private static List<int> GetDigits(int value)
    {
        var list = new List<int>();
        if (value == 0)
        {
            list.Add(0);
            return list;
        }
        while (value > 0)
        {
            list.Insert(0, value % 10);
            value /= 10;
        }
        return list;
    }

    /// <summary>
    /// 按当前 Canvas 宽度缩放 "连"字与数字之间的间距，保证不同屏幕比例下视觉一致。
    /// </summary>
    private void AdjustSpacing()
    {
        if (_lianRect == null || _digitParentRect == null || _parentRect == null) return;

        float parentWidth = _parentRect.rect.width;
        _lastParentWidth = parentWidth;

        float ratio = parentWidth / _referenceWidth;
        float targetGap = _referenceGap * ratio;

        // "连"字右边缘（锚定左上角，x 相对于左边缘）
        float lianRightEdge = _lianRect.anchoredPosition.x
            + _lianRect.sizeDelta.x * (1f - _lianRect.pivot.x);

        // DigitParent 锚定中心 (0.5, 0.5)，pivot (0, 0.5)
        // digitLeftEdge = parentWidth/2 + anchoredPosition.x
        // 目标: digitLeftEdge = lianRightEdge + targetGap
        float targetX = lianRightEdge + targetGap - parentWidth * 0.5f;

        var pos = _digitParentRect.anchoredPosition;
        pos.x = targetX;
        _digitParentRect.anchoredPosition = pos;
    }

    private void PlayScaleAnimation()
    {
        if (_scaleRoutine != null)
            StopCoroutine(_scaleRoutine);
        _scaleRoutine = StartCoroutine(ScaleRoutine());
    }

    private IEnumerator ScaleRoutine()
    {
        var t = transform;
        float half = scaleDuration * 0.5f;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / half;
            t.localScale = Vector3.Lerp(_baseScale, _baseScale * scaleAmplitude, p);
            yield return null;
        }
        t.localScale = _baseScale * scaleAmplitude;

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            float p = elapsed / half;
            t.localScale = Vector3.Lerp(_baseScale * scaleAmplitude, _baseScale, p);
            yield return null;
        }
        t.localScale = _baseScale;
    }
}
