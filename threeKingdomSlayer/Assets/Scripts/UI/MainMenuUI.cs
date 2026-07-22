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
    [Tooltip("场景中 StageConfigManager 组件上的关卡列表。运行时自动查找")]
    public StageConfigManager stageConfigManager;
    private List<StageConfig> stageConfigs = new List<StageConfig>();

    [Header("过渡")]
    public CameraManager cameraManager;

    private GameObject stageGrid;
    private float _uiScale;

    private void RefreshStageConfigs()
    {
        if (stageConfigManager == null)
            stageConfigManager = UnityEngine.Object.FindObjectOfType<StageConfigManager>();

        if (stageConfigManager != null && stageConfigManager.stages.Count > 0)
            stageConfigs = new List<StageConfig>(stageConfigManager.stages);
        else
            Debug.LogWarning("[MainMenuUI] 未找到 StageConfigManager 或关卡列表为空，请将 StageConfigManager 添加到场景并配置关卡");
    }

    private void Awake()
    {
        // MainMenuUI 与 StageConfigManager 的 Awake 顺序并不保证；在 Start 时再读取关卡列表。
        RefreshStageConfigs();
    }

    private void Start()
    {
        RefreshStageConfigs();

        UpdateCoinDisplay();
        CreateStageGrid();
        SetupButtons();
    }

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
            emptyTxt.text = "未找到关卡配置\n请选择场景中的 StageConfigManager，在 Inspector 中拖入 StageConfig 资产";
            emptyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            emptyTxt.fontSize = 18;
            emptyTxt.alignment = TextAnchor.MiddleCenter;
            emptyTxt.color = Color.gray;
        }

        foreach (var cfg in stageConfigs)
            CreateStageButton(cfg);
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

        if (!unlocked)
            btn.interactable = false;
        else
        {
            int stageId = cfg.stageId;
            btn.onClick.AddListener(() => OnStageSelected(stageId));
        }
    }

    #endregion

    #region 按钮配置

    private void SetupButtons()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(OnQuitGame);
        }
        RefreshUI();
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
    }

    #endregion

    #region 按钮事件

    private void OnStageSelected(int stageId)
    {
        Debug.Log("[MainMenu] 选择关卡: " + stageId);
        var cfg = stageConfigs.Find(s => s != null && s.stageId == stageId);
        StageController.PendingStageConfig = cfg;
        StartBattle();
    }

    public void OnNewGame()
    {
        Debug.Log("[MainMenu] 新游戏");
        SaveManager.Delete();
        // 新游戏从第一个关卡开始
        StageController.PendingStageConfig = stageConfigs.Count > 0 ? stageConfigs[0] : null;
        RefreshUI();
        StartBattle();
    }

    public void OnContinueGame()
    {
        Debug.Log("[MainMenu] 继续游戏");
        // 从关卡列表中按顺序找第一个未通关的关卡
        StageConfig nextStage = null;
        foreach (var cfg in stageConfigs)
        {
            if (cfg != null && !SaveManager.IsStageCleared(cfg.stageId))
            {
                nextStage = cfg;
                break;
            }
        }
        // 若全部通关，则回到第一关
        if (nextStage == null && stageConfigs.Count > 0)
            nextStage = stageConfigs[0];

        StageController.PendingStageConfig = nextStage;
        StartBattle();
    }

    public void OnDeleteSave()
    {
        Debug.Log("[MainMenu] 删除存档");
        SaveManager.Delete();
        // 重建选关网格（所有关卡变为未解锁状态）
        if (stageGrid != null)
        {
            Destroy(stageGrid);
            stageGrid = null;
        }
        CreateStageGrid();
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
