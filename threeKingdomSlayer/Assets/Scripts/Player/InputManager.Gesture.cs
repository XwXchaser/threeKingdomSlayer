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
        // 持枪姿态进行中（招架 / 防御）：不识别滑动
        // （滑动出招必须先松手退出防御，姿态自己回落）
        if (StanceActive) return false;

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
            && verticalDrift <= chargeMovementTolerance
            && IsChargeAllowed())   // 没有前置招式 → 不进入蓄力态（也就没有蓄力表现）
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

        // 持枪姿态进行中（招架 / 防御）：抬手这一下的手势被吞掉
        // （姿态自己回落；防御中松手 → 回落 → 走完则收枪）
        if (StanceActive) return;

        // 「按下即出」：抬手时若还有待兑现的轻攻击 → 立刻兑现
        // （否则会等到 pressTapDelay 才出，或被下面的 hold 闸门吃掉）
        if (_pressTapPending) CommitPressTap();

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
        // 站桩蓄力（长按不滑 → 穿刺）：同样要求「有前置招式」。
        // 不成立时按普通处理往下走（滑动/点按），不吞输入。
        if (CurrentChargeLevel >= 1 && IsChargeAllowed())
        {
            if (!restrictChargedToVertical) { ProcessLongPressGesture(releasePos); return; }
        }
        // 「按下即出」已经打过一记轻攻击 → 抬手不再补一记点按（否则一次点击变成两段）
        if (_pressTapFired)
        {
            _pressTapFired = false;
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

    /// <summary>
    /// 调试 / 跑关机器人用：直接投递一次手势，**不打断当前连招**。
    /// 走的是与玩家输入完全相同的路径（SubmitGesture → 招式状态机），所以连招会正常接段。
    /// </summary>
    public bool SubmitGestureForDebug(MoveGesture gesture, int chargeLevel, int targetColumn,
        Vector2 screenPos, bool slashLeftToRight = true, float slashVisualTilt = 0f)
    {
        // 调试注入也要**守教学闸门**（真实输入会守）→ 否则机器人会在教学拍里乱出招：
        // 2026-11-04 实测：机器人在「连招蓄力」拍的阶段 1 连点三下 → 点戳连段 Jab1→Jab2→Jab3
        // → 「指向突刺」的连段边（只挂在 Jab1 上）就永远用不上了 → 这一拍收不了 → 整条链路卡死。
        if (!TutorialAllowsDebugGesture(gesture, chargeLevel)) return false;
        return SubmitGesture(gesture, chargeLevel, targetColumn, slashLeftToRight, slashVisualTilt, screenPos);
    }

    /// <summary>调试注入版的教学闸门判定（与真实释放路径同一套规则 ✅）。</summary>
    private bool TutorialAllowsDebugGesture(MoveGesture gesture, int chargeLevel)
    {
        if (tutorialGestureTarget == TutorialGestureTarget.None) return true;
        if (gesture == MoveGesture.SwipeHorizontal || gesture == MoveGesture.SwipeVertical)
            return TutorialAllowsSwipeGesture(gesture, chargeLevel);
        // 点按 / 按住：走点按那一道（ComboHold 只放行「蓄满后的按住松手」）
        return TutorialAllowsTapGesture(chargeLevel);
    }

    private bool SubmitGesture(MoveGesture gesture, int chargeLevel, int targetColumn,
        bool slashLeftToRight = true, float slashVisualTilt = 0f,
        Vector2 screenPos = default, ISupplyPropTarget supplyTarget = null)
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
            timestamp = Time.time,
            screenPos = screenPos,
            supplyTarget = supplyTarget
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
    /// 点中补给物件时：这一下同时命中该物件（点中本体，与所在列无关）。
    /// </summary>
    private void ProcessTapGesture(Vector2 position)
    {
        int column = GetStabColumnFromScreenPosition(position);
        if (column < 0) return;

        ISupplyPropTarget supplyTarget = null;
        if (BreakableProp.TryHitAtScreenPoint(position, out var prop)) supplyTarget = prop;

        bool executed = SubmitGesture(MoveGesture.Tap, CurrentChargeLevel, column, screenPos: position, supplyTarget: supplyTarget);

        // 局外（主菜单 / 落地页）没有战斗状态，招式不会执行 → 物件的交互直接生效。
        // 局内若招式被冷却或状态挡住，则这一下不生效（保持“照常出招、吃冷却”的语义）。
        if (!executed && supplyTarget != null && !IsInBattleState())
            supplyTarget.ApplySupplyHit(AttackType.Stab, 1f);
    }

    /// <summary>当前是否处于战斗进行中（局内才有招式结算）。</summary>
    private static bool IsInBattleState()
    {
        var ps = PlayerState.Instance;
        return ps != null && ps.stageState == StageState.InProgress;
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

        // 滑向的蓄力等级：**只有「有前置招式」（连段窗口内）时才带**。详见 ChargeForSwipe()。
        int swipeCharge = ChargeForSwipe();

        // 近垂直划动（上/下）→ 挑飞
        // 方向与垂直轴夹角 < verticalSwipeThreshold
        if (angleToVertical < verticalSwipeThreshold)
            return SubmitGesture(MoveGesture.SwipeVertical, swipeCharge, -1);

        // 近水平划动（左/右）→ 横扫
        // 方向与水平轴夹角 < horizontalSwipeThreshold
        if (angleToHorizontal < horizontalSwipeThreshold)
            return SubmitGesture(MoveGesture.SwipeHorizontal, swipeCharge, -1);

        // 对角线划动 → 斩击（兜底）
        bool slashLeftToRight = direction.x > 0;
        float slashVisualTilt = GetSlashVisualTilt(direction);
        Debug.Log($"[SlashTilt] Input charged dir={direction} leftToRight={slashLeftToRight} tilt={slashVisualTilt:F2}");
        return SubmitGesture(MoveGesture.SwipeDiagonal, swipeCharge, -1, slashLeftToRight, slashVisualTilt);
    }

    /// <summary>
    /// 滑向手势能带的蓄力等级 —— **只有「有前置招式」时才带**（2026-11-05）。
    ///
    /// 问题：「站桩蓄力 + 横滑/斜滑/竖滑」= **无前置的纯蓄力** —— 中立态就能打出蓄力招
    /// （蓄力横扫 Sweep / 蓄力横斩 Slash / 蓄力上挑 Launch），
    /// 绕过了「终结技身份由前置决定」（design/combo-move-state-machine.md:201）。
    ///
    /// 判据：<c>comboChargeActive</c> —— 招式状态机在「招式进行中再次按下」时置位，
    /// 即**这次按下属于连段（有前置招式）**；站桩蓄力是「直通」，不算连段（见状态机里的注释）。
    ///
    /// 影响面：
    ///   • 连段里的蓄力**不受影响**：指向突刺（Jab1 --Hold--> StabR2）、
    ///     SlashC2（Slash --Hold--> SlashC2）、枪突三下 → 蓄力上挑终结技（Jab3 --竖滑--> LaunchFinisher）
    ///     —— 这几个的蓄力都发生在连段窗口内 → comboChargeActive 为真 → 蓄力照常带。
    ///   • 站桩蓄力（长按不滑 → Pierce）**不受影响**（走的不是滑向路径）。
    ///   • 中立态的蓄力上挑（Launch）会失去入口 → 变成无入口内容（待设计定）。
    /// </summary>
    private int ChargeForSwipe()
    {
        if (restrictChargedToVertical) return 0;
        if (IsChargeAllowed()) return CurrentChargeLevel;
        if (CurrentChargeLevel >= 1)
        {
            DebugLog.Info("[InputManager] 滑向蓄力不成立：没有前置招式（comboChargeActive=false）→ 按未蓄力处理");
        }
        return 0;
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