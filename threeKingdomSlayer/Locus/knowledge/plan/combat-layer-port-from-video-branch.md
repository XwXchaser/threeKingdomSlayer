---
id: kd_1984e112-c650-49cb-85fb-a24e233f03e5
injectMode: inherit
injectAgents:
- unity
aiEditMode: confirm
---

# 战斗层移植记录：来自 route-fake-movement（连招 + 骑兵109/102胆怯）

> 一句话：连招/输入与骑兵109、102胆怯形态已在本机可用并通过编译与冒烟；视频路线场景层仍在来源分支，未合入。

跨会话交接。做完这一轮后本机分支 `port/combat-layer-from-video-branch` 上已有来源分支的战斗层代码与资产。

## 1. 来源与边界

- 来源：`origin/route-fake-movement`，tip `f3d0407`（本机原同名分支停在 `c017a25`，需先 fetch 才能看到新提交）。
- 本机基线：`experiment/curved-scroll-travel`，两线分叉点 `2846123`。
- 结果分支与提交：`port/combat-layer-from-video-branch` / `ce7ae446`（101 文件）。
- 只取**战斗层**；来源分支的**视频路线场景层**未合入（见第 3 节）。

## 2. 已移植内容

| 类别 | 落点 |
|------|------|
| 输入与招式状态机 | `Assets/Scripts/MoveSystem/{PlayerMoveStateMachine,MoveTableConfig,MoveGesture}.cs`、`Assets/Scripts/Attack/StabMotionParams.cs` |
| 接入改造 | `Player/InputManager.cs`、`Player/AttackSystem.cs`、`Core/AttackSkillConfig.cs`（窗口/`repeatSelf`/`moveEdges` 写在一层资产上）、`Core/HeroConfig.cs`（新增 `moveTable` 字段）、`Core/HitFeedbackManager.cs`、`Attack/{AttackWave,StabSweepEffect,SweepEffect}.cs`、`Effects/{ChargeStabVisual,LaunchVisualEffect,PierceAimIndicator}.cs` |
| 招式资产 | `Assets/ScriptableObjects/Moves/MoveTable_Zhangfei.asset` + `Moves/Zhangfei/{Jab1,Jab2,Jab3,LaunchFinisher}.asset`；`Warrior/Hero_Zhangfei.asset` 接上招式表 |
| 敌人设计 | `Enemy/CavalryEnemy.cs`（骑乘冲锋状态机）、`Enemy/CavalryVisualController.cs`、`Resources/EnemyPrefabs/Enemy_109.prefab`；`Enemy/Enemy.cs`（骑乘委派、胆怯形态 `EnemyFormState`、免控、落马）、`EnemyManager.cs`、`Core/ColumnManager.cs`（骑兵槽位预留/免击退）、`EnemySpriteController.cs` |
| 敌人美术 | `Animations/Enemy_102.controller`、`Enemy_102_Idle/Walk`、`Enemy_102_Coward{Idle,Hit,Dead,Launched,Transition}.anim`、`Sprites/Enemy/Enemy2/**`（Normal 步行 + Coward/LostSplit 帧） |
| 打断链 | `interruptsCavalryCharge` 从 `AttackSkillConfig` 贯穿 `AttackWave`/`SweepEffect`/`StabSweepEffect` 到 `Enemy.TakeDamage`；`Zhangfei_Pierce/Sweep/Launch` 标记为可打断冲锋 |
| 场景接线 | `Assets/Scenes/Battle.scene` 玩家对象新增 `PlayerMoveStateMachine`（场景 `moveTable` 留空，由 `HeroConfig.moveTable` 提供；`showDebugPanel=false`） |
| 设计文档 | `design/combo-move-state-machine.md`、`design/cavalry-enemy.md`、`design/cavalry-enemy-animation.md`、`plan/combo-progress-and-next.md`、`plan/stab-combo-video-reference-workflow.md` |

连招链路（张飞）：`Tap → Jab1(20/0.45s) → Jab2(26/0.40s) → Jab3(34/0.60s) → LaunchFinisher(60/0.60s)`，接续窗口 0.65–0.95。中立态下未配置的手势回落为直通映射，因此横滑/竖滑/斜滑的攻击（Sweep/Slash/Launch/Parry）仍可达。

## 3. 有意排除（不要顺手合入）

- `Assets/RouteData/FakeStage01/**`（节点数据 + Presentations + **Videos 6 个 mp4** + changbanpo 背景图）。
- 来源的 `Scripts/RouteFake/{FakeMovementPresenter,FakeRouteConfig,FakeRouteLaunch,FakeRoutePresentation,FakeRouteRuntime}`、`Scripts/Route/RouteChoice*`、`Scripts/RouteV2/*`。
- 路线状态改造：`StageController.cs`、`SaveManager.cs`、`SpikeTrapController.cs`、`PassiveTriggerModule.cs`、`TimedPassiveModule.cs`、`Ultimate*`、`Effect/{CycloneEffect,ShootFireEffect,TimedArrowEffect}.cs`。
- 原因：本机是卷轴实验 + 另一套 route host；两线各自实现了同名不同内容的 `RouteFake/FakeRouteNodeConfig.cs`、`FakeRouteStageConfig.cs`，整份合并会互相覆盖。
- 未删除 `Resources/EnemyPrefabs/Enemy_1.prefab`（来源分支已删；本机 `EnemyPool.cs` 仅在注释中提及，保留无副作用）。

## 4. 验证结果

- 编译与 Console：无错误、无警告。
- 引用闭包：用 `AssetDatabase.GUIDToAssetPath` 校验移植资产（102 动画/控制器/prefab、Enemy_109），未解析引用 0。102 各胆怯动画与 prefab 各留 1 个空 sprite 键，来源分支同样如此（**有意空帧**，不是移植缺陷）。
- 运行冒烟（Battle.scene Play Mode）：`PlayerMoveStateMachine` 生效且 `UsesTable=True`、`moveTable=MoveTable_Zhangfei`，引用齐全；模拟一次 Tap 实际执行 `Zhangfei_Jab1`，第二次 Tap 进入输入缓冲（窗口 65% 未开，符合「空挥不推进段位」），段中蓄力横滑报「无匹配边」（符合设计）。
- 未覆盖：连段交接手感、扫击收尾、骑兵冲锋循环（本场景不生成骑兵，需关卡配置）。

## 5. 遗留与决策点

1. **数值**：`Enemy_101.prefab` 采用来源数值 `maxHealth 16→200`、`attackDamage 1→3`（本机此前是调试值 1）。若卷轴实验仍需要原值，改回即可。
2. **骑兵未接线**：`Enemy_109` 目前没有任何关卡/路线数据的 spawn 引用，视觉仍是占位（101 骑手 + 105 坐骑）。
3. **来源分支仍在推进**：视频路线流程、节点视频部署仍在 `route-fake-movement` 上；后续若要把场景层合过来，需先决定保留哪一套 RouteFake 实现。
4. **既有问题（非本次引入）**：`Battle.scene` 的 Main Camera 上有 2 个缺失脚本组件；本机 `.meta` 存在非 hex guid 混淆（1032/1113），属既有工程设置。
5. **本机未提交改动保持原样**：`Assets/Experiments/CurvedScroll/{FakeRouteDataTrial.unity,YJunctionSample.unity,Shaders/YScrollScenery.shader}`、`Locus/knowledge/{memory/project-mistake-note.md,plan/latest-todolist.md}`、`Locus/workspace-trees/default.json`。
