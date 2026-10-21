---
id: kd_50b867c8-e575-4cc1-ba87-796ef15212a0
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: ColumnManager 拆分实施方案（2026-10-20，写给后续接手的 AI）：含现状画像（2149 行区段图 + 全部 public API + 消费者清单）、5 刀拆分顺序与每刀验收、与「删 FillUpRule/legacy 分支」的合并点、可直接复用的 Play Mode 验收脚本（Stage_Trial 注入 / 整排清空补齐 A/B / 豁免 A/B / 阵型逐位比对 / 冰系回归 / 缺失脚本检查）、以及本项目特有的 8 个坑。
---

---
summary: ColumnManager 拆分实施方案（2026-10-20，写给后续接手的 AI）：含现状画像（2149 行区段图 + 全部 public API + 消费者清单）、5 刀拆分顺序与每刀验收、与「删 FillUpRule/legacy 分支」的合并点、可直接复用的 Play Mode 验收脚本（Stage_Trial 注入 / 整排清空补齐 A/B / 豁免 A/B / 阵型逐位比对 / 冰系回归 / 缺失脚本检查）、以及本项目特有的 8 个坑。
---

# ColumnManager 拆分实施方案（2026-10-20）

> **本文是给「下一个执行者」的操作手册**，默认你只读过本文 + 下面列出的两份权威文档。
> 目标：把 `Assets/Scripts/Core/ColumnManager.cs`（2149 行）按**调度语义**拆成若干职责类，
> 并顺手清掉已不可达的 PerColumn 遗留分支与 `FillUpRule` 枚举（合起来称为「批次 2 + F」）。
>
> 配套权威文档（**动手前必读**）：
> - `design/percolumn-fillup-rules.md` —— 补齐规则权威版（术语、普通补齐/入场/空排占位符/999 门/击退的定义）
> - `design/immutable-constraints.md` —— 不可变约束 8 条（**任何重构都不能违反**）
> - `design/formation-fillup-mechanism-20261020.md` —— 机制地图与本次拆分方案雏形
> - `design/architecture-decoupling-analysis.md` —— 全局架构现状与批次顺序
> - `plan/architecture-optimization-handoff-20261020.md` —— 累计进度与验收清单
>
> 测量基线：2026-10-20，分支 `feat/stage-select-talent-overlays`，HEAD `0817024`（已 push）。
> **行号仅供定位；实际改动请用方法名搜索**，因为每刀提交后行号会漂移。

---

## 0. 给执行者的速览

| 项 | 内容 |
|---|---|
| 现状 | `ColumnManager` 2149 行，同时负责：逻辑排队列、入场调度、补齐调度、击退回位、横向位移、查询、骑兵槽位 |
| 外部压力 | 外部只用到 **21 个方法名**，其中 **13 个是纯查询**；`AttackSystem` 一家就占了 31 处调用 |
| 目标 | 拆成 5 个职责类：`LogicalRowQueue` / `ColumnQueries` / `SpawnEntryMover` / `WaveMarchScheduler` / `DisplacementMover`；`ColumnManager` 退化为「门面 + 转发」 |
| 关键原则 | **每刀只做「搬到新类 + 原类转发」，不改行为**；每刀一个提交 + 一次回归 |
| 硬约束 | 补齐/击退/横向/Boss 四种调度语义必须保持独立；`ColumnManager` 必须是**唯一**的订单创建者 |
| 合并项 | 最后一刀顺手删 `FillUpRule` 枚举 + `StageConfig.fillUpRule` 字段 + `ColumnManager` 里已不可达的 legacy 分支 |

---

## 0.7 进度（截至 2026-10-20 夜）

| 刀 | 抽出 | 状态 |
|---|---|---|
| **1** | `LogicalRowQueue`（逻辑排队列：活人排 / 空排占位符 / 999 门） | ✅ **已完成**（`facdbde`）：ColumnManager 2149 → 2026 行 |
| **2** | `ColumnQueries`（13 个只读查询） | ✅ **已完成**（`415d4c0`）：ColumnManager 2026 → 1958 行 |
| 3 | `SpawnEntryMover`（入场调度） | ⬜ 待做（中风险） |
| 4 | `WaveMarchScheduler`（补齐调度） | 🟡 **刀 4a 已完成**（`09416f2`）：状态 + 只读查询已归位，ColumnManager 1836 → 1791 行；**刀 4b（流程）待做** |
| 5 | `DisplacementMover`（击退回位 / 横向位移 / 破冰顶开 / 聚拢） | ⬜ 待做（中高风险） |
| 6（=F） | 删 PerColumn 遗留分支 + `FillUpRule` 枚举与字段 | ⬜ 待做（中；建议在刀 4/5 之后） |

**刀 1 / 刀 2 的验收数据**（Play Mode，注入 `Stage_Trial` 第 1 波：6 排含一个空排占位符）：

- 入场后敌人在逻辑行 **0/1/3/4/5**（与拆分前一致）
- 整排清空基线：杀 r0 → `col1/r0 · col1/r2 · col2/r3 · col3/r4`（与拆分前**逐位一致**）
- 豁免 A/B：col1/r1 打 `ignoreFillUpOrders` 后杀 r0 → `col1/r1[豁免] · col1/r2 · col2/r3 · col3/r4`（豁免者不动）
- 主流程：落地页 → 开始关卡 → `phase=Battle`、出怪、范围攻击连掉 20×3

**给后续执行者的实现要点**（省得重新踩）：

1. 刀 1：`LogicalWaveRow` / `LogicalWaveRowKind` 由 ColumnManager 的**嵌套类型提为顶层类型**，
   因此 ColumnManager 里原有引用**原样编译**，改动只落在字段与方法体；队列类不决定何时补齐、不派单、不移动敌人。
2. 刀 2：`ColumnQueries` 用委托 `() => columns` 取列数组（避免 `InitializeColumns` 重建后拿到旧引用）；
   ColumnManager 侧保留同名 public 方法改为委派，**调用方零改动**。
3. 刀 3：`SpawnEntryMover` 只依赖两样——逻辑排队列（取目标排）+ 一个「入场全部完成」的回调
   （`StartPendingLogicalLayoutReflow`）；「入场占用中就跳过其它调度分支」的语义由 `Tick()` 返回 true 表达。
4. 刀 6 的结果：**补齐规则现在只有一种**（逐排补齐），`FillUpRule` 枚举与 `fillUpRule` 字段已从
   `Data/StageConfig.cs` 删除；`WaveSpawner` 只剩逐排路径；`ColumnManager` 的「按列找空位前压」
   legacy 分支与两个 inert 入口（`RowBasedFillUp` / `CompactAllColumns`）已删。
   结论：**AI 部署关卡时已经没有「补齐规则」这个选项可填**。

5. 刀 4a 的结果：波次行军的**状态**（generation / 计划与生效的目标行与目标槽 / 准备中与待行军集合 /
   暂停快照 / 当前步骤描述）与**只读查询**（`IsRowFullyVacated` / `GetMaxOccupiedRow` /
   `IsTargetReserved` / `CanStartOrder` / `CanEnterRow`）已归位到 `WaveMarchScheduler`，
   依赖只有「列数组」与「骑兵槽位判定」两个委托。**流程**（`StartWaveMarch` /
   `TryBuildLogicalWaveMarchPlan` / `BeginWaveStep` / `StartPreparedWaveStep` / 暂停恢复）仍在
   ColumnManager，刀 4b 再搬。
6. ⚠️ 刀 4a 的坑（后续执行者注意）：批量替换时把引用写成了**字段** `_waveMarch.xxx`，
   而该字段由惰性属性创建 → 运行时 200 次 NullReferenceException（`ColumnManager.Update:139`）。
   改为走**属性** `WaveMarch.xxx` 后正常。**惰性属性 + 同名字段并存时，批量替换必须用属性名。**

> 附带修掉一个既有问题（`49d4e96`）：落地页就绪态把局内 HUD 整组隐藏时，连击数字启动协程会报
> 「Coroutine couldn't be started because the game object is inactive」→ 加 `activeInHierarchy` 守卫。

---

## 1. 不可违反的规则（动手前先背下来）

### 1.1 不可变约束 8 条（`design/immutable-constraints.md` 全文）

1. 位移不可重叠（目标槽校验 + Boss 墙）
2. 未受位移者保持原位
3. **普通补齐唯一触发条件 = 某排在全部列中完全清空**，由 `ColumnManager` 统一调度
4. 击退回位必须返回**精确原槽**
5. 攻击打断边界（未被击中的攻击中敌人不被打断）
6. **调度所有权**：普通补齐 / 击退回位 / 横向位移 / Boss 推进必须用不同订单语义；禁止敌人自行创建移动；订单完成要校验 owner + generation
7. 配置验证只在波次生成时做
8. 聚拢冲突用自己的裁决规则，不借用击退回位或普通补齐

### 1.2 补齐规则权威版要点（`design/percolumn-fillup-rules.md`）

- **普通补齐 / 波次行军**：只由**整排清空**触发（战斗产生）。
- **新波次入场（SpawnEntry）**：敌人生成在逻辑目标排**后方固定两排**，再移动到各自逻辑排目标。
  **入场不是普通补齐**，不改逻辑排队列、不删普通空排、不开 999 门。
- **普通配置空排** `(0,0,0,0,0)`：等同"没有活人的死人排"，随前方活人排前移；只有当其前方已无任何活人排时才自动移除。
- **节奏门 999**：某排任意槽位为 999 → 整排不生成敌人；门前的配置敌群清空前，后方不得跨越。
- **击退**：只移动被击退者，停顿后回**精确原槽**；不回传给未受击敌人。
- **横向位移**：只改 column，不附带 row 回位或补齐。
- **Boss**：独立推进规则，不参与普通敌人的补齐与回位。

### 1.3 已实现且**待验收**的行为（别改坏）

`percolumn-fillup-rules.md` 末尾的「待验收：补齐事务与攻击/位移并发」——重点覆盖：敌人攻击动画中死亡、击退/击飞回位、行军中被位移、共享血量组、Boss 墙、连续箭雨/远程敌人。拆分过程中若这些行为出问题，优先怀疑你动了 owner/generation 校验。

---

## 2. 现状画像

### 2.1 区段图（`Assets/Scripts/Core/ColumnManager.cs`，2026-10-20 的 2149 行）

| 行段 | 区段 | 体积 | 归属建议 |
|---|---|---|---|
| 1–220 | 字段、生命周期、`Column` 结构、骑兵槽位 | 220 | 拆散（字段按新类分） |
| 221–261 | 添加敌人 | 40 | 留 `ColumnManager`（槽位写入） |
| 263–352 | 移除敌人（含 `TriggerFillForward` / `TriggerAllBossFillForward`） | 89 | 槽位 + 触发器 → 触发器归 `WaveMarchScheduler` |
| 354–369 | 更新敌人位置 | 15 | 留 |
| 371–551 | **查询接口**（13 个只读方法） | 180 | → **`ColumnQueries`**（刀 2） |
| 552–775 | **波次布局 + 逻辑排队列 + SpawnEntry 调度** | 223 | 队列 → `LogicalRowQueue`（刀 1）；入场 → `SpawnEntryMover`（刀 3） |
| 776–1230 | **波次行军（规则 1/2）**：规划、派单、暂停/恢复、999 门 | 454 | → **`WaveMarchScheduler`**（刀 4） |
| 1232–1539 | **精确击退回位** | 307 | → **`DisplacementMover`**（刀 5） |
| 1541–2078 | **位移效果**（破冰顶开、击退波、横向位移、聚拢） | 537 | → **`DisplacementMover`**（刀 5） |
| 2079–2140 | `PostDisplacementFillUp` 等 | 62 | 随位移类 |
| 2141–2148 | 工具方法（`DumpColumns`） | 8 | 留 |

### 2.2 关键字段分组（决定新类怎么持有状态）

| 字段组 | 现有字段 | 归属 |
|---|---|---|
| 逻辑排队列 | `_logicalWaveRows` / `_logicalRowsByInitialRow` / `_waveEnemyLogicalRows` / `_openedRhythmGateRows` / `_logicalLayoutDirty` / `_waveLayoutGeneration` | `LogicalRowQueue` |
| 入场 | `_spawnEntryGeneration` / `_spawnEntryEnemies` / `_pendingSpawnEntryEnemies` | `SpawnEntryMover` |
| 补齐 | `_waveGeneration` / `_plannedWaveTargetRows` / `_activeWaveTargetRows` / `_plannedWaveTargetSlots` / `_activeWaveTargetSlots` / `_pausedWaveEnemies` / `_pausedWaveTargetRows` / `_preparingWaveEnemies` / `_pendingWaveEnemies` | `WaveMarchScheduler` |
| 击退回位 | `_pushReturnTransactions` / `_pushReturnGeneration` / `_pushWorkList` / `_pushHitSet` / `_pushByColumn` | `DisplacementMover` |
| 骑兵槽位 | `_cavalryReservedByEnemy` / `_cavalryReservedSlots` | 留 `ColumnManager`（或独立 `CavalrySlots`） |
| 槽位 | `columns` / `_rowEnemies` | 留 `ColumnManager` |
| 查询缓存 | `_rangeQueryList` | → `ColumnQueries` |

### 2.3 外部消费者（实测，2026-10-20）

外部只调用这 **21 个方法名**（括号内为调用次数）：

```
查询（13）：GetAllEnemiesInRange(13) GetEnemiesInRange(7) GetEnemyAt(3) GetFrontEnemy(1)
             GetEnemiesInColumn(1) GetAllEnemies(2) GetColumn(2) GetColumnEnemyCount(1)
             GetCombatBossCoveringColumn(2) DumpColumns(2) …
写（8）：  StartWaveMarch(1) StartSpawnEntry(1) ConfigureWaveRowLayout(1) ClearWaveRowLayout(1)
             TriggerAllBossFillForward(1) ApplyDirectionalPush(1) ApplyPushWave(1)
             PostDisplacementFillUp(2) CanAdvanceIntoRow(1)
```

调用方分布：`AttackSystem` 31 处（几乎全是查询）、`WaveSpawner` 6、`UpgradeEffectManager` 4、`CycloneZone` 2、`ActiveSkillRunner` 2，其余各 1。

> **结论：消费者面比想象中窄**——这正是拆分的可行性来源。`Enemy` 自己只有 1 处直接调用（其余经 `AttackSystem.Instance.columnManager` / `EnemyManager.Instance.columnManager`）。

### 2.4 已不可达的死代码（F 项要删的）

| 位置 | 内容 | 说明 |
|---|---|---|
| `ColumnManager.RowBasedFillUp` | 打警告 `legacy compaction is disabled` 后直接返回 | PerColumn 遗留入口，已 inert |
| `ColumnManager.CompactAllColumns` | 同上 | 已 inert |
| `StartWaveMarch()` 的 legacy 分支 | 没有逻辑排队列时走"按列找空位前压" | **PerColumn 资产已清零 → 不可达** |
| `StageConfig.fillUpRule` + `enum FillUpRule` | 字段/枚举 | 删掉后 `WaveSpawner` 两分支合一、`Enemy.cs:924` 的 `== FillUpRule.PerRow` 判断可简化 |

> 删这些**不改变任何现役行为**：现役资产 100% 是 PerRow，逻辑排队列路径恒定生效。
> 验证方式：删完后 `grep -rn "PerColumn" --include=*.cs Assets/Scripts` 应为空，
> 且 `StageConfig` 的 `fillUpRule` 字段删除后现有 12 个 PerRow 资产的序列化键会被 Unity 忽略（安全）。

---

## 3. 目标架构

```
                    ColumnManager（门面 / 槽位持有 / 骑兵槽位）
                      │  转发（先全部保留 public 方法签名，只改实现委托）
        ┌─────────────┼──────────────┬────────────────┐
        ▼             ▼              ▼                ▼
  LogicalRowQueue  ColumnQueries  SpawnEntryMover  WaveMarchScheduler
   （队列/门/占位）  （只读查询）    （入场调度）      （补齐调度）
                                        └──────────┬─────────┘
                                                   ▼
                                            DisplacementMover
                                      （击退回位/横向位移/破冰顶开/聚拢）
```

**依赖方向**：`ColumnManager` → 各新类；新类之间**不互相引用**（需要协作时通过 `ColumnManager` 转发或注入委托），
避免重新造出上帝类。

**每个新类的形状**（推荐一致）：普通类（非 MonoBehaviour），由 `ColumnManager` 持有实例并注入它自己需要的依赖
（如 `Func<Enemy, bool>` 判空、`Action` 回调）。**不要**让新类去 `FindObjectOfType`。

---

## 4. 逐刀实施方案

> 通用要求：每刀 = 一个提交 + 一次 Play Mode 回归 + 一次文档更新。刀与刀之间不要合并。

### 刀 1：`LogicalRowQueue`（逻辑排队列）— 风险低

**目标**：把「活人排 / 普通空排占位符 / 999 节奏门 / 前导空排移除 / 门开关」这套**纯数据 + 队列操作**搬出去。

- 新建 `Assets/Scripts/Core/LogicalRowQueue.cs`
- 搬运内容：`_logicalWaveRows` / `_logicalRowsByInitialRow` / `_waveEnemyLogicalRows` / `_openedRhythmGateRows` / `_logicalLayoutDirty` / `_waveLayoutGeneration`
  与相关方法：`ConfigureWaveRowLayout` / `ClearWaveRowLayout` / `IsRhythmGateRow` / `IsRhythmGateOpen` / `RegisterWaveEnemy` / `UnregisterWaveEnemy` / 队列重排与差分规划相关私有方法
- `ColumnManager` 侧：保留同名 public 方法，改为转发（**签名不变**）
- **验收**：`Stage_Trial` 注入后，敌人仍落在逻辑目标行 0/1/3/4/5（row2 是空排占位符）；999 门场景行为不变
- **提交**：`refactor(column): 逻辑排队列抽成 LogicalRowQueue`

### 刀 2：`ColumnQueries`（只读查询）— ✅ 已完成（`415d4c0`）

- 新建 `Assets/Scripts/Core/ColumnQueries.cs`
- 搬运 13 个只读方法（`GetAllEnemiesInRange` / `GetEnemiesInRange` / `GetEnemyAt` / `GetFrontEnemy` /
  `GetEnemiesInColumn` / `GetAllEnemies` / `GetColumn` / `GetColumnEnemyCount` / `GetTotalEnemyCount` /
  `IsColumnEmpty` / `AreAllColumnsEmpty` / `GetEnemiesByRowLimit` / `GetCombatBossCoveringColumn` / `DumpColumns`）
- 内部仍要读 `columns` / `_rowEnemies` → 把这两个集合以只读方式交给 `ColumnQueries`（构造函数传入引用）
- **注意**：查询方法里用了 `_rangeQueryList` 做复用避免 GC → 一并搬走
- **验收**：`AttackSystem` 的命中判定不变（打三下各掉 20 血）；`QTEController` 选目标不变
- **提交**：`refactor(column): 只读查询抽成 ColumnQueries`

### 刀 3：`SpawnEntryMover`（入场调度）— 风险中

- 新建 `Assets/Scripts/Core/SpawnEntryMover.cs`
- 搬运：`_spawnEntry*` 字段、`StartSpawnEntry` / `StartPreparedSpawnEntry` / `OnSpawnEntryComplete` /
  `IsSpawnEntryPending` / `CanStartSpawnEntryOrder` / `IsContinuousWaveTargetReserved` / `CanContinuousWaveEnterRow` / `RequestCavalryDismountReflow`
- **约束**：入场**不改逻辑排队列**、不删普通空排、不开 999 门、不调用普通 `StartWaveMarch()`
- **验收**：见 §5.2 的「入场」脚本（敌人先出现在目标后方两排，再走到目标排）

### 刀 4：`WaveMarchScheduler`（补齐调度）— 风险中高

- 新建 `Assets/Scripts/Core/WaveMarchScheduler.cs`
- 搬运：`_waveGeneration` / `_planned*` / `_active*` / `_paused*` / `_preparing*` / `_pending*` 字段，
  `StartWaveMarch` / `TryBuildLogicalWaveMarchPlan` / `BeginWaveStep`（两个重载）/ `StartPreparedWaveStep` /
  `OnWaveEnemyRushComplete` / `IsRowFullyVacated` / `CanAdvanceIntoRow` / `CanStartWaveMarchOrder` / 暂停与恢复
- **约束**：owner + generation 校验必须原样保留；禁止按 `attackRange` 过滤源排成员（历史缺陷）
- **豁免**：`Enemy.ignoreFillUpOrders` 的跳过逻辑（见 §5.3）必须跟着一起搬，且**留在规划层**
- **验收**：见 §5.3 的补齐 A/B 脚本

### 刀 5：`DisplacementMover`（位移类）— 风险中高

- 新建 `Assets/Scripts/Core/DisplacementMover.cs`
- 搬运：`_pushReturn*` / `_pushWorkList` / `_pushHitSet` / `_pushByColumn` 字段，
  精确击退回位区段（`RegisterPushReturn` / `CancelPushReturnForEnemy` / `NotifyPushReturnTopologyChanged` …）、
  位移效果区段（`ApplyBreakIcePush` / `RegisterBreakIceReturn` / `MoveEnemyToColumnAtRow` / `CanPushColumn` /
  `ExecutePush` / `ApplyPushWave` / `ApplyDirectionalPush` / `ApplyConvergenceWave` / `PostDisplacementFillUp`）
- **约束**：击退只移动被击退者、必须回精确原槽；横向位移只改 column；聚拢用自己的裁决
- **验收**：见 §5.4

### 刀 6（= F）：删 PerColumn 遗留 + `FillUpRule` 字段 — 风险中

在刀 4、5 之后做（此时 `ColumnManager` 已经是转发门面，删死代码不会与新类纠缠）：

1. 删 `RowBasedFillUp`（inert）、`CompactAllColumns`（inert）
2. 删 `StartWaveMarch()` 的 legacy 分支（无逻辑队列时的那条路径）
3. 删 `Assets/Scripts/Data/StageConfig.cs` 的 `fillUpRule` 字段与 `enum FillUpRule`
4. 简化 `WaveSpawner`（两分支合一、去掉 `SpawnRow` 的 `fillRule` 参数）、`Enemy.cs` 里 `== FillUpRule.PerRow` 的判断、`StageFormation.GetFillUpRule`
5. 删 `StageConfig.OnValidate` 里对 `FillUpRule.PerColumn` 的检查（枚举没了就没得查）
6. **验证**：`grep -rn "FillUpRule\|PerColumn" --include=*.cs Assets/Scripts` 结果为空；现役两关卡行为不变

---

## 5. 验收手册（可直接复制的 Play Mode 脚本）

> 通用注意事项见 §6。**进入 Play Mode 前**先 `LoadScene("Battle")` 再等 `FakeRouteRuntime.Instance.IsPrepared`；
> 所有等待用 `ctx.WaitFrames(n)`，不要用 `Task.Delay`。

### 5.1 注入 `Stage_Trial`（多排场景）

`Assets/Resources/StageConfigs/Stage_Trial.asset` 第 1 波 = 6 排（含一个 `(0,0,0,0,0)` 空排占位符）+ 5 个敌人。
`0x65` = 敌人 id **101**（不是 105）。

```csharp
var trial = Resources.Load<StageConfig>("StageConfigs/Stage_Trial");
UnityEngine.Object.FindObjectOfType<WaveSpawner>().stageConfig = trial;   // WaveSpawner 自己的字段优先
StageController.Instance.SetCurrentNodeBattleConfig(trial);
StageController.Instance.StartRouteBattle(trial);
// 等 120 帧，敌人应落在逻辑目标行 0/1/3/4/5（物理行 = 目标 + 2，SpawnEntry 未走完前会看到 +2）
```

### 5.2 入场（SpawnEntry）验收

- 注入后 120 帧：敌人物理行 = 逻辑行 + 2，state = `Moving`，`RushMoveOrderOwner = SpawnEntry`
- 再等：state 变为 `Idle`/`Attacking`，行 = 逻辑行

### 5.3 补齐 A/B（整排清空 → 后排前移）

```csharp
// 清掉最前排
var cm = AttackSystem.Instance.columnManager;
foreach (var e in cm.GetAllEnemies().Where(e => e.rowIndex == 0).ToList())
    e.TakeDamage(99999f, DamageType.Stab);
// 等 240 帧，观察后排 rowIndex 是否 -1
```

期望（2026-10-20 实测基线）：杀前 `col2/r0 · col1/r1 · col1/r3 · col2/r4 · col3/r5` → 杀后 `col1/r0 · col1/r2 · col2/r3 · col3/r4`。

**豁免 A/B**：给 row1 的敌人（col1）置 `ignoreFillUpOrders = true` 再杀 r0 →
期望 `col1/r1[豁免] · col1/r2 · col2/r3 · col3/r4`（**它不动，同排他人照常前移**）。

### 5.4 位移回归

- 击退：命中后排敌人 → 它后移；停顿后**回精确原槽**；**未受击的同列敌人不动**
- 横向位移：只有命中者改 column，且不触发任何补齐
- 破冰顶开：被顶开者移动 + 回位登记，**不动未受击者**

### 5.5 阵型逐位比对（拆查询层时必做）

```csharp
var sc = StageController.Instance; var fc = sc.stageConfig.formationConfig;
int maxRow = Mathf.Max(fc.maxVisibleRows - 1, 0);
for (int col = 0; col < 5; col++)
  for (int row = 0; row <= maxRow; row++)
  {
      float expect = RowFormation.GetColumnOffsetX(row, col, maxRow, fc.manualRowHalfWidths,
                        fc.formationPreset, fc.formationMaxSpread, fc.formationMinSpread, fc.formationPowerCurve);
      // 与 sc.GetFormationOffset(col, row) 比较，差异应 < 0.0001
  }
```

### 5.6 冰系回归（`EnemyFreeze` 拆分后）

- `Freeze(3f)` → `state=Frozen`、`IsSuspended=True`、`IsActionPaused=True`
- 冻结中 `ApplyIceHit` → 概率不累积
- `BreakFreeze()` → `state=Idle`、`IsSuspended=False`、冰壳销毁
- `Freeze(0.4f)` → 1.5s 后自然解冻回原状态，`IsInPostFreezeRecovery=True`

### 5.7 遗留场景缺失脚本检查

```csharp
foreach (var path in new[]{"Assets/Experiments/CurvedScroll/Diagnostics/Bisect.unity",
                           "Assets/Experiments/CurvedScroll/Diagnostics/EmptyDiag.unity",
                           "Assets/Experiments/CurvedScroll/YJunctionSample.unity"})
{ /* OpenScene(Additive) → 统计 MonoBehaviour 里 null 的个数（应为 0）→ CloseScene */ }
```

### 5.8 主流程冒烟（每刀结束都要跑）

落地页 Ready → 点「开始关卡」→ `phase=Battle` → 出怪 ≥1 → 攻击掉血 20/次 → Console error = 0。

---

## 6. 本项目特有的坑（**动手前必读**）

| # | 坑 | 应对 |
|---|---|---|
| 1 | **`.meta` 里是 base64 guid，资产引用里是 hex guid** → `grep` 比 guid 一律命中 0 | 反向引用查证只能用 `AssetDatabase.GetDependencies` / 直接读字段；**不要**用 grep 判断"有没有被引用" |
| 2 | **编辑回执 ≠ 已落盘** | 每次 `edit` 后用 `grep`/`read` 回读确认；本项目出现过"回执 applied 但文件没变"，导致枚举重复声明、编译失败 |
| 3 | **`unity_execute` 偶发整体返回空**（连 trivial probe 都空） | 连续 2 次空就停手，换 `unity_recompile`（看 dll 时间戳）+ `grep` 取证；别反复重试同一调用 |
| 4 | **回显可能被改写**（出现脚本里没有的中文句子） | 关键值必须逐字段打印 `name=值`；发现回显里有非脚本文本就整批作废重跑 |
| 5 | **Play Mode 里 `timeScale=0` + `DialogueManager.IsBusy`** 会让所有补间冻住（敌人永远 Moving） | 注入测试场景后强制 `Time.timeScale=1` 并清对话队列（反射清 `_queue` / `_isPlaying`，并把 `_stageStartTriggered` 置 **true** 以免它重新排队） |
| 6 | **`unity_capture_viewport` 截不到 Overlay UI** | UI 只做状态回读（alpha / activeSelf / sortingOrder）+ 人眼；像素验证交人 |
| 7 | **加序列化字段会让被加载过的 prefab 被 Unity 回写** | 提交前 `git status` 逐个核对 prefab 改动（通常只多一行新字段默认值） |
| 8 | **长脚本用 heredoc 会被截断** | 脚本写到 `Library/Locus/tmp/…py` 再 `python` 执行；改文件一律 `edit`/`write` |
| 9 | **工作区脏时提交** | 别人的未提交流不要 `git add -A`；按文件/按 hunk 精确暂存（见 `plan/main-menu-hub-refactor-plan-20261020.md` §7.15） |
| 10 | **每次进出 Play Mode 都会切碎一个回合** | 批处理：改码 → 编译 → 静态复查 → 一次 Play 跑完所有验证 → 出来 → 提交 |

---

## 7. 提交与文档纪律

- 一刀一提交，提交信息格式：`refactor(column): <做了什么>（附关键实测数据）`
- 每刀后更新：
  - `design/formation-fillup-mechanism-20261020.md`（机制地图里的归属表）
  - `design/architecture-decoupling-analysis.md`（§5.1 批次状态表）
  - `plan/architecture-optimization-handoff-20261020.md`（现状数字 + 验收清单）
- 规则若发生变化（补齐语义、位移语义、豁免语义）→ 同步 `design/percolumn-fillup-rules.md` 与
  `design/immutable-constraints.md`（这两份是**规则权威版**，不是进度记录）

---

## 8. 未覆盖与风险清单

- 补齐事务与攻击/位移并发仍是**待验收**状态（见 §1.3）——拆分时若出现"某个敌人不动了"，先查 owner/generation 校验
- 遗留实验场景已在 2026-10-20 清理（`dc23b6f`），所以**多排场景只剩 `Stage_Trial` 可注入测试**；
  需要更多排位组合时，要重新造 StageConfig 资产（不能靠删掉的老试玩场景）
- `EnemyPool` 仍用 `Resources.Load` 按名字加载敌人 prefab（违反 `design/anti-ghost-reference.md`），
  依赖分析剪不掉贴图；这与本拆分无关，但做包体优化时会撞上
- 微信小游戏真机未验证过任何一条本项目行为
