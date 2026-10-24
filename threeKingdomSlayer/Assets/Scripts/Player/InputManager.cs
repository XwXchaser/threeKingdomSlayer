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

    [Header("蓄力招式开关")]
    [Tooltip("打开后只允许竖滑蓄力（上挑）：站桩蓄力（穿刺）与横滑蓄力（横扫）都不成立，蓄力指示器与蓄力武器视觉也不出现。\n蓄力 stab / 蓄力 slash 还没做完 —— 2026-10-21 起默认关闭（用户要求恢复蓄力表现），打开前先确认蓄力出招与命中正常。")]
    public bool restrictChargedToVertical = false;

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
    /// <summary>教学手势拍：非 None 时只接受这个目标手势，其它手势一律不成立（不出招、也不消耗蓄力段）。</summary>
    [System.NonSerialized] public TutorialGestureTarget tutorialGestureTarget = TutorialGestureTarget.None;
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
    ChargedVertical = 3
}
