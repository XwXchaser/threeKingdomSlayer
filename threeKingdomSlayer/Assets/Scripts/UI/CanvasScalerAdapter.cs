using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 动态调整 CanvasScaler.matchWidthOrHeight，防止在长屏设备上 Canvas 有效宽度过度压缩。
///
/// 原理：
///   参考分辨率 1080x1920，Match=1（高度驱动）时，屏幕越"长"（H/W 越大），
///   Canvas 有效宽度越小，导致 UI 元素水平方向重叠。
///   此脚本在屏幕过长时自动降低 match 值，保证 Canvas 有效宽度不低于阈值。
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class CanvasScalerAdapter : MonoBehaviour
{
    [Tooltip("参考分辨率宽度")]
    [SerializeField] private float _referenceWidth = 1080f;
    [Tooltip("参考分辨率高度")]
    [SerializeField] private float _referenceHeight = 1920f;
    [Tooltip("Canvas 有效宽度最低比例（相对于参考宽度），0.9 表示不低于 1080*0.9=972")]
    [SerializeField, Range(0.5f, 1f)] private float _minWidthRatio = 0.9f;

    private CanvasScaler _scaler;
    private int _lastScreenW;
    private int _lastScreenH;

    private void Awake()
    {
        _scaler = GetComponent<CanvasScaler>();
        ApplyMatch();
    }

    private void Update()
    {
        // 检测分辨率变化（折叠屏、窗口缩放等）
        if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
            ApplyMatch();
    }

    private void ApplyMatch()
    {
        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;

        if (_scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        float match = CalculateMatch(Screen.width, Screen.height);
        _scaler.matchWidthOrHeight = match;
    }

    private float CalculateMatch(float screenW, float screenH)
    {
        // Match=1 时的 Canvas 有效宽度
        float match1Width = screenW * _referenceHeight / screenH;
        float minWidth = _referenceWidth * _minWidthRatio;

        // 宽度充足，保持高度驱动
        if (match1Width >= minWidth)
            return 1f;

        // 宽度不足，解出刚好满足 minWidth 的 match 值
        // CanvasScaler 内部使用对数加权：
        //   log(scaleFactor) = (1-m)*log(screenW/refW) + m*log(screenH/refH)
        //   而 canvasWidth = screenW / scaleFactor
        //   令 canvasWidth = minWidth => scaleFactor = screenW / minWidth
        float targetScale = screenW / minWidth;
        float logSW = Mathf.Log(screenW / _referenceWidth);
        float logSH = Mathf.Log(screenH / _referenceHeight);
        float logTarget = Mathf.Log(targetScale);

        float denominator = logSH - logSW;
        if (Mathf.Approximately(denominator, 0f))
            return 1f;

        float m = (logTarget - logSW) / denominator;
        return Mathf.Clamp01(m);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            // 在 Editor 中预览 19.5:9 屏幕效果（用 Game 视图分辨率）
            // 不修改实际 match 值，只显示计算结果
        }
    }
#endif
}
