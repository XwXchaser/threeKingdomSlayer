using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 海浪效果管理器 — 编排逐排错开的海浪特效
///
/// 调用 TriggerWave(startRow, endRow, damage) 启动海浪序列：
/// 从 startRow 到 endRow，每排错开 rowStaggerDelay 秒依次生成海浪。
/// 每个海浪使用 WaveEffectPlayer 播放 wave1→2→3→2→1 动画。
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("海浪预制体")]
    public GameObject wavePrefab;

    [Header("旋风预制体（用于主动技能旋风）")]
    public GameObject cycloneWavePrefab;

    [Header("编排参数")]
    [Tooltip("每排海浪之间的错开延迟（秒）")]
    public float rowStaggerDelay = 0.15f;

    [Header("定位参数")]
    [Tooltip("海浪Y轴高度偏移")]
    public float waveYOffset = 0.5f;
    [Tooltip("海浪起始Z偏移（在目标排前方）")]
    public float waveStartZOffset = -1f;

    private int _maxRow;
    private float _rowSpacing;
    private float _formationZ;

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
        CacheFormationParams();
    }

    private void CacheFormationParams()
    {
        if (StageController.Instance != null)
        {
            _maxRow = StageController.Instance.GetMaxVisibleRows() - 1;
            _rowSpacing = StageController.Instance.GetRowSpacing();
            _formationZ = StageController.Instance.GetFormationOffsetZ();
        }
        else
        {
            _maxRow = 4;
            _rowSpacing = 2.5f;
            _formationZ = 0f;
        }
    }

    /// <summary>
    /// 触发海浪效果
    /// </summary>
    public void TriggerWave(int startRow, int endRow, int damage)
    {
        TriggerWave(startRow, endRow, damage, 0f, wavePrefab);
    }

    public void TriggerWave(int startRow, int endRow, int damage, float bossPoiseDamagePercent)
    {
        TriggerWave(startRow, endRow, damage, bossPoiseDamagePercent, wavePrefab);
    }

    /// <summary>
    /// 触发海浪效果（使用自定义预制体）
    /// </summary>
    public void TriggerWave(int startRow, int endRow, int damage, float bossPoiseDamagePercent, GameObject overridePrefab)
    {
        if (overridePrefab == null)
        {
            Debug.LogWarning("[WaveManager] overridePrefab 未配置");
            return;
        }

        CacheFormationParams();
        var immediateHits = ApplyImmediateActiveHits(startRow, endRow, bossPoiseDamagePercent);
        StartCoroutine(WaveSequence(startRow, endRow, damage, bossPoiseDamagePercent, immediateHits, overridePrefab));
    }

    /// <summary>
    /// 触发海浪效果（覆盖所有排，从最前排到最后排）
    /// </summary>
    public void TriggerWave(int damage)
    {
        CacheFormationParams();
        TriggerWave(0, _maxRow, damage, 0f, wavePrefab);
    }

    private HashSet<Enemy> ApplyImmediateActiveHits(int startRow, int endRow, float bossPoiseDamagePercent)
    {
        var hitEnemies = new HashSet<Enemy>();
        var enemies = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (enemies == null) return hitEnemies;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || enemy.isPhaseTransitioning) continue;
            if (enemy.isBoss && enemy.bossState != BossState.InCombat) continue;
            if (!enemy.isBoss && (enemy.rowIndex < startRow || enemy.rowIndex > endRow)) continue;

            if (!enemy.ApplyActiveDisplacementHit()) continue;
            if (enemy.isBoss && enemy.state != EnemyState.Stunned)
                enemy.TakeActiveDisplacementPoiseDamage(bossPoiseDamagePercent);
            hitEnemies.Add(enemy);
        }
        return hitEnemies;
    }

    private IEnumerator WaveSequence(int startRow, int endRow, int damage, float bossPoiseDamagePercent, HashSet<Enemy> immediateHits, GameObject prefab)
    {
        var hitEnemies = new HashSet<Enemy>();
        var immediateHitEnemies = immediateHits ?? new HashSet<Enemy>();
        var pushedEnemies = new List<Enemy>();
        var combatBosses = SnapshotCombatBosses();
        var delay = new WaitForSeconds(rowStaggerDelay);

        for (int row = startRow; row <= endRow; row++)
        {
            SpawnWaveForRow(row, damage, bossPoiseDamagePercent, hitEnemies, immediateHitEnemies, pushedEnemies, prefab);
            yield return delay;
        }

        for (int i = 0; i < combatBosses.Count; i++)
        {
            var boss = combatBosses[i];
            if (boss == null || hitEnemies.Contains(boss)) continue;
            if (boss.rowIndex >= startRow && boss.rowIndex <= endRow) continue;
            SpawnBossWave(boss, damage, bossPoiseDamagePercent, hitEnemies, immediateHitEnemies, prefab);
        }

        // Backward-push effects are aggregated so only actually pushed enemies arm exact-slot returns.
        if (prefab != null)
        {
            var player = prefab.GetComponent<WaveEffectPlayer>();
            if (player != null)
            {
                float waveDuration = player.frameInterval * 5f;
                yield return new WaitForSeconds(waveDuration);
            }
        }

        if (pushedEnemies.Count > 0)
            AttackSystem.Instance?.columnManager?.PostDisplacementFillUp(pushedEnemies);
    }

    private List<Enemy> SnapshotCombatBosses()
    {
        var combatBosses = new List<Enemy>();
        var enemies = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        if (enemies == null) return combatBosses;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy != null && enemy.isBoss && enemy.bossState == BossState.InCombat)
                combatBosses.Add(enemy);
        }
        return combatBosses;
    }

    private void SpawnBossWave(Enemy boss, int damage, float bossPoiseDamagePercent, HashSet<Enemy> hitEnemies, HashSet<Enemy> immediateHitEnemies, GameObject prefab)
    {
        if (boss == null || boss.state == EnemyState.Dead || boss.isPhaseTransitioning) return;

        Vector3 spawnPos = new Vector3(0f, waveYOffset, GetRowZ(boss.rowIndex) + waveStartZOffset);
        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        var player = go.GetComponent<WaveEffectPlayer>();
        if (player != null)
            player.PlayBossOnly(spawnPos, boss, damage, bossPoiseDamagePercent, hitEnemies, immediateHitEnemies);
        else
            Destroy(go);
    }

    private void SpawnWaveForRow(int row, int damage, float bossPoiseDamagePercent, HashSet<Enemy> hitEnemies, HashSet<Enemy> immediateHitEnemies, List<Enemy> pushedEnemies, GameObject prefab)
    {
        float rowZ = GetRowZ(row);
        Vector3 spawnPos = new Vector3(0f, waveYOffset, rowZ + waveStartZOffset);

        var go = Instantiate(prefab, spawnPos, Quaternion.identity);
        var player = go.GetComponent<WaveEffectPlayer>();
        if (player != null)
        {
            player.Play(spawnPos, row, damage, bossPoiseDamagePercent, hitEnemies, immediateHitEnemies, pushedEnemies);
        }
        else
        {
            Debug.LogWarning("[WaveManager] WaveEffect.prefab 缺少 WaveEffectPlayer 组件");
            Destroy(go);
        }
    }

    private float GetRowZ(int row)
    {
        // 与 Enemy.GetRowZ 公式一致: (maxRow - row) * (-spacing) + offset
        // 化简为: (row - maxRow) * spacing + offset
        return (row - _maxRow) * _rowSpacing + _formationZ;
    }
}
