---
id: kd_b30bbb29-64fa-4982-8f1a-5044011a151e
injectMode: inherit
summary: 非Boss战斗体验丰富化分析文档：系统关联全景图、已发现断裂点、三层改进方案、优先级排序，供后续开发决策参考。
aiEditMode: inherit
---

# 非Boss战斗体验丰富化分析

## 概述

本文档整合了对当前非Boss战斗系统的完整分析，包括：系统关联全景、已发现的关键断裂点、以及按投入产出比排序的三层改进方案。供后续开发时读取判断方向。

---

## 一、当前战斗循环

```
玩家攻击 → 击杀敌人 → 获得EXP → 升级 → 三选一弹窗（暂停）→ 选奖励 → 继续战斗
                ↓
           连击计数 → 10连击 → Buff (+50% ATK, 但无效⚠️)
                ↓
           Boss死亡 → 道具三选一（暂停）
```

**实际可用内容极其有限：**
- 升级三选一：只出2个选项（DamagePlus + AttackSpeed），选满10级后弹窗变空
- 连击Buff：10连击触发但**数值不生效**（`IStatModifierApplier` 从未注册）
- 道具三选一：Boss死后弹1个选项（Wave），点完即无
- 被动攻击（幻影武器/回旋波/连锁弹射/烈焰/箭雨）：代码完备但**不在池子里**
- 敌人类型：5种非Boss已定义，Stage只用了2种（101持剑杂兵 + 102盾兵）

---

## 二、系统关联全景图

```
                    ┌──────────────────────────────────────┐
                    │           PlayerState                │
                    │  AddExp() → OnLevelUp → 三选一弹窗    │
                    └──────────┬───────────────────────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
    ┌─────────────┐  ┌──────────────┐  ┌─────────────────┐
    │UpgradeEffect│  │PassiveTrigger│  │TimedPassive     │
    │Manager      │  │Module        │  │Module           │
    │             │  │              │  │                 │
    │数值倍率     │  │每N次攻击触发 │  │每N秒自动触发    │
    │damage/AS/MS │  │Phantom/Return│  │FireAOE/Arrow    │
    │range/push   │  │Wave/Chain    │  │                 │
    └──────┬──────┘  └──────┬───────┘  └────────┬────────┘
           │                │                    │
           ▼                ▼                    ▼
    ┌──────────────────────────────────────────────────┐
    │              AttackSystem                        │
    │  GetFinalDamage() ← 数值倍率                     │
    │  ExecutePhantomAttack() ← 被动攻击               │
    │  ExecuteReturnWave()                             │
    │  ExecuteChainBounce()                            │
    │  OnAttackPerformed → 驱动 PassiveTriggerModule   │
    └──────────────────┬───────────────────────────────┘
                       │
         ┌─────────────┼─────────────┐
         ▼             ▼             ▼
    ┌─────────┐  ┌──────────┐  ┌──────────┐
    │ Enemy   │  │ComboMgr  │  │BuffMgr   │
    │TakeDmg  │──│计数+触发  │──│AddBuff   │
    │Die→EXP  │  │          │  │(无人消费)│
    └─────────┘  └──────────┘  └──────────┘
```

### 关键数据流

| 触发链 | 路径 |
|--------|------|
| 升级→三选一 | `PlayerState.AddExp()` → `OnLevelUp` → `UpgradeChoiceManager.OnPlayerLevelUp()` → 暂停+弹窗 |
| 选择→数值生效 | `UpgradeEffectManager.ApplyUpgrade()` → 数值倍率累加 → `AttackSystem.GetFinalDamage()` 查询 |
| 选择→被动攻击 | `UpgradeEffectManager` → `PassiveTriggerModule.Register()` → 监听 `AttackSystem.OnAttackPerformed` → 计数触发 |
| 选择→计时被动 | `UpgradeEffectManager` → `TimedPassiveModule.Register()` → `Update()` 计时触发 |
| 选择→道具 | `UpgradeEffectManager` → `ItemInventory.AddItem()` → UI按钮点击 → `WhirlwindController` / `ExecuteLightning` |
| 击杀→EXP | `EnemyManager` → `PlayerState.AddExp()` |
| 受击→连击 | `Enemy.OnDamageTaken` → `ComboManager.OnEnemyDamaged()` → `BuffManager.AddBuff()` |

---

## 三、已发现的关键断裂点

### 3.1 Combo Buff 数值不生效（P0）

**位置**：`BuffManager.cs`

**问题**：`IStatModifierApplier` 从未被注册。`combo_atk_10` Buff 创建后，`BuffManager.Update()` 调用 `ApplyModifierToApplier()`，但 `_appliers.TryGetValue("atk", ...)` 永远返回 false。

**影响**：连击系统形同虚设——10连击触发但攻击力不变。

**修复方向**：在 `AttackSystem.Start()` 或 `UpgradeEffectManager.Start()` 中注册一个 `IStatModifierApplier`，将 `StatModifier` 的 `Multiply`/`Add` 值注入 `GetFinalDamage()` 计算链。

### 3.2 IEffectExecutor 注册表为空（P1）

**位置**：`UpgradeEffectManager.cs`

**问题**：`_executors` Dictionary 从未被填充。`on_attack_trigger`、`on_kill_chance`、`unlock_attack` 三种 effectType 没有对应实现。

**影响**：`design/in-game-growth-system.md` 中设计的机制修饰型、资源经济型、攻击解锁型奖励全部无法实现。

**修复方向**：为每种 effectType 实现 `IEffectExecutor` 并注册。

### 3.3 FireAOE/Arrow 绕过 AttackSystem（P1）

**位置**：`TimedPassiveModule.cs` → `ShootFireEffect.Play()` / `TimedArrowEffect.Play()`

**问题**：直接调用 prefab 的 `.Play()` 方法，传入原始 `cfg.damage`，不经过 `AttackSystem.GetFinalDamage()`。

**影响**：烈焰喷射和箭雨的伤害不吃 `UpgradeEffectManager` 的 `damage_multiplier`。玩家选 DamagePlus 后这两种被动攻击伤害不变。

**修复方向**：让这两种效果走 `AttackSystem` 的伤害计算管线，或至少在调用前查询 `UpgradeEffectManager.GetDamageMultiplier()`。

### 3.4 三选一池子几乎为空（P0）

**位置**：`Assets/ScriptableObjects/Upgrades/UpgradePoolConfig.asset`、`ItemPoolConfig.asset`

**问题**：
- `UpgradePoolConfig`：commonPool 仅 2 个（DamagePlus、AttackSpeed），rarePool / legendaryPool 为空
- `ItemPoolConfig`：commonPool 仅 1 个（Wave），rarePool / legendaryPool 为空

已定义的 16 个 `UpgradeDefinition` 中 14 个未入池。

**影响**：升级选项从第3级开始出现重复或空选项。

**修复方向**：将已定义 asset 按稀有度拖入对应池子。

### 3.5 道具手势未被识别（P2）

**位置**：`InputManager.cs`

**问题**：`UpgradeDefinition.gestureId` 支持 `circle` 和 `long_press_swipe_down`，但 `InputManager.ProcessGesture()` 不识别这些手势。道具激活全靠 UI 按钮点击（`BuffDisplayPanel.OnItemIconClicked`）。

**影响**：道具使用时没有"操作感"，只是点按钮。

**修复方向**：在 `ProcessGesture()` 中增加画圈检测和长按下滑检测，调用 `ItemInventory.TryConsume()`。

### 3.6 Boss C技权重路由无效（P2）

**位置**：`Enemy.SelectBossAction()`

**问题**：`normalAttackWeight` 和 `cAttackWeight` 两个分支都调用 `StartAttacking()`，无差异化。C技行为依赖 `attackSequence` 中的 `AttackStep.isCAttack` 字段，而非权重选择。

**影响**：Boss 阶段配置中的 C技权重不起作用。

**修复方向**：为 C技分支选择独立的攻击序列或设置标志位。

---

## 四、三层改进方案

### 第一层：修复断裂点（投入最小，收益最大）

> 代码已写完，只是没有正确接上。

| # | 任务 | 改动范围 | 优先级 |
|---|------|---------|--------|
| 1.1 | 注册 `IStatModifierApplier("atk")`，让 Combo Buff 生效 | `AttackSystem.cs` 或 `UpgradeEffectManager.cs` | **P0** |
| 1.2 | 填充 `UpgradePoolConfig` 池子（common/rare/legendary） | `UpgradePoolConfig.asset`（Inspector拖拽） | **P0** |
| 1.3 | 填充 `ItemPoolConfig` 池子 | `ItemPoolConfig.asset`（Inspector拖拽） | **P0** |
| 1.4 | `FireAOE`/`Arrow` 走 `AttackSystem` 伤害管线 | `TimedPassiveModule.cs` | P1 |
| 1.5 | 实现 `IEffectExecutor`（`on_kill_chance`、`on_attack_trigger`） | 新建 `EffectExecutor` 类 + 注册 | P1 |

### 第二层：激活闲置内容（已有资产进入游戏）

> 资产已存在，只需要编排和配置。

| # | 任务 | 改动范围 | 优先级 |
|---|------|---------|--------|
| 2.1 | 增加连击档位（5/20/30），配合不同 Buff 类型 | `ComboBuffConfig.asset` | P0 |
| 2.2 | 波次混编（前排盾兵+后排弓箭手、骷髅海+长枪精英） | `StageConfig` / `WaveConfig` | P1 |
| 2.3 | 非Boss敌人配置差异化 `attackSequence`（部分用C技） | 各 Enemy prefab | P1 |
| 2.4 | 不同敌人配置不同 `cAttackProbability` | 各 Enemy prefab | P1 |
| 2.5 | 创建大旋风/落雷的 `UpgradeDefinition` asset | `ScriptableObjects/Upgrades/Definitions/` | P1 |

### 第三层：新机制（需要新代码+新设计）

> `design/in-game-growth-system.md` 第三期详案的落地。

| # | 任务 | 改动范围 | 优先级 |
|---|------|---------|--------|
| 3.1 | 道具手势识别（画圈→大旋风、长按下滑→落雷） | `InputManager.cs` | P2 |
| 3.2 | `unlock_attack` 完整管线（`IGestureRecognizer` + `UnlockedAttackRegistry`） | `AttackSystem.cs` + `InputManager.cs` | P2 |
| 3.3 | 前置条件系统（`UpgradePrerequisite` 过滤） | `UpgradeChoiceManager.CollectEligible()` | P2 |
| 3.4 | 局外成长接入三选一（开局预置能力、增加选项数） | `PlayerState` + `UpgradeChoiceManager` | P3 |
| 3.5 | 等级系统完善（同名奖励升级叠加） | `UpgradeEffectManager` | P2 |

---

## 五、已定义的 UpgradeDefinition 资产清单

### 数值型（category=0）

| Asset | upgradeId | effectType | 稀有度 | 建议池 |
|-------|-----------|-----------|--------|--------|
| `DamagePlus.asset` | `damage_plus` | `damage_multiplier` | Common | commonPool ✅已入 |
| `AttackSpeed.asset` | `attack_speed` | `attack_speed` | Common | commonPool ✅已入 |
| `Wisdom.asset` | `exp_boost` | `exp_multiplier` | Common | commonPool |
| `StabRangeBoost.asset` | `stab_range_boost` | `stab_range_boost` | Rare | rarePool |
| `SweepRangeBoost.asset` | `sweep_range_boost` | `sweep_range_boost` | Rare | rarePool |
| `PushWave.asset` | `push_wave` | `push_wave` | Rare | rarePool |
| `ConvergenceWave.asset` | `convergence_wave` | `convergence_wave` | Rare | rarePool |
| `OnKillCoin.asset` | `on_kill_coin` | `on_kill_chance` | Rare | rarePool ⚠️需IEffectExecutor |
| `OnAttackStab.asset` | `on_attack_stab` | `on_attack_trigger` | Rare | rarePool ⚠️需IEffectExecutor |

### 被动攻击型（category=2, AttackPassive）

| Asset | upgradeId | effectType | 稀有度 | 建议池 |
|-------|-----------|-----------|--------|--------|
| `PhantomWeapon.asset` | `phantom_weapon` | `passive_phantom_weapon` | Rare | rarePool |
| `ReturnWave.asset` | `return_wave` | `passive_return_wave` | Rare | rarePool |
| `ChainBounce.asset` | `chain_bounce` | `passive_chain_bounce` | Rare | rarePool |

### 计时被动型（category=3, TimedPassive）

| Asset | upgradeId | effectType | 稀有度 | 建议池 |
|-------|-----------|-----------|--------|--------|
| `TimedFireAOE.asset` | `timed_fire_aoe` | `passive_timed_aoe` | Rare | rarePool |
| `TimedArrow.asset` | `timed_arrow_volley` | `passive_timed_arrow` | Legendary | legendaryPool |

### 道具型（category=1, Item）

| Asset | upgradeId | gestureId | 稀有度 | 建议池 |
|-------|-----------|----------|--------|--------|
| `Wave.asset` | `wave` | `wave` | Common | ItemPoolConfig ✅已入 |
| `Item_HealthPotion.asset` | `health_potion` | `health_potion` | Common | ItemPoolConfig |
| `TestDamageBoost.asset` | `test_damage_boost` | `damage_boost` | Rare | ItemPoolConfig |
| *(待创建)* | `whirlwind` | `circle` | Rare | ItemPoolConfig |
| *(待创建)* | `lightning` | `long_press_swipe_down` | Legendary | ItemPoolConfig |

---

## 六、敌人资产清单

| Prefab | enemyId | 名称 | HP | ATK | 特性 |
|--------|---------|------|-----|-----|------|
| `Enemy_1.prefab` | 1 | 骷髅兵 | 15 | 10 | 基础杂兵，无特殊 |
| `Enemy_101.prefab` | 101 | 持剑杂兵 | 15 | 2 | 慢速攻击，有 ParryStunThreshold |
| `Enemy_102.prefab` | 102 | 盾兵 | 30 | 10 | 相邻共享血量 |
| `Enemy_103.prefab` | 103 | 长枪兵 | 100 | 15 | 高血量高伤害，精英 |
| `Enemy_104.prefab` | 104 | Boss | 500 | 10 | BOSS，含QTEController、多阶段 |
| `Enemy_105.prefab` | 105 | 弓箭手 | 80 | 5 | 远程，attackRange=3 |

**当前 Stage_1 仅使用 101 + 102。**

### 推荐波次编排方案

| 波次 | 前排 (row 0-1) | 后排 (row 2-3) | 体验目标 |
|------|---------------|---------------|---------|
| 前期 | 骷髅兵(1)×8 | — | 割草暖身 |
| 前期 | 持剑杂兵(101)×6 | 弓箭手(105)×2 | 引入远程威胁 |
| 中期 | 盾兵(102)×4 | 弓箭手(105)×3 | 前排肉盾+后排输出 |
| 中期 | 持剑杂兵(101)×4 + 长枪兵(103)×1 | — | 精英混编 |
| 后期 | 盾兵(102)×3 | 长枪兵(103)×2 + 弓箭手(105)×2 | 复合编队 |
| BOSS波 | Boss(104)×1 | 杂兵若干 | 最终挑战 |

---

## 七、推荐执行顺序

```
第一轮（立刻见效）:
  1.1 修复 Combo Buff → 连击系统真正生效
  1.2 填充 UpgradePoolConfig → 升级选项从2个变9个
  1.3 填充 ItemPoolConfig → 道具选项从1个变多
  2.1 增加连击档位 → 连击层次感

第二轮（丰富战斗）:
  1.4 Fire/Arrow 走 AttackSystem → 伤害倍率一致
  2.2 波次混编 → 敌人组合多样化
  2.3 非Boss C技差异化 → 敌人攻击需对策
  2.5 创建大旋风/落雷 asset → 道具型完备

第三轮（深化系统）:
  1.5 IEffectExecutor 实现 → on_kill_chance/on_attack_trigger 可用
  3.1 道具手势识别 → 道具操作感
  3.2 unlock_attack 管线 → 攻击解锁
  3.3 前置条件系统 → 组合解锁
```

---

## 八、关联文档索引

| 文档 | 内容 |
|------|------|
| `design/in-game-growth-system.md` | 三选一系统第三期详案（等级系统、效果架构、unlock_attack） |
| `design/three-choice-reward-system.md` | 三选一技术设计（数值buff/道具/被动攻击三种类型） |
| `design/attack-interrupt-system.md` | 三级打断体系（普通/C技/QTE）+ 霸体机制 |
| `design/boss-mechanics.md` | BOSS机制完整设计（架势/眩晕/招架/阶段/QTE） |
| `design/attack-cooldown.md` | 攻击冷却双模式设计 |
| `design/qte-sweep-design.md` | QTE Sweep 蓄力横斩格挡设计 |
| `memory/unity-project-understanding/qte-system.md` | QTE系统工程实现备忘 |
| `memory/unity-project-understanding/combat-targeting.md` | 战斗目标选取（ColumnManager.GetEnemiesInRange） |
| `memory/unity-project-understanding/attack-effect-lifecycle.md` | 攻击特效生命周期诊断 |
