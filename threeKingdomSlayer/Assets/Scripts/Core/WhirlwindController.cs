using UnityEngine;

/// <summary>
/// 大旋风（画圈道具）状态机
///
/// InputManager 检测到画圈手势后激活；手指离开后停用。
/// 激活期间：
///   - 每 (1/floatValue) 秒对 rangeRows 内敌人造成一次伤害
///   - 每累积 360° 击飞范围内所有敌人
///   - 伤害 = baseAttackConfig.damage × UpgradeEffectManager.GetDamageMultiplier()
/// </summary>
public class WhirlwindController : MonoBehaviour
{
    public static WhirlwindController Instance { get; private set; }

    [Header("画圈参数")]
    [Tooltip("圈累积阈值（度），达到后激活")]
    public float detectionAngle = 270f;
    [Tooltip("圈有效最小半径（像素）")]
    public float minRadius = 100f;
    [Tooltip("圈有效最大半径（像素，0=自动取屏幕高×0.45）")]
    public float maxRadius;
    [Tooltip("反向超过此角度（度）则重置累积")]
    public float directionLockReverseThreshold = 30f;

    public bool IsActive { get; private set; }

    // 圈检测状态
    private float _accumulatedAngle;
    private float _lastAngle;
    private bool _directionLocked;
    private int _circleDirection; // 1=顺时针, -1=逆时针
    private bool _circleTriggered;

    // 大旋风攻击状态
    private UpgradeDefinition _activeDef;
    private float _tickInterval;
    private float _tickTimer;
    private float _angleSinceLastLaunch;
    private float _lastActiveAngle;

    private AttackSystem _attackSystem;
    private Vector2 _screenCenter;

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
        if (maxRadius <= 0f)
            maxRadius = Screen.height * 0.45f;
        _screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void Update()
    {
        if (!IsActive) return;

        _tickTimer -= Time.deltaTime;
        if (_tickTimer <= 0f)
        {
            _tickTimer += _tickInterval;
            ExecuteTickDamage();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ── 公开 API（InputManager 调用）──

    /// <summary>每帧传入手指位置，返回是否触发了画圈</summary>
    public bool UpdateCircleDetection(Vector2 fingerPos)
    {
        if (_circleTriggered) return true;

        Vector2 offset = fingerPos - _screenCenter;
        float dist = offset.magnitude;
        if (dist < minRadius || dist > maxRadius)
            return false;

        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

        if (!_directionLocked)
        {
            // 首次采样：仅记录，不累积
            _lastAngle = angle;
            _directionLocked = true;
            return false;
        }

        float delta = Mathf.DeltaAngle(_lastAngle, angle);
        _lastAngle = angle;

        if (_circleDirection == 0 && Mathf.Abs(delta) > 1f)
            _circleDirection = delta > 0 ? 1 : -1;

        // 方向反转检测
        if (_circleDirection != 0 && delta * _circleDirection < -directionLockReverseThreshold)
        {
            ResetCircleDetection();
            return false;
        }

        _accumulatedAngle += delta;

        if (Mathf.Abs(_accumulatedAngle) >= detectionAngle)
        {
            _circleTriggered = true;
            Debug.Log($"[WhirlwindController] 画圈触发 accumulatedAngle={_accumulatedAngle:F0}°");
        }

        return _circleTriggered;
    }

    /// <summary>激活大旋风（InputManager 在触发后调用）</summary>
    public void Activate(UpgradeDefinition def)
    {
        _activeDef = def;
        _tickInterval = def.floatValue > 0f ? 1f / def.floatValue : 0.5f;
        _tickTimer = 0f; // 立即造成第一跳伤害
        _angleSinceLastLaunch = 0f;
        _lastActiveAngle = Mathf.Atan2(0f, 0f) * Mathf.Rad2Deg; // 将在首帧修正
        IsActive = true;
        Debug.Log($"[WhirlwindController] 激活 tickInterval={_tickInterval:F3}s rangeRows={def.baseAttackConfig?.rangeRows ?? 1}");
    }

    /// <summary>手指离开时停用</summary>
    public void Deactivate()
    {
        IsActive = false;
        _activeDef = null;
        _circleTriggered = false;
        ResetCircleDetection();
        Debug.Log("[WhirlwindController] 停用");
    }

    /// <summary>大旋风激活中每帧调用，自动追踪角度累积并判定击飞</summary>
    public void TickActive(Vector2 fingerPos)
    {
        if (!IsActive) return;

        Vector2 offset = fingerPos - _screenCenter;
        float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(_lastActiveAngle, angle);
        _lastActiveAngle = angle;

        _angleSinceLastLaunch += delta;
        if (Mathf.Abs(_angleSinceLastLaunch) >= 360f)
        {
            _angleSinceLastLaunch -= 360f * Mathf.Sign(_angleSinceLastLaunch);
            ExecuteLaunch();
        }
    }

    /// <summary>重置圈检测（手指离开/打断时）</summary>
    public void ResetCircleDetection()
    {
        _accumulatedAngle = 0f;
        _lastAngle = 0f;
        _directionLocked = false;
        _circleDirection = 0;
        _circleTriggered = false;
    }

    /// <summary>是否有画圈型道具可用</summary>
    public bool CanCircle => ItemInventory.Instance != null && ItemInventory.Instance.HasItem("circle");

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
