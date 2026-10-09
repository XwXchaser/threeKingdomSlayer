---
id: kd_d53ef6da-d092-49ce-a73f-735114c10e77
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# N1→E0 城墙素材：生成提示词与验收工作流

## 1. 当前状态与本轮交付边界

- 状态：**暂缓，保留历史提示词；尚未调用图片生成接口，尚无本轮新图片。** 用户已调整顺序为：先以场景母图推导N1→E0→E1→J1整体参考图，E0构图验收后再拆素材。当前待办入口为 `plan/scroll-scene-current-todolist.md`，不得继续按本文单件城墙批次自动生成。
- 本文交付素材清单、完整中英文提示词、负面约束、参考图分工、参数和验收流程，不代表图片生成或 Unity 部署完成。
- 用户已亲自试摆并确认当前城墙位置能够被观测，作为本轮制作的可行性前提。旧交接中“暂不追求 N1 城墙可见”的结论不再阻止本轮独立城墙素材制作；但不重新启用旧城墙天空叠图。
- 用户确认：视觉行进距离不必严格等同于实际距离，但**行进时间保持一致**。本轮不改代码、路线参数、相机或任何场景。
- 下文六件/三批是方向调整前的候选方案，不再作为当前优先顺序或必做配额。恢复单件制作时须根据已验收E0构图重新确认清单、提示词和参数；本文不是生成授权。

## 2. 当前试摆依据与比例

目标对象的完整路径：

`Assets/Experiments/CurvedScroll/YJunctionSample.unity/Y Junction - select for preview/Opening Roadside Props/N1_E0WallPreview_Distant_Left_01`

最近一次工具读取来自 **disk YAML**：Y 场景当时未加载，因此不包含用户尚未保存的 Editor 改动。读到的已保存 Transform 为：

```text
localPosition = (-0.32, 0, 40)
localScale = (3.03, 3.03, 3.03)
sortingOrder = 0
scenery index = 80
```

现有试摆图片：

`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_distant_ruined_wall_module_02_v1.png`

已检查：742×384、RGBA，Alpha bbox 使用右/下排他坐标为 `(28,29,713,355)`，有效主体约 685×326px；PPU=100。按当前缩放计算的 authored 可见包络约为 **20.76×9.88 世界单位**，不是经过 Shader/相机投影后的屏幕尺寸。

新母版以这版宽高关系与可读轮廓为参照，但不照抄旧图片画风，不直接沿用 Scale=3.03。更换图片后按实际 Alpha 高度换算初始缩放：

```text
初始 scale = 目标 authored 高度 × PPU / 新图片实际 Alpha 主体高度
```

之后以实际 Game 画面验收。不能以 Bounds、计算比例或 Shader 全局变量的单次回读代替目视检查，也不能把过去 z=45–55 不可读简单归结为已经证实的“淡出”。

## 3. 已读取的工作流和参考职责

### 3.1 工作流

- `skill/workflows/image-asset-generation.md`：生成、编辑、透明 Alpha、落盘、浅深底验收、失败处理。
- `skill/gpt-image-generation.md`：接口、multipart image[]、模型、质量与透明参数。
- `design/art-style-guide.md`：本轮最高画风依据。不能套用旧角色规范中的低分辨率、固定色数或“最多两阶阴影”。
- `plan/n1-scene-art-production-handoff.md` 第14、15节：城墙候选与正交城门拆分。

### 3.2 参考图

已确认以下源文件存在并读取：

1. **R1，世界与绘制风格**：`C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png`。只参考黄土、冷灰石材、深木、受光、建筑重量与像素色簇；不复制人物、战争箭矢、道路或整张构图。
2. **R2，可部署空间轮廓**：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0WallPrototypes_v1/n1_e0_distant_ruined_wall_module_02_v1.png`。只参考宽高关系、右侧主望楼、低矮长墙和贴地基底；不是最高画风参考。它只锁定本轮主件的空间职责，不要求逐像素重绘。
3. **R3，可部署新画风校验**：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`。用于本地检查体块和像素绘制语言，不上传进建筑生成，避免模型把角色甲胄或武器移植到城墙。
4. **R4，城门几何**：`Assets/Experiments/CurvedScroll/Art/EnvironmentV2/N1/E0GateHalf3DPrototypes_v2/gate_front_facade_orthographic_v2.png`。仅在第三批使用，定义正面立面和透明门洞，不决定新画风。

母版通过后以其作为后续墙段的建筑材质与结构参考；不能把未验收母版自动用作第二批输入。

## 4. 素材清单与批次

| 编号 | 素材 | 职责 | 生成批次 |
|---|---|---|---|
| M01 | 望楼连接长残墙 | N1 中可见的 E0 城墙方向锚点，先验收 | A：独立一张 |
| M02 | 常规垛口墙段 | 复用、连接，建立城墙连续性 | B：2×2 图集左上 |
| M03 | 大缺口破墙段 | 打散整齐重复，表达战后破损 | B：2×2 图集右上 |
| M04 | 渐低断墙端部 | 墙段收尾，连接瓦砾侧景 | B：2×2 图集左下 |
| M05 | 厚重墙墩连接段 | 增加墙体节奏，不含另一个大望楼 | B：2×2 图集右下 |
| M06 | E0 城门正面外立面 | 后续门外空间锚点，真实透明拱门 | C：独立一张，可选 |

本批不额外生成烟雾、火焰、人物、完整背景或战后道具；已有候选保留。旗帜、烟尘等需要动画时另拆，不以静态背景烟雾遮盖接缝。

M06 只交付外立面，不等于完成穿门。门洞内墙、拱顶内衬、出口、深度遮挡仍需独立半3D结构；不能让普通 YScrollScenery billboard 承担这些空间职责。

## 5. 共同生成参数与成本边界

```text
任务类型：参考图编辑 / 多图融合
endpoint：POST https://api.muskapis.com/v1/images/edits
上传：multipart/form-data，重复 image[] 字段
model：gpt-image-2.5-sunburst
quality：high
input_fidelity：high
size：1536x1024
background：transparent
output_format：png
n：1（每个批准批次）
```

- API 没有在本文确认独立 negative_prompt 字段，故将下文负面约束直接附在主 prompt 内，不发送臆造参数。
- 原生透明能力按本次实际输出验证，不因参数写了 transparent 或 HTTP 200 就宣称通过。
- 六件方案共三次拟议请求，不是已发出的任务；准确费用尚未获知，不以角色视频费用代替图片报价。
- 模型、质量、尺寸、参考图分工或生成范围改变时重新审批。超时、透明失败和效果不符时不自动付费重试。
- 默认新产物保存到当前 Windows 用户的 `Pictures/gptGen/` 下版本化子目录；不覆盖旧候选。当前主机 home 已检查为 `C:/Users/Administrator`，真正执行前仍核对最终目录。

## 6. 批次 A：M01 主城墙母版

### 6.1 参考图上传顺序

- image[0] = R1：最高风格依据。
- image[1] = R2：可部署的宽高轮廓与建筑组织依据。

### 6.2 中文完整提示词

```text
为三国战争动作游戏制作一件可独立部署的城墙环境 Sprite，不是完整场景插画。

参考图职责：第一张只定义世界风格、暖黄土与冷灰石材的关系、粗粝但有重量的建筑、明亮受光和高清像素色簇。第二张只定义可部署城墙的宽高关系和结构组织：一段横向低长墙，右侧三分之一位置有一座主望楼，沿底部少量贴地瓦砾。第一张决定画风；第二张不能把写实插画、柔雾或固定斜拍透视带入结果。不要复制第一张的角色、箭矢、道路、天空或整体构图。

主体是一段经历战火但仍可辨认结构的汉末城墙：暖赭黄夯土墙体、灰褐石砌基座、厚重垛口，右侧主望楼带深木柱梁和冷灰瓦顶。少量裂口、崩缺和靠着墙根的石块表明战后破损；不要把整面墙做成无法识别的废墟堆。主望楼是最明确的剪影读点，左段墙体相对低矮，外形起伏自然。与试摆参考保持约2.1:1的可见主体宽高关系，主体完整入画，底部基线稳定，四周至少留32像素透明安全边界；不强行拉伸参考图。

使用高清像素化三国战场美术：清楚的大体块、阶梯式轮廓、成簇硬边明暗、有限且有目的的石土纹理。暖土受光、冷灰石材与深木暗部形成层次，风化边缘可见但不铺满随机噪点。不使用照片纹理，不把普通插画套马赛克滤镜，也不是低分辨率复古像素。建筑重量和结构优先于细碎装饰。远景职责通过相对收敛的色彩和明暗表达，但主体与轮廓仍接近不透明，不画白雾、不整体降低Alpha制造距离。

建筑正面立面朝向观看者，主要竖边垂直、横向墙基水平；不烘入明显侧面透视、消失点、俯视地面、等距斜拍或透视收缩。体积来自墙面、柱梁和屋檐的明暗，不通过斜拍整栋建筑制造。

只交付这一个连贯城墙模块的真实RGBA透明PNG。没有背景、天空、山、道路、地面大平台、底座板、人物、旗帜、文字、UI、烟雾或火焰。墙根瓦砾只贴近建筑脚点，不能成为一块铺满画面的地面。禁止棋盘格、白底、灰底被画入像素，禁止光晕、投影矩形和柔化脏边；不得裁切望楼屋顶、墙体端部或瓦砾脚点。
```

### 6.3 English full prompt

```text
Create ONE independently deployable city-wall environment sprite for a Three Kingdoms war action game, not a complete scene illustration.

REFERENCE ROLES: Image 1 defines only the visual world: warm ochre earth against cool gray masonry, weighty weathered architecture, bright daylight and high-definition pixel clusters. Image 2 defines only the deployable envelope and structural arrangement: a low horizontal wall run, one dominant watchtower in the right third, and a small amount of grounded rubble. Image 1 has priority for style. Do not inherit photorealistic illustration, soft atmospheric fog or a fixed oblique camera from Image 2. Do not copy characters, arrows, roads, sky or the overall composition from Image 1.

Depict a late-Han Chinese fortification damaged by war but still structurally readable: warm ochre rammed-earth walls, gray-brown masonry foundations, thick battlements, and a dominant right-hand watchtower with dark timber beams and a cool gray tiled roof. Use a few cracks, broken edges and stones resting against the wall base to express aftermath. Do not turn the entire structure into an unrecognizable rubble heap. The watchtower is the strongest silhouette cue; the left wall remains lower with natural height variation. Aim for an approximately 2.1:1 visible-subject width-to-height ratio matching the tested spatial envelope. Keep the entire module inside the frame, a stable horizontal footing, and at least 32 pixels of transparent margin on every side. Do not stretch the reference.

Use high-definition pixel-cluster Three Kingdoms battlefield art: strong large forms, stepped contours, clustered hard-edged shading, and controlled stone-and-earth texture. Warm lit earth, cool gray stone and dark timber shadows establish depth. Weathering must support the structure rather than cover everything with random speckles. No photographic texture, no mosaic filter over ordinary illustration, and no low-resolution retro pixel style. Architectural weight and readability take priority over tiny ornament. Convey its distant-landmark role through restrained color and contrast, not white fog or globally reduced alpha. The structure and silhouette should remain nearly opaque.

Use a front-facing architectural elevation: principal vertical edges stay vertical and the wall footing stays horizontal. Do not bake in a prominent side perspective, vanishing point, visible ground plane, isometric view or perspective taper. Volume comes from shading on walls, timber and eaves, not an oblique camera angle.

Deliver only this single coherent city-wall module as a true RGBA transparent PNG. No background, sky, mountains, road, broad ground platform, display base, people, flags, text, UI, smoke or flames. Rubble may touch the architectural footing but must not become a large terrain slab. No painted checkerboard, white or gray background, halo, rectangular shadow or dirty softened fringe. Do not crop the tower roof, wall ends or grounded rubble.
```

### 6.4 验收重点

- 第一眼读成城墙与望楼，不是石堆、木牌或欧美城堡。
- 轮廓比例能在用户已确认的位置接续，不以“放大更多”弥补错误构图。
- 真实 Alpha、安全边界、贴地底部、石土分材质、无烘焙天空和烟雾。
- 缩小到真实 Game 显示尺度仍能读出望楼、垛口和低长墙；不能只给高清放大图验收。

## 7. 批次 B：M02–M05 四种墙段图集

### 7.1 前置条件与参考顺序

M01 已生成、通过机器检查并获用户目视验收后，才批准本批。

- image[0] = 已验收 M01 原生透明母版：锁定墙体材质、垛口语言、石块尺度和光向。
- image[1] = R1：辅助画风，不复制角色或场景。

### 7.2 中文完整提示词

```text
为同一三国战场城池制作一张可拆分的环境Sprite图集。第一张参考图是已经验收的城墙母版，锁定夯土颜色、石砌基座、石块尺度、垛口厚度、硬边像素色簇和光照方向；第二张只辅助高清像素化战争美术，不复制角色、道路、天空或武器。不要重新设计为另一座城池。

输出1536×1024的真实RGBA透明PNG，严格2×2布局，共四个独立城墙模块。每个模块完整放在自己的格子里，格间至少64像素纯透明间隔，外边至少32像素透明留白，无标签、分隔线、边框或背景。四个模块使用一致的绘制比例、基准墙高、石块尺寸、基础厚度和受光方向，不为填满格子而分别缩放主体。

左上：M02，常规垛口墙段。厚实暖赭黄夯土立面、灰褐石砌基座，三至四个清楚垛口，少量轻度崩缺，结构完整，不带门洞、旗帜或望楼。
右上：M03，大缺口破墙段。保持同一基准墙高和基座，中央上部有宽而不规则的崩塌缺口，暴露粗粝土石截面，瓦砾靠着墙根，左右残留部分仍能辨认原垛口节奏。破损是这段墙的一部分，不是另外漂浮的碎石。
左下：M04，渐低断墙端部。画面左侧保留完整墙高，向画面右侧自然断裂并降到墙根瓦砾，用作墙段收尾。没有俯视地面、地台和远处建筑，脚点稳定。
右下：M05，厚重墙墩连接段。中央有稍宽且稍高的夯土加固墙墩，两侧各连接一小段垛口墙；同一石砌基座，清楚的竖向加固结构，不增加屋顶、望楼、城门或奇幻装饰。

四段都是正面建筑立面，主要竖边垂直、基线水平，无斜拍、等距透视、消失点、透视收缩或烘入侧面街道。使用大体块和成簇硬边明暗保持重量，纹理有限而有方向，不能变成照片、油画、柔化插画或随机噪点。所有模块保持接近不透明实体，周围完全透明，轮廓干净。

禁止人物、文字、UI、标签、天空、山、道路、大地面平台、底座板、火焰、烟雾、旗帜、白底、灰底、伪透明棋盘格、光晕、重影。禁止不同格子相连、重叠、裁切或共享瓦砾，不能把四格画成一张完整场景。无需像素级无缝拼接，但必须看起来属于同一城墙家族。
```

### 7.3 English full prompt

```text
Create a separable environment-sprite atlas for the SAME fortified city in a Three Kingdoms battlefield. Image 1 is the approved city-wall master and controls rammed-earth color, masonry foundations, stone scale, battlement thickness, hard-edged pixel clusters and lighting direction. Image 2 supports only the high-definition pixel-cluster war-art style. Do not copy its people, roads, sky or weapons. Do not redesign this as a different city.

Deliver a true RGBA transparent PNG at 1536x1024 in a strict 2-by-2 layout containing FOUR independent wall modules. Every module must fit completely inside its own cell. Leave at least 64 pixels of purely transparent separation between modules and at least 32 pixels at the outer canvas edges. No labels, dividers, borders or background. Keep a consistent drawing scale, reference wall height, stone size, foundation thickness and lighting across all four modules. Do not independently enlarge subjects just to fill their cells.

TOP LEFT, M02: a regular battlement wall segment. Weighty warm ochre rammed-earth elevation over a gray-brown masonry foundation, three or four readable crenellations and only light edge damage. No gate opening, flags or watchtower.
TOP RIGHT, M03: a heavily breached wall segment. Retain the same reference wall height and foundation, with a broad irregular collapse in the upper center exposing rough earth-and-stone cross-sections. Rubble rests against the footing. The remaining ends still show the original battlement rhythm. Broken pieces belong to this wall, not a floating separate rock collection.
BOTTOM LEFT, M04: a descending ruined wall termination. The left side retains full wall height and breaks down naturally toward rubble on the right. It ends a wall run. No visible ground plane, display platform or distant building. Maintain a stable footing.
BOTTOM RIGHT, M05: a reinforced wall-pier connector. A slightly wider and taller central rammed-earth pier joins short battlement wall runs on both sides. Keep the same masonry foundation and readable vertical reinforcement. No roof, watchtower, gateway or fantasy ornament.

All four are front-facing architectural elevations with principal verticals upright and horizontal footing. No oblique camera, isometric view, vanishing point, perspective taper or baked-in street. Use strong large forms and clustered hard-edged shading for weight, with controlled directional texture. No photograph, oil painting, soft illustration or random pixel noise. The structures remain nearly opaque; their surroundings are completely transparent with clean cutout edges.

No people, text, UI, labels, sky, mountains, roads, broad ground slab, display base, flames, smoke, flags, white or gray background, painted checkerboard, halo or ghosting. Modules must not connect across cells, overlap, be cropped or share rubble. Do not turn the atlas into a complete scene. Pixel-perfect seamless tiling is not required, but all modules must clearly belong to the same wall family.
```

### 7.4 验收重点

- 四个主体能够独立裁切，无串格、重影、标签或脚点裁断。
- 四种轮廓不同，但基准墙高、石块尺度和受光一致；不是四种不相干画风。
- 图集方案不保证自动无缝。拼接需求依靠部署和端部遮挡验收；若必须严格无缝，另批制作材质贴片，不能把这批Sprite声称为无缝墙材质。

## 8. 批次 C：M06 E0 城门正面外立面（可选，独立批准）

### 8.1 参考顺序

- image[0] = 已验收 M01：建筑材质、用色与光照。
- image[1] = R4：严格正面外立面与透明拱门几何。
- image[2] = R1：辅助画风。

### 8.2 中文完整提示词

```text
制作一件与已经验收城墙属于同一城池的三国城门正面外立面Sprite，不是完整城门场景或穿门视频。

第一张参考图锁定墙体材质、暖赭黄夯土、冷灰石基、深木、光向与高清像素色簇；第二张仅定义严格正面立面和中央拱门空间，不能把它的柔化细碎写实纹理当成新画风；第三张辅助厚重三国战场美术，不复制人物、道路、天空或战斗内容。

主体为汉末城池的厚重正面城门：左右墙体、灰褐石砌基座、粗石拱门边框、顶部深木门楼和冷灰瓦檐，垛口宽厚。有克制的战火裂痕、墙面破边和门楼损伤，但结构仍完整可读，与母版共享同样石块尺度和材料关系。可以有少量无字、无徽记的暗朱红残布作为小面积装饰，不增加飘扬长旗杆或独立军旗。

最重要的几何规则：严格正面正交立面，竖边垂直、横边水平。中央拱门从拱顶下缘贯通到脚点，洞内必须是真实Alpha透明，直接透出查看背景。拱顶实体不可被误抠除。没有门扇、封闭栅栏、落下的尖刺、门洞内墙、洞内阴影实心板、道路、天空或出口背景堵住通道；不把门洞内侧深度画进单张图片。左右墙脚处于同一水平基线，建筑全部入画，四周至少32像素透明边界。

采用高清像素化三国战场美术，清楚大体块、成簇硬边光影、干净阶梯边缘，暖土受光与冷石深木形成重量。纹理不覆盖结构，无照片质感、油画、普通插画像素滤镜、软光晕或随机碎像素。

输出1536×1024真实RGBA透明PNG，只含一个完整外立面。禁止透视收缩、三分之四斜拍、等距视角、俯视地面、人物、武器、文字、UI、烟雾、火焰、整片背景、白底、灰底、伪透明棋盘格、实体门洞填充、主体裁切和透光墙体。本素材只服务城门外部立面，不声称表现真实穿门深度。
```

### 8.3 English full prompt

```text
Create ONE front-facade sprite of a Three Kingdoms city gate belonging to the same fortified city as the approved wall. This is not a complete gateway scene or a gate-traversal video.

Image 1 controls materials, warm ochre rammed earth, cool gray masonry, dark timber, lighting direction and high-definition pixel clusters. Image 2 controls ONLY the strict frontal elevation and central arch opening; do not inherit its soft, finely textured realism as the new style. Image 3 supports weighty Three Kingdoms battlefield art. Do not copy people, roads, sky or combat content.

Depict a late-Han Chinese city gate with substantial side walls, gray-brown masonry foundations, a rough stone arch frame, a dark timber gatehouse with cool gray tiled eaves above, and thick battlements. Use restrained war damage, cracked earth, chipped edges and minor gatehouse damage while retaining a coherent structure. Match the master's stone scale and material relationships. A small amount of plain dark vermilion torn cloth may provide accent, with no writing or emblem. Do not add tall flagpoles or independent military flags.

HIGHEST-PRIORITY GEOMETRY: strict front-facing orthographic elevation, verticals upright and horizontals level. The central arch opening must be genuinely alpha-transparent from beneath the arch crown all the way to the footing. It must reveal any background placed behind the PNG. Preserve the solid arch crown; do not mistakenly cut it away. No door leaves, closed portcullis, lowered spikes, tunnel side walls, solid shadow panel, road, sky or exit backdrop may fill the opening. Do not bake tunnel depth into this single image. Both side-wall feet share a horizontal baseline. Keep the entire building inside the canvas with at least 32 pixels of transparent margin on every side.

Use high-definition pixel-cluster Three Kingdoms battlefield art with strong large forms, clustered hard-edged shading and clean stepped contours. Warm lit earth against cool stone and dark timber creates weight. Texture supports structure. No photographic surface, oil painting, mosaic filter over ordinary illustration, soft halo or random pixel speckles.

Deliver a true RGBA transparent PNG at 1536x1024 containing only this single complete exterior facade. No perspective taper, three-quarter view, isometric angle, visible ground plane, people, weapons, text, UI, smoke, flames, full background, white or gray fill, painted checkerboard, solid-filled gateway, cropping or translucent masonry. This asset is only an exterior facade and does not claim to provide actual traversable tunnel depth.
```

### 8.4 验收重点

- 用白、深灰和土色背景分别检查门洞贯通且拱顶实体保留。
- 不将“观察缝”当作可穿越门洞，不生成堵路背景。
- 与 M01 是同一材质家族；主立面没有被烘入斜向透视。
- 本轮不生成内墙/拱顶/出口，不部署穿门系统。

## 9. 生成、落盘、处理与审批步骤

1. 将拟执行批次的中英文完整提示词、负面约束、参考图职责和参数提交用户确认。明确这一次只生成哪一批；阅读本文不等于批准。
2. 核对源图可读，检查 `MUSK_API_KEY` 存在但不输出密钥。确认版本化目录，不运行历史生成脚本，以免误发旧收费任务。
3. 按指定图片顺序实际 multipart 上传，不只把本地路径写入 prompt。
4. 每个批准批次只提交一次；记录请求参数、参考图路径/哈希、响应及实际用量。签名 URL 不作为长期交付，下载图片到本地。
5. 原图、request.json、response.json 保留。检查 PNG 签名、实际尺寸、Color Type/模式、Alpha 最小/最大值、透明与半透明像素数、四角和主体区域。
6. 不把 Alpha 最大值253/254直接判为失败；但墙体内部应接近不透明，不接受整面墙低Alpha。检查白底、深底和土色底的光晕、灰边、彩边与透底。
7. 若无 Alpha、画入棋盘格或透明背景参数被忽略，明确标为失败候选，停止自动后续请求。由用户决定批准重试或另选抠图分支；不循环堆提示词付费试错。
8. 图集裁切按真实Alpha主体与格区处理，保留透明安全边界。只清理确认是生成杂片的独立碎片，不误删瓦砾、破口和门洞拱顶。保留原始未裁切版本。
9. 提供完整图、浅深底对照、轮廓缩小预览及实际文件路径给用户验收。母版未通过不推进图集；图集未通过不自动补生。
10. 只有用户授权纳入项目后，才输出新的版本化Assets目录并导入；不覆盖现有PNG、不删除旧图、不改变GUID。

## 10. 后续 Unity 导入与场景验收（本轮不执行）

### 10.1 墙体 Sprite 接入

- 沿用环境规格，不套用角色PPU16：Sprite/Single、PPU100、Point、Uncompressed、Alpha Is Transparency=true、mipmap off、maxTextureSize2048。
- 通过 Unity TextureImporter API 修改设置；不得直接改 .meta。
- 依据真实 Alpha 底边计算 Custom Pivot，以建筑墙脚为地面基线；透明边距与贴地瓦砾需要同时检查。
- 不用 Renderer.bounds.min.y 加第二次 Y 补偿，不移动 Battle Camera 或战斗对象。
- 普通城墙使用现有环境投影材质并加入 scenery；不能把整个城池烘进天空背景，也不能假设提高 Sorting Order 一定解决投影深度问题。
- 保留用户当前对象和Transform。在用户同意替换前，用旁置或可开关对照对象验收新素材，不自动删除其试摆结果。

### 10.2 画面与持久化检查

- 先按真实Alpha尺寸换算初始缩放，再检查用户已确认的可见位置；不把旧图片Scale机械复制给高分辨率图片。
- 检查开局/静止观测、接近、两侧掠过与到站构图，确认无突然出现、贴地错位、门洞堵路、屋顶裁断或遮挡中央战斗区。
- 场景与纯背景两种视图都提供给用户，保留其可亲自检查的对象。临时运行时隐藏不能保存进 Battle 或当作正式结果。
- 仅在用户授权范围内保存 Y 或独立E0表现层；不 Save All，不打开或保存 `Assets/Experiments/CurvedScroll/FakeRouteDataTrial.unity`。
- 保存后回读具体对象、Sprite、材质、scenery引用与dirty，再检查实际Game；不能只根据SaveScene回执声称部署完成。

## 11. 行进时间约束

用户确认的目标是保留既有段的行进时长，不要求画面中城墙距离与实际路线距离严格对应。

代码证据：`Assets/Scripts/Core/BattleYRouteHost.cs` 的 `MoveTo(end, seconds)` 用 `elapsed / duration` 采样现有进度曲线，再在起终点之间插值，终点与时长分别输入。这个结论不意味着已有正式N1→E0距离或本轮已经接通该流程；源代码默认时长也不等同实际场景序列化值。

未来授权实现时：

```text
u = 段内有效行进时间 / 该段既有时长 T
显示进度 = 原有进度曲线(u)
画面距离 = 起点 + (视觉终点 - 起点) × 显示进度
```

- 固定T，按画面需要调整视觉终点或布景显露位置；不以新距离/旧速度反推更长或更短时长。
- 保留既有加减速、暂停与到站流程，不用生成素材改变逻辑节点拓扑或到站触发。
- 时间验收至少比较正常旅行、暂停恢复和到站；如允许跳过，沿用既有跳过语义，不强行把跳过也等同完整播放时长。
- 当前只准备素材，不修改 `YScrollSample.Evaluate()`、Length、junction、radius、turnAngle、Battle相机或旅行参数。

## 12. 完成与未完成

本轮完成：工作流与参考读取、试摆保存值核对、六件素材计划、三批完整中英文提示词与负面约束、参数和验收流程。

本轮未完成且未执行：图片生成、收费请求、去背裁切、Unity导入、素材替换、城门穿越、旅行时间配置或测试。
