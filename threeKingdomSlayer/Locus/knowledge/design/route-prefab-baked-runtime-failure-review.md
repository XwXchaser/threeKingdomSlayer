---
id: kd_aec42590-df09-4ecc-bb13-39892f781020
injectMode: inherit
aiMaintained: inherit
---

# 路线 Prefab/烘焙运行时方案失败复盘

## 1. 文档目的

记录探索类关卡重构中“Authoring 总览场景 + ScriptableObject 烘焙 + Battle 运行时动态实例化 Prefab”方案的失败原因、实际表现、验证证据和迁移结论，避免后续继续在同一坐标/生命周期模型上打补丁。

本文只记录复盘，不代表当前实现已经修复，也不要求删除现有实验资产。

## 2. 原方案目标

原方案试图同时满足：

- `Battle.scene` 常驻玩家、敌人、UI、Wave和固定战斗坐标；
- `Stage01_Route.scene` 作为完整路线总览编辑场景；
- 节点和Travel通过Prefab在Battle运行时动态实例化；
- Baker把总览场景中的节点、边、入口、出口、路径和摄像机锚点写入ScriptableObject；
- 运行时只保留当前节点、Travel边和目标节点三套表现；
- 玩家不在Battle世界中永久位移，Travel通过镜头和环境表现制造前进感。

## 3. 失败结论

该方案不适合作为当前项目的正式运行时空间架构，应停止继续扩展。核心原因不是单个Camera参数或单个Prefab错误，而是以下两个基础模型没有统一：

1. Authoring总览空间、烘焙资产空间、Battle运行时空间反复使用不同坐标基准，并在运行时通过多处补偿互相转换。
2. 环境Scene加载、目标环境预加载、Arrival镜头、节点进入、Wave生成和旧环境卸载没有形成单一的原子生命周期。

因此修复一个表象会暴露另一个问题：

- 修坐标后，敌人与环境可能不在同一空间；
- 修环境对齐后，Arrival镜头会返回原节点或原点；
- 修提前卸载后，镜头演出和敌人生成仍可能不同步；
- 修按钮遮挡后，Travel几何仍可能没有和Camera使用同一坐标系。

## 4. 已验证的运行时事实

### 4.1 路线逻辑并非主要失败点

已验证：

- 8个节点、9条边的DAG可烘焙；
- N0、N1、N4、N7可以通过路线逻辑抵达；
- Wave按节点正确生成，敌人内容通常正确；
- 节点金币和路线选择流程基本可运行；
- Additive Scene列表在部分流程中能出现Battle、当前节点、Travel和目标节点。

这说明路线数据、节点Wave和基础推进逻辑可以保留。

### 4.2 环境和敌人曾处于不同空间

一次N1 Combat采样得到：

```text
N1 Environment Root = (0, 0, 0)
N1 Arena = (-30, -0.5, 28)
Enemy = approximately (-1.15, 0.48, 0.52)
Main Camera = (0, 3, 18) or later (0, 3, -10)
```

这证明环境Scene内部仍保留总览世界坐标，而敌人与Camera使用Battle固定坐标。此时敌人已经生成，但不在当前环境构图中，攻击/受击等后续事件会让问题看起来像“敌人突然出现”。

后续曾将环境子物体归一化，采样变为：

```text
N1 Environment Root = (0, 0, 0)
N1 Arena = (0, -0.5, 0)
Enemy = approximately (-1.16, 0.46, 0.62)
Main Camera = (0, 3, -10)
```

这验证了局部环境坐标可以让敌人和地板处于同一空间，但也暴露出原方案此前依赖运行时根节点补偿，导致同一资产在不同阶段被重复移动。

### 4.3 Arrival镜头曾明确出现“原路返回”

诊断探针记录过完整序列：

```text
Travel末点：(-30, 3, 24)
Arriving阶段：
(-30, 3, 24)
→ (-27, 3, 21)
→ (-19, 3, 14)
→ (-9, 3, 4)
→ (0, 3, -4)
→ (0, 3, -10)
```

这不是主观感受，而是明确的Camera路径。根因是Travel使用源节点/总览坐标，而目标Combat Camera使用目标节点或Battle局部坐标，Arrival阶段直接插值两套空间。

之后虽然尝试记录`sourceNodeAuthoringPosition`、目标节点位置补偿和不同转换方向，但没有形成唯一、稳定、可维护的坐标规���。

### 4.4 选择路线后的UI按钮并非唯一遮挡来源

诊断探针记录：

```text
routeButtons = 0
```

因此路线按钮点击后确实已经隐藏。画面中仍出现的遮挡主要来自节点环境中的`LeftMarker`、`RightMarker`、`ForwardMarker`等白盒几何，而不是UI按钮本身。

这说明将问题归因于UI遮挡会误导调试；需要同时检查环境几何、Canvas和SceneView总览层。

### 4.5 Authoring总览曾污染Game画面

`Stage01_Route.scene`曾以Additive方式保留在运行时观察层，但没有可靠地排除Main Camera渲染。结果Game中出现：

- 总览场景的蓝色Arena地面；
- 节点标签和路线连接线；
- 这些对象与当前Battle环境叠加，造成“节点场景不对/地板一直不变”的假象。

后来通过EditorOnly Layer和Camera Culling Mask尝试隔离，但这个调试层本身又引入了额外的场景生命周期和编辑器API问题。

## 5. 具体失败点

### 5.1 坐标源过多且语义不清

同一条路线数据同时出现过：

- Authoring场景世界坐标；
- `RouteNodeDefinition.authoringPosition`；
- `RouteEdgeDefinition.travelPath`世界/根局部坐标；
- `sourceExitPosition`；
- `targetEntrancePosition`；
- Battle Camera当前世界坐标；
- 当前节点原点补偿；
- 源节点位置补偿；
- 目标节点位置补偿；
- Additive环境Scene根节点运行时偏移。

这些字段单独看都合理，但没有一个统一的“运行时坐标空间契约”，导致代码中出现多种互相冲突的转换：

```text
authoredPosition - currentNode.authoringPosition
authoredPosition - targetNode.authoringPosition
authoredPosition - sourceNodeAuthoringPosition
root.position = -node.authoringPosition
root.position = -edge.sourceExitPosition
```

这类补偿不能作为长期维护方案。

### 5.2 RuntimePresentation职责反复变化

`RouteRuntimePresentation`先负责：

- 动态实例化节点Prefab；
- 动态实例化TravelPrefab；
- 创建路线按钮；
- 移动Camera；
- 预加载目标节点；
- 清理Travel。

后续又被改成只负责UI和Camera，再与`RouteEnvironmentSceneLoader`、`RouteTransitionDirector`并行控制。过程中曾出现：

- 旧Camera协程和新Camera协程并存；
- 旧Prefab字段仍留在组件上；
- 目标节点Prefab预加载和Additive环境Scene同时存在；
- 多个组件响应同一状态事件。

这种职责漂移导致“谁是Main Camera唯一写入源”长期不明确。

### 5.3 状态机和表现机没有原子衔接

理想流程应该是：

```text
Combat清空
→ 等待阻塞奖励
→ 到达路口
→ 停住并显示路线选择
→ 选择路线并锁定
→ 转向
→ Travel
→ 目标环境完成加载和对齐
→ Arrival构图
→ Combat构图
→ 生成目标Wave
```

原方案实际曾出现：

```text
OnNodeEntered
→ Loader开始卸载/加载
→ Director开始Arrival
→ CompleteArrival
→ EnterNode
→ SpawnWave
```

但Loader、Director、RouteProgressionController各自监听事件并启动协程，没有一个统一的完成门控。因此会出现：

- 环境先卸载，Arrival尚未结束；
- 敌人先生成，环境仍在旧坐标；
- 目标Scene已加载但尚未对齐；
- Camera已切换但Scene几何尚未准备；
- 攻击事件触发后才显得对象出现。

### 5.4 Additive Scene曾被当成Prefab的替代品，却未完成坐标迁移

虽然生成了：

```text
N0_Plain_Environment.unity
N1_Ambush_Environment.unity
N4_Guard_Environment.unity
...
```

但这些Scene最初只是从总览Visual对象复制而来，内部继续保留总览坐标。仅生成Scene文件不等于完成运行时Scene架构。

正确迁移还需要：

- Scene内部根节点和子物体局部坐标规范；
- Camera Anchor与环境同一局部空间；
- Travel Scene使用独立且明确的源/目标坐标契约；
- Loader不再重复移动根节点；
- 运行时只使用Scene内对象，不再混用Prefab实例。

### 5.5 调试工具反而增加了误判

曾加入过：

- RouteAuthoring Scene静态Camera预览；
- RouteAuthoring Play Mode按钮预览；
- Battle SceneView跟随Main Camera；
- Additive加载Authoring总览观察层；
- RouteRuntimeDiagnosticProbe。

其中按钮预览器错误地隐藏总览对象并实例化临时对象，无法代表真实运行链路；Additive总览观察层还曾污染Game画面；SceneView跟随工具在Play Mode调用`EditorSceneManager.OpenScene`，造成大量编辑器异常：

```text
InvalidOperationException: This cannot be used during play mode
```

结论：调试工具必须只观察真实运行对象，不能另起一套近似运行流程。

## 6. 应保留的内容

- `StageProgressionMode.Route`；
- `RouteStageConfig`；
- `RouteNodeDefinition`中的节点逻辑配置；
- Route DAG和唯一终点校验；
- 节点Wave引用；
- 节点金币奖励；
- `RouteProgressionController`的基础路线推进职责；
- `WaveSpawner.SpawnExternalWave`；
- 现有Battle固定坐标约束；
- 暂停、跳过、失败重开等产品规则。

## 7. 应停止或废弃的内容

- Authoring世界坐标直接烘焙后在运行时多重补偿；
- Runtime动态实例化节点/Travel Prefab作为正式环境来源；
- `RouteRuntimePresentation`同时负责环境实例化和Camera控制；
- 多个组件同时写Main Camera；
- Arrival阶段直接把源坐标、目标坐标插值在一起；
- Authoring总览场景作为运行时可见环境；
- 仅为验证流程而创建的按钮式Play Mode预览器；
- 旧`RouteWhiteboxPresentation`及其屏幕覆盖/摄像机子物体走廊原型；
- 未经明确坐标契约的`authoringPosition`、`sourceExitPosition`、`targetEntrancePosition`运行时叠加补偿。

## 8. 推荐迁移方案

采用：

```text
Battle.scene常驻
+ Additive节点环境Scene
+ Additive Travel环境Scene
+ 场景内局部Camera Anchor/Path
```

运行时规则：

- Battle永远保持玩家、敌人、UI、Wave和Main Camera；
- 节点环境Scene内部所有对象使用节点局部坐标，根节点为零；
- Travel Scene内部所有对象使用边局部坐标，根节点为零；
- Loader只负责Additive加载、卸载和就绪门控，不再根据总览世界坐标移动根节点；
- Director只负责演出，不负责场景空间纠正；
- Camera只由一个Director写入；
- 目标环境完成加载并确认可见后，才允许目标Combat Wave生成；
- SceneView调试工具只观察真实Battle/Additive对象，不加载或执行Authoring逻辑组件。

## 9. 后续实现顺序

1. 删除或隔离旧Prefab环境实例化链路；
2. 规范化N0/N1环境Scene为局部坐标；
3. 规范化N0→N1 Travel Scene为局部坐标；
4. 让Loader只做Scene生命周期和就绪信号；
5. 让Progression等待环境就绪后再Spawn目标Wave；
6. 让Director实现Combat→Junction→Turn→Travel→Arrival→Combat完整演出；
7. 只验证N0→N1；
8. 再迁移N4、N7和其余分支；
9. 最后清理旧白盒、旧预览和历史兼容字段。

## 10. 复盘结论

当前失败不是因为Prefab技术本身不可用，而是因为本方案同时把Prefab、Authoring总览世界坐标、Additive Scene、Battle固定坐标和多套事件协程混在一起，形成了不可预测的运行时空间。

如果继续采用Prefab方案，必须彻底定义唯一运行时坐标空间和唯一环境/镜头生命周期；否则不应继续扩展。

对于当前项目的美术调节、镜头节奏和SceneView观测需求，推荐采用“Battle常驻 + Additive环境Scene + Scene内局部Camera Anchor/Path”，并以N0→N1为新的干净垂直切片重新实现。
