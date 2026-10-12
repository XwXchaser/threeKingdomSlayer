---
id: kd_9fde5d4a-d068-4725-a171-de7fb22e652c
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1 v2重构思考：固定双转角、右官道、左绿色石路与无字箭头路牌

## 状态与本轮边界

用户明确否决J1 v1，随后v2获授权生成，HTTP200，原图/请求/响应已保存、验证并附图。用户进一步指出：**左侧在J1尚不该出现村庄，只能有篱笆等装饰**。v2当前为需局部修订候选，不是已验收版本。最新v3修订准备见 `plan/j1-scene-reference-v3-left-road-refinement.md`；本文件保留固定几何证据及v2实际请求历史，未改场景/代码/路线或部署素材。

N1（战斗）→E0（战斗）→J1（分叉选择）的拓扑决策保持，E1已取消但旧实现中的停点引用和占位仍未迁移。下文提示词保留本次v2批准版本，仅在用户再次授权时才能重做；不以API成功自动推进拆素材或部署。生成结果见第10节。

## 1. v1失败不是需要多摆几件道具，而是空间定义错误

- 旧prompt要求“中央官道直行，左侧旁路”，将画面右侧只定义为官道路肩；结果成为一条黄土主路配左侧村屋，不符合固定左右两支。
- 旧prompt明确禁止“左右对称Y叉”和“右转”，与当前路线几何冲突。几何对称和美术镜像是两个概念：允许左右固定对称缓弯，不把内容物镜像。
- 两侧地面都以黄土为主，没有落实左侧绿色石头路的材料识别。
- 旧prompt禁止所有箭头，错误排除了用户需要的实体无字箭头路牌。
- 新参考图应由固定几何约束派生，不修改道路来迎合自由生成图；v1仅保留请求/响应/原图作未通过候选，不作v2构图主参考。

## 2. 实际角度与路径证据（只读核对）

| 项目 | 当前值/事实 | 含义 |
|---|---|---|
| `YScrollSample.turnAngle`场景值 | 45° | 当前J1两支固定最终朝向 |
| `YScrollSample.Evaluate` | Left sign=-1；Right sign=+1 | 左终态-45°，右终态+45° |
| 曲线起点 | 路线距离190 | J1当前停点/来路正向仍为0° |
| 实际运行曲线半径 | 28.1595573 | 两侧沿圆弧逐渐转向，不是尖角折线 |
| 45°圆弧长度 | 约22.1165 | 转向期间朝向连续变化，不是一开始就45° |
| 圆弧末端（路线根坐标） | 左(-8.2477,0,209.9118)，右(8.2477,0,209.9118) | 后接左右±45°直线段 |
| Shader路网 | `_JunctionDistance=190`、`_CurveRadius=28.1595573`，用同一`_YSAngle` | 美术道路不得画成右0°直行 |
| 方向规则SO | `ScrollWorldRouteRules_Trial.asset`：leftAngle=-45、forwardAngle=0、rightAngle=45 | 单一路线不能自由覆盖方向 |

方向规则SO中的defaultTurnRadius=12属于另一套authoring默认参数，当前Y运行实际半径为28.1595573；不能混用。固定±45°是世界空间终态朝向，不是强迫图上道路画成左右屏幕45°直线。应保留实际透视和远端下弯机位，路面开口沿实际弧线显露。

只读现场：Battle+Y两场景clean，progress0、viewCamera=None、scenery297；本轮未修改这些状态。Host左右战斗引用仍SmallBattle、按钮仍山谷/营地，后续只需按右官道/左村庄绑定内容与按钮；当前Y左右转角本身不需为J1参考图改成Forward+Left。

## 3. 新构图：几何左右固定，内容物左右有差异

```text
          左：村庄 / 绿色石头路       右：官道 / 暖黄压实土路
                    ↖ -45°         +45° ↗
                      \           /
                       \ 无字路牌 /
                        \   ↔   /
                         \     /
                         E0来路
```

### 左支：村庄绿色石头路

- 村路从共同来路连贯变为灰绿色石板/不规则石块铺路，沿-45°缓弯向左前方延伸。
- “绿色”以青灰/灰绿石块、缝隙苔草和路肩植被共同表现；仍能看清石材边界，不是只有黄土旁边摆绿草，不画荧光绿沥青或现代园林。
- 路肩和远端为村屋、篱笆、矮土石墙及少量树/灌木；侧景沿左道路走势组织，不能堆在路口将路面盖住。
- 明亮但有战乱痕迹：屋舍少量破损、个别淡烟/零星残骸即可，不把村庄画成巨型火海或阴暗密林。

### 右支：官道

- 较开阔的暖黄压实土路，沿+45°缓弯向右前方延伸；不是中央直行主路，也不新增第三个出口。
- 地面用车辙、砂石和低对比路肩表达运输官道，保持与E0黄土世界一致。
- 侧景少量辎车、木箱、低残墙、无字残旗；远方道路更开阔，物件在外肩，不堵路也不形成营地大门。

### 岔路中央：无字实体箭头路牌

- 路牌处于两支已经分开后的中央分隔地带，是中景选择地标，不插在共同来路或任一路面的通行中心。
- 建议形式：一根粗粝木柱、两块箭头形木牌，分别朝左路/右路。无文字、数字、阵营徽记，不画浮空UI箭头、发光导航或现代交通牌。
- 视觉大小服务于识别，两箭头能读清，背景留低对比空隙；不画成巨型跨路门架。
- 几何位置建议（尚未部署）：相对J1约(0,0,20)，即当前根坐标(0,0,210)。计算至左右路线中心最近距离约6.38，扣道路半宽2.45仍约3.93单位净距。此为构图建议，具体位置/体量需后续Game验收。
- 路牌后方不是第三条继续前进的道路；以低矮草/土肩和低对比远景收束，不用巨峰/巨石强制堵路。

### 镜头/空间连续性

- 使用当前Battle机位：Camera(0,3,-10)、俯角18°、FOV60、1:2画幅、环境地面y=-1.8、远端弯曲起点18。
- 来路由下方中央进入，在中景分成两支，两侧路面入口和缓弯都可读。下半幅保留来路/选择UI净空，但不能以“中央安全区”理由把两支画没了。
- 原E0门垣已在身后；不生成前方第二座完整城门或回头视角。蓝天、浅云、冷灰山体延续E0，左绿石路是局部材质变化，不是全画面突然换季/换时间。

## 4. 几何示意与下一轮参考图分工

已在Library临时目录产出并回读两类几何示意，不是收费生图，不是已验收的美术图：

- 俯视验证：`Library/Locus/tmp/j1-rethink-v2/j1_fixed_fork_topdown_v2.png`，显示实际圆弧中心线及±45°终态。含分析文字，**不作为最终图中的文字或直接上传图片**。
- 透视几何约束：`Library/Locus/tmp/j1-rethink-v2/j1_fixed_fork_perspective_v2_checked.png`，1024×2048，依据当前镜头与Y投影，绿色/黄土地面分区和无字路牌只作布局示意，非美术质量标准。石缝、色块边缘、天空均为分析占位，不能烘成成品。
- 早期 `j1_fixed_fork_perspective_v2.png` 因远地面遮挡/重复色块绘制错误已弃用，不作为生成输入。参数/记录在同目录json中。

建议v2提交的参考图顺序：

| 顺序 | 参考文件 | 唯一职责 |
|---|---|---|
| Image 1 | `Library/Locus/tmp/j1-rethink-v2/j1_fixed_fork_perspective_v2_checked.png` | 首要几何/构图参考：保留来路、两支口、固定缓弯方向与中央路牌落点；不复制分析图平涂/锯齿/材质线条 |
| Image 2 | `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/E0_ApprovedSceneReference_v3.png` | 环境连续性：蓝天冷灰山/暖黄土、石木材家族及日照；不复制前方城门 |
| Image 3 | `C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png` | 最高画风补充：大体块、高清像素色簇、厚重三国材质；忽略人物、武器、大城门、旗上符号 |

上一轮E0 Game截图保留用于我们比对镜头，不继续把一张中央直路图作为强构图输入。J1 v1是失败记录，不作为v2几何主图。几何引导图需要用户认可；认可后才随新提示词提交。

## 5. v2完整中文提示词（待批准，不执行）

以图1为首要几何构图约束，绘制一张三国战场连续卷轴游戏的J1分叉节点完整场景参考图，竖屏1:2、1024×2048、完整不透明PNG。图1是分析布局图，只保留共同来路、左右两支道路的开口/缓弯走向和中央路牌落点，不复制平涂色块、粗糙边缘或分析用石缝条纹。图2继承蓝天浅云、冷灰蓝远山、暖黄土及石木材质和日照连续性，不复制完整前方城门。图3只参考高清像素化的大体块、锐利像素色簇、材质体积与三国战争气氛，忽略其中人物、武器、战斗特效、旗上符号和大城门构图。

镜头在E0门垣之后，固定低机位观察，无人物身体，保持当前部署镜头的高度、俯角、地平线和开阔前景。共同黄土来路从下方中央进入中景，明确分成且仅分成左前和右前两条可通行道路。两条道路先沿圆弧平滑分开，世界方向最终分别相对来路为左-45°和右+45°；以图1透视走向为准，不把数字当成屏幕上的45°斜线，不画一条中央主路配侧支路。分叉后中央没有第三条直行道路。

左前方是村庄绿色石头路：清楚可见的青灰/灰绿色不规则石板与石块铺面，石缝嵌少量苔草，路肩逐渐长出低矮草和灌木，整条路沿左弯通向村庄。屋舍、矮篱笆、低土石墙和稀疏树木沿道路外缘递进，村落由中景向左远方延伸，不堵住路口。它是战场世界中的受战乱影响村庄，不是现代园林、童话花园、荧光绿路或阴暗森林；只有少量破损、淡烟和零星残骸。

右前方是官道：暖黄压实土路，低对比砂石和车辙沿+45°右缓弯延伸，视野比左村路更开阔。道路外肩少量古代木质辎车、木箱、矮残墙和无文字破红旗，表达撤退运输路线；不做营地门楼、不复制城市正门，不在道路上横置拒马、车辆或大石头。

分叉中间的分隔地带立一根粗粝木柱，装两块明确箭头形木路牌，一块指向左村路，一块指向右官道。木路牌完全无文字、数字、徽记和涂画，是静态世界实体，不是UI图标、不发光、不浮空。路牌略靠近两路分开的中景，作为视觉选择焦点，与两条道路均保持通行净空；路牌后是低草/黄土肩和远景，不是第三条路，也不是巨型堵路山峰。

整体延续E0的明亮蓝天浅云、冷灰蓝山脉、暖黄土；绿色只作为左村路石材与植被的识别色，右官道保持暖土色，两侧材质差异明确但来自同一连续环境。远/中/近层有厚重体积、干净轮廓和清楚像素色簇，不使用照片写实、光滑3D、复古低分辨率或普通插画马赛克。共同前景道路开阔，分叉路面连续、左右入口第一眼可辨。只画静态环境和上述无字箭头路牌：无人物、士兵、玩家身体、手臂、武器、敌阵、斩击、HUD、文本、字幕、浮空箭头、水印或黑边。

## 6. Complete v2 English prompt (pending approval, not submitted)

Create one complete environmental reference concept of the J1 road fork for a continuous scrolling Three Kingdoms battlefield game, vertical 1:2, 1024x2048, opaque PNG. Image 1 is the PRIMARY GEOMETRY AND COMPOSITION guide: preserve the shared approach, both branch mouths, their curved directions, the median and the physical signpost location. It is only an analytical layout; do not copy its flat fills, coarse edges or placeholder stone-joint stripes. Image 2 supplies E0's blue daytime sky, cool gray-blue mountain family, warm ochre soil, stone-and-timber materials and continuous sunlight, not another complete gate ahead. Image 3 supplies only the high-definition pixel-cluster art language, substantial volumes and Three Kingdoms war atmosphere; ignore its people, weapons, combat effects, heraldry and monumental gate composition.

The empty low observation camera is beyond the E0 gate, with deployed camera height, mild downward view, horizon and broad shared foreground retained. The ochre approach enters from the lower center and divides clearly into EXACTLY TWO traversable routes, one curving forward-left and one curving forward-right. Both turns are smooth circular arcs, ending at fixed WORLD-SPACE headings of -45 degrees left and +45 degrees right relative to the approach. Match the perspective geometry of Image 1; do not treat these numbers as screen-space 45-degree lines. Do not draw a straight central main road with an incidental left side lane. There is NO THIRD STRAIGHT EXIT beyond the fork.

The LEFT branch leads to the VILLAGE and is a GREEN STONE ROAD. Show actual blue-gray and muted gray-green irregular stone paving with visible stone edges, a little moss or grass in the joints and low vegetation along the shoulder. Follow the left arc into a village of small thatched houses, low fences, earth-and-stone wall remnants and sparse trees placed outside the lane. The village recedes toward the left distance while its road mouth stays visible. This is a lightly war-damaged village in the same battlefield world, not a modern park, fairy-tale garden, neon-green surface or dark forest. Only restrained damage, thin smoke and a few remnants.

The RIGHT branch is the OFFICIAL ROAD: warm ochre compacted dirt, understated gravel and wagon tracks following the right arc, with a more open distant view. A few ancient wooden baggage carts, supply crates, low ruined walls and plain torn red flags belong to its outer shoulder. No camp gate, repeated city gate, road-spanning barricades or carts blocking the lane.

In the MEDIAN between the separated routes, place a rough wooden post with TWO CLEAR ARROW-SHAPED WOODEN DIRECTION BOARDS: one points toward the left village lane and one toward the right official road. The physical boards are completely unlettered, without numbers, heraldry or painted symbols. This is an anchored world object, not floating UI or a glowing navigation icon. Place it in the readable midground with clearance from both roads as the choice focal point. Behind it use only low grass, an ochre shoulder and distant landscape: no third roadway and no giant peak as a roadblock.

Keep E0's bright daytime blue sky, pale clouds, cool gray-blue mountains and warm ochre ground. Green is a local identifying material/vegetation accent of the left village branch while the right road remains warm dirt; the two routes still belong to one continuous world. Build far, middle and near layers with substantial material volumes, clean silhouettes and deliberate sharp high-definition pixel clusters. No photograph, glossy 3D, low-resolution retro art or mosaic-filtered illustration. The shared lower approach is broad and empty; both branch surfaces are connected and immediately readable. Environment and the required textless physical arrow sign only: no people, soldiers, protagonist body, hands, arms, weapons, enemy formations, slashes, HUD, text, subtitles, floating arrows, watermarks or black borders.

## 7. 新负面约束（不沿用v1的“禁止所有箭头/对称Y叉”）

> **重要：下列负面词明确禁止的是“直行官道+左旁路”和错误的屏幕几何，不禁止正确的固定双转角，也不禁止本轮必须出现的两块实体无字箭头路牌。**

中文：中央保持直行官道且只在左侧分出旁路、右路不转弯、三条及以上出口、错误的任意方向或左右交换、锐角折线急弯、只有黄土路旁摆绿草而没有绿色石铺面、荧光绿色道路、现代柏油、现代交通牌、路牌带文字/数字/阵营徽记、无路牌、只有一块箭头牌、浮空UI箭头、发光导航、路牌插在任一路中心堵路、两路不连贯、巨大中央山峰/堵路巨石、第三座前方城门、回头镜头、阴暗森林/童话花园、左右内容机械镜像、下半幅塞满人物/车辆/拒马、士兵/玩家身体/手臂/武器/战斗特效/UI文字、水印、黑边。

English: a straight central official road with only a left side lane, no right branch, three or more exits, arbitrary or swapped headings, abrupt angular kinks, dirt road with green grass but no actual green stone paving, neon-green road, modern asphalt, modern traffic signs, lettering/numbers/heraldry on the signboards, missing signpost, only one arrow board, floating UI arrows, glowing navigation, signpost blocking either lane, disconnected road mouths, giant central peak or road-blocking boulders, another complete gate ahead, backward-looking camera, dark forest or fairy-tale garden, mechanically mirrored props, crowded foreground, people, soldiers, protagonist body, hands, arms, weapons, combat effects, HUD, text, watermark, black borders.

## 8. v2已批准参数与验收门槛

本次实际批准并使用：`/v1/images/edits`、gpt-image-2.5-sunburst、1024x2048、quality=high、input_fidelity=high、output_format=png、n=1、不请求透明。用户是在新v2准备后另行授权生产，并未复用v1授权。Image[]顺序为checked固定几何透视图、E0场景参考、新画风战场概念图。

验收顺序：

1. 几何先行：来路只分左右两支，各沿已定圆弧达到±45°，无中央直行第三路。地面走向在相机透视下吻合引导图，不只口头写角度。
2. 左右内容：左是村庄+绿色石头路，右是官道+暖黄土/运输线索，第一眼可读；左不能只是房子在黄土路旁。
3. 中央实体无字双箭头路牌清楚可见，位置在分隔地带、非路面中心，无文字/数字/符号/发光UI。
4. 同一世界的蓝天、山体与光照连续，原城门已在身后；物体尺度和低机位保持，不用抬升镜头改成俯视图。
5. 完整参考图用户验收后才拆分成道路石材/官道/路肩、村屋篱笆、中央箭头路牌、辎车箱物、植被等可部署素材。中央路牌最终是独立可选对象，不烘进天空或UI。

## 9. 待工程实施（本轮不执行）

- 保留Y路线固定±45°/现有圆弧，不按上一版计划改右路为Forward；只在未来专项授权时变更几何参数。
- Host/UI内容改右官道、左村庄，并与真实choice/target/战斗资产和按钮位置一起验证，不能仅换文字；现有SmallBattle和山谷/营地仍是占位。
- 左村路绿色石铺面需独立材质分区/纹理，当前地面三层材质并未区分左右路面，不能只在概念图上改色却声称已部署。
- E0转战斗、移除旧E1停点仍为待实施项，N1→E0与门垣1.2倍/草排序基线保持。

依据：`Assets/Experiments/CurvedScroll/Scripts/YScrollSample.cs`、`Assets/Experiments/CurvedScroll/Scripts/ScrollWorldRouteRules.cs`、`Assets/Experiments/CurvedScroll/RouteTrialData/ScrollWorldRouteRules_Trial.asset`、`Assets/Experiments/CurvedScroll/Shaders/YGroundThreeLayerE0Route.shader`、`design/changbanpo-six-node-level-design.md`、`design/art-style-guide.md`、`skill/workflows/route-spatial-reference.md`。

## 10. v2生产与验证记录（2026-10-10，待用户验收）

- 用户明确授权“发起生产”。一次 `/v1/images/edits` 请求，HTTP200，1张结果，耗时约103.3秒，无自动重试。
- 英文提示词使用第6节完整正文加第7节最新英文负面约束，SHA-256=`427690cd997901584f0e978a6b319ab8351c4b119639acd941e8b6eb5fe3e8f0`。批准的中文词、负面词、输入职责/哈希也保存在请求记录。
- 原图：`C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v2.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v2.request.json`。
- 原始响应：`C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v2.response.json`。
- 验证状态：`C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v2.status.json`。
- 文件验证：PNG可读取，实际1024×2048、RGB/Color Type2、不透明，4230080 bytes；SHA-256=`dcdf229317158bd088490a7fcc01b678086b39bc45710ceea2a86a8304fde2e2`。四文件均存在并回读，三张输入哈希与提交记录一致，v1原图仍保留。
- 视觉观察：左侧村庄与带苔草的灰绿石铺面、右侧暖黄官道/车辙、中央实体无字双箭头路牌均已出现；蓝天远山及黄土世界延续，无可见人物/文字/HUD。内容方向较v1改进。
- 限制：单张生成图不能证明两支道路精确对应运行几何的±45°终态；需用户结合几何参考确认岔口可读性与曲线形状，必要时另行几何比对。不能把HTTP成功或“写了45°”当作空间验收。
- 用户回评：左侧村庄出现过早，应只保留篱笆等路侧装饰；v2需修订，后续不继续使用要求J1直接显露屋舍的旧prompt。v3拟以v2单图有限编辑，仅替换左侧建筑，绿色石路、道路曲线、中央无字路牌、右路和整体光照保持；准备稿见 `plan/j1-scene-reference-v3-left-road-refinement.md`。尚未再次生成/导入Unity/拆素材/修改节点流程。
