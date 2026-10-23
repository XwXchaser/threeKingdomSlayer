using UnityEngine;

/// <summary>
/// 把 <see cref="Time.timeScale"/> 的 0 / 非 0 同步给 <see cref="GameplayPause"/>。
///
/// 任何一处把 timeScale 置 0（升级三选一、暂停菜单、对话、教学引导、选关 overlay）
/// 都会自动冻结「游戏内容」，调用点不需要各自维护暂停开关。
/// UI 不受影响，仍按 unscaled 时间运行，保证弹窗可交互。
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class GameplayPauseWatcher : MonoBehaviour
{
    private static GameplayPauseWatcher s_instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (s_instance != null) return;
        var go = new GameObject("[GameplayPauseWatcher]");
        go.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(go);
        s_instance = go.AddComponent<GameplayPauseWatcher>();
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(this);
            return;
        }
        s_instance = this;
        GameplayPause.Reset();
    }

    private void Update()
    {
        GameplayPause.SetPaused(Time.timeScale <= 0f);
    }
}
