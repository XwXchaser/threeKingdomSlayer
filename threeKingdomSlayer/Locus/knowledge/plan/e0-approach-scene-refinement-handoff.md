---
id: kd_6379cba2-6190-42dc-b990-6d07c9fb48e4
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 门前过渡层场景完善：交接与下一步

## 1. 交接摘要

用户已验收 E0 门垣构图首版，并明确“远景效果可以，可以进行下一步场景完善工作”。本文件是下一阶段的入口：在**不改动已验收门垣组**的前提下，完善门前两侧的近中景过渡层（木栅、拒马、破车、散盾、草簇、土堆等），建立从远到近连续可读的战场侧带。

本阶段已完成两步：先完成独立 E0 门前侧带试搭，随后按用户确认将门垣与侧带整合进正式 `Battle.scene` + Additive `YJunctionSample.unity` 连续路线。当前正式环境顺序为 N1 尾段 → E0 门外 → E1 锚点 → J1 分岔；独立 E0 场景仍保留作源与对照，不再承担正式路线入口。E0 门外静止 Game 采样已检查，**构图仍待用户目视美术验收**。

尚未完成、不得宣称为已完成的内容：门内半3D空间（内墙/拱顶/出口）、E1正式集结美术、J1官道/村落两侧环境、完整自然奖励→确认→旅行流程、战斗/HUD与分辨率验收、最终美术验收。

## 2. 上一阶段已交付（已验收）

- 源场景：`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity`（保留为独立源与对照，不是正式路线入口）。
- 正式连续承载：`Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- 新门垣组（启用）：`E0 Outer Gate Preview/E0 Approved Gate - Composition v1`，6 个 SpriteRenderer。
- 旧门垣组（隐藏保留作对照）：`E0 Outer Gate Preview/E0 Outer Ruined Gate - Front Assembly`。
- 旧路侧组 `E0 Outer Gate Preview/E0 War Aftermath - Roadside` 保留原有9个对象；其中两块旧墙候选当前隐藏，其余原路侧对象未调整。
- 保存现场：Edit Mode，仅加载 E0，`dirty=false`，`progress=0`，`scenery=63`，无空项/重复，Console warning/error=0。
- 上阶段已验收截图：静止 `Library/Locus/Screenshots/locus_game_20261009_064356_787.png`；接近 distance12 `Library/Locus/Screenshots/locus_game_20261009_065730_005.png`（均 489×979）。
- 本阶段保存重载后的试搭截图（不是美术验收）：静止 `Library/Locus/Screenshots/locus_game_20261009_092311_509.png`；接近 distance12 `Library/Locus/Screenshots/locus_game_20261009_092337_621.png`（均 489×979）。

门垣组对象参数（相对该组 local 值，组自身原点/单位缩放）：

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
- 独立 E0 源场景未被本次整合保存覆盖；正式保存目标为 `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 与 `Assets/Scenes/Battle.scene`。Y 重载后确认 dirty=false、progress=0、scenery=267、空项/展示敌人引用=0；门垣组6个Renderer及参数保持不变，独立源场景磁盘哈希保持不变。
- 正式 Y 路线采样（489×979，纯场景临时绑定 Battle Main Camera）：N1开场 `Library/Locus/Screenshots/locus_game_20261009_104547_857.png`；E0门外 distance90 `Library/Locus/Screenshots/locus_game_20261009_110253_207.png`；E1锚点 distance156 `Library/Locus/Screenshots/locus_game_20261009_110349_390.png`；J1 distance190 `Library/Locus/Screenshots/locus_game_20261009_110525_638.png`。采样后已恢复 progress=0、viewCamera=None、Game非最大化；截图只作为当前结构证据，E1/J1画面尚非美术验收。
- Unity Console warning/error=0；原共享 Shader/材质哈希恢复不变。正式路线使用独立 `N1_Ground_ThreeLayer_E0Route.mat`、`YSampleScenery_E0Route.mat`、`YGrass_E0Route.mat` 与对应 `*E0Route.shader` 副本，避免污染共享资源。
- 下一步请用户目视判断 N1→E0 的显露节奏、门垣体量、两侧密度、破车/拒马组合，以及 E1/J1 是否需要专用环境素材；当前 E1 仍复用少量 E0 战后物件作占位，J1分岔组已后移但还需补官道/村落语义。

## 4. 目标场景与可编辑范围

- 正式运行结构：Active `Assets/Scenes/Battle.scene`，Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`；独立 `E0OuterGatePreview.unity` 仅作为源与对照。
- Root：`E0 Outer Gate Preview`，位置 (0,-1.8,0)，单位缩放、旋转 0。
- 相机：`E0 Preview Camera`，位置 (0,3,-10)，旋转 (18,0,0)，FOV 60；保持原相机。
- 正式 Y `YScrollSample`：progress 0、right false、animate false、turnAngle 45、battleHost None、Length≈266.12；JunctionDistance=190、CurveRadius=28.1595573、scenery=267。E0/E1/J1停点由 Inspector 直接引用三个 `ScrollRouteNodePresentation`，世界距离为90/156/190。
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
- 已验收门垣组和四张新素材 PNG 视为基线；如需改动先回读并与用户确认。

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
- 素材与方案目录：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/`（含 `References/`、`Architecture/`、`Candidates/`、`Plans/`、`README.md`）。
- 可复用布景流程经验：`memory/scene-art-production-pipeline.md`。
- 源试搭对象名和local参数以 `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Approach Detail - Mid and Near v1` 为准；正式路线对象为 Y 层下 `E0 Integrated - Gate and Aftermath @ 90` 及其 Approach/Roadside 子组。
