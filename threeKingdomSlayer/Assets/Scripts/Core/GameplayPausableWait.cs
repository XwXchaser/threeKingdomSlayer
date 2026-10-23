using UnityEngine;

/// <summary>
/// 暂停感知的实时等待：游戏内容专用。
///
/// 与 <see cref="WaitForSecondsRealtime"/> 的区别：游戏内容暂停（timeScale == 0）时本等待不走时，
/// 恢复后从剩余时间继续。UI 仍应使用 WaitForSecondsRealtime，保证暂停时 UI 继续走时。
/// 实现：CustomYieldInstruction 只有 keepWaiting 会被逐帧轮询，
/// 因此用 <see cref="GameplayPause.UnscaledTime"/>（扣除暂停时长的单调时钟）作为计时基准。
/// </summary>
public sealed class GameplayPausableWait : CustomYieldInstruction
{
    private readonly float _endTime;

    public GameplayPausableWait(float seconds)
    {
        _endTime = GameplayPause.UnscaledTime + Mathf.Max(0f, seconds);
    }

    public override bool keepWaiting => GameplayPause.UnscaledTime < _endTime;
}
