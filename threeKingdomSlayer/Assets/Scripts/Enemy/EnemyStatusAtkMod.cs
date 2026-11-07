using UnityEngine;

/// <summary>
/// 敌人的**出站攻击力修正**状态（督战单位 1013「号令」的落点）。
///
/// 口径（2026-11-05 拍板，见 <c>plan/enemy-commander-buff-unit-plan.md</c>）：
///   • 加攻（<see cref="UpId"/>）：**可叠加**，每层 +X%，波内不过期；
///   • 减攻（<see cref="DownId"/>）：指挥官死亡时**一次性结算**，不叠加；
///   • 最终倍率 = (1 + 加攻层数 × X) × (1 − 减攻比例)。
///
/// 状态挂在敌人的状态宿主上（<c>Enemy.StatusHost</c>）→ 回池时由既有的
/// <c>StatusHost.ClearAll()</c> 统一清理，**不会跨波泄漏**。
///
/// 只作用于**出站伤害**（敌人打玩家的那一下），不影响敌人受到的伤害。
/// </summary>
public sealed class EnemyStatusAtkMod : IStatusEffect
{
    /// <summary>加攻状态 id（<see cref="EnemyStatusRegistry.AtkUp"/>）。</summary>
    public const string UpId = EnemyStatusRegistry.AtkUp;

    /// <summary>减攻状态 id（<see cref="EnemyStatusRegistry.AtkDown"/>）。</summary>
    public const string DownId = EnemyStatusRegistry.AtkDown;

    private readonly Enemy _owner;
    private readonly bool _isUp;
    private float _percentPerStack;
    private int _stacks;
    private bool _cleared;

    private EnemyStatusAtkMod(Enemy owner, bool isUp)
    {
        _owner = owner;
        _isUp = isUp;
        owner.StatusHost?.Add(this);
    }

    // ── IStatusEffect ──────────────────────────────────────────────────────

    /// <summary>状态 id（加攻 / 减攻各一个）。</summary>
    public string StatusId => _isUp ? UpId : DownId;

    /// <summary>生效中（敌人死了 / 被清掉都不算）。</summary>
    public bool IsActive => !_cleared && _owner != null && _owner.state != EnemyState.Dead && _stacks > 0;

    /// <summary>没有进度概念（血条用的进度条不适用）。</summary>
    public float GetProgress() => 0f;

    /// <summary>层数（加攻 = 已施放次数；减攻 = 1）。</summary>
    public int GetStacks() => _stacks;

    /// <summary>显示优先级（低于疾病 20 / 冻结 30）。</summary>
    public int GetPriority() => _isUp ? 15 : 16;

    /// <summary>每帧结算：本状态不随时间变化（只随施加 / 清除）。</summary>
    public void Tick(float deltaTime) { }

    /// <summary>清空（敌人回池 / 状态结束时）。</summary>
    public void Clear()
    {
        if (_cleared) return;
        _cleared = true;
        _stacks = 0;
        if (_owner != null) _owner.StatusHost?.Remove(this);
    }

    /// <summary>该状态提供的倍率（1 = 无修正）。</summary>
    public float Multiplier => _isUp
        ? 1f + _stacks * _percentPerStack
        : 1f - _stacks * _percentPerStack;

    // ── 对外接口 ────────────────────────────────────────────────────────────

    /// <summary>
    /// 给敌人叠加一层加攻（到上限就不再涨）。
    /// </summary>
    /// <param name="enemy">目标敌人。</param>
    /// <param name="percentPerStack">每层百分比（0.1 = +10%）。</param>
    /// <param name="maxStacks">层数上限。</param>
    public static void ApplyUp(Enemy enemy, float percentPerStack, int maxStacks)
    {
        if (enemy == null) return;
        var status = enemy.StatusHost?.Get(UpId) as EnemyStatusAtkMod;
        if (status == null || status._cleared)
        {
            status = new EnemyStatusAtkMod(enemy, true);
            status._percentPerStack = percentPerStack;
            status._stacks = 0;
        }
        status._percentPerStack = percentPerStack;
        if (status._stacks < maxStacks) status._stacks++;
    }

    /// <summary>
    /// 给敌人一次性结算减攻（不叠加；指挥官只死一次）。
    /// </summary>
    public static void ApplyDown(Enemy enemy, float percent)
    {
        if (enemy == null) return;
        var status = enemy.StatusHost?.Get(DownId) as EnemyStatusAtkMod;
        if (status == null || status._cleared)
        {
            status = new EnemyStatusAtkMod(enemy, false);
            status._percentPerStack = percent;
        }
        status._percentPerStack = percent;
        status._stacks = 1;
    }

    /// <summary>
    /// 该敌人的**出站攻击倍率**（加攻 × 减攻；1 = 未被修正）。
    /// 伤害结算点用它（近战 / 骑兵冲锋 / 远程箭矢）。
    /// </summary>
    public static float GetOutgoingMultiplier(Enemy enemy)
    {
        if (enemy == null) return 1f;
        float m = 1f;
        var up = enemy.StatusHost?.Get(UpId) as EnemyStatusAtkMod;
        if (up != null && up.IsActive) m *= up.Multiplier;
        var down = enemy.StatusHost?.Get(DownId) as EnemyStatusAtkMod;
        if (down != null && down.IsActive) m *= down.Multiplier;
        return m;
    }

    /// <summary>该敌人的加攻层数（0 = 没有被加成）。</summary>
    public static int GetUpStacks(Enemy enemy)
    {
        if (enemy == null) return 0;
        var up = enemy.StatusHost?.Get(UpId) as EnemyStatusAtkMod;
        return up != null && up.IsActive ? up.GetStacks() : 0;
    }
}
