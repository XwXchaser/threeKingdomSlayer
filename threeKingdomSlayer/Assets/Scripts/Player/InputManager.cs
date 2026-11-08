using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 输入管理器
/// 使用Unity Input System检测点击、长按、滑动等手势
/// 通过划动角度判定攻击类型（纯方向判定，不依赖屏幕区域）
/// </summary>
public partial class InputManager : MonoBehaviour, IPlayerInput
{
    public static InputManager Instance { get; private set; }

    private const float SlashSlopeDeadZone = 0.08f;
    private const float SlashMaxVisualTilt = 15f;

    [Header("手势参数")]
    public float longPressDuration = 0.3f;   // 长按判定时间（秒）
    [Tooltip("最小蓄力时间（秒）：所有蓄力攻击（横扫、斩击、穿刺、挑飞）必须按住鼠标达到此时长后，手势判定才生效。\n" +
             "未达到前，任何划动/点击仅触发普通攻击（戳击/斩击）或无操作。")]
    public float minChargeTime = 0.5f;       // 最小蓄力时间（秒）

    [Tooltip("教学关用：置 true 时蓄力永远算 0 级，玩家无法打出蓄力/终结技")]
    public bool suppressCharging;
    public float swipeThreshold = 30f;       // 滑动判定最小距离（像素）
    [Tooltip("有效滑动必须在此时间内完成，超时的缓慢移动不会触发招式。")]
    public float maxSwipeDuration = 0.25f;
    [Tooltip("手指离开此死区后才开始统计滑动耗时。")]
    public float swipeStartDeadZone = 8f;
    [Tooltip("有效滑动的最低平均速度（像素/秒）。")]
    public float minSwipeSpeed = 180f;
    [Tooltip("一次招式触发后的最短重新识别间隔（秒）。")]
    public float swipeRearmDelay = 0.1f;
    [Tooltip("停留蓄力允许的轻微移动范围（像素）。")]
    public float chargeMovementTolerance = 20f;
    [Tooltip("垂直角度阈值（度）：划动方向与垂直轴夹角小于此值时判定为挑飞。范围0~90")]
    public float verticalSwipeThreshold = 30f;
    [Tooltip("水平角度阈值（度）：划动方向与水平轴夹角小于此值时判定为横扫。范围0~90")]
    public float horizontalSwipeThreshold = 30f;
    [Tooltip("长按下滑角度阈值（度）：长按后划动方向与下方向夹角小于此值时判定为落雷道具。")]
    public float swipeDownAngleThreshold = 30f;

    [Header("工具引用")]
    public AttackSystem attackSystem;
    public WhirlwindController whirlwindController;
    [Tooltip("招式状态机。留空时直接调用 AttackSystem（旧行为）")]
    public PlayerMoveStateMachine moveStateMachine;

    // 输入模型开关已撤回：蓄力与否是唯一分叉轴，方向不作为分叉依据（见设计文档 11.6）


    [Header("蓄力分级")]
    [Tooltip("各级蓄力所需的按住时长（秒，升序）。第一级应与蓄力视觉出现时刻一致（默认 0.3s，即 longPressDuration），继续按住提升等级")]
    public float[] chargeLevelTimes = { 0.3f, 0.6f, 1.0f };

    /// <summary>
    /// 当前蓄力等级：0 = 未达一级；>=1 表示蓄力招式已就绪。
    /// 以「按住时长」实时计算，不存状态，避免跨手势残留。
    /// </summary>
    public int CurrentChargeLevel
    {
        get
        {
            if (suppressCharging) return 0;                          // 教学关：不允许完成充能
            if (IsGuarding) return 0;                                // 防御姿态：不是蓄力
            if (!IsChargeAllowed()) return 0;                        // **没有前置招式 → 不存在蓄力**
            if (!isLongPress || hasTriggeredDuringHold) return 0;   // 本次按住已经出过招 → 必须先抬手重按
            if (chargeLevelTimes == null || chargeLevelTimes.Length == 0)
                return isCharged ? 1 : 0;
            // ⚠️ **必须和 SegmentDuration 用同一把时钟**（2026-11-08）：
            //   教学时停（timeScale=0）时玩家照样要能「按住蓄满」→ 派生招式才出得来。
            //   原来这里写死 `Time.time - segmentStartTime`（缩放时间）→ 冻结期间蓄力永远是 0
            //   → 松手只打得出 0 级蓄力 → 指向突刺出不来 → 这一拍死等。
            float held = SegmentDuration;
            int level = 0;
            for (int i = 0; i < chargeLevelTimes.Length; i++)
            {
                if (held >= chargeLevelTimes[i]) level = i + 1;
            }
            return level;
        }
    }

    /// <summary>
    /// **这次按住能不能算「蓄力」** —— 只有「有前置招式」（连段窗口内）才算。
    ///
    /// 2026-11-05 用户口径：「我要关掉当前无前置纯蓄力」→
    ///   **中立态不许蓄力**：没有蓄力等级、没有蓄力进度指示器、没有蓄力武器后拉表现，
    ///   也不能靠蓄力打出蓄力招（蓄力横扫 / 蓄力横斩 / 蓄力上挑 / 站桩蓄力穿刺）。
    ///
    /// 判据：<c>comboChargeActive</c> —— 招式状态机在「**招式进行中再次按下**」或
    /// 「按住期间招式节点切换」时置位，即**玩家自己刚出过招**（= 有前置招式）；
    /// 站桩蓄力（中立态长按）是「直通」，不会置位 → 蓄力不成立 ✓。
    /// 未按住时该标记每帧清零（<c>Update</c>）→ 不会残留成「假前置」。
    ///
    /// ⚠️ 不要再加「状态机此刻是否活跃」的条件：真实连招里节点可能先于蓄力完成而过期，
    ///    那会让连招的蓄力随机失效（指向突刺教学会变得不稳）。
    /// </summary>
    public bool IsChargeAllowed()
    {
        return comboChargeActive;
    }

    /// <summary>
    /// 持枪姿态是否在进行中（招架 / 举枪 / 防御 / 回落）。
    /// 期间：蓄力等级恒 0（不出现蓄力视觉）、蓄力指示器让位、蓄力武器让位（否则场上两杆长枪）。
    /// </summary>
    public bool IsGuarding
    {
        get
        {
            var ps = PlayerState.Instance;
            return ps != null && ps.WeaponStance != null
                && ps.WeaponStance.Phase != PlayerWeaponStance.StancePhase.Idle;
        }
    }

    /// <summary>
    /// 指针是否仍被按住。
    /// **注意**（2026-10-29）：防御姿态**不再**看它 —— 防御只认**防御键的 hold**
    /// （<c>PlayerState.DefendHeld</c>）。旧的“按住屏幕任意处也算按住、会把玩家拖回防御态”
    /// 的逻辑与教学用的 <c>forcePointerHeld</c> 一起移除。
    /// </summary>
    public bool PointerHeld => isTouching;

    /// <summary>
    /// 指针是否处于「已按下且未被输入门拦下」的状态。
    /// 供招式状态机判断「按住不放时本段先不结束」的宽限与连段蓄力保持。
    /// QTE 输入优先级高于连招，QTE 期间不视为连招输入。
    /// </summary>
    /// <summary>调试用：当前是否已进入长按态（教学探针用）。</summary>
    public bool IsLongPressForDebug => isLongPress;

    public bool IsPointerDown => isTouching && gameplayInputEnabled && skillInputEnabled
        // ⚠️ 教学时停（allowInputWhileTimeFrozen）时**也要算「按着」**（2026-11-08）：
        //   `PlayerMoveStateMachine.UpdatePointerHold` 靠 IsPointerDown 判断「玩家正按住」
        //   → 时停时它恒为 false → 连段蓄力标记（comboChargeActive）永远置不上
        //   → 蓄力等级恒为 0 → 松手出不来派生招式 → 这一拍死等。
        //   口径与下面 Update() 的早退分支一致（时停且放行输入时照常处理输入 ✓）。
        && blockInputFrames <= 0 && (Time.timeScale > 0f || allowInputWhileTimeFrozen) && !IsAnyQTEActive();

    /// <summary>按住时长（秒）：连段蓄力用它驱动「拉回蓄势位」的进度</summary>
    public float HoldDurationSeconds => isTouching ? Mathf.Max(0f, SegmentDuration) : 0f;

    /// <summary>当前按住段的时长（教学时停时走 unscaled 时钟 → 冻结中也能蓄满）。</summary>
    private float SegmentDuration => useUnscaledHoldTime
        ? Time.unscaledTime - segmentStartTimeUnscaled
        : Time.time - segmentStartTime;

    /// <summary>当前指针位置对应的戳击目标列（C2 的「指向」用）。未落在有效列上时返回 -1</summary>
    public int CurrentPointerStabColumn => GetStabColumnFromScreenPosition(currentPointerPos);

    // ── 「按下即出」实现 ─────────────────────────────────────────────────

    /// <summary>按下时备好「待兑现的轻攻击」（按下即出）。</summary>
    private void ArmPressTap(Vector2 pos)
    {
        _pressTapPending = false;
        _pressTapFired = false;
        if (pressTapDelay <= 0f) return;
        if (!CanCommitPressTap()) return;
        _pressTapPending = true;
        _pressTapStart = Time.time;
        _pressTapPos = pos;
    }

    /// <summary>取消待兑现的轻攻击（划动 / 手指漂移 / 开始蓄力）。</summary>
    private void CancelPressTap()
    {
        _pressTapPending = false;
    }

    /// <summary>现在能不能兑现一记轻攻击（输入门 / QTE / 防御姿态 / 教学闸门 / 是否在战斗）。</summary>
    private bool CanCommitPressTap()
    {
        if (!skillInputEnabled) return false;
        if (!gameplayInputEnabled) return false;
        if (blockInputFrames > 0) return false;
        if (IsAnyQTEActive()) return false;
        if (StanceActive) return false;                                  // 招架/防御姿态中
        if (!TutorialAllowsTapGesture(CurrentChargeLevel)) return false;   // 教学手势闸门
        if (!IsInBattleState()) return false;                             // 局外（落地页/主菜单）
        return true;
    }

    /// <summary>每帧检查：到点了就兑现（不等抬手）。</summary>
    private void UpdatePressTap()
    {
        if (!_pressTapPending) return;
        if (!CanCommitPressTap()) { CancelPressTap(); return; }
        // 玩家开始蓄力了（按住到一级）→ 不再自动出轻攻击
        // （保持 2026-11-05 的口径： neutral 状态不许蓄力，也不该凭空多打一记）
        if (CurrentChargeLevel >= 1) { CancelPressTap(); return; }
        if (Time.time - _pressTapStart >= pressTapDelay) CommitPressTap();
    }

    /// <summary>兑现待发的轻攻击。</summary>
    private void CommitPressTap()
    {
        if (!_pressTapPending) return;
        _pressTapPending = false;
        if (!CanCommitPressTap()) return;
        int column = GetStabColumnFromScreenPosition(_pressTapPos);
        if (column < 0) return;
        bool executed = SubmitGesture(MoveGesture.Tap, CurrentChargeLevel, column, screenPos: _pressTapPos);
        if (executed) _pressTapFired = true;
    }

    /// <summary>
    /// 当前这次按下是否被招式状态机判定为「连段蓄力」（按下时置位，抬手或复位时清空）。
    /// 连段蓄力不是穿刺蓄力：依附蓄力事件链的附带表现（穿刺瞄准指示器、蓄力视觉进出场）应当让位。
    /// </summary>
    [System.NonSerialized] public bool comboChargeActive;

    [Header("蓄力招式开关")]
    [Tooltip("打开后只允许竖滑蓄力（上挑）：站桩蓄力（穿刺）与横滑蓄力（横扫）都不成立，蓄力指示器与蓄力武器视觉也不出现。\n蓄力 stab / 蓄力 slash 还没做完 —— 2026-10-21 起默认关闭（用户要求恢复蓄力表现），打开前先确认蓄力出招与命中正常。")]
    public bool restrictChargedToVertical = false;

    // ── 「按下即出」（承诺点）────────────────────────────────────────────
    [Header("按下即出（承诺点）")]
    [Tooltip("按下后多久自动兑现一记轻攻击（秒）：抬手晚于这个时刻就先出招，不再等抬手。\n" +
             "0 = 关闭（默认）。\n" +
             "⚠️ **实测结论：默认关闭**。本项目的“按住”本身就是一个手势（按住到一级 = 蓄力，\n" +
             "   门槛 0.3s），而滑动要 50px/180px·s⁻¹ 才成立 —— 任何小于蓄力门槛的“按下即出”窗口，\n" +
             "   都会把「按住一下再划」误打成「先一记轻攻击 + 划动被丢弃」。\n" +
             "   真正的跟手收益已由**输入缓冲 + 命中取消**提供（招式状态机那两个开关）。\n" +
             "   想试按就把这个值调到 0.12 左右（两种自动取消：① 指针移动超过容差；② 玩家开始蓄力）。")]
    public float pressTapDelay = 0f;
    [Tooltip("按下后指针移动超过这个距离（像素）就取消待发的轻攻击")]
    public float pressTapSlop = 8f;

    private bool _pressTapPending;
    private float _pressTapStart;
    private Vector2 _pressTapPos;
    /// <summary>本次按住已经由「按下即出」打过一记轻攻击（抬手时不要再补一记点按）</summary>
    private bool _pressTapFired;

    // 技能输入开关（狂怒大招期间关闭）
    [System.NonSerialized] public bool skillInputEnabled = true;
    /// <summary>
    /// 玩家输入是否被放行（**只读**）。
    ///
    /// 2026-10-22 起这个开关不再允许直接写：真正的状态由 <see cref="GameplayInputGate"/> 持有
    /// （每个 owner 各自申请 / 释放一份拦截 + 原因枚举）。
    /// 旧实现是"多个模块轮流写同一个全局布尔，后写者覆盖前写者"，
    /// 结果取决于调用顺序，代码上看不出来（隐式全局状态）——现在谁在拦、因为什么拦，一次查询就有答案。
    /// </summary>
    public bool gameplayInputEnabled => !GameplayInputGate.IsBlocked;

    /// <summary>IPlayerInput 端口实现：内核用它判断「现在能不能兑现缓冲」。</summary>
    bool IPlayerInput.GameplayInputEnabled => gameplayInputEnabled;
    /// <summary>教学手势拍：非 None 时只接受这个目标手势，其它手势一律不成立（不出招、也不消耗蓄力段）。</summary>
    [System.NonSerialized] public TutorialGestureTarget tutorialGestureTarget = TutorialGestureTarget.None;
    /// <summary>
    /// 教学时停用：<c>timeScale = 0</c> 时仍然处理玩家输入。
    /// 教学招架拍要把游戏整个冻住（让新手从容出手），但玩家必须还能做手势 → 只能靠这条开关。
    /// 只有教学导演会开它，拍结束必须清零。
    /// </summary>
    [System.NonSerialized] public bool allowInputWhileTimeFrozen;
    /// <summary>
    /// **按住时长改用不受 timeScale 影响的时钟**（教学时停专用）。
    ///
    /// 教学把整个游戏冻住（timeScale=0）时，玩家的「按住蓄满」必须还能继续攒时间
    /// → 否则蓄力条永远涨不上去 → 派生招式出不来 → 这一拍死等（2026-11-08）。
    /// 正常战斗保持 false（按住时长仍是**游戏时间**，卡肉/慢动作的手感不变 ✓）。
    /// </summary>
    [System.NonSerialized] public bool useUnscaledHoldTime;
    /// <summary>输入屏蔽帧计数器（由 UpgradeChoiceManager 设置，防止选择选项的点击触发攻击）</summary>
    [System.NonSerialized] public int blockInputFrames = 0;

    // ── IPlayerInput 端口实现（内核只读这些，不碰识别逻辑）──

    /// <summary>连段蓄力标记（端口形态）。</summary>
    public bool ComboChargeActive
    {
        get => comboChargeActive;
        set => comboChargeActive = value;
    }

    /// <summary>教学期：只允许竖滑蓄力（上挑）。</summary>
    public bool RestrictChargedToVertical => restrictChargedToVertical;

    /// <summary>最小蓄力时间（秒）。</summary>
    public float MinChargeTime => minChargeTime;

    /// <summary>长按判定时间（秒）。</summary>
    public float LongPressDuration => longPressDuration;

    /// <summary>技能输入是否放行（狂化大招期间会关掉）。</summary>
    public bool SkillInputEnabled
    {
        get => skillInputEnabled;
        set => skillInputEnabled = value;
    }

    /// <summary>屏蔽若干帧输入（防止 UI 弹窗的点击误触发攻击）。</summary>
    public void BlockInputForFrames(int frames)
    {
        blockInputFrames = Mathf.Max(0, frames);
    }

    // ── IPlayerInput 的事件（显式实现：内核走接口，上层仍可用本类的公开委托字段）──
    // 公开字段不能直接实现接口的 event/property（C# 规则），所以这里显式转发到同一份委托。

    event System.Action<Vector2> IPlayerInput.OnChargeBegan
    {
        add { OnChargeBegan += value; }
        remove { OnChargeBegan -= value; }
    }

    event System.Action<Vector2, float> IPlayerInput.OnChargeUpdated
    {
        add { OnChargeUpdated += value; }
        remove { OnChargeUpdated -= value; }
    }

    event System.Action IPlayerInput.OnChargeEnded
    {
        add { OnChargeEnded += value; }
        remove { OnChargeEnded -= value; }
    }

    event System.Action<AttackType, int> IPlayerInput.OnAttackExecuted
    {
        add { OnAttackExecuted += value; }
        remove { OnAttackExecuted -= value; }
    }

    // 触摸状态
    private Vector2 touchStartPos;
    private float touchStartTime;
    private Vector2 segmentStartPos;
    private float segmentStartTime;
    /// <summary>按住段的 unscaled 起点（教学时停用：冻结中蓄力也要能继续攒时间）。</summary>
    private float segmentStartTimeUnscaled;
    private bool isTouching;
    private bool isLongPress;
    private bool isCharged;     // 当前分段蓄力是否已满
    private bool isSwiping;
    private bool hasTriggeredDuringHold;
    private int currentTouchId = -1;
    private Vector2 currentPointerPos; // 当前指针位置（鼠标/触摸），用于蓄力指示器

    // 防连触发
    private float lastGestureTime = float.MinValue;

    // 速度门控追踪：仅在瞬时速度达标后才开始计时和累积距离
    private Vector2 swipeTrackStartPos;
    private float swipeTrackStartTime;
    private bool isSwipeTracking;
    private Vector2 lastFramePos;
    private float lastFrameTime;

    // 事件
    public System.Action<AttackType, int> OnAttackExecuted; // attackType, targetColumn

    // 蓄力事件（用于动态蓄力瞄准指示器 ChargeIndicatorController）
    /// <summary>
    /// 蓄力开始，参数：屏幕坐标
    /// </summary>
    public System.Action<Vector2> OnChargeBegan;
    /// <summary>
    /// 蓄力进度更新，参数：屏幕坐标, progress (0→1)
    /// </summary>
    public System.Action<Vector2, float> OnChargeUpdated;
    /// <summary>
    /// 蓄力结束
    /// </summary>
    public System.Action OnChargeEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 登记到输入端口持有者：内核只通过 IPlayerInput 读输入状态 / 订阅输入事件
        //（2026-10-22 asmdef 分层第九批）。手势识别逻辑仍然只在本类里 ✅。
        PlayerInputHub.Current = this;

        // DPI 自适应：高 DPI 屏幕增加滑动阈值，避免误触发
        float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
        swipeThreshold = Mathf.Clamp(swipeThreshold * (dpi / 160f), 30f, 150f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (ReferenceEquals(PlayerInputHub.Current, this))
            PlayerInputHub.Current = null;
    }

    private void Start()
    {
        if (attackSystem == null)
            attackSystem = AttackSystem.Instance;
        if (moveStateMachine == null)
            moveStateMachine = FindObjectOfType<PlayerMoveStateMachine>();
    }

    public void CancelCurrentGesture()
    {
        if (isTouching)
            OnChargeEnded?.Invoke();
        isTouching = false;
        isLongPress = false;
        isCharged = false;
        isSwiping = false;
        hasTriggeredDuringHold = false;
        isSwipeTracking = false;
        currentTouchId = -1;
    }

    /// <summary>
    /// 持枪姿态是否在进行中（招架 / 防御）。
    /// 期间所有手势被吞掉：滑动出招必须先松手退出防御。
    /// </summary>
    private bool StanceActive
    {
        get
        {
            var ps = PlayerState.Instance;
            return ps != null && ps.WeaponStance != null && ps.WeaponStance.Phase != PlayerWeaponStance.StancePhase.Idle;
        }
    }

    private void Update()
    {
        // 暂停时不处理任何输入。
        // 例外：教学时停（allowInputWhileTimeFrozen）——世界停住，但玩家仍要能做手势
        // （招架拍：冻结在出伤窗口里，玩家必须能上划招架）。
        if (!gameplayInputEnabled || (Time.timeScale == 0f && !allowInputWhileTimeFrozen))
        {
            if (isTouching)
            {
                isTouching = false;
                isLongPress = false;
                isCharged = false;
                isSwiping = false;
                hasTriggeredDuringHold = false;
                OnChargeEnded?.Invoke();
            }
            return;
        }

        // 未按住时连段蓄力标记不允许残留
        if (!isTouching) comboChargeActive = false;

        // 输入屏蔽帧：由 UpgradeChoiceManager 在恢复 timeScale 后设置，防止选择选项的点击触发攻击
        if (blockInputFrames > 0)
        {
            blockInputFrames--;
            if (isTouching)
            {
                OnChargeEnded?.Invoke();
            }
            isTouching = false;
            isLongPress = false;
            isCharged = false;
            isSwiping = false;
            hasTriggeredDuringHold = false;
            return;
        }

        // DebugLog.Info($"[InputManager] Update frame={Time.frameCount} timeScale={Time.timeScale} isTouching={isTouching} mouseDown={Input.GetMouseButtonDown(0)} mouseUp={Input.GetMouseButtonUp(0)} mouseHeld={Input.GetMouseButton(0)} touchCount={Input.touchCount}"); // COMMENTED: too verbose

        // BUG FIX: 鼠标和触摸输入互斥
        // 如果有触摸输入，则跳过鼠标输入（避免在触摸屏设备上双重触发）
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        else
        {
            // 鼠标输入（PC调试用）
            HandleMouseInput();
        }

        // 「按下即出」：到点就兑现一记轻攻击（不等抬手）
        UpdatePressTap();

        // 每帧更新当前分段的蓄力状态与进度。
        if (isTouching)
        {            float segmentDuration = SegmentDuration;

            // 横向位移不再清零蓄力：横向移动 = 改目标列（指向），只有纵向抖动受容差限制。
            // 高速滑动仍由 TryDetectHoldSwipe 的速度门控拦下，不会被这里放行。
            float verticalDrift = Mathf.Abs(currentPointerPos.y - segmentStartPos.y);

            if (!isLongPress && !hasTriggeredDuringHold && segmentDuration >= longPressDuration
                && verticalDrift <= chargeMovementTolerance
                && IsChargeAllowed())   // 没有前置招式 → 不进入蓄力态（也就没有蓄力表现）
            {
                isLongPress = true;
            }

            if (isLongPress)
            {
                float chargeProgress = Mathf.Clamp01(segmentDuration / minChargeTime);
                isCharged = chargeProgress >= 1f;
                OnChargeUpdated?.Invoke(currentPointerPos, chargeProgress);
            }
        }
    }

    /// <summary>执行落雷：5×5 网格扩散伤害（切比雪夫距离，加法衰减），BOSS 全额</summary>
    public void ExecuteLightning(UpgradeDefinition def)
    {
        var enemies = EnemyManager.Instance?.GetAllAliveEnemies();
        if (enemies == null || enemies.Count == 0)
        {
            DebugLog.Info("[InputManager] 落雷：无存活敌人");
            return;
        }

        var cfg = def.baseAttackConfig;
        float baseDmg = cfg.damage
            * (UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetDamageMultiplier() : 1f);

        int hitCount = 0;
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.state == EnemyState.Dead) continue;

            int col = enemy.columnIndex;
            int row = enemy.rowIndex;
            int dist = Mathf.Max(Mathf.Abs(col - 2), Mathf.Abs(row - 2));
            if (dist > 2) continue;

            float dmg = enemy.isBoss ? baseDmg : baseDmg * (1f - dist * 0.1f);
            if (dmg <= 0f) continue;

            enemy.TakeDamage(dmg, cfg.damageType);
            hitCount++;
        }

        DebugLog.Info($"[InputManager] 落雷 baseDmg={baseDmg:F0} hit={hitCount}");
    }
}

/// <summary>教学手势拍只接受哪一个手势；None = 不限制（正式战斗）。</summary>
public enum TutorialGestureTarget
{
    None = 0,
    /// <summary>点一下（未滑动、未蓄力）</summary>
    Tap = 1,
    /// <summary>未蓄力的横向滑动</summary>
    SwipeHorizontal = 2,
    /// <summary>按住蓄力后再竖划（上挑）</summary>
    ChargedVertical = 3,
    /// <summary>未蓄力的竖向滑动（= 招架）</summary>
    ParryVertical = 4,
    /// <summary>
    /// **禁止出招**（2026-11-01 教学关用）：这一拍里玩家不能攻击（点按/滑动/蓄力全部不成立），
    /// 但**UI 点击仍然可用**（防御键、技能槽都是 UI 按钮，不走手势通道）。
    ///
    /// 用途：霸体教学拍 —— 敌人正在打霸体技时不能被玩家打死，否则教学瞬间就没了。
    /// 映射上不需要改 <c>InputManager.Mapping</c>：点按判定是「== Tap 才成立」、
    /// 滑动判定的 default 分支返回 false → 新增枚举值自动把所有手势挡住。
    /// </summary>
    BlockAll = 5,
    /// <summary>
    /// **连招蓄力**（2026-11-04 教学拍用）：只接受「按住蓄满后松手」——
    /// 普通的点按 / 滑动都不成立（这一拍要教的是「连招里蓄力释放」，不是站桩蓄力）。
    ///
    /// ⚠️ 教学闸门在**手势路径的第 1 道**（<c>InputManager.ProcessTutorialGate</c>），
    /// 排在「连招释放」那道**前面** → 用 <see cref="BlockAll"/> 会把连招的松手一起 drop 掉，
    /// 所以要单独开这个值：蓄满的按住松手放行，其余全部拦掉。
    /// </summary>
    ComboHold = 6
}
