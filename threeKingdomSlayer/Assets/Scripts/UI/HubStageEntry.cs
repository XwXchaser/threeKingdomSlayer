using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页右侧的**关卡入口**（景色不规则形底图 + TMP 关卡名 + 星级 + 「选关」提示）。
///
/// 设计：`plan/hub-layout-rework-20261024.md`（参考「露米娅大陆」参考图的排布）。
/// 位置与尺寸由 `HubLayoutStageWindow`（Tools/落地页布局台）现场拖拽调整，这里不写死坐标。
/// </summary>
public class HubStageEntry : MonoBehaviour
{
    [Header("贴图")]
    public Sprite baseSprite;
    public Sprite starEmpty;
    public Sprite starHalf;
    public Sprite starFull;

    [Header("引用")]
    public Image baseImage;
    public TMP_Text stageNameText;
    public TMP_Text hintText;
    public Image[] stars = new Image[0];

    [Header("交互")]
    public Button button;
    public string stageSelectScene = "StageSelect";

    [Header("调试")]
    [Tooltip("留空则读存档里的「当前关卡」。")]
    public FakeRouteStageConfig overrideStage;

    [Header("文案")]
    public string hintLabel = "选关";

    private void OnEnable()
    {
        Bind();      // 运行时接线：编辑器里加的 onClick 监听不会被序列化进场景
        Refresh();
    }

    /// <summary>刷新成当前关卡的样子（名字 / 底图 / 星级）。</summary>
    public void Refresh()
    {
        var stage = ResolveStage();
        if (stageNameText != null)
            stageNameText.text = stage != null ? stage.stageName : "—";
        if (baseImage != null && baseSprite != null)
            baseImage.sprite = baseSprite;
        if (hintText != null)
            hintText.text = hintLabel;
        ApplyStars(stage);
    }

    private FakeRouteStageConfig ResolveStage()
    {
        if (overrideStage != null) return overrideStage;
        var catalog = RouteCatalog.Resolve();
        if (catalog == null) return null;
        int activeId = SaveManager.GetActiveFakeRouteStageId();
        var routes = catalog.GetSelectableRoutes();
        for (int i = 0; i < routes.Count; i++)
            if (routes[i] != null && routes[i].stageId == activeId)
                return routes[i];
        return routes.Count > 0 ? routes[0] : null;
    }

    /// <summary>星级：**目前项目没有评星数据**，先用「已通关 = 满星、未通关 = 空心」占位。</summary>
    private void ApplyStars(FakeRouteStageConfig stage)
    {
        if (stars == null || stars.Length == 0) return;
        bool cleared = stage != null && SaveManager.IsStageCleared(stage.stageId);
        for (int i = 0; i < stars.Length; i++)
        {
            if (stars[i] == null) continue;
            stars[i].sprite = cleared
                ? (starFull != null ? starFull : stars[i].sprite)
                : (starEmpty != null ? starEmpty : stars[i].sprite);
        }
    }

    private void OnClick()
    {
        var flow = SceneFlowManager.Instance;
        if (flow == null)
        {
            Debug.LogWarning("[HubStageEntry] 找不到 SceneFlowManager，无法打开选关界面");
            return;
        }
        flow.SwitchOverlay(stageSelectScene);
    }

    /// <summary>给按钮接线（运行时调用；编辑器里加的监听不会被保存）。</summary>
    public void Bind()
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }
}
