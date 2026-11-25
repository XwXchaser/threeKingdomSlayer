using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// **无限模式 HUD**（2026-11-16 新增）。
///
/// 职责（纯展示）：
///   · 顶部信息条：总分 / 当前节点名；
///   · 波次结算飘字（+分数）；
///   · **局结束结算面板**（阵亡 / 提前退出共用：本局总分 + 历史最高分）。
///
/// ⚠ **不显示圈数**（2026-11-24 用户定：“圈数也是个错误需求”）：
///   圈只是内部的难度 / 内容复用单位，不进 HUD、不进分数、不进结算。
/// ⚠ **没有圈结算面板**（2026-11-19 用户定：“除非玩家死亡、结束本局，你出这个结算干嘛？”）
///   ⇒ 走完一圈**直接接着走下一圈**，不打断玩家。
/// ⚠ **没有退出按钮**（2026-11-24 用户定：“本来就有暂停菜单的退出方案…纯属违规需求”）
///   ⇒ 退出走**暂停菜单**，HUD 不再提供退出入口。
///
/// 全部**运行时创建**（不碰场景、不需要美术资源）⇒ 不用改 Battle.scene。
/// 数据源是 <see cref="EndlessRunController"/> 的事件，本组件不读任何运行时对象。
/// </summary>
public class EndlessHud : MonoBehaviour
{
    public static EndlessHud Instance { get; private set; }

    /// <summary>取或建 HUD（场景里没有也能用）。</summary>
    public static EndlessHud GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("EndlessHud");
        return go.AddComponent<EndlessHud>();
    }

    [Header("配色")]
    public Color panelColor = new Color(0.08f, 0.07f, 0.06f, 0.72f);
    public Color accentColor = new Color(0.95f, 0.82f, 0.42f, 1f);
    public Color textColor = new Color(0.96f, 0.94f, 0.88f, 1f);

    private Canvas _canvas;
    private TextMeshProUGUI _scoreText;
    private TextMeshProUGUI _nodeText;

    // 局结束结算面板（阵亡 / 提前退出共用）
    private GameObject _settlePanel;
    private CanvasGroup _settleGroup;
    private TextMeshProUGUI _settleScoreText;
    private TextMeshProUGUI _settleBestText;
    private float _settleFadeTimer;
    private bool _settleFreezePending;

    /// <summary>面板淡入时长（2026-11-24 用户定：0.2–0.35 秒）。</summary>
    private const float SettlementFadeSeconds = 0.28f;

    private EndlessRunController _controller;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUi();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unsubscribe();
    }

    /// <summary>绑定到运行时（开始一局时调用）。</summary>
    public void Bind(EndlessRunController controller)
    {
        Unsubscribe();
        _controller = controller;
        if (_controller == null) return;
        _controller.OnStateChanged += RefreshInfo;
        _controller.OnRunEnded += OnRunEnded;
        _controller.Ledger.OnScoreChanged += OnScoreChanged;
        SetVisible(true);
        RefreshInfo();
    }

    private void Unsubscribe()
    {
        if (_controller == null) return;
        _controller.OnStateChanged -= RefreshInfo;
        _controller.OnRunEnded -= OnRunEnded;
        _controller.Ledger.OnScoreChanged -= OnScoreChanged;
        _controller = null;
    }

    // ─────────────────────────── 构建 ───────────────────────────

    private void BuildUi()
    {
        var canvasGo = new GameObject("EndlessHudCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(UIResolutionHelper.ReferenceWidth, UIResolutionHelper.ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var root = canvasGo.GetComponent<RectTransform>();

        // ── 顶部信息条 ──
        var info = CreatePanel(root, "InfoPanel", new Color(panelColor.r, panelColor.g, panelColor.b, panelColor.a));
        var infoRt = info.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0f, 1f);
        infoRt.anchorMax = new Vector2(0f, 1f);
        infoRt.pivot = new Vector2(0f, 1f);
        infoRt.anchoredPosition = new Vector2(24f, -24f);
        infoRt.sizeDelta = new Vector2(560f, 140f);

        _scoreText = CreateText(infoRt, "ScoreText", "分数 0", 46, TextAlignmentOptions.TopLeft, accentColor);
        _scoreText.rectTransform.anchoredPosition = new Vector2(28f, -24f);
        _scoreText.rectTransform.sizeDelta = new Vector2(500f, 60f);

        _nodeText = CreateText(infoRt, "NodeText", "", 30, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.78f, 0.72f, 1f));
        _nodeText.rectTransform.anchoredPosition = new Vector2(28f, -88f);
        _nodeText.rectTransform.sizeDelta = new Vector2(520f, 40f);

        BuildSettlement(root);
    }

    /// <summary>
    /// **局结束结算面板**（2026-11-24）：阵亡与提前退出共用。
    /// 只显示本局总分 + 历史最高分（不显示圈数、不显示铜钱）。
    /// 淡入 0.28 s → 淡入完成后冻结战斗（面板本身走 unscaled，仍可交互）。
    /// </summary>
    private void BuildSettlement(RectTransform root)
    {
        _settlePanel = CreatePanel(root, "SettlementPanel", new Color(0f, 0f, 0f, 0.74f));
        var rt = _settlePanel.GetComponent<RectTransform>();
        Stretch(rt);
        // 全屏遮罩要挡住战斗点击
        var mask = _settlePanel.GetComponent<Image>();
        if (mask != null) mask.raycastTarget = true;

        _settleGroup = _settlePanel.AddComponent<CanvasGroup>();
        _settleGroup.alpha = 0f;

        var card = CreatePanel(rt, "Card", new Color(panelColor.r, panelColor.g, panelColor.b, 0.97f));
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(880f, 560f);

        var title = CreateText(cardRt, "Title", "本局结束", 56, TextAlignmentOptions.Center, accentColor);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -56f);
        titleRt.sizeDelta = new Vector2(820f, 72f);

        _settleScoreText = CreateText(cardRt, "ScoreText", "本局总分 0", 52, TextAlignmentOptions.Center, textColor);
        var scoreRt = _settleScoreText.rectTransform;
        scoreRt.anchorMin = new Vector2(0.5f, 1f);
        scoreRt.anchorMax = new Vector2(0.5f, 1f);
        scoreRt.pivot = new Vector2(0.5f, 1f);
        scoreRt.anchoredPosition = new Vector2(0f, -170f);
        scoreRt.sizeDelta = new Vector2(820f, 70f);

        _settleBestText = CreateText(cardRt, "BestText", "历史最高 0", 40, TextAlignmentOptions.Center,
                                     new Color(0.8f, 0.78f, 0.72f, 1f));
        var bestRt = _settleBestText.rectTransform;
        bestRt.anchorMin = new Vector2(0.5f, 1f);
        bestRt.anchorMax = new Vector2(0.5f, 1f);
        bestRt.pivot = new Vector2(0.5f, 1f);
        bestRt.anchoredPosition = new Vector2(0f, -262f);
        bestRt.sizeDelta = new Vector2(820f, 60f);

        var confirm = CreateButton(cardRt, "ConfirmButton", "返回落地页", new Vector2(0f, -190f));
        confirm.onClick.AddListener(OnSettlementConfirm);

        _settlePanel.SetActive(false);
    }

    private GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return go;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string content, float size,
                                      TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = UiFontUtility.ResolveCjkFont();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private Button CreateButton(RectTransform parent, string name, string label, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.19f, 0.15f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.15f, 1f, 1f);
        colors.pressedColor = new Color(0.75f, 0.72f, 0.68f, 1f);
        btn.colors = colors;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420f, 96f);
        rt.anchoredPosition = position;

        var text = CreateText(rt, "Label", label, 42, TextAlignmentOptions.Center, textColor);
        Stretch(text.rectTransform);
        return btn;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────── 事件 ───────────────────────────

    private void RefreshInfo()
    {
        if (_controller == null) return;
        _scoreText.text = $"分数 {_controller.Ledger.TotalScore}";
        _nodeText.text = $"{_controller.CurrentNodeName}  ·  {_controller.CurrentBranchName}";
        // 节点序号：圈结算时 _nodeIndex 会走到节点总数之外 → 夹一下，别出现 "5/4"
        if (_controller.NodeCount > 0)
        {
            int shown = Mathf.Clamp(_controller.NodeIndex + 1, 1, _controller.NodeCount);
            _nodeText.text = $"{_controller.CurrentNodeName}（{shown}/{_controller.NodeCount}）  ·  {_controller.CurrentBranchName}";
        }
    }

    /// <summary>分数变化（击杀 / 重开）→ 数字立即刷新。</summary>
    private void OnScoreChanged(int total)
    {
        if (_scoreText != null) _scoreText.text = $"分数 {total}";
    }

    private void OnRunEnded(bool improved)
    {
        // 局结束（阵亡 / 主动退出）→ 结算面板（2026-11-24 用户定：两种走同一个面板，只显示分数）。
        int total = _controller != null ? _controller.Ledger.TotalScore : 0;
        var data = SaveManager.Load();
        int best = data != null ? data.endlessBestScore : total;
        bool newRecord = _controller != null && _controller.LastRunSetRecord;

        if (_settleScoreText != null) _settleScoreText.text = $"本局总分 {total}";
        if (_settleBestText != null)
            _settleBestText.text = newRecord ? $"历史最高 {best}  ·  新纪录！" : $"历史最高 {best}";

        if (_settlePanel != null)
        {
            _settlePanel.SetActive(true);
            _settleGroup.alpha = 0f;
            _settleFadeTimer = SettlementFadeSeconds;
            _settleFreezePending = true;
        }

        Debug.Log($"[EndlessHud] 结算：本局 {total}，最高 {best}，新纪录={newRecord}");
    }

    private void OnSettlementConfirm()
    {
        Time.timeScale = 1f;
        _settleFadeTimer = 0f;
        _settleFreezePending = false;
        if (_settleGroup != null) _settleGroup.alpha = 0f;
        if (_settlePanel != null) _settlePanel.SetActive(false);

        // 回落地页（重载 Battle 场景）。本局已在 EndRun 里结束并落盘，这里只负责回场。
        if (StageController.Instance != null) StageController.Instance.GoToMainMenu();
        else Debug.LogWarning("[EndlessHud] 找不到 StageController，无法回落地页");
    }

    private void Update()
    {
        if (_settleFadeTimer > 0f)
        {
            _settleFadeTimer -= Time.unscaledDeltaTime;
            if (_settleGroup != null)
                _settleGroup.alpha = Mathf.Clamp01(1f - _settleFadeTimer / SettlementFadeSeconds);

            // 淡入完成后才冻结：先让阵亡 / 退出的那一下表现播完，再定住场面
            if (_settleFadeTimer <= 0f && _settleFreezePending)
            {
                _settleFreezePending = false;
                Time.timeScale = 0f;
            }
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null) _canvas.enabled = visible;
    }
}
