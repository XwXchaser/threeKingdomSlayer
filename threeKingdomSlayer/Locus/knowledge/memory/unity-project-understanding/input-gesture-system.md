---
id: kd_e17bd432-53a4-4edb-ad26-f41462946f3c
type: memory
path: unity-project-understanding/input-gesture-system.md
title: input-gesture-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1786179760946
updatedAt: 1786179760946
---

# input-gesture-system

## Summary
输入手势系统：按住划动即触发招式，停留蓄力。速度门控追踪核心机制。

<!-- locus:body:start -->
# Input Gesture System

## Overview
按住+划动即可触发招式（无需松手），停留则蓄力。Stab/Pierce 仅松手触发。

## 交互矩阵

| 操作 | 条件 | 招式 |
|------|------|------|
| 按住+快速划动（未蓄力） | instantSpeed≥180, dist≥30px, dur≤0.25s | Slash/Parry |
| 按住+快速划动（已蓄力） | 同上 + isLongPress + isCharged | Slash/Sweep/Launch |
| 停留 | dist≤20px, dur≥0.3s → 开始蓄力 | Charge |
| 松手（未划动） | dist<30px | Stab |
| 松手（已蓄力） | isLongPress + isCharged | Pierce |

## 速度门控追踪 (Speed-Gated Tracking)
核心方法：`TryDetectHoldSwipe(Vector2 currentPos)` — 鼠标和触摸共用。

1. 每帧计算瞬时速度：`frameDelta / frameTime`
2. 瞬时速度 ≥ `minSwipeSpeed`(180) 且过了 `swipeRearmDelay`(0.1s) → 开始追踪
3. 从追踪起点累积距离，若 `trackDist ≥ swipeThreshold`(30) 且 `trackDur ≤ maxSwipeDuration`(0.25s) → 触发招式
4. 超时或速度骤降 → 放弃追踪，可重新触发

**关键设计决策**：不使用 `segmentStartTime`（初次按下时间）来计算 swipe 耗时，因为用户可能先停留再划动，此时 `segmentDuration` 早已超过 `maxSwipeDuration`。改为仅在速度达标后才开始独立计时。

## 关键字段
| 字段 | 说明 |
|------|------|
| `segmentStartPos/Time` | 当前分段起点（每次招式后重置），用于蓄力判定 |
| `isSwipeTracking` | 是否正在追踪一次快速划动 |
| `swipeTrackStartPos/Time` | 速度达标后开始追踪的起点 |
| `lastFramePos/Time` | 上一帧位置/时间，用于计算瞬时速度 |
| `lastGestureTime` | 上次招式触发时间，用于 rearm delay |
| `isLongPress` | segmentDuration≥longPressDuration 且未超移动容忍 |
| `isCharged` | segmentDuration≥minChargeTime |

## 可调参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `longPressDuration` | 0.3s | 长按判定时间 |
| `minChargeTime` | 0.5s | 最小蓄力时间 |
| `swipeThreshold` | 30px | 滑动判定最小距离 |
| `maxSwipeDuration` | 0.25s | 有效滑动最大耗时 |
| `minSwipeSpeed` | 180px/s | 有效滑动最低瞬时速度 |
| `swipeRearmDelay` | 0.1s | 招式后重新识别间隔 |
| `chargeMovementTolerance` | 20px | 蓄力允许的轻微移动 |
| `verticalSwipeThreshold` | 30° | 垂直判定角 |
| `horizontalSwipeThreshold` | 30° | 水平判定角 |

## 关键文件
- `Assets/Scripts/Player/InputManager.cs` — 主输入管理器
- `Assets/Scripts/Player/AttackSystem.cs` — `TryExecuteAttack` 执行攻击

## 注意事项
- 鼠标和触摸走同一 `TryDetectHoldSwipe` 方法，修改追踪逻辑时两路自动同步
- `ResetSegment` 必须重置 `isSwipeTracking`，否则残留的追踪状态会在下一分段立即超时
- `TouchPhase.Began` 必须初始化 `isSwipeTracking`、`lastFramePos`、`lastFrameTime`
<!-- locus:body:end -->
