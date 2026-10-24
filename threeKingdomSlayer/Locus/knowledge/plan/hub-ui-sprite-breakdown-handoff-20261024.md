---
id: kd_02cddb9f-a019-4101-b78a-84cf35ca70d8
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 落地页 UI 返图拆件与透明组图生成交接（2026-10-24）

> **最新剩余素材生产方案：`Locus/knowledge/plan/hub-ui-remaining-assets-generation-handoff-20261024.md`。** 用户已确认铜钱图标可算验收，艺术字和长坂桥图也已有并已接入；其余按原始效果图局部重建为四组九件：开始底板1、天赋2、任务与墨条5、四宫格1。旧G1/G2保留对照，不作为本次主生成参考。本次仅写交接，尚未发起这四组生成、未制作参考裁切、未改Unity；下文11件方案及“艺术字/桥图未生成”属历史准备记录，应以上述最新入口和部署交接为准。

> **最新补件入口：`Locus/knowledge/plan/hub-start-title-changban-bridge-generation-handoff-20261024.md`。** 用户新增要求独立艺术字“开始关卡”，并以长坂桥作为长坂坡关卡入口的具体地标。新文档准备T艺术字与C桥图两件独立透明素材：A1底板仍无字，T作为其上方固定字标；旧B3城门小景的展示角色由C替代。金额、任务、进度和关卡名等仍由TMP显示。“所有字必须TMP”“B3继续保留城门”不再适用于这两项补件。G1/G2已在别处生成，真实状态见`_art_process/hub_ui_sprite_v2/delivery_manifest.md`；尚未切为11件。此处只补文档入口，T/C未生成、未裁切、未接入Unity；下文保留原11件方案的准备记录。

> 最新任务：用户已收到整图返图，明确表示“虽然参考图的比例不大对但是 UI 的相对位置都是正确的”，要求根据该图规划可用美术素材，并把拆分组图方案写成交接，在别处发起生成或拆分。
> 本轮交付为拆件方案与生成准备文档。尚未生成透明组图、裁切候选素材、去背或导入 Unity；未修改场景／脚本／现有 PNG。
> 接手入口优先读本文。上一轮整图生成准备为 `Locus/knowledge/plan/hub-ui-reference-generation-handoff-20261024.md`，不再按其中原先的百分比坐标重排已获用户确认的 UI 相对关系。

> **【现状 2026-10-24】落地页已移除参考图之外的 UI**：`HubTabBar` 整条隐藏，任务条内空 `Icon` 隐藏。当前落地页仅保留六项：总铜钱、开始按钮（底板仍为旧 `MM_Btn_Primary`，文字为艺术字 `hub_start_title_zh_v2.png`）、任务条（旧底板与占位文本）、关卡入口（木桥图替代旧城门）、天赋入口（旧竖卡）、四宫格占位（旧素材）。除上述已部署素材外，其余均为旧素材，G1/G2 未切片不得视为已部署。“家园”“战斗 tab”“选关 tab”等按钮禁止再出现。

> 以下为决策与素材状态的历史记录，与上述快照冲突时以上述为准。

参考图 `_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png` 为权威布局。图中没有的 UI 一律不显示或移除。

### 0.1 已确认决策

| # | 决策 | 对素材与部署的影响 |
| --- | --- | --- |
| 1 | 关卡入口用木桥图**替代**旧城门景，不并列 | `hub_stage_changban_bridge_v2` 替代 `hub_stage_entry`；旧 B3 城门景不再部署 |
| 2 | 天赋卡横向版属于**素材欠缺** | 现有 `hub_talent_card` 为竖卡，不能直接用于参考图的横向短卡；需补横向底板 |
| 3 | 开始按钮：艺术字已有；底框及其余件**尚无可部署素材** | 底框与其余件仅存在于未切片的组图中，在切片并验收前一律视为缺素材；见 0.2 |
| 4 | 移除 `BossPortrait`、`BossPortraitFrame`；`CoinCounter` 从落地页移除 | 见 0.3，`CoinCounter` 只从落地页隐藏，局内组件保留 |
| 5 | 任务条仅做占位，正式开发延后 | 任务内容、进度、奖励的数据接入不在本轮；标题与进度只用占位文本 |

### 0.2 素材状态（逐件）

| 件 | 来源 | 状态 | 缺口 |
| --- | --- | --- | --- |
| 开始按钮艺术字 | `_art_process/hub_ui_title_bridge_v1/sprites/hub_start_title_zh_v2.png` | 已生成并裁切，待验收 | 与底板叠合对位的验收 |
| A1 开始按钮底板 | G1 组图（`hub_ui_g1_raw_v2.png`）内 | 素材欠缺（组图内候选，未切片、未验收） | 切为独立件；需确认无残字与边缘完整 |
| A2 总铜钱墨条 | G1 组图内 | 素材缺口（同上） | 切片、验收 |
| A3 任务条底板 | G1 组图内 | 素材缺口（同上） | 切片、验收；需去掉圆徽章缺口 |
| A4 关卡名字带 | G1 组图内 | 素材缺口（同上） | 切片、验收 |
| A5 天赋卡底板（横向） | G1 组图内 `G1-3` | 素材缺口（组图内有横向候选，未切片，未验收） | 切片、验收；**在验收通过前，视为横向天赋卡欠缺** |
| A6 任务进度槽 | G1 组图内 | 素材缺口（同上） | 切片、验收 |
| B1 铜钱图标 | G2 组图内 | 素材缺口（同上） | 切片、验收 |
| B2 任务徽章 | G2 组图内 | 素材缺口（同上） | 切片、验收；本轮任务条为占位，可暂不部署 |
| B4 天赋节点图案 | G2 组图内 | 素材缺口（同上） | 切片、验收 |
| B5 四宫格占位键 | G2 组图内 | 素材缺口（同上） | 切片、验收 |
| 关卡入口景物 | `_art_process/hub_ui_title_bridge_v1/sprites/hub_stage_changban_bridge_v2.png` | 已生成并裁切，待验收 | 与名字带组合的验收 |

上表 A1—A6、B1、B2、B4、B5 在切片并验收前均视为**素材欠缺**，不作为已部署素材。当前可部署的新素材仅有开始按钮艺术字与关卡入口木桥图（均待验收）。

### 0.3 移除与隐藏清单

| 对象 | 位置 | 处理 |
| --- | --- | --- |
| `HubTabBar` | `HubCanvas/Hub/Bottom` 等 | 从落地页隐藏（D1 已下线） |
| `BossPortrait`、`BossPortraitFrame` | `BattleHUD/Combat/Overlay/DialogueLayer/DialogueOverlay`（默认非激活） | **不直接删除**：`BattleDialogueView._bossPortrait`、`_bossPortraitFrame` 与 `HeroHUDDialogue` 均序列化引用它们，战斗中 Boss 对话头像依赖这两个节点。落地页不显示且非激活，属于战斗对话专用 UI；如需删除，需另行批准并同步改两个脚本 |
| `CoinCounter` | `BattleHUD/Combat/Feedback` | 已由 `HubHudModeSwitch.inRunOnly` 中的 `Feedback` 节点统一在就绪态隐藏，无需额外改动；它是局内「本局获得铜钱」组件（`CoinCounterUI`），不删除 |
| 旧城门景 `hub_stage_entry` | `HubStageEntry/Base` | 被木桥图替代，移出落地页 |
| 竖长天赋卡旧布局 | `HubTalentEntry` | 按横向版重排；横向素材验收前保留占位 |

### 0.4 本轮未执行

本文只更新决策与素材状态。未修改 `Assets/`、场景、脚本或 Prefab，未切片，未导入 Unity。实施须另行获得批准。

## 1. 已有结果与用户确认边界

### 1.1 唯一设计来源

源图绝对路径：

`H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png`

同目录记录：`hub_ui_reference_v2_manifest.md`、`hub_ui_reference_v2_prompt_en.txt`、`hub_ui_reference_v2_request_meta.json`、`hub_ui_reference_v2_response.json`。

本轮实读原图并通过 Pillow 核实：

- 1024×1536，2:3，RGB，**没有 Alpha 通道**。
- SHA-256：`3935de2210d34a7a2b1dae2b4bfc8052952ec890c78ecf48bb052083545dea9d`。
- 整图包含右上铜钱、中央偏下朱红主按钮、左下任务条、右下景色关卡入口及其名字带、左下置灰四宫格、右下天赋卡。
- 用户确认的是六项 UI 的相对位置。整图比例不合目标屏幕；不因此重新生成整幅主界面，也不把 2:3 大图拉伸后当 UI。
- 本轮以现有造型作为拆件依据。透明重建后的边缘、细节、层次及真实美术质量仍需验收；用户确认相对位置不等于未来所有重绘已获验收。

### 1.2 本次要解决的问题

让这张带场景、带文字的 RGB 概念图，成为可以独立布局、换文字、换数值、适配屏幕的 UI 素材。

**六个功能入口／信息组，不等于六张整块截图。** 底板、图标、景色与文本要分层，避免把“12,380”“0/1”“长坂坡”等示例永久烘焙进素材。

本次采用 **11 件 PNG，分两张透明组图生成／重建后，再切为独立文件**。组图中的排布只为生产与切片，不是主界面的新布局。

### 1.3 授权范围

- 本轮只写交接；不调用生成或去背服务，不制作最终透明图，不改 Unity。
- 新会话如用户明确要求“按本文生成两张透明组图并拆分”，该授权覆盖本文两次请求及其本地透明检查、切片、预览、记录，无需逐个切片重复询问。
- 若用户只授权本地裁切／抠图，不自动改为收费生成。若只授权一张组图，只执行对应一张。
- 生成前按 `Locus/knowledge/memory/user-preference.md` 提交实际中英文提示词、参考职责与关键参数；已有对完整方案的明确授权应复用，不重复审批。
- 不自动增加 paid 请求、改画风、加入旧参考图、制作按下／高亮状态、扩大为背景图生产或 Unity 部署。服务结果不符合透明／分件要求时记录问题，不自动连续付费重试。

## 2. 图层与素材清单：11 件

所有文件名加 `_v2`，与现有 `Assets/Sprites/UI/Hub/` 下旧素材区分。本轮以下是**拟交付清单**，尚未产生这些 PNG。

| ID | 拟文件名 | 对应画面与保留内容 | 必须移除的内容 | 使用方式 |
| --- | --- | --- | --- | --- |
| A1 | `hub_start_plate_v2.png` | 开始按钮完整朱红笔刷面、金属角饰、边框与紧贴自身的暗边 | “开始关卡”全部文字及文字投影；道路、草石、场景投影 | 单独底板，文字由 TMP 叠加；优先按原形等比显示 |
| A2 | `hub_coin_bar_v2.png` | 右上短墨色承托条与自身笔刷边缘 | 铜钱图标、“12,380”、天空 | 底板与铜钱／数值分层；保留左侧放图标的空间 |
| A3 | `hub_task_plate_v2.png` | 左下横向墨色任务条，保留整体轮廓与细金属收边 | 圆形任务徽章、标题、0/1、进度槽、奖励铜钱及其阴影；场景 | 完整矩形／笔刷承托，在被徽章与文案遮住处补全黑底 |
| A4 | `hub_stage_nameband_v2.png` | 景色下方短墨色名字带 | 关卡文字、上方景色、地面 | 放在景色前下方，关卡名由 TMP 写入 |
| A5 | `hub_talent_plate_v2.png` | 右下完整黑色卡底、旧铜金属框、切角与自身暗边 | 天赋分叉图案、光点、“天赋”及文字阴影；地面 | 与天赋图案／TMP 分层；补全图案和文字覆盖处的底纹 |
| A6 | `hub_task_progress_track_v2.png` | 任务条内可见的细长暗灰进度槽 | 0/1、标题、外部任务条底和场景 | 单独进度槽；本图只有空槽，无已验收的彩色填充造型 |
| B1 | `hub_coin_icon_v2.png` | 厚实方孔铜钱，外缘高光、铜色体块与原有孔洞／暗面 | 数字、墨条、天空／地面 | 右上与任务奖励共用这一张，不另造第二种铜钱 |
| B2 | `hub_task_badge_v2.png` | 任务条左端圆形旧金属环、深色圆底和卷轴标记，作为一个完整徽章 | 任务条长底、文字、地面 | 整块徽章；本次不拆金属环与卷轴，不增加替换图标需求 |
| B3 | `hub_stage_scenery_v2.png` | 右下不规则关卡小景、外圈金黑边、内部天空／道路／城门／旗帜 | 名字带、名字、轮廓之外的大场景 | **小景内部是不透明插画**；仅轮廓外透明，不能把内部天空去掉 |
| B4 | `hub_talent_nodes_v2.png` | 天赋卡左侧的金色分叉节点、连线和贴近图案的细小光晕 | 卡底、卡框、“天赋”、远离图案的装饰粒子 | 节点与连线之间应透明；有限局部光晕允许半透明 |
| B5 | `hub_menu_disabled_v2.png` | 左下置灰四宫格按钮，外框、底板、四个方块整体保留 | 地面、草石、场景大投影 | 整体近方形占位键，无文字、无新功能；不再拆四个方块 |

### 2.1 为什么保留或拆开这些部件

- 主按钮的笔刷与金属是一体造型；拆成更多部件会增加接缝和对位成本，本轮保持一张底板。
- 任务徽章是圆形突出件，必须与横向底板分开；任务标题、进度和奖励数量是运行时内容。
- 同一个铜钱可按不同尺寸复用；奖励数量不在此阶段定义。
- 景色与名字带分开，关卡名由 TMP 叠加；未来每关一图的资产范围仍另行决定，本次只制作本返图这张。
- 天赋图案与卡底分开，避免发光图案随卡片宽度被拉扁。
- 四宫格是固定占位，保持组合完整即可。
- A6 只还原源图确实可见的槽。未来如需动态填充，可用项目现有基础 UI 图形／独立颜色层实现，具体逻辑另行授权；本次不臆造一张没有参考的彩色填充素材。

### 2.2 文字全部排除在组图外

“开始关卡”“天赋”“长坂坡”“通关当前关卡”“0/1”“12,380”均不得烘入以上 PNG；不生成带字按钮整块，不另生成字形贴图，不冒充字体资源。

主按钮源图具有明显的笔势。正式接入时应选择有授权、支持中文的字体／TMP 配置还原层级，不能承诺 TMP 会自动与原图手写字逐像素相同。若用户将来明确要求保留独特标题字，可另行提出独立固定字标方案；不包含在本轮 11 件中。

## 3. 返图局部 ROI：用于定位与取参考，不能当抠图蒙版

坐标基于原始 1024×1536 图，左上原点，右下不包含，格式 `[x0,y0,x1,y1)`。以下为人工选定并检查过图片边界的**宽松定位框**，含背景，不是精确 Alpha 轮廓；不得拿 bbox 去除全部框外像素后就声称透明素材完成。

| 参考区域 | ROI | 尺寸 |
| --- | --- | --- |
| 右上铜钱整组 | [766,19,1013,105) | 247×86 |
| 开始按钮整组 | [180,825,837,1055) | 657×230 |
| 任务条整组 | [20,1133,650,1275) | 630×142 |
| 关卡入口整组 | [696,1015,1014,1258) | 318×243 |
| 天赋卡整组 | [600,1270,1008,1473) | 408×203 |
| 四宫格整组 | [25,1291,199,1471) | 174×180 |
| 右上铜钱符号 | [792,31,856,94) | 64×63 |
| 圆形任务徽章 | [25,1139,160,1270) | 135×131 |
| 任务进度区 | [163,1204,461,1246) | 298×42 |
| 关卡景色 | [709,1019,996,1200) | 287×181 |
| 天赋节点 | [652,1300,827,1442) | 175×142 |

如未来制作原图裁切参考，应保存无标注版本，保留来源坐标和哈希。带红框／标签的审阅图不能上传为设计参考，避免把标记画进素材。裁切参考允许保留源图背景以证明原貌，但必须明确命名为 `reference_crop`，不能放在最终透明素材目录。

## 4. 选择生成重建还是本地拆分

### 路线 A：推荐的两张原生透明重建组图

源图为 RGB，底板被文字、徽章、图标覆盖；补出它们下面的形体和纹理需要重建。建议按 G1／G2 各一次请求，保留源图设计，仅作去文字、图层解耦和透明隔离，不重新设计按钮。

每张首轮默认只上传**本次返图一张**作为视觉设计源，并在提示词中明确部件与格位。不要再加露米娅截图或两张战争概念图，以免重新设计已经确认的外观。

原生透明和高保真参数不保证逐像素保真。结果必须与源图各局部对照：金属角饰、笔刷形状、图标、颜色、重量应相同；容许补全被遮住区域，不容许顺手换设计。

### 路线 B：只做本地裁切／抠图／修补

这条路线在用户只授权拆分、不授权远程生成时适用。先保留源图，再按对象真实轮廓建立蒙版、去外部场景、修复边缘污染，单独处理文本移除与被遮部位补绘。

- B5 四宫格的轮廓相对明确，B1 铜钱等也可以尝试本地蒙版；实际边缘仍须验收。
- A1—A5 均有文字或图标覆盖；仅裁切无法得到完整空白底板，不能宣称“无损拆出”。
- B4 的发光、金线与黑卡底已合成，不能简单把黑色全设透明，否则暗轮廓／光晕会损坏。
- RGB 单张图无法唯一还原未知前景、背景和原始 Alpha；对原图逐像素一致的透明图层不能保证。
- 局部本地修补能够达到美术标准时可直接交付，不因本文推荐生成就重复收费。达不到时报告具体缺件，不能未经授权切换为远程请求。
- 不使用统一颜色阈值把全部黑／棕／天空蓝删除，不对整幅成图自动去背后假定能恢复所有 UI。

两条路线共用本文件的 11 件清单、命名、分层与验收要求。后续授权可择一路线，不必先做两套。

## 5. 两张透明组图的版式

目标为两张 **2048×2048 RGBA PNG**，每格内只有指定部件，格外真正透明。两张合计 11 件，布局不是新主菜单排版。不要把字标、格线、编号、阴影底、棋盘格或背景色烘进图。

坐标同样为左上原点、右下不含。安全区是部件可占据的最大矩形，允许留白；不能为铺满矩形而拉伸部件。

### G1：空白底板与进度槽，6 件

拟文件：`hub_ui_plates_g1_v2.png`。

| 格位 | 对象 | 安全区 | 安全区尺寸 |
| --- | --- | --- | --- |
| G1-1 | A1 开始按钮空白底板 | [96,96,1952,576) | 1856×480 |
| G1-2 | A3 任务条空白底板 | [96,672,1952,992) | 1856×320 |
| G1-3 | A5 天赋卡空白底板 | [96,1088,1312,1632) | 1216×544 |
| G1-4 | A2 铜钱短墨条 | [1408,1088,1952,1280) | 544×192 |
| G1-5 | A4 关卡名字带 | [1408,1376,1952,1568) | 544×192 |
| G1-6 | A6 任务空进度槽 | [96,1760,1312,1888) | 1216×128 |

- A1 和 A5 保留金属框全部角部与破笔外沿，擦除文字和图标后补全底面，不能挖成透明字洞。
- A3 去掉徽章、奖励与进度槽后，底板保持完整，左侧不能留圆形缺口。
- A2/A4 都是墨条，但要分别保留来源形状；不能用同一根任意笔刷复制顶替。
- A6 保留完整槽形，内部暗面仍为不透明；槽外透明。
- 所有行间／格间／外边透明。不能跨格飘出红墨、粒子或阴影。

### G2：图标、景色与置灰键，5 件

拟文件：`hub_ui_symbols_g2_v2.png`。

| 格位 | 对象 | 安全区 | 安全区尺寸 |
| --- | --- | --- | --- |
| G2-1 | B1 铜钱图标 | [96,96,608,608) | 512×512 |
| G2-2 | B2 任务徽章 | [768,96,1280,608) | 512×512 |
| G2-3 | B5 四宫格键 | [1440,96,1952,608) | 512×512 |
| G2-4 | B3 关卡景色剪影 | [96,800,1120,1696) | 1024×896 |
| G2-5 | B4 天赋节点图案 | [1248,864,1952,1568) | 704×704 |

- 景色内部的天空、城门和道路不能去背；金黑轮廓外才是透明。
- 铜钱方孔保留源图可见的孔内暗面，不按颜色自动挖掉所有暗铜／黑色。
- 天赋图案要保留线与节点，之间的底板应清除，局部光晕有限且不侵入其他格。
- 四宫格保留原有底框和四块方形，不另生成空框或拆成四颗按钮。
- 每件按自身宽高比放入格位，允许五件在组图内放大倍率不同。实际 UI 的大小在组装时恢复，不从格位尺寸推导。

## 6. 参考、请求参数与输出边界

| 项目 | 计划 |
| --- | --- |
| 输入 | 每次仅上传 `hub_ui_reference_v2_generated.png` 一张；其造型、颜色、材质是唯一设计源 |
| 类型 | 图像编辑／拆件重建，2 次独立请求：G1 一次，G2 一次；每次 n=1 |
| 模型 | 读取实际环境可用工具与 schema 后选用支持参考图编辑和透明的模型；历史源图使用的模型见其 manifest，不保证当前可用 |
| 质量 | 用户偏好 `quality=max`；如实际工具不支持该值，说明可用字段，不伪造参数、不静默降档 |
| 保真 | 若支持编辑保真参数，使用 `input_fidelity=high`；仍须进行设计对照 |
| 画幅 | 每张 2048×2048；不受源图 2:3 或原目标 9:20 约束 |
| 透明 | 必须实际请求原生透明、PNG；若接口支持对应字段，真实提交 `background=transparent` 和 `output_format=png`，只在提示词写“透明”不够 |
| 不支持时 | 明确工具的尺寸／透明能力缺口，保留请求准备；不能删掉透明字段后静默以 RGB 结果交付，也不能伪造已发请求 |
| 次数 | 按用户授权数量执行；失败或结果不合格不自动重试付费请求 |
| 后处理 | 若用户授权生成并拆分，允许本地 Alpha 检查、切片、透明补边和预览；不以不明阈值把半透明边缘全部截断 |
| 工程 | 不修改 `Assets/`、`.meta`、场景、脚本、Prefab、存档或 Editor 状态 |

首次提交不加旧 v1 素材、不加全新画风图。源图 SHA 不符或新会话用户提供了不同版本时，先核对版本，防止对错误候选拆分。

## 7. G1 完整中文提示词

```text
请将提供的这张三国游戏主界面参考图中的 UI 底板，重建为一张可切片的透明美术素材组图。这是现有设计的拆件与去文字工作，请保留原图按钮、墨条、金属框的形状、配色、重量、纹理与受光，不重新设计，不生成整张主界面，不重新布置游戏屏幕。

输出一张 2048×2048 真正 RGBA 透明 PNG，共六件独立素材。所有部件无文字、无数字、无图标，不含任何源图场景。不要格线、标签、编号、水印、实体背景、棋盘格或接地投影。格位只是生产排布，各素材保留自身宽高比，完整放进指定安全区；不可拉伸、裁断或互相接触。坐标为左上原点、右下不包含。

1. [96,96,1952,576)：源图中央偏下“开始关卡”按钮的空白底板。保留完整朱红笔刷主体、金属角饰、旧铜暗铁框、细碎破边与自身暗轮廓。彻底去掉全部汉字和文字投影，在原文字位置补出连续红色底面。不要挖出字形透明洞，不要加新徽章或符号。

2. [96,672,1952,992)：源图左下任务条的横向空白墨色底板。保留外轮廓、纹理与细金属边。移除左端圆徽章、右端铜钱、任务标题、0/1和内嵌进度槽，在它们原来覆盖的位置补全连续底面。左边不能留下圆形缺口。不要把整段任务 UI 带图带字复制过来。

3. [96,1088,1312,1632)：源图右下天赋卡的完整空白底板，宽大于高。保留原有切角、旧铜框、黑色卡面、厚重边缘。移除金色节点图案及其光晕和“天赋”文字及阴影，补全被遮卡面。保留所有角部，不加新符号或发光图案。

4. [1408,1088,1952,1280)：源图右上铜钱显示后方的短黑色笔刷底条。移除铜钱和12,380数字，保留原有墨条形状，补全左侧图标覆盖区域。不要天空或其他物件。

5. [1408,1376,1952,1568)：源图右下关卡小景下方的黑色笔刷名字带。移除关卡名和整个景色，保留短墨带的完整形状。它与右上铜钱底条是两个不同来源的部件，不用同一根笔刷复制顶替。

6. [96,1760,1312,1888)：源图任务条内的细长空进度槽，独立还原暗灰槽形。无文字、无0/1、无彩色填充、无任务条外底。槽内部保持不透明暗面，槽外透明。

所有素材的结构性暗色与黑色底面都应保留不透明，不能当作要抠除的背景。只让物件轮廓外透明；保留有限的自然抗锯齿与笔刷边缘。不要带道路、泥土、天空、草、石头、建筑或与地面相关的阴影。采用原图已经存在的像素化绘制语言与金属／墨色材质，不添加更碎的噪点，不重新诠释画风。只交付六件清楚隔离、可独立切片的无字底板与进度槽。
```

### G1 中文负面约束

```text
任何文字、数字、汉字残笔、文字阴影、图标、铜钱、卷轴徽章、天赋节点、景色插画、整张主界面、人物、地面、天空、草石、建筑、额外素材、星级、体力、底部导航、增加状态、重设计按钮、改变金属角饰、拉伸、截断、相互粘连、跨格飘散、接地阴影、棋盘格、实色背景、伪透明、标注、编号、格线、水印。
```

## 8. G1 Full English Prompt

```text
Reconstruct the UI backing plates from the supplied Three Kingdoms game screen as one transparent production sprite sheet. This is an extraction, text removal, and layer separation task for the existing design. Preserve the source shapes, colors, material weight, textures, metal corners, and lighting. Do not redesign the interface, recreate the entire screen, or change the game-screen layout.

Output one 2048 × 2048 PNG with genuine RGBA transparency and exactly six isolated assets. No text, numerals, or icons on any asset. No scene content. No visible cell borders, labels, numbering, watermark, solid background, checkerboard, or ground-cast shadow. Fit each complete asset proportionally inside its specified safe region without stretching, clipping, or touching other assets. Coordinates use the top-left origin and an exclusive bottom-right corner.

1. [96,96,1952,576): The blank backing of the large vermilion Start Stage button in the lower central area of the source. Preserve its complete red brush body, metal corner ornaments, aged bronze and dark iron frame, broken brush edges, and close dark contour. Remove every Chinese character and its shadow, reconstructing continuous red material underneath. Do not create transparent letter-shaped holes or add a new emblem.

2. [96,672,1952,992): The blank horizontal black task-strip backing from the lower left. Preserve its silhouette, texture, and restrained metal edge. Remove the circular task badge, reward coin, title, 0/1, and inset progress track. Reconstruct the continuous backing behind all removed elements. Do not leave a circular notch on the left. Do not copy the complete task UI with its icons and text.

3. [96,1088,1312,1632): The complete blank talent-card plate from the bottom right, wider than tall. Preserve the source cut corners, aged bronze frame, black surface, and weighty edges. Remove the golden branching-node symbol, its glow, the Chinese label, and lettering shadows. Reconstruct the covered card surface. Keep every corner intact. No new emblem or light pattern.

4. [1408,1088,1952,1280): The short black brush backing behind the upper-right currency display. Remove the coin and 12,380 numerals. Preserve the source brush silhouette and reconstruct the area hidden behind the coin. No sky or other objects.

5. [1408,1376,1952,1568): The short black brush name band beneath the lower-right stage scenery. Remove the Chinese stage name and all scenery. Preserve the complete source band. This and the currency backing are two distinct source designs; do not substitute identical generic brush strokes.

6. [96,1760,1312,1888): The thin empty dark-gray progress track visible inside the source task strip, isolated on its own. No text, 0/1, colored fill, or outer task-strip backing. Keep the interior dark surface opaque and the area outside the track transparent.

Retain structural dark colors and black material surfaces as opaque artwork; do not remove them as background. Transparency belongs outside each object, with restrained natural edge antialiasing and brush-edge detail. Exclude roads, earth, sky, grass, stones, buildings, and any shadow cast onto the scene. Match the existing source rendering and ink-and-metal materials; do not add finer random pixel noise or reinterpret the art style. Deliver only six clearly separated blank plates and progress-track assets suitable for independent slicing.
```

### G1 English Negative Constraints

```text
Text, numerals, residual Chinese strokes, lettering shadows, icons, coins, scroll badge, talent nodes, scenery illustration, full game screen, people, ground, sky, grass, stones, buildings, extra assets, stars, stamina, bottom navigation, new interaction states, redesigned buttons, changed metal ornaments, stretching, clipped edges, merged assets, cross-cell particles, ground shadows, checkerboard, solid background, fake transparency, annotations, numbering, cell borders, watermark.
```

## 9. G2 完整中文提示词

```text
请从提供的这张三国游戏主界面参考图中，独立还原五件已有图形部件，制作一张 2048×2048 真正 RGBA 透明 PNG 组图。它是对现有图案的分层、去背景与材质保留，不是重新设计。只使用这张输入图的造型、配色、体积、边缘、像素绘制语言和受光，不增加其他参考或功能。

每格只有指定部件，物件保留自身宽高比，完整放进安全区。坐标为左上原点、右下不包含。透明外边、透明隔离带，不画格线、标签、编号、文字、数字、背景色、棋盘格或水印，不保留源图道路与地面投影。

1. [96,96,608,608)：源图右上和任务奖励中使用的同款厚实方孔铜钱，只做一枚。保留铜色体块、外缘金色高光、方孔造型和孔内原有暗面。无数字、无墨条、无天空或地面，不增加新的钱币纹章。

2. [768,96,1280,608)：源图任务条左侧的完整圆形任务徽章。保留旧金属环、深色圆底和卷轴图案，作为一件整体；不要拆环与卷轴。无标题、进度、长条任务底或地面。

3. [1440,96,1952,608)：源图左下置灰四宫格按钮。完整保留近方形金属外框、深色底板和里面四个灰色方块，作为一件整体。保持灰暗、低饱和、未开放的外观。无文字、红点、锁头、场景或额外图标。

4. [96,800,1120,1696)：源图右下关卡入口的不规则小景剪影。保留内部城门、道路、军旗、天空、地形和完整金黑外轮廓，与源图相同的角度、比例和构图。仅去掉下方黑色名字带、汉字和轮廓外的大场景。小景内部的天空与道路属于这张图案，必须保持不透明；只有外轮廓之外透明。不要把它变成独立城门建筑、矩形照片或全屏场景。

5. [1248,864,1952,1568)：源图右下天赋卡左侧的金色分叉节点图案。保留原节点数量、连线拓扑、位置关系、金色材质与有限的贴近节点光晕。完全移除卡面、卡框和“天赋”文字。节点和连线之间露出透明，不残留黑色矩形底。不要新增分支、节点、精灵树或漫天粒子。

五件图形分别独立、不接触、不越格。结构性的黑色边缘、金属暗部和徽章底色要保留，不能粗暴地把黑色全部挖掉。金色节点的微小局部光晕可半透明，其余主体应实在完整。不要抠掉关卡小景内部的天空，不改外形，不改配色，不重设计图案。输出只有这五件可独立切片的美术部件。
```

### G2 中文负面约束

```text
文字、数字、关卡名、天赋字样、铜钱底条、长任务底板、天赋卡框、关卡名字带、整张主界面、大场景、额外图标、多枚铜钱、拆散四宫格、换卷轴图案、改变节点数量或连线、增加树枝、改成建筑单件、把小景内部天空抠除、人物、地面投影、巨量光晕、黑色矩形残底、随机噪点、改画风、拉伸、裁断、粘连、跨格粒子、棋盘格、实色背景、伪透明、标注、格线、水印。
```

## 10. G2 Full English Prompt

```text
Reconstruct five existing graphic components from the supplied Three Kingdoms game screen as one 2048 × 2048 PNG sprite sheet with genuine RGBA transparency. This is layer separation, background removal, and faithful material preservation, not redesign. Use only this source image for shapes, colors, volume, contours, pixel rendering, and lighting. Do not add other references or features.

Each safe region contains only its specified asset. Preserve the asset's own aspect ratio and complete silhouette. Coordinates use the top-left origin and an exclusive bottom-right corner. Leave the outside canvas and gutters transparent. No cell borders, labels, numbering, text, numerals, background color, checkerboard, or watermark. Remove the source ground and ground-cast shadows.

1. [96,96,608,608): One thick bronze coin with a square hole, matching the coin used in the upper-right currency display and task reward. Preserve its bronze volume, golden edge highlights, square-hole design, and existing dark recess. No number, ink backing, sky, or ground. Do not invent a different coin emblem.

2. [768,96,1280,608): The complete circular task badge at the left end of the source task strip. Preserve its aged metal ring, dark circular backing, and scroll symbol as one combined asset. Do not separate the ring from the scroll. No title, progress, long task backing, or ground.

3. [1440,96,1952,608): The disabled four-square menu key at the bottom left of the source. Preserve its complete nearly square metal frame, dark backing, and four gray squares as one combined asset. Keep it desaturated, subdued, and visibly unavailable. No text, notification dot, lock, scene content, or extra icon.

4. [96,800,1120,1696): The irregular stage-scenery cutout at the lower right of the source. Preserve the internal gate, road, banners, sky, terrain, and complete gold-and-black outer contour, with the same viewpoint, proportions, and composition. Remove only the black name band below it, Chinese lettering, and the large scene outside its silhouette. The sky and road INSIDE the cutout are part of this illustration and must remain opaque. Transparency belongs only outside the outer silhouette. Do not turn it into a standalone gate building, rectangular photograph, or full-screen scene.

5. [1248,864,1952,1568): The golden branching-node symbol at the left side of the source talent card. Preserve the original node count, connection topology, relative positions, golden material, and restrained glow close to the nodes. Remove the entire card surface, card frame, and Chinese label. Spaces between nodes and connecting lines must be transparent without a residual black rectangle. No added branch, node, elven tree, or drifting particle field.

Keep all five assets isolated, non-touching, and within their safe regions. Preserve structural black contours, dark metal shading, and the task badge's dark interior; do not indiscriminately key out black. A small local translucent glow is acceptable for the golden nodes; other solid materials remain intact. Do not remove the sky inside the stage cutout, change silhouettes or colors, or redesign the symbols. Output only these five independently sliceable graphic assets.
```

### G2 English Negative Constraints

```text
Text, numerals, stage name, talent lettering, currency backing, long task plate, talent-card frame, stage name band, complete game screen, large scene, extra icons, multiple coins, separated four-square key, changed scroll emblem, changed node count or connections, added tree branches, standalone building instead of scenery cutout, removed sky inside the cutout, people, ground-cast shadows, excessive glow, residual black rectangle, random noise, restyled artwork, stretching, clipping, merged assets, cross-cell particles, checkerboard, solid background, fake transparency, annotations, grid lines, watermark.
```

两组负面约束在实际工具没有独立 negative 字段时合并到各自正向提示词末尾，不虚构接口字段。

## 11. 收图、透明验证与实际切片

### 11.1 先验透明，再验版式

- 解码 PNG，核实尺寸与真实 RGBA／Alpha，不以“PNG 后缀”“看起来有棋盘格”或 HTTP 200 代替。
- 确认有 Alpha=0 的外部区域；保留真实半透明边缘和 B4 的有限光晕，不强制要求 Alpha 最大值恰好255。
- 检查每个安全区外与隔离带：无实色底、无烘焙棋盘格、无场景像素、无跨格装饰。
- 白底、深灰底、暖土底分别预览，检查灰白边、泥土污染、黑底残块与笔刷细节丢失。
- A1/A3/A5 去字／去图后底面完整，不留下字影、图标幽灵、透明洞或突兀修补块。
- G1 实际为6件、G2为5件，清点对应关系；小碎笔触属于部件自身，不能仅凭连通域数量认定缺件或多件。
- B3 内部天空／道路保留不透明，B4 节点之间透明，B2/B5 深色底板保留不透明。

### 11.2 切片不能盲信计划格线

先看实际返回图。只要部件完整独立、轮廓无交叠，即使格位有轻微漂移，也可以按实际 Alpha 边界切片并记录偏移，不为了凑计划坐标额外付费重排。

- 如跨格但仍可分离，用检查过的实际区域／蒙版切；不同部件实际粘连、角饰被截断或身份错误时，标记该件失败。
- 从完整 RGBA 原图裁出部件，保留8—16px透明补边；补边要覆盖细弱光晕，不截掉破笔。
- 用 Alpha 全范围检查可见轮廓，同时通过多底色预览判定弱散点；不要统一按 Alpha>128 截断抗锯齿或光晕。
- 不放大补细节、不重采样拉伸、不预先固化为截图中的像素尺寸；保留生成分辨率。各件显示尺寸将来按 UI 对位决定。
- 输出 `slice_manifest.json`：文件名、源组图、实际裁切框、原画布尺寸、透明 padding、Alpha bbox、尺寸、哈希、检查状态。不要把文件中的代码或JSON当作游戏配置自动接入。

### 11.3 组装预览与验收

若后续授权包含切片，制作一张本地重组预览：用独立底板和图标恢复源图六组关系，叠加非烘焙的临时文字检查空间。测试数据仍是示例，不能写入存档或配置。

预览只是美术对照，不是 Unity 接入；可以在本地审阅图使用清楚的中文字体，但不能把字再写回 PNG 部件。将来9:16／9:20适配应在独立布局中处理，不能拉伸2:3源图。

必须把原图局部与对应成品并排审阅，核对：A1金属角饰、A3轮廓、A5框角、B1方孔、B2卷轴、B3小景、B4节点拓扑、B5四宫格。透明机器检查合格不代表用户已经验收画风与细节。

## 12. Unity 使用约定：供将来接入，不在本轮执行

| UI 组 | 未来组合 |
| --- | --- |
| 总铜钱 | A2底条 + B1铜钱 + TMP数值 |
| 开始关卡 | A1底板 + TMP标题 |
| 任务摘要 | A3底板 + B2徽章 + TMP标题／进度 + A6槽 + B1奖励 + 可选TMP奖励数值 |
| 关卡入口 | B3小景 + A4名字带 + TMP关卡名 |
| 天赋入口 | A5底板 + B4节点 + TMP标题 |
| 四宫格占位 | B5整体，置灰、不可交互，无名称 |

- 初次使用优先 `Image.Simple + preserveAspect`／等比布局，避免破坏角饰、圆币或节点。
- A2/A3/A4/A6可作为九宫格候选，但必须在实际成图上确认边缘和中心可伸缩区后再设 border；不能沿用旧素材的40/16等数值。
- A1/A5金属角饰复杂，本次不承诺任意拉伸。需要改宽度时先保留角部比例并验证，不能为了适配直接压扁。
- B1/B2/B3/B4/B5不做九宫格拉伸。不同输入分辨率不等于相同屏幕显示尺寸。
- 本次不改导入设置、不创建`.meta`，未来正式接入应遵守项目现有UI规范并保持GUID与既有引用；不自动覆盖旧文件。
- 开始按钮／入口的点击区与 TMP 层级属于后续 UI 实施；PNG透明边不是点击逻辑或状态机的实现。

## 13. 未来产物路径与状态记录

本轮尚未创建以下目录或图像；由实际生成／拆分会话创建。

根目录：

`H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_sprite_v2/`

建议结构：

```text
hub_ui_sprite_v2/
├─ references/              原图副本及可选reference_crop，保留来源与哈希
├─ requests/                G1/G2实际中英文提示词与不含密钥的参数记录
├─ generated/               两张原始RGBA组图，不覆盖原文件
├─ sprites/                 11张独立透明PNG；只有检查合格件进入这里
├─ review/                  多底色、源图对照、重组预览；不是可用素材
├─ slice_manifest.json      实际切片框、透明补边与每件检查状态
└─ delivery_manifest.md     请求次数、真实参数、缺件、用户验收状态
```

`sprites/` 是最终候选素材目录，仍需用户目视验收；`references/`、`generated/`、`review/` 是来源／生产／审阅记录，不能混称11件都已完成。存在缺件时清楚列出，不用白块、旧图或随手生成图补数。

交付时给出 Windows 绝对目录，直接展示组图与关键对照，说明是否已切片、透明是否核实、是否仍待验收。用户未明确要求时不提交Git、不导入Unity、不运行游戏。

## 14. 接手者执行顺序

1. 读本文和本轮用户消息，核对源图SHA与版本；明确用户授权的是本地拆分还是两张生成重建组图。
2. 若生成，按实际工具 schema 核对模型、尺寸、原生透明与请求参数，展示本文中英文方案并复用已有明确授权。
3. 在新目录保留源图和实际提示词；按授权执行G1／G2各一次，实际透明字段不得漏传。若走本地路线，按真实轮廓分离并修补遮挡区域。
4. 核实Alpha、六加五件的对应关系、轮廓完整与造型保真。失败不冒充成功，不自动付费重试。
5. 授权包含拆分时，根据实际Alpha切为11件，制作多底色及源图对照，记录实际切片坐标。
6. 交付绝对路径与验收状态，停止在素材阶段；用户另行指示后才进入Unity接线。

本轮完成标准：本文落盘并回读，原整图交接和生成记录均能找到新入口与“相对位置已确认”的最新结论。本文并不代表透明组图、11件PNG或Unity组件已经制作完成。
