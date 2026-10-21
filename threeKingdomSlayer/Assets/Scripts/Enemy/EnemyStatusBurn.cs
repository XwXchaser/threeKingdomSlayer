using UnityEngine;

/// <summary>
/// 敌人的**灼烧状态**（持续伤害 DoT 的一种）。
///
/// 迁移说明（2026-10-22，批次 C）：灼烧原本由玩家侧的 <c>UpgradeEffectManager</c>
/// 用一个 <c>Dictionary&lt;Enemy, DotState&gt;</c> 持有并结算——状态挂在**别人**身上，
/// 于是「这个敌人是不是在燃烧」只能反过来去问管理器。现在灼烧是**敌人自己的状态**：
/// 挂在 <c>Enemy.StatusHost</c> 上，由敌人自己每帧结算，查询也只走状态宿主。
///
/// 数值口径与迁移前完全一致：
///   • 总伤害在持续时间内按固定间隔（<see cref="TickInterval"/>）平摊；
///   • 再次施加时：总跳数与剩余跳数重置，单跳伤害取「较大值」；
///   • BOSS 在非应战 / QTE 攻击 / 阶段切换期间暂停结算；
///   • 关卡不在进行中不结算。
/// </summary>
public sealed class EnemyStatusBurn : IStatusEffect
{
    /// <summary>状态 id（与 <see cref="EnemyStatusRegistry.Burn"/> 一致）。</summary>
    public const string Id = EnemyStatusRegistry.Burn;

    /// <summary>跳字间隔（秒）。</summary>
    public const float TickInterval = 1f;

    private readonly Enemy _owner;
    private int _damagePerTick;
    private int _totalTicks;
    private int _ticksRemaining;
    private float _tickTimer;
    private bool _cleared;

    private EnemyStatusBurn(Enemy owner)
    {
        _owner = owner;
        owner.StatusHost?.Add(this);
    }

    // ── IStatusEffect ──

    /// <summary>状态 id。</summary>
    public string StatusId => Id;

    /// <summary>是否处于灼烧中（敌人已死 / 状态已结束都不算）。</summary>
    public bool IsActive => !_cleared && _owner != null && _owner.state != EnemyState.Dead && _ticksRemaining > 0;

    /// <summary>进度（0~1，血条用）。</summary>
    public float GetProgress()
    {
        if (!IsActive) return 0f;
        return Mathf.Clamp01((_ticksRemaining - 1 + Mathf.Clamp01(_tickTimer / TickInterval)) / Mathf.Max(1f, _totalTicks));
    }

    /// <summary>灼烧没有层数概念。</summary>
    public int GetStacks() => 0;

    /// <summary>显示优先级（低于疾病 20、冻结 30）。</summary>
    public int GetPriority() => 10;

    /// <summary>每跳结算：到点掉血，伤害不计入连击、不打断攻击、不播受击动画。</summary>
    public void Tick(float deltaTime)
    {
        if (_cleared) return;
        if (_owner == null || _owner.state == EnemyState.Dead) { Clear(); return; }
        if (!EnemyStatusDoTRules.IsTickingAllowed(_owner)) return;

        _tickTimer -= deltaTime;
        if (_tickTimer <= 0f)
        {
            Debug.Log($"[RouteDiag] BurnTick frame={Time.frameCount} enemy={_owner.DebugTag} routeCombat=" + EnemyStatusDoTRules.DescribeRouteCombat());
            _owner.TakeDamage(_damagePerTick, DamageType.Pierce, Color.red,
                countsForCombo: false, canInterruptAttack: false, triggerHitAnimation: false, ignoreDamageModifiers: true,
                feedbackSource: HitFeedbackSource.Dot, feedbackStrength: HitFeedbackStrength.None);
            _ticksRemaining--;
            _tickTimer += TickInterval;
        }

        if (_owner.state == EnemyState.Dead || _ticksRemaining <= 0) Clear();
    }

    /// <summary>清空（敌人回池 / 状态结束时）。</summary>
    public void Clear()
    {
        if (_cleared) return;
        _cleared = true;
        _ticksRemaining = 0;
        if (_owner != null) _owner.StatusHost.Remove(this);
    }

    // ── 对外接口 ──

    /// <summary>
    /// 对敌人施加灼烧：<paramref name="totalDamage"/> 是**持续时间内的总伤害**，
    /// 按固定间隔平摊；重复施加时剩余跳数重置、单跳伤害取较大值。
    /// </summary>
    public static void ApplyTo(Enemy enemy, int totalDamage, float burnDuration)
    {
        if (enemy == null) return;
        int tickCount = Mathf.Max(1, Mathf.CeilToInt(burnDuration / TickInterval));
        int damagePerTick = totalDamage / tickCount;
        if (damagePerTick <= 0 || burnDuration <= 0f) return;

        var status = enemy.StatusHost.Get(Id) as EnemyStatusBurn;
        if (status == null || status._cleared) status = new EnemyStatusBurn(enemy);

        status._totalTicks = tickCount;
        status._ticksRemaining = tickCount;
        status._damagePerTick = Mathf.Max(status._damagePerTick, damagePerTick);
        status._tickTimer = TickInterval;
    }

    /// <summary>该敌人是否处于灼烧状态。</summary>
    public static bool IsBurning(Enemy enemy)
    {
        if (enemy == null) return false;
        var status = enemy.StatusHost.Get(Id) as EnemyStatusBurn;
        return status != null && status.IsActive;
    }

    /// <summary>当前处于灼烧状态的敌人数（诊断用）。</summary>
    public static int CountActive()
    {
        var columnManager = EnemyStatusDoTRules.ResolveColumnManager();
        if (columnManager == null) return 0;
        var all = columnManager.GetAllEnemies();
        if (all == null) return 0;
        int count = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null) continue;
            if (IsBurning(e)) count++;
        }
        return count;
    }
}
