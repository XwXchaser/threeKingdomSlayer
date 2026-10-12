---
id: kd_92c740d1-481c-43a1-9d64-d73fa078d3b3
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
summary: J1 v4已按四图与max参数生成但用户验收不通过：左右道路在转弯后被错误画回远方，违背实际−45°/+45°路径；原图保留但不得作J1几何或部署依据，未部署Unity。
---

# J1 v4 同机位分叉道路场景参考图：生图准备稿

## 本轮范围与状态

用户先指出当前J1路牌过大、场景空旷、旧J1参考图不适合当前分支道路，随后要求“请准备生图工作”。准备阶段完成本地底稿、布局示意、比例板、完整提示词和请求规格；随后用户明确要求“发起生成”，按本稿提交一次并保存原图。**用户已明确验收不通过：生成图把左右道路画成转弯后重新回到远方的道路，违背当前J1实际路径；没有修改/保存正式Unity场景、材质、Sprite或代码，未部署生成图。**

v4原图、请求和响应保留作失败记录，不覆盖旧v1/v2/v3，也不再作为J1几何、拆素材或部署依据。

## 准备包与交付路径

准备包：`C:/Users/Administrator/Pictures/gptGen/j1_geometry_locked_reference_v4/`

- `references/`：候选生成输入，四张图均已回读、验证实际尺寸并记录SHA-256。
- `review/`：布局标注、尺度板、用户截图、纯场景基线和左右转弯几何检查，仅供审阅。
- `review/rejected_intermediate/`：首轮旧树素材布景试验；因画风干扰及左路遮挡风险弃用，不上传、不作为部署依据。
- `reference_manifest.prepared.json`：实际输入路径、角色职责、尺寸、哈希和本地准备状态。
- `j1_scene_reference_v4.prompt.zh.txt` / `j1_scene_reference_v4.prompt.en.txt`：完整待审提示词，含对应语言负面约束。
- `j1_scene_reference_v4.request.prepared.json`：不含密钥的请求准备记录，不是已提交回执。

未来获批生成的主候选拟保存为：`C:/Users/Administrator/Pictures/gptGen/j1_geometry_locked_reference_v4/j1_scene_reference_v4.png`。同基名保存 `.request.json`、`.response.json`、`.status.json`，不覆盖本轮准备文件。

## 锁定的J1规格

- Active宿主：`Assets/Scenes/Battle.scene`；Additive环境：`Assets/Experiments/CurvedScroll/YJunctionSample.unity`。
- J1节点与几何分叉距离190，路线根渲染高度-1.8。
- Battle Camera position=(0,3,-10)、pitch=18°、FOV=60，输出1024×2048、1:2。
- 弯道半径28.1595573，左右最终朝向-45°/+45°，先圆弧后直线；不能把世界角度直接画成屏幕45°折线。
- 当前道路半宽2.45，地面在相机前18单位以后向下弯。
- 可见两支从共同开口向两侧离开视口，不强行添加两条在画面内部一直通到远山的道路。
- N1/E0的明亮日间蓝天、冷灰远山与暖黄土光照延续。
- J1左侧尚未抵达村庄，不能出现房屋、屋顶群、村庄天际线或建筑式塔台。

此次底稿来自隔离Unity预览场景：复制已读Renderer与相机设置，使用真实YScrollSample/Shader投影；渲染后销毁预览对象，正式场景没有临时相机绑定或progress变更。两侧几何体块进行了道路中心距离及保守脚点/体宽净空检查（最小约0.55单位）；这不替代最终素材Alpha轮廓和行进遮挡验收。灰块只说明可部署占位，不是最终造型。

## 参考图上传顺序与唯一职责

| 顺序 | 文件（准备包references目录） | 职责 |
|---|---|---|
| 1 | `01_j1_actual_geometry_190.png`，1024×2048 | 最高优先级机位与真实地面投影：地平线、下半幅、分叉开口、左右铺面占位。可重画细节，不移动道路。 |
| 2 | `02_j1_massing_and_small_sign_guide.png`，1024×2048 | 布景占位和较小路牌体量：左外肩、中央低地/灌木/少量树冠、右运输侧带。灰体块必须替换为自然美术，不复制其几何轮廓。 |
| 3 | `03_e0_approved_light_and_materials.png`，1024×2048 | E0连续环境的蓝天、冷灰山、暖黄土、木石用色及日照；不复制城门、军事障碍或其直路构图。 |
| 4 | `04_original_battlefield_style.png`，1024×1536 | 原画风的大体块、高清像素色簇和厚重材质；不转移人物、兵器、战斗特效、徽记、旗阵或大城门。 |

上传图均无分析文字、路径色线或角色比例尺。用户带HUD截图仅留在review中，不上传；旧J1 v3因透视不符本次不上传；角色比例板仅内部校准，不上传。

## 路牌尺度与构图目标

路牌实际Alpha≥32主体高833px，PPU100、当前Scale0.6 → 可见高4.998世界单位；1011站立Alpha可见高约3.1136。准备图中路牌Scale0.3 → 2.499单位，约为同深度角色的80%。这是拟议参考图尺度，没有修改正式路牌。

- 路牌仍处于中央分隔区，根坐标约(0,0,207)，不插在任一支路中间。
- 两块实体木箭头保持可读，无字、无数字、无阵营符号；路牌在1024×2048中高度约7–9%画幅，避免再变成巨大门架。
- 中央后方由低土肩、成组灌木与少量疏树轮廓收束；路牌后留低对比空隙，不产生第三道路，不用巨峰堵路。
- 左支：短段低篱笆、低土石肩、成组灌木和疏树沿弯道递进；中景能够辨认村路身份，但无村庄建筑。
- 右支：运输车/箱物作为一处主体，低残墙/土肩/少量残旗构成另一层；其后更开阔，但不能又回到光秃平地。
- 近景：两侧低草、碎石与少量木件连接到中景，不把中央共同来路填满。

## 拟提交参数（待确认）

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
encoding: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x2048
quality: max
input_fidelity: high
output_format: png
n: 1
background: 不设置transparent；要求完整不透明场景图
image[]: 按上表顺序上传四张实际图片
prompt: 下方完整英文正向词与英文负面约束合并
```

仅一次候选请求，不自动批量、不自动重试。API Key已检查存在，仅记录存在性，没有输出或写入任何密钥。输入高保真和几何引导不能保证逐像素锁定，返回后仍须对齐检查。

## 完整中文正向提示词

制作一张三国战场连续卷轴游戏的J1分叉节点完整场景参考图，竖屏1:2，1024×2048，不透明PNG。目标是在现有实际道路机位中建立足够丰富的远、中、近景侧带，让左村路和右官道不用依赖巨大路牌也能读清。它是后续独立素材制作和Unity布景的构图母图，保持可实现的空间和尺度。

四张参考图职责严格分开。图1是最高优先级的真实Unity机位与地面投影：保留下半幅共同来路、地面消失位置、左右分支开口、缓弯轮廓、天空与地面的比例；不把图1改成新的摄影机或自由透视。图2只说明景物占位和缩小路牌的尺度，其中灰色体块是树冠、灌木、低土肩与运输侧景的占位，不是成品造型；将它们自然重绘成环境，不能复制灰色多边形或其机械排列。图3只继承E0的明亮蓝天、浅云、冷灰蓝山脉、暖黄土和厚重木石的同一日照/色彩家族，不复制正面门垣、军事拒马或直路构图。图4只参考高清像素化三国战场的厚重体积、锐利像素色簇、木石材质和冷暖组织，忽略其中人物、武器、飞箭、旗上图案与大城门。

必须保留图1真实道路几何。共同黄土来路从下方中央进入，在已有开口分成且仅分成左侧灰绿色石路和右侧暖黄官道；两支沿现有圆弧逐渐朝左右转开，世界最终朝向为左-45°和右+45°。这些是世界方向，不能改成图上45°尖角折线。当前卷轴远端向下弯，两支会向画幅两侧离开；依照图1的实际可见范围，不凭空拉出两条一直深入远山的长路，不移动分叉点或地平线，不增加中央直行出口。路面可提高石材与车辙的可读性，但地面覆盖、开口和道路净空保持。

岔路中央分隔地带是一片低矮、有体积的土石草地：低土肩、若干形状不同的灌木和少量疏树冠在路牌后方形成高低错落的中景轮廓，左右两路从这片陆地的两侧绕开。中央不成为第三条道路，也不堆巨大山峰、巨石、跨路高墙或密林堵住出口。两块木箭头后保留低对比空隙，不让树干或灌木吞掉箭头剪影。

中央保留一根实体粗木双箭头路牌，造型跟随图2的小路牌：可见高约为同深度成年游戏角色的80%，在全图中约7–9%画幅高度，远小于路侧树木，不是门架或巨型纪念物。左右箭头无文字、数字、涂画和徽记，分别朝两个出口，木柱脚点稳定落在两路之间的低土草地上。图内不画用于校准的角色。

左侧石路使用青灰/灰绿色不规则石板、清楚石边和少量苔草缝，暖土肩自然衔接。短段低矮乡间篱笆、成组灌木、低土石肩及稀疏树木沿左侧外肩形成近中远连续侧带；它们跟着左弯离开画幅，不横向排成一堵墙。J1尚未进入村庄：左路、左侧边缘和左远景都禁止出现民居、屋顶、完整村庄天际线、塔楼、门楼或建筑式望台。通过篱笆和自然路肩提示通向村庄的乡间路线，保持路面入口可见。

右侧保持较开阔的暖黄压实土官道，少量车辙和砂砾沿路组织。外肩有一处完整古代木质运输车与独立木箱的主体组合，另以几段低矮破土石墙、自然土垒、草石和少量无文字破红旗建立运输路线节奏。车辆、车辕、低墙和旗均在道路净空外，不横放堵路；远侧比左路疏，但具有连续体量，不能只摆一辆车就结束。不开设营地大门，不放第二座城门，不画人物、马匹或牲畜。

前景两侧用低草簇、碎石和少量断木连接中景，疏密不均但有设计；共同来路与左右路口保持开阔，不能把“丰富场景”解释成把小物件均匀撒满全部地面。远山与天空保持图1的可见比例，景物轮廓层层递进，主体用大体块而不是细噪点。画风为高清像素化三国战场动作美术，厚重、粗粝、明亮，木石有明确受光面与锐利色簇，冷山/灰绿植被与暖土同属于连续日间环境。整张图只有静态环境；无玩家身体、手、武器、士兵、HUD、文字、数字、浮空导航、说明线、标注框、水印或黑边。

## 中文负面约束

巨大路牌、路牌高于同深度角色、跨路门架、第三条中央直行道路、官道保持中央直行、将世界±45°画成屏幕尖角折线、移动分叉位置、改相机俯角/FOV、拉长道路透视、降低地平线以显露更多道路、把远端下弯改成平直无限道路；左侧村屋/屋顶群/村庄远景/建筑式塔台、第二座城门、营地大门、密林/大山峰/巨石堵路、路肩侧带变成横向连续高墙；车辆或车辕压入道路、左右完全镜像、只有路牌背后空无景物、草石均匀撒满中央；直接照抄灰色几何体、多边形灰块、旧低分辨率树素材、平涂模糊色块、普通插画马赛克、照片、光滑3D、童话花园、阴暗夜景、突然黄昏；人物、马匹、玩家身体/手/武器、士兵、HUD、字幕、文字牌、数字、徽记、现代交通牌、浮空箭头、彩色分析线、标注框、比例角色、水印、logo、黑边。

## Complete English positive prompt

Create one complete environmental reference concept of the J1 fork for a continuous scrolling Three Kingdoms battlefield game. Vertical 1:2, 1024x2048, opaque PNG. Build a substantial, readable environment in the EXISTING DEPLOYED ROAD VIEW, with coherent foreground, middle-ground and distant roadside masses. The left village lane and right official road must read even when the signpost is hidden. This image is a composition master for later independent asset production and Unity scene authoring; preserve physically deployable positions and proportions.

REFERENCE PRIORITIES: Image 1 is the highest-priority ACTUAL UNITY CAMERA AND GROUND-PROJECTION guide. Preserve the shared lower approach, the ground disappearance line, the visible branch mouths, their curved footprints and the sky-to-ground proportion. Do not invent another lens, viewpoint or perspective. Image 2 is ONLY a massing and SMALL-SIGN scale guide. Its gray polygons stand for foliage, shrubs, low earth shoulders and transport-side masses; redraw them as natural environment art, never as gray polygonal objects or mechanically repeated silhouettes. Image 3 supplies ONLY E0's bright blue daytime sky, pale clouds, cool gray-blue mountain family, ochre soil, substantial stone/timber materials and continuous sunlight. Do not copy its frontal gate, military barricades or straight-road composition. Image 4 supplies ONLY the high-definition pixel-cluster Three Kingdoms art language: weight, clear material volumes, deliberate sharp color clusters and warm/cool organization. Ignore its people, weapons, arrows, heraldic patterns and monumental gate.

KEEP THE EXACT ROAD TOPOLOGY AND PROJECTED FOOTPRINT FROM IMAGE 1. The ochre shared approach enters from the lower center and opens into EXACTLY TWO traversable branches: muted gray-green stone paving on the left and a warm ochre official road on the right. Both follow the existing smooth circular arcs toward final WORLD-SPACE headings of -45 degrees left and +45 degrees right. These are world headings, not screen-space 45-degree angular lines. The scrolling ground bends downward in the distance, and the branches leave toward the lateral edges of this narrow portrait view. Respect that visible extent; do not invent two long roads converging into distant mountains. Keep the fork location, horizon and ground coverage; do not add a third straight exit. Improve stone/track material readability without moving the road mouths or reducing clearance.

Between the branches, make a low, substantial EARTH-AND-VEGETATION MEDIAN. Low earth/stone shoulders, differently shaped shrub groups and a few sparse tree crowns rise and overlap behind the sign into a coherent middle-ground silhouette. The two lanes go around its sides. This median is land, not a third road, and must not become a huge peak, giant boulder, high crossing wall or dense forest blocking the branches. Leave a restrained, low-contrast opening behind the two wooden arrow boards so their silhouette remains readable.

Keep ONE physically planted rough wooden post with TWO unlettered arrow-shaped wooden boards, matching the SMALL SIGN in Image 2. Its visible height is approximately 80 percent of a standing game adult at the SAME DEPTH, about 7 to 9 percent of the full image height, clearly smaller than the roadside trees. It is not a gate frame or giant monument. The arrows point separately toward the left and right exits, without text, numbers, painted marks or heraldry. The post foot rests in the low earthen/grass median, outside both traveled lanes. Do not include the calibration character in the image.

The LEFT lane has readable irregular blue-gray/gray-green paving stones, clear stone edges and restrained moss/grass joints, naturally joined to a warm earthen shoulder. Organize short sections of low rural fence, grouped shrubs, low earth-and-stone banks and sparse trees into connected near, middle and farther roadside layers FOLLOWING THE LEFT ARC out of the portrait frame. Do not line them up horizontally as a solid wall. J1 is still BEFORE reaching the village: no houses, roofs, village skyline, tower, gatehouse or architectural lookout may appear along the left edge or left distance. Convey a rural lane leading toward a village through fences and natural shoulders alone; keep its mouth visible.

The RIGHT lane remains a more open warm ochre compacted-dirt official road with understated gravel and wagon tracks. Place one clear ancient wooden transport-cart and separate supply-crate grouping on its outer shoulder. Other layers use a few low broken earth/stone wall fragments, natural earth berms, grass, scattered stones and restrained PLAIN TORN RED FLAGS. All cart shafts, walls and flags remain outside road clearance. The farther side is sparser than the left but has coherent volumes; do not leave an isolated cart against empty ground. No camp gate, second city gate, people, horses or livestock.

Link the near shoulders to the middle-ground with low grass clumps, small rocks and a little broken timber. Use intentional irregular grouping, never evenly scatter small props over the entire ground. The shared foreground and both exits remain open. Preserve Image 1's visible sky and mountain proportion, and layer environmental silhouettes through depth rather than adding uniform noise. High-definition pixelated Three Kingdoms battlefield action art: substantial, rough but bright; clear lit material planes and sharp deliberate color clusters. Cool mountains and muted gray-green vegetation coexist with warm soil in one continuous daytime scene. STATIC ENVIRONMENT ONLY: no protagonist body, hands, weapons, soldiers, HUD, words, numbers, floating navigation icons, guide lines, annotation boxes, watermarks or black borders.

## English negative constraints

Oversized signpost; sign taller than a same-depth standing character; road-spanning gate frame; third straight central road; straight-ahead official road; screen-space 45-degree angular fork; shifted fork; changed camera pitch or field of view; elongated road perspective; lowered horizon to expose extra road; flat infinite road replacing the downward ground bend; left-side houses, roof clusters, village skyline or architectural lookout; another frontal city gate; camp gate; dense forest, giant mountain, boulder or high wall blocking an exit; horizontal continuous scenery wall; cart or shafts inside road clearance; mechanically mirrored left/right decoration; sign against an empty background; evenly scattered props covering the central ground; copied gray polygon guide objects; obsolete low-resolution tree silhouettes; flat blurry color blocks; mosaic-filtered illustration; photograph; glossy 3D; fairy-tale garden; dark night; sudden sunset; people, horses, protagonist body, hands, weapons, soldiers, HUD, captions, text signs, numbers, heraldry, modern road signs, floating arrows, colored analysis lines, annotation boxes, calibration character, logo, watermark, black borders.

## 生成后验收标准

1. PNG签名、1024×2048实际尺寸、完整可解码、不透明场景输出；保留原始请求/响应/图，不以HTTP成功代替验收。
2. 与图1叠图检查共同来路、分叉开口、地面消失位置和左右道路投影；不同角度/更长道路透视即不通过。
3. 遮住路牌后仍能读出左石路与右官道；中央由低地形/植被收束，不出现第三出口。
4. 路牌位置和体量接近图2：完整两箭头可读，不再支配环境，不把80%角色尺度只解释成缩小柱子而保留巨大木牌。
5. 左侧无村庄建筑；右侧为运输侧带而非营地/第二城门；景物都能映射到相应路肩或中央分隔带。
6. 近中远存在主次与体积：草石连接形体，非散点填空；道路净空没有被大物件侵入。
7. 把最终参考图缩到用户截图约319×632检视路线和路牌可读性。尺寸比例/像素统计仅辅助，用户目视验收为最终判断。
8. 单张构图通过后才拆素材；左右转弯图本轮只做几何净空审阅，不自动追加收费成图或部署Unity。

## 本轮保护与状态验证

Battle/Y均为Edit Mode、dirty=false，progress=0、right=false、root position=(0,0,0)、viewCamera=None。六个保护文件的SHA-256在准备前后一致。没有修改正式路牌Scale0.6，没有改道路Shader/材质、Node_J1停点或Battle相机。全图输入和比例参考只存在于本地准备包及Library临时目录。

## v4一次生成结果（2026-10-12本机时间）

用户明确要求“发起生成”，按已审准备稿提交一次 `/v1/images/edits`，四张实际image[]按原顺序上传；sunburst / quality=max / input_fidelity=high / 1024×2048 / PNG / n=1，不请求透明，没有重试。

- HTTP200，1张结果，请求/下载/PNG头校验合计约163.05秒。
- 原图：`C:/Users/Administrator/Pictures/gptGen/j1_geometry_locked_reference_v4/j1_scene_reference_v4.png`。
- 请求：同目录 `j1_scene_reference_v4.request.json`；响应：`j1_scene_reference_v4.response.json`；状态：`j1_scene_reference_v4.status.json`。准备稿和输入图保留。
- 实际PNG=1024×2048 RGB、Color Type2、4133062 bytes，Pillow完整解码通过；SHA-256=`a67f41dd27a6218681fbeaaabb66d0f94203759c003f13f495d783bd9e0b9762`。
- 实际英文prompt与prepared请求一致，prompt SHA-256=`8be6fee146bf8c092243afd6bd69a8bbe271d0b9e39cf65fd53aebda1788792f`；四张输入哈希一致，六个保护项目文件没有变化。
- 原图已直接read附图；另存 `review/j1_scene_reference_v4_game_size_preview.png`（320×640）及 `review/j1_scene_reference_v4_geometry_comparison.png`（底稿/候选对照），不覆盖原图。
- 视觉观察：路牌比现有巨型路牌缩小；中央灌木、石块与稀疏树形成分隔景观，左侧篱笆石路及右侧运输车/箱物/低残墙/红旗较完整。未发现明显左侧民居/屋顶或HUD。
- 用户验收结论：**rejected**。失败原因不是一般的局部透视重绘，而是道路拓扑/方向错误：当前Unity路线在圆弧结束后继续保持左−45°、右+45°，而v4把两条路画成向远处重新回正，复现了旧J1参考图的误导。
- 原图保留，状态文件已标记 `generated_rejected_by_user`；`unity_deployed=false`，不允许作为后续J1布局输入；本轮不自动重试或追加收费请求。

## 中断恢复后的后续准备

v4拒绝状态、唯一一次请求和原PNG哈希已再次核对。后续已另建 `plan/j1-no-return-geometry-v5-preparation.md`，准备包在 `C:/Users/Administrator/Pictures/gptGen/j1_no_return_geometry_v5/`：用当前路线中心线及道路半宽生成解析道路带和等比例俯视拓扑，明确一次圆弧后固定世界−45°/+45°、向左右出画且不回正。v5完整中英文词、五图职责和max参数已落盘，远程请求0、未获新批准。v4原图未被复制进v5作为几何输入，正式Battle/Y及道路资源未改变。
