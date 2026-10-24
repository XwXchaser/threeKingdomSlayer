using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// QTEController 的**状态切换**：QTE 状态机的进入 / 退出 / 推进。
    ///
    /// 按机制从 <c>QTEController.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 QTEController.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class QTEController : MonoBehaviour
{
    #region 状态切换

    /// <summary>
    /// 切换 QTE 数据（用于 BOSS 转阶段时更换 QTE 配置）
    /// 重置攻击索引，回到 Idle 状态
    /// </summary>
    public void SwitchQteData(BossQTEData newData)
    {
        if (IsQTEActive && enemy != null && enemy.state == EnemyState.QTEAttacking)
            AbortQTE();
        else
            CleanupInactiveQTE();

        qteData = newData;
        _currentAttackIndex = 0;
        DebugLog.Info($"[QTEController] 切换QTE数据: {newData?.name}, state={_state}");
    }

    private void CleanupInactiveQTE()
    {
        UnregisterQTEActivity();
        _qteGeneration++;
        SetStrictInputLock(false);
        KillProjectileSequence();
        if (_activeProjectile != null)
        {
            Destroy(_activeProjectile);
            _activeProjectile = null;
        }
        ClearAllArrowWaves();
        if (_animator != null) _animator.speed = 1f;
        if (qteDisplay != null) qteDisplay.ClearAllIndicators();
        _activeQTEs.Clear();
        _currentAttack = null;
        _currentQTEIndex = 0;
        _qtePhaseStarted = false;
        _state = QTEState.Idle;
    }

    /// <summary>
    /// 触发下一轮 QTE 攻击（由 Enemy 的 Idle 调度调用）
    /// 返回 false 表示当前无法触发（QTE 序列已耗尽、敌人在眩晕/击飞中等）
    /// </summary>
    public bool TriggerQTEAttack()
    {
        DebugLog.Info($"[QTEController] TriggerQTEAttack: attackIndex={_currentAttackIndex}, totalAttacks={qteData?.qteAttacks?.Count}");
        if (qteData == null || qteData.qteAttacks.Count == 0) return false;

        // 眩晕/击飞期间禁止触发QTE
        if (enemy.state == EnemyState.Stunned || enemy.state == EnemyState.Launched)
        {
            DebugLog.Info($"[QTEController] 敌人在眩晕/击飞状态({enemy.state})，跳过QTE");
            return false;
        }

        // QTE 序列已耗尽（非循环模式）
        if (_currentAttackIndex >= qteData.qteAttacks.Count)
        {
            DebugLog.Info("[QTEController] QTE序列已耗尽");
            return false;
        }

        _currentAttack = qteData.qteAttacks[_currentAttackIndex];
        _completionStarted = false;
        _qteGeneration++;
        DebugLog.Info($"[QTEController] 当前攻击: {_currentAttack?.name}, slots={_currentAttack?.qteSlots?.Count}, useMultiPhase={_currentAttack?.UseMultiPhaseAnimation}, useBranched={_currentAttack?.UseBranchedAnimation}");
        _state = QTEState.PerformingQTEAttack;
        RegisterQTEActivity();
        _performingTimer = 0f;
        _qtePhaseStarted = false;
        _qtePhaseTimer = 0f;

        // 清除上一轮可能残留的指示器（防御性清理，防止重叠）
        DebugLog.Info($"[QTE_DIAG] TriggerQTEAttack: clearing indicators before new attack, attackIndex={_currentAttackIndex}");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        _activeQTEs.Clear();
        _currentQTEIndex = 0;
        _nextQTEStartTime = 0f;

        // 创建 QTE 实例（顺序队列；delay 表示上一段结束后到下一段出现的间隔）
        foreach (var slot in _currentAttack.qteSlots)
        {
            if (slot.config == null) continue;
            _activeQTEs.Add(new QTEInstance
            {
                config = slot.config,
                spawnTime = slot.delay,
                warningEndTime = 0f,
                judgeEndTime = 0f,
                resolved = false,
                success = false
            });
        }

        // 清理上一轮残余箭矢波
        ClearAllArrowWaves();
        _arrowWavesSpawned = false;
        _fixedEndTimer = -1f;
        _failureDamagePending = false;
        _judgingSpeedApplied = false;  // Branched 慢放标记重置

        // 顺序单槽模式不再预先计算三个并列slot的总时长；每段按自身警示/判定结束。
        _effectiveJudgeDuration = 0f;
        _endAnimTimer = -1f;

        enemy.EnterQTEAttack();
        StartQTEAnimation();

        // 防御型 QTE：在每个指示器生成时发射对应箭矢波。
        // 箭矢飞行覆盖“指示器入场 + judgeWindow”，避免在前摇期间提前飞完。
        if (!(_currentAttack.UseMultiPhaseAnimation && _currentAttack.isDefensiveQTE && _currentAttack.arrowPrefab != null))
            SpawnProjectile();

        OnQTETriggered?.Invoke();
        if (UsesStrictInputRule)
        {
            if (qteDisplay == null)
                qteDisplay = QTEDisplay.Instance;
            qteDisplay?.ShowStrictModePrompt();
        }
        SetStrictInputLock(true);
        return true;
    }

    private void SetStrictInputLock(bool locked)
    {
        if (_inputRule != QTEInputRule.Strict || _strictInputLockApplied == locked) return;
        _strictInputLockApplied = locked;
        if (locked)
            ComboManager.Instance?.Freeze();
        else
            ComboManager.Instance?.Resume();
        SetBuffDisplayLocked(locked);
    }

    private static void SetBuffDisplayLocked(bool locked)
    {
        // 经登记表统一施加（Buff 面板可能有多个）
        QteInputLockTargets.SetAllLocked(locked);
    }

    private void SpawnProjectile()
    {
        if (_currentAttack.projectilePrefab == null) return;

        _activeProjectile = Instantiate(_currentAttack.projectilePrefab);
        EnemyProjectileVisualPriority.Apply(_activeProjectile);
        _activeProjectile.transform.position = enemy.transform.position;

        int generation = _qteGeneration;
        var spawnedProjectile = _activeProjectile;
        var projectile = spawnedProjectile.GetComponent<QTEProjectile>();
        if (projectile != null)
        {
            Vector3 targetPos = GetProjectileTargetPosition();
            projectile.Initialize(_currentAttack.projectileFlightTime, targetPos, () => OnProjectileReachedTarget(generation));
        }
        else
        {
            // 无 QTEProjectile 组件的普通 prefab：直接飞过去
            Vector3 targetPos = GetProjectileTargetPosition();
            spawnedProjectile.transform.DOMove(targetPos, _currentAttack.projectileFlightTime)
                .SetEase(Ease.Linear)
                .SetTarget(spawnedProjectile.transform)
                .SetUpdate(UpdateType.Normal, false)
                .OnComplete(() =>
                {
                    OnProjectileReachedTarget(generation);
                    if (spawnedProjectile != null)
                    {
                        Destroy(spawnedProjectile);
                        if (_activeProjectile == spawnedProjectile)
                            _activeProjectile = null;
                    }
                });
        }
    }

    private Vector3 GetProjectileTargetPosition()
    {
        float offsetZ = StageFormation != null ? StageFormation.GetFormationOffsetZ() : 0f;
        float zPos = offsetZ + _currentAttack.projectileTargetZ;
        return new Vector3(0, 1.5f, zPos);
    }

    private void OnProjectileReachedTarget(int generation)
    {
        if (generation != _qteGeneration || !IsQTEActive) return;
        StartQTEPhase();
    }

    private void StartQTEPhase()
    {
        if (_qtePhaseStarted) return;
        _qtePhaseStarted = true;
        _qtePhaseTimer = 0f;
        _fixedEndTimer = _currentAttack != null && _currentAttack.fixedQteDuration > 0f
            ? _currentAttack.fixedQteDuration
            : -1f;
        _nextQTEStartTime = _currentQTEIndex < _activeQTEs.Count
            ? _activeQTEs[_currentQTEIndex].spawnTime
            : 0f;
    }

    private bool CanEndQTEPhase()
    {
        if (_fixedEndTimer >= 0f && _qtePhaseTimer < _fixedEndTimer)
            return false;

        if (_currentAttack == null || !_currentAttack.isDefensiveQTE)
            return true;

        foreach (var wave in _arrowWaves.Values)
        {
            for (int i = 0; i < wave.Count; i++)
            {
                if (wave[i] != null)
                    return false;
            }
        }
        return _arrowLaunchDelays.Count == 0;
    }

    private void KillProjectileSequence()
    {
        if (_activeProjectile == null) return;
        // 备用弹道的 Tween 的 target 是 projectile 的 **Transform**（DOMove 的默认 target），
        // 只用 GameObject 引用 Kill 匹配不到 → Tween 会活下来访问已销毁的 Transform。
        // 两种引用都杀一遍（原始托管引用，目标已销毁时也安全）。
        DOTween.Kill(_activeProjectile.transform, false);
        DOTween.Kill(_activeProjectile, false);
    }



    #endregion
}