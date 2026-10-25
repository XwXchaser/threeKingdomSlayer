using System;
using UnityEngine;

/// <summary>
/// 玩家的**常驻持枪实体**：招架 → 防御姿态 的同一条动作链。
///
/// 为什么要有它（2026-10-26）：
///   原来的招架是一个"自播自毁"的独立特效（ParryVisualEffect），防御姿态又另起一个实例，
///   于是场上同时有两把枪、两套深度、两套姿态基准 → parry 与 defend 之间是跳变，没有连贯性。
///   现在改成：招架 = 持枪实体播"举枪"动作；防御 = 同一实例沿同一条路径落位/回落。
///
/// 姿态的表达（2026-10-26 修正，两条硬约束）：
///   1) **锚点是矛头（枪尖），不是枪身根节点**。prefab 的根在**枪尾**，枪沿本地 +Y 延伸
///      <c>MeshWeaponVisual.Length</c>（10.8）个单位 → 根节点 = 矛头 − 长度·方向。
///      之前用根节点当锚点，等于把"枪尾"当成"手的位置"，位置关系全错。
///   2) **深度在前景带**（z ≈ -5）。本游戏所有武器视觉都在相机与玩家之间：Stab 起点 z=-5.5、
///      Slash 生成点 z=-3.6、原版 Parry 枢轴 z=-6（相机 z=-10、玩家 z=0、敌人 z=+9）。
///      之前把枪放在玩家深度（z=+0.4）→ 投影落在屏幕中线偏上（敌人所在高度带），
///      读起来就是"一根枪飞到敌人面前"。
///
/// 方向：用「枪尖指向」反解旋转 —— <c>rot = FromToRotation(枪身本地 +Y, 目标方向)</c>。
///   参考图（《起源》第三人称背面视角 = 我们相机视角）实测：枪尖在画面右上 ≈42°；
///   相机屏幕右 = +X，玩家面朝 +Z（角色右手在 +X 侧）→ 与参考图同侧，不需要镜像。
///
/// 动作链（按玩家定的规则）：
///   招架 → 举枪（Entry → Idle）
///     · 玩家按住      → 落到 DefendHold（比 Idle 略低）→ 防御态
///     · 玩家没按住    → 沿原路回落（Idle → Entry）→ 回落途中按住 → 接住 → 落 DefendHold
///     · 回落走完仍未按住 → 收枪，parry 结束
/// </summary>
public sealed class PlayerWeaponStance
{
    public enum StancePhase
    {
        /// <summary>没有持枪（未招架 / 已收枪）。</summary>
        Idle = 0,
        /// <summary>举枪中（Entry → Idle）。</summary>
        Raising = 1,
        /// <summary>举枪到位，等待"按住 / 不按住"分流。</summary>
        Settled = 2,
        /// <summary>回落中（Idle/Defend → Entry）。</summary>
        Returning = 3,
    }

    private readonly PlayerState _owner;

    private GameObject _weapon;
    private Transform _weaponTransform;
    private float _weaponBaseScale = 1f;
    private float _weaponLength = 10.8f;   // 枪身长度（世界单位，scale=1 时）
    /// <summary>
    /// 「根节点 → 矛头」的距离 = 枪身长度的一半。
    /// 依据：根节点在枪身**中段**（Stab 视觉就是用 <c>Length*0.5</c> 把模型往后推，让矛头落在射线末端），
    /// 且 <c>MeshWeaponVisual.Length</c> = 10.8 就是整杆枪的世界长度。
    /// （SkinnedMeshRenderer 的包围盒在 100×11.05 的缩放链下虚高 ~20%，不可直接用。）
    /// </summary>
    private float _tipOffset = 5.4f;     // 根节点 → 矛头（= Length/2）
    private float _stanceScale = 1f;

    // 当前姿态与目标姿态（位置 = **杆尾锚点**（= 手的位置），相对玩家；旋转 = 世界旋转）
    private Vector3 _rootFrom, _rootTo;
    private Quaternion _poseFrom, _poseTo;
    private float _moveT;
    private float _moveDuration = 0.2f;
    private bool _moving;

    // 姿态集合（由招式资产推导）
    private Vector3 _rootAnchor;      // 杆尾（手的位置）——由“矛头锚点 + 斜举方向”反推
    private Vector3 _headAnchor;      // 矛头在斜举位时的落点（用户指定的画面位置）
    private Quaternion _poseIdle, _poseEntry, _poseDefend;
    private AttackSkillConfig _cfg;

    private float _recoilAmount;
    private Vector3 _recoilOffset;
    private float _recoilTimer;
    private float _recoilTotal;

    private bool _atDefend;
    private Action _onArrive;
    private bool _holding;

    public StancePhase Phase { get; private set; } = StancePhase.Idle;

    /// <summary>当前是否处于防御姿态（举枪完成后、按住维持）。</summary>
    public bool IsDefending => Phase == StancePhase.Settled && _holding && _atDefend;

    public PlayerWeaponStance(PlayerState owner)
    {
        _owner = owner;
    }

    // ── 对外接口 ───────────────────────────────────────────────

    /// <summary>招架：举枪（Entry → Idle）。</summary>
    public void PlayParry(AttackSkillConfig cfg)
    {
        if (cfg == null) return;
        _cfg = cfg;
        EnsureWeapon(cfg);
        BuildPoses(cfg);

        Phase = StancePhase.Raising;
        _rootFrom = _rootAnchor;
        _rootTo = _rootAnchor;
        _poseFrom = _poseEntry;
        _poseTo = _poseEntry;
        ApplyPose(1f);

        MoveTo(_rootAnchor, _poseIdle, Mathf.Max(0.05f, cfg.parryStanceRaiseSeconds), onArrived: () =>
        {
            if (Phase == StancePhase.Raising) Phase = StancePhase.Settled;
        });
    }

    /// <summary>受击：武器先后退再回位。</summary>
    public void Recoil(AttackSkillConfig cfg)
    {
        if (_weaponTransform == null || cfg == null) return;
        _recoilTotal = Mathf.Max(0.05f, cfg.parryGuardRecoilSeconds);
        _recoilTimer = _recoilTotal;
        _recoilAmount = cfg.parryGuardRecoilDistance;
    }

    /// <summary>收枪：立即结束（破防 / 场景重置用）。</summary>
    public void Reset()
    {
        Phase = StancePhase.Idle;
        _moving = false;
        _holding = false;
        _atDefend = false;
        _recoilTimer = 0f;
        if (_weapon != null) UnityEngine.Object.Destroy(_weapon);
        _weapon = null;
        _weaponTransform = null;
    }

    /// <summary>每帧推进：按"手指是否按住"决定去防御位还是回落。</summary>
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

        if (_moving)
        {
            _moveT += deltaTime / Mathf.Max(0.01f, _moveDuration);
            float t = Mathf.Clamp01(_moveT);
            float eased = t * t * (3f - 2f * t);   // smoothstep：起步收尾都软
            ApplyPose(eased);
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

        // 分流：举枪到位后，按住 → 防御位；没按住 → 回落
        if (Phase == StancePhase.Settled || Phase == StancePhase.Returning)
        {
            if (_holding)
            {
                if (!_atDefend)
                {
                    _atDefend = true;
                    Phase = StancePhase.Settled;
                    MoveTo(_rootAnchor, _poseDefend, _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceDefendEnterSeconds) : 0.15f);
                }
            }
            else
            {
                if (_atDefend || Phase == StancePhase.Settled)
                {
                    _atDefend = false;
                    Phase = StancePhase.Returning;
                    MoveTo(_rootAnchor, _poseEntry, _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceReturnSeconds) : 0.8f, onArrived: () =>
                    {
                        // 回落走完仍未按住 → parry 结束，收枪
                        if (!_holding) Reset();
                    });
                }
            }
        }
    }

    // ── 内部实现 ───────────────────────────────────────────────

    private void MoveTo(Vector3 root, Quaternion pose, float duration, Action onArrived = null)
    {
        _rootFrom = CurrentRoot();
        _poseFrom = CurrentPose();
        _rootTo = root;
        _poseTo = pose;
        _moveT = 0f;
        _moveDuration = Mathf.Max(0.01f, duration);
        _moving = true;
        _onArrive = onArrived;
    }

    private Vector3 CurrentRoot() => Vector3.Lerp(_rootFrom, _rootTo, Mathf.Clamp01(_moveT));
    private Quaternion CurrentPose() => Quaternion.Slerp(_poseFrom, _poseTo, Mathf.Clamp01(_moveT));

    private Vector3 PlayerPosition => _owner != null ? _owner.transform.position : Vector3.zero;

    /// <summary>
    /// 应用当前姿态：**杆尾锚点**（手的位置）+ 枪尖指向 → 枪身从杆尾沿方向延伸。
    /// 三种姿态共用同一个杆尾锚点 → 举枪 = 绕着“手”把矛头从右下扇形扫到右上。
    /// </summary>
    private void ApplyPose(float t)
    {
        if (_weaponTransform == null) return;
        Vector3 root = Vector3.Lerp(_rootFrom, _rootTo, t);
        Quaternion pose = Quaternion.Slerp(_poseFrom, _poseTo, t);

        _weaponTransform.position = PlayerPosition + root + _recoilOffset;
        _weaponTransform.rotation = pose;
    }

    /// <summary>
    /// 由招式资产推导出三组姿态。
    /// 数据里给的是**矛头在斜举位的落点**（画面位置），杆尾（模型根节点）由它反推：
    ///     根节点 = 矛头 − 矛头偏移·(斜举方向)
    /// 注意：prefab 的根节点**不在枪尾**，而在矛头下方约 2.18 个单位（实测）→ 偏移靠运行时测量。
    /// 三种姿态（起手 / 斜举 / 防御保持）共用同一个根节点，只有指向不同 → 举枪 = 绕“手”把矛头扇形扫上去。
    /// </summary>
    private void BuildPoses(AttackSkillConfig cfg)
    {
        _headAnchor = cfg.parryStanceHeadAnchor;
        _poseIdle = PoseForAngle(cfg.parryStanceTipDegrees);
        _poseEntry = PoseForAngle(cfg.parryStanceTipDegrees - cfg.parryStanceEntryDropDegrees);
        _poseDefend = PoseForAngle(cfg.parryStanceTipDegrees - cfg.parryStanceDefendDropDegrees);

        Vector3 dir = _poseIdle * Vector3.up;
        _rootAnchor = _headAnchor - dir * (_tipOffset * _stanceScale);
    }



    /// <summary>
    /// 由「枪尖指向的角度」反解世界旋转：角度 0 = 指向屏幕右(+X)，90 = 竖直向上。
    /// 枪身长轴 = prefab 本地 +Y（用 localRotation=identity，让模型本地 +Y 就是枪身长轴）。
    /// </summary>
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

            // 枪身长度：与 Stab 判定用的是同一份（10.8，scale=1 时的世界长度）
            var dynamics = _weapon.GetComponentInChildren<WeaponTasselDynamics>();
            if (dynamics != null && dynamics.WeaponLength > 0f) _weaponLength = dynamics.WeaponLength;
            else if (meshVisual != null && meshVisual.Length > 0f) _weaponLength = meshVisual.Length;

            // 根节点在枪身中段 → 矛头距根 = 长度的一半
            _tipOffset = _weaponLength * 0.5f;
        }

        _weaponTransform = _weapon.transform;
        // 姿态由 FromToRotation 直接控制 → 用 identity 让模型本地 +Y 就是枪身长轴
        _weaponTransform.localRotation = Quaternion.identity;
        _stanceScale = Mathf.Max(0.05f, cfg.parryStanceScale);
        float s = _weaponBaseScale * _stanceScale;
        _weaponTransform.localScale = new Vector3(s, s, s);
    }
}
