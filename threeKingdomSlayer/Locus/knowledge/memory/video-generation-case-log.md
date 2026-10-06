---
id: kd_b36a0f8e-c80c-4114-824c-e74466d1d189
injectMode: inherit
summary: AI 视频/图像素材一次性任务台账：Enemy2 受击、Seedance 路线、持剑敌军 1011（v4 Idle、受击、Attack/Dead/Walk 动作动画）与 GPT Image 单帧去背的文件、参数、费用和验收结论，以及 FakeRoute 部署状态。
aiEditMode: inherit
---

# AI 视频/图像素材生产：已验收案例与实测数据

> 本文件只记录**一次性任务状态与实测数据**（task id、费用、文件、验收结论），流程与可复用规则见 `skill/workflows/` 下对应文档。新增任务请追加到"任务台账"，不要写回流程文档。

## 一、任务台账

接入状态：`REACH_API_KEY` 已配置在用户环境变量；上传 / 创建 / 轮询链路已实测可用（调用时仅检查存在，绝不输出密钥）。

### Enemy2 受击动画视频

| 版本 | task id | 文件 | 大小 | 结论 |
|---|---|---|---|---|
| v1 | `task_61cb7006d143479b8dbe5a059ecefa9b` | `C:/Users/steam/Videos/doubaoVideo/enemy2_hitted_return_v1.mp4` | — | 用户否决：未用受击图作动作参考；提示词缺少"全程完整显示、禁止超框" |
| v2 | `task_0c31001bd51f4175908abb00d676966d` | `C:/Users/steam/Videos/doubaoVideo/enemy2_hitted_return_v2.mp4` | 565,229 bytes | 状态 `success`，容器 `ftyp/uuid/free/mdat/moov` 校验通过；用户认可 |

- 规格：`seedance-2-fast`、4 秒、720p、1:1、无音频、无水印、seed=-1。
- API：completion_tokens=87300，total_tokens=87300，cost.spend=0.48888（币种未推断）。
- 截至该次：未做逐帧定位测量、透明 PNG 制作与 Unity 部署。

### Enemy2 失盾 → 后仰胆怯（本轮用户已验收）

**当前交付状态：失盾转态 v3 视频用户验收通过；其实际尾帧已提取，独立胆怯循环 v1 已生成下载但明确待验收；未制作完整透明动画帧或部署 Unity。** 用户验收不等于已完成像素级锚点/比例测量。

| 版本 | task id | 文件 | 大小 | 结论 |
|---|---|---|---|---|
| v1 | `task_402944ad7157435abc919a6f19fe5d6f` | `C:/Users/steam/Videos/doubaoVideo/enemy2_shield_disarm_v1.mp4` | 1,147,694 bytes | 用户否决动作；仅上传原始站姿，没有已认可中间/尾帧 |
| v2 | `task_a809c859f2d34f69985ac0ecfc9e9ad0` | `C:/Users/steam/Videos/doubaoVideo/enemy2_shield_disarm_v2.mp4` | 2,031,343 bytes | 三张对齐参考；用户认为还行，但关键姿态转换太快、缺少过渡 |
| v3 | `task_9ee89aec2caf4df99c10a0d72e459e97` | `C:/Users/steam/Videos/doubaoVideo/enemy2_shield_disarm_v3.mp4` | 2,189,833 bytes | 沿用相同三图，仅优化运动过渡；用户确认“不错，当前可验收” |

- v1：`seedance-2-fast`、4秒、720p、1:1、无音频/水印；usage=87300，cost.spend=0.48888。
- v2/v3：同模型、6秒、720p、1:1、无音频/水印、seed=-1；每次 usage=130500，cost.spend=0.7308。费用币种未推断，未包含图片生成费用。
- v3 已验证 `ftyp/uuid/free/mdat/moov` 容器及文件长度；SHA256：`47dd7f37b111e7352d075dd9e585a1d7706c3194dba0e34928bba95ca4803388`。后续为循环准备已用PyAV完整解码：960×960、24fps、145帧，末帧索引144、PTS=6.000秒；未完成逐帧位置/比例测量。
- 下载：v3 首轮超时保留 1,196,032 bytes；续传前核对同任务远端前64KiB与本地一致，HTTP 206 总长度2,189,833；完成后才把 `.part` 改为 `.mp4`。未因下载失败重新生成。

#### 已验收关键图与实际上传来源

1. 起始身份/持盾 Idle：`Assets/Sprites/Enemy/Enemy2/Enemy_2.png`。
2. 中间挑盾姿态：`C:/Users/steam/Pictures/gptGen/enemy2_shield_disarmed_keyframe_v11.png`，用户验收；对应 `_v11_raw.png` 和 `_v11.response.json` 保留。
3. 尾帧：`C:/Users/steam/Pictures/gptGen/enemy2_unshielded_fearful_idle_v3.png`，用户验收；对应 `_v3_raw.png` 和 `_v3.response.json` 保留。

实际上传是三张准备副本，而非直接上传原文件：
- `Library/Locus/tmp/enemy2_video_v2_refs/01_idle.png`
- `Library/Locus/tmp/enemy2_video_v2_refs/02_disarm.png`
- `Library/Locus/tmp/enemy2_video_v2_refs/03_fear.png`

均为1024方形白底，用普通 `reference_image`，不是 first_frame/last_frame 混合模式。原 Idle 先按2倍分辨率归一，再共同乘0.68；中间和尾帧乘0.68，最近邻采样。脚底基线准备目标 y=800、脚底中点 x=512，并移除首图手部上下露出的短武器。原中间脚底 y=901、尾图 y=864；此准备是近似基线对齐，不证明两只脚及解剖尺度像素级完全一致。没有按盾+人物整体包围框分别缩放。

#### 本轮动作结论与否决项

- 中间帧：持盾手必须被向上牵引，不往胸口回缩；不能为避免抛掷感而强加整个人向反方向侧倾。另一侧肩/肘/张开手与膝盖须有可见受力，而非只改持盾手。
- 失盾尾帧：胆怯、躯干后仰、双掌前挡，不是握拳迎战，也不是向前蜷缩。
- 盾：先垂直脱手，再小幅上升并整体渐隐；不飞出画布、不碎裂、不增加特效；人物不淡出。
- v3节奏：0–0.6起始；0.6–1.4受力脱手；1.4–1.7中间姿态随动；1.7–2.7盾淡出；2.7–4.8连续收势转胆怯；4.8–6轻微待机。时间是提示词目标，不是已测得的精确帧点。
- 图片早期失败：无参考纯文生图导致身份漂移；手收胸前/躯干侧倾误导方向；紧贴盾边半握变成仍握盾；只改手臂导致身体僵硬。仅用户验收的 v11 和胆怯 v3 可作为对应动作参考，不复用失败图。

完整 v3 Prompt、规格、参考图哈希和任务状态暂存 `Library/Locus/tmp/enemy2_disarm_video_v3.json`，创建脚本为 `Library/Locus/tmp/create_enemy2_disarm_video_v3.py`；这些是临时目录记录，可复用步骤和提示模板见 `skill/workflows/character-hit-animation-video-workflow.md`。后续使用前应检查临时文件仍在，不视为永久资产。

### Enemy2 胆怯循环候选与下载优化（未验收）

**用户认可本次速度与生成水平，但明确说明视频不代表已验收；循环v1保持待验收，不得与已验收的失盾转态v3混淆。**

- 输入：`C:/Users/steam/Pictures/gptGen/enemy2_shield_disarm_v3_actual_last_frame.png`，从已验收转态v3提取真实最后一帧，960×960，保留白底和完整画布；同图作为 `first_frame` 和 `last_frame`，不混用reference_image。
- 输出：`C:/Users/steam/Videos/doubaoVideo/enemy2_fear_loop_v1.mp4`。
- task：`task_7699499ba66a485eb4e1fea967fde0da`，服务端success；Seedance-2-fast，请求4秒/720p/1:1，无音频/水印，seed=-1。
- 文件1,365,744 bytes；SHA256 `05a13195ed0f379974e012480eaa0a1dcc480f6d446c8efa4ae1d799236eecd4`；MP4容器与全部97帧解码通过。实际960×960、24fps、容器时长4.041667秒、末帧PTS=4.000秒。
- 抽帧保持后仰胆怯姿态，但首尾非像素一致；整画布首尾MAE约1.536，仅为诊断且受白底稀释，不能证明无缝。尚未验收原片→循环和循环→循环接缝、脚底漂移、人物缩放，未去背/部署。
- API usage=87300，cost.spend=0.48888（币种未推断）。

#### 本次阶段计时（2026-09-16，UTC记录）

| 阶段 | 秒 | 边界 |
|---|---:|---|
| 上传尾帧 | 4.481 | 单次上传 |
| 创建请求 | 0.314 | 得到任务ID |
| accepted到success观察 | 175.087 | 包括排队、查询间隔及本地准备；不是纯服务端推理时长 |
| Range探测及4路分段下载 | 47.222 | 下载墙钟时间，不是四段耗时之和 |
| 上传开始到完整下载 | 227.109 | 约3分47秒，不含随后解码与人工验收 |

四段各341,436 bytes，耗时37.511/23.572/11.877/45.603秒；均核对206、Content-Range、总长度、分段长度和ETag后拼接。完整记录和Prompt暂存 `Library/Locus/tmp/enemy2_fear_loop_v1.json`；接触表 `Library/Locus/tmp/enemy2_fear_loop_v1_contact_sheet.png`。临时记录不视为永久资产。

#### 下载方法比较证据

- 历史v3单连接180秒仅取得1,196,032 bytes后超时，后来续传完成。
- 对同一已验收v3整文件重新测速：4路Range约41.057秒下载2,189,833 bytes，约52.09KiB/s，所有段和合并SHA256与已知原片一致；见 `Library/Locus/tmp/video_download_benchmark/full_results.json`。
- 当前无启用的系统/环境代理，默认与强制直连是同一路径；下载host为新加坡区域对象存储。不能归因于“绕过代理”或确定为服务端限速。
- 本轮循环视频实际4路47.222秒下载完成，比此前频繁180秒超时再续传的操作更顺畅；文件大小和网络时段不同，不能宣传固定倍速。只证明此条件下方法有效，不代表生成模型本身加速。
- 可复用步骤已写入 `skill/reachapi-seedance-video-generation-v2.md` 和角色视频workflow。当前脚本是本次任务实现，并非已完成可复用下载器的所有失败恢复能力。

### 持剑敌军 v4 Idle 与 GPT Image 单帧去背

**当前状态：用户已验收 v4 Idle 视频内容，并认可 GPT Image 对 f000 与 f008 两帧的单帧去背视觉效果。先前手动六帧去背被否决：浅底显示灰色脏边，深底只是遮住问题。六帧集合 0/8/16/24/32/40 现已全部为原生 alpha（含 f040 第 5 次重跑），并均已生成「对齐到原帧几何」的副本，未对齐版本同时保留。对齐后的帧间一致性、逐帧像素保真与 Unity 部署均未验收。**

#### 已验收视频与原始帧

- task：`task_688460292bfc499f9f1f217b9a4c329c`，服务端 `success`。
- 视频：`C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4`；1,677,207 bytes；SHA256 `151ea2bf81ede4675733a6852f24580a58a23d740561a7919f9dd367eba052ab`。
- 规格：`seedance-2-fast`、4秒、720p、1:1、无音频/水印、seed=-1；同一张 v4 的72%安全画布深灰底准备图作为首尾帧。
- 实际：960×960、24fps、97帧、容器时长4.041667秒；MP4盒结构和后续 FFmpeg 完整解码通过。API usage：completion_tokens=87300、total_tokens=87300；cost.spend=0.48888（按原值记录）。
- 用户观察约2秒完成一轮，抽查 f000 与 f048 支持“接近回到起始姿态”，但未据此声称逐像素无缝。
- 原始帧：`Library/Locus/tmp/sword_enemy_v4_idle_cycle_v1/clean_raw/f000.png` 至 `Library/Locus/tmp/sword_enemy_v4_idle_cycle_v1/clean_raw/f048.png`，共49张960×960 RGB PNG；无标注、未去背、未裁切缩放，保留视频灰底。`clean_raw` 中的 clean 指无标注，不代表透明。
- 手动候选六帧来自0/8/16/24/32/40帧，位于 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_v4_idle_cycle_v1_keyframes/`；本轮用户否决其灰边，不能作为已验收正式资源。临时原始帧若丢失可从上述 MP4 重新解码，不视为永久资产。

#### 用户认可的 GPT Image f000 去背测试

- 输入：`Library/Locus/tmp/sword_enemy_v4_idle_cycle_v1/clean_raw/f000.png`；源帧SHA256 `60e521d26b60e2fb0439959d00817824e737f7497415d4c123e4d6eaf1631505`。
- API：`POST https://api.muskapis.com/v1/images/edits`，multipart `image[]` 单图编辑，HTTP `200`；模型 `gpt-image-2.5-sunburst`，quality=`high`、input_fidelity=`high`、size=`960x960`、background=`transparent`、output_format=`png`。
- 输出：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_v4_idle_gpt_cutout_test/idle_f000_gpt_cutout_v1.png`；1,073,929 bytes；SHA256 `ecdfec45c86f52204d56b8aa8747282a4ef7fb6ab5a3364cf57a049377d1b2a5`。
- 完整Prompt/参数：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_v4_idle_gpt_cutout_test/idle_f000_gpt_cutout_v1.request.json`。
- 原始响应：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_v4_idle_gpt_cutout_test/idle_f000_gpt_cutout_v1.response.json`。
- 实际PNG：960×960、RGBA、Color Type 6、四角透明、Alpha范围0–254；675,076个完全透明像素、246,524个非零且小于255的像素。其中216,651个处于250–254，约占非零像素88%；大部分主体接近完全不透明，不能把“没有255”直接判为失败。
- 浅色/白底与深底合成预览中灰边明显改善，用户明确认可抠图效果。此前助手把动画几何问题泛化成“抠图不适合/失败”的结论应纠正：去背视觉成功与动画一致性是独立验收。
- 仍需记录的几何差异：输出Alpha≥128前景框 `[107,149,773,871]`；原帧主体估计 `[161,187,696,778]`，可见宽/高约放大24.5%/22.2%，框底约下移93px，并有局部重绘。这不否定单帧去背认可，但在动画部署前仍需对齐与帧间检查；不能自动把这张图替代原视频首帧。
- API usage：input_tokens=833、output_tokens=4153、total_tokens=4986；未返回cost，不推断本次实际费用。仅执行一次f000图片编辑测试，未重新创建视频任务，未处理剩余五帧。

#### f008 三次请求与路由差异（同日实测，前两次失败）

**结论：`background=transparent` 的原生透明能力随服务路由变化。同一份提交在三条路由上给出三种结果，只有两条返回原生 alpha；第三次结果用户已目检认可。**

| 次 | 图片 host | 响应回显字段 | 结果 |
|---|---|---|---|
| f000（已验收） | `cdn.jd23kjs.work` | 无回显，仅 `{created,data,usage}` | 原生 RGBA |
| f008 #1 | `r2.52image.xyz` | model/quality/size/output_format（无 background） | RGB，棋盘格被画入像素 |
| f008 #2 | `r2.52image.xyz` | 同上 | 同上；prompt 追加禁止棋盘格条款无效 |
| f008 #3 | `img.zxai.us` | 含 `background=transparent` | 原生 RGBA，用户认可 |

- 三次提交结构完全一致：`POST /v1/images/edits`、multipart `image[]`、`background=transparent`、`output_format=png`、`gpt-image-2.5-sunburst`、`quality=high`、`input_fidelity=high`、`size=960x960`、`n=1`；prompt 逐字复用 f000 那次的 2061 字符原文（#2 额外追加“必须真实 alpha、禁止绘制棋盘格”条款）。三次差异不能归因于提交方式。
- 输入始终为 `clean_raw/f008.png`（960×960 RGB、无 alpha、灰底、未抠图未标注未缩放），SHA256 `f7b2d8de19fb547513a672fa8d65a30892d9edb8948b4fce6b413dd78fbf9310`。
- 失败两次的实测：`color_type=2`(RGB)，背景是画入的棋盘格（背景行明暗游程 16/17px 交替，两级灰 245/254）。成功路由的回显含 `background`，失败路由不含；但 f000 成功那次完全无回显字段，故该差异只能作为线索，不能作为判据。
- 输出 `idle_f008_gpt_cutout_v3.png`：1,145,369 bytes，SHA256 `758d4fd20ea537c0b02fd300ba3b466c5b504541e0f5a7301440d63285a07fcd`；960×960 RGBA、`color_type=6`、四角 alpha=0、范围 0–254；全透 636,258 / 半透 285,342 / alpha=255 为 0；剪影内 alpha 均值 252.9（f000 基线 253.0）。请求、响应与浅/深底预览在同目录 `idle_f008_gpt_cutout_v3.request.json`、`.response.json`、`_preview_white.png`、`_preview_dark.png`。
- 几何：本次前景框 `[84,83,777,876]`（692×792，底边 875），原帧主体约 `[165,178,690,778]`（525×600，底边 777）→ 宽 +31.8%、高 +32.0%、底边下移 98px；f000 那次为 665×721、底边 870（+26.7%/+20.2%）。不同帧漂移量不同，逐帧直接跑 GPT 会产生帧间抖动，未经对齐不得作为动画序列。
- 边缘环亮度比（1px/2px/3px）：f000 基线 0.58/0.79/0.86，f008 #3 为 0.66/0.83/0.86，同一量级。边缘与光晕验收应与该基线比对，不使用 1.0 之类的绝对值。
- 用量：#1 total 8,177、#2 total 8,245、#3 total 2,553（input 870 / output 1683）；三次响应均未返回 cost 字段，不推断金额。失败路由的输出 token 明显更高。
- 命名对应：`idle_f008_gpt_cutout_v1/v2.png` 是两次失败路由的 API 原图（RGB，画入棋盘格）；`v3.png` 是原生 RGBA 交付；`*_v1_whitebg_keyed*.png`、`*_v2_checkerkeyed_v3*.png` 是失败期间做的本地抠图候选（其中 `v2_checkerkeyed_v3_defringed.png` 清除了被角色包住的棋盘格 2,073 px），因 v3 已获认可，不作为交付。

#### 六帧批量去背与大小对齐（同日，f016/f024/f032 成功，f040 兜底）

- 请求：四次 `POST /v1/images/edits`，参数与 prompt 与 f000/f008 完全一致（逐字 prompt），输入分别为 `clean_raw/f016.png`、`f024.png`、`f032.png`、`f040.png`。
- 结果：f016 / f024 / f032 命中 `img.zxai.us`（回显含 `background`），均返回 `color_type=6` RGBA，全透像素 675,268 / 654,911 / 644,206，剪影内 alpha 均值 253.0 / 252.9 / 252.9。f040 命中 `r2.52image.xyz`（回显不含 `background`），返回 `color_type=2` RGB + 画入棋盘格 → 本地棋盘格抠图兜底 `idle_f040_gpt_cutout_v1_localkeyed.png`。
- 路由指纹累计：回显含 `background` → 原生 RGBA 3/3；回显不含 `background` → RGB + 棋盘格 3/3；完全无回显 → 原生 RGBA 1/1。失败路由用量稳定偏大（input 1445 / output 6732），成功路由为 input 870 / output 1683。仍属强线索，不作定论。
- 用量：f016 / f024 / f032 各 total 2,553（in 870 / out 1683）；f040 total 8,177（in 1445 / out 6732）；四次均未返回 cost 字段，不推断金额。
- 对齐方法：以原帧主体估计框（灰底距离阈值 ≥18）为基准，**统一缩放**（比例 = 原帧高度 / 输出高度）+ 脚底锚点（底边行与脚心 x）对齐回 960×960 原画布；不做逐帧裁剪后再拉伸，避免尺寸忽大忽小。
- 对齐结果（scale / 输出框 / 原帧框 / 宽高残差）：f000 0.8172、[154,188,698,778]、宽 +11px；f008 0.7566、[167,178,692,778]、+0px；f016 0.8264、[156,164,693,778]、+6px；f024 0.7954、[155,160,704,778]、+3px；f032 0.7847、[141,162,711,778]、+14px；f040 交付版 0.7560、[157,179,708,777]、宽 +2px（f040 早先 v1 兜底版为 0.9403、[162,180,709,778]、−2px，仅备查）。六帧对齐后高度残差均为 0，底边 776–777（1px 差来自取整），脚心 x 偏差 ≤2px。
- 每帧模型自带缩放比在 0.76–0.94 之间浮动，证明逐帧直接使用必须对齐；这也是保留对齐前版本的原因。
- f040 本地兜底版有明显偏亮边缘：1px 环亮度比 v1 1.40 / v2 1.32 / v2 加强去边(3px) 1.41（对齐后 1.61），远差于原生 alpha 帧（f016 0.47、f008 0.71）。原因是模型把棋盘格画进像素后，轮廓存在 2–3px 浅色混合带，且剑、盔等薄结构没有可借色的内部邻居，颜色去污染无法收敛。该兜底版不建议作为动画帧。
- f040 重跑与最终结果：第 2 次（`idle_f040_gpt_cutout_v2`）仍命中 `r2.52image.xyz`（回显不含 `background`，usage 8177），返回 RGB；第 3、4 次因工具层将调用截断、未落盘任何文件，是否计费无法确认；**第 5 次**（全程日志 `Library/Locus/tmp/sword_enemy_idle_extract/f040_v5.log`）命中 `img.zxai.us`（回显含 `background`，usage 870/1683），拿到原生 alpha。
- 更正：曾据被打乱的回显片段推断“同路由同输入会返回同一张图”，该结论作废——f040 第 1、2 次的字段与 prompt 完全相同、host 与 usage 相同，但图片字节为 910,257 与 917,092，说明坏后端同样是随机生成，重跑只是概率问题。
- f040 交付版：`idle_f040_gpt_cutout_v5.png`（1,148,095 bytes，RGBA、四角 alpha=0，全透 633,281 / 半透 288,319 / alpha=255 为 0，剪影内 alpha 均值 253.0）；对齐版 `idle_f040_gpt_cutout_v5_aligned.png`（scale 0.7560，框 [157,179,708,777]，原帧框 [156,180,705,778]，残差 w+2 h+0，底边 776 比原帧低 1px 取整，脚心 624.5 对比原帧 622.5，1px 环亮度比 0.49，与原生基线 0.47 同量级）。
- 当前状态：六帧 f000 / f008 / f016 / f024 / f032 / f040 全部为原生 alpha 且已生成对齐版；此前的 v1 / v2 本地兜底版与 deep 版仅备查（f040 兜底版 1px 环亮度比 1.32–1.61，远差于原生帧），不作为交付。
- 素材：`idle_f{000,008,016,024,032,040}_*_aligned.png`（对齐版）与同名未对齐原图并存；对照表 `sheet_6frames_aligned_gray.png`、`sheet_6frames_unaligned_gray.png`；逐帧预览 `*_aligned_preview_gray.png`。

#### 六帧落地为 enemy1011（部署记录）

**用途：与 enemy101 并排对比新 idle 的实际效果；除 id、名称、idle 外行为数据与 101 完全一致。用户已开始观察，观感验收未提交。**

- 新建资产：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1..6.png`；`Assets/Animations/Enemy_1011_Idle.anim`（6 键 @0.1s、30fps、Loop、0.6s）；`Assets/Animations/Enemy_1011.controller`（101 控制器副本，仅 Idle 的 motion 换成新 clip，其余 6 个状态不变）；`Assets/Resources/EnemyPrefabs/Enemy_1011.prefab`（enemyId 1011、名称“持剑杂兵_1011”）。未改动 101、其它敌人、共享脚本或场景。
- id 由预制体**文件名**解析：`EnemyPool` 读 `Resources/EnemyPrefabs/` 并按 `Enemy_{id}` 解析，所以必须叫 `Enemy_1011.prefab`；写成 `Enemy_101_1.prefab` 会被解析成 101 撞号。
- 关卡接线：当前生效的不是 `Assets/Resources/StageConfigs/*`，而是 `Battle.scene` 中 `Battle Y Route Host` 的 opening/left/right 三个槽位，它们**都指向同一份** `Assets/Experiments/CurvedScroll/RouteTrialData/SmallBattle.asset`（stageId -9101）。该资产第 0 排原为 `[101,0,101,0,101]`；实测排布为 **索引 i → 世界 x = i − 2（列距 1.0 单位）**；`enemyIds` 序列化为小端 int32 十六进制，101 = `65000000`、1011 = `f3030000`。已改为 `[0,0,1011,0,101]`（1011 在画面正中 x=0，右侧 x=2 一个 101）。
- 相机为竖屏透视（aspect 0.5），世界 z=−1 处可见 x ≈ [−2.74, +2.74]；五列敌人本来就贴边，把新单位放最外侧会把横伸的剑切出画面。
- 尺寸/密度约定：素材按原生像素补边到 **1108×1108**（角色保持 590–618px、零重采样；脚底自底 210px、脚心 x≈549），导入设置与 101 相同（PPU 16、Center pivot、Point、Uncompressed、Tight）；单位 `scale = 3.15×16÷606 = 0.083168`，碰撞盒 local size/center ×2.1643，血条 `yOffset = 35.28`。公式见 `skill/workflows/image-asset-generation.md`。
- 世界等效与实测（Play Mode 并排）：精灵世界框高 5.759 对 101 的 5.760；脚底 y 均 −1.746；碰撞盒世界 0.502×1.287×0.036、中心 y 0.067（两者相同）；血条世界高 2.934（两者相同）；角色世界高六帧 3.07–3.21（101 为 3.15）。idle 实测节奏：每帧 0.095–0.104s、循环 0.596–0.602s，均匀无短帧（101 为每帧 0.30s、循环 0.6s）。
- 已知差异（不是尺寸问题）：新 idle 的剑横伸，可见宽度 2.7–3.0 世界单位（101 为 2.00），而列距只有 1.0，所以相邻会互相压叠；且两者 z、sortingOrder 相同，谁在上由渲染顺序决定。用户曾据此误认为“没有替换 idle 而是叠放”，实测预制体只有 1 个 SpriteRenderer、0 子物体，描边 Shader 两个 Pass 均采样自身 `_MainTex`，不存在额外叠画。
- 待办：观感验收；后续更多动画按部署约定执行。

#### 受击动画（进行中）：正面先行

**方案 A（每方向 1 次视频任务；首帧 = 末帧 = idle 锚点图；受击姿势参考图不上传，只用于写提示词与目检）。**

- 正面关键姿势参考图 v1 已生成（图生图，未上传作首尾帧）：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_front_keyframe_v1.png`，1024×1024 RGB、1,024,256 bytes，响应 host `r2.52image.xyz`（不透明图不受路由影响），usage total 8,350；并排对比图 `..._v1_compare.png`。客观核对（相对输入）：画布未变、脚底 828→828 未动、脚心 664.0→660.5、包围盒宽 −47/高 −15 px、主体像素 −3.7%，符合“轻受击压缩、原地不动”的预期。**姿态观感待用户目检。**
- 正面受击视频：2 次尝试均被服务端判失败，`error_code=RJ_INTERNALSERVICEERROR`（“model service is temporarily unavailable”），`cost.spend=0` **未计费**；task：`task_f97d9ff3600c4e428e9b9d4e210c8bd7`、`task_dcbd4a695e9c4617bacd8406592faf15`。参数：`seedance-2-fast`、4s、720p、1:1、无音频/水印、seed=-1，首尾帧同为 `clean_raw/f000.png`，输出目标 `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_front_v1.mp4`（未产出）。
- 请求与完整提示词记录：`Library/Locus/tmp/sword_enemy_hit_front_v1.json`（含两次尝试与失败详情）。
- 关键姿势图版本与验收（均**用户否决**）：
  - v1（脚底 828→828 未动、包围盒宽 −47 / 高 −15、面积 −3.7%）：幅度太小，像角色自己后仰调整，没有受击感。
  - v2（加强冲击 + 眯眼；脚底 810 = −18px、宽 −76、面积 −7.8%）：像踉跄了一下，仍无正面冲击感；且重画范围过大（腿脚区也变了 83.9%）。
  - v3（强调眼神 + 冲击；背景仍纯色、边框 std 0.62，测量可信；包围盒 834×715、脚底 851 = +22px、脚心 −10.5px、面积 +15.6%）：明显朝某个方向倾斜、幅度过大；过度强调表情，直接改变了原有面部特征。
  - 诊断：① v1/v2 的幅度被写进提示词的 light hit / no knockback 自己压住了；② v3 过度纠偏，而且**正面视角下“向后”不可见，模型只能用左右倾斜来表达后弓**，所以出现明显侧倾；③ 把“表情作为全帧最清晰读点”会促使模型重绘面部结构，破坏身份。
  - 待验证的 v4 配方：幅度取 v1/v2 与 v3 之间，且只用**正面可见**的读点（双肩上耸、脖子压缩、胸部压扁、手臂被动反应、膝盖受力微屈），显式禁止身体纵轴左右倾斜与头偏离骨盆中线；表情只允许**轻微眼脸收窄**，并冻结眼睛/嘴/头盔面罩的大小、形状、位置。
  - 更根本的问题：方案 A 里关键图**不上传**，对视频没有任何约束力（只影响文字描述）。若正面冲击连续读不出来，应考虑方案 C（每方向两段严格首尾帧，把关键图当作被锁定的“受击极值”帧），代价是每方向 2 次计费。
- 待办：等视频服务恢复后重试正面；验收通过后再按同规格做左右两侧（左右提示词必须显式禁止旋转 / 四分之三视角 / 镜像）。
- 正面视频 v1（方案 A）：**已产出**。task `task_b081681c404543c99248ee02e04b6cef`，服务端 `success`（本地观察 310s），`cost.spend=0.48888`（按原值记录）。文件 `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_front_v1.mp4`，1,599,639 bytes；容器 `ftyp/uuid/free/mdat/moov` 校验通过、声明长度 = 实际长度；解码实测 960×960、24fps、97 帧、容器时长 4.042s。
- 客观核对（相对锚点 f000：面积 133,690 / 533×590 / 底边 777 / 脚心 623.5）：脚底漂移 1px、脚心漂移 1.5px（**钉住**）；首帧与末帧均 +1.0% 面积、宽 +1px、高 +0（**起止 ≈ idle，符合要求**）；但偏差 >2% 的区间是第 18~73 帧（t=0.75~3.04s），**受力不是开场即到**；且全程面积只增不减（峰值 +9.2%）、左缘在 23~163px 之间大幅移动（剑被甩出去），**轮廓是“外扩”而非“压扁”**。接触表 `Library/Locus/tmp/sword_enemy_hit_front_v1/contact_sheet.png`（每 8 帧 + 峰值帧），单帧同目录 `f000.png` 起。
- 正面视频 v1 用户**验收否决**：“看着像角色充气一样，没有姿态的变化，表情也没有受击感”。
  - 失败机制（按因果排序）：① 提示词里写了 “the upper body briefly reads **shorter and slightly wider**” ，模型把“变宽”直接实现为整体放大 → 看起来就是充气；② 同时把倾斜/迈步/摔倒/旋转/位移全部禁止，模型只剩下“体积变化”这一条通道可以表达冲击；③ 方案 A 下参考图不上传，模型对目标姿势没有视觉锚点，只能用最便宜的形变（缩放脉冲）满足“首=尾 + 有反应”；④ 面部被显式冻结（因为 v3 被否决的理由是改了原有面部特征），于是表情零读点；⑤ 4s 时长 + 首尾同帧，也促使模型用一个能回到原点的简单函数填满全片。
  - **矛盾点必须由用户裁定**：“表情要有受击感”与“不得改变原有面部特征”在“纯文字 + 视频模型”下是相互冲突的；后续要么允许受击瞬间小幅表情变化（身份几何冻结），要么放弃表情读点只做姿态。
  - 未做的事情：未抽帧去背、未对齐、未部署、未新增任何收费任务。
- 正面视频 v2（修正版：情景层 + 尺寸红线 + 关节读点 + 绿幕锚点 + 允许受击瞬间小幅表情）：**已产出，待验收**。task `task_39f03a1065f5447bb302ab4e81bfe03b`，`success`（本地观察 248s），`cost.spend=0.48888`；文件 `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_front_v2.mp4`（1,989,310 bytes），容器与解码校验均通过（960×960、24fps、97 帧、4.042s）。
  - 锚点换成**纯绿幕 #00B140**（角色 0% 偏绿像素，绿幕安全）：由已部署 idle 首帧合成，位置与 idle 一致（底边 777、脚心 623.5）。
  - 客观对比 v1 → v2（关键改善）：脚底漂移 1px → **0px**；面积下限 “锚点 +0.6%” → **“锚点 −0.9%”（首次出现低于 idle 的压缩）**；面积上限 +9.2% → **+5.1%**（膨胀减轻）；偏差 >2% 窗口 0.75–3.04s → **0.54–1.67s**（更早更短）；首/末帧 +1.1%/+1.3%（回到 idle）。
  - 仍存问题：受力仍在 **0.54s** 附近才开始（提示词要求 0.05–0.15s），节奏偏慢；绿幕不够纯（逐帧边框 G 161.6–169.2，std 最大 17.2），且角色边缘有 **1–2px 绿色溢出（4,212–5,194 px，占主体 ≤3.43%）** → 后续抠图需用**绿色优势阈值键 + 边缘去污染**，而不是简单颜色距离。
  - 过程失误：本地守候脚本查询时把 headers 传成了 data（位置参数），导致 20 次查询全为 `TypeError`；任务本身未受影响。
  - 待办：用户目视验收 v2；未抽帧、未去背、未对齐、未部署；未新增付费任务。若仍不通过，建议转方案 C。
- 受击 6 帧选帧（已由用户确认）：源帧 **16 / 48 / 76 / 80 / 84 + 已部署 idle 首帧**；理由与数据见选帧图 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_front_v2_selection.png`（上半身变化量：f12=16.0 → f16=46.4 跳变；f16–36 为外扩相、f44–72 为收缩相；f80 变化量 22.7、顶 190≈idle 的 188）。原始未抠图帧在 `Library/Locus/tmp/sword_enemy_hit_front_v2/selected/`。
- 受击帧抠图（按用户要求走 GPT 生图流，不使用本地阈值抠图）：5 帧（16/48/76/80/84）发图生图语义去背（输入 = 绿幕底未抠图帧，提示词 = 已验证模板改为 remove flat chroma-green + remove green spill）。**结果 5/5 全部落在坏路由 `r2.52image.xyz`（回显不含 `background`）→ `color_type=2` RGB、无 alpha**；usage 各 8,196 tokens；输出在 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_front_gpt_cutout/`。路由指纹累计：坏路由 8/8 均为 RGB。
  - 坏路由输出的实测：背景被换成**近白（249–251，std 4–6，非棋盘格）**、主体内绿色残留 **0**（去污干净），但**角色被重新缩放/位移**（脚底相对源帧差 +9 / +1 / +6 / −32 / −24 px，包围盒高度也变）→ 即使事后抠白底也会破坏逐帧脚底锚点，不能视为可用。
  - 关键结论（影响后续对齐）：**GPT 生图流会逐帧改变角色缩放**。对受击动画而言，逐帧按包围盒高度归一化会把“压缩”这个姿态本身归一化掉，所以对齐必须用「**单一全局缩放 + 逐帧脚底锚点**」，并单独量化残留的逐帧缩放差。
  - 逐帧探针（按用户建议的“一帧一帧 + 探针中止”协议）：帧 16 再次命中坏路由 `r2.52image.xyz`（回显不含 background），**立即中止，仅消耗 1 次**而不是 5 次。至此坏路由连续 **6/6**（相距约十几分钟，说明该路由会粘连一段时间）。
  - 深度排查结论（推翻“CDN 压平 alpha”的猜测）：16 次请求汇总后确认接口是**多后端路由**，至少 3 套后端：G1 `cdn.jd23kjs.work`（无回显、in 833/out 4153）、G2 `img.zxai.us`（回显含 background、in 870/out 1683）、B `r2.52image.xyz`（回显不含 background、in 1445–1513/out 6732）。**G 类 6/6 均原生 RGBA；B 类 10/10 均无 alpha 的 RGB**，无例外。
  - 排除重编码的证据：好/坏两种 PNG 都带生成器特征 chunk `caBX`（重编码会丢失）；坏输出背景是**带噪声的近白**（边框 240–254，恰好 255 仅 4.75%–12.45%）→ 是模型自己画了底。
  - 机制推断：B 类像**降级回退**（首选后端忙/不可用时顶上）；G/B 会成段交错（相隔 32 秒即可能切换），与请求速率无关。
  - 待办：等路由切换后再逐帧抠图；不自行重试、不自行改用本地抠图；未对齐、未部署。可选项：试同族模型 `gpt-image-2.5-flare`（尚未实测，需授权）。
  - 抠图路线对比实测（免费后处理实验，非 API）：① 变体 A = 对**原始绿幕帧**做绿优势阈值键 + 绿通道封顶 + 1px 环去污染 → 边缘比 **0.63–0.67**（已部署 idle 原生 alpha 基线 **0.61**）、全透 765k–771k 各帧一致、脚底 777 / 脚心 623.5 逐帧锁定（整数归位 ≤3px）、几何无重采样；② 变体 B = 对 **B 后端近白底输出**做白底键 + 单一全局缩放 → 边缘比 **2.31–2.61**（亮白边）、全透 360k–529k 不一致（底未抠净）、残留逐帧缩放 ±7%、帧 16 脚心错位到 570。
  - 结论：**键绿幕（原始帧）明显优于键近白底（B 输出）**，变体 A 的边缘质量与 GPT 原生 alpha 同级。产出：`sword_enemy_hit_front_keyed_A_greenkey/`、`..._B_whitekey/`；预览表 `sword_enemy_hit_front_keyed_A_greenkey_sheet.png` / `..._B_whitekey_sheet.png`。
  - 待办：由用户决定是否采用变体 A（注：它不是 GPT 原生 alpha）或继续等 G 类时段重跑 GPT 抠图；均未部署。
  - 变体 A 的后续优化（均免费）：用户发现旧版 A 仍有**绿色残边**（我的亮度指标漏掉了它）。逐个修正后的权衡：
    - A（旧：绿通道封顶）gEx1px **−8.8**（基线 −46）✗绿边；亮度比 0.66 ✓描边在
    - A2（反混合 + 2px 环颜色替换）gEx1px **−48.0** ✓无绿边；但亮度比 **0.88** ⚠可能把深色描边替换掉
    - A3（只反混合，不改环颜色）gEx1px −10.3 ✗仍有淡绿；亮度比 0.70 ✓——因为 alpha 坡度（(60−gEx)/40）把中段 alpha 给高了，反混合用错系数
    - A4（线性色键 alpha = 1 − gEx/gEx_bg）gEx1px −17.3，但仍给背景留下极淡绿雾（半透像素 60 万+）✗
  - 教训：① 针对“绿边”的验证指标不能只用亮度或整环均值（材质差异会干扰），也不应该自己瞬间写一个未校准的差分指标；② 在看不见结果的情况下反复盲改，风险高于收益——已经把 A2/A3 两版材料交给用户目检裁定；③ GPT 原生 alpha 仍是最可靠的路线，只是受路由制约。
  - **用户裁定（2026-10-03）：A2 总体合格** —— 即“**绿幕视频 + 优化后的 A2 抠图（反混合去污染 + 2px 环颜色替换）可以达到验收水准**”；GPT 抠图仍是最优解，因此往后固定为：**先发单次探针试能否拿到支持透明的后端，不行再自行抠图**。经验总结已写入 `memory/hit-animation-video-pipeline-lessons.md`，SOP 已写入 `skill/workflows/character-hit-animation-video-workflow.md` 第 6.A 节第 8 条。
  - 下一步（待用户授权）：把这套 6 帧按 idle 的部署约定补边到 1108×1108（PPU 16 / Center pivot / Point / Uncompressed），建 `Enemy_1011_HitFront.anim` 与控制器状态。
  - **已部署（2026-10-03，用户授权）**：
    - 新素材：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_hit1..5.png`（1108×1108、PPU 16、Center pivot (554,554)、Point、Uncompressed——与 idle 逐项相同）；第 6 帧直接引用 `Enemy_1011_idle1.png`（不新建素材）。
    - 新 clip：`Assets/Animations/Enemy_1011_HitFlash.anim`（30fps，键 0/0.15/0.30/0.45/0.60/0.75 → hit1..hit5 + idle1，Loop 关，stop 0.9）。
    - 接法与 101 完全一致：只把控制器 `HitFlash` 状态的 motion 换成新 clip（`Enemy.cs:810` 是 `_animator.Play("HitFlash")`，按状态名播放，**代码未改**）；未改动 101 资产、共享脚本、场景。
    - idle 降速：控制器 `Idle` 状态 `speed = 0.75`（实测每帧 0.1s → 0.133s，循环 0.6s → 0.8s）。受击速度参考 101 的 HitFlash（2 帧 / 每帧 0.15s），本次采用**每帧 0.15s、6 帧共 0.9s、不循环**。
    - Play Mode 实测：受击关键帧映射 0→hit1 / 0.15→hit2 / 0.30→hit3 / 0.45→hit4 / 0.60→hit5 / 0.75→idle1 ✓；完整播放每帧 0.148–0.152s，0.757s 起显示 idle1 并无缝接回 Idle；idle 每帧 0.132–0.137s（=0.1÷0.75）✓。
    - 方向规则（左/右/正面）**未做**：这是 2026-10-03 正面受击部署时的历史快照。后续已完成并接入 `HitLeft` / `HitRight`，当前最终规则见 `Locus/knowledge/plan/enemy1011-animation-deployment-handoff.md`。
  - **事故与修复（2026-10-03）**：用户发现 1011 的图像看起来偏到左边一列（实际在 col2）且与邻兵重叠。
    - 测量根因：补边时我把“**脚心锚到画布中心**”，而源素材的角色脚心在 x=623、画布中心是 480（脚心在中心右侧 143px）→ 整幅画被**向左推了 0.74 世界单位**（列距 1.0，所以看起来正好差一列）。质量中心实测：外部源素材 **−0.08** 单位、基准 101 **−0.04**、被改前的 1011 **−0.85**。
    - 修复：撤销横向重锚定，改为“**源画布居中补边**”（x+74 / y+120），纵向仍与 101 的脚底地面线一致；重建 11 张素材（idle1–6 + hit1–5）。重建后质量中心 **−0.04 ~ −0.09** 单位，与 101 同级。导入设置、clip 键引用、prefab 均未变动（同路径覆盖，GUID 不变）。
    - 遗留（美术固有）：该美术的**剑向左侧伸出约 1.7 世界单位**（相对身体中心），而列距只有 1.0 → 剑仍会跨进相邻列，靠对齐无法消除；要彻底消除只能改美术（剑的持姿）或加大阵型列距。
    - 另记：攻击目标列由 `InputManager.GetColumnFromScreenPosition` 决定——只考虑“有前排敌人的列”，且与输入位置的屏幕 x 差距超过**半列宽（Screen.width/10）就阻断攻击**。当时战场只 2 个敌人（1011 在 0.50W、101 在 0.884W，中间三列全空），所以 0.60–0.70W 输入一律不触发，很容易误判为“打不到”。已把测试排布改为满排 `[101,101,1011,101,101]`（19 个采样点阻断 0 次）。

#### 动作动画 Attack / Dead / Walk（2026-10-04；Walk v1 已被用户否决）

| 动作 | 版本 | task id | 成片 | 大小 (bytes) | 费用 | 结论 |
|---|---|---|---|---|---|---|
| Attack | v1 | `task_e92bbc89a802478db2863251b66d9abb` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_attack_front_v1.mp4` | 2,015,371 | 0.48888 | 中间版 |
| Attack | v2 | `task_67c87dbe7385459cb37f16aa958bee7e` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_attack_front_v2.mp4` | 2,222,219 | 0.48888 | **已验收并部署**（16 帧 → `Enemy_1011_attack1..16.png`；clip `Enemy_1011_Attack.anim` 18 键 / 30fps / stop 2.9，命中键 2.000s） |
| Dead | v4 | `task_e3d84ebe3403436192b5c1d3697488aa` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_dead_front_v4.mp4` | 1,960,796 | 0.48888（会话记录；`record_v4.json` 的 attempts 无 cost 字段） | **已验收并部署**（6 帧 → `Enemy_1011_dead1..6.png`；clip `Enemy_1011_Dead.anim` 6 键 / stop 0.6） |
| Walk | v1 | `task_46dfa536ac1a40e6b5d16af3612a4e96` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_walk_front_v1.mp4` | 2,486,774（sha256 `887d5cf408d37848…`） | 0.48888 | **用户否决**（像散步，不像行军；测量见下） |
| Walk | v2 | `task_18fe8d8af7554019a6b6905c5150c540` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_walk_front_v2.mp4` | 2,908,962（sha256 `f377bcab628655b3…`） | 0.48888 | 手臂问题已修，但**真实超框**，需 v3（见下） |
| Walk | v4 | `task_18e87a74532b4f1f86161a1b00b318cf` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_walk_front_v4.mp4` | 2,590,328（sha256 `763084a9aa1dea88…`） | 0.48888 | **用户已验收**（语义改为冲锋跑；v3 未生成） |

- 四支视频参数一致：`seedance-2-fast`、请求 4s / 720p / 1:1、无音频与无日水印、seed=-1；**实际输出均为 960×960、24fps、97 帧**；首尾帧同为 `Library/Locus/tmp/sword_enemy_hit_front_v2/anchor_green.png`（960²；绿键前景框 x[155,697] y[187,776]，最小留白 16.1%，背景中位 RGB(0,177,64)）。
- 补录说明：本段任务此前只存在于 `Library/Locus/tmp/*/record*.json`，台账漏记，2026-10-04 补录。
- Walk v1 否决的客观测量（`Library/Locus/tmp/sword_enemy_walk_v1/`）：自由臂一侧（腰高带左缘）全程只移动 **8 px**；躯干横向摆动 **31 px（0.16 世界单位）**；跨步周期 ≈ **16 帧（0.67s）= 1.5 步/秒** 的慢节奏；抬腿峰值 46~49 px（身高 8%）；最低像素行 756..777（支撑脚并不固定）。
- **根因在提示词（我方）**：v1 写了“剑保持完全相同的持手、握法、高度与刃角，只允许随手臂跟随 ≤10°”，把持剑臂锁死，模型连另一条手臂一起冻结，只剩身体晃动 → 读成散步。教训：**行进/位移类动作必须把手臂摆动写成主读点并给出度数区间**；对剑只约束方向红线（不高过肩线、不指向镜头、不横扫、横向伸出不超给定姿势），不能锁角度。
- **Walk v2 实测**（`compare_v1v2_walk.py`，主力指标同口径）：**手臂摆动已解决** —— 肩→手带左缘行程 66 → **234 px**，自由臂一侧（剑带左缘）10 → **322 px**，抬腿 49/46 → **70/66 px**。但**出现真实超框**：23 帧最小边距 < 40 px，最严重贴到上边缘（边距 **0 px**）；f029 整体高度 **768 px**（基准 590 px，高 30%）。顶部贴边像素中位 RGB(66,90,103) 属金属色，**排除绿键误判**。成因：提示词里的“硬摆臂”被模型做成**剑挥过头顶**。另：最低行 753..791（较基准下沉 15 px）、躯干摆动 260 px、跨步周期 19 帧（自相关仅 0.307，节奏不如 v1 规整）。
- **Walk v3 对策**（只改三处，其余逐字沿用 v2）：①加回 dead v4 验证过的「绝对防超框——最高优先级、四边 ≥8% 空白、碰到边缘就把姿势做紧凑」；②新增「高度与接地红线」——头盔顶与靴底各自钉在同一行（靴底误差 2~3 px 内）；③摆臂段补上界——双手与剑刃任何时刻不得高过肩线、不得抬到头侧、不得越到身体另一侧。
- 复用教训：**只要动作量大，防超框段必须跟提示词一起走**（dead v4 当时也是这个组合才过），否则“动作到位”会以“出框”为代价。
- **Walk v4（冲锋跑）与本地抠图的边界（2026-10-04）**：v4 验收后取 6 帧，但本地绿幕键始终带「灰色半透明脏边」，且**不是参数问题**：
  - 判据：过渡带(d 20–80)/实体 = v4 **0.104–0.118**，而 dead 0.073 / attack 0.077；边缘相邻实体环 gEx **−30** vs idle −54 / attack −66.6；而同 d 的 alpha 映射曲线与已验收素材一致（甚至更紧）→ 差异来自**源本身（快速跑动→边更软、描边偏绿）**；噪声底抬高（清压缩幽灵残影：可见像素 274k→146k）、外扩环半径自适应（反而更灰）、α<12 归零（≈0）都无法解决。
  - 结论：应转 `skill/workflows/image-asset-generation.md` 的 **GPT 语义去背分支**（与已验收 idle1 同源路径）；1 帧探针 0.48888 量级以外为图像调用。
- **Walk 部署记录（2026-10-04）**：素材 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_walk1..6.png`（1108²、PPU16、Center(554,554)、Point、Uncompressed、alphaIsTransparency——逐项复制自 idle1 导入设置，`TextureImporterSettings` 不含压缩项所以 `textureCompression` 单独设）；clip `Assets/Animations/Enemy_1011_Walk.anim`（由 `Enemy_1011_Idle.anim` CopyAsset 后仅替换 6 个 sprite 引用，Loop 开；**stopTime 由复制来的 0.5333 改为 0.6**，使 6 姿势等距 0.1s——注意 `clip.length` 显示值滞后于 stopTime）；控制器 `Idle/AnimatorStateTransition[4]/Walk` 的 motion 由 `Enemy_101_Walk` 换为 `Enemy_1011_Walk`。
- **Play Mode 实测**（激活中的 1011 实例）：speed=1 依次 walk1→walk6（约每 0.1s）、t≈0.58 回环到 walk1（norm 1.01）；speed=2 时 0.3s 一圈（正好一拍一排）；Play("Idle") 后 sprite 回 idle1，位置不变。**坑**：`FindObjectsOfType<Enemy>(true)` 会先命中对象池里**未激活的克隆**（`activeInHierarchy=False` 时 `Animator.layerCount=0`、Play 完全无效，很容易误判为接线错），测实例必须筛激活中的。
- **GPT 语义去背的新坑（本次 11 次调用实测，强烈建议写入流程）**：模型**不重画姿势，但会把整幅画随机放大**。逐帧按高度归一化的形状比对（对缩放不变）shapeIoU 0.946–0.976（= 同姿势），而需要的回缩倍率为 **0.740–1.000**（同一帧不同次调用：f027 = 0.820 / 0.917 / 0.958；f032 = 0.791 / 0.933 / 0.740）→ **单帧视觉验收不能证明动画可用**，必须做「帧间倍率一致性」择优（本次 f027 取 v3×0.958、f032 取 v2×0.933，其余 4 帧 ≤±1% 不重采样），并保留 v1/v2/v3 全部候选作为重试记录。对齐时**用单一全局偏移 + 仅 outlier 帧整幅缩放**，禁逐帧锚脚（会把跑步的腾空相位抹掉）。

#### 左受击 P1（方向受击，2026-10-04；历史生产记录，最终已验收并部署）

| 版本 | task id | 成片 | 大小 (bytes) | 费用 | 结论 |
|---|---|---|---|---|---|
| 视频 v1 | `task_062817282e4b4b5288e68d8642f94ff6` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_left_v1.mp4` | 1,796,328 | 0.48888 | 废：抬剑换姿势 + 2.3s 平台 |
| 视频 v2 | `task_1fda879142924efe860629206b4808f1` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_left_v2.mp4` | 1,778,478 | 0.48888 | 废：模型自创姿势，用户判「像向左倾斜、没有后仰、像摆 pose」 |
| 视频 v3 | `task_3992f2cba10041af902a0aff47a43e05` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_left_v3.mp4` | 1,801,808（sha256 `53627876…`） | 0.48888 | **待用户裁定**：首尾已回到同一 idle，但变形姿势仍冻结 1.21s |

- 三支参数一致：`seedance-2-fast`、4s / 720p / 1:1 / 无音频 / 无水印 / seed=-1；实际输出均为 960×960 / 24fps / 97 帧；首尾帧都是 `Library/Locus/tmp/sword_enemy_hit_front_v2/anchor_green.png`（v3 首次与末次复用同一张上传 URL）。
- 关键图 6 次（v1–v6，≈45,129 tokens）：`C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_left_keypose_v1..v6.png`；v3（身体）与 v5（头）被认可，v6 = v3 身体 + v5 转头，用户裁定「后仰幅度稍稍差一点点」。
- **三次视频同一失败模式**：模型必然把变形姿势冻结 1.2–2.4s（v1 2.3s / v2 2.4s / v3 1.21s），提示词里的「禁止平顶保持」三次都无效；v3 的上升段与回位段各只有 0.42s，且没有 overshoot。
- v3 客观测量（`Library/Locus/tmp/sword_enemy_hit_left_v3/metrics_v3.json`）：f001–f023 idle → f024 起跳 → f033 峰值 → **f039–f067 冻结平台** → f068–f078 回位 → f079–f097 idle；97 帧最低行恒为 775；上半身质点 x 460→531（+71）；肩线倾斜 −70→+8；峰值宽 740 px（idle 540 / 已认可 v6 558）；左带剑区 15,225→8,435，右带 0→11,706。
- 未抽帧选帧、未抠图、未对齐、未补边、未部署；HitRight 未开始。
- **抠图阶段（2026-10-04）**：选帧 v2（f024/f033/f069/f070/f071/f075）获用户验收后开始抠图。**GPT 语义去背探针 2 次均落 B 类后端**（`echoed` 无 `background`、`out_tokens=6732`、Color Type 2、近白噪声底）→ 按 SOP 停手未批量；转 SOP 兜底路线**本地绿幕键**（`Library/Locus/tmp/sword_enemy_hit_left_v3/local_key_v2_final/`，6 帧）：白底绿像素 0、bandRatio 0.066–0.077、羽化 226–228/209–214/164–176/134–153/84–115、ring gEx −19.2…−20.7。额外发现：**h264 压缩幽灵**会让可视 bbox 虚胖（f024 `[0,98,875,779]`），散射淡像素均色 ≈[150,50,178]（反混合放大的品红色度噪声）；新增可测量的一步「幽灵清除」（离实心剪影 >5px 且 alpha<0.6 的像素置 0）后 bbox 回到源帧（`[0,184,710,779]`）、淡像素带 3.97%→0.65%。待用户裁定本地产物或等 G 路由重试 GPT。
- 已作废但**未计费**：`Library/Locus/tmp/sword_enemy_hit_left_locked_v1/`（把 v6 极值当首帧的「锁帧」方案，未创建任务）。旧版判读：`Library/Locus/tmp/sword_enemy_hit_left_v1/verdict_v1.md`、`.../sword_enemy_hit_left_v2/verdict_v2.md`、`.../sword_enemy_hit_left_v3/verdict_v3.md`。

#### 右受击 P1（方向受击，2026-10-04；历史生产记录，最终已对齐并部署）

| 版本 | task id | 成片 | 大小 (bytes) | 费用 | 结论 |
|---|---|---|---|---|---|
| 视频 v1 | `task_71ccbe537c0548d9af0b9a5c0dea6613` | `C:/Users/Administrator/Videos/doubaoVideo/sword_enemy_hit_right_v1.mp4` | 2,160,235（sha256 `7daca7fe…`） | 0.48888 | **待用户裁定**：方向读点成立、中段无冻结平台；但剑臂被甩出画面左 135px、飘带飞到头盔上方 62px |

- 提示词：`Library/Locus/tmp/sword_enemy_hit_right_v1/prompt.txt`（依左侧成功案例 v3 重推，并做了约束冲突修订：解除与 idle 冲突的「剑不得高过肩线」、区分「固定根部」与「允许局部上身反冲」、去掉会冻手臂的 `no arm swing`、加不对称外扩上限与反平台节奏）；设计说明 `keywords_design.md`，优化前版本 `prompt_before_optimization.txt`。
- 参数同左侧：`seedance-2-fast` / 4s / 720p / 1:1 / 无音频 / 无水印 / seed −1；首尾帧均为 `Library/Locus/tmp/sword_enemy_hit_front_v2/anchor_green.png`；实际输出 960×960 / 24fps / 97 帧。
- 客观判读（`Library/Locus/tmp/sword_enemy_hit_right_v1/metrics_right_v1.json`）：上身质点 444→413（左移 31px）、头左移 ~30px、画面左肩带顶升 ~98px、画面右肩带顶降 ~109px、最低行恒 775；静止段只在 f4–f17 / f79–f97；剑臂最左到 x=20–24（idle 155）、左带 +9,445px、宽峰 688（+148）、f018 单帧异常宽 804；飘带 f060–f069 升至 y=126（高过头盔顶 62px）。
- **视频内容与 6 帧已被用户验收（2026-10-04）**：f019 / f030 / f051 / f054 / f057 / f075；排除异常 f018；同源 960²、脚底行 775，f075 变形量 12%。
- **GPT 抠图已完成**：6/6 输出均为 PNG Color Type 6 / RGBA / 960² / Alpha max 254 / 角落无棋盘格；输出 `C:/Users/Administrator/Pictures/gptGen/sword_enemy_hit_right_gpt_cutout/`，总览 `right_cutout_v1_sheet_white.png` / `right_cutout_v1_sheet_dark.png` / `right_source_vs_gpt_v1_sheet.png`，报告 `gpt_cutout_report_right_v1.json`。
- GPT 输出逐帧几何未通过，bbox 宽高 `642/588, 797/728, 787/707, 747/723, 634/680, 727/781`、底行 `779,872,864,874,839,907`、最大 shift `[-68,-61]`；需先做统一几何对齐/异常帧整幅缩放，再补边到 1108，原始 GPT 输出不进入 Assets。
- 未对齐、未补边、未部署；HitRight 尚缺判定/播放规则（同左侧 §4）。

#### Launch 后半段落地尾段（2026-10-05；历史生产记录，最终已验收并作为 Fall 段部署）

| 版本 | task id | 成片 | 大小 | 费用 | 状态 |
|---|---|---|---:|---:|---|
| Landing tail v1 | `task_9f5c4081f24c4206afeae33c071e19c3` | `C:/Users/Administrator/Videos/doubaoVideo/enemy1011_launch_landing_v1.mp4` | 2,107,363 bytes | 0.48888 | **待用户验收**；空中→接地/倒地终态，不含起身 |

- 首帧：Rise 实际末帧 `Library/Locus/tmp/sword_enemy_launch_v1/frames_rise_v1/f097.png`；尾帧：Dead v2 倒地参考 `landing_tail_dead_lastframe_v2_green.png`。
- 解码：97 帧；f001–f015 仍在下降，约 f020 开始进入倒地终态，f020–f097 保持同一落地姿态；脚底最低行由 699→813 后稳定在 671 的可见包围框指标（源为绿幕 RGB，未抠图）。
- 任务曾两次遇到本地 TLS/路径下载失败，服务端始终 `success`；第三次使用同 task 结果 URL 通过 curl 续传成功，未重建任务、未新增费用。
- **范围边界**：本段不制作起身回 Idle；用户验收尾帧后，再单独准备 `Launched_Land`/起身动画和代码/Animator 接线。

### Enemy 1011 最终 Unity 动画验收（2026-10-05）

- 用户确认 Enemy 1011 的全部通用动画已验收：Idle、正面/左/右受击、Attack、Dead、Walk、Launched_Rise、Launched_Fall、Launched_Getup。
- 这些动画的动作类别、姿态因果、锚点、节奏和状态语义可作为后续所有敌人的通用动作设计参考；1011 的剑侧、体型和像素几何不作为其他敌人的直接复制目标。
- 正式部署交接与当前播放规则：`Locus/knowledge/plan/enemy1011-animation-deployment-handoff.md`。
- 1011 姿态与 Prompt 复用参考：`Locus/knowledge/memory/enemy1011-animation-pose-reference.md`。
- 当前工程没有独立 Landing Clip；`landing1..6` 作为 Fall 段帧，真实落地后由运行时代码播放 Getup。

### Seedance 路线视频

| 段 | task id | 最终文件 | 大小 | 结论 |
|---|---|---|---|---|
| N3→E3 | `task_db0f2676cf0541929b2bd94255623daf` | `C:/Users/steam/Videos/doubaoVideo/N3toE3_village_supply_yard_480p_4s.mp4` | 3,145,128 bytes | 用户验收 |
| E3→EW | `task_a553daccef564d35b66602bd9d4308ed` | `C:/Users/steam/Videos/doubaoVideo/E3toOuterWoods_480p_4s.mp4` | 2,715,939 bytes | 用户验收，随后部署 Unity |
| EW→J3 | —（用户裁剪拼接） | `Assets/RouteData/FakeStage01/Presentations/Videos/EWtoJ3.mp4` | — | 用户验收；Unity 读取约 7.40 秒；严格首尾帧版本因侧路瞬移被否决 |

- 前两段参数：`seedance-2-fast`、4 秒、480p、adaptive、无音频、无水印；seed 分别为 184729 与 731842。
- 服务端：两次均 `completion_tokens=39891`、`cost.spend=0.22339`（币种未推断）。
- 尺寸实测：Unity 侧读取到约 4.04 秒、560×752；**不要把请求档位等同于实际像素尺寸**。
- 对照：原 10 秒 720p 任务对象大小 12,912,149 bytes，约为本次 4 秒 480p 的 4 倍；因时长与分辨率同时变化，不能据此推导单一压缩比。
- 原 10 秒任务的完整 prompt 未保存，后来 4 秒任务用的是用户确认过的重构版，不能称为逐字复用。
- 同一旧视频测速（当次）：代理约 3.2 KiB/s，直连约 16 KiB/s；仅为当次结果。
- 下载记录：直连首轮 180 秒收到 2,473,984 bytes 后超时并保留 `.part`，用 `--continue-at -` 续传约 89 秒完成余下 671,144 bytes；另一次首轮收到 1,015,808 bytes 后续传完成。
- 轮询记录：首轮 12 次查询仍为 `generating`，停止的是本地等待而非服务端生成；后续单次查询返回 `success`。不可把本地退出码判成服务端失败，也不可自动再次付费生成。

## 二、FakeRoute 视频与节点部署状态

- N3 的视频挂在 EV 的出边 `ev_to_n3` 上；N3 出边的 `N3toE3` 是另一段视频，两者不可混淆。
- 已部署资产：`Assets/RouteData/FakeStage01/Presentations/Videos/N3toE3.mp4`、`Assets/RouteData/FakeStage01/Presentations/N3toE3.asset`、`Assets/RouteData/FakeStage01/RouteNode_FakeStage01_E3.asset`。
- E3 节点无战斗、无奖励逻辑，单出口 `e3_to_j3`，必须等待玩家点击，不自动转场；保持 N3→E3→J3，不创建 Scene。
- 节点脚本绑定事故与修复：旧节点磁盘 `m_Script` 为 fileID=0（节点类型与 Stage 类型共用 `FakeRouteConfig.cs`）。修复方式：原脚本连同 `.meta` 移到 `Assets/Scripts/RouteFake/FakeRouteStageConfig.cs`（保留原 GUID 与 Stage 类型），`FakeRouteNodeConfig` 独立为 `Assets/Scripts/RouteFake/FakeRouteNodeConfig.cs`；再用 SerializedObject 设置 `m_Script` 并保存，恢复 15 个已有节点，未删除重建。
- N3 尾帧被覆盖的原因与修复：N3 曾被配置为 `battleBackground=N3_Battle`，导致进入战斗后覆盖视频尾帧。已验收的最小配置修复：`Presentations/EVtoN3.asset` 使用 Video 且 `holdLastFrameAsBackground=true`；`RouteNode_FakeStage01_N3.asset` 的 `battleBackground=None`、`routeChoiceBackground=None`；N3→E3 表现同样 `holdLastFrameAsBackground=true`；E3 不配置静态战斗/选择背景（只改配置，未改通用 Presenter 算法）。
- N1 仍保留 `BattleRoad` 背景，不存在"逐字段复制 N1"的做法。
- 当前 Skip 行为：对非循环视频执行 12 倍速播放至结束，不是立即丢弃尾帧。
- 记录事故：N3 静态背景清空之前曾被错误报告为"部署完成"。

## 三、路线空间参考（Blender 灰模）

- Blender 工程：`C:/blenderFiles/Route_SpatialProof_Clean_2x3.blend`；工作 Scene `Route_SpatialProof_Clean`（保留旧 Scene，禁止把旧网格混入当前渲染）。
- 机位参考图：`route_clean_EW_2x3.png`、`route_clean_Merge_2x3.png`、`route_clean_Shared_2x3.png`、`route_clean_J3_2x3.png`，均在 `C:/Users/steam/Pictures/gptGen/`；这些是彩色灰模，不是最终美术图。
- 相机：`EW_Approach` (0,-18,3)、`Merge_Approach` (0,-3,3)、`Shared_Road` (0,18,3)、`J3_Arrival` (40,72,3)；均 35mm、Vertical sensor fit、sensor_height=32、clip_end=1000，输出 1024×1536、100%、像素 1:1（早先的 50mm 只是建议，不是最终值）。
- 几何：汇合位于 y≈10；共同道路经两段圆弧向右绕行到 x≈40、再到 y≈86，长度约 98.8 Blender 单位（空间示意，不是游戏时间或真实米数）。弯内侧 `Inside_Bend_Bank` 是大体块遮挡，美术阶段需转译为土坡/树林，不能照搬成人工墙。
- 进度：已完成静态视图检查，未生成或验收连续相机运动；用户最终用一段裁剪拼接视频完成 `EW→J3`，文件 `Assets/RouteData/FakeStage01/Presentations/Videos/EWtoJ3.mp4`，Unity 读取约 7.40 秒，已验收。关键帧分段不等于创建逻辑节点，当时未授权由中间帧自动建节点。
- 图源与弃用：EW 本地风格源 `changbanpo_village_outer_woods_dusk_tail_v1.png`；旧 J3 候选 `changbanpo_J3_night_merge_fork_v1.png`（未体现长距离，不再作为免修改尾帧）；`changbanpo_EW_side_road_reveal_mid_v1.png`、`changbanpo_EW_incoming_road_merge_mid_v2/v3.png` 未通过几何验收，不可复用；旧 `blender_*` 多轮截图存在裁边、道路重叠与比例错误，当前以 `route_clean_*_2x3.png` 为准。
- 单张 EW 图片无法精确反求焦距与相机高度，这套机位只能称几何参考，不能称 EW 精确标定。
