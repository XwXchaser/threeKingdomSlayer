---
id: kd_3e71781d-686e-40f6-80e2-a1f1eebaf529
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# Stab 新画风长枪：三视图进度与制作交接

## 1. 当前结论与本轮边界

**最新交付与限制**：用户需要前／左／右／后四张单图及合图。V06单次补左侧已完成，交付 `StabWeaponArtSource/12_AssembledFourViewV06/`：四张384×1024单图、1536×1024无字合图、1856×1440中文标注版及trace。前右后逐字节沿用V5，左侧整体等比对齐，左／右枪尖枪尾高度差0px；所有单图与合图区域逐像素一致。左侧红缨展开和远侧根部遮挡仍待验收。当前入口 `plan/stab-assembled-four-view-v06.md`，校验在交付目录 `delivery_verification.json`。V5五视图及此前评审资料全部保留。

当前已找到并核对完整造型母图、枪体三视图、单根红飘带三视图、生成请求和导出记录。**首版三视图 V01 已生成并导出六张独立 PNG；可用记录仍写 `user_acceptance_pending: true`，不能据此宣称最终美术验收或建模验收通过。**

此前 `plan/bugs-and-requests.md` 的 REQ-001 写“概念图尚未制作 / 提示词尚未提交”，已经落后于实际文件。`Library/Locus/tmp/stab_orthographic_refs_v01/generation_spec.json` 与输出目录中的 `approved_specification.json` 也保留了生成前的 `prepared_for_user_approval_not_submitted`；应以真实成图、两次 HTTP 200 回执和导出记录判断执行进度，保留历史原件而不篡改。

枪体V02和红缨V02各提交一次；分组件顶底V03与V04各两次；完整装配V05一次；V06补左侧一次。所有版本分别获用户批准，无自动追加。V06生成在中断前完成，恢复仅做本地对齐与合图，没有再次生成。没有建模、绑定、动画或Unity工程改动。

## 2. 可以直接打开的素材

### 项目内归档（本轮新增，优先使用）

绝对目录：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\StabWeaponArtSource\`

该目录位于 `Assets/` 外，Unity 不会导入。17 个来源文件为逐字节一致的副本，总计 5,108,780 字节。来源和 SHA-256 见 `StabWeaponArtSource/00_Index/source_manifest.json`。本轮未自动提交、推送或配置 Git 忽略。

| 文件 / 目录 | 用途和状态 |
|---|---|
| `StabWeaponArtSource/01_Reference/assembly_reference.png` | 当前下游选用的完整造型母图，1280×1280 RGB PNG；连接位置与枪体/飘带比例依据 |
| `StabWeaponArtSource/02_OrthographicV01/body/front.png` | 无布料枪体正面，512×1024 RGB PNG，V01 待最终验收 |
| `StabWeaponArtSource/02_OrthographicV01/body/right.png` | 无布料枪体右侧面，同上 |
| `StabWeaponArtSource/02_OrthographicV01/body/back.png` | 无布料枪体背面，同上 |
| `StabWeaponArtSource/02_OrthographicV01/ribbon/front.png` | 单根飘带正面，512×1024 RGB PNG，V01 待最终验收 |
| `StabWeaponArtSource/02_OrthographicV01/ribbon/right.png` | 单根飘带右侧面，同上 |
| `StabWeaponArtSource/02_OrthographicV01/ribbon/back.png` | 单根飘带背面，同上 |
| `StabWeaponArtSource/03_TraceV01/body/body_board.png` | 枪体原始三视图板，1536×1024；仅供整体对照和追溯 |
| `StabWeaponArtSource/03_TraceV01/ribbon/ribbon_board.png` | 飘带原始三视图板，同上 |
| `StabWeaponArtSource/03_TraceV01/approved_specification.json` | 历史中英文提示词、参考图职责、参数与导出规则；保留原始旧状态 |
| `StabWeaponArtSource/03_TraceV01/body/`、`StabWeaponArtSource/03_TraceV01/ribbon/` | 两次生成的 request / status / verification，历史记录原样保留 |
| `StabWeaponArtSource/03_TraceV01/export_verification.json` | 历史六图导出校验与已披露缺陷 |
| `StabWeaponArtSource/00_Index/assembly_v02_specification.json` | 完整装配三视图 V02 历史草案；用户暂缓飘带后停用，未提交 |
| `StabWeaponArtSource/00_Index/body_v02_specification.json` | 刚性枪体三视图合图规格、实际结果哈希和生成状态；V02 已获用户验收 |
| `StabWeaponArtSource/00_Index/tassel_v02_specification.json` | 红缨V02已按用户批准生成，记录规格、输出哈希与待验收状态 |
| `StabWeaponArtSource/05_TasselV02/stab_tassel_orthographic_v02.png` | 红缨V02正/右侧/背面合图，1536×1024 RGB PNG；同目录trace为追溯资料 |
| `StabWeaponArtSource/07_TopBottomV03_Failed/` | 两组失败端视原图及18份追溯文件，不能作有效顶底图 |
| `StabWeaponArtSource/00_Index/top_bottom_v04_specification.json` | 双参考修订已批准并执行两次，评审板已交付，几何一致性未通过 |
| `StabWeaponArtSource/09_FiveViewV04/` | 两张中文评审板、两张无字板、十张视图、生成与像素验证记录；顶底只能作候选 |

### 原始生成目录（原件保留）

- 完整母图：`C:\Users\Administrator\Pictures\gptGen\stab_original_shape_restyle_v01_retry02\stab_original_shape_restyle_v01_retry02.png`
- V01 最终候选图片目录：`C:\Users\Administrator\Pictures\gptGen\stab_orthographic_refs_v01\body\` 与 `C:\Users\Administrator\Pictures\gptGen\stab_orthographic_refs_v01\ribbon\`，各含 `front.png`、`right.png`、`back.png`。
- 装配母图副本：`C:\Users\Administrator\Pictures\gptGen\stab_orthographic_refs_v01\assembly_reference.png`。
- 仅供预览/追溯：`C:\Users\Administrator\Pictures\gptGen\stab_orthographic_refs_v01\source_boards\`。
- 历史脚本：`Library/Locus/tmp/stab_orthographic_refs_v01/generate_boards.py`；旧任务输出目录已存在，脚本会拒绝重复生成。不要直接重跑旧任务。

母图 SHA-256：`6475df45a489976d0de95f042235d4080e1adf36c18c54f639dbd231ec46c9ae`。

## 3. 历史版本和真实进度

以下旧版本均在 `C:\Users\Administrator\Pictures\gptGen\` 下。表中的候选评价来自相应 `verification.json`，不替代用户最终验收。

| 版本 / 阶段 | 已有产物 | 结论 |
|---|---|---|
| 项目原型 `Assets/Sprites/zhangfei/stab.png` | 旧像素长枪、枪头、金绿连接件、红棕杆、枪尾 | 武器身份和旧技术接线参考；不能定义新画风 |
| `stab_no_serpent_v01/stab_no_serpent_v01.png` | 1280×1280 白底成图 | 已去掉绿色盘蛇，保留原身份；仍是较粗的旧像素表达、左右多处红缨 |
| `stab_restyle_v01/stab_restyle_v01.png` | 1024×1536 新画风候选 | 材质体积提升，但变成较通用枪型，新增明显折肩、皮革握套和多布带 |
| `stab_restyle_v02_short_tassels/stab_restyle_v02_short_tassels.png` | 1024×1536 修订图 | 布带更明确、更短，记录显示长度/展开宽度目标未达标 |
| `stab_restyle_v03_single_tassel/stab_restyle_v03_single_tassel.png` | 1024×1536 修订图 | 已去握套和握套箍、成为一根红带；旧“短束红缨”目标仍未达标 |
| `stab_original_shape_restyle_v01/`、`stab_original_shape_restyle_v01_retry01/` | 提示词、请求、`RemoteDisconnected` 错误记录 | 未交付图片，远端完成/计费结果未知；不是可用美术版本 |
| `stab_original_shape_restyle_v01_retry02/stab_original_shape_restyle_v01_retry02.png` | 1280×1280 当前母图 | 恢复尖叶形枪头、金绿交叉连接件、连续红棕杆、几何金尾与右侧单根飘带；被下游选为三视图唯一参考 |
| `stab_orthographic_refs_v01/` | 枪体与飘带两块 1536×1024 板、六张 512×1024 图 | 两次生成均返回 HTTP 200，已经导出并完成技术检查；最终用户验收未在可用记录中确认 |
| 刚性枪体 V02 合图 | `StabWeaponArtSource/04_BodyV02/stab_body_orthographic_v02.png`，1536×1024 RGB PNG | 已获用户明确验收；历史枪刃折肩和排版偏差记录保留，不再作为待验收阻塞 |
| 红缨 V02 合图 | `StabWeaponArtSource/05_TasselV02/stab_tassel_orthographic_v02.png`，1536×1024 RGB PNG | 用户批准后单次生成并技术校验，独立红缨完整；侧边距不足，待最终美术验收 |
| 完整装配三视图 | 尚无对应三视图产物；仅有正面完整母图 | 用户暂缓飘带，草案见 §6，不执行 |
| 长枪 3D 模型、UV、材质、骨链 | 相关素材目录及 Unity 长枪模型检索未找到交付 | 尚无证据证明完成 |
| Unity 3D 替换 | `Assets/Prefabs/Stab.prefab` 仍为 `SpriteRenderer + BoxCollider2D` | 尚未部署 3D 长枪 |

历史三视图请求记录记载上轮已批准两组提示词和生成参数；本轮刚性枪体 V02 则是在用户明确回复“请发起生成”后提交，实际只提交一次并返回 HTTP 200。

## 4. 新画风与 V01 / V02 检查

### 4.1 画风和造型来源

新画风依据 `design/art-style-guide.md`：高清像素色簇、大体块体积、冷钢灰、朱红布料、暖木与少量金属强调色。已验收 1011 Idle 是材质表达的具体参照，不能把角色、甲胄或剑柄复制到枪上。

当前母图已明显增加钢刃脊线、金属分面、杆体体积和红布折面。金色连接件仍较亮、轮廓较硬，最终是否足够贴合 1011 属于需要用户判断的美术项；不要在后续三视图任务中擅自删金绿件或改配色。

母图的新轮廓重点：一杆直枪、尖叶形冷钢刃、金绿交叉连接件、短银色连接段、连续红棕杆、金色几何尾、一根向右外展再弯回的朱红飘带；没有独立皮革把手，没有盘蛇。

### 4.2 本轮重新验证的技术事实

- 本地逐张解码两块源板、六张导出图，PNG 签名、各块 CRC、IDAT 解压长度均正确，格式为 8-bit RGB，无 Alpha。
- 六张图均为 512×1024；与各自 1536×1024 源板的左/中/右等宽区域逐像素一致，没有单图独立缩放或移动。
- 完整母图副本与选定来源逐字节一致，SHA-256 与历史生成脚本锁定值一致。
- 以 `min(R,G,B) < 220` 作为可复查的主体包围盒近似阈值，枪体三视图顶部/底部均为 y=39/972，上下余量 39/51px。飘带分别为 y=94/893、90/900、92/902，顶部相差 4px、底部相差 9px。该阈值用于裁切检查，不是实体几何测量。
- 未检测到主体跨出各自裁切区；技术导出完成。
- **V02 合图**：`C:\Users\Administrator\Pictures\gptGen\stab_body_orthographic_v02\stab_body_orthographic_v02.png`，项目归档 `StabWeaponArtSource/04_BodyV02/stab_body_orthographic_v02.png`，1536×1024 RGB PNG，HTTP 200，SHA-256 `c108a368fbe8a4bc028c214705d243eafa84ffd9b4f66f330242b87d1d7a130b`；全 PNG chunk CRC 与 IDAT 解码通过。
- V02 近似可见主体包围盒（阈值 `min(R,G,B)<220`，局部坐标）：正面 `[245,25,352,980]`、右侧 `[226,24,285,979]`、背面 `[158,24,265,980]`；枪尖高度差 1px、尾鐏高度差 1px，三者均未出各自区域。
- V02 限制：正/背面枪刃中段折肩仍比当前母图明显；正/背面长轴分别偏离区域中心约 +42/-44px；顶部余量约 24–25px，未达到原设定的32px目标。没有自动重生成或后处理缩放/重定位。

### 4.3 距离建模定稿仍存在的缺口

1. **枪刃母图对应（历史差异，V02 已验收）**：V01 与 V02 正/背枪刃中段的收腰和折肩都比母图明显；该差异保留作追溯。用户已明确验收枪体 V02，后续枪体建模以该验收版为准，不再擅自改回旧母图轮廓。
2. **红带投影对应**：侧面已变窄，背面转到相反侧；仍有 4/9px 的根/尾高度差以及折面变化，只能作为补定侧背的候选。准确正交投影需从同一确认模型渲染。
3. **跨组件比例**：枪体与飘带分别占据接近整个画布高度，是独立放大的参考。不能因为都是 512×1024 就按同像素比例装配。以完整母图为唯一跨组件比例和挂点依据。
4. **挂点信息**：母图中红根贴在上部连接件右侧；当前只有二维位置，绕轴角、前后深度和根部实际连接面未明确。独立红带图不能证明红根已连到枪体。
5. **模型形体**：枪头厚度、刃脊截面、金绿交叉件绕轴的连续结构、尾件深度仍含生成补定。图像上的像素台阶应保留在绘制/材质语言，不逐像素建成实体锯齿。
6. **审批记录冲突**：母图 `verification.json` 保留最终验收待定，下游规格称其“已认可”。可确认的是该母图已经作为获得生成批准的 V01 输入；最终三视图是否通过仍需用户结论。

旧“红缨长度必须为刃长 45%–50%”来自早期修订提示词。后来的 V01 已批准提示词明确要求“不缩短、不换造型”。本次准备沿用当前母图，不自动重新执行旧缩短指标；若用户重新提出短缨需求，作为新造型版本单独处理。

## 5. 后续制作顺序（待办，不在本轮自动实施）

1. 评审现有母图和 V01 六张图。若用户接受其造型和侧背补定，可直接作为建模参考，不重复生成相同六图。
2. 刚性枪体 V02 已验收；当前准备单根红缨 V02 三视图，按 `plan/stab-tassel-three-view-generation-prompts.md` 的双参考职责执行。§6 完整装配草案仍保留作历史，不是当前生成任务。
3. 图像验收同时记录：造型母图对应、枪刃折肩、红带挂点、根/尾对齐与冷钢/朱红/金绿材质。生成后的完整三视图仍可能不精确，不能把请求成功当成投影几何通过。
4. 造型确认后，建模交接使用一份整体模型；枪体与红带为可分离组件，确认长轴、枪尖、枪尾和红带根。后续只需同一模型的正交渲染即可获得真正对应的三视图。
5. 建模、绑定、动画和 Unity 接入需各自授权和验证。保持普通三枪突、指向突刺与上挑的命中时序、交接与单把可见武器语义。

建议建模轴向：Unity 接入层本地 +Y 从枪尾指向枪尖，+X 为正面图的右侧，+Z 为定义的正面朝向。Blender/FBX 坐标转换另行验证，不假定导出自动正确。轴向约定用于建模交接，不要求生成图绘出坐标轴。

## 6. 完整装配三视图 V02 历史草案（用户要求先不管飘带，已暂缓，未提交）

### 目标、参考和参数

- 目标：补充同一杆新画风长枪的完整装配正面、右侧、背面参照，保持母图造型、红带长度及枪体/红带比例。
- 唯一上传参考：`StabWeaponArtSource/01_Reference/assembly_reference.png`，负责武器身份、全部比例、红带正面姿态、固定根位置和现有材质表达；SHA-256 为 §2 的锁定值。
- V01 两块三视图板和 1011 Idle 仅供人工对照，不上传为竞争参考；不上传旧 `stab.png` 或速度形变 `stab_v13.png`。
- 拟用模型：`gpt-image-2.5-sunburst`，沿用 V01 的接口 `https://api.muskapis.com/v1/images/edits`。
- 拟用参数：`size=1536x1024`、`quality=max`、`input_fidelity=high`、`background=opaque`、`output_format=png`、`n=1`；一次图像编辑任务，纯白底，不要求透明。
- 新候选输出目录：`C:\Users\Administrator\Pictures\gptGen\stab_assembly_orthographic_v02\`。目录需不存在；不得覆盖 V01。
- 导出：源板另存 `source_boards/assembly_board.png`，按三等宽区域不缩放裁成 `assembly/front.png`、`assembly/right.png`、`assembly/back.png`，均 512×1024；校验后可归档到 `StabWeaponArtSource/04_AssemblyV02/`。
- 传输/重试：沿用直接 HTTPS；保存请求、回执和校验。未知完成结果时不自动重提或换模型。
- 费用：未询价、未产生本轮生成费用；草案未获本轮生成批准，不调用接口。

### 中文提示词

```text
只使用输入图片作为长枪造型和材质依据，制作这同一杆武器的完整装配正交三视图。所有视角都包含固定枪体和同一条连接在枪上的朱红飘带，保持输入图的整枪长度、枪刃宽度、枪杆直径、各连接段长度、枪尾比例、飘带长度、展开宽度和根部位置。不重新设计，不缩短飘带，不增加部件。

枪体由尖叶形双刃冷钢枪头、中央脊线、金色与深绿交叉连接件、短银色连接段、连续笔直的红棕圆杆和金色几何枪尾构成。正面枪刃边缘应遵从输入图既有轮廓，不能把中段轻微收腰放大成新的大折肩、倒钩或锯齿；像素阶梯仅属于绘制语言，不是实体锯齿。侧面显示真实的刃片厚度、中央脊线和削薄刃口；侧背面只补合理的同类结构，不改变直径、长度或零件数量。没有独立把手、皮革握套或绿色盘蛇。

朱红飘带只有一个收束根部、一片连续柔性主体和一个主要尖尾。根部连接在输入图上部金绿连接件右侧的原位置，固定方式清楚，不悬空，不增加新的环扣。保持正面外展后弯回的姿态，将其解释为带浅折面的薄布片；前后起伏克制，不绕杆，不自交，不增加第二条带或第二个尾端。

所有视角是同一个静止三维物体的正面、右侧面和背面正交投影，只改变观察方向，不改变姿态。侧面应显示飘带真实的薄边、浅折面和前后深度，允许发生合理遮挡，不能把正面的宽布轮廓重复画一遍。背面显示同一固定根部和同一布片的反面；在整体绕轴观察后，它的画面位置应合理转到相反侧，不在每一视图都强制摆在画面右边。

输出一张1536×1024纯白底PNG，分成三个等宽无边框区域：左正面，中右侧面，右背面，每区只展示一把完整的装配长枪。三个视图同尺度、竖直，枪尖、枪尾、连接件、红带根部和红带末端处于相同高度。每把完整轮廓留在本区内，枪尖和枪尾上下均留至少32像素余量。不得独立放大飘带，不得裁断枪尖、枪尾或红带末端。

沿用输入图的高清像素色簇和清楚实体体积：冷银钢刃、深绿连接件、少量暖金属、红棕木杆、朱红布片及深红折面。光照中性、材质分区清晰，轮廓干净。只展示武器；不要人物、手、场景、文字、标签、尺寸线、坐标轴、分隔线、局部放大、透视、3/4视角、光效或运动残影。
```

### English prompt

```text
Use only the input image as the spear's design and material reference. Produce complete assembled orthographic views of this SAME weapon. Every view includes the rigid weapon body and the SAME attached vermilion ribbon. Preserve the reference's total length, blade width, shaft diameter, connector segment lengths, butt proportions, ribbon length, spread and attachment position. Do not redesign, shorten the ribbon or add components.

The body consists of a pointed leaf-shaped double-edged cool steel head with a central ridge, a gold-and-dark-green crossed connector, a short silver connection, a continuous straight round red-brown pole and an angular gold butt ornament. Match the existing front blade outline in the input: do not exaggerate its subtle waist into new large stepped shoulders, hooks or saw teeth. Pixel stair steps are rendering language, not physical serrations. The side reveals blade thickness, the central ridge and tapered cutting edges. Complete only plausible matching hidden surfaces without changing diameters, lengths or component counts. No separate handle, leather grip sleeve or coiled green serpent.

The vermilion ribbon has one gathered root, one continuous flexible cloth surface and one main tapered terminal. Attach its root at the original position on the right side of the upper gold-green connector shown in the reference. Make the connection readable, with no floating root or new clasp. Preserve its front outward-then-returning curve. Resolve it as thin cloth with shallow folds and restrained depth variation. No shaft wrapping, self-crossing, second ribbon or extra tail.

All views are orthographic projections of ONE fixed static three-dimensional object: front, right side and back. Only the viewing direction changes; the pose does not. The side shows the ribbon's thin edge, shallow folds and actual depth, allowing physically reasonable occlusion rather than repeating its broad front silhouette. The back shows the reverse of the SAME surface and the SAME attachment root. Its screen position should move naturally to the opposite side as the assembly is viewed around its axis; do not force it to the screen-right in every view.

Output one pure-white-background 1536x1024 PNG with three equal-width unbordered regions: front left, right side center and back right. Show one complete assembled spear per region, upright at identical scale, with aligned tip, butt, connector, ribbon-root and ribbon-terminal heights. Keep each entire silhouette inside its own region and leave at least 32 pixels above the tip and below the butt. Do not enlarge the ribbon independently or crop the weapon or ribbon terminal.

Preserve the input's high-definition pixel clusters and readable physical volume: cool silver steel, dark-green connector, restrained warm metal, red-brown wood, vermilion cloth and deep-red folds. Use neutral lighting, clear material separation and clean edges. Show only the weapon. No characters, hands, scenery, text, labels, dimension lines, axes, dividers, detail insets, perspective, three-quarter views, glow or motion trails.
```

### 负面提示词 / Negative constraints

```text
三种不同武器、视角间变姿态或比例、新增大折肩或倒钩、实体锯齿、独立把手、皮革握套、盘蛇、删改金绿连接件、额外零件、额外红带、分叉尾端、粗绳、实心红雕塑、悬空根部、绕杆、自交、独立放大飘带、截断、透视、3/4视角、人物、手、背景场景、文字、标签、尺寸线、骨骼、坐标轴、分隔线、局部放大、光效、残影。

Three different weapons, changed pose or scale between views, new large stepped shoulders or hooks, physical saw teeth, separate handle, leather grip sleeve, coiled serpent, altered or removed gold-green connector, extra components, extra ribbons, branching terminals, thick cord, solid red sculpture, floating root, shaft wrapping, self-crossing, independently enlarged ribbon, cropping, perspective, three-quarter view, characters, hands, scenery, text, labels, dimension lines, bones, axes, dividers, detail insets, glow, motion trails.
```

### 验收重点

- 正面与母图的枪刃轮廓、全枪比例、金绿件、尾件及红带姿态一致，没有借三视图机会重新设计。
- 三视图中的尖/尾/各连接段高度一致；红根与末端高度差在 2px 内是本版目标，真实差异需如实报告，不通过逐图缩放掩盖。
- 完整装配中的红带比例以母图为准；固定点与枪体连续，正/侧/背的绕轴位置和遮挡关系可解释。
- 枪头侧面有厚度但不是第二块正面刃；红带侧面有薄片结构但不是厚绳；背面折面属于同一物体。
- 一杆枪、一条红带，轮廓完整、每区上下至少32px余量；PNG 可解码，挂点/结构的美术判断与技术文件状态分别记录。
- 输出仍属二维建模参照；不得宣称已生成 3D 网格、骨骼、物理效果或精确工程投影。

## 7. Unity 后续接入注意（调查结果，不在本轮实施）

`Assets/Prefabs/Stab.prefab` 当前为根节点 `Stab`，`Transform` 本地旋转约 (90,0,0)、缩放 (0.1,0.1,0.1)，`SpriteRenderer` 引用 `Assets/Sprites/zhangfei/stab.png`，另有 `BoxCollider2D`。没有现成的 MeshRenderer 接入。

`Assets/Scripts/Attack/StabSweepEffect.cs` 并非完全渲染器无关：

- `Create` 从 `SpriteRenderer.sprite.bounds.size.y` 计算半长和初始偏移；
- `Initialize` 同样使用 Sprite 长度，并创建 Sprite 专用 `WeaponMotionBlurController`；
- `ApplyDeformRoll` 确认绕 `_deformRoot` 的本地 Y 轴旋转，可供未来模型适配沿用；
- 形变与枪尾锚定使用 Sprite 半长，蓄力和高速阶段仍会换 Sprite。

因此“模型长轴沿 Y”能保留滚转参数语义，**不等于换一个 FBX 就能无改动运行**。未来需单独适配模型长度/枪尖枪尾锚点、渲染/模糊及换帧分支，并检查 `ChargeStabVisual`、上挑与横扫的共同用枪路径。本轮没有实施这些改动。

相关行为依据：`design/combo-zhangfei-stab3-launch.md`、`design/combo-zhangfei-aim-stab.md`、`plan/combo-progress-and-next.md` §6、`memory/stab-vfx-generation.md`。不把 `stab_v13.png` 的高速形变轮廓用于静态建模。
