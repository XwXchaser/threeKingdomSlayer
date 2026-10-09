---
id: kd_214f05d0-3240-41c0-b816-1c68721ffc06
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 外围残门垣预览：场景制作交接（2026-10-08）

## 1. 接手结论与当前优先项

用户确认 **E0 是外围残门垣，画面左侧插有旗帜**，并授权继续场景搭建。本轮已用既有资产保存一个独立、可检查的 E0 场景预览：

`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity`

随后用户要求停止继续搭建、制作交接。本交接阶段仅回读现场和整理文档，没有再改场景。

**下次优先：检查最后保存的左旗帜在静止和接近采样中的完整性、与左墙柱的连接，然后让用户目视验收整体门垣。** 最后降旗/缩旗已经保存并回读，但没有重新截图或重载验证这次调整，不能称为裁切问题已修复。

这只是独立环境预览，尚未接入正式 N1→E0→E1→J1 流程，不是已完成的 E0 节点或穿门系统。

## 2. 用户已确认的方向

- 顺序：**N1→E0→E1→J1**。不采用旧实验 N0→J0→N1。
- E0 为外围残门垣，不是完整城市主城门；左侧有独立插旗。
- 视觉行进距离可以为构图调整，**各段行进时间保持一致**。本轮没有更改任何旅行时长。
- 从既有场景推导连续参考图，再按 E0 整体构图拆素材；旧六件城墙量产提示词仍暂缓。
- 用户提供的空间/画风母图：`C:/Users/Administrator/Downloads/n1_battlefield_reference_retry_20261007_011121.png`，已读为1024×2048、RGB、1:2。
- 不删除用户用于检查的试摆结果。用户原先在 Y 中摆好的城墙保留，本轮没有调整它。
- 本轮复用已有候选进行可检查的空间预览，没有发起远程生图、付费请求或生成新图片。R1/R2 场景参考图也未生成、未验收；不能因已搭预览而勾选它们完成。

## 3. 结束时的 Editor 与 Git 现场

交接前通过 Unity 工具实际回读：

| 项目 | 结果 |
|---|---|
| Editor | Edit Mode |
| Active Scene | `Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity` |
| 已加载场景 | 仅 E0OuterGatePreview |
| E0 dirty | false |
| E0 scene/meta | 两个文件均存在 |
| Console warning/error | 0（本次读取） |
| Git branch | route-scroll-movement，跟踪 origin/route-scroll-movement |
| E0 scene/meta Git状态 | untracked，未提交 |

开始搭建时只加载 Battle；随后 Additive 打开 Y 作为源，创建 E0。初次三场景同时加载造成 Game 重叠，之后在 Battle/Y 均 clean 的前提下无保存地关闭它们，单独查看 E0。**Battle 和 Y 当前没有加载，不代表它们被删除或其磁盘文件发生本轮修改。** 用户两次最新状态公告也确认 Active Scene 为 E0。

工作区已有 Battle、N1预览战斗配置、骑兵素材、其他知识文件及打包产物等改动；不要 reset/restore/clean 或将全部 diff 归因于 E0。本文的文件哈希仅用于本轮前后对照，不可覆盖未来用户修改。

## 4. E0 场景层级与运行边界

```text
E0 Outer Gate Preview                         [YScrollSample]
├─ E0 Ground - N1 Y Terrain                   [MeshFilter, MeshRenderer]
├─ E0 Sky - N1 Far Background                 [SpriteRenderer]
├─ E0 Outer Ruined Gate - Front Assembly
│  ├─ E0_GatePier_Left_FlagSide
│  ├─ E0_GateWall_Left_OuterRun
│  ├─ E0_GateWall_Right_CollapsedRun
│  ├─ E0_Flag_Left_WallTop
│  └─ E0_GatePier_Right_Broken
└─ E0 War Aftermath - Roadside
   ├─ E0_Roadside_ScatteredShieldArrows_Right
   ├─ E0_Roadside_OverturnedCart_Left
   ├─ E0_Roadside_CharredLogs_Left
   ├─ E0_Approach_BrokenWall_Left
   ├─ E0_Approach_LowRuin_Right
   ├─ E0_Grass_Left_Near
   ├─ E0_Grass_Right_Near
   ├─ E0_Grass_Left_Mid
   └─ E0_Grass_Right_Mid
E0 Preview Camera                            [Camera，无AudioListener]
```

- Root 世界位置=(0,-1.8,0)，旋转=0，Scale=(1,1,1)。这是 E0 预览自己的地面高度，不回写 Y 根位置。
- `YScrollSample`：progress=0、right=false、animate=false、duration=10、turnAngle=45、battleHost=None、Length=160。
- scenery=15：1个地面Renderer＋5个门垣/旗帜Renderer＋9个路侧Renderer；空项=0，重复=0。天空不在 scenery 中。
- 预览相机引用在同一场景内持久保存：位置(0,3,-10)，旋转(18,0,0)，FOV60，near0.3、far1000、depth0、cullingMask=-1、enabled=true；最近Game视口253×505。
- 没有 BattleHost、StageController、战斗对象或自动E0节点提交逻辑。duration=10是独立预览字段且animate=false，**不是正式 N1→E0 时长**。
- 复用了 YSampleTerrain 的分叉地面网格。当前静止/近距离视图看直路，不代表已经制作独立E0地形或门后道路。
- 没有创建前文曾检查过的 `E0OuterRuinedGate_Preview.prefab`；当前交付物是场景。

## 5. 保存的对象参数

下表都是相对各自父组的 **localPosition/localScale**。两个父组本身均为局部原点、单位缩放，所以数字处于同一预览空间；不要把这些Y值与世界Y混用。所有对象active、SpriteRenderer enabled。

### 门垣与左旗帜

父路径：`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Outer Ruined Gate - Front Assembly`

| 对象 | Local Position | Local Scale | flipX | order | 图片简称 |
|---|---|---|---|---:|---|
| E0_GatePier_Left_FlagSide | (-8.9,0,35) | (3.25,3.25,3.25) | true | 0 | wall_04 |
| E0_GateWall_Left_OuterRun | (-17,0,35.2) | (3.4,3.4,3.4) | false | 0 | wall_01 |
| E0_GateWall_Right_CollapsedRun | (18,0,35.2) | (3.25,3.25,3.25) | false | 0 | wall_02 |
| E0_GatePier_Right_Broken | (9,0,35) | (3.45,3.45,3.45) | true | 0 | wall_03 |
| E0_Flag_Left_WallTop | **(-6.2,5.7,34.82)** | **(0.88,0.88,0.88)** | false | 1 | N1_SmallBrokenFlag_v1 |

左旗完整对象路径：

`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Outer Ruined Gate - Front Assembly/E0_Flag_Left_WallTop`

最后保存前的旗参数曾为(-6.2,8.5,34.82)、Scale1.15；下面所有有效构图截图都早于最后一次降旗，不能用这些截图证明当前旗帜显示情况。最后世界位置回读为(-6.2,3.9,34.82)。下一次检查除裁切外，还应确认降低后旗杆是否与左墙柱自然连接、是否有过多重叠。

### 战后路侧

父路径：`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 War Aftermath - Roadside`

| 对象 | Local Position | Local Scale | flipX | 图片简称 |
|---|---|---|---|---|
| E0_Roadside_OverturnedCart_Left | (-3.5,0,6) | (0.55,0.55,0.55) | false | aftermath_01 |
| E0_Roadside_ScatteredShieldArrows_Right | (3.25,0,8.5) | (0.43,0.43,0.43) | false | N1_BrokenShieldArrows_v1 |
| E0_Roadside_CharredLogs_Left | (-4.2,0,15) | (0.38,0.38,0.38) | false | N1_FallenLogs_v1 |
| E0_Approach_BrokenWall_Left | (-6.1,0,25) | (0.85,0.85,0.85) | false | wall_03 |
| E0_Approach_LowRuin_Right | (6,0,26.5) | (0.85,0.85,0.85) | false | wall_02 |
| E0_Grass_Left_Near | (-3.85,0,4.5) | (0.31,0.17,0.17) | false | N1_GrassMedium_v1 |
| E0_Grass_Right_Near | (3.8,0,6) | (0.29,0.16,0.16) | true | N1_GrassMedium_v1 |
| E0_Grass_Left_Mid | (-4.7,0,17.5) | (0.3,0.16,0.16) | false | N1_GrassMedium_v1 |
| E0_Grass_Right_Mid | (4.8,0,19) | (0.3,0.16,0.16) | true | N1_GrassMedium_v1 |

路侧全部order=0。名称CharredLogs并不证明本轮制作了焦黑材质：实际复用的是既有FallenLogs图片。

## 6. 实际引用资产

墙体/破车：

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_wall_module_01_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_wall_module_02_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_wall_module_03_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_wall_module_04_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_war_aftermath_prop_01_v1.png`

旗帜/路侧：

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_SmallBrokenFlag_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_BrokenShieldArrows_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_FallenLogs_v1.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_GrassMedium_v1.png`

材质与地面/天空：

- 普通物件：`Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_v2.mat`。
- 草：`Assets/Experiments/CurvedScroll/Authoring/YGrass.mat`。本预览使用普通Sprite，未制作新的风动细分网格；不等于N1已有草风动被修改。
- 地面mesh：`Assets/Experiments/CurvedScroll/Authoring/YSampleTerrain.asset`，Local Scale=(6,1,6)。
- 地面材质：`Assets/Experiments/CurvedScroll/Authoring/N1_Ground_ThreeLayer_v1.mat`。
- 天空Sprite：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/N1_FarBackground_v1.png`。
- 天空材质：`Assets/Experiments/CurvedScroll/Authoring/N1_FarBackground_v1.mat`。天空local=(0,5,300)、Scale=(22,33,1)、order=-100，保留Sprite纹理覆盖材质采样的既有方式。

本轮没有修改共享PNG、Importer、材质、Shader或Mesh资产。墙体候选和旗帜已检查真实RGBA、四角Alpha0，使用环境PPU100等既有导入设置。具体是否为最终新画风仍需用户评审。

## 7. 预览迭代与截图证据

截图都位于Library临时证据目录，交接时已核对文件存在；若将来丢失需重新截，不视为永久美术交付。

| 完整路径 | 用途和限制 |
|---|---|
| `Library/Locus/Screenshots/locus_game_20261008_130944_665.png` | 无效验收图：Battle、N1与E0相机同时渲染，含HUD/环境重叠。不能引用为E0最终构图。 |
| `Library/Locus/Screenshots/locus_game_20261008_131612_933.png` | 独立初稿：中央wall_06瓦砾堵通道、旗在左边缘裁切。已被后续构图取代。 |
| `Library/Locus/Screenshots/locus_game_20261008_131851_804.png` | 调整后Edit Mode：左侧墙柱/插旗、右倾塌墙、中央开放、两侧破车与散盾；使用旧旗高度/缩放。 |
| `Library/Locus/Screenshots/locus_game_20261008_132226_500.png` | 保存并重载后Play Mode，distance=0，确认上述构图可由磁盘重建；仍使用旧旗参数。 |
| `Library/Locus/Screenshots/locus_game_20261008_132615_558.png` | Play手动distance=12（progress=0.075）接近采样，通道开放，旧旗顶部裁切；是最后降旗的依据。不是自然旅行。 |

初稿的中央缺口墙对象已在本轮新场景内改作左侧外墙（重新命名/换Sprite），最终没有中央wall_06挡路，也没有完整门楼/拱门。不能拿最初“中央开放门垣”的话术当作最终已建中央门洞资产。

已完成的重载是在**降旗之前**。最后降旗之后仅进行了SaveScene、live回读和磁盘存在/哈希检查，未再进入Play、截图或重载。

## 8. 原N1与受保护文件

用户原对象仍在原场景：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props/N1_E0WallPreview_Distant_Left_01`

本轮加载时live回读：local=(-0.32,0,40)、Scale=(3.03,3.03,3.03)、order0、原Y scenery=81；本轮未改变这些值。Y现在已关闭，接手时以最新实际状态为准。

本轮前后SHA-256核对以下文件未变化：Battle.scene、YJunctionSample.unity、FakeRouteDataTrial.unity、YScrollSample.cs、BattleYRouteHost.cs、YScrollScenery.shader、N1_Ground_ThreeLayer_v1.mat、YSampleScenery_v2.mat、N1_FarBackground_v1.mat。Battle相对Git原有diff仍在，不属于此次E0改动。

最终E0文件：47714 bytes，SHA-256=`b1bb9f56f230572aaf0d829863af9bd2184f93b1b3c274aeb0e4869d30eaf6d0`。这是交接时记录，后续合法修改应重新记录，不能据此还原覆盖。

## 9. 未完成与限制

- [ ] 最终旗位置/缩放的截图、接近采样、保存重载验收。
- [ ] 用户对E0整体构图、门垣体量、左旗连接、两侧残骸密度的目视验收。
- [ ] 以场景母图递推的R1/R2完整参考图、缺口素材正式排产与最终画风验收。
- [ ] N1→E0自然接近、E0停留内容/唯一出口、E0→E1和J1接线。
- [ ] 正式段时长基线与暂停/恢复验收。开始时Battle测试Host openingDuration=3、branchDuration=3，并不代表正式N1→E0秒数。
- [ ] 门前、入门、门内、门后的深度与遮挡连续性；当前只有两侧Sprite开放口，没有半3D内墙/拱顶/出口。
- [ ] E1残军集结与J1官道/村落场景。
- [ ] 正式战斗/HUD叠加验收、分辨率适配和性能采样；当前证据仅独立253×505视口。
- [ ] 保存/暂停/失败重开/奖励的完整自然流程回归。

当前用户要求交接，以上均不在本交接阶段继续执行。

## 10. 下次接手顺序与保护规则

1. 先读本文与 `plan/scroll-scene-current-todolist.md`，核对用户新消息、Editor状态、场景dirty和Git，不重复询问已确认的E0建筑语义。
2. 当前预览已保留，优先直接检查它；不要删除对象或创建另一套同名场景。若用户有新修改，先回读再调整。
3. 独立E0中分别检查progress=0和12/160=0.075，旗帜完整性与墙柱连接。仅在Play中手动改变progress做实验，结束回到Edit，不能把实验进度保存为基线。无自动旅行时不要声称时间一致性已通过。
4. 对最终保存旗参数做重载检查，给用户实际Game图；随后由用户评价整体构图，不以工具指标替代审美验收。
5. E0与Battle/Y同时打开会有双相机/双环境重叠。不要复制初次无效截图流程；未来集成需要一个明确的渲染/环境生命周期。恢复Battle查看时先检查dirty并保存授权E0工作，再正常打开 `Assets/Scenes/Battle.scene`，由既有宿主加载Y；不是覆盖或删除E0文件。
6. 当前用户试摆、Battle相机/战斗坐标、Y移动核心和既有旅行时长都要保护。场景修改通过Unity API；不Save All、不整文件restore，不自动删除试摆。
7. 不打开、保存或部署到 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。共享材质/Shader不得为单个素材可见性随意改动。
8. 继续生图前仍需完整中英文提示词、参考图职责和关键参数审批。旧城墙单件提示词不作为这次授权；本轮既有素材预览不代表新生成已完成。

## 11. 文档入口

- 当前场景制作交接：本文。
- 专项待办：`plan/scroll-scene-current-todolist.md`。
- 里程碑：`plan/october-milestone-plan.md`，原日期与总验收项未由本预览自动勾选。
- 既有候选和半3D职责：`plan/n1-scene-art-production-handoff.md`。
- N1远景：`plan/n1-far-scenery-handoff-20261008.md`。
- 暂缓旧提示词：`plan/n1-e0-wall-generation-prompts.md`。

没有commit/push。本轮交付为保存的独立E0预览和本交接文档；停止在最后旗帜调整的视觉复验之前。
