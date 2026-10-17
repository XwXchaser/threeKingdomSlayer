---
id: kd_21942a6c-cc88-4b7f-a49d-4e64c16a4a87
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1 布景交接与村庄部署入口（2026-10-14）

## 状态

**用户已验收本轮 J1 布景（2026-10-14，包含第一轮植被接入与第二轮篱笆/密度/右官道修正）。** 已验收的透明植被素材接入正式 Additive Y 路线层，并保存/重载验证通过。Battle.scene 保持用户已有 dirty 改动，不由本轮保存覆盖。

下一阶段为**村庄相关场景部署**（左支 −45° 绿色石路 → `EV·村落外围` → `N3 村落搜救`），入口见本文末「下一阶段：村庄场景部署」。

正式结构：

`Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`

不操作：`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。

## 新增素材

来源原图：

`C:/Users/Administrator/Pictures/gptGen/j1_vegetation_blockers_g2_v3/alpha_cleanup_v1/j1_tree_grass_alpha_cleanup_v1.png`

已按实际主体范围制作四张独立 Sprite，未按原图组固定格线机械切片：

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/Vegetation/J1_Tree_Broad_v2.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/Vegetation/J1_Tree_Willow_v2.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/Vegetation/J1_Grass_TallDense_v2.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/Vegetation/J1_Grass_TallWide_v2.png`

导入设置均已回读：Single Sprite、PPU 100、Point、Uncompressed、mipmap off、Alpha Is Transparency、Clamp；Pivot 使用实际主体脚点附近的自定义值。四张 Sprite 的像素尺寸分别为 1096×1229、908×1233、648×755、1003×595。

树和高草的生成原图、Alpha验收报告仍保留在 `C:/Users/Administrator/Pictures/gptGen/j1_vegetation_blockers_g2_v3/alpha_cleanup_v1/`。

## 场景接入

新增正式层级：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/J1 Integrated - Official Road and Stone Lane @ 190/J1 Vegetation - Accepted Trees and Tall Grass`

共14个新 Renderer：

- 2棵树：`J1_Tree_Broad_Main`、`J1_Tree_Willow_Secondary`。
- 6丛中央高草：树根/中央低处填补。
- 3丛左路肩高草。
- 3丛右路肩高草。

树使用现有 `Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_E0Route.mat`。高草保留原 SpriteRenderer 作为布局源但禁用显示，使用独立 Wind Mesh 子节点实际渲染；新建：

- `Assets/Experiments/CurvedScroll/Authoring/J1Vegetation/J1_Grass_TallDense_v2_WindMesh.asset`
- `Assets/Experiments/CurvedScroll/Authoring/J1Vegetation/J1_Grass_TallDense_v2_Wind.mat`
- `Assets/Experiments/CurvedScroll/Authoring/J1Vegetation/J1_Grass_TallWide_v2_WindMesh.asset`
- `Assets/Experiments/CurvedScroll/Authoring/J1Vegetation/J1_Grass_TallWide_v2_Wind.mat`

新草材质参数：`_WindStrength=0.10`、`_WindSpeed=1.7`、`_WindTimeOverride=-1`。每个新风动网格45顶点，自动时间生效。

`YScrollSample.scenery` 从434增加至448。新组所有实际 Renderer 均在 scenery 中，重复引用0、空引用0；禁用的布局源 SpriteRenderer未追加为重复渲染。

## 现有篱笆修正

按交接文档中的实例尺度建议，对8个现有实例进行了有限调整：

- 左侧 `J1_Left_Fence_01/02`：0.52
- 左侧 `J1_Left_Fence_03/04`：0.44
- 左侧 `J1_Left_Fence_05/06`：0.38
- 右侧 `J1_Right_Fence_01`：0.44
- 右侧 `J1_Right_Fence_02`：0.38

未更换 Sprite、Importer 或路线参数。主相机采样中左路道路净空仍保持开放。

## 采样与验证

采样目录：

`Library/Locus/tmp/j1-vegetation-deployment-20261014/final/`

已生成16个主相机采样，覆盖 Left/Right 的 Progress 0、0.25、0.5、0.75、1，以及接近段175、J1选择190、左右分支202和后段232。

重点回读：

- `final/J1_190.png`：树群在路牌后形成中央上层遮挡，路牌、左石路、右官道和运输车仍可读。
- `final/left232.png`：左石路转弯后中央道路保持开放，树群未侵入当前道路画面。
- `final/right232.png`：右官道和运输车方向保持开放，没有新增植被堵住道路。

重载验证：

- Y 场景关闭并以 Additive 重载成功。
- Active Scene 仍是 `Assets/Scenes/Battle.scene`。
- Y 场景 dirty=false；Battle.scene dirty=false（本轮保存 Y 时没有保存 Battle，当前Battle dirty状态以现场为准）。
- 根节点位置恢复为 `(0,0,0)`，progress=0、right=false、viewCamera=None（Edit Mode正常）。
- Y 场景 `scenery=448`，新组仍有2棵树和12个高草风动网格。
- Console error/warning=0。
- 路线代码、BattleYRouteHost、J1地面材质和Shader哈希保持不变；FakeRouteDataTrial未操作。

## 2026-10-14 第二轮密度和篱笆修正

用户回报的4项问题：篱笆仍矮；左篱笆压/架在道路边；树之外两侧空；右官道装饰不足。再次按J1参考图与主相机实际视野调整：

- 10个可见完整/新增篱笆的Y缩放为0.93–0.98，按A03/A04 Alpha可见高度换算约2.44–2.75世界单位。
- 所有篱笆改接新材质`Assets/Experiments/CurvedScroll/Authoring/J1Vegetation/J1_Fence_Oriented.mat`，使用新增`Assets/Experiments/CurvedScroll/Shaders/YOrientedSceneryE0Route.shader`。其投影保留SpriteRenderer作者X轴，使栏杆沿路线切线，而不是像旧景物Shader那样始终水平铺过屏幕。
- 篱笆横向scale 0.86，锚点沿中心线法线外移；先前基于完整Alpha footprint的粗略净空估算是距道路中心约4.48–4.60米，新朝向材质后已重新采样左右分支画面。视觉上路面仍开阔，左石路内缘能看见篱笆并由路肩衔接。
- 两侧新增第二层乔木与低/高草、灌木、碎石，在近中远层形成侧景；右官道增加可见的箱组、较大的旗帜轮廓及运输车展示实例，保留原正式素材，不收费生成。
- 新追加引用后`YScrollSample.scenery=566`。

最终采样位于`Library/Locus/tmp/j1-scenery-refinement-20261014/oriented/`：`J1_190.png`、`approach184.png`、`left202.png`、`right202.png`。验证到路牌可读、左右车道开放，J1位置能看到两侧树群、较高篱笆、右侧箱车旗组；左右转弯没有新植被盖住道路中心。

保存并关闭/重开Y场景后回读：root=(0,0,0)、progress=0、right=false、Edit Mode viewCamera=None正常、dirty=false、Battle未保存；scenery=566、null=0、duplicate=0；11个篱笆均保留定向材质。Console新增error/warning=0。

## 用户验收结论（2026-10-14）

用户在 Game 内目视后明确回复「已验收」，验收范围为：

- J1 中央两棵乔木、两种高草的透明素材与中央遮挡；
- 加高并沿路侧定向的篱笆；
- 左右两侧补充的树、草、灌木、碎石层次；
- 右官道的箱子、旗帜、运输车组合。

这是用户对**可看画面**的验收，不等于完整自然 Play 流程、左右按钮真实点击、战斗/奖励回归已验收；这些仍未执行。未新增收费生图。

## 验收边界

已响应两轮反馈并保存Y场景；没有修改路线代码、地面Shader/材质或Battle场景。Battle.scene 原有 dirty 改动不由本轮保存覆盖。完整自然Play流程、左右选路按钮点击、战斗/奖励回归仍未执行。未新增收费生图。

## 下一阶段：村庄场景部署

### 设计语义（以 `design/changbanpo-six-node-level-design.md` 为准）

```text
J1（190，无战斗选择）
└─ 左路 −45°：绿色石板村落路
   ├─ EV·村落外围（225，无战斗，savePoint=true，唯一出口）
   └─ N3 村落搜救（266.116455，战斗节点，背景语义＝村落边缘空地）
```

- EV 是玩家**实际左转行进后**才抵达的村落外围；N3 是村落搜救战斗。
- 关键约束（§1.1）：**J1 机位尚未进入村庄，左侧只出现篱笆等路侧装饰，不得出现房屋、屋顶、村庄远景、建筑式塔台**。村庄建筑只能在玩家沿左路继续前进后显露。
- 因此村庄建筑**不能**加进 J1 组（`J1 Integrated - Official Road and Stone Lane @ 190`），应新建独立村落组并在 EV/N3 显现。

### 当前现场事实（2026-10-14 只读核对）

- 路线资产：`Assets/RouteData/ChangbanpoScroll/`，节点 `Node_EV`（nodeId=EV，displayName=`EV·村落外围`，无战斗、savePoint、唯一出口 `Choice_EV_N3`）、`Node_N3`（村落搜救战斗）。
- `YScrollSample.nodePresentations` 中：N1/E0/J1 已有 `backgroundAnchor`，而 **EV、N2、N3 的 backgroundAnchor 均为空**；目前没有任何村落景物组绑定到 EV/N3。
- Y 场景中**不存在**村庄景物组。现存 `Left - Valley Encounter`（21 项，隐藏）、`Left branch landmarks`（30 项，隐藏）、`Approach - Valley Road`（482 项，隐藏）、`Junction Rock Barrier and Roadside Density`（32 项，隐藏）都属于被 §1.1 取代的旧“山谷”语义，**不可直接当作村庄交付**；是否个别复用需逐项判断，不要整组启用。
- 项目内目前**没有村庄建筑素材**（无房屋/茅屋/屋顶/村落相关 PNG）。已有 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Architecture/` 的 `CampGate_v1`、`CommandTent_v1`、`Watchtower_v1` 属营地/城塞语义，不是村落民居。
- 可复用的通用自然素材：`Assets/Experiments/CurvedScroll/Art/ValleyProps.png`（`TreeBroad`/`TreePine`/`BushCompact`/`BushWide`/`RockRound`/`RockAngular`/`CliffLow` 等）、`Art/EnvironmentV2/Grass/`、J1 已验收的 `J1/Vegetation/` 树草，以及 J1 的 `J1_Fence_Oriented.mat` 定向篱笆材质。

### 建议执行步骤

1. **先定 EV/N3 的构图与显露节奏**：确认左路转弯后村落何时、以何种体量出现，中央战斗区（N3）保持连续开阔。
2. **确定村庄美术来源**。现有素材里没有民居；两条候选路线需用户决定：
   - 参考已验收的 `C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v3.png`（该版本曾在 J1 左侧绘制村庄建筑，画风已获用户验收，但**几何属于 J1 视角，不能整张搬到 EV**）；
   - 或按 `design/art-style-guide.md` 新出村庄外景参考图并拆件生成，走 `skill/workflows/image-asset-generation.md` 的透明/参考图流程。
   未经用户明确批准，不发起收费生成。
3. **在 Y 层新建独立村落组**（例如 `EV Village Outskirts @ 225`、`N3 Village Rescue @ 266`），按路径采样点 + 横向偏移摆放，不使用任意世界坐标撒点；沿用 `YSampleScenery_E0Route.mat` / `YGrass_E0Route.mat` / `J1_Fence_Oriented.mat` 等既有路线投影材质。
4. **绑定锚点**：把新组登记进 `YScrollSample.scenery`，并为其设置对应 `nodePresentations` 的 `backgroundAnchor`（当前为空）。
5. **导入与贴地**：新 Sprite 沿 PPU 100、Point、Uncompressed、mipmap off、Alpha Is Transparency 基线；按实际 Alpha 底边设 Pivot，回读 `Sprite.pivot`/`Renderer.bounds.min.y` 并在真实 Game 画面确认脚点。
6. **分节保存与采样**：只保存授权 Y 层；按 Left 的 Progress 在 EV(225)、N3(266) 附近取样，并检查中央战斗区无遮挡、道路净空与建筑不悬空。

### 保护边界

- 只编辑 `Assets/Scenes/Battle.scene` 与 Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。
- 不修改 `YScrollSample.Evaluate/Length/JunctionDistance/CurveRadius/turnAngle`、Battle 相机、战斗坐标、地面 Shader/材质。
- 不打开、保存、回退或向 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 部署。
- 不因村庄布景改路线节点距离或 Host 流程；EV→N3 的节点/出口资产当前正确，除 backgroundAnchor 外无需改动。
- 保留用户既有未提交改动，不用 Save All 清理 Battle。

### 相关入口

- `design/changbanpo-six-node-level-design.md` §1.1、§5.6（节点语义、N3 背景语义、J1→EV→N3 分镜）。
- `plan/scroll-node-deployment-correction-20261011.md`（EV/N3 锚点坐标与节点资产现状）。
- `memory/y-junction-scene-authoring-workflow.md`（Y 路线布景与保存规则）。
- `design/art-style-guide.md`、`skill/workflows/image-asset-generation.md`。

并发保护：本项目有大量与布景无关的用户改动，均保留。Y场景diff较大包含新层级、引用追加及Unity序列化重排；比较Hierarchy确认没有删除既有对象，既有标记物仅调整篱笆Transform并移动已授权右侧运输装饰。Battle.scene保留其既有`stabPrefab`/`visualScale`修改，未被本轮保存覆盖。
