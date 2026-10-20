---
id: kd_mainmenu-hub-refactor-plan-20261020
injectMode: inherit
injectAgents:
- '*'
summary: 主菜单/落地页重构方案 —— 落地即 Battle 场景、关卡内容进场景即加载、选关与天赋降级为二级 overlay；阶段 A/B/C 已完成，含包体实测数据、分阶段计划与 16 条坑
aiEditMode: inherit
---

# 主菜单 / 落地页重构方案（2026-10-20）

> 目标平台：微信小程序。加载完毕后玩家**直接进入 Battle 场景**，看到的就是当前进度的关卡起始场景。
> 本文是**可交接的实施计划**：现状机制 → 目标形态 → 分阶段任务 → 验收 → 已知坑。
> **进度：阶段 A / B / C / D1 / D3 / D4 全部完成并推送（MainMenu 已删除，全局回落地页）；阶段 F（`fb5279b`）、G（`1210dc5`）、H（`d3676c3`）已完成、本地提交未 push；只剩 D0 中央轻交互、D2 顶部状态条；阶段 E 已重测（真实可省 2.4MB，Tutorial_YJ 的 9.3MB 已落地）。**

---

## 0. 一句话目标

```
小程序启动
  └─ 直接进 Battle.scene（build index 0）
       ├─ ① 自动读存档「当前进度」= route + checkpoint 节点
       ├─ ② 加载 route 场景表现（背景就位）        ← 进场景即完成
       ├─ ③ 玩家看到的 = 关卡的起始场景
       ├─ ④ 点中央区域 → 轻交互（不推进关卡）
       ├─ ⑤ 点「开始关卡」大按钮（= 参考图的「捕捉」）→ 正式开跑 route
       └─ 底部 tab：战斗 / 选关 / 天赋 / 家园
```

**选关页与天赋页不再是必经流程**，是玩家主动打开的二级内容。

> ✅ ① ② ③ ⑤ + 底部 tab 已全部实现；④ 中央轻交互未做（可砍）。

---

## 0.1 当前状态（2026-10-20 收尾快照）

**分支**：`feat/stage-select-talent-overlays`（HEAD = `fb5279b`，本地提交，未 push）

**build settings（4 个）**：`Battle.scene`（index 0，启动） / `YJunctionSample.unity` / `StageSelect.scene` / `TalentTree.scene`

**现在能跑的完整链路**：

```
启动 → Battle.scene
  └─ FakeRouteRuntime.Start()：读存档当前关卡（默认教学关 9100）
       └─ PrepareForEntry → 加载 YJunctionSample（additive）+ 定位到起始节点（含 checkpoint）
            └─ Phase = Ready
                 ├─ 局内 HUD 全隐藏（HubHudModeSwitch）；头像 / 金币 / 「开始关卡」/ 底部 4 tab 可见
                 ├─ 点 tab → 选关 / 天赋 overlay 打开、能关
                 └─ 点「开始关卡」→ RunPrepared → 局内 HUD 恢复 → 对话/战斗/奖励/下一节点…
                      └─ 结算或暂停返回 → 重载 Battle → 又回到 Ready（带 checkpoint）
```

| 项 | 状态 |
|---|---|
| 阶段 A（选关 overlay）/ B（天赋 overlay） | ✅ `908e7c0` |
| 阶段 C（落地页自举） | ✅ `ff912e9`，验收 4/5/7/9/10 均跑过 |
| 阶段 D1（底部 tab）/ D3（回落地页）/ D4（删 MainMenu） | ✅ `8f30a2e` / `1cdc7f8`+`87d266d` / `f39d4ba` |
| 就绪态/关卡态 HUD 拆分 | ✅ `8b780cd` |
| 阶段 E | ✅ 重测：敌人美术真实只浪费 2.4MB（旧文档的「114MB 死资源」作废）；`Tutorial_YJ` 移出 build（9.3MB） |
| 阶段 F：自举路径收尾修复（结算/冷启动接档/通关推进/选关立即生效 + 补提交路线资产） | ✅ `fb5279b` + `46e7c19`（未 push） |
| 阶段 G：界面接管全屏（画布层级 / 不透明底 / 落地页内容让位） | ✅ `1210dc5`（未 push） |
| 阶段 H：tab 栏改为界面导航（选关/天赋是界面，不是二级窗口） | ✅ `d3676c3`（未 push） |
| D0 中央轻交互 / D2 体力状态条 / 字体 36MB | ⬜ 未做 |

**待用户人眼验收**：hub 实际观感（头像/金币/开始按钮/tab 栏的大小与位置、就绪态背景是否好看）。截图工具在本机截不到 Overlay UI（§7.14），只能人眼看。

**已知遗留**：
- `FakeRouteRuntime.cs` / `FakeRouteStageSaveSnapshot.cs` 里挂着可破坏道具（`BreakableProp`）的未提交改动
- 配置里 `enemyId = 999` 出现 7 次但没有对应 prefab → 运行时会走 Cube fallback
- `EnemyPool` 用 `Resources.Load("EnemyPrefabs/...")` 按名字加载（违反 `anti-ghost-reference.md`，属未来架构改造）

---

## 1. 需求原文与澄清记录

用户原话（2026-10-20）：

> 改动的最大目标就是在这个界面就加载好关卡内容，无需选关，直接加载玩家当前的最新进度（如果玩家选择了其他关卡则再次开启游戏进入时加载对应关卡）而不需要进入后还要点选关，就是说玩家开游戏一进来就是关卡的起始场景。
> 参考图的背景就是游戏的主内容，玩家点捕捉就相当于直接开启了关卡而非选关再加载。

已确认的两个决策点：

| Q | 答案 | 影响 |
|---|---|---|
| Q1 敌人要不要在点「捕捉」前摆出来？ | **不要** | 不用改敌人刷怪时机；只拆 `FakeRouteRuntime.Begin()` 成 Prepare / Run 两段 |
| Q2 `MainMenu.scene` 保留还是废弃？ | **废弃**（顺带做包体精简） | build index 0 改成 Battle.scene；MainMenu 从 build settings 移除 |

### 曾经走错的方向（别再走）

- ❌ 把选关做成「选**节点**」（N1/E0/J1/EV/N2/N3）→ 选关是选**路线**（教学关 / 正式YJ），点哪条都从该路线的 `startNode` 开始
- ❌ 把需求拆成「hub 模式（无敌人无 route）+ 关卡模式」两个互斥模式 → 是**一个连续流程的两个阶段**
- ❌ 以为「战斗」是 Battle 场景的某种子状态 → 「战斗」= 当前主页面的内容本身，点击就开打

---

## 2. 参考图 → 本项目映射

参考图（露米娅大陆）结构 = **一个场景，不切场景**：

```
┌──────────────────────────────────────┐
│ 头像 │ 体力 278/100 │ 金币 11,870     │  顶部状态条
│ 商城  首充礼包              3天11时 任务│  左右功能入口
│         捕获率 60%                     │  中央 = 主体玩法区
│    X119        [ 捕捉 ]          人→  │  「捕捉」= 本项目的「开始关卡」
│ 欢迎来到露米娅大陆!                    │
│ 捕获3种[火山]噜咪 (3/3)                │  任务条
├──────────────────────────────────────┤
│   噜咪   交易   火山🔒   家园           │  底部 tab（二级内容切换）
└──────────────────────────────────────┘
```

映射：

| 参考图 | 本项目 | 现状 |
|---|---|---|
| 中央玩法区 | 战斗场景的 gameplay 区 | 已有 |
| 「捕捉」大按钮 | **「开始关卡」**（启动 route 流程） | **缺** |
| 底部 tab | 战斗 / 选关 / 天赋 / 家园 | **缺** |
| 顶部状态条 | 体力 / 金币 | 已有 `CoinCounter` |
| 任务条 | 击杀里程碑 | 已有 `KillMilestoneDisplay` |
| 选关 / 天赋 | 二级 overlay 场景 | 天赋**有资产无入口**；选关**有死代码** |

---

## 3. 现状机制（接手前必读）

### 3.1 场景清单与体积（实测，`AssetDatabase.GetDependencies`）

| # | 场景 | 依赖数 | 贴图数 | 依赖体积 | 状态（2026-10-20 更新） |
|---|---|---|---|---|---|
| ~~0~~ | ~~`Assets/Scenes/MainMenu.scene`~~ | ~~101~~ | ~~25~~ | ~~45.7 MB~~ | **已于 D4 删除**（`f39d4ba`） |
| — | `Assets/Scenes/Tutorial/Tutorial_YJ.unity` | 571 | 247 | 154.8 MB | 遗留副本，已从 build settings 移除（`0972fa2`），文件还在磁盘 |
| **0** | `Assets/Scenes/Battle.scene` | 462 | 183 | 92.2 MB | **启动场景**（落地页） |
| 1 | `Assets/Experiments/CurvedScroll/YJunctionSample.unity` | 146 | 60 | 59.0 MB | **route 场景**（表现层 additive） |
| 2 | `Assets/Scenes/StageSelect.scene` | 9 | 0 | 0.0 MB | overlay |
| 3 | `Assets/Scenes/TalentTree.scene` | — | — | — | overlay |

> 当前 build settings 实际只有 4 个：Battle / YJunctionSample / StageSelect / TalentTree。

> 依赖体积是各场景独立计算，共享资产会重复计。**并集**才是真实包体。

### 3.2 现在的启动→开打链路

> ⚠️ **本节描述的是 MainMenu 时代的历史链路，已于 D4（`f39d4ba`）作废**；现在的真实链路看 §4.1 与 §3.3。

```
微信小程序启动
  └─ MainMenu.scene（build index 0）
       └─ MainMenuUI：点「新游戏」/「继续」/「教学关」
            ├─ 写 FakeRouteLaunch.PendingConfig = <FakeRouteStageConfig>
            ├─ （选关）SaveManager.SetActiveFakeRouteStage(cfg.stageId)
            └─ CameraManager.PlayDeparture() → SceneManager.LoadSceneAsync("Battle")
                 ├─ StageController.Awake()（StageController.cs:69-113）
                 │    └─ 读 FakeRouteLaunch.PendingConfig → fakeRouteStageConfig
                 └─ FakeRouteRuntime.Start()（FakeRouteRuntime.cs:82-91）
                      └─ autoStart==true && stageController.fakeRouteStageConfig!=null
                           └─ Begin(route)（:100-139）
                                ├─ AcquireExclusiveDriving()  ← 强制 StageController.autoStartStage=false
                                ├─ movementPresenter.TryValidate()
                                └─ BeginRouteNextFrame()（:141+）
                                     ├─ 决定 startNode（checkpoint 或 routeConfig.startNode）
                                     ├─ movementPresenter.Prepare(routeConfig, …)   ← **加载场景表现**
                                     └─ 走节点流程：对话 → 战斗 → 奖励 → 下一节点 → … → 结算
                                          └─ 结算 → StageController.GoToMainMenu() → 回 MainMenu
```

### 3.3 关键开关（改造的抓手）

| 开关 | 位置 | 当前值 | 作用 |
|---|---|---|---|
| `FakeRouteRuntime.autoStart` | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs:24`（`[SerializeField] private bool autoStart = true;`） | Battle.scene 里 **true** | **true = 进 Battle 立刻跑完整条 route；false = 进 Battle 只 Prepare 不跑** ← 落地页改造的主开关 |
| `StageController.autoStartStage` | `StageController.cs:17` | Battle.scene 里 **false**（且被 `AcquireExclusiveDriving()` 强制 false，:72） | StageController 自身不自动开战，交给 FakeRouteRuntime 驱动 |
| `MainMenuUI.showDebugStageSelect` | `MainMenuUI.cs` | **true** | 临时面板「教学关 / 正式关卡 / 加铜钱 +500」；定稿前删 |
| `MainMenuUI.CreateStageGrid()` | `MainMenuUI.cs` | **死代码**（`Start()` 从未调用） | 选关网格的旧实现，逻辑要搬到 `StageSelectUI` |

### 3.4 「当前进度」存档 API（已存在，直接用）

| API | 用途 |
|---|---|
| `SaveManager.GetActiveFakeRouteStageId()` | 当前关卡（stageId） |
| `SaveManager.SetActiveFakeRouteStage(int stageId)` | 选关页写回 |
| `SaveManager.GetFakeRouteSnapshot(routeId, stageId)` | checkpoint 节点 |
| `SaveManager.ClearFakeRouteSnapshot(routeId, stageId)` | 清进度（重开该关时用） |
| `SaveManager.SaveFakeRouteSnapshot(snapshot)` | 存 checkpoint |

`MainMenuUI.OnContinueGame()`（MainMenuUI.cs:592+）**已经实现了「读当前进度 → 写 PendingConfig → 进 Battle」**，落地页改造要把这段逻辑搬到 Battle 侧复用。

### 3.5 两条可选择的 route

| route | asset | routeId | stageId | 节点 | 在 `StageConfigManager.fakeRouteStages` 里？ |
|---|---|---|---|---|---|
| 教学关 | `Assets/RouteData/Tutorial/Tutorial_Route.asset` | `tutorial_route` | 9100 | N1, E0 | **否**（只在 `MainMenuUI.tutorialRouteConfig`） |
| 正式YJ | `Assets/RouteData/ChangbanpoScroll/...`（`StageConfigManager` 引用） | `changbanpo_scroll_preview` | 1 | N1, E0, J1, EV, N2, N3 | **是**（唯一一条） |

> ⚠️ **不要**把教学关直接插到 `fakeRouteStages[0]`——`MainMenuUI.OnNewGame/OnContinueGame` 用 `fakeRouteStageConfigs[0]` 当「正式关」，插前面会改变行为。要么加到**末尾**，要么在 `StageConfigManager` 新增一个 `tutorialStage` 字段。

### 3.6 路线表现层

- `ScrollRoutePresenter.routeSceneName = Assets/Experiments/CurvedScroll/YJunctionSample.unity`（两条 route 共用同一个 scroll 场景）
- `FakeRouteRuntime.Begin()` 内部 `movementPresenter.Prepare(routeConfig, …)` 负责加载它
- `IRouteMotionPresenter.Prepare` / `IsSceneReady` / `TryValidate` 在 `Assets/Scripts/RouteFake/IRouteMotionPresenter.cs`

### 3.7 已知的历史遗留

| 遗留 | 位置 | 说明 |
|---|---|---|
| `Tutorial_YJ.unity` 在 build settings 但几乎无用 | `ProjectSettings/EditorBuildSettings.asset` | 唯一依赖 9.3MB / 7 张贴图（老 `Enemy1` 贴图 + stronghold 道具）。route 场景实际是 `YJunctionSample.unity` |
| `MainMenuUI.CreateStageGrid()` 死代码 | `MainMenuUI.cs` | 从未被调用 |
| `TalentTreePanel` 常驻 MainMenu 且无 UI 入口（进阶） | `MainMenu.scene` Canvas 下 `active=False`；`MainMenuUI` 没有 `talentButton` 字段 | **但** `TalentButton` 的 persistent onClick 已绑 `TalentTreePanel.Toggle`，所以点击**其实能打开**（只是默认关闭、无返回按钮）。阶段 B 已改为开 `TalentTree` overlay 场景 |
| `DisplacementDebugTool` / `ItemTestHelper` 常驻生产场景 | `Battle.scene` → `Player/AttackSystem` | 已在 `refactor/p0-displacement-debug-editor-only` 用 `#if UNITY_EDITOR` 处理 |
| `.meta` 用 base64 GUID、prefab 引用用 hex GUID | 全项目 | 文本 grep 比对 GUID 会失效，**依赖分析必须用 `AssetDatabase.GetDependencies`** |

---

## 4. 目标形态

### 4.1 场景拓扑

```
Build Settings
  [0] Assets/Scenes/Battle.scene          ← 启动场景（小程序加载完直接进）
  [1] Assets/Scenes/StageSelect.scene      ← overlay（additive）
  [2] Assets/Scenes/TalentTree.scene      ← overlay（additive）
  [3] Assets/Experiments/CurvedScroll/YJunctionSample.unity   ← route 表现场景（additive，运行时加载）
  （MainMenu.scene / Tutorial_YJ.unity 移除）

运行时：
  Battle.scene (active)
    ├─ [overlay 栈] StageSelect.scene / TalentTree.scene  ← SceneFlowManager 管理
    └─ [additive] YJunctionSample.unity                  ← ScrollRoutePresenter 管理
```

### 4.2 落地页（Battle 场景）的两种状态

| 状态 | 触发 | 表现 |
|---|---|---|
| **就绪态** | 进 Battle 场景，自动读进度 → `PrepareForEntry` | route 背景就位（无敌人）；「开始关卡」大按钮；底部 4 个 tab；**局内 HUD 全部隐藏** |
| **关卡态** | 点「开始关卡」→ `RunPrepared` | 大按钮与 tab 栏隐藏；局内 HUD 恢复；跑 route：对话 → 战斗 → 奖励 → 下一节点 → 结算 → 回就绪态 |

**HUD 可见性（`HubHudModeSwitch`，2026-10-20 用户拍定）**：

| 内容 | 就绪态 | 关卡态 | 说明 |
|---|---|---|---|
| 头像 `UltPortraitButton` | ✅ 常驻 | ✅ | **局内 = 必杀技按钮，局外（就绪态）= 将来换角色入口**，现在只留接口，不做功能 |
| 金币 `CoinCounter` | ✅ | ✅ | 顶部状态条（体力还没有） |
| 「开始关卡」/ tab 栏 | ✅ | ❌ | |
| 技能键 + QTE 板（`HudCard/FlipPanel`） | ❌ | ✅ | 翻牌正/背面都是局内内容 |
| 血条（4 个部件 + 血量数字）、经验条 | ❌ | ✅ | |
| 暂停键 `PauseLayer` | ❌ | ✅ | hub 没有暂停语义 |
| 击杀里程碑 / 连击 / Buff / Boss 血条 / 蓄力指示 / WaveText | ❌ | ✅ | 全属局内 |

> 用户明确：**敌人不在就绪态摆出来**（Q1 答案），所以就绪态中央是空场。
> 实现要点：`HubHudModeSwitch` 用「进入就绪态前记录 active → 隐藏 → 离开时还原」，**不要强行 SetActive(true)** —— 这些节点里有自己的显隐逻辑（连击数、击杀里程碑、QTE 翻牌），强行打开会显示不该显示的东西。
> 头像「局外换角色」的接口位置：`Assets/Scripts/UI/UltimateButtonUI.cs:200` 目前写的是 `ultimateButton.interactable = ready`（只在必杀技亮起来时可点），就绪态下能量=0 所以头像纯展示。将来要做换角色，就在 `IsPrepared` 时把 `interactable` 打开并换成换角色面板入口。

### 4.3 四个 tab = 四个界面

| tab | 界面 | 载体 | 属于该界面的内容 |
|---|---|---|---|
| 战斗 | 战斗界面 | `Battle.scene` 本体 | 落地页（就绪态：开始关卡 / 头像 / 金币）+ 局内 HUD（关卡态） |
| 选关 | 选关界面 | additive `StageSelect.scene` | 路线列表、标题、返回 |
| 天赋 | 天赋界面 | additive `TalentTree.scene` | 天赋树、金币、返回 |
| 家园 | 待定 | 占位 | — |

### 4.4 界面职责划分（2026-10-20 两轮用户反馈后定稿）

第一轮反馈：打开选关/天赋时「开始关卡 + 头像 + 金币」没消失。
第二轮澄清（用户原话）：**「天赋还是作为界面而不是二级窗口。选关也是界面不是二级窗口」** —— 它们与「战斗」同级，底部 tab 就是界面导航。

| 内容 | 归属 | 在选关/天赋界面上 |
|---|---|---|
| 底部 tab 栏 | App 级导航（`Battle.scene` 的 `HubTabBar`，自己挂 overrideSorting 画布 order 50） | **常驻**，当前 tab 高亮，点 tab 直接切界面 |
| 开始关卡按钮 | 战斗界面 | 隐藏（`IsPrepared && !HasOverlay`） |
| 头像 / 金币 | 战斗界面（`HubHudModeSwitch.hubOnly`） | 隐藏 |
| 局内 HUD | 战斗界面关卡态 | 本就隐藏 |

四条硬规则：

1. **界面画布必须是 Screen Space - Overlay 且 order > `BattleHUD(Canvas)`(21)**：选关 40、天赋 41。Overlay 画布永远画在 Camera 画布之后，旧值 10/11 会被 HUD 盖住。
2. **界面自带不透明全屏底**（0.05/0.06/0.09，alpha 1），不靠下层场景透出来。
3. **界面切换由 tab 驱动**：`SceneFlowManager.SwitchOverlay`（先卸当前再开目标，不叠两层）；战斗 tab = `CloseAllOverlays`。
4. **tab 栏自己的画布 order 50 + GraphicRaycaster**，保证在每个界面上可见可点；它只在关卡态（`IsPrepared=false`）隐藏。

> 保留：两个界面各自的「返回」按钮仍能关掉自己（等价于点战斗 tab），不影响「界面」语义。
> 纯 UI 界面不接管摄像机：两个 overlay 场景的 `Main Camera` 已停用（避开双相机渲染与 AudioListener 重复）。

---

## 5. 分阶段实施计划

> 每阶段独立可验证、独立 commit、独立 push。分支命名 `feat/<阶段名>`。

### 阶段 A：统一「可选关卡」数据源 + 选关 overlay ✅ 已完成（`feat/stage-select-talent-overlays`）

**目标**：选关页列出 2 条 route（教学关 / 正式YJ）；点选 → 写存档「当前关卡」→ 下次开游戏生效。

| # | 任务 | 文件 | 状态 |
|---|---|---|---|
| A1 | `SceneFlowManager`（overlay 加载/卸载栈 + active scene 替换 + `UIOverlayScene` 标记） | `Assets/Scripts/Managers/SceneFlowManager.cs` | ✅ |
| A2 | `StageSelectUI`（列 route + 点选写存档 `SetActiveFakeRouteStage`） | `Assets/Scripts/UI/StageSelectUI.cs` | ✅ |
| A3 | `StageSelect.scene` | `Assets/Scenes/StageSelect.scene` | ✅ |
| A4 | 数据源统一：`StageConfigManager.tutorialStage` 字段 + `GetSelectableRoutes()` | `Assets/Scripts/Core/StageConfigManager.cs` + `MainMenu.scene` | ✅ |
| A5 | 点选行为：只写存档 + 状态提示（不立即切） | `StageSelectUI` | ✅ |
| A6 | `UIOverlayCloseButton`（overlay 通用关闭器） | `Assets/Scripts/UI/UIOverlayCloseButton.cs` | ✅ |

**验证（Play Mode，已跑通）**：
- 选关页列出 2 条路线：教学关[可挑战] / 长坂坡[当前][未解锁]
- 点「教学关」→ `SaveManager.GetActiveFakeRouteStageId()` 变为 9100
- 两个 overlay 的「返回」都能正常卸载自身，下层场景保留

> ⚠️ **本阶段踩到的坑（已写进 §7）**：
> 1. **解锁判断不能用 stageId 数字比大小**。教学关 stageId=9100、正式关 stageId=1，`cfg.stageId <= GetNextAvailableStageId()` 会把教学关永远判为未解锁。改用**列表顺序**（已通关 N 条则前 N+1 条可选）。
> 2. **`LoadSceneAsync` 按名字找场景，overlay 场景必须加进 `EditorBuildSettings`**，否则不报错、就是不加载。

---

### 阶段 B：天赋 overlay ✅ 已完成（同上分支）

**目标**：天赋页可打开、可改天赋（`TalentTreePanel` 本身功能完整，缺的是入口 + 场景）。

| # | 任务 | 文件 | 状态 |
|---|---|---|---|
| B1 | `TalentTree.scene`（复用 `TalentTreePanel`，配置从 MainMenu 场景逐字段拷贝） | `Assets/Scenes/TalentTree.scene` | ✅ |
| B2 | 入口 → `SceneFlowManager.OpenOverlay("TalentTree")` | `MainMenuUI.cs` | ✅ |
| B3 | 「返回」按钮 → `UIOverlayCloseButton` | `TalentTree.scene` | ✅ |

**验证**：天赋页正常渲染（10 按钮 / 21 图 / 22 TMP 文本），「返回」能关闭 overlay。

> 📌 `MainMenu.scene` 里的旧 `TalentTreePanel`（`active=False`）**暂时保留**，等阶段 C 清理。
> 📌 旧 `TalentButton` 的 persistent onClick 是 `TalentTreePanel.Toggle`（**原本是能打开的**，不是死功能）；`MainMenuUI.SetupButtons()` 里的 `RemoveAllListeners()` 把它换成了开 overlay 场景。

---

### 阶段 C：落地页（Battle 场景自举）✅ 已完成（`feat/stage-select-talent-overlays` @ `ff912e9`）

**目标**：小程序加载完直接进 Battle 场景，自动恢复当前进度；玩家点「开始关卡」才正式开打。

| # | 任务 | 文件 | 状态 |
|---|---|---|---|
| C1 | 拆 `FakeRouteRuntime`：新增 `PrepareForEntry()` / `PrepareSceneOnly()` / `RunPrepared()` / `RunPreparedFlow()` / `ResolveStartNode()`；**`Begin()` 一行没改** | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` | ✅ |
| C2 | Battle 场景自举：`Start()` 里 `ResolveActiveRouteFromSave()`（`StageConfigManager.GetSelectableRoutes()` + `GetActiveFakeRouteStageId()`，id 失效回退第一条） | 同上 | ✅ |
| C3 | ~~关掉 `autoStart`~~ → 改成 `Start()` 内**双分支**（见下方偏差说明） | 同上 | ✅ 做法与计划不同 |
| C4 | 「开始关卡」大按钮：新组件 `BattleStartStageButton`，`Ready` 态可点 → `RunPrepared()`；其余状态用 `CanvasGroup` 隐藏 | `Assets/Scripts/UI/BattleStartStageButton.cs` + `Battle.scene` | ✅ |
| C5 | build index 0 = `Battle.scene`（MainMenu 保留在 index 1 当 legacy 后门） | `ProjectSettings/EditorBuildSettings.asset` | ✅ |
| C6 | 就绪态「中央区域可点」轻交互 | — | ⬜ 未做（表现待定，并入阶段 D） |

**与计划的偏差（重要，别按旧计划回改）**：
- C3 **没有**关掉 `autoStart`。关掉会让「从 MainMenu 进 Battle」的老路径也掉进就绪态 → 功能空窗。实际做法是在 `Start()` 里分支：`StageController.fakeRouteStageConfig != null`（MainMenu 通过 `FakeRouteLaunch.PendingConfig` 传的）→ `Begin()` 老行为自动开跑；为 `null` 才自举。**旧路径零回归。**
- `Battle.scene` 必须自带一个 `StageConfigManager`（`tutorialStage` + `fakeRouteStages`，配置与 MainMenu 逐字段一致），否则自举时找不到路线。

**实测（Play Mode）**：
- 进场景：`Phase=Ready`、`IsPrepared=true`、路线=教学关·初见、**敌人 0 个**、`gameplayInputEnabled=false`；已加载场景 `Battle + YJunctionSample`
- 点按钮：`EnteringNode` → 2.5s 后 `Battle`，`stageState=InProgress`，敌人出现 1 个，按钮自动隐藏（alpha 0）
- Console error/warn = 0

> ⚠️ **视觉验收未做**：本环境的截图工具截不到 Overlay UI（§7.15），按钮外观必须人眼确认。

**风险（已消解 / 仍存在）**：
- ~~C1 拆 `Begin()` 影响 `MainMenuUI`~~ → `Begin()` 未改动
- ~~C2 教学关与正式关不在同一列表~~ → 阶段 A4 的 `GetSelectableRoutes()` 已统一
- 仍存在：就绪态下战斗 HUD 是完整可见的（含暂停、QTE 板、技能键）；是否要压暗/隐藏非必要项待美术验收

---

### 阶段 D：底部 tab + 收尾

| # | 任务 | 说明 |
|---|---|---|
| D0 | 就绪态「中央区域可点」轻交互（原 C6） | 点中央给反馈但不推进关卡；也可以直接砍掉，靠大按钮就够 |
| D1 | ✅ **底部 tab 栏**（`ff912e9` 后 `8f30a2e`） | 新组件 `BattleHubTabBar`（`Battle.scene` / `BattleHUD(Canvas)/HubTabBar`）：战斗 / 选关 / 天赋 / 家园。CanvasGroup 控制显隐，只在 `IsPrepared` 时可见可点；选关/天赋走 `SceneFlowManager.OpenOverlay`；家园暂置灰占位 |
| D2 | 顶部队列、金币等状态条 | 复用 `CoinCounter`，补体力（还没有） |
| D3 | ✅ 结算后回落地页（`1cdc7f8` + `87d266d`） | 实际做法不是 `PrepareForEntry` 原地重置，而是 `FakeRouteRuntime.IsHubEntry` 标记 + `StageController.GoToMainMenu()` 里重载 Battle 场景（干净状态、自动重新自举）；回落地页时 `StartFromCheckpoint = true` 接上次进度 |
| D4 | ✅ 删 `MainMenu.scene`（`f39d4ba`） | 删前逐项审计：AudioListener / CameraManager / Directional Light / EventSystem / SafeAreaAdapter / StageConfigManager / SceneFlowManager 在 Battle 都有；TalentTreePanel 在 TalentTree.scene、StageSelectUI 在 StageSelect.scene。`GoToMainMenu()` 改为一律重载 Battle；`mainMenuSceneName` 字段与 `SceneFlowManager.SceneMainMenu` 已删；暂停/战败/胜利三个按钮文案改为「返回」 |

---

### 阶段 E：包体精简（⚠️ 旧数字已推翻，2026-10-20 实测重测）

**实测方法**：根 = build settings 里 enabled 的 6 个场景 + `Assets/Resources/` 全部 36 个资源，用 `AssetDatabase.GetDependencies(root, true)` 求并集（1239 个资产），再比 `Assets/Sprites/Enemy/` 全量。

| 项 | 实测 | 结论 |
|---|---|---|
| `Assets/Sprites/Enemy/` | 417 张 / 117.9 MB（磁盘） | **被引用 405 张 / 112.6 MB；未被引用只有 12 张 / 5.2 MB**（其中 `BOSS.psd` 3.9MB 是源文件） |
| `Assets/Resources/EnemyPrefabs/` | 12 个 prefab | **12 个里有 10 个真在用**：配置里实际出现的 enemyId = `101 / 103 / 104 / 105 / 106 / 107 / 108 / 109 / 1011 / 1021`（扫 139 个 `.asset` 的 `enemyIds` 十六进制字段解码得到） |
| 未用的 prefab | `Enemy_1`、`Enemy_102` | 各自独占贴图仅 0.1 MB / 2.3 MB，删完约省 **2.4 MB** |
| `Tutorial_YJ.unity` | build settings 里的遗留副本 | 全工程搜不到任何引用 → 从 build settings 移除可省 **9.3 MB**（不删文件，可逆） |
| `MainMenu.scene` | 7.7 MB 独有依赖 | 需保留（legacy 入口），不省 |
| 字体 | 旧文档记 36 MB（TMP Examples / 未用字重） | 仍是最大单项，未重新测 |

> ❌ **旧结论作废**：「Sprites/Enemy 114MB 死资源、实际只用 3 个敌人」是错的。敌人美术几乎全部进包（112.6 MB），真正的浪费只有 2.4 MB。**不要再按旧文档去批量删敌人贴图——那会删掉 104/107/108 这些在用资源。**

> `EnemyPool` 是 `Resources.Load("EnemyPrefabs/...")` 按名字加载（`EnemyPool.resourcesPrefabPath`），所以依赖分析**无法**剪掉 prefab 包裹的贴图——想省包体只能删 prefab 本身。这也违反 `anti-ghost-reference.md`，属未来的架构改造项。

> ⚠️ 扫描还发现配置里出现了 **`enemyId = 999`（7 处）但 `Resources/EnemyPrefabs/` 里没有对应 prefab** → 运行时会走 `EnemyPool` 的临时 Cube fallback。如果 999 是特意的占位符就没事；如果不是，需要补 prefab 或改配置。

> 待授权动作：① 删 `Enemy_1` / `Enemy_102` prefab 及其独占贴图（~2.4MB）；② 把 `Tutorial_YJ.unity` 从 build settings 移除（~9.3MB，可逆）。两项都需要用户点头。

---

### 阶段 F：自举路径收尾修复（2026-10-20，`fb5279b` + `46e7c19`）

阶段 C/D 只验到了「进场景 = Ready + 点按钮能开打」，**结算、冷启动与选关的闭环从来没跑过**。四个问题同源：落地页自举没有 MainMenu 传参，`StageController.fakeRouteStageConfig` 是 null。

| # | 问题 | 根因 | 修法 |
|---|---|---|---|
| F1 | 通关不结算（无 Victory、无通关铜钱、不记录已通关） | `CompleteFakeRouteStage()` 首行 `if (fakeRouteStageConfig == null) return;`，自举模式下永远命中 | `PrepareForEntry()` 里 `stageController.fakeRouteStageConfig = routeConfig;`（同时修好 `RestartStageCoroutine` / `OnRestartButton` 的同源分支） |
| F2 | 冷启动不接 checkpoint（回 `startNode`、丢血量/升级） | `StartFromCheckpoint` 是 static，只有 `GoToMainMenu()` 与 HUD 重来会置 true；进程首次启动是 false | `ResolveStartNode()` 改成 `StartFromCheckpoint || IsHubEntry`，落地页路径无条件试恢复；有快照 = 该关没打完（通关时已清） |
| F3 | 打完教学关回落地页还是教学关 | 没人把存档「当前关卡」推到下一条 | `FinishRoute()` 新增 `AdvanceActiveStageAfterClear()`：`GetSelectableRoutes()` 里当前条目的下一条；已是最后一条则不动 |
| F4 | 选关后落地页仍是旧关卡 | 旧行为是「只写存档，下次进入生效」 | `StageSelectUI.OnRouteSelected()` 写档后直接 `FakeRouteRuntime.Instance.PrepareForEntry(cfg)` 换路线，再关 overlay |

**资产引用补齐（`46e7c19`）**：`Tutorial_Route.asset` 及其 Node/Battle/Choice、`Formal_*_Battle`、`tutorial_t0_open/choice`、`StageSelectUI.cs.meta`、`UIOverlayCloseButton.cs.meta` 一直是 untracked，而 `Battle.scene` 的 `StageConfigManager.tutorialStage` 已经指向它们。clean clone 会直接丢默认路线。

**Play Mode 实测（一次跑完，存档先备份后还原）**：

| 项 | 结果 |
|---|---|
| 基线 | `Phase=Ready`、`IsPrepared=true`、`IsHubEntry=true`、`sc.route=教学关` |
| 冷启动接档 | 植入 `checkpoint=J1 / level=7 / hp=42`、`StartFromCheckpoint=false` → 重载 Battle 后 `preparedStartNode=J1`、`_restoredFromCheckpoint=true`、`level=7`、`hp=42`、`coins=99` |
| 选关立即生效 | overlay 加载 → `OnRouteSelected(教学关)` → overlay 关闭、`active=9100`、`sc.route=教学关`、回 Ready |
| 落地页 UI | 等 Update 跑过后：`startBtnAlpha=1.00 blocks=True interactable=True`、`tabAlpha=1.00`、局内 HUD 隐藏 15/15、空引用 0 |
| 通关收尾 | `StartCoroutine(FinishRoute)` → `phase=Completed`、tutorial 快照已清、`IsStageCleared(9100)=true`、`active=9100→1` |
| 回落地页 | `GoToMainMenu()` → `scene=Battle`、`Phase=Ready`、按钮与 tab 均可见 |
| 开打冒烟 | 点「开始关卡」→ `Phase=Battle`、`gameplayInputEnabled=true`、`enemies=1`、`stageState=InProgress` |
| Console | warn/error = 0 |

**未覆盖**：真实手打到通关（用 `FinishRoute` 驱动收尾）、战败重来（只做了代码路径确认）、就绪态观感（截图工具截不到 Overlay UI）。

---

### 阶段 G：二级页面接管全屏（2026-10-20，`1210dc5`）

用户验收反馈：切换到选关/天赋页时，「开始游戏」与头像 UI 没有消失。

| # | 问题 | 根因 | 修法 |
|---|---|---|---|
| G1 | 落地页 UI 压在二级页上面 | `BattleHUD(Canvas)` 是 Screen Space - Overlay（21），二级页是 Screen Space - Camera（10/11）；Overlay 画布永远画在摄像机画布之后 | 两个 overlay 画布改 Screen Space - Overlay，order 40 / 41 |
| G2 | 天赋页无全屏底 | 只有 `TalentTreePanel` + `CloseButton`，背景透出战斗场景 | 补全屏 `Background`（与选关页同款颜色） |
| G3 | hub 内容不消失 | 只按 `IsPrepared` 控显隐，没有「二级页打开」这个维度 | `HubHudModeSwitch.hubOnly`（头像/金币）+ 按钮/tab 栏 AND `!HasOverlay` |
| G4 | overlay 场景自带摄像机 | 纯 UI 页却持有 Main Camera，两机同时渲染 | 停用两个 overlay 场景的 `Main Camera` |

**Play Mode 实测**：

| 项 | 结果 |
|---|---|
| 基线 | 头像/金币 active、开始按钮 alpha 1.00、tab alpha 1.00 |
| 选关页打开 | canvas `ScreenSpaceOverlay order=40`；头像/金币 `active=false`；开始按钮/tab alpha 0.00 |
| 选关页关闭 | 头像/金币 active、按钮/tab alpha 1.00（全部还原） |
| 天赋页打开 | canvas `order=41`；`Background` 就位（0.05/0.06/0.09/0.96）；hub 同样让位 |
| 天赋页关闭 | 全部还原，`Phase=Ready` |
| Console | warn/error = 0 |

**未覆盖**：未在微信小程序/真机上确认 Overlay 画布层级；选关页点击选中后的视觉反馈只是状态文字（选完立即关页）。

> ⚠️ 本阶段第 3 条「tab 栏也 AND `!HasOverlay`」在阶段 H 已回退：tab 栏是界面导航，必须常驻。

---

### 阶段 H：tab 栏改为界面导航（2026-10-20，`d3676c3`）

用户澄清：**选关与天赋是「界面」，不是「二级窗口」**。所以底部 tab 不是装饰，是界面导航，必须常驻；阶段 G 把 tab 栏一起藏掉是错的。

| # | 任务 | 文件 | 状态 |
|---|---|---|---|
| H1 | `HubTabBar` 挂自己的 Canvas（`overrideSorting`，order 50）+ `GraphicRaycaster`，画在选关(40)/天赋(41)之上 | `Battle.scene` / `HubTabBar` | ✅ |
| H2 | tab 栏可见性回到只看 `IsPrepared`（不再 AND `!HasOverlay`） | `BattleHubTabBar.cs` | ✅ |
| H3 | tab 行为：战斗 → `CloseAllOverlays`；选关/天赋 → `SwitchOverlay`（先卸当前再开目标） | `BattleHubTabBar.cs` + `SceneFlowManager.SwitchOverlay` | ✅ |
| H4 | 当前 tab 高亮（`activeTabTint` 1/0.86/0.55，非当前纯白） | `BattleHubTabBar.cs` + `Battle.scene` | ✅ |
| H5 | 两个界面的全屏底改为完全不透明（alpha 1） | `StageSelect.scene` / `TalentTree.scene` | ✅ |

**Play Mode 实测**：

| 步骤 | 结果 |
|---|---|
| 战斗界面基线 | `tabAlpha=1.00`、战斗高亮、头像/金币显示、开始按钮 alpha 1.00 |
| 点选关 tab | `overlay=StageSelect`、`tabAlpha=1.00`（常驻）、选关高亮、头像/金币隐藏、开始按钮 alpha 0.00 |
| 选关界面点天赋 tab | 选关已卸载=True、天赋加载、天赋高亮、tab 仍常驻 |
| 天赋界面点战斗 tab | 天赋已卸载=True、头像/金币/开始按钮全部还原、战斗高亮、`Phase=Ready` |
| Console | warn/error = 0 |

**未覆盖**：tab 栏在界面上的视觉叠放（截图工具截不到 UI，需人眼）；天赋树面板内容可能与底栏区域重叠（未调）。

---

## 6. 验收步骤

### 阶段 A/B
1. 进 Play Mode → 主菜单 → 点「选关」→ overlay 弹出，列出「教学关·初见」「长坂坡·卷轴节点预览」
2. 点「正式YJ」→ `SaveManager.GetActiveFakeRouteStageId()` == 1
3. 点「天赋」→ overlay 弹出，节点/连线/金币正常，能关闭

### 阶段 C（已跑：4/5/7/9/10；未做：6/8）
4. ✅ **build index 0 = Battle.scene**，直接 Play 就是战场
5. ✅ 进战场时 route 表现已就位（`Battle + YJunctionSample`），**敌人 0 个**（Q1 要求）
6. ⬜ 点中央 → 有反馈但不推进关卡（原 C6，未实现，可能直接砍）
7. ✅ 点「开始关卡」→ `EnteringNode` → `Battle`、`stageState=InProgress`、敌人出现、按钮自动隐藏
8. ✅ 结算后回就绪态（D3 已实现：重载 Battle → `Phase=Ready`、敌人 0、路线不变）
9. ✅ 退出重进恢复同一 checkpoint：进关后 `snapshot.checkpointNodeId=N1` → `GoToMainMenu` → 落地页 `_restoredFromCheckpoint=true`、`preparedStartNode=N1`
10. ✅ 选关页换路线：`SetActiveFakeRouteStage(1)` → `GoToMainMenu` → 落地页加载「长坂坡·卷轴节点预览」，`Phase=Ready`

---

## 7. 已知坑（务必先看）

1. **`unity_recompile` 可能假成功**：判据 = `Library/ScriptAssemblies/Assembly-CSharp.dll` 的 mtime 晚于源码，或用反射确认运行中类型有新成员。参考 `tutorial-beat-and-formal-config-handoff-20261019.md` §9.1
2. **场景 SerializedObject 改动不一定落盘**：改完必须 grep 磁盘上的 `.scene` 复核。参考同文 §9.2
3. **给枚举加新值要先编译再写场景**：`enumValueIndex` 在未编译前会被忽略。参考同文 §9.3
4. **`.meta` 的 GUID 是 base64、prefab 引用是 hex**：文本 grep 比对 GUID 会得到错误结论，依赖分析必须用 `AssetDatabase.GetDependencies`
5. **Locus hot reload 对嵌套类型有编译限制**：在 `InputManager` 里加嵌套 `IGestureFilter` 接口 + 嵌套 filter 类会报 `CS0120`（patch 子类拿不到 private 成员）。**改成同类的实例方法即可**（见 `refactor/p1-input-gesture-gates`）
6. **Editor 场景脏状态会自己写回磁盘**：`DisplacementDebugTool` 那种「脚本默认 true」的设计会被复活。修法是**场景字段 + 脚本默认值双修**
7. **`FakeRouteLaunch.PendingConfig` 是 MainMenu 时代的跨场景传参**：落地页改造后 Battle 要自举读存档，不能依赖它
8. **教学关不在 `StageConfigManager.fakeRouteStages` 里**：插到列表头部会改变 `fakeRouteStageConfigs[0]` 的语义（正式关），只能加末尾或用独立字段
9. **解锁判断不能用 stageId 数字比大小**（阶段 A 踩到）：教学关 stageId=9100、正式关 stageId=1，`cfg.stageId <= SaveManager.GetNextAvailableStageId()` 会把教学关永远判为「未解锁」。**改用列表顺序**（已通关 N 条则前 N+1 条可选）
10. **`LoadSceneAsync` 按名字找场景，overlay 场景必须加进 `EditorBuildSettings`**：不加的话**不报错、就是不加载**，很难查（阶段 A 踩到）
11. **`FindFirstObjectByType<T>()` 找不到 inactive 对象**：`TalentTreePanel` 在 MainMenu 场景里是 `active=False`，要用 `FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)`（阶段 B 踩到）
12. **跨场景拷贝组件配置要分两步**：`SerializedObject` 绑定的目标对象在切换场景后会被销毁 → 先把源值取到数组，再开目标场景写入（阶段 B 踩到）
13. **`unity_execute` 的 `request_editor_status` 不要写死**：Play Mode 进/退和 domain reload 抢时序，写死成 `playing` 时会报「Status Changed: now editing」并**直接结束这一轮**，看起来就像「任务又断了」。正确做法：先确认状态（用 `unity_set_play_mode` 幂等推进到目标态），再发读取调用
14. **`unity_capture_viewport` 在本环境截不到 Overlay UI**（阶段 C 踩到）：把 `StartStageButton` 和 HUD 底栏都改成洋红、甚至把主相机 `clearFlags=SolidColor` + 背景洋红，截图里**一像素洋红都没有**；连续两次截图几乎一模一样。结论：它不能用来做 UI 像素验证。UI 验证改用「组件状态回读 + `EventSystem.RaycastAll` + 人眼」
15. **工作区脏时提交自己的改动**（阶段 C 踩到）：`FakeRouteRuntime.cs` 里混着上一轮未提交的 `BreakableProp` 改动（而 `Assets/Scripts/Prop/` 本身还是 untracked）。直接 `git add` 会提交一个引用 untracked 类型的坏 commit。做法：`git diff <file> > full.patch` → `sed -n '1,Np'` 只留自己的 hunk → `git apply --cached mine.patch`，然后 `git show :<file> | grep -c BreakableProp` 必须为 0（阶段 F 实测：这套 CRLF 环境下 `git apply --cached` 会报 `patch does not apply`，改用「`git show HEAD:<path>` 取出 → 脚本套上自己的替换写成 mine.cs → 覆盖工作区文件 `git add` → 把完整工作区文件覆盖回来」，`git show :./<path>` 注意要在子目录里加 `./` 前缀）
16. **无 sprite 的 `Image` 不要赌它一定出网格**：本项目所有 UI 一律配精灵（按钮用 `MM_Btn_Primary` / `Secondary` + `SpriteSwap`，pressed 用 `*_Pressed`）。落地页大按钮直接复用这套，尺寸/字号也抄主菜单（479×172、TMP 54、字色 `0.97/0.92/0.78`）
17. **引用了 untracked 资产**（D4 收尾时发现）：`Assets/Sprites/UI/MainMenu` / `Talent` / `Tutorial` 三个目录一直是 untracked，但 `908e7c0` 的 overlay 场景和落地页按钮已经在用它们的 GUID —— **新克隆会全是丢引用**。已补提交（`31d2937`）。提交前先 `git status --short` 看场景/预手引用到的目录是否都是 tracked
18. **删场景前必须做「能力对比审计」**（D4）：把待删场景的根对象组件列出来，逐项到保留场景里找对应。最容易漏的是不显眼的基础设施：`AudioListener` / `EventSystem` / `SafeAreaAdapter`（刘海适配）/ `CameraManager` / `Directional Light`。MainMenu 审计结果见阶段 D4
19. **跨文件的同一改动不能按 hunk 拆提交**：`BattleHUD` 引用 `StageController.blockRestartOnDefeat`，两边的改动拆到两个 commit 会让中间那个 commit 编译不过。**这种必须成组提交**，并在 commit message 里说明带上了什么
20. **每次进/出 Play Mode 都会注入 `[Unity Editor Status Changed]`，当前回合立即结束**：25 分钟内自己开关 5 次，就把工作切成了 9 个短回合。**批处理**：改代码 → 编译 → 静态检查 → 进一次 Play 把所有验证项一次跑完 → 出来 → 提交（详见 `memory/work-loop-discipline.md`）
21. **Play Mode 里 `WaitFrames` 的帧很快**：实测 90 帧 ≈ 0.32s（≈280fps），等 `WaitForSecondsRealtime(0.5f)` 的收尾逻辑用 60 帧只等到 0.2s，会看到「`phase=Completed` 但快照没清、没记通关」的假故障。**用 `Time.realtimeSinceStartup` 量一下再下结论**（阶段 F 踩到）
22. **自举模式下 `StageController.fakeRouteStageConfig == null` 是一串隐藏 bug 的总开关**：结算、战败重来、重来按钮都按这个字段分支。任何新增的「按这个字段判断是否跑卷轴路线」的逻辑，落地页里都会走 else 分支
23. **`StartFromCheckpoint` 这类 static 旗标扛不住进程重启**：落地页语义是「进游戏 = 当前进度」，判定要用「有没有合法快照」而不是「上一轮有没有置旗标」
24. **同帧读取 UI 状态会读到旧值**：`IsPrepared` 变 true 的那一帧 `BattleStartStageButton` / `BattleHubTabBar` / `HubHudModeSwitch` 的 `Update` 还没跑，此刻读 `alpha` / `interactable` 会得到 0 / false。验证脚本要额外等几帧再读（阶段 F 踩到）
25. **验证会污染存档（PlayerPrefs）**：`SetActiveFakeRouteStage` / `SaveFakeRouteSnapshot` / `MarkStageCleared` 都直写 PlayerPrefs。做法：开工前 `JsonUtility.ToJson(SaveManager.Load())` 备份、结束时 `SaveManager.Save(FromJson(...))` 还原（阶段 F 用的就是这个）
26. **Screen Space - Overlay 画布永远压在 Screen Space - Camera 画布之上**，与 sortingOrder 无关（`BattleHUD` 21 vs 二级页 10/11 → 二级页永远在下）。additive 叠 UI 页面时，**两边必须是同一种 render mode**，否则只能靠隐藏来实现「不叠在一起」（阶段 G 踩到）

---

## 8. 已完成的前置工作（可直接依赖）

| 分支 | 内容 | 状态 |
|---|---|---|
| `fix/stab-pushback-three-bugs` | 重击突刺推退 3 个 bug（debug tool / InputManager combo gate / AttackSystem override 时序）+ `tutorial-beat-and-formal-config-handoff-20261019.md` §13 | ✅ pushed |
| `refactor/p0-displacement-debug-editor-only` | `DisplacementDebugTool` 用 `#if UNITY_EDITOR` 包住 | ✅ pushed |
| `refactor/p1-input-gesture-gates` | `InputManager.ProcessGesture` 拆成 5 个独立 gate 方法 | ✅ pushed |
| `feat/stage-select-talent-overlays` | **阶段 A+B**：`SceneFlowManager` / `StageSelectUI`+`StageSelect.scene` / `TalentTree.scene` / `UIOverlayCloseButton` / `StageConfigManager.tutorialStage` / `MainMenuUI` 接线 / build settings | ✅ pushed（`908e7c0`） |
| `feat/stage-select-talent-overlays` | **阶段 C**：`FakeRouteRuntime` 拆 Prepare/Run + Battle 自举读存档 + `BattleStartStageButton` + Battle 自带 `StageConfigManager` + build index 0 = Battle | ✅ pushed（`ff912e9`） |
| `feat/stage-select-talent-overlays` | **阶段 D1**：`BattleHubTabBar` + `Battle.scene/HubTabBar`（战斗/选关/天赋/家园） | ✅ pushed（`8f30a2e`） |
| `feat/stage-select-talent-overlays` | **阶段 D3**：`IsHubEntry` + `GoToMainMenu` 回落地页（含 `StartFromCheckpoint=true` 接上次进度） | ✅ pushed（`1cdc7f8`、`87d266d`） |
| `feat/stage-select-talent-overlays` | **就绪态/关卡态 HUD 拆分**：`HubHudModeSwitch`（就绪态隐藏 15 个局内节点，头像/金币常驻） | ✅ pushed（`8b780cd`） |
| `feat/stage-select-talent-overlays` | **阶段 D4**：删 `MainMenu.scene` + `MainMenuUI.cs`，`GoToMainMenu()` 一律回落地页，按钮文案改「返回」 | ✅ pushed（`f39d4ba`） |
| `feat/stage-select-talent-overlays` | 补提交 UI 精灵（`Sprites/UI/MainMenu` / `Talent` / `Tutorial`，之前一直 untracked 但已被场景引用） | ✅ pushed（`31d2937`） |

---

## 9. 给接手者的建议路径

1. 读 §0.1（当前状态）——能省掉一半核对时间
2. 读 §3（现状机制）——尤其是 §3.3 开关表和 §3.4 存档 API；§3.2 已作废（MainMenu 时代）
3. 读 `Locus/knowledge/plan/tutorial-beat-and-formal-config-handoff-20261019.md`（本项目的关卡/配置现状 + 踩坑记录）
4. 读 `Locus/knowledge/design/anti-ghost-reference.md`（数据引用规范：Inspector 拖拽，不准 Resources/字符串查找）
5. 阶段 A/B/C/D/F/G/H 已全部完成，**不要再重做**。剩下的只有：D0 中央轻交互（可砍）、D2 体力状态条（无设计）、阶段 E 字体那 36MB
6. 验证落地页手感时：进 Play Mode 前先把存档 `SaveManager.SetActiveFakeRouteStage(9100)` 或 `1`，两种路线都要跑一遍
7. 提交前先看 §7.15 / §7.17（工作区脏时的 hunk 拆分 + untracked 资产）——`FakeRouteRuntime.cs` / `FakeRouteStageSaveSnapshot.cs` 里还挂着可破坏道具的未提交改动
8. 要动结构（拆程序集 / 收拾上帝类 / 清路线驱动）先读 `Locus/knowledge/design/architecture-decoupling-analysis.md`：那里有实测基线、六个结构性问题与 0-4 批改造顺序，以及“工作区必须先收敛”的前置条件
9. **结构改造已于 2026-10-20 落地**（41 个提交，结束于 `afbd41e`）：交接与验收清单见 `Locus/knowledge/plan/architecture-optimization-handoff-20261020.md`

---

## 10. 关键文件索引

| 内容 | 路径 |
|---|---|
| 场景流转（overlay 栈） | `Assets/Scripts/Managers/SceneFlowManager.cs` |
| 选关页 | `Assets/Scripts/UI/StageSelectUI.cs`、`Assets/Scenes/StageSelect.scene` |
| 天赋页 | `Assets/Scripts/UI/TalentTreePanel.cs`、`Assets/Scenes/TalentTree.scene` |
| 主菜单（待废弃） | `Assets/Scripts/UI/MainMenuUI.cs`、`Assets/Scenes/MainMenu.scene` |
| 关卡启动总控 | `Assets/Scripts/Managers/StageController.cs` |
| 卷轴路线运行时（已拆 Prepare/Run） | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` |
| 「开始关卡」按钮 | `Assets/Scripts/UI/BattleStartStageButton.cs`（`Battle.scene` / `BattleHUD(Canvas)/StartStageButton`） |
| 跨场景传参（MainMenu 时代） | `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` |
| 路线表现层 | `Assets/Scripts/Core/ScrollRoutePresenter.cs` |
| 存档 / 当前进度 | `Assets/Scripts/Core/SaveManager.cs` |
| 输入 | `Assets/Scripts/Player/InputManager.cs` |
| 战斗 UI | `Assets/Scripts/UI/BattleHUD.cs` |
| 关卡配置 | `Assets/Scripts/Core/StageConfigManager.cs`、`Assets/RouteData/` |
