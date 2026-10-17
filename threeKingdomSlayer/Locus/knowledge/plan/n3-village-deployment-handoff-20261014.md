---
id: kd_29150209-dc5d-4f08-8e1b-117cde2a06c0
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N3 村落搜救战斗布景部署交接（2026-10-14）

## 当前状态

**2026-10-15 更新：四面石材组图已获用户验收，透明底部已核对，生产Atlas已接入以下12个墙段并保存重载验证。** 新接入与透明处理以 `plan/n3-wall-surface-deployment-handoff-20261015.md` 为准；下文灰墙制作/首轮采样是历史记录。墙体尺寸、Mesh与布局不变。

用户已明确同意使用灰色带黑色描边的面片模拟沿路围墙，并授权根据现有场景素材开始部署村庄战斗节点。本轮已完成正式 Y 路线层的 N3 村庄布景部署，用户随后已在 Game 中明确回复“已验收，效果不错”。

正式结构保持：

`Assets/Scenes/Battle.scene` + Additive `Assets/Experiments/CurvedScroll/YJunctionSample.unity`

未操作：`Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。

## 部署结果

新增独立层级：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/N3 Village Rescue - Accepted Art and Grey Wall Study @ 266`

四个子组：

- `Village Houses - Near Mid Far`：8个已验收茅屋切片，左右近/中/远层。
- `Courtyard Props - Well Gate Wood`：井、院门、柴垛、短横墙/残墙共7个生活与边界物件。
- `Road Parallel Walls - Grey Black Outline`：4条墙链，共12个4米墙段，使用灰色实体面、黑色边缘线、细分顶面和端面变体。
- `Roadside Trees Grass and Rocks`：复用现有J1树、草、碎石，共43个路侧自然层对象。

N3 `ScrollRouteNodePresentation.backgroundAnchor` 已绑定到该独立村庄组。N3距离为266.1165，组锚点局部位置按路线计算为`(-46.4315, 0, 248.0956)`，旋转Y=315度；进入Play后Host将路线根移动到groundHeight=-1.8，不把该运行时Y保存进Y场景根。

`YScrollSample.scenery`由566增加到636，新增Renderer 70；回读为null=0、重复=0。Y场景保存并关闭/重开成功，Active Scene仍为`Assets/Scenes/Battle.scene`，Battle和Y均dirty=false。

## 已导入素材

Windows可复制的Unity素材目录：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Assets\Experiments\CurvedScroll\Art\EnvironmentV2\N3\Village\`

已按真实Alpha轮廓从G1/G2原图切出8个独立PNG，原始图集未修改：

- `N3_H1_CompactHouse_v1.png`
- `N3_H2_LongHouse_v1.png`
- `N3_H3_DamagedHouse_v1.png`
- `N3_W1_Well_v1.png`
- `N3_B1_IntactWall_v1.png`
- `N3_B2_DamagedWall_v1.png`
- `N3_B3_CourtyardGate_v1.png`
- `N3_B4_Firewood_v1.png`

切片脚本：`Library/Locus/tmp/n3-village-deployment-20261014/slice_accepted_assets.py`。其注释“nearest-owner”是历史准备说明；本次实际使用每件限定象限/轮廓范围的Alpha阈值bbox裁切，没有覆盖像素归属冲突。每张图均导入为Single Sprite、PPU100、Point、Uncompressed、mipmap off、Clamp、Alpha Is Transparency；首次导入后又依据manifest修正8件Sprite的横向Custom Pivot，并对全部场景实例补偿Transform X，保存/重载后位置保持。`slice_manifest.json`已被Unity导入并有.meta。

预览接线使用：

- 房屋/生活Sprite：`Assets/Experiments/CurvedScroll/Authoring/YSampleScenery_E0Route.mat`
- 沿路墙当前石材：`Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_StoneWall_Outline.mat`；原灰墙`N3_GreyWall_Outline.mat`保留为对照。
- 树草：复用现有J1正式资源/风动材质
- 碎石：复用`ValleyProps.png`中的现有Sprite资源

## 沿路灰墙原型

新建：

`Assets/Experiments/CurvedScroll/Shaders/YVillageWallPrototype.shader`

`Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_GreyWall_Outline.mat`

`Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_Wall4m_Open_Open.asset`

`Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_Wall4m_Start_Open.asset`

`Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_Wall4m_Open_End.asset`

`Assets/Experiments/CurvedScroll/Authoring/N3Village/N3_Wall4m_Start_End.asset`

每个墙段包含两侧面、窄顶面和按首/尾需要的短端面；墙体沿Z细分为8段，方便路线下弯。Shader采用：

- 面色约`#949494`；
- 黑色边缘约`#060606`；
- `_OutlineWidth=0.045`；
- 当前Shader支持且无编译消息。

这是**结构验证材质**，不是最终石墙美术。用户已对灰黑面片构成的墙体观感和N3首轮效果明确验收。后续保留尺寸、墙体空间关系与黑色描边，只制作和接入正交石材面图；生成方案见`plan/n3-road-parallel-wall-art-analysis-20261014.md`。

## 采样与验证

采样目录：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\n3-village-deployment-20261014\final\`

已生成左路：J1=190、转弯中203、转完212.118、EV225、途中240/255、N3=266.1165；右路：190、202、236、N3。采样结束后progress/right/camera/targetTexture和临时禁用状态全部恢复。

代表性文件：

- `final/left_190.0.png`：J1已验收画面，未出现村庄建筑。
- `final/left_255.0.png`：接近N3时两侧屋舍、树草和灰墙开始显露，中央道路保持连续。
- `final-pivot-corrected/left_266.1.png`：Pivot修正后的N3停点，两侧村舍边缘和灰墙可读，中央五列战斗地面保持开放。`final-pivot-corrected/left_255.0.png`为修正后的接近采样。
- `final/right_236.0.png`：右路现有官道画面未新增村庄组遮挡。

独立验证：

`Library/Locus/tmp/n3-village-deployment-20261014/verification.json`

结果：已有1224个旧Transform全部未改变；Battle.scene、FakeRouteDataTrial、路线脚本、BattleYRouteHost、共享地面/景物Shader、J1地面材质哈希均未变化；新组70个Renderer；总scenery=636；null=0；duplicate=0；N3锚点绑定；Pivot修正后Y再次保存/关闭/重开，Y dirty=false。

Unity Console error/warning=0。Play状态运行回读：Host绑定Main Camera，路线根运行时Y=-1.8，N3锚点仍指向村庄组，墙Shader supported=true、Shader messages=0，progress=0/right=false的验证后状态保持不变。

当前Game viewport截图（从N1起始战斗，不代表N3画面）：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\Screenshots\locus_game_20261014_154115_401.png`

## 用户验收边界与未完成项

首轮N3用户已对灰黑墙体、两侧屋舍、道路净空和整体效果明确验收。后续四面石材组图也已获美术验收，并于2026-10-15完成对应12段墙接入；用户随后已在Game中确认新石墙效果并完成本轮验收。新材质Game验收已闭环，不把首轮灰墙验收与新材质验收混为一谈。

未做：

- 石墙贴面已接入；原始图/生产Atlas分离，透明底部与实体墙取样均已处理，详见最新石墙交接。
- 没有生成或部署G3粮袋、陶罐、竹筐、谷束。
- 没有生成或部署G4淡烟、焦木、搜救痕迹。
- EV独立村落外围组已完成并获用户Game验收：绑定EV225 Presentation Anchor，新增45 Renderer，scenery=681/null0/duplicate0；详见`plan/ev-village-outskirts-deployment-handoff-20261015.md`。完整自然流程、真实战斗、奖励与退出回归仍未执行。
- 没有运行完整自然N1→J1→EV→N3流程、真实战斗、奖励或退出回归；本轮根据项目规则不扩大到完整玩法自检。
- 没有修改Battle.scene、路线代码、相机、道路Shader或路线几何。

用户已完成首轮N3 Game验收，2026-10-15也验收四面组图并授权接入。已用一张组图派生Side A/B、Top、End生产Atlas替换面材质，保留Mesh、路线朝向、Transform与村庄布局；早期“先A后Top/End、B后置”建议已被用户直接组图要求取代。新一轮额外收费生成仍需另行提示词审批，本轮未追加生图。
