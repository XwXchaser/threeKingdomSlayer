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
/// 姿态的表达（重要）：
///   **不用手调欧拉角**（枪 prefab 自带 (90,0,0)，不同路径下含义还不一样，极易搞错），
///   而是「枪尖指向 + 握持点」两个量：
///     · 枪尖方向 = 从 prefab 的本地 +Y（枪身长轴）指向目标世界方向 → <see cref="Quaternion.FromToRotation"/>
///     · 握持点 = 枪身根节点的世界偏移（相对玩家）
///   参考图（《起源》第三人称背面视角）实测：枪斜横过身前，枪尖在画面右上 ≈42°；
///   我们的相机屏幕右 = +X，玩家面朝 +Z（右手在 +X 侧）→ 与参考图同一侧，不需要镜像。
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

    // 当前姿态与目标姿态（位置 = 相对玩家的偏移；旋转 = 世界旋转）
    private Vector3 _anchorFrom, _anchorTo;
    private Quaternion _poseFrom, _poseTo;
    private float _moveT;
    private float _moveDuration = 0.2f;
    private bool _moving;

    // 姿态集合（由招式资产推导）
    private Vector3 _anchorIdle, _anchorEntry, _anchorDefend;
    private Quaternion _poseIdle, _poseEntry, _poseDefend;
    private AttackSkillConfig _cfg;

    private float _recoilAmount;
    private Vector3 _recoilOffset;
    private float _recoilTimer;
    private float _recoilTotal;

    public StancePhase Phase { get; private set; } = StancePhase.Idle;

    /// <summary>当前是否处于防御姿态（举枪完成后、按住维持）。</summary>
    public bool IsDefending => Phase == StancePhase.Settled && _holding;

    private bool _holding;

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

        // 收掉站桩蓄力的武器表现（否则场上两把枪：蓄力枪 + 持枪实体）
        var chargeVisual = UnityEngine.Object.FindObjectOfType<ChargeStabVisual>();
        if (chargeVisual != null) chargeVisual.SuppressFadeAndDestroy();

        Phase = StancePhase.Raising;
        _anchorFrom = _anchorEntry;
        _poseFrom = _poseEntry;
        _anchorTo = _anchorEntry;
        _poseTo = _poseEntry;
        ApplyPose(1f);

        MoveTo(_anchorIdle, _poseIdle, Mathf.Max(0.05f, cfg.parryStanceRaiseSeconds), onArrived: () =>
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
                if (Phase != StancePhase.Settled || !_atDefend)
                {
                    _atDefend = true;
                    Phase = StancePhase.Settled;
                    MoveTo(_anchorDefend, _poseDefend, _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceDefendEnterSeconds) : 0.15f);
                }
            }
            else
            {
                if (_atDefend || Phase == StancePhase.Settled)
                {
                    _atDefend = false;
                    Phase = StancePhase.Returning;
                    MoveTo(_anchorEntry, _poseEntry, _cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceReturnSeconds) : 0.8f, onArrived: () =>
                    {
                        // 回落走完仍未按住 → parry 结束，收枪
                        if (!_holding) Reset();
                    });
                }
            }
        }
    }

    // ── 内部实现 ───────────────────────────────────────────────

    private bool _atDefend;
    private Action _onArrive;

    private void MoveTo(Vector3 anchor, Quaternion pose, float duration, Action onArrived = null)
    {
        _anchorFrom = CurrentAnchor();
        _poseFrom = CurrentPose();
        _anchorTo = anchor;
        _poseTo = pose;
        _moveT = 0f;
        _moveDuration = Mathf.Max(0.01f, duration);
        _moving = true;
        _onArrive = onArrived;
    }

    private Vector3 CurrentAnchor() => Vector3.Lerp(_anchorFrom, _anchorTo, Mathf.Clamp01(_moveT));
    private Quaternion CurrentPose() => Quaternion.Slerp(_poseFrom, _poseTo, Mathf.Clamp01(_moveT));

    private Vector3 PlayerPosition => _owner != null ? _owner.transform.position : Vector3.zero;

    private void ApplyPose(float t)
    {
        if (_weaponTransform == null) return;
        Vector3 anchor = Vector3.Lerp(_anchorFrom, _anchorTo, t);
        Quaternion pose = Quaternion.Slerp(_poseFrom, _poseTo, t);
        _weaponTransform.position = PlayerPosition + anchor + _recoilOffset;
        _weaponTransform.rotation = pose;
    }

    /// <summary>由招式资产推导出三组姿态：起手位 / 斜举位 / 防御保持位。</summary>
    private void BuildPoses(AttackSkillConfig cfg)
    {
        _anchorIdle = cfg.parryStanceAnchor;
        _anchorEntry = cfg.parryStanceAnchor + new Vector3(cfg.parryStanceEntryAnchorOffset.x,
            cfg.parryStanceEntryAnchorOffset.y, cfg.parryStanceEntryAnchorOffset.z);
        _anchorDefend = cfg.parryStanceAnchor + new Vector3(0f, -cfg.parryStanceDefendAnchorDrop, 0f);

        _poseIdle = PoseForAngle(cfg.parryStanceTipDegrees);
        _poseEntry = PoseForAngle(cfg.parryStanceTipDegrees - cfg.parryStanceEntryDropDegrees);
        _poseDefend = PoseForAngle(cfg.parryStanceTipDegrees - cfg.parryStanceDefendDropDegrees);
    }

    /// <summary>
    /// 由「枪尖指向的角度」反解世界旋转：角度 0 = 指向屏幕右(+X)，90 = 竖直向上。
    /// 枪身长轴 = prefab 本地 +Y（用 localRotation=identity，让模型长轴直接对齐）。
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
        }

        _weaponTransform = _weapon.transform;
        // 姿态由 FromToRotation 直接控制 → 用 identity 让模型本地 +Y 就是枪身长轴
        _weaponTransform.localRotation = Quaternion.identity;
        float s = Mathf.Max(0.01f, cfg.parryStanceScale);
        _weaponTransform.localScale = new Vector3(_weaponBaseScale * s, _weaponBaseScale * s, _weaponBaseScale * s);
    }
}
