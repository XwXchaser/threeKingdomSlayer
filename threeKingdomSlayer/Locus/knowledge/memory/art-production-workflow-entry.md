---
id: kd_b3807e87-49f5-4d81-8c3b-cfb2348653ac
summary: 角色制作/生成工作流唯一入口：S0-S5 阶段表（需求→风格规格→身份图→动作视频→抠图落位→接线验收）、每阶段必读的技术规范与易漏项、开工铁律与工具速查；避免遗漏读取规范并控制生成成本。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 角色制作 / 生成工作流（美术素材）

> **这是美术素材工作的唯一入口**。任何"做角色 / 做动画 / 重新生成敌人形象 / 出视频 / 抠图落位 / 接 Unity"的会话，**先读本文件**，再按阶段读它指向的规范；不要凭记忆开工，也不要只看角色任务书就动手。

## 0. 开工铁律（先对一遍）

1. **先分析后实现**：用户没说"开始做 / 实现"就只出方案，不动文件（`memory/user-preference.md`）。
2. **生图 / 视频必须先审批**：提交**完整中文 + 英文提示词、负面词、参考图职责、尺寸/质量/透明度/时长**等关键参数，等明确批准；参数或提示词实质变化要**重新报批**。
3. **GPT Image 一律 `quality=max`**（编辑类再叠 `input_fidelity=high`），不降档（`skill/gpt-image-generation.md`）。
4. **一个动作只生成一次**，失败先分析、不自动重试；`create` 响应丢失时用 `--resume`，绝不重发（`skill/reachapi-seedance-video-generation-v2.md`）。
5. **不自行追加需求**：需求没写的（额外画面表现、阈值、兜底、时长）只能列为建议让用户拍板（`memory/user-preference.md`）。
6. **阶段闸门**：上一阶段的产物没被用户认可，不进入下一阶段的付费生成。**身份图没验收就做视频＝白花钱**。
7. **写完必须回读核对**（脚底/中心/高度/alpha/哈希），不能凭工具回执宣布完成（`memory/tool-result-verification.md`）。
8. **收尾给一条明确回复**：完成了什么、验证结果、未完成/阻塞、下一步要用户做什么（`memory/work-loop-discipline.md`）。
9. **交付本地素材先给 Windows 绝对路径**，并区分"最终候选目录"与"仅供预览/追溯的目录"。
10. **角色动画部署只接到第一关**，由用户开游戏验收；不自动进 Play Mode 自检（`memory/user-preference.md`）。

## 1. 阶段总览（谁在什么时候读什么）

| 阶段 | 目标 | 必读文档 | 产出 | 闸门 |
|---|---|---|---|---|
| **S0 需求与边界** | 弄清做哪个角色/哪个动作、能不能动已验收资产 | `memory/user-preference.md`；该角色的任务书（如 `plan/enemy-commander-buff-unit-plan.md`） | 范围确认 | 用户明确"开始做" |
| **S1 风格与规格基线** | 定死画风、比例、朝向、动作语义、交付硬要求 | `design/art-style-guide.md`；`design/pixel-character-and-enemy-animation-spec.md` | 角色设定卡 | 用户认可设定卡 |
| **S2 身份图 / 锚图** | 产出可当视频首帧的身份图 + 绿幕锚图 | `skill/gpt-image-generation.md`；`memory/visual-library.md`；角色任务书 | 身份图 + 锚图 + 量测 | **用户验收身份图** |
| **S3 动作视频** | 每个动作一支视频 | `skill/reachapi-seedance-video-generation-v2.md`；`memory/enemy1012-art-replacement-pipeline.md` | mp4 + 逐帧量测 | 用户验收视频；**一次只做一个动作** |
| **S4 抠图·选帧·落位** | 变成可入库的精灵帧 | `memory/enemy1012-art-replacement-pipeline.md`（§3 落位口径 / §5 写精灵 / §9 工具）；pixel spec §5 §6 §10 | PNG 帧序列 + 落位记录 | 逐帧回读核对 |
| **S5 接线与验收** | 入 Unity、接 Animator/Prefab | 角色任务书的工程附录；`memory/known-issues.md` | 资产接线完成 | **用户开游戏验收**（不自动自检） |

## 2. 各阶段要点与**易漏项**

### S1 风格与规格基线（最常被跳过的一步）

- `design/art-style-guide.md`：读 §1 核心定义、§2 两类参考的职责（概念图 vs 已验收角色）、§3 角色设计语言、§9 共同验收标准。画风基准以**已验收角色**为准，不是旧素材。
- `design/pixel-character-and-enemy-animation-spec.md`：读 §5 动画帧通用硬性要求、§6 位置与比例锁定、§7 动作语义、§8 推荐视频流水线、§9 视频提示词限制、§10 质量检查清单。
  - **易漏 ①**：§6 "按最大动作展开范围预留安全边距，建议四周至少 15%"。带长道具（旗杆、长枪）的角色要先算**道具最大行程**，再决定画幅与人物在画幅里的占比——旗杆类角色用 1:1 会让旗尖出画（实测见 `plan/enemy-commander-buff-unit-plan.md` §2.15.10.1）。
  - **易漏 ②**：§7 每个动作的语义。Idle 不许迈步/扬尘；Attack 是「起手→蓄力→命中→收势」且收势要能回 Idle；Dead 是自身失力坠落，**不等于被击飞**；Hitted 必须有身体反应而不只是表情。
  - **易漏 ③**：§5 每帧独立 PNG、真 Alpha、装备完整不贴边、同一动作画布与锚点统一。
- **通用规范 vs 角色设定冲突**时（例如通用 Dead 语义 vs 角色任务书里"旗手倒地要旗杆旗面倒地"）：**先问用户**，不要自己选一边。

### S2 身份图 / 锚图

- 先查视觉库：`memory/visual-library.md`（`index.csv`）找已有资产、实机截图、候选图，别在仓库里翻图。
- 工具链：`Tools/visual_brief/submit_1013_identity.py`（单角色固定规格）/ `submit_enemy_proportion_lineup.py`（多图研究图）；提示词正文与参考图清单外置在 `Tools/visual_brief/prompts/`（**唯一真源**，改词就改文件）。
- 参考图职责要在清单里逐张写清；`image[]` 顺序有意义。
- 生图后**先量测再加工**：比例/落位用 `Tools/visual_brief/compose_identity_greenscreen.py`（绿幕口径：1024²、绿 (0,159,62)、主体高 696、脚底 895、中心 474.5）。
- **易漏**：去背/合成后要**回读** alpha、脚底、中心、高度；研究图（比例对照图）**不能**直接当身份图或游戏素材加工。

### S3 动作视频

- **先找参考，再写动作（2026-11-08 用户定；2026-11-09 修订来源）**：新动作**先找成品参考**，把参考给用户看过，再据参考写动作描写与提示词；参考落地 `EnemyXXXXArtSource/00_Reference/`，来源与许可记进任务书。**来源优先「游戏实机截图或立绘」**（三国题材游戏里同类兵种的实机画面、角色立绘）；**Wikimedia 等实拍照片库对本项目无参考价值，不要用**。**不要凭想象编动作。**
- 路线选择（`skill/reachapi-seedance-video-generation-v2.md`）：
  - **有验收过的动作母本** → 替换视频工作流（`reference_image` + `reference_video`），动作从母本继承，提示词**只写形象层**（标准句 24 字，别扩写）。
  - **新动作**（无母本）→ 首尾帧工作流 + **可量化幅度**的文字描述；已知风险是幅度失控（实测 4.5×），必须写上限数字。
  - `reference_video` 与 `first_frame/last_frame` **不能混用**；跨原型换角色时替换路线会"形象回退成参考片的人"，要用首尾帧锚图路线。
- 色彩一致性走**三级门槛** G1 输入片基 / G2 输出片基 / G3 交付物颜色，各自给数值可判；输入端绿幕不保证输出片基一致。
- **易漏**：时长必须等于参考视频时长；`aspect_ratio`/`resolution` 影响**像素高度**；轮询要限次；成片要抽帧看位置/比例/超框/装备一致性，**不能只看回执**。
- 表情：母本里脸可见就随母本继承，**不要另发表情片**；新动作的**起手表情必须等于已验收 idle 的长相**。
- 叠轮会掉锐度（3.71→3.33→3.14），**一个动作只生成一次**。

### S4 抠图·选帧·落位

- 抠图：`Tools/visual_brief/chroma_key.py`（T=79 + halo 去绿边 + 二值 alpha）。**视频不保证透明**，逐帧都要抠。
- 落位口径见 `memory/enemy1012-art-replacement-pipeline.md` §3/§9（idle：k=0.92366、脚底 897、中心 521；新角色**必须实测**，不许直接套用）。
- 选帧：步态用周期配对（§9.1）；攻击类用峰值/DP；**通用兜底是掩码配准**（新片 vs idle 掩码 IoU 求 dx/dy）。
- **易漏**：PIL 的 bbox 右/下是 exclusive，缩放后最底行可能透明 → 要按**可见底行**对齐；`convert("RGB")` 会把透明区变黑底；写精灵必须保留 alpha；**只覆盖 PNG 字节、保留 .meta**（GUID/导入设置不变）；写完回读。

### S5 接线与验收

- 导入设置沿用已验收角色（spriteMode 1 / PPU 16 / Point / 无压缩 / alphaIsTransparency=1 / maxTextureSize 2048），最终以实测为准。
- pivot 与落位**不要默认沿用**：idle 与 attack/dead 的 pivot 常常不同。
- 部署只做到"接入第一关 + 配好动画"，然后通知用户自检（`memory/user-preference.md`）。

## 3. 角色任务书 / 历史经验（按角色取用）

| 场景 | 文档 |
|---|---|
| 1013 旗手（当前角色） | `plan/enemy-commander-buff-unit-plan.md`（顶部状态 + §2 画风/身份 + §4 交付 + §5 工程附录） |
| 四兵种并排比例研究图 | `memory/enemy-proportion-lineup-reference.md`；`plan/…` §2.15–§2.15.10 |
| 管线级经验（表情/选帧/落位/抠图/写精灵） | `memory/enemy1012-art-replacement-pipeline.md` |
| 已知问题与调试步骤 | `memory/known-issues.md`；`plan/unresolved-issues.md` |
| 视觉库索引 | `memory/visual-library.md` |

**新角色的任务书写法**：沿用 1013 任务书的结构（角色特征与机制 → 画风与身份图方案 → 参考图职责与参数 → 交付物与验收 → 工程附录），顶部写清"当前状态 / 已否决方案 / 最新任务"，避免下个会话把旧方案当现决。

## 成本控制建议（视任务适用，非强制执行计划）

- 区分聊天模型上下文 token 与外部生图 / 视频接口返回的 usage；接口不返回价格时不推算费用，不能把两种 token 直接相加当作账单。
- 先明确交付物和验收目标，必要时参考四兵种并排图确认体型方向；再逐个进行角色身份图、动作和正式素材。并排图不能直接当 Sprite。
- 避免重复读取整篇历史、相同原图及完整响应；先看索引 / 摘要，再按需读取具体章节，必要时用一张有标识的对照图比较。对身份和细节验收仍需看清原图，不能为省 token 牺牲判断。
- 优先本地做文件核对、抽帧、去背与对照；外部生成前展示用户要求的中英文提示词、负面词、参考图职责和关键参数，并按其授权执行，失败先分析而非自动重试。高质量参数、现有验收流程与 Unity 使用边界不得擅自降低或扩大。
- 只汇报当前候选、已确认事项、未完成事项与下一决定；历史日志、原文件、完整词和请求保留路径，必要时再读取。

**参考资产的持久性**：`C:/Users/steam/Pictures/gptGen/enemy_proportion_lineup_v1/` 是本机生成档案；`Library/Locus/tmp/` 属于可清理的临时目录；新会话或另一台机器使用前须验证路径与文件存在，不把任一目录默认视为已同步。
