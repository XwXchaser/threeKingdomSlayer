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

    [Header("手势参数")]
    public float longPressDuration = 0.3f;   // 长按判定时间（秒）
    [Tooltip("最小蓄力时间（秒）：所有蓄力攻击（横扫、斩击、穿刺、挑飞）必须按住鼠标达到此时长后，手势判定才生效。\n" +
             "未达到前，任何划动/点击仅触发普通攻击（戳击/斩击）或无操作。")]
    public float minChargeTime = 0.5f;       // 最小蓄力时间（秒）
    public float swipeThreshold = 50f;       // 滑动判定最小距离（像素）
    [Tooltip("垂直角度阈值（度）：划动方向与垂直轴夹角小于此值时判定为挑飞。范围0~90")]
    public float verticalSwipeThreshold = 30f;
    [Tooltip("水平角度阈值（度）：划动方向与水平轴夹角小于此值时判定为横扫。范围0~90")]
    public float horizontalSwipeThreshold = 30f;
    [Tooltip("长按下滑角度阈值（度）：长按后划动方向与下方向夹角小于此值时判定为落雷道具。")]
    public float swipeDownAngleThreshold = 30f;

    [Header("工具引用")]
    public AttackSystem attackSystem;
    public WhirlwindController whirlwindController;

    // 技能输入开关（狂怒大招期间关闭）
    [System.NonSerialized] public bool skillInputEnabled = true;
    /// <summary>输入屏蔽帧计数器（由 UpgradeChoiceManager 设置，防止选择选项的点击触发攻击）</summary>
    [System.NonSerialized] public int blockInputFrames = 0;

    // 触摸状态
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isTouching;
    private bool isLongPress;
    private bool isCharged;     // 蓄力是否已满（pressDuration >= minChargeTime）
    private bool isSwiping;
    private int currentTouchId = -1;
    private Vector2 currentPointerPos; // 当前指针位置（鼠标/触摸），用于蓄力指示器

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
    }

    private void Update()
    {
        // 暂停时不处理任何输入
        if (Time.timeScale == 0f)
        {
            if (isTouching)
            {
                isTouching = false;
                isLongPress = false;
                isCharged = false;
                isSwiping = false;
                OnChargeEnded?.Invoke();
            }
            return;
        }

        // 输入屏蔽帧：由 UpgradeChoiceManager 在恢复 timeScale 后设置，防止选择选项的点击触发攻击
        if (blockInputFrames > 0)
        {
            blockInputFrames--;
            isTouching = false;
            isLongPress = false;
            isCharged = false;
            isSwiping = false;
            return;
        }

        Debug.Log($"[InputManager] Update frame={Time.frameCount} timeScale={Time.timeScale} isTouching={isTouching} mouseDown={Input.GetMouseButtonDown(0)} mouseUp={Input.GetMouseButtonUp(0)} mouseHeld={Input.GetMouseButton(0)} touchCount={Input.touchCount}");

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

        // 每帧更新蓄力进度（鼠标/触摸通用）
        if (isTouching)
        {
            float pressDuration = Time.time - touchStartTime;
            float chargeProgress = Mathf.Clamp01(pressDuration / minChargeTime);
            OnChargeUpdated?.Invoke(currentPointerPos, chargeProgress);

            // 画圈道具检测（按住中每帧）
            if (whirlwindController != null)
            {
                if (whirlwindController.IsActive)
                {
                    whirlwindController.TickActive(currentPointerPos);
                }
                else if (whirlwindController.CanCircle)
                {
                    if (whirlwindController.UpdateCircleDetection(currentPointerPos))
                    {
                        if (ItemInventory.Instance != null && ItemInventory.Instance.TryConsume("circle"))
                        {
                            var def = ItemInventory.Instance.GetDefinition("circle");
                            whirlwindController.Activate(def);
                        }
                        else
                        {
                            whirlwindController.ResetCircleDetection();
                        }
                    }
                }
            }
        }
    }

    #region 鼠标输入

    private void HandleMouseInput()
    {
        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            // UI 之上的点击不处理游戏输入
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            Debug.Log($"[InputManager] MouseDown frame={Time.frameCount} overUI={overUI}");
            if (overUI)
                return;

            touchStartPos = Input.mousePosition;
            touchStartTime = Time.time;
            isTouching = true;
            isLongPress = false;
            isSwiping = false;
            currentPointerPos = Input.mousePosition;

            // 触发蓄力开始事件
            OnChargeBegan?.Invoke(currentPointerPos);
        }

        // 鼠标按住
        if (isTouching && Input.GetMouseButton(0))
        {
            // 更新当前指针位置（供蓄力指示器跟随鼠标）
            currentPointerPos = Input.mousePosition;

            float pressDuration = Time.time - touchStartTime;
            Vector2 currentPos = (Vector2)Input.mousePosition;
            float swipeDistance = Vector2.Distance(currentPos, touchStartPos);

            // 检测长按
            if (!isLongPress && pressDuration >= longPressDuration && swipeDistance < swipeThreshold)
            {
                isLongPress = true;
            }

            // 检测蓄力完成（pressDuration >= minChargeTime）
            if (!isCharged && pressDuration >= minChargeTime)
            {
                isCharged = true;
            }

            // 检测滑动
            if (!isSwiping && swipeDistance >= swipeThreshold)
            {
                isSwiping = true;
            }
        }

        // 鼠标松开
        if (Input.GetMouseButtonUp(0) && isTouching)
        {
            Vector2 releasePos = Input.mousePosition;
            float pressDuration = Time.time - touchStartTime;
            float swipeDistance = Vector2.Distance(releasePos, touchStartPos);

            Debug.Log($"[InputManager] MouseUp frame={Time.frameCount} pressDuration={pressDuration:F3} swipeDistance={swipeDistance:F1}");

            if (whirlwindController != null && whirlwindController.IsActive)
            {
                whirlwindController.Deactivate();
            }
            else
            {
                ProcessGesture(releasePos, pressDuration, swipeDistance);
            }

            // 触发蓄力结束事件（在重置状态之前）
            OnChargeEnded?.Invoke();

            isTouching = false;
            isLongPress = false;
            isCharged = false;
            isSwiping = false;
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
                // UI 之上的触摸不处理游戏输入
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    break;

                touchStartPos = touch.position;
                touchStartTime = Time.time;
                currentTouchId = touch.fingerId;
                isTouching = true;
                isLongPress = false;
                isSwiping = false;
                currentPointerPos = touch.position;

                // 触发蓄力开始事件
                OnChargeBegan?.Invoke(currentPointerPos);
                break;

            case TouchPhase.Moved:
                if (isTouching)
                {
                    float pressDuration = Time.time - touchStartTime;
                    float swipeDistance = Vector2.Distance(touch.position, touchStartPos);

                    if (!isLongPress && pressDuration >= longPressDuration && swipeDistance < swipeThreshold)
                    {
                        isLongPress = true;
                    }

                    if (!isCharged && pressDuration >= minChargeTime)
                    {
                        isCharged = true;
                    }

                    if (!isSwiping && swipeDistance >= swipeThreshold)
                    {
                        isSwiping = true;
                    }

                    currentPointerPos = touch.position;
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

                    if (whirlwindController != null && whirlwindController.IsActive)
                    {
                        whirlwindController.Deactivate();
                    }
                    else
                    {
                        ProcessGesture(touch.position, pressDuration, swipeDistance);
                    }

                    // 触发蓄力结束事件（在重置状态之前）
                    OnChargeEnded?.Invoke();

                    isTouching = false;
                    isLongPress = false;
                    isCharged = false;
                    isSwiping = false;
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
    private void ProcessGesture(Vector2 releasePos, float pressDuration, float swipeDistance)
    {
        Debug.Log($"[InputManager] ProcessGesture frame={Time.frameCount} pressDuration={pressDuration:F3} swipeDistance={swipeDistance:F1} skillInputEnabled={skillInputEnabled}");
        if (!skillInputEnabled) return;

        bool isSwiped = swipeDistance >= swipeThreshold;

        // QTE 优先级检测：QTE 窗口内优先匹配 QTE（窗口短暂且后果重大）
        if (TryConsumeQTEInput(releasePos, isSwiped, swipeDistance, pressDuration))
            return;

        // 道具手势检测：长按+下滑 → 落雷（QTE 之下、攻击之上）
        if (TryConsumeItemGesture(releasePos, isSwiped, swipeDistance, pressDuration))
            return;

        if (pressDuration >= minChargeTime)
        {
            // 蓄力已满 → 蓄力攻击判定
            if (isSwiped)
            {
                Vector2 swipeDirection = releasePos - touchStartPos;
                ProcessSwipeGesture(swipeDirection, releasePos);
            }
            else
            {
                // 长按后松开（无滑动） → 穿刺
                ProcessLongPressGesture(releasePos);
            }
        }
        else
        {
            // 未达到最小蓄力时间 → 仅触发普通攻击
            if (isSwiped)
            {
                Vector2 swipeDirection = releasePos - touchStartPos;
                float angleToVertical = Vector2.Angle(swipeDirection, Vector2.up);
                if (angleToVertical < verticalSwipeThreshold)
                {
                    // 无蓄力垂直划动 → 招架
                    bool executed = attackSystem?.TryExecuteAttack(AttackType.Parry) ?? false;
                    if (executed) OnAttackExecuted?.Invoke(AttackType.Parry, -1);
                }
                else
                {
                    // 快速滑动 → 斩击
                    bool executed = attackSystem?.TryExecuteAttack(AttackType.Slash) ?? false;
                    if (executed) OnAttackExecuted?.Invoke(AttackType.Slash, -1);
                }
            }
            else
            {
                // 快速点击 → 戳击
                ProcessTapGesture(releasePos);
            }
        }
    }

    /// <summary>
    /// 处理点击手势 → 戳击
    /// </summary>
    private void ProcessTapGesture(Vector2 position)
    {
        int column = GetColumnFromScreenPosition(position);
        if (column >= 0)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Stab, column) ?? false;
            if (executed)
            {
                OnAttackExecuted?.Invoke(AttackType.Stab, column);
            }
        }
    }

    /// <summary>
    /// 处理长按手势 → 穿刺
    /// </summary>
    private void ProcessLongPressGesture(Vector2 position)
    {
        int column = GetColumnFromScreenPosition(position);
        if (column >= 0)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Pierce, column) ?? false;
            if (executed)
            {
                OnAttackExecuted?.Invoke(AttackType.Pierce, column);
            }
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
    private void ProcessSwipeGesture(Vector2 direction, Vector2 releasePos)
    {
        // 计算方向向量与垂直轴（上方向 = (0,1)）的夹角
        float angleToVertical = Vector2.Angle(direction, Vector2.up);
        // 计算方向向量与水平轴（右方向 = (1,0)）的夹角
        float angleToHorizontal = Vector2.Angle(direction, Vector2.right);

        // 近垂直划动（上/下）→ 挑飞
        // 方向与垂直轴夹角 < verticalSwipeThreshold
        if (angleToVertical < verticalSwipeThreshold)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Launch) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Launch, -1);
            return;
        }

        // 近水平划动（左/右）→ 横扫
        // 方向与水平轴夹角 < horizontalSwipeThreshold
        if (angleToHorizontal < horizontalSwipeThreshold)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Sweep) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Sweep, -1);
            return;
        }

        // 对角线划动 → 斩击（兜底）
        bool defaultExecuted = attackSystem?.TryExecuteAttack(AttackType.Slash) ?? false;
        if (defaultExecuted) OnAttackExecuted?.Invoke(AttackType.Slash, -1);
    }

    #endregion

    #region 道具手势

    /// <summary>
    /// 尝试将当前手势作为道具输入消费（QTE 之后、攻击之前）
    /// 仅支持 long_press_swipe_down（画圈已在 Update 中按住检测处理）
    /// </summary>
    private bool TryConsumeItemGesture(Vector2 releasePos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        if (!isSwiped) return false;
        if (ItemInventory.Instance == null) return false;
        if (!ItemInventory.Instance.HasItem("long_press_swipe_down")) return false;

        // 长按判定
        if (pressDuration < longPressDuration) return false;

        // 下滑方向判定
        Vector2 swipeDir = releasePos - touchStartPos;
        float angleToDown = Vector2.Angle(swipeDir, Vector2.down);
        if (angleToDown > swipeDownAngleThreshold) return false;

        // 消耗道具
        if (!ItemInventory.Instance.TryConsume("long_press_swipe_down"))
            return false;

        var def = ItemInventory.Instance.GetDefinition("long_press_swipe_down");
        if (def == null || def.baseAttackConfig == null)
        {
            Debug.LogWarning("[InputManager] 落雷道具定义或 baseAttackConfig 为空");
            return true; // 已消耗，但不执行
        }

        ExecuteLightning(def);
        return true;
    }

    /// <summary>执行落雷：5×5 网格扩散伤害（切比雪夫距离，加法衰减），BOSS 全额</summary>
    private void ExecuteLightning(UpgradeDefinition def)
    {
        var enemies = EnemyManager.Instance?.GetAllAliveEnemies();
        if (enemies == null || enemies.Count == 0)
        {
            Debug.Log("[InputManager] 落雷：无存活敌人");
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

        Debug.Log($"[InputManager] 落雷 baseDmg={baseDmg:F0} hit={hitCount}");
    }

    #endregion

    #region 屏幕坐标映射

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
                Debug.Log($"[InputManager] QTE 划动成功 speed={swipeSpeed:F0}");
                return true;
            }
        }
        else
        {
            if (_cachedQTEController.TryQTEClick(releasePos))
            {
                Debug.Log("[InputManager] QTE 点击成功");
                return true;
            }
        }

        return false;
    }

    #endregion
}
