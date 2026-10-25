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

    /// <summary>
    /// 缓存的 Transform **原始托管引用**。
    /// 销毁时用它 Kill Tween：Unity 销毁过程中 <c>transform</c> 已是「假 null」，
    /// <c>transform.DOKill(false)</c> 会静默失败 → Tween 继续访问已销毁的 Transform
    /// → MissingReferenceException（本项目规则见 memory/dotween-destroyed-transform.md）。
    /// </summary>
    private Transform _destroySafeTransform;

    private void OnDestroy()
    {
        SceneRegistry<Enemy>.Unregister(this);
        _attackTelegraph?.StopWarning();
        _attackTween?.Kill(false);
        _attackTween = null;
        // 用原始引用 Kill（目标已销毁时 DOKill 会因 Unity 假 null 直接跳过）
        DOTween.Kill(_destroySafeTransform, false);
        DestroyFlashMaterials();
    }

    private void OnDisable()
    {
        SceneRegistry<Enemy>.Unregister(this);
        _attackTelegraph?.StopWarning();
        EnemyManager.Instance?.columnManager?.CancelPushReturnForEnemy(this, "cancel-reset");
        StopHitStop();
        ClearFreezeOnTeardown();
        StopHitScaleFeedback();
        // 受击闪白协程：停用时协程会被 Unity 停掉，但**引用会变野**。
        // 而 UpdateAttack 用「协程还在不在」判断「受击动画是否在播」→ 野引用会让冷却分支每帧早退
        // → 敌人再也启动不了攻击（复现：被打多次后就不还手了）。这里必须清干净。
        if (_hitFlashRoutine != null)
        {
            StopCoroutine(_hitFlashRoutine);
            _hitFlashRoutine = null;
        }
        _hitStunAccum = 0f;
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
        // 同 OnDisable：受击闪白协程的野引用会锁死敌人的攻击循环（见 OnDisable 注释）
        if (_hitFlashRoutine != null)
        {
            StopCoroutine(_hitFlashRoutine);
            _hitFlashRoutine = null;
        }

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
