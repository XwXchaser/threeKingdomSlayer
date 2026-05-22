---
id: kd_1e5bda6b-3133-419b-bb0d-44ddfdbe9366
type: memory
path: plan-a-hero-hud-implementation.md
title: plan-a-hero-hud-implementation
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778769385905
updatedAt: 1778769788777
---

# plan-a-hero-hud-implementation

## Summary
Plan A 英雄 HUD Prefab 提取实现记录：架构、文件清单、已知问题、陷阱和测试要点

<!-- locus:body:start -->
# Plan A: HeroHUD Prefab 实现记录

## 架构
- `HeroHUD.cs` — 挂载在英雄 HUD Prefab 根节点，持有 healthSlider, healthText, reviveText, 6组cooldown Image + chargeFill Image
- `BattleHUD.cs` — 战斗时通过 `HeroConfig.heroHUDPrefab` 实例化 HeroHUD，通过 `heroHUDParent` Transform 定位父容器
- `HeroConfig.heroHUDPrefab` — `GameObject` 字段，指向英雄 HUD Prefab

## 文件清单
- `Assets/Scripts/UI/HeroHUD.cs` — 新建
- `Assets/Scripts/UI/BattleHUD.cs` — 重写，移除英雄专属字段，新增 heroHUDParent/waveText/coinText/bossHealthBarPrefab/bossBarsParent
- `Assets/Scripts/Core/HeroConfig.cs` — 新增 heroHUDPrefab 字段
- `Assets/Scripts/Enemy/Enemy.cs` — 新增 bossHealthBarPrefab 字段（可空）
- `Assets/Scripts/Core/UltimateEffect_Berserk.cs` — 改用 BattleHUD.SetHealthBarColor/ResetHealthBarColor
- `Assets/Prefabs/UI/HeroHUD_Zhangfei.prefab` — 张飞 HUD Prefab，含 Health(Slider), Health(TMP), 6个cooldown Image + 子Image
- `Assets/Scenes/Battle.scene` — BattleHUD 下移除英雄子节点，新增 HeroHUDParent + WaveText，接线 coinText/waveText/heroHUDParent
- `Assets/ScriptableObjects/Warrior/Hero_Zhangfei.asset` — heroHUDPrefab 已赋值

## 已知问题
- 重构后存在缩放问题和未赋值问题（待后续修复）
- bossHealthBarPrefab/bossBarsParent 是新增字段，当前 NULL（需等 BossHealthUI Prefab 创建后接线）

## 关键陷阱
- **禁止手动编辑 Unity YAML 后拼接 SceneRoots 之后的内容** — SceneRoots 必须是 YAML 文件的最后一个文档，否则场景解析失败（0 root objects）
- 通过 Editor API（DestroyImmediate/Instantiate/SerializedObject）操作场景，避免 YAML 手工编辑

## 测试要点
- 进入战斗后 HeroHUD 正确实例化
- 血量、冷却、充能 UI 更新正确
- Boss 血条功能（需等 BossHealthUI Prefab 创建后测试）
<!-- locus:body:end -->
