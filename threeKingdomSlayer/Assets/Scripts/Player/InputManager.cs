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
    /// 设计文档规则：
    /// - 水平滑动（左右）：斩击（任意区域）
    /// - 从屏幕一侧（Left/Right）水平滑向另一侧：横扫
    /// - 中间区域垂直滑动（上下）：挑飞
    /// 优先级：横扫 > 挑飞 > 斩击
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
        ScreenZone startZone = GetScreenZone(touchStartPos);

        // 优先级1：从屏幕一侧水平滑向另一侧 → 横扫
        if (isHorizontal && startZone != ScreenZone.Middle && zone != ScreenZone.Middle && startZone != zone)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Sweep) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Sweep, -1);
            return;
        }

        // 优先级2：中间区域垂直滑动 → 挑飞
        if (isVertical && zone == ScreenZone.Middle)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Launch) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Launch, -1);
            return;
        }

        // 优先级3：水平滑动 → 斩击（兜底）
        if (isHorizontal)
        {
            bool executed = attackSystem?.TryExecuteAttack(AttackType.Slash) ?? false;
            if (executed) OnAttackExecuted?.Invoke(AttackType.Slash, -1);
            return;
        }

        // 其他情况也作为斩击处理
        bool defaultExecuted = attackSystem?.TryExecuteAttack(AttackType.Slash) ?? false;
        if (defaultExecuted) OnAttackExecuted?.Invoke(AttackType.Slash, -1);
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
    ///
    /// BUG FIX: 使用基于实际敌人位置的最近列检测，而非简单的屏幕均匀分割。
    /// 之前简单地将屏幕X坐标均匀映射到5列，但阵型是梯形/扇形的，
    /// 列的实际X位置不是均匀分布的，导致点击A列却打到B列。
    ///
    /// 新方案：遍历所有列的最前排敌人，找到屏幕X坐标最近的敌人所在列。
    /// 如果没有任何敌人，则回退到均匀分割方案。
    /// </summary>
    private int GetColumnFromScreenPosition(Vector2 screenPos)
    {
        // BUG FIX: 使用基于实际敌人位置的最近列检测
        // 将屏幕坐标转换为世界坐标（通过摄像机）
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // 回退到均匀分割
            return FallbackGetColumn(screenPos);
        }

        // 创建一条从摄像机穿过屏幕点的射线
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        
        // 在Z=0平面上计算射线与平面的交点
        // 假设敌人在Z=0平面附近（实际敌人Z坐标可能不同，但用于列判定足够了）
        float planeZ = 0f;
        float rayDistance = (planeZ - ray.origin.z) / ray.direction.z;
        Vector3 worldPoint = ray.origin + ray.direction * rayDistance;

        // 遍历所有列的最前排敌人，找到最近的列
        int bestColumn = -1;
        float bestDistance = float.MaxValue;

        if (AttackSystem.Instance != null && AttackSystem.Instance.columnManager != null)
        {
            for (int col = 0; col < 5; col++)
            {
                Enemy frontEnemy = AttackSystem.Instance.columnManager.GetFrontEnemy(col);
                if (frontEnemy != null && frontEnemy.state != EnemyState.Dead)
                {
                    // 使用敌人的世界X坐标与射线交点X坐标的距离
                    float dist = Mathf.Abs(frontEnemy.transform.position.x - worldPoint.x);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestColumn = col;
                    }
                }
            }
        }

        // 如果找到了最近的敌人列，使用它
        if (bestColumn >= 0)
        {
            return bestColumn;
        }

        // 回退：如果没有敌人，使用均匀分割
        return FallbackGetColumn(screenPos);
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
}
