---
id: kd_d1ab9556-9743-4da9-b864-39368ba0906a
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: 卷轴路线（Fake Route）Phase 6 交接清单：现状、已完成项、工作区状态与提交注意、下一步任务（Phase 6 出口内嵌迁移 + P1 清单）、关键文件索引、三条验收口径、硬约束与坑、待用户决定项。
---

# 卷轴路线（Fake Route）交接清单 — 2026-10-15

接手者从这份文档开始即可继续任务；更细的过程证据在 `plan/route-flow-port-plan.md`。

## 0. 现状一句话

分支 `route-scroll-movement`（基线提交 `d94e4a0`）。`Battle.scene` 的正式卷轴流程（`FakeRouteRuntime` + `ScrollRoutePresenter` + `RouteChoicePanel`）**Phase 1~5 已完成并持久化**，背景"变灰/看不全"问题**已结案并经用户真机验收**，本任务遗留的临时诊断**已清理**。下一步主任务是 **Phase 6：出口内嵌迁移**。

## 1. 接手先做的三件事

1. 打开 `Assets/Scenes/Battle.scene`，确认 Hierarchy 有 `Route Flow (Scroll)`（`ScrollRoutePresenter` + `FakeRouteRuntime`，`autoStart = true`，`choicePanelPrefab = RouteChoicePanel_Test`），旧 `Battle Y Route Host` 为禁用状态。
2. 菜单 `Tools/Fake Route/Validator` 跑一次，应为 0 error。
3. 跑一次程序化全链路回归（方法见 `plan/route-flow-port-plan.md` 的"程序化全链路验收（通过）"一节）：N1 战斗 → 奖励 → E0 → J1 左路 → EV → N3 → Completed。

## 2. 已完成（不要重做）

- **Phase 1**：移植选择 UI（`Assets/Scripts/Route/{RouteChoiceLayout,RouteChoicePanel,RouteChoiceButton}.cs`、`Assets/Prefabs/UI/RouteChoice*_Test.prefab`、`Assets/Sprites/UI/RouteChoiceArrow_Test.png`），GUID 与视频分支一致。
- **Phase 2/3**：运行器与数据模型——`Assets/Scripts/RouteFake/FakeRouteRuntime.cs`（状态机）、`FakeRouteNodeConfig` / `FakeRouteChoiceConfig` / `FakeRouteStageConfig` / `FakeRouteStageSaveSnapshot` / `FakeRouteGraphValidator` / `FakeRoutePresentation` / `FakeRouteLaunch` / `IRouteMotionPresenter`；新增字段 `entryDialogue / postBattleDialogue / postBattlePresentation / isFinalNode / layout / openingTravel` 与快照字段 `ultimateEnergy / killCount / coinCount / choiceHistory`。
- **Phase 4**：共享脚本增量——`StageController`（fake 分支启动/失败/重开/Cleanup/`CompleteFakeRouteStage`）、`SaveManager`（`activeFakeRouteStageId` + 3 方法）、`UltimateSystem`（能量恢复/取消）、`DialogueManager`（`IsBusy`/`QueueEventAndNotify`）、`StageConfigManager.fakeRouteStages`、`MainMenuUI`（卷轴选关）、`BattleHUD.OnRestartButton`。
- **Phase 5**：场景接线（`Battle.scene` 的 `Route Flow (Scroll)`；`MainMenu.scene` 的 `fakeRouteStages = [Changbanpo_ScrollRoute]`）。全部旧关卡卸载：`routeStagesV2` 清空、Build Settings 只剩 `MainMenu / Battle / YJunctionSample`。
- **打包崩溃修复**：`RouteStageTargetsV2` 曾有两个 `m_Script` 写成场景内部 fileID 的残留组件，导致构建版加载 `levelN` 时 `[Position out of bounds!]` + SIGTRAP。已拆成独立文件 `Assets/Scripts/RouteV2/RouteStageTargetsV2.cs` 并重建对象，模拟器与真机均可进关卡。
- **背景问题结案**（详见 `memory/android-build-state.md`）：
  - `_VerticalOffset = -0.23` 是**与比例无关的固定纵向标定**，真机效果即以此为基准（曾被误改为 0，已回退；材质现与仓库零 diff）。
  - `Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader` 的 `cover.y` 由 `min(1,imageAspect/screenAspect)` 改为恒 `1`：横向仍按比例裁切、纵向永不裁切。修复后 0.450 / 0.500 / 0.5625 / 0.750 四档比例"天空结束行"全为 16.9%（修复前 0.750 为 12.8%）。
  - 用户已确认真机正常、打包可运行。
- **本任务临时诊断清理**：`Assets/Scripts/Core/ScrollRoutePresenter.cs` 删除 `DiagLogs` 与 `DiagReport()`（308 → 246 行），编译 0 error / 0 warn，Console 不再出现 `[ScrollDiag]`。

## 3. 工作区状态（重要）

- `git status` 有 **225 个未提交改动**，来自本轮移植 + 其它并行工作。**不要 `git checkout -- .` / 不要清理工作区**。
- `Assets/Scenes/Battle.scene`、`Assets/Experiments/CurvedScroll/YJunctionSample.unity` 在移植开始前就已是脏文件（用户布局），**只能做增量修改，不要整文件覆盖**；基线快照见 `Library/Locus/tmp/route-port-baseline-status.txt`、哈希见 `route-port-baseline-hashes.txt`、原始字节备份在 `Library/Locus/tmp/route-port-backup/`。
- 是否提交、如何分批由用户决定。

## 4. 下一步任务

### P0-b（主任务）Phase 6：出口内嵌迁移

- **目标**：出口数据从独立资产改为**内嵌于节点**（照搬视频分支形态），字段集合以**当前节点设计**为准。
- **硬约束**：仍然**只做增量**。`completionPolicy` / `rewards` / `terminalPolicy` 与旧 host 保留到 Phase 7：`Assets/Scripts/Core/BattleYRouteHost.cs` 第 121 / 142 / 166 行仍在读这些字段，提前删会让整个工程编译失败（详见 `plan/route-flow-port-plan.md` §8.2.1）。
- **建议步骤**（与 changelist CSV 的 P6 行一致）：先让 `FakeRouteStageConfig` / 节点声明可序列化的内嵌出口类型 → 把 5 条出口数据迁进 6 个节点资产的 `outgoingChoices` → 确认出口目标与旅行资产引用不变 → 最后再删 5 个独立出口资产（删资产属破坏性动作，需用户确认）。
- **验收**：
  1. `Tools/Fake Route/Validator`（`Assets/Scripts/RouteFake/Editor/FakeRouteValidator.cs`）0 error；
  2. 程序化全链路回归与第 1 节同口径通过；
  3. 改动前后同一位置截图逐段均色一致（口径见第 6 节）。

### P1 后续（按推荐顺序）

1. **出口箭头摆位授权**：5 个出口的 `layout.overrideDefault` 均为 false，按钮现在按面板默认水平排布（2 个出口为 ±80）。请用户用 `Tools/Fake Route/Choice Layout Preview` 摆到"指向对应道路"的位置；**车道选择依据同一字段（layout 的 x 符号），摆位与车道必须一致**。
2. **Phase 7 清理轮**：删 `completionPolicy` / `rewards` / `terminalPolicy`、旧 host `BattleYRouteHost.cs`、死文件 `BattleYVisualBridge.cs`、`YScrollSample.battleHost` 的类型引用，以及旧 V2（`RouteStageRuntimeV2` / `RouteStageTargetsV2` / `RouteStageConfigV2` / `Stage01_RouteV2.unity`，本轮保持禁用）。
3. **终局数值**：`Changbanpo_ScrollRoute.clearCoinReward` 现为 0，待定。
4. **开局强度定调**：进关卡不操作约 **8 秒**死亡（实测 100→73 仅 2 秒），一进 `Defeat` 全屏被 `DarkOverlay`（黑 a=0.72）压暗约 3.5 倍。是否属设计预期待用户定。
5. 可选：若希望"直接 Play `Battle.scene` 就跑卷轴路线"，给场景里 `StageController.fakeRouteStageConfig` 赋值（当前特意留空以免干扰其它测试）。

## 5. 关键文件索引

| 用途 | 路径 |
|---|---|
| 流程状态机（浏览器/表现接口/拒绝回访） | `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` |
| 卷轴表现层（Prepare→EnsureRoute 加载 Y 场景） | `Assets/Scripts/Core/ScrollRoutePresenter.cs` |
| 启动与表现桥 | `Assets/Scripts/RouteFake/FakeRouteLaunch.cs`、`FakeRoutePresentation.cs`、`IRouteMotionPresenter.cs` |
| 数据模型 | `Assets/Scripts/RouteFake/{FakeRouteNodeConfig,FakeRouteChoiceConfig,FakeRouteStageConfig,FakeRouteStageSaveSnapshot,FakeRouteGraphValidator}.cs` |
| 校验器（0 error 门槛） | `Assets/Scripts/RouteFake/Editor/FakeRouteValidator.cs` |
| 选择 UI | `Assets/Scripts/Route/*.cs`、`Assets/Prefabs/UI/RouteChoice*_Test.prefab` |
| 关卡数据 | `Assets/RouteData/ChangbanpoScroll/`（+ `Nodes/`、`Choices/`、`Travels/`） |
| 共享系统 | `Assets/Scripts/Managers/StageController.cs`、`Assets/Scripts/Core/SaveManager.cs`、`Assets/Scripts/Core/UltimateSystem.cs`、`Assets/Scripts/UI/MainMenuUI.cs`、`Assets/Scripts/UI/BattleHUD.cs` |
| 背景天空 | `Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader`、`Assets/Experiments/CurvedScroll/Authoring/YSampleScenerySkybox_v2.mat` |
| 打包入口 | `Assets/Scripts/Editor/AndroidBuildMenu.cs` |

## 6. 验收口径（三条，固定下来）

1. **编译**：改动后 `unity_recompile` → 0 error；Console 无新增 error/warn。
2. **流程**：`Tools/Fake Route/Validator` 0 error + 程序化全链路回归（N1→奖励→E0→J1 左路→EV→N3→Completed）与既有记录同口径。
3. **画面**：同帧多比例渲染对比（把两个相机的 `targetTexture` 指向不同尺寸 RT，`cam.aspect` 随之变化），指标为"天空结束行" + 5% 分段均色；产物放 `Library/Locus/tmp/`。当前基准：0.450 / 0.500 / 0.5625 / 0.750 四档天空结束行均为 **16.9%**。
   - 抓图前先 `EditorApplication.isPaused = true` 冻结，或先重置玩家（`PlayerState.ResetPlayer()` + `StageController.SetState(StageState.InProgress)` 并关掉 `Defeat(panel)`），否则会测到失败遮罩态。

## 7. 坑与硬约束（不要踩）

1. **不能从 Locus 代码片段驱动 `BuildPipeline.BuildPlayer`**：必现 `InvalidCastException @ EditorCompilation.CompleteActiveBuildWhilePumping`。构建必须由用户点菜单 `Tools/打包/Android（ARM64+X86，传统布局）`（显式 `BuildOptions.None`，避开 Tuanjie 的单文件容器模式）。
2. **`TimedPassiveModule.cs` 的 `[TimedPassiveDiag]` 日志不要删**：它是未修问题 TODO-4（`plan/unresolved-issues.md`“路线计时被动跨节点时序”）的关键调试日志。用的是原生 `Debug.Log`（5 处，含每帧一条），**会进构建**；要减噪请加开关而不是删，先问用户。
   - 反过来，`Assets/Scripts/DebugLog.cs` 的 `DebugLog.Info/Warning`（140 处调用）带 `[Conditional("UNITY_EDITOR")]`，**构建中零开销**；Editor 里 `[RowTrace]` / `[CavalryDiag]` / `[Enemy]` 刷屏属正常现象，不必清。
3. **Release 包会剥掉 `Debug.Log`**：设备上抓不到游戏日志；要看游戏日志得用带诊断的包（如 `Build/Android/` 里的 diag 包）。
4. **真机为准**：同一份包在真机渲染正常；模拟器（x86 软件 GL）曾出现暗暖静止帧，未完全归因、不阻塞。在模拟器上判定前先 `svc power stayon true` + 确认应用在前台且未进 Defeat。
5. **Play 模式下关卡会继续跑**：不操作约 8 秒玩家死亡并进入 Defeat（全屏压暗），测量前必须确认 `StageController.currentState` 与玩家血量。
6. **共享资产并发**：`Battle.scene`、`YJunctionSample.unity`、`StageController.cs`、`SaveManager.cs` 等有其它并行改动，改前先看基线快照，避免覆盖。

## 8. 参考文档

- 主计划与执行进度：`plan/route-flow-port-plan.md`
- 当前版 vs 视频分支对照：`plan/route-flow-current-vs-video-branch.md`
- 逐文件清单（含 P6 待确认行）：`plan/route-flow-port-changelist.csv`
- Phase 3 补丁说明：`plan/route-flow-phase3-patch-sheet.md`
- 视频分支权威需求：`design/route-fake-movement-architecture.md`
- 六节点叙事节奏：`design/changbanpo-six-node-level-design.md`
- 构建/设备排障与背景结案：`memory/android-build-state.md`
- 未修问题（含 TODO-4）：`plan/unresolved-issues.md`

## 9. 待用户决定

1. 225 个未提交改动的提交策略。
2. 开局约 8 秒死亡是否属设计预期（要不要出生保护/降低开局强度）。
3. 出口箭头 layout 的授权值（摆位与车道必须一致）。
4. `clearCoinReward` 终局数值。
