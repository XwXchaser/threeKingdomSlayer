using System.Collections;
using UnityEngine;


/// <summary>
/// CavalryEnemy 的**冲锋循环**：蓄势 → 冲锋 → 撞击 → 撤退的状态机与判定。
///
/// 按机制从 <c>CavalryEnemy.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 CavalryEnemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public sealed partial class CavalryEnemy : MonoBehaviour
{
    private IEnumerator CavalryCycle()
    {
        int generation = ++_generation;
        _damageCommitted = false;
        _frozenDuringCharge = false;
        _enemy.EnterCavalryControl();
        Diag("CYCLE_START", $"originRow={_chargeOriginRow} grids={_chargeGrids} damage={ChargeDamage:F1}");

        // ── 前摇（可被打断） ──
        SetPhase(CavalryPhase.Windup, "预备冲锋");
        float elapsed = 0f;
        while (elapsed < windupDuration && !IsStale(generation))
        {
            if (!StepPaused()) elapsed += Time.deltaTime;
            yield return null;
        }
        if (OwnerGone) { StopCycle(); yield break; }
        if (IsStale(generation))
        {
            yield return RecoveryAndRetreat(generation);
            StopCycle();
            yield break;
        }
        // 前摇期间被冻结：解冻后不续播冲锋，直接收招返航（设计 §4.1 / 用例 11f）
        if (_frozenDuringCharge)
        {
            _frozenDuringCharge = false;
            Diag("CYCLE_FROZEN_ABORT", $"前摇中被冻结，解冻后直接返航 row={_enemy.rowIndex}");
            yield return RecoveryAndRetreat(generation);
            StopCycle();
            yield break;
        }

        // ── 前移 ──
        SetPhase(CavalryPhase.Charging, "冲锋前移");
        var manager = EnemyManager.Instance?.columnManager;
        while (_enemy != null && _mounted && _enemy.state != EnemyState.Dead && _enemy.rowIndex > 0 && !IsStale(generation))
        {
            // 冻结：原地停住（不推进、不落位）；解冻后本次冲锋作废，转为返航
            if (StepPaused()) { yield return null; continue; }
            if (_frozenDuringCharge)
            {
                _frozenDuringCharge = false;
                Diag("CYCLE_FROZEN_ABORT", $"解冻后终止冲锋，进入返航 row={_enemy.rowIndex}");
                yield return RecoveryAndRetreat(generation);
                StopCycle();
                yield break;
            }

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

            // 位移补间期间被冻结：这一排不提交，解冻后直接返航，不续播冲锋、也不打人
            // （设计 §4.1 / 用例 11f）。少了这段，MoveVisualToRow 补间结束后 rowIndex 会先落进
            // 下一排、循环条件随即退出，于是冲锋照打，玩家这一次冻结白挨一下。
            if (_frozenDuringCharge)
            {
                _frozenDuringCharge = false;
                manager.ReleaseCavalrySlot(_enemy);
                Diag("CYCLE_FROZEN_ABORT", $"位移中被冻结，放弃本步并返航 row={_enemy.rowIndex}");
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
            if (OwnerGone || !_mounted) yield break;
            if (IsStale(generation)) break;
            if (StepPaused()) { yield return null; continue; }
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(origin, lunge, Mathf.Clamp01(elapsed / strikeWindup));
            yield return null;
        }

        if (OwnerGone || !_mounted) yield break;
        if (IsStale(generation))
        {
            SetAnimationStage(AnimationStage.Interrupted);
            Diag("STRIKE_INTERRUPTED", target != null ? $"target={target.DebugTag}" : "target=player");
        }
        else if (!_damageCommitted)
        {
            // 伤判：与 1011 同口径——等屏幕上真实显示命中帧精灵再结算；
            // 精灵名缺失/与 clip 不符时最多等 HitFrameGrace，然后按计时兜底，避免卡死。
            if (!IsStrikeHitSpriteShowing())
            {
                float grace = 0f;
                while (!IsStrikeHitSpriteShowing() && grace < HitFrameGrace)
                {
                    if (OwnerGone || !_mounted) yield break;
                    if (StepPaused()) { yield return null; continue; }
                    grace += Time.deltaTime;
                    yield return null;
                }
                if (!IsStrikeHitSpriteShowing())
                    Diag("STRIKE_HIT_SPRITE_FALLBACK", $"declared={StrikeHitSpriteName} grace={grace:F3}s");
            }

            // 阶段事件先将Animator采样到命中帧，随后仍由玩法提交唯一一次伤害。
            SetAnimationStage(AnimationStage.StrikeRecover);
            _damageCommitted = true;
            ResolveStrikeDamage(target);
        }

        // 无论命中还是被打断，都走完收招再返航
        Vector3 recoverStart = _enemy.transform.localPosition;
        elapsed = 0f;
        while (elapsed < strikeRecover)
        {
            if (OwnerGone || !_mounted) yield break;
            if (StepPaused()) { yield return null; continue; }
            elapsed += Time.deltaTime;
            _enemy.transform.localPosition = Vector3.Lerp(recoverStart, origin, Mathf.Clamp01(elapsed / strikeRecover));
            yield return null;
        }
        if (!OwnerGone)
            _enemy.transform.localPosition = origin;

        // 攻击动作没演完就不进返航（strikeRecover 配得过短时在这里补足到尾帧）。
        // 卡肉帧不推进计时；超时兜底避免配置错误导致卡死。
        if (!OwnerGone && _mounted && _visual != null)
        {
            float waited = 0f;
            while (_visual != null && !_visual.IsMountedAttackClipFinished && waited < MaxAttackTailWait)
            {
                if (OwnerGone || !_mounted) yield break;
                if (StepPaused()) { yield return null; continue; }
                waited += Time.deltaTime;
                yield return null;
            }
        }
    }

    /// <summary>屏幕上当前是否正显示声明的命中帧精灵（与 Enemy.UpdateAttackHitFrames 同一口径）。</summary>
    private bool IsStrikeHitSpriteShowing()
    {
        if (!HasStrikeHitFrame) return false;
        if (_visual == null) _visual = GetComponent<CavalryVisualController>();
        return _visual != null && _visual.IsSpriteShowing(StrikeHitSpriteName);
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
            // 先进入Interrupted，避免从Charging瞬间广播一次Retreating并显示背向占位。
            // OnInterruptingHit已设置相同视觉子阶段，这里不重播打断动画。
            Phase = CavalryPhase.Retreating;
            LastReason = "冲锋被打断，收招返航";
            SetAnimationStage(AnimationStage.Interrupted);
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

        // 转身动作要在后退之前完整演完：这里先原地等待，位置不动。
        if (retreatTurnDuration > 0f)
        {
            Diag("RETREAT_TURN_START", $"duration={retreatTurnDuration:F2}");
            float turnElapsed = 0f;
            while (turnElapsed < retreatTurnDuration)
            {
                if (OwnerGone || !_mounted) yield break;
                if (StepPaused()) { yield return null; continue; }
                turnElapsed += Time.deltaTime;
                yield return null;
            }
            if (OwnerGone || !_mounted) yield break;
            Diag("RETREAT_TURN_END", "开始后退");
        }

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
            yield return MoveVisualToRow(nextRow, generation, retreatStepDuration);
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
}