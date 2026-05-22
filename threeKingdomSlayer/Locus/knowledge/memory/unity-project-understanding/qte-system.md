---
id: kd_6dc15d1a-77b7-4a98-a7b2-4bdb38d7d679
type: memory
path: unity-project-understanding/qte-system.md
title: qte-system
inheritInjectMode: true
summaryEnabled: false
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779287256500
updatedAt: 1779287256502
---

# qte-system

<!-- locus:body:start -->
# QTE System

## Overview
QTE 系统由 `QTEController`（挂载在 Boss prefab 上）、`QTEDisplay`（挂载在 Canvas 上）、`QTEConfig`/`QTEAttackConfig`/`BossQTEData` ScriptableObject 组成。

## Key Components
- **QTEController**: 状态机 Idle→CoolingDown→WaitingForAttackFinish→PerformingQTEAttack→QTEJudging→QTECompleted
- **QTEDisplay**: 管理 QTE 指示器 prefab 的生成/销毁/动画
- **InputManager.TryConsumeQTEInput**: 对手势进行 QTE 判定

## Critical Configuration
- Canvas 是 `ScreenSpaceCamera` 模式，render camera 必须正确设置
- `RectTransformUtility.RectangleContainsScreenPoint` 和 `GetWorldCorners` 需要传入 camera 参数才能在 ScreenSpaceCamera 模式下正确工作
- QTE 指示器 prefab 位于 `Assets/Prefabs/QTE/`，config 位于 `Assets/ScriptableObjects/QTE/`

## Known Fix: QTE 无法交互 (2024)
- **根因**: Canvas 为 ScreenSpaceCamera 模式，`IsClickInQTEArea` 调用 `RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos)` 未传 camera，始终返回 false
- **修复**: 新增 `GetQTECanvasCamera()` 获取 Canvas.worldCamera，传入 `RectangleContainsScreenPoint` 和 `GetWorldCorners`→`WorldToScreenPoint`

## Known Fix: Parry/Swipe 无法命中 BOSS (2024)
- **根因**: `ColumnManager.GetEnemiesInRange(rangeRows)` 仅按 rowIndex < rangeRows 过滤，BOSS 位于后排时被排除
- **修复**: 新增条件 `|| (e.isBoss && e.bossState == BossState.InCombat)` 始终包含已应战的 BOSS
<!-- locus:body:end -->
