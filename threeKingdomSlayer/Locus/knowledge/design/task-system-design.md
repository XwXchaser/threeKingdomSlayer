---
id: kd_1d50d4d6-663e-4e98-8197-d01c225be798
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 任务系统设计（内容形态 + 数据架构）

> 2026-10-24 需求：落地页左下要有一条「任务」信息（参考手游 / 小游戏的做法），任务内容将来会非常丰富 →
> **先把任务内容与数据架构规划出来**，再谈表现。
> 相关：`plan/hub-layout-rework-20261024.md`（落地页排布）、
> `design/out-of-game-growth-system.md`（局外成长三层）、`design/game-mechanics.md`（系统实现地图）、
> `design/architecture-decoupling-analysis.md`（结构问题清单：隐式全局网是其中之一）。

---

## 一、边界：什么算「任务」，什么不算

| 系统 | 归属 | 说明 |
| --- | --- | --- |
| 击杀里程碑 `KillRewardManager` | **局内成长** | 单局内击杀达阈值给局内资源产出。**任务系统不要重复实现「击杀 N 给奖励」** |
| 三选一升级 / 连击 buff / 经验三选一 | **局内成长** | 单局内的构筑与数值 |
| 关卡进度 `ProgressSaveSection` | **关卡状态** | 「打到哪了」，不是「目标」 |
| 天赋树 `TalentTreeState` | **局外永久解锁** | 任务可以作为**解锁途径**给天赋 |
| **任务系统（本设计）** | **局外目标** | 长期目标 / 每日 / 成长 / 收集 / 挑战 / 成就，奖励走局外资源（铜钱、道具、天赋解锁、外观） |

> 一句话：**任务 = 局外的「做什么 → 拿什么」**；局内变强不写成任务。

---

## 二、内容形态：先列全，再决定做哪些

手游 / 小游戏的常见任务类别，以及我们是否需要（★ = 建议优先）：

| 类别 | 典型描述 | 我们要吗 | 备注 |
| --- | --- | --- | --- |
| 主线 / 章节 | 「通过长坂坡第一章」 | ★ | 跟关卡推进绑定，可由关卡系统自动发 |
| 日常 | 「今日击杀 30 个敌人」 | ★ | 每日重置，量大、门槛低 |
| 周常 | 「本周通关 5 关」 | ★ | 每周重置，奖励更好 |
| 成长 / 累计 | 「累计获得 5000 铜钱」 | ★ | 账号累计，永不重置，引导长线 |
| 收集 / 图鉴 | 「收集 3 种敌人的情报」 | ★ | 需要「去重计数」语义 |
| 挑战 | 「单局打出 20 连击」「只用一招通关」 | ★ | 需要「峰值 / 条件达成」语义 |
| 限时活动 | 「活动期内完成 X」 | ○ | 需要活动窗口与独立奖励池 |
| 成就 | 「首次击败 104 Boss」 | ○ | 本质是永不重置的一次性任务 |
| 签到 | 「每日登录领奖」 | ○ | 独立日历玩法，与战斗无关 |

**结论**：前 6 类覆盖绝大多数需求，且可以**用同一套「计数器 + 阈值 + 重置策略」模型表达**；
后 3 类是同一模型上的附加字段（窗口期 / 日历），不新开体系。

---

## 三、核心模型：一切归约为「计数器 + 阈值」

### 3.1 计数器语义（CounterKind）

| kind | 含义 | 例子 |
| --- | --- | --- |
| `Cumulative` | **累计计数**（累加） | 累计击杀数、累计通关数、累计铜钱 |
| `Peak` | **历史峰值**（取最大值） | 最高连击数、最高波数、单局最高伤害 |
| `Distinct` | **去重集合大小** | 收集到几种敌人 / 几种道具 |

> 只有这三种。新增任务类型时**不要新增计数器语义**，只新增计数器 id。

### 3.2 计数器作用域（Scope）

| scope | 含义 | 重置时机 |
| --- | --- | --- |
| `Run` | 单局内 | 每局结束 |
| `Stage` | 单关内 | 换关卡 |
| `Account` | 账号累计 | 永不（除周期任务） |
| `Daily` / `Weekly` | 周期内 | 每日 / 每周重置时刻 |

### 3.3 重置策略（ResetPolicy）

`Never`（永不）/ `Daily` / `Weekly` / `OnStageClear`（每过一关清零，用于「本关内做到 X」）。

---

## 四、事件源：战斗里的事怎么变成计数

### 4.1 事件总线（唯一的入口）

```
TaskEvent { string eventId; string subjectId; int value; }
```

- `TaskEventHub`：**纯静态类**，放在服务层（`Locus.Services`），**不做 MonoBehaviour 单例**。
- 只允许**任务系统**订阅；业务代码只负责**发布**事件，不直接碰任务逻辑。
- **发布点必须显式**（写在明确的状态迁移处），不做全场景轮询广播——
  否则会重演 `design/architecture-decoupling-analysis.md` 里点名的「隐式全局网」。

### 4.2 事件清单（第一版）

| eventId | subjectId | value | 发布点（候选文件） |
| --- | --- | --- | --- |
| `enemy_killed` | enemyId | 1 | `Enemy.Combat`（死亡结算处） |
| `boss_killed` | bossId | 1 | 同上（`isBoss`） |
| `stage_cleared` | stageId | 1 | `FakeRouteRuntime.FinishRoute` / `StageRunFlow` |
| `node_entered` | nodeId | 1 | `FakeRouteRuntime` 进入节点处 |
| `run_started` / `run_ended` | stageId | 成功=1/失败=0 | 战斗开始 / 结束 |
| `wave_reached` | — | 波数 | `WaveSpawner` / 敌人生成处 |
| `combo_changed` | — | 当前连击 | `ComboManager`（Peak 计数用） |
| `move_used` | moveId | 1 | `AttackSystem`（招式释放处） |
| `skill_used` | skillId | 1 | `ActiveSkillRunner` |
| `upgrade_chosen` | upgradeId | 1 | `UpgradeChoiceManager.ConfirmChoice` |
| `item_gained` / `item_used` | itemId | 数量 | 道具系统 |
| `coins_gained` | — | 数量 | `SaveManager.AddCoins` 调用处 |
| `talent_unlocked` | talentNodeId | 1 | `TalentTreeState.TryUnlock` |
| `task_claimed` | taskId | 1 | 任务领奖处（供「完成任务」类任务） |

> **计数器 id 与事件 id 是两套词表**：计数器 id（`kill_enemy` / `clear_stage` / `collect_enemy_type` …）
> 由任务系统定义并登记；事件 id 由战斗侧发布。两者的映射写在计数器定义里。

### 4.3 计数器定义（词表）

```
TaskCounter { counterId; counterKind; eventId; subjectFilter; scope; }
```

例：
- `kill_enemy` = Cumulative + 事件 `enemy_killed` + 作用域 Account
- `kill_enemy_1011` = Cumulative + 事件 `enemy_killed` + subject 过滤 `1011`
- `clear_stage` = Cumulative + 事件 `stage_cleared`
- `max_combo` = Peak + 事件 `combo_changed`
- `collect_enemy_type` = Distinct + 事件 `enemy_killed`（按 subject 去重）

---

## 五、数据资产

### 5.1 `TaskDefinition`（ScriptableObject，一条任务）

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| `taskId` | string | 唯一 id（存档按它记账） |
| `title` / `description` | string | 文案 |
| `icon` | Sprite | 图标（Inspector 拖拽，守 `design/anti-ghost-reference.md`） |
| `categoryId` | string | **类别 id**（见下） |
| `counterId` | string | 用哪个计数器 |
| `targetValue` | int | 阈值 |
| `resetPolicy` | string | `never` / `daily` / `weekly` / `onStageClear` |
| `rewards` | List\<TaskReward\> | 奖励 |
| `prerequisiteTaskIds` | List\<string\> | 前置任务（可多个 → 支持分支） |
| `autoClaim` | bool | 完成即自动发奖（日常任务常见） |
| `displayOrder` | int | 排序 |
| `hidden` | bool | 是否在列表里隐藏（后台累计类任务） |
| `windowStart` / `windowEnd` | string | 限时活动窗口（可空） |

> **类别用 string id 而不是枚举**：手游任务种类会持续增加，枚举加一种要改代码 + 迁所有资产
> —— 与项目里 `BuffType` / `TalentUnlockKind` 退役的教训一致。词表放 `TaskCategories`（静态类，只读）。

### 5.2 `TaskReward`

`type`（string id：`coin` / `item` / `talentUnlock` / `exp`…）+ `amount` + 可选引用
（`itemId` / `talentNodeId`）。奖励发放走现有 API：`SaveManager.AddCoins` / `ItemInventory` /
天赋解锁载荷（复用 `TalentNodeConfig` 的 `moveConfig` + `flagId` **载荷范式**，不新造枚举）。

### 5.3 `TaskCatalog`（ScriptableObject，任务清单）

- `tasks: List<TaskDefinition>`（全部任务）
- `dailyResetHour` / `weeklyResetWeekday`（周期重置时刻）
- `Validate()` → `List<string>`：taskId 重复 / counterId 不在词表 / 前置不存在 / 前置成环 / 奖励类型非法 / targetValue ≤ 0
- 校验菜单：`Tools/数据校验/任务表`（与现有 `Tools/数据校验/关卡清单`、`天赋树` 同构）

### 5.4 存档

- `SaveData` 新增字段（**字段名属于中心模型，改名会毁旧档**）：
  `taskCounters`（counterId → value 的列表）、`taskProgress`（taskId → value）、`claimedTaskIds`、
  `taskPeriodStamp`（周期起始时间戳）。
- 新增 `TaskSaveSection : ISaveSection` 负责这一片的迁移（幂等），并在 `SaveSections` 登记。
  → **不改中心模型结构之外的东西**（分片契约见 `Data/Save/ISaveSection.cs`）。

---

## 六、运行时

| 件 | 形态 | 职责 |
| --- | --- | --- |
| `TaskEventHub` | 服务层静态类 | 发布 / 订阅 `TaskEvent` |
| `TaskService` | 服务层静态类 | 收事件 → 更新计数器 → 判断完成 → 发奖 → 周期重置 |
| `TaskProgressView` | UI 组件 | 落地页左下那条（`HubTaskInfoBar`）：显示置顶任务 |

### 6.1 置顶规则（落地页只显示一条）

1. 手动置顶（运营 / 剧情需要）优先；
2. 其次「进行中且即将完成」（剩余量最小）；
3. 再次「未领取的已完成任务」；
4. 再次按类别优先级：主线 > 日常 > 周常 > 成长 > 收集 > 挑战；
5. 都没有 → 显示「暂无任务」占位。

### 6.2 边界与安全

- 事件发布点必须是**明确的状态迁移处**，不做轮询、不做全局广播。
- 计数器字典只存**有变化的项**（PlayerPrefs 体积敏感）。
- 每日重置用本地时间（小游戏可接受）；若后续要服务端校验再议。
- 领奖必须**幂等**：已领取 id 集合防重复发奖。

---

## 七、要改的文件（落地清单）

**新增**

- `Assets/Scripts/Data/TaskDefinition.cs`（SO）
- `Assets/Scripts/Data/TaskCatalog.cs`（SO + `Validate`）
- `Assets/Scripts/Data/TaskReward.cs`
- `Assets/Scripts/Data/TaskCounters.cs`（计数器词表 + `TaskCategories` 类别词表）
- `Assets/Scripts/Data/TaskEvents.cs`（`TaskEvent` 结构 + 事件 id 词表）
- `Assets/Scripts/Data/Save/TaskSaveSection.cs`
- `Assets/Scripts/Services/TaskEventHub.cs`
- `Assets/Scripts/Services/TaskService.cs`
- `Assets/Scripts/UI/HubTaskInfoBar.cs`（落地页那条）

**修改**

- `Assets/Scripts/Data/SaveData.cs`：加 4 个字段（名字一旦定下不再改）
- `Assets/Scripts/Data/Save/ISaveSection.cs`：`SaveSections` 登记 `TaskSaveSection`
- 事件发布点若干（§4.2 表里那批文件）
- `Assets/Scenes/Battle.scene`：挂 `HubTaskInfoBar`

**未来**

- `Assets/Scripts/UI/TaskPanel.cs` + `TaskPanel.scene`（完整任务列表界面，与选关 / 天赋同级的 overlay）
- `Assets/RouteData/Tasks/*.asset`（任务资产目录）

---

## 八、分期

| 阶段 | 内容 | 产出 |
| --- | --- | --- |
| **T1 最小可用** | `TaskDefinition` + `TaskCatalog` + 计数器（只做 `Cumulative`）+ 事件总线 + 存档分片 + 落地页一条 | 支持「累计击杀 N / 通关 N / 累计铜钱 / 收集 N 种」 |
| **T2 周期与挑战** | `Peak` / `Distinct` 计数器 + 每日 / 每周重置 + 前置任务链 + 完整任务界面 | 日常 / 周常 / 成长 / 挑战 / 收集 |
| **T3 扩展** | 限时活动窗口 + 成就 + 签到 + 任务作为天赋解锁途径 | 活动期玩法 |

---

## 九、风险

1. **事件总线变成新的隐式全局网** → 只允许任务系统订阅；发布点显式；不在任务系统里反向依赖战斗代码。
2. **存档体积**：计数器 / 进度字典会长大 → 只存变化项；任务数量控制在百级。
3. **任务 id 与计数器 id 拼错** → 校验菜单一键查（词表驱动）。
4. **与击杀里程碑重复** → 边界见 §一；新增前先查 §一那张表。
5. **UI 性能**：落地页只渲染一条；完整列表分页 / 只实例化可见项。
6. **老存档兼容**：新字段缺失由 `TaskSaveSection.Migrate` 幂等补齐，**不改已有字段名**。
