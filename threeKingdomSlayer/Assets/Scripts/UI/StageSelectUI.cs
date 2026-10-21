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
    [Tooltip("场景中 StageConfigManager 组件上的路线关卡列表。运行时自动查找")]
    public StageConfigManager stageConfigManager;

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
        if (stageConfigManager == null)
            stageConfigManager = FindObjectOfType<StageConfigManager>();
        if (stageConfigManager == null)
        {
            Debug.LogWarning("[StageSelectUI] 找不到 StageConfigManager，选关列表为空");
            return;
        }
        _routes = stageConfigManager.GetSelectableRoutes();
    }

    private void BuildGrid()
    {
        if (_grid != null) Destroy(_grid.gameObject);

        _grid = new GameObject("StageGrid", typeof(RectTransform)).GetComponent<RectTransform>();
        _grid.SetParent(transform, false);
        _grid.anchorMin = new Vector2(0.06f, 0.14f);
        _grid.anchorMax = new Vector2(0.94f, 0.86f);
        _grid.offsetMin = Vector2.zero;
        _grid.offsetMax = Vector2.zero;

        var grid = _grid.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200f * _uiScale, 80f * _uiScale);
        grid.spacing = new Vector2(12f * _uiScale, 12f * _uiScale);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        if (_routes.Count == 0)
        {
            var emptyGo = new GameObject("EmptyHint", typeof(RectTransform));
            emptyGo.transform.SetParent(_grid, false);
            var emptyTxt = emptyGo.AddComponent<Text>();
            emptyTxt.text = "未找到路线关卡配置";
            emptyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emptyTxt.fontSize = Mathf.RoundToInt(18f * _uiScale);
            emptyTxt.alignment = TextAnchor.MiddleCenter;
            emptyTxt.color = Color.gray;
            return;
        }

        foreach (var cfg in _routes)
            CreateRouteButton(cfg, ResolveUnlock());
    }

    /// <summary>
    /// 路线解锁：按列表顺序推进（教学关 → 正式关 → …），已通关 N 条则前 N+1 条可选。
    /// 不能用 stageId 数字比大小——教学关 stageId=9100 会被永远判为未解锁。
    ///
    /// 至少放行前两条（教学关 index 0 + 第一个正式关 index 1）：旧规则下教学关未通关时
    /// 正式关永远锁死，打不过教学关的玩家就卡死了（BUG-002，2026-10-21 实测）。
    /// </summary>
    private int _clearedRouteCount = -1;

    private int ResolveUnlock()
    {
        if (_clearedRouteCount < 0)
        {
            int cleared = 0;
            for (int i = 0; i < _routes.Count; i++)
                if (SaveManager.IsStageCleared(_routes[i].stageId)) cleared++;
            _clearedRouteCount = cleared;
        }
        // 保底 1：即使一条没通关，也放行前两条（教学关 + 第一个正式关）。
        return Mathf.Max(_clearedRouteCount, 1);
    }

    private void CreateRouteButton(FakeRouteStageConfig cfg, int clearedCount)
    {
        var go = new GameObject("RouteBtn_" + cfg.stageId, typeof(RectTransform));
        go.transform.SetParent(_grid, false);
        var img = go.AddComponent<Image>();
        img.color = panelColor;
        var btn = go.AddComponent<Button>();
        int index = _routes.FindIndex(s => s != null && s.stageId == cfg.stageId);
        bool unlocked = index >= 0 && index <= clearedCount;
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

    private Text AddLabel(Transform parent)
    {
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(parent, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);
        var txt = textGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = Mathf.RoundToInt(16f * _uiScale);
        txt.alignment = TextAnchor.MiddleCenter;
        return txt;
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

        var runtime = FakeRouteRuntime.Instance;
        if (previous != cfg.stageId)
        {
            if (runtime != null)
            {
                Debug.Log("[StageSelect] 切换当前关卡并重载落地页: " + cfg.stageName);
                runtime.PrepareForEntry(cfg);
            }
            else
            {
                Debug.LogWarning("[StageSelect] 找不到 FakeRouteRuntime，仅写存档（下次进入生效）: " + cfg.stageName);
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
