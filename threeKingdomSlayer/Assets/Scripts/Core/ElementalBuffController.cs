using UnityEngine;

/// <summary>
/// 元素一次性 BUFF 的作用域。与招式资产的 ColumnMode 配合做合规判定。
/// </summary>
public enum ElementalScope
{
    /// <summary>只对单列重攻击生效（初始版）</summary>
    SingleColumnHeavy = 0,
    /// <summary>任意重攻击生效（局外解锁版）</summary>
    AnyHeavy = 1
}

/// <summary>
/// 玩家身上的元素一次性 BUFF：不设时限、挂到被消耗为止、可并存（当前只实装冰系）。
/// 刻意与 BuffManager 分开：那个只承载数值 StatModifier，这里承载"改写规则"的一次性效果。
/// </summary>
public static class PlayerElementalBuffs
{
    private static bool _iceReady;
    private static ElementalScope _iceScope = ElementalScope.SingleColumnHeavy;

    public static bool IceReady => _iceReady;
    public static ElementalScope IceScope => _iceScope;

    /// <summary>状态变化（获得 / 消费 / 清空）。HUD 订阅这里刷新图标。</summary>
    public static event System.Action OnChanged;

    public static void GrantIce(ElementalScope scope = ElementalScope.SingleColumnHeavy)
    {
        bool wasReady = _iceReady;
        _iceReady = true;
        _iceScope = scope;
        DebugLog.Info($"[ElementalBuff] 获得破冰预备 scope={scope}{(wasReady ? "（已存在，刷新作用域）" : "")}");
        OnChanged?.Invoke();
    }

    /// <summary>作用域匹配则消费并返回 true。一次攻击只在首次实际命中时调用。</summary>
    public static bool TryConsumeIce(ColumnMode columnMode, bool isHeavy)
    {
        if (!_iceReady) return false;
        if (!isHeavy) return false;
        if (_iceScope == ElementalScope.SingleColumnHeavy && columnMode != ColumnMode.SingleColumn) return false;

        _iceReady = false;
        DebugLog.Info($"[ElementalBuff] 消费破冰预备 (columnMode={columnMode}, scope={_iceScope})");
        OnChanged?.Invoke();
        return true;
    }

    public static void Clear()
    {
        bool changed = _iceReady;
        _iceReady = false;
        _iceScope = ElementalScope.SingleColumnHeavy;
        if (changed) OnChanged?.Invoke();
    }
}

/// <summary>
/// 破冰攻击会话：招式开始时开一次；首次实际命中时校验作用域并消费 BUFF；
/// 之后本次攻击命中的所有冻结目标都享受破冰（多目标各自双倍、BUFF 只消费一次）。
/// 用"招式开始时间 + 动作时长"做作用域结束，避免为每个招式单独挂收尾回调。
/// </summary>
public static class IceBreakRuntime
{
    private static AttackSkillConfig _cfg;
    private static float _sessionEndTime;
    private static bool _armed;
    private static readonly System.Collections.Generic.HashSet<Enemy> _brokenThisAttack =
        new System.Collections.Generic.HashSet<Enemy>();

    /// <summary>本次攻击中已经推过的冻结目标（同列批量推动时防止重复推）。</summary>
    private static readonly System.Collections.Generic.HashSet<Enemy> _pushedThisAttack =
        new System.Collections.Generic.HashSet<Enemy>();

    private static readonly System.Collections.Generic.List<Enemy> _breakColumnBuffer =
        new System.Collections.Generic.List<Enemy>();

    /// <summary>本次攻击里正在被破冰的冻结目标：它们彼此只停不停撞（设计 §5.7）。</summary>
    private static readonly System.Collections.Generic.HashSet<Enemy> _breakSetThisAttack =
        new System.Collections.Generic.HashSet<Enemy>();

    public static bool IsBreakSetMember(Enemy enemy) => enemy != null && _breakSetThisAttack.Contains(enemy);

    /// <summary>本次攻击中被破冰解冻过的目标（这些目标不再叠加招式自带的普通击退）。</summary>
    public static bool IsBrokenThisAttack(Enemy enemy) => enemy != null && _brokenThisAttack.Contains(enemy);

    public static bool HasArmedSession => _armed;

    /// <summary>本次破冰招式的最大档击退排数（取自招式资产）。</summary>
    public static int BreakIcePushRows => _cfg != null ? _cfg.breakIcePushRows : 0;

    public static void BeginAttack(AttackSkillConfig cfg, float attackDuration)
    {
        _cfg = cfg;
        _armed = false;
        _brokenThisAttack.Clear();
        _pushedThisAttack.Clear();
        _breakSetThisAttack.Clear();
        // 会话寿命 = 本次招式实际动作时长（与动作锁同一口径），而不是资产上的 actionDuration：
        // 后者不含攻速修正，会在招式结束后留下尾巴，让窗口外的伤害误触发破冰（设计 §7）。
        _sessionEndTime = cfg != null
            ? Time.time + Mathf.Max(0.1f, attackDuration) + 0.15f
            : 0f;
    }

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

    public static bool TryResolve(Enemy target, DamageType damageType, HitFeedbackSource feedbackSource, ref float damage)
    {
        // 碰撞伤害本身不得再触发破冰（否则会递归翻倍）
        if (damageType == DamageType.Collision) return false;
        // 只认玩家攻击本体造成的伤害：DoT / 被动 / 道具 / 位移 / 地刺都不算“本次重攻击命中”
        if (feedbackSource != HitFeedbackSource.BasicAttack) return false;
        if (target == null || !target.IsFrozen) return false;
        if (_cfg == null || Time.time > _sessionEndTime) return false;
        if (!_cfg.isHeavy) return false;
        // 同一目标在一次攻击里只结算一次（设计 §6：每个冻结目标各自走一遍 ①–④）
        if (_brokenThisAttack.Contains(target)) return false;

        if (!_armed)
        {
            if (!PlayerElementalBuffs.TryConsumeIce(_cfg.columnMode, _cfg.isHeavy)) return false;
            _armed = true;
        }

        // ①推动 + ②碰撞（设计 §5.7）：同列被本次重攻击覆盖的冻结目标从后往前逐个推，
        // 它们彼此不算阻挡，避免“两次破冰互相撞停并各自吃碰撞伤害”。
        if (!_pushedThisAttack.Contains(target))
            PushFrozenTargetsInColumn(target);
        _brokenThisAttack.Add(target);
        // ③原始命中双倍
        damage *= 2f;
        // ④破冰动画（hitted 由本次 TakeDamage 同步播放）
        target.BeginIceBreaking();

        DebugLog.Info($"[IceBreak] {target.DebugTag} 破冰：伤害 x2 → {damage:F0}（cfg={_cfg.name}）");
        return true;
    }
}
