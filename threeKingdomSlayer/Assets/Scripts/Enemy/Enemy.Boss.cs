using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Enemy 的**BOSS 专项：推进到应战排 / 暂停等待 / 恢复推进。**
///
/// 按机制从 <c>Enemy.cs</c> 拆出（2026-10-21，批次 3 刀 C：partial 分文件）。
/// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// 阶段相关的多阶段编排已拆到 EnemyBossPhases；这里只保留「推进与补齐链」的耦合部分。
/// </summary>
public partial class Enemy : MonoBehaviour
{
    #region Boss 分阶段推进

    /// <summary>
    /// Boss 分阶段推进入口：根据当前 rowIndex 决定行为
    ///   rowIndex >= 2: 暂停等待前两排清空
    ///   rowIndex <= 1: 直接进入战斗
    /// 由 EnemyManager.RegisterEnemy 在 Boss 注册后调用
    /// </summary>
    public void StartBossPhaseAdvance()
    {
        if (!isBoss || bossState != BossState.None || state == EnemyState.Dead) return;

        // BOSS 已在最前排：直接应战
        if (rowIndex <= 1)
        {
            bossState = BossState.InCombat;
            DebugLog.Info($"[Enemy] Boss直接进入战斗(rowIndex={rowIndex}): {DebugTag}, col={columnIndex}");
            OnBossEngaged?.Invoke(this);
            return;
        }

        // BOSS 在后方：参与正常补齐链，TryStartRushMove 中的跨列检查保证
        // BOSS 仅在整排前方清空时才前进。到达 rowIndex=2 时触发 BossPause。
        DebugLog.Info($"[BOSS_ADVANCE] Boss加入补齐链(rowIndex={rowIndex}): {DebugTag}, col={columnIndex}, 等待TriggerBossFillForward");
    }

    /// <summary>
    /// BOSS 补齐检查：指定排（跨所有列）是否已无存活非BOSS敌人。
    /// 使用 enemy.rowIndex 而非列表位置判断排归属（PerRow 模式下列表位置不再反映真实排号）。
    /// </summary>
    private bool IsRowClearForBoss(int row)
    {
        var cm = EnemyManager.Instance?.columnManager;
        if (cm == null) return true;

        for (int c = 0; c < cm.columnCount; c++)
        {
            var col = cm.GetColumn(c);
            if (col == null) continue;
            foreach (var e in col.enemies)
            {
                if (e == null) continue;
                if (e == this) continue;
                if (e.rowIndex != row) continue;
                if (e.state != EnemyState.Dead)
                    return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 逐排补齐检查：前方一整排（跨所有列）是否已无存活敌人。
    /// 用于 PerRow 模式下非补齐移动的前置检查。
    /// </summary>
    private bool IsFrontRowClear()
    {
        if (rowIndex <= 0) return true;
        return IsRowClearForBoss(rowIndex - 1);
    }

    /// <summary>
    /// Boss 在第3排暂停：停止移动，等待前两排清空
    /// </summary>
    private void BossPause()
    {
        bossState = BossState.Approaching;
        state = EnemyState.Idle;
        CancelRushMoveOrder(resetActiveMovement: true);

        StopHitScaleFeedback();
        transform.DOKill(false);
        UpdateWorldPosition();
        transform.localScale = originalScale;

        // BUG FIX: 确保 Boss 在 Approaching 阶段可见
        // 强制启用 SpriteRenderer（某些路径可能导致 renderer 被禁用）
        if (renderers != null)
        {
            foreach (var r in renderers)
            {
                if (r != null) r.enabled = true;
            }
        }
        gameObject.SetActive(true);

        // 立即触发一次 UpdateAlpha 确保透明度正确
        // 防御：若 flashMaterials 意外为 null，直接通过 renderer 设置 alpha
        if (flashMaterials != null && flashMaterials.Length > 0)
        {
            UpdateAlpha();
        }
        else if (renderers != null)
        {
            float rowAlpha = GetAlphaForRow(rowIndex);
            foreach (var r in renderers)
            {
                if (r != null) r.material.color = new Color(1f, 1f, 1f, rowAlpha);
            }
        }

        // 订阅列修改事件，检测前两排是否清空
        var cm = EnemyManager.Instance?.columnManager;
        if (cm != null)
        {
            _onColumnsModifiedHandler = OnColumnsModifiedForBoss;
            cm.OnColumnsModified += _onColumnsModifiedHandler;
        }

        float alpha = GetAlphaForRow(rowIndex);
        DebugLog.Info($"[BOSS_ADVANCE] Boss暂停在第3排: {DebugTag}, col={columnIndex}, rowIndex={rowIndex}, alpha={alpha}, 等待前两排清空");
    }

    /// <summary>
    /// 列修改回调：Boss 分阶段推进的两种等待模式
    ///   1. BossState.Approaching（BossPause）：检测前两排(rowIndex 0,1)是否全部清空
    ///   2. BossState.None + pendingRushMove（补齐等待）：检测前方排是否已清空，重试 TryStartRushMove
    /// </summary>
    private void OnColumnsModifiedForBoss()
    {
        if (state == EnemyState.Dead) return;

        if (bossState == BossState.Approaching)
        {
            // 模式1：BossPause — 等待前两排清空
            var cm = EnemyManager.Instance?.columnManager;
            if (cm == null) return;

            for (int c = 0; c < cm.columnCount; c++)
            {
                var col = cm.GetColumn(c);
                if (col == null) continue;
                foreach (var e in col.enemies)
                {
                    if (e != null && e != this && e.state != EnemyState.Dead && e.rowIndex <= 1)
                    {
                        DebugLog.Info($"[BOSS_ADVANCE] Boss前两排仍有敌人，继续等待: {DebugTag}, blockingEnemy={e.DebugTag}, blockingRow={e.rowIndex}");
                        return;
                    }
                }
            }

            DebugLog.Info($"[BOSS_ADVANCE] Boss前两排已清空，调用BossResume: {DebugTag}");
            BossResume();
            return;
        }

        if (bossState == BossState.None && pendingRushMove && rowIndex > 2)
        {
            // 模式2：补齐等待 — 前方排已清空，重试 TryStartRushMove
            if (IsRowClearForBoss(rowIndex - 1))
            {
                var cm = EnemyManager.Instance?.columnManager;
                if (cm != null && _onColumnsModifiedHandler != null)
                {
                    cm.OnColumnsModified -= _onColumnsModifiedHandler;
                    _onColumnsModifiedHandler = null;
                }
                DebugLog.Info($"[Enemy] Boss前方排已清空，重试补齐: {DebugTag}, col={columnIndex}, row={rowIndex}");
                TryStartRushMove();
            }
        }
    }

    /// <summary>
    /// Boss 恢复推进：从第3排(rowIndex=2)补齐移动到第2排(rowIndex=1)
    /// </summary>
    private void BossResume()
    {
        // 取消订阅
        var cm = EnemyManager.Instance?.columnManager;
        if (cm != null && _onColumnsModifiedHandler != null)
        {
            cm.OnColumnsModified -= _onColumnsModifiedHandler;
            _onColumnsModifiedHandler = null;
        }

        DebugLog.Info($"[BOSS_ADVANCE] Boss恢复推进: {DebugTag}, col={columnIndex}, 从第3排→第2排");

        int generation = _rushMoveOrderGeneration + 1;
        CancelRushMoveOrder(resetActiveMovement: true);
        AssignRushMoveOrder(RushMoveOrderOwner.Boss, generation, 1);
        TryStartRushMove();
    }

    #endregion

    #region BOSS多阶段系统

    internal Coroutine _phaseTransitionRoutine;

    #endregion
}
