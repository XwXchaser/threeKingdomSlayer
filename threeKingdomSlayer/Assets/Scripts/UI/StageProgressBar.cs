using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 关卡进度条：点线式展示波次进度，与 QTE 判定框共用底部区域。
/// 始终显示 4 个节点 + 3 条连线。每 3 波滚动一次窗口。
/// 玩家绿点统一在波次完成后右移（Option A）。
/// </summary>
[ExecuteAlways]
public class StageProgressBar : MonoBehaviour
{
    [Header("布局")]
    [Tooltip("相邻节点间距（像素）")]
    public float dotSpacing = 150f;
    [Tooltip("白线高度（像素）")]
    public float lineThickness = 6f;
    [Tooltip("节点圆点直径（像素）")]
    public int dotDiameter = 20;
    [Tooltip("玩家点直径（像素），比节点大以区分")]
    public int playerDotDiameter = 28;
    [Tooltip("窗口可见节点数（固定 4 个）")]
    public int visibleDots = 4;
    [Tooltip("每次滚动露出的波次数（固定 3）")]
    public int scrollGroupSize = 3;

    [Header("颜色")]
    public Color nodeColor = Color.red;
    public Color playerColor = Color.green;
    public Color lineColor = Color.white;

    [Header("动画")]
    public float moveDuration = 0.3f;
    public float scrollDuration = 0.5f;
    public Ease scrollEase = Ease.InOutCubic;

    [Header("QTE 联动")]
    [Tooltip("QTE 判定框 RectTransform，进度条显示时自动隐藏")]
    public RectTransform qteFrameRect;

    [Header("美术素材")]
    [Tooltip("行走线（可选，留空使用程序化白线）")]
    public Sprite lineSprite;
    [Tooltip("进度条外框（可选）")]
    public Sprite frameSprite;
    [Tooltip("玩家位置指示器（可选，留空使用程序化绿点）")]
    public Sprite playerDotSprite;
    [Tooltip("关卡节点（可选，留空使用程序化红点）")]
    public Sprite nodeSprite;

    // 运行时引用
    private RectTransform _contentRect;
    private float _contentOriginX; // Content 初始 X 偏移，首次从 prefab 读取，后续滚动以此为基准
    private Image _lineImage;
    private Image _frameImage;
    private Image _playerDotImage;
    private RectTransform _playerDotRect;
    private List<GameObject> _nodeDots = new List<GameObject>();

    // 运行时状态
    private int _totalWaves;
    private int _playerDotIndex;      // 玩家所在的概念节点索引（已完成波次数）
    private int _windowStartIndex;    // 窗口左端对应的概念节点索引
    private int _activeBossCount;     // 当前交战中 BOSS 数量
    private int _pendingBossWaveIndex = -1; // BOSS 入场动画中，完成后 OnWaveCompleted 跳过

    private WaveSpawner _waveSpawner;
    private StageConfig _stageConfig;
    private bool _initialized;
    private float _uiScale;

    // 共享纹理
    private static Texture2D _circleTex;
    private static Texture2D _whiteTex;

    private void Awake()
    {
        if (!Application.isPlaying) return; // Edit Mode 预览由 OnEnable 处理

        EnsureTextures();

        _uiScale = UIResolutionHelper.UIScale;
        dotSpacing *= _uiScale;
        lineThickness *= _uiScale;
        dotDiameter = Mathf.Max(1, Mathf.RoundToInt(dotDiameter * _uiScale));
        playerDotDiameter = Mathf.Max(1, Mathf.RoundToInt(playerDotDiameter * _uiScale));

        var go = gameObject;
        go.layer = LayerMask.NameToLayer("UI");
        go.AddComponent<RectMask2D>();
        BuildVisuals();
        go.SetActive(false);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            _editModePreviewDirty = true;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
        {
            _editModePreviewDirty = false;
#if UNITY_EDITOR
            DestroyEditModePreview();
#endif
        }
    }

    /// <summary>
    /// 构建可视化子节点（Frame / Content / Line / PlayerDot）。
    /// 编辑模式预览和运行时共用同一套创建逻辑。
    /// </summary>
    private void BuildVisuals()
    {
        // Frame（查找已有或新建；序列化后会在 prefab 中保留，支持手动调整）
        var frameT = transform.Find("Frame");
        if (frameT != null)
        {
            _frameImage = frameT.GetComponent<Image>();
            if (_frameImage == null) _frameImage = frameT.gameObject.AddComponent<Image>();
        }
        else
        {
            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            if (!Application.isPlaying) frameGo.hideFlags = HideFlags.DontSave;
            frameGo.transform.SetParent(transform, false);
            _frameImage = frameGo.GetComponent<Image>();
            var frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.sizeDelta = Vector2.zero;
        }
        _frameImage.raycastTarget = false;
        if (frameSprite != null)
        {
            _frameImage.sprite = frameSprite;
            _frameImage.type = Image.Type.Sliced;
            _frameImage.color = Color.white;
            _frameImage.enabled = true;
        }
        else
        {
            _frameImage.enabled = false;
        }

        // Content 容器（查找已有或新建）
        var contentT = transform.Find("Content");
        if (contentT != null)
        {
            _contentRect = contentT.GetComponent<RectTransform>();
            _contentOriginX = _contentRect.anchoredPosition.x;
        }
        else
        {
            var contentGo = new GameObject("Content", typeof(RectTransform));
            if (!Application.isPlaying) contentGo.hideFlags = HideFlags.DontSave;
            contentGo.transform.SetParent(transform, false);
            _contentRect = contentGo.GetComponent<RectTransform>();
            _contentRect.anchorMin = new Vector2(0f, 0.5f);
            _contentRect.anchorMax = new Vector2(0f, 0.5f);
            _contentRect.pivot = new Vector2(0f, 0.5f);
            _contentRect.anchoredPosition = Vector2.zero;
            _contentOriginX = 0f;
        }

        // 行走线（总是重建）
        var lineGo = new GameObject("Line", typeof(RectTransform), typeof(Image));
        lineGo.transform.SetParent(_contentRect, false);
        _lineImage = lineGo.GetComponent<Image>();
        if (lineSprite != null)
        {
            _lineImage.sprite = lineSprite;
            _lineImage.type = Image.Type.Sliced;
        }
        else
        {
            _lineImage.sprite = Sprite.Create(_whiteTex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
            _lineImage.type = Image.Type.Sliced;
        }
        _lineImage.color = lineColor;
        _lineImage.raycastTarget = false;
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0f, 0.5f);
        lineRt.anchorMax = new Vector2(0f, 0.5f);
        lineRt.pivot = new Vector2(0f, 0.5f);
        lineRt.anchoredPosition = Vector2.zero;
        lineRt.sizeDelta = new Vector2(0f, lineThickness);

        // PlayerDot（总是重建）
        var pdGo = new GameObject("PlayerDot", typeof(RectTransform), typeof(Image));
        pdGo.transform.SetParent(_contentRect, false);
        _playerDotImage = pdGo.GetComponent<Image>();
        if (playerDotSprite != null)
        {
            _playerDotImage.sprite = playerDotSprite;
        }
        else
        {
            _playerDotImage.sprite = Sprite.Create(_circleTex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
        }
        _playerDotImage.color = playerColor;
        _playerDotImage.raycastTarget = false;
        _playerDotRect = pdGo.GetComponent<RectTransform>();
        _playerDotRect.anchorMin = new Vector2(0f, 0.5f);
        _playerDotRect.anchorMax = new Vector2(0f, 0.5f);
        _playerDotRect.pivot = new Vector2(0.5f, 0.5f);
        _playerDotRect.sizeDelta = new Vector2(playerDotDiameter, playerDotDiameter);
        _playerDotRect.anchoredPosition = new Vector2(0f, 0f);
        _playerDotRect.SetAsLastSibling();
    }

    /// <summary>
    /// 销毁可视化子节点（编辑模式下参数变化时调用）
    /// </summary>
    private void DestroyVisuals()
    {
        // 清理节点列表中的 GameObject
        foreach (var nd in _nodeDots)
            if (nd != null) DestroyImmediate(nd);
        _nodeDots.Clear();

        // 销毁 Frame / Content（Content 销毁会连带销毁 Line / PlayerDot / Node_*）
        if (_frameImage != null) { DestroyImmediate(_frameImage.gameObject); _frameImage = null; }
        if (_contentRect != null) { DestroyImmediate(_contentRect.gameObject); _contentRect = null; _lineImage = null; _playerDotImage = null; _playerDotRect = null; }
    }

    private void Start()
    {
        // 初始化交由 BattleHUD 调用 Initialize()，避免时序问题
    }

    /// <summary>
    /// 由 BattleHUD 在实例化后调用，注入依赖并完成初始化。
    /// </summary>
    public void Initialize(StageConfig stageConfig, WaveSpawner waveSpawner)
    {
        if (_initialized) return;

        _waveSpawner = waveSpawner;
        if (_waveSpawner == null)
        {
            Debug.LogWarning("[StageProgressBar] WaveSpawner 为 null");
            return;
        }

        _stageConfig = stageConfig;
        if (_stageConfig == null)
        {
            Debug.LogWarning("[StageProgressBar] StageConfig 为 null");
            return;
        }

        _totalWaves = _stageConfig.waves.Count;
        if (_totalWaves <= 0) return;

        LayoutContent();

        // 波次事件：仅用于移动玩家点
        _waveSpawner.OnWaveStarted += OnWaveStarted;
        _waveSpawner.OnWaveCompleted += OnWaveCompleted;

        // BOSS 交战事件：切换进度条 ↔ QTE 框
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnBossEngaged += OnBossEngaged;

        int currentWave = _waveSpawner.CurrentWaveIndex;
        if (currentWave > 0)
        {
            _playerDotIndex = currentWave;
            _windowStartIndex = Mathf.Max(0, _playerDotIndex - (visibleDots - 1));
            _windowStartIndex = (_windowStartIndex / scrollGroupSize) * scrollGroupSize;
        }

        _initialized = true;
        SetVisible(true);
        SnapPlayerToPosition();
    }

    private void OnDestroy()
    {
        if (_waveSpawner != null)
        {
            _waveSpawner.OnWaveStarted -= OnWaveStarted;
            _waveSpawner.OnWaveCompleted -= OnWaveCompleted;
        }
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnBossEngaged -= OnBossEngaged;
    }

    // ═══════════════════════════════════════════
    //  事件
    // ═══════════════════════════════════════════

    private void OnWaveStarted(int waveIndex)
    {
        // 波次开始不切换 UI，切换由 BOSS 交战事件驱动
    }

    private void OnWaveCompleted(int waveIndex)
    {
        if (!_initialized) return;
        if (waveIndex < 0 || waveIndex >= _totalWaves) return;

        // BOSS 波：玩家点已在 OnBossEngaged 入场动画中移动，此处跳过
        if (waveIndex == _pendingBossWaveIndex)
        {
            _pendingBossWaveIndex = -1;
            return;
        }

        TryMovePlayerRight();
    }

    // ═══════════════════════════════════════════
    //  移动逻辑
    // ═══════════════════════════════════════════

    private void TryMovePlayerRight()
    {
        int nextIndex = _playerDotIndex + 1;
        if (nextIndex > _totalWaves) return; // 已通关

        // 可以在当前窗口内右移
        if (nextIndex < _windowStartIndex + visibleDots)
        {
            AnimatePlayerTo(nextIndex);
            return;
        }

        // 需要滚动窗口
        if (_windowStartIndex + visibleDots - 1 >= _totalWaves)
        {
            // 最后一组，直接走到终点
            AnimatePlayerTo(nextIndex);
            return;
        }

        PerformScrollThenMove();
    }

    private void PerformScrollThenMove()
    {
        int nextIndex = _playerDotIndex + 1;
        if (nextIndex > _totalWaves) return;

        int newWindowStart = _windowStartIndex + scrollGroupSize;
        newWindowStart = Mathf.Min(newWindowStart, Mathf.Max(0, _totalWaves - visibleDots + 1));

        if (newWindowStart == _windowStartIndex)
        {
            AnimatePlayerTo(_totalWaves);
            return;
        }

        _windowStartIndex = newWindowStart;

        var seq = DOTween.Sequence();
        seq.Append(_contentRect.DOAnchorPosX(_contentOriginX - newWindowStart * dotSpacing, scrollDuration)
            .SetEase(scrollEase));

        int targetIndex = Mathf.Min(nextIndex, _totalWaves);
        seq.AppendCallback(() =>
        {
            _playerDotIndex = targetIndex;
            UpdatePlayerDotPosition();
        });

        seq.AppendCallback(UpdateNodeAppearance);
    }

    private void AnimatePlayerTo(int targetIndex)
    {
        _playerDotIndex = Mathf.Clamp(targetIndex, 0, _totalWaves);
        float targetX = _playerDotIndex * dotSpacing;
        _playerDotRect.DOKill();
        _playerDotRect.DOAnchorPosX(targetX, moveDuration).SetEase(Ease.OutCubic);
    }

    private void SnapPlayerToPosition()
    {
        float targetX = _playerDotIndex * dotSpacing;
        _playerDotRect.anchoredPosition = new Vector2(targetX, _playerDotRect.anchoredPosition.y);
        UpdateContentPosition();
        UpdateNodeAppearance();
    }

    private void UpdatePlayerDotPosition()
    {
        float targetX = _playerDotIndex * dotSpacing;
        _playerDotRect.anchoredPosition = new Vector2(targetX, _playerDotRect.anchoredPosition.y);
    }

    // ═══════════════════════════════════════════
    //  布局
    // ═══════════════════════════════════════════

    private void LayoutContent()
    {
        // 清理旧节点
        foreach (var nd in _nodeDots)
            if (nd != null) Destroy(nd);
        _nodeDots.Clear();

        // Content 宽度：容纳所有概念节点（totalWaves + 1 个点：起点 + 每个波次后一个点）
        int totalDots = _totalWaves + 1; // dot 0=起点, dot N=终点
        float contentWidth = (_totalWaves) * dotSpacing + dotSpacing; // 留一个间距余量
        _contentRect.sizeDelta = new Vector2(contentWidth, lineThickness);

        // 白线宽度
        var lineRt = _lineImage.GetComponent<RectTransform>();
        lineRt.sizeDelta = new Vector2(contentWidth, lineThickness);

        // 创建所有节点
        for (int i = 0; i < totalDots; i++)
        {
            var go = CreateNodeDot(i);
            _nodeDots.Add(go);
        }

        // 窗口初始位置
        _windowStartIndex = 0;
        _playerDotIndex = 0;
        UpdateContentPosition();
        UpdateNodeAppearance();

        // 确保绿点在所有红点之上
        _playerDotRect.SetAsLastSibling();
    }

    private GameObject CreateNodeDot(int dotIndex)
    {
        var go = new GameObject($"Node_{dotIndex}", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_contentRect, false);

        var img = go.GetComponent<Image>();
        img.sprite = Sprite.Create(_circleTex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
        img.color = nodeColor;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(dotDiameter, dotDiameter);
        rt.anchoredPosition = new Vector2(dotIndex * dotSpacing, 0f);

        return go;
    }

    private void UpdateContentPosition()
    {
        float x = _contentOriginX - _windowStartIndex * dotSpacing;
        _contentRect.anchoredPosition = new Vector2(x, _contentRect.anchoredPosition.y);
    }

    private void UpdateNodeAppearance()
    {
        // 所有节点默认红色，超出总波次的终点节点可以变成灰色
        for (int i = 0; i < _nodeDots.Count; i++)
        {
            var img = _nodeDots[i].GetComponent<Image>();
            if (img != null)
            {
                // 已通过的节点变暗，未通过的保持红色
                if (i < _playerDotIndex)
                    img.color = new Color(nodeColor.r * 0.4f, nodeColor.g * 0.4f, nodeColor.b * 0.4f, 0.5f);
                else
                    img.color = nodeColor;
            }
        }
    }

    // ═══════════════════════════════════════════
    //  显隐
    // ═══════════════════════════════════════════

    /// <summary>
    /// 切换进度条可见性。true = 进度条，false = QTE 框。
    /// QTEFrame 使用 CanvasGroup 控制显隐而非 SetActive，
    /// 避免杀死 QTE 指示器的 DOTween 退场动画导致残留。
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (qteFrameRect != null)
        {
            var cg = qteFrameRect.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = qteFrameRect.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = visible ? 0f : 1f;
            cg.blocksRaycasts = !visible;
            cg.interactable = !visible;

            // QTEFrame Image 无 sprite 时不渲染，补白色 sprite 确保背景可见
            if (!visible)
            {
                var qteImg = qteFrameRect.GetComponent<Image>();
                if (qteImg != null && qteImg.sprite == null)
                {
                    EnsureTextures();
                    qteImg.sprite = Sprite.Create(_whiteTex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
                    qteImg.type = Image.Type.Sliced;
                }
            }
        }
        gameObject.SetActive(visible);
    }

    // ═══════════════════════════════════════════
    //  BOSS 交战/死亡 → 切换 UI
    // ═══════════════════════════════════════════

    private void OnBossEngaged(Enemy boss)
    {
        if (!_initialized) return;
        if (boss == null) return;

        bool wasZero = _activeBossCount == 0;
        _activeBossCount++;
        boss.OnDeath += OnBossDeath;

        if (wasZero)
        {
            _pendingBossWaveIndex = _waveSpawner.CurrentWaveIndex;
            AnimatePlayerForwardThenSwitch();
        }
    }

    /// <summary>
    /// BOSS 入场：先平移动画移玩家点到下一位置，完成后切 QTE 面板
    /// </summary>
    private void AnimatePlayerForwardThenSwitch()
    {
        int nextIndex = _playerDotIndex + 1;
        if (nextIndex > _totalWaves)
        {
            SetVisible(false);
            return;
        }

        // 可以在当前窗口内右移
        if (nextIndex < _windowStartIndex + visibleDots)
        {
            _playerDotIndex = nextIndex;
            float targetX = _playerDotIndex * dotSpacing;
            _playerDotRect.DOKill();
            _playerDotRect.DOAnchorPosX(targetX, moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => SetVisible(false));
            UpdateNodeAppearance();
            return;
        }

        // 需要滚动窗口
        if (_windowStartIndex + visibleDots - 1 >= _totalWaves)
        {
            // 最后一组
            _playerDotIndex = nextIndex;
            float targetX = _playerDotIndex * dotSpacing;
            _playerDotRect.DOKill();
            _playerDotRect.DOAnchorPosX(targetX, moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() => SetVisible(false));
            UpdateNodeAppearance();
            return;
        }

        // 滚动 + 移动 + 切 QTE
        int newWindowStart = _windowStartIndex + scrollGroupSize;
        newWindowStart = Mathf.Min(newWindowStart, Mathf.Max(0, _totalWaves - visibleDots + 1));
        _windowStartIndex = newWindowStart;
        _playerDotIndex = Mathf.Min(nextIndex, _totalWaves);

        var seq = DOTween.Sequence();
        seq.Append(_contentRect.DOAnchorPosX(_contentOriginX - newWindowStart * dotSpacing, scrollDuration)
            .SetEase(scrollEase));
        seq.AppendCallback(() =>
        {
            UpdatePlayerDotPosition();
            UpdateNodeAppearance();
        });
        seq.AppendInterval(moveDuration);
        seq.AppendCallback(() => SetVisible(false));
    }

    private void OnBossDeath(Enemy boss)
    {
        if (boss == null) return;
        boss.OnDeath -= OnBossDeath;

        _activeBossCount--;
        if (_activeBossCount <= 0)
        {
            _activeBossCount = 0;
            SetVisible(true);
        }
    }

    // ═══════════════════════════════════════════
    //  工具
    // ═══════════════════════════════════════════

    private static void EnsureTextures()
    {
        if (_circleTex == null)
        {
            int size = 64;
            _circleTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _circleTex.wrapMode = TextureWrapMode.Clamp;
            _circleTex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            float center = size * 0.5f;
            float radius = size * 0.5f - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 1f - Mathf.Clamp01((dist - radius + 1f) / 2f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }
            _circleTex.SetPixels(pixels);
            _circleTex.Apply();
        }

        if (_whiteTex == null)
        {
            _whiteTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            _whiteTex.wrapMode = TextureWrapMode.Clamp;
            _whiteTex.filterMode = FilterMode.Bilinear;
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            _whiteTex.SetPixels(px);
            _whiteTex.Apply();
        }
    }

    // 编辑模式预览脏标记（非 EDITOR 也声明，避免 OnEnable 引用失败）
    private bool _editModePreviewDirty;

#if UNITY_EDITOR
    private static readonly HideFlags PreviewFlags = HideFlags.DontSave | HideFlags.NotEditable;

    private void Update()
    {
        if (Application.isPlaying) return;
        if (_editModePreviewDirty && gameObject.activeInHierarchy)
        {
            _editModePreviewDirty = false;
            RebuildEditModePreview();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!gameObject.activeInHierarchy) return;
        // 参数变化 → 延迟重建预览（避免 serialization 冲突）
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying && gameObject.activeInHierarchy)
                RebuildEditModePreview();
        };
    }

    private void DestroyEditModePreview()
    {
        // 清理临时 RectMask2D
        var masks = GetComponents<RectMask2D>();
        foreach (var m in masks)
            if ((m.hideFlags & PreviewFlags) != 0) DestroyImmediate(m);

        // 清理 Content 及其子节点，以及 Frame（仅 DontSave 标记的临时对象）
        if (_contentRect != null && (_contentRect.hideFlags & HideFlags.DontSave) != 0)
        {
            DestroyImmediate(_contentRect.gameObject);
            _contentRect = null;
        }
        else if (_contentRect != null)
        {
            for (int i = _contentRect.childCount - 1; i >= 0; i--)
            {
                var child = _contentRect.GetChild(i);
                if ((child.hideFlags & PreviewFlags) != 0)
                    DestroyImmediate(child.gameObject);
            }
        }

        if (_frameImage != null && (_frameImage.hideFlags & HideFlags.DontSave) != 0)
        {
            DestroyImmediate(_frameImage.gameObject);
            _frameImage = null;
        }

        _lineImage = null;
        _playerDotImage = null;
        _playerDotRect = null;
        _nodeDots.Clear();
    }

    private void RebuildEditModePreview()
    {
        DestroyEditModePreview();
        EnsureTextures();

        var mask = gameObject.AddComponent<RectMask2D>();
        mask.hideFlags = PreviewFlags;

        BuildVisuals();

        // 仅标记 Content 的子节点为不可编辑，保留 Frame/Content 可选中调整位置
        if (_contentRect != null)
            MarkChildrenOnly(_contentRect, PreviewFlags);

        // 预览线宽 = visibleDots * dotSpacing
        if (_lineImage != null)
            _lineImage.GetComponent<RectTransform>().sizeDelta = new Vector2(visibleDots * dotSpacing, lineThickness);

        // 创建 visibleDots 个节点
        if (_contentRect != null)
        {
            for (int i = 0; i < visibleDots; i++)
            {
                var dotGo = new GameObject($"Node_{i}", typeof(RectTransform), typeof(Image));
                dotGo.hideFlags = PreviewFlags;
                dotGo.transform.SetParent(_contentRect, false);
                var dotImg = dotGo.GetComponent<Image>();
                dotImg.sprite = Sprite.Create(_circleTex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
                dotImg.color = nodeColor;
                dotImg.raycastTarget = false;
                var dotRt = dotGo.GetComponent<RectTransform>();
                dotRt.anchorMin = new Vector2(0f, 0.5f);
                dotRt.anchorMax = new Vector2(0f, 0.5f);
                dotRt.pivot = new Vector2(0.5f, 0.5f);
                dotRt.sizeDelta = new Vector2(dotDiameter, dotDiameter);
                dotRt.anchoredPosition = new Vector2(i * dotSpacing, 0f);
            }
        }

        if (_playerDotRect != null) _playerDotRect.SetAsLastSibling();
    }

    private static void MarkSubtree(Transform root, HideFlags flags)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            child.gameObject.hideFlags = flags;
            MarkSubtree(child, flags);
        }
    }

    private static void MarkChildrenOnly(Transform parent, HideFlags flags)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            child.gameObject.hideFlags = flags;
            MarkSubtree(child, flags);
        }
    }
#endif
}
