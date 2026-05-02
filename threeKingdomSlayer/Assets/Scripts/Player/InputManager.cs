using UnityEngine;

/// <summary>
/// 输入管理器
/// 使用Unity Input System检测点击、长按、滑动等手势
/// 区分不同手势区域（屏幕左侧/右侧/中间）
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("手势参数")]
    public float longPressDuration = 0.3f;   // 长按判定时间（秒）
    public float swipeThreshold = 50f;       // 滑动判定最小距离（像素）
    public float screenZoneWidthRatio = 0.3f; // 左右区域宽度比例

    [Header("攻击系统")]
    public AttackSystem attackSystem;

    // 触摸状态
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isTouching;
    private bool isLongPress;
    private bool isSwiping;
    private int currentTouchId = -1;

    // 事件
    public System.Action<AttackType, int> OnAttackExecuted; // attackType, targetColumn

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
        // 鼠标输入（PC调试用）
        HandleMouseInput();
        // 触摸输入（移动端）
        HandleTouchInput();
    }

    #region 鼠标输入

    private void HandleMouseInput()
    {
        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            touchStartTime = Time.time;
            isTouching = true;
            isLongPress = false;
            isSwiping = false;
        }

        // 鼠标按住
        if (isTouching && Input.GetMouseButton(0))
        {
            float pressDuration = Time.time - touchStartTime;
            Vector2 currentPos = (Vector2)Input.mousePosition;
            float swipeDistance = Vector2.Distance(currentPos, touchStartPos);

            // 检测长按
            if (!isLongPress && pressDuration >= longPressDuration && swipeDistance < swipeThreshold)
            {
                isLongPress = true;
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

            ProcessGesture(releasePos, pressDuration, swipeDistance);

            isTouching = false;
            isLongPress = false;
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
                touchStartPos = touch.position;
                touchStartTime = Time.time;
                currentTouchId = touch.fingerId;
                isTouching = true;
                isLongPress = false;
                isSwiping = false;
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

                    if (!isSwiping && swipeDistance >= swipeThreshold)
                    {
                        isSwiping = true;
                    }
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (isTouching && touch.fingerId == currentTouchId)
                {
                    float pressDuration = Time.time - touchStartTime;
                    float swipeDistance = Vector2.Distance(touch.position, touchStartPos);

                    ProcessGesture(touch.position, pressDuration, swipeDistance);

                    isTouching = false;
                    isLongPress = false;
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
    /// </summary>
    private void ProcessGesture(Vector2 releasePos, float pressDuration, float swipeDistance)
    {
        // 1. 滑动手势（距离 >= 阈值）
        if (swipeDistance >= swipeThreshold)
        {
            Vector2 swipeDirection = releasePos - touchStartPos;
            ProcessSwipeGesture(swipeDirection, releasePos);
            return;
        }

        // 2. 长按手势（时间 >= 阈值，距离 < 阈值）
        if (pressDuration >= longPressDuration)
        {
            ProcessLongPressGesture(releasePos);
            return;
        }

        // 3. 点击手势（时间 < 阈值，距离 < 阈值）
        ProcessTapGesture(releasePos);
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
    /// 处理滑动手势
    /// </summary>
    private void ProcessSwipeGesture(Vector2 direction, Vector2 releasePos)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // 归一化角度到 0~360
        if (angle < 0) angle += 360f;

        // 判断滑动方向
        // 水平滑动（左右）：角度在 0±45 或 180±45
        // 垂直滑动（上下）：角度在 90±45 或 270±45
        bool isHorizontal = (angle < 45f || angle > 315f) || (angle > 135f && angle < 225f);
        bool isVertical = (angle > 45f && angle < 135f) || (angle > 225f && angle < 315f);

        // 判断屏幕区域
        ScreenZone zone = GetScreenZone(releasePos);

        if (isHorizontal)
        {
            // 水平滑动 → 斩击（任意区域）
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Slash) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Slash, -1);
        }
        else if (isVertical && zone == ScreenZone.Middle)
        {
            // 中间区域向上滑动 → 挑飞
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Launch) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Launch, -1);
        }
        else if (isHorizontal && (zone == ScreenZone.Left || zone == ScreenZone.Right))
        {
            // 从一侧滑向另一侧 → 横扫
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Sweep) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Sweep, -1);
        }
        else
        {
            // 默认作为斩击处理
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Slash) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Slash, -1);
        }
    }

    #endregion

    #region 屏幕坐标映射

    /// <summary>
    /// 屏幕区域枚举
    /// </summary>
    private enum ScreenZone
    {
        Left,
        Middle,
        Right
    }

    /// <summary>
    /// 根据屏幕坐标判断区域
    /// </summary>
    private ScreenZone GetScreenZone(Vector2 screenPos)
    {
        float screenWidth = Screen.width;
        float zoneWidth = screenWidth * screenZoneWidthRatio;

        if (screenPos.x < zoneWidth)
            return ScreenZone.Left;
        else if (screenPos.x > screenWidth - zoneWidth)
            return ScreenZone.Right;
        else
            return ScreenZone.Middle;
    }

    /// <summary>
    /// 根据屏幕X坐标映射到列索引（0~4）
    /// </summary>
    private int GetColumnFromScreenPosition(Vector2 screenPos)
    {
        float screenWidth = Screen.width;
        // 将屏幕X坐标映射到5列
        float normalizedX = screenPos.x / screenWidth; // 0~1
        int column = Mathf.FloorToInt(normalizedX * 5f);
        return Mathf.Clamp(column, 0, 4);
    }

    #endregion
}
