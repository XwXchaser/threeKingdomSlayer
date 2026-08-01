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
updatedAt: 1785469358253
---

# user-preference

## Summary
用户交互偏好：分析指令不可直接实现

<!-- locus:body:start -->
## 交互偏好

- **先分析后实现**：若用户未在对话中明确提及「实现」「开始做」「动手」等指令，仅做分析、方案设计和调查，不直接修改代码或场景。只有在用户明确说「开始实现」「按这个方案做」之后才进入工程改动。
- **「分析」= 仅分析，禁止实现**：当用户说「分析」时，严格只做分析、讨论方案、梳理逻辑，不允许直接动手改代码。明确说「开始实施」「实现」「做吧」等指令后才可修改代码。
- **任务闭环与回复保证**：每次任务必须以一条明确的用户可见回复结束，说明已完成的工作、验证结果、未完成项或阻塞原因，以及下一步需要用户做什么。即使工具调用失败、子程序异常、超时、返回空结果或任务被中断，也不得静默结束；必须主动报告当前状态。调用子程序后不能把等待子程序作为最终状态，必须在收到结果后继续处理并回复；若子程序无结果或失败，须说明失败并改用直接工具或给出明确的人工排查步骤。
<!-- locus:body:end -->
