---
id: kd_4a9116b1-c70a-4de3-8eeb-801deb71c4fe
type: memory
path: project-mistake-note.md
title: project-mistake-note
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1778764012219
updatedAt: 1783672691761
---

# project-mistake-note

## Summary
更新至 2025-12 — 新增 HUD 头像火焰特效开发经验：先用中心 debug target 验证链路，再确保特效真正进入头像内部层级

<!-- locus:maintain-rules:start -->
- Keep only durable and reusable project memory
- Consolidate duplicates or conflicts into the latest conclusion
- Remove temporary context, one-off tasks, and unsupported guesses
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
### UI新特效难以在目标位置验证时，先在屏幕中心做 debug target 独立验证渲染链路 ✅ 已验证（2025-01）
- 症状：给头像背后加火焰特效，多次调整后完全不可见，无法判断是渲染问题还是逻辑问题
- 根因：特效直接挂在目标位置（头像旁）时，可能被遮挡、alpha=0、CanvasGroup 隐藏、sibling order 错误等多重因素同时影响，无法逐一排查
- 修复：在屏幕中心放置一个独立的 debug target（ReadyFireEffect_Debug），先确认该 target 可见、粒子系统/帧动画正常工作，再回迁到目标位置
- 预防规则：**当 UI 新特效在目标位置反复不可见时，不要盲目调整参数。先在屏幕中心创建一个独立 debug target 验证渲染/逻辑链路完整，确认可见后再迁移到目标位置并调整参数。验证完成后务必移除 debug target**
- 文件：`Assets/Scripts/UI/UIReadyFireEffect.cs`, `Assets/Scripts/UI/UltimateButtonUI.cs`, `Assets/Scenes/Battle.scene`

### HUD 头像火焰特效挂在外部平级节点，运行时容易“逻辑已触发但看起来没生成” ✅ 已修复（2025-12）
- 症状：大招充能满后逻辑已触发，甚至中心调试 target 可见，但头像位置始终观测不到火焰，容易误判为特效逻辑失效
- 根因：`ReadyFireEffect` 初期挂在 `Health(Slider)` 下，和 `UltPortraitButton` 是平级关系，只能靠外部 offset 对齐头像；一旦头像布局、尺寸、层级发生变化，特效就可能偏离头像、被遮挡，或虽然存在但不在预期区域
- 修复：将火焰节点迁入 `UltPortraitButton` 内部，作为头像按钮的子物体，并放在 `UltBase` / `UltFill` / `Head` 之前渲染；位置和尺寸改为在头像局部空间内调整
- 预防规则：**所有“附着在某个 UI 元素上”的持续特效，都应优先挂在该 UI 元素内部，而不是挂在外部父节点后靠 anchoredPosition/offset 对齐。只要需求是“跟着某个 UI 元素走并稳定处于其前后层级”，就必须优先保证层级归属正确，再谈参数微调**
- 文件：`Assets/Scenes/Battle.scene`, `Assets/Scripts/UI/UltimateButtonUI.cs`, `Assets/Scripts/UI/UIReadyFireEffect.cs`
<!-- locus:body:end -->
