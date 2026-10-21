using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 列管理器
/// 管理5列敌人，提供添加/移除/查询接口
/// 实现补齐逻辑：当某列前排死亡，后排自动前移
/// </summary>
public class ColumnManager : MonoBehaviour
{

    /// <summary>
    /// 关卡/阵型信息（只读）。 历史上直接摸 StageController 门面，现只依赖角色接口
    /// （2026-10-20 批次 1）。延迟解析：本组件可能早于 StageController Awake。
    /// </summary>
    private IStageFormation _stageFormation;

    private IStageFormation StageFormation => _stageFormation ??= StageController.Instance;
    [Header("列配置")]
    public int columnCount = 5; // 默认5列

    // 5列敌人列表
    private Column[] columns;

    // GC 优化：范围查询复用列表

    // Wave march has a preparing barrier and one exact generation per row step.
    /// <summary>入场调度（新波次远处入场）。状态与流程见 SpawnEntryMover。</summary>
    private SpawnEntryMover _spawnEntry;

    private SpawnEntryMover SpawnEntry => _spawnEntry ??= new SpawnEntryMover(_queue, () => WaveMarch.StartPendingLogicalLayoutReflow());
    /// <summary>逻辑排队列（活人排 / 空排占位符 / 999 节奏门）。数据与队列操作见 LogicalRowQueue。</summary>
    private readonly LogicalRowQueue _queue = new LogicalRowQueue();

    /// <summary>波次行军（普通补齐）的状态、查询与流程。刀 4b 后流程也归位到调度器。</summary>
    private WaveMarchScheduler _waveMarch;

    private WaveMarchScheduler WaveMarch => _waveMarch ??= new WaveMarchScheduler(
        () => columns,
        IsCavalrySlotReserved,
        _queue,
        () => SpawnEntry.IsPreparing || SpawnEntry.HasPending,
        () => Displacement.HasPushReturnTransactions,
        (Enemy e) => Displacement.HasPushReturnTransaction(e),
        GetCavalryReservationOwner,
        () => CancelInvoke(nameof(StartWaveMarch)));

    /// <summary>只读查询层（战斗侧取数入口）。取列数组用委托，避免重建后拿到旧引用。</summary>
    private ColumnQueries _queries;

    private ColumnQueries Queries => _queries ??= new ColumnQueries(() => columns);

    /// <summary>位移类（精确击退回位 / 破冰顶开 / 击退 / 横向位移 / 聚拢）。实现见 DisplacementMover（刀 5）。</summary>
    private DisplacementMover _displacement;

    private DisplacementMover Displacement => _displacement ??= new DisplacementMover(
        () => columns,
        IsCavalrySlotReserved,
        () => StageFormation != null ? StageFormation.GetMaxVisibleRows() : 5,
        ReleaseCavalrySlot,
        ReleaseEnemyFromSchedulers,
        ReleaseEnemyFromMovementSchedulers,
        (bool preserveForPushReturn) => WaveMarch.AbortWaveMarch(preserveForPushReturn: preserveForPushReturn),
        () => WaveMarch.TryResumePausedWaveAfterPushReturns(),
        () => WaveMarch.ResetPausedState());

    // Cavalry owns only its next physical slot. Ordinary WaveMarch/SpawnEntry and push
    // must respect this reservation; the logical wave row still belongs to the same enemy.
    private readonly Dictionary<Enemy, long> _cavalryReservedByEnemy = new Dictionary<Enemy, long>();
    private readonly Dictionary<long, Enemy> _cavalryReservedSlots = new Dictionary<long, Enemy>();

    private static long SlotKey(int column, int row) => ((long)column << 32) ^ (uint)row;

    private Enemy GetCavalryReservationOwner(int column, int row)
    {
        _cavalryReservedSlots.TryGetValue(SlotKey(column, row), out var owner);
        return owner != null && owner.state != EnemyState.Dead ? owner : null;
    }

    public bool IsCavalrySlotReserved(int column, int row, Enemy exclude = null)
    {
        return _cavalryReservedSlots.TryGetValue(SlotKey(column, row), out var owner)
            && owner != null && owner != exclude && owner.state != EnemyState.Dead;
    }

    public bool CanCavalryEnterRow(Enemy enemy, int targetRow)
    {
        if (enemy == null || !IsValidColumn(enemy.columnIndex) || targetRow < 0 || !CanAdvanceIntoRow(targetRow)) return false;
        int bossRow = Displacement.GetBossRowInColumn(enemy.columnIndex);
        if (!enemy.isBoss && bossRow >= 0 && targetRow >= bossRow) return false;
        return !columns[enemy.columnIndex].IsRowOccupied(targetRow, enemy)
            && !IsCavalrySlotReserved(enemy.columnIndex, targetRow, enemy)
            && !IsContinuousWaveTargetReserved(enemy, enemy.columnIndex, targetRow);
    }

    public bool TryReserveCavalrySlot(Enemy enemy, int row)
    {
        if (enemy == null || !CanCavalryEnterRow(enemy, row)) return false;
        ReleaseCavalrySlot(enemy);
        long key = SlotKey(enemy.columnIndex, row);
        _cavalryReservedSlots[key] = enemy;
        _cavalryReservedByEnemy[enemy] = key;
        return true;
    }

    public void ReleaseCavalrySlot(Enemy enemy)
    {
        if (enemy == null || !_cavalryReservedByEnemy.TryGetValue(enemy, out long key)) return;
        _cavalryReservedByEnemy.Remove(enemy);
        if (_cavalryReservedSlots.TryGetValue(key, out var owner) && owner == enemy)
            _cavalryReservedSlots.Remove(key);
    }

    /// <summary>
    /// 列结构变化事件（RemoveEnemy / UpdateEnemyRow 后触发）
    /// Boss 用此事件检测前排是否清空以恢复推进
    /// </summary>
    public System.Action OnColumnsModified;

    private void Awake()
    {
        InitializeColumns();
    }

    private void Update()
    {
        Displacement.UpdatePushReturns();

        // 入场流程占用时，本帧不进入其它调度分支（与拆分前一致）
        if (SpawnEntry.Tick()) return;

        // 补齐批次的准备/放行推进归 WaveMarchScheduler（刀 4b）。
        WaveMarch.TickPreparing();
    }

    private void InitializeColumns()
    {
        columns = new Column[columnCount];
        for (int i = 0; i < columnCount; i++)
        {
            columns[i] = new Column(i);
        }
    }

    #region 添加敌人

    /// <summary>
    /// 向指定列添加敌人（追加到队列末尾）
    /// </summary>
    public void AddEnemyToColumn(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        columns[columnIndex].AddEnemy(enemy);
    }

    /// <summary>
    /// 批量向指定列添加敌人
    /// </summary>
    public void AddEnemiesToColumn(int columnIndex, List<Enemy> enemies)
    {
        if (!IsValidColumn(columnIndex)) return;

        foreach (var enemy in enemies)
        {
            columns[columnIndex].AddEnemy(enemy);
        }
    }

    /// <summary>
    /// 向所有列添加敌人（用于波次生成）
    /// 传入长度为5的列表，每个元素是对应列的敌人列表
    /// </summary>
    public void AddEnemiesToAllColumns(List<Enemy>[] columnEnemies)
    {
        for (int i = 0; i < columnCount && i < columnEnemies.Length; i++)
        {
            if (columnEnemies[i] != null)
            {
                AddEnemiesToColumn(i, columnEnemies[i]);
            }
        }
    }

    #endregion

    #region 移除敌人

    /// <summary>
    /// 从指定列移除敌人
    /// </summary>
    public void RemoveEnemyFromColumn(int columnIndex, Enemy enemy, bool skipChain = false)
    {
        if (!IsValidColumn(columnIndex)) return;

        LogicalWaveRow clearedLogicalRow = null;
        bool mayCreateWaveMarch = _queue.UseRowMarchPlanner
            && enemy != null
            && !enemy.isBoss
            && enemy.state == EnemyState.Dead
            && _queue.TryGetRowForEnemy(enemy, out clearedLogicalRow);

        columns[columnIndex].RemoveEnemy(enemy, skipChain: true);
        ReleaseCavalrySlot(enemy);
        ReleaseEnemyFromSchedulers(enemy, columnIndex);
        columns[columnIndex].ResumeRushMoveChain();

        if (mayCreateWaveMarch && !WaveMarch.HasLivingMembers(clearedLogicalRow))
            WaveMarch.RequestLogicalLayoutReflow(clearedLogicalRow);
        else if (!_queue.UseRowMarchPlanner && enemy != null && enemy.state == EnemyState.Dead)
            WaveMarch.StartWaveMarch();

        NotifyPushReturnTopologyChanged();
        OnColumnsModified?.Invoke();
    }

    /// <summary>
    /// 触发补齐前移（Boss死亡延迟补齐用）。
    /// 委托给 Column.TriggerFillForward，由调用方自行控制。
    /// </summary>
    public void TriggerFillForward(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return;
        columns[columnIndex].TriggerFillForward();
        OnColumnsModified?.Invoke();
    }

    /// <summary>
    /// 扫描所有列，触发 Boss 独立补齐（StartWaveMarch 跳过 Boss，需单独调用）。
    /// 波次生成后和选择完成后调用。
    /// </summary>
    public void TriggerAllBossFillForward()
    {
        for (int i = 0; i < columnCount; i++)
            columns[i].TriggerBossFillForward();
    }

    /// <summary>
    /// 清空所有列
    /// </summary>
    public void ClearAllColumns()
    {
        _cavalryReservedByEnemy.Clear();
        _cavalryReservedSlots.Clear();
        WaveMarch.AbortWaveMarch();
        Displacement.CancelAllPushReturns();
        ClearWaveRowLayout();
        WaveMarch.ResetPausedState();
        SpawnEntry.Reset();
        for (int i = 0; i < columnCount; i++)
            columns[i].enemies.Clear();
    }

    /// <summary>
    /// 清空指定列
    /// </summary>
    public void ClearColumn(int columnIndex)
    {
        if (!IsValidColumn(columnIndex)) return;
        for (int i = columns[columnIndex].enemies.Count - 1; i >= 0; i--)
        {
            ReleaseCavalrySlot(columns[columnIndex].enemies[i]);
            Displacement.CancelPushReturn(columns[columnIndex].enemies[i], "cancel-reset");
        }
        columns[columnIndex].enemies.Clear();
    }

    #endregion

    #region 更新敌人位置

    /// <summary>
    /// A normal movement/landing topology change only requests a wave rescan.
    /// </summary>
    public void UpdateEnemyRow(int columnIndex, Enemy enemy)
    {
        if (!IsValidColumn(columnIndex)) return;

        NotifyPushReturnTopologyChanged();
        OnColumnsModified?.Invoke();
        if (!_queue.UseRowMarchPlanner)
            WaveMarch.StartWaveMarch();
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 获取指定列
    /// </summary>
    public Column GetColumn(int columnIndex)
    {
        return Queries.GetColumn(columnIndex);
    }

    /// <summary>
    /// 获取指定列的最前排敌人
    /// </summary>
    public Enemy GetFrontEnemy(int columnIndex)
    {
        return Queries.GetFrontEnemy(columnIndex);
    }

    /// <summary>
    /// 获取指定列指定排的敌人
    /// </summary>
    public Enemy GetEnemyAt(int columnIndex, int rowIndex)
    {
        return Queries.GetEnemyAt(columnIndex, rowIndex);
    }

    /// <summary>
    /// 获取指定列的所有敌人
    /// </summary>
    public List<Enemy> GetEnemiesInColumn(int columnIndex)
    {
        return Queries.GetEnemiesInColumn(columnIndex);
    }

    /// <summary>
    /// Returns the living InCombat Boss whose horizontal footprint covers the requested column.
    /// Bosses remain stored in their center column; coverage only changes attack targeting.
    /// </summary>
    public Enemy GetCombatBossCoveringColumn(int columnIndex)
    {
        return Queries.GetCombatBossCoveringColumn(columnIndex);
    }

    /// <summary>
    /// 获取所有列的所有敌人（用于遍历攻击）
    /// </summary>
    public List<Enemy> GetAllEnemies()
    {
        return Queries.GetAllEnemies();
    }

    /// <summary>
    /// 获取指定列中 rowIndex 小于 rangeRows 的所有敌人（按排索引而非列表位置过滤）
    /// Boss 敌人始终包含在内，不受 rangeRows 限制
    /// </summary>
    public List<Enemy> GetEnemiesInRange(int columnIndex, int rangeRows)
    {
        return Queries.GetEnemiesInRange(columnIndex, rangeRows);
    }

    /// <summary>
    /// 获取所有列前N排的所有敌人（用于斩击/横扫等范围攻击）
    /// </summary>
    public List<Enemy> GetAllEnemiesInRange(int rangeRows)
    {
        return Queries.GetAllEnemiesInRange(rangeRows);
    }

    /// <summary>
    /// 获取所有列中 rowIndex 小于等于 maxRowIndex 的敌人（按排索引而非列表位置过滤）
    /// </summary>
    public List<Enemy> GetEnemiesByRowLimit(int maxRowIndex)
    {
        return Queries.GetEnemiesByRowLimit(maxRowIndex);
    }

    /// <summary>
    /// 指定列是否为空
    /// </summary>
    public bool IsColumnEmpty(int columnIndex)
    {
        return Queries.IsColumnEmpty(columnIndex);
    }

    /// <summary>
    /// 是否所有列为空
    /// </summary>
    public bool AreAllColumnsEmpty()
    {
        return Queries.AreAllColumnsEmpty();
    }

    /// <summary>
    /// 获取指定列的敌人数量
    /// </summary>
    public int GetColumnEnemyCount(int columnIndex)
    {
        return Queries.GetColumnEnemyCount(columnIndex);
    }

    /// <summary>
    /// 获取所有列的敌人总数
    /// </summary>
    public int GetTotalEnemyCount()
    {
        return Queries.GetTotalEnemyCount();
    }

    #endregion

    public void ConfigureWaveRowLayout(IList<RowConfig> rows, int rowOffset)
    {
        _queue.Configure(rows, rowOffset);
        SpawnEntry.Reset();
    }

    public void ClearWaveRowLayout()
    {
        _queue.Clear();
        SpawnEntry.Reset();
    }

    public bool IsRhythmGateRow(int row)
    {
        return _queue.IsRhythmGateRow(row);
    }

    /// <summary>Mounted cavalry retains its logical row membership, but only resumes ordinary marching after dismount.</summary>
    public void RequestCavalryDismountReflow(Enemy enemy)
    {
        if (enemy == null || !_queue.UseRowMarchPlanner || !_queue.HasRowFor(enemy)) return;
        _queue.MarkDirty();
        WaveMarch.StartPendingLogicalLayoutReflow();
    }

    public bool IsSpawnEntryPending(Enemy enemy) => SpawnEntry.IsPending(enemy);

    public bool IsCavalryReadyForCombat(Enemy enemy) => enemy != null &&
        !IsSpawnEntryPending(enemy) && !enemy.IsRushMoveOrder(RushMoveOrderOwner.SpawnEntry, enemy.RushMoveOrderGeneration);

    public void RegisterWaveEnemy(Enemy enemy, int configuredRow)
    {
        _queue.RegisterEnemy(enemy, configuredRow);
    }

    public void StartSpawnEntry(IList<Enemy> enemies)
    {
        SpawnEntry.Start(enemies);
    }

    public void UnregisterWaveEnemy(Enemy enemy)
    {
        _queue.UnregisterEnemy(enemy);
    }

    public bool IsRhythmGateOpen(int row)
    {
        return _queue.IsRhythmGateOpen(row);
    }

    public bool IsContinuousWaveTargetReserved(Enemy enemy, int column, int row)
    {
        return WaveMarch.IsTargetReserved(enemy, column, row);
    }

    public bool CanStartSpawnEntryOrder(Enemy enemy, int generation)
    {
        return SpawnEntry.CanStartOrder(enemy, generation);
    }

    public bool CanStartWaveMarchOrder(Enemy enemy, int generation)
    {
        return WaveMarch.CanStartOrder(enemy, generation);
    }

    public bool CanContinuousWaveEnterRow(Enemy enemy, int column, int row)
    {
        return WaveMarch.CanEnterRow(enemy, column, row);
    }

    public bool CanAdvanceIntoRow(int row)
    {
        return !IsRhythmGateRow(row) || IsRhythmGateOpen(row);
    }

    #region 波次行军（规则1/2）

    /// <summary>
    /// Cross-column wave march. Legacy non-layout stages may still request a topology scan.
    /// PerRow stages consume only logical layout changes caused by full row deaths.
    /// 流程实现见 WaveMarchScheduler（刀 4b）。
    /// </summary>
    public void StartWaveMarch()
    {
        WaveMarch.StartWaveMarch();
    }

    /// <summary>
    /// 检查指定排是否在所有列均无存活（非 Dead）的敌人。
    /// Launched 敌人仍占据排位——击飞不等于空出。
    /// </summary>
    public bool IsRowFullyVacated(int row)
    {
        return WaveMarch.IsRowFullyVacated(row);
    }

    private int GetMaxOccupiedRow()
    {
        return WaveMarch.GetMaxOccupiedRow();
    }

    /// <summary>
    /// 敌人被移出列 / 死亡时，从入场、补齐、击退三个调度器里摘除它。
    /// </summary>
    private void ReleaseEnemyFromSchedulers(Enemy enemy, int sourceColumn)
    {
        if (enemy == null) return;

        SpawnEntry.Remove(enemy);
        if (enemy.RushMoveOrderOwner == RushMoveOrderOwner.SpawnEntry)
            enemy.CancelRushMoveOrder(resetActiveMovement: true);

        if (!SpawnEntry.HasPending)
            WaveMarch.StartPendingLogicalLayoutReflow();

        ReleaseEnemyFromMovementSchedulers(enemy, sourceColumn);
        Displacement.CancelPushReturn(enemy, enemy.state == EnemyState.Dead ? "cancel-dead" : "cancel-removed");
    }

    /// <summary>
    /// 从补齐批次里摘除该敌人（并按需补发一次全局补齐扫描）。
    /// 实现见 WaveMarchScheduler.ReleaseWaveMember（刀 4b）。
    /// </summary>
    private void ReleaseEnemyFromMovementSchedulers(Enemy enemy, int sourceColumn)
    {
        WaveMarch.ReleaseWaveMember(enemy, sourceColumn);
    }

    #endregion

    #region 精确击退回位

    /// <summary>取消该敌人的精确回位事务（实现见 DisplacementMover，刀 5）。</summary>
    public void CancelPushReturnForEnemy(Enemy enemy, string reason = "cancel-reset")
    {
        Displacement.CancelPushReturnForEnemy(enemy, reason);
    }

    /// <summary>拓扑变化后让挂起的回位重新尝试（实现见 DisplacementMover，刀 5）。</summary>
    public void NotifyPushReturnTopologyChanged()
    {
        Displacement.NotifyPushReturnTopologyChanged();
    }

    #endregion

    #region 位移效果（击退/聚拢）

    /// <summary>
    /// 破冰推动（方案 A）：逐排向后推进到最远合法排，被占则停在阻挡者前一排。
    /// 实现见 DisplacementMover（刀 5）。
    /// </summary>
    public BreakIcePushResult ApplyBreakIcePush(Enemy frozen, int maxRows)
    {
        return Displacement.ApplyBreakIcePush(frozen, maxRows);
    }

    /// <summary>破冰后的延迟回位（实现见 DisplacementMover，刀 5）。</summary>
    public void RegisterBreakIceReturn(Enemy enemy, int originalColumn, int originalRow, int displacedRow, float delaySeconds)
    {
        Displacement.RegisterBreakIceReturn(enemy, originalColumn, originalRow, displacedRow, delaySeconds);
    }

    /// <summary>将敌人移动到目标列的指定排（实现见 DisplacementMover，刀 5）。</summary>
    public bool MoveEnemyToColumnAtRow(Enemy enemy, int targetCol, int targetRow)
    {
        return Displacement.MoveEnemyToColumnAtRow(enemy, targetCol, targetRow);
    }

    /// <summary>将敌人追加到目标列末尾（实现见 DisplacementMover，刀 5）。</summary>
    public void MoveEnemyToColumnEnd(Enemy enemy, int targetCol)
    {
        Displacement.MoveEnemyToColumnEnd(enemy, targetCol);
    }

    /// <summary>击退栈式阻塞检测（实现见 DisplacementMover，刀 5）。</summary>
    public bool CanPushColumn(int columnIndex, int pushAmount, HashSet<Enemy> hitEnemies)
    {
        return Displacement.CanPushColumn(columnIndex, pushAmount, hitEnemies);
    }

    /// <summary>对单列执行击退（实现见 DisplacementMover，刀 5）。</summary>
    public bool ExecutePush(int columnIndex, int pushAmount, List<Enemy> columnHitEnemies)
    {
        return Displacement.ExecutePush(columnIndex, pushAmount, columnHitEnemies);
    }

    /// <summary>击退波完整逻辑（实现见 DisplacementMover，刀 5）。</summary>
    public bool ApplyPushWave(List<Enemy> hitEnemies, int pushAmount, bool canInterruptCFrame = false, List<Enemy> pushedEnemies = null)
    {
        return Displacement.ApplyPushWave(hitEnemies, pushAmount, canInterruptCFrame, pushedEnemies);
    }

    /// <summary>方向推（Slash 专属）（实现见 DisplacementMover，刀 5）。</summary>
    public bool ApplyDirectionalPush(List<Enemy> hitEnemies, int step, bool pushRight, bool canInterruptCFrame = false, List<Enemy> movedEnemies = null)
    {
        return Displacement.ApplyDirectionalPush(hitEnemies, step, pushRight, canInterruptCFrame, movedEnemies);
    }

    /// <summary>聚拢波完整逻辑（实现见 DisplacementMover，刀 5）。</summary>
    public void ApplyConvergenceWave(List<Enemy> hitEnemies, int step, float convergenceDamagePercent, bool canInterruptCFrame = false)
    {
        Displacement.ApplyConvergenceWave(hitEnemies, step, convergenceDamagePercent, canInterruptCFrame);
    }

    #endregion

    /// <summary>
    /// Arms exact-slot returns for enemies that were actually pushed backward.
    /// Complete only after this same enemy reaches its exact first origin; no other enemy is moved.
    /// </summary>
    public void PostDisplacementFillUp(IEnumerable<Enemy> pushedEnemies)
    {
        Displacement.PostDisplacementFillUp(pushedEnemies);
    }

    /// <summary>
    /// Legacy no-argument entry retained for API compatibility. It cannot create return orders
    /// because exact origins are registered only by ExecutePush.
    /// </summary>
    public void PostDisplacementFillUp()
    {
        Displacement.PostDisplacementFillUp();
    }

    /// <summary>
    /// Legacy manual compaction entry retained for compatibility. It is intentionally inert;
    /// normal fill is owned exclusively by StartWaveMarch.
    /// </summary>

    /// <summary>
    /// 调试用：导出所有列中敌人的 (col, row, name)
    /// </summary>
    public string DumpColumns()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[DumpColumns]\n");
        for (int c = 0; c < columnCount; c++)
        {
            sb.Append($"  col={c}: ");
            var col = columns[c];
            if (col.enemies.Count == 0)
            {
                sb.Append("(empty)\n");
                continue;
            }
            for (int i = 0; i < col.enemies.Count; i++)
            {
                var e = col.enemies[i];
                sb.Append($"[{e.rowIndex}]{e.name}");
                if (i < col.enemies.Count - 1) sb.Append(", ");
            }
            sb.Append("\n");
        }
        return sb.ToString();
    }

    #region 工具方法

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < columnCount;
    }

    #endregion
}
