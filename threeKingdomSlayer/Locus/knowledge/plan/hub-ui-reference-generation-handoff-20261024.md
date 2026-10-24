---
id: kd_57b7a12e-45d2-484d-b920-a25f71153d26
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 主菜单落地页整图参考生成交接（2026-10-24）

> **【现状快照 2026-10-24，生图 AI 须遵守】** 落地页实际场景与素材已变化，以下为准：
> - **参考图之外的 UI 已移除**：底部 tab 栏（战斗 / 选关 / 天赋 / 家园）整条隐藏；任务条内的空图标位已隐藏。**不得再生成“家园”“战斗 tab”“选关 tab”“首页入口”等任何按钮或标签。**
> - **只保留六项 UI**：总铜钱（右上）、开始关卡（下半中央）、任务条（左下）、关卡入口（右下景色块）、天赋入口（右下横向卡）、四宫格占位（左下置灰）。
> - **开始按钮**：底板仍为旧素材 `MM_Btn_Primary`；文字已用独立艺术字 `hub_start_title_zh_v2.png` 替代（TMP 文字已隐藏）。待底板 A1 切片验收后替换。
> - **关卡入口**：景色图已从旧城门替换为木桥图 `hub_stage_changban_bridge_v2.png`（运行时由 `HubStageEntry.baseSprite` 驱动）；名字带仍为旧素材。
> - **天赋卡**：仍为旧竖卡 `hub_talent_card`（260×408），横向版尚无素材，需要生成横向底板。
> - **任务条 / 铜钱 / 四宫格 / 天赋图案**：仍为旧素材；未切片的 G1/G2 组图不得作为已部署素材。
> - **任务条内容**：为占位文本（“本关目标”“0/0”），数据未接入。
> - **不存在或不显示**：首页头像、等级、经验条、体力、星级、局内战斗 HUD、武将角色、敵人。生成内容不得出现这些元素。

> **最新进展：整图已在别处生成，用户确认“比例不大对，但 UI 的相对位置都是正确的”。下一阶段拆件入口为 `Locus/knowledge/plan/hub-ui-sprite-breakdown-handoff-20261024.md`。** 源图在 `_art_process/hub_ui_reference_v2/hub_ui_reference_v2_generated.png`，1024×1536 RGB。新方案将六组 UI 分为11件独立素材，规划两张真正透明组图（6件底板与槽＋5件图标与景色），所有文字交给TMP。不要为修正比例而重新生成整图，也不要重新套用下文原先的百分比坐标。拆件仍为准备文档，尚未生成组图、去背、切片或导入Unity。

> 以下保留的是前一轮整图生成前准备记录，历史状态：当时未调用图片生成服务，未生成图片，未改 Unity 场景、脚本或素材。
> 用户已同意本轮分析方向，并要求制作交接文档，在别处发起生成。该同意覆盖本方案与文档准备，不视为对任意生成接口、费用或后续修改的自动授权。
> 本文交付的是一份可以带到新会话的完整制作说明，包括参考图职责、构图、画风、中英文提示词、参数意图和验收口径。

## 1. 给接手者的首要说明

### 本次要做的东西

制作 **一张完整竖屏主菜单／落地页 UI 参考效果图**：我们的三国战场画风、完整关卡环境、六个已确认的 UI 元素在同一张画面中成立。画面应像打开游戏后看到的就绪态界面，可用于用户审构图、视觉层级、按钮造型和材质统一性。

目前最大缺口是缺少这张整体视觉定稿。现有零散 UI 素材不能作为已经验收的设计答案。先有整图，再讨论独立素材拆分和 Unity 布局落地。

本次不制作透明素材合集、图标九宫格、资产拆件表、多张方案拼版、战斗 HUD、选关页或天赋树详情页。

### 授权与后续边界

- 本会话仅准备并保存交接文档；不发起生成。
- 用户在新会话发起生成时，接手者先展示实际采用的完整中英文提示词、负面提示词、三张参考图的职责、模型／尺寸／质量／透明度和产出数量，确认其与本方案一致。
- 若新会话用户已明确批准该完整方案并要求生成，则依该授权执行；不能把本轮的“同意分析方向”扩大为任意收费请求的批准。
- 若参考图、提示词或关键参数发生实质改变，应说明改变，并在生成前取得对应批准。
- 首次只制作一个完整构图候选；后续轮次按用户反馈单独决定。不自动批量抽卡，不擅自出第二套功能或第二套布局。
- 完成生成后交图给用户审阅；不自动导入 `Assets/`，不覆盖既有素材，不修改场景，不运行游戏。

## 2. 项目背景与最新决策

### 主界面的含义

游戏启动后直接进入 `Assets/Scenes/Battle.scene`，加载当前进度对应的关卡环境。就绪态没有敌人；玩家点击“开始关卡”才推进正式流程。主菜单画面主体就是即将游玩的关卡环境，选关、天赋是玩家主动进入的其他界面。

### 只包含六项 UI

1. 右上：总铜钱图标与数值。
2. 下半部居中：“开始关卡”主按钮。
3. 左下：一条任务摘要，包含标题、进度与奖励标识。
4. 右下、天赋入口上方：不规则景色关卡入口，含关卡名。
5. 右下：天赋入口卡。
6. 左下底部：四宫格菜单占位键，置灰，无名称，无指定功能。

明确不放：头像、武将／玩家角色、敌人、吉祥物、等级、经验条、体力、星级评价、底部 tab 导航、商城、首充、签到、活动倒计时、红点通知、捕获率、捕捉手势、战斗 HUD、宣传标语。也不添加设置、邮件、背包、家园等未经确认的功能入口。

底部四宫格仅为未定功能占位；“天赋”取代参考图的右下“家园”大入口位置，但不继承家园功能。

### 本轮构图纠偏的来源

用户已在当前对话认可：先依据原图重新分析，再把我们的六项功能与画风组织成整张参考图。

直接查看原图后确认：**原图关卡入口“火山”在右下、家园卡上方；捕捉主操作在屏幕下半部。** 原交接中“关卡入口在屏高 24%—41%、主按钮在 56%—63%”是历史误读，本轮整图生成不得沿用。

关卡入口与天赋卡在右下上下成组，任务条与四宫格在左下成组，中央留给场景和开始按钮。左上允许自然留白，不用新功能补空位。

## 3. 参考图清单与各自职责

以下三张图都已在本会话读图；尺寸已通过文件读取核实。发起生成时必须真正把图像作为参考输入交给生成服务；只在提示词中写本地文件路径并不等于服务收到图片。

| 编号 | Windows 绝对路径 | 实测尺寸 | 唯一职责 |
| --- | --- | --- | --- |
| A | `C:/Users/Administrator/Downloads/微信图片_20261019125149_30_455.jpg` | 1116×2480，9:20 | 学习竖屏构图、场景占主体、主操作在下半部、左下任务、右下关卡与大入口成组的关系。六项功能之外的内容全部舍弃 |
| B | `C:/Users/Administrator/Downloads/微信图片_20260928101136_13_455.png` | 1024×1536，2:3 | 我们的画风与 UI 材质依据：高清像素化三国战场、暖土与冷铁、朱红、墨色笔刷、金属收边、清楚的数字文字。不复制战斗状态、角色、技能栏、血条、连击或暂停按钮 |
| C | `C:/Users/Administrator/Downloads/微信图片_20260928101137_14_455.png` | 1024×1536，2:3 | 我们的环境、材质、光色与战争气质依据：明亮天空、暖土道路、木栅／军旗／建筑、厚重色块。只取环境语言，不把敌军搬入落地页 |

文字规范：`Locus/knowledge/design/art-style-guide.md`。

补充说明：

- A 的软萌造型、糖果色、厚白描边和卡通手势不能成为我们的画风依据。
- B/C 的角色和战争冲突只用于理解世界气质；本张落地页环境无人，不展示战斗。
- 现有六张 `Assets/Sprites/UI/Hub/` 素材均尚未获得用户目视验收，不强制把它们拼入整图，也不据此限制新整图的造型。
- 1011 六帧 Idle 是项目角色标准，但本张图无人，首次生成不必另加角色参考，以免诱导生成角色。
- 本文未提供就绪态游戏底图。首次候选属于画风与 UI 构图参考，不宣称与当前 Unity 场景几何逐像素一致。若用户另提供当前关卡的无 UI 截图，可作为环境构图参考加入；这会改变参考输入，生成前应明确说明。

## 4. 原图的布局与内容分析

坐标采用左上原点，x 向右、y 向下。百分比相对于完整画面，用于读图，不作为已经实测的 Unity RectTransform。

| 原图区域 | 可见内容 | 应继承的构图职责 |
| --- | --- | --- |
| 顶部约 0%—11% | 头像、资源、活动进度 | 小信息靠边，中央视线保持通畅 |
| 两侧约 12%—27% | 商城、礼包、活动 | 边缘布置次级入口；我们没有这些功能，不填充替代入口 |
| 中央约 28%—62% | 火山道路、捕获率、目标生物 | 连续的环境纵深与玩法展示区域；我们的就绪态用空道路与关卡环境承担 |
| 约 65%—78% | 卡牌手势和“捕捉” | 主操作在下半部，以显著尺度和独特轮廓建立焦点 |
| 左下约 80%—89% | 欢迎信息、黄色任务条 | 近期目标摘要；我们只保留任务条，不加公告 |
| 右下约 79%—90% | 火山景色、名称、星级 | 当前关卡标识与切换入口；我们只保留景色与名称，不加星级 |
| 底部约 90%—98% | 四宫格、小按钮、家园大卡 | 左右不对称分组；我们仅放左下四宫格、右下天赋 |

构图核心：远景—道路—近景有完整的视线；主操作在下半部；底部左右两组错落，形成支撑，避免满屏等大的卡片。

## 5. 本次整图构图规格

### 5.1 比例与安全边距

- 主方案画幅：**9:20 竖屏**，设计坐标基准 **1080×2400**。这与 A 的 1116×2480 同比，优先用于验证本轮向原图靠拢的构图。
- 建议最终候选至少 1080 像素宽；更高的同比分辨率可以接受。
- 下表是本次生成的构图目标，属于新的设计规格，不是当前场景的回读数值。
- 主要内容左右保留约 3% 边距，上下保留约 3% 空间。边缘留白只是概念图构图保护区，不等于已经实现任何设备的 SafeArea 适配。
- 9:16 是后续布局适配验证目标；不通过把 9:20 整图纵向压缩来“适配”。底部左右分组应靠底组织，中央环境吸收高度变化。此次无需额外生成一张 9:16 成图。

### 5.2 六项元素的位置与尺度

以下为元素完整视觉包围盒的目标范围，包含边缘装饰与文字。允许小幅度艺术调整，但不得改变分组关系、功能数量或主次。

| 元素 | x 范围 | y 范围 | 尺度／布局重点 |
| --- | --- | --- | --- |
| 总铜钱 | 72%—96% | 3%—6.5% | 右上紧凑横条；铜钱图标在左、白色数值在右；无额外标题 |
| 开始关卡 | 29%—71% | 68%—75.5% | 下半部居中，宽约屏宽 42%；最大字号、最强朱红与明暗对比 |
| 任务条 | 3%—62% | 83%—89% | 左下横向，宽约屏宽 59%；内部为任务标记、标题／进度、奖励标识，保持足够文字面积 |
| 关卡入口 | 74%—97% | 79%—89% | 小型不规则景色剪影加名字带，完整落在天赋卡上方；不变成右侧中部大海报 |
| 天赋入口 | 68%—97% | 90%—97% | 右下横向短卡，约屏宽 29%，宽大于高；大于普通四宫格键，弱于开始按钮 |
| 四宫格占位 | 3%—16% | 90.5%—96.5% | 近方形，小尺寸、低饱和、置灰，不加文字 |

这些范围不会要求任务条和关卡入口横向相交；天赋入口与关卡入口上下保留清楚间隔。不要把旧方案的 260×408 竖长天赋卡搬入本次构图。

### 5.3 中央场景与视觉路径

- 顶部到约 65% 的中央为环境主体，保留可读的天空、远景、道路与两侧层次。
- 道路在画面下部较宽，向远处收束。木栅、军旗、土坡或建筑放在两侧和远处，不把正中央塞满景物。
- 画面是战前就绪态，安静但有战争气质。可以有轻微尘烟与自然环境层次，不表现爆炸、战斗斩击或满屏火星。
- 左上自然保留环境，不放头像，不为左右对称添加无用图标。
- 全图第一操作焦点为“开始关卡”；关卡环境建立世界感，任务提供下一目标，选关与天赋保持次级。
- 底部四宫格与天赋之间允许留空，不建立贯穿屏幕的底栏，也不把三个入口补齐成导航排。

### 5.4 结构示意

```text
┌──────────────────────────────────┐
│                         铜钱 数值 │
│                                  │
│          天空、远景、关卡环境      │
│                                  │
│            开阔道路              │
│                                  │
│                                  │
│          【 开始关卡 】           │
│                                  │
│                         关卡景色 │
│ 【任务摘要／进度／奖励】   关卡名  │
│                                  │
│ 四宫格占位               【天赋】 │
└──────────────────────────────────┘
```

## 6. 统一美术方向与单件造型

### 总体语言

**明亮的暖土三国战场 + 高清像素化色簇 + 墨黑信息承托 + 冷铁／旧铜结构 + 集中的朱红主操作。**

形体有重量，材质清楚，像素化体现在组织过的色块、阶梯轮廓与锐利局部高光。UI 比背景更易读，所有部件共享边缘厚度、光照方向、阴影强度和金属色。

不把整图压暗来表达凶悍；不铺随机噪点来表达像素。场景可以粗粝，正文承托区域要安静。字形保持清楚，不给每个字都做碎像素边缘。

| 部件 | 造型与材质 | 主次要求 |
| --- | --- | --- |
| 场景 | 暖土、赭石道路，冷蓝灰远景和明亮天空；木栅、旧军旗、土坡／城墙等形成三国战争身份；近中远三层 | 环境完整、中央开阔；红旗只作局部识别，不吞没主按钮 |
| 铜钱 | 有厚度的方孔铜钱符号，短墨色承托条，浅色数字，少量铜金高光 | 清楚、紧凑，不做大资源面板，不增加加号充值入口 |
| 开始按钮 | 宽而有力的朱红笔刷形，少量方向性破边，局部暗铁／旧铜收边，厚实浅色中文 | 唯一最强操作色；不画手、不画武将、不用巨型兵器挡住道路 |
| 任务条 | 横向墨色笔刷底，边缘少量旧铜结构；简洁任务标记与小奖励图标；文字区低纹理 | 摘要清楚、有成长目标感；普通状态不发光，不照搬原图的大块亮黄 |
| 关卡入口 | 从同一世界裁下的小块战场景色剪影；地形和地标形成不规则外轮廓，少量旧金收边，下接短墨色名字带 | 图内景物呼应主背景；轮廓清楚可点；无厚方框、无星级和进度徽章 |
| 天赋卡 | 横向深墨短卡，局部铜铁包边；中心为厚实、易读的分叉节点／树状图案，节点少量暖金亮光，下部或侧下方清楚写“天赋” | 保留右下大入口份量，亮度和对比低于开始按钮；不画人物或房屋，不用满卡霓虹光 |
| 四宫格 | 暗铁与灰铜的近方形键，四个简洁方块符号，低饱和、置灰 | 一眼看出占位与未开放感；无标题、锁头、红点、说明文字 |

字体：主按钮可有受控笔势，其余中文与数字采用清楚的粗体。文字、图标不能贴到毛边或高光上。所有部件避免软萌白色粗描边、糖果塑料、繁复宫廷金饰、全屏飘动卷轴等与当前方向不符的语言。

## 7. 文案与信息边界

整图需要有文字来判断真实信息密度。优先让生成模型绘制以下文案；若字形不可靠，后续可用确定性排版覆盖，不能靠编造额外内容解决。

| 位置 | 本次成图文字 | 性质与限制 |
| --- | --- | --- |
| 主按钮 | 开始关卡 | 已确认功能文案 |
| 天赋卡 | 天赋 | 已确认功能文案 |
| 关卡入口 | 长坂坡 | 仅代表一张关卡示例，不决定正式命名，也不代表所有关卡共用同一张图 |
| 铜钱 | 12,380 | 仅为检验多位数宽度的排版样例，不是玩家真实存档数值 |
| 任务标题 | 通关当前关卡 | 仅为单行长度示例，不批准新增任务、任务 ID 或奖励规则 |
| 任务进度 | 0/1 | 仅为进度排版示例 |
| 任务奖励 | 一枚小铜钱图标，无数字 | 只示意奖励槽位；不编造奖励数量和经济数值 |
| 四宫格 | 无文字 | 功能待定，不命名 |

任务条是局外目标摘要，将来可能显示主线、成长、挑战等内容；不把“本关目标”固定成整个任务系统的类别名。具体任务与奖励应另按 `Locus/knowledge/design/task-system-design.md` 决定。

图片中的文字只用于整体设计审阅。正式 Unity 素材阶段应保留空文字底，再由 TMP 叠加动态关卡名、铜钱、任务与进度；本次无需提前制作那些拆件。

## 8. 生成参数意图与输出要求

| 项 | 本轮要求 |
| --- | --- |
| 任务类型 | 多参考图引导的完整游戏界面概念图 |
| 输入 | A 布局参考 + B 画风／UI 材质参考 + C 场景／色彩参考，三图职责不能互换 |
| 数量 | 首次一个完整候选，不是一个画布内拼多个小方案 |
| 构图画幅 | 9:20 竖屏，设计基准 1080×2400；同比分辨率可提高 |
| 模型 | 在实际生成环境中选择支持参考图输入和该构图需求的图像模型；本文不假定具体接口已可用 |
| 质量 | 用户现有偏好为 `quality=max`。如果实际供应商不支持该字段或枚举，先说明并核对等效最高档；不能伪造字段或静默降档 |
| 背景／透明 | 完整不透明场景，PNG；不需要透明、不需要抠图 |
| 输出 | 干净的单张成图，无手机外壳、无展示台、无方案说明、无尺寸标线、无水印 |
| 后期 | 不拉伸、不对整图套像素滤镜；中文需要修复时，用已获授权的字体／排版工具补字并保留原生成图 |

### 实际接口与尺寸适配

1080×2400 是构图目标，并非承诺所有服务都接受这一 `size` 参数。发起者必须读取实际工具 schema，以真实支持的参数提交。

若工具只能输出 2:3 等比例，不能因为 B/C 是 2:3 就直接把它当成交付比例，更不能把短图拉长。应在请求前明确给出可行处理方式及其对构图的影响，并取得对应批准。目标是保住 9:20 的布局与完整六项元素，不静默裁掉顶部资源或底部入口。

### 后续文件位置建议

在实际项目根目录下新建独立目录，不覆盖 v1：

`H:/Project/threeKingdomSlayer/threeKingdomSlayer/_art_process/hub_ui_reference_v2/`

建议由实际生成会话创建：

- `hub_ui_reference_v2_generated.png`：服务原始输出，保留不覆盖。
- `hub_ui_reference_v2_review.png`：提交用户审阅的完整候选；如做过裁切或文字修复，记录处理过程。
- `hub_ui_reference_v2_prompt_zh.txt`：实际提交对应的中文版本。
- `hub_ui_reference_v2_prompt_en.txt`：实际提交对应的英文版本。
- `hub_ui_reference_v2_manifest.md`：实际模型／参数、参考图次序与职责、原始和最终尺寸、处理记录、用户验收状态。避免记录密钥、令牌或私有服务凭据。

这些是未来生成交付约定；本轮尚未创建此目录或上述产物。生成后优先向用户提供可粘贴到资源管理器的绝对路径，并直接展示最终候选图。

## 9. 完整中文正向提示词

以下是完整中文语义版本。若服务主要采用英文，使用下一节英文版本作为主要提交文本；二者是同一方案的两个语言版本，不需要重复请求两次。

```text
请使用提供的三张参考图，为一款三国战场动作游戏制作一张完整的竖屏主菜单／落地页 UI 参考效果图。最终画面为 9:20 竖屏，设计基准 1080×2400。只做一个完整界面，画面铺满，不带手机外壳、展示背景、说明标注或水印。

三张输入图承担不同职责：
参考图 A 是露米娅大陆的竖屏游戏截图，只参考构图关系和界面信息层级：大面积真实玩法环境、下半部中央的主操作、左下任务条、右下关卡入口以及它下方的大功能入口。不要继承它的软萌美术、角色、手势、商业化内容或多余按钮。
参考图 B 是我们的三国战斗界面概念图，用于确定高清像素化画风、墨色笔刷承托、金属收边、冷铁与暖土的对比、朱红与金白的强调，以及清楚有力的文字。不要复制其中的战斗 HUD、技能栏、血条、暂停、连击、角色或攻击特效。
参考图 C 是我们的三国战场概念图，用于确定环境的厚重体积、暖土道路、冷蓝灰远景、明亮天空、军旗、木栅与城池建筑的材质与色彩。不要复制其中的人物、敌人、武器攻击或战斗场面。

这是玩家开游戏后、按“开始关卡”之前看到的画面。场景已经加载，但没有任何人物、敌人或战斗。中央是一条从近景向远处延伸的开阔战场道路，两侧有克制的木栅、军旗、土坡或古代军事建筑，天空明亮，近中远景层次清楚。画面具有三国战争身份、厚重感与动作游戏的直接感，环境仍然明亮可读。主体环境占据中央与上半部，正中道路保持贯通，不用人物、巨型兵器或装饰挡住视线。

全图只有六个 UI 元素，必须按下列位置组织。百分比以左上角为原点，x 向右、y 向下，表示元素完整范围。

1. 右上总铜钱：x 72%—96%，y 3%—6.5%。小型方孔铜钱图标，左图右数，短墨色承托，白色数字“12,380”。紧凑清楚，不加其他资源、加号或标题。左上保持环境留白。

2. 下半部居中“开始关卡”按钮：x 29%—71%，y 68%—75.5%。这是全图最明确的操作焦点。采用宽而有力的朱红笔刷轮廓，少量方向性破边，局部暗铁和旧铜收边，厚实清楚的浅色中文“开始关卡”。它拥有全图 UI 中最强的朱红、最大字号与最强明暗对比。不要手持卡牌、武将或巨型武器造型。

3. 左下任务条：x 3%—62%，y 83%—89%。一条足够宽的横向墨色笔刷信息条，边缘少量旧铜结构，内部有小型任务标记、清楚的任务标题“通关当前关卡”、进度“0/1”和一枚小铜钱奖励图标，奖励不写数量。标题和进度可分为两行。文字承托区平整、低纹理、高可读。它是任务摘要，不是宽大的任务面板，不添加额外公告。

4. 右下关卡入口：x 74%—97%，y 79%—89%，位于天赋卡上方。它是一小块被裁下来的战场景色剪影，地形和同一世界的关卡地标共同形成不规则外轮廓，少量旧金边使它从主背景中分离。底部连接一条短墨色名字带，写清楚“长坂坡”。造型清楚可点，与主背景景物呼应。不要星星、评价、数字进度、徽章或厚重矩形大框，不要移到右侧中部。

5. 右下天赋入口：x 68%—97%，y 90%—97%。横向、宽大于高的深墨短卡，少量铜铁包边，中心或主体为厚实、易读的分叉节点／树状图案，少量节点发出暖金亮光，清楚写“天赋”。它比普通小按钮更有份量，但亮度和视觉对比低于“开始关卡”。不画人物、房屋或家园，不用纤细精灵树和霓虹光。

6. 左下四宫格占位键：x 3%—16%，y 90.5%—96.5%。近方形暗铁灰铜按钮，四个简洁方块符号，低饱和、置灰。没有文字、没有指定功能、没有锁头或红点。它与右侧天赋入口之间自然留空，不添加其他图标，不建立底部 tab 栏。

UI 统一使用墨色笔刷底、暗铁、旧铜和少量金色高光。朱红主要集中在开始按钮，其他入口克制。整体采用高清像素化色簇组织、清楚的阶梯边缘、厚实的形体和锐利局部高光；禁止用随机噪点或对普通插画套马赛克滤镜来制造像素感。所有 UI 共享光照方向、边缘重量与阴影强度。场景可以粗粝，文字区域保持安静。主按钮文字可有受控笔势，其他中文和数字用清晰粗体，不要满屏书法。重要 UI 保留边缘留白，不贴边、不互相遮挡。

整张图应让人先感受到正在进入的关卡世界，同时第一操作焦点落在“开始关卡”；随后能读到任务、找到关卡入口与天赋。请输出完成度高、材质统一、构图可信的单张完整游戏界面概念图。
```

### 中文负面提示词

```text
人物，武将，主角立绘，士兵，敌人，人群，吉祥物，头像，等级，经验条，体力，捕获率，捕捉手势，手持卡牌，巨型武器遮路，商城，首充，充值加号，活动，签到，倒计时，通知红点，星级，评价徽章，底部 tab 导航，多余功能入口，设置，邮件，背包，家园，战斗 HUD，血条，技能栏，连击，暂停按钮，爆炸，大范围斩击，满屏火星，软萌 Q 版，糖果色，塑料感，厚白描边，仙侠飘逸水墨，精灵树，霓虹科幻，宫廷式繁复金饰，全画面黑灰，照片写实，随机碎像素噪点，马赛克滤镜，所有按钮等大，所有按钮同时发光，厚重面板堵满场景，右侧中部大型关卡海报，竖长天赋卡，假中文，乱码，额外标语，元素重叠，文字越界，裁掉 UI，手机外壳，展示台，多方案拼版，标注线，水印。
```

如果实际服务没有独立 negative prompt 字段，将上述约束合入同一个提示词末尾，不凭空增加 API 参数。

## 10. Full English Positive Prompt

```text
Using the three supplied reference images, create one complete portrait main menu / landing screen concept for a Three Kingdoms battlefield action game. The intended final composition is 9:20 portrait, with a design canvas of 1080 × 2400. Show one full game screen filling the canvas, without a phone frame, presentation background, annotations, or watermark.

The three references have distinct roles:
Reference A is the portrait game screenshot from Lumia Continent. Use it only for composition and information hierarchy: a large visible gameplay environment, a primary action in the lower central area, a task strip at lower left, and a stage entry above a larger feature entry at lower right. Do not inherit its cute art style, characters, hand gesture, monetization features, or extra buttons.
Reference B is our Three Kingdoms combat UI concept. Use it for the high-definition pixel-art rendering language, black ink-brush information backings, metal edging, cool iron against warm earth, vermilion and gold-white accents, and bold readable typography. Do not copy its combat HUD, skill bar, health bar, pause button, combo display, characters, or attack effects.
Reference C is our Three Kingdoms battlefield concept. Use it for substantial forms, warm earthen roads, cool blue-gray distance, bright skies, military banners, wooden barricades, and ancient military architecture. Do not copy its people, enemies, weapon attacks, or battle action.

This screen is the moment after the game has loaded the current stage, before the player presses Start Stage. There are no people, enemies, or combat. An open battlefield road extends from the near foreground into the distance. Restrained wooden barricades, military banners, earthen banks, or ancient military architecture sit along its sides. Keep the sky bright and distinguish foreground, middle ground, and distance. The world feels weighty, martial, and direct while remaining bright and readable. The environment occupies the center and upper screen. Keep the central road visually continuous; do not block it with a character, oversized weapon, or ornamental centerpiece.

Include exactly six UI elements, arranged as follows. Percentages use the top-left as the origin, with x increasing to the right and y increasing downward. Each range describes the complete visual bounds of that element.

1. Total coins at upper right: x 72–96%, y 3–6.5%. A small thick bronze coin with a square hole, followed by the white number “12,380” on a short black ink backing. Keep it compact. No other resource, plus sign, or heading. Leave the upper-left area open to the environment.

2. The primary “开始关卡” button in the lower center: x 29–71%, y 68–75.5%. This is the clearest action focus of the entire interface. Use a broad, forceful vermilion brush-shaped silhouette with a few directional broken edges and restrained dark iron and aged bronze edging. Render the exact Chinese label “开始关卡” in substantial, clearly readable light-colored lettering. Give it the strongest vermilion accent, largest UI type, and highest UI contrast. Do not use a card-holding hand, warrior, or oversized weapon.

3. A task strip at lower left: x 3–62%, y 83–89%. Use a sufficiently wide horizontal black ink-brush backing with restrained aged bronze edge details. Include a small task marker, the exact task title “通关当前关卡”, progress “0/1”, and one small coin reward icon with no reward amount. The title and progress may occupy two lines. Keep the text area calm, low-texture, and readable. This is a compact task summary, not a large task panel. Do not add an announcement strip.

4. A stage entry at lower right: x 74–97%, y 79–89%, immediately above the talent card. Shape it like a small cutout of the battlefield scenery. Terrain and a recognizable landmark from the same environment form an irregular outer silhouette, separated from the main background by restrained aged-gold edging. Attach a short black ink name band at its base with the exact Chinese name “长坂坡”. Make the entry clearly recognizable as clickable and visually related to the background. No stars, ratings, numerical stage progress, badges, or heavy rectangular frame. Do not move it to the middle-right area.

5. A talent entry at bottom right: x 68–97%, y 90–97%. Use a horizontal short card that is wider than it is tall, with a deep ink-black body and restrained bronze and iron edging. Its main symbol is a substantial, legible branching node or tree diagram with a few warm golden illuminated nodes. Include the exact Chinese label “天赋”. Give it more presence than a small utility button, while keeping its brightness and contrast below the Start Stage button. No character, house, home feature, delicate elven tree, or neon glow.

6. A disabled four-square menu placeholder at bottom left: x 3–16%, y 90.5–96.5%. A nearly square dark iron and gray-bronze key containing four simple square symbols. It is desaturated and visibly disabled. No text, assigned function, lock, or red notification dot. Leave natural open space between this key and the talent card; add no other icons and no bottom tab navigation bar.

Unify the UI with black ink-brush backings, dark iron, aged bronze, and restrained golden highlights. Concentrate vermilion on the primary button. Render forms using deliberate high-definition pixel-art color clusters, readable stepped contours, substantial volume, and sharp local highlights. Do not imitate pixel art with random noise or a mosaic filter applied to a conventional painting. All interface elements share a consistent lighting direction, edge weight, and shadow strength. The environment may be rugged, but text backings remain calm. Allow controlled brush character on the primary label; use clear bold typography for other Chinese text and numbers. Keep important elements inset from the canvas edges and prevent overlap.

The image should immediately establish the world of the stage, with the first action focus on “开始关卡”. The task summary, stage selection entry, and talent entry should be easy to locate afterward. Deliver one polished, coherent full-screen game interface concept.
```

### English Negative Prompt

```text
People, warriors, player character illustration, soldiers, enemies, crowds, mascot, avatar, player level, experience bar, stamina, capture percentage, capture gesture, card-holding hand, giant weapon blocking the road, shop, first purchase offer, recharge plus sign, events, daily sign-in, countdown, red notification dots, stars, ratings, rating badges, bottom tab navigation, extra feature entries, settings, mail, inventory, home feature, combat HUD, health bar, skill bar, combo display, pause button, explosions, large slash effects, excessive sparks, cute chibi styling, candy colors, plastic material, thick white outlines, ethereal xianxia ink painting, elven tree, sci-fi neon, ornate imperial gold decoration, uniformly dark gray image, photorealism, random pixel noise, mosaic filter, equally sized buttons, every button glowing, heavy panels filling the environment, large middle-right stage poster, tall vertical talent card, gibberish Chinese, malformed text, extra slogans, overlapping elements, overflowing text, cropped UI, phone frame, presentation mockup, multiple concepts in one sheet, annotation lines, watermark.
```

If the actual service has no separate negative prompt field, append these constraints to the main prompt. Do not invent unsupported API fields.

## 11. 生成后的验收与返工方式

### 第一关：构图与功能边界

- [ ] 只有一张完整界面；六项元素齐全，数量与功能符合本文。
- [ ] 原图关卡位置误读已纠正：关卡入口在右下、天赋入口上方；主按钮在下半部中央。
- [ ] 无角色、敌人、头像、等级、经验、体力、星级、底部 tab 或商业化按钮。
- [ ] 中央道路有完整纵深；UI 不把关卡环境切成碎片。
- [ ] 主按钮是最清楚的操作焦点；天赋与关卡入口不会因面积或发光抢主按钮。
- [ ] 左下任务条有足够文本宽度；与右下关卡入口不相交。
- [ ] 四宫格无名称、功能、红点或锁头，确实置灰。
- [ ] 顶部资源与底部入口完整可见，没有出界或裁切。

任何一条结构性条件失败，应先修构图与内容。不得因为材质漂亮就当作整图通过。

### 第二关：画风统一

- [ ] 与 B/C 属于同一个三国战场美术世界：暖土、冷铁、朱红、块面和重量感成立。
- [ ] 世界明亮、有空气层次；没有全屏黑灰、软萌塑料、霓虹或仙侠化。
- [ ] 像素色簇服务形体，边缘干净，没有随机噪点和整体马赛克滤镜。
- [ ] 六个 UI 元素共享墨色、铜铁、光向、边缘重量与阴影关系。
- [ ] 任务条和关卡名承托区平静，文字易读；金色高光有主次。
- [ ] 关卡小景与大背景属于同一个地点／视觉世界，没有互相矛盾的地标与气候。

### 第三关：文案与交付真实性

- [ ] “开始关卡”“天赋”“长坂坡”“通关当前关卡”“0/1”“12,380”可读，没有额外文字。
- [ ] 已明确铜钱、任务、进度是排版示例，不把示例写成实际数值或上线规则。
- [ ] 中文若被修复，保留原始产物，记录修复方式；不把未经修正的乱码交成最终版。
- [ ] 实际成图文件可打开，尺寸和比例经过回读；记录真实模型与提交参数。
- [ ] 直接展示最终候选，并给出 Windows 绝对路径；“已生成”必须对应实际成功结果。
- [ ] 未导入 Unity、未覆盖现有素材；用户目视验收状态写明“待验收”或实际反馈。

### 返工方式

按用户反馈确定一次改动的目标，例如先修按钮位置，再修材质。每轮保留前版与实际提示词；局部编辑可能重绘其他区域，回读整张图确认六项布局仍成立。任何新增功能、改变参考图职责或主要参数，都应在请求前说明。用户尚未验收时，不自动进入素材拆分和场景部署。

## 12. 与既有文档的关系与本轮完成范围

- 本文是本次获同意的“整张落地页参考图”生成准备入口。
- `Locus/knowledge/plan/hub-layout-rework-20261024.md` 仍记录历史实现和返工。本次仅在该文档顶部增加指向本文的补充说明，不篡改其场景快照或把新构图写成已经落地。
- `Locus/knowledge/plan/main-menu-hub-refactor-plan-20261020.md` 提供启动与 overlay 的工程背景，其中旧 tab、头像等方案不作为本次功能清单。
- `Locus/knowledge/design/art-style-guide.md` 是画风规范。
- `Locus/knowledge/design/task-system-design.md` 是任务语义与架构背景；本文的样例文案不是任务系统实施。

本轮完成标准：本文保存成功并回读；旧布局交接顶部能找到新入口；未调用生成服务、未生成图、未修改场景／脚本／素材。本轮完成不代表整图已经生成或用户已经验收。
