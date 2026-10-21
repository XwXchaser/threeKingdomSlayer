using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 新波次**入场调度**（SpawnEntry）。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 3）。职责边界（规则见
/// <c>design/percolumn-fillup-rules.md</c>）：
///   • 敌人先生成在逻辑目标排**后方固定两排**，再由本类连续移动到各自逻辑排目标；
///   • **入场不是普通补齐**：不改逻辑排队列、不删除普通空排、不开启 999 门、
///     也不调用普通的 <c>StartWaveMarch()</c>；
///   • 全部入场完成后，才回调宿主「可以请求布局重排」。
///
/// 依赖：逻辑排队列（取目标排）+ 一个「入场全部完成」的回调；其余（派单、移动）
/// 都走 Enemy 自己的 RushMove 订单 API，不反向依赖 ColumnManager。
/// </summary>
public sealed class SpawnEntryMover
{
    private readonly LogicalRowQueue _queue;
    private readonly Action _onAllEntryCompleted;

    private bool _isPreparing;
    private int _generation;
    private readonly List<Enemy> _enemies = new List<Enemy>();
    private readonly HashSet<Enemy> _pending = new HashSet<Enemy>();

    public SpawnEntryMover(LogicalRowQueue queue, Action onAllEntryCompleted)
    {
        _queue = queue;
        _onAllEntryCompleted = onAllEntryCompleted;
    }

    /// <summary>是否正在准备入场（准备期间不发起普通补齐）。</summary>
    public bool IsPreparing => _isPreparing;

    /// <summary>入场订单的 generation（订单校验用）。</summary>
    public int Generation => _generation;

    /// <summary>还有没有已放行但尚未走完的入场者。</summary>
    public bool HasPending => _pending.Count > 0;

    /// <summary>清空入场状态（重开关卡 / 重配波次布局时）。</summary>
    public void Reset()
    {
        _isPreparing = false;
        _enemies.Clear();
        _pending.Clear();
    }

    /// <summary>该敌人是否处在入场流程中（准备中或已放行未走完）。</summary>
    public bool IsPending(Enemy enemy)
    {
        return enemy != null && (_isPreparing && _enemies.Contains(enemy) || _pending.Contains(enemy));
    }

    /// <summary>该敌人能否开始执行入场订单（generation 校验）。</summary>
    public bool CanStartOrder(Enemy enemy, int generation)
    {
        return !_isPreparing && generation == _generation && _pending.Contains(enemy);
    }

    /// <summary>开始一轮入场：给每个「还在目标排之后」的敌人派 SpawnEntry 订单。</summary>
    public void Start(IList<Enemy> enemies)
    {
        if (enemies == null || enemies.Count == 0)
            return;

        _generation++;
        if (_generation <= 0) _generation = 1;
        _enemies.Clear();
        _pending.Clear();

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null || enemy.isBoss || enemy.state == EnemyState.Dead)
                continue;
            if (!_queue.TryGetRowForEnemy(enemy, out var logicalRow) || logicalRow.removed)
                continue;

            int targetRow = logicalRow.currentRow;
            if (enemy.rowIndex <= targetRow)
                continue;
            enemy.GetComponent<CavalryEnemy>()?.NotifySpawnEntryStarted();
            if (!enemy.AssignRushMoveOrder(RushMoveOrderOwner.SpawnEntry, _generation, targetRow, RushMoveMode.ContinuousEmptyRun))
            {
                enemy.GetComponent<CavalryEnemy>()?.NotifySpawnEntryCompleted();
                continue;
            }

            enemy.OnRushMoveComplete -= OnComplete;
            enemy.OnRushMoveComplete += OnComplete;
            _enemies.Add(enemy);
        }

        _isPreparing = _enemies.Count > 0;
    }

    /// <summary>从入场流程里摘掉一个敌人（死亡 / 被移出列时）。</summary>
    public void Remove(Enemy enemy)
    {
        if (enemy == null) return;
        if (_enemies.Remove(enemy) || _pending.Remove(enemy))
            enemy.OnRushMoveComplete -= OnComplete;
    }

    /// <summary>
    /// 每帧推进：剔除中途死亡的入场者；全员就绪后放行（开始真正移动）。
    /// 返回 true 表示「入场流程占用中」，宿主此时应跳过其它调度分支。
    /// </summary>
    public bool Tick()
    {
        if (!_isPreparing) return false;

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (enemy == null || enemy.state == EnemyState.Dead || !enemy.IsRushMoveOrder(RushMoveOrderOwner.SpawnEntry, _generation))
            {
                if (enemy != null)
                    enemy.OnRushMoveComplete -= OnComplete;
                _enemies.RemoveAt(i);
                continue;
            }
            if (!enemy.IsRushMoveReady)
                return true;
        }

        StartPrepared();
        return true;
    }

    private void StartPrepared()
    {
        _isPreparing = false;
        _pending.Clear();
        for (int i = 0; i < _enemies.Count; i++)
        {
            var enemy = _enemies[i];
            if (enemy == null || !enemy.IsRushMoveOrder(RushMoveOrderOwner.SpawnEntry, _generation))
                continue;
            _pending.Add(enemy);
        }
        _enemies.Clear();

        foreach (var enemy in _pending)
            enemy.TryStartRushMove();
    }

    private void OnComplete(Enemy enemy, RushMoveOrderOwner owner, int generation)
    {
        if (owner != RushMoveOrderOwner.SpawnEntry || generation != _generation)
            return;
        enemy.OnRushMoveComplete -= OnComplete;
        _pending.Remove(enemy);
        enemy.GetComponent<CavalryEnemy>()?.NotifySpawnEntryCompleted();
        if (_pending.Count == 0)
            _onAllEntryCompleted?.Invoke();
    }
}
