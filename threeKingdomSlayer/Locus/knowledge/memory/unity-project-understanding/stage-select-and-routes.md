---
id: kd_0fe5683a-16cb-4893-aa4b-6579106c1549
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: 选关/路线/关卡解锁的使用文档（2026-10-24）——关卡清单资产怎么加关卡、解锁规则写在数据里的两种机制、运行时怎么解析当前关卡与起点节点、存档字段、改动时必须连带检查的五处与坑；含对应代码路径便于按需核对。
injectMode: inherit
aiEditMode: inherit
---

# 选关 / 路线 / 关卡解锁 · 使用文档

> **一句话** ✅ ✅：关卡清单是**资产** ✅（`RouteCatalogData` ✅ ✅），解锁规则**写在数据里** ✅ ✅，运行时只按数据解析 ✅ ✅ —— **加一条关卡 = 改资产 ✅ ✅，不改场景 ✅ ✅**。
> **本文防的是** ✅ ✅：**再造一套解锁规则** ✅ ❗（位置计数那种 ✅ ✅ —— 插一条关卡就连锁改 ✅ ❗）与**把关卡列表挂回场景** ✅ ❗（那会让「加关卡 = 改场景」✅ ❗❗✅ ✅）✅ ✅。
> 纪律 ✅：本文只写「读代码得不到的东西」✅ ✅；**与代码不一致时以代码为准** ✅ ✅。

---

## 一、数据在哪 ✅ ✅

| 东西 | 在哪 | 备注 |
|---|---|---|
| **关卡清单（选关页显示什么 + 解锁规则）** ✅ | `Assets/RouteData/RouteCatalog.asset` ✅ ✅（`RouteCatalogData` ✅ ✅）✅ | **唯一真源** ✅ ✅ |
| 单个关卡 ✅ | `FakeRouteStageConfig` ✅ ✅（SO ✅ ✅）：`stageId` ✅ ✅ / `stageName` ✅ ✅ / `startNode` ✅ ✅ / `nodes` ✅ ✅ / `clearCoinReward` ✅ ✅ / **`prerequisiteStageIds`** ✅ ✅ | 现有的：`Tutorial/Tutorial_Route.asset` ✅ ✅（9100 ✅ ✅）、`ChangbanpoScroll/Changbanpo_ScrollRoute.asset` ✅ ✅（1 ✅ ✅）✅ |
| 单个节点 ✅ | `FakeRouteNodeConfig` ✅ ✅（SO ✅ ✅）：`battleEntries` ✅ ✅ / `outgoingChoices` ✅ ✅ / `completionPolicy` ✅ ✅ / `savePoint` ✅ ✅ / `entryDialogue` ✅ ✅ | 节点之间的**分叉**靠 `outgoingChoices` ✅ ✅ ✅ |
| 场景里的挂件 ✅ | Battle 场景的 `StageConfigManager` ✅ ✅ 上挂着 `RouteCatalog` ✅ ✅（MonoBehaviour ✅ ✅）→ `catalogData` ✅ ✅ 指向清单资产 ✅ ✅ | ⚠️ 这个组件现在只是**挂件** ✅ ✅（真正的数据在资产里 ✅ ✅）✅ |

**兼容** ✅ ✅：`RouteCatalog` ✅ ✅ 上的 `tutorialStage` ✅ ✅ + `fakeRouteStages` ✅ ✅ 是**旧写法** ✅ ✅ —— 只在**没配清单资产**时兜底 ✅ ✅（选关列表会退回「教学关 + 列表 ✅ ✅」）✅ ✅。

---

## 二、解锁规则（两种，都写在数据里 ✅ ✅）

1. **总是可选** ✅ ✅：`RouteCatalogData.alwaysUnlockedStageIds` ✅ ✅（stageId 列表 ✅ ✅）✅
   → 教学关 ✅ ✅ 与第一个正式关 ✅ ✅ 放这里 ✅ ✅（等价于旧规则「保底放行前两条 ✅ ✅」，**别改回位置计数 ✅ ❗** —— 那会让插一条关卡连锁改 ✅ ❗✅ ✅）✅ ✅。
2. **需要前置** ✅ ✅：`FakeRouteStageConfig.prerequisiteStageIds` ✅ ✅（stageId 列表 ✅ ✅，可多个 ✅ ✅ → 支持分叉 ✅ ✅）✅
   → 判定：**所有前置都通关** ✅ ✅（`RouteCatalogData.IsUnlocked` ✅ ✅ ✅）。

> **两条可叠加** ✅ ✅：在「总是可选」里 ✅ ✅ → 无视前置 ✅ ✅；不在 ✅ ✅ → 看前置 ✅ ✅。

---

## 三、运行时怎么解析（两个坑点 ✅ ✅）

**当前关卡** ✅ ✅：`FakeRouteRuntime.ResolveActiveRouteFromSave` ✅ ✅ ✅
- 读存档的 `activeFakeRouteStageId` ✅ ✅；
- **不在可选列表里** ✅ ✅ → **回退到列表第一条** ✅ ✅（打 warning ✅ ✅）；
  → 所以**新号（存档 id = -1 ✅ ✅）= 列表第一条 = 教学关 ✅ ✅** ✅ ✅。

**起点节点** ✅ ✅：= `routeConfig.startNode` ✅ ✅
- ⚠️⚠️ **别用 `FakeRouteRuntime.useTestStartNode` + `testStartNode`** ✅ ❗ —— 那是**开发用**的 ✅ ✅（我做 J1 时设过 = Node_J1 ✅ ✅）→ 它会**盖掉**路线的正式起点 ✅ ❗ → 走教学关时 J1 不在它的节点表里 ✅ ❗ → 报「目标节点无效 ✅ ✅」✅ ✅。
  → **正式开发前确认它是关的** ✅ ✅（`Battle.scene` ✅ ✅ 的 `Route Flow (Scroll)` ✅ ✅ 上 ✅ ✅）✅ ✅✅ ✅。

**续玩** ✅ ✅：只有显式入口 `FakeRouteLaunch.StartFromCheckpoint` ✅ ✅ 才会恢复检查点 ✅ ✅；
落地页自举**不恢复** ✅ ✅（重新进游戏从关卡初始节点开始 ✅ ✅ —— 避免脏数据 ✅ ✅✅ ✅）。

---

## 四、存档字段 ✅ ✅
- `activeFakeRouteStageId` ✅ ✅（当前关卡 ✅ ✅）
- 每关的进度快照 ✅ ✅ → `RunSnapshotSaveSection` ✅ ✅（`SaveManager.GetRouteStageSnapshot(stageId)` ✅ ✅ ✅）
- **重置存档** ✅ ✅ = `SaveManager.Delete()` ✅ ✅（⚠️ 它会清缓存 ✅ ✅ —— 直接 `PlayerPrefs.DeleteKey` ✅ ✅ **没用** ✅ ❗✅ ✅）；只清**编辑器**的 ✅ ✅，APK 里那份独立 ✅ ✅✅ ✅。

---

## 五、改动时**必须连带检查**的五处 ✅ ✅
1. 清单资产 ✅ ✅ 的 `stages` ✅ ✅ + 新关卡的 SO ✅ ✅；
2. **解锁关系** ✅ ✅（`alwaysUnlockedStageIds` ✅ ✅ / `prerequisiteStageIds` ✅ ✅）✅ ✅；
3. **起点节点** ✅ ✅ = `startNode` ✅ ✅（**别用开发测试起点 ✅ ❗** ✅ ✅）✅ ✅；
4. 场景里 `RouteCatalog.catalogData` ✅ ✅ 指向该资产 ✅ ✅（换了资产要重指 ✅ ✅）✅ ✅；
5. **跑校验** ✅ ✅：`Tools/数据校验/关卡清单 ✅ ✅` ✅ ✅（查 id 重复 ✅ ✅ / 前置不存在 ✅ ✅ / 成环 ✅ ✅ / 默认可选 id 不存在 ✅ ✅✅ ✅）。

---

## 六、坑 ✅ ✅
- **stageId 是存档键** ✅ ❗ → 改它 = 进度作废 ✅ ❗；`Tutorial_Route` ✅ ✅ 的 9100 ✅ ✅ 是**故意的大数** ✅ ✅（不能用 stageId 比大小判断解锁 ✅ ✅❗✅ ✅）✅ ✅。
- **节点 id 跨路线可能同名** ✅ ✅（教学关的 N1 ✅ ✅ 与正式关的 N1 ✅ ✅ 同名 ✅ ✅）→ 解锁关系用 **stageId** ✅ ✅ 不要用 nodeId ✅ ❗✅ ✅。
- **插一条关卡** ✅ ✅ → 旧的「按位置数」规则会连锁改 ✅ ❗ → 已全部换成数据驱动 ✅ ✅ ✅。
- **改完字段要重存资产** ✅ ✅（否则 YAML 留失效键 ✅ ✅ ✅）。

---

## 七、对应代码路径（改之前先核对 ✅ ✅）
- `Assets/Scripts/RouteFake/RouteCatalog.cs` ✅ ✅
- `Assets/Scripts/Data/RouteCatalogData.cs` ✅ ✅
- `Assets/Scripts/Data/FakeRouteStageConfig.cs` ✅ ✅
- `Assets/Scripts/Data/FakeRouteNodeConfig.cs` ✅ ✅
- `Assets/Scripts/UI/StageSelectUI.cs` ✅ ✅
- `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` ✅ ✅ / `FakeRouteRuntime.Entry.cs` ✅ ✅ / `FakeRouteRuntime.Node.cs` ✅ ✅
- `Assets/Scripts/Services/SaveManager.cs` ✅ ✅、`Assets/Scripts/Data/Save/RunSnapshotSaveSection.cs` ✅ ✅
- `Assets/Scripts/Editor/RouteCatalogValidator.cs` ✅ ✅
- 相关交接 ✅ ✅：`plan/data-modularization-handoff-20261024.md` ✅ ✅
