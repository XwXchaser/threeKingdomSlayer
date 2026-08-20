---
id: kd_a9df5837-75e3-4f6e-872f-1b0dedc64c06
injectMode: inherit
aiMaintained: inherit
---

# 路线节点非战斗玩法设计

## 1. 定位

路线玩法属于完整 Wave 之间的非战斗关卡层，不改变固定战斗场，也不修改现有 PerRow 补齐铁律。

基本循环：

```text
固定战斗节点（完整Wave）
→ 结算现有阻塞奖励
→ 顺序发放节点固定奖励
→ 显示上下左右路线卡
→ 玩家点击并锁定目标节点
→ 播放可跳过的Travel前进演出
→ 切换节点环境主题
→ 在同一固定战斗空间开始目标节点Wave
```

玩家和摄像机不会在战斗世界中永久累加位移。Travel通过玩家跑步、背景/地面反向滚动、前景掠过、镜头轻推、环境音与主题渐变等方式表现前进。这样地刺、火焰、投射物、固定阵型和现有攻击坐标仍可继续工作。

## 2. 与战斗系统的边界

每个路线节点对应一个完整 `WaveConfig`，节点内部完整复用当前战斗逻辑：

- PerRow稳定逻辑排队列；
- 全零空排占位符；
- 999节奏门；
- SpawnEntry与WaveMarch；
- 击退精确原槽回位；
- Boss、QTE、共享血量、经验升级、Boss锦囊和击杀奖励。

路线层不得接管排内战斗，也不得把Wave改成逐排激活。`WaveSpawner`只负责生成指定Wave和报告清空；路线控制器负责决定下一节点。

## 3. 路线图结构

首版由设计师手工配置有向无环图（DAG）：

- 一个固定入口；
- 一个固定首节点；
- 一个唯一终点节点；
- 允许分叉、不同路径长度和重新汇合；
- 首版禁止自环与循环；
- 所有可达路径最终必须能到达唯一终点；
- 未选择的分支不会因为一次选择而永久封死，未来若存在其他合法路径仍可进入；
- 因首版是DAG，正常流程不会重访已清场节点；“已清场节点自动跳过战斗和奖励，抵达后直接重新选路”仅作为防御规则和未来扩展边界。

路线长度允许不同，玩家自行决定实际经过的节点。只有玩家实际访问的节点参与本局战斗和奖励。

## 4. 数据概念

建议结构：

```text
RouteStageConfig
├─ startNode
├─ finalNode
└─ allNodes[]

RouteNodeDefinition
├─ encounterWave
├─ nodeType
├─ preview
├─ environmentTheme
├─ fixedRewards[]
├─ outgoingEdges[Up/Down/Left/Right]
└─ travelPresentation

RouteEdgeDefinition
├─ direction
├─ destinationNode
├─ unlockConditions[]
└─ previewOverride（可选）
```

节点和出口使用Inspector直接资产引用，不使用字符串ID、Resources查找或静态缓存。

## 5. 节点类型与预告

路线卡可展示：

- 敌群主题与代表敌人/场景图；
- 危险度（设计师手工配置）；
- 普通、精英或Boss标记；
- 固定金币奖励预告；
- 环境主题；
- 可选的路线距离或节点深度。

不显示精确敌人数、精确阵型和精确战斗数值。

## 6. 路线选择

### 6.1 时机

节点完整Wave清空后：

1. 等待现有Boss锦囊、经验升级等阻塞流程全部完成；
2. 顺序发放当前节点固定奖励；
3. 奖励队列全部完成后显示路线选择；
4. 玩家点击并锁定目标；
5. 进入Travel。

### 6.2 输入与布局

- 每个节点最多四条出口；
- 固定绑定屏幕上、下、左、右四个区域；
- 每个方向最多一条出口；
- 点击对应区域中的路线卡选择；
- 中心区域不响应路线选择；
- 即使只有一个可见出口，也必须由玩家点击确认，不能自动前进。

### 6.3 条件显隐

- 出口可预留局内与局外解锁条件扩展，但首版不配置、不求值；所有设计出口默认可见。
- 条件系统启用后，同一出口的多个条件全部按AND组合；
- 条件不满足时路线完全隐藏，不显示锁定卡或解锁要求；
- 每个非终点节点必须至��配置一条无条件保底出口，确保运行时不会无路可走；
- 条件在展示路线卡前统一求值，选中后立即锁定，Travel途中不能反悔。

具体支持的条件类型留待实现前另行定义。

## 7. 节点固定奖励

首版节点固定奖励先只支持金币，用于验证“节点清场 → 奖励队列 → 路线选择”的结算顺序与每节点仅一次领取规则。

- 每个节点可配置一项金币奖励；
- 奖励在当前节点完整清场、现有阻塞奖励结束后发放；
- 同一节点每局最多领取一次；
- 终点节点也正常发放金币奖励，再进入胜利结算；
- 具体局内道具、局外永久道具、回血、经验、UT等奖励类型仅预留扩展，不在首版设计或实现。

## 8. Travel非战斗演出

Travel是节点之间的前进演出阶段，不是BGM本身。可组合：

- 玩家原地跑步；
- 背景和地面反向滚动；
- 前景物体掠过；
- 镜头轻微推进和惯性；
- 路线名称、距离或节点标题；
- BGM、环境音、天气和主题渐变；
- 后续剧情或节点专属演出。

所有Travel与剧情/节点演出均允许跳过，也允许使用现有暂停系统暂停。

- 跳过按钮在Travel开始时显示，Travel结束或跳过完成时隐藏；
- 暂停期间Travel时序、音画播放与跳过输入必须一并冻结；恢复后从同一演出进度继续。

跳过规则：

- 跳过只缩短演出等待；
- 必须立即应用目标环境主题和最终状态；
- 不得漏发奖励、漏执行状态重置、漏触发节点进入事件；
- 同一演出完成逻辑必须幂等，正常播放结束和跳过只能完成一次。

开局流程也播放Travel：

```text
固定首节点
→ 开局Travel
→ 抵达首节点
→ 开始首个完整Wave
```

开局不展示路线选择。

## 9. 节点空间、入口出口与路口

节点视觉不是孤立擂台，而是道路网络中的一段战线：玩家从入口抵达主战场，敌人位于玩家与战场后方的路口/出口之间。清空节点Wave后，玩家通过主战场到达后方路口，再选择下一条路线。

```text
Arrival Entrance
→ Player Combat Position
→ Enemy Formation
→ Combat Arena Rear / Route Junction
→ Visible Forward / Left / Right Exits
```

### 9.1 战斗Arena

- 主战场保持当前固定战斗坐标、镜头高度、俯仰和FOV；普通Travel不改变这些参数。
- Arena后方可看见前、左、右路线的道路、门、旗帜、山道或其他地标，作为空间线索；它们必须处在敌人阵型之后，不能干扰战斗可读性。
- 后方路线不在3D场景中展示，仍可通过对应方向槽位的路线卡选择。
- 后方出口是例外配置：通常不提供为可选行走路线，仅用于明确需要折返、叙事或特殊机制的分支；首版DAG禁止循环，避免玩家利用后方路线绕圈。
- 清场后玩家先抵达路口南侧观察位，面向路口中心，因此能看到左、前、右出口；后方路线位于镜头后方。

### 9.2 纯路口节点

允许没有Wave、没有节点奖励的纯路口节点：

```text
Travel抵达纯路口
→ 可选剧情/主题/环境表现
→ 显示当前可见出口
→ 玩家选择并锁定路线
→ 下一段Travel
```

纯路口节点不进入战斗，不创建敌人，不触发节点奖励队列；它可用于分叉、汇合、剧情、门槛或空间叙事。纯路口同样必须至少保留一条无条件出口，并允许配置节点剧情。

### 9.3 出入口与方向槽

前/左/右/后是路线逻辑和UI方向槽，不等于世界坐标上的90度转弯。每条边应使用美术配置的实际出口、路径和入口：

```text
RouteEdgeVisual
├─ choiceSlot                  前 / 左 / 右 / 后
├─ sourceExitAnchor            源节点实际出口锚点
├─ corridorPath                美术配置路径或Spline
├─ targetEntranceAnchor        目标节点实际入口锚点
├─ travelPresentation
└─ arrivalVariant
```

因此逻辑“左路线”可以先左偏、再转向前方；“后路线”默认不配置，若明确配置则玩家先进入路口中心并完成180°转身后再进入过道。固定的是UI方向槽和每方向最多一条边，不是地理夹角。

### 9.4 路口选择的行军路径

```text
观察位 → 路口中心 → 选定出口 → 高速过道
```

- 选择前方：观察位直接沿前路进入过道；
- 选择左/右：先行至路口中心，玩家和镜头水平Yaw转向对应出口，再进入过道；
- 选择后方（例外）：先行至路口中心，完成180°转身，再进入后方过道；
- 普通Travel不改变镜头俯仰、高度或FOV。

### 9.5 Travel镜头与无缝切换

普通Travel只允许：

- 水平Yaw转向；
- 沿配置路径平移；
- 玩家跑步、背景/地面反向滚动、前景掠过；
- 环境主题、音频和场景道具渐变。

Boss或剧情专属演出才允许改变镜头俯仰、高度或FOV。

正常Travel按三段表现：

```text
源节点出口构图
→ 高速过道遮挡段
→ 目标节点入口构图
```

遮挡段使用烟尘、山壁、营门、树林、旗帜或城墙等近景覆盖预加载与主题替换点。跳过演出时允许直接切换到目标节点的到达界面图/入口构图，不伪装连续镜头。

## 10. 关卡空间编辑与运行时分层（重构方向）

路线关卡采用“一关一张总览编辑场景 + 固定Battle运行场”的双层结构，不把每个节点拆成独立Unity Scene，也不把Travel几何绑定到摄像机。

```text
Assets/Scenes/RouteAuthoring/Stage01_Route.scene  策划/美术总览编辑，不进Build
Assets/Scenes/Battle.scene                         唯一运行战斗场
Assets/Data/Route/Stage01_Route.asset              运行时DAG配置
Assets/Prefabs/Route/Stage01/Nodes/                节点环境Prefab
Assets/Prefabs/Route/Stage01/Travel/               边Travel表现Prefab
```

### 10.1 总览编辑场景

`Stage01_Route.scene` 中完整摆放该关的节点、入口、出口、路口、过道、分叉、汇合和主题预览，供策划直接调整空间关系、路线节奏、节点难度和美术衔接：

```text
Stage01_RouteRoot
├─ Node_N0_Plain
│  ├─ CombatAnchor
│  ├─ ArrivalEntrance
│  ├─ JunctionObservationPoint
│  ├─ Exit_Forward / Exit_Left / Exit_Right
│  ├─ CombatArenaPreview
│  └─ JunctionPreview
├─ Node_N1_Ambush
├─ Edge_N0_Left_N1
│  ├─ SourceExit
│  ├─ TravelPath
│  ├─ TravelVisual
│  └─ TargetEntrance
└─ ...
```

该场景仅为编辑源，不加入Build Settings；其完整空间不会在运行时常驻。

### 10.2 Authoring与Baker

- 每个节点挂 `RouteNodeAuthoring`，直接引用对应 `RouteNodeDefinition`；
- 每条边挂 `RouteEdgeAuthoring`，直接引用源节点、目标节点、方向槽、出口、入口、Travel路径与表现Prefab；
- `RouteStageBaker` 从总览场景同步生成/校验 `RouteStageConfig`；
- Baker必须校验DAG、唯一终点、可达性、每方向唯一出口、非终点保底出口、节点Wave/金币/危险度及节点和Travel引用完整。

运行时仅读取烘焙后的配置和Prefab引用，不加载总览编辑场景。

### 10.3 正确的Travel运行时空间

```text
Battle.scene
├─ MainCamera
├─ CameraFeedbackChild
├─ TravelCameraRig
├─ RuntimeNodeRoot
├─ RuntimeTravelRoot
└─ RuntimeTargetNodeRoot
```

Travel开始后：

1. 清理战斗特效并锁输入，执行统一过渡重置；
2. 当前节点切到路口构图；
3. 玩家抵达观察位；前方直行，左右经中心Yaw转向，后方仅例外地180°转身；
4. `RuntimeTravelRoot` 实例化该边的 `TravelPresentationPrefab`；
5. 相机沿该边 `TravelPath` 移动，保持战斗相机高度、俯仰与FOV；
6. Travel过道的墙体、遮挡、旗帜、火把与出口是实际Prefab几何，不能作为相机子物体或屏幕覆盖背景；
7. 中段预加载目标节点环境；
8. 入口遮挡完成后卸载当前节点和Travel，恢复固定战斗机位并生成目标Wave；
9. 跳过直接应用目标入口构图和节点状态，不使用黑屏。

运行时最多保留“当前节点 + 当前Travel边 + 目标节点预加载”三套表现；未选择分支不实例化。包体增加只来自实际资源，不来自总览编辑场景。

### 10.4 当前白盒的处理

现有 `RouteWhiteboxPresentation` 的相机绑定/屏幕覆盖走廊仅为已废弃的流程验证原型，不能作为正式Travel架构。路线DAG、节点Wave、金币结算和输入锁定可保留；Travel视觉将在总览场景、Authoring组件、Baker与运行时根节点完成后重做。

## 11. 节点环境主题

不同节点可以切换表现，但固定战斗坐标不变。

建议主题资产包含：

```text
NodeEnvironmentTheme
├─ background
├─ ground
├─ foreground
├─ ambientColor
├─ BGM
├─ ambientSFX
├─ optionalSceneProps
└─ travelPresentation
```

静态布局优先使用Prefab或直接资产引用。Travel结束后切换目标主题，再生成目标Wave。

## 12. 跨节点状态

### 10.1 继承

- 玩家当前生命值；
- UT技能及其能量/状态；
- 已获得的三选一Build及等级；
- 局内可持有道具与本局资源；
- 已领取的节点奖励和已访问路线记录。

### 10.2 Travel开始时统一重置

选择路线并进入Travel时，发送一次统一“节点过渡开始”事件，由各系统响应：

- Combo清零；
- 当前限时被动实例清除，不继承剩余时间；
- 三选一主动技能CD刷新完成；
- 周期型被动计时刷新为可触发状态；
- Build本身及等级保留；
- 场上地刺、火焰、旋风、攻击波、投射物、伤害数字和临时碰撞体等清理；
- 当前攻击、蓄力、QTE和敌人攻击流程必须结束或安全取消；
- 禁止战斗输入和敌人AI。

重置只在Travel开始时执行一次。演出跳过不得重复执行。

## 13. 节点状态机

```text
StageEntry
→ OpeningTravel
→ NodeEntering
→ NodeCombat
→ NodeCombatCleared
→ ExistingBlockingRewards
→ NodeRewardQueue
→ RouteChoice
→ TravelStarting（统一重置）
→ TravelPlaying / Skipped
→ NodeEntering
...
→ FinalNodeRewardQueue
→ StageVictory
```

已清场节点防御流程：

```text
Travel抵达已清场节点
→ 不生成Wave
→ 不重复发奖励
→ 直接重新计算并展示该节点当前可见出口
```

## 14. 失败与存档

- 玩家失败后整局重开；
- 路线选择、已访问节点、局内Build和局内奖励全部清空；
- 不提供节点检查点；
- 已经在节点领取并写入存档的局外永久道具不回收；
- 最终通关后执行现有关卡胜利与局内资源结算。

## 15. 运行时职责建议

### RouteProgressionController

负责：

- 当前节点与已清场节点集合；
- 节点奖励领取状态；
- 路线条件求值；
- 方向卡选择和锁定；
- Travel状态机与跳过；
- 环境主题切换；
- 固定首节点和唯一终点；
- 将指定Wave交给WaveSpawner。

### WaveSpawner

只负责：

- 生成外部指定的一个Wave；
- 管理该Wave敌人和清空回调；
- 不在路线模式下自行决定下一个Wave。

### StageController

- 旧线性关卡继续按`StageConfig.waves`推进；
- 路线模式下把节点推进权交给RouteProgressionController；
- 保留胜利、失败和最终结算职责。

### NodeTransitionReset

统一广播Travel开始事件。Combo、主动技能、被动技能、场上效果和输入系统各自清理自身状态，避免路线控制器直接了解所有战斗系统细节。

## 16. 编辑器与配置校验

首版必须验证：

- 起点、固定首节点和唯一终点存在；
- 起点到所有参与节点可达；
- 所有可达节点都能到达唯一终点；
- 禁止自环和任何有向循环；
- 非终点节点至少一个出口；
- 每方向最多一条出口；
- 非终点节点至少一条无条件保底出口；
- 终点不得配置出口；
- 节点Wave、预告、主题和首版金币奖励引用/数值完整；
- 危险度必须由设计师填写；
- 路线条件系统首版不启用，若资产意外配置未支持条件应报错；

## 17. 分期实现建议

### Phase 1：流程骨架（Authoring重构进行中）

- 已新增 `StageProgressionMode.Route`、`RouteStageConfig`、`RouteNodeDefinition`、固定方向出口和DAG运行时校验；
- 已新增 `RouteProgressionController`：固定首节点Travel、外部指定Wave、节点金币奖励、路线选择状态和终点胜利状态；
- 已让 `WaveSpawner` 支持外部节点Wave，路线模式不再自动推进线性下一Wave；
- 已创建 `Assets/Scenes/RouteAuthoring/Stage01_Route.scene`：8个节点、9条边的可视化总览编辑场景；每个节点已配置Arena、入口、后方路口、前/左/右出口白盒与难度/金币标签，边已用Travel段和方向标签连接；
- 已创建 `RouteNodeAuthoring`、`RouteEdgeAuthoring`、`RouteStageAuthoring` 和 `RouteStageBaker`，并成功烘焙到 `Assets/RouteData/Stage01/Stage01_Route.asset`；
- `Battle.scene` 已创建 `RouteRuntime/RuntimeNodeRoot`、`RuntimeTravelRoot`、`RuntimeTargetNodeRoot`，路线测试关卡和控制器已改引用新烘焙资产；
- 旧 `RouteWhiteboxPresentation` 相机绑定/屏幕覆盖原型已停用，旧 `Assets/Resources/RouteWhitebox` 数据仅保留待后续清理；
- 下一步：节点/边环境Prefab、Runtime根节点实例化、TravelCameraRig沿烘焙路径播放、正式路线UI与统一过渡重置。

### Phase 2：主题与正式表现

- 节点环境主题；
- 敌群和奖励预告卡；
- 背景/地面滚动；
- BGM与环境音过渡；
- 精英、Boss及特殊路线表现；
- 正式剧情演出和通用跳过管线。

### Phase 3：路线图工具

- 节点拖拽和方向连线；
- DAG、可达性、唯一终点和保底路线验证；
- 路线长度、奖励、危险度统计；
- 调试跳转和路线模拟。

## 19. 新版空间架构与Stage01迁移方案

### 19.1 权威空间源

路线关卡使用“一张总览编辑场景作为唯一空间源”的架构。节点和边都必须是总览场景中的真实GameObject，策划/美术直接在场景中查看、移动、旋转、摆放环境Prefab和Travel素材；ScriptableObject只保存节点/边的逻辑配置及Baker生成的运行时空间数据，不作为手工空间编辑入口。

```text
Assets/Scenes/RouteAuthoring/Stage01_Route.scene
└─ Stage01_RouteRoot
   ├─ Nodes
   │  ├─ Node_N0_Plain
   │  │  ├─ Environment
   │  │  ├─ CombatAnchor
   │  │  ├─ ArrivalEntrance
   │  │  ├─ JunctionObservation
   │  │  ├─ Exit_Forward
   │  │  ├─ Exit_Left
   │  │  └─ Exit_Right
   │  ├─ Node_N1_Ambush
   │  └─ ...
   └─ Edges
      ├─ Edge_N0_Left_N1
      │  ├─ SourceExit
      │  ├─ JunctionTurnPoint
      │  ├─ TravelPath
      │  ├─ TravelVisual
      │  └─ TargetEntrance
      └─ ...
```

`RouteNodeAuthoring`对应一个Node GameObject，直接引用`RouteNodeDefinition`；`RouteEdgeAuthoring`对应一个Edge GameObject，直接引用源节点、目标节点及空间锚点。节点与边不能只存在于资产或仅通过字符串/坐标隐式关联。

### 19.2 坐标规则

- 总览场景以`Stage01_RouteRoot`为统一空间根，所有节点和边均在其下编辑。
- Node/Edge的场景Transform是美术编辑坐标；Baker读取锚点的世界Transform，再转换为相对于关卡根的局部坐标保存。
- 节点环境Prefab内部使用自身根节点的局部坐标；运行时节点环境不会把总览场景坐标直接当作Battle世界坐标。
- Battle运行时保持固定战斗坐标。当前节点环境、Travel边和目标节点分别对齐到Battle运行时根节点，由运行时根据烘焙的入口/出口/路径数据完成表现转换。
- Edge路径必须明确首点、末点、源出口、转向点和目标入口；禁止只保存一组无法解释的世界坐标。
- Baker校验节点/边引用完整、边首尾与源/目标锚点误差、锚点方向、异常缩放、空路径和空间断链；校验失败时不得覆盖上一份有效运行时配置。

### 19.3 运行时空间职责

运行时最多保留当前Node、当前Edge Travel和目标Node三套表现。Travel执行“源节点观察位→路口中心→水平Yaw转向→TravelPath→遮挡段→目标入口构图→固定战斗机位”的流程。Travel Visual必须是实际Prefab几何，不使用摄像机子物体或屏幕覆盖背景伪造过道。

### 19.4 Stage01迁移

现有Stage01的8个节点和9条边、Wave、金币和路线逻辑资产保留；现有白盒Node/Travel Prefab作为Node/Edge的可替换表现素材重新挂接到总览场景。Stage01总览场景最终必须能直接看到完整节点网络、每条边的路径和入口/出口连接，并由Baker重新生成运行时路线配置。实施顺序为：先完成Authoring组件与Baker空间数据，再完成N0→N1单链路，最后恢复8节点/9条边完整运行。
