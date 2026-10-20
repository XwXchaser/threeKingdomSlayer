using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 底部 tab 栏（战斗 / 选关 / 天赋 / 家园）——**界面导航**，不是弹窗。
///
/// 职责：四个 tab = 四个界面。「战斗」就是 Battle 场景本体（落地页 / 关卡），
/// 「选关」「天赋」是整页界面（additive 加载的 overlay 场景），它们不是浮在战斗页上的二级窗口，
/// 而是与它同级的页面，所以：
///   • tab 栏在每个界面上都常驻（就绪态可见），自己不知道「当前界面」以外的事情；
///   • 当前 tab 高亮用 image 颜色区分；
///   • 点 tab = 切界面（战斗 → 关掉 overlay；选关/天赋 → SwitchOverlay）。
///
/// 它自身排在一个 overrideSorting 的Canvas（order 50）上，画在选关/天赋页（40/41）之上。
/// 显隐用 CanvasGroup，不 SetActive（关掉自己 Update 就跑不回来了）。
/// </summary>
public sealed class BattleHubTabBar : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("留空则自动找场景里的 FakeRouteRuntime")]
    public FakeRouteRuntime routeRuntime;

    [Tooltip("留空则用 SceneFlowManager.Instance")]
    public SceneFlowManager sceneFlow;

    [Tooltip("整条 tab 栏的 CanvasGroup（控制显隐/点击）")]
    public CanvasGroup group;

    [Header("tab 按钮")]
    public Button battleTab;
    public Button stageSelectTab;
    public Button talentTreeTab;
    public Button homeTab;

    [Header("overlay 场景名（与场景文件名一致）")]
    public string stageSelectScene = "StageSelect";
    public string talentTreeScene = "TalentTree";

    [Header("当前 tab 高亮")]
    public Color activeTabTint = new Color(1f, 0.86f, 0.55f, 1f);
    public Color inactiveTabTint = Color.white;

    private bool _wired;
    private bool _tabApplied;
    private string _appliedTab;

    private void Start()
    {
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        if (group == null) group = GetComponent<CanvasGroup>();
        Wire();
        ApplyState(false);
    }

    private void Wire()
    {
        if (_wired) return;
        _wired = true;

        // 战斗 tab：战斗界面就是本场景，点它 = 关掉叠着的其它界面
        if (battleTab != null)
        {
            battleTab.onClick.RemoveAllListeners();
            battleTab.onClick.AddListener(GoToBattleTab);
        }
        if (stageSelectTab != null)
        {
            stageSelectTab.onClick.RemoveAllListeners();
            stageSelectTab.onClick.AddListener(OpenStageSelect);
        }
        if (talentTreeTab != null)
        {
            talentTreeTab.onClick.RemoveAllListeners();
            talentTreeTab.onClick.AddListener(OpenTalentTree);
        }
        // 家园：占位，明确置灰
        if (homeTab != null)
        {
            homeTab.onClick.RemoveAllListeners();
            homeTab.interactable = false;
        }
    }

    private void Update()
    {
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        Wire();
        // tab 栏是界面导航：只要在就绪态就在，不随 overlay 开关隐藏。
        ApplyState(routeRuntime != null && routeRuntime.IsPrepared);
        ApplyTabHighlight();
    }

    /// <summary>按当前叠着的界面高亮对应 tab（战斗 = 无 overlay）。</summary>
    private void ApplyTabHighlight()
    {
        string current = sceneFlow != null ? sceneFlow.CurrentOverlay : null;
        if (_tabApplied && current == _appliedTab) return;
        _tabApplied = true;
        _appliedTab = current;
        Tint(battleTab, current == null);
        Tint(stageSelectTab, current == stageSelectScene);
        Tint(talentTreeTab, current == talentTreeScene);
        Tint(homeTab, false);
    }

    private void Tint(Button button, bool active)
    {
        if (button == null) return;
        var image = button.targetGraphic as Image;
        if (image != null) image.color = active ? activeTabTint : inactiveTabTint;
    }

    private void ApplyState(bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    /// <summary>切回「战斗」界面：关掉叠着的选关/天赋界面。</summary>
    public void GoToBattleTab()
    {
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        if (sceneFlow == null)
        {
            Debug.LogWarning("[HubTabBar] 找不到 SceneFlowManager，无法切回战斗界面");
            return;
        }
        Debug.Log("[HubTabBar] 切到战斗界面");
        sceneFlow.CloseAllOverlays();
    }

    public void OpenStageSelect()
    {
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        if (sceneFlow == null)
        {
            Debug.LogWarning("[HubTabBar] 找不到 SceneFlowManager，无法打开选关界面");
            return;
        }
        Debug.Log("[HubTabBar] 切到选关界面");
        sceneFlow.SwitchOverlay(stageSelectScene);
    }

    public void OpenTalentTree()
    {
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        if (sceneFlow == null)
        {
            Debug.LogWarning("[HubTabBar] 找不到 SceneFlowManager，无法打开天赋界面");
            return;
        }
        Debug.Log("[HubTabBar] 切到天赋界面");
        sceneFlow.SwitchOverlay(talentTreeScene);
    }
}
