using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// StabSweepEffect 的**蓄力段**：蓄力保持、蓄力姿态与蓄力帧的接管/归还。
    ///
    /// 按机制从 <c>StabSweepEffect.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 StabSweepEffect.cs **同类型**，行为完全一致。
    /// </summary>

public sealed partial class StabSweepEffect : MonoBehaviour
{
    public StabStartPose CapturePose()
    {
        float lengthScale = 1f;
        if (_deformRoot != null && _visualBaseLocalScale.y > 1e-4f)
            lengthScale = _deformRoot.localScale.y / _visualBaseLocalScale.y;

        return new StabStartPose
        {
            valid = true,
            worldPosition = transform.position,
            worldRotation = transform.rotation,
            lengthScale = lengthScale
        };
    }

    /// <summary>
    /// 连段交接：立刻隐藏本段视觉并关闭命中，避免两把枪重叠。
    /// 本体时间线继续跑完并在完成时自行销毁，因此不做 Kill/Destroy（那会连带影响同帧新建的下一段）。
    /// </summary>
    public void HandOff()
    {
        _hitsEnabled = false;
        // 本段的残影不交给下一段继承，否则交接瞬间会留下上一枪的鬼影。
        _afterimage?.Clear();
        if (_visualTransform != null)
            _visualTransform.gameObject.SetActive(false);
    }

    /// <summary>蓄力保持中（枪体已被拉回蓄势位）</summary>
    public bool IsChargeHeld => _chargeHoldActive;

    /// <summary>蓄力素材帧来源：只借用帧做「姿势档位」，不搬蓄力视觉的进出场/跟手/帧闪</summary>
    public void SetChargeVisualSource(ChargeStabVisual chargeVisual)
    {
        _chargeVisual = chargeVisual;
    }

    /// <summary>
    /// 连段蓄力：按下后接管枪体的回收，把它沿回收路径拉回蓄势位（位移即蓄力条）。
    /// 若按下时还没进回收段，则等回收段开始再接（保证第三击已经捅出去）。
    /// </summary>
    public void BeginChargeHold()
    {
        if (_sequence == null || _chargeHoldActive || !_sequence.IsActive()) return;

        float elapsed = _sequence.Elapsed();
        if (elapsed < _retractStartTime)
        {
            _chargeHoldPending = true;
            return;
        }

        StartChargePullback(RatioAt(elapsed));
    }

    /// <summary>按住时长（秒），驱动拉回进度</summary>
    public void SetChargeHoldHeldSeconds(float seconds)
    {
        _chargeHoldHeldSeconds = Mathf.Max(0f, seconds);
    }

    /// <summary>结束蓄力保持：没有蓄力招式接手时，由本组件把回收段剩下的部分走完（不 seek 时间线，避免跳位）</summary>
    public void EndChargeHold()
    {
        _chargeHoldPending = false;
        if (!_chargeHoldActive) return;

        _chargeHoldActive = false;
        _chargeHoldFull = false;
        _chargeFrameApplied = false;
        if (_renderer != null && _baseSprite != null) _renderer.sprite = _baseSprite;

        _chargeRecoverActive = true;
        _chargeRecoverTime = 0f;
        _chargeRecoverDuration = _retractDurationCached * (1f - TimeRatioAt(_chargeHoldRatio));
        _chargeRecoverFromPosition = transform.position;
        _chargeRecoverFromOffset = _visualOffsetRoot != null ? _visualOffsetRoot.localPosition : Vector3.zero;
        _chargeRecoverFromRoll = _motion.rollDegrees * (1f - _chargeHoldRatio);

        if (_chargeRecoverDuration <= 0.01f) FinishChargeRecover();
    }

    /// <summary>续完回收：从蓄势位平滑走到枪尾位并把仰角/自转收回，然后自行销毁</summary>
    private void UpdateChargeRecover()
    {
        _chargeRecoverTime += Time.deltaTime;
        float p = _chargeRecoverDuration > 0f
            ? Mathf.Clamp01(_chargeRecoverTime / _chargeRecoverDuration)
            : 1f;
        float e = 1f - Mathf.Pow(1f - p, 3f);   // 与回收段同一缓动

        transform.position = Vector3.Lerp(_chargeRecoverFromPosition, _retractToPosition, e);
        if (_visualOffsetRoot != null)
        {
            _visualOffsetRoot.localPosition = Vector3.Lerp(_chargeRecoverFromOffset, Vector3.zero, e);
            _visualOffsetRoot.localRotation = Quaternion.Euler(0f,
                Mathf.Lerp(_chargeAppliedYaw, 0f, e),
                Mathf.Lerp(_chargeAppliedTilt, _motion.visualTiltDegrees, e));
        }
        if (_motion.rollDegrees != 0f)
            ApplyDeformRoll(_deformBaseRotation, Mathf.Lerp(_chargeRecoverFromRoll, 0f, e));

        if (p >= 1f) FinishChargeRecover();
    }

    private void FinishChargeRecover()
    {
        _chargeRecoverActive = false;

        // 旧时间线已无意义（停在那里即可，但要清掉回调，避免它在对象销毁时重复 Destroy）
        Sequence stale = _sequence;
        _sequence = null;
        if (stale != null)
        {
            stale.OnKill((DG.Tweening.TweenCallback)null);
            stale.OnComplete((DG.Tweening.TweenCallback)null);
            stale.Kill(false);
        }

        if (_visualTransform != null) _visualTransform.DOKill();
        Destroy(gameObject);
    }

    /// <summary>
    /// 交出蓄势位的枪体姿态：枪体中心 / 世界旋转 / 世界缩放，供上挑原地起手（不产生位移）。
    /// </summary>
    public bool TryGetChargeHoldPose(out Vector3 center, out Quaternion rotation, out Vector3 scale)
    {
        center = default;
        rotation = default;
        scale = default;
        if (!_chargeHoldActive || _deformRoot == null) return false;

        center = _deformRoot.position;
        rotation = _deformRoot.rotation;
        scale = _deformRoot.lossyScale;
        return true;
    }

    /// <summary>
    /// 姿态已被下一招接手：隐藏本段视觉与命中，并让时间线继续跑完自行销毁
    /// （不能 Kill/Destroy，否则会连带影响同帧新建的下一段）。
    /// </summary>
    public GameObject TakeHeldMeshVisual()
    {
        if (!_chargeHoldActive || _visualTransform == null ||
            _visualTransform.GetComponent<MeshWeaponVisual>() == null) return null;
        GameObject visual = _visualTransform.gameObject;
        if (_hitDeformationRoutine != null)
        {
            StopCoroutine(_hitDeformationRoutine);
            _hitDeformationRoutine = null;
        }
        _visualTransform.DOKill();
        _visualTransform.SetParent(null, true);
        _visualTransform = null;
        _tasselDynamics?.SetMotionPaused(false);
        _tasselDynamics = null;
        ReleaseAfterChargeHold();
        return visual;
    }

    public void ReleaseAfterChargeHold()
    {
        _chargeHoldPending = false;
        _chargeHoldActive = false;
        _chargeHoldFull = false;
        _chargeFrameApplied = false;
        HandOff();
        if (_sequence != null && _sequence.IsActive()) _sequence.Play();
    }

    /// <summary>回收段内的时间比例 → 收回距离比例（OutCubic 效果）</summary>
    private float RatioAt(float elapsed)
    {
        if (_retractDurationCached <= 0f) return 0f;
        float t = Mathf.Clamp01((elapsed - _retractStartTime) / _retractDurationCached);
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    /// <summary>收回距离比例 → 回收段内的时间比例（OutCubic 的逆）</summary>
    private float TimeRatioAt(float distanceRatio)
    {
        return 1f - Mathf.Pow(1f - Mathf.Clamp01(distanceRatio), 1f / 3f);
    }

    private void TryHoldForCharge()
    {
        if (_chargeHoldPending) StartChargePullback(0f);
    }

    private void StartChargePullback(float fromRatio)
    {
        _chargeHoldPending = false;
        _chargeHoldActive = true;
        _chargeHoldFromRatio = Mathf.Clamp01(fromRatio);
        _chargeHoldRatio = _chargeHoldFromRatio;
        _chargeHoldPull = 0f;
        _chargeHoldHeldSeconds = 0f;
        _chargeHoldShakePhase = 0f;
        _chargeFrameApplied = false;

        // 保持期间不允许卡肉把自己解冻
        if (_hitStopRoutine != null)
        {
            StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = null;
            _tasselDynamics?.SetMotionPaused(false);
        }

        _motionBlur?.SetStrength(0f);
        _sequence.Pause();
        ApplyChargeHoldPose(_chargeHoldFromRatio, 0f);
    }

    /// <summary>按「拉回进度」把枪体摆到蓄势位：位移即蓄力条；到拉满后再向后一顿，随后叠加前后微颤</summary>
    private void ApplyChargeHoldPose(float ratio, float shake)
    {
        if (_deformRoot == null) return;

        float p = Mathf.Clamp01(ratio);
        Vector3 position = Vector3.Lerp(_retractFromPosition, _retractToPosition, p);
        _deformRoot.localScale = Vector3.Lerp(_retractFromScale, _retractToScale, p);
        _visualOffsetRoot.localPosition = Vector3.Lerp(_retractFromVisualOffset, Vector3.zero, p);

        float tilt = _motion.visualTiltDegrees + _motion.chargeHoldPitchDegrees * p;
        if (shake > 0f)
        {
            // 前后抖动：沿枪身长轴顶住（略带一点姿态微摆，避免机械感）
            float sin = Mathf.Sin(_chargeHoldShakePhase);
            position += _rayDirection * (sin * shake);
            tilt += sin * shake * 12f;
        }

        transform.position = position;
        // 指向偏摆：绕（近）世界竖直轴把枪身转向手指所指的列；上限来自招式资产
        float yawCap = Mathf.Abs(_motion.chargeHoldYawDegrees);
        float targetYaw = Mathf.Clamp(_chargeAimYawDelta, -yawCap, yawCap);
        // 指向偏摆平滑：目标列是按敌人投影量化出来的，直接切会一跳一跳
        float yawSmooth = _motion.chargeHoldYawSmoothSeconds;
        if (yawSmooth <= 0f)
            _chargeAppliedYaw = targetYaw;
        else
            _chargeAppliedYaw = Mathf.Lerp(_chargeAppliedYaw, targetYaw,
                1f - Mathf.Exp(-3f * Time.deltaTime / yawSmooth));
        _visualOffsetRoot.localRotation = Quaternion.Euler(0f, _chargeAppliedYaw, tilt);
        _chargeAppliedTilt = tilt;
        if (_motion.rollDegrees != 0f)
            ApplyDeformRoll(_deformBaseRotation, Mathf.Lerp(_motion.rollDegrees, 0f, p));
    }

    /// <summary>蓄力素材帧只做姿势档位：拉回中用一档，拉满切另一档（不做帧闪）</summary>
    private void ApplyChargeFrame()
    {
        if (_renderer == null || _chargeVisual == null) return;

        bool full = _chargeHoldPull >= 1f;
        if (_chargeFrameApplied && full == _chargeHoldFull) return;

        _chargeHoldFull = full;
        _chargeFrameApplied = true;
        Sprite frame = full ? _chargeVisual.chargeSprite1 : _chargeVisual.chargeSprite2;
        if (frame != null) _renderer.sprite = frame;
    }

    /// <summary>
    /// 被下一招接手 / 需要立刻清掉时调用：隐藏视觉与命中并立即销毁
    /// （用于避免与下一招的枪体同时存在；与 HandOff 不同，不等时间线跑完）。
    /// </summary>
}