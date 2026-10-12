---
id: kd_3e4c5554-7867-43dc-95a4-3a1559154f12
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: J1素材部署历史交接：左石路、右官道、路牌和路侧密度已保存。后续节点更正已将J1恢复到190米并移除E1；当前流程与scenery434以scroll-node-deployment-correction-20261011.md为准。
---

# J1 场景素材接入与部署交接（2026-10-11）

> **后续节点更正已完成，当前入口为 `plan/scroll-node-deployment-correction-20261011.md`。** J1停点已从180恢复到190米，旧E1代码/场景占位/引用已移除；N1→E0→J1、左EV→N3/右N2按新节点资产运行，scenery=434。下文记录素材首轮部署历史，其180米停点、E1保留和旧Host状态不再代表当前实现。

## 1. 本轮结果

本轮已按已验收的 J1 v3 参考图，把现有 J1 素材部署到正式路线层：

```text
Assets/Scenes/Battle.scene
└─ Additive Assets/Experiments/CurvedScroll/YJunctionSample.unity
   └─ Y Junction - select for preview
      └─ J1 Integrated - Official Road and Stone Lane @ 190
```

已完成：

- A01 中央无字双箭头路牌接入；
- A03/A04 完整与破损低木篱笆接入；
- A05 完整木质运输车接入；
- 复用现有箱子、残旗、草、灌木和石块，补齐左右路肩与 J1 接近段；
- 新建分支感知地面 Shader/材质，左支使用灰绿色石路，右支及共同来路继续使用黄土道路；
- 取消的 E1 视觉占位组隐藏但未删除；
- 路线 Host 的选路按钮更新为“左路：村庄石路”和“右路：官道”。

本轮没有生成新图片，没有修改 `YScrollSample.Evaluate()`、`Length`、`JunctionDistance`、`CurveRadius`、`turnAngle`、Battle 相机或战斗坐标，也没有打开/保存 `FakeRouteDataTrial.unity`。

## 2. 新增资源与导入基线

### 新建工程资源

- `Assets/Experiments/CurvedScroll/Shaders/YGroundJ1Branches.shader`
- `Assets/Experiments/CurvedScroll/Authoring/J1_Ground_Branches.mat`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/` 下的6张PNG及meta

### J1图片导入

透明道具：

- `J1_A01_DoubleArrowSignpost_v1.png`
- `J1_A03_IntactLowFence_v1.png`
- `J1_A04_BrokenLowFence_v1.png`
- `J1_A05_CompleteWoodenCart_v1.png`

导入为 Single Sprite，PPU=100、Point、Uncompressed、mipmap off、Alpha Is Transparency、Clamp；脚点按明显主体 Alpha 底边设定。当前关键脚点：

- A01 pivot=(529,91) px；
- A03 pivot=(400.5,89) px；
- A04 pivot=(395,92) px；
- A05 pivot=(833,66) px。

地面纹理：

- `J1_StoneRoad_Center_v1.png`
- `J1_StoneRoad_Sides_v1.png`

保持 Default Texture、Bilinear、Repeat、Uncompressed、mipmap on。两张PNG哈希与 J1 纹理交接记录中的本地平铺修正版一致：

- Center：`821f5fa72a05cc8d68ccbe080ee8b3c80d92d8338c5d67011c157d9a0d546ee4`；
- Sides：`f58c699906761bc2eb89b3deffe15a384f1d08bf56ce7b3bfb99a5cfdf54fadc`。

## 3. 正式场景布局

J1 根组：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/J1 Integrated - Official Road and Stone Lane @ 190`

根组局部位置为 `(0,0,190)`，用于对应几何分叉区。其下四个主要分组当前 Renderer 数量：

| 分组 | Renderer | 职责 |
|---|---:|---|
| Central Signpost and Divider | 11 | 路牌、分隔带低矮草灌木石块 |
| Left Stone Lane - Fences and Low Vegetation | 44 | 左侧村庄石路篱笆、低草、灌木、石块 |
| Right Official Road - Transport and Supplies | 45 | 右侧官道运输车、箱子、残旗、篱笆、低草、灌木、石块 |
| J1 Approach - Low Shoulder Detail | 44 | J1前段两侧低矮路肩密度 |
| **合计** | **144** | |

新增对象均为可选中、可保存的真实 Hierarchy 对象。复用图集对象按实际 Alpha 底边做了局部 Y 脚点补偿；J1透明新图使用Importer Pivot，不做重复补偿。

### J1停点

- 几何分叉仍为 `JunctionDistance=190`；
- J1选择展示停点改为 `worldDistance=180`，保留约10米共同来路，让玩家在选择界面同时看到路牌、左石路入口和右官道入口；
- `YScrollSample.Length=266.116455`；
- `CurveRadius=28.1595573`；
- `turnAngle=45`；
- 左路终态 heading=-45°，右路终态 heading=+45°。

## 4. 地面接入

正式 Terrain 的 MeshRenderer 改绑：

`Assets/Experiments/CurvedScroll/Authoring/J1_Ground_Branches.mat`

该材质沿现有 Y 路径距离计算分辨：

- 共同来路：`N1_Road_v1` 黄土；
- 右支官道：继续使用 `N1_Road_v1` 黄土；
- 左支：使用 `J1_StoneRoad_Center_v1` 灰绿色石路；
- 左支道路外缘：使用 `J1_StoneRoad_Sides_v1`；
- 外侧地面和肩部：继续使用现有 N1 外地/肩部纹理。

材质关键参数：

- `_RoadHalfWidth=2.45`；
- `_ShoulderWidth=1.5`；
- `_JunctionDistance=190`；
- `_CurveRadius=28.1595573`；
- `_LeftStoneTileSize=6`；
- `_StoneEntryLength=8`；
- `_BranchMaterialBlend=0.6`；
- `_LeftStoneRoadBlend=0.45`；
- `_LeftStoneTint=(0.89,1.04,0.93,1)`。

Shader 已在 Unity 中重新导入并回读，当前无 Shader 编译错误。

## 5. 旧内容处理

- `E1 Integrated - Residual Muster @ 150` 已隐藏，但9个对象仍保留；没有删除或改GUID。
- 旧 `Left branch landmarks`、`Right branch landmarks`、`Left - Valley Encounter`、`Right - Camp Encounter`、`Junction Rock Barrier and Roadside Density` 均保持隐藏。
- `YScrollSample.leftBranchVisuals/rightBranchVisuals/leftBranchEncounter/rightBranchEncounter/junctionVisuals` 已置空，避免旧山谷/营地组在新 J1 布景下被 Host 重新启用。
- J1 新组是持续可见的 authored scenery，不依赖旧分支组显隐。

## 6. 验证结果

### Edit Mode 保存回读

完成 Y 场景保存、关闭并重载后：

- Active Scene=`Assets/Scenes/Battle.scene`；
- Battle dirty=false；
- Y dirty=false；
- progress=0；
- right=false；
- root position=(0,0,0)；
- viewCamera=None（Edit Mode 下正常，Play 时由 Host 绑定 Main Camera）；
- `YScrollSample.scenery=441`；
- null 引用=0；
- duplicate 引用=0；
- J1 Renderer=144；
- J1 Renderer 全部 `Default/0`；
- J1 地面材质正确绑定；
- J1停点=180；
- Console warning/error=0。

回读还确认原先297项 scenery 引用的顺序全部保持不变；新增引用追加在后面。850个既有对象均保留。保护文件哈希检查为340个文件，无意外变化；Battle只保留其原有工作区差异，没有被本轮保存覆盖。

### 画面采样

纯场景采样目录：

`Library/Locus/tmp/j1-deployment-20261011/final/`

包含：

- `01_approach_168.png`：J1前段接近；
- `02_j1_decision_180.png`：J1选择停点；
- `03_geometric_fork_190.png`：几何分叉起点；
- `04_left_curve_202.png`：左路转弯；
- `05_right_curve_202.png`：右路转弯；
- `06_left_exit_232.png`：左路后段；
- `07_right_exit_232.png`：右路后段；
- `08_left_endpoint_266.png`：左路终点；
- `09_right_endpoint_266.png`：右路终点；
- `10_n1_0.png`：N1复核；
- `11_e0_90.png`：E0复核。

Play Mode 下另做了左右选路入口隔离检查：两条路线均能到达 progress=1，左路 heading=-45°、右路 heading=+45°；J1组保持启用，旧山谷/营地组保持隐藏，地面材质保持 `J1_Ground_Branches.mat`。

带 HUD 的 J1选择截图：

`Library/Locus/Screenshots/locus_game_20261011_125818_813.png`

截图显示“左路：村庄石路”和“右路：官道”按钮、中央路牌、左石路和右侧运输车。Console warning/error=0。

## 7. 尚未完成与验收边界

本轮完成的是**J1视觉素材接入与路线层部署**，不是完整 J1 逻辑交付：

- `BattleYRouteHost.cs` 仍执行旧的 N1→E0故事停点→E1故事停点→J1选择流程；E1逻辑取消尚未迁移到 Host；
- E0尚未接入独立战斗配置、奖励等待和唯一出口门控；
- J1选择后的左右目标仍是测试分支战斗配置，尚未接入正式 N2 / EV / N3；
- 门内半3D空间、门后到J1的完整穿门表现仍未制作；
- 左石路与右官道的当前接入验证是固定主相机采样，不等于用户完整自然旅行审美验收；
- Play Mode 的程序化清敌回归在奖励等待/战斗状态交织处未可靠走通，因此没有宣称完整 N1→E0→J1自然流程通过；
- 当前J1参考图和素材部署仍需用户对整体比例、路面纹理、路牌大小、运输车体量和路侧密度做最终主观验收。

## 8. 保护范围

后续继续工作时：

- 保持 Battle.scene 的 Player、Enemy、Camera、StageController、HUD、战斗坐标不变；
- 不修改 `YScrollSample.Evaluate()`、路线长度、分叉半径和转角；
- 旧E1对象已在后续用户明确要求下移除；其余旧候选不自动删除；
- 不向 `FakeRouteDataTrial.unity` 部署；
- 不用 Save All 清理 Battle；
- J1 新组和材质的后续调整先在 Game 主相机下取样，再只保存授权的 Y 层。
