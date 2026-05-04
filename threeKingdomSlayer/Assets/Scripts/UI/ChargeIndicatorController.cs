using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 动态蓄力瞄准指示器
/// 在鼠标/手指位置生成径向填充圆环，跟随蓄力进度从空→满
/// 使用对象池复用实例，ScreenSpace-Overlay Canvas 坐标系
///
/// 使用方式：
///   1. 挂载到任意 GameObject 上（建议挂在 Canvas 下）
///   2. 指定 canvasRectTransform（ScreenSpace-Overlay Canvas）
///   3. 可选：调整 indicatorSize / indicatorColor / poolCapacity
///   4. 自动订阅 InputManager 的蓄力事件
/// </summary>
public class ChargeIndicatorController : MonoBehaviour
{
    [Header("Canvas 设置")]
    [Tooltip("ScreenSpace-Overlay Canvas 的 RectTransform。留空则在 Start() 中自动查找")]
    public RectTransform canvasRectTransform;

    [Header("指示器外观")]
    [Tooltip("指示器尺寸（像素）")]
    public Vector2 indicatorSize = new Vector2(80f, 80f);
    [Tooltip("指示器基础颜色")]
    public Color indicatorColor = Color.white;
    [Tooltip("对象池容量（最大同时活跃的指示器数量）")]
    public int poolCapacity = 3;

    // 对象池
    private Stack<GameObject> pool = new Stack<GameObject>();
    private GameObject currentIndicator;
    private Image currentImage;

    private void Start()
    {
        // 自动查找 Canvas
        if (canvasRectTransform == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
                canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        // 订阅 InputManager 蓄力事件
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan += OnChargeBegan;
            InputManager.Instance.OnChargeUpdated += OnChargeUpdated;
            InputManager.Instance.OnChargeEnded += OnChargeEnded;
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnChargeBegan -= OnChargeBegan;
            InputManager.Instance.OnChargeUpdated -= OnChargeUpdated;
            InputManager.Instance.OnChargeEnded -= OnChargeEnded;
        }

        // 清理对象池
        ClearPool();
    }

    /// <summary>
    /// 蓄力开始：从池中取指示器，定位到鼠标位置，fillAmount = 0
    /// </summary>
    private void OnChargeBegan(Vector2 screenPos)
    {
        // 如果已有活跃指示器，先归还
        if (currentIndicator != null)
        {
            ReturnToPool(currentIndicator);
        }

        // 从池中取或创建新指示器
        currentIndicator = GetFromPool();
        currentImage = currentIndicator.GetComponent<Image>();

        // 定位到鼠标/触摸位置
        currentIndicator.transform.position = screenPos;
        currentIndicator.transform.SetAsLastSibling(); // 置于 UI 最上层

        // 初始状态：空（无蓄力）
        currentImage.fillAmount = 0f;
        currentIndicator.SetActive(true);
    }

    /// <summary>
    /// 蓄力进度更新：跟随鼠标/触摸位置，fillAmount 从 0→1
    /// </summary>
    private void OnChargeUpdated(Vector2 screenPos, float progress)
    {
        if (currentIndicator != null && currentImage != null && currentIndicator.activeInHierarchy)
        {
            currentIndicator.transform.position = screenPos;
            // progress 由 InputManager 计算：Clamp01(pressDuration / minChargeTime)
            currentImage.fillAmount = Mathf.Clamp01(progress);
        }
    }

    /// <summary>
    /// 蓄力结束：隐藏当前指示器，归还对象池
    /// </summary>
    private void OnChargeEnded()
    {
        if (currentIndicator != null)
        {
            ReturnToPool(currentIndicator);
            currentIndicator = null;
            currentImage = null;
        }
    }

    #region 对象池管理

    /// <summary>
    /// 从池中取一个指示器实例，池空则创建新实例
    /// </summary>
    private GameObject GetFromPool()
    {
        // 尝试从池中弹出有效实例
        while (pool.Count > 0)
        {
            GameObject obj = pool.Pop();
            if (obj != null)
                return obj;
        }

        // 池空 → 创建新实例
        return CreateIndicatorInstance();
    }

    /// <summary>
    /// 将指示器归还到对象池
    /// </summary>
    private void ReturnToPool(GameObject indicator)
    {
        if (indicator == null) return;

        indicator.SetActive(false);
        pool.Push(indicator);
    }

    /// <summary>
    /// 动态创建指示器 Image 实例（无需预制体）
    /// 在 ScreenSpace-Overlay Canvas 下创建，使用 Radial360/Top 填充方式
    /// </summary>
    private GameObject CreateIndicatorInstance()
    {
        GameObject go = new GameObject("ChargeIndicator", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(canvasRectTransform, false);
        go.SetActive(false);

        // RectTransform 尺寸
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = indicatorSize;

        // Image 设置：Radial360 填充，从顶部开始
        Image img = go.GetComponent<Image>();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = 2; // Image.Origin360.Top = 2
        img.fillAmount = 0f;
        img.color = indicatorColor;
        img.raycastTarget = false; // 不阻挡鼠标/触摸穿透

        // 创建纯白精灵作为指示器纹理
        img.sprite = CreateWhiteSprite();

        return go;
    }

    /// <summary>
    /// 创建纯白 1x1 精灵，用于径向填充指示器
    /// </summary>
    private static Sprite CreateWhiteSprite()
    {
        // 先尝试加载 Unity 内置 UI 精灵
        Sprite builtin = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        if (builtin != null)
            return builtin;

        // 兜底：程序化生成 64x64 白色圆形纹理
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 清空对象池中的所有实例
    /// </summary>
    private void ClearPool()
    {
        if (currentIndicator != null)
        {
            if (Application.isPlaying)
                Destroy(currentIndicator);
            else
                DestroyImmediate(currentIndicator);
            currentIndicator = null;
            currentImage = null;
        }

        while (pool.Count > 0)
        {
            GameObject obj = pool.Pop();
            if (obj != null)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
        }
    }

    #endregion
}
