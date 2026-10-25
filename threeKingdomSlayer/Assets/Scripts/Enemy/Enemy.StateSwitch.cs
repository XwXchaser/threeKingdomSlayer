using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// Enemy 状态机的**状态切换**：进入 / 退出各状态时的处理（从 <c>Enemy.State.cs</c> 拆出）。
    ///
    /// 按机制从 <c>Enemy.State.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class Enemy : MonoBehaviour
{
    #region 状态切换

    /// <summary>
    /// 开始向前移动（常规移动，基于 moveSpeed 秒数）
    /// BUG FIX: 当 rowIndex == 0 时，敌人已经到达最前排，不应再向前移动。
    /// 如果此时调用 StartMoving()，UpdateWorldPosition() 中 isMovingToNextRow == true 时，
    /// targetRowZ = -((0 - 1) * rowSpacing) = +rowSpacing（正数），
    /// 敌人会向远离玩家的方向（正 Z 方向）移动，表现为"后退"。
    /// 修复：rowIndex == 0 时直接进入 Attacking 状态。
    ///
    /// BUG FIX: 如果已经在 Moving 状态，不重置 moveProgress。
    /// 否则当 Column.RemoveEnemy() 或 ColumnManager.UpdateEnemyRow() 中调用 StartMoving()
    /// 时，会重置 moveProgress = 0f，导致正在移动中的敌人回到移动起点，看起来像是后退。
    ///
    /// 注意：保护检查只基于 state，不检查 isMovingToNextRow。
    /// 因为 ResetMovementState() 会重置 isMovingToNextRow = false，
    /// 如果保护检查依赖 isMovingToNextRow，会导致保护失效，
    /// moveProgress 被重置为 0，造成"无限补齐"的无限循环。
    /// </summary>
    /// <summary>
    /// 开始向前移动
    /// </summary>
    /// <param name="isRush">是否为补齐移动（由死亡/重排列触发）。补齐移动完成后不触发 UpdateEnemyRow()。</param>
    public void StartMoving(bool isRush = false)
    {
        if (state == EnemyState.Dead) return;
        if (!isBoss && !isRush) return;
        if (isRush && !HasRushMoveOrder) return;

        // 逐排补齐模式：非补齐移动时，检查前方整排是否已完全清空
        if (!isRush && StageFormation != null && !IsFrontRowClear())
        {
            DebugLog.Info($"[Enemy] PerRow 模式等待前排清空: {DebugTag}, col={columnIndex}, row={rowIndex}");
            return;
        }

        // 如果敌人已在攻击范围内，直接进入攻击状态而非移动
        // 补齐移动（isRush=true）优先级高于攻击：即使已在攻击范围内，也先向前补齐空位
        int atkRange = (int)Mathf.Max(1, attackRange);
        if (!isRush && rowIndex < atkRange)
        {
            StartAttacking();
            return;
        }

        // 补齐移动：如果已在目标位置或之前，不需要移动，恢复攻击
        if (isRush && targetRow >= 0 && rowIndex <= targetRow)
        {
            if (rowIndex < atkRange)
                StartAttacking();
            else
                state = EnemyState.Idle;
            return;
        }

        // BUG FIX: 如果已经在 Moving 状态，不重置 moveProgress
        // 避免被 Column.RemoveEnemy() / ColumnManager.UpdateEnemyRow() 重复调用时
        // 重置移动进度，导致敌人回到移动起点（看起来像后退）
        // 注意：不检查 isMovingToNextRow，因为 ResetMovementState() 会重置它
        if (state == EnemyState.Moving)
        {
            return;
        }

        isRushMove = isRush;
        DebugLog.Info($"[Enemy] StartMoving: {DebugTag}, col={columnIndex}, row={rowIndex}, targetRow={rowIndex - 1}, isRush={isRush}");
        state = EnemyState.Moving;
        isMovingToNextRow = true;
        moveProgress = 0f;

        // 仅1021：破盾不抢走 ShieldBreak；胆怯移动保留无盾姿态，不改变102等敌人的行为。
        if (isRush && (_shieldGuard == null || formState == EnemyFormState.Normal))
        {
            _animator?.SetTrigger("Walk");
            // 加速 Walk 动画，使两帧都能在 moveSpeed 时间内可见
            // Walk clip 约 0.6s 循环，需在 moveSpeed 时间内至少完成一次循环
            if (_animator != null && moveSpeed > 0f)
                _animator.speed = Mathf.Max(1f, 0.6f / moveSpeed);
        }

        // DOTween: 前进补齐时 Y 轴弹跳动画（一边前进一边沿 Y 轴跳动）
        if (isRush)
        {
            DOTween.Kill(transform, false); // 终止之前可能残留的弹跳动画
            StopHitScaleFeedback();
            transform.localScale = originalScale; // 重置被 DOPunchScale 打断后的残留 scale
            float moveDuration = moveSpeed;
            float bounceHeight = 0.5f; // 弹跳峰值高度
            // 通过 DOTween.To 驱动 bounceYOffset，形成抛物线弹跳轨迹
            DOTween.To(() => bounceYOffset, x => bounceYOffset = x, bounceHeight, moveDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .SetTarget(transform)
                .SetId("rushBounce");
            DOTween.To(() => bounceYOffset, x => bounceYOffset = x, 0f, moveDuration * 0.5f)
                .SetEase(Ease.InQuad)
                .SetDelay(moveDuration * 0.5f)
                .SetTarget(transform)
                .SetId("rushBounce");
        }
    }


    /// <summary>
    /// 开始攻击
    /// BUG FIX: 攻击分为两个阶段：
    ///   阶段1：攻击冷却（attackTimer）— 先进入冷却再攻击
    ///   阶段2：攻击动画（attackAnimTimer）— 冷却结束后播放攻击动画并造成伤害
    /// 这样敌人进入攻击范围后不会立即攻击，而是先进入冷却，符合"先冷却后攻击"的规则。
    ///
    /// 攻击优先级规则（Problem 1 修复）：
    ///   1. 进入攻击范围 → 先进入攻击冷却，再执行攻击
    ///   2. 攻击冷却期间需要向前补齐 → 先补齐再攻击
    ///   3. 正在攻击动画中 → 完成当前攻击后再判断是否需要向前补齐
    /// </summary>
    public void StartAttacking()
    {
        if (state == EnemyState.Dead || IsCoward || formState == EnemyFormState.ShieldBroken) return;
        // 骑乘期间不做普通攻击：攻击行为全部由 CavalryEnemy 的冲锋循环负责。
        if (_cavalry != null && _cavalry.IsMounted)
        {
            state = EnemyState.Idle;
            return;
        }

        // Boss 首次进入攻击状态：若尚未进入 InCombat，立即进入并创建血条
        if (isBoss && bossState == BossState.None)
        {
            bossState = BossState.InCombat;
            DebugLog.Info($"[Enemy] Boss直接进入战斗(rowIndex={rowIndex}): {DebugTag}, col={columnIndex}");
            OnBossEngaged?.Invoke(this);
        }

        state = EnemyState.Attacking;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        // 使用与攻击动画 OnComplete 中相同的冷却计算，保证补齐后首次攻击也遵循攻击 CD
        float totalInterval = (1f / attackSpeed);
        float cooldown = totalInterval * 0.4f;
        if (cooldown < 0.1f) cooldown = 0.1f;
        attackTimer = cooldown;
    }

    public void Stun(float duration)
    {
        if (state == EnemyState.Dead) return;
        if (state == EnemyState.Stunned) return;
        // 骑乘期间免疫眩晕；落马后恢复普通受控。
        if (_cavalry != null && _cavalry.IsMounted) return;
        // 击飞状态下不允许进入眩晕，否则敌人会冻结在半空
        // 但需要重置 Poise（相当于击飞中破防不触发眩晕，只重置架势）
        if (state == EnemyState.Launched)
        {
            currentPoise = maxPoise;
            OnPoiseChanged?.Invoke(this, currentPoise, maxPoise);
            return;
        }

        // 眩晕时清理移动状态，避免 DOTween 残留和状态不一致
        // 若正在补齐移动，恢复 pendingRushMove 标记，确保眩晕结束后链式补齐能继续
        if (state == EnemyState.Moving)
        {
            if (isRushMove && HasRushMoveOrder)
            {
                pendingRushMove = true;
                if (_animator != null)
                {
                    _animator.speed = 1f;
                    _animator.ResetTrigger("Walk");
                }
            }
            DOTween.Kill(transform, false);
            StopHitScaleFeedback();
            transform.localScale = originalScale;
            isMovingToNextRow = false;
            isRushMove = false;
            moveProgress = 0f;
            UpdateWorldPosition();
        }
        // 眩晕时清理攻击状态，Kill 攻击动画避免 OnComplete 覆盖 Stun 状态
        else if (state == EnemyState.Attacking)
        {
            _attackTelegraph?.StopWarning();
            if (_attackTween != null && _attackTween.IsActive())
            {
                _attackTween.Kill();
                _attackTween = null;
            }
            transform.DOKill(false);
            StopHitScaleFeedback();
            transform.localScale = originalScale;
            isAttackAnimating = false;
            isAttackDrawPhase = false;
                UpdateWorldPosition();
        }

        // 清除霸体状态，描边随之更新
        isSuperArmor = false;
        isCFrame = false;

        state = EnemyState.Stunned;
        UpdateOutlineState();
        stunTimer = duration;
        _appliedStunDuration = duration;
        _poiseRecoveryEndTime = Time.time + duration;
        _isStunExiting = false;
        _resumeStunAfterLaunch = false;
        if (_stunExitRoutine != null)
        {
            StopCoroutine(_stunExitRoutine);
            _stunExitRoutine = null;
        }

        if (isBoss)
        {
            if (_hitFlashRoutine != null)
            {
                StopCoroutine(_hitFlashRoutine);
                _hitFlashRoutine = null;
            }
            _animator?.ResetTrigger("Hit");
            _animator?.Play("StunStart", 0, 0f);
        }
        else
        {
            GetComponent<EnemySpriteController>()?.TriggerHitFlash();
        }
    }

    /// <summary>
    /// 挑飞：打断当前攻击动作（类似招架），将敌人击飞（使用敌人自身默认击飞时长）
    /// 若已处于击飞状态则忽略（延长由 TakeDamage 处理）
    /// </summary>
    public void Launch()
    {
        Launch(launchDuration);
    }

    /// <summary>
    /// 挑飞：使用自定义击飞时长（供旋风等技能覆盖默认时长）
    /// </summary>
    public void Launch(float customDuration)
    {
        if (state == EnemyState.Dead) return;
        if (_getupRoutine != null)
        {
            StopCoroutine(_getupRoutine);
            _getupRoutine = null;
        }
        _getupInterrupted = false;
        if (_cavalry != null && _cavalry.IsMounted)
            _cavalry.DismountFromLaunch();
        if (state == EnemyState.Launched)
        {
            // 重新击飞：保持原始地面基准，仅重置击飞参数（供 Cyclone 等技能对已击飞敌人再次触发）
            launchTimer = customDuration;
            currentLaunchYHeight = Random.Range(launchYHeightMin, launchYHeightMax);
            launchVelocityY = Mathf.Sqrt(2f * launchGravity * currentLaunchYHeight);
            _launchExtendCount = 0;
            PlayLaunchVisual();
            DebugLog.Info($"[Enemy] 重新击飞: {DebugTag}, duration={customDuration:F2}s, v0={launchVelocityY:F2}");
            return;
        }

        // 保存被中断的眩晕剩余时间，落地后恢复
        _remainingStunOnLaunch = (state == EnemyState.Stunned && stunTimer > 0f) ? stunTimer : 0f;
        _resumeStunAfterLaunch = state == EnemyState.Stunned || _isStunExiting;
        if (_stunExitRoutine != null)
        {
            StopCoroutine(_stunExitRoutine);
            _stunExitRoutine = null;
        }
        _isStunExiting = false;

        // 清理所有 DOTween 动效（攻击动画、受击抖动等）
        _attackTelegraph?.StopWarning();
        StopHitScaleFeedback();
        transform.DOKill(false);
        DOTween.Kill(transform, false);
        if (_attackTween != null && _attackTween.IsActive())
        {
            _attackTween.Kill();
            _attackTween = null;
        }
        UpdateWorldPosition();
        transform.localScale = originalScale;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        UpdateOutlineState();
        isMovingToNextRow = false;
        pendingRushMove = HasRushMoveOrder;

        state = EnemyState.Launched;
        _animator?.ResetTrigger("Hit");
        if (!IsCoward || !_cowardTransitionPlaying)
            PlayLaunchVisual();
        launchTimer = customDuration;
        launchStartLocalPos = transform.localPosition;
        currentLaunchYHeight = Random.Range(launchYHeightMin, launchYHeightMax);
        launchVelocityY = Mathf.Sqrt(2f * launchGravity * currentLaunchYHeight);
        _launchExtendCount = 0;

        DebugLog.Info($"[Enemy] 挑飞: {DebugTag}, duration={customDuration:F2}s, v0={launchVelocityY:F2}");
    }

    // ── 击飞状态查询（供 CycloneEffect 等外部读取）──
    public Vector3 LaunchStartLocalPos => launchStartLocalPos;
    public float CurrentLaunchYHeight => currentLaunchYHeight;
    public bool IsLaunchRising => launchVelocityY > 0f;

    /// <summary>
    /// 延长浮空时间（被攻击命中时调用）
    /// 从当前位置叠加反弹速度，产生"颠球"效果
    /// 连续延长时间逐次衰减：0.50/0.40/0.30/0.20/0.15s（第5次起为下限）
    /// </summary>
    public void ExtendLaunch(float extendTime)
    {
        if (state != EnemyState.Launched) return;

        // 衰减表：按已延长次数递减
        float[] decayTable = { 0.50f, 0.40f, 0.30f, 0.20f, 0.15f };
        float actualExtend = _launchExtendCount < decayTable.Length
            ? decayTable[_launchExtendCount]
            : decayTable[decayTable.Length - 1];
        _launchExtendCount++;

        launchTimer += actualExtend;
        launchVelocityY = launchReboundVelocity;
        PlayLaunchVisual();
        DebugLog.Info($"[Enemy] 延长浮空: {DebugTag}, +{actualExtend:F2}s (base={extendTime:F2}, count={_launchExtendCount}), 剩余={launchTimer:F2}s");
    }

    /// <summary>
    /// 检查敌人当前是否可被击飞（挑飞）
    /// Boss：必须 poise 归零进入 Stun 后才能被 Launch
    /// 普通敌人：currentPoise < poiseDamage 即可击飞，无需 Stun 前置
    /// ForceLaunch Buff：始终可击飞
    /// </summary>
    public bool CanBeLaunched(float poiseDamage = 0f)
    {
        // 盾兵（1021）持盾或处于破盾表现期：挑飞打在盾上，不吃击飞而是脱盾。
        // 只对有 ShieldGuardEnemy 的单位生效，102 等其它敌人行为不变。
        if (_shieldGuard != null
            && (formState == EnemyFormState.Normal || formState == EnemyFormState.ShieldBroken))
            return false;
        if (PlayerState.Instance != null && PlayerState.Instance.HasBuff(BuffIds.ForceLaunch))
            return true;
        if (isBoss)
            return state == EnemyState.Stunned;
        return currentPoise <= poiseDamage;
    }

    /// <summary>
    /// 打断当前攻击动作（招架触发）
    /// BOSS: 回到 Idle 等待调度；非BOSS: 返回攻击冷却阶段
    /// </summary>
    public bool CancelAttack()
    {
        if (state == EnemyState.Dead) return false;

        // 全阶段可打断：AttackDraw / AttackSpawn / 冷却阶段均可打断
        // 清理 DOTween 残留（修复形变不恢复问题）
        _attackTelegraph?.StopWarning();
        transform.DOKill(false);
        StopHitScaleFeedback();

        if (_attackTween != null && _attackTween.IsActive())
        {
            _attackTween.Kill();
            _attackTween = null;
        }
        UpdateWorldPosition();
        transform.localScale = originalScale;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        _animator?.Play("Idle", 0, 0f);
        UpdateOutlineState();

        if (isBoss)
        {
            state = EnemyState.Idle;
            SetBossActionCooldown();
        }
        else
        {
            // 打断不把冷却推回**满间隔**。
            // 原因：满间隔重置 + 「受击期间冷却不推进」= 连续命中的玩家能把敌人的攻击永久压制
            // （数值草案 §2.0「Hit数与硬直压制」记录的就是这个坑；实测：每 0.3s 命中一次时，
            //  敌人 8 秒内只打出 1 次攻击，几乎不还手）。
            // 改法：与正常循环用同一个冷却公式（0.4×间隔，与 StartAttacking / 攻击完成一致），
            // 保证被打断后仍按原节奏重新准备攻击，不会被玩家锁死。
            float totalInterval = (1f / attackSpeed);
            float cooldown = totalInterval * 0.4f;
            if (cooldown < 0.1f) cooldown = 0.1f;
            attackTimer = cooldown;
        }

        DebugLog.Info($"[Enemy] 打断攻击成功: {DebugTag}, col={columnIndex}");
        return true;
    }

    /// <summary>
    /// Parry 式攻击打断（只做效果：不造成伤害、不跳字、不触发受击动画/闪白/卡肉）。
    /// 门控与 TakeDamage 的「Parry 打断」分支等价（即 canInterruptCFrame 与 isParryInterrupt 同时为真）：
    /// 处于攻击动画中且不在收招段（AttackDraw）的敌人都可被打断，包括 Boss 的霸体/CFrame。
    /// 与 TakeDamage 的早退保持一致：死亡、Boss 未进入战斗、转阶段无敌、锁血期间都不打断。
    /// 返回是否真的打断了敌人的攻击。
    /// </summary>
    public bool TryInterruptAttackByParry()
    {
        if (state == EnemyState.Dead) return false;
        if (isBoss && bossState != BossState.InCombat) return false;
        if (isPhaseTransitioning || _healthLocked) return false;
        if (state != EnemyState.Attacking || !isAttackAnimating || isAttackDrawPhase) return false;

        return CancelAttack();
    }

    /// <summary>
    /// 进入 QTE 攻击状态（由 QTEController 调用）
    /// 中断当前攻击，切换到 QTEAttacking 状态
    /// </summary>

    #endregion
}