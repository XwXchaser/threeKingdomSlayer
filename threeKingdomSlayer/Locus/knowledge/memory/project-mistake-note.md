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
updatedAt: 1780456545655
---

# project-mistake-note

## Summary
更新至 2025-08-10 — 新增 Victory(panel) Canvas层级遮挡 + 测试硬编码 triggerParam=1 未回退修复

<!-- locus:body:start -->
### Victory(panel) 被三选一弹窗遮挡 ✅ 已修复（2025-08-10）
- 症状：通关后"胜利"大字被左侧三选一 UpgradePopup 遮挡，不可见
- 根因：`UpgradePopup.prefab` 自带 Canvas sortOrder=100，而 `BattleHUD(Canvas)` sortOrder=0。三选一弹出后将 Victory(panel) 完全盖住
- 修复：给 `Battle.scene` 中 `BattleHUD(Canvas)/Victory(panel)` 添加 Canvas 组件，设置 `overrideSorting=true, sortingOrder=200`，并添加 GraphicRaycaster
- 预防规则：**多层 UI 的层级关系不能仅靠 Hierarchy 顺序**。当子面板使用独立 Canvas 时，sortOrder 必须显式大于所有可能弹出的面板。默认 Canvas sortOrder=0，任何带 sortOrder>0 的弹出面板都会遮挡
- 文件：`Assets/Scenes/Battle.scene`（Victory(panel) 新增 Canvas + GraphicRaycaster）

### 测试硬编码 triggerParam=1 未回退 ✅ 已修复（2025-08-10）
- 症状：ReturnWave（配置 intValue=4）和 ChainBounce（配置 intValue=6）每次攻击都触发，无视配表间隔
- 根因：`PassiveTriggerModule.Register()` 中硬编码 `triggerParam = 1`，忘记在测试完成后还原为 `def.intValue`
- 修复：新增 `[SerializeField] private bool _forceTriggerEveryAttack` 测试开关（默认 false），`triggerParam = _forceTriggerEveryAttack ? 1 : def.intValue`。需要测试时在 Inspector 勾选即可，不会忘记还原
- 预防规则：**测试硬编码值应改为可开关的 Inspector 选项**，而非注释/取消注释。注释容易被遗忘，导致下次提交时带上去
- 文件：`Assets/Scripts/Core/PassiveTriggerModule.cs`（Register 方法 + _forceTriggerEveryAttack 字段）
<!-- locus:body:end -->
