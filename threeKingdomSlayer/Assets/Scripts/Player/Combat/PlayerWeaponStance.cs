using System;
using UnityEngine;

/// <summary>
/// 玩家的**常驻持枪实体**：招架 → 防御姿态 的同一条动作链。
///
/// 为什么要有它（2026-10-26）：
///   原来的招架是一个"自播自毁"的独立特效（ParryVisualEffect），防御姿态又另起一个实例，
///   于是场上同时有两把枪、两套深度、两套姿态基准 → parry 与 defend 之间是跳变。
///   现在改成：招架 = 持枪实体播"挥入"动作；防御 = 同一实例沿同一条路径落位。
///
/// 动作链（用户定的规则）：
///   招架 → **挥入**（从屏外斜劈入画 → 斜举位；完整播完，预输入不切断它）
///     → 过冲 → 回弹 → 定格（格住的那一下）
///       ├─ 玩家按住 → 立刻接防御位（同角度、位置略低）→ 防御态
///       │     ├─ 松手 → **收枪**：沿原路挥出画面 → 出画后销毁
///       │     └─ 破防 → 枪被打脱手，同样挥出画面
///       └─ 玩家没按 → **收枪**：沿原路挥出画面 → 出画后销毁
///
/// 三条硬规则（2026-10-26 用户定）：
///   1) **预输入只是"提前按住"**：本质仍先播完 parry 再回防御位。挥入中一按住就切防御位 = BUG。
///   2) **武器必须进出画面**：进入从屏外挥入，离开（收枪 / 破防）要挥出画面后才销毁，
///      不允许在画面里原地消失（准则见 design/combo-move-state-machine.md §19）。
///   3) **成功格挡 → 收尾加速；没成功 → 正常速度**（沿用旧版招架的规则）。
///
/// 姿态的表达：
///   不用手调欧拉角，而是「枪尖指向 + 矛头落点」：
///     rot = FromToRotation(枪身本地 +Y, 目标方向)；根节点 = 矛头 − (Length/2)·方向。
///   注意：prefab 的根节点在枪身**中段**（不是枪尾）→ 矛头距根 = MeshWeaponVisual.Length/2。
/// </summary>
public sealed class PlayerWeaponStance
{
    public enum StancePhase
    {
        /// <summary>没有持枪（未招架 / 已收枪）。</summary>
        Idle = 0,
        /// <summary>挥入中（招架动作进行中）：从屏外斜劈到斜举位，含过冲/回弹/定格。</summary>
        SwingIn = 1,
        /// <summary>防御姿态（按住维持）。</summary>
        Defending = 2,
        /// <summary>收枪中：挥出画面，出画后销毁。</summary>
        Exiting = 3,
    }

    private enum EaseMode
    {
        Smooth,
        Out,
    }

    private readonly PlayerState _owner;

    private GameObject _weapon;
    private Transform _weaponTransform;
    private float _weaponBaseScale = 1f;
    private float _weaponLength = 10.8f;   // 整杆枪的世界长度（scale=1 时）
    private float _tipOffset = 5.4f;      // 根节点 → 矛头（= Length/2，根在枪身中段）
    private float _stanceScale = 1f;

    // 当前姿态与目标姿态（位置 = 根节点相对玩家的偏移；旋转 = 世界旋转）
    private Vector3 _rootFrom, _rootTo;
    private Quaternion _poseFrom, _poseTo;
    private float _moveT;
    private float _moveDuration = 0.2f;
    private bool _moving;
    private EaseMode _ease = EaseMode.Smooth;
    private Action _onArrive;

    // 姿态集合
    private Vector3 _rootAnchor;        // 杆尾（手的位置）
    private Vector3 _defendRootOffset;  // 防御位相对杆尾的下沉量（**只用位置差，不用角度差**）
    private Quaternion _poseIdle, _poseEntry, _poseOvershoot;
    private AttackSkillConfig _cfg;

    private float _recoilAmount;
    private Vector3 _recoilOffset;
    private float _recoilTimer;
    private float _recoilTotal;

    private bool _holding;
    private bool _holdRequested;     // 预输入：挥入期间记住"玩家按着"
    private bool _parrySucceeded;     // 本次招架是否真的格开了什么 → 决定收尾速度

    public StancePhase Phase { get; private set; } = StancePhase.Idle;

    /// <summary>当前是否处于防御姿态（挥入播完 + 按住维持）。</summary>
    public bool IsDefending => Phase == StancePhase.Defending;

    public PlayerWeaponStance(PlayerState owner)
    {
        _owner = owner;
    }

    // ── 对外接口 ───────────────────────────────────────────────

    /// <summary>
    /// 招架：挥入（从屏外斜劈入画 → 斜举位）。
    /// <paramref name="succeeded"/> = 这次是否真的格开了攻击 → 决定收尾是加速还是正常速度。
    /// </summary>
    public void PlayParry(AttackSkillConfig cfg, bool succeeded)
    {
        if (cfg == null) return;
        _cfg = cfg;
        _parrySucceeded = succeeded;
        EnsureWeapon(cfg);
        BuildPoses(cfg);

        _holdRequested = _holding;   // 招架前就按着 = 预输入
        Phase = StancePhase.SwingIn;

        // 起点在屏外（矛头出画），动作从这里挥入
        _rootFrom = _rootAnchor;
        _rootTo = _rootAnchor;
        _poseFrom = _poseEntry;
        _poseTo = _poseEntry;
        ApplyPose(1f);

        StartSwingIn();
    }

    /// <summary>受击：武器先后退再回位。</summary>
    public void Recoil(AttackSkillConfig cfg)
    {
        if (_weaponTransform == null || cfg == null) return;
        _recoilTotal = Mathf.Max(0.05f, cfg.parryGuardRecoilSeconds);
        _recoilTimer = _recoilTotal;
        _recoilAmount = cfg.parryGuardRecoilDistance;
    }

    /// <summary>收枪（立即结束）：破防 / 场景重置用。</summary>
    public void Reset()
    {
        Phase = StancePhase.Idle;
        _moving = false;
        _holding = false;
        _holdRequested = false;
        _recoilTimer = 0f;
        if (_weapon != null) UnityEngine.Object.Destroy(_weapon);
        _weapon = null;
        _weaponTransform = null;
    }

    /// <summary>每帧推进。</summary>
    public void Tick(float deltaTime, bool pointerHeld)
    {
        if (_weaponTransform == null) return;
        _holding = pointerHeld;

        // 受击后退的衰减
        if (_recoilTimer > 0f)
        {
            _recoilTimer -= deltaTime;
            float k = _recoilTimer > 0f ? Mathf.Clamp01(_recoilTimer / _recoilTotal) : 0f;
            _recoilOffset = new Vector3(0f, 0f, -_recoilAmount * k);
            if (_recoilTimer <= 0f) _recoilOffset = Vector3.zero;
        }

        // 预输入：挥入期间只**记住**按住，不切断动作（规则 1）
        if (Phase == StancePhase.SwingIn && _holding) _holdRequested = true;

        if (_moving)
        {
            _moveT += deltaTime / Mathf.Max(0.01f, _moveDuration);
            float t = Mathf.Clamp01(_moveT);
            ApplyPose(t);
            if (t >= 1f)
            {
                _moving = false;
                var arrived = _onArrive;
                _onArrive = null;
                arrived?.Invoke();
            }
        }
        else
        {
            ApplyPose(1f);
        }

        // 防御态：松手 → 收枪
        if (Phase == StancePhase.Defending && !_holding)
            BeginExit();

        // 收枪途中按住 → 接住 → 回防御位
        if (Phase == StancePhase.Exiting && _holding)
            EnterDefend();
    }

    // ── 动作分段 ───────────────────────────────────────────────

    /// <summary>挥入：斜劈入画 → 过冲 → 回弹 → 定格 → 分流。</summary>
    private void StartSwingIn()
    {
        float swingIn = _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceSwingInSeconds) : 0.18f;
        float settle = _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceSettleSeconds) : 0.08f;
        float hold = _cfg != null ? Mathf.Max(0f, _cfg.parryStanceHoldSeconds) : 0.10f;

        MoveTo(_rootAnchor, _poseIdle, swingIn, EaseMode.Out, onArrived: () =>
        {
            // 过冲：冲过目标角再收回来（“力度”的来源）
            MoveTo(_rootAnchor, _poseOvershoot, settle * 0.4f, EaseMode.Smooth, onArrived: () =>
            {
                MoveTo(_rootAnchor, _poseIdle, settle * 0.6f, EaseMode.Smooth, onArrived: () =>
                {
                    // 定格：格住的那一下（保持姿势不动）
                    MoveTo(_rootAnchor, _poseIdle, hold, EaseMode.Smooth, onArrived: AfterSwingIn);
                });
            });
        });
    }

    /// <summary>挥入播完后的分流：按着 → 防御位；没按 → 收枪出画。</summary>
    private void AfterSwingIn()
    {
        if (_holdRequested || _holding) EnterDefend();
        else BeginExit();
    }

    /// <summary>进入防御位：角度与斜举位一致（读数一致），只用位置差表达“稍低于 idle”。</summary>
    private void EnterDefend()
    {
        Phase = StancePhase.Defending;
        float dur = _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceDefendEnterSeconds) : 0.12f;
        MoveTo(_rootAnchor + _defendRootOffset, _poseIdle, dur, EaseMode.Smooth);
    }

    /// <summary>收枪：沿原路挥出画面 → 出画后才销毁。</summary>
    private void BeginExit()
    {
        Phase = StancePhase.Exiting;
        float dur = _cfg != null
            ? (_parrySucceeded ? Mathf.Max(0.05f, _cfg.parryStanceExitFastSeconds) : Mathf.Max(0.05f, _cfg.parryStanceExitNormalSeconds))
            : 0.3f;
        MoveTo(_rootAnchor, _poseEntry, dur, EaseMode.Out, onArrived: () => Reset());
    }

    // ── 内部实现 ───────────────────────────────────────────────

    private void MoveTo(Vector3 root, Quaternion pose, float duration, EaseMode ease = EaseMode.Smooth, Action onArrived = null)
    {
        _rootFrom = CurrentRoot();
        _poseFrom = CurrentPose();
        _rootTo = root;
        _poseTo = pose;
        _moveT = 0f;
        _moveDuration = Mathf.Max(0.01f, duration);
        _ease = ease;
        _moving = true;
        _onArrive = onArrived;
    }

    private Vector3 CurrentRoot() => Vector3.Lerp(_rootFrom, _rootTo, Mathf.Clamp01(_moveT));
    private Quaternion CurrentPose() => Quaternion.Slerp(_poseFrom, _poseTo, Mathf.Clamp01(_moveT));

    private Vector3 PlayerPosition => _owner != null ? _owner.transform.position : Vector3.zero;

    /// <summary>应用当前姿态：根节点位置 + 枪身指向。</summary>
    private void ApplyPose(float t)
    {
        if (_weaponTransform == null) return;
        float e = _ease == EaseMode.Out ? 1f - Mathf.Pow(1f - t, 3f) : t * t * (3f - 2f * t);
        Vector3 root = Vector3.Lerp(_rootFrom, _rootTo, e);
        Quaternion pose = Quaternion.Slerp(_poseFrom, _poseTo, e);
        _weaponTransform.position = PlayerPosition + root + _recoilOffset;
        _weaponTransform.rotation = pose;
    }

    /// <summary>
    /// 由招式资产推导姿态：矛头落点 → 杆尾（手的位置）；三种指向：起手（屏外）/ 斜举 / 过冲。
    /// </summary>
    private void BuildPoses(AttackSkillConfig cfg)
    {
        Vector3 headAnchor = cfg.parryStanceHeadAnchor;
        _poseIdle = PoseForAngle(cfg.parryStanceTipDegrees);
        _poseEntry = PoseForAngle(cfg.parryStanceEntryAngleDegrees);
        _poseOvershoot = PoseForAngle(cfg.parryStanceTipDegrees + cfg.parryStanceOvershootDegrees);

        // 根节点（手的位置）= 矛头 − (Length/2)·斜举方向
        Vector3 dir = _poseIdle * Vector3.up;
        _rootAnchor = headAnchor - dir * (_tipOffset * _stanceScale);

        // 防御位：只用位置差（角度与斜举位一致 → 枪尖上扬角每次都读得一样）
        _defendRootOffset = new Vector3(0f, -cfg.parryStanceDefendAnchorDrop, 0f);
    }

    /// <summary>由「枪尖指向的角度」反解世界旋转：0 = 指向屏幕右(+X)，90 = 竖直向上。</summary>
    private Quaternion PoseForAngle(float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
        return Quaternion.FromToRotation(Vector3.up, dir);
    }

    private void EnsureWeapon(AttackSkillConfig cfg)
    {
        GameObject prefab = cfg.parryGuardWeaponPrefab != null ? cfg.parryGuardWeaponPrefab : cfg.attackWavePrefab;
        if (prefab == null) return;

        if (_weapon == null)
        {
            _weapon = UnityEngine.Object.Instantiate(prefab);
            _weapon.name = "StanceWeapon";
            _weapon.transform.SetParent(null);
            foreach (var col in _weapon.GetComponentsInChildren<Collider>()) UnityEngine.Object.Destroy(col);
            foreach (var col2 in _weapon.GetComponentsInChildren<Collider2D>()) UnityEngine.Object.Destroy(col2);
            var meshVisual = _weapon.GetComponentInChildren<MeshWeaponVisual>();
            if (meshVisual != null) meshVisual.SetSorting(2);
            _weaponBaseScale = _weapon.transform.localScale.x;

            // 枪长：与 Stab 判定用的是同一份（10.8 = 整杆长度，scale=1）
            var dynamics = _weapon.GetComponentInChildren<WeaponTasselDynamics>();
            if (dynamics != null && dynamics.WeaponLength > 0f) _weaponLength = dynamics.WeaponLength;
            else if (meshVisual != null && meshVisual.Length > 0f) _weaponLength = meshVisual.Length;
            _tipOffset = _weaponLength * 0.5f;   // 根在枪身中段 → 矛头距根 = 长度的一半

            // 红缨物理：持枪时收敛摆幅（让枪头的视觉角度稳定）
            if (dynamics != null)
                dynamics.SetSettle(cfg.parryStanceTasselDamping, cfg.parryStanceTasselMaxBend);
        }

        _weaponTransform = _weapon.transform;
        // 姿态由 FromToRotation 直接控制 → 用 identity 让模型本地 +Y 就是枪身长轴
        _weaponTransform.localRotation = Quaternion.identity;
        _stanceScale = Mathf.Max(0.05f, cfg.parryStanceScale);
        float s = _weaponBaseScale * _stanceScale;
        _weaponTransform.localScale = new Vector3(s, s, s);
    }
}
