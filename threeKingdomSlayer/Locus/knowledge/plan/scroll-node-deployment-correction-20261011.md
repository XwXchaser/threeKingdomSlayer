---
id: kd_ee04a4ee-f331-4966-be32-45a74b5bf560
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: N1/E0战斗与奖励确认接线已完成，J1更正到190米；旧E1代码、场景占位和引用已移除，左J1→EV→N3、右J1→N2按Inspector节点资产运行。两次Play程序化全链路回归通过，正式数值和存档未交付。
---

# N1→E0→J1 节点部署更正与 E1 移除（2026-10-11）

## 当前结果

依据 `design/changbanpo-six-node-level-design.md` 第1.1节，本轮更正正式 Battle + Additive Y 路线的节点部署：

```text
N1（战斗，d=0）
→ 战斗完成、奖励等待、玩家确认唯一出口
→ E0（战斗，d=90）
→ 战斗完成、奖励等待、玩家确认唯一出口
→ J1（无战斗，d=190）
├─ 左路 −45°：EV（无战斗，d=225）→ 唯一出口确认 → N3（流程预览战斗）
└─ 右路 +45°：N2（流程预览战斗）
```

J1停点从上一轮为取景设置的180米更正到190米，与设计中现有Y路线几何分叉相同。E0保持门外90米，不把战斗阵型移进门洞。路线几何、CurveRadius、转角、Length、Battle相机及战斗坐标未改变。

E1已从本轮运行流程、Y场景部署和节点引用中移除。该结果覆盖旧 `plan/j1-scene-deployment-handoff-20261011.md` 中“J1=180、旧E1仍隐藏保留、Host尚未迁移”的状态。

## 资产与 Inspector 部署

新路线目录：`Assets/RouteData/ChangbanpoScroll/`。

- `Changbanpo_ScrollRoute.asset`：FakeRouteStageConfig，startNode=N1，nodes=N1/E0/J1/EV/N2/N3；不存在E1。
- `Nodes/`：六份 FakeRouteNodeConfig。
- `Choices/`：N1→E0、E0→J1、J1→EV、J1→N2、EV→N3 五份出口。
- `Travels/`：五份独立旅行配置，duration统一3秒，speedCurve继承现有路线调速曲线。
- `Battles/E0_Battle_Preview.asset`：复制当前N1新画风预览配置的独立E0配置，1名1011，仅用于流程部署验证；正式波次/难度尚待内容任务。

N1仍引用既有 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`；N2/N3继续使用 `Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset` 作为预览战斗。没有改动这些共享战斗配置。

Battle场景的 `BattleYRouteHost` 通过Inspector引用新路线及明确的左右choice：

- 左：`j1_to_ev → EV`；
- 右：`j1_to_n2 → N2`；
- EV唯一出口：`ev_to_n3 → N3`。

正式场景锚点位于：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Route Nodes - N1 E0 J1 and Branches`

| 锚点 | 路径距离 | 路线局部位置 | 朝向 |
|---|---:|---|---|
| N1 - Presentation Anchor | 0 | (0,0,0) | 0° |
| E0 - Presentation Anchor | 90 | (0,0,90) | 0° |
| J1 - Presentation Anchor | 190 | (0,0,190) | 0° |
| EV - Presentation Anchor | 225 | (-17.3578,0,219.0219) | -45° |
| N2 - Presentation Anchor | 266.116455 | (46.4315,0,248.0956) | +45° |
| N3 - Presentation Anchor | 266.116455 | (-46.4315,0,248.0956) | -45° |

路径距离与转弯后的局部Z不同；运行器读取 `worldDistance`，不会把分支锚点的Z误当作行进距离。N1/E0/J1的backgroundAnchor分别绑定开场路侧、E0门垣与J1布景组。BattleEncounterAuthoring已改为N1/E0/N2/N3身份，锚点和朝向与实际路径一致。

`Route Connections` 记录五条连接及显式sourceNode/choice/targetNode引用、路径点，便于Scene核对。运行仍由唯一Battle宿主驱动StageController，没有在Y层放第二套真实战斗系统。

## 代码更正

- `Assets/Scripts/Core/BattleYRouteHost.cs`：消费FakeRouteNodeConfig的战斗和出口引用，取代旧硬编码E0/E1故事停点；战斗后等待奖励/经验/对话/死亡回调收尾，单出口也必须确认；重复点击和奖励/暂停期间点击被拒绝。
- `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs`：删除旧e0/e1/j1StoryStop字段，改为显式nodePresentations数组，通过节点资产和worldDistance获取运行停点。
- `Assets/Experiments/CurvedScroll/Scripts/ScrollRouteNodePresentation.cs`：增加方向字段，按既有YScrollSample.Evaluate计算左右分支锚点位置和朝向。

当前宿主只处理本预览图中已有的战斗及出口确认内容；含对话/独立节点奖励或条件战斗Entry的配置会明确拒绝启动，不声称已交付通用正式路线运行器。

## E1清理与保护范围

- 删除旧 `E1 Integrated - Residual Muster @ 150` 组及其9个占位子对象；其中7个登记Renderer引用一并移除，scenery从441降为434。
- 旧156米的未绑定节点锚点取消E1身份并转为N1开场锚点；场景锚点均已绑定明确节点资产，没有残留E1节点。
- `E1 story stop`、旧storyStop确认字段/方法和e1StoryStop引用已从当前Host/Y代码删除；项目C#检索及Y场景检索无相关残留。
- J1的144个景物Renderer、N1/E0美术布局、门垣1.2倍基线、共享PNG/meta/Shader/材质未调整。
- 原Stage01白盒、Stage01V2和FakeRouteDataTrial实验资产未迁移或回退。
- Battle只按本轮授权更正路线宿主绑定；Player/Camera/Enemy/StageController/HUD配置没有为节点美术改动。

## 验证证据

C#完整编译及domain reload通过。FakeRouteGraphValidator校验新图通过；全部节点有唯一场景锚点，travelDistance与source/target路径距离一致。

两次Play Mode完整沿Host流程推进：敌人通过TakeDamage清空，教学通过BattleDialogueView.Advance推进，奖励通过现有ConfirmChoice处理。未使用真实手势通关，不能作为战斗手感/数值验收。

已验证：

- N1战斗中不能确认离站；
- 强制触发一轮真实奖励选择时，timeScale=0、确认被拒绝、路线距离保持不变；
- N1、E0战后都停在Confirm exit，直到唯一出口确认；
- 双击只接受第一次确认；
- 旅行暂停时progress不变，恢复后继续；
- E0在90米启动独立E0战斗，生成1名1011；
- E0确认后直接到J1=190米，期间没有E1阶段；J1无战斗Entry，alive=0；
- 左路实际消费j1_to_ev，抵达EV=225/-45°；EV保持无战斗、等待唯一出口确认，随后到N3=266.116455；
- 右路实际消费j1_to_n2，抵达N2=266.116455/+45°；
- 左右预览战斗都能结束为Completed (test only)，重复分支点击被拒绝。

第二轮测得N1→E0约3.0137秒、E0→J1约3.0048秒、J1→N2约3.0068秒，符合每段3秒配置。

更正后的J1实际Play截图：`Library/Locus/Screenshots/locus_game_20261011_141306_226.png`，489×979，使用Battle Main Camera，顶部显示J1节点身份。

结构化过程证据：`Library/Locus/tmp/node-deployment-correction-20261011/` 下的 `left-to-j1-test.json`、`left-branch-test.json`、`right-to-j1-test.json`、`right-branch-test.json`。

退出Play后再重载Battle/Y：Active=Battle，双方dirty=false，progress=0、right=false、viewCamera=None、scenery=434、null=0、duplicate=0，节点/出口引用保持，Console warning/error=0。

## 仍未完成

- E0、N2、N3正式波次、难度、时长与奖励数值；当前均为流程预览内容。
- EV、N2/N3后续环境和正式关卡续接；当前N2/N3是预览终点，未宣称整关通关系统完成。
- 门内半3D空间、实际穿门遮挡和完整美术/分辨率验收。
- 正式安全节点存档/恢复；savePoint字段存在不代表已消费保存语义。
- 真实手势战斗、失败路径、完整道具弃置阻塞和Boss/QTE覆盖。

后续继续布景时以本文件的190米J1锚点及新路线资产为准，不再恢复旧E1停点或180米临时J1展示停点。
