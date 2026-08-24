---
id: kd_fc1817f0-c03f-43a6-bb88-42e428f07611
injectMode: inherit
aiEditMode: inherit
---

# 场景化路线关卡 V2 架构与局内流程

## 1. 文档定位

本文以当前对话确认的需求为准，覆盖路线关卡的配置边界、Combat 节点场景结构、场景加载和局内流程。

本文不继承旧路线文档中的以下假设：

- 不在 `Battle.scene` 中摆放路线节点、路口、道路或场景白盒；
- 不创建独立的 Junction 逻辑节点；
- 不使用 Authoring 总览场景、Baker、RouteWorldRoot、RouteWorldGraph 或旧 Channel 移动模型；
- 不把节点环境作为 Battle 的常驻层级；
- 不用字符串 ID、`Resources.Load` 或静态缓存寻找运行时场景对象；
- 不把路线关卡的空间坐标写入 Battle 或通过多套补偿换算。

当前设计方向是：

```text
Battle.scene 常驻战斗运行空间
+ RouteStage Scene 按关卡加载一次
+ RouteStage Scene 内包含全部 CombatNode
+ 玩家、敌人、Battle Camera 的战斗坐标固定
+ 通过移动/旋转路线场景根节点模拟玩家移动
```

## 2. 核心概念

### 2.1 Battle 场景

`Battle.scene` 是唯一常驻的战斗运行场，负责：

- Player；
- Enemy、EnemyPool、ColumnManager、WaveSpawner；
- Main Camera 和固定战斗坐标；
- Battle HUD、暂停、升级、QTE、失败和胜利结算；
- 路线流程与 RouteStage Scene 加载接口。

Battle 不负责保存路线节点的空间摆放，不包含 CombatArea、HeadJunction、TailJunction 或 Travel 几何。

### 2.2 CombatNode

路线关卡由若干 CombatNode 组成，但不是每个 CombatNode 一个 Unity Scene。一个路线关卡使用一张独立的路线运行场景，场景内摆放该关卡的全部 CombatNode 及其道路/连接表现；Battle.scene 不包含这些对象。

```text
RouteStageScene
└─ RouteStageRoot
   ├─ CombatNode_A
   │  ├─ HeadJunction
   │  ├─ CombatArea
   │  └─ TailJunction
   ├─ CombatNode_B
   └─ ...
```

三者是玩家视角下的连续阶段，而不是三个同时对齐到 Battle 的目标：

- `HeadJunction`：节点抵达阶段。进入目标节点后，路线场景移动，使玩家从上一节点 Tail 抵达该节点 Head；
- `CombatArea`：战斗阶段。Head 抵达后自动继续移动，使该节点 CombatArea 对齐 Battle 固定战斗区域，随后自动开始战斗；
- `TailJunction`：节点离开阶段。战斗序列和对应奖励完成后，路线场景移动，使玩家从 CombatArea 抵达该节点 Tail；到达 Tail 后才显示路线选择。

因此一个 CombatNode 内部固定是两段移动：

```text
Head → CombatArea
CombatArea → Tail
```

CombatNode 的三个点位均由设计师在 RouteStage Scene 中自由设置。它们不是一次性同时对齐，而是按阶段分别使用。

### 2.3 Junction 不是独立节点

Junction 是 CombatNode 的子节点和空间锚点，不是路线图中的独立节点。

因此不再存在：

```text
CombatNode → JunctionNode → CombatNode
```

实际路线关系是：

```text
CurrentCombatNode.TailJunction
→ RouteConnection.targetNode
→ TargetCombatNode.HeadJunction
```

Junction 不拥有：

- 独立 StageConfig；
- 独立 CombatNode 状态；
- 独立节点奖励；
- 独立 Wave；
- 独立路线推进状态。

## 3. 配置资产分层

### 3.1 StageConfig：战斗内容配置

现有 `StageConfig` 继续只负责战斗内容：

```text
StageConfig
├─ stageId / stageName
├─ waves[]
├─ formationConfig
├─ fillUpRule
├─ killMilestones[]
├─ clearCoinReward
└─ 战斗数值与补齐配置
```

它不负责：

- CombatNode 场景引用；
- Head/Tail 空间锚点；
- 路线出口；
- Travel 表现；
- 路线节点奖励。

一个 CombatNode 可以引用一个 StageConfig。这样既保留现有 Wave/战斗管线，也避免把战斗配置和场景配置混成一个资产。

### 3.2 CombatNodeConfig：单个节点配置

建议新增独立 ScriptableObject：

```text
CombatNodeConfig
├─ nodeId
├─ displayName
├─ battleEntries[]
│  ├─ battleConfig
│  ├─ reward
│  └─ condition
├─ fixedCoinReward
├─ preview
├─ nodeTags / dangerLevel
└─ outgoingConnections[]
```

字段语义：

- `nodeId`：编辑器和存档使用的稳定节点标识；
- `battleConfig`：该节点进入时交给战斗系统的 `StageConfig`；
- `fixedCoinReward`：该节点本局首次完成后的固定金币奖励；
- `preview`、`nodeTags`、`dangerLevel`：路线选择卡展示信息，不暴露精确阵型和数值；
- `outgoingConnections`：从当前节点 Tail 出发的可选连接。

节点的空间对象不保存在 `CombatNodeConfig` 中，而由已加载的 RouteStage Scene 中的 `CombatNodeSceneEntry` 绑定到对应配置。运行时通过场景入口组件取得 Head、CombatArea、Tail 的直接引用，不使用运行时字符串查找。

### 3.3 RouteConnection：Tail 到目标 Head 的逻辑连接

连接定义为 CombatNodeConfig 中的可序列化列表项：

```text
RouteConnection
├─ choiceSlot
├─ targetNode
└─ travelPresentation
```

字段语义：

- `choiceSlot`：路线卡位置，例如 Forward、Left、Right、Back；它是 UI/逻辑槽位，不直接等同世界坐标角度；
- `targetNode`：直接引用目标 `CombatNodeConfig`。当前采用终点节点模式，普通连接必须填写目标节点，终点由目标节点的 `isFinalNode` 表达；
- `travelPresentation`：该连接的移动速度、音频、遮挡和演出参数。

连接的空间路径不放在 ScriptableObject 中，而由 RouteStage Scene 内的 `RouteConnectionSceneBinding` 直接引用：

```text
RouteConnectionSceneBinding
├─ sourceNode / targetNode
├─ sourceTail
├─ targetHead
├─ turnPoints[]
├─ travelPath[]
└─ optional occlusionPoints[]
```

每一条连接都有独立的场景路径。即使多条连接指向同一个目标 Head，也必须分别配置各自的 source Tail、路径和转向数据。
### 3.4 RouteStageConfig：整局路线配置

建议新版本的 RouteStageConfig 只负责整局入口、全局结算和节点清单：

```text
RouteStageConfig
├─ stageId / stageName
├─ routeScene
├─ startNode
├─ allCombatNodes[]
├─ clearCoinReward
└─ optional route-wide presentation

CombatNodeConfig
├─ nodeId
├─ displayName
├─ battleEntries[]
│  ├─ battleConfig
│  ├─ reward
│  └─ condition
├─ fixedCoinReward
├─ preview
├─ nodeTags / dangerLevel
└─ outgoingConnections[]

RouteConnection
├─ choiceSlot
├─ targetNode
└─ travelPresentation
```

`RouteStageConfig.routeScene` 是整张路线场景的唯一 Scene 引用。`CombatNodeConfig` 不引用 Scene，因为所有节点都位于同一张 RouteStage Scene 内。
## 4. 场景结构和空间契约

### 4.1 RouteStage Scene 内的固定节点结构

路线关卡使用一张 `RouteStage Scene`，场景内包含该关卡全部 CombatNode：

```text
RouteStageRoot
├─ CombatNode_N0
│  ├─ HeadJunction       [CombatNodeJunction]
│  ├─ CombatArea         [节点环境根]
│  └─ TailJunction       [CombatNodeJunction]
├─ CombatNode_N1
│  ├─ HeadJunction
│  ├─ CombatArea
│  └─ TailJunction
└─ ...
```

每个 CombatNode 的三个子节点职责固定：

- `HeadJunction`：该 CombatNode 的进入锚点；
- `CombatArea`：节点环境、道路、战斗场地和节点表现；
- `TailJunction`：该 CombatNode 的离开锚点和路线选择出口集合。

建议 `RouteStageSceneEntry` 挂在 `RouteStageRoot`，提供路线场景就绪状态和全部节点入口绑定。每个节点入口提供对应的 Head、CombatArea、Tail 直接引用。这些引用均在路线场景 Inspector 中配置，不由运行时名称查找。

### 4.2 运行时对齐规则

Battle 保留固定的战斗区域目标和固定的路线场景挂点。节点的三点不是同时对齐，而是按玩家阶段分别使用：

1. 进入关卡时，读取 `RouteStageConfig.startNode` 对应的 HeadJunction；
2. 将首节点 HeadJunction 对齐到 Battle 的固定进入位置；
3. 沿首节点内部配置的 Head→Combat 路径移动路线场景根节点；
4. 将首节点 CombatArea 对齐到 Battle 固定战斗区域；
5. 场景就绪后自动开始首节点当前应执行的战斗。

后续路线选择后：

1. 读取当前节点 Tail 和目标节点 Head 的场景 Pose；
2. 根据这条具体连接的路径/转向点，让玩家从当前 Tail 移动到目标 Head；
3. 到达目标 Head 后，沿目标节点内部 Head→Combat 路径移动；
4. 目标 CombatArea 对齐 Battle 固定战斗区域；
5. 自动开始目标节点当前应执行的战斗。

Head→Combat、Combat→Tail、Tail→目标Head 是独立的场景路径阶段。三点可由设计师自由设置，不能假设固定间距、同一直线或统一朝向。

### 4.4 多个 Tail 汇入同一 Head

多个源 CombatNode 可以通过不同 RouteConnection 指向同一个目标 CombatNode 的 Head。目标 Head 是同一个场景 Transform，具有唯一的目标 Pose；不同来源不应为同一个 Head 计算不同的最终落点。

每条入边独立决定：

- 从哪个源 Tail 出发；
- 经过哪些路径点和转向点；
- 何时完成旋转；
- 如何播放遮挡和抵达演出。

共同的结束条件是：目标节点的同一个 Head 到达固定的 Head 阶段目标位置，并随后自动执行该节点内部的 Head→Combat 移动。由于源 Tail 不同，每条入边的旋转过程可以不同；但目标 Head 的最终朝向和位置保持由目标节点统一定义。

因此不存在“同一个 Head 应该旋转三次”的冲突：三条连接是三条不同的 Travel 过程，共享同一个目标 Head 的最终 Pose。只有当设计要求同一节点从不同入口拥有不同的抵达姿态时，才需要为该节点增加多个入口变体，而不能让一个 Head 同时承担多个互相冲突的姿态。

### 4.6 存档点与失败恢复（当前规则）

- `savePoint` 节点在抵达 Head 后立即保存，不在 Tail 保存。
- 保存后继续执行该节点 Head→Combat 和 BattleEntry。
- 失败恢复前先执行运行时 `ResetAll`，清理当前战斗和路线运行态；之后载入最近存档点快照，并从保存节点自身的 Head 重新开始。这里不是从新的后续节点，也不是从 `startNode` 开始。
- MainMenu 的“继续游戏”不读取路线快照，而是从最后未完成关卡的 `RouteStageConfig.startNode` 重新开始。
- 快照不保存敌人、投射物、连击、当前攻击、QTE 和临时 Buff。
- 重开整局清除路线快照并从 `startNode` 开始；失败恢复保留快照并从最近存档节点 Head 开始。

为了避免依靠目测配置，RouteStage Scene 需要编辑器校验/预览工具，至少提供：

- 显示每个 CombatNode 的 Head→Combat→Tail 三阶段路径；
- 显示每个 RouteConnection 的 source Tail、turnPoints、travelPath 和 target Head；
- 预览从任意入边抵达同一 Head 的旋转和平移结果；
- 校验路径首尾是否连接对应 Tail/Head；
- 校验目标 Head 最终 Pose 是否唯一；
- 校验 Head→Combat、Combat→Tail 是否存在有效路径；
- 对多个入边汇入同一 Head 的情况分别绘制路径并报告断线、反向、重叠或朝向异常；
- 提供“应用运行时对齐结果”的编辑器预览，但不修改 Player、Battle Camera 或 Battle.scene。

## 5. 场景加载生命周期

Battle 不保存所有节点场景���例。运行时由唯一的路线场景协调器管理：

```text
RouteStageController
└─ CombatSceneLoader
   └─ TravelPresentationController（如需要）
```

职责边界：

### RouteStageController

负责：

- 当前 CombatNodeConfig；
- 已完成节点集合；
- 已领取节点奖励集合；
- 路线选择；
- 终点节点处理；
- 调度场景加载、战斗开始和 Travel；
- 处理清场后的下一步流程。

### RouteStageSceneLoader

只负责：

- 加载目标路线关卡 Scene；
- 获取该场景的 `RouteStageSceneEntry`；
- 报告路线场景加载完成和场景就绪；
- 当前路线关卡结束时卸载整张路线场景；
- 不决定战斗、不发奖励、不显示路线选择。

路线流程中不卸载单个 CombatNode。CombatNode 是同一张路线场景中的子层级；切换节点只改变路线场景根节点的运行时 Pose 和当前节点状态。
### TravelPresentationController

只负责：

- 锁定路线选择后的输入；
- 控制场景根节点移动/旋转和路线演出；
- 处理暂停和跳过；
- 在一次演出中只调用一次完成回调。

它不决定目标节点、不修改 Player、不生成 Wave。

## 6. 局内完整流程

### 6.1 进入 Battle

```text
MainMenu 选择 RouteStageConfig
→ 加载 Battle.scene
→ Battle 读取待开始的 RouteStageConfig
→ 加载该 RouteStageConfig.routeScene
→ 获取路线场景中的 RouteStageSceneEntry
→ 读取 RouteStageConfig.startNode
→ 将 startNode.Head 对齐到起始进入位置
→ 沿 startNode 内部 Head→Combat 路径移动
→ startNode.CombatArea 对齐 Battle 固定战斗区域
→ 自动开始 startNode 的 battleEntries
```

`RouteStageConfig.startNode` 是进入关卡后第一个 CombatNodeConfig 的唯一来源。开局不显示路线选择。

### 6.2 CombatNode 内部自动流程

```text
进入当前 CombatNode 的 Head
→ 沿 Head→Combat 的场景路径移动
→ CombatArea 对齐 Battle 固定战斗区域
→ 自动开始该节点当前应执行的 battleEntries
→ 每个适用战斗完成后发放对应奖励
→ 所有适用战斗完成/跳过
→ 发放节点完成奖励
→ 沿 Combat→Tail 的场景路径自动移动
→ 到达 Tail
→ 显示路线选择
```

战斗结束后不立即显示路线选择，也不在 CombatArea 停留等待选择。路线选择只发生在到达 Tail 之后。
### 6.3 CombatNode 战斗与奖励序列

一个 CombatNode 可以配置多个有序 `battleEntries`：

```text
CombatNode
→ Head→Combat
→ 检查 battleEntries[0..n]
→ 条件满足且未完成：开始对应 StageConfig
→ 战斗完成：发放对应战斗奖励
→ 检查下一条 battleEntry
→ 所有适用 battleEntry 完成或跳过
→ 发放节点完成奖励
→ Combat→Tail
→ 到达 Tail 后才显示路线选择
```

路线层不得接管 Wave 内部排队、补齐、敌人位移或攻击逻辑。
### 6.4 Tail 路线选择

```text
到达当前 CombatNode.Tail
→ 读取当前节点 outgoingConnections
→ 过滤当前可见/可用出口
→ 显示路线选择卡
→ 玩家点击一个出口
→ 立即锁定选择
```

每个 Tail 可配置多个目标 CombatNode。即使只有一个出口，也需要玩家点击确认，不能自动前进。Head 进入 Combat、Combat 到 Tail 都不需要玩家选择。

### 6.5 Tail→目标Head Travel

```text
路线已锁定
→ 统一清理战斗临时状态
→ 锁定战斗输入和敌人流程
→ 读取当前 Tail 与目标 Head 的场景 Pose
→ 若目标 Head 朝向不同，先按该连接的路径完成旋转
→ 再沿该连接在场景中配置的路径移动到目标 Head
→ 自动开始目标节点 Head→Combat 的内部移动
```

节点切换期间不加载或卸载 CombatNode。所有节点已经存在于当前 RouteStage Scene 中；切换节点只是改变整张路线场景的运行时根节点 Pose，并更新当前节点状态。

玩家的“移动”和“旋转”来自路线场景空间关系：

- 当前 CombatNode 的 `TailJunction` 提供出发位置；
- 目标 CombatNode 的 `HeadJunction` 提供目标位置和目标朝向；
- Tail 与 Head 之间的道路、路径点、转向点和遮挡点由 RouteStage Scene 中的实际 Transform 定义；
- 若目标方向不同，具体连接先旋转再移动；
- Travel 中不能使用代码写死的前进距离、固定 90 度、`Vector3.back` 或额外补偿。

Player、Enemy、Battle Camera 的固定战斗坐标不移动。
### 6.6 终点节点

```text
到达当前 CombatNode.Tail
→ 若当前节点是终点节点，直接执行终点演出
→ 演出完成或跳过
→ 发放整关 clearCoinReward
→ 标记路线关卡完成
→ 进入现有胜利结算并退出/返回主菜单
```

终点节点是普通连接的目标，不是独立的终点连接。进入终点节点后仍执行目标节点的 Head→Combat、BattleEntry 和 Combat→Tail；到达终点节点 Tail 后不再显示路线选择，也不再寻找下一个节点。终点节点可以将 Tail 配置在 CombatArea 附近，以避免无意义的离场位移。
### 6.7 失败和重开

```text
玩家死亡
→ 停止 Wave 和敌人流程
→ 卸载整张当前 RouteStage Scene
→ 清空本局路线状态、已完成节点和未结算节点奖励
→ 保留现有存档规则
→ 重新加载 Battle.scene 和该关卡 RouteStage Scene
→ 从 startNode 重新开始
```

不提供节点检查点。玩家生命、Build、局内资源等跨节点状态只在同一局内保留，整局失败后清空。

### 7. 节点重访规则

一个节点的运行时状态至少包括：

```text
NodeRuntimeState
├─ visited
├─ battleEntryStates[]
├─ nodeRewardClaimed
└─ atTail
```

- 进入 Head、Head→Combat、Combat→Tail 是节点流程，不是路线选择；
- 每个 battleEntry 独立记录是否完成，条件不满足时本次跳过但不视为战斗完成；
- 节点的所有适用 battleEntry 完成后，发放一次节点奖励并自动移动到 Tail；
- 重访节点时根据每个 battleEntry 的状态和条件决定是否再次执行；
- 是否允许重复战斗、条件变化后是否补触发，必须由 battleEntry 的配置规则决定，不能用“场景是否重新加载”推断；
- 节点重访不改变 RouteStage Scene 的加载状态。
## 8. 首版配置校验

首版必须校验：

- RouteStageConfig 有效；
- startNode 存在并属于 allCombatNodes；
- 每个 CombatNodeConfig 有效；
- RouteStageConfig 的 `routeScene` 引用存在；
- battleConfig 引用存在；
- RouteStage Scene 内存在唯一 `RouteStageSceneEntry`；
- Entry 的 HeadJunction、CombatArea、TailJunction 引用完整；
- 所有连接目标 CombatNodeConfig 引用有效；
- choiceSlot 不重复；
- 终点节点不再寻找下一个连接；
- 非终点节点至少有一个可用出口；
- RouteStage Scene 内每个 CombatNode 都有唯一的 HeadJunction、CombatArea、TailJunction；
- 每个 CombatNode 的 Head→Combat 和 Combat→Tail 路径存在且可用；
- 每条普通连接都有 source Tail、target Head 和独立路径；
- 终点节点不使用无目标终点连接；
- 同一目标 Head 可被多个连接引用，但每条连接的路径、转向点和来源 Tail 必须独立。

首版不校验或不强制：

- DAG；
- 禁止循环；
- 固定唯一 finalNode；
- 场景总览坐标；
- 跨 Scene Transform 直接引用。

## 9. 实现阶段

### Phase 1：配置和节点场景垂直切片

- 新建 CombatNodeConfig 和 RouteConnection 数据模型；
- 保留 StageConfig 作为战斗配置；
- 新建一张最小 RouteStage Scene；
- 在同一张场景中配置两个 CombatNode 的 HeadJunction、CombatArea、TailJunction；
- Battle 中创建固定环境入口接口；
- 实现路线场景一次加载、Head 对齐和场景就绪回调；
- 验证“加载节点 → 生成一波战斗 → 清场”。

### Phase 2：单连接 Travel

- 为 N0 配置一个目标 CombatNode；
- 先验证 N0.Head→N0.Combat→N0.Tail 的自动流程；
- 到达 N0.Tail 后显示一个路线选择；
- 点击后沿场景配置的 Tail→目标Head 路径移动；
- 不移动 Player，只移动/旋转 RouteStage Scene 根节点；
- 自动执行目标 Head→Combat 的内部路径；
- 目标节点生成战斗；
- 验证暂停、跳过、路线场景生命周期和重复回调。

### Phase 3：多个出口和终点

- Tail 配置多个 RouteConnection；
- 方向槽与目标节点预览接线；
- 终点节点；
- 节点固定奖励只领取一次；
- 终点结算和返回流程。

### Phase 4：重访、环路和正式表现

- 根据产品最终决定实现重访规则；
- 增加节点状态保存；
- 增加 Travel 场景或连接专属表现；
- 增加环境音、主题、剧情和可跳过演出。

## 10. 当前旧资产和旧文档处理结论

以下内容属于旧路线实验，不应作为新实现依据：

- `Node_N0.asset`、`Node_J0.asset`、`Node_N1.asset` 的旧节点模型；
- `J0_Battle.asset` 这类把 Junction 当作战斗/路线节点的配置；
- `RouteWorldRoot` 及其旧场景组件；
- `RouteWorldGraph`、`RouteWorldMotion`、`RouteWorldNodeFlow`；
- 旧版 `completionJunction` 字段；
- 旧版 `finalNode` 强制终点模型；
- 旧文档中的 `Assets/Scenes/RouteAuthoring/Stage01_Route.scene`、`Assets/Data/Route/...`、`RouteNodeAuthoring`、`RouteStageBaker` 等当前项目不存在或已不适用的引用。

旧资产可以暂时保留在项目中作为历史资料，但新运行时不得引用它们。新架构完成垂直切片并验证后，再单独清理旧脚本和旧资产。
