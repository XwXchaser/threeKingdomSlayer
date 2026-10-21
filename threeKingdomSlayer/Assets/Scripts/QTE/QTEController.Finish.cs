using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
    /// QTEController 的**收尾与表现**：Sweep 动画回调、格挡表现、QTE 攻击收尾。
    ///
    /// 按机制从 <c>QTEController.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 QTEController.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class QTEController : MonoBehaviour
{
    #region Sweep 动画回调

    public void OnSweepFailureHit()
    {
        if (_state != QTEState.QTEEnding) return;
        ApplyPendingFailureDamage();
    }

    private void ApplyPendingFailureDamage()
    {
        if (!_failureDamagePending) return;

        _failureDamagePending = false;
        float damage = _activeQTEs.Find(qte => qte.resolved && !qte.success)?.config.failureDamage ?? 0f;
        if (damage > 0f && PlayerState.Instance != null)
        {
            PlayerState.Instance.TakeDamage(damage);
            DebugLog.Info($"[QTEController] QTE失败伤害结算: {damage:F0}");
        }
    }

    /// <summary>
    /// 由 End2 AnimationEvent 回调，通知 QTE 结果动画播放完毕
    /// </summary>
    public void OnSweepResultAnimationEnd()
    {
        if (_state == QTEState.QTEEnding)
        {
            ApplyPendingFailureDamage();
            DebugLog.Info("[QTEController] OnSweepResultAnimationEnd → CompleteQTEAttack");
            CompleteQTEAttack();
        }
    }

    #endregion

    #region 格挡表现

    /// <summary>
    /// 玩家 Swipe QTE 输入识别后的固定倾角挥动反馈。
    /// </summary>
    private static readonly List<Enemy> EmptySwipeVisualTargets = new List<Enemy>(0);

    private void PlaySwipeInputVisual(QTEInstance qte)
    {
        if (qte == null || qte.config == null) return;

        var player = PlayerState.Instance;
        var slashConfig = player != null && player.heroConfig != null
            ? player.heroConfig.GetSkillConfig(AttackType.Slash)
            : null;
        if (player == null || slashConfig == null || slashConfig.attackWavePrefab == null) return;

        // SweepEffect 对 leftToRight 的视觉朝向与 QTE 图案的填充方向相反，故取反。
        bool leftToRight = Mathf.Abs(Mathf.DeltaAngle(qte.config.swipeDirection, 0f)) > 90f;
        const float indicatorTilt = 32.65f;
        float angleOffset = indicatorTilt;

        Vector3 playerPos = player.transform.position;
        Vector3 center = new Vector3(0f, playerPos.y + slashConfig.slashSpawnYOffset, playerPos.z + slashConfig.slashSpawnZOffset);

        // 完全走正式 Slash 的 SweepEffect 管线；空目标列表确保它只播放视觉。
        SweepEffect.Create(center, slashConfig.damageType, 0f, EmptySwipeVisualTargets, leftToRight,
            slashConfig.slashSweepHalfWidth, slashConfig.slashSweepAngle, slashConfig.slashSweepDuration,
            prefab: slashConfig.attackWavePrefab,
            rotateSprite1: stabBlockRotateSprite1,
            rotateSprite2: stabBlockRotateSprite2,
            angleOffset: angleOffset,
            movementTilt: indicatorTilt,
            additionalWeaponRotation: 20f);
    }

    /// <summary>
    /// 播放 QTE 格挡成功表现：矛从下方弧形扫入屏幕中央，同时自转。
    /// Pivot 在摄像机中心负责公转，Spear 偏移在 Pivot 下负责自转。
    /// </summary>
    private void PlayBlockVisual()
    {
        if (stabBlockEffectPrefab == null) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Pivot：挂在摄像机中心，负责公转（绕摄像机 X 轴从下方扫入）
        var pivot = new GameObject("QTE_BlockVFX_Pivot");
        pivot.transform.SetParent(cam.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 0f, stabBlockDistance);
        pivot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        // Spear：偏移在 Pivot 下，stabBlockOffsetY 映射到 localZ（Pivot 旋转后将变为摄像机 Y 偏移）
        var visual = Instantiate(stabBlockEffectPrefab, pivot.transform);
        visual.transform.localPosition = new Vector3(stabBlockOffsetX, 0f, -stabBlockOffsetY);
        visual.transform.localScale = stabBlockScale;
        visual.transform.localRotation = Quaternion.identity;
        foreach (var collider in visual.GetComponentsInChildren<Collider>()) Destroy(collider);
        var meshVisual = visual.GetComponent<MeshWeaponVisual>();
        meshVisual?.SetSorting(2);

        var sr = visual.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) sr = visual.GetComponent<SpriteRenderer>();
        if (sr != null && stabBlockSprite != null)
            sr.sprite = stabBlockSprite;

        float duration = stabBlockDuration;

        // 公转：Pivot X 90° → 0°，Spear 从下方弧形扫入摄像机中心
        pivot.transform.DOLocalRotate(new Vector3(0f, 0f, 0f), duration).SetEase(Ease.InOutQuad).SetUpdate(UpdateType.Normal, false);

        // 自转：Spear Z 0° → 900°
        visual.transform.DOLocalRotate(new Vector3(0f, 0f, 900f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetUpdate(UpdateType.Normal, false);

        // 三帧精灵均匀切换
        if (sr != null && stabBlockRotateSprite1 != null && stabBlockRotateSprite2 != null)
        {
            float third = duration / 3f;
            var spriteSeq = DOTween.Sequence().SetUpdate(UpdateType.Normal, false);
            spriteSeq.SetTarget(visual);
            spriteSeq.AppendInterval(third);
            spriteSeq.AppendCallback(() => { if (sr != null) sr.sprite = stabBlockRotateSprite1; });
            spriteSeq.AppendInterval(third);
            spriteSeq.AppendCallback(() => { if (sr != null) sr.sprite = stabBlockRotateSprite2; });
        }

        // 末尾20% fadeout
        if (meshVisual != null)
        {
            var fadeSequence = DOTween.Sequence().SetTarget(visual).SetUpdate(UpdateType.Normal, false);
            fadeSequence.AppendInterval(duration * 0.8f);
            fadeSequence.Append(DOTween.To(() => meshVisual.Alpha, meshVisual.SetAlpha, 0f, duration * 0.2f));
        }
        else if (sr != null)
        {
            DOVirtual.DelayedCall(duration * 0.8f, () =>
            {
                if (sr != null) sr.DOFade(0f, duration * 0.2f).SetUpdate(UpdateType.Normal, false);
            }).SetUpdate(UpdateType.Normal, false);
        }

        // 动画结束后销毁
        DOVirtual.DelayedCall(duration + 0.05f, () =>
        {
            if (pivot != null) Destroy(pivot);
        }).SetUpdate(UpdateType.Normal, false);
    }

    #endregion

    #region QTE 攻击收尾

    /// <summary>
    /// 中止 QTE（敌人被打入 Stun/Launch/Dead 等非 QTEAttacking 状态）
    /// </summary>
    public void AbortQTE()
    {
        UnregisterQTEActivity();
        _qteGeneration++;
        // 清理飞行物
        if (_activeProjectile != null)
        {
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (proj != null) proj.ContinuePassThrough(0.5f, null);
            else Destroy(_activeProjectile);
            _activeProjectile = null;
        }

        // 清理箭矢波
        ClearAllArrowWaves();

        StopQTEAnimation();

        DebugLog.Info("[QTE_DIAG] AbortQTE: clearing indicators");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        _activeQTEs.Clear();
        _state = QTEState.Idle;
        SetStrictInputLock(false);

        // BUG FIX: AbortQTE 需要确保 BOSS 设置冷却并回到 Idle，否则下一帧立即重新触发 QTE
        enemy.ExitQTEAttack();

        OnQTEAttackFinished?.Invoke();
        DebugLog.Info("[QTEController] QTE已中止");
    }

    private void CompleteQTEAttack()
    {
        if (_completionStarted)
        {
            Debug.Log($"[QTE_FLIP_DIAG] CompleteQTEAttack ignored duplicate state={_state}");
            return;
        }

        _completionStarted = true;
        UnregisterQTEActivity();
        ApplyPendingFailureDamage();
        SetStrictInputLock(false);
        _state = QTEState.QTECompleted;

        bool isDefensive = _currentAttack != null && _currentAttack.isDefensiveQTE;

        // 非防御型 QTE 的失败伤害已在 ResolveQTE 失败瞬间结算；收尾仅处理飞行物。
        if (!isDefensive && _activeProjectile != null)
        {
            bool anyFailed = _activeQTEs.Exists(qte => qte.resolved && !qte.success);
            var proj = _activeProjectile.GetComponent<QTEProjectile>();
            if (anyFailed && proj != null)
                proj.ContinuePassThrough(0.8f, null);
            else if (proj != null)
                proj.DestroyOnSuccess();
            else
                Destroy(_activeProjectile);
            _activeProjectile = null;
        }

        // 清理残余箭矢波（防御型 QTE：已 Deflect 的已完成，未到达的需强制销毁）
        ClearAllArrowWaves();

        // 通知敌人恢复
        enemy.ExitQTEAttack();

        // 停止 QTE 精灵动画
        StopQTEAnimation();

        // 清理指示器
        DebugLog.Info($"[QTE_DIAG] CompleteQTEAttack: clearing indicators, attackIndex={_currentAttackIndex}");
        if (qteDisplay != null)
            qteDisplay.ClearAllIndicators();

        // 前进到下一个 QTE 攻击
        _currentAttackIndex++;
        if (_currentAttackIndex >= qteData.qteAttacks.Count)
        {
            if (qteData.loopAttacks)
                _currentAttackIndex = 0;
            else
            {
                _state = QTEState.Idle;
                OnQTECompleted?.Invoke();
                OnQTEAttackFinished?.Invoke();
                return;
            }
        }

        _state = QTEState.Idle;
        OnQTECompleted?.Invoke();
        OnQTEAttackFinished?.Invoke();
    }

    #endregion
}