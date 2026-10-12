---
id: kd_3aac5c0f-aee8-40e3-9c03-1549e85b5c11
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1 A01 无字双箭头木路牌：制作参考与生成词

## 1. 状态与授权

用户已验收J1 v3，并同意5件新素材的拆分分析，随后要求“同意，请开始A01的制作”。局部形体参考、完整中英文提示词、参数和验收标准已提交对话，用户明确回复“发起生成”。已按下述获批版本提交一次A01请求，异步任务t4已完成；HTTP200，原图已落盘并完成机器/视觉检查；无自动重试。

A01成品已取得并验证，没有导入Unity或修改场景。当前用户美术验收仍待定；不自动开始A02–A05。这里只制作J1的A01，不与已验收E0石拱A01混用编号或文件名；不自动开始A02–A05。

## 2. 目标与单件边界

- 交付一件可独立部署的实体木路牌Sprite：一根粗木柱、上方指左的箭头木牌、稍低指右的箭头木牌和必要绑绳。
- 以已验收J1 v3的木柱厚度、箭头板上下错位、左右伸展、暖棕木材、裂纹和日照作为形体与材质依据；不改变为同高双箭头、单箭头或第三块直行牌。
- 不能包含原图柱脚的草、碎石、路面、远山、天空、篱笆及其它道具；被草石遮挡的木柱下端补全为自然平稳的木柱脚，不新增木座、石墩、展示平台或土台。
- 两块牌完全无字、无数字、无徽记，不是发光或浮空UI。保留轻微侧面厚度和原观察角度，不转为等距俯视或背面。

## 3. 参考图职责与证据

### Image 1：路牌局部形体主参考

- 上传：`Library/Locus/tmp/j1-a01-preparation/j1_a01_shape_crop_x3_v1.png`，768×936 RGB PNG。
- SHA-256=`91dd68c1c4702c080c0dd619fc8dceb466c95647ea3749066b0c4422a1809db8`。
- 从v3原图按区域(384,610)–(640,922)裁出256×312局部后仅作3倍nearest放大，未抠图、重绘或锐化，不是新生成成品。
- 决定木柱与箭头牌的剪影、上下位置、板厚、绑绳和木材材质；周围仍有原图背景，必须明确全部忽略。
- 原尺寸局部保留：`Library/Locus/tmp/j1-a01-preparation/j1_a01_shape_crop_native_v1.png`。
- 裁切清单：`Library/Locus/tmp/j1-a01-preparation/j1_a01_reference_manifest_v1.json`。

### Image 2：已验收J1 v3整体环境

- 上传：`C:/Users/Administrator/Pictures/gptGen/changbanpo_j1_reference_workflow/j1_after_e0_reference_v3.png`，1024×2048 RGB PNG。
- SHA-256=`1979a354b757fb034c460c41431b76738ccddd8694fda0272b22ea86e7b17378`。
- 只补充明亮日间光照、暖棕木材与高清像素色簇的绘制关系；不复制道路、村庄/建筑、其它道具和全幅构图。
- 原图哈希在局部参考制作后再次校验，未变化。

两图重复multipart `image[]`，顺序固定为局部形体→整体环境。不上传带A01方框/文字的分析标注图、旧E0石拱或其它角色图，避免生成标记或改造路牌设计。

## 4. 生成参数（已批准并实际提交）

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
background: transparent
output_format: png
n: 1
image[]: Image 1 then Image 2
prompt: English full prompt + English negative prompt below
automatic retries: 0
```

- max延续最近明确确认的最高质量标准；1024×1024是A01单件画布，不是修改J1场景比例。
- 仅发起一次请求；若透明不支持、RGB/假棋盘格、超时或结果不明，保留请求/响应并报告，不自动重试、降参数或另发去背请求。
- 凭据仅取环境变量MUSK_API_KEY，不写入文件或输出。当前仅确认变量存在，未测试付费请求或远程额度。
- 拟议新目录：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/`。
- 拟议原图：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_a01_blank_double_arrow_signpost_v1.png`；同时保留同基名`.request.json`、`.response.json`、`.status.json`。提交前检查这些路径不存在，不覆盖旧文件。

## 5. 中文完整提示词

为高清像素化三国战场游戏制作一件独立环境Sprite：J1分叉口的无字双箭头木路牌。只绘制一件完整组合，不生成完整场景，也不把参考图裁切块直接作为成品。

参考图职责：图1是已验收J1 v3路牌的局部放大，只用于锁定路牌本身的剪影、比例、上下错位、木材和连接方式。图1里的天空、山、草、石块、道路及背景物件全都不是本素材。图2是已验收的完整J1场景，只辅助明亮日间光照、暖棕木色和高清像素色簇画风，不复制其环境布局。图1决定路牌造型，图2辅助光照和绘制语言。

保留一根粗粝、略不规则但直立的木柱，顶部自然平切或轻微劈裂。柱上只有两块有厚度的箭头形木牌：上方牌向画面左伸出并指左，稍低的牌向画面右伸出并指右。保持参考图清楚的上下错位、两侧伸展和相近板宽，不改成同高镜像双箭头，不增加第三块直行牌。板面由粗加工木板形成，保留少量钉点、木纹、裂隙和崩边，结构仍完整。木柱和木牌用参考图中的朴素麻绳或木连接固定。两块板面完全无文字、数字、字母、徽记、符号和涂画。

采用参考图的近正面观察角度，只保留轻微可见的板厚和木柱侧面体积；柱身竖直、箭头方向清楚，不改变为背面、强烈三分之四斜拍或等距俯视。参考图柱脚被草和石块遮挡，必须补全隐藏的木柱下端并给出自然稳定的底部脚点，不能把遮挡的草、石堆或土地一起复制。不要新增木座、石墩、圆盘、展示平台、土台或外伸支撑架。路牌自身是一件连贯组合，两块箭头和绑绳都不能断开悬空。

沿用J1明亮日间受光关系：暖金棕色受光木面、较深偏冷棕色阴影、清楚的厚度与边缘高光。以大体块、阶梯轮廓、硬边明暗色簇和克制木纹塑造粗木材质。小尺寸观看时箭头剪影和柱身仍清楚；不使用随机碎像素、照片纹理、光滑3D、普通插画马赛克滤镜、低分辨率复古像素或玩具感卡通比例。

输出1024×1024真正RGBA透明PNG。完整主体居中，最高木柱、左右箭头尖端、绑绳和柱脚全部入画，保持原宽高关系并等比绘制。主体占画布高度约80%–85%，四边至少64像素真实透明安全留边，不拉伸填满方形画布。木材和绳子接近不透明，外部为空透明，边缘干净；不绘制投影地面、接地阴影、背景色、棋盘格、白灰边、雾、光晕或背景残留。只交付这一个可独立摆放的路牌。

## 6. English full prompt

Create ONE independently deployable environment sprite for a high-definition pixel-cluster Three Kingdoms battlefield game: the unlettered double-arrow wooden signpost at the J1 fork. Render only this complete assembled prop, not a full scene and not a background-containing crop used as the final asset.

REFERENCE ROLES: Image 1 is an enlarged local crop of the signpost in the approved J1 v3 scene. It controls ONLY the signpost silhouette, proportions, staggered board heights, timber appearance and fastening. Its sky, mountains, plants, stones, roads and surrounding props are NOT part of this asset. Image 2 is the approved complete J1 scene and supports only its bright daytime lighting, warm brown wood palette and high-definition pixel-cluster rendering language. Do not copy the environment layout. Image 1 controls the prop design; Image 2 supports lighting and style.

Preserve one substantial, rough-hewn, slightly irregular but upright wooden post with a naturally flat-cut or lightly split top. Attach EXACTLY TWO solid wooden arrow boards: the HIGHER board extends toward screen-left and points LEFT; the SLIGHTLY LOWER board extends toward screen-right and points RIGHT. Preserve the reference's clearly staggered heights, lateral reach and comparable board widths. Do not move the two boards to the same height or add a third forward-pointing board. The boards are rough timber with a few small nail marks, restrained wood grain, cracks and chipped edges while remaining structurally intact. Use the reference's simple hemp-rope or wooden fastening. Both board faces are completely blank: NO lettering, words, numbers, letters, emblems, symbols or painted markings.

Use the reference's nearly frontal observation angle with only subtle visible board thickness and post side planes. Keep the post upright and both arrow directions instantly readable. Do not turn the prop to its back, a strongly oblique three-quarter camera or a top-down isometric view. Grass and stones conceal the original post's lower end: reconstruct the hidden wooden shaft to a natural, stable bottom footing without copying those occluders. Do not add a wooden stand, stone pedestal, disk base, display platform, earth mound or projecting support frame. Keep the two arrows, post and fastening as one coherent assembly with no disconnected or floating parts.

Match the J1 bright daytime lighting: warm golden-brown lit timber planes, deeper slightly cooler brown shadows, readable thickness and purposeful edge highlights. Use strong large forms, stepped silhouettes, hard-edged shading clusters and restrained wood texture. Keep both arrows and the post clear when viewed at game scale. No random pixel noise, photographic texture, smooth 3D render, mosaic-filtered illustration, low-resolution retro pixel art or toy-like cartoon proportions.

Deliver a TRUE RGBA transparent PNG at 1024x1024. Center the entire prop, including the top of the post, both arrow tips, ropes and bottom footing. Preserve its width-to-height relationship with uniform scaling. Aim for approximately 80 to 85 percent of the canvas height while keeping at least 64 pixels of purely transparent margin on every side; do not stretch the prop to fill the square. Timber and rope remain nearly opaque. Everything outside the prop is genuinely transparent with clean cutout edges. No ground plane, cast or contact shadow, background color, painted checkerboard, white or gray fringe, haze, halo or background remnants. Deliver ONLY this single placeable signpost.

## 7. 负面提示词

中文：文字、数字、字母、徽记、绘制符号、单箭头、第三块直行牌、同高镜像双箭头、箭头朝错方向、断开或悬浮木牌、现代交通牌、荧光导航、金属招牌、装饰石墩、木座、展示平台、支撑架、草、灌木、碎石、道路、土台、天空、山、篱笆、房屋、塔楼、车辆、人物、武器、UI、水印、logo、白底、灰底、彩色背景、绘入棋盘格、阴影底板、接地阴影、光晕、白灰脏边、裁断箭头或柱脚、照片写实、光滑3D、马赛克滤镜、软萌玩具感、随机像素噪点。

English: lettering, text, numbers, letters, heraldry, painted symbols, one arrow, a third forward board, same-height mirrored arrows, wrong arrow direction, disconnected or floating boards, modern traffic sign, glowing navigation, metal sign, stone pedestal, wooden stand, display platform, projecting support frame, grass, shrubs, stones, roads, earth mound, sky, mountains, fences, houses, towers, carts, people, weapons, HUD, watermark, logo, white background, gray background, colored backdrop, painted checkerboard, shadow slab, cast or contact shadow, halo, white or gray fringe, cropped arrow tips or post footing, photographic realism, smooth 3D render, mosaic filter, cute toy proportions, random pixel noise.

## 8. 验收与后续边界（A01 v1已执行并获用户验收）

1. 返回1张图片，PNG尺寸1024×1024，可完整加载；机器读取Color Type/模式，核实有效Alpha、透明四角和边缘，记录透明/半透明分布与主体框。
2. 先检查剪影：只有一根柱和两块板，上左下右，无字无徽记，上下错位/木材粗细与局部参考一致；被遮挡柱脚已合理补全。
3. 不得有草、石堆、地面、山、天空、背景残片、基座或阴影平台；全轮廓及至少64px留边完整。
4. 在浅色、深色、暖土色底以及缩小图中检查边缘和箭头可读性；验证图另存，不覆盖原图，不自动抠图或修Alpha。
5. 回读原图实际附图并提供完整文件路径；技术成功不等于用户验收。若本次路由返回RGB/画入棋盘格，明确报告未获得透明成品并暂停，不自动再发收费请求。
6. 本次以独立成品图为交付目标；通过用户验收后才能在另行授权下导入/部署。当前不修改Battle、Y路线、PPU/Pivot或材质，不制作A02–A05。

## 9. A01 v1实际结果（2026-10-11）

- 输出：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_a01_blank_double_arrow_signpost_v1.png`。
- 请求、响应、状态：同目录下同基名的`.request.json`、`.response.json`、`.status.json`。
- 一次提交，HTTP200，耗时126.63秒，无自动重试；实际参数为`gpt-image-2.5-sunburst`、`1024x1024`、`quality=max`、`input_fidelity=high`、`background=transparent`、PNG、n=1。两张输入参考哈希与请求记录一致。
- PNG机器验证：936145 bytes，1024×1024，RGBA，PNG Color Type 6；四角Alpha=0，Alpha有效范围0–254，透明像素870668，半透明像素177908。原图SHA-256=`146160cd27fd9ad48a74d0075f4b590901629575b34bbc6e1eaec186058bb0b1`。
- 主体框：Alpha>16为`[119,100,904,934]`；Alpha>0为`[39,63,1006,1024]`。四边主要主体完整，柱脚没有被裁掉；Alpha>0的右边距18px、下边距0px来自极低Alpha像素，不能把Alpha>0框直接当作可见主体边界。
- 视觉检查：单根木柱、上方左箭头、稍低右箭头、绑绳和木材体积均清楚；无文字、无徽记、无草石道路背景、无棋盘格绘入。白色/深色/赭色三种底色对照已回读附图。
- 限制：无可见大面积光晕，但诊断发现Alpha=1像素共8611个，其中外部64px安全区有180个；外围其余边缘为少量低Alpha抗锯齿/散点。未进行后处理，原始API PNG完整保留。是否接受该低Alpha残留由用户美术验收决定。
- 辅助检查图（不作为正式素材）：`Library/Locus/tmp/j1-a01-validation/j1_a01_v1_edge_validation.png`、`Library/Locus/tmp/j1-a01-validation/j1_a01_v1_alpha1_diagnostic.png`。
- 当前状态：技术验证通过，视觉检查完成，用户明确回复“已验收”；`user_approval=approved`。未导入Unity，不修改场景，不开始A02–A05。用户随后要求分析组图生产方式，本轮不发起新的生成请求。
