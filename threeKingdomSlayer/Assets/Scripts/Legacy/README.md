# Legacy：遗留路线实现（不参与产品流程）

> 这个目录里的代码**不参与当前产品流程**，只服务于早期实验与遗留演示。
> 2026-10-20 从 `Assets/Scripts/Route` / `Assets/Scripts/RouteV2` / `Assets/Scripts/Core` 收拢到这里。

## 里面有什么

| 目录 | 内容 | 原属 |
|---|---|---|
| `RouteV1/` | `RouteProgressionController`、`RouteStageConfig`、`RouteNodeDefinition`、`RouteDirection`、`RouteWorld*`（原型：世界锚点/图/运动/节点流） | `Assets/Scripts/Route/` |
| `RouteV1/BattleYRouteHost.cs` | 旧的 N1→E0→J1 预演驱动（`ScrollRoutePresenter` 是它的继任者） | `Assets/Scripts/Core/` |
| `RouteV2/` | `RouteStageRuntimeV2`、`RouteStageConfigV2`、`RouteStageSceneEntryV2`、`RouteStageTargetsV2`、`RouteStageV2Launch` | `Assets/Scripts/RouteV2/` |
| `RouteV1/Editor/`、`RouteV2/Editor/` | 上述实现的编辑器工具（预演窗口、校验器） | 各自目录 |

## 为什么留着

- **实验场景还要用**：`Assets/Experiments/CurvedScroll/` 下的试玩/诊断场景
  （`Bisect.unity`、`ScrollCombatTrial.unity`、`ValleyCombatTrial.unity`、`FakeRouteDataTrial.unity`）
  历史上挂着 V1/V2 驱动。它们不在 build settings 里，只在编辑器里跑。
- **删除成本高于收益**：这些实现没有资产依赖（只有代码 IL），对包体的影响可以忽略。

## 现在的边界（2026-10-20 起）

现役代码**不再引用**本目录的任何类型：

- `StageController` 里 V1/V2 的死分支、`routeStageConfig` / `routeStageConfigV2` 字段已删除；
- `StageConfigManager.routeStagesV2`（死字段）已删除；
- `BattleHUD` 的 `RouteStageV2Launch` 旗标分支已删除；
- `Battle.scene` 里挂着 `RouteStageTargetsV2` 的残留对象已删除；
- `YScrollSample.battleHost` 改成 `Component` 类型（只当「动画由谁接管」的标记），不再依赖旧驱动类型。

也就是说：**Legacy 现在是一个孤岛，只有遗留实验场景会用它。**

现役代码里唯一与 Legacy 相关的部分，是 `StageController` 上 5 个**只给遗留实现用**的入口
（`SetCurrentNodeBattleConfig` / `StopCombatForRouteTravel` / `StartCurrentRouteNode` /
`CompleteCurrentCombatNode` / `CompleteRouteStage`）。它们刻意**不引用任何 Legacy 类型**
（V2 结算改成由调用方传 `stageId`），所以 Legacy 仍然不与现役代码产生类型依赖。

## 什么时候可以删

前置条件（按成本排序）：

1. 确认遗留实验场景不再需要 V1/V2 流程——`Bisect.unity` 等场景里对 `StageController.routeStageConfig` /
   `routeStageConfigV2` 的序列化赋值会随字段删除而失效（这两个字段已删）。
2. 如果确实还要保留这些实验：把共享类型（`StageController`、`Enemy`、`FakeRouteStageConfig` 等）
   先搬进自己的 asmdef，然后给本目录加一个 `includePlatforms: ["Editor"]` 的 asmdef，
   才能真正把它从玩家包里剔除。
   > 注意：**asmdef 程序集不能引用 Assembly-CSharp**，而本目录大量依赖 Assembly-CSharp 里的类型，
   > 所以「给 Legacy 加 Editor-only asmdef」这一步必须等共享类型先独立出来，否则直接编译不过。

## 规则

- **现役代码不许引用本目录的类型**（新功能请基于 `RouteFake/` 的 FakeRoute 流程）。
- 本目录只允许小修（让遗留实验还能跑），不做功能扩展。
