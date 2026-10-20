using UnityEngine;

/// <summary>
/// 落地页（hub）的 HUD 可见性总开关，管两个互相独立的维度：
///
/// ① 就绪态 / 关卡态（<see cref="FakeRouteRuntime.IsPrepared"/>）
///    就绪态把「局内专用」的 HUD 整组隐藏，只留常驻内容；点「开始关卡」后原样恢复。
///
/// ② 二级页面开关（<see cref="SceneFlowManager.HasOverlay"/>）
///    选关 / 天赋这类二级全屏页面打开期间，把「hub 专属」内容（头像、金币…）整组隐藏。
///    职责边界：Battle 场景只负责「战斗」这一个 tab 的内容，二级页面打开时它让位；
///    不能靠「被页面盖住」来凑合——BattleHUD 是 Screen Space Overlay 画布，
///    永远画在二级页（Screen Space Camera）之上，不主动隐藏就会压在页面上。
///
/// 两个维度都用「记录原始 active → 隐藏 → 需要时还原」，而不是强行 SetActive(true)：
/// 这些节点里有自己的显隐逻辑（连击数、击杀里程碑、QTE 翻牌…），强行打开会显示不该显示的东西。
/// </summary>
public sealed class HubHudModeSwitch : MonoBehaviour
{
    [Tooltip("留空则自动找场景里的 FakeRouteRuntime")]
    public FakeRouteRuntime routeRuntime;

    [Tooltip("局内专用节点：就绪态隐藏，进关后按原状态还原")]
    public GameObject[] inRunOnly;

    [Tooltip("hub 专属节点：选关/天赋等二级页面打开期间隐藏（头像、金币）")]
    public GameObject[] hubOnly;

    private bool[] _originalActive;
    private bool _captured;
    private bool _appliedReady;

    private bool[] _hubOriginalActive;
    private bool _hubCaptured;
    private bool _appliedOverlay;
    private bool _subscribed;

    private void OnEnable()
    {
        Capture();
        CaptureHub();
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
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        Capture();
        CaptureHub();
        ApplyFromState();
        TrySubscribe();
        if (HubStateSource.Instance == null)
            Debug.LogWarning("[HubHudMode] 场景里没有 HubStateSource，HUD 可见性不会随状态变化");
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        var state = HubStateSource.Instance;
        if (state == null) return;
        state.Changed += OnHubStateChanged;
        _subscribed = true;
    }

    private void OnHubStateChanged() => ApplyFromState();

    /// <summary>把唯一状态源的两个维度映射成两组显隐；只在真正变化时才改对象。</summary>
    private void ApplyFromState()
    {
        var state = HubStateSource.Instance;
        bool ready = state != null && state.IsPrepared;
        if (ready != _appliedReady) Apply(ready);

        bool overlayOpen = state != null && state.IsOverlayOpen;
        if (overlayOpen != _appliedOverlay) ApplyOverlay(overlayOpen);
    }

    private void Capture()
    {
        if (_captured || inRunOnly == null) return;
        _captured = true;
        _originalActive = new bool[inRunOnly.Length];
        for (int i = 0; i < inRunOnly.Length; i++)
            _originalActive[i] = inRunOnly[i] != null && inRunOnly[i].activeSelf;
    }

    private void Apply(bool ready)
    {
        if (!_captured) Capture();
        if (inRunOnly == null || _originalActive == null) return;

        for (int i = 0; i < inRunOnly.Length; i++)
        {
            var go = inRunOnly[i];
            if (go == null) continue;
            bool want = ready ? false : _originalActive[i];
            if (go.activeSelf != want) go.SetActive(want);
        }

        _appliedReady = ready;
        Debug.Log("[HubHudMode] " + (ready ? "就绪态：隐藏局内 HUD" : "关卡态：恢复局内 HUD"));
    }

    private void CaptureHub()
    {
        if (_hubCaptured || hubOnly == null) return;
        _hubCaptured = true;
        _hubOriginalActive = new bool[hubOnly.Length];
        for (int i = 0; i < hubOnly.Length; i++)
            _hubOriginalActive[i] = hubOnly[i] != null && hubOnly[i].activeSelf;
    }

    /// <summary>二级页面打开/关闭：隐藏或还原 hub 专属内容（头像、金币）。</summary>
    private void ApplyOverlay(bool overlayOpen)
    {
        if (!_hubCaptured) CaptureHub();
        if (hubOnly == null || _hubOriginalActive == null) return;

        for (int i = 0; i < hubOnly.Length; i++)
        {
            var go = hubOnly[i];
            if (go == null) continue;
            bool want = overlayOpen ? false : _hubOriginalActive[i];
            if (go.activeSelf != want) go.SetActive(want);
        }

        _appliedOverlay = overlayOpen;
        Debug.Log("[HubHudMode] " + (overlayOpen ? "二级页面打开：隐藏 hub 专属内容（头像/金币）" : "二级页面关闭：恢复 hub 专属内容"));
    }
}
