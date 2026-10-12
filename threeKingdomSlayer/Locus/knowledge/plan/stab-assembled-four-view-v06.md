---
id: kd_0963422f-ec65-4043-bdd3-1ee4210f2c28
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# V5 补左侧：带红缨长枪前／左／右／后四视图 V06

## 当前任务

用户明确需要前、左、右、后四张单图及合图。以完整装配V5为基准，只新增左侧一张，不重新生成现有前、右、后，不把顶底候选放进本次合图。

用户已明确批准补左侧，单次生成HTTP 200返回1024×1536 RGB PNG。中断前生成已完成，恢复时仅修复本地绘图参数并完成等比对齐、四张单图和合图，未再次生成。主交付在 `StabWeaponArtSource/12_AssembledFourViewV06/`：前／左／右／后单图各384×1024、1536×1024无字合图和1856×1440中文标注版。前右后与V5逐字节一致；四个单图与合图对应区域逐像素一致。

## 参考图与观察方向

唯一上传参考：`StabWeaponArtSource/12_AssembledFourViewV06Preparation/front_right_back_reference.png`。左到右依次是V5前、右、后，负责枪体身份、全枪比例、分段高度、单根红缨和固定静止姿态。

原图：

- `StabWeaponArtSource/11_AssembledFiveViewV05/views/front.png`
- `StabWeaponArtSource/11_AssembledFiveViewV05/views/right.png`
- `StabWeaponArtSource/11_AssembledFiveViewV05/views/back.png`

Y向枪尖，+X是前视画面右侧，+Z朝前视观察者。**左视观察者位于−X，沿+X看，画面右为+Z，上为+Y。** 右视则从+X沿−X看。

这是同一把枪的另一侧：外部轮廓与右侧基本镜像对应，但可见钢刃面、金绿连接件、红缨根部和折面遮挡会不同。红缨根在物体+X一侧，从−X观察时可能被枪套部分遮住；不强制把结重新挪到观察者前面，不通过水平翻转右视图冒充左侧。

## 中文提示词

```text
根据输入参考图，补画这同一把带单根朱红红缨长枪的左侧正交单视图。输入图左到右是前、右、后三个现有视图，只用于锁定武器造型、长度比例、零件高度、材质和红缨静止姿态。输出只能有一把完整长枪的左侧，不生成多视图，不重设计枪体或红缨。

定义+Y指向枪尖，+X是前视图画面右，+Z朝前视观察者。左侧观察者位于−X，沿+X看向武器，画面右为+Z、上为+Y。这是参考右侧的相反观察方向，不是前面、后面、斜视或3/4。枪刃从这个方向显示狭窄侧面和中央脊厚度，不画成宽大的正面叶形刃。

保留冷银双刃枪头、中央刃脊、金绿交叉枪套、短银色连接段、连续红棕直圆杆和金色几何尾鐏，保持全部分段长度、杆径及连接高度。红缨只有一个固定收束根、一片连续薄布面、一个主尖尾，挂在金绿枪套上部的同一+X侧。保持现有下垂长度、外展回弯和有限前后起伏，不换风吹姿态。由−X观察时允许枪套和枪杆正常遮住远侧的根部或布面；显示同一条红缨另一面的折面，不为了看清把结移到近侧。不能直接水平镜像或复制右侧图，不能新增第二根红缨或零件。

输出一张1024×1536纯白不透明PNG，只含一把完整竖直长枪，居中，枪尖、枪尾及红缨末端全部可见，四边留至少64像素。正交投影，不透视缩短，不倾斜。保持参考的高清像素色簇、冷钢、深绿、暖金、红棕木质和朱红深红布面，中性柔和光照、干净边缘。不含人物、手、背景、文字、标签、尺寸线、坐标轴、边框、光效或残影。
```

## English prompt

```text
Use the input reference to add ONE orthographic LEFT-SIDE view of the SAME spear with one attached vermilion tassel. The input contains existing FRONT, RIGHT and BACK views from left to right. They lock weapon identity, proportions, component heights, materials and the fixed cloth pose. Output only one complete spear from its LEFT side, not a multi-view sheet or a redesigned weapon.

Define +Y toward the tip, +X as screen-right in the front view, and +Z toward the front observer. The LEFT observer is at -X looking along +X, with screen-right +Z and screen-up +Y. This is the opposite observation direction to the reference RIGHT view, not front, back, oblique or three-quarter. Show the narrow blade profile and central ridge thickness, not the broad leaf-shaped front face.

Preserve the cool-silver double-edged head, central ridge, gold-and-dark-green crossed socket, short silver connection, continuous straight round red-brown pole and angular gold butt. Match all segment lengths, pole diameter and connection heights. The tassel has one gathered fixed root, one continuous thin cloth surface and one main tapered terminal, attached at the SAME upper socket on the object's +X side. Preserve its existing drop, outward-returning curve and restrained depth, with no new wind-blown pose. From -X, allow the socket and pole to normally occlude the far-side root or cloth. Show the reverse cloth folds rather than move the attachment to the near side for visibility. Do not simply horizontally mirror or copy the RIGHT image. No second tassel or extra component.

Output one opaque pure-white 1024x1536 PNG containing exactly one complete upright spear, centered with tip, butt and cloth terminal visible and at least 64 pixels of margin on every edge. Use orthographic projection, no foreshortening or tilt. Retain the reference's high-definition pixel clusters, cool steel, dark green, warm gold, red-brown wood and vermilion/deep-red cloth under soft neutral light with clean edges. No characters, hands, scenery, text, labels, dimension lines, axes, borders, glow or motion trails.
```

## 双语负面约束

```text
右侧图直接镜像、复制右侧、宽正面枪刃、前视、后视、3/4透视、斜视、改变枪长或杆径、改变分段高度、红缨根移到近侧、换风吹姿态、双红缨、第二尾端、新增握套或环扣、盘蛇、悬空零件、粗绳、实体红雕塑、人物、手、场景、文字、标签、多视图、裁断、倾斜、残影、发光。

Directly mirrored RIGHT image, copied right view, broad front blade, front view, back view, three-quarter perspective, oblique view, changed length or pole diameter, changed component heights, root moved to near side, new wind-blown pose, paired tassels, extra terminal, new grip or clasp, coiled serpent, floating components, thick cord, solid red sculpture, characters, hands, scenery, text, labels, multiple views, cropping, tilt, motion trails, glow.
```

## 参数与交付

- 模型：`gpt-image-2.5-sunburst`；图像编辑接口沿用已有成功任务。
- 实际已提交一次左侧生成：1024×1536、max质量、high参考保真、白底不透明PNG、n=1，HTTP 200。
- 拟用输出：`C:\Users\Administrator\Pictures\gptGen\stab_assembled_left_view_v06\`。
- 新左侧原图保留。交付版只做**全图等比缩放与白底留边**，按枪尖／枪尾高度对齐V5，不单独拉伸枪头、枪杆或红缨。记录具体倍率与位移，不用形变掩盖造型差异。
- 项目交付：`StabWeaponArtSource/12_AssembledFourViewV06/`，含 `views/front.png`、`left.png`、`right.png`、`back.png`，均384×1024。
- 无字合图：`assembled_four_view_plain.png`，1536×1024，顺序**前／左／右／后**。
- 中文标注版：`assembled_four_view_annotated.png`，视图标题与观察方向由本地排版加入。
- 前／右／后三张复制V5原单图，不重画、不缩放。新左侧对齐偏差、隐藏结构和最终美术验收如实记录。
- 执行规格：`StabWeaponArtSource/00_Index/assembled_four_view_v06_specification.json`。

## 验证

已验证PNG解码、四张单图与无字/中文合图区域逐像素一致、前右后与V5逐字节一致。左侧仅整体等比缩放0.6825396825396826并白底留边，无镜像、旋转或独立拉伸；左／右枪尖与枪尾高度差均0px，四图主体完整。记录见交付目录 `delivery_verification.json`。

左侧补图红缨展开较宽，远侧根部遮挡和分段结构未严格对应，需用户美术验收；对齐不代表3D网格或认证精确投影。原始左侧图片和一次生成的trace已归档，中断恢复未再次生成。
