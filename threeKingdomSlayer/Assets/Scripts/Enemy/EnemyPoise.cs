using UnityEngine;

/// <summary>
/// 敌人的**韧性（Poise）**：削韧、破势（Poise 归零 → 眩晕）与 QTE 专用韧性伤害。
///
/// 拆自 <c>Enemy</c>（2026-10-21，批次 3 刀 E1）。职责边界：
///   • 只管**韧性**这一条线：普通削韧、主动位移的百分比削韧、QTE 期间的削韧与延迟眩晕；
///   • **扣血 / 破盾 / 共享血量 / 死亡**仍在 Enemy（那是 <c>TakeDamage</c>，战斗核心，
///     跨 15+ 字段与 10+ 方法，另开一刀，见交接文档 §14 的契约分析）；
///   • 韧性相关成员在 Enemy 里**本来就全是 public**，因此本类不需要任何 internal 化，
///     也不需要转发委托——只通过 <see cref="Enemy"/> 引用访问（同 EnemyFreeze / EnemyHitFeedback 的做法）。
///
/// 规则依据：design/immutable-constraints.md 第 5 条（打断边界：Parry 窗口外不削韧）。
/// </summary>
public sealed class EnemyPoise
{
    private readonly Enemy _owner;

    public EnemyPoise(Enemy owner)
    {
        _owner = owner;
    }

    /// <summary>
    /// V2 位移主动技能对未眩晕 Boss 造成最大架势百分比伤害。
    /// QTE 中破势延迟到 QTE 完整结束；转阶段中不结算。
    /// </summary>
    public bool TakeActiveDisplacementPoiseDamage(float maxPoisePercent)
    {
        if (!_owner.isBoss || maxPoisePercent <= 0f) return false;
        if (_owner.state == EnemyState.Dead || _owner.state == EnemyState.Stunned || _owner.state == EnemyState.Launched) return false;
        if (_owner.bossState != BossState.InCombat || _owner.isPhaseTransitioning) return false;

        _owner.currentPoise = Mathf.Max(0f, _owner.currentPoise - _owner.maxPoise * Mathf.Clamp01(maxPoisePercent));
        _owner.OnPoiseChanged?.Invoke(_owner, _owner.currentPoise, _owner.maxPoise);

        if (_owner.currentPoise > 0f) return false;

        if (_owner.state == EnemyState.QTEAttacking)
        {
            _owner.deferredStun = true;
            DebugLog.Info($"[Enemy] 主动位移破势延迟Stun: {_owner.DebugTag}");
            return true;
        }

        _owner.Stun(_owner.stunDuration);
        return true;
    }

    /// <summary>
    /// 受到架势（Poise）伤害 — 仅 Parry 在敌人 Attacking 态时调用
    /// Poise 不会自动回复；归零时触发 STUN，STUN 结束后重置 Poise 到满值
    /// Parry 判定窗口外不削韧：窗口外 parry 只造成伤害不打断、不削韧，
    /// 避免"连续乱按 parry 把敌人削晕"的锁死（与打断门控一致）
    /// </summary>
    public bool TakePoiseDamage(float poiseDamage)
    {
        if (!_owner.isBoss) return false;
        if (_owner.state == EnemyState.Dead) return false;
        if (_owner.state == EnemyState.Stunned) return false;
        if (_owner.state != EnemyState.Attacking) return false;
        if (!_owner.isAttackAnimating || _owner.isAttackDrawPhase) return false;
        if (_owner.bossState != BossState.InCombat) return false;

        _owner.currentPoise -= poiseDamage;
        _owner.OnPoiseChanged?.Invoke(_owner, _owner.currentPoise, _owner.maxPoise);

        if (_owner.currentPoise <= 0f)
        {
            _owner.currentPoise = 0f;
            _owner.Stun(_owner.stunDuration);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 霸体窗口内的削韧（重攻击打霸体）。
    ///
    /// 规则（2026-10-25 霸体反制规则重定）：
    ///   • 削韧只在**霸体窗口**内生效 —— C 技霸体（<c>isCFrame</c>）或阶段霸体（<c>isSuperArmor</c>）；
    ///     窗口外的敌人不吃这份削韧（破势是“破霸体”的手段，不是通用输出窗）。
    ///   • 韧性归零 → **破势眩晕**：进入 Stunned，眩晕同时解除霸体（Stun 里会清 isCFrame / isSuperArmor）。
    ///   • 普通敌人用 <c>poiseStunDuration</c>（短眩晕），BOSS 沿用 <c>stunDuration</c>。
    /// </summary>
    public bool TakeSuperArmorPoiseDamage(float poiseDamage)
    {
        if (poiseDamage <= 0f) return false;
        if (_owner.state == EnemyState.Dead) return false;
        if (_owner.state == EnemyState.Stunned) return false;

        _owner.currentPoise -= poiseDamage;
        _owner.OnPoiseChanged?.Invoke(_owner, _owner.currentPoise, _owner.maxPoise);

        if (_owner.currentPoise > 0f) return false;

        _owner.currentPoise = 0f;
        float stunSeconds = _owner.isBoss ? _owner.stunDuration : _owner.poiseStunDuration;
        _owner.Stun(stunSeconds);
        DebugLog.Info($"[Enemy] 破势: {_owner.DebugTag} 霸体窗口内被削韧 {poiseDamage:F0} → 眩晕 {stunSeconds:F2}s");
        return true;
    }

    /// <summary>
    /// QTE 专用 Poise 伤害 — 仅 QTEController 在 QTEAttacking 态时调用
    /// interruptibleOnStun=true: Poise归零→触发Stun→QTEController中止QTE
    /// interruptibleOnStun=false: Poise归零→播放受击硬直动画，Poise重置，QTE继续
    /// </summary>
    public bool TakeQTEPoiseDamage(float poiseDamage, bool interruptibleOnStun)
    {
        if (_owner.state == EnemyState.Dead) return false;
        if (_owner.state != EnemyState.QTEAttacking) return false;

        _owner.currentPoise -= poiseDamage;
        _owner.OnPoiseChanged?.Invoke(_owner, _owner.currentPoise, _owner.maxPoise);

        if (_owner.currentPoise <= 0f)
        {
            _owner.currentPoise = 0f;
            if (interruptibleOnStun)
            {
                // 延迟Stun：等QTE动画播完再进入Stun，避免截断block动画和音效
                if (!_owner.deferredStun)
                {
                    _owner.deferredStun = true;
                    _owner.currentPoise = _owner.maxPoise;
                    _owner.OnPoiseChanged?.Invoke(_owner, _owner.currentPoise, _owner.maxPoise);
                    DebugLog.Info($"[Enemy] QTE破防延迟Stun: {_owner.DebugTag}");
                }
                return true;
            }
            else
            {
                // QTE 继续：仅播放受击硬直，不改变状态
                _owner.currentPoise = _owner.maxPoise;
                _owner.OnPoiseChanged?.Invoke(_owner, _owner.currentPoise, _owner.maxPoise);
                // QTE期间禁止发Hit trigger：Any State→HitFlash会劫持QTE动画状态机
                // 导致后续QTEBlocked/QTEFollowUp trigger被忽略
                return false;
            }
        }
        return false;
    }
}
