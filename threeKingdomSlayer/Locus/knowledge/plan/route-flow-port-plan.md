---
id: kd_dc71c498-e563-40f9-a958-94bcc1e8f820
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 卷轴路线正式化：安全移植方案（BattleYRouteHost 预演流程 → FakeRoute 流程 + 卷轴表现）

> 状态：**方案阶段，未改动 Unity 项目**。本文件只记录可执行步骤、边界、回退与验收。
> 两版行为对照见 `plan/route-flow-current-vs-video-branch.md`；逐文件改动与回退清单见 `plan/route-flow-port-changelist.csv`。
> 前置调查来源：本分支 Editor 实查 + `origin/route-fake-movement`（tip `e3639e6`）只读比对，对照副本在 `Library/Locus/tmp/videobranch/`。

## 1. 目标与已确认边界

**目标**：把 `Assets/Scenes/Battle.scene` 里的预演流程（`Assets/Scripts/Core/BattleYRouteHost.cs` + OnGUI 按钮）替换为正式流程：保留卷轴化移动表现；节点结算、出口选择、存档、失败恢复、通关结算改用视频分支（`origin/route-fake-movement`）的模型。

用户已确认（2026-10-14 反馈）：

1. 视频分支**没有移动概念**——它的表现层（`FakeMovementPresenter` 播视频/静态/黑屏）整体丢弃，位置由卷轴 Presenter 顶替；运行器与表现层本就是「Inspector 引用 + 完成回调」边界，替换不影响运行器逻辑。
2. 出口形态使用视频分支的**自定义按钮型**（`RouteChoicePanel` + `RouteChoiceButton` + `RouteChoiceLayout`，箭头图标按 `layout.rotation` 指路）。
3. **保留卷轴移动**；节点结算与选择节点表现改用视频分支那套（箭头按钮指路 + 出口绑定表现）。
4. **先禁止回访**。
5. 卷轴世界扩容（新增 E 类节点锚点、第二个分叉）**不在本轮范围**。
6. 最终目标是把测试流程变成**正式游戏流程**：完整存档 + 路线推进记录 + 战斗节点结算。

## 2. 安全前提（当前有其他 agent 在改动项目）

### 2.1 强制规则

- **优先纯新增**：Phase 1、2 只新增脚本与 UI 资产，不接线即不生效。
- **共享文件只做最小增量**：不改名、不删除、不移动既有序列化字段与 GUID；新字段追加在末尾。
- **编辑前先读锚点**：用文本锚点（唯一片段）而不是行号，因为其他 agent 可能已改动同一文件。
- **每次共享文件改动后立刻编译/回读**：`unity_recompile` + Console 0 error，再回读目标文件确认落盘（见 `memory/tool-result-verification.md`）。
- **禁止的 Git 动作**：`git checkout .` / `git restore .` / `git stash` / `git commit`（会连带丢弃或提交他人的在途改动）。回退只针对自己的路径逐文件处理。
- **不碰的场景/设置**：`Assets/Scenes/MainMenu.scene`（Phase 5 除外）、`Assets/Experiments/CurvedScroll/YJunctionSample.unity`、`ProjectSettings/**`、`Assets/Resources/**`。

### 2.2 Phase 0 基线记录（开工前必做）

```text
git rev-parse HEAD                                  → 记录基线 commit
git status --porcelain > Library/Locus/tmp/route-port-baseline-status.txt
逐文件哈希：待改共享文件 + Assets/RouteData/ChangbanpoScroll/** + Assets/Scenes/Battle.scene
  → Library/Locus/tmp/route-port-baseline-hashes.txt
逐文件副本：待改共享脚本与资产 → Library/Locus/tmp/route-port-backup/
```

作用：开工时锁定「哪些改动是我的」，其他 agent 的改动通过再次比对 `git status --porcelain` 识别；回退时用副本人工反向编辑，而不是整体 restore。

### 2.3 开工前的并发检查（每个 Phase 前重跑一次）

```text
git status --porcelain -- Assets/Scripts/RouteFake Assets/Scripts/Route Assets/Scripts/Core Assets/Scripts/Managers \
                          Assets/Scripts/UI Assets/RouteData/ChangbanpoScroll Assets/Scenes
```

若目标路径出现**非本方案**的改动，暂停该 Phase 并上报，不覆盖他人改动。

## 3. 分阶段计划

### Phase 1：出口 UI 组件（纯新增）

移植（连同 `.meta`，保持 GUID）：

| 来源（`origin/route-fake-movement`） | 当前分支落点 | 行数 |
|---|---|---|
| `Assets/Scripts/Route/RouteChoiceLayout.cs` | 同路径 | 10 |
| `Assets/Scripts/Route/RouteChoicePanel.cs` | 同路径 | 133 |
| `Assets/Scripts/Route/RouteChoiceButton.cs` | 同路径 | 161 |
| `Assets/Prefabs/UI/RouteChoicePanel_Test.prefab` | 同路径 | — |
| `Assets/Prefabs/UI/RouteChoiceButton_Test.prefab` | 同路径 | — |
| `Assets/Sprites/UI/RouteChoiceArrow_Test.png` | 同路径 | — |
| `Assets/Scripts/RouteFake/Editor/FakeRouteChoiceLayoutWindow.cs` | 同路径 | 220 |

不移植：`Assets/Scripts/Route/Editor/RouteChoiceLayoutPreviewWindow.cs`（旧 `RouteWorld*`/`RouteV2` 预览，仅在被替换的 V2 运行器上有意义）。

依赖已具备：`Assets/Plugins/Demigiant/DOTween`、TextMesh Pro、`BattleHUD(Canvas)`（`RouteChoicePanel.FindOverlayCanvas()` 期望的父级，根 Overlay Canvas）。

验收（不落盘）：编辑器内把面板 prefab 临时挂到 `BattleHUD(Canvas)` 手动试摆，确认标题/图标/标签/指引动效；试完删除临时实例并确认场景 `dirty=false`；`Tools/Fake Route/Choice Layout Preview` 可打开并对 FakeRoute 节点生效。

### Phase 2：运行器 + 卷轴 Presenter（纯新增，未接线不生效）

| 新增文件 | 内容 |
|---|---|
| `Assets/Scripts/RouteFake/IRouteMotionPresenter.cs` | 表现接口：`PlayOpening / Play(choice) / PlayNodePresentation / SetNodeBackground / Skip / IsPlaying` |
| `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` | 移植视频分支 623 行状态机（枚举 Phase + `_generation` 单次提交 + 奖励等待 + 存档点 + 终点结算 + 失败），**3 处必改**：① `RunBattleEntries` 去掉 `yield break`，一次进入跑完本节点全部未完成 Entry；② `TrySelectChoice` 拒绝已访问目标；③ 表现调用改为 `IRouteMotionPresenter` |
| `Assets/Scripts/Core/ScrollRoutePresenter.cs` | 由 `BattleYRouteHost` 的移动/锚点代码改写：读 `YScrollSample` + `ScrollRouteNodePresentation.worldDistance` + `FakeRouteTravelPresentation(duration/speedCurve)`，写 `progress/right` |
| `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` | 视频分支的 5 行静态入口（当前分支缺）：`PendingConfig` / `StartFromCheckpoint` |

`FakeRouteRuntime` 的 Awake 必须承担现在由 host 承担的独占职责（否则两套驱动会打架）：

- `stage.autoStartStage = false`；
- 禁用场景内的 `RouteStageRuntimeV2` 实例；
- 不修改 Player/Camera/Enemy 坐标，不修改 `Evaluates`/`Length`/`turnAngle` 语义。

卷轴侧零场景改动的原因（已核实）：`YScrollSample.battleHost` 在 Y 场景中为 **null**，且只用于 `Update` 里的 `battleHost == null` 自走判断；新 Presenter 只需 `animate=false` 并写 `progress`，**无需改 Y 场景、无需改 `YScrollSample.cs`**。`leftBranchVisuals/rightBranchVisuals/junctionVisuals` 在场景里是空引用，Presenter 必须容忍空（与现 host 的 `SetBranchGroup` 空判断一致），本轮不补接线。

验收：现有 6 节点资产（`Assets/RouteData/ChangbanpoScroll/`）上程序化跑通「N1 战斗 → 奖励等待 → 出口面板 → 卷轴旅行 → E0 → J1 选择」。

### Phase 3：数据模型扩展（共享 SO 脚本 + 资产）

| 文件 | 改动 |
|---|---|
| `Assets/Scripts/RouteFake/FakeRouteNodeConfig.cs` | +`entryDialogue / postBattleDialogue / postBattlePresentation / isFinalNode`；`terminalPolicy` 与 `isFinalNode` 合并；建议 -`completionPolicy / rewards`（当前无消费者，host 还主动拒绝） |
| `Assets/Scripts/RouteFake/FakeRouteChoiceConfig.cs` | +`RouteChoiceLayout layout`；保留 `presentation: FakeRouteTravelPresentation`（卷轴旅行） |
| `Assets/Scripts/RouteFake/FakeRouteStageConfig.cs` | +`openingTravel`（替掉 host 的 `openingDuration` + `travelProgressCurve` 硬编码）；`clearCoinReward` 由 0 改为真实值 |
| `Assets/Scripts/RouteFake/FakeRouteGraphValidator.cs` | +终点必须 `isFinalNode` 且无出口；+出口必须有 presentation；+禁止指向已访问节点的校验 |
| `Assets/Scripts/RouteFake/FakeRouteStageSaveSnapshot.cs` | +`ultimateEnergy / killCount / coinCount`；`routeChoiceHistory` 由 `List<string>` 升级为 `{sourceNodeId, choiceId, targetNodeId}`（路线推进记录） |
| `Assets/RouteData/ChangbanpoScroll/**` | 6 节点 / 5 出口 / 5 旅行资产按新字段重导（不动 GUID 与引用） |

风险：改共享 SO 脚本会触发全资产重导；若其他 agent 同时在改这批资产会冲突 → 因此排在 Phase 3，且以 §2.3 检查为门槛。

### Phase 4：结算 / 存档 / 入口 API（共享脚本，增量且配置为空时行为中立）

| 文件 | 改动 | 中立性 |
|---|---|---|
| `Assets/Scripts/Managers/StageController.cs` | +`fakeRouteStageConfig` 字段 + `FakeRouteLaunch` 分支；+`CompleteFakeRouteStage(int)`；`OnPlayerDefeated → runtime.HandleStageDefeat`；`RestartStage` 的 fake 分支（从最近存档点重进） | `fakeRouteStageConfig == null` 时全部跳过 |
| `Assets/Scripts/Core/SaveManager.cs` | +`SetActiveFakeRouteStage / GetActiveFakeRouteStageId / ClearActiveFakeRouteStage` | 纯新增 |
| `Assets/Scripts/Core/UltimateSystem.cs` | +`SetEnergyForRouteRestore`；+`CancelUltimate` + `_activeEffect` 跟踪 | 新增方法；仅 `ActivateUltimate` 内多一行记录，需单独冒烟 |
| `Assets/Scripts/Core/UltimateEffect.cs` | +`public virtual void Cancel() {}` | 空实现，中立 |
| `Assets/Scripts/UI/DialogueManager.cs` | +`IsBusy`、`QueueEventAndNotify`（各 2–5 行） | 纯新增 |
| `Assets/Scripts/Core/StageConfigManager.cs` | +`fakeRouteStages`（保留 `routeStagesV2`） | 列表为空时无影响 |
| `Assets/Scripts/UI/MainMenuUI.cs` | FakeRoute 选关按钮 + 新游戏/继续（从该关 `startNode` 开始，不读失败快照）+ 清快照 | `fakeRouteStages` 为空时无影响 |

本轮**不做**：`Assets/Scripts/Core/SpikeTrapController.cs` 按视频版重写（当前分支已有 `SetRouteVisibility(true/false)` 路线开关，够用）；`Assets/Scripts/Core/TimedPassiveModule.cs`（当前分支已带诊断日志，视频版只是去日志）；QTE/蓄力取消链路专项核对。

### Phase 5：切换与清理（唯一的场景改动）

- `Assets/Scenes/Battle.scene`：**新增**一个对象（例如 `Route Flow (Fake)`）挂 `FakeRouteRuntime` + `ScrollRoutePresenter`，填 `routeConfig` / 出口面板 prefab / stage 引用；**不删除**旧对象，只把 `Battle Y Route Host` 设为 `SetActive(false)` → 回退 = 重新勾选 active。
- `Assets/Scenes/Battle.scene`：`RouteStageRuntimeV2` / `RouteStageTargetsV2` 保持 `enabled=false`。
- `Assets/Scenes/MainMenu.scene`：`StageConfigManager.fakeRouteStages` 填入 FakeRoute 关卡配置。
- 保留 `Assets/Scripts/Core/BattleYRouteHost.cs` 与 `Assets/Scripts/Core/BattleYVisualBridge.cs` 文件：`Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` 的 `battleHost` 字段类型指向 `BattleYRouteHost`，删类会连带编译失败；该引用当前为 null，如需真正删除必须与 YScrollSample 的一行类型改动同批，**列为独立清理项，需另行授权**（Y 场景当前正被其他 agent 改动）。
  - 已查实：`BattleYVisualBridge` 在已加载场景中 **0 个实例**（死文件，仅被 host 一行 `GetComponent` 提到）；`BattleYRouteHost` 在 `Battle.scene` 有 1 个实例（`Battle Y Route Host`，active）。
  - 结论：**先不删**（保留 = 零风险 + 一键切回；删除 = 少约 361 行死代码，代价是改 YScrollSample 一行 + 一次编译）。

### Phase 6（待确认，建议排最后）：出口数据形态迁移（内嵌）

用户已定：出口数据形态照搬视频分支的「选择内嵌节点」。具体动作：

- 删除 `Assets/Scripts/RouteFake/FakeRouteChoiceConfig.cs`（ScriptableObject 资产版），改在 `FakeRouteStageConfig.cs` 内声明 `[Serializable] sealed class FakeRouteChoiceConfig { choiceId / displayName / targetNode / presentation / layout / placeholderDuration }`；
- 把 5 个出口资产（`Assets/RouteData/ChangbanpoScroll/Choices/Choice_{N1_E0, E0_J1, J1_EV, J1_N2, EV_N3}.asset`）的内容搬进 6 个节点资产的 `outgoingChoices[]`（`presentation` 继续引用现有 `Assets/RouteData/ChangbanpoScroll/Travels/Travel_*.asset`）；
- 删除 5 个出口资产与 `.meta`；
- 字段集合以**当前节点设计**为准（`savePoint`、`terminalPolicy`、`visualProfile`、`battleEntries` + 本轮新增的 `entryDialogue/postBattleDialogue/postBattlePresentation/isFinalNode`），**只改出口的承载方式**，不照抄视频分支的节点字段集。

影响面（已实查引用）：5 个出口资产只被 6 个节点资产 + `Battle.scene` 里 `BattleYRouteHost.leftBranchChoice/rightBranchChoice` 两个场景字段引用，共 8 处。

**回退取舍（重要）**：内嵌形态是数据模型级改动；迁移完成后旧预演 host 的字段类型（资产型出口）已对不上，**「一键切回旧流程」只在迁移之前成立**。迁移后的回退 = 恢复备份的 `Assets/RouteData/ChangbanpoScroll/**` + 4 个 RouteFake 脚本 + 重新编译。

建议顺序（待确认）：先用**现有资产形态 + 新增 `layout` 字段**跑通并切换新运行器（Phase 1–5），最后再做本迁移。理由：① 运行器代码两种形态几乎相同（都读 `node.outgoingChoices`）；② 迁移前始终保持一键切回旧流程；③ 迁移单一目的、易于用校验器 + 一次 Play 回归验证。若要求一次到位，则按本 Phase 合并进 Phase 3，回退粒度改为「恢复备份目录」。

### Phase 7（清理轮，最后）：删旧字段与旧驱动

- 删 `FakeRouteNodeConfig` 的 `completionPolicy / rewards / terminalPolicy`（并把 `IsTerminal` 收敛为只读 `isFinalNode`）；
- 删 `Assets/Scripts/Core/BattleYRouteHost.cs`（+ 场景对象）与 `Assets/Scripts/Core/BattleYVisualBridge.cs`；
- 顺带修正 `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs` 的 `battleHost` 字段类型（实查值为 null，改类型不丢引用）；
- 旧 V2（`RouteStageRuntimeV2` / `RouteStageTargetsV2` / `RouteStageConfigV2` / `RouteStageV2` 场景）的去留在本轮一并决定；
- 前置条件：Phase 5 切换已验收（新流程可完整跑完），且确认无其他 agent 正在改这些文件；
- 每步都要编译 + Console 0 error，且回退粒度 = 恢复备份副本。

> 本阶段是纯收益（少死代码），不影响功能，因此不阻塞新流程上线。

## 4. 行为契约（移植后必须成立的 5 条硬规则）

1. 战斗清空 ≠ 节点完成：必须等 `UpgradeChoiceManager.IsChoosing`、`ItemDiscardPopup.IsShowing`、`ExpGemManager.IsCollecting` 全部结束。
2. 一次进入执行完本节点全部未完成 `BattleEntry`（禁止回访的直接推论；视频分支原实现是「一次只跑一条」）。
3. 单出口也出面板、也必须等玩家点击；点击即锁定；`_generation` 保证只提交一次。
4. 终点（`isFinalNode`）：无出口 → 整关奖励 + `MarkStageCleared` + Victory，只发一次。
5. 存档只写在节点提交后 / 节点进入时（`savePoint`）；不存旅行中间帧、敌人、投射物、连击、技能冷却、临时 Buff。

## 5. 验收清单

- Phase 1：面板观感（标题/箭头指向/layout 与目标路一致）、1080×1920 参考坐标在本项目动态 `CanvasScaler` 下的位置正确。
- Phase 2：6 节点流程闭环；奖励未结束不能出面板；双击只提交一次；旅行期间输入锁定、暂停冻结。
- Phase 3：`Tools/Fake Route/Validate Assets` 全绿；节点资产字段与设计一致。
- Phase 4：`savePoint` 快照写入/恢复；失败 → 从存档节点重进且不重复发奖励；终点只发一次整关奖励并 `MarkStageCleared`；MainMenu 新游戏/继续/选关按规则从 `startNode` 开始。
- Phase 5：Play 实机（真实手势战斗）走完 N1 → E0 → J1 →（左 EV→N3 / 右 N2），观察对象切换与旧对象禁用无副作用。
- 全局：Console 0 error；`Assets/Scenes/Battle.scene` 与 Y 场景的脏标记状态明确（谁改的、是否保存）。

## 6. 回退方案

| Phase | 回退动作 |
|---|---|
| 1 | 删除新增的 3 个脚本 + 2 个 prefab + 1 个 sprite + 1 个 Editor 窗口（含 `.meta`）；若曾在编辑器试摆，确认场景未保存 |
| 2 | 删除 4 个新增脚本；场景未接线，游戏行为不变 |
| 3 | 用 `Library/Locus/tmp/route-port-backup/` 副本人工反向编辑 5 个 SO 脚本；资产按备份副本恢复并 `ImportAsset(ForceUpdate)`；**不要**整体 restore |
| 4 | 同上逐文件反向编辑 7 个共享脚本；仅回退本方案新增的方法/分支，保留其他 agent 的改动 |
| 5 | `Battle Y Route Host` 重新 `SetActive(true)`；禁用新对象；MainMenu 的 `fakeRouteStages` 清空；场景回到切换前姿态 |
| 任一阶段 | 停止后续 Phase，记录现象与最小复现，先确认是「本方案引入」还是「并发改动引入」（对比 §2.2 基线哈希） |

## 7. 症状 → 回退判定

| 症状 | 最可能来源 | 处理 |
|---|---|---|
| 单出口自动进入下一节点、无按钮 | 契约 3 未落地（运行器或面板） | 回退到 Phase 2 修复，不切场景 |
| 清场后直接进下一节点、吞掉三选一 | 契约 1 未落地 | 同上 |
| 多 Entry 节点只打了一场就出出口 | 契约 2 未落地（照抄了视频分支的 `yield break`） | 同上 |
| 终点无结算、不标记通关 | 契约 4 未落地（旧 `terminalPolicy` 路径残留） | 检查 Phase 4 的 `CompleteFakeRouteStage` 接线 |
| 回合恢复后重复发奖励/重复战斗 | 存档点与 `generation` 保护 | 回退 Phase 4 的存档路径 |
| 选关不解锁 | `MarkStageCleared` 未接 | Phase 4 |
| 出按钮但位置/指向不对 | `layout` 数据或 CanvasScaler 适配 | 回退 Phase 1/3 |

## 8. 决策记录

### 8.1 已确认（用户 2026-10-14）

1. **出口数据形态照搬视频分支的「选择内嵌节点」**——但字段集合以**当前节点设计**为准（当前设计与视频分支原有设计不一致，见 Phase 6）。是否把该迁移排到最后待确认（Phase 6）。
2. **`openingTravel` 独立成 `FakeRouteTravelPresentation` 资产**（建议路径 `Assets/RouteData/ChangbanpoScroll/Travels/Travel_Opening.asset`），替掉运行器里的 `openingDuration` + `travelProgressCurve` 硬编码。
3. 旧 V2（`RouteStageRuntimeV2` / `RouteStageTargetsV2` / `RouteStageConfigV2` / `RouteStageV2` 场景）**本轮保持禁用**，切换验收通过后单列清理轮。
4. **开场暂不播**：`openingTravel` 字段保留但留空（留空 = 直接进 startNode）。依据：N1 锚点距离为 0，现有 3 秒开场是无位移空等。
5. `completionPolicy` / `rewards` **删除**、出口数据形态**内嵌**、出口 UI 用**按钮型**（均已确认）。
6. Phase 6（出口内嵌迁移）**排到最后**；字段/旧组件的删除统一放到 Phase 7。

### 8.2 待确认

1. Phase 3 应用时机：需 Edit Mode（编译 + 域重载）。若编辑器在 Play Mode（他人测试中），需等其结束或显式授权停 Play。
2. `BattleYRouteHost.cs` / `BattleYVisualBridge.cs` 的最终删除（连带 `YScrollSample.battleHost` 类型改动）：建议本轮只禁用不删（详见 8.3），与 Phase 7 一起处理。

### 8.2.1 删除动作延后的原因（重要）

用户已定「删 `completionPolicy` / `rewards`」，但**不能在与模型增量同一批执行**：`Assets/Scripts/Core/BattleYRouteHost.cs` 仍读 `node.rewards`(121) 与 `node.terminalPolicy`(142/166)，删字段会使旧 host **编译失败**，连带拖垮整个工程（包括其他人正在做的改动）。因此：

- Phase 3 只做**追加**（`entryDialogue / postBattleDialogue / postBattlePresentation / isFinalNode / layout / openingTravel / 快照字段`），旧字段全保留，保证每步都能编译、旧流程可一键切回；
- 删除 `completionPolicy / rewards / terminalPolicy` 与旧 host、`YScrollSample.battleHost` 类型修正、旧 V2 处理统一放入 **Phase 7（清理轮）**，可与 Phase 6 同批或后置。

### 8.3 两个字段的实际作用（供决策）

- `completionPolicy`（`FakeRouteNodeCompletionPolicy { BattleAndReward, ChoiceConfirmation, EnemyDefeated, Immediate }`，定义于 `Assets/Scripts/RouteFake/FakeRouteNodeConfig.cs:4/15`）：预演期设想「节点内容怎样算完成」。**全项目无任何读取点**（只有定义与 6 个资产上的值：N1/E0/N2/N3 = `BattleAndReward`，J1/EV = `ChoiceConfirmation`）。正式模型由节点结构自动决定：有未完成 `BattleEntry` → 战斗；没有 → 出口/终点。
- `rewards`（`List<KillRewardEntry>`，类型定义于 `Assets/Scripts/Core/KillMilestoneConfig.cs:22`，属战斗内击杀里程碑奖励）：6 个节点全为空，**无发放代码**；唯一读取点是 `Assets/Scripts/Core/BattleYRouteHost.cs:121`，且是当作「不支持」的拒绝条件（节点填了 rewards 会导致流程拒绝启动）。正式模型节点不发独立奖励，奖励全部来自战斗。
- `BattleYVisualBridge.cs`：18 行旧桥接组件，已加载场景中 **0 个实例**（死文件）。
- `YScrollSample.battleHost`（`Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs:15`）：字段类型为 `BattleYRouteHost`，实查值为 **null**，仅用于 `Update` 里的 `battleHost == null` 自走判断；删除 host 类必须同批改这一行类型。

## 9. 参考

- 两版对照：`plan/route-flow-current-vs-video-branch.md`
- 逐文件清单：`plan/route-flow-port-changelist.csv`
- 视频分支权威需求：`design/route-fake-movement-architecture.md`
- 节点与叙事节奏：`design/changbanpo-six-node-level-design.md`
- 既有移植评审（清理顺序与测试门槛）：`plan/scroll-route-video-branch-review.md`
- 旧空间移动架构（与本方案冲突，仅作历史）：`design/route-stage-gameplay-spec.md`
- 竖屏 UI 适配：`design/ui-resolution-adaptation.md`
- 工具结果纪律：`memory/tool-result-verification.md`

## 10. 执行进度

### Phase 0（完成）

- 基线 commit：`d94e4a0`。工作树状态快照：`Library/Locus/tmp/route-port-baseline-status.txt`。
- 57 个目标文件的哈希与原始字节副本：`Library/Locus/tmp/route-port-baseline-hashes.txt`、`Library/Locus/tmp/route-port-backup/`。
- 目标文件中的**既有脏文件（不得覆盖）**：`Assets/Scenes/Battle.scene`、`Assets/Experiments/CurvedScroll/YJunctionSample.unity`，记录见 `Library/Locus/tmp/route-port-baseline-dirty-targets.txt`。

### Phase 1（完成；纯新增，零行为影响）

移植 12 个文件（含 `.meta`；文本统一 CRLF；字节级保持 GUID）：`Assets/Scripts/Route/RouteChoiceLayout.cs`、`RouteChoicePanel.cs`、`RouteChoiceButton.cs`、`Assets/Prefabs/UI/RouteChoicePanel_Test.prefab`、`RouteChoiceButton_Test.prefab`、`Assets/Sprites/UI/RouteChoiceArrow_Test.png`。

Editor 实查验证：

- 三个类型进入 `Assembly-CSharp` 并可解析；
- 两个 prefab 载入成功，`GetMonoBehavioursWithMissingScriptCount = 0`；
- 引用全部命中：`panel.buttonPrefab → RouteChoiceButton_Test`、`panel.defaultIcon → RouteChoiceArrow_Test`、`button.labelText → Label（方正粗黑宋简体 SDF）`、`button.iconImage → Icon`；
- GUID 与视频分支一致：`RouteChoicePanel.cs = 94e0fdf7…`、`RouteChoiceButton.cs = 304e8317…`、`RouteChoiceButton_Test.prefab = 1ed1e4ba…`、`RouteChoiceArrow_Test.png = bb042fea…`、`RouteChoicePanel_Test.prefab = 5295112b…`（与视频分支 `Battle.scene` 中 `choicePanelPrefab` 的引用 GUID 相同）；
- Console：0 error / 0 warn。

未做：未接线；未改 `Battle.scene`、`MainMenu.scene`、`YJunctionSample.unity`；未改任何共享脚本与路线资产。

### Phase 3（完成；纯追加，零行为影响）

精确补丁表：`plan/route-flow-phase3-patch-sheet.md`。实际落地：

- 5 处编辑：`Assets/Scripts/RouteFake/FakeRouteNodeConfig.cs`（+`entryDialogue/postBattleDialogue/postBattlePresentation/isFinalNode`）、`FakeRouteChoiceConfig.cs`（+`layout`）、`FakeRouteStageConfig.cs`（+`openingTravel`）、`FakeRouteStageSaveSnapshot.cs`（+`ultimateEnergy/killCount/coinCount/choiceHistory` + `FakeRouteChoiceSaveState` 类）、`FakeRouteGraphValidator.cs`（`IsTerminal` helper + 2 处终点判定改用 `IsTerminal`）；
- 2 个新文件：`Assets/Scripts/RouteFake/FakeRoutePresentation.cs`（29 行，含 meta）、`Assets/Scripts/RouteFake/Editor/FakeRouteValidator.cs`（69 行，含 meta）；
- 资产：仅 `Node_N2.asset` / `Node_N3.asset` 写入 `isFinalNode = true`（Unity API + `SaveAssetIfDirty`，未触发全量 `SaveAssets`，未动他人未保存的场景）；
- 保留：`dialogues/rewards/completionPolicy/terminalPolicy`（删字段会使旧 host 编译失败，延后到 Phase 7）。

验证证据：

- 编译通过（`status: compiled`，域重载完成）；
- 回读：N2/N3 `isFinalNode=True`、其余 `False`；6 个节点 `savePoint=True`；5 个出口 `layout` 非空且 `presentation` 非空；`openingTravel=null`（不播开场）；
- `FakeRouteGraphValidator.TryValidate == True`；
- `Tools/Fake Route/Validate Assets` 可运行：`Changbanpo_ScrollRoute` 无错误；唯一错误来自既有实验资产 `Assets/Experiments/CurvedScroll/RouteTrialData/ValleyBranchTrial.asset`（节点 `D_End` 非终点且无出口），**与本流程无关**，未修改；
- 改动面：`git diff --numstat` 仅小增量（Node_N2/N3 各 +4，选择/关卡配置 +2，校验器 +7/-2，节点配置 +7，快照 +13）；
- 未改：`Battle.scene`、`YJunctionSample.unity`（保持他人既有 `M`）、`MainMenu.scene`、任何共享脚本。

落地时的两处适配（已记入补丁表）：移植的 `FakeRouteValidator` 第 37 行原为视频模型判断（`presentation.mode/videoClip`），已改为卷轴模型的 `presentation == null`；`errors` 计数器从循环外移入循环内，避免跨资产误报。

行尾约定：工程内绝大多数文件为 CRLF（`StageController`/`SaveManager`/prefab/`BattleYRouteHost`），而早期用工具生成的 `Assets/Scripts/RouteFake/**` 与 `Assets/RouteData/ChangbanpoScroll/**` 曾是 LF。本次把**改动过的**文件统一为 CRLF（含 Phase 1 新增文件）；git `autocrlf=true` 下不影响 diff。后续做漂移哈希比对时，这 9 个文件应视为“已由本方案有意改动”。

### 决策补充

- **开场旅行暂时跳过**：`FakeRouteStageConfig.openingTravel` 保留为独立资产字段，留空即不播开场。依据：N1 锚点距离为 0，现有 3 秒开场是无位移空等。
- `completionPolicy` / `rewards`：按用户决定删除。
- 出口数据形态内嵌迁移：排为 Phase 6，最后执行。

### Phase 5（完成；场景接线已保存）

- `Assets/Scenes/Battle.scene` 新增对象 `Route Flow (Scroll)`：`ScrollRoutePresenter`（`routeSceneName` = `Assets/Experiments/CurvedScroll/YJunctionSample.unity`、`battleCamera` = Main Camera）+ `FakeRouteRuntime`（`presenterBehaviour` = 该 Presenter、`choicePanelPrefab` = `Assets/Prefabs/UI/RouteChoicePanel_Test.prefab`、`autoStart` = true、`routeConfig` 留空）。
- 旧驱动 `Battle Y Route Host` 设为 `SetActive(false)`（对象保留，回退 = 重新勾选）。
- `StageController.autoStartStage` 仍为 false；`StageController.fakeRouteStageConfig` **场景留空** → 直接 Play `Battle.scene` 不会自启动（不影响其他人现有测试）；启动入口为 MainMenu。
- `Assets/Scenes/MainMenu.scene` 的 `StageConfigManager.fakeRouteStages` 已加入 `Assets/RouteData/ChangbanpoScroll/Changbanpo_ScrollRoute.asset`（`routeStagesV2` 保留不变），已保存。
- 持久化核对：`Battle.scene` 中 `ScrollRoutePresenter` / `FakeRouteRuntime` / `BattleYRouteHost` / `RouteChoicePanel_Test` 的脚本 GUID 各出现 1 次；旧驱动对象 `m_IsActive: 0`；`YJunctionSample.unity` 哈希与基线一致（未改）。

#### 用户验收步骤

1. 打开 `Assets/Scenes/MainMenu.scene` 并 Play（建议用选关里的 “[卷轴]” 按钮，不会删存档；也可用“继续游戏”）。
2. 预期：Battle 加载 → N1 战斗 → 清场后停在奖励界面直到三选一/经验收集结束 → **箭头按钮面板**出现（N1 单出口）→ 点击 → 卷轴旅行到 E0 → E0 战斗 → 奖励 → 单出口 → 旅行到 J1 → **两个箭头按钮**（左：村庄石路 → EV；右：官道断后 → N2）→ 点击 → 旅行 → EV（无战斗，单出口）→ N3（终点战）→ 战斗+奖励结束后进入整关结算（Victory + 标记通关）。
3. 反面验收：清场后不能绕过奖励直接进下一节点；双击出口只能提交一次；旅行期间不能操作；失败后点“重开”应从最近存档点（6 个节点均为 savePoint）重新进入且不重复发奖励。

#### 切回旧流程

启用 `Battle.scene/Battle Y Route Host`，禁用/删除 `Route Flow (Scroll)`（或把 `StageController.fakeRouteStageConfig` 保持为空）；MainMenu 的 `fakeRouteStages` 列表可保留（不影响旧流程）。

### Phase 5 验收中发现并修复的问题

**现象**：从 MainMenu 点卷轴关卡后报 `[FakeRoute] 表现层不可用: 卷轴路线场景未加载或缺少 YScrollSample`，流程不上。

**根因**：`TryValidate` 在 `Begin` 时要求卷轴场景已加载，但场景是在 `PlayOpening` 里才懒加载的 → 顺序矛盾。

**修复**：把校验拆成两段——`Begin` 只做配置级校验（`TryValidate`），新增 `IRouteMotionPresenter.Prepare(config, canContinue)` 在开场前完成“场景加载 + 锚点校验”，并用 `IsSceneReady` 判定；未就绪则明确报错并停在 `Phase=None`（不静默改节点语义）。

### 程序化全链路验收（通过）

从 `MainMenu.scene` Play → 点选关按钮 → Battle → 卷轴流程，实测日志：

```text
N1   WaitingReward → 三选一 ×2 → ChoosingRoute（1 个按钮“继续前进：残门垣前 → E0·残门垣前”，pos=(0,0)）
     → FakeMoving → EnteringNode E0（progress=0.3382 = E0 锚点 90/266.116）
E0   Battle → 奖励 → 单出口 → 旅行
J1   两出口 → 点左（right=False，即左车道 ✓）→ progress=0.845（EV 锚点 225/266.116）
EV   无战斗 → 单出口 → 旅行 → N3（progress=1.000）
N3   Battle → 奖励 → Completed（终点结算）
收尾  stage1 已通关=true；假路线快照已清理（null）；activeFakeRouteStageId=1
```

要点：奖励未结束不会出现出口面板（三选一两次都被阻塞等待）；单出口也需点击；车道由 layout 的 x 符号推断（左=-80 → 左车道）；终点只结算一次。

### 剩余事项

1. **出口箭头位置未授权**：5 个出口的 `layout.overrideDefault` 均为 false，按钮现在出现于面板默认水平排布（2 个出口时为 ±80）。请用 `Tools/Fake Route/Choice Layout Preview` 把箭头摆到“指向对应道路”的位置；车道选择依据同一字段（layout 的 x 符号），因此摆位与车道必须一致。
2. `Changbanpo_ScrollRoute.clearCoinReward = 0`：终点结算暂不发铜钱，数值待定。
3. Phase 6（出口内嵌迁移）、Phase 7（删 `completionPolicy/rewards/terminalPolicy` 与旧驱动）待办。
4. 若希望“直接 Play `Battle.scene` 就跑卷轴路线”，在场景里给 `StageController.fakeRouteStageConfig` 赋值即可（当前特意留空以免干扰其他测试）。
5. **临时诊断未清**：`Assets/Scripts/Core/ScrollRoutePresenter.cs`（本任务新增：`const bool DiagLogs = true` + `DiagReport()`，约 60 行）；`Assets/Scripts/Core/TimedPassiveModule.cs` 的 `[TimedPassiveDiag]`（5 处，含每帧一条的 `Update active`）。上架前必须处理。
6. **玩法观察（待用户定）**：进关卡不操作时玩家约 **8 秒**死亡（实测 100→73 仅 2 秒），一进 `Defeat` 全屏被 `DarkOverlay`(黑 a=0.72) 压暗约 3.5 倍。是否属于设计预期（需要出生保护/降低开局强度）待定。

### 下一步（建议优先级）

> 交接清单（现状 / 已完成 / 关键文件 / 验收口径 / 硬约束 / 待决策）：`plan/route-flow-phase6-handoff-20261015.md`

**P0-a 清理临时诊断 —— 已完成（2026-10-15）**：

- `Assets/Scripts/Core/ScrollRoutePresenter.cs`：已删 `DiagLogs` 常量与 `DiagReport()` 协程及其调用（308 → 246 行）；`unity_recompile` 0 error，Console 不再出现 `[ScrollDiag]`。
- `Assets/Scripts/Core/TimedPassiveModule.cs`：`[TimedPassiveDiag]` **确认保留**——它是未修问题 TODO-4（`plan/unresolved-issues.md`“路线计时被动跨节点时序”）的关键调试日志；要减噪请先与用户确认再加开关。

**P0-b 主任务 = Phase 6：出口内嵌迁移**（用户 2026-10-14 已确认形态，见 §8.1）：

- 目标：把出口数据从独立资产改成“内嵌于节点”（照搬视频分支形态），字段集合以**当前节点设计**为准。
- 关键约束：仍然**只做增量**——旧字段（`completionPolicy` / `rewards` / `terminalPolicy`）与旧 host 保留到 Phase 7，`BattleYRouteHost.cs` 读 `node.rewards(121)` / `node.terminalPolicy(142/166)` 不能提前断（见 §8.2.1）。
- 验收：（1）`Tools/Fake Route/Validator`（`Assets/Scripts/RouteFake/Editor/FakeRouteValidator.cs`）0 error；（2）程序化全链路回归（N1战斗→奖励→E0→J1左路→EV→N3→Completed）与本文档已有回归同口径；（3）改动前后同一位置截图逐段均色一致。

**P1 后续**：出口箭头 layout 授权（用户用预览工具摆位）→ Phase 7 清理旧字段/旧 host/旧 V2 → 终局数值（`clearCoinReward` 等）→ 开局 8 秒死亡这条玩法定调。

### 背景“变灰/看不全”（2026-10-15 结案，用户真机验收通过）

过程与结论（详细证据见 `memory/android-build-state.md` 同名章节）：

1. 天空 quad 渲染正常，屏幕顶部 ⇄ `ValleySkybox_v2` 顶行；`HudCard` 在屏幕**底部**（早先“HUD 挡住天空”是误判）。
2. 真机截图（864×1920 = 0.450）与编辑器同位置帧逐带对比：**天空均占顶部 ~35%，地面从 35% 起**，颜色逐段吻合 ⇒ 纵向构图与宽高比无关（“地面占比高”不是比例问题，是卷轴所处位置不同）。
3. `_VerticalOffset` 是**与比例无关的固定纵向标定**：真机包用 `-0.23`（可见贴图上部 77%，云带位于屏幕 23.5~26.6%）；本任务一度误改为 0，**已回退为 -0.23**（用户明确“真机那套才是我想要的效果”）。
4. 实现“任何比例内容一致、只裁切”的修正：`Assets/Experiments/CurvedScroll/Shaders/YSkyBackground.shader` 的 `cover.y` 由 `min(1, imageAspect/screenAspect)` 改为恒为 `1`（横向仍按比例裁切，纵向永不裁切）。修复后 0.450 / 0.500 / 0.5625 / **0.750** 四种比例的“天空结束行”全为 **16.9%**（修复前 0.750 是 12.8%），分段均色逐段一致。
5. 验收口径固定为：同帧多比例渲染（相机 `targetTexture` 指向不同尺寸 RT）+ “天空结束行” + 5% 分段均色；产物 `Library/Locus/tmp/fix_*.png`（对照 `asp_*.png`）。
6. 已知取舍：比例宽于贴图（>0.667，平板/横屏）时贴图轻微横向拉伸而非裁切（贴图外无内容可露），内容不变；手机竖屏（0.4~0.6）不会遇到。
7. **用户已确认**：真机显示正常、打包能正常运行。

