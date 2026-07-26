using System.Collections.Generic;
using UnityEngine;

public class ActiveSkillRunner : MonoBehaviour
{
    public static ActiveSkillRunner Instance
    {
        get
        {
            if (_instance == null)
                _instance = Object.FindObjectOfType<ActiveSkillRunner>();
            return _instance;
        }
        private set => _instance = value;
    }
    private static ActiveSkillRunner _instance;

    [Header("特效预制体（未配置时回退使用 TimedPassiveModule）")]
    [SerializeField] private GameObject _fireEffectPrefab;
    [SerializeField] private GameObject _arrowEffectPrefab;
    [SerializeField] private GameObject _cycloneEffectPrefab;

    private readonly List<Enemy> _cycloneCandidates = new List<Enemy>();
    private readonly Dictionary<string, int> _chargeAttackShockwaveLayers = new Dictionary<string, int>();

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

    public bool TryActivate(ActiveSkillDefinition definition, int level)
    {
        if (definition == null || level <= 0) return false;

        switch (definition.activeEffectType)
        {
            case ActiveSkillEffectType.FireAoe:
                return ActivateFire(definition, level);
            case ActiveSkillEffectType.FireLine:
                return ActivateFireLine(definition, level);
            case ActiveSkillEffectType.ArrowRain:
                return ActivateArrow(definition, level);
            case ActiveSkillEffectType.Cyclone:
                return ActivateCyclone(definition, level);
            case ActiveSkillEffectType.ChargeAttackShockwave:
                return ActivateChargeAttackShockwave(definition, level);
            default:
                return false;
        }
    }

    private bool ActivateFire(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedAoeLevels == null || level > definition.timedAoeLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFire failed: timedAoeLevels null or level {level} > count {definition.timedAoeLevels?.Count}");
            return false;
        }
        var cfg = definition.timedAoeLevels[level - 1];
        var prefab = _fireEffectPrefab != null ? _fireEffectPrefab : TimedPassiveModule.Instance?.fireEffectPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[ActiveSkillRunner] ActivateFire failed: fireEffectPrefab is null");
            return false;
        }
        if (cfg.columns == null || cfg.columns.Count == 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFire failed: columns null or empty (level={level})");
            return false;
        }

        Debug.Log($"[ActiveSkillRunner] ActivateFire: spawning effect, columns=[{string.Join(",", cfg.columns)}], damage={cfg.damage}");
        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        if (effect == null)
        {
            Debug.LogError("[ActiveSkillRunner] ActivateFire failed: ShootFireEffect component missing on prefab");
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.columns, cfg.damage);
        return true;
    }

    private bool ActivateFireLine(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedAoeLevels == null || level > definition.timedAoeLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFireLine failed: timedAoeLevels null or level {level} > count {definition.timedAoeLevels?.Count}");
            return false;
        }
        var cfg = definition.timedAoeLevels[level - 1];
        var prefab = _fireEffectPrefab != null ? _fireEffectPrefab : TimedPassiveModule.Instance?.fireEffectPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[ActiveSkillRunner] ActivateFireLine failed: fireEffectPrefab is null");
            return false;
        }
        if (cfg.columns == null || cfg.columns.Count == 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateFireLine failed: columns null or empty (level={level})");
            return false;
        }

        Debug.Log($"[ActiveSkillRunner] ActivateFireLine: spawning effect, columns=[{string.Join(",", cfg.columns)}], damage={cfg.damage}, critIfBurning");
        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        if (effect == null)
        {
            Debug.LogError("[ActiveSkillRunner] ActivateFireLine failed: ShootFireEffect component missing on prefab");
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.columns, cfg.damage, -1, 0, 0f, true);
        return true;
    }

    private bool ActivateArrow(ActiveSkillDefinition definition, int level)
    {
        if (definition.timedArrowLevels == null || level > definition.timedArrowLevels.Count)
            return false;
        var cfg = definition.timedArrowLevels[level - 1];
        var prefab = _arrowEffectPrefab != null ? _arrowEffectPrefab : TimedPassiveModule.Instance?.arrowEffectPrefab;
        if (prefab == null) return false;

        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<TimedArrowEffect>();
        if (effect == null)
        {
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.rowCount, cfg.arrowCount, cfg.damage);
        return true;
    }

    private bool ActivateCyclone(ActiveSkillDefinition definition, int level)
    {
        if (definition.cycloneLevels == null || level > definition.cycloneLevels.Count)
            return false;
        var cfg = definition.cycloneLevels[level - 1];
        var prefab = _cycloneEffectPrefab != null ? _cycloneEffectPrefab : TimedPassiveModule.Instance?.cycloneEffectPrefab;
        var columnManager = AttackSystem.Instance?.columnManager;
        if (prefab == null || columnManager == null) return false;

        _cycloneCandidates.Clear();
        var enemies = columnManager.GetAllEnemies();
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.state == EnemyState.QTEAttacking || enemy.isPhaseTransitioning)
                continue;
            if (enemy.state == EnemyState.Launched || enemy.CanBeLaunched(float.MaxValue))
                _cycloneCandidates.Add(enemy);
        }
        if (_cycloneCandidates.Count == 0) return false;

        int pickCount = Mathf.Min(Mathf.Max(1, cfg.enemyCount), _cycloneCandidates.Count);
        for (int i = 0; i < pickCount; i++)
        {
            int randomIndex = Random.Range(i, _cycloneCandidates.Count);
            var selected = _cycloneCandidates[randomIndex];
            _cycloneCandidates[randomIndex] = _cycloneCandidates[i];
            _cycloneCandidates[i] = selected;

            var instance = Instantiate(prefab);
            var effect = instance.GetComponent<CycloneEffect>();
            if (effect == null)
            {
                Destroy(instance);
                continue;
            }
            effect.Setup(selected, Mathf.Max(0, cfg.damage), Mathf.Max(0f, cfg.landingDamagePercent), cfg.knockupDuration);
        }
        return true;
    }

    private bool ActivateChargeAttackShockwave(ActiveSkillDefinition definition, int level)
    {
        if (definition.chargeAttackShockwaveLevels == null || level > definition.chargeAttackShockwaveLevels.Count)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateChargeAttackShockwave failed: levels missing for Lv.{level}");
            return false;
        }

        var cfg = definition.chargeAttackShockwaveLevels[level - 1];
        if (cfg.rangeRows <= 0 || cfg.damage < 0)
        {
            Debug.LogWarning($"[ActiveSkillRunner] ActivateChargeAttackShockwave failed: invalid rows={cfg.rangeRows} damage={cfg.damage}");
            return false;
        }

        if (!_chargeAttackShockwaveLayers.ContainsKey(definition.upgradeId))
            _chargeAttackShockwaveLayers[definition.upgradeId] = 0;
        _chargeAttackShockwaveLayers[definition.upgradeId]++;
        Debug.Log($"[ActiveSkillRunner] 主动冲击波充能+1: {definition.displayName} 当前 {_chargeAttackShockwaveLayers[definition.upgradeId]} 层");
        return true;
    }

    public List<ActiveChargeAttackShockwaveConsumeResult> ConsumeAllChargeAttackShockwaves()
    {
        var results = new List<ActiveChargeAttackShockwaveConsumeResult>();
        foreach (var pair in _chargeAttackShockwaveLayers)
        {
            if (pair.Value <= 0) continue;
            var entry = ActiveSkillInventory.Instance?.GetEntry(pair.Key);
            if (entry == null || entry.definition == null || entry.definition.chargeAttackShockwaveLevels == null || entry.level > entry.definition.chargeAttackShockwaveLevels.Count)
                continue;

            results.Add(new ActiveChargeAttackShockwaveConsumeResult
            {
                upgradeId = pair.Key,
                layers = pair.Value,
                config = entry.definition.chargeAttackShockwaveLevels[entry.level - 1]
            });
        }

        for (int i = 0; i < results.Count; i++)
            _chargeAttackShockwaveLayers[results[i].upgradeId] = 0;
        return results;
    }

    public void ResetAll()
    {
        _chargeAttackShockwaveLayers.Clear();
    }
}

public struct ActiveChargeAttackShockwaveConsumeResult
{
    public string upgradeId;
    public int layers;
    public ChargeAttackShockwaveLevelConfig config;
}

