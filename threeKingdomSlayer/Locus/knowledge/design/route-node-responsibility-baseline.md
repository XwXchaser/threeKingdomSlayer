---
id: kd_4a05f94c-c97a-4aab-8a5c-1538d75ae6ef
injectMode: inherit
aiEditMode: inherit
---

# 路线节点分工更新基线（V2，当前对话方向）

> 本文已按当前重构方向修正。旧版“独立 JunctionNode、completionJunction、Battle 内 RouteWorldRoot、RouteWorldRoot 移动”规则全部失效。详细配置和流程以 `route-scene-architecture-v2.md` 为准。

## 当前模型

- 路线关卡由若干 CombatNode 组成。
- 每个 CombatNode 都位于 RouteStage Scene 内，并包含 `HeadJunction`、`CombatArea`、`TailJunction` 子节点。
- Junction 是 CombatNode 的空间子节点，不是独立逻辑节点。
- Tail 的每个出口直接指向目标 CombatNode；目标入口是目标 Scene 的 HeadJunction。
- CombatNode 自己引用战斗 `StageConfig` 和节点固定奖励。
- Battle.scene 只保留固定 Player、Enemy、Camera、UI、战斗管理器和场景加载接口。
- Player 不移动；Travel 期间移动/旋转已加载的 CombatNode 场景表现，模拟玩家移动。

## 运行时关系

```text
CurrentCombatNode.TailJunction
→ RouteConnection.targetNode
→ TargetCombatNodeScene.HeadJunction
```

不再使用：

- `RouteNodeType.Junction`；
- `completionJunction`；
- Junction 独立奖励、Wave 或状态；
- `RouteWorldRoot`、`RouteWorldGraph`、旧 Channel；
- `forwardDistance`、固定 90 度、`Vector3.back` 等代码路线参数。

## 状态流程

```text
StageEntry
→ LoadStartCombatNodeScene
→ AlignHeadToBattleEntry
→ Combat
→ NodeReward
→ RouteChoice
→ LockConnection
→ Travel / FinalArrival
→ LoadAndAlignTargetScene
→ Combat
```

终点由目标 CombatNode 的 `isFinalNode` 表达，不创建虚拟终点 Junction 或额外的终点连接。进入终点节点后仍执行其 Head→Combat、BattleEntry 和 Combat→Tail；抵达终点 Tail 后执行终点演出并结算。

## 重要边界

- WaveSpawner 只负责现有战斗波次、敌人生成和清空事件。
- RouteStageController 负责当前节点、连接选择、场景加载调度和节点状态。
- CombatSceneLoader 只负责加载、就绪和卸载 Scene。
- TravelPresentationController 只负责场景移动/旋转、暂停和跳过。
- 场景就绪并完成 Head 对齐前，不得生成目标节点 Wave。
