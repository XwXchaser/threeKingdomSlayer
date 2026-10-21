using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人的**移动订单（RushMove）与世界坐标**：派单/校验/取消/完成订单、冻结前落位、
/// 以及按列排换算世界坐标。
///
/// 拆自 <c>Enemy</c>（2026-10-21，批次 3 刀 D）。职责边界：
///   • 订单语义（owner + generation 校验）集中在这里：谁派单、订单是否仍然有效、
///     何时算完成并回调补齐链；
///   • 「什么时候发起移动」（<c>TryStartRushMove</c>，含 BOSS 应战分支）**留在 Enemy** ——
///     它同时牵扯 BOSS 推进与攻击状态机，拆开会同时牵动两块边界；
///   • 状态字段仍留在 <c>Enemy</c>（可见性 internal），通过 <see cref="Enemy"/> 引用访问
///     （同 <c>EnemyFreeze</c> / <c>EnemyHitFeedback</c> / <c>EnemyBossPhases</c> 的既有做法）。
///
/// 规则依据：design/immutable-constraints.md 第 6 条（调度所有权：订单完成必须校验 owner + generation）
/// 与 design/percolumn-fillup-rules.md（补齐与位移的排位语义）。
/// </summary>
public sealed class EnemyMovement
{
    private readonly Enemy _owner;

    public EnemyMovement(Enemy owner)
    {
        _owner = owner;
    }

    // ───────────────────────── 订单派发与校验 ─────────────────────────

    public bool AssignRushMoveOrder(RushMoveOrderOwner owner, int generation, int orderTargetRow, RushMoveMode mode = RushMoveMode.Step)
    {
        if (owner == RushMoveOrderOwner.None || generation <= 0 || _owner.state == EnemyState.Dead)
            return false;
        if (_owner._cavalry != null && _owner._cavalry.IsSpecialMoveActive)
            _owner._cavalry.AbortForExternalOrder($"AssignRushMoveOrder:{owner}");

        if (_owner.isBoss && owner != RushMoveOrderOwner.Boss)
            return false;
        if (!_owner.isBoss && owner == RushMoveOrderOwner.Boss)
            return false;

        if (_owner.HasRushMoveOrder && !IsRushMoveOrder(owner, generation))
            return false;

        _owner._rushMoveOrderOwner = owner;
        _owner._rushMoveOrderGeneration = generation;
        _owner._rushMoveMode = mode;
        if (orderTargetRow < 0)
        {
            DebugLog.Warning($"[RowGuard] {_owner.DebugTag} 收到目标排<0 的订单 owner={owner} target={orderTargetRow} 当前row={_owner.rowIndex}，已夹到 0");
            orderTargetRow = 0;
        }
        _owner.targetRow = orderTargetRow;
        _owner.pendingRushMove = _owner.rowIndex > orderTargetRow;
        _owner.rushMoveDelayTimer = 0f;
        _owner.rushMoveChainTriggered = false;
        return true;
    }

    public bool IsRushMoveOrder(RushMoveOrderOwner owner, int generation)
    {
        return _owner._rushMoveOrderOwner == owner && _owner._rushMoveOrderGeneration == generation;
    }

    public void CancelRushMoveOrder(RushMoveOrderOwner owner, int generation, bool resetActiveMovement = false)
    {
        if (!IsRushMoveOrder(owner, generation)) return;

        if (resetActiveMovement && _owner.IsRushMovementActive)
            ResetRushMovementToCurrentRow();

        ClearRushMoveOrder();
    }

    public void CancelRushMoveOrder(bool resetActiveMovement = false)
    {
        if (!_owner.HasRushMoveOrder) return;

        if (resetActiveMovement && _owner.IsRushMovementActive)
            ResetRushMovementToCurrentRow();

        ClearRushMoveOrder();
    }

    private void ResetRushMovementToCurrentRow()
    {
        DOTween.Kill(_owner.transform, false);
        _owner.StopHitScaleFeedback();
        if (_owner._animator != null)
        {
            _owner._animator.speed = 1f;
            _owner._animator.ResetTrigger("Walk");
            _owner.PlayIdleVisual();
        }
        _owner.bounceYOffset = 0f;
        _owner.isMovingToNextRow = false;
        _owner.isRushMove = false;
        _owner.moveProgress = 0f;
        if (_owner.state == EnemyState.Moving)
            _owner.state = EnemyState.Idle;
        UpdateWorldPosition();
    }

    private void ClearRushMoveOrder()
    {
        _owner._rushMoveOrderOwner = RushMoveOrderOwner.None;
        _owner._rushMoveOrderGeneration = 0;
        _owner._rushMoveMode = RushMoveMode.Step;
        _owner.pendingRushMove = false;
        _owner.targetRow = -1;
        _owner.rushMoveDelayTimer = 0f;
        _owner.rushMoveChainTriggered = false;
    }

    public void CompleteRushMoveOrder()
    {
        if (!_owner.HasRushMoveOrder || _owner.rushMoveChainTriggered) return;

        RushMoveOrderOwner owner = _owner._rushMoveOrderOwner;
        int generation = _owner._rushMoveOrderGeneration;
        _owner.rushMoveChainTriggered = true;
        ClearRushMoveOrder();
        _owner.OnRushMoveComplete?.Invoke(_owner, owner, generation);
    }

    /// <summary>
    /// 冻结前落位：把在途的补齐移动立刻走完。
    /// 复用 UpdateMovement 的完整完成分支（行号推进 / 地刺检测 / 撞击回退 / CompleteRushMoveOrder 补齐链回调），
    /// 不另写一份，避免漏回调把整列补齐挂住（设计 §4.1 / P1c）。
    /// </summary>
    public void CompletePendingMovement()
    {
        if (_owner.state != EnemyState.Moving || !_owner.isMovingToNextRow) return;

        int fromRow = _owner.rowIndex;
        _owner.moveProgress = 1f;
        _owner.UpdateMovement();

        // 完成分支的个别早退路径（如 BOSS 推进暂停）会留下 Moving 空壳；清成 Idle，
        // 否则冻结会把 Moving 当作 _preFrozenState，解冻后 UpdateMovement 直接早退卡死。
        if (_owner.state == EnemyState.Moving && !_owner.isMovingToNextRow)
            _owner.state = EnemyState.Idle;

        DebugLog.Info($"[Freeze] {_owner.DebugTag} 冻结前落位：在途移动已完成 row {fromRow}→{_owner.rowIndex}");
    }

    // ───────────────────────── 世界坐标 ─────────────────────────

    /// <summary>
    /// 更新世界坐标
    /// 列：X轴使用梯形/扇形阵型偏移，排：Z轴偏移
    /// 移动过程中：从当前排(rowIndex)位置平滑移动到前一排(rowIndex-1)位置
    /// 非移动中：固定在当前排(rowIndex)位置
    ///
    /// BUG FIX: 移动过程中使用 rowIndex（当前排位置）计算阵型偏移，
    /// 因为 rowIndex 在移动过程中尚未更新（移动完成后才 rowIndex--）。
    /// 这样 X 轴和 Z 轴同步，避免"梯形向内聚拢"问题。
    /// </summary>
    public void UpdateWorldPosition()
    {
        float xPos;
        float rowSpacing = 2.5f;
        var stage = _owner.StageFormation;
        if (stage != null)
        {
            // BUG FIX: 移动过程中使用 rowIndex（旧排位置）计算 X 轴偏移
            // 因为 rowIndex 在移动过程中尚未更新（移动完成后才 rowIndex--）
            // 这样 X 轴和 Z 轴同步，避免"梯形向内聚拢"问题
            xPos = stage.GetFormationOffset(_owner.columnIndex, _owner.rowIndex);
            rowSpacing = stage.GetRowSpacing();
        }
        else
        {
            // 回退到原始直线排列
            xPos = (_owner.columnIndex - 2) * 2.0f;
        }

        // 获取阵型整体Z轴偏移（用于调整敌人生成位置远离/靠近摄像机）
        float offsetZ = 0f;
        if (stage != null)
        {
            offsetZ = stage.GetFormationOffsetZ();
        }

        float zPos;
        if (_owner.isMovingToNextRow)
        {
            // BUG FIX: 移动过程中同时平滑过渡 X 轴和 Z 轴。
            // 之前 X 轴仅使用 rowIndex（当前排），导致移动完成时 X 瞬间跳跃到目标排的偏移量。
            // 现在 X 和 Z 都从当前排(rowIndex)平滑过渡到目标排(rowIndex-1)，
            // 使阵型的梯形/扇形展开在移动过程中就正确显示，而非抵达后才调整。
            //
            // rowIndex 尚未更新，使用 rowIndex 作为起点，rowIndex-1 作为终点
            float currentX = xPos; // 当前排的 X 偏移（已在上面通过 GetFormationOffset 计算）
            float targetX = stage.GetFormationOffset(_owner.columnIndex, _owner.rowIndex - 1);
            xPos = Mathf.Lerp(currentX, targetX, _owner.moveProgress);

            // Z 轴：从当前排向目标排平滑过渡
            float currentRowZ = _owner.GetRowZ(_owner.rowIndex, rowSpacing, offsetZ);
            float targetRowZ = _owner.GetRowZ(_owner.rowIndex - 1, rowSpacing, offsetZ);
            zPos = Mathf.Lerp(currentRowZ, targetRowZ, _owner.moveProgress);
        }
        else
        {
            // 非移动中：固定在当前排位置
            zPos = _owner.GetRowZ(_owner.rowIndex, rowSpacing, offsetZ);
        }

        // 使用 localPosition 而非 position，这样敌人会相对于父节点定位
        // DOTween: 前进补齐时 Y 轴弹跳由 bounceYOffset 驱动（非补齐移动时 bounceYOffset = 0）
        _owner.transform.localPosition = new Vector3(xPos, _owner.bounceYOffset + _owner.EffectiveVisualYOffset, zPos);
    }
}
