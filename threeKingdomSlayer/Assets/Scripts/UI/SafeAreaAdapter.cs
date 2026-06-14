using UnityEngine;

/// <summary>
/// 安全区适配 — 挂载到 Canvas，自动偏移 RectTransform 避开刘海和底部导航条
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdapter : MonoBehaviour
{
    private Rect _lastSafeArea;
    private RectTransform _rect;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea)
            ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        var safeArea = Screen.safeArea;
        var screenW = Screen.width;
        var screenH = Screen.height;

        var anchorMin = safeArea.position;
        var anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenW;
        anchorMin.y /= screenH;
        anchorMax.x /= screenW;
        anchorMax.y /= screenH;

        _rect.anchorMin = anchorMin;
        _rect.anchorMax = anchorMax;
        _lastSafeArea = safeArea;
    }
}
