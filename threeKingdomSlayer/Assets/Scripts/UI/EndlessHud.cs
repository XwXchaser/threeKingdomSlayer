using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// **无限模式 HUD**（2026-11-16 新增）。
///
/// 职责（纯展示 + 两个按钮）：
///   · 顶部信息条：当前圈数 / 总分 / 当前节点名；
///   · 波次结算飘字（+分数）；
///   · 圈结算面板：本圈得分 / 总分 / 历史最佳 + 「继续」「退出」两个按钮。
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
    private TextMeshProUGUI _circleText;
    private TextMeshProUGUI _scoreText;
    private TextMeshProUGUI _nodeText;
    private TextMeshProUGUI _toastText;
    private RectTransform _settlePanel;
    private TextMeshProUGUI _settleTitle;
    private TextMeshProUGUI _settleDetail;
    private Button _continueButton;
    private Button _exitButton;

    private float _toastTimer;
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
        _controller.OnWaveSettled += OnWaveSettled;
        _controller.OnCircleSettled += OnCircleSettled;
        _controller.OnRunEnded += OnRunEnded;
        SetVisible(true);
        RefreshInfo();
    }

    private void Unsubscribe()
    {
        if (_controller == null) return;
        _controller.OnStateChanged -= RefreshInfo;
        _controller.OnWaveSettled -= OnWaveSettled;
        _controller.OnCircleSettled -= OnCircleSettled;
        _controller.OnRunEnded -= OnRunEnded;
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
        infoRt.sizeDelta = new Vector2(560f, 190f);

        _circleText = CreateText(infoRt, "CircleText", "第 1 圈", 46, TextAlignmentOptions.TopLeft, accentColor);
        _circleText.rectTransform.anchoredPosition = new Vector2(28f, -24f);
        _circleText.rectTransform.sizeDelta = new Vector2(500f, 60f);

        _scoreText = CreateText(infoRt, "ScoreText", "分数 0", 40, TextAlignmentOptions.TopLeft, textColor);
        _scoreText.rectTransform.anchoredPosition = new Vector2(28f, -84f);
        _scoreText.rectTransform.sizeDelta = new Vector2(500f, 52f);

        _nodeText = CreateText(infoRt, "NodeText", "", 30, TextAlignmentOptions.TopLeft, new Color(0.8f, 0.78f, 0.72f, 1f));
        _nodeText.rectTransform.anchoredPosition = new Vector2(28f, -136f);
        _nodeText.rectTransform.sizeDelta = new Vector2(520f, 40f);

        // ── 波结算飘字 ──
        _toastText = CreateText(root, "ToastText", "", 56, TextAlignmentOptions.Center, accentColor);
        var toastRt = _toastText.rectTransform;
        toastRt.anchorMin = new Vector2(0.5f, 1f);
        toastRt.anchorMax = new Vector2(0.5f, 1f);
        toastRt.pivot = new Vector2(0.5f, 1f);
        toastRt.anchoredPosition = new Vector2(0f, -320f);
        toastRt.sizeDelta = new Vector2(700f, 80f);
        _toastText.alpha = 0f;

        // ── 圈结算面板 ──
        var settle = CreatePanel(root, "SettlePanel", new Color(0f, 0f, 0f, 0.72f));
        _settlePanel = settle.GetComponent<RectTransform>();
        Stretch(_settlePanel);

        var box = CreatePanel(_settlePanel, "Box", new Color(0.13f, 0.11f, 0.09f, 0.98f));
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(860f, 760f);

        _settleTitle = CreateText(boxRt, "Title", "圈结算", 56, TextAlignmentOptions.Center, accentColor);
        _settleTitle.rectTransform.anchoredPosition = new Vector2(0f, 390f);
        _settleTitle.rectTransform.sizeDelta = new Vector2(760f, 70f);

        // ⚠ 定位口径：这些子物件的 pivot 都是 (0.5,0.5) → anchoredPosition 是**中心**偏移，
        //   想让文字靠左就得自己算（先前直接给 -360 → 整块文字跑到屏幕外，看不见）。
        _settleDetail = CreateText(boxRt, "Detail", "", 38, TextAlignmentOptions.Top, textColor);
        _settleDetail.rectTransform.anchoredPosition = new Vector2(0f, 220f);
        _settleDetail.rectTransform.sizeDelta = new Vector2(720f, 300f);

        _continueButton = CreateButton(boxRt, "ContinueButton", "继续", new Vector2(0f, -150f));
        _continueButton.onClick.AddListener(OnContinueClicked);
        _exitButton = CreateButton(boxRt, "ExitButton", "退出", new Vector2(0f, -260f));
        _exitButton.onClick.AddListener(OnExitClicked);

        _settlePanel.gameObject.SetActive(false);
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
        _circleText.text = $"第 {_controller.Circle} 圈";
        _scoreText.text = $"分数 {_controller.Ledger.TotalScore}";
        _nodeText.text = $"{_controller.CurrentNodeName}  ·  {_controller.CurrentBranchName}";
        // 节点序号：圈结算时 _nodeIndex 会走到节点总数之外 → 夹一下，别出现 "5/4"
        if (_controller.NodeCount > 0)
        {
            int shown = Mathf.Clamp(_controller.NodeIndex + 1, 1, _controller.NodeCount);
            _nodeText.text = $"{_controller.CurrentNodeName}（{shown}/{_controller.NodeCount}）  ·  {_controller.CurrentBranchName}";
        }
        // 控制器不再等玩家选择时，收起结算面板（程序化“继续”也要生效）
        if (!_controller.IsWaitingForCircleChoice && _settlePanel.gameObject.activeSelf)
            _settlePanel.gameObject.SetActive(false);
    }

    private void OnWaveSettled(int gained, int total)
    {
        _toastText.text = $"+{gained}";
        _toastText.alpha = 1f;
        _toastTimer = 1.2f;
    }

    private void OnCircleSettled(int circleGain, int total)
    {
        if (_controller == null) return;
        var data = SaveManager.Load();
        int best = data != null ? data.endlessBestScore : 0;
        _settleTitle.text = $"第 {_controller.Circle} 圈结算";
        _settleDetail.text =
            $"本圈得分  +{circleGain}\n" +
            $"当前总分  {total}\n" +
            $"历史最佳  {best}\n\n" +
            "下一圈敌人更强。";
        _settlePanel.gameObject.SetActive(true);
    }

    private void OnRunEnded(bool improved)
    {
        _settlePanel.gameObject.SetActive(false);
        if (_controller != null)
        {
            _settleTitle.text = "结束";
            _settleDetail.text = improved ? "新纪录！" : "";
        }
    }

    private void OnContinueClicked()
    {
        _settlePanel.gameObject.SetActive(false);
        _controller?.ContinueToNextCircle();
    }

    private void OnExitClicked()
    {
        _settlePanel.gameObject.SetActive(false);
        _controller?.ExitToLanding();
    }

    private void Update()
    {
        if (_toastTimer > 0f)
        {
            _toastTimer -= Time.unscaledDeltaTime;
            _toastText.alpha = Mathf.Clamp01(_toastTimer / 0.5f);
        }
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null) _canvas.enabled = visible;
    }
}
