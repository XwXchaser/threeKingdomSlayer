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
updatedAt: 1782490275055
---

# project-mistake-note

## Summary
更新至 2025-08-10 — 新增 BuffIcon raycastTarget=False 导致 UI 点击穿透

<!-- locus:body:start -->
### 自定义Editor中新增struct List字段不显示 Inspector 配置 ✅ 已修复（2025-06-27）
- 症状：`UpgradeDefinitionEditor` 中新加的 `DrawCycloneSection()` 已验证代码路径和数据均正确（`cycloneLevels` 5个元素、`effectType` 匹配、`FindProperty` 非null），但 Inspector 中不显示 Lv.1–Lv.5 配置
- 根因：Editor 脚本与源文件虽时间戳同步，但 Unity 未触发 domain reload / 重新编译，导致 Inspector 使用了旧版 Editor DLL（旧版无 `case "passive_timed_cyclone"` 分支）
- 修复：强制 `unity_recompile` 后恢复正常
- 预防规则：**新增 `[CustomEditor]` 分支或修改 Editor 脚本后，若 Inspector 不生效，先执行 `unity_recompile` 排除 DLL 过期问题，不要先怀疑代码逻辑**
- 文件：`Assets/Scripts/Editor/UpgradeDefinitionEditor.cs` (DrawCycloneSection)

### BuffIcon raycastTarget=False 导致 UI 点击穿透 ✅ 已修复（2025-08-10）
- 症状：点击血包 BuffIcon 时 `overUI=False`，InputManager 将点击降级为游戏 stab 攻击，血包无法使用
- 根因：BuffIcon 的 `Icon` / `Frame` 子级 Image 的 `raycastTarget` 在 Inspector 中设为 `false`。GraphicRaycaster 扫描时跳过这些 Graphic，`IsPointerOverGameObject()` 返回 `false`
- 修复：`BuffIcon.Setup()` 中对 `UpgradeCategory.Item` 类型显式设置 `_iconImage.raycastTarget = true`
- 预防规则：**所有需要点击交互的 UI 元素，其 Image 组件的 `raycastTarget` 必须为 `true`**。这是 GraphicRaycaster 命中检测的必要条件。Button 的 `targetGraphic` 仅影响按钮视觉过渡，不影响射线检测
- 文件：`Assets/Scripts/UI/BuffIcon.cs` (Setup)

### Hit trigger 遗留导致击飞落地后闪现 HitFlash ✅ 已修复（2025-06-07）
- 症状：Enemy_101 击飞落地后播放 HitFlash 动画而非直接回到 Idle
- 根因：`AttackWave.HitTarget()` 先调 `TakeDamage()`（设置 Hit trigger，此时 state 仍为 Stunned 而非 Launched），再调 `Launch()`。`Launch()` 的 `_animator.Play("Launched_Rise")` 不会清除已设置的 Hit trigger。落地切回 Idle 后，Idle→HitFlash 过渡（HasExitTime=False, If=Hit）立即捕获该遗留 trigger
- 修复：`Enemy.Launch()` 中 `_animator.Play("Launched_Rise")` 之前加 `_animator.ResetTrigger("Hit")`
- 预防规则：**动画状态切换前清理可能竞态的 trigger**，尤其是 `TakeDamage` 和 `Launch` 这种同一帧内先后调用的场景
- 文件：`Assets/Scripts/Enemy/Enemy.cs` (Launch)
<!-- locus:body:end -->
