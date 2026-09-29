using System.Collections;
using UnityEngine;

/// <summary>
/// 骑兵专属行为（109）：骑乘期间只有一次次的冲锋循环
///   判定 → 前摇 → 前移 → 命中（打前方敌人或玩家）/ 被打断 → 原路返航 → 结束
/// 规则要点：
/// - 骑乘期间不做普通攻击；落马后交回普通敌人逻辑（含普通攻击与普通补齐）。
/// - 冲锋可以从任意排发起，不需要先回到某固定排；冲锋伤害 = chargeDamage × (发起排 + 1) × 波次攻击倍率。
/// - 冲锋路径上出现敌人就停在它后面一排前探打它，不越过、不并排。
/// - 返航目标是本次冲锋的发起排；中途受阻就停在阻塞前等待，状态机继续循环重试。
/// - 只有被标记为“可打断骑兵冲锋”的攻击（当前 = 蓄力攻击）能打断冲锋；Parry 暂不打断。
/// - 真正进入 Enemy.Launch() 才永久落马。
/// 该组件不创建普通 WaveMarch 订单；普通补齐仍由 ColumnManager 独立负责。
/// </summary>
public sealed class CavalryEnemy : MonoBehaviour
{
    [Header("骑兵配置")]
    [Tooltip("骑兵敌人ID，用于校验Prefab配置。")]
    [SerializeField] private int cavalryEnemyId = 109;
    [Tooltip("冲锋基础伤害；最终伤害 = 本值 × 冲锋格数（发起排+1）× 波次攻击倍率。")]
    [SerializeField, Min(0f)] private float chargeDamage = 8f;
    [Tooltip("判定成功后到开始前移的动作前摇时长；前摇期间可被打断。")]
    [SerializeField, Min(0f)] private float windupDuration = 0.3f;
    [Tooltip("命中前探时长。")]
    [SerializeField, Min(0.02f)] private float strikeWindup = 0.18f;
    [Tooltip("命中收招时长。")]
    [SerializeField, Min(0.02f)] private float strikeRecover = 0.22f;
    [Tooltip("命中时向前探出的本地距离。")]
    [SerializeField, Min(0f)] private float strikeLungeDistance = 0.5f;
    [Tooltip("被打断后进入返航前的收招时长。")]
    [SerializeField, Min(0f)] private float interruptRecover = 0.2f;
    [Tooltip("两次冲锋之间的冷却。")]
    [SerializeField, Min(0f)] private float chargeCooldown = 1f;
    [Tooltip("SpawnEntry结束后推迟战斗判定的时间，避免入场同帧发起冲锋。")]
    [SerializeField, Min(0f)] private float spawnEntrySettleDuration = 0.35f;

    public enum CavalryPhase { MountedIdle, Windup, Charging, Striking, Retreating, RetreatBlocked, Dismounted }

    public CavalryPhase Phase { get; private set; } = CavalryPhase.MountedIdle;
    public event System.Action<CavalryEnemy> OnStateChanged;
    public string LastReason { get; private set; } = "";

    private Enemy _enemy;
    private Coroutine _cycleRoutine;
    private bool _mounted;
    private int _generation;
    private bool _damageCommitted;
    private int _chargeOriginRow = -1;
    private int _chargeGrids = 1;
    private float _nextChargeTime;
    private bool _spawnEntryCompleted = true;
    private float _spawnEntrySettleUntil;

    public bool IsMounted => _mounted;
    /// <summary>冲锋判定已开始且本次伤害尚未提交：只有这段时间会被“可打断冲锋”的攻击打断。</summary>
    public bool IsCharging => _mounted && (Phase == CavalryPhase.Windup || Phase == CavalryPhase.Charging || Phase == CavalryPhase.Striking);
    public bool IsSpecialMoveActive => _mounted && Phase != CavalryPhase.MountedIdle && Phase != CavalryPhase.Dismounted;
    public int ChargeOriginRow => _chargeOriginRow;
    public int ChargeGrids => _chargeGrids;

    /// <summary>本次冲锋造成的伤害：chargeDamage × 冲锋格数 × 波次攻击倍率。</summary>
    public float ChargeDamage
    {
        get
        {
            float waveMult = _enemy != null && _enemy.BaseAttackDamage > 0f
                ? _enemy.attackDamage / _enemy.BaseAttackDamage
                : 1f;
            return chargeDamage * Mathf.Max(1, _chargeGrids) * waveMult;
        }
    }

    public bool IsSpawnEntrySettling => !_spawnEntryCompleted || Time.time < _spawnEntrySettleUntil;

    private bool OwnerGone => _enemy == null || _enemy.state == EnemyState.Dead;
    private bool IsStale(int generation) => generation != _generation || !_mounted || _enemy == null;
    private bool HasMovementOrder() => _enemy != null && (_enemy.HasRushMoveOrder || _enemy.IsRushMovementActive);

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
    }

    /// <summary>每次从对象池取出时调用。Enemy.Initialize 会调用本方法。</summary>
    public void ResetCavalry()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        _enemy ??= GetComponent<Enemy>();
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);

        _mounted = true;
        _generation = 0;
        _damageCommitted = false;
        _chargeOriginRow = -1;
        _chargeGrids = 1;
        _nextChargeTime = 0f;
        _spawnEntryCompleted = true;
        _spawnEntrySettleUntil = 0f;
        Phase = CavalryPhase.MountedIdle;
        LastReason = "";
        OnStateChanged?.Invoke(this);
    }

    /// <summary>
    /// Enemy 在 Idle 状态每帧调用，驱动骑乘态状态机（判定 / 返航重试）。
    /// 返回 true 表示本帧由骑兵接管。
    /// </summary>
    public bool Tick()
    {
        if (_enemy == null || !_mounted || _enemy.state == EnemyState.Dead)
            return false;
        if (_cycleRoutine != null)
            return true;
        if (_enemy.state != EnemyState.Idle)
            return false;
        if (IsSpawnEntrySettling || HasMovementOrder())
            return false;
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress)
            return false;

        // 返航受阻：每帧重试继续返航，不重新发起冲锋
        if (Phase == CavalryPhase.RetreatBlocked)
        {
            TryResumeRetreat();
            return true;
        }

        if (Time.time < _nextChargeTime)
            return false;

        var manager = EnemyManager.Instance?.columnManager;
        if (manager == null || !manager.IsCavalryReadyForCombat(_enemy))
            return false;

        // row0 没有冲锋距离：如果还欠一次返航（任何中断路径都可能把它留在 row0），先把返航补完。
        if (_enemy.rowIndex <= 0)
        {
            if (_chargeOriginRow > _enemy.rowIndex)
            {
                _cycleRoutine = StartCoroutine(RetreatCycle());
                return true;
            }
            return false;
        }

        _chargeOriginRow = _enemy.rowIndex;
        _chargeGrids = _chargeOriginRow + 1;
        _cycleRoutine = StartCoroutine(CavalryCycle());
        return true;
    }

    #region 冲锋循环

    private IEnumerator CavalryCycle()
    {
        int generation = ++_generation;
        _damageCommitted = false;
        _enemy.EnterCavalryControl();
        Diag("CYCLE_START", $"originRow={_chargeOriginRow} grids={_chargeGrids} damage={ChargeDamage:F1}");

        // ── 前摇（可被打断） ──
        SetPhase(CavalryPhase.Windup, "预备冲锋");
        float elapsed = 0f;
        while (elapsed < windupDuration && !IsStale(generation))
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (OwnerGone) { StopCycle(); yield break; }
        if (IsStale(generation))
        {
            yield return RecoveryAndRetreat(generation);
            StopCycle();
            yield break;
        }

        // ── 前移 ──
        SetPhase(CavalryPhase.Charging, "冲锋前移");
        var manager = EnemyManager.Instance?.columnManager;
        while (_enemy != null && _mounted && _enemy.state != EnemyState.Dead && _enemy.rowIndex > 0 && !IsStale(generation))
        {
            manager ??= EnemyManager.Instance?.columnManager;
            int nextRow = _enemy.rowIndex - 1;
            var column = manager?.GetColumn(_enemy.columnIndex);

            // 节奏门等硬阻挡：本次冲锋作废，直接返航
            if (manager == null || column == null || !manager.CanAdvanceIntoRow(nextRow))
            {
                Diag("CHARGE_BLOCKED_PATH", $"nextRow={nextRow}");
                _nextChargeTime = Time.time + Mathf.Max(0f, chargeCooldown);
                yield return RetreatRoutine();
                StopCycle();
                yield break;
            }

            // 前方已经有敌人：停在它后面一排打它，打完返航
            var blocker = column.GetEnemyAtRow(nextRow);
            if (blocker != null && blocker != _enemy && blocker.state != EnemyState.Dead)
            {
                Diag("CHARGE_CONTACT_ENEMY", $"nextRow={nextRow} blocker={blocker.DebugTag}");
                yield return StrikeRoutine(generation, blocker);
                yield return RetreatRoutine();
                StopCycle();
                yield break;
            }

            // 目标槽被其他系统预留或占用
            if (!manager.TryReserveCavalrySlot(_enemy, nextRow))
            {
                blocker = column.GetEnemyAtRow(nextRow);
                if (blocker != null && blocker != _enemy && blocker.state != EnemyState.Dead)
                {
                    Diag("CHARGE_SLOT_TAKEN", $"nextRow={nextRow} blocker={blocker.DebugTag}");
                    yield return StrikeRoutine(generation, blocker);
                }
                else
                {
                    Diag("CHARGE_BLOCKED_SLOT", $"nextRow={nextRow}");
                    _nextChargeTime = Time.time + Mathf.Max(0f, chargeCooldown);
                }
                yield return RetreatRoutine();
                StopCycle();
                yield break;
            }

            Diag("CHARGE_STEP_START", $"nextRow={nextRow}");
            yield return MoveVisualToRow(nextRow, generation);

            if (OwnerGone)
            {
                manager.ReleaseCavalrySlot(_enemy);
                StopCycle();
                yield break;
            }
            if (IsStale(generation))
            {
                // 被打断：放弃未提交的位移，收招后返航，本次不结算伤害
                manager.ReleaseCavalrySlot(_enemy);
                yield return RecoveryAndRetreat(generation);
                StopCycle();
                yield break;
            }

            // 移动途中前方新出现敌人（例如被玩家位移推进来）：回到本排打它
            blocker = column.GetEnemyAtRow(nextRow);
            if (blocker != null && blocker != _enemy && blocker.state != EnemyState.Dead)
            {
                manager.ReleaseCavalrySlot(_enemy);
                yield return MoveVisualToRow(_enemy.rowIndex, generation);
                if (OwnerGone) { StopCycle(); yield break; }
                if (IsStale(generation))
                {
                    yield return RecoveryAndRetreat(generation);
                    StopCycle();
                    yield break;
                }
                Diag("CHARGE_INTERCEPT_ENEMY", $"nextRow={nextRow} blocker={blocker.DebugTag}");
                yield return StrikeRoutine(generation, blocker);
                yield return RetreatRoutine();
                StopCycle();
                yield break;
            }

            _enemy.SetRowIndex(nextRow);
            manager.ReleaseCavalrySlot(_enemy);
            Diag("CHARGE_STEP_COMMIT", $"row={nextRow}");
            SpikeTrapController.Instance?.CheckAndTrigger(_enemy);
            if (OwnerGone)
            {
                StopCycle();
                yield break;
            }
        }

        if (OwnerGone)
        {
            StopCycle();
            yield break;
        }
        if (IsStale(generation))
        {
            yield return RecoveryAndRetreat(generation);
            StopCycle();
            yield break;
        }

        // ── 到达 row0：对玩家结算本次冲锋 ──
        yield return StrikeRoutine(generation, null);
        yield return RetreatRoutine();
        StopCycle();
    }

    /// <summary>命中表现：向前探出、结算一次伤害、再收回。伤害提交后不再可被打断。</summary>
    private IEnumerator StrikeRoutine(int generation, Enemy target)
    {
        if (OwnerGone) yield break;

        SetPhase(CavalryPhase.Striking, target != null ? $"命中{target.DebugTag}" : "命中玩家");
        _enemy.PlayCavalryStrikeVisual();

        Vector3 origin = _enemy.transform.localPosition;
        Vector3 lunge = origin + new Vector3(0f, 0f, -strikeLungeDistance);

        float elapsed = 0f;
        while (elapsed < strikeWindup)
        {
            if (OwnerGone) yield break;
            if (IsStale(generation)) break;
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(origin, lunge, Mathf.Clamp01(elapsed / strikeWindup));
            yield return null;
        }

        if (OwnerGone) yield break;
        if (IsStale(generation))
        {
            Diag("STRIKE_INTERRUPTED", target != null ? $"target={target.DebugTag}" : "target=player");
        }
        else if (!_damageCommitted)
        {
            _damageCommitted = true;
            ResolveStrikeDamage(target);
        }

        // 无论命中还是被打断，都走完收招再返航
        elapsed = 0f;
        while (elapsed < strikeRecover)
        {
            if (OwnerGone) yield break;
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(lunge, origin, Mathf.Clamp01(elapsed / strikeRecover));
            yield return null;
        }
        if (!OwnerGone)
            _enemy.transform.localPosition = origin;
    }

    private void ResolveStrikeDamage(Enemy target)
    {
        float damage = ChargeDamage;
        if (target != null)
        {
            if (target.state == EnemyState.Dead)
            {
                Diag("STRIKE_TARGET_GONE", "damage skipped");
                return;
            }
            target.TakeDamage(damage, DamageType.Stab,
                feedbackStrength: HitFeedbackStrength.Heavy,
                canInterruptAttack: true,
                feedbackSource: HitFeedbackSource.BasicAttack);
            Diag("STRIKE_ENEMY", $"target={target.DebugTag} damage={damage:F1}");
            return;
        }

        PlayerState.Instance?.TakeDamage(damage, _enemy);
        Diag("STRIKE_PLAYER", $"damage={damage:F1}");
    }

    /// <summary>被打断：收招并返航，本次不结算任何伤害。</summary>
    private IEnumerator RecoveryAndRetreat(int generation)
    {
        _damageCommitted = true;
        if (_enemy != null && _mounted && _enemy.state != EnemyState.Dead)
        {
            SetPhase(CavalryPhase.Retreating, "冲锋被打断，收招返航");
            Diag("RECOVERY_RETREAT", $"fromRow={_enemy.rowIndex} origin={_chargeOriginRow}");
            yield return RestoreVisualToRow(interruptRecover);
        }
        // 返航不是可打断阶段：使用当前代际，否则返回第一步就会因代际失效而中止。
        yield return RetreatRoutine();
    }

    /// <summary>返航：原路逐排退回本次冲锋的发起排；受阻就停在阻塞前等待。</summary>
    private IEnumerator RetreatRoutine()
    {
        if (OwnerGone || !_mounted) yield break;

        // 返航不是可打断阶段：代际必须用当前值。
        // 否则任何一次打断（它会 ++_generation）都会让返航第一步立即自我中止，
        // 骑兵就停在当前排既不返航也不重新发起。
        int generation = _generation;
        int target = Mathf.Max(0, _chargeOriginRow);
        if (_enemy.rowIndex >= target)
        {
            SetPhase(CavalryPhase.MountedIdle, "返航完成");
            yield break;
        }

        SetPhase(CavalryPhase.Retreating, "返航");
        var manager = EnemyManager.Instance?.columnManager;
        while (_enemy != null && _mounted && _enemy.state != EnemyState.Dead && _enemy.rowIndex < target)
        {
            manager ??= EnemyManager.Instance?.columnManager;
            int nextRow = _enemy.rowIndex + 1;
            if (manager == null || !manager.CanAdvanceIntoRow(nextRow) || !manager.TryReserveCavalrySlot(_enemy, nextRow))
            {
                SetPhase(CavalryPhase.RetreatBlocked, "返航受阻");
                Diag("RETREAT_BLOCKED", $"from={_enemy.rowIndex} nextRow={nextRow} target={target}");
                yield break;
            }

            Diag("RETREAT_STEP_START", $"nextRow={nextRow}");
            yield return MoveVisualToRow(nextRow, generation);
            if (OwnerGone)
            {
                manager.ReleaseCavalrySlot(_enemy);
                yield break;
            }
            if (IsStale(generation))
            {
                manager.ReleaseCavalrySlot(_enemy);
                yield break;
            }

            var column = manager.GetColumn(_enemy.columnIndex);
            var blocker = column?.GetEnemyAtRow(nextRow);
            if (blocker != null && blocker != _enemy && blocker.state != EnemyState.Dead)
            {
                manager.ReleaseCavalrySlot(_enemy);
                yield return RestoreVisualToRow(interruptRecover);
                SetPhase(CavalryPhase.RetreatBlocked, "返航受阻");
                Diag("RETREAT_BLOCKED", $"from={_enemy.rowIndex} blocker={blocker.DebugTag} target={target}");
                yield break;
            }

            _enemy.SetRowIndex(nextRow);
            manager.ReleaseCavalrySlot(_enemy);
            Diag("RETREAT_STEP_COMMIT", $"row={nextRow}");
            SpikeTrapController.Instance?.CheckAndTrigger(_enemy);
        }

        if (_enemy != null && _mounted && _enemy.state != EnemyState.Dead && _enemy.rowIndex >= target)
        {
            Diag("RETREAT_DONE", $"row={_enemy.rowIndex} target={target}");
            SetPhase(CavalryPhase.MountedIdle, "返航完成");
        }
    }

    private void TryResumeRetreat()
    {
        if (!_mounted || _enemy == null || _enemy.state != EnemyState.Idle) return;
        if (_cycleRoutine != null) return;
        if (HasMovementOrder() || IsSpawnEntrySettling) return;

        var manager = EnemyManager.Instance?.columnManager;
        if (manager == null || !manager.IsCavalryReadyForCombat(_enemy)) return;

        if (_enemy.rowIndex >= Mathf.Max(0, _chargeOriginRow))
        {
            _nextChargeTime = Time.time + Mathf.Max(0f, chargeCooldown);
            SetPhase(CavalryPhase.MountedIdle, "返航完成");
            return;
        }

        _cycleRoutine = StartCoroutine(RetreatCycle());
    }

    private IEnumerator RetreatCycle()
    {
        int generation = _generation;
        if (_enemy != null && _mounted && _enemy.state != EnemyState.Dead)
            yield return RetreatRoutine();
        StopCycle();
    }

    /// <summary>协程收尾：设置冷却、恢复 Idle 表现，交还控制权。</summary>
    private void StopCycle()
    {
        _cycleRoutine = null;
        if (_enemy != null && _mounted && _enemy.state != EnemyState.Dead)
        {
            if (Phase != CavalryPhase.RetreatBlocked)
            {
                _nextChargeTime = Time.time + Mathf.Max(0f, chargeCooldown);
                SetPhase(CavalryPhase.MountedIdle);
            }
            _enemy.ExitCavalryControl();
            Diag("CYCLE_END", $"row={_enemy.rowIndex} origin={_chargeOriginRow}");
        }
    }

    #endregion

    #region 位移

    private IEnumerator MoveVisualToRow(int row, int generation)
    {
        if (_enemy == null) yield break;
        Vector3 start = _enemy.transform.localPosition;
        Vector3 end = _enemy.GetCavalryRowLocalPosition(row);
        float duration = Mathf.Max(0.02f, _enemy.moveSpeed);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (OwnerGone) yield break;
            if (IsStale(generation)) yield break;
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (!OwnerGone)
            _enemy.transform.localPosition = end;
    }

    /// <summary>把视觉位置连续拉回当前物理排（收招用，不改变 rowIndex）。</summary>
    private IEnumerator RestoreVisualToRow(float duration)
    {
        if (_enemy == null) yield break;
        Vector3 start = _enemy.transform.localPosition;
        Vector3 end = _enemy.GetCavalryRowLocalPosition(_enemy.rowIndex);
        if (duration <= 0f)
        {
            _enemy.transform.localPosition = end;
            yield break;
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (OwnerGone) yield break;
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (!OwnerGone)
            _enemy.transform.localPosition = end;
    }

    #endregion

    #region 外部事件

    /// <summary>被“可打断骑兵冲锋”的攻击命中时调用（当前 = 蓄力攻击）。</summary>
    public void OnInterruptingHit()
    {
        if (!_mounted || _enemy == null || _enemy.state == EnemyState.Dead) return;
        if (!IsCharging || _damageCommitted) return;
        _generation++;   // 让进行中的前摇/位移立即作废
        Diag("INTERRUPT", $"phase={Phase} row={_enemy.rowIndex}");
    }

    /// <summary>外部调度（SpawnEntry / WaveMarch）需要接管移动时，立刻结束骑兵动作并交还位置。</summary>
    public void AbortForExternalOrder(string reason)
    {
        if (!_mounted || _enemy == null) return;
        _generation++;
        _damageCommitted = true;
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);
        _enemy.RestoreCavalryWorldPosition();
        SetPhase(CavalryPhase.MountedIdle, reason);
        Diag("ABORT_EXTERNAL", reason);
    }

    public void NotifySpawnEntryStarted()
    {
        _spawnEntryCompleted = false;
        _spawnEntrySettleUntil = float.PositiveInfinity;
        Diag("SPAWN_ENTRY_START");
    }

    public void NotifySpawnEntryCompleted()
    {
        _spawnEntryCompleted = true;
        _spawnEntrySettleUntil = Time.time + Mathf.Max(0f, spawnEntrySettleDuration);
        Diag("SPAWN_ENTRY_COMPLETE", "combat decision delayed");
    }

    public void TraceEnemyRowChange(int from, int to, string caller)
    {
        Diag("ROW_CHANGE", $"{from}->{to} caller={caller}");
    }

    /// <summary>真正进入击飞时永久落马；Enemy.Launch 负责随后进入 Launched 状态。</summary>
    public void DismountFromLaunch() => Dismount(false, "击飞落马");

    public void Dismount(bool launchAfter = true, string reason = "击飞落马")
    {
        if (!_mounted || _enemy == null || _enemy.state == EnemyState.Dead)
            return;

        _mounted = false;
        _generation++;
        _damageCommitted = true;
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);
        _enemy.RestoreCavalryWorldPosition();
        _enemy.CancelCavalryAttackOnDismount();
        SetPhase(CavalryPhase.Dismounted, reason);
        EnemyManager.Instance?.columnManager?.RequestCavalryDismountReflow(_enemy);
        Diag("DISMOUNT", reason);

        // 落马后立即进入普通 Launched 流程；由 Enemy.Launch 调用时不再回调（避免递归）。
        if (launchAfter)
            _enemy.Launch();
    }

    private void OnDisable()
    {
        if (_cycleRoutine != null)
        {
            StopCoroutine(_cycleRoutine);
            _cycleRoutine = null;
        }
        EnemyManager.Instance?.columnManager?.ReleaseCavalrySlot(_enemy);
    }

    #endregion

    private void SetPhase(CavalryPhase phase, string reason = "")
    {
        Phase = phase;
        LastReason = reason;
        OnStateChanged?.Invoke(this);
    }

    private void Diag(string eventName, string detail = "")
    {
        if (_enemy == null) return;
        Debug.Log($"[CavalryDiag] {eventName} {_enemy.DebugTag} col={_enemy.columnIndex} phase={Phase} state={_enemy.state} row={_enemy.rowIndex} mounted={_mounted} origin={_chargeOriginRow} grids={_chargeGrids} committed={_damageCommitted} {detail}");
    }
}
