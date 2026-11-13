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
        /// <summary>破防中：枪被打脱手（绕世界 Z 逆时针旋转 + 向左漂移离开画面）。</summary>
        Broken = 4,
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
    private float _weaponLength = 10.8f;   // 整杆枪的世界长度（prefab 原尺寸）
    private float _tipOffset = 5.4f;      // 根节点 → 矛头（= Length/2，根在枪身中段）

    // 当前姿态与目标姿态（位置 = 根节点相对玩家的偏移；旋转 = 世界旋转）
    private Vector3 _rootFrom, _rootTo;
    private Quaternion _poseFrom, _poseTo;
    private float _moveT;
    private float _moveDuration = 0.2f;
    private bool _moving;
    private EaseMode _ease = EaseMode.Smooth;
    private Action _onArrive;

    // 姿态集合
    private Vector3 _rootAnchor;        // 杆尾（手的位置）——斜举位
    private Vector3 _entryRoot;         // 起手位的枪尾（默认与斜举位相同 = 枪尾不动，只挑枪尖）
    private Vector3 _exitRoot;          // 收枪位的枪尾（要把整杆推出画面）
    private Vector3 _defendRootOffset;  // 防御位相对杆尾的下沉量（**只用位置差，不用角度差**）
    private Vector3 _pivotLocalOffset;  // 旋转支点在枪尾时，根节点相对支点的本地偏移（枪尾→根 = +Y 半个身长）
    private Quaternion _poseIdle, _poseEntry, _poseOvershoot, _poseExit;
    private AttackSkillConfig _cfg;

    private float _recoilAmount;
    private Vector3 _recoilOffset;
    private float _recoilTimer;
    private float _recoilTotal;

    private bool _holding;
    private bool _holdRequested;     // 预输入：挥入期间记住"玩家按着"
    private bool _parrySucceeded;     // 本次招架是否真的格开了什么 → 决定收尾速度

    // ── 破防（枪被打脱手）───────────────────────────────────────────
    //   破防不再走「收枪出画」那条路（沿原路挥出 ⇒ 读起来像玩家自己把枪收走），
    //   而是专属动作：**以破防那一刻的姿态为起点**，绕**世界 Z 轴逆时针旋转**整杆枪
    //   （枪尾为支点）+ **轻微向左漂移**，直到离开画面后销毁。
    private float _brokenT;
    private float _brokenDuration = 0.55f;
    private bool _brokenWaitForRecoil;   // ★ 等后坐播完再起旋转（问题 2）
    private Vector3 _brokenBaseRoot;      // 破防起点的**视觉**根节点（含应变/后坐偏移）
    private Quaternion _brokenBasePose;   // 破防起点的**视觉**姿态

    // ── 命中定格 / 受击慢放（2026-11-12，局部打击感）──────────────────────
    //   ★ **局部优先：不动全局 Time.timeScale**，只作用在**持枪姿态**这一侧。
    //   ① 命中定格（Layer A）：命中帧把姿态定住 N 秒（**真实时间**）
    //      ⇒ 画面里摆出「被击中」的那一帧；**世界照常跑** ⇒ 靠对比读出顿感。
    //      （全局慢动作没有对比 ⇒ 只像掉帧；这是传统动作游戏的通行做法，见
    //        design/combat-hit-feedback-direction.md 与 memory/hit-feedback-system.md）
    //   ② 受击慢放（Layer B）：破防那一刻起，姿态以 slowScale 速度播 slowSeconds（真实秒）
    //      ⇒ 读成「被冲击」而不是「被定住」（魂系受击慢放的做法）。
    private float _hitStopRemaining;     // 命中定格剩余（真实秒）
    private float _breakSlowRemaining;   // 受击慢放剩余（真实秒）
    private float _breakSlowScale = 0.35f;

    // ── 应变表现的连续性（2026-11-12）─────────────────────────────────
    //   _strainActive：是否还在表现"被压"（防御中）
    //   ⚠ 2026-11-12 改版：原先用 "_strainFade 在收枪时淡出" 来消除「松手瞬间跳回原位」，
    //      现在改成**吸收**（见 MoveTo 的 absorbOffsets）：收枪/破防的**起点直接取视觉位置**、
    //      并把后坐/应变**归零** ⇒ ① 从被命中后的位置无缝继续 ② 轨迹不再被偏移污染。
    //   _shakeBlend：临界抖动的淡入淡出包络 ⇒ 进入/退出临界不「啪」地一下出现/消失。
    private bool _strainActive;
    private float _shakeBlend;
    private float _exitDuration = 0.3f;

    // ── 破防压力的表现（2026-11-09）────────────────────────────────────
    //   口径：**不显示任何数值**，玩家只从枪的样子读状态（被压低 / 枪尖下垂 / 抖 / 红缨甩开）。
    private WeaponTasselDynamics _tassel;
    private PlayerGuard.StrainLevel _lastStrain = PlayerGuard.StrainLevel.Steady;
    private MeshWeaponVisual _meshVisual;   // 闪白（命中确认）用

    /// <summary>防御位额外下沉（满档，世界单位）。</summary>
    private const float StrainExtraDefendDrop = 0.45f;
    /// <summary>枪尖额外下垂（满档，度）。</summary>
    private const float StrainTipDropDegrees = 4f;
    /// <summary>持续微抖的**角度**幅度（满档，度；仅临界档）。</summary>
    private const float StrainShakeDegrees = 0.8f;
    /// <summary>持续微抖的**位移**幅度（世界单位；仅临界档）。</summary>
    private const float StrainShakePosition = 0.05f;
    /// <summary>微抖频率（Hz）。</summary>
    private const float StrainShakeHz = 14f;
    /// <summary>红缨摆幅上限倍率（稳 / 吃力 / 临界）。</summary>
    private static readonly float[] TasselMaxBendScale = { 1f, 1.35f, 1.6f };
    /// <summary>格挡后退倍率（稳 / 吃力 / 临界）。</summary>
    private static readonly float[] RecoilScale = { 1f, 1.3f, 1.6f };

    public StancePhase Phase { get; private set; } = StancePhase.Idle;

    /// <summary>当前是否处于防御姿态（挥入播完 + 按住维持）。</summary>
    public bool IsDefending => Phase == StancePhase.Defending;

    /// <summary>当前是否在**挥入中**（防御按键按下后的举枪过程）。</summary>
    public bool IsSwingingIn => Phase == StancePhase.SwingIn;

    public PlayerWeaponStance(PlayerState owner)
    {
        _owner = owner;
    }

    // ── 对外接口 ───────────────────────────────────────────────

    /// <summary>
    /// 招架：挥入（从屏外滑入 → 斜举位）。
    /// <param name="succeeded">已知结果时直接给（弹飞物格挡那种同帧成立的）；
    /// 未知时留 false，等**接触帧**判定完用 <see cref="SetParryResult"/> 回填。</param>
    /// </summary>
    public void PlayParry(AttackSkillConfig cfg, bool succeeded = false)
    {
        if (cfg == null) return;
        _cfg = cfg;
        _parrySucceeded = succeeded;
        EnsureWeapon(cfg);
        BuildPoses(cfg);

        _holdRequested = _holding;   // 招架前就按着 = 预输入
        Phase = StancePhase.SwingIn;

        // 挥入阶段不表现"被压"（此时还没有破防值）⇒ 防御位才开
        _strainActive = false;
        _shakeBlend = 0f;

        // 起点在屏外（矛头出画），动作从这里挥入
        _rootFrom = _entryRoot;
        _rootTo = _entryRoot;
        _poseFrom = _poseEntry;
        _poseTo = _poseEntry;
        ApplyPose(1f);

        StartSwingIn();
    }

    /// <summary>
    /// **防御按键按下**：举起长枪（复用招架的挥入动作）→ 随后落防御位。
    ///
    /// 与手势招架的区别（2026-10-29 用户定：取消精防）：
    ///   • **不做任何判定** —— 没有接触帧、不反弹飞行物、敌人不会被打断；
    ///   • 只负责“把枪举起来”这条表现链（挥入 → 防御位 → 收枪出画）。
    /// 防御期间的免伤/破防值走 <see cref="PlayerGuard"/>，与招架无关。
    /// </summary>
    public void PlayDefend(AttackSkillConfig cfg)
    {
        // 复用同一条挥入链；_parrySucceeded 保持 false → 收枪走正常速度。
        PlayParry(cfg);
    }

    /// <summary>
    /// 挥入期间受击：**跳位**进防御位（取消挥入、立即落防御姿态，并起后退反馈）。
    ///
    /// 需求（2026-10-29）：「若在 parry 的表现时受击就直接进 defend 的受击表示就好（允许武器跳位）」。
    /// 受击伤害由 <see cref="PlayerGuard"/> 吸收（免伤、只涨破防值）。
    /// </summary>
    public void InterruptToDefend(AttackSkillConfig cfg)
    {
        if (Phase != StancePhase.SwingIn) return;

        _moving = false;          // 取消挥入的后续运动
        _holdRequested = true;    // 挥入被打断 → 直接进防御位，不再走收枪
        EnterDefend(0.06f);       // 短促过渡 = “跳位”
        Recoil(cfg);              // 受击后退再回位
    }

    /// <summary>回填招架结果（格开=true）→ 决定收枪是加速（格挡成功）还是正常速度。</summary>
    public void SetParryResult(bool succeeded) { _parrySucceeded = succeeded; }

    /// <summary>受击：武器先后退再回位（后退量/时长随破防压力变大变慢）。</summary>
    public void Recoil(AttackSkillConfig cfg)
    {
        if (_weaponTransform == null || cfg == null) return;
        var guard = _owner != null ? _owner.Guard : null;
        var level = guard != null ? guard.Strain : PlayerGuard.StrainLevel.Steady;
        float scale = RecoilScale[(int)level];
        _recoilTotal = Mathf.Max(0.05f, cfg.parryGuardRecoilSeconds * scale);
        _recoilTimer = _recoilTotal;
        _recoilAmount = cfg.parryGuardRecoilDistance * scale;
        // ★ 立刻摆出「被顶开」的姿态（2026-11-12）：
        //   后坐位移原本是在衰减段里按计时算出来的 ⇒ 命中帧上还是旧值，
        //   会变成「定格结束后才突然跳出去」—— 定格帧显示的就不是被击中的那一帧了。
        //   这里命中即写入 ⇒ 命中定格（ApplyHitStop）期间画面摆的正是「刚被顶开」的姿态。
        _recoilOffset = new Vector3(0f, 0f, -_recoilAmount);
    }

    /// <summary>
    /// **命中定格**（局部顿）：把持枪姿态定住 <paramref name="seconds"/>（**真实秒**）。
    ///
    /// 为什么是「局部」而不是改全局 <c>Time.timeScale</c>：
    ///   顿感来自**对比** —— 被击中的东西定住了、其它东西还在动。
    ///   全局慢动作/时停会让**所有东西一起停** ⇒ 没有对比 ⇒ 读成掉帧/卡了一下，
    ///   而且会把「节奏卡顿」的错觉误传给战斗系统。
    ///   项目既有口径见 memory/hit-feedback-system.md：「局部表现暂停，不修改全局 Time.timeScale」。
    /// </summary>
    public void ApplyHitStop(float seconds)
    {
        if (seconds <= 0f) return;
        if (seconds > _hitStopRemaining) _hitStopRemaining = seconds;
    }

    /// <summary>收枪（立即结束）：破防 / 场景重置用。</summary>
    public void Reset()
    {
        Phase = StancePhase.Idle;
        _moving = false;
        _holding = false;
        _holdRequested = false;
        _recoilTimer = 0f;
        _strainActive = false;
        _shakeBlend = 0f;
        _brokenT = 0f;
        _brokenWaitForRecoil = false;
        _hitStopRemaining = 0f;
        _breakSlowRemaining = 0f;
        if (_weapon != null) UnityEngine.Object.Destroy(_weapon);
        _weapon = null;
        _weaponTransform = null;
        _meshVisual = null;
    }

    /// <summary>
    /// 枪闪白（破防那一拍的「命中确认」层，REQ-006）。
    /// 只闪材质不动姿态 ⇒ 与「被顶开」的位移不冲突。
    /// </summary>
    public void FlashGuardBreak(float seconds)
    {
        if (_meshVisual == null)
        {
            if (_weapon == null) return;
            _meshVisual = _weapon.GetComponentInChildren<MeshWeaponVisual>();
        }
        if (_meshVisual == null) return;
        _meshVisual.PlayFlash(seconds);
    }

    /// <summary>每帧推进。</summary>
    public void Tick(float deltaTime, bool pointerHeld)
    {
        if (_weaponTransform == null) return;
        _holding = pointerHeld;

        // ★ 破防的慢放**完全由动画时长本身决定**（2026-11-13 定稿）：
        //   不再靠全局 timeScale（会被教学/对话/三选一/暂停菜单的 `Time.timeScale = 1` 顶掉），
        //   也不再叠本地慢放（否则会被减速两次）。
        //   `parryStanceBreakSeconds` = 0.85 游戏秒 ⇒ 150° 匀速转完 ≈ 3°/帧（读成“被缓缓打飞”）。

        // ★ 命中定格（局部顿）：姿态停住、**世界照常跑**。
        //   定格期间后坐计时也不走 ⇒ 画面里摆的是「刚被顶开」的那一帧（最有信息量的一帧）。
        if (_hitStopRemaining > 0f)
        {
            _hitStopRemaining -= Time.unscaledDeltaTime;
            ApplyPose(1f);
            return;
        }

        // ★ 受击慢放（局部慢动作）：**只有持枪姿态**慢放，世界照常。
        if (_breakSlowRemaining > 0f)
        {
            _breakSlowRemaining -= Time.unscaledDeltaTime;
            deltaTime *= _breakSlowScale;
        }

        // 受击后退的衰减
        if (_recoilTimer > 0f)
        {
            _recoilTimer -= deltaTime;
            float k = _recoilTimer > 0f ? Mathf.Clamp01(_recoilTimer / _recoilTotal) : 0f;
            k = k * k * (3f - 2f * k);      // ★ 平滑收尾：最后一帧不再「啪」地归零
            _recoilOffset = new Vector3(0f, 0f, -_recoilAmount * k);
            if (_recoilTimer <= 0f) _recoilOffset = Vector3.zero;
        }

        // 破防：枪被打飞（专属动作）⇒ 这一段不再走其它分支
        if (Phase == StancePhase.Broken)
        {
            // ★★ 2026-11-12（问题 2）：**先把「被打退」那一下播完，再起破防的旋转**
            //   ⇒ 破防不再与受击后坐并行（并行时 0.18s 的后退会被 0.55s 的旋转盖住）。
            //   等后坐期间：姿态保持破防起点 + 正在衰减的后坐（能看见"被顶开"），
            //   而 _brokenT 仍是 0 ⇒ 旋转/漂移都还没开始。
            if (_brokenWaitForRecoil)
            {
                if (_recoilTimer > 0f)
                {
                    ApplyPose(1f);
                    return;
                }
                // 后坐播完 ⇒ 把它归零（此时位置已回到破防起点），然后起旋转
                _brokenWaitForRecoil = false;
                _recoilOffset = Vector3.zero;
                _recoilTimer = 0f;
            }
            // ★ 破防的慢放**完全由动画时长本身决定**（不再靠全局 timeScale）。
            //   原因（2026-11-13 实测）：全局慢放会被教学/对话/三选一/暂停菜单的
            //   `Time.timeScale = 1` 直接顶掉（DialogueManager.cs:95、TutorialStageDirector.cs:250 …）
            //   ⇒ 真实游戏里等于没慢。
            //   现状：`parryStanceBreakSeconds` = 0.85 游戏秒 ⇒ 150° 匀速转完 ≈ 3°/帧（读成“被缓缓打飞”）。
            _brokenT += deltaTime / Mathf.Max(0.05f, _brokenDuration);
            ApplyPose(1f);
            if (_brokenT >= 1f) Reset();
            return;
        }

        // 预输入：挥入期间只**记住**按住，不切断动作（规则 1）
        if (Phase == StancePhase.SwingIn && _holding) _holdRequested = true;

        // 破防压力 → 红缨摆幅（压力越大，枪尾红缨甩得越开 = "手要滑了"）
        UpdateTasselSettle();

        // 临界抖动的淡入淡出包络（进入/退出临界不「啪」地一下出现/消失）
        UpdateShakeBlend(deltaTime);

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
    private void EnterDefend(float durationOverride = -1f)
    {
        Phase = StancePhase.Defending;
        // ★ 防御位开始表现"被压"（线性跟随破防值）
        _strainActive = true;
        float dur = durationOverride > 0f
            ? durationOverride
            : (_cfg != null ? Mathf.Max(0.05f, _cfg.parryStanceDefendEnterSeconds) : 0.12f);
        MoveTo(_rootAnchor + _defendRootOffset, _poseIdle, dur, EaseMode.Smooth);
    }

    /// <summary>收枪：沿原路挥出画面 → 出画后才销毁。</summary>
    /// <param name="durationOverride">时长覆盖（秒）；&lt;=0 时用配置值。</param>
    public void BeginExit(float durationOverride = -1f)
    {
        Phase = StancePhase.Exiting;
        float dur = durationOverride > 0f
            ? durationOverride
            : (_cfg != null
                ? (_parrySucceeded ? Mathf.Max(0.05f, _cfg.parryStanceExitFastSeconds) : Mathf.Max(0.05f, _cfg.parryStanceExitNormalSeconds))
                : 0.3f);
        // ★ 应变表现在这段时间里**连续淡出**（而不是松手瞬间就没了）
        _exitDuration = dur;
        // ★ 起点 = 枪**现在画面上**的位置（含后坐/应变），并把叠加偏移吸收掉
        //   ⇒ 被命中后松手，收枪从「被顶开后的那个位置」无缝继续，而不是回到标准起点。
        MoveTo(_exitRoot, _poseExit, dur, EaseMode.Out, onArrived: () => Reset(), absorbOffsets: true);
    }

    // ── 内部实现 ───────────────────────────────────────────────

    private void MoveTo(Vector3 root, Quaternion pose, float duration, EaseMode ease = EaseMode.Smooth, Action onArrived = null, bool absorbOffsets = false)
    {
        if (absorbOffsets)
        {
            // ★★ 2026-11-12（问题 1）：起点取**视觉位置**（含后坐/应变/抖动），
            //   并把这些**叠加偏移吸收**进起点（归零）。
            //   ⇒ ① 收枪/破防从枪「现在在的地方」无缝继续（被命中后松手不再丢表现）；
            //   ② 动作轨迹不再被正在衰减的后坐 / 正在变化的应变污染。
            ComputeVisual(Mathf.Clamp01(_moveT), out _rootFrom, out _poseFrom);
            _recoilOffset = Vector3.zero;
            _recoilTimer = 0f;
            _strainActive = false;
            _shakeBlend = 0f;
        }
        else
        {
            _rootFrom = CurrentRoot();
            _poseFrom = CurrentPose();
        }
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

    /// <summary>
    /// 破防压力 → 枪的表现（2026-11-09）。
    ///
    /// 设计口径：**不显示数值**，玩家只从枪的样子读状态：
    ///   • 被压低（防御位下沉）→ 吃力的量（连续）
    ///   • 枪尖下垂 → 吃力的量（连续）
    ///   • 持续微抖 → **临界的标志**（稳档/吃力档完全不抖 → 玩家能"突然开始抖"= 知道进临界了）
    ///   • 红缨甩得更开 → 吃力的标志（见 <see cref="UpdateTasselSettle"/>）
    ///
    /// ★ 2026-11-12（问题 1：格挡恢复不线性、复位两段瞬移）：
    ///   下沉/下垂改成**线性跟随破防值**（不再按档位闸门硬切），
    ///   且收枪/破防时叠加偏移被**吸收**进起点（见 <see cref="MoveTo"/>）
    ///   ⇒ 破防值回落与松手复位全程平滑，不再出现两段跳变。
    /// </summary>
    private void ApplyStrain(ref Vector3 root, ref Quaternion pose)
    {
        var guard = _owner != null ? _owner.Guard : null;
        if (guard == null) return;
        if (!_strainActive) return;

        float ratio = guard.MeterRatio;   // 连续量 0~1

        // ① 被压低：**线性跟随破防值**（旧版按档位硬切 ⇒ 回落跨阈值时“啪”地跳一下）
        root += new Vector3(0f, -StrainExtraDefendDrop * ratio, 0f);

        // ② 枪尖下垂：同样连续
        //    ⚠️ 不影响读招：招架的"角度一致"设计只针对**挥入那一瞬**；
        //    防御位是"按住"的持续状态，压角度不会干扰玩家读**敌人**的招式。
        if (_cfg != null)
        {
            float dropDeg = StrainTipDropDegrees * ratio;
            if (dropDeg > 0.01f)
                pose = Quaternion.Slerp(pose, PoseForAngle(_cfg.parryStanceTipDegrees - dropDeg), 1f);
        }

        // ③ 持续微抖：**只有关键档才抖**（"突然开始抖"= 明确的信号），
        //    但起/止走淡入淡出包络 ⇒ 不会“啪”地一下出现/消失。
        if (_shakeBlend > 0.001f)
        {
            float ph = Time.unscaledTime * StrainShakeHz;
            float amp = StrainShakeDegrees * ratio * _shakeBlend;
            pose = pose * Quaternion.AngleAxis(Mathf.Sin(ph * 6.283f) * amp, Vector3.right);
            root += new Vector3(Mathf.Sin(ph * 3.1f) * StrainShakePosition * _shakeBlend,
                                Mathf.Cos(ph * 4.7f) * StrainShakePosition * _shakeBlend, 0f);
        }
    }

    /// <summary>破防压力 → 红缨摆幅上限（压力越大甩得越开）。</summary>
    private void UpdateTasselSettle()
    {
        var guard = _owner != null ? _owner.Guard : null;
        if (guard == null || _tassel == null) return;
        var level = guard.Strain;
        if (level == _lastStrain) return;
        _lastStrain = level;
        float maxBend = _cfg != null ? _cfg.parryStanceTasselMaxBend : 14f;
        float damping = _cfg != null ? _cfg.parryStanceTasselDamping : 26f;
        _tassel.SetSettle(damping, maxBend * TasselMaxBendScale[(int)level]);
    }

    /// <summary>
    /// 临界抖动的**淡入淡出包络**（2026-11-12）。
    /// 「突然开始抖」是设计想要的信号，但硬切会变成一次位置/角度的跳变 ⇒ 用短包络淡入淡出。
    /// </summary>
    private void UpdateShakeBlend(float deltaTime)
    {
        var guard = _owner != null ? _owner.Guard : null;
        float target = guard != null && guard.Strain == PlayerGuard.StrainLevel.Critical ? 1f : 0f;
        _shakeBlend = Mathf.MoveTowards(_shakeBlend, target, deltaTime * 8f);
    }

    /// <summary>当前**基准**根节点/姿态（原始插值 + 应变，**不含后坐**）。</summary>
    private void ComputeBase(float t, out Vector3 root, out Quaternion pose)
    {
        float e = _ease == EaseMode.Out ? 1f - Mathf.Pow(1f - t, 3f) : t * t * (3f - 2f * t);
        root = Vector3.Lerp(_rootFrom, _rootTo, e);
        pose = Quaternion.Slerp(_poseFrom, _poseTo, e);
        // 破防压力的表现：被压低 / 枪尖下垂 / 持续微抖（不显示数值）
        ApplyStrain(ref root, ref pose);
    }

    /// <summary>当前**视觉**根节点/姿态（含应变与后坐偏移，不含支点偏移）。</summary>
    private void ComputeVisual(float t, out Vector3 root, out Quaternion pose)
    {
        ComputeBase(t, out root, out pose);
        root += _recoilOffset;
    }

    /// <summary>应用当前姿态：根节点位置 + 枪身指向。</summary>
    private void ApplyPose(float t)
    {
        if (_weaponTransform == null) return;
        Vector3 root;
        Quaternion pose;
        if (Phase == StancePhase.Broken)
        {
            // ★ 破防：以破防那一刻的**基准**姿态为起点（"以破防为止"）
            root = _brokenBaseRoot;
            pose = _brokenBasePose;
            // ★ 等后坐播完的那段时间里，保留「被顶开」的位移（后坐本身在衰减）
            if (_brokenWaitForRecoil) root += _recoilOffset;
            float k = Mathf.Clamp01(_brokenT);      // 等后坐期间 k=0 ⇒ 旋转尚未开始
            // 绕**世界 Z 轴**逆时针旋转（屏幕 +X 右 / +Y 上 ⇒ 正向 Z 旋转 = 屏幕逆时针）。
            //   前乘 ⇒ 旋转发生在世界空间；因为支点偏移用 pose 算，枪尾会**留在原地**、整杆翻转。
            float eased = BreakSpinCurve(k);
            float spin = BreakSpinDegrees * eased;
            // ★ 被撞的余震（2026-11-13）：旋转刚起的 ~1/3 段叠一个衰减抖动
            //   ⇒ 读成「被顶了一下」而不是机械匀速旋转（原来只有匀速 ⇒ 生硬）。
            spin += BreakWobbleDegrees * Mathf.Sin(k * BreakWobbleCycles * Mathf.PI * 2f) * Mathf.Max(0f, 1f - k * 3f);
            pose = Quaternion.AngleAxis(spin, Vector3.forward) * pose;
            // 轻微向左漂移（真正离开画面主要靠旋转）—— 跟着同一条缓动曲线 ⇒ 整体一起失速
            root += Vector3.left * (BreakDriftDistance * eased);
        }
        else
        {
            ComputeVisual(t, out root, out pose);
        }
        // **旋转支点在枪尾**：根节点相对支点的偏移随姿态一起转。
        // 落定姿态下这一项 = 0 → 落定画面与之前完全一致；
        // 起手/收枪时枪头绕枪尾甩出一道弧（枪头位移 ≈ 身长·sin拧角）。
        Vector3 pivotOffset = pose * _pivotLocalOffset - _poseIdle * _pivotLocalOffset;
        _weaponTransform.position = PlayerPosition + root + pivotOffset;
        _weaponTransform.rotation = pose;
    }

    /// <summary>破防旋转角度（度）。</summary>
    private float BreakSpinDegrees => _cfg != null ? _cfg.parryStanceBreakSpinDegrees : 230f;

    /// <summary>破防向左漂移（世界单位）。</summary>
    private float BreakDriftDistance => _cfg != null ? _cfg.parryStanceBreakDrift : 0.8f;

    /// <summary>破防旋转曲线：**匀速**（2026-11-13 定稿）。
    ///
    /// 为什么不能用“失速”曲线：数学上，**角速度单调递减且末速为 0 ⇒ 起步必然 ≥ 2 倍均值**
    ///   ⇒ 用户读到的就是「起步最快、然后加速离开」（实测反馈）。
    /// ⇒ 现在的口径：**角速度恒定**（没有任何加速），「慢」完全由 **动画时长** 决定
    ///   （`parryStanceBreakSeconds` = 0.85 游戏秒 ⇒ 150° 匀速 ≈ 3°/帧）。
    /// </summary>
    private static float BreakSpinCurve(float k) => k;

    /// <summary>破防旋转起手的余震幅度（度）：读成「被顶了一下」。</summary>
    private float BreakWobbleDegrees => _cfg != null ? _cfg.parryStanceBreakWobbleDegrees : 3f;

    /// <summary>余震抖动次数（整个旋转过程内）。</summary>
    private const float BreakWobbleCycles = 2f;

    /// <summary>
    /// **破防**：枪被打脱手 —— 以破防这一刻的姿态为起点，沿**世界 Z 轴逆时针旋转**整杆枪
    /// （枪尾为支点）+ **轻微向左漂移**，直到离开画面后销毁。
    ///
    /// 为什么不走 BeginExit（2026-11-12 用户定）：
    ///   原来的破防走的是「收枪出画」那条路（沿原路挥出）⇒ 读起来像玩家自己把枪收走，
    ///   不像被打爆 ⇒ 破防要有一条**专属动作**。
    /// </summary>
    public void BeginBreak(float durationOverride = -1f)
    {
        if (_weaponTransform == null) return;

        // 冻结当前**基准**姿态（含应变、不含后坐）作为破防起点 ⇒「以破防为止」
        //   ⇒ 后坐留给等待期自己播（问题 2：先后退、再破防）
        Vector3 root;
        Quaternion pose;
        ComputeBase(Mathf.Clamp01(_moveT), out root, out pose);
        _brokenBaseRoot = root;
        _brokenBasePose = pose;

        _moving = false;          // 取消收枪/回位等后续运动
        _onArrive = null;
        _holdRequested = false;
        _holding = false;
        _strainActive = false;    // 应变已被吸收进起点 ⇒ 之后不再叠加
        _shakeBlend = 0f;
        // ★ 后坐**不归零**：它要先播完（问题 2），破防的旋转在那之后才开始
        _brokenWaitForRecoil = _recoilTimer > 0f;
        // ★ 受击慢放（Layer B）：破防的慢放**已改由动画时长本身决定**（`parryStanceBreakSeconds`）
        //   ⇒ 本地慢放不再叠加，否则会被减速两次（0.35 × 0.85 ⇒ 枪几乎冻住）。
        _breakSlowScale = _cfg != null ? Mathf.Clamp(_cfg.parryGuardBreakSlowScale, 0.05f, 1f) : 0.35f;
        _breakSlowRemaining = 0f;

        Phase = StancePhase.Broken;
        _brokenDuration = durationOverride > 0f
            ? durationOverride
            : (_cfg != null ? Mathf.Max(0.1f, _cfg.parryStanceBreakSeconds) : 0.55f);
        _brokenT = 0f;
    }

    /// <summary>
    /// 由招式资产推导姿态：
    ///   · 斜举位：矛头落在 <c>parryStanceHeadAnchor</c>，枪尖指向 <c>parryStanceTipDegrees</c>
    ///   · 起手位：**枪尾基本不动**，只有枪尖指向压低（<c>parryStanceEntryAngleDegrees</c>）→ “枪尖从下往上挑”
    ///   · 收枪位：枪尖继续往下 + 枪尾推出画面 → **整杆出画后才销毁**
    ///   · 防御位：与斜举位**同角度**，只用位置下沉（角度一致 → 枪尖上扬角每次读得一样）
    /// </summary>
    private void BuildPoses(AttackSkillConfig cfg)
    {
        Vector3 headAnchor = cfg.parryStanceHeadAnchor;
        _poseIdle = PoseForAngle(cfg.parryStanceTipDegrees);

        // 起手/收枪的**拧转**：绕枪自身 local X 轴（枪身长轴是 local +Y）。
        // 只加在起手与收枪两个姿态上 → 防御位（_poseIdle）不受影响，终态与原来完全一致。
        float roll = cfg.parryStanceEntryRollDegrees;
        _poseEntry = PoseForAngle(cfg.parryStanceEntryAngleDegrees) * Quaternion.AngleAxis(roll, Vector3.right);
        _poseExit = PoseForAngle(cfg.parryStanceExitAngleDegrees) * Quaternion.AngleAxis(roll, Vector3.right);
        _poseOvershoot = PoseForAngle(cfg.parryStanceTipDegrees + cfg.parryStanceOvershootDegrees);

        // 根节点（手的位置）= 矛头 − (Length/2)·斜举方向
        Vector3 dir = _poseIdle * Vector3.up;
        _rootAnchor = headAnchor - dir * _tipOffset;

        // **旋转支点 = 枪尾端**：根节点相对枪尾的本地偏移（枪尾在根节点下方 半个身长）
        _pivotLocalOffset = new Vector3(0f, _tipOffset, 0f);

        // 起手：枪尾不动（默认），只有枪尖压到下方 → 挑起来
        _entryRoot = _rootAnchor + cfg.parryStanceEntryRootOffset;
        // 收枪：枪尾也推出画面 → 整杆出画
        _exitRoot = _rootAnchor + cfg.parryStanceExitRootOffset;

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
            _meshVisual = meshVisual;
            _weaponBaseScale = _weapon.transform.localScale.x;

            // 枪长：与 Stab 判定用的是同一份（10.8 = 整杆长度，scale=1）
            var dynamics = _weapon.GetComponentInChildren<WeaponTasselDynamics>();
            if (dynamics != null && dynamics.WeaponLength > 0f) _weaponLength = dynamics.WeaponLength;
            else if (meshVisual != null && meshVisual.Length > 0f) _weaponLength = meshVisual.Length;
            _tipOffset = _weaponLength * 0.5f;   // 根在枪身中段 → 矛头距根 = 长度的一半

            // 红缨物理：持枪时收敛摆幅（让枪头的视觉角度稳定）
            if (dynamics != null)
            {
                dynamics.SetSettle(cfg.parryStanceTasselDamping, cfg.parryStanceTasselMaxBend);
                _tassel = dynamics;
            }
        }

        _weaponTransform = _weapon.transform;
        // 姿态由 FromToRotation 直接控制 → 用 identity 让模型本地 +Y 就是枪身长轴
        _weaponTransform.localRotation = Quaternion.identity;
        // 尺寸不归动作管：持枪与其它招式用同一把枪、同一个大小（prefab 原尺寸）→ 不做缩放
    }
}
