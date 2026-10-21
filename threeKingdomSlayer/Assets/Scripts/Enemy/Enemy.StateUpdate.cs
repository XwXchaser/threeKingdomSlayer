using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// Enemy 状态机的**状态更新**：每帧推进各状态（从 <c>Enemy.State.cs</c> 拆出）。
    ///
    /// 按机制从 <c>Enemy.State.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class Enemy : MonoBehaviour
{
    #region 状态更新

    private void UpdateStun()
    {
        if (_isStunExiting) return;

        stunTimer -= Time.deltaTime;
        if (stunTimer <= 0f)
            BeginStunExit();
    }

    internal void BeginStunExit()
    {
        currentPoise = maxPoise;
        _poiseRecoveryEndTime = 0f;
        OnPoiseChanged?.Invoke(this, currentPoise, maxPoise);

        if (!isBoss)
        {
            CompleteStunExit();
            return;
        }

        _isStunExiting = true;
        _animator?.Play("StunEnd", 0, 0f);
        _stunExitRoutine = StartCoroutine(CompleteBossStunExitAfterAnimation());
    }

    private System.Collections.IEnumerator CompleteBossStunExitAfterAnimation()
    {
        yield return new WaitForSeconds(BOSS_STUN_END_DURATION);
        _stunExitRoutine = null;
        _isStunExiting = false;
        if (state != EnemyState.Dead)
            CompleteStunExit();
    }

    private void CompleteStunExit()
    {
        state = EnemyState.Idle;
        _animator?.Play("Idle", 0, 0f);

        // BUG FIX: Stun 会清除霸体（Stun() 内 isSuperArmor=false），退出眩晕后
        // 按当前阶段配置恢复永久霸体（阶段 isSuperArmor=1 则恢复，否则保持 false）
        if (isBoss)
        {
            var phase = GetCurrentPhaseData();
            isSuperArmor = phase != null && phase.isSuperArmor;
        }

        // Normal enemies never autonomously move after stun recovery.
        if (isBoss && (bossState == BossState.InCombat || _bossEngageTimer > 0f))
        {
            SetBossActionCooldown();
        }
        else if (HasRushMoveOrder)
        {
            TryStartRushMove();
        }
    }

    /// <summary>
    /// 更新击飞状态：恒定重力加速度驱动
    /// 初始击飞给予上升初速度 sqrt(2*g*H)，空中受击叠加反弹速度
    /// </summary>
    private void UpdateLaunch()
    {
        launchTimer -= Time.deltaTime;

        // 恒定重力（计时器到期后加速下落，避免悬浮）
        float gravity = launchTimer > 0f ? launchGravity : launchGravity * 3f;
        launchVelocityY -= gravity * Time.deltaTime;

        // 挑飞期间眩晕计时继续跑，避免落地后恢复已过期的眩晕
        if (_remainingStunOnLaunch > 0f)
            _remainingStunOnLaunch = Mathf.Max(0f, _remainingStunOnLaunch - Time.deltaTime);

        // 计算新Y偏移
        float currentY = transform.localPosition.y - launchStartLocalPos.y;
        float newY = currentY + launchVelocityY * Time.deltaTime;

        // 着陆条件：自然落地（低于地面且正在下落）
        bool landed = newY <= 0f && launchVelocityY <= 0f;

        if (landed)
        {
            // 浮空结束，回到地面
            transform.localPosition = new Vector3(
                transform.localPosition.x,
                launchStartLocalPos.y,
                transform.localPosition.z);

            // 先退出击飞状态，再根据情况决定后续行为
            state = EnemyState.Idle;

            // 通知击飞落地（供 CycloneEffect 等监听落地伤害）。
            // 回调可能造成死亡，因此后续必须重新检查状态。
            OnLaunchedLanded?.Invoke(this);
            if (state == EnemyState.Dead)
                return;

            // 地刺检测：击飞落地后触发
            SpikeTrapController.Instance?.CheckAndTrigger(this);
            if (state == EnemyState.Dead)
                return;

            if (_remainingStunOnLaunch > 0f)
            {
                state = EnemyState.Stunned;
                stunTimer = _remainingStunOnLaunch;
                _remainingStunOnLaunch = 0f;
                _isStunExiting = false;
                _resumeStunAfterLaunch = false;
                UpdateOutlineState();
                if (isBoss)
                    _animator?.Play("StunLoop", 0, 0f);
                return;
            }

            if (_resumeStunAfterLaunch)
            {
                _resumeStunAfterLaunch = false;
                BeginStunExit();
                return;
            }

            if (IsCoward)
                _cowardTransitionPlaying = false;

            if (BeginGetup())
                return;

            PlayIdleVisual();

            // Landing never creates movement. It may only resume an existing scheduler order.
            ResumeAfterGetup();
            return;
        }

        // 应用位置
        transform.localPosition = new Vector3(
            transform.localPosition.x,
            launchStartLocalPos.y + newY,
            transform.localPosition.z);
    }

    private bool BeginGetup()
    {
        if (state != EnemyState.Idle || !HasGetupAnimatorState())
            return false;

        if (_getupRoutine != null)
            StopCoroutine(_getupRoutine);

        _getupInterrupted = false;
        state = EnemyState.GettingUp;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        UpdateOutlineState();
        PlayGetupVisual();
        _getupRoutine = StartCoroutine(GetupRoutine());
        return true;
    }

    private System.Collections.IEnumerator GetupRoutine()
    {
        const float fallbackDuration = 0.6f;
        float elapsed = 0f;
        while (state == EnemyState.GettingUp && !_getupInterrupted && elapsed < fallbackDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _getupRoutine = null;
        if (state != EnemyState.GettingUp || _getupInterrupted)
            yield break;

        state = EnemyState.Idle;
        PlayIdleVisual();
        ResumeAfterGetup();
    }

    internal void ResumeAfterGetup()
    {
        if (state == EnemyState.Dead || state == EnemyState.Launched || state == EnemyState.GettingUp)
            return;

        if (HasRushMoveOrder)
        {
            TryStartRushMove();
            return;
        }

        int atkRange = (int)Mathf.Max(1, attackRange);
        if (rowIndex < atkRange)
            StartAttacking();
    }

    /// <summary>
    /// 更新移动状态
    /// 常规移动：基于 moveSpeed（秒/排），从当前排(rowIndex)移动到前一排(rowIndex-1)
    /// 移动完成后更新 rowIndex，检查是否到达攻击距离
    ///
    /// 链式补齐：
    ///   - 补齐移动（isRush=true）在移动完全完成（moveProgress >= 1.0）时，
    ///     触发 OnRushMoveComplete 事件，通知列管理器启动下一个需要补齐的敌人。
    ///     必须等待前一敌人完全补齐完毕，后一敌人才能开始补齐。
    ///   - 每个敌人每次补齐只前进一排，不再连续多排补齐。
    ///     后续补齐通过延迟计时器（rushMoveDelayTimer）在移动完成后触发。
    ///     这样各敌人最终停在各自正确的排位置（rowIndex = listPosition），不会发生重合。
    ///
    /// 补齐延迟：
    ///   - 补齐移动完成后，如果还需要继续前进（rowIndex >= attackRange），
    ///     启动延迟计时器，等待 rushMoveDelay 秒后再开始下一次补齐移动。
    ///   - 这样可以将单次补齐移动速度（moveSpeed）加快，
    ///     而整体补齐节奏通过 rushMoveDelay 来调节，达到"快移动+停顿"的效果。
    /// </summary>
    internal void UpdateMovement()
    {
        // 教学手势拍：敌人站住不动（移动进度冻结，解除后从原进度继续）
        if (tutorialHold) return;
        if (!isMovingToNextRow) return;

        // 常规移动：基于 moveSpeed（秒/排）
        float speed = moveSpeed;
        moveProgress += Time.deltaTime / speed;

        if (moveProgress >= 1f)
        {
            bool wasRush = isRushMove;
            DebugLog.Info($"[Enemy] 移动完成: {DebugTag}, col={columnIndex}, oldRow={rowIndex}, newRow={rowIndex - 1}, isRush={wasRush}");
            moveProgress = 0f;
            isMovingToNextRow = false;
            isRushMove = false;

            // 移动结束切回 Idle（退出 Walk 动画）
            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.ResetTrigger("Walk");
            }
            PlayIdleVisual();
            rushMoveChainTriggered = false; // 重置链式触发标记

            // 移动完成：rowIndex 前进一排
            int oldRowForLog = rowIndex;
            rowIndex--;

            // 地刺检测：补齐到达新位置时触发
            SpikeTrapController.Instance?.CheckAndTrigger(this);
            if (state == EnemyState.Dead)
                return;

            // BUG FIX: 防止 rowIndex 变为负数
            if (rowIndex < 0) rowIndex = 0;
            if (rowIndex != oldRowForLog)
                DebugLog.Info($"[RowTrace] {DebugTag} row {oldRowForLog}→{rowIndex} | caller=UpdateMovement(moveComplete)");

            // Rush completion belongs to the exact scheduler order that started this step.
            if (wasRush)
            {
                var col = EnemyManager.Instance?.columnManager?.GetColumn(columnIndex);
                if (col != null)
                {
                    bool occupied = col.IsRowOccupied(rowIndex, this);
                    var manager = EnemyManager.Instance?.columnManager;
                    occupied |= manager != null && manager.IsCavalrySlotReserved(columnIndex, rowIndex, this);
                    if (_rushMoveMode == RushMoveMode.ContinuousEmptyRun)
                    {
                        if (_rushMoveOrderOwner == RushMoveOrderOwner.SpawnEntry)
                            occupied = manager != null && manager.IsCavalrySlotReserved(columnIndex, rowIndex, this);
                        else
                            occupied = manager == null || !manager.CanContinuousWaveEnterRow(this, columnIndex, rowIndex);
                    }
                    if (occupied)
                    {
                        rowIndex++;
                        state = EnemyState.Idle;
                        UpdateWorldPosition();
                        CompleteRushMoveOrder();
                        return;
                    }
                }
            }

            // 共享血量组：补齐移动后检查是否仍在同一排，不同排则解散
            // 但如果组内有成员正在移动中或等待补齐，跳过检查（rowIndex 仍在变动）
            if (sharedHealthGroup != null)
            {
                bool anyMemberInFlux = false;
                foreach (var m in sharedHealthGroup.members)
                {
                    if (m != this && m.state != EnemyState.Dead && (m.state == EnemyState.Moving || m.pendingRushMove))
                    {
                        anyMemberInFlux = true;
                        break;
                    }
                }
                if (!anyMemberInFlux)
                {
                    bool sameRow = true;
                    foreach (var m in sharedHealthGroup.members)
                    {
                        if (m != this && m.state != EnemyState.Dead && m.rowIndex != rowIndex)
                        {
                            sameRow = false;
                            break;
                        }
                    }
                    if (!sameRow)
                    {
                        DebugLog.Info($"[Enemy] 补齐移动导致解散共享血量组: {DebugTag}, row={rowIndex}");
                        sharedHealthGroup.Disband();
                    }
                }
            }

            // Boss 分阶段推进：到达第3排(rowIndex=2)，暂停等待前两排清空
            if (isBoss && rowIndex == 2 && bossState == BossState.None)
            {
                BossPause();
                // 先清理旧的补齐指令，再检查是否可以恢复推进。
                // BossResume 会分配新的 rush order，必须先清理旧 order 防止被误完成。
                if (HasRushMoveOrder)
                    CompleteRushMoveOrder();
                OnColumnsModifiedForBoss(); // 立即检查前两排是否已空，防止漏检
                return;
            }

            // 使用 attackRange 决定攻击距离
            int atkRange = (int)Mathf.Max(1, attackRange);
            bool reachedAttackRange = rowIndex < atkRange;

            if (wasRush)
            {
                if (!HasRushMoveOrder)
                {
                    state = EnemyState.Idle;
                }
                else if (targetRow >= 0 && rowIndex > targetRow)
                {
                    if (_rushMoveMode == RushMoveMode.ContinuousEmptyRun)
                    {
                        int nextRow = rowIndex - 1;
                        var columnManager = EnemyManager.Instance?.columnManager;
                        if (columnManager != null && !columnManager.CanAdvanceIntoRow(nextRow))
                        {
                            state = EnemyState.Idle;
                            CompleteRushMoveOrder();
                            return;
                        }
                    }

                    // Multi-row scheduler orders, including exact-slot push return, resume after the configured delay.
                    state = EnemyState.Idle;
                    pendingRushMove = true;
                    float delay = _rushMoveMode == RushMoveMode.ContinuousEmptyRun
                        ? 0f
                        : StageFormation?.GetRushMoveDelay() ?? 0f;
                    if (delay > 0f)
                        rushMoveDelayTimer = delay;
                    else
                        TryStartRushMove();
                }
                else
                {
                    if (reachedAttackRange)
                        StartAttacking();
                    else
                        state = EnemyState.Idle;

                    CompleteRushMoveOrder();

                    if (isBoss && rowIndex <= 1 && (bossState == BossState.Approaching || bossState == BossState.None))
                    {
                        var cm = EnemyManager.Instance?.columnManager;
                        if (cm != null && _onColumnsModifiedHandler != null)
                        {
                            cm.OnColumnsModified -= _onColumnsModifiedHandler;
                            _onColumnsModifiedHandler = null;
                        }
                        _bossEngageTimer = 1f;
                        DebugLog.Info($"[BOSS_ADVANCE] Boss到达应战排, 设置engageTimer=1s: {DebugTag}, col={columnIndex}, rowIndex={rowIndex}, bossState={bossState}");
                    }
                }
            }
            else
            {
                // 自然移动完成后，通知ColumnManager更新列内排序
                if (reachedAttackRange)
                {
                    StartAttacking();
                }
                else
                {
                    state = EnemyState.Idle;
                }
                EnemyManager.Instance?.OnEnemyMovedForward(this);
                DebugLog.Info($"[Enemy] 自然移动完成，触发 UpdateEnemyRow: {DebugTag}, col={columnIndex}");
            }
        }

        UpdateWorldPosition();
    }

    /// <summary>
    /// 更新攻击状态（三阶段攻击循环）
    ///   阶段1：攻击冷却（attackTimer > 0）→ 等待冷却
    ///   阶段2：AttackSpawn 前摇（DOTween，可被招架打断）→ 向前翻面，完成时造成伤害
    ///   阶段3：AttackDraw 收招（DOTween，不可打断）→ 返回原位
    ///
    /// 攻击优先级规则：
    ///   1. 冷却期间如果 pendingRushMove == true → 先执行补齐再攻击
    ///   2. AttackSpawn 动画期间 → 可被招架打断（CancelAttack）
    ///   3. AttackDraw 动画期间 → 不可中断，完成当前攻击后检查 pendingRushMove
    /// </summary>
    private void UpdateAttack()
    {
        // 教学手势拍：不再起新的一刀（已经挥出去的这一下让它播完，不硬切动画）
        if (tutorialHold && !isAttackAnimating) return;
        if (IsCoward || formState == EnemyFormState.ShieldBroken)
        {
            isAttackAnimating = false;
            isAttackDrawPhase = false;
            return;
        }
        if (_cavalry != null && _cavalry.IsMounted)
        {
            state = EnemyState.Idle;
            return;
        }

        if (isAttackAnimating)
        {
            // 阶段2：DOTween 攻击动画播放中
            // 动画完成后通过 OnComplete 回调处理（PerformAttack + 设置冷却 + TryStartRushMove）
            return;
        }
        else
        {
            // 受击停顿/受击动画期间不推进攻击冷却，避免 Hit 与 Attack Trigger 竞争。
            if (_hitStopRemaining > 0f || _hitFlashRoutine != null)
                return;

            // 阶段1：攻击冷却
            // BUG FIX: 冷却期间如果标记了需要补齐，先执行补齐再攻击
            if (pendingRushMove)
            {
                TryStartRushMove();
                return; // 如果成功开始移动，直接返回，不再处理冷却
            }

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                // 冷却结束，使用 DOTween 播放攻击动画
                PlayAttackAnimationTween();
            }
        }
    }

    private void PerformAttack()
    {
        if (_animator != null)
        {
            AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
            if (!current.IsName("Attack") && !current.IsName("CAttack"))
            {
                Debug.LogWarning($"[ATTACK_ANIM_DIAG] DamageWithoutAttackAnimation enemy={DebugTag} frame={Time.frameCount} state={state} animatorEnabled={_animator.enabled} currentHash={current.fullPathHash} currentTime={current.normalizedTime:F3} hitStop={_hitStopRemaining:F3}");
            }
        }

        // 通知玩家受到伤害
        // 由EnemyManager转发给PlayerState
        DebugLog.Info($"[Enemy] {DebugTag} PerformAttack 触发, attackDamage={attackDamage}");
        EnemyManager.Instance?.OnEnemyAttackPlayer(this);
    }

    private System.Collections.IEnumerator TraceAttackAnimatorEntry(string expectedState, int attackStartFrame)
    {
        yield return new WaitForSecondsRealtime(0.12f);
        if (_animator == null || state != EnemyState.Attacking || !isAttackAnimating)
            yield break;

        AnimatorStateInfo current = _animator.GetCurrentAnimatorStateInfo(0);
        AnimatorStateInfo next = _animator.GetNextAnimatorStateInfo(0);
        if (!current.IsName(expectedState) && !next.IsName(expectedState))
        {
            Debug.LogWarning($"[ATTACK_ANIM_DIAG] TriggerNotEntered enemy={DebugTag} startFrame={attackStartFrame} frame={Time.frameCount} expected={expectedState} animatorEnabled={_animator.enabled} inTransition={_animator.IsInTransition(0)} currentHash={current.fullPathHash} currentTime={current.normalizedTime:F3} nextHash={next.fullPathHash} hitStop={_hitStopRemaining:F3} hitFlashActive={_hitFlashRoutine != null}");
        }
    }

private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            DebugLog.Warning($"[Enemy] {DebugTag} isRanged=true 但 projectilePrefab 为空");
            return;
        }

        GameObject go = Instantiate(projectilePrefab);
        var proj = go.GetComponent<EnemyProjectile>();
        if (proj == null)
            proj = go.AddComponent<EnemyProjectile>();

        Vector3 startPos = transform.position;
        float camZ = Camera.main != null ? Camera.main.transform.position.z : 0f;
        float endZ = camZ + projectileZTargetOffset;
        float endX = useFixedProjectileLandingX && fixedProjectileLandingXByColumn != null &&
            columnIndex >= 0 && columnIndex < fixedProjectileLandingXByColumn.Length
            ? fixedProjectileLandingXByColumn[columnIndex]
            : projectileLandingXSpread > 0f
                ? projectileLandingXCenter + Random.Range(-projectileLandingXSpread, projectileLandingXSpread)
                : startPos.x + projectileXOffset;

        float durationMultiplier = arrowConfig != null ? arrowConfig.GetFlightDurationMultiplierForRow(rowIndex) : 1f;
        float arcHeightMultiplier = arrowConfig != null ? arrowConfig.GetArcHeightMultiplierForRow(rowIndex) : 1f;
        float maxDescentPitch = arrowConfig != null ? arrowConfig.maxDescentPitch : 89f;
        proj.Launch(startPos, endZ, endX, attackDamage, projectileArcHeight * arcHeightMultiplier,
            projectileFlyDuration * durationMultiplier, this, maxDescentPitch: maxDescentPitch);

        DebugLog.Info($"[Enemy] {DebugTag} 发射飞行物: start=({startPos.x:F1},{startPos.y:F1},{startPos.z:F1}) endZ={endZ:F1} endX={endX:F1}");
    }

    /// <summary>
    /// 使用 DOTween 播放攻击动效（三阶段攻击循环）
    ///   AttackSpawn（前摇翻面）：向前移动 + 镜像翻转，完成时造成伤害，可被招架打断
    ///   AttackDraw（收招返回）：后退到原位 + 翻转回正，不可被招架打断
    ///   冷却阶段（AttackSpeed）：等待 attackTimer 倒计时，冷却结束后再次进入 AttackSpawn
    ///
    ///   按 attackSequence 顺序循环执行每步攻击
    /// </summary>
    private void PlayAttackAnimationTween()
    {
        if (attackSequence == null || attackSequence.Count == 0)
        {
            DebugLog.Warning("[Enemy] attackSequence 为空，跳过攻击");
            return;
        }

        var step = attackSequence[_currentAttackStep];
        _currentAttackStep = (_currentAttackStep + 1) % attackSequence.Count;

        bool isCAttack = step.isCAttack;
        float spawnDuration = step.spawnDuration;
        float drawDuration = step.drawDuration;
        float extraCooldown = step.extraCooldown;
        bool useFlip = step.useFlip;
        currentStepSpawnDuration = spawnDuration;
        float approachDuration = spawnDuration;
        float firstResolveHitTime = GetFirstResolveHitTime(step);
        if (firstResolveHitTime >= 0f)
        {
            approachDuration = firstResolveHitTime;
        }
        float telegraphDuration = Mathf.Min(0.3f, Mathf.Max(0f, approachDuration));

        // ── 命中帧 ─────────────────────────────────────────────────────────────
        // 声明命中帧后：出伤/击发时刻由「命中帧精灵出现在屏幕上」决定（见 UpdateAttackHitFrames），
        // 收招时长改为 clip 剩余时间，使整段位移与动画同时结束。
        SetupHitFrames(step);
        bool hasDeclaredHitFrames = _hitFrameSprites.Count > 0;
        float planClipLength = 0f;
        if (hasDeclaredHitFrames)
        {
            string clipTrigger = string.IsNullOrEmpty(step.animationTrigger)
                ? (isCAttack ? "CAttack" : "Attack")
                : step.animationTrigger;
            AnimationClip planClip = AttackHitFrameResolver.ResolveClipForTrigger(
                _animator != null ? _animator.runtimeAnimatorController : null, clipTrigger);
            if (planClip != null)
            {
                planClipLength = planClip.length;
                if (planClipLength > approachDuration)
                    drawDuration = planClipLength - approachDuration;
            }
            else
            {
                DebugLog.Warning($"[Enemy] {DebugTag} 命中帧已声明但解析不到攻击 clip（trigger={clipTrigger}），沿用 spawnDuration/drawDuration");
            }
        }

        // 直接切回 Idle（不用 SetTrigger，避免残留 Idle 触发器与 Launch 竞争）
        _animator?.Play("Idle", 0, 0f);
        // 攻击动画基准速度固定为 1：补齐移动会把 speed 提到 2×（见 StartMoving），残留会让动画与位移/命中错拍
        if (_animator != null) _animator.speed = 1f;

        isAttackAnimating = true;
        isAttackDrawPhase = false;
        isCFrame = isCAttack;
        UpdateOutlineState();
        string trigger = string.IsNullOrEmpty(step.animationTrigger) ? "Attack" : step.animationTrigger;
        int attackStartFrame = Time.frameCount;
        _animator?.SetTrigger(trigger);
        StartCoroutine(TraceAttackAnimatorEntry(trigger, attackStartFrame));

        Vector3 startPos = transform.localPosition;
        Vector3 startScale = transform.localScale;

        float totalInterval = (1f / attackSpeed);
        float forwardDistance = 0.5f;

        _attackTween = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
        _attackTween.SetTarget(transform);
        _attackTween.SetId("attackAnim");
        _suspendTweenPaused = false;

        // 远程攻击：不移动；声明命中帧时由命中帧决定击发时刻
        if (isRanged)
        {
            if (hasDeclaredHitFrames)
            {
                // 击发出现在声明命中帧上（见 UpdateAttackHitFrames），这里只把时长覆盖到 clip 结束
                float rangedLength = planClipLength > 0f ? planClipLength : (spawnDuration + drawDuration);
                _attackTween.AppendInterval(Mathf.Max(0.1f, rangedLength));
            }
            else
            {
                // AttackSpawn 结束 → 发射飞行物
                _attackTween.AppendInterval(spawnDuration);
                _attackTween.AppendCallback(() =>
                {
                    SpawnProjectile();
                    isAttackDrawPhase = true;
                });
                // AttackDraw 阶段：等待到 Animator clip 结束（而非硬编码 drawDuration）
                float clipLength = _attackClip != null ? _attackClip.length : (spawnDuration + drawDuration);
                float remainingTime = Mathf.Max(0.1f, clipLength - spawnDuration);
                _attackTween.AppendInterval(remainingTime);
            }
        }
        else
        {
            // 近战：向前 + 翻转（AttackSpawn）
            _attackTween.Append(transform.DOLocalMoveZ(startPos.z - forwardDistance, approachDuration).SetEase(Ease.OutQuad));
            if (useFlip)
                _attackTween.Join(transform.DOScaleX(-startScale.x, approachDuration).SetEase(Ease.OutQuad));

            if (!hasDeclaredHitFrames)
            {
                // AttackSpawn 完成 → 造成伤害，进入 AttackDraw 收招阶段
                _attackTween.AppendCallback(() =>
                {
                    PerformAttack();
                    isAttackDrawPhase = true; // 进入收招阶段，此后不可被招架打断
                    isCFrame = false;          // 伤害帧结束霸体窗口
                    UpdateOutlineState();
                });
            }

            // AttackDraw：后退到原位 + 翻转回正（不可被招架打断）
            // 声明命中帧时 drawDuration 已被改写为 clip 剩余时间，整段位移与动画同时结束
            _attackTween.Append(transform.DOLocalMoveZ(startPos.z, drawDuration).SetEase(Ease.InQuad));
            if (useFlip)
                _attackTween.Join(transform.DOScaleX(startScale.x, drawDuration).SetEase(Ease.InQuad));
        }

        // 预警回调必须在时间线构建完成后再插入：InsertCallback 会把 Sequence 时长撑到该位置，
        // 若在 Append 之前插入（历史 BUG），后续元素会整体后移，出伤被推到整段动画结束之后。
        if (_attackTelegraph != null && telegraphDuration > 0f)
        {
            float telegraphStart = Mathf.Max(0f, approachDuration - telegraphDuration);
            _attackTween.InsertCallback(telegraphStart,
                () => _attackTelegraph?.BeginWarning(telegraphDuration));
        }

        // 收招完成
        _attackTween.OnComplete(() =>
        {
            _attackTelegraph?.StopWarning();
            _attackTween = null;
            // 兜底：声明了命中帧却整段都没出现（动画被受击打断或配置写错）
            ResolvePendingHitFramesOnAttackEnd();
            isAttackAnimating = false;
            isAttackDrawPhase = false;
            isCFrame = false;
                UpdateOutlineState();

            if (isBoss)
            {
                // 若已被 Stun/Launch 打断，不覆盖状态
                if (state != EnemyState.Stunned && state != EnemyState.Launched && state != EnemyState.Frozen)
                {
                    state = EnemyState.Idle;
                    SetBossActionCooldown();
                }
            }
            else
            {
                if (state != EnemyState.Stunned && state != EnemyState.Launched && state != EnemyState.Frozen)
                {
                    int atkRange = (int)Mathf.Max(1, attackRange);
                    if (HasRushMoveOrder)
                    {
                        float cooldown = totalInterval * 0.4f + extraCooldown;
                        if (cooldown < 0.1f) cooldown = 0.1f;
                        attackTimer = cooldown;
                    }
                    else if (rowIndex < atkRange)
                    {
                        state = EnemyState.Attacking;
                        float cooldown = totalInterval * 0.4f + extraCooldown;
                        if (cooldown < 0.1f) cooldown = 0.1f;
                        attackTimer = cooldown;
                    }
                    else
                    {
                        state = EnemyState.Idle;
                    }
                }
            }

            // Attack completion may only resume an already owned order.
            if (state != EnemyState.Stunned && state != EnemyState.Launched && state != EnemyState.Frozen)
            {
                PlayIdleVisual();
                if (HasRushMoveOrder)
                    TryStartRushMove();
            }
        });
    }

    #endregion
}