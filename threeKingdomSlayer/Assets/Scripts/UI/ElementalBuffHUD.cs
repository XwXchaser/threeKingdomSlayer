using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 元素一次性 BUFF 的屏幕提示（当前只有冰系的“破冰预备”）。
/// 零场景接线：首次获得 BUFF 时自动创建一个 Overlay 小图标，消费后淡出。
/// 之后若要并入正式 HUD，把这个组件挂到已有面板上并指定 iconOverride 即可。
/// </summary>
public class ElementalBuffHUD : MonoBehaviour
{
    [Tooltip("自定义图标；为空时用程序化冰晶图标（占位）")]
    public Sprite iconOverride;
    [Tooltip("图标边长（相对屏幕高度的比例）")]
    [Range(0.03f, 0.15f)] public float sizeRatio = 0.07f;
    [Tooltip("图标位置（屏幕宽高比例，左下角为原点）")]
    public Vector2 screenPosition = new Vector2(0.14f, 0.40f);

    private static ElementalBuffHUD _instance;
    private static Sprite _proceduralIcon;

    private CanvasGroup _group;
    private RectTransform _iconRect;
    private float _targetAlpha;
    private float _popTimer;
    private int _lastWidth;
    private int _lastHeight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Hook()
    {
        PlayerElementalBuffs.OnChanged -= OnBuffChanged;
        PlayerElementalBuffs.OnChanged += OnBuffChanged;
    }

    private static void OnBuffChanged()
    {
        if (!Application.isPlaying) return;

        if (!PlayerElementalBuffs.IceReady)
        {
            if (_instance != null) _instance.Hide();
            return;
        }

        if (_instance == null) _instance = CreateInstance();
        _instance.Show();
    }

    private static ElementalBuffHUD CreateInstance()
    {
        var root = new GameObject("ElementalBuffHUD");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        return root.AddComponent<ElementalBuffHUD>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        BuildIcon();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void BuildIcon()
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(transform, false);

        var img = go.AddComponent<Image>();
        img.sprite = iconOverride != null ? iconOverride : GetProceduralIcon();
        img.raycastTarget = false;
        img.preserveAspect = true;

        _iconRect = (RectTransform)go.transform;
        _iconRect.anchorMin = Vector2.zero;
        _iconRect.anchorMax = Vector2.zero;
        _iconRect.pivot = new Vector2(0.5f, 0.5f);
        Layout();

        _group = go.GetComponent<CanvasGroup>();
        _group.alpha = 0f;
        _group.blocksRaycasts = false;
    }

    private void Layout()
    {
        if (_iconRect == null) return;
        _lastWidth = Screen.width;
        _lastHeight = Screen.height;
        float size = Mathf.Clamp(Screen.height * sizeRatio, 48f, 220f);
        _iconRect.sizeDelta = new Vector2(size, size);
        _iconRect.anchoredPosition = new Vector2(Screen.width * screenPosition.x, Screen.height * screenPosition.y);
    }

    private void Show()
    {
        _targetAlpha = 1f;
        _popTimer = 0.22f;
    }

    private void Hide()
    {
        _targetAlpha = 0f;
        _popTimer = 0f;
    }

    private void Update()
    {
        if (_group == null) return;

        if (Screen.width != _lastWidth || Screen.height != _lastHeight)
            Layout();

        if (_popTimer > 0f)
        {
            _popTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(1f - _popTimer / 0.22f);
            if (_iconRect != null) _iconRect.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, t);
        }
        else if (_iconRect != null && _targetAlpha > 0f)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.04f;
            _iconRect.localScale = Vector3.one * pulse;
        }

        _group.alpha = Mathf.MoveTowards(_group.alpha, _targetAlpha, Time.unscaledDeltaTime * 6f);
    }

    /// <summary>程序化冰晶图标：圆角方块 + 六向雪花线。美术给图后可替换。</summary>
    private static Sprite GetProceduralIcon()
    {
        if (_proceduralIcon != null) return _proceduralIcon;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "IceBuffIcon_Procedural"
        };

        var px = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float half = size * 0.44f;
        float corner = 12f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Abs(x - cx) - (half - corner);
                float dy = Mathf.Abs(y - cy) - (half - corner);
                float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f))
                                + Mathf.Min(Mathf.Max(dx, dy), 0f) - corner;

                if (outside > 0f)
                {
                    px[y * size + x] = new Color32(0, 0, 0, 0);
                    continue;
                }

                Color32 c = outside > -2.5f
                    ? new Color32(232, 248, 255, 255)
                    : new Color32(68, 148, 226, 235);

                float ax = Mathf.Abs(x - cx);
                float ay = Mathf.Abs(y - cy);
                bool arm = (ax < 1.2f && ay < 20f)
                           || (ay < 1.2f && ax < 20f)
                           || (Mathf.Abs(ax - ay) < 1.2f && ax < 15f)
                           || (Mathf.Abs(ax + ay) < 1.2f && ax < 15f);
                if (arm) c = new Color32(255, 255, 255, 245);

                px[y * size + x] = c;
            }
        }

        tex.SetPixels32(px);
        tex.Apply();
        _proceduralIcon = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 64f);
        return _proceduralIcon;
    }
}
