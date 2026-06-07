using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 定时被动模块 — 单例
///
/// 管理 effectType=passive_timed_aoe 的等级与计时器。
/// 计时器到期时实例化 ShootFireEffect（视觉+伤害一体化），而非自行结算伤害。
/// </summary>
public class TimedPassiveModule : MonoBehaviour
{
    public static TimedPassiveModule Instance { get; private set; }

    [Header("火焰特效")]
    [Tooltip("ShootFireEffect 预制体")]
    public GameObject fireEffectPrefab;

    private class TimedState
    {
        public UpgradeDefinition definition;
        public float timer;
        public TimedAoeLevelConfig config;
    }

    private Dictionary<string, TimedState> _states = new Dictionary<string, TimedState>();

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

    /// <summary>注册/更新定时被动（由 UpgradeEffectManager 调用）</summary>
    public void Register(UpgradeDefinition def, int level)
    {
        if (def.timedAoeLevels == null || level > def.timedAoeLevels.Count)
        {
            Debug.LogWarning($"[TimedPassiveModule] {def.upgradeId} 缺少 Lv.{level} 的 timedAoeLevels 配置");
            return;
        }

        var cfg = def.timedAoeLevels[level - 1];

        if (_states.TryGetValue(def.upgradeId, out var existing))
        {
            existing.config = cfg;
            existing.definition = def;
            // 升级不重置计时器：剩余时间不变，下一轮才用新间隔
        }
        else
        {
            _states[def.upgradeId] = new TimedState
            {
                definition = def,
                config = cfg,
                timer = cfg.intervalSeconds
            };
            Debug.Log($"[TimedPassiveModule] 注册 {def.displayName} Lv.{level} interval={cfg.intervalSeconds}s damage={cfg.damage} columns=[{string.Join(",", cfg.columns)}]");
        }
    }

    public void Unregister(string upgradeId)
    {
        _states.Remove(upgradeId);
    }

    public void ResetAll()
    {
        _states.Clear();
    }

    // ── 冷却显示 API ──

    /// <summary>获取所有已注册的计时被动 upgradeId（供 UI 遍历）</summary>
    public IEnumerable<string> RegisteredUpgradeIds
    {
        get
        {
            foreach (var kv in _states)
                yield return kv.Key;
        }
    }

    /// <summary>获取当前剩余时间（秒），-1=未注册</summary>
    public float GetTimer(string upgradeId)
    {
        return _states.TryGetValue(upgradeId, out var s) ? s.timer : -1f;
    }

    /// <summary>获取总间隔时间（秒），-1=未注册</summary>
    public float GetInterval(string upgradeId)
    {
        return _states.TryGetValue(upgradeId, out var s) ? s.config.intervalSeconds : -1f;
    }

    private void Update()
    {
        foreach (var kv in _states)
        {
            var state = kv.Value;
            state.timer -= Time.deltaTime;
            if (state.timer <= 0f)
            {
                state.timer = state.config.intervalSeconds;
                SpawnFire(state.config.columns, state.config.damage);
            }
        }
    }

    private void SpawnFire(List<int> columns, int damage)
    {
        if (columns == null || columns.Count == 0 || fireEffectPrefab == null) return;

        var instance = Instantiate(fireEffectPrefab);
        instance.GetComponent<ShootFireEffect>().Play(columns, damage);
    }
}
