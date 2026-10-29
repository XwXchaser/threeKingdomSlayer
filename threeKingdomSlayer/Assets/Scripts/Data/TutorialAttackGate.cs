using System.Collections.Generic;

/// <summary>
/// 教学的**招式白名单**：教学关里"还没教到的招式"不允许发出。
///
/// 为什么需要（2026-10-26）：教学原本只在"手势拍"进行时用 <c>InputManager.tutorialGestureTarget</c> 过滤手势，
/// 拍之外玩家可以随意使用任何招式 —— 于是：
///   • 玩家在招架拍之前就上划出招架（这一拍要教的就是它）→ 抢跑；
///   • 玩家用蓄力上挑（Launch）直接把教学敌人打死 → 这一拍被跳过。
/// 于是教学变成"可跳过的动画"，而不是"逐步学会的操作"。
///
/// 口径：白名单 = **已经教完的招式 ∪ 当前这一拍要教的招式**。
/// 拍开始时把当前招式加进去（玩家在这一拍里必须能出它），拍结束时它就转为"已教"。
///
/// 位置：Data 层（最低层）→ 战斗层（<c>AttackSystem</c>）与教学层（导演）都能用，不产生反向依赖。
/// 未启用时（_allowed == null）= 正式战斗不限制。
/// </summary>
public static class TutorialAttackGate
{
    private static HashSet<AttackType> _allowed;

    /// <summary>是否处于"限制模式"（false = 不限制，正式战斗）。</summary>
    public static bool Enabled => _allowed != null;

    /// <summary>设置白名单（传 null = 不限制）。</summary>
    public static void SetAllowed(IEnumerable<AttackType> types)
    {
        _allowed = types == null ? null : new HashSet<AttackType>(types);
    }

    /// <summary>关闭限制（回到正式战斗的行为）。</summary>
    public static void Clear() => _allowed = null;

    /// <summary>该招式现在允许发出吗。</summary>
    public static bool Allows(AttackType type) => _allowed == null || _allowed.Contains(type);

    /// <summary>当前白名单的可读描述（调试用）。</summary>
    public static string Describe()
    {
        if (_allowed == null) return "不限制";
        var list = new List<AttackType>(_allowed);
        list.Sort();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            if (i > 0) sb.Append('/');
            sb.Append(list[i]);
        }
        return sb.ToString();
    }
}
