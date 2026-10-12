---
id: kd_6379cba2-6190-42dc-b990-6d07c9fb48e4
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 门前过渡层场景完善：交接与下一步

## 1. 交接摘要

> **2026-10-11后续更新：** J1素材已部署，J1节点已更正到190米，旧E1代码/场景占位/引用已移除；E0已接独立流程预览战斗及奖励/唯一出口门控。当前节点部署与Play回归入口为 `plan/scroll-node-deployment-correction-20261011.md`。下文“仅准备、尚未接线、E1保留”等内容为前序任务历史；门垣1.2倍布局继续有效，正式战斗数值与门内深度仍待制作。

用户已验收 E0 门垣构图首版与正式 N1→E0 整合基线。本文件记录后续过渡层完善及用户授权的门垣体量/草排序修复；未新增授权时仍保护已验收布局，不扩展到缺少参考图的E1/J1。

用户已验收此前正式 N1→E0 整合效果，后续完成30件路肩细节及授权的1.2倍门垣/草排序修复（见3.1）。**2026-10-10最新决定：舍弃E1逻辑节点，E0改为战斗节点，下一节点直接为J1。当前任务只梳理改动并准备J1场景参考图，不实施节点/场景/代码迁移，不提交生图。** 新目标为N1（战斗）→E0（战斗，保留ID）→J1（分叉选择），详见 `plan/j1-scene-reference-preparation.md`；设计权威入口见 `design/changbanpo-six-node-level-design.md` 1.1。独立E0源仍保留，参考图→拆素材→搭建工作流继续适用。

尚未完成、不得宣称为已完成的内容：E0正式战斗配置与奖励/唯一出口门控、旧E1停点引用解除、新E0→J1流程与方向接线、门内半3D空间（内墙/拱顶/出口）、J1参考图和官道/村落环境、自然流程、战斗/HUD与分辨率验收。E1独立节点与参考图任务已取消，当前场景中的占位尚未删除或迁移。

## 2. 上一阶段已交付（已验收）

- 源场景：`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity`（保留为独立源与对照，不是正式路线入口）。
- 正式连续承载：`Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- 新门垣组（启用）：`E0 Outer Gate Preview/E0 Approved Gate - Composition v1`，6 个 SpriteRenderer。
- 旧门垣组（隐藏保留作对照）：`E0 Outer Gate Preview/E0 Outer Ruined Gate - Front Assembly`。
- 旧路侧组 `E0 Outer Gate Preview/E0 War Aftermath - Roadside` 保留原有9个对象；其中两块旧墙候选当前隐藏，其余原路侧对象未调整。
- E0源场景历史保存现场：Edit Mode，`dirty=false`，`progress=0`，`scenery=63`，无空项/重复，Console warning/error=0；当前正式Battle+Y现场见第3节。
- 上阶段已验收截图：静止 `Library/Locus/Screenshots/locus_game_20261009_064356_787.png`；接近 distance12 `Library/Locus/Screenshots/locus_game_20261009_065730_005.png`（均 489×979）。
- 本阶段保存重载后的试搭截图（不是美术验收）：静止 `Library/Locus/Screenshots/locus_game_20261009_092311_509.png`；接近 distance12 `Library/Locus/Screenshots/locus_game_20261009_092337_621.png`（均 489×979）。

独立 E0 源场景的历史门垣组参数（相对该组 local 值，组自身原点/单位缩放；本轮源场景未改，正式Y实例新参数见3.1节）：

| 对象 | Local Position | Local Scale | order | flipX |
|---|---|---:|---:|---|
| E0_A01_CentralStoneArch | (0,0,42) | 1.4 | 1 | false |
| E0_A02_LeftFlagSidePier | (-8.15,0,42) | 0.8704 | 2 | false |
| E0_A03_RightCompactPier | (7.62,0,42) | 0.6664 | 2 | false |
| E0_B01_RightLowWall | (15.4,0,42) | 1.2104 | 0 | false |
| E0_B01_LeftLowWall_Reuse | (-14.7,0,42) | 1.2104 | 0 | true |
| E0_Flag_Left_Independent | (-9.18,6.8,42.35) | 0.95 | 1 | false |

详细依据、脚点修正和截图证据见 `plan/e0-approved-gate-composition-handoff.md`。

## 3. 本阶段试搭与正式路线整合结果

- 新建 `E0 Approach Detail - Mid and Near v1`，作为 `E0 Outer Gate Preview` 子节点；包含 Far Shoulder、Mid Shoulder、Near Shoulder、Grass Stitch、Ground Debris 五个分层，共 42 个 SpriteRenderer：17 件木制障碍/辎重/土垒主体、4 件低土堆脚部连接、21 丛草。
- 全部使用现有 Sprite 和材质，无新生图、未改导入设置或共享材质/Shader。对象只用 local 位置和等比缩放，Y=0；近中远两侧交错布置。最内侧非草大型主体边缘在 `|x| >= 3.05`，地面 `_RoadHalfWidth=2.45`；静止和接近 Game 画面均保留中央通道净空。
- 隐藏旧路侧组中的 `E0_Approach_BrokenWall_Left` 与 `E0_Approach_LowRuin_Right`，对象、Transform、Sprite、Renderer 与 scenery 引用仍保留，未删除。
- 独立 E0 源场景未被本轮保存覆盖；本轮只保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。Y 重载后确认 dirty=false、progress=0、scenery=297、空项/重复=0；门垣组及正式E0整合组保持不变，E1占位和J1/分支状态未改。
- 正式 Y 路线采样（489×979，纯场景临时绑定 Battle Main Camera）：N1开场 `Library/Locus/Screenshots/locus_game_20261009_104547_857.png`；E0门外 distance90 `Library/Locus/Screenshots/locus_game_20261009_110253_207.png`；E1锚点 distance156 `Library/Locus/Screenshots/locus_game_20261009_110349_390.png`；J1 distance190 `Library/Locus/Screenshots/locus_game_20261009_110525_638.png`。采样后已恢复 progress=0、viewCamera=None、Game非最大化；截图只作为当前结构证据，E1/J1画面尚非美术验收。
- Unity Console warning/error=0；原共享 Shader/材质哈希恢复不变。正式路线使用独立 `N1_Ground_ThreeLayer_E0Route.mat`、`YSampleScenery_E0Route.mat`、`YGrass_E0Route.mat` 与对应 `*E0Route.shader` 副本，避免污染共享资源。
- 本轮继续工作范围仅为 N1→E0 接近段：新增 `N1 to E0 - Shoulder Detail v1`，共30个既有Sprite（13件低矮残骸/路障、17丛草），分为 Approach Shoulder、E0 Wall Foot、Grass Stitch。位置集中在z=52–88及门垣墙脚z=127–129，使用路线专用材质，中央道路半宽2.45保持净空。原N1/E0/E1/J1及分支组的Transform/Renderer状态与工作前快照一致。
- 采样：distance54 `Library/Locus/Screenshots/locus_game_20261009_133233_860.png`；distance72修改后 `Library/Locus/Screenshots/locus_game_20261009_132744_939.png`；E0门外distance90 `Library/Locus/Screenshots/locus_game_20261009_132856_156.png`；保存重载后distance72 `Library/Locus/Screenshots/locus_game_20261009_135518_187.png`。均为489×979、Battle主相机、Host实际地面高度-1.8的临时纯场景采样，不是完整自然旅行或本轮最终美术验收。
- 最新Y回读dirty=false、progress=0、scenery=297、空项/重复=0；Active为Battle，Additive为Y，HUD/Player/Enemies显隐、Y根位置0、viewCamera=None及Game非最大化均已恢复。Battle当前dirty=true且本轮未保存：本轮开始时双方clean，预览恢复后Battle变dirty，但Unity导出的修复前/后临时副本在规范换行后完全一致，原Battle磁盘哈希未变。保留dirty而不擅自保存/清除。Console warning/error=0，334个保护文件哈希均未变化。
- 最新制作任务转为J1完整场景参考图准备；E1节点已取消，不再制作独立E1图或布景。当前E1占位和旧停点仍属未迁移实现，暂不删除/覆盖。J1参考图获批准生成并验收后才拆素材/部署，门后的物理过渡空间继续归属E0→J1边。完整提示词和影响清单见 `plan/j1-scene-reference-preparation.md`。

### 3.1 2026-10-10 门垣1.2倍与草排序修复（用户已授权，视觉待确认）

- 只修改正式 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，不改独立E0源、Battle文件、路线脚本/参数、PNG/meta、材质或Shader。
- 正式门垣路径：`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/E0 Integrated - Gate and Aftermath @ 90/Gate Facade - Depth Sorted`。新增此局部节点与SortingGroup，锚点处于路线坐标(0,0,132)，外部Sorting Layer=Default、Order=0。原6个建筑/旗SpriteRenderer重设父节点但不重建，scenery引用保持。
- 以立面脚点(0,0,132)为基准，全部Sprite等比尺度及横向/旗高度偏移乘1.2，纵向位置不变；不缩放原父组，避免原local z=42被推远到50.4。

| 正式对象 | 相对Gate Facade的localPosition | localScale（xyz一致） | 组内Order |
|---|---|---:|---:|
| E0_A01_CentralStoneArch | (0,0,0) | 1.68 | 1 |
| E0_A02_LeftFlagSidePier | (-9.78,0,0) | 1.04448 | 2 |
| E0_A03_RightCompactPier | (9.144,0,0) | 0.79968 | 2 |
| E0_B01_RightLowWall | (18.48,0,0) | 1.45248 | 0 |
| E0_B01_LeftLowWall_Reuse | (-17.64,0,0) | 1.45248 | 0 |
| E0_Flag_Left_Independent | (-11.016,8.16,0.35) | 1.14 | 1 |

- 修复 `E0 Integrated - Gate and Aftermath @ 90 - Approach` 与 `N1 to E0 - Shoulder Detail v1`：46个Order3恢复Order0，两个组共72个Renderer全部Default/0，位置/尺寸/颜色/引用无变化。全路线106个已注册草Renderer均为Default/0。原分支4个旧远地标Order-2保持，不扩大到E1/J1调整。
- 门垣子Renderer的0/1/2只用于SortingGroup内拼接，组外按立面锚点深度排序；不再用Order3抬高全部草/道具。天空保持独立；透明材质queue3000未改。
- 截图均489×979、Main Camera、实际groundHeight=-1.8：修复前门外distance90 `Library/Locus/Screenshots/locus_game_20261010_094029_652.png`；修复后 `Library/Locus/Screenshots/locus_game_20261010_095023_252.png`；distance72 `Library/Locus/Screenshots/locus_game_20261010_095924_573.png`；保存重载后的distance90 `Library/Locus/Screenshots/locus_game_20261010_101301_247.png`；distance102 `Library/Locus/Screenshots/locus_game_20261010_101445_151.png`。
- 门外停点左旗完整、门洞保持净空；distance102接近时左旗开始被视口边缘裁切，属于采样中观察到的视觉边界，未修改相机规避。此次没有测试完整自然奖励/旅行、门内半3D或其它分支。
- 保存并单独重载Y后，297项scenery无空项/重复、6个门垣Renderer各注册一次、门垣Z/停点90/156/190/路线长度不变；未修改组与快照一致，334个保护文件哈希不变，Console warn/error0。

## 4. 目标场景与可编辑范围

- 正式运行结构：Active `Assets/Scenes/Battle.scene`，Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`；独立 `E0OuterGatePreview.unity` 仅作为源与对照。
- Root：`E0 Outer Gate Preview`，位置 (0,-1.8,0)，单位缩放、旋转 0。
- 相机：`E0 Preview Camera`，位置 (0,3,-10)，旋转 (18,0,0)，FOV 60；保持原相机。
- 正式 Y `YScrollSample`：progress 0、right false、animate false、turnAngle 45、battleHost None、Length≈266.12；JunctionDistance=190、CurveRadius=28.1595573、scenery=297。E0/E1/J1停点由 Inspector 直接引用三个 `ScrollRouteNodePresentation`，世界距离为90/156/190。本轮不改路线参数或停点。
- 正式 Y 地面使用独立 `Assets/Experiments/CurvedScroll/Authoring/N1_Ground_ThreeLayer_E0Route.mat`；天空 `Skybox Background - editable` 仍使用独立天空材质，不加入 scenery。

## 5. 可复用素材清单（Props/N1 与 Grass）

已核对的基础 `_v1` 图为 **Sprite 类型**，可直接用于部署；`_grounded_v2` 备用图当前是 **Texture(Default) 类型，AssetDatabase 取不到 Sprite**，不能直接当插片使用（如需使用必须另行确认导入设置，且不得擅自改动共享资产）。

| 素材 | 用途 | 尺寸(px) | 现有脚点 pivot |
|---|---|---:|---|
| `Props/N1/N1_StakeCluster_v1.png` | 近景木桩簇 | 275×409 | (137.5,16) |
| `Props/N1/N1_LowChevalDeFrise_v1.png` | 低拒马 | 405×252 | (202.5,16) |
| `Props/N1/N1_PalisadeShort_v1.png` | 短木栅 | 326×377 | (163,16) |
| `Props/N1/N1_FallenLogs_v1.png` | 倒木 | 456×277 | (228,16) |
| `Props/N1/N1_BrokenCart_v1.png` | 破车 | 961×993 | (480.5,170) |
| `Props/N1/N1_BrokenShieldArrows_v1.png` | 散盾断箭 | 406×368 | (203,16) |
| `Props/N1/N1_LowEarthMound_ColorV4.png` | 低土堆 | 1024×1024 | (512,297) |
| `Props/N1/N1_LowEarthMound_SceneRef_v3.png` | 低土堆（场景参考色） | 1024×1024 | 已导入为 Sprite |
| `Props/N1/N1_MidgroundTimberBarricade_v1_clean.png` | 中景木栅 | — | — |
| `Props/N1/N1_MidgroundDistantRuins_v1_clean.png` | 中景远残垣 | — | — |
| `Props/N1/N1_MidgroundEarthenRampart_v1_clean.png` | 中景土垒 | — | — |
| `Props/N1/N1_MidgroundReinforcedMound_v1_clean.png` | 中景加固土丘 | — | — |
| `Grass/GrassSmall_v1.png` | 草簇 | 387×310 | (193.5,0) |
| `Grass/GrassWide_v1.png` | 宽草簇 | 400×284 | (200,0) |
| `Props/N1/N1_GrassMedium_v1.png` | 草簇（草着色器） | 397×256 | (198.5,16) |

- 源 E0 试搭普通插片使用 `YSampleScenery_v2.mat`、草使用 `YGrass.mat`；正式 Y 路线使用 `YSampleScenery_E0Route.mat`、`YGrass_E0Route.mat` 及对应专用 Shader 副本，不污染共享材质/Shader。
- 所有基础 `_v1` 图导入基线：PPU100、Point、Uncompressed、mipmap off、Alpha Is Transparency、maxTextureSize2048。
- 素材文件、GUID 和 meta 在本轮只读检查中未改动；引用现状以最新回读为准。

## 6. 保护边界

- 不改 `Assets/Scenes/Battle.scene` 的 Player、Enemy、Camera、StageController、HUD 和战斗坐标；本次只更新 BattleYRouteHost 的路线状态与时长字段。
- 本次已获用户明确授权调整 `YScrollSample` 路线长度/分叉距离并接入正式 Y 场景；不改 turnAngle 语义、Battle战斗坐标或 FakeRoute 实验场景。
- 不打开、保存、回退 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。
- 不修改共享 Shader/材质来强行遮盖单件素材问题；不为草修改 `YScrollScenery.shader`。
- 不整文件 `git restore`，不 Save All，不自动删除旧试摆、旧路侧组或共享候选。
- 独立源门垣组和四张新素材PNG仍保护。正式Y实例本轮已按用户授权放大1.2倍并分离内部/外部排序，新基线见3.1；后续改变体量、位置、图像或层级需先回读并确认授权。

## 7. 验证规则

- 每个 Sprite 若调整过脚点，按真实主体 Alpha 底边设 Pivot（Custom），**不用 Transform Y 二次补偿**。
- 只用相对父组的 local 值记录新增对象；保持等比缩放，不拉伸。
- 保存后重载回读；在 Game 中检查静止与接近画面，中央通道不被大型物件侵入。
- 侧景不能遮挡敌人、武器、特效和 HUD；近/中/远要有尺度与对比差异。
- 技术导入成功（HTTP/类型/尺寸）不等于美术验收；以用户实际 Game 目视为准。

## 8. 相关文档入口

- 门垣构图交接：`plan/e0-approved-gate-composition-handoff.md`。
- B01 生成与结果：`plan/e0-b01-generation-prompts.md`。
- A03 重制：`plan/e0-a03-remake-prompts.md`。
- 批次 A 生成词：`plan/e0-batch-a-generation-prompts.md`。
- 专项待办：`plan/scroll-scene-current-todolist.md`。
- 最新起始段改动/J1参考图准备：`plan/j1-scene-reference-preparation.md`。
- 素材与方案目录：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/`（含 `References/`、`Architecture/`、`Candidates/`、`Plans/`、`README.md`）。
- 可复用布景流程经验：`memory/scene-art-production-pipeline.md`。
- 源试搭对象名和local参数以 `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Approach Detail - Mid and Near v1` 为准；正式路线对象为 Y 层下 `E0 Integrated - Gate and Aftermath @ 90` 及其 Approach/Roadside 子组。
