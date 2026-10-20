using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MainMenuUI : MonoBehaviour
{
    [Header("按钮")]
    public Button newGameButton;
    public Button continueButton;
    public Button deleteSaveButton;
    public Button quitButton;

    [Header("铜钱")]
    public TMP_Text coinText;

    [Header("关卡配置")]
    [Tooltip("场景中 StageConfigManager 组件上的线性关卡列表。运行时自动查找")]
    public StageConfigManager stageConfigManager;
    private List<StageConfig> stageConfigs = new List<StageConfig>();
    private List<RouteStageConfigV2> routeStageConfigsV2 = new List<RouteStageConfigV2>();
    private List<FakeRouteStageConfig> fakeRouteStageConfigs = new List<FakeRouteStageConfig>();

    [Header("过渡")]
    public CameraManager cameraManager;

    [Header("教学关")]
    [Tooltip("新档第一次点“新游戏”时先打这条教学路线（走正式卷轴流程）；留空则跳过教学，直接进正式路线")]
    public FakeRouteStageConfig tutorialRouteConfig;

    [Header("临时选关（调试用）")]
    [Tooltip("打开后主菜单左上角出现「教学关 / 正式关卡」两个按钮，方便直接进关验收。定稿前删掉即可")]
    public bool showDebugStageSelect = true;

    [Header("纯 UI overlay 入口")]
    [Tooltip("主菜单的「选关」按钮。点击后叠加打开 StageSelect 纯 UI 场景（不中断主菜单）。")]
    public Button stageSelectButton;

    [Tooltip("主菜单的「天赋」按钮。点击后叠加打开 TalentTree 纯 UI 场景。")]
    public Button talentTreeButton;

    private GameObject stageGrid;
    private float _uiScale;

    private void RefreshStageConfigs()
    {
        if (stageConfigManager == null)
            stageConfigManager = UnityEngine.Object.FindObjectOfType<StageConfigManager>();

        if (stageConfigManager != null)
        {
            if (stageConfigManager.routeStagesV2.Count > 0)
                routeStageConfigsV2 = new List<RouteStageConfigV2>(stageConfigManager.routeStagesV2);
            if (stageConfigManager.fakeRouteStages.Count > 0)
                fakeRouteStageConfigs = new List<FakeRouteStageConfig>(stageConfigManager.fakeRouteStages);
        }
        if (stageConfigs.Count == 0 && routeStageConfigsV2.Count == 0 && fakeRouteStageConfigs.Count == 0)
            Debug.LogWarning("[MainMenuUI] 未找到线性或路线关卡配置，请配置 StageConfigManager");
    }

    private void Awake()
    {
        // MainMenuUI 与 StageConfigManager 的 Awake 顺序并不保证；在 Start 时再读取关卡列表。
        RefreshStageConfigs();
    }

    private void Start()
    {
        // 主菜单永远不能被上一局残留的暂停冻住（战斗/台词/教学被打断时可能留下 timeScale=0）
        Time.timeScale = 1f;
        RefreshStageConfigs();

        UpdateCoinDisplay();
        SetupButtons();
        CreateDebugStageSelect();
    }

    #region 临时选关（调试用）

    /// <summary>临时入口：左上角两个按钮，直接进教学关或正式路线。定稿时关掉 showDebugStageSelect 即可。</summary>
    private void CreateDebugStageSelect()
    {
        if (!showDebugStageSelect) return;
        if (transform.Find("DebugStageSelectPanel") != null) return;

        float s = UIResolutionHelper.UIScale;
        var panel = new GameObject("DebugStageSelectPanel", typeof(RectTransform));
        var rt = panel.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(24f * s, -24f * s);
        rt.sizeDelta = new Vector2(300f * s, 170f * s);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f * s;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        CreateDebugStageButton(panel.transform, "教学关", s, StartTutorialRoute);
        CreateDebugStageButton(panel.transform, "正式关卡（简单）", s, StartFormalRoute);
        CreateDebugStageButton(panel.transform, "加铜钱 +500", s, AddCoinsForTest);
        Debug.Log("[MainMenu] 临时选关按钮已生成");
    }

    private void CreateDebugStageButton(Transform parent, string label, float s, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("DebugBtn_" + label, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.24f, 0.34f, 0.92f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.26f, 0.34f, 0.5f, 1f);
        colors.highlightedColor = new Color(0.38f, 0.5f, 0.7f, 1f);
        colors.pressedColor = new Color(0.18f, 0.24f, 0.36f, 1f);
        btn.colors = colors;
        go.AddComponent<LayoutElement>().preferredHeight = 64f * s;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);
        var txt = textGo.AddComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = Mathf.RoundToInt(18 * s);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        btn.onClick.AddListener(onClick);
    }

    /// <summary>直接进教学关（不管存档状态）。</summary>
    public void StartTutorialRoute()
    {
        if (tutorialRouteConfig == null)
        {
            Debug.LogWarning("[MainMenu] 未配置教学路线，无法进入教学关");
            return;
        }
        StageController.PendingStageConfig = null;
        StageController.PendingRouteStageConfig = null;
        RouteStageV2Launch.PendingConfig = null;
        FakeRouteLaunch.StartFromCheckpoint = false;
        FakeRouteLaunch.PendingConfig = tutorialRouteConfig;
        SaveManager.ClearFakeRouteSnapshot(tutorialRouteConfig.routeId, tutorialRouteConfig.stageId);
        SaveManager.SetActiveFakeRouteStage(tutorialRouteConfig.stageId);
        Debug.Log("[MainMenu] 临时选关 → 教学关");
        StartBattle();
    }

    /// <summary>临时：点一次加 500 铜钱，用来测试天赋树解锁。</summary>
    public void AddCoinsForTest()
    {
        int total = SaveManager.AddCoins(500);
        UpdateCoinDisplay();
        Debug.Log("[MainMenu] 测试加铜钱 +500 → 当前 " + total);
    }

    /// <summary>直接进正式路线（当前配置已是简化版预览）。</summary>
    public void StartFormalRoute()
    {
        RefreshStageConfigs();
        if (fakeRouteStageConfigs.Count == 0)
        {
            Debug.LogWarning("[MainMenu] 没有正式卷轴路线配置");
            return;
        }
        var cfg = fakeRouteStageConfigs[0];
        StageController.PendingStageConfig = null;
        StageController.PendingRouteStageConfig = null;
        RouteStageV2Launch.PendingConfig = null;
        FakeRouteLaunch.StartFromCheckpoint = false;
        FakeRouteLaunch.PendingConfig = cfg;
        SaveManager.ClearFakeRouteSnapshot(cfg.routeId, cfg.stageId);
        SaveManager.SetActiveFakeRouteStage(cfg.stageId);
        Debug.Log("[MainMenu] 临时选关 → 正式关卡: " + cfg.stageName);
        StartBattle();
    }

    #endregion

    #region 选关网格

    private void CreateStageGrid()
    {
        _uiScale = UIResolutionHelper.UIScale;

        stageGrid = new GameObject("StageGrid", typeof(RectTransform));
        var rt = stageGrid.GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = new Vector2(0.05f, 0.38f);
        rt.anchorMax = new Vector2(0.95f, 0.88f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var grid = stageGrid.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200f * _uiScale, 80f * _uiScale);
        grid.spacing = new Vector2(12f * _uiScale, 12f * _uiScale);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.Flexible;

        if (stageConfigs.Count == 0)
        {
            var emptyGo = new GameObject("EmptyHint", typeof(RectTransform));
            emptyGo.transform.SetParent(stageGrid.transform, false);
            var emptyTxt = emptyGo.AddComponent<Text>();
            emptyTxt.text = "未找到线性关卡配置";
            emptyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emptyTxt.fontSize = 18;
            emptyTxt.alignment = TextAnchor.MiddleCenter;
            emptyTxt.color = Color.gray;
        }

        foreach (var cfg in routeStageConfigsV2)
            CreateRouteStageV2Button(cfg);

        foreach (var cfg in fakeRouteStageConfigs)
            CreateFakeRouteStageButton(cfg);
    }

    /// <summary>卷轴路线关卡按钮（假移动/纯逻辑路线 + 卷轴表现）。</summary>
    private void CreateFakeRouteStageButton(FakeRouteStageConfig cfg)
    {
        var go = new GameObject("FakeRouteStageBtn_" + cfg.stageId, typeof(RectTransform));
        go.transform.SetParent(stageGrid.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.28f, 0.22f, 1f);
        var btn = go.AddComponent<Button>();
        bool unlocked = cfg.stageId <= SaveManager.GetNextAvailableStageId();
        var colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.4f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.38f, 1f);
        colors.pressedColor = new Color(0.18f, 0.3f, 0.2f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
        btn.colors = colors;
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);
        var txt = textGo.AddComponent<Text>();
        string status = unlocked ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]") : "[未解锁]";
        txt.text = cfg.stageName + "\n[卷轴] " + status;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = Mathf.RoundToInt(16 * _uiScale);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        btn.interactable = unlocked;
        if (unlocked)
        {
            int stageId = cfg.stageId;
            btn.onClick.AddListener(() => OnFakeRouteStageSelected(stageId));
        }
    }

    private void CreateRouteStageV2Button(RouteStageConfigV2 cfg)
    {
        var go = new GameObject("RouteStageV2Btn_" + cfg.stageId, typeof(RectTransform));
        go.transform.SetParent(stageGrid.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.28f, 0.22f, 1f);
        var btn = go.AddComponent<Button>();
        bool unlocked = cfg.stageId <= SaveManager.GetNextAvailableStageId();
        var colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.4f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.38f, 1f);
        colors.pressedColor = new Color(0.18f, 0.3f, 0.2f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
        btn.colors = colors;
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);
        var txt = textGo.AddComponent<Text>();
        txt.text = cfg.stageName + "\n[场景化] " + (unlocked ? "[可挑战]" : "[未解锁]");
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = Mathf.RoundToInt(16 * _uiScale);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        btn.interactable = unlocked;
        if (unlocked)
        {
            int stageId = cfg.stageId;
            btn.onClick.AddListener(() => OnRouteStageV2Selected(stageId));
        }
    }

    private void CreateStageButton(StageConfig cfg)
    {
        var go = new GameObject("StageBtn_" + cfg.stageId, typeof(RectTransform));
        go.transform.SetParent(stageGrid.transform, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.22f, 0.18f, 1f);

        var btn = go.AddComponent<Button>();
        int nextAvailable = SaveManager.GetNextAvailableStageId();
        bool unlocked = cfg.stageId <= nextAvailable;

        var colors = btn.colors;
        colors.normalColor = new Color(0.35f, 0.3f, 0.22f, 1f);
        colors.highlightedColor = new Color(0.5f, 0.45f, 0.3f, 1f);
        colors.pressedColor = new Color(0.3f, 0.25f, 0.15f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
        btn.colors = colors;

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);

        var txt = textGo.AddComponent<Text>();
        string status = unlocked ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]") : "[未解锁]";
        txt.text = cfg.stageName + "\n" + status;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = Mathf.RoundToInt(16 * _uiScale);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);

        btn.interactable = unlocked;
        if (unlocked)
        {
            int stageId = cfg.stageId;
            btn.onClick.AddListener(() => OnStageSelected(stageId));
        }
    }

    private void CreateRouteStageButton(RouteStageConfig cfg)
    {
        var go = new GameObject("RouteStageBtn_" + cfg.stageId, typeof(RectTransform));
        go.transform.SetParent(stageGrid.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.28f, 0.22f, 1f);
        var btn = go.AddComponent<Button>();
        int nextAvailable = SaveManager.GetNextAvailableStageId();
        bool unlocked = cfg.stageId <= nextAvailable;
        var colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.4f, 0.28f, 1f);
        colors.highlightedColor = new Color(0.35f, 0.55f, 0.38f, 1f);
        colors.pressedColor = new Color(0.18f, 0.3f, 0.2f, 1f);
        colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.35f);
        btn.colors = colors;
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(4, 4);
        textRt.offsetMax = new Vector2(-4, -4);
        var txt = textGo.AddComponent<Text>();
        string status = unlocked ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]") : "[未解锁]";
        txt.text = cfg.stageName + "\n[路线] " + status;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = Mathf.RoundToInt(16 * _uiScale);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        btn.interactable = unlocked;
        if (unlocked)
        {
            int stageId = cfg.stageId;
            btn.onClick.AddListener(() => OnRouteStageSelected(stageId));
        }
    }



    private void SetupButtons()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitGame);
        }
        if (stageSelectButton != null)
        {
            stageSelectButton.onClick.RemoveAllListeners();
            stageSelectButton.onClick.AddListener(OnStageSelectClicked);
        }
        if (talentTreeButton != null)
        {
            talentTreeButton.onClick.RemoveAllListeners();
            talentTreeButton.onClick.AddListener(OnTalentTreeClicked);
        }
        RefreshUI();
    }

    /// <summary>打开选关页（纯 UI overlay 场景，叠在主菜单之上）。</summary>
    private void OnStageSelectClicked()
    {
        Debug.Log("[MainMenu] 打开选关页");
        OpenOverlayScene(SceneFlowManager.SceneStageSelect);
    }

    /// <summary>打开天赋页（纯 UI overlay 场景）。</summary>
    private void OnTalentTreeClicked()
    {
        Debug.Log("[MainMenu] 打开天赋页");
        OpenOverlayScene(SceneFlowManager.SceneTalentTree);
    }

    private void OpenOverlayScene(string sceneName)
    {
        var flow = SceneFlowManager.Instance;
        if (flow != null)
        {
            flow.OpenOverlay(sceneName);
            return;
        }
        Debug.LogWarning("[MainMenuUI] 找不到 SceneFlowManager，无法打开 overlay: " + sceneName);
    }

    private void RefreshUI()
    {
        bool hasSave = SaveManager.HasSave;

        UpdateCoinDisplay();

        if (newGameButton != null)
            newGameButton.gameObject.SetActive(!hasSave);

        if (continueButton != null)
            continueButton.gameObject.SetActive(hasSave);

        if (deleteSaveButton != null)
            deleteSaveButton.gameObject.SetActive(hasSave);

        if (newGameButton != null)
        {
            newGameButton.onClick.RemoveAllListeners();
            newGameButton.onClick.AddListener(OnNewGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueGame);
        }

        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.RemoveAllListeners();
            deleteSaveButton.onClick.AddListener(OnDeleteSave);
        }

        // 刷新选关按钮的解锁状态
        RefreshStageGrid();
    }

    /// <summary>
    /// 刷新选关网格中每个按钮的解锁状态与文字
    /// </summary>
    private void RefreshStageGrid()
    {
        if (stageGrid == null) return;
        int nextAvailable = SaveManager.GetNextAvailableStageId();

        for (int i = 0; i < stageConfigs.Count; i++)
        {
            var cfg = stageConfigs[i];
            var child = stageGrid.transform.Find("StageBtn_" + cfg.stageId);
            if (child == null) continue;

            var btn = child.GetComponent<Button>();
            var txt = child.GetComponentInChildren<Text>();
            if (btn == null || txt == null) continue;

            bool unlocked = cfg.stageId <= nextAvailable;
            string status = unlocked ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]") : "[未解锁]";
            txt.text = cfg.stageName + "\n" + status;
            txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            btn.interactable = unlocked;

            btn.onClick.RemoveAllListeners();
            if (unlocked)
            {
                int stageId = cfg.stageId;
                btn.onClick.AddListener(() => OnStageSelected(stageId));
            }
        }

        for (int i = 0; i < routeStageConfigsV2.Count; i++)
        {
            var cfg = routeStageConfigsV2[i];
            var child = stageGrid.transform.Find("RouteStageV2Btn_" + cfg.stageId);
            if (child == null) continue;
            var btn = child.GetComponent<Button>();
            var txt = child.GetComponentInChildren<Text>();
            if (btn == null || txt == null) continue;
            bool unlocked = cfg.stageId <= nextAvailable;
            string status = unlocked ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]") : "[未解锁]";
            txt.text = cfg.stageName + "\n[路线] " + status;
            txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            btn.interactable = unlocked;
            btn.onClick.RemoveAllListeners();
            if (unlocked)
            {
                int stageId = cfg.stageId;
                btn.onClick.AddListener(() => OnRouteStageV2Selected(stageId));
            }
        }

        for (int i = 0; i < fakeRouteStageConfigs.Count; i++)
        {
            var cfg = fakeRouteStageConfigs[i];
            var child = stageGrid.transform.Find("FakeRouteStageBtn_" + cfg.stageId);
            if (child == null) continue;
            var btn = child.GetComponent<Button>();
            var txt = child.GetComponentInChildren<Text>();
            if (btn == null || txt == null) continue;
            bool unlocked = cfg.stageId <= nextAvailable;
            string status = unlocked ? (SaveManager.IsStageCleared(cfg.stageId) ? "[已通关]" : "[可挑战]") : "[未解锁]";
            txt.text = cfg.stageName + "\n[卷轴] " + status;
            txt.color = unlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            btn.interactable = unlocked;
            btn.onClick.RemoveAllListeners();
            if (unlocked)
            {
                int stageId = cfg.stageId;
                btn.onClick.AddListener(() => OnFakeRouteStageSelected(stageId));
            }
        }
    }

    #endregion

    #region 按钮事件

    private void OnRouteStageV2Selected(int stageId)
    {
        var cfg = routeStageConfigsV2.Find(s => s != null && s.stageId == stageId);
        StageController.PendingStageConfig = null;
        StageController.PendingRouteStageConfig = null;
        RouteStageV2Launch.StartFromCheckpoint = false;
        RouteStageV2Launch.PendingConfig = cfg;
        StartBattle();
    }

    private void OnStageSelected(int stageId)
    {
        Debug.Log("[MainMenu] 选择关卡: " + stageId);
        var cfg = stageConfigs.Find(s => s != null && s.stageId == stageId);
        StageController.PendingStageConfig = cfg;
        StartBattle();
    }

    private void OnRouteStageSelected(int stageId)
    {
        Debug.Log("[MainMenu] 旧路线入口已停用: " + stageId);
    }

    /// <summary>选择卷轴路线关卡：清快照、从 startNode 开始（不读失败恢复快照）。</summary>
    private void OnFakeRouteStageSelected(int stageId)
    {
        var cfg = fakeRouteStageConfigs.Find(s => s != null && s.stageId == stageId);
        if (cfg == null) return;
        StageController.PendingStageConfig = null;
        StageController.PendingRouteStageConfig = null;
        RouteStageV2Launch.PendingConfig = null;
        FakeRouteLaunch.StartFromCheckpoint = false;
        FakeRouteLaunch.PendingConfig = cfg;
        SaveManager.ClearFakeRouteSnapshot(cfg.routeId, cfg.stageId);
        SaveManager.SetActiveFakeRouteStage(cfg.stageId);
        StartBattle();
    }
    public void OnNewGame()
    {
        Debug.Log("[MainMenu] 新游戏");
        SaveManager.Delete();

        // 新档第一次进游戏先走教学关；教学关本身就是一条正式路线，结算完回主菜单点天赋。
        if (tutorialRouteConfig != null && !SaveManager.Load().tutorialCompleted)
        {
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            FakeRouteLaunch.StartFromCheckpoint = false;
            FakeRouteLaunch.PendingConfig = tutorialRouteConfig;
            SaveManager.ClearFakeRouteSnapshot(tutorialRouteConfig.routeId, tutorialRouteConfig.stageId);
            SaveManager.SetActiveFakeRouteStage(tutorialRouteConfig.stageId);
            Debug.Log("[MainMenu] 进入教学关: " + tutorialRouteConfig.stageName);
            RefreshUI();
            StartBattle();
            return;
        }
        if (fakeRouteStageConfigs.Count > 0)
        {
            var cfg = fakeRouteStageConfigs[0];
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            FakeRouteLaunch.StartFromCheckpoint = false;
            SaveManager.ClearFakeRouteSnapshot(cfg.routeId, cfg.stageId);
            SaveManager.SetActiveFakeRouteStage(cfg.stageId);
            FakeRouteLaunch.PendingConfig = cfg;
        }
        else if (routeStageConfigsV2.Count > 0)
        {
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.StartFromCheckpoint = false;
            FakeRouteLaunch.PendingConfig = null;
            SaveManager.ClearRouteStageSnapshot(routeStageConfigsV2[0].stageId);
            RouteStageV2Launch.PendingConfig = routeStageConfigsV2[0];
        }
        else
        {
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            FakeRouteLaunch.PendingConfig = null;
        }
        RefreshUI();
        StartBattle();
    }

    public void OnContinueGame()
    {
        Debug.Log("[MainMenu] 继续游戏");

        // 教学没通关时，「继续」必须回到教学关：教学路线不在选关列表里，
        // 按 activeFakeRouteStageId 查不到就会掉进第一条正式路线（实测会直接进 YJ）。
        if (tutorialRouteConfig != null && !SaveManager.Load().tutorialCompleted)
        {
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            FakeRouteLaunch.StartFromCheckpoint = false;
            SaveManager.ClearFakeRouteSnapshot(tutorialRouteConfig.routeId, tutorialRouteConfig.stageId);
            SaveManager.SetActiveFakeRouteStage(tutorialRouteConfig.stageId);
            FakeRouteLaunch.PendingConfig = tutorialRouteConfig;
            Debug.Log("[MainMenu] 继续教学关: " + tutorialRouteConfig.stageName);
            StartBattle();
            return;
        }

        if (fakeRouteStageConfigs.Count > 0)
        {
            // 继续游戏：从最后未完成的卷轴关卡重新开始（不读取失败恢复快照）。
            int activeId = SaveManager.GetActiveFakeRouteStageId();
            var cfg = fakeRouteStageConfigs.Find(s => s != null && s.stageId == activeId) ?? fakeRouteStageConfigs[0];
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            FakeRouteLaunch.StartFromCheckpoint = false;
            SaveManager.ClearFakeRouteSnapshot(cfg.routeId, cfg.stageId);
            SaveManager.SetActiveFakeRouteStage(cfg.stageId);
            FakeRouteLaunch.PendingConfig = cfg;
        }
        else if (routeStageConfigsV2.Count > 0)
        {
            var cfg = routeStageConfigsV2[0];
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.StartFromCheckpoint = false;
            FakeRouteLaunch.PendingConfig = null;
            SaveManager.ClearRouteStageSnapshot(cfg.stageId);
            RouteStageV2Launch.PendingConfig = cfg;
        }
        else
        {
            StageController.PendingStageConfig = null;
            StageController.PendingRouteStageConfig = null;
            RouteStageV2Launch.PendingConfig = null;
            FakeRouteLaunch.PendingConfig = null;
        }
        StartBattle();
    }

    public void OnDeleteSave()
    {
        Debug.Log("[MainMenu] 删除存档");
        SaveManager.Delete();
        RefreshUI();
    }

    private void UpdateCoinDisplay()
    {
        if (coinText != null)
            coinText.text = $"总铜钱: {SaveManager.GetCoins()}";
    }

    private void StartBattle()
    {
        if (cameraManager != null)
            cameraManager.PlayDeparture();
        else
        {
            Debug.LogWarning("[MainMenu] CameraManager 未赋值，直接加载");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Battle");
        }
    }

    public void OnQuitGame()
    {
        Debug.Log("[MainMenu] 退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion
}
