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
updatedAt: 1779459656311
---

# project-mistake-note

## Summary
更新至 2025-07-16 — DigitSlot 尺寸统一 + 衰减速度视觉修复

<!-- locus:body:start -->
### DigitSlot 全拉伸 anchor 导致数字不可见 ✅ 已修复
- 症状：DigitSlot 子节点（StaticImage/FillImage）锚点改为全拉伸 (0,0)-(1,1) 后，运行时 GO active=true、alpha=1 但数字不可见
- 根因：HorizontalLayoutGroup (childControlWidth=true) + preserveAspect=true 的 Image 在拉伸锚点下 preferredWidth 计算异常，无 LayoutElement 导致 DigitSlot 宽度坍缩为 0
- 修复（2025-07）：
  1. DigitSlot prefab 添加 LayoutElement（preferredWidth=50, preferredHeight=50, minWidth=30, minHeight=30）
  2. ComboDisplayUI.RebuildDigits 末尾添加 LayoutRebuilder.ForceRebuildLayoutImmediate() 确保 ContentSizeFitter 即时生效
  3. 场景重导入恢复 ComboDisplayUI 序列化数据（重编译后 Inspector 字段被清空的 Unity 已知问题）
- 文件：Assets/Prefabs/UI/DigitSlot.prefab, Assets/Scripts/UI/ComboDisplayUI.cs

### DigitSlot 与「连」字尺寸不一致导致 fillAmount 衰减速度视觉差异 ✅ 已修复
- 症状：「连」字 FillImage（100×100）和 DigitSlot FillImage（50×50, DigitParent scale=2.5）虽然 fillAmount 数学衰减速率完全一致，但像素级视觉速度差 2 倍
- 根因：fillAmount 裁剪的是 RectTransform 渲染宽度，「连」100px vs 数字 125px（50×2.5）宽度不同，每秒消失像素数不同
- 修复（2025-07）：
  1. DigitSlot prefab 尺寸从 50×50 改为 100×100（LayoutElement 同步：prefW/H 100, minW/H 60）
  2. Battle.scene DigitParent localScale 从 (2.5, 2.5, 2.5) 重置为 (1, 1, 1)
- 文件：Assets/Prefabs/UI/DigitSlot.prefab, Assets/Scenes/Battle.scene
<!-- locus:body:end -->
