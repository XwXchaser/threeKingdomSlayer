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
    public bool TryAbsorbDamage(float damage, Enemy source = null)
    {
        if (State == GuardState.Broken) return false;

        float absorbed = Mathf.Max(0f, damage);
        AccumulatedDamage += absorbed;
        _owner?.WeaponStance?.Recoil(ParryConfig);
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

        if (AccumulatedDamage >= Threshold) Break(ParryConfig);
        return true;
    }

    private void Break(AttackSkillConfig cfg)
    {
        State = GuardState.Broken;
        _breakTimer = cfg != null ? cfg.parryGuardBreakDuration : 1.5f;
        // 破防：持枪姿态收掉 + 拦下全部玩法输入
        _owner?.WeaponStance?.Reset();
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
        GameplayInputGate.Release(this);
    }
}
