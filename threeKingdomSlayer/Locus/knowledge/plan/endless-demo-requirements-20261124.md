---
id: kd_ec40b5f0-bb4c-4434-9499-1d71fa08e6c5
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 无尽关 20–30 分钟 demo：需求与待办（2026-11-24）

> **最新交接**：本文件保留 2026-11-24 首版需求与历史数据，不能直接当作现况或执行清单。请先读 `plan/endless-demo-handoff-20261124.md` §0.8、`plan/endless-demo-equipment-p0-implementation-plan.md` 与 `design/gameplay-progression-loop.md` §3.3–§3.4。
> 用户已确认 F1、F2 API、F5、F6、F7 和武器 / 招式界面已验收；本文“未改代码”“无尽不产钱”“无永久解锁 / 天赋解耦”等是旧状态。无尽铜钱已有击杀链，本局结束结算，不要重复加钱。
> 两套奖励口径已更新：装备变体有独立池内档位、内部圈数改变档位概率、同款直接替换；经验升级品质概率全局统一、不同圈不升级池；两者同次均按内容种类去重。现有装备、排除四项、波末下方选槽按新 P0 计划执行；新装备内容另批。
>
> 目标：用**已有内容**做出一个可玩 20–30 分钟的无尽关小 demo，验证「天赋解锁 + 局内三选一 + 循环推进」的闭环。
> 下文“本轮只梳理、未改代码/资产”等均为首版书写时的范围说明，不代表后续批次未实施。

---

## 0. 首版结论（历史快照；当前状态以文首最新交接为准）

| 项 | 结论 |
|---|---|
| 内容是否够 | 够。一圈左路 124 只 / 右路 68 只，2 圈即可 20–30 分钟 |
| 最大缺口 | 无尽模式**不产铜钱** → 天赋买不了 |
| 天赋树 | 改回**传统单树**：属性 + 机制解锁并存；**招式节点移出**（跟武器走） |
| 新做 UI | 仅 1 个：**局结算 panel + 短过场**（阵亡/提前退出共用） |
| 新做敌人 | 仅 1 个：**藤甲兵**（1011 染色 + 数值变体） |

---

## 1. 节点内容现状（实测 2026-11-24）

解析 `Assets/RouteData/ChangbanpoScroll/Battles/Formal_*.asset`：

| 节点 | 波数 | 敌人总数 | 构成 | 倍数（hp/atk/dmg） | Boss |
|---|---|---|---|---|---|
| N1 | 1 | 7 | 1011×1、1013×1、1012×5 | 1/1/1 | 无 |
| E0 | 5 | 49 | 1011×8、1012×28、1051×14、109×3、1021×2 | 全 1 | 无 |
| N2（右） | 3 | 12 | 1011×6、1021×2、105×1、109×1、104×1 | 4.5 / 2.9 / 3.5 | 104 |
| N3（左） | 5 | 68 | 1011×10、1012×32、106×11、105×9、102×1、109×2、1021×4、107×1 | 全 1 | 107 |

要点：

- 一圈实际：**左路 124 只 / 右路 68 只**。
- 最大压力点：**N3 第 3 波 30 只（1012×25）**。
- **108 从未出场**；102 只在 N3 出现 1 只。
- 所有 `waveTintColor` 都是白色（未使用染色）。

---

## 2. 节点内容目标（待用户调）

> **“圈”只是内部循环单位，不是玩家可见概念**（2026-11-24 用户定：“不存在圈数这个概念，圈数也是个错误需求”）。
> 下文用它描述内容复用周期；**HUD / 结算面板不得显示圈数**。

| 节点 | 定位 | 建议 | 目标时长 |
|---|---|---|---|
| N1 | 开场热身 | 1–2 波，纯近战，8–14 只，无远程 | 40–60 秒 |
| E0 | 主战场 | 5 波；第 4 波进 109 骑兵、第 5 波进 1021 盾 + 1051 弓 | 3–4 分钟 |
| N2（右） | 官道 + Boss | 2 波小怪 + 1 Boss；Boss 按圈轮换 104→107→108 | 3–4 分钟 |
| N3（左） | 村庄远程压力 + Boss | 4 波（106/105 远程 + 1021 盾）+ 1 Boss | 4–5 分钟 |

时长推导（不是先定圈数）：

```
右路一圈 ≈ 1 + 3.5 + 3.5 + 1.5(旅行/三选一) ≈ 9–10 分钟
左路一圈 ≈ 1 + 3.5 + 4.5 + 1.5              ≈ 10–11 分钟
20–30 分钟 ⇒ 2 圈（左右各一，2 个 Boss）
```

### 2.1 配置卡池 + 随机抽取（2026-11-24 用户定）

> 用户原话：“本质是我来配，但是随机从多个配置卡中抽取，如果只有一个那就是固定的，配了多个那就是随机的。”
> **配置由用户手写，不由程序生成。**

- **数据结构已支持**：`FakeRouteNodeConfig.battleEntries` 是 `List<FakeRouteBattleEntry>`，
  每条 entry 有 `StageConfig battleConfig` ⇒ 一个节点可以挂多张卡。
- **现状**：`EndlessRunController.TryGetNodeBattleConfig` 只取**第一个非空** battleConfig。
- **要改的**：进入节点时从所有非空卡里**随机抽一张**（同一次战斗内固定，不能每帧变）；只有 1 张时退化为固定。
- **随机源**：用 `EndlessRunController.seed` + 圈号 + 节点 id 派生，保证同种子可复现。
- **配置方式**：在节点资产 `Node_N1/E0/N2/N3.asset` 的 `battleEntries` 里加多条，每条拖一个 `StageConfig`。
- **不做**：不改 `FakeRouteRuntime` 的剧情关行为（它按 `battleEntries` 顺序逐条打）。

---

## 3. 天赋树：只装「全局成长」

> 2026-11-24 用户澄清：“这玩意儿怎么还会在天赋树里面呢？……我没说那就是不入池也不显示。”
> 结合 `design/gameplay-progression-loop.md` 的四类对象：天赋树**不是**技能/装备解锁表。

### 3.1 四类对象与归属

| 对象 | 获取方式 | 是否进天赋树 |
|---|---|---|
| 主武器及招式 | Boss / 固定宝箱，回局外装备 | ❌ |
| **全局成长（属性 + 少量机制）** | **天赋树，铜钱购买** | ✅ |
| 本局成长属性 | 局内经验升级 | ❌ |
| 影之武装 / 饰品 | 精英/Boss 装备宝箱 + 永久解锁 | ❌ |

⇒ 现有 `TalentTree_Pools` 的 **12 个 `talent_pool_*` 节点全部移出天赋树**——它们属于影之武装解锁，应走掉落/永久解锁体系。
⇒ 现有 `TalentTree_Moves` 的 **4 个招式节点**移出——跟主武器。
⇒ **移出不等于删除内容**：冲击波、海浪、旋风、染病等仍然是可用的影之武装，只是不再由天赋树售卖。

### 3.2 保留节点（全局成长）

| 线 | 节点 | 效果 | 现状价格 |
|---|---|---|---|
| 属性 | `talent_stat_hp1/2/3` | max_health +30 ×3（链） | 1（测试值） |
| 属性 | `talent_stat_combo` | combo_threshold_delta −5 | 1（测试值） |
| 属性 | `talent_stat_coin` | coin_gain +0.3 | 120 |
| 属性 | `talent_stat_slot` | item_slot +1 | 1（测试值） |
| 属性 | `talent_stat_slot_active` | active_slot +1 | 180 |
| 属性 | `talent_stat_slot_passive` | passive_slot +1 | 180 |
| 机制 | `talent_break_formation` | attack_poise_damage +1 | 200 |
| 机制 | `talent_pursuit` | kill_cooldown_reduction +0.5 | 250 |
| 机制 | `talent_strategy` | choice_count +1 | 300 |
| 机制 | `talent_stat_revive` | revive_count +1 | 1（测试值） |
| 机制 | `talent_undying` | undying +1 | 400 |

> `talent_stat_slot_active/passive`（影之武装/饰品槽位）是否保留在树里，取决于“槽位成长属于局外全局成长”的口径；
> `gameplay-progression-loop.md` §3.4 写“后续槽位成长属于局外全局成长的讨论范围”，**暂保留**。

### 3.3 移除节点

- `talent_pool_*` × 12（冲击波/铁壁/海浪/旋风/专注/染病/蛇形/火龙舌/地刺/冰雾/疾风/震荡）
- `talent_move_*` × 4（指向突刺/枪突收尾/横扫/斩击C2）

### 3.4 数据清理

4 个升级的 `requiredTalentIds` 指向**不存在的天赋节点**，且随 Pools 一并清空：
**2026-11-24 用户定：清掉这些引用，后续再写新的天赋解锁顺序。**

| 升级 | 引用节点 | 处置 |
|---|---|---|
| `convergence_wave` 聚拢 | `talent_pool_convergence` | 清引用 |
| `push_wave` 击退 | `talent_pool_push` | 清引用 |
| `arrow_volley` 箭矢 | `talent_pool_arrowvolley` | 清引用 |
| `return_wave` 回旋 | `talent_pool_return` | 清引用 |

### 3.5 预解锁口径（用户 2026-11-24 定）

- **除教程教过的内容外，全部锁**。
- 教学关教过的招式/技能：待核对教学导演实际用了哪些（`TutorialStageDirector`），只预解锁这些。
- 影之武装/饰品的“初始解锁一部分、其余靠精英/Boss/探索宝箱”按 `gameplay-progression-loop.md` §3.4 执行，与天赋树无关。

---

## 4. 影之武装 / 饰品：新的刷取掉落（首版历史描述）

> ⚠️ 下表“三选一候选可同时包含成长属性与装备”已过时：经验只抽属性，装备箱抽影之武装 / 饰品，两类分开但共享波末队列；装备池独立档与升级全局品质规则见新 P0。

> 权威文档：`design/gameplay-progression-loop.md` §3.4 / §4.1–§4.3。
> 2026-11-24 用户：“影之武装和饰品的系统改动还是要做的，要用新的刷取掉落逻辑。”

| 项 | 现状 | 目标 |
|---|---|---|
| 获取 | 局内三选一池 + 天赋节点解锁（`talent_pool_*`） | **精英 / Boss 的装备宝箱**；一部分初始解锁，其余靠击败精英/Boss/探索宝箱**永久解锁** |
| 槽位 | 主动 2 / 被动 2 | 初始各 2，后续由局外成长扩展 |
| 品质 | 无 | 普通 / 精良 / 传说 |
| 三选一 | 只出升级 | 候选可同时包含成长属性与影之武装/饰品 |
| 节奏 | 无约定 | **约 3 分钟**提供一次能选到影之武装的机会（波末选择） |

现有资产映射（供改造参考）：

- 主动类：`ActiveSkills/` 9 个（ArrowRain、AxeSmash、ChainPull、ChargeAttackShockwave、Cyclone、Disease、FireAOE、FireLine、IceMist、Wave）
- 被动/计时类：`Upgrades/Definitions/` 中 `AttackPassive` 6 个、`TimedPassive` 5 个
- 属性类：`Numeric` 22 个（`statId` 非空 = 属性；空 = 机制）

> 具体刷取规则、品质与成长等级对应、宝箱频率以 `gameplay-progression-loop.md` 为准；本文只记 demo 的最小实现范围。
> ⚠ 现有 **7 项**池（common 6 + rare 1）和桶概率 0.60/0.35/0.05 属于旧体系，改造时一并重定。

---

## 5. 经济：击杀铜钱 + 结算

### 5.1 击杀铜钱（首版错误分析，已由 handoff §0.8 纠正）

> **不要按本节加钱**：现有 `StageRunFlow.OnEnemyDied` 已结算击杀铜钱，补上的缺口是无尽结束时的落盘；再次在无尽击杀回调加钱会双倍。

- 位置：`EndlessRunController` 击杀回调（`Ledger.AddKill(enemy.enemyId)` 旁）。
- 做法：`PlayerState.Instance?.AddCoins(Mathf.RoundToInt(enemy.coinReward * TalentCapabilities.CoinGainMultiplier))`。
- 现状证据：`EndlessScoreLedger` 只有分数；`Enemy.coinReward=10` 只在 `StageRunFlow:361` 结算；`KillRewardManager` 在无尽里只 `ResetRewards()`。

### 5.2 结算 panel + 过场（新，唯一新增 UI）

- **触发**：阵亡、玩家提前退出，两种走同一个面板。
- **内容（2026-11-24 用户定）**：**只显示分数**，不显示铜钱。
  - 本局总分
  - 历史最高分
  - **不显示圈数** —— 用户明确：“不存在圈数这个概念，圈数也是个错误需求”。
- **过场**：面板出现前短淡入（0.2–0.35 秒），确认后回落地页。
- **现状**：`EndlessHud.OnRunEnded` 只做 3 秒飘字，无面板。

---

## 6. 藤甲兵（唯一新敌人）

> **权威文档：`design/enemy-variants.md` §1**（方向已确认）。本节只记 demo 落地范围。
> ⚠ 不是我上一版写的“高减伤 + 慢速”：正式设计是 **全场霸体 + 灼烧易伤 + 灼烧不退**。

| 项 | 规则 |
|---|---|
| 基底 | 101 剑士 / 102 盾兵 的变体 |
| 核心 | **全场霸体**（轻攻击打不断前摇）+ **灼烧易伤** + **灼烧不退** |
| 对策一 | 火系影之武装（火龙舌 / 烈焰喷射 / 蛇形喷射） |
| 对策二 | 重攻击拆前摇、削韧破势（必须保留，否则没抽到火就卡死） |
| 视觉 | 暂用原形象 + 渲染（霸体描边色复用）；后续替换 |

### 6.1 三个技术落点（已核对代码，见目标文档 §1.1）

1. **霸体**：`isSuperArmor` 目前只有 BOSS 有赋值路径（来自 `BossPhaseData`），普通敌人恒为 false
   ⇒ 预制体需加开关，并接入 `inArmorWindow` 判定。
2. **灼烧易伤**：没有 `DamageType.Fire`，火就是灼烧状态；灼烧每跳走 `ignoreDamageModifiers: true`
   ⇒ 倍率必须加在 `EnemyStatusBurn.Tick` 内部（敌人侧新字段）。
3. **灼烧不退**：`_ticksRemaining` 归零即结束 ⇒ 需要“永久”标记。

### 6.2 demo 范围

- 做：霸体开关 + 灼烧易伤字段 + 永久灼烧标记；1011 复制一个藤甲兵 prefab。
- 不做：火弱点抗性矩阵、独立美术。
- 注册：新 prefab **必须**加进 Battle 场景 `EnemyPool.enemyPrefabList`，否则退化成方块。

---

## 7. Todo（首版历史清单，已由新 P0 取代）

> **最新待办**：`plan/endless-demo-handoff-20261124.md` §0.8；实施步骤和验收：`plan/endless-demo-equipment-p0-implementation-plan.md`。下面复选框只记录最初计划，不代表当下状态。

> 用户口径：“完成无尽关 demo 先要改影之武装和饰品的掉落和替换逻辑……先改整体框架，再新增内容。”
> 框架步骤 F1–F7 详见 §10；F8 才是内容。
> **交接入口**：`plan/endless-demo-handoff-20261124.md`（含工程事实、禁止临时设计清单、将来的设计）。

- [ ] **F1 存档：永久解锁**：`SaveData` 增 `unlockedActiveSkills` / `unlockedAccessories`；`SaveManager` 读写。
- [ ] **F2 装备槽 + 替换**：主动 `ActiveSkillInventory.Replace`；被动新增 `AccessoryLoadout`（槽位/装备/替换/重算）。
- [ ] **F3 宝箱 → 三选一 → 装备**：候选过滤加永久解锁；确认后装备或替换；槽位满时弹替换选择。
- [ ] **F4 品质与候选规则**：`UpgradeRarity` 与“普通/精良/传说 × 成长等级”的映射。
- [ ] **F5 天赋树解耦**：移除 `TalentTree_Pools`（12）+ `TalentTree_Moves`（4）；清 `requiredTalentIds`；`RequirementsMet` 不再依赖。
- [ ] **F6 玩家可见层**：HUD 去“第 N 圈”；结算面板（总分 + 最高分）。
- [ ] **F7 经济**：无尽击杀 `PlayerState.AddCoins`（§5.1）。
- [ ] **F8 内容**（框架后）：藤甲兵（§6）、配置卡随机抽取（§2.1）、节点内容（用户配）。

---

## 8. 已定 / 后续

**已定（2026-11-24）**

- 天赋树**保留槽位节点**（巧思 / 博学）。
- 除教程内容外全锁。
- 结算只显示总分 + 最高分，不显示铜钱 / 圈数。

**后续（不属于本轮框架）**

- 影之武装 / 饰品的宝箱频率、初始解锁名单（用户：“后续才要定的内容”）。
- 新增影之武装 / 饰品内容（用户：“后续再讨论”）。

---

## 9. 不做

- 不做程序生成编队（保持 `useAuthoredLevels=true`）
- 不做新 Boss / 新招式
- 不做火弱点抗性矩阵（藤甲兵只做霸体 + 灼烧易伤）
- 不改物理、摄像机、输入
- 主武器的获取与招式实装属于另一条线（见 `design/main-weapon-acquisition-and-bosses.md`）

---

## 10. 整体框架改造（首版历史方案，勿按旧口径执行）

> 这里的旧“现状 / DoD / 品质映射”已过时；以 `plan/endless-demo-handoff-20261124.md` §0.8 和 `plan/endless-demo-equipment-p0-implementation-plan.md` 为准。

> 用户 2026-11-24：“完成无尽关 demo 先要改影之武装和饰品的**掉落和替换逻辑**，也就是玩法机制的修改；
> 根据上线版本的文档，先改整体框架，再新增内容。”
> 权威玩法：`design/gameplay-progression-loop.md` §3.4。

### 10.1 现状（已核对代码）

| 能力 | 现状 | 落点 |
|---|---|---|
| 精英掉落 → 宝箱 → 波末装备三选一 | ✅ **已有**（2026-11-17 改过） | `EquipmentChestManager.SpawnChest` → `OnChestArrived` → `UpgradeChoiceManager.GrantEquipmentChoices(1)` |
| 影之武装候选 | ✅ 有池 | `ActiveSkillPoolConfig`（common/rare/legendary）+ `CollectActiveSkills` |
| 饰品候选 | ✅ 有收集 | `CollectAccessories`（同池的机制型被动） |
| **永久解锁** | ❌ 无 | `SaveData` 只有 `unlockedTalents` / `unlockedManuals` |
| **替换 / 换装** | ❌ 无 | `ActiveSkillInventory.CanAcquire` 满槽直接 false；被动是 `PlayerState.acquiredUpgrades` 裸 List |
| **品质** | ⚠ 只有稀有度 | `UpgradeRarity`（common/rare/legendary），无“普通/精良/传说 × 成长等级”映射 |
| **天赋解耦** | ❌ 未做 | `RequirementsMet` 仍看 `requiredTalentIds`；Pools 12 节点仍在树里 |

### 10.2 改造步骤

| 步 | 目标 | 主要落点 |
|---|---|---|
| **F1** | 存档：永久解锁 | `SaveData` 增 `unlockedActiveSkills` / `unlockedAccessories`；`SaveManager` 读写 |
| **F2** | 装备槽 + 替换 | 主动 `ActiveSkillInventory.Replace`；被动新增 `AccessoryLoadout`（槽位/装备/替换/重算 `UpgradeEffectManager`） |
| **F3** | 宝箱 → 三选一 → 装备 | 候选过滤加“已永久解锁”；确认后装备或替换；槽位满时弹替换选择 |
| **F4** | 品质与候选规则 | 明确稀有度与“普通/精良/传说 + 成长等级”的映射；宝箱候选按品质抽 |
| **F5** | 天赋树解耦 | 移除 Pools 12 + Moves 4；清 `requiredTalentIds`；`RequirementsMet` 不再依赖 |
| **F6** | 玩家可见层 | HUD 去“第 N 圈”；结算面板（总分 + 最高分） |
| **F7** | 经济 | 无尽击杀 `PlayerState.AddCoins` |
| **F8** | 内容 | 藤甲兵、配置卡随机抽取、节点内容（用户配） |

### 10.3 边界

- **不新增影之武装/饰品内容**（用户：“后续再讨论”）。
- 不改主武器获取与招式实装（另一条线）。
- 宝箱频率与初始解锁名单属于内容层，**后续才定**；框架只预留接口。
