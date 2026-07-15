using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public sealed class StabSweepEffect : MonoBehaviour
{
    private const float ThrustDuration = 0.2f;
    private const float RetractDuration = 0.3f;
    private const float HitDistanceTolerance = 0.35f;
    private const int SortingOrderAboveEnemies = 10;

    private ColumnManager _columnManager;
    private int _column;
    private int _rangeRows;
    private int _visualRangeRows;
    private float _damage;
    private DamageType _damageType;
    private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> _hitCandidates = new List<Enemy>();
    private Action<Enemy> _onHit;
    private Action _onFirstHit;
    private Action _onComplete;
    private Vector3 _rayOrigin;
    private Vector3 _rayDirection;
    private float _rayLength;
    private Sequence _sequence;
    private bool _hitAny;

    public static void Create(GameObject prefab, Vector3 startPosition, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows,
        float damage, DamageType damageType, ColumnManager columnManager,
        Action<Enemy> onHit, Action onFirstHit, Action onComplete, float targetDuration = -1f)
    {
        var ray = new GameObject("StabRay");
        ray.transform.position = startPosition;

        var visual = Instantiate(prefab, ray.transform);
        visual.name = "StabVisual";
        visual.transform.localPosition = Vector3.zero;
        var spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            float visualLength = spriteRenderer.sprite.bounds.size.y * visual.transform.localScale.y;
            visual.transform.localPosition = Vector3.back * (visualLength * 0.5f);
        }

        ray.AddComponent<StabSweepEffect>().Initialize(visual, targetPosition, column, rangeRows, visualRangeRows, damage, damageType,
            columnManager, onHit, onFirstHit, onComplete, targetDuration);
    }

    private void Initialize(GameObject visual, Vector3 targetPosition, int column, int rangeRows, int visualRangeRows, float damage,
        DamageType damageType, ColumnManager columnManager, Action<Enemy> onHit, Action onFirstHit,
        Action onComplete, float targetDuration)
    {
        _column = column;
        _rangeRows = rangeRows;
        _visualRangeRows = visualRangeRows;
        _damage = damage;
        _damageType = damageType;
        _columnManager = columnManager;
        _onHit = onHit;
        _onFirstHit = onFirstHit;
        _onComplete = onComplete;

        var renderer = visual.GetComponentInChildren<SpriteRenderer>();
        if (renderer != null)
            renderer.sortingOrder = SortingOrderAboveEnemies;

        _rayOrigin = transform.position;
        _rayDirection = (targetPosition - _rayOrigin).normalized;
        _rayLength = Vector3.Distance(_rayOrigin, targetPosition);
        transform.rotation = Quaternion.LookRotation(_rayDirection, Vector3.up);
        float durationScale = targetDuration > 0f ? targetDuration / (ThrustDuration + RetractDuration) : 1f;
        _sequence = DOTween.Sequence().SetTarget(transform);
        _sequence.Append(transform.DOMove(targetPosition, ThrustDuration * durationScale).SetEase(Ease.OutQuad).OnUpdate(CheckHits));
        _sequence.AppendCallback(CheckHits);
        _sequence.Append(transform.DOMove(_rayOrigin, RetractDuration * durationScale).SetEase(Ease.InQuad));
        _sequence.OnKill(() => Destroy(gameObject));
        _sequence.OnComplete(() =>
        {
            _onComplete?.Invoke();
            Destroy(gameObject);
        });
    }

    private void OnDestroy()
    {
        _sequence?.Kill();
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

        for (int i = 0; i < _hitCandidates.Count; i++)
        {
            var enemy = _hitCandidates[i];
            if (enemy == null || enemy.state == EnemyState.Dead || _hitEnemies.Contains(enemy))
                continue;

            _hitEnemies.Add(enemy);
            enemy.TakeDamage(_damage, _damageType);
            if (!_hitAny)
            {
                _hitAny = true;
                _onFirstHit?.Invoke();
            }
            _onHit?.Invoke(enemy);
        }
    }
}
