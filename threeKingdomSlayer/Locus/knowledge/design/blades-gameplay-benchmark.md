---
id: kd_336e4b29-73ac-49f0-bff6-3d90e91e0446
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_8c04b7e1-5f2a-4d93-b6c8-1e9a47d5f620
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 《上古卷轴：刀锋》(Blades) 玩法层测绘（2026-11-05，从逆向产物的 257 张设计数据表实测）：敌人 101 家族×189 变体的线性成长与数据化 AI 参数、技能 41 类/674 个 rank 资产（逐级伤害-耐力-等级-技能点四线门槛，Guardbreaker/PowerAttack 完整数值）、声明式挑战与教学目标资产（tutorial 闪避/眩晕/用技能 + 武器类型/位置/眩晕类型修饰器）、320 个 DungeonSettings 关卡生成；给出对我们当前三件事（1013 督战型敌人/重攻击与破防的身份/教学关声明式化）的直接结论与采纳清单。
aiEditMode: inherit
---

# 对标分析：《上古卷轴：刀锋》(Blades) 玩法层测绘

> **性质**：这是 `design/blades-architecture-benchmark.md`（架构层）的**玩法层姊妹篇**。架构篇讲"代码与数据怎么组织"，本文讲"**玩法怎么设计的**"。
> **来源**：2026-11-05 从逆向产物 `Library/Locus/tmp/blades/tables/`（257 张设计数据表）中实测读出；数值均为表内原值。
> **边界**：只做设计参考；抽取的美术/音频/资产表**不入库、不进 Unity 项目、不对外分发**。

---

## 0. 结论先行

### 0.1 为什么它是最像的那一个

| 维度 | Blades | 我们 |
|---|---|---|
| 引擎 | Unity 2019.2.16f1 / IL2CPP | Unity 2022.3 / BIRP |
| 题材 | 上古卷轴（奇幻） | 三国 |
| 视角 / 操作 | 横屏、键鼠+手柄、站桩输出 | 竖屏、单手手势 |
| 战斗骨架 | 技能=每 Rank 一资产 + 敌人变体 + 声明式目标 | 招式=招式资产 + 敌人 Prefab + 硬编码教学拍 |
| 内容量 | 14498 类型 / 1482 bundle / 1.23 GB | 226 脚本 / 13 个敌人 Prefab |

**它不是同类竞品（题材/视角都不同），但它是"能拆开看的同类结构"** —— 而且我们手上有它的完整数据表。

### 0.2 对我们当前三件事的直接结论

| 我们的在做的事 | Blades 的做法 | 结论 |
|---|---|---|
| **1013 督战型敌人**（不攻击、全场加攻、死亡全场减攻） | **没有"敌方指挥官"这种东西**；但敌人能力是 `_knownAbilities` 数据引用、AI 反应时间是 `_aiParameter` 区间 | 1013 是**原创**。最该抄的不是它的内容，而是**"敌人能力 = 数据引用"**：把"号令全场加攻"做成能力表条目，而不是写死在一个组件里 |
| **"重攻击没有伤害身份"**（Pierce 20 = Stab 20，砍/挑/扫全是功能招） | `PowerAttack` 与 `Guardbreaker` 是**两个独立技能**，各 13/16 个 rank 资产，带**英雄等级门槛 + 技能点门槛 + 耐力消耗 + 逐级伤害曲线** | **这是根因解**：不是调数值，是给"重"和"破防"各一条**可见的成长线**（详见 §3.2） |
| **教学关硬编码拍**（`TutorialStageDirector`） | 教学目标是**声明式资产**：`Challenge_TUTORIAL_Dodge`（闪避 5 次）、`Challenge_TUTORIAL_StunEnemy`（眩晕 3 次，且指定**眩晕类型/武器类型/位置**）、`PerformAbility`（用技能 N 次，带"是否含打断"开关） | 我们教学关的每一拍都该是一张"目标卡"，而不是一段脚本 |

---

## 1. 敌人：101 家族 × 189 变体（实测）

### 1.1 结构

| 表 | 行数 | 关键字段 |
|---|---|---|
| `BGS.Game.Data.EnemyData` | **101**（敌人"家族"） | `_variants[]`、`_lootTableList[]`、`_experienceTableId`（**出现等级区间**）、`_bossFamilyIds`、`_category` |
| `BGS.Game.Data.EnemyVariant` | **189**（变体） | `_stats`（**59 个字段**）、`_compound`（外观组合）、`_knownAbilities[]`、`_aiParameter`、`_spellCooldownFactor`、`_maneuverCooldownFactor`、`_isSpellCaster` |

- 每个家族 1~8 个变体；**家族名带 `Abyss` 后缀 = 同一套形象的深渊难度变体**（如 `Troll Family` / `Troll Family Abyss`）。
- 家族谱系（节选）：元素系 `Frost/Flame/Storm Atronach`、小怪系 `Skeever/Spider/Wolf/Bear/Spriggan`、人形系 `Duelist(剑士/法师/蛮族/复仇者)/Necromancer/Warlord/Goblin/Troll/Draugr/Wight/Dremora`、龙与亡灵法师。

### 1.2 数值模型：线性成长（实测样本）

| 变体 | healthBase | healthIncrement（每级） |
|---|---|---|
| Skeleton7_SkeletonFiend | 83 | 15.6 |
| Atronach_Frost1_FrostAtronach | 105 | 28.4 |
| Troll_Ancient | 212 | 29.1 |
| Duelist_Swordmage5 | 697 | 25.1 |
| Spriggan_Basic | 221 | 3.8 |
| Stone_Skeever | −153 | 16.8 |

> `healthBase` 可为负 ⇒ 实际 HP 是**家族基准 + 变体增量 + 等级增量**的组合，不是单值。

### 1.3 对我们的意义

1. **一个"新敌人"在 Blades = 一份 stats，不是一套美术 + 一个 Prefab**。我们现在是"一敌人一 Prefab + 一套动画"，敌人内容产能被美术卡死（这正是 1013 只能借 104 形象的原因）。
2. **AI 个体差异是数据**：`aiParameter._reactionTime = {minimumValue: 0.2, maximumValue: 0.3}` ⇒ 同一套 AI 有"迟钝/机敏"个体，不需要第二套 AI 代码。我们的 `1011/1012` 只是换了贴图和 C 技，属于"视觉变体"，而 Blades 的变体是**数值 + 外观 + 能力**三重差异。
3. **能力表挂在敌人身上**（`_knownAbilities`）⇒ 敌人行为差异 = 配置差异。这对 1013 直接适用（见 §6.1）。

---

## 2. 技能：41 类 / 674 个 rank 资产（实测）

### 2.1 单个 rank 资产的字段（以 Fireball rank11 为例）

`_abilityRank` / `_abilityPointCost` / `_staminaCost` / `_magickaCost` / `_cooldown` / `_initialCooldown` / `_channelDuration` / `_projectileSpeed` / `_damage` / `_powerLevel` / **`_castingPointers` / `_projectilePointers` / `_impactPointers`** / `_requiredHeroLevel` / `_canBeCastOutOfCombat`

> 三套表现指针 = **施法 / 弹道 / 命中**三段解耦（架构篇 §0.1 第 2 条已列）。玩法层的意义：**一个技能加一个新表现，不必改它的数值资产**。

### 2.2 ★ 四类"动词型"技能的完整数值（对我们最相关）

#### (a) `Guardbreaker`（破防）—— 13 个 rank

| rank | powerLevel | bonusDamage | CD | stamina | 英雄等级门槛 | 技能点 |
|---|---|---|---|---|---|---|
| 1 | 16 | 131.1 | 8.09 | 215 | 16 | 4 |
| 3 | 30 | 188.1 | 8.09 | 265 | 50 | 3 |
| 6 | 42 | 238.4 | 8.09 | 320 | — | — |
| 9 | 53 | 285.5 | 8.09 | 375 | — | — |
| 13 | 65 | 356.8 | 8.09 | 435 | — | — |

额外字段：`_damageToCauseStagger = 1.0`、`_stunDuration = 2.1`。

#### (b) `PowerAttack`（重攻击）—— 16 个 rank

| rank | powerLevel | bonusDamage | CD | stamina | 英雄等级门槛 | 技能点 |
|---|---|---|---|---|---|---|
| 1 | 2 | 75.3 | 8.09 | 145 | 2 | 1 |
| 6 | 37 | 255.3 | 8.09 | 230 | 47 | 2 |
| 16 | 72 | 449.6 | 8.09 | 355 | — | — |

#### (c) `IndomitableSmash`（不屈猛击）—— 12 个 rank

`bonusDamage = 0`（**不造成伤害**），但 `_bonusResistance = 497.09`、`_statusesToRemove = [4,5,6,7,8]`（**一次性解除 5 种负面状态**）。

#### (d) `Breath`（吐息）—— 22 个 rank

含 `_castingPointers` / `_impactPointers` / `_damageType` / `_damagePerSecond` / `_duration` ⇒ **持续伤害型技能的标准字段集**（施法 → 命中 → 每秒伤害 × 时长）。

### 2.3 对我们的意义（★ 这条直接解我们的问题）

1. **"重攻击"和"破防"是两个独立技能，各自有完整成长线**：等级门槛（16/33/50 …）、技能点门槛、逐级伤害、逐级耐力消耗。
   → 我们的"重"目前只是 `interruptsCavalryCharge` 这个**隐式标记**，于是它"没有伤害身份"。**Blades 的解法不是调数值，是给它一条和别的技能同规格的成长线**——包括**解锁门槛**（我们的天赋树"招式解锁"节点正好可以承载这个门槛）。
2. **CD 全 rank 恒定，成长只加"更强"不加"更快"** → 我们的动作锁模式（`useActionBasedCooldown=true`）也必须遵守这条：逐级成长不要动动作时长，否则连招窗口会整体漂移。
3. **零伤害技能有成熟先例**（`IndomitableSmash`：0 伤害 + 497 抗性 + 解除 5 状态）→ 我们的海浪/旋风"零伤害控场"是被验证过的设计；而且"**解除负面状态**"这个维度我们还没用（我们的节点奖励里已经有一个「重击解冻」秘籍，说明方向是对的）。
4. **DoT 技能的标准字段**（`damagePerSecond` + `duration` + 施法/命中表现分离）→ 我们的蛇形喷射/染病可以直接照抄这套字段结构。

---

## 3. 挑战与教学：声明式目标资产（实测）

### 3.1 挑战类型（20 种，同一套公共字段）

`ChallengeData` 公共字段：`_name`（本地化 key）、`_weight`、`_roundToQuantity`、`_selector`（指向 `ChallengeSelector`）、`_oneTimeOnly`、`_challengePoolCategory`、`_minQuantity` / `_maxQuantity`、`_reward`。

类型清单：`DealDamage` / `Defeat` / **`Dodge`** / **`StunEnemy`** / **`PerformAbility`** / `Perform` / `PerformCriticalHit` / `Brew` / `Smith` / `Build` / `Enchant` / `Temper` / `Repair` / `Restore` / `Salvage` / `CollectChests` / `CraftDecorations` / `FindInstantConsumable` / `AbyssReachFloor` / `AbyssCompleteFloors`。

`ChallengeData` 单例：`_maxActive = 3`（同时最多 3 个活跃挑战）、`_generationFrequencySeconds = 3600`、`_challengePools`、`_unlockChallengesQuestPointer`。

### 3.2 ★ 教学目标是"资产"，不是脚本

| 资产 | 关键字段 |
|---|---|
| `Challenge_TUTORIAL_Dodge` | `_minQuantity = _maxQuantity = 5`（**闪避 5 次**）、`_oneTimeOnly = 1`、`_weight = 420000`（教学权重极高）、`_reward` |
| `Challenge_TUTORIAL_StunEnemy` | `_minQuantity = _maxQuantity = 3`（**眩晕 3 次**）、**`_targetStunType = 1`**（指定眩晕类型）、**`_modifierWeaponType`**（指定武器）、**`_modifierLocation`**（指定位置） |
| `PerformAbility`（44 条） | `_targetAbility`（指定技能）、`_includeInterruptions`（**是否把"打断"算作完成**） |
| `ChallengeSelector`（216 条） | `_requiredPlayerLevel` / `_lockedAtPlayerLevel` / `_requiredTownLevel` / `_lockedAtTownLevel` / `_requiredAbyssLevel` / `_requiredQuestPointer` / `_requiredBuildingLevel` ⇒ **四重解锁门是纯数据** |

### 3.3 对我们的意义

- 我们的教学关现在是 `TutorialStageDirector` 的**硬编码拍**（等敌人站定 → 冻结 → 提示 → 判定成功 → 放行）。Blades 的做法是：**每一拍 = 一张"目标卡"**（做什么 × 多少次 × 对谁 × 在哪 × 用什么），条件和奖励全是数据。
  → 对我们的直接映射：`Challenge_TUTORIAL_StunEnemy` 的 `_targetStunType` / `_modifierWeaponType` / `_modifierLocation` 就是我们教学关"用招架打 1012 的破势 C 技"那一拍的天然数据表示。
- `PerformAbility._includeInterruptions` = **"用技能打断"那一拍**的现成开关（我们教学关正好有这一拍：`TutorialStageDirector` 的"技能打断拍"）。

---

## 4. 关卡与路线（实测规模）

| 表 | 行数 | 说明 |
|---|---|---|
| `BGS.Game.LevelGeneration.DungeonSettingsScriptableObject` | **320** | 入口房 / 房间链接 / 固定布局 |
| `BGS.Game.LevelGeneration.Abyss.AbyssSlice` | **161** | `_levelRange` / `_randomWeight` / `_dungeonSettingsPointer` ⇒ **按等级段抽关卡** |
| `BGS.Game.Quests.*` | 98 任务持有者 | 任务图整体序列化进一个 JSON 字段 |

→ 对我们的意义：我们的卷轴路线是**手工搭的固定拓扑**；Blades 是"**关卡池 + 等级段随机权重**"。这与我们支柱五"分支要给出差异"的诉求同源，但实现更轻——他们不需要手工保证每条分支都有意义，因为关卡是抽的、权重是配的。

---

## 5. 采纳清单（对照我们的现状）

| # | 采纳项 | Blades 怎么做 | 我们的落点 | 成本 | 收益 |
|---|---|---|---|---|---|
| 1 | **重攻击 / 破防 做成两条显式成长线** | `PowerAttack` / `Guardbreaker` 各 13~16 rank，带等级+技能点+耐力+逐级伤害 | `AttackSkillConfig` 增字段（门槛/消耗/逐级表）+ 天赋树"招式解锁"承载门槛 | 中 | ★★★ 直接解决"重攻击没有伤害身份"这个老问题 |
| 2 | **敌人 = 变体 + 线性成长 + 能力表** | 101 家族 × 189 变体，`healthBase+healthIncrement`，`_knownAbilities` | `Enemy` 增运行时 stats 覆盖层；1013 的"号令"做成能力表条目 | 中高 | ★★★ 敌人内容产能脱离美术（新敌人可以只换数值/能力） |
| 3 | **教学关声明式目标卡** | `Challenge_TUTORIAL_*` + 修饰器（眩晕类型/武器/位置/是否含打断） | 新增 `ChallengeObjective` 资产类型，`TutorialStageDirector` 改为消费它 | 中 | ★★ 教学关可复用、可配置、可加新拍 |
| 4 | **表现三段解耦** | 施法/弹道/命中三套指针 | `AttackSkillConfig` 增表现组字段 | 中 | ★★（架构篇已列，重复记一次） |
| 5 | **关卡池 + 等级段权重** | 320 DungeonSettings + 161 AbyssSlice | 我们的卷轴路线加"节点池 + 权重" | 中高 | ★★ 支柱五的分支差异可以靠权重而非手工拓扑 |

## 6. 不借鉴

- ❌ **UID + 字符串寻址**（`design/anti-ghost-reference.md` 明令禁止）：借它的"内容哈希校验"，不借"运行时按 id 查表"。
- ❌ **把服务端逻辑打进客户端**（`BGS.Game.Network.Server.*` 打进包）：我们是单机，没有这条需求。
- ❌ **`_statusesToRemove` 那种"一次解除 5 种状态"的大而全解法**：我们的状态只有 3 种（灼烧/疾病/冻结），照抄会过度设计；**只借"技能可解除负面状态"这个维度**（我们的「重击解冻」已经在用）。

---

## 7. 产物索引与复跑

- 全部数据表：`Library/Locus/tmp/blades/tables/*.csv`（**257 张**，本文数字全部出自此处）。
- 重建方式、解析要点、模拟器实测环境、已知坑：见 `memory/blades-reverse-engineering-assets.md`（**§3 复跑方法**：`dump_metadata.py` → `dump_tables.py` → `build_rest_spec.py`）。
- 架构层对照：见 `design/blades-architecture-benchmark.md`。
- 离线化进度：见 `plan/blades-handoff-20261029.md`、`plan/blades-offline-revival-plan.md`。
- 玩法类参考清单（同批抓取的其他作品）：见 `reference/game-design-references.md`。
