---
id: kd_b04915ea-9b88-4ead-a2a5-5b8b0e37f2aa
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: Y 路线全流程回归记录（2026-10-09，机器 A）：Play Mode 下驱动 N1战斗→奖励等待→E0确认→E1确认→J1左路→分支战斗→Completed 全链路 16.7s 跑通、Console 0/0、6 张截图证据；含必须声明的未覆盖项（未真实操作战斗、三选一阻塞未触发、只测左路）与再跑方式。
---

# Y 路线全流程回归（2026-10-09，机器 A）

> **用途**：记录 N1战斗 → 奖励等待 → E0确认 → E1确认 → J1分支 → 分支战斗 的完整流程回归证据，对应 `plan/scroll-scene-current-todolist.md` 的 P4-03 / P4-02 未完成项。
> **边界**：本次是**流程接线验证**，不是真实操作通关。战斗由脚本程序化清空，请看第 3 节的未覆盖项。

## 1. 环境与前置

- 分支 `route-scroll-movement`，HEAD `60d1fe6`（= `origin/route-scroll-movement`）。
- 打开场景：`Assets/Scenes/Battle.scene`（宿主，active）+ `Assets/Experiments/CurvedScroll/YJunctionSample.unity`（附加层）。两者进入前均 `dirty=false`。
- 关键前置修复（本次同步时发现）：合并后 Unity 内存里的 Y 场景仍是合并前版本，`YScrollSample` 的 `e0/e1/j1StoryStop` 与分支视觉/遭遇引用读出来全是 `None`（磁盘 YAML 里都有值）。已强制 close + reopen Y 场景，重载后全部引用解析到 `Route Story Stops/Node - Presentation Anchor[1..3]` 等对象。**若当时保存过场景，这些引用会被写成 null。**
- 宿主配置（现场回读）：`useStoryStops=true`、`storyStopDuration=3`、`storyStopLabel="E0 战后余烬"`、`openingDuration=3`、`branchDuration=3`、`openingDistance=0`、`showRouteChoice=false`。
- `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`：1 波、5 排，仅第 2 排中列 1 个敌人（`enemyId=1011`）。

## 2. 执行方式与时间线

驱动方式：进入 Play Mode 后，用一段运行在 Editor 主线程、跨帧等待的脚本按相位推进——
清敌用 `Enemy.TakeDamage(999999f)`；升级选择用订阅 `UpgradeChoiceManager.OnChoicesReady` + `ConfirmChoice`；E0/E1 用 `BattleYRouteHost.ConfirmStoryStop()`；分支用 `ChooseBranch(false)`；每步截图。

| 时间 | 相位 | 事件 |
|---|---|---|
| 0.0s | `Battle` | N1 战斗就绪，场上 1 个敌人，`stageState=InProgress` |
| 0.1s | `Waiting rewards` | 清空 N1 → 进入奖励等待状态并立即满足退出条件 |
| 3.2s | `E0 story stop` | 3.0s 旅行后到达 E0 停点（`storyStopWaiting=true`） |
| 4.4s | — | 截图后调用 `ConfirmStoryStop()` |
| 7.5s | `E1 story stop` | 3.0s 旅行后到达 E1 停点 |
| 8.7s | — | 截图后 `ConfirmStoryStop()` |
| 11.7s | `Choose route` | 3.0s 旅行后到达 J1，`showRouteChoice=true` |
| 12.3s | `Branch travel` | 选择**左路**（山谷），分支视觉组切换 |
| 15.3s | `Battle` | 3.0s 分支旅行后启动分支战斗（1 个敌人） |
| 16.1s | `Completed (test only)` | 清空分支战斗 → 流程终点 |

- 全程 **Console error = 0、warning = 0**（Play Mode 前后均复核）。
- 退出 Play Mode 后：两个场景仍 `dirty=false`、`YScrollSample` 引用仍解析、场景文件与 `HEAD` 逐字节一致（Play Mode 未向场景泄漏改动）。

## 3. 未覆盖项（不得据此宣称"流程全部验收"）

1. **未真实操作战斗**：敌人由脚本一次性清空，未使用真实手势/连招 → 战斗手感、打击感、难度、HUD 叠加下的画面都未验证。
2. **升级三选一/道具弃置阻塞路径未触发**：N1 只有 1 个敌人，未产生升级选择（脚本日志中无自动选择记录），`Blocking()` 中 `UpgradeChoiceManager.IsChoosing` / `ItemDiscardPopup.IsShowing` 分支未被真实走过。
3. **只测了左路**：右路未点击（两条路指向同一 `SmallBattle.asset`，接线相同）。
4. `Defeat` 失败路径、旅行中暂停/恢复（P4-02）、10 次进出 Play Mode 生命周期、`ViewCamera` 绑定后的正式预览截图均未做。
5. E1/J1 仍为结构占位，画面美术验收仍属用户目视验收范畴（`plan/scroll-scene-current-todolist.md` §8 的首要项）。

## 4. 证据文件

截图（375×750，Game 视图；程序化检查：非黑像素 100%、每张约 2800–3300 种颜色，确认均为真实渲染画面）：

```text
Library/Locus/tmp/flow-regression-20261009/01_n1_battle.png
Library/Locus/tmp/flow-regression-20261009/02_e0_stop.png
Library/Locus/tmp/flow-regression-20261009/03_e1_stop.png
Library/Locus/tmp/flow-regression-20261009/04_j1_choose.png
Library/Locus/tmp/flow-regression-20261009/05_branch_battle.png
Library/Locus/tmp/flow-regression-20261009/06_final_Completed_test_only.png
```

截图位于 `Library/`（不入库），需要发到另一台机器时要单独复制。

## 5. 复核与再跑方式

关键代码入口：

- `Assets/Scripts/Core/BattleYRouteHost.cs`：`Phase` 状态机、`ConfirmStoryStop()`、`ChooseBranch(bool)`、`Start()` 中的 E0/E1/J1 停点序列。
- `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs`：`GetNormalizedProgress(Transform stop)`、`e0/e1/j1StoryStop`、`viewCamera`。
- `Assets/Scripts/Managers/StageController.cs`：`StartRouteBattle` / `SetRouteRewardWaitState` / `SetRouteTravelState` / `OnRouteBattleCompleted`。

再跑建议：先确认 Y 场景的停点引用已解析（重载后 `e0StoryStop` 显示为 `Route Story Stops/Node - Presentation Anchor[1]`），再进 Play Mode；若需要覆盖第 3 节第 2 项，应改用能产生升级的关卡配置或先触发一次 `TriggerItemChoice()`。
