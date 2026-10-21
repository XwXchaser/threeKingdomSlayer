using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
    /// InputManager 的**指针输入**：鼠标与触摸的按下/抬起/移动采集。
    ///
    /// 按机制从 <c>InputManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 InputManager.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class InputManager : MonoBehaviour
{
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
}