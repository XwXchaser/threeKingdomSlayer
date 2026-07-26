using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单排海浪动画播放器 — 挂载于 WaveEffect.prefab
///
/// Play() 启动动画序列: wave1 → wave2 → wave3(判定帧) → wave2 → wave1
/// 每帧沿 +Z 移动，wave3 时触发命中检测和击退。
/// </summary>
public class WaveEffectPlayer : MonoBehaviour
{
    [Header("海浪精灵子对象")]
    public GameObject wave1;
    public GameObject wave2;
    public GameObject wave3;

    [Header("动画节奏")]
    [Tooltip("每帧持续时间（秒）")]
    public float frameInterval = 0.1f;

    [Header("Z轴移动")]
    [Tooltip("整个动画周期的总Z位移")]
    public float zMoveTotal = 2.5f;

    private int _targetRow;
    private int _damage;
    private float _zStep;
    private HashSet<Enemy> _hitEnemies;
    private List<Enemy> _pushedEnemies;

    public void Play(Vector3 startPos, int targetRow, int damage, HashSet<Enemy> hitEnemies, List<Enemy> pushedEnemies)
    {
        _targetRow = targetRow;
        _damage = damage;
        _hitEnemies = hitEnemies;
        _pushedEnemies = pushedEnemies;
        _zStep = zMoveTotal / 5f; // 5段移动（5帧切换之间）

        transform.position = startPos;
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        var wait = new WaitForSeconds(frameInterval);

        // 帧1: wave1
        SetActiveWave(1);
        MoveZ();
        yield return wait;

        // 帧2: wave2
        SetActiveWave(2);
        MoveZ();
        yield return wait;

        // 帧3: wave3 — 判定帧
        SetActiveWave(3);
        MoveZ();
        DoHitCheck();
        yield return wait;

        // 帧4: wave2
        SetActiveWave(2);
        MoveZ();
        yield return wait;

        // 帧5: wave1
        SetActiveWave(1);
        MoveZ();
        yield return wait;

        // 结束
        Destroy(gameObject);
    }

    private void SetActiveWave(int index)
    {
        if (wave1 != null) wave1.SetActive(index == 1);
        if (wave2 != null) wave2.SetActive(index == 2);
        if (wave3 != null) wave3.SetActive(index == 3);
    }

    private void MoveZ()
    {
        transform.position += Vector3.forward * _zStep;
    }

    private void DoHitCheck()
    {
        var cm = AttackSystem.Instance?.columnManager;
        if (cm == null) return;

        // 收集目标排所有存活敌人（遍历列中全部敌人，避免 push 后同排多敌人漏检）
        var hitEnemies = new List<Enemy>();
        for (int col = 0; col < cm.columnCount; col++)
        {
            var colEnemies = cm.GetEnemiesInColumn(col);
            if (colEnemies == null) continue;
            foreach (var enemy in colEnemies)
            {
                if (enemy.rowIndex == _targetRow && enemy.state != EnemyState.Dead)
                    hitEnemies.Add(enemy);
            }
        }

        // 伤害（去重：同一波次序列中已被命中的敌人跳过）
        foreach (var enemy in hitEnemies)
        {
            if (!_hitEnemies.Contains(enemy))
            {
                enemy.TakeDamage(_damage, DamageType.Slash);
                _hitEnemies.Add(enemy);
            }
        }

        // 轻微击退（视觉效果）
        if (hitEnemies.Count > 0)
        {
            cm.ApplyPushWave(hitEnemies, 1, pushedEnemies: _pushedEnemies);
        }
    }
}
