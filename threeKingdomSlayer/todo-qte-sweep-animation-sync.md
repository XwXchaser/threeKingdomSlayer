# QTE Sweep 动画与指示器同步优化 — 已完成

## 变更摘要

### 动画 Clip 合并
将分散的 QTE Sweep 动画帧合并为按类型分组的 clip：
| Clip | 帧 | 时长 | 用途 |
|------|-----|------|------|
| `Boss_104_QTE_Sweep_Start` | start1-7 | 1.42s | 前摇 |
| `Boss_104_QTE_Sweep_Happen` | happen1-3 | 0.52s | QTE 判定窗口 |
| `Boss_104_QTE_Sweep_Hit` | hit1-4 | 0.62s | 格挡失败 |
| `Boss_104_QTE_Sweep_Blocked` | blocked1-4 | 0.52s | 格挡成功 |
| `Boss_104_QTE_Sweep_End` | end1-2 | 0.60s | 收尾 |

### Animator Controller 简化
- 移除分散的 Hit1-4、Blocked1-4 状态链
- 移除 `QTEFollowUp` 参数
- `Start → Happen → Blocked → End`（成功，QTEBlocked trigger，ExitTime=0 立即切换）
- `Start → Happen → Hit → End`（失败，ExitTime=1 自动过渡）

### 代码变更
- `QTEAttackConfig.cs`：`UseBranchedAnimation` 不再要求 FollowUp；新增 `EffectiveLeadTime` 自动从 Start clip 推导
- `QTEController.cs`：移除 `QTEFollowUp` trigger；使用 `EffectiveLeadTime`

### Animator 速度调制（核心同步机制）
Happen 动画（~0.50s）短于 QTE judgeWindow（~2.80s），通过 Animator.speed 实现同步：
- **慢放**：进入判定阶段首个帧，`_judgingSpeedApplied` 标记 + `animator.speed = happenClipLength / judgeDuration`，将 Happen 拉伸至覆盖整个 QTE 窗口
- **提前结束**：所有 slot resolved（玩家成功）→ 不等 judgeDuration 到期，立即调用 `StartQTEEndingPhase()`
- **加速收尾**：成功时 `animator.speed = 3f`，0.12s 后触发 `QTEBlocked` trigger 并重置 speed=1，播 Blocked 动画
- **失败**：`animator.speed = 1f` 后立即发 `QTEHit` trigger

### 时间自动推导
- `animationLeadTime` 自动从 `animationStartClip.length` 获取（当前 1.42s）
- QTE 窗口期 = `effectiveJudgeDuration`（来自 QTEConfig_Sweep，当前 2.80s = warningDuration 0.80 + judgeWindow 2.00）
- Happen 动画通过 speed 调制匹配窗口，调整 judgeWindow 即可改变判定时长

## 状态
- [x] 动画帧合并（happen/hit/blocked）
- [x] Block 触发立即切换动画（ExitTime=0）
- [x] 移除 FollowUp 假动画
- [x] 清理旧分散 .anim 文件
- [x] 配置和代码同步
- [x] Happen 慢放覆盖 QTE 窗口（_judgingSpeedApplied）
- [x] 全部 slot resolved 提前进入结束阶段
- [x] 成功时 3x 加速收尾再切 Blocked

## 涉及文件
- `Assets/Animations/Boss_104_QTE_Sweep_Happen.anim`
- `Assets/Animations/Boss_104_QTE_Sweep_Hit.anim`
- `Assets/Animations/Boss_104_QTE_Sweep_Blocked.anim`
- `Assets/Animations/Boss_104.controller`
- `Assets/Scripts/QTE/QTEAttackConfig.cs`
- `Assets/Scripts/QTE/QTEController.cs`
- `Assets/ScriptableObjects/QTE/QTEAttackConfig_Sweep.asset`
