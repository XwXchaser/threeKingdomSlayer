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
    /// 移除指定敌人（通常是最前排死亡）
    /// 移除敌人后，后方所有敌人标记 pendingRushMove = true，
    /// 更新排索引 SetRowIndex(i+1)，然后链式触发补齐移动。
    ///
    /// 链式补齐改进：
    ///   - SetRowIndex(i+1)：后方敌人排索引设为新列表位置+1，
    ///     补齐移动完成后 rowIndex-- = 列表位置，不会与其他敌人重合。
    ///   - 链式触发：第一个敌人补齐移动完全完成(moveProgress>=1.0)时，
    ///     通过 OnRushMoveComplete 事件启动下一个敌人。
    ///     必须等待前一敌人完全补齐完毕，后一敌人才能开始补齐。
    ///
    /// Problem 3 修复（补齐延迟）：
    ///   - 补齐移动完成后若还需继续前进，启动延迟计时器，
    ///     延迟结束后再开始下一次补齐移动。
    /// </summary>
    /// <param name="skipChain">true=仅移除敌人，不压缩列表、不启动链（逐排补齐时由 ColumnManager 统一处理）</param>
    public void RemoveEnemy(Enemy enemy, bool skipChain = false)
    {
        int index = enemies.IndexOf(enemy);
        if (index >= 0)
        {
            enemies.RemoveAt(index);
            int colIndex = enemy.columnIndex;
            DebugLog.Info($"[Column] RemoveEnemy: column={colIndex}, deadIndex={index}, remaining={enemies.Count}, skipChain={skipChain}");

            if (skipChain) return;

            // 紧凑排列存活敌人，Dead 跳过并移除，Launched 保留在原位不参与补齐
            int writeIdx = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy e = enemies[i];
                if (e.state == EnemyState.Dead)
                {
                    DebugLog.Info($"[Column] 跳过 Dead 敌人: {e.DebugTag}, col={colIndex}, row={e.rowIndex}");
                    continue;
                }
                // 存活敌人（含Launched）：紧凑前移并标记补齐
                // Boss 在 Approaching 状态时不参与补齐（由 BossPause/BossResume 自行控制）
                if (i != writeIdx) enemies[writeIdx] = e;
                e.targetRow = writeIdx;

                // 仅当敌人需要移动时才重置状态；正在攻击动画中的敌人不打断
                bool needsMove = e.rowIndex != writeIdx;
                if (needsMove && !e.isAttackAnimating)
                {
                    e.ResetMovementState();
                }
                else if (needsMove && e.isAttackAnimating)
                {
                    // 攻击动画中：保留状态，仅标记 targetRow/pendingRushMove
                    // 由攻击动画 OnComplete 中的 TryStartRushMove 自然衔接
                    DebugLog.Info($"[Column] 标记补齐（保留攻击动画）: {e.DebugTag}, col={colIndex}, curRow={e.rowIndex}, targetRow={writeIdx}");
                }
                // Boss 不参与列补齐链，移动由自身状态机控制
                if (!e.isBoss)
                    e.pendingRushMove = true;
                DebugLog.Info($"[Column] 标记补齐移动: {e.DebugTag}, col={colIndex}, curRow={e.rowIndex}, targetRow={writeIdx}");
                writeIdx++;
            }

            // 移除 Dead 敌人留下的空洞
            if (writeIdx < enemies.Count)
                enemies.RemoveRange(writeIdx, enemies.Count - writeIdx);

            // 启动链式补齐：从第一个 pendingRushMove 的敌人开始
            StartRushMoveChain(colIndex);
        }
    }

    /// <summary>
    /// 从列表中第一个 pendingRushMove=true 的敌人开始链式补齐
    /// </summary>
    private void StartRushMoveChain(int colIndex)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].pendingRushMove)
            {
                enemies[i].OnRushMoveComplete += OnColumnRushMoveComplete;
                enemies[i].TryStartRushMove();
                DebugLog.Info($"[Column] 启动链式补齐: {enemies[i].DebugTag}, col={colIndex}, row={enemies[i].rowIndex}");
                return;
            }
        }
    }

    /// <summary>
    /// 链式补齐回调：当前敌人补齐完成后，启动下一个 pendingRushMove 的敌人
    /// </summary>
    private void OnColumnRushMoveComplete(Enemy enemy)
    {
        enemy.OnRushMoveComplete -= OnColumnRushMoveComplete;

        int idx = enemies.IndexOf(enemy);
        for (int i = idx + 1; i < enemies.Count; i++)
        {
            if (enemies[i].pendingRushMove)
            {
                enemies[i].OnRushMoveComplete += OnColumnRushMoveComplete;
                enemies[i].TryStartRushMove();
                DebugLog.Info($"[Column] 链式触发下一个: {enemies[i].DebugTag}, col={columnIndex}, row={enemies[i].rowIndex}");
                return;
            }
        }
    }

    /// <summary>
    /// 从击飞落地敌人启动链式补齐
    /// 击飞敌人落地后需要前移时，必须通过此方法而非直接 TryStartRushMove()，
    /// 以确保 OnRushMoveComplete 被正确订阅，链式触发不会中断。
    /// </summary>
    public void StartRushFromLaunched(Enemy enemy)
    {
        int idx = enemies.IndexOf(enemy);
        if (idx < 0 || !enemy.pendingRushMove) return;

        // 落地后立即开始补齐，不等待前方敌人移动完成。
        // 前方敌人与落地敌人目标排不同，可并发移动，互不阻塞。
        enemy.OnRushMoveComplete += OnColumnRushMoveComplete;
        enemy.TryStartRushMove();
        DebugLog.Info($"[Column] 击飞落地启动链式: {enemy.DebugTag}, col={columnIndex}, row={enemy.rowIndex}");
    }

    /// <summary>
    /// 逐排补齐：根据 clearRows 压缩本列敌人列表。
    /// clearRows[r]=true 表示第 r 排（跨所有列）已清空，该排的敌人都应移除/跳过。
    /// 压缩后标记需要前移的敌人并启动链式补齐。
    ///
    /// 注意：使用 enemy.rowIndex 而非列表位置判断排归属。
    /// RemoveEnemy(skipChain=true) 后列表位置已变化，rowIndex 才是真实排号。
    /// </summary>
    public void CompactByClearRows(bool[] clearRows, int? pushedToRow = null)
    {
        // 第一遍：计算每个存活的敌人应该移动到的新排号
        // targetRow = rowIndex - 低于该排的已清空排数
        // 这与 PerColumn 的 writeIdx 不同：writeIdx 是顺序紧凑（0,1,2...），
        // 而 row-based 会保留排与排之间的空隙（仅压缩掉已清空的排）
        //
        // pushedToRow: 位移效果推入的目标排。该排敌人不参与紧凑（防止击退被补齐抵消），
        // 但更后排的敌人可越过它们填补前方的空排。

        // BOSS 墙壁：找到本列 BOSS 所在排，BOSS 不参与压缩，身后敌人不可跨越 BOSS
        int bossRow = -1;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy e = enemies[i];
            if (e != null && e.isBoss && e.state != EnemyState.Dead)
            {
                bossRow = e.rowIndex;
                break;
            }
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy e = enemies[i];
            if (e == null) continue;
            int row = e.rowIndex;

            bool isClearRow = row < clearRows.Length && clearRows[row];
            if (e.state == EnemyState.Dead || isClearRow)
                continue;

            // BOSS 不参与紧凑——BOSS 前进由独立的 IsRowClearForBoss + BossPause 系统管控
            if (e.isBoss) continue;

            // 被推入排的敌人不参与紧凑，防止击退效果被补齐抵消
            if (pushedToRow.HasValue && row == pushedToRow.Value)
                continue;

            // 统计低于 row 的已清空排数
            // 若 BOSS 存在且敌人在 BOSS 身后，只统计 BOSS 之后的空排（不可跨越 BOSS 墙壁）
            int clearBelow = 0;
            int scanStart = (bossRow >= 0 && row > bossRow) ? bossRow : 0;
            for (int r = scanStart; r < row && r < clearRows.Length; r++)
            {
                if (clearRows[r]) clearBelow++;
            }

            int newRow = row - clearBelow;
            // 确保 BOSS 身后的敌人不会因压缩而落到 BOSS 之前（BOSS 自身正常前移不受影响）
            if (bossRow >= 0 && !e.isBoss && row > bossRow && newRow <= bossRow)
                newRow = bossRow + 1;

            if (newRow != row)
            {
                e.targetRow = newRow;
                // BUG FIX: 不重置特殊状态敌人。
                // ResetMovementState 会 Kill DOTween 动画 + 重置 state → Idle，
                // 导致晕眩/攻击动作/正在进行的补齐移动/QTE攻击被意外打断。
                // BOSS 免疫位移但可能因无条件 PostDisplacementFillUp 进入此分支，
                // 若处于 QTEAttacking 被重置将导致 QTE 中止。
                // 对于这些状态，仅设置 targetRow 和 pendingRushMove，
                // 由 TryStartRushMove 等待状态恢复后再开始补齐移动。
                if (e.state == EnemyState.Stunned || e.state == EnemyState.Launched || e.state == EnemyState.QTEAttacking || e.isAttackAnimating || e.state == EnemyState.Moving)
                {
                    if (!(e.isBoss && e.bossState == BossState.Approaching))
                        e.pendingRushMove = true;
                    DebugLog.Info($"[Column] RowBased 标记补齐（保留状态）: {e.DebugTag}, col={columnIndex}, curRow={row}, targetRow={newRow}, state={e.state} isAttackAnimating={e.isAttackAnimating}");
                }
                else
                {
                    e.ResetMovementState();
                    if (!(e.isBoss && e.bossState == BossState.Approaching))
                        e.pendingRushMove = true;
                    DebugLog.Info($"[Column] RowBased 标记补齐: {e.DebugTag}, col={columnIndex}, curRow={row}, targetRow={newRow}");
                }
            }
        }

        // 第二遍：从列表中移除 Dead 和 clearRow 的敌人
        int writeIdx = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy e = enemies[i];
            int row = e.rowIndex;
            bool isClearRow = row < clearRows.Length && clearRows[row];
            if (e.state == EnemyState.Dead || isClearRow)
                continue;

            if (i != writeIdx) enemies[writeIdx] = e;
            writeIdx++;
        }

        if (writeIdx < enemies.Count)
            enemies.RemoveRange(writeIdx, enemies.Count - writeIdx);

        StartRushMoveChain(columnIndex);
    }

    /// <summary>
    /// 触发补齐前移：将列中所有存活敌人向列表前方补齐。
    /// 用于波次生成后的初始前移——敌人 spawn 在靠后排，需要逐步前进到攻击位置。
    /// 逻辑与 RemoveEnemy 的存活敌人重排相同，但不移除任何敌人。
    /// </summary>
    public void TriggerFillForward()
    {
        if (enemies.Count == 0) return;

        int writeIdx = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy e = enemies[i];
            if (e.state == EnemyState.Dead) continue;
            if (i != writeIdx) enemies[writeIdx] = e;
            e.targetRow = writeIdx;
            e.ResetMovementState();
            e.pendingRushMove = true;
            writeIdx++;
        }

        if (writeIdx < enemies.Count)
            enemies.RemoveRange(writeIdx, enemies.Count - writeIdx);

        StartRushMoveChain(columnIndex);
    }

    /// <summary>
    /// 获取该列敌人总数
    /// </summary>
    public int EnemyCount => enemies.Count;

    /// <summary>
    /// 该列是否为空
    /// </summary>
    public bool IsEmpty => enemies.Count == 0;

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
    /// 逐列紧凑：将本列存活敌人向前紧凑，填补空排。
    /// Boss 所在排作为墙壁，身后敌人紧凑到 bossRow+1 及之后，不可越过 Boss。
    /// rangeStart/rangeEnd（含）分段紧凑：波区 [rangeStart, rangeEnd] 从 rangeStart 起排，
    /// 后方 [rangeEnd+1, ...] 从 rangeEnd+1 起排独立紧凑，互不跨越。默认 -1 表示全列。
    /// </summary>
    public void CompactColumn(int bossRow, int rangeStart = -1, int rangeEnd = -1)
    {
        if (enemies.Count == 0) return;

        var compacted = new List<Enemy>();
        int targetRow = 0;
        bool bossPlaced = false;
        bool hasRange = rangeStart >= 0 && rangeEnd >= 0;
        bool passedRangeEnd = false;

        if (hasRange) targetRow = rangeStart;

        // DEBUG: 打印入参和当前列状态
        DebugLog.Info($"[CompactColumn] col={columnIndex} bossRow={bossRow} range=[{rangeStart},{rangeEnd}] hasRange={hasRange} list=[{string.Join(",", enemies.ConvertAll(e => $"{e.name}@{e.rowIndex}"))}]");

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy e = enemies[i];
            if (e.state == EnemyState.Dead) continue;

            // 波区前方敌人保持原位
            if (hasRange && e.rowIndex < rangeStart)
            {
                DebugLog.Info($"[CompactColumn]   {e.name} row={e.rowIndex} → skip (before range)");
                compacted.Add(e);
                continue;
            }

            // 进入波区后方：切换为后方独立紧凑，重置 boss 墙状态
            if (hasRange && e.rowIndex > rangeEnd && !passedRangeEnd)
            {
                passedRangeEnd = true;
                targetRow = rangeEnd + 1;
                bossPlaced = false;
                DebugLog.Info($"[CompactColumn]   → passedRangeEnd, targetRow reset to {targetRow}");
            }

            // Boss 到达：固定在 bossRow，身后敌人从 bossRow+1 起排
            if (!bossPlaced && e.isBoss && bossRow >= 0)
            {
                bossPlaced = true;
                if (targetRow > bossRow)
                {
                    DebugLog.Warning($"[Column] CompactColumn: targetRow={targetRow} > bossRow={bossRow} in col={columnIndex}");
                }
                targetRow = bossRow;
            }

            e.targetRow = targetRow;
            DebugLog.Info($"[CompactColumn]   {e.name} row={e.rowIndex} → targetRow={targetRow} bossPlaced={bossPlaced} passedRangeEnd={passedRangeEnd}");

            if (e.rowIndex != targetRow)
            {
                e.ResetMovementState();
                // Boss 不参与列补齐链，移动由自身状态机控制
                if (!e.isBoss)
                    e.pendingRushMove = true;
            }

            compacted.Add(e);
            targetRow++;

            // Boss 身后敌人从 bossRow+1 起排
            if (bossPlaced && e.isBoss)
                targetRow = bossRow + 1;
        }

        enemies.Clear();
        enemies.AddRange(compacted);
        StartRushMoveChain(columnIndex);
    }

    #endregion
}
