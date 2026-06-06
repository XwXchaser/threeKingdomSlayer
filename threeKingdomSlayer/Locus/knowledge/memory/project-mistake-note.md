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
updatedAt: 1780762747373
---

# project-mistake-note

## Summary
更新至 2025-08-10 — 新增 BuffIcon raycastTarget=False 导致 UI 点击穿透

<!-- locus:body:start -->
### BuffIcon raycastTarget=False 导致 UI 点击穿透 ✅ 已修复（2025-08-10）
- 症状：点击血包 BuffIcon 时 `overUI=False`，InputManager 将点击降级为游戏 stab 攻击，血包无法使用
- 根因：BuffIcon 的 `Icon` / `Frame` 子级 Image 的 `raycastTarget` 在 Inspector 中设为 `false`。GraphicRaycaster 扫描时跳过这些 Graphic，`IsPointerOverGameObject()` 返回 `false`
- 修复：`BuffIcon.Setup()` 中对 `UpgradeCategory.Item` 类型显式设置 `_iconImage.raycastTarget = true`
- 预防规则：**所有需要点击交互的 UI 元素，其 Image 组件的 `raycastTarget` 必须为 `true`**。这是 GraphicRaycaster 命中检测的必要条件。Button 的 `targetGraphic` 仅影响按钮视觉过渡，不影响射线检测
- 文件：`Assets/Scripts/UI/BuffIcon.cs` (Setup)
<!-- locus:body:end -->
