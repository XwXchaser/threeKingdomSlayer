using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一枚飞向收集点的金币图标（屏幕空间 UI）。
///
/// 轨迹：起点 → 贝塞尔控制点（带一点抛物线高度）→ 目标点；
/// 起飞有随机散布（像一把撒出去的铜钱），尾段淡出。
/// 用 unscaled 计时：暂停时也该收尾（否则暂停中金币会僵在半空）。
/// </summary>
public sealed class SupplyCoinIcon : MonoBehaviour
{
    /// <summary>起飞点（屏幕坐标）。</summary>
    public Vector3 startPosition;

    /// <summary>目标点（屏幕坐标）。</summary>
    public Vector3 targetPosition;

    /// <summary>贝塞尔控制点相对起点的偏移。</summary>
    public Vector2 arcOffset;

    /// <summary>单枚飞行时长（秒）。</summary>
    public float duration = 0.32f;

    /// <summary>起飞延迟（秒）——多枚错峰，形成「一把撒出去」的观感。</summary>
    public float delay = 0f;

    /// <summary>到达时的回调。</summary>
    public System.Action onArrive;

    private RectTransform _rect;
    private Image _image;
    private float _elapsed;
    private bool _done;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    private void Update()
    {
        if (_done) return;
        _elapsed += Time.unscaledDeltaTime;
        float t = (_elapsed - delay) / Mathf.Max(0.01f, duration);
        if (_image != null)
        {
            var c = _image.color;
            // 等待起飞期间不可见（否则会堆在画布中心）
            c.a = t < 0f ? 0f : Mathf.Clamp01((1f - t) / 0.4f);
            _image.color = c;
        }
        if (t < 0f) return;
        if (t >= 1f)
        {
            Arrive();
            return;
        }

        Vector3 control = startPosition + new Vector3(arcOffset.x, arcOffset.y, 0f);
        Vector3 a = Vector3.Lerp(startPosition, control, t);
        Vector3 b = Vector3.Lerp(control, targetPosition, t);
        Vector3 p = Vector3.Lerp(a, b, t);
        if (_rect != null) _rect.position = p;
    }

    private void Arrive()
    {
        _done = true;
        if (_image != null)
        {
            var c = _image.color;
            c.a = 0f;
            _image.color = c;
        }
        onArrive?.Invoke();
        Destroy(gameObject);
    }
}
