using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// BOSS **多阶段系统**：阶段配置查询、Idle 行动加权调度、转阶段演出与阶段参数套用。
///
/// 拆自 <c>Enemy</c>（2026-10-21，批次 3 刀 B）。职责边界：
///   • 只处理**阶段相关的编排**：当前阶段数据、BOSS Idle 的行动选择（普通/C技/QTE 加权）、
///     转阶段的进入/演出/收尾；
///   • 「BOSS 推进到应战排、暂停等待、恢复推进」属于**行军/补齐**范畴，留在 Enemy；
///   • 依赖通过 <see cref="Enemy"/> 引用访问（同 <c>EnemyFreeze</c> / <c>EnemyOutline</c> /
///     <c>EnemyHitFeedback</c> 的既有做法）：状态字段留在 Enemy（可见性 internal），
///     不注入委托、不搬字段。
///
/// 规则依据：design/percolumn-fillup-rules.md（BOSS 墙与补齐）、design/immutable-constraints.md
/// 第 6 条（订单完成校验 owner + generation）。
/// </summary>
public sealed class EnemyBossPhases
{
    private readonly Enemy _owner;

    public EnemyBossPhases(Enemy owner)
    {
        _owner = owner;
    }

    /// <summary>获取当前阶段的 BossPhaseData。</summary>
    public BossPhaseData GetCurrentPhaseData()
    {
        if (!_owner.isBoss || _owner.bossPhases == null || _owner.bossPhases.Count == 0) return null;
        if (_owner.currentBossPhase < 0 || _owner.currentBossPhase >= _owner.bossPhases.Count) return null;
        return _owner.bossPhases[_owner.currentBossPhase];
    }

    /// <summary>设置 BOSS Idle 行动冷却计时器（从当前阶段的 actionInterval 随机取值）。</summary>
    public void SetBossActionCooldown()
    {
        var phase = GetCurrentPhaseData();
        if (phase != null)
            _owner.actionCooldownTimer = Random.Range(phase.actionInterval.x, phase.actionInterval.y);
        else
            _owner.actionCooldownTimer = Random.Range(0.3f, 1f);
    }

    /// <summary>BOSS Idle 调度：加权随机选择下一个行动（Attack/CAttack/QTE）。</summary>
    public void SelectBossAction()
    {
        if (_owner.state != EnemyState.Idle) return;
        var phase = GetCurrentPhaseData();
        if (phase == null)
        {
            _owner.StartAttacking();
            return;
        }

        float totalWeight = phase.normalAttackWeight + phase.cAttackWeight + phase.qteWeight;
        if (totalWeight <= 0f)
        {
            _owner.StartAttacking();
            return;
        }

        float roll = Random.Range(0f, totalWeight);

        if (roll < phase.normalAttackWeight)
        {
            // 普通攻击
            _owner.StartAttacking();
        }
        else if (roll < phase.normalAttackWeight + phase.cAttackWeight)
        {
            // C技
            _owner.StartAttacking();
        }
        else
        {
            // QTE
            var qte = _owner.GetQTEController();
            if (qte != null && qte.TriggerQTEAttack())
            {
                // QTE 成功触发，QTEController 内部调用 EnterQTEAttack()
                // 冷却在 ExitQTEAttack() 中设置
                return;
            }
            // QTE 触发失败（序列耗尽/眩晕等），回 Idle 等冷却
            SetBossActionCooldown();
        }
    }

    /// <summary>进入转阶段：锁血 + 无敌 + 打断当前攻击 + 播放转阶段演出。</summary>
    public void EnterPhaseTransition(BossPhaseData nextPhase)
    {
        // BUG FIX: 转阶段时若处于 QTE 中，清理 QTE 状态（同 Die）
        if (_owner.state == EnemyState.QTEAttacking)
        {
            _owner.GetQTEController()?.AbortQTE();
        }

        _owner._healthLocked = true;
        _owner.isPhaseTransitioning = true;

        DebugLog.Info($"[Enemy] BOSS转阶段: {_owner.DebugTag}, phase={_owner.currentBossPhase} → {nextPhase.phaseIndex} ({nextPhase.phaseName})");

        // 打断当前攻击
        if (_owner._attackTween != null && _owner._attackTween.IsActive())
        {
            _owner._attackTween.Kill();
            _owner._attackTween = null;
        }
        _owner.transform.DOKill(false);
        _owner.StopHitScaleFeedback();
        _owner.isAttackAnimating = false;
        _owner.isAttackDrawPhase = false;
        _owner.isCFrame = false;
        _owner.UpdateOutlineState();

        // 转阶段演出优先（设计 4.3 / 用例 11e）：先解除冻结与破冰序列
        _owner.ClearFreezeForSystemOverride();

        if (_owner._phaseTransitionRoutine != null)
            _owner.StopCoroutine(_owner._phaseTransitionRoutine);
        _owner._phaseTransitionRoutine = _owner.StartCoroutine(PhaseTransitionSequence(nextPhase));
    }

    private IEnumerator PhaseTransitionSequence(BossPhaseData nextPhase)
    {
        while (_owner.state == EnemyState.Launched)
        {
            yield return null;
        }

        // 转阶段会结束当前眩晕；无敌已在 EnterPhaseTransition 生效，先完整播放眩晕收招。
        if (_owner.state == EnemyState.Stunned)
        {
            _owner.BeginStunExit();
            while (_owner._isStunExiting || _owner.state == EnemyState.Launched)
            {
                yield return null;
            }
        }

        // 设置无敌：切换到 Idle 状态
        _owner.state = EnemyState.Idle;

        // 播放转阶段动画
        if (_owner._animator != null && !string.IsNullOrEmpty(nextPhase.transitionTriggerName))
        {
            _owner._animator.SetTrigger(nextPhase.transitionTriggerName);
            yield return new WaitForSeconds(nextPhase.transitionDuration);
        }

        CompletePhaseTransition(nextPhase);
    }

    /// <summary>转阶段收尾：套用新阶段参数、切换 QTE 数据、广播阶段变化。</summary>
    public void CompletePhaseTransition(BossPhaseData nextPhase)
    {
        int newPhaseIndex = nextPhase.phaseIndex;

        // 应用新阶段配置
        if (nextPhase.attackSequence != null && nextPhase.attackSequence.Count > 0)
            _owner.attackSequence = nextPhase.attackSequence;

        _owner.stabDamageMultiplier = nextPhase.stabDamageMultiplier;
        _owner.slashDamageMultiplier = nextPhase.slashDamageMultiplier;
        _owner.pierceDamageMultiplier = nextPhase.pierceDamageMultiplier;
        _owner.sweepDamageMultiplier = nextPhase.sweepDamageMultiplier;
        _owner.launchDamageMultiplier = nextPhase.launchDamageMultiplier;
        _owner.poiseDamageMultiplier = nextPhase.poiseDamageMultiplier;

        _owner.isSuperArmor = nextPhase.isSuperArmor;

        // 切换 QTE 数据
        if (nextPhase.qteData != null)
        {
            var qte = _owner.GetQTEController();
            if (qte != null)
                qte.SwitchQteData(nextPhase.qteData);
        }

        _owner.currentBossPhase = newPhaseIndex;
        _owner.OnBossPhaseChanged?.Invoke(_owner, _owner.currentBossPhase);
        _owner._healthLocked = false;
        _owner.isPhaseTransitioning = false;
        _owner._currentAttackStep = 0;
        _owner.state = EnemyState.Idle;
        _owner.isAttackAnimating = false;
        _owner.isAttackDrawPhase = false;
        SetBossActionCooldown();

        _owner.UpdateOutlineState();

        DebugLog.Info($"[Enemy] BOSS转阶段完成: {_owner.DebugTag}, phase={_owner.currentBossPhase} ({nextPhase.phaseName}), superArmor={_owner.isSuperArmor}");
    }
}
