---
id: kd_c2be871f-45ba-4f8f-9f3d-fd30ae6f3630
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 数据模块化交接（2026-10-24）：技能/招式/选角/天赋/状态/选关六个系统的数据化现状——已完成四刀（天赋前置显式化、选关解锁显式化+清单资产化、状态去枚举、天赋解锁载荷去枚举）、三个数据校验器菜单、剩余硬编码点与下一步建议。新对话的入口文档。
injectMode: inherit
aiEditMode: inherit
---

# 数据模块化 · 交接（2026-10-24）

> **背景（用户定调 ✅）**：技能 ✅ / 招式 ✅ / 选角 ✅（以后可能换英雄 ✅、换武器与对应招式 ✅✅）/ 天赋 ✅ / 状态 ✅ / 选关 ✅
> **都是增量或有延展的内容** ✅ —— 写死会导致项目僵化 ✅ ✅，后续开发推不动 ✅ ✅。
> 本次把这些系统里**「可调数值与内容编排」**全部搬进资产 ✅ ✅，**加一条内容 = 加资产 ✅ ✅**。

**分支** ✅：`feat/stage-select-talent-overlays` ✅（已 push ✅ ✅）

---

## 一、六个系统的现状 ✅

| 系统 | 载体（数据 ✅） | 加一条内容要做什么 | 状态 |
|---|---|---|---|
| **招式** ✅ | `AttackSkillConfig` ✅（114 个字段 ✅ ✅） | 加资产 ✅ ✅ | ✅ 本来就是数据驱动 ✅ |
| **技能** ✅ | `ActiveSkillDefinition` ✅ + `ActiveSkillPoolConfig` ✅ | 加资产 ✅ ✅ | ✅ 本来就是数据驱动 ✅ |
| **选角** ✅ | `HeroConfig` ✅：招式 ✅ / 移动表 ✅ / 天赋树 ✅ / 大招 ✅ 全是**引用** ✅ ✅ | 加资产 ✅ ✅ | ✅ **换英雄 ✅ 换武器 ✅ 换对应招式 ✅ 全程零代码改动 ✅ ✅** ✅ |
| **天赋** ✅ | `TalentTreeConfig` ✅：节点内联 ✅ + `prerequisiteIds` ✅（可分叉 ✅ ✅）+ `moveConfig`/`flagId` ✅（载荷组合 ✅ ✅） | 加资产 ✅ ✅ | ✅ **本次改造 ✅** ✅ |
| **状态** ✅ | 数值型 ✅ → `ComboBuffConfig` ✅ + `StatModifier` ✅ ✅；机制型 ✅ → `CombatBuffDefinition` ✅ + `BuffIds` ✅ ✅ | 加资产 ✅ ✅ | ✅ **本次改造 ✅** ✅ |
| **选关** ✅ | `RouteCatalogData` ✅ 资产 ✅ ✅：`stages` ✅ + `alwaysUnlockedStageIds` ✅ + `FakeRouteStageConfig.prerequisiteStageIds` ✅ ✅ | 加资产 ✅ ✅（**不用改场景** ✅ ✅） | ✅ **本次改造 ✅** ✅ |

**统一范式** ✅ ✅：> **一条内容 = 一个资产 ✅ + 资产之间用「引用 / id」关联 ✅ + 规则写在数据里 ✅ → 加内容零代码改动 ✅ ✅**

---

## 二、本次做的四刀 ✅

### ① 天赋前置显式化 ✅（`fb6af69` ✅）
- `TalentNodeConfig.prerequisiteIds` ✅（nodeId 列表 ✅ ✅，可多个 ✅ → **支持分叉** ✅ ✅）
- `TalentTreeConfig.GetPrerequisites` ✅：优先用显式 id ✅；**没填则退回旧行为** ✅（前置 = 列表上一项 ✅）→ **老资产零改动可用** ✅ ✅
- `TalentTreeState.IsAvailable` / `TryUnlock` ✅：改成「**所有前置**都解锁 ✅」✅
- `TalentTreePanel` ✅：连线**按边画** ✅ ✅（原来按"上一项"画 ✅ ❗ → 没法分叉 ✅ ✅）
- 新增 `TalentTreeConfig.Validate()` ✅ + 菜单 **`Tools/数据校验/天赋树 ✅`** ✅

### ② 选关解锁显式化 + 清单资产化 ✅（`262525b` ✅）
- 新增 **`RouteCatalogData`**（ScriptableObject ✅ ✅）：`stages` ✅ / `alwaysUnlockedStageIds` ✅ / `IsUnlocked` ✅（所有前置都通关 ✅ ✅）/ `Validate` ✅ ✅
- `FakeRouteStageConfig.prerequisiteStageIds` ✅（stageId 列表 ✅ ✅）✅
- `RouteCatalog.catalogData` ✅ 引用 ✅ ✅：`GetSelectableRoutes()` ✅ 优先用资产 ✅ ✅；**旧字段保留兜底** ✅ ✅✅ ✅
- `StageSelectUI` ✅：解锁判定走数据 ✅ ✅；旧的位置计数规则**只作兜底** ✅ ✅
- 资产 `Assets/RouteData/RouteCatalog.asset` ✅ ✅（2 条 ✅：教学关 9100 ✅ + 长坂坡 1 ✅；默认可选 = 前两条 ✅ ✅ —— **等价于旧规则 ✅**，不重新引入 BUG-002 ✅ ✅）
- 菜单 **`Tools/数据校验/关卡清单 ✅`** ✅

### ③ 状态去枚举 ✅（`86b8d7b` ✅）
- **事实** ✅：项目里有**两套 buff 系统** ✅ —— `BuffManager` ✅ + `ActiveBuff` ✅ + `StatModifier` ✅ **本来就是 string id** ✅ ✅（数值型状态早就是数据驱动 ✅ ✅）；`PlayerState.buffTimers` ✅ + `BuffType` ✅ 枚举只有 2 个值 ✅ ✅ 且**没人调 `ApplyBuff`** ✅ ✅ → 死通道 ✅ ✅
- **`BuffType` 枚举退役** ✅ ✅ → 换成 **`BuffIds`** ✅ ✅（`force_launch` ✅ / `probability_launch` ✅ ✅）
- `PlayerState` 的 buff 通道改成 **string id** ✅ ✅ → 与 `BuffManager` ✅ ✅ 同一套 ✅ ✅
- 新增 **`CombatBuffDefinition`** ✅ ✅（SO ✅ ✅）：`buffId` ✅ / `displayName` ✅ / `defaultDuration` ✅ / **`launchProbability`** ✅ ✅ → **击飞概率 0.3 从代码挪进资产** ✅ ✅（数值与原值一致 ✅ ✅）
- 资产 `Assets/RouteData/CombatBuffs/ProbabilityLaunch.asset` ✅ ✅ + Battle 场景接线 ✅ ✅
- 菜单 **`Tools/数据校验/战斗状态定义 ✅`** ✅

### ④ 天赋解锁载荷去枚举 ✅（`1d0332f` ✅）
- **事实** ✅：`TalentUnlockKind` ✅ **本来就是冗余的** ✅ ✅ —— 数据里 `kind=0 ⟺ 有 moveConfig` ✅、`kind=1 ⟺ 有 flagId` ✅ ✅ → 删掉它**不丢任何信息** ✅ ✅✅ ✅
- **`TalentUnlockKind` 枚举退役** ✅ ✅ → 载荷改成**两个独立可选字段** ✅ ✅：`moveConfig` ✅ + `flagId` ✅ ✅ → **一个节点可同时给两个载荷** ✅ ✅（= 组合 ✅ ✅）
- 消费方（`TalentTreeState` ✅ ×3 ✅ + `PlayerMoveStateMachine` ✅）✅ ✅ 去掉 kind 判断 ✅ ✅
- **迁移无损** ✅ ✅（实测 ✅ ✅）：9 个节点 ✅ —— 带招式 3 ✅（正是原 kind=0 的三个 ✅）、带开关 6 ✅ ✅（正是 kind=1 的 ✅）、都空 0 ✅ → **老资产一个字节都没改** ✅ ✅✅ ✅

---

## 三、三个数据校验器（改完数据一键验 ✅）

`Tools/数据校验/` ✅ 下：
- **天赋树 ✅** —— 前置 id 不存在 ✅ / 自指 ✅ / 环 ✅ / nodeId 重复 ✅ / 空节点 ✅
- **关卡清单 ✅** —— stageId 重复 ✅ / 前置 id 不在清单 ✅ / 成环 ✅ / 默认可选 id 不存在 ✅
- **战斗状态定义 ✅** —— buffId 为空 ✅ / 重复 ✅ / **不在 `BuffIds` 词表里 ✅**（拼错 id 最容易踩 ✅ ✅）/ 概率越界 ✅

---

## 四、还剩的硬编码（**都是「机制」不是「内容」✅ ✅**）

| 位置 | 说明 | 建议 |
|---|---|---|
| 36 个枚举 ✅ ✅（`Assets/Scripts/Data` ✅ ✅） | 多为**敌人/战斗内部状态** ✅ ✅（`EnemyState` ✅ / `BossState` ✅ / `RushMoveMode` ✅ …）✅ ✅ | **不动** ✅ ✅ —— 机制词表 ✅ ✅，加机制要写代码 ✅ ✅（那是应该的 ✅ ✅）✅ |
| `TalentUnlockKind` ✅ | ✅ **已退役** ✅ ✅ | — ✅ |
| `BuffType` ✅ | ✅ **已退役** ✅ ✅ | — ✅ |

**判断标准（写下来当规则 ✅ ✅）** ✅：
| 类别 | 载体 | 加一条要做什么 |
|---|---|---|
| **数值型** ✅（属性增减 ✅ / 时长 ✅ / 叠层 ✅） | 资产 ✅ ✅ | **加资产** ✅ ✅ |
| **机制型** ✅（强制击飞 ✅ / 概率击飞 ✅ / 敌人行为 ✅） | 资产 + 代码 ✅ ✅ | 加资产 ✅ ✅（新机制仍要写代码 ✅ ✅）✅ |

→ **「可调数值与内容编排」已全部数据化 ✅ ✅** ✅ —— 剩下的枚举都是**机制词表** ✅ ✅ ✅。

---

## 五、下一步建议（新对话从这里接 ✅ ✅）

1. **选关页正式做起来** ✅ ✅ —— 清单资产 ✅ ✅ 已就位 ✅ ✅；要做的是**UI** ✅ ✅（`StageSelectUI` ✅ ✅ 现在是程序化生成按钮 ✅ ✅ —— 可换成正式美术 ✅ ✅）+ **解锁状态的显示** ✅ ✅（数据驱动 ✅ ✅ ✅）。
2. **天赋页** ✅ ✅ —— 面板 ✅ ✅ 的连线已改成按边 ✅ ✅ → 现在可以**真做分叉** ✅ ✅ 了 ✅ ✅（美术稿 ✅ ✅ → `treePosition` ✅ ✅ 填进去 ✅ ✅ ✅）。
3. **补齐关卡的"正式"配置** ✅ ✅ —— ⚠️ **N1 的第 0 条战斗还是开发测试战斗** ✅ ✅（`Test_Node_Battle` ✅ stageId 1199 ✅ ✅，血量 ×0.25 ✅；正式战斗 `Formal_N1_Battle` ✅ 1101 ✅ 排在第 1 条 ✅ ✅）→ **正式开发前应删掉这一条** ✅ ✅（删后 `Formal_N1_Battle` ✅ 自动变成第 0 条 ✅ ✅）✅ —— **待用户拍板** ✅ ✅。
4. **第一关命名** ✅ ✅ —— 现为「长坂坡·卷轴 ✅ ✅」，但它其实含 N1→E0→J1→EV→N3 整条线 ✅ ✅，若按"一关=一段" ✅ ✅ 计算 ⚠️ 命名可能还要再定 ✅ ✅ ✅。

---

## 六、坑（勿重复踩 ✅ ✅）

1. **别用 GUID 引用扫描来判断资产是不是孤儿** ✅ ❗ —— 规格 ✅ ✅ 是**按名字**加载的 ✅ ✅（`N3.asset` ✅ ✅ 当成孤儿删了 ❗❗ → 它是**部署中 N3 段的规格** ✅ ✅ ✅）。清资产前**按名字/路径也查一遍** ✅ ✅✅ ✅。
2. **枚举加一种值 = 所有资产重映射** ✅ ❗ → 改成独立可选字段 ✅ ✅（老资产继续有效 ✅ ✅✅ ✅）。
3. **存档兼容** ✅ ✅：天赋存 **nodeId** ✅ ✅（不是序号 ✅ ✅）→ 改前置结构**不毁老档** ✅ ✅；关卡进度存 stageId ✅ ✅ ✅。
4. **回退链式前置要保留兜底** ✅ ✅：`GetPrerequisites` ✅ ✅ 没填 id 时退回"上一项" ✅ ✅ → 老资产零改动 ✅ ✅✅ ✅。
5. **Unity 改完字段要重存资产** ✅ ✅ —— 否则 YAML 里留着失效的键 ✅ ✅（`unlockKind` ✅ ✅ / `unlockKind` ✅ ✅ 之类 ✅ ✅）；Unity 忽略未知键 ✅ ✅ ✅，但会误导人 ✅ ✅。
6. **改场景资产后要 `AssetDatabase.Refresh()`** ✅ ✅ —— 否则编辑器还在用缓存的旧版本 ✅ ✅（今天回退 J1 时踩过 ✅ ✅ ✅）。

---

## 七、相关文档 ✅ ✅
- **使用文档（给新会话/AI 的入口 ✅ ✅）** ✅：
  - **功能地图 ✅ ✅**：`memory/unity-project-understanding/README.md` ✅ ✅ —— 「要做什么 → 看哪份 → 读哪个代码 ✅ ✅」✅ ✅
  - 选关/路线 ✅ ✅：`memory/unity-project-understanding/stage-select-and-routes.md` ✅ ✅
  - 天赋树 ✅ ✅：`memory/unity-project-understanding/talent-system.md` ✅ ✅
  - 战斗状态 ✅ ✅：`memory/unity-project-understanding/buff-and-status.md` ✅ ✅
  - Y 场景管线 ✅ ✅：`memory/unity-project-understanding/yscene-table-pipeline.md` ✅ ✅
  - ⏳ **招式/技能/选角 ✅ ✅ 还没写** ✅（需先读一遍代码 ✅ ✅）✅ ✅ —— 从功能地图 ✅ ✅ 能看到这条待补 ✅ ✅。
- Y 场景表驱动管线（**另一条线** ✅ ✅）：`plan/scene-pipeline-handoff-20261023.md` ✅ ✅
- Y 场景重建方案（批次状态 ✅ ✅）：`plan/y-scene-rebuild-plan-20261023.md` ✅ ✅
- 校验工具化方案（刀一~刀四 ✅ ✅）：`plan/yscene-verification-tooling-plan-20261024.md` ✅ ✅
- 已知问题（工具侧 ✅ ✅）：`memory/known-issues.md` ✅ ✅

## 八、规矩 ✅ ✅（新会话务必遵守 ✅ ✅）
1. **改某个功能 → 同步更新它对应的那份使用文档** ✅ ✅（否则文档开始骗人 ✅ ❗✅ ✅）。
2. **文档与代码不一致时以代码为准** ✅ ✅（每份文档末尾都列了对应代码路径 ✅ ✅ —— 先核对再动手 ✅ ✅✅ ✅）。
3. **不确定就量** ✅ ✅ —— 别脑内枚举假设 ✅ ✅（今天两次误判 J1 路牌位置 ✅ ✅ ✅，都是没量 ✅ ✅❗）✅ ✅。
4. **不确定要动哪个域** ✅ ✅ → 先看**功能地图** ✅ ✅ ✅（`memory/unity-project-understanding/README.md` ✅ ✅ ✅）✅ ✅。
