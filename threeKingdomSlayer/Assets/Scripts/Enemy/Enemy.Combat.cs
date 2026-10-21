using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Enemy 的**战斗结算：命中帧（动画帧 → 出伤）、伤害系统、韧性/护盾/位移命中，以及冰系薄转发。**
///
/// 按机制从 <c>Enemy.cs</c> 拆出（2026-10-21，批次 3 刀 C：partial 分文件）。
/// partial class 与 Enemy.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
/// 命中与伤害是战斗核心，改动前先读 design/immutable-constraints.md 第 5、6 条。
/// </summary>
public partial class Enemy : MonoBehaviour
{
    #region 命中帧（动画帧 → 出伤）

    /// <summary>
    /// 读取本步攻击声明的命中帧；无声明时保持空，出伤退回旧 spawnDuration 逻辑。
    /// </summary>

    #endregion

    #region 伤害系统

    /// <summary>
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage, DamageType damageType = DamageType.Stab, Color? damageNumberColor = null, bool canInterruptCFrame = false, bool isParryInterrupt = false, bool countsForCombo = true, bool canInterruptAttack = true, bool triggerHitAnimation = true, bool ignoreDamageModifiers = false, HitFeedbackSource feedbackSource = HitFeedbackSource.BasicAttack, HitFeedbackStrength? feedbackStrength = null, Vector3? impactPosition = null, Vector3 impactDirection = default, bool diseaseStabHit = false, bool interruptsCavalryCharge = false, HitReactionDirection hitReactionDirection = HitReactionDirection.None, bool suppressHitReactionAnimation = false)
    {
        if (state == EnemyState.Dead) return;
        if (isBoss && bossState != BossState.InCombat) return;
        if (isPhaseTransitioning) return; // 转阶段无敌

        // 锁血检查
        if (_healthLocked) return;

        // 破冰（P4）：合规重攻击命中冻结目标 → 播破冰动画 + 本段伤害翻倍。
        // 推动（①）与碰撞（②）由 BreakIcePush 负责。
        // 命中反应走注册表（扩展点）：冰系是内置处理器，新增元素/反应只需注册一个。
        DamageReactionRegistry.TryResolve(this, damageType, feedbackSource, ref damage);

        // 转阶段血量阈值检查（在扣血前判断）
        if (isBoss && bossPhases != null && currentBossPhase < bossPhases.Count - 1)
        {
            var nextPhase = bossPhases[currentBossPhase + 1];
            if (nextPhase != null)
            {
                float hpPercent = currentHealth / maxHealth;
                float dmgMultiplier = ignoreDamageModifiers ? 1f : GetDamageMultiplier(damageType);
                // BUG FIX: 预测伤害需包含 launchedDamageTakenMultiplier，
                // 否则击飞状态下实际伤害 > 预测值，可能一击打穿阈值直接致死
                float predictedDmg = damage * dmgMultiplier;
                if (state == EnemyState.Launched)
                    predictedDmg *= launchedDamageTakenMultiplier;
                float dmgPercent = predictedDmg / maxHealth;
                if (hpPercent - dmgPercent <= nextPhase.triggerHealthPercent)
                {
                    // QTE 演出中不立即转阶段：挂起标记，血量在下方统一 clamp 到阈值，
                    // 避免 AbortQTE 让 QTE 瞬间消失。QTE 结束（ExitQTEAttack）再执行转阶段。
                    if (state == EnemyState.QTEAttacking)
                    {
                        _deferredPhaseTransition = nextPhase;
                        DebugLog.Info($"[Enemy] QTE中血量达转阶段阈值，挂起转阶段: {DebugTag}, pendingPhase={nextPhase.phaseIndex}");
                    }
                    else
                    {
                        currentHealth = nextPhase.triggerHealthPercent * maxHealth;
                        // BUG FIX: 若 triggerHealthPercent=0 导致 hp=0，直接死亡而非转阶段
                        if (currentHealth <= 0f)
                        {
                            Die();
                            return;
                        }
                        EnterPhaseTransition(nextPhase);
                        return;
                    }
                }
            }
        }

        // ----- 攻击打断逻辑 -----
        // QTEAttacking: 不可被任何攻击打断（独立状态，不进入此分支）
        // Attacking + AttackDraw: 不可打断
        // Boss:  普通窗口（无霸体/CFrame）→ 所有攻击可打断
        //        霸体/CFrame → 仅 Parry 可打断
        // 非Boss: 普通窗口任何攻击可打断; CFrame/SuperArmor 仅 canInterruptCFrame 可打断
        if (canInterruptAttack && state == EnemyState.Attacking && isAttackAnimating && !isAttackDrawPhase)
        {
            if (isBoss)
            {
                if (!isSuperArmor && !isCFrame)
                    CancelAttack();
                else if (isParryInterrupt)
                {
                    _allowHitFlashOnce = true;
                    CancelAttack();
                }
            }
            else
            {
                if (!isSuperArmor && !isCFrame)
                    CancelAttack();
                else if (canInterruptCFrame)
                    CancelAttack();
            }
        }

        bool allowHitReactionAnimation = triggerHitAnimation
            && !suppressHitReactionAnimation
            && !IsFrozenHitReactionSuppressed // 冻结中受击不播 hitted（破冰序列除外）
            && feedbackSource != HitFeedbackSource.Dot
            && damageType != DamageType.Launch
            && formState != EnemyFormState.ShieldBroken; // 破盾表现期：普通受击不打断掉盾动画
        HitReactionDirection resolvedHitReactionDirection = allowHitReactionAnimation
            ? ResolveHitReactionDirection(hitReactionDirection)
            : HitReactionDirection.None;

        if (sharedHealthGroup != null)
        {
            sharedHealthGroup.TakeDamage(damage, damageType, this, damageNumberColor,
                allowHitReactionAnimation, countsForCombo, canInterruptAttack, ignoreDamageModifiers,
                feedbackSource, feedbackStrength, impactPosition, impactDirection, diseaseStabHit,
                resolvedHitReactionDirection, suppressHitReactionAnimation);
            return;
        }

        // 击飞状态下受到伤害倍率
        if (state == EnemyState.Launched)
            damage *= launchedDamageTakenMultiplier;

        // 应用弱点倍率
        float multiplier = ignoreDamageModifiers ? 1f : GetDamageMultiplier(damageType);
        float finalDamage = damage * multiplier;

        // 盾牌判定（盾兵/1021）：固定减伤 / 破盾。DoT 等 ignoreDamageModifiers 伤害整段跳过。
        bool shieldBlocked = false;
        bool shieldBrokenNow = false;
        if (_shieldGuard != null && !ignoreDamageModifiers)
        {
            var guardResult = _shieldGuard.ProcessHit(finalDamage, damageType, hitReactionDirection, interruptsCavalryCharge);
            finalDamage = guardResult.finalDamage;
            shieldBlocked = guardResult.blocked;
            shieldBrokenNow = guardResult.broke;
        }

        DebugLog.Info($"[Enemy] TakeDamage: {DebugTag}, col={columnIndex}, raw={damage:F1}, mult={multiplier:F2}, final={finalDamage:F1}, hp={currentHealth:F1}→{currentHealth - finalDamage:F1}");

        currentHealth -= finalDamage;

        // QTE 中挂起转阶段时，血量不得跌破转阶段阈值（否则 QTE 未结束即触发死亡/转阶段）
        if (_deferredPhaseTransition != null)
        {
            float floor = _deferredPhaseTransition.triggerHealthPercent * maxHealth;
            if (currentHealth < floor)
            {
                currentHealth = floor;
                DebugLog.Info($"[Enemy] QTE锁血: {DebugTag}, hp保持={currentHealth:F1}");
            }
        }

        if (!isBoss && feedbackSource != HitFeedbackSource.Dot)
            AudioManager.Instance?.PostEvent("Enemy_Hit");
        HitFeedbackStrength resolvedFeedbackStrength = feedbackStrength
            ?? HitFeedbackManager.ResolveStrength(damageType, feedbackSource, finalDamage, false);
        if (shieldBlocked)
            resolvedFeedbackStrength = HitFeedbackStrength.Light; // 格挡：弱化卡肉，避免读成重击
        HitFeedbackManager.Trigger(HitFeedbackManager.CreateDamageContext(this, damageType,
            feedbackSource, resolvedFeedbackStrength, finalDamage, false, impactPosition, impactDirection,
            diseaseStabHit, resolvedHitReactionDirection));
        if (countsForCombo)
            OnDamageTaken?.Invoke(this);
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

        // 受伤跳字（经呈现层端口 → 界面层的跳字池）
        var damageNumbers = CombatPresenters.DamageNumbers;
        if (finalDamage > 0f && damageNumbers != null)
        {
            // 被盾格挡时用专属颜色跳字，让"减伤"可读（否则玩家会读成没打中）
            Color? resolvedNumberColor = shieldBlocked && _shieldGuard != null
                ? _shieldGuard.BlockDamageNumberColor
                : damageNumberColor;
            damageNumbers.Spawn(transform.position, finalDamage, resolvedNumberColor, resolvedFeedbackStrength);
        }

        // 血条显示（非BOSS、未死亡时显示）
        if (!isBoss && currentHealth > 0f)
        {
            if (cachedHealthBar == null)
                cachedHealthBar = GetComponent<EnemyHealthBar>();
            if (cachedHealthBar == null)
                cachedHealthBar = gameObject.AddComponent<EnemyHealthBar>();
            cachedHealthBar.Show(currentHealth / maxHealth);
        }

        if (allowHitReactionAnimation)
        {
            // BUG FIX: 同步应用闪白（立即设置颜色，不依赖 Update 循环）
            ApplyHitFlashImmediate();

            // 触发受伤闪白效果（非致命伤通过 Update 循环过渡恢复）
            hitFlashTimer = HIT_FLASH_DURATION;
            DebugLog.Info($"[Enemy] 触发闪白: {DebugTag}, duration={HIT_FLASH_DURATION}");

            // DOTween: 受击大小抖动效果（与闪白同步触发）
            RestartHitScaleFeedback(resolvedFeedbackStrength);

            if (isBoss && state == EnemyState.Stunned)
            {
                if (!_isStunExiting)
                {
                    if (_hitFlashRoutine != null)
                    {
                        StopCoroutine(_hitFlashRoutine);
                        _hitFlashRoutine = null;
                    }
                    _animator?.ResetTrigger("Hit");
                    _animator?.Play("StunHit", 0, 0f);
                }
            }
            else
            {
                TriggerHitFlash(resolvedHitReactionDirection);
            }
        }

        // 格挡表现：专属状态覆盖普通受击动画（护盾火花/后仰）
        if (shieldBlocked && _shieldGuard != null)
            _shieldGuard.PlayBlockVisual();

        // 破盾：伤害结算完成后再启动表现流程（死亡时不触发）
        if (shieldBrokenNow && _shieldGuard != null && currentHealth > 0f)
            _shieldGuard.OnShieldBroken();

        // 击飞状态下被攻击延长浮空时间
        if (state == EnemyState.Launched && launchedHitExtendDuration > 0f)
        {
            ExtendLaunch(launchedHitExtendDuration);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
        else if (_cavalry != null && interruptsCavalryCharge)
        {
            // 只有被标记为“可打断骑兵冲锋”的攻击才会中断冲锋（当前 = 蓄力攻击）。
            // 普通攻击只造成伤害，不打断；真正击飞走 Launch -> 落马。
            _cavalry.OnInterruptingHit();
        }
    }

    /// <summary>
    /// 同步应用闪白效果（在 TakeDamage 中调用）
    /// 直接通过材质实例设置 color = white，确保即使敌人立即死亡，
    /// 闪白也在协程等待期间可见（协程中 state=Dead 但 UpdateAlpha 不执行，
    /// 材质颜色在协程等待期间保持白色）。
    /// </summary>
    private void ApplyHitFlashImmediate()
    {
        if (flashMaterials == null) return;
        foreach (var mat in flashMaterials)
        {
            if (mat != null) mat.color = Color.white;
        }
    }

    /// <summary>
    /// 受伤视觉反馈（闪白+抖动），不修改HP，供 SharedHealthGroup 调用
    /// </summary>
    public void ApplyDamageFeedback(HitFeedbackStrength feedbackStrength = HitFeedbackStrength.Standard,
        HitReactionDirection hitReactionDirection = HitReactionDirection.None,
        bool suppressHitReactionAnimation = false)
    {
        if (state == EnemyState.Dead) return;

        ApplyHitFlashImmediate();

        if (isBoss && state == EnemyState.Stunned)
        {
            if (!_isStunExiting)
            {
                if (_hitFlashRoutine != null)
                {
                    StopCoroutine(_hitFlashRoutine);
                    _hitFlashRoutine = null;
                }
                _animator?.ResetTrigger("Hit");
                _animator?.Play("StunHit", 0, 0f);
            }
        }
        else if (!suppressHitReactionAnimation)
        {
            TriggerHitFlash(ResolveHitReactionDirection(hitReactionDirection));
        }

        hitFlashTimer = HIT_FLASH_DURATION;

        RestartHitScaleFeedback(feedbackStrength);
    }

    public void ApplyHitStop(float duration)
    {
        if (duration <= 0f || state == EnemyState.Dead) return;

        if (_hitStopRemaining <= 0f)
        {
            PushSuspend(SuspendSource.HitStop);
            if (HitFeedbackManager.EnableDebugLogs)
                Debug.Log($"[HitFeedback] Freeze enemy={DebugTag} requested={duration:F3}s animator={_animator != null} animatorWasEnabled={_suspendAnimatorWasEnabled} frame={Time.frameCount}");
        }

        _hitStopRemaining = Mathf.Max(_hitStopRemaining, duration);
    }

    /// <summary>挂起：暂停 Animator 与攻击位移。可叠加（卡肉 + 冻结），全部解除才恢复。</summary>
    public void PushSuspend(SuspendSource source)
    {
        if (source == SuspendSource.None) return;
        if ((_suspendSources & source) != 0) return;
        bool wasSuspended = IsSuspended;
        _suspendSources |= source;
        if (wasSuspended) return;

        _suspendAnimatorWasEnabled = _animator != null && _animator.enabled;
        if (_animator != null)
            _animator.enabled = false;
        // 与 Animator 同时暂停攻击位移：否则「精灵冻结、位移/出伤继续」会错拍
        PauseAttackTweenForSuspend();
    }

    /// <summary>解除挂起。仍有其它来源时保持挂起状态。</summary>
    public void PopSuspend(SuspendSource source)
    {
        if (source == SuspendSource.None) return;
        if ((_suspendSources & source) == 0) return;
        _suspendSources &= ~source;
        if (IsSuspended) return;

        if (state != EnemyState.Dead && _animator != null && _suspendAnimatorWasEnabled)
            _animator.enabled = true;
        _suspendAnimatorWasEnabled = false;
        ResumeAttackTweenAfterSuspend();
    }

    #region 冻结（冰系）

    // 冰系的**状态与流程**已搬到 EnemyFreeze（Assets/Scripts/Enemy/EnemyFreeze.cs），
    // 表现层在 EnemyFreezeVisual（冰壳覆盖层 / 着色 / 碎冰特效）。
    // 本区段只保留对外接口与转发，Enemy 不再持有冰系状态。


    #endregion

    // ── 打击反馈：实现见 EnemyHitFeedback（批次 3 刀 A），此处保留同名薄转发 ──

    private void UpdateHitStop() => HitFeedback.UpdateHitStop();

    private void StopHitStop() => HitFeedback.StopHitStop();

    private void PauseAttackTweenForSuspend() => HitFeedback.PauseAttackTweenForSuspend();

    private void ResumeAttackTweenAfterSuspend() => HitFeedback.ResumeAttackTweenAfterSuspend();

    private void RestartHitScaleFeedback(HitFeedbackStrength feedbackStrength = HitFeedbackStrength.Standard)
        => HitFeedback.RestartHitScaleFeedback(feedbackStrength);

    internal void StopHitScaleFeedback() => HitFeedback.StopHitScaleFeedback();

    private void TriggerHitFlash(HitReactionDirection hitReactionDirection) => HitFeedback.TriggerHitFlash(hitReactionDirection);

    private void CreateFlashMaterials() => HitFeedback.CreateFlashMaterials();

    private void ResetWaveTint() => HitFeedback.ResetWaveTint();

    private void DestroyFlashMaterials() => HitFeedback.DestroyFlashMaterials();

    /// <summary>
    /// 位移主动的零伤害强控命中：打断攻击前摇（含CFrame/SuperArmor），
    /// 不扣HP、不生成伤害数字，计入Combo，并强制播放受击动画与反馈。
    /// QTE、收招、转阶段及非交战Boss不受影响。
    /// </summary>
    public bool ApplyActiveDisplacementHit(bool suppressHitReactionAnimation = false,
        HitReactionDirection hitReactionDirection = HitReactionDirection.None)
    {
        if (state == EnemyState.Dead || state == EnemyState.QTEAttacking || isPhaseTransitioning) return false;
        if (isBoss && bossState != BossState.InCombat) return false;
        if (state == EnemyState.Attacking && isAttackDrawPhase) return false;

        if (state == EnemyState.Attacking && isAttackAnimating)
            CancelAttack();

        ApplyHitFlashImmediate();
        hitFlashTimer = HIT_FLASH_DURATION;
        RestartHitScaleFeedback(HitFeedbackStrength.Heavy);
        if (!suppressHitReactionAnimation && state != EnemyState.Launched)
        {
            hitFlashTimer = HIT_FLASH_DURATION;
            TriggerHitFlash(ResolveHitReactionDirection(hitReactionDirection));
        }
        HitFeedbackManager.Trigger(new HitFeedbackContext(this, DamageType.Poise,
            HitFeedbackSource.Displacement, HitFeedbackStrength.Heavy, 0f, false, true, transform.position,
            hitReactionDirection: hitReactionDirection));
        OnDamageTaken?.Invoke(this);
        return true;
    }

    // ── 韧性：实现见 EnemyPoise（批次 3 刀 E1），此处保留同名薄转发 ──

    public bool TakeActiveDisplacementPoiseDamage(float maxPoisePercent) => Poise.TakeActiveDisplacementPoiseDamage(maxPoisePercent);

    public bool TakePoiseDamage(float poiseDamage) => Poise.TakePoiseDamage(poiseDamage);

    public bool TakeQTEPoiseDamage(float poiseDamage, bool interruptibleOnStun) => Poise.TakeQTEPoiseDamage(poiseDamage, interruptibleOnStun);

    /// <summary>
    /// 根据伤害类型获取倍率
    /// </summary>
    public float GetDamageMultiplier(DamageType damageType)
    {
        float multiplier;
        switch (damageType)
        {
            case DamageType.Stab: multiplier = stabDamageMultiplier; break;
            case DamageType.Slash: multiplier = slashDamageMultiplier; break;
            case DamageType.Pierce: multiplier = pierceDamageMultiplier; break;
            case DamageType.Sweep: multiplier = sweepDamageMultiplier; break;
            case DamageType.Launch: multiplier = launchDamageMultiplier; break;
            case DamageType.Poise: multiplier = poiseDamageMultiplier; break;
            default: multiplier = 1f; break;
        }

        // 盾兵持盾时减伤；被击飞进入胆怯形态后失去该减伤。
        if (!IsCoward && normalFormDamageReductionPercent > 0f)
            multiplier *= 1f - Mathf.Clamp01(normalFormDamageReductionPercent);

        return multiplier;
    }

    /// <summary>
    /// 死亡
    /// 流程：
    ///   1. state = Dead（Update 提前返回，不再处理攻击/移动逻辑）
    ///   2. 取消正在执行的攻击 DOTween 动画
    ///   3. 立即触发 OnDeath 事件 → EnemyManager 计入击杀、判断通关
    ///   4. 启动死亡动效协程 DeathBounceAndFall()（弹起+旋转+掉落，纯视觉）
    ///   5. 协程结束后触发 OnDeathAnimComplete → Boss 锦囊等
    ///   6. 回收到对象池
    /// </summary>
    public void Die()
    {
        if (state == EnemyState.Dead) return;

        EnemyManager.Instance?.columnManager?.CancelPushReturnForEnemy(this, "cancel-dead");

        // QTE 演出中死亡：立即清理 QTE 状态（飞行物/指示器/输入拦截）
        if (state == EnemyState.QTEAttacking)
        {
            GetQTEController()?.AbortQTE();
            _deferredPhaseTransition = null; // 死亡不再转阶段
        }

        bool wasLaunched = (state == EnemyState.Launched);
        OnDying?.Invoke(this);
        StopHitStop();
        ClearFreezeOnTeardown();
        StopHitScaleFeedback();
        state = EnemyState.Dead;
        UpdateOutlineState();
        PlayDeadVisual();

        // BUG FIX: 取消正在执行的攻击 DOTween 动画
        // 若敌人在攻击动画中被秒杀，立即中断攻击动作（前移+翻转），直接进入死亡状态
        // Kill() 后立即重置位置和缩放：Kill 会留下中断时的中间值（如翻转-0.2），
        // 若不重置，死亡动效期间敌人位置/缩放会是错误的
        _attackTelegraph?.StopWarning();
        if (_attackTween != null && _attackTween.IsActive())
        {
            _attackTween.Kill();
            _attackTween = null;
            UpdateWorldPosition();
            transform.localScale = originalScale;
        }
        isAttackAnimating = false;
        isAttackDrawPhase = false;
        isMovingToNextRow = false;

        // 立即隐藏血条（非BOSS），避免血条随死亡动画飘落
        if (!isBoss && cachedHealthBar != null)
        {
            cachedHealthBar.Hide();
        }

        // 立即触发死亡事件：计入击杀数、判断通关（不等死亡动画播完）
        OnDeath?.Invoke(this);

        // 清理所有残留 DOTween（rush bounce 等），避免与死亡动画冲突
        transform.DOKill();

        // 启动死亡动效协程（弹起 + 旋转 + 重力掉落，纯视觉表现）
        // 骑兵类（109）的非击飞死亡改由专用视觉组件接管：骑手摔落→战马逃跑，不能再叠加根弹起/旋转。
        bool customDeathVisual = !wasLaunched
            && CustomDeathVisualHandler != null
            && CustomDeathVisualHandler(this);

        if (customDeathVisual)
        {
            // 演出与回收由接管方负责，结束时调用 CompleteCustomDeathVisualSequence()。
        }
        else if (wasLaunched)
            StartCoroutine(LaunchDeathEffect());
        else
            StartCoroutine(DeathBounceAndFall());
    }

    /// <summary>
    /// 自定义死亡视觉演出结束：与 DeathBounceAndFall 的收尾一致（还原姿态 → OnDeathAnimComplete → 回对象池）。
    /// 只能由通过 <see cref="CustomDeathVisualHandler"/> 接管演出的组件调用。
    /// </summary>
    public void CompleteCustomDeathVisualSequence()
    {
        if (state != EnemyState.Dead) return;

        transform.localScale = originalScale;
        transform.localRotation = originalRotation;

        OnDeathAnimComplete?.Invoke(this);

        EnemyPool.Instance?.ReturnEnemy(this);
    }

    /// <summary>
    /// 死亡动效协程 — 弹起 + 随机旋转 + 重力掉落
    /// 使用 DOTween 实现：
    ///   1. 立即闪白（ApplyHitFlashImmediate）
    ///   2. 弹起（Y 轴向上 OutQuad 缓动）
    ///   3. 随机旋转（在 X 和 Z 轴上随机角度，贯穿整个动画）
    ///   4. 受重力掉落离开屏幕（Y 轴向下 InQuad 缓动）
    ///   5. 恢复缩放和旋转（供对象池复用），回收到对象池
    /// </summary>
    private System.Collections.IEnumerator DeathBounceAndFall()
    {
        // 立即将材质颜色设为白色（闪白）
        ApplyHitFlashImmediate();

        // 保存起始位置
        Vector3 startPos = transform.localPosition;

        // 构建 DOTween 序列
        Sequence deathSeq = DOTween.Sequence().SetTarget(transform).SetUpdate(true);
        deathSeq.SetId("deathAnim");

        float jumpHeight = Random.Range(1.5f, 3.0f);   // 弹起高度
        float fallDistance = 20f;                        // 掉落距离（确保离开屏幕）
        float jumpDuration = 0.3f;                       // 弹起时长
        float fallDuration = 0.8f;                       // 掉落时长

        // 阶段1：弹起（OutQuad 缓动，模拟起跳的加速度）
        deathSeq.Append(transform.DOLocalMoveY(startPos.y + jumpHeight, jumpDuration).SetEase(Ease.OutQuad));

        // 阶段2：受重力掉落离开屏幕（InQuad 缓动，模拟自由落体加速）
        deathSeq.Append(transform.DOLocalMoveY(startPos.y - fallDistance, fallDuration).SetEase(Ease.InQuad));

        // 随机旋转（贯穿整个动画：弹起阶段 + 掉落阶段）
        // 在 X 轴和 Z 轴上随机旋转，模拟被击飞后的翻滚
        Vector3 randomRotation = new Vector3(
            Random.Range(-60f, 60f),   // X 轴轻微翻转
            0f,                          // Y 轴不旋转（2D 精灵）
            Random.Range(-360f, 360f)   // Z 轴大角度旋转
        );
        float totalAnimDuration = jumpDuration + fallDuration;
        transform.DORotate(randomRotation, totalAnimDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad)
            .SetTarget(transform)
            .SetUpdate(true)
            .SetId("deathAnim");

        // 等待序列完成
        yield return deathSeq.WaitForCompletion();

        // 恢复缩放和旋转（供对象池复用）
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;

        // 死亡动画结束 → 触发事件（Boss锦囊等）
        OnDeathAnimComplete?.Invoke(this);

        // 死亡动效结束后回收到对象池
        EnemyPool.Instance?.ReturnEnemy(this);
    }

    /// <summary>
    /// 击飞死亡动效 — 从当前击飞位置旋转坠落
    /// 不弹起，直接从当前位置（可能在空中）旋转并掉出屏幕
    /// </summary>
    private System.Collections.IEnumerator LaunchDeathEffect()
    {
        ApplyHitFlashImmediate();

        Vector3 startPos = transform.localPosition;

        Sequence deathSeq = DOTween.Sequence().SetTarget(transform).SetUpdate(true);
        deathSeq.SetId("deathAnim");

        float fallDistance = 20f;
        float fallDuration = 0.8f;

        // 从当前位置（可能在击飞空中）直接坠落
        deathSeq.Append(transform.DOLocalMoveY(startPos.y - fallDistance, fallDuration).SetEase(Ease.InQuad));

        // 随机旋转（比普通死亡更大角度，更有击飞感）
        Vector3 randomRotation = new Vector3(
            Random.Range(-90f, 90f),
            0f,
            Random.Range(-540f, -360f)
        );
        deathSeq.Join(transform.DORotate(randomRotation, fallDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));

        yield return deathSeq.WaitForCompletion();

        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity;

        // 死亡动画结束 → 触发事件
        OnDeathAnimComplete?.Invoke(this);

        EnemyPool.Instance?.ReturnEnemy(this);
    }

    public void CancelDeathAnimationAndReturnToPool()
    {
        if (state != EnemyState.Dead || !gameObject.activeInHierarchy) return;

        StopAllCoroutines();
        transform.DOKill(false);
        EnemyPool.Instance?.ReturnEnemy(this);
    }

    #endregion
}
