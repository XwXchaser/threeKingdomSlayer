---
id: kd_2b297c74-8d71-40ca-bc78-6e9575cc5d03
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# BUG 与新增小需求收集（唯一滚动入口）

> **用途**：用户不定时反馈的 BUG 与新增小需求登记在这里，之后开新对话逐条修复或实现。
> **权威进度表**：`plan/october-milestone-plan.md`（承诺节点、验收标准、决策点）。本文件只收集条目，不替代里程碑。
> **不要再往** `plan/latest-todolist.md` 追加新条目，该文件已停用为历史记录。

## 登记规则

- ID：`BUG-###` 按发现顺序递增；`REQ-###` 独立编号，两者不共用序号。
- **每条必须自包含**：新对话只读这一条就能开工，不依赖当时的口头描述或聊天上下文。
- 涉及文件/资产必须写真实路径（含 `.cs`、`.prefab`、`.asset`），便于直接定位与回读验证。
- 状态取值：`待处理` → `进行中` → `待验收` → `已关闭`。修复或实现完成后移到文末「已关闭」区，保留结论与改动文件，不删除。
- 优先级：`高`（阻塞演示或流程） / `中` / `低`（观感、打磨）。
- 涉及图片/视频生成的条目，状态先置 `待提示词审批`：按 `memory/user-preference.md`，必须先提交中英文提示词、负面提示词、参考图职责与关键参数并获得批准，之后才允许调用生成接口。

### BUG 条目模板

````markdown
### BUG-000 一句话现象
- 日期：
- 类型：BUG / 表现 | 逻辑 | 数值 | UI | 性能
- 现象：
- 复现：
- 期望：
- 涉及：`Assets/Scripts/...`
- 优先级：
- 状态：待处理
- 备注：
````

### 需求条目模板

````markdown
### REQ-000 一句话需求
- 日期：
- 类型：需求 / 战斗 | 表现 | UI | 美术 | 工具
- 动机：
- 期望行为：
- 验收标准：
- 相关：`design/...`、`plan/...`
- 状态：待处理
````

---

## 待处理 BUG

暂无登记条目。

---

## 待实现需求

### REQ-001 张飞长枪换 3D 模型（含概念图）

- 日期：2026-10-07
- 类型：需求 / 美术 + 表现
- 动机：2D 精灵绕长轴自转会出现视觉变薄的副作用；换 3D 模型后同样的自转变成真正的绕轴旋转，表现更可信。
- 期望行为：长枪由 3D 模型承载，`stabRollDegrees` 的语义与取值沿用，无需改写招式参数。
- 前置条件：
  - 先出概念图确认枪身造型、比例与握持方式（尚未制作）。
  - 接手时先确认模型枪身长轴是否为其本地 Y 轴，以便 `stabRollDegrees` 直接沿用。
- 验收标准：画面中始终只有一把可见武器；三段枪突与指向突刺的手感、衔接、命中时序不变；`stab_v13` 高速帧替换可退休，刺出速度感另找表达方式。
- 相关：`plan/combo-progress-and-next.md` §6、`design/combo-zhangfei-stab3-launch.md`、`plan/stab-combo-video-reference-workflow.md`
- 状态：待提示词审批（概念图的生成提示词与参数尚未提交）

### REQ-002 多攻击动作敌人的预警挂点（每个攻击步骤一个）

- 日期：2026-10-07
- 类型：需求 / 战斗系统 + 配置
- 动机：当前每个敌人 Prefab 只有一个 `AttackTelegraphAnchor`（挂 `EnemyAttackTelegraph`），该敌人的所有攻击动作共用同一个预警点位，多种攻击动作时无法表达各自的出招点。
- 期望行为：
  - `AttackStep` 增加挂点引用（`EnemyAttackTelegraph telegraphAnchor`）；未指定时回退到该敌人的现有默认挂点。
  - Prefab 层级升级为「每攻击步一个挂点」，例如 `Enemy/AttackTelegraphAnchors/Attack1..N`；每个挂点独立保存 Position/Rotation/Scale，可各自绑定三帧 Sprite、颜色与播放节奏。
  - 运行时按当前攻击步骤使用对应挂点调用 `BeginWarning`，不再统一使用同一个实例。
  - 明确 Edit Mode 预览方式：Inspector 可指定「当前预览的攻击步骤」并显示该步骤对应挂点。
- 约束：禁止按 `spriteName`、攻击索引或字符串在运行时推断挂点位置（`design/anti-ghost-reference.md`）；现有已部署三帧 Sprite 的十个敌人 Prefab 不得被迁移破坏。
- 验收标准：多攻击动作敌人的每个攻击步骤可用不同挂点；Edit Mode 预览不修改挂点 Transform；战斗时序与伤害不变。
- 相关：`design/enemy-attack-telegraph.md`、`plan/attack-damage-frame-sync-plan.md` §9.3
- 状态：待处理

---

## 已关闭（归档）

暂无。
