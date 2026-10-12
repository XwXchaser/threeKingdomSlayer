---
id: kd_cc7898d6-4a6d-4d43-8452-39780a2211a0
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Stab 长枪与红缨：补充顶视／底视和中文说明

## 当前范围与执行状态

用户要求长枪和红缨都提供顶视图、底视图，并配对应文字解释。最终交付为两张五视图建模参考板，各含正面、右侧、背面、顶视、底视及中文说明，同时保留无文字原图。

V03各一次生成均视角失败，记录在 `StabWeaponArtSource/07_TopBottomV03_Failed/`。用户随后明确批准V04两组修订，各一次生成已完成，形成端向构图，但深度比例仍与原三视图不一致，**合格建模顶底图仍为0**。

当前交付为 `StabWeaponArtSource/09_FiveViewV04/` 的两张2560×1760中文评审板、两张无字板、十张独立视图、生成原图及追溯记录。图上明确标明“顶视候选／底视候选”和“几何未通过”。旧三视图像素保持一致；顶底各对按共同倍率排版。校验见同目录 `delivery_verification.json`。以下提示词为V03历史规格；V04执行记录见 `plan/stab-top-bottom-v04-revision.md`。本次顶底共四次获批生成已执行，无自动追加。

## 1. 统一观察坐标

- +Y：枪尾到枪尖；红缨尾端到根部，红缨静止垂落方向为−Y。
- +X：正面图的画面右侧。
- +Z：正面观察者所在一侧；前后厚度沿Z。
- 正面：从+Z朝−Z；画面右为+X、上为+Y。
- 右侧：从+X朝−X；画面右为−Z、上为+Y。
- 背面：从−Z朝+Z；画面右为−X、上为+Y。
- 顶视：从+Y朝−Y；投影平面X–Z，画面右为+X、上为−Z。
- 底视：从−Y朝+Y；投影平面X–Z，画面右为+X、上为+Z。

顶视和底视的画面上下方向不同，这是绕到相反观察侧后的坐标关系，不代表重新摆姿态。顶底是整个物体的端向正交投影，采用正常可见遮挡；不是横向摆放的正面，不是剖切截面，不是零件展开图，不是把二维图旋转90度。

## 2. 新视角能回答的建模问题

### 枪体

| 视图 | 板上中文说明 |
|---|---|
| 正面 FRONT | 锁定全枪轮廓和分段比例：双刃枪头、金绿连接件、红棕直杆与金色尾鐏。 |
| 右侧 RIGHT | 检查中央刃脊、刃口厚度、圆杆及尾件深度；侧面应比正面窄。 |
| 背面 BACK | 检查同一刃片的背面与金绿箍绕轴连续关系；零件数量和高度一致。 |
| 顶视 TOP | 从枪尖上方沿−Y看向枪尾，检查刃宽／厚度和连接件的端向轮廓；按正常遮挡显示。 |
| 底视 BOTTOM | 从枪尾下方沿+Y看向枪尖，检查尾鐏截形与同轴装配；正常遮挡，不把后方零件展开。 |

细长枪沿Y投影后会变成紧凑的端向形状。为了看清结构，说明板可将顶底一对共同放大，同一倍率、明确标注“端向放大”；它们不与长幅正侧背共享像素比例。不得用放大改变顶底之间的相对尺寸。

### 红缨

| 视图 | 板上中文说明 |
|---|---|
| 正面 FRONT | 锁定一个收束根、一片连续布面、一个主尖尾，以及外展后回弯的轮廓。 |
| 右侧 RIGHT | 检查前后起伏、浅折面和薄边；不能把宽正面重复成侧面。 |
| 背面 BACK | 检查同一布片的反面折面和同一根部；保持长度与静止姿态。 |
| 顶视 TOP | 从固定根上方沿−Y看向尾端，检查布片在X–Z平面的展开及前后起伏。 |
| 底视 BOTTOM | 从尾端下方沿+Y看向固定根，检查同一片布的折面重叠；姿态不变。 |

红缨有前后折面起伏，即使布厚很小，顶底投影也可能因折面叠合而有宽度；不能画成完全无厚度的直线，也不能据此建成实心粗绳。顶底应保持同一姿态和相同根部基准，不能分别生成两种风吹形状。

## 3. 参考文件与职责

| 任务 | 唯一上传参考 | SHA-256 |
|---|---|---|
| 枪体顶底 | `StabWeaponArtSource/04_BodyV02/stab_body_orthographic_v02.png` | `c108a368fbe8a4bc028c214705d243eafa84ffd9b4f66f330242b87d1d7a130b` |
| 红缨顶底 | `StabWeaponArtSource/05_TasselV02/stab_tassel_orthographic_v02.png` | `d5d59dcbad2fb4674c64a6f4be3e167f2ad488852c382d9e4bfa2cd9743373a7` |

参考只负责对应物体的形状、比例、厚度关系、材质和静止姿态。枪体不含红缨，红缨不含枪体。不要上传旧母图竞争造型；已验收枪体的折肩也不再改回旧版本。

## 4. 枪体顶底提示词

### 中文

```text
使用输入图中同一把无红缨长枪的正面、右侧和背面作为唯一形状依据，为游戏3D建模补充这把枪的顶视图和底视图。保持输入中的枪刃轮廓、中央刃脊、金绿交叉枪套、银色连接段、红棕圆杆与金色几何尾鐏；不重画正侧背，不重新设计，不改变任何零件数量或比例。

定义竖直枪轴为Y轴，+Y从枪尾指向枪尖；+X是正面画面右侧，+Z是正面观察者一侧。左区顶视从枪尖上方+Y沿−Y看向枪尾；右区底视从枪尾下方−Y沿+Y看向枪尖。顶视和底视都是整个同一物体在X–Z平面的正交端向投影，不是侧视，不是把正面横放，不是3/4视角，不是剖面，不是爆炸图。顶视画面右为+X、上为−Z；底视画面右为+X、上为+Z。

依据正侧背合理补全端向结构：枪头是沿X较宽、沿Z较薄且有中央刃脊的双刃片；枪杆为同轴圆杆；枪套和尾鐏有与侧视一致的实体深度。按真实可见遮挡绘制完整物体的端向轮廓及可见表面，不为了展示所有零件而把它们分开、透视拉长或透视缩短，不暴露被遮住的零件，不新增洞、环或装饰。

输出一张1536×1024纯白不透明PNG，左右两个等宽无边框区域，每区768×1024。左顶视、右底视，各只出现一个端向投影，共两个。两图用同一正交比例，端向尺寸可共同放大以便看清，不能独立改尺寸；主体居中、上下左右至少64像素留白，轮廓完整。光照中性、冷银钢／深绿／暖金／红棕材料区分清楚，保留输入的高清像素色簇和厚重分面。不要文字、标签、轴线、尺寸线、边框、人物、手、红缨、场景、阴影投影、运动特效。中文标注由后续本地排版添加。
```

### English

```text
Use the front, right-side and back views of the SAME cloth-free spear in the input as the sole shape reference. Add a top view and a bottom view of that spear for game 3D modeling. Preserve its blade outline, central ridge, gold-and-dark-green crossed socket, silver connection, red-brown round pole and angular gold butt. Do not redraw the existing views, redesign it, or change component counts or proportions.

Define the vertical shaft axis as Y, with +Y from butt to tip. +X is screen-right in the front view and +Z is toward the front observer. The left top view looks from above the tip at +Y toward -Y and the butt. The right bottom view looks from below the butt at -Y toward +Y and the tip. Both are orthographic end-on projections of the SAME complete object onto X–Z: not side views, a front image laid horizontally, three-quarter views, cutaway sections or exploded diagrams. Screen-right is +X in both; screen-up is -Z in the top view and +Z in the bottom view.

Complete only plausible end-on structure consistent with the existing views: a double-edged blade broad along X and thin along Z with a central ridge; a coaxial round pole; socket and butt depths corresponding to the side view. Render the actual visible silhouette and surfaces under normal occlusion. Do not separate components, stretch perspective, reveal hidden parts, add holes, rings or ornaments to make every component visible.

Output one opaque pure-white 1536x1024 PNG with two equal-width unbordered 768x1024 regions: top left, bottom right. Show exactly one end-on projection per region, two in total, at the SAME orthographic scale. Both may share one enlarged end-on scale for readability, never independent sizes. Center the complete silhouettes with at least 64 pixels of margin on every edge. Use neutral lighting, cool silver steel, dark green, warm gold and red-brown material separation; retain the input's high-definition pixel clusters and substantial planes. No text, labels, axes, dimension lines, borders, characters, hands, tassels, scenery, cast shadows or motion effects. Chinese annotations will be added locally afterward.
```

## 5. 红缨顶底提示词

### 中文

```text
使用输入图中同一根朱红红缨的正面、右侧和背面作为唯一形状依据，为游戏3D建模补充同一静止红缨的顶视图和底视图。保留一个收束固定根部、一片连续的薄柔性布面、一个主尖尾，以及输入的宽窄变化、外展后回弯和浅折面；不新增红缨，不改姿态，不重新设计。

定义Y为根部到尾端的竖直方向，+Y指向根部、−Y指向尾端；+X是正面画面右侧，+Z是正面观察者一侧。左区顶视从固定根上方+Y沿−Y看向尾端，右区底视从尾端下方−Y沿+Y看向固定根。两者是同一静止布面在X–Z平面的正交端向投影。顶视画面右为+X、上为−Z；底视画面右为+X、上为+Z。不能把正面图横放，不能变成斜俯视、3/4视图、剖面或零件展开图。

依据正侧背保持布片的横向展开和有限前后起伏，用真实可见遮挡表现根部、浅折面和尾端的重叠。布片很薄，折面会产生有限投影宽度，但不是粗绳、实心红雕塑或多条黏连流苏。上下观察不改变形状、布厚、长度或根部基准；不要为展示全长把布面拉直、展开、变成另一种风吹形状，不新增分叉、隐藏尾巴、金属环扣或枪体。

输出一张1536×1024纯白不透明PNG，左右两个等宽无边框区域，每区768×1024。左顶视、右底视，每区只有一个完整端向投影，共两个，采用同一正交比例和可比较的根部基准。为了阅读可共同放大，不能独立改变尺度；主体居中，四边至少64像素留白，不裁断。保留朱红主面、深红折面和少量橙红高光，以高清像素色簇组织布面，中性柔和光照、轮廓干净。不要文字、标签、轴线、尺寸线、边框、人物、手、枪头、枪杆、枪尾、金属件、场景、光效或运动残影。中文标注由后续本地排版添加。
```

### English

```text
Use the front, right-side and back views of the SAME vermilion spear tassel in the input as the sole shape reference. Add top and bottom views of that SAME static tassel for game 3D modeling. Preserve one gathered attachment root, one continuous thin flexible cloth surface, one main pointed terminal, the existing width variation, outward-returning curve and shallow folds. Add no tassel, change no pose and do not redesign it.

Define Y as the vertical root/terminal direction: +Y toward the root, -Y toward the terminal. +X is screen-right in the front view; +Z is toward the front observer. The left top view looks from above the root at +Y toward -Y and the terminal. The right bottom view looks from below the terminal at -Y toward +Y and the root. Both are orthographic end-on projections of the SAME static cloth surface onto X–Z. Screen-right is +X in both; screen-up is -Z in the top view and +Z in the bottom view. Do not lay the front image horizontally or make oblique overhead, three-quarter, cutaway or exploded views.

Maintain the lateral spread and restrained front-back depth inferred consistently from the existing three views. Use normal visible occlusion to show overlap of the root, shallow folds and terminal. Thin cloth can have finite projected width from folding, but must not become a thick cord, solid red sculpture or fused streamers. Opposite viewing directions do not change shape, thickness, length or the root datum. Do not straighten, unfold or invent a different wind-blown pose to expose full length. No branching, hidden tails, metal clasps or weapon parts.

Output one opaque pure-white 1536x1024 PNG with two equal-width unbordered 768x1024 regions: top left, bottom right. Each shows exactly one complete end-on projection, two in total, at the SAME orthographic scale and comparable root datum. Both may share one enlargement for readability, never independent scales. Center the silhouettes with at least 64 pixels of margin on every edge and no cropping. Retain vermilion planes, deep-red folds and restrained orange-red highlights organized as high-definition pixel clusters, with soft neutral lighting and clean edges. No text, labels, axes, dimension lines, borders, characters, hands, spearhead, shaft, butt, metal parts, scenery, glow or motion trails. Chinese annotations will be added locally afterward.
```

## 6. 双语负面约束

两组共用：

```text
正面横放、二维旋转伪装顶底视、侧视、斜俯视、3/4透视、剖面、透视爆炸图、分离零件、隐藏结构透视、不同尺寸、不同姿态、不同设计、重复物体、文字、标签、轴线、尺寸线、边框、人物、手、场景、投影阴影、裁断、运动特效。

Front image laid horizontally, fake top/bottom made by rotating a 2D image, side view, oblique overhead view, three-quarter perspective, cutaway, perspective exploded diagram, separated parts, x-ray hidden structure, different scale, changed pose or design, duplicate objects, text, labels, axes, dimension lines, borders, characters, hands, scenery, cast shadows, cropping, motion effects.
```

枪体额外：

```text
红缨、布料、新增握套、盘蛇、龙饰、新增洞或环扣、改变枪头或尾鐏。

Tassels, cloth, new grip sleeve, coiled serpent, dragon decoration, new holes or clasps, redesigned blade or butt.
```

红缨额外：

```text
枪头、枪套、枪杆、枪尾、金属环扣、粗绳、实心红雕塑、多条流苏、分叉、第二尾端、展开拉直、动态风吹姿态。

Spearhead, socket, pole, butt, metal clasp, thick cord, solid red sculpture, multiple streamers, branching, extra terminal, straightened or unfolded cloth, new wind-blown pose.
```

## 7. 参数、输出与排版

- 每组模型：`gpt-image-2.5-sunburst`；接口：`https://api.muskapis.com/v1/images/edits`。
- 每组参数：`size=1536x1024`、`quality=max`、`input_fidelity=high`、`background=opaque`、`output_format=png`、`n=1`。
- 共两次生成，一次枪体顶底，一次红缨顶底；每个任务一次提交，不自动重试或换模型。
- 拟用输出：`C:\Users\Administrator\Pictures\gptGen\stab_body_top_bottom_v03\`、`C:\Users\Administrator\Pictures\gptGen\stab_tassel_top_bottom_v03\`。目录需不存在。
- 两张纯图各自左右等宽拆为 `top.png`／`bottom.png`，保持源图比例，不独立缩放掩盖差异。
- 最终两张中文五视图板统一画布2560×1760：左侧上排为原正／右侧／背面三个512×1024图格，右侧纵向两块为顶／底端视。所有中文通过本地字体排版，并保留无字版与每图的观察方向。
- 本地准备草稿：`StabWeaponArtSource/06_FiveViewPreparation/body_five_view_draft.png`、`tassel_five_view_draft.png`。仅已有三图加说明；顶底格标为待生成。
- 完整执行规格：`StabWeaponArtSource/00_Index/five_view_v03_specification.json`。

## 8. 生成后验证

1. 枪体保持已验收V02身份，红缨保持当前V02静止曲面；新增视角不改变旧三图。
2. 两个端视确实沿±Y观察，画面为X–Z投影，遵循正常遮挡，无横放旧图或斜透视。
3. 顶底的X/Z尺寸与正侧厚度关系可解释；不能独立放大其中一幅或画成不同物体。
4. 两图完整、每格边距至少64px；检查PNG解码、参数、参考哈希与裁切同源。
5. 中文标签与解释对应正确物体、视图和观察方向，无乱码，不覆盖主体。
6. 端向隐藏面仍是基于二维图的合理补定，不宣称从已存在网格渲染或精确CAD投影。严格几何对应需后续从同一模型正交渲染。
