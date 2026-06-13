---
id: kd_88224985-75b2-4921-aff3-d5e2a3b1bd6f
type: design
path: three-choice-reward-system.md
title: three-choice-reward-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779547549506
updatedAt: 1781349044076
---

# three-choice-reward-system

## Summary
三选一奖励系统的完整技术设计文档。涵盖数值buff型、道具型（大旋风+落雷）、被动攻击型三种类型的分类、数据流、UI布局、手势冲突矩阵、当前实现状态、待修复问题、配置资产清单和关键设计决策。贯穿整个游戏开发周期。

## Content
### 4.5 UI 显示

- 图标 + 角标显示触发信息（计时类显示间隔秒数，计数类显示阈值）
- **计时被动冷却显示**：图标上叠加 Radial360 顺时针冷却填充 + 右上角倒计时数字
  - 由 `BuffDisplayPanel.Update()` 每帧驱动，读取 `TimedPassiveModule` 公开 API
  - **fillAmount 约定**（与 HeroHUD 普通攻击冷却一致）：`0 = 冷却中，1 = 就绪`，公式 `fillAmount = 1 - (timer / interval)`
  - **FillClockwise = true**（顺时针），**FillOrigin = Bottom(2)**，Image Type = Filled, FillMethod = Radial360
  - `BuffIcon.CooldownFill` 对应此填充层，`CooldownDim` 为灰色蒙层（仅 visible 切换）
