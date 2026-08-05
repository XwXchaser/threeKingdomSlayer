using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class StabSweepEffect : MonoBehaviour
{
    private const float ThrustDuration = 0.2f;
    private const float RetractDuration = 0.3f;
    private const float WindupRatio = 0.12f;
    private const float ThrustRatio = 0.32f;
    private const float PenetrationRatio = 0.08f;
    private const float RetractRatio = 0.48f;
    private const float WindupDistance = 0.25f;
    private const float PenetrationDistance = 0.2f;
    // 蓄力：轻微压缩，长度0.96、宽度1.04
    private const float WindupLengthScale = 0.96f;
    private const float WindupWidthScale = 1.04f;
    // 高速刺出：拉伸，长度1.10、宽度0.92
    private const float ThrustLengthScale = 1.10f;
    private const float ThrustWidthScale = 0.92f;
    // 首次命中：瞬间压缩到长度0.97、宽度1.07，回弹至长度1.04、宽度0.97
    private const float HitCompressLength = 0.97f;
    private const float HitCompressWidth = 1.07f;
    private const float HitBounceLength = 1.04f;
    private const float HitBounceWidth = 0.97f;
    private const float SpeedFrameStartRatio = 0.34f;
    private const float SpeedFrameEndRatio = 0.82f;
    private const float HitDistanceTolerance = 0.35f;
    private const int SortingOrderWithEnemies = 0;

    private ColumnManager _columnManager;
    private int _column;
    private int _rangeRows;
    private int _visualRangeRows;
    private float _damage;
    private DamageType _damageType;
    private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> _hitCandidates = new List<Enemy>();
    private Enemy _coveredBossTarget;
    private Action<Enemy> _onHit;
    private Action<Enemy> _onFirstHitBeforeDamage;
    private Action _onFirstHit;
    private Action _onComplete;
    private Vector3 _rayOrigin;
    private Vector3 _rayDirection;
    private float _rayLength;
    private Vector3 _visualBaseLocalPosition;
    private Vector3 _visualBaseLocalScale;
    private Transform _visualTransform;
    private Transform _deformRoot;
    private WeaponMotionBlurController _motionBlur;
    private Sequence _sequence;
    private Coroutine _hitStopRoutine;
    private bool _hitAny;
    private bool _usingSpeedSprite;
    private float _halfBaseSpriteLength;
    private Coroutine _hitDeformationRoutine;

    public static void Create(GameObject prefab, Sprite speedSprite, Vector3 startPosition, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows,
        float damage, DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget,
        Action<Enemy> onHit, Action<Enemy> onFirstHitBeforeDamage, Action onFirstHit, Action onComplete, float visualReachOffset, float visualStartXOffset, float targetDuration = -1f)
    {
        var ray = new GameObject("StabRay");
        ray.transform.position = startPosition;
        ray.transform.rotation = Quaternion.LookRotation((targetPosition - startPosition).normalized, Vector3.up);

        var visual = Instantiate(prefab, ray.transform);
        visual.name = "StabVisual";
        var spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            float visualLength = spriteRenderer.sprite.bounds.size.y * visual.transform.localScale.y;
            visual.transform.localPosition = Vector3.back * (visualLength * 0.5f - visualReachOffset);
        }
        visual.transform.position += Vector3.right * visualStartXOffset;

        ray.AddComponent<StabSweepEffect>().Initialize(visual, speedSprite, targetPosition, column, rangeRows, visualRangeRows, damage, damageType,
            columnManager, coveredBossTarget, onHit, onFirstHitBeforeDamage, onFirstHit, onComplete, targetDuration);
    }

    private void Initialize(GameObject visual, Sprite speedSprite, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows, float damage,
        DamageType damageType, ColumnManager columnManager, Enemy coveredBossTarget, Action<Enemy> onHit, Action<Enemy> onFirstHitBeforeDamage, Action onFirstHit,
        Action onComplete, float targetDuration)
    {
        _column = column;
        _rangeRows = rangeRows;
        _visualRangeRows = visualRangeRows;
        _damage = damage;
        _damageType = damageType;
        _columnManager = columnManager;
        _coveredBossTarget = coveredBossTarget;
        _onHit = onHit;
        _onFirstHitBeforeDamage = onFirstHitBeforeDamage;
        _onFirstHit = onFirstHit;
        _onComplete = onComplete;

        var renderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = SortingOrderWithEnemies;

        _motionBlur = renderer != null
            ? new WeaponMotionBlurController(renderer, 0.4f, 0.02f, 32f)
            : null;

        _visualTransform = visual.transform;
        _visualBaseLocalPosition = _visualTransform.localPosition;
        _visualBaseLocalScale = _visualTransform.localScale;

        var sr2 = visual.GetComponentInChildren<SpriteRenderer>();
        _halfBaseSpriteLength = sr2 != null ? sr2.sprite.bounds.size.y * _visualBaseLocalScale.y * 0.5f : 0f;

        _deformRoot = new GameObject("DeformRoot").transform;
        _deformRoot.SetParent(transform, false);
        _visualTransform.SetParent(_deformRoot, false);
        _visualTransform.localPosition = _visualBaseLocalPosition;
        _visualTransform.localScale = Vector3.one;

        _rayOrigin = transform.position;
        _rayDirection = (targetPosition - _rayOrigin).normalized;
        _rayLength = Vector3.Distance(_rayOrigin, targetPosition);
        transform.rotation = Quaternion.LookRotation(_rayDirection, Vector3.up);

        float totalDuration = targetDuration > 0f ? targetDuration : ThrustDuration + RetractDuration;
        float windupDuration = totalDuration * WindupRatio;
        float thrustDuration = totalDuration * ThrustRatio;
        float penetrationDuration = totalDuration * PenetrationRatio;
        float retractDuration = totalDuration * RetractRatio;
        Vector3 windupPosition = _rayOrigin - _rayDirection * WindupDistance;
        Vector3 penetrationPosition = targetPosition + _rayDirection * PenetrationDistance;
        Vector3 windupScale = GetVisualScale(WindupWidthScale, WindupLengthScale);
        Vector3 thrustScale = GetVisualScale(ThrustWidthScale, ThrustLengthScale);
        Sprite baseSprite = renderer != null ? renderer.sprite : null;
        _sequence = DOTween.Sequence().SetTarget(transform);
        _sequence.Append(transform.DOMove(windupPosition, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_deformRoot.DOScale(windupScale, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Append(transform.DOMove(targetPosition, thrustDuration).SetEase(Ease.InCubic)
            .OnStart(() => _motionBlur?.SetStrength(28f))
            .OnUpdate(CheckHits));
        _sequence.Join(_deformRoot.DOScale(thrustScale, thrustDuration).SetEase(Ease.InCubic));
        if (renderer != null && baseSprite != null && speedSprite != null)
        {
            float speedFrameStart = windupDuration + thrustDuration * SpeedFrameStartRatio;
            float speedFrameEnd = windupDuration + thrustDuration * SpeedFrameEndRatio;
            _sequence.InsertCallback(speedFrameStart, () =>
            {
                renderer.sprite = speedSprite;
                _usingSpeedSprite = true;
                _motionBlur?.SetStrength(14f);
            });
            _sequence.InsertCallback(speedFrameEnd, () => RestoreBaseSprite(renderer, baseSprite));
        }
        _sequence.AppendCallback(CheckHits);
        _sequence.AppendCallback(() => _onComplete?.Invoke());
        _sequence.Append(transform.DOMove(penetrationPosition, penetrationDuration).SetEase(Ease.OutQuad)
            .OnStart(() => _motionBlur?.SetStrength(18f))
            .OnUpdate(CheckHits));
        _sequence.Join(_deformRoot.DOScale(_visualBaseLocalScale, penetrationDuration).SetEase(Ease.OutQuad));
        _sequence.Append(transform.DOMove(_rayOrigin, retractDuration).SetEase(Ease.OutCubic)
            .OnStart(() => _motionBlur?.SetStrength(0f)));
        _sequence.Join(_deformRoot.DOScale(_visualBaseLocalScale, retractDuration).SetEase(Ease.OutCubic));
        _sequence.OnKill(() => Destroy(gameObject));
        _sequence.OnComplete(() => Destroy(gameObject));
    }

    private void RestoreBaseSprite(SpriteRenderer renderer, Sprite baseSprite)
    {
        if (renderer == null) return;
        renderer.sprite = baseSprite;
        _usingSpeedSprite = false;
        _visualTransform.localPosition = _visualBaseLocalPosition;
        _visualTransform.localScale = Vector3.one;
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
        float scaleDelta = _deformRoot.localScale.y - 1f;
        _visualTransform.localPosition = _visualBaseLocalPosition
            + Vector3.forward * _halfBaseSpriteLength * scaleDelta;
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
        _visualTransform.DOScale(new Vector3(HitCompressWidth, HitCompressLength, 1f), 0.02f).SetEase(Ease.OutQuad);
        yield return new WaitForSeconds(0.02f);
        _visualTransform.DOScale(new Vector3(HitBounceWidth, HitBounceLength, 1f), 0.05f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(0.05f);
        _visualTransform.DOScale(Vector3.one, 0.06f).SetEase(Ease.OutQuad);
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
        _motionBlur?.Dispose();
        _sequence?.Kill();
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

    private void CheckHits()
    {
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
            if (!_hitAny)
                _onFirstHitBeforeDamage?.Invoke(enemy);
            HitFeedbackStrength feedbackStrength = _hitAny ? HitFeedbackStrength.Light : HitFeedbackStrength.Standard;
            Vector3 impactPosition = transform.position + Vector3.up * 0.8f;
            enemy.TakeDamage(_damage, _damageType, feedbackStrength: feedbackStrength,
                impactPosition: impactPosition, impactDirection: _rayDirection);
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
