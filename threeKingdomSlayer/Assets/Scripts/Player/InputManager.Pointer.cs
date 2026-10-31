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
        return GetTopUIAt(screenPos) != null;
    }

    /// <summary>取该屏幕点**最上面**的可交互 UI（没有则 null）。</summary>
    private static GameObject GetTopUIAt(Vector2 screenPos)
    {
        var es = EventSystem.current;
        if (es == null) return null;
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
            return go;
        }
        return null;
    }

    /// <summary>
    /// **区域位置识别**：这次点击算「战斗手势」还是「UI 点击」。
    ///
    /// 背景（2026-10-29 用户定）：防御键这类**动作按钮**就压在敌人所在的画面区域上，
    /// 玩家点的是**敌人** → 必须是攻击，不能被按钮吞掉。
    /// 但**模态 UI**（三选一 / 对话 / 结算面板）仍然照旧吞掉点击 → 不能在面板上打出去。
    ///
    /// 判定：UI 命中的是**动作按钮**（<see cref="IActionButton"/>）且它判定“这次点击让给战斗” → 放行手势；
    /// 其余情况一律按 UI 点击处理（手势被抑制）。
    /// </summary>
    private static bool ShouldKeepGestureOverUI(Vector2 screenPos)
    {
        var ui = GetTopUIAt(screenPos);
        if (ui == null) return false;
        var actionButton = ui.GetComponent<IActionButton>();
        if (actionButton == null) return false;   // 模态 UI 不放行
        return actionButton.ShouldYieldToCombat(screenPos);
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
                // 区域位置识别：点在动作按钮（防御键）上、但下面有敌人 → 当作战斗手势
                if (!ShouldKeepGestureOverUI(Input.mousePosition))
                {
                    isTouching = false;
                    return;
                }
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

        Touch touch = PickGameplayTouch();
        if (touch.fingerId < 0) return;   // 没有可用的游戏手指（都在 UI 上）

        switch (touch.phase)
        {
            case TouchPhase.Began:
                // UI 之上的触摸不处理游戏输入（QTE 活跃时除外：QTE 指示器本身是 UI 元素）
                if (IsPointerOverUIAt(touch.position) && !IsAnyQTEActive()
                    && !ShouldKeepGestureOverUI(touch.position))
                {
                    isTouching = false;
                    break;
                }

                BeginTouchTracking(touch);
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

    /// <summary>
    /// 选一根手指做**游戏指针**。
    ///
    /// 规则（2026-10-29 用户定：hold 防御时不能触发蓄力等长按）：
    ///   • 已经跟踪着的手指优先（保持手势连续性）；
    ///   • 否则取**第一根不在 UI 上**的手指 —— 按住防御键 / 点 UI 的那根手指
    ///     **永远不会**成为游戏指针 → 不会误触发蓄力、长按、点按。
    ///   • 全都在 UI 上时，退回 touch[0]，让 Began 分支自己去判“这次是 UI 点击，不算手势”。
    /// </summary>
    private Touch PickGameplayTouch()
    {
        // ① 已跟踪的手指还在 → 继续用它
        if (currentTouchId >= 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId == currentTouchId) return t;
            }
        }

        // ② 取第一根不在 UI 上的手指
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (IsPointerOverUIAt(t.position) && !IsAnyQTEActive()) continue;
            return t;
        }

        // ③ 全都在 UI 上：交给 Began 分支判定（它会因在 UI 上而拒绝）
        if (currentTouchId < 0 && Input.touchCount > 0) return Input.GetTouch(0);

        return new Touch { fingerId = -1 };
    }

    /// <summary>开始跟踪一根手指作为游戏指针（起手、蓄力起点）。</summary>
    private void BeginTouchTracking(Touch touch)
    {
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
    }

    #endregion
}