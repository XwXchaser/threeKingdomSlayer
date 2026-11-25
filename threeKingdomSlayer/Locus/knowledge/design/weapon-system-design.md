---
id: kd_55406462-e0a3-46e7-8ab6-5584eab05128
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 武器系统与局内外经济（主角化 + 武器=战斗身份）

> **2026-11-21 局内外循环口径更新**：主武器、全局成长、局内影之武装 / 饰品及永久解锁的最新讨论见 `design/gameplay-progression-loop.md`。本文件仍保留此前的设计与工程记录；若与新文档冲突，以新文档的用户确认口径为准，未确认部分不视为新结论。
>
> **本文是长线主设计文档。** 讨论于 2026-11-16，结论已与用户对齐；标记 **【暂定】** 的条目表示用户外出期间按推荐先定、可随时推翻。
> 配套：`design/pbz-reference-analysis-20261109.md`（对标数据）、`design/endless-mode-design.md`（无限模式）、
> 执行清单 `plan/weapon-endless-mode-plan-20261116.md`。

---

## 0. 一句话

**主角是唯一的、固定的；武器承担全部战斗身份（连段 / 重攻击 / 绝技 / 处决 / 破势值 / 动作性能）。**
局外决定"你能拿到什么、能升到几级"，局内决定"这一局拿到什么、换成什么"。

---

## 1. 概念重排

### 1.1 旧结构 vs 新结构

```
旧：HeroConfig（武将）= 招式表 + 槽位 + 属性 + 大招 + 天赋树 → 天然可扩展成多武将
新：Protagonist（唯一主角）= 资源 + 基础动作 + 槽位 + 被动树 + 永久数值
    WeaponDefinition（多把） = 连段 + 重攻击 + 绝技 + 处决 + 破势值 + 动作性能 + 专属机制
```

### 1.2 主角身份不影响数据层

用户定（2026-11-16）：**另立一个通用主角、还是保留张飞作唯一主角，在设计与数据层上没有区别**，
只是美术是否要换的问题。→ 本设计按"单一主角"写，**不预设主角身份**，`HeroConfig` 可继续作为主角档案的承载类。

### 1.3 不做的事（用户已否）

| 项 | 结论 |
|---|---|
| 主角层的攻防一体战斗资源（PBZ 的"煞气"） | ❌ 不做。与当前系统无关联，暂不新增 |
| 局内自由切换武器 | ⏸ 第一版不做，待定（见 §7.3） |
| 连招进局内三选一 | 🚫 永不做（见 §3.4） |

---

## 2. 武器资产模型

### 2.1 PBZ 对照（实测）

| PBZ 字段 | 我们的对应 | 载体 |
|---|---|---|
| 攻击力 | 伤害系数 | 新字段，叠乘 `AttackSkillConfig.damage` |
| 破盾值 | 我们的"破盾"= 1021 的盾 + 敌人霸体 → 需要**武器级破势值** | 现有 `poiseDamage` / `interruptsCavalryCharge` 是招式级，武器级需聚合量 |
| 攻击连段 | 招式表（手势 → 入口 + 窗口） | `MoveTableConfig` 从 `HeroConfig.moveTable` 迁到武器 |
| 杀气攻击 | 蓄力重攻击（Pierce / Sweep / Launch） | 现有体系 |
| 绝技 | **每把武器一个绝技**（现在只有一个"狂怒"） | `UltimateSkillConfig` 体系（用户定：现有 ult 跟着武器走） |
| 处决动作 | 新增：低血敌人处决 | 新系统 |
| 动作性能 | 闪避 / 格挡 / 跳跃的性能差异 | 现有硬直 / 窗口 / 射程参数 |

### 2.2 持法档位 → 我们的手势系统（关键翻译层）

我们的输入只有 **点 / 长按 / 横划 / 竖划 / 斜划 + 蓄力 0–3 级**，没有"单手/双手"这种输入设备。
PBZ 的三大类（单手刀刃 / 双手刀刃 / 双持刀刃）必须翻译成**同一手势映射到不同招式 + 不同窗口/硬直/射程**：

| 档位 | 输入表达 | 体验 |
|---|---|---|
| 单手 | 点线链长、窗口短、单发伤害低、硬直短 | 快、连段长 |
| 双手 | 蓄力重击为主、单发伤害高、硬直长、破势强；蓄力分级收益（`chargeLevelDamageMultipliers` 至今是空表，正好用上） | 重、慢、清场 |
| 双持 | 点/划各成招、分支多、覆盖多列但单发弱 | 广、灵活 |

→ **不用新输入设备，但"换武器 = 换手感"成立**（对应 PBZ 制作人"武器差异要在操作和画面上都能读出来"）。

### 2.3 首批 3 把（建议）

| 武器 | 轴 | 招式位 | 机制开关 | 已有资产 |
|---|---|---|---|---|
| 长枪（默认） | 中距离、连段 | 指向突刺 + 枪突链 + 挑飞 | 枪突收尾（重攻击削韧 80） | 全套已有 |
| 巨斧 | 慢、破防、群 | 抡斧 + 破阵横扫 | 正面重攻击**必定破盾** | `ActiveSkill_AxeSmash`（forceBreak 已实现） |
| 锁链 | 远程 / 拉扯 | 追命锁链 + 勾拽 | 锁最远最弱敌人到本列最前 | `ActiveSkill_ChainPull` |

---

## 3. 局内 / 局外分工

### 3.1 总表

| 内容 | 局外（天赋页 + 武器台） | 局内（三选一） |
|---|---|---|
| **属性升级卡**（纯数值，可重复叠加） | 解锁"哪些卡能进池" | 抽到就叠 |
| **影之武装**（≈主动） | 解锁进池 + **解锁等级上限** + 高档才带的机制 | 抽到新的一件 / 更高品质 → 替换或升级 |
| **饰品**（≈被动） | 同上 | 同上 |
| **连招（招式）** | 随武器升级解锁 | 🚫 不进局内 |
| **武器资源（暗源）** | — | 靠**进攻**获取，用于释放影之武装 |
| **槽位 / 永久数值天赋** | ✅ | — |

> **当前长枪招式树（2026-11-25）**：`move_spear_slash`（横扫，根）→ `move_spear_slashc2`（斩击 C2）；`move_spear_stabc2`（指向突刺，根）→ `move_spear_launch`（枪突收尾）。**基础招式 = 戳 / 上挑 / 招架**。条款见 `MainWeapon_ZhangfeiSpear.asset`。

### 3.2 品质（3 档）

用户定（2026-11-16）：**3 档**（普通 / 精良 / 传说）。
- 档内靠**等级**涨数值（品质框 + 数值）；
- **部分内容只有单一品质**（例：提供一次复活、触发后移除的饰品 = 只有精良品质，没有普通/传说）。

数据承载已落地：`UpgradeDefinition.quality`（`UpgradeQuality` 枚举）+ `defaultLevelCap`（见 §4.2）。
`UpgradeQuality` 与 `UpgradeRarity` 是**两个独立维度**。

### 3.3 槽位

用户定：主动 / 被动槽位**初始各 2**，更多槽位由**局外天赋**解锁。

| 项 | 值 | 状态 |
|---|---|---|
| `HeroConfig.activeSkillSlotCount` | 3 → **2** | ✅ 已改（代码默认值 + `Hero_Zhangfei.asset`） |
| `HeroConfig.passiveSlotCount` | 3 → **2** | ✅ 已改（同上） |
| `HeroConfig.itemSlotCount` | 资产值 4（代码默认 2） | 未动（用户未提） |
| 巧思 `talent_stat_slot_active`（`active_slot` +1，180 铜钱） | 局外解锁 +1 主动槽 | 已有，未改 |
| 博学 `talent_stat_slot_passive`（`passive_slot` +1，180 铜钱） | 局外解锁 +1 被动槽 | 已有，未改 |

> ⚠ **未买天赋的玩家现在少 1 个主动槽 + 1 个被动槽**（2/2 而不是 3/3）。这是用户明确要的"初始 2"。
> 局外天赋「巧思 / 博学」买齐后回到 3/3。**需要实机确认三选一卡池在 2 槽下的体验**（可选项变少、升级更早占满）。

### 3.4 红线：连招不进局内

我们的操作是手势。一局之内换连段 = 换操作 = 玩家要重新学，且与已有的"教学拍"机制冲突。
PBZ 也是这么切的：连段属于武器（局外），局内只有武装 / 饰品。

---

## 4. 局外成长：解锁等级上限（软锁）

### 4.1 设计意图（用户定）

用户否掉了"每圈重置 build"的方案：**这一局的积累是 roguelite 最值钱的东西，不能扔。**
取而代之的是**软锁**——局内成长有上限，而这个上限**只能靠局外成长解锁**：

```
局内：三选一 → 属性卡无限叠（软锁内） / 装备卡品质替换
局外：解锁「某条升级的等级上限」→ 软锁抬高
```

这样长期进度被拆成两条轴：
- **局内**：这一局能堆到多高（受软锁限制）；
- **局外**：软锁能抬到多高 + 解锁哪些条目 + 新武器 → **唯一会"满"的东西 = 付费点与留存点**。

### 4.2 已实施（Batch 1，2026-11-16）

| 载体 | 内容 |
|---|---|
| `UpgradeDefinition.defaultLevelCap` | **基础上限（软锁）**：0 = 用 `maxLevel`。资产硬上限仍是 `maxLevel`，天赋不能突破 |
| `UpgradeDefinition.maxLevel` | 资产硬上限（等级数据只有这么多行） |
| `TalentNodeConfig.targetId` | 天赋节点的**作用目标 id**（statId = `upgrade_level_cap` 时必填 = 目标 upgradeId） |
| `TalentStatService.StatUpgradeLevelCap` | statId 常量 |
| `TalentStatService.GetUpgradeLevelCap(upgradeId)` | 读已解锁节点 → 返回**绝对上限**（多节点取最大）；0 = 没有局外放宽 |
| `UpgradeEffectManager.RegisterLevelCapProvider` | 挂点（此前零注册方）→ 已在 `Awake` 里接到 `TalentStatService` |

**有效上限口径**（`UpgradeEffectManager.GetEffectiveMaxLevel`）：

```
assetCap  = max(1, def.maxLevel)                       // 硬上限
baseCap   = def.defaultLevelCap > 0 ? defaultLevelCap : assetCap   // 软锁
talentCap = max(局外天赋节点给的绝对上限)              // 唯一的抬升途径
cap       = clamp(max(baseCap, talentCap), 1, assetCap)
if (等级数据行数 > 0 && 行数 < cap) cap = 行数          // 夹数据上限
```

该函数已接进**抽样过滤**（`UpgradePoolSampler`）、**应用升级**（`UpgradeEffectManager.ApplyUpgrade`）、
**主动技能库存**（`ActiveSkillInventory`）、**卡面显示**（`UpgradeCard`）——**软锁一处生效，四处同步**。

**实测**（2026-11-16，Editor 内）：

| 用例 | 结果 |
|---|---|
| `maxLevel=10, defaultLevelCap=3` | 有效上限 = **3** ✅ |
| `maxLevel=10, defaultLevelCap=0` | 有效上限 = 10（= 改造前行为）✅ |
| `maxLevel=10, defaultLevelCap=99` | 有效上限 = **10**（天赋/配置不能突破硬上限）✅ |
| 无已解锁天赋节点 | `GetUpgradeLevelCap` = 0 ✅ |

### 4.3 未做：天赋节点的**内容**

用户原话："我会设计'升级上限'这样的软锁"——**内容（哪些条目配哪些上限节点、多少钱）由用户定**。
机制已通，等内容填入即可生效（节点：statId = `upgrade_level_cap`、statAdd = 绝对上限、targetId = 目标 upgradeId）。

---

## 5. 分层乘算公式

### 5.1 改造前的问题（实测）

```csharp
// 改造前
public float GetDamageMultiplier() => Mathf.Max(0f, _damageMultiplier + _buffDamageBonus);
```

- **全项目只有一个伤害乘区标量**：数值升级 `_damageMultiplier += v`、buff `_buffDamageBonus += v` → **加法百分比与乘法百分比被压平成同一个加法数**；
- `StatModifierType.Multiply` 在生效路径里**被忽略**（`ApplyBuffModifier` 只按 statId 累加）；
- `HeroConfig.damageBonusPercent` **只作用于大招**（`UltimateEffect_Berserk`），普通攻击路径不读它；
- 射程衰减惩罚走 `GetAttackDamagePenalty()`，**不在伤害公式里** → 当时已有两个未统一的乘区。

⇒ 无法表达：同类相加/异类相乘、固定值加成、属性转化器。

### 5.2 分层模型（已实施）

`Assets/Scripts/Core/StatLayerModel.cs`（新）：

```
1) 基础值 + 固定值层（flat，如 +20 攻击）
2) × (1 + 加法层百分比)        ← 同 statId 的百分比先相加
3) + 转化器产出的固定值         ← 输入只取「到第 2 步为止」的值
4) × Π(1 + 乘法层乘区)         ← 异类乘区各自独立相乘
```

- **同类相加、异类相乘** —— roguelite 防指数膨胀的标准做法（10 张 +10% = ×2，不是 ×2.59）；
- 固定值层 = "+X 攻击"这类 flat；加法层 = "+X% 伤害"；乘法层 = "×X"；条件乘区 = 只在特定敌人状态下生效（后续）。

### 5.3 转化器与防环

```csharp
// 霸王血铠：每 100 点生命 → +20 攻击力
new StatConverter { sourceStatId = "max_health", perSourceValue = 100f, targetStatId = "atk", gain = 20f }
```

**防环规则（写死，不允许改）**：转化器的输入只取「基础值 + 固定值层 + 加法层」，**不含任何转化器产出**
⇒ 转化器之间不可能互相喂养，结构上不会成环。

代价：一条「吸血 → 生命 → 攻击力」的链只读到第一跳（吸血给的生命进不了攻击力）——刻意选择的安全口径。

合法源属性只有**汇点属性**（只被加法影响、不参与转化）：当前只有 `max_health`
（`UpgradeEffectManager.ResolveStatSource` → `PlayerState.MaxHealth`）。

### 5.4 统一伤害出口（已实施）

```csharp
public float ComputeDamage(float baseDamage)   // UpgradeEffectManager
```

已把 5 处伤害出口改成走它（改造前都是 `base * GetDamageMultiplier()`）：

| 文件 | 用途 |
|---|---|
| `AttackSystem.Utils.cs` `GetFinalDamage` | 玩家普通攻击（主路径） |
| `AttackSystem.Unlock.cs` | 解锁攻击 |
| `InputManager.cs` | 落雷 |
| `PassiveTriggerModule.cs` | 被动触发（箭雨等） |
| `WhirlwindController.cs` | 旋风 |

**未改**：大招路径（`UltimateEffect_Berserk` 自带 `damageBonusPercent` 链）——保持原样，避免动大招手感。
**迁移期等价性**：没有 flat / 乘区 / 转化器时，`ComputeDamage(x) = x × (1 + Σ加法)` = 改造前的 `x × (_damageMultiplier + _buffDamageBonus)`。

**实测**（Editor 内）：

| 操作 | 倍率 | `ComputeDamage(20)` |
|---|---|---|
| 初始 | 1.0 | 20 |
| `AddDamageBonus(0.5)`（+50%） | 1.5 | 30 |
| `AddFlatDamageBonus(20)` | 1.5 | 60（=(20+20)×1.5） |
| `AddMultiplicativeDamageBonus(0.2)` | 1.8 | 72（=×1.2） |
| `ResetAll()` 后 | 1.0 | 20 ✅ 复原 |

### 5.5 尚未统一的部分（后续批次）

- **条件乘区**（暴击 / 破防态 / 处决）尚未入模型；
- 大招伤害未走 `ComputeDamage`；
- 伤害数字显示未处理大数（无限叠加后会溢出显示）→ 需千分位 / 科学计数。

---

## 6. 影之武装资源（"暗源"）

### 6.1 范围（务必与 §1.3 的"气"区分）

这个资源**只服务影之武装（主动技能）**：不参与蓄力重击、不做破防惩罚。

### 6.2 规则

| 规则 | 口径 | 为什么 |
|---|---|---|
| 获取 | **命中 / 击杀 / 破防**才给 | 防御姿态**不回** → 防止"按住防御纯用技能" |
| 消耗 | 释放影之武装扣 | 资源是"进攻的副产品"，不是"时间的副产品" |
| 见底 | 不能释放 | 明确的资源惩罚 |
| 回复 | 脱战 / 一段时间不消耗后缓慢回（有上限） | 否则"存着不打再一次性放"变成最优解 |
| 数值铁律 | **每点耗能的期望伤害必须低于同等时间的普攻** | 否则最优解仍是"用技能代替打怪" |

标定方法：`耗能 ÷ 期望伤害` 与普攻的每点体力产出对齐；上线前用跑关机器人测"最优解是不是纯放技能"。

### 6.3 状态

- **UI 显示：用户明确要求先做**（"首先需要新的配套UI显示，实机标定要慢慢测"）→ 见执行计划 Batch 3；
- 数值标定：慢慢测，不阻塞其它批次。

---

## 7. 待实施：卡片实例数据模型

### 7.1 问题（实测）

`_appliedUpgrades` 是 `Dictionary<upgradeId, int>` ⇒ **同一个 id 只能有一个等级、总量有上限**
（`maxLevel` 默认 10；抽样时"已达上限即排除"）。

⇒ 现状**不支持**：
- 抽到第二张同类卡并独立持有（"两个 +2 饰品"）；
- 用 +3 替换其中一个 +2（**槽位里放的是实例，不是 id**）；
- 同类卡无限叠加（属性卡）。

### 7.2 目标模型

```
持有物 = List<CardInstance>
CardInstance { upgradeId, instanceId, quality, level, source(局内/局外) }
槽位（主动 2 / 被动 2）= 指向某个 CardInstance
```

- 抽卡 = 生成一个实例；同类可并存；
- 替换 = 把槽位指向另一个实例（旧的回收到背包或直接消失，规则待定）；
- 升级 = 提升**实例**的等级（受 §4 的软锁约束）；
- 读 `GetUpgradeLevel(id)` 的地方（被动触发模块 / 主动技能等级 / 描述格式器）改成"按实例汇总"。

### 7.3 局内切换（⏸ 待定）

PBZ 的切换 = 回满杀气 + 自动专属攻击 + 武器冷却 + 脱离破防。
**第一版不做**。理由：自由切换会稀释"武器=一套连段+一棵升级树"的成长感——玩家永远能切到最合适的那把。

若将来做，我们的对应可以是：切换 = 重置动作锁 + 回满大招能量 + 自动打出一记该武器的**切换技**（每把武器不同 → 每把武器 +1 段美术）。

---

## 8. 局外经济与变现映射

### 8.1 参考游戏的经济骨架（PBZ 实测）

| 层 | PBZ | 我们的对应 |
|---|---|---|
| 通用底座 | 意识尘埃（武器升级 / 能力修行 / 秘药都吃它） | 铜钱 |
| 专用材料 | 金属 + 溶剂（只给武器升级）；黑石 / 杀气琥珀 / 记忆之花（各给一条能力线）；榕树之心（稀有） | 武器材料 + 招式残卷 |
| 纠错层 | 兵刃逆炼符 / 影之消退符（洗点，按等级消耗） | 洗点券（**我们目前没有，是缺口**） |
| 不可逆层 | 铸魂（变体，专属材料 + 5000 尘埃，**不可回退**） | 武器铸魂 / 限定变体 |

**五条可抄的机制**：多材料线分流 / 升级解锁**新操作**而非数字 / 洗点便宜 / 不可逆终局层 / 敌人等级**不跟随玩家**。

### 8.2 变现点映射

| 变现方式 | 放什么 | 靠哪个系统 |
|---|---|---|
| 激励视频复活 | 无限关死亡后续命 | 无限关 + 分数 |
| 激励视频双倍 | 局内分数 / 材料翻倍 | 分数系统 + 武器材料 |
| 内购 · 月卡 | 每日铜钱 / 材料 | 铜钱经济 + 武器材料 |
| 内购 · 角色 | 稀有武器、外观 | 武器台（永久收藏） |
| 内购 · 便利 | 额外主动 / 被动槽、洗点券 | 天赋页槽位 + 逆炼符式洗点 |
| 内购 · 赛季 | 每周任务 → 限定武器 / 材料 | 无限关圈数任务 |

**商业判断**：付费点应落在**武器**（收集品、每把独立升级曲线）而不是数值；
免费路径 = 打 Boss 掉武器；付费路径 = 稀有 / 限定变体（对应"铸魂"层）。

> 行业背景（2026 口径）：微信小游戏分 IAA / IAP 两类，主流是**混合变现**；
> IAP 首发新游分成可向 2:8 靠拢、首发激励金上限 400 万–5000 万流水不分成（**限首发新游**）。
> ⚠ 本项目当前 APK 340MB → 微信小游戏是**硬门槛**（包体 + 团结引擎导出），建议先按安卓渠道跑通玩法闭环。

---

## 9. 接缝清单（文件级）

| 主题 | 文件 | 状态 |
|---|---|---|
| 分层乘算模型 | `Assets/Scripts/Core/StatLayerModel.cs` | ✅ 新建 |
| 伤害统一出口 | `UpgradeEffectManager.ComputeDamage` + 5 处调用点 | ✅ 已改 |
| 等级上限软锁 | `UpgradeDefinition.defaultLevelCap` / `TalentNodeConfig.targetId` / `TalentStatService.GetUpgradeLevelCap` / `RegisterLevelCapProvider` | ✅ 已改 |
| 品质档 | `UpgradeDefinition.quality`（`UpgradeQuality`） | ✅ 字段已加（**无内容**） |
| 槽位基线 2/2 | `HeroConfig`（代码 + `Hero_Zhangfei.asset`）/ `ActiveSkillInventory.DefaultCapacity` / `UpgradeChoiceManager.DefaultPassiveSlotCount` | ✅ 已改 |
| 卡片实例模型 | `UpgradeEffectManager._appliedUpgrades` / `ActiveSkillInventory._entries` / 被动模块 / 描述格式器 | ⏸ Batch 2 |
| 影之武装替换 UI | `ItemDiscardPopup`（现成可复用） | ⏸ Batch 3 |
| 暗源资源 + UI | 新增 | ⏸ Batch 3 |
| 武器资产 | 新增 `WeaponDefinition` / 武器台页 | ⏸ Batch 4 |
| 洗点券 | 新增 | ⏸ Batch 5 |

---

## 10. 待拍板 / 风险

| # | 项 | 现状 |
|---|---|---|
| 1 | 局外解锁上限的**具体内容**（哪些条目、多少钱） | 用户定 |
| 2 | 品质 3 档的**命名与框体** | 用户定（普通 / 精良 / 传说暂定） |
| 3 | 槽位最终数（2 + 天赋解锁 vs PBZ 的 6 饰品 / 2 武装） | 暂定 2+解锁 |
| 4 | 局内切换 | ⏸ 待定 |
| 5 | 暗源数值 | 慢慢标定 |
| 6 | 武器材料 / 招式残卷 / 洗点券的货币名与产出速率 | 未设计 |
| 7 | 卡片实例模型里"替换掉的旧实例"去哪（回收 / 消失 / 可换回） | 未设计 |
| 8 | 2 槽对三选一卡池的影响（可选项变少、升级更早占满） | **需实机确认** |
| 9 | 无限叠加后伤害数字显示溢出 | 待处理 |

> **风险提示**：本轮把主动/被动槽位从 3 降到 2，**未买「巧思 / 博学」的玩家会少一个槽**。
> 这是用户明确要求，但会立刻改变现有玩家的三选一体验 → 用户回来后第一件事建议实机跑一局确认。
