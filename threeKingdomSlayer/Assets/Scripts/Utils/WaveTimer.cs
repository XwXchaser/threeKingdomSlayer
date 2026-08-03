using UnityEngine;

/// <summary>
/// 波次计时器：记录每波通关耗时，Boss战额外记录Boss击杀耗时。
/// 挂载到 WaveSpawner 所在的 GameObject 上即可。
/// </summary>
public class WaveTimer : MonoBehaviour
{
    private WaveSpawner _waveSpawner;
    private StageConfig _stageConfig;

    private float _waveStartTime;
    private float _bossDieTime;
    private int _currentWaveIndex = -1;
    private bool _isBossWave;
    private bool _bossDied;

    private void Start()
    {
        _waveSpawner = FindObjectOfType<WaveSpawner>();
        _stageConfig = _waveSpawner?.ResolvedStageConfig;

        if (_waveSpawner == null)
        {
            Debug.LogWarning("[WaveTimer] WaveSpawner not found");
            return;
        }

        _waveSpawner.OnWaveStarted += OnWaveStarted;
        _waveSpawner.OnWaveCompleted += OnWaveCompleted;

        // 监听Boss死亡
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
            enemyManager.OnAnyEnemyDied += OnAnyEnemyDied;
    }

    private void OnDestroy()
    {
        if (_waveSpawner != null)
        {
            _waveSpawner.OnWaveStarted -= OnWaveStarted;
            _waveSpawner.OnWaveCompleted -= OnWaveCompleted;
        }
        var enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
            enemyManager.OnAnyEnemyDied -= OnAnyEnemyDied;
    }

    private void OnWaveStarted(int waveIndex)
    {
        _currentWaveIndex = waveIndex;
        _waveStartTime = Time.time;
        _bossDied = false;
        _bossDieTime = 0f;

        _isBossWave = false;
        if (_stageConfig != null && waveIndex < _stageConfig.waves.Count)
            _isBossWave = _stageConfig.waves[waveIndex].isBossWave;

        string bossTag = _isBossWave ? " [BOSS]" : "";
        Debug.Log($"[WaveTimer] Wave{waveIndex + 1}{bossTag} 开始 | time={Time.time:F1}s");
    }

    private void OnWaveCompleted(int waveIndex)
    {
        float elapsed = Time.time - _waveStartTime;
        string bossTag = _isBossWave ? " [BOSS]" : "";

        string timeInfo;
        if (_isBossWave && _bossDied)
        {
            float bossElapsed = _bossDieTime - _waveStartTime;
            timeInfo = $"总耗时={elapsed:F1}s | Boss击杀={bossElapsed:F1}s";
        }
        else
        {
            timeInfo = $"耗时={elapsed:F1}s";
        }

        Debug.Log($"[WaveTimer] Wave{waveIndex + 1}{bossTag} 完成 | {timeInfo} | {GetPlayerBuildString()}");
    }

    private static string GetPlayerBuildString()
    {
        var ps = PlayerState.Instance;
        int lv = ps != null ? ps.currentLevel : -1;
        float xp = ps != null ? ps.currentExp : 0f;
        int xpNeed = ps != null ? ps.GetExpRequiredForNextLevel() : -1;

        var parts = new System.Text.StringBuilder();
        parts.Append($"Lv={lv}");
        if (xpNeed > 0)
            parts.Append($" XP={xp:F0}/{xpNeed}");

        // 被动
        var passives = new System.Collections.Generic.List<string>();
        if (ps != null && ps.acquiredUpgrades != null)
        {
            foreach (var u in ps.acquiredUpgrades)
            {
                if (u.currentLevel <= 0) continue;
                // 过滤掉主动技能（它们在 ActiveSkillInventory 中）
                if (u.definition != null && u.definition.category == UpgradeCategory.ActiveSkill) continue;
                passives.Add($"{u.definition.displayName}Lv{u.currentLevel}");
            }
        }
        if (passives.Count > 0)
            parts.Append($" | 被动: {string.Join(" ", passives)}");

        // 主动
        var actives = new System.Collections.Generic.List<string>();
        var inv = ActiveSkillInventory.Instance;
        if (inv != null)
        {
            foreach (var e in inv.Entries)
            {
                if (e.definition == null) continue;
                actives.Add($"{e.definition.displayName}Lv{e.level}");
            }
        }
        if (actives.Count > 0)
            parts.Append($" | 主动: {string.Join(" ", actives)}");

        return parts.ToString();
    }

    private void OnAnyEnemyDied(Enemy enemy)
    {
        if (!_isBossWave || _bossDied || enemy == null || !enemy.isBoss)
            return;

        // Boss在本波内死亡
        _bossDied = true;
        _bossDieTime = Time.time;

        float bossElapsed = _bossDieTime - _waveStartTime;
        int waveNum = _currentWaveIndex + 1;
        float hp = enemy.maxHealth;
        Debug.Log($"[WaveTimer] Wave{waveNum} Boss击杀 | 耗时={bossElapsed:F1}s | BossHP={hp:F0}");
    }
}
