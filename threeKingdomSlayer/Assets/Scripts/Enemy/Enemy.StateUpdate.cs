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

    #endregion
}