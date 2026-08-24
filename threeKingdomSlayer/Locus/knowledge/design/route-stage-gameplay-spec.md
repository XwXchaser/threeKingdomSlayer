---
id: kd_7b0b3fe1-8bf7-4f26-ab3c-5085f77cc015
injectMode: inherit
aiEditMode: inherit
---

# 场景化路线关卡设计

## 1. 设计目标

路线关卡的核心玩法是“节点到节点的移动”。该场景化关卡结构是关卡系统的上位结构，可以承载：

- 纯线性、只经过一次的关卡；
- 在 Tail 进行分支选择的路线关卡；
- 后续允许重访、条件分支或剧情分支的关卡。

纯线性关卡和分支路线关卡不是两套平等的关卡架构。纯线性关卡只是 `outgoingConnections[]` 每次只有一个可用目标的场景化关卡特例。玩家在固定战斗空间中完成一个 CombatNode 的战斗，再从该节点 Tail 进入下一个节点；系统通过移动和旋转 RouteStageRoot，模拟玩家从当前节点移动到目标节点。

当前文档的实现优先级：

1. 节点内部 `Head→Combat→Tail` 的连续移动；
2. Tail 到目标 Head 的旋转和移动；
3. 目标节点 Head→Combat 的自动进入；
4. 战斗完成后继续移动到 Tail 并开放下一次选择；
5. 终点节点抵达和终点演出。

条件系统、完整存档系统、强退恢复、存档版本校验等属于后置边界功能，不改变核心节点移动闭环。

核心原则：

- 一关一张路线场景；
- 路线场景内始终存在该关卡的全部 CombatNode；
- Battle 场景只提供固定战斗运行空间；
- Player Transform 不移动；
- Enemy 和战斗坐标不移动；
- 普通路线移动只改变 RouteStageRoot 的位移和旋转；
- 路线选择只发生在 Tail；
- Head→Combat、Combat→Tail 和 Tail→目标Head 都是自动移动阶段；
- 目标方向不一致时，先旋转再移动；
- 正常完成和跳过只能推进一次阶段。

## 2. 场景结构

### 2.1 Battle 场景

Battle 场景包含：

- Player；
- Enemy 和敌人管理系统；
- Wave 生成和战斗管理系统；
- Battle Camera；
- Battle HUD；
- 固定战斗区域；
- 固定的路线场景挂点和阶段目标点；
- 路线关卡流程控制接口。

Battle 场景不包含路线节点、路线道路、Head、CombatArea、Tail 或路线场景环境。

### 2.2 RouteStage 场景

每个路线关卡对应一张 RouteStage 场景：

```text
RouteStageRoot
├─ CombatNode_A
│  ├─ HeadJunction
│  ├─ CombatArea
│  └─ TailJunction
├─ CombatNode_B
│  ├─ HeadJunction
│  ├─ CombatArea
│  └─ TailJunction
├─ Connection_A_B
├─ Connection_B_C
└─ ...
```

所有 CombatNode 和 Connection 都位于同一张 RouteStage 场景中，并在场景中保持设计师配置的初始空间关系。

### 2.3 CombatNode

每个 CombatNode 包含三个连续阶段：

- `HeadJunction`：节点进入位置；
- `CombatArea`：节点战斗位置；
- `TailJunction`：节点离开位置和路线选择位置。

三个点位的位置、旋转和相对距离由设计师自由设置，不使用固定间距，也不要求三点处于同一直线上。

三点都属于同一个 CombatNode，并随 RouteStageRoot 一起移动和旋转。

## 3. 配置资产

### 3.1 RouteStageConfig

RouteStageConfig 描述整关路线：

```text
RouteStageConfig
├─ stageId
├─ stageName
├─ routeScene
├─ startNode
├─ combatNodes[]
├─ clearReward
└─ stagePresentation
```

字段含义：

- `stageId`：关卡标识；
- `stageName`：关卡名称；
- `routeScene`：该关卡唯一的 RouteStage 场景；
- `startNode`：进入关卡后首先进入的 CombatNodeConfig；
- `combatNodes[]`：该关卡使用的全部 CombatNodeConfig；
- `clearReward`：整关完成奖励；
- `stagePresentation`：整关级别的演出配置。

`startNode` 是首个节点的唯一来源，不依据场景顺序、对象名称、空间位置或数组顺序决定。`startNode` 必须属于 `combatNodes[]`，并且 RouteStage Scene 必须存在一个 `CombatNodeSceneEntry` 直接引用它。

RouteStageConfig 的 `routeScene` 是唯一需要加载的路线场景。所有 CombatNode 和连接表现都在该场景中。

### 3.2 CombatNodeConfig

CombatNodeConfig 描述一个 CombatNode 的逻辑内容：

```text
CombatNodeConfig
├─ nodeId
├─ displayName
├─ sceneBinding
├─ battleEntries[]
├─ isFinalNode
├─ savePoint
├─ preview
└─ outgoingConnections[]
```

字段含义：

- `nodeId`：节点稳定标识；
- `displayName`：节点名称；
- `sceneBinding`：不作为 ScriptableObject 对场景对象的反向引用。由 RouteStage 场景内的 `CombatNodeSceneEntry` 直接引用本配置，运行时通过配置对象引用完成绑定；
- `battleEntries[]`：进入该节点后按顺序处理的战斗列表；
- `isFinalNode`：该节点是否为终点节点。进入终点节点后仍执行 Head→Combat、BattleEntry 和 Combat→Tail，但到达 Tail 后不显示路线选择，直接执行终点演出并结算；
- `savePoint`：该节点是否记录为本关存档点；
- `preview`：路线选择时使用的预览信息；
- `outgoingConnections[]`：从该节点 Tail 出发的连接列表。

CombatNodeConfig 不包含节点奖励字段。节点完成后的金币、经验、道具等收益均来自实际执行的战斗内容和敌人配置；不存在独立的“节点奖励”阶段。

### 3.3 BattleEntry

一个 CombatNode 可以拥有多个有序 BattleEntry：

```text
BattleEntry
├─ battleConfig
└─ condition
```

字段含义：

- `battleConfig`：该场战斗使用的战斗内容配置；战斗中的金币、经验、道具等收益由该配置关联的敌人和现有战斗奖励系统产生；
- `condition`：该场战斗是否适用于当前局内状态。条件系统可读取局内状态或局外存档/成长状态，具体条件类型在条件系统接入时定义。

BattleEntry 按列表顺序处理。条件不满足的条目本次进入节点时跳过，不启动战斗；本次跳过不等于永久完成，未来是否再次执行由未来进入节点时的条件结果决定。

### 3.4 RouteConnection

RouteConnection 描述从一个 CombatNode Tail 到另一个 CombatNode Head 的普通路线：

```text
RouteConnection
├─ choiceSlot
├─ targetNode
├─ sceneBinding
└─ presentation
```

普通连接：

- `targetNode` 必须存在；
- `targetNode` 可以是普通节点或 `isFinalNode = true` 的终点节点；
- `sceneBinding` 指向当前 Tail、目标 Head 以及该连接的路径数据。

路线选择只显示当前 Tail 的普通连接。终点不是一个需要玩家点击的“终点连接”，而是一个被普通连接抵达的 `isFinalNode`。进入终点节点后不再显示路线选择，完成该节点的战斗流程和终点演出后直接结算。
## 4. 场景路径配置

所有移动均由 RouteStageRoot 完成。路径的三个类别必须独立配置：

```text
NodeInternalArrival: Head → Combat
NodeInternalExit:    Combat → Tail
NodeConnection:      Tail → TargetHead
```

每个阶段都必须明确：

```text
StageMotion
├─ sourceAnchor
├─ targetAnchor
├─ rotationPivot
├─ rotationOrder = RotateThenTranslate
├─ translationPath
├─ finalPose
└─ completionCondition
```

`sourceAnchor` 和 `targetAnchor` 是该阶段实际使用的场景点位。`finalPose` 是路线场景根节点完成阶段后的最终姿态，不是 Player 或 Camera 的姿态。

### 4.1 节点内部路径

每个 CombatNode 配置两段内部路径：

```text
Head → CombatArea
CombatArea → Tail
```

- Head→Combat：进入节点后自动移动，CombatArea 到达 Battle 的 `combatTarget` 后开始战斗；
- Combat→Tail：战斗序列结束后自动移动，Tail 到达 Battle 的 `tailTarget` 后开放路线选择。

三点位置、旋转和相对距离由设计师自由设置，不使用固定间距，也不要求三点处于同一直线。

### 4.2 节点之间路径

每条普通 RouteConnection 独立配置：

```text
Tail → TargetHead
```

连接移动顺序固定为：

```text
当前 Tail
→ 按目标 Head 的方向和连接 TurnPivot 完成水平旋转
→ 沿该连接的 translationPath 平移
→ 抵达目标 Head
→ 自动执行目标节点 Head→Combat
```

连接的移动距离、旋转支点、转向位置和路径形状全部来自场景实际 Transform。代码不提供固定前进距离、固定旋转角度或额外位移。

### 4.3 路线场景根节点的运行时姿态

RouteStageRoot 是路线移动的唯一对象。所有 Head、CombatArea、Tail、路径点和连接表现都作为其子层级参与同一刚体变换。

路线移动执行器必须：

- 在阶段开始时读取该阶段的局部路径数据和 RouteStageRoot 起始 Pose；
- 计算该阶段的目标 RouteStageRoot Pose；
- 先执行水平旋转，再执行平移；
- 旋转时保持场景中的 rotationPivot 世界位置不变；
- 阶段执行期间只由一个执行器写入 RouteStageRoot；
- 正常完成和跳过只能提交一次最终 Pose。

### 4.4 路径点的坐标契约

路径点、转向点和阶段锚点是设计时空间数据，保存为相对于所属路径/节点/连接根的局部坐标。它们随 RouteStageRoot 一起移动，用于路线场景的空间配置、连接关系校验和阶段表现；它们不是运行时移动过程中独立移动的世界坐标源。

路线移动的最终正确性只由 source/target 锚点和 RouteStageRoot 的目标对齐 Pose 决定。路径起终点的 Delta 不得作为额外位移叠加，否则会造成阶段先偏离目标、再返回目标的错误表现。当前 V2 的旋转支点保持和最终锚点校正属于既定架构，不是待修复缺口。

### 4.5 当前路线移动实现边界

当前 V2 采用“阶段开始读取配置 + RouteStageRoot 单一执行器移动 + 目标锚点最终校正”的实现边界：

1. 阶段开始时读取当前节点/连接的锚点、路径和转向配置；
2. 先按目标姿态完成旋转，并保持配置的 rotationPivot 世界位置；
3. 再将 RouteStageRoot 移动到目标锚点计算出的最终 Pose；
4. 阶段结束时只提交一次最终 Pose，并由目标锚点校正结果。

路线移动不修改 Player、Enemy 或 Battle Camera。路径点是否承担独立的逐点演出，不改变上述最终 Pose 契约，也不属于当前三个路线 BUG 的修复范围。
多个源节点可以指向同一个目标节点 Head：
Tail_B ─┼─→ TargetHead
Tail_C ─┘
```

规则：

- 目标 Head 是同一个场景点位；
- 目标 Head 的最终位置和朝向由目标 CombatNode 唯一确定；
- 每条入边拥有自己的 source Tail、路径点、转向点和旋转过程；
- 不同 Tail 可以使用不同的旋转角度、旋转方向和移动路径；
- 三条路径最终都抵达同一个目标 Head 姿态；
- 抵达目标 Head 后，统一执行目标节点的 Head→Combat 内部路径。

因此，同一个目标 Head 不需要保存三套姿态，也不需要被旋转三次。不同的是入边的 Travel 过程，目标 Head 的最终姿态保持一致。

如果设计需要同一节点拥有不同的入口姿态，必须配置多个入口变体，而不是让一个 Head 同时承担互相冲突的姿态。

### 汇入结构的测试方式

正式流程从 `RouteStageConfig.startNode` 开始，只能沿当前节点实际配置的 outgoingConnections 前进。如果起点 A 没有 A→C、A→D，不能在同一条正常流程中直接从 C/D 测试 C→B、D→B。

测试应提供独立测试入口：临时将 `startNode` 设置为 C 或 D，或使用编辑器/调试工具选择任意 CombatNode 作为测试起点，并把路线场景根节点初始化到该节点 Head。测试完成后恢复正式 `startNode`，测试入口不参与正式运行流程。

目标节点 B 可以是普通节点，也可以是 `isFinalNode` 终点节点。汇入结构与终点属性相互独立。


Battle 提供固定的阶段目标：

```text
BattleStageTargets
├─ initialHeadTarget
├─ combatTarget
└─ tailTarget
```

用途：

- 首节点 Head 抵达 `initialHeadTarget`；
- CombatArea 抵达 `combatTarget` 后开始战斗；
- CombatArea 完成战斗后移动到当前节点 Tail，Tail 抵达 `tailTarget` 后显示路线选择。

这些目标属于 Battle 固定运行空间。路线场景通过 RouteStageRoot 的位移和旋转与它们对齐，Player、Enemy 和 Battle Camera 不移动。

## 7. 完整局内流程

### 7.1 进入关卡

```text
选择 RouteStageConfig
→ 加载 Battle 场景
→ 加载 RouteStageConfig.routeScene
→ 获取 RouteStageRoot 和场景绑定
→ 读取 RouteStageConfig.startNode
→ 将 startNode.Head 移动到 initialHeadTarget
→ 自动沿 startNode.Head→Combat 路径移动
→ 将 startNode.CombatArea 对齐 combatTarget
→ 自动开始 startNode 的 BattleEntry 序列
```

进入关卡不显示路线选择。RouteStage Scene 的加载完成、场景绑定校验完成、起始节点定位完成和首节点进入演出完成，才允许开始首个 BattleEntry。

### 7.2 CombatNode 内部流程

```text
进入 Head
→ Head→Combat 自动移动
→ CombatArea 对齐 combatTarget
→ 按顺序检查 battleEntries
→ 条件满足的条目进入战斗
→ 战斗完成后由现有敌人/战斗系统发放收益
→ 条件不满足的条目本次跳过
→ 检查下一条条目
→ 所有条目本次处理完成
→ Combat→Tail 自动移动
→ Tail 抵达 tailTarget
→ 显示路线选择
```

如果当前节点没有任何 battleEntry，或全部条目本次均因条件不满足而跳过，则仍然执行 Combat→Tail；此时不产生战斗收益。

### 7.3 Tail 路线选择

```text
到达 Tail
→ 生成当前节点可用连接列表
→ 显示路线选择
→ 玩家点击一个连接
→ 立即锁定连接
```

即使只有一个可用连接，也必须由玩家点击确认。

Head→Combat 和 Combat→Tail 都不需要玩家选择。当前 V2 的路线选择界面属于既有功能验证界面；是否后续替换为正式 Canvas 表现属于独立 UI 工作，不是当前路线流程 BUG，也不改变 Tail 才能选择、点击后立即锁定的规则。

### 7.4 普通连接

```text
到达当前 Tail
→ 玩家选择普通连接
→ 锁定连接
→ 清理战斗临时状态
→ 锁定战斗输入
→ 按目标 Head 方向和连接 TurnPivot 完成水平旋转
→ 沿连接 translationPath 移动到目标 Head
→ 自动执行目标节点 Head→Combat
→ 目标 CombatArea 到达 combatTarget
→ 开始目标节点 BattleEntry 序列
```

RouteStageRoot 是唯一被路线移动逻辑修改的场景根节点。

### 7.5 进入终点节点

```text
到达当前 Tail
→ 玩家选择通往终点节点的普通连接
→ 沿连接路径抵达终点节点 Head
→ 自动执行终点节点 Head→Combat
→ 执行终点节点 BattleEntry
→ 终点节点 BattleEntry 全部本次处理完成
→ 自动执行终点节点 Combat→Tail
→ 到达终点节点 Tail
→ 不显示路线选择
→ 执行终点演出
→ 演出完成或跳过
→ 发放整关奖励
→ 进入胜利结算
```

终点节点是普通连接的目标，不是终点连接本身。终点节点不创建下一个 CombatNode，也不执行终点节点之后的 Tail→目标Head 移动。终点节点的 Tail 可以设置在与 CombatArea 相同的位置，避免额外离场位移。

### 8. 战斗和奖励规则

## 8. 战斗和奖励规则

### 8.1 战斗顺序

BattleEntry 按配置顺序处理。一个条目只有两种结果：本次执行战斗，或因条件不满足而本次跳过。

### 8.2 已完成 BattleEntry

本局内已完成的 BattleEntry 不重复挑战。条件不满足造成的 `SkippedThisVisit` 不写成已完成，未来再次进入节点时重新求值。

### 8.3 战斗收益

BattleEntry 不配置独立奖励字段。实际战斗完成后的金币、经验、道具等收益由 battleConfig、敌人配置和现有战斗奖励系统产生。

### 8.4 节点完成

节点不存在独立节点奖励。所有 BattleEntry 本次处理完成后，路线流程直接开始 Combat→Tail 移动。

### 8.5 整关奖励

整关奖励只在终点节点到达 Tail 后，终点演出完成或被跳过时发放。

## 9. 节点状态

运行时至少记录：

```text
NodeRuntimeState
├─ visited
├─ battleEntryStates[]
├─ currentPhase
└─ atTail
```

每个 BattleEntry 的状态至少区分：

```text
BattleEntryRuntimeState
├─ NotProcessed
├─ Completed
├─ SkippedThisVisit
└─ Failed
```

- `Completed`：本局已经完成，后续重访不重复挑战；
- `SkippedThisVisit`：本次进入因条件不满足而跳过，不写成永久完成；
- `Failed`：战斗失败时的中断状态，整局失败或恢复存档时按对应规则重建；
- 条件在每次进入节点、按列表顺序处理到该条目时重新求值。

节点运行状态还必须记录当前路线阶段的 owner/generation，防止旧的战斗清空回调、奖励回调或移动完成回调推进新阶段。

`currentPhase` 至少包括：

```text
EnteringHead
MovingToCombat
Combat
MovingToTail
AtTail
ChoosingRoute
TravelingToHead
Completed
```

节点重访时：

- 已完成的 BattleEntry 不重复挑战；
- 未完成的 BattleEntry 按当前条件重新判断；
- 本次跳过的 BattleEntry 不写成永久完成，未来进入节点时重新判断；
- 场景不会因为节点切换而重新加载。

## 10. 暂停、存档点、失败和重开

### 10.1 暂停

暂停必须同时冻结：

- 路线场景移动和旋转；
- 节点内部自动移动；
- 路线演出计时；
- 跳过输入；
- 当前战斗流程。

恢复后从相同阶段继续。暂停不改变当前节点、BattleEntry、奖励发放或路线选择状态。

### 10.2 跳过

跳过只缩短当前移动/演出阶段，不跳过必须执行的战斗和条件求值。跳过后必须：

- 应用当前阶段最终 Pose；
- 完成当前节点或连接的状态转换；
- 不重复发放任何战斗收益；
- 不把 `SkippedThisVisit` 写成永久完成；
- 不跳过必须执行的战斗；
- 只调用一次完成回调。

### 10.3 存档点

部分 CombatNode 可配置为 `savePoint`。玩家抵达该节点 Head 后立即记录本关最近存档点，然后才执行该节点 Head→Combat 和 BattleEntry。存档点不是 Tail 时刻；若玩家在该节点战斗中或之后失败，恢复时从该节点 Head 重新进入，并重新执行该节点的 Head→Combat 与尚未永久完成的 BattleEntry。当前节点在本次进入时执行过的战斗不能因为 Head 存档而标记为已完成。

存档点至少需要保存：

- 当前 RouteStageConfig；
- 最近存档节点；
- 本关路线进度；
- 当前关卡内每个 CombatNode 的挑战状态；
- 每个节点内各 BattleEntry 的挑战完成状态；
- 已到达过的节点和已触发的剧情/剧情选项状态；
- 玩家生命值；
- 玩家当前持有的技能及其等级/状态；
- UT 技能充能值；
- 条件系统需要的其他局内状态快照。

不保存以下短时战斗状态：

- 连击数；
- 当前攻击、蓄力和 QTE 状态；
- 临时战斗 Buff；
- 场上敌人、投射物和临时效果；
- 其他只属于当前战斗过程的短时状态。

### 10.4 失败和重开

普通关卡重开与存档点恢复是两种不同流程。

#### 普通关卡重开

```text
选择重新开始关卡
→ 清空整局路线状态
→ 清空所有节点挑战状态
→ 清空所有 BattleEntry 完成状态
→ 清空已到达节点和剧情/剧情选项状态
→ 重置玩家本局状态
→ RouteStageRoot 恢复初始编辑姿态
→ 从 RouteStageConfig.startNode 的 Head 重新开始
```

普通重开会刷新整关状态，不从任何中间节点继续。

#### 失败后的存档点恢复

```text
玩家失败
→ 执行运行时 ResetAll / 清理战斗和路线临时状态
→ 载入最近存档点保存的整关节点挑战状态
→ 载入该存档点保存的玩家状态
→ RouteStageRoot 恢复初始编辑姿态
→ 从存档点节点的 Head 重新开始
→ 根据已保存的节点/BattleEntry 挑战状态决定本次执行内容
```

这里的起点是**最近存档点对应的原节点**，不是新的后续节点，也不是 `RouteStageConfig.startNode`。恢复时该存档节点被视为重新进入：重新执行该节点的 Head→Combat，并按恢复后的状态重新处理本次应执行的 BattleEntry。恢复前执行 `ResetAll` 是预期的运行时清理步骤；它用于清除当前运行态，之后再载入快照中的持久状态。

从存档节点 Head 开始时：

- 存档点之前已完成的节点和 BattleEntry 按保存状态处理；
- 存档节点自身从 Head 重新进入，重新执行 Head→Combat；该节点本次进入的 BattleEntry 不因存档而跳过，按当前条件重新判断并执行；
- 存档点之后未完成的节点按未完成状态处理；
- Head→Combat、Combat→Tail 和 Tail 路线流程重新执行；
- 不恢复连击、当前攻击、蓄力、QTE、临时 Buff、敌人、投射物和其他临场效果。

强制退出游戏不等同于失败恢复。当前 MainMenu 的“继续游戏”语义是：从最后未完成的路线关卡重新开始，并从该关卡 `RouteStageConfig.startNode` 的 Head 进入；不会读取路线存档点快照，也不会从最近存档节点继续。

### 10.5 终点演出

终点演出允许跳过。跳过时直接应用终点演出的最终状态，然后发放整关奖励并进入胜利结算；跳过不得重复发放奖励，也不得回到路线选择。

RouteStage 场景需要提供编辑器工具，用于验证和预览：

- 显示每个节点的 Head、CombatArea、Tail；
- 显示 Head→Combat 路径；
- 显示 Combat→Tail 路径；
- 显示每条 Tail→TargetHead 连接路径；
- 显示路径方向、转向点和最终姿态；
- 预览路线场景根节点移动和旋转；
- 预览多个 Tail 汇入同一 Head 的不同 Travel 过程；
- 检查路径首尾是否连接正确点位；
- 检查路径是否存在空点或断点；
- 检查普通连接是否存在目标 Head；
- 检查终点连接是否没有目标节点；
- 检查同一目标 Head 的最终姿态是否一致；
- 检查节点内部三阶段路径是否完整；
- 检查 `startNode` 是否属于 `combatNodes[]`；
- 检查 `RouteStageConfig.routeScene` 与当前 RouteStage Scene 是否一致；
- 检查每个 CombatNodeConfig 是否恰好有一个场景绑定；
- 检查每个场景绑定是否恰好对应一个 CombatNodeConfig；
- 检查每个普通连接是否恰好有一条逻辑连接和一条场景绑定；
- 检查 `choiceSlot` 在同一 Tail 下不重复；
- 检查所有必需组件、路径、锚点和引用完整。

预览工具只能操作路线场景中的测试表现，不移动 Battle 场景中的 Player、Enemy 或 Battle Camera。

## 12. 关卡起始节点

关卡起始节点由 `RouteStageConfig.startNode` 直接引用确定。

```text
RouteStageConfig
├─ routeScene
├─ startNode  ← 唯一入口节点
└─ combatNodes[]
```

进入关卡时：

1. 读取 `RouteStageConfig.startNode`；
2. 在已加载的 RouteStage Scene 场景绑定中找到该节点的 Head；
3. 将路线场景根节点设置为起始运行姿态，使 startNode.Head 到达初始 Head 目标；
4. 自动执行 startNode.Head→Combat；
5. 开始 startNode 的 BattleEntry 序列。

场景中 CombatNode 的排列顺序、对象名称、空间位置和数组顺序都不能决定起始节点。`startNode` 必须属于 `combatNodes[]`，并且必须存在对应的场景绑定。

## 11. 条件系统示例

条件可以读取局外状态，也可以读取当前局内状态。条件在 BattleEntry 或路线连接被处理时求值。

### 局外状态示例

```text
玩家持有某道具数量 ≥ 3
玩家已解锁某个永久能力
玩家已完成某个章节
玩家拥有某个局外标记
```

例如：玩家局外持有至少 3 个“通行令”，某个 BattleEntry 才会在本次进入节点时执行；否则本次跳过。

### 局内状态示例

```text
某个 CombatNode 已完成
某个 CombatNode 曾经到达过
某段 BattleEntry 已完成
玩家当前持有某个技能
玩家当前铜钱/资源达到阈值
某个剧情事件已触发
某个剧情选项已选择
```

例如：玩家先到达节点 A，触发“发现敌军”的状态；之后从另一条路线进入节点 B 时，B 的 BattleEntry 条件读取该状态，决定本次是否出现伏击战斗或剧情分支。

条件系统只负责返回是否满足，不负责启动战斗、移动场景或发放奖励。条件不满足时，当前 BattleEntry 本次跳过；未来再次进入节点时重新求值。

## 12. RouteStage 场景中的逻辑 GameObject

RouteStage Scene 除了美术构件，还应包含以下逻辑对象：

```text
RouteStageRoot
├─ RouteStageSceneEntry
├─ CombatNodes
│  ├─ CombatNodeSceneEntry_A
│  ├─ CombatNodeSceneEntry_B
│  └─ ...
├─ Connections
│  ├─ RouteConnectionSceneBinding_A_B
│  └─ ...
└─ RouteStageDebugGizmos（仅编辑器/调试时启用）
```

### RouteStageSceneEntry

负责提供整张路线场景的根引用、节点入口列表和连接绑定列表。它不负责战斗和路线状态机。

### CombatNodeSceneEntry

每个节点一个，直接引用：

- 对应的 CombatNodeConfig；
- HeadJunction；
- CombatArea；
- TailJunction；
- Head→Combat 路径；
- Combat→Tail 路径。

### RouteConnectionSceneBinding

每条连接一个，直接引用：

- 对应的 RouteConnection；
- source Tail；
- target Head；
- 路径点；
- 转向点；
- 可选遮挡点。

终点节点不需要 RouteConnectionSceneBinding；它是普通连接的目标节点。终点节点到达 Tail 后不再寻找下一条连接。

### 编辑器辅助对象

路径点、转向点、阶段目标点和调试 Gizmo 属于空间编辑/校验数据，不承担独立的路线逻辑状态。运行时状态由 Battle 中的路线流程控制器保存。

## 17. 当前场景化路线待办（交接清单）

### 当前路线实现边界与真实 BUG

以下事项不属于当前路线 BUG 或修复目标：

- RouteStageRoot 按当前 V2 锚点、旋转支点和最终 Pose 契约移动；不得把路径起终点 Delta 作为额外位移；
- 当前路线选择界面可以继续作为既有功能界面使用，是否改为正式 Canvas 表现属于独立 UI 工作；
- `RouteBattleEntryV2.conditionEnabled` 当前只表示该条目是否被配置为跳过测试开关，不等同于已完成的条件系统；它不是本轮缺陷；
- MainMenu 的“继续游戏”不读取路线存档点快照；它从最后未完成的路线关卡的 `RouteStageConfig.startNode` 开始。失败面板的路线重开才使用最近存档点快照：先执行运行时 `ResetAll`，再从保存节点自身的 Head 重新进入，而不是从新节点或 `startNode` 开始。选关/新游戏同样从 `startNode` 开始。

本轮只处理以下三个真实路线 BUG：

### P0-1：战斗清空必须等待奖励选择结算

战斗清空回调只能表示当前 BattleEntry 的敌人/波次已经清空，不能直接推进 `_battleEntryCompleted`、节点胜利演出或 Combat→Tail。路线运行时必须进入“战斗已清空、等待阻塞奖励”的中间状态，持续等待当前节点产生的所有三选一、Boss 道具选择及弃置流程完成；只有 `UpgradeChoiceManager.IsChoosing == false` 且弃置弹窗不再显示时，才允许标记 BattleEntry 完成并继续节点流程。

等待期间必须保持奖励 UI 可交互，但禁止普通攻击手势、主动技能、限次道具和任何被动战斗触发。奖励选择完成回调与轮询兜底必须受当前 BattleEntry owner/generation 保护，并且只能推进一次，避免旧清空回调或重复回调进入 Tail。

验收：在最后一波产生经验升级或 Boss 道具三选一时，清场后必须停留在奖励界面；确认/弃置完成前不能进入节点胜利演出、Combat→Tail 或路线选择；确认完成后只推进一次。

### P0-2：非战斗阶段技能运行态和 UI 必须就绪

Travel、Head→Combat、Combat→Tail、Tail、路线选择、节点胜利演出以及等待三选一/弃置期间均属于非战斗阶段。此期间不允许主动技能释放，不允许计时被动或持续战斗效果触发；Hero HUD 保持可见，但所有技能冷却环、冷却数字和计时显示必须呈现“就绪”，不能冻结上一个 Combat 的剩余时间。

进入任意非战斗阶段时统一清理当前攻击/蓄力、主动技能冷却、计时被动计时器、临时战斗效果及相关输入状态。UI 不应只读取旧运行态并冻结，而应以当前路线阶段为准显示就绪；进入正式 Combat 前保持就绪，Combat 开始后才重新启用正常冷却和计时。

验收：从 Combat 离开到 Tail、选择路线、Travel、进入下一节点前，主动技能 UI 无倒计时且可视为就绪；期间无被动效果生成；正式 `StartRouteBattle` 后冷却/计时从新的 Combat 状态开始。

### P0-3：Head→Combat 技能触发问题（待持续观测）

当前不将该问题判定为已修复。测试期间保留 `[TimedPassiveDiag]`、`[RouteDiag]`、DoT 诊断日志，重点观察：Head→Combat 是否出现 `TimerExpired`/`BurnTick`，`StartRouteBattle` 后首次触发是否成功，以及效果启动失败时是否错误进入冷却。


以下事项仍会影响最终实现字段或边界行为：

1. 存档点快照是否还要保存局外状态的版本号或校验信息；
2. 强退恢复是否自动继续，还是回到主菜单后由玩家选择继续；
3. 条件系统的具体条件类型和条件组合规则；
4. 存档点在到达可存档节点 Head 的同一时刻写入；恢复从该 Head 重新执行 Head→Combat。
5. 普通关卡重开是通过重置并复用已加载 RouteStage Scene，还是先卸载后重新加载同一 RouteStage Scene。两者对玩家可见结果必须一致：所有关卡状态清空、RouteStageRoot 恢复初始编辑姿态、从 startNode Head 开始；
6. 已完成 BattleEntry 的重访规则：当前规则是不重复挑战；如果未来需要允许重访重做，必须新增明确配置字段，不能由运行时猜测；
7. 存档点恢复后，存档节点的 Head→Combat 是否播放完整移动演出，还是直接应用最终 Combat Pose。当前规则按“重新执行 Head→Combat”处理。

以下规则已经确定：

- RouteStage Scene 指一关独立的路线 Unity 场景，场景内始终包含全部 CombatNode 和连接表现；
- 节点切换不加载或卸载单个 CombatNode；
- `RouteStageConfig.startNode` 唯一决定普通关卡重开的起点；
- BattleEntry 条件可以读取局内或局外状态；
- 条件不满足时本次跳过；
- 全部 BattleEntry 本次跳过时直接进入 Tail；
- 不存在节点奖励或 BattleEntry 独立奖励；
- 终点演出完成或跳过后发放整关奖励；
- 终点节点可将 Tail 设置在 CombatArea 位置，避免额外离场位移；
- 存档点在节点 Head 到达时保存；
- 存档点保存当前关卡所有节点的挑战状态、BattleEntry 状态、已到达节点和剧情状态；
- 存档点保存玩家生命、持有技能、UT 充能；不保存连击和其他短时战斗状态；
- 存档点位于节点 Head，不保存敌人、投射物或其他 Combat 临场效果；
- 从存档节点 Head 重开时，已完成 BattleEntry 不重复挑战，未完成 BattleEntry 按当前条件重新判断；
- 普通关卡重开清空整关状态并从 startNode Head 开始；
- 存档点恢复载入整关挑战快照，并从存档节点 Head 开始；存档节点重新执行 Head→Combat 和本次应执行的 BattleEntry；
- 强退不等同于普通失败。
## 14. 当前核心玩法落地范围

第一阶段只实现节点到节点移动闭环：

```text
加载 RouteStage Scene
→ startNode.Head
→ Head→Combat
→ CombatArea 到达 combatTarget
→ 执行一个 battleConfig
→ Combat→Tail
→ Tail 到达 tailTarget
→ 玩家选择普通连接
→ 先旋转再沿连接路径移动到目标 Head
→ 目标 Head→Combat
→ 重复
```

第一阶段暂不实现：

- 多个 BattleEntry；
- 条件系统；
- 多 Tail 汇入同一 Head 的完整编辑器预览；
- 存档点和强退恢复；
- 终点演出和整关结算。

这些功能建立在核心移动闭环验证成功之后。
```text
RouteStageConfig.startNode
→ RouteStage Scene 中对应 CombatNodeSceneEntry
→ Head
→ Head→Combat
→ BattleEntry 顺序处理
→ 战斗收益由 battleConfig/敌人配置产生
→ 条件不满足的条目本次跳过
→ Combat→Tail
→ Tail 路线选择
→ 普通连接先旋转再移动到目标 Head
→ 目标 Head→Combat
→ 下一节点
→ Tail 选择终点
→ 整关结算
```
