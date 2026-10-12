---
id: kd_fbcc9ef1-13e0-4a5e-87af-f1a4f8921338
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Stab 刚性长枪：游戏 3D 建模三视图合图准备

## 当前任务

用户最新要求：先不处理飘带，先把长枪本体三视图做好并合成一张，当前先思考关键词、准备生成。

本任务已提交一次刚性枪体的正面／右侧／背面合图生成，并已获得用户明确验收。生成结果为1536×1024 RGB PNG，完全排除红缨、飘带和布料；用户验收记录见 `StabWeaponArtSource/04_BodyV02/user_acceptance.json`。技术回执保留原样，已披露的轮廓与排版偏差不因验收而删除。用户当前要求继续制作红缨三视图，入口为 `plan/stab-tassel-three-view-generation-prompts.md`；完整装配草案尚未提交。

## 1. 长枪关键词及其职责

| 关键词 | 英文表达 | 约束 |
|---|---|---|
| 三国战场长枪、刚性武器本体 | Three Kingdoms battlefield spear, rigid weapon body | 全长完整、无人物或软布部件 |
| 尖叶形双刃钢枪头 | Pointed leaf-shaped double-edged steel spearhead | 沿用当前正面轮廓，不变成剑、戟或三叉矛 |
| 中央刃脊、薄刃截面、削薄刃口 | Central ridge, thin blade profile, tapered cutting edges | 正/背为宽面，右侧为有真实厚度的窄面 |
| 深绿枪套、暖金交叉箍与连接段 | Dark-green socket, warm-gold crossed bands and fittings | 连接件同轴，侧背绕轴结构连续 |
| 短银色连接段 | Short silver connection | 保留枪套到杆体的连接顺序，不增加新零件 |
| 连续红棕直圆杆 | Continuous straight round red-brown pole | 长度与直径在三视图保持一致，无独立握套 |
| 几何金色尾鐏、菱形折面、下尖端 | Angular gold butt fitting, diamond-shaped planes, pointed terminal | 保留现有尾部身份，侧视有体积 |
| 高清像素色簇、冷钢/暖金/木质分面 | High-definition pixel clusters, cool steel, warm metal, wood planes | 延续新画风；像素阶梯属于绘制，不逐像素建成实体锯齿 |
| 同一物体的正交三视图 | Orthographic front, right and back views of the same object | 绕竖直长轴0°/90°/180°，固定结构、尺度和关键高度 |
| 3D建模参考、纯白底、结构可读 | Game 3D modeling reference, white background, readable construction | 克制高光、无投影遮挡轮廓、无特效和背景 |

结构关键词优先于“精美”“电影级”“超细节”等泛化画质词，避免生成装饰插画或三种武器设计。

## 2. 参考图与合图规则

唯一上传参考：当前新画风的完整长枪母图。它包含一根红色飘带，但本次提示词明确要求完全删除飘带，只保留刚性枪体：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\StabWeaponArtSource\01_Reference\assembly_reference.png`

项目相对路径：`StabWeaponArtSource/01_Reference/assembly_reference.png`。

SHA-256：`6475df45a489976d0de95f042235d4080e1adf36c18c54f639dbd231ec46c9ae`。

参考职责：母图锁定枪刃、枪套、杆体、尾鐏的最新造型、材质和整体比例；红色飘带只作为必须排除的干扰物，不参与三视图。已有无布料三视图板 `StabWeaponArtSource/03_TraceV01/body/body_board.png` 只作人工对照，不上传为第二个竞争参考，以免把其中较明显的枪刃折肩带进新稿。无需上传旧速度帧或角色图。

本轮枪刃以完整母图的实际轮廓为准。不要沿用 V01 无布料板中更明显的中段折肩，也不要自行重新设计枪头；只把母图看得到的前面结构补为同一实体的右侧和背面。

直接一次生成一张合图，避免分别生成三图造成枪体身份漂移。输出1536×1024，三个512×1024等宽区域，左正面／中右侧／右背面。三根长轴分别落在x=256/768/1280附近，枪尖/枪尾和连接件高度一致，上下至少32px余量，不做单图独立缩放或用裁切掩盖比例差。

## 3. 中文提示词

```text
只参考输入图片中的新画风长枪刚性枪体，制作一张用于游戏3D建模的正交三视图合图。输入图中的红色飘带不是本次目标，必须完全删除；不要把任何布料形状带入三视图。以输入图锁定枪体的最新造型、比例、材质和零件身份，将它补成同一件可建模实体的正面、右侧面和背面，不重新设计。

保留输入图中的尖叶形双刃冷钢枪头、中央刃脊和原有枪刃外轮廓，深绿枪套与暖金交叉箍、短银色连接段、连续笔直的红棕圆杆，以及有菱形折面和下尖端的几何金色尾鐏。保持各段长度、杆径和零件数量，合理补全被飘带遮挡的枪体，不新增把手、握套或装饰，不新增大折肩、倒钩。完全不包含红缨、飘带、布料、盘蛇、龙形装饰和人物。

正面和背面显示同一刃片的两面；右侧是绕竖直枪轴90度的正交窄面，清楚显示中央刃脊厚度和向两侧削薄的刃口，不能再画成宽正面。枪杆是圆柱，三视图直径一致。交叉金箍贴合枪套，侧背面绕轴连续；枪头、连接件、枪杆与尾鐏同轴连接，零件不悬空、不发生无意穿插。尾鐏侧面显示合理厚度。像素阶梯只属于绘制边缘，不是实体锯齿。

输出一张1536×1024纯白底PNG，三个等宽无边框区域：左正面、中右侧、右背面。每区仅一把完整竖直长枪，长轴位于本区中央，同尺度，枪尖、枪尾和各连接段高度对齐，上下至少32像素余量。三个视图对应0度、90度、180度的同一物体，只改变观察方向，不改变结构、比例或姿态。

保持高清像素色簇和清楚的大体块体积，以冷银钢、深绿、暖金、红棕木质区分材质。使用中性柔和光照、克制高光和干净边缘，结构可读。不要透视、3/4视角、地面投影、背景、文字、标签、尺寸线、坐标轴、分隔线、局部放大、光效或运动残影。
```

## 4. English prompt

```text
Use only the rigid spear body in the input image to produce one orthographic turnaround sheet for game 3D modeling. The input contains a red ribbon, but that ribbon is outside this task and must be removed completely; carry no cloth shape into the views. Use the input to lock the weapon body's current design, proportions, materials and component identity, then resolve it as ONE buildable object in front, right-side and back views without redesigning it.

Preserve the input's pointed leaf-shaped double-edged cool-steel head, its existing blade outline and central ridge; dark-green socket with warm-gold crossed bands; short silver connection; continuous straight round red-brown pole; and angular gold butt fitting with diamond-shaped planes and a pointed terminal. Keep segment lengths, pole diameter and component count. Plausibly complete the body hidden by the ribbon. Add no handle, grip sleeve, ornament, large stepped shoulder or hook. Exclude all tassels, ribbons, cloth, coiled serpents, dragon decorations and characters.

Front and back show the two faces of the SAME blade. The right view is an orthographic 90-degree rotation around the vertical shaft axis, exposing the narrow blade profile, central ridge thickness and tapered cutting edges, not another broad front view. The round pole has identical diameter in every view. Crossed gold bands follow the socket continuously around its axis. Head, connectors, pole and butt form one coherent axial assembly with no floating parts or unintended intersections. Show plausible butt thickness from the side. Pixel stair steps are drawing language, not physical serrations.

Output one pure-white-background 1536x1024 PNG with three equal-width unbordered regions: front left, right side center, back right. Each region contains one complete upright spear, its shaft centered, at identical scale with aligned tip, butt and connector heights and at least 32 pixels above and below. These are 0, 90 and 180-degree views of ONE object. Only the viewing direction changes, not structure, proportions or pose.

Preserve high-definition pixel clusters and readable large forms. Separate materials with cool silver steel, dark green, warm gold and red-brown wood. Use soft neutral lighting, restrained highlights and clean edges. No perspective, three-quarter views, ground shadows, scenery, text, labels, dimension lines, axes, dividers, detail insets, glow or motion trails.
```

## 5. 负面词 / Negative constraints

```text
红缨、飘带、布料、皮革握套、独立把手、盘蛇、龙饰、人物、手、剑、戟、三叉矛、额外零件、枪杆弯曲、三种设计、视角间变比例、侧面重复宽正面、悬空连接件、无意穿插、实体锯齿、过强反光、模糊轮廓、裁断、透视、3/4视角、投影、背景、文字、标签、尺寸线、坐标轴、分隔线、特效、残影。

Tassels, ribbons, cloth, leather grip sleeve, separate handle, coiled serpent, dragon ornament, characters, hands, sword, halberd, trident, extra components, bent shaft, three different designs, changed proportions between views, broad front blade repeated as a side view, floating fittings, unintended intersections, physical serrations, excessive reflections, blurred edges, cropped weapon, perspective, three-quarter view, cast shadows, scenery, text, labels, dimension lines, axes, dividers, effects, motion trails.
```

## 6. 生成参数和交付

| 参数 | 拟用值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst`，沿用此前成功记录 |
| 接口 | `https://api.muskapis.com/v1/images/edits` |
| 尺寸 | `1536x1024` |
| 质量 / 参考保真 | `max` / `high` |
| 背景 / 格式 | `opaque`，白底 PNG，非透明 |
| 数量 | `n=1`，一次任务、一张合图 |
| 输入 | 仅完整造型母图；提示词明确删除全部飘带 |
| 提交策略 | 已按用户明确指令单次提交；结果未知时不自动重提或换模型 |
| 实际结果 | HTTP 200；1536×1024 RGB PNG；文件 SHA-256 `c108a368fbe8a4bc028c214705d243eafa84ffd9b4f66f330242b87d1d7a130b`；一次提交 |
| 原始成图 | `C:\Users\Administrator\Pictures\gptGen\stab_body_orthographic_v02\stab_body_orthographic_v02.png` |
| 项目归档 | `StabWeaponArtSource/04_BodyV02/stab_body_orthographic_v02.png`；完整 trace 同目录 `trace/` |

实际输出目录：`C:\Users\Administrator\Pictures\gptGen\stab_body_orthographic_v02\`。

最终候选合图：`stab_body_orthographic_v02.png`。请求、回执和检查记录在原始目录 `trace/`，项目内逐字节归档于 `StabWeaponArtSource/04_BodyV02/`。本次保留合图作为主交付，没有拆图或重采样。

本地规格副本：`StabWeaponArtSource/00_Index/body_v02_specification.json`。规格已更新为 `generated_verified_user_accepted`，提交次数为1；用户验收记录独立保存于 `StabWeaponArtSource/04_BodyV02/user_acceptance.json`。

## 7. 验收重点

- 正/右侧/背面是一件武器；长轴居中，尖、尾和全部连接段高度对齐。
- 轮廓、段长、杆径、金绿件和尾鐏对应当前正面参考；没有红带、握套或新增结构。
- 侧面有真实刃片/尾件厚度；枪套交叉箍绕轴关系可解释；结构没有悬空。
- PNG尺寸正确、可解码、主体完整；发现投影不一致如实标记，不通过单视图缩放掩盖。
- 该图用于建模参考。真正严格的三视图最终由同一确认模型正交渲染，生成图本身不构成3D模型交付。
