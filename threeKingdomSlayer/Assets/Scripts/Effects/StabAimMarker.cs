using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// C2（stab → 蓄力指向释放）的瞄准标记。
///
/// 职责：连段蓄力期间告诉玩家「这一击会打哪一列的第 1 / 2 排」——
///   目标列第 1 排 = 实心菱形；第 2 排 = 同形缩小（表示会穿过去）；
///   两者之间从枪尖方向拉一条流光，亮度随蓄力进度增长（这条带就是本链的「蓄力条」）；
///   手指横向改列时标记平滑滑到新列（同时只有一列亮）。
///
/// 边界：只在 InputManager.comboChargeActive 为真时工作，与 ChargeStabVisual / PierceAimIndicator
/// 的让位条件互补，不会同屏出现两套蓄力指示。全部视觉由纯色 Sprite 程序化生成，不依赖新美术。
/// </summary>
public class StabAimMarker : MonoBehaviour
{
    [Header("目标招式（C2 资产：取 rangeRows 与视觉射程）")]
    public AttackSkillConfig targetMove;

    [Header("外观")]
    public Color markerColor = new Color(1f, 0.95f, 0.85f, 1f);
    [Tooltip("第 1 排标记的世界尺寸（直径）")]
    public float markerSize = 0.6f;
    [Range(0f, 1f)] public float secondRowScale = 0.6f;
    [Range(0f, 1f)] public float secondRowAlpha = 0.45f;
    [Tooltip("流光带宽度（世界单位）")]
    public float beamWidth = 0.08f;
    [Range(0f, 1f)] public float beamMinAlpha = 0.25f;
    [Tooltip("标记整体抬高（世界单位），用来贴地或贴敌人中心")]
    public float heightOffset = 0f;

    [Header("出现时机与进度")]
    [Tooltip("蓄力进度达到此比例才出现，与一级蓄力门槛对齐")]
    [Range(0f, 1f)] public float appearProgress = 0.3f;
    public AnimationCurve progressCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("列切换 / 淡入淡出")]
    public float columnSlideSeconds = 0.08f;
    public float fadeSeconds = 0.06f;

    [Header("排序")]
    public string sortingLayerName = "Default";
    public int sortingOrder = 6;

    private GameObject _root;
    private SpriteRenderer _row0;
    private SpriteRenderer _row1;
    private SpriteRenderer _beam;
    private Sprite _quadSprite;

    private readonly List<Vector3> _pathPoints = new List<Vector3>(8);
    private bool _chargeActive;
    private float _progress;
    private int _column = -1;
    private int _shownColumn = -1;
    private bool _hasRow1;

    private Vector3 _start, _target0, _target1;
    private Vector3 _pos0, _pos1;
    private float _visibleAlpha;

    private static readonly Quaternion DiamondRotation = Quaternion.Euler(0f, 0f, 45f);

    private void Awake()
    {
        _quadSprite = CreateQuadSprite();

        _root = new GameObject("StabAimMarker_Visual");
        _root.transform.SetParent(transform, false);

        _beam = CreateQuad("Beam", _root.transform);
        _row0 = CreateQuad("Row0", _root.transform);
        _row1 = CreateQuad("Row1", _root.transform);

        SetVisible(false);
    }

    private void Start()
    {
        EnsureSubscribed();
    }

    private void OnEnable()
    {
        EnsureSubscribed();
    }

    /// <summary>
    /// 防御式订阅：组件的 OnEnable 可能早于 InputManager.Instance 就绪（场景加载顺序不定），
    /// 因此 Start 与 Update 都会重试；已订阅时直接返回，无额外开销。
    /// </summary>
    private void EnsureSubscribed()
    {
        if (_subscribed) return;
        var im = InputManager.Instance;
        if (im == null) return;
        im.OnChargeBegan += OnChargeBegan;
        im.OnChargeUpdated += OnChargeUpdated;
        im.OnChargeEnded += OnChargeEnded;
        _subscribed = true;
    }

    private void OnDisable()
    {
        var im = InputManager.Instance;
        if (im != null && _subscribed)
        {
            im.OnChargeBegan -= OnChargeBegan;
            im.OnChargeUpdated -= OnChargeUpdated;
            im.OnChargeEnded -= OnChargeEnded;
        }
        _subscribed = false;
        _chargeActive = false;
    }

    private void OnChargeBegan(Vector2 screenPosition)
    {
        _chargeActive = false;   // 等 OnChargeUpdated 确认这是「连段蓄力」
        _progress = 0f;
        _shownColumn = -1;
        _visibleAlpha = 0f;
    }

    private void OnChargeUpdated(Vector2 screenPosition, float progress)
    {
        var im = InputManager.Instance;
        if (im == null || !im.comboChargeActive)
        {
            _chargeActive = false;
            return;
        }

        _chargeActive = true;
        _progress = progress;

        // 与松手释放同一套列映射：用的是本次蓄力事件的屏幕位置
        int column = im.GetStabColumnFromScreenPosition(screenPosition);
        if (column != _shownColumn)
        {
            _shownColumn = column;
            _column = column;
            RefreshTargets();
        }
    }

    private void OnChargeEnded()
    {
        _chargeActive = false;
    }

    /// <summary>按目标列重算标记位置（起点 / 第 1 排 / 第 2 排）</summary>
    private void RefreshTargets()
    {
        if (targetMove == null || _column < 0) return;
        var attackSystem = AttackSystem.Instance;
        if (attackSystem == null) return;
        if (!attackSystem.TryGetStabIndicatorPath(targetMove, _column, _pathPoints, out int _)) return;
        if (_pathPoints.Count < 3) return;

        _start = _pathPoints[0] + Vector3.up * heightOffset;
        _target0 = _pathPoints[1] + Vector3.up * heightOffset;
        _hasRow1 = _pathPoints.Count >= 4;   // 起点 + 第1排 + 第2排 + 视觉终点
        _target1 = _hasRow1 ? _pathPoints[2] + Vector3.up * heightOffset : _target0;

        // 刚出现（还没显示出来）时直接吸附到目标位置，避免从上一列的残留位置滑过来
        if (_visibleAlpha <= 0.01f)
        {
            _pos0 = _target0;
            _pos1 = _target1;
        }
    }

    private bool _subscribed;

    private void Update()
    {
        EnsureSubscribed();
        float dt = Time.unscaledDeltaTime;

        bool shouldShow = _chargeActive && _progress >= appearProgress && _column >= 0 && targetMove != null;
        float fadeStep = fadeSeconds > 0.0001f ? dt / fadeSeconds : 1f;
        _visibleAlpha = Mathf.MoveTowards(_visibleAlpha, shouldShow ? 1f : 0f, fadeStep);

        if (_visibleAlpha <= 0.001f)
        {
            SetVisible(false);
            return;
        }
        SetVisible(true);

        float slideStep = columnSlideSeconds > 0.0001f ? Mathf.Clamp01(dt / columnSlideSeconds) : 1f;
        _pos0 = Vector3.Lerp(_pos0, _target0, slideStep);
        _pos1 = Vector3.Lerp(_pos1, _target1, slideStep);

        float p = Mathf.Clamp01(progressCurve.Evaluate(Mathf.Clamp01(_progress)));
        float size = markerSize * Mathf.Lerp(0.85f, 1.15f, p);

        // 第 1 排：实心菱形（正方形转 45°）
        _row0.transform.position = _pos0;
        _row0.transform.localScale = Vector3.one * size;
        _row0.transform.rotation = DiamondRotation;
        _row0.color = WithAlpha(markerColor, _visibleAlpha);

        // 第 2 排：同形缩小 + 更淡（表示穿透）
        _row1.gameObject.SetActive(_hasRow1);
        if (_hasRow1)
        {
            _row1.transform.position = _pos1;
            _row1.transform.localScale = Vector3.one * (size * secondRowScale);
            _row1.transform.rotation = DiamondRotation;
            _row1.color = WithAlpha(markerColor, _visibleAlpha * secondRowAlpha);
        }

        // 流光：从枪尖（起点）指向第 1 排，亮度/宽度随进度增长
        Vector3 from = _start;
        Vector3 to = _pos0;
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length > 0.0001f)
        {
            Vector3 mid = (from + to) * 0.5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            _beam.transform.position = mid;
            _beam.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            _beam.transform.localScale = new Vector3(beamWidth * Mathf.Lerp(0.4f, 1f, p), length, 1f);
            _beam.color = WithAlpha(markerColor, _visibleAlpha * Mathf.Lerp(beamMinAlpha, 1f, p));
        }
        else
        {
            _beam.gameObject.SetActive(false);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_root == null) return;
        if (_root.activeSelf != visible) _root.SetActive(visible);
    }

    private SpriteRenderer CreateQuad(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _quadSprite;
        sr.color = WithAlpha(markerColor, 0f);
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;
        return sr;
    }

    private static Color WithAlpha(Color c, float alpha)
    {
        c.a = Mathf.Clamp01(alpha);
        return c;
    }

    /// <summary>1×1 世界单位的纯白 Sprite（PPU = 4，贴图 4×4），避免为新标记引入美术资产</summary>
    private static Sprite CreateQuadSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
    }
}
