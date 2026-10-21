using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// DisplacementMover 的**聚拢波与补齐收尾**：聚拢伤害与位移后的补齐触发。
///
/// 按机制从 <c>DisplacementMover.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 DisplacementMover.cs **同类型**，行为完全一致（本刀只搬不改，位移三原则的判定逻辑原样保留）。
/// </summary>

public sealed partial class DisplacementMover
{
    public void ApplyConvergenceWave(List<Enemy> hitEnemies, int step, float convergenceDamagePercent, bool canInterruptCFrame = false)
    {
        if (hitEnemies == null || hitEnemies.Count == 0) return;
        if (step <= 0) return;

        DebugLog.Info($"[Displacement] ApplyConvergenceWave: step={step} hitCount={hitEnemies.Count}");

        _rowEnemies.Clear();
        foreach (var e in hitEnemies)
        {
            if (e.state == EnemyState.Dead || e.isBoss) continue;
            if (e.isCFrame && !canInterruptCFrame) continue;
            if (!_rowEnemies.ContainsKey(e.rowIndex))
                _rowEnemies[e.rowIndex] = new List<Enemy>();
            _rowEnemies[e.rowIndex].Add(e);
        }

        // 槽位优先级：从中心 col=2 向外
        int[] prioritySlots = { 2, 1, 3, 0, 4 };

        foreach (var kv in _rowEnemies)
        {
            int row = kv.Key;
            var enemies = kv.Value;
            int N = enemies.Count;

            // 取 N 个最靠近中心的槽位
            var slots = new List<int>(N);
            for (int i = 0; i < N && i < prioritySlots.Length; i++)
                slots.Add(prioritySlots[i]);

            // 按距 col=2 由近到远排序（近者优先分配近槽）
            enemies.Sort((a, b) => Mathf.Abs(a.columnIndex - 2).CompareTo(Mathf.Abs(b.columnIndex - 2)));

            for (int i = 0; i < enemies.Count && i < slots.Count; i++)
            {
                var enemy = enemies[i];
                int slot = slots[i];
                int dist = Mathf.Abs(enemy.columnIndex - slot);
                if (dist > 0 && dist <= step)
                {
                    DebugLog.Info($"[Displacement] Conv MOVE {enemy.DebugTag} col {enemy.columnIndex}→{slot} row={row}");
                    MoveEnemyToColumnAtRow(enemy, slot, row);
                }
            }
        }

        // Convergence is horizontal-only and must not publish row-topology changes.
    }

    /// <summary>
    /// Arms exact-slot returns for enemies that were actually pushed backward.
    /// Complete only after this same enemy reaches its exact first origin; no other enemy is moved.
    /// </summary>
    public void PostDisplacementFillUp(IEnumerable<Enemy> pushedEnemies)
    {
        if (pushedEnemies == null) return;

        foreach (var enemy in pushedEnemies)
        {
            if (enemy == null || enemy.state == EnemyState.Dead)
                continue;
            if (_pushReturnTransactions.TryGetValue(enemy, out var transaction))
                transaction.scheduled = true;
        }
    }

    /// <summary>
    /// Legacy no-argument entry retained for API compatibility. It cannot create return orders
    /// because exact origins are registered only by ExecutePush.
    /// </summary>
    public void PostDisplacementFillUp()
    {
    }
}