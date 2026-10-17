---
id: kd_b4ac32f2-7868-43e3-b144-74c1da9e6f1f
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 路线流程移植：Phase 3 补丁表（精确到可直接应用）

> 用途：把 Phase 3（节点模型增量）写成"旧片段 → 新片段"的可直接执行补丁，避免应用时再做设计判断。
> 上游：`plan/route-flow-port-plan.md`；逐文件清单：`plan/route-flow-port-changelist.csv`。
> 应用前提：编辑器处于 **Edit Mode**（需编译 + 域重载）。应用前重跑一次漂移检查（见 §1）。

## 0. 关键顺序修正（务必遵守）

Phase 2（运行器）、Phase 3（节点模型）、Phase 4（结算/存档 API）**属于同一个编译单元**：

- `FakeRouteRuntime` 调用 `StageController.CompleteFakeRouteStage`、`SaveManager.SetActiveFakeRouteStage`、`DialogueManager.IsBusy/QueueEventAndNotify`、`UltimateSystem.SetEnergyForRouteRestore`（Phase 4 内容）；
- `StageController` 又引用 `FakeRouteLaunch` 与 `FakeRouteRuntime`（Phase 2 内容）。

因此写入顺序为 **Phase 3 → Phase 2 → Phase 4，最后一次性编译**。本文件覆盖 Phase 3 的精确补丁；Phase 2/4 以"从视频分支逐块取用"的形式列在 §4/§5。

## 1. 应用前检查（必须全部为"干净"）

```text
# 目标文件是否仍与基线一致（脚本 + ChangbanpoScroll 资产）
# 期望：无输出
python → 比对 Library/Locus/tmp/route-port-baseline-hashes.txt
# 场景是否仍是他人在改（Battle.scene / YJunctionSample.unity）→ 本阶段不得改动
git status --porcelain -- Assets/Scenes Assets/Experiments/CurvedScroll/YJunctionSample.unity
```

最近一次检查结果（2026-10-14）：57 个目标文件**全部与基线一致**，无漂移。

## 2. Phase 3 补丁（5 处编辑 + 2 个新文件 + 2 处资产）

### 3-A `Assets/Scripts/RouteFake/FakeRouteNodeConfig.cs`

在 `public string displayName;` 之后插入（其余字段本阶段**一律保留**）：

```csharp
    [Header("节点内容")]
    public DialogueEventData entryDialogue;
    public DialogueEventData postBattleDialogue;
    public FakeRoutePresentation postBattlePresentation;
    [Tooltip("终点节点：BattleEntry 完成后不显示出口，收尾进入整关结算。")]
    public bool isFinalNode;
```

> 保留但不删除：`dialogues / rewards / completionPolicy / terminalPolicy`。
> 原因：`Assets/Scripts/Core/BattleYRouteHost.cs` 仍读取 `node.rewards`(121) 与 `node.terminalPolicy`(142/166)，删字段会让**旧 host 编译失败**，进而拖垮整个工程与其他人正在做的改动。删除动作统一放到 Phase 7（旧 host 退役后）。

### 3-B `Assets/Scripts/RouteFake/FakeRouteChoiceConfig.cs`

在 `public FakeRouteTravelPresentation presentation;` 之后插入：

```csharp
    [Tooltip("路线选择按钮在面板中的位置与旋转；未覆盖时按默认排布。")]
    public RouteChoiceLayout layout = new RouteChoiceLayout();
```

（`RouteChoiceLayout` 已在 Phase 1 移植进 `Assembly-CSharp`。）

### 3-C `Assets/Scripts/RouteFake/FakeRouteStageConfig.cs`

在 `public string stageName = "假移动路线关卡";` 之后插入：

```csharp
    [Tooltip("开场表现；留空表示不播开场，直接进入 startNode。")]
    public FakeRouteTravelPresentation openingTravel;
```

### 3-D `Assets/Scripts/RouteFake/FakeRouteStageSaveSnapshot.cs`

在 `public string checkpointNodeId;` 之后插入：

```csharp
    public int ultimateEnergy;
    public int killCount;
    public int coinCount;
    public List<FakeRouteChoiceSaveState> choiceHistory = new List<FakeRouteChoiceSaveState>();
```

在文件末尾追加：

```csharp
[Serializable]
public sealed class FakeRouteChoiceSaveState
{
    public string sourceNodeId;
    public string choiceId;
    public string targetNodeId;
}
```

> 现有 `visitedNodeIds / completedNodeIds / routeChoiceHistory(List<string>)` 本阶段保留（已确认全项目**无读取点**，删除无收益且会动到 SaveManager 序列化面）；Phase 7 再决定删除或迁移。

### 3-E `Assets/Scripts/RouteFake/FakeRouteGraphValidator.cs`

三处替换 + 一处新增 + 一个 helper：

1. `if (node == config.startNode && node.terminalPolicy != FakeRouteTerminalPolicy.None)` → `if (node == config.startNode && IsTerminal(node))`
2. `if (node.terminalPolicy != FakeRouteTerminalPolicy.None && node.outgoingChoices.Count > 0)` → `if (IsTerminal(node) && node.outgoingChoices.Count > 0)`
3. 出口缺表现的校验**无需新增**：本工程校验器第 34 行已有 `if (choice.presentation == null) { error = "出口缺少旅行表现: " + choice.choiceId; return false; }`。
4. 新增 helper：

```csharp
    static bool IsTerminal(FakeRouteNodeConfig node)
    {
        return node != null && (node.isFinalNode || node.terminalPolicy != FakeRouteTerminalPolicy.None);
    }
```

> 兼容期语义：`isFinalNode` 与 `terminalPolicy != None` 都视为"终点"，使旧数据与新数据同时通过校验。
> 已核实：5 个出口资产的 `presentation` 均非空，新增校验不会误报。

### 3-F 新文件 `Assets/Scripts/RouteFake/FakeRoutePresentation.cs`

从 `origin/route-fake-movement` 逐字节移植（29 行，含 `FakeRoutePresentationMode { StaticImage, Video, ChibiBlackout }`）。依赖 `UnityEngine.Video`（`Packages/manifest.json` 已含 `com.unity.modules.video`）。该类型在视频分支只被 `FakeMovementPresenter` 使用，移植后处于"仅作为 `postBattlePresentation` 字段类型"的状态。

> 若你不想现在引入该类型：可改为先不加 `postBattlePresentation` 字段，或临时改成 `FakeRouteTravelPresentation`。改动成本极低，但后续 E 类情景节点演出仍需补回。

### 3-G 新文件 `Assets/Scripts/RouteFake/Editor/FakeRouteValidator.cs`

从视频分支移植（69 行）：`Tools/Fake Route/Validate Assets`（资产注册完整性 + `m_Script` 绑定检查）与 `Tools/Fake Route/Reimport And Validate Assets`。对本项目尤其有用——本工程 meta 使用非 hex 混淆 GUID，历史上出现过多节点 `m_Script` 为空的问题。

移植时**必须**做两处适配（已在落地时完成）：

- 第 37 行原为视频模型判断（`choice.presentation.mode == FakeRoutePresentationMode.Video && videoClip == null`），在本工程 `presentation` 是 `FakeRouteTravelPresentation`，已改写为 `if (choice.presentation == null)`（等价于"出口缺少旅行表现"）；
- `int errors = 0;` 原在 for 循环**外**（跨资产累计），会把前一个资产的错误算到后一个资产头上；已移入循环内按资产计数。

### 3-H 资产改动（仅 2 处）

- `Assets/RouteData/ChangbanpoScroll/Nodes/Node_N2.asset`：`isFinalNode = true`
- `Assets/RouteData/ChangbanpoScroll/Nodes/Node_N3.asset`：`isFinalNode = true`
- 其余 4 个节点保持 `false`（默认值）；`terminalPolicy` 与 `savePoint` **不动**。
- `openingTravel` 全部留空（开场暂不播）。
- 执行方式：用 Unity API（`SerializedObject` + `ApplyModifiedProperties` + `AssetDatabase.SaveAssets`），不要手改 YAML。
- 说明：当前 N2/N3 是左右两支的终点；接入正式拓扑（N2/N3 继续向后连）时再改回 `false` 并补出口——不属于本阶段。

## 3. Phase 3 验证

1. `unity_recompile`（Edit Mode）→ 0 error；
2. Console 0 error / 0 warn；
3. 回读：`Node_N2/N3.isFinalNode == true`，其余节点 `false`；5 个出口 `layout` 非空且 `presentation` 非空；`Changbanpo_ScrollRoute.openingTravel == null`；
4. `FakeRouteGraphValidator.TryValidate(Changbanpo_ScrollRoute, out err)` 返回 true（校验器菜单同时可用）；
5. 旧流程仍可用：`Battle.scene` Play，`Battle Y Route Host` 行为不变（读 `rewards/terminalPolicy` 的校验路径未破坏）。

## 4. Phase 2 取用清单（同批写入，最后一起编译）

| 新文件 | 来源 | 必改 |
|---|---|---|
| `Assets/Scripts/RouteFake/IRouteMotionPresenter.cs` | 新写（约 20 行） | — |
| `Assets/Scripts/RouteFake/FakeRouteRuntime.cs` | `Library/Locus/tmp/videobranch/RouteFake/FakeRouteRuntime.cs`（623 行） | ① `RunBattleEntries` 去掉首次 Entry 后的 `yield break`，改为循环跑完本节点全部未完成 Entry；② `TrySelectChoice` 增加"目标已访问则拒绝"；③ 表现调用改走 `IRouteMotionPresenter`；④ 去掉对 `FakeMovementPresenter` 的直接引用，改由接口调用 `PlayOpening/Play/PlayNodePresentation/SetNodeBackground/Skip`；⑤ `Awake` 承担现 host 的独占职责：`stage.autoStartStage=false`、禁用场景内 `RouteStageRuntimeV2` |
| `Assets/Scripts/Core/ScrollRoutePresenter.cs` | 由 `Assets/Scripts/Core/BattleYRouteHost.cs` 的移动/锚点/分支视觉代码改写 | 实现 `IRouteMotionPresenter`；空引用容忍（Y 场景 `leftBranchVisuals/rightBranchVisuals/junctionVisuals` 当前为 null）；不改 `YScrollSample.Evaluate/Length/turnAngle` |
| `Assets/Scripts/RouteFake/FakeRouteLaunch.cs` | 视频分支 5 行 | — |

## 5. Phase 4 取用清单（同批写入）

| 文件 | 改动 | 视频分支参照 |
|---|---|---|
| `Assets/Scripts/Managers/StageController.cs` | ① 字段 `public FakeRouteStageConfig fakeRouteStageConfig;`；② `Awake` 最前加 `FakeRouteLaunch.PendingConfig` 分支（同时清空 `routeStageConfigV2/routeStageConfig/stageConfig`）；③ `StartStage` 最前加 fake 分支（`_routeStageSettled=false` → `ResetPlayer` → `ResetEnergy` → `ClearAllEnemies` → `FindObjectOfType<FakeRouteRuntime>().Begin(cfg)`）；④ 新增 `CompleteFakeRouteStage(int)`（照 `CompleteRouteStage` 写，`MarkStageCleared(fakeRouteStageConfig.stageId)`）；⑤ `SetRouteRewardWaitState` 内补 `UltimateSystem.Instance?.CancelUltimate();`；⑥ `OnPlayerDefeated` 增加 fake runtime 的 `HandleStageDefeat()`；⑦ `RestartStageCoroutine` 增加 fake 分支（清敌 → `ResetPlayer` → 等一帧 → `StartStage()`）；⑧ `CleanupRouteStage(bool)` 增加 fake runtime 处理 | 视频版 `StageController.cs` 573-584 / 76-115 / 176-196 / 419-440 / 489-545 |
| `Assets/Scripts/Core/SaveManager.cs` | `SaveData` 增加 `activeFakeRouteStageId`；新增 `SetActiveFakeRouteStage / GetActiveFakeRouteStageId / ClearActiveFakeRouteStage` | 视频版 167-190 |
| `Assets/Scripts/Core/UltimateSystem.cs` | 增加 `_activeEffect` 记录 + `SetEnergyForRouteRestore(int)` + `CancelUltimate()` | 视频版 107-113 / 145-162 |
| `Assets/Scripts/Core/UltimateEffect.cs` | 增加 `public virtual void Cancel() { }` | 视频版 13-15 |
| `Assets/Scripts/UI/DialogueManager.cs` | 增加 `IsBusy`（`_isPlaying \|\| _queue.Count > 0`）与 `QueueEventAndNotify(DialogueEventData)` | 视频版 24-28 / 98-101 |
| `Assets/Scripts/Core/StageConfigManager.cs` | 增加 `public List<FakeRouteStageConfig> fakeRouteStages`（保留 `routeStagesV2`） | 视频版 15-16 |
| `Assets/Scripts/UI/MainMenuUI.cs` | 增加 FakeRoute 选关按钮 + `OnFakeRouteStageSelected` + 新游戏/继续分支（从 `startNode` 开始、清快照、`SetActiveFakeRouteStage`） | 视频版对应方法 |

> 本轮**不做**：`SpikeTrapController` 视频版重写（当前分支已有 `SetRouteVisibility` 路线开关）、`TimedPassiveModule` 去日志（当前分支保留诊断）、`UpgradeChoicePopup` 差异（与路线流程无关）。`SaveManager` 的 `GetFakeRouteSnapshot/SaveFakeRouteSnapshot/ClearFakeRouteSnapshot` 当前分支**已有**，无需移植。

## 6. Phase 3 回退

```text
1) 覆盖回滚 5 个脚本：从 Library/Locus/tmp/route-port-backup/ 逐文件复制回原位
2) 删除新增文件：FakeRoutePresentation.cs(+meta)、Editor/FakeRouteValidator.cs(+meta)
3) 资产回滚：Node_N2 / Node_N3 的 isFinalNode 置回 false（Unity API，随后 SaveAssets + 回读）
4) 编译确认 0 error；Console 0/0
```

新增字段是纯追加，回退不涉及数据迁移；`Assets/RouteData/ChangbanpoScroll/**` 的 5 个出口与 5 个旅行资产本阶段**不改内容**。

## 7. 应用后的检查点

- [ ] 5 处编辑 + 2 个新脚本 + 2 处资产完成，编译 0 error
- [ ] Console 0/0
- [ ] 回读字段与 `TryValidate == true`
- [ ] 旧流程 Play 回归（`Battle Y Route Host` 行为不变）
- [ ] `git status --porcelain` 确认只多了预期的文件
