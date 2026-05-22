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
updatedAt: 1779443784457
---

# project-mistake-note

## Summary
更新至 2025-07-16 — 添加 DigitSlot 数字不可见问题

<!-- locus:body:start -->
### DigitSlot 全拉伸 anchor 导致数字不可见
- 症状：DigitSlot 子节点（StaticImage/FillImage）锚点改为全拉伸 (0,0)-(1,1) 后，运行时 GO active=true、alpha=1 但数字不可见
- 疑因：HorizontalLayoutGroup (childControlWidth=true) + preserveAspect=true 的 Image 在拉伸锚点下 preferredWidth 计算异常，导致 DigitSlot 宽度坍缩为 0
- 状态：待修复
- 影响：第三期连击数字显示
<!-- locus:body:end -->
