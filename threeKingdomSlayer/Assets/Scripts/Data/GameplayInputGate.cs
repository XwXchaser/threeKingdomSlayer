using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 玩家输入被拦下的原因（诊断用）。
///
/// 旧实现只有一个全局布尔 <c>InputManager.gameplayInputEnabled</c>，多个模块轮流写它，
/// 「谁最后写的」决定结果 —— 谁在什么时候写的，从代码上看不见（隐式全局状态）。
/// 现在每一份拦截都带一个原因，问「为什么点不动」只需要看这一个枚举。
/// </summary>
public enum InputBlockReason
{
    /// <summary>未拦下。</summary>
    None = 0,

    /// <summary>路线流程：落地页准备 / 节点间旅行 / 等待奖励。</summary>
    RouteTravel,

    /// <summary>结算面板：胜利 / 战败。</summary>
    ResultPanel,

    /// <summary>教学手势门槛：等玩家做对指定手势。</summary>
    TutorialGate,

    /// <summary>遗留路线驱动（RouteV1 / RouteV2 原型）。</summary>
    LegacyRoute,

    /// <summary>其它未归类原因。</summary>
    Other,
}

/// <summary>
/// 玩家输入门：全工程**唯一**允许决定「玩家输入是否被拦下」的地方。
///
/// 规则：
/// <list type="number">
/// <item>每个 owner 最多持有一份拦截（重复申请只更新原因，不会叠加计数）。</item>
/// <item>全局被拦 = 至少有一个 owner 持有拦截。owner 各自管自己的那份，互不覆盖。</item>
/// <item>owner 被销毁（场景重载 / 对象回收）时，拦截会自动失效，不会把输入永久锁死。</item>
/// </list>
///
/// 用法：<c>GameplayInputGate.RequestBlock(this, InputBlockReason.RouteTravel)</c> 申请，
/// <c>GameplayInputGate.Release(this)</c> 释放；读用 <see cref="IsBlocked"/>。
/// 诊断：<see cref="Describe"/> 会列出当前所有持有者与原因。
/// </summary>
public static class GameplayInputGate
{
    /// <summary>按引用比较 owner（不用 Unity 的重载 ==，避免已销毁对象被当成 null 而查不到）。</summary>
    private sealed class OwnerComparer : IEqualityComparer<object>
    {
        public static readonly OwnerComparer Instance = new OwnerComparer();

        public new bool Equals(object x, object y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    private static readonly Dictionary<object, InputBlockReason> Blocks =
        new Dictionary<object, InputBlockReason>(OwnerComparer.Instance);

    private static readonly List<object> PruneBuffer = new List<object>();

    /// <summary>玩家输入当前是否被拦下。</summary>
    public static bool IsBlocked
    {
        get
        {
            PruneDestroyedOwners();
            return Blocks.Count > 0;
        }
    }

    /// <summary>当前持有拦截的 owner 数量。</summary>
    public static int BlockCount
    {
        get
        {
            PruneDestroyedOwners();
            return Blocks.Count;
        }
    }

    /// <summary>是否有人以该原因持有拦截（诊断 / 自检用）。</summary>
    public static bool IsBlockedBy(InputBlockReason reason)
    {
        PruneDestroyedOwners();
        foreach (var kv in Blocks)
        {
            if (kv.Value == reason) return true;
        }
        return false;
    }

    /// <summary>申请拦截（同一 owner 重复申请只更新原因）。</summary>
    public static void RequestBlock(object owner, InputBlockReason reason)
    {
        if (ReferenceEquals(owner, null)) return;
        // 已销毁的 owner 不受理：否则它再也调不到 Release，输入会被永久锁死。
        if (owner is UnityEngine.Object unityObject && unityObject == null) return;

        bool wasBlocked = IsBlocked;
        Blocks[owner] = reason == InputBlockReason.None ? InputBlockReason.Other : reason;
        LogIfStateChanged(wasBlocked);
    }

    /// <summary>释放自己的那份拦截（不影响别的 owner）。</summary>
    public static void Release(object owner)
    {
        if (ReferenceEquals(owner, null)) return;

        bool wasBlocked = IsBlocked;
        Blocks.Remove(owner);
        LogIfStateChanged(wasBlocked);
    }

    /// <summary>清空所有拦截（场景重载等极端兜底用；正常流程应各自 Release）。</summary>
    public static void ReleaseAll()
    {
        bool wasBlocked = IsBlocked;
        Blocks.Clear();
        LogIfStateChanged(wasBlocked);
    }

    /// <summary>
    /// 诊断：当前输入被谁、因为什么拦下。
    /// 例：「输入门：已拦下，持有者 2 个 → BattleHUD(ResultPanel) TutorialGestureHint(TutorialGate)」
    /// </summary>
    public static string Describe()
    {
        PruneDestroyedOwners();
        if (Blocks.Count == 0) return "输入门：未拦下（可操作）";

        var sb = new StringBuilder();
        sb.Append("输入门：已拦下，持有者 ").Append(Blocks.Count).Append(" 个 →");
        foreach (var kv in Blocks)
        {
            sb.Append(' ').Append(DescribeOwner(kv.Key)).Append('(').Append(kv.Value).Append(')');
        }
        return sb.ToString();
    }

    private static string DescribeOwner(object owner)
    {
        if (owner is UnityEngine.Object unityObject)
        {
            if (unityObject == null) return "<已销毁>";
            string objectName = unityObject.name;
            return string.IsNullOrEmpty(objectName) ? unityObject.GetType().Name : objectName;
        }
        return owner.GetType().Name;
    }

    /// <summary>owner 被销毁后，拦截自动失效（否则输入会被永久锁死）。</summary>
    private static void PruneDestroyedOwners()
    {
        if (Blocks.Count == 0) return;

        PruneBuffer.Clear();
        foreach (var kv in Blocks)
        {
            if (kv.Key is UnityEngine.Object unityObject && unityObject == null)
                PruneBuffer.Add(kv.Key);
        }
        for (int i = 0; i < PruneBuffer.Count; i++) Blocks.Remove(PruneBuffer[i]);
        PruneBuffer.Clear();
    }

    /// <summary>只在「拦 / 不拦」发生翻转时打一条日志，避免刷屏。</summary>
    private static void LogIfStateChanged(bool wasBlocked)
    {
        bool nowBlocked = Blocks.Count > 0;
        if (nowBlocked == wasBlocked) return;
        Debug.Log("[InputGate] " + Describe());
    }
}
