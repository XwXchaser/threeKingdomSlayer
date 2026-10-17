---
id: kd_2550c842-e38b-4dbb-9ed4-4bead96610b3
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0→J1 门后路肩部署交接

> **状态：部署已完成（2026-10-16）**。新组已建、10 个对象已复制并登记，`scenery` **685 → 695**、空项 0、重复 0；已保存并关闭重载复核，Console 0/0。
>
> **⚠️ 验收标准第 1 条（「90 米时门洞外能看到低路肩」）无法按字面满足**，且不是本次放置的问题：路线投影 `drop = 0.009×(depth−18)²` 在该距离把所有路肩压到相机下方，整段新路肩都落在视野下缘以下。实测把**已验收的 J1 组**在同样 90 米处开关，可见像素同样是 **0**。详见下方「验证结果」。

## 目标

只填门垣与 J1 已验收路肩之间的空路。门垣立面在世界 z=132，J1 路肩从 z=173 开始；这段启用景物目前为空。E0 仍在 90 米，J1 仍在 190 米，`Travel_E0_J1` 仍是 100 米、3 秒。不缩短路程，不新增逻辑节点。

新组挂在路线根下，不挂进 E0 或 J1：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/E0 to J1 - Beyond Gate Shoulder`

组局部原点为 0。当前磁盘还没有这个组。

## 部署前基线

本文件编写时只读回读：

- `YScrollSample.scenery` 长度 **685**，空项 0；
- `N1 Lamp Flags - Behind Battle` 已存在，本任务不修改；
- `Beyond Gate Shoulder` 尚不存在；
- 只应保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`。

若打开场景后 scenery 不是 685，先停止并报告，不要按本表覆盖别人的新引用。

## 复制来源

用 Unity API 复制下列现有对象，再改名、位置和等比正缩放。复制后显式沿用源对象的 `sharedMaterial`，不要产生材质实例，也不要改成 `Sprites/Default`。所有 Scale 分量保持正数；源对象已有的 `flipX` 原样保留，不要用负 X Scale 转向。

| 新对象 | 复制来源 | Local Position | Scale |
|---|---|---|---|
| E0J1_Logs_Left | `N1 to E0 - Shoulder Detail v1/Approach Shoulder/N1E0_Logs_Left_01` | (-5.4, 0, 140) | 0.34 |
| E0J1_Stake_Right | 同组 `N1E0_Stake_Left_03` | (5.8, 0, 146) | 0.30 |
| E0J1_Palisade_Left | `Opening Roadside Props/N1_PalisadeShort_Left_01` | (-6.2, 0, 152) | 0.32 |
| E0J1_Cheval_Right | `Approach Shoulder/N1E0_Cheval_Right_04` | (5.2, 0, 158) | 0.28 |
| E0J1_Earth_Left | `N1 to E0 - Shoulder Detail v1/E0 Wall Foot/E0_WallFoot_LowEarth_Left` | (-5.6, 0, 164) | 0.26 |
| E0J1_Timber_Right | `E0 Integrated - Gate and Aftermath @ 90 - Approach/Far Shoulder/E0_Far_TimberBarricade_Left` | (6.4, 0, 168) | 0.24 |
| E0J1_Grass_Left_143 | `E0 Integrated - Gate and Aftermath @ 90 - Approach/Grass Stitch/E0_Grass_01_LeftFront` | (-3.9, 0, 143) | 0.22 |
| E0J1_Grass_Right_151 | 同上 | (3.9, 0, 151) | 0.20 |
| E0J1_Grass_Left_159 | 同上 | (-4.1, 0, 159) | 0.18 |
| E0J1_Grass_Right_166 | 同上 | (4.1, 0, 166) | 0.16 |

前 6 件因此使用 `YSampleScenery_E0Route`；4 丛草使用源草的 `YGrass_E0Route`。Sorting 全部保持 Default / 0。10 个新 Renderer 追加到 scenery 末尾，结果应为 **695**，空项 0、重复 0。

这些 Y 都是 0。路线 Shader 会和地面一起下压，不要像 N1 路灯旗那样另加抬高。

## 禁止

- 不移动、复制或加密 J1 从 z=173 起的对象，包括 `J1 Approach - Low Shoulder Detail`。
- 不改门垣、N1 旗帜、破车、路线长度、`JunctionDistance`、旅行配置、Battle 相机和战斗坐标。
- 不启用 N1 灯旗组里的停用对象。
- 不新增房屋、第二座门、炊烟或路灯旗。
- 不改 Shader、共享材质资产或天空背景。
- 部署中途不要调用 `Tools/Curved Scroll/Open Battle Host`。

## 验收与保存

加载 Battle，并 Additive 加载 Y。临时把 `viewCamera` 绑到 Battle Main Camera，路线根 Y 设为 -1.8，分别在路径距离 90、140、160、190，以及右路 236 渲染。结束恢复 progress、right、viewCamera、根高度、RenderTexture。

通过标准：

- 90 米时门洞外能看到低路肩，不挡门洞；
- 140–168 米时左右物体分开，中央 `|x| < 2.45` 没有新物体；
- 190 米的已验收 J1 构图不变，因为新物体已在镜头后方约 22 米，应被淡出；
- 右路 236 不露出这组直道物体；若露出，只停用或外移冲突的那一件，不改整组 Z。

只保存 Y 场景，关闭重载后再确认 10 个对象的位置、正缩放、材质和 scenery=695。Console warning/error 应为 0。Battle 与 MainMenu 不保存。

## 执行结果（2026-10-16）

创建组 `E0 to J1 - Beyond Gate Shoulder`（挂在路线根下，局部原点 0、单位旋转缩放，非 Prefab 实例），10 个对象全部用 `Object.Instantiate` 从指定源复制，显式沿用源的 `sharedMaterial`（未产生材质实例，未改成 `Sprites/Default`），源 `flipX` 原样保留：

| 对象 | Local Position | Scale | flipX | 材质 |
|---|---|---|---|---|
| E0J1_Logs_Left | (-5.4, 0, 140) | 0.34 | false | YSampleScenery_E0Route |
| E0J1_Stake_Right | (5.8, 0, 146) | 0.30 | true | YSampleScenery_E0Route |
| E0J1_Palisade_Left | (-6.2, 0, 152) | 0.32 | false | YSampleScenery_E0Route |
| E0J1_Cheval_Right | (5.2, 0, 158) | 0.28 | true | YSampleScenery_E0Route |
| E0J1_Earth_Left | (-5.6, 0, 164) | 0.26 | false | YSampleScenery_E0Route |
| E0J1_Timber_Right | (6.4, 0, 168) | 0.24 | false | YSampleScenery_E0Route |
| E0J1_Grass_Left_143 | (-3.9, 0, 143) | 0.22 | false | YGrass_E0Route |
| E0J1_Grass_Right_151 | (3.9, 0, 151) | 0.20 | false | YGrass_E0Route |
| E0J1_Grass_Left_159 | (-4.1, 0, 159) | 0.18 | false | YGrass_E0Route |
| E0J1_Grass_Right_166 | (4.1, 0, 166) | 0.16 | false | YGrass_E0Route |

- 全部等比**正缩放**、Sorting = Default/0；新 10 个 Renderer 追加到 `scenery` 末尾（索引 686–695），总数 **695**、空项 0、重复 0；
- 每个对象 2 个组件（Transform + SpriteRenderer），无多余子节点；
- 关闭重载复核：位置/缩放/材质/flipX/scenery 均与内存一致，6 个剩余 N1 旗仍注册；
- 磁盘回读：6 件用 `YSampleScenery_E0Route`（guid `cfe9bb7b…`），4 丛草用 `YGrass_E0Route`（guid `1a8212c7…`）；
- Console `0/0`；Battle 与 MainMenu 未保存。

守卫项全部未变：门垣世界 z=132、J1 从 173 起、N1 旗组 6 个、两辆破车坐标、路线 `Length=266.117`、`JunctionDistance=190`、`CurveRadius=28.15956`、`ExitTail=54`。

### 验收矩阵实测

以亮度开关对照（组 OFF vs ON，阈值 ≥8）逐距离测量本组可见像素：

| 路径距离 | 本组可见像素 | 备注 |
|---|---|---|
| 90 m | **0** | 整组在视野下缘以下（见下） |
| 100 m | **0** | 同上 |
| 110 m | 2 | 刚开始进入 |
| 120 m | 415 | |
| 130 m | 2996 | 最清晰（门洞穿过视角） |
| 140 m | 2878 | 左右分开，中央无侵入 |
| 160 m | 340 | 接近淡出 |
| 190 m | **0** | 已在镜头后方，符合预期 |
| 236 m（右路） | **0** | 未泄露直道物体，无需停用任何一件 |

- **中央侵入检查**：10 件世界 X 范围最窄为 ±3.46（`E0J1_Grass_Left_143`），均未进入 `|x| < 2.45` 的道路带；
- **190 m 已验收 J1 构图未变**：本组在此距离贡献 0 像素，与交接预期一致；
- **右路 236 m 未泄露**：在 200/210/220/236/240/250 m 六个点测量均为 0 像素；
- **90 m 门洞**：本组未遮挡门洞（本身就不可见）；但同样在 90 m 处开关**已验收的 J1 组**，可见像素也为 **0**。

### 为什么 90 m 看不到路肩（机制，非放置错误）

投影下压量 `drop = 0.009 × (depth − 18)²`（`depth` = 物体 Z − 行进 Z）。在 90 m 处，新物体 depth 为 50–78：

| depth | drop |
|---|---|
| 50 | 9.22 |
| 60 | 15.88 |
| 78 | 32.40 |

而新物体本身只有 0.4–1.3 世界单位高。即地面与路肩一起被下压 9–32 个单位，塌到相机视锥以下——这是路线 Shader 设计使然，不是本次摆放失误。**把新物体抬高或改 Z 会同时破坏“随地面一起下压”这一要求**，因此本轮不做补偿。

若产品上确实需要在 90 m 看到门后路肩，那是**投影曲线/相机取景的既有问题**（影响全部路肩，包括已验收的 J1），需单独讨论，不在本交接授权范围内。
