using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**QTE 攻击**：进入/退出 QTE 攻击状态与 QTE 控制器获取。
///
/// 按机制从 <c>Enemy.StateSwitch.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.StateSwitch.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
    public void EnterQTEAttack()
    {
        if (state == EnemyState.Dead) return;

        // 清理上一轮延迟Stun标记
        deferredStun = false;

        // 中断当前攻击动画
        _attackTelegraph?.StopWarning();
        if (_attackTween != null && _attackTween.IsActive())
        {
            _attackTween.Kill();
            _attackTween = null;
        }
        transform.DOKill(false);
        StopHitScaleFeedback();
        UpdateWorldPosition();
        transform.localScale = originalScale;
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isCFrame = false;
        isMovingToNextRow = false;

        state = EnemyState.QTEAttacking;
        UpdateOutlineState();
        DebugLog.Info($"[Enemy] 进入QTE攻击: {DebugTag}");
        Debug.Log($"[QTE_FLIP_DIAG] Enemy state -> QTEAttacking frame={Time.frameCount}");
    }

    /// <summary>
    /// QTE 攻击完成回调（状态已在 ExitQTEAttack 中处理）
    /// </summary>
    private void OnQTEAttackFinished()
    {
        DebugLog.Info($"[Enemy] QTE攻击完成: {DebugTag}");
    }

    /// <summary>
    /// 退出 QTE 攻击状态（由 QTEController 调用）
    /// </summary>
    public void ExitQTEAttack()
    {
        // 动画标记清理仅在 QTEAttacking 态执行
        if (state == EnemyState.QTEAttacking)
        {
            isAttackAnimating = false;
            isAttackDrawPhase = false;
            isCFrame = false;
            }

        // 状态恢复和冷却设置：即使 AbortQTE 导致 state 提前变化也要执行
        if (state == EnemyState.QTEAttacking || state == EnemyState.Idle || state == EnemyState.Attacking)
        {
            if (isBoss)
            {
                state = EnemyState.Idle;
                Debug.Log($"[QTE_FLIP_DIAG] Enemy state -> Idle (ExitQTEAttack boss) frame={Time.frameCount}");
                var phase = GetCurrentPhaseData();
                actionCooldownTimer = phase != null ? phase.postQTECooldown : 3f;
            }
            else
            {
                state = EnemyState.Attacking;
                float totalInterval = (1f / attackSpeed);
                attackTimer = totalInterval * 0.4f;
                if (attackTimer < 0.1f) attackTimer = 0.1f;
            }
        }

        // 延迟Stun：QTE期间poise归零，等动画播完再进入Stun
        if (deferredStun)
        {
            deferredStun = false;
            DebugLog.Info($"[Enemy] QTE结束执行延迟Stun: {DebugTag}");
            Stun(stunDuration);
        }

        // QTE 中血量跌到转阶段阈值：QTE 结束后补执行转阶段（此时已退出 QTE，EnterPhaseTransition 不再 AbortQTE）
        // 若同时挂起 deferredStun，PhaseTransitionSequence 会先消化 Stunned 再转阶段
        if (_deferredPhaseTransition != null && state != EnemyState.Dead)
        {
            var pending = _deferredPhaseTransition;
            _deferredPhaseTransition = null;
            DebugLog.Info($"[Enemy] QTE结束执行挂起转阶段: {DebugTag}, pendingPhase={pending.phaseIndex}");
            EnterPhaseTransition(pending);
            return;
        }

        UpdateOutlineState();
        if (HasRushMoveOrder && state != EnemyState.Stunned && state != EnemyState.Launched && state != EnemyState.Frozen)
            TryStartRushMove();
        DebugLog.Info($"[Enemy] 退出QTE攻击: {DebugTag}");
    }

    /// <summary>
    /// 获取 QTEController（懒加载缓存）
    /// </summary>
    public QTEController GetQTEController()
    {
        if (_qteController == null)
            _qteController = GetComponent<QTEController>();
        return _qteController;
    }
}