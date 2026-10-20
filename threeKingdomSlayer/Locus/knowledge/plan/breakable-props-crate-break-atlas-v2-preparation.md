---
id: kd_c60b7264-1bd2-4d67-959c-47f83d204821
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 木箱炸裂六格组图 v2 · 生成准备方案

> 状态：方案待用户审阅，尚未调用图片或视频生成接口。当前请求是“准备生成图片的方案，最好做组图”，不构成发起生成的授权。
>
> 本文是本轮组图建议，不把建议记为已批准的项目设计。不沿用旧版木箱组图的动作描述，不改写旧尝试的历史记录。

## 1. 本轮目标

生成一张 1536×1024、3 列 × 2 行、真实 RGBA 透明的木箱破坏六格组图。阅读顺序为上排从左到右，再下排从左到右。

- 格 1：完整箱对照。
- 格 2：第一次出现破坏就已经明显炸开，不再画“小孔加细裂纹”。
- 格 3：箱体完全解体，主要板块、铁件和破裂底板都已脱离。
- 格 4：飞散展开最大，中心留出透明空隙。
- 格 5：同一批碎片开始落下。
- 格 6：低矮分散残骸，无完整底板、托盘、立箱框或悬空部件。

本轮只产出一张组图候选，先验收动作和结构。它不是已经通过的游戏动画，也不预先承诺六帧可以直接部署。

## 2. 与失败版本的区别

1. 删除“箱高减少三分之一”“只剩底板和一角”等会诱导坍塌的描述。
2. 破坏第一格直接呈现可读的大板块脱离；底板从这一格就断成两块并开始翻起。
3. 格 3 起没有完整箱体核心；格 4 必须看到碎片之间的真实透明间隙。
4. 用同一局部坐标、安全区和原始物件尺度约束六格，禁止逐格放大填满。
5. 格 1 的箱体宽仅占单格约 47%，为飞散留空间；单格四边至少保留 80px（15.625%）空白。
6. 正向提示词先确定每格的可见结果，负面提示词再排除旧问题；不把“高保真”当作像素锁定保证。

## 3. 唯一参考图与职责

输入：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/Prop_Crate_base_v1.png`

已核对：

- 1024×1024，RGBA。
- SHA-256：`c6757ef3bfa794a30e89ba1f20149edfbacf25f2a16dd31a5fa57ffe96702d0a`。
- `alpha > 8` 主体包围盒：`(50,104)-(986,959)`。
- 已验收形象：近立方箱，轻微四分之三视角，正面与画面右侧面可见；顶面三块木板，两道水平冷铁灰铁箍，角铁、铆钉和正面旧竖裂。

参考职责：锁定同一只箱子的形体、暖木/冷铁配色、机位、受光和像素色簇。破坏推进由本方案的格位描述决定。

实际请求只上传这一张未修改基图。作废六格图、出框视频、酒缸、白底预览和整张场景均不上传。机位不变，因此受击部位写为“可见正面的偏左区域”，不要求画出原图不可见的左侧面。

## 4. 格位、尺度与坐标

坐标原点为画布左上，x 向右、y 向下，右/下边界不包含。以下坐标仅用于构图，不得绘成格线或文字。

| 格 | 整图范围 | 内容 |
|---|---|---|
| 1 | `(0,0)-(512,512)` | 完整箱对照 |
| 2 | `(512,0)-(1024,512)` | 已发生大破裂 |
| 3 | `(1024,0)-(1536,512)` | 完全解体 |
| 4 | `(0,512)-(512,1024)` | 最大飞散 |
| 5 | `(512,512)-(1024,1024)` | 碎片下坠 |
| 6 | `(1024,512)-(1536,1024)` | 落地残骸 |

每格使用相同的 512×512 局部坐标：

- 逻辑原点/接地参考点：`(256,400)`。它不随碎片包围盒移动，不画地线。
- 完整箱构图目标：约 `(136,181)-(376,400)`，宽约 240px、高约 219px，保留原图比例。
- 全阶段所有可见碎片的安全区域：`(80,80)-(432,432)`，四边至少 80px 留白。
- 格 2–5 保持相同的部件绘制尺度；展开来自部件平移和旋转，不能靠整体缩放或重新取景。
- 格 6 的低矮残骸约位于 `x=112..416、y=330..400`，接地高度与格 1 一致，不保留平整底板。

这些是生成构图要求，不是模型会精确执行的承诺。输出后必须量测；不足 80px 不得静默放宽为通过。

## 5. 单次请求规格

```text
POST https://api.muskapis.com/v1/images/edits
Content-Type: multipart/form-data
image[]: exactly one approved base image (section 3)
model: gpt-image-2.5-sunburst
size: 1536x1024
quality: max
input_fidelity: high
background: transparent
output_format: png
n: 1
```

- 实际提交的 `prompt`：§6 English 正向提示词 + 空行 + §7 English 负面提示词，逐字拼接。
- 中文内容供审阅，与英文表达相同的目标。
- 负面提示词并入 `prompt`，不假设接口支持独立 `negative_prompt` 字段。
- 客户端超时：300 秒；自动重试：0。上述两项不是上传的模型字段。
- API Key 只从 `MUSK_API_KEY` 读取，不写入文件、日志或回复。
- 原生透明是请求要求，实际结果仍需检查 PNG Color Type 与 Alpha，不能仅凭参数判断。

拟输出（本轮尚未创建）：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v2/crate/Prop_Crate_BreakAtlas_v2.png`

拟预览/追溯子目录：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_break_atlas_v2/crate/review/`

候选目录保存原始 PNG、原参考副本、双语词、请求规格、脱敏响应和检查结果；`review/` 只保存浅/深底预览。原始输出不被预览或后处理覆盖。生成前同名 PNG 已存在则停止，不覆盖。

## 6. 正向提示词

### 中文（完整审阅版）

以提供的已验收三国军需木箱为唯一身份和材质参考，制作一张 1536×1024 的真实 RGBA 透明六格破坏动画组图，3 列、2 行。上排从左到右是阶段 1、2、3，下排从左到右是阶段 4、5、6。这是同一只木箱的一次连续破坏，所有格共享同一固定机位、绘制尺度、受光和原始位置。

完整箱保持输入图的近立方体形状、轻微四分之三视角、可见正面与画面右侧面、顶面三块木板、两道水平冷铁灰铁箍、角铁、铆钉和正面旧竖裂。保持暖色粗木、偏冷暗部、左上方暖光、阶梯状轮廓和成簇明暗色块。破裂后的部件沿用这些材质和造型特征，木板断口具有明确厚度。

每格为 512×512，使用各自局部坐标，左上为 (0,0)。完整箱约宽 240px、高 219px，位于 x=136..376、y=181..400，接地参考点固定为 (256,400)。六格使用完全相同的绘制尺度和逻辑原点，后续格不按碎片包围盒重新居中或放大。所有箱体和碎片必须完整位于各格的 x=80..432、y=80..432 内，使每格四边至少有 80px 真正透明留白。坐标和格线不得画出来。

格 1，整图 (0,0)-(512,512)：完整、稳定的原木箱，忠实于输入形象，没有新增破坏，按上述尺度摆放。

格 2，整图 (512,0)-(1024,512)：来自画面左侧、指向右侧的水平冲击已经造成明显破裂。可见正面的偏左区域已经炸开，约三分之一正面缺失，三到四块较大的木板断块已经脱离箱体，向右和右上方移动，部件之间有清楚的透明缝隙。上铁箍已经断开并弹离，下铁箍张开。底板已经从中部断成两块，两块开始离开接地位置并翻起。观众一眼能看出箱子已被打破。

格 3，整图 (1024,0)-(1536,512)：完整箱体结构已经消失。正面板、右侧板、顶板和两块破裂底板全部分离，成为少量清晰的大木板断块，主要向右和右上方飞散，少量较小碎片向左侧反弹。两道铁箍以断开的弯曲铁条形态脱离，角铁也已离开原位置。两块底板明显悬空并旋转，箱子中心出现清楚的透明空隙。

格 4，整图 (0,512)-(512,1024)：同一批碎片到达最大展开范围，仍然全部处于每格 80px 安全边内。主要木板、两道断开的铁箍和可读角铁延续上一格的方向与相对位置并继续旋转；通过木纹、断口和铆钉让主要碎片可追踪。中心大部分为空，所有木面都已拆开，没有站立箱框，没有连接成平整托盘的底板。大碎片轮廓清楚，辅以少量木屑。

格 5，整图 (512,512)-(1024,1024)：同一批主要木板、弯曲铁条和角铁结束上升并向右下方落下，集中进入格内下半部。部分低处碎片接近 y=400 的原接地高度；较高碎片仍在下坠。两个底板断块保持彼此分离，并以不同角度落下。

格 6，整图 (1024,512)-(1536,1024)：碎片已全部落地，形成约位于 x=112..416、y=330..400 的低矮、分散残骸。长短不同的独立板条、两道铁箍的弯曲断段和角铁平放或低斜靠；两块底板仍然分开，属于普通残片。残骸之间留有透明间隙，整体重心略偏右，所有部件已停止悬浮。

六格应当像一个游戏 Sprite 破坏动作的六个关键姿态，破坏程度单向推进，部件的材质、粗细和身份连续。主要冲击方向始终从左向右。背景和所有空隙都是真实透明 Alpha。

### English（实际提交的正向部分）

Use the provided approved Three Kingdoms supply crate as the sole identity and material reference. Create one genuinely transparent RGBA 1536x1024 six-cell destruction animation sheet, arranged in three columns and two rows. Read stages 1, 2, and 3 left to right across the top row, then stages 4, 5, and 6 left to right across the bottom row. This is one continuous destruction of the same crate. All cells share one fixed camera angle, drawing scale, lighting, and original location.

For the intact crate, preserve the input's near-cubic form, slight three-quarter view, visible front and screen-right face, three top planks, two horizontal cool iron-gray bands, corner plates, rivets, and existing vertical split on the front. Preserve warm rough timber, cooler shadows, warm upper-left light, stepped contours, and clustered light and shadow blocks. Broken parts retain these materials and design features, with clearly readable thickness on the broken timber edges.

Each cell is 512x512 and uses local coordinates with (0,0) at its upper-left corner. The intact crate is approximately 240 pixels wide and 219 pixels tall, occupying x=136..376 and y=181..400, with a fixed ground-reference point at (256,400). Use exactly the same drawing scale and logical origin in all six cells. Do not recenter or enlarge later stages to fit their debris bounding boxes. Keep the entire crate and every fragment inside local x=80..432 and y=80..432 in every cell, leaving at least 80 pixels of genuinely transparent space along all four cell edges. Coordinates and cell lines must not be drawn.

Cell 1, whole-sheet bounds (0,0)-(512,512): the intact, stable original crate, faithful to the input, without new damage, placed at the specified scale.

Cell 2, whole-sheet bounds (512,0)-(1024,512): a horizontal impact traveling from screen left toward screen right has already caused an obvious break. The left portion of the visible front has burst open, with about one third of the front missing. Three or four substantial broken timber pieces have already detached and moved rightward and upward-right, with clear transparent gaps between the parts. The upper iron band has snapped open and sprung away, and the lower band has opened. The bottom panel has already split through its middle into two pieces; both pieces start lifting away from the ground and flipping upward. The crate must unmistakably read as already broken.

Cell 3, whole-sheet bounds (1024,0)-(1536,512): the assembled crate structure has disappeared. The front, right side, top, and both broken bottom-panel pieces have all separated into a small set of clear, substantial timber fragments, traveling mainly rightward and upward-right, with a few smaller fragments recoiling leftward. Both iron bands detach as broken bent strips, and corner plates leave their original positions. The two bottom-panel pieces are visibly airborne and rotating. A clear transparent gap opens in the center of the former crate.

Cell 4, whole-sheet bounds (0,512)-(512,1024): the same debris reaches its widest spread while remaining fully inside the 80-pixel cell safety margins. The main timber pieces, both broken bands, and readable corner plates continue from their previous directions and relative positions while rotating. Make the major fragments traceable through their grain, broken edges, and rivets. Most of the center is empty, every wooden face is disassembled, no crate frame remains standing, and the bottom pieces do not connect into a flat pallet. Keep the major fragments clear, with only a few small splinters.

Cell 5, whole-sheet bounds (512,512)-(1024,1024): the same major timber pieces, bent iron strips, and corner plates finish rising and fall downward-right into the lower half of the cell. Some lower pieces approach the original ground-reference height at y=400, while higher pieces are still falling. The two broken bottom-panel pieces remain separate and fall at different angles.

Cell 6, whole-sheet bounds (1024,512)-(1536,1024): all debris has landed in a low, scattered ruin approximately within x=112..416 and y=330..400. Independent plank strips of varied lengths, bent broken sections from both iron bands, and corner plates lie flat or lean low. The two bottom-panel pieces are still separated and are ordinary debris. Leave transparent gaps between the remains. The ruin is weighted slightly toward the right, and every piece has stopped floating.

The six cells should read as six key poses of one game-sprite destruction action, with damage advancing in one direction and with continuous material, thickness, and identity of the parts. The main impact direction is always left to right. The background and every empty gap must be genuine transparent alpha.

## 7. 负面提示词

### 中文（完整审阅版）

不要把这画成六种不同木箱或一个箱子内同时出现六个阶段。不要画六格以外的第七个物件、缩略插图、编号、文字、格线、外框或坐标。不要只加小穿孔、细裂纹或仍贴在表面的碎屑来代替格 2 的实际破裂。不要画慢慢开箱、盖子掀开、四壁铰接向内倒下或重力坍塌。格 3 起不要保留站立箱框、完整背板、完整底板、平整木托盘或由整排板条拼成的底座。不要把碎片重新拼回箱子，不要让主要铁件无故消失。不要放大后续格，不要改变机位，不要裁掉或跨格绘制碎片。不要画武器、枪尖、人物、内容物、火焰、火花、尘烟、地面、投影或地平线。不要画厚涂插画、照片写实、光滑 3D、萌系、随机碎像素噪点、平滑渐变、柔化光晕、白边、灰边、棋盘格、白底、灰底或绿幕底。

### English（实际提交的负面部分）

Do not depict six different crate designs or combine six stages inside one cell. Do not add a seventh object outside the six cells, inset thumbnails, numbers, text, cell lines, borders, or coordinates. Do not substitute a small puncture, hairline cracks, or still-attached chips for the actual break in cell 2. No slow opening, lifting lid, walls hinged inward, or gravity collapse. From cell 3 onward, no standing crate frame, intact back panel, intact bottom panel, flat wooden pallet, or base made from a joined row of planks. Do not rebuild the crate or make major iron fittings vanish without cause. No enlargement of later stages, camera changes, cropped debris, or fragments crossing cells. No weapon, spear tip, person, contents, flames, sparks, dust, smoke, ground, cast shadow, or horizon. No painterly illustration, photorealism, glossy 3D, cute style, random pixel noise, smooth gradients, soft glow, halo, white fringe, gray fringe, checkerboard, white background, gray background, or green-screen background.

## 8. 验收与报告

### 技术检查

- 文件可读、1536×1024、RGBA/PNG Color Type 6；记录 SHA-256、文件大小、耗时及服务端实际返回的 usage/cost。
- Alpha 存在有效透明区；四角与确认的空背景以 `alpha ≤ 2` 为透明容差，不因主体最大 Alpha 为 253/254 直接判失败。
- 对每格 `alpha > 8` 的有效像素量测包围盒：四边余量均至少 80px；没有跨格像素或连通域。
- 背景和部件间空隙是真实透明，无画入棋盘格；浅/深底预览无明显灰边、白边或彩色光晕。
- 数值格位是布局要求，不用独立裁切/缩放来掩盖生成阶段的格位失败。

### 美术检查

- 完整格仍是已验收的箱子，六格机位和绘制尺度一致。
- 格 2 一眼可读为已经炸开；不是只有旧裂纹、新小孔或零散小木屑。
- 格 3 起所有主要木面已经解体，底板断成两块且离开原位；没有站立箱框。
- 格 4 有明确的透明中心和向右飞散方向，非内爆塌陷，也非均匀四周自爆。
- 格 3–6 的主要木片、铁箍和角铁可追踪，部件不会突然重组或无故消失。
- 末格为低矮分散残骸，没有完整底板、木托盘、规则整排板条、悬空碎片。
- 缩小查看后破坏轮廓仍然清楚。格式/Alpha 通过与动作/美术通过分别报告，用户验收单独记录。

### 停止与后续边界

收到用户针对本方案的明确“发起生成”后，才按上述不变参数与双语词提交一次。失败、超时、结果不明、透明/边距/观感不对，都停止并报告；不自动改词、改尺寸、换参考或重试。

本方案不包含裁切、补边、对齐、Unity 导入、代码修改或视频生成。组图即使通过静态审阅，也要另行验证帧间连续性。若未来获准取帧，应使用统一 512×512 格位、同一倍率和同一锚点；不得按各格 Alpha 包围盒单独缩放铺满。正式完整箱继续以已验收原图为来源，模型重绘的格 1 仅作组图对照。
