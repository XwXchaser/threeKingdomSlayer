---
id: kd_6bcd5f5f-70bc-44b2-bb19-084f9e4e2f39
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Stab 红缨：游戏 3D 建模三视图合图准备

## 当前任务

用户已验收刚性长枪 V02 三视图，现在要求制作红缨三视图。本任务只生成红缨这一件柔性布料部件的正面／右侧／背面合图，不重新生成枪体，不把枪体带进输出。

本轮准备采用两张参考图，并严格分工：

1. **完整母图**负责红缨在整枪上的真实身份、连接根位置、正面总体比例和原始向右外展再回弯的姿态。
2. **已有红缨单体正面图**负责放大锁定红缨本身的轮廓、宽窄变化、布面折线、朱红/深红/橙红色簇和单根连续结构。

已有 V01 红缨侧面、背面和整块板图只作为人工比较，不再上传其他参考；其记录明确说明侧背面是从单一正面来源推定，不能把它当作已验收蓝图。V01 正面单图以参考 B 身份使用。用户明确回复“批准生成”后，本版已单次提交，HTTP 200 返回1536×1024 RGB PNG，完成技术校验与项目归档，红缨最终美术验收待确认。

## 1. 红缨造型关键词

| 关键词 | 英文表达 | 约束 |
|---|---|---|
| 单根朱红红缨 | One continuous vermilion spear tassel | 一个固定根、一个连续主体、一个主尾端 |
| 枪套右侧固定根 | Gathered attachment root on the right side of the socket | 根部位置沿用完整母图，不悬空，不增加环扣 |
| 向右外展、向下回弯 | Outward sweep to the viewer's right, then a returning downward curve | 保留当前正面剪影和方向感 |
| 成束布片、浅折面 | Single bundled thin cloth surface with shallow folds | 是薄布片，不是粗绳、旗帜、羽毛或实体雕塑 |
| 宽窄渐变、单主尖尾 | Controlled width variation and one tapered main terminal | 尾端不分叉，不出现第二根尾巴 |
| 朱红主面、深红暗面、橙红高光 | Vermilion main planes, deep-red shadow folds, restrained orange-red highlights | 延续当前高清像素色簇，不使用发光 |
| 侧面薄边、有限前后深度 | Thin side edge with restrained front-back depth | 右侧不能重复正面宽度 |
| 同一静止物体的正交三视图 | Orthographic front, right side and back views of the same static tassel | 只改变观察方向，不换姿态、不换比例 |
| 游戏 3D 建模参考 | Game 3D modeling reference | 白底、无文字、无枪体、结构可读 |

避免使用“飘逸丝带、华丽流苏、动态旗帜、长披风、飞舞布条”等开放词，它们容易生成多根分叉布条或动态姿态，而不是可建模的一根红缨。

## 2. 参考图职责与文件

### 参考 A：完整枪体母图

绝对路径：

`C:\Users\Administrator\Pictures\gptGen\stab_original_shape_restyle_v01_retry02\stab_original_shape_restyle_v01_retry02.png`

项目归档：`StabWeaponArtSource/01_Reference/assembly_reference.png`

SHA-256：`6475df45a489976d0de95f042235d4080e1adf36c18c54f639dbd231ec46c9ae`

只负责：

- 红缨在枪套右侧的固定根位置；
- 红缨相对枪头、枪套和枪杆的纵向比例；
- 正面向右外展后向下回弯的整体姿态；
- 当前新画风与枪体材质关系。

输出中不得出现枪头、枪套、枪杆、枪尾或任何其他枪体部件。

### 参考 B：红缨单体正面图

项目归档：`StabWeaponArtSource/02_OrthographicV01/ribbon/front.png`

原始路径：`C:\Users\Administrator\Pictures\gptGen\stab_orthographic_refs_v01\ribbon\front.png`

SHA-256：`e9e28892eb093323310391a4a52e0ca157047fd3be2af851e66faf989d5d7090`

只负责：

- 单根红缨的前视轮廓；
- 根部收束、主体变宽、外展、回弯和主尖尾；
- 布面中的朱红、暗红、橙红色簇和折线组织。

它是当前已有红缨候选的正面形状参考，不代表侧背面已严格确定。已有 `ribbon/right.png` 与 `ribbon/back.png` 只作人工对照，不上传，避免多个推定版本互相冲突。

## 3. 合图规则

一次生成一张合图，避免三次生成导致红缨身份、长度和折面漂移。

- 画布：1536×1024；纯白不透明背景；PNG；RGB；
- 三个等宽无边框区域，每区 512×1024；
- 左：正面；中：右侧面；右：背面；
- 每区只显示一根完整红缨，不显示枪体；
- 统一尺度，根部与主尾端高度对齐；
- 正面保留当前外展后回弯轮廓；
- 右侧显示薄片边缘、浅折面和真实有限厚度，不能变成正面宽度的复制；
- 背面显示同一片布的反面折面和同一根部，不能重新设计成另一种姿态；
- 每个主体距区域边缘留至少 32px，不能裁断根部或尾端；
- 不在生成后独立缩放或平移单个视图；如需拆图，只能按三个固定等宽区域原样裁切。

## 4. 中文提示词

```text
使用两张输入参考图，制作一张用于游戏3D建模的单根朱红红缨正交三视图合图。参考图A是完整的新画风长枪，只负责锁定红缨在枪套右侧的固定根位置、相对枪体的纵向比例、正面向右外展后向下回弯的整体姿态和当前高清像素材质关系。参考图B是红缨单体正面图，只负责锁定这根红缨本身的轮廓、宽窄变化、布面折线和朱红/深红/橙红色簇。不要混合其他版本的红缨设计。

输出中只显示红缨，不显示枪头、枪套、枪杆、枪尾、金属环扣、人物或场景。红缨必须是一个固定收束根部、一片连续的薄柔性布面、一个主要渐尖尾端。保留参考图A中的根部位置和参考图B中的正面剪影：根部收束，主体向观察者右侧外展，随后向下回弯，宽度沿曲线有设计过的渐变，最后收成一个主要尖尾。不要缩短、拉长、改变方向或增加第二根尾巴。

把它解释为一片可建模的薄布片，带少量连续浅折面和有限前后深度。不是粗绳，不是实心红色雕塑，不是羽毛，不是旗帜，不是披风，不是多根流苏。保持朱红主面、深红暗部和少量橙红高光，以高清像素色簇表达布料体积；不要发光，不要运动残影，不要把像素阶梯画成锯齿或裂口。

制作同一件静止三维红缨的正面、右侧面和背面正交投影，只改变观察方向，不改变姿态、长度、根部位置或比例。输出一张1536×1024纯白底PNG，分成三个等宽无边框区域：左正面、中右侧、右背面。每区只显示一根完整红缨，统一尺度，根部和主尾端高度对齐，距上下左右区域边缘至少32像素。正面显示完整外展后回弯剪影；右侧显示薄边、浅折面和有限厚度，不能重复正面宽度；背面显示同一布片的反面折面和同一固定根部，不能凭空换姿态。

结构清楚、轮廓干净、中性柔和光照、克制高光。不要枪体、金属件、环扣、人物、手、场景、文字、标签、尺寸线、坐标轴、分隔线、透视、3/4视角、局部放大、背景纹理、光效或运动残影。
```

## 5. English prompt

```text
Use the two input reference images to produce one orthographic turnaround sheet of a single vermilion spear tassel for game 3D modeling. Reference A is the complete new-style spear and is used only to lock the tassel's attachment root on the right side of the socket, its vertical proportion relative to the weapon, its overall front pose sweeping outward to the viewer's right and returning downward, and the current high-definition pixel material language. Reference B is the isolated front view of the tassel and is used only to lock this tassel's silhouette, width variation, cloth fold organization and vermilion/deep-red/orange-red color clusters. Do not combine other tassel designs.

Show ONLY the tassel. Do not show the spearhead, socket, shaft, butt, metal clasps, characters or scenery. The tassel must be one gathered fixed root, one continuous thin flexible cloth surface and one main tapered terminal. Preserve Reference A's attachment position and Reference B's front silhouette: a gathered root, a body sweeping outward toward the viewer's right, then curving back downward, with deliberate width variation along the curve and one primary pointed terminal. Do not shorten, lengthen, redirect or add a second tail.

Resolve it as a modelable thin cloth surface with a few continuous shallow folds and restrained front-back depth. It is not a thick cord, solid red sculpture, feather, flag, cape or multiple streamers. Preserve vermilion main planes, deep-red shadows and limited orange-red highlights, using high-definition pixel clusters to express cloth volume. No glow, motion trails, physical saw teeth or deep tears.

Produce front, right-side and back orthographic projections of the SAME static three-dimensional tassel. Only the viewing direction changes; pose, length, root position and proportions remain fixed. Output one pure-white-background 1536x1024 PNG divided into three equal-width unbordered regions: front left, right side center and back right. Each region contains one complete tassel at identical scale, with aligned root and main-terminal heights and at least 32 pixels of margin from every region edge. The front shows the complete outward-then-returning silhouette; the right side shows the thin edge, shallow folds and limited thickness rather than repeating the broad front width; the back shows the reverse folds of the same cloth surface and the same fixed root, not an invented new pose.

Use clean readable construction, soft neutral lighting and restrained highlights. No spear body, metal parts, clasps, characters, hands, scenery, text, labels, dimension lines, axes, dividers, perspective, three-quarter view, detail inset, textured background, glow or motion trails.
```

## 6. 负面词 / Negative constraints

```text
枪头、枪套、枪杆、枪尾、金属环扣、人物、手、场景、双红缨、多根飘带、分叉尾巴、隐藏第二个尾端、粗绳、实心红雕塑、羽毛、旗帜、披风、蝴蝶结、盘蛇、龙形、深裂口、锯齿裂边、轮廓自交、绕杆、不同姿态、不同长度、不同尺度、透视、3/4视角、独立放大、裁断、文字、标签、尺寸线、坐标轴、分隔线、背景纹理、发光、运动残影。

Spearhead, socket, shaft, butt, metal clasps, characters, hands, scenery, paired red tassels, multiple ribbons, branching tail, hidden second terminal, thick cord, solid red sculpture, feather, flag, cape, bow knot, coiled serpent, dragon form, deep tears, saw-tooth torn edges, self-crossing silhouette, wrapping around a shaft, changed pose, changed length, changed scale, perspective, three-quarter view, independently enlarged view, cropping, text, labels, dimension lines, axes, dividers, textured background, glow, motion trails.
```

## 7. 生成参数与审批状态

| 参数 | 拟用值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst` |
| 接口 | `https://api.muskapis.com/v1/images/edits` |
| 尺寸 | `1536x1024` |
| 质量 / 参考保真 | `max` / `high` |
| 背景 / 格式 | `opaque`，纯白底 RGB PNG |
| 数量 | `n=1`，一次生成一张合图 |
| 输入参考 | 参考 A 完整母图 + 参考 B 红缨单体正面图 |
| 拟用输出目录 | `C:\Users\Administrator\Pictures\gptGen\stab_tassel_orthographic_v02\` |
| 失败策略 | 只提交一次；结果未知不自动重提、不换模型 |
| 当前状态 | `generated_verified_user_acceptance_pending` |
| 实际提交 | 用户明确批准后提交一次，HTTP 200；无重试 |
| 原始成图 | `C:\Users\Administrator\Pictures\gptGen\stab_tassel_orthographic_v02\stab_tassel_orthographic_v02.png` |
| 项目归档 | `StabWeaponArtSource/05_TasselV02/stab_tassel_orthographic_v02.png` |
| SHA-256 | `d5d59dcbad2fb4674c64a6f4be3e167f2ad488852c382d9e4bfa2cd9743373a7` |

用户已明确批准本版中英文提示词、负面词、两张参考图职责及参数。本次生成已完成，PNG chunk CRC、IDAT解码、1536×1024尺寸与参考哈希核对通过。原始请求、回执在输出目录 `trace/`；项目归档含合图及8份提示词/请求/状态/校验文件，逐字节一致；原始服务 response 只在原始目录保留。

## 8. 验收重点

- 只出现红缨，不出现任何枪体零件；
- 正面保留当前母图与单体参考中的单根外展回弯轮廓；枪体验收不代表红缨造型已验收；
- 右侧是真实薄边和有限厚度，不是正面宽度复制；
- 背面属于同一片布，根部、长度、主尾端和折面逻辑可解释；
- 三视图统一尺度，根部和尾端对齐，主体不裁断；
- 朱红、深红、橙红材质关系符合新画风；
- 输出是二维建模参考，不宣称为网格、骨链或布料模拟交付。

## 9. 实际检查结果与待验收项

合图目检：左正面／中右侧／右背面，无枪体、金属、人物或背景；每视图一个收束根部和一个主尖尾。右侧明显较窄，呈现薄布折面；正背保留朱红、深红及橙红像素色簇。

边界审计使用 `min(R,G,B)<220` 近似可见主体阈值，三个512×1024区域的包围盒分别为正面 `[147,74,505,964]`、右侧 `[193,77,340,960]`、背面 `[14,74,357,964]`。均在各自区域内，无裁断；但正面右侧余量约6px、背面左侧约14px，未达到原设定32px全边距。完整主体顶部差3px，红色主体顶部差1px，尾端底部差4px；阈值测量不是几何尺寸。

本次没有独立缩放、平移、裁切或自动重生成。正面造型、侧背折面对应及排版余量仍由用户验收；技术成功不等于最终美术通过。
