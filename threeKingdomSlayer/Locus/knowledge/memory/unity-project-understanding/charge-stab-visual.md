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
updatedAt: 1782990177888
---

# charge-stab-visual

## Summary
蓄力 Stab 前端可视化：世界空间武器精灵，跟随手指 X 移动 + Z 旋转，charge1→charge2→ready→loop 精灵状态机

<!-- locus:body:start -->
# Charge Stab Visual System

## Overview
蓄力时在世界空间显示 Stab 武器精灵，X 轴跟随手指左右移动，Z 轴旋转模拟 Slash 朝向。纯前端表现，不触发任何攻击逻辑。

## Key File
- `Assets/Scripts/Effects/ChargeStabVisual.cs` — MonoBehaviour，挂载在 `Player` GameObject

## 行为流程
```
按下 → 0~30% 隐藏
     → 30%: Stab.prefab 实例化，世界空间出现
       - X: 屏幕坐标→世界坐标，clamp ±halfWidth
       - Y/Z: playerY + spawnYOffset, playerZ + spawnZOffset
       - Z 旋转: 手指左右偏移映射到 ±maxAngle（方向取反，与手指同向）
       - 精灵: charge1 (30%~80%映射) → charge2 (80%~100%映射)
     → 100%: ready 精灵停留 readyDuration 秒
     → 100%+: loop1 ↔ loop2 每 loopInterval 秒交替
松手 → fadeOutDuration 秒渐隐 → Destroy
```

## Inspector 可调参数 (默认值)
| 参数 | 默认值 | 说明 |
|------|--------|------|
| stabPrefab | Stab.prefab | 视觉基底 prefab |
| chargeSprite1/2 | stab_charge1/2.png | 蓄力中精灵 |
| readySprite | stab_charge_ready.png | 蓄满精灵 |
| loopSprite1/2 | stab_charge_loop1/2.png | 满蓄循环精灵 |
| spawnYOffset | 0 | Y 偏移（同 Slash） |
| spawnZOffset | -3.6 | Z 偏移（同 Slash） |
| halfWidth | 3 | X 轴移动半宽 |
| maxAngle | 60 | Z 旋转最大角度 |
| visualScale | (0.1, 0.1, 0.1) | 缩放 |
| appearThreshold | 0.3 | 出现阈值（进度比例） |
| readyDuration | 0.2 | ready 精灵停留时长 |
| loopInterval | 0.3 | loop 交替间隔 |
| fadeOutDuration | 0.25 | 渐隐时长 |

## 关键同步参数
- `minChargeTime = 1s` (InputManager)
- `appearThreshold = 0.3` → 视觉在 0.3s 出现
- `loopInterval = 0.3s` → 与 ThornArmorEffect 统一

## 精灵资源
- `Assets/Sprites/zhangfei/stab_charge1.png` ~ `stab_charge_loop2.png` (5 张, 512x512)
<!-- locus:body:end -->
