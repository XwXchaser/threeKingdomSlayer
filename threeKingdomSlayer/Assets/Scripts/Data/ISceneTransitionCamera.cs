/// <summary>
/// **场景过渡相机端口**：<c>SceneFlowManager</c> 切场景时要让相机播推入动画，
/// 但它不该依赖具体的 <c>CameraManager</c>（那是界面 / 表现层的东西）。
///
/// 2026-10-22（asmdef 分层第五批）：内核 → 界面硬引用接口化。
/// </summary>
public interface ISceneTransitionCamera
{
    /// <summary>是否已经指定了下一个场景（没有就直接切，不播动画）。</summary>
    bool HasNextScene { get; }

    /// <summary>下一个场景名（推入动画结束后由相机自己 LoadSceneAsync）。</summary>
    string NextScene { get; set; }

    /// <summary>播放离场（推入）动画。</summary>
    void PlayDeparture();
}
