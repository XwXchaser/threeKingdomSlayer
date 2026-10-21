using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **QTE 英雄 HUD 端口**：QTE 演出需要英雄 HUD 上的两个挂点，
/// 以及「严格模式」提示的表现。
///
/// 2026-10-22（asmdef 分层第七批）：内核 → 界面硬引用接口化。
/// 端口刻意**不暴露 TMP_Text** —— 契约层不该依赖文字组件包 ✅，
/// 「提示怎么表现」是界面层的事 ✅。
/// </summary>
public interface IQteHud
{
    /// <summary>QTE 框（指示器用它定位）。</summary>
    RectTransform QteFrameRect { get; }

    /// <summary>QTE 指示器挂点（没有则 QTE 退回自己的 Canvas）。</summary>
    RectTransform QteIndicatorArea { get; }

    /// <summary>显示「严格模式」提示（表现与淡出由界面层负责）。</summary>
    void ShowStrictModePrompt(float durationSeconds);
}

/// <summary>
/// **QTE 输入锁的施加对象**：QTE 严格模式期间要冻住 Buff 面板的点击。
/// </summary>
public interface IQteInputLockTarget
{
    /// <summary>锁定 / 解锁 QTE 输入。</summary>
    void SetQTEInputLocked(bool locked);
}

/// <summary>
/// **QTE 输入锁的登记表**：Buff 面板可以有多个 ✅（同屏多个面板 ✅），
/// 所以是「登记 + 统一施加」而不是单例 ✅。
/// </summary>
public static class QteInputLockTargets
{
    private static readonly List<IQteInputLockTarget> _targets = new List<IQteInputLockTarget>();

    /// <summary>登记一个施加对象。</summary>
    public static void Register(IQteInputLockTarget target)
    {
        if (target == null) return;
        if (_targets.Contains(target)) return;
        _targets.Add(target);
    }

    /// <summary>注销一个施加对象。</summary>
    public static void Unregister(IQteInputLockTarget target)
    {
        if (target == null) return;
        _targets.Remove(target);
    }

    /// <summary>给所有已登记的对象加锁 / 解锁。</summary>
    public static void SetAllLocked(bool locked)
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            var t = _targets[i];
            if (t == null) continue;
            t.SetQTEInputLocked(locked);
        }
    }
}
