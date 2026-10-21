using System.Diagnostics;

/// <summary>
/// 条件编译日志 — 仅在 Editor 下输出，Build 中完全剥离（零开销）
/// </summary>
public static class DebugLog
{
    [Conditional("UNITY_EDITOR")]
    public static void Info(string message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    public static void Warning(string message)
    {
        UnityEngine.Debug.LogWarning(message);
    }
}
