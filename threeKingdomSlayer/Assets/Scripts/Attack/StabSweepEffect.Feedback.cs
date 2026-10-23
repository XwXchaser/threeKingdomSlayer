using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// StabSweepEffect 的**命中反馈**：命中抖动、脉冲、武器残影与销毁回收。
    ///
    /// 按机制从 <c>StabSweepEffect.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 StabSweepEffect.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class StabSweepEffect : MonoBehaviour
{
    public void SetChargeAimYawDelta(float degrees)
    {
        _chargeAimYawDelta = degrees;
    }

    /// <summary>
    /// 触发命中震动。scale：首排 = 1，第二排起用资产的第二排倍率。
    /// 用 realtime 独立计时 + 专用震动节点，避免被卡肉（seq.Pause）冻住，也不与主序列抢 transform。
    /// </summary>
    private void TriggerHitShake(float scale)
    {
        if (_shakeRoot == null) return;
        if (_motion.shakeDuration <= 0f) return;
        if (_motion.shakeAmplitude <= 0f && _motion.shakeLateral <= 0f
            && _motion.shakeRollDegrees <= 0f && _motion.shakePitchDegrees <= 0f) return;

        _shakeActive = true;
        _shakeStartRealtime = GameplayPause.UnscaledTime;
        _shakeScale = Mathf.Clamp01(scale);
    }

    private void UpdateHitShake()
    {
        if (_shakeRoot == null) return;

        if (!_shakeActive)
        {
            if (_shakeRoot.localPosition != Vector3.zero) _shakeRoot.localPosition = Vector3.zero;
            if (_shakeRoot.localRotation != Quaternion.identity) _shakeRoot.localRotation = Quaternion.identity;
            return;
        }

        float duration = Mathf.Max(0.0001f, _motion.shakeDuration);
        float t = GameplayPause.UnscaledTime - _shakeStartRealtime;
        if (t >= duration)
        {
            _shakeActive = false;
            _shakeRoot.localPosition = Vector3.zero;
            _shakeRoot.localRotation = Quaternion.identity;
            return;
        }

        float decay = 1f - (t / duration);
        decay *= decay;                                   // 二次衰减：先重后轻
        float w = Mathf.PI * 2f * Mathf.Max(1f, _motion.shakeFrequency);
        float axial = Mathf.Sin(t * w) * decay * _shakeScale * _motion.shakeAmplitude;
        float lateral = Mathf.Sin(t * w * 1.37f) * decay * _shakeScale * _motion.shakeLateral;
        float vertical = Mathf.Sin(t * w * 0.83f) * decay * _shakeScale * _motion.shakeLateral * 0.6f;
        float roll = Mathf.Sin(t * w * 1.11f) * decay * _shakeScale * _motion.shakeRollDegrees;
        float pitch = Mathf.Sin(t * w * 1.23f) * decay * _shakeScale * _motion.shakePitchDegrees;

        // 本地 Z = 枪轴（沿射线前伸）；X = 水平垂直；绕 Z 的旋转 = 长轴滚转
        _shakeRoot.localPosition = new Vector3(lateral, vertical, axial);
        _shakeRoot.localRotation = Quaternion.Euler(pitch, 0f, roll);
    }

    private void TriggerHitPulse()
    {
        if (_hitDeformationRoutine != null)
            StopCoroutine(_hitDeformationRoutine);
        _hitDeformationRoutine = StartCoroutine(HitPulseRoutine());
    }

    private System.Collections.IEnumerator HitPulseRoutine()
    {
        _visualTransform.DOKill();
        _visualTransform.DOScale(new Vector3(HitCompressWidth, HitCompressLength, 1f), 0.02f)
            .SetTarget(_visualTransform).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(0.02f);
        _visualTransform.DOScale(new Vector3(HitBounceWidth, HitBounceLength, 1f), 0.05f)
            .SetTarget(_visualTransform).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.05f);
        _visualTransform.DOScale(Vector3.one, 0.06f)
            .SetTarget(_visualTransform).SetEase(Ease.OutQuad);
        _hitDeformationRoutine = null;
    }

    private void UpdateMotionBlur(float multiplier)
    {
        if (_motionBlur == null)
            return;
        _motionBlur.UpdateMotion(_visualTransform.position, _visualTransform.eulerAngles.z,
            _rayDirection, multiplier, 18f, Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_hitStopRoutine != null)
        {
            StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = null;
            _tasselDynamics?.SetMotionPaused(false);
        }
        if (_hitDeformationRoutine != null)
        {
            StopCoroutine(_hitDeformationRoutine);
            _hitDeformationRoutine = null;
        }
        // 用原始引用 Kill：目标已销毁时 DOKill 会因 Unity 假 null 直接跳过，Tween 会继续访问已销毁对象
        DOTween.Kill(_visualTransform, false);
        DOTween.Kill(transform, false);
        _motionBlur?.Dispose();
        _afterimage?.Clear();
        _sequence?.Kill();
    }

    /// <summary>取当前姿态，交给下一段当做起点（连段交接）</summary>
}