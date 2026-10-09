---
id: kd_f705dc23-d939-4ccb-8e83-8a13673c7188
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 已验收素材门垣构图：首版试搭交接

## 1. 用户任务与完成边界

用户已验收B01，并要求先用当前素材搭建E0，观察能否确立城墙构图语言。任务因网络中断后继续，首先核对现场没有未保存或并发改动。

已完成：在现有 `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity` 内新增已验收素材的门垣构图组，修正新Sprite可见脚点，保留旧试摆作对照，保存并重载后检查静止和手动distance12的实际Game画面。未新生图，未修改Battle、Y路线、共享Shader/材质或道路Mesh。

这是一版**独立E0城墙语言试搭**，不是正式N1→E0→E1接线、完整穿门空间或最终审美验收。当前素材足以说明石拱/石土墙身/左右高低/左旗/低连接墙的关系；门内深度、外侧断墙变化、独立瓦砾与焦木仍属后续内容。

**用户已确认“远景效果可以”，门垣构图作为已验收基线，并授权进入门前过渡层场景完善。** 下一阶段入口：`plan/e0-approach-scene-refinement-handoff.md`；本节表格中的门垣组参数视为基线，后续微调前先回读并与用户确认。

## 2. 保存现场

- Edit Mode，仅加载并激活E0OuterGatePreview，dirty=false。
- Root位置=(0,-1.8,0)，单位缩放、旋转0；地面/天空未改。
- `YScrollSample`：viewCamera=同场景E0 Preview Camera，progress=0，right=false，animate=false，duration=10，turnAngle=45，battleHost=None，Length=160。
- Camera位置(0,3,-10)，旋转(18,0,0)，FOV60，保持原相机；实际放大Game检查为489×979、18:9 Portrait。
- 原Game视口maximize=false，检查期间暂时放大，结束已恢复false。
- `scenery=21`，无空项/重复；当前启用且Renderer enabled的scenery=16（其中5个旧门垣Renderer所在组隐藏）。天空不在scenery。
- 本次Console warning/error回读为0。
- 只保存E0，不Save All，不提交/推送Git。

保存E0：63146 bytes，SHA-256=`cec0964c0d37f45e50eb147403aa28aac3a732fce9a7912d6c1cdbc6ee1ea88c`。这是本次收尾记录，后续合法修改以最新现场为准，不据此还原覆盖。

## 3. 新布局及可切换旧组

新组（active=true）：

`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Approved Gate - Composition v1`

旧组（active=false，但对象、Transform、Sprite、材质和原scenery引用完整保留）：

`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Outer Ruined Gate - Front Assembly`

两组互斥查看，勿同时开启导致双门垣叠加。旧路侧组 `E0 War Aftermath - Roadside` 未改位置或内容，目前复用其破车、散盾、草和远处候选小墙；近距离截图会更明显看到旧路侧小墙，后续若需统一美术先回读，不自动删除。

新组所有值都是相对该组的local值，组自身局部原点/单位缩放。各建筑保持等比缩放，不拉伸已验收Sprite；排序只用于门墩覆盖低墙和旗底座连接。

| 对象 | Local Position | Local Scale（xyz一致） | order | flipX |
|---|---|---:|---:|---|
| E0_A01_CentralStoneArch | (0,0,42) | 1.4 | 1 | false |
| E0_A02_LeftFlagSidePier | (-8.15,0,42) | 0.8704 | 2 | false |
| E0_A03_RightCompactPier | (7.62,0,42) | 0.6664 | 2 | false |
| E0_B01_RightLowWall | (15.4,0,42) | 1.2104 | 0 | false |
| E0_B01_LeftLowWall_Reuse | (-14.7,0,42) | 1.2104 | 0 | true |
| E0_Flag_Left_Independent | (-9.18,6.8,42.35) | 0.95 | 1 | false |

- A01/A02/A03引用 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Architecture/` 对应验收PNG。
- B01保持现有 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Candidates/e0_b01_low_wall_right_v1.png` 路径，不移动或重建GUID。
- 左侧连接墙是B01镜像复用的构图试验，不称为已生成独立左连接墙。左、右高墙墩本身不是镜像：A02宽残墙、A03紧凑竖向墙墩。
- 旗复用 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_SmallBrokenFlag_v1.png`，低底座由左墙墩遮挡连接；不新生图。
- 新组共6个SpriteRenderer，均使用既有 `Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat`。共享材质和Shader未改。

## 4. Sprite脚点修正

本次先检查原始PNG Alpha，再修正新资产Importer，未修改PNG字节、PPU、GUID或已有旧候选。

| 新资产 | 实际Sprite.pivot（px） | 依据 |
|---|---|---|
| e0_a01_stone_arch_front_v1.png | (512,282) | 明显主体Alpha≥32 bbox底边742，画布1024 |
| e0_a02_ruined_pier_left_v1.png | (512,68) | 明显主体底边956 |
| e0_a03_ruined_pier_right_v2.png | (512,45) | 明显主体底边979 |
| e0_b01_low_wall_right_v1.png | (512,318) | 主体底边706，原Pivot已正确 |

- Alpha1–7等远处生成残留没有被当作脚点；也未全图阈值化删除正常边缘。
- A01/A02/A03之前归档时Pivot在画布底边0，会因透明留白而悬浮。本次改为真实主体脚点，不用Transform Y叠加补偿。
- 使用Single/Custom Pivot、PPU100、Point、Uncompressed、mipmap off、Alpha Is Transparency、FullRect等既有环境规格。
- PNG低Alpha残留仍未清理，现为构图预览；最终成品清理须另存可验证副本，不能偷偷覆盖原图。

## 5. 截图证据

| 文件 | 结果/限制 |
|---|---|
| `Library/Locus/Screenshots/locus_game_20261009_055629_987.png` | 旧试摆基线，253×505，没有中央石拱 |
| `Library/Locus/Screenshots/locus_game_20261009_061641_144.png` | 新组初稿偏大、左旗被边缘裁掉，不作最终验收 |
| `Library/Locus/Screenshots/locus_game_20261009_063942_459.png` | 最终保存重载后的Edit Game，489×979 |
| `Library/Locus/Screenshots/locus_game_20261009_064356_787.png` | 最终Play静止progress0，与保存布局一致，489×979 |
| `Library/Locus/Screenshots/locus_game_20261009_065730_005.png` | Play手动distance12、progress0.075接近采样，489×979；不是自动自然旅行 |

检查观察：中央石拱入口可读，左旗完整，左右高低和低墙形成连续门垣，静止/接近采样中没有明显堵住道路。门后仍直接看到远山背景，没有真实内墙/出口遮挡结构，不称为穿门完成。不同Sprite的轮廓与材质关系已由用户确认“远景效果可以”，且不以脚点数值代替审美验收；本次构图作为已验收远景基线。

临时Play实验结束已恢复progress0，并退出到Edit；未把distance12保存为基线。放大Game窗口后已恢复原maximize=false。

## 6. 保护文件验证

下列文件相对于本轮开始哈希未变化：

- `Assets/Scenes/Battle.scene`（原有Git diff仍在）
- `Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`（未加载/保存）
- `Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs`
- `Assets/Experiments/CurvedScroll/Shaders/YScrollScenery.shader`
- `Assets/Experiments/CurvedScroll/Authoring/N1_Ground_ThreeLayer_v1.mat`
- `Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat`
- `Assets/Experiments/CurvedScroll/Authoring/N1_FarBackground_v1.mat`
- `Assets/Experiments/CurvedScroll/Authoring/YSampleTerrain.asset`
- A01/A02/A03/B01四张PNG原始字节

本次变更为E0场景、新三件Importer Pivot、知识/目录说明的状态更新。后续保持GUID和meta，不整文件restore、不自动删除旧试摆。

## 7. 后续验收与工作顺序

1. 用户先看当前静止和接近图，判断城墙体量、左右高低、石拱宽度、左旗和外侧连接墙的关系；当前用户仅授权试搭，不自动继续付费生图。
2. 如果构图语言通过，再决定补独立左连接墙、墙根瓦砾、墙顶断木与路侧密度。避免复用结果迫使参考图改变。
3. 门内深度与E0→E1接线仍是独立任务，必须明确定义内墙/拱顶/出口，不把本次正面Sprite组宣称为完整通道系统。
4. 正式战斗/HUD、分辨率、旅行时长/暂停恢复及路线自然流程尚未验证，本次证据只是独立E0预览。
