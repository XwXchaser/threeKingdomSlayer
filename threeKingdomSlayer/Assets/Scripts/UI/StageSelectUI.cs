using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 选关页（纯 UI overlay 场景）。
///
/// 场景结构：Canvas(StageSelectUI) → 由本脚本运行时生成节点网格。
/// 交互：点节点 → 写存档「当前关卡」→ 落地页（下层 Battle 场景）当场重载该路线 → 关闭 overlay。
///
/// 与 MainMenuUI 的关系：网格生成逻辑原来在 MainMenuUI 里（但 Start() 从未调用 CreateStageGrid，
/// 属于死代码），现在搬到这里成为唯一实现。
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [Header("数据")]
    [Tooltip("场景中 RouteCatalog 组件上的路线关卡列表。运行时自动查找")]
    public RouteCatalog routeCatalog;

    [Header("标题")]
    public Text titleText;

    [Header("状态提示")]
    public Text statusText;

    [Header("关闭")]
    public Button closeButton;

    [Header("配色")]
    public Color panelColor = new Color(0.16f, 0.18f, 0.24f, 0.96f);
    public Color buttonColor = new Color(0.25f, 0.4f, 0.28f, 1f);
    public Color buttonHighlightColor = new Color(0.35f, 0.55f, 0.38f, 1f);
    public Color buttonPressedColor = new Color(0.18f, 0.3f, 0.2f, 1f);
    public Color buttonLockedColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
    public Color lockedTextColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private List<FakeRouteStageConfig> _routes = new List<FakeRouteStageConfig>();
    private RectTransform _grid;
    private RectTransform _gridLayout;
    private float _uiScale;

    private void Awake()
    {
        _uiScale = UIResolutionHelper.UIScale;
        RefreshStageConfigs();
        BuildGrid();
    }

    private void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(OnCloseClicked);
        }
    }

    private void OnCloseClicked()
    {
        var flow = SceneFlowManager.Instance;
        if (flow != null) flow.RequestCloseOverlay();
        else Debug.LogWarning("[StageSelectUI] 找不到 SceneFlowManager，无法关闭");
    }

    private void RefreshStageConfigs()
    {
        if (routeCatalog == null)
            routeCatalog = RouteCatalog.Resolve();
        if (routeCatalog == null)
        {
            Debug.LogWarning("[StageSelectUI] 找不到 RouteCatalog，选关列表为空");
            return;
        }
        _routes = routeCatalog.GetSelectableRoutes();
    }

    private void BuildGrid()
    {
        if (_grid != null) Destroy(_grid.gameObject);

        // ⚠ 页面根节点默认是 100×100（Unity 新建 RectTransform 的默认值）→
        //   网格是它的子节点（锚点拉伸）→ 网格只有 100 宽 → 只能排一列。
        //   ⇒ 先把根节点撑满画布。
        var rootRt = transform as RectTransform;
        if (rootRt != null)
        {
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
        }

        _grid = new GameObject("StageGrid", typeof(RectTransform)).GetComponent<RectTransform>();
        _grid.SetParent(transform, false);
        _grid.anchorMin = new Vector2(0.06f, 0.14f);
        _grid.anchorMax = new Vector2(0.94f, 0.86f);
        _grid.offsetMin = Vector2.zero;
        _grid.offsetMax = Vector2.zero;

        // ⚠ **LayoutGroup 必须挂在子节点上**（2026-11-16 实测修）：
        //   GridLayoutGroup 会把自己的 RectTransform 压成**内容大小**，
        //   挂在容器上时容器被压成 88×72（刚好一个格子）→ 三个按钮全叠在屏幕正中央。
        //   ⇒ 容器只负责占位（锚点拉伸），排版交给子节点。
        var layoutRt = new GameObject("Grid", typeof(RectTransform)).GetComponent<RectTransform>();
        layoutRt.SetParent(_grid, false);
        // ⚠ 排版子节点必须有**真实高度**（拉伸填满容器）：
        //   高度为 0 时 GridLayoutGroup 只能排一列 → 三个按钮竖着叠。
        layoutRt.anchorMin = new Vector2(0f, 0f);
        layoutRt.anchorMax = new Vector2(1f, 1f);
        layoutRt.pivot = new Vector2(0.5f, 1f);
        layoutRt.offsetMin = Vector2.zero;
        layoutRt.offsetMax = Vector2.zero;
        _gridLayout = layoutRt;

        var grid = layoutRt.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200f * _uiScale, 80f * _uiScale);
        grid.spacing = new Vector2(12f * _uiScale, 12f * _uiScale);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        if (_routes.Count == 0)
        {
            var emptyGo = new GameObject("EmptyHint", typeof(RectTransform));
            emptyGo.transform.SetParent(layoutRt, false);
            var emptyTxt = emptyGo.AddComponent<Text>();
            emptyTxt.text = "未找到路线关卡配置";
            emptyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emptyTxt.fontSize = Mathf.RoundToInt(18f * _uiScale);
            emptyTxt.alignment = TextAnchor.MiddleCenter;
            emptyTxt.color = Color.gray;
            return;
        }

        CreateEndlessButton();
        foreach (var cfg in _routes)
            CreateRouteButton(cfg);

        // ⚠ **层级**：画布上的 Background 图是 Canvas 的子节点，默认盖在网格上 →
        //   网格必须排到 Canvas 的最后（否则整页按钮都看不见，2026-11-16 实测）。
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// **无尽关入口**（2026-11-16）：独立于剧情关的第二种模式（循环刷分 + 局内 build 跨圈保留）。
    /// 放在选关列表第一位（第一行第一格），点击后：关闭选关页 → 建运行时 + HUD → 开跑。
    /// </summary>
    private void CreateEndlessButton()
    {
        var go = new GameObject("EndlessBtn", typeof(RectTransform));
        go.transform.SetParent(_gridLayout != null ? _gridLayout : _grid, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.30f, 0.17f, 0.11f, 0.96f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.18f, 1.1f, 1f);
        colors.pressedColor = new Color(0.8f, 0.76f, 0.72f, 1f);
        btn.colors = colors;

        // 用 TMP（默认字体带中文；选关页其它按钮用的是 legacy Text）
        var labelGo = new GameObject("Text", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(4, 4);
        labelRt.offsetMax = new Vector2(-4, -4);
        var tmp = labelGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.font = UiFontUtility.ResolveCjkFont();
        tmp.text = "[无尽关]\n循环刷分";
        tmp.fontSize = Mathf.RoundToInt(16f * _uiScale);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.86f, 0.55f, 1f);

        btn.onClick.AddListener(OnEndlessSelected);
    }

    private void OnEndlessSelected()
    {
        Debug.Log("[StageSelect] 选择无尽关");
        var controller = EndlessRunController.GetOrCreate();
        var hud = EndlessHud.GetOrCreate();
        hud.Bind(controller);
        // 先关选关页（否则 overlay 会挡住战斗输入），再开跑
        OnCloseClicked();
        controller.StartEndlessRun();
    }

    /// <summary>
    /// 路线解锁 ✅ —— **规则写在数据里** ✅ ✅：
    /// ① <c>RouteCatalogData.alwaysUnlockedStageIds</c> ✅ —— 总是可选 ✅（教学关 ✅ / 第一个正式关 ✅）；
    /// ② 其余关卡要求 <c>prerequisiteStageIds</c> 里的**所有前置都通关** ✅ ✅（可多个 ✅ → 支持分叉 ✅）。
    ///
    /// 旧行为（按列表位置数前面通了几个 ✅）只作为**没配清单资产时的兜底** ✅ ✅ ——
    /// 那个写法中间插一条关卡会连锁改 ✅ ❗。
    /// </summary>
    private int _clearedRouteCount = -1;

    private int ResolveClearedCount()
    {
        if (_clearedRouteCount < 0)
        {
            int cleared = 0;
            for (int i = 0; i < _routes.Count; i++)
                if (SaveManager.IsStageCleared(_routes[i].stageId)) cleared++;
            _clearedRouteCount = cleared;
        }
        return _clearedRouteCount;
    }

    private bool IsStageUnlocked(FakeRouteStageConfig cfg)
    {
        if (cfg == null) return false;
        var data = routeCatalog != null ? routeCatalog.catalogData : null;
        if (data != null) return data.IsUnlocked(cfg, id => SaveManager.IsStageCleared(id));
        // 兜底：旧规则 ✅（保底放行前两条 ✅ —— 教学关 + 第一个正式关 ✅）
        int index = _routes.FindIndex(s => s != null && s.stageId == cfg.stageId);
        return index >= 0 && index <= Mathf.Max(ResolveClearedCount(), 1);
    }

    private void CreateRouteButton(FakeRouteStageConfig cfg)
    {
        var go = new GameObject("RouteBtn_" + cfg.stageId, typeof(RectTransform));
        go.transform.SetParent(_gridLayout != null ? _gridLayout : _grid, false);
        var img = go.AddComponent<Image>();
        img.color = panelColor;
        var btn = go.AddComponent<Button>();
        bool unlocked = IsStageUnlocked(cfg);
        var colors = btn.colors;
        colors.normalColor = buttonColor;
        colors.highlightedColor = buttonHighlightColor;
        colors.pressedColor = buttonPressedColor;
        colors.disabledColor = buttonLockedColor;
        btn.colors = colors;

        var txt = AddLabel(go.transform);
        string status = unlocked
            ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]")
            : "[未解锁]";
        bool isCurrent = SaveManager.GetActiveFakeRouteStageId() == cfg.stageId;
        txt.text = cfg.stageName + "\n" + (isCurrent ? "[当前] " : "") + status;
        txt.color = unlocked ? Color.white : lockedTextColor;

        btn.interactable = unlocked;
        if (unlocked)
        {
            int stageId = cfg.stageId;
            btn.onClick.AddListener(() => OnRouteSelected(stageId));
        }
    }

    private TMPro.TextMeshProUGUI AddLabel(Transform parent)
    {
        // ⚠ **必须用 TMP + 项目中文字体**（2026-11-16 修）：
        //   原来用 legacy Text + Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
        //   ——那是拉丁字体，**中文没有字形** → 按钮上的关卡名全是空白（看上去像“空按钮”）。
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(parent, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);
        var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.font = UiFontUtility.ResolveCjkFont();
        tmp.fontSize = Mathf.RoundToInt(16f * _uiScale);
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    // ────────────────────────── 选择处理 ──────────────────────────

    /// <summary>
    /// 选关：写存档「当前关卡」，并让下层的落地页（Battle 场景里的 FakeRouteRuntime）
    /// 立刻 PrepareForEntry 换成新路线，然后关闭自己。
    /// 旧做法是「只写存档，下次进入生效」，但落地页就在下层场景里，不立即应用会让玩家
    /// 关掉选关页后看到的还是旧关卡。
    /// </summary>
    private void OnRouteSelected(int stageId)
    {
        var cfg = _routes.Find(s => s != null && s.stageId == stageId);
        if (cfg == null) return;
        int previous = SaveManager.GetActiveFakeRouteStageId();
        SaveManager.SetActiveFakeRouteStage(cfg.stageId);

        var runtime = RouteFlowHub.Current;
        if (previous != cfg.stageId)
        {
            if (runtime != null)
            {
                Debug.Log("[StageSelect] 切换当前关卡并重载落地页: " + cfg.stageName);
                runtime.PrepareForEntry(cfg);
            }
            else
            {
                Debug.LogWarning("[StageSelect] 找不到路线驱动（IRouteFlow），仅写存档（下次进入生效）: " + cfg.stageName);
            }
        }
        else
        {
            Debug.Log("[StageSelect] 已是当前关卡: " + cfg.stageName);
        }

        if (statusText != null)
            statusText.text = "已切换到「" + cfg.stageName + "」";

        OnCloseClicked();
    }
}
