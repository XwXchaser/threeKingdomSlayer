using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 全局「游戏内容暂停门」。
///
/// 背景：升级三选一、暂停菜单、对话、教学引导都会把 <see cref="Time.timeScale"/> 置 0。
/// 普通 DOTween Tween 与普通 Animator 会跟着 timeScale 一起停，
/// 但显式 <c>SetUpdate(true)</c>（unscaled）的 Tween、以及读 <see cref="Time.unscaledDeltaTime"/> 的协程不会停，
/// 于是「敌人死亡动画 / 战斗特效 / 伤害数字 / 镜头反馈」会在暂停期间继续播放。
///
/// 规则：
///   - 游戏内容（战斗表现）用 <see cref="Bind{T}(T)"/> 登记 unscaled Tween，用 <see cref="UnscaledDelta"/>、
///     <see cref="UnscaledTime"/> 计时 → 暂停时全部冻结。
///   - UI（三选一弹窗、HUD、QTE 面板、对话等）继续用 unscaled 运行，保证暂停时仍可交互。
///   - 暂停状态由 <see cref="GameplayPauseWatcher"/> 依据 timeScale 自动同步，调用点不需要手动维护。
/// </summary>
public static class GameplayPause
{
    private static readonly List<Tween> s_tweens = new List<Tween>();
    private static readonly List<Tween> s_pausedByGate = new List<Tween>();

    private static bool s_isPaused;
    private static float s_pausedAccumulated;
    private static float s_pauseStartedAt;

    /// <summary>当前是否处于「游戏内容暂停」（timeScale == 0）。</summary>
    public static bool IsPaused => s_isPaused;

    /// <summary>游戏内容使用的 unscaled 步进：暂停时为 0，其余等于 Time.unscaledDeltaTime。</summary>
    public static float UnscaledDelta => s_isPaused ? 0f : Time.unscaledDeltaTime;

    /// <summary>
    /// 游戏内容使用的单调时钟：与 Time.unscaledTime 等价，但扣掉了所有暂停时长。
    /// 用于「按真实时间算寿命 / 冷却」的场景，暂停期间不流逝。
    /// </summary>
    public static float UnscaledTime
    {
        get
        {
            if (!s_isPaused) return Time.unscaledTime - s_pausedAccumulated;
            return s_pauseStartedAt - s_pausedAccumulated;
        }
    }

    /// <summary>登记一个「游戏内容」的 unscaled Tween：暂停时冻结，恢复时从原处继续。</summary>
    /// <remarks>UI 动画不要登记，否则弹窗会在暂停期间被冻住。</remarks>
    public static T Bind<T>(T tween) where T : Tween
    {
        if (tween == null) return tween;
        Prune();
        s_tweens.Add(tween);
        if (s_isPaused)
        {
            tween.Pause();
            s_pausedByGate.Add(tween);
        }
        return tween;
    }

    /// <summary>切换暂停状态（由 GameplayPauseWatcher 调用）。</summary>
    public static void SetPaused(bool paused)
    {
        if (s_isPaused == paused) return;
        s_isPaused = paused;
        if (paused)
        {
            s_pauseStartedAt = Time.unscaledTime;
            PauseRegistered();
        }
        else
        {
            s_pausedAccumulated += Time.unscaledTime - s_pauseStartedAt;
            ResumeRegistered();
        }
    }

    /// <summary>清空登记（Play Mode 重入 / 状态复位用）。</summary>
    internal static void Reset()
    {
        s_isPaused = false;
        s_pausedAccumulated = 0f;
        s_pauseStartedAt = 0f;
        s_tweens.Clear();
        s_pausedByGate.Clear();
    }

    private static void PauseRegistered()
    {
        s_pausedByGate.Clear();
        for (int i = 0; i < s_tweens.Count; i++)
        {
            Tween t = s_tweens[i];
            if (!IsUsable(t)) continue;
            // 已经被卡肉等逻辑暂停的 Tween 不接管：恢复时交给原逻辑继续，避免双重恢复打架。
            if (!t.IsPlaying()) continue;
            t.Pause();
            s_pausedByGate.Add(t);
        }
    }

    private static void ResumeRegistered()
    {
        for (int i = 0; i < s_pausedByGate.Count; i++)
        {
            Tween t = s_pausedByGate[i];
            if (!IsUsable(t))
            {
                // 目标在暂停期间被销毁：直接结束，避免恢复瞬间访问已销毁对象。
                if (t != null && t.IsActive()) t.Kill(false);
                continue;
            }
            t.Play();
        }
        s_pausedByGate.Clear();
    }

    private static void Prune()
    {
        for (int i = s_tweens.Count - 1; i >= 0; i--)
        {
            Tween t = s_tweens[i];
            if (t == null || !t.IsActive() || !IsTargetAlive(t))
                s_tweens.RemoveAt(i);
        }
    }

    /// <summary>Tween 目标是否还活着（DOTween 不会自动杀掉目标已销毁的 Tween，需要我们自己判断）。</summary>
    private static bool IsTargetAlive(Tween t)
    {
        object raw = t.target;
        if (raw == null) return true;                 // DOTween.To 之类没有 Unity 目标，交给 DOTween 自己处理
        if (raw is UnityEngine.Object unityTarget) return unityTarget != null; // Unity 假 null = 已销毁
        return true;
    }

    /// <summary>Tween 是否仍然可用：对象存活、目标没被销毁。</summary>
    private static bool IsUsable(Tween t)
    {
        if (t == null) return false;
        if (!t.IsActive()) return false;
        return IsTargetAlive(t);
    }
}
