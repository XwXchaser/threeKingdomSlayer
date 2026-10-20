using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页的「开始关卡」大按钮。
///
/// 只在「落地页就绪态且没有二级页面打开」时可见可点：点一下 → <see cref="FakeRouteRuntime.RunPrepared"/>
/// 正式开跑路线。其余状态（加载中 / 跑关卡中 / 已结算 / 选关或天赋页打开）整块隐藏，
/// 不遮挡二级页面，也不会误触重复启动。
/// 用 CanvasGroup 控制显隐而不是 SetActive：物体自己关掉后 Update 就再也不会跑回来。
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class BattleStartStageButton : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("留空则自动找场景里的 FakeRouteRuntime")]
    public FakeRouteRuntime routeRuntime;

    [Tooltip("按钮上的 TMP 文字（优先）")]
    public TMPro.TextMeshProUGUI tmpLabel;

    [Tooltip("按钮上的 Text 文字（没有 TMP 时才用）")]
    public Text label;

    [Header("文案")]
    public string readyLabel = "开始关卡";

    [Header("显隐")]
    [Tooltip("非就绪态的 CanvasGroup alpha")]
    [Range(0f, 1f)] public float hiddenAlpha = 0f;

    private Button _button;
    private CanvasGroup _group;
    private string _appliedLabel;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _group = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
        ApplyState(false);
    }

    private void Update()
    {
        // routeRuntime 可能比本按钮晚 Awake（FakeRouteRuntime 在 Battle 场景根节点），每帧兜底取一次。
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        // 选关/天赋这类二级页面打开时，落地页的一切都不该露出来。
        ApplyState(routeRuntime != null && routeRuntime.IsPrepared && !IsOverlayOpen());
    }

    private static bool IsOverlayOpen()
    {
        var flow = SceneFlowManager.Instance;
        return flow != null && flow.HasOverlay;
    }

    private void ApplyState(bool ready)
    {
        if (_button != null) _button.interactable = ready;

        if (_group != null)
        {
            _group.alpha = ready ? 1f : hiddenAlpha;
            _group.blocksRaycasts = ready;
            _group.interactable = ready;
        }

        if (!ready) return;
        if (_appliedLabel == readyLabel) return;
        _appliedLabel = readyLabel;
        if (tmpLabel != null) tmpLabel.text = readyLabel;
        else if (label != null) label.text = readyLabel;
    }

    private void OnClick()
    {
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        if (routeRuntime == null)
        {
            Debug.LogWarning("[BattleStartStageButton] 找不到 FakeRouteRuntime");
            return;
        }
        Debug.Log("[BattleStartStageButton] 玩家点「开始关卡」");
        routeRuntime.RunPrepared();
    }
}
