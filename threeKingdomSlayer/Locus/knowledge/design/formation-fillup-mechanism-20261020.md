---
id: kd_abf56691-6570-4e9f-ae24-3be5c94452b3
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 阵型/补齐/位移机制地图（2026-10-20）：澄清 PerColumn 并非纯遗留（11 个资产在用，含现役 N1_Battle/E0_Battle_Preview/Stage_1），两条规则都不留空；「让敌人无视空排补齐」的正确落点是订单执行资格而非创建侧；给出 ColumnManager 按调度语义拆成 5 刀的方案与顺序。
---

---
summary: 阵型/补齐/位移机制地图（2026-10-20）：澄清 PerColumn 并非纯遗留（11 个资产在用，含现役 N1_Battle/E0_Battle_Preview/Stage_1），两条规则都不留空；「让敌人无视空排补齐」的正确落点是订单执行资格而非创建侧；给出 ColumnManager 按调度语义拆成 5 刀的方案与顺序。
---

# 阵型 / 补齐 / 位移：机制地图与拆分方案

> 目的：在动 `ColumnManager`（2130 行）之前，先把机制关系与相关系统理清。
> 权威规则见 `design/percolumn-fillup-rules.md`（补齐规则权威版）与
> `design/immutable-constraints.md`（不可变约束 8 条）。本文是**读代码后的结构说明 + 拆分方案**。

---

## 1. 先纠正一个前提：`PerColumn` 不是纯遗留

| 事实 | 数据 |
|---|---|
| 资产取值分布 | `fillUpRule: 0`（PerColumn）**11** 个 / `fillUpRule: 1`（PerRow）**12** 个 |
| 其中**现役**关卡用 PerColumn | `RouteData/Stage01/N1_Battle.asset`、`RouteData/ChangbanpoScroll/Battles/E0_Battle_Preview.asset`、`Resources/StageConfigs/Stage_1.asset` |
| 其余 PerColumn 资产 | 4 个试玩数据（`RouteTrialData/*`、`TrialBattle`）+ `J0_Battle` / `N0_Battle` / `N1_ArtPreview_Battle` |
| 代码里的分支点 | `WaveSpawner.cs:176/178/200/256`、`Enemy.cs:924`、`StageFormation.cs:78`（默认值是 PerColumn） |

> 文档里说的「旧 PerColumn 兼容模式仍保留历史扫描入口」指的是**代码路径**是遗留的，
> 但**现役内容仍在用这条路径**——所以它既是技术债也是内容依赖。

---

## 2. 两条规则路径的实际差异

```
StageConfig.fillUpRule
├─ PerRow（新，权威版）                          ├─ PerColumn（遗留扫描）
│  WaveSpawner:                                   │  WaveSpawner:
│   ① ConfigureWaveRowLayout(注册逻辑排队列)        │   ① ClearWaveRowLayout（不注册队列）
│      队列元素 = 活人排 / 普通空排占位符 / 999 门  │   ② SpawnRow：敌人【直接生成在目标排】
│   ② SpawnRow：敌人【先生成在目标后方两排】        │   ③ StartWaveMarch：legacy 扫描入口
│   ③ StartSpawnEntry：SpawnEntry 订单入场          │      （按列找空位往前压）
│      ※ 不改逻辑队列、不删空排、不开 999 门        │      ※ 立即执行一次
│  ④ 整排清空 → 逻辑队列差分 → WaveMarch 订单       │  ④ 整排清空 → legacy 扫描补齐
│  ⑤ 敌人按战斗状态决定何时执行（规则2）            │  ⑤ 同上
└──────────────────────────────────────────────  └──────────────────────────────────────────────
```

**关键**：两条规则**都不留空**。PerColumn 是「按列找空位前压」，PerRow 是「逻辑队列前移 + 前导空排移除」——都会把空位填掉。
**所以「删掉 PerColumn」并不会让场上出现空位。**

> 已失效的遗留入口（当前是 inert，只打警告）：
> `ColumnManager.RowBasedFillUp`（:1525 "legacy compaction is disabled"）、
> `ColumnManager.CompactAllColumns`（:2100 "legacy displacement compaction is disabled"）。

---

## 3. 机制关系图（谁产生、谁执行、谁读）

```
                    ┌──────────────── 配置层（Data） ────────────────┐
                    │ StageConfig.fillUpRule / waves / RowConfig      │
                    │ RowConfig.IsRhythmGate（999）                   │
                    │ FormationConfig / RowFormation（几何，已在 Data）│
                    └───────────────┬───────────────────────────────┘
                                    │
        ┌───────────────────────────┴───────────────────────────┐
        │ 波次生成                                              │
        │ WaveSpawner → 敌人生成（目标排 or 后方两排）              │
        │   ├─ 注册逻辑排布局（PerRow）                            │
        │   └─ 创建共享血量组 CreateSharedHealthGroups             │
        └───────────────┬───────────────────────────────┘
                        │
        ┌───────────────▼────────────────────────────────────┐
        │ ColumnManager：槽位 + 逻辑队列 + 调度（唯一创建者）      │
        │                                                  │
        │  逻辑排队列 _logicalWaveRows / _logicalRowsByInitialRow│
        │    · 活人排 / 普通空排占位符 / 999 节奏门              │
        │    · _openedRhythmGateRows（门开关状态）              │
        │                                                  │
        │  调度订单（owner + generation + 精确目标槽）           │
        │    SpawnEntry  → 入场（不改队列）                     │
        │    WaveMarch   → 整排清空后的补齐（改队列）            │
        │    PushReturn  → 击退精确原槽回位（不改队列）          │
        │    Boss        → Boss 独立推进（不改队列）              │
        │    DirectionalPush → 横向位移（只改 column）           │
        │                                                  │
        │  查询层（只读）：GetAllEnemiesInRange / GetEnemiesInRange│
        │    GetEnemyAt / GetFrontEnemy / GetEnemiesInColumn ... │
        │  骑兵槽位预留：_cavalryReservedByEnemy / _cavalryReservedSlots│
        └───────────────┬────────────────────────────────────┘
                        │
   ┌────────────────────┼──────────────────────────────────────┐
   │ 消费方（只读为主）                                            │
   │  AttackSystem（31 处调用：范围查询/前排敌/列内查询）            │
   │  WaveSpawner（6）、UpgradeEffectManager（4）、CycloneZone（2） │
   │  ActiveSkillRunner（2）、InputManager / Enemy / CycloneItem（1）│
   │                                                         │
   │ 消费方（写）                                                  │
   │  WaveSpawner：Configure/ClearWaveRowLayout、StartSpawnEntry、   │
   │               StartWaveMarch、TriggerAllBossFillForward        │
   │  位移效果：ApplyDirectionalPush、ApplyPushWave、ApplyBreakIcePush│
   │           （击退/聚拢/破冰顶开各自语义）                       │
   └─────────────────────────────────────────────────────────┘
```

**外部只用到 21 个方法名**，其中 13 个是纯查询（`GetAllEnemiesInRange` 13 处、`GetEnemiesInRange` 7 处…），
8 个是写操作（4 个调度入口 + 3 个位移 + 1 个查询）。**消费者面比想象中窄。**

---

## 4. 拆分方案：按「调度语义」切，不按文件切

`RushMoveOrderOwner` 已经把四种语义编码好了（`SpawnEntry` / `WaveMarch` / `PushReturn` / `Boss`），
文档也要求「普通补齐、击退回位、横向位移、Boss 推进必须使用不同调度语义」——但它们现在**全挤在一个类里**，
字段上就已经能看出耦合：`_logicalWaveRows`（队列）、`_plannedWaveTargetRows` / `_activeWaveTargetSlots`（WaveMarch）、
`_pausedWaveEnemies`（WaveMarch 暂停）、`_spawnEntryEnemies`（入场）、`_pushReturnTransactions`（击退回位）、
`_cavalryReservedSlots`（骑兵）、`_rowEnemies`（槽位）共 30+ 字段。

建议 5 刀，**每刀都是「搬到新类 + ColumnManager 转发」，不改行为**：

| 刀 | 抽出 | 内容 | 风险 | 验收 |
|---|---|---|---|---|
| 1 | `LogicalRowQueue` | 逻辑排队列（活人排/空排占位符/999 门/门开关/前导空排移除）——**纯数据 + 队列操作，可脱离场景单测** | 低 | 队列操作单测；PerRow 关卡阵型与推进不变 |
| 2 | `ColumnQueries` | 13 个只读查询（范围/列内/前排/列敌数…）；`AttackSystem` 等 31 处调用改指向它或经 ColumnManager 转发 | 低 | 攻击命中判定与 QTE 选目标不变 |
| 3 | `SpawnEntryMover` | 入场调度（`_spawnEntry*` + `StartSpawnEntry` + SpawnEntry 订单执行） | 中 | 波次入场观感不变（用户已验收过的流程） |
| 4 | `WaveMarchScheduler` | 补齐调度（`_planned/_active/_paused*` + `StartWaveMarch` + 差分规划） | 中高 | 整排清空推进、999 门开启、地刺同步死亡等**待验收项**要回归 |
| 5 | `PushReturn / DirectionalPush` | 击退精确原槽回位 + 横向位移（`_pushReturnTransactions` / `_pushByColumn` 等） | 中高 | 击退/聚拢/破冰顶开三条位移路径逐个验 |

**顺序理由**：1/2 是纯数据与纯读，可独立测；3 是独立调度；4/5 风险最高，放在最后，此时前面已经建立了可回归的骨架。
另外：**几何部分已经在 Data**（`RowFormation.GetColumnOffsetX`、`FormationConfig`），不用重复搬。

### 4.1 拆分期间必须守住的约束（`immutable-constraints.md` 8 条）

1. 位移不可重叠（目标槽校验 + Boss 墙）
2. 未受位移者保持原位
3. 普通补齐唯一触发条件＝整排清空
4. 击退回位必须回精确原槽
5. 攻击打断边界
6. **调度所有权**：不同语义用不同订单；禁止敌人自行创建移动；订单完成校验 owner+generation
7. 配置验证只在波次生成时做
8. 聚拢冲突用自己的裁决规则

> 尤其第 6 条：拆完之后，「谁能创建订单」必须仍然唯一（`ColumnManager` 或它的继任者），
> 否则就回到「敌人根据攻击范围自行创建移动」的老 bug（105 远程敌人永远不补齐）。

---

## 5. 「让敌人无视空排补齐」怎么做（用户提出的方向）

### 5.1 关键判断：它与「删 PerColumn」是两件事

- 删 PerColumn **不会**产生空位（见 §2：两条规则都不留空）。
- 「无视空排补齐」= **允许某些敌人不参与补齐** → 落点在**订单执行资格**层，不在规则选择层。

权威文档「规则2」已经单独定义了这一层：

| 敌人状态 | 对**已存在**的补齐订单 |
|---|---|
| 未进入攻击范围 | 执行 |
| 已进入攻击范围且前方条件不要求移动 | 留在原位攻击 |
| 正在攻击动画中 | 等收招后再确认订单与目标槽 |
| 攻击冷却中 | 可结束冷却并执行订单 |
| 眩晕 / 击飞 / QTE | 状态结束后**只恢复仍有效的既有订单**，不得自行创建订单 |

→ 「无视补齐」的正确实现 = 给敌人一个**豁免标志**（如 `Enemy.ignoreFillUpOrders`），
在**执行**阶段跳过，且**不影响订单创建**。

### 5.2 为什么绝不能实现在「创建」层

历史 bug「105 远程敌人永远不补齐」的根因，就是订单创建阶段按 `attackRange` 过滤了源排成员。
如果在创建阶段过滤豁免者，会重蹈覆辙。

### 5.3 必须先定的一个语义点

**豁免者算不算「该排存活占位」？**

- 若**算占位**：它留在原位 → 该排永远不算清空 → 整条逻辑队列卡住（后面的人永远补不过来）。
  除非它被击杀/推走，队列才会继续。
- 若**不算占位**（视为空位）：整排清空判定会把它排除 → 队列正常推进，但**会出现真正的空排**，
  这就等于把「空排」重新引入为运行时概念——而权威文档里空排只来自**配置**（`(0,0,0,0,0)` 占位符）与 **999 门**。

→ 建议：**豁免 = 该敌人自己不执行移动，但整排清空判定按「它仍然占位」处理**（与 Boss 墙类似：它挡着，
但它自己不动）。这样「无视补齐」不会破坏队列推进逻辑，代价是那一排后方确实补不过来——这恰好就是
「无视空排补齐」想要的观感。**若你要的是「队列照常推进、只是它不动」，那它必须算占位（上面这条）；
若你要「它周围照常推进」，那它必须不算占位，等于引入运行时空排——需要单独设计。**

### 5.4 「让敌人无视空排补齐」怎么做 —— ✅ 已实现（`b3c382b`）

**落点**：`Enemy.ignoreFillUpOrders`（Inspector 可按 prefab 预设，也可运行时由效果置位）。
生效位置在 `ColumnManager` 的补齐**规划/派单**层（`TryBuildLogicalWaveMarchPlan` 与遗留
`BeginWaveStep(sourceRow,…)`）：被豁免的敌人**不获得目标行、不预留目标槽、不派单**。

**实测语义**（注入 `Stage_Trial` 第 1 波：6 排含一个空排占位符、5 个敌人）：

| | 杀前 | 杀掉 row0 后 |
|---|---|---|
| 基线 | `col2/r0 · col1/r1 · col1/r3 · col2/r4 · col3/r5` | `col1/r0 · col1/r2 · col2/r3 · col3/r4` |
| 豁免（col1/r1） | 同上 | `col1/r1[豁免] · col1/r2 · col2/r3 · col3/r4` |

即：**被豁免的敌人不前移填补空排；同排其他敌人照常前移；它仍算占位**
（与「击退留下的空位不传给未受击敌人」同一口径）。

**为什么落在规划层而不是执行层**：若只在执行层跳过，已预留的目标槽会悬空
（永远占着却没人去填），后续规划会被它卡住。从源头不派单，不留悬空预留。

> ⚠️ 与历史缺陷「105 远程敌人永远不补齐」的区别：那次是**按状态误过滤**（本该动的敌人拿不到订单），
> 这里是**设计意图的排除**（这个敌人本来就不该动）。代码里已写明区别。

---

## 6. 与结构线的关系

- 本文件是 `design/architecture-decoupling-analysis.md` §5 批次 2（`ColumnManager` 拆分）的**前置分析**；
  拆分按 §4 的 5 刀推进，每刀独立提交 + 独立回归。
- 拆完后，「阵型/补齐/位移」这套机制就有了明确的归属：
  几何在 `Locus.Data`、队列在 `LogicalRowQueue`、查询在 `ColumnQueries`、
  调度在 `SpawnEntryMover` / `WaveMarchScheduler` / 位移回位类 —— 与文档 §4.1 的分层一致。
