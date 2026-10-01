using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 输入管理器
/// 使用Unity Input System检测点击、长按、滑动等手势
/// 通过划动角度判定攻击类型（纯方向判定，不依赖屏幕区域）
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private const float SlashSlopeDeadZone = 0.08f;
    private const float SlashMaxVisualTilt = 15f;

    [Header("手势参数")]
    public float longPressDuration = 0.3f;   // 长按判定时间（秒）
    [Tooltip("最小蓄力时间（秒）：所有蓄力攻击（横扫、斩击、穿刺、挑飞）必须按住鼠标达到此时长后，手势判定才生效。\n" +
             "未达到前，任何划动/点击仅触发普通攻击（戳击/斩击）或无操作。")]
    public float minChargeTime = 0.5f;       // 最小蓄力时间（秒）
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
            if (!isLongPress || hasTriggeredDuringHold) return 0;   // 本次按住已经出过招 → 必须先抬手重按
            if (chargeLevelTimes == null || chargeLevelTimes.Length == 0)
                return isCharged ? 1 : 0;
            float held = Time.time - segmentStartTime;
            int level = 0;
            for (int i = 0; i < chargeLevelTimes.Length; i++)
            {
                if (held >= chargeLevelTimes[i]) level = i + 1;
            }
            return level;
        }
    }

    /// <summary>
    /// 指针是否处于「已按下且未被输入门拦下」的状态。
    /// 供招式状态机判断「按住不放时本段先不结束」的宽限与连段蓄力保持。
    /// QTE 输入优先级高于连招，QTE 期间不视为连招输入。
    /// </summary>
    public bool IsPointerDown => isTouching && gameplayInputEnabled && skillInputEnabled
        && blockInputFrames <= 0 && Time.timeScale > 0f && !IsAnyQTEActive();

    /// <summary>按住时长（秒）：连段蓄力用它驱动「拉回蓄势位」的进度</summary>
    public float HoldDurationSeconds => isTouching ? Mathf.Max(0f, Time.time - segmentStartTime) : 0f;

    /// <summary>当前指针位置对应的戳击目标列（C2 的「指向」用）。未落在有效列上时返回 -1</summary>
    public int CurrentPointerStabColumn => GetStabColumnFromScreenPosition(currentPointerPos);

    /// <summary>
    /// 当前这次按下是否被招式状态机判定为「连段蓄力」（按下时置位，抬手或复位时清空）。
    /// 连段蓄力不是穿刺蓄力：依附蓄力事件链的附带表现（穿刺瞄准指示器、蓄力视觉进出场）应当让位。
    /// </summary>
    [System.NonSerialized] public bool comboChargeActive;

    // 技能输入开关（狂怒大招期间关闭）
    [System.NonSerialized] public bool skillInputEnabled = true;
    [System.NonSerialized] public bool gameplayInputEnabled = true;
    /// <summary>输入屏蔽帧计数器（由 UpgradeChoiceManager 设置，防止选择选项的点击触发攻击）</summary>
    [System.NonSerialized] public int blockInputFrames = 0;

    // 触摸状态
    private Vector2 touchStartPos;
    private float touchStartTime;
    private Vector2 segmentStartPos;
    private float segmentStartTime;
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
    }

    private void Start()
    {
        if (attackSystem == null)
            attackSystem = FindObjectOfType<AttackSystem>();
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

    private void Update()
    {
        // 暂停时不处理任何输入
        if (!gameplayInputEnabled || Time.timeScale == 0f)
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

        // 每帧更新当前分段的蓄力状态与进度。
        if (isTouching)
        {
            float segmentDuration = Time.time - segmentStartTime;

            // 横向位移不再清零蓄力：横向移动 = 改目标列（指向），只有纵向抖动受容差限制。
            // 高速滑动仍由 TryDetectHoldSwipe 的速度门控拦下，不会被这里放行。
            float verticalDrift = Mathf.Abs(currentPointerPos.y - segmentStartPos.y);

            if (!isLongPress && !hasTriggeredDuringHold && segmentDuration >= longPressDuration
                && verticalDrift <= chargeMovementTolerance)
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

    #region 鼠标输入

    private void HandleMouseInput()
    {
        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            // UI 之上的点击不处理游戏输入（QTE 活跃时除外：QTE 指示器本身是 UI 元素）
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (overUI && !IsAnyQTEActive())
            {
                isTouching = false;
                return;
            }

            touchStartPos = Input.mousePosition;
            touchStartTime = Time.time;
            segmentStartPos = touchStartPos;
            segmentStartTime = touchStartTime;
            isTouching = true;
            isLongPress = false;
            isCharged = false;
            isSwiping = false;
            hasTriggeredDuringHold = false;
            isSwipeTracking = false;
            currentPointerPos = Input.mousePosition;
            lastFramePos = Input.mousePosition;
            lastFrameTime = Time.time;

            // 触发蓄力开始事件
            OnChargeBegan?.Invoke(currentPointerPos);
        }

        // 鼠标按住
        if (isTouching && Input.GetMouseButton(0))
        {
            currentPointerPos = Input.mousePosition;
            TryDetectHoldSwipe(currentPointerPos);
        }

        // 鼠标松开
        if (Input.GetMouseButtonUp(0) && isTouching)
        {
            Vector2 releasePos = Input.mousePosition;
            float pressDuration = Time.time - touchStartTime;
            float swipeDistance = Vector2.Distance(releasePos, touchStartPos);

            DebugLog.Info($"[InputManager] MouseUp frame={Time.frameCount} pressDuration={pressDuration:F3} swipeDistance={swipeDistance:F1}");
            try
            {
                ProcessGesture(releasePos, pressDuration, swipeDistance);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputManager] ProcessGesture exception: {ex}");
            }

            // 触发蓄力结束事件（在重置状态之前）
            OnChargeEnded?.Invoke();

            isTouching = false;
            isLongPress = false;
            isCharged = false;
            isSwiping = false;
            hasTriggeredDuringHold = false;
        }
    }

    #endregion

    #region 触摸输入

    private void HandleTouchInput()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                // UI 之上的触摸不处理游戏输入（QTE 活跃时除外：QTE 指示器本身是 UI 元素）
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId) && !IsAnyQTEActive())
                {
                    isTouching = false;
                    break;
                }

            touchStartPos = touch.position;
            touchStartTime = Time.time;
            segmentStartPos = touchStartPos;
            segmentStartTime = touchStartTime;
            currentTouchId = touch.fingerId;
            isTouching = true;
            isLongPress = false;
            isCharged = false;
            isSwiping = false;
            hasTriggeredDuringHold = false;
            isSwipeTracking = false;
            currentPointerPos = touch.position;
            lastFramePos = touch.position;
            lastFrameTime = Time.time;

                // 触发蓄力开始事件
                OnChargeBegan?.Invoke(currentPointerPos);
                break;

            case TouchPhase.Moved:
                if (isTouching)
                {
                    currentPointerPos = touch.position;
                    TryDetectHoldSwipe(touch.position);
                }
                break;

            case TouchPhase.Stationary:
                if (isTouching)
                {
                    currentPointerPos = touch.position;
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isTouching && touch.fingerId == currentTouchId)
                {
                    float pressDuration = Time.time - touchStartTime;
                    float swipeDistance = Vector2.Distance(touch.position, touchStartPos);
                    try
                    {
                        ProcessGesture(touch.position, pressDuration, swipeDistance);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[InputManager] ProcessGesture(touch) exception: {ex}");
                    }

                    // 触发蓄力结束事件（在重置状态之前）
                    OnChargeEnded?.Invoke();

                    isTouching = false;
                    isLongPress = false;
                    isCharged = false;
                    isSwiping = false;
                    hasTriggeredDuringHold = false;
                    currentTouchId = -1;
                }
                break;
        }
    }

    #endregion

    #region 手势识别

    /// <summary>
    /// 处理手势识别
    /// 蓄力规则：
    /// - pressDuration >= minChargeTime：蓄力已满，允许判定蓄力攻击（穿刺/横扫/挑飞/斩击）
    /// - pressDuration <  minChargeTime：未达到最小蓄力时间，仅触发普通攻击（戳击/斩击），不触发蓄力攻击
    /// </summary>
    private bool TryConsumeLiveGesture(Vector2 direction, Vector2 releasePos, float segmentDuration, float swipeDistance)
    {
        bool isSwiped = swipeDistance >= swipeThreshold;
        if (TryConsumeStrictQTEInput(releasePos, true, swipeDistance, segmentDuration))
            return true;
        if (IsAnyQTEActive() && TryConsumeQTEInput(releasePos, true, swipeDistance, segmentDuration))
            return true;
        if (!skillInputEnabled)
            return true;

        bool executed;
        MoveGesture directionGesture = MoveGestureDefaults.ClassifySwipe(direction, verticalSwipeThreshold, horizontalSwipeThreshold);

        if (CurrentChargeLevel >= 1)
        {
            // 蓄力滑动：竖滑 → 挑飞，横滑 → 横扫，斜滑 → 斩击。
            // 只有真的出招才算「已消费」：否则这次滑动不该重置蓄力分段、也不该吞掉松手。
            // （连段蓄力时横移换目标列会走到这里，而该节点可能根本没有滑动边）
            executed = ProcessSwipeGesture(direction, releasePos);
            // 连段蓄力：未出招不算消费（保留蓄力与松手的机会）；站桩蓄力：一律算消费（旧语义，避免按住连滑反复刷同一个蓄力招式）
            if (!comboChargeActive) executed = true;
        }
        else
        {
            // 未蓄力滑动：竖滑 → 招架，其余 → 斩击
            executed = SubmitGesture(directionGesture, CurrentChargeLevel, -1, direction.x > 0f, GetSlashVisualTilt(direction));
        }

        if (executed)
        {
            hasTriggeredDuringHold = true;
            lastGestureTime = Time.time;
        }
        return executed;
    }

    /// <summary>
    /// 按住期间的速度门控划动检测（鼠标和触摸共用）。
    /// 仅在瞬时速度达标后才开始累积距离和计时，避免慢速移动被误判。
    /// </summary>
    /// <returns>true 表示触发了招式并已重置分段</returns>
    private bool TryDetectHoldSwipe(Vector2 currentPos)
    {
        float segmentDuration = Time.time - segmentStartTime;
        float verticalDrift = Mathf.Abs(currentPos.y - segmentStartPos.y);

        // 瞬时速度
        float frameDelta = Vector2.Distance(currentPos, lastFramePos);
        float frameTime = Time.time - lastFrameTime;
        float instantSpeed = frameTime > 0.0001f ? frameDelta / frameTime : 0f;
        lastFramePos = currentPos;
        lastFrameTime = Time.time;

        // 速度达标且过了重新识别间隔 → 开始追踪
        if (!isSwipeTracking && instantSpeed >= minSwipeSpeed
            && Time.time - lastGestureTime >= swipeRearmDelay)
        {
            isSwipeTracking = true;
            swipeTrackStartPos = currentPos;
            swipeTrackStartTime = Time.time;
        }

        if (isSwipeTracking)
        {
            float trackDist = Vector2.Distance(currentPos, swipeTrackStartPos);
            float trackDur = Time.time - swipeTrackStartTime;

            if (trackDist >= swipeThreshold && trackDur <= maxSwipeDuration)
            {
                Vector2 direction = currentPos - swipeTrackStartPos;
                DebugLog.Info($"[InputManager] Hold swipe VALID dist={trackDist:F0} dur={trackDur:F3} instantSpeed={instantSpeed:F0}");
                if (TryConsumeLiveGesture(direction, currentPos, segmentDuration, trackDist))
                {
                    ResetSegment(currentPos);
                    lastFramePos = currentPos;
                    lastFrameTime = Time.time;
                    return true;
                }
            }

            // 超时或速度骤降 → 放弃本次追踪
            if (trackDur > maxSwipeDuration || instantSpeed < minSwipeSpeed * 0.5f)
            {
                isSwipeTracking = false;
            }
        }

        // 蓄力条件检查：横向位移视为“指向/改列”，只有纵向抖动受容差限制（与 Update 内一致）；
        // 本次按住已经出过招时不再累积蓄力（必须抬手重按）
        if (!isLongPress && !hasTriggeredDuringHold && segmentDuration >= longPressDuration
            && verticalDrift <= chargeMovementTolerance)
            isLongPress = true;
        if (isLongPress)
            isCharged = segmentDuration >= minChargeTime;

        return false;
    }

    private void ResetSegment(Vector2 position)
    {
        OnChargeEnded?.Invoke();
        segmentStartPos = position;
        segmentStartTime = Time.time;
        isLongPress = false;
        isCharged = false;
        isSwiping = false;
        isSwipeTracking = false;
        OnChargeBegan?.Invoke(position);
    }
    private void CancelChargeAndResetSegment(Vector2 position)
    {
        OnChargeEnded?.Invoke();
        segmentStartPos = position;
        segmentStartTime = Time.time;
        isLongPress = false;
        isCharged = false;
        isSwiping = false;
        isSwipeTracking = false;
        OnChargeBegan?.Invoke(position);
    }
    private void ProcessGesture(Vector2 releasePos, float pressDuration, float swipeDistance)
    {
        if (!skillInputEnabled) return;

        bool isSwiped = swipeDistance >= swipeThreshold;
        if (TryConsumeStrictQTEInput(releasePos, isSwiped, swipeDistance, pressDuration))
            return;

        if (IsAnyQTEActive())
        {
            if (attackSystem != null && attackSystem.IsActionPlaying)
                return;
            if (TryConsumeQTEInput(releasePos, isSwiped, swipeDistance, pressDuration))
                return;
        }

        if (hasTriggeredDuringHold)
            return;

        // 连招蓄力：本次按下已属于连招，已蓄成的那一下松手不再触发射手招式（站桩蓄力）；
        // 普通点击（未蓄力）必须照旧放行，否则连段接不上。
        // 例外：招式仍在进行且该节点配了「松手释放」的边（C2 链）时，把这次松手交给状态机裁定；
        //       状态机在节点激活分支下无匹配边不会回落直通，因此没有该边的节点行为与改造前一致。
        if (comboChargeActive && CurrentChargeLevel >= 1)
        {
            if (moveStateMachine != null && moveStateMachine.IsMoveActive)
            {
                int holdColumn = GetStabColumnFromScreenPosition(releasePos);
                if (holdColumn >= 0)
                    SubmitGesture(MoveGesture.Hold, CurrentChargeLevel, holdColumn);
            }
            return;
        }

        if (CurrentChargeLevel >= 1)
        {
            ProcessLongPressGesture(releasePos);
        }
        else if (isSwiped)
        {
            // 未蓄力的滑动：与按住期间的未蓄力判定一致（竖滑 → 招架，其余 → 斩击）
            Vector2 swipeDirection = releasePos - touchStartPos;
            ProcessSwipeGesture(swipeDirection, releasePos);
        }
        else if (!isSwiped)
        {
            ProcessTapGesture(releasePos);
        }
    }

    /// <summary>
    /// 把手势投递给招式状态机；未配置状态机时直接调用 AttackSystem（保持旧行为）。
    /// 返回是否实际执行了招式；成功后按解析出的攻击类型派发 OnAttackExecuted。
    /// </summary>
    private bool SubmitGesture(MoveGesture gesture, int chargeLevel, int targetColumn,
        bool slashLeftToRight = true, float slashVisualTilt = 0f)
    {
        bool charged = chargeLevel >= 1;
        var input = new GestureInput
        {
            gesture = gesture,
            charged = charged,
            chargeLevel = chargeLevel,
            targetColumn = targetColumn,
            slashLeftToRight = slashLeftToRight,
            slashVisualTilt = slashVisualTilt,
            timestamp = Time.time
        };

        AttackType resolvedType;
        bool executed;
        if (moveStateMachine != null)
        {
            executed = moveStateMachine.SubmitInput(input, out resolvedType);
        }
        else if (attackSystem != null)
        {
            resolvedType = MoveGestureDefaults.ResolveAttackType(gesture, charged);
            executed = attackSystem.TryExecuteAttack(resolvedType, targetColumn,
                slashLeftToRight, slashVisualTilt, charged, chargeLevel: chargeLevel);
        }
        else
        {
            return false;
        }

        if (executed)
            OnAttackExecuted?.Invoke(resolvedType, targetColumn);

        return executed;
    }

    /// <summary>
    /// 处理点击手势 → 戳击
    /// </summary>
    private void ProcessTapGesture(Vector2 position)
    {
        int column = GetStabColumnFromScreenPosition(position);
        if (column >= 0)
        {
            SubmitGesture(MoveGesture.Tap, CurrentChargeLevel, column);
        }
    }

    /// <summary>
    /// 处理长按手势 → 穿刺
    /// </summary>
    private void ProcessLongPressGesture(Vector2 position)
    {
        int column = GetStabColumnFromScreenPosition(position);
        if (column >= 0)
        {
            SubmitGesture(MoveGesture.Hold, CurrentChargeLevel, column);
        }
    }

    /// <summary>
    /// 处理滑动手势（纯角度判定）
    /// 规则：
    /// - 近垂直划动（与垂直方向夹角 < verticalSwipeThreshold）→ 挑飞 Launch
    /// - 近水平划动（与水平方向夹角 < horizontalSwipeThreshold）→ 横扫 Sweep
    /// - 对角线划动 → 斩击 Slash（兜底）
    /// 不再依赖屏幕区域（Left/Middle/Right），仅通过划动角度识别
    /// </summary>
    private bool ProcessSwipeGesture(Vector2 direction, Vector2 releasePos)
    {
        // 计算方向向量与垂直轴（上方向 = (0,1)）的夹角
        float angleToVertical = Vector2.Angle(direction, Vector2.up);
        // 计算方向向量与水平轴（右方向 = (1,0)）的夹角
        float angleToHorizontal = Vector2.Angle(direction, Vector2.right);

        // 近垂直划动（上/下）→ 挑飞
        // 方向与垂直轴夹角 < verticalSwipeThreshold
        if (angleToVertical < verticalSwipeThreshold)
            return SubmitGesture(MoveGesture.SwipeVertical, CurrentChargeLevel, -1);

        // 近水平划动（左/右）→ 横扫
        // 方向与水平轴夹角 < horizontalSwipeThreshold
        if (angleToHorizontal < horizontalSwipeThreshold)
            return SubmitGesture(MoveGesture.SwipeHorizontal, CurrentChargeLevel, -1);

        // 对角线划动 → 斩击（兜底）
        bool slashLeftToRight = direction.x > 0;
        float slashVisualTilt = GetSlashVisualTilt(direction);
        Debug.Log($"[SlashTilt] Input charged dir={direction} leftToRight={slashLeftToRight} tilt={slashVisualTilt:F2}");
        return SubmitGesture(MoveGesture.SwipeDiagonal, CurrentChargeLevel, -1, slashLeftToRight, slashVisualTilt);
    }

    private float GetSlashVisualTilt(Vector2 direction)
    {
        float horizontal = Mathf.Abs(direction.x);
        if (horizontal < 0.001f)
            return 0f;
        float slope = direction.y / horizontal;
        if (Mathf.Abs(slope) <= SlashSlopeDeadZone)
            return 0f;
        return Mathf.Clamp(Mathf.Atan(slope) * Mathf.Rad2Deg, -SlashMaxVisualTilt, SlashMaxVisualTilt);
    }

    #endregion

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

    #region 屏幕坐标映射

    public int GetPierceColumnFromScreenPosition(Vector2 screenPos)
    {
        return GetStabColumnFromScreenPosition(screenPos);
    }

    private int GetStabColumnFromScreenPosition(Vector2 screenPos)
    {
        int targetedColumn = GetColumnFromScreenPosition(screenPos);
        return targetedColumn >= 0 ? targetedColumn : FallbackGetColumn(screenPos);
    }

    /// <summary>
    /// 根据屏幕X坐标映射到列索引（0~4）
    /// 将敌人投影到屏幕空间，直接比较屏幕X距离。
    /// 如果最近匹配超过半列宽度，返回 -1 阻断攻击（防止空列自动跳转到邻列）。
    /// </summary>
    private int GetColumnFromScreenPosition(Vector2 screenPos)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return FallbackGetColumn(screenPos);

        int bestColumn = -1;
        float bestDistance = float.MaxValue;

        if (AttackSystem.Instance != null && AttackSystem.Instance.columnManager != null)
        {
            for (int col = 0; col < 5; col++)
            {
                Enemy frontEnemy = AttackSystem.Instance.columnManager.GetFrontEnemy(col);
                if (frontEnemy != null && frontEnemy.state != EnemyState.Dead)
                {
                    Vector3 enemyScreenPos = mainCamera.WorldToScreenPoint(frontEnemy.transform.position);
                    float dist = Mathf.Abs(enemyScreenPos.x - screenPos.x);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestColumn = col;
                    }
                }
            }
        }

        // 阈值：半列宽度。超过此距离说明点击位置与存活的敌人列偏差过大，阻断攻击
        float halfColumnWidth = Screen.width / 10f;
        if (bestColumn >= 0 && bestDistance <= halfColumnWidth)
            return bestColumn;

        // 没有足够近的存活敌人 → 返回 -1，攻击不会触发
        return -1;
    }

    /// <summary>
    /// 回退方案：将屏幕X坐标均匀映射到5列
    /// </summary>
    private int FallbackGetColumn(Vector2 screenPos)
    {
        float screenWidth = Screen.width;
        float normalizedX = screenPos.x / screenWidth; // 0~1
        int column = Mathf.FloorToInt(normalizedX * 5f);
        return Mathf.Clamp(column, 0, 4);
    }

    #endregion

    #region QTE 输入

    private QTEController _cachedQTEController;

    private bool IsAnyQTEActive()
    {
        if (_cachedQTEController == null)
            _cachedQTEController = FindObjectOfType<QTEController>();
        return _cachedQTEController != null && _cachedQTEController.IsQTEActive;
    }

    private bool TryConsumeStrictQTEInput(Vector2 releasePos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        if (_cachedQTEController == null)
            _cachedQTEController = FindObjectOfType<QTEController>();
        if (_cachedQTEController == null || !_cachedQTEController.IsStrictInputActive)
            return false;

        return _cachedQTEController.TryStrictInput(touchStartPos, releasePos, isSwiped, swipeDistance, pressDuration);
    }

    /// <summary>
    /// 尝试将当前手势作为 QTE 输入消费
    /// 若 QTE 活跃且手势匹配成功，返回 true（阻止普通攻击处理）
    /// </summary>
    private bool TryConsumeQTEInput(Vector2 releasePos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        if (_cachedQTEController == null)
            _cachedQTEController = FindObjectOfType<QTEController>();
        if (_cachedQTEController == null || !_cachedQTEController.IsQTEActive)
            return false;

        if (isSwiped)
        {
            Vector2 direction = releasePos - touchStartPos;
            float swipeSpeed = swipeDistance / Mathf.Max(pressDuration, 0.001f);
            if (_cachedQTEController.TryQTESwipe(touchStartPos, direction, swipeSpeed, releasePos))
            {
                DebugLog.Info($"[InputManager] QTE 划动成功 speed={swipeSpeed:F0}");
                return true;
            }
        }
        else
        {
            if (_cachedQTEController.TryQTEClick(releasePos))
            {
                DebugLog.Info("[InputManager] QTE 点击成功");
                return true;
            }
        }

        return false;
    }

    #endregion
}
