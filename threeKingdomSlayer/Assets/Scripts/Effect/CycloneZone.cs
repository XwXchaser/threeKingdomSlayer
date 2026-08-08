using System.Collections.Generic;
using UnityEngine;

public sealed class CycloneZone : MonoBehaviour
{
    [SerializeField] private float zoneScaleMultiplier = 2.2f;

    private readonly HashSet<Enemy> _triggeredEnemies = new HashSet<Enemy>();
    private readonly HashSet<Enemy> _pendingLandingEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> _pendingLandingWorkList = new List<Enemy>();
    private ColumnManager _columnManager;
    private int _rowIndex;
    private int _damage;
    private int _landingDamage;
    private float _bossPoiseDamagePercent;
    private float _knockupDuration;
    private float _remainingDuration;
    private bool _initialized;
    private bool _zoneEnded;
    private float _fadeWaitRemaining;

    public void Setup(ColumnManager columnManager, int rowIndex, int damage,
        int landingDamage, float bossPoiseDamagePercent, float knockupDuration, float zoneDuration)
    {
        _columnManager = columnManager;
        _rowIndex = rowIndex;
        _damage = damage;
        _landingDamage = landingDamage;
        _bossPoiseDamagePercent = bossPoiseDamagePercent;
        _knockupDuration = knockupDuration;
        _remainingDuration = Mathf.Max(0.01f, zoneDuration);
        _triggeredEnemies.Clear();
        _pendingLandingEnemies.Clear();
        _zoneEnded = false;
        _fadeWaitRemaining = 0f;
        _initialized = true;

        PositionAtRow();
        transform.localScale *= zoneScaleMultiplier;
        GetComponent<CycloneEffect>()?.PlayZoneVisual(_remainingDuration);
        ScanRow();
    }

    private void Update()
    {
        CleanupPendingLandingEnemies();

        if (_zoneEnded)
        {
            _fadeWaitRemaining -= Time.deltaTime;
            TryFinish();
            return;
        }

        if (!_initialized)
            return;

        _remainingDuration -= Time.deltaTime;
        if (_remainingDuration <= 0f)
        {
            EndZone();
            return;
        }

        PositionAtRow();
        ScanRow();
    }

    private void EndZone()
    {
        _initialized = false;
        _zoneEnded = true;
        _fadeWaitRemaining = 0.25f;
        GetComponent<CycloneEffect>()?.StopZoneVisual();
        TryFinish();
    }

    private void TryFinish()
    {
        if (_pendingLandingEnemies.Count > 0)
            return;

        if (_fadeWaitRemaining > 0f)
            return;

        var visual = GetComponent<CycloneEffect>();
        if (visual == null || visual.ZoneVisualFadeCompleted)
            Destroy(gameObject);
    }

    private void PositionAtRow()
    {
        Transform parent = EnemyPool.Instance != null
            ? (EnemyPool.Instance.enemiesRoot != null ? EnemyPool.Instance.enemiesRoot : EnemyPool.Instance.poolRoot)
            : null;
        if (parent != null && transform.parent != parent)
            transform.SetParent(parent, false);

        int maxRow = StageController.Instance != null ? StageController.Instance.GetMaxVisibleRows() - 1 : 4;
        float rowSpacing = StageController.Instance != null ? StageController.Instance.GetRowSpacing() : 2.5f;
        float offsetZ = StageController.Instance != null ? StageController.Instance.GetFormationOffsetZ() : 0f;
        float x = StageController.Instance != null
            ? StageController.Instance.GetFormationOffset(2, _rowIndex)
            : 0f;
        float z = (maxRow - _rowIndex) * (-rowSpacing) + offsetZ;
        transform.localPosition = new Vector3(x, 0f, z);
    }

    private void ScanRow()
    {
        if (_columnManager == null)
            return;

        for (int columnIndex = 0; columnIndex < _columnManager.columnCount; columnIndex++)
        {
            var enemies = _columnManager.GetEnemiesInColumn(columnIndex);
            if (enemies == null)
                continue;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (enemy == null || enemy.rowIndex != _rowIndex || _triggeredEnemies.Contains(enemy))
                    continue;
                if (!CanTrigger(enemy))
                    continue;

                _triggeredEnemies.Add(enemy);
                TriggerEnemy(enemy);
            }
        }
    }

    private static bool CanTrigger(Enemy enemy)
    {
        if (enemy.state == EnemyState.Dead || enemy.state == EnemyState.QTEAttacking || enemy.isPhaseTransitioning)
            return false;
        if (enemy.isBoss && enemy.bossState != BossState.InCombat)
            return false;
        return true;
    }

    private void TriggerEnemy(Enemy enemy)
    {
        if (enemy.isBoss && enemy.state != EnemyState.Stunned)
        {
            enemy.ApplyActiveDisplacementHit();
            enemy.TakeActiveDisplacementPoiseDamage(_bossPoiseDamagePercent);
            return;
        }

        enemy.ApplyActiveDisplacementHit();
        if (!enemy.CanBeLaunched(float.MaxValue))
            return;

        if (_damage > 0)
            enemy.TakeDamage(_damage, DamageType.Sweep, feedbackSource: HitFeedbackSource.ActiveSkill,
                feedbackStrength: HitFeedbackStrength.Heavy);
        enemy.Launch(_knockupDuration);

        if (_landingDamage > 0)
        {
            _pendingLandingEnemies.Add(enemy);
            enemy.OnLaunchedLanded += OnEnemyLanded;
        }
    }

    private void OnEnemyLanded(Enemy enemy)
    {
        if (!_pendingLandingEnemies.Remove(enemy))
            return;

        enemy.OnLaunchedLanded -= OnEnemyLanded;
        if (enemy.state != EnemyState.Dead)
            enemy.TakeDamage(_landingDamage, DamageType.Sweep, feedbackSource: HitFeedbackSource.ActiveSkill,
                feedbackStrength: HitFeedbackStrength.Heavy);
        TryFinish();
    }

    private void CleanupPendingLandingEnemies()
    {
        if (_pendingLandingEnemies.Count == 0)
            return;

        _pendingLandingWorkList.Clear();
        foreach (var enemy in _pendingLandingEnemies)
        {
            if (enemy == null || enemy.state == EnemyState.Dead)
                _pendingLandingWorkList.Add(enemy);
        }

        for (int i = 0; i < _pendingLandingWorkList.Count; i++)
        {
            var enemy = _pendingLandingWorkList[i];
            if (enemy != null)
                enemy.OnLaunchedLanded -= OnEnemyLanded;
            _pendingLandingEnemies.Remove(enemy);
        }
    }

    private void OnDestroy()
    {
        foreach (var enemy in _pendingLandingEnemies)
        {
            if (enemy != null)
                enemy.OnLaunchedLanded -= OnEnemyLanded;
        }
    }
}
