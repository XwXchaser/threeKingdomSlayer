using UnityEngine;

/// <summary>
/// 落地页 / 关卡态的 HUD 切换：就绪态（<see cref="FakeRouteRuntime.IsPrepared"/>）下把「局内专用」的 HUD 整组隐藏，
/// 只留常驻内容（头像、金币）+ hub 内容（开始关卡按钮、底部 tab 栏）；点「开始关卡」后原样恢复。
///
/// 采用「记录进入就绪态前的 active 状态 → 隐藏 → 离开时还原」的做法，而不是强行 SetActive(true)，
/// 因为这些节点里有自己的显隐逻辑（连击数、击杀里程碑、QTE 翻牌…），强行打开会显示不该显示的东西。
/// </summary>
public sealed class HubHudModeSwitch : MonoBehaviour
{
    [Tooltip("留空则自动找场景里的 FakeRouteRuntime")]
    public FakeRouteRuntime routeRuntime;

    [Tooltip("局内专用节点：就绪态隐藏，进关后按原状态还原")]
    public GameObject[] inRunOnly;

    private bool[] _originalActive;
    private bool _captured;
    private bool _appliedReady;

    private void Start()
    {
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        Capture();
        Apply(false);
    }

    private void Update()
    {
        if (routeRuntime == null) routeRuntime = FakeRouteRuntime.Instance;
        bool ready = routeRuntime != null && routeRuntime.IsPrepared;
        if (ready != _appliedReady) Apply(ready);
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
}
