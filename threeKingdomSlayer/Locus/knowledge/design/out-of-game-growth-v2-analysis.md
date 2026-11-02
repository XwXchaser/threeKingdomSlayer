---
id: kd_cf9e200b-8b13-4677-91fc-9e499d9e006c
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 局外成长与关卡配置 · 现状分析与落地方案

> 2026-11 本轮分析产出。取代 `design/out-of-game-growth-system.md` 的"当前实现"部分（该文档的方案层仍有效，但实现现状以本文为准）。
>
> 本文所有结论均来自**工程实测**（代码 + 资产 + 运行时数据），不引用未验证的设计意图。文中标注 `[实测]` 的数据来自资产/运行时 dump，标注 `[文档]` 的来自知识库设计文档。

---

## 📚 本文定位：现状盘点底稿

本文是**证据底稿**（现状实测 + 决策记录 + 能力盘点）。具体设计与执行已拆到以下文档：

| 文档 | 内容 | 类型 |
|---|---|---|
| `design/talent-tree-architecture.md` | 天赋页面 · **功能架构**（两级结构 / 数据模型 / 解锁语义 / 存档 / 能力天赋落点 / 校验） | design |
| `design/talent-page-ui.md` | 天赋页面 · **页面需求设计**（信息架构 / 三态 / 连线 / 滚动 / 已知缺陷 / 分工） | design |
| `design/in-run-growth-and-unlocks.md` | **局内成长与解锁内容**（三选一 / 流派分线 / 槽位 / 满槽金币补偿 / 等级上限 / 秘籍系统） | design |
| `design/stage-configuration.md` | **关卡配置**（清单 / 拓扑 / StageConfig 字段 / 波次规范 / 本周配方 / 104 两阶段 / 铜钱 / 调试工具） | design |
| `design/move-split-and-derivatives.md` | **招式衍生与拆分**（招式图模型 / 点线划线口径 / C2·C4 / Slash 衍生 / 扩展协议） | design |
| `plan/growth-stage-move-plan-20261102.md` | **本周 todolist**（依赖顺序 + 5 个批次 + 待定项 + 风险） | plan |
| `plan/talent-scheme-alignment-20261102.md` | **天赋方案对齐**（我的方案 vs 另一侧已实现方案；11 项差异 + 4 个待拍板冲突 + 任务清单裁剪） | plan |
| `design/art-asset-brief-20261102.md` | **美术素材需求（美术依据文档）**（A 节点图标 / B 分支 / C 页面 / D 秘籍 / E 反馈；按编号交付 + 抗变更约定 + 变更影响矩阵） | design |

> **拆分理由**：原 3 个主题里，「局内成长与解锁内容」（三选一 / 槽位 / 满槽补偿 / 等级上限 / 秘籍）既不属于页面，也不属于关卡，而是天赋解锁效果的**消费端** → 独立成文。

---

## 0. 一页结论

**要做的事在数据模型层面都有承载物，不需要新架构。** 但实测发现 4 个硬问题，其中 2 个直接卡住"解锁内容分配"这条主线：

| 级别 | 问题 | 影响 |
|---|---|---|
| **P0** | `talent_unlock_c2` 的 `moveConfig` 在提交 `d278593`（2026-10-29）里被**主动清空** → **C2（stabC2）现在开局就能用**；同时该节点变成 hidden + 无载荷 + 无后继者的**孤儿节点** | 本轮需求要求 C2 由天赋解锁 → 与该提交直接冲突，必须回滚 |
| **P0** | 8 个可见天赋节点里，只有 `ice_thaw` / `ice_break_push` 两个 flag 有代码消费；`break_formation` / `pursuit` / `extra_choice` / `undying` **全是空壳**（资产描述里就写着"占位，待讨论"） | 要做的"生命值 / 连击加伤 / 槽位 / 钱币"4 个能力**全部没有现成开关** |
| **P1** | 卷轴正式关 `clearCoinReward = 0`、`killMilestones` 未配、节点 `rewards[]` 字段存在但**运行时从不读取** → 单次通关收入 ≈ 敌人只数 × 10 铜钱 | 天赋总定价 1750 铜钱 ≈ 需要 35 次通关才买得完，局内外闭环在数学上不成立 |
| **P1** | 主动 / 被动槽**共用同一个字段** `HeroConfig.itemSlotCount`(=4)；被动上限 6 硬编码在采样器里 | "降配到 3 主动 3 被动"不是改个数值，要先拆成独立计数器 |

另有两点确认：

- **1011 vs 1012 的区别就是"有无 C 技霸体前摇"**（用户猜测正确），外加攻速更慢、命中帧更早、招架窗口更长。
- **1012 在任何正式关里一次都没出现过**（只在教学关 `Tutorial_E0_Battle`）。

---

## 1. 已确认的四个设计决策（2026-11）

| # | 决策 | 对实现的约束 |
|---|---|---|
| 1 | **解除"禁止纯数值提升"限制**；「保持连击本身就是一种条件」 | 连击加伤可作天赋。`ComboBuffConfig.triggers[]` 支持多档位 → 可做「阈值下调（20→15）」或「新档位（40 连击 +20%）」，两者都是**有条件的**，不算粗暴 |
| 2 | **降配：基础 3 主动 + 3 被动**；隔壁能力渐进解锁更多槽位 | 拆 `HeroConfig` 字段；被动上限 6 硬编码改为可配 + 天赋加成 |
| 3 | **满槽 → 金币补偿** | 插入点已定位（`UpgradeChoiceManager.ShowNextChoice` 空候选分支）；局内铜钱 = 局外铜钱，闭环天然成立 |
| 4 | **技能解锁按流派分线** | 流派定义见 §3；解锁通道 `requiredTalentIds` 已实现但零使用 |

> 决策 1 推翻了 `design/out-of-game-growth-system.md` 里"禁止直接伤害数值提升"的约束。该约束若要保留需单独确认——本文按"已解除"处理。

---

## 2. 现状盘点

### 2.1 天赋系统骨架（已存在，可直接用）

`Assets/Scripts/Data/TalentTreeConfig.cs` + `Assets/ScriptableObjects/Talents/TalentTree_Main.asset`（treeId = `main`）

| 项 | 实现 |
|---|---|
| 节点结构 | `TalentNodeConfig` 内联类：`nodeId` / `displayName` / `description` / `coinCost` / `moveConfig` / `flagId` / `prerequisiteIds` / `treePosition` / `icon` / `hidden` |
| **多分支支持** | ✅ **已支持**：`prerequisiteIds` **显式空列表 = 无前置**（可作独立根），非空 = 全部前置都要解锁 |
| 招式解锁载体 | ✅ `moveConfig` 直接拖 `AttackSkillConfig`；运行时 `PlayerMoveStateMachine.IsMoveLocked` 判定「锁住就不响应、也不回退成别的招式」 |
| 技能解锁载体 | ✅ **`UpgradeDefinition.requiredTalentIds` + `TalentUpgradeRequirementProvider` 已实现**，但全项目 **0 条资产使用** |
| 存档 | `SaveData.unlockedTalents`（nodeId 列表）；铜钱走 `SaveManager` → `PropSaveSection` → `PropType.Coin` |
| 校验器 | `TalentTreeValidator`（菜单"校验天赋树"）+ `TalentTreeConfig.Validate()` |

#### 当前 9 个节点 [实测]

| # | nodeId | 名称 | 价 | 载荷 | 前置 | hidden | flag 有代码消费？ |
|---|---|---|---|---|---|---|---|
| 0 | `talent_unlock_c2` | 指向突刺 | 100 | **无（moveConfig 空 + flagId 空）** | 无 | ✅ | — |
| 1 | `talent_ice_thaw` | 重击解冻 | 150 | flag `ice_thaw` | 无 | | ✅ `IceBreakSession` |
| 2 | `talent_ice_break_push` | 破冰冲击 | 200 | flag `ice_break_push` | `ice_thaw` | | ✅ `IceBreakSession` |
| 3 | `talent_unlock_c4` | 枪突收尾 | 150 | move `Zhangfei_LaunchFinisher` | `ice_break_push` | | — |
| 4 | `talent_break_formation` | 破阵 | 200 | flag `break_formation` | `unlock_c4` | | ❌ **空壳** |
| 5 | `talent_pursuit` | 追击 | 250 | flag `pursuit` | `break_formation` | | ❌ **空壳** |
| 6 | `talent_strategy` | 运筹 | 300 | flag `extra_choice` | `pursuit` | | ❌ **空壳** |
| 7 | `talent_undying` | 不屈 | 400 | flag `undying` | `strategy` | | ❌ **空壳** |
| 8 | `talent_unlock_parry` | 招架 | 0 | move `Zhangfei_Parry` | 无 | ✅ | — |

**总定价 1750 铜钱**（含 hidden 的 c2）。当前结构是**一条单链 + 2 个隐藏旁支**，不是多分支。

> ⚠️ `talent_ice_thaw` 与 `talent_unlock_c2` 的 `treePosition` **完全相同**（`-56, 360`），目前靠 c2 隐藏才没视觉冲突。

#### 两个必须先修的数据缺陷

**① `talent_unlock_c2` 的引用被主动清空（提交 `d278593`，2026-10-29）**

该提交前的引用是 `moveConfig: {fileID: 11400000, guid: f0d6ad785959ad941b9b51d73a275d01}` = `Zhangfei_StabR2`（已实测确认该 guid 指向 `Assets/ScriptableObjects/Moves/Zhangfei/Zhangfei_StabR2.asset`）。
提交把它改成 `{fileID: 0}` 并加了 `hidden: 1`。

commit message 的用途②写明「招式改为直接开放后，把原来的解锁节点藏起来」→ **这是有意的决定，不是意外丢失**。但改得不干净，留下三处问题：

| 问题 | 后果 |
|---|---|
| 节点变成 `hidden + 无 moveConfig + 无 flagId` | `HasValidUnlockPayload == false` → `TryUnlock` 返回 `Invalid` → **永远买不了**；且 `prerequisiteIds = []` → **不参与任何链** → 彻底成为孤儿节点 |
| 同一提交把 `talent_ice_thaw.prerequisiteIds` 从 `[talent_unlock_c2]` 清空 | 冰系链被剪断，c2 在链上不再有位置 |
| 同一提交把 `talent_ice_thaw.treePosition` 改成 `(-56, 360)` | 与 hidden 的 `talent_unlock_c2`（同样是 `-56, 360`）**坐标完全重叠** |

净效果：**C2 从「被天赋锁住」变成「完全免费直接开放」，而天赋树上没有任何东西表示它。**

→ 本轮需求明确要求「stabC2 由天赋解锁」→ **必须回滚该决定**：恢复 `moveConfig` 指向 `Zhangfei_StabR2`、取消 `hidden`、并重新梳理它与 `talent_ice_thaw` 的前置关系。

**② `TalentTreeConfig.Validate()` 的环检测不完整**

```csharp
cur = ps[0];   // ← 只沿第一条前置边回溯
```
多前置节点只检查第一条边。多分支上线前必须修，否则会出现校验器看不见的环。

#### 天赋页实现现状（`Assets/Scripts/UI/TalentTreePanel.cs`，447 行，零场景搭 UI）

运行时按 `TalentTreeConfig` 生成全部节点与连线，美术替换点全是 `SerializedField`，节点坐标直接取 `treePosition`（1080×1920 设计像素，原点=面板中心）。

**✅ 已支持多分支，重排数据即可，UI 零改动**

| 能力 | 位置 |
|---|---|
| 连线按「边」画（每个节点 → 它的每个前置） | `BuildNodes()` 第 204-219 行 |
| 节点坐标自由 | `rect.anchoredPosition = node.treePosition` |
| 多前置 | `TalentTreeConfig.GetPrerequisites` |
| 隐藏节点不显示但仍建对象（保下标一一对应） | 第 226-229 行 |
| 刷新三态（锁定/可买/已解锁）+ 连线点亮 | `Refresh()` 第 271-320 行 |
| 数据自检 | `TalentTreeConfig.Validate()` + `TalentTreeValidator`（菜单「校验天赋树」） |

**🔴 最大的落地障碍：天赋页没有滚动容器**

`Assets/Scenes/TalentTree.scene` **没有 ScrollRect 组件**（无 `!u!225`）。面板是全屏 `Stretch` 的静态层，节点直接挂在 `_nodeRoot` 上。

当前 8 个节点挤在 x ∈ [-56, 56]、y ∈ [-480, 360]（一条斜线），勉强塞进 1080×1920。
→ **换成 20 个节点的多分支后必然溢出屏幕，节点做出来是看不到的。**
→ 必须先加 **ScrollRect + ContentSizeFitter**，或改成**分页**。这是多分支天赋树的硬前置。

**🔴 连线当前根本没画出来**

```csharp
// TalentTreePanel.cs:402-412
private static void PlaceConnector(RectTransform rect, Vector2 from, Vector2 to)
{
    rect.sizeDelta = new Vector2(length, 0f);   // ← 高度被设成 0
    ...
}
```

`connectorThickness`（默认 10f）**全文件只出现在声明处（第 34 行），从未被使用**。场景里 `connector` 精灵已配（guid `12ae361e6cdd27244be09cb399e502f9`），但 RectTransform 高度为 0 → 精灵被压成零高 → **运行时看不到任何连线**。

→ 天赋页现在实际呈现的是**一堆孤立圆点**，根本没有「树」的结构。这也解释了为什么 8 个节点能被塞成一条斜线。
→ 修法：`sizeDelta = new Vector2(length, connectorThickness)`，或改用 `Image.type = Sliced` + 九宫格。**建议实机截图复核一次**（本文只做了代码级确认）。

**🟠 没有分支归属概念**

`TalentNodeConfig` 只有 10 个字段（nodeId / displayName / description / coinCost / moveConfig / flagId / prerequisiteIds / treePosition / icon / hidden），**没有 branchId / groupId**。版式信息只有 `treePosition` 一个二维坐标。

→ 20 个节点平铺在 1080×1920 里，玩家**读不出哪几个属于同一条枝**；加一个节点就要重排全树坐标。
→ 解法：给 `TalentNodeConfig` 加 `branchId`（纯字符串，**不影响存档**——存档存的是 nodeId 字符串而非节点下标；`hidden` 字段已经开了先例）。

**🟠 三处需要注意的既有限制**

| 项 | 问题 |
|---|---|
| **隐藏节点不画连线** | `BuildNodes()` 第 213 行 `if (from.hidden || to.hidden) continue;` → 用隐藏节点当「天赋门」时，玩家看到的是一个**没有连线的孤立节点**，不知道它从哪来 |
| **没有「页 / 分支」概念** | 全部节点一个平面。`BattleHubTabBar.talentTreeTab` 是「从落地页进天赋页」的一级导航，不是天赋页内部的分支页。若需求指多 tab → 面板目前完全没有该结构 |
| **没有二次确认 / 撤销** | `OnNodeClicked` 点一下即买、扣钱即生效。局外花 100–400 铜钱（现总 1750，将来 2850）不可撤销，局外成长体验偏硬 |

**🟢 与流派分线完全兼容**：流派分线只需在 `TalentTree_Main.asset` 加节点 + 设 `prerequisiteIds` + 排 `treePosition`；技能解锁走 `UpgradeDefinition.requiredTalentIds`，不在面板里。

---

## 4A. 分支结构与互锁表达能力

### 4A.1 版式问题与逻辑问题要分开

**版式是渲染问题，互锁是数据模型问题——版式解决不了互锁。**

面板的「自由坐标平面 + 按边画连线」这个**渲染机制**能画任意拓扑（含多根、多叉），**不构成障碍**。
但当前实现缺三样：**分支归属字段**（§2.1）、**滚动容器**、**可见的连线**（见上）。

### 4A.2 现有模型能表达什么

| 想要的互锁 | 现有模型 | 结论 |
|---|---|---|
| **多个起始分支** | `prerequisiteIds = []` = 无前置 = 独立根 | ✅ **已支持** |
| **AND 型跨枝前置**（A 枝节点需 B 枝节点才能买） | `GetPrerequisites` 返回 List；`IsAvailable` / `TryUnlock` 要求**全部**已解锁 | ✅ **已支持，零代码** |
| **OR 型**（满足 A 或 B 任一即可） | `prerequisiteIds` 是 AND；`requiredTalentIds` 也是 AND | ❌ 不支持 |
| **互斥 / 专精**（选了蓄力流就不能选位移流） | 无相关字段 | ❌ 不支持 |
| **分支计数**（买满 A 枝 N 个才开 B 枝） | 无 | ❌ 不支持 |
| **分支代价**（买 A 枝让 B 枝变贵/失效） | 无 | ❌ 不支持 |

### 4A.3 我们的设计实际只需要 AND 型

§4.2 的节点表目前**没有跨枝关系**，全是同枝链式——「分支互锁」是本轮新提的要求。

| 类型 | 建议 | 理由 |
|---|---|---|
| **AND 型跨枝前置** | ✅ **做**（零代码） | 最自然的形态就是把「隔壁能力」接进来：把「巧思(+1主动槽)」设为流派深节点的前置 → 玩家必须先扩槽再走深层 → **直接强化「降配 3+3」的痛感**。填两个 id 即可，面板会画两条线 |
| **互斥 / 专精** | ❌ **不做** | 需求方向是「给玩家选择空间」，互斥是把选择空间砍掉。且**铜钱本身就是天然互斥**（总量有限），不需要写代码 |
| **OR 型 / 分支计数** | ❌ **暂不做** | 当前设计没有必须用它们表达的东西。不为「将来可能用」而现在扩模型 |
| **信息型互锁**（关卡需求 ↔ 解锁度对照） | 🟡 **单独评估，第二阶段** | 关卡设计天然产生这个需求：N1 基础 → E0 出现 1012（霸体，必须 C2）→ N3 出现 104（必须 C4 + 招架）。玩家应该在**天赋页就能看到「下一关要破霸体，你还没解锁 C2」**，而不是进去挨打才知道缺什么。但需新增「关卡 → 需要能力」映射，成本不低，**不与本轮互锁混做** |

### 4A.4 建议版式（不推翻现有框架）

| # | 改动 | 解决什么 | 归属 |
|---|---|---|---|
| 1 | 修 `PlaceConnector`：`sizeDelta = (length, connectorThickness)`，或改 `Image.type = Sliced` + 九宫格 | 连线可见 | 面板侧 |
| 2 | **两级导航**：顶栏大页面 Tab（能力 / 招式 / 技能）+ 页内分支分组 | 溢出屏幕 + 分支归属 | 面板侧 |
| 3 | **`TalentNodeConfig` 加 `pageId` + `branchId`**；`TalentTreeConfig` 加 `branches[]` 元数据 | 面板能按页面/分支组织；也是将来做关卡对照的数据基础 | **数据层（我方）** |
| 4 | **跨页 / 跨枝前置用文字徽标**（"需要先解锁：巧思"）而不是跨页连线 | 跨页画不了连线 | 面板侧 |

**替代方案**（若不做 Tab）：分支容器 + 滚动——每枝一个带标题的容器，节点在枝内自动纵向排位，容器整体放在可滚动的 ScrollRect 里。
但 **Tab 方案改动更小，且天然解决「玩家一眼看到有哪几条路可走」**——后者正是「多个起始分支」要给玩家的信息。

> `branchId` 是纯字符串，**不影响存档**（存档存 nodeId 字符串，不是节点下标）。`hidden` 字段已经开了「给 `TalentNodeConfig` 加字段」的先例。

### 4A.5 天赋页结构：两级（已确认）

```
天赋页
├─ 【能力页】能力 / 机制开关        ← 页内多个起始分支
├─ 【招式页】招式解锁              ← 页内多个起始分支
└─ 【技能页】流派技能              ← 页内 3 个起始分支 = 3 种流派
```

**关键点（用户 2026-11 明确）**：

- 大页面分三类：**能力 / 招式 / 技能**。
- **每个大页面自身有多个起始分支**（不是一条从头点到尾的链）。
- **技能页的分支按流派划分**：蓄力流 / 位移流 / 火系协同 = **3 条起始分支**。
  → **不是十几个技能堆在一条树上由头点到尾**。每个流派是一个独立起点，流派内的技能是该分支的子节点。

→ 因此数据模型需要**两级**标识：

| 字段 | 位置 | 取值示例 | 用途 |
|---|---|---|---|
| `pageId` | `TalentNodeConfig` | `ability` / `move` / `skill` | 顶栏 Tab |
| `branchId` | `TalentNodeConfig` | `flow_charge` / `flow_displace` / `flow_fire` / `combat_chain` / `body` / `fortune` | 页内分支分组、标题、分支内自动排位 |
| `branches[]` | `TalentTreeConfig` | 每项：branchId / displayName / description / icon / order | 分支元数据（标题、图标、顺序），避免面板侧硬编码 |

**分支根节点的判定**：同一 `branchId` 内没有同枝前置的节点 = 该分支的起始分支点。→ **不需要额外的 `isRoot` 字段**。

**跨页前置**：允许（例如「霜寒·重击解冻」是能力页节点，但前置是招式页的「指向突刺 C2」）。
→ 面板侧表现为：当前页看不到前置节点，需用**文字徽标**表达（"需要先解锁：指向突刺"）。

### 4A.6 分支内容划分（2026-11 已确认）

**两级结构**：大页面（能力 / 招式 / 技能）→ 页内起始分支 → 节点。

| 页面 | 分支 | 节点 |
|---|---|---|
| **招式页** | **点线**（起手为点） | 指向突刺 C2 (100) → 枪突收尾 C4 (180)〔同一条链〕 |
| | **划线**（起手为划） | 横扫位移 (150)〔Slash；后续要改 Slash 并为 Slash 添加更多衍生〕 |
| **能力页** | **战斗向** | 破阵 (200) → 追击 (250) → 运筹 (300) |
| | **生存向** | 铁血 生命+30 (100) → 连击精通 阈值↓ (150) → 不屈 (400) |
| | **经济向** | 聚财 铜钱+30% (120) → 巧思 主动槽+1 (180) → 博学 被动槽+1 (180) |
| **技能页** | **蓄力流** | 蓄力入门 (80) → 铁壁 (120) / 震荡 (160) / 冲击波 (200) |
| | **位移流** | 位移入门 (80) → 专注 (120) / 海浪 (160) / 旋风 (200) / **染病** (140) |
| | **火系** | 蛇形喷射 (130) → 火龙舌 (180) |

**已定归属**

| 项 | 归属 |
|---|---|
| 染病（触发键=戳击命中） | **归入位移流** |
| 通用技能：疾风 / 智慧 / 地刺 / 箭雨 | **常驻池，不进天赋页**（默认就在局内三选一池里） |
| 重击解冻（`ice_thaw`） | **从天赋树移出 → 改为「秘籍」掉落**（见 §4A.7） |

### 4A.7 秘籍系统：局内掉落 → 局外永久解锁（2026-11 新增需求）

> 需求：「重击解冻这类**技能提升**加入道具解锁——单次掉落获得后自动解锁，类似秘籍。
> 可能在敌人身上随机或固定掉落，也可能在场景破坏物中随机或固定掉落。」

**现有可复用的**

| 载体 | 能力 |
|---|---|
| `Assets/Scripts/Prop/BreakableProp.cs` | 已有 **SupplyLayer.InRun / OutOfRun** 分层机制；局外层奖励直接进存档金币（`outOfRunCoinReward`）→ **已有「奖励进存档」的先例** |
| `ISupplyPropTarget` + `MoveGesture.supplyTarget` | 手势点中物件的拾取通道已有（点中补给物件时同一击同时命中） |
| 存档分片（`TalentSaveSection` / `PropSaveSection` / `SaveManager`） | 新增一个「已获得秘籍」列表片即可，与天赋存档同构 |
| `KillRewardManager` + `KillRewardType` | 已有 Coin / Heal / RandomUpgrade 三种 —— **没有「解锁」类型** |

**现有掉落 / 奖励通道实测（2026-11）**

| 通道 | 位置 | 现状 |
|---|---|---|
| **敌人击杀掉道具** | `EnemyManager.TryDropItem()` + `DropItemPoolConfig`（`baseDropChance` + 加权 `pool`） | ✅ **已存在**。但开头有 **V2 短路**：`if (ActiveSkillInventory.Instance.UsesActiveSkills) return;` → **V2 规则下敌人不掉任何道具**。掉落率受「捡漏 `item_drop_rate`」加成 |
| **敌人掉铜钱** | `Enemy.coinReward`（10/只）→ `StageRunFlow` → `PlayerState.AddCoins` | ✅ 已在跑 |
| **击杀里程碑** | `KillRewardManager` + `StageConfig.killMilestones` + `KillRewardType`（Coin / Heal / RandomUpgrade） | ✅ 已在跑；**没有「解锁」类型** |
| **场景破坏物** | `BreakableProp`：`SupplyLayer.InRun / OutOfRun` 分层；局外层奖励直接进存档金币 | ✅ 已在跑 |
| **拾取通道** | `ISupplyPropTarget` + `MoveGesture.supplyTarget`（点中物件时同一击同时命中） | ✅ 已在跑 |
| **局内道具栏** | `ItemInventory` / `ItemEntry`（有槽位上限、满时弹弃置窗、局末清空） | ✅ 已在跑 —— **但秘籍不走这里**（秘籍是永久解锁，不能占局内槽位） |

> ⚠️ 更正：2026-11 早前的一份分析曾断言「敌人身上掉落全项目零基础」——**这是错的**，那次 grep 漏掉了 `DropItemPoolConfig` / `dropItemPoolConfig` 的命名。实测已有完整的敌人掉落通道，只是被 V2 短路禁用。

**需要新建 / 修改的**

| # | 内容 | 成本 |
|---|---|---|
| 1 | **秘籍定义资产**：`ManualDefinition : UpgradeDefinition`（继承 → 可直接放进现有加权掉落池，复用 `WeightedDropItem` 结构）；含 id / 名称 / 说明 / 解锁的 flag 或 moveConfig / 图标 | 小 |
| 2 | **秘籍存档片**：`ManualSaveSection`（已获得秘籍 id 列表），与 `TalentSaveSection` 同构；面板侧显示为「已获得」不可再买 | 小 |
| 3 | **掉落分流**：`EnemyManager.TryDropItem` 里按类型分派——`ManualDefinition` → 写存档解锁；普通 `UpgradeDefinition(category=Item)` → 进 `ItemInventory`。**同时需处理 V2 短路**（V2 下只掉秘籍、不掉普通道具） | 中 |
| 4 | **破坏物掉秘籍**：`BreakableProp` 加一个秘籍字段 + 概率，复用现有分层 / 拾取 / 渐变销毁流程 | 小 |
| 5 | **掉落物外观**：地上那个可点的秘籍物件（prefab + 掉落表现） | 中 |

**⚠️ 必须写死的边界（否则玩家会问「能掉的为什么要花钱买」）**

| 获取方式 | 只给什么 | 理由 |
|---|---|---|
| **秘籍掉落**（局内打怪 / 打破坏物） | **技巧类**：单次获得即永久生效、无等级、无铜钱成本（如重击解冻） | 「秘籍」语义 = 机遇型、不可复制 |
| **铜钱购买**（天赋树） | **流派 / 能力类**：需要等级、需要前置、需要路线承诺（如 C2 / C4 / 流派技能 / 槽位 / 铜钱获取） | 可规划、可积累 |

两者**不重叠**。

### 4A.8 技能等级上限作为解锁内容（2026-11 新增需求）

> 需求：「当前技能最多升到 5 级，局外能力天赋解锁更高等级可升到 10 级。」

**上限判定的两处落点**（都要改）

| 位置 | 现有代码 |
|---|---|
| `Assets/Scripts/Core/UpgradePoolSampler.cs` `CollectUnified` | `if (currentLevel >= wu.upgrade.maxLevel) continue;` |
| `Assets/Scripts/Core/UpgradeEffectManager.Apply.cs` `ApplyUpgrade` | `if (newNumericLevel > def.maxLevel) { …跳过… }` |

→ 需要**运行时上限覆盖**：`effectiveMaxLevel = max(资产 maxLevel, 天赋解锁的 cap)`，由能力页天赋 flag 驱动。

**🔴 数据现状（实测）**：池内 12 个技能全是 5 级，**6–10 级的数值不存在**。

git 历史扫描结果（`git log` 逐提交比 `maxLevel` 与数值行数）：

| 技能 | 当前 | 历史最大 maxLevel | 6–10 级数值可否找回 |
|---|---|---|---|
| 铁壁 `charge_damage_reduction` | 5 | **10**（11 行） | ✅ 可完整找回（commit `0655bf3`） |
| 地刺 `spike_trap` | 5 | **10**（11 行） | ✅ 可完整找回（commit `399a15b`） |
| 疾风 `attack_speed` | 5 | 10（仅 6 行） | ⚠️ 不全 |
| 专注 `cooldown_reduction` | 5 | 5 | ❌ 历史里从没有 10 级版本 |
| 智慧 `exp_boost` | 5 | 5 | ❌ 同上 |
| 蛇形喷射 / 震荡 | 5 | 10 | 🟡 结构体行数需单独审计 |
| 拔苗助长 | 2 | 2 | —（本来就是 2 级） |

`design/numerical-balance-draft.md` §2.2 已预告过这件事：「原 10 级技能：关键节点 Lv1/3/5 对应原 Lv2/6/10，中间 Lv2/4 做平滑过渡」——压缩到 5 级时被丢弃的 5 个等级就是这批数据。

**✅ 定调（2026-11 用户）：「解锁上限」与「实际有无这么高的等级」是两码事。**

→ 不为"凑满 10 级"而先做数值。**先做通用的上限解锁机制**，技能能升多高由它自己的数据决定。

**实现要点（三个上限分开算）**

```
资产上限   def.maxLevel                 // 例：5
天赋解锁   unlockedCap（能力页天赋 flag）  // 例：10
数据上限   availableRows(def)           // 该技能实际有几行等级数值

实际可升上限 = min( max(资产上限, 天赋解锁), 数据上限 )
```

**为什么必须再夹一层「数据上限」**（否则会静默失效）：

| 读取入口 | 越界行为 | 后果 |
|---|---|---|
| `UpgradeDefinition.GetNumericConfig(level)` | `numericLevels.Count >= level` 不成立 → 回退到 `floatValue * level` 线性外推 | 数值不报错但**是假的** |
| `ActiveSkillDefinition.GetCooldown(level)` | `Mathf.Clamp` 到最后一行 | 静默封顶 |
| `UpgradeDefinition.GetTriggerInterval(level)` | 越界返回 **-1** | `TimedPassiveModule`：`if (interval <= 0f)` → **警告 + 该被动不注册 = 直接失效** ⚠️ |
| `UpgradeDefinition.GetTriggerThreshold(level)` | 越界返回 **-1** | `PassiveTriggerModule`：`if (threshold <= 0)` → 兜底用 `intValue` 或 4 |

→ **在夹了「数据上限」之后，永远不会走到越界分支**，不必改这四个读取函数。

---

### 2.2 招式与解锁链路

`Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset`（roots 只有 `Tap → Zhangfei_Jab1`）

```
中立态 Tap → Zhangfei_Jab1 → Jab2 → Jab3 ──(蓄力上划)──> Zhangfei_LaunchFinisher  = C4
                        └─(Hold 蓄力, 窗口 0.55~1)──> Zhangfei_StabR2            = C2
```

#### 招式数值 [实测]

| 招式 | 资产 | 伤害 | 架势 | isHeavy | 排数 | pushBackRows | 其他 |
|---|---|---|---|---|---|---|---|
| Stab（点） | `Zhangfei_Stab` | 20 | 0 | — | 1 | 0 | cd 0.3 / action 0.45 |
| **Slash**（横滑） | `Zhangfei_Slash` | 20 | 0 | — | 1（MultiColumn） | **0** | cd 0.65 / action 0.5 |
| Launch（竖滑蓄力） | `Zhangfei_Launch` | 2 | 50 | ✅ | 1 | 0 | |
| Parry（防御键） | `Zhangfei_Parry` | 0 | 30 | — | 1 | 0 | `parryGestureEnabled=false`，手势招架已下线 |
| **stabC2** | `Zhangfei_StabR2` | **35** | **30** | ✅ | **2** | **1** | cd 0.3 / action 0.66 / 蓄力指向 |
| **stabC4** | `Zhangfei_LaunchFinisher` | **60** | **80** | ✅ | **3** | 0 | cd 0.3 / action 0.6 / `releaseParryEnabled=true` |
| 枪突 1/2/3 | `Zhangfei_Jab1/2/3` | 30/33/37 | 0 | — | 1 | 0 | 受击方向 Front→Left→Right 三段各异 |

**对照用户描述**：

- **stabC2 = 击退 + 打断霸体** ✅ 完全吻合（重攻击 + 击退 1 排 + 架势 30）
- **stabC4 = 击飞 + 大量伤害 + 追击连段** ✅ 完全吻合（挑飞 + 60 伤 + 3 排 + 附带格挡）
- **Slash = 横向位移 + 群特攻** 🟡 **群**成立（MultiColumn 全列）；**横向位移当前是死的** 👇

#### Slash 横向位移为什么是死的

```
AttackSystem.ApplySlashDirectionalPush
  → UpgradeEffectManager.GetDirectionalPushStep()
    → _directionalPushStep 字段
```
该字段**全项目只有 `DebugSetDirectionalPush`（调试方法）会写**，没有任何升级或天赋驱动它 → `step <= 0` → 直接 return。

引擎通道本身是通的（`ColumnManager.ApplyDirectionalPush` → `DisplacementMover.Push.cs:437`），只差一个数据源。

> 另注：`charge-build-three-options.md` §三 描述的 `ExecuteSweep` 管线里第 4 步写的是 `[如有] 位移 → ApplySlashDirectionalPush` —— 说明这条管线设计时就预期它由某个升级驱动，只是那个升级从未做出来。

---

### 2.3 敌人机制（6 种可用敌人）

数据来自 `Assets/Resources/EnemyPrefabs/*.prefab` 的 `Enemy` 组件 [实测]。

| 敌人 | HP | 伤害 | 攻速 | 架势 | 眩晕 | 射程 | 金币 | 经验 | 核心机制 | 正确对策 |
|---|---|---|---|---|---|---|---|---|---|---|
| **1011** 持剑杂兵 | 100 | 3 | 0.4 | 50 | 5s | 1 | 10 | 80 | 普通攻击（`isCAttack=false`），命中帧 t=2.4，招架窗口 0.4 | 任意攻击打断 |
| **1012** 刀兵 | 100 | 3 | **0.28** | 50 | 5s | 1 | 10 | 80 | **C 技（`isCAttack=true`）= 霸体前摇**，命中帧 t=2.0，招架窗口 0.5 | **重攻击**打断 / 招架在出伤窗口格开 |
| **105** 弓箭手 | 100 | 4 | 0.25 | 40 | 2s | 3 | 10 | 50 | 远程，落点随机散布 ±1.2 | 招架弹反 / 位移打断 |
| **109** 骑兵 | 400 | 6 | 0.4 | 50 | 5s | 1 | 10 | 100 | `CavalryEnemy` 冲锋循环；**骑乘期免疫眩晕与位移、招架无效**；伤害 = 8×(发起排+1) | **蓄力/重攻击**（`interruptsCavalryCharge`）打断，本次不结算伤害 |
| **1021** 盾卫 | 150 | 10 | 0.2 | 50 | 1.5s | 1 | 10 | 50 | `ShieldGuardEnemy`：盾侧/正面格挡（伤害 −10，下限 1）；破盾需「反侧 + 重攻击」 | 反侧重攻击破盾 → 掉盾 → 胆怯（受伤 ×1.5、不再攻击） |
| **104** BOSS | 4500 | 10 | 0.2 | 100 | 5s | 5 | 10 | 400 | 占 5 排；**3 阶段**（100%/70%/30%，后两阶段 `superArmor`）；带 QTE | 重攻击拆前摇 / 招架格开 QTE |

**对比 BOSS 变体** [实测]：

| prefab | HP | 阶段 | 说明 |
|---|---|---|---|
| `Enemy_104` | 4500 | **3**（1.0 / 0.7 / 0.3，Phase2、3 霸体） | 正式 BOSS |
| `Enemy_108` | 500 | **2**（1.0 / 0.7） | 弱化版 BOSS |
| `Enemy_107` | 500 | 1 | 单阶段 |

→ **要做"两阶段 104"需新建 `BossPhaseData_104_Phase1/2.asset`**（复用现有 Phase1+Phase2 参数，去掉 Phase3），在 `Enemy_104.prefab` 的 `bossPhases` 列表里替换。不能直接用 `Enemy_108`（HP 只有 500，是弱化版）。

#### 1021 破盾的概率问题（已记录于 `design/gameplay-pillars.md` §4.2）

破盾条件 = `受击方向 == Right` **且** `isHeavy`；而所有重攻击的 `hitReactionDirection` 都是 `Random` → **蓄力攻击砸盾卫只有约 1/3 概率摸到反侧并破盾**。

→ **C2 是目前破盾的唯一可靠手段**（蓄力指向可以稳定选中列，但仍随机方向；这是独立于天赋之外的缺口）。

---

### 2.4 关卡与路线

**拓扑** [实测 `Changbanpo_ScrollRoute.asset`]：

```
N1 → E0 → J1 → { 左: EV(无战斗) → N3 ｜ 右: N2 }
```
route 级 `clearCoinReward = 0`。

#### 正式关配置 [实测]

| 资产 | stageId | 波数 | 编队 | clearCoin | killMilestones |
|---|---|---|---|---|---|
| `Formal_N1_Battle` | 1101 | **1** ✅ | 3 排 × 1011 = 5 只 | 0 | 未配 |
| `Formal_E0_Battle` | 1102 | **2** ⚠️（要 3） | w0: 1011/1021/1011/1011；w1: 109/1021/1011/1011 | 0 | 未配 |
| `Formal_N2_Battle` | 1103 | 3 | w2 = boss 波，含 104 | 0 | 未配 |
| `Formal_N3_Battle` | 1104 | **3** ⚠️（要 5） | w2 = boss 波，含 104 | 0 | 未配 |

#### 教学关 [实测]

| 资产 | stageId | 波 | 编队 | 倍率 |
|---|---|---|---|---|
| `Tutorial_N1_Battle` | 9001 | 2 | 1 只 1011 / 3 只 1011 | dmg×0.3, exp×0 |
| `Tutorial_E0_Battle` | 9002 | 2 | 1 只 1012 / 1 只 1012 | hp×1.5, dmg×0.3, exp×3 / ×1 |

> `TutorialAttackGate` 只按 `AttackType` 白名单控制，**无法区分 C1 与 C2**（二者都是 `Stab`）。因为 C2 未被天赋锁住，教学关里按住即可打出 C2。

#### 节点级奖励字段是死字段

`FakeRouteNodeConfig.rewards[]`（`List<KillRewardEntry>`）字段存在，但**全项目无任何消费点**（`grep '\.rewards'` 在 `RouteFake/` 与 `Flow/` 下零命中）。

---

### 2.5 铜钱经济

```
局内收入：
  敌人 coinReward = 10/只  →  PlayerState.AddCoins()  →  局内 session coinCount
  击杀里程碑 StageConfig.killMilestones  →  正式关未配（Stage_1 配了 5/20/50 杀 → 30/50/100 铜钱）
  整关 StageConfig.clearCoinReward = 0
  路线级 FakeRouteStageConfig.clearCoinReward = 0
  节点级 rewards[]  →  死字段
局内升级：敛财 OnKillCoin（击杀 10%/级 概率掉 1 铜钱，maxLv 5）
局外支出：唯一出口 = TalentTreeState.TryUnlock 扣 coinCost
结算链路：StageController.SettleCoins() → SaveManager.SetCoins(存档)
```

**关键结论：局内铜钱与局外铜钱是同一个池子**（结算时合并）→ "满槽变钱"能把局内成长折算成局外解锁力，闭环天然成立。

**当前失衡**：一次 N1 通关 ≈ 5 × 10 = **50 铜钱**，买完全部天赋（1750）需要约 **35 次通关**。

---

### 2.6 局内成长与槽位

#### 池配置 [实测 `ActiveSkillPoolConfig.asset`]

| 稀有度 | 权重 | 条目 |
|---|---|---|
| Common | 0.60 | 专注 · 智慧 · 铁壁 · 地刺 · **火龙舌** · **染病** |
| Rare | 0.35 | 震荡 · **冲击波** · **箭雨** · 蛇形喷射 · 疾风 |
| Legendary | 0.05 | **旋风** · 拔苗助长 · **海浪** |

（加粗 = 主动技能，池内主动共 6 个）

#### 主动技能资产 8 个，池内只有 6 个

| 在池内 | 不在池内 |
|---|---|
| 火龙舌 `ActiveSkill_FireLine` · 染病 `ActiveSkill_Disease` · 冲击波 `ActiveSkill_ChargeAttackShockwave` · 箭雨 `ActiveSkill_ArrowRain` · 旋风 `ActiveSkill_Cyclone` · 海浪 `ActiveSkill_Wave` | **冰雾 `ActiveSkill_IceMist`**（且 `_grantDefaultSkills = 0`，Battle.scene 实测 → **游戏里根本没有冰雾**）· **烈焰喷射 `ActiveSkill_FireAOE`**（只有被动「蛇形喷射」在池里） |

#### 槽位现状

| | 上限来源 | 现值 |
|---|---|---|
| 主动技能槽 | `ActiveSkillInventory.Capacity` → `HeroConfig.itemSlotCount` | **4** |
| 道具槽 | `ItemInventory.Capacity` → `HeroConfig.itemSlotCount` | 4（**同一字段**） |
| 被动槽 | `UpgradeChoiceManager.CreateSampler` → `PassiveUpgradeCount >= 6` **硬编码** | **6** |
| UI | `FrontItemBar._itemSlots` = 4；`PassiveSkillRow._slots` = 4 + 溢出 "+N" | |

#### ⚠️ 被动槽口径与 UI 不一致

| | 口径 |
|---|---|
| **被动上限判定** | `UpgradeEffectManager.PassiveUpgradeCount` = `_appliedUpgrades.Count` → **含 Numeric**（专注/智慧/疾风/铁壁/地刺/拔苗助长…） |
| **被动排 UI 显示** | `PassiveSkillRow.IsPassive()` = 仅 `AttackPassive` + `TimedPassive` → **不含 Numeric** |

→ 玩家塞了 3 个数值被动，**被动排上一个图标都不显示**，但确实占了槽。**降配到 3 槽后这个矛盾会变成玩家可见的问题**（频繁撞顶却不知道为什么）。

#### 现行「槽位不足时三选一怎么弹出」= **根本没有弹出**

| 情况 | 现行行为 | 代码位置 |
|---|---|---|
| 被动满 6 | 新被动**直接从候选池剔除**，玩家看不到 | `UpgradePoolSampler.CollectRegular/CollectUnified`：`currentLevel == 0 && IsPassiveCapReached() → continue` |
| 主动满 4 | `CanAcquire` false → **同样从池里剔除** | `CollectUnified` → `CanAcquireActiveSkill` |
| 道具满 | **只有这一路会弹「弃置弹窗」**（`ItemDiscardPopup`） | `UpgradeEffectManager.Apply.cs:61-81` |

→ 现行是**「满了就藏起来」**，玩家不会知道自己被卡住了。要做的"满槽补偿"是**全新规则**。

#### 其他相关机制

- `UpgradePoolSampler.DrawByRarity`：候选不足 `choiceCount` 时**返回更少的卡片**（不补足）；`UpgradeChoicePopup.ShowChoices` 已支持隐藏多余卡片 → 1 卡 / 2 卡弹窗可正常工作。
- 空候选时 `UpgradeChoiceManager.ShowNextChoice`（第 258-265 行）→ 直接 `FinishAllChoices()` 跳过，**玩家什么也没拿到** → 这是"满槽→金币补偿"的插入点。
- `UpgradeDefinition.prerequisites`（`UpgradePrerequisite`：需要某升级 + 最低等级）**已实现但零使用** → 流派**内部**的组合解锁可用。
- `UpgradeDefinition.requiredTalentIds` **已实现但零使用** → 天赋解锁技能进池的现成通道。
- `TalentUpgradeRequirementProvider` 语义 = **AND**（列表里所有 id 都必须已解锁）。**不支持 OR**。
- 教学关支持 `forcedChoices`（固定三选一，不走随机池）。

---

### 2.7 流派体系

**主文档**：`design/in-run-build-archetypes.md`（V2 构筑主轴）
**数值配套**：`design/numerical-balance-draft.md`（§0.2 玩家构筑 / §2.3 流派平衡原则 / §3–5 逐级数值 / §7 Build 完全体）
**触发键视角**：`design/gameplay-pillars.md` §3.2–3.3
**蓄力线专项**：`design/charge-build-three-options.md`

#### 流派归属（覆盖池内 14 条）

| 流派 | 核心技能 | 辅助 | 共用触发键 | 定位 |
|---|---|---|---|---|
| **蓄力流** | 铁壁（减伤）· 震荡（蓄力命中波）· 冲击波（叠层主动） | 疾风 | **蓄力攻击** | 高爆发，把蓄力窗口从"代价"变成"资产" |
| **位移流** | 海浪（零伤击退）· 旋风（零伤浮空）· 专注（CD−） | 拔苗助长 · 疾风 | **主动技能 CD** | 高频控制/打断/削韧换安全输出 |
| **火系协同** | 蛇形喷射（挂灼烧）· 火龙舌（Lv3 起灼烧暴击 200%） | — | **灼烧状态** | **跨流派通用**，不单独成线 |
| **疾病流** | 染病（"下一次戳击"使目标染病） | — | **戳击命中** | 天然绑定枪突链（全是戳击） |
| **通用** | 疾风 · 智慧 · 地刺 · 箭雨 | — | — | 全流派可用 |

> 疾病流在 `gameplay-pillars` §3.2 被点名为独立触发键，但 `numerical-balance-draft` §0.2/§2.3 只列三线。**是否升格为第四条线待定**（倾向升格——它有独立触发键和专属载体"枪突链"，正好与要解锁的 C2/C4 咬合）。

#### ⚠️ `numerical-balance-draft.md` 的敌人谱系已过时

该文档写的是旧体系（101=16HP、103=30HP、105=8HP、106=16HP、Boss=500HP），**实测当前 prefab 是** 1011=100 / 1012=100 / 1021=150 / 105=100 / 109=400 / 104=4500。

→ **§3–§5 的技能数值表仍然有效**（资产实配来源）；**§1.2 / §1.3 / §9.x 的敌人与波次表不能拿来配关卡**，须用实测 prefab 数值重算。

---

## 3. 关键机制缺口清单

| # | 缺口 | 现成承载物 | 需要写什么 |
|---|---|---|---|
| 1 | 天赋解锁技能进池 | ✅ `requiredTalentIds` + Provider | **零代码**，只填资产 |
| 2 | 流派内组合解锁 | ✅ `prerequisites`（`UpgradePrerequisite`） | **零代码**，只填资产 |
| 3 | 招式解锁（C2） | ✅ `moveConfig` | 回滚 `d278593` 对 C2 的"直接开放"处理：恢复引用、取消 hidden、理顺前置 |
| 4 | Slash 横向位移 | ⚠️ 引擎通道通（`ColumnManager.ApplyDirectionalPush`），**无数据源** | 加一个驱动 `_directionalPushStep` 的升级或 flag |
| 5 | 冰雾破冰顶开 → 技能升级 | ⚠️ 现在由 `talent_ice_break_push` flag 门控（`IceBreakSession.ApplyBreakPush`） | 删天赋节点；改判定来源；新建承载该效果的升级条目 |
| 6 | 生命上限提升 | ❌ 无 flag 消费点 | 新 flag + `PlayerState` 读 flag 改 `maxHealth` |
| 7 | 连击加伤天赋 | ✅ `ComboBuffConfig.triggers[]` 多档位 | 加档位或阈值下调；阈值可由 flag 控制 |
| 8 | 槽位上限提升 | ⚠️ 共用 `itemSlotCount` | 拆字段（见 §5） |
| 9 | 铜钱获取提升 | ❌ 无 flag 消费点 | 新 flag + 敌人/里程碑/通关奖励结算处乘系数 |
| 10 | 关卡发铜钱 | ✅ `killMilestones` / `clearCoinReward` 字段都在 | 纯配置 |
| 11 | 满槽→金币补偿 | ✅ 插入点已定位 | 改 `ShowNextChoice` 空候选分支 + toast |
| 12 | 天赋树多前置环检测 | ✅ `Validate()` 存在 | 修 `GetPrerequisites(cur)[0]` 只查一条边 |
| 13 | 被动排 UI 口径 | ⚠️ 见 §2.6 | 扩展显示 Numeric，或改口径 |

---

## 4. 天赋树方案

### 4.1 结构原则

1. **多分支开端**：每条枝的根节点 `prerequisiteIds: []`（显式空 = 无前置），玩家可以只点一枝
2. **流派分线**：每条流派枝 = 根节点（开线）+ N 个技能解锁节点
3. **技能解锁用「节点 → 资产」单向引用**：技能节点的 `flagId` 作凭证，技能的 `requiredTalentIds` 填该节点的 nodeId
4. **「分支根节点」技巧**（绕开 AND 语义）：根节点是"开线开关"；具体技能的 `requiredTalentIds` 指向**它自己那个节点**而不是根 → 玩家逐个解锁，根节点只负责把这条枝接进树

### 4.2 节点表

| 枝 | 节点 | nodeId | 载荷 | 价 | 前置 | 作用 |
|---|---|---|---|---|---|---|
| **战技** | 指向突刺 | `talent_unlock_c2` | `moveConfig = Zhangfei_StabR2` | 100 | — | **修好引用** + 取消 hidden |
| | 横扫位移 | `talent_unlock_slash_push` | flag `slash_push` | 150 | — | 新增：驱动 Slash 横向位移 |
| | 重击解冻 | `talent_ice_thaw` | flag `ice_thaw` | 120 | — | 保留 |
| | 枪突收尾 | `talent_unlock_c4` | `moveConfig = Zhangfei_LaunchFinisher` | 180 | `talent_unlock_c2` | **解绑冰系前置** |
| **蓄力** | 蓄力入门 | `talent_flow_charge` | flag `flow_charge` | 80 | — | 开线 |
| | 铁壁 | `talent_skill_ironwall` | flag `unlock_ironwall` | 120 | `talent_flow_charge` | 铁壁进池 |
| | 震荡 | `talent_skill_shockwave` | flag `unlock_shockwave` | 160 | `talent_flow_charge` | 震荡进池 |
| | 冲击波 | `talent_skill_charge_wave` | flag `unlock_charge_wave` | 200 | `talent_flow_charge` | 冲击波进池 |
| **位移** | 位移入门 | `talent_flow_displace` | flag `flow_displace` | 80 | — | 开线 |
| | 专注 | `talent_skill_focus` | flag `unlock_focus` | 120 | `talent_flow_displace` | 专注进池 |
| | 海浪 | `talent_skill_wave` | flag `unlock_wave` | 160 | `talent_flow_displace` | 海浪进池 |
| | 旋风 | `talent_skill_cyclone` | flag `unlock_cyclone` | 200 | `talent_flow_displace` | 旋风进池 |
| **火系** | 蛇形喷射 | `talent_skill_snake` | flag `unlock_snake` | 130 | — | 蛇形进池 |
| | 火龙舌 | `talent_skill_fireline` | flag `unlock_fireline` | 180 | `talent_skill_snake` | 火龙舌进池 |
| **疾病** | 染病 | `talent_skill_disease` | flag `unlock_disease` | 140 | — | 染病进池 |
| **体魄** | 铁血 | `talent_vitality` | flag `vitality` | 100 | — | 生命上限 +30 |
| | 连击精通 | `talent_combo` | flag `combo_mastery` | 150 | — | 连击阈值 20→15 |
| | 巧思 | `talent_slot_active` | flag `slot_active` | 180 | — | **主动槽 +1** |
| | 博学 | `talent_slot_passive` | flag `slot_passive` | 180 | — | **被动槽 +1** |
| **财货** | 聚财 | `talent_coin_gain` | flag `coin_gain` | 120 | — | 铜钱获取 +30% |

**合计 2,850 铜钱** → 目标单次完整通关 600–800 铜钱 → **约 4 次通关点满**。

### 4.3 旧的 4 个空壳节点

| 节点 | 建议 | 理由 |
|---|---|---|
| `talent_break_formation` 破阵 | **实现**（所有攻击 +1 架势伤害） | 便宜且好用，是"机制开关"不是数值 |
| `talent_pursuit` 追击 | **实现**（击杀后攻击 CD −0.5s） | 蓄力流 / 位移流都吃这个 |
| `talent_strategy` 运筹 | **实现**（3 选 1 → 4 选 1） | `in-game-growth-system.md` §3.12 已列扩展接口；`UpgradeChoicePopup` + 自适应布局已支持 3/4/5 |
| `talent_undying` 不屈 | **实现**（血量 <30% 触发 2s 无敌，每局 1 次） | 机制开关 |
| `talent_ice_break_push` 破冰冲击 | **删除**，效果移到冰雾的技能升级侧 | 用户 2026-11 决策 |

### 4.4 排版

`treePosition` 口径 = 1080×1920，原点面板中心，+Y 向上。**当前所有节点挤在 x ∈ [-56, 56] 的一条竖线上**，多分支必须重排。

> 具体坐标待与另一边的天赋页改造对齐后再定（见 §9）。

---

## 5. 槽位方案（降配 3+3 + 渐进解锁）

### 5.1 目标

```
基础：3 主动 + 3 被动
天赋：巧思 +1 主动、博学 +1 被动 → 渐进到 4+4 / 5+5
```

### 5.2 实现改动点

| # | 改动 | 位置 | 风险 |
|---|---|---|---|
| 1 | `HeroConfig` 拆字段：`activeSkillSlotCount` / `passiveSlotCount`，`itemSlotCount` 留给道具 | `Assets/Scripts/Data/HeroConfig.cs` | **序列化**：新 int 字段在老资产里是 0 → 必须同时改 `Hero_Zhangfei.asset` 填 3，且读取处 `Mathf.Max(1, v)` 兜底 |
| 2 | `ActiveSkillInventory.Capacity` 改读新字段 | `Core/ActiveSkillInventory.cs:56` | 低 |
| 3 | 被动上限 6 硬编码 → 读配置 + 天赋 flag 加成 | `Core/UpgradeChoiceManager.cs:98` | 低 |
| 4 | 被动排 UI 口径对齐（见 §2.6） | `UI/PassiveSkillRow.cs` | 中 |
| 5 | UI 槽位数：`FrontItemBar._itemSlots`=4、`PassiveSkillRow._slots`=4 | `Assets/Scenes/Battle.scene` | 若天赋能加到 5+，槽位要补 |

### 5.3 必须先解决的设计后果

3 主动 + 3 被动 = **总共 6 格**，而池里 14 条里有 8 条是非主动非道具（占被动槽）。

→ 降配到 3 被动后，池里 **16 个"非主动条目"抢 3 格**（8 Numeric + 4 AttackPassive + 4 TimedPassive）
→ 玩家几乎必然很快撞顶，之后每次升级只能"升级已有条目"或（按新规则）**全部变成金币补偿**

这其实是个**很强的流派承诺机制**（拿了铁壁+震荡就锁死蓄力流），但必须配套：

- 池子用 `requiredTalentIds` 收窄，让玩家只看到自己解锁的流派 → 候选数变少，"满槽"更容易触发
- 建议把**通用**条目（疾风 / 智慧 / 地刺）设为**不占被动槽**的"常驻词条"，把 3 个被动槽留给真正的流派机制

---

## 6. 满槽 → 金币补偿规则

### 6.1 触发点（已定位）

`Assets/Scripts/Core/UpgradeChoiceManager.cs:258-265`

```csharp
if (_currentChoices == null || _currentChoices.Count == 0)
{
    if (_pendingQueue.Count > 0 || ...) ShowNextChoice();
    else FinishAllChoices();      // ← 现在：直接跳过，玩家什么也没拿到
    return;
}
```

### 6.2 规则草案

| 项 | 建议 |
|---|---|
| 触发条件 | `CollectEligible()` 返回 **0 候选**（所有条目满级或被槽位排除） |
| 金额 | `补偿 = 30 + 5 × 当前等级` 铜钱（Lv10 → 80，Lv30 → 180）。对齐一次升级 ≈ 200 XP ≈ 6 只 101 的经验量 |
| 发放去向 | `PlayerState.AddCoins(n)` → 局内 session → `StageController.SettleCoins()` → 存档 → 天赋树可花 |
| UI 反馈 | toast：「技能位已满，改为获得 X 铜钱」。`PlayerState.OnCoinGained` 事件可挂 |
| 部分可用 | 候选只剩 1–2 条时**仍正常弹窗**（`UpgradeChoicePopup.ShowChoices` 已支持隐藏多余卡片） |

### 6.3 设计收益

"满槽变钱"把**局内成长**（技能槽）折算成**局外资源**（铜钱 → 天赋），正是"打通局内外流程"要的效果，并给玩家一个主动取舍：继续占槽拿流派强度，还是提前转钱换局外解锁力。

---

## 7. 关卡配置方案

> 敌人数值以实测 prefab 为准（`numerical-balance-draft` 的敌人表已过时，见 §2.7）。倍率待定。

### 7.1 设计原则

**每一关只考"玩家当前解锁能力能解的机制"；下一关的机制必须先在上一关见过。** 难度增长靠**逼玩家买对的天赋**，不靠堆血量。

### 7.2 配方骨架

| 节点 | 波数 | 敌人配方 | 考什么 | 玩家需要 |
|---|---|---|---|---|
| **N1** | 1 | 4×1011 | 手感 + "击杀 = 铜钱"认知 | 基础招式 |
| **E0** | 3 | ① 3×1011 ② 1×1012 + 1×1021 + 1×1011 ③ 1×109 + 2×1012 | 霸体前摇 → 盾卫 → 骑兵 | **C2**（重攻击）+ 招架 |
| **N3** | 5 | ① 4×1011 ② 2×1012 + 2×1012 ③ 2×105 + 1×109 ④ 3×1021 + 2×105 ⑤ **104 两阶段** | 远程 → 混编 → **BOSS 毕业考** | **C2 + C4** + 招架格 QTE |

### 7.3 流派 / 招式与关卡的咬合

| 关卡元素 | 逼玩家做什么 |
|---|---|
| E0 的 **1012**（C 技霸体前摇） | 普通攻击打不动 → 必须用 **C2** 重攻击 → 驱动"买 C2"的天赋决策 |
| E0/N3 的 **109**（骑兵） | 骑乘期免疫眩晕位移，只能靠 `interruptsCavalryCharge` 打断 → 再次强化 C2 必要性 |
| N3 的 **1021**（盾卫） | 破盾要「反侧 + 重攻击」，而重攻击受击方向 Random（~1/3 概率）→ **C2 是目前破盾的唯一可靠手段** |
| N3 的 **105**（随机远程） | 需要"位移 / 控制" → **位移流技能**（海浪 / 旋风）的局内价值 → 驱动"买位移枝天赋" |
| N3 的 **104** BOSS | 霸体 + QTE → 需要 **C4**（重攻击拆前摇）+ **招架**（格 QTE） |

### 7.4 关卡发铜钱（闭环的必要条件）

| 项 | 建议 |
|---|---|
| `StageConfig.killMilestones` | 每关配 3 档（如 5 / 15 / 30 杀 → 30 / 60 / 120 铜钱 + 回血） |
| `StageConfig.clearCoinReward` | N1 ≈ 60 / E0 ≈ 120 / N3 ≈ 200 |
| 节点 `rewards[]` | 要么实现消费（按节点发奖），要么先删字段（不留死字段） |

目标：单次完整通关 **600–800 铜钱**。

### 7.5 两条路径规则必须遵守

来自 `design/numerical-balance-draft.md` §0.5（仍有效）：

- **规则九**：Boss 敌人必须放在该波 `rows` 数组的**最后一个 RowConfig**
- **规则十**：远程敌人（105/106）必须部署在该波 `rows` 数组的**后 50%** 范围内

---

## 8. 落地顺序

```
① P0 修数据（不写代码，先让链路成立）
   - talent_unlock_c2 的 moveConfig 指向 Zhangfei_StabR2、取消 hidden
   - talent_unlock_c4 解绑冰系前置
   - 4 个空壳 flag 节点：实现 or 隐藏
   - talent_ice_break_push 删除（效果移到技能升级侧）
② P0 关卡发铜钱（让闭环有收入）
   - 正式关 clearCoinReward + killMilestones
③ P1 天赋树重排为多分支（纯数据 + 修环检测）
④ P1 技能解锁通道：批量填 requiredTalentIds + prerequisites
⑤ P1 关卡波次重配（N1 / E0 / N3）+ 新建 104 两阶段 BossPhaseData
⑥ P2 槽位规则（拆字段 + 满槽金币补偿 + 被动排 UI 口径）
⑦ P2 能力类天赋（生命 / 连击 / 槽位 / 钱币 / Slash 位移）
⑧ P3 冰雾 + 烈焰喷射归属决策与接入
```

---

## 9. 合并边界与待确认项

### 9.1 合并边界

本方案的天赋相关内容**只动数据层**：

- `Assets/Scripts/Data/TalentTreeConfig.cs`（仅修环检测）
- `Assets/ScriptableObjects/Talents/TalentTree_Main.asset`（节点与前置）
- 各 `UpgradeDefinition.requiredTalentIds` / `prerequisites`
- `Assets/Scripts/Core/UpgradeEffectManager.cs` / `PlayerState.cs`（新增 flag 消费点）

**不动** `Assets/Scripts/UI/TalentTreePanel.cs` 与 `Assets/Scenes/TalentTree.scene`（另一侧在做）。

#### 9.1.1 与另一侧的交叉项（必须协调，不能各做各的）

`d278593` 已经动过 `TalentTreePanel.cs`（加 `hidden` 处理），所以两边都在同一批文件上。以下 4 项**跨越边界**，需要和另一侧对齐后再动：

| # | 交叉项 | 归属 | 说明 |
|---|---|---|---|
| 1 | **C2 回滚**（恢复 `moveConfig` 指向 `Zhangfei_StabR2`、取消 `hidden`、理顺前置） | **数据层（我方）** | 只改 `TalentTree_Main.asset`，不碰面板代码。但会让 `talent_unlock_c2` 重新出现在面板上 → 另一侧的排版要给它留位置 |
| 2 | **滚动容器 / 分页** | **面板 + 场景（另一侧）** | 多分支 20 节点必然溢出 1080×1920。**这是多分支天赋树的硬前置**，不解决就白排版 |
| 3 | **隐藏节点要不要画连线** | **面板（另一侧）** | 当前隐藏节点不画连线 → 用隐藏节点当「天赋门」会出现孤立节点。若另一侧打算用隐藏节点做分支门 → 需要改成画暗色连线 |
| 4 | **节点 id 命名** | 双方 | 若另一侧重命名了 nodeId，存档（存 nodeId 字符串）会丢记录 → 必须走迁移（见 §9.2）。**我方新增节点一律沿用 `talent_*` 前缀** |
| 5 | **`TalentNodeConfig.branchId` 字段** | **数据层（我方）** | 面板按分支分页 / 分组需要它。纯字符串，不影响存档。我方加字段，另一侧按 `branchId` 分页 |
| 6 | **分支划分方案** | 我方定内容，另一方定表现 | 我方给 7 条枝的节点清单与归属；另一侧给 Tab 栏美术与交互 |

#### 9.1.2 建议的排版分工

| 我方提供 | 另一侧提供 |
|---|---|
| 节点清单（nodeId / 名称 / 描述 / 价 / 前置 / 载荷） | 美术稿与最终坐标（`treePosition`） |
| 分支划分方案（§4.2 节点表） | 滚动容器 / 分页实现 |
| 流派分线的进池配置（`requiredTalentIds`） | 节点图标（现在 5 个节点图标为空） |

### 9.2 存档兼容性风险

存档存的是 **nodeId 字符串**（`SaveData.unlockedTalents`）。若另一侧重命名了节点 id，老存档会丢失该节点的解锁记录。**重命名 nodeId 必须走迁移，不能直接改。**

### 9.3 待确认项

| # | 问题 | 倾向 |
|---|---|---|
| 1 | 疾病流是否升格为第四条独立线？ | 升格（触发键"戳击命中"绑 C2/C4 枪突链） |
| 2 | 通用数值条目（疾风/智慧/地刺）是否占被动槽？ | 不占（作常驻词条），把 3 槽留给流派机制 |
| 3 | 冰雾 / 烈焰喷射是否纳入解锁池？ | 待定（"冰冻技能的击退效果回归技能升级处"这条需求直接涉及冰雾） |
| 4 | 天赋页的页面/分支划分方式（另一侧改造后确定） | — |
| 5 | 节点 id 命名是否沿用 `talent_*` 前缀 | 必须沿用或提供迁移 |

---

## 10. 参考文件索引

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Data/TalentTreeConfig.cs` | 天赋树数据模型 + 校验 |
| `Assets/Scripts/Core/TalentTreeState.cs` | 解锁判定 + 存档读写 |
| `Assets/Scripts/Core/TalentUpgradeRequirementProvider.cs` | 天赋 → 技能入池（AND 语义） |
| `Assets/Scripts/Data/UpgradeDefinition.cs` | `requiredTalentIds` / `prerequisites` 字段 |
| `Assets/Scripts/Core/UpgradePoolSampler.cs` | 候选收集（槽位上限在这里） |
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 三选一流程（满槽补偿插入点） |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` | 升级应用 + 被动计数 + 数值/行为注册表 |
| `Assets/Scripts/Data/HeroConfig.cs` | 槽位 / 生命 / 天赋树引用 |
| `Assets/Scripts/Data/StageConfig.cs` | 波次 + 里程碑 + 通关奖励 |
| `Assets/Scripts/Data/FakeRouteNodeConfig.cs` | 节点（`rewards[]` 是死字段） |
| `Assets/Scripts/Player/Combat/IceBreakSession.cs` | 冰系破冰（`ice_thaw` / `ice_break_push` 消费点） |
| `Assets/Scripts/Player/Combat/AttackSystem.Utils.cs:206` | Slash 横向位移（无数据源） |
| `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs:330` | 招式天赋锁定判定 |
| `Assets/Scripts/Data/Save/` | 存档分片：`Progress` / `Prop` / `RunSnapshot` / `Supply` / `Talent` / `Tutorial` |
| `Assets/Scripts/Data/DropItemPoolConfig.cs` | 敌人击杀掉道具池（`baseDropChance` + 加权 `pool`） |
| `Assets/Scripts/Enemy/EnemyManager.cs:208` | `TryDropItem`（**V2 短路禁用**） |
| `Assets/Scripts/Prop/BreakableProp.cs` | 场景破坏物（`ISupplyPropTarget` / SupplyLayer 分层 / 局外奖励进存档） |
| `Assets/Scripts/Core/ExpGemManager.cs` + `ExpGem.cs` | **可见掉落物范式**：生成 → 飞向玩家 → 到达结算 |
| `Assets/Scripts/Core/HealthPotionManager.cs` | 复用 `gemPrefab` 生成药水（局内道具掉落范式） |
| `Assets/Scripts/Core/ComboManager.cs` | 连击计数 + 多档位触发（`ComboBuffConfig.triggers`） |
| `Assets/Scripts/Core/BuffManager.cs` + `StatModifier.cs` | 属性修正注册表 + Buff 生命周期 |
| `Assets/Scripts/Data/RouteCatalogData.cs` + `UI/StageSelectUI.cs` | 选关页 + 关卡解锁判定 |
| `design/in-run-build-archetypes.md` | 流派权威定义 |
| `design/numerical-balance-draft.md` | 技能数值 + 敌人波次规则（敌人表已过时） |
| `design/gameplay-pillars.md` | 五支柱 + 敌人反制表 |
| `design/attack-interrupt-system.md` | 霸体 / 打断 / 破势规则 |
| `design/in-game-growth-system.md` | 局内三选一 + §3.12 扩展接口 |
| `design/anti-ghost-reference.md` | 所有引用必须 Inspector 可追踪 |
| `design/immutable-constraints.md` | 位移 / 补齐不可变约束 |

---

## 11. 本周改动需求汇总 + 项目能力盘点

### 11.1 本周改动需求（R1–R12）

| # | 需求 | 归属 |
|---|---|---|
| **R1** | 天赋页改为**两级结构**：大页面（能力 / 招式 / 技能）× 页内多个起始分支 | 数据层我方 + 面板另一侧 |
| **R2** | 招式解锁：**slash / stabC2 / stabC4** 由天赋解锁（回滚 `d278593` 的「C2 直接开放」） | 数据层我方 |
| **R3** | 技能解锁**按流派分线**：蓄力流 / 位移流（含染病）/ 火系；通用技能（疾风 / 智慧 / 地刺 / 箭雨）**常驻池不进天赋页** | 数据层我方（纯资产） |
| **R4** | 能力解锁：生命上限 / 连击加伤 / 技能槽位（**基础 3 主动 3 被动 + 天赋渐进解锁**）/ 铜钱获取 | 数据层 + 新增消费点 |
| **R5** | 旧的 4 个空壳 flag 实现（破阵 / 追击 / 运筹 / 不屈） | 新增消费点 |
| **R6** | 「重击解冻」这类**技巧类**改为**秘籍掉落**（敌人 + 破坏物，两个来源都要） | 新建（大量复用） |
| **R7** | **技能等级上限**作为解锁内容（通用上限机制；技能实际能升多高由自己的数据决定） | 新增通用机制 |
| **R8** | **满槽 → 金币补偿**（不再「满了就藏起来」） | 改造 + 复用 |
| **R9** | 关卡配置：N1 固定 1 波 / E0 固定 3 波 / N3 固定 5 波、末波**两阶段 104** | 纯配置 + 新建 2 个 BossPhaseData |
| **R10** | 局内外铜钱闭环：正式关发铜钱（`clearCoinReward` + `killMilestones`） | 纯配置 |
| **R11** | 划线分支后续要给 Slash 加衍生（本期不定义具体内容） | 暂缓 |
| **R12** | 竖滑蓄力招（Launch / Pierce / Sweep）不进划线（未规划入池的招式本期不动） | 不做 |

### 11.2 盘点方法（避免再误判「项目没有」）

这次改用**按能力面逐个搜**而不是按关键词猜：

| 盘点面 | 怎么查的 |
|---|---|
| 存档 | `ls Assets/Scripts/Data/Save/` + 读 `SaveManager` 公开 API |
| 掉落 / 拾取 | `grep TryDropItem` 追调用点 + 读 `EnemyManager` 掉落段 + `DropItemPoolConfig` |
| 可见掉落物 | `grep Instantiate / Spawn` 在 `EnemyManager` + `ExpGemManager` / `HealthPotionManager` |
| 拾取通道 | `grep ISupplyPropTarget` 找全部实现类 |
| 天赋消费点 | `grep IsFlagUnlocked / TalentTreeState\.` 全项目 |
| 数值系统 | `grep class StatModifier` + `BuffManager` 公开 API |
| 扩展点 | `grep Register` 于 `UpgradeEffectManager` / `UpgradePoolSampler` / `BuffManager` |
| UI 设施 | `ls Assets/Scripts/UI/` 全清单 |
| 关卡解锁 | `RouteCatalogData` + `StageSelectUI.IsStageUnlocked` |

### 11.3 三处错判更正

| # | 曾经的错判 | 实测真相 |
|---|---|---|
| 1 | 「敌人身上掉落全项目零基础，要新建整套」 | **错**。`DropItemPoolConfig` + `EnemyManager.TryDropItem` 已完整（加权池 + 掉落率 + 捡漏加成），只是被 **V2 短路**禁用。之前那次 grep 只查了 `dropPrefab/lootPrefab/dropTable` 等词形，漏掉 `dropItemPoolConfig` |
| 2 | 「C2 的 `moveConfig` 引用丢了」 | **表述不准**。是提交 `d278593` **有意清空**（commit message 用途②明说「招式改为直接开放后把解锁节点藏起来」）。问题不是「丢失」而是**改得不干净**（孤儿节点 + 链剪断 + 坐标重叠） |
| 3 | 「`ice_thaw` / `ice_break_push` 的 flag 有代码消费」 | **不准**。它们用的是 `TalentTreeState.IsUnlocked(nodeId)`（**节点解锁态**）；`IsFlagUnlocked` 全项目**零调用** → **`flagId` 字段本身是零消费者的装饰** |

### 11.4 已有 / 缺失 / 复用 vs 新增 矩阵

| 需求 | ✅ 已有（可直接用） | ❌ 缺失 | 复用 vs 新增 |
|---|---|---|---|
| **R1 两级天赋页** | `prerequisiteIds` 多前置 + 显式空=根（多分支已支持）；`hidden` 字段；`TalentTreePanel` 按边画线 + 坐标自由 + 三态刷新；`TalentTreeConfig.Validate()` + `TalentTreeValidator`；`TalentSaveSection`；`BattleHubTabBar.talentTreeTab` / `HubTalentEntry` 入口 | `pageId` / `branchId` 字段；`TalentTreeConfig.branches[]` 元数据；**滚动容器**（`TalentTree.scene` 无 ScrollRect）；**连线高度 0 bug**；隐藏节点不画连线；页内二级导航 | 数据层**新增 3 个字段**；面板侧**修 bug + 加滚动 + 加二级导航** |
| **R2 招式解锁** | `moveConfig` 载荷；`PlayerMoveStateMachine.IsMoveLocked`；`AttackSystem` 招架门控；三个招式资产齐全 | C2 的 `moveConfig`（被清空）；**Slash 横向位移的数据源**（`_directionalPushStep` 只有 debug 写） | C2 = **纯数据**；Slash 位移 = **新增驱动**（引擎通道已通） |
| **R3 流派分线** | `requiredTalentIds` + `TalentUpgradeRequirementProvider`（AND 语义）；`prerequisites` / `UpgradePrerequisite`；`ActiveSkillPoolConfig` 14 条；`UpgradePoolSampler.AddRequirementProvider` 扩展点 | OR 语义（**本期不需要**）；Provider 不支持 flagId | **纯资产配置，零代码** |
| **R4 能力解锁** | `StatModifier` + `BuffManager`（`AddBuff` / `RegisterApplier`）；**`ComboBuffConfig.triggers[]` 多档位 + `ComboManager.CheckTriggers`** → 连击阈值天赋**可零代码**（加一档）；`HeroConfig.maxHealth`；`UpgradeEffectManager.RegisterNumericApplier` / `RegisterExecutor` 注册表 | **`IsFlagUnlocked` 零消费者** → flag 机制要新建消费点；槽位字段拆分（`activeSkillSlotCount` / `passiveSlotCount`）；生命上限消费点；铜钱获取消费点；`UpgradeEffectManager` 无「运行时等级上限」概念 | 连击 = **纯资产**；其余 = **新增消费点 + 拆字段** |
| **R5 四个空壳 flag** | `TalentNodeConfig.flagId` 字段已存在 | **无任何消费者** | **新增 4 个消费点**（破阵=架势伤害 / 追击=CD / 运筹=选项数 / 不屈=无敌） |
| **R6 秘籍掉落** | **`DropItemPoolConfig` + `EnemyManager.TryDropItem`**（加权池 + 掉落率 + 捡漏加成，仅被 V2 短路禁用）；**`BreakableProp`**（`ISupplyPropTarget` / SupplyLayer 分层 / `TryHitAtScreenPoint` / 描边 / 渐变销毁 / 局外奖励进存档的先例）；**`ExpGemManager` + `ExpGem`**（可见掉落物范式：生成 → 飞向玩家 → 到达结算）；`HealthPotionManager`（复用 `gemPrefab`）；`ISupplyPropTarget` 拾取通道；`TalentSaveSection`（存档片同构范式） | 秘籍定义资产；秘籍存档分片；掉落分派（秘籍 vs 局内道具）；可见的秘籍掉落物；V2 短路的处理 | **大量复用**：掉落池 / 破坏物 / 飞行物范式 / 存档片范式<br>**新增**：秘籍资产 + 存档片 + 分派 |
| **R7 等级上限解锁** | 上限判定 2 处（`UpgradePoolSampler` / `UpgradeEffectManager`）；等级读取 4 入口各有越界兜底（`GetNumericConfig` 线性外推 / `GetCooldown` Clamp / `GetTriggerInterval` 返回 -1 → **`TimedPassiveModule` 会直接不注册** / `GetTriggerThreshold` 返回 -1 → 兜底） | 运行时上限覆盖；`availableRows(def)` 查询 | **新增通用机制**（三个上限相乘，见 §4A.8）；**不必改 4 个读取函数** |
| **R8 满槽金币补偿** | `PlayerState.AddCoins` → session → `StageController.SettleCoins` → 存档（**局内局外同一个池**）；`CoinCounterUI.OnCoinGained`（订阅 `PlayerState.OnCoinGained`）；**`KillRewardUI.rewardPopup`**（标题 + 金额 + 淡入淡出 + 缩放动效，形态现成） | `UpgradeChoiceManager.ShowNextChoice` 空候选分支**直接跳过**；**无通用 Toast 组件** | **改造 1 处分支** + **复用 `KillRewardUI` 形态**（不必新建 Toast 系统） |
| **R9 关卡配置** | `StageConfig`（`waves` / `RowConfig` / `killMilestones` / `clearCoinReward` / `healthMultiplier` / `damageMultiplier` / `expMultiplier` / `formationConfig`）；`FakeRouteStageConfig.prerequisiteStageIds`；`RouteCatalogData` + `StageSelectUI`（选关 + 解锁判定）；`KillRewardManager`（里程碑 Coin / Heal） | N1 / E0 / N3 的波次数据（E0 2→3、N3 3→5）；**104 两阶段的 `BossPhaseData`**（现为 3 阶段） | **纯配置** + **新建 2 个 BossPhaseData 资产** |
| **R10 铜钱闭环** | 完整：敌人 `coinReward` / 里程碑 / 通关奖励 / 局内「敛财」/ 天赋消费 / `SettleCoins` 结算 | 正式关全部 `clearCoinReward = 0`、`killMilestones` 未配 | **纯配置** |

### 11.5 总体结论

| 类别 | 条目 |
|---|---|
| **纯数据 / 纯配置（零代码）** | R3 流派分线、R9 波次、R10 铜钱、R4 的连击阈值部分、R2 的 C2 引用修复 |
| **新增小机制（有现成载体）** | R4 生命/槽位/铜钱消费点、R5 四个 flag 消费点、R7 运行时上限、R6 秘籍资产 + 存档片 + 分派 |
| **面板侧（另一边）** | R1 的滚动容器 + 二级导航 + 连线 bug + 隐藏节点连线 |
| **已确认无需做** | OR 语义、互斥/专精、分支计数、关卡需求↔解锁度对照（第二阶段再议） |
