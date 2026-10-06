---
id: kd_f0d90b75-0a40-4f40-a46e-7346240c69f7
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 长坂坡 N1 场景美术生产交接

> 本文是后续对话继续 N1 场景搭建的直接入口。最新侧带布局与保存回读结果见第 12 节。
> 当前目标是：在不改变卷轴运动和 Battle 战斗逻辑的前提下，继续完善 N1「当阳乱军」的连续 2.5D 场景美术。
> 不要把当前测试 Host 当成正式 Stage01 路线，也不要把当前截图描述为整关场景最终验收。

## 1. 新对话启动顺序

开始任何后续工作前按顺序执行：

1. 读取本文件；
2. 读取 `Locus/knowledge/design/art-style-guide.md`；
3. 读取 `Locus/knowledge/design/curved-scroll-travel-experiment.md`；
4. 读取 `Locus/knowledge/memory/y-junction-scene-authoring-workflow.md`；
5. 读取 `Locus/knowledge/plan/curved-scroll-development-handoff.md` 第 14 节及当前最新段落；
6. 确认 Active Scene=`Assets/Scenes/Battle.scene`；
7. 确认 Additive=`Assets/Experiments/CurvedScroll/YJunctionSample.unity`；
8. 确认 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 未加载；
9. 读取 `git status --short --branch`，保留现有脏工作区，不做全仓库回退；
10. 读取 Unity Console error/warning；
11. 检查 Battle 与 Y 场景 dirty 状态；
12. 检查 `YScrollSample.viewCamera` 是否为 None。Edit Mode 下为 None 是正常的，Play 时由 `BattleYRouteHost` 绑定 Battle Main Camera。

## 2. 当前正式边界

正式测试结构：

```text
Assets/Scenes/Battle.scene
└─ Battle Y Route Host
   └─ Additive Assets/Experiments/CurvedScroll/YJunctionSample.unity
```

### Battle.scene 负责

- Player、Enemy、StageController、对象池、HUD、Battle Camera；
- 真实战斗、波次、QTE、投射物、奖励；
- `BattleYRouteHost` 流程。

### YJunctionSample.unity 负责

- Y 形道路与三层地面；
- 远景背景；
- N1 新画风路侧和中景 Sprite；
- 路线层的真实 Hierarchy 对象、脚点、排序和布局。

禁止：

- 移动 Battle.scene 的 Player、Enemy、Camera、StageController、QTE 或投射物；
- 修改 `YScrollSample.Evaluate()`、`Length`、junction、radius、turnAngle 或路线参数；
- 把真实 Enemy、对象池或第二套战斗管理器放进 Y 场景；
- 打开、保存、回退或向 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity` 部署任何东西；
- 用 `visualYOffset` 批量掩盖环境脚点错误；
- 用整文件 Git restore 覆盖用户改动。

## 3. 当前测试 Host 与正式 N1 的区别

当前 Battle Host 是测试宿主，不是正式 Stage01 路线运行器。

当前已验证配置：

```text
BattleYRouteHost.routeSceneName
= Assets/Experiments/CurvedScroll/YJunctionSample.unity

BattleYRouteHost.openingBattleConfig
= Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset

BattleYRouteHost.leftBattleConfig
= Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset

BattleYRouteHost.rightBattleConfig
= Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset

BattleYRouteHost.useSampleBattle = true
BattleYRouteHost.driveRouteInPlay = false
BattleYRouteHost.openingDistance = 0
```

`N1_ArtPreview_Battle.asset` 是本轮为了观察新画风而创建的独立预览资产：

```text
Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset
```

其测试阵容为：

```text
1011 / 空 / 1011 / 空 / 1011
```

正式 N1 资产仍然是：

```text
Assets/RouteData/Stage01/Node_N1.asset
Assets/RouteData/Stage01/N1_Battle.asset
```

本轮没有修改正式 `N1_Battle.asset` 的波次数据。不要把 `N1_ArtPreview_Battle` 描述为正式 Node_N1 流程。

## 4. 当前已部署的 N1 场景结果

### 4.1 道路材质

独立 N1 三层材质：

```text
Assets/Experiments/CurvedScroll/Authoring/N1_Ground_ThreeLayer_v1.mat
```

引用：

```text
_Road    = Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/N1_Road_v1.png
_Shoulder = Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/N1_Shoulder_v1.png
_Outer   = Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/N1_OuterGround_v1.png
```

参数保留当前道路几何基线：

```text
_RoadHalfWidth = 2.45
_ShoulderWidth = 1.5
_RoadBlend = 1.1
_OuterBlend = 1.3
```

当前道路使用 `CurvedScroll/Y Ground Three Layer Candidate`，没有修改卷轴参数和道路 Mesh。

### 4.2 远景

当前 Y 场景使用无城墙远景：

```text
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/N1_FarBackground_v1.png
Assets/Experiments/CurvedScroll/Authoring/N1_FarBackground_v1.mat
```

Sky 对象：

```text
Y Junction - select for preview/Skybox Background - editable
```

职责：

- 天空；
- 云；
- 远山；
- 黄土远雾。

当前已明确撤销“战斗时城墙远景 + 后续同城墙穿越”的特殊目标。旧城墙对象保留但禁用：

```text
N1 Distant City Wall Landmark
```

不要把 `N1_FarBackground_WithCityWall_v1` 或城墙 Sprite 重新接回当前 N1，除非用户重新明确授权该方向。

### 4.3 N1 插片和中景

N1 素材目录：

```text
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/
```

当前类别包括：

- `N1_StakeCluster_v1.png`；
- `N1_PalisadeShort_v1.png`；
- `N1_SmallBrokenFlag_v1.png`；
- `N1_LowChevalDeFrise_v1.png`；
- `N1_BrokenCart_v1.png`；
- `N1_GrassMedium_v1.png`；
- `N1_MidgroundEarthenRampart_v1_clean.png`；
- `N1_MidgroundTimberBarricade_v1_clean.png`；
- `N1_MidgroundReinforcedMound_v1_clean.png`；
- `N1_MidgroundDistantRuins_v1_clean.png`。

当前普通竖直/中景插片使用：

```text
Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat
```

当前草簇使用：

```text
Assets/Experiments/CurvedScroll/Authoring/YGrass.mat
```

当前 `YScrollSample.scenery` 回读数量：

```text
42
```

其中包含 Terrain、N1 近景插片、N1 中景侧带和相关可见对象；天空不应该加入 `scenery`。

## 5. 当前已确认的场景目标

N1「当阳乱军」设计语义：

- 开阔黄土战场；
- 远处低矮山脉和战场轮廓；
- 少量烟尘；
- 边缘破车、断旗、木制军械；
- 中央和左右保持连续战斗空间；
- 不画窄路；
- 不用环境暗示玩家正在狭窄通道内移动。

当前新画风的场景层次目标：

```text
远景：天空、云、远山、低对比黄土雾
中景：两侧低矮残墙/土坡/木土防线/远处残垣
近景：草簇、木桩、破旗、拒马、辎车
地面：中央道路、肩部、外侧地面
```

当前最重要的验收标准不是“物件数量”，而是：

- 两侧形成有设计的战场侧带；
- 中央道路和战斗区保持清楚；
- 中景不变成一排独立告示牌；
- 近景、中景、远景之间有尺度和对比差异；
- 所有插片在真实 Game 画面中脚点贴地；
- 不出现明显悬空或深度错误。

## 6. 本轮已经修复的错误经验

### 6.1 不要把概念图当作素材拆分源

概念图只用来确认：

- N1 会出现什么物体；
- 战场氛围；
- 空间层次；
- 叙事感觉。

它不直接决定：

- Sprite 画布尺寸；
- PPU；
- Pivot；
- Alpha 边界；
- Unity 世界尺寸；
- 物件实际部署位置。

技术规格必须来自当前已部署 EnvironmentV2 资产，画风来自新画风指南和 1011。

### 6.2 不要用“整张背景图叠城墙”解决可穿越建筑

失败原因：

- `YSkyBackground.shader` 是屏幕空间背景，不知道路线距离和地面高度；
- 背景图里的城墙没有世界坐标，不能自然到达或穿越；
- 把城墙作为普通 Sprite 又可能与下弯地面脱节；
- 合成图只能表达图像位置，不能证明空间接地。

当前明确撤销城墙穿越目标。城墙候选可以保留，但暂不部署。

### 6.3 不要用 Renderer.bounds.min.y 叠加 Transform Y 补偿

错误过程：

```text
先按 Alpha bbox 设置 Pivot
→ 再看 Renderer.bounds.min.y
→ 再把 Transform 向上抬
→ 运行时 Y Shader 再次投影
→ 可见像素悬浮
```

正确规则：

1. 先按真实 Alpha bbox 设置 Pivot；
2. Transform 局部 Y 保持路线父层地面基线；
3. 让 `Y Sample Scenery` / `Y Grass` Shader 处理运行时投影；
4. 用 Game 画面检查可见脚点；
5. Edit Mode 未绑定相机时的 bounds 不可单独作为最终验收。

### 6.4 Y 场景中的旧美术清理是可逆禁用，不是删除

旧树、旧岩石、旧营地、旧展示敌人等已经从运行时 `scenery` 中移除/禁用，但共享资产没有删除。后续若要重新启用，必须先确认是否仍符合新画风，不能自动恢复整组旧对象。

### 6.5 `SpriteRenderer` 的 `[PerRendererData] _MainTex` 会覆盖材质纹理

天空 Shader 和 SpriteRenderer 都使用 `_MainTex` 时，只替换 Material 的 `_MainTex` 不一定改变实际屏幕采样；必须同时回读：

- SpriteRenderer.sprite；
- SpriteRenderer.sprite.texture；
- Material._MainTex；
- Shader；
- Renderer 是否可见。

这次城墙背景误判就是因为材质换了，但 SpriteRenderer 仍引用旧 Sprite。

## 7. 当前未完成项

以下内容不要描述为已完成：

- 正式 `Node_N1` 流程接入；
- `Stage01` 正式路线与 Y 场景的完整绑定；
- 正式 N1→E0 连续旅行；
- 真实 N1 场景与概念图的最终整体美术验收；
- 中景侧带与远景战场结构的最终密度验收；
- 左右分支自然 Play 全流程；
- 奖励、路线选择、暂停、重开和存档回归；
- 城门接近/穿越系统。

## 8. 下一次继续工作的建议顺序

### P0：只做当前 N1 场景视觉收尾

1. 保持当前无城墙远景；
2. 复核远景、中景、近景三层在 Game 的比例；
3. 只调整现有中景侧带的横向位置、缩放和 z 节奏；
4. 不再生成更多小物件，除非 Game 截图证明某类职责缺失；
5. 对每次调整只保存 Y 场景，Battle 不保存；
6. 每次调整后回读 `scenery` 数量、材质、对象 active 和 Console。

### P1：补充可复用中景模块

只有在当前侧带仍然不足时，才制作新素材。优先：

- 更短的低矮土堤模块；
- 更短的木土防线模块；
- 低对比远处残垣模块。

生成约束：

- 透明 RGBA；
- 单元内至少 48px 安全边界；
- 横向结构不能触碰左右边界；
- 不生成整张场景图；
- 每件可独立 Hierarchy 部署；
- 真实 Alpha bbox 和底部脚点必须验收。

### P2：再考虑路线连续性

当前 N1 场景视觉样板稳定后，才开始：

- N1→E0 的战后余烬环境段；
- 撤退辎车、残旗、烟尘事件；
- N1 战斗构图到转场起点的连续关系。

不要先做城门穿越，也不要同时扩展 N2–N6。

## 9. 每次修改的验收门槛

- [ ] Active Scene=`Assets/Scenes/Battle.scene`；
- [ ] Y Additive 已加载；
- [ ] FakeRoute 未加载；
- [ ] Battle/Y dirty 状态已记录；
- [ ] Console error/warning=0；
- [ ] 不存在运行时生成并保存的临时对象；
- [ ] 新 Sprite 的真实 Alpha bbox、Pivot、PPU、Filter、Compression 已回读；
- [ ] Game 画面确认脚点，不只看 Inspector/bounds；
- [ ] 中央战斗区无遮挡；
- [ ] 远景不加入普通 `scenery`；
- [ ] 退出 Play 后临时 Host/UI/相机状态已恢复；
- [ ] 正式 N1 配置没有被测试预览修改。

## 10. 当前项目引用入口

- 新画风指南：`Locus/knowledge/design/art-style-guide.md`
- 卷轴设计与错误经验：`Locus/knowledge/design/curved-scroll-travel-experiment.md`
- Y 场景操作规则：`Locus/knowledge/memory/y-junction-scene-authoring-workflow.md`
- 当前长期交接：`Locus/knowledge/plan/curved-scroll-development-handoff.md`
- 长坂坡关卡设计：`Locus/knowledge/design/changbanpo-six-node-level-design.md`
- 当前工作场景：`Assets/Scenes/Battle.scene`
- 当前 Additive 场景：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`
- 当前道路材质：`Assets/Experiments/CurvedScroll/Authoring/N1_Ground_ThreeLayer_v1.mat`
- 当前 N1 远景材质：`Assets/Experiments/CurvedScroll/Authoring/N1_FarBackground_v1.mat`
- 当前 N1 预览战斗：`Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`

## 11. 当前 Git 状态提醒

当前工作区存在大量既有未提交改动，包括 Battle 场景、Y 场景、战斗层文件、N1 资源和知识文档。后续对话必须：

- 不执行全仓库 reset/restore/clean；
- 不删除 `N1_ArtPreview_Battle.asset`；
- 不覆盖用户已有 Battle/Y 场景改动；
- 不把本交接中的“已完成”理解为已提交或已推送；
- 每次保存前先读取当前 dirty 状态并保留无关改动。

## 12. 2026-10-06 现有中景侧带调整与保存回读

### 12.1 本轮范围与观察

本轮按照 P0 执行：先取得 Battle Main Camera 的纯场景 Game 截图，再调整现有中景。没有生成、导入或替换新素材，没有改代码、Shader、材质、Sprite Pivot 或近景草簇/木桩/破旗/拒马/辎车。

开局基线截图显示：中景结构较小、集中在地平线附近，两侧前中段存在空档；部分远处残垣在当前距离下几乎不可见。本轮只改 `Y Junction - select for preview/Opening Roadside Props` 下 14 个已有对象的 `localPosition` 和 `localScale`：

- 8 个 `N1_SideBand`：适度加宽、保持低矮比例，左右 z 错开，补足两侧前中段；
- 2 个 `N1_Mid_*_Far2`：从较内侧移回中景侧带；
- 4 个既有低对比 `N1_Mid_Ruins`：移入当前曲率下可见的侧后方范围，并调整尺度。

所有调整对象局部 Y 仍为 0；没有叠加 bounds/Y 补偿。按完整 Sprite 矩形计算，调整对象内缘距路线中心最小为 2.546，大于道路半宽 2.45；这只是静态范围检查，实际遮挡仍以 Game 画面为准。

### 12.2 已保存的局部变换

所有目标均为 Y 场景下 `Y Junction - select for preview/Opening Roadside Props` 的直接子节点；下表为局部坐标及局部缩放。

| 对象 | localPosition (x,y,z) | localScale (x,y,z) |
|---|---|---|
| N1_SideBand_Left_01 | (-4.85,0,3) | (0.90,0.62,0.62) |
| N1_SideBand_Left_02 | (-5.70,0,10) | (0.90,0.56,0.56) |
| N1_SideBand_Left_03 | (-6.20,0,19) | (0.86,0.48,0.48) |
| N1_SideBand_Left_04 | (-6.50,0,29) | (0.72,0.38,0.38) |
| N1_SideBand_Right_01 | (5.00,0,5) | (0.92,0.62,0.62) |
| N1_SideBand_Right_02 | (5.80,0,13) | (0.92,0.56,0.56) |
| N1_SideBand_Right_03 | (6.20,0,23) | (0.84,0.46,0.46) |
| N1_SideBand_Right_04 | (6.60,0,33) | (0.70,0.38,0.38) |
| N1_Mid_Timber_Right_Far2 | (5.70,0,36) | (0.64,0.44,0.44) |
| N1_Mid_Rampart_Left_Far2 | (-5.70,0,38) | (0.68,0.40,0.40) |
| N1_Mid_Ruins_Right_Far | (7.60,0,27) | (0.90,0.72,0.72) |
| N1_Mid_Ruins_Left_Far | (-7.80,0,29) | (0.90,0.72,0.72) |
| N1_Mid_Ruins_Right_Far2 | (8.20,0,33) | (0.84,0.68,0.68) |
| N1_Mid_Ruins_Left_Far2 | (-8.40,0,35) | (0.84,0.68,0.68) |

### 12.3 验证与结束现场

- 通过 Unity API 仅保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`，单独关闭/重载 Y 后，14 个目标变换回读一致；随后再次进入 Play，运行实例的 14 个变换也回读一致。
- 保存前后磁盘对比：仅这 14 个已有 Transform 的位置/缩放字段变化，新增/删除对象均为 0。保存前已记录双方 dirty=false，未调用 Save All。
- 本轮起止 SHA-256 对比确认以下既存文件未变化：Battle.scene、Node_N1.asset、N1_Battle.asset、N1_ArtPreview_Battle.asset、FakeRouteDataTrial.unity、YScrollSample.cs、BattleYRouteHost.cs、YScrollScenery.shader、YGrass.shader、N1 道路/远景材质。它们相对 Git HEAD 的既存 diff 仍须保留。
- 真实 Game 图：开局 distance=0 纯场景、三名 1011（隐藏 UI）遮挡检查、暂停后手动 distance=12（progress=0.075）纯场景预览；本轮调整的中景未见明显悬空，中央画面保持开放。distance=12 是手动预览，不是自然路线流程测试。
- 改动对象仍 active=true、SpriteRenderer enabled=true，统一保留 PPU100、Point、Uncompressed、原 Pivot、`YSampleScenery_v2.mat` 和 `CurvedScroll/Y Sample Scenery`；Shader supported=true。
- `scenery=42`，空项=0、重复=0；天空不在 scenery 中；旧城墙及旧树石/营地组继续禁用。
- 结束为 Edit Mode，Active=`Assets/Scenes/Battle.scene`，只加载 Battle+Y，FakeRoute 未加载；Battle/Y 均 dirty=false。
- 结束 Y 根位置=(0,0,0)、progress=0.5、right=true、turnAngle=45、viewCamera=None；Host/UI/临时渲染隐藏和 Game 窗口最大化均已恢复。Battle Camera 仍为 position=(0,3,-10)、rotation=(18,0,0)。
- Console error/warning=0。未执行 commit/push。

临时对比与回读图位于 `Library/Locus/tmp/n1-sideband-review/`：`opening-before-after.jpg`（左旧/右新）、`after-opening.png`、`after-combat-no-ui.png`、`after-distance12.png`；这些仅为本地临时证据，不是永久美术交付。

### 12.4 仍需用户目视验收与接线提醒

- 本轮完成的是一组现有侧带的调整和保存回读，不等于 N1 整体美术最终通过。下一轮先请用户检查：侧带是否自然、低矮比例是否可信、是否仍有独立告示牌观感；不要未经新截图依据继续添加物件或生图。
- 未验证自然 N1→E0、左右分支完整流程、奖励/暂停/重开/存档；第 7 节未完成项继续有效。
- 既存天空接线需同时看 Renderer 与材质：SpriteRenderer 实际使用无城墙 `N1_FarBackground_v1.png`，但 `N1_FarBackground_v1.mat._MainTex` 回读仍是 `N1_FarBackground_WithCityWall_v1.png`。当前 `[PerRendererData]` Sprite 纹理覆盖使实际 Game 无城墙；本轮没有修改此既存材质。以后排查或换 Renderer 时不要只看材质纹理，也不要据此自动重新启用城墙方向。

## 13. 2026-10-06 N1 直路尾段侧带延展

### 13.1 目标与结果

本轮继续按 P0 检查保存后的真实 Game 构图。开局到约 32 单位的近/中景节奏已成立，但 48–60 单位曾出现几乎只剩道路和远山的空段。通过多组运行时试摆，确认将已有中景模块按左右交错、每约 4–6 单位一件向分叉前延展，可以补足直路尾段；不需要生成新素材，也不改变路线或地面 Shader。

最终持久化 10 个复用现有 Sprite 的 Y 场景对象，均位于 `Y Junction - select for preview/Opening Roadside Props` 下：

| 对象 | 复用源 | localPosition | localScale |
|---|---|---|---|
| N1_SideBand_Left_05 | N1_Mid_Rampart_Left_Far | (-4.65,0,42) | (0.82,0.60,0.60) |
| N1_SideBand_Right_05 | N1_Mid_Timber_Right_Far | (4.80,0,46) | (0.86,0.58,0.58) |
| N1_SideBand_Left_06 | N1_Mid_Mound_Left_Mid | (-4.75,0,52) | (0.86,0.64,0.64) |
| N1_SideBand_Right_06 | N1_Mid_Rampart_Right_Mid | (4.85,0,56) | (0.90,0.62,0.62) |
| N1_SideBand_Left_07 | N1_Mid_Timber_Left_Mid | (-4.90,0,62) | (0.92,0.64,0.64) |
| N1_SideBand_Right_07 | N1_Mid_Mound_Right_Near | (4.75,0,66) | (0.86,0.62,0.62) |
| N1_SideBand_Left_08 | N1_Mid_Rampart_Left_Far | (-4.80,0,72) | (0.88,0.60,0.60) |
| N1_SideBand_Right_08 | N1_Mid_Timber_Right_Far | (4.80,0,76) | (0.88,0.60,0.60) |
| N1_Mid_Ruins_Left_Tail | N1_Mid_Ruins_Left_Far | (-6.20,0,80) | (1.02,0.82,0.82) |
| N1_Mid_Ruins_Right_Tail | N1_Mid_Ruins_Right_Far | (6.30,0,82) | (1.04,0.82,0.82) |

所有新增对象局部 Y=0，内缘保留在道路外侧；中央战斗通道没有放置物件。

### 13.2 保存与验证

- 仅保存 `Assets/Experiments/CurvedScroll/YJunctionSample.unity`；Battle.scene 未保存、未变脏。
- Y 场景单独关闭/重载后，10 个对象全部存在、active=true、SpriteRenderer enabled=true，local Y=0，且仍引用 `YSampleScenery_v2.mat` / `CurvedScroll/Y Sample Scenery`。
- `YScrollSample.scenery` 从 42 增加为 52；空引用=0、重复引用=0。尾段对象索引为 42–51。
- Play 重载后 `BattleYRouteHost` 进入 `Battle`，路线相机由 Host 会话绑定，10 个尾段对象全部回读存在；没有移动 Battle Camera 或战斗对象。
- 隐藏 UI/战斗渲染的纯场景 Game 截图在 48、60、72 单位均显示两侧环境，中央道路保持开放。截图证据位于 `Library/Locus/tmp/n1-scene-followup/`：`reloaded-opening.png`、`reloaded-distance60.png`、`reloaded-distance72.png`。
- 本轮尝试过把原有 5 个远端模块直接后移、缩放和地面开关对照；这些候选均未保存，最终恢复了原有 14 个侧带调整和 `scenery=42` 的基线后，才创建本节 10 个尾段对象。
- 结束为 Edit Mode，Active=`Assets/Scenes/Battle.scene`，只加载 Battle+Y，FakeRoute 未加载；Battle/Y 均 dirty=false，Console error/warning=0。
- 结束 Y 根位置=(0,0,0)、progress=0.5、right=true、turnAngle=45、viewCamera=None；Host/UI/临时渲染隐藏和 Game 窗口状态均恢复。Battle Camera 仍为 position=(0,3,-10)、rotation=(18,0,0)。
- 受保护文件 SHA-256 对比确认只有 `Assets/Experiments/CurvedScroll/YJunctionSample.unity` 在本轮发生变化；没有改代码、Shader、材质、正式 N1 配置或 FakeRoute。未执行 commit/push。

### 13.3 下一步边界

- 先由用户目视确认 48–72 单位尾段的密度、模块重复感和两侧接续是否自然；不要在没有新 Game 证据时继续加密或生成新素材。
- 本轮只覆盖直路尾段，不等于 Left/Right 分支终点、Encounter 数据消费、自然路线战斗、奖励、暂停、重开或存档回归完成。
- 如果尾段验收通过，下一步应检查分叉入口本身的语义和左右侧带接续；不要先扩展 N2–N6，也不要重新启用城墙远景。

## 14. 2026-10-06 N1→E0 城墙与战后余烬素材生产

### 14.1 方向修正

用户明确指出：当前素材种类太少，继续复制土坡、木栅、残垣不能真正改善场景；下一步应先制作后续节点景观，思考 N1→下一个节点的连续内容。

本轮将下一段明确为正式设计中的 **N1→E0「战后余烬」**：

```text
N1 开阔黄土战场
→ 远处低对比残城/城墙轮廓
→ 两侧中远景残墙、破损墙段和城门方向锚点
→ 城门影响范围：焦黑木梁、翻倒辎车、散盾断箭、残旗、瓦砾与烟尘
→ E0 敞开残破城门外
```

这不是把城墙重新接回当前 N1 天空背景；城墙/城门将作为 N1 尾段到 E0 的独立环境素材族，后续按距离连续显露。当前 N1 无城墙远景保持不变。

### 14.2 已生成并整理的候选素材

候选项目目录：

```text
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/
```

当前包含：

- `n1_e0_wall_module_01_v1.png` 至 `n1_e0_wall_module_06_v1.png`：近/中景石土城墙模块，包括完整短墙、断裂墙段、倾斜塌墙、墙角端部、带垛口墙段和带残旗的缺口墙；
- `n1_e0_broken_open_gate_module_v1.png`：敞开残破石土城门，中央拱门洞保持透明，可作为 E0 门外和后续穿门段的空间锚点；
- `n1_e0_distant_ruined_wall_module_01_v1.png` 至 `_04_v1.png`：低对比远景残城、破损瞭望塔墙段、远处敞开门洞和断裂残墙，用于城门尚未到达时的空间预告；
- `n1_e0_war_aftermath_prop_01_v1.png` 至 `_06_v1.png`：翻倒辎车、散盾断箭、塌门梁、焦黑残旗、燃烧瓦砾和石块木箱，用于城门影响范围的战后余烬密度。

原始生成图与响应保存在当前用户目录：

```text
C:/Users/Administrator/Pictures/gptGen/n1_e0_wall_prototypes_20261006_v1/
```

其中 `cutout_modules/` 保存独立裁切预览；原始 PNG 和 `.response.json` 保留，不覆盖。

### 14.3 素材验收与 Unity 导入

- 所有候选 PNG 均为 RGBA，四角透明；机器检查记录真实 Alpha bbox，透明安全边界约 24–32px。
- 第一批 7 个资产已按 Alpha 主体裁切，清理了小型生成碎片；第二批 10 个资产也已按模块裁切并清理小连通块。
- Unity 导入基线已回读：TextureImporter=`Sprite`、Single、PPU=100、Point、Uncompressed、Alpha Is Transparency=true、mipmap off、maxTextureSize=2048、RGBA32。
- 第一批墙体和城门已使用 Custom Pivot，按真实 Alpha 底边计算；第二批远景墙和战后物件也已从默认 BottomCenter修正为 Alpha-based Custom Pivot。所有候选都没有放入 `YScrollSample.scenery`。
- 城门洞在浅色、深色和土色背景对照中保持透明；候选图没有烘入道路、人物或 HUD。
- 当前候选目录只是生产/验收目录，不是正式 N1 场景部署目录；本轮没有复制对象进 Y 场景，也没有修改 Battle、路线、Shader、材质或正式 N1 配置。

### 14.4 后续景观部署顺序

候选素材通过用户目视验收后，按以下顺序建立 N1→E0 的表现段：

1. **N1 尾段**：保持当前开阔黄土战场，只在远端以低对比残城模块提示方向；不使用完整城墙横幅，不遮挡战斗区。
2. **接近段**：两侧交错部署低矮墙段、破墙端部和远景残城；城墙逐步变大，不能突然整屏出现。
3. **门外段**：使用敞开残破城门作为远/中景空间锚点，旁边加入少量战后余烬道具；中央通道仍保持可穿越。
4. **E0 门外**：城门、残墙、辎车、残旗、瓦砾和烟尘形成战后余烬节点构图；该段才考虑独立的 E0/Travel Presentation 数据，不把所有内容继续堆进当前 N1 战斗开场。
5. **E0→E1**：沿已记录的“穿过城门后的城郊位置”继续设计门洞后视角；不要把城门模块误当作当前 N1 天空背景。

### 14.5 当前明确不做

- 不把 `N1_DistantCityWallLandmark_v1`、`N1_CityWallScreenOverlay_v1` 或旧城墙材质重新接回当前 N1 远景；
- 不把完整城墙图当作天空背景或普通单个 `scenery` 大卡片；
- 不在没有路线距离/锚点设计前把城门部署进 Y 场景；
- 不修改 `YScrollSample.Evaluate()`、路线长度、转角、Battle Camera、Player、Enemy、Battle.scene 或正式 N1 配置；
- 不把本轮候选素材的生成和导入验收描述为 N1→E0 连续旅行已完成。

### 14.6 本轮结束现场

- Active=`Assets/Scenes/Battle.scene`；Additive=`Assets/Experiments/CurvedScroll/YJunctionSample.unity`；FakeRoute 未加载；
- Battle/Y 均 dirty=false；`scenery=52`，空引用=0，重复=0；`viewCamera=None` 为 Edit Mode 正常状态；
- Console error/warning=0；没有保存场景或修改运行时配置；未执行 commit/push。
- 本轮完成的是 **N1→E0 景观内容设计 + 城墙/城门/战后余烬候选素材生产与导入验收**，不等于正式路线接入或用户最终美术验收。

## 15. 2026-10-06 半3D城门模块修正与正交素材候选

### 15.1 对上一批城门素材的结论

用户指出上一批城门不适合使用，原因成立：

- 城门建筑本身带有固定透视角度和斜向轮廓；
- 不能稳定正面部署，也不能通过 Unity 平面旋转得到可信左右斜向视图；
- 图片把建筑的角度烘死在 Sprite 内，无法表现玩家真正穿过门洞时的内外墙深度；
- 不能靠缩放、排序或 `visualYOffset` 修复，因此上一批带透视城门候选只保留为失败参考，不再部署。

### 15.2 正确的半3D职责拆分

城门穿越不再制作一张“完整斜拍城门图”，而拆为正交模块：

```text
前入口：gate_front_facade
├─ 正面门墙/门洞，严格正面
├─ gate_inner_wall_left：左侧门洞内墙贴片
├─ gate_inner_wall_right：右侧门洞内墙贴片
├─ gate_arch_soffit：门洞顶部拱顶/内衬
├─ gate_exit_end_wall：远端出口墙面，可留小观察缝
└─ 后续另配：出口外墙、门外地面/余烬和深度遮挡层
```

这些图片都必须保持：

- 正交正面源图；
- 垂直边保持垂直，水平边保持水平；
- 不画透视收缩、消失点或斜拍建筑角度；
- 左右斜向效果由后续半3D平面/Transform旋转产生，而不是烘进图片；
- 内墙贴片是“墙面材质模块”，不是完整独立建筑；
- 普通 `YScrollScenery` billboard 只适合外部立面和远近景，不直接承担穿门内墙几何。

### 15.3 新候选目录

```text
Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0GateHalf3DPrototypes_v2/
```

当前包含：

- `gate_front_facade_orthographic_v2.png`：正面城门外立面，中央拱门洞保留真实透明；
- `gate_inner_wall_left_orthographic_v2.png`：左侧门洞内墙贴片；
- `gate_inner_wall_right_orthographic_v2.png`：右侧门洞内墙贴片；
- `gate_arch_soffit_orthographic_v2.png`：门洞顶部拱顶/内衬横向贴片；
- `gate_exit_end_wall_orthographic_v2.png`：远端出口端墙，中央窄观察缝保持透明。

原始生成图与响应：

```text
C:/Users/Administrator/Pictures/gptGen/n1_e0_gate_orthographic_v1/
```

### 15.4 已完成验收

- 正面门墙原始 Alpha 的拱门透明孔洞已保留；此前误把拱顶实体区域当成残留背景并切成三角形的中间版本已废弃。
- 五个 v2 模块均为 RGBA、四角透明、独立 PNG；已按主体 Alpha 底边保留透明安全边界。
- Unity 导入基线已回读：Sprite/Single、PPU=100、Point、Uncompressed、Alpha Is Transparency=true、mipmap off、maxTextureSize=2048、Custom Pivot、RGBA32。
- 没有把带窄观察缝的出口墙误当作门洞；它只承担远端端墙职责。真正可穿越的中央门洞只在 `gate_front_facade` 中保留，后续穿门段需要另设空间深度关系。
- 当前没有将这五个模块加入 `YScrollSample.scenery`，没有创建场景实例，也没有修改 Battle、Y 路线、Shader、材质或正式 N1 配置。
- 受保护文件从本轮检查点起 SHA-256 未变化；Battle/Y 仍 dirty=false，Console error/warning=0。

### 15.5 下一步实现边界

正式部署前必须先建立独立的半3D Gate Presentation/Authoring 层：

1. 用正面门墙作为入口平面；
2. 在门洞左右两侧以独立内墙平面形成可见深度；
3. 用拱顶内衬遮住门洞上方，避免看到天空背景；
4. 用出口端墙形成远端空间终点，但不能封死玩家路径；
5. 由平面位置、旋转、宽度和深度关系生成斜向视图；
6. 以 Game 相机检查从门外接近、进入门洞、穿过中段、抵达出口四个采样位置；
7. 通过后再决定是否写入独立 E0/Travel Presentation，不把这些对象堆进当前 N1 战斗开场。

在这套半3D结构通过前，不得宣称城门穿越系统完成，也不得重新启用旧城墙天空叠图。
