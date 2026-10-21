using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


/// <summary>
/// Enemy 的**移动推进**：每帧的寻位与位移。
///
/// 按机制从 <c>Enemy.StateUpdate.cs</c> 拆出（2026-10-22，partial 分文件）。
/// partial class 与 Enemy.StateUpdate.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// </summary>

public partial class Enemy : MonoBehaviour
{
    internal void UpdateMovement()
    {
        // 教学手势拍：敌人站住不动（移动进度冻结，解除后从原进度继续）
        if (tutorialHold) return;
        if (!isMovingToNextRow) return;

        // 常规移动：基于 moveSpeed（秒/排）
        float speed = moveSpeed;
        moveProgress += Time.deltaTime / speed;

        if (moveProgress >= 1f)
        {
            bool wasRush = isRushMove;
            DebugLog.Info($"[Enemy] 移动完成: {DebugTag}, col={columnIndex}, oldRow={rowIndex}, newRow={rowIndex - 1}, isRush={wasRush}");
            moveProgress = 0f;
            isMovingToNextRow = false;
            isRushMove = false;

            // 移动结束切回 Idle（退出 Walk 动画）
            if (_animator != null)
            {
                _animator.speed = 1f;
                _animator.ResetTrigger("Walk");
            }
            PlayIdleVisual();
            rushMoveChainTriggered = false; // 重置链式触发标记

            // 移动完成：rowIndex 前进一排
            int oldRowForLog = rowIndex;
            rowIndex--;

            // 地刺检测：补齐到达新位置时触发
            SpikeTrapController.Instance?.CheckAndTrigger(this);
            if (state == EnemyState.Dead)
                return;

            // BUG FIX: 防止 rowIndex 变为负数
            if (rowIndex < 0) rowIndex = 0;
            if (rowIndex != oldRowForLog)
                DebugLog.Info($"[RowTrace] {DebugTag} row {oldRowForLog}→{rowIndex} | caller=UpdateMovement(moveComplete)");

            // Rush completion belongs to the exact scheduler order that started this step.
            if (wasRush)
            {
                var col = EnemyManager.Instance?.columnManager?.GetColumn(columnIndex);
                if (col != null)
                {
                    bool occupied = col.IsRowOccupied(rowIndex, this);
                    var manager = EnemyManager.Instance?.columnManager;
                    occupied |= manager != null && manager.IsCavalrySlotReserved(columnIndex, rowIndex, this);
                    if (_rushMoveMode == RushMoveMode.ContinuousEmptyRun)
                    {
                        if (_rushMoveOrderOwner == RushMoveOrderOwner.SpawnEntry)
                            occupied = manager != null && manager.IsCavalrySlotReserved(columnIndex, rowIndex, this);
                        else
                            occupied = manager == null || !manager.CanContinuousWaveEnterRow(this, columnIndex, rowIndex);
                    }
                    if (occupied)
                    {
                        rowIndex++;
                        state = EnemyState.Idle;
                        UpdateWorldPosition();
                        CompleteRushMoveOrder();
                        return;
                    }
                }
            }

            // 共享血量组：补齐移动后检查是否仍在同一排，不同排则解散
            // 但如果组内有成员正在移动中或等待补齐，跳过检查（rowIndex 仍在变动）
            if (sharedHealthGroup != null)
            {
                bool anyMemberInFlux = false;
                foreach (var m in sharedHealthGroup.members)
                {
                    if (m != this && m.state != EnemyState.Dead && (m.state == EnemyState.Moving || m.pendingRushMove))
                    {
                        anyMemberInFlux = true;
                        break;
                    }
                }
                if (!anyMemberInFlux)
                {
                    bool sameRow = true;
                    foreach (var m in sharedHealthGroup.members)
                    {
                        if (m != this && m.state != EnemyState.Dead && m.rowIndex != rowIndex)
                        {
                            sameRow = false;
                            break;
                        }
                    }
                    if (!sameRow)
                    {
                        DebugLog.Info($"[Enemy] 补齐移动导致解散共享血量组: {DebugTag}, row={rowIndex}");
                        sharedHealthGroup.Disband();
                    }
                }
            }

            // Boss 分阶段推进：到达第3排(rowIndex=2)，暂停等待前两排清空
            if (isBoss && rowIndex == 2 && bossState == BossState.None)
            {
                BossPause();
                // 先清理旧的补齐指令，再检查是否可以恢复推进。
                // BossResume 会分配新的 rush order，必须先清理旧 order 防止被误完成。
                if (HasRushMoveOrder)
                    CompleteRushMoveOrder();
                OnColumnsModifiedForBoss(); // 立即检查前两排是否已空，防止漏检
                return;
            }

            // 使用 attackRange 决定攻击距离
            int atkRange = (int)Mathf.Max(1, attackRange);
            bool reachedAttackRange = rowIndex < atkRange;

            if (wasRush)
            {
                if (!HasRushMoveOrder)
                {
                    state = EnemyState.Idle;
                }
                else if (targetRow >= 0 && rowIndex > targetRow)
                {
                    if (_rushMoveMode == RushMoveMode.ContinuousEmptyRun)
                    {
                        int nextRow = rowIndex - 1;
                        var columnManager = EnemyManager.Instance?.columnManager;
                        if (columnManager != null && !columnManager.CanAdvanceIntoRow(nextRow))
                        {
                            state = EnemyState.Idle;
                            CompleteRushMoveOrder();
                            return;
                        }
                    }

                    // Multi-row scheduler orders, including exact-slot push return, resume after the configured delay.
                    state = EnemyState.Idle;
                    pendingRushMove = true;
                    float delay = _rushMoveMode == RushMoveMode.ContinuousEmptyRun
                        ? 0f
                        : StageFormation?.GetRushMoveDelay() ?? 0f;
                    if (delay > 0f)
                        rushMoveDelayTimer = delay;
                    else
                        TryStartRushMove();
                }
                else
                {
                    if (reachedAttackRange)
                        StartAttacking();
                    else
                        state = EnemyState.Idle;

                    CompleteRushMoveOrder();

                    if (isBoss && rowIndex <= 1 && (bossState == BossState.Approaching || bossState == BossState.None))
                    {
                        var cm = EnemyManager.Instance?.columnManager;
                        if (cm != null && _onColumnsModifiedHandler != null)
                        {
                            cm.OnColumnsModified -= _onColumnsModifiedHandler;
                            _onColumnsModifiedHandler = null;
                        }
                        _bossEngageTimer = 1f;
                        DebugLog.Info($"[BOSS_ADVANCE] Boss到达应战排, 设置engageTimer=1s: {DebugTag}, col={columnIndex}, rowIndex={rowIndex}, bossState={bossState}");
                    }
                }
            }
            else
            {
                // 自然移动完成后，通知ColumnManager更新列内排序
                if (reachedAttackRange)
                {
                    StartAttacking();
                }
                else
                {
                    state = EnemyState.Idle;
                }
                EnemyManager.Instance?.OnEnemyMovedForward(this);
                DebugLog.Info($"[Enemy] 自然移动完成，触发 UpdateEnemyRow: {DebugTag}, col={columnIndex}");
            }
        }

        UpdateWorldPosition();
    }
}