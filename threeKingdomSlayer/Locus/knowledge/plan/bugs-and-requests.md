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

### BUG-001 可破坏道具（箱桶）在重挑战后不重置

- 日期：2026-10-21
- 类型：BUG / 逻辑
- 现象：箱桶一类可破坏道具被打破后，**重新挑战同一关卡**时箱桶仍然是破的状态（没有重建），一直保留在场景里。
- 复现：进入关卡 → 打碎一个箱/桶 → 退出（或战败重来 / 从选关重新进入同一关）→ 再次进入该关卡 → 箱桶仍是碎的。
- 期望：关卡重开时，可破坏道具应回到初始状态（未破坏），与其它场景物件的重建逻辑一致。
- 涉及：`Assets/Scripts/Prop/`（`BreakableProp` 与其生成/重置逻辑）、`Assets/Scripts/Core/ColumnManager.cs`（`ClearAllColumns` / 关卡重置入口）、关卡节点的重新进入路径（`Assets/Scripts/RouteFake/FakeRouteRuntime.cs`、`Assets/Scripts/Managers/StageController.cs`）。
- 优先级：中（观感与可玩性；不阻塞流程，但会让重复挑战的关卡“越打越空”）
- 状态：待处理（用户 2026-10-21 反馈，指示“先放放”，本轮不修）
- 备注：需先确认道具状态是「场景对象残留」还是「存档里存了破坏状态」——本轮存档里已见到 `FakeRouteStageSaveSnapshot.nodeStates`（含 `completedEntryIndices`）与 `RouteNodeBattleSaveState`，两者都可能是来源；重置时应与「节点重进」路径一起处理。

### BUG-002 落地页「开始关卡」从存档检查点起步，正式关卡在选关里被锁死

- 日期：2026-10-21
- 类型：BUG / 流程（设计待定）
- 现象（两个现象同源）：
  1. 落地页点「开始关卡」进入的是**教学关的后段节点**（实测存档 `tutorial_route` 的 checkpoint = `E0`），而不是该关卡的起点 `N1`——因为 `FakeRouteRuntime.ResolveStartNode()` 在落地页入口会去恢复存档检查点。
  2. 选关界面里**正式关卡（长坂坡·卷轴节点预览，stageId=1）被锁住**：解锁规则是“按列表顺序，已通关 N 条则前 N+1 条可选”，而教学关未通关 → 正式关永远不可选。玩家打不过教学关就**无法进入正式关卡**（死锁）。
- 复现：新存档（或教学关未通关的存档）→ 落地页 → 选关 → 正式关显示「[未解锁]」不可点；点「开始关卡」则从教学关的检查点节点（E0）继续。
- 期望：正式关卡应当有可达路径（要么教学关可完成、要么允许直接选正式关、要么提供调试直选）。
- 涉及：`Assets/Scripts/RouteFake/FakeRouteRuntime.cs`（`ResolveStartNode` / `IsHubEntry`）、`Assets/Scripts/UI/StageSelectUI.cs`（`ResolveUnlock` / `CreateRouteButton`）、`Assets/Scripts/Core/SaveManager.cs`（`MarkStageCleared` / `GetFakeRouteSnapshot` / `ClearFakeRouteSnapshot`）、`Assets/Scripts/Core/StageConfigManager.cs`（`GetSelectableRoutes`）。
- 优先级：高（阻塞正式关卡的验收与试玩）
- 状态：待处理（2026-10-21 已用存档手段临时绕开：把教学关标记为已通关 + 当前关卡切到正式关；**未改代码**。绕开方式与回退步骤见备注）
- 备注：临时绕开做法（仅本机 Editor 存档）：`SaveManager.MarkStageCleared(9100)` + `SaveManager.SetActiveFakeRouteStage(1)`；回退 = 把 `clearedStageIds` 里的 9100 移除 + `SetActiveFakeRouteStage(9100)`。待产品决策：选关解锁规则是否改为“正式关卡默认可选、教学关只作为引导”。

---

## 待实现需求

### REQ-001 张飞长枪换 3D 模型（含概念图）

- 日期：2026-10-07
- 类型：需求 / 美术 + 表现
- 动机：2D 精灵绕长轴自转会出现视觉变薄的副作用；换 3D 模型后同样的自转变成真正的绕轴旋转，表现更可信。
- 期望行为：长枪由 3D 模型承载，`stabRollDegrees` 的语义与取值沿用，无需改写招式参数。
- 当前美术进度：刚性枪体 V02 合图已获用户明确验收，记录在 `StabWeaponArtSource/04_BodyV02/user_acceptance.json`。红缨V02已按双参考图和批准参数单次生成、技术校验并归档 `StabWeaponArtSource/05_TasselV02/`，待最终美术验收；完整母图和V01保留为历史参考。没有找到已交付的长枪 3D 模型，`Assets/Prefabs/Stab.prefab` 仍使用原 `SpriteRenderer`。
- 素材：项目内副本 `StabWeaponArtSource/01_Reference/assembly_reference.png`、`StabWeaponArtSource/02_OrthographicV01/body/`、`StabWeaponArtSource/02_OrthographicV01/ribbon/`；原始成图在 `C:/Users/Administrator/Pictures/gptGen/stab_orthographic_refs_v01/`。枪体/飘带分别放大展示，装配比例以完整母图为准。
- 前置条件：
  - 用户验收枪体后批准制作红缨独立三视图，已生成一次。当前入口为 `plan/stab-tassel-three-view-generation-prompts.md` 和 `StabWeaponArtSource/00_Index/tassel_v02_specification.json`；检查记录披露正面右边距6px、背面左边距14px，未满足32px目标，但没有裁断。完整装配三视图仍为历史草案。
  - 红缨输入为完整新画风母图及V01红缨正面单图，职责分别为连接与比例、独立红缨轮廓；输出只保留红缨，未重新生成已验收枪体。
  - 接手时确认模型枪身长轴为 Unity 接入层本地 Y 轴，以便 `stabRollDegrees` 沿用；现代码仍依赖 Sprite 长度、换帧和模糊，需要单独适配，并非直接替换 FBX 即可运行。
- 验收标准：画面中始终只有一把可见武器；三段枪突与指向突刺的手感、衔接、命中时序不变；`stab_v13` 高速帧替换可退休，刺出速度感另找表达方式。
- 相关：`plan/stab-3d-three-view-production-handoff.md`（素材、真实进度与后续制作入口）、`plan/combo-progress-and-next.md` §6、`design/combo-zhangfei-stab3-launch.md`、`plan/stab-combo-video-reference-workflow.md`
- 当前补充任务：V5缺左侧，用户要求前／左／右／后单图及合图。V06单次補左侧并完成交付 `StabWeaponArtSource/12_AssembledFourViewV06/`：四张单图、无字合图、中文标注版和trace。前右后沿用V5原文件，左侧整体等比对齐，左／右枪尖枪尾高度差0px；原图与合图像素校验通过。左側红缨与遮挡仍待美术验收。入口 `plan/stab-assembled-four-view-v06.md`。
- 状态：进行中（刚性枪体V02已验收；V06前左右后四图已交付，新增左侧待验收；旧顶底候选和3D模型接入未完成。V06只提交一次，中断恢复未重新生成，没有Unity工程或模式改动。）

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
