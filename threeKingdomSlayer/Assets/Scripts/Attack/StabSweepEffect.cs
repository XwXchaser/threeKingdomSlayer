using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class StabSweepEffect : MonoBehaviour
{
    private const float ThrustDuration = 0.2f;
    private const float RetractDuration = 0.3f;
    private const float WindupRatio = 0.12f;
    private const float ThrustRatio = 0.28f;
    private const float PenetrationRatio = 0.08f;
    private const float RetractRatio = 0.52f;
    private const float WindupDistance = 0.4f;
    private const float PenetrationDistance = 0.32f;
    // 蓄力：更明显压缩，长度0.92、宽度1.08
    private const float WindupLengthScale = 0.92f;
    private const float WindupWidthScale = 1.08f;
    // 高速刺出：更明显拉伸，长度1.18、宽度0.86
    private const float ThrustLengthScale = 1.18f;
    private const float ThrustWidthScale = 0.86f;
    // 首次命中：更明显压缩到长度0.90、宽度1.15，回弹至长度1.10、宽度0.92
    private const float HitCompressLength = 0.90f;
    private const float HitCompressWidth = 1.15f;
    private const float HitBounceLength = 1.10f;
    private const float HitBounceWidth = 0.92f;
    private const float SpeedFrameStartRatio = 0.1f;
    private const float SpeedFrameEndRatio = 1.0f;
    private const float HitDistanceTolerance = 0.35f;
    private const int SortingOrderWithEnemies = 0;

    private ColumnManager _columnManager;
    private int _column;
    private int _rangeRows;
    private int _visualRangeRows;
    private float _damage;
    private DamageType _damageType;
    private bool _interruptsCavalryCharge;
    private HitReactionDirection _hitReactionDirection = HitReactionDirection.None;
    private bool _suppressHitReaction;
    private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> _hitCandidates = new List<Enemy>();
    private StabMotionParams _motion = StabMotionParams.Default;
    private StabStartPose _startPose = StabStartPose.None;
    private bool _hitsEnabled;
    private Enemy _coveredBossTarget;
    private Action<Enemy> _onHit;
    private Func<Enemy, bool> _onFirstHitBeforeDamage;
    private Action _onFirstHit;
    private Action _onComplete;
    private Vector3 _rayOrigin;
    private Vector3 _rayDirection;
    private float _rayLength;
    private Vector3 _visualBaseLocalPosition;
    private Vector3 _visualBaseLocalScale;
    private Vector3 _visualTargetOffsetLocal;
    private Transform _visualTransform;
    private Transform _visualOffsetRoot;
    private Transform _deformRoot;
    private WeaponMotionBlurController _motionBlur;
    private Sequence _sequence;
    private Coroutine _hitStopRoutine;
    private bool _hitAny;
    private bool _usingSpeedSprite;
    private float _halfBaseSpriteLength;
    private Coroutine _hitDeformationRoutine;
    private ChargeStabVisual _chargeVisual;
    private SpriteRenderer _renderer;
    private Sprite _baseSprite;
    private bool _chargeHoldPending;
    private bool _chargeHoldActive;
    private bool _chargeHoldFull;
    private bool _chargeFrameApplied;
    private float _chargeHoldFromRatio;
    private float _chargeHoldRatio;
    private float _chargeHoldPull;
    private float _chargeHoldHeldSeconds;
    private float _chargeHoldShakePhase;

    /// <summary>震动专用节点：只承载命中震动偏移，避免与主序列的时间线抢同一 transform</summary>
    private Transform _shakeRoot;
    /// <summary>蓄力指向：来自 AttackSystem 的「当前朝向 → 指向列」偏摆增量（度）</summary>
    private float _chargeAimYawDelta;
    /// <summary>当前实际施加的偏摆角（度），用于回收段收回</summary>
    private float _chargeAppliedYaw;
    // 命中震动：独立 realtime 计时，不受卡肉（seq.Pause）影响
    private bool _shakeActive;
    private float _shakeStartRealtime;
    private float _shakeScale = 1f;
    private float _chargeAppliedTilt;
    private bool _chargeRecoverActive;
    private float _chargeRecoverTime;
    private float _chargeRecoverDuration;
    private Vector3 _chargeRecoverFromPosition;
    private Vector3 _chargeRecoverFromOffset;
    private float _chargeRecoverFromRoll;
    private float _retractStartTime;
    private float _retractDurationCached;
    private Quaternion _deformBaseRotation;
    private Vector3 _retractFromPosition;
    private Vector3 _retractToPosition;
    private Vector3 _retractFromScale;
    private Vector3 _retractToScale;
    private Vector3 _retractFromVisualOffset;

    public static StabSweepEffect Create(GameObject prefab, Sprite speedSprite, Vector3 startPosition, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows,
        float damage, DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Func<Enemy, bool> onFirstHitBeforeDamage, Action onFirstHit, Action onComplete,
        float visualReachOffset, float visualStartXOffset, float visualTargetRandomRadius, float baseRayLength,
        float targetDuration = -1f, bool interruptsCavalryCharge = false,
        HitReactionDirection hitReactionDirection = HitReactionDirection.None, bool suppressHitReaction = false,
        StabMotionParams motion = default, StabStartPose startPose = default,
        ChargeStabVisual chargeVisual = null)
    {
        var ray = new GameObject("StabRay");
        ray.transform.position = startPosition;
        Vector3 rayVector = targetPosition - startPosition;
        ray.transform.rotation = Quaternion.LookRotation(rayVector.normalized, Vector3.up);

        var visual = Instantiate(prefab, ray.transform);
        visual.name = "StabVisual";
        var spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            float visualLength = spriteRenderer.sprite.bounds.size.y * visual.transform.localScale.y;
            visual.transform.localPosition = Vector3.back * (visualLength * 0.5f - visualReachOffset);
        }
        visual.transform.position += Vector3.right * visualStartXOffset;

        if (motion.windupRatio <= 0f) motion = StabMotionParams.Default;
        var effect = ray.AddComponent<StabSweepEffect>();
        effect.Initialize(visual, speedSprite, targetPosition, column, rangeRows, visualRangeRows, damage, damageType,
            columnManager, coveredBossTarget, onHit, onFirstHitBeforeDamage, onFirstHit, onComplete, targetDuration,
            visualTargetRandomRadius, baseRayLength, interruptsCavalryCharge, hitReactionDirection,
            suppressHitReaction, motion, startPose, chargeVisual);
        return effect;
    }

    private void Initialize(GameObject visual, Sprite speedSprite, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows, float damage,
        DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Func<Enemy, bool> onFirstHitBeforeDamage, Action onFirstHit,
        Action onComplete, float targetDuration, float visualTargetRandomRadius, float baseRayLength,
        bool interruptsCavalryCharge = false, HitReactionDirection hitReactionDirection = HitReactionDirection.None,
        bool suppressHitReaction = false, StabMotionParams motion = default, StabStartPose startPose = default,
        ChargeStabVisual chargeVisual = null)
    {
        _column = column;
        _rangeRows = rangeRows;
        _visualRangeRows = visualRangeRows;
        _damage = damage;
        _damageType = damageType;
        _interruptsCavalryCharge = interruptsCavalryCharge;
        _hitReactionDirection = hitReactionDirection;
        _suppressHitReaction = suppressHitReaction;
        _motion = motion.windupRatio <= 0f ? StabMotionParams.Default : motion;
        _startPose = startPose;
        _columnManager = columnManager;
        _coveredBossTarget = coveredBossTarget;
        _onHit = onHit;
        _onFirstHitBeforeDamage = onFirstHitBeforeDamage;
        _onFirstHit = onFirstHit;
        _onComplete = onComplete;
        _chargeVisual = chargeVisual;

        var renderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = SortingOrderWithEnemies;

        _visualTransform = visual.transform;
        _visualBaseLocalPosition = _visualTransform.localPosition;
        _visualBaseLocalScale = _visualTransform.localScale;
        Quaternion visualBaseLocalRotation = _visualTransform.localRotation;
        Matrix4x4 visualWorldMatrix = _visualTransform.localToWorldMatrix;

        var sr2 = visual.GetComponentInChildren<SpriteRenderer>();
        _halfBaseSpriteLength = sr2 != null ? sr2.sprite.bounds.size.y * _visualBaseLocalScale.y * 0.5f : 0f;

        _deformRoot = new GameObject("DeformRoot").transform;
        _visualOffsetRoot = new GameObject("VisualOffsetRoot").transform;
        _visualOffsetRoot.SetParent(transform, false);
        _visualOffsetRoot.localPosition = Vector3.zero;
        _visualOffsetRoot.localRotation = Quaternion.identity;
        _visualOffsetRoot.localScale = Vector3.one;
        _shakeRoot = new GameObject("ShakeRoot").transform;
        _shakeRoot.SetParent(_visualOffsetRoot, false);
        _shakeRoot.localPosition = Vector3.zero;
        _shakeRoot.localRotation = Quaternion.identity;
        _shakeRoot.localScale = Vector3.one;
        _deformRoot.SetParent(_shakeRoot, false);
        _deformRoot.localPosition = _visualBaseLocalPosition;
        _deformRoot.localRotation = visualBaseLocalRotation;
        _deformRoot.localScale = _visualBaseLocalScale;
        _visualTransform.SetParent(_deformRoot, false);
        _visualTransform.localPosition = Vector3.zero;
        _visualTransform.localRotation = Quaternion.identity;
        _visualTransform.localScale = Vector3.one;

        if (!ApproximatelyEqual(visualWorldMatrix, _visualTransform.localToWorldMatrix))
            Debug.LogError("[StabSweepEffect] DeformRoot hierarchy migration changed the visual world transform");

        _motionBlur = renderer != null
            ? new WeaponMotionBlurController(renderer, 0.4f, 0.02f, 32f)
            : null;

        _rayOrigin = transform.position;
        _rayDirection = (targetPosition - _rayOrigin).normalized;
        _rayLength = Vector3.Distance(_rayOrigin, targetPosition);
        transform.rotation = Quaternion.LookRotation(_rayDirection, Vector3.up);
        _visualTargetOffsetLocal = CreateVisualTargetOffset(visualTargetRandomRadius, baseRayLength);

        // 连段交接：从上一段当前姿态起步（位置与朝向在起手阶段过渡到本段目标列）
        if (_startPose.valid)
        {
            transform.position = _startPose.worldPosition;
            transform.rotation = _startPose.worldRotation;
            _deformRoot.localScale = GetVisualScale(1f, Mathf.Max(0.1f, _startPose.lengthScale));
        }

        Quaternion deformBaseRotation = _deformRoot.localRotation;

        float totalDuration = targetDuration > 0f ? targetDuration : ThrustDuration + RetractDuration;
        float windupDuration = totalDuration * _motion.windupRatio;
        float thrustDuration = totalDuration * _motion.thrustRatio;
        float penetrationDuration = totalDuration * _motion.penetrationRatio;
        float holdDuration = Mathf.Max(0f, _motion.windupHoldSeconds);
        float retractDuration = Mathf.Max(0.02f,
            totalDuration - windupDuration - holdDuration - thrustDuration - penetrationDuration);
        Vector3 windupPosition = _rayOrigin - _rayDirection * WindupDistance;
        Vector3 penetrationPosition = targetPosition + _rayDirection * PenetrationDistance;
        Vector3 windupScale = GetVisualScale(WindupWidthScale, WindupLengthScale);
        Vector3 thrustScale = GetVisualScale(_motion.thrustWidthScale, _motion.thrustLengthScale);
        Sprite baseSprite = renderer != null ? renderer.sprite : null;
        _renderer = renderer;
        _baseSprite = baseSprite;
        _sequence = DOTween.Sequence().SetTarget(transform);
        Quaternion aimRotation = Quaternion.LookRotation(_rayDirection, Vector3.up);
        // 画面内姿态倾角：整段保持，作为持枪姿态语言（不改变轨迹）
        if (_motion.visualTiltDegrees != 0f)
            _visualOffsetRoot.localRotation = Quaternion.Euler(0f, 0f, _motion.visualTiltDegrees);
        _sequence.Append(transform.DOMove(windupPosition, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_deformRoot.DOScale(GetVisualScale(WindupWidthScale, WindupLengthScale), windupDuration).SetEase(Ease.OutQuad));
        // 交接时从上一段朝向转到本段目标朝向（转向发生在起手阶段，命中前完成）
        if (_startPose.valid)
        {
            float snapRatio = Mathf.Clamp01(_motion.redirectSnapRatio);
            if (snapRatio > 0f && windupDuration > 0.01f)
            {
                // 释放指向：先用起手段的一部分「甩」过去并过冲一点，再回正到目标朝向
                float snapDuration = Mathf.Max(0.01f, windupDuration * snapRatio);
                float remainDuration = Mathf.Max(0f, windupDuration - snapDuration);
                float startYaw = _startPose.worldRotation.eulerAngles.y;
                float travelSign = Mathf.Sign(Mathf.DeltaAngle(startYaw, aimRotation.eulerAngles.y));
                if (Mathf.Approximately(travelSign, 0f)) travelSign = 1f;
                Quaternion overshootRotation = aimRotation * Quaternion.Euler(0f, _motion.redirectOvershootDegrees * travelSign, 0f);

                _sequence.Join(transform.DORotateQuaternion(overshootRotation, snapDuration).SetEase(Ease.OutQuad));
                if (remainDuration > 0.005f)
                    _sequence.Insert(snapDuration,
                        transform.DORotateQuaternion(aimRotation, remainDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                _sequence.Join(transform.DORotateQuaternion(aimRotation, windupDuration).SetEase(Ease.OutQuad));
            }
        }
        if (holdDuration > 0f)
            _sequence.AppendInterval(holdDuration);

        // 命中只能在刺出阶段开始之后生效（交接时起始位置可能已经在敌人前方）
        _sequence.InsertCallback(windupDuration + holdDuration, () => _hitsEnabled = true);
        _sequence.Append(transform.DOMove(targetPosition, thrustDuration).SetEase(Ease.InCubic)
            .OnStart(() => _motionBlur?.SetStrength(_motion.blurThrust * _motion.motionBlurScale))
            .OnUpdate(CheckHits));
        // 绕枪身长轴自转：刺出段 0 → 设定值
        if (_motion.rollDegrees != 0f)
            _sequence.Join(DOTween.To(() => 0f, v => ApplyDeformRoll(deformBaseRotation, v), _motion.rollDegrees, thrustDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_visualOffsetRoot.DOLocalMove(_visualTargetOffsetLocal, thrustDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_deformRoot.DOScale(GetVisualScale(ThrustWidthScale, ThrustLengthScale), thrustDuration).SetEase(Ease.InCubic));
        if (renderer != null && baseSprite != null && speedSprite != null)
        {
            float speedFrameStart = windupDuration + holdDuration + thrustDuration * _motion.speedFrameStart01;
            float speedFrameEnd = windupDuration + holdDuration + thrustDuration * _motion.speedFrameEnd01;
            _sequence.InsertCallback(speedFrameStart, () =>
            {
                renderer.sprite = speedSprite;
                _usingSpeedSprite = true;
                _motionBlur?.SetStrength(_motion.blurSpeedFrame * _motion.motionBlurScale);
            });
            _sequence.InsertCallback(speedFrameEnd, () => RestoreBaseSprite(renderer, baseSprite));
        }
        _sequence.AppendCallback(CheckHits);
        _sequence.AppendCallback(() => _onComplete?.Invoke());
        _sequence.Append(transform.DOMove(penetrationPosition, penetrationDuration).SetEase(Ease.OutQuad)
            .OnStart(() => _motionBlur?.SetStrength(_motion.blurPenetration * _motion.motionBlurScale))
            .OnUpdate(CheckHits));
        _sequence.Join(_deformRoot.DOScale(_visualBaseLocalScale, penetrationDuration).SetEase(Ease.OutQuad));
        _sequence.Append(transform.DOMove(_rayOrigin, retractDuration).SetEase(Ease.OutCubic)
            .OnStart(() => _motionBlur?.SetStrength(0f)));
        // 自转在回收段转回 0，保证每段起步时枪身面朝一致
        if (_motion.rollDegrees != 0f)
            _sequence.Join(DOTween.To(() => _motion.rollDegrees, v => ApplyDeformRoll(deformBaseRotation, v), 0f, retractDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_visualOffsetRoot.DOLocalMove(Vector3.zero, retractDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_deformRoot.DOScale(_visualBaseLocalScale, retractDuration).SetEase(Ease.OutCubic));
        // 连段蓄力：按下后由本组件接管回收，把枪体「拉回蓄势位」，因此记录回收路径两端
        _retractStartTime = windupDuration + holdDuration + thrustDuration + penetrationDuration;
        _retractDurationCached = retractDuration;
        _retractFromPosition = penetrationPosition;
        _retractToPosition = _rayOrigin;
        _retractFromScale = _visualBaseLocalScale;   // 回收段起点缩放已是基准（穿入段已还原）
        _retractToScale = _visualBaseLocalScale;
        _retractFromVisualOffset = _visualTargetOffsetLocal;
        _deformBaseRotation = deformBaseRotation;
        _sequence.InsertCallback(_retractStartTime, TryHoldForCharge);
        _sequence.OnKill(() =>
        {
            _visualTransform?.DOKill();
            Destroy(gameObject);
        });
        _sequence.OnComplete(() =>
        {
            _visualTransform?.DOKill();
            Destroy(gameObject);
        });
    }

    /// <summary>绕枪身长轴的自转：在形变节点的本地 Y（也就是精灵长轴）上叠加角度</summary>
    private void ApplyDeformRoll(Quaternion baseRotation, float rollDegrees)
    {
        if (_deformRoot == null) return;
        _deformRoot.localRotation = baseRotation * Quaternion.Euler(0f, rollDegrees, 0f);
    }

    private void RestoreBaseSprite(SpriteRenderer renderer, Sprite baseSprite)
    {
        if (renderer == null) return;
        renderer.sprite = baseSprite;
        _usingSpeedSprite = false;
        _visualTransform.localPosition = Vector3.zero;
        _visualTransform.localScale = Vector3.one;
    }

    private Vector3 CreateVisualTargetOffset(float baseRadius, float baseRayLength)
    {
        if (baseRadius <= 0f || baseRayLength <= 0f || Camera.main == null)
            return Vector3.zero;

        float rangeScale = _rayLength / baseRayLength;
        float radius = baseRadius * Mathf.Sqrt(Mathf.Max(rangeScale, 0f));
        radius = Mathf.Clamp(radius, baseRadius * 0.5f, baseRadius * 1.5f);
        float angle = UnityEngine.Random.value * Mathf.PI * 2f;
        float distance = radius * Mathf.Sqrt(UnityEngine.Random.value);
        Vector3 offsetWorld = Camera.main.transform.right * (Mathf.Cos(angle) * distance)
            + Camera.main.transform.up * (Mathf.Sin(angle) * distance);
        return transform.InverseTransformVector(offsetWorld);
    }

    private static bool ApproximatelyEqual(Matrix4x4 a, Matrix4x4 b)
    {
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 4; column++)
                if (Mathf.Abs(a[row, column] - b[row, column]) > 0.0001f)
                    return false;
        return true;
    }

    private Vector3 GetVisualScale(float widthMultiplier, float lengthMultiplier)
    {
        return new Vector3(
            _visualBaseLocalScale.x * widthMultiplier,
            _visualBaseLocalScale.y * lengthMultiplier,
            _visualBaseLocalScale.z);
    }

    private void Update()
    {
        if (_chargeRecoverActive)
        {
            UpdateChargeRecover();
            return;
        }

        if (!_chargeHoldActive) return;

        float held = _chargeHoldHeldSeconds;
        float pullSeconds = _motion.chargeHoldPullSeconds;
        _chargeHoldPull = Mathf.Clamp01(held / pullSeconds);

        // 阶段一：拉到蓄势位；阶段二：到位后再向后一顿（表达开始蓄力），之后才开始前后微颤
        float target = Mathf.Clamp01(_motion.chargeHoldRetractRatio);
        float ratio = Mathf.Lerp(_chargeHoldFromRatio, target, _chargeHoldPull);
        float shake = 0f;
        if (held > pullSeconds)
        {
            float settleSeconds = _motion.chargeHoldSettleSeconds;
            float settle = settleSeconds > 0f
                ? Mathf.Clamp01((held - pullSeconds) / settleSeconds)
                : 1f;
            ratio = Mathf.Lerp(target, target + _motion.chargeHoldSettleRatio, settle);
            shake = _motion.chargeHoldShakeAmplitude * settle;
        }

        _chargeHoldRatio = Mathf.Clamp01(ratio);
        _chargeHoldShakePhase += Time.deltaTime * _motion.chargeHoldShakeFrequency * Mathf.PI * 2f;

        ApplyChargeHoldPose(_chargeHoldRatio, shake);
        ApplyChargeFrame();
    }

    private void LateUpdate()
    {
        UpdateHitShake();
        if (_deformRoot == null) return;
        float lengthDelta = _deformRoot.localScale.y - _visualBaseLocalScale.y;
        _deformRoot.localPosition = _visualBaseLocalPosition
            + Vector3.up * (_halfBaseSpriteLength * lengthDelta / Mathf.Max(_visualBaseLocalScale.y, 0.0001f));
    }

    /// <summary>指向偏摆增量（度）：由 AttackSystem 按「当前朝向 → 手指指向列」给出，实际角度被资产上限夹住</summary>
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
        _shakeStartRealtime = Time.realtimeSinceStartup;
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
        float t = Time.realtimeSinceStartup - _shakeStartRealtime;
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
        }
        if (_hitDeformationRoutine != null)
        {
            StopCoroutine(_hitDeformationRoutine);
            _hitDeformationRoutine = null;
        }
        if (_visualTransform != null)
            _visualTransform.DOKill();
        _motionBlur?.Dispose();
        _sequence?.Kill();
    }

    /// <summary>取当前姿态，交给下一段当做起点（连段交接）</summary>
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
        yield return new WaitForSecondsRealtime(duration);
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
        visualPosition += _rayDirection * (_halfBaseSpriteLength * lengthScale);
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
