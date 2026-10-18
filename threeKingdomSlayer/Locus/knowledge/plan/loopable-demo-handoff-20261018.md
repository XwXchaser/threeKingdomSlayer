---
id: kd_7ad1b9fe-1e65-469d-8f73-d4530b5c98c8
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可循环 Demo 里程碑 · 交接（2026-10-18）

> 本文是新里程碑的**唯一入口**。目标是在现有战斗与路线系统上，做一个"能循环游玩"的小 demo：
> 教学关 → N1 → N2 → N3 打完收工 → 回主菜单用局外成长变强 → 再来。
> 本里程碑的数值、关卡配置、局外成长形态都**还没定**，等后续单独讨论；本文只锁定目标、现状锚点、待决策与坑。

## 0. 开工前先读

1. 本文
2. `design/out-of-game-growth-system.md`（局外成长方向：天赋树 / Loadout / 铜钱闭环）
3. `design/gameplay-pillars.md`（玩法五支柱，判断"哪里有决策"）
4. `plan/ice-element-handoff-20261018.md`（冰系现状与未验证项，避免误判为已完成）

## 1. 现状基线（2026-10-18 实测）

| 项 | 事实 |
|---|---|
| 分支 / HEAD | `route-scroll-movement` @ `75bba2a`，与 `origin` 一致 |
| 可用敌人 | **1011、1021、109、BOSS（104/107/108）及其衍生** |
| 路线系统 | 已在跑 **RouteV2**（滚动路线 + 分叉选择），Stage01 的 V2 配置有 4 个战斗节点 |
| 战斗内成长 | 三选一升级、连击、蓄力/位移/火系协同、冰系冻结破冰均已实现 |
| 局外成长 | **未实现**。铜钱能赚能存（`SaveManager`），但没有消费入口、没有天赋/属性升级界面 |
| 教学关 | **没有教学关资产**。但有对话系统与 `Tutorial` 事件类型、`tutorialCompleted` 存档位 |
| 存档 | 有 V2 路线存档结构，但"每个战斗节点存档"尚未开启（见 §3.3） |

**工作区状态（务必先处理）**：39 个已跟踪改动 + 37 个未跟踪文件未提交，其中冰系是完整一坨但**有 4 项只有代码没有实机验证**（见 `plan/ice-element-handoff-20261018.md` §9）。开工新里程碑前建议先把冰系单独提交，否则后面每一次改动都在混着两件事的树上做。

## 2. 本里程碑要的东西

### 2.1 循环体验

```
主菜单 → 教学关（初见杀） → 正式开局 → N1 → N2 → N3 → 结算/通关
                                    ↑                          ↓
                              局外成长（花铜钱：属性 + 解锁招式）←──┘
```

- **完整 N1→N3** 可玩通。
- **教学关**：带一段"初见杀"演出——玩家最后被 109 一下戳死，不是战败判定，是脚本化体验。
- **初期上限就是 N3**：预设玩家第一轮刚好能打到 N3 尽头。做不到 N3 之后的内容，原因是"提升不够"，而不是弹一个"未开放"。
- **局外成长**：基础属性升级 + 新招式解锁。**先锁定当前 C2 与 C4 招式**，把它们作为第一轮之后的解锁奖励。
- **J1 不能走右路**：分支选择里右侧这条路在第一轮要不可选（或选择后不成立）。

### 2.2 明确不在本期

- 关卡/数值平衡（后续讨论）
- 局内三选一与局外成长的联动配平（后续讨论）
- 1021/109 之外的敌人扩展

## 3. 现状锚点（可直接照着改的地方）

### 3.1 路线与节点（RouteV2）

- 配置：`Assets/RouteData/Stage01V2/Stage01_RouteV2.asset`（`stageId: 1`，`routeSceneName: Stage01_RouteV2`），`combatNodes` = 4 个。
- 节点资产：`Assets/RouteData/Stage01V2/Node_A.asset` … `Node_D.asset`，类型 `RouteNodeConfigV2`。
- 实测字段：

| 节点 | savePoint | isFinalNode | outgoingConnections |
|---|---|---|---|
| A | 0 | 0 | 3 条（choiceSlot `To B` / `To C` / `To D`） |
| B | 0 | **1** | 0 |
| C | 1 | 0 | 1 |
| D | 1 | 0 | 1 |

- **J1 右路锁定的落点**：Node A 的 `outgoingConnections`（当前一次给出三条）。要么删掉对应 connection，要么给 connection 加"锁定/条件"字段并在选择 UI 里置灰。**当前没有条件字段**，需要新增一个（`conditionEnabled` 目前只出现在 battleEntries 上）。
- 路线运行时：`Assets/Scripts/RouteV2/RouteStageRuntimeV2.cs`，节点推进 `RunNode` → `RunBattleEntries` → 尾部锚点 → 选择（`_choosing`）。

### 3.2 战斗与关卡配置

- 战斗节点内是 `battleEntries`（可多个），每个 battle 指向一个战斗配置资产。
- N1 的关卡数据在 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`（`fillUpRule = PerRow`，row0/row4 有敌人、中间空排）——**这是本地改过、未提交的配置**，提交前要确认。

### 3.3 存档（重点，需要先验证再改）

**已有结构**（`Assets/Scripts/Core/SaveManager.cs`）：

```csharp
RouteStageSaveSnapshot {
    stageId, currentNodeId, currentHealth, currentRevives,
    currentLevel, currentExp,
    nodeStates[{ nodeId, completedEntryIndices }],
    upgrades[{ upgradeId, level }]
}
```

- 写入：`RouteStageRuntimeV2.SaveRouteCheckpoint()`，只在**到达 `savePoint == true` 的节点时**写（调用点 `RouteStageRuntimeV2.cs:164` 起始节点、`:468` 连接到达）。
- 读取：`RouteStageV2Launch.StartFromCheckpoint` 为真时才恢复（`StageController.StartStage` / `RouteStageRuntimeV2` 两处）。
- 参照实现：`SaveManager` 里还有旧的**卷轴/FakeRoute 存档**（`FakeRouteStageSaveSnapshot`、`activeFakeRouteStageId`），那套是完整的；V2 存档若恢复不灵，直接对照它。
- 主菜单已有 New Game / Continue / Delete Save，Continue 走的就是存档恢复。

**结论与待办**：
1. "每个战斗节点存档"当前**没开满**：A、B 是 `savePoint: 0`。开启方式是把战斗节点都设为 `savePoint: 1`（或改成"进入战斗前必定存"）。
2. 恢复是否真的正确（血量/等级/升级/已通关 entry 能否回到原位）**没有实测证据**。第一步应该是：跑一遍"打到 C → 退出 → Continue → 看是否落在 C 且状态正确"。
3. 教学关是否也参与存档、失败重进是否从教学关重来，需要拍板（见 §4）。

### 3.4 招式与"锁 C2/C4"

- 招式资产：`Assets/ScriptableObjects/Moves/Zhangfei/`（`Zhangfei_Jab1/2/3`、`Zhangfei_StabR2`、`Zhangfei_LaunchFinisher`）+ `Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset`（`roots: [gesture 0 → 一个 move]`，`states` 为空）。
- **C2 = `Zhangfei_StabR2`**（指向突刺，冰系文档里已按此口径把 `isHeavy` 设为 1）。
- **C4 尚未确认**：按命名最可能是 `Zhangfei_LaunchFinisher`（挑飞终结）。开工第一步先确认 C1–C4 与资产的对应关系，别锁错。
- **当前没有"招式锁定"机制**：`AttackSystem` 只有 `RegisterUnlockedAttack(id, cfg, level, floatValue)`（升级效果解锁）和 `finisherParryUnlocked` 这个先例标志。要锁 C2/C4，需要新增一个"玩家已解锁招式集合"，在招式解析处拦截，并在 HUD/招式表里表达"未解锁"。
- 提示：`MoveTable_Zhangfei.asset` 的 `states` 是空的，连段目前很可能不是靠这张表驱动的，改之前先确认连段从哪儿来。

### 3.5 教学关

- 已有：`DialogueEventType.Tutorial`、`SaveData.tutorialCompleted` / `completedTutorialDialogueIds`、对话资产 `Assets/ScriptableObjects/Dialogue/Tutorial_Stage3_Start.asset`、`Tutorial_TestStage_Start.asset`。
- 缺：**教学关本身**（没有对应的路线节点/战斗配置资产）。两种做法见 §4。

### 3.6 109 初见杀

- `Assets/Resources/EnemyPrefabs/Enemy_109.prefab`：`chargeDamage: 1`、`chargeStepDuration: 0.35`；实际伤害 = `chargeDamage × 冲锋格数 × 波次攻击倍率`（见 `CavalryEnemy.ChargeDamage`）。
- 一击必杀有两条路：改数值（把 chargeDamage 调到超过玩家 maxHealth），或教学关用脚本化敌人（固定伤害、不可操作）。**推荐后者**，避免污染正式骑兵数值。

## 4. 待决策（需要你拍板，讨论后再动手）

1. **局外成长的形态**：铜钱直接买属性等级？还是天赋树 + 铜钱？先做最薄的一层（属性等级 + 招式解锁）还是直接按 `design/out-of-game-growth-system.md` 铺开？
2. **C4 到底是哪个资产**，C1–C4 的命名与资产映射表要不要一并整理进文档。
3. **"只能打到 N3"的实现方式**：N3 结尾就结算（推荐），还是难度曲线自然卡住（需要配平）。
4. **教学关挂在哪**：独立 stage（主菜单入口 → 教学 → 回主菜单）还是 Route V2 的 `startNode` 前置节点（推荐，省一套流程）。
5. **存档粒度**：每个战斗节点进战斗前存（推荐）；战斗中退出是否算失败；教学关是否参与存档。
6. **J1 右路是删掉还是置灰**（置灰更可读，能暗示"以后能走"）。

## 5. 建议实施顺序

| 阶段 | 内容 | 验收 |
|---|---|---|
| **P0 卫生** | 冰系单独提交；删除 `DEBUG_IceTest_Bootstrap`；确认 N1 `rows` 与 `YJunctionSample.unity` 的改动归属 | 工作区只剩一件事 |
| **P1 存档** | 验证 V2 checkpoint 恢复；战斗节点全开 `savePoint`；必要时对照 FakeRoute 存档 | 打到任意节点退出 → Continue → 状态与位置正确 |
| **P2 N1→N3 打通** | 三节点战斗配置串联；N3 结尾结算回主菜单；铜钱落袋 | 一轮完整通关，无需作弊 |
| **P3 教学关** | 教学节点 + 109 初见杀演出 + 对话 | 新档第一次必走教学关，死法可复现 |
| **P4 招式锁定** | 玩家已解锁招式集合 + C2/C4 锁定 + HUD/招式表表达 | 首轮无 C2/C4，解锁后可用 |
| **P5 局外成长** | 铜钱消费 → 基础属性升级 + 招式解锁界面 | 花铜钱变强，下一轮明显更远 |
| **P6 J1 右路锁** | connection 条件下 + 选择 UI 置灰 | 首轮右路不可选 |

P1 是最有风险的一环（存档坏了整个循环不成立），建议先做 P1 再做 P2。

## 6. 坑与风险

1. **编译回执可能造假**：`unity_recompile` 曾回报成功但 `Library/ScriptAssemblies/Assembly-CSharp.dll` 没重建（Play Mode 跑旧代码）。核对方式：看 DLL 的 mtime/大小，或用反射查新类型/新方法；必要时 `touch` 源文件 + `CompilationPipeline.RequestScriptCompilation()`。
2. **Play Mode 下工具调用偶发返回空且不执行**：写验证结果请用**绝对路径**落盘，再用 shell 读。
3. **`git pull` 会碰到脏树**：本仓库大、Unity 同时导入时合并可能跑 7 分钟以上并留下 `index.lock`（确认无 git 进程后删）。改动集中在 `Enemy.cs` / `Enemy_1021.prefab` 这类热门文件时，用"备份 → 最小范围 stash → ff 合并 → pop"流程（本次已验证可行）。
4. **`YJunctionSample.unity` 相对 HEAD 净减 10 万行**，与玩法无关，提交前必须单独确认。
5. **冰系 4 项未验证**（DoT 来源门、同列多冻结、补齐落位、骑兵冻结），别在教学关里假设它们一定正常。
6. **`DEBUG_IceTest_Bootstrap`** 还挂在 `Assets/Scenes/Battle.scene`，会给玩家白送冰雾，正式流程前必须删。

## 7. 下次对话的开场动作

1. 读本文 + `design/out-of-game-growth-system.md`
2. 先答 §4 的六个问题（至少 1、3、4）
3. 从 **P1 存档** 开始；动手前把冰系提交掉
