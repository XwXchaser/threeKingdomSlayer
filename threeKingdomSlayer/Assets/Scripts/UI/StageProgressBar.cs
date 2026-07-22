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
    [Tooltip("节点交替的 Y 轴偏移（像素），用于形成轻微起伏的路线")]
    public float nodeVerticalOffset = 14f;
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
    [Tooltip("行走线（未完成路段）")]
    public Sprite lineSprite;
    [Tooltip("已完成路段")]
    public Sprite completedLineSprite;
    [Tooltip("进度条外框（可选）")]
    public Sprite frameSprite;
    [Tooltip("玩家位置指示器")]
    public Sprite playerDotSprite;
    [Tooltip("普通关卡节点")]
    public Sprite nodeSprite;
    [Tooltip("Boss 关卡节点")]
    public Sprite bossNodeSprite;

    // 运行时引用
    private RectTransform _contentRect;
    private float _contentOriginX; // Content 初始 X 偏移，首次从 prefab 读取，后续滚动以此为基准
    private Image _lineImage;
    private Image _completedLineImage;
    private readonly List<Image> _uncompletedSegmentImages = new List<Image>();
    private readonly List<Image> _completedSegmentImages = new List<Image>();
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
        nodeVerticalOffset *= _uiScale;
        dotDiameter = Mathf.Max(1, Mathf.RoundToInt(dotDiameter * _uiScale));
        playerDotDiameter = Mathf.Max(1, Mathf.RoundToInt(playerDotDiameter * _uiScale));

        var go = gameObject;
        go.layer = LayerMask.NameToLayer("UI");
        go.AddComponent<RectMask2D>();
        BuildVisuals();
    }

    private void Start()
    {
        // BattleHUD 通常会在这里前完成依赖注入；若它的 Start 顺序靠后，
        // OnEnable 会在本帧后再次尝试，避免进度条永久未初始化。
        TryInitializeFromScene();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            _editModePreviewDirty = true;
            return;
        }

        if (_initialized)
            gameObject.SetActive(true);
        else
            TryInitializeFromScene();
    }

    private void TryInitializeFromScene()
    {
        if (_initialized) return;
        var stageConfig = StageController.Instance?.stageConfig;
        var waveSpawner = WaveSpawner.Instance;
        if (stageConfig != null && waveSpawner != null)
            Initialize(stageConfig, waveSpawner);
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

        // 运行时重新建立 Content 内的动态节点，避免继承 Prefab 预览残留。
        for (int i = _contentRect.childCount - 1; i >= 0; i--)
        {
            var child = _contentRect.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
        _nodeDots.Clear();
        _uncompletedSegmentImages.Clear();
        _completedSegmentImages.Clear();

        // 保留旧字段引用供初始化检查兼容；实际显示按节点逐段创建。
        _lineImage = null;
        _completedLineImage = null;

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
        if (_contentRect != null) { DestroyImmediate(_contentRect.gameObject); _contentRect = null; _lineImage = null; _completedLineImage = null; _playerDotImage = null; _playerDotRect = null; }
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

        // This object can be inactive while its parent flip face is hidden, so Awake may not
        // have built its cached visuals before BattleHUD injects the stage dependencies.
        if (_contentRect == null || _playerDotRect == null)
            BuildVisuals();

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
        _playerDotRect.DOKill();
        _playerDotRect.DOAnchorPos(GetNodePosition(_playerDotIndex), moveDuration).SetEase(Ease.OutCubic)
            .OnComplete(UpdateNodeAppearance);
    }

    private void SnapPlayerToPosition()
    {
        _playerDotRect.anchoredPosition = GetNodePosition(_playerDotIndex);
        UpdateContentPosition();
        UpdateNodeAppearance();
    }

    private void UpdatePlayerDotPosition()
    {
        _playerDotRect.anchoredPosition = GetNodePosition(_playerDotIndex);
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

        int totalDots = _totalWaves + 1; // dot 0=起点, dot N=终点
        float contentWidth = (_totalWaves) * dotSpacing + dotSpacing; // 留一个间距余量
        _contentRect.sizeDelta = new Vector2(contentWidth, lineThickness + nodeVerticalOffset * 2f);

        CreateSegments(totalDots);

        // 创建所有节点
        for (int i = 0; i < totalDots; i++)
        {
            var go = CreateNodeDot(i);
            _nodeDots.Add(go);
        }

        CenterContentOnBoard();

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
        bool isBossNode = _stageConfig != null
            && dotIndex > 0
            && dotIndex - 1 < _stageConfig.waves.Count
            && _stageConfig.waves[dotIndex - 1].isBossWave;
        img.sprite = isBossNode && bossNodeSprite != null ? bossNodeSprite : nodeSprite;
        if (img.sprite == null)
            img.sprite = Sprite.Create(_circleTex, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
        img.preserveAspect = true;
        img.color = (nodeSprite != null || (isBossNode && bossNodeSprite != null)) ? Color.white : nodeColor;
        img.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        float size = isBossNode ? dotDiameter * 2.1f : dotDiameter * 1.6f;
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = GetNodePosition(dotIndex);

        return go;
    }

    private Vector2 GetNodePosition(int dotIndex)
    {
        float y = dotIndex % 2 == 0 ? -nodeVerticalOffset : nodeVerticalOffset;
        return new Vector2(dotIndex * dotSpacing, y);
    }

    private void CreateSegments(int totalDots)
    {
        for (int i = 0; i < totalDots - 1; i++)
        {
            var start = GetNodePosition(i);
            var end = GetNodePosition(i + 1);
            var uncompleted = CreateSegmentImage($"UncompletedSegment_{i}", lineSprite, lineColor, start, end);
            var completed = CreateSegmentImage($"CompletedSegment_{i}", completedLineSprite != null ? completedLineSprite : lineSprite, completedLineSprite != null ? Color.white : new Color(1f, 0.78f, 0.18f, 1f), start, end);
            _uncompletedSegmentImages.Add(uncompleted);
            _completedSegmentImages.Add(completed);
        }
    }

    private Image CreateSegmentImage(string name, Sprite sprite, Color fallbackColor, Vector2 start, Vector2 end)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_contentRect, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite != null ? sprite : Sprite.Create(_whiteTex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f);
        image.type = sprite != null ? Image.Type.Simple : Image.Type.Sliced;
        image.preserveAspect = false;
        image.color = sprite != null ? Color.white : fallbackColor;
        image.raycastTarget = false;

        var rt = go.GetComponent<RectTransform>();
        Vector2 delta = end - start;
        float length = delta.magnitude;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = start;
        rt.sizeDelta = new Vector2(length, lineThickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        return image;
    }

    private void CenterContentOnBoard()
    {
        if (_contentRect == null) return;

        float firstWindowWidth = (visibleDots - 1) * dotSpacing;
        float boardWidth = ((RectTransform)transform).rect.width;
        _contentOriginX = (boardWidth - firstWindowWidth) * 0.5f;
        _contentRect.anchoredPosition = new Vector2(_contentOriginX, _contentRect.anchoredPosition.y);
    }

    private void UpdateContentPosition()
    {
        float x = _contentOriginX - _windowStartIndex * dotSpacing;
        _contentRect.anchoredPosition = new Vector2(x, _contentRect.anchoredPosition.y);
    }

    private void UpdateNodeAppearance()
    {
        for (int i = 0; i < _completedSegmentImages.Count; i++)
        {
            bool completed = i < _playerDotIndex;
            _uncompletedSegmentImages[i].gameObject.SetActive(!completed);
            _completedSegmentImages[i].gameObject.SetActive(completed);
        }

        for (int i = 0; i < _nodeDots.Count; i++)
        {
            var img = _nodeDots[i].GetComponent<Image>();
            if (img == null) continue;

            img.color = i < _playerDotIndex
                ? new Color(0.52f, 0.52f, 0.52f, 0.72f)
                : Color.white;
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

    public System.Action OnBossTransitionComplete;

    private void OnBossTransitionCompleted()
    {
        SetVisible(false);
        OnBossTransitionComplete?.Invoke();
    }
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
            _playerDotRect.DOKill();
            _playerDotRect.DOAnchorPos(GetNodePosition(_playerDotIndex), moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(OnBossTransitionCompleted);
            UpdateNodeAppearance();
            return;
        }

        // 需要滚动窗口
        if (_windowStartIndex + visibleDots - 1 >= _totalWaves)
        {
            // 最后一组
            _playerDotIndex = nextIndex;
            _playerDotRect.DOKill();
            _playerDotRect.DOAnchorPos(GetNodePosition(_playerDotIndex), moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(OnBossTransitionCompleted);
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
        seq.AppendCallback(OnBossTransitionCompleted);
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

    private void ScheduleDestroyEditModePreview()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !Application.isPlaying)
                DestroyEditModePreview();
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
        if (this == null || gameObject == null) return;

        DestroyEditModePreview();
        if (this == null || gameObject == null) return;
        EnsureTextures();

        var mask = GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = gameObject.AddComponent<RectMask2D>();
            mask.hideFlags = PreviewFlags;
        }

        BuildVisuals();

        // 仅标记 Content 的子节点为不可编辑，保留 Frame/Content 可选中调整位置
        if (_contentRect != null)
            MarkChildrenOnly(_contentRect, PreviewFlags);

        // 预览节点与连接路段
        if (_contentRect != null)
        {
            for (int i = 0; i < visibleDots - 1; i++)
            {
                var start = GetNodePosition(i);
                var end = GetNodePosition(i + 1);
                var segment = CreateSegmentImage($"UncompletedSegment_{i}", lineSprite, lineColor, start, end);
                segment.gameObject.hideFlags = PreviewFlags;
            }

            for (int i = 0; i < visibleDots; i++)
            {
                var dotGo = CreateNodeDot(i);
                dotGo.hideFlags = PreviewFlags;
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
