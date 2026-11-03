---
id: kd_2ffdf028-186a-461f-82ff-2de6812d8fa3
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 局内成长与解锁内容

> 2026-11 设计文档。属于需求 **R3 / R4（消费端）/ R6 / R7 / R8**。
> 解锁内容的**产出端**（天赋树）见 `design/talent-tree-architecture.md`。
> 现状盘点与实测证据见 `design/out-of-game-growth-v2-analysis.md`（§2.5 / §2.6 / §2.7）。

---

## 1. 定位与边界

本文覆盖「**局内一局之内**玩家能拿到什么、能带多少、满了怎么办」，以及「**解锁内容怎么真正生效到局内**」。

| 负责 | 不负责 |
|---|---|
| 三选一抽池与选项呈现 | 天赋页长什么样（→ `design/talent-page-ui.md`） |
| 技能进池的门槛（流派分线） | 关卡里出什么敌人（→ `design/stage-configuration.md`） |
| 主动 / 被动槽位上限与满槽处理 | 招式连段结构（→ `design/move-split-and-derivatives.md`） |
| 技能等级上限 | |
| 秘籍掉落 → 永久解锁 | |

---

## 2. 三选一流程（现状）

```
升级触发 → UpgradeChoiceManager 排队
  → ShowNextChoice()
    → DrawChoices() → UpgradePoolSampler.CollectUnified() → DrawByRarity()
    → 弹出 UpgradeChoicePopup（card1/2/3，候选不足时隐藏多余卡）
  → 玩家点击 → ConfirmChoice() → UpgradeEffectManager.ApplyUpgrade()
  → FinishAllChoices() → timeScale = 1
```

| 环节 | 位置 |
|---|---|
| 流程管理 | `Assets/Scripts/Core/UpgradeChoiceManager.cs`（460 行） |
| 抽池 | `Assets/Scripts/Core/UpgradePoolSampler.cs` |
| 效果应用 | `Assets/Scripts/Core/UpgradeEffectManager.cs` + `.Apply.cs` |
| 弹窗 | `Assets/Scripts/UI/UpgradeChoicePopup.cs` + `UpgradeCard.cs` |

**扩展点（已存在，本周要用）**

| 扩展点 | 位置 | 用途 |
|---|---|---|
| `UpgradePoolSampler.AddRequirementProvider(provider)` | 采样器 | 入池条件源（天赋解锁走这个） |
| `UpgradeChoiceManager._selectionEffects` | 管理器 | 选中后的额外效果 |
| `UpgradeEffectManager.RegisterNumericApplier(effectType, applier)` | 效果管理 | 数值型升级效果 |
| `UpgradeEffectManager.RegisterExecutor(effectType, executor)` | 效果管理 | 行为型升级效果 |
| `BuffManager.RegisterApplier(statId, applier)` | Buff | 属性修正接入 |
| `UpgradeChoiceManager.forcedChoices` | 管理器 | 教学关固定三选一 |

> **纪律**：新增能力一律走这些注册表，**不要往 `ApplyUpgrade` 的分派里加 if-else**（该文件头注释已明写）。

---

## 3. 池与稀有度

`Assets/ScriptableObjects/ActiveSkills/ActiveSkillPoolConfig.asset`

| 稀有度 | 权重 | 条目 |
|---|---|---|
| Common | 0.60 | 专注 · 智慧 · 铁壁 · 地刺 · **火龙舌** · **染病** |
| Rare | 0.35 | 震荡 · **冲击波** · **箭雨** · 蛇形喷射 · 疾风 |
| Legendary | 0.05 | **旋风** · 拔苗助长 · **海浪** |

（加粗 = 主动技能，池内主动 6 个）

### 3.1 主动技能资产 8 个，池内只有 6 个

| 在池内 | 不在池内 |
|---|---|
| 火龙舌 · 染病 · 冲击波 · 箭雨 · 旋风 · 海浪 | **冰雾** `ActiveSkill_IceMist`（且 `_grantDefaultSkills = 0`，Battle.scene 实测 → **游戏里根本没有冰雾**）· **烈焰喷射** `ActiveSkill_FireAOE`（只有被动「蛇形喷射」在池里） |

> 冰雾的归属与「破冰顶开」技能升级的迁移相关，见 §8。**本周待定**。

---

## 4. 流派分线与入池

### 4.1 机制（已存在，零使用）

`UpgradeDefinition.requiredTalentIds` + `TalentUpgradeRequirementProvider`：

```
池收集 → 对每个候选调 RequirementsMet(def)
       → TalentUpgradeRequirementProvider.IsSatisfied()
       → 逐个检查 requiredTalentIds 里的 nodeId 是否已解锁
```

⚠️ **语义是 AND**（列表里所有 id 都必须已解锁）。

### 4.2 流派线的落地方式

> **载荷约定（2026-11-02 裁决后）**：节点的载荷用 **`upgradeConfig`**（指向该技能资产），**不靠 `flagId`** ——
> `flagId` 目前全项目零消费者（`IsFlagUnlocked` 无调用点），用它做凭证会得到一个“买了但没生效”的节点。

**「分支根节点 + 技能各指向自己那个节点」**：

```
蓄力流入门（节点 A）           ← 分支根，负责把这条枝接进树
 ├─ 铁壁（节点 B）→ 节点 B.upgradeConfig = 铁壁资产
 │                   铁壁资产.requiredTalentIds = [B]
 ├─ 震荡（节点 C）→ 节点 C.upgradeConfig = 震荡资产
 │                   震荡资产.requiredTalentIds = [C]
 └─ 冲击波（节点 D）→ 同上
```

**双向引用是必要的**：

| 方向 | 作用 |
|---|---|
| 节点 → 技能（`upgradeConfig`） | 面板自动显示该技能的图标与 1 级文案（`TalentNodeInfo`） |
| 技能 → 节点（`requiredTalentIds`） | 节点未解锁时，该技能**不进局内三选一池**（`TalentUpgradeRequirementProvider`，AND 语义） |

玩家买「蓄力流入门」→ 这条枝在树上出现；再逐个买技能节点 → 对应技能**开始进池**。

**为什么不用根节点做 `requiredTalentIds`**：那样会一次解锁整条枝，失去「逐个解锁」的节奏感；而 AND 语义又做不到「买了枝上任一节点就进池」。

> ⚠️ **AND 语义的坑**：`requiredTalentIds` 是「全部满足」。若技能 A 的前置是技能 B，而玩家先拿了 A 拿不到 B，则 A **永远拿不到**（不可达节点）。
> → 用 `prerequisites` 表达技能间前置时，**前置技能必须是所有玩家都会买的**，否则会把技能做成陷井。

### 4.3 通用技能常驻（不进天赋页）

| 技能 | id | 处理 |
|---|---|---|
| 疾风 | `attack_speed` | **不进任何 `requiredTalentIds`** → 默认在池里 |
| 智慧 | `exp_boost` | 同上 |
| 地刺 | `spike_trap` | 同上 |
| 箭雨 | `active_arrow_rain` | 同上 |

### 4.4 各流派的技能清单（对应天赋节点）

| 流派 | 技能 | id | 天赋节点 |
|---|---|---|---|
| 蓄力流 | 铁壁 | `charge_damage_reduction` | `talent_skill_ironwall` |
| | 震荡 | `charge_hit_shockwave` | `talent_skill_shockwave` |
| | 冲击波 | `active_charge_attack_shockwave` | `talent_skill_charge_wave` |
| 位移流 | 专注 | `cooldown_reduction` | `talent_skill_focus` |
| | 海浪 | `active_wave` | `talent_skill_wave` |
| | 旋风 | `active_cyclone` | `talent_skill_cyclone` |
| | **染病** | `active_disease` | `talent_skill_disease` |
| 火系 | 蛇形喷射 | `timed_fire_aoe` | `talent_skill_snake` |
| | 火龙舌 | `active_fire_line` | `talent_skill_fireline` |

> **注意**：火系是**跨流派协同**（`design/in-run-build-archetypes.md`：「定位：跨流派通用组合，不单独作为第三条完整 Build」）。它作为一条分支给玩家开线，但**任何流派都能顺手拿**——即火系的技能解锁**不应挡住**其他流派。

### 4.5 流派内的组合前置（`prerequisites`，已存在零使用）

`UpgradeDefinition.prerequisites`（`UpgradePrerequisite`：需要某升级 + 最低等级）→ 表达「流派内部的组合解锁」，例：

```
震荡.prerequisites = [铁壁, Lv1]   // 先有铁壁才出现震荡
```

> ⚠️ **约束**：前置是 AND。如果玩家买了震荡但没买铁壁，震荡就**永远拿不到**（不可达节点）。
> → 用 `prerequisites` 时必须保证前置技能是**所有玩家都会买**的，否则会把技能做成陷阱。

---

## 5. 槽位体系

### 5.1 现状与目标

> ✅ **A1 已实现（2026-11-03）**：三个槽位已拆成三个独立字段，基础值 **3 主动 / 3 被动 / 4 道具**。
> 「天赋渐进解锁更多槽位」的读开关仍属 B 批（**未做**）；被动槽口径仍属 A2（**未做**）。

| | 拆分前 | 拆分后（A1 已实现） | 后续 |
|---|---|---|---|
| 主动技能槽 | 4（`HeroConfig.itemSlotCount`，**与道具槽共用**） | **3**（`HeroConfig.activeSkillSlotCount` → `ActiveSkillInventory.Capacity`） | 天赋渐进解锁（B 批） |
| 被动槽 | 6（`UpgradeChoiceManager.CreateSampler` 里的硬编码 `PassiveUpgradeCount >= 6`） | **3**（`HeroConfig.passiveSlotCount` → `UpgradeChoiceManager.PassiveSlotCapacity`） | 天赋渐进解锁（B 批） |
| 道具槽 | 4（同一个 `itemSlotCount`） | **4**（`HeroConfig.itemSlotCount`，**语义不变**） | 不变 |

### 5.2 必须拆字段（阻断项）✅ 已完成

| 字段 | 归属 | 默认 | 读取处 | 状态 |
|---|---|---|---|---|
| `activeSkillSlotCount` | `HeroConfig` | 3 | `ActiveSkillInventory.Capacity` | ✅ |
| `passiveSlotCount` | `HeroConfig` | 3 | `UpgradeChoiceManager.PassiveSlotCapacity` → 采样器的被动上限判定 | ✅ |
| `itemSlotCount` | `HeroConfig`（保留原义） | 4 | `ItemInventory.Capacity` | ✅（未改语义） |

⚠️ **序列化陷阱**：Unity 新增 int 字段在老资产里是 **0**。必须**同时改 `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` 填 3**，并在读取处 `Mathf.Max(1, v)` 兜底。

> ✅ A1 已按此处理：`Hero_Zhangfei.asset` 已显式写入 `activeSkillSlotCount: 3` / `passiveSlotCount: 3`（磁盘 YAML 已回读确认），
> 两处读取点均带 `Mathf.Max(1, v)` 兜底（老资产 / 漏改资产 → 至少 1 槽，不会归零）。


### 5.3 槽位满时的行为（现行）

| 情况 | 现行行为 | 代码位置 |
|---|---|---|
| 被动满 | 新被动**直接从候选池剔除**，玩家看不到 | `UpgradePoolSampler`：`currentLevel == 0 && IsPassiveCapReached() → continue` |
| 主动满 | `CanAcquire` false → **同样剔除** | `CollectUnified` → `CanAcquireActiveSkill` |
| 道具满 | **只有这一路弹「弃置弹窗」**（`ItemDiscardPopup`） | `UpgradeEffectManager.Apply.cs` 第 61-81 行 |

→ 现行是「**满了就藏起来**」，玩家不会知道自己被卡住了。**R8 要改成金币补偿。**

### 5.4 ✅ 被动槽口径与 UI 不一致（已解决）

> ✅ **A2 已实现（2026-11-03）**：按 **方案 A**——`PassiveSkillRow` 现在显示**全部占用被动槽的条目**（含 Numeric），
> 口径与上限判定完全一致。**数值逻辑未动。**

| | 口径 |
|---|---|
| **上限判定** | `PassiveUpgradeCount` = `_appliedUpgrades.Count` → **含 Numeric**（专注 / 智慧 / 疾风 / 铁壁 / 地刺 / 拔苗助长…） |
| **被动排 UI 显示** | `PassiveSkillRow.OccupiesPassiveSlot()` = `Numeric` + `AttackPassive` + `TimedPassive` | ✅ 已对齐（原先只有后两类） |

→ 玩家塞了 3 个数值被动，**被动排上现在能看到 3 个图标**（原先一个都不显示，但确实占槽）。

处理方案见 `design/talent-tree-architecture.md` §6.2（A：UI 显示全部占用 / B：Numeric 不占槽）。

#### ⚠️ 实测新发现：底部有两处被动展示，内容重叠

| 位置 | 组件 | 显示内容 |
|---|---|---|
| 底部**中间**卡片 | `BuffDisplayPanel` 的 ColumnA（6 槽） | **全部**被动条目 + **数值**（如 8% / 10%） |
| 底部**右侧**条 | `PassiveSkillRow`（4 槽 + 溢出） | **全部**被动条目 + **等级**（A2 后） |

→ A2 之后两处显示的是**同一批条目**（重叠；A2 之前 ColumnA 显示全部、右侧条只显示触发型，本来就部分重叠）。
两处**职责不同**（一个给数值、一个给图标与等级），但视觉上冗余。
**是否收敛属于 HUD 结构问题 → 归 UI / 美术侧判断，本周不动。**

> 另一个口径细节：`PassiveUpgradeCount` 按 `upgradeId` 去重，而被动排按 `UpgradeDefinition` 逐条列。
> 当前项目内 **40 个升级资产无重复 upgradeId**（已实测），两者数目一致。

### 5.5 UI 槽位数

| 组件 | 当前槽位 | 说明 |
|---|---|---|
| `FrontItemBar._itemSlots` | 4 | 局内主动技能 + 道具 |
| `PassiveSkillRow._slots` | 4 + 溢出「+N」 | 被动展示 |

→ 天赋最多可解锁到 4 主动 / 4 被动时，**现有 4 个槽位刚好够**；再往上需要补槽位。

---

## 6. 满槽 → 金币补偿（R8）

> ✅ **A3 已实现（2026-11-03）**：三选一**没有候选**时不再静默跳过，而是把这次升级折算成
> `30 + 5 × 当前等级` 铜钱（基数与每级增量都是 Inspector 可调字段）。
> 实测：Lv1 → +35、Lv2 → +40、Lv3 → +45，局内 session → 结算 → 存档（`props` 里的 `PropType.Coin`）→ 天赋树可花。

### 6.1 触发点

`Assets/Scripts/Core/UpgradeChoiceManager.cs` 第 258-265 行：

```csharp
if (_currentChoices == null || _currentChoices.Count == 0)
{
    if (_pendingQueue.Count > 0 || ...) ShowNextChoice();
    else FinishAllChoices();      // ← 现在：直接跳过，玩家什么也没拿到
    return;
}
```

### 6.2 规则

| 项 | 内容 |
|---|---|
| 触发条件 | `CollectEligible()` 返回 **0 候选**（所有条目满级或被槽位排除） |
| 处理 | **不弹三选一**，改为发放铜钱补偿 + 提示 |
| 金额 | `补偿 = 30 + 5 × 当前等级` 铜钱（Lv10 → 80，Lv30 → 180）。对齐一次升级 ≈ 200 XP ≈ 6 只 101 的经验量 |
| 发放去向 | `PlayerState.AddCoins(n)` → 局内 session → `StageController.SettleCoins()` → 存档 → **天赋树可花** |
| 部分可用 | 候选只剩 1–2 条时**仍正常弹窗**（`UpgradeChoicePopup.ShowChoices` 已支持隐藏多余卡片） |

### 6.3 为什么钱会真的流回天赋树

```
敌人击杀 → PlayerState.AddCoins(10) → 局内 session coinCount
        → StageController.SettleCoins() → SaveManager.SetCoins(存档)
        → 天赋树 TalentTreeState.TryUnlock 扣 coinCost
```

**局内铜钱与局外铜钱是同一个池子**（结算时合并）→ 「满槽变钱」把局内成长折算成局外解锁力，闭环天然成立。

### 6.4 提示 UI（复用，不新建）

| 现有件 | 能力 | 复用方式 |
|---|---|---|
| **`KillRewardUI.rewardPopup`** | 标题文本 + 金额文本 + 淡入淡出 + 缩放动效（`popupDuration` / `popupFadeDuration` / `popupScaleEase`） | **形态完全现成**，套用即可 |
| `CoinCounterUI.OnCoinGained` | 订阅 `PlayerState.OnCoinGained`（金额 + 累计）做数字跳动 | 自动响应，无需额外接线 |
| `SupplyCoinBurst` / `SupplyCoinIcon` | 金币爆散 / 图标 | 可选加强 |

> 项目**没有通用 Toast 组件**（只有 `ItemDiscardPromptAdapter`）。→ **不要新建 Toast 系统**，复用 `KillRewardUI` 的 popup 形态。

### 6.5 实现现状（A3）与两个新发现

| 项 | 现状 |
|---|---|
| 触发点 | `UpgradeChoiceManager.ShowNextChoice` 的「候选为空」分支；**只对升级三选一生效**（Boss 锦囊的 Item 池空仍按原样跳过） |
| 金额 | `_fullSlotCoinBase`（默认 30）+ `_fullSlotCoinPerLevel`（默认 5）× `PlayerState.currentLevel`，两者都是 Inspector 可调 |
| 发放 | `PlayerState.AddCoins(n)` → 局内 `coinCount` → `StageController.SettleCoins()` → `SaveManager.SetCoins` → 存档 `props` → **天赋树可花**（实测 320 → 355） |
| 提示 | 走**新端口** `CombatPresenters.InRunReward`（`IInRunRewardPopup`）；`KillRewardUI` 实现该端口并复用它现有的 `rewardPopup` 形态。**界面上没有这个实现时安全降级**——`CoinCounterUI` 仍会飘「+N」 |

#### ⚠️ 发现 1：`KillRewardUI` **不在 Battle 场景里**（组件存在但没部署）

- `Assets/Scripts/UI/KillRewardUI.cs` 有完整的奖励弹窗（标题 + 金额 + 淡入淡出 + 缩放动效），但**Battle 场景里只有 `KillRewardManager`**（逻辑），`KillRewardUI`（视图）**没挂** → 击杀里程碑弹窗当前也是不显示的。
- 结论：**本次不新建 UI**（纪律：不新建 Toast 系统 / 不动 UI 框架）。做法是——
  - 核心只认端口 `CombatPresenters.InRunReward`（有实现就弹标题弹窗，没有就降级）；
  - `KillRewardUI` 实现该端口并自我登记 → **UI 侧一旦把它挂进场景，核心零改动就能生效**；
  - 今天的实际反馈 = `CoinCounterUI` 的「+N」飘字（实测飘字在屏内、alpha=1，玩家看得见）。
- 待办（归 UI 侧）：把 `KillRewardUI` 挂进 Battle 场景并配好 `rewardPopup` / `rewardTitleText` / `rewardAmountText`。

#### ⚠️ 发现 2：铜钱预算会超过 T-05 的「单关 600–800」

- A1 把被动槽从 6 降到 3 之后，**满槽来得早得多**；而池子并不是一开始就空——
  已持有的条目**还能继续升级**（每个 5 级），所以池子要等「已持有的全部升满 + 新条目全被槽位排除」才空。
- 按当前池子（12 条 × 约 5 级）粗算：约 30 次升级用光池子，之后的每次升级才发铜钱补偿。
  若一局约 40 次升级，补偿约 10 次 × 80~180 ≈ **800~1800 铜钱**，叠加 T-05 的击杀 + 通关铜钱会**超出 600–800**。
- → **金额是设计决策（§0.6.4）**，代码已把基数与每级增量做成 Inspector 字段，可用跑关机器人 `battle_end.coins` 观测后再调。
  本次按文档建议值（30 + 5×等级）落地，**未自行改价**。

---

## 7. 技能等级上限解锁（R7）

> ✅ **A4 已实现（2026-11-03）**：有效上限查询已落地，**现有两处上限判定都改读它**。
> 实测：**现有 40 个升级资产的有效上限 == 资产 maxLevel → 行为零变化**；
> 数据上限与天赋上限两个分支都已用合成定义实测通过。

### 7.1 上限判定的两处

| 位置 | 代码 |
|---|---|
| `UpgradePoolSampler.CollectUnified` | `if (currentLevel >= wu.upgrade.maxLevel) continue;` |
| `UpgradeEffectManager.ApplyUpgrade` | `if (newNumericLevel > def.maxLevel) { …跳过… }` |

→ 需要**运行时上限覆盖**。

### 7.2 三个上限相乘

```
资产上限   def.maxLevel                  // 例：5
天赋解锁   unlockedCap（能力页天赋 flag）  // 例：10
数据上限   availableRows(def)            // 该技能实际有几行等级数值

实际可升上限 = min( max(资产上限, 天赋解锁), 数据上限 )
```

**「解锁上限」与「实际有无这么高的等级」是两码事**（2026-11 定调）——不为凑满 10 级先做数值，先做通用机制。

### 7.3 为什么必须夹「数据上限」

| 读取入口 | 越界行为 | 后果 |
|---|---|---|
| `UpgradeDefinition.GetNumericConfig(level)` | `numericLevels.Count >= level` 不成立 → 回退 `floatValue * level` 线性外推 | 不报错但**数值是假的** |
| `ActiveSkillDefinition.GetCooldown(level)` | `Mathf.Clamp` 到最后一行 | 静默封顶 |
| `UpgradeDefinition.GetTriggerInterval(level)` | 越界返回 **-1** | `TimedPassiveModule`：`if (interval <= 0f)` → **警告 + 该被动不注册 = 直接失效** ⚠️ |
| `UpgradeDefinition.GetTriggerThreshold(level)` | 越界返回 **-1** | `PassiveTriggerModule`：`if (threshold <= 0)` → 兜底用 `intValue` 或 4 |

→ **夹了「数据上限」之后永远不会走到越界分支**，这四个读取函数**一行都不用改**。

### 7.4 池内技能的等级数据现状

| 技能 | 当前 maxLevel | 数据行数 | 6–10 级数据 |
|---|---|---|---|
| 疾风 / 铁壁 / 震荡 / 专注 / 蛇形喷射 / 地刺 / 连刺 / 敛财 / 击退波 / 虚幻武器 / 聚拢波 | 5 | 5 | ❌ 无 |
| 拔苗助长 / 延长 / 波长 | 2 | 2 | — |
| 海浪（主动） | 3 | 3 | — |
| 箭矢齐射 / 反伤 / 神力 / 谋略 / 捡漏 / 箭雨（被动） | 10 | 10 | ✅ **有，但不在池里** |

git 历史里 **铁壁 / 地刺** 曾有完整 10 级数值（commit `0655bf3` / `399a15b`），需要时可取回；**专注 / 智慧** 历史里从没有 10 级版本。

### 7.5 实现现状（A4）

| 项 | 落点 |
|---|---|
| 有效上限查询 | `UpgradeEffectManager.GetEffectiveMaxLevel(def)` = `min(max(资产上限, 天赋上限), 数据上限)` |
| 数据上限 | `UpgradeDefinition.GetAvailableLevelCount()`（按 effectType 取对应等级表；`ActiveSkillDefinition` 覆写为冷却表行数）。**0 = 没有等级数据 → 不夹上限**（走旧版字段 / 固定值兜底的定义不会被误伤） |
| 天赋上限 | **扩展点** `UpgradeEffectManager.RegisterLevelCapProvider(Func<UpgradeDefinition,int>)`。当前项目**还没有「解锁等级上限」的天赋节点** → 没有注册方 → 天赋项恒为 0 |
| 改读它的判定点 | `UpgradePoolSampler.CollectRegular` / `CollectUnified` / `CollectItem`（注入 `Func<UpgradeDefinition,int>`，不注入则退回资产 maxLevel）、`UpgradeEffectManager.ApplyUpgrade`（被动型与数值型两处）、`ActiveSkillInventory.CanAcquire`、`UpgradeCard` 的等级状态文案 |

> 夹住数据上限后，`GetTriggerInterval` / `GetTriggerThreshold` **永远不会越界**
> （越界返回 -1 → 触发模块判定「不适用」→ 该被动不注册 = 直接失效）→ **这四个读取函数一行都不用改**。
>
> ⚠️ 与 B 批的关系：天赋上限是扩展点，**真正的「解锁到 10 级」需要 B 批把节点与载荷补齐**才能接上；
> 机制已就位，数据一到即生效。


---

## 8. 秘籍系统（R6）

> 需求：「重击解冻这类**技能提升**加入道具解锁——单次掉落获得后自动解锁，类似秘籍。
> 可能在敌人身上随机或固定掉落，也可能在场景破坏物中随机或固定掉落。」

### 8.1 现有可复用的通道（实测）

| 通道 | 位置 | 现状 |
|---|---|---|
| **敌人击杀掉道具** | `EnemyManager.TryDropItem()` + `DropItemPoolConfig`（`baseDropChance` + 加权 `pool`） | ✅ 已存在。⚠️ 开头有 **V2 短路**：`if (ActiveSkillInventory.Instance.UsesActiveSkills) return;` → **V2 下敌人不掉任何道具**。掉落率受「捡漏 `item_drop_rate`」加成 |
| **敌人掉铜钱** | `Enemy.coinReward` → `StageRunFlow` → `PlayerState.AddCoins` | ✅ 已在跑 |
| **击杀里程碑** | `KillRewardManager` + `StageConfig.killMilestones` + `KillRewardType`（Coin / Heal / RandomUpgrade） | ✅ 已在跑；**没有「解锁」类型** |
| **场景破坏物** | `BreakableProp`：`SupplyLayer.InRun / OutOfRun` 分层；局外层奖励直接进存档金币（`outOfRunCoinReward`） | ✅ 已在跑 |
| **可见掉落物范式** | `ExpGemManager.SpawnGem()` + `ExpGem`（生成 → 飞向玩家 → `OnGemArrived` 结算）；`HealthPotionManager` 复用 `gemPrefab` 生成药水 | ✅ 已在跑 |
| **拾取通道** | `ISupplyPropTarget` + `MoveGesture.supplyTarget`（点中物件时同一击同时命中） | ✅ 已在跑 |
| **存档片范式** | `TalentSaveSection`（nodeId 列表） | ✅ 已在跑 |

### 8.2 新增 / 修改清单

| # | 内容 | 成本 |
|---|---|---|
| 1 | `ManualDefinition : UpgradeDefinition`（含 id / 名称 / 说明 / 解锁的 flag 或 moveConfig / 图标） | 小 |
| 2 | `ManualSaveSection`（已获得秘籍 id 列表），与 `TalentSaveSection` 同构 | 小 |
| 3 | `EnemyManager.TryDropItem` 分派：`ManualDefinition` → 写存档解锁；普通 `category=Item` → 进 `ItemInventory`。**同时处理 V2 短路**（V2 下只掉秘籍、不掉普通道具） | 中 |
| 4 | `BreakableProp` 加秘籍字段 + 概率，复用现有分层 / 拾取 / 渐变销毁 | 小 |
| 5 | 可见的秘籍掉落物（**照抄 `ExpGemManager` 范式**：生成 → 飞向玩家 → 到达结算） | 中 |
| 6 | 天赋页显示「秘籍已获得」态 | 见 `design/talent-page-ui.md` §3.2 |

### 8.3 ⚠️ 秘籍不走 `ItemInventory`

`ItemInventory` 是**局内道具栏**：有槽位上限、满了弹弃置窗、局末清空。秘籍是**永久解锁**，必须直接写存档。

### 8.4 边界规则

| 获取方式 | 只给什么 |
|---|---|
| **秘籍掉落** | **技巧类**：单次获得即永久生效、无等级、无铜钱成本（如重击解冻） |
| **铜钱购买** | **流派 / 能力类**：需要等级、需要前置、需要路线承诺 |

两者**不重叠**。

### 8.5 破冰顶开迁移

现状：`IceBreakSession.ApplyBreakPush` 由 `TalentTreeState.IsUnlocked("talent_ice_break_push")` 门控。

迁移后：

| 步骤 | 内容 |
|---|---|
| 1 | 删除 `talent_ice_break_push` 天赋节点 |
| 2 | `IceBreakSession` 的判定改为「秘籍已获得」 |
| 3 | 破冰顶开效果由**冰雾的技能升级**承载（需要一个承载该效果的升级条目） |

⚠️ **冰雾当前不在池里、也不在游戏里**（`_grantDefaultSkills = 0`）→ 冰雾的归属是**本周待定项**。

---

## 9. 验收口径

| # | 检查 | 方式 |
|---|---|---|
| 1 | 未解锁流派的技能**不出现在**三选一里 | 实机 |
| 2 | 买了流派根节点后，技能**逐个**开始进池 | 实机 |
| 3 | 通用技能（疾风 / 智慧 / 地刺 / 箭雨）**始终**在池里 | 实机 |
| 4 | 主动槽 = 3、被动槽 = 3 | 存档 + 实机 |
| 5 | 买了「巧思」后主动槽变 4、「博学」后被动槽变 4 | 实机 |
| 6 | 槽位满时**弹金币补偿**，不再静默跳过 | 实机（清空池后升级） |
| 7 | 补偿金额随等级增长 | 实机 |
| 8 | 补偿的铜钱在结算后进入存档，且**能在天赋树花** | 实机 |
| 9 | 池空时提示可见（复用 `KillRewardUI` 形态） | 实机 |
| 10 | 被动排 UI 显示的条目与实际占用一致 | 实机（见 §5.4 待定项） |
| 11 | 买了「精通」天赋后，10 级技能能升到 10；只有 5 级数据的技能停在 5 | 实机 |
| 12 | 秘籍从敌人 / 破坏物掉落并永久解锁，重进游戏仍是已解锁 | 实机 |
| 13 | 秘籍解锁后天赋页显示「已获得」，不可再买 | 实机 |
| 14 | 存档往返：秘籍与天赋都保持 | 实机 |

---

## 10. 参考

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Core/UpgradeChoiceManager.cs` | 三选一流程（满槽补偿插入点） |
| `Assets/Scripts/Core/UpgradePoolSampler.cs` | 候选收集 + 槽位上限 |
| `Assets/Scripts/Core/UpgradeEffectManager.cs` / `.Apply.cs` | 升级应用 + 注册表 |
| `Assets/Scripts/Core/ActiveSkillInventory.cs` | 主动技能库存（槽位） |
| `Assets/Scripts/Core/ItemInventory.cs` | 局内道具库存 |
| `Assets/Scripts/Core/ComboManager.cs` + `Data/ComboBuffConfig.cs` | 连击 buff（多档位触发） |
| `Assets/Scripts/Core/BuffManager.cs` + `Data/StatModifier.cs` | 属性修正 |
| `Assets/Scripts/Enemy/EnemyManager.cs:208` | 敌人掉落（V2 短路） |
| `Assets/Scripts/Data/DropItemPoolConfig.cs` | 掉落池 |
| `Assets/Scripts/Prop/BreakableProp.cs` | 场景破坏物 |
| `Assets/Scripts/Core/ExpGemManager.cs` | 可见掉落物范式 |
| `Assets/Scripts/Data/Save/TalentSaveSection.cs` | 存档片范式 |
| `design/talent-tree-architecture.md` | 解锁产出端 |
| `design/out-of-game-growth-v2-analysis.md` | 现状盘点与实测证据 |
