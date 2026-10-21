using DG.Tweening;
using UnityEngine;

/// <summary>
/// 冰系（冻结）**状态与流程**：冻结、自然解冻、破冰推动与碰撞结算、破冰动画、冰伤害概率与衰退。
///
/// 从 <c>Enemy</c>（原 5 千多行的上帝类）抽出。职责边界：
///   • 本类持有冰系的**全部状态**（剩余时间、概率、破冰序列、预冻结状态、回位事务……）
///     与状态迁移逻辑；
///   • **表现**在 <see cref="EnemyFreezeVisual"/>（冰壳覆盖层/着色/碎冰特效）；
///   • **战斗语义**（挂起令牌、状态机、受击、动画器）仍在 <see cref="Enemy"/>，
///     本类通过 <c>internal</c> 成员访问，不新增对外接口。
///
/// 不是 MonoBehaviour：由 Enemy 懒加载创建（<c>new EnemyFreeze(this)</c>），
/// 避免改 prefab 序列化，也不在对象池复用路径上多一次 GetComponent。
/// </summary>
public sealed class EnemyFreeze : IEnemyStatusInstance
{
    private readonly Enemy _owner;

    // ── 冰系状态（原本散在 Enemy 上） ──
    private float _freezeRemaining;
    private float _iceBreakRemaining;
    private int _breakIceOriginColumn;
    private int _breakIceOriginRow;
    private BreakIcePushResult _pendingBreakPush;
    private bool _breakSequencePending;
    private float _breakSequenceDelay;
    private float _freezeChance;
    private float _lastIceHitTime;
    private EnemyState _preFrozenState = EnemyState.Idle;
    private float _postFreezeRecoverRemaining;
    private Tween _breakIcePushTween;

    public EnemyFreeze(Enemy owner)
    {
        _owner = owner;
        // 登记到敌人的状态宿主：由它统一驱动每帧结算与统一查询（2026-10-22）。
        owner.StatusHost?.Add(this);
    }

    // ── IEnemyStatusInstance：状态效果的统一落地接口 ──

    /// <summary>状态 id（与 EnemyStatusRegistry.Frozen 一致）。</summary>
    public string StatusId => EnemyStatusRegistry.Frozen;

    /// <summary>当前是否冻结。</summary>
    public bool IsActive => IsFrozen;

    /// <summary>冻结没有进度概念（血条不显示）。</summary>
    public float GetProgress() => 0f;

    /// <summary>冻结没有层数概念。</summary>
    public int GetStacks() => 0;

    /// <summary>显示优先级（比灼烧/疾病高）。</summary>
    public int GetPriority() => 30;

    /// <summary>每帧结算：只放与状态无关的两项（状态相关的仍由状态机调用）。</summary>
    public void Tick(float deltaTime)
    {
        UpdateFreezeChanceDecay();
        TickPostFreezeRecovery();
    }

    /// <summary>清空（敌人回收时由宿主调用；实际清理走 ClearFreezeOnTeardown）。</summary>
    public void Clear() { }

    // ══════════════════════════════════════════════════════════════
    // 状态查询
    // ══════════════════════════════════════════════════════════════

    public bool IsFrozen => _owner.state == EnemyState.Frozen;

    /// <summary>
    /// 冻结中受击不播受击动画（设计 4.2）。
    /// 例外：破冰序列（含 0.12s 顶开窗口）——那时受击动画要与碎冰同步播放（设计 4.6）。
    /// </summary>
    public bool IsFrozenHitReactionSuppressed => _owner.state == EnemyState.Frozen && !_breakSequencePending;

    /// <summary>自然解冻后的短暂停顿：不接补齐/行军，保证玩家能看到“解冻了”这一步。</summary>
    public bool IsInPostFreezeRecovery => _postFreezeRecoverRemaining > 0f;

    /// <summary>解冻后停顿计时递减（由 Enemy 的 Update 调用）。</summary>
    public void TickPostFreezeRecovery()
    {
        if (_postFreezeRecoverRemaining > 0f)
            _postFreezeRecoverRemaining -= Time.deltaTime;
    }

    /// <summary>当前累计的冻结概率（冰伤害累积值）。</summary>
    public float FreezeChance => _freezeChance;

    /// <summary>是否允许进入冻结：空中、起身、QTE、阶段切换、死亡、未应战 BOSS 都不允许。</summary>
    public bool CanBeFrozen
    {
        get
        {
            var state = _owner.state;
            if (state == EnemyState.Dead) return false;
            if (state == EnemyState.Frozen) return false;
            if (state == EnemyState.IceBreaking) return false; // 破冰动画期间不可再冻（设计 4.6 / 用例 6b）
            if (state == EnemyState.Launched) return false;
            if (state == EnemyState.GettingUp) return false;
            if (state == EnemyState.QTEAttacking) return false;
            if (_owner.isPhaseTransitioning) return false;
            if (_owner.isBoss && _owner.bossState != BossState.InCombat) return false;
            return true;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // 冻结 / 解冻
    // ══════════════════════════════════════════════════════════════

    /// <summary>冻结：暂停而非取消。已冻结时只把时长重置为满值，不重复挂起。</summary>
    public bool Freeze(float duration)
    {
        if (duration <= 0f) duration = _owner.freezeDuration;

        if (_owner.state == EnemyState.Frozen)
        {
            _freezeRemaining = Mathf.Max(_freezeRemaining, duration);
            return true;
        }
        if (!CanBeFrozen) return false;

        // 在途补齐移动先落位（走完整完成分支，含补齐链回调），再进入冻结（设计 §4.1）
        _owner.CompletePendingMovement();

        _preFrozenState = _owner.state;
        _owner.state = EnemyState.Frozen;
        _freezeRemaining = duration;
        _owner.PushSuspend(Enemy.SuspendSource.Frozen);
        _owner.FreezeVisual.Apply(true, _owner.FreezeVisualContext());
        _postFreezeRecoverRemaining = 0f;
        DebugLog.Info($"[Freeze] {_owner.DebugTag} 进入冻结 {duration:F2}s (原状态={_preFrozenState})");
        return true;
    }

    /// <summary>自然解冻：从暂停处续播原动作。</summary>
    private void EndFreezeNatural()
    {
        if (_owner.state != EnemyState.Frozen) return;
        _freezeRemaining = 0f;
        _owner.FreezeVisual.Apply(false, _owner.FreezeVisualContext());
        _owner.state = _preFrozenState;
        _owner.PopSuspend(Enemy.SuspendSource.Frozen);
        _postFreezeRecoverRemaining = Mathf.Max(0f, _owner.freezeRecoveryDelay);

        // 自然解冻用另一套表现（塌陷/融化），与破冰爆散区分
        if (_owner.ResolveMeltPrefab() != null)
        {
            _owner.FreezeVisual.SpawnFittedFx(_owner.ResolveMeltPrefab(), 1.15f, _owner.FreezeVisualContext());
        }

        DebugLog.Info($"[Freeze] {_owner.DebugTag} 自然解冻 → {_owner.state}，停顿 {_postFreezeRecoverRemaining:F2}s 后再接补齐");
    }

    /// <summary>破冰解冻：取消被暂停的动作。</summary>
    public void BreakFreeze()
    {
        if (_owner.state != EnemyState.Frozen) return;
        _freezeRemaining = 0f;
        _owner.FreezeVisual.Apply(false, _owner.FreezeVisualContext());
        _owner.PopSuspend(Enemy.SuspendSource.Frozen);
        _owner.CancelAttack();
        _owner.state = EnemyState.Idle;
        DebugLog.Info($"[Freeze] {_owner.DebugTag} 破冰解冻 → Idle");
    }

    /// <summary>
    /// 系统演出接管（BOSS 转阶段）：强制解除冻结与破冰序列。
    /// 冻结是“暂停”而非“取消”，但阶段演出不允许被暂停，必须由演出方主动清掉挂起令牌：
    /// 否则挂起令牌残留 Frozen 会让 Animator 永久禁用、冻结计时不再递减、冰块覆盖层不销毁。
    /// </summary>
    public void ClearFreezeForSystemOverride()
    {
        bool hasFrozenToken = (_owner.SuspendSources & Enemy.SuspendSource.Frozen) != 0;
        if (_owner.state != EnemyState.Frozen && _owner.state != EnemyState.IceBreaking && !hasFrozenToken) return;

        _owner.FreezeVisual.Apply(false, _owner.FreezeVisualContext());
        _freezeRemaining = 0f;
        _breakSequencePending = false;
        _breakSequenceDelay = 0f;
        _iceBreakRemaining = 0f;
        _postFreezeRecoverRemaining = 0f;
        _owner.PopSuspend(Enemy.SuspendSource.Frozen);
        DebugLog.Info($"[Freeze] {_owner.DebugTag} 系统演出接管，强制解除冻结 (state={_owner.state})");
    }

    public void UpdateFrozen()
    {
        // 顶开到位 → 碰撞 + 碎冰
        if (_breakSequencePending)
        {
            _breakSequenceDelay -= Time.deltaTime;
            if (_breakSequenceDelay <= 0f)
            {
                StartBreakSequence();
                return;
            }
        }

        _freezeRemaining -= Time.deltaTime;

        // 冰壳呼吸：让"冻住"在读感上不是一张静止图片
        var overlay = _owner.FreezeVisual.OverlayRenderer;
        if (overlay != null)
        {
            float t = 0.5f + 0.5f * Mathf.Sin(Time.time * 6f);
            Color c = overlay.color;
            c.a = Mathf.Lerp(0.55f, 0.92f, t);
            overlay.color = c;
        }

        if (_freezeRemaining <= 0f)
            EndFreezeNatural();
    }

    // ══════════════════════════════════════════════════════════════
    // 破冰：推动 + 碰撞结算 + 碎冰动画
    // ══════════════════════════════════════════════════════════════

    /// <summary>①破冰推动 + ②碰撞伤害。双倍伤害与破冰动画由调用方接着处理。</summary>
    public void BreakIcePush()
    {
        if (_owner.state != EnemyState.Frozen) return;

        var cm = EnemyManager.Instance?.columnManager;
        if (cm == null) return;

        _breakIceOriginColumn = _owner.columnIndex;
        _breakIceOriginRow = _owner.rowIndex;

        var res = cm.ApplyBreakIcePush(_owner, IceBreakRuntime.BreakIcePushRows);
        _pendingBreakPush = res;

        string blockedTag = res.blockedBy != null
            ? res.blockedBy.DebugTag + "@row" + res.blockedBy.rowIndex
            : "none";
        Debug.Log("[BreakIcePush] " + _owner.DebugTag + " col=" + _owner.columnIndex + " row " + res.fromRow + "->" + res.toRow
                  + " moved=" + res.moved + " blockedBy=" + blockedTag);

        // 回位事务当场登记：到期时间 = 顶开位移 + 破冰动画 + 停留
        if (res.moved)
        {
            float delay = Enemy.BreakPushMoveSeconds
                          + Mathf.Max(0f, _owner.iceBreakAnimDuration)
                          + Mathf.Max(0f, _owner.breakIceReturnDelay);
            cm.RegisterBreakIceReturn(_owner, _breakIceOriginColumn, _breakIceOriginRow, _owner.rowIndex, delay);
        }
    }

    /// <summary>顶开到位后的碰撞结算（双方各一次）。</summary>
    private void ApplyPendingBreakCollision()
    {
        var res = _pendingBreakPush;
        if (res.blockedBy == null) return;

        float collisionDamage = 0f;
        var stageForDamage = _owner.StageFormation;
        var stageConfigForDamage = stageForDamage != null ? stageForDamage.StageConfig : null;
        if (stageConfigForDamage != null)
            collisionDamage = stageConfigForDamage.collisionDamage;

        if (collisionDamage <= 0f)
        {
            Debug.LogWarning("[BreakIcePush] 被 " + res.blockedBy.DebugTag + " 挡住，但关卡 collisionDamage<=0，未结算碰撞伤害");
            return;
        }

        float blockerHpBefore = res.blockedBy.currentHealth;
        float selfHpBefore = _owner.currentHealth;

        res.blockedBy.TakeDamage(collisionDamage, DamageType.Collision,
            feedbackSource: HitFeedbackSource.Collision,
            countsForCombo: true,
            canInterruptAttack: false,
            triggerHitAnimation: !res.blockedBy.IsFrozen,
            hitReactionDirection: HitReactionDirection.Front);
        _owner.TakeDamage(collisionDamage, DamageType.Collision,
            feedbackSource: HitFeedbackSource.Collision,
            countsForCombo: true,
            canInterruptAttack: false,
            triggerHitAnimation: false,
            hitReactionDirection: HitReactionDirection.None);

        Debug.Log("[BreakIcePush] 碰撞结算：" + _owner.DebugTag + " " + selfHpBefore + "->" + _owner.currentHealth
                  + " / " + res.blockedBy.DebugTag + " " + blockerHpBefore + "->" + res.blockedBy.currentHealth);
    }

    /// <summary>④破冰动画：停冻结、播特效；hitted 由本次 TakeDamage 正常同步播放。</summary>
    public void BeginIceBreaking()
    {
        if (_owner.state != EnemyState.Frozen) return;

        // 被顶开时：先保持冰冻形态跑完位移，到位那一刻才碰撞 + 碎冰
        if (_pendingBreakPush.moved)
        {
            _breakSequencePending = true;
            _breakSequenceDelay = Enemy.BreakPushMoveSeconds;
            return;
        }

        StartBreakSequence();
    }

    private void StartBreakSequence()
    {
        _breakSequencePending = false;
        _breakSequenceDelay = 0f;

        ApplyPendingBreakCollision();

        _freezeRemaining = 0f;
        _owner.FreezeVisual.Apply(false, _owner.FreezeVisualContext());
        _owner.PopSuspend(Enemy.SuspendSource.Frozen);

        if (_owner.ResolveIceBreakPrefab() != null)
            _owner.FreezeVisual.SpawnFittedFx(_owner.ResolveIceBreakPrefab(), 1.35f, _owner.FreezeVisualContext());
        else
            _owner.FreezeVisual.SpawnProceduralIceBreakFx(_owner.FreezeVisualContext());

        _owner.state = EnemyState.IceBreaking;
        _iceBreakRemaining = Mathf.Max(0.01f, _owner.iceBreakAnimDuration);
        DebugLog.Info("[Freeze] " + _owner.DebugTag + " 破冰 -> IceBreaking " + _iceBreakRemaining.ToString("F2") + "s");
    }

    /// <summary>破冰推动的位移补间：从被推前的位置快速滑到新排，避免“瞬移”。</summary>
    public void PlayBreakIcePushMove(Vector3 fromWorldPosition, float duration = 0.12f)
    {
        var t = _owner.transform;
        Vector3 end = t.position;
        t.position = fromWorldPosition;

        _breakIcePushTween?.Kill();
        _breakIcePushTween = t
            .DOMove(end, Mathf.Max(0.02f, duration))
            .SetEase(Ease.OutQuad)
            .SetTarget(t)
            .SetId("breakIcePush");
    }

    public void UpdateIceBreaking()
    {
        _iceBreakRemaining -= Time.deltaTime;
        if (_iceBreakRemaining > 0f) return;

        _iceBreakRemaining = 0f;
        _owner.CancelAttack();
        _owner.state = EnemyState.Idle;

        DebugLog.Info($"[Freeze] {_owner.DebugTag} 破冰动画结束 → Idle");
    }

    // ══════════════════════════════════════════════════════════════
    // 冰伤害概率
    // ══════════════════════════════════════════════════════════════

    /// <summary>冰伤害入口：叠加冻结概率 → 前摇翻倍 → clamp → 按当前累计概率掷骰。</summary>
    public void ApplyIceHit(float chancePerHit)
    {
        if (chancePerHit <= 0f) return;
        // 冻结中不累加、不掷骰；破冰动画期间同样不累加不掷骰（设计 3 / 4.6）
        if (_owner.state == EnemyState.Dead || _owner.state == EnemyState.Frozen || _owner.state == EnemyState.IceBreaking) return;

        float delta = chancePerHit * (_owner.IsInAttackWindup ? _owner.freezeWindupChanceMultiplier : 1f);
        _freezeChance = Mathf.Clamp(_freezeChance + delta, 0f, _owner.freezeChanceMax);
        _lastIceHitTime = Time.time;

        float effective = Mathf.Max(0f, _freezeChance - _owner.freezeResist);
        if (effective <= 0f) return;
        bool rolled = Random.value < effective;
        DebugLog.Info($"[Ice] {_owner.DebugTag} chance={_freezeChance:F2} effective={effective:F2} windup={_owner.IsInAttackWindup} roll={(rolled ? "HIT" : "miss")}");
        if (rolled)
        {
            _freezeChance = 0f;
            Freeze(_owner.freezeDuration);
        }
    }

    /// <summary>冻结概率衰退。冻结中不累加，但仍按时间衰退。</summary>
    public void UpdateFreezeChanceDecay()
    {
        if (_freezeChance <= 0f) return;
        if (Time.time - _lastIceHitTime < _owner.freezeChanceDecayDelay) return;
        _freezeChance = Mathf.Max(0f, _freezeChance - _owner.freezeChanceDecayPerSecond * Time.deltaTime);
    }

    // ══════════════════════════════════════════════════════════════
    // 回收
    // ══════════════════════════════════════════════════════════════

    /// <summary>死亡 / 回收：清掉冻结状态与残留挂起，避免对象池复用时动画仍被停住。</summary>
    public void ClearFreezeOnTeardown()
    {
        if (_freezeRemaining > 0f || _owner.state == EnemyState.Frozen)
        {
            _owner.FreezeVisual.Apply(false, _owner.FreezeVisualContext());
            _freezeRemaining = 0f;
        }
        _freezeChance = 0f;
        _lastIceHitTime = 0f;
        _iceBreakRemaining = 0f;
        _breakSequencePending = false;
        _breakSequenceDelay = 0f;
        _postFreezeRecoverRemaining = 0f;
        _breakIcePushTween?.Kill();
        _breakIcePushTween = null;
        _owner.ResetSuspendForTeardown();
    }
}
