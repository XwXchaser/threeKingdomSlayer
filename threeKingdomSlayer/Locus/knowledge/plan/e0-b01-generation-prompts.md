---
id: kd_3d19acfe-dcd8-4a69-9786-f997c7a4e962
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 B01：右侧低矮连接残墙生成方案

## 1. 当前状态

用户已同意B01为下一件构件，并要求按参考图开始制作，同时把参考图放到E0场景素材文件夹。

本轮已完成：

- 新建 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/`。
- 归档已验收场景参考图：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/E0_ApprovedSceneReference_v3.png`。
- 归档已验收A01/A02/A03建筑候选到 `Architecture/`。
- 归档B01范围审阅图、无标记局部形体图、A03材质小样到 `References/B01/`。
- 创建E0目录说明 `README.md`。
- 源图和归档PNG逐一校验哈希一致；没有移动或覆盖旧文件，没有修改E0场景，最新Unity回读为Edit Mode、仅加载E0、dirty=false。

**用户“同意，请按照参考图开始制作”明确授权制作B01，原图与三底图交付后用户已验收，并进一步授权用现有素材试搭E0构图。B01已用于保存的独立门垣构图组，保存重载与Play静止/接近采样已检查；组合构图仍待整体目视验收。** 未自动付费重试，正式路线和穿门空间未接入。

## 2. 构件职责与参考范围

B01位于已验收A03右侧墙墩的外侧，沿门垣横向延伸，直到更远处的独立断墙之前。只制作低墙本体：

- 低矮、横向，整体宽度明显大于高度。
- 顶部大体连续，仅有少量短残垛和浅崩口，向外侧略微降低。
- 暖赭夯土墙身占主体，底部为灰褐石基；少量石材可在缺口或边缘露出。
- 左端与A03相邻，保持朴素的竖向连接截面；右端是连接面或克制断口，不生成另一个高墙墩。
- 参考图中下部被木桩与瓦砾遮挡，需要补全稳定水平脚点；不把遮挡物烘入素材。

黄色范围只供用户对位审阅。范围内前景瓦砾、木桩不属于墙体。局部图包含上下文，模型必须排除这些内容并重新画成正面透明物件；不直接裁切RGB参考图部署。

## 3. 参考图、上传顺序与哈希

### 审阅定位图，不上传

`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/B01/B01_TargetScope_Review.png`

- 1100×900 RGB，黄色轮廓圈定B01低墙位置。
- 源图检查上下文为(600,590,875,815)，Nearest放大4倍。
- SHA-256：`5a0c9117809a1c81a93f54a7d8ae2f69a2407f2e6eb958dda766e31bd8ef7ca1`。
- 不作为API输入，避免黄色标注进入成品。

### image[0]：唯一形体参考

`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/B01/B01_ShapeReference.png`

- 源图为已验收E0 v3。源裁区(680,650,822,785)，142×135，Nearest放大4倍至568×540 RGB。
- 只取木桩后方的暖赭低墙与顶部短残垛；排除木桩、瓦砾、草、天空、右侧烟尘和高墙墩。
- 放大不增加原图细节。局部锁定低墙形态和破损关系，成品重新绘制正交结构并补足被遮挡基础。
- SHA-256：`bb2efc3f316e57c6d57f1bace7729c35a11534e64d61ac781fcd2e36b5a6bf3c`。

### image[1]：石材参考，仅材质

`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/B01/A03_StoneMaterial_Swatch.png`

- 来自已验收A03 v2的局部(440,770,647,947)，207×177，Nearest放大3倍至621×531 RGBA。
- 只决定灰褐石材颜色、受光和硬边色簇；不决定低墙剪影。
- SHA-256：`ec2e0e9b4628c40a6b6227701e63706b8cc04fd73615172ffaabc0f553b3ee49`。

### image[2]：土面参考，仅材质

`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/References/B01/A03_EarthMaterial_Swatch.png`

- 来自已验收A03 v2的局部(570,421,698,644)，128×223，Nearest放大3倍至384×669 RGBA。
- 只决定暖赭夯土表面、日间受光与硬边绘制；不把土面解释成密集砖砌。
- SHA-256：`92f55d0fba044dd6b95fb6f467be2af4a58ecd2bef484530d8e7a1ef21e582bb`。

### image[3]：新画风辅助

`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`

- 已核对的新画风概念图14，1024×1536 RGB。
- 只决定高清像素化三国战场的大体块、建筑重量、硬边色簇及材质语言，不复制人物、军旗、武器或完整城门。
- SHA-256：`643d50375a9fcdd2210aa40f43b5a70fb05099f01664de9ae54dfb0423016210`。

整体参考图为 `References/E0_ApprovedSceneReference_v3.png`，SHA-256=`44eb18f47a622e58f20e9c7fceafde20dad059e96ec5b5ad0cf1fc25ab1b923a`。它已归档供一起查看，不再与局部同时上传牵引整幅场景重构。

不上传整件A02、整件A03或失败A03 v1，避免低墙再次变成竖向墙墩。

## 4. 完整中文提示词

```text
为高清像素化三国战场游戏制作一件独立建筑Sprite：E0石拱门右侧墙墩外缘延伸的低矮连接残墙B01。只交付这段低墙本体，不包括相邻高墙墩。

参考职责分开：第一张是已验收E0场景中的低墙局部，唯一决定本物件的大形体。目标是木桩后方暖赭色低墙，顶部有少量短残垛，墙身低矮横向延伸。第一张中的高墙墩、木桩、前景瓦砾、草、天空、烟尘和断木是上下文，均不属于本物件。第二张只提供已验收建筑家族的灰褐石材、明亮日间受光和硬边色簇。第三张只提供连续暖赭夯土表面的颜色和绘制语言。第四张只辅助新画风的大体块和三国战争建筑重量，不复制人物、完整城门楼、旗帜或战斗构图。材质参考不能把横向低墙改成高墩、尖塔或三角墙片。

主体是一段厚实而低矮的横向夯土残墙，整体可见宽度约为高度的1.8至2.3倍，优先呈现水平连续的墙身，而不是把竖向墙墩压扁。左端具有朴素竖向连接截面，可以紧贴后续独立墙墩；右端保持可接续的短断面，外缘只略微降低，不长出新的高柱。墙顶保留两到三处低矮、错落的残垛，中间只有一处浅崩口或少量缺角，没有深V形缺口、一路下降到地面的长阶梯轮廓，也没有完整整齐的垛口横幅。

上部大部分为连续暖赭色夯土块面，几道克制裂纹、少量掉角和暴露断面表达战损；不要把整面夯土分成密集砖缝，也不把墙身画成多块巨大石板。底部为低矮灰褐石砌基座，几层粗石承担脚点，颜色和受光与材质小样一致。顶部及局部断边可少量露石，但石材不能覆盖全部墙身。概念图被木桩和瓦砾挡住的下部应补全为连续稳定的石基，不照抄遮挡物。

严格正面正交建筑立面，主要竖边垂直、脚点共用水平基线；不烘入斜拍、等距视角、透视收缩、俯视顶面、宽大侧面或可见地面。厚度通过明暗折面和少量破口表达。本件没有门洞、屋顶、门楼或内墙，不是从概念图裁下一块带背景的图片，而是重新绘制的独立可摆放模块。

沿用已验收E0家族的明亮日间光照，暖土和灰褐石材清楚分材质，暗部有重量但不整体压黑。使用清晰大体块、阶梯轮廓、成簇硬边光影和有目的的土石纹理；不是照片写实、油画软边、光滑3D渲染、马赛克滤镜或低分辨率复古像素。细节服务墙体结构，不堆随机噪点。

输出1024×1024真实RGBA透明PNG，横向主体完整入画，不为了填满方形画布增高墙体。主体居中、石基脚点和两端不裁断，四周至少32像素纯透明安全边界。墙体近乎不透明，背景干净，无白边、灰边、光晕、矩形底板或地面平台。只允许几块紧贴基础的小碎石，不形成大型瓦砾坡。

禁止：相邻A03高墙墩、完整左墩缩小版、第二根高柱、长三角阶梯墙片、完整城门楼、门洞、屋顶、木质承重门框、木桩、焦木、旗帜、草、烟火、地面、天空、远山、人物、武器、文字、徽记、HUD、黄色审阅标注线、浓密砖缝、大片石板墙面、瓦砾大底座、伪透明棋盘格、白底、灰底、裁断墙脚或两端。
```

## 5. Full English prompt

```text
Create ONE independent architectural sprite for a high-definition pixel-cluster Three Kingdoms battlefield game: B01, the low connector wall extending outward from the right-side pier of the E0 stone-arch entrance. Deliver only this low wall run, without the adjoining tall pier.

KEEP REFERENCE ROLES SEPARATE. Image 1 is a local crop from the approved E0 scene and is the ONLY silhouette authority. The target is the low warm ochre wall behind the wooden stakes, with a few short broken battlements and a horizontally continuous body. The tall pier, stakes, foreground rubble, grass, sky, smoke and broken timber in Image 1 are context and must be excluded. Image 2 controls only the approved architectural family's gray-brown masonry, bright daytime lighting and hard-edged pixel clusters. Image 3 controls only the continuous warm ochre earth surface and its rendering language. Image 4 supports only the large forms and architectural weight of the new Three Kingdoms war-art style. Do not copy its people, grand gatehouse, flags or combat composition. Material references must not turn this horizontal low wall into a tall pier, spire or triangular wall panel.

Depict a substantial but low horizontal rammed-earth wall run. Aim for a visible width approximately 1.8 to 2.3 times its height. Prioritize a horizontally continuous wall body, not a vertically compressed version of a tall pier. Its left end has a simple upright joining face that can meet a separate pier. Its right end remains a short joinable broken face, with only a modest outward height reduction and no new tall column. Two or three low irregular battlement remnants remain on top, with one shallow chip or small breach. No deep V-shaped opening, long staircase silhouette descending all the way to the ground, or perfectly regular complete battlement banner.

Most of the upper body is continuous warm ochre rammed earth, with a few restrained cracks, chipped corners and exposed broken sections. Do not divide the earth into dense brick joints or build the wall body from giant rectangular stone plates. A low gray-brown masonry foundation supports the footing, using a few courses of rough blocks that match the material swatch's color and light. Limited stone may appear at the top or broken edges, without covering the entire earthen body. Reconstruct the lower portion hidden by stakes and rubble in the concept as a stable continuous stone footing; do not copy the occluders.

Use a strict front-facing orthographic architectural elevation, principal verticals upright and footing on one horizontal baseline. No baked oblique view, isometric camera, perspective taper, bird's-eye top, broad side face or visible terrain. Express thickness through shaded planes and limited chips. This asset has no gateway, roof, gatehouse or tunnel interior. It is a newly drawn independently deployable module, not a background-containing crop from the scene concept.

Match the approved E0 family's bright daytime lighting. Keep warm earth and gray-brown stone clearly distinct, with weighty shadows without globally darkening the wall. Use strong large forms, stepped contours, clustered hard-edged shading and purposeful earth-and-stone texture. No photographic realism, soft oil-paint edges, smooth 3D render, mosaic filter or low-resolution retro pixel art. Texture supports the architecture instead of random noise.

Deliver a true RGBA transparent PNG at 1024x1024. Keep the complete horizontal subject inside the canvas, without increasing its height to fill the square. Center it with intact foundation and both ends and at least 32 pixels of purely transparent outer margin. Structural surfaces remain nearly opaque. Clean transparent background, no white or gray fringe, halo, rectangular display base or terrain slab. Only a few small stones touching the foundation are allowed, not a broad rubble slope.

No adjoining A03 tall pier, miniature version of a complete left pier, second tall column, long triangular staircase wall panel, complete gatehouse, gateway opening, roof, wooden load-bearing frame, stakes, charred timber, flags, grass, smoke, fire, ground, sky, mountains, people, weapons, text, emblems, HUD, yellow review annotations, dense brick joints across the earth, broad stone-plate wall face, large rubble base, painted checkerboard, white or gray background, or cropped footing and ends.
```

## 6. 实际调用参数、输出和成本边界

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
request format: multipart/form-data
model: gpt-image-2.5-sunburst
quality: high
input_fidelity: high
size: 1024x1024
background: transparent
output_format: png
n: 1
image[]: B01局部形体 → A03石材小样 → A03土面小样 → 概念图14
automatic retries: 0
```

实际提交一次请求，HTTP200返回1张结果；准确费用未由本文查询，记录实际usage但不推断金额。模型/质量/尺寸与本文一致，无自动付费重试；本次实际RGBA验证通过，不将transparent参数本身视为成功证明。

外部原始产物已保存：

- `C:/Users/Administrator/Pictures/gptGen/e0_batch_b_from_approved_v3/e0_b01_low_wall_right_v1.png`
- 同名`.request.json`和`.response.json`。

已验证并按当前用户授权纳入E0素材目录的候选：

- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Candidates/e0_b01_low_wall_right_v1.png`

候选仍位于Candidates，未经用户验收不归为正式部署素材，没有放入场景。

## 7. 验收重点

1. 先核对剪影与对应位置：低矮横向连接墙，与A03竖向墙墩明显不同；外端轻度降低，没有第二高墩或大阶梯断墙。
2. 石土分材质：连续暖赭墙身与低石基，尺寸/光向能与A03搭配；不照搬整件A03的裂纹布局。
3. 木桩、瓦砾、草、旗、烟和道路没有烘入；被遮挡墙脚合理补全。
4. 检查PNG实际尺寸、RGBA/Color Type、Alpha、脚点、留边和浅/深/土色底。Alpha最大253/254不自动否决，主体低Alpha透底和背景残留单独记录。
5. 原图、请求、响应保留；主要成品通过read附到对话并提供完整路径。若不符合目标，明确标为失败候选，不自动抽卡。
6. 通过用户目视验收前不作为后件主参考，也不部署；以后授权部署时通过Unity API，保护原E0试摆和用户布局。

## 8. B01 v1结果与归档验证

- 原图1003460 bytes，SHA-256=`bfcf1b707b197c4bc7a6fd6e65e1532e27cf02cbfc0a9a60aa1049fdf678a1e3`，实际1024×1024 RGBA、PNG Color Type6，Alpha0–254、四角0，761374个完全透明像素。非零Alpha中95.612%为240及以上，最大254不自动判为主体透底。
- 生成请求从本文第5节直接提取英文Prompt，与执行记录逐字一致；参考顺序为B01局部→A03石样→A03土样→概念图14，四张哈希均核对一致。仅提交一次，HTTP200，1张结果，sunburst/high/input_fidelity high/1024x1024/transparent/PNG/n=1，无自动重试。
- 视觉回读：形成低矮横向墙身、连续暖赭表面和底部灰褐石基，没有重复A03竖向墙墩；顶部有低残垛和浅破口，没有烘入木桩、草、旗、烟或大瓦砾堆。用户已验收B01，并授权用已有素材搭建E0构图预览；单件验收不等于整个场景已完全复现概念图。
- 主体Alpha≥128 bbox=(20,333,1012,706)，992×373px，宽高比约2.6595，高于拟议1.8–2.3；更扁宽，用户已接受当前造型，不自动重生。主体左右留边仅20/12px，不满足32px目标，但主要脚点和两端完整。原图未缩放、补边、裁切或覆盖。
- 全非零Alpha bbox=(0,99,1015,996)，外部存在低Alpha残留；Alpha1–31总计8429像素，包含正常边缘，不能直接全图阈值化清理。部署前须在不损伤主体边缘的前提下检查并另存清理版本。
- 三底检查图：`Library/Locus/tmp/e0-b01-v1-validation/light_dark_ochre_preview.png`，浅色/深色/黄土色由左到右；已read附图，轮廓与材质可读，没有明显矩形背景。检查后原图哈希再次确认未变。
- 项目归档：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/E0/Candidates/e0_b01_low_wall_right_v1.png`，PNG与外部原图字节一致，独立meta已生成；通过TextureImporter导入Sprite/Single、PPU100、Point、Uncompressed、mipmap off、Alpha Is Transparency、maxTextureSize2048。Custom Pivot=(0.5,318/1024)，按明显主体底边定位，实际Sprite.pivot=(512,318)。用户随后授权试搭，B01已用于独立E0场景的左右低墙，静止和distance12的Game画面已检查。
- 用户已授权使用现有素材试搭独立E0构图。A01/A02/A03的实际Sprite脚点已通过TextureImporter调整为(512,282)/(512,68)/(512,45)px；PPU100及PNG原始字节、GUID不变。没有用Transform Y双重补偿，静止及distance12的Game画面中可见脚点贴合地面。
- 新组：`Assets/Experiments/CurvedScroll/E0OuterGatePreview.unity/E0 Outer Gate Preview/E0 Approved Gate - Composition v1`，6个Renderer；旧门垣组隐藏但完整保留，scenery共21项、无空项/重复。只保存E0后已重载回读，退出Play恢复progress0、animate=false，Edit Mode、dirty=false。没有接入正式路线或制作门内空间。
- 静止/接近截图与后续边界见 `plan/e0-approved-gate-composition-handoff.md`。本次没有继续生图；新场景组合仍待用户整体目视验收。
