---
id: kd_0515d020-4f6f-4837-affa-072aeb242dad
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 天赋页面 · 功能架构

> ⚠️ **本文 §2.1 / §3.2 / §5 / §10 已被另一侧的实现取代（2026-11-02）。**
> 另一侧已独立实现并验收（提交 `a0c84ad4`）：**3 个独立树资产**（`TalentTree_Stats` / `TalentTree_Moves` / `TalentTree_Pools`）+ **`TalentCatalog`**（3 引用 + 合法 id 集合）+ **`col`/`row` 整数格点**取代手填坐标 + **`upgradeConfig`**（页 3 直接引用 `UpgradeDefinition`）+ **`statId`/`statAdd`**（页 1 通用数值载荷）+ **`TalentNodeInfo`**（图标与文案自动取自解锁内容）。
> → 本文提出的「单树 + `pageId`/`branchId` + `branches[]`」**不再采用**。
>
> **仍然有效的部分**（这些是另一侧**没有覆盖**的）：
> - §4.1 **flag 机制零消费者**的更正（`IsFlagUnlocked` 全项目零调用 → 本周统一用 nodeId 解锁态表达「已解锁」）
> - §6 **能力天赋的实现落点**（生命 / 铜钱 / 破阵 / 追击 / 运筹 / 不屈 / 槽位）—— 但挂点要改成读对方的 `statId`
> - §7 **存档与 nodeId 兼容规则**（改名必须写迁移）
> - §8 **两条局外获取路径**（铜钱买 / 秘籍掉）与边界规则
> - §9.2 **删 `talent_ice_break_push` 的连带影响**
> - §10 校验规则中「多前置环检测」「载荷必须非空」两条仍适用
>
> **两套方案的差异与待拍板冲突见 `plan/talent-scheme-alignment-20261102.md`** —— 动手前先读它。
>
> 以下内容按原样保留，便于追溯。

> 2026-11 设计文档。属于「局外成长与关卡配置」这一轮的**需求 R1 / R2 / R3 / R4 / R5**。
> 现状盘点与实测证据见 `design/out-of-game-growth-v2-analysis.md`（§2.1 天赋系统、§4A 天赋页结构）。
> 页面视觉与交互见 `design/talent-page-ui.md`。

---

## 1. 定位与边界

**天赋页是局外成长的唯一入口之一**（另一条是「秘籍」掉落，见 §8）。它负责：

| 负责 | 不负责 |
|---|---|
| 组织解锁内容（页 → 分支 → 节点） | 决定某个关卡该打什么敌人（→ `design/stage-configuration.md`） |
| 决定节点之间的解锁关系 | 决定招式本身的连段结构（→ `design/move-split-and-derivatives.md`） |
| 消费铜钱 / 记录秘籍 | 局内三选一的抽池规则与槽位上限（→ `design/in-run-growth-and-unlocks.md`） |
| 把解锁结果**广播**给消费方 | 消费方自己的实现 |

> **边界原则**：天赋树只做「**开关 + 顺序 + 价格**」三件事。所有效果实现都放在消费方（战斗 / 数值 / UI），天赋树通过 `flagId` / `moveConfig` / 节点解锁态被查询。

---

## 2. 概念模型

```
天赋页（TalentTreeConfig, treeId = "main"）
 └─ 大页面 pageId        能力 / 招式 / 技能
     └─ 分支 branchId     一条起始分支 = 一个主题（流派 / 招式线 / 能力方向）
         └─ 节点 nodeId   一个可购买项
```

| 概念 | 载体 | 说明 |
|---|---|---|
| 节点 | `TalentNodeConfig`（内联可序列化类） | 只在树资产里被引用一次，做成独立资产会散落几十个 `.asset` |
| 分支 | **新增** `TalentBranchConfig` | 分支元数据：id / 名称 / 说明 / 图标 / 顺序 |
| 大页面 | **新增** `pageId` 字符串 | 三选一：`ability` / `move` / `skill` |

### 2.1 本周确定的分支划分

```
天赋页
├─ 能力页 ability
│   ├─ 战斗向 combat   破阵 → 追击 → 运筹
│   ├─ 生存向 body     铁血(生命+30) → 连击精通(连击阈值↓) → 不屈
│   └─ 经济向 fortune  聚财(铜钱+30%) → 巧思(主动槽+1) → 博学(被动槽+1)
├─ 招式页 move
│   ├─ 点线 chain      指向突刺 C2 → 枪突收尾 C4
│   └─ 划线 sweep      横扫位移（Slash；后续加衍生）
└─ 技能页 skill
    ├─ 蓄力流 charge   蓄力入门 → 铁壁 / 震荡 / 冲击波
    ├─ 位移流 displace 位移入门 → 专注 / 海浪 / 旋风 / 染病
    └─ 火系   fire     蛇形喷射 → 火龙舌
```

**已定归属**

| 项 | 归属 | 理由 |
|---|---|---|
| 染病（触发键=戳击命中） | **位移流** | 控场 + DoT，与海浪/旋风同属「控场换安全输出」 |
| 疾风 / 智慧 / 地刺 / 箭雨 | **常驻池，不进天赋页** | 全流派通用，默认就在局内三选一池里 |
| 重击解冻（`ice_thaw`） | **从天赋树移出 → 秘籍掉落** | 「技巧类」应走机遇型获取，不占铜钱 |

### 2.2 分支根节点的判定

**同一 `branchId` 内没有同枝前置的节点 = 该分支的起始分支点。**

→ 不需要额外的 `isRoot` 字段。分支的根可以有多个（例：火系分支目前只有一条链，但将来可以并列多个起点）。

---

## 3. 数据模型

### 3.1 现有字段（`TalentNodeConfig`，10 个）

| 字段 | 类型 | 语义 |
|---|---|---|
| `nodeId` | string | 唯一 id，**写进存档**。改名会让老档丢失该节点 |
| `displayName` / `description` | string | 面板显示 |
| `coinCost` | int | 解锁花费铜钱。0 = 免费 |
| `moveConfig` | AttackSkillConfig | **载荷 A**：解锁一个招式 |
| `flagId` | string | **载荷 B**：点亮一个机械开关 |
| `prerequisiteIds` | List\<string\> | 前置节点 nodeId 列表（语义见 §5） |
| `treePosition` | Vector2 | 面板坐标（1080×1920 口径，原点=面板中心，+Y 向上） |
| `icon` | Sprite | 节点图标，留空用程序化圆形占位 |
| `hidden` | bool | 不在面板显示，但仍参与前置与存档（`d278593` 引入） |

> **载荷有效性**：`HasValidUnlockPayload` = `moveConfig != null || flagId 非空`。两者都空 = 无效节点，`TryUnlock` 返回 `Invalid` → **买不了**。
> ⚠️ 当前 `talent_unlock_c2` 就是这种状态（见 §9.1）。

### 3.2 本周新增字段

| 字段 | 位置 | 类型 | 用途 |
|---|---|---|---|
| `pageId` | `TalentNodeConfig` | string | 顶栏大页面分组（`ability` / `move` / `skill`） |
| `branchId` | `TalentNodeConfig` | string | 页内分支分组 |
| `branches[]` | `TalentTreeConfig` | List\<TalentBranchConfig\> | 分支元数据 |

```csharp
[Serializable]
public sealed class TalentBranchConfig
{
    public string branchId = "";
    public string displayName = "";
    [TextArea(2, 4)] public string description = "";
    public Sprite icon;
    public int order = 0;          // 页内分支排序
}
```

**为什么用字符串而不是枚举**：分支会随设计增删，字符串让策划直接在 Inspector 里填，不用改代码。存档里也只存 `nodeId`，不受影响。

**对存档的影响**：**零**。存档存的是 `nodeId` 字符串列表，不是节点下标，也不是结构体。新增字段只影响面板如何组织显示。

### 3.3 节点 id 命名约定

| 前缀 | 用途 | 例 |
|---|---|---|
| `talent_move_*` | 招式解锁节点 | `talent_move_c2` |
| `talent_flow_*` | 流派开线节点 | `talent_flow_charge` |
| `talent_skill_*` | 技能解锁节点 | `talent_skill_ironwall` |
| `talent_ability_*` | 能力节点 | `talent_ability_vitality` |

> ⚠️ **改名必须走迁移**。存档存 nodeId 字符串，直接改名 = 老档静默丢失该节点的解锁记录。详见 §7.2。

---

## 4. 载荷与消费方

| 载荷 | 消费方（查什么） | 现状 |
|---|---|---|
| `moveConfig` | `PlayerMoveStateMachine.IsMoveLocked()` —— 锁住的招式**不响应且不回退**成别的招式；`AttackSystem` 对招架另有单独门控 | ✅ 已在用（C4 / 招架） |
| `flagId` | `TalentTreeState.IsFlagUnlocked(tree, flagId)` | ❌ **全项目零调用**（见 §6） |
| 节点解锁态 | `TalentTreeState.IsUnlocked(nodeId)` | ✅ 已在用（`IceBreakSession` 的 `ice_thaw` / `ice_break_push` 用的就是这个，**不是** flag） |

### 4.1 ⚠️ flag 机制的真实状态（重要更正）

`IsFlagUnlocked` 在全项目**没有任何调用点**。`ice_thaw` / `ice_break_push` 看起来像「flag 被消费」，实际消费的是 **nodeId 解锁态**。

**含义**：设计文档里「flag = 点亮一个机械开关，后续系统读它」的机制**从未落地**。本周若要用 flag，必须同时建消费点；否则 flagId 字段只是装饰。

**建议**：本周**不引入新的 flag 消费点**，改为**统一用 nodeId 解锁态**表达「某能力已解锁」。理由：

- 消费方要的语义是「这个东西我有了没有」—— nodeId 解锁态已经能回答
- flagId 一个节点只能带一个值，多档位/多分支的 flag 语义会很快不够用
- 避免同时存在两套「已解锁」真相来源

→ 若确定要走 flag，需要先扩展 `TalentNodeConfig`（一个节点多个 flag）并建一个统一的 `TalentFlagService` 供战斗侧查询。**这是独立决策，不属于本周范围。**

---

## 5. 前置语义

### 5.1 规则

| `prerequisiteIds` 的值 | 语义 |
|---|---|
| **显式空列表** `[]` | **无前置** = 独立根节点（可单独购买） |
| **非空列表** | **全部前置都必须已解锁**（AND 语义） |
| `null`（老资产没这个字段） | 退回旧行为：前置 = 列表里的**上一个**节点 |

来源：`TalentTreeConfig.GetPrerequisites()`。

### 5.2 支持与不支持

| 能力 | 支持 |
|---|---|
| 多根并存（每条分支独立起点） | ✅ |
| 多前置（AND） | ✅ |
| OR 语义（满足任一即可） | ❌ |
| 互斥 / 专精 | ❌ |
| 分支计数（买满 N 个才开另一枝） | ❌ |

**本周确认不做** OR / 互斥 / 计数：

- 需求方向是「给玩家多个起始分支 = 选择空间」，互斥是砍掉选择空间，方向相反
- **铜钱本身就是天然互斥**（总量有限，选了这枝就没钱买那枝），不需要写代码

### 5.3 跨页前置

允许。例：`重击解冻` 若是能力页节点、其前置是招式页的 `指向突刺 C2`，`prerequisiteIds` 直接填 nodeId 即可。

面板侧呈现见 `design/talent-page-ui.md` §5（跨页连线画不了 → 用文字徽标）。

---

## 6. 能力类天赋的实现落点

| 天赋 | 效果 | 消费方 | 状态 |
|---|---|---|---|
| 铁血 | 生命上限 +30 | `PlayerState` 读 `heroConfig.maxHealth` | **需新增消费点** |
| 连击精通 | 连击阈值下调 | `ComboBuffConfig.triggers[]` 加一档即可 | ✅ **零代码**（`ComboManager.CheckTriggers` 已支持多档位） |
| 巧思 | 主动槽 +1 | `ActiveSkillInventory.Capacity` | **需拆字段**（见下） |
| 博学 | 被动槽 +1 | `UpgradePoolSampler` 的被动上限判定 | **需拆字段 + 新增消费点** |
| 聚财 | 铜钱获取 +30% | 敌人 `coinReward` / 里程碑 / 通关奖励结算处 | **需新增消费点** |
| 破阵 | 所有攻击 +1 架势伤害 | 伤害结算 | **需新增消费点** |
| 追击 | 击杀后攻击 CD −0.5s | 冷却判定 | **需新增消费点** |
| 运筹 | 三选一 → 四选一 | `UpgradeChoiceManager` 选项数 | **需新增消费点**（`in-game-growth-system.md` §3.12 已列该扩展接口） |
| 不屈 | 血量 <30% 触发 2s 无敌（每局 1 次） | `PlayerState.TakeDamage` | **需新增消费点** |

### 6.1 槽位字段必须拆（阻断项）

现状：`HeroConfig.itemSlotCount`（=4）**同时**被两个库存读取：

```csharp
// ActiveSkillInventory.cs:56
public int Capacity => ... heroConfig.itemSlotCount
// ItemInventory.cs:22
public int Capacity => ... heroConfig.itemSlotCount
```

→ 主动槽和道具槽共用一个字段。被动上限则硬编码在采样器里：

```csharp
// UpgradeChoiceManager.cs:98
() => UpgradeEffectManager.Instance != null && UpgradeEffectManager.Instance.PassiveUpgradeCount >= 6
```

**本周目标**：基础 **3 主动 + 3 被动**，天赋渐进解锁更多。

**需要拆成三个独立计数**：

| 字段 | 归属 | 默认 |
|---|---|---|
| `activeSkillSlotCount` | `HeroConfig` | 3 |
| `passiveSlotCount` | `HeroConfig` | 3 |
| `itemSlotCount` | `HeroConfig`（道具，保留原义） | 4 |

⚠️ **序列化陷阱**：Unity 新增 int 字段在老资产里是 **0**，不是默认值。必须**同时改 `Hero_Zhangfei.asset` 填 3**，并在读取处 `Mathf.Max(1, v)` 兜底，否则主动槽变 0 → 游戏直接坏。

### 6.2 被动槽口径与 UI 不一致（已知问题）

| | 口径 |
|---|---|
| **上限判定** | `UpgradeEffectManager.PassiveUpgradeCount` = `_appliedUpgrades.Count` → **含 Numeric**（专注 / 智慧 / 疾风 / 铁壁 / 地刺 / 拔苗助长…） |
| **被动排 UI 显示** | `PassiveSkillRow.IsPassive()` = 仅 `AttackPassive` + `TimedPassive` → **不含 Numeric** |

→ 玩家塞了 3 个数值被动，**被动排上一个图标都不显示**，但确实占了槽。**降到 3 槽后这个矛盾会变成玩家可见的问题**（频繁撞顶却不知道为什么）。

**处理选项（二选一，需拍板）**

| 选项 | 做法 | 影响 |
|---|---|---|
| **A（推荐）** | 被动排 UI 扩展为显示全部占用槽位的条目 | 玩家看得见占用了什么；不改数值逻辑 |
| **B** | 被动槽只算 `AttackPassive + TimedPassive`，Numeric 不占槽 | Numeric 变成无限槽（8 个数值条目 × 5 级 = 40 级），需要另设上限 |

---

## 7. 解锁判定链

### 7.1 API（`TalentTreeState`）

| API | 语义 | 谁用 |
|---|---|---|
| `IsUnlocked(nodeId / node)` | 存档里有没有这个 nodeId | `IceBreakSession`、面板刷新 |
| `IsAvailable(tree, node)` | 自己没解锁 **且** 全部前置已解锁 | 面板「可买」态 |
| `HasEnoughCoins(node)` | 存档铜钱 ≥ `coinCost` | 面板价格着色 |
| `TryUnlock(tree, node)` | 校验 → 扣钱 → 写存档 → 触发 `OnChanged` | 面板点击 |
| `IsMoveUnlocked(tree, move)` | 招式是否被某个已解锁节点指向 | — |
| `IsMoveGatedAndLocked(tree, move)` | 招式配了解锁节点但还没解锁 | `AttackSystem`（招架） |
| `IsFlagUnlocked(tree, flagId)` | flag 是否被已解锁节点点亮 | ⚠️ **零调用**（见 §4.1） |

### 7.2 存档

| 项 | 值 |
|---|---|
| 载体 | `SaveData.unlockedTalents`（nodeId 列表） |
| 分片 | `Assets/Scripts/Data/Save/TalentSaveSection.cs`（读写 / 迁移 / 清空） |
| 铜钱 | `SaveManager` → `PropSaveSection` → `PropType.Coin`（**注意**：`PropType` 目前只有 `Coin = 0`，注释写「后续扩展: Key, Gem, ...」） |
| 秘籍（新增） | **新建** `ManualSaveSection`，与 `TalentSaveSection` 同构（id 列表） |

**兼容性规则**

| 改动 | 是否安全 |
|---|---|
| 改 `displayName` / `description` / `coinCost` / `icon` / `treePosition` | ✅ 安全 |
| 改 `pageId` / `branchId` | ✅ 安全（不影响存档） |
| **改 `nodeId`** | ❌ **老档丢失该节点记录** → 必须写迁移 |
| **删节点** | ❌ 同上 |

---

## 8. 两条局外获取路径

| 路径 | 给什么 | 载体 | 存档 |
|---|---|---|---|
| **铜钱购买** | 流派 / 能力类：需要等级、需要前置、需要路线承诺 | 天赋树节点 | `unlockedTalents` |
| **秘籍掉落** | 技巧类：单次获得即永久生效、无等级、无铜钱成本 | 局内敌人 / 破坏物掉落 | `unlockedManuals`（新增） |

**两者不重叠。** 边界必须写死，否则玩家会问「能掉的为什么要花钱买」。

### 8.1 秘籍系统（详细见 `design/in-run-growth-and-unlocks.md` §8）

要点：

- `ManualDefinition : UpgradeDefinition` 继承 → **可直接放进现有加权掉落池**，池结构完全复用
- 掉落时按类型分派：`ManualDefinition` → 写存档解锁；普通 `category = Item` → 进 `ItemInventory`（局内道具栏）
- ⚠️ **秘籍不能走 `ItemInventory`**（局内道具栏有槽位上限、会弹弃置窗、局末清空）
- 敌人侧：`EnemyManager.TryDropItem` 开头有 **V2 短路**（`if (UsesActiveSkills) return;`）→ V2 下敌人不掉任何道具，需要处理
- 破坏物侧：`BreakableProp` 已有 `ISupplyPropTarget` / SupplyLayer 分层 / 描边 / 渐变销毁 / 局外奖励进存档的先例 → 加字段即可

---

## 9. 本周必做（数据层）

### 9.1 回滚 `d278593` 对 C2 的处理

该提交（2026-10-29）把 `talent_unlock_c2` 的 `moveConfig` 从 `Zhangfei_StabR2` **主动清空**并设为 `hidden`（commit message 用途②：「招式改为直接开放后，把原来的解锁节点藏起来」）。

**问题不是「引用丢失」，而是「改得不干净」**：

| 问题 | 后果 |
|---|---|
| 节点 = `hidden` + 无 `moveConfig` + 无 `flagId` | `HasValidUnlockPayload == false` → 永远买不了；`prerequisiteIds = []` → 不参与任何链 → **孤儿节点** |
| 同一提交把 `talent_ice_thaw.prerequisiteIds` 从 `[talent_unlock_c2]` 清空 | 冰系链剪断 |
| 同一提交把 `talent_ice_thaw.treePosition` 改成 `(-56, 360)` | 与 hidden 的 `talent_unlock_c2`（同样 `-56, 360`）**坐标完全重叠** |

**净效果**：C2 从「被天赋锁住」变成「完全免费直接开放」，天赋树上没有任何东西表示它。

→ **回滚动作**：恢复 `moveConfig = Zhangfei_StabR2`、取消 `hidden`、重新梳理它与冰系节点的前置关系。

### 9.2 删除 `talent_ice_break_push`

破冰顶开效果按决策迁移到冰雾的技能升级侧 → 该天赋节点删除。

⚠️ **连带**：`talent_unlock_c4.prerequisiteIds = [talent_ice_break_push]`。删掉后 `GetPrerequisites` 找不到 id 就跳过 → **C4 会静默变成「无前置根节点」**（不是买不到，是变成白送）。
→ `TalentTreeConfig.Validate()` 会报「前置 talent_ice_break_push 不存在」→ **改造前必须跑一次校验器**。

### 9.3 新增 `pageId` / `branchId` / `branches[]`

见 §3.2。

### 9.4 节点表重排

按 §2.1 的分支划分重排 `TalentTree_Main.asset`，同时：

- 补齐 `flagId` / `moveConfig` 载荷（所有节点必须 `HasValidUnlockPayload == true`）
- 修正 `treePosition`（当前 8 个节点挤在 x ∈ [-56, 56] 的一条斜线上，多分支必须重排）
- 5 个节点的 `icon` 为空 → 需美术补图

---

## 10. 校验规则

`TalentTreeConfig.Validate()` + `TalentTreeValidator`（菜单「校验天赋树」）现有检查：

| 检查 | 状态 |
|---|---|
| 前置 id 不存在 | ✅ |
| 自指（成环） | ✅ |
| 重复 nodeId | ✅ |
| 空 nodeId / 空节点 | ✅ |
| **多前置的环** | ❌ **只沿第一条前置边回溯**（`cur = ps[0]`）→ 多分支上线前必须修 |

**本周新增校验建议**

| 新增 | 理由 |
|---|---|
| 节点 `HasValidUnlockPayload` 必须为 true | 防止再出现「买不了的节点」 |
| `pageId` / `branchId` 必须在 `branches[]` / 页面清单里有定义 | 防止节点被漏在分组外 |
| 同 `branchId` 内必须存在至少一个根节点 | 防止分支画不出来 |
| `treePosition` 不得重复 | 已发生过 c2 / ice_thaw 完全重叠 |
| 同一分支内不得有 `hidden` 节点作为唯一入口 | 隐藏节点不画连线，会让玩家看到孤立节点 |

---

## 11. 明确不做（本周）

| 项 | 理由 |
|---|---|
| OR 语义前置 | 当前设计不需要；不为了「将来可能用」扩模型 |
| 互斥 / 专精 | 与「多个起始分支 = 给玩家选择空间」的方向相反；铜钱已是天然互斥 |
| 分支计数 | 同上 |
| 分支代价（买 A 让 B 变贵） | 同上 |
| flag 机制扩展 | 见 §4.1——本周统一用 nodeId 解锁态 |
| 关卡需求 ↔ 解锁度对照展示 | 有价值但要新增「关卡 → 需要能力」映射，成本不低，**第二阶段再议** |

---

## 12. 参考

| 文件 | 职责 |
|---|---|
| `Assets/Scripts/Data/TalentTreeConfig.cs` | 数据模型 + `Validate()` |
| `Assets/Scripts/Core/TalentTreeState.cs` | 解锁判定 + 存档读写 |
| `Assets/Scripts/Data/Save/TalentSaveSection.cs` | 天赋存档分片 |
| `Assets/Scripts/Data/UpgradeDefinition.cs` | `requiredTalentIds` / `prerequisites` |
| `Assets/Scripts/Core/TalentUpgradeRequirementProvider.cs` | 天赋 → 技能入池（AND 语义） |
| `Assets/Scripts/MoveSystem/PlayerMoveStateMachine.cs:330` | 招式天赋锁定判定 |
| `Assets/Scripts/Player/Combat/IceBreakSession.cs` | 冰系破冰（消费 nodeId 解锁态） |
| `design/talent-page-ui.md` | 页面视觉与交互 |
| `design/in-run-growth-and-unlocks.md` | 局内成长与解锁消费端 |
| `design/out-of-game-growth-v2-analysis.md` | 现状盘点与实测证据 |
