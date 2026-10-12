---
id: kd_135b8dde-aa84-4d4e-8f3f-65609b953345
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 带红缨长枪：完整装配五视图 V05

## 1. 当前要求与制作边界

用户当前要求制作一把**带红缨的完整长枪五视图**，即同一装配物体的正面、右侧面、背面、顶视、底视。沿用此前中文解释要求，主交付为一张无字五视图合图、一张中文说明板和五张独立视图。

用户已明确批准本版一次生成，实际提交一次并HTTP 200返回完整装配五视图。主交付在 `StabWeaponArtSource/11_AssembledFiveViewV05/`：1536×1024无字合图、2304×1760中文说明板和五张独立图。正／侧／背装配基本可读，顶底深宽比约0.72／0.83，而侧／正约0.35，几何核对未通过，新增端视保留候选，待用户美术验收。

本轮不涉及Unity运行时、场景、Prefab或模型接入。Editor当前为Play Mode，文件准备不依赖或改变它。

## 2. 当前已具备的完整装配参考

本地三视图装配草稿：

`H:\Project\threeKingdomSlayer\threeKingdomSlayer\StabWeaponArtSource\10_AssembledFiveViewPreparation\assembled_three_view_reference_v05.png`

SHA-256：`f4fd3575125c8942514c99540658eb5669b64c408ffd89e0e3468a3585d472ee`。

来源：

- 枪体沿用已验收的 `StabWeaponArtSource/04_BodyV02/stab_body_orthographic_v02.png`，保持全部原像素，不擅自把枪头改回旧母图。
- 红缨沿用 `StabWeaponArtSource/05_TasselV02/stab_tassel_orthographic_v02.png` 的正／侧／背，使用共同缩放比例0.34375；红缨纵向下垂约为整枪长度的0.32，与完整母图的比例关系接近。
- 根部放在金绿连接件上部，正面右侧、背面相反侧、右侧图靠近杆轴，布片允许正常遮挡连接件。
- 红缨背景仅用 `min(R,G,B)>=245` 阈值清除，未重画布料。具体二维缩放与根部映射见同目录 `assembly_layout.json`。

**这只是统一比例和挂点的二维装配草稿，不是已确认模型的正交渲染。** 侧背连接面仍需生成时补定；旧枪体V02已验收的身份优先，红缨V02尚无单独验收记录。

## 3. 参考职责

参考A：上述完整装配草稿，负责同一长枪的造型、颜色、比例、单根红缨、连接根及三个绕轴视角。不可把红缨单体按全尺寸另贴，不额外放大红缨。

参考B：`StabWeaponArtSource/08_TopBottomV04Preparation/end_on_camera_guide.png`，SHA-256 `f7bb3c56c54a0da6b366cab76734973ff59b2e35751a4d68fa4f6bbf512b2ed0`。它只解释顶／底沿±Y观察，Y长度消失，仅保留X宽和Z深的摄像机方向；灰块、文字和箭头都不是武器结构，不进输出。

旧V03斜视、V04厚度未通过的顶底图不上传，不当作造型依据。旧完整母图只用于人工核对红缨挂点和相对长度，不上传为第三个竞争参考。

## 4. 几何与观察约束

- Y轴向枪尖，红缨根部也靠+Y；X是正面右，Z朝正面观察者。
- 正面从+Z沿−Z看；右侧从+X沿−X看；背面从−Z沿+Z看。
- 顶视从枪尖上方沿−Y看，画面右+X、上−Z；底视从枪尾下方沿+Y看，画面右+X、上+Z。
- 顶底投影严格只显示X–Z占据轮廓，整枪Y长度沿视线压缩。枪杆不能成为可见长条，红缨不能再次显示完整根到尾的纵向长度。
- 同一不透明装配物体的顶底外轮廓应在画面上下镜像后对应；可见材质和遮挡顺序不同。
- 枪体与红缨不能在每个视角重新摆位或换风吹姿态。观察到侧面／背面只是同一静止物体绕轴观看。
- 二维装配草稿近似可见主体宽：正面194px、右侧69px、背面189px；Z深／X宽约0.36。新顶底图的深宽比目标约0.32–0.40，用于防止再次生成厚圆团。这是图像比例约束，不是宣称物理尺寸已确定。

## 5. 一张五视图的排版

生成一张1536×1024无字白底PNG：

- 左侧三个384×1024区域（x=0–383、384–767、768–1151）：依次正面、右侧、背面。三把完整竖直长枪，同尺度、枪尖和枪尾高度一致，上下至少48px余量。
- 右侧384×1024栏：上半x=1152–1535、y=0–511为顶视，下半y=512–1023为底视。两幅端视同尺度、保持原厚度关系，不自行扩展成圆形团块。
- 五格各含一把枪体加同一根红缨的完整装配投影；不画独立红缨、独立零件或装配展开图。
- 顶底可以使用相同倍率做端向展示，必须维持深宽比；长幅三图共同尺度。
- 所有标签与解释由本地添加，不让图像模型生成中文。

## 6. 中文提示词

```text
制作一张用于游戏3D建模的“同一把带单根红缨长枪”完整装配正交五视图。参考A是已经统一比例和挂点的完整长枪三视图，锁定枪刃、中央刃脊、金绿交叉枪套、银色连接段、红棕直圆杆、金色几何尾鐏，以及接在枪套上部右侧的一根朱红布缨。参考B只解释摄像机端向观察，灰块、箭头、文字不是目标，不复制。不要使用其他武器或红缨造型。

五个视图都是同一件固定装配物体。只有一把枪和一根红缨，红缨一个收束根部、一片连续薄布、一个主尖尾。保持参考A的红缨长度、宽窄变化和向外展开后回弯的静止姿态；下垂高度约为整枪长度的32%，不能独立放大、缩短、拆开、换风吹姿态。根部实际连接在金绿枪套上部；正面在右，背面转到相反侧，右侧显示薄边、有限前后起伏和正确遮挡。枪头、枪套、枪杆、尾鐏同轴，红缨不悬空。不新增握套、环扣、盘蛇、双缨或零件。

输出1536×1024纯白不透明PNG。左侧三个等宽384×1024区域依次为正面、右侧、背面，各画一把完整竖直带红缨长枪，同一尺度、枪尖枪尾及红缨根部高度对齐，上下至少48像素余量，主体不跨格、不裁断。右侧384×1024区域分成上下两个384×512格，上为顶视，下为底视，各只有一个完整装配端向投影。总共恰好五个物体投影，不增加局部图。

Y轴从枪尾指向枪尖，X是正面右侧，Z朝正面观察者。正面沿−Z看，右侧沿−X看，背面沿+Z看。顶视从枪尖上方沿−Y看，底视从枪尾下方沿+Y看。顶视和底视只投影X宽与Z深，整把枪和红缨的Y长度完全沿视线消失；不能显示长枪杆或完整纵长红缨，不能画斜俯视、3/4透视或缩短的长枪。顶视画面右+X、上−Z，底视画面右+X、上+Z。同一装配顶底外轮廓在画面上下镜像后对应，只有可见折面与遮挡先后不同。严格正常遮挡，不剖切，不X光显示隐藏件，不爆炸展开。

参考A的整枪装配正面宽与右侧深度之比约为X:Z=1:0.36。顶底端向的Z深／X宽维持约0.32–0.40，不扩大成厚圆团或多叶花。红缨是带浅折面和有限深度的薄布片，不是粗绳、实心雕塑、分叉流苏。顶底可以共同放大以阅读，但不能改变深宽比例。若表面被更近的枪体或布面遮挡，按真实遮挡不画出，不为展示所有零件重新摆物体。

保持高清像素色簇、清楚实体分面、冷银钢刃、深绿与暖金连接件、红棕木杆、朱红主布面、深红折面及少量橙红高光。中性光照，清晰完整轮廓。不要文字、标签、尺寸线、坐标轴、分隔线、边框、人物、手、场景、发光、残影。标签和中文解释随后本地排版添加。
```

## 7. English prompt

```text
Create one assembled orthographic FIVE-VIEW sheet of the SAME spear with ONE attached red tassel for game 3D modeling. Reference A is the assembled three-view sheet with unified proportions and attachment placement. It locks the blade, central ridge, gold-and-dark-green crossed socket, silver connection, straight round red-brown pole, angular gold butt, and ONE vermilion cloth tassel attached at the upper-right socket. Reference B explains ONLY the end-on camera direction; its grey block, arrows and words are not the target and must not be copied. Use no other spear or tassel design.

All five views show ONE fixed assembled object: one spear, one tassel with one gathered root, one continuous thin cloth surface and one main tapered terminal. Preserve A's tassel length, width variation and static outward-then-returning curve, with a vertical drop about 32 percent of total spear length. Never enlarge it independently, shorten it, detach it or invent a different wind-blown pose. The root physically connects to the upper gold-green socket: right in front, opposite side in back; the right-side view shows a thin edge, limited depth variation and coherent occlusion. Head, socket, pole and butt are coaxial; the tassel root is not floating. No new grip, clasp, coiled serpent, paired tassels or extra components.

Output one opaque pure-white 1536x1024 PNG. The left three equal 384x1024 regions contain front, right-side and back, one complete upright tasselled spear each, at identical scale with aligned spear tip, butt and tassel-root heights. Leave at least 48 pixels above and below; no clipping or cross-region subjects. The right 384x1024 column contains two 384x512 cells: TOP above, BOTTOM below, one complete assembled end-on projection each. Exactly FIVE projections in total, no detail insets.

Y runs from butt to tip, X is screen-right in front and Z points toward the front observer. Front looks along -Z, right side along -X and back along +Z. TOP looks from above the tip along -Y; BOTTOM from below the butt along +Y. Top and bottom project ONLY X width and Z depth; the entire spear/tassel Y length collapses along the sight line. No long pole, full-length hanging tassel, oblique overhead, three-quarter perspective or foreshortened spear. Screen-right is +X in both; screen-up is -Z in top and +Z in bottom. The external top/bottom silhouettes correspond after vertical screen reflection; only visible facets and occlusion order differ. Use normal occlusion, no cutaway, x-ray or exploded assembly.

A's full assembled front width and right-side depth approximately give X:Z=1:0.36. Keep end-on Z-depth/X-width around 0.32-0.40, never a thick round cluster or multi-petal flower. The tassel is thin cloth with shallow folds and limited depth, not rope, solid sculpture or branching streamers. The two end views may share one enlargement for readability, but never change the depth/width ratio. Surfaces hidden by closer weapon or cloth parts stay occluded; do not rearrange the object to expose every component.

Preserve high-definition pixel clusters and readable physical planes: cool silver blade, dark green and warm gold fittings, red-brown wood, vermilion cloth, deep-red folds and restrained orange-red highlights. Use neutral lighting and complete clean silhouettes. No text, labels, dimension lines, axes, dividers, borders, characters, hands, scenery, glow or motion trails. View labels and Chinese explanations will be added locally afterward.
```

## 8. 负面词 / Negative constraints

```text
五种不同武器、两根红缨、悬空根部、拆分枪体与红缨、独立放大红缨、每视角换姿态、粗绳、实心红雕塑、多叶花、厚圆团、分叉、第二尾端、新增零件或握套、盘蛇、长枪杆出现在顶底视、完整纵长红缨出现在顶底视、斜俯视、3/4透视、透视缩短、横放正面、二维旋转伪装顶底、剖切、X光、爆炸图、局部放大、裁断、主体跨格、不同尺度、人物、手、文字、标签、尺寸线、轴、边框、场景、光效、残影。

Five different weapons, paired tassels, floating root, separated weapon and cloth, independently enlarged tassel, changed pose between views, thick cord, solid red sculpture, multi-petal flower, thick round cluster, branching, second terminal, new components or grip, coiled serpent, long pole in top/bottom, full-length hanging tassel in top/bottom, oblique overhead, three-quarter perspective, foreshortening, front laid sideways, fake end views by 2D rotation, cutaway, x-ray, exploded diagram, detail inset, clipping, cross-region subjects, mismatched scale, characters, hands, text, labels, dimension lines, axes, borders, scenery, glow, motion trails.
```

## 9. 参数与交付

- `gpt-image-2.5-sunburst`；`https://api.muskapis.com/v1/images/edits`。
- `size=1536x1024`、`quality=max`、`input_fidelity=high`、`background=opaque`、`output_format=png`、`n=1`。
- 用户批准后已执行一次任务，HTTP 200返回一张五视图纯图；未追加生成、未换模型。
- 拟用输出目录：`C:\Users\Administrator\Pictures\gptGen\stab_assembled_five_view_v05\`。
- 主图文件：`stab_assembled_five_view_v05.png`。
- 本地规格：`StabWeaponArtSource/00_Index/assembled_five_view_v05_specification.json`。
- 图格固定裁切：正／侧／背384×1024，顶／底384×512。裁切无独立缩放；中文说明板使用本地字体添加。
- 实际项目交付：`StabWeaponArtSource/11_AssembledFiveViewV05/`，保留无字原图、中文板、五张独立图和trace。五张完整主体、裁切及中文板图像区域已逐像素校验。
- 实际裁切以留白为界：正面 `[0,0,384,1024]`、右侧 `[352,0,384,1024]`、背面 `[704,0,384,1024]`、顶 `[1088,192,448,384]`、底 `[1088,560,448,384]`。原图略跨预定格，采用实际留白提取，未独立缩放或改形。
- 原图SHA-256：`15fddd686807898ae93547d580426d80e286ea9e876614a528d5ff05ee13c154`。技术和美术检查在交付目录 `delivery_verification.json`。

## 10. 中文说明与验收重点

| 视图 | 板上说明 |
|---|---|
| 正面 | 检查已验收枪体身份、单根红缨、上部右侧挂点及整枪比例。 |
| 右侧 | 检查刃脊厚度、圆杆深度、薄布前后起伏与根部真实连接；不要重复宽正面。 |
| 背面 | 红缨位于正面相反侧，保持同一根部、长度和静止形状；连接件绕轴连续。 |
| 顶视 | 沿−Y从枪尖看向枪尾，只留下装配X–Z投影；枪杆与布面按正常遮挡，不能成为长条。 |
| 底视 | 沿+Y从枪尾看向枪尖，同一装配的端向反面；不展开被遮挡零件，不改变布缨姿态。 |

验收分开记录：PNG与裁切技术、枪体身份、红缨装配、五视图观察方向、图像深宽比对应、最终用户美术验收。生成图不宣称为模型／骨链或精确CAD渲染。若顶底仍未达标，如实交付候选和证据，不标为合格工程五视图。
