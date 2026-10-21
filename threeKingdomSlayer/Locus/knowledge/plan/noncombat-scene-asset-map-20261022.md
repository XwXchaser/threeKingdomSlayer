---
id: kd_d4bce40d-0323-42d0-b23b-88cf4a1088a8
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_7c2e5a91-4b6d-4f38-9d21-5c8a1f0b7e44
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 非战斗部分现状测绘（2026-10-22）：4 个 build 场景的搭建方式、Battle.scene 的 19 个根对象、overlay 栈机制、资产配置分布（SO 12 类 / Resources 5 目录 / 主体走 Inspector 引用）、局内 10 个 phase 的流程、节点模型与存档结构，并给出「为新机制留接口」的现状与下一刀建议。
---

# 场景搭建 / 资产配置 / 非战斗局内构成：现状测绘

> 目的：在给新机制加扩展点之前，先把「非战斗部分是怎么搭的、资产怎么配的、局内流程长什么样」搞清楚。
> 本文全部结论来自 2026-10-22 对当前工程（HEAD `13e909e`）的实测，不含推测；推测处已标注。

---

## 0. 一句话结论

**场景是「1 个主场景 + 2 个 additive overlay 界面场景 + 1 个 additive 路线场景」**；
**资产以 Inspector 引用为主，Resources 只剩 5 个目录**；
**局内流程由 `FakeRouteRuntime` 的 10 个 phase 状态机驱动，节点内容与分支写在 ScriptableObject 里**。
扩展点已经在位的部分不少（角色接口、端口接口、事件源、执行器注册表），
**仍缺扩展点的核心是「节点执行流程」**（新节点机制会撞 `FakeRouteRuntime` 的协程）。

---

## 1. 场景搭建

### 1.1 Build Settings 的 4 个场景

| # | 场景 | 角色 |
|---|---|---|
| 0 | `Assets/Scenes/Battle.scene` | **主场景**：落地页（Hub）+ 战斗 + 路线宿主，全在一场戏里 |
| 1 | `Assets/Experiments/CurvedScroll/YJunctionSample.unity` | **路线表现场景**：additive 加载，卷轴/分支的美术与演出 |
| 2 | `Assets/Scenes/StageSelect.scene` | **选关页**：纯 UI overlay 场景 |
| 3 | `Assets/Scenes/TalentTree.scene` | **天赋页**：纯 UI overlay 场景 |

### 1.2 两类场景切换（`Assets/Scripts/Managers/SceneFlowManager.cs`，194 行）

- **active scene 替换**：`ReplaceActiveScene` → `LoadSceneAsync`（可先播摄像机推入动画，`CameraManager.nextScene` + `PlayDeparture`）。
- **overlay 场景叠加**：`OpenOverlay` / `SwitchOverlay` / `CloseOverlay` / `CloseAllOverlays`，用一个 `_overlayStack` 维护层级；
  已打开的不会重复加载。
- overlay 场景的根节点挂 `UIOverlayScene` 标记（`sceneName` + `pauseUnderlyingScene`）。
- **已知坑（已修）**：主场景 `LoadSceneMode.Single` 重载时 Unity 会连带卸载 overlay，
  但 `SceneFlowManager` 是 `DontDestroyOnLoad` → `_overlayStack` 残留 → `HasOverlay` 永远为真 → Hub 内容永久隐藏。
  修法在 `OnSceneLoaded` 里清栈（`SceneFlowManager.cs:64`）。

### 1.3 `Battle.scene` 的 19 个根对象（75 个组件 / 51 种类型）

| 根对象 | 组件 | 职责 |
|---|---|---|
| `Main Camera` | Camera, CameraManager, AudioListener, CameraFeedbackController | 摄像机 + 转场 + 打击反馈震动 |
| `BattleHUD(Canvas)` | Canvas, CanvasScaler, GraphicRaycaster, **BattleHUD, PauseMenuUI, CanvasGroup, QTEDisplay, HubHudModeSwitch, HubStateSource** | 战斗 HUD + 暂停 + QTE + **Hub 状态源** |
| `Canvas` | Canvas, CanvasScaler, GraphicRaycaster, SafeAreaAdapter | 安全区适配层（QTE 指示器 / 背景） |
| `StageController` | StageController | 关卡门面（战斗侧唯一入口） |
| `EnemyPool` | EnemyPool | 敌人对象池 |
| `Enemies` | — | 敌人容器 |
| `Player` | PlayerHitFeedback, ThornArmorEffect, ChargeStabVisual, PierceAimIndicator, PlayerMoveStateMachine | 玩家表现层 |
| `Manager` | **16 个**（BuffManager, ComboManager, UpgradeChoiceManager, UpgradeEffectManager, PassiveTriggerModule, ItemInventory, TimedPassiveModule, WaveManager, SpikeTrapController, ItemEffectRunner, DialogueManager, ActiveSkillInventory, ActiveSkillRunner, WaveTimer, PixelHitEffectManager…） | 全局服务集中挂点 |
| `Route Flow (Scroll)` | ScrollRoutePresenter, **FakeRouteRuntime** | 路线流程 + 卷轴表现 |
| `SceneFlowManager` | SceneFlowManager | 场景/overlay 栈 |
| `StageConfigManager` | StageConfigManager | 可选路线清单（Inspector 拖入） |
| `EventSystem` | EventSystem, StandaloneInputModule | 输入 |
| `Sweep` / `Pierce` | MeshCollider, SpriteRenderer | 横扫/穿刺的判定体 |
| `CycloneItemController` | CycloneItemController | 旋风道具 |
| `FrontItemBar` | CanvasGroup, FrontItemBar | 顶部道具栏 |
| `Battle Y Route Host` | BattleYRouteHost | **禁用的遗留对象**（Legacy 的旧预演驱动） |
| `Locus_TintRegressionProbe` | SpriteRenderer, Enemy | **禁用的调试探针** |

> ⚠️ 场景里还留着 2 个**禁用**的遗留/调试对象（`Battle Y Route Host` 挂的是 `Assets/Scripts/Legacy/RouteV1/` 里的类型）。
> 它们不参与运行，但会随场景一起进包（连带 `Legacy` 程序集无法剔除，见 §4.3）。

### 1.4 Hub（落地页）的界面结构

`BattleHUD(Canvas)` 下 16 个子对象：`KillCount` / `Victory(panel)` / `ChargeIndicator` / `CoinCounter` /
`HeroHUDParent` / `WaveText` / `BossBars` / `ComboDisplay` / `HittedOverlay` / `BuffDisplayPanel` /
`KillMilestoneDisplay` / `DialogueLayer` / `PauseLayer` / `Defeat(panel)` / **`StartStageButton`** / **`HubTabBar`**。

- **tab 栏 = 界面导航**（`BattleHubTabBar`，191 行）：四个 tab = 四个界面（战斗 / 选关 / 天赋 / 家园）。
  tab 栏画在 `order 50` 的独立 Canvas 上，压在选关/天赋页（40/41）之上。
- **状态单一来源**：`HubStateSource`（73 行）每帧算一次 `IsPrepared`（路线是否就绪）与 `Overlay`（当前 overlay 场景名），
  变化时广播 `Changed`；三个订阅者（开始按钮 / tab 栏 / HUD 模式切换）不再各自轮询。

---

## 2. 资产配置

### 2.1 ScriptableObject 分布（实测）

| 类型 | 数量 | 所在目录 |
|---|---|---|
| `StageConfig` | 12 | `Assets/Resources/StageConfigs`、`Assets/RouteData/ChangbanpoScroll/Battles`、`Assets/RouteData/Tutorial` |
| `UpgradeDefinition` | 40 | `Assets/Resources/UpgradeDefinitions`、`Assets/ScriptableObjects/ActiveSkills`、`Assets/ScriptableObjects/Upgrades/Definitions` |
| `AttackSkillConfig` | 11 | `Assets/Prefabs/UI/Skills`、`Assets/ScriptableObjects/Moves/Zhangfei` |
| `FakeRouteStageConfig` | 2 | `Assets/RouteData/ChangbanpoScroll`、`Assets/RouteData/Tutorial` |
| `FormationConfig` | 2 | `Assets/RouteData/Tutorial`、`Assets/ScriptableObjects/StageFor` |
| `HeroConfig` / `UpgradePoolConfig` / `TalentTreeConfig` | 各 1 | `Assets/ScriptableObjects/{Warrior,Upgrades,Talents}` |

**约定**：路线数据（`RouteData/`）与 ScriptableObject（`ScriptableObjects/`）分目录；`Resources/` 只留运行时按名加载的那几个。

### 2.2 `Resources` 目录（只有 5 个）

`EnemyPrefabs`(12) / `StageConfigs`(5) / `UI`(9) / `Materials`(1) / `UpgradeDefinitions`(1)。
→ **主体资产引用走 Inspector 拖拽**（场景组件上的 `public` 字段），符合 `design/anti-ghost-reference.md`。

### 2.3 三种引用方式并存

| 方式 | 数量 | 位置 | 评价 |
|---|---|---|---|
| Inspector 引用 | 绝大多数 | 场景组件 / prefab 的 `public` 字段 | ✅ 规范 |
| `Resources.Load` | 8 处 | `StageRegistry` / `WeaponMotionBlurController` / `EnemyPool` / `ItemDiscardPopup` | ⚠️ anti-ghost 违规项（`EnemyPool` 按名加载还会让包体依赖剪不掉） |
| **硬编码资源路径字符串** | 1 处 | `ScrollRoutePresenter.routeSceneName = "Assets/Experiments/CurvedScroll/YJunctionSample.unity"` | ⚠️ 幽灵引用（改名/移动即断），且该场景路径**不在** Resources 里 |

---

## 3. 非战斗的局内构成

### 3.1 流程状态机（`FakeRouteRuntime.FakeRoutePhase`，10 个态）

```
None → Ready ──点「开始关卡」──► RunPreparedFlow
                                     │
        ┌────────────────────────────┴────────────────────────────┐
        ▼                                                          ▼
 EnteringNode（进节点：演出 + 入口对话）                    Defeated（战败）
        │
        ▼
 Battle（节点战斗：WaveSpawner 出怪 / 清场）
        │
        ▼
 WaitingReward（等三选一或节点奖励）
        │
        ├──有出口──► RouteChoiceTransition ──► ChoosingRoute（分支面板）──► FakeMoving（移动）──► EnteringNode(下一个)
        │
        └──终点节点──► Completed（整关结算：铜钱 / 标记通关 / 胜利面板）
```

- 战斗侧由 `StageController` 承担（`IRouteStageHost` 端口），路线侧由 `FakeRouteRuntime` 承担。
- 表现侧由 `ScrollRoutePresenter`（实现 `IRouteMotionPresenter` 端口）承担：开场演出、节点演出、分支移动。

### 3.2 节点模型（`Assets/Scripts/Data/FakeRouteNodeConfig.cs`）

| 字段 | 运行时行为 |
|---|---|
| `nodeId` / `displayName` | 存档与进度条的锚点 |
| `entryDialogue` / `postBattleDialogue` / `dialogues` | 对话（`DialogueEventData`，已在 `Locus.Data`） |
| `postBattlePresentation` | 战后表现（`FakeRoutePresentation`） |
| `battleEntries` | **节点内的战斗条目列表**（每条一个 `StageConfig` + 条件） |
| `rewards` | 节点奖励（`KillRewardEntry`） |
| `outgoingChoices` | **出口/分支**（`FakeRouteChoiceConfig` → 目标节点） |
| `completionPolicy` | 节点完成判定（BattleAndReward / ChoiceConfirmation / EnemyDefeated / Immediate） |
| `terminalPolicy` | 终点行为（None / CompleteWhenContentFinished / CompleteAfterPresentation） |
| `isFinalNode` / `savePoint` / `visualProfile` | 终点标记 / 是否存档点 / 场景表现配置 |

### 3.3 存档（`Assets/Scripts/Core/SaveManager.cs`，326 行，单一 blob）

`SaveData` 9 个字段：`clearedStageIds` / `props`（道具含铜钱）/ `coinCount`(已废弃) / `tutorialCompleted` /
`completedTutorialDialogueIds` / `routeStageSnapshots`(V1 旧) / `fakeRouteSnapshots` / `activeFakeRouteStageId` / `unlockedTalents`。

`FakeRouteStageSaveSnapshot`（路线检查点，30 字段）：检查点节点、HP/复活/等级/经验、已访问/已完成节点、
分支选择历史、每个节点的已完成战斗条目、升级等级、主动技能等级、道具、**可破坏道具的破坏状态**（BUG-001 的根因在这里）。
存储：`PlayerPrefs` 单 key `player_save` + JSON（`JsonUtility`）。

### 3.4 选关与天赋

- 选关（`StageSelectUI` 220 行）：`StageConfigManager.GetSelectableRoutes()` = 教学关 + 正式路线列表（Inspector 顺序）。
  解锁规则在 `StageSelectUI.ResolveUnlock()`（BUG-002 已修为「保底放行前两条」）。
- 天赋（`TalentTreePanel` 435 行 + `TalentTreeState` 127 行）：解锁状态存在 `SaveData.unlockedTalents`。

---

## 4. 对「为新机制留接口」的影响

### 4.1 已经在位的扩展点 ✅

| 位置 | 形态 |
|---|---|
| 战斗侧角色 | `IStageFormation` / `IStageBattleState` / `IRunFlow` / `ICombatStaging` / `IRewardGate` + `IRouteStageHost` 合成端口（全在 `Locus.Data`） |
| 路线侧端口 | `IRouteMotionPresenter`（表现）、`IRouteBattleHost`→`IRouteStageHost`（战斗宿主） |
| 局内 Build | `IEffectExecutor`（行为型）、`IUpgradeNumericApplier` / `IUpgradeDescriptionFormatter`（本轮新增） |
| 玩家输入 | `GameplayInputGate`（按 owner + 原因） |
| Hub 状态 | `HubStateSource` 单一计算源 + 事件广播 |
| 场景切换 | `SceneFlowManager` 的 overlay 栈 |

### 4.2 仍缺扩展点（下一刀的候选，按优先级）

| # | 位置 | 现象 | 新机制会怎么撞 |
|---|---|---|---|
| 1 | **`FakeRouteRuntime` 的节点执行流程**（914 行，10 个 phase 写死在协程里） | `EnterNode` / `RunBattleEntries` / `PlayPostBattleSequence` / `TrySelectChoice` 直接按类型分支 | 支柱五「节点奖励 / 分支预览 / 新节点形态」要加就得改这个类 |
| 2 | `UpgradeChoiceManager`（567 行） | 抽取/展示/关卡完成回调都在里面 | 新升级池形态会撞 |
| 3 | `RouteChoicePanel`（133 行） | 分支面板的 UI 与数据绑定 | 新分支形态（多选/带条件）会撞 |
| 4 | 存档单一 blob | `SaveData` 9 字段 + 两套快照 | 新存档域要加字段（批次 4 计划中） |

### 4.3 顺带发现（未处理，仅记录）

1. `Battle.scene` 里 2 个禁用对象（`Battle Y Route Host`=Legacy 旧驱动、`Locus_TintRegressionProbe`=调试探针）——**Legacy 因此无法剔出包体**。
2. `ScrollRoutePresenter.routeSceneName` 是**硬编码资源路径字符串**（幽灵引用）。
3. `Resources.Load` 8 处（anti-ghost 违规项，且 `EnemyPool` 按名加载导致包体依赖剪不掉）。
4. `SaveManager` 仍是单一 blob（批次 4 的存档分片未做）。

---

## 5. 建议的下一刀

**给「节点执行流程」加扩展点**：把 `FakeRouteRuntime` 里按节点类型分支的部分收成
「节点内容执行器」注册表（与本轮升级体系的 `IUpgradeNumericApplier` 同一范式），
让新增一种节点形态（新的完成策略 / 新的内容块）不必改 `FakeRouteRuntime` 本体。

做法上仍走本项目已验证的两步：
1. 先按机制把 `FakeRouteRuntime` 拆成 partial（节点流程 / 分支选择 / 检查点 / 演出）——机械搬移，内容丢失 = 0；
2. 再把「按类型分支」的部分改成注册表，行为用「节点快照全量比对」验证。
