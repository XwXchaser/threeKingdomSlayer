---
id: kd_mainmenu-hub-refactor-plan-20261020
injectMode: inherit
injectAgents:
- '*'
summary: 主菜单/落地页重构方案 —— 落地即 Battle 场景、关卡内容进场景即加载、选关与天赋降级为二级 overlay；含包体实测数据与分阶段实施计划
aiEditMode: inherit
---

# 主菜单 / 落地页重构方案（2026-10-20）

> 目标平台：微信小程序。加载完毕后玩家**直接进入 Battle 场景**，看到的就是当前进度的关卡起始场景。
> 本文是**可交接的实施计划**：现状机制 → 目标形态 → 分阶段任务 → 验收 → 已知坑。
> 任何 AI 接手时，建议从 §3 的「现状机制」和 §5 的「阶段划分」读起。

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

| # | 场景 | 依赖数 | 贴图数 | 依赖体积 | 状态 |
|---|---|---|---|---|---|
| 0 | `Assets/Scenes/MainMenu.scene` | 101 | 25 | 45.7 MB | **启动场景**（要废弃） |
| 1 | `Assets/Scenes/Tutorial/Tutorial_YJ.unity` | 571 | 247 | 154.8 MB | **几乎无用**（见 §3.4） |
| 2 | `Assets/Scenes/Battle.scene` | 462 | 183 | 92.2 MB | 真正的战场 |
| 3 | `Assets/Experiments/CurvedScroll/YJunctionSample.unity` | 146 | 60 | 59.0 MB | **route 场景**（表现层加载） |
| 4 | `Assets/Scenes/StageSelect.scene` | 9 | 0 | 0.0 MB | 本次新建 |

> 依赖体积是各场景独立计算，共享资产会重复计。**并集**才是真实包体。

### 3.2 现在的启动→开打链路

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
| **就绪态** | 进 Battle 场景，自动读进度 → Prepare | route 场景背景就位；中央可点（轻交互）；「开始关卡」大按钮可点；底部 tab 可用 |
| **关卡态** | 点「开始关卡」 | `FakeRouteRuntime` 跑 route：对话 → 战斗 → 奖励 → 下一节点 → 结算 → 回就绪态 |

> 用户明确：**敌人不在就绪态摆出来**（Q1 答案），所以就绪态中央可以是空场 + 交互反馈，或后续再补表现。

### 4.3 底部 tab 规划

| tab | 内容 | 形态 |
|---|---|---|
| 战斗 | 当前关卡（就绪态 / 关卡态） | Battle 场景本体 |
| 选关 | 2 条 route（教学关 / 正式YJ） | overlay 场景 `StageSelect.scene` |
| 天赋 | `TalentTreePanel` | overlay 场景 `TalentTree.scene` |
| 家园 | 待定 | 先留占位 |

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

### 阶段 C：落地页（Battle 场景自举）★ 核心

**目标**：小程序加载完直接进 Battle 场景，自动恢复当前进度；玩家点「开始关卡」才正式开打。

| # | 任务 | 文件 | 说明 |
|---|---|---|---|
| C1 | **拆分 `FakeRouteRuntime.Begin()`** | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` | 拆成 `PrepareForEntry(route, startNode)`（只做 `movementPresenter.Prepare`）与 `RunPrepared()`（走节点流程）。`Begin()` 保留为「Prepare + Run」的组合，避免动 `MainMenuUI` 现有调用点 |
| C2 | **Battle 场景自举「当前进度」** | 新增 `BattleEntryBootstrap.cs` 或扩 `StageController` | 进 Battle 场景时：读 `SaveManager.GetActiveFakeRouteStageId()` → 找到 route config → 读 checkpoint → `PrepareForEntry`。**不能依赖 `FakeRouteLaunch.PendingConfig`（那是 MainMenu 时代的跨场景传参）** |
| C3 | **关闭 `FakeRouteRuntime.autoStart` 的自动 Begin** | `Battle.scene`（`Route Flow (Scroll)` 组件的 `autoStart`） | 改为 `false`；改为由 C4 的「开始关卡」按钮显式触发 |
| C4 | **「开始关卡」大按钮** | `Battle.scene` + 新 UI 脚本 | 参考图「捕捉」位。点击 → `FakeRouteRuntime.RunPrepared()`（或 `Begin(route)`，取决于 C1 的拆法） |
| C5 | **build index 0 改成 Battle.scene** | `ProjectSettings/EditorBuildSettings.asset` | `MainMenu.scene` 移出 |
| C6 | **「中央区域可点」轻交互** | `InputManager` / 新脚本 | 就绪态点中央 → 给反馈（不推进关卡）。**具体表现待定** |

**风险**：
- C1 拆 `Begin()` 会影响 `MainMenuUI` 现有调用点 → 用「保留 `Begin()` = Prepare + Run」的方式保证零回归
- C2 的 route 查找：教学关与正式关不在同一个列表里（§3.5）→ 先做 A4 的数据源统一
- C3 关掉 `autoStart` 后，**从 MainMenu 进 Battle 的老路径也会变成就绪态** → 需要 C4 的按钮兜住，或保留一条 legacy 直通开关

---

### 阶段 D：底部 tab + 收尾

| # | 任务 | 说明 |
|---|---|---|
| D1 | Battle 场景底部 tab 栏（战斗 / 选关 / 天赋 / 家园） | 4 个按钮，切 overlay |
| D2 | 顶部队列、金币等状态条 | 复用 `CoinCounter`，补体力（还没有） |
| D3 | 结算后回落地页（而不是回 MainMenu） | `StageController.GoToMainMenu()` → 改成回就绪态 |
| D4 | 删 `MainMenu.scene` / `MainMenuUI` / 临时 debug 面板 | 定稿清理 |

---

### 阶段 E：包体精简（可与 A~D 并行）

**实测数据**（`AssetDatabase.GetDependencies`，2026-10-20）：

| 项 | 体积 | 动作 | 预计省 |
|---|---|---|---|
| `MainMenu.scene` 独有依赖 | **7.7 MB / 16 张贴图** | 废弃场景后自动不打包 | ~5-6 MB（`MM_Background` 2.7MB + `zhangfei_idle×4` + `MM_Btn×4`；Talent 的 `TT_*` 5 张要被 `TalentTree.scene` 复用则仍进包） |
| `Tutorial_YJ.unity` 独有依赖 | **9.3 MB / 7 张贴图** | 从 build settings 移除 | 9.3 MB（含老 `Enemy1` 贴图 + stronghold 道具） |
| `Assets/Resources/UI/` | 4.6 MB（`UpgradeChoice` 3.1 + `ItemDiscard` 1.5） | 战斗内 UI 必需 | — |
| **`Assets/Sprites/Enemy/`** | **114 MB / 416 张** | 实际只用 3 个敌人（1011/1021/109）。**其余 400+ 张是死资源** | **最大头，单独排期** |
| `Assets/Experiments/CurvedScroll/Art/` | 118.5 MB | 大部分只被非 build 场景引用，不进包；但 `YJunctionSample` 用到的那 59MB 是必需的 | — |

> ⚠️ 删死资源前必须确认引用关系（`anti-ghost-reference.md`：禁 Resources 查找 / 禁字符串 ID 查找）。建议逐张用 `AssetDatabase.GetDependencies` 反查，而不是按文件名猜。

> 历史上另有一份包体方案：`plan/package-size-optimization.md`（Android 50MB→8-12MB 目标，字体/TMP Examples 为主）。**本项目现目标是微信小程序，两边目标不同但字体那 36MB 的结论仍然适用。**

---

## 6. 验收步骤

### 阶段 A/B
1. 进 Play Mode → 主菜单 → 点「选关」→ overlay 弹出，列出「教学关·初见」「长坂坡·卷轴节点预览」
2. 点「正式YJ」→ `SaveManager.GetActiveFakeRouteStageId()` == 1
3. 点「天赋」→ overlay 弹出，节点/连线/金币正常，能关闭

### 阶段 C
4. **build index 0 = Battle.scene** → 小程序启动直接进战场，无主菜单
5. 进战场时：背景/滚动场景已就位（route 表现加载完），**但没有敌人在场**（Q1 要求）
6. 点中央 → 有反馈但不推进关卡
7. 点「开始关卡」→ route 流程跑起来：对话 → 战斗 → 奖励 → 下一节点 → … → 结算
8. 结算后回到就绪态（不是回主菜单）
9. 退出重进 → 恢复同一个 route + 同一个 checkpoint 节点
10. 在选关页选了另一条 → 退出重进 → 加载的是新选的那条

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

---

## 8. 已完成的前置工作（可直接依赖）

| 分支 | 内容 | 状态 |
|---|---|---|
| `fix/stab-pushback-three-bugs` | 重击突刺推退 3 个 bug（debug tool / InputManager combo gate / AttackSystem override 时序）+ `tutorial-beat-and-formal-config-handoff-20261019.md` §13 | ✅ pushed |
| `refactor/p0-displacement-debug-editor-only` | `DisplacementDebugTool` 用 `#if UNITY_EDITOR` 包住 | ✅ pushed |
| `refactor/p1-input-gesture-gates` | `InputManager.ProcessGesture` 拆成 5 个独立 gate 方法 | ✅ pushed |
| `feat/stage-select-talent-overlays` | **阶段 A+B**：`SceneFlowManager` / `StageSelectUI`+`StageSelect.scene` / `TalentTree.scene` / `UIOverlayCloseButton` / `StageConfigManager.tutorialStage` / `MainMenuUI` 接线 / build settings | ✅ pushed |

---

## 9. 给接手者的建议路径

1. 读 §3（现状机制）——尤其是 §3.3 开关表和 §3.4 存档 API
2. 读 `Locus/knowledge/plan/tutorial-beat-and-formal-config-handoff-20261019.md`（本项目的关卡/配置现状 + 踩坑记录）
3. 读 `Locus/knowledge/design/anti-ghost-reference.md`（数据引用规范：Inspector 拖拽，不准 Resources/字符串查找）
4. 从**阶段 A** 开始（已有半成品，最快出可见成果）
5. 阶段 C 是核心难点，`Begin()` 的拆分要小心，建议先写一个 Play Mode 验证脚本：进 Battle → 检查 `movementPresenter.IsSceneReady` → 点按钮 → 检查 route 是否跑起来

---

## 10. 关键文件索引

| 内容 | 路径 |
|---|---|
| 场景流转（overlay 栈） | `Assets/Scripts/Managers/SceneFlowManager.cs` |
| 选关页 | `Assets/Scripts/UI/StageSelectUI.cs`、`Assets/Scenes/StageSelect.scene` |
| 天赋页 | `Assets/Scripts/UI/TalentTreePanel.cs`、`Assets/Scenes/TalentTree.scene` |
| 主菜单（待废弃） | `Assets/Scripts/UI/MainMenuUI.cs`、`Assets/Scenes/MainMenu.scene` |
| 关卡启动总控 | `Assets/Scripts/Managers/StageController.cs` |
| 卷轴路线运行时（**要拆 Begin 的地方**） | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` |
| 跨场景传参（MainMenu 时代） | `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` |
| 路线表现层 | `Assets/Scripts/Core/ScrollRoutePresenter.cs` |
| 存档 / 当前进度 | `Assets/Scripts/Core/SaveManager.cs` |
| 输入 | `Assets/Scripts/Player/InputManager.cs` |
| 战斗 UI | `Assets/Scripts/UI/BattleHUD.cs` |
| 关卡配置 | `Assets/Scripts/Core/StageConfigManager.cs`、`Assets/RouteData/` |
