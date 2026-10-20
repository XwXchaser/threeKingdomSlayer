---
id: kd_4ea58f45-f1e2-4cf9-a976-5ba0fcafcc66
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可破坏补给序列帧组图 · 生成词准备（2026-10-19）

> 当前只准备酒缸破坏第一帧的关键帧生成词，不授权调用 API。六格组图和木箱序列都暂停。另一个 AI 在用户明确说“发起生成”前不得提交请求。

## 0. 已核对的基图

两张基图已由用户目视验收。实际图像与旧 §10 描述不完全一致，序列词必须以实际图像为准。

酒缸：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/wine_jar/Prop_WineJar_base_v1.png`

- SHA-256：`368a366b95cdcd2488042b59dd0c823179871cc0ac70edb1f7b26a7d537976b5`
- 1024×1024，RGBA，PNG Color Type 6，四角 Alpha 0。
- 实际形体：矮胖圆腹陶缸，正面略偏，口部有破边麻布和双股麻绳；右侧有一条垂下的绳结。腹部已有一条从左下方向上的旧裂纹，以及横向磨损环。
- 实心包围盒约 `(53,112)-(974,956)`，主体很大，组图时必须缩小，不能原样放进 512 格。

木箱：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/Prop_Crate_base_v1.png`

- SHA-256：`c6757ef3bfa794a30e89ba1f20149edfbacf25f2a16dd31a5fa57ffe96702d0a`
- 1024×1024，RGBA，PNG Color Type 6，四角 Alpha 0。
- 实际形体：近立方军需箱，轻微四分之三视角，顶面三块木板，正面和右侧面清楚；两道水平冷铁灰铁箍，四角包铁，正面已有一条竖向旧裂。
- 实心包围盒约 `(50,104)-(986,959)`，同样必须在组图中缩小。

## 0.1 当前生成入口：酒缸破坏第一帧

用户要求先定型酒缸破裂的第一帧形象。当前只批准准备这一张关键帧，不准备或生成 `Break_3..6`，也不生成木箱。

已作废的反面参考：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_break_v1/wine_jar/Prop_WineJar_Break_2.png`

它的问题是腹部大面积内压炸开，看不出从左侧刺入的冲击。不得上传，不得作为编辑输入。

待批准的单次请求：

```text
POST https://api.muskapis.com/v1/images/edits
Content-Type: multipart/form-data
image[]: C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/wine_jar/Prop_WineJar_base_v1.png
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
background: transparent
output_format: png
n: 1
retries: 0
timeout: 300 seconds
```

输出：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_break_keypose_v2/wine_jar/Prop_WineJar_Impact_v1.png`

只上传已验收基图一张。实际英文 Prompt 必须与本节逐字一致；中文只供审阅。Key 只读 `MUSK_API_KEY`，不落盘、不回显。同名成功图已存在、请求失败、超时或结果不对时停止，不自动重试。

### 0.2 v1 结果与失败原因（2026-10-19）

`Prop_WineJar_Impact_v1.png` 已生成，但用户判定“完全没破裂”，该图作废，不得作为后续参考或编辑输入。

失败原因在提示词：它要求“只增加一个小穿孔、几条短裂纹、两三块很小的陶片仍贴近缸壁”，同时禁止“腹部大洞”。基图本身已有大面积旧裂纹和破损，模型只重绘了旧磨损，没有形成新的可读破裂。

v2 仍未执行，且方向不够：它仍写“刚被刺中的瞬间”，不是用户要求的第一帧完成态。

v3 的第一帧必须直接是酒缸已经破裂的形象：左半腹被向右打碎，约一半腹壁缺失，五到七块大陶片已经飞到右侧，暗色空心清楚可见。右侧后壁仍连接口沿和缸底，避免变成四周均匀自爆。麻布和绳结仍在，不出现酒液。

### 0.3 v1 生成记录

- 已按本节提交一次：HTTP 200，耗时 62.6s，路由 `r2.52image.xyz`，usage in 1323 / out 7024，cost 未返回。
- 文件：`C:/Users/Administrator/Pictures/gptGen/breakable_props_break_keypose_v2/wine_jar/Prop_WineJar_Impact_v1.png`，1,746,411 bytes，SHA-256 `032e993bfdb4e821c0063e864a14037530171e68a945a2933513b207a56e0ab5`。
- 机器验收：1024×1024、RGBA / Color Type 6、四角 Alpha 0、8px 边带 max alpha 1、Alpha 范围 0–254，通过。
- 几何对比基图：实心包围盒 `22,58–1008,967`（基图 `53,112–974,956`），宽 +65、高 +65、最低实心行 +11 → 模型整体放大约 7% 并下移 11px，与“保持相同比例、位置”有偏差，需目视判定是否可接受。
- 已落盘追溯文件：`prompt.en.txt`、`base_reference.png`、`request.json`、`response.json`、`status.json`、`Prop_WineJar_Impact_v1_preview_white.png`。
- 观感验收待用户完成；未生成六格组图、未生成木箱、未裁切、未导入 Unity。

### v3 中文提示词

编辑所提供的已验收粗陶酒缸，制作一张 1024×1024、真正 RGBA 透明的破坏第一帧。第一帧直接呈现酒缸已经破裂的结果，不是刚出现小孔的瞬间，也不是新缸。

保持相同画布、机位、比例、位置、暖陶土配色、左上暖光、偏冷暗部、阶梯状像素色块和绘制密度。保留矮胖圆腹、破边麻布、双股绳圈、右侧垂下的绳结、口沿和缸底。

左半边腹壁已经被从左侧向右打碎，缺失面积约占整个腹壁的一半。五到七块大陶片已经向右侧和右上方飞出，并明显离开缸体，露出大面积暗色空心。破口边缘朝右翻卷。右侧后壁仍连接口沿和缸底，使破坏方向清楚，不像四周均匀自爆。麻布和绳结仍留在缸上。不出现酒液，不画武器、枪尖或人物。

纯透明背景。不画地面、投影、尘烟、文字或第二只缸。禁止细纹、小孔、未脱落碎片、四周放射炸裂、整缸消失、紫红色液体、厚涂插画、照片写实、光滑3D、柔化光晕、白边、灰边、棋盘格、白底和灰底。

### v3 English prompt

Edit the provided approved coarse earthen wine jar into one genuinely transparent RGBA 1024x1024 first break frame. The first frame must directly show the jar already broken, not the instant a small hole appears, and not a new jar.

Keep the same canvas, camera angle, scale, placement, warm clay palette, warm upper-left light, cooler shadows, stepped pixel clusters, and rendering density. Preserve the short round belly, torn hemp-cloth cover, double rope loop, hanging rope knot on the right, mouth rim, and base.

The left half of the belly wall has already been smashed from screen left toward the right, with about half of the belly wall missing. Five to seven large clay shards have already flown rightward and upward-right and are clearly separated from the jar, exposing a large dark hollow interior. The broken rim curls toward the right. The right rear wall still connects the mouth rim to the base, so the impact direction remains readable and the jar does not look evenly exploded from internal pressure. The cloth and rope knot remain attached. No liquid appears. Do not draw a weapon, spear tip, or person.

Use a pure transparent background. Do not draw ground, cast shadows, dust, smoke, text, or a second jar. Forbidden: hairline cracks, a small puncture, attached unbroken chips, cracks radiating evenly in every direction, a completely vanished jar, purple or red liquid, painterly illustration, photorealism, glossy 3D, soft glow, halo, white fringe, gray fringe, checkerboard, white background, gray background.

## 1. 为什么改成一张组图

`plan/breakable-props-base-generation-handoff-20261019.md` §10 仍是逐帧单图方案，且其“共享前缀”实际只描述酒缸，不能直接执行。

本方案改为每个道具一张 1536×1024 六格组图：

- 六个阶段在同一次生成中共享机位、比例、配色和像素密度。
- 第一格由模型按已验收基图缩小重绘；部署时仍用原基图字节作为 `Break_1`，不把组图第一格拉伸回 1024。
- 组图只负责 `Break_2..6` 的连续动作和第一格对照。
- 已知风险：`input_fidelity=high` 不能锁定像素，模型仍可能重绘整图或忽略格线。因此先各做一张探针，不自动批量重试。

## 2. 格位

画布 1536×1024。原点在左上，右边界和下边界不包含。

| 阶段 | 格位 | 画面职责 |
|---|---|---|
| 1 | `(0,0)-(512,512)` | 完整基图对照 |
| 2 | `(512,0)-(1024,512)` | 刚被戳击 |
| 3 | `(1024,0)-(1536,512)` | 开始崩开 |
| 4 | `(0,512)-(512,1024)` | 主体解体 |
| 5 | `(512,512)-(1024,1024)` | 碎片下坠 |
| 6 | `(1024,512)-(1536,1024)` | 贴地残址 |

每件主体在各自 512×512 格内居中，距离格边界至少 48px。相邻物件之间保持真实透明隔离。格线只存在于提示词中，不得画进图片。

## 3. 待批准请求

每个道具一次请求，两次请求相互独立。

```text
POST https://api.muskapis.com/v1/images/edits
Content-Type: multipart/form-data
image[]: exactly one approved base image
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
background: transparent
output_format: png
n: 1
retries: 0
timeout: 300 seconds
```

输出路径：

```text
C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v1/wine_jar/Prop_WineJar_BreakAtlas_v1.png
C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v1/crate/Prop_Crate_BreakAtlas_v1.png
```

Key 只读 `MUSK_API_KEY`，不写入文件、日志或回复。提交前解析实际 multipart，确认只有一张图、字段完整，且英文 Prompt 与本文逐字一致。中文只供审阅，不提交。

同名成功 PNG 已存在时停止。HTTP 失败、超时、非 RGBA、画入棋盘格、格位错乱或观感不对，都停止，不改词、不降档、不自动重试。

## 4. 酒缸序列

输入只上传：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/wine_jar/Prop_WineJar_base_v1.png`

### 中文提示词

编辑所提供的已验收粗陶酒缸，制作一张 1536×1024、真正 RGBA 透明的六阶段破坏组图。只画这一只缸的连续破坏，不设计新缸，不加入第二只缸。

使用六格，原点在左上，右边界和下边界不包含：

- `(0,0)-(512,512)`：完整未受新破坏的酒缸。忠实缩小输入图，保留矮胖圆腹、破边麻布、双股绳圈、右侧垂下的绳结、左下方旧裂纹和横向磨损环。
- `(512,0)-(1024,512)`：同一位置刚被从左侧水平戳击。腹部左侧只有一个小穿孔，孔口边缘向右内压陷，裂纹从该孔向右侧延伸。两三块小陶片向右方刚刚离开，仍贴近缸壁。右侧缸壁、麻布和绳结保持完整，没有液体涌出。
- `(1024,0)-(1536,512)`：左侧受击区被向右推开，四到六片大陶片向右侧和右上方翻出，移动距离约为缸宽的五分之一。一股暗黄褐色浊酒从左侧破口向右浅出，带少量浅色泡沫。右侧后壁仍连成一体，麻布只轻微松动。
- `(0,512)-(512,1024)`：左侧大陶片继续向右上方飞出，浊黄酒形成一条向右的短浅流，第一次看到空心内壁。麻绳从口部松开并向右侧垂落。右侧后壁仍然站立。
- `(512,512)-(1024,1024)`：大陶片沿向右的冲击方向下落，多片翻出内凹面；浊黄酒散成向右飞的小液滴。数片陶片已到达原接地高度，右侧大陶壁开始倾斜。
- `(1024,512)-(1536,1024)`：原足迹内只剩低矮残址：八到十片大小不同的陶片平放或低斜靠，较大碎片偏向右侧；另有皱起的麻布、松开的绳结和一小块暗黄褐色湿痕。没有站立的缸，没有任何东西悬空。

六个阶段保持相同机位、比例、暖陶土配色、左上暖光、偏冷暗部、阶梯状像素色块和绘制密度。只推进破坏程度。所有主体和碎片距离格边界至少 48px，不得触边或跨格。

破坏必须是从画面左侧水平刺入的外部冲击，不是缸内压力引起的自爆。穿孔、裂纹和飞出的陶片都从左侧受击点开始，大碎片主要向右侧和右上方移动；缸体右侧后壁在前四格保持完整。不画武器、枪尖或人物。

内容物是三国浊黄酒，不是红葡萄酒。液体使用暗黄褐、暗琥珀色的浊液，带少量浅色泡沫；不得使用紫红、品红、深红或现代红酒色。液体只从左侧破口向右浅流出，不得从口部向上喷泉。

不画格线、编号、文字、地面、地平线、投影、尘云、烟、火星、酒杯或第二只缸。禁止厚涂插画、照片写实、光滑3D、萌系、平滑渐变、柔化光晕、白边、灰边、棋盘格、白底和灰底。

### English prompt

Edit the provided approved coarse earthen wine jar into one genuinely transparent RGBA 1536x1024 damage atlas. Show only six consecutive damage stages of this same jar. Do not design a new jar and do not add a second jar.

Use six cells. The origin is the upper-left, and right and bottom edges are exclusive:

- (0,0)-(512,512): the intact jar before the new impact. Faithfully redraw the input at a smaller scale. Preserve its short round belly, torn hemp-cloth cover, double rope loop, hanging rope knot on the right, existing lower-left crack, and horizontal wear rings.
- (512,0)-(1024,512): the same jar has just received a horizontal thrust from screen left. Add one small puncture only on the left side of the belly, with its rim pressed inward toward the right and cracks running rightward from that hole. Two or three tiny clay chips have just broken away toward the right but remain close to the wall. The right wall, cloth, and rope knot remain intact, and no liquid spills.
- (1024,0)-(1536,512): the left impact area is pushed open toward the right. Four to six large shards flip rightward and upward-right, moving about one fifth of the jar width. A stream of opaque muddy yellow-amber millet wine spills rightward from the left breach, with a few pale bubbles. The right rear wall remains connected, and the cloth only loosens slightly.
- (0,512)-(512,1024): the large left-side shards continue flying upward-right, the turbid yellow wine forms one short rightward stream, and the hollow inner wall becomes visible for the first time. The rope loosens from the mouth and hangs toward the right. The right rear wall is still standing.
- (512,512)-(1024,1024): the large shards fall along the rightward impact direction, many showing concave inner faces. The turbid yellow wine breaks into small droplets traveling right. Several shards have reached the original ground-contact height, and the large right wall begins to tilt.
- (1024,512)-(1536,1024): only a low ruin remains inside the original footprint: eight to ten flat or low-leaning clay shards of varied sizes, with the larger pieces shifted toward the right; crumpled hemp cloth, a loose rope knot, and a small muddy yellow-amber damp stain. No jar remains standing, and nothing floats.

Keep the same camera angle, scale, warm clay palette, warm upper-left light, cooler shadows, stepped pixel clusters, and rendering density across all six stages. Advance only the damage. Keep every object and fragment at least 48 pixels inside its cell boundary. Nothing may touch an edge or cross into another cell.

The damage must read as an external horizontal spear thrust entering from screen left, not as internal pressure or an explosion. The puncture, cracks, and flying shards all originate at the left impact point, and the large debris travels mainly rightward and upward-right. The right rear wall remains intact through the first four stages. Do not draw the weapon, spear tip, or a person.

The contents are turbid Three Kingdoms millet wine, not red grape wine. Use an opaque muddy yellow-amber liquid with a few pale bubbles. Do not use purple, crimson, deep red, or modern red-wine colors. The liquid spills rightward only from the left breach and must not erupt upward from the mouth.

Do not draw cell lines, numbers, text, ground, a ground line, cast shadows, dust, smoke, sparks, a cup, or a second jar. Forbidden: painterly illustration, photorealism, glossy 3D, cute style, smooth gradients, soft glow, halo, white fringe, gray fringe, checkerboard, white background, gray background.

## 5. 木箱序列

输入只上传：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/Prop_Crate_base_v1.png`

### 中文提示词

编辑所提供的已验收粗木军需箱，制作一张 1536×1024、真正 RGBA 透明的六阶段破坏组图。只画这一只箱子的连续破坏，不设计新箱，不加入第二只箱。

使用六格，原点在左上，右边界和下边界不包含：

- `(0,0)-(512,512)`：完整未受新破坏的木箱。忠实缩小输入图，保留近立方体、轻微四分之三视角、顶面三块木板、正面与右侧面、两道水平冷铁灰铁箍、四角包铁，以及正面已有的竖向旧裂。
- `(512,0)-(1024,512)`：同一位置刚被戳击。正面中央出现一个小而清楚的戳击孔，裂缝沿木纹向两条铁箍延伸。一两块小木屑刚离体但仍贴得很近。两条铁箍仍固定，箱子仍是一个完整实体。
- `(1024,0)-(1536,512)`：正面木板向外崩成三到四块，张开约箱宽的五分之一。上铁箍被顶起并轻微离开原位，缝隙中飞出木屑。箱体轮廓仍可读。
- `(0,512)-(512,1024)`：正面和侧面板向外翻开，两条铁箍正在脱离，箱高减少约三分之一。木板、木屑和一块角铁向外飞散，箱底仍在原接地位置。
- `(512,512)-(1024,1024)`：大部分木板已经离开箱体，只剩底板和一角靠近地面。铁箍与角铁在半空下坠，木屑向外散开。
- `(1024,512)-(1536,1024)`：原足迹内只剩低矮残址，包括长短不一的板条、一条弯曲铁箍、一块角铁和一小堆木屑，全部平放或低斜靠。没有站立结构，没有任何东西悬空，也不再有箱子形状。

六个阶段保持相同机位、比例、暖木配色、冷铁灰铁件、左上暖光、偏冷暗部、阶梯状像素色块和绘制密度。只推进破坏程度。所有主体和碎片距离格边界至少 48px，不得触边或跨格。

不画格线、编号、文字、地面、地平线、投影、尘云、烟、火星、标签或第二只箱。禁止现代塑料箱、厚涂插画、照片写实、光滑3D、萌系、平滑渐变、柔化光晕、白边、灰边、棋盘格、白底和灰底。

### English prompt

Edit the provided approved rough timber supply crate into one genuinely transparent RGBA 1536x1024 damage atlas. Show only six consecutive damage stages of this same crate. Do not design a new crate and do not add a second crate.

Use six cells. The origin is the upper-left, and right and bottom edges are exclusive:

- (0,0)-(512,512): the intact crate before the new impact. Faithfully redraw the input at a smaller scale. Preserve its near-cubic form, slight three-quarter view, three top planks, readable front and right faces, two horizontal cool iron-gray bands, iron corner plates, and the existing vertical crack on the front.
- (512,0)-(1024,512): the same crate has just been struck. Add one small, clear spear-thrust hole at the center of the front face, with a split running along the grain toward both iron bands. One or two small splinters have just separated but remain very close to the surface. Both bands stay fastened, and the crate remains one solid object.
- (1024,0)-(1536,512): the front planks burst outward into three or four boards, opened by about one fifth of the crate width. The upper iron band is pushed upward and slightly lifted, while wood chips fly from the gap. The crate silhouette remains readable.
- (0,512)-(512,1024): the front and side boards swing outward, both iron bands are coming off, and the crate has lost about one third of its height. Boards, splinters, and one corner plate fly outward, while the crate bottom remains at the original ground contact.
- (512,512)-(1024,1024): most boards have left the crate. Only the bottom panel and one corner remain near the ground. The iron bands and corner plates are falling in midair, and wood chips scatter outward.
- (1024,512)-(1536,1024): only a low ruin remains inside the original footprint: plank strips of varied lengths, one bent iron band, one iron corner plate, and a small heap of splinters, all lying flat or leaning low. Nothing stands, nothing floats, and no crate shape remains.

Keep the same camera angle, scale, warm timber palette, cool iron-gray fittings, warm upper-left light, cooler shadows, stepped pixel clusters, and rendering density across all six stages. Advance only the damage. Keep every object and fragment at least 48 pixels inside its cell boundary. Nothing may touch an edge or cross into another cell.

Do not draw cell lines, numbers, text, ground, a ground line, cast shadows, dust, smoke, sparks, a label, or a second crate. Forbidden: modern plastic crate, painterly illustration, photorealism, glossy 3D, cute style, smooth gradients, soft glow, halo, white fringe, gray fringe, checkerboard, white background, gray background.

## 6. 验收口径

生成后先做机器检查：

- 1536×1024、RGBA、PNG Color Type 6、四角 Alpha 0。
- 背景没有画入棋盘格。
- 以 Alpha 大于 8 的连通区域检查六件是否分开；不能按固定 512 线硬切相连物体。
- 8px 外边带内不得有有效碎片。

视觉验收必须逐格看：

- 第一格是否仍是同一只缸或箱，而不是新设计。
- 六格是否是同一机位和比例下的连续动作。
- 酒缸是否保留麻布、绳结和旧裂纹身份；木箱是否保留三分面、双铁箍和角铁身份。
- 末格是否只剩贴地残址，且没有悬空碎片。
- 缩小到每格 128 高后，前三格仍能辨认原物件。

通过后才讨论按实际 Alpha 边界裁出 `Break_2..6`。`Break_1` 永远使用已验收基图原字节，不从组图裁切。未通过时不自动修图、不自动付费重生成。

## 7. 生成记录（2026-10-19，两张组图各提交一次）

酒缸：

- `C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v1/wine_jar/Prop_WineJar_BreakAtlas_v1.png`
- 2,516,260 bytes，SHA-256 `ff6124e4ec38b7d15e79de6c8bec29c0fad507cdcacdcb42b2abbcba865e3fe5`，1536×1024 / Color Type 6，HTTP 200，57.4s，usage in 1829 / out 5488。

木箱：

- `C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v1/crate/Prop_Crate_BreakAtlas_v1.png`
- 2,469,345 bytes，SHA-256 `5767fe1b301bee6737a0277c14bc40a31f95edfa0cacddc228630a3965808d4f`，1536×1024 / Color Type 6，HTTP 200，61.0s，usage in 1642 / out 5488。

两张均通过机器检查：四角 Alpha 0、8px 外边带无有效碎片（`alpha>8` 像素数 0）、无跨格连通域、未画入棋盘格。连通域（`alpha>8`，≥200px）逐格数量：酒缸 `1/1/6/7/10/1`，木箱 `1/1/5/8/16/2`；六格均未相互粘连。

不符合项（不阻断裁切，但需记录）：格内留白未达本文要求的 48px。酒缸最小边距 12px（格 3 左侧）、格 4 右 18px、格 5 右 20px；木箱最小 22px（格 4 右侧）。裁切时若按实际 `alpha>8` 包围盒取帧，格间不会互相入侵，但边缘余量为零。

未做：未裁切 `Break_2..6`、未导入 Unity、未改动项目资产；观感验收（是否仍是同一只缸/箱、是否连续、是否左侧刺入）待用户完成。
