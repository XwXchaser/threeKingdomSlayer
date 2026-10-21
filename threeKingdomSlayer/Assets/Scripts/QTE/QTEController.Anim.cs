using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// QTEController 的**QTE 动画**（翻牌 / 提示动画的播放与节奏）。
    ///
    /// 按机制从 <c>QTEController.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 QTEController.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class QTEController : MonoBehaviour
{
    #region QTE 动画

    private void StartQTEAnimation()
    {
        if (_currentAttack == null)
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: _currentAttack 为 null, 跳过");
            return;
        }
        if (_animator == null)
            _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: Animator 组件缺失, 跳过");
            return;
        }

        // 强制启用 Animator（防止上一轮 QTEEnd 后 Animator 被意外禁用）
        if (!_animator.enabled)
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: Animator 被禁用，重新启用");
            _animator.enabled = true;
        }

        DebugLog.Info($"[QTEController] StartQTEAnimation: attackIndex={_currentAttackIndex}, enabled={_animator.enabled}, goActive={_animator.gameObject.activeInHierarchy}, currentState={_animator.GetCurrentAnimatorStateInfo(0).shortNameHash}");

        // 清除所有 QTE 相关 trigger 防止状态机残留
        _animator.ResetTrigger("QTETripleStab");
        _animator.ResetTrigger("QTESweep");
        _animator.ResetTrigger("QTEAttack");
        _animator.ResetTrigger("QTEEnd");
        _animator.ResetTrigger("QTEBlocked");
        _animator.ResetTrigger("QTEHit");

        // 清除基础攻击/受击 trigger，防止 Parry 等残留的 Hit trigger 劫持 QTE 转换
        // （AnyState → HitFlash 优先级高于 Idle → QTE_*，会导致 Boss 播受击动画而非 QTE 动画）
        _animator.ResetTrigger("Hit");
        _animator.ResetTrigger("Walk");
        _animator.ResetTrigger("Attack");
        _animator.ResetTrigger("CAttack");

        // 强制回到Idle确保Animator从干净状态开始
        _animator.Play("Idle", 0, 0f);
        // BUG FIX: 某些Unity版本 Play+Update(0) 不完全重置，额外做一次
        _animator.Update(0f);
        _animator.Play("Idle", 0, 0f);

        var curState = _animator.GetCurrentAnimatorStateInfo(0);
        DebugLog.Info($"[QTEController] StartQTEAnimation: 重置后状态 IsName(Idle)={curState.IsName("Idle")}, shortNameHash={curState.shortNameHash}");

        if (_currentAttack.UseMultiPhaseAnimation)
        {
            _animator.SetTrigger("QTETripleStab");
            DebugLog.Info("[QTEController] 设置 QTETripleStab trigger");
        }
        else if (_currentAttack.UseBranchedAnimation)
        {
            _animator.SetTrigger("QTESweep");
            DebugLog.Info("[QTEController] 设置 QTESweep trigger");
            // 验证 trigger 是否被设置
            var nextState = _animator.GetCurrentAnimatorStateInfo(0);
            DebugLog.Info($"[QTEController] SetTrigger后状态: IsName(Idle)={nextState.IsName("Idle")}, shortNameHash={nextState.shortNameHash}");
        }
        else if (_currentAttack.qteAnimationClip != null)
        {
            _animator.SetTrigger("QTEAttack");
            DebugLog.Info("[QTEController] 设置 QTEAttack trigger");
        }
        else
        {
            DebugLog.Info("[QTEController] StartQTEAnimation: 无匹配动画类型");
        }
    }

    private void StopQTEAnimation()
    {
        if (_animator == null) return;
        _animator.speed = 1f;  // 恢复默认速度

        if (_currentAttack != null && (_currentAttack.UseMultiPhaseAnimation || _currentAttack.UseBranchedAnimation))
        {
            _animator.SetTrigger("QTEEnd");
        }
        else
        {
            _animator.Play("Idle");
        }
    }

    #endregion
}