using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 打击反馈：**卡肉（HitStop）** + **受击缩放** + **闪白动画/材质** + 挂起时的攻击位移暂停。
///
/// 拆自 <c>Enemy</c>（2026-10-21，批次 3 刀 A）。职责边界：
///   • 本类只放**反馈的表现逻辑**；状态字段仍留在 <c>Enemy</c>（可见性改为 internal），
///     这样 Enemy 内部上百处使用点一行都不用改；
///   • 依赖通过 <see cref="Enemy"/> 引用访问（同 <c>EnemyFreeze</c> / <c>EnemyOutline</c> 的既有做法），
///     不引入委托；
///   • Enemy 侧保留同名薄转发，调用方零改动。
///
/// 规则依据：design/immutable-constraints.md 第 5 条（攻击打断边界）
/// 与 memory/hit-feedback-system.md（首批打击反馈：局部 Animator 卡肉、命中来源/强度上下文）。
/// </summary>
public sealed class EnemyHitFeedback
{
    private readonly Enemy _owner;

    public EnemyHitFeedback(Enemy owner)
    {
        _owner = owner;
    }

    // ───────────────────────── 卡肉（HitStop） ─────────────────────────

    /// <summary>每帧推进卡肉计时；到期后解除挂起，恢复 Animator 与攻击位移。</summary>
    public void UpdateHitStop()
    {
        if (_owner._hitStopRemaining <= 0f) return;
        _owner._hitStopRemaining -= GameplayPause.UnscaledDelta;
        if (_owner._hitStopRemaining > 0f) return;

        _owner._hitStopRemaining = 0f;
        _owner.PopSuspend(Enemy.SuspendSource.HitStop);
        if (HitFeedbackManager.EnableDebugLogs)
            Debug.Log($"[HitFeedback] Resume enemy={_owner.DebugTag} frame={Time.frameCount}");
    }

    public void StopHitStop()
    {
        _owner._hitStopRemaining = 0f;
        _owner.PopSuspend(Enemy.SuspendSource.HitStop);
    }

    // ───────────────────────── 破防慢放（REQ-006 · A1） ─────────────────────────

    /// <summary>
    /// 破防时「攻击者被顶住 → 被打慢」的表现。
    ///
    /// 口径（<c>design/guard-break-presentation-overhaul.md</c> §3.2）：
    ///   ① 先**冻结** freezeSeconds（复用卡肉机制：Animator 停 + 攻击位移暂停）⇒ 读成「被顶住」；
    ///   ② 冻结结束后以 <c>Animator.speed = slowScale</c> **慢放** slowSeconds ⇒ 读成「被打慢」；
    ///   ③ 结束还原 <c>speed = 1</c>。
    ///
    /// ⚠ 只改 Animator 的速度，**不改攻击判定时钟**（判定仍走正常 deltaTime）
    ///   ⇒ 慢放期间会出现「动作慢、判定正常」的错拍；这是有意取舍（不动判定数值）。
    /// ⚠ 对象池复用会带脏值 ⇒ 还原必须在 <see cref="StopGuardBreakSlow"/> 做（OnDisable / ResetEnemy 都调）。
    /// </summary>
    public void PlayGuardBreakSlow(float freezeSeconds, float slowSeconds, float slowScale)
    {
        StopGuardBreakSlow();
        if (_owner == null || slowSeconds <= 0f) return;
        _owner._guardBreakSlowRoutine = _owner.StartCoroutine(
            GuardBreakSlowRoutine(freezeSeconds, slowSeconds, slowScale));
    }

    /// <summary>停止破防慢放并还原 Animator 速度（停用 / 回池时必须调用）。</summary>
    public void StopGuardBreakSlow()
    {
        if (_owner == null) return;
        if (_owner._guardBreakSlowRoutine != null)
        {
            _owner.StopCoroutine(_owner._guardBreakSlowRoutine);
            _owner._guardBreakSlowRoutine = null;
        }
        if (_owner._animator != null && _owner._animator.speed != 1f)
            _owner._animator.speed = 1f;
    }

    private System.Collections.IEnumerator GuardBreakSlowRoutine(float freezeSeconds, float slowSeconds, float slowScale)
    {
        // 等冻结结束（卡肉计时归零）：冻结期间 Animator 是 disabled ⇒ speed 无意义
        float wait = Mathf.Max(0f, freezeSeconds) + 0.25f;
        while (_owner != null && _owner._hitStopRemaining > 0f && wait > 0f)
        {
            wait -= GameplayPause.UnscaledDelta;
            yield return null;
        }
        if (_owner == null || _owner._animator == null) yield break;

        _owner._animator.speed = Mathf.Clamp(slowScale, 0.05f, 1f);
        float t = 0f;
        while (t < slowSeconds)
        {
            t += GameplayPause.UnscaledDelta;
            // ★ **每帧重申**：敌人自己的状态机会写 `animator.speed`
            //   （Attack 结束 Enemy.Attack.cs:334、转走路 Enemy.StateSwitch.cs:96、Movement、FormSwitch …）
            //   ⇒ 只在开始时写一次的话，慢放会被踩掉（实测只活了 ~0.07s）。
            //   协程在 Update 之后恢复 ⇒ 这一行会盖掉那些写入。
            if (_owner == null || _owner._animator == null) yield break;
            _owner._animator.speed = Mathf.Clamp(slowScale, 0.05f, 1f);
            yield return null;
        }
        if (_owner != null && _owner._animator != null) _owner._animator.speed = 1f;
        if (_owner != null) _owner._guardBreakSlowRoutine = null;
    }

    // ───────────────────────── 挂起时的攻击位移暂停 ─────────────────────────

    /// <summary>挂起：与 Animator 同时暂停攻击位移（自持标记，不依赖 DOTween 的暂停查询接口）。</summary>
    public void PauseAttackTweenForSuspend()
    {
        if (_owner._suspendTweenPaused) return;
        if (_owner._attackTween == null || !_owner._attackTween.IsActive()) return;
        _owner._attackTween.Pause();
        _owner._suspendTweenPaused = true;
    }

    /// <summary>解除挂起：只恢复自己暂停过的攻击位移。</summary>
    public void ResumeAttackTweenAfterSuspend()
    {
        if (!_owner._suspendTweenPaused) return;
        _owner._suspendTweenPaused = false;
        if (_owner._attackTween != null && _owner._attackTween.IsActive())
            _owner._attackTween.Play();
    }

    // ───────────────────────── 受击缩放 ─────────────────────────

    public void RestartHitScaleFeedback(HitFeedbackStrength feedbackStrength = HitFeedbackStrength.Standard)
    {
        StopHitScaleFeedback();

        float basePeak = feedbackStrength switch
        {
            HitFeedbackStrength.Light => 1.1f,
            HitFeedbackStrength.Heavy => 1.28f,
            _ => 1.2f
        };
        float durationScale = feedbackStrength == HitFeedbackStrength.Heavy ? 1.15f : 1f;
        bool superArmor = _owner.isSuperArmor;
        float peakMultiplier = superArmor ? Mathf.Min(basePeak, 1.1f) : basePeak;
        float expandDuration = (superArmor ? 0.04f : 0.06f) * durationScale;
        float recoverDuration = (superArmor ? 0.06f : 0.09f) * durationScale;

        Sequence sequence = DOTween.Sequence().SetTarget(_owner.transform);
        sequence.Append(DOTween.To(
            () => _owner._hitScaleMultiplier,
            ApplyHitScaleMultiplier,
            peakMultiplier,
            expandDuration).SetEase(Ease.OutQuad));
        sequence.Append(DOTween.To(
            () => _owner._hitScaleMultiplier,
            ApplyHitScaleMultiplier,
            1f,
            recoverDuration).SetEase(Ease.InQuad));

        _owner._hitScaleTween = sequence;
        sequence.OnComplete(() => FinishHitScaleFeedback(sequence));
        sequence.OnKill(() => FinishHitScaleFeedback(sequence));
    }

    private void FinishHitScaleFeedback(Tween owner)
    {
        if (_owner._hitScaleTween != owner) return;

        _owner._hitScaleTween = null;
        _owner._hitScaleMultiplier = 1f;
        ApplyHitScaleMultiplier(1f);
    }

    public void StopHitScaleFeedback()
    {
        Tween activeTween = _owner._hitScaleTween;
        _owner._hitScaleTween = null;
        if (activeTween != null && activeTween.IsActive())
            activeTween.Kill(false);

        _owner._hitScaleMultiplier = 1f;
        ApplyHitScaleMultiplier(1f);
    }

    private void ApplyHitScaleMultiplier(float multiplier)
    {
        _owner._hitScaleMultiplier = multiplier;
        if (_owner.originalScale == Vector3.zero) return;

        float currentX = _owner.transform.localScale.x;
        float xSign = currentX < 0f ? -1f : 1f;
        _owner.transform.localScale = new Vector3(
            Mathf.Abs(_owner.originalScale.x) * xSign * multiplier,
            _owner.originalScale.y * multiplier,
            _owner.originalScale.z * multiplier);
    }

    // ───────────────────────── 闪白 ─────────────────────────

    /// <summary>
    /// 触发受伤精灵闪烁（Animator HitFlash 状态，0.3秒后自动回到 Idle）
    /// C技霸体帧(isCFrame)、击飞(Launched)、死亡(Dead)、QTE攻击中不触发 HitFlash 动画
    /// </summary>
    public void TriggerHitFlash(HitReactionDirection hitReactionDirection)
    {
        // 骑乘合成动作没有正式受击Clip；继续闪白/缩放/卡肉，保留当前阶段，避免显示101步兵。
        if (_owner._cavalryVisual != null && _owner._cavalryVisual.OwnsMountedAnimator) return;
        if (_owner._hitFlashRoutine != null)
            _owner.StopCoroutine(_owner._hitFlashRoutine);
        _owner._hitFlashRoutine = _owner.StartCoroutine(HitFlashRoutine(hitReactionDirection));
    }

    private IEnumerator HitFlashRoutine(HitReactionDirection hitReactionDirection)
    {
        // 守卫：C帧弹刀、击飞、死亡、QTE 和眩晕不播受击动画；Getup 可被受击替换。
        bool wasGettingUp = _owner.state == EnemyState.GettingUp;
        bool allowHit = !_owner.isCFrame && (!_owner.isSuperArmor || _owner._allowHitFlashOnce);
        _owner._allowHitFlashOnce = false;
        if (_owner.state != EnemyState.Launched
            && _owner.state != EnemyState.Dead
            && _owner.state != EnemyState.QTEAttacking
            && _owner.state != EnemyState.Stunned
            && !_owner.IsFrozenHitReactionSuppressed
            && allowHit)
        {
            if (wasGettingUp)
            {
                _owner._getupInterrupted = true;
                if (_owner._getupRoutine != null)
                {
                    _owner.StopCoroutine(_owner._getupRoutine);
                    _owner._getupRoutine = null;
                }
            }
            _owner.PlayHitVisual(hitReactionDirection);
        }
        yield return new WaitForSeconds(wasGettingUp ? 0.9f : 0.4f);
        if (wasGettingUp && _owner.state == EnemyState.GettingUp && _owner._getupInterrupted)
        {
            _owner._getupInterrupted = false;
            _owner.state = EnemyState.Idle;
            _owner.PlayIdleVisual();
            _owner.ResumeAfterGetup();
        }
        _owner._hitFlashRoutine = null;
        // 受击反应正常结束 → 连续硬直预算重置（下次再挨打重新计）
        _owner._hitStunAccum = 0f;
    }

    // ───────────────────────── 闪白材质 ─────────────────────────

    /// <summary>
    /// 创建或恢复每个 Renderer 的专属材质实例。
    /// 对象池复用时保留实例，避免销毁后 Renderer 持有失效材质。
    /// </summary>
    public void CreateFlashMaterials()
    {
        if (_owner.renderers == null || _owner.renderers.Length == 0) return;

        if (_owner.flashMaterials == null || _owner.flashMaterials.Length != _owner.renderers.Length)
        {
            DestroyFlashMaterials();
            _owner.flashMaterials = new Material[_owner.renderers.Length];
        }

        for (int i = 0; i < _owner.renderers.Length; i++)
        {
            var renderer = _owner.renderers[i];
            if (renderer == null) continue;

            if (_owner.flashMaterials[i] == null)
                _owner.flashMaterials[i] = new Material(renderer.sharedMaterial);

            if (renderer.sharedMaterial != _owner.flashMaterials[i])
                renderer.material = _owner.flashMaterials[i];

            _owner.flashMaterials[i].color = Color.white;
        }
    }

    public void ResetWaveTint()
    {
        if (_owner._spriteRenderer != null)
            _owner._spriteRenderer.color = _owner._prefabColor;

        if (_owner.renderers == null) return;
        foreach (var renderer in _owner.renderers)
        {
            if (renderer != null && !(renderer is SpriteRenderer))
                renderer.SetPropertyBlock(null);
        }
    }

    /// <summary>
    /// 销毁每个 Renderer 的专属材质实例（仅在对象最终销毁时调用）。
    /// </summary>
    public void DestroyFlashMaterials()
    {
        if (_owner.flashMaterials == null) return;

        foreach (var mat in _owner.flashMaterials)
        {
            if (mat != null)
                Object.Destroy(mat);
        }
        _owner.flashMaterials = null;
    }
}
