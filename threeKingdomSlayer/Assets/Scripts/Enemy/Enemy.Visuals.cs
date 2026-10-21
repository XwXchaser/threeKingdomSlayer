using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Enemy 的**表现层：世界坐标与排位换算、透明度（按排淡出）、描边（描边细节见 EnemyOutline）。**
///
/// 按机制从 <c>Enemy.cs</c> 拆出（2026-10-21，批次 3 刀 C：partial 分文件）。
/// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// 位置计算影响命中判定与补齐，改动前先确认行/列语义（design/percolumn-fillup-rules.md）。
/// </summary>
public partial class Enemy : MonoBehaviour
{
    #region 位置与透明度

    /// <summary>
    /// 重置移动状态（列内补齐时调用）
    /// 重置 state=Idle、isMovingToNextRow=false、moveProgress=0，
    /// 以便 StartMoving() 能通过 state==Moving 保护检查，重新开始移动。
    ///
    /// BUG FIX: 不再重置 pendingRushMove 标记，
    /// 由 Column.RemoveEnemy() 单独控制 pendingRushMove 的设置。
    /// </summary>
    public void ResetMovementState()
    {
        // 若敌人正在播放攻击动画，先清理 DOTween 序列并重置位置/缩放
        // 否则后续 StartMoving() 中的 DOTween.Kill(transform, false) 会直接中断攻击 tween，
        // 留下中间值（如 ScaleX=-0.2）导致视觉错误，且攻击动画与补齐移动并发
        if (_attackTween != null && _attackTween.IsActive())
        {
            _attackTween.Kill();
            _attackTween = null;
            UpdateWorldPosition();
            transform.localScale = originalScale;
        }
        isAttackAnimating = false;
        isAttackDrawPhase = false;

        state = EnemyState.Idle;
        isMovingToNextRow = false;
        moveProgress = 0f;
    }

    /// <summary>
    /// 击飞中的敌人静默补齐：直接更新 rowIndex 和 X/Z 位置，保留当前 Y（击飞高度）。
    /// 不播放 DOTween 弹跳动画，不触发补齐链。
    /// 由 Column 在 RemoveEnemy / CompactByClearRows / TriggerFillForward 中调用。
    /// </summary>
    public void SilentFillToTargetRow()
    {
        if (targetRow < 0 || rowIndex <= targetRow) return;

        int oldRow = rowIndex;
        rowIndex = targetRow;
        targetRow = -1;

        float xPos;
        float rowSpacing = 2.5f;
        float offsetZ = 0f;
        var stage = StageFormation;
        if (stage != null)
        {
            xPos = stage.GetFormationOffset(columnIndex, rowIndex);
            rowSpacing = stage.GetRowSpacing();
            offsetZ = stage.GetFormationOffsetZ();
        }
        else
        {
            xPos = (columnIndex - 2) * 2.0f;
        }

        float zPos = GetRowZ(rowIndex, rowSpacing, offsetZ);

        // 保留当前 Y：击飞高度由 UpdateLaunch() 管理，不重置
        float currentY = transform.localPosition.y;
        transform.localPosition = new Vector3(xPos, currentY, zPos);

        DebugLog.Info($"[Enemy] 击飞静默补齐: {DebugTag}, col={columnIndex}, row={oldRow}→{rowIndex}, pos=({xPos:F2},{currentY:F2},{zPos:F2})");
    }

    /// <summary>
    /// Boss 延迟补齐：等待 delay 秒后尝试开始前移。
    /// 由 Column.TriggerBossFillForward 调用。
    /// </summary>
    public void StartFillForwardDelay(float delay)
    {
        ResetMovementState();
        int savedTargetRow = targetRow;
        int generation = _rushMoveOrderGeneration + 1;
        CancelRushMoveOrder(resetActiveMovement: true);
        AssignRushMoveOrder(RushMoveOrderOwner.Boss, generation, savedTargetRow);
        StartCoroutine(FillForwardDelayRoutine(delay));
    }

    private System.Collections.IEnumerator FillForwardDelayRoutine(float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        if (state == EnemyState.Dead) yield break;
        TryStartRushMove();
    }

    public bool IsRushMovementActive => state == EnemyState.Moving && isMovingToNextRow && isRushMove;
    public bool HasRushMoveOrder => _rushMoveOrderOwner != RushMoveOrderOwner.None;
    /// <summary>仍在马上的骑兵：免疫眩晕/击退/位移，且不会发起普通攻击。</summary>
    public bool IsMountedCavalry => _cavalry != null && _cavalry.IsMounted;
    public RushMoveOrderOwner RushMoveOrderOwner => _rushMoveOrderOwner;
    public int RushMoveOrderGeneration => _rushMoveOrderGeneration;
    public bool IsRushMoveReady => HasRushMoveOrder
        && state != EnemyState.Dead
        && state != EnemyState.Launched
        && state != EnemyState.Stunned
        && state != EnemyState.QTEAttacking
        && !isAttackAnimating
        && !(state == EnemyState.Moving && !IsRushMovementActive)
        && rushMoveDelayTimer <= 0f;

    /// <summary>
    /// 尝试开始补齐移动（链式触发）
    /// 根据当前状态决定是否立即开始移动：
    ///   - Idle：直接开始移动（或攻击，若已到最前排）
    ///   - Attacking（冷却阶段）：中断冷却，优先补齐
    ///   - Attacking（动画阶段）：等待动画完成，由 UpdateAttack() 调用
    ///   - Stunned/Launched：等待恢复
    ///
    /// 返回值用于调度器判断：只有 Started / AlreadyRushing 会产生未来的 Rush 完成回调。
    /// </summary>
    public RushMoveStartResult TryStartRushMove()
    {
        if (!HasRushMoveOrder || !pendingRushMove || state == EnemyState.Dead || state == EnemyState.Frozen || IsInPostFreezeRecovery)
            return RushMoveStartResult.Rejected;
        if (_rushMoveOrderOwner == RushMoveOrderOwner.WaveMarch &&
            _cavalry != null && _cavalry.IsMounted)
            return RushMoveStartResult.Deferred;

        if (_rushMoveOrderOwner == RushMoveOrderOwner.WaveMarch)
        {
            var scheduler = EnemyManager.Instance?.columnManager;
            if (scheduler != null && !scheduler.CanStartWaveMarchOrder(this, _rushMoveOrderGeneration))
                return RushMoveStartResult.Deferred;
        }
        else if (_rushMoveOrderOwner == RushMoveOrderOwner.SpawnEntry)
        {
            var scheduler = EnemyManager.Instance?.columnManager;
            if (scheduler != null && !scheduler.CanStartSpawnEntryOrder(this, _rushMoveOrderGeneration))
                return RushMoveStartResult.Deferred;
        }

        switch (state)
        {
            case EnemyState.Idle:
                if (rushMoveDelayTimer > 0f)
                    return RushMoveStartResult.Deferred;

                // BOSS 已在应战排(row<=1)，不再参与补齐前移；若尚未进入战斗则立即应战
                if (isBoss && rowIndex <= 1)
                {
                    DebugLog.Info($"[BOSS_ADVANCE] Boss已在应战排(rowIndex={rowIndex}), 拒绝补齐: {DebugTag}, bossState={bossState}");
                    CompleteRushMoveOrder();
                    if (bossState == BossState.None)
                    {
                        bossState = BossState.InCombat;
                        DebugLog.Info($"[BOSS_ADVANCE] Boss应战(补齐拒绝路径): {DebugTag}, col={columnIndex}, row={rowIndex}");
                        OnBossEngaged?.Invoke(this);
                        SetBossActionCooldown();
                    }
                    return RushMoveStartResult.Rejected;
                }

                // BOSS 补齐规则：前方一整排（所有列）必须清空才前进，而非仅看本列
                if (isBoss && bossState == BossState.None && rowIndex > 2)
                {
                    var gateManager = EnemyManager.Instance?.columnManager;
                    if (gateManager != null && !gateManager.CanAdvanceIntoRow(rowIndex - 1))
                    {
                        if (_onColumnsModifiedHandler == null)
                        {
                            _onColumnsModifiedHandler = OnColumnsModifiedForBoss;
                            gateManager.OnColumnsModified += _onColumnsModifiedHandler;
                        }
                        return RushMoveStartResult.Deferred;
                    }

                    if (!IsRowClearForBoss(rowIndex - 1))
                    {
                        var cm = EnemyManager.Instance?.columnManager;
                        if (cm != null && _onColumnsModifiedHandler == null)
                        {
                            _onColumnsModifiedHandler = OnColumnsModifiedForBoss;
                            cm.OnColumnsModified += _onColumnsModifiedHandler;
                            DebugLog.Info($"[Enemy] Boss等待前方排清空(row={rowIndex - 1}): {DebugTag}, col={columnIndex}");
                        }
                        return RushMoveStartResult.Deferred;
                    }
                    if (_onColumnsModifiedHandler != null)
                    {
                        var cm2 = EnemyManager.Instance?.columnManager;
                        if (cm2 != null)
                            cm2.OnColumnsModified -= _onColumnsModifiedHandler;
                        _onColumnsModifiedHandler = null;
                    }
                }
                pendingRushMove = true;
                StartMoving(true);
                if (state != EnemyState.Moving)
                {
                    CompleteRushMoveOrder();
                    return RushMoveStartResult.Rejected;
                }
                return RushMoveStartResult.Started;

            case EnemyState.Attacking:
                if (!isAttackAnimating)
                {
                    ResetMovementState();
                    pendingRushMove = true;
                    StartMoving(true);
                    if (IsRushMovementActive)
                        return RushMoveStartResult.Started;
                    CompleteRushMoveOrder();
                    return RushMoveStartResult.Rejected;
                }
                return RushMoveStartResult.Deferred;

            case EnemyState.Launched:
                return RushMoveStartResult.Deferred;

            case EnemyState.Stunned:
                return RushMoveStartResult.Deferred;

            case EnemyState.Moving:
                return IsRushMovementActive
                    ? RushMoveStartResult.AlreadyRushing
                    : RushMoveStartResult.Deferred;

            default:
                return RushMoveStartResult.Deferred;
        }
    }

    // ── 移动订单与世界坐标：实现见 EnemyMovement（批次 3 刀 D），此处保留同名薄转发 ──

    public bool AssignRushMoveOrder(RushMoveOrderOwner owner, int generation, int orderTargetRow, RushMoveMode mode = RushMoveMode.Step)
        => Movement.AssignRushMoveOrder(owner, generation, orderTargetRow, mode);

    public bool IsRushMoveOrder(RushMoveOrderOwner owner, int generation) => Movement.IsRushMoveOrder(owner, generation);

    public void CancelRushMoveOrder(RushMoveOrderOwner owner, int generation, bool resetActiveMovement = false)
        => Movement.CancelRushMoveOrder(owner, generation, resetActiveMovement);

    public void CancelRushMoveOrder(bool resetActiveMovement = false) => Movement.CancelRushMoveOrder(resetActiveMovement);

    private void CompleteRushMoveOrder() => Movement.CompleteRushMoveOrder();

    public void CompletePendingMovement() => Movement.CompletePendingMovement();

    private void UpdateWorldPosition() => Movement.UpdateWorldPosition();

    /// <summary>
    /// 根据排索引计算Z轴位置
    /// 在Unity中，-Z轴是前进方向（朝向玩家），+Z轴是后退方向（远离玩家）。
    /// rowIndex=0在最前排（最靠近玩家），rowIndex越大越远离玩家。
    ///
    /// 公式：zPos = (maxVisibleRows - 1 - rowIndex) * (-rowSpacing) + offsetZ
    /// </summary>
    public float GetRowZ(int row, float spacing, float offset)
    {
        int maxRow = 4; // 默认值
        var stage = StageFormation;
        if (stage != null)
        {
            maxRow = stage.GetMaxVisibleRows() - 1;
        }
        return (maxRow - row) * (-spacing) + offset;
    }

    /// <summary>
    /// 设置列和排索引
    /// </summary>
    public void SetColumnPosition(int col, int row)
    {
        columnIndex = col;
        rowIndex = row;
        UpdateWorldPosition();
    }

    /// <summary>
    /// 仅设置排索引（列内前移时调用）
    /// </summary>
    public void SetRowIndex(int row)
    {
        if (row < 0)
        {
            DebugLog.Warning($"[RowGuard] {DebugTag} SetRowIndex 收到负数排 {row}（当前 {rowIndex}），已夹到 0\n{new System.Diagnostics.StackTrace(1, false)}");
            row = 0;
        }
        int oldRow = rowIndex;
        rowIndex = row;
        UpdateWorldPosition();
        // 追踪所有 rowIndex 变化，定位击退跳排根因
        var st = new System.Diagnostics.StackTrace(1, true);
        var frame = st.GetFrame(0);
        string caller = frame != null ? $"{frame.GetMethod().DeclaringType?.Name}.{frame.GetMethod().Name}:{frame.GetFileLineNumber()}" : "?";
        DebugLog.Info($"[RowTrace] {DebugTag} row {oldRow}→{row} | caller={caller}");
        if (_cavalry != null)
            _cavalry.TraceEnemyRowChange(oldRow, row, caller);
    }

    /// <summary>
    /// Re-evaluate attack state after displacement without scheduling movement.
    /// An active attack animation is allowed to finish; outside range, a normal enemy waits Idle for ColumnManager.
    /// </summary>
    public void RecheckAttackRange()
    {
        if (state == EnemyState.Dead) return;
        // 骑乘期间免疫位移：不会被击退/横移，因此不需要在这里取消冲锋。

        int atkRange = (int)Mathf.Max(1, attackRange);
        if (rowIndex < atkRange)
        {
            if (state == EnemyState.Idle)
                StartAttacking();
            return;
        }

        if (isBoss || isAttackAnimating || state == EnemyState.Launched || state == EnemyState.Stunned || state == EnemyState.QTEAttacking)
            return;

        if (state == EnemyState.Attacking)
        {
            state = EnemyState.Idle;
            PlayIdleVisual();
        }
    }

    /// <summary>
    /// 更新透明度（基于排索引）+ 受伤闪白效果
    /// 使用材质实例直接修改 color，确保闪白在对象禁用前始终可见。
    /// </summary>
    private void UpdateAlpha()
    {
        if (flashMaterials == null || flashMaterials.Length == 0) return;

        float alpha = GetAlphaForRow(rowIndex);

        foreach (var mat in flashMaterials)
        {
            if (mat == null) continue;

            // 使用白色作为基础色，仅通过透明度控制显示
            Color color = Color.white;
            color.a = alpha;

            // 受伤闪白效果：hitFlashTimer > 0 时设置全白全透明（alpha=1），无视排索引透明度
            if (hitFlashTimer > 0f)
            {
                color = Color.white; // (1, 1, 1, 1) — 全白全透明
            }

            mat.color = color;
        }

        _attackTelegraph?.SetParentAlpha(hitFlashTimer > 0f ? 1f : alpha);
    }

    /// <summary>
    /// 根据排索引获取透明度
    /// </summary>
    private float GetAlphaForRow(int row)
    {
        // 在攻击范围内的敌人始终全不透明，提高辨识度
        int atkRange = (int)Mathf.Max(1, attackRange);
        if (row < atkRange)
            return 1f;

        // 从StageController获取透明度配置
        // 默认：第0排=1.0, 第1排=0.8, 第2排=0.6, 第3排=0.4, 第4排=0.2, 第5排+=0
        float[] factors = StageFormation?.GetRowAlphaFactors()
                          ?? DefaultRowAlphaFactors;
        if (row < factors.Length)
            return factors[row];
        return 0f;
    }

    #endregion

    #region 描边系统

    public enum OutlineDebugMode { None, CAttack, SuperArmor, QTE }

    private bool _pierceHighlighted;
    private EnemyOutline _outline;

    public void SetPierceHighlight(bool highlighted)
    {
        if (_pierceHighlighted == highlighted)
            return;

        _pierceHighlighted = highlighted;
        UpdateOutlineState();
    }

    /// <summary>
    /// 把当前状态打包交给 <see cref="EnemyOutline"/>：描边的优先级与材质处理都在那边，
    /// Enemy 只提供事实（状态、覆盖色/宽度）。
    /// </summary>
    internal void UpdateOutlineState()
    {
        if (_spriteRenderer == null) return;
        if (_outline == null) _outline = new EnemyOutline();

        var facts = new EnemyOutline.OutlineFacts
        {
            PierceHighlighted = _pierceHighlighted,
            Dead = state == EnemyState.Dead,
            PhaseTransitioning = isPhaseTransitioning,
            QteAttacking = state == EnemyState.QTEAttacking,
            SuperArmor = isSuperArmor,
            CFrameAttacking = isCFrame && state == EnemyState.Attacking,
            DebugMode = debugForceOutline,
            CColor = outlineColorCOverride,
            CWidth = outlineWidthCOverride,
            SuperArmorColor = outlineColorSuperArmorOverride,
            SuperArmorWidth = outlineWidthSuperArmorOverride,
            QteColor = outlineColorQTEOverride,
            QteWidth = outlineWidthQTEOverride
        };
        _outline.Apply(_spriteRenderer, in facts);
    }

    #endregion
}
