---
id: kd_a6d20422-f158-4be7-8380-fa469d394caf
injectMode: inherit
aiMaintained: inherit
skillEnabled: true
skillSurface: command
---

# analyze-before-implement

## Summary
当用户要求分析可行性时，只分析不实现；等用户确认后再动手。

## Content
## 规则

当用户的请求中包含"分析可行性"、"分析一下"、"这个能做吗"等类似表述时：

1. **只做分析，不做实现** — 不创建/修改任何代码文件或场景资产
2. 分析内容应包括：技术路径、风险评估、依赖项、替代方案
3. 明确给出"可行"或"不可行"的结论
4. 结束时询问用户是否要进入实现阶段

用户明确说"开始实现"或"做吧"后，才进入工程实现。
