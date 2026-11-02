---
id: kd_1d2ddd08-c549-42b0-87de-4013efbac6ac
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 关卡配置

> 2026-11 设计文档。属于需求 **R9**（关卡波次）+ **R10**（铜钱配置），以及关卡侧的**调试与校验**。
> 解锁内容的产出端见 `design/talent-tree-architecture.md`；局内成长见 `design/in-run-growth-and-unlocks.md`。
> 现状盘点与实测证据见 `design/out-of-game-growth-v2-analysis.md`（§2.3 / §2.4 / §2.5）。

---

## 1. 定位

本文管「**玩家打什么**」：关卡清单、拓扑、波次编队、难度曲线、奖励发放、以及**怎么调试**。

| 负责 | 不负责 |
|---|---|
| 关卡清单与解锁关系 | 天赋页结构 |
| `StageConfig` 的波次 / 倍率 / 奖励配置 | 敌人自身的行为逻辑（→ `design/attack-interrupt-system.md`） |
| 难度曲线与「关卡考什么机制」 | 招式连段结构（→ `design/move-split-and-derivatives.md`） |
| 配置的校验与调试工具 | 局内三选一池 |

---

## 2. 关卡清单与解锁

### 2.1 三层数据

| 层 | 资产 / 类 | 职责 |
|---|---|---|
| **清单** | `Assets/RouteData/RouteCatalog.asset`（`RouteCatalogData`） | 选关页要列哪些关卡 + 顺序 + 「总是可选」清单 |
| **路线** | `Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset`（`FakeRouteStageConfig`） | 路线拓扑、节点列表、整关奖励、前置关卡 |
| **战斗** | `Assets/RouteData/ChangbanpoScroll/Battles/Formal_*.asset`（`StageConfig`） | 每场战斗的波次编队与倍率 |

### 2.2 解锁判定

```
StageSelectUI.IsStageUnlocked(cfg)
  ① RouteCatalogData.alwaysUnlockedStageIds  → 总是可选（教学关 / 第一个正式关）
  ② 其余：FakeRouteStageConfig.prerequisiteStageIds 里的所有前置都通关
       → SaveManager.IsStageCleared(id) → ProgressSaveSection
```

⚠️ `prerequisiteStageIds` **写在数据里**（不是代码里排顺序）→ 中间插一条关卡不会连锁改。

### 2.3 拓扑（当前）

```
N1 → E0 → J1 → { 左: EV(无战斗) → N3 ｜ 右: N2 }
```

- 左支 EV 是**纯演出节点**（无战斗）
- `FakeRouteStageConfig.clearCoinReward` = **0**（整关铜钱奖励未开）
- `prerequisiteStageIds` = `[]`（整条路线是 `stageId = 1` 的一条）

---

## 3. `StageConfig` 字段全解

`Assets/Scripts/Data/StageConfig.cs`

### 3.1 波次

```csharp
public List<WaveConfig> waves;   // 按顺序执行
```

`WaveConfig`：

| 字段 | 含义 |
|---|---|
| `waveId` | 波次序号 |
| `isBossWave` | 是否 Boss 波 |
| `rows` | `List<RowConfig>`，**按顺序从远到近生成** |
| `healthMultiplier` | 本波敌人血量倍率（1 = 不变） |
| `bossHealthMultiplier` | 仅对 `isBoss` 生效；0 表示用通用倍率 |
| `attackSpeedMultiplier` | >1 敌人攻击更频繁 |
| `damageMultiplier` | >1 玩家承伤更高 |
| `expMultiplier` | 本波击杀给的经验倍率（教学关用来控升级节奏） |
| `waveTintColor` | 敌人颜色叠加（白 = 不变） |
| `enableDynamicRush` | 存活敌人越少，补齐延迟越短 |
| `rushMoveDelay` / `rushMoveDelayMin` | 补齐移动延迟 |

`RowConfig`：

| 字段 | 含义 |
|---|---|
| `enemyIds[]` | **每个 ID 对应一列**：`enemyIds[0]` = 列0（最左）… `enemyIds[4]` = 列4（最右）。`0` = 空槽 |
| `999`（`RowConfig.RhythmGateMarker`） | 该排为**节奏门**，整排不生成敌人 |

> ⚠️ **硬规则**：`enemyIds` 的**长度决定该排有几个站位**，且数组下标**直接就是列号**（`WaveSpawner` 第 311 行注释：「直接使用数组索引作为列号」）。
> → 想让敌人在列 2，就必须写成 `[0, 0, X, 0, 0]`；写成 `[X, 0, 0]` 会把它放在**列 0**。

### 3.2 奖励

| 字段 | 含义 | 现状 |
|---|---|---|
| `killMilestones` | 累计击杀里程碑，每个阈值可配多条奖励（铜钱 / 回血 / 升级） | **正式关全部未配** |
| `clearCoinReward` | 通关奖励铜钱 | **正式关全部 = 0** |
| `collisionDamage` | 元素反应（冰系）破冰推动时的碰撞伤害 | 50 |

### 3.3 其他

| 字段 | 含义 |
|---|---|
| `stageId` / `stageName` | 关卡标识与显示名 |
| `formationConfig` | 阵型配置资产（多关共享），空则用代码默认值 |
| `rushMoveDelay` | 连续补齐移动间的延迟（每关可不同） |
| `GetTotalEnemyCount()` | 本关非零 enemyId 总数 |

---

## 4. 波次配置规范（硬规则）

来自 `design/numerical-balance-draft.md` §0.5（实测写明的强制约束）：

| 规则 | 内容 | 违反后果 |
|---|---|---|
| **规则九** | **Boss 敌人必须放在该波 `rows` 数组的最后一个 RowConfig**；Boss 之前是护卫，之后不得再有排 | Boss 在杂兵之前登场，破坏仪式感 |
| **规则十** | **远程敌人（105 随机远程 / 106 固定远程）必须部署在该波 `rows` 的后 50%**（`rows[N/2]` 到 `rows[N-1]`） | 远程当前排当前排承伤，定位错乱 |
| **规则八** | 普通波清场时长 **30–55 秒**（上限 60 秒） | 太脆或太肉 |
| **规则七** | **每波至少 1 次升级，每连续两波至少 3 次升级** | 成长断档 |
| **规则五** | Boss HP 乘数直接对准设计目标，**不叠加二次乘数** | 实际 HP 偏离 |
| **规则三** | XP 产出按 `Σ(每种敌人数 × 该敌人 ExpReward)` 算，**不可按 HP 估算** | 升级进度失真 |
| **规则四** | 增加敌人数 = 增加 XP = 加速升级 → 需同步估算等级偏移 | 难度与成长脱节 |

### 4.1 可用敌人（实测 prefab）

| 敌人 | HP | 伤害 | 攻速 | 架势 | 眩晕 | 射程 | 经验 | 机制 |
|---|---|---|---|---|---|---|---|---|
| **1011** 持剑杂兵 | 100 | 3 | 0.4 | 50 | 5s | 1 | 80 | 普通攻击，无霸体。任意攻击打断 |
| **1012** 刀兵 | 100 | 3 | 0.28 | 50 | 5s | 1 | 80 | **C 技霸体前摇**（命中帧 t=2.0）。重攻击打断 / 招架格开 |
| **105** 弓箭手 | 100 | 4 | 0.25 | 40 | 2s | 3 | 50 | 远程，落点随机 ±1.2。招架弹反 / 位移打断 |
| **109** 骑兵 | 400 | 6 | 0.4 | 50 | 5s | 1 | 100 | 冲锋循环；**骑乘期免疫眩晕位移、招架无效**。蓄力/重攻击打断 |
| **1021** 盾卫 | 150 | 10 | 0.2 | 50 | 1.5s | 1 | 50 | 盾侧/正面格挡；破盾需「反侧 + 重攻击」（方向 Random → ~1/3 概率） |
| **104** BOSS | 4500 | 10 | 0.2 | 100 | 5s | 5 | 400 | 占 5 排；**现 3 阶段**（100%/70%/30%，后两阶段霸体）；带 QTE |

> **1011 vs 1012 的唯一本质区别 = 有无 C 技霸体前摇**（1012 另有攻速更慢、命中帧更早、招架窗口更长）。
> ⚠️ **1012 在任何正式关里一次都没出现过**（只在教学关 `Tutorial_E0_Battle`）。

### 4.2 敌人编队规范（另两条）

| 规范 | 内容 |
|---|---|
| 5 列结构 | 每排 `enemyIds` 长度 ≤ 5，下标 = 列号 |
| BOSS 占位 | BOSS 占 5 排（`occupySlots = 5`）→ 必须放在最后排 |

---

## 5. 本周关卡配方

> 原则：**每一关只考「玩家当前解锁能力能解的机制」；下一关的机制必须先在上一关见过。**
> 难度增长靠**逼玩家买对的天赋**，不靠堆血量。

### 5.1 当前状态 vs 目标

| 资产 | stageId | 当前 | 目标 |
|---|---|---|---|
| `Formal_N1_Battle` | 1101 | **1 波**（3 排 × 1011 = 5 只） | 1 波 ✅ **不用改波数**，只调编队与铜钱 |
| `Formal_E0_Battle` | 1102 | **2 波** | **3 波** |
| `Formal_N2_Battle` | 1103 | 3 波（末波 104） | 不动（与 N3 功能重叠，见 §5.4） |
| `Formal_N3_Battle` | 1104 | **3 波**（末波 104 三阶段） | **5 波**，末波 **104 两阶段** |

### 5.2 配方（草案，倍率待实机调）

| 节点 | 波 | 编队（列 0→4） | 考什么 | 玩家需要 |
|---|---|---|---|---|
| **N1** | 1 | `[0,0,1011,0,0]` / `[0,1011,0,1011,0]` | 手感 +「击杀 = 铜钱」认知 | 基础招式 |
| **E0** | 1 | 3×1011 | 基础 | 基础招式 |
| | 2 | `[1012,0,0,0,0]` + `[0,0,1021,0,0]` + `[0,1011,0,0,0]` | **C 技霸体**（需 C2 重攻击）+ **盾卫**（破盾需重攻击） | **C2** + 招架 |
| | 3 | `[0,0,109,0,0]` + `[0,1012,0,0,0]` | **骑兵冲锋**（骑乘期免疫眩晕位移）→ 再次逼出 C2 | **C2** |
| **N3** | 1 | 4×1011 | 复现基础 | — |
| | 2 | 2×1012 + 2×1012 | 霸体已成常规 | C2 熟练 |
| | 3 | 2×105 + 1×109 | **随机远程** → 需要位移 / 控制 | **位移流技能**（海浪 / 旋风） |
| | 4 | 3×1021 + 2×105 | 盾阵 + 远程混合 | C2 + 位移 |
| | 5 | 护卫 + **104（两阶段）** | **BOSS 毕业考** | **C2 + C4** + 招架格 QTE |

### 5.3 配方与解锁的咬合关系（这张表是关卡设计的核心）

| 关卡元素 | 逼玩家做什么 |
|---|---|
| E0 的 **1012**（C 技霸体前摇） | 普通攻击打不动（弹刀反馈）→ 必须用 **C2** 重攻击 → 驱动「买 C2」的天赋决策 |
| E0/N3 的 **109**（骑兵） | 骑乘期免疫眩晕与位移、招架无效 → 只能靠 `interruptsCavalryCharge` 打断 → 再次强化 C2 必要性 |
| N3 的 **1021**（盾卫） | 破盾需「反侧 + 重攻击」，而重攻击受击方向是 Random（~1/3 概率）→ **C2 是目前破盾的唯一可靠手段** |
| N3 的 **105**（随机远程） | 需要位移 / 控制 → **位移流技能**的局内价值 → 驱动「买位移枝天赋」 |
| N3 的 **104** BOSS | 霸体 + QTE → 需要 **C4**（重攻击拆前摇 / 挑飞 / 附带格挡）+ **招架**（格 QTE） |

### 5.4 ⚠️ N2 / N3 功能重叠

当前拓扑右支终点是 **N2**（3 波，末波也有 104），左支终点是 **N3**。两者难度定位几乎相同 → **分支没有意义**（`design/gameplay-pillars.md` §5.4 已记录此问题）。

本周**不改拓扑**（属于另一个议题），但配方上建议把 **N3 做成真正的终点关**（5 波 + 两阶段 BOSS），**N2 降级为中段关**，让左支（EV → N3）与右支（N2）产生难度差。

### 5.5 104 两阶段

现状：`Enemy_104.prefab` 的 `bossPhases` 有 **3 个**（`BossPhaseData_104_Phase1/2/3.asset`，触发线 100% / 70% / 30%，Phase2 起 `superArmor`）。

**目标：两阶段。**

| 方案 | 做法 | 评价 |
|---|---|---|
| **A（推荐）** | 新建 `BossPhaseData_104_Phase1.asset` / `_Phase2.asset`（复用现有 Phase1 + Phase2 参数，去掉 Phase3），在 `Enemy_104.prefab` 的 `bossPhases` 里换成这两个 | 保留 4500 HP 的正式 BOSS |
| **B** | 直接用 `Enemy_108.prefab`（已经是 2 阶段） | ❌ HP 只有 500，是弱化版 BOSS，不适合当最终关 |

> `BossPhaseData` 关键字段：`phaseIndex` / `phaseName` / `triggerHealthPercent` / `attackSequence` / `qteData` / `normalAttackWeight` / `cAttackWeight` / `qteWeight` / `actionInterval` / `postQTECooldown` / `isSuperArmor` / 各招式伤害倍率 / `poiseDamageMultiplier` / `transitionTriggerName` / `transitionDuration`。

---

## 6. 铜钱与经验配置（R10）

### 6.1 收入通道

```
敌人 coinReward (10/只) ──┐
击杀里程碑 killMilestones ─┼→ PlayerState.AddCoins → session
整关 clearCoinReward ─────┘        ↓
                        StageController.SettleCoins() → 存档铜钱
                                    ↓
                        天赋树 TryUnlock 扣 coinCost
```

### 6.2 现状缺口

| 项 | 现状 |
|---|---|
| `Formal_*_Battle.clearCoinReward` | **全部 = 0** |
| `Formal_*_Battle.killMilestones` | **全部未配**（`Stage_1` 配了 5/20/50 杀 → 30/50/100 铜钱） |
| `FakeRouteStageConfig.clearCoinReward`（路线级） | **0** |
| 节点级 `rewards[]`（`FakeRouteNodeConfig`） | **字段存在但运行时从不读取** → 死字段 |
| 敌人 `coinReward` | 10/只（已在跑） |
| 局内升级「敛财 `on_kill_coin`」 | 击杀 10%/级 概率掉 1 铜钱，maxLv 5（已在跑） |

### 6.3 本周配置建议

| 项 | 建议 |
|---|---|
| `Formal_N1.killMilestones` | 3 杀 → 20 铜钱；8 杀 → 50 铜钱 + 回血 |
| `Formal_N1.clearCoinReward` | 60 |
| `Formal_E0.killMilestones` | 5 → 30；12 → 60；20 → 120 + 回血 |
| `Formal_E0.clearCoinReward` | 120 |
| `Formal_N3.killMilestones` | 8 → 40；18 → 80；30 → 150 + 回血 |
| `Formal_N3.clearCoinReward` | 200 |
| 节点级 `rewards[]` | **要么实现消费，要么先删字段**（不留死字段） |

**目标收入**：单次完整通关 **600–800 铜钱**；天赋树总定价约 **2850** → **约 4 次通关点满**。

> 定价与收入的详细推导见 `design/out-of-game-growth-v2-analysis.md` §4.2 / §7.4。

---

## 7. 现成工具链与测关卡流程

> ⚠️ **本周不开新工具**。项目里已有一套完整的「配表 → 跑测 → 分析 → 调参」工具链，本节只做**复用与接线**。

### 7.1 已有工具（全部复用）

| 工具 | 位置 | 能力 |
|---|---|---|
| **跑关机器人** | `Assets/Scripts/Utils/BattleAutoPlayer.cs`（524 行） | 挂到场景任意对象 → Play → 勾 `autoStart`（或按 **P** 键）→ 自动开打。**走手势路径**（`InputManager.SubmitGestureForDebug`）而不是直接调 `TryExecuteAttack` → 与玩家同一条路，连招能正常接段。**跨节点续跑**：一场战斗结束后不退出，接下一节点，直到整关跑完或按 P 停。波次边界接 `WaveSpawner.OnWaveStarted/OnWaveCompleted`（不用「存活数变化」推断，否则会把一波切成好几段）。Boss QTE 可选 `Suppress`（关掉，量干净 DPS）/ `Auto`（自动作答，更接近会玩的玩家）。参数：`attackInterval` / `waveGap` / `useAreaAttacks` / `useHeavyAttacks` / `heavyChance` / `preferredRouteChoiceId` / `recordTelemetry` |
| **埋点** | `Assets/Scripts/Utils/LevelTelemetry.cs` | 写 `Library/Locus/telemetry/run-*.jsonl`（JSONL，一行一条）。事件：`battle_begin` / `wave_begin` / `wave_end` / `battle_end` / `hp_changed` |
| **埋点分析** | `Tools/analyze_level_telemetry.py` | 逐波清场时长 / 剩余敌人 / 掉血 / 等级 → 对照「普通波 30–55s、Boss 波 45–90s」；经验曲线核对（铁律七：每波 ≥1 次升级、连续两波 ≥3 次）；难度墙检查。参数：`--csv out.csv` / `--latest` |
| **难度表生成** | `Tools/build_first_level_difficulty_table.py` | 从**资产实测**（节点/波次现状 + 敌人 prefab + 经验曲线 + 铁律）生成 xlsx → `Locus/knowledge/plan/first-level-difficulty-20261025-v2.xlsx`。6 个 sheet：波次明细 / 敌人能力 / 玩家成长 / 配对与铁律 / 待决与风险 / **无聊根因与解法**。**只产出表格，不改任何游戏资产**；输出被占用时自动写 `-v2` |
| **关卡配置编辑器** | `Assets/Scripts/Editor/WaveConfigEditor.cs`（313 行） | `RowConfigDrawer` / `WaveConfigDrawer` / `StageConfigEditor` —— 在 Inspector 里可视化编队（比手改 YAML 安全） |
| **教学关跑测驱动** | `Assets/Scripts/Editor/TutorialFlowDriver.cs`（257 行） | 教学关全流程自动驱动 |
| **清单校验** | `Assets/Scripts/Editor/RouteCatalogValidator.cs`（菜单「Tools/数据校验/关卡清单」） | 校验选关清单 |
| **天赋树校验** | `Assets/Scripts/Editor/TalentTreeValidator.cs`（菜单「校验天赋树」） | 校验天赋树 |
| **战斗数值总表** | `Assets/Scripts/Editor/CombatDataTableWindow.*.cs` | 敌人 / 技能 / 三选一升级定义三个 tab 的表格化编辑 |
| **难度调试关卡** | `Assets/Resources/StageConfigs/`（`Stage_1` / `Stage_Trial` / `Stage_BossRushTest` / `Stage_WaveMarchBugTest` / `QTE_V2_TestStage`） | 调参专用关卡 |

### 7.2 测关卡与调参流程（5 步，沿用 `plan/bot-and-level-tuning-handoff-20261025.md`）

```
① 定成长缺口（最大的杠杆，其余数值都跟着它）
      ↓
② 按「无聊根因与解法」重排波次
      （Excel 的「无聊根因与解法」页 —— 编排问题，不是数值问题）
      ↓
③ 落配置：Formal_N*.asset（waves / healthMultiplier / damageMultiplier）
         + Node_*.asset（battleEntries）
      ↓
④ 量：机器人跑一遍拿「下限」，真人跑一遍拿「上限」
      ↓
⑤ 重跑难度表脚本（改完配置必重跑，Excel 里的建议值要跟着资产走）
```

### 7.3 机器人怎么用（操作步骤）

| 步 | 操作 |
|---|---|
| 1 | 把 `BattleAutoPlayer` 组件挂到场景任意对象 |
| 2 | `autoStart = true`（或 Play 后按 **P** 启动 / 停止） |
| 3 | Boss QTE 选 `Suppress`（量机械 DPS）或 `Auto`（量接近会玩的玩家） |
| 4 | 路线分叉用 `preferredRouteChoiceId` 指定走哪条 |
| 5 | 跑完看 `Library/Locus/telemetry/run-*.jsonl` |
| 6 | `python Tools/analyze_level_telemetry.py` → 摘要 + 逐波 ✔/⚠ 判定 |

> ⚠️ **机器人会接管输入**（自己出招）→ 只在调试关卡里用。
> ⚠️ **机器人不是「平均玩家」**：它不躲、不用招架、不走位 → 耗时偏乐观 → **它是「下限探针」，不是基准线**。

### 7.4 埋点字段（分析脚本按这个口径判定）

| 事件 | 字段 |
|---|---|
| `battle_begin` | label(=战斗配置名)、hp、level、upgrades、time |
| `wave_begin` | battle、wave、enemies、composition（如 `1011x2 1021x1`）、hp、level、upgrades |
| `wave_end` | wave、duration、hpLost、alive、usedHeavy、level、upgrades |
| `battle_end` | victory、duration、waves、hpLost、deaths、level、upgrades、coins |

**已知口径坑**

| 坑 | 说明 |
|---|---|
| 波次边界必须接 `WaveSpawner.OnWaveStarted` | 用「存活数变化」推断会把**一波切成好几段**（第一版就是这么错的） |
| 出波事件触发时敌人还没实例完 | 统计要**延后一帧** |
| `hpLost` 目前按波次近似 | 每波开始时快照 HP；逐次掉血要走 `PlayerState.OnHealthChanged`（**未接**） |

### 7.5 ⚠️ 难度表脚本的基线已漂移（**跑之前必须先修**）

`Tools/build_first_level_difficulty_table.py` 自称「数据全部来自资产实测」，但它的内置基线停在 2026-10-25，**资产后来改过，脚本没同步**：

| 项 | 脚本内置 | 当前实测 | 状态 |
|---|---|---|---|
| 1011 | hp=200 atk=3 | **hp=100** atk=3 | ⚠️ 过期 |
| 109 | hp=500 atk=10 | **hp=400 atk=6** | ⚠️ 过期 |
| 105 | hp=200 atk=5 | **hp=100 atk=4** | ⚠️ 过期 |
| 1021 | hp=150 atk=10 | hp=150 atk=10 | ✅ |
| 波次现状 | N1 5 波 / E0 5 波 / N2 6 波 / N3 6 波 | **N1 1 / E0 2 / N2 3 / N3 3** | ⚠️ 过期 |

→ **本周第一步就要把脚本的 `ENEMY` 表与波次基线重新对资产**，否则生成的「CURRENT」列是错的，Excel 的建议值会把人带偏。
→ 脚本输出 `first-level-difficulty-20261025-v2.xlsx`（已有）与 `first-level-difficulty-20261025.xlsx`（已有），文件名带日期 → **重跑后应改名为当前日期**，否则新旧难分。

### 7.6 已知现场问题（关卡配置直接相关）

| # | 问题 | 处理 |
|---|---|---|
| 1 | **E0 波次顺序跟教学台词错位** | 台词 wave3=破盾(1021)、wave4=骑将(109) → 1021 应在第 0 波、109 在第 1 波（现状错位） |
| 2 | **N3 倍率全是 ×0.1（占位）** | 重算 |
| 3 | **N1 的第 0 条是开发用 `Test_Node_Battle`** | 删不删待拍板 |
| 4 | **「关卡无聊」** | 根因在 Excel「无聊根因与解法」页 —— 是**编排问题**，不是数值问题 |
| 5 | **105 一戳就死 → 远程压力 = 0 → 招架没有使用场景** | 血量已提过一轮（文档记 8→40），当前实测 100，需重新确认 |
| 6 | **敌人被连续命中后不再攻击**（已修，但要知道） | 玩家连续平A 仍能近乎无代价打死敌人 → 这是「难度失真」的来源之一，调难度时必须心里有数 |
| 7 | **Pierce / Sweep 已从 `Hero_Zhangfei.skillConfigs` 卸下** | 「蓄力后直接松手」现在没招式可出（指向突刺链 C1→C2 仍可用） |

### 7.7 工作纪律（沿用项目规矩）

| 纪律 | 说明 |
|---|---|
| 改数据后**跑校验**；改完配置**重跑难度表脚本** | |
| 本机 Editor Play Mode **帧率极低（2~5fps）** | 按**帧数**规划等待，**别用秒数**；进出 Play Mode **批处理**（一次进、一次出跑完所有验证项） |
| 验证会**污染存档** | `SaveManager` 直写 PlayerPrefs → **验前备份、验完还原**（落地页的「清除存档」按钮也能一键清） |
| Unity 资产编辑 | **改 prefab 前先备份、存盘后回读磁盘**（见 `memory/project-mistake-note.md`） |
| 文档与代码不一致时 | **以代码与资产实测为准** |
| 每批改动完成即提交推送 | `Locus/workspace-trees/default.json` 与 `~$*.xlsx` **不提交** |

---

## 8. 验收口径

| # | 检查 | 方式 |
|---|---|---|
| 1 | N1 恰好 1 波、E0 恰好 3 波、N3 恰好 5 波 | 打开资产核对 |
| 2 | 每个 Boss 波的 BOSS 在**最后一个 RowConfig** | 难度表脚本 + 实机 |
| 3 | 每个含 105 的波，105 都在后 50% 排 | 难度表脚本 + 实机 |
| 4 | 每排 `enemyIds` 下标与预期列一致 | `WaveConfigEditor` 目视 + 实机 |
| 5 | 每波至少 1 次升级、每连续两波至少 3 次 | `analyze_level_telemetry.py` 的经验核对 |
| 6 | 普通波清场 30–55 秒 | **机器人跑下限 + 真人跑上限**，夹住目标 |
| 7 | BOSS 战 45–90 秒 | 同上 |
| 8 | 104 恰好 2 阶段，转阶段线与霸体正确 | 实机 |
| 9 | 单次完整通关铜钱 600–800 | 机器人 `battle_end.coins` + 真人复核 |
| 10 | 铜钱在结算后进入存档，且**能在天赋树花** | 实机 |
| 11 | 关卡清单校验零问题 | 菜单「Tools/数据校验/关卡清单」 |
| 12 | **难度表已重跑**，且脚本基线已与当前资产对齐 | 检查 `ENEMY` 表 + 波次基线 + xlsx 日期 |

---

## 9. 参考

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Data/StageConfig.cs` | 关卡配置 |
| `Assets/Scripts/Data/FakeRouteStageConfig.cs` / `FakeRouteNodeConfig.cs` | 路线与节点 |
| `Assets/Scripts/Data/RouteCatalogData.cs` | 选关清单 |
| `Assets/Scripts/UI/StageSelectUI.cs` | 选关页与解锁判定 |
| `Assets/Scripts/Core/KillRewardManager.cs` | 击杀里程碑发放 |
| `Assets/Scripts/Managers/StageController.cs` | 铜钱结算（`SettleCoins`） |
| `Assets/Scripts/Wave/WaveSpawner.cs` | 波次生成（enemyIds 下标 = 列号） |
| `Assets/Scripts/Data/Save/ProgressSaveSection.cs` | 通关记录 |
| `design/numerical-balance-draft.md` | 硬规则与数值方法论 |
| `design/gameplay-pillars.md` §5 | 路线与节点机制 |
| `design/out-of-game-growth-v2-analysis.md` | 现状盘点与实测证据 |
