---
id: kd_72064c32-919f-4d37-af07-e80bc6f27a16
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 落地页入口 + 关卡续跑：两处修正（2026-10-27）

> 用户反馈的两个问题：① 天赋页完全无法进入；② 从关卡退出后应该从**关卡开头**继续，而不是从节点处续跑。
> 本文记录**根因**与**修法**（代码已落地并自测通过）。

---

## 0. 一句话

- 问题1 的真因不是天赋页坏了，而是**落地页上所有"看起来像导航"的入口都是死的**（四宫格键被做成置灰占位、顶栏 TabBar 被关掉），而**唯一能用的入口是右下两张小卡片**。
- 问题2 的真因是 `FakeRouteLaunch.StartFromCheckpoint` 这个**静态开关永不复位**，且"退出关卡回落地页"会主动把它打开 —— 与代码自己注释里写的意图（重进游戏应从关卡开头开始）**正好相反**。

---

## 1. 问题1：天赋页 / 选关入口

### 1.1 排查结论（实测）

| 项 | 结论 |
|---|---|
| 天赋页本身 | **没坏**：overlay 加载成功（`TalentTree` 场景 isLoaded，Canvas order 41 高于落地页 25）、渲染正常（8 个节点 + 价格 + 返回 + 铜钱） |
| 点节点购买 | **能用**：点「指向突刺」→ 存档 `unlockedTalents=[talent_unlock_c2]`，铜钱 300 → 200 |
| 选关页 | 同理正常（`StageSelect` overlay 加载成功） |
| **左下四宫格键** | **全部 `interactable=false` + 置灰** → 点了没反应 |
| **顶栏 TabBar**（战斗/选关/天赋/家园） | **整个对象 `active=false`**（落地页重构后被关掉） |

### 1.2 设计文档的权威口径（`plan/hub-ui-reference-generation-handoff-20261024.md`）

- "左下底部：四宫格菜单占位键，**置灰，无名称，无指定功能**"
- "关卡入口与天赋卡在**右下**上下成组" → **选关/天赋的正式入口就是右下两张卡**（`HubStageEntry` / `HubTalentEntry`）
- "面板里只允许一个动态元素" ⇒「开始关卡」的呼吸动效是唯一的

### 1.3 修法（用户 2026-10-27 决定）

| 项 | 处理 |
|---|---|
| 四宫格键 | **从面板上撤下**（对象保留、置 inactive）：它们无名称无功能，玩家当按钮点却没反应 —— 这正是"天赋页无法进入"的来源。`HubUiRestructure.BuildMenuKeys` 也一并改了（重跑编辑器工具不会把它们复活） |
| 选关 / 天赋入口 | 保持**右下两张卡**（有文字标签、正常外观、非灰化） |
| 呼吸动效 | 两张 Side 卡的 `UiButtonMotion.idleEnabled` 本来就是 `false`（静态）→ **面板里只有「开始关卡」在动**，符合"只能有一个动态按钮物体" |

> `HubMenuPlaceholder`（置灰占位实现）保留未删，将来要恢复四宫格只改 `activeSelf`。

### 1.4 附带发现（未改，供决策）

天赋页自身的观感问题：背景半透明 → 战斗 HUD 的血条/任务条从顶部透出来；节点只有暗色圆环，没有 `TT_Icon_*` 图标。

---

## 2. 问题2：退出后从节点续跑

### 2.1 根因（三个缺陷叠在一起）

```
StageRunFlow.cs:165        GoToMainMenu()      →  FakeRouteLaunch.StartFromCheckpoint = true;   ← 退出时主动打开
BattleHUD.cs:526           OnRestartButton()   →  FakeRouteLaunch.StartFromCheckpoint = true;   ← 「重来」用不上却也打开
（无任何复位点）            StartFromCheckpoint 是静态开关，一旦置位就永远是 true
```

消费端：`FakeRouteRuntime.ResolveStartNode()` / `BeginRouteNextFrame()` → 读它 → `RestoreCheckpoint` → 从存档点节点接着跑。

而代码自己的注释（`FakeRouteRuntime.Entry.cs:137-146`，2026-10-22 记的 BUG-005）写的是**相反的意图**：

> 落地页自举**不再**恢复检查点 —— 重新进入游戏从**关卡的初始节点**开始，而不是从上次退出的节点续跑。
> 原因：① 玩家反馈「重进游戏直接落在 N3」是 bug（关卡前段内容被跳过）；② 检查点里还存着血量/等级这些上一次的状态……

⇒ 当年只改了"落地页自举不主动恢复"这一半，**"退出时主动打开开关"那一半没改**，所以症状一直没消失。

### 2.2 修法（4 处）

| # | 改动 | 文件 |
|---|---|---|
| 1 | `GoToMainMenu()` **不再**置位（退出＝下次从关卡开头开始） | `Assets/Scripts/Managers/StageRunFlow.cs` |
| 2 | 开关改成**一次性消费**：新增 `FakeRouteLaunch.ConsumeStartFromCheckpoint()`（读走即清） | `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` |
| 3 | 两个消费点都改用消费式调用 | `Assets/Scripts/RouteFake/FakeRouteRuntime.Entry.cs`（`ResolveStartNode` / `BeginRouteNextFrame`） |
| 4 | 「重来」按钮**不再**置位（它走 `RestartStage` 重开当前节点战斗，用不到这个开关；置位只会毒化下一次落地页进入） | `Assets/Scripts/UI/BattleHUD.cs` |

> 存档侧无需改动：教学路线两个节点（`N1`/`E0`）与正式路线仍有 `savePoint=true` → 快照照常写；
> **要不要从存档点续跑**由调用方显式决定（默认否）。

### 2.3 自测（Play Mode，真实退出路径）

```
[测试] 写入快照 checkpoint=E0（模拟打到 E0 后退出）
[测试] 回到落地页：IsPrepared=True StartFromCheckpoint=False（退出后应为 False）
[测试] ★ 退出后再进：当前节点=N1（期望 N1）StartFromCheckpoint=False
   → ✔ 退出关卡后再进入 = 从关卡开头（N1），不再续跑到 E0

[测试] 写入快照 checkpoint=E0 并显式置 StartFromCheckpoint=true
[测试] ★ 显式续玩：当前节点=E0（期望 E0）StartFromCheckpoint=False（消费后应为 False）
   → ✔ 显式续玩仍能用（落在 E0），且标记已一次性消费
```

---

## 3. 本轮改动文件清单

| # | 文件 | 改动 |
|---|---|---|
| 1 | `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` | 开关改一次性消费（`ConsumeStartFromCheckpoint`）+ 注释写清根因 |
| 2 | `Assets/Scripts/RouteFake/FakeRouteRuntime.Entry.cs` | 两个消费点改用消费式调用 |
| 3 | `Assets/Scripts/Managers/StageRunFlow.cs` | `GoToMainMenu` 不再置位 |
| 4 | `Assets/Scripts/UI/BattleHUD.cs` | 「重来」不再置位 |
| 5 | `Assets/Scripts/UI/HubMenuPlaceholder.cs` | 注释更新：四宫格键已撤下（保留实现） |
| 6 | `Assets/Scripts/Editor/HubUiRestructure.cs` | `BuildMenuKeys` 里把键置 inactive（重跑工具不复活） |
| 7 | `Assets/Scenes/Battle.scene` | `MenuKey_1..4` 置 inactive |

---

## 4. 还没做的（用户决定延后）

| 项 | 状态 |
|---|---|
| **正式关首次进入的教学**（C2 复合手势 + 用重攻击打断霸体） | **延后**。正式路线目前**没有任何教学拍**（教学导演只对教学路线生效）→ 玩家买了 C2 也没人教 |
| 教学关通关后**引导去天赋树解锁 C2** | 未做（天赋树本身可用） |
| 天赋页观感（半透明背景透出 HUD、节点无图标） | 未做 |
| 包体优化（只留 ARM64 −70MB / 重建字体图集 −33MB） | 未做 |
