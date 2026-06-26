---
id: kd_fb351cd8-3e22-4ce9-902e-7b6b93cee3dc
type: design
path: cyclone-feature.md
title: cyclone-feature
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1782490500562
updatedAt: 1782490500564
---

# cyclone-feature

## Summary
旋风(Cyclone) TimedPassive 升级功能：周期性随机击飞敌人，配合 cyclone 精灵动画，高级解锁落地伤害。

## Content
# 旋风 (Cyclone) — TimedPassive 击飞效果

## 概述
- **类型**：TimedPassive（定时触发被动）
- **effectType**：`passive_timed_cyclone`
- **稀有度**：Rare
- **最高等级**：5
- **触发机制**：每 N 秒随机选取场上敌人击飞

## 效果
| 等级 | 间隔(s) | 敌人数 | 击飞时长(s) | 击飞伤害 | 落地伤害% |
|------|---------|--------|------------|---------|----------|
| Lv.1 | 10 | 2 | 2.0 | 30 | 0（未解锁）|
| Lv.2 | 9  | 2 | 2.2 | 35 | 0 |
| Lv.3 | 8  | 3 | 2.5 | 40 | 50% |
| Lv.4 | 7  | 3 | 2.5 | 50 | 50% |
| Lv.5 | 6  | 4 | 3.0 | 60 | 70% |

### 落地伤害规则
- `landingDamagePercent > 0` 时解锁落地伤害
- 落地伤害 = 击飞伤害 × landingDamagePercent
- 敌人中途死亡 / 效果结束不触发落地伤害

### 目标选取规则
- Fisher-Yates 随机洗牌后取前 N 个
- 过滤条件：`CanBeLaunched(float.MaxValue)` — 非死亡、非 Boss（除非眩晕中）
- 可被 `ForceLaunch` Buff 强制击飞

## 代码架构

### 数据层
- `Assets/Scripts/Core/UpgradeDefinition.cs`
  - `CycloneLevelConfig` struct：intervalSeconds, enemyCount, knockupDuration, damage, landingDamagePercent
  - `List<CycloneLevelConfig> cycloneLevels`
  - `GetTriggerInterval()` 新增 cyclone case

### 运行时
- `Assets/Scripts/Core/TimedPassiveModule.cs`
  - `cycloneEffectPrefab` 字段引用 `CycloneEffect.prefab`
  - `SpawnCyclone()`：随机选敌 → Launch(customDuration) → 每个敌人实例化一个 CycloneEffect
- `Assets/Scripts/Effect/CycloneEffect.cs`
  - 跟踪敌人 Y 轴判断上升/下降阶段
  - cyclone1-6 顺序播放（上升阶段），cyclone5-6 循环（持续阶段）
  - 监听 `Enemy.OnLaunchedLanded` 触发落地伤害
- `Assets/Scripts/Enemy/Enemy.cs`
  - `Launch(float customDuration)` 重载
  - `OnLaunchedLanded` 事件
  - 公开 `LaunchStartLocalPos`, `CurrentLaunchYHeight`, `IsLaunchRising`

### 配置资产
- `Assets/ScriptableObjects/Upgrades/Definitions/Cyclone.asset` — 5 级配置
- `Assets/Prefabs/Effects/CycloneEffect.prefab` — 含 CycloneEffect + SpriteRenderer（6 个 cyclone sprite）
- `Assets/ScriptableObjects/Upgrades/UpgradePoolConfig.asset` — rarePool 添加 Cyclone (weight=10)

### 编辑器
- `Assets/Scripts/Editor/UpgradeDefinitionEditor.cs`
  - `DrawCycloneSection()`：每级显示间隔/敌人数/击飞时长/伤害/落地伤害%

### 动画
- 三个 Controller 按 Enemy_101 模板改造（Launched_Rise → Launched_Fall → Idle）：
  - `Assets/Animations/Enemy_102.controller`
  - `Assets/Animations/Enemy_103.controller`
  - `Assets/Animations/Enemy_105.controller`
- 击飞动画全部使用 `enemy5_launch.png` 精灵
- 新增 `Enemy_XXX_Launched_Fall.anim` 各 1 个（单帧 enemy5_launch）

### 特效素材
- `Assets/Sprites/EffectSprites/cyclone/` — cyclone1-6 共 6 张精灵
