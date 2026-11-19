---
id: kd_b3c6d966-7f64-4681-ac5a-387bb4ffc5b0
summary: 投壶兵 1014 的纯机制描述（不含美术）：单位定位、实读数值、命中结算顺序、毒的口径、打落/招架两条解法、与各系统的交互表、数值调整入口与已知缺口。数据来自 Enemy_1014.prefab 与源码实读。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 投壶兵 1014 · 纯机制描述

> **这份文档只讲机制，一条美术都不涉及。** 形象 / 画风 / 道具 / 组图见 `design/enemy-1014-art-brief.md`。
> **数据来源**：2026-11-16 实机核对 —— `Assets/Resources/EnemyPrefabs/Enemy_1014.prefab` 活体实读 + 源码实读，
> 不是任何交接文档的转述。数值以 **Prefab / 资产里的真值**为准，改数值只改资产。

---

## 0. 一句话

**一个远程毒投掷兵。** 他朝玩家扔一只**带毒的陶壶**：

- 陶壶的**直接伤害可以被防御吸收**；
- 但**毒吸收不掉** —— 防御只免直伤，毒照扣；
- 所以玩家的对策不是"扛住"，而是**在陶壶落地前把它处理掉**：**挥枪打落**（半径 3.5）或**招架弹开**（半径 4）。

---

## 1. 单位定位

| 项 | 值 | 出处 |
|---|---|---|
| 敌人 id / 名称 | `1014` / **投壶兵** | `Enemy_1014.prefab` → `Enemy` 组件 |
| 攻击方式 | 远程（`isRanged = true`） | 同上 |
| 核心威胁 | **毒不可防御** ⇒ 防御位不再是万能保险 | `PlayerPoisonStatus.cs` |
| 唯一对策 | 处理掉空中的陶壶（打落 / 招架弹开） | §4 |
| 占位 | 1 格（`occupySlots = 1`） | Prefab |
| 出现位置 | **当前未上任何关卡**。只在 Battle 场景 `EnemyPool.enemyPrefabList`（16 项）里注册；`Assets/RouteData/` 全库无 1014 引用 | 2026-11-16 实测 |
| 外观 | **纯占位**：沿用 106 弓箭手的贴图与动画（`Assets/Sprites/Enemy/Enemy5/enemy5_idle.png` + `Assets/Animations/Enemy_105.controller`）；**投掷物也是箭矢占位**（`Assets/Prefabs/arrow.prefab`） | Prefab / `arrow.prefab` 实读 |
| 教学 | 新兵种按规矩**教学关不出** | `plan/week-20261114-new-enemy-two-skills-parry-counter.md` §4 W4 |

---

## 2. 数值（实读自 `Enemy_1014.prefab`）

### 2.1 本体

| 字段 | 值 |
|---|---|
| `maxHealth` | 100 |
| `attackDamage` | **5**（陶壶直伤，**可被防御吸收**） |
| `attackSpeed` | 0.3 |
| `attackRange` | 4 |
| `moveSpeed` | 0.5 |
| `normalFormDamageReductionPercent` | 0 |
| `maxPoise` / `stunDuration` / `poiseStunDuration` | 40 / 2 / 1.5 |
| `freezeDuration` / `freezeChanceMax` | 4 / 0.75 |
| `coinReward` / `expReward` | 10 / 50 |

### 2.2 投射物

| 字段 | 值 | 说明 |
|---|---|---|
| `projectilePrefab` | `Assets/Prefabs/arrow.prefab` | ⚠ **箭矢占位**，不是陶壶 |
| `projectileArcHeight` | 3 | 抛物线弧高（世界单位） |
| `projectileFlyDuration` | 1 s | 飞行时长 |
| `projectileZTargetOffset` | 5 | 落点 Z = 相机 Z + 5 |
| `useFixedProjectileLandingX` | true | 落点 X 取**固定值**（不随机） |
| `fixedProjectileLandingXByColumn` | `[-2, -1, 0, 1, 2]` | 五列各一个落点 X |
| `arrowConfig` | `Assets/ScriptableObjects/ArrowConfigs/Enemy_106_RowFlight.asset` | ⚠ **复用 106 的排数飞行配置**（按排数调飞行时长/弧高） |
| **`projectilePoisonDamage`** | **2.5 / 跳** | |
| **`projectilePoisonDuration`** | **4 s** | |
| **`projectilePoisonTickInterval`** | **0.5 s** | |
| **`projectileCanBeShotDown`** | **true** | 投壶兵与 106 为 true；**其余远程默认 false** |

---

## 3. 战斗流程

### 3.1 出招 → 投射物离手

1. 敌人进入 `Attacking` → `AttackSpawn` 前摇 → 命中帧调 `Enemy.SpawnProjectile()`（`Enemy.Attack.cs:231`）。
2. 投射物是 `projectilePrefab` 的实例，写入发射时的**伤害快照**（含督战单位 1013 的号令倍率 `EnemyStatusAtkMod.GetOutgoingMultiplier`）⇒ **号令层数之后变化不影响已在途的陶壶**。
3. 同时把毒参数与 `canBeShotDown` **随发射写入投射物**（`Enemy.Attack.cs:265-268`）⇒ 这些是**发射瞬间的快照**，不是命中时再读敌人。
4. `proj.Launch(...)` → 抛物线飞行，落点由 `projectileZTargetOffset` 与 `fixedProjectileLandingXByColumn[敌人所在列]` 决定。
   - 飞行途中投射物**独立于敌人**（敌人死亡/受击不影响已飞出的陶壶）。

### 3.2 命中 → **先毒、后伤**（`EnemyProjectile.OnArrival`）

```
毒先于伤害结算：
  if (poisonDuration > 0 && poisonDamagePerTick > 0)
      PlayerState.Instance?.ApplyPoison(毒/跳, 持续, 间隔);
  然后：
  PlayerState.Instance?.TakeDamage(damage, 来源);
```

⇒ **顺序后果**：陶壶即使被防御挡下，**毒照样上**；毒先上、直接伤害后被吸收。

### 3.3 毒怎么扣血（`PlayerPoisonStatus`）

| 规则 | 实现 |
|---|---|
| 结算路径 | `PlayerState.TakeDamageUnblockable()` —— **直接扣血**，不走 `TakeDamage` |
| ⇒ 防御吸收不到 | 防御只处理"一次伤害" |
| ⇒ 反伤护盾弹不走 | 反射通道同样只处理直接伤害 |
| ⇒ 不吃蓄力减伤 / 不屈天赋 | 那是"一次伤害"的判定，不是持续伤害 |
| 跳伤间隔 | 0.5 s（`projectilePoisonTickInterval`） |
| 持续 | 4 s |
| 叠层 | **不叠层**：同 id 后登记者**覆盖**先登记者，`Refresh()` 取 `max(强度)`、`max(时长)` |
| 归属 | 挂在玩家 `PlayerState.StatusHost`（与反伤护盾同级）；状态 id = `poison`，优先级 30 |

**实测口径下的总毒量**（按 `Tick` 的实际顺序推）：
第 0.5 s 起跳，每 0.5 s 一跳，4.0 s 时状态移除 ⇒ **共 7 跳 × 2.5 = 17.5 点伤害**。

> 口径提示：早前交接文档写的是"毒是直伤的 4 倍"（按 2.5×4s=10 对直伤 5 估的），
> **按代码实际是 7 跳 17.5 点 ≈ 直伤的 3.5 倍**。对外口径以本节为准。

### 3.4 两条解法

| 解法 | 触发条件 | 结果 | 代码位置 |
|---|---|---|---|
| **挥枪打落** | 玩家**出招中**（`_actionLockTimer > 0`）且陶壶在**半径 3.5** 内（`projectileShootDownRadius`） | 陶壶被清除：**不伤害、不留毒** | `AttackSystem.ShootDownProjectilesNear` |
| **招架弹开** | 招架命中判定时，陶壶在**半径 4** 内（`parryProjectileRange`）且非 QTE 投射物 | 陶壶旋转坠落淡出：**不伤害、不留毒** | `AttackSystem.DeflectProjectilesNear` → `EnemyProjectile.Deflect()` |

⚠ **两条硬约束**（都实测过）：
1. **打落的触发窗口 = 整个动作播放期**（不是只在命中帧）⇒ 抬手阶段就已经在打落了。
2. **世界停走时不打落**（`Time.timeScale > 0` 才扫）⇒ 教学暂停 / 暂停菜单 / 三选一弹窗打开时，陶壶不会被打落。
   （这是刻意保留的旧行为：不写这条，教学拍会被远程偷伤。）

⚠ **只有 `canBeShotDown = true` 的投射物会被打落** ⇒ 弓手的箭**保持原行为**，不会被顺手打掉。
（`Enemy.projectileCanBeShotDown` 字段默认 **false**，只有 1014 / 106 显式打开。）

---

## 4. 这个兵种的设计意图（一句话规则）

> **防御只能挡住"这一下"，挡不住"接下来这四秒"。**

- 已有远程（1051 弓手 / 106 弓箭手）：防御站着不动也能全免 ⇒ 远程不构成决策压力。
- 投壶兵把"防御"从**万能保险**降级成**只挡直伤**：防御位仍能免掉 5 点直伤，但会吃到 17.5 点毒。
  ⇒ 防御不再是"我就站着不动"的答案，**必须主动处理空中目标**。
- 于是"半空打落投射物"这条新规则有了用武之地：它把远程变成**高优先级目标**，并且给了玩家一条**主动的**解法。

---

## 5. 与其它系统的交互（实读）

| 系统 | 投壶兵的表现 |
|---|---|
| **防御（Guard）** | 免掉陶壶直伤 5；**毒照扣 17.5** |
| **招架（Parry）** | 陶壶在半径 4 内可被**弹开**（非 QTE 投射物都吃这一条）⇒ 免伤免毒 |
| **挥枪打落** | 出招中、半径 3.5 内 ⇒ 免伤免毒（**唯一"主动清场"手段**） |
| **反伤护盾** | 毒是直扣，**不被反射** |
| **蓄力减伤 / 不屈天赋** | 只作用于"一次伤害"⇒ **对毒无效** |
| **冻结（冰）** | 走敌人通用冻结字段（`freezeDuration 4` / `freezeChanceMax 0.75`），与毒无关 |
| **破防 / 霸体** | `normalFormDamageReductionPercent = 0`，不自带减伤；`guardBreakBonus 45` |
| **QTE 投射物隔离** | 陶壶**不是** QTE 投射物 ⇒ 会被常规招架弹开 |
| **敌人死亡 / 受击** | 不影响**已在途**的陶壶 |

---

## 6. 数值调整入口（改资产，不改代码）

| 想改什么 | 改哪里 |
|---|---|
| 直伤 | `Enemy_1014.prefab` → `Enemy.attackDamage` |
| 毒强度 / 时长 / 间隔 | `Enemy_1014.prefab` → `Enemy.projectilePoisonDamage` / `projectilePoisonDuration` / `projectilePoisonTickInterval` |
| 能不能被打落 | `Enemy_1014.prefab` → `Enemy.projectileCanBeShotDown`（**默认 false，别全局改**） |
| 打落半径 | `AttackSystem`（场景组件）→ `projectileShootDownRadius`，默认 **3.5** |
| 招架反弹半径 | `AttackSystem` → `parryProjectileRange`，默认 **4** |
| 飞行时长 / 弧高 / 落点 | `Enemy_1014.prefab` → `projectileFlyDuration` / `projectileArcHeight` / `projectileZTargetOffset` / `fixedProjectileLandingXByColumn` |
| 按排数的飞行曲线 | `Assets/ScriptableObjects/ArrowConfigs/Enemy_106_RowFlight.asset`（**当前与 106 共用**） |
| 毒扣血路径 | `PlayerPoisonStatus.cs`（`Tick` → `PlayerState.TakeDamageUnblockable`） |

> ⚠ **改 Prefab 的坑**（2026-11-14 实测踩过）：
> ① `git pull` 后 Unity 重新导入会用**内存里的旧 Prefab** 覆盖磁盘 ⇒ 改磁盘没用；
> 正确做法是**改内存实例 + `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()`** 显式写盘。
> ② `.meta` 里的 GUID 是 base64，场景 YAML 用 32 位 hex ⇒ 拿 `.meta` 的串去 grep 场景**搜不到**。
> ③ 域重载会保留 ScriptableObject / 场景实例的**内存值** ⇒ 只改 C# 默认值**运行时不生效**，新字段必须写进 `.asset` 并 `SaveAssets()`。

---

## 7. 已知缺口 / 待拍板

| # | 事项 | 状态 |
|---|---|---|
| 1 | **正式外观** | 现在是 106 弓箭手占位（含**箭矢占位**投掷物）⇒ 形象 + 陶壶道具都要重做，见美术需求书 |
| 2 | **106 也临时带毒** | `Enemy_106_Fixed.prefab` 的 `projectilePoisonDamage/Duration` 目前是 `2.5 / 4`、`canBeShotDown = true`（当时指定当载体验证用）⇒ **要还原就清零** |
| 3 | **箭要不要也能被打落** | 现在箭保持原行为（不会被顺手打掉）。若要改，把 `projectileCanBeShotDown` 默认改 true 或逐个敌人打开 ⇒ **会改变现役远程的对策面，需用户拍板** |
| 4 | **数值校准** | 毒 2.5 / 4 s / 0.5 s 间隔是初值；直伤 5 未与 1051 弓手对齐过 |
| 5 | **未进任何关卡** | 只在 `EnemyPool` 注册，`Assets/RouteData/` 无引用 ⇒ 波次编排由用户定（2026-11-09 用户口径：关卡节奏/波次只由用户调整） |
| 6 | **教学** | 新兵种教学关不出；教学拍要在陶壶出现时给出"处理空中目标"的引导（**待设计**） |

---

## 8. 相关文档

- 美术需求书（形象 / 画风 / 实机画面 / 已有兵种 / 组图流程）：`design/enemy-1014-art-brief.md`
- 本周需求与方案来源：`plan/week-20261114-new-enemy-two-skills-parry-counter.md` §2 §4.1
- 本轮交接与踩坑：`plan/session-handoff-20261114.md`
- 玩法与机制总览：`design/gameplay-pillars.md`、`design/game-mechanics.md`
- 不可变约束（位移/补齐）：`design/immutable-constraints.md`
