---
id: kd_236c94c0-c488-45ea-b4b2-ed33ebde999f
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 落地页剩余美术素材：四组九件生成交接（2026-10-24）

> ⚠️ **本文的四组九件方案已大部分被 V9 拆件取代。当前权威入口：`Locus/knowledge/plan/hub-ui-v9-assembly-handoff-20261025.md`（用户已签收，8 件已接入 Battle 场景）。**
> 实际落地对照：开始无字底板 → `hub_v9_start_brush`；任务底板 → `hub_v9_task_strip`；任务徽章 → `hub_v9_task_target`；铜钱墨条/资源栏底 → `hub_v9_resource_bar`；铜钱图标 → `hub_v9_coin`；四宫格置灰键 → `hub_v9_talent_frame` + `hub_v9_key_symbol_1..4`（改成了「方形底 + 彩色符号」）；天赋横向卡底 → **未按本文做**，改成了方形底框 + 经脉武将图标（`hub_v9_talent_frame` / `hub_v9_talent_icon`）。本文其余分析与提示词仍可作为素材工艺参考。

> 状态：生成准备文档。用户要求根据上一轮方案准备交接，在别处发起生成。本轮未调用图片服务，未生成参考裁切、组图或独立素材，未修改 Unity 工程。
> 本次目标：回到原始效果图的朱红破笔、硬朗角饰、墨色承托和克制金属质感，制作尚缺的四组九件素材。
> 部署背景见 `Locus/knowledge/plan/hub-ui-deployment-handoff-20261024.md`。本文件是本次剩余素材生产入口，不重做已经生成的艺术字、木桥图或可算验收的铜钱图标。

## 1. 最新范围与保留成果

用户已明确：四宫格占位键、天赋 UI、开始关卡底板、任务栏相关仍需要生成。上一轮分析将它们拆为七件主要组件，加铜钱墨条和关卡名字带，共九件。

| 组 | 内容 | 件数 | 首轮请求 |
| --- | --- | --- | --- |
| S | 开始关卡无字底板 | 1 | 单件图，一次 |
| T | 横向天赋卡底、独立节点图案 | 2 | 双件透明组图，一次 |
| I | 任务底板、卷轴徽章、空进度槽、铜钱墨条、关卡名字带 | 5 | 五件透明组图，一次 |
| M | 完整置灰四宫格占位键 | 1 | 单件图，一次 |

合计四次请求、四张原始输出，切为九件独立 PNG。首次每组一个候选，不自动多版本抽卡。四张图的生产排布不改变游戏中的相对位置。

### 1.1 已有成果不重复生产

- 铜钱图标：用户说“可算验收”；本次不重画，最终选用哪个已有文件属于后续接入核对，不在本轮猜测。
- “开始关卡”艺术字：已生成并导入，S 底板必须无字，之后叠加该艺术字。
- 长坂桥关卡图：已生成并导入，继续替代旧城门图。本次只补它下方的无字名字带。
- 普通文字与动态信息：仍由 TMP 承担，不烘入素材。
- 任务数据：继续延后，本次只做占位视觉资源，不生产任务配置、奖励规则或交互逻辑。
- 本次不增加头像、等级、经验、体力、星级、tab、商城、红点、锁头、按下态或发光态。

### 1.2 已有素材文件证据

以下为本轮文件读取结果，不代表新的用户验收结论：

| 项 | 实际路径 | 实测 | SHA-256 |
| --- | --- | --- | --- |
| 原始效果图 R | `_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png` | 1024×1536，RGB | `3935de2210d34a7a2b1dae2b4bfc8052952ec890c78ecf48bb052083545dea9d` |
| 已有艺术字 | `_art_process/hub_ui_title_bridge_v1/sprites/hub_start_title_zh_v2.png` | 1376×515，RGBA | `153ca008bff783d4642db881fc60f11160d0ea3b9fdaf2fe6d796a906740edaf` |
| 已有木桥图 | `_art_process/hub_ui_title_bridge_v1/sprites/hub_stage_changban_bridge_v2.png` | 1014×609，RGBA | `24631e5f596972c98d31fc136c2c477a2a6092bdd7d017ee0e398e80d3ecb668` |

路径均相对项目根目录：`H:/Project/threeKingdomSlayer/threeKingdomSlayer/`。

## 2. 为什么本轮重新参考原图局部

旧组图 `_art_process/hub_ui_sprite_v2/generated/hub_ui_g1_raw_v2.png`、`hub_ui_g2_raw_v2.png` 已有输出，但尚未完成独立切片与用户验收。它们不能直接视为可部署素材。

上一轮读图对比观察到：

- G1 开始底板偏圆润雕花红牌，原图则是朱红破笔和硬朗金属角饰。
- G1 任务底板偏完整金属长框，原图主要由低纹理墨色长条承托。
- G1 墨条两端变成重复尖刺，削弱自然破笔形状。
- G2 天赋节点更接近强高光珠宝，原图的连线与成长结构应更突出。

因此，新一轮以 **R 的对应局部裁切**作为直接输入；旧 G1/G2 只作内部问题对照，不上传为本轮设计源。保留旧文件，不覆盖、不删除，也不为“已有组图”强行接受偏离参考的造型。

本次是在同一设计上补全、去文字、解耦图层。不同物件各自保留原来的轮廓，不能把所有 UI 统一重绘成同一种厚雕花相框。

## 3. 参考图准备：未来会话执行，本轮尚未裁切

原图 R 的绝对路径：

`H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png`

坐标为原始1024×1536画布，左上原点，右下不含，`[x0,y0,x1,y1)`。这些是宽松参考框，含场景与文字，不是Alpha蒙版。

| 参考文件名 | ROI | 尺寸 | 输入职责 |
| --- | --- | --- | --- |
| `S_start_reference.png` | [180,825,837,1055) | 657×230 | S唯一参考：朱红底、角饰、破笔和留字空间 |
| `T_talent_reference.png` | [600,1270,1008,1473) | 408×203 | T唯一参考：卡底、节点、图文比例 |
| `I_task_reference.png` | [20,1133,650,1275) | 630×142 | I输入1：任务底、徽章、空槽 |
| `I_currency_reference.png` | [766,19,1013,105) | 247×86 | I输入2：铜钱墨条；不生产图标或数值 |
| `I_nameband_reference.png` | [696,1180,1014,1258) | 318×78 | I输入3：名字带；去掉文字、上沿景色与场景 |
| `M_menu_reference.png` | [25,1291,199,1471) | 174×180 | M唯一参考：切角外框与置灰四块 |

准备时用原始像素无重采样裁切；不画红框、格线、标签，不增加样式。记录来源与哈希，明确命名为参考图，不能将带场景的裁切放入最终 `sprites/`。

S、T、M各上传一张对应参考；I按任务、铜钱、名字带次序上传三张。不要上传整张主菜单、露米娅截图、战争概念图或旧组图，以免重新生成整个界面或改画风。

## 4. 九件独立素材与形状要求

统一以 `_v3` 命名，避免覆盖 v2。以下是未来产物清单，当前尚未生成。

| ID | 拟文件名 | 保留 | 清除／补全 |
| --- | --- | --- | --- |
| S1 | `hub_start_plate_v3.png` | 朱红破笔、硬朗切角、暗铁旧铜角饰、贴近物体的暗轮廓 | 清除汉字及其阴影，补全红底；去地面和接地投影 |
| T1 | `hub_talent_plate_v3.png` | 宽约为高两倍的横向黑卡、旧铜细框、切角和材质 | 清除节点、光晕、文字及阴影，补全卡面；不变成竖卡 |
| T2 | `hub_talent_nodes_v3.png` | 原图节点关系、金色连线、少量局部亮面 | 清除卡底和文字；节点间透明；不加宝石切面、节点或分支 |
| I1 | `hub_task_plate_v3.png` | 横向墨条、自然破边、克制细收边、低纹理文本区 | 清除徽章、奖励、文字、进度槽；补完整底，不留圆缺口 |
| I2 | `hub_task_badge_v3.png` | 圆金属环、深色圆底和卷轴整体 | 清除长条底和场景；卷轴只留简洁非文字标记 |
| I3 | `hub_task_progress_track_v3.png` | 细长暗灰空槽、轻微边缘区分 | 清除0/1等文字、外任务底和场景；不加填充 |
| I4 | `hub_coin_bar_v3.png` | 短墨条、自然笔刷两端、完整数值承托区 | 清除铜钱和数字，补全被遮黑底；不加重复金属锯齿 |
| I5 | `hub_stage_nameband_v3.png` | 关卡入口下方的短墨色名字带 | 清除名字、景色及地面；不生成城门或木桥 |
| M1 | `hub_menu_disabled_v3.png` | 切角近方形外框、深灰底、四个灰块整体 | 清除场景和接地投影；无字、红点、锁头、新功能 |

### 4.1 共同美术控制

- 冷暗金属、旧铜、墨黑为共同结构语言；朱红集中于S，金色节点集中于T2。
- 正视UI图形，整体不带新的倾斜透视。原有金属厚度和细倒角保留，不变成3D展示物。
- 像素化由清楚块面、阶梯边缘和局部纹理组织；不增加细碎噪点、密集布纹或全图马赛克。
- 不新增龙纹、卷草、宝石、宫廷装饰；源图本来有的简单角饰可保留。
- 深色底板必须不透明，不能因“透明输出”把黑色承托区抠掉。
- 不生成任何文字、数字、假汉字、说明、标注或水印；I2卷轴上的非文字图形从简。
- 金属／笔刷之间的光向、阴影重量与锐度一致；普通卡片不抢开始按钮。

## 5. 输出画布与组图安全区

以下是设计目标，实际提交前必须核对工具 schema。不得将不支持的尺寸或枚举当成已经可用的参数。

| 组 | 目标画布 | 输出件数 | 备注 |
| --- | --- | --- | --- |
| S | 1536×1024 | 1 | 长按钮居中，上下允许透明，不为填高压缩／拉伸 |
| T | 2048×2048 | 2 | 上卡底、下节点，隔离生成 |
| I | 2048×2048 | 5 | 长条分行、徽章独占区域 |
| M | 1024×1024 | 1 | 方形置灰键居中，四周透明 |

安全区用左上原点、右下不含；完整部件必须在其内，保持自身宽高比。安全区是最大范围，不是要求部件铺满的尺寸。

| 格位 | 对象 | 安全区 |
| --- | --- | --- |
| S-1 | S1开始底板 | [96,256,1440,768) |
| T-1 | T1天赋卡底 | [128,160,1920,1120) |
| T-2 | T2天赋节点 | [640,1280,1408,1920) |
| I-1 | I1任务底板 | [96,96,1952,480) |
| I-2 | I3空进度槽 | [96,608,1312,736) |
| I-3 | I2卷轴徽章 | [1440,576,1952,1088) |
| I-4 | I4铜钱墨条 | [96,864,1312,1216) |
| I-5 | I5名字带 | [96,1376,1312,1696) |
| M-1 | M1四宫格 | [96,96,928,928) |

所有外边与隔离带真正透明。不画格线／标签，不跨区飞溅或放地面阴影。T2局部微光可半透明，不能蔓延到卡底安全区。

若实际工具不支持目标尺寸，应提交可用尺寸及同比换算后的安全区说明，保持件数、分组和形状；不得默默返回不同版式、拆成额外付费请求或拉伸成目标尺寸。

## 6. 生成参数、参考与次数

- 默认四次独立图像编辑请求，S／T／I／M各一次，`n=1`。完整中英文是同一方案的两种语言，不各提交一遍。
- 读取实际可用模型和参数schema，不沿用旧服务名就推定可用。本文件不承诺旧供应商或历史模型仍可访问。
- 质量沿用用户偏好`quality=max`；如工具无该值，说明最高可用档位，不伪造参数或静默降档。
- 编辑保真如支持则用`input_fidelity=high`；它不保证轮廓与原图逐像素一致。
- 原生透明PNG。如实际接口有`background=transparent`、`output_format=png`，必须真实提交，提示词不能代替字段。
- 不支持透明时说明能力缺口；不能省略透明字段重发RGB，或将烘焙棋盘格称为透明。
- 用户在别处明确批准本文并要求生成时，按该授权执行，不重复申请已获批准的步骤。当前“准备文档”不等于本轮发起生成。
- 生成前按用户偏好展示实际中英文提示词、负面约束、参考职责、真实模型／尺寸／透明度／数量；已有完整明确授权可复用。
- 用户若只授权某组，只执行该组；后续重试、额外变体和实质改参考按授权处理，不自动连续付费重试。
- 用户授权“生成并切片交付”时，本地透明检查、无损切片、补透明边、审阅与记录均在该素材任务内，无需逐小步重复审批。
- 不导入Unity、不修改场景／脚本／Prefab／存档、不创建`.meta`、不切换Editor状态、不运行游戏、不提交Git。

## 7. S 中文正向提示词

```text
请以提供的原始游戏按钮局部截图为唯一设计参考，还原一件“开始关卡”按钮的无字底板。忠实保留参考的朱红破笔、宽横轮廓、硬朗切角、暗铁旧铜折边和左右金属角饰。这是原设计去文字与透明隔离，不是新设计雕花牌匾。

清除全部汉字、文字阴影和文字遮挡痕迹，补全连续的红色底面。中央留出完整、低干扰的艺术字承托区。朱红色通过受控笔触和大块明暗形成质感，外围保留原图向右延伸的少量破笔、飞白和锐利边缘。不要光滑塑料渐变，不加龙纹、卷草、宝石、圆润浮雕或新徽章。

保留原有金属厚度、折面和紧贴按钮的暗轮廓，去掉投在地面上的宽大阴影。彻底清除道路、泥土、石头、草和场景背景。底板红面和金属暗面保持不透明，只有物件轮廓外与真实孔隙透明，不留下字形透明洞。

输出1536×1024真正RGBA透明PNG。完整按钮放在安全区[96,256,1440,768)，左上原点、右下不含。按参考自身宽高比等比居中，不为了填满画布高度拉高、压扁或裁掉角饰。保留全部笔刷边缘。只要一件无字底板，无文字、数字、标注、格线、水印、白底或棋盘格。
```

### S 中文负面约束

```text
文字、汉字残笔、文字阴影残留、字形透明洞、数字、龙纹、卷草、宝石、圆润雕花框、光滑塑料、宫廷牌匾、新徽章、背景、道路、泥土、草石、宽大接地投影、3D展示台、随机噪点、拉伸、裁断、格线、标签、水印、白底、棋盘格、伪透明。
```

## 8. S English Prompt

```text
Use the supplied original game-button detail as the sole design reference. Reconstruct one text-free Start Stage button backing, faithfully retaining the vermilion brushwork, wide horizontal silhouette, angular cut corners, dark iron and aged bronze folded edging, and metal end ornaments. This is text removal and transparent isolation of the existing design, not a newly designed carved plaque.

Remove all Chinese lettering, lettering shadows, and residual letter shapes, reconstructing continuous red material underneath. Preserve a complete, visually calm central area for the existing separate title artwork. Create red material with controlled brush strokes and broad light-and-dark masses. Retain the source's restrained rightward broken brush edges, dry-brush marks, and sharp contours. No smooth plastic gradient, dragons, scrollwork, gemstones, rounded relief carvings, or new emblems.

Preserve the original metal thickness, folded planes, and close dark contour. Remove the broad shadow cast onto the ground and all road, earth, stones, grass, and environment. The red surface and dark metal remain opaque. Transparency belongs outside the object and in genuine openings, not in letter-shaped holes.

Output one genuine RGBA transparent PNG at 1536 × 1024. Place the complete button within safe region [96,256,1440,768), using a top-left origin and exclusive bottom-right corner. Center it proportionally at its source aspect ratio. Do not stretch vertically, squash, or clip ornaments to fill the canvas. Preserve every brush edge. Only one blank backing: no text, numerals, annotations, cell borders, watermark, white background, or checkerboard.
```

### S English Negative Constraints

```text
Text, residual Chinese strokes, residual lettering shadows, letter-shaped transparent holes, numerals, dragons, scrollwork, gemstones, rounded carved frame, smooth plastic, ornate imperial plaque, new emblem, environment, road, earth, grass, stones, broad ground-cast shadow, 3D display pedestal, random noise, stretching, clipping, cell borders, labels, watermark, white background, checkerboard, fake transparency.
```

## 9. T 中文正向提示词

```text
请以提供的原始天赋入口局部截图为唯一参考，把同一设计还原为两件彼此分离的透明UI素材：完整横向卡底，以及独立金色分叉节点图案。保持参考中的硬朗切角、旧铜细框、墨黑卡面、厚重块面和克制亮度，不重做整个主菜单。

输出2048×2048真正RGBA透明PNG，两件分别位于下列安全区，左上原点、右下不含。不要画出格线或标签。

第一件，安全区[128,160,1920,1120)：完整横向天赋卡底。宽约为高两倍，保留参考左右端形状、切角、金属层次和黑色底面。移除天赋节点、光晕、“天赋”文字及其阴影，补全被遮卡面。中央和右侧保持低纹理，留给以后独立图标与文字。不能变成竖卡、圆润雕花框或新造牌匾。

第二件，安全区[640,1280,1408,1920)：单独的金色分叉节点图案，保持参考节点数量、连线拓扑、相对大小与位置。节点和连线清楚、厚实，暖金线条配少量局部亮面，微光贴近节点。不要新增宝石切面、分支或强烈光球。完全移除卡底、卡框和文字，节点与连线间的空隙真实透明，不能残留黑色矩形。

两件各自保持宽高比，完整留在安全区，不交叠、不跨区发光。金属暗部和卡面黑色保持不透明。去掉所有地面与场景投影。材质与原截图一致，不额外加细碎噪点、密集织纹、龙纹、珠宝或卷草。无文字、数字、标注、水印、实色背景或棋盘格。只交付两件完整、可独立切片的透明部件。
```

### T 中文负面约束

```text
文字、数字、假汉字、整张界面、地面、场景、竖卡、圆润雕花、龙纹、卷草、宝石切面、珠宝吊坠、强烈光球、漫天粒子、新节点、新分支、改变连线、黑色矩形残底、底板挖空、交叠、跨格光晕、拉伸、裁断、噪点、白底、棋盘格、格线、标签、水印。
```

## 10. T English Prompt

```text
Use the supplied original talent-entry detail as the sole reference. Reconstruct its existing design as two separate transparent UI assets: a complete horizontal card backing and an isolated golden branching-node symbol. Preserve the source angular corners, restrained aged bronze edging, ink-black surface, substantial forms, and controlled brightness. Do not recreate the full menu.

Output one genuine RGBA transparent PNG at 2048 × 2048. Place the two complete assets in these safe regions, using a top-left origin and exclusive bottom-right corner. Do not draw cell borders or labels.

Asset one, region [128,160,1920,1120): the complete horizontal talent-card backing, approximately twice as wide as tall. Retain the source end shapes, cut corners, metal layers, and black surface. Remove the node symbol, glow, Chinese talent label, and lettering shadow, reconstructing the covered card surface. Keep the center and right area low-texture for later separate graphics and typography. Do not turn it into a vertical card, rounded carved frame, or newly invented plaque.

Asset two, region [640,1280,1408,1920): the isolated golden branching-node symbol. Preserve the source node count, connection topology, relative sizes, and positions. Use substantial readable nodes and warm-gold connections with restrained local highlights. Keep any tiny glow close to the nodes. No added gemstone facets, branches, or intense light orbs. Remove the entire card surface, frame, and lettering. Gaps between nodes and connections are genuinely transparent, without a residual black rectangle.

Preserve each asset's aspect ratio and complete silhouette inside its own region. No overlap or glow crossing between regions. Dark metal and the black card surface remain opaque. Remove the ground and scene-cast shadows. Match the source materials without extra fine noise, dense fabric textures, dragons, jewelry, or scrollwork. No text, numerals, annotations, watermark, solid background, or checkerboard. Deliver only two complete, independently sliceable transparent assets.
```

### T English Negative Constraints

```text
Text, numerals, fake Chinese lettering, full interface, ground, scene content, vertical card, rounded carvings, dragons, scrollwork, gemstone facets, jewelry pendant, intense light orbs, drifting particles, added nodes, added branches, changed connections, residual black rectangle, hollowed-out card surface, overlap, cross-cell glow, stretching, clipping, noise, white background, checkerboard, cell borders, labels, watermark.
```

## 11. I 中文正向提示词

```text
使用提供的三张原始UI局部截图，忠实还原五件独立透明素材。输入1是任务条，负责任务底板、圆形卷轴徽章和空进度槽；输入2是右上铜钱显示，只负责其墨色底条；输入3是关卡名字带，只负责墨色名字带。不要复制铜钱图标、文字、数字或景色。

这是原图分层、去字与补全，不是设计一套新的厚金属面板。保留墨色笔刷主导的承托、自然破边、克制细收边和低纹理文本区，避免旧式雕花长框与重复金属锯齿尖刺。

输出2048×2048真正RGBA透明PNG，五件放入以下安全区；左上原点，右下不含。安全区不画框、不编号。各件保留自身比例，不强行铺满格位。

1. 安全区[96,96,1952,480)：横向任务空白底板。保留原图长墨条形状和右端自然破笔。去掉左侧圆徽章、右侧铜钱、标题、0/1和内嵌进度槽，补出被遮盖的完整墨色底。左侧不能留圆形缺口。中央安静、低纹理，不加连续厚金属框、雕花或新标记。

2. 安全区[96,608,1312,736)：独立的细长暗灰空进度槽，轻微边缘区分。去掉0/1、任务长底、标题和场景。内部保持不透明暗灰，无彩色填充、无文字。

3. 安全区[1440,576,1952,1088)：任务条左侧的圆形卷轴徽章，金属环、深色圆底和卷轴作为一件完整组合。卷轴剪影清楚，小尺寸仍能辨识；纸面只留简洁非文字标记，不画假汉字或密集纸纹。去掉长条任务底和地面。

4. 安全区[96,864,1312,1216)：右上铜钱显示的短墨色承托条。去掉铜钱与所有数字，补全被遮盖底面，保留其自然笔刷形状和完整数值承托区。不要天空、图标、金属锯齿或实心雕花框。

5. 安全区[96,1376,1312,1696)：关卡入口下方的短墨色名字带。去掉地名、景色、地面与上沿建筑。保留它自身的笔刷轮廓，不要复制第四件当作同一条。它中央承托文字的区域保持完整，不生成木桥或城门。

五件完整分离，格间和外围透明，无场景、接地投影、标签、数字、格线或水印。结构性的黑色墨底、徽章深色圆底和进度槽内部都必须保留不透明。只让物件轮廓外和真实孔隙透明，不留文字阴影、图标幽灵或字形透明洞。边缘有自然笔刷细节但不过度散落，不以随机噪点伪造像素画风。
```

### I 中文负面约束

```text
文字、数字、假汉字、字影残留、图标幽灵、字形透明洞、铜钱图标、关卡景色、城门、木桥、背景、地面、天空、厚金属长框、雕花、龙纹、重复锯齿尖刺、密集织纹、卷轴假字、新功能、彩色进度填充、圆形缺口、素材粘连、拉伸、裁断、跨格飞溅、白底、棋盘格、伪透明、格线、标签、水印。
```

## 12. I English Prompt

```text
Use the three supplied original UI detail images to faithfully reconstruct five isolated transparent assets. Input 1 is the task strip and defines the task backing, circular scroll badge, and empty progress track. Input 2 is the upper-right currency display and defines only its black backing. Input 3 defines only the stage-name brush band. Do not reproduce coin icons, lettering, numerals, or scenery.

This is layer separation, text removal, and reconstruction of the source, not a new set of heavy metal panels. Preserve ink-brush information backings, natural broken edges, restrained thin edging, and low-texture text areas. Avoid carved metal long frames and repeated serrated metal spikes.

Output one genuine RGBA transparent PNG at 2048 × 2048. Fit the five complete assets proportionally inside the following safe regions, with a top-left origin and exclusive bottom-right corner. Do not draw or number the regions. Do not stretch objects to fill them.

1. Region [96,96,1952,480): the blank horizontal task-strip backing. Retain its long ink silhouette and natural broken brush end on the right. Remove the circular badge, reward coin, title, 0/1, and inset progress track. Reconstruct complete ink material behind all removed parts, without a circular notch on the left. Keep the center calm and low-texture. No continuous heavy metal frame, carved decoration, or new symbol.

2. Region [96,608,1312,736): the isolated thin, empty dark-gray progress track, with restrained edge definition. Remove 0/1, the outer task backing, title, and environment. Keep its interior opaque dark gray, without colored fill or text.

3. Region [1440,576,1952,1088): the circular scroll badge at the left of the task strip, retaining its metal ring, dark circular backing, and scroll as one combined asset. Make the scroll silhouette clear at small size. Use only a simple non-lettering mark on the paper, not fake Chinese characters or dense paper texture. Remove the long task backing and ground.

4. Region [96,864,1312,1216): the short black ink backing from the upper-right currency display. Remove the coin and every numeral, reconstructing the covered surface. Preserve its natural brush silhouette and complete calm numeric area. No sky, icons, serrated metal spikes, or solid carved frame.

5. Region [96,1376,1312,1696): the short black stage-name brush band. Remove the place name, scenery, ground, and any building at its upper edge. Preserve its own source silhouette rather than duplicating asset four. Keep a complete central text area. Do not generate a bridge or city gate.

All five assets are complete and isolated, with transparent gutters and outside canvas. No scene, ground-cast shadow, labels, numerals, cell borders, or watermark. Structural black ink, the badge's dark circular backing, and the progress-track interior remain opaque. Only the outside contours and real openings are transparent. No lettering shadows, ghost icons, or letter-shaped holes. Retain controlled natural brush detail without excessive scattered particles or random noise imitating pixel art.
```

### I English Negative Constraints

```text
Text, numerals, fake Chinese lettering, residual lettering shadows, ghost icons, letter-shaped holes, coin icon, stage scenery, city gate, bridge, environment, ground, sky, heavy metal long frame, carvings, dragons, repeated serrated spikes, dense fabric texture, fake script on the scroll, new feature, colored progress fill, circular notch, merged assets, stretching, clipping, cross-cell splashes, white background, checkerboard, fake transparency, cell borders, labels, watermark.
```

## 13. M 中文正向提示词

```text
以提供的原始四宫格按钮局部为唯一参考，忠实还原一件完整的置灰菜单占位键。保持源图近方形切角外框、深灰金属底板和四个灰色方块的形状、间距与位置。外框、底板、四个方块作为一件整体，不拆成五颗按钮，不重新设计图标。

材质为暗铁灰，只有少量克制的边缘高光，整体低饱和、安静、清楚，维持未开放占位的状态。不要提高亮度做成激活态，不加文字、锁头、红点、徽章或新符号，不加雕花、宝石和密集噪点。

输出1024×1024真正RGBA透明PNG，完整按钮等比居中放在安全区[96,96,928,928)，左上原点、右下不含，保留全部切角和边缘厚度。清除道路、泥土、草石与宽大地面投影，保留物体自身贴近边缘的暗轮廓。黑灰底板保持不透明，外围透明。正视UI图形，不做3D展示台或倾斜透视。无标签、数字、格线、水印、白底或棋盘格。
```

### M 中文负面约束

```text
文字、数字、锁头、红点、徽章、新符号、激活亮色、发光、雕花、宝石、拆散四个方块、增加方块、改变图标、地面、草石、宽大投影、3D展示台、倾斜透视、拉伸、裁断、随机噪点、白底、棋盘格、格线、标签、水印。
```

## 14. M English Prompt

```text
Use the supplied original four-square button detail as the sole reference. Faithfully reconstruct one complete disabled menu placeholder key. Preserve its nearly square cut-corner frame, dark-gray metal backing, and four gray squares with the source shapes, spacing, and positions. The frame, backing, and four squares form one combined asset. Do not separate them into multiple buttons or redesign the icon.

Use dark iron gray with only restrained edge highlights. Keep it desaturated, subdued, and clearly readable as an unavailable placeholder. Do not brighten it into an active state or add text, lock, notification dot, badge, or new symbol. No carved decoration, gemstones, or dense noise.

Output one genuine RGBA transparent PNG at 1024 × 1024. Center the complete key proportionally inside safe region [96,96,928,928), using a top-left origin and exclusive bottom-right corner. Preserve every cut corner and the frame thickness. Remove the road, earth, grass, stones, and broad ground shadow, while retaining the close dark object contour. The black-gray backing remains opaque and the outside is transparent. Use a front-facing UI view without a 3D display pedestal or new perspective tilt. No labels, numerals, cell borders, watermark, white background, or checkerboard.
```

### M English Negative Constraints

```text
Text, numerals, lock, notification dot, badge, new symbol, active bright colors, glow, carvings, gemstones, separated squares, extra squares, redesigned icon, ground, grass, stones, broad cast shadow, 3D display pedestal, perspective tilt, stretching, clipping, random noise, white background, checkerboard, cell borders, labels, watermark.
```

四组负面约束若无独立API字段，分别附加到对应正向提示词，不虚构工具参数。

## 15. 收图与切片验收

### 15.1 真实透明与件数

1. 实际解码PNG，核实尺寸、RGBA/Alpha、外部透明与主体不透明；文件扩展名、HTTP成功、棋盘格显示均不能代替检查。
2. S=1、T=2、I=5、M=1，共九件。按对象对应关系清点，不能只按连通域数量判定，小笔触与微光可能是同件的一部分。
3. 检查隔离带无粘连、外轮廓无裁切，黑底没有被挖空，文字／图标没有残影。
4. 白、深灰、暖土三底预览；S额外检查朱红底边缘，T2检查光晕与黑色矩形残底。
5. 保留真实半透明边缘，不以统一高Alpha阈值截断破笔或微光，不强制要求Alpha最大值恰好255。

### 15.2 必须回到原图比较

- S：金属角饰硬朗、朱红破笔方向与原图对应，无圆润雕花和长地面影。把已有艺术字叠上审阅，不只看空底板。
- T：卡底横向、切角和图文留白合理；节点数量／连线未变，亮度克制，不珠宝化。
- I：墨色主导、正文低纹理；任务底没有圆缺口；徽章是卷轴；进度槽为空；两根墨条分别保留原形，不变成规则锯齿。
- M：切角方框和四灰块小尺寸清楚，状态置灰，无新增功能符号。

透明机器检查通过不等于造型或用户验收通过。源图并排审阅发现明显形状漂移时，应记录具体失败件；不能因为“有PNG”就将它标为可部署。

### 15.3 切片规则

- 先检查实际返回对象，不盲切计划坐标。对象独立、完整但位置轻微偏移时，可根据实际Alpha边界切片并记录，不为凑格位自动付费重排。
- 互相粘连、缺件、角饰被切掉或对象身份错误时标为失败，不能裁掉问题边缘掩盖。
- 每件保留约8—16px透明补边并覆盖全部微弱边缘；不放大、不拉伸、不“增强细节”。
- 原始组图保留不覆盖，记录每件实际框、尺寸、padding、哈希、来源和检查状态。

### 15.4 组合与缩小审阅

制作本地审阅拼图，不操作Unity：

- 新S底板＋已有艺术字；保留四字宽高比，观察文字承托与角饰是否抢焦点。
- T卡底＋T节点＋临时“天赋”字，检查左图右字结构与主次。
- I任务底＋徽章＋空槽＋占位文案＋已有铜钱示意，检查文本空间；临时文字不写入PNG部件或游戏数据。
- I名字带＋已有木桥图＋临时关卡名，检查组合比例。
- M独立显示，与其余入口并排看是否保持最低视觉权重。

在接近源图各件大小和更小一档下观察可读性，不能仅凭高分辨率放大图验收。正式屏幕适配属于后续UI任务，不把原2:3图整张拉伸至9:16或9:20。

## 16. 未来目录、交付与接入边界

以下目录与图像**本轮尚未创建**，由实际生成会话建立：

`H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_remaining_v3/`

```text
hub_ui_remaining_v3/
├─ references/                 六张原图局部裁切，含来源框与哈希记录
├─ requests/                   S/T/I/M实际中英文提示词与无凭据参数
├─ generated/                  四张原始输出及实际响应记录
├─ sprites/                    九件检查合格的独立透明PNG候选
├─ review/                     多底色、原图对比、组合与缩小审阅
├─ slice_manifest.json         实际切片框、尺寸、padding、哈希、状态
└─ delivery_manifest.md        实际请求次数、参数、缺件、验收反馈
```

交付时直接展示最终候选与关键对照，提供Windows绝对路径，区分`sprites/`最终候选与`review/`审阅图；不把参考裁切或旧组图充作合格件。源图、旧v2产物、艺术字和桥图全部保留。

后续如获授权接入，须再处理脚本序列化字段，避免`OnEnable`把Image写回旧图。不能只改Image.sprite；不能删除隐藏的`StartStageButton/Text`及被对话脚本引用的BossPortrait节点。具体操作仍以部署交接与实际对象回读为准，本文件不执行接线。

九宫格border必须在新图上确认后设定，不能沿用旧40/16等值。图标和复杂角饰初用等比显示，不能把横卡由竖卡压扁，也不能把天赋图案拉宽。

## 17. 关联文档与完成标准

- `Locus/knowledge/plan/hub-ui-deployment-handoff-20261024.md`：最新部署背景与用户缺件结论；其中保留旧开始底板是过渡状态，不是取消本次生产。
- `Locus/knowledge/plan/hub-ui-sprite-breakdown-handoff-20261024.md`：原11件方案与历史组图；本轮范围缩为四组九件，铜钱图标和关卡景色不重做。
- `Locus/knowledge/plan/hub-start-title-changban-bridge-generation-handoff-20261024.md`：已有艺术字和桥图相关记录，本轮复用产物。
- `Locus/knowledge/design/art-style-guide.md`：共同画风规范。

本轮完成标准：本文保存并回读，部署交接与原拆件交接顶部可找到新入口；四组中英文提示词、九件清单与格位数量一致；未调用图片服务，未创建素材产物，未修改Unity。本文完成不代表图片生成或部署已经完成。
