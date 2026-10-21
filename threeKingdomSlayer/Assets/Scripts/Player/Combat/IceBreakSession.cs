using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **破冰攻击会话**（玩家侧）：一次攻击的上下文。
///
/// 职责：回答「这次命中合不合规、能不能破冰」，并执行**攻击自身**的破冰效果
/// （同列顶开 —— 那是攻击的 AoE，不是敌人的状态变化）。
///
/// 2026-10-22 从静态类 <c>IceBreakRuntime</c> 拆出：那个类同时握着
/// 「玩家攻击的会话」与「敌人冻结状态的破冰反应」两件事，方向抽象。
/// 现在两者分开：
///   • 玩家侧（本类）：会话、作用域校验、破冰 BUFF 的消费、同列顶开；
///   • 敌人侧（<c>EnemyFreeze.TryBreak</c>）：自己解冻、伤害翻倍、破冰动画。
///
/// 保留说明：
///   • 「重击解冻」这一层就包含碎冰翻倍：解冻那一击固定 ×2
///     （旧版曾把它拆成独立天赋「碎冰重击」，用户确认这是错的，已合并回重击解冻）。
///   • 会话寿命 = 本次招式实际动作时长（与动作锁同一口径），而不是资产上的 actionDuration：
///     后者不含攻速修正，会在招式结束后留下尾巴，让窗口外的伤害误触发破冰（设计 §7）。
/// </summary>
public static class IceBreakSession
{
    /// <summary>破冰相关能力都由天赋节点 id 控制：没点就是“只有冻结”，点了才有对应能力。</summary>
    public const string TalentIdThaw = "talent_ice_thaw";
    public const string TalentIdBreakPush = "talent_ice_break_push";

    private static AttackSkillConfig _cfg;
    private static float _sessionEndTime;
    private static bool _armed;

    /// <summary>本次攻击中已经破冰解冻过的目标（这些目标不再叠加招式自带的普通击退）。</summary>
    private static readonly HashSet<Enemy> _brokenThisAttack = new HashSet<Enemy>();

    /// <summary>本次攻击中已经推过的冻结目标（同列批量推动时防止重复推）。</summary>
    private static readonly HashSet<Enemy> _pushedThisAttack = new HashSet<Enemy>();

    private static readonly List<Enemy> _breakColumnBuffer = new List<Enemy>();

    /// <summary>本次攻击里正在被破冰的冻结目标：它们彼此只停不停撞（设计 §5.7）。</summary>
    private static readonly HashSet<Enemy> _breakSetThisAttack = new HashSet<Enemy>();

    /// <summary>本次攻击中正在被破冰的冻结目标（用于位移时判断“它是不是我推的”）。</summary>
    public static bool IsBreakSetMember(Enemy enemy) => enemy != null && _breakSetThisAttack.Contains(enemy);

    /// <summary>本次攻击中被破冰解冻过的目标（这些目标不再叠加招式自带的普通击退）。</summary>
    public static bool IsBrokenThisAttack(Enemy enemy) => enemy != null && _brokenThisAttack.Contains(enemy);

    /// <summary>本次攻击的破冰 BUFF 是否已经消费过（消费过说明作用域已判定通过）。</summary>
    public static bool HasArmedSession => _armed;

    /// <summary>本次破冰招式的最大档击退排数（取自招式资产）。</summary>
    public static int BreakIcePushRows => _cfg != null ? _cfg.breakIcePushRows : 0;

    /// <summary>当前会话的招式资产名（诊断用）。</summary>
    public static string ConfigName => _cfg != null ? _cfg.name : "-";

    /// <summary>招式开始：开一次会话。</summary>
    public static void BeginAttack(AttackSkillConfig cfg, float attackDuration)
    {
        _cfg = cfg;
        _armed = false;
        _brokenThisAttack.Clear();
        _pushedThisAttack.Clear();
        _breakSetThisAttack.Clear();
        _sessionEndTime = cfg != null
            ? Time.time + Mathf.Max(0.1f, attackDuration) + 0.15f
            : 0f;
    }

    /// <summary>招式结束：清会话。</summary>
    public static void EndAttack()
    {
        _cfg = null;
        _armed = false;
        _sessionEndTime = 0f;
        _brokenThisAttack.Clear();
        _pushedThisAttack.Clear();
        _breakSetThisAttack.Clear();
    }

    /// <summary>
    /// 判定「这次命中能不能破冰」（玩家侧）。
    /// 通过时会消费一次破冰 BUFF（作用域不匹配则不通过），并把目标记进「本次已破冰」。
    /// 敌人侧再据此做自己的反应（解冻 / 翻倍 / 动画）。
    /// </summary>
    public static bool TryBeginBreak(Enemy target, DamageType damageType, HitFeedbackSource source)
    {
        // 碰撞伤害本身不得再触发破冰（否则会递归翻倍）
        if (damageType == DamageType.Collision) return false;
        // 只认玩家攻击本体造成的伤害：DoT / 被动 / 道具 / 位移 / 地刺都不算“本次重攻击命中”
        if (source != HitFeedbackSource.BasicAttack) return false;
        if (target == null) return false;
        if (_cfg == null || Time.time > _sessionEndTime) return false;
        if (!_cfg.isHeavy) return false;
        // 同一目标在一次攻击里只结算一次（设计 §6：每个冻结目标各自走一遍 ①–④）
        if (_brokenThisAttack.Contains(target)) return false;

        // 机制门槛：没点“重击解冻”天赋，重击打在冻结目标身上就是普通伤害，不会解冻。
        // 教学关里玩家只有冰雾（只冻不解），天赋点出来才有后面的能力。
        if (!TalentTreeState.IsUnlocked(TalentIdThaw)) return false;

        if (!_armed)
        {
            if (!PlayerElementalBuffs.TryConsumeIce(_cfg.columnMode, _cfg.isHeavy)) return false;
            _armed = true;
        }

        _brokenThisAttack.Add(target);
        return true;
    }

    /// <summary>
    /// 攻击的 AoE 效果：同列被本次重攻击覆盖的冻结目标从后往前逐个推。
    /// 属于**攻击**的效果（不是敌人的状态变化），所以留在玩家侧。
    /// </summary>
    public static void ApplyBreakPush(Enemy firstTarget)
    {
        if (firstTarget == null) return;
        // 需要“击退碰撞”天赋，否则只解冻不位移。
        if (!TalentTreeState.IsUnlocked(TalentIdBreakPush)) return;
        if (_pushedThisAttack.Contains(firstTarget)) return;
        PushFrozenTargetsInColumn(firstTarget);
    }

    /// <summary>
    /// 设计 §5.7：同列被本次重攻击覆盖（rowIndex &lt; rangeRows）的冻结目标从后往前逐个推。
    /// 先处理 row 大的目标，这样已推动的成员按排坐标成为后续（更靠前）目标的阻挡者，
    /// 未处理的成员不阻挡，不会出现“互相顶替/重叠”。
    /// </summary>
    private static void PushFrozenTargetsInColumn(Enemy firstTarget)
    {
        var cm = EnemyManager.Instance != null ? EnemyManager.Instance.columnManager : null;
        if (cm == null) return;

        int col = firstTarget.columnIndex;
        int range = _cfg != null ? Mathf.Max(1, _cfg.rangeRows) : 1;

        _breakColumnBuffer.Clear();
        var all = cm.GetAllEnemies();
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null || e.state == EnemyState.Dead) continue;
            if (e.columnIndex != col) continue;
            if (!e.IsFrozen) continue;
            if (e.rowIndex >= range) continue; // 超出本次招式覆盖范围的不算本次命中
            _breakColumnBuffer.Add(e);
        }

        _breakColumnBuffer.Sort((a, b) => b.rowIndex.CompareTo(a.rowIndex));
        for (int i = 0; i < _breakColumnBuffer.Count; i++)
            _breakSetThisAttack.Add(_breakColumnBuffer[i]);
        for (int i = 0; i < _breakColumnBuffer.Count; i++)
        {
            var e = _breakColumnBuffer[i];
            if (_pushedThisAttack.Contains(e)) continue;
            e.BreakIcePush();
            _pushedThisAttack.Add(e);
        }
        _breakColumnBuffer.Clear();
    }
}
