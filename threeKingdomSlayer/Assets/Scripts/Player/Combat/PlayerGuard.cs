using System;
using UnityEngine;

/// <summary>
/// 玩家的**防御破防值**（防御姿态期间累积的伤害条）。
///
/// 规则（2026-10-26 用户定）：
///   • 防御姿态期间**免伤**：伤害不扣血，只累积破防值（“真衰减条”）。
///   • 破防值**每帧按固定速率回落**（不再有“停手 N 秒才开始降”的阶段）→ 不挨打就自然回落。
///     阈值 / 回落速率 = 自然的记忆窗口长度（默认 120 / 30 每秒 ≈ 4 秒）。
///   • 破防值满 → **破防**：玩家进入硬直（不能行动 / 不能放技能 / 不能做手势输入），持续一小段时间。
///
/// 姿态（举枪 / 回落 / 防御保持）与武器表现都在 <see cref="PlayerWeaponStance"/>；本类只管破防值这条线。
/// 归属：由 <see cref="PlayerState"/> 持有（与 Enemy → EnemyPoise 的做法一致），不新增场景组件。
/// </summary>
public sealed class PlayerGuard
{
    /// <summary>破防相关状态（姿态状态在 PlayerWeaponStance）。</summary>
    public enum GuardState
    {
        /// <summary>未破防。</summary>
        None = 0,
        /// <summary>破防硬直中。</summary>
        Broken = 1,
    }

    private readonly PlayerState _owner;

    private float _breakTimer;

    public GuardState State { get; private set; } = GuardState.None;

    /// <summary>当前累积的破防值（= 衰减条当前长度）。</summary>
    public float AccumulatedDamage { get; private set; }

    /// <summary>
    /// 破防后**防御被锁住**多久（秒，2026-11-09 用户定）。
    ///
    /// 破防的惩罚不能只是 1.5 秒硬直：硬直期间玩法输入被门禁拦下，但**防御键是 UI** →
    /// 玩家可以在硬直里按一下，防御键的**预输入**会把这次按压排队 → 硬直一结束就**自动进防御**
    /// → 破防惩罚被抵消、防御变成免费护盾。
    /// → 所以破防后这段时间内**防御键直接按不了**（灰掉），玩家必须重新决定。
    /// </summary>
    public float DefenseLockSeconds = 3f;

    /// <summary>防御是否被锁住（破防后的一段时间内）→ 防御键灰掉、按不了。</summary>
    public bool IsDefenseLocked { get; private set; }
    private float _defenseLockTimer;

    /// <summary>锁住防御一段时间（破防时调用）。</summary>
    public void LockDefense(float seconds)
    {
        _defenseLockTimer = Mathf.Max(0f, seconds);
        IsDefenseLocked = _defenseLockTimer > 0f;
    }

    public bool IsBroken => State == GuardState.Broken;

    /// <summary>状态变化回调（表现层可订阅）。</summary>
    public event Action<GuardState> OnStateChanged;

    /// <summary>
    /// 防御期间吸收了伤害时广播（参数 = 本次吸收的伤害量）。
    /// 教学关的防御拍靠它判定"玩家成功防住了一下"（破防值累积、HP 不变）。
    /// </summary>
    public event Action<float> OnDamageAbsorbed;

    public PlayerGuard(PlayerState owner)
    {
        _owner = owner;
    }

    /// <summary>破防阈值（来自招架招式资产）。</summary>
    public float Threshold
    {
        get
        {
            var cfg = ParryConfig;
            return cfg != null ? Mathf.Max(1f, cfg.parryGuardBreakThreshold) : 120f;
        }
    }

    /// <summary>破防条的显示比例（0~1），供 HUD 用。</summary>
    public float MeterRatio => Mathf.Clamp01(AccumulatedDamage / Threshold);

    /// <summary>
    /// 破防压力的**定性档位**（2026-11-09）。
    ///
    /// ⚠️ **玩家看不到任何数字**（用户 2026-11-09：「我不想显示具体剩余的值」）：
    /// 档位只给**表现层**用 —— 枪被压低 / 枪尖下垂 / 持续微抖 / 红缨甩开；
    /// 玩家从**枪的样子**读状态，而不是从条子读数值。
    /// </summary>
    public enum StrainLevel
    {
        /// <summary>稳：还撑得住（表现与改造前完全一致）。</summary>
        Steady = 0,
        /// <summary>吃力：开始被压（枪下沉 + 红缨开始甩 + 格挡后退变大变慢）。</summary>
        Strained = 1,
        /// <summary>临界：快崩了（枪明显下沉下垂 + 持续抖 + 红缨狂甩 + 后退很大）。</summary>
        Critical = 2,
    }

    /// <summary>进入「吃力」的阈值（破防条比例）。</summary>
    public const float StrainedThreshold = 0.45f;

    /// <summary>进入「临界」的阈值（破防条比例）。</summary>
    public const float CriticalThreshold = 0.8f;

    /// <summary>当前破防压力档位（**只给表现层用，玩家看不到数值**）。</summary>
    public StrainLevel Strain
    {
        get
        {
            float r = MeterRatio;
            if (r >= CriticalThreshold) return StrainLevel.Critical;
            if (r >= StrainedThreshold) return StrainLevel.Strained;
            return StrainLevel.Steady;
        }
    }

    /// <summary>破防条当前是否应该显示（防御中才有值）。</summary>
    public bool MeterVisible => _owner != null && _owner.WeaponStance != null && _owner.WeaponStance.IsDefending;

    private AttackSkillConfig ParryConfig
    {
        get
        {
            var hero = _owner != null ? _owner.heroConfig : null;
            return hero != null ? hero.GetSkillConfig(AttackType.Parry) : null;
        }
    }

    // ── 伤害与破防 ─────────────────────────────────────────────

    /// <summary>
    /// 防御期间吸收伤害：返回 true 表示这次伤害被防御吃掉（不掉血，只累积破防值）。
    /// 非防御状态一律返回 false，由调用方照常结算。
    /// <paramref name="source">攻击者（用来做「格挡」反馈：攻击者被卡肉 + 镜头震动）。
    /// </summary>
    public bool TryAbsorbDamage(float damage, Enemy source = null, float guardBreakBonus = 0f)
    {
        if (State == GuardState.Broken) return false;

        float absorbed = Mathf.Max(0f, damage);
        AccumulatedDamage += absorbed + Mathf.Max(0f, guardBreakBonus);
        _owner?.WeaponStance?.Recoil(ParryConfig);
        // ★ 局部顿（命中定格）：命中帧把**持枪姿态**定住一小段（真实时间），世界照常跑
        //   ⇒ 画面摆出「刚被顶开」的那一帧，靠对比读出顿感（不动全局 Time.timeScale）。
        _owner?.WeaponStance?.ApplyHitStop(ParryConfig != null ? ParryConfig.parryGuardBlockHitStopSeconds : 0.033f);
        // 格挡金属音：与下面的卡肉/镜头震动同帧发生
        AudioManager.Instance?.PostEvent("Guard_Block");
        OnDamageAbsorbed?.Invoke(absorbed);

        // 格挡反馈（2026-10-26）：挡下一下必须有明确的画面反馈 ——
        // 攻击者被卡肉（这一下顿住）+ 镜头重击级震动。
        // 否则玩家只看到血条没掉、破防条涨一点，**感觉不到「挡住了」**。
        if (source != null)
        {
            source.ApplyHitStop(0.12f);
        }
        Vector3 blockPos = source != null ? source.transform.position
            : (_owner != null ? _owner.transform.position : Vector3.zero);
        CameraFeedbackController.Instance?.RequestGuardBlock(blockPos);

        if (AccumulatedDamage >= Threshold) Break(ParryConfig, blockPos, source);
        return true;
    }

    /// <summary>
    /// **破防**：硬直 + 防御锁 + 专属动作 + **演出包**（REQ-006）。
    ///
    /// 演出包（<c>design/guard-break-presentation-overhaul.md</c> §3.1 的目标时序）：
    ///   f0    命中帧：枪闪白 + 接触点火花 + 屏幕红边框 + 专属音 + 枪被顶开 + 攻击者被冻结
    ///   f0-3  定格 4 帧（局部顿，世界照常跑）
    ///   f4    攻击者转入慢放（被顶开/被打慢）+ 镜头极短推近
    ///   f19+  全部回正常速度，硬直开始
    /// ⚠ **不打断攻击者的攻击表现**（2026-11-13 用户拍板）⇒ 不切受击动画。
    /// </summary>
    private void Break(AttackSkillConfig cfg, Vector3 impactPos, Enemy source = null)
    {
        State = GuardState.Broken;
        _breakTimer = cfg != null ? cfg.parryGuardBreakDuration : 1.5f;
        // 破防 → 防御被锁住（防御键灰掉、按不了）：否则预输入会在硬直结束后自动进防御 → 破防等于没破。
        LockDefense(DefenseLockSeconds);
        // 破防：持枪姿态**被打脱手**（2026-11-12 用户定）
        //   ★ 不再走「收枪出画」（沿原路挥出 ⇒ 读起来像玩家自己把枪收走，不像被打爆）
        //   → 专属动作：以破防这一刻的姿态为起点，绕**世界 Z 轴逆时针旋转** + 轻微向左漂移出画。
        _owner?.WeaponStance?.BeginBreak();
        // ★ 局部顿（命中定格）：破防定格比普通格挡长一档（破防是更大的冲击）。
        //   只停**持枪姿态**，世界照常跑 ⇒ 靠对比读出顿感（不动全局 Time.timeScale）。
        _owner?.WeaponStance?.ApplyHitStop(cfg != null ? cfg.parryGuardBreakHitStopSeconds : 0.066f);
        // 破防镜头：比普通格挡**时长略长、幅度更小**（2026-11-12）——
        //   破防是「被顶开」，要读得出这一下；但幅度大 + 时长长会读成「地动山摇」。
        CameraFeedbackController.Instance?.RequestGuardBreak(impactPos);

        // ── REQ-006 · A1：攻击者「被顶住 → 被打慢」（不切受击动画）──
        if (source != null)
        {
            source.ApplyGuardBreakReaction(
                cfg != null ? cfg.parryGuardBreakEnemyFreezeSeconds : 0.25f,
                cfg != null ? cfg.parryGuardBreakEnemySlowSeconds : 0.35f,
                cfg != null ? cfg.parryGuardBreakEnemySlowScale : 0.25f);
        }

        // ── REQ-006 · A2：命中确认（画面上必须有人说「打中了」）──
        //  枪闪白
        _owner?.WeaponStance?.FlashGuardBreak(cfg != null ? cfg.parryGuardBreakWeaponFlashSeconds : 0.12f);
        //  接触点火花（复用现成通路；强度取 Heavy）
        if (source != null)
        {
            PixelHitEffectManager.Instance?.RequestHit(new HitFeedbackContext(
                source,
                DamageType.Stab,
                HitFeedbackSource.BasicAttack,
                HitFeedbackStrength.Heavy,
                0f,
                false,
                false,
                impactPos,
                Vector3.zero,
                true));
        }
        //  屏幕红边框（PlayerHitFeedback 挂在 Player 根物体上，而 PlayerState 在其子物体 ⇒ 必须往父级找）
        if (_owner != null)
        {
            _owner.GetComponentInParent<PlayerHitFeedback>()?.TriggerGuardBreakFlash(
                cfg != null ? cfg.parryGuardBreakScreenFlashSeconds : 0.25f);
        }

        // ── REQ-006 · A3：收尾（专属音 + 镜头推近）──
        AudioManager.Instance?.PostEvent("Guard_Break",
            cfg != null ? cfg.parryGuardBreakSoundPitch : 0.75f,
            cfg != null ? cfg.parryGuardBreakSoundVolume : 1.4f);
        CameraFeedbackController.Instance?.RequestGuardBreakPush(
            cfg != null ? cfg.parryGuardBreakPushDegrees : 2f);

        // ── REQ-006 · 无双口径：破防那一拍 = **世界定格 + 径向模糊** ──
        //   ★ 子弹时间不在这里开：命中这一帧保持 1× 速度（冲击要脆），
        //     子弹时间挂在**脱手旋转的那一刻**（见 PlayerWeaponStance），
        //     否则后坐会把窗口耗光、旋转等窗口结束才开 ⇒ 读成「弹射」。
        //   慢的是「别人」（敌人被压住、拖成流线）；我方的枪在子弹时间里**一起慢放**（用户口径）。

        GameplayInputGate.RequestBlock(this, InputBlockReason.GuardBroken);
        DebugLog.Info($"[Guard] 破防！硬直 {_breakTimer:F2}s（累积伤害 {AccumulatedDamage:F0}）");
        OnStateChanged?.Invoke(State);
    }

    // ── 每帧推进 ───────────────────────────────────────────────

    public void Tick(float deltaTime)
    {
        // 破防值：真衰减条 —— 每帧固定回落（不挨打就自然掉）
        float perSecond = ParryConfig != null ? ParryConfig.parryGuardDecayPerSecond : 30f;
        if (AccumulatedDamage > 0f && perSecond > 0f)
            AccumulatedDamage = Mathf.Max(0f, AccumulatedDamage - perSecond * deltaTime);

        // 防御锁：破防后的一段时间内防御用不了（防御键灰掉）
        if (IsDefenseLocked)
        {
            _defenseLockTimer -= deltaTime;
            if (_defenseLockTimer <= 0f)
            {
                _defenseLockTimer = 0f;
                IsDefenseLocked = false;
            }
        }

        if (State != GuardState.Broken) return;

        _breakTimer -= deltaTime;
        if (_breakTimer > 0f) return;

        State = GuardState.None;
        AccumulatedDamage = 0f;
        GameplayInputGate.Release(this);
        OnStateChanged?.Invoke(State);
    }

    /// <summary>场景重置 / 死亡等：强制清空（含破防硬直与输入门）。</summary>
    public void ForceReset()
    {
        State = GuardState.None;
        AccumulatedDamage = 0f;
        _breakTimer = 0f;
        IsDefenseLocked = false;
        _defenseLockTimer = 0f;
        GameplayInputGate.Release(this);
    }
}
