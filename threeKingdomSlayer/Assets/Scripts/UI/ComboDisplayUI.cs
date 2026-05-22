using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 连击特效显示 — 两张精灵图：填充倒计时图 + 静态图，命中时缩放动画
/// </summary>
public class ComboDisplayUI : MonoBehaviour
{
    [Header("图片")]
    [Tooltip("填充图（Image.Type=Filled, FillMethod=Horizontal, FillOrigin=Left）")]
    public Image fillImage;
    [Tooltip("静态图（普通 Image）")]
    public Image staticImage;

    [Header("缩放动画")]
    [Tooltip("缩放峰值（1=原大小, 1.3=放大30%）")]
    public float scaleAmplitude = 1.3f;
    [Tooltip("缩放动画时长（秒）")]
    public float scaleDuration = 0.15f;

    private Coroutine _scaleRoutine;
    private int _lastCombo;
    private Vector3 _baseScale;
    private Color _fillColor, _staticColor;

    private void Awake()
    {
        _baseScale = transform.localScale;

        if (fillImage != null)
        {
            _fillColor = fillImage.color;
            _fillColor.a = 0f;
            fillImage.color = _fillColor;
        }
        if (staticImage != null)
        {
            _staticColor = staticImage.color;
            _staticColor.a = 0f;
            staticImage.color = _staticColor;
        }
    }

    private void Start()
    {
        if (ComboManager.Instance != null)
            ComboManager.Instance.OnComboUpdated += OnComboUpdated;
    }

    private void OnDestroy()
    {
        if (ComboManager.Instance != null)
            ComboManager.Instance.OnComboUpdated -= OnComboUpdated;
    }

    private void Update()
    {
        var cm = ComboManager.Instance;
        if (cm == null) return;

        if (cm.CurrentCombo > 0 && fillImage != null)
            fillImage.fillAmount = cm.ComboResetProgress;
    }

    private void OnComboUpdated(int combo)
    {
        if (combo > 0)
        {
            if (combo > _lastCombo)
            {
                SetVisible(true);

                if (fillImage != null)
                    fillImage.fillAmount = 1f;

                PlayScaleAnimation();
            }
        }
        else
        {
            SetVisible(false);
        }

        _lastCombo = combo;
    }

    private void SetVisible(bool visible)
    {
        float a = visible ? 1f : 0f;
        if (fillImage != null)
        {
            var c = fillImage.color;
            c.a = a;
            fillImage.color = c;
        }
        if (staticImage != null)
        {
            var c = staticImage.color;
            c.a = a;
            staticImage.color = c;
        }
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
