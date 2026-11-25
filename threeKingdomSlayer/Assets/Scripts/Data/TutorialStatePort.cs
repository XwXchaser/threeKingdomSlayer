/// <summary>
/// **教学状态端口**（2026-11-25）——Data 层契约，避免 Flow 反向引用 UI。
///
/// 分层事实：
///   · `TutorialStageDirector` 在 `Locus.UI`（`Assets/Scripts/Tutorial/Locus.UI.asmref`）；
///   · `EndlessRunController` 在 `Locus.Flow`（`Assets/Scripts/Managers/Locus.Flow.asmref`）；
///   · `Locus.UI` 引用 `Locus.Flow`，**反向不成立**。
///
/// 所以 Flow 不能在模式接管时直接调教学导演。由导演在 Awake 里注册这个端口，
/// Flow 只通过 Data 层接口请求「回收所有教学专属状态」。
/// </summary>
public interface ITutorialStateGate
{
    /// <summary>教学会话是否真的在进行（路线是教学路线且未被无尽挂起）。</summary>
    bool IsTutorialSessionActive();

    /// <summary>
    /// 模式接管（无尽 / 非教学流程）时回收所有教学专属状态：
    /// 招式白名单、防御键隐藏 / 静音、强制三选一、教学输入 static、教学时停、教学遮罩。
    /// 幂等。
    /// </summary>
    void ReleaseTutorialOnlyStateForModeTakeover();
}

/// <summary>教学状态端口的注册表（同一时刻只有一个教学导演）。</summary>
public static class TutorialStatePort
{
    private static ITutorialStateGate _gate;

    public static ITutorialStateGate Gate => _gate;
    public static bool HasGate => _gate != null;

    public static void Register(ITutorialStateGate gate)
    {
        if (gate != null) _gate = gate;
    }

    public static void Unregister(ITutorialStateGate gate)
    {
        if (object.ReferenceEquals(_gate, gate)) _gate = null;
    }

    /// <summary>请求回收教学专属状态；没有注册方（无教学导演）时什么都不做。</summary>
    public static void ReleaseForModeTakeover()
    {
        _gate?.ReleaseTutorialOnlyStateForModeTakeover();
    }
}
