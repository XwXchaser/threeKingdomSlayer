using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// StabSweepEffect 的**命中判定与中断**：卡肉暂停、命中前取消、射线命中扫描与出伤。
    ///
    /// 按机制从 <c>StabSweepEffect.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 StabSweepEffect.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class StabSweepEffect : MonoBehaviour
{
    public void AbortAndDestroy()
    {
        _chargeHoldActive = false;
        _chargeRecoverActive = false;
        _hitsEnabled = false;
        if (_hitDeformationRoutine != null)
        {
            StopCoroutine(_hitDeformationRoutine);
            _hitDeformationRoutine = null;
        }
        if (_visualTransform != null) _visualTransform.DOKill();
        Destroy(gameObject);
    }

    /// <summary>
    /// 在首次命中结算前取消本次戳击，用于「按下即出轻攻击」模型下的起手改写。
    /// 返回 false 表示已经命中或序列已结束，不可取消。
    /// 取消不会触发能量、被动计数、击退波与收招回调。
    /// </summary>
    public bool TryCancelBeforeHit()
    {
        if (_hitAny || _sequence == null || !_sequence.IsActive())
            return false;

        _sequence.Kill();
        Destroy(gameObject);
        return true;
    }

    private void PauseSequenceForHitStop(HitFeedbackStrength feedbackStrength)
    {
        if (_chargeHoldActive) return;
        if (_sequence == null || !_sequence.IsActive()) return;
        if (_hitStopRoutine != null)
            StopCoroutine(_hitStopRoutine);
        _hitStopRoutine = StartCoroutine(HitStopRoutine(HitFeedbackManager.GetHitStopDuration(feedbackStrength)));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration)
    {
        if (_sequence != null) _sequence.Pause();
        _tasselDynamics?.SetMotionPaused(true);
        _afterimage?.SetPaused(true);
        yield return new WaitForSecondsRealtime(duration);
        _tasselDynamics?.SetMotionPaused(false);
        _afterimage?.SetPaused(false);
        // 蓄力保持期间不能被卡肉解冻
        if (!_chargeHoldActive && _sequence != null && _sequence.IsActive())
            _sequence.Play();
        _hitStopRoutine = null;
    }

    private Vector3 GetVisualTipPosition()
    {
        Vector3 visualPosition = _deformRoot != null ? _deformRoot.position : transform.position;
        float lengthScale = _deformRoot != null
            ? _deformRoot.localScale.y / Mathf.Max(_visualBaseLocalScale.y, 0.0001f)
            : 1f;
        visualPosition += _rayDirection * (_halfVisualLength * lengthScale);
        return visualPosition;
    }

    private void CheckHits()
    {
        if (!_hitsEnabled) return;

        var column = _columnManager?.GetColumn(_column);
        if (column == null) return;

        float tipDistance = Vector3.Dot(transform.position - _rayOrigin, _rayDirection);
        _hitCandidates.Clear();
        for (int i = 0; i < column.enemies.Count; i++)
        {
            var enemy = column.enemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || _hitEnemies.Contains(enemy))
                continue;
            bool isCombatBoss = enemy.isBoss && enemy.bossState == BossState.InCombat;
            if (enemy.rowIndex >= _rangeRows && !isCombatBoss)
                continue;
            if (enemy.isBoss && !isCombatBoss)
                continue;

            float enemyDistance = (enemy.rowIndex + 1) * _rayLength / _visualRangeRows;
            if (tipDistance < enemyDistance - HitDistanceTolerance)
                continue;

            _hitCandidates.Add(enemy);
        }

        if (_coveredBossTarget != null
            && _coveredBossTarget.state != EnemyState.Dead
            && !_hitEnemies.Contains(_coveredBossTarget))
        {
            float bossDistance = _rayLength;
            if (tipDistance >= bossDistance - HitDistanceTolerance)
                _hitCandidates.Add(_coveredBossTarget);
        }

        _hitCandidates.Sort((a, b) => a.rowIndex.CompareTo(b.rowIndex));
        for (int i = 0; i < _hitCandidates.Count; i++)
        {
            var enemy = _hitCandidates[i];
            if (enemy == null || enemy.state == EnemyState.Dead || _hitEnemies.Contains(enemy))
                continue;

            _hitEnemies.Add(enemy);
            bool diseaseStabHit = false;
            if (!_hitAny)
                diseaseStabHit = _onFirstHitBeforeDamage?.Invoke(enemy) == true;
            HitFeedbackStrength feedbackStrength = _hitAny ? HitFeedbackStrength.Light : _motion.firstHitStrength;
            // 命中震动：首排 = 1，第二排起按资产倍率；独立计时，不受卡肉暂停影响
            TriggerHitShake(_hitAny ? _motion.shakeSecondRowScale : 1f);
            Vector3 impactPosition = GetVisualTipPosition();
            enemy.TakeDamage(_damage, _damageType, feedbackStrength: feedbackStrength,
                triggerHitAnimation: !_suppressHitReaction,
                impactPosition: impactPosition, impactDirection: _rayDirection,
                diseaseStabHit: diseaseStabHit, interruptsCavalryCharge: _interruptsCavalryCharge,
                hitReactionDirection: _hitReactionDirection);
            if (!_hitAny)
                PauseSequenceForHitStop(feedbackStrength);
            if (!_hitAny)
            {
                _hitAny = true;
                TriggerHitPulse();
                _onFirstHit?.Invoke();
            }
            _onHit?.Invoke(enemy);
        }
    }
}