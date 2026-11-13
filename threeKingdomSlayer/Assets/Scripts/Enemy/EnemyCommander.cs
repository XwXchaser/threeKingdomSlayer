using UnityEngine;

/// <summary>
/// **督战型增益单位**（enemyId 1013，临时名「督战鼓手」）。
///
/// 机制（2026-11-05 拍板，见 <c>plan/enemy-commander-buff-unit-plan.md</c>）：
///   • **常态**：不攻击、不造成任何伤害；正常行军 / 补齐 / 受击 / 被挑飞（配合
///     <c>Enemy.StartAttacking()</c> 里的早退）。
///   • **施放（号令）**：周期触发 → **在场**敌人攻击力 +X%（可叠加，上限 N 层）。
///   • **死亡**：死亡瞬间 → **在场**敌人攻击力 −Y%（一次性结算）。
///
/// 「在场」= 已注册进 <c>EnemyManager</c> 且未死亡的全部敌人，**排除 BOSS 与自己**。
///
/// **号令可被打断**：指挥官没有霸体 → 前摇期间被玩家本体伤害（BasicAttack / ActiveSkill /
/// Ultimate）命中即**取消本次施放**，并重置为完整周期（DoT / 被动 / 道具 / 位移不打断）。
///
/// 临时形象：借 104 BOSS 的精灵与通用动作；施法动作借放箭 QTE 的三段状态名（**不放箭**）。
/// </summary>
public class EnemyCommander : MonoBehaviour
{
    [Header("号令（加攻）")]
    [Tooltip("每次施放：全场敌人出站攻击力 +X%（0.1 = +10%）")]
    public float atkBonusPercent = 0.10f;

    [Tooltip("加攻层数上限（到顶后不再叠加）")]
    public int maxStacks = 5;

    [Header("死亡（减攻）")]
    [Tooltip("死亡瞬间：当时仍在场的敌人出站攻击力 −Y%（0.25 = −25%，一次性结算）")]
    public float deathAtkReductionPercent = 0.25f;

    [Header("节奏")]
    [Tooltip("施放周期（秒）：一次号令到下一次号令")]
    public float castInterval = 4.0f;

    [Tooltip("首次施放的前置延迟（秒）：入场后的安全窗口")]
    public float firstCastDelay = 2.5f;

    [Tooltip("施放前摇（秒）：前摇期间可被玩家本体伤害打断")]
    public float castWindup = 0.8f;

    [Tooltip("举旗预警时长（秒）；0 = 不预警")]
    public float telegraphDuration = 0.3f;

    [Header("施法表现（借 104 的放箭 QTE 三段；不会放箭）")]
    public string castStartState = "QTE_TripleStab_Start";
    public string castLoopState = "QTE_TripleStab_Loop";
    public string castEndState = "QTE_TripleStab_End";
    [Tooltip("施法段时长（秒）：Loop / End 各占一段")]
    public float castFollowDuration = 0.3f;

    private Enemy _enemy;
    private Animator _animator;
    private EnemyAttackTelegraph _telegraph;

    private float _nextCastTimer;
    private bool _casting;
    private float _castTimer;
    private bool _appliedThisCast;
    private int _castCount;

    /// <summary>已施放的号令次数（显示用）。</summary>
    public int CastCount => _castCount;

    /// <summary>当前是否正在施放（前摇 / 演出中）。</summary>
    public bool IsCasting => _casting;

    /// <summary>距离下一次号令的剩余秒数（调试 / 面板用）。</summary>
    public float NextCastTimer => _nextCastTimer;

    /// <summary>当前这次施放已经进行的时间（秒）。</summary>
    public float CastTimer => _castTimer;

    /// <summary>这次施放是否已经落下（加攻已施加）。</summary>
    public bool AppliedThisCast => _appliedThisCast;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _animator = GetComponent<Animator>();
        _telegraph = GetComponent<EnemyAttackTelegraph>();
    }

    private void OnEnable()
    {
        // ⚠️ 敌人回池时 `Enemy.OnDeath` 会被置空 → 每次启用都要重新订阅（见方案 §4.9）。
        if (_enemy != null)
        {
            _enemy.OnDeath -= OnCommanderDied;
            _enemy.OnDeath += OnCommanderDied;
            _enemy.OnDamagedWithSource -= OnDamagedWithSource;
            _enemy.OnDamagedWithSource += OnDamagedWithSource;
        }
        _nextCastTimer = firstCastDelay;
        _casting = false;
        _appliedThisCast = false;
        _castCount = 0;
    }

    private void OnDisable()
    {
        if (_enemy != null)
        {
            _enemy.OnDeath -= OnCommanderDied;
            _enemy.OnDamagedWithSource -= OnDamagedWithSource;
        }
        StopTelegraph();
    }

    private void Update()
    {
        if (_enemy == null || _enemy.state == EnemyState.Dead) return;

        // ★ **补齐优先于号令**（2026-11-09 用户定）：
        //   前方出现补齐需求（已派单、待前移）→ **打断正在进行的号令** → 回 Idle → 走 Walk 补齐。
        //   对标 105 的口径：补齐需求一旦成立，动作让位；号令与移动**不并发**。
        //   （号令在 Enemy 状态机之外，所以这里由督战组件自己判、自己断。）
        if (_casting && HasPendingFillUp())
        {
            InterruptCast("前方补齐");
            return;
        }

        if (_casting)
        {
            _castTimer += Time.deltaTime;
            if (!_appliedThisCast && _castTimer >= castWindup)
            {
                _appliedThisCast = true;
                ApplyAtkUpToAll();
                PlayState(castLoopState);
            }
            if (_appliedThisCast && _castTimer >= castWindup + castFollowDuration)
            {
                PlayState(castEndState);
            }
            if (_castTimer >= castWindup + castFollowDuration * 2f)
            {
                _casting = false;
                _nextCastTimer = castInterval;
                PlayState("Idle");
            }
            return;
        }

        _nextCastTimer -= Time.deltaTime;
        // ★ 移动中 / 有待执行的补齐订单 → **不开始号令**（同一条「补齐优先」规则的反向：
        //   否则会出现「边走边号令」，与被要求消除的「边鼓舞边补齐」是同一个毛病）。
        if (_nextCastTimer <= 0f && CanStartCast()) BeginCast();
    }

    // ── 补齐与号令的互斥 ────────────────────────────────────────────────────

    /// <summary>
    /// 该敌人是否有**待执行的前移订单**（普通补齐 / 新波次入场都算）。
    /// 判定用「已派单 + pendingRushMove」，与 Enemy 内部的补齐口径一致：
    /// 订单已存在但目标排就在脚下（pendingRushMove=false）→ 不需要动 → 不打断号令。
    /// </summary>
    private bool HasPendingFillUp()
    {
        if (_enemy == null) return false;
        return _enemy.HasRushMoveOrder && _enemy.pendingRushMove;
    }

    /// <summary>能不能开始号令：移动中 / 有待执行的前移订单 → 不能。</summary>
    private bool CanStartCast()
    {
        if (_enemy == null) return false;
        if (_enemy.state == EnemyState.Moving) return false;
        if (HasPendingFillUp()) return false;
        return true;
    }

    /// <summary>
    /// 打断号令：取消本次施放 → 回 Idle → 重置为**完整**周期。
    /// 与「本体伤害打断」同一口径（重置为完整周期），差别只在触发来源。
    ///
    /// ⚠️ 必须重置 <c>_nextCastTimer</c>：号令进行中该计时器是冻结的（早已 ≤0），
    ///   不重置的话下一帧就会立刻重开一次号令（打断 → 立刻再打断 的死循环）。
    ///
    /// ⚠️ 不重置 Animator 的 "Walk" trigger：补齐移动可能已经把 trigger 置上
    ///   （移动先于打断发生），保留它才能让 Idle → Walk 的转移正常触发（变回 Walk 补齐）。
    /// </summary>
    private void InterruptCast(string reason)
    {
        _casting = false;
        _appliedThisCast = false;
        _nextCastTimer = castInterval;
        StopTelegraph();
        // 只在敌人没有自己的专属演出动画时才回 Idle：
        // 击飞 / 眩晕 / 冻结 / 起身 各有各的动画，强行播 Idle 会把它们盖掉。
        if (_enemy != null && !HasOwnPerformanceVisual(_enemy.state))
            PlayState("Idle");
        DebugLog.Info($"[Commander] 号令被{reason}打断 → 本次取消，重置为完整周期");
    }

    /// <summary>该状态是否由敌人自己的演出动画接管（此时不能播 Idle 盖掉它）。</summary>
    private static bool HasOwnPerformanceVisual(EnemyState state)
    {
        return state == EnemyState.Launched
            || state == EnemyState.Stunned
            || state == EnemyState.Frozen
            || state == EnemyState.GettingUp
            || state == EnemyState.Dead;
    }

    // ── 施放 ────────────────────────────────────────────────────────────────

    private void BeginCast()
    {
        _casting = true;
        _castTimer = 0f;
        _appliedThisCast = false;
        StartTelegraph();
        PlayState(castStartState);
    }

    /// <summary>号令落下：给**在场**敌人叠一层加攻。</summary>
    private void ApplyAtkUpToAll()
    {
        int count = 0;
        int stacks = 0;
        foreach (var e in GetPresentEnemies())
        {
            EnemyStatusAtkMod.ApplyUp(e, atkBonusPercent, maxStacks);
            // 让该敌人的**血条视图**存在 → 它左上角的加攻标记才会出现
            //（血条视图是惰性创建的：平时只有被打过的敌人才有；走现成的呈现层端口，不新增通道）
            CombatPresenters.EnemyHealthBars?.GetOrCreate(e.gameObject);
            if (stacks == 0) stacks = EnemyStatusAtkMod.GetUpStacks(e);
            count++;
        }
        _castCount++;
        // ⚠️ 只在**层数真的涨了**时打日志：号令每 4 秒一次，一直刷会把 Console 淹掉
        //   （实测跑一轮机器人时刷了 70+ 行，机器人的日志全被冲掉）。
        if (stacks > 0 && stacks <= maxStacks)
            DebugLog.Info($"[Commander] 号令 ×{stacks}: 在场敌人 +{atkBonusPercent * 100f:F0}% 攻击力（{count} 个）");
    }

    /// <summary>指挥官死亡：给**当时仍在场**的敌人一次性减攻。</summary>
    private void OnCommanderDied(Enemy dead)
    {
        if (deathAtkReductionPercent <= 0f) return;
        int count = 0;
        foreach (var e in GetPresentEnemies())
        {
            EnemyStatusAtkMod.ApplyDown(e, deathAtkReductionPercent);
            count++;
        }
        DebugLog.Info($"[Commander] 督战单位死亡: 在场敌人 -{deathAtkReductionPercent * 100f:F0}% 攻击力（{count} 个）");
    }

    /// <summary>
    /// 受击打断：只有**玩家本体**的伤害（本体招式 / 主动技能 / 大招）能打断号令；
    /// DoT / 被动 / 道具 / 位移 / 地刺 / 碰撞不打断（沿用「只有玩家攻击本体算数」的既有口径）。
    /// </summary>
    private void OnDamagedWithSource(Enemy enemy, HitFeedbackSource source)
    {
        if (!_casting) return;
        if (source != HitFeedbackSource.BasicAttack
            && source != HitFeedbackSource.ActiveSkill
            && source != HitFeedbackSource.Ultimate)
            return;

        InterruptCast("本体伤害");
    }

    /// <summary>
    /// 「在场」敌人：已注册且未死亡的全部敌人，**排除 BOSS 与自己**。
    /// ⚠️ <c>GetAllAliveEnemies()</c> 返回的是内部列表引用 → 遍历期间不得修改它。
    /// </summary>
    private System.Collections.Generic.List<Enemy> GetPresentEnemies()
    {
        var result = new System.Collections.Generic.List<Enemy>();
        var manager = EnemyManager.Instance;
        if (manager == null) return result;
        var all = manager.GetAllAliveEnemies();
        if (all == null) return result;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null || e == _enemy) continue;
            if (e.state == EnemyState.Dead) continue;
            if (e.isBoss) continue;               // BOSS 排除（独立平衡与 QTE 体系）
            result.Add(e);
        }
        return result;
    }

    // ── 表现 ────────────────────────────────────────────────────────────────

    private void PlayState(string stateName)
    {
        if (_animator == null || string.IsNullOrEmpty(stateName)) return;
        if (!_animator.HasState(0, Animator.StringToHash(stateName))) return;
        _animator.Play(stateName, 0, 0f);
    }

    private void StartTelegraph()
    {
        if (_telegraph == null || telegraphDuration <= 0f) return;
        _telegraph.BeginWarning(telegraphDuration);
    }

    private void StopTelegraph()
    {
        if (_telegraph == null) return;
        _telegraph.StopWarning();
    }
}
