using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Enemy 的**对象池：回收时的状态复位与资源清理。**
///
/// 按机制从 <c>Enemy.cs</c> 拆出（2026-10-21，批次 3 刀 C：partial 分文件）。
/// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// 复位必须覆盖挂起令牌、协程、材质实例，否则复用后残留。
/// </summary>
public partial class Enemy : MonoBehaviour
{
    #region 对象池

    private void OnDestroy()
    {
        _attackTelegraph?.StopWarning();
        _attackTween?.Kill(false);
        _attackTween = null;
        transform.DOKill(false);
        DestroyFlashMaterials();
    }

    private void OnDisable()
    {
        _attackTelegraph?.StopWarning();
        EnemyManager.Instance?.columnManager?.CancelPushReturnForEnemy(this, "cancel-reset");
        StopHitStop();
        ClearFreezeOnTeardown();
        StopHitScaleFeedback();
        initialized = false;
    }

    /// <summary>
    /// 重置状态（回收到对象池时调用）
    /// </summary>
    public void ResetEnemy()
    {
        EnemyManager.Instance?.columnManager?.CancelPushReturnForEnemy(this, "cancel-reset");
        _cavalry?.ResetCavalry();
        _shieldGuard?.ResetShieldGuard();
        StopHitStop();
        ClearFreezeOnTeardown();
        StopHitScaleFeedback();

        // 状态宿主：施加型状态（灼烧 / 疾病）不能跟着敌人进池复用，整体清空。
        // 常驻状态（冻结）会在下次访问宿主时自动补登记。
        _statusHost?.ClearAll();

        // 终止所有活跃的 DOTween 动画（完成当前值后跳转到最终值）
        transform.DOKill(true);
        transform.localScale = originalScale;
        transform.localRotation = originalRotation;

        state = EnemyState.Dead;
        formState = EnemyFormState.Normal;
        _cowardTransitionPlaying = false;
        currentHealth = 0f;
        currentPoise = 0f;
        initialized = false;
        isSuperArmor = false;
        isPhaseTransitioning = false;
        _pierceHighlighted = false;
        _healthLocked = false;
        _isStunExiting = false;
        _resumeStunAfterLaunch = false;
        _getupInterrupted = false;
        if (_getupRoutine != null) { StopCoroutine(_getupRoutine); _getupRoutine = null; }
        if (_stunExitRoutine != null) { StopCoroutine(_stunExitRoutine); _stunExitRoutine = null; }
        currentBossPhase = 0;
        if (_phaseTransitionRoutine != null) { StopCoroutine(_phaseTransitionRoutine); _phaseTransitionRoutine = null; }
        // 清理 Boss 分阶段推进订阅
        if (_onColumnsModifiedHandler != null)
        {
            var cm = EnemyManager.Instance?.columnManager;
            if (cm != null)
                cm.OnColumnsModified -= _onColumnsModifiedHandler;
            _onColumnsModifiedHandler = null;
        }
        // 清理 QTE 完成事件订阅
        if (isBoss)
        {
            var qte = GetQTEController();
            if (qte != null) qte.OnQTEAttackFinished -= OnQTEAttackFinished;
        }
        bossState = BossState.None;
        sharedHealthGroup = null;

        _rushMoveOrderOwner = RushMoveOrderOwner.None;
        _rushMoveOrderGeneration = 0;
        _rushMoveMode = RushMoveMode.Step;
        pendingRushMove = false;
        targetRow = -1;
        rushMoveDelayTimer = 0f;
        rushMoveChainTriggered = false;
        OnRushMoveComplete = null;
        OnDying = null;
        OnDeath = null;
        OnDamageTaken = null;
        OnHealthChanged = null;
        OnPoiseChanged = null;
        OnBossEngaged = null;
        OnBossPhaseChanged = null;
        ResetWaveTint();
        gameObject.SetActive(false);
    }

    #endregion
}
