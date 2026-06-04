---
id: kd_399bc9de-e1f4-4112-89cf-18da8734a4e6
type: memory
path: debug-parameters-reference.md
title: debug-parameters-reference
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780552342158
updatedAt: 1780552342159
---

# debug-parameters-reference

## Summary
全项目调试参数速查表 — 按模块列出可调参数、类型、默认值、配置文件位置及修改方式，供AI快速定位和验证。

<!-- locus:body:start -->
# 调试参数速查表

> 用途：快速定位全项目可调参数的位置、默认值、修改方式。  
> 更新：每次新增/删除/修改参数后需同步更新。

---

## 1. 关卡配置 (StageConfig / WaveConfig / RowConfig)

**文件**: `Assets/Scripts/Core/StageConfig.cs`  
**资产**: `StageConfig` ScriptableObject（通过 `CreateAssetMenu("一夫当关/关卡配置")` 创建）  
**运行时入口**: `StageController.Instance.stageConfig`

| 字段 | 类型 | 默认值 | 说明 | 修改方式 |
|------|------|--------|------|----------|
| `StageConfig.stageId` | int | 0 | 关卡ID | Inspector |
| `StageConfig.stageName` | string | "第一关" | 关卡名称 | Inspector |
| `StageConfig.waves` | List\<WaveConfig\> | [] | 波次列表 | Inspector |
| `StageConfig.fillUpRule` | FillUpRule | PerColumn | 补齐规则：逐列/逐排 | Inspector |
| `StageConfig.rushMoveDelay` | float | 0.2 | 全局补齐移动延迟（秒） | Inspector |
| `StageConfig.killMilestones` | List\<KillMilestoneEntry\> | [] | 累计击杀里程碑 | Inspector |
| `StageConfig.clearCoinReward` | int | 100 | 通关铜钱奖励 | Inspector |
| `StageConfig.formationConfig` | FormationConfig | null | 阵型配置资产引用 | Inspector |
| `WaveConfig.waveId` | int | 0 | 波次ID | Inspector |
| `WaveConfig.isBossWave` | bool | false | 是否为Boss波 | Inspector |
| `WaveConfig.rows` | List\<RowConfig\> | [] | 该波所有排 | Inspector |
| `WaveConfig.enableDynamicRush` | bool | false | 启用动态补齐加速 | Inspector |
| `WaveConfig.rushMoveDelay` | float | 0.2 | 动态补齐基础延迟（≥10敌人时） | Inspector |
| `WaveConfig.rushMoveDelayMin` | float | 0.02 | 动态补齐最低延迟（→0敌人时） | Inspector |
| `RowConfig.enemyIds` | int[5] | [0,0,0,0,0] | 该排每列敌人ID（0=空） | Inspector |
| `KillMilestoneEntry.count` | int | 0 | 击杀数阈值 | Inspector |
| `KillMilestoneEntry.rewards` | List\<KillReward\> | [] | 奖励列表 | Inspector |

### 动态补齐逻辑

**代码位置**: `StageController.GetRushMoveDelay()`（`Assets/Scripts/Managers/StageController.cs`）

- 读取当前波次的 `WaveConfig.enableDynamicRush`
- 若为 false：回退到 `StageConfig.rushMoveDelay`（全局值）
- 若为 true：根据存活敌人数量在 `[rushMoveDelayMin, rushMoveDelay]` 之间线性插值
- 敌人数量阈值硬编码在 `GetRushMoveDelay()` 中（当前: `aliveCount >= 10` 用最大值，`→0` 用最小值）

---

## 2. 敌人实体 (Enemy.cs)

**文件**: `Assets/Scripts/Enemy/Enemy.cs`  
**修改方式**: Prefab Inspector 或枚举配置

### 基础属性

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `enemyName` | string | "骷髅兵" | 显示名称 |
| `enemyId` | int | 0 | 敌人类型ID |
| `occupySlots` | int | 1 | 占用列数 |
| `maxHealth` | float | 100 | 最大生命值 |
| `attackSpeed` | float | 1 | 攻击速度系数 |
| `attackDamage` | float | 10 | 攻击伤害 |
| `attackRange` | float | 1 | 攻击范围 |
| `moveSpeed` | float | 1 | 移动速度 |

### 攻击序列

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `attackSequence` | List\<AttackStep\> | [] | 攻击步骤序列（循环） |
| `AttackStep.isCAttack` | bool | false | 是否C技（霸体窗口） |
| `AttackStep.spawnDuration` | float | 0 | 前摇时长（秒） |
| `AttackStep.drawDuration` | float | 0 | 收招时长（秒） |
| `AttackStep.extraCooldown` | float | 0 | 额外冷却（秒） |
| `AttackStep.useFlip` | bool | false | 攻击时左右翻转 |

### 远程攻击

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `isRanged` | bool | false | 是否远程单位 |
| `projectilePrefab` | GameObject | null | 飞行物Prefab |
| `projectileArcHeight` | float | 3 | 抛物线最高点 |
| `projectileFlyDuration` | float | 1 | 飞行时长 |
| `projectileZTargetOffset` | float | 5 | 目标Z偏移 |
| `projectileXOffset` | float | 0 | 目标X偏移 |

### 架势/击飞系统

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `maxPoise` | float | 50 | 最大架势值 |
| `stunDuration` | float | 1.5 | 眩晕时长（秒） |
| `launchDuration` | float | 2 | 击飞时长（秒） |
| `launchGravity` | float | 20 | 下落重力加速度 |
| `launchReboundVelocity` | float | 8 | 空中反弹速度 |
| `launchYHeightMin` | float | 1.5 | 初始击飞Y轴最小高度 |
| `launchYHeightMax` | float | 4.5 | 初始击飞Y轴最大高度 |
| `launchedDamageTakenMultiplier` | float | 1.5 | 浮空受伤倍率 [1~5] |
| `launchedHitExtendDuration` | float | 0.5 | 空中受击延长时间（秒） |

### 奖励/弱点/Boss

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `coinReward` | int | 10 | 击杀铜钱 |
| `expReward` | float | 10 | 击杀经验 |
| `gemSprite` | Sprite | null | 经验宝石精灵 |
| `isBoss` | bool | false | 是否Boss |
| `bossHealthBarPrefab` | GameObject | null | Boss血条Prefab |
| `stabDamageMultiplier` | float | 1 | 刺击伤害倍率 |
| `slashDamageMultiplier` | float | 1 | 斩击伤害倍率 |
| `pierceDamageMultiplier` | float | 1 | 穿刺伤害倍率 |
| `sweepDamageMultiplier` | float | 1 | 横扫伤害倍率 |
| `launchDamageMultiplier` | float | 1 | 击飞伤害倍率 |
| `poiseDamageMultiplier` | float | 1 | 架势伤害倍率 |
| `shareHealthWithAdjacent` | bool | false | 同行相邻同ID共享血量 |
| `parryStunThresholds` | ParryStunThreshold[] | [] | 招架血量眩晕阈值 |

### 运行时状态 (NonSerialized, 代码内部)

| 字段 | 类型 | 说明 |
|------|------|------|
| `state` | EnemyState | 当前状态机状态 |
| `currentHealth` | float | 当前血量 |
| `currentPoise` | float | 当前架势值 |
| `bossState` | BossState | Boss推进状态 |
| `instanceId` | int | 运行时唯一ID |

---

## 3. 敌人池/配置 (EnemyPool / EnemyConfig)

**文件**: `Assets/Scripts/Enemy/EnemyPool.cs`  

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `EnemyPool.poolSize` | int | ~ | 对象池容量 |
| `EnemyPool.enemyPrefabs` | 列表 | ~ | 敌人Prefab列表 |

---

## 4. 英雄/技能配置 (HeroConfig / AttackSkillConfig / UltimateSkillConfig)

**文件**: `Assets/Scripts/Core/` 下各 Config 类  

| 字段 | 类型 | 说明 |
|------|------|------|
| `HeroConfig.maxHealth` | float | 英雄最大生命 |
| `HeroConfig.baseAttack` | float | 基础攻击力 |
| `HeroConfig.moveSpeed` | float | 移动速度 |
| `AttackSkillConfig.damageMultiplier` | float | 技能伤害倍率 |
| `AttackSkillConfig.cooldown` | float | 冷却时间 |
| `AttackSkillConfig.range` | float | 技能范围 |
| `UltimateSkillConfig.damageMultiplier` | float | 大招伤害倍率 |
| `UltimateSkillConfig.energyCost` | float | 能量消耗 |

---

## 5. 升级系统 (UpgradePoolConfig / UpgradeDefinition)

**文件**: `Assets/Scripts/Upgrade/`

| 字段 | 类型 | 说明 |
|------|------|------|
| `UpgradeDefinition.upgradeId` | int | 升级ID |
| `UpgradeDefinition.maxLevel` | int | 最大等级 |
| `UpgradeDefinition.valuesPerLevel` | float[] | 每级数值 |
| `UpgradePoolConfig.pool` | List | 可选升级池 |

---

## 6. 阵型配置 (FormationConfig)

**文件**: `Assets/Scripts/Core/FormationConfig.cs`（ScriptableObject）

| 字段 | 类型 | 说明 |
|------|------|------|
| 列间距/排间距 | float | 阵型空间参数 |
| 可见排数 | int | 玩家可见排数 |

---

## 7. 管理器参数

### StageController

**文件**: `Assets/Scripts/Managers/StageController.cs`

| 字段/方法 | 说明 |
|-----------|------|
| `stageConfig` | 当前关卡配置引用 |
| `GetRushMoveDelay()` | 动态补齐延迟计算（硬编码阈值: 10敌人） |
| `currentWaveIndex` | 当前波次索引 |

### WaveSpawner

**文件**: `Assets/Scripts/Wave/WaveSpawner.cs`

| 字段 | 类型 | 说明 |
|------|------|------|
| `stageConfig` | StageConfig | 关卡配置引用（可选，留空自动获取） |
| `enemyPool` | EnemyPool | 敌人池引用 |
| `columnManager` | ColumnManager | 列管理器引用 |
| `enemyManager` | EnemyManager | 敌人管理器引用 |
| `spawnRoot` | Transform | 生成根节点 |
| `CurrentWaveIndex`(property) | int | 当前波次（只读） |

### EnemyManager

**文件**: `Assets/Scripts/Core/EnemyManager.cs`

| 字段 | 说明 |
|------|------|
| `IsAllEnemiesDead`(property) | 所有敌人是否已死亡 |
| `EnemyDeath` 事件 | 敌人死亡事件 |
| 列压缩/补齐触发逻辑 | `ColumnManager` 交互 |

---

## 8. UI/HUD 参数 (BattleHUD / Combo / KillReward)

### BattleHUD

**文件**: `Assets/Scripts/UI/BattleHUD.cs`

| 字段 | 说明 |
|------|------|
| Boss血条模板 | Boss血条显示 |
| 血量条动画参数 | fillAmount插值速度等 |

### ComboManager

**文件**: `Assets/Scripts/UI/ComboManager.cs`

| 字段 | 说明 |
|------|------|
| `comboTimeout` | 连击超时时间（秒） |
| `comboDisplayThreshold` | 连击显示最低数 |

### KillRewardManager

**文件**: `Assets/Scripts/Core/KillRewardManager.cs`

| 字段 | 说明 |
|------|------|
| 击杀里程碑检查 | 基于 `KillMilestoneEntry` |

### CoinCounterUI

| 字段 | 说明 |
|------|------|
| 铜钱计数动画参数 | DOTween 持续/缓动 |

---

## 9. QTE系统 (QTEController)

**文件**: `Assets/Scripts/QTE/QTEController.cs`

| 字段 | 说明 |
|------|------|
| QTE时间窗口 | QTE判定时间 |
| QTE成功伤害倍率 | QTE成功时的伤害加成 |

---

## 10. 被动技能/测试开关 (PassiveTriggerModule)

**文件**: `Assets/Scripts/Passive/PassiveTriggerModule.cs`

| 字段 | 说明 |
|------|------|
| 测试开关（`#if UNITY_EDITOR`） | 编辑器中强制启用/禁用某些被动 |

---

## 11. Editor 调试块 (UNITY_EDITOR)

散落在各文件中的 `#if UNITY_EDITOR` 块，通常用于：
- 绘制 Gizmos（攻击范围、移动路线等）
- OnGUI 调试标签
- 快捷键触发测试行为
- 强制覆盖运行时参数

---

## 快速查询索引

| 需求 | 查哪里 |
|------|--------|
| 调整关卡敌人数量/排布 | `StageConfig` 资产 → Inspector |
| 调整补齐速度 | `StageConfig.rushMoveDelay` 或 `WaveConfig.enableDynamicRush` + `rushMoveDelay` / `rushMoveDelayMin` |
| 调整敌人血量/伤害/速度 | Enemy Prefab → Inspector |
| 调整架势/击飞参数 | Enemy Prefab → Inspector |
| 调整英雄属性 | `HeroConfig` 资产 → Inspector |
| 调整技能伤害/冷却 | `AttackSkillConfig` / `UltimateSkillConfig` 资产 |
| 调整升级数值曲线 | `UpgradeDefinition` 资产 |
| 调整连击超时 | `ComboManager` Inspector |
| 开启/关闭被动测试 | `PassiveTriggerModule` 代码 `#if UNITY_EDITOR` |
| 调整波次机制 | `WaveConfig`（每个波次独立配置） |
| 查看动态补齐硬编码阈值 | `StageController.GetRushMoveDelay()` 代码 |
| 调整QTE判定窗口 | `QTEController` Inspector |
<!-- locus:body:end -->
