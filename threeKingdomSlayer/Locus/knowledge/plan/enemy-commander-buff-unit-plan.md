---
id: kd_70f598da-bd69-49b0-a673-02617f7b75ca
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 督战型增益单位 enemyId 1013 的开发方向与制作方案（2026-11-05，数值/形象/打断规则已拍板，未实施）：§0 开发方向（身份卡、补的三层缺口、三条设计不变量、范围边界、后续扩展）、机制规格与在场口径、净效果曲线、"硬门"性质、号令可打断规则、相关功能引用与机制理解索引、对标参考、技术改动清单 M1-M9、预制体构造步骤、验收口径、风险表与决策记录。
---

# 督战型增益单位制作方案（2026-11-05）
> **状态：开发方向与方案已定，未实施。** 本文只定义与规划；实施顺序见 §12，决策记录见 §13。
> 定位：给玩家一个**必须优先解决的目标**（现役敌人没有"先打谁"的决策）。
> 配套背景：`design/gameplay-pillars.md` §4（敌人机制与反制表）、`design/game-mechanics.md` §5.1（敌人图鉴）、`design/immutable-constraints.md`（位移/补齐硬约束）。
> 对标参照：`design/blades-gameplay-benchmark.md`（Blades 玩法层实测）、`reference/game-design-references.md`（外部作品清单）。
> 工程基线：分支 `feat/stage-select-talent-overlays`，实测数据取自 2026-11-05 的活体工程（预制体 / 招式资产 / 已部署波次 / 代码分支）。

---

## 0. 开发方向（Direction）

> 本节记录"1013 往哪个方向长"，是后续所有实现取舍的判据；具体机制与步骤在 §2、§5、§6、§12。

### 0.1 身份卡

| 项 | 内容 |
|---|---|
| 代号 / id | 督战鼓手（临时名）/ **`enemyId = 1013`** |
| 一句话 | **不攻击、给在场敌人叠加攻击力、死后给在场敌人永久减攻的敌方指挥单位** |
| 玩法定位 | 玩家第一个"**必须优先解决**"的目标（硬门 + 时机决策） |
| 所属支柱 | 支柱四「敌人机制」；服务支柱三「技能组合」与支柱五「分支」 |
| 体量 | 一个**普通单位**（占 1 格 / 150 血 / 经验 150），不是 BOSS、不进 QTE |
| 状态 | 机制与数值已拍板；**未实施**（实施顺序见 §12） |

### 0.2 它补的三层缺口

1. **优先级缺口**：现役 4 种"门"（1011 C技霸体 / 1021 盾卫 / 105 远程 / 109 冲锋）**都是"打哪个都行"**——没有任何敌人会让"先打它"明显更优。
2. **敌方增益缺口**：场上所有增益都来自玩家侧（灼烧 / 疾病 / 冻结 / 连击 Buff），敌方没有任何"让局面变糟"的能力。1013 是第一个。
3. **"何时杀"的决策缺口**：它把"先打谁"从空缺变成**时机成本**（号令层数）vs **时间成本**（去后排点它）。

### 0.3 三条设计不变量（改代码时不能破坏）

1. **只用既有动词**：威胁必须落在 **{选列 / 换招 / 择时 / 用技能}** 四个动词上 —— 玩家**没有位移动词**（`Assets/Scripts/Player/` 下无 `transform` 位置赋值），任何"需要走位才能解"的威胁都是无解。
2. **不新增移动语义**：它正常行军（**不设** `ignoreFillUpOrders`），一切位移走 `ColumnManager` 既有通道，遵守 `design/immutable-constraints.md`。
3. **不改既有平衡口径**：不加新系统、不改通用伤害公式；倍率只作用在"**出站伤害**"这一层，且只作用于本单位的加/减攻。

### 0.4 明确不做（范围边界）

- 不做召唤 / 增援、不做连携护卫、不做敌方 BOSS 版（另议）。
- 不做"玩家可位移/闪避"（那是玩家侧的大改，属另一条线）。
- **波次编排与正式投放不在本次范围**（属关卡配置工作）。
- 最终美术形象另立排期（本次用 104 BOSS 形象 + 放箭 QTE 动作做替身）。

### 0.5 后续可能的扩展方向（供以后排期，本次不做）

| # | 方向 | 触发条件 / 参照 |
|---|---|---|
| 1 | **敌方指挥官体系**：1013 是第一个，后续可做"第二个指挥官"（不同增益方向） | Blades 的 `_knownAbilities` 数据化 ⇒ 把"号令"做成**能力表条目**，而不是写死在一个组件里 |
| 2 | **处决**：把"破盾 / 破势"变成一击必杀 | Sekiro 的"破防 → 处决"⇒ 让"优先目标"有硬收益 |
| 3 | **Director 式动态编排**：由玩家表现决定本波出什么门 | L4D2 的 AI Director ⇒ 比继续扩静态波次表省力 |
| 4 | **重攻击 / 破防成长线化**：做成有解锁门槛与消耗的成长线 | Blades 的 `PowerAttack` / `Guardbreaker` ⇒ 同时解决"重攻击没有伤害身份"这个老问题 |

---

## 1. 它要解决的问题

现役非 Boss 敌人只有 4 种"门"（实测）：

| ID | 机制 | 需要的对策 |
|---|---|---|
| 1011 | 每次攻击都是 C 技（霸体） | 重攻击 / 招架拆前摇 |
| 1021 | 盾侧格挡 → 破盾 → 胆怯 | 挑飞（确定性）/ 反侧重攻击（≈1/3 概率） |
| 105 | 后排远程 | 招架弹反 / 定点清除（起手够不着，要等前排被打空后它自己前移上来，见 §2.5） |
| 109 | 骑乘冲锋（免控） | 蓄力攻击打断 |

**四种都是"打哪个都行"**——玩家没有"先解决谁"的问题，因为没有任何一个敌人会让"先打它"明显更优。这个单位就是来补这层决策的。

它同时是**第一个"敌方增益来源"**：目前场上的所有增益都来自玩家侧（灼烧 / 疾病 / 冻结 / 连击 Buff），敌方没有任何"让局面变糟"的能力。

---

## 2. 机制规格

### 2.1 三个动作

| 动作 | 触发 | 效果 | 时长 |
|---|---|---|---|
| **常态** | — | **不攻击、不造成任何伤害**；正常行军 / 补齐 / 受击 / 被挑飞 / 被眩晕，与普通敌人无异 | — |
| **施放（加攻）** | 周期触发（首次有前置延迟） | **在场**所有敌人攻击力 **+X%**，**可叠加** | **永久**（波内不过期） |
| **死亡（减攻）** | 死亡瞬间 | **在场**所有敌人攻击力 **−Y%** | **永久**（波内不过期） |

> 措辞落点：" permanent"= 在本波/本节点生命周期内**不会自行过期**，只随波次/节点结束或敌人回池而清除。实现上的清理由对象池回收路径统一负责（见 §5.4）。

### 2.2 "在场"的准确定义（实现必须逐条对齐）

| 计入 | 不计入 |
|---|---|
| 已注册进 `EnemyManager` 且未死亡的全部敌人 | BOSS（`isBoss == true`）—— BOSS 有独立平衡与 QTE 体系，加成会把终局战变成 DPS 检验 |
| 正在入场行进的单位（`SpawnEntry` 未走完）—— 已在屏且已注册，排除会造成"箭打到了却没被加成"的困惑 | 指挥官自己 |
| | 尚未生成的敌人（后续波次 / 后续入场）—— "在场"就是不在场 |

工程依据：`EnemyManager.allAliveEnemies` 在生成时即登记（`RegisterEnemy`），死亡时移除（`OnEnemyDied`）。因此"在场"= `EnemyManager.Instance.GetAllAliveEnemies()` 过滤掉 `isBoss` 与自己。

> **口径已定，不作为拍板项**：死亡减攻作用于**死亡瞬间仍在场**的敌人；之后才入场的敌人不吃这个减攻 —— 这是"在场"定义的直接结果，与加攻的施加口径一致（同一次施放/结算只作用于当时在场的单位）。
> 补充事实（已核对）：**一波之内的全部敌人是生成即注册**（`WaveSpawner` 按 `foreach (RowConfig row in wave.rows)` 一次性建出整波敌人，`SpawnEntry` 只是让它们错峰走进场），所以"加攻"从第一次施放起就覆盖本波全部敌人；"减攻"只覆盖死亡瞬间还活着的那些。

### 2.3 叠加与净效果曲线

```
净效果(k) = k × X − Y        （k = 指挥官已施放次数）
```

以首版建议值 X=10%、Y=25%、上限 5 层：

| k（已施放次数） | 0 | 1 | 2 | 3 | 4 | 5 |
|---|---|---|---|---|---|---|
| 全场累计加攻 | 0% | +10% | +20% | +30% | +40% | +50% |
| 死亡减攻 | −25% | −25% | −25% | −25% | −25% | −25% |
| **净效果** | **−25%** | **−15%** | **−5%** | **+5%** | **+15%** | **+25%** |

**教学口径（一句话）**：*它每号令一次，全场敌人永久变强 10%；它一死，全场敌人永久变弱 25%。所以最好在它号令两次以内点掉它——拖过第三次你就在净亏。*

### 2.4 首版数值建议

| 参数 | 建议值 | 理由 |
|---|---|---|
| 每次加攻 X | **+10%** | 让"号令 3 次"跨过盈亏平衡点（§2.3） |
| 加攻层数上限 | **5** | 超过后不再叠加；避免长时间存活把倍率推到不可控（+50% 已能让骑兵一撞带走玩家 48 血） |
| 死亡减攻 Y | **−25%** | 略大于 2 层加攻 → "两次内点掉"这条规则成立 |
| 减攻可叠加 | **否**（一次性结算） | 指挥官只死一次 |
| 施放周期 | **4.0s** | 与玩家一次长按蓄力 + 一次换列的时间量级相当 |
| 首次施放前置 | **2.5s** | 给玩家"入场后有一段安全窗口" |
| 施放前摇 | **0.8s**（举旗预警） | 玩家能预判下一次号令何时落下 |

### 2.5 补齐与可达性（**更正上一版的理解**）

上一版写"玩家通常打不到后排、105 远程是打不到的"——**这是错的**。实测补齐机制（PerRow 逻辑排队列）：

1. 波次的排构成**逻辑排队列**（活人排 / 空排占位符 / 999 节奏门），见 `Assets/Scripts/Core/LogicalRowQueue.cs`。
2. 某活人排被清空（`HasLivingMembers` 为假）→ `MarkRowRemoved` → `ReassignRuntimeRows()` 把后面的活人排**运行时排号整体前移一格** → `WaveMarchScheduler` 给这些敌人派行军订单 → **每个敌人一次补齐只前进一排**（`rushMoveDelay` 逐排间隔）。
3. 因此**任何敌人（含 105 远程、含本方案的新单位）都会随着前面整排被打空而一排一排前移，最终进到 row 0，被玩家平A直接命中**。历史上"105 远程永远不补齐"是**已修复的缺陷**（代码注释留有记录：`WaveMarchScheduler.cs` 中「历史缺陷：105 远程敌人永远不补齐」）。
4. 唯一例外是显式声明 `ignoreFillUpOrders` 的敌人（不参与行军，全项目当前零使用）。

**结论**：

- **"打不到后排"不成立**，成立的只有"打起来慢"——而这正是优先级决策需要的**时间成本**。
- 玩家可以用 3 排手段提前点掉后排目标：Pierce（3 排单列）/ Sweep（3 排多列）/ 枪突终结（3 排多列）/ 指向突刺（2 排）/ 箭雨（1~5 排）/ 旋风（1~3 排）；也可以选择先清前排、让目标自己走上来。
- 玩家当前**没有位移动词**（`Assets/Scripts/Player/` 下无 `transform.position` / `localPosition` 赋值），角色固定在阵型前方，能做的只有 **选列 / 换招 / 择时 / 用技能** —— 这条仍然成立，但它不再限制目标的可达性。

### 2.6 指挥官是"硬门"（设计资产，不是副作用）

排被判定为"清空"的依据是**该逻辑排没有活着的成员**（`ColumnManager.RemoveEnemyFromColumn` → `WaveMarch.HasLivingMembers`）。指挥官活着 → 它所在的那一排永远不算清空 → 后面的排不前移 → 波次也无法结束（`EnemyManager` 以 `allAliveEnemies.Count == 0` 判波次完成）。

因此：

1. **指挥官必然被玩家处理掉**，玩家不会无视这个机制。
2. 决策不是"要不要杀"，而是 **"什么时候杀"** —— 这正是我们要的：时机成本（号令层数）vs 时间成本（去后排点它要花掉的注意力与走位）。
3. 指挥官**不攻击** ⇒ 不贡献伤害，但**占一个前排格子**；它在后排时，它是那一排的"锁"，后面的压力被它挡着——杀掉它，一整排敌人立刻前移接管你的注意力。这是它的第二层价值。
4. 不会死锁：指挥官自己会随波次前移，最终进 row 0（可被平A命中）。

### 2.7 号令可被打断（2026-11-05 已定）

**规则**：指挥官**没有霸体**（`isCAttack = false`、非 BOSS ⇒ `isCFrame` / `isSuperArmor` 均为 false），所以它的号令前摇**会被直接伤害打断** —— 与普通敌人的前摇可被打断是同一条既有规则（判定代码见 §4.2）。

| 判定 | 伤害来源（`HitFeedbackSource`） | 说明 |
|---|---|---|
| **打断** | `BasicAttack`（玩家本体招式）、`ActiveSkill`（主动技能）、`Ultimate`（大招） | 玩家主动造成的直接伤害 |
| **不打断** | `Dot`（灼烧 / 疾病）、`Passive` / `Phantom`（被动攻击）、`Item`（道具）、`Displacement`（击退 / 挑飞）、`SpikeTrap`（地刺）、`Collision`（破冰碰撞） | 沿用既有口径"只有玩家攻击本体算数"（样板见 §4.2） |

**打断后发生什么**：

1. 本次施放**取消** —— 不加攻、层数不涨、头顶飘字不跳。
2. 施法动作被打断（敌人受击反应照常播 HitFlash），指挥官回到 Idle。
3. 下一次尝试**重置为完整周期**（4.0s）—— 与"普通敌人的攻击被打断后重新进入冷却"是同一套语义。

**为什么不做"短重试"**：重试间隔越短，玩家越能用很轻的伤害把它近乎静音（风险见 §10）。用完整周期 + 150 血的上限，让"压制"在几秒内自然收敛成"击杀"——这正是我们要的"优先级目标"结局。

---

## 3. 数值参考（仅供对数用，不作为设计前提）

现役敌人基础攻击力（prefab 实测）：1011 / 1012 = 3，1021 = 10，109 冲锋 = 8 ×（发起排+1）。

| 来源 | 基础 | +50%（5 层） | −25%（死亡减攻） |
|---|---|---|---|
| 1011 / 1012 单次 | 3 | 4.5 | 2.25 |
| 1021 单次 | 10 | 15 | 7.5 |
| 109 冲锋（row3 起冲） | 32 | 48 | 24 |

> 这张表只用于实现时对数（确认倍率乘在了正确的伤害源上），**不作为机制取舍的前提**。机制的取舍依据是 §2.3 的净效果曲线与 §2.6 的时机决策。

---

## 4. 相关功能引用与机制理解（实现者必读）

> 本节把 1013 会碰到的每条既有链路写清楚：**改哪个文件、哪个方法、为什么**。
> 行号会漂移，按方法名搜索。

### 4.1 伤害结算链：出站倍率要乘在哪三处

| 伤害来源 | 结算路径 | 我们的乘点 |
|---|---|---|
| 近战 | `Enemy.Attack.PerformAttack()` → `EnemyManager.OnEnemyAttackPlayer(enemy)` → `PlayerState.TakeDamage(damage, enemy)` | 在 `OnEnemyAttackPlayer` 里算 `enemy.attackDamage × 出站倍率` |
| 骑兵冲锋 | `CavalryEnemy.ChargeDamage` = `chargeDamage × 冲锋格数 × 波次倍率` → `EnemyManager` → `PlayerState` | 在 `ChargeDamage` 属性里乘（**独立伤害源，不改就完全不吃增益**） |
| 远程箭矢 | `Enemy.Attack.SpawnProjectile()` 把 `attackDamage` 作为**快照**传给 `EnemyProjectile.Launch(...)` → `EnemyProjectile.OnArrival()` → `PlayerState.TakeDamage` | 在 `SpawnProjectile` 发射时快照一次出站倍率（与波次倍率的快照口径一致） |

要点：`Enemy.attackDamage` 在 `Enemy.Init.ApplyWaveScaling()` 里**已经**乘过波次 `damageMultiplier` → 我们的出站倍率必须乘在它**之后**。BOSS 走同一条近战链，但被排除在加 / 减攻之外。

### 4.2 打断与霸体的既有口径

- 敌人前摇被打断的判定在 `Enemy.Combat.TakeDamage()`：
  `if (!inArmorWindow) CancelAttack(); else if (canInterruptCFrame || isHeavyAttack) CancelAttack();`
  其中 `inArmorWindow = isCFrame || isSuperArmor`。
  → **1013 不设 `isCAttack`、不是 BOSS ⇒ 无霸体 ⇒ 任何直接伤害都能打断它的前摇**（§2.7 的规则来源）。
- "只有玩家攻击本体算数"的既有样板：`Enemy.Combat.TakeDamage()` 里
  `if (poiseDamage > 0f && inArmorWindow && feedbackSource == HitFeedbackSource.BasicAttack)`
  → 我们的"谁能打断"按同一思路划分，来源枚举见 `Assets/Scripts/Data/HitFeedbackEnums.cs`（共 10 种）。
- 教学关已有"用技能打断 C 技"（`Assets/Scripts/Tutorial/TutorialStageDirector.*`）→ 主动技能能打断前摇，故可打断来源 = `BasicAttack + ActiveSkill + Ultimate`。

### 4.3 补齐与行军："硬门"与可达性的来源

- 逻辑排队列：`Assets/Scripts/Core/LogicalRowQueue.cs`
  `Configure / RegisterEnemy / HasLivingMembers / MarkRowRemoved / ReassignRuntimeRows`。
- 触发链：`ColumnManager.RemoveEnemyFromColumn()` → `!WaveMarch.HasLivingMembers(clearedLogicalRow)` → `WaveMarch.RequestLogicalLayoutReflow(...)` → `WaveMarchScheduler.StartWaveMarch()` → `TryBuildLogicalWaveMarchPlan()`（按 `logicalRow.currentRow` 派单）→ 敌人**一次补齐只前进一排**（`rushMoveDelay` 逐排间隔）。
- **排被清空的判据 = 该逻辑排没有活着的成员** → 指挥官活着 = 那一排永不清空 = 后排不前移 = 波次不结束（`EnemyManager` 以 `allAliveEnemies.Count == 0` 判波次完成）。这是"硬门"（§2.6）。
- **1013 不要勾 `Enemy.ignoreFillUpOrders`**（豁免补齐，全项目当前零使用）→ 勾了它就不参与行军、永远待在原地。
- 历史上"105 远程永远不补齐"是**已修复**的缺陷（`WaveMarchScheduler.cs` 注释有记录）→ 现在**所有敌人**都会随前面整排被打空而一排一排前移到 row 0 → "打不到后排"不成立，只有"打起来慢"。

### 4.4 敌人注册与"在场"枚举

- `EnemyManager.RegisterEnemy(enemy)`（生成即注册，订阅 `enemy.OnDeath`）/ `OnEnemyDied()`（死亡即移除）/ `GetAllAliveEnemies()`（**返回内部列表引用** → 遍历期间不得修改它）。
- 一波敌人是**一次性建出**的：`WaveSpawner` 按 `foreach (RowConfig row in wave.rows)` 建出整波，`SpawnEntry` 只是让它们错峰走进场 → 所以"在场"从波次生成那一刻起 = 本波全员。
- `Enemy.Die()` 流程：`OnDying` → 状态置 Dead → **`OnDeath?.Invoke(this)`（减攻的施加点）** → 死亡动效 → `OnDeathAnimComplete` → 回池。

### 4.5 状态宿主与"永久"的生命周期

- `StatusContainer`（`Assets/Scripts/Status/StatusContainer.cs`）：`Add`（同 id 覆盖）/ `Remove`（结算期延迟生效）/ `ClearAll()`。
- `Enemy.StatusHost`（内部字段 `_statusHost`；getter 每次访问会 `EnsureResidentStatusRegistered()`，**只补登记常驻状态"冻结"**）。
- 回收清理**已经存在**：`Assets/Scripts/Enemy/Enemy.Pool.cs:75` `_statusHost?.ClearAll();`，由 `EnemyPool.ReturnEnemy()` → `enemy.ResetEnemy()` 调用 → 施加型状态**不会跨波泄漏**。
- 推论：加攻 / 减攻必须"每次施放 / 结算时重新 Add"，不能指望被清空后自动回来。
- 查询入口：`EnemyStatusRegistry.GetStatus(enemy, id)` / `GetActive(enemy)`（宿主式 + 外部提供者两路）。

### 4.6 显示层可复制的三处模式

| 要显示什么 | 抄哪里 |
|---|---|
| 敌人身上的状态条 + 层数文本 | `Assets/Scripts/UI/EnemyHealthBar.cs` → `UpdateDotVisuals()` 每帧查 `EnemyStatusRegistry` 并画 `disease` / `burn` 两根条 + `layersText`；第三根"加攻"条照抄这个模式 |
| 敌人身上的状态描边 | `Assets/Scripts/Enemy/Enemy.Visuals.cs` → `UpdateOutlineState()` 组装 `EnemyOutline.OutlineFacts`（已有 `PierceHighlighted / SuperArmor / CFrameAttacking / QteAttacking / Dead / PhaseTransitioning`）→ 加 `AtkBuffed` 事实位是同构扩展，优先级在 `EnemyOutline` 里裁决 |
| 攻击前摇预警 | `Assets/Scripts/Enemy/EnemyAttackTelegraph.cs` → `BeginWarning(duration)` / `StopWarning()` |

### 4.7 动画与状态机入口

- 敌人通用动作播放入口：`Assets/Scripts/Enemy/Enemy.FormSwitch.cs` → `PlayIdleVisual / PlayHitVisual / PlayGetupVisual / PlayDeadVisual`。
- 104 的 controller `Assets/Animations/Boss_104.controller` 状态清单：
  `Idle / Walk / Attack / CAttack / QTEAttack / HitFlash / Dead / Launched_Rise / Launched_Fall / StunStart / StunLoop / StunHit / StunEnd / Phase2Transition / Phase3Transition / QTE_TripleStab_Start / QTE_TripleStab_Loop / QTE_TripleStab_End / QTE_Sweep_*`。
  → **缺 `HitLeft` / `HitRight`**（受击回落 HitFlash）、**缺 `Launched_Getup`**（被挑飞后起身无动画，只报 Animator 警告）。
- 施法播放方式：`animator.Play("QTE_TripleStab_Start")`（**状态名**，非 trigger）—— `AttackStep.animationTrigger` 走的是 `SetTrigger`，所以**不能**借敌人的攻击序列来播施法动作。

### 4.8 "永不攻击"的实现口径

- `Enemy.StateSwitch.StartAttacking()`：开头早退 `state = EnemyState.Idle; return;`（与骑兵 `_cavalry.IsMounted` 分支同款）。
- `Enemy.Movement` 的行军完成分支在 `StartAttacking()` 之后**独立**调用 `CompleteRushMoveOrder()` → 早退**不会卡住补齐队列**。
- `Enemy.Attack.UpdateAttack()` 开头也有骑兵早退；指挥官不进 Attacking 态即可。

### 4.9 对象池与预制体注册（两个必做项）

- `Assets/Scenes/Battle.scene` → `Battle/EnemyPool` → `enemyPrefabList`（Inspector 引用，当前 13 项；`EnemyPool` 2026-10-23 起**不再扫目录**）→ 1013 必须手动加，否则运行时会 fallback 到临时 Cube。
- `EnemyPool.ReturnEnemy()` → `enemy.ResetEnemy()` 是唯一回收路径；`ResetEnemy` 会把 `OnDeath = null` → **组件必须在每次 `OnEnable` 重新订阅**，不能只在 `Awake` 订阅一次。

### 4.10 对标参考（做 1013 前先看一眼）

| 参考 | 可直接用到本单位的点 |
|---|---|
| `design/blades-gameplay-benchmark.md`（Blades 玩法层实测） | ① **敌人能力 = 数据引用**（他们的 `_knownAbilities`）⇒ 1013 的"号令"宜做成能力表条目，而不是写死在一个组件里；② **表现三段解耦**（施法/弹道/命中）⇒ 我们的"施法"只有施法段，落地段与命中反馈应分开配；③ **重攻击/破防是两条显式成长线**（有等级门槛 + 技能点 + 耐力 + 逐级伤害）⇒ 1013 号令的可打断性若要给玩家对策，"用重攻击打断号令"这条才成立 |
| `reference/game-design-references.md`（外部作品清单） | Left 4 Dead 2 的"**特殊敌人专属音效提示**" ⇒ 1013 出场加一声鼓声/号角，比再加 UI 省事；Sekiro 的"**破防 → 处决**" ⇒ 未来做"优先目标"的收益设计时可参考 |

---

## 5. 技术方案

### 5.1 新增文件

| 文件 | 内容 |
|---|---|
| `Assets/Scripts/Enemy/EnemyCommander.cs` | 组件（挂在指挥官预制体上）：施放计时 / 前摇 / 打断处理 / 施加全场加攻 / 死亡时施加全场减攻 / Inspector 参数 |
| `Assets/Scripts/Enemy/EnemyStatusAtkMod.cs` | `IStatusEffect` 实现：永久、可叠加、暴露层数供 UI 查询；两个 id（`atk_up` / `atk_down`）或一个带符号的实现 |

### 5.2 改动清单（M1–M9）

| # | 文件 | 改动 | 依据 / 风险 |
|---|---|---|---|
| **M1** | `Assets/Scripts/Enemy/Enemy.cs`（Init 段） | 缓存 `_commander` 组件引用 | 与既有 `_cavalry` / `_shieldGuard` 同一模式 |
| **M2** | `Assets/Scripts/Enemy/Enemy.StateSwitch.cs` → `StartAttacking()` | 开头加"永不攻击"早退：`state = EnemyState.Idle; return;` | 复用骑兵分支（`_cavalry.IsMounted`）的早退写法。**已核对**：`Enemy.Movement.cs` 的行军完成分支在 `StartAttacking()` 之后独立调用 `CompleteRushMoveOrder()`，所以早退**不会卡住补齐队列** |
| **M3** | `Assets/Scripts/Enemy/EnemyManager.cs` → `OnEnemyAttackPlayer()` | 伤害 = `enemy.attackDamage × 出站攻击倍率` | 近战的唯一结算点（`Enemy.PerformAttack` → 这里 → `PlayerState.TakeDamage`） |
| **M4** | `Assets/Scripts/Enemy/CavalryEnemy.cs` → `ChargeDamage` 属性 | 同乘出站攻击倍率 | 骑兵是**独立伤害源**（`chargeDamage × 排数 × 波倍率`），不改则 109 完全不吃增益（实测它的伤害才是全场最疼的） |
| **M5** | `Assets/Scripts/Enemy/Enemy.Attack.cs` → `SpawnProjectile()` | 发射箭矢时快照一次出站倍率 | 远程箭矢伤害在 `EnemyProjectile.Launch` 时按值快照（与波次倍率的快照口径一致） |
| **M6** | `Assets/Scripts/Enemy/EnemyStatusRegistry.cs` | 新增状态 id 常量 | 统一查询入口；显示层与逻辑层共用 |
| **M7** | `Assets/Scripts/UI/EnemyHealthBar.cs` + `Assets/Scripts/Enemy/Enemy.Visuals.cs` + `EnemyOutline.OutlineFacts` | ① 血条第三根状态条（增益/减益）+ 层数文本；② 新增 `AtkBuffed` 描边事实位与颜色 | 血条已有 `disease` / `burn` 两根条 + `layersText` 层数文本可直接照抄；描边已有 `PierceHighlighted` / `SuperArmor` / `CFrameAttacking` 等事实位，加一个 `AtkBuffed` 是同构扩展 |
| **M8** | `Assets/Scripts/Enemy/Enemy.cs` + `Enemy.Combat.cs` | 新增 `public System.Action<HitFeedbackSource> OnDamagedWithSource;`，在 `TakeDamage` 的命中点触发（紧邻现有 `OnDamageTaken?.Invoke(this)`） | §2.7 需要区分伤害来源：DoT / 被动 / 道具 / 位移**不能**打断号令，只有玩家本体可以。现有 `OnDamageTaken` 不带来源信息，所以必须加一个带来源的轻量事件（只多一个字段 + 一行触发） |
| **M9** | `Assets/Scripts/Enemy/EnemyCommander.cs` | 订阅 M8 的事件：前摇阶段被打断 → 取消本次施放、重置为完整周期、回 Idle | 实现 §2.7 的打断规则；重置周期与"敌人攻击被打断后重新冷却"同语义 |

> **不需要为"永久状态跨波泄漏"改架构**：既有 `Assets/Scripts/Enemy/Enemy.Pool.cs:75` 已有 `_statusHost?.ClearAll();`（注释写明"施加型状态（灼烧 / 疾病）不能跟着敌人进池复用"），且 `EnemyPool.ReturnEnemy` → `enemy.ResetEnemy()` 是唯一回收路径。施加型状态（加攻 / 减攻）会随回池被清掉，**不会跨波泄漏**。
> 唯一的坑：`Enemy.StatusHost` getter 每次访问都会跑 `EnsureResidentStatusRegistered()`，它只补登记**常驻状态（冻结）**。所以加攻 / 减攻必须"每次施放 / 结算时重新 Add"（`StatusContainer.Add` 同 id 覆盖），**不能**指望被清空后自动回来。

### 5.3 倍率的存放方式（选 B）

| 选项 | 做法 | 结论 |
|---|---|---|
| A | `Enemy` 上加一个运行时 `float outgoingDamageMultiplier` | 最省事，但**无法在 UI 上表达层数**，且跨波清理要手工复位（容易漏） |
| **B（推荐）** | `EnemyStatusAtkMod : IStatusEffect` 挂在 `StatusHost`，自带层数/进度，UI 走 `EnemyStatusRegistry` 查询，回池由**既有**的 `StatusHost.ClearAll()` 统一清理（无需新增改动） | 多一个状态类，但显示与清理都走既有通道 |

**倍率公式**：`最终倍率 = (1 + 加攻层数 × X) × (1 − 减攻层数 × Y)`；减攻只有一层（死亡时一次性结算，不叠加）。

### 5.4 生命周期与清理（"永久"必须钉死）

| 时机 | 处理 |
|---|---|
| 生成 / 从池取出 | 组件 `OnEnable` 订阅 `enemy.OnDeath`；层数天然为 0（回池时状态宿主已被清空） |
| 敌人死亡 | 指挥官组件在 `OnDeath` 回调里施加全场减攻；它自己的状态随回池清理 |
| 回池 | **既有** `Enemy.Pool.cs:75` `_statusHost?.ClearAll()` → 本方案**无需新增**清理代码 |
| 波次结束 / 节点切换 | `EnemyManager.ClearAll` → 全部回池 → 同上 |
| BOSS | 明确**不**加、**不**减（§2.2） |
| 玩家死亡 / 重开 | 同上（走清场路径） |

> ⚠️ `Enemy.Pool.cs` 的重置段会 `OnDeath = null`，所以组件必须在**每次 `OnEnable`** 重新订阅，不能只在 `Awake` 订阅一次。

### 5.5 "在场"枚举的写法

```csharp
var list = EnemyManager.Instance.GetAllAliveEnemies();   // 注意：返回的是内部列表引用
for (int i = 0; i < list.Count; i++)
{
    var e = list[i];
    if (e == null || e == self || e.isBoss || e.state == EnemyState.Dead) continue;
    // 施加加攻
}
```

- **遍历期间不得修改该列表**（不能在这里杀敌 / 回收），否则会破坏遍历。
- 频率：每 4s 一次全场遍历（场上 5~15 个）→ 开销可忽略。
- `GetAllAliveEnemies()` 返回的是内部列表引用，若后续需要，改成返回副本（本次可不改，只要调用方不修改）。

---

## 6. 资产与美术

### 6.1 临时形象方案（2026-11-05 已定：借 104 BOSS 形象 + 放箭 QTE 动作做施法）

| 项 | 做法 | 实测依据 |
|---|---|---|
| **形象** | 直接复用 `Enemy_104.prefab` 的 sprite `BOSS_idle`（32×32）、`localScale = 0.18`、`visualYOffset = 1.035` | 屏幕高度 = 32 × 0.18 = **5.76 单位**，与 1012 的 69.25 × 0.08 = 5.54 基本相当 → **不需要额外缩放** |
| **通用动作** | 复用 `Assets/Animations/Boss_104.controller` 的 Idle / Walk / Dead / HitFlash / Launched_Rise / Launched_Fall 等 | 建议**复制一份**为 `Assets/Animations/Animator_1013.controller`，避免以后改 BOSS controller 时连带影响 1013 |
| **施法动作（关键）** | 复用放箭 QTE（TripleStab）的三段：`Boss_104_QTE_TripleStab_Start`（0.50s）/ `_Loop`（0.30s）/ `_End`（0.30s） | Animator 状态名 `QTE_TripleStab_Start` / `QTE_TripleStab_Loop` / `QTE_TripleStab_End` 在 `Boss_104.controller` 里已存在，可直接 `animator.Play(...)` |

**安全核对（已验证）**：这四个 clip 的 **AnimationEvent 数均为 0** → 复用不会触发任何事件；箭矢是 `QTEController` 自己生成的（不是 clip 事件），而 1013 **不挂 QTEController** → **不会放箭**。

**施法时序映射**（对齐已锁定的 4.0s 周期）：

```
t=0.0s  前摇：举旗预警 0.3s（EnemyAttackTelegraph.BeginWarning）
t=0.3s  播放 QTE_TripleStab_Start（0.5s）
t=0.8s  ← 施放点：施加全场加攻；同时播放 QTE_TripleStab_Loop（0.3s）
t=1.1s  播放 QTE_TripleStab_End（0.3s）→ 回 Idle
t=1.4s  待机，等下一个周期（周期 4.0s）
```

**临时替身的两处已知缺口（可接受，正式美术再修）**：
- `Boss_104.controller` **没有 `HitLeft` / `HitRight`**（BOSS 只有 HitFlash）→ 1013 的受击方向表现回落到 HitFlash（`Enemy.PlayHitVisual` 有 `HasState` 兜底，不会报错）。
- **也没有 `Launched_Getup`** → 被挑飞后起身无动画（`PlayGetupVisual` 无 `HasState` 保护，只会报 Animator 警告）。要补可在 1013 的 controller 里加一个 `Launched_Getup` 状态，motion 指向 `Enemy_1012_Launched_Getup`（体型不同会有不匹配）。

### 6.2 预制体构造步骤（刀 3 的具体动作）

1. 复制 `Assets/Resources/EnemyPrefabs/Enemy_104.prefab` → `Assets/Resources/EnemyPrefabs/Enemy_1013.prefab`。
2. **删除 `QTEController` 组件**（1013 不是 BOSS，不进 QTE 流程）。
3. 按 §6.3 的表改 `Enemy` 组件字段。
4. `attackSequence` **清空**，`attackDamage = 0`（配合 M2 早退兜底）。
5. Animator 的 controller 指向 `Animator_1013.controller`（从 `Boss_104.controller` 复制）。
6. 加 `EnemyCommander` 组件，挂 Inspector 参数（X / 上限 / Y / 周期 / 首施前置 / 前摇）。
7. **把 `Enemy_1013` 加进 `Assets/Scenes/Battle.scene` 里 `Battle/EnemyPool` 的 `enemyPrefabList`**（实测：该列表是 Inspector 引用，当前 13 项；`EnemyPool` **不再扫目录**，2026-10-23 已改成 Inspector 注册）。漏了这步 → 运行时会 fallback 到临时 Cube。
8. `BoxCollider` 保留 104 的原值即可（它只在**破冰推动**的碰撞结算里用，见 `EnemyFreeze.cs` 的 `collisionDamage` 路径，与本单位无关）。

### 6.3 预制体字段对照表

| 字段 | 104 现值 | 1013 目标 | 理由 |
|---|---|---|---|
| `enemyId` | 104 | **1013** | 已定 |
| `enemyName` | boss | 督战鼓手（临时名） | |
| `isBoss` | true | **false** | 非 BOSS：不吃加攻/减攻、不进 BOSS 流程 |
| `occupySlots` | 5 | **1** | 普通单位占一格 |
| `maxHealth` | 4500 | **150** | 点掉它的成本要低；随波次 hp× 缩放 |
| `attackDamage` | 10 | **0** | 永不攻击 |
| `attackSequence` | 2 步 | **清空** | 配合 M2 早退 |
| `attackRange` | 2 | 1 | 普通 |
| `moveSpeed` | 0.3 | 0.3 | 正常行军，随波次前移 |
| `expReward` | 400 | **150** | 优先级目标给足经验（高于 109 的 100） |
| `coinReward` | 10 | 10 | 默认 |
| `bossPhases` | 2 | **空** | |
| `bossHealthBarPrefab` | BossHealthBar | **null** | 走普通敌人血条 |
| `visualYOffset` | 1.035 | 1.035 | 沿用 104 的精灵基准 |
| `localScale` | 0.18 | 0.18 | 沿用 104（屏幕高度与 1012 相当） |
| Animator | `Boss_104.controller` | **`Animator_1013.controller`**（复制） | 隔离 BOSS 后续改动 |

### 6.4 最终形象（另立排期，不在本次范围）

按 `design/art-style-guide.md` 做专属督战鼓手（六帧 Idle + 举旗 + 死亡），并**新做"施法"动作**（临时用的 QTE 箭矢动作只是替身）。画风基准只有两张用户概念图 + 已验收的 1011 六帧 Idle。

---

## 7. 波次编排（**本次不做**）

波次落点属关卡配置工作，不在"做这个角色"的范围内，此处只留结论备查：

- 每一波最多放 1 个；**放后排**才能保住"什么时候杀"的决策（放前排会被玩家顺手点掉，机制自动消解）。
- 放它的那一波需要新增一个逻辑排，并按难度表口径重算该波 `healthMultiplier`。
- 不放 E0（教学段）与 BOSS 波。

---

## 8. 可读性（机制隐形 = 白做）

玩家必须能一眼看出三件事：

| 信息 | 方案 |
|---|---|
| **谁是指挥官** | 本体描边用**金色/橙色**（与 C 技红描边区分），外形用旗帜子物件区分 |
| **它已经号令了几次** | 头顶飘字 `号令 ×N`（N = 已施放次数），随施放更新 |
| **哪些敌人被加成** | 被加成敌人：**橙色描边**（`EnemyOutline` 新事实位 `AtkBuffed`）+ 血条第三根状态条显示层数（照抄疾病条/灼烧条 + `layersText` 模式） |

- 施放前摇：0.8s 举旗预警，可用 `EnemyAttackTelegraph.BeginWarning`（实测该组件提供 `BeginWarning/StopWarning`）。
- **教学关本期不进**：教学关 2026-11-02 刚验收通过，改动风险高；改由正式关的视觉标记自然教学（指挥官第一次出现在 N2 时给一个短提示气泡即可，属可选）。

---

## 9. 验收口径（Play Mode）

1. **不攻击**：指挥官在场 60s，玩家 HP 不因它下降；`attackTimer` 在走但无出伤、无攻击动画。
2. **施放**：每 4s 一次；每次全场上层数 +1；描边与状态条同步刷新；到 5 层不再叠加。
3. **死亡减攻**：杀死指挥官瞬间，**当时仍在场**的敌人出站倍率下降（之后才入场的敌人不吃减攻 —— §2.2 已定口径，不是待议项）。
4. **近战与骑兵都吃到倍率**：1011 单次 3 → 4.5；109 冲锋 32 → 48。
5. **远程也吃到**：105 的箭矢伤害 4 → 6（5 层）。
6. **BOSS 不受影响**：BOSS 波敌人数值与现在一致。
7. **硬门（§2.6）**：指挥官活着时，它所在排不算清空 → 后排不前移、波次不结束；杀掉它后该排立即前移、波次继续。
8. **后排前移（回归补齐机制）**：把指挥官放在 row 2，清空 row 0/1 后它自己走到 row 0，并可被平A命中（确认新单位没有被 `ignoreFillUpOrders` 之类的开关误伤）。
9. **跨波不泄漏**：波次结束后，下一波新生成的敌人层数为 0、无描边（走既有 `StatusHost.ClearAll()` 路径）。
10. **节点切换不残留**：结束节点 → 进入下一节点开场，敌人无加攻、无减攻。
11. **对象池复用**：同一预制体在两波各生成一次，第二次从 0 层开始。
12. **补齐不被卡**：指挥官在场时，同列后方敌人正常行军（回归 M2 早退不卡队列）。
13. **施法表现**：按 §6.1 的时序，前摇 → 举旗预警 → `QTE_TripleStab_Start` → 施放点 → `_Loop` → `_End` → 回 Idle；全程**不放箭**、不弹 QTE UI、无异常日志。
14. **形象继承**：1013 的屏幕高度与 1012 相当（不需额外缩放）；受击表现为 HitFlash；被挑飞后起身无动画但**不报错**。
15. **号令可被打断**：前摇中用招式命中它 → 本次施放取消（不加攻、层数不涨），回 Idle；下一次尝试在 4.0s 后。
16. **只有玩家本体能打断**：用灼烧 / 疾病 / 地刺 / 击退打它 → 施放**不**被打断（层数照常涨）。
17. **打断不误伤**：指挥官以外的敌人、以及被击退 / 眩晕的指挥官，都不会出现"漏放号令"或"重复施放"。

---

## 10. 风险清单

| 风险 | 表现 | 处理 |
|---|---|---|
| 永久状态随对象池泄漏 | 下一波敌人白带 +50% | 既有 `Enemy.Pool.cs:75` `_statusHost?.ClearAll()` 已覆盖；验收 9 回归即可，**无需新增架构改动** |
| 早退破坏补齐队列 | 后排不动 | 已核对 `CompleteRushMoveOrder` 独立调用；验收 12 |
| 指挥官放前排 → 机制被自动消解 | 玩家清前排时顺手点掉它，"什么时候杀"的决策消失 | 落位定在后排（§7） |
| 轻点静音 | 玩家用很轻的伤害每 4s 点一下 → 号令几乎永不落下，机制只剩死亡减攻 | 150 血的上限让"压制"在几秒内收敛成"击杀"；若实测仍偏弱 → 把重试周期调到 2.0s 或 HP 降到 120（都是 Inspector 参数） |
| 硬门造成卡关感 | 波次迟迟不推进 | 指挥官会随波次前移进 row 0（可平A命中），不会死锁；验收 7/8 |
| 减攻覆盖小（早杀=覆盖少） | 玩家觉得奖励不明显 | Y 取 25%（够大） |
| 机制隐形 | 玩家不知道谁被加成 | §8 三条可读性（描边 + 飘字 + 状态条） |
| 与 109 抢优先级 | 两个目标互相稀释 | 同波不共存；指挥官血量压低（150）保证点掉成本低 |
| 玩家反复横跳 | 操作碎、疲劳 | 指挥官摆后排 + 血量 150，玩家可用 3 排手段一次点到，不必反复横跳 |
| 依赖未实现系统 | —— | 本方案**只依赖既有通道**（状态宿主 / 描边 / 血条 / 事件 / 补齐调度），不依赖天赋树、节点奖励、QTE 等 |

---

## 11. 已拍板事项与遗留

**已定（2026-11-05，可直接落地）**

| 项 | 结论 |
|---|---|
| 数值 | +10%/次、上限 5 层、死亡减攻 −25%、周期 4.0s、首施前置 2.5s、施放前摇 0.8s |
| BOSS | **排除**（BOSS 既不吃加攻也不吃减攻） |
| 敌人 id | **1013** |
| 形象 | 临时借 104 BOSS 形象 + 通用动作；施法动作用放箭 QTE（TripleStab）；最终形象另立排期 |
| 号令可打断 | **可打断**（指挥官没有霸体）→ 规则与来源划分见 §2.7 |
| 波次落点 | 本次不做（属关卡配置工作） |

**遗留（无阻塞项）**

- 实现期可自主决定的小项：减攻的落地表现（全局飘字 / 描边变色 / 音效）、头顶"号令 ×N"的飘字实现方式、`EnemyStatusAtkMod` 用两个 id（`atk_up` / `atk_down`）还是一个带符号的实现。
- 若实测"轻点静音"偏强（§10），优先调 `EnemyCommander` 的两个 Inspector 参数（重试周期 / HP），不改代码结构。

---

## 12. 实施顺序（建议 4 刀）

| 刀 | 内容 | 验收 |
|---|---|---|
| **刀 1** | 逻辑层：状态类 + 倍率（M1/M3/M4/M5/M6）+ 带来源的受击事件（M8）+ `EnemyCommander` 组件（含打断处理 M9），**用现有 1012 预制体临时挂组件验证行为**，不碰美术 | 验收 1/2/3/4/7/15/16 |
| **刀 2** | 显示层：状态条 + 层数文本 + 描边事实位 + 头顶飘字（M7） | 验收 2/3 的可读性 |
| **刀 3** | 正式预制体：按 §6.2 的 8 步造 `Enemy_1013.prefab`（复制 104 → 删 QTEController → 改字段 → 复制 controller → 加组件 → 加进 `Battle/EnemyPool.enemyPrefabList`） | 验收 13/14 + 1/2 |
| **刀 4** | 全量回归（教学关 + 正式路线）+ 文档回写 | `design/gameplay-pillars.md` §4.1 反制表加一行；`design/game-mechanics.md` §5.1 图鉴加一行；`plan/first-level-difficulty-20261103.xlsx`「敌人能力」页加一行 |

**回写要点**：反制表要写清它是**硬门**（所在排不清空则波次不推进），决策是**"什么时候杀"**（号令层数 vs 去后排的时间成本），**不是"要不要杀"**；并注明"后排敌人会随波次前移到前排，不是打不到，只是慢"。

---

## 13. 决策记录（2026-11-05）

> 记录本轮讨论里**被拍板 / 被纠正**的结论，防止后续接手者重犯同样的误判。

| # | 议题 | 结论 | 来源 |
|---|---|---|---|
| D1 | 死亡减攻是否覆盖"之后才入场"的敌人 | **不作为设计议题**：按"在场"口径直接推导——死亡瞬间在场的生效，之后入场的不吃 | 用户裁定 |
| D2 | "105 远程玩家打不到" | **纠正**：所有敌人都会随前面整排被打空而**一排一排前移到 row 0** ⇒ "打不到"不成立，只是慢。**推论**：1013 应放**后排**（放前排会被顺手点掉，机制自动消解） | 用户纠正 + 代码核实（§2.5） |
| D3 | 永久状态会不会随对象池跨波泄漏 | **纠正：不存在**。`Enemy.Pool.cs:75` 已有 `_statusHost?.ClearAll()`（由 `ReturnEnemy → ResetEnemy` 调用）⇒ 本方案**无需新增清理代码** | 代码核实（我上一轮 grep 用的是公开属性 `StatusHost`，实际字段为私有 `_statusHost`，故漏查） |
| D4 | 号令能否被打断 | **可打断**（1013 没有霸体）；仅玩家本体伤害（`BasicAttack` / `ActiveSkill` / `Ultimate`）能打断，DoT / 被动 / 道具 / 位移 / 地刺 / 碰撞不能 | 用户裁定（§2.7） |
| D5 | 增攻"有没有牙"（对 3 点血的杂兵） | **不作为机制设计前提**；数值参考见 §3，平衡调整属另议 | 用户裁定 |
| D6 | 数值 | +10%/次、上限 5 层、死亡减攻 −25%、周期 4.0s、首施前置 2.5s、施放前摇 0.8s | 用户拍板 |
| D7 | BOSS | **排除**（不吃加攻、不吃减攻） | 用户拍板 |
| D8 | 形象 | 临时借 104 BOSS 形象 + 通用动作；施法用放箭 QTE（TripleStab）三段；最终形象另立排期 | 用户拍板（§6.1） |
| D9 | 敌人 id | **1013** | 用户拍板 |
| D10 | 波次落点 | **不属本次范围**（属关卡配置工作，另行排期） | 用户裁定（§7） |
| D11 | 落地时机 | 其他 agent 在并行工作期间**只写文档、不落地实现** | 用户要求 |

### 13.1 两条来自代码核实的"别再踩"的结论

1. **不要凭印象判断"打得到打不到"**：后排敌人会随整排清空前移；判断可达性要看 `LogicalRowQueue` + `WaveMarchScheduler` 的派单逻辑，不要看文档旧描述。
2. **不要凭 grep 结果下"存在泄漏"的结论**：字段可能是私有且命名与公开属性不同（`_statusHost` vs `StatusHost`）；下结论前先确认字段名与唯一回收路径。
