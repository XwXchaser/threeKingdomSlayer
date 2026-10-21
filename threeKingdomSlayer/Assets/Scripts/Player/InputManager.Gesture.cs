using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
    /// InputManager 的**手势识别**：滑动方向、长按、蓄力分段与招式提交。
    ///
    /// 按机制从 <c>InputManager.cs</c> 拆出（2026-10-22，partial 分文件）。
    /// partial class 与 InputManager.cs **同类型**，行为完全一致，只为降低单文件体量与阅读成本。
    /// </summary>

public partial class InputManager : MonoBehaviour
{
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

        // 教学手势拍：不是目标手势就不执行（也不算消费，蓄力段保留给正确动作）
        if (!TutorialAllowsSwipeGesture(directionGesture, CurrentChargeLevel)) return false;

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

        // 手势过路径拆为多个独立 gate，每个 gate 返回 true = 已消耗 / 已处理（stop 后续）。
        // 顺序与原原硬编码顺序一致：
        //   1) 教学手势拍过滤（drop 非目标手势）
        //   2) QTE 输入消费（吃掉输入避免后续误触）
        //   3) hold 中已出过招（松手不产生攻击）
        //   4) combo 链释放（把松手交给招式状态机裁定（C2 链在这里触发））
        //   5) 默认路径：以上都过过的，释放普通戳 / 长穿 / 滑斩
        // 新增门控只要在这里追加一行即可，不用改 ProcessGesture 主体。
        if (ProcessTutorialGate(releasePos, isSwiped)) return;
        if (ProcessQTEGate(releasePos, isSwiped, swipeDistance, pressDuration)) return;
        if (ProcessHoldGate()) return;
        if (ProcessComboBranchGate(releasePos)) return;
        ProcessDefaultPathGate(releasePos, isSwiped);
    }

    /// <summary>教学手势拍：返回 true 表示输入被 drop（不是目标手势）。</summary>
    private bool ProcessTutorialGate(Vector2 releasePos, bool isSwiped)
    {
        if (tutorialGestureTarget == TutorialGestureTarget.None) return false;
        int tutorialCharge = CurrentChargeLevel;
        if (isSwiped)
        {
            var tutorialDirection = MoveGestureDefaults.ClassifySwipe(releasePos - touchStartPos,
                                                                 verticalSwipeThreshold, horizontalSwipeThreshold);
            if (!TutorialAllowsSwipeGesture(tutorialDirection, tutorialCharge)) return true;
        }
        else if (!TutorialAllowsTapGesture(tutorialCharge)) return true;
        return false;
    }

    /// <summary>QTE 输入消费：消费成功则 return true。</summary>
    private bool ProcessQTEGate(Vector2 releasePos, bool isSwiped, float swipeDistance, float pressDuration)
    {
        if (TryConsumeStrictQTEInput(releasePos, isSwiped, swipeDistance, pressDuration)) return true;
        if (IsAnyQTEActive())
        {
            if (attackSystem != null && attackSystem.IsActionPlaying) return true;
            if (TryConsumeQTEInput(releasePos, isSwiped, swipeDistance, pressDuration)) return true;
        }
        return false;
    }

    /// <summary>一次按住期间已出过招：本帧松手不再产生攻击。</summary>
    private bool ProcessHoldGate() => hasTriggeredDuringHold;

    /// <summary>
    /// combo 链释放：松手给招式状态机判定（C2 连段会在这里触发）。
    /// 返回 true = 已处理（本帧不再走默认路径）。
    /// </summary>
    private bool ProcessComboBranchGate(Vector2 releasePos)
    {
        if (!comboChargeActive || CurrentChargeLevel < 1) return false;
        if (moveStateMachine != null && moveStateMachine.IsMoveActive)
        {
            int holdColumn = GetStabColumnFromScreenPosition(releasePos);
            if (holdColumn >= 0)
                SubmitGesture(MoveGesture.Hold, CurrentChargeLevel, holdColumn);
        }
        return true;
    }

    /// <summary>默认路径：以上 gate 都没消耗时执行的释放逻辑（普通戳 / 长穿 / 滑斩）。</summary>
    private void ProcessDefaultPathGate(Vector2 releasePos, bool isSwiped)
    {
        if (CurrentChargeLevel >= 1)
        {
            // 教学期：站桩蓄力（穿刺）不成立，按什么都没发生
            if (!restrictChargedToVertical) ProcessLongPressGesture(releasePos);
            return;
        }
        if (isSwiped)
        {
            // 未蓄力的滑动：与按住期间的未蓄力判定一致（竖滑 → 招架，其余 → 斩击）
            Vector2 swipeDirection = releasePos - touchStartPos;
            ProcessSwipeGesture(swipeDirection, releasePos);
            return;
        }
        ProcessTapGesture(releasePos);
    }

    /// <summary>
    /// 把手势投递给招式状态机；未配置状态机时直接调用 AttackSystem（保持旧行为）。
    /// 返回是否实际执行了招式；成功后按解析出的攻击类型派发 OnAttackExecuted。
    /// </summary>
    /// <summary>
    /// 教学引导用：玩家做对手势后，把这一下原样交给输入系统，让它真的打出去
    /// （否则“教完什么都不发生”，教学没有意义）。
    /// </summary>
    public bool ReplayGesture(MoveGesture gesture, Vector2 screenPos, int chargeLevel = 0, int targetColumn = -1)
    {
        if (attackSystem == null) attackSystem = FindObjectOfType<AttackSystem>();
        if (gesture == MoveGesture.SwipeHorizontal && restrictChargedToVertical) chargeLevel = 0;

        // 有明确目标列就用它（教学引导指向的是具体某个敌人），否则按点击位置换算
        int column = targetColumn >= 0 ? targetColumn : GetStabColumnFromScreenPosition(screenPos);
        bool charged = chargeLevel >= 1;
        var attackType = MoveGestureDefaults.ResolveAttackType(gesture, charged);

        // 引导期间 timeScale=0，动作锁不会走时；重放必须带 cancelCurrentMove 才能立刻出手
        if (moveStateMachine != null) moveStateMachine.NotifyExternalReset();
        bool ok = attackSystem != null && attackSystem.TryExecuteAttack(
            attackType, column, true, 0f, charged, cancelCurrentMove: true, chargeLevel: chargeLevel);

        var ps = PlayerState.Instance;
        Debug.Log("[Tutorial] 重放手势 " + gesture + "->" + attackType + " col=" + column + " charge=" + chargeLevel +
                  " 结果=" + ok + " 阶段=" + (ps != null ? ps.stageState.ToString() : "null"));
        if (ok) OnAttackExecuted?.Invoke(attackType, column);
        return ok;
    }

    private bool SubmitGesture(MoveGesture gesture, int chargeLevel, int targetColumn,
        bool slashLeftToRight = true, float slashVisualTilt = 0f)
    {
        // 教学期没有「站桩蓄力」：按住不放一律不出招（不走连招松手、也不走长按穿刺）
        // combo 链蓄力（Hold 由 C2 松手释放触发）必须放行，否则连段接不上。
        if (gesture == MoveGesture.Hold && restrictChargedToVertical && !comboChargeActive)
            return false;

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
            return SubmitGesture(MoveGesture.SwipeHorizontal, restrictChargedToVertical ? 0 : CurrentChargeLevel, -1);

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
}