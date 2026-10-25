---
id: kd_8b99933c-1736-455b-a630-3cbb06e8a62f
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: '何时加载：要给 1011 风格锁定刀兵出图/做动作视频、查 1011 新形象定稿（v11）或核对已部署攻击帧时读本文件；何时忽略：只关心 v1–v10 的历史演进、旧视频弯路或逐版提交台账时直接读 plan/enemy1011-style-locked-dao-generation-archive.md。活文档只含 §0 入口、§5 调用纪律、§23 v11、§24 攻击动画、§25 新形象定稿。'
---

# 1011 风格锁定刀兵：生成交接

> **本文是活文档**：只保留 §0 入口、§5 调用纪律、§23 v11、§24 攻击动画、§25 新形象定稿。
> §1–§4、§6–§22（v1–v10 立绘与旧换人视频的逐版记录）已原文拆到 `plan/enemy1011-style-locked-dao-generation-archive.md`，**编号未变**，需要追溯时按原编号去查。
>
> 拆分记录：2026-10-24 由 1936 行 / 142,877 字节拆为 315 行活文档 + 1634 行归档；拆分前全文备份在 `Library/Locus/tmp/handoff_backup_before_split.md`。拆分只搬段落，未改任何结论文字。

## 0. 给发起侧的入口

**最新结论（2026-10-24）：1011 的新形象已定稿为 v11。** 具体包括两件事：① 角色立绘以 §23 的 v11（SHA `67da5838…b2c39`）为准，已登记进 `design/art-style-guide.md`（见 §23.8）；② 攻击动作已用 v11 形象重生成并部署——成片 `dao_v11_attack_v1_4s_720p.mp4`、16 帧已按 1011 尺寸定稿并覆写 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_attack1..16.png`（见 §24、§25）。

**当前存在的不一致（先看这条）：** 1011 目前只有 attack 是 v11，`idle / walk / hit / hitLeft / hitRight / dead / rise / landing / getup` 共 9 组、每组 5–6 帧仍是旧 1011 美术。游戏里会看到“新兵出招、旧兵待机”。下一步应按 §25.4 的顺序把这些动作逐个换成 v11，而不是再改画风。

历史方向不再继续：v8 大方块、v9/v10 碎切面、v1–v4 的甲结构、v5 的平滑立绘、v6 的内部方格。v7 只作形象来源，其平滑画法不作为基准。没有新的生成任务获批，§15–§17 旧视频路线不重发。

发起任何新版本前必须复述：唯一输入、参数、实际提交文本、输出目录，并取得当次明确“发起生成”。一次只提交一个请求；失败、超时、结果不明、棋盘格或身份偏离时保留记录并停下；不自动重试、不降档、不换模型、不改词再发。图片规则不可外推到视频：视频另有费用确认、限次轮询与下载校验（见 §15）。

本方案不连做去背、选帧、抠图、对齐、补边、Clip 或 Unity 接入。通过前不替换 `Assets/Sprites/Enemy/Enemy1_1/`、Prefab 或 Animator。

**§16 交付的是生成方案与提交文本，视频由用户在别处发起，本侧不调用生成接口。** 本侧在 §16.4 只负责背景色的 G2 数值验收；表情、动作、身份、尺寸由用户目视验收。

身份与面相的历史交接仍是 `plan/enemy1011-face-identity-generation-handoff.md`。本文不改那份状态，也不把 §27 脸图或 §21 六兵种组图当成输入。

## 5. 参数

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[]: ONLY enemy1011_idle1_green_input.png
prompt: section 6 English full prompt + blank line + section 6 English negative prompt
read timeout: 900 seconds
automatic retries: 0
```

- 不传 `background`。制作底用词约束为均匀不透明纯绿 `RGB(0,159,62)`，本阶段不请求透明。
- 不使用独立负面字段。负面词接在正文后，中间一个空行。
- 凭据只从环境变量 `MUSK_API_KEY` 读取。不得写入脚本、请求记录、响应摘录、文档或回复。
- 不先调用付费探针。模型可用性如需核对，只用免费 `GET /v1/models`。
- 15 秒内泛化 HTTP 400，或读超时、断连、504，都立即停止。不换 `gpt-image-2`，不降到 `quality=high`。
- 网关节点会随机掐断空闲连接。拿不到图只保留 `.status.json`，不在同一次授权里重发。

## 23. v11：按 N1 破盾的材质画法重绘 v7

### 23.1 目的与改动范围

**状态：2026-10-24 已生成，并获用户角色画风确认。** 下文保留当时的准备方案，实际提交见 §23.7、用户验收见 §23.8；不因本节历史参数和词仍在而再次发起。

本轮不再靠暖色或增加颗粒模拟画风。目标是：先读到完整形体，笔触顺着材料与结构走，细节疏密有变化，集中在转折、边缘和连接处。

- 金属：完整的受光面、清楚的边缘厚度和集中反光，不铺满锤凿石面似的小切片。
- 布：用大褶皱解释袖子、衣摆的体积，不把布面画成多边形拼片或木纹。
- 皮肤：简洁的脸面与手部体积，不复制金属磨损、石材颗粒或密集小切面。
- 色彩：保留冷灰铁甲、麻色衣服和红色识别布；不把全身统一染黄。
- 形象：保留 v7 的敞脸盔、红头巾、方铁牌、麻绳、短护腕、环首刀、战斗脸、短壮比例和姿势。保留的是设计，所有表面允许重画。

画法参考换用 `Assets/Experiments/CurvedScroll/Art/EnvironmentV2/Props/N1/N1_BrokenShieldArrows_v1.png`：盾心、铁边和铆钉可直接对照头盔、胸牌、护腕的金属表现。J1 木篱、N3 水井只供本地并排验收，本次不继续加参考图。只换一个画法参考，便于判断结果。

不沿用 v10 的多边形切面，不回到 v8 的大方块，也不保留 v7 的平滑卡通表面。本节是候选方案，不代表画风已验收，不修改 `design/art-style-guide.md`。

### 23.2 两张实际输入与顺序

`image[]` 必须正好两张，顺序固定：

```text
1. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\n1_broken_shield_green_style_input.png
   406x368 RGB, Color Type 2, 138,371 bytes
   SHA-256 38d6031e18754c01cf2fd435ec28031ef9ed010132adc03210027eacad4484ea
   职责：只提供 N1 已接入道具的材质画法、硬边笔触、厚度和反光组织；不提供盾牌、箭或人物装备设计。

2. C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v7\enemy1011_style_locked_dao_v7.png
   1024x1024 RGB, Color Type 2, 1,331,292 bytes
   SHA-256 ff8ae3594cd0e7cc5af43cc89a357014d3c2c05a2f7b6be843fb758ba0f385be
   职责：只提供角色设计、服饰、武器、比例和姿势；不锁定平滑卡通表面，所有表面允许重绘。
```

第一张副本来源：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Assets\Experiments\CurvedScroll\Art\EnvironmentV2\Props\N1\N1_BrokenShieldArrows_v1.png
406x368 RGBA, 222,107 bytes
SHA-256 f38d8d56491037d6b3aa0eca5ea012d67a84b94b0de69507af058c3543583874
```

原图实测是真实 Alpha：83,733 个透明像素、65,675 个不透明像素、半透明像素 0。绿幕副本只将透明区域填为 `RGB(0,159,62)`，原尺寸不变；已从落盘文件逐像素回读，65,675 个可见像素 RGB 全部保留，原资产哈希未变。查看器的棋盘格不是原图内容，不可把它当作实际背景。

发起前复核两张实际输入的哈希。不要上传原 RGBA、J1 木篱副本、1011、v8、v9、v10、截图或视频帧；不重新放大或采样参考图。不要把盾牌和箭画进角色图。

### 23.3 参数与授权

```text
endpoint: POST https://api.muskapis.com/v1/images/edits
content-type: multipart/form-data
model: gpt-image-2.5-sunburst
size: 1024x1024
quality: max
input_fidelity: high
output_format: png
n: 1
background: omit
image[] order:
  1. n1_broken_shield_green_style_input.png
  2. enemy1011_style_locked_dao_v7.png
prompt: section 23.4 English prompt only
read timeout: 900 seconds
automatic retries: 0
```

参数沿用已执行字段组，本次没有在线核验接口，也没有调用免费或付费探针。本版不加独立负面词，不降低质量或保真。凭据只从环境变量 `MUSK_API_KEY` 读取，不写入记录或回复。

发起侧先复述两张输入及顺序、参数、§23.4 全文和输出路径，取得当次明确“发起生成”后只提交一次。失败、超时、结果不明都记录并停止，不自动换图、换词、换模型或重发。`input_fidelity=high` 不保证比例或局部像素不变，以实际结果验收。

### 23.4 实际提交文本

沿用本轮对话交付的方案文本。本次“更新交接文档”只授权写文档和准备参考副本，不代表已提交生成。中文供检阅，不提交。

按第一张图的游戏素材画法，重新绘制第二张图的士兵，保留他的服装设计、比例和姿势。用有方向、疏密变化的硬边笔触表现形体。金属保持完整受光面、清楚的边缘厚度和集中反光；布料通过大褶皱表现，皮肤简洁。保留冷灰铁甲和麻色衣服。不要把全身画成小多边形、方块或平滑卡通，也不要画出盾牌和箭。

```text
Repaint the soldier in image 2 in the game-asset drawing style of image 1. Preserve his costume design, proportions and pose. Use directional, hard-edged brush marks with varied size and density. Give metal continuous surfaces, clear edge thickness and concentrated highlights; describe cloth with broad folds and keep skin simple. Retain cool-gray iron and the hemp-colored tunic. Avoid an all-over pattern of small polygon facets, square blocks or smooth cartoon shading. Do not include the shield or arrows.
```

实际 prompt 只取上面唯一英文代码块。不要拼接旧版词、额外负面词、“暖光＋颗粒”或限制明暗阶数；也不要自动改写本段。

### 23.5 输出与记录

```text
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.request.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.response.json
C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.status.json
```

同目录只有已准备的 `n1_broken_shield_green_style_input.png`；它不是生成结果。提交前检查四个结果/记录文件不存在，不覆盖已有内容。

按 §7 保存实际 prompt、文本哈希、输入顺序/完整哈希、参数、提交次数、HTTP/状态、时间、输出尺寸/字节数/哈希；响应保留原始 JSON，去除凭据与签名 URL 敏感内容。原图完整下载解码，回读后再报告落盘。未返回的费用不推断。

本版仍要求单个完整角色、纯绿制作底、全身和刀完整、脚底不裁切；以输入图保持构图，不把这些额外要求偷偷追加到 §23.4。若实际背景或构图偏离，记录偏差供用户验收，不本地重画后冒充原始返图。

### 23.6 验收重点

与 v7、v10、N1 破盾、J1 木篱和 N3 水井并排，以目视为主：

- **金属**：盔面、胸牌、护腕是完整金属曲面和有厚度的边缘，集中反光；不是整片石材般的小切面。
- **布料**：大褶皱能解释袖子和衣摆，纹理有疏密和方向；不是多边形拼片，也不是复制木纹。
- **皮肤**：脸和手保留简洁体积，不铺碎切面、不做塑料式平滑高光；战斗脸仍可读。
- **形象与比例**：仍是 v7 的短壮刀兵，服饰、持刀方向和姿势保持；没有盾、箭、篱笆或其他场景物件。
- **场景协调**：在相近显示大小下，角色与已接入素材的结构、边缘和材质处理协调；不能仅凭变暖、颗粒增多或高频数上升判通过。
- **完整性**：画布 1024x1024，背景接近均匀纯绿，全身、双手、双脚和刀完整入画。

用户目视验收决定是否通过。若仅变色，仍是密集切面/大方块/平滑卡通，或身份、比例漂移，则保留结果并停下；不自动重发，不进入视频、选帧、去背、补边或 Unity 接入。

### 23.7 2026-10-24 v11 提交记录（已提交一次；角色画风已获用户验收）

按 §23.3–§23.5 提交了一次，未重试、未降档、未换模型、未传 `background`、未加负面词。

| 项 | 实测 |
| --- | --- |
| 输入顺序 | 1 = `n1_broken_shield_green_style_input.png`（406×368，SHA `38d6031e…484ea`、138,371 B，四角 `(0,159,62)`，画法）；2 = `enemy1011_style_locked_dao_v7.png`（SHA `ff8ae359…852be`、1,331,292 B，形象）。提交前均复核 |
| 参数回显 | `model=gpt-image-2.5-sunburst`、`quality=max`、`size=1024x1024`、`background=auto`（未传）、`n=1` |
| prompt | 仅 §23.4 单块，510 字符，SHA-256 `c2a8342b21304a6760849d2a1f8aa3e8092340aa9118002eb6bdf8b1e517093c` |
| HTTP | 200，耗时 41.36 s；usage 8,434（image 1,120 + text 290 → out 7,024） |
| 输出图 | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png`，1,598,516 B，1024×1024，签名通过，Color Type 2，SHA-256 `67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39` |
| 记录文件 | 同目录 `.request.json`、`.response.json`、`.status.json`；原 `n1_broken_shield_green_style_input.png` 未动 |

底色：四角 `(3,153,71)`，均匀无棋盘格。

主体包围盒 774×884（v7 711×805、v10 742×882）——**三版新图里主体最大**；留白 左 7.7% / 右 16.7% / 上 8.1% / 下 5.6%。未裁切，但纵向占 86% 画布，上下留白均低于 15% 惯例。

**画风代理指标（只报数，不代判定）**

| 图 | 内部高频（水平/垂直，每千内部像素） | 主体唯一色数 | 主体像素 |
| --- | --- | --- | --- |
| 1011 idle | 289.2 / 284.8 | 77,828 | 164,200 |
| v7 | 137.2 / 147.7 | 91,824 | 284,318 |
| v10 | 216.8 / 209.5 | 158,946 | 325,649 |
| **v11** | **179.8 / 187.8** | **126,660** | 357,353 |

即 v11 相对 v10 高频下降、色数下降（从“密集切面”往回收），仍高于 v7。该指标不能区分“笔触疏密”与“噪点”，以目视为准。

并排对照（另行生成，非原图修改）：`Library\Locus\tmp\dao_v5_repl_video\v11_vs_v10_v7_n1shield.png`（左→右 N1 破盾、v7、v10、v11，等高 520 px）。

**提交备注**：先前一次相同命令因调用格式损坏未真正执行，未产生请求也未计费；本记录对应的是 20:22 实际提交的那一次。

### 23.8 2026-10-24 用户角色画风确认与规范登记

用户明确表示：“很好，当前这版就是我们要的画风，总结角色画风特征到美术文档中”。**v11 角色静帧画风通过，不再处于候选／待画风验收状态。**

已更新 `design/art-style-guide.md`：

- §2.3 登记原图路径、规格、完整哈希与验收范围，v11 成为后续角色具体绘制的已验收样本。
- §3.1–§3.3 总结短壮厚重比例、结构与装备层级、分材质塑形及成年敌兵战斗神态。
- §4.3–§4.4 固化有方向、疏密变化的硬边色簇、深色结构边、大体块优先和成功提交词先例；允许顺着体积的局部折面，不允许规则方格、全身统一碎切面或平滑塑料卡通。
- §5.2 与 §12 说明冷灰铁、暖麻布、棕皮绳和红色识别的关系，以及 N1/J1/N3 指定已接入样本的材质协调职责；撤销“只有 1011 是角色画法依据／全部 EnvironmentV2 都是旧素材”的过时限制。

这是角色画风确认，不自动批准新生图、新动画、全兵种同装备或资源替换。§24 的动作、表情时序、帧间一致性与实机大小仍按用户对应验收处理；本次只写文档，没有改图片、Unity 资源或场景。

## 24. 攻击动画 v1：v11 形象 + 1011 源片动作（2026-10-24 已提交一次，待目视验收）

### 24.1 决策与输入

用户 2026-10-24 确认三项：① 做身份图归一化副本；② 表情沿用 v3 的“顶刀点窄幅呲牙、劈砍段闭口”；③ 动作源就用 `2b1888f6….mp4`。

| 角色 | 素材 / 哈希 |
| --- | --- |
| `reference_video`（动作＋镜头） | `C:\Users\steam\Videos\doubaoVideo\2b1888f6388ed56b14da731eb0aeb229.mp4`，817,576 B，SHA-256 `f624106031c57b…bfa5c9`（与 v1/v2/v3 字节相同） |
| `reference_image`（身份＋服饰＋金属画法） | `C:\Users\steam\Pictures\gptGen\enemy1011_attack_anim_v1\input_identity_v11_696.png`，1024×1024，432,162 B，SHA-256 `544b35842302752968d57db8ddd7d408ac3a20043086fb00130705c2033f8da6`；由 v11（`67da5838…`）等比 0.78733 缩到主体高 696、脚底 896，四角 `(0,159,62)` |

归一化依据：实测参考图里主体占画布的比例会被带到成片（v5 立绘 0.78 → v1 成片 0.798，而目标源片为 0.68），v11 原图是 0.863，所以先缩到 0.68 再上传，避免“生成完才缩”的二次重采样。

### 24.2 参数与提交

`seedance-2-fast` / 4 s / 720p / 1:1 / 无音频 / 无水印 / seed −1；不传 `background`；prompt 为六段英文（动作锁定、v11 身份、金属画法、尺寸与地线、v3 表情阶梯、背景与禁令），**3,677 字符，SHA-256 `b52a14f095c640529f6eff2a2f025775f6d5d5a94ea6a46d7588da52eac2fff7`**，原文落盘 `Library/Locus/tmp/dao_v5_repl_video/dao_v11_attack_v1.prompt.txt`。

- Task `task_b80657b7a0354a15a5b8a09bdc981708`，`create_attempts=1`；usage 173,700，`cost.spend=0.57321`（条件 `has_video_input=true`、`720p`）。
- 轮询 2 轮共 16 次拿到 `success`，观察等待 123.3 s；下载 177.38 s。

### 24.3 成片与实测

`C:\Users\steam\Videos\doubaoVideo\dao_v11_attack_v1_4s_720p.mp4`，2,011,964 B，SHA-256 `b32eabc7ce4e5db9b1420aa6397fe38c8a9be096e09e50c3345a204b3b578009`；960×960 / 24fps / 97 帧 / 4.042 s / h264 / 无音轨；`ftyp`、box、全帧解码通过。

| 项 | 本版 v11-attack-v1 | 参考（v1/v2/v3） |
| --- | --- | --- |
| 背景 ΔR/ΔG/ΔB 相对 `(0,159,62)` | 0 / **−18…−7** / −6…−2（最差 f011） | v1 −27…−5、v2 −26…−19、v3 −28…−7 |
| 首/中/尾背景 | f000 `(0,152,58)`、f024 `(0,142,59)`、f096 `(0,143,56)` | — |
| 主体质心 vs 源片 | 均差 31.2 px，峰值 84 px（f049） | v1 均 18 / 峰 71 |
| 主体高 vs 源片 | 均 1.10（0.888–1.212）；首帧 653 vs 593 | v1 1.216 |
| 口腔代理峰值（对 v5 闭嘴基线） | **+73**，无超阈窗口 | v2 +4,176、v3 +5,081 |

对照图：`Library\Locus\tmp\dao_v5_repl_video\attack_v1_src_vs_out.png`（上排源片、下排新片，f000/012/024/036/048/060/072/096）。数据：`attack_v1_analysis.json`。

**待你判定的三点**：① 动作均差 31 px、峰值 84 px 比前几版大，是否还能接受；② 主体仍偏大约 10%，是否按 §24.1 的同样的方式在 16 帧上统一缩；③ 口腔代理只 +73、无窗口，即呲牙在本版几乎读不出来（可能太弱，也可能基线口径不同），需目视确认。

### 24.4 尚未做的

未做 16 槽位抽取、未抠图、未缩放定稿、未替换 `Assets/Sprites/Enemy/Enemy1_1/`、未改 clip 或 controller。当前游戏里跑的还是 v3 换人片的 16 帧（k=0.77832 版）。

### 24.5 槽位 → 抠图 → 定稿 → 接入（2026-10-24 已完成）

**一、槽位**（`v11_attack16/v11_attack16_manifest.json`）：成片自身 34 个保持段，取 medoid 后按 1011 节奏吸附，16 个均单调、互不重复；编号对应 a01 f010、a02 f019、a03 f027、a04 f035、a05 f043、a06 f049、a07 f051、a08 f053、a09 f055、a10 f061、a11 f065、a12 f067、a13 f069、a14 f074、a15 f078、a16 f082。底边非单一：站立/收势段 841–845，挥砍段 a06–a09 到 871–883（查低点为刀尖：f051 底行仅 2 像素且偏右 115 px；源片同段也从 777–779 下陷到 849–857，属继承）。

**二、抠图**（SVL Chroma，绿幕 T=79、softness 16、halo 1、半透明转不透明）：16 帧中位——可见 188,274、被删 15.4%、绿>8 434、半透明 0、幕布残留 0、孔 0。产物：`v11_attack16_matting/Attack/keyed_ui_t79/`（16 张 960×960 RGBA），报告 `v11_attack16_matting/matte_report_t79.json`。

**三、定稿缩放与落位**：键控帧均值高 686.7 → `k = 0.94410`（目标旧 1011 均值 648.3）；`dx = 74 + (960−nw)/2`，站立底线对齐 896，挥砍下陷保留。结果：16 帧均值高 **648.1**（对旧 648.3 = 1.000），站立底边 896/897，挥砍 913–933；逐帧比例 0.835–1.143 是姿态差异，非缩放错误。产物：`v11_attack16_final/`（16 张 1108×1108）+ `v11_attack16_final_manifest.json`。

**四、接入**：覆写 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_attack1..16.png`，`.meta` 未动；Unity 重导入后抽查 attack1/5/6/9/16 均为 `1108×1108 → 1024`、spritePPU 14.787、bounds 69.25、pivot 512,512；`Enemy_1011_Attack` 仍 18 键 / 17 张 / 2.933 s / Clamp / 30fps。

**备份**：本版替换前的 v3 版 16 帧在 `Library\Locus\tmp\backup_Enemy1_1_v3k0778_20261024\`；原始 1011 在 `Library\Locus\tmp\backup_Enemy1_1_20261024-170945\`。

**对照图**：`Library\Locus\tmp\dao_v5_repl_video\v11_attack16_deployed_vs_original.png`（16 对，左原始 1011、右 v11）。

**待用户在游戏内验收**：动作、表情、身份，以及与 1011 同框的实际大小。

## 25. 1011 新形象定稿（2026-10-24 用户确认）

### 25.1 定稿内容

**1011 的角色形象以 v11 为准。** 包含两件事，缺一不可：

1. **立绘基准**：`enemy1011_style_locked_dao_v11.png`，画风已登记进 `design/art-style-guide.md`（§23.8）。
2. **已部署的攻击表现**：v11 形象的攻击 16 帧已进 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_attack1..16.png`（§24.5）。

### 25.2 定稿资产清单

| 作用 | 路径 | 规格 / 哈希 |
| --- | --- | --- |
| 立绘基准（源头） | `C:\Users\steam\Pictures\gptGen\enemy1011_style_locked_dao_v11\enemy1011_style_locked_dao_v11.png` | 1024×1024，1,598,516 B，`67da58389ed877ce7183b98ad06245c7ddc39eba79ce055fc3792d7e0cbb2c39` |
| 视频用归一化身份图 | `C:\Users\steam\Pictures\gptGen\enemy1011_attack_anim_v1\input_identity_v11_696.png` | 1024×1024，432,162 B，`544b35842302752968d57db8ddd7d408ac3a20043086fb00130705c2033f8da6`；主体高 696、脚底 895、底色 `(0,159,62)` |
| 攻击成片 | `C:\Users\steam\Videos\doubaoVideo\dao_v11_attack_v1_4s_720p.mp4` | 960×960 / 24fps / 97 帧 / 4.042 s，2,011,964 B，`b32eabc7ce4e5db9b1420aa6397fe38c8a9be096e09e50c3345a204b3b578009` |
| 16 槽位原帧（绿幕） | `Library\Locus\tmp\dao_v5_repl_video\v11_attack16\` | `v11a_attack01_f010.png` … `v11a_attack16_f082.png` + 两张核对图 + manifest |
| 抠图产物 | `…\v11_attack16_matting\Attack\keyed_ui_t79\` | 960×960 RGBA，T=79 / softness 16 / halo 1，半透明/幕布残留/孔均为 0 |
| 定稿 sprite | `…\v11_attack16_final\` | 1108×1108，k=0.94410，均值高 648.1 |
| 已部署 | `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_attack1..16.png` | `.meta` 未动；PPU 14.787（导入 1024）、bounds 69.25、pivot 512,512 |

### 25.3 尺寸基准（以后所有动作沿用）

- 站立底线：画布行 **896**（Unity 侧导入后 895/896 两像素抖动属抗锯齿）。
- 主体均值高：**648.1 px**（旧 1011 的 648.3，比值 1.000）。
- 落位规则：`dx = 74 + (960 − nw)/2`；先按 `k = 648.3 / 键控均值高` 统一缩，再对底。
- 挥砍段允许向下超出 30–40 px（刀尖），这是源片继承的动势，不做逐帧对齐。

### 25.4 剩下 9 组动作的推荐顺序

现状：`idle / walk / hit(5) / hitLeft / hitRight / dead / rise / landing / getup` 均为旧 1011 美术，与新攻击同屏会明显不一致。

1. **idle 优先**（6 帧）：它是敌人待机常态，不换则“新兵只在出招时存在”。
2. **walk**（6 帧）：移动时占屏时间最长。
3. **hit / hitLeft / hitRight**（5+6+6）：打击反馈直接关系手感。
4. **dead / rise / landing / getup**：击飞链路，可最后做，但必须同一批风格。

每组都走同一配方：找对应动作的源片 → 上传 `reference_video` + 本文 §25.2 的归一化身份图 → 参数同 §24.2（`seedance-2-fast / 4s / 720p / 1:1`）→ 成片后按 §24.5 四步（槽位 → T=79 抠图 → k 缩到 648.3 → 落位 896）→ 覆写并备份。

若无对应源片，先做 **6 帧 idle**：可从 v11 立绘出 1 个 4 秒循环，再按 6 槽位取帧——但要事先确认循环首尾能接上。

### 25.5 不再使用的方向与回退

- 不重跑：v1–v4（甲结构）、v5（平滑）、v6（内部方格）、v7（平滑本体）、v8（大方块）、v9/v10（碎切面）。
- 回退层级：`backup_Enemy1_1_v3k0778_20261024`（v3 换人版）→ `backup_Enemy1_1_20261024-170945`（原始 1011）。

### 25.6 本次定稿未覆盖的事

`Prefab`、`Animator`、`.meta` 导入设置、clip 时长与命中帧未改；idle/walk/hit/dead 未换；未选帧、未进 Play Mode。画风定稿不等于全动作已验收。

## 26. 待办：用 v11 重做 1011 其余 9 组动作（2026-10-24 立项，**尚未发起任何生成**）

> **本组当前状态（2026-10-25 收尾）**：三种受击**全部已用 v11 形象完成并验收**——正面受击是**新动作**（`HitFlash` 状态改指新建的 `Enemy_1011_HitHeavy`，7 键 1.05 s），左/右受击是**替换**。**最终状态表、命名陷阱、选帧闸门、预览包规范、备份位置、遗留事项，全部集中在 §26.11–§26.12**；§26.1–§26.9 是本组开工前的计划与背景（保留作历史对照），部分内容已被实际结果取代。
>
> **⚠ 2026-10-25 晚间变更（本文后续所有「1011」均指当时的 v11 产物）**：这一整套 v11 产物已**整体改名为 1012** 并独立入库（`Assets/Sprites/Enemy/Enemy1012/` + `Assets/Animations/Enemy_1012_*.anim` + `Enemy_1012.controller`），**1011 已还原为原本造型**（精灵与控制器回到仓库 HEAD，`Enemy_1011_hitHeavy*` 已不存在）。**1012 的上传与部署交接以 `plan/enemy1012-v11-art-handoff.md` 为准**；本文保留 v11 一轮的完整过程与实测数值，作为 1012 的制作溯源。

### 26.1 范围（attack 除外）

| 组 | 要换的 sprite | 对应 clip | 时长 / fps / wrap | clip 实际用到 |
| --- | --- | --- | --- | --- |
| idle | `idle1..6`（6） | `Enemy_1011_Idle` | 0.6 s / 30 / Default | 6 |
| walk | `walk1..6`（6） | `Enemy_1011_Walk` | 0.6 s / 30 / Default | 6 |
| hit | `hit1..5`（5） | `Enemy_1011_HitFlash` | 0.9 s / 30 / Default | 5 + `idle1` |
| hitLeft | `hitLeft1..6`（6） | `Enemy_1011_HitLeft` | 0.9 s / 30 / ClampForever | 1–5 + `idle1` |
| hitRight | `hitRight1..6`（6） | `Enemy_1011_HitRight` | 0.9 s / 30 / ClampForever | 1–5 + `idle1` |
| dead | `dead1..6`（6） | `Enemy_1011_Dead` | 0.6 s / 30 / Clamp | 6 |
| rise | `rise1..6`（6） | `Enemy_1011_Launched_Rise` | 0.6 s / 30 / ClampForever | 6 |
| landing | `landing1..6`（6） | `Enemy_1011_Launched_Fall` | 0.6 s / 30 / Loop | 6 |
| getup | `getup1..6`（6） | `Enemy_1011_Launched_Getup` | 0.6 s / 30 / ClampForever | 6 |

合计 **53 个 sprite**。注意 `hitLeft6` 与 `hitRight6` 存在但**未被任何 clip 引用**，是否一并换由用户定。

### 26.2 为什么必须先做

attack 已是 v11，其余 9 组仍是旧 1011，同屏会出现“新兵出招、旧兵待机/跑/受击/死亡”。

### 26.3 动作源（开工前第一个决策）

已核：`C:\Users\steam\Videos\doubaoVideo\` 下 69 个 mp4 中**只有 attack 有 1011 源片**（`2b1888f6….mp4`），其余 9 组没有对应源片。

- **方案 A（定格源片）**：先用旧 1011 对应 sprite 拼一段定格源片（本地零成本，动作与现有 clip 严格一致），再走 `reference_video` + v11 归一化身份图。适合 walk / hit / hitLeft / hitRight / dead / rise / landing / getup。
- **方案 B（纯图生成）**：只用 `reference_image` 出 4 秒。动作自由度高，但会引入与现有 clip 不同的动作，且循环/首尾必须验。仅建议 idle（或 idle 也走 A 保持零风险）。

### 26.4 固定配方（不得改）

- 参数：`seedance-2-fast` / 4 s / 720p / 1:1 / 无音频 / 无水印 / seed −1；不传 `background`；一次授权只发一个任务。
- `reference_image`：`C:\Users\steam\Pictures\gptGen\enemy1011_attack_anim_v1\input_identity_v11_696.png`（SHA `544b3584…f8da6`）。**例外**：dead / landing / getup 这类非站立构图需另做一张同规则归一化副本（主体高 696、接触线按该动作实际接触点）。
- 后处理四步：① 在**成片自身**取保持段 medoid、时间单调、去重（对应槽位数）；② SVL Chroma 绿幕 T=79 起手 + `softness 16` + `halo 1` + 半透明转不透明；③ `k = 648.3 / 键控均值高`；④ 落位 `dx = 74 + (960−nw)/2`，站立动作用底线 896。
- 命名与接入：覆写同名 `Enemy_1011_<group><i>.png`，`.meta` 不动；备份到 `Library/Locus/tmp/backup_Enemy1_1_<group>_<日期>/`；Unity 重导入后抽查 PPU 14.787 / bounds 69.25 / pivot 512,512，并确认对应 clip 键数不变。

### 26.5 顺序与费用

idle → walk → hit → hitLeft → hitRight → dead → rise → landing → getup。每条视频历史价 ≈ `0.573`，9 条 ≈ `5.2`；若 idle 走 B 方案另需首尾帧验证。

### 26.6 纪律

一次授权一个任务；发起前复述输入、参数、提交词、输出目录并取得当次明确批准；失败/超时/结果不明保留记录并停；不自动重发、不降档、不换模型。

**一个动作只允许一次生成。禁止把上一轮成片再当 `reference_video` 叠加重塑形象或表情。** 改脸、改底色、改动作都要回到原始源片 + 原始身份图重新提交一次。实测依据与写法见 `skill/workflows/character-hit-animation-video-workflow.md` 第 13 节（清晰度逐轮下降 3.71 → 3.33 → 3.14，且第二轮只能改第一轮已画对的部分）。表情必须写进第一次提交的同一份词，写成跟随源片动作节拍的时间性表演；「只改脸」类增量指令无效。

### 26.7 记录规则（防止文档再膨胀）

每组只在本文加 **一行** 结果（task、费用、成片 SHA、部署日期）；逐帧指标、失败指纹、早期方案写到 `plan/enemy1011-style-locked-dao-generation-archive.md` 或该组的 request/status json。不再复制历史版本表格。

### 26.7.1 已完成结果（2026-10-25；**注意：下表的 1011 产物已于当晚改名为 1012，1011 已还原原造型**）

| 组 | task | 费用 | 成片 SHA | 状态 |
| --- | --- | --- | --- | --- |
| idle | `task_7340f7c493d144a0a5cd5b5ad2d2c000` | 0.57321 | `c1d8adbc…7c78` | **已部署并验收**：6 帧覆写 `Enemy_1011_idle1..6.png`，旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_idle_20261024/` |
| walk | `task_ddd396265a014eb3ac2e67fd48e5e586` | 0.57321 | `8fd620c2…0075` | **已部署并验收**：取 v1 的 f22/24/26/28/30/32 覆写 `Enemy_1011_walk1..6.png`，旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_walk_20261024/` |
| dead | `task_c519de4b4591455aaa60177dd319609e` | 0.57321 | `b38f0d49…97baa` | **已生成并部署（2026-10-25 用户验收）**：成片 `C:/Users/steam/Videos/doubaoVideo/dao_v11_dead_v1_4s_720p.mp4`（2,554,738 B）；6 帧覆写 `Enemy_1011_dead1..6.png`，旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_dead_20261025/`；槽位 f025/f035/f049/f057/f065/f077；k=0.93077（站立参考 f010 对齐 idle1 主体高 605），单一全局缩放 + 单一贴图偏移 (106,172) |
| rise | `task_b7721e8b226e4988a26b47bd8bbcfebb` | 0.57321 | `8d34f043…f372` | **已生成并部署（2026-10-25 用户验收）**：成片 `C:/Users/steam/Videos/doubaoVideo/dao_v11_rise_v1_4s_720p.mp4`（2,132,813 B）；6 帧覆写 `Enemy_1011_rise1..6.png`，旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_rise_20261025/`；**按旧美术的动作帧选取重选**（f049/f050/f052/f056/f070/f097，与 `00_Index/frame-provenance.csv` 一致）；k=0.93077、贴图偏移 (106,172)（与 dead 同基准） |
| landing | `task_b0c668d56042492da5eeba0907c468e6` | 0.57321 | `fde58ff3…ebde` | **已生成并部署（2026-10-25 用户验收）**：成片 `C:/Users/steam/Videos/doubaoVideo/dao_v11_landing_v1_4s_720p.mp4`（2,140,093 B）；6 帧覆写 `Enemy_1011_landing1..6.png`，旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_landing_20261025/`；**按旧美术的动作帧选取重选**（f001/f006/f010/f015/f020/f097）；**继承整条链的全局 k=0.93077 + 贴图偏移 (106,172)**（本组无站立帧，不另立基准） |
| getup | `task_870751286af54048a795478a9208b0a9` | 0.57321 | `193de9e5…1a66` | **已生成并部署（2026-10-25 用户验收）**：成片 `C:/Users/steam/Videos/doubaoVideo/dao_v11_getup_v1_4s_720p.mp4`（2,373,297 B）；6 帧覆写 `Enemy_1011_getup1..6.png`（原为旧美术），备份 `Library/Locus/tmp/backup_Enemy1_1_getup_20261025/`；**按旧美术的动作帧选取重选**（f001/f016/f031/f046/f061/f097）；链上全局 k/偏移；站立帧与 idle1 同尺度（610 vs 605，脚底 898、中心 521） |

**dead 本轮要点（2026-10-25）**：
- **源片未换**：`Enemy1011ArtSource/01_SourceVideos/sword_enemy_dead_front_v4.mp4`（1,960,796 B，SHA `3a3924c2…545f6b`）作 `reference_video`；其首帧与 attack 源片 `2b1888f6…` 逐像素几乎相同（同角色同机位），所以走的是与 attack 同一套「源片 + 身份图」配方。
- **身份图为 dead 专用副本**（§26.4 规则）：`Library/Locus/tmp/dao_v11_dead/input_identity_v11_dead_A696_ground830.png`（SHA `40fb5b37…4c8e8`）—— 主体高 696 不变（与基准图逐像素同尺寸，只平移 66 px），接触线 896→**830**（= 源片脚底线 778 在 1024 画布上的位置）。rise / getup 可复用同一张；**landing 需要另做**（该动作全程在空中，接触线是躺姿地面线）。
- **G2 背景**：逐帧四角中位对基准键 (0,159,62) 的 ΔR 0…15 / ΔG **−7…0** / ΔB 2…12（最差 ΔG f014），比 idle(−21…−13)/walk(−15…−9)/attack(−18…−7) 更贴键；偏离 >25 的像素 max **91**/帧（idle 122 / walk 553 / attack 237），亮像素 **0**。
- **抠图**：T=79 / softness 16 / halo 1，holes 0、leak 0、semi 0、green>8 452–735 px。
- **落位**：k=0.93077、整帧等比缩到 894×894、统一贴图偏移 (106,172)，站立帧脚底 898 / 主体中心 x=521（与 idle1 一致）；**没有逐帧重锚**，尸体相对站立位的几何关系与源片、旧美术一致。
- **模型偏差（用户已接受）**：脸被画成**大张嘴的痛呼**（词里写的是嘴闭着、不露齿、不仰头）——与 hitLeft/hitRight 的「大张嘴龇牙」同类偏差；死亡场景读作临死呼喊，验收通过。
- **工具链**：`Library/Locus/tmp/dao_v11_dead/`（`submit_dead_v1.py` / `poll_download_dead_v1.py` / `build_dead_frames.py`；抠图用 `C:/sprite-video-lab-models/venv/Scripts/python.exe`）；预览包 `preview_dead/`（00..05.png + anim.gif 100 ms/帧 + 尾帧保持 300 ms + contact.png + manifest.json）。

**rise 本轮要点（2026-10-25）**：
- **源片**：`Enemy1011ArtSource/01_SourceVideos/enemy1011_launch_rise_v1.mp4`（1,913,855 B，SHA `9748a773…8a93`）；身份图复用 dead 的副本 A（`40fb5b37…4c8e8`）——**rise/getup 可复用，landing 需另做**（该动作全程在空中）。
- **命名核对**：rise/landing/getup 三个源片与游戏动作**一致**（rise=站立→被击飞；landing=空中→下落触地；getup=躺姿→起身站定），不存在 hit 那种反向命名。
- **G2 背景**：ΔR 0…2 / ΔG −22…−10 / ΔB −9…−2（最差 ΔG f014）；偏离 >25 像素 max 106/帧，亮像素 0。
- **抠图**：holes 0 / leak 0 / semi 0 / green>8 210–574；**源片自带的接触阴影被 T=79 键掉**（站立帧键控后底边 777，而绿幕遮罩看到的是 785 —— 判「是否离地」要用键控 alpha，不要用绿幕遮罩 bbox）。
- **落位**：k=0.93077、894×894、贴图偏移 (106,172)，站立帧脚底 898 / 中心 x=521。
- **刀的轨迹（用户 2026-10-25 指出）**：f050–f080 刀**在手里**（手握环首、刀身前指），**f085 起脱手**、在空中单独翻飞，到 f097 仍是分离状态 —— 与源片及旧美术一致。缩略图容易误判，判刀要逐帧放大看。
- **击飞链的首尾帧连贯（2026-10-25 核对，方案已改）**：
  - **旧链本来就是连续的**：`enemy1011_launch_rise_v1.mp4` 的末帧 f097 ≡ `enemy1011_launch_landing_v1.mp4` 的首帧 f001（bbox 全同：830×538/底 702/中 473/顶 164，逐像素平均差 3.31/通道 = 编码噪声）；`landing` 末帧 f097 ≡ `enemy1011_launch_getup_v1.mp4` 首帧 f001（861×318/底 672/中 480/顶 354，差 3.49）。**所以 v11 替换链天然连贯**（两节替换的是同一张源图），**不需要也不应该改用首尾帧工作流**。
  - **落地后的核对方法**：v11 landing 出片后，拿它的首帧与 v11 rise 的真尾帧比（同一张源图，应几乎一致），这就是“首尾帧连贯”的验收。
  - **尾帧锚点（不重新上色）**：`Library/Locus/tmp/dao_v11_rise/continuity/rise_tail_f097_raw.png` = rise 成片真实末帧（f097），**保留它自己的底色**（实测 (0,140,58)）；可选的「去接触阴影」变体 `rise_tail_f097_noshadow_sameplate.png` 也只合回**同一个底色**。**首版把它重合成到批次基准键 (0,159,62) 是错的**：这一批 v11 新视频的真实底色是 G≈141–152，换键会与前后片对不上（用户 2026-10-25 指出）。
  - **landing 专用身份图**：`Library/Locus/tmp/dao_v11_rise/input_identity_v11_landing_A696_ground717.png`（主体高 696 不变、接触线 896→**717** = landing 源片里躺姿地面线 672 在 1024 画布上的位置；底色 (0,159,62)，与前几组身份图同键）。rise / getup 仍用副本 A（脚底 830）。
  - **不要用首尾帧工作流做替换**：`first_frame`/`last_frame` 不能与 `reference_video`/`reference_image` 混用；而且替换的含义是“动作永远来自已有源片”。

补充事实：idle 源片用 `Enemy1011ArtSource/01_SourceVideos/sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4`（灰底，需在词里要求换成绿）。walk 源片用 `sword_enemy_walk_front_v4.mp4`（自带绿底但 G≈160–172），实测节拍为 f0–9 静立 / f10–88 步态 / f89–96 静立，步幅约 10.5–11 帧。成片底色逐次偏低（idle ΔG −21…−13，walk ΔG −15…−9），按每帧实测键色抠图可避免把偏差带进 sprite。

**walk 部署细节**：按旧 walk 的节奏轮廓反查同构帧——旧帧脚底线 `875, 906, 884, 874, 905, 893`、刀位 `345, 341, 222, 143, 132, 304`；v1 选 f22/f24/f26/f28/f30/f32（间隔全 2，跨度一个步幅），刀位 `256, 254, 112, 51, 50, 250`。缩放 k=0.92561，按旧槽位逐槽对齐脚底线（新高 `620, 633, 622, 621, 626, 625` 对旧 `619, 638, 618, 616, 639, 615`），均值 624.5 对 624.2。导入设置 PPU 16 / Point / max 2048 / pivot (0.65, 0.55) 与原 walk 相同，clip 仍 0.6 s / 30 fps / 6 键。**第 6 槽刀位左边距 345 对旧 304，是六槽里最松的一槽。**

**身份图**：用原图 `input_identity_v11_696.png`（底 `0,159,62`，SHA `544b3584…f8da6`），不要用染绿副本 `input_identity_v11_696_plate138.png`（底 `0,138,59`，SHA `6033001b…fd7f`）——实测输出底色比请求值低 9–21，词里写 `RGB(0,159,62)` 实测落到 G≈138–146，偏高反而更接近批次键。

**walk 表情三次尝试的结论**：v1 无表情 → v2 改嘴（写了“下颌松开”→ 静立时张嘴）→ v3 只传视频（嘴闭住但仍无变化）。用户放弃表情路线，采用 v1 的冷脸。**代价是清晰度逐轮下降**，教训见 §26.6 与 `skill/workflows/character-hit-animation-video-workflow.md` §13。

**补记（2026-10-25）**：walk 其实还生成过 **v4**（`dao_v11_walk_v4_4s_720p.mp4`，SHA `7bfa4a59…a98cb`，01:11），它才是 attack 配方的一次性提交——回原始源片 `763084a9` + 身份图 + 每步两半的表情阶梯，**逐帧脸确有可见差异**，但**未部署**（部署管线是照 v1 写的，`build_walk6.py` 第 11 行 `CLIP = dao_v11_walk_v1_4s_720p.mp4`）。所以“walk 表情试过且失败”的说法不完整：链式的 v2/v3 失败，**一次性配方（v4）没有失败，只是没被采用**。v4 的逐帧脸对照图在 `Library/Locus/tmp/dao_v11_walk_v4/walk_faces_lineage.png`（第 1 行=已部署的 v1，第 3 行=v4）。

### 26.10 新动作：v11 正面受击（hitHeavy，2026-10-25 已生成并接入，**定稿见 §26.11**）

**背景**：旧正面受击两处不满足要求——① 动作太轻微；② 旧源片角色戴**全封闭大盔**，根本没有脸可演。实测旧源片 `sword_enemy_hit_front_v2.mp4` 主体高波动仅 **Δ40 = 6.6%**（同角色 attack 源片 `sword_enemy_attack_front_v2.mp4` 是 Δ184 = 30.4%），而本方法逐帧锁死身体运动，**提示词改不动幅度** → 必须换动作源片。本组**不用源片**，改走纯生成词。

| 项 | 实测 |
| --- | --- |
| task | `task_178c011a4c1746a48d358e0e9e960e96`（HTTP 200，create_attempts=1） |
| 费用 | `cost.spend=0.7308`，usage 130500，`has_video_input=false` / 720p |
| 成片 | `C:\Users\steam\Videos\doubaoVideo\dao_v11_hitfront_v1_6s_720p.mp4`，3,067,539 B，SHA `6a92b665…7f86`；960×960 / 24fps / 145 帧 / 6.04 s / h264，ftyp+box+PyAV 解码通过 |
| 提交词 | `Library/Locus/tmp/dao_v11_hits/dao_v11_hitfront_v3.prompt.txt`，8,012 字符，SHA `6b4d494d…3e43e`（v1/v2 留档追溯） |
| 参数 | `seedance-2-fast` / **6 s** / 720p / 1:1 / 无音频 / 无水印 / seed −1 / 不传 background；**不传 reference_video**；reference_image 仅 1 张 = `input_identity_v11_696.png` |
| G1 输入片基 | 身份图四角 64×64 中位全 (0,159,62)，std=0，顶部空白带 G 恒 159 |
| G2 输出片基 | 逐帧角 ΔG **−18.0…−21.2**（最差 f33），与 idle(−21…−13)/walk(−15…−9)/attack(−18…−7) 同向同量级 → 抠图必须**逐帧实测键色**（实测 auto_key=(1,138,60)） |
| G2 特效闸门 | 背景偏离 >25 的像素数 max **844** @f60、mean 76.8（干净基线 idle 122 / walk 553 / attack 237，阈值 1500）；亮像素 max 413 @f65、mean 180（idle 166 / walk 625）→ **无渐变/暗角/闪白/粒子** |
| 尺度/地线 | 脚底行全程 826–842（不漂），首尾站立帧 alpha bbox 完全一致 (162,188,731,840)，主体高波动 Δ84=13.0% 全部来自上身与刀，**无缩放漂移** |
| 已入库 | `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_hitHeavy1..5.png` + 各自 `.meta`（新增，未覆盖任何现有资源）；import 设置 PPU 16 / alignment 0 / maxTextureSize 2048 / alphaIsTransparency 1，与 idle/hit/attack 一致；GUID 全新且已查重 |

**槽位与落位**（k=0.92791，取自站立参考帧 f116 对齐 idle1 主体高 605；统一整帧等比缩放后按 alpha 底边钉脚底行 898、主体居中 x=521；画布 1108×1108）：

| 槽 | 源帧 | 内容 | 最终高 / 底边 |
| --- | --- | --- | --- |
| 1 | f030 | 受力瞬间（刀举、头抬起、**露上齿 grimace**） | 622 / 898 |
| 2 | f050 | 刀被震落、脸仍紧绷 | 628 / 898 |
| 3 | f068 | 踉跄弓身、刀垂下 | 568 / 898 |
| 4 | f088 | 最深失衡 | 563 / 898 |
| 5 | f108 | 起身回弹、接近站姿 | 608 / 898 |
| 6 | — | **直接用现成 `Enemy_1011_idle1.png`**（不生成，保证回到 idle 逐像素一致） | 605 / 898 |

**两处必须记录的缺陷**：① 模型在受击时**把头抬起来了**（词里明写 “head stays level and forward”，f32–f56 头抬起、脸完全露出——表演因此可读，但属词面违背，且首尾待机帧头是低垂的）；② 源片**自带接触阴影**（硬边暗绿 G≈71 vs 键 136，距离 68 < 阈值 79，**已被 T=79 键掉**，实测残留阴影样像素仅 40，抠图脚部干净，无需额外处理）。

**G3 交付物颜色**：站姿帧（slot5）头/躯干/腿三段中位色与 idle1 差 ≤3；弯身帧偏暗 Δ(−28,−22,−18) 属姿态自阴影，非整体偏色。

**待办（已于 2026-10-25 全部完成，见 §26.11–§26.12）**：① 重导入后抽查 5 张 PPU/bounds/pivot；② 新建 `Enemy_1011_HitHeavy.anim`（6 键 / 0.9 s / 30 fps，第 6 键指向 `idle1`）；③ `Enemy_1011.controller` 加状态并决定是**新增 HitHeavy** 还是接管 `HitFlash`；④ `Enemy.cs` 的 `HitReactionDirection` → 状态名映射（`Assets/Scripts/Enemy/Enemy.cs:1116-1122`）与 `EnemySpriteController.hittedSprite`（白闪由 Unity 侧负责，**精灵里绝不能自带闪白**）。**最终决定：接管 `HitFlash` 状态**（不新增状态），clip 为 7 键 / 1.05 s（6 帧 + idle1）。

### 26.11 正面受击新动作：定稿与接入（2026-10-25 已完成）

**选帧**：用户验收后重挑 6 帧，权重放到后半段（视频前半 f0–f24 几乎静止、可用帧少，f64–f96 有 17 帧连续踉跄）。三套候选比过后取 **set C**（源帧间隔 20/12/10/12/14，姿态递进：受力龇牙 → 刀竖起 → 弓身刀平 → 更深 → 起身 → 接近站姿）。第 7 格直接用现成 `Enemy_1011_idle1.png`，回 idle 逐像素一致。

**选帧定稿（2026-10-25 用户驳回后重挑，以“与 idle 的差异”为客观判据）**：用户指出“头尾帧都是回到 idle 的纯无效动作”。改用**逐帧与 idle1 的差异**做闸门（脚本 `Library/Locus/tmp/dao_v11_hits/measure_vs_idle.py`：抠图+落位后与部署中的 `Enemy_1011_idle1.png` 求 meanDiff 与差异像素数）。
- 正面受击：idle 区 = meanDiff **5.5** / 64k px（f028 及更早）；动作区 f030–f104 = 16.2–**32.7**。
- 定稿 6 帧 = **f034 / f050 / f066 / f078 / f090 / f100**（meanDiff 17.8 / 21.9 / 32.0 / 26.6 / 22.1 / 31.0），末帧是“起身中”而非站姿，末键用现成 idle1。

**已入库（均为新增，未覆盖任何原有资源）**

| 资产 | 说明 |
| --- | --- |
| `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_hitHeavy1..6.png` | set C 六帧；导入 PPU 16 / align 0 / pivot 0.6539,0.5487 / maxTex 2048 / Point / alphaIsTransparency，纹理 1108→1024，bounds extents 34.63 —— **与 idle1 逐项一致** |
| `Assets/Animations/Enemy_1011_HitHeavy.anim` | 7 键 0/0.15/0.30/0.45/0.60/0.75/0.90 s = hitHeavy1..6 + idle1，30 fps，stop 1.05，Default |
| `Assets/Animations/Enemy_1011.controller` | **HitFlash 状态的 motion 换成 `Enemy_1011_HitHeavy`**（guid `244ba8c8658d8b547a6633eee24aa888`）；其余 9 个状态未动 |

**代码零改动**：`Enemy.cs:1116-1122` 的 `HitReactionDirection.Front → "HitFlash"` 路径原样复用，正面受击自动播新动画。回退只需把 HitFlash 的 motion 指回 `Enemy_1011_HitFlash`（guid `280fc508363260740b7b4dea498f85bb`，旧 clip 资产保留）。

**白闪**：1011 prefab 上**没有** EnemySpriteController（`GetComponent<EnemySpriteController>()?.TriggerHitFlash()` 是空调用），闪白走材质路径，精灵里无闪白。

**两条踩坑记录**

1. **手写 `.meta` 的 GUID 会被 Unity 拒收**：项目里的 GUID 是 base64 形如 `…=`（带 `=` 尾）。我手写 48 字节无填充版，Unity 报 “does not have a valid GUID and its corresponding Asset file will be ignored”，6 个资源全部加载不出来。**正确做法：不要手写 GUID，删掉 .meta 让 Unity 重新生成，再用 `TextureImporter` API 按参照物（idle1）复制导入设置。**
2. **流程纪律**：选帧阶段必须先出图给用户验收，**未验收不得写入 `Assets/`**。本轮曾把未验收的选帧直接入库，已回滚（备份 `Library/Locus/tmp/dao_v11_hits/backup_hitHeavy_setA_5frames/`）。

### 26.12 受击左/右：替换（2026-10-25 已生成、已替换入库、**已验收**）

> **本节是本轮收尾的唯一权威记录，不需要依赖任何对话历史。**

#### 26.12.1 最终状态（三个受击动作）

| 游戏动作 | Animator 状态 | clip | 精灵 | 成片 | 源片 |
| --- | --- | --- | --- | --- | --- |
| 正面受击 | `HitFlash` | `Enemy_1011_HitHeavy.anim`（7 键 / 1.05 s / 30 fps / Default，hitHeavy1..6 + idle1） | `Enemy_1011_hitHeavy1..6.png` | `dao_v11_hitfront_v1_6s_720p.mp4`（纯生成词） | 无（纯生成） |
| **左侧受击** | `HitLeft` | `Enemy_1011_HitLeft.anim`（6 键 / 0.9 s / 30 fps / ClampForever，hitLeft1..5 + idle1） | `Enemy_1011_hitLeft1..6.png` | `dao_v11_hitright_v1_4s_720p.mp4` | `sword_enemy_hit_right_v1.mp4` |
| **右侧受击** | `HitRight` | `Enemy_1011_HitRight.anim`（6 键 / 0.9 s / 30 fps / ClampForever，hitRight1..5 + idle1） | `Enemy_1011_hitRight1..6.png` | `dao_v11_hitleft_v1_4s_720p.mp4` | `sword_enemy_hit_left_v3.mp4` |

所有 18 张精灵：**1108×1108 画布 / PPU 16 / alignment 0 / pivot 0.6539,0.5487 / maxTextureSize 2048 / filterMode Point / alphaIsTransparency**，纹理 1108→1024，sprite bounds extents 34.63 —— **与 `Enemy_1011_idle1.png` 逐项一致**。`.meta` 与 GUID 全程未动，所以三个 clip 无需修改。

#### 26.12.2 命名陷阱（本次踩过，务必记住）

> **源片文件名的 legacy 命名与游戏里的动作是相反的。** 以精灵/状态命名为准，**源片文件名不可当动作名用**。
>
> | 源片 | 身体被击后倒向 | 实际对应游戏动作 |
> | --- | --- | --- |
> | `sword_enemy_hit_left_v3.mp4` | 画面**右** | **HitRight** |
> | `sword_enemy_hit_right_v1.mp4` | 画面**左** | **HitLeft** |
>
> 判定口径：**身体被击后倒向哪一侧 = 哪个动作**。量方向必须用「上身（头带）相对脚底的偏移」，**不能用全身质心**（刀在身后摆，会把质心带偏：hit_left_v3 全身质心向左 73 px，但上身实际甩向画面右 +254 px）。
> **这条口径是用户 2026-10-25 确认的，不要再自行推导。**

#### 26.12.3 两条侧击的生成与选帧数据

| 项 | 左侧受击（= HitLeft） | 右侧受击（= HitRight） |
| --- | --- | --- |
| task | `task_acd2318bc5a54a8e8641a20695c79ea4` | `task_2554c50d63b941a682e5ebce01236223` |
| 费用 | 0.57321（usage 173700，has_video_input=true） | 0.57321（同） |
| 成片 SHA | `e03930bfa0a07ca4b386b87dc97bad5ff6cff2621cb68b6f84268297e322f895` | `71c84c392760e50b296ccec7aaf4509c357b4f468af9868143401cdc6133fe3c` |
| 提交词 | `dao_v11_hitright_v1.prompt.txt` 6,175 字符 SHA `77667a86…24ba` | `dao_v11_hitleft_v1.prompt.txt` 6,184 字符 SHA `42c42b95…f18df7` |
| G2 背景 | ΔG −17.5…−7.2（最差 f061） | ΔG −18.2…−11.5（最差 f052） |
| 特效闸门 | 背景偏离像素 max 234 @f19 / 亮像素 max 351 | 背景偏离像素 max 221 @f72 / 亮像素 max 535 |
| 幅度 | 4.6%（源片 14.7%） | 5.3%（源片 5.8%） |
| 侧向位移 | Δ183 px 保留 | Δ71 px 保留 |
| 抠图验收 | holes/leak/semi 全 0；green>8 276–362 px；阴影样 73–100 px | holes/leak/semi 全 0；green>8 281–428 px；阴影样 49–131 px |
| G3 颜色 | 站姿帧与 idle1 逐带差 ≤3 | 站姿帧与 idle1 逐带差 ≤3 |
| 选帧（clip 用前 5 + 备用） | **f020 / f024 / f032 / f042 / f050**（备用 f028） | **f024 / f028 / f040 / f058 / f066**（备用 f068） |
| 落位 | k=0.93077，固定 x 贴图保侧移，底边 898，主体中心 460–486 | k=0.93077，同规则，主体中心 463–503 |

**选帧双闸门（都不可省）**：① **与 idle 的差异**（脚本 `measure_vs_idle.py` / `measure_vs_idle_right.py`）：idle 区指纹 meanDiff **5.5** / 差异像素 **64k**，落在该区的帧一律不取；② **受力位移覆盖**：每一槽要落在受力位移曲线的不同相位，**必须包含位移峰值那一格**（左受击的峰值是 f024，上身 −91；右受击峰值是 f028–f032 段，+254）。初版右受击从 f020 之后取帧、全是回摆段，漏掉最猛的一下，已修正。

**已知偏差（用户已接受）**：① 词里写“嘴闭着、牙关咬紧只露一线上齿”，模型画成**大张嘴龇牙**，读起来偏“痛呼”而非硬挨一下；② 模型把受击表达成“刀臂甩出 + 头猛地歪”，力度比源片软（幅度 4.6–5.3%）；③ 源片在保持段是同一个姿势，**5 槽姿态高度重复**——左/右的真实内容只有“一记重击 → 长时间保持 → 弹回 idle”。

#### 26.12.4 本轮三次事故（都已修，写下来防止复发）

1. **复制脚本改路径没匹配上 → 两条动作出自同一条视频。** 用 `sed` 复制“左受击”的落位脚本做“右受击”时，脚本里是 `WORK / "frames_left"`，而我的 sed 模式写成 `dao_v11_hits/frames_left` → **没匹配上**，右受击仍然读左边的帧目录；两套精灵其实都出自同一条视频（两边都出现同一帧 f024、402,672 B 完全相同），`dao_v11_hitright_v1` 从未被用来出帧。**用户从预览图发现左右雷同。**
   → 防线脚本 `Library/Locus/tmp/dao_v11_hits/verify_hit_packs.py`（检查：预览包帧 == 工程内精灵；左右两套精灵集合**无交集**；左右两套源帧列表不相同）。**每次做完两条以上同类动作都要跑，FAIL 即不许入库。**
2. **手写 `.meta` 的 GUID 会被 Unity 拒收。** 项目 GUID 是 base64 形如 `…=`（带 `=` 尾）；手写 48 字节无填充版时 Unity 报 “does not have a valid GUID and its corresponding Asset file will be ignored”，资源全部加载不出来。**正确做法：不要手写 GUID，删掉 .meta 让 Unity 重新生成，再用 `TextureImporter` API 按参照物（idle1）复制导入设置。**
3. **流程纪律：选帧阶段必须先出预览给用户验收，未验收不得写入 `Assets/`。** 本轮曾把未验收的选帧直接入库（后已回滚）。

#### 26.12.5 预览包规范与位置

每个动作一个包，**包里同时含挑选帧与可播放 GIF**（用户明确要求）：

```
Library/Locus/tmp/dao_v11_hits/preview_hitHeavy/   anim.gif / contact.png / 00..06.png / manifest.json
Library/Locus/tmp/dao_v11_hits/preview_hitLeft/    anim.gif / contact.png / 00..05.png / manifest.json
Library/Locus/tmp/dao_v11_hits/preview_hitRight/   anim.gif / contact.png / 00..05.png / manifest.json
```
- `00.png…` = **按播放顺序**命名的落位成品精灵（最后一张是现成 idle1），可直接当序列播；`anim.gif` 按**游戏内键间隔**（150 ms/帧）+ 尾帧保持（300 ms）合成；`contact.png` 带槽位标注；`manifest.json` 记录槽位→精灵→源帧，并标出**不在 clip 里的备用帧**。
- **播放序列必须与 clip 的实际键序列一致**：clip 末键是 idle 精灵，就不要从成片里再抽一张站姿占槽（用户原话：“头尾帧都是回到 idle 的纯无效动作”）。
- 生成器 `make_preview_pack.py`；GIF 时长单位是**毫秒**（`KEY_STEP*1000`），写成 `*100` 会被 Pillow 按 1/100 s 取整（回读时长全错）。
- 抠图/选帧脚本：`key_place_hitheavy_setC.py`（正面）、`key_place_hitleft_v1.py`（右）、`key_place_hitright_v1.py`（左）、`verify_keying.py`、`measure_vs_idle.py` / `measure_vs_idle_right.py`、`verify_hit_packs.py`。

#### 26.12.6 备份位置

```
Library/Locus/tmp/backup_Enemy1_1_hitLR_swap2_20261025/  ← 最终入库前的两套
Library/Locus/tmp/backup_Enemy1_1_hitLR_swap_20261025/   ← 第一次对调前
Library/Locus/tmp/backup_Enemy1_1_hitLeft_20261025/      ← 左侧原始旧美术
Library/Locus/tmp/backup_Enemy1_1_hitRight_20261025/     ← 右侧原始旧美术
Library/Locus/tmp/dao_v11_hits/backup_hitHeavy_setA_5frames/   ← 正面受击被驳回的那版
Library/Locus/tmp/dao_v11_hits/backup_hitLeft_v1_20261025/    ← 侧击被驳回的那版
```

#### 26.12.7 遗留（下一位接手须知）

1. **1021 可能同源反向**：`enemy1021_hit_right_v1.mp4` 里矛从画面右侧刺入、身体倒向画面左 → 按 26.12.2 的口径它其实对应游戏的 **HitLeft**，而 1021 的 `hitRight` 精灵正是从它抽的帧。**1021 一个字没动**（旧帧都在备份里），要不要一起核/一起对调待定。
2. **左/右受击幅度只有 4.6–5.3%**，与正面受击的 13% 差一档，同屏会明显偏轻；且源片保持段姿态重复。**要拉齐得给左/右也换纯生成新动作（各 ≈0.7308，同正面受击的配方）。**
3. `hitLeft6` / `hitRight6` 仍不被 clip 引用（§26.1 老问题），已一并替换但不进播放。
4. 原受击组 `Enemy_1011_hit1..5.png`（**正面受击的旧美术**）**未被动过**，但 `HitFlash` 状态已改指 `Enemy_1011_HitHeavy`，所以这 6 张旧图现在已不被任何 clip 引用（可删可留）。
5. **白闪由 Unity 侧负责**（`EnemySpriteController.hittedSprite`；1011 prefab 上没有该组件，闪白走材质路径），**精灵里绝不能自带闪白**。
6. 后续若再做受击/打击类动作，沿用本轮结论：**动作要有事件阶梯（不能写成状态保持）→ 词里绑死动作节拍 → 一次提交 → 选帧过 idle 差异闸门 + 受力位移峰值闸门 → 出预览包给用户验收 → 再入库**。

---

### 26.9 三种受击：原计划（**已于 2026-10-25 全部完成，实际结果见 §26.11–§26.12；本节保留作历史对照**）

**源片（均在 `Enemy1011ArtSource/01_SourceVideos/`，哈希见 `00_Index/videos.csv`）**

| 组 | 源片 | 字节 | SHA-256 | 规格 | 底色实测 |
| --- | --- | --- | --- | --- | --- |
| hit | `sword_enemy_hit_front_v2.mp4` | 1,989,310 | `ef374866a9b8106208c4f893f49bc55930173481e0fb954dce868df02de77698` | 960×960 / 24fps / 97 帧 / 4.042 s | f0 (0,173,60)、f48 (0,166,60)、f96 (0,170,60) |
| hitLeft | `sword_enemy_hit_left_v3.mp4` | 1,801,808 | `536278768a1a3dfa9940b0752a1b27a91d0579cbdd101637ff0f9845645cd3b1` | 同上 | f0 (0,173,62)、f48 (0,166,60)、f96 (0,168,58) |
| hitRight | `sword_enemy_hit_right_v1.mp4` | 2,160,235 | `7daca7fe61d0838169b87962878f0235753857999c775eac74cbe71c59ee1bd0` | 同上 | f0 (0,173,60)、f48 (1,165,62)、f96 (0,170,60) |

**槽位 → 源帧（来自 `00_Index/frame-provenance.csv`，是当前旧美术的真实出处，新一轮应保持同相位）**

| 组 | 1 | 2 | 3 | 4 | 5 | 6 | clip |
| --- | --- | --- | --- | --- | --- | --- | --- |
| hit | f016 | f048 | f076 | f080 | f084 | — | `Enemy_1011_HitFlash` 0.9 s / 30 / Default，用 5 帧 + `idle1` 收尾 |
| hitLeft | f024 | f033 | f069 | f070 | f071 | f075 | `Enemy_1011_HitLeft` 0.9 s / 30 / ClampForever，用 1–5 + `idle1` |
| hitRight | f019 | f030 | f051 | f054 | f057 | f075 | `Enemy_1011_HitRight` 0.9 s / 30 / ClampForever，用 1–5 + `idle1` |

`hitLeft6` 与 `hitRight6` 存在但**未被任何 clip 引用**（§26.1），是否一并换由用户定。

**源片动作节拍（帧间差 MAD，实测）**

- hit：中位 0.36，尖峰在 f15(12.3)、f18(5.7)、f38(7.9)、f41(9.7)、f42(8.1)、f45(5.5)、f73(7.4)、f75(7.3)、f77(6.3)、f81(3.6)。一次冲击 + 一次回弹，中段大量静止。
- hitLeft：中位 0.38，两次受力窗口：f23–f34（13.6, 11.9, 13.8, 9.3, 4.8, 2.6, 3.7, 3.4, 3.0, 2.2, 2.5, 1.1）与 f67–f78（8.0, 10.9, 13.8, 13.6, 8.6, 3.7, 4.9, 3.3, 1.7, 0.7, 1.2, 0.5）。
- hitRight：中位 0.60，f17(15.7)、f20–f30 连续、f46–f58 连续、f66–f77 零散。动作密度最高。

**当前旧美术的已知瑕疵（新一轮有机会修掉）**

- `hitLeft1` 左缘 12 px 淡痕；`hitLeft3`（源 f069）左缘 6×6 px 剑尖残片。
- `hitRight1` 实际 alpha>20 右缘 795，部署记录写 853（差 58 px），待目检；源片 f018 异常宽已被排除不用。

**提交配方（沿用 §26.4，另加以下三条）**

1. 一次授权一个任务，**只提交一次**。输入 = 原始源片 + 原图 `input_identity_v11_696.png`，**不得用任何已生成的成片当 `reference_video`**。
2. 词里底色写 `RGB(0,159,62)`（源片本身底色 G≈165–173，输出会低 9–21，实测落在 G≈138–146，接近批次键即可）。
3. **表情必须写进第一次提交**，且写成跟随源片受力节拍的时间性表演。参考写法：受力瞬间眼缝挤紧、眉心下压、下颌咬死、嘴全程闭着（不露齿、不张嘴、不喊）；受力过后眉松开一线、眼睛重新睁开、脸回到冷脸；头颈回弹随源片走，不为了露脸而抬头。**不要写“下颌松开”这类会被画成张嘴的措辞**（walk v2 的教训）。受击是硬冲击，宁可让表情更硬也不要开口。

**后处理（与 idle / walk 一致，已验证可用）**

1. 在**成片自身**按上面「槽位 → 源帧」表的相位找对应帧（不要直接沿用源片帧号，成片有重绘与漂移）。
2. SVL Chroma：T=79 / softness 16 / halo 1 / 半透明转不透明；按每帧实测键色（本次源片底色≈`0,166,60`，成片会偏暗）。
3. 缩放：命中组是受击姿态，**主体高不等于站立 idle**。以旧组六帧（或五帧）的平均高与逐槽脚底线为基准，单一全局缩放 + 逐槽钉脚底锚点，不逐帧按高度归一化（会把“受击变矮”抹掉，见工作流 §12.3）。
4. 落位与命名：覆写同名 `Enemy_1011_<group><i>.png`，`.meta` 不动；备份到 `Library/Locus/tmp/backup_Enemy1_1_<group>_<日期>/`；Unity 重导入后确认 clip 键数不变。

**成本**：每条 ≈ `0.573`，三条 ≈ `1.72`。


### 26.8 下一轮怎么开（当前状态）

**本轮已交付（2026-10-25，均已验收）**：正面受击（`HitFlash` 状态改指新建的 `Enemy_1011_HitHeavy`，7 键 1.05 s）+ 左侧受击（替换）+ 右侧受击（替换）。1011 的 idle / walk / attack 早就是 v11。

**1011 仍是旧美术的组**：`dead` / `rise` / `landing` / `getup`（源片 `sword_enemy_dead_front_v4.mp4`、`enemy1011_launch_rise_v1.mp4`、`enemy1011_launch_landing_v1.mp4`、`enemy1011_launch_getup_v1.mp4`）——**注意：这些源片同样可能与游戏动作命名相反，动手前先按 §26.12.2 的口径核对**。

**下一轮建议优先级**（按用户已知悉并接受程度排序）：
1. **左右受击改纯生成新动作**（各 ≈0.7308）：现版幅度只有 4.6–5.3%、姿态重复，与正面 13% 差一档；正面那条的配方（纯生成词 + 事件阶梯 + 峰值取帧）已验证有效。
2. **1021 的 hitRight 命名核对**：见 §26.12.7 遗留 1。
3. **dead / rise / landing / getup 四组**替换。

**开工前必须做的三件事**
1. 读本文件 §26.11–§26.12（受击的全部结论与事故记录），以及 `skill/workflows/character-hit-animation-video-workflow.md` §14（§14.1 死脸判据 / §14.4 特效禁令 / §14.9 预览包规范 / §14.10 idle 差异闸门 / §14.12 命名纠正 / §14.13 来源校验）。
2. **先核命名口径**（源片名 vs 游戏动作），不要自行推导。
3. **先出预览包给用户验收，再写 `Assets/`**；出完帧必须跑 `Library/Locus/tmp/dao_v11_hits/verify_hit_packs.py`（两条同类动作时），FAIL 不许入库。

**可复用的生成配方（已验证）**
- 身份图：`C:\Users\steam\Pictures\gptGen\enemy1011_attack_anim_v1\input_identity_v11_696.png`（底 0,159,62，SHA `544b3584…f8da6`），**用原图不要用染绿副本**。
- 参数：`seedance-2-fast` / 4 s / 720p / 1:1 / 无音频 / 无水印 / seed −1 / 不传 `background`。
- 带视频输入 4 s/720p 的一次性生成 ≈ **0.57321**；纯生成 6 s ≈ **0.7308**。
- 词的结构：动作锁定 → 角色（+**敞脸盆盔**、不许照抄源片封闭盔）→ 画法（soft even highlights）→ 尺度/地线/相机独立锁 → **面部事件阶梯**（绑动作节拍）→ 26 项特效禁令 → 禁止项 → 绿幕 RGB(0,159,62)。
- 后处理：槽位（在成片自身取）→ SVL Chroma T=79 / softness 16 / halo 1 / 半透明转不透明 → 单一全局 k + 逐槽钉脚底 898 → 侧击用**固定 x 贴图**（保侧移）→ 出预览包 → 验收后入库。
- 踩过的坑：源片自带接触阴影（硬边暗绿，T=79 能键掉）；模型会把“闭嘴咬牙”画成大张嘴龇牙；模型会把受击力度收软；**量受力方向要用上身相对脚底、不要用全身质心**；**复制脚本改路径后必须做逐字节校验**（左右两套同源事故）。

**（历史版本：上一轮 idle 起手时的发起模板，已完成，保留供追溯）**

```text
任务：给 1011 敌人用 v11 形象重做 attack 以外的 9 组动作。
先读这三个文件，读完先复述当前状态与边界：
1. Locus/knowledge/plan/enemy1011-style-locked-dao-generation-handoff.md（活文档，§0 / §25 / §26）
2. Locus/knowledge/design/art-style-guide.md（§2.3 v11 样本）
3. Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md（视频接口与轮询/下载纪律）
本轮只做第一组 idle（6 帧），不要一次开全局。
先回答三件事再动手：① idle 走 §26.3 的方案 A（旧 sprite 拼定格源片）还是 B（纯图生成）；
② 归一化参考图用现成的 input_identity_v11_696.png 还是该动作另做；③ 预计费用与轮询预算。
待我明确回复“发起生成”后，只提交一个任务；成片后按 §26.4 四步出帧、覆写 Enemy_1011_idle1..6.png 并备份。
```
