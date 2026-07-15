using System.Collections.Generic;
using UnityEngine;

public class CycloneItemController : MonoBehaviour
{
    public static CycloneItemController Instance { get; private set; }

    [SerializeField] private GameObject cycloneEffectPrefab;

    private readonly List<Enemy> _targetSnapshot = new List<Enemy>();

    private bool _isActive;
    private float _remainingDuration;
    private float _tickTimer;
    private float _cooldownRemaining;
    private float _cooldownDuration;
    private CycloneItemConfig _config;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (_cooldownRemaining > 0f)
            _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - Time.deltaTime);

        if (!_isActive) return;

        _remainingDuration -= Time.deltaTime;
        _tickTimer -= Time.deltaTime;

        if (_tickTimer <= 0f && _remainingDuration > 0f)
        {
            ExecuteTick();
            _tickTimer = Mathf.Max(0.01f, _config.intervalSeconds);
        }

        if (_remainingDuration <= 0f)
            StopEffect();
    }

    public bool TryActivate(UpgradeDefinition definition)
    {
        if (definition == null || definition.effectType != "item_cyclone" || cycloneEffectPrefab == null || _cooldownRemaining > 0f)
            return false;

        CycloneItemConfig config = definition.cycloneItemConfig;
        if (config.durationSeconds <= 0f || config.intervalSeconds <= 0f || config.rowCount <= 0)
            return false;

        _config = config;
        _cooldownDuration = Mathf.Max(0f, config.cooldownSeconds);
        _cooldownRemaining = _cooldownDuration;
        _remainingDuration = config.durationSeconds;
        _tickTimer = 0f;
        _isActive = true;
        ExecuteTick();
        _tickTimer = config.intervalSeconds;
        return true;
    }

    public float CooldownRemaining => _cooldownRemaining;
    public float CooldownDuration => _cooldownDuration;
    public bool IsOnCooldown => _cooldownRemaining > 0f;

    public float CooldownFill
    {
        get
        {
            if (_cooldownDuration <= 0f) return 0f;
            return 1f - Mathf.Clamp01(_cooldownRemaining / _cooldownDuration);
        }
    }

    public void ResetAll()
    {
        _cooldownRemaining = 0f;
        _cooldownDuration = 0f;
        StopEffect();
    }

    private void ExecuteTick()
    {
        var columnManager = AttackSystem.Instance?.columnManager;
        if (columnManager == null) return;

        var enemies = columnManager.GetAllEnemiesInRange(_config.rowCount);
        _targetSnapshot.Clear();
        for (int i = 0; i < enemies.Count; i++)
            _targetSnapshot.Add(enemies[i]);

        for (int i = 0; i < _targetSnapshot.Count; i++)
        {
            Enemy enemy = _targetSnapshot[i];
            if (!IsValidTarget(enemy))
                continue;

            var instance = Instantiate(cycloneEffectPrefab);
            var effect = instance.GetComponent<CycloneEffect>();
            if (effect == null)
            {
                Destroy(instance);
                continue;
            }

            effect.Setup(
                enemy,
                Mathf.Max(0, _config.initialDamage),
                Mathf.Max(0f, _config.landingDamagePercent),
                enemy.launchDuration);
        }
    }

    private static bool IsValidTarget(Enemy enemy)
    {
        if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.state == EnemyState.Dead)
            return false;
        if (enemy.state == EnemyState.QTEAttacking || enemy.isPhaseTransitioning)
            return false;

        return enemy.state == EnemyState.Launched || enemy.CanBeLaunched(float.MaxValue);
    }

    private void StopEffect()
    {
        _isActive = false;
        _remainingDuration = 0f;
        _tickTimer = 0f;
        _targetSnapshot.Clear();
    }
}
