using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// **无限模式 HUD**（2026-11-16 新增）。
///
/// 职责（纯展示 + 一个退出按钮）：
///   · 顶部信息条：当前圈数 / 总分 / 当前节点名；
///   · 波次结算飘字（+分数）；
///   · 局结束飘字（最终分 / 新纪录）。
///
/// ⚠ **没有圈结算面板**（2026-11-19 用户定：“除非玩家死亡、结束本局，你出这个结算干嘛？”）
///   ⇒ 走完一圈**直接接着走下一圈**，不打断玩家；
///   ⇒ 退出入口改成顶部信息条里的**常驻小按钮**（否则玩家会被困在这一局里）。
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
        _controller.OnRunEnded += OnRunEnded;
        SetVisible(true);
        RefreshInfo();
    }

    private void Unsubscribe()
    {
        if (_controller == null) return;
        _controller.OnStateChanged -= RefreshInfo;
        _controller.OnWaveSettled -= OnWaveSettled;
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

        // ── 退出（常驻小按钮，挂在画面右上角）──
        //   圈结算面板没了，但玩家仍然需要能退出这一局 ⇒ 做成常驻按钮。
        _exitButton = CreateButton(root, "ExitButton", "退出", Vector2.zero);
        var exitRt = _exitButton.GetComponent<RectTransform>();
        exitRt.anchorMin = new Vector2(1f, 1f);
        exitRt.anchorMax = new Vector2(1f, 1f);
        exitRt.pivot = new Vector2(1f, 1f);
        exitRt.anchoredPosition = new Vector2(-24f, -24f);
        exitRt.sizeDelta = new Vector2(160f, 72f);
        var exitLabel = _exitButton.GetComponentInChildren<TextMeshProUGUI>();
        if (exitLabel != null) exitLabel.fontSize = 36f;
        _exitButton.onClick.AddListener(OnExitClicked);
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
    }

    private void OnWaveSettled(int gained, int total)
    {
        _toastText.text = $"+{gained}";
        _toastText.alpha = 1f;
        _toastTimer = 1.2f;
    }

    private void OnRunEnded(bool improved)
    {
        // 局结束（阵亡 / 主动退出）：用飘字报最终分，不弹面板。
        var data = SaveManager.Load();
        int best = data != null ? data.endlessBestScore : 0;
        _toastText.text = improved ? $"本局 {_controller.Ledger.TotalScore}  ·  新纪录！" : $"本局 {_controller.Ledger.TotalScore}";
        _toastText.alpha = 1f;
        _toastTimer = 3f;
        if (improved) Debug.Log("[EndlessHud] 新纪录 " + best);
    }

    private void OnExitClicked()
    {
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
