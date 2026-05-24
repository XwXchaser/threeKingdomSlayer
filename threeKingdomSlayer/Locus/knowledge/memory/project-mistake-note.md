---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
type: memory
path: project-mistake-note.md
title: project-mistake-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778764012219
updatedAt: 1779628686530
---

# project-mistake-note

## Summary
更新至 2025-07-18 — ISSUE5 Stab wave 旅行方向修复

<!-- locus:body:start -->
### Stab Wave 视觉旅行方向错误 ✅ 已修复（2025-07-18）
- 症状：戳击 wave 视觉上穿过空排飞到错误位置，而非走到目标敌人处。当 row0 有敌人、rows 1-2 为空时，wave 反向飞向 row3 方向
- 根因：`AttackWave.SetupTravel` 中 stab 用 `closestZ` 判断方向，`endTravelZ = closestZ ± 2.5` 的设计假设 wave 在目标前方足够远处生成。但实际 wave 固定生成在 Z=0.5（prefab Z + zOffset），而敌人可能处于负 Z。当 startZ(0.5) > targetZ(-1.0) 时 `closestZ + 2.5 = 1.5`，DOTween 从 0.5→1.5 正方向移动，与目标方向相反
- 修复：stab 改为向 **最远目标**（furthestZ）方向旅行，`endTravelZ = furthestZ`，wave 从 player 侧直走到范围内最远敌人处再收回。当 rangeRows 增大（Buff）时自然走到新范围内最远排。非 stab（Pierce/Sweep）逻辑不变
- 预防规则：Travel 型 wave 的 `startZ`（固定 spawn 点）和 `endTravelZ` 必须确保在空间同一侧，否则 DOTween 移动方向与视觉预期相反
- 文件：`Assets/Scripts/Attack/AttackWave.cs` (SetupTravel)

### CanvasGroup.blocksRaycasts 导致全屏点击拦截 🔁 反复出现（2025-07-18）
- 症状：新建/替换 Canvas 后，游戏内所有交互失效（攻击、按钮等），看起来像输入系统挂了
- 根因：CanvasGroup 组件默认 `blocksRaycasts = true`。即使 Canvas 透明（alpha=0），只要 Canvas 覆盖全屏且 sortingOrder 高于其他 UI，就会吃掉所有点击事件
- 历史：暂停菜单出现过（commit `af1b4e1` — 点击穿透修复），三选一弹窗又出现一次
- 预防规则：**任何带 CanvasGroup 的全屏/覆盖式 Canvas，必须在初始化时同步设置 `blocksRaycasts = false`；显示时设为 `true`，隐藏时立即设为 `false`**。这包括：暂停菜单、升级弹窗、GameOver 面板、任何半屏以上覆盖层
- 检查方法：出问题时在 Inspector 中逐个关闭 Canvas（禁用 GameObject），确认交互恢复后检查该 Canvas 的 CanvasGroup.blocksRaycasts
<!-- locus:body:end -->
