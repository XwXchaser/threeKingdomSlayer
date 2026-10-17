---
id: kd_a7a9d391-c9f2-4f96-b114-88cdead5ce83
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 关卡流程两版对照：当前卷轴预演流程 vs 视频分支流程（回退与决策用）

> 用途：切换正式流程前后，判断「行为变化来自哪一版」；出问题时按本表定位回退目标。
> 移植执行见 `plan/route-flow-port-plan.md`，逐文件清单见 `plan/route-flow-port-changelist.csv`。
> 证据来源：`route-scroll-movement` 分支 Editor 实查（2026-10-14）+ `origin/route-fake-movement` tip `e3639e6` 只读比对。**未做任何 merge/cherry-pick。**

## 1. 两版身份

| | 当前版（本分支 `route-scroll-movement`） | 视频版（`origin/route-fake-movement`） |
|---|---|---|
| 流程驱动 | `Assets/Scripts/Core/BattleYRouteHost.cs`（343 行，IMGUI） | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs`（623 行，枚举状态机） |
| 移动表现 | 卷轴世界：`Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs`（`progress` 1 维坐标 + 节点锚点距离） | 无移动概念：`Assets/Scripts/RouteFake/FakeMovementPresenter.cs`（399 行，视频/静态图/黑屏过场） |
| 出口 UI | `OnGUI` 一个 150px 高框 + 1~2 个 `GUILayout.Button` | `Assets/Scripts/Route/RouteChoicePanel.cs` + `RouteChoiceButton.cs` + `RouteChoiceLayout.cs`，prefab 化 + DOTween 指引动效 |
| 权威文档 | 无（预演性质；`design/route-stage-gameplay-spec.md` 描述的是空间移动 V2，与两者都不同） | `design/route-fake-movement-architecture.md` + `design/changbanpo-six-node-level-design.md` 5.6 |

分支关系：两者 merge-base `d98c371`（`main` 侧基线 `46b7214`），**不是同一实现的两个版本**，而是两套并行实现。

## 2. 逐项对照

| 维度 | 当前版 | 视频版 | 采用 |
|---|---|---|---|
| 状态机 | 字符串 `Phase` + 布尔（`cleared/choiceLocked/IsWaitingForExit/showRouteChoice`） | 枚举 `FakeRoutePhase`（None/EnteringNode/Battle/WaitingReward/RouteChoiceTransition/ChoosingRoute/FakeMoving/Completed/Defeated）+ `_generation` 单次提交 | 视频版 |
| 节点内容 | 只支持战斗 + 出口确认；校验器**拒绝**带对话或节点奖励的节点 | 节点可带 `entryDialogue / postBattleDialogue / postBattlePresentation / battleBackground` | 视频版字段，卷轴表现 |
| 节点模型 | `FakeRouteNodeConfig`：`dialogues[] / rewards[] / completionPolicy / terminalPolicy / savePoint / visualProfile / outgoingChoices` | `FakeRouteNodeConfig`：`entryDialogue / postBattleDialogue / postBattlePresentation / isFinalNode / savePoint / battleBackground / routeChoice(Transition/Background) / outgoingChoices` | 视频版 + 卷轴专属（`visualProfile`） |
| 出口模型 | `FakeRouteChoiceConfig` = 独立 ScriptableObject 资产（5 个 `Choice_*.asset`），**无 layout**；`presentation` = `FakeRouteTravelPresentation` | `FakeRouteChoiceConfig` = 节点内嵌 `[Serializable]` 类，含 `layout`；`presentation` = `FakeRoutePresentation`（视频/静态/黑屏） | 数据形态待定（见方案 §8.1）；`layout` 必须引入 |
| 出口数量 | 硬编码 `leftBranchChoice` / `rightBranchChoice` 两个 Inspector 字段，要求节点出口数恰好 2 或 1 | 按 `node.outgoingChoices` 动态生成任意数量按钮 | 视频版 |
| 单出口 | 必须点击（与视频版一致） | 必须点击 | 一致 |
| 节点内多 Entry | 循环跑完本节点全部 Entry | 一次进入**只跑第一条未完成 Entry**，靠回访跑下一条 | 当前版（因禁止回访） |
| 回访 | 禁止（`visited.Contains(target)` 拒绝） | 允许 | 当前版 |
| 战斗结算 | 清空 → `SetRouteRewardWaitState()` → 等 `Blocking()` + 死亡动画收尾 → 出按钮 | 同上（顺序与阻塞条件一致） | 一致 |
| 终点结算 | `terminalPolicy != None` → 只把 `Phase` 设为 `"Completed (test only)"`，**不结算、不发奖励、不标记通关**；`clearCoinReward = 0` | `isFinalNode → FinishRoute` → `SaveManager.ClearFakeRouteSnapshot` + `CompleteFakeRouteStage(clearCoinReward)`（整关奖励 + `MarkStageCleared` + `SettleCoins` + Victory） | 视频版 |
| 存档点 | `savePoint = true` 但从未写快照 | 节点进入时按 `savePoint` 写独立快照；`StartFromCheckpoint` 时 replace 恢复（玩家数值/被动/主动/UT/节点状态/选择历史） | 视频版 |
| 失败恢复 | `Phase = "Defeated"` 后结束协程；`StageController.OnPlayerDefeated` 不认识该运行器 | `HandleStageDefeat()`（generation++、停协程）+ `RestartStage` 的 fake 分支 + 从存档节点重进 | 视频版 |
| 入口链路 | `MainMenuUI` → `RouteStageConfigV2`（`Stage01_RouteV2` + `Assets/Scenes/RouteStageV2/Stage01_RouteV2.unity`），与 `ChangbanpoScroll` **无引用关系**；节点流程只在直接 Play `Battle.scene` 时成立 | `MainMenuUI` → `StageConfigManager.fakeRouteStages` → `FakeRouteLaunch.PendingConfig` → `StageController.fakeRouteStageConfig` → `FakeRouteRuntime.Begin` | 视频版 |
| 表现切换 | 出口的 `FakeRouteTravelPresentation`（`duration / travelDistance / curveStrength / backgroundOffset`）+ 节点锚点距离 + `travelProgressCurve` | 出口的 `presentation`（视频/静态/黑屏）+ `holdLastFrameAsBackground` | 卷轴版（视频版无移动概念） |

## 3. 视频版可复用 / 不可复用清单

**可复用（移植）**

| 文件 | 行数 | 说明 |
|---|---|---|
| `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` | 623 | 状态机/结算/存档点/终点/失败；3 处必改见方案 Phase 2 |
| `Assets/Scripts/Route/RouteChoicePanel.cs` | 133 | 面板；父级取 `BattleHUD(Canvas)`，本项目已存在该根 Canvas |
| `Assets/Scripts/Route/RouteChoiceButton.cs` | 161 | 箭头 Icon + TMP Label + Button + DOTween 指引动效；依赖 DOTween/TMP（本项目已具备） |
| `Assets/Scripts/Route/RouteChoiceLayout.cs` | 10 | `overrideDefault / anchoredPosition / rotation` |
| `Assets/Scripts/RouteFake/Editor/FakeRouteChoiceLayoutWindow.cs` | 220 | 2:3 / 1080×1920 预览拖拽箭头写回 `layout` |
| `Assets/Scripts/RouteFake/Editor/FakeRouteValidator.cs` | 69 | 资产校验菜单（`Tools/Fake Route/Validate Assets`、`Reimport And Validate Assets`） |
| `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` | 5 | 静态入口；当前分支缺 |
| `Assets/Prefabs/UI/RouteChoicePanel_Test.prefab`、`RouteChoiceButton_Test.prefab`、`Assets/Sprites/UI/RouteChoiceArrow_Test.png` | — | 一并移植（美术仍是 `_Test` 占位，结构/工具链是集成点） |
| `Assets/Scripts/Managers/StageController.cs` 的 fake 分支（`+133` 行） | — | `CompleteFakeRouteStage` / fake 启动 / 失败 / 重开 / 分阶段清理 |
| `Assets/Scripts/Core/SaveManager.cs` 的 fake API（`+132` 行） | — | `SetActiveFakeRouteStage` 等 |
| `Assets/Scripts/Core/UltimateSystem.cs`（`+19`）、`UltimateEffect.cs`（`+2`）、`Assets/Scripts/UI/DialogueManager.cs`（`+6`）、`Assets/Scripts/Core/StageConfigManager.cs`（`+4`）、`Assets/Scripts/UI/MainMenuUI.cs`（变更 79 行） | — | 结算/取消大招/对话通知/入口 |

**不可复用（丢弃）**

- `Assets/Scripts/RouteFake/FakeMovementPresenter.cs`（399 行）与 `FakeRoutePresentation.cs`（29 行：视频/静态/黑屏表现资产）——卷轴没有移动概念，位置由 `ScrollRoutePresenter` 顶替。
- `Assets/RouteData/FakeStage01/**`（节点/表现资产 + 6 个 mp4 + 长坂坡背景图）：与卷轴节点资产是两套，**不合入**。
- `Assets/Scripts/Route/Editor/RouteChoiceLayoutPreviewWindow.cs`（167 行）：面向被替换的旧 `RouteWorld*`/`RouteV2` 运行器。
- 视频版对 `Assets/Scripts/Core/SpikeTrapController.cs`（118 行）、`Assets/Scripts/Core/TimedPassiveModule.cs`（去日志）、`Assets/Scripts/Enemy/*`、`Assets/Scripts/Player/*` 等战斗层改动：属另一条移植线，本轮不动。

## 4. 当前版实际行为（回退对照基准）

`Assets/Scripts/Core/BattleYRouteHost.cs`：

| 行段 | 行为 |
|---|---|
| 40–51 | `Awake`：`stage.autoStartStage=false`；禁用所有 `RouteStageRuntimeV2`；禁用 `BattleYVisualBridge` |
| 53–101 | `Start`：additive 加载 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，关掉 Y 场景相机，把 `YScrollSample.battleHost` 指回自己，`progress=0`，3 秒开场旅行到 startNode |
| 103–151 | `ValidateBindings`：拒绝「带对话/奖励的节点」「条件 BattleEntry」「出口数 ≠ 1 且 ≠ 左右硬编码分支」 |
| 153–177 | `EnterNode`：逐条战斗 → `IsWaitingForExit`；`terminalPolicy != None` 时只设 `Phase = "Completed (test only)"` |
| 187–211 | `Combat`：清空 → `SetRouteRewardWaitState` → 等奖励阻塞 + 已死敌人动画收尾 |
| 213–245 | `ConfirmSingleExit` / `ChooseBranch` / `Choose`：拒绝已访问目标与进度回退 |
| 247–269 | `Travel`：`progress` 从源锚点插值到目标锚点，时长取 `choice.presentation.duration` |
| 314–342 | `OnGUI`：左上 `(12,12)` 一个 150px 高 `GUI.skin.box` + 标题/阶段文本 + 1~2 个 60px 高按钮，按 `Screen.width/540` 缩放 |

当前场景/资产现状：

- `Assets/Scenes/Battle.scene`：`Battle Y Route Host`（enabled）、`RouteStageRuntimeV2`（enabled=false，但脚本自带 OnGUI）、`RouteStageTargetsV2`、`DialogueLayer`、`BattleHUD(Canvas)`；**无** `FakeRouteRuntime` / 视频背景层 / 出口面板引用。
- `Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset`：`routeId=changbanpo_scroll_preview`、`configurationVersion=1`、`clearCoinReward=0`、6 节点（N1/E0/J1/EV/N2/N3）+ 5 出口 + 5 旅行资产。
- Y 场景锚点（`ScrollRouteNodePresentation.node → worldDistance`）：N1=0、E0=90、J1=190、EV=225、N2=266.12、N3=266.12。
- `YScrollSample.battleHost` = **null**；`leftBranchVisuals/rightBranchVisuals/junctionVisuals` = **null**（分支视觉切换当前是空操作）。
- `StageController`（本分支）缺：`fakeRouteStageConfig`、`CompleteFakeRouteStage`、失败/重开的 fake 分支。`SaveManager` 缺：`SetActiveFakeRouteStage / GetActiveFakeRouteStageId / ClearActiveFakeRouteStage`。`UltimateSystem` 缺：`SetEnergyForRouteRestore`、`CancelUltimate`（含 `UltimateEffect.Cancel`）。`DialogueManager` 缺：`IsBusy / QueueEventAndNotify`。
- 已有可复用：`SaveManager.Get/Save/ClearFakeRouteSnapshot`、`FakeRouteStageSaveSnapshot`（字段与视频版不同，见方案 Phase 3）、`PlayerState.ResetPlayer/ResetCooldownsForNodeTransition`、`ActiveSkillInventory.ResetCooldowns/AcquireOrUpgrade`、`TimedPassiveModule.ResetTimers/SetSuppressImmediateEffects/TriggerPendingCombatStartEffects`、`EnemyManager.ClearAllEnemies`（含死亡 Tween 收尾）、`UltimateSystem.CurrentEnergy/ResetEnergy`。

## 5. 项目里并存的三套路线架构（切换时勿混用）

| 架构 | 脚本 | 场景/资产 | 当前状态 |
|---|---|---|---|
| FakeRoute（视频版模型） | `Assets/Scripts/RouteFake/**` | `Assets/RouteData/ChangbanpoScroll/**`（卷轴版）、`Assets/RouteData/FakeStage01/**`（视频版，不在本分支） | 本分支有部分配置类型，无运行器 |
| RouteStageV2（空间移动） | `Assets/Scripts/RouteV2/**` | `Assets/Scenes/RouteStageV2/Stage01_RouteV2.unity`、`Assets/RouteData/Stage01V2/**` | `Battle.scene` 中组件 enabled=false，由 host 在 `Awake` 禁用 |
| legacy RouteStage | `Assets/Scripts/Route/**`（`RouteProgressionController` 等） | `Assets/RouteData/Stage01/**` | 场景内无实例，`MainMenuUI.OnRouteStageSelected` 已停用 |

`design/route-stage-gameplay-spec.md`（Head/Combat/Tail 空间移动 + RouteStageRoot）描述的是第二套，与本方案目标相反；视频版文档明确把该空间模型标为历史基线（`design/route-fake-movement-architecture.md` §1、§13.3）。

## 6. 不能整份 merge 的原因

1. **同名类不同内容**：`FakeRouteNodeConfig`、`FakeRouteStageConfig` 两分支都有但字段不同；`FakeRouteChoiceConfig` 一方是 ScriptableObject 资产、一方是内嵌 `[Serializable]` 类。
2. **场景差异不可控**：`Assets/Scenes/Battle.scene` 两分支差 1578 行（视频版多出视频背景层/对话层/面板接线），merge 冲突无法自动收敛。
3. **资产族不同**：`Assets/RouteData/FakeStage01/**`（含 mp4）与 `Assets/RouteData/ChangbanpoScroll/**` 是两套节点数据，不能混用。
4. 结论：按文件**逐项取用**（方案 Phase 1–5），场景在**当前** `Battle.scene` 上手工接线。

## 7. 差异清单（摘要）

结构差异：节点内容字段、出口数量表达、出口 UI 形态、终点结算、存档点、失败恢复、入口链路、状态机健壮性、表现模型、节点/锚点规模（卷轴仅 6 锚点 1 分叉，正式拓扑需要更多 E 类节点与第二个分叉——本轮不做）。

语义差异（4 条，必须由移植版决定）：
1. 一次进入跑完节点全部未完成 Entry（当前版）vs 一次只跑一条（视频版，为回访设计）。
2. 禁止回访（用户既定）vs 允许回访（视频版）。
3. 清空 ≠ 完成，必须等奖励阻塞结束（两版一致，需保持）。
4. 终点只发一次整关奖励并标记通关（视频版有，当前版无）。

## 8. 对照材料复现方式（回退/复核用）

```text
# 单文件读取
git show origin/route-fake-movement:threeKingdomSlayer/<路径>

# 批量提取（不含大体积视频时用 pathspec 限定）
git ls-tree -r --name-only origin/route-fake-movement -- Assets/RouteData/FakeStage01
```

本轮已提取的对照副本（非项目代码，位于项目临时目录）：

- `Library/Locus/tmp/videobranch/RouteFake/`：`FakeRouteRuntime.cs`、`FakeMovementPresenter.cs`、`FakeRoutePresentation.cs`、`FakeRouteNodeConfig.cs`、`FakeRouteStageConfig.cs`、`FakeRouteLaunch.cs`、`FakeRouteChoiceLayoutWindow.cs`
- `Library/Locus/tmp/videobranch/Route/`：`RouteChoicePanel.cs`、`RouteChoiceButton.cs`、`RouteChoiceLayout.cs`、`RouteNodeDefinition.cs`、`RouteChoiceLayoutPreviewWindow.cs`
- `Library/Locus/tmp/videobranch/Managers/`：`StageController.cs`（视频版）、`Battle_video.scene`（视频版 Battle 场景快照，仅用于核对对象与接线）
- `Library/Locus/tmp/videobranch/Nodes/`：`FakeStage01.asset` + 16 个 `RouteNode_FakeStage01_*.asset`

注意：`Library/` 下的内容不属于交付物，`list`/`grep` 会跳过；需要长期保留请另行授权复制到项目外。

## 9. 回退相关注意

- 本方案中**不改变行为**的部分：Phase 1（UI 新增）、Phase 2（脚本新增但未接线）、Phase 4 的新增方法（在 `fakeRouteStageConfig == null` / 列表为空时不参与）。
- **会改变行为**的部分：Phase 3（共享 SO 字段与资产重导）、Phase 5（场景切换、MainMenu 入口）。回退判定优先看这两处。
- 唯一需要留意的既有热路径改动：`UltimateSystem.ActivateUltimate` 内新增一行 `_activeEffect = effect`（为 `CancelUltimate` 服务），需单独冒烟。
