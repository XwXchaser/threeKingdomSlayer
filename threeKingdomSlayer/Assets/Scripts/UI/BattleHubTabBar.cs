using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页底部 tab 栏（战斗 / 选关 / 天赋 / 家园）。
///
/// 只在「落地页就绪态且没有二级页面打开」时可见可点：开打后或选关/天赋页打开时整条隐藏，
/// 不遮挡技能键、QTE 板与二级页面。显隐用 CanvasGroup，不 SetActive（关掉自己 Update 就跑不回来了）。
/// tab 按钮用代码 AddListener 绑到本组件的公开方法，字段本身仍在 Inspector 里拖着，可追踪。
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

    private bool _wired;

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

        // 战斗 tab：已在落地页，点它只做视觉反馈（保持可点，避免看起来像坏了）
        if (battleTab != null)
        {
            battleTab.onClick.RemoveAllListeners();
            battleTab.onClick.AddListener(() => Debug.Log("[HubTabBar] 已在战斗 tab"));
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
        // 二级页面（选关/天赋）打开时整条 tab 栏让位：否则它会压在页面上并误触。
        bool overlayOpen = sceneFlow != null && sceneFlow.HasOverlay;
        ApplyState(routeRuntime != null && routeRuntime.IsPrepared && !overlayOpen);
    }

    private void ApplyState(bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    public void OpenStageSelect()
    {
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        if (sceneFlow == null)
        {
            Debug.LogWarning("[HubTabBar] 找不到 SceneFlowManager，无法打开选关页");
            return;
        }
        Debug.Log("[HubTabBar] 打开选关 overlay");
        sceneFlow.OpenOverlay(stageSelectScene);
    }

    public void OpenTalentTree()
    {
        if (sceneFlow == null) sceneFlow = SceneFlowManager.Instance;
        if (sceneFlow == null)
        {
            Debug.LogWarning("[HubTabBar] 找不到 SceneFlowManager，无法打开天赋页");
            return;
        }
        Debug.Log("[HubTabBar] 打开天赋 overlay");
        sceneFlow.OpenOverlay(talentTreeScene);
    }
}
