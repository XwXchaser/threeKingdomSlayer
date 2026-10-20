---
id: kd_df5c050a-1d54-41aa-bf73-ef6a1b500264
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 可破坏补给基图探针 · 生成交接（2026-10-19）

> 状态（2026-10-19）：酒缸、木箱**两张基图已由用户目视验收通过**。基图生成阶段结束；破坏序列的规则见 §10，尚无任何破坏帧产出，也未导入 Unity。

## 0. 当前授权与停止边界

- 已批准内容：两张 1024×1024 基图探针，以及下文逐字英文 Prompt、参考职责和请求参数。
- 每张只提交一次。HTTP 失败、超时、结果不明、观感不对或透明失败，都停止并报告，不改词、不换底、不降档、不自动重试。
- 同名成功 PNG 已存在时停止，不覆盖。
- 基图探针（`Prop_WineJar_base_v1.png`、`Prop_Crate_base_v1.png`）已于 2026-10-19 由用户**目视验收通过**，验收结论只来自用户，不由 API 成功、文件落盘或 RGBA 有效推断。
- 破坏序列 `Break_1..6` 的产出规则见 §10；目前**尚未产出任何破坏帧**。
- 不把 API 成功、文件落盘或 RGBA 有效写成美术验收通过。

## 1. 先读文件

1. `plan/breakable-props-art-handoff-20261019.md`：原始需求与失败素材。
2. `design/art-style-guide.md`：画风权威；概念图和 1011 定风格，EnvironmentV2 不能反向定义新画风。
3. `skill/gpt-image-generation.md`：接口、multipart、透明路由与完成性规则。
4. `skill/workflows/image-asset-generation.md`：两级透明验收和交付规则。
5. `memory/user-preference.md`：`quality=max`、审批边界、绝对路径交付。

## 2. 画风与禁止继承

目标是高清像素化三国村落补给：阶梯状轮廓、成簇明暗色块、清楚受光面和暗部、暖土色配冷铁灰。边缘干净，缩小后剪影仍能看出“缸”或“箱”，并看得出能被打碎。

明确禁止继承：

- `C:/Users/Administrator/Pictures/gptGen/prop_wine_jar.png`
- `C:/Users/Administrator/Pictures/gptGen/prop_crate.png`
- `C:/Users/Administrator/Pictures/gptGen/prop_wine_jar_v2.png`
- `C:/Users/Administrator/Pictures/gptGen/prop_ui/prop_wine_jar_v2_pixel256.png`
- `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/Crates_Corrected.png`

这些只是反面参考。不得上传，不得作为编辑输入，也不得用降采样、限色或像素滤镜自动“修正”新结果。

## 3. 参考图与职责

两次请求使用相同三张参考，顺序固定：

1. `01_art_style.png`
   - 原样复制：`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`
   - 只提供三国战场、暖土冷铁、厚重材质和像素色簇。
   - 禁止复制人物、武器、军旗、城楼和整张构图。
2. `02_well_material.png`
   - 原样复制：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N3/Village/N3_W1_Well_v1.png`
   - 只提供粗木、麻绳、陶土/石块和冷铁箍的绘制语言。
   - 禁止复制井架、吊桶、石圈或对称结构。
3. `03_timber_material.png`
   - 原样复制：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/J1/J1_A05_CompleteWoodenCart_v1.png`
   - 只提供木板、暖色切面、钉眼和铁件边缘。
   - 禁止复制车轮、车辕、车厢或横向构图。

不上传柴垛、旧木箱、E0 残门、整张 EV 场景、带标注图或战斗界面概念图。参考图不裁切、不缩放、不清理 Alpha。

## 4. 两次请求

输出根目录：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/`

每次保存到独立子目录：

- `C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/wine_jar/`
- `C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/`

每次请求：

```text
POST https://api.muskapis.com/v1/images/edits
Content-Type: multipart/form-data
image[] order: 01_art_style.png, 02_well_material.png, 03_timber_material.png
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

Key 只从环境变量 `MUSK_API_KEY` 读取。不得写入文件、日志、知识库或回复，也不得打印完整请求头。

提交前解析实际 multipart，确认三图顺序、上述字段和英文 Prompt 均在请求体中。实际英文 Prompt 必须与本文对应段落逐字一致；中文只供审阅，不提交。

## 5. 酒缸基图

输出：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/wine_jar/Prop_WineJar_base_v1.png`

### 中文提示词

制作一张 1024×1024、真正 RGBA 透明的高清像素化三国村落道具图。画面只有一只完整、可独立放置的粗陶酒缸，居中，四周留出充足透明边界。

酒缸矮胖、口宽、腹鼓、底稳，轮廓略微不对称，厚重且明显可被戳击打碎。使用暖土陶色，以阶梯状轮廓和成簇明暗色块塑造体积；受光面暖，暗部偏冷。口部盖一块旧麻布，用粗麻绳系紧。腹部有磨损、少量小裂纹和克制的冷铁灰高光。

图1只提供三国战场的暖土冷铁关系和像素色簇，不复制人物、武器、军旗或建筑。图2只提供粗木、麻绳、陶土和冷铁件的绘制语言，不复制井架、吊桶或石圈。图3只提供木板和铁件边缘语言，不复制板车。

纯透明背景。不画地面、阴影、底座、天空、文字、标签、酒杯、酒滴或第二只缸。

禁止：厚涂插画、照片写实、光滑3D、萌系、低分辨率噪点、像素滤镜、随机碎像素、平滑渐变、柔化光晕、白边、灰边、棋盘格、白底、灰底。

### English prompt

Create one genuinely transparent RGBA prop image, 1024x1024, in a high-definition pixel-cluster Three Kingdoms village style. Show exactly one complete, independently placeable coarse earthen wine jar, centered with generous transparent margins.

Make the jar short, heavy, wide-mouthed, belly-rounded, stable, and slightly asymmetrical. It must look thick and visibly breakable by a spear thrust. Use warm clay colors, stepped contours, and clustered light and shadow blocks. Lit faces are warm; shadows are cooler. Cover the mouth with old hemp cloth tied by thick rope. Add wear, a few small cracks, and restrained cool iron-gray highlights on the belly.

Image 1 supplies only the warm-earth and cool-iron battlefield relationship and pixel clusters. Do not copy its people, weapons, flags, or buildings. Image 2 supplies only the drawing language of rough timber, rope, clay, and cool iron fittings. Do not copy its well frame, bucket, or stone ring. Image 3 supplies only timber and iron-edge language. Do not copy its cart.

Use a pure transparent background. Do not draw a floor, shadow, base, sky, text, label, cup, drip, or second jar.

Forbidden: painterly illustration, photorealism, glossy 3D, cute style, low-resolution noise, pixel filter, random pixel debris, smooth gradients, soft glow, halo, white fringe, gray fringe, checkerboard, white background, gray background.

## 6. 木箱基图

输出：

`C:/Users/Administrator/Pictures/gptGen/breakable_props_base_v1/crate/Prop_Crate_base_v1.png`

### 中文提示词

制作一张 1024×1024、真正 RGBA 透明的高清像素化三国军需道具图。画面只有一只完整、可独立放置的粗木军需箱，居中，四周留出充足透明边界。

木箱厚重、略旧、轮廓清楚，并明显可被戳击打碎。使用暖色粗木板、阶梯状边缘和成簇明暗色块；受光面暖，暗部偏冷。加两道水平冷铁灰铁箍、四角包铁、少量钉眼和一条清楚裂缝。采用轻微四分之三视角，让顶面、正面和一面侧面可读，但主体仍是单一箱子。

图1只提供三国战场的暖土冷铁关系和像素色簇，不复制人物、武器、军旗或建筑。图2只提供粗木、麻绳、陶土和冷铁件的绘制语言，不复制井架、吊桶或石圈。图3只提供木板、钉眼和铁件边缘语言，不复制板车。

纯透明背景。不画地面、阴影、底座、天空、文字、标签、箱堆或第二只箱。

禁止：现代木箱、塑料、厚涂插画、照片写实、光滑3D、萌系、低分辨率噪点、像素滤镜、随机碎像素、平滑渐变、柔化光晕、白边、灰边、棋盘格、白底、灰底。

### English prompt

Create one genuinely transparent RGBA prop image, 1024x1024, in a high-definition pixel-cluster Three Kingdoms military style. Show exactly one complete, independently placeable rough timber supply crate, centered with generous transparent margins.

Make the crate heavy, worn, clearly readable, and visibly breakable by a spear thrust. Use warm rough planks, stepped edges, and clustered light and shadow blocks. Lit faces are warm; shadows are cooler. Add two horizontal cool iron-gray bands, iron corner plates, a few nail holes, and one clear split. Use a slight three-quarter view so the top, front, and one side are readable, while keeping one single crate as the subject.

Image 1 supplies only the warm-earth and cool-iron battlefield relationship and pixel clusters. Do not copy its people, weapons, flags, or buildings. Image 2 supplies only the drawing language of rough timber, rope, clay, and cool iron fittings. Do not copy its well frame, bucket, or stone ring. Image 3 supplies only plank, nail, and iron-edge language. Do not copy its cart.

Use a pure transparent background. Do not draw a floor, shadow, base, sky, text, label, crate stack, or second crate.

Forbidden: modern crate, plastic, painterly illustration, photorealism, glossy 3D, cute style, low-resolution noise, pixel filter, random pixel debris, smooth gradients, soft glow, halo, white fringe, gray fringe, checkerboard, white background, gray background.

## 7. 必须保存的追溯文件

每个子目录保存：

- 最终 PNG。
- `prompt.zh.txt` 与 `prompt.en.txt`。
- 三张未改参考副本。
- 实际提交前的 `request.json` 或可解析 multipart 记录；不得包含 API Key。
- 原始 `response.json`；若响应含 Key、签名 URL 凭据或其他敏感字段，保存前移除。
- `status.json`：HTTP 状态、耗时、usage/cost（未返回就写未返回，不推断）、输出字节数、SHA-256、PNG Color Type、尺寸、Alpha 范围和四角 Alpha。

## 8. 机器验收

用 Pillow 或等价读取，不得只看预览器：

- PNG 签名正确，尺寸为 1024×1024，模式 RGBA，PNG Color Type 6。
- 四角 Alpha 为 0，并记录透明、半透明和主体 Alpha 分布。
- Alpha 最大值 253 或 254 不自动判失败；低 Alpha 主体、外部光晕和边缘污染才需要报告。
- 生成白底、深灰底、暖土色底三张预览，供目视查灰边、白边和光晕。
- 检查背景是否被画成棋盘格：背景行不能呈周期性明暗交替，灰度直方图不能只有两级。
- 若落到已知不执行透明的后端并返回 RGB，停止。不填白、不本地假抠图、不立即重发。

## 9. 对话交付

完成后在对话中实际附图两张原图，并给出可直接粘贴到 Windows 资源管理器的完整绝对路径。同时列出三张预览路径、SHA-256、PNG Color Type、Alpha 统计和剩余问题。

必须明确：生成完成不等于用户验收通过。用户未回复验收前，不进入破坏帧、不导入 Unity、不改任何项目资产。

---

## 10. 破坏序列（第二阶段 · 2026-10-19）

### 10.1 授权与状态

- 酒缸、木箱基图已由用户在对话中**目视验收通过**（2026-10-19）。
- 破坏序列：规则、逐字提示词与参数已写入本节，但**尚未产出任何帧**（`Break_2..6` 未生成，`Break_1` 仅为基图字节副本）。发起生成前仍需用户当次明确放行。
- 每帧只提交一次，不重试、不改词、不换参考、不降档。
- `Break_1` 直接复用已验收基图（字节相同），不重新生成，保证首帧与静态图零偏移。
- 不在本阶段导入 Unity、不改项目资产、不写破坏逻辑；部署与实现属后续阶段，需另行授权。
- 生成完成 ≠ 验收通过：每帧的观感验收仍由用户完成。

### 10.2 帧设计

酒缸：`1` 完整 → `2` 戳穿点+放射细裂 → `3` 沿裂纹分瓣张开、酒液短弧溅出、麻布松脱 → `4` 陶片外飞、露出空心中轴 → `5` 陶片开始下坠、液滴化 → `6` 贴地残片+麻布绳结。

木箱：`1` 完整 → `2` 正面戳穿+顺纹开裂、铁箍完好 → `3` 正面板炸开 3~4 块、上箍顶起 → `4` 两侧张开、铁箍弹飞、塌陷三分之一 → `5` 只剩底板与一角、铁件半空下坠 → `6` 板条+弯箍+铁角片+木屑的低矮残址。

两件共同约束：同画布、同机位、同比例、同接地位置、同配色与同像素色块密度；只有破坏程度推进；碎片不得触边。

### 10.3 参考与参数

```text
POST https://api.muskapis.com/v1/images/edits
Content-Type: multipart/form-data
image[] (only one): <prop>/Prop_*_base_v1.png   # 已验收基图，唯一参考
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

只上传基图的理由：序列必须锁定同一形体、机位、比例和配色；再叠加画风/材质参考会把构图、井架或板车元素带回来。Key 只从 `MUSK_API_KEY` 读取，不落盘、不回显。

### 10.4 输出路径

```text
C:/Users/Administrator/Pictures/gptGen/breakable_props_break_v1/wine_jar/Prop_WineJar_Break_1..6.png
C:/Users/Administrator/Pictures/gptGen/breakable_props_break_v1/crate/Prop_Crate_Break_1..6.png
```

### 10.5 提示词组装规则

实际提交的英文 Prompt = `EN 共享前缀 PREFIX` + 空行 + `EN <道具> Stage N` + 空行 + `EN 共享后缀 SUFFIX`，三段逐字拼装，不改写、不追加。中文段落只供审阅，不提交。

### 10.6 EN 共享前缀 PREFIX

```text
Edit the provided existing approved game prop image of a single coarse earthen wine jar. Produce one damage-progression frame of that same jar, not a new design and not a new object.

Keep the same 1024x1024 canvas, the same camera angle, the same orthographic feel, the same object scale, the same left-right and top-bottom placement, and the same ground-contact footprint as the input jar. The mouth height, the widest belly width and the rim shape must stay unchanged. Keep the identical warm clay palette, the identical clustered light and shadow blocks, the same warm upper-left key light with a cooler shadow side, the same stepped pixel-cluster contours, the same hemp cloth and thick rope on the mouth, the same wear, small cracks and restrained cool iron-gray highlights, and the same rendering density as the input.

Change only the damage state described below. Everything not described as broken stays identical to the input.
```

### ZH 共享前缀 PREFIX（酒缸）

```text
编辑所提供的、已验收的粗陶酒缸道具图，产出同一只缸的一张破坏进程帧，不是新设计，也不是新物件。

保持 1024×1024 画布、相同机位、相同正交感、相同物件比例、相同左右与上下位置、相同接地足迹。口部高度、腹部最宽处和口沿形状不得改变。保持完全相同的暖陶土配色、成簇明暗色块、左上方暖主光与偏冷暗部、阶梯状像素色块轮廓、口部麻布与粗麻绳、磨损、小裂纹、克制的冷铁灰高光，以及与输入相同的绘制密度。

只改变下面描述的破坏状态，未被描述的完好部分必须与输入一致。
```

### 10.7 EN 酒缸 Stage 2

```text
Damage stage 2 of 6: one compact spear thrust hole has just punched through the front of the belly, with fine radiating cracks spreading outward from it. Two or three small clay chips have just separated and are still very close to the jar surface. The mouth cloth and rope are still tied. The jar silhouette is still intact and still reads as one whole jar.
```

### 10.8 EN 酒缸 Stage 3

```text
Damage stage 3 of 6: the belly has split along the radiating cracks into six to eight large clay shards that are still around the original axis, opened outward by about one fifth of the jar width. A dark liquid bursts out of the largest gap in a short arc, with a few droplets. The hemp cloth has come loose and is lifting off the mouth. The jar footprint is still in the same place.
```

### 10.9 EN 酒缸 Stage 4

```text
Damage stage 4 of 6: the large clay shards are now flying apart outward beyond the original silhouette, the rope has come off, the liquid forms a short spurting arc, and the hollow inner axis of the jar is visible for the first time. The lowest shards are still close to the original ground contact.
```

### 10.10 EN 酒缸 Stage 5

```text
Damage stage 5 of 6: the shards are still moving outward and are now starting to fall, most of them turned to show their inner concave faces, the liquid has broken into small droplets, and several shards are already at the ground-contact level.
```

### 10.11 EN 酒缸 Stage 6

```text
Damage stage 6 of 6: only a low ground-resting ruin remains inside the original footprint, formed by eight to ten flat or tilted clay shards of different sizes, the crumpled hemp cloth, and a loose rope knot, with a damp stain implying the spill. No standing jar shape, nothing floating, and everything low at ground-contact level.
```

### 10.12 EN 木箱共享前缀 PREFIX

```text
Edit the provided existing approved game prop image of a single rough timber supply crate. Produce one damage-progression frame of that same crate, not a new design and not a new object.

Keep the same 1024x1024 canvas, the same slight three-quarter camera angle, the same orthographic feel, the same object scale, the same left-right and top-bottom placement, and the same ground-contact footprint as the input crate. The top face, the front face and the readable side face must keep the same proportions and the same plank layout. Keep the identical warm rough plank palette, the identical clustered light and shadow blocks, the same warm upper-left key light with a cooler shadow side, the same two horizontal cool iron-gray bands, the same iron corner plates, the same nail holes, the same existing split, and the same stepped pixel-cluster contours and rendering density as the input.

Change only the damage state described below. Everything not described as broken stays identical to the input.
```

### ZH 共享前缀 PREFIX（木箱）

```text
编辑所提供的、已验收的粗木军需箱道具图，产出同一只木箱的一张破坏进程帧，不是新设计，也不是新物件。

保持 1024×1024 画布、相同的轻微四分之三机位、相同正交感、相同物件比例、相同左右与上下位置、相同接地足迹。顶面、正面与可见侧面保持相同比例和相同木板排布。保持完全相同的暖色粗木板配色、成簇明暗色块、左上方暖主光与偏冷暗部、两道水平冷铁灰铁箍、四角包铁、钉眼、既有裂缝，以及相同的阶梯状像素色块轮廓与绘制密度。

只改变下面描述的破坏状态，未被描述的完好部分必须与输入一致。
```

### 10.13 EN 木箱 Stage 2

```text
Damage stage 2 of 6: one spear thrust hole has just punched through the center of the front panel, with a clear split spreading outward along the plank grain toward both iron bands. One or two splinters have just broken off and are still very close to the surface. Both iron bands are still fastened and the crate still reads as one solid crate.
```

### 10.14 EN 木箱 Stage 3

```text
Damage stage 3 of 6: the front panel has burst outward and broken into three or four boards that are opened outward by about one fifth of the crate width, the upper iron band has been pushed up and slightly lifted, and wood chips and splinters are flying out of the gap. The box shape is still readable.
```

### 10.15 EN 木箱 Stage 4

```text
Damage stage 4 of 6: the front and side boards are swinging outward, both iron bands are being thrown off the crate, the crate has lost about a third of its height, and boards, splinters and one corner plate are flying outward radially. The bottom of the crate is still at the original ground contact.
```

### 10.16 EN 木箱 Stage 5

```text
Damage stage 5 of 6: most boards have left the box, only the bottom panel and one corner still remain near the ground, the iron bands and corner plates are falling in mid-air, and wood chips are scattering outward.
```

### 10.17 EN 木箱 Stage 6

```text
Damage stage 6 of 6: only a low ground-resting ruin remains inside the original footprint, formed by scattered plank strips of different lengths, one bent iron band, one iron corner plate and a small heap of splinters and chips, all lying flat or leaning low. Nothing is standing, nothing is floating, and no crate shape is left.
```

### 10.18 EN 共享后缀 SUFFIX

```text
Use a pure transparent background and keep it genuinely transparent. Keep every flying fragment inside the canvas: no debris may touch or cross the image edge. Do not draw a floor, ground line, cast shadow, dust cloud, smoke, sparks, text, label, UI, extra prop, or a second jar or crate. Do not turn the image into painterly illustration, photorealism, glossy 3D, or cute style. Do not add smooth gradients, soft glow, halo, white fringe, gray fringe, checkerboard, white background, or gray background.
```

### ZH 共享后缀 SUFFIX

```text
使用纯透明背景，并保持真正透明。所有飞散碎片必须留在画布内，任何碎片都不得触碰或越出画面边缘。不画地面、地平线、投影、尘云、烟、火星、文字、标签、UI、额外道具，也不画第二只缸或第二只箱。不得变成厚涂插画、照片写实、光滑3D 或萌系。不得加入平滑渐变、柔化光晕、光晕环、白边、灰边、棋盘格、白底或灰底。
```

### 10.19 追溯文件

每帧保存：成品 PNG、`prompt.en.txt`、`prompt.zh.txt`（对应阶段中文）、一张基图参考副本、`request.json`（字段与顺序、图片 SHA-256，不含 Key）、`response.json`（去掉签名 URL 路径，只留 host）、`status.json`（HTTP 状态、耗时、usage/cost、字节数、SHA-256、Color Type、Alpha 统计、与基图的包围盒和接地线差值）。

### 10.20 机器验收与停止条件

- 通过项：PNG 签名、1024×1024、RGBA / Color Type 6、四角 Alpha 为 0、背景非画入棋盘格、画面 8px 边带内无 `alpha>8` 像素（防碎片被裁切）。
- **2026-10-19 修订（用户同意）**：四角与 8px 边带改用容差 `alpha ≤ 2` 视为透明。依据：已验收基图 `Prop_WineJar_base_v1.png` 自身边带 max alpha 即为 1，绝对 0 会误杀与基准同量级的输出。`Break_4` 左下角单像素 alpha=1 据此判定通过，不重发。
- 记录项：实心（`alpha>=200`）包围盒与基图的宽高差、最低实心行与基图接地线差值、Alpha 分布；不做逐帧 rescale 或逐帧锚点归一化，部署阶段统一处理。
- Alpha 最大值 253/254 不自动判失败。
- 任一帧出现非 200、非 PNG、Color Type ≠ 6、画入棋盘格、四角非 0 或碎片触边：立即停止并报告，不改词、不重试、不补生成，也不把已有帧说成已验收。
- `Break_1` 必须是基图字节副本，需用 SHA-256 比对确认。

### 10.21 状态记录（2026-10-19 终止）

- 按本节提交了酒缸 `Break_2..4`，三帧均 HTTP 200 / 1024×1024 / Color Type 6，机器验收在 `alpha ≤ 2` 容差下通过。
- **用户随后判定该版破坏序列方向错误，生成已终止。** 本节 10.2～10.18 的帧设计与提示词属于该废弃版本，不得直接续用。
- `Break_5` 的请求已发出但被打断：无返回、无产出，服务端是否计费未知，**不得自动重发**。
- 现存产出仅作废稿 / 反面参考：`breakable_props_break_v1/wine_jar/Prop_WineJar_Break_1..4.png`；木箱无任何破坏帧，`breakable_props_break_v1/crate/` 未创建。
- 重做前必须先由用户重新确定破坏表现方向（碎片尺度、动作节奏、是否保留液体溦出、是否改为程序化切分），再重写 Stage 段落并提交审批。
- 基图验收结论不受影响：两张基图仍为已验收状态。
