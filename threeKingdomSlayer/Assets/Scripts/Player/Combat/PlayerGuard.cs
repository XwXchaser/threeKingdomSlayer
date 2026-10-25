using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 玩家的**防御姿态**（招架后摇期间长按进入）。
///
/// 规则（2026-10-25 用户定）：
///   • 入口：只在**招架后摇**（招架的动作锁窗口）期间长按进入；进入后只要一直按着就维持。
///   • 防御期间**免伤**：受到的伤害不扣血，只累积「破防值」。
///   • 破防值超过阈值 → **破防**：进入硬直（不能行动 / 不能放技能 / 不能做手势输入），持续一小段时间。
///   • 防御中滑动不出招：必须先松手退出防御，再重新操作。
///
/// 数值与武器 Prefab 全部来自**招架招式资产**（<c>AttackSkillConfig</c> 的 parryGuard* 字段）
/// → Inspector 可调、无幽灵引用；招架没装配时整套防御不可用。
///
/// 归属：由 <see cref="PlayerState"/> 持有（与 Enemy → EnemyPoise 的做法一致），不新增场景组件。
/// </summary>
public sealed class PlayerGuard
{
    /// <summary>防御状态机。</summary>
    public enum GuardState
    {
        /// <summary>未防御。</summary>
        None = 0,
        /// <summary>防御姿态中：免伤，累积破防值。</summary>
        Guarding = 1,
        /// <summary>破防硬直中：不能行动 / 技能 / 手势。</summary>
        Broken = 2,
    }

    private readonly PlayerState _owner;

    private GameObject _weaponInstance;
    private Transform _weaponTransform;
    private Vector3 _weaponHomePosition;
    private float _breakTimer;
    private float _timeSinceHit;

    public GuardState State { get; private set; } = GuardState.None;

    /// <summary>防御期间累积的伤害（破防值）。</summary>
    public float AccumulatedDamage { get; private set; }

    public bool IsGuarding => State == GuardState.Guarding;
    public bool IsBroken => State == GuardState.Broken;

    /// <summary>状态变化回调（表现层可订阅）。</summary>
    public event Action<GuardState> OnStateChanged;

    public PlayerGuard(PlayerState owner)
    {
        _owner = owner;
    }

    /// <summary>招架招式配置（防御的全部数值与武器来源都挂在它上面）。</summary>
    private AttackSkillConfig ParryConfig
    {
        get
        {
            var hero = _owner != null ? _owner.heroConfig : null;
            return hero != null ? hero.GetSkillConfig(AttackType.Parry) : null;
        }
    }

    // ── 进入 / 退出 ─────────────────────────────────────────────

    /// <summary>尝试进入防御姿态（由输入层在「招架后摇期间长按」时调用）。</summary>
    public bool TryEnter()
    {
        if (State != GuardState.None) return false;
        var cfg = ParryConfig;
        if (cfg == null) return false;   // 没装配招架 → 没有防御

        State = GuardState.Guarding;
        AccumulatedDamage = 0f;
        _timeSinceHit = 0f;
        SpawnWeapon(cfg);
        OnStateChanged?.Invoke(State);
        return true;
    }

    /// <summary>退出防御（松手 / 破防 / 场景重置）。</summary>
    public void Exit()
    {
        if (State == GuardState.None) return;
        if (State == GuardState.Guarding)
        {
            State = GuardState.None;
            AccumulatedDamage = 0f;
            DespawnWeapon();
            OnStateChanged?.Invoke(State);
            return;
        }
        // 破防硬直中：硬直没走完不许提前退出
    }

    /// <summary>场景重置 / 死亡等：强制清空（含破防硬直与输入门）。</summary>
    public void ForceReset()
    {
        State = GuardState.None;
        AccumulatedDamage = 0f;
        _breakTimer = 0f;
        _timeSinceHit = 0f;
        DespawnWeapon();
        GameplayInputGate.Release(this);
    }

    // ── 伤害与破防 ─────────────────────────────────────────────

    /// <summary>
    /// 防御期间吸收伤害：返回 true 表示这次伤害被防御吃掉（不掉血，只累积破防值）。
    /// 非防御状态一律返回 false，由调用方照常结算。
    /// </summary>
    public bool TryAbsorbDamage(float damage)
    {
        if (State != GuardState.Guarding) return false;

        AccumulatedDamage += Mathf.Max(0f, damage);
        _timeSinceHit = 0f;
        PlayRecoil();

        var cfg = ParryConfig;
        float threshold = cfg != null ? cfg.parryGuardBreakThreshold : 120f;
        if (threshold > 0f && AccumulatedDamage >= threshold) Break(cfg);
        return true;
    }

    private void Break(AttackSkillConfig cfg)
    {
        State = GuardState.Broken;
        _breakTimer = cfg != null ? cfg.parryGuardBreakDuration : 1.5f;
        DespawnWeapon();
        // 破防硬直：拦下全部玩法输入（手势 / 技能）
        GameplayInputGate.RequestBlock(this, InputBlockReason.GuardBroken);
        DebugLog.Info($"[Guard] 破防！硬直 {_breakTimer:F2}s（累积伤害 {AccumulatedDamage:F0}）");
        OnStateChanged?.Invoke(State);
    }

    // ── 每帧推进 ───────────────────────────────────────────────

    public void Tick(float deltaTime)
    {
        switch (State)
        {
            case GuardState.Guarding:
                TickGuarding(deltaTime);
                break;
            case GuardState.Broken:
                TickBroken(deltaTime);
                break;
        }
    }

    private void TickGuarding(float deltaTime)
    {
        var cfg = ParryConfig;
        float delay = cfg != null ? cfg.parryGuardDecayDelay : 3f;
        float perSecond = cfg != null ? cfg.parryGuardDecayPerSecond : 30f;
        if (perSecond <= 0f) return;

        _timeSinceHit += deltaTime;
        if (AccumulatedDamage <= 0f) return;
        if (_timeSinceHit < delay) return;   // 连续挨打时不衰减

        AccumulatedDamage = Mathf.Max(0f, AccumulatedDamage - perSecond * deltaTime);
    }

    private void TickBroken(float deltaTime)
    {
        _breakTimer -= deltaTime;
        if (_breakTimer > 0f) return;

        State = GuardState.None;
        AccumulatedDamage = 0f;
        GameplayInputGate.Release(this);
        OnStateChanged?.Invoke(State);
    }

    // ── 武器表现 ───────────────────────────────────────────────

    private void SpawnWeapon(AttackSkillConfig cfg)
    {
        DespawnWeapon();
        GameObject prefab = cfg.parryGuardWeaponPrefab != null ? cfg.parryGuardWeaponPrefab : cfg.attackWavePrefab;
        if (prefab == null) return;

        Vector3 basePos = _owner != null ? _owner.transform.position : Vector3.zero;
        Vector3 pos = basePos + new Vector3(cfg.parryGuardWeaponOffsetX, cfg.parryGuardWeaponOffsetY, cfg.parryGuardWeaponOffsetZ);

        // 先用 prefab 自身朝向，再绕 Y 旋到横举（枪身长轴在 prefab 里朝 Z），最后加一点倾角
        Quaternion rot = Quaternion.Euler(0f, cfg.parryGuardWeaponYawDegrees, 0f) * prefab.transform.rotation;
        rot = rot * Quaternion.Euler(0f, 0f, cfg.parryGuardWeaponTiltDegrees);

        _weaponInstance = UnityEngine.Object.Instantiate(prefab, pos, rot);
        _weaponInstance.name = "GuardWeapon";
        _weaponInstance.transform.SetParent(null);

        var collider = _weaponInstance.GetComponent<Collider>();
        if (collider != null) UnityEngine.Object.Destroy(collider);
        var collider2D = _weaponInstance.GetComponent<Collider2D>();
        if (collider2D != null) UnityEngine.Object.Destroy(collider2D);

        var meshVisual = _weaponInstance.GetComponentInChildren<MeshWeaponVisual>();
        if (meshVisual != null) meshVisual.SetSorting(2);

        _weaponTransform = _weaponInstance.transform;
        _weaponHomePosition = _weaponTransform.position;
    }

    private void DespawnWeapon()
    {
        // 先杀掉后摇 tween：武器销毁后 tween 继续写已销毁的 Transform 会抛 MissingReferenceException
        if (_weaponTransform != null) _weaponTransform.DOKill();
        if (_weaponInstance != null) UnityEngine.Object.Destroy(_weaponInstance);
        _weaponInstance = null;
        _weaponTransform = null;
    }

    /// <summary>受击：武器先后退再回位（赵云式举枪防御的后仰）。</summary>
    private void PlayRecoil()
    {
        if (_weaponTransform == null) return;
        var cfg = ParryConfig;
        if (cfg == null) return;

        float back = cfg.parryGuardRecoilDistance;
        float seconds = Mathf.Max(0.05f, cfg.parryGuardRecoilSeconds);

        Vector3 home = _weaponHomePosition;
        Vector3 backPos = home + Vector3.back * back;

        _weaponTransform.DOKill();
        _weaponTransform.DOMove(backPos, seconds * 0.35f).SetEase(Ease.OutQuad)
            .SetTarget(_weaponTransform);
        _weaponTransform.DOMove(home, seconds * 0.65f).SetEase(Ease.OutCubic)
            .SetDelay(seconds * 0.35f).SetTarget(_weaponTransform);
    }
}
