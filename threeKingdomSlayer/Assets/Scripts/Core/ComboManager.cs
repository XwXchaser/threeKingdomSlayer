using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 连击管理器 - 单例
/// 监听敌人受击事件，积累连击数，达到阈值时通过 BuffManager 触发限时 Buff。
/// </summary>
public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance { get; private set; }

    [Header("配置")]
    public ComboBuffConfig config;

    /// <summary>当前连击数（只读）</summary>
    public int CurrentCombo => _currentCombo;
    public bool IsFrozen => _isFrozen;

    /// <summary>连击重置进度 1→0（1=刚命中, 0=即将归零）</summary>
    public float ComboResetProgress
    {
        get
        {
            if (config == null || _currentCombo <= 0) return 0f;
            float now = _isFrozen ? _freezeStartedAt : Time.time;
            float elapsed = now - _lastHitTime;
            return Mathf.Clamp01(1f - elapsed / GetEffectiveResetDelay());
        }
    }

    // 运行时状态
    private int _currentCombo;
    private float _lastHitTime;
    private int _lastHitFrame;
    private bool _isFrozen;
    private float _freezeStartedAt;
    private HashSet<int> _hitEnemiesThisFrame = new HashSet<int>();
    private HashSet<int> _triggeredThresholds = new HashSet<int>();
    private HashSet<string> _activeComboBuffIds = new HashSet<string>();

    // 事件
    public System.Action<int> OnComboUpdated;
    public System.Action<string> OnComboTrigger;

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
        var em = EnemyManager.Instance;
        if (em != null)
        {
            // 已存在的敌人
            foreach (var enemy in em.GetAllAliveEnemies())
                SubscribeEnemy(enemy);
            // 未来新敌人
            em.OnEnemyRegistered += SubscribeEnemy;
        }
    }

    private void OnDestroy()
    {
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnEnemyRegistered -= SubscribeEnemy;

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (_isFrozen || config == null || _currentCombo <= 0) return;

        if (Time.time - _lastHitTime >= GetEffectiveResetDelay())
        {
            ResetCombo();
        }
    }

    /// <summary>按当前连击层级计算有效重置窗口</summary>
    private float GetEffectiveResetDelay()
    {
        if (config == null) return 3f;
        float baseDelay = config.resetDelay;
        if (_currentCombo < 10) return baseDelay;
        if (_currentCombo < 20) return 2.5f;
        if (_currentCombo < 30) return 2.0f;
        if (_currentCombo < 40) return 1.7f;
        return 1.5f;
    }

    /// <summary>为敌人注册受击回调</summary>
    public void SubscribeEnemy(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.OnDamageTaken += OnEnemyDamaged;
    }

    /// <summary>重置连击数（关卡开始、断连时调用）</summary>
    public void ResetCombo()
    {
        if (BuffManager.Instance != null)
        {
            foreach (var buffId in _activeComboBuffIds)
                BuffManager.Instance.RemoveBuff(buffId);
        }
        _activeComboBuffIds.Clear();
        _currentCombo = 0;
        _triggeredThresholds.Clear();
        OnComboUpdated?.Invoke(0);
    }

    public void Freeze()
    {
        if (_isFrozen) return;
        _isFrozen = true;
        _freezeStartedAt = Time.time;
    }

    public void Resume()
    {
        if (!_isFrozen) return;
        _lastHitTime += Time.time - _freezeStartedAt;
        _isFrozen = false;
    }

    private void OnEnemyDamaged(Enemy enemy)
    {
        if (_isFrozen || config == null) return;

        int increment;
        if (config.hitIncrementMode == HitIncrementMode.PerEnemy)
        {
            // 每帧同敌人只计一次
            if (Time.frameCount != _lastHitFrame)
            {
                _lastHitFrame = Time.frameCount;
                _hitEnemiesThisFrame.Clear();
            }
            int id = enemy.GetInstanceID();
            if (!_hitEnemiesThisFrame.Add(id)) return;
            increment = 1;
        }
        else
        {
            increment = 1;
        }

        _lastHitTime = Time.time;
        _currentCombo += increment;
        OnComboUpdated?.Invoke(_currentCombo);

        CheckTriggers();
    }

    private void CheckTriggers()
    {
        var triggers = config.triggers;
        if (triggers == null) return;

        // triggers 按阈值升序排列，从高到低检查是否触发
        for (int i = 0; i < triggers.Count; i++)
        {
            var t = triggers[i];
            if (_currentCombo >= t.comboThreshold && _triggeredThresholds.Add(t.comboThreshold))
            {
                TriggerBuff(t);
            }
        }
    }

    private void TriggerBuff(ComboBuffTrigger trigger)
    {
        if (BuffManager.Instance != null)
        {
            BuffManager.Instance.AddBuff(trigger.buffId, 0f, trigger.modifier);
            _activeComboBuffIds.Add(trigger.buffId);
        }
        OnComboTrigger?.Invoke(trigger.buffId);
        Debug.Log($"[ComboManager] 连击 {trigger.comboThreshold} 触发 Buff: {trigger.buffId}");
    }
}
