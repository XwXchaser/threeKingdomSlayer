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
    private const float WindupLengthScale = 0.94f;
    private const float WindupWidthScale = 1.06f;
    private const float ThrustLengthScale = 1.1f;
    private const float ThrustWidthScale = 0.9f;
    private const float ImpactLengthScale = 1.16f;
    private const float ImpactWidthScale = 0.84f;
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
    private Sequence _sequence;
    private Coroutine _hitStopRoutine;
    private bool _hitAny;

    public static void Create(GameObject prefab, Vector3 startPosition, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows,
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

        ray.AddComponent<StabSweepEffect>().Initialize(visual, targetPosition, column, rangeRows, visualRangeRows, damage, damageType,
            columnManager, coveredBossTarget, onHit, onFirstHitBeforeDamage, onFirstHit, onComplete, targetDuration);
    }

    private void Initialize(GameObject visual, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows, float damage,
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

        _visualTransform = visual.transform;
        _visualBaseLocalPosition = _visualTransform.localPosition;
        _visualBaseLocalScale = _visualTransform.localScale;

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
        Vector3 impactScale = GetVisualScale(ImpactWidthScale, ImpactLengthScale);

        _sequence = DOTween.Sequence().SetTarget(transform);
        _sequence.Append(transform.DOMove(windupPosition, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Join(_visualTransform.DOScale(windupScale, windupDuration).SetEase(Ease.OutQuad));
        _sequence.Append(transform.DOMove(targetPosition, thrustDuration).SetEase(Ease.InCubic).OnUpdate(CheckHits));
        _sequence.Join(_visualTransform.DOScale(thrustScale, thrustDuration).SetEase(Ease.InCubic));
        _sequence.AppendCallback(CheckHits);
        _sequence.AppendCallback(() => _onComplete?.Invoke());
        _sequence.Append(transform.DOMove(penetrationPosition, penetrationDuration).SetEase(Ease.OutQuad).OnUpdate(CheckHits));
        _sequence.Join(_visualTransform.DOScale(impactScale, penetrationDuration).SetEase(Ease.OutQuad));
        _sequence.Append(transform.DOMove(_rayOrigin, retractDuration).SetEase(Ease.OutCubic));
        _sequence.Join(_visualTransform.DOScale(_visualBaseLocalScale, retractDuration).SetEase(Ease.OutCubic));
        _sequence.OnKill(() => Destroy(gameObject));
        _sequence.OnComplete(() => Destroy(gameObject));
    }

    private Vector3 GetVisualScale(float widthMultiplier, float lengthMultiplier)
    {
        return new Vector3(
            _visualBaseLocalScale.x * widthMultiplier,
            _visualBaseLocalScale.y * lengthMultiplier,
            _visualBaseLocalScale.z);
    }

    private void OnDestroy()
    {
        if (_hitStopRoutine != null)
        {
            StopCoroutine(_hitStopRoutine);
            _hitStopRoutine = null;
        }
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
            enemy.TakeDamage(_damage, _damageType, feedbackStrength: feedbackStrength);
            if (!_hitAny)
                PauseSequenceForHitStop(feedbackStrength);
            if (!_hitAny)
            {
                _hitAny = true;
                _onFirstHit?.Invoke();
            }
            _onHit?.Invoke(enemy);
        }
    }
}
