using System.Collections.Generic;
using UnityEngine;

public sealed class CycloneZone : MonoBehaviour
{

    /// <summary>
    /// 关卡/阵型信息（只读）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageFormation _stageFormation;

    private IStageFormation StageFormation => _stageFormation ??= StageController.Instance;
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

        int maxRow = StageFormation != null ? StageFormation.GetMaxVisibleRows() - 1 : 4;
        float rowSpacing = StageFormation != null ? StageFormation.GetRowSpacing() : 2.5f;
        float offsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
        float x = StageFormation != null
            ? StageFormation.GetFormationOffset(2, _rowIndex)
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
            enemy.ApplyActiveDisplacementHit(suppressHitReactionAnimation: true);
            enemy.TakeActiveDisplacementPoiseDamage(_bossPoiseDamagePercent);
            return;
        }

        enemy.ApplyActiveDisplacementHit(suppressHitReactionAnimation: true);
        if (!enemy.CanBeLaunched(float.MaxValue))
            return;

        if (_damage > 0)
            enemy.TakeDamage(_damage, DamageType.Sweep, feedbackSource: HitFeedbackSource.ActiveSkill,
                feedbackStrength: HitFeedbackStrength.Heavy, suppressHitReactionAnimation: true);
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
                feedbackStrength: HitFeedbackStrength.Heavy, suppressHitReactionAnimation: true);
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
