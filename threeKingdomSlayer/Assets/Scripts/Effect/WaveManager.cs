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
    private HashSet<Enemy> _waveHitEnemies = new HashSet<Enemy>();
    private readonly List<Enemy> _wavePushedEnemies = new List<Enemy>();

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
    /// <param name="startRow">起始排（最前排=0）</param>
    /// <param name="endRow">结束排（含）</param>
    /// <param name="damage">伤害值</param>
    public void TriggerWave(int startRow, int endRow, int damage)
    {
        if (wavePrefab == null)
        {
            Debug.LogWarning("[WaveManager] wavePrefab 未配置");
            return;
        }

        CacheFormationParams();
        StartCoroutine(WaveSequence(startRow, endRow, damage));
    }

    /// <summary>
    /// 触发海浪效果（覆盖所有排，从最前排到最后排）
    /// </summary>
    public void TriggerWave(int damage)
    {
        CacheFormationParams();
        TriggerWave(0, _maxRow, damage);
    }

    private IEnumerator WaveSequence(int startRow, int endRow, int damage)
    {
        _waveHitEnemies.Clear();
        _wavePushedEnemies.Clear();
        var delay = new WaitForSeconds(rowStaggerDelay);

        for (int row = startRow; row <= endRow; row++)
        {
            SpawnWaveForRow(row, damage);
            yield return delay;
        }

        // Backward-push effects are aggregated so only actually pushed enemies arm exact-slot returns.
        if (wavePrefab != null)
        {
            var player = wavePrefab.GetComponent<WaveEffectPlayer>();
            if (player != null)
            {
                float waveDuration = player.frameInterval * 5f;
                yield return new WaitForSeconds(waveDuration);
            }
        }

        if (_wavePushedEnemies.Count > 0)
            AttackSystem.Instance?.columnManager?.PostDisplacementFillUp(_wavePushedEnemies);
    }

    private void SpawnWaveForRow(int row, int damage)
    {
        float rowZ = GetRowZ(row);
        Vector3 spawnPos = new Vector3(0f, waveYOffset, rowZ + waveStartZOffset);

        var go = Instantiate(wavePrefab, spawnPos, Quaternion.identity);
        var player = go.GetComponent<WaveEffectPlayer>();
        if (player != null)
        {
            player.Play(spawnPos, row, damage, _waveHitEnemies, _wavePushedEnemies);
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
