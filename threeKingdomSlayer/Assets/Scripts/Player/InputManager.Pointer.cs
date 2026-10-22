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
    /// <summary>
    /// 指针位置是否在 UI 元素上 ✅。
    ///
    /// **不能只用 <c>EventSystem.IsPointerOverGameObject()</c>** ✅：那读的是 EventSystem
    /// **上一帧**的射线缓存 ✅ —— 指针刚按下那一帧还没更新 ✅ → UI 点击会漏成游戏手势 ✅
    /// （表现：点按钮被判成攻击手势 ✅）。所以这里**当场做一次射线检测** ✅（与更新顺序无关 ✅）。
    /// </summary>
    private static bool IsPointerOverUIAt(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        var data = new PointerEventData(es) { position = screenPos };
        var results = new System.Collections.Generic.List<RaycastResult>();
        es.RaycastAll(data, results);
        for (int i = 0; i < results.Count; i++)
        {
            var go = results[i].gameObject;
            if (go == null) continue;
            // 不可交互的（blocksRaycasts 关闭的）不挡 ✅
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null && !cg.blocksRaycasts) continue;
            return true;
        }
        return false;
    }

    #region 鼠标输入

    private void HandleMouseInput()
    {
        // 鼠标按下
        if (Input.GetMouseButtonDown(0))
        {
            // UI 之上的点击不处理游戏输入（QTE 活跃时除外：QTE 指示器本身是 UI 元素）
            bool overUI = IsPointerOverUIAt(Input.mousePosition);
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
                if (IsPointerOverUIAt(touch.position) && !IsAnyQTEActive())
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