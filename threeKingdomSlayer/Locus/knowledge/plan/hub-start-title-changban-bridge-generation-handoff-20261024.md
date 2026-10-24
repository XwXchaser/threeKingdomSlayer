---
id: kd_a2b5364b-4be0-411c-b586-323b1dbec272
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# “开始关卡”艺术字与长坂桥关卡图生成交接（2026-10-24）

> 状态：生成前准备文档。本轮未调用图片／去背服务，未生成两件素材或参考裁切，未改 Unity、脚本、场景或既有图片。
> 用户最新需求：已有 UI 组图仍缺独立艺术字“开始关卡”，关卡入口需要具有长坂坡印象的具体地标——长坂桥；参考露米娅的“火山”是具体火山形象这一表达方式。
> 本文供用户在别处发起两件单独素材生成。此前“所有字交给 TMP”“B3 继续沿用城门小景”的统一处理，在这两处由本轮需求修正。

## 1. 接手者先读：两件产物与范围

| 任务 | 产物 | 使用位置 | 与既有拆件关系 |
| --- | --- | --- | --- |
| T：艺术字 | `hub_start_title_zh_v2.png`，独立透明四字字标 | 叠在开始按钮红色底板上，等比显示 | 新增固定字标；不把字烧回 A1 底板，不替换其他动态文本 |
| C：长坂桥 | `hub_stage_changban_bridge_v2.png`，独立透明关卡景物图标 | 右下选关入口，独立名字带上方 | 替代当前 B3 城门小景的角色；保留原 B3 文件，不覆盖删除 |

每件一个候选、一次图像请求，**分别生成，不放在一张组图里**。两件都输出真正透明 PNG。T 是固定标题，C 是新地标构图；不重做主界面、不重生 G1/G2 两张组图、不改变已确认的 UI 相对位置。

金额、任务内容、进度、关卡名称与“天赋”等普通／动态文字继续使用 TMP。关卡名仍由现有名字带＋TMP 承担，C 内不写“长坂坡”或“长坂桥”。

木桥的具体结构是本游戏的美术设定；本文不声称它是古桥形制的考据复原，不依赖上一轮回复中未经本会话工具核实的外部网页链接。

## 2. 授权与交付边界

- 当前会话只制作交接文档；用户在其他会话发起生成。
- 接手者使用实际可用图像工具时，应先展示完整中英文提示词、负面约束、参考职责、模型／尺寸／质量／透明度／数量。若用户已经明确批准本文两件方案并要求生成，复用该授权，不重复询问。
- 用户明确要求“按本文生成两件素材”时，可执行 T、C 各一次，并完成本地透明检查、无损裁切补边、对照预览和记录。这些属于同一素材交付，不逐步要求审批。
- 若用户只授权其中一件，只做对应一件；若指定本地提取艺术字，先走本地路线，不能自行改成收费生成。
- 新增变体、额外请求、重试、改关键参考、改画风或实质改构图，需要按当前用户授权处理，不从“准备文档”推定为已授权。
- 不导入 `Assets/`、不创建 `.meta`、不操作场景／Prefab／存档／Editor 状态，不自动运行游戏、不提交 Git。

## 3. 当前文件证据与参考图职责

### 3.1 已存在的源文件

下列尺寸、模式与 SHA-256 已在本轮从文件读取；原图与两张组图已在前一轮实际读图。

| 编号 | 文件绝对路径 | 实测 | SHA-256 |
| --- | --- | --- | --- |
| R0 | `H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png` | 1024×1536，RGB | `3935de2210d34a7a2b1dae2b4bfc8052952ec890c78ecf48bb052083545dea9d` |
| R1 | `H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_sprite_v2/generated/hub_ui_g1_raw_v2.png` | 2048×2048，RGBA | `145499634e3adf0d86b3bad5417fd2ae5a0cebde56e5f9048d655e44b0381bcd` |
| R2 | `H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_sprite_v2/generated/hub_ui_g2_raw_v2.png` | 2048×2048，RGBA | `e8f4330f620a7224e6c8e4befcb5a63ed288841a4394b5f56d35b8ac045ef8a2` |

组图记录：`_art_process/hub_ui_sprite_v2/delivery_manifest.md`。该记录写明 G1/G2 已生成，但尚未切成11件，造型细节仍待目视验收；不能从“有组图”推定独立底板或图标已存在。

### 3.2 T 的输入：只用原按钮局部

在未来生成会话，从 R0 无重采样裁出：

- ROI：`[180,825,837,1055)`，左上原点、右下不含，657×230。
- 拟参考文件：`references/T_start_button_reference_crop.png`。
- 保留原图按钮、文字、原有背景，作为识别字形的来源参考；不加框、箭头、编号或说明。
- 这是**参考裁切**，不是成品，不声称已有透明字标。生成只提取／还原字，不能把红按钮或场景一起输出。
- T 只上传这一张裁切。无需 R1 空底板作为第二输入，也不加战争概念图，避免把字标重新解释成新的按钮设计。

### 3.3 C 的输入：R2 的材质参考

C 默认只上传 R2 一张。职责仅为：校准现有 UI 的绘制颗粒、厚重体块、暖土／冷色关系、边缘清晰度与小尺寸完成度。

- R2 下方的旧 B3 城门景色属于**将被替换的主体**；不复制其城门、城墙、远山、天空、道路透视、外部形状或金黑边框。
- 铜钱／徽章／四宫格／天赋图案也不复制进 C。
- 新图主体由本文件明确的木桥构图决定。不要加入 R0 全屏图，避免再次生成整片城门场景。
- 不把露米娅整张截图作为第二输入；“具体地标图标”的语义已经写入提示词，无需诱导火山、卡通色彩和其他按钮。
- 如后续用户另提供喜欢的桥形参考，可单独说明其结构职责，再确认是否改变本轮方案；当前不虚构已经存在桥参考图。

把本地路径写在提示词里不等于服务收到图片。接手者须实际上传对应参考图，并核对请求记录中的图片数量与顺序。

## 4. T：艺术字的美术规格

### 4.1 固定四字，忠实原图

- 简体中文，必须准确为 **开始关卡**，左到右四字、一行，不能用繁体、近似字或多加标点。
- 维持 R0 的粗壮笔势、四字相对大小、间距、视觉重心与收笔方向，不重新写成细长书法、草书或宫廷金字。
- 象牙白／暖米白为主，局部很浅的暖金受光；深褐色薄轮廓，右下贴近字身的短暗影，保证放在朱红底上清楚。
- 允许源图已有的轻微笔画不平与锋芒，不人为添加大量破洞、飞溅、武器、火焰或装饰。
- 输出作为一件整体字标，不拆四字，不制作字体，不重做按钮。

### 4.2 画布与透明

- 设计目标：1536×512（3:1）透明 PNG，文字整体居中，视觉宽度约占画布82%—90%；保留完整笔锋和暗影。
- 字的整体宽高比沿用参考字形，不强行贴满高度，也不把字拉宽以适配画布。
- 透明区域包含字外、字间与字内正常空隙；暗描边和短阴影保留为字的一部分。
- 不残留红色底板、金属角饰、道路、矩形深底、棋盘格或“擦字”斑块。
- 实际 API 若不支持1536×512，允许使用其支持的横向透明画布（例如确实支持的1536×1024或2048×1024），文字保持约3:1的横向带状占位，额外上下留透明。收到后无损裁出带补边的字标。**API画幅变化不能导致字形拉伸或重排**；实际参数必须在提交前说明。

### 4.3 本地提取分支

用户若指定优先提取，可从 R0 的按钮局部建立精细蒙版，保留字形、描边与短阴影。红底会污染米白字边，不能只保留亮像素或全删深色。通过多底色和按钮叠合检查后交付。

如果本地方法达不到清楚字形与干净边缘，应说明具体问题，不能宣称逐像素无损还原未知Alpha，也不能擅自增加远程生成。本文件的默认发起内容是下面的独立字标生成；替代路线不要求两套都做。

## 5. C：长坂桥关卡图的美术规格

### 5.1 地点识别先于复杂背景

核心形象是**完整的粗木板桥横跨河水**。一眼能读成木桥与河流的关系，再由两岸、旗布与材质建立三国战场身份。

- 桥为唯一主角，横向宽度约占整件可见图形的65%—75%。
- 视角为稍俯视三分之四，桥从左下近岸斜向右上远岸；有清楚桥面、厚木侧梁、支撑木桩／桥墩，完整两端落在岸上。
- 木桥低矮、结实、跨度清楚，木板和连接关系简洁。可以边缘磨损、局部松裂，但主桥完整可通行，不做断桥和人物战斗故事画。
- 两端只带小块不规则土岸，下方和侧边露出一小段青灰河水；少量灰绿芦苇和一面小残红旗布作为次要点缀。
- 不加屋顶、桥楼、石拱、现代护栏、长城式女墙、吊桥高塔或夸张吊索，不用这些复杂轮廓抢走木桥本体。
- 这是具体地标型入口图标，不画城门、城池、连续远山、整片天空或通向远处的巨大场景，不把桥缩成远景小物件。
- 不出现张飞等武将、士兵、马匹、人影、巨型武器或战斗特效。

### 5.2 材质与轮廓

- 暖褐木梁、黄赭土岸、低饱和青灰河水、灰绿芦苇、少量朱红旗布。
- 顶面受光，梁侧和桥下较暗，用大块色簇、明确受光面和有控制的局部纹理塑造厚重感；不套像素滤镜、不铺随机碎点。
- 桥、岸、水共同形成紧凑横向的自然剪影，不套照片式矩形，不沿用旧B3的不规则金属相框，不做厚黑描边或包围全部景物的金边。
- 天然物件可以有深色轮廓与局部高光，轮廓清楚即可；不增加悬浮岛底座、切面地台或世界地图式地块。
- 河水是图标的实体内容，水面保持可读的青灰色，不把整段河水抠成透明。桥外的空隙与景物外轮廓之外透明；不保留铺满画布的“水背景”。

### 5.3 画布与组合

- 设计目标：1024×1024透明PNG，整件可见图形约为横向1.3:1至1.6:1，中心摆放，四周透明余量至少约6%。不为填满正方形拉高桥或扩出大背景。
- 可提高同比分辨率，前提是在实际请求前明确，保持同样构图。
- 不加“长坂坡”“长坂桥”、星级、数字、名字带、金属外框或按钮底。
- 正式组合仍使用 A4 名字带＋TMP“长坂坡”，C 在其上方；未来对齐时保留桥两端与河流识别。
- 小尺寸验证重点：约200—300px宽下不看文字也能看出“木桥跨水”。不承诺只凭一座木桥就能让所有人唯一识别长坂坡，地名由入口文字共同表达。

## 6. 请求参数与执行次数

| 项目 | T 艺术字 | C 长坂桥 |
| --- | --- | --- |
| 输入图 | R0按钮局部裁切，1张 | R2透明组图，1张，仅校准材质 |
| 请求类型 | 图像编辑，忠实还原字标并透明隔离 | 参考图引导的新关卡地标图标 |
| 原生设计画幅 | 1536×512；实际接口不支持时按§4.2横向透明留白 | 1024×1024透明方形画布 |
| 数量 | n=1，一次 | n=1，一次 |
| 质量 | 用户偏好`quality=max`，按真实工具字段核对 | 同左 |
| 保真 | 支持时`input_fidelity=high`，仍需核对字形 | 支持时可用相应参考保真参数，不能因此复制城门构图 |
| 背景 | 原生透明、PNG | 原生透明、PNG |
| 工具能力 | 读取实际schema后选用支持图像输入与透明输出的模型，本文不预设历史模型仍可用 | 同左 |
| 记录 | 真实模型、尺寸、参考图哈希、实际提交字段、次数、输出 | 同左 |

若实际接口有 `background=transparent` 与 `output_format=png` 字段，须真实提交；提示词不能代替参数。若字段名或枚举不同，用工具实际支持的值，不能伪造`max`或静默降档。不支持透明时说明能力缺口，不能删除字段后把RGB当成透明结果交付。

两件各一次请求，不自动输出多套变体。完整中英文版本为同一语义方案，不因此各请求一次。工具没有独立负面参数时，把对应负面约束附在主提示词末尾。

## 7. T 完整中文提示词

```text
请以提供的按钮局部图片为唯一字形参考，忠实还原其中的简体中文艺术字“开始关卡”，制作一件独立的透明字标。只要四个字及其原有描边和短阴影，不要按钮、金属框或场景。这是已有字形的提取与还原，不是另设计一种书法。

准确文字为“开始关卡”，四个简体字从左到右一行，字形结构必须正确，无增字、缺字、错字、异体字或标点。保留参考中的粗壮有力笔势、四字相对大小、间距、重心、收笔与整体横向比例，确保缩小后仍然清楚。不要改成细长草书，不增加武器、火焰、飞溅或徽章。

字面为象牙白／暖米白，局部极浅暖金受光；字外有贴近笔画的深褐薄轮廓和右下很短的暗影，服务它叠在朱红按钮上时的可读性。描边和暗影属于字标，需要保留；它们不能变成整块矩形黑底。

设计画幅为1536×512的横向透明画布；如果实际请求使用更高的横向画布，请仍把四字作为约3:1的完整横向带状字标居中放置，上下多余区域保持透明。不要拉宽、压扁、裁掉笔锋或为了填满高度改变字形。文字视觉宽度约占画布82%—90%，保留完整边缘与阴影。

输出真正RGBA透明PNG。字外、四字之间及各字正常内部空隙透明。彻底去掉源图红色按钮、金属角饰、道路、地面、草石以及它们造成的边缘污染。不画白底、棋盘格、色板、说明文字或水印。只交付一件准确、完整、边缘干净的“开始关卡”独立艺术字。
```

### T 中文负面约束

```text
错字、缺字、增字、繁体、异体字、乱码、标点、细长草书、重新设计字形、四字分成四张、红色按钮、金属框、武器、火焰、徽章、大片飞溅、道路、草石、人物、矩形黑底、红色残边、过粗描边、长投影、模糊、字形拉伸、笔锋裁断、白底、棋盘格、色板、标注、水印。
```

## 8. T Full English Prompt

```text
Use the supplied button-detail image as the sole reference for the lettering. Faithfully reconstruct its exact Simplified Chinese artistic title “开始关卡” as one isolated transparent wordmark. Include only the four characters, their existing close outline, and short shadow. Exclude the button, metal frame, and environment. This is faithful extraction and reconstruction of the existing lettering, not a new calligraphy design.

The exact text is “开始关卡”: four correctly formed Simplified Chinese characters, left to right on one line. No added, missing, substituted, traditional, or malformed characters and no punctuation. Preserve the reference's substantial forceful brush strokes, relative character sizes, spacing, visual balance, stroke endings, and overall horizontal proportions. Keep the title readable when reduced. Do not turn it into thin cursive script or add weapons, fire, splashes, or emblems.

Use ivory / warm off-white character faces with extremely restrained pale warm-gold lighting. Retain a close, thin dark-brown contour and a very short lower-right dark shadow that makes the wordmark readable over a vermilion button. The contour and short shadow are part of the lettering and must remain; they must not form a solid rectangular black backing.

The intended design canvas is 1536 × 512, horizontal and transparent. If the actual request uses a taller horizontal canvas, keep the full title as an approximately 3:1 horizontal band centered on it, with the extra space above and below fully transparent. Do not stretch, squash, clip stroke tips, or change character proportions to fill the height. The lettering should occupy about 82–90% of the canvas width, with complete contours and shadow preserved.

Output a genuine RGBA transparent PNG. Areas outside, between, and within the normal openings of the characters are transparent. Remove all source red button material, metal corners, road, earth, grass, stones, and their color contamination along the lettering edges. No white background, checkerboard, color swatch, explanatory label, or watermark. Deliver one exact, complete, clean-edged “开始关卡” wordmark.
```

### T English Negative Constraints

```text
Wrong characters, missing characters, added characters, traditional Chinese substitutions, alternate character forms, gibberish, punctuation, thin cursive lettering, redesigned glyphs, four separate character images, red button, metal frame, weapon, fire, emblem, large splashes, road, grass, stones, people, rectangular black backing, residual red fringes, excessively thick outline, long cast shadow, blur, stretched lettering, clipped stroke tips, white background, checkerboard, color swatches, annotations, watermark.
```

## 9. C 完整中文提示词

```text
为三国战场动作游戏制作一件“长坂坡——长坂桥”关卡入口图标，真正透明PNG。它需要像关卡选择中的具体地标一样一眼可读：主体是一座完整、厚重、略显破旧的木板桥横跨河流，带极少量两岸与环境点缀。

提供的参考图片是一张既有UI图形组图。只参考它的材质完成度、厚重块面、暖土与冷色对比、像素化色簇和清楚边缘。必须把里面的城门景色主体替换为下面的木桥构图。不要复制城门、城墙、山峰、整片天空、远处道路、旧小景外形、金黑相框，也不要复制铜钱、卷轴徽章、四宫格或天赋图案。

稍俯视的三分之四角度，桥从画面左下近岸斜向右上远岸。桥身横向占整件可见图形宽度的65%—75%，是唯一主角。清楚表现宽厚木板桥面、粗重木侧梁、支撑木桩／桥墩和两端落在岸上的关系。桥低矮结实，跨度明确；可以有表面磨损与局部木板裂痕，但桥面完整可通行。不是断桥，不讲述人物战斗故事。

两端各接一小块不规则黄赭土岸，桥下及侧边露出一小段低饱和青灰河水。只加克制的灰绿芦苇和一面小残红旗布，不堆建筑与道具。桥面受光、梁侧偏暗，以清楚的大块明暗和像素色簇建立体积；木梁暖褐，土岸暖黄，河水青灰，旗布朱红。

桥、少量岸地与河水共同构成紧凑横向的自然景物轮廓，整体可见宽高比约1.3:1至1.6:1。将它居中放在1024×1024画布里，四周至少约6%透明余量。不要为填满正方形把桥拉高或扩成大场景。河水本身是图标的可见内容，要有青灰实体颜色，不能把水全抠成透明；也不能把河水铺成全画布背景。景物轮廓外与合理空隙真正透明。

不加文字、地名、星星、数字、名字带、按钮底板、金属相框、厚黑描边或包围景物的金边。自然物件可以有深色轮廓和局部高光。不做悬浮岛底座、剖切地台、矩形风景照片。不要人物、士兵、马匹、武器或战斗特效。不要屋顶廊桥、石拱桥、现代栏杆、桥楼、高塔或吊索。

这是一件小尺寸入口用的具体木桥地标图标。缩小到200—300像素宽时，仍应清楚读出“完整木桥跨过河流”，不依赖文字解释。画面应与既有三国游戏UI共享厚重、粗粝但清楚的像素化绘制语言，不要软萌玩具感、照片写实、随机噪点或马赛克滤镜。输出一件完整孤立、边缘干净的RGBA透明素材。
```

### C 中文负面约束

```text
城门、城墙、宫殿、山峰主体、整片天空、大场景、远景小桥、矩形照片、地图碎片、悬浮岛、剖切底座、金属相框、包边金线、厚黑外框、名字带、任何文字、长坂坡字样、长坂桥字样、星级、数字、铜钱、徽章、四宫格、天赋节点、人物、张飞、士兵、马匹、武器、爆炸、斩击、断桥、坍塌桥、屋顶廊桥、石拱桥、现代护栏、桥楼、吊桥高塔、夸张吊索、纯俯视地图、桥身过小、河水透明消失、河水铺满背景、软萌Q版、塑料玩具、照片写实、随机噪点、马赛克滤镜、白底、棋盘格、标注、水印。
```

## 10. C Full English Prompt

```text
Create one stage-entry landmark icon for a Three Kingdoms battlefield action game: Changban Bridge, representing Changbanpo. Output a genuinely transparent PNG. It must read as a specific landmark at small size: one complete, substantial, weathered wooden plank bridge spanning a river, with only small pieces of the two banks and restrained environmental accents.

The supplied reference is an existing UI-asset sheet. Use it only for material finish, substantial forms, warm earth against cooler colors, deliberate pixel-art color clusters, and clean edges. Replace its gate-scene subject entirely with the wooden-bridge composition below. Do not copy its city gate, walls, mountain peaks, full sky, receding road, old scenery silhouette, or gold-and-black frame. Do not copy its coin, scroll badge, four-square key, or talent symbol.

Use a slightly elevated three-quarter view. The bridge runs diagonally from the near bank at lower left toward the far bank at upper right. Its horizontal span occupies about 65–75% of the complete visible artwork width and is the sole focal subject. Clearly show the thick wooden plank deck, heavy side beams, timber support posts / piers, and both ends landing on the banks. The bridge is low, sturdy, and structurally readable. Allow surface wear and a few plank cracks, but keep the deck complete and passable. This is not a broken bridge or a character battle illustration.

Attach only a small irregular patch of yellow-ochre earth at each end. Show a short stretch of muted blue-gray river beneath and beside the bridge. Add only restrained gray-green reeds and one small tattered vermilion banner cloth, without piles of architecture or props. Light the top of the deck and keep beam sides darker, using clear broad light-and-dark masses and deliberate pixel-art clusters for volume. Use warm brown timber, warm ochre banks, blue-gray water, and a small vermilion accent.

The bridge, small bank patches, and water together form a compact horizontal natural silhouette, with visible-art proportions approximately 1.3:1 to 1.6:1. Center it on a 1024 × 1024 canvas with at least about 6% transparent margin on every side. Do not stretch the bridge vertically or expand the composition into a large scene to fill the square. The river is visible artwork with a solid readable blue-gray color; do not key the water out as transparency. Do not extend the river into a full-canvas background. The area outside the scenery silhouette and appropriate gaps are genuinely transparent.

No text, place name, stars, numerals, name band, button plate, metal picture frame, thick black perimeter frame, or gold border encircling the artwork. Natural objects may retain dark contours and local highlights. No floating-island pedestal, cutaway terrain base, or rectangular landscape photograph. No people, soldiers, horses, weapons, or combat effects. No roofed corridor bridge, stone arch bridge, modern railings, gatehouse on the bridge, tall towers, or suspension cables.

This is a specific wooden-bridge landmark for a small stage-entry button. At 200–300 pixels wide, it must still clearly read as a complete wooden bridge spanning water without relying on text. Match the existing game's weighty, rugged but readable pixel-art rendering, avoiding cute toy proportions, photorealism, random noise, or a mosaic filter. Deliver one complete isolated RGBA transparent asset with clean edges.
```

### C English Negative Constraints

```text
City gate, city walls, palace, mountain-peak focal subject, full sky, large scene, tiny distant bridge, rectangular photograph, map fragment, floating island, cutaway pedestal, metal picture frame, encircling gold trim, thick black frame, name band, any lettering, Changbanpo label, Changban Bridge label, stars, numerals, coin, badge, four-square key, talent nodes, people, Zhang Fei, soldiers, horses, weapons, explosion, slash effect, broken bridge, collapsed bridge, roofed corridor bridge, stone arch bridge, modern railings, bridge gatehouse, tall suspension towers, exaggerated suspension cables, straight top-down map view, undersized bridge, transparent missing river, river filling the entire background, cute chibi styling, plastic toy, photorealism, random noise, mosaic filter, white background, checkerboard, annotations, watermark.
```

## 11. 验证与审阅：两件分别判断

### T 艺术字

- 逐字核对“开／始／关／卡”，无错字、残笔、额外符号；四字一行，字内空隙和暗描边合理。
- 与 R0 按钮文字并排，观察笔势、粗细、间距、重心、颜色；高保真参数不代替视觉核对。
- 实际解码核查Alpha、外部透明和字间透明，白／深灰／朱红三底预览，重点查红边、黑底残块、灰白污染和过粗描边。
- 提供一张叠回源图按钮的局部审阅图；如另选R1红底作附加预览，说明底板来自尚待验收的组图，不把它当已定稿。
- 在约320—480px字标宽度下看四字是否易读、边缘是否清楚；这是本地预览规格，不是Unity实际按钮尺寸回读。
- 不为了“透明干净”删除深褐轮廓和短阴影，不自动锐化／加粗／改字。

### C 长坂桥

- 第一眼看见木桥主体，桥面、侧梁、桥墩、两岸、河水关系清楚；桥没有变成远景配角。
- 无城门、无大背景、无框、无字、无人物；桥完整可通行。
- 河水是可见青灰色内容，外围透明，不是整张透明蓝底或矩形水景。
- 实际解码核查Alpha，白／深灰／暖土三底查看外缘、芦苇与旗布；不把黑暗部一并抠除。
- 在约200、250、300px宽分别预览，检查桥与水仍清楚，细枝／木纹没有变成噪声。
- 与R2并排判断材质亲缘关系，但不要为追求旧B3形状而恢复城门或金黑相框。

透明检查通过不代表美术验收通过。只有RGBA文件、真实外部透明和实际可读图形同时成立，才是候选素材；HTTP成功、PNG后缀、棋盘格显示都不能代替这些检查。

如需无损裁切，依据实际Alpha边界保留足够透明padding、笔锋／旗布／阴影，不用统一高阈值截掉半透明边缘；保留原始输出。不要拉伸、不擅自全图去噪，不在失败结果上临时填补未获认可的字形或桥构图。

## 12. 未来产物目录与记录

以下目录和文件**本轮尚未创建**；由实际生成会话建立。

根目录：

`H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_title_bridge_v1/`

建议结构：

```text
hub_ui_title_bridge_v1/
├─ references/
│  ├─ T_start_button_reference_crop.png   来自R0的657×230无重采样裁切
│  ├─ C_ui_material_reference.png         R2原样副本
│  └─ reference_manifest.json             来源路径、裁切框、尺寸与哈希
├─ requests/                             两件实际中英文提示词与参数，无凭据
├─ generated/
│  ├─ T_start_title_raw.png               原始输出，不覆盖
│  └─ C_changban_bridge_raw.png           原始输出，不覆盖
├─ sprites/
│  ├─ hub_start_title_zh_v2.png           最终候选字标
│  └─ hub_stage_changban_bridge_v2.png    最终候选桥图
├─ review/                               多底色、缩小、源图对照与叠合审阅
└─ delivery_manifest.md                  实际模型/参数/请求次数/结果/验收状态
```

`sprites/`只放检查通过的最终候选；与原始生成、参考裁切、审阅图分开保存。缺任何一件就列明缺件，不宣称两件都已完成。

交付时直接展示最终图及关键预览，给用户可粘贴到资源管理器的Windows绝对路径。记录真实模型、实际画幅、原生透明字段、参考图哈希、是否后处理、用户验收状态；不记录密钥或令牌。

## 13. 关联文档与新旧规则

- `Locus/knowledge/plan/hub-ui-sprite-breakdown-handoff-20261024.md`：11件历史拆分方案。A1底板继续无字；本轮在它上方新增T。B3城门小景由C承担长坂坡地标表达，旧产物保留追溯。
- `_art_process/hub_ui_sprite_v2/delivery_manifest.md`：两张组图的真实生成状态，尚未切片，本轮没有替它们补做切片。
- `Locus/knowledge/plan/hub-ui-reference-generation-handoff-20261024.md`：整图初始方案；源图比例有偏差，但用户已确认UI相对位置。
- `Locus/knowledge/design/art-style-guide.md`：画风依据，尤其图标的单焦点、轮廓与小尺寸可读性。

本轮完成标准：本文保存并回读，拆件交接顶部指向本文且说明固定艺术字例外与桥图替代关系；未调用生成、未创建素材产物、未修改Unity。新会话获相应生成授权后按两件分别执行并交图验收。
