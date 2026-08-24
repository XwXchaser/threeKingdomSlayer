---
id: kd_b45c2927-6f30-4348-9953-583d6ad951ee
injectMode: inherit
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# use-architecture-constraints

## Summary
新对话开发时如何加载和使用 anti-ghost-reference 架构规范的实用指南。

## Content
## 何时使用

开启新对话、开发新功能时，将此技能的约束注入到对话上下文中。

## 使用步骤

### 步骤 1：在新对话开始时加载架构规范

在第一条消息中告知 Agent：
```
请阅读 design/anti-ghost-reference.md 的架构规范，本次开发需遵循其中所有约束。
```

### 步骤 2：开发过程中让 Agent 自查

在 Agent 完成代码实现后，要求其自查：
```
请检查本次改动是否存在幽灵引用，逐项对照 anti-ghost-reference 检查清单。
```

### 步骤 3：提交前全局扫描

```
请在 Assets/Scripts/ 下搜索 Resources.Load、static Dictionary 缓存配置等幽灵引用模式，确认整个项目无残留。
```

## 搜索命令参考

| 幽灵引用模式 | 搜索正则 |
|---|---|
| Resources.Load 加载配置 | `Resources\.Load` |
| 静态字典缓存 | `static.*Dictionary` |
| 通过 enemyId/string 查找配置 | 视具体项目而定 |
| `.config` 间接访问 | `\.config\b` |

## 注意事项

- `Resources.Load` 本身不全是坏事，加载 Prefab（如 `EnemyPrefabs/`）是合理的
- 关键是**配置数据**不应通过 Resources.Load 获取，应通过 Inspector 引用
- 如果确实需要共享配置（多个 Prefab 用同一套参数），用 ScriptableObject + Inspector 拖拽，不用 Resources.Load
