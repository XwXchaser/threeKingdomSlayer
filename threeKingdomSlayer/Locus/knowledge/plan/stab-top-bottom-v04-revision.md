---
id: kd_8a808827-0065-40f6-8abb-353440d77aaa
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Stab 顶／底视图 V04 修订：生成前审批稿

## 1. V03 结果与本轮范围

用户已批准 V03 长枪、红缨各一次生成，两组均返回 HTTP 200 并下载成功，但**都没有生成所需的顶／底正交投影**：

- 枪体变成缩短的斜视图，仍能看到串行排列的枪头、枪套、长枪杆和枪尾。
- 红缨变成另外两幅长幅正背面，根部到尾端的Y长度仍在画面中。

失败素材和18份原图／追溯文件已逐字节归档于 `StabWeaponArtSource/07_TopBottomV03_Failed/`。两张中文说明板保留在 `StabWeaponArtSource/06_FiveViewPreparation/`，新增端视图格仍写“待生成”。不能把失败图片填入顶底格或当作完成的五视图。

用户明确批准V04两组修订后，已各执行一次，均HTTP 200且PNG技术通过。V04解决了“仍显示完整Y长轴”的构图问题，但端向深宽比与原三视图不一致，几何核对未通过。已交付带限制标注的中文评审板、无字版和独立视图，不能称为合格建模五视图。不自动追加其他收费任务。

## 2. 修订依据

Y轴从枪尾指向枪尖／红缨尾端指向固定根。顶视沿−Y、底视沿+Y，投影只保留X和Z：Y长度不能显示成长轴。长枪端视应为紧凑的轮廓；红缨端视呈现横向展开和浅深度叠合。

对同一完整不透明物体的相反轴向正交观察，外部占据轮廓应基本相同，依约定在画面上下镜像；只是可见表面和遮挡先后不同。顶／底不应生成两种形状或姿态。并非将已画出的正面图二维旋转。

未知深度只从现有三视图推定；生成图不保证精确CAD对应。严谨五视图最终应来自同一确认模型的正交渲染。

## 3. 两张输入的职责

每组参考A仍为对应V02三视图：

- 枪体：`StabWeaponArtSource/04_BodyV02/stab_body_orthographic_v02.png`，SHA-256 `c108a368fbe8a4bc028c214705d243eafa84ffd9b4f66f330242b87d1d7a130b`。
- 红缨：`StabWeaponArtSource/05_TasselV02/stab_tassel_orthographic_v02.png`，SHA-256 `d5d59dcbad2fb4674c64a6f4be3e167f2ad488852c382d9e4bfa2cd9743373a7`。

新增共用参考B：`StabWeaponArtSource/08_TopBottomV04Preparation/end_on_camera_guide.png`。

B只解释竖直长物体沿±Y投影后，Y长度消失、X宽度和Z深度留下的观察方向。图中的灰色长方体、箭头和文字都不是武器形状、材质或输出内容。其尺寸比例不用于重新设计枪体或红缨。

旧V03失败图片不作为生成参考，不重画原三视图，不把已验收枪刃改回旧母图。

## 4. 枪体 V04 完整提示词

### 中文

```text
这是沿长枪轴线观察的两个端向正交图。最重要：枪身长度Y完全沿摄像机视线消失，画面只保留X宽度和Z厚度。不能出现长红棕枪杆，不能把枪头、枪套、枪杆、枪尾从上到下串成一把缩短的枪；不能出现斜视、俯视透视或长幅正面。

参考A的三视图只决定同一把枪的造型、厚度和材质：冷银双刃片和中央脊、金绿枪套、圆杆、金色尾鐏。参考B只决定摄像机沿±Y的方向和长度压缩原则，灰色块、文字、箭头均不属于目标，不复制。

左区TOP：从枪尖上方沿−Y正交看向枪尾。右区BOTTOM：从枪尾下方沿+Y正交看向枪尖。顶视画面右为+X、上为−Z；底视画面右为+X、上为+Z。两图都是同一完整物体的X–Z投影，轮廓紧凑。侧面和正面的枪刃都不得重复显示；枪刃在端向中只贡献沿X的宽度和沿Z的脊厚，不出现沿Y的尖叶形全长。枪杆投影为圆形区域并被较近零件正常遮挡，不表现成长条。

用正常遮挡表现银色刃面、金绿连接件和金色尾件可见的端向表面，所有零件同轴重叠，不分拆，不剖切，不透视隐藏零件。同一物体的顶底外轮廓在屏幕上下镜像后应对应，不能变成两个尺寸或两种设计；两侧可见材质和遮挡面可以不同。不新增孔洞、装饰或握套。

生成1536×1024纯白不透明PNG，左TOP、右BOTTOM两个768×1024等宽无边框区域，每区一个投影。两个投影中心对齐、同尺度、可共同放大，四边至少64像素边距。沿用A的高清像素色簇、中性光照和金属体积。最终图不要文字、箭头、标线、轴、边框、人物、红缨或背景；中文解释随后本地添加。
```

### English

```text
Create TWO ORTHOGRAPHIC END-ON views looking directly ALONG the spear's long axis. PRIMARY RULE: the entire Y length collapses into the camera line of sight; only X width and Z depth remain in the picture. No long red-brown pole. Do not arrange head, socket, pole and butt sequentially down the image as a shortened spear. No oblique overhead, perspective or tall front view.

Reference A's three views define the SAME spear's identity, thickness and materials: cool silver double-edged blade and central ridge, gold-green socket, round pole, angular gold butt. Reference B defines ONLY the +/-Y camera direction and length-collapse principle. Its grey block, arrows and words are not target geometry and must not be copied.

Left TOP looks orthographically from above the tip along -Y toward the butt. Right BOTTOM looks from below the butt along +Y toward the tip. Screen-right is +X in both; screen-up is -Z for top and +Z for bottom. Both are compact X-Z projections of the SAME complete object. Do not repeat the front or side blade. The blade contributes only its X width and Z ridge thickness end-on, never its leaf-shaped Y length. The pole projects as a circular region normally occluded by closer components, never a long strip.

Show only visible silver, gold-green and gold end surfaces under normal occlusion, with all components overlapping coaxially. Do not separate, cut away or x-ray parts. Opposite orthographic views of the same opaque object have corresponding external silhouettes after a vertical screen reflection: no different sizes or designs. Visible surfaces, materials and occlusion order may differ. Add no holes, ornaments or grip.

Output one opaque pure-white 1536x1024 PNG with two equal unbordered 768x1024 regions: TOP left, BOTTOM right, one projection each. Match center and scale; both may share one enlargement. Leave at least 64 pixels on every edge. Retain A's high-definition pixel clusters, neutral lighting and metal volume. No text, arrows, lines, axes, borders, characters, tassels or scenery. Chinese captions will be added locally afterward.
```

## 5. 红缨 V04 完整提示词

### 中文

```text
这是沿红缨根部到尾端竖轴观察的两个端向正交图。最重要：Y长度沿摄像机视线完全压缩；画面只保留X横向展开和Z浅深度。不要画根部在上、长尖尾在下的竖长红缨，不重复正面、背面或侧面，不把正面图旋转成横长图，不展平或改造布片。

参考A三视图锁定同一根朱红布缨的单根收束根、连续薄布面、主尖尾、外展回弯姿态和折面。参考B只解释摄像机沿±Y、Y长度消失的观察原则；灰色块、箭头和文字不是目标造型，不复制。仅显示布缨，不出现枪体或金属件。

左TOP从固定根上方沿−Y正交看向尾端；右BOTTOM从尾端下方沿+Y正交看向固定根。画面右均为+X，顶视上为−Z，底视上为+Z。保持原静止姿态，让根部、折面和尾端沿视线叠合，呈现布片实际在X–Z平面的紧凑横向投影。根部和尾端可以有X偏移，但Y不能表现成画面长轴。

用正常遮挡显示连续薄布的浅褶、边缘和可见面；不是粗绳、实体雕塑或分叉流苏。两个投影共同的占据轮廓在屏幕上下镜像后对应，只改变可见折面和遮挡先后，不换风吹姿态、不改长度或布厚。不要新增尾端、环扣或结构。采用朱红面、深红折面和克制橙红高光，沿用A的高清像素色簇。

生成1536×1024纯白不透明PNG，左TOP、右BOTTOM两个768×1024等宽无边框区域，每区一个完整端向投影。同尺度、根部基准可比较、可以成对放大，四边至少64像素余量。中性光照，轮廓完整。不要文字、箭头、标线、轴、边框、人物、枪体、金属、光效或背景；中文解释随后本地添加。
```

### English

```text
Create TWO ORTHOGRAPHIC END-ON views looking directly along the tassel's vertical root-to-terminal axis. PRIMARY RULE: its Y length fully collapses along the camera sight line; only X lateral spread and shallow Z depth remain. Do not draw a tall tassel with root above and long pointed tail below. Do not repeat front, back or side views, rotate a front drawing horizontally, flatten or redesign the cloth.

Reference A's three views lock the SAME vermilion tassel: one gathered root, one continuous thin cloth surface, one main terminal, its outward-returning static pose and folds. Reference B explains ONLY the +/-Y camera and the loss of Y length. Its grey block, arrows and words are not target design and must not be copied. Show only the tassel, no weapon or metal parts.

Left TOP looks orthographically from above the root along -Y toward the terminal. Right BOTTOM looks from below the terminal along +Y toward the root. Screen-right is +X in both; screen-up is -Z for top and +Z for bottom. Preserve the static pose and overlap root, folds and terminal along the sight line to form the compact lateral X-Z footprint of the cloth. Root and terminal may differ in X, but Y cannot appear as the image long axis.

Use normal occlusion to show shallow folds, thin edges and visible cloth surfaces, not a thick rope, solid sculpture or branching streamers. The two occupied outer silhouettes correspond after a vertical screen reflection; only visible folds and occlusion order change. No different wind-blown poses, length or thickness. Add no terminal, clasp or structure. Use vermilion planes, deep-red folds and restrained orange-red highlights with A's high-definition pixel clusters.

Output one opaque pure-white 1536x1024 PNG with two equal unbordered 768x1024 regions: TOP left, BOTTOM right, exactly one complete end-on view each. Match scale and comparable root datum; both may share one enlargement. Leave at least 64 pixels on every edge. Use neutral light and complete clean silhouettes. No text, arrows, lines, axes, borders, characters, weapon, metal, glow or scenery. Chinese captions will be added locally afterward.
```

## 6. 双语负面约束

```text
竖长正面、根在上尾在下的全长红缨、长枪杆、枪头枪套枪杆枪尾顺序排列、斜俯视、3/4透视、透视缩短、横放正面、二维旋转、剖切、爆炸图、X光隐藏面、不同姿态、不同轮廓尺寸、额外物体、复制方向图灰色块或箭头、文字、标签、轴、线、边框、人物、场景、阴影、光效。

Tall front view, full-length tassel with root above and terminal below, long shaft, head/socket/pole/butt arranged sequentially, oblique overhead, three-quarter perspective, foreshortened perspective, front laid sideways, 2D rotation, cutaway, exploded diagram, x-ray hidden surfaces, changed pose or silhouette size, extra objects, copied grey guide block or arrows, text, labels, axes, lines, borders, characters, scenery, cast shadow, glow.
```

枪体额外禁止红缨／布料，红缨额外禁止任何枪体／金属件。

## 7. 参数与交付

- 参数沿用：`gpt-image-2.5-sunburst`、1536×1024、max、high参考保真、白底不透明PNG、n=1。
- 共两次新增生成，各任务一次；不会自动重试或换模型。
- 新目录：`C:\Users\Administrator\Pictures\gptGen\stab_body_top_bottom_v04\`、`C:\Users\Administrator\Pictures\gptGen\stab_tassel_top_bottom_v04\`。
- 完整修订规格：`StabWeaponArtSource/00_Index/top_bottom_v04_specification.json`。
- 已交付 `StabWeaponArtSource/09_FiveViewV04/`：两张2560×1760中文评审板、两张无字板及十张独立视图；原三视图保持原像素。新增顶底仅作几何未通过的候选。
- 端向构图条件已落实，但单纯Y长轴消失不足以证明几何正确：枪体端视深宽比约0.92，而侧／正面比约0.56；红缨端视约1.57，而侧／正面约0.41。该图像包围盒测量只用于揭示结构比例不一致，不是实体尺寸。
- 新增视图共4张候选，建模几何通过0张。中文板直接标注候选、几何未通过和厚度以原侧视为准；最终工程端视仍需同一确认模型的正交渲染。
