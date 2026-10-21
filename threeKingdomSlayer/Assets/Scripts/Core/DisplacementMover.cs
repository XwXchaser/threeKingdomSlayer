using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 破冰顶开的结果（刀 5 从 <c>ColumnManager.BreakIcePushResult</c> 提为顶层类型）。
/// </summary>
public struct BreakIcePushResult
{
    public bool moved;
    public Enemy blockedBy;
    public int fromRow;
    public int toRow;
}

/// <summary>
/// 位移类：**精确击退回位** + **位移效果**（破冰顶开 / 击退 / 横向位移 / 聚拢）。
///
/// 拆自 <c>ColumnManager</c>（2026-10-20，批次 2 刀 5）。职责边界：
///   • 本类持有**位移相关的全部状态**：回位事务表、回位 generation/orderId、回位抑制标志，
///     以及击退 / 横推 / 聚拢用的复用容器（避免每帧 new 分配导致 GC）；
///   • 规则（design/immutable-constraints.md）：
///     击退**只移动被击退者**、必须回**精确原槽**；横向位移**只改 column**、不发布行拓扑变化；
///     聚拢用中心槽位裁决；BOSS 不参与击退，且其所在排是「墙」；
///   • 依赖宿主（<c>ColumnManager</c>）的部分通过**构造委托**注入：列数组、骑兵槽位、
///     可见排数、释放敌人/槽位、中止与恢复补齐。
///
/// 事件订阅（<c>OnRushMoveComplete</c>）与退订都在本类内部成对完成，
/// 避免宿主侧退订不掉（委托目标变了）。
/// </summary>
public sealed partial class DisplacementMover
{
    // ── 构造注入的宿主能力 ──
    private readonly Func<Column[]> _columnsProvider;
    private readonly Func<int, int, Enemy, bool> _isCavalrySlotReserved;
    private readonly Func<int> _getMaxVisibleRows;
    private readonly Action<Enemy> _releaseCavalrySlot;
    private readonly Action<Enemy, int> _releaseEnemyFromSchedulers;
    private readonly Action<Enemy, int> _releaseEnemyFromMovementSchedulers;
    private readonly Action<bool> _abortWaveMarch;
    private readonly Action _resumePausedWaveAfterPushReturns;
    private readonly Action _resetPausedWaveState;

    /// <summary>一次「被击退 → 回精确原槽」的事务。</summary>
    private sealed class PushReturnTransaction
    {
        public Enemy enemy;
        public int originalColumn;
        public int originalRow;
        public int displacedColumn;
        public int displacedRow;
        public int generation;
        public int orderId;
        public float returnDueTime;
        public bool scheduled;
        public int lastBlockedRow = int.MinValue;
    }

    private const float pushReturnDelay = 0.35f;
    private bool _suppressPushReturnResume;
    private readonly Dictionary<Enemy, PushReturnTransaction> _pushReturnTransactions = new Dictionary<Enemy, PushReturnTransaction>();
    private readonly List<PushReturnTransaction> _pushReturnWorkList = new List<PushReturnTransaction>();
    private int _pushReturnGeneration;
    private int _pushReturnOrderId;

    // 位移复用容器（避免每帧 new 分配导致 GC）
    private readonly List<Enemy> _pushWorkList = new List<Enemy>();
    private readonly HashSet<Enemy> _pushHitSet = new HashSet<Enemy>();
    private readonly Dictionary<int, List<Enemy>> _pushByColumn = new Dictionary<int, List<Enemy>>();
    private readonly Dictionary<int, List<Enemy>> _rowEnemies = new Dictionary<int, List<Enemy>>();

    public DisplacementMover(
        Func<Column[]> columnsProvider,
        Func<int, int, Enemy, bool> isCavalrySlotReserved,
        Func<int> getMaxVisibleRows,
        Action<Enemy> releaseCavalrySlot,
        Action<Enemy, int> releaseEnemyFromSchedulers,
        Action<Enemy, int> releaseEnemyFromMovementSchedulers,
        Action<bool> abortWaveMarch,
        Action resumePausedWaveAfterPushReturns,
        Action resetPausedWaveState)
    {
        _columnsProvider = columnsProvider;
        _isCavalrySlotReserved = isCavalrySlotReserved;
        _getMaxVisibleRows = getMaxVisibleRows;
        _releaseCavalrySlot = releaseCavalrySlot;
        _releaseEnemyFromSchedulers = releaseEnemyFromSchedulers;
        _releaseEnemyFromMovementSchedulers = releaseEnemyFromMovementSchedulers;
        _abortWaveMarch = abortWaveMarch;
        _resumePausedWaveAfterPushReturns = resumePausedWaveAfterPushReturns;
        _resetPausedWaveState = resetPausedWaveState;
    }

    private Column[] Columns => _columnsProvider != null ? _columnsProvider() : null;

    private int ColumnCount => Columns != null ? Columns.Length : 0;

    private bool IsValidColumn(int columnIndex)
    {
        return columnIndex >= 0 && columnIndex < ColumnCount;
    }

    // ───────────────────────── 精确击退回位 ─────────────────────────

    /// <summary>
    /// Exact-slot backward-push returns. Only registered pushed enemies are considered;
    /// no scan may create a return for an unrelated enemy.
    /// </summary>
    private int NextPushReturnGeneration()
    {
        _pushReturnGeneration++;
        if (_pushReturnGeneration <= 0) _pushReturnGeneration = 1;
        return _pushReturnGeneration;
    }

    private int NextPushReturnOrderId()
    {
        _pushReturnOrderId++;
        if (_pushReturnOrderId <= 0) _pushReturnOrderId = 1;
        return _pushReturnOrderId;
    }


    // ───────────────────────── 位移效果 ─────────────────────────

    /// <summary>
    /// 破冰推动（方案 A）：逐排向后推进到最远合法排，被占则停在阻挡者前一排。
    /// 不在这里登记回位，回位由调用方（Enemy）在破冰动画结束时用 RegisterBreakIceReturn 登记。
    /// </summary>

    /// <summary>
    /// 聚拢波完整逻辑：将击中敌人向 col=2 移动 step 列。
    /// 冲突裁决：仅多敌人争同一位置时 → 各承受 convergenceDamagePercent% HP 伤害 → 重新分配到 col=1/col=3 末尾。
    /// 按行分组，从 col=2 向外分配槽位，始终朝中心聚拢不越界。
    /// convergenceDamagePercent 保留签名兼容，新算法无冲突故不施加伤害。
    /// </summary>
}
