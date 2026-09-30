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

    public static StabSweepEffect Create(GameObject prefab, Sprite speedSprite, Vector3 startPosition, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows,
        float damage, DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Func<Enemy, bool> onFirstHitBeforeDamage, Action onFirstHit, Action onComplete,
        float visualReachOffset, float visualStartXOffset, float visualTargetRandomRadius, float baseRayLength,
        float targetDuration = -1f, bool interruptsCavalryCharge = false,
        StabMotionParams motion = default, StabStartPose startPose = default)
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
            visualTargetRandomRadius, baseRayLength, interruptsCavalryCharge, motion, startPose);
        return effect;
    }

    private void Initialize(GameObject visual, Sprite speedSprite, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows, float damage,
        DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Func<Enemy, bool> onFirstHitBeforeDamage, Action onFirstHit,
        Action onComplete, float targetDuration, float visualTargetRandomRadius, float baseRayLength,
        bool interruptsCavalryCharge = false, StabMotionParams motion = default, StabStartPose startPose = default)
    {
        _column = column;
        _rangeRows = rangeRows;
        _visualRangeRows = visualRangeRows;
        _damage = damage;
        _damageType = damageType;
        _interruptsCavalryCharge = interruptsCavalryCharge;
        _motion = motion.windupRatio <= 0f ? StabMotionParams.Default : motion;
        _startPose = startPose;
        _columnManager = columnManager;
        _coveredBossTarget = coveredBossTarget;
        _onHit = onHit;
        _onFirstHitBeforeDamage = onFirstHitBeforeDamage;
        _onFirstHit = onFirstHit;
        _onComplete = onComplete;

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
        _deformRoot.SetParent(_visualOffsetRoot, false);
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
        _sequence = DOTween.Sequence().SetTarget(transform);
        Quaternion aimRotation = Quaternion.LookRotation(_rayDirection, Vector3.up);
        // 画面内姿态倾角：整段保持，作为持枪姿态语言（不改变轨迹）
        if (_motion.visualTiltDegrees != 0f)
            _visualOffsetRoot.localRotation = Quaternion.Euler(0f, 0f, _motion.visualTiltDegrees);
        _sequence.Append(transform.DOMove(windupPosition, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_deformRoot.DOScale(GetVisualScale(WindupWidthScale, WindupLengthScale), windupDuration).SetEase(Ease.OutQuad));
        // 交接时从上一段朝向转到本段目标朝向（转向发生在起手阶段，命中前完成）
        if (_startPose.valid)
            _sequence.Join(transform.DORotateQuaternion(aimRotation, windupDuration).SetEase(Ease.OutQuad));
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

    private void LateUpdate()
    {
        if (_deformRoot == null) return;
        float lengthDelta = _deformRoot.localScale.y - _visualBaseLocalScale.y;
        _deformRoot.localPosition = _visualBaseLocalPosition
            + Vector3.up * (_halfBaseSpriteLength * lengthDelta / Mathf.Max(_visualBaseLocalScale.y, 0.0001f));
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
        if (_sequence == null || !_sequence.IsActive()) return;
        if (_hitStopRoutine != null)
            StopCoroutine(_hitStopRoutine);
        _hitStopRoutine = StartCoroutine(HitStopRoutine(HitFeedbackManager.GetHitStopDuration(feedbackStrength)));
    }

    private System.Collections.IEnumerator HitStopRoutine(float duration)
    {
        _sequence.Pause();
        yield return new WaitForSecondsRealtime(duration);
        if (_sequence != null && _sequence.IsActive())
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
            Vector3 impactPosition = GetVisualTipPosition();
            enemy.TakeDamage(_damage, _damageType, feedbackStrength: feedbackStrength,
                impactPosition: impactPosition, impactDirection: _rayDirection,
                diseaseStabHit: diseaseStabHit, interruptsCavalryCharge: _interruptsCavalryCharge);
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
