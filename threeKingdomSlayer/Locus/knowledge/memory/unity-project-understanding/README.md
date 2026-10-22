---
id: kd_236f4cca-6d80-4bed-8fd6-348373e8c03c
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 项目功能地图（2026-10-24）——按「要做什么功能」索引到对应的使用文档与代码入口：选关/路线、天赋树、战斗状态与Buff、招式与选角、Y场景表驱动管线、存档；含「加一条内容要动哪几处」速查与「文档与代码不一致时以代码为准」的纪律。
injectMode: inherit
aiEditMode: inherit
---

# 项目功能地图（2026-10-24）

> **给 AI/人 的入口** ✅：先看这里 ✅ → 定位到「这份功能怎么用 / 怎么改 / 怎么验」✅ → **再按需读代码** ✅ ✅。
> **纪律** ✅：文档只写**读代码得不到的东西** ✅（数据在哪 ✅ / 联动点 ✅ / 坑 ✅）；**文档与代码不一致时以代码为准** ✅ ✅（每份文档末尾都列了对应代码路径 ✅ ✅，先核对再动手 ✅ ✅）。

---

## 一、功能 → 文档 → 代码入口（速查 ✅）

| 要做的事 | 使用文档 | 主要代码入口 |
|---|---|---|
| **选关页 / 路线 / 关卡解锁** ✅ | `stage-select-and-routes.md` ✅ | `Assets/Scripts/RouteFake/RouteCatalog.cs` ✅、`Assets/Scripts/Data/RouteCatalogData.cs` ✅、`Assets/Scripts/UI/StageSelectUI.cs` ✅、`Assets/Scripts/RouteFake/FakeRouteRuntime*.cs` ✅ |
| **天赋树 / 天赋页** ✅ | `talent-system.md` ✅ | `Assets/Scripts/Data/TalentTreeConfig.cs` ✅、`Assets/Scripts/Core/TalentTreeState.cs` ✅、`Assets/Scripts/UI/TalentTreePanel.cs` ✅、`Assets/Scripts/Data/Save/TalentSaveSection.cs` ✅ |
| **战斗状态 / Buff** ✅ | `buff-and-status.md` ✅ | `Assets/Scripts/Core/BuffManager.cs` ✅、`Assets/Scripts/Data/StatModifier.cs` ✅、`Assets/Scripts/Data/CombatBuffDefinition.cs` ✅、`Assets/Scripts/Data/BuffIds.cs` ✅、`Assets/Scripts/Player/Combat/PlayerState.cs` ✅ |
| **招式 / 技能 / 选角（换英雄·换武器·换招式）** ✅ | ⏳ **待补** ✅（需要先读一遍代码 ✅ ✅）✅ | `Assets/Scripts/Data/HeroConfig.cs` ✅、`Assets/Scripts/Data/AttackSkillConfig.cs` ✅、`Assets/Scripts/Data/ActiveSkillDefinition.cs` ✅、`Assets/Scripts/Editor/CombatDataTableWindow*.cs` ✅（既有总表窗口 ✅ ✅）✅ |
| **Y 场景（美术搭景 / 表驱动）** ✅ | `yscene-table-pipeline.md` ✅ | `Assets/Scripts/Editor/Experiments/YSceneSpecTools.cs` ✅、`Assets/Scripts/Editor/Experiments/YScenePlacementImporter.cs` ✅、`Assets/Scripts/Editor/Experiments/YSceneTableizeWindow.cs` ✅ |
| **存档** ✅ | 见「三、联动点」✅ ✅ | `Assets/Scripts/Services/SaveManager.cs` ✅、`Assets/Scripts/Data/Save/*.cs` ✅（分片 ✅ ✅）✅ |

**全局约定** ✅ ✅：
- **内容 = 资产** ✅ ✅（ScriptableObject ✅ ✅）；**规则写在数据里** ✅ ✅；**机制 = 代码** ✅ ✅（`Assets/Scripts/Data` ✅ ✅ 下的枚举 ✅ ✅ 是机制词表 ✅ ✅ —— 加机制要写代码 ✅ ✅，那是应该的 ✅ ✅）。
- **加一条内容 = 加资产** ✅ ✅（选关 ✅ ✅ / 天赋 ✅ ✅ / 状态 ✅ ✅ / 招式 ✅ ✅ / 场景 ✅ ✅ 全部如此 ✅ ✅✅ ✅）。
- **改数据后必须跑校验** ✅ ✅：`Tools/数据校验/` ✅ ✅（天赋树 ✅ / 关卡清单 ✅ / 战斗状态定义 ✅ ✅）✅ ✅。
- **数据校验器的清单** ✅ ✅：`Assets/Scripts/Editor/TalentTreeValidator.cs` ✅、`RouteCatalogValidator.cs` ✅、`CombatBuffValidator.cs` ✅ ✅。

---

## 二、「加一条内容」要动哪几处（最容易漏的联动 ✅ ✅）

| 加什么 | 动哪里 | **必须连带检查** ✅ |
|---|---|---|
| **一个关卡** ✅ | `RouteCatalogData.stages` ✅ ✅ + 该关的 `FakeRouteStageConfig` ✅ ✅ | ① 场景的 `RouteCatalog.catalogData` ✅ ✅ 指向该资产 ✅ ✅；② **解锁规则** ✅ ✅（`alwaysUnlockedStageIds` ✅ ✅ / `prerequisiteStageIds` ✅ ✅）；③ 路线起点 ✅ ✅ = `routeConfig.startNode` ✅ ✅（**别用 `FakeRouteRuntime.useTestStartNode` ✅ ✅** —— 那是开发用 ✅ ✅）；④ 跑 `Tools/数据校验/关卡清单 ✅ ✅` ✅ ✅ |
| **一条天赋** ✅ | `TalentTreeConfig.nodes` ✅ ✅ | ① `prerequisiteIds` ✅ ✅（留空 = 上一项 ✅ ✅）；② **解锁载荷** ✅ ✅（`moveConfig` ✅ ✅ / `flagId` ✅ ✅ —— 至少一个 ✅ ✅）；③ `treePosition` ✅ ✅（面板位置 ✅ ✅）；④ `nodeId` ✅ ✅ **不能改** ✅ ✅（存档按它存 ✅ ✅）；⑤ 跑 `Tools/数据校验/天赋树 ✅ ✅` ✅ ✅ |
| **一种状态** ✅ | 数值型 ✅ → `ComboBuffConfig` ✅ ✅；机制型 ✅ → `CombatBuffDefinition` ✅ ✅ + `BuffIds` ✅ ✅ | ① 机制型**还要写消费方代码** ✅ ✅（那是机制 ✅ ✅ 不是内容 ✅ ✅）；② 跑 `Tools/数据校验/战斗状态定义 ✅ ✅` ✅ ✅（会查「id 不在词表里 ✅ ✅」）✅ ✅ |
| **一个场景段** ✅ | `Authoring/Placement/{name}.json` ✅ ✅ → 规格 ✅ ✅ → 段 prefab ✅ ✅ | ① **坐标空间** ✅ ✅（`space` ✅ ✅ = local ✅ ✅ 不做世界换算 ✅ ✅）；② **精灵按名字取** ✅ ✅（多图集 ✅ ✅ 必须带 `spriteName` ✅ ✅）；③ **嵌套 prefab 实例** ✅ ✅ 的变换要自己带 ✅ ✅；④ 跑 `Tools/Y 场景规格/⑧ 全链路校验 ✅ ✅` + `⑨ 画面逐像素 ✅ ✅` ✅ ✅ |

---

## 三、跨域联动点（最容易漏 ✅ ✅）

1. **存档** ✅ ✅：`SaveManager` ✅ ✅ 是**单个 PlayerPrefs 键** ✅ ✅（`player_save` ✅ ✅）✅ ✅；分片在 `Assets/Scripts/Data/Save/` ✅ ✅（Progress ✅ / Prop ✅ / RunSnapshot ✅ / Talent ✅ / Tutorial ✅ ✅✅ ✅）。
   → **重置存档** ✅ ✅ = `SaveManager.Delete()` ✅ ✅（**会清缓存 ✅ ✅** —— 直接 `PlayerPrefs.DeleteKey` ✅ ✅ 没用 ✅ ✅✅ ✅）。
   → ⚠️ 只清**编辑器**的 ✅ ✅；装好的 APK ✅ ✅ 里那份独立 ✅ ✅ ✅。
2. **天赋存档按 nodeId 存** ✅ ✅ → 改前置结构**不毁老档** ✅ ✅；**改 nodeId 会毁** ✅ ❗✅ ✅。
3. **关卡进度按 stageId 存** ✅ ✅ → 改 stageId 会毁 ✅ ❗；改**解锁关系** ✅ ✅ 不会 ✅ ✅。
4. **Y 场景的段表** ✅ ✅ = 场景里的 `YRouteSegments` ✅ ✅（**唯一真源** ✅ ✅ —— 规格里的 `segmentSpecs` ✅ ✅ 已删 ✅ ✅ ✅）。
5. **代码改了字段** ✅ ✅ → **要重存相关资产** ✅ ✅（否则 YAML 留着失效的键 ✅ ✅ —— Unity 忽略它 ✅ ✅，但会误导人 ✅ ✅✅ ✅）。
6. **改完场景资产要 `AssetDatabase.Refresh()`** ✅ ✅ —— 否则编辑器还在用缓存的旧版本 ✅ ✅（今天回退 J1 时踩过 ✅ ✅✅ ✅）。

---

## 四、纪律 ✅ ✅
- **新增/修改功能时** ✅ ✅ → 同步更新**对应的那份使用文档** ✅ ✅（否则它就开始骗人了 ✅ ✅❗）✅ ✅。
- 文档里的**数值与路径** ✅ ✅ → 随手核一下 ✅ ✅（每份文档末尾都有对应代码路径 ✅ ✅✅ ✅）。
- **不确定就量** ✅ ✅ —— 别脑内枚举假设 ✅ ✅（今天两次误判 J1 路牌位置 ✅ ✅ ✅，都是没量 ✅ ✅❗）✅ ✅。
