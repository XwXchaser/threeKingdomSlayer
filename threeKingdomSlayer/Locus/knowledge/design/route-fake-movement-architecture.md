---
id: kd_4518874e-fcb2-4448-b3e6-c134e15466ca
injectMode: inherit
aiEditMode: inherit
---

# 假移动场景化路线：需求与实现方向

## 1. 文档定位

本文是当前 `route-fake-movement` 分支的场景化路线权威需求与实现方向。

旧空间移动版本保留在 `route-scene-v2-baseline` 分支，仅作为历史基线和战斗流程参考，不构成新方案的兼容约束。新方案允许直接舍弃旧路线相关代码、组件和资产模型，重新制作纯逻辑路线系统。

本文优先级高于旧路线文档中关于 `RouteStageRoot`、Head、CombatArea、Tail、路径、旋转、Pose 和 RouteStage Scene 的内容。旧文档中的空间移动规则不适用于本方案。

---

### 当前实现阶段（2026-03）

FakeRoute 假移动路线已完成可验收的逻辑垂直切片与存档恢复闭环：

- 新建 `Assets/Scripts/RouteFake/` 纯逻辑路线层，不依赖旧空间移动运行器；
- `Battle.scene` 作为唯一战斗运行场景，节点不是 Unity Scene；
- 节点只保存逻辑 ID、BattleEntry、终点/存档点属性和出口；
- 路线选项直接引用目标节点，支持多来源汇入同一目标节点；
- 当前测试拓扑为 `A → B/C → D`，D 为唯一终点；
- A/B/C/D 使用不同的普通敌人和混合阵列，已验证节点战斗配置确实随节点切换；
- 假移动当前使用独立 `FakeMovementPresenter` 的代码占位等待，支持暂停语义和跳过占位表现；
- FakeRoute 配置包含基础编辑器校验与运行时校验；
- 节点战斗清空会等待奖励、经验收集和弃置流程后才推进；
- B/C 作为存档点，目标节点提交后、目标战斗开始前保存快照；
- 失败恢复先清理当前运行态，再从对应存档点节点重新进入；
- 快照已保存并验收：节点、BattleEntry、路线选择历史、玩家生命/复活/等级/经验、击杀数、局内铜钱、被动等级、主动技能持有/等级、UT 能量；
- 主动技能冷却、普通攻击冷却、计时被动剩余时间、敌人、投射物、连击、QTE、临时效果和占位动画进度不保存，恢复时重置；
- 新旧路线快照已隔离，并通过 `routeArchitectureId`、`snapshotVersion`、`routeId`、`stageId`、`configurationVersion` 校验；
- MainMenu“继续游戏”不读取失败恢复快照，而是从最后未完成路线关卡的 `startNode` 开始；
- 上述路线拓扑和快照恢复已由用户实际验收。

## 当前已知边界与后续作业

- 正式路线选择 UI 尚未替换当前调试 `OnGUI` 面板；
- 当前已实现节点专属路线选择前转场：奖励流程完成后先播放 `routeChoiceTransition`，完成后切换到 `routeChoiceBackground` 并显示路线选择 UI；
- `FakeRoutePresentation` 已支持静态图片和视频，并已用于节点 Combat 背景、路线选择画面及路线移动表现；
- 当前测试使用 `RouteChoiceBlack` 作为路线选择前转场和选择路线后的黑屏过场，使用道路图片作为路线选择画面；
- 正式视频、最终背景资源、音效和正式路线选择 Canvas UI 仍待接入；
- 条件系统尚未定义，因此 `conditionEnabled` 仍是临时测试开关；
- 剧情和剧情选项状态尚未定义或保存；
- 更复杂的节点阶段状态尚未定义；当前只在安全节点边界保存，不保存假移动或战斗中间状态；
- 旧 `Route/`、`RouteV2/` 脚本与旧 RouteStage 场景仍保留在工程中，仅确保不被 FakeRoute 新运行时引用；
- 计时被动跨节点的状态机问题属于既有战斗系统后续事项，不是 FakeRoute 存档闭环已完成的证明。

## 可复用开发经验

1. 假移动方案应将“路线逻辑”和“移动表现”严格分离：运行器提交目标节点，Presenter 只播放表现并回调完成。
2. 不要在旧空间移动运行器上删除函数拼接新语义；当空间模型被彻底废弃时，新建纯逻辑运行器更容易保证状态闭环。
3. 路线场景的分支汇入不需要空间特殊处理。多个 Choice 直接引用同一目标节点即可。
4. 路线节点战斗清空不等于 BattleEntry 完成，必须等待经验收集和阻塞奖励 UI，否则会吞掉升级或提前进入下一节点。
5. 快照恢复使用 replace 语义：先清理当前运行态，再按快照重建 Build 和玩家状态；UI 刷新应发送显示同步事件，不能伪造升级事件。
6. 新旧路线节点即使使用相同名称或 `stageId`，也不能直接复用快照；应使用独立存档集合和架构/路线/配置版本校验。
7. 存档只在安全节点边界写入，可以避免保存战斗波次、动画中间帧和临时对象；失败恢复统一从最近有效节点重新进入。
8. 配置中的稳定 `nodeId` 是玩家数据契约，不能由对象名称或数组索引替代；节点语义变化时应更换 ID 或递增配置版本。
9. 编辑器/Unity 场景操作后必须重新检查实际序列化引用，不能仅凭代码修改推断场景已接线；新��组件结构性改动必须真实编译后再验证。



## 2. 产品目标

当前路线关卡由多个逻辑 CombatNode 组成。玩家完成一个节点战斗后选择路线，系统通过播放背景画面模拟移动，随后进入玩家选择的目标节点并开始挑战。

本方案的核心不是“把真实移动换成另一种移动实现”，而是将路线系统重构为：

```text
逻辑节点图
+ 玩家路线选择
+ 背景动画表现
+ 节点战斗流程
+ 路线状态快照
```

玩家看到的是移动表现，程序处理的是逻辑节点切换。两者必须解耦。

---

## 3. 不可变空间规则

新方案中以下对象都不发生移动：

- Player Transform；
- Battle Camera；
- Battle 场景中的敌人和战斗坐标；
- Battle 场景中的背景根节点；
- 任意路线场景根节点；
- 不存在的 RouteStage Scene 实例。

新运行时不得执行或依赖：

- RouteStage Scene Additive 加载/卸载；
- `RouteStageRoot` 位移或旋转；
- Head / CombatArea / Tail 对齐；
- 路径点、TravelPath、rotationPivot；
- 世界坐标、局部坐标或 Pose 计算；
- 通过场景空间关系决定目标节点。

路线节点不再代表空间位置，而只代表一个逻辑关卡单元。

---

## 4. Unity 场景边界

新方案只使用以下 Unity 场景转换：

```text
MainMenu.unity
    ↓ 场景加载
Battle.unity
```

路线节点不是 Unity Scene，不单独加载或卸载。进入 Battle 后，整局路线始终由 Battle 场景中的纯逻辑路线运行器管理。

运行时结构建议为：

```text
Battle.scene
├─ Player
├─ Enemy / EnemyPool / WaveSpawner
├─ StageController
├─ BattleHUD
├─ FakeRouteRuntime
├─ FakeMovementPresenter
├─ RouteChoicePanel
└─ FakeRoutePresentationRoot
```

`FakeMovementPresenter` 使用 Battle 场景内已有的背景表现对象或直接引用的表现 Prefab，不创建路线 Unity Scene。

---

## 5. 纯逻辑数据模型

建议不继续使用 `RouteStageConfigV2` 的命名和类型，避免把旧空间语义带入新系统。推荐新建：

```text
FakeRouteStageConfig
├─ routeId
├─ stageId
├─ stageName
├─ startNode
├─ nodes[]
└─ clearCoinReward

FakeRouteNodeConfig
├─ nodeId
├─ displayName
├─ battleEntries[]
├─ isFinalNode
├─ savePoint
├─ battleBackground
├─ routeChoicePresentation
└─ outgoingChoices[]

FakeRouteChoiceConfig
├─ choiceId
├─ displayName
├─ targetNode
└─ presentation
```

### 5.1 路线配置

`FakeRouteStageConfig` 负责：

- 本关稳定 `routeId`；
- 关卡解锁和结算使用的 `stageId`；
- 关卡显示名称；
- 唯一 `startNode`；
- 本关全部节点的直接 Inspector 引用；
- 整关奖励。

`startNode` 是唯一的正常开局入口。不能依据节点数组顺序、对象名称、空间位置或场景顺序推断起点。

### 5.2 节点

`FakeRouteNodeConfig` 负责：

- 节点稳定 `nodeId`；
- 节点展示信息；
- 有序 BattleEntry；
- 终点属性；
- 存档点属性；
- 从当前节点可选的出口。

节点不包含 Head、CombatArea、Tail、场景引用或空间数据。

### 5.3 BattleEntry

每个 BattleEntry 直接引用现有 `StageConfig`：

```text
FakeRouteBattleEntry
└─ battleConfig: StageConfig
```

BattleEntry 按配置顺序处理：

- 条件满足且未完成：开始挑战；
- 条件不满足：本次跳过，不写成永久完成；
- 已完成：按规则跳过；
- 所有条目处理完成：节点完成。

当前旧 V2 的 `conditionEnabled` 只是临时跳过测试开关，不应作为正式条件系统继续扩展。正式条件应后续使用明确的条件资产或条件数据结构。

### 5.4 路线选项

每个选项直接引用目标节点和表现：

```text
当前节点 A
├─ Forward → Node B → ForwardPresentation
├─ Left    → Node C → LeftPresentation
└─ Right   → Node D → RightPresentation
```

`choiceId` 用于稳定存档、诊断和 UI 识别；运行时目标使用 Inspector 直接引用，不通过字符串查找节点。

一个目标节点可以被多个来源选项引用。不同来源只代表不同的逻辑选择和表现，不需要保存多个目标 Pose。

---

## 6. 游戏流程

### 6.1 开局

```text
MainMenu 选择路线关卡
→ 设置待启动 FakeRouteStageConfig
→ 加载 Battle.unity
→ FakeRouteRuntime 初始化
→ 从 startNode 开始
→ 直接执行 startNode 的 BattleEntry
```

开局没有 Head 抵达、节点内部移动或空间对齐阶段。

### 6.2 节点挑战

```text
进入节点
→ 设置当前节点和当前阶段
→ 按顺序处理 BattleEntry
→ 启动 StageConfig 战斗
→ 战斗清空
→ 等待三选一、Boss 奖励、弃置和经验收集流程结束
→ 标记当前 BattleEntry 完成
→ 处理下一条 BattleEntry
→ 所有条目处理完成
```

战斗清空事件不能直接推进路线选择，必须经过奖励等待阶段。

### 6.3 路线选择

```text
普通节点战斗完成
→ 等待奖励、经验、弃置流程完成
→ 播放当前节点 routeChoicePresentation
→ 表现完成后显示当前节点 outgoingChoices 和路线选择 UI
→ 玩家点击一个选项并立即锁定
→ 记录 sourceNode / choiceId / targetNode
→ 清理跨节点战斗短时状态
→ 进入 FakeMoving
```

即使只有一个出口，也需要玩家确认，不能自动进入目标节点。

### 6.3.1 路线选择前表现

路线选择前表现由当前节点的 `routeChoicePresentation` 直接引用。它负责把 Combat 画面过渡到可展示路线分叉的画面；纯黑静态图片只是首版占位手法，后续可替换为静态图、视频或 Animator 表现。

该阶段不显示路线选择 UI、不接受路线选择输入、不提交目标节点、不写入新的路线存档。暂停时冻结表现、音频和计时；跳过只完成当前表现并进入路线选择 UI。表现完成后才进入 `ChoosingRoute`，显示当前节点对应的分叉画面和出口选项。

### 6.3.2 路线选择画面

路线选择画面属于当前节点的非战斗表现，不等同于目标节点空间位置。其背景由当前节点的 `routeChoicePresentation` 提供，路线 UI 只读取当前节点的 `outgoingChoices`。每个节点可配置自己的路线选择画面；即使只有一个出口，也必须等待玩家确认。

### 6.4 假移动

```text
FakeMoving
→ FakeMovementPresenter 播放该选项绑定的背景动画
→ 播放转场和音效
→ 动画完成或玩家跳过
→ 只提交一次目标节点
→ 进入目标节点
```

动画只负责视觉表现，不负责：

- 决定目标节点；
- 修改路线状态；
- 保存快照；
- 发放奖励；
- 启动战斗。

动画失败时不得静默把当前节点改成目标节点。应由运行器决定重试、回退或报错；首版可在配置和资源有效性保证下直接报告失败并停留在安全状态。

### 6.5 抵达目标节点

动画完成后，运行器提交目标节点。提交成功等价于旧设计中的“抵达目标节点 Head”：

```text
提交目标节点
→ 更新 currentNode
→ 更新节点阶段
→ 若目标节点为 savePoint，立即写入快照
→ 开始目标节点 BattleEntry
```

存档发生在目标节点提交之后、目标节点战斗开始之前。

### 6.6 终点节点

```text
终点节点 BattleEntry 全部处理完成
→ 不显示路线选择
→ 播放终点表现（如有）
→ 终点表现完成或跳过
→ 发放整关奖励
→ 标记关卡通关
→ 进入现有 Victory 结算
```

整关奖励只能发放一次。终点节点不需要 outgoingChoices。

---

## 7. 路线运行时状态机

新运行器应使用明确的枚举状态，而不是依赖多个布尔值组合：

```text
None
EnteringNode
Battle
WaitingReward
RouteChoiceTransition
ChoosingRoute
FakeMoving
Completed
Defeated
```

正常流程：

```text
EnteringNode
→ Battle
→ WaitingReward
→ RouteChoiceTransition
→ ChoosingRoute
→ FakeMoving
→ EnteringNode
```

终点流程：

```text
EnteringNode
→ Battle
→ WaitingReward
→ Completed
```

失败流程：

```text
任意运行阶段
→ Defeated
```

必须使用递增的 `routeGeneration`、operation token 或等价机制保护异步回调：

- 新节点开始时使旧 token 失效；
- 新 BattleEntry 开始时使旧战斗清空回调失效；
- 新假移动开始时使旧动画回调失效；
- 失败后所有旧回调失效；
- 路线按钮点击和动画完成只能提交一次。

---

## 8. 背景表现层

`FakeMovementPresenter` 只负责：

- 播放前进、左转、右转或特殊移动画面；
- 播放遮罩、转场和音频；
- 响应暂停；
- 响应跳过；
- 在一次表现结束时触发一次完成回调。

表现优先使用 Inspector 直接引用的 Prefab 或表现资产，不使用字符串动画状态查找。表现资源可以内部使用 Animator，但 Animator 不直接调用路线运行器。

推荐表现资源边界：

```text
FakeMovementPresentation
├─ presentationPrefab
├─ duration
├─ skipAllowed
└─ audioClip / optional presentation data
```

暂停时必须冻结表现和表现计时。跳过只能完成当前假移动表现，不能跳过目标节点战斗、奖励或条件处理。

---

## 9. 状态重置规则

### 9.1 节点切换时重置

进入 FakeMoving 前统一清理：

- WaveSpawner 当前生成流程；
- 场上敌人；
- 投射物；
- 当前攻击和蓄力；
- QTE；
- 连击；
- 普通攻击冷却；
- 主动技能剩余冷却；
- 计时被动计时器；
- DoT 和临时战斗效果；
- 战斗输入和当前手势。

必须保留：

- 玩家生命和复活次数；
- 当前等级和经验；
- 被动 Build；
- 主动技能持有情况及等级；
- 其他已确认的跨节点持久状态。

### 9.2 失败恢复时

失败恢复顺序固定为：

```text
停止战斗和假移动
→ 使所有旧 generation/token 失效
→ 执行运行时 ResetAll
→ 读取有效快照
→ 恢复节点和 BattleEntry 状态
→ 恢复玩家持久状态
→ 重建被动/主动技能运行态
→ 从快照节点重新进入
```

`ResetAll` 是预期的清理步骤，不等于丢失快照中的持久状态。它清除的是当前运行对象，快照加载随后负责重建需要保留的状态。

---

## 10. 存档规则

### 10.1 MainMenu 继续游戏

“继续游戏”不读取路线存档点快照。

```text
MainMenu → 继续游戏
→ 查找最后未完成的路线关卡
→ 清理本局运行态
→ 清除该路线旧的失败恢复快照（若规则要求从头开始）
→ 从该关卡 startNode 开始
```

不能固定使用 `routeStageConfigs[0]` 代表最后未完成关卡。应由永久存档中的当前路线标识、通关列表和可用关卡共同决定。

### 10.2 新游戏/选关

```text
选择新路线关卡
→ 清理本局路线状态
→ 清除该关卡旧快照
→ 从该路线 startNode 开始
```

### 10.3 失败后的存档点恢复

```text
玩家失败
→ 清理当前战斗和假移动
→ 执行 ResetAll
→ 载入最近有效存档点快照
→ 从 checkpointNodeId 对应节点重新进入
→ 按已保存的 BattleEntry 状态决定本次挑战内容
```

恢复节点是快照对应的原节点，不是新的后续节点，也不是 `startNode`。该节点重新进入，但快照中已完成的更早节点和 BattleEntry 状态继续保留。

假移动中途不保存动画进度。假移动中途失败时，恢复到上一个有效存档点；没有有效存档点时按本关 `startNode` 重新开始。

### 10.4 存档边界

首版安全存档只发生在逻辑节点提交成功后、目标战斗开始前。因此快照不需要保存：

- 假��动动画进度；
- 假移动中间帧；
- 当前路线选择的未完成提交；
- 当前战斗波次；
- 当前节点战斗临场状态。

---

## 11. 新旧节点存档兼容策略

### 11.1 结论

旧 V2 快照与假移动快照**默认不兼容**，不能直接读取后按同名 `nodeId` 静默恢复。

原因：

- 旧快照的 `currentNodeId` 绑定旧 V2 节点语义；
- 旧节点资产类型是 `RouteNodeConfigV2`，新节点资产类型将不同；
- 旧路线节点可能包含 Head/Tail 过程和空间移动阶段，新节点没有这些阶段；
- 旧快照没有架构版本或路线模型版本；
- 同一个 `stageId` 和同名节点 ID 不足以证明两套配置语义相同。

因此，不能因为旧快照中存在 `A`，就认为新方案的 `A` 可以安全恢复。

### 11.2 推荐隔离方式

优先采用独立的新快照字段或独立存档版本：

```text
SaveData
├─ routeStageSnapshots        // 旧路线/V2历史格式
└─ fakeRouteSnapshots         // 假移动格式
```

新快照至少包含：

```text
FakeRouteStageSaveSnapshot
├─ snapshotVersion
├─ routeArchitectureId
├─ routeId
├─ stageId
├─ configurationVersion
├─ checkpointNodeId
├─ nodeStates[]
├─ routeChoiceHistory[]
├─ playerState
└─ persistentBuildState
```

其中：

- `routeArchitectureId` 固定标识假移动架构，例如 `fake-route-v1`；
- `snapshotVersion` 用于字段演进和迁移；
- `routeId` 标识具体路线配置，不能只依靠 `stageId`；
- `configurationVersion` 标识该路线节点和战斗编排版本；路线拓扑、节点语义或 BattleEntry 映射发生不兼容变化时必须递增；
- `checkpointNodeId` 使用新路线节点的稳定 ID；
- `nodeStates` 使用节点 ID 和 BattleEntry 索引/状态；
- `routeChoiceHistory` 只在未来需要条件或剧情依据时启用。

如果工程上必须复用 `routeStageSnapshots` 字段，则必须新增架构标识和版本字段；缺少标识的旧快照一律视为旧格式，不得进入新恢复路径。

### 11.3 旧快照处理

新方案首版建议：

- 不迁移旧 V2 路线快照；
- 不让新系统读取旧 `routeStageSnapshots`；
- 新系统无有效 `fakeRouteSnapshots` 时，从对应路线 `startNode` 开始；
- 旧快照可以保留用于旧分支，但在新分支中被明确忽略；
- 如果未来产品要求迁移，必须制作一次性显式迁移工具，并逐节点校验新旧路线 ID、节点 ID、BattleEntry 映射和完成语义。

这会丢弃旧 V2 的路线中途恢复数据，但不会影响永久通关记录、铜钱、教程等与路线架构无关的存档数据。

### 11.4 节点稳定 ID规则

新节点的 `nodeId` 必须：

- 在同一 `routeId` 下唯一；
- 一旦进入正式存档后不能随意修改；
- 不使用对象名称替代；
- 不使用数组索引替代；
- 不依赖 ScriptableObject GUID 作为玩家数据中的唯一业务 ID；
- 如果节点被重做导致语义不再相同，应分配新的 `nodeId` 或提高路线配置版本。

---

## 12. 快照字段规划

### 12.1 首版需要保存

```text
FakeRouteStageSaveSnapshot
├─ snapshotVersion
├─ routeArchitectureId
├─ routeId
├─ stageId
├─ configurationVersion
├─ checkpointNodeId
├─ nodeStates[]
├─ currentHealth
├─ currentRevives
├─ currentLevel
├─ currentExp
└─ passiveUpgrades[]
```

节点状态至少包括：

```text
FakeRouteNodeSaveState
├─ nodeId
├─ visited
└─ completedEntryIndices[]
```

当前需求已确认但代码尚未完整支持的字段：

- 后续条件系统状态；
- 剧情和剧情选项状态；
- 更完整的路线选择历史；
- 条件/剧情所需的其他局内状态；

### 12.2 不保存

- 敌人、投射物和场上对象；
- 连击；
- 当前攻击、蓄力和 QTE；
- 临时 Buff、DoT 和表现对象；
- 主动技能剩余冷却；
- 计时被动剩余计时；
- 假移动动画��度；
- 战斗波次和当前战斗临场状态。

技能持有/等级属于跨节点状态，技能剩余冷却属于短时状态。节点切换和失败恢复时重置冷却，再按快照重建持有的技能和等级。

---

## 13. 实现边界和代码迁移

### 13.1 新建

建议新建独立目录和类型：

```text
Assets/Scripts/RouteFake/
├─ FakeRouteStageConfig.cs
├─ FakeRouteRuntime.cs
├─ FakeRouteSaveState.cs
├─ FakeRouteLaunch.cs
├─ FakeMovementPresenter.cs
├─ FakeRouteChoicePanel.cs
└─ Editor/
```

对应配置资产放在新的路线数据目录，不与旧 V2 资产混用。

### 13.2 可复用

按实际依赖复用：

- `StageConfig`；
- `WaveSpawner`；
- `EnemyManager`；
- `PlayerState`；
- `UpgradeEffectManager`；
- `ActiveSkillInventory`；
- `TimedPassiveModule`；
- 奖励 UI 和战斗 UI；
- `SaveManager` 的永久存档基础。

### 13.3 新运行时不应引用

```text
RouteStageRuntimeV2
RouteStageSceneEntryV2
RouteCombatNodeEntryV2
RouteConnectionSceneBindingV2
RouteStageTargetsV2
RouteStageRoot
RouteProgressionController
RouteWorldGraph
RouteWorldMotion
RouteStageV2Validator
RouteStageV2PreviewWindow
```

这些类型的主要职责是空间路线或旧路线流程，不应被新逻辑路线运行器依赖。

### 13.4 StageController 边界

`FakeRouteRuntime` 负责：

- 当前路线和节点；
- BattleEntry 顺序；
- 路线选择；
- 假移动；
- 节点状态；
- 存档点；
- 失败恢复；
- 终点结算调度。

`StageController` 负责：

- 启动一个 `StageConfig` 战斗；
- 管理 Wave 和敌人；
- 报告当前 BattleEntry 清空；
- 等待并结束奖励流程；
- 停止战斗；
- 处理玩家死亡和最终结算。

路线控制器不得接管 Wave 内部排队、敌人位移或攻击逻辑。

---

## 14. 分阶段实现计划

### Phase 0：数据和流程设计

- 新建 FakeRoute 数据模型；
- 确定 routeId、nodeId、choiceId 规则；
- 确定 `fakeRouteSnapshots` 隔离策略；
- 明确新旧快照不兼容；
- 完成 Inspector 资产结构和校验规则。

### Phase 1：纯逻辑垂直切片

暂时使用可控的测试等待代替动画：

```text
A 战斗完成
→ 点击 B
→ 测试等待
→ B 战斗开始
```

验证节点切换、BattleEntry、奖励等待、终点和失败流程。

### Phase 2：接入单个背景表现

- 接入一个路线选择；
- 播放一个背景动画；
- 验证暂停、跳过和一次性完成回调；
- 验证动画期间不生成目标节点敌人。

### Phase 3：多出口和汇入

配置并验证：

```text
A → B
A → C
A → D
C → B
D → B
```

确认目标节点只由逻辑引用决定，多个来源不需要任何空间绑定。

### Phase 4：存档点和失败恢复

- 目标节点提交后保存；
- 后续 savePoint 覆盖前一个快照；
- 失败前执行 ResetAll；
- 从快照节点重新进入；
- 存档节点自身 BattleEntry 按规则重新处理；
- 假移动中途不保存动画进度。

### Phase 5：继续游戏和关卡进度

- Continue 查找最后未完成路线关卡；
- Continue 从该关卡 startNode 开始；
- Continue 不读取失败恢复快照；
- 新游戏和选关清除目标路线旧快照；
- 通关后清理或失效路线快照。

### Phase 6：补齐持久状态

- 主动技能及等级；
- UT 充能；
- 条件状态；
- 剧情状态；
- 路线选择历史；
- snapshotVersion 和配置兼容校验。

### Phase 7：清理旧路线实现

完成新系统垂直切片和验收后，再评估删除或移出：

- 旧 V2 运行器；
- 旧 RouteStage 场景和绑定组件；
- 旧路径校验和预览工具；
- 旧 RouteWorld 运行时。

在此之前只禁止新系统引用它们，���急于删除，保留回滚空间。

---

## 15. 验收标准

新系统必须满足：

- Player Transform 不改变；
- Battle Camera 不改变；
- 不加载 RouteStage Scene；
- 不创建或修改 RouteStageRoot；
- 不计算 Head、Tail、路径或 Pose；
- 每个节点可独立配置 Combat 背景和路线选择前/分叉表现；
- 每个路线选项有唯一目标节点和选择后移动表现引用；
- 节点战斗结束后先等待奖励流程，再播放路线选择前表现；
- 路线选择前表现完成或跳过前，不显示路线选择 UI、不接受路线选择输入；
- 表现完成后才显示当前节点对应的路线分叉画面和 UI；
- 路线选择锁定后，才播放所选路线移动表现；
- 移动表现完成前不会提交目标节点或开始目标节点战斗；
- 目标节点提交后切换其 Combat 背景，再按原顺序触发存档点和目标战斗；
- 三类表现均可使用静态图片或视频，纯黑只作为可替换占位表现；
- 所有表现暂停时冻结，跳过只完成当前表现阶段；
- 表现和旧回调不会重复提交节点或重复推进流程；
- 失败恢复先 ResetAll，再从最近有效存档点进入；
- MainMenu Continue 从最后未完成关卡的 startNode 开始；
- 新旧路线快照不会被静默混用；
- 节点 ID 变更会被视为存档兼容性变化；
- 旧空间移动代码不参与新运行时。

最终架构：

```text
一个 Battle Unity Scene
+ 一个纯逻辑节点图
+ 节点 Combat 背景
+ 节点路线选择前/分叉表现
+ 路线选项移动过场
+ 玩家路线选择
+ 多个 StageConfig 战斗内容
+ 独立版本化的假移动路线快照
```

