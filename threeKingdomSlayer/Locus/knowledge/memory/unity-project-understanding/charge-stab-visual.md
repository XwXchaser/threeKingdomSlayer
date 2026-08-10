---
id: kd_19d3b3c2-00e3-4913-911d-45d93cffa451
type: memory
path: unity-project-understanding/charge-stab-visual.md
title: charge-stab-visual
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782990177886
updatedAt: 1786266789255
---

# charge-stab-visual

## Summary
蓄力 Stab 前端可视化 + Launch 衔接：指尖平面跟随/左右clamp+Zroll/上下Xpitch，Launch接管pose+三帧精灵序列charge2→charge1→stab

<!-- locus:body:start -->
# Charge Stab Visual System

## Overview
蓄力时在世界空间显示 Stab 武器精灵。视觉中心跟随指尖投射到玩家附近的相机平行平面；X 轴位置仍以玩家为中心按 `halfWidth` clamp；左右移动驱动 Z 轴 roll；上下移动只在视觉出现后按相对出现点驱动轻微 X 轴 pitch（向上与向下可分别配置上限）。纯前端表现，不触发任何攻击逻辑。

Launch 释放时接管蓄力视觉的实时 pose（位置/旋转/缩放），从该姿态直接播放上挑动画，播放 stab_charge2→stab_charge1→stab 三帧收束序列。

## Key File
- `Assets/Scripts/Effects/ChargeStabVisual.cs` — MonoBehaviour，挂载在 `Player` GameObject
- `Assets/Scripts/Player/AttackSystem.cs` — Launch 视觉释放时读取并接管蓄力视觉 pose

## 行为流程
```
按下 → 0~30% 隐藏
     → 30%: Stab.prefab 实例化，世界空间出现
       - 出现瞬间记录 pitch baseline（指尖在跟随平面上的位置）
       - 位置: 屏幕指尖 raycast 到相机 forward 法线平面，视觉中心跟随该点
       - X: clamp 到 playerX ± halfWidth，保留原左右范围限制
       - Y/Z: 不再锁定为 playerY/playerZ 固定偏移，而由指尖平面命中点决定
       - Z 旋转: 手指左右偏移映射到 ±maxAngle
       - X 俯仰: 只按出现后上下相对位移计算，向下为正、向上为负；向下最大 maxDownPitchAngle，向上最大 maxPitchAngle
       - 精灵: charge1 (30%~80%映射) → charge2 (80%~100%映射)
     → 100%: ready 精灵停留 readyDuration 秒
     → 100%+: loop1 ↔ loop2 每 loopInterval 秒交替
松手 → 非 Launch 时 fadeOutDuration 秒渐隐 → Destroy
Launch 释放 → AttackSystem 读取当前蓄力视觉位置/旋转/缩放 → SuppressFadeAndDestroy 立即销毁蓄力视觉
            → Launch_Visual 从该 pose 播放三帧序列(stab_charge2→stab_charge1→stab) + 旋转上挑
```

## ChargeStabVisual API
| 方法 | 说明 |
|------|------|
| `TryGetCurrentVisualPose(out pos, out rot, out scale)` | 读取当前蓄力视觉实例的世界位置/旋转/缩放，无实例返回 false |
| `SuppressFadeAndDestroy()` | 跳过渐隐直接销毁蓄力视觉（Launch 接管用） |

## Inspector 可调参数
| 参数 | 说明 |
|------|------|
| stabPrefab | Stab.prefab，视觉基底 prefab |
| chargeSprite1/2 | 蓄力中精灵 |
| readySprite | 蓄满精灵 |
| loopSprite1/2 | 满蓄循环精灵 |
| spawnYOffset | 跟随平面锚点沿相机 up 的偏移 |
| spawnZOffset | 跟随平面锚点沿世界 Z 的偏移 |
| halfWidth | X 轴移动半宽 |
| maxAngle | 左右移动产生的 Z 轴 roll 最大角度 |
| maxPitchAngle | 向上移动产生的 X 轴 pitch 最大角度，出现瞬间为 0 |
| maxDownPitchAngle | 向下移动产生的 X 轴 pitch 最大角度，出现瞬间为 0 |
| verticalTiltHalfHeight | 上下移动多少世界单位达到最大 pitch |
| visualScale | 视觉缩放 |
| appearThreshold | 出现阈值（进度比例） |
| readyDuration | ready 精灵停留时长 |
| loopInterval | loop 交替间隔 |
| fadeOutDuration | 渐隐时长 |

## AttackSystem Launch 字段
| 字段 | 说明 |
|------|------|
| _launchSprite1 | Launch 帧1：stab_charge2 |
| _launchSprite2 | Launch 帧2：stab_charge1 |
| _launchSprite3 | Launch 帧3：stab |

## 关键同步参数
- `longPressDuration = 0.3s` (Battle.scene InputManager)
- `minChargeTime = 1s` (InputManager / Battle.scene)
- 蓄力有效窗口：`longPressDuration` → `minChargeTime`，归一化进度 = `InverseLerp(longPressDuration/minChargeTime, 1, rawProgress)`
- `appearThreshold = 0.3` → 指示器在 rawProgress 达 0.3 时出现；`ChargeStabVisual` 在 rawProgress 达 `longPressDuration/minChargeTime` 时出现
- `maxPitchAngle` 默认 10°（向上），`maxDownPitchAngle` 默认 20°（向下），`verticalTiltHalfHeight` 默认 2 世界单位
- Launch 三帧均分 `launchFlickDuration`（默认 0.20s），每帧约 0.067s

## 蓄力进度归一化 (2024 fix)
- `ChargeIndicatorController.GetChargeBeginProgress()`: 返回 `longPressDuration / minChargeTime`，fillAmount = `InverseLerp(beginProgress, 1, rawProgress)`
- `ChargeStabVisual.GetChargeBeginProgress()`: 同上，OnChargeUpdated 将 rawProgress 归一化后再传 UpdateSprite；UpdateSprite 内不再做 appearThreshold 二次映射
- **ChargeIndicatorController parentCanvas 修复**: `Start()` 使用 `transform.parent?.GetComponentInParent<Canvas>()` 跳过自身 Canvas 获取父级 BattleHUD Canvas，确保 UpdatePosition 坐标转换正确

## 精灵资源
- `Assets/Sprites/zhangfei/stab_charge1.png` ~ `stab_charge_loop2.png` (5 张, 512x512)
- `Assets/Sprites/zhangfei/stab.png` — Launch 第3帧

## 注意
- `Stab.prefab` 基础旋转为 `(90,0,0)`，代码只叠加运行时 pitch/roll，不改 prefab pivot。
- 遵守项目 2.5D 深度规则：不要为该视觉使用高 sortingOrder 覆盖 Z 深度。
- Launch 无蓄力视觉时保留原固定玩家 offset + scale-in fallback。
- Launch 精灵序列：`stab_charge2` → `stab_charge1` → `stab`（三帧均分 duration），替换了旧的 `stab_rotate1` → `stab_rotate2`。AttackSystem 新增 `_launchSprite1/2/3` 字段，`_stabRotate1Sprite`/`_stabRotate2Sprite` 仍保留给 Slash 的 SweepEffect。
<!-- locus:body:end -->
