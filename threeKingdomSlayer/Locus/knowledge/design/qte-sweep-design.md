---
id: kd_462cd811-8a63-4ed1-b6e3-0c5e670fe54f
type: design
path: qte-sweep-design.md
title: qte-sweep-design
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1781972967409
updatedAt: 1781972967410
---

# qte-sweep-design

## Summary
QTE Sweep 完整设计文档：蓄力横斩格挡 QTE，动画驱动阶段推进，结果分支动画（Blocked/FollowUp），实施进度 Phase 1-3 完成。

## Content
# QTE Sweep 设计文档

## 设计意图

Sweep 是 BOSS 蓄力横斩攻击，玩家通过划动 QTE 格挡。与 TripleStab（防御型·箭矢波）不同，Sweep 的反馈直接体现在 BOSS 身体动画上。

## 核心原则

1. **动画是主时钟**：QTE 结果无论成败，都等 Happen 动画播完才切结果动画，不提前中断
2. **提早失败只消指示器**：提早划动/方向错误 → 指示器下滑消失，Boss 动画继续不受影响
3. **总时长锁定**：整个 QTE 期间（Start+Happen+Blocked/FollowUp）Boss 处于 QTEAttacking 状态，不会进入其他行为

## 完整动画链路

```
Idle →[QTESweep]→ Start(1.4s) →[ExitTime]→ Happen(1.5s) ─→ Blocked(1.0s,占位) →[ExitTime]→ Idle
                                                          └→ FollowUp(1.0s,占位) →[ExitTime]→ Idle
```

## QTE 时序参数

| 参数 | 值 | 说明 |
|------|-----|------|
| animationLeadTime | 1.42s | Start 动画长度，QTE 阶段在此之前不启动 |
| warningDuration | 0.5s | 填充动画 |
| judgeWindow | 1.0s | 判定窗口 |
| 总判定窗口 | 1.5s | 匹配 Happen 动画 |
| swipeDirection | 180° | 反方向左划 = 格挡横斩 |
| cooldownAfterQTE | 3s | |
| interruptibleOnStun | false | 蓄力攻击不可打断 |

## 玩家操作 × 结果矩阵

| 时机 | 操作 | QTE 结果 | 指示器 | Boss 动画 |
|------|------|---------|--------|----------|
| Start 期间 | — | 不可操作 | 未出现 | Start 继续 |
| warning 填充(0.5s) | 划动 | 失败（提早） | 立即下滑消失 | Happen 继续，播完 → FollowUp |
| judge 窗口(1.0s) | 正确方向划动 | 成功 | 成功特效→下滑 | Happen 继续，播完 → Blocked |
| judge 窗口 | 方向错误 | 失败 | 立即下滑消失 | Happen 继续，播完 → FollowUp |
| judge 窗口 | 不操作 | 失败（超时） | 失败特效→下滑 | Happen 继续，播完 → FollowUp |

## 实施进度

- [x] Phase 1: 4 个 .anim 文件 (Start 1.4s, Happen 1.5s, Blocked 1.0s占位, FollowUp 1.0s占位)
- [x] Phase 2: Animator State Machine (3 Trigger + 4 State + 转换 + Abort兜底)
- [x] Phase 3: QTEAttackConfig 扩展 (animationBlockedClip/animationFollowUpClip/UseBranchedAnimation) + QTEAttackConfig_Sweep + QTEConfig_Sweep
- [ ] Phase 4: QTEController 改造（核心逻辑）
- [ ] Phase 5: BossQTEData 注册

## Phase 4 关键改动点

1. StartQTEAnimation: 增加 QTESweep 分支 (UseBranchedAnimation → SetTrigger("QTESweep"))
2. ResolveQTE: sweep 模式只记录结果，不推进状态机
3. UpdateJudging/UpdatePerforming: sweep 模式用固定动画时长驱动阶段推进（_qtePhaseTimer >= 1.5s 进 QTEEnding）
4. StartQTEEndingPhase: 参数化 playerBlocked，分支发 QTEBlocked/QTEFollowUp
5. StopQTEAnimation: 保留给 AbortQTE（发 QTEEnd trigger，Animator 通过兜底路径回 Idle）
6. TryQTESwipe 提早失败: 只消 indicator，不中断动画
