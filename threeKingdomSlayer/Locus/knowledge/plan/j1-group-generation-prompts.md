---
id: kd_c5703d62-8423-4bc3-9447-05bb33d68039
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# J1 路侧道具透明组图 G1：完整运输车与两种低篱笆

## 1. 状态与范围

用户已验收J1 A01双箭头路牌，并明确要求“同意，请发起一次组图场景物件生成”。本次按既有授权直接提交一张三件透明物件图集；完整中英文提示词、负面词、参考职责及参数记录在下方。质量沿用用户同意的组图默认high，不采用准备过程临时提及的medium，不另行要求重复确认。

**已按用户授权提交一次请求并生成G1 v1，待用户验收。** 三件为A05完整运输车、A03完整低篱笆、A04破损低篱笆。实际使用1536×1024、quality=high、透明PNG；原图已回读并检查三种底色。模型未严格遵守y=512格界，但三件独立有间隔，建议后续将裁切线改为y=588；当前未裁切、后处理或导入Unity。已验收A01直接复用，A02石路平铺材质不放入Sprite组图；不制作其他灌木、石块、旗、房屋或场景背景。

## 2. 输出版式与格位

拟输出横向组图`1536×1024`，固定三格，位置以左上为(0,0)，右下坐标不包含：

| 格位 | 边界(x0,y0,x1,y1) | 资产 | 剪影要求 |
|---|---|---|---|
| 上格 | (0,0,1536,512) | A05完整木质运输车 | 一辆完整双轮木车；整体横向，车厢、两轮、车轴与车辕均在格内 |
| 左下格 | (0,512,768,1024) | A03完整低木篱笆 | 一段低矮、稀疏、完整的乡村道路篱笆 |
| 右下格 | (768,512,1536,1024) | A04破损低木篱笆 | 与A03同高度/木料/光照的一段，带有限断杆和缺口 |

- 每格仅有一件完整资产；格边至少32像素真实透明间距，内部留出约48像素安全边；素材不能跨格、相碰、投影/溢色到相邻格。
- 不画格线、分隔线、标签、字母编号、边框或网格底板。未被主体占据的区域必须是真实透明。
- 车不做两辆不同车或车队；上下篱笆是一对匹配变体，不要额外加木箱、旗帜、木桩堆或碎木碎片图标。
- 采用1536×1024和high，保留主要剪影与适中细节。模型可能不严格服从格位；若主体明显跨格或挤到格边，整图不直接切片。

## 3. 参考图职责

1. **Image 1：左下篱笆局部形体参考** `Library/Locus/tmp/j1-group-preparation/j1_group_fence_shape_crop_x3_v1.png`，1170×840 RGB；哈希=`135afd0e81af6f7cdf35e64cdce17bb6769ff171daa0fb05af0ff6845faf4a77`。只决定低矮乡村篱笆的疏密、木桩横杆关系及路侧尺度。裁图里出现的石路、草、山、树、墙与原有其他篱笆背景全部忽略。
2. **Image 2：右下运输车局部形体参考** `Library/Locus/tmp/j1-group-preparation/j1_group_cart_shape_crop_x3_v1.png`，702×990 RGB；哈希=`9e0e061001aea1da1595a03d5b3f33fe315362c9e7b7cbe07b5babb0f6741458`。只决定古代木车车厢、双轮和侧前视比例；参考车部分被画幅裁切，需完整重构为一辆独立车辆。裁图里的红旗、木架、墙、石块、地面、烟和天空全部忽略。
3. **Image 3：已验收A01材质/绘制风格参考** `C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_a01_blank_double_arrow_signpost_v1.png`，1024×1024 RGBA；哈希=`146160cd27fd9ad48a74d0075f4b590901629575b34bbc6e1eaec186058bb0b1`。只辅助木材冷暖、受光和像素色簇；不要生成路牌、箭头、绑绳柱或重复图中对象。

不上传带文字/框线的拆分图，不上传整幅J1 v3，避免模型复绘整场景或把路牌复制进组图。

局部参考制作清单：`Library/Locus/tmp/j1-group-preparation/j1_group_reference_manifest_v1.json`。裁图是分析参考，不是成品，不作后处理。

## 4. 请求参数（按用户的一次组图生成授权执行）

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: high
input_fidelity: high
background: transparent
output_format: png
n: 1
image[] order: fence crop, cart crop, accepted A01 style reference
automatic retries: 0
```

- `high`沿用已同意的组图默认候选质量；三件共享一张1536×1024图集，不按单件max绘制，细节服从小尺寸剪影可读性。
- 已有透明实测基线为sunburst edits + `quality=max` + `input_fidelity=high` + `1024x1024` + transparent PNG，返回RGBA。**本次1536x1024 + high + 三件透明图集组合尚无本项目实测**；这一次结果必须实际读IHDR/Alpha，可能出现格式、尺寸或透明能力不符合预期。若返回非RGBA、假棋盘格、HTTP失败、超时或结果不明，只保留记录，不自动重试、不转白底、不再付费补生成。
- 只提交一次，n=1。凭据仅使用环境变量MUSK_API_KEY，不写入聊天、计划文件或请求内容。
- 拟输出目录：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/`。
- 拟图集：`j1_roadside_props_atlas_g1_v1.png`；同目录同基名保存`.request.json`、`.response.json`、`.status.json`。
- 生成后机器/视觉检查通过时，固定坐标裁出3个派生候选，保存至`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_roadside_props_atlas_g1_v1_crops/`：`j1_a05_complete_wooden_cart_v1.png`、`j1_a03_village_fence_intact_v1.png`、`j1_a04_village_fence_broken_v1.png`。派生物与母图分开，不覆盖；裁切不等于Unity导入或美术验收。

## 5. 中文完整提示词

为高清像素化三国战场游戏制作一张横向、真实透明的环境道具Sprite组图。画布1536×1024，严格按下面三个固定矩形格位绘制；每格是一件完整、单独可用的物件，绝不跨格。

格位布局（坐标原点在左上，右/下边界不包含）：
- 上方整幅格(0,0)至(1536,512)：只绘制一辆完整古代双轮木质运输车，三分之四侧前视。画全车厢、左右两只车轮、车轴、支撑结构和向前伸出的车辕/牵引杆。主体以横向轮廓为主，完整位于此格中央，并留至少48px内部安全边。
- 左下格(0,512)至(768,1024)：只绘制一段完整的低矮乡村木篱笆，稀疏木桩与一至两根横杆；开放、简朴，不形成高墙。
- 右下格(768,512)至(1536,1024)：只绘制一段与左下格同一家族、同高度、同木料和同受光方向的破损低木篱笆；只做一至两处断杆、倾斜木桩或自然缺口，整体仍明显是路侧篱笆。

参考图职责：图1只提供村路篱笆的低矮尺度、稀疏木桩横杆和粗木材身份；忽略图1中的全部道路、草、石、山、树、墙及背景篱笆。图2只提供古代运输车厢、双轮和车辕的形体比例；它在参考中被裁切，请补全完整车体，并忽略其中旗帜、木架、墙石、地面、烟雾与天空。图3只提供已验收A01的木材色彩、日间光照与高清像素色簇绘制语言；不要重复图3的路牌。

三件素材使用统一的高清像素化三国环境美术：清楚的大色块、坚实结构、阶梯状轮廓、硬边受光面与阴影簇。木材采用暖棕/赭棕受光面、较深偏冷棕色暗部，材质厚重但不整体压黑。保留少量必要木纹与连接细节，缩小后剪影仍清楚；不追求密集微小纹理或A01级别精雕。

木车必须是一辆完整、稳定可放置的古代运输车：两只尺寸合理的木辐轮、清楚的车轴、完整车厢围板、必要横梁与车辕；车轮不互相重叠成一团，车厢不悬空。木车本体不载人、不带马匹、不带巨旗，车厢可为空；不附加木箱，以便后续独立摆放现有箱子。

透明与分格要求：整个画布输出真正RGBA透明PNG，画布外及每格未被物件占据区域完全透明。不要添加任何格线、分隔线、编号、文字、标签、边框、白底、灰底、颜色底板或棋盘格。每格内的素材边缘与阴影都不得触碰或跨越格界；格与格之间保持透明隔离。每个物件完整居中在自己的格内，至少48像素透明留边；不可裁断车辕、车轮、篱笆端柱或脚点。只画道具本体，不加地面投影、草、石块或共享底座。

禁止：整张J1场景、天空、远山、地面、弯曲道路、石路、村庄、房屋、屋顶、塔楼、营地、敌人、人物、马匹、旗帜、徽记、文字、数字、箭头路牌、木箱、草簇、碎石、火把、烟尘、UI、多个木车、车队、断裂失去承重的车轮/车体、密排尖桩军事围墙、拒马、石基高墙、任何格线/标号/版面文字、白/灰/彩色背景、绘入棋盘格、白边/灰边/光晕、照片写实、光滑3D、马赛克滤镜、随机像素噪点。

## 6. English full prompt

Create one horizontal, genuinely transparent environment-prop sprite atlas for a high-definition pixel-cluster Three Kingdoms battlefield game. Canvas: 1536x1024. Follow the three fixed rectangular cells below exactly. Each cell contains one complete, independently usable prop. NO prop may cross a cell boundary.

CELL LAYOUT (origin at upper-left; right and bottom coordinates are exclusive):
- TOP FULL-WIDTH CELL, (0,0) to (1536,512): exactly ONE complete ancient two-wheeled wooden transport cart, shown in a three-quarter side-front view. Include the full cargo bed, both wheels, axle, structural supports and forward shafts/pull-beams. Keep a predominantly horizontal silhouette, fully inside this cell, centered with at least 48 px of internal transparent safety margin.
- BOTTOM-LEFT CELL, (0,512) to (768,1024): exactly ONE intact low rural wooden roadside fence segment, with sparse posts and one or two rails. Open and modest, never a high wall.
- BOTTOM-RIGHT CELL, (768,512) to (1536,1024): exactly ONE damaged low wooden fence segment matching the bottom-left fence in family, height, timber, scale and light direction. Use only one or two broken rails, leaning posts or natural gaps; it must remain clearly recognizable as a roadside fence.

REFERENCE ROLES: Image 1 supplies only the low rural fence scale, sparse post-and-rail arrangement and rough timber identity. Ignore all roads, grass, stones, mountains, trees, walls and background fences in that crop. Image 2 supplies only the ancient cart-bed, two-wheel and shaft proportions. The cart is cropped in the reference; reconstruct the complete cart and ignore all flags, timber frames, walls, rocks, ground, smoke and sky. Image 3 supplies only the approved A01 wood palette, daylight lighting and high-definition pixel-cluster rendering language. Do NOT reproduce the signpost in Image 3.

Use a consistent high-definition pixel-cluster Three Kingdoms environment style: clear large color masses, sturdy construction, stepped silhouettes, hard-edged lit planes and shadow clusters. Use warm brown/ochre-brown lit timber against deeper, slightly cooler brown shadows. Keep the materials weighty without globally darkening them. Use only restrained, purposeful wood grain and construction details. The silhouettes must remain readable at small game scale; do not pursue dense micro-texture or A01-level ornamental detail.

The cart must be a complete, stable, placeable ancient transport wagon: two distinct, appropriately sized spoked wooden wheels, a readable axle, a complete boarded cargo bed, essential cross-bracing and forward pull-shafts. Do not merge both wheels into one tangled shape or leave the cargo bed floating. No driver, people, horses or large flags. The bed may be empty; do not attach crates, so existing crate props can be placed independently.

TRANSPARENCY AND CELL RULES: Deliver a true RGBA transparent PNG. All canvas and cell areas not occupied by the props must be fully transparent. Do not draw cell lines, dividers, numbers, lettering, labels, borders, white/gray/color backplates or a checkerboard. Prop pixels, antialiasing and shadows must never touch or cross cell boundaries; preserve transparent separation between cells. Center each complete prop in its own cell with at least 48 px of transparent internal margin. Do not crop the cart shafts, wheels, fence end posts or feet. Props only: no ground shadow, grass, stones or shared base.

NEGATIVE CONSTRAINTS: full J1 scene, sky, mountains, ground, curved road, stone road, village, houses, roofs, towers, camp, enemies, people, horses, flags, heraldry, text, numbers, arrow signpost, wooden crates, grass clumps, rubble, torches, smoke, UI, multiple carts, wagon convoy, broken or non-load-bearing wheels/cart body, dense pointed military palisade, chevaux-de-frise, tall stone-based wall, any grid lines, cell labels or layout text, white/gray/colored background, painted checkerboard, white/gray fringe, halo, photorealism, smooth 3D render, mosaic filter, random pixel noise.

## 7. 验收与停止条件

- 机器验证：请求HTTP200并恰有1张图；文件是可读取的1536×1024 PNG；读取Color Type/Alpha，真实Alpha透明四角和区域空白；不把预览器棋盘格当透明证据。
- 分格验证：按固定坐标取三格，检查主体是否完整、没有越格、跨格阴影、混合物件、标签/分割线或靠边裁断。
- 造型验证：上格是一辆完整双轮车；左下完整低篱笆；右下匹配的轻微破损变体。三件都保留可用轮廓，缩小后仍可辨识。
- 边缘验证：将整图及裁切候选分别合成白、深色和赭土底，检查彩边、白边、外部低Alpha噪点和格间污染；不覆盖原始API图，不未经批准清除像素或重绘。
- 交付：实际read附上原始组图及真实完整路径；明确组图只是候选，未经用户逐件验收不导入Unity。
- 停止：若返回RGB、画入棋盘格、尺寸不符、主体跨格或运输车不完整，记录失败并停止；不自动重试或拆分冒充可用素材。
- A01沿用已验收成品；A02石路、其他组图、导入和场景部署均不在本次收费请求内。

## 8. G1 v1实际生成结果（2026-10-11）

- 原图：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_roadside_props_atlas_g1_v1.png`。
- 请求：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_roadside_props_atlas_g1_v1.request.json`。
- 响应：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_roadside_props_atlas_g1_v1.response.json`。
- 状态：`C:/Users/Administrator/Pictures/gptGen/j1_assets_from_approved_v3/j1_roadside_props_atlas_g1_v1.status.json`。
- 异步任务t5完成；一次请求、无自动重试，HTTP200，返回1张图，耗时98.529秒。实际参数sunburst / high / input_fidelity high / 1536x1024 / transparent / PNG / n=1；三张参考哈希与请求提示词哈希复核一致。
- 原图1863563 bytes，1536×1024 RGBA PNG，Color Type6；Alpha范围0–254，四角Alpha0，透明像素1087533。SHA-256=`3c67e0feb220deb4f2ff6074502ffcd9f24b5d830e27a27a9609b10ea242e8a7`。
- 原图和白/深/赭土色底对照均已read附图：完整木车、一段完整低篱笆、一段破损低篱笆可辨；无路牌重生成、无文字/格线/场景背景，未见明显大面积光晕。低Alpha边缘/散点仍存在，未清除或重采样原图。
- **预设格位未完全服从**：车的Alpha>16轮廓到y522，跨越预设y512的部分有346像素；不按y512直接裁切。实际三件没有视觉重叠，y522–653区间没有Alpha>16像素，建议裁切水平分界y588、下半左右分界x768。候选裁切区域为车辆(0,0,1536,588)、完整篱笆(0,588,768,1024)、破损篱笆(768,588,1536,1024)。这是后续切片建议，不代表已裁出素材。
- 辅助预览仅在项目临时目录：`Library/Locus/tmp/j1-group-validation/j1_g1_v1_three_backgrounds.png`；分析数据`Library/Locus/tmp/j1-group-validation/j1_g1_v1_validation.json`。不作为正式素材。
- 用户随后明确提出“已验收，请生成石路纹理”，G1组图已获用户验收。只交付原始组图，未创建派生切片，未导入Unity；石路纹理进入单独准备阶段。
