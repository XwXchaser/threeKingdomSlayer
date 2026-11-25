---
id: kd_5aa5247f-6d6e-4ca4-aaf0-4c910e02522e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 无尽关 demo 交接（2026-11-24）

> **最新入口（2026-11-25）**：先读本文件 **§0.9（当前状态 / 验证 / 待配置项）** 与 `plan/endless-demo-equipment-p0-implementation-plan.md`，玩法规则以 `design/gameplay-progression-loop.md` §3.3–§3.4 为准。
> §0–§0.8、§1–§7 保留历史批次背景；其中 F 状态、装备品质、同款升级、候选分池及验证描述若与 §0.8 / §0.9 / P0 冲突，以 §0.9 为准。
> 需求与节点历史数据见 `plan/endless-demo-requirements-20261124.md`；敌人变体见 `design/enemy-variants.md`。

---

## 0. 一句话现状

**装备变体抽取 + 波末换装 + 经验品质的框架已落地（2026-11-25）**：现有 8 主动 / 8 饰品 / 14 属性已入池，圈数→档位、同次按种类去重、空槽 / 同款 / 满槽换装与配置报错全部接通；**未进 Play Mode、正式数值待用户填写**。
（§0.5–§0.8 是历史批次记录；现状、验证与待办以 **§0.9** 为准。）

---

## 0.5 本轮决策与进度（2026-11-24 第二批）

**用户已定三件事**（解除 F1/F5/F6 的阻塞）：

1. **招式归武器**：另外建立**简易的「换武器 + 武器招式解锁升级」界面**，先做功能，UI 布局与美术后续再优化。
   ⇒ 招式不再由天赋树售卖；`talent_move_*` 随 Moves 树移出，招式的解锁/升级挂到武器上。
2. **初始解锁名单**：现有影之武装 / 饰品**全部视为已解锁**。
3. **分数口径**：删除圈里程碑分 —— **从来没有这样的内容**。

**已完成（编译通过，Console 0 error）**

| 项 | 落点 |
|---|---|
| F1 存档字段 | `SaveData.unlockedActiveSkills` / `unlockedAccessories`（旧档迁移补空列表） |
| F1 分片 | `Assets/Scripts/Data/Save/EquipmentUnlockSaveSection.cs`（已登记进 `SaveSections`） |
| F1 判定/发放 | `Assets/Scripts/Core/EquipmentUnlockService.cs`（`IsEquipment` / `IsUnlocked` / `Grant`） |
| F1 初始解锁 | `UpgradeDefinition.unlockedByDefault`（默认 true ⇒ 现有内容全部视为已解锁；新内容取消勾选即走掉落解锁） |
| F7 击杀铜钱 | **已在原链路上**：`StageRunFlow.OnEnemyDied`（`StageController.Start` 无条件订阅）已经做了 `AddKill + AddCoins(coinReward × 聚财)`，且不按关卡状态拦截 ⇒ 无尽关一直在加钱，**不要再在击杀回调里加一遍** |
| F7 落盘 | `StageController.SettleCoins()` 改 public；`EndlessRunController.EndRun` 在 `SaveBest` 前调用（阵亡 / 退出都算）—— 原来阵亡不结算，本局铜钱全丢，这才是真正缺口 |
| F6 去圈 | `EndlessScoreLedger` 删 `CircleScore` / `SettleCircle` / `circleBaseBonus` / `circleBonusStep`；`EndlessHud` 删「第 N 圈」 |
| 计分简化 | **只按击杀计分**：`AddKill` 即刻加分并抛 `OnScoreChanged`（HUD 数字当场跳）。删掉 `SettleWave` / `WaveSettlementInput` / `WaveScore` / 连击/无伤/快枪系数 / `CircleDifficultyMultiplier`，以及波末飘字；控制器不再订 `WaveSpawner` 的波次事件 |
| F6 结算面板 | `EndlessHud` 运行时创建全屏面板：本局总分 + 历史最高（含「新纪录」），淡入 0.28 s，确认回落地页 |
| F6 退出接线 | `PauseMenuUI.OnMainMenuClicked` → 无尽运行中改走 `EndRun(false)`（原来直奔 `GoToMainMenu`，丢分丢钱且不弹面板） |
| 新高分判定 | `EndlessRunController.LastRunSetRecord`（写档前判定，修掉「有分就算新纪录」） |
| F2 主动换装 | `ActiveSkillInventory.Replace(entryId, def)` / `Remove(entryId)`（槽位不动、内容换掉、等级回 Lv.1） |
| F2 饰品换装 | `UpgradeEffectManager.RemoveUpgrade(def)`（新文件 `UpgradeEffectManager.Remove.cs`）+ `Assets/Scripts/Core/AccessoryLoadout.cs` |
| F3 候选过滤 | `UpgradePoolSampler.CollectActiveSkills` / `CollectAccessories` 加 `EquipmentUnlockService.IsUnlocked` |

**F2 关键限制**：`RemoveUpgrade` **只支持占被动槽的机制型被动**（`AttackPassive` / `TimedPassive`）。
`Numeric` 类机制条目（地刺 / 击退波 / 聚拢波 / 反伤）的效果是层叠相加的，没有可逆减法 ⇒ 明确拒绝拆卸（返回 false）。
以后要支持它们，得做「从持有列表整体重算」而不是减色。

**F3 未做**：满槽时的「选哪个槽替换」交互面板（需要新 UI，可照 `ItemDiscardPopup` + `IItemDiscardPrompt` 那套契约写）。

**验证范围**：静态编译 + 只读核对（存档迁移、分片登记、解锁判定、账本口径）已通过；**未进 Play Mode 实测**，手感与面板版面待用户验收。

**已知遗留**：`SaveData.endlessMaxCircle` 仍在写但已无任何界面读取，属待清理的死数据。

**口径纠正（2026-11-24 实读代码）**：需求文档 §5.1 写的「无尽模式不产铜钱」不成立 ——
钱一直在进本局 session 钱包（`StageRunFlow.OnEnemyDied`），缺的只是阵亡时的落盘。
以后看到「无尽不产钱」先查这条订阅链，别在击杀回调里重加一遍（会双倍）。

**下一批顺序**：F2（槽位/替换 + 被动重建重算）→ F3（候选过滤 + 满槽替换交互）→ 武器/招式简易界面 → F5（天赋解耦 + 清 12 条引用）→ F4 → F8。

---

## 0.6 第三批（2026-11-24，已验收）：武器 + 招式界面、天赋解耦

### 已做

| 项 | 落点 |
|---|---|
| 武器数据 | `Assets/Scripts/Data/MainWeaponConfig.cs`（含 `WeaponMoveEntry`：解锁价 / 升级价 / 每级伤害 / 引导）、`MainWeaponCatalog.cs` |
| 武器资产 | `Assets/ScriptableObjects/Weapons/MainWeapon_ZhangfeiSpear.asset`（长枪）、`MainWeaponCatalog_Main.asset`（已挂到 `Hero_Zhangfei.weaponCatalog`） |
| 存档 | `SaveData.equippedWeaponId` / `unlockedWeaponMoves` + `WeaponSaveSection`（**没有招式等级字段**） |
| 服务 | `Assets/Scripts/Core/WeaponService.cs`：`Equipped` / `TryEquip` / `IsMoveUnlocked` / `IsEntryUnlocked` / `ArePrerequisitesMet` / `TryUnlockMove` / `ApplyEquippedHeroConfig` |
| 招式门改道 | `PlayerMoveStateMachine.IsMoveLocked`、`AttackSystem` 招架门 → `WeaponService`；`PlayerState.Start` 套用存档武器 |
| 招式伤害 | `AttackSystem.GetFinalDamage` × `WeaponService.GetMoveDamageMultiplier`（每级 +10%） |
| 简易 UI | `Assets/Scripts/UI/WeaponLoadoutPanel.cs`（左选武器 / 右**招式树** / 铜钱）、`HubWeaponEntry.cs`（落地页克隆「天赋」卡出一个「武器」入口，不改场景） |
| 入口创建点 | `HubHudModeSwitch.ApplyFromState()` → `HubWeaponEntry.EnsureCreated()`。⚠ **不能放 `BattleHubTabBar.Start`**：该 tab 栏在落地页是 **inactive**，`Start` 永不执行（这就是“界面没变化”的原因） |
| F5 天赋解耦 | 12 条 `requiredTalentIds` 全部清空；`TalentCatalog_Main` = `[Stats, null, null]`；`Hero_Zhangfei.talentTree = null`；`TalentTree_Pools` / `TalentTree_Moves` 节点清空（文件保留避免 GUID 断链） |
| 教学引导迁移 | `WeaponMoveEntry` 新增 `moveGuideGesture/Dialogue/Phase2/Phase3`；`MoveGuideService.CollectPending(tree, catalog, weapon)` 先扫武器；横扫 / 指向突刺 两条台词已搬过去（id 前缀 `weapon_move:`） |

### 招式树口径（2026-11-24 初版；**2026-11-25 纠正**）

- **基础招式不进树**：**戳 / 上挑 / 招架**写在武将技能表 `hero.skillConfigs` 里，永远可用。
  ⚠️ **横扫不在此列**：旧 `TalentTree_Moves` 里 `talent_move_slash`（横扫，`moveGuideGesture=SwipeHorizontal`，引导 `guide_move_slash`）本来就是**解锁项**，且是 `斩击 C2` 的前置。
  2026-11-24 武器化时只搬了 3 个节点，把横扫节点和 C2 前置一起丢了 → 基础 Slash 被 `WeaponService` 当成“不在目录 ⇒ 不锁”，非教学关直接可用；`guide_move_slash` 成孤儿资产。已于 2026-11-25 补回。
- **树里只放招式 / 派生招式**，节点就是招式本身；**不做纯数值升级**。
- 解锁规则只有一条：**前置全部已解锁 + 铜钱够**；分支互不影响，除非显式配了前置。
- 长枪当前 **4 个节点**：`横扫`（根，1，旧节点原价，待填正式价）→ `斩击 C2`（50）；`指向突刺`（根，30）→ `枪突收尾`（40）。
- 横扫的解锁引导 = `guide_move_slash` + `MoveGuideGesture.SwipeHorizontal`（挂在新 `move_spear_slash` 条目上）。

### 池内容事实（2026-11-24 实测）

- **长枪招式 0 个入池**，三个池里没有任何 `AttackSkillConfig`：
  - 普通池 `UpgradePoolConfig`：属性 + 位移/机制；物品池 `ItemPoolConfig`：道具；
  - 装备池 `ActiveSkillPoolConfig`：8 个影之武装（火龙舌 / 染病 / 冰雾 / 箭雨 / 斧击 / 锁链 / 旋风 / 海浪）+ 饰品 + 属性。
- 代码里没有 `unlock_attack` 类升级引用任何招式 ⇒ 招式从来不入三选一池。

### 已知遗留

- `TalentTree_Main`（孤儿树：`talent_ice_thaw` + 隐藏招架节点）未处理，需拍定：并入 Stats 还是删。
- 天赋资源列仍是 `铜钱 → 主动槽 → 被动槽`，会被读成「一条分支锁另一条」；要不要断开待定。
- 其余 4 把武器（双刀 / 弓 / 大刀 / 单手剑）未实装，目录里也没放 → UI 只显示长枪。
- 武器入口是运行时克隆的卡（「天赋」卡左侧一格），美术与正式摆位待后续。
- 清档只清 PlayerPrefs；已加「清档时顺手 `WeaponLoadoutPanel.Hide()`」，避免看到旧画面。

---

## 0.7 下一步（用户指定）：影之武装 / 饰品的**制作与替换**

> ⚠️ **后续口径更新**：本节原建议的「品质 → 等级上限」「同款升级」及独立弃置弹窗不再是当前施工规则；完整规则 / 进度 / P0 待办以紧随其后的 §0.8 和 `plan/endless-demo-equipment-p0-implementation-plan.md` 为准。

### 已经有的（可直接用）

| 能力 | 落点 |
|---|---|
| 精英 / Boss 100% 掉箱 → 到人 → 排队装备三选一 | `EliteDropRules.ShouldDropEquipment` + `EquipmentChestManager.SpawnChest` → `UpgradeChoiceManager.GrantEquipmentChoices(1)` |
| 波末统一弹（与升级三选一同队列） | `UpgradeChoiceManager.deferChoicesToWaveEnd` |
| 候选：影之武装 / 饰品（已加永久解锁过滤） | `UpgradeChoiceManager.CollectActiveSkills` / `CollectAccessories` |
| 主动换装 | `ActiveSkillInventory.Replace(entryId, def)` / `Remove(entryId)` |
| 饰品拆卸 | `UpgradeEffectManager.RemoveUpgrade(def)` + `Assets/Scripts/Core/AccessoryLoadout.cs` |
| 永久解锁 | `EquipmentUnlockService`（`unlockedByDefault` 或存档 `unlockedActiveSkills` / `unlockedAccessories`） |

### 还缺的（按顺序）

1. **满槽替换交互**：选中新装备但槽位满时，用现有 UI 走「换哪一格」；契约照 `IItemDiscardPrompt` + `ItemDiscardResult`（`Assets/Scripts/Data/CombatPresenters.cs`）。
2. **内容制作**：新增影之武装 / 饰品条目（方向见 `design/gameplay-progression-loop.md` §4.1–§4.3）。
3. **品质接上**：`UpgradeDefinition.quality` 字段已存在但零消费者，先定「品质 → 候选权重 / 等级上限」再写资产。
4. **初始解锁名单**：现在靠 `unlockedByDefault = true` 全解锁；上线前改成「一部分初始解锁，其余靠掉落」。

### 注意

- `RemoveUpgrade` **只支持占被动槽的机制型被动**；`Numeric` 机制条目（地刺 / 击退波 / 聚拢波 / 反伤）效果层叠相加、没有可逆减法，不能拆。
- 替换时机应该在**波末三选一**（此时没有技能在施放）。

---

## 0.8 最新交接与待办：双抽池 + 波末换装（本轮规划，未实施）

> **先读** `design/gameplay-progression-loop.md` §3.3–§3.4，再按 `plan/endless-demo-equipment-p0-implementation-plan.md` 执行；当前文档与新计划仅是需求 / 方案，**尚未修改代码、资产或场景**。新会话需以用户当时明确授权为准。

### 用户确认的现状和范围

- 用户确认 **F1 存档解锁、F2 换装 API、F5 天赋解耦、F6 去圈 / 只按击杀计分 / 结算面板、F7 铜钱落盘、武器与招式界面均已完成并验收**；当时编译通过、Console 0 error。F2 是底层 API 完成，不代表玩家换装交互及新变体规则已经闭环。
- 当前第一批只做**现有影之武装（主动）/ 饰品（被动）的获取、配置、入池与波末替换闭环**，并纳入与之共享奖励展示、现由用户明确指定的**经验升级品质 / 全局统一概率 / 同次去重**。不做夺锋、四护符、藤甲兵、新 Boss / 武器或新战斗机制。
- 装备：玩家看品质 / 等级；池计算用用户**另配的抽取档**（类自走棋费用），每个变体独立配置；内部 `Circle` 只改变各档概率，不显示 / 不计分。没有池内个数限制，不合成、不自动升级。
- 装备同一次三选一**不能出同款的不同品质 / 等级**，不保底主动 / 饰品各一；可出 3 个主动或 3 个饰品（以真实可用配置为限）。同款不能同时装备两份；已装备同款时选中直接用新变体换原槽，等级不是 +1；不同款满槽时点击新装备，**下方列出对应类别已装备的装备，点旧装备替换**。
- 经验：品质 / 具体效果可以不同，抽取概率全局统一、不随圈数、不走装备档；同一次三选一不能出现同一升级内容的不同品质。跨多次升级仍按既有本局属性成长语义累积，不占槽 / 不被换装删除；软上限细节不擅定。
- 档位、品质、等级和概率由**用户填写**；无档可抽、配置阶段本就缺货、候选不足等明确报告配置问题，禁止静默改概率、伪造补偿或自行添加保底。**同次去重导致原本有效的某档暂时耗尽**不是简单的配置缺货，剩余候选如何抽取 / 归一化尚待确认；有限候选展示 / 阻塞恢复等异常具体 UX 也未定，见新 P0。
- 地刺 `spike_trap` / 击退波 `push_wave` / 聚拢波 `convergence_wave` / 反伤 `charge_reflect_shield` **暂不入装备池**，保留资产，不删、不伪装成可拆饰品。

### 最新只读核对（执行前需复核）

- `Assets/Scenes/Battle.scene` 当时 Edit Mode、dirty=false；`UpgradeChoiceManager.deferChoicesToWaveEnd=true`、`pauseGameDuringChoice=true`；`Hero_Zhangfei` 主动 / 被动基础各 2；未配置开局默认主动、`startingChoices=0`。
- 当前素材盘点：**10 个主动资产 / 9 个占槽被动资产**。当前池含 8 主动 / 8 占槽被动；主动冲击波、主动烈焰喷射、被动旋风未入原池；按当前要求提供接入能力和清单，但不要自行编定档位 / 概率 / 缺失数值。
- 已清的 12 条不覆盖所有资产：`ActiveSkill_Disease`、`ActiveSkill_FireLine`、`ActiveSkill_IceMist` 仍分别有 `talent_pool_disease/fireline/icemist`；未入池的主动冲击波仍有 `talent_pool_chargewave`。旧 `TalentUpgradeRequirementProvider` 仍检查它们 ⇒ 清档玩家受门槛阻挡；处理前重新检查真实引用与项目范围。
- `ReturnWave` / `ChainBounce` 的对应逐级效果行为空，当前走旧字段回退；不能冒充完整 5 级变体。现有品质 `quality` 为默认值、代码零消费者；旧池权重 0.60 / 0.35 / 0.05 不是新设计的数值决策。
- `StageRunFlow` 波末会等经验宝石，但未显式等飞行中的装备箱 ⇒ 末杀精英箱 / 末波须验证，属于静态风险，不声称已复现。`PassiveSkillRow` 刷新签名不包含装备身份，同等级换装可能显示旧图标；主动替换也需同步 `PlayerState.acquiredUpgrades`。
- 该轮已只读检查 `UpgradeChoiceManager`、`UpgradePoolSampler`、`WaveSpawner`、`StageRunFlow`、`ActiveSkillInventory` / `AccessoryLoadout`、`FakeRouteRuntime.Checkpoint`、品质与等级字段；工作区已有大量未提交改动，禁止回滚或覆盖。

### 当前 Todolist（历史 F1–F8 表以此更新为准）

- [x] F1、F2 API、F5、F6、F7、武器 / 招式简易界面：用户已验收（按用户报告；本轮不重新测试）。
- [x] **P0-A** 已做：分支 / Editor / 场景只读快照；验证全程纯内存 + 只读校验，未写 `player_save`。
- [x] **P0-B** 已做：`kindId` / `visibleLevel`、`EquipmentPoolConfig`、`AttributePoolConfig`、圈数阶段、全局品质权重。
- [x] **P0-C** 已做：`EquipmentChoiceDrawer` / `AttributeChoiceDrawer`，统一抽池、同次按种类去重、逐条配置报错、四项机制排除。
- [x] **P0-D** 已做：`EquipmentLoadout` + 满槽下方替换 UI + 波末等装备箱 + 数据 / 被动刷新同步（未进 Play Mode）。
- [x] **P0-E** 已做：属性全局品质抽取、同次去重、沿用既有逐级累积；接口不含圈数 / 装备档。
- [x] **P0-F** 已做：现有 16 装备 / 14 属性入池、场景接线、Inspector 补字段、校验菜单、完整编译与纯内存验证。
- [ ] **用户填写与验收**：正式装备变体品质 / 等级 / 效果 / 档位及概率，经验升级品质数值 / 全局概率；验证具体配置后再宣称正式池可用。
- [ ] 后续另批：永久解锁的真实掉落发放与初始名单、局外展示、额外新装备、藤甲兵、配置卡随机与节点内容。不要把 `EquipmentUnlockService.Grant` 的存在宣称为掉落发放已接通。

---

## 0.9 本批实施结果（2026-11-25，框架完成 / 正式数值待填）

> 本节记录实际落地结果，§0.8 的待办以本节为准。**未进 Play Mode 实测**；正式数值 / 档位 / 概率 / 分段仍由用户填写。

### 用户拍板的未定规则

1. 同次去重导致某档 / 某品质抽空 ⇒ **视为配置错误，整组作废**（不出残缺候选）。
2. 一个候选都没有（配置缺货 / 去重后全空）⇒ **两类奖励统一报错跳过**；删除属性池的铜钱补偿。

### 当前状态（可直接接续）

- **抽取逻辑：完成**。装备 / 属性两套抽取器通过真实资产 + 纯内存验证，Console 0 error。
- **替换逻辑：代码与数据链路完成**。`EquipmentLoadout` 编排级用例 22 项全过（含 PlayerState 同步、满槽候选、异款替换）。
- **未完成的是验收，不是主逻辑**：未进 Play Mode；正式变体 / 档位 / 概率未填，多品质路径暂无实机可观察数据。
- **回滚分支**（新件装不上 → 回滚旧件）是防御性路径，公开 API 下几乎不可触发，未单独覆盖。

### 已落地

| 项 | 落点 |
|---|---|
| 变体身份 | `UpgradeDefinition.kindId`（空 = upgradeId）+ `visibleLevel`（装备生效等级）+ `KindId` |
| 装备池 | `Assets/ScriptableObjects/Equipment/EquipmentPoolConfig.asset`：16 变体（8 主动 + 8 饰品）、每行独立档位 / 权重、圈数阶段；默认 1 档 / 全圈数占位 |
| 属性池 | `Assets/ScriptableObjects/Upgrades/AttributePoolConfig.asset`：14 属性、全局品质权重（默认 普通 1 / 精良 0 / 传说 0） |
| 装备抽取 | `EquipmentChoiceDrawer`：圈数→档→档内权重；主动 / 饰品统一、不保底、同次按 kindId 去重；配置问题逐条报错 |
| 属性抽取 | `AttributeChoiceDrawer`：全局品质→品质内权重；同次按 kindId 去重；不看圈 / 不用装备档 |
| 换装 | `EquipmentLoadout`：空槽直装 / 同款替换原槽 / 满槽异款返回待选；被动先拆后装且失败回滚 |
| 主动变体 | `ActiveSkillInventory.TryEquipVariant` / `ReplaceVariant` / `ReplaceKindVariant`，同步 `PlayerState.acquiredUpgrades` |
| 被动变体 | `UpgradeEffectManager.ApplyUpgradeAtLevel`（不 +1、按可见等级登记） |
| 满槽 UI | `IEquipmentReplacePrompt` + `UpgradeChoicePopup` + `EquipmentReplaceListView`（候选下方一排旧装备，点即替换） |
| 卡片 | 装备显示「品质 Lv.N + 该级实际数值」；属性显示品质 + 当前→下一级；品质配色 |
| 圈数注入 | Data 层 `IEndlessCircleSource`；`EndlessRunController` 开局注册 / 结束注销 |
| 波末时序 | `StageRunFlow` 在等经验宝石后**再等飞行装备箱**（3 秒超时） |
| 显示刷新 | `PassiveSkillRow` 签名加入变体实例 id（同等级换装不再留旧图标） |
| 配置可见性 | 两个自定义 Inspector 补出 kindId / 品质 / 可见等级 / statId / 初始解锁 / 基础上限 |
| 校验入口 | 菜单「一夫当关/校验奖励池配置（无尽 demo）」逐阶段跑配置检查 |
| 教学工具 | `TutorialSetupTool` 同步从新装备池移除冰雾 |

### 验证（实际执行）

- 完整编译通过，Console **0 error**（仅既有警告）。
- 纯内存用例：抽取 / 去重 / 耗尽 / 未解锁 / 排除机制 / 等级越界 / 重复身份 **21 项通过**；组件级装备 / 替换 / 槽位计数 **20 项通过**；编排级（`EquipmentLoadout` + `PlayerState` 同步 + 满槽候选 + 异款替换）**22 项通过**。
- 真实资产校验：16 装备变体 0 非法；装备 / 属性各抽 3 轮 0 error；未入池清单与 §0.8 一致。
- 未写真实存档：`SaveManager.Load` 只读；全程未调用 `SaveManager.Save`。

### 已知边界 / 待办

- **未进 Play Mode**：换装 UI 版面、暂停期间点击、波末连续多奖励的实机体验待用户验收。
- 未入池（保留资产，由用户决定）：`active_charge_attack_shockwave`、`active_fire_aoe`、`cyclone`（被动旋风）；属性 `stab_range_boost` / `sweep_range_boost`（同为 attack_range，旧池未含）。
- `attack_range_boost` 是**覆盖**语义（`SetAttackRangeBonus`），品质变体之间也是后选覆盖先选；其余 13 条属性为逐级相加。
- 替换列表最多约 5 件同类别装备并排（基础 2 + 天赋扩槽）。
- 保存 Battle.scene 时顺带清掉了 `EndlessRunController` 上已从代码删除的 `n1NodesPerCircle` 等旧字段（代码里已不存在，非功能变更）。
- 正式数值 / 档位 / 概率 / 分段全部待用户填写；在此之前正式池不可宣称可出货。

### 追加修复（2026-11-25）：无尽关攻击手势全部失效

- **不是输入门问题**：实测 `GameplayInputGate` 未拦、`timeScale=1`、`stageState=InProgress`，但 `TutorialAttackGate` 仍处于限制态且白名单为空。
- **根因**：无尽让位后 `RouteFlowHub.Current` 仍是已挂起的假路线，`TutorialStageDirector.IsOnTutorialRoute()` 只看 `RouteConfig` → 误判为教学关 → 白名单启用但无人教学 → `AttackSystem` 丢弃所有招式。
- **修复**：`FakeRouteRuntime.IsSuspended` + `IsOnTutorialRoute` 排除挂起路线 + `EndlessRunController` 开局 / 结束时 `TutorialAttackGate.Clear()`。
- **验证**：修后白名单“不限制”、调试手势成功、`BattleAutoPlayer` 连续清怪并进入下一节点 / 下一波，`timeScale=1`、输入门 open。
- 详情见 `memory/known-issues.md` §28。

### 追加修复②（2026-11-25）：无尽关防御键也被教学状态隐藏

- **用户口径**：防御键只应在**教学关、且还没教到防御**时锁住；无尽 / 正式关必须可用。
- **根因**：`RefreshDefenseButtonAvailability` 自己复制了一份 `RouteConfig == tutorialRouteConfig` 判定，不看路线已被无尽挂起；`_defendTaught=false` → 隐藏。
  同类复制判定还有：防御静音还原 / 强制三选一还原 / `UpdateAttackGate` / 开场台词 / 教学完成 / 教学黑幕。
- **修复**：统一 `IsTutorialSessionActive()`（教学路线且 `!IsSuspended`）；新增 Data 层 `TutorialStatePort`（Flow 不能引用 UI）；新增 `ReleaseTutorialOnlyStateForModeTakeover()` 一次性回收白名单 / 防御键隐藏与静音 / 强制三选一 / 教学输入 / 遮罩 / 教学时停，无尽开局与结束都调用。
- **验证**：无尽开局后防御键 `Available=True / Muted=False`，真实指针按下 → `IsDefendingActive=True / SwingIn`，松开退出；攻击手势与机器人清怪正常。
- 需求 / 审计 / 验收矩阵见 `plan/tutorial-only-state-scope-20261125.md`；症状记录见 `memory/known-issues.md` §29。

### 追加修复③（2026-11-25）：武器化时漏掉「横扫」解锁节点

- **现象**：清档后非教学关仍可直接横扫（Slash）；用户指出“还有对应的解锁教学，这不可能是基础招式”。
- **根因**：旧 `TalentTree_Moves` 有 4 个节点：`指向突刺` / `枪突收尾` / **`横扫`（talent_move_slash）** / **`斩击 C2`（前置 = 横扫）**。2026-11-24 搬到武器树时只搬了 3 个，横扫节点和 C2 前置一起丢失；`WeaponService.FindEntry(Zhangfei_Slash)` 返回 null → `IsMoveUnlocked` 走“配置缺失不锁”的回退 → 非教学关永远可用。`guide_move_slash` 引导台词成了孤儿资产。
- **修复**：
  1. `MainWeapon_ZhangfeiSpear.asset` 补回 `move_spear_slash`（横扫）：`Zhangfei_Slash`、`unlockedByDefault=false`、`cost=1`（旧节点原价，待填正式价）、`moveGuideGesture=SwipeHorizontal`、`moveGuideDialogue=guide_move_slash`、位置 (0,0)；
  2. `斩击 C2` 的前置改为 `move_spear_slash`，位置 (0,190)；
  3. `WeaponLoadoutPanel` 改用 `IsEntryUnlocked(entry)` 判定节点状态（避免 `PlayerState`/目录未就绪时误显示“已解锁”）。
- **验证**：武器面板 4 节点：横扫“花 1 解锁”、斩击 C2“需先解锁：横扫”；`IsMoveUnlocked(Zhangfei_Slash)=False`；无尽关（教学白名单 free）横滑 `SubmitGestureForDebug= False` → 未解锁时打不出横扫。
- 旧 `TalentTree_Moves.asset` 保持空文件（不恢复天赋 UI）；数据只活在武器资产上。
- **✅ 用户已确认修复（2026-11-25）。**

### 测试配置（2026-11-25，临时）

- 场景：`EndlessRunController.debugForceEliteDropsFirstCircle = true` ⇒ **第一圈 N1 / E0 所有敌人** 都按精英处理（100% 掉宝箱）。实现：Data 层 `EquipmentDropTestMode` + `WaveSpawner` 在 `Initialize` 前注入 `enemy.dropsEquipment=true`；`EnemyPool.ReturnEnemy` 回池时恢复预制体默认值。
- 测试变体（`Assets/ScriptableObjects/Equipment/TestVariants/`）：
  - `ActiveSkill_FireLine_Fine`（Fine Lv1）/ `ActiveSkill_FireLine_Legendary`（Legendary Lv2）
  - `TimedFireAOE_Fine`（Fine Lv1）/ `TimedFireAOE_Legendary`（Legendary Lv2）
  - `kindId` 分别指向 `active_fire_line` / `timed_fire_aoe`，`unlockedByDefault=true`。
- 装备池：原 `active_fire_line` / `timed_fire_aoe` 的权重**临时改为 0**；4 个变体 tier1 / weight100（其余 14 个基础装备不变）。
- 验证：N1 7 只敌人全部 `drops=true`；击杀入队装备选择；波末三选一出现测试变体；满槽选旧装备列表可点；同款 Fine→Legendary 精确变 Lv2；Console 0 error。
- **还原**：取消场景勾选；把原两条权重改回 100（或你正式填的值）；删除 `TestVariants/` 与池内 4 行（或保留作测试资产）。
- 本次测试顺手修掉两个替换误判（见 `memory/known-issues.md` §31）：被动内 `AttackPassive`/`TimedPassive` 可互换；同一变体重抽幂等成功。
- **2026-11-25 第二批已实施**：精确排除当前已装备的完全一致变体（同档位耗尽按缺货少出）、装备优先于属性的波末队列、装备掉落时圈数盖章、只等敌人飞行箱、场景破坏物装备入口。验收方案见 `plan/equipment-draw-priority-acceptance-20261125.md`。

### 关键文件（本批新增 / 改动）

- 数据：`Assets/Scripts/Data/EquipmentPoolConfig.cs`、`AttributePoolConfig.cs`、`EquipmentVariantRules.cs`、`IEndlessCircleSource.cs`、`IEquipmentReplacePrompt.cs`、`UpgradeDefinition.cs`
- 抽取：`Assets/Scripts/Core/EquipmentChoiceDrawer.cs`、`AttributeChoiceDrawer.cs`、`RewardDrawReport.cs`
- 换装：`Assets/Scripts/Core/EquipmentLoadout.cs`、`AccessoryLoadout.cs`、`ActiveSkillInventory.cs`、`UpgradeEffectManager.Apply.cs`
- 流程 / UI：`Assets/Scripts/Core/UpgradeChoiceManager.cs`、`Assets/Scripts/UI/UpgradeCard.cs`、`UpgradeChoicePopup.cs`、`EquipmentReplaceListView.cs`、`UpgradeQualityStyle.cs`
- 配置 / 工具：`Assets/ScriptableObjects/Equipment/EquipmentPoolConfig.asset`、`Assets/ScriptableObjects/Upgrades/AttributePoolConfig.asset`、`Assets/Scripts/Editor/RewardPoolValidationMenu.cs`

### 下一步（按顺序）

1. **填正式数据**（用户）：复制现有资产做不同品质 / 等级变体 → 每个变体设 `upgradeId`（唯一）、`kindId`（= 原件 upgradeId）、`quality`、`visibleLevel`，并填对应等级行；再把变体逐行加入 `EquipmentPoolConfig`（档位 / 权重）或 `AttributePoolConfig`（权重）。
2. **配圈数阶段**：`EquipmentPoolConfig.stages` 按 min/max 分段、给每段各档权重；属性在 `AttributePoolConfig.qualityWeights` 填全局品质权重。
3. **跑校验菜单**「一夫当关/校验奖励池配置（无尽 demo）」：0 error 再进 Play Mode。
4. **Play Mode 验收**：精英掉箱 → 波末弹装备 → 空槽直装 / 同款换品质（等级不 +1）/ 满槽下方点旧装备替换 → 下一波正常开始；属性三选一同次不出同款；换饰品后属性不丢。
5. **待拍板**：1–2 张候选是否照实显示（当前照实）；「配置错误中止」是否接受「报错跳过」（当前实现，避免 timeScale=0 卡死）；`attack_range_boost` 覆盖语义是否保留。
6. **后续另批**：初始解锁名单与真实掉落发放、局外装备展示、新影之武装 / 饰品（夺锋 / 四护符）、藤甲兵、配置卡与节点内容。

---

## 1. 四类对象（长期架构，不要改口径）

| 对象 | 获取 | 生命周期 | 进天赋树 | 进三选一 |
|---|---|---|---|---|
| **主武器 + 招式** | Boss / 固定宝箱 | 永久，回局外装备 | ❌ | ❌ |
| **全局成长（属性 + 少量机制）** | 天赋树，铜钱 | 永久 | ✅ **唯一** | ❌ |
| **本局成长属性** | 局内经验升级 | 本局清空 | ❌ | ✅ |
| **影之武装 / 饰品** | 精英 / Boss 装备宝箱 | 本局装备、可替换；**永久解锁资格存盘** | ❌ | ✅（波末） |

来源：`design/gameplay-progression-loop.md` §3.1–§3.4。

**推论（用户已确认）**：

- 天赋树里的 `talent_pool_*`（12 个）与 `talent_move_*`（4 个）**全部移出**。
- 移出 ≠ 删除内容：冲击波、海浪、旋风、染病等仍是可用的影之武装，只是不再由天赋树售卖。
- 天赋树保留槽位节点（巧思 / 博学）。
- ~~除教程教过的内容外，全部锁。~~ **旧推论作废**：当前 demo 按 §0.8，现有影之武装 / 饰品全部视为初始解锁；长线初始名单另定。不要据此锁掉已验收的现有装备。

---

## 2. 工程事实（首版历史快照；现状以 §0.8 为准）

### 2.1 已有

| 能力 | 落点 |
|---|---|
| 精英死亡 → 宝箱飞向玩家 | `EquipmentChestManager.SpawnChest` |
| 宝箱到达 → 排队一次装备三选一 | `OnChestArrived` → `UpgradeChoiceManager.GrantEquipmentChoices(1)` |
| 波末统一弹（与升级三选一同队列） | `deferChoicesToWaveEnd` |
| 影之武装候选 | `ActiveSkillPoolConfig`（common/rare/legendary）+ `CollectActiveSkills` |
| 饰品候选 | `CollectAccessories`（同池的机制型被动） |
| 主动槽位/冷却/激活 | `ActiveSkillInventory`（`AcquireOrUpgrade` / `TryActivate` / `Capacity`） |
| 被动数值结算 | `UpgradeEffectManager`（`PassiveSlotUserCount`） |
| 无尽手写关卡 + 每圈倍率 | `EndlessRunController`（`useAuthoredLevels`、`hpGrowthPerCircle` 等） |

### 2.2 缺失（本轮框架要补）

| 缺口 | 现状证据 |
|---|---|
| **永久解锁** | `SaveData` 只有 `unlockedTalents` / `unlockedManuals`；无 `unlockedActiveSkills` / `unlockedAccessories` |
| **替换 / 换装** | `ActiveSkillInventory.CanAcquire` 槽位满直接 `false`；被动是 `PlayerState.acquiredUpgrades` 裸 `List`，无槽位/移除 API |
| **品质映射** | 只有 `UpgradeRarity`（common/rare/legendary），无“普通/精良/传说 × 成长等级” |
| **天赋解耦** | `RequirementsMet` 仍读 `requiredTalentIds`；Pools 12 节点仍在 `TalentTree_Pools` |
| **去圈数** | `EndlessHud` 显示“第 N 圈”；`EndlessScoreLedger.CircleScore` / `circleBaseBonus` 仍参与计分 |
| **结算面板** | `EndlessHud.OnRunEnded` 只做 3 秒飘字 |
| **击杀铜钱** | `EndlessScoreLedger` 只有分数；`Enemy.coinReward` 只在 `StageRunFlow:361` 结算 |

### 2.3 数据问题（必须清）

4 个升级的 `requiredTalentIds` 指向**不存在的天赋节点**，导致永远进不了池：

- `convergence_wave` → `talent_pool_convergence`
- `push_wave` → `talent_pool_push`
- `arrow_volley` → `talent_pool_arrowvolley`
- `return_wave` → `talent_pool_return`

用户已定：**清掉引用**，后续再写新的解锁顺序。

---

## 3. 关键约束（避免临时设计）

> 用户原话：“**不要为了做这个 demo 而做太多临时设计。**”

| 不要 | 要 |
|---|---|
| 用 `PlayerPrefs` 临时 key 存解锁 | 走 `SaveData` 正式字段 + `SaveSection` 读写 + 旧档迁移 |
| 在 UI 里直接改 `acquiredUpgrades` 实现换装 | 走 `AccessoryLoadout` 正式槽位 API |
| 把玩家品质 / 等级直接当作装备池档 | **池内抽取档独立配置**；现有品质枚举可评估复用，禁止新造冲突的平行品质口径 |
| 把 Pools 节点 `hidden=1` 藏起来就算移除 | 从资产里删节点并清 `requiredTalentIds` |
| 在 HUD 里硬编码隐藏“第 N 圈” | 改 `EndlessScoreLedger` / `EndlessHud` 的数据源 |
| 用减伤+染色冒充藤甲兵 | 按 `enemy-variants.md` §1：霸体 + 灼烧易伤 + 永久灼烧 |
| `Random.value` 抽配置卡 | `seed + 节点 id` 派生，同种子可复现 |
| 给 demo 单独写一套战斗流程 | 复用 `EndlessRunController` + `FakeRouteRuntime` 现有链路 |

---

## 4. Todolist（首版历史记录；当前待办见 §0.8）

### F1 存档：永久解锁

- `SaveData` 增 `unlockedActiveSkills` / `unlockedAccessories`（`List<string>`，存 `upgradeId`）。
- 新增 `EquipmentUnlockSaveSection`（照 `PropSaveSection` 的写法），提供 `IsUnlocked` / `Unlock` / `Clear`。
- 旧档迁移：字段为空即视为“初始解锁名单”为空（不报错）。
- **DoD**：解锁后重进游戏仍为已解锁；清档归零。

### F2 装备槽 + 替换

- 主动：`ActiveSkillInventory.Replace(int entryId, ActiveSkillDefinition def)`，满槽时替换指定槽位。
- 被动：新增 `AccessoryLoadout`（槽位数来自 `TalentCapabilities`，装备/替换/移除 + 触发 `UpgradeEffectManager` 重算）。
- **DoD**：槽位满时能把 A 换成 B，且数值/图标/触发同步更新，无残留。

### F3 宝箱 → 三选一 → 装备

- 候选过滤：`CollectActiveSkills` / `CollectAccessories` 增加“已永久解锁”条件。
- 确认后：未满槽 → 装备；满槽 → 弹替换选择（用现有三选一 UI，不新做界面）。
- **DoD**：精英击杀 → 波末三选一 → 选中即装备或替换；未解锁的不会出现。

### F4 品质与候选规则

- 明确 `UpgradeRarity` 与“普通 / 精良 / 传说 + 成长等级”的映射（写进 `design/`，不只在代码里）。
- **DoD**：同一件装备的品质与等级可配置，候选抽取按品质权重。

### F5 天赋树解耦

- 移除 `TalentTree_Pools`（12）与 `TalentTree_Moves`（4）；合并为单树（`Stats` 保留）。
- 清 4 个死引用（§2.3）。
- `RequirementsMet` 不再依赖被移除的节点。
- 去掉三页分页 UI（`TalentTreePanel`）。
- **DoD**：天赋页只剩全局成长节点；影之武装/饰品不再需要天赋解锁。

### F6 玩家可见层

- HUD 去掉“第 N 圈”（数据源同步改，不要只在 UI 隐藏）。
- 结算面板：本局总分 + 历史最高分；阵亡与提前退出共用；面板前短淡入（0.2–0.35 s）。
- **DoD**：结算只显示分数；无圈数、无铜钱。

### F7 经济

- 无尽击杀 `PlayerState.AddCoins(enemy.coinReward * TalentCapabilities.CoinGainMultiplier)`（位置：`EndlessRunController` 的 `Ledger.AddKill` 旁）。
- **DoD**：击杀后铜钱增加，数值与剧情关一致。

### F8 内容（框架完成后）

- **藤甲兵**：按 `design/enemy-variants.md` §1（霸体开关、`EnemyStatusBurn.Tick` 内易伤、永久灼烧）；prefab 必须加入 `EnemyPool.enemyPrefabList`。
- **配置卡随机抽取**：`TryGetNodeBattleConfig` 从节点全部非空 `battleEntries` 随机抽一张（`seed + 节点 id` 派生，单次战斗固定）。
- **节点内容**：由用户配多张 `StageConfig`，不要程序造。

---

## 5. 将来的设计（长期，供后续对话理解上下文）

- **影之武装**：本局主动战斗手段，可围绕它建 Build；本局可成长与更替；本局结束不保留。
- **饰品**：本局装备效果，可插拔替换；换掉不影响已获得的成长属性；初始 2 槽。
- **品质**：普通 / 精良 / 传说；允许部分内容只有单一品质。
- **获取节奏**：约 3 分钟应有一次能选到影之武装的机会（波末）；不开局白送 Build。
- **永久解锁**：一部分初始解锁，其余靠精英/Boss/探索宝箱；解锁后立刻可在本局后续宝箱出现，但不保证立刻抽到。
- **暗源**：靠进攻获取、防御姿态不回复（具体数值待定）。
- **主武器**：双刀（Boss「双刃刺客」）、单手剑（Boss「守剑校尉」）、弓（固定宝箱，不做 Boss）；大刀沿用 104。
  详见 `design/main-weapon-acquisition-and-bosses.md`。

---

## 6. 待定（后续讨论，不要提前实现）

1. 宝箱频率、初始解锁名单。
2. 新增影之武装 / 饰品内容。
3. 每件装备的品质、成长等级、具体数值。
4. 主武器 Boss 的阶段细节与数值。

---

## 7. 文档索引

| 文档 | 用途 |
|---|---|
| `plan/endless-demo-equipment-p0-implementation-plan.md` | **最新 P0 执行方案**：装备变体抽取、波末换装、经验品质 / 全局概率与验收 |
| `design/gameplay-progression-loop.md` | 玩法权威（四类对象、获取、Build 方向） |
| `design/enemy-variants.md` | 藤甲兵 / 自爆兵设计 |
| `design/main-weapon-acquisition-and-bosses.md` | 主武器获取与 Boss |
| `plan/endless-demo-requirements-20261124.md` | 需求、节点实测数据、F1–F8 详表 |
| `design/endless-mode-design.md` §2.4 | 无尽地面/路面连续性（已验收） |
| `design/in-run-growth-and-unlocks.md` | 旧被动与属性分流的历史分析 |
