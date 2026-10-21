using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**出招推进**：攻击状态、动画播放与命中时机。
///
/// 按机制从 <c>Enemy.StateUpdate.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.StateUpdate.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
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
}