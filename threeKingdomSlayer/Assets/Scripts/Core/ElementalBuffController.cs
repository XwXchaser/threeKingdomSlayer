using UnityEngine;

/// <summary>
/// 玩家身上的元素一次性 BUFF：不设时限、挂到被消耗为止、可并存（当前只实装冰系）。
/// 刻意与 BuffManager 分开：那个只承载数值 StatModifier，这里承载“改写规则”的一次性效果。
///
/// 2026-10-22：状态本体已迁到玩家侧状态 <c>IceChargeStatus</c>（挂在 PlayerState.StatusHost 上），
/// 本类退为**门面**（保持原有调用面不变：HUD 订阅 OnChanged、主动技能 GrantIce、破冰会话 TryConsumeIce）。
/// </summary>
public static class PlayerElementalBuffs
{
    /// <summary>状态变化（获得 / 消费 / 清空）。HUD 订阅这里刷新图标。</summary>
    public static event System.Action OnChanged;

    /// <summary>取玩家侧的元素 BUFF 状态（没有则建一个并登记到玩家状态容器）。</summary>
    private static IceChargeStatus Resolve()
    {
        var player = PlayerState.Instance;
        if (player == null) return null;
        var status = player.StatusHost.Get(IceChargeStatus.Id) as IceChargeStatus;
        if (status == null)
        {
            status = new IceChargeStatus();
            status.OnChanged += () => OnChanged?.Invoke();
            player.StatusHost.Add(status);
        }
        return status;
    }

    public static bool IceReady => Resolve() != null && Resolve().IsActive;

    public static ElementalScope IceScope => Resolve() != null ? Resolve().Scope : ElementalScope.SingleColumnHeavy;

    /// <summary>获得破冰预备（作用域匹配则可被一次合规重攻击消费）。</summary>
    public static void GrantIce(ElementalScope scope = ElementalScope.SingleColumnHeavy)
    {
        Resolve()?.Grant(scope);
    }

    /// <summary>作用域匹配则消费并返回 true。一次攻击只在首次实际命中时调用。</summary>
    public static bool TryConsumeIce(ColumnMode columnMode, bool isHeavy)
    {
        var status = Resolve();
        return status != null && status.TryConsume(columnMode, isHeavy);
    }

    public static void Clear()
    {
        Resolve()?.Clear();
    }
}
