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
    [SerializeField] private WaveManager _waveManager;

    private readonly Dictionary<string, int> _chargeAttackShockwaveLayers = new Dictionary<string, int>();
    private ActiveSkillDefinition _armedDiseaseDefinition;
    private int _armedDiseaseLevel;
    private int _armedDiseaseLayers;

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
        if (PlayerState.Instance == null || PlayerState.Instance.stageState != StageState.InProgress) return false;
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
            case ActiveSkillEffectType.Wave:
                return ActivateWave(definition, level);
            case ActiveSkillEffectType.Disease:
                return ActivateDisease(definition, level);
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

        Debug.Log($"[ActiveSkillRunner] ActivateFireLine: spawning effect, columns=[{string.Join(",", cfg.columns)}], damage={cfg.damage}, critIfBurning={level >= 3}");
        var instance = Instantiate(prefab);
        var effect = instance.GetComponent<ShootFireEffect>();
        if (effect == null)
        {
            Debug.LogError("[ActiveSkillRunner] ActivateFireLine failed: ShootFireEffect component missing on prefab");
            Destroy(instance);
            return false;
        }
        effect.Play(cfg.columns, cfg.damage, -1, 0, 0f, level >= 3);
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
        if (definition.waveLevels == null || level > definition.waveLevels.Count)
            return false;
        var cfg = definition.waveLevels[level - 1];
        var columnManager = AttackSystem.Instance?.columnManager;
        var prefab = _cycloneEffectPrefab != null ? _cycloneEffectPrefab : TimedPassiveModule.Instance?.cycloneEffectPrefab;
        if (columnManager == null || prefab == null || cfg.rangeRows <= 0)
            return false;

        float zoneDuration = cfg.cycloneDuration > 0f ? cfg.cycloneDuration : 2f;

        void SpawnZone(int row)
        {
            var instance = Instantiate(prefab);
            var zone = instance.GetComponent<CycloneZone>();
            if (zone == null)
            {
                Destroy(instance);
                return;
            }
            zone.Setup(columnManager, row, cfg.damage, cfg.landingDamage,
                cfg.bossPoiseDamagePercent, 1.2f, zoneDuration);
        }

        for (int row = 0; row < cfg.rangeRows; row++)
            SpawnZone(row);

        // Boss 固定在第二排，若 rangeRows 覆盖不到则额外创建 Boss 排 Zone
        for (int col = 0; col < columnManager.columnCount; col++)
        {
            var boss = columnManager.GetCombatBossCoveringColumn(col);
            if (boss != null && boss.rowIndex >= cfg.rangeRows)
            {
                SpawnZone(boss.rowIndex);
                break; // 一个 Boss 排 Zone 即可覆盖所有列
            }
        }

        return true;
    }

    private bool ActivateWave(ActiveSkillDefinition definition, int level)
    {
        if (definition.waveLevels == null || level > definition.waveLevels.Count)
            return false;

        var cfg = definition.waveLevels[level - 1];
        var waveManager = _waveManager != null ? _waveManager : WaveManager.Instance;
        if (waveManager == null || cfg.rangeRows <= 0 || cfg.damage < 0)
            return false;

        waveManager.TriggerWave(0, cfg.rangeRows - 1, cfg.damage, cfg.bossPoiseDamagePercent);
        return true;
    }

    private bool ActivateDisease(ActiveSkillDefinition definition, int level)
    {
        if (definition.diseaseLevels == null || level > definition.diseaseLevels.Count)
            return false;

        var config = definition.diseaseLevels[level - 1];
        if (config.totalDamage <= 0 || config.durationSeconds <= 0)
            return false;

        _armedDiseaseDefinition = definition;
        _armedDiseaseLevel = level;
        _armedDiseaseLayers++;
        Debug.Log($"[ActiveSkillRunner] 染病已武装: {definition.displayName} Lv.{level}, layers={_armedDiseaseLayers}");
        return true;
    }

    public bool ConsumeArmedDisease(Enemy enemy)
    {
        if (_armedDiseaseDefinition == null || enemy == null || UpgradeEffectManager.Instance == null)
            return false;

        var config = _armedDiseaseDefinition.diseaseLevels[_armedDiseaseLevel - 1];
        UpgradeEffectManager.Instance.ApplyDisease(enemy, config.totalDamage, config.durationSeconds, _armedDiseaseLayers, smartSpread: config.smartSpread);
        Debug.Log($"[ActiveSkillRunner] 染病附着: {enemy.DebugTag}, totalDamage={config.totalDamage}, duration={config.durationSeconds}s, layers={_armedDiseaseLayers}");
        _armedDiseaseDefinition = null;
        _armedDiseaseLevel = 0;
        _armedDiseaseLayers = 0;
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
        _armedDiseaseDefinition = null;
        _armedDiseaseLevel = 0;
        _armedDiseaseLayers = 0;
    }
}

public struct ActiveChargeAttackShockwaveConsumeResult
{
    public string upgradeId;
    public int layers;
    public ChargeAttackShockwaveLevelConfig config;
}

