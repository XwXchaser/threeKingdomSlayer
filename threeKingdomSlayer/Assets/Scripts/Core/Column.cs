using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单列敌人管理
/// 维护一列中的敌人列表，index 0 = 最前排（靠近玩家）
/// </summary>
[System.Serializable]
public class Column
{
    public int columnIndex; // 0~4
    public List<Enemy> enemies = new List<Enemy>(); // index 0 = 最前排

    public Column(int index)
    {
        columnIndex = index;
        enemies = new List<Enemy>();
    }

    /// <summary>
    /// 获取最前排的敌人
    /// </summary>
    public Enemy GetFrontEnemy()
    {
        if (enemies.Count > 0)
            return enemies[0];
        return null;
    }

    /// <summary>
    /// 获取指定排的敌人（0=最前排）
    /// </summary>
    public Enemy GetEnemyAtRow(int rowIndex)
    {
        // BUG FIX: 不能使用 enemies[rowIndex] 列表索引——compact/fill-up/push 后
        // 列表位置 ≠ rowIndex。必须按 enemy.rowIndex 遍历查找。
        foreach (var e in enemies)
        {
            if (e.rowIndex == rowIndex)
                return e;
        }
        return null;
    }

    /// <summary>
    /// 在队列末尾添加敌人
    /// 注意：enemy 的 rowIndex 应在调用此方法前由调用方设置好
    /// 此方法仅将敌人加入列表，不再覆盖 rowIndex
    /// </summary>
    public void AddEnemy(Enemy enemy)
    {
        // BUG FIX: 不再覆盖 enemy 的 rowIndex
        // 调用方（如 WaveSpawner.SpawnRow）已通过 enemy.Initialize() 设置了正确的 rowIndex
        // 这里只设置 columnIndex 以确保列索引正确
        enemy.columnIndex = columnIndex;
        enemies.Add(enemy);
    }

    /// <summary>
    /// 移除指定敌人。仅从列表中删除，不做紧凑或补齐。
    /// 普通补齐与击退回位均由 ColumnManager 的独立调度器负责。
    /// </summary>
    public void RemoveEnemy(Enemy enemy, bool skipChain = false)
    {
        int index = enemies.IndexOf(enemy);
        if (index >= 0)
        {
            enemies.RemoveAt(index);
            int colIndex = enemy.columnIndex;
            DebugLog.Info($"[Column] RemoveEnemy: column={colIndex}, deadIndex={index}, remaining={enemies.Count}");
        }

        if (_chainWaitingEnemy == enemy)
        {
            enemy.OnRushMoveComplete -= OnChainRushComplete;
            _chainWaitingEnemy = null;
            DebugLog.Info($"[Column] 列链等待者已移除: {enemy.DebugTag}, col={columnIndex}");
            if (!skipChain)
                TryAdvanceChain();
        }
    }

    public void ResumeRushMoveChain()
    {
        if (_chainEndHandler != null && _chainWaitingEnemy == null)
            TryAdvanceChain();
    }

    /// <summary>
    /// Runs one owned displacement chain. Every member is assigned the same owner/generation,
    /// and stale completion callbacks are ignored.
    /// </summary>
    public void StartRushMoveChain(RushMoveOrderOwner owner, int generation, System.Action onChainEnd = null)
    {
        CancelRushMoveChain();
        _chainOwner = owner;
        _chainGeneration = generation;
        _chainEndHandler = onChainEnd;
        TryAdvanceChain();
    }

    private System.Action _chainEndHandler;
    private Enemy _chainWaitingEnemy;
    private RushMoveOrderOwner _chainOwner = RushMoveOrderOwner.None;
    private int _chainGeneration;

    public void ReleaseRushMoveOwnership(Enemy enemy)
    {
        if (_chainWaitingEnemy != enemy) return;

        enemy.OnRushMoveComplete -= OnChainRushComplete;
        _chainWaitingEnemy = null;
    }

    public void CancelRushMoveChain(bool cancelOwnedMovement = false)
    {
        if (_chainWaitingEnemy != null)
        {
            _chainWaitingEnemy.OnRushMoveComplete -= OnChainRushComplete;
            _chainWaitingEnemy = null;
        }

        if (cancelOwnedMovement && _chainOwner != RushMoveOrderOwner.None)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy != null)
                    enemy.CancelRushMoveOrder(_chainOwner, _chainGeneration, resetActiveMovement: true);
            }
        }

        _chainEndHandler = null;
        _chainOwner = RushMoveOrderOwner.None;
        _chainGeneration = 0;
    }

    private void TryAdvanceChain()
    {
        if (_chainWaitingEnemy != null)
            return;

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || enemy.isBoss || enemy.state == EnemyState.Dead)
                continue;
            if (!enemy.IsRushMoveOrder(_chainOwner, _chainGeneration))
                continue;
            if (!enemy.IsRushMoveReady)
            {
                _chainWaitingEnemy = enemy;
                enemy.OnRushMoveComplete -= OnChainRushComplete;
                enemy.OnRushMoveComplete += OnChainRushComplete;
                var deferredResult = enemy.TryStartRushMove();
                if (deferredResult == RushMoveStartResult.Rejected)
                {
                    enemy.OnRushMoveComplete -= OnChainRushComplete;
                    enemy.CancelRushMoveOrder(_chainOwner, _chainGeneration);
                    _chainWaitingEnemy = null;
                    continue;
                }
                return;
            }

            _chainWaitingEnemy = enemy;
            enemy.OnRushMoveComplete -= OnChainRushComplete;
            enemy.OnRushMoveComplete += OnChainRushComplete;
            var result = enemy.TryStartRushMove();
            if (result == RushMoveStartResult.Rejected)
            {
                enemy.OnRushMoveComplete -= OnChainRushComplete;
                enemy.CancelRushMoveOrder(_chainOwner, _chainGeneration);
                _chainWaitingEnemy = null;
                continue;
            }

            return;
        }

        var handler = _chainEndHandler;
        _chainEndHandler = null;
        _chainOwner = RushMoveOrderOwner.None;
        _chainGeneration = 0;
        handler?.Invoke();
    }

    private void OnChainRushComplete(Enemy enemy, RushMoveOrderOwner owner, int generation)
    {
        enemy.OnRushMoveComplete -= OnChainRushComplete;
        if (_chainWaitingEnemy != enemy || owner != _chainOwner || generation != _chainGeneration)
            return;

        _chainWaitingEnemy = null;
        TryAdvanceChain();
    }

    /// <summary>
    /// Landing no longer starts a new column chain. Existing owned orders resume through Enemy.TryStartRushMove.
    /// </summary>
    public void StartRushFromLaunched(Enemy enemy, System.Action onChainEnd = null)
    {
        if (enemy == null || !enemy.IsRushMoveOrder(_chainOwner, _chainGeneration)) return;
        if (_chainWaitingEnemy != null && _chainWaitingEnemy != enemy) return;

        if (_chainEndHandler == null)
            _chainEndHandler = onChainEnd;
        _chainWaitingEnemy = enemy;
        enemy.OnRushMoveComplete -= OnChainRushComplete;
        enemy.OnRushMoveComplete += OnChainRushComplete;
        enemy.TryStartRushMove();
    }

    /// <summary>
    /// Legacy row-compaction API retained for compatibility. Ordinary WaveMarch is owned by ColumnManager.
    /// </summary>
    public void CompactByClearRows(bool[] clearRows, ISet<Enemy> protectedEnemies = null)
    {
        DebugLog.Warning($"[Column] CompactByClearRows ignored: legacy compaction is disabled, col={columnIndex}");
    }

    /// <summary>
    /// Legacy entry retained for API compatibility. Normal-enemy scheduling is owned by ColumnManager.
    /// </summary>
    public void TriggerFillForward()
    {
        DebugLog.Warning($"[Column] TriggerFillForward ignored for normal enemies: col={columnIndex}");
    }

    /// <summary>
    /// Boss 独立补齐入口：Boss 不参与列链，需在普通敌人链启动后自行前移。
    /// </summary>
    public void TriggerBossFillForward()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].isBoss && enemies[i].bossState == BossState.None)
            {
                enemies[i].targetRow = 0;
                enemies[i].StartFillForwardDelay(0.5f);
                DebugLog.Info($"[Column] 触发Boss独立补齐: {enemies[i].DebugTag}, col={columnIndex}, row={enemies[i].rowIndex}");
                break;
            }
        }
    }

    /// <summary>
    /// 获取该列敌人总数
    /// </summary>
    public int EnemyCount => enemies.Count;

    /// <summary>
    /// 该列是否为空
    /// </summary>
    public bool IsEmpty => enemies.Count == 0;

    /// <summary>
    /// 是否有待补齐的敌人（pendingRushMove=true）
    /// </summary>
    public bool HasPendingRushEnemies()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (!enemy.isBoss && enemy.HasRushMoveOrder) return true;
        }
        return false;
    }

    #region 位移辅助方法

    /// <summary>
    /// 静默移除敌人（不触发补齐链、不压缩列表），用于位移操作。
    /// </summary>
    public void RemoveEnemySilent(Enemy enemy)
    {
        enemies.Remove(enemy);
    }

    /// <summary>
    /// 按 rowIndex 升序插入敌人，用于位移后重新插入。
    /// 不触发补齐链。调用前 enemy.rowIndex 需已设为目标值。
    /// </summary>
    public void InsertEnemySorted(Enemy enemy)
    {
        enemy.columnIndex = columnIndex;
        int insertIdx = enemies.Count;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].rowIndex > enemy.rowIndex)
            {
                insertIdx = i;
                break;
            }
        }
        // 防御性检测：若插入位置前后存在同 rowIndex 的敌人，记录警告
        if (insertIdx < enemies.Count && enemies[insertIdx].rowIndex == enemy.rowIndex)
            DebugLog.Warning($"[Column] InsertEnemySorted OVERLAP: {enemy.DebugTag} row={enemy.rowIndex} collides with {enemies[insertIdx].DebugTag} at same row in col={columnIndex}");
        else if (insertIdx > 0 && enemies[insertIdx - 1].rowIndex == enemy.rowIndex)
            DebugLog.Warning($"[Column] InsertEnemySorted OVERLAP: {enemy.DebugTag} row={enemy.rowIndex} collides with {enemies[insertIdx - 1].DebugTag} at same row in col={columnIndex}");
        enemies.Insert(insertIdx, enemy);
    }

    /// <summary>
    /// 检查指定 rowIndex 是否已被占据（排除指定敌人自身）。
    /// </summary>
    public bool IsRowOccupied(int rowIndex, Enemy exclude = null)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != exclude && enemies[i].rowIndex == rowIndex)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Legacy displacement-compaction API retained for compatibility. Backward push is now
    /// handled by ColumnManager exact-slot PushReturn transactions, so this method is inert.
    /// </summary>
    public void PrepareDisplacementCompaction(ISet<Enemy> protectedEnemies)
    {
        DebugLog.Warning($"[Column] PrepareDisplacementCompaction ignored: legacy displacement compaction is disabled, col={columnIndex}");
    }

    /// <summary>
    /// Legacy manual column-compaction API retained for compatibility. It is intentionally inert.
    /// </summary>
    public void CompactColumn(int bossRow, int rangeStart = -1, int rangeEnd = -1)
    {
        DebugLog.Warning($"[Column] CompactColumn ignored: legacy compaction is disabled, col={columnIndex}");
    }

    #endregion
}
