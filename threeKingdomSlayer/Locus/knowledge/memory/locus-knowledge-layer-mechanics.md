---
id: kd_d0ab600e-a4df-4eba-8640-0d151b90b944
injectMode: inherit
summary: Locus 三层知识通道实测：design/memory/plan 可检索，skill/ 可注册为命令但不可检索，workflow/ 两者都不通；skill 注册必须有 frontmatter summary。
aiEditMode: inherit
---

# Locus 知识层可发现性机制（本项目实测）

## 三层通道是彼此独立的

| 目录 | 知识检索（`knowledge_query`） | Skill 注册（`skill_list` / 斜杠命令） |
|---|---|---|
| `design/` `memory/` `plan/` `reference/` | 可检索 | 否 |
| `skill/`（含任意子目录） | 不可检索 | 可注册，命令默认取文件名 |
| `workflow/` | 不可检索 | 不可注册（补 skill frontmatter 也无效） |

验证方式：用唯一短语做 `knowledge_query`；`design/anti-ghost-reference.md` 能被「幽灵引用」命中，而含同一词的 `skill/use-architecture-constraints.md` 不会命中；`skill/gpt-image-generation.md` 多次出现 `MUSK_API_KEY`，同样不被检索命中。

## Skill 注册的硬性前提

1. frontmatter 必须有非空 `summary`。只有 `skillEnabled: true` + `skillSurface: command` 不会注册，且**静默失效**：2026-08-23 创建的项目 skill 文档因缺 `summary`，到 2026-09-14 前全部不可调用、不可检索。
2. `summary` 要写成「何时加载、何时忽略」，它是被召回的唯一判据，不是内容摘要。
3. 命令默认由文件名推导（`use-architecture-constraints.md` → `/use-architecture-constraints`）；需要别名时才写 `commandTrigger`（`deepseek-anti-loop.md` → `/anti-loop`）。
4. 子目录可用：`skill/unity-ui/patterns.md` 注册为 `/patterns`。
5. 旧格式的 `## Summary` / `## Content` 结构包裹层不影响注册，但已按 Locus 规范移除，正文摘要改为 H1 下的导语。

## 自定义 `workflow/` 目录不可用（该目录已废弃）

曾单独建立的 `Locus/knowledge/workflow/` 没有 `.locus-meta`，不进注入的知识结构，也不进检索索引。实测：给其中文档补 `summary` + `skillEnabled: true` + `skillSurface: command` 后，`skill_list source=project` 仍不返回该条目，编辑该文件也不触发「Knowledge index: updated」；结论是放进去的文档只能被**知道确切路径**的一方用 `read` 打开。

2026-09-14 处置：该目录文档已并入 `skill/workflows/`，目录删除。此后流程类文档一律放 `skill/workflows/<name>.md`（可召回、可命令触发，`skill_list` 实测注册成功），不再新建同级知识目录。

## 引用纪律

跨目录引用必须写完整项目相对路径，例如 `Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md`。由于 `skill/`（含 `skill/workflows/`）不参与检索，路径是它唯一的导航手段；文档改名或移位时必须同步全部引用方。
