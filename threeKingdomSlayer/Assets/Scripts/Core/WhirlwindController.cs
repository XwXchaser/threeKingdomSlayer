using UnityEngine;

/// <summary>
/// 大旋风（道具）状态机 — 点击 BuffIcon 激活，自动运转。
/// 激活期间：
///   - 每 (1/floatValue) 秒对 rangeRows 内敌人造成一次伤害
///   - 每累积 360° 击飞范围内所有敌人
///   - autoDuration 秒后自动停用
///   - 伤害 = baseAttackConfig.damage × UpgradeEffectManager.GetDamageMultiplier()
/// </summary>
public class WhirlwindController : MonoBehaviour
{
    public static WhirlwindController Instance { get; private set; }

    [Header("自动运转")]
    [Tooltip("激活后持续秒数")]
    public float autoDuration = 5f;
    [Tooltip("每360°触发击飞的转速（度/秒），0=不击飞")]
    public float autoSpinSpeed = 180f;

    public bool IsActive { get; private set; }

    private UpgradeDefinition _activeDef;
    private float _tickInterval;
    private float _tickTimer;
    private float _autoTimer;
    private float _angleSinceLastLaunch;

    private AttackSystem _attackSystem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        _attackSystem = AttackSystem.Instance;
    }

    private void Update()
    {
        if (!IsActive) return;

        // 自动倒计时
        _autoTimer -= Time.deltaTime;
        if (_autoTimer <= 0f)
        {
            Deactivate();
            return;
        }

        // 持续伤害
        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer += _tickInterval;
            ExecuteTickDamage();
        }

        // 自动旋转 → 击飞
        if (autoSpinSpeed > 0f)
        {
            _angleSinceLastLaunch += autoSpinSpeed * Time.deltaTime;
            if (_angleSinceLastLaunch >= 360f)
            {
                _angleSinceLastLaunch -= 360f;
                ExecuteLaunch();
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── 公开 API ──

    /// <summary>点击 BuffIcon 激活，自动运转 autoDuration 秒后停用</summary>
    public void Activate(UpgradeDefinition def)
    {
        _activeDef = def;
        _tickInterval = def.floatValue > 0f ? 1f / def.floatValue : 0.5f;
        _tickTimer = 0f;
        _autoTimer = autoDuration;
        _angleSinceLastLaunch = 0f;
        IsActive = true;
        Debug.Log($"[WhirlwindController] 激活 tickInterval={_tickInterval:F3}s duration={autoDuration}s");
    }

    /// <summary>停用</summary>
    public void Deactivate()
    {
        IsActive = false;
        _activeDef = null;
        Debug.Log("[WhirlwindController] 停用");
    }

    // ── 内部 ──

    private void ExecuteTickDamage()
    {
        if (_activeDef == null || _activeDef.baseAttackConfig == null || _attackSystem == null) return;

        var cfg = _activeDef.baseAttackConfig;
        float dmgMult = UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f;
        float dmg = cfg.damage * dmgMult;

        var enemies = _attackSystem.columnManager?.GetAllEnemiesInRange(cfg.rangeRows);
        if (enemies == null || enemies.Count == 0) return;

        // 通过 AttackSystem 的 AttackWave 造成伤害（保证统一伤害管道）
        Vector3 wavePos = Vector3.zero;
        if (enemies.Count > 0)
            wavePos = enemies[0].transform.position;

        AttackWave.Create(wavePos, cfg.damageType, dmg, enemies, prefab: cfg.attackWavePrefab);
    }

    private void ExecuteLaunch()
    {
        if (_activeDef == null || _activeDef.baseAttackConfig == null || _attackSystem == null) return;

        var cfg = _activeDef.baseAttackConfig;
        var enemies = _attackSystem.columnManager?.GetAllEnemiesInRange(cfg.rangeRows);
        if (enemies == null || enemies.Count == 0) return;

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.state != EnemyState.Dead)
                enemy.Launch();
        }

        Debug.Log($"[WhirlwindController] 击飞 {enemies.Count} 个敌人");
    }
}
