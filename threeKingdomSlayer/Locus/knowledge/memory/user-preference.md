---
id: kd_80a1d38e-bdc4-4777-a6de-ef1f2e026ca9
type: memory
path: user-preference.md
title: user-preference
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780999254363
updatedAt: 1780999272020
---

# user-preference

## Summary
用户交互偏好：分析指令不可直接实现

<!-- locus:body:start -->
## 交互偏好

- **先分析后实现**：若用户未在对话中明确提及「实现」「开始做」「动手」等指令，仅做分析和方案设计，输出 Plan 给用户确认。只有在用户明确说「开始实现」「按这个方案做」之后才进入工程改动。收到新需求默认先思考，不直接修改代码或场景。
- **「分析」= 仅分析，禁止实现**：当用户说「分析」时，严格只做分析、讨论方案、梳理逻辑，不允许直接动手改代码。明确说「开始实施」「实现」「做吧」等指令后才可修改代码。
<!-- locus:body:end -->
