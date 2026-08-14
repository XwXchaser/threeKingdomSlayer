---
id: kd_462cd811-8a63-4ed1-b6e3-0c5e670fe54f
injectMode: inherit
summary: QTE Sweep 完整设计文档：蓄力横斩格挡 QTE，Animator speed 调制同步动画与判定窗口，成功加速收尾切 Blocked，已全部实施完成。
aiMaintained: inherit
---

# QTE Sweep 设计文档

## 设计意图

Sweep 是 BOSS 蓄力横斩攻击，玩家通过划动 QTE 格挡。结果反馈直接体现在 BOSS 身体动画上。

## 核心原则

1. **动画通过 speed 调制匹配 QTE 窗口**：Happen clip（0.50s）通过 Animator.speed 慢放覆盖 judge 窗口（~2.80s），调整 clip 帧数或 judgeWindow 即可同步
2. **玩家成功立即加速收尾**：所有 slot resolved 后不等 judgeDuration 到期，立即 3x 加速播完剩余 Happen 帧再切 Blocked，减少等待感
3. **提早失败只消指示器**：提早划动/方向错误 → 指示器下滑消失，Boss 动画继续不受影响
4. **总时长锁定**：整个 QTE 期间 Boss 处于 QTEAttacking 状态，不会进入其他行为

## 完整动画链路

```
Idle →[QTESweep]→ Start(1.42s) →[ExitTime]→ Happen(0.52s, speed调制) ─→ Blocked(0.52s) →[ExitTime]→ End(0.60s) → Idle
                                                                          └→ Hit(0.62s) →[ExitTime]→ End(0.60s) → Idle
```

- Happen 进入判定阶段首帧设置 `animator.speed = happenLength / judgeDuration`，拉伸覆盖整个窗口
- 玩家成功 → 全部 slot resolved → 3x 加速 → QTEBlocked trigger → Blocked
- 玩家失败/超时 → QTEHit trigger → Hit

## QTE 时序参数

| 参数 | 值 | 说明 |
|------|-----|------|
| animationLeadTime | 1.42s | Start 动画长度（自动从 clip 推导） |
| warningDuration | 0.80s | QTE 指示器预警阶段 |
| judgeWindow | 2.00s | 判定窗口（QTEConfig_Sweep） |
| 有效判定总长 | ~2.80s | warningDuration + judgeWindow |
| Happen clip 长度 | 0.52s | 原始动画，通过 speed 调制匹配窗口 |
| swipeDirection | 180° | 反方向左划 = 格挡横斩 |
| cooldownAfterQTE | 3s | |
| interruptibleOnStun | false | 蓄力攻击不可打断 |

## 玩家操作 × 结果矩阵

| 时机 | 操作 | QTE 结果 | 指示器 | Boss 动画 |
|------|------|---------|--------|----------|
| Start 期间 | — | 不可操作 | 未出现 | Start 继续 |
| warning 填充 | 划动 | 失败（提早） | 立即下滑消失 | Happen 继续，播完后切 Hit |
| judge 窗口 | 正确方向划动 | 成功 | 成功特效→下滑 | Happen 3x 加速收尾 → Blocked |
| judge 窗口 | 方向错误 | 失败 | 立即下滑消失 | Happen 继续，播完后切 Hit |
| judge 窗口 | 不操作 | 失败（超时） | 失败特效→下滑 | Happen 继续，播完后切 Hit |

## 实施进度

- [x] Phase 1: 5 个 .anim 文件 (Start/Happen/Blocked/Hit/End)
- [x] Phase 2: Animator State Machine (3 Trigger + 5 State + 转换 + 兜底)
- [x] Phase 3: QTEAttackConfig 扩展 + QTEAttackConfig_Sweep + QTEConfig_Sweep
- [x] Phase 4: QTEController 改造（_judgingSpeedApplied 慢放 + 提前结束 + 加速收尾）
- [x] Phase 5: BossQTEData 注册

## Phase 4 关键实现

1. `TriggerQTEAttack()`：UseBranchedAnimation 分支 → SetTrigger("QTESweep")，重置 `_judgingSpeedApplied = false`
2. `UpdateJudging()`：首个判定帧设置 `animator.speed = happenLength / judgeDuration`；全部 slot resolved → 立即 `StartQTEEndingPhase()`
3. `StartQTEEndingPhase()`：playerBlocked → speed=3f, 0.12s 后 TriggerBlockedAfterAcceleration；失败 → speed=1f, SetTrigger("QTEHit")
4. `StopQTEAnimation()`：重置 `animator.speed = 1f`，发 QTEEnd trigger 回 Idle
5. AbortQTE：StopQTEAnimation + 战斗恢复
