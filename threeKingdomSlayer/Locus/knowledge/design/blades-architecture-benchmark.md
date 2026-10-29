---
id: kd_7251485b-a10d-4f6e-b422-d7e2b4dc6e2b
injectMode: inherit
injectAgents:
- '*'
summary: 对标游戏《上古卷轴：刀锋》(Blades) 的架构测绘与可借鉴项：身份确认（Unity 2019.2.16f1 / IL2CPP / 14498 类型 / 1482 bundle）、内容清单结构、技能=每 Rank 一资产的数值与表现字段模型、敌人原型+变体+线性成长系数、任务/挑战/对话的声明式条件门、UID+校验哈希寻址；给出对我方项目的采纳清单（表现三段解耦 / Rank 资产化 / 敌人变体 / 引用校验哈希 / PropertyLogic 词缀层 / 解锁门数据化）与目标文件、成本、风险，并明确不借鉴其字符串 ID 间接寻址（与 anti-ghost-reference 规范冲突）。含证据分级与产物索引。
aiEditMode: inherit
---

# 对标分析：《上古卷轴：刀锋》(Blades) 架构测绘与可借鉴项

> **来源**：对已停服安卓包 `shanggujuanzhoudaofeng.apk`（1.23 GB）的静态逆向 + 雷电模拟器运行实测。
> **证据分级**：【运行时】= 模拟器实跑日志/截图；【静态】= IL2CPP metadata / AssetBundle 直接读出；【推断】= 合理推断但未实证。
> **边界**：本文只做架构分析。抽取的美术/音频/资产表**不进本仓库、不进本项目、不对外分发**；仅作设计参考。原始 APK 未做任何修改。
> **分析日期**：2026-10-28。工具与产出物位置见 §9。

---

## 0. 结论先行

### 0.1 值得偷师的 5 项

| # | 做法 | Blades 怎么做 | 对我们的价值 | 采纳判断 |
|---|---|---|---|---|
| 1 | **技能 = 每个 Rank 一个独立资产** | 41 个技能类 / 674 个 rank 资产（`FireballRank11` 这种），每个资产自带全套数值 | 策划改数值=改资产，不动代码；等级曲线天然可审 | **采纳** |
| 2 | **表现三段解耦**：施法 / 弹道 / 命中 | `_castingPointers` / `_projectilePointers` / `_impactPointers` 三套独立 UID 指针 | 直接命中我们「VFX 归属混乱」的老问题 | **采纳**（改 `AttackSkillConfig`） |
| 3 | **敌人 = 原型 + 变体 + 缩放系数** | `EnemyData` 101 原型 → `EnemyVariant` 189 变体；变体带 `_stats`（`healthBase + healthIncrement` 线性成长）、`_aiParameter`（反应时间窗口）、`_knownAbilities` | 我们现在敌人是「一敌人一 Prefab」，加等级就要复制 Prefab | **条件采纳**（先落 `EnemyTypes` 侧的缩放系数，不动 Prefab） |
| 4 | **资产寻址带校验哈希** | 所有引用是 `{_id, _validationHash, _description, _type}`，改资产不破引用，坏引用可检测 | 解决我们「改资产后引用静默失效」 | **条件采纳**：只借「校验哈希 + 编辑期校验」思路，**不借**它的间接寻址（见 §4.3 与我们的反幽灵引用规范冲突） |
| 5 | **内容清单与本体分离** | 本体 AssetBundle（1482 个 / 1.12 GB）+ `Manifest` / `MetaManifest.json` / `Manifests` 结构，远端只发增量 | 我们做 Android 包体优化的天然路线（见 `plan/package-size-optimization.md`） | **采纳（远期）** |

### 0.2 明确「不借鉴」的一项

**不借鉴它的字符串 ID 间接查表**。Blades 用 UID + 描述串寻址，好处是抗热更；代价是引用在 Inspector 里不可见、策划不知道运行时读的是哪份数据。
本项目已有明文规范禁止这种模式：**`design/anti-ghost-reference.md`**（禁止 `Resources.Load` / 静态缓存 / 字符串 ID 查找）。
→ **我们保留 Inspector 直接引用，只加「校验哈希 + 编辑期一致性检查」这一层保险。**

### 0.3 一条与玩法无关但值得看的发现

Blades **把服务端权威逻辑打进了客户端包**（`BGS.Game.Network.Server.*`：`ServerActor` / `ServerEncounter` / `ServerCombatLog` / `PvpServerGameLogic`），并支持 `PvpEnvironment.DirectServerIp/Port` 直连与 `FightDummy` 木桩模式。
→ 对我们无直接价值（我们是单机战斗游戏），但它证明了一件事：**「网络层抽象」做对了，客户端就能自持服务端**。我们若将来做排行榜/云存档，`Assets/Scripts/Services` 那一层的边界（asmdef 已分层）能直接照抄这个思路。

---

## 1. 这游戏是什么（身份确认）

| 项 | 值 | 证据 |
|---|---|---|
| 产品 | 《上古卷轴：刀锋》(Blades) | 启动画面 logo「上古卷轴」；metadata 内 `com.zenimax.blade`【运行时】 |
| 包名 | `com.shengqugames.blades.aligames` | 模拟器 `pm list packages`【运行时】 |
| 版本 | 1.6.3.1.108（versionCode 108，minSdk 21 / targetSdk 26） | `dumpsys package`【运行时】 |
| 研发 | 吉极游戏（Jiji Games）· 中国发行：神启游戏 | metadata 内 `*.jijiagames.com` / `*.playbladesasia.com`【静态】 |
| 引擎 | Unity **2019.2.16f1**，IL2CPP，`Stripping = Disabled` | `globalgamemanagers` + 启动日志【运行时】 |
| 音频 | Wwise（AkSoundEngine + `wwise_sb_*` SoundBank 全部内置） | `lib/` + bundle 名【静态】 |
| 骨骼 | FinalIK + VRIK | metadata `RootMotion.FinalIK`（80 类型）【静态】 |
| AI | Behavior Designer（`ai_behaviortrees` bundle） | bundle + metadata【静态】 |
| 对话/任务 | Devdog Quest System Pro | metadata 81 类型【静态】 |
| 代码规模 | **14498 托管类型 / 99958 方法 / 62948 字段** | global-metadata v24 全量导出【静态】 |

> 规模对照：本项目 `Assets/Scripts` 226 脚本 / 0→12 asmdef 批次后分层（见 `plan/architecture-optimization-handoff-20261020.md`）。
> Blades 的托管类型量是我们的 **60 倍级**——它不是「一个小手游」，是被改造成手游的整款 3A 内容量。

---

## 2. 内容管线：AssetBundle 分组与清单结构

### 2.1 分组实测【静态】

1482 个 bundle / 1.12 GB，扁平放在 `assets/Bundles/`：

| 组 | 内容 |
|---|---|
| `town_*` | 城镇建筑（townhall / house / workshop / enchanter / forge / alchemy…）546 个 |
| `levels_*` | 关卡与地牢房间（cavedungeon / forestdungeon / ayleiddungeon…）576 个 |
| `character_*` | 敌人（draugr / atronach / lich / spider / dragon / bear / skeletons…）+ 玩家形象 |
| `weapon* / armor* / shield* / ingredient*` | 武器/护甲/盾/素材 |
| `wwise_sb_*` | Wwise 音效 Bank（按技能/场景/角色切分，如 `wwise_sb_spell_fireball`） |
| `ui_* / menus_*` | UI 资源 |
| `clutter_* / facedecals / visual_*` | 场景杂物、贴花、视觉特效 |
| `ai_behaviortrees` | 敌人行为树资产 |

### 2.2 清单结构【静态】

metadata 字面量里给出了客户端期待的清单文件名【静态】：

```
MetaManifest.json      ← 顶层清单（描述版本 + 内容总览）
Manifests              ← 各 bundle 的依赖清单目录
Bundles                ← bundle 目录
ManifestExtraData
WwiseExtractedAssets / WwiseExtractionCache.bin
```
清单里的 bundle 分组名也已确认：`gameplaymetadata / common / common_client / dungeonsettings / localization / common_stripped / pvpserver`。
→ **我们做包体优化（`plan/package-size-optimization.md`）时可以直接借用这套「本体 + 增量清单」的分层命名。**

### 2.3 运行时验证：内容本体确实全在包内【运行时】

APK 装进雷电模拟器后**能正常启动到标题画面**（见 `boot_screen2.png`），说明本体资源完整、无需联网即可加载标题。
卡点只有一个：拉不到远端清单 → 报「更新包下载失败 (0-10000-1)」。

---

## 3. 数据建模：三种最值得抄的表结构

### 3.1 技能：每 Rank 一个资产【静态】

`assets/Bundles/abilities_sab` 里 828 个 MonoBehaviour，对应 **41 个技能类 / 674 个 rank 资产**。
单条资产（`BGS.Game.Gameplay.FireballAbility` 的 rank11）完整字段：

| 字段 | 示例值 | 含义 |
|---|---|---|
| `_abilityRank` | 11 | 技能等级 |
| `_abilityPointCost` | -1 | 技能点消耗 |
| `_staminaCost` / `_magickaCost` | 0 / 195 | 耐力/魔力消耗 |
| `_cooldown` / `_initialCooldown` | 3.54 / 1.40 | CD / 初始 CD |
| `_channelDuration` | 0.90 | 引导时长 |
| `_projectileSpeed` | 15.0 | 弹速 |
| `_damage` | 353.18 | 伤害 |
| `_powerLevel` | 57 | 威力等级（用于伤害公式的档位） |
| `_castingPointers` / `_projectilePointers` / `_impactPointers` | UID 指针 | **施法/弹道/命中三套表现** |
| `_requiredHeroLevel` / `_canBeCastOutOfCombat` | -1 / 0 | 解锁与脱战可用 |

**可借鉴点**：
- 数值与表现解耦在**同一个资产的不同字段组**里 → 策划改数值不动表现，改表现不动数值。
- `_powerLevel` 这种「档位」字段：伤害不写死，而是按等级档位在代码里查表 → 数值调平衡时只动一列。

### 3.2 敌人：原型 + 变体【静态】

- `BGS.Game.Data.EnemyData`（101 条）：`_enemyName`、`_variants[]`、`_lootTableList[]`、`_experienceTableId`、`_bossFamilyIds`、`_category`。
- `BGS.Game.Data.EnemyVariant`（189 条）：`_stats`（`_healthBase` / `_healthIncrement` / `_magickaBase` / `_stamina…`）、`_compound`（外观组合）、`_knownAbilities[]`、`_aiParameter`（`_reactionTime.minimumValue=0.2 / maximumValue=0.3` 等）、`_spellCooldownFactor` / `_maneuverCooldownFactor`、`_isSpellCaster`。

**可借鉴点**：
- **线性成长**：`healthBase + healthIncrement`（按等级）—— 我们敌人 HP 现在是每种一个固定值。
- **AI 参数是数据不是代码**：反应时间窗口写成 min/max 区间 → 同一套 AI 有「迟钝型/机敏型」个体差异，不需要写第二套 AI。
- **技能表挂在敌人身上**：`_knownAbilities` 是数据引用 → 敌人行为差异是配置差异。

### 3.3 任务 / 挑战 / 对话：声明式 + 条件门【静态】

| 表 | 规模 | 结构 |
|---|---|---|
| `Challenge.*ChallengeData`（DealDamage/Defeat/Smith/Brew/Perform/Build/PerformAbility） | **2402 条** | `_weight` `_minQuantity/_maxQuantity` `_oneTimeOnly` `_challengePoolCategory` `_reward` |
| `Challenge.ChallengeSelector` | 216 条 | `_requiredPlayerLevel` `_lockedAtPlayerLevel` `_requiredTownLevel` `_lockedAtTownLevel` `_requiredAbyssLevel` `_requiredQuestPointer` `_requiredBuildingLevel` |
| `LevelGeneration.DungeonSettingsScriptableObject` | 320 条 | 入口房 / 房间链接 / 固定布局 |
| `LevelGeneration.Abyss.AbyssSlice` | 161 条 | `_levelRange` `_randomWeight` `_dungeonSettingsPointer` |
| `Quests.*QuestHolderScriptableObject` | 98 条 | `_serializedJsonString`（整棵任务图序列化进 JSON）+ UID 指针 |
| `Devdog.QuestSystemPro.Dialogue.Dialogue` | 669 条 | 节点图：`_nodes` / `_edges` / 条件挂 `Flag` |

**可借鉴点**：
- **四重解锁门**（玩家等级 / 城镇等级 / 深渊等级 / 前置任务 / 建筑等级）是**纯数据**，我们的节点奖励解锁目前是硬编码在代码里的。
- **任务整图序列化成一个 JSON 字段** → 任务资产可整体版本化/热更；我们的 `DialogueEventData` 是逐条资产。
- **对话边挂条件（Flag）+ 目标值 + 比较符** → 条件系统是数据。

### 3.4 引用机制：UID + 校验哈希（⚠ 与我们的规范冲突）【静态】

所有跨资产引用长这样：
```json
{"_uid": {"_id": "d4764ff8-b8fa-462c-ac85-22bc7f25f8bd",
          "_validationHash": 1293715972,
          "_description": "BFQ2_03_DungeonSettings_A",
          "_type": "BGS.Game.LevelGeneration.DungeonSettings"},
 "_uidParent": {...}, "_searchType": 0}
```
→ 引用**不依赖对象在文件里的位置**，改资产不破引用；`_validationHash` 能在加载/编辑期发现「引用指向的内容变了」。

**与我们的冲突**：`design/anti-ghost-reference.md` 明令禁止「字符串 ID 查找 / 间接层」，因为 Inspector 里看不到真实数据来源。
**结论（折中）**：
- ✅ 借：`_validationHash` 机制 → 给我们的资产引用加一个「内容哈希」，Editor 里做一致性校验 + 破坏引用时立刻报错，而不是运行时静默用错数据。
- ❌ 不借：GUID/字符串寻址 + 运行时查表 → 我们继续用 Inspector 直接引用。

---

## 4. 战斗与 AI 框架

### 4.1 技能执行是分步状态机【静态】

类型名可证：`AbilityExecutionStep` / `AbilityExecutionResult` / `AbilityExecutionType` / `AbilityExecutor` / `AbilityInterrupted` / `AbilityReasonFailure` / `AbilityExecutionEnded`。
→ 技能 = 一串步骤，每步有自己的类型与可中断点。

### 4.2 战斗状态用消息枚举全枚举了一遍【静态】

`BGS.Game.Network.Shared` 下的状态消息：`PlayerChargingStateChangeMessage` / `PlayerBlockingStateChangeMessage` / `PlayerDrainingStateChangeMessage` / `PlayerChannelingStateChangeMessage` / `PlayerFollowThroughStateChangeMessage` / `PlayerRecoveryStateChangeMessage` / `PlayerAutoAttackStateChangeMessage` / `PlayerAttackStateChangeMessage` / `PlayerStateChangeMessage` / `PlayerDeadStateChangeMessage`。
→ 蓄力 / 招架 / 引导 / 后摇 / 硬直 / 死亡 —— 每个都是**独立的、可单独通知的状态**。
这与我们 `design/attack-cooldown.md`（攻击冷却双模式：独立 CD vs 动作锁定）和 `design/attack-interrupt-system.md`（三级打断）的方向一致，但 Blades 把它们做成了**可枚举、可消息化的状态集合**，而不是每个招式各写一套开关。

### 4.3 状态效果与元素【静态】

- 元素：火/冰/毒/雷（`AugmentedFlamesPerk` / `AugmentedFrostPerk` / `AugmentedPoisonPerk` / `AugmentedShockPerk`）。
- 状态效果：`CombatStatusEffect` / `CombatStatusElementalEffect` / `ChangeStatusEffectMessage` / `RestoreActorMessage`。
- 装备词缀逻辑 18 种（`BGS.Game.ActorBonus.*PropertyLogic`）：`HastePropertyLogic`（加速）、`PowerfulBlockPropertyLogic`（强化格挡）、`InstantKillPropertyLogic`（处决）、`ShortenStaggerPropertyLogic`（缩短硬直）、`SelfRepairOnDamagePropertyLogic`（受伤自愈）、`FortifyComboDamagePropertyLogic`（强化连段伤害）…

**可借鉴点**：`ActorBonus` 这一层把「被动/词缀」做成**统一的可组合属性逻辑**，而不是每个技能各写 if。我们的 `UpgradeEffectManager` 现在是 1276 行的上帝类（见 `design/architecture-decoupling-analysis.md`）——Blades 的解法是：**每个词缀一个 PropertyLogic 类 + 统一接口**，这样加词缀=加一个类，不动中心管理器。
→ 这条我列为**第二优先**（收益大，但改动面比 §0.1 那几条大）。

---

## 5. 服务端与网络架构（对我们主要是「反面教材」与「架构范式」）

### 5.1 客户端/服务端分层【静态】

- 共享协议：`BGS.Game.Network.Shared`（108 类型）+ `Blade.RN.Shared`（60 类型）
- 客户端：`BGS.Game.Network.Client`（45）+ `Blade.RN.Client`（17）
- 服务端逻辑：`BGS.Game.Network.Server`（7）+ `Blade.RN.Shared.ENetLib`（22）

### 5.2 实时层是自研 ENet 框架【静态】

`Blade.RN` 自研：ENet 可靠 UDP 传输（`Blade.RN.Client.ENetLib` / `Blade.RN.Shared.ENetLib`）+ DiffieHellman/Crypto 握手 + `NetData` 属性复制系统（`NetDataProperty` 13 种基础类型）+ `NetObject` 复制 + `NetControllerThread`（网络线程与主线程分离）。
→ 关键工程点：**属性级复制**（只同步变化的那个属性），不是整对象快照。

### 5.3 REST 层规模【静态】

- 路径常量 **183 条**（`/characters/{0}/quests/{1}/complete`、`/characters/{0}/towns/current/buildings/{1}/upgrade`…）
- DTO **271 个**（`BGS.Shared.Rest.Api.BladeServer`）、服务类 **116 个**（`BGS.Game.Rest.Services`）
- 环境配置：**16 个环境名**（local/dev/qa/int/devtest/devblue…prod）+ **14 个区域**（us-east-1…sa-east-1），每环境 6 个 base URL（authentication / game / admin / analytics / arena / 主机名）
- PvP 竞技场服走 **AWS GameLift**（`GetAwsGameliftEndpoints`）

### 5.4 运行时验证：服务端已死，客户端卡在引导 JSON【运行时】

```
E/Unity: Curl error 6: Could not resolve host: bladesasia.jijiagames.com
```
→ 客户端启动第一件事是拉 `https://bladesasia.jijiagames.com/china/Android/host_info_cn.json`（引导文件，决定连哪台机），这步失败 → 直接弹「更新包下载失败」。
同时：`http://api.mygm.sdo.com/v1/basic/handshake` **成功**、`Ghome init is 0` → **九游/GHome 账号链还活着**。

> 附带的架构启示：**用一个 JSON 引导文件把「环境」与「代码」解耦**，是运营上极省事的设计（换服不用发版）。我们如果将来做多渠道/多环境包，可以抄这个接缝。

---

## 6. 对我们项目的采纳清单

> 成本单位：S ≈ 半天，M ≈ 1–3 天，L ≈ 1 周+。「目标文件」是我们仓库内的落点。

| 优先级 | 采纳项 | 目标文件 | 成本 | 收益 | 主要风险 |
|---|---|---|---|---|---|
| **P0** | 表现三段解耦（施法/弹道/命中） | `Assets/Scripts/Data/AttackSkillConfig.cs`、`Assets/Scripts/Data/MoveTableConfig.cs` | M | 命中我们「VFX 归属混乱」痛点；策划可独立调表现 | 现有招式资产要迁移字段 |
| **P0** | 技能 Rank 资产化（数值/表现/解锁分字段组） | `Assets/Scripts/Data/ActiveSkillDefinition.cs`、`Assets/Scripts/Data/UltimateSkillConfig.cs` | M | 平衡调整不再碰代码；等级曲线可审 | 需先定 Rank 字段规范 |
| **P1** | 敌人原型+变体（HP/伤害线性成长系数、AI 反应时间窗口） | `Assets/Scripts/Data/EnemyTypes.cs`、`Assets/Scripts/Enemy/Enemy*.cs` | M | 加等级不用复制 Prefab；敌人个体差异做成配置 | Prefab 与变体表的对应关系要定清楚 |
| **P1** | 资产引用校验哈希 + Editor 一致性检查 | 新增 Editor 工具（放 `Assets/Scripts/Editor/`） | S–M | 改资产后引用静默失效能被立刻发现 | 需先有稳定的引用结构 |
| **P1** | 状态效果统一 PropertyLogic 层 | `Assets/Scripts/Core/UpgradeEffectManager.cs`（现 1276 行上帝类） | L | 新增词缀=加一个类，中心管理器不再膨胀 | 改动面大，需排期（对齐 `plan/architecture-optimization-handoff-20261020.md` 批次制） |
| **P2** | 解锁门纯数据化（等级/城镇等级/前置任务/建筑等级） | `Assets/Scripts/Data/FakeRouteNodeConfig.cs`、`FakeRouteChoiceConfig.cs` | M | 节点奖励解锁不再硬编码 | 依赖 §3.3 的条件数据模型 |
| **P2** | 「本体 + 增量清单」分包体 | `plan/package-size-optimization.md` | L | 目标 50MB → 8–12MB 有结构可循 | 工程量大 |

**不做**：UID/GUID 间接寻址（与 `design/anti-ghost-reference.md` 冲突）。

---

## 7. 风险与边界

1. **版权**：内容与美术授权自 ZeniMax；Blades 的代码/资产/数据表均属其权利人。抽取物**仅本地私用做设计参考**，不进本仓库、不进 Unity 项目、不对外分发、不上传。
2. **不做破解**：不剥离客户端里的授权与付费内容校验、不对外提供改包。本文的离线化分析只到「技术接缝与可行性」层面。
3. **对本项目无直接代码收益**：Blades 的服务端架构对我们的单机玩法没有移植价值；值得抄的是**数据建模与表现分层**，不是它的联机系统。
4. **不要照抄它的量级**：14498 类型是 3A 内容量级；我们按「加一条内容 = 加资产」的原则走（见 `memory/unity-project-understanding/README.md`），不要被它的规模带偏。

---

## 8. 待验证 / 未完成

- Blades 的 `host_info_cn.json` **精确 JSON 字段结构**仍未知（原服务器已死，文件拿不到）。目前只有 `EndpointsDefinition` 的行为证据（`GetCurrentBackendName` / `GetBackendEndpointType` / `GetAppKey` / `GetAwsGameliftEndpoints` / `SetNetworkLocation`）与 16 环境 + 14 区域的取值集合。→ 待本地影子后端跑通后反推。
- REST 183 条路径的**请求/响应字段级契约**尚未成文（DTO 字段已全部导出，但没和路径、方法、HTTP 动词对齐）。
- Blades 的**数值公式**（`_powerLevel` → 伤害）未反推（需 IL2CPP 反汇编，本轮未做）。

---

## 9. 证据索引

产出物全在 `Library/Locus/tmp/blades/`（Unity 的 `Library` 目录，不入库）：

| 文件 | 内容 |
|---|---|
| `types.csv` | 14498 类型（namespace / base / 方法数 / 字段数 / 接口标记） |
| `methods.csv` | 99958 方法（类型 + 方法名） |
| `fields.csv` | 62948 字段（类型 + 字段名） |
| `namespace_summary.csv` | 命名空间统计 |
| `string_literals.txt` | 15399 条 IL2CPP 字符串常量（API 路径/环境名/错误文案都在里面） |
| `tables/*.csv` | 257 张设计数据表（技能/敌人/任务/对话/掉落/配方/挑战…） |
| `dump_metadata.py` / `dump_tables.py` | 可复跑的解析脚本 |
| `server.py` + `ca.crt`/`server.crt`/`server.key` | 本地影子后端（TLS + 请求日志 + 路由表） |
| `logcat_boot2.txt` / `boot_screen2.png` | 模拟器启动日志与「更新包下载失败」截图 |

复跑方式见 `memory/blades-reverse-engineering-assets.md`。

**相关文档**：`design/anti-ghost-reference.md`（§3.4 冲突依据）、`design/architecture-decoupling-analysis.md`（§4.3 改造依据）、`design/attack-cooldown.md`、`design/attack-interrupt-system.md`（§4.2 状态集合）、`plan/package-size-optimization.md`（§6 P2）、`memory/unity-project-understanding/README.md`（§7.4 纪律）。
