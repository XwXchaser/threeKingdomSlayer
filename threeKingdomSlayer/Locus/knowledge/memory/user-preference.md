---
id: kd_builtin_memory_user_preference
type: memory
path: user-preference.md
title: user-preference
injectMode: rule
summaryEnabled: false
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1778051399341
updatedAt: 1779940228617
---

# user-preference

<!-- locus:maintain-rules:start -->
- Record only long-term user preferences that stay stable across tasks
- Prioritize language, reporting style, code style, taboos, and explicit requirements
- Keep each entry short and limited to stable preferences or hard constraints
- Keep the list within 20 items and merge similar preferences
- Remove one-off arrangements, temporary phrasing, and unconfirmed inferences
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
- **先分析后实现**：若用户未在对话中明确提及「实现」「开始做」「动手」等指令，仅做分析和方案设计，输出 Plan 给用户确认。只有在用户明确说「开始实现」「按这个方案做」之后才进入工程改动。收到新需求默认先思考，不直接修改代码或场景。
<!-- locus:body:end -->
