---
id: kd_b90a4cc1-dbec-47a4-88b3-85f2116830fa
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 批次 A：石拱与左右残墙墩生成词

## 1. 状态与任务边界

用户已明确验收 E0 v3：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v3.png`。本批以它作为 E0 建筑形体和材质关系的内容基准，不因文件名仍含 R1 而要求先重新生成另一张 E0 图。

用户已根据本文批准发起生成。**A01、A02 v1此前获用户验收，原图保留；A03 v1因左右同形且偏离E0参考未通过。用户随后明确要求“开始纠正并重制”，A03 v2已按局部形体参考与材质小样重制、验证、附图，并获用户明确验收。** A03 v1历史词暂停复用；新方案与实际结果见 `plan/e0-a03-remake-prompts.md`。A01/A02未改图或回退，Unity资产/场景未修改，批次B/C未生成。

批次 A 包含：

| 编号 | 拟议物件名 | 主要职责 | 不包含 |
|---|---|---|---|
| A01 | E0_StoneArchGate_Front | 中央低矮石拱与两侧短石门墩的正面外立面 | 门楼、旗、长连接墙、门洞内部、道路 |
| A02 | E0_RuinedPier_Left_FlagSide | 左侧较高的夯土石基残墙墩，提供左旗的安装位置 | 旗帜、旗杆、墙顶焦木、长连接墙 |
| A03 | E0_RuinedPier_Right_Collapsed | 右侧较矮、较破损的夯土石基残墙墩 | 第二个门洞、木支架、长连接墙、大瓦砾堆 |

A01自带维持石拱结构所必需的短门墩；A02/A03是更外侧的墙墩，不是再制作一套中央门柱。墙体连接段属于批次B；独立瓦砾和焦木属于批次C；左旗独立控制。

## 2. 已阅读依据

- `Locus/knowledge/design/art-style-guide.md`：新画风权威入口，重点为第1、2、4、5、6、9、12节。
- `Locus/knowledge/design/changbanpo-six-node-level-design.md`：E0战后余烬、唯一出口和N1→E0→E1→J1职责。
- `Locus/knowledge/design/curved-scroll-travel-experiment.md` 第8.1、8.6节：概念图、部署规格、画风参考分工；不直接裁切完整参考图制作Sprite。
- `Locus/knowledge/skill/workflows/image-asset-generation.md`：图生图、原生透明、落盘、视觉验收、关键图直接附到对话和完整文件路径。
- `Locus/knowledge/skill/gpt-image-generation.md`：multipart image[]、模型、参数和失败处理。

新画风的执行结论：高清像素化三国战场，以大体块、阶梯轮廓、硬边色簇、明确受光面与暗部塑造重量；暖赭夯土对比灰褐/冷灰石材；明亮日间受光，暗部厚重但不整体压暗。不能以固定低分辨率、固定色数、最多两阶阴影或随机碎像素定义画风。建筑不继承角色Q化比例，也不移植角色甲胄、红缨和武器。

## 3. 参考图职责与实际核对

### 3.1 已验收 E0 v3：建筑身份与形体

`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v3.png`

- 已回读，实际1024×2048 RGB PNG。
- SHA-256：`44eb18f47a622e58f20e9c7fceafde20dad059e96ec5b5ad0cf1fc25ab1b923a`。
- 决定低矮浅石拱、暖赭夯土与灰褐石材、左右不对称破损、朴素外围残门垣身份。
- 只借用指定建筑的设计，重新绘制独立正交素材；不继承整张图的透视、天空、地面、远山、旗、木桩、草、车或烟。
- 不直接从RGB概念图裁切建筑当作最终透明素材。

### 3.2 战场概念图14：世界与绘制语言

`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`

- 已回读，实际1024×1536 RGB PNG。
- SHA-256：`643d50375a9fcdd2210aa40f43b5a70fb05099f01664de9ae54dfb0423016210`。
- 只决定建筑重量、高清像素色簇、硬边明暗、暖土冷石及日间光照表现。
- 不复制概念图里的宏伟完整城门、望楼、人物、兵器、箭矢、盾墙、旗阵和浓尘。
- 在绘制语言上以它及新画风规范为准；在E0结构身份和破损方式上以v3为准，两类优先级不能混成“哪张图统管所有内容”。

### 3.3 仅作本地比对，不上传建筑请求

- 概念图13：`C:/Users/Administrator/Downloads/微信图片_20260928101136_13_455.png`，1024×1536 RGB，已读；用于核对同一世界的冷暖、大体块和可读性。不上传，避免HUD、角色和斩击混入建筑。
- 1011 Idle首帧：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`，1108×1108 RGBA，已读；作为已部署绘制语言校验。不上传，避免甲片、头盔、武器进入石土建筑。
- 其余五帧基准见 `design/art-style-guide.md` 第2.2节；本批静态建筑不需要把全部角色动画帧作为生成输入。
- N1母图保留为整体空间连续性来源；本批已有v3，不再上传额外战场整图以牵引模型重建场景。
- 旧EnvironmentV2墙体、旧完整门楼与E0试摆截图只提供技术对照，不作新画风/新形体输入。

### 3.4 分件请求的上传顺序

| 请求 | image[0] | image[1] | image[2] | image[3] |
|---|---|---|---|---|
| A01 | 已验收E0 v3，指定中央石拱形体 | 概念图14，仅辅助绘制语言 | 无 | 无 |
| A02 | 已验收A01原生透明母版，锁定石材/光向/色簇 | E0 v3，指定左侧较高墙墩形体 | 概念图14，仅辅助绘制语言 | 无 |
| A03 | 已验收A01母版，锁定建筑家族 | 已验收A02，锁定夯土石基和相对墙高 | E0 v3，指定右侧塌损形体 | 概念图14，仅辅助绘制语言 |

上表保留A03 v1实际请求的参考职责，**不是下一版的执行方案**。用户指出左右近同形且偏离参考，A03 v1未通过。不能继续让A02充当右墩的形体/相对高度主参考；下一版须先在E0 v3中明确具体目标局部与单件边界，再提交新参考职责和中英文词审批。A01/A02原图和验收历史保留，A03不作为后批母版。

## 4. 拟议生成与交付参数

三件均各用一次独立请求，n=1，不把三件塞进一张场景图。首件通过后才制作后件。

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
reference upload: 按上表顺序重复image[]字段
model: gpt-image-2.5-sunburst
quality: high
input_fidelity: high
size: 1024x1024（每件）
background: transparent
output_format: png
n: 1（每次）
automatic retries: 0
```

- 1024×1024是每件物件的输出画布，不是把E0场景改为方图；建筑可见轮廓比例由物件职责决定，不拉伸主体填满画布。
- 质量档位不等于画风或保身份能力；选择high是常规候选规格，不擅自升到max或改变费用范围。
- 请求原生透明不等于实际获得透明；必须读PNG Color Type、Alpha及浅/深/土色底效果。若本次路由不提供有效Alpha，保留结果并停止后续收费请求，不自动重试、不自动去背、不将RGB称为透明。
- 外围至少32px真实透明安全留边，脚点完整；门洞内部必须贯通至底边的真实透明区域，不画白底、天空或黑色填充。
- 版本化目录：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/`；当前已保存A01/A02/A03 v1的原图、请求及原始响应；A01/A02已验收，A03待验收。
- 拟议文件：`e0_a01_stone_arch_front_v1.png`、`e0_a02_ruined_pier_left_v1.png`、`e0_a03_ruined_pier_right_v1.png`；每件同时保存同名`.request.json`和`.response.json`，不覆盖旧资产。
- 相同画布不保证相同绘制比例。A03须以A02为相对尺度参考并保留多余透明留白，不独立放大矮墙墩；最终仍要对比石块大小、主体高度、Alpha bbox并核对部署尺度。
- 后续授权导入时：环境Sprite沿用已查明的PPU100、Point、Uncompressed、Alpha Is Transparency、mipmap off、maxTextureSize2048；Pivot依据真实Alpha脚点设置。1011的PPU16不适用于本批环境Sprite。
- 材质、相机投影与世界Scale由实际Game检查确定，不从概念图推算、不机械复用旧试摆Scale；本轮不实施。

## 5. A01 中央低矮石拱门

### 5.1 中文完整提示词

```text
为高清像素化三国战场动作游戏制作一件可独立部署的建筑Sprite：E0外围残门垣的中央低矮石拱正面外立面。只生成这一件完整模块。

参考图职责：第一张是已验收的E0场景概念图，只取中央灰褐石拱和两侧短石门墩的设计，锁定低矮、朴素、战损后仍可通行的建筑身份；不复制场景取景、道路、远山或周边残墙。第二张只提供高清像素化三国战场绘制语言、建筑重量、大体块、硬边色簇和暖土冷石的明暗关系；不能把其中宏伟完整门楼、人物、军旗或战斗内容带入结果。第一张决定本物件形体，第二张辅助画风。

主体是灰褐色粗加工石块砌成的浅弧石拱，下方两侧为短而厚的石砌门墩；拱券石块沿浅弧排列，砌缝清楚，左右门墩竖直且底部共用水平基线，顶部保留一至两层不规则残石。保留少量崩边和缺角，结构仍连贯可辨认，不能把拱顶切断成悬浮石块。主体整体横向稍宽，参考可见宽高关系约1.5至1.7比1；门洞净宽约占主体宽度的55%至65%。这些为剪影目标，不为填满画布拉伸建筑。只含石拱及维持其结构的短门墩，没有高塔、屋顶、木质承重框架或两侧长城墙。

几何优先：严格正面正交建筑立面，竖边垂直，基线水平，不烘入三分之四斜拍、等距视角、消失点、可见地面或透视收缩。只画前立面，门洞内墙、拱顶内衬和门后出口另件制作。本图中央门洞从拱顶下缘贯通到画布下方的外部透明区域，必须是真实Alpha透明，保留实体拱顶和两侧门墩；没有门扇、栅栏、实心阴影板、道路、天空或山景堵住门洞。

沿用E0的明亮日间受光，暖色受光边对比偏冷灰褐石面，暗部有重量但不整体压黑。用清晰大体块、阶梯轮廓、成簇硬边明暗和少量有目的的凿痕塑造石材。裂纹、砌缝和颗粒服务结构，不铺随机噪点。不采用照片纹理、光滑3D渲染、普通插画加马赛克或低分辨率复古像素；不照搬角色Q版比例。

输出1024×1024真实RGBA透明PNG，主体和脚点完整入画，四周至少32像素完全透明留边。石体接近不透明，轮廓干净，无白边、灰边、光晕、投影底板或地面大平台。墙脚只可有很少的紧贴碎石，独立瓦砾另做。

禁止：完整场景、长连接墙、宏伟城门楼、屋顶、望楼、木立柱、木横梁、木斜撑、尖刺闸门、关闭门扇、洞内背景、实心黑色门洞、伪透明棋盘格、白底、灰底、人物、武器、旗帜、文字、徽记、UI、火焰、烟尘、草、车、裁断门墩脚点或拱顶。不要从概念图直接裁一块带背景的建筑，须重新绘制独立正交透明素材。
```

### 5.2 English full prompt

```text
Create ONE independently deployable architectural sprite for a high-definition pixel-cluster Three Kingdoms war action game: the low central stone-arch front facade of the E0 outer ruined gate.

REFERENCE ROLES: Image 1 is the approved E0 environment concept. Use only its central gray-brown shallow stone arch and short masonry jambs to define a modest, low, war-damaged but traversable structure. Do not copy the scene camera, road, mountains or neighboring wall runs. Image 2 supports only the Three Kingdoms pixel-cluster rendering language: architectural weight, strong large forms, hard-edged color clusters and warm-earth/cool-stone shading. Do not import its grand intact gatehouse, people, flags or combat. Image 1 controls this object's silhouette; Image 2 supports style.

Build a shallow segmental arch from rough-dressed gray-brown masonry blocks above two short, substantial stone jambs. Arrange the arch blocks along the shallow curve with readable joints. Keep the jambs upright with a common horizontal footing. One or two irregular stone courses may remain above the arch. Use limited chipped edges and missing corners while preserving a coherent load-bearing arch; no floating or disconnected arch crown. Aim for a visible overall width-to-height ratio of about 1.5 to 1.7, with the clear passage approximately 55 to 65 percent of the overall width. These are silhouette targets, not a reason to stretch the building to fill the canvas. Include only the stone arch and the short jambs required to support it: no tall towers, roof, timber load-bearing frame or long adjoining wall runs.

HIGHEST-PRIORITY GEOMETRY: a strict front-facing orthographic elevation, principal verticals upright and footing horizontal. Do not bake in a three-quarter camera, isometric angle, vanishing point, visible ground plane or perspective taper. Paint only the front facade. Tunnel side walls, the inner arch lining and the far exit will be separate assets. The central opening must be genuinely alpha-transparent from beneath the solid arch crown all the way through the bottom to the outer transparent canvas. Preserve the solid arch and jambs. No door leaves, grille, solid shadow panel, road, sky or mountain backdrop may fill the opening.

Keep the bright daytime lighting of E0, warm lit edges against cooler gray-brown stone planes and weighty shadows without globally darkening the structure. Use strong large forms, stepped contours, clustered hard-edged shading and a few purposeful tool marks. Cracks, joints and grain support the architecture instead of becoming random noise. No photographic texture, smooth 3D render, mosaic-filtered illustration or low-resolution retro pixel art. Do not apply character chibi proportions to the building.

Deliver a true RGBA transparent PNG at 1024x1024. Keep the complete silhouette and both footings inside the canvas with at least 32 pixels of purely transparent outer margin. Stone surfaces remain nearly opaque with clean cutout edges. No white or gray fringe, halo, display base, rectangular shadow or broad ground slab. Only a very small amount of stones touching the footing is allowed; independent rubble will be made separately.

No complete scene, long connecting walls, grand gatehouse, roof, watchtower, wooden load-bearing posts, timber lintel, diagonal timber braces, spiked portcullis, closed doors, backdrop inside the opening, solid black gateway, painted checkerboard, white or gray background, people, weapons, flags, text, emblems, UI, fire, smoke, grass or carts. Do not crop the jamb footings or arch crown. Do not crop a background-containing fragment out of the concept image; redraw a separate orthographic transparent asset.
```

## 6. A02 左侧高残墙墩

### 6.1 中文完整提示词

```text
为同一E0外围残门垣制作一件独立环境Sprite：左侧较高的夯土石基残墙墩。不是中央石拱门柱，不是完整城墙段。只生成这一件模块。

参考图职责：第一张是已经验收的A01石拱母版，锁定灰褐石块颜色、石块尺度、砌缝、日间受光方向和高清硬边色簇，石基必须属于同一建筑家族。第二张是已验收的E0场景图，只取左侧较高残墙墩的暖赭夯土、灰石基座、破损轮廓和厚重比例；它上方的旗帜、旗杆与残木不属于本物件，不复制道路、天空、草或瓦砾大地台。第三张只辅助新画风的大体块、材质重量、暖土冷石和像素绘制语言，不引入完整城楼、人物或战斗内容。

主体为厚实、较高但不修长的外围夯土墙墩。下部约三分之一至五分之二使用与A01同尺度的粗石砌基座，上部为暖赭黄夯土墙体，土石分界自然，墙面有几处清晰裂缝、掉角和露出断面的战损。顶部保留少量不规则残垛，轮廓有高低变化但主要承重墙体仍直立。正面主体可见宽高比约0.65至0.85，不把它做成细长独立塔楼。两侧保留可与后续低墙接续的简短断面，不伸出长墙。

墙顶留一个朴素的安装位置供后续独立左旗插入，但本图不生成旗帜、旗杆、底座或红布，也不生成墙顶木桩与焦梁。这些物件之后独立部署。墙脚只保留少量紧贴基座的碎石，不生成完整瓦砾堆、草丛或黄土地台。

严格正面正交立面，竖边垂直、脚点共用水平基线，不画宽大侧面、俯视顶面、等距视角、消失点或斜拍透视。体积靠大块受光面、深浅折面与裂口表现，整件外墙不倾斜。

明亮日间受光与A01一致。暖赭土墙与较冷灰褐石基清楚分材质，暗部厚重但不全黑。使用大体块、阶梯轮廓和成簇硬边明暗；夯土表现块状断面和克制颗粒，石基表现明确砌缝与石块厚度。禁止照片材质、柔化写实插画、随机噪点、马赛克滤镜、低分辨率复古像素和角色式Q版比例。

输出1024×1024真实RGBA透明PNG。完整主体和脚点入画，至少32像素外部透明留边，轮廓干净，墙体接近不透明。不独立改变石块绘制比例来填满画布。

禁止：第二个门洞、完整城门、屋顶、望楼、长连接墙、木框主体、旗帜、旗杆、墙顶焦木、红布、人物、武器、文字、阵营标识、UI、道路、天空、山、地面大平台、草、烟火、底板阴影、伪透明棋盘格、白底、灰底、光晕或脚点裁断。不要直接裁切原概念图，须重新绘制可独立摆放的正交透明墙墩。
```

### 6.2 English full prompt

```text
Create ONE independent environment sprite for the SAME E0 outer ruined gate: the taller LEFT rammed-earth wall pier on a masonry base. This is not a jamb of the central stone arch and not a complete wall run.

REFERENCE ROLES: Image 1 is the approved A01 stone-arch master and controls gray-brown stone color, masonry block scale, joints, daytime light direction and hard-edged pixel clusters. The base must belong to the same architectural family. Image 2 is the approved E0 scene and defines only the taller left pier's warm ochre earth, gray stone foundation, damaged silhouette and weighty proportions. Its flag, flagpole and timber remnants are separate assets; do not copy its road, sky, grass or broad rubble ground. Image 3 supports only the new style's large forms, material weight, warm-earth/cool-stone relationship and pixel-cluster rendering, without importing a complete gatehouse, people or combat.

Depict a substantial, taller but not slender perimeter wall pier. Rough masonry matching A01 occupies approximately the lower one-third to two-fifths, with warm ochre rammed earth above. Let the materials meet naturally. Use a few readable cracks, chipped corners and exposed earth cross-sections. A small number of irregular broken battlements remain at the top. The silhouette varies in height, while the main load-bearing body stays upright. Aim for a frontal visible width-to-height ratio of roughly 0.65 to 0.85, not a thin freestanding tower. Leave short wall-joining ends on both sides without extending long wall runs.

Provide a simple top location where a separate left flag can later be inserted, but do not paint the flag, pole, stand or red cloth into this sprite. Exclude timber stakes and charred beams on top; they will be deployed separately. Only a few small stones may touch the foundation. No large rubble pile, grass or earthen display platform.

Use a strict frontal orthographic elevation, vertical edges upright and footing on one horizontal baseline. No broad visible side face, bird's-eye top, isometric angle, vanishing point or oblique baked perspective. Express volume through large lit planes, shadows and broken cross-sections, not by tilting the whole wall.

Match A01's bright daytime lighting. Clearly separate warm ochre earth from cooler gray-brown masonry. Shadows are weighty but not globally black. Use large forms, stepped silhouettes and clustered hard-edged shading. Earth shows restrained grain and broken mass; stone shows readable joints and block thickness. No photographic materials, soft realistic illustration, random noise, mosaic filter, low-resolution retro pixel art or character chibi proportions.

Deliver a true RGBA transparent PNG at 1024x1024. Keep the entire pier and footing inside the canvas with at least 32 pixels of transparent outer margin. Clean cutout edges, nearly opaque structure. Do not independently change masonry drawing scale merely to fill the canvas.

No second gateway, complete city gate, roof, watchtower, long connecting walls, timber-dominated frame, flags, flagpoles, top timber remnants, red cloth, people, weapons, text, faction emblems, UI, road, sky, mountains, broad ground slab, grass, smoke, flames, shadow display base, painted checkerboard, white or gray background, halo or cropped footing. Do not directly crop the concept image; redraw a separately deployable orthographic transparent pier.
```

## 7. A03 右侧塌损墙墩（v1历史词，暂停复用）

以下保留获批后实际提交的v1词，用于解释结果，不直接作为重试授权。用户反馈左右同形、偏离E0参考；下一版必须重定形体参考与拆分范围，不能只追加“更破损”或降低高度后再次抽卡。

### 7.1 中文完整提示词

```text
为同一E0外围残门垣制作一件独立环境Sprite：右侧较矮、破损更明显的夯土石基墙墩。只生成这一件模块，不是把左墙墩镜像后改名。

参考图职责：第一张已验收的A01石拱锁定石材颜色、石块尺度、砌缝、光向和硬边色簇。第二张已验收的A02左墙墩锁定同一夯土与石基材质家族，并提供右墩的相对高度和绘制比例。第三张已验收E0场景只取画面右侧更低、更塌损的墙墩形体及不对称关系；不复制地面、周边瓦砾、木桩、天空或烟。第四张只辅助新画风的厚重体块和像素绘制语言，不添加人物、旗阵或完整门楼。

右墩下部保留坚实的粗石基座，石块尺度与A01、A02一致，上部暖赭夯土墙体有更明显缺失。主体左侧朝向中央门口，保留比较完整、竖直的内缘；顶部向画面右侧的外围自然断裂并降低，右上方形成塌损断面，主体仍直立，不整件斜倒。暴露的夯土层和石块断口清楚，少量碎石贴着墙根；不用大瓦砾堆遮住墙脚。

以A02为相同绘制比例参考，右墩可见高度约为左墩的75%至85%，比左墩更矮、更不规则；保留更多透明留白，不为填满画布而放大右墩。使用与左墩相近的基座厚度和石块尺寸，不能通过缩小石块尺寸冒充较小墙墩。两侧只留短的后续墙段连接面，没有长连接墙、另一个门洞或新塔楼。

严格正面正交立面，竖边垂直、基线水平，无宽大侧面、俯视、斜拍、等距视角、消失点和透视收缩。体积靠受光块面和不规则破口表现，图内不烘入穿门空间。

与A01、A02保持一致的明亮日间受光、暖赭夯土与冷灰褐石材、大体块、成簇硬边明暗和干净阶梯轮廓。战损颗粒服务结构，不铺照片纹理、柔雾或随机碎像素，不套马赛克滤镜，不使用低分辨率复古像素或角色Q版比例。

输出1024×1024真实RGBA透明PNG，脚点完整，外部至少32像素纯透明留边。实体墙体接近不透明，无白边、灰边、彩边、光晕和矩形投影。大瓦砾、焦木、旗帜和草后续独立制作，本图最多保留几块贴着基础的小碎石。

禁止：照搬或镜像完整左墩、对称完整双塔、屋顶、城门楼、第二门洞、木质承重门框、木支架、旗帜、红布、文字、徽记、人物、武器、UI、烟火、天空、道路、地台、草丛、浓雾、宽大瓦砾底座、伪透明棋盘格、白底、灰底或裁切主体。不要直接裁切场景概念图，须重新绘制独立正交透明素材。
```

### 7.2 English full prompt

```text
Create ONE independent environment sprite for the SAME E0 outer ruined gate: the shorter, more heavily damaged RIGHT rammed-earth wall pier on a stone foundation. Do not mirror the left pier and rename it.

REFERENCE ROLES: Image 1, the approved A01 stone arch, controls masonry color, block scale, joints, light direction and hard-edged pixel clusters. Image 2, the approved A02 left pier, controls the same earth-and-stone material family and provides relative height and drawing scale. Image 3, the approved E0 scene, defines only the lower, more collapsed right-hand silhouette and the asymmetrical relationship; do not copy ground, neighboring rubble, stakes, sky or smoke. Image 4 supports only weighty large forms and the new pixel-cluster rendering language, without adding people, flag formations or a complete gatehouse.

Keep a substantial rough masonry base with stone block sizes matching A01 and A02. The warm ochre rammed-earth body above has greater material loss. The object's left side faces the central passage and retains a relatively complete upright inner edge. Its top breaks down naturally toward the image-right outer side, exposing a collapsed upper-right cross-section. Keep the main pier upright instead of tipping the whole asset over. Show readable broken earth layers and stone edges. A few small stones touch the footing; do not conceal the footing under a large rubble mound.

At the same nominal drawing scale as A02, target a visible height approximately 75 to 85 percent of the left pier. Make the right pier lower and more irregular. Leave additional transparent space rather than enlarging it to fill the canvas. Match foundation thickness and stone sizes; do not fake the height difference by changing block scale. Only short ends for later wall connections are allowed. No long wall runs, another passage or a new tower.

Use a strict front-facing orthographic elevation, verticals upright and footing horizontal. No broad visible side, bird's-eye camera, oblique angle, isometric view, vanishing point or perspective taper. Convey volume through lit mass and irregular broken sections; do not bake a traversable tunnel into this image.

Match A01 and A02: bright daytime lighting, warm ochre earth against cooler gray-brown stone, strong large forms, clustered hard-edged shading and clean stepped contours. Damage texture supports structure. No photographic surfaces, soft fog, random pixel speckles, mosaic filter, low-resolution retro pixel art or character chibi proportions.

Deliver a true RGBA transparent PNG at 1024x1024 with complete footing and at least 32 pixels of purely transparent outer margin. Structural surfaces stay nearly opaque. No white, gray or colored fringe, halo or rectangular cast-shadow base. Large rubble, charred timber, flags and grass will be separate assets. At most a few small stones may rest directly against the base.

No copied or mirrored intact left pier, symmetric complete twin towers, roof, gatehouse, second opening, timber load-bearing gateway, timber braces, flags, red cloth, text, emblems, people, weapons, UI, smoke, fire, sky, road, display ground slab, grass, dense fog, broad rubble base, painted checkerboard, white or gray background or cropped silhouette. Do not crop the environment concept; redraw an independent orthographic transparent asset.
```

## 8. 验收与批准后的执行顺序

1. 先提交本文件供用户审阅；生成目标、参考图职责、完整中英文词和关键参数经批准后，才发起A01一次请求。若用户仅批准A01，不自动提交A02/A03。
2. A01检查文件格式、尺寸、Alpha、主体和门洞；查看浅色、深色和黄土底。门洞必须开放且真正透明，石拱实体必须保留。原生透明失败或请求结果不明时保留原始响应并停下，不自动付费重试。
3. 将A01原图作为关键图直接附到对话，并提供完整路径。用户认可后，将实际文件、哈希和素材尺度作为后件参考。
4. A02重点检查夯土/石基分材质、较高厚实体块、没有烘入左旗或焦木、与A01相同的石块家族和光向。认可后再执行A03。
5. A03重点检查更矮、更破损、右侧向外崩降、不依赖整件斜倒；检查石块尺度、基座和与A02的相对高差。
6. 原图、请求、响应保留；透明裁边或其他处理须另存，记录原生Alpha与后处理边界。Alpha最大253/254不单独判为失败，以主体透底和浅深底实际结果判断。
7. 三件并排检查建筑家族，并与E0 v3、概念图14和1011绘制语言对照。正交素材不应照抄概念图的斜透视，但须能解释如何重建已验收E0剪影。
8. 未经授权不导入Assets、不改旧PNG/GUID/Importer、不保存E0场景、不启动穿门工程。三件外立面通过也不代表门内空间完成。

## 9. A01 v1实际结果与限制

- 原图：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a01_stone_arch_front_v1.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a01_stone_arch_front_v1.request.json`。
- 原始响应：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a01_stone_arch_front_v1.response.json`。
- 实际请求从本文第5.2节直接提取英文Prompt，两张参考图路径/哈希与获批方案一致。仅提交1次生成请求，HTTP200，1张结果，quality=high、input_fidelity=high、1024x1024、background=transparent、PNG、n=1。此前一次本地校验因手工误录第二张参考图哈希而停止，发生在写请求文件及调用API之前；纠正校验后才实际提交，不是重复收费请求。
- 原图1091862 bytes，SHA-256=`094fddc3efae43cab0c0abec138a982ed0c283ab8f6181858abed6248a1722ec`。PNG已加载、verify并回读；实际1024×1024 RGBA、Color Type6，Alpha0–254，四角0，790157个完全透明像素。非零Alpha中91.724%为240及以上；最大值254不是透底失败依据。
- 主体Alpha≥128 bbox=(37,288,988,741)，951×453px，宽高比约2.10:1，高于原拟议1.5–1.7:1。门洞中部y=600近透明净宽393px，约为主体宽的41%；多行测得约38%–41%，低于原拟议55%–65%。用户在看到原图和限制说明后明确验收并要求继续，故当前造型和比例以用户验收版本为准，不以原拟议数值自动否决或重生。
- 可见主体外另有3306个低Alpha残留像素（Alpha1–7）；中央列拱顶以下有13个Alpha1–15残留像素，虽无明显堵洞实体，但不能声称门洞内所有像素均严格0。全非零Alpha bbox=(25,32,992,984)，因此严格32px纯透明留边检查未完全通过；明显主体的脚点和边界保持完整。
- 已制作仅供检查的浅/深/黄土底合成图：`Library/Locus/tmp/e0-a01-v1-validation/light_dark_ochre_preview.png`，原图未清理、裁切、缩放或覆盖。三种底下石拱和开放通道可辨认；微量残留应另存后处理版本再验证，不自动修改API原图。
- 原图和检查合成图已通过read附到对话；用户明确验收A01并授权继续，A01已作为A02的未改动参考图。外部和门洞低Alpha残留仍未清理，用户造型验收不等于已经完成部署前的像素清理。未收费重试、去背、导入或部署。

## 10. A02 v1实际结果与限制

- 原图：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a02_ruined_pier_left_v1.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a02_ruined_pier_left_v1.request.json`。
- 原始响应：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a02_ruined_pier_left_v1.response.json`。
- 英文Prompt从本文第6.2节直接提取，上传顺序为已验收A01原图→已验收E0 v3→概念图14，三张哈希前后核对均相同。参数保持sunburst/high/input_fidelity high/1024x1024/transparent/PNG/n=1，仅提交一次请求，无自动重试，HTTP200返回1张结果。
- 原图1588133 bytes，SHA-256=`a8077b005282ae7200e0169341572210641482f9217c027e950ce828302b069f`。已verify、加载并read附图；实际1024×1024 RGBA、PNG Color Type6，Alpha0–254，四角0，540641个完全透明像素；非零Alpha中95.838%为240及以上，不能因最大254就称主体透底。
- 主体Alpha≥128 bbox=(107,59,917,955)，810×896px，宽高比约0.904:1，比原拟议0.65–0.85略宽；顶部高低不规则、主体直立、暖赭墙面和灰褐石基可辨认，没有烘入左旗、旗杆、墙顶焦木、HUD或完整场景。用户看到原图及限制说明后明确验收并要求继续，当前造型和比例以验收版本为准，不用原拟议机器比例反向否决或重生。
- 全非零Alpha bbox=(0,9,1014,965)，严格32px完全透明外围检查未通过；Alpha≥8 bbox=(107,58,918,957)，明显主体具有充足留边。原图包含15721个Alpha1–31像素（其中包括轮廓抗锯齿及外围残留），不把全部低Alpha都判为杂片。Alpha≥128主体bbox外非零像素共5323个、最大Alpha42，其中也包含邻近轮廓的正常边缘；远处残留应在后处理时按位置识别，不能直接全图阈值化删除正常边缘。
- 三底检查图：`Library/Locus/tmp/e0-a02-v1-validation/light_dark_ochre_preview.png`，按左到右浅色/深色/黄土色，已read附到对话。三底下轮廓和材质可读，没有明显大面积背景或矩形底板；原图未清理、裁切、缩放或覆盖，哈希再次确认未变。
- A02已获用户验收，未改动原图已作为A03的建筑家族和相对高度参考。低Alpha残留仍保留，用户造型验收不等于部署前像素清理已完成。收费重试、清理原图、导入Assets和场景部署均未执行。

## 11. A03 v1实际结果与限制

- 原图：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a03_ruined_pier_right_v1.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a03_ruined_pier_right_v1.request.json`。
- 原始响应：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a03_ruined_pier_right_v1.response.json`。
- 英文Prompt从本文第7.2节直接提取，SHA-256=`092d59c8a618ae38c909b27b6346a921bec9d079d5bbe6811c7f1eea3c1e8513`；上传顺序为已验收A01→已验收A02→E0 v3→概念图14。四张参考前后哈希均一致，原图未清理、裁切或缩放。sunburst/high/input_fidelity high/1024x1024/transparent/PNG/n=1，实际仅提交一次请求，无自动重试，HTTP200返回1张图。
- 原图1417010 bytes，SHA-256=`8002f06a5a1f03c53e6276ff7f7194f9b71109afd4ed1dd4f60d5c1f123adbfd`。已verify、加载并read附图，实际1024×1024 RGBA、Color Type6，Alpha0–254、四角0、656891个完全透明像素；非零Alpha中96.358%为240及以上，最大254不作为主体透底失败依据。
- 主体Alpha≥128 bbox=(174,90,894,914)，720×824px，宽高比约0.874。A02同阈值主体高896px，原生画布上右/左高度约0.9196，高于原拟议0.75–0.85，当前两侧高差较小，不声称已符合相对高度规格。像素高度不是最终部署世界尺度；需用户先判断此版形体是否接受，不自动缩放素材或以Transform掩盖尚未验收差异。
- 初次回读只确认了向右崩降、直立石基、石土材质和无烘入附属物，未充分对照单件剪影及参考图目标位置。用户随后指出左右几乎相同、与概念图不相似；复核确认A02/A03均是高左缘→向右阶梯下降的宽墙片，差异主要为局部缺角和高度，不足以构成独立右侧形体。A03 v1未通过造型验收，不能以RGBA、材质相近或高差数字解释为合格。
- 全非零Alpha bbox=(0,36,1008,924)，严格32px完全透明外围检查未通过；Alpha≥8 bbox=(174,89,895,915)，明显主体完整且安全留边充足。Alpha1–31共10121像素（包含正常轮廓边缘和低Alpha残留）；Alpha≥128主体bbox外非零像素1337个、最大Alpha41，不全图粗暴阈值化清理。
- 三底检查图：`Library/Locus/tmp/e0-a03-v1-validation/light_dark_ochre_preview.png`，左到右浅色/深色/黄土色，已read附图。三底下轮廓和石土材质可读，没有明显矩形背景；原图未修改，生成后哈希再次核对一致。
- 批次A三件均已生成，但A03 v1已因同质化/偏离参考被用户指出问题，不通过；A01/A02此前验收与原图保留。未提交批次B/C、未收费重试、未清理API原图、未导入Assets或部署场景，也没有后台生成任务。

## 12. 造型复核结论与纠正边界

- 对照证据：E0 v3建筑局部的检查放大图已附到对话：`Library/Locus/tmp/e0-a03-v1-shape-review/approved_reference_architecture_closeup.png`。它仅为概念图检查放大，不是透明资产或已批准的下一版生图输入，源图未修改。
- 参考图不是左高右低的两块三角墙片。石拱旁的竖向夯土石基门墩、外侧连接低墙、更远断墙端部应分开辨认；左右区别来自各自墙墩主体、局部破口和附属连接关系，不能把右墩当作A02的缩短变体。
- 问题一：拆分和文字形体定义先偏离参考。A02已经形成高左缘、向右下阶梯下降的墙片，A03又明确要求同样的完整左缘和向右外侧崩降，正向描述事实上指定了重复剪影。“不要镜像/复制左墩”的负面词不能抵消它。
- 问题二：参考职责把A01/A02作为前两张整件图，却将唯一右侧造型依据放在第三张完整竖幅场景里。结果更像对A02做材质一致的变体，这是可见的结果和合理原因判断，不声称已测得模型注意力权重。
- 问题三：先前验收检查偏重Alpha、材质和脚点，没有先确认单件是否能重建参考的建筑轮廓。这是检查不足，不以模型随机性或高差参数未遵守替代纠正。
- 下一版建议：先在v3放大图标出A02/A03对应的具体墙墩范围，墙墩只保留紧凑竖向主体与局部缺损，外侧连接墙/断墙另件。右墩形体由该局部及审阅后的剪影定义；A01或A02只提供小范围材质/光向，不再让整件A02控制剪影。不使用A03 v1作为主要参考，不把当前图缩放、镜像或换裂纹当新版本。
- E0局部可作为新的生成参考候选，但需在新词中明确它不是裁切成品：模型须重新绘制正交独立RGBA素材，不能保留天空、地面、木桩、旗或瓦砾大底座。是否使用新裁图、简化剪影和具体参数先审批，不自动提交。
- 用户随后明确要求“开始纠正并重制”，已按本节纠正方向执行一次A03 v2：指定v3右墩局部为形体参考，A01石样/A02土样仅提供材质；原图和三底图已交付，用户已明确验收通过。下一件建议为右侧低矮连接残墙B01，未提交生成。详见 `plan/e0-a03-remake-prompts.md`。A01/A02及v1原图保留，未生成下一批或部署。

## 13. 文档与历史方案关系

- 本文件是已验收E0 v3后的批次A准备入口。
- `plan/n1-e0-wall-generation-prompts.md` 的旧六件/三批方案继续暂缓，不把本批扩大为望楼长墙、完整主城门或图集量产。
- `plan/scroll-scene-current-todolist.md` 记录最新执行状态；本文件的三件拆分与比例是待批准生产方案，不自动写成Design决策。
