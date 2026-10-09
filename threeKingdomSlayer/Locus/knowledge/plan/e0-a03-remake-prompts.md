---
id: kd_e5f535dd-8232-4681-9725-3ed031f4f2c6
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# E0 A03 重制：以参考局部锁定右侧竖向墙墩

## 1. 本次任务和授权边界

用户指出A02/A03左右几乎一模一样、与E0 v3参考图的造型不相似，随后明确要求“开始纠正并重制”。已据此落实具体参考范围与完整中英文词，并在本次授权内提交一次A03 v2请求；**新图已生成、验证和附到对话，用户已明确验收通过**。

本次执行用户明确要求的A03纠正重制，参考职责由整件A02牵引改为目标局部锁定剪影、材质小样统一绘制语言。准备并回读完整中英文词后，在用户“开始纠正并重制”的明确授权范围内提交一次请求；不是自动付费抽卡。A01/A02和A03 v1原图保留，未修改Unity场景，未生成批次B/C。

## 2. 具体纠正内容

A03 v1同时被整件A02和“高左缘→向右阶梯下降”的文字牵引，形成另一块宽墙片。仅添加“不要复制”或把整体缩小不能改变形体。

A03 v2对应E0 v3中**紧邻石拱右侧的独立竖向夯土石基墙墩**：

- 主体紧凑、直立，连续夯土墙身占上部主要面积，石材集中于低基座和少量露出的破损边。
- 顶部只留少量不规则矮残垛，两个侧缘大体直立；右上角可有局部缺损，不生成从左高到右低的长阶梯墙。
- 外侧连接低墙、远端断墙、木桩/残木和前景大瓦砾各属其他物件，不并入墙墩。
- 概念图被瓦砾挡住的基础需要补全为简朴稳定的石基，不能照抄遮挡物。
- A01/A02只截取材质小样，不能再把整件A02作为剪影或相对高度参考。
- 不将“画布上比A02矮多少”当作形体定义。本版先核对墙墩身份、紧凑竖向轮廓与石土组织，再讨论真实部署比例。

黄色轮廓仅用于用户确认目标，未上传API。无标记局部和材质小样仅为生成参考，不是最终Sprite，不直接抠图成品。

## 3. 参考图与文件

### 3.1 用户审阅定位图，不上传

`Library/Locus/tmp/e0-a03-remake-reference-v2/a03_target_scope_review.png`

- 960×900 RGB。
- 黄色轮廓标出紧邻石拱右侧的墙墩，排除相邻石拱、低墙和独立瓦砾堆。
- SHA-256：`7f749e1c05dfe8945715b8d413f84515fe2bdfe177392b7f3a2e618ed2bb828c`。

### 3.2 image[0]：唯一形体主参考

`Library/Locus/tmp/e0-a03-remake-reference-v2/a03_target_pier_closeup.png`

绝对路径：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/e0-a03-remake-reference-v2/a03_target_pier_closeup.png`

- 原始来源：`C:/Users/Administrator/Pictures/gptGen/changbanpo_e0_reference_workflow/r1_n1_to_e0_approach_v3.png`，源图SHA-256=`44eb18f47a622e58f20e9c7fceafde20dad059e96ec5b5ad0cf1fc25ab1b923a`。
- 源图裁取范围（左上原点、右下排他）：(621,628,711,798)，90×170，Nearest放大4倍至360×680 RGB。
- 局部中的暖赭竖向墙墩是目标，木桩、右侧墙段、石拱边、地面、草和遮挡瓦砾只是上下文，必须排除。
- 放大不创造原图没有的细节；只锁定大形体、夯土/石基关系，模型重绘正交素材并补全被遮挡脚点。
- SHA-256：`ca615f967c5b74e379d2be86f3d4ca04578f565a3ec49ec32db4176542007986`。

### 3.3 image[1]：A01石材小样

`Library/Locus/tmp/e0-a03-remake-reference-v2/a01_stone_material_swatch.png`

绝对路径：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/e0-a03-remake-reference-v2/a01_stone_material_swatch.png`

- 来源：已验收 `C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a01_stone_arch_front_v1.png`，源图SHA-256=`094fddc3efae43cab0c0abec138a982ed0c283ab8f6181858abed6248a1722ec`。
- 源图局部(105,505,260,656)，155×151，Nearest放大3倍至465×453 RGBA。
- 仅控制灰褐石材、受光与色簇；不控制墙墩剪影，不要求按小样放大倍率推算世界石块尺寸。
- SHA-256：`d2e9cd9ca57401efd836f2cbb59c270875ff83aa7165d888a01b57d592319995`。

### 3.4 image[2]：A02夯土小样

`Library/Locus/tmp/e0-a03-remake-reference-v2/a02_earth_material_swatch.png`

绝对路径：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/e0-a03-remake-reference-v2/a02_earth_material_swatch.png`

- 来源：已验收 `C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a02_ruined_pier_left_v1.png`，源图SHA-256=`a8077b005282ae7200e0169341572210641482f9217c027e950ce828302b069f`。
- 源图局部(363,289,532,511)，169×222，Nearest放大3倍至507×666 RGBA。
- 仅控制暖赭土色、颗粒和硬边绘制。禁止将它解释成堆砌的大石板或复制整件A02轮廓。
- SHA-256：`e66e20c195fc23ff860543d8e3b5e6edf4601ad1cbf746b542911c97fab59f38`。

### 3.5 image[3]：新画风辅助

`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`

- 1024×1536 RGB，已读，SHA-256=`643d50375a9fcdd2210aa40f43b5a70fb05099f01664de9ae54dfb0423016210`。
- 只控制高清像素化三国战场的大体块、硬边色簇和建筑重量，不移植人物、门楼、旗帜、武器或战斗构图。
- 1011 Idle和概念图13保留本地绘制语言比对，不上传。

上次生成的A03 v1、整件A02、整幅E0图和黄色定位图均不作为新请求输入。初次探索局部 `a03_shape_context_v2_candidate.png` 含过多木桩/低墙，仅作为检查草稿，不使用。

## 4. 完整中文提示词

```text
为高清像素化三国战场游戏重新绘制一件独立建筑Sprite：E0石拱门右侧紧邻门口的竖向夯土石基墙墩。只生成这个墙墩本体。

参考职责必须分开：第一张是已验收E0概念图中的目标局部，唯一决定墙墩的大形体。目标是图中靠左的暖赭色直立墙墩，顶部有少量短残垛，正面是一块连续夯土墙身。第一张中的木桩、右侧低墙、画面左边的石拱、地面、草和前景瓦砾不属于目标。第二张只提供灰褐石材的颜色、受光和硬边色簇。第三张只提供暖赭土色、土面肌理和绘制语言，不把它重新解释成大块石板。第四张只辅助高清像素化三国战争美术的重量和像素色簇，不复制人物、宏伟门楼、军旗或战斗场面。任何材质参考都不能改变第一张所指定的竖向墙墩身份。

主体是一段紧凑直立的厚夯土墙墩。两侧大体竖直，顶部近水平但保留两三处短小、错落的战损残垛，右上角只有一处有限缺损。上部约四分之三是连续暖赭色夯土块面，有一道主要裂口、少量凹坑和风化边，不将整个墙身切成密集方形砖块，也不由多块巨大矩形石板堆满。下部约四分之一是朴素灰褐石砌基座，几层粗石托住墙身；石头可以在破口处少量露出，但不能爬满上半部。

轮廓必须首先读成门口旁的竖向墙墩，而不是带长斜坡的断墙片。宽度约为自身高度的一半，保持紧凑而厚重，不拉成长尖塔。两个侧缘都收束在主体宽度内，不向右伸出低墙尾巴，没有从左侧最高点一路阶梯下降到右侧墙脚的大对角轮廓。没有相连的第二块墙面、第二个高墩或一体瓦砾坡。参考局部被瓦砾挡住的墙脚必须补全成稳定平直的石基，不能把遮挡瓦砾抄进成品。

重新绘制严格正面正交立面，竖边垂直、基线水平，不照搬局部场景的透视和遮挡。厚度通过明暗折面、缺口和小范围露石表现，不画宽大侧面、俯视顶面或透视收缩。本件不含门洞、内墙、拱顶或连接墙，不是从概念图抠出的一块带背景图片。

保持明亮日间光照和已验收家族的暖土对冷灰褐石材。使用明确大体块、干净阶梯边缘、成簇硬边明暗和克制颗粒，土与石材质清楚分开。没有照片纹理、光滑3D塑料感、油画软边、随机噪点、马赛克滤镜或低分辨率复古像素。不要靠复杂碎纹理掩盖错误轮廓。

输出1024×1024真实RGBA透明PNG，主体完整、居中，脚点和顶部不裁断，四周至少32像素完全透明留边。实体墙身接近不透明，外部干净，无白边、灰边、光晕或矩形底板。墙脚最多几块紧贴基座的小碎石，不能形成大地台。

禁止：高左缘向右连续阶梯下降的三角形墙片、整件左墩的缩小或镜像变体、长低墙尾巴、大片砖缝夯土、石板覆盖整个墙身、宽大瓦砾坡、独立底座板、木桩、旗杆、旗帜、木梁、草、地面、天空、山、烟火、人物、武器、文字、徽记、HUD、黄色标注线、完整门楼、门洞、伪透明棋盘格、白底、灰底或裁切主体。
```

## 5. Full English prompt

```text
Redraw ONE independent architectural sprite for a high-definition pixel-cluster Three Kingdoms battlefield game: the upright rammed-earth pier on a stone base immediately to the RIGHT of the E0 stone-arch entrance. Deliver only the pier itself.

REFERENCE ROLES MUST STAY SEPARATE. Image 1 is a close-up of the approved E0 concept and is the ONLY silhouette authority. The target is the warm ochre upright pier toward the left of that crop, with a few short broken battlements above a continuous rammed-earth front. The timber stakes, low wall on the right, stone arch at the left edge, ground, grass and foreground rubble in Image 1 are context, not parts of this asset. Image 2 controls only gray-brown masonry color, lighting and hard-edged pixel clusters. Image 3 controls only ochre earth color, surface character and rendering language; do not reinterpret it as giant stone slabs. Image 4 supports only the weight and pixel-cluster language of the Three Kingdoms war-art style. Do not copy its people, grand gatehouse, flags or battle. Material references must never replace the upright pier identity established by Image 1.

Build a compact, substantial upright rammed-earth pier. Both side edges stay broadly vertical. Its top is nearly level, interrupted by two or three short, irregular war-damaged crenellation remnants and one limited upper-right chip. Approximately the upper three-quarters is one continuous warm ochre earthen mass, with one dominant crack, a few pits and weathered edges. Do not divide the whole body into dense square bricks or stack huge rectangular stone plates. Approximately the lower quarter is a simple gray-brown masonry foundation with a few courses of rough stones supporting the earth. Limited stone may show at damaged edges, but do not extend block masonry across the upper body.

The silhouette must first read as an upright pier beside an entrance, not a descending broken wall panel. Aim for a visible width around half its height, compact and weighty rather than a thin spire. Both side edges terminate within the pier's own width. No low wall tail projecting to the right, no long diagonal staircase silhouette running from a high left corner down to the right footing, no attached second wall panel or second pier, and no integrated rubble slope. Complete the portion of the base hidden by rubble in the concept as a stable horizontal masonry footing; do not copy the occluding rubble into the asset.

Redraw a strict front-facing orthographic elevation, vertical edges upright and footing horizontal. Do not inherit the scene crop's perspective or occlusion. Express thickness through shaded planes, chips and limited exposed stone rather than a broad side face, bird's-eye top or perspective taper. This asset has no passage, tunnel wall, arch lining or connecting wall run. It is a newly drawn isolated asset, not a background-containing cutout of the concept.

Keep bright daytime lighting and the approved material family's warm earth against cooler gray-brown stone. Use strong large forms, clean stepped contours, clustered hard-edged shading and restrained grain, with visibly different earth and stone. No photographic textures, smooth plastic 3D rendering, soft oil-paint edges, random noise, mosaic filter or low-resolution retro pixel art. Do not hide an incorrect silhouette under dense surface detail.

Deliver a true RGBA transparent PNG at 1024x1024. Center the complete pier with intact top and footing and at least 32 pixels of purely transparent margin on every side. Structural surfaces remain nearly opaque; clean outer transparency with no white or gray fringe, halo or rectangular base. At most a few small stones may touch the foundation, without becoming a broad terrain slab.

No triangular wall panel with a high left edge descending in continuous steps to the right, scaled or mirrored version of a complete left pier, long low-wall tail, dense brick joints across the earth, giant stone plates covering the whole body, broad rubble slope, display base, wooden stakes, flagpole, flag, timber beams, grass, ground, sky, mountains, smoke, fire, people, weapons, text, emblems, HUD, yellow annotation lines, complete gatehouse, gateway opening, painted checkerboard, white or gray background or cropped silhouette.
```

## 6. 拟议参数与执行条件

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
image[] order: 目标局部 → A01石样 → A02土样 → 概念图14
automatic retries: 0
```

实际产物：

- `C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a03_ruined_pier_right_v2.png`
- 同名`.request.json`和`.response.json`。

新版本已提交一次请求并成功返回，保留v1，原图和参考来源不覆盖，无自动重试。后续请求超时/结果不明仍不自动重发；透明失败保留结果并停止，不自动换服务、换参数或收费去背。

## 7. 验收顺序

1. 先看剪影和指定位置是否吻合：独立竖向墙墩，连续夯土墙身，有限上角缺损，没有同旧A02/A03一样的三角阶梯墙尾。
2. 与目标局部对照：主体与连接低墙、瓦砾、木桩分开；被遮挡脚点补全，正交重绘不照抄场景透视。核对没有把灰石拱门或黄色审阅线带入结果。
3. 然后看同家族材质：暖土对灰石、日间受光、清楚色簇；不以复制A02裂纹和石块布局换取相似材质。
4. 最后检查PNG、实际尺寸、RGBA/Alpha、四角、主体透明度与浅/深/黄土底边缘。技术成功不替代剪影通过。
5. 关键成品通过read直接附到对话并给完整路径。若造型仍错，明确标失败，不自动抽卡或用Transform缩放掩盖。
6. 用户验收前不作为后续主参考；不因本次重制而擅自重做A02或部署E0。A02与参考左墩的对应关系如需重审，另列具体范围和方案。

## 8. 本轮完成证据

- 目标局部、黄色定位图、A01石样和A02土样均已保存并read检查；源图哈希前后未改变。实际上传为目标局部→A01石样→A02土样→概念图14，未上传黄色定位图、整件A02或失败A03 v1。
- 已提交一次sunburst/high/input_fidelity high/1024x1024/transparent/PNG/n=1请求，HTTP200返回1张；请求及原始响应保存为同目录同名`.request.json`和`.response.json`。执行Prompt与本文件第5节逐字核对一致，四张参考哈希均匹配，无自动重试。
- 原图：`C:/Users/Administrator/Pictures/gptGen/e0_batch_a_from_approved_v3/e0_a03_ruined_pier_right_v2.png`，1289551 bytes，SHA-256=`50b5858015a9c701513533cbfaf28571cffa147342ae1f70531590ef19b02ac5`。实际1024×1024 RGBA、Color Type6，Alpha0–254、四角0，557919个完全透明像素；非零Alpha中96.326%为240及以上。已verify、加载和read附图。
- 视觉回读：已由宽三角阶梯墙片变为独立竖向墙墩，连续暖赭墙面占主导，石材主要集中在基座，顶部有短残垛，未烘入低墙尾、木桩、旗或瓦砾坡；与旧A02/A03剪影已有明确区别。主体Alpha≥128 bbox=(201,42,826,978)，625×936px，宽高比0.6677；比拟议约0.5稍宽，但用户看到结果及限制后已明确验收，当前造型和比例以验收版为准，不自动重生。
- Alpha≥8 bbox=(201,41,829,979)，明显主体完整且留边超过32px；全非零Alpha bbox=(0,16,1014,1000)，外围有低Alpha残留，因此严格纯透明留边未完全通过。Alpha1–31共12663像素，包含正常边缘和杂片，不直接全图阈值化清理。原图未覆盖或修改。
- 三底检查图：`Library/Locus/tmp/e0-a03-v2-validation/light_dark_ochre_preview.png`，左到右浅色/深色/黄土色，已read附到对话。轮廓与材质可读，没有明显矩形底板。源图哈希再次确认未变化。
- 用户已验收A03 v2，当前作为右侧独立墙墩的形体/材质基准；v1继续保留为未通过版本。下一件建议是右侧低矮连接残墙B01，尚未准备完整生图词或提交请求。未清理原图、未导入Assets、未修改场景，没有后台任务。
