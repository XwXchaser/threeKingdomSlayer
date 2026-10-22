using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 落地页的「开始关卡」大按钮。
///
/// 只在「落地页就绪态且没有界面打开」时可见可点：点一下 → <see cref="FakeRouteRuntime.RunPrepared"/>
/// 正式开跑路线。其余状态（加载中 / 跑关卡中 / 已结算 / 选关或天赋界面打开）整块隐藏，
/// 不遮挡界面，也不会误触重复启动。
/// 状态由 <see cref="HubStateSource"/> 单一计算后事件广播，本组件不再自己每帧轮询。
/// 用 CanvasGroup 控制显隐而不是 SetActive：物体自己关掉后订阅也一并断掉。
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class BattleStartStageButton : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("留空则自动找场景里的路线驱动（实现 IRouteFlow 的组件）")]
    public MonoBehaviour routeRuntime;

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

    private bool _subscribed;

    private void OnEnable()
    {
        TrySubscribe();
        ApplyFromState();
    }

    private void OnDisable()
    {
        if (_subscribed && HubStateSource.Instance != null)
            HubStateSource.Instance.Changed -= OnHubStateChanged;
        _subscribed = false;
    }

    private void Start()
    {
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
        // 初始态由状态源决定，不写死 false（否则 OnEnable 已应用的正确态会被覆掉，下一帧再恢复 = 一帧闪烁）
        ApplyFromState();
        TrySubscribe();
        if (HubStateSource.Instance == null)
            Debug.LogWarning("[BattleStartStageButton] 场景里没有 HubStateSource，按钮会一直停在隐藏态");
    }

    /// <summary>订阅唯一状态源。Enable 阶段源可能还没 Awake，所以 Start 再试一次。</summary>
    private void TrySubscribe()
    {
        if (_subscribed) return;
        var state = HubStateSource.Instance;
        if (state == null) return;
        state.Changed += OnHubStateChanged;
        _subscribed = true;
    }

    private void OnHubStateChanged() => ApplyFromState();

    /// <summary>就绪 且 没有界面叠在上层 = 可显示。</summary>
    private void ApplyFromState()
    {
        var state = HubStateSource.Instance;
        ApplyState(state != null && state.IsPrepared && !state.IsOverlayOpen);
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
        var flow = routeRuntime as IRouteFlow ?? RouteFlowHub.Current;
        if (flow == null)
        {
            Debug.LogWarning("[BattleStartStageButton] 找不到路线驱动（IRouteFlow）");
            return;
        }
        Debug.Log("[BattleStartStageButton] 玩家点「开始关卡」");
        flow.RunPrepared();
    }
}
