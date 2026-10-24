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
    [Tooltip("留空则自动找场景里的路线驱动（实现 IRouteFlow 的组件）")]
    public MonoBehaviour routeRuntime;

    [Tooltip("局内专用节点：就绪态隐藏，进关后按原状态还原")]
    public GameObject[] inRunOnly;

    [Tooltip("落地页专属节点：就绪态显示，关卡态隐藏；overlay 打开时也隐藏")]
    public GameObject[] readyOnly;

    [Tooltip("hub 专属节点：选关/天赋等二级页面打开期间隐藏（头像、金币）")]
    public GameObject[] hubOnly;

    private bool[] _originalActive;
    private bool _captured;
    private bool _appliedReady;

    private bool[] _readyOriginalActive;
    private bool _readyCaptured;

    private bool _hubCaptured;
    private bool _appliedOverlay;
    private bool _subscribed;
    private bool _playPrimed;

    private void OnEnable()
    {
        Capture();
        CaptureReady();
        CaptureHub();
        TrySubscribe();
        ApplyFromState();
    }

    /// <summary>
    /// **重新采样**三个名单的初始状态。
    /// 必须在**编辑期替换名单**（如重构分组）后调用：否则 `_originalActive` 仍是旧名单的下标，
    /// 与新名单错位 → 显隐结果随机。
    /// </summary>
    public void Recapture()
    {
        _captured = false;
        _readyCaptured = false;
        _hubCaptured = false;
        Capture();
        CaptureReady();
        CaptureHub();
        if (!Application.isPlaying) { ApplyEditModeAppearance(); return; }
        _appliedReady = !_appliedReady;
        _appliedOverlay = !_appliedOverlay;
        ApplyFromState();
    }

    /// <summary>编辑模式下的外观：等同「就绪态」——战斗 UI 隐藏、落地页显示、overlay 让位。</summary>
    private void ApplyEditModeAppearance()
    {
        Apply(true);            // 战斗专属 → 隐藏
        ApplyOverlay(false);    // hub 专属 → 显示
        ApplyReadyOnly(true);   // 落地页专属 → 显示
    }

    private void OnDisable()
    {
        if (_subscribed && HubStateSource.Instance != null)
            HubStateSource.Instance.Changed -= OnHubStateChanged;
        _subscribed = false;
    }

    private void Start()
    {
        Capture();
        CaptureReady();
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

    /// <summary>把唯一状态源的两个维度映射成三组显隐；只在真正变化时才改对象。</summary>
    private void ApplyFromState()
    {
        // 编辑模式：显示成**就绪态外观**（战斗 UI 隐藏、落地页显示）——
        // 这样调落地页布局时不会被战斗 UI 叠着看不清。
        if (!Application.isPlaying)
        {
            _playPrimed = false;
            ApplyEditModeAppearance();
            return;
        }

        var state = HubStateSource.Instance;
        bool ready = state != null && state.IsPrepared;
        bool overlayOpen = state != null && state.IsOverlayOpen;

        // 进入 Play 后第一次强制应用：清掉编辑模式留下的预览隐藏，
        // 否则状态恰好等于缓存默认值时，战斗组会一直保持隐藏。
        if (!_playPrimed)
        {
            _playPrimed = true;
            _appliedReady = !ready;
            _appliedOverlay = !overlayOpen;
        }

        if (ready != _appliedReady) Apply(ready);
        if (overlayOpen != _appliedOverlay) ApplyOverlay(overlayOpen);
        // 就绪态专属节点：就绪态显示，且 overlay 打开时让位
        ApplyReadyOnly(ready && !overlayOpen);
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
        if (inRunOnly == null) return;

        // **显式语义**：战斗专属 UI 在关卡态一定显示、就绪态一定隐藏。
        // 不用「恢复原始 active」—— 编辑模式会把就绪态外观写进场景文件，
        // 原始状态就不再可靠（组容器本来应该是激活的）。
        for (int i = 0; i < inRunOnly.Length; i++)
        {
            var go = inRunOnly[i];
            if (go == null) continue;
            SetShown(go, !ready);
        }

        _appliedReady = ready;
    }

    private void CaptureHub()
    {
        if (_hubCaptured || hubOnly == null) return;
        _hubCaptured = true;
    }

    private void CaptureReady()
    {
        if (_readyCaptured || readyOnly == null) return;
        _readyCaptured = true;
        _readyOriginalActive = new bool[readyOnly.Length];
        for (int i = 0; i < readyOnly.Length; i++)
            _readyOriginalActive[i] = readyOnly[i] != null && readyOnly[i].activeSelf;
    }

    /// <summary>就绪态专属：就绪态且没有 overlay 打开时显示，其余隐藏。</summary>
    private void ApplyReadyOnly(bool show)
    {
        if (!_readyCaptured) CaptureReady();
        if (readyOnly == null) return;
        for (int i = 0; i < readyOnly.Length; i++)
        {
            var go = readyOnly[i];
            if (go == null) continue;
            SetShown(go, show);
        }
    }

    /// <summary>二级页面打开/关闭：隐藏或还原 hub 专属内容（头像、金币）。</summary>
    private void ApplyOverlay(bool overlayOpen)
    {
        if (!_hubCaptured) CaptureHub();
        if (hubOnly == null) return;
        for (int i = 0; i < hubOnly.Length; i++)
        {
            var go = hubOnly[i];
            if (go == null) continue;
            SetShown(go, !overlayOpen);
        }

        _appliedOverlay = overlayOpen;
    }

    /// <summary>
    /// 统一显隐入口。
    /// 运行时：SetActive（局内 / 落地页切换，语义不变）。
    /// 编辑模式：**不改 m_IsActive**，否则编辑器预览会随场景保存、开局就少一半 UI；
    ///          改用 DontSaveInEditor 的 CanvasGroup 做预览隐藏，不落盘。
    /// </summary>
    private static void SetShown(GameObject go, bool shown)
    {
        if (Application.isPlaying)
        {
            if (go.activeSelf != shown) go.SetActive(shown);
            SetPreviewAlpha(go, true);   // 清掉编辑模式留下的预览隐藏（仅作用于 DontSave 的预览 CanvasGroup）
            return;
        }
        SetPreviewAlpha(go, shown);
    }

    /// <summary>编辑模式预览：只操作本组件创建的 DontSaveInEditor CanvasGroup，场景原有 CanvasGroup 不动。</summary>
    private static void SetPreviewAlpha(GameObject go, bool shown)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            if (shown) return;
            cg = go.AddComponent<CanvasGroup>();
            cg.hideFlags = HideFlags.DontSaveInEditor;
        }
        if ((cg.hideFlags & HideFlags.DontSaveInEditor) == 0) return;

        cg.alpha = shown ? 1f : 0f;
        cg.blocksRaycasts = shown;
        cg.interactable = shown;
    }
}
