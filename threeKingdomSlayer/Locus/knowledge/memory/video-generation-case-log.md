---
id: kd_b36a0f8e-c80c-4114-824c-e74466d1d189
injectMode: inherit
summary: 1021素材台账现状：ShieldBreak视频/修订六帧/T79透明与分件已验收并接入第一关；HitFlash与持盾死亡（含`Enemy_1021_Dead.anim`）已游戏验收；HitRight已接入待游戏验收；CowardIdle的呼吸最高点图、循环视频与6帧透明素材均已验收；CowardHit v1/v2/v3 验收不通过，v4 视频已生成待看片；v4 受击峰值图不通过。CowardLaunched 单图 10 秒完整链 v2 已提交一次（task_78a8dfe0…）并下载解码：虽为停格结构（174/241 帧静止 + 33 次单帧换图），用户反馈“效果挺好”，已按 1011 三段式选出 18 帧（用户剔除待机首末后剩 16 帧），并按 T=80 完成抠图：`keyed_t80_v1/`（960×960 RGBA、半透明0/幕布残留0），**已验收**。旧 6 秒带剑版验收不通过。1051：v2 三次（含透明探针）不通过；**v3 阶段A 已验收**；**Idle 6 秒源片与 6 帧循环帧也已验收**；**射箭 v1（10s）验收不通过**（箭矢拿反、射击姿态与105不一致），待拍板参考图与箭矢朝向约束。去背/抠图仍暂缓。不自动自检。
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

### Enemy 109 Striking v1（2026-10-08，用户否决：没有正朝玩家刺击）

- 用户已批准完整 Striking 提示词、首帧与收费规格；本次只创建了 **1 个**视频任务：`task_193cf338cbf846a783ad47296ba7f0ef`。
- 参数：`seedance-2-fast`、4 秒、720p、1:1、无音频、无水印、seed `-1`；只使用 `first_frame`，无尾帧、无普通 reference_image。
- 动作：Charging 首姿势 → 一次正面长枪前刺 → 收枪/战马急停 → 正面低枪戒备；不包含返航循环，不强制回 Charging 或 MountedIdle。
- 首帧：`Library/Locus/tmp/cavalry_concept/cavalry_strike_first_frame_charging1_green_v1_960.png`（已验收 Charging1 内容的 960×960 RGB 绿幕副本）；SHA-256 `df498ba127c9094ae17270221c254d25eeaa4e8c68500fe08560993d39fb5d2c`。
- 实际提交英文提示词：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_v1.en.txt`；SHA-256 `8f19829bede15932cc7b9641f0246bf28d4ff3bad868ad2fd6e87be2fecc4faf`。中文对照：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_v1.zh.txt`。
- UTC 09:14:43 创建返回真实 task id 和 `queued`；首轮 12 次限时查询结束时仍为 `generating`，随后第 13 次查询同一 task 返回 `success`，未重复创建任务。API usage：`completion_tokens=87300`、`total_tokens=87300`；cost 原值 `spend=0.48888`（不推断币种）。
- 成片已下载：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v1_720p_4s.mp4`；2,297,494 bytes；SHA-256 `501513c4199cf87aa5450a88ceccddcd508d31b08c5ad64679ae4c560f127b4a`。
- 技术校验：MP4 顶层包含 `ftyp / uuid / free / mdat / moov`；FFmpeg 完整解码成功，实际 97 帧、960×960、24fps、约 4.04 秒，无音频。
- 用户验收结论：**不通过**。角色没有形成正朝玩家的纵深刺击；长枪在画面左侧保持横向/斜向展开，未形成“枪尖朝镜头/玩家”的正面命中读感。
- 抽帧诊断：`Library/Locus/tmp/cavalry_concept/striking_v1_rejected_review/striking_v1_actual_motion_contact_sheet.png`；原视频 SHA-256 未变，未做任何修改。关键问题出现在约 f021–f038（0.83–1.54 秒）刺击段。
- 当前边界：下载、容器与完整解码校验已完成；视频美术验收已否决。未做去背、选帧或 Unity 部署；原视频保留作失败参考，不得直接进入生产。
- 修正方向：先制作并验收一张“正面纵深长枪刺击”关键姿势图，再用它约束下一次视频。关键姿势必须让枪尖朝向镜头/玩家、靠近画面中央前方，枪杆大部分沿深度方向缩短/被前端遮挡；持枪手仍在画面左侧，但不能把枪尖继续放在左侧横向展开。
- 请求/运行记录：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v1.request.json`、`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v1.create_response.json`、`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v1.runtime.json`。
- 前置 Windup/Charging 12 张透明帧已获用户验收并导入 `Assets/Sprites/Enemy/Enemy109/`，统一 1108×1108、固定补边 (74,120)，导入设置与 MountedIdle 一致（有效 Pivot 为 Center / (554,554)）；动画 Clip、Animator、Prefab 与视觉路由接线仍按用户要求延期。

### Enemy 109 刺击尾帧 v3–v5 与 video v2 准备（2026-10-08）

- 新方向最终确认为：**马体/马头向画面右侧侧身，骑手上身/面甲/视线面向玩家，长枪朝玩家纵深刺击，左手握缰**。单层人马 Sprite 不要求所有部位完全同向。后续收枪、完整回身和背向返航仍另行制作。
- v3 第一次请求返回 HTTP 401 `token invalid`，无图片；用户修复 API 并另行授权后生成 v3_attempt2，用户因骑手看向马头前方而否决，不能用作视频尾帧。
- v4 输出：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v4.png`；整体姿势获认可，但枪杆弯曲，未作为最终终点。
- v5 输出：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v5.png`；使用 v4 为唯一编辑参考，只申请修直枪杆的一次请求；用户明确验收。960×960 RGB、PNG Color Type 2、1,105,360 bytes；SHA-256 `fe14eeab03e7cce921c24457d190b1bfe6304125f779bd5f217174a7bc6ed71c`。
- v5 参数：gpt-image-2.5-sunburst、quality high、input_fidelity high、960x960、PNG、n=1；background 省略。本次 HTTP 200，路由 img.zxai.us；usage in 1741 / out 1683 / total 3424，cost 未返回，不推断金额/币种。
- v5 请求/响应：`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v5.request.json`、`C:/Users/Administrator/Pictures/gptGen/cavalry_striking_lastframe_v5.response.json`。Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_lastframe_v5.runtime.json`，已更新验收状态。
- 首尾帧 staging：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/first_frame_charging1_960.png` 与 `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/last_frame_striking_v5_960.png`，均 960×960 RGB、逐字节复制原图，未缩放/重定位/统一背景。
- video v2 Prompt：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v2.en.txt`、`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v2.zh.txt`。请求规格：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2.request-spec.json`。
- **2026-10-08 准备阶段**：当时未上传/创建；拟定 seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1，Charging1 first_frame + v5 last_frame，不混普通参考图。2026-10-09 实际创建/下载见下一条目。只做到命中，不收枪、回身或返航。
- v5 绿幕仍有颜色变化；已验收的是动作图作为视频终点，不是透明/统一背景的 Unity 成品。未做去背、抽生产帧或 Unity 部署。状态与具体边界见 `Locus/knowledge/plan/cavalry-enemy-visual-handoff.md` §10。

### Enemy 109 Striking video v2（2026-10-09，用户否决：举枪指向不是刺击）

- 用户明确批准创建；只创建一次 task `task_49dbbb7612ae42cda74dac3014a406e3`。参数 seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1；Charging1 first_frame + v5 last_frame，未混普通参考图。
- 服务端 success；usage completion_tokens/total_tokens=87300；cost 原值 spend=0.48888，不推断币种。UTC 创建 accepted 03:13:04，首次观测 success 03:15:50；该间隔含服务端排队、生成、查询及本地间隔，不称纯推理时间。
- 下载路径：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v2_720p_4s.mp4`，2,772,210 bytes；SHA-256 `54a72b49b4fcb5f1db960f83075f45655d8c31441f8b443ce7f66442cb7cddcf`。
- MP4 容器 `ftyp/uuid/free/mdat/moov` 全部有效，FFmpeg 完整解码退出码 0；实际 960×960、24fps、97 帧、约 4.04 秒，无音频。
- 诊断图：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2/diagnostic_review/motion_contact_sheet.png`。原像素诊断帧与解码报告同目录；这是验收预览，不是生产选帧。
- 用户明确**验收不通过**：骑兵不是完成转身后刺出一击，而是转身后举起长枪再指向玩家，动作语义错误。约 2.0–2.5s 过头举枪，约 1s 马蹄尘土；正确首尾图不代表中间有真实刺击。保留原视频，不选生产帧、不去背、不部署。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v2.runtime.json`；创建/上传、有限查询、分段下载、完整解码均已完成。原 task、v5 和 Charging1 保留，未生成第二个 task、未付费去背、未生产选帧或 Unity 部署。

### Enemy 109 Striking video v3（2026-10-09，用户否决：仍为指向不是刺击）

- 用户在否决 v2 后明确要求发起 v3；只创建一次 task `task_711a9851c69a44a39ccd1a56162dd505`，不重复 v2。
- 参数仍为 seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1；复用 Charging1 first_frame + 已验收 v5 last_frame，不使用失败视频作为参考。
- 新动作约束：先完成马体向右侧身，骑手持续看着玩家、长枪低于头盔；随后右肩驱动、右肘伸展、右手和整把直枪沿轴向玩家纵深前移一次。禁止举枪过头、旋枪瞄准、停顿指向、仅改枪向、不刺出。
- 英文 Prompt：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v3.en.txt`；SHA-256 `a37e2439dd4b13b184372cb894066c3295e1242ae230fd27186d35cd54a68c2d`。中文对照同名 `.zh.txt`。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v3.runtime.json`，当前 `user_rejected_pointing_not_thrust`；task 已 success、下载/解码有效，但视频美术不通过，只创建一次。
- 成片：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v3_720p_4s.mp4`，2,650,016 bytes；SHA-256 `039bb42a90ca3a97bb397384f3496a6bdd5327bb98b48dcaff96b704c282ac13`。MP4 `ftyp/uuid/free/mdat/moov` 结构完整；FFmpeg 全量解码退出码0：960×960、24fps、97帧、约4.04s、无音频。
- API usage completion_tokens/total_tokens=87300，cost 原值 spend=0.48888，不推断币种。有限查询到 success，同 task 4段 Range 下载；无重复创建。
- 诊断接触表：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v3/diagnostic_review/motion_contact_sheet.png`；连续出枪段 f045–f053：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v3/diagnostic_review/thrust_consecutive_f045_f053.png`。
- 用户明确否决：依然没有解决长枪非刺出、只是指向玩家。去掉过头举枪不等于刺击成立；约1–1.9s右向横枪、f048–f050旋枪转到前景的路径是失败样本，不能进入生产。
- 当前无生产选帧、无去背、无 Unity 部署、无自动再抽卡。v2、v5、Charging1 和本次 v3 原视频均保持不变。

### Enemy 109 Striking video v4 六秒方案（2026-10-09，仅准备，未收费）

- 用户要求重新思考生成词并建议实际视频改为6秒；新请求规格为 seedance-2-fast / **duration_seconds=6** / 720p / 1:1 / 无音频无水印 / seed=-1，不拉长旧4秒视频。
- 正向动作约束改为：转身阶段完成枪轴对准；转身后沿同轴拉回至右肋弯肘位并短暂可读；随后一次快速沿轴推出到已验收v5。转身时允许调整方向；刺击前必须拉回、命中后禁止拉回，不再全局禁止合法预备动作。
- 英文：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v4.en.txt`；中文：`Library/Locus/tmp/cavalry_concept/prompt_cavalry_striking_video_v4.zh.txt`；方案：`Library/Locus/tmp/cavalry_concept/brief_cavalry_striking_video_v4.md`；规格：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4.request-spec.json`。
- Charging1/v5原图保持；用户随后授权统一绿幕的生成前副本，当前用下面记录的新副本而非旧绿幕输入。未制作中间关键帧，首尾帧API不混第三图；中间预备只文字指导，如果失败考虑验收预备帧后拆段，需另批。
- 本次未上传、未创建任务、无新费用或Unity修改。拟定输出 `C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v4_720p_6s.mp4`，当前不存在；收费生成仍待用户批准。

### Enemy 109 首尾绿幕与主体色偏检查（2026-10-09，未修改原素材）

- 用户指出首帧与视频绿幕不同、角色变色。实测原Charging f036背景中位RGB(0,159,62)，上传首帧(0,177,64)纯色，v5尾帧(2,182,77)非纯色；v3 f001(0,175,63)→0.25s(0,161,69)→末帧(0,175,74)。v2也变；原YUV背景平面随时间变化，不只是播放器显示问题。
- 配准材料样本显示主体并非完全RGB恒定：v3 0.25s头盔(58,62,82)→(62,62,82)，红布(186,28,26)→(180,30,31)。马胸形变匹配可信度较低。不能从局部变化证明全身统一调色，不能断言绿幕不一致是单一原因。
- 已有首帧与正式Charging1深层161025像素平均每通道差<1，说明首帧绿幕合成本身没有把主体整体改色。后续模型重绘/光照和编码因素仍需区分。
- 诊断：`Library/Locus/tmp/cavalry_concept/cavalry_chroma_color_diagnostic_v1/findings.md`，同目录有材质对照PNG、v2/v3共194帧背景CSV、详细JSON及只读源分析脚本。
- 建议在6秒v4收费前另存背景一致首尾副本、保留主体RGB和原尺度；不要通过整图调色匹配背景，主体颜色/绿幕稳定分别验收。候选统一绿色及副本处理需用户确认；本轮未统一背景、未去背、未发生成请求或Unity修改。

### Enemy 109 v4统一绿幕生成前准备（2026-10-09，已完成，未创建视频）

- 用户同意先完成v4生成前工作；背景改为同一 `(0,159,62)`，只制作独立RGB副本，不重画角色、不改Unity。
- First：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/prep_green_015962_v1/first_frame_charging1_green_0_159_62.png`，SHA-256 `36374bd36ddf11ff911429134e64d2004195d886b1bc5bf2a8d771934238ac85`。
- Last：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/prep_green_015962_v1/last_frame_striking_v5_green_0_159_62.png`，SHA-256 `eb41bfcfbfb157b9a07ea26a570866826084f2660cc78cadd2916297d9b229c1`。
- 两图960×960 RGB，未缩放/平移。First从既有RGBA合成，高Alpha主体194074像素RGB逐像素保留；Last保护303246主体像素及内部高光，只有明确背景和窄混合边缘改变。Last的边缘估计仅重铺色键背景，不是生产Alpha。
- 机器检查、边缘回读后用户已明确验收新副本并批准生成；首尾/前后对照、报告和manifest均在`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/prep_green_015962_v1/`。
- v4六秒中英文Prompt已改为(0,159,62)无色偏/溢色条款；下文记录实际创建和成片。上述“未创建”只代表准备阶段，统一输入不保证输出保色。

### Enemy 109 Striking video v4 六秒（2026-10-09，用户否决：转身后摆枪而非同步刺击）

- 用户验收common-green首尾并批准生成；只创建一次task `task_d666303dd1bb4a3488df82ac341b69ff`，最终success。seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1，首尾均用统一(0,159,62)新副本，不重用旧绿幕输入。
- 成片：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v4_720p_6s.mp4`，3,094,462 bytes；SHA-256 `cb69bd422af381307034cbc22ef678af65c73d8a54362beba03f3dc9f9fcae7b`。MP4结构有效，FFmpeg全量解码0：960×960、24fps、145帧、约6.04s、无音频。
- API usage completion_tokens/total_tokens130500，cost原值spend=0.7308，不推断币种。只创建一次；有限查询后同task四Range下载并验证。
- 诊断：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/diagnostic_review/motion_contact_sheet.png`与`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4/diagnostic_review/weapon_transition_consecutive_f103_f114.png`。只是验收预览。
- 用户明确否决：转身与刺出应该同时进行，而本版仍然先转身再将枪尖摆正。旧“转身完成→对准→收枪→停顿→刺”结构是错误目标，已废止，不再作为生成/验收规则。
- 输出绿幕G中位数140–154（目标159），f001(0,154,60)→0.25s(1,144,60)→尾帧(0,150,60)，仍变暗。主体局部样本有小变化，形变/生成重绘因素无法完全排除；统一背景不是保色硬保证。
- Runtime：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v4.runtime.json`，`user_rejected_sequential_pointing_not_simultaneous_thrust`。保留原视频，无生产选帧、去背、Unity改动或自动再生成。

### Enemy 109 同步旋身刺击 video v5 六秒（2026-10-09，用户已验收）

- 用户明确转身与刺出同时进行，随后要求“请将其加入关键词描述并发起生成”。中英文加入侧转前坐深/夹膝压镫/腰胸压缩/右肩后压/弯肘收手到右肋与整枪回收，释放腰胸肩时马体转向、右手前送/肘伸展同步；旧转完后预备结构失效。
- 仅创建一次task `task_4207390cad1d4fbc83e0a40607a8ea71`，最终success。seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1，first_frame+last_frame复用已验收统一(0,159,62)副本，原图哈希不变。
- 实际文件：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v5_720p_6s.mp4`，14,777,507 bytes；SHA-256 `7f65098ce2f0d51c33fe15dc3c188ab17eb4bd69803b16729b220f3c335b1602`。四Range检查ETag和完整长度后拼接，`ftyp/free/mdat/moov`有效；FFmpeg全量解码0，960×960/24fps/145帧/约6.04s/无音频。
- API usage completion_tokens/total_tokens=130500；cost原值spend=0.7308，不推断币种。2次状态观察和1次仅取URL的GET，无重复创建。下载查询→拼接完成约12.122s，不当作模型推理耗时。
- Prompt/brief/request-spec在`Library/Locus/tmp/cavalry_concept/`，英文`prompt_cavalry_striking_video_v5.en.txt` SHA-256 `6e4d96e7e6bad72fe0483f9004c898ad9b410d3ebe82f63da93fb55adc05316d`，中文`.zh.txt` SHA-256 `78393eb6610b4d78fc571d54e4d79d38c064062bb28cb27f63b0820ced3a55fa`。节奏建议0–0.3s起始、0.3–1.1s蓄力、1.1–2.4s同步释放、余段命中，不改玩法。
- 历史预审有疑虑：约1.5–2.75s压胸/低头与抬枪靠肩、约3–3.75s侧转伸臂重叠但仍左摆、约3.25s枪尖出框。随后用户明确“很好，已验收，请根据动作语言和敌人动画需要拆分动画关键帧”；视频已通过，原代理疑虑不覆盖验收，不重做视频。本轮抽帧避开f076–f081出框段。
- 全145帧四角背景中位G144–153，相比输入159仍暗且时变，f001(0,153,59)、f007(1,147,59)。本轮只统计新背景，没有全材质配准，不证明全身RGB变化或其原因。
- 诊断目录`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/diagnostic_review/`：`motion_contact_sheet.png`、`transition_contact_sheet.png`、`decode_and_color_validation.json`、`background_timeline.csv`、`findings.md`。诊断报告先把终端进度145误读fps，已按输入流24fps更正；145是帧数。
- Runtime `Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5.runtime.json`最新为`user_accepted_video_24_raw_keyframes_exported_frame_review_pending`，user_art_verdict=accepted；创建脚本不再执行。下述已完成本地关键帧选择，仍无去背、Unity接线/场景改动或自动新生成。

### Enemy 109 Striking v5 初版12帧（2026-10-09，已被24帧版取代）

- 授权只涉及已验收视频的本地拆帧；无收费API或新增视频任务。145帧完整原尺寸RGB解码，挑选12张f001/f034/f040/f046/f070/f074/f083/f086/f092/f103/f115/f145；源PNG与导出逐字节、哈希一致，未缩放/移位/调色/去背。
- 交付：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/keyframe_selection_v1/selected_frames_v1/`。`striking/`为独立原始PNG，`striking_selected_raw.png`总览、`striking_slow_review.webp`慢放、`striking_timing_0p4s_preview.webp`时序预览，manifest和README同目录。
- 动作按入口/急停蓄力4帧→同步旋身刺出4帧→命中1帧→惯性收稳3帧；长停顿和f076–f081枪尖出框排除。第9张f092为命中主帧，建议对齐现有0.18s，全部建议0.4s，未接线或改玩法。
- 12张均960×960 RGB，轮廓无边缘4px接触；最窄左27px/右46px。源视频哈希保持`7f65098ce2f0d51c33fe15dc3c188ab17eb4bd69803b16729b220f3c335b1602`。两个WebP各12帧，末端审阅停留不属于游戏时序。
- 首帧后续可复用正式Charging1，其余11张待去背授权；1108补边仍未执行。本轮帧序待用户查看，不把视频验收自动等同选帧验收，不反播命中尾段充作收枪/返航。
- 用户Editor已处playing E0预览；只读确认现有Sprite1108/PPU16/Point/有效Center pivot，未停Play、保存场景、导入或改Animator/Prefab/C#。

### Enemy 109 Striking v5 24帧版（2026-10-09，历史候选；核心段越界）

- 用户认为12帧过少，要求拆出24帧；本地复用已验证解码源，保留原12个动作节点并补12个真实姿势。序列f001/f031/f034/f037/f040/f043/f046/f065/f068/f070/f072/f074/f075/f082/f083/f084/f086/f089/f092/f097/f103/f109/f115/f145，f092为第19张命中。
- 目录`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v5/keyframe_selection_v1/selected_frames_v2_24/`；独立PNG在`striking/`，总览`striking_24_selected_raw.png`、慢放`striking_24_slow_review.webp`、连续预览`striking_24_sequence_preview.webp`，manifest/README/CSV同目录。
- 24张不同源PNG，960×960 RGB，源/导出字节和哈希一致；两个WebP均24帧可解码，原12帧及视频哈希保持。f076–f081出框段排除，无插值/缩放/调色/去背。
- 24帧待用户查看；旧12帧时序不自动套用，预览速度不改玩法。首帧可复用Charging1，其余23张去背、补边和Unity接线未执行；Editor最新editing E0预览，未更改状态/场景，无API。

### Enemy 109 Striking v6 边界控制版与6+6+6关键帧（2026-10-09，技术完成；用户验收待返回）

- 用户判定v5核心f075–f082枪尖越界，不满足本次6+6+6，明确建议重生成控制边界；确认后仅创建一次v6 task `task_1bcb08e0d51d4df8bd09c9388f4b7d4b`。
- v6文件：`C:/Users/Administrator/Videos/doubaoVideo/cavalry_lancer_striking_v6_720p_6s.mp4`，3,252,977 bytes；SHA-256 `119df1a087a34856c14b104e4a50607fd60cc6967a6ee1c0fb0981dbfadf415b`。MP4/FFmpeg通过，960×960/24fps/145帧/约6.04s/无音频；usage130500，cost原值spend=0.7308。
- 全片逐帧边界通过：最低L/T/R/B=48/98/70/60px，要求32px；4px边缘前景像素全0，bad frames=0。报告和接触表在`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/diagnostic_review/`。
- 18帧严格按用户分段：pre f055/f059/f063/f067/f071/f074；core damage f075/f076/f078/f080/f081/f082；post-083 f083/f086/f089/f092/f097/f103。PNG与源逐字节/哈希一致，核心段未跳过f076–f081。
- 交付目录：`Library/Locus/tmp/cavalry_concept/cavalry_striking_video_v6/keyframes_6plus6plus6_v2/`，含三个分组子目录、`keyframes_18_all.png`、`keyframes_6_core_damage_thrust.png`、`keyframes_18_review.webp`、CSV、manifest和README。无去背/Unity导入/Clip接线或玩法时序改动，帧序等待用户验收。

### Enemy 109 背向返航形象与4秒奔跑循环 v1（2026-10-10，形象与视频均已验收；暂不抠图）

- 背向起始形象：`C:/Users/Administrator/Pictures/gptGen/enemy109_retreat_back_startframe_v1.png`，960×960 RGB，1,004,086字节；SHA-256 `645b6bf888fa4a9ba7258884a9525426132cfc233137b3ef1a1d1f4c73340b31`。GPT Image单次编辑成功，用户明确“已验收，准备这个奔跑动画制作，4秒loop的即可”。右手枪在画面右侧，正背面人马合一；未抠图或部署。
- 为视频四边各补32px背景，原960窗口逐像素保留、零重采样/重绘，得到1024×1024同一首尾副本：`Library/Locus/tmp/cavalry_concept/retreat_run_loop_v1/first_last_retreat_back_padded_1024.png`；SHA-256 `3b825459db173e8c99c6ac93a2637f55865086835dc1ef7daffc4933a1a74e13`。
- 用户随后“发起生成”，仅创建1次 task `task_767c173fa0ea4ff2a5869894ce316d9b`；seedance-2-fast / 4s / 720p / 1:1 / 无音频水印 / seed=-1；只上传同一图一次用于first/last，不混参考图/视频。英文Prompt SHA `f862113827320ae2a73fe88ca09da4415f52f38e1fc0d63f21e35bc3a5b9dd4a`。
- 原视频：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_run_loop_v1_720p_4s.mp4`，2,388,438字节；SHA-256 `9adf1f6be61a6c02bcbcf675f7d9e88981f2b1b2cb3d72aee768b0e0d5e343cd`。服务success，MP4/全量解码通过；960×960、24fps、97帧、约4.04s、无音频；usage87300，cost原值spend=0.48888，不推断币种。
- 首轮有限12次查询仍generating，之后单次查询同task为success。4路Range完整下载10.408秒（含取URL/探测）；accepted→success观察约307.0秒，不是纯推理时长。
- 全97帧未检测到触边裁切，最低L/T/R/B余量301/16/244/64px；枪尖部分帧未达到拟定3%安全带。前轮预审观察尾部约3.79–4.00s趋向站定；用户随后明确“已验收，先不抠图，准备头尾帧思考该范围如何完成转身回到idle状态”。**视频已获用户验收，旧接缝疑虑保留为历史，不覆盖验收。** 任意循环相位退出/Unity衔接尚未验证。
- 审阅图、97帧WebP及全帧报告：`Library/Locus/tmp/cavalry_concept/retreat_run_loop_v1/diagnostic_review/`；运行记录：`Library/Locus/tmp/cavalry_concept/retreat_run_loop_v1.runtime.json`。原视频保持，无剪帧/倒放/补生；生产选帧、去背、Unity部署未执行，当前明确暂停抠图。
- 转回Idle首尾准备：`Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/`。第一帧为真实视频第97帧（零基96，PTS49152/12288=4.0s），FFmpeg重解码RGB回读一致；最后姿态目标用现有MountedIdle1。已另存整体0.9375比例/单次偏移的RGB Idle参考候选（无抠图，原源保留），两端蹄底同为y892、蹄中点差约0.5px。用户确认采用背面→画面左侧侧身→正面，以延续原正面→画面右侧攻击→背面方向；对齐后的Idle终点构图/比例也已确认。随后6秒Prompt获“发起生产”授权，实际结果见下一项；不再沿用旧“尚未创建、待批准”状态。

### Enemy 109 返航到位→左侧转身回 MountedIdle v1（2026-10-11，视频与24帧已验收；色差问题已诊断未修复）

- 用户复核6秒双语Prompt、方向和对齐Idle终点后明确“发起生产”，仅创建一次 `task_75f597274c3a4dc28fb978b719692522`。规格 `seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1`，只上传已批准的 first_frame 和 last_frame，不混普通参考图/视频/音频。
- 输入：实际已验收奔跑视频尾帧 `first_frame_actual_retreat_video_tail_960.png` SHA-256 `a03b02000c5295bb760fdc09f016ba7392d4323b016ffe57698e5f405cf38570`；对齐Idle `last_frame_mountedIdle1_aligned_candidate_960.png` SHA-256 `2273e61abc6dfad13ff5bb51a9b0e487fab4d894356947c30337c5d217865e73`。均在 `Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/`，960×960 RGB，创建前哈希核对一致。
- 英文Prompt SHA-256 `a2e133eb309093876c7081d02d9456f3b5a6192ee056f74d1975f090d49ba0ee`，中文 `277f8b18d954bd8c64672b41f38559bc206ec0d61938a5b8d41a6b6dff071044`；原批准文件与输入保持，未改背景。
- 原片：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_retreat_to_idle_v1_720p_6s.mp4`，3,801,380 bytes；SHA-256 `c01c129c581e0b95df5fe7d427d15e7d18d47e8b2f6fd7b9077ed92127074b7a`。实际H.264 / 960×960 / 24fps / 145帧 / 约6.04s，末帧名义时间6.000s；MP4结构与FFmpeg全量解码0，流信息确认无音轨，审阅后原片哈希不变。
- API usage completion_tokens / total_tokens = 130500，cost原值spend=0.7308，不推断币种。首轮12次有限查询仍generating，后续只查询原task一次为success；四路Range验证后下载、容器与解码通过才改为正式MP4，下载阶段约6.283s。没有第二次创建。
- 历史代理接触表预审：背面→画面左侧侧身→正面方向符合目标，人马共同转向、马腿抬落换位、枪由画面右侧转至左侧；采样未看到第二圈或攻击。曾记约2.5–3.5s侧身停留较久并反复迈步；用户随后已验收原片，旧疑虑不覆盖用户验收。接触表仍不证明全帧解剖或无滑步。
- 全145帧绿色主导诊断未检测到4px边缘前景触碰；最低L/T/R/B余量28/28/40/65px。**18帧低于拟定32px安全带**（f071–075、107–109、119–122、130–135），因此只可记“未检测到裁切”，不可记“全部构图约束通过”；该掩码不是Alpha抠图。
- 首帧输入/实际包围框只差左边1px，尾帧包围框均 `[240,33,658,893]`（右下排他），蹄底可见行892；前景联合RGB MAE分别5.7304 / 7.6063，不宣称像素一致或最终Unity尺度/锚点衔接已通过。
- 审阅目录 `Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/diagnostic_review/`：全程、侧身连续16帧、最后16帧、首尾对照、145帧WebP、`validation_and_endpoint_report.json`与`findings.md`；WebP重复仅为诊断，原动作是非循环转换。规格/brief及 `Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1.runtime.json` 已同步结果。
- 用户明确“已验收，请截取有效帧供我验收”，随后确认“当前这批挑选帧已验收”：**原视频与24帧帧序均通过**。保持暂不抠图、未Unity导入/Clip/路由接入、未玩法时序或场景修改、未Git提交。6秒是制作源时长，不是游戏到位等待时间；任意奔跑循环相位退出与后续映射仍未验证。

#### 转回Idle有效帧 v1：24张本地原帧（帧序已验收，色彩一致性单独待处理）

- 目录 `Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/keyframes_v1_24/`，按动作节点4+8+8+4分组：起始f001/019/024/029；背面到左侧f034/039/044/049/054/059/064/069；左侧到正面f079/086/092/098/103/110/114/118；收稳f124/128/138/145。删除相似站定/侧身踏步长停留，保留真实首尾锚点，未均匀抽全部6秒。
- 24张完整960×960 RGB PNG，与原视频对应解码像素全相等，和保存的解码源PNG逐字节/哈希一致；RGB哈希24个均不同。未裁切/缩放/平移/调色/插值/镜像/去背。独立回读24/24通过，CSV顺序与manifest一致，原视频哈希不变。
- 避开原片18张低于32px余量的帧；候选最低L/T/R/B=35/33/42/66px、4px边缘诊断前景全0，24/24达到32px。此量测不是Alpha或解剖验收。
- `keyframes_24_all.png`及四组总览、独立分组PNG、`selected_frames_manifest.json`、`selected_frames.csv`和README已导出；`keyframes_24_sequence_preview.webp` / `keyframes_24_slow_review.webp`均480×480、24帧，顺序/像素回读通过。2.68s快速与6.24s慢放只是审阅节奏，不是游戏时序或Loop。`_decoded_source/`为追溯副本，不作为第二套动画帧。
- 本轮只有本地解码选帧，零新收费API，无Unity/玩法/场景操作。用户随后明确“当前这批挑选帧已验收”，视频与帧序均accepted；颜色问题单独记录，不自动抠图、改色或部署。
- 用户指出收尾色差，要求先检查流程。本次只诊断：两张上传输入角落64px中位首帧(0,152,63)、尾帧(0,168,60)，输入本来就不同；本次规格明确no_background_normalization，遗漏了此前刺击v4已验证的“另存只改背景统一副本”前置项。尾帧按Nearest900px+(12,20)+边缘延展重建与输入逐像素一致，非对齐变换意外调色。
- 输出f001(0,146,60)、已选第23张f138(0,153,59)、第24张f145(0,160,57)；全145帧G中位145..160。f140/f141 G153→f142 G157→f145 G160的末段变化已在原视频。24张帧是原片RGB复制，没有用静态尾图替换f145；哈希24/24不变。
- 原始YUV角落f001(96,112,69)→f145(102,107,65)；color range/space/primaries/trc均unknown。默认与强制601一致，709仍时变且更暗，因此不能用换播放器/固定解码矩阵解释全部问题。生成模型与服务端编码贡献尚不能完全拆开，不能认定输入背景差是主体变色唯一因果。
- f138→f145局部材质配准NCC约0.980..0.982，配对RGB中位差头盔(0,+1,-2)、红布(+2,-1,-3)、马胸(+2,+2,-4)，主体也不是完全不变；只统一或去掉背景不保证修复主体色差。现行SVL key_frame逐图auto_key_color，不是全批固定首帧绿色。
- 详细诊断：`Library/Locus/tmp/cavalry_concept/retreat_to_idle_v1/diagnostic_review/background_color_diagnosis.md`。本轮未修改PNG/MP4/Skills/Unity，零新生成API；保持已验收帧，不因色差自动重做。

#### 转回Idle24帧：SVL透明版 v1（2026-10-11，抠图完成；透明结果待用户验收）

- 用户“请继续未完成的抠图工作”恢复本地去背授权。仅处理已验收24张TurnToIdle原帧；4秒返航奔跑目前没有已确认选帧清单，本次未自行抽选或部署。
- 先对7张代表帧扫描T55/70/79，采用现行SVL Chroma / 逐帧auto_key / T79 / softness16 / halo1 / 半透明转不透明；无Real-ESRGAN、全图校色、旧环替换或新收费API。同一源码+配方重新处理Idle1，与已存在keyed_ui_t79逐像素一致。
- 产物 `Enemy109ArtSource/03_SelectedFrames/TurnToIdle/keyed_ui_t79/` 共24张，`turnToIdle1_f001.png`至`turnToIdle24_f145.png`；raw副本及清单同动作目录，root source manifest仅追加TurnToIdle，不擅改旧T70/T79备注。
- 独立回读24/24为960×960 RGBA、Alpha仅0/255、半透明0、透明隐藏RGB0；24份raw与来源字节/哈希相同，原MP4哈希不变。核心掩码3257182像素次0删除/0改色；边缘反混合每帧1075–2295可见像素RGB发生变化，不能宣称所有可见像素原样。非透明边界最小L/T/R/B=37/36/43/67px。
- 已读白/深底总览、枪杆/缰绳/马尾/马蹄局部源图对照与末两帧接现有Idle1的中性底对照；未见明显新增绿边、主体大片缺口或细部断裂。两版24帧WebP（中性底2.68s、白底慢放6.24s）与CSV顺序通过；速度仅审阅，不改玩法。
- `TurnToIdle/matting_report.json`、`raw_manifest.json`、`frame_list.csv`、README与`review/`完整落盘；Runtime/spec记录抠图true、matte_user_verdict=pending、Unity=false。**只去掉制作背景，既有生成器主体轻微色差仍保留，未做主体校色或静态尾图替换。** 未补1108/导入Unity/建Clip/改场景或提交。
- 用户质疑此前只凭数值结论不充分，要求实际抠图对照。新增只读候选复核 `Enemy109ArtSource/03_SelectedFrames/TurnToIdle/endpoint_review_v2/`：使用同一T79实际抠出f002/f140/f141/f144；总览、局部材质和切换预览均经RGBA合成并逐帧像素回读。v1两份早期切换WebP错误使用不透明绿底源图，已在v2 report标作superseded，不能当作抠图证据。
- 实图确认±1端点不修复色差：f001/f002背景中位G146/145，f144/f145 G159/160；f140/f141的G153且与f138收尾姿势/主体较接近，但与Idle1的主体色差更大。候选提供视觉取舍，不自动替换/重排已验收序列；四候选均待用户决定。当前序列、24张透明版、Idle、MP4哈希全未改，无收费API或Unity操作。

### Enemy109 刺击收尾→转身接背向返航 v1（2026-10-11，视频已验收；f010共用端点11帧已抠图部署）

- 用户确认头尾副本和双语词后明确“已确认，发起生成，时长为6秒”；只将原4秒提案改成6秒及对应时间段，精确头尾哈希未变。唯一task `task_c67ba690deac46f2bb2cb17c31f0471f`，上传2次，创建POST仅1次，原task success，禁止重复创建。
- `seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1`，仅first_frame+last_frame。头是Attack18/f103的0.9375倍/整幅(12,59)对齐副本；尾是真实返航视频f001/T79去背副本，两张960×960RGB背景均(0,159,62)，上传sha分别`890a1ca3bb5c54d14027e716031477cd527801dd2ab947718748eb1d54842878`、`244c3ee6fb89e2d239ef93faaa363fc10e700347cc5cdc3615cee5627468ce1c`。
- 原片 `C:/Users/Administrator/Videos/doubaoVideo/enemy109_strike_to_retreat_v1_720p_6s.mp4`，3,788,350字节，sha`a207d2ce1310bb22e36045348bd9c6039f59bc0f784b88b28a38fa28f2cb3d96`。实际H.264、960×960、24fps、145帧、约6.04秒、无音轨，容器与FFmpeg全量解码通过。最后下载分段曾超时，保留前三段后续传第4段成功，身份/长度通过，没有重生成。usage completion_tokens/total_tokens=130500；cost原值spend=0.7308，币种未返回。
- 生成后的历史预审曾检测到右侧身→斜背→正背方向；原尺寸f054水平枪尖越过右边，36帧触及4px外围、57帧低于32px安全带，约4.4秒后趋于背向站定。上述是历史代理预审，不覆盖用户随后对视频的明确验收；也不意味着原视频的边界问题已修复，若选帧用于抠图/部署仍应避开/检查越框动作。
- 上传平底已统一但输出空背景中位范围R0–3/G143–155/B59–63，首(0,155,59)、尾(0,150,61)。本轮没有主体材质配准，不把背景时变等同主体统一染色原因。
- 原始视频诊断与预览仍在`Enemy109ArtSource/02_VideoPreparation/StrikeToRetreat_v1/generated_review_6s/`；其diagnostic_frames不是生产选帧。视频用户验收后，依照“已验收，请挑选12帧有效帧”，新增生产选帧目录`Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_v1/`：保留真实f001/f145首尾，12帧源号f001/f010/f027/f035/f039/f043/f072/f075/f079/f087/f104/f145。f013–025与f046–068枪尖裁切段排除。重解码独立核对12/12 RGB像素、SHA、CSV和两份预览顺序通过；最小留白L/T/R/B=44/33/38/64px，选帧边缘4px前景全0。原片哈希未变，选帧状态`selection_user_verdict=pending`；不是透明帧，不表示该序列已用户验收。本轮未抠图、未Unity导入、未改玩法或场景。完整清单见`selected_frames_manifest.json`，交接入口`plan/enemy109-turn-to-retreat-video-preparation.md`。
- 后续用户指定第02张f010作为新的StrikeToRetreat头帧与Strike尾帧，并明确授权“抠图并替换部署”。另建`Enemy109ArtSource/03_SelectedFrames/StrikeToRetreat_f010_v2/`，从f010保留11张T79 RGBA；原12张RGB与MP4哈希保持。核心1,359,179像素次0删除/改RGB，14,321边缘反混合像素次留档，Alpha0/255、幕布/隐藏RGB0。
- 新帧Nearest16/15到1024、置1108(61,57)恢复Strike映射。Attack18 PNG覆盖且meta/GUID保留，与新StrikeToRetreat1字节/像素相同；新Clip/Controller状态/Prefab直接引用接通，仅成功刺击后按现有返航格数时长播放一次，阻塞/HitStop续播，打断不共用。针对性+隔离CavalryCycle验证通过；自然路线/游戏速度美术未覆盖，透明美术pending。测试停用根对象触发1条伤害数字inactive协程日志，已归因留档，未改跳字系统。权威接线和验证见`plan/enemy109-matting-unity-handoff-20261010.md` §12；没有新收费任务。

### Enemy109 冲锋被打断 K1/K2 与 A段视频 v1（2026-10-14；帧序/f029已验收，整套12张透明帧完成）

- 用户纠正后续制作目标为打断动画，已有背向返航视频不重生；完整中英文词、参考职责与规格审阅后明确“发起生成”，单图编辑POST只1次，无自动重试。
- 模型`gpt-image-2.5-sunburst`、quality=max、input_fidelity=high、1024×1024PNG、n=1、不请求Alpha，绿幕目标(0,159,62)。唯一输入为现有Charging1/f036的32px补边同底副本，源192075可见RGB保持；批准输入和两版Prompt哈希未变。
- 原图`C:/Users/Administrator/Pictures/gptGen/enemy109_charge_interrupted_keypose_v1.png`，HTTP200、POST66.062s、1024×1024RGB/ColorType2、1,071,876bytes、sha`eed75c1aefabcd3d26699f1bcafc5398b9945d0ffc2d9a7d0a0e318fdbb8b46a`，工程字节副本、请求/脱敏响应、完整词及生产记录在`Enemy109ArtSource/01_KeyPosePreparation/ChargeInterrupted_v1/`。Usage input2227/output7024/total9251、cost未返回，host=r2.52image.xyz。
- 原图和同画布对照已回读附图：骑手回仰收缰仍坐鞍、馬前膝屈/后蹄支撑、单枪完整；用户美术pending。诊断边距165/148/297/173px；输入底929→输出851约上移78px，后续锚点需核对，不能凭姿态bbox变化断言统一缩放或直接部署。背景角落64块中位(2,162,63)、std约0.6–0.9，不是精确请求底色，主体未配准比较。
- 无重生成/改色/移位/抠图/视频/Unity/Play。验收后才另准备视频方案。恢复入口`plan/enemy109-charge-interrupted-keypose-preparation.md`，原片和返航/成功转身部署不改。
- 后续用户将首图K1定位为“命中关键帧”，并要求K2呈现马急停、骑手措不及防，再衔接转身返航；K1职责得到确认，锚点/配色/部署仍是独立验收项。K2位于`Enemy109ArtSource/01_KeyPosePreparation/ChargeInterruptedTail_v1/`：K1原RGB唯一输入的字节副本、K2前蹄落地/骑手惯性前扑坐鞍Prompt及对照留档。K2 POST仅1次、HTTP200、1024×1024 RGB、SHA`a8af5e01cfae6a606388a527d635a5fde2f69eaa6d08b4c2a8019586761da703`；回读观察为骑手前扑/低头/仍在鞍上、缰枪完整，四个脚部支撑轮廓可见。虽无地面线且技术检查未证明承重/锚点/颜色，用户之后明确“K2已验收，思考冲锋被打断的视频内容”，故K2关键姿态状态更新为accepted。
- 当前内容方案拆为A段K1→K2刹停（对应0.20s普通打断恢复或0.22s刺击提交前打断收招）与B段K2→背向返航f001转身桥，再复用已验收返航Loop；生成视频源时长不得等同游戏Clip时长。现有路由只让成功StrikeRecover播放StrikeToRetreat，Interrupted→Retreating暂切静态背向pose；有位移才需转身，Windup原地打断不应无条件旋转。详细动作、阶段和锚点/色键门槛见`plan/enemy109-charge-interrupted-keypose-preparation.md`“视频内容方案”章节。
- A段视频经完整双语词、参考职责、规格与输入副本审阅后，用户明确“确认并生成”。唯一task `task_2db36264422f43af9add2480770f4e6c`，首尾上传2次、create POST仅1次；`seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1`。服务端success；usage completion/total=87300，cost `spend=0.48888`（币种未返回）。curl查询原task成功，生成没有重试。
- 最终文件`C:/Users/Administrator/Videos/doubaoVideo/enemy109_charge_interrupted_v1_720p_4s.mp4`，2,114,011 bytes，SHA`d3df3049161ade578d178c658972087d55e5c24a06310b9e961f5998a40d22d9`；实际960×960、24fps、97帧、4.04s、无音轨。4段HTTP206、MP4 ftyp/moov/mdat边界和FFmpeg全量解码通过。正式记录及审阅输入在`Enemy109ArtSource/02_VideoPreparation/ChargeInterrupted_v1/`。
- 诊断接触表/末12连续帧/首尾对照均已保存：外4px无前景，最小边距L/T/R/B=120/86/242/154；输出背景每帧四角中位范围R0–2/G143–154/B56–60，跟输入键(0,159,62)有偏差和轻微漂移；主体颜色未配准验证。原尺寸抽帧显示大体K1→K2且尾段收稳，但开头约0.3–0.8秒追加马前躯/前蹄上抬，再落蹄，和原拟直接刹停动作有偏差。这是历史代理观察；用户后来明确“已验收，请挑选有效帧”，视频美术accepted，不自动重生成。
- 按用户验收后的选帧要求，新增`Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/`：12张960×960 RGB真实源帧f001/003/009/017/021/025/029/033/039/051/063/097，保留真实首尾，前段按动作节点加密，尾段去掉长停留的近似重复。独立完整重解码核对12/12像素、PNG哈希、CSV及静态/动态预览顺序通过，MP4指纹不变；最小留白L/T/R/B=120/90/243/158px，外4px诊断前景0。清单`selected_frames_manifest.json`、验证`selection_verification.json`和总览/WebP已保存。用户后续“已验收，请抠图”确认帧序accepted。RGB原件保持未裁切/缩放/移位/调色/插帧，后续透明版另存，不把原件验收等同整套Alpha/部署验收。
- f029 T79小样随后获用户“已验收，可以按照当前参数继续抠图”确认。11帧按同参数生成、f029逐字节复用，共12张960×960 RGBA，目录`Enemy109ArtSource/03_SelectedFrames/ChargeInterrupted_v1/keyed_ui_t79/`，原RGB和MP4不变。配方SVL Chroma/逐帧auto-key/T79/softness16/halo1/alpha-aware despill/半透明转不透明；无额外描边替色、压绿、平滑或全图调色。
- `matting_report.json`及独立`matting_verification.json`回读12/12 ColorType6/Alpha0或255，半透明/透明隐藏RGB/源键色残留0，990743保护核心像素次Alpha/RGB改动0，10523可见边缘RGB像素次反混色变化另记。白底可见gEx>0每帧801–1317、gEx>8每帧216–569；不宣称零绿。f029原PNG哈希`7349b5b99fb26827fbee556e9327089aa3d64679ec3c5d34d22bb8232c931573`保持；三底总览、细节对照和12帧中性底WebP顺序/像素核对通过。
- 视频/帧序/f029小样及ChargeInterrupted01–11透明帧accepted；ChargeInterrupted12_f097透明帧因用户指出色差不采纳，文件保留追溯但不作为返航端点。未Unity导入、1108补边、Clip/Prefab修改或Play自检，未新收费API。
- 用户要求“思考从63帧关键帧开始返航的动画内容”。采用f063透明帧为失衡尾态起点，接真实返航f001背向支撑帧；方案分为恢复重心→收枪牵缰→人马短步转向→背向接入。f063透明底行788，返航f001底行891，相差约103px，后续首尾准备需单独整体源空间对齐；不能使用f097色差帧或自动缩放/漂移补差。详细分析见`plan/enemy109-charge-interrupted-keypose-preparation.md`“从f063开始的返航动画内容”。
- f063→返航f001转身桥已按确认规格创建一次并完成下载解码：task `task_9af5590e1ede438fa2ca54e6210aa74c`，2,833,549 bytes，SHA `b1c5094dc2eb5503ee9ccd00c02a231eb9c6978e775c9094861b7830a05f8daa`，960×960/24fps/97帧/4.04s/无音轨，create POST 1次，usage 87300、spend 0.48888（币种未返回）。成片待用户美术验收；代理预审发现中段未保持单向转身（右→左→后侧→正背）并含原地小跑感，不自动重生成。
- **本批触发工作流修正（2026-10-14）**：审计确认“输入统一绿幕”不是输出片基的决定因素——输入四角精确`(0,159,62)`、std=0、可见像素零改色时，成片片基仍系统性偏低约5–20G，且多批最差帧均落在开头0.2–0.35秒、最低G均为143。原流程只在输入端设门槛，输出端仅要求“记录”，再加上“不得自动改色/重生”与“背景漂移不是作废条件”的写法，使同一缺陷每批重复并仅被标为已知问题；2026-10-11诊断中“若用户批准再列为独立检查项”的待决项也一直未被重新提出。经用户批准，`skill/reachapi-seedance-video-generation-v2.md`的§色彩一致性改为 G1输入片基/G2输出片基/G3交付物 三级门槛，新增“偏差必须升级为待用户决策项”与“参考帧来自上一代产物时先与身份基准比主体核心RGB”，并删除旧结案写法；`skill/workflows/character-hit-animation-video-workflow.md`验收清单与纪律行同步。

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

### Enemy 1021 持盾 Idle 定妆图 v3（用户已验收，尚未动画/部署）

**验收依据：用户在本轮明确确认“idle形象已验收”。** 已验收文件为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`，作为 1021 后续动作的身份、装备、盾形与举盾姿态锚点；本次确认不代表 Idle 视频、循环、透明帧、锚点对齐或 Unity 部署通过。

- **实际文件**：1024×1024、RGB PNG、Color Type 2（无 Alpha）、983,338 bytes；SHA-256 `446d6d558baa7d4a5d6e6fd9bf712c07b99c63a1c4f2e711cc0b28a7564427d0`。保留浅灰底原始生成结果，尚未去背、缩放对齐或导入 `Assets/`。
- **参数与请求**：`POST https://api.muskapis.com/v1/images/edits`、multipart 两个 `image[]`；`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、`size=1024x1024`、`output_format=png`、`n=1`，未请求透明。只提交一次获批英文 Prompt，无自动重试；HTTP 200。请求记录时间为 `2026-10-09T17:17:59.413235+00:00`（UTC）；响应未返回 task id。
- **完整原词与输入记录**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.request.json`；Prompt UTF-8 SHA-256 `246e7766ee2b916fdc2070a91d62b9b9a691cdfc43c7f3e8e433ae5b67eec4be`。原始响应为同目录 `enemy1021_shield_hold_v3.response.json`，图片 URL host 为 `r2.52image.xyz`。
- **人物参考**：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`，合成到 `#E6E6E6`、保留 1108×1108 原画布，未去剑或重采样；上传副本为 `Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/01_1011_idle1_lightgray.png`，SHA-256 `67a32329aa33cc53a8b9da38bad1689b344f598b31402afa9fffd3b0e8f36ab1`。
- **盾牌参考**：`C:/Users/steam/Downloads/微信图片_20260928101134_12_223.png`，取 `(685,575,1024,1120)` 局部，339×545；上传副本为 `Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/02_shield_material_detail.png`，SHA-256 `126d6f6917c669ddf27694b94936dca4c1a011aedfb0de24ff5e4ddfab9b323d`。只借用木材与铁件构造，裁剪排除人物和握持手；原纹章留在参考像素中，由 Prompt 明确禁止继承。临时副本可能被清理，来源与处理方式已记入请求 JSON。
- **用量与费用**：input 2,487（image 1,865 / text 622）；output 7,024（image 7,024 / text 0）；total 9,511。接口未返回 cost，不推断金额或币种。
- **已验收形象**：人物以 1011 为基底改剑为盾，沿用其体型、头盔、甲胄、红缨/围巾及冷铁灰、朱红、暖金关系；盾在画面左侧、胸腹前方，持盾手/手指/前臂/握把藏在盾后，画面右侧左手空垂。具体盾高、底部收窄与材质按已验收成图保留，不因早期提示词目标与成图差异自动修图或再生成。
- **早期版本与当前阶段**：`enemy1021_idle_keypose_v1/v2.png` 与 `enemy1021_shield_hold_v1/v2.png` 属于此前探索，未成为身份锚点；1011+109 参考方案已被本次 1011 主参考 + 用户盾牌局部参考取代。后续 Idle 视频及各项制作/部署需独立授权和验收；本轮仅更新记录，未创建新生成任务或修改代码/Unity 资产。生产交接见 `plan/enemy1021-art-production-handoff.md`。

### Enemy 1021 持盾 Idle 视频 v1（已下载；四帧透明Idle已部署，待最终美术验收）

**授权与结果：用户批准绿幕参考、词和参数后只创建一次任务，随后下载成功。其他AI完成四帧抠图；本次已将四帧Idle部署到Unity并完成运行验证。技术通过不等于用户最终接受循环与画面，其他动作和独立掉盾仍未制作。**

- **Task ID**：`task_e169c149759b4216b75c879672389448`。创建 HTTP 200，初始 `queued`；accepted UTC `2026-10-10T11:03:50.632365+00:00`。
- **任务状态**：初轮 12 次查询均 `generating`，最后 UTC `2026-10-10T11:05:44.269101+00:00` 后停止自动轮询；用户随后要求下载。下载流程于 UTC `2026-10-10T11:27:15.862758+00:00` 观察到 `success`，未重新创建任务。
- **实际参考**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`，1024×1024 RGB PNG，344,540 bytes，SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`。只上传一次，同一 URL 分别用作 `first_frame` 和 `last_frame`；没有混入普通 `reference_image`。
- **绿幕准备**：本地连通背景替换为 `#00B140` / RGB(0,177,64)，未调用生图 API，未重采样、移动或覆盖已验收原图。替换 890,636 个背景像素，保留的 157,940 个像素与源图相同；红缨及部分外缘仍有原灰底混色细浅边，已向用户说明，用户同意继续。它是不透明参考副本，不是透明动画成品。处理报告为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.processing.json`。
- **规格**：`seedance-2-fast`、请求 4 秒 / 720p / 1:1、无音频、无水印、seed=-1。完整获批英文词已实际提交；Prompt UTF-8 SHA-256 `07e24f1bec3723e9b6e29e171c500e80cada4ee050e2c69fc9869bedefe91678`。目标是局部温和呼吸、双脚和骨盆稳定、盾保持已举盾且持盾手隐藏、副手空垂、无举盾/收盾切换。
- **持久请求留档**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.request.json`，5,976 bytes；已回读并核对 Prompt、task ID、参考图哈希与实际运行记录一致。不保存 Key 或临时签名 URL。运行记录为 `Library/Locus/tmp/enemy1021_idle_video_v1/runtime.json`；创建脚本为 `Library/Locus/tmp/create_enemy1021_idle_video_v1.py`，其已执行，不要再次运行创建脚本。
- **实际视频**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.mp4`，2,000,007 bytes；SHA-256 `808593f9cbc5b9132ba7992f5ad62159f77be0ea6e4eb5c2feaa04b0c07fce2f`。H.264，960×960、24fps、97帧、容器时长4.041667秒；首帧PTS=0、末帧PTS=4.000秒，音频流0。MP4顶层 `ftyp/uuid/free/mdat/moov` 和全部97帧解码通过，落盘文件回读长度与哈希匹配；同目录 `.part` 已原子改名，不再存在。
- **下载与校验留档**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.download.json`；运行清单 `Library/Locus/tmp/enemy1021_idle_video_v1/download_manifest.json`；脚本 `Library/Locus/tmp/download_enemy1021_idle_video_v1.py`。预览接触表 `Library/Locus/tmp/enemy1021_idle_video_v1/contact_sheet.png`，检查帧0/24/48/72/96，原尺寸首尾帧及5张检查帧保留在同任务临时目录。检查帧不是已选生产帧，不能据此声称无缝循环或锚点验收完成。
- **usage/cost**：completion_tokens=87,300，total_tokens=87,300，`cost.spend=0.48888`；币种未推断。这是现有生成任务的返回值，下载未创建新收费任务。
- **计时**：原上传约6.020秒，创建请求约3.055秒；本次Range探测与4路分段下载墙钟83.808秒，各段HTTP206、精确范围、总长度与ETag核验通过。下载结束UTC `2026-10-10T11:28:39.672981+00:00`，解码约0.891秒。中间未持续观察服务端，所以不把相隔23分钟的success查询时间称为模型生成耗时。
- **部署结论**：四帧来源 `Library/Locus/tmp/enemy1021_idle_v1_frames/keyed_ui_t79/idle1_f001.png`、`idle2_f025.png`、`idle3_f049.png`、`idle4_f073.png`；SVL报告显示RGBA、半透明0、幕布残留0。以固定偏移(74,91)零重采样补到1108后部署，Clip为0.96s循环并接入Idle，Prefab scale=0.090161，碰撞盒/血条世界尺寸匹配基准。Play Mode自然生成、四帧循环、旧ShieldVisual清空和对象池复用通过，Console错误0。
- **Idle 阶段边界**：本段记录当时仅完成 Idle 的部署。Walk 后续已完成六帧制作、部署并由用户验收，见下一项；Attack/Hit/ShieldGuardHit/ShieldBreak 等其余动作和独立掉盾对象仍未完成。

### Enemy 1021 持盾 Walk 视频 v1（六帧已部署，用户确认验收通过）

**最新授权与状态：视频与六帧经用户验收后，已使用现有SVL本地Chroma完成透明处理并部署 Unity；用户明确“已验收确认walk帧已部署”。本次未创建新收费任务。用户同时要求今后部署仅接入第一关及对应动画，完成后通知用户开游戏验收；只有在对话中明确要求时才运行自检。**

- **Task ID**：`task_2d7741be5ad14839b33f12f061c6b606`。创建 HTTP 200，accepted UTC `2026-10-11T02:32:05.621600+00:00`；第11次查询于 UTC `2026-10-11T02:33:49.915782+00:00` 返回 `success`，未重新创建。
- **规格**：`seedance-2-fast`、请求4秒/720p/1:1、无音频、无水印、seed=-1；完整获批英文词 SHA-256 `e51dc65ba349be6a395c9551ee814eb9ba2c01d533afcf109ffea043a88a3464`。
- **参考图**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`，1024×1024 RGB，344,540 bytes，SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`；一次上传，同一 URL 作为 `first_frame` 和 `last_frame`。
- **视频**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_walk_v1.mp4`，2,812,023 bytes；SHA-256 `2d20f36dc068c73c0710971b69c8f1ea8c650e809f787921eee2baef636fbc58`。H.264，960×960、24fps、97帧、容器时长4.041667秒，音频流0；首帧PTS=0、末帧PTS=4.000秒。MP4顶层 `ftyp/uuid/free/mdat/moov` 和全部97帧解码通过，`.part`已原子改名并不存在。
- **下载留档**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_walk_v1.download.json`；运行清单 `Library/Locus/tmp/enemy1021_walk_video_v1/download_manifest.json`；检查接触表 `Library/Locus/tmp/enemy1021_walk_video_v1/contact_sheet.png`，检查帧0/24/48/72/96。请求留档为 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_walk_v1.request.json`；运行记录为 `Library/Locus/tmp/enemy1021_walk_video_v1/runtime.json`。
- **usage/cost**：completion_tokens=87,300，total_tokens=87,300，`cost.spend=0.48888`；币种未推断。下载未创建新收费任务。
- **计时**：Range分段下载墙钟23.167秒，四段HTTP206、精确范围、总长度与ETag核验通过；下载完成 UTC `2026-10-11T02:53:56.995255+00:00`，完整解码约0.96秒。
- **有效帧提取（2026-10-11）**：97张原始RGB帧已解码至 `Library/Locus/tmp/enemy1021_walk_v1_frames/clean_raw/f001.png` 至 `f097.png`；f001对应零基0，f097对应零基96。源帧全为960×960，不裁切、不缩放、不去背。
- **选帧依据**：下半身固定ROI相似度在滞后约11帧（0.458秒）出现重复低谷；对比中段完整周期f040→f051，排除首尾Idle和相邻重复平台。六帧按顺序为 **f040/f042/f044/f046/f048/f050**（源时间1.625/1.708/1.792/1.875/1.958/2.042秒），覆盖两腿承重、前摆与回收相位。候选PNG字节哈希与对应原始帧一致，未通过逐帧伸缩/平移掩盖动作变化。
- **候选交付**：`Library/Locus/tmp/enemy1021_walk_v1_frames/selected_v1/walk1_f040.png` 至 `walk6_f050.png`；完整选择与哈希在 `Library/Locus/tmp/enemy1021_walk_v1_frames/selected_frames_v1.json`。接触表 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_CANDIDATES_v1_6frames.png`；循环GIF `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_CANDIDATES_v1_loop_0p6s.gif`。两者只用同一个固定预览ROI放大人物，原始候选保持全画布；GIF按每帧0.1s/循环0.6s为评审候选节奏，不是源视频时序或正式Clip保证。
- **六帧验收与抠图（2026-10-11）**：用户明确验收六帧并授权抠图；源帧哈希再次核对一致。使用现有 `C:/sprite-video-lab` 与 `C:/sprite-video-lab-models/venv/`，无新安装、无服务启动、无远程生成。自动取背景色，容差扫描70/79/90后取 **T79 / softness16 / halo1 / 半透明转不透明**，不做平滑重绘。
- **透明结果**：`Library/Locus/tmp/enemy1021_walk_v1_frames/keyed_ui_t79/` 内的 `walk1_f040.png`、`walk2_f042.png`、`walk3_f044.png`、`walk4_f046.png`、`walk5_f048.png`、`walk6_f050.png`，均为960×960 RGBA、Alpha0/255、四角透明。各帧半透明像素/幕布残留/封闭孔均0；白底可见绿gEx>8为434–615，中位536.5；被删主体中位12.845%（幕布族阈值口径，非真实身体损失比例）。距边4px内部RGB平均差最大0.005467，未重绘角色。
- **几何与预览**：无逐帧缩放/裁切/移动/对齐，源图诊断框与透明轮廓差约1–3px；跑步抬脚/起伏保持原样，不把脚底逐帧拉齐。白底与棋盘格六帧图 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_6frames_white_alpha.png`；局部对照 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_edge_checks.png`；抠图后循环GIF `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_loop_0p6s.gif`（0.1s/帧，仅评审节奏）。六帧图、局部图、单帧与GIF已回读附图。
- **抠图报告**：`Library/Locus/tmp/enemy1021_walk_v1_frames/svl_matte_walk_v1_report.json`；完整Alpha/来源哈希/几何核验 `Library/Locus/tmp/enemy1021_walk_v1_frames/walk_matting_verification_v1.json`。
- **Unity 部署（2026-10-11）**：六帧按 Idle 相同固定偏移 `(74,91)` 零重采样补至1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_walk1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_walk6.png`。`Assets/Animations/Enemy_1021_Walk.anim` 为30fps、0.6秒循环、六个唯一精灵，含旧 `ShieldVisual` 清空键；`Assets/Animations/Enemy_1021.controller` 的 `Walk` motion 已替换，Prefab scale 仍为0.090161。Prefab、Idle Clip及其meta与部署前备份字节一致。
- **部署留档**：`Library/Locus/tmp/enemy1021_walk_deploy_v1/preparation_report.json`、`Library/Locus/tmp/enemy1021_walk_deploy_v1/asset_verification.json`、`Library/Locus/tmp/enemy1021_walk_deploy_v1/runtime_verification.json`。用户提出新边界前已完成受控对象池实例运行测试：117次采样、六张Walk与四张Idle均可见、两次前进一排及回Idle、同实例回收再取出通过；不代表自然波次调度回归。后续画面自检已停止。
- **用户验收**：用户确认 Walk 帧部署验收通过；今后动画部署完成即通知用户开游戏检查，未明确要求不进入运行自检，详见 `memory/user-preference.md`。

### Enemy 1021 ShieldGuardHit 格挡关键帧参考图 v1/v2（2026-10-11，均未通过；继续优化）

**授权与状态：用户批准两张格挡关键帧参考图，K1 接触承力与 K2 微小回弹各提交一次，API 返回 HTTP 200，图片已下载并通过 PNG/尺寸核验。用户随后明确验收不通过：角色姿态基本没有变化，仅在盾牌上出现特效，违反动作素材禁止特效的要求。两张均为失败版，不用作后续动作参考；未重新生成，也未进入视频、抠图、对齐或 Unity 部署。**

- **参考图**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`，1024×1024 RGB，SHA-256 `446d6d558baa7d4a5d6e6fd9bf712c07b99c63a1c4f2e711cc0b28a7564427d0`；唯一职责为锁定1021身份、装备、盾牌、比例、配色和画风。
- **共同参数**：`POST https://api.muskapis.com/v1/images/edits`，multipart 单张 `image[]`；`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、`size=1024x1024`、`output_format=png`、`n=1`；每张只提交一次，无自动重试。中英文提示词与完整字段分别保存在两份 `.request.json`。
- **K1 接触帧**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v1.png`，1,098,725 bytes，SHA-256 `56fdbfc3ae9e3f2676199830250e13aeb69799f40416fa0e3ca61bc0dbbaefef`；生成 POST 约115.9秒，下载约3.022秒，usage input/output `893/958`，接口未返回费用；URL host `pre-signed-firefly-prod.s3-accelerate.amazonaws.com`。
- **K2 回弹帧**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_rebound_v1.png`，999,232 bytes，SHA-256 `f19f082ed9948df52863024700e11044ee24048b980a03ae4ab510d7eb83ef39`；生成 POST 约122.702秒，下载约3.553秒，usage input/output `1207/8597`，接口未返回费用；URL host `cdn.jd23kjs.work`。
- **机器核验**：两张均为可读 `1024×1024` PNG、Color Type 2/RGB、文件大小大于1KB；这是符合本次浅灰参考图请求的结果，不是透明 Unity 素材。
- **v1验收与修订**：用户明确否决v1两图，指出角色姿态没有变化、仅出现盾牌特效，且动作规范不允许特效。按反馈重新设计肩肘承力、盾牌压入/回弹、膝屈与空手补偿，并将火花、闪光、木屑、裂纹、运动线、残影等全部设为禁止；用户审阅修订词后明确要求发起生成。
- **v2 K1承力压入**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v2.png`，1,111,759 bytes，SHA-256 `6dd5400f33134dfbcdfcd0fc7f75fec670560a6ced628a41a24cac3da34e7772`；HTTP200，生成POST约161.104秒，下载约7.386秒，usage input/output `1167/8596`，接口未返回费用；URL host `cdn.jd23kjs.work`。
- **v2 K2释放回弹**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_rebound_v2.png`，1,020,121 bytes，SHA-256 `49df3772aa0649236534f176c6f47a87415f8dfb2affda1c1b6b416308343b4e`；HTTP200，生成POST约97.053秒，下载约3.356秒，usage input/output `831/1084`，接口未返回费用；URL host `pre-signed-firefly-prod.s3-accelerate.amazonaws.com`。
- **v2用户验收结论与下一版动作方向**：用户确认盾牌位移有效，但角色整体肢体姿态仍无足够变化，v2未通过。下一轮先只改K1：保留盾位移，以骨盆为支点令上身向画面右侧轻微倾斜；4–6°作为初始绘制目标，须使头盔/肩部相对骨盆可见右移，结合持盾肩肘收紧和双膝屈曲。躯干仍正面朝向、不后仰，脚底和根位置固定，动作图绝不出现特效。K1验收前不生成K2。新收费Prompt须先交用户审阅并明确批准，尚未再次生成。
- **请求留档**：v1请求、响应和图片保留在输出旁sidecar及 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v1/`。v2完整中英文提示词、脱敏响应与核验位于输出旁 `.request.json`、`.response.json`、`.result.json`；配置与脚本位于 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v2/`。v2仅静态姿态参考，未生成视频、抠图/对齐或Unity部署。

### Enemy 1021 ShieldGuardHit K1 v3（2026-10-11，已生成，待用户验收）

- **用户授权与目标**：用户认可K1 v2的盾位移方向，但认为肢体变化不足，要求上身向画面右侧轻微倾斜。用户审阅完整中英文词与双参考职责后明确批准，只生成K1。获批目标为骨盆支点、上身右倾约8°、头肩胸相对骨盆右移25–35px、脚底与根位置固定、正面朝向、不后仰、无任何特效；这些是提示词目标，不是成图测量结果。
- **实际输入**：图1 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` 锁定身份、Idle基准、装备和构图，SHA-256 `446d6d558baa7d4a5d6e6fd9bf712c07b99c63a1c4f2e711cc0b28a7564427d0`；图2 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v2.png` 只参考盾臂收紧及盾位移，SHA-256 `6dd5400f33134dfbcdfcd0fc7f75fec670560a6ced628a41a24cac3da34e7772`。两张都是1024×1024 RGB，原图未裁切或处理。
- **请求与结果**：`POST /v1/images/edits`，multipart两个 `image[]`；`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、PNG、`n=1`，英文原词实际提交SHA-256 `aced6647caae55ce3dba14d2d71f373912d1c452d537a8f0d91678bcf8675bde`。仅一次POST，无自动重试，HTTP200；生成请求63.108秒，下载3.150121秒，usage input/output `2662/7024`，total `9686`；没有返回task id或cost，URL host `r2.52image.xyz`。
- **文件**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v3.png`，965,738 bytes，SHA-256 `ec3613fc53e63c38f9ae10b5eabd33054fcbd73529726f9d90eed81ac31b1031`。实际1024×1024 RGB PNG，完整解码和落盘回读哈希一致。下载完成UTC `2026-10-11T12:32:56.382210+00:00`。
- **视觉与用户反馈**：原图及Idle/v2/v3对照已回读附图。未见火花、木屑、震动线；盾位、盾臂和空手姿态改变。代理曾认为右倾读感偏弱，**用户随后反馈“其实已经很接近了，建议稍稍让其缩脖子表达受到冲击即可”**，因此保留v3整体姿态，不因代理倾角判断再增加侧倾；尚未最终验收。未定量核验倾角/脚底锚点。对照 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v3/K1_IDLE_V2_V3_REVIEW.png` 为相同完整画布50%最近邻预览，无各图独立裁切、对齐或姿态处理。
- **v4生成结果**：用户批准只补轻微缩颈后，使用v3作为唯一参考提交一次编辑。`POST /v1/images/edits`，`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024 PNG，HTTP200；生成请求99.501秒，下载3.749151秒，usage input/output `653/1023`，total `1676`，cost未返回，URL host `pre-signed-firefly-prod.s3-accelerate.amazonaws.com`，无task id。
- **v4文件**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v4.png`，994,135 bytes，SHA-256 `fe6d53654ee1f985f440f835a9e7d0bef35b264f8cbc5d42e0ea399f40ad875d`；实际1024×1024 RGB PNG，完整解码、尺寸和哈希回读通过。
- **v4视觉与用户反馈**：未见火花、木屑、震动线等特效，缩颈读感比v3明显；但同时带动了头部、肩甲和盾牌/构图，变化大于仅颈肩局部微调的目标。用户随后反馈“很好”，要求用v4思考视频生成词，现已作为获认可的承力姿态参考。K2仍未另行生成。v3/v4对照 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v4/K1_V3_V4_REVIEW.png`：上排相同完整画布50%预览，下排相同颈肩ROI 100%对照，未对齐或修改原图。
- **v4留档**：输出旁 `.request.json`、`.response.json`（下载URL脱敏）、`.result.json`；配置和脚本在 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v4/`，脚本已执行，禁止重跑。
- **留档与范围**：输出旁 `.request.json`、`.response.json`（下载URL脱敏）、`.result.json`；脚本与配置在 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v3/`。K1生成脚本已执行，不要重跑。未生成K2 v3、视频、透明帧或Unity资产。

### Enemy 1021 ShieldGuardHit 六秒视频 v1（2026-10-11，透明六帧v3已接入，用户游戏验收通过）

- **授权**：用户认可K1 v4，并审阅完整中英文视频词后明确“时间改为6秒，发起生成”。仅将源视频时长由4改为6，其他提示词与参数保持方案。
- **Task ID**：`task_e6757d35965a46c389e06bfd86a6d4ae`，创建HTTP200，accepted UTC `2026-10-11T13:25:25.591157+00:00`。仅创建一次，无自动重试。
- **实际参数**：`seedance-2-fast`，6秒 / 720p / 1:1，无音频、无水印、seed=-1。两张普通 `reference_image`，未混用 `first_frame` / `last_frame`。
- **实际输入**：图1 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` 锁定身份和起止Idle，SHA-256 `446d6d558baa7d4a5d6e6fd9bf712c07b99c63a1c4f2e711cc0b28a7564427d0`；图2 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v4.png` 锁定缩颈、耸肩及盾位的承力峰值，SHA-256 `fe6d53654ee1f985f440f835a9e7d0bef35b264f8cbc5d42e0ea399f40ad875d`。原图均1024×1024 RGB，未去背、染绿或缩放；上传分别7.282秒和6.661秒，创建请求2.108秒。
- **动作目标**：约0.5秒接住左侧来力；盾向画面右侧受压、缩颈耸肩、膝屈和空手延迟，随后盾向左回弹、颈肩释放回Idle。提示词仍要求0.25–0.35秒完成受力与恢复，余下保持Idle；固定双脚/根位置/机位，无任何特效。精确时序为请求目标，成片须独立验收。
- **查询与费用**：首轮12次全部 `generating`，随后查询同一任务UTC `2026-10-11T13:38:48.417507+00:00` 返回 `success`，usage completion/total `130500/130500`、cost `spend=0.7308`（币种未知）。未重新生成。
- **视频落盘**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.mp4`，2,310,345 bytes，SHA-256 `cefcc521c18651cadc0777a29f44f23b5fb44ccd97ad76290eba2d78692d7e1f`。实际960×960、24fps、145帧、H.264，容器时长6.041667秒、first/last PTS `0/6.0`、音频流0。
- **下载与核验**：四段Range精确覆盖全文件，每段HTTP206、字节数和ETag核验通过；下载墙钟89.724秒，完成UTC `2026-10-11T13:40:18.140569+00:00`，全部帧解码1.189秒，MP4盒结构 `ftyp/uuid/free/mdat/moov` 完整，落盘哈希独立回读一致，最终 `.part`已改名且不存在。
- **留档**：完整请求 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.request.json`，下载后13,481 bytes，已同步success、usage/cost和报告；英文原词SHA-256 `fc83097a561aee4862ea902d516bb9ee94998d97f06d9213388e4057af87ddf4`未变。下载报告为同目录 `.download.json`，运行/分段记录在 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/runtime.json`、`download_manifest.json`。
- **视频验收与六帧截取**：用户明确“已验收，请截取有效帧”，要求六帧。145张RGB原始帧已完整解码至 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/clean_raw/`，固定身体ROI差值及逐帧目检定位受力f021..f026和回弹f090..f101；中间f027..f089几乎保持同姿态，不均匀采样整段六秒。
- **六帧候选**：零基f023/f024/f026/f090/f093/f101，时间0.958/1.000/1.083/3.750/3.875/4.208秒；初次承力→压入→缩颈峰值→回弹→颈肩释放→恢复Idle。目录 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/selected_v1/`，文件 `guard1_f023.png` 至 `guard6_f101.png`。六张源图均960×960 RGB，原始PNG字节与重新解码像素双重核验通过；用户已验收六帧并明确要求开始抠图。源文件未改动，`selected_frames_v1.json` 已记录 `selected_frames_accepted_by_user=true`。
- **候选交付与留档**：`Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_SELECTED_6_REVIEW_v1.png`、同目录 `GUARD_SELECTED_6_PREVIEW_v1.gif` 已回读附图，选择及哈希记录 `selected_frames_v1.json`。预览使用统一ROI；GIF每帧80ms/一轮480ms只表示候选姿态顺序，不定义源时序或正式Clip。视频与六帧选帧均已验收；透明v3交付后，用户明确“已验收，请接入”，已授权并完成静态接入。
- **抠图v3（当前交付）**：本地现有SVL Chroma自动取底色，threshold14/softness16/halo1/半透明转不透明；灰底接近甲胄高光，补充底色欧氏距离26、8连通外部背景及逐帧确认的真实空隙清理，并保护内部高光。六张960×960 RGBA位于 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/keyed_gray_v3/`，沿用 `guard1_f023.png` 至 `guard6_f101.png`；原画布与动作位置保留；该透明源目录未裁切、缩放、平移、对齐或重绘，另按既有固定补边规则部署到Assets。无远程生成、安装或新增生成费用。
- **抠图核验与预览**：六图源/输出哈希回读一致、Alpha仅0/255、透明RGB全0，已识别连通背景及真实空隙泄漏0、距可见轮廓4px的内部RGB改动0。报告 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/guard_matting_verification_gray_v3.json`；白底/棋盘格 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_6_WHITE_CHECKER_v3.png`、原图/白底/深底细节 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_SOURCE_WHITE_DARK_DETAILS_v3.png`、GIF `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_6_PREVIEW_v3.gif` 均已回读。GIF80ms/帧仅用于姿态评审，不定义正式Clip。
- **中间版与验收边界**：`keyed_ui_t14_connectivity_v1/` 与 `keyed_gray_t14_v2/` 因局部近灰高光保护/外部灰边问题被v3替代，试验文件保留。最终透明结果已获用户验收并授权接入；本轮未进入Play Mode或运行自检。
- **ShieldGuardHit静态接入**：六张透明v3按现有固定 `(74,91)` 偏移零重采样补边至1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldGuardHit1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldGuardHit6.png`，源像素保留。导入沿用Idle的Single/PPU16/Point/无压缩/无Mipmaps/Tight/Center；实际Unity导入1024×1024、Sprite PPU14.7870035，与既有动作一致。新建 `Assets/Animations/Enemy_1021_ShieldGuardHit.anim`：30fps、0.30秒非循环，六帧键位0/0.033333/0.066667/0.133333/0.2/0.266667秒，含 `ShieldVisual` null键，无Transform曲线或AnimationEvent。仅替换 `Assets/Animations/Enemy_1021.controller` 的ShieldGuardHit motion，保留exit time 1、duration 0回Idle转场。
- **接线保存与游戏验收**：Prefab已引用1021控制器与 `blockAnimatorState=ShieldGuardHit`，第一关 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` 已含1021；未修改Prefab、场景、第一关配置、代码或其他动画。报告 `Library/Locus/tmp/enemy1021_shield_guard_deploy_v1/preparation_report.json`、`Library/Locus/tmp/enemy1021_shield_guard_deploy_v1/asset_verification.json` 已回读，补边像素一致、六帧引用完整、Clip/Controller保存及重导入完成。按用户边界，代理未进行Play Mode、动画采样、对象池或画面自检；用户自行开游戏后明确“已验收，更新交接文档准备在其他对话继续任务”，格挡游戏表现验收通过。部署留档已同步 `user_game_acceptance=accepted`、`game_acceptance_pending=false`；后续从 `plan/enemy1021-art-production-handoff.md` 与 `plan/enemy1021-minimal-playable-handoff.md` 继续剩余目标，不重复制作已验收动作。

### Enemy 1021 持盾 Attack 盾撞视频 v1（2026-10-11，素材v2已验收并接入，用户游戏验收通过）

**授权与状态：视频仅生成一次；用户认可盾撞动作但指出整体前移/缩放，确认十二帧后授权抠图和对齐。用户随后明确“已验收，请接入然后等我验收”，现已使用对齐v2补边、导入并接入1021 Attack；用户已完成游戏验收并确认通过。本轮只静态接入，未运行额外自检。后续在其他对话继续1021动画与机制完善。**

- **Task ID**：`task_941e179a2fbd4ee080d0e7c66022469b`；创建 HTTP200，accepted UTC `2026-10-11T06:29:01.914241+00:00`。
- **规格**：`seedance-2-fast`，4秒/720p/1:1，无音频、无水印、seed=-1。
- **动作语义**：双膝微屈、右肘收盾蓄势，躯干允许约15–25°小幅侧转；腿腰与右肩带动盾面中上部短促前顶，接触短顿后回收至Idle。双脚原地，镜头固定，盾仍在画面左侧，持盾手隐藏；不加碰撞特效。用户授权此次小幅侧转，不能套用旧模板的“禁止任何躯干转动”。
- **实际参考**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`，1024×1024 RGB，344,540 bytes，SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`。只上传一次，同一URL作为 `first_frame` 和 `last_frame`，未混普通参考图、未另生关键姿态图。
- **完整请求留档**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.request.json`；下载前7,857 bytes，下载后已同步 `success`、usage/cost与下载报告路径，原提示词保持不变。负面词追加在 `input.content` 文本中，未使用不支持的 `negative_prompt` 字段；提交文本UTF-8 SHA-256 `fdcb8bff319094b28bc169ea62c3e86fce30b15b28e0af8fb94f9107a014828b`。
- **轮询记录**：首次查询 UTC `2026-10-11T06:29:03.954831+00:00`，第12次 UTC `2026-10-11T06:30:59.709692+00:00`，全部 `generating`；单次超时20秒、总预算120秒，自动轮询已停止。
- **运行记录与脚本**：`Library/Locus/tmp/enemy1021_attack_video_v1/runtime.json`；创建脚本 `Library/Locus/tmp/create_enemy1021_attack_video_v1.py` 已执行，勿再次运行。下载脚本 `Library/Locus/tmp/download_enemy1021_attack_video_v1.py` 经curl查询原任务得到success；`Library/Locus/tmp/resume_enemy1021_attack_video_v1.py` 保留前三个完整分段及末段3,327 bytes，只续传缺失615,900 bytes完成下载，未重新生成。
- **实际成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.mp4`，2,476,908 bytes，SHA-256 `ea41b8a47b01b4d9ddea455222d5653f5d93184a4512b5c9ec99e97e0838b404`；H.264、960×960、24fps、97帧、容器时长4.041667秒、末帧PTS4.000秒、音频流0。顶层 `ftyp/uuid/free/mdat/moov` 及全部帧解码通过，最终文件回读哈希一致，`.mp4.part` 已原子改名、不再存在。
- **下载证据**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.download.json`、`Library/Locus/tmp/enemy1021_attack_video_v1/download_manifest.json`。检查接触表 `Library/Locus/tmp/enemy1021_attack_video_v1/contact_sheet.png` 及0/24/48/72/96检查帧仅用于文件诊断，不是已选生产帧或动作验收结论。
- **费用与耗时**：上传5.256秒、创建请求2.355秒。原任务返回completion_tokens=87,300、total_tokens=87,300、`cost.spend=0.48888`（币种未知），下载未产生新生成任务。success观察UTC `2026-10-11T06:43:56.565016+00:00`，下载完成UTC `2026-10-11T06:48:25.843527+00:00`。首次Python查询TLS握手超时，换curl成功；Range下载末段超时后续传，续传查询+下载+解码墙钟63.318秒，全部97帧解码约0.980秒。初次下载开始至完成269.278秒含失败与本地恢复准备，不当作纯网络吞吐或模型生成耗时。
- **帧处理与边界（用户确认12帧后）**：候选帧原始RGB保留于 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/review_candidates_12/`；用户确认12帧后按SVL Chroma T79/softness16/halo1/半透明转不透明处理，报告 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/matting_report_t79.json`。透明未对齐帧位于 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/matted/frames/keyed_ui_t79/`，12帧半透明0、幕布残留0、封闭孔0。
- **已确认选帧**：零基idx 000/024/032/036/040/048/056/064/072/080/088/096。用户明确要求先选有效帧供验收，再抠图和对齐；这12帧均已获确认，原始RGB字节保留。
- **尺寸/锚点对齐v2（当前交付）**：用暴露的头盔冠部/眼部实际模板匹配尺寸估算整体倍率，排除被盾遮挡的冠饰；按双靴底接触中点对齐到 `(484.25,780)`。整帧等比缩放，最近邻重采样，保留下蹲/盾前伸/侧身姿态，不按逐帧包围框fit。顶撞阶段源倍率约1.217–1.229，修正比例约0.814–0.822；待机起始帧保持1倍原始抠图像素。输出 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/aligned_v2_960/`，报告 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/alignment_report_v2.json`，v1为已替代的中间版。
- **核验与评审产物**：`Library/Locus/tmp/enemy1021_attack_video_v1/frames/verification_report_v2.json` 记录12帧960×960 RGBA、Alpha0/255、透明区RGB全0、画布边界完整、底边均780、重新测得脚底中点最大X误差1.75px。接触表 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNED_12_WHITE_REVIEW_v2.png`；前后对照 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNMENT_BEFORE_AFTER_v2.png`；GIF `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNMENT_BEFORE_AFTER_v2_preview.gif` 为每帧0.12秒评审预览，不定义游戏时序。正常SVL抠图仍有每帧562–806个gEx>8可见像素；整帧尺度/位置校正无法消除局部透视或重绘变化，仍需用户检查边缘和动作。
- **Unity接入（素材v2验收后）**：十二张对齐v2帧按固定 `(74,91)` 偏移零重采样补到1108×1108，导入 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_attack1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_attack12.png`，导入设置与Idle一致。创建 `Assets/Animations/Enemy_1021_Attack.anim`：30fps、2秒非循环、十二个有效Sprite键，`attack6` 接触姿态在1秒；含旧 `ShieldVisual` 清空键。`Assets/Animations/Enemy_1021.controller` 的Attack motion已替换，原转场保留，Prefab沿用该Controller，第一关 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` 已含1021。
- **部署留档与边界**：`Library/Locus/tmp/enemy1021_attack_deploy_v1/preparation_report.json` 与 `Library/Locus/tmp/enemy1021_attack_deploy_v1/asset_verification.json` 已回读；补边像素一致、十二个root引用完整、Clip/Controller已保存。没有修改脚本、攻击数值、命中帧声明、Prefab或场景；本轮未进入Play Mode、未做额外运行或画面自检。用户已验收 Attack 通过，后续在其他对话继续。

### Enemy 1021 HitRight 反侧受击：视频/六帧已验收，用户自抠后已接入第一关，等待游戏验收（2026-10-14）

- **方向与参考边界**：画面右侧来力，击中无盾侧肩胸；用户允许参考1011 HitRight原词，但不上传其动作图片。身份锚点为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`。
- **关键图v1**：`C:/Users/steam/Pictures/gptGen/enemy1021_hit_right_keypose_v1.png`，1024×1024 RGB，1,149,099 bytes，SHA-256 `77049b6b2e328bfa56471b1f678b0aff2f716af260ed8fa4d32c19029b5e6419`。只上传1021 Idle，单次 `gpt-image-2.5-sunburst` 编辑，quality=max/input_fidelity=high。用户当时认可姿态并授权视频；后续成片反馈要求修订，不再将其作为新版视频的动作依据。
- **视频v1**：Task ID `task_0fe59a2e947441778045dac0611a615e`，2026-10-12创建一次；spec为seedance-2-fast/4秒/720p/1:1/无音频/无水印/seed=-1，两个普通reference_image分别为1021 Idle与关键图v1。未上传1011图片，未混用首尾帧。
- **v1下载**：用户要求下载后，于2026-10-13查询现有任务得到success；`C:/Users/steam/Videos/doubaoVideo/enemy1021_hit_right_v1.mp4`，1,462,829 bytes，SHA-256 `75a16f91bf6409f9d51f3fd316111bcdee61bb216a3cea5ae1915a8f3238ed66`。实测960×960/24fps/97帧/4.041667秒/H.264/无音频，容器与全部帧解码通过。4路Range下载30.477秒，返回cost.spend=0.48888（币种未知）。完整请求、响应、下载记录在视频旁同名JSON；检查接触表在 `Library/Locus/tmp/enemy1021_hit_right_video_v1/contact_sheet.png`，不是生产选帧。
- **用户对v1反馈**：参考图让动作走错方向，要求像1011一样歪头，同时盾牌敞开表达没有挡住攻击。旧词的“盾只做很小被动震动”和小幅头部转动压弱了这两个读点；v1视频未获动作验收，不继续选帧、抠图或部署。
- **v2授权**：用户审阅新版完整双语图像提示词后明确“请制作参考图”。仅上传已验收1021 Idle，不上传旧关键图或任何1011图片；生成目标为歪头与盾牌外敞的受击峰值。
- **关键图v2结果**：`C:/Users/steam/Pictures/gptGen/enemy1021_hit_right_keypose_v2.png`，HTTP200，1024×1024 RGB，990,459 bytes，SHA-256 `de620e2a72353f32aefcebd1f8c1bcd81575b64968f8ba7afc7c2dfc0404aeee`。单次 `/v1/images/edits`，gpt-image-2.5-sunburst/quality=max/input_fidelity=high/background=opaque/PNG/n=1；usage input/output=1639/8176，cost未返回，不推断费用。没有重试。
- **v2留档与验收**：完整请求、响应、结果核验在图片旁同名 `.request.json`、`.response.json`、`.result.json`，成图已回读附到对话。盾牌明显外敞、胸甲和持盾臂露出、头部歪斜更明显；脚底位置及局部绘制相对Idle有变化，不能宣称像素或几何锁定。用户随后明确“这个可以，准备视频生成”，v2姿态已认可；完整双语视频词、规格和上传副本呈现后，用户再明确“发起视频生成”，授权一次v2视频任务。
- **视频v2本地准备**：`Library/Locus/tmp/enemy1021_hit_right_video_v2/job.json` 保存完整双语词与拟用seedance-2-fast/4秒/720p/1:1/无音频/无水印/seed=-1规格。两张普通reference_image为1021 Idle与v2峰值，不上传1011图片或旧版HitRight；拟输出 `C:/Users/steam/Videos/doubaoVideo/enemy1021_hit_right_v2.mp4`。主读点为冲击后头部歪斜与盾牌被动外敞；图1决定身份、根与双脚，图2仅决定上身局部峰值。0.30秒受击、0.50–0.65秒经过峰值、2.10秒前回Idle是提示词目标，非实际测得时序。
- **上传副本与待批边界**：本地上传副本在 `Library/Locus/tmp/enemy1021_hit_right_video_v2/upload_copies/01_idle_gray232.png` 与 `02_peak_v2_gray232.png`。两图以外部连通中性背景Mask统一为RGB(232,232,232)，四角64×64区域均值/中位数232、std0；未Mask的源RGB逐像素变化0，内部金属高光保留。峰值图整图平移(-1,+32)px、无重采样或缩放，未裁剪前景；个别靴底约±5px残差，不宣称双脚精确一致。原图哈希不变。准备报告 `reference_preparation.json` 与原件/副本对照 `REFERENCE_UPLOAD_REVIEW_v2.png` 已回读附图。用户审阅后已授权按上述副本、双语词和4秒规格生成；已上传两张副本并创建一次任务，没有制作透明帧或修改Unity资产。
- **视频v2创建与状态**：Task ID `task_f1086e5874ee4ae7bf386a0bd0a1aea6`，HTTP200，accepted UTC `2026-10-13T05:46:47.503966+00:00`；规格及实际提交英文词与获批job一致，Prompt SHA-256 `417023193f446f1babf126ce17bbc1a9422b4984f22329e6ca8e05f22cf4dcd6`。首轮第10次查询于UTC `2026-10-13T05:50:05.375845+00:00`返回success；轮询窗口88.741秒，不当作纯模型推理时长。创建只有一次，没有重试。
- **视频v2下载与核验**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_hit_right_v2.mp4`，1,847,087 bytes，SHA-256 `adbe1efbd2539b17cbd15321fa1b74609cc47f0128515ab19c7bbb901bad1514`。4路Range/ETag验证通过，下载28.444秒；960×960/24fps/97帧/4.041667秒/H.264/无音频/末帧PTS4秒，ftyp/uuid/free/mdat/moov及全帧解码通过，最终文件回读哈希与下载报告一致。原任务cost.spend=0.48888（币种未知），completion_tokens=87300。
- **视频v2交付边界**：完整请求、响应、下载核验在视频旁 `.request.json`、`.response.json`、`.download.json`；运行/下载manifest在 `Library/Locus/tmp/enemy1021_hit_right_video_v2/`。稀疏文件检查接触表 `contact_sheet.png` 已回读，可见歪头与盾外敞，但不证明动作节奏、背景/主体全帧颜色或用户美术验收。用户随后明确“已验收，但是存在严重的流程问题，当前视频并非绿幕”，v2视频动作已验收，背景流程另列故障；不把灰底问题改写为动作未通过。后续补救见下方，不因服务端success自动推进。
- **灰底流程纠正**：此次代理在视频词和副本中选择RGB(232,232,232)浅灰底，未沿用用户需要的绿幕流程。以后角色视频先准备统一绿幕上传副本并验收，不自动沿用定妆灰底。当前视频及原帧保留不变，本轮不重新生成、不收费。
- **97帧解码与六帧候选**：用户提出“能否截取有效帧然后染绿背景再抠图”，已解码至 `Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/clean_raw/`。候选零基f018/f024/f050/f052/f054/f066，依次为已受击敞盾→代表峰值→三段恢复→回Idle；f016/f017过渡异常、前后Idle等待和中段重复姿态均未选。六张960×960 RGB原画布PNG在 `frames/selected_v1/`，`hitRight1_f018.png` 至 `hitRight6_f066.png`，逐张与新解码像素和来源哈希一致，无裁切、缩放、平移、染色或抠图。用户后续明确“已验收挑选帧，清补回绿底然后再抠图”；manifest已更新selected_frames_accepted_by_user=true。选帧PNG/GIF预览此前已回读附图，GIF120ms/帧仅为评审预览。
- **背景实测与本地小样**：全部97帧四角64×64 ROI背景中位数范围RGB227–228，最大std约1.06；主体全帧颜色配准未验证，不仅凭底稳定判主体恒色。对f024做单帧背景连通蒙版（距逐帧灰底26、保护内部高光，SVL T14/softness16），反混合后另存RGB(0,177,64)绿底，再用SVL T79/softness16/halo1/半透明转不透明。样本保留1498个受保护近灰内部像素，识别到的外部底残留0、半透明0、透明RGB非零0，距轮廓4px内部RGB变更0；源几何、原图和视频哈希不变。不使用生成式重绘、安装或远程请求。
- **小样文件与结论边界**：`frames/green_feasibility_sample_v1/hitRight2_f024_green_sample.png`、`hitRight2_f024_green_keyed_sample.png`、直接灰底对照 `hitRight2_f024_direct_gray_sample.png`；`GRAY_GREEN_KEY_FEASIBILITY_REVIEW_v1.png` 与 `sample_report.json` 已回读。染绿依然需要先准确分离灰底，只是中间交付，不能将近灰金属整体染绿。单帧结果支持本地补救，不等于六帧已处理、未知封闭空隙已清除或用户已验收小样。用户已明确授权按已验收六帧进行灰底分离、恢复绿底并本地抠图。六帧批处理已完成，结果待用户验收；没有对齐、部署Unity或改Unity资产。

- **六帧绿底恢复与SVL批处理（已完成，透明验收待返回）**：绿底中间文件在 `Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/green_recovered_v1/`，统一RGB(0,177,64)；最终透明帧在 `frames/keyed_green_t79_v1/`，文件名 `hitRight1_f018.png` / `hitRight2_f024.png` / `hitRight3_f050.png` / `hitRight4_f052.png` / `hitRight5_f054.png` / `hitRight6_f066.png`。每张960×960 RGBA，绿底先补再走SVL Chroma T79/softness16/halo1/半透明转不透明，原始灰底来源不变，无裁切、缩放、对齐或平移。报告 `hitright_green_matting_report_v1.json`、总览 `HITRIGHT_GREEN_MATTED_6_REVIEW_v1.png`、细节 `HITRIGHT_GREEN_MATTED_DETAIL_v1.png` 均已回读。
- **批处理核验与已知边界（2026-10-13）**：六帧连接到外部的灰底残留均0、半透明像素均0、透明RGB非零均0，透明结果与源内部RGB变化均0。最初报告写入因numpy.int64序列化失败，图片已落盘；收尾脚本只读取既有图片补写报告，没有重新生成或覆盖素材。封闭近灰色区域尚保留为前景（可能包含手指/红缨空隙，也可能是金属高光），当时不能宣称全部空隙清理完毕；用户随后要求暂不部署，代理未自动清孔、对齐或接入。
- **用户自抠与接入（2026-10-14）**：用户自行在 SVL 网页处理六帧（文件时间 17:11–17:38），成品仍在 `frames/keyed_green_t79_v1/`，随后明确“我已抠好那批图，你可以接入了”。代理回读：六张 960×960 RGBA，绿边极低（可见像素平均绿优势 0.0、>8 仅0–4像素）；边缘留有半透明像素（155–699/帧），全透明区RGB未清零。为与 1021 其他五套动作一致，补边时做了两项归一：半透明→不透明（699/338/485/363/155/190 像素）与全透明RGB→0（66.9万–78.5万像素）；用户源图未被修改，源文件哈希与记录一致。
- **接入结果**：六张按固定 `(74,91)` 零重采样补边为1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_hitRight1.png` 至 `...hitRight6.png`，导入设置与 Idle 一致（Single/PPU16/Point/无压缩/无Mipmaps/Tight/Center，实际导入1024×1024、Sprite PPU14.7870035）。新建 `Assets/Animations/Enemy_1021_HitRight.anim`：30fps、0.9秒、非循环，6个精灵键（0/0.15/0.30/0.45/0.60/0.75秒）加末姿态保持键（0.8667秒），含 `ShieldVisual` 清空键，无Transform曲线与AnimationEvent；时长与1011已验收的HitRight/HitFlash约定一致（0.9s、6键、exit 0.9回Idle）。控制器新增 `HitRight` 状态与 `HitRight → Idle`（hasExitTime、exit 0.9、duration 0），其余14个状态motion未变；Prefab仍引用该Controller，第一关配置仍含enemyId 1021；121个受保护文件哈希一致。报告 `Library/Locus/tmp/enemy1021_hitright_deploy_v1/asset_verification.json`。未进入Play Mode、未采样动画或运行自检，游戏表现待用户验收。

### Enemy 1021 HitFlash 正面受击关键图：v1–v4均未通过，制作暂停（2026-10-13）

- **授权**：用户同意通用受击HitFlash的对称胸肩压缩、缩颈、盾牌小幅松开方向；完整双语词、参考和参数呈现后明确“发起生成”。只提交一次图像编辑，不自动重试，暂不部署。
- **唯一实际参考**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`，1024×1024 RGB，344,540 bytes，SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`。四角64×64背景实测RGB(0,177,64)、std0。没有上传1011动作图或HitRight姿态图；1011只借用正面受击原词。
- **生成规格与结果**：`POST /v1/images/edits`，gpt-image-2.5-sunburst/quality=max/input_fidelity=high/1024×1024/background=opaque/PNG/n=1；HTTP200。`C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v1.png`，1,051,224 bytes，SHA-256 `0c674c36021787cb91f2737d2192087f4b8706a0d18a24262fb3be143adbd035`，1024×1024 RGB。usage input/output=1932/7024，cost未返回，不推断费用。
- **v1姿态反馈与纠正**：代理最初把头盔下沉与肩甲形状变化读为双肩受力，判断偏乐观；用户明确反馈双肩没有可读移动、缺少轻微后仰，像主动后缩而非受击。v1姿态未通过，不用于视频。新目标是肩关节带肩甲明显上提并后撤、胸口绕腰部轻微向后折开、下巴略被惯性带起，缩颈仅为次要反应；保持固定双脚和正面左右朝向，但允许头胸在前后方向俯仰，不用全身缩放代替。
- **背景边界**：实际绿底四角中位RGB(2,177,66)，std约0.58/0.78/0.83，并非严格RGB(0,177,64)恒色；视频前必须另存同色绿幕上传副本并检查，不能把“提示词写绿幕”当作锁色。原v1 PNG保留追溯，不自行覆盖或重生成。
- **留档**：完整获批请求、响应、结果在图片旁同名 `.request.json`、`.response.json`、`.result.json`；双语准备与执行状态在 `Library/Locus/tmp/enemy1021_hit_front_keypose_v1/job.json`。尚未生成HitFlash视频、选帧、抠图或修改Unity。此前HitRight透明候选保留且暂不部署。
- **v2生成结果**：用户明确“发起生成”后，以同一绿幕Idle为唯一参考提交一次修订请求；未上传v1、1011动作图或HitRight图。`C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v2.png`，HTTP200，1024×1024 RGB，1,082,775 bytes，SHA-256 `6864343baa029793780757e00ede93dfa746e7a1382ba6b0b408ff6a003686e0`；usage input/output=2059/7024，cost未返回，不推断费用。Prompt SHA-256 `eb231431952f4685f7d699b6d7fa18020d38ea13bb33f5e9a1adb35943f9ed1a`；无自动重试。
- **v2视觉反馈**：v2 `C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v2.png` 虽表现肩线抬高、胸口后仰、下巴抬起，但用户明确验收不通过：有明显左右朝向，且后仰不像受击。不可用于视频或选帧。对比图 `Library/Locus/tmp/enemy1021_hit_front_keypose_v2/IDLE_V1_V2_POSE_REVIEW.png` 对照Idle/v1/v2，不改变原图。
- **v3修订方向与边界**：去掉明显12–15°后仰目标，改为上胸被击退约5–8°的短促轻微后仰，避免把正面受击误导成转身或挺胸；保持胸甲中心、腰带扣、面罩中线和骨盆严格朝正前方。主要可读点改为双肩在根与脚固定下短促同步上提、上胸局部受压缩短、头颈和手臂稍滞后；盾只作次级小幅松动。使用词强制镜头/身体左右朝向锁定，不要求视觉上露出侧面。完整中英文v3提示词和绿幕参数保存在 `Library/Locus/tmp/enemy1021_hit_front_keypose_v3/job.json`。用户审阅完整双语词后明确“发起生成”，已仅上传绿幕Idle图并提交一次单图编辑；未上传v1/v2或1011动作图。
- **v3生成结果与验证**：HTTP200，`C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v3.png`，1,071,680 bytes，1024×1024 RGB，SHA-256 `8878d97aeab84c39b0c886a40178c86f6ac9b3741b402bc4b05d9b6f80b75842`，回读格式和哈希一致。规格sunburst/max/high/不透明PNG/n=1不变；Prompt SHA-256 `1295e680675a9ad20483165cdd7b9056709b533584cf7f4d03bd08befeff11d0`。API返回input/output=1075/963，cost未返回，不推断费用。无自动重试。
- **v3视觉与背景边界**：原图与同画布Idle对照 `Library/Locus/tmp/enemy1021_hit_front_keypose_v3/IDLE_V3_POSE_REVIEW.png` 均已回读附图；代理观察正面朝向比v2稳定，但肩部受力与轻微后仰读感仍弱，头盔/体型相对Idle有变化，不能宣称身份或几何锁定。四角64×64背景中位RGB(1,177,68)，std约0.58/0.82/0.78，不是严格RGB(0,177,64)恒色。用户后续反馈肩部与双手姿态不对、受击感弱，v3不再作为已认可姿态，按1011正面受击图重新修订。未生成视频、抠图或部署；完整请求、响应、结果记录位于成图旁同名JSON。
- **v4准备与授权**：用户要求根据1011正面受击图修正肩肘手语言。代理仅在本地查看1011 Idle和hit1/hit2/hit4/hit5，不上传1011动作图；实际唯一参考仍为1021绿幕Idle `enemy1021_shield_hold_v3_green_v1.png`。肩带上提内收、双肘屈曲留在身体两侧、空手前臂从下垂收向腰侧、持盾肘屈曲并带盾小幅下沉外偏是新词目标。完整双语词与规格在 `Library/Locus/tmp/enemy1021_hit_front_keypose_v4/job.json`；用户明确“发起生成”后提交一次。
- **v4生成与验证**：HTTP200，`C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v4.png`，1,040,103 bytes，1024×1024 RGB，SHA-256 `a0c623298cc50ec34e431f6a83a157a34694c9841f6d94ce2638fece49633669`，回读格式与哈希通过。sunburst/quality=max/input_fidelity=high/不透明绿底PNG/n=1，Prompt SHA-256 `922926c04ea523f7ea3273a73287806650531bfe9d5f1c455f5aa665122d7ebf`。usage input/output=1790/7024，cost未返回，不推断费用。第一次工具调用被权限层拦截、无文件或POST执行；确认提交次数0后换可用Python工具完成唯一一次实际请求，未付费重试。
- **v4视觉边界与最终反馈**：原图已回读附图；代理观察空手屈肘收至腰侧、肩部和持盾臂姿势改变，正面朝向基本保持，但这不是用户验收。四角64×64背景中位RGB(2,176,66)，std约0.58/0.84/0.81，不是严格RGB(0,177,64)恒色，任何后续视频仍需统一绿幕上传副本。用户随后明确“这好像还不如V1版本，算了先写交接文档吧，我们在别的对话继续”。v4未通过，v1不因比较而重新获批；HitFlash v1–v4均无用户认可基准，生产暂停。未生成HitFlash视频、选帧、抠图或修改Unity，所有候选与请求JSON保留追溯。
- **暂停交接留档**：`Library/Locus/tmp/enemy1021_hit_front_keypose_v4/job.json` 与 `C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v4.result.json` 已同步 `not_accepted_user_paused`，不改原图、请求、响应或Prompt哈希。下一对话入口为 `plan/enemy1021-art-production-handoff.md` §0，机制入口为 `plan/enemy1021-minimal-playable-handoff.md` §0；没有批准v5、HitFlash视频、HitRight部署或Unity自检。

### Enemy 1021 HitFlash 视频 v1（2026-10-14，第一关游戏验收通过）

- **本次新授权**：用户明确“继续hitflash动画的制作工作，我建议还是按照v1参考题去准备生成视频”，完整双语视频词、绿幕副本、参考职责和4秒参数呈现后回复“发起生成”。这是新授权使用v1，不抹去2026-10-13关键图v1–v4未通过的历史；视频、选帧、透明结果和游戏表现仍须分别验收。
- **规格与实际提交**：seedance-2-fast / 4秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1。两张普通 `reference_image`，不混首尾帧；实际完整英文Prompt（正文+负面词）SHA-256 `44e1eb9950f515c9ec9c86a07716259cd78010088d46086a2b77276179bd17b8`，只提交一次创建请求。
- **Task ID**：`task_16d49a77f64446e693cf170b55106a32`，创建HTTP200，accepted UTC `2026-10-14T04:23:10.769696+00:00`。图1/图2上传HTTP200，耗时4.320/3.482秒，创建请求2.159秒。首轮在accepted后120秒停止，共查询6次，全部generating；最后观察UTC `2026-10-14T04:25:03.786081+00:00`。usage/cost仍未返回，不推断费用；停止的是本地轮询，非取消服务端任务。
- **实际上传参考**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/upload_copies/01_idle_green_00B140.png`（SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`，负责身份、绘制比例、镜头、根/双脚坐标及开头/恢复Idle）；同目录 `02_hitflash_v1_green_00B140.png`（SHA-256 `e2cb862037bbb054c78617826b14a62e5aeb0fd14610cc131142bc9972255e63`，只负责v1局部胸肩/盾臂/空手受击姿态）。两张均1024×1024 RGB，四角64×64空背景RGB(0,177,64)、std0；未上传1011动作图、v2–v4、HitRight或ShieldGuardHit图片/视频。
- **副本处理边界**：保留源图，只在v1副本替换4连通空背景904,073像素，再整图平移(+2,+9)px；没有缩放、重采样或重绘，背景蒙版外RGB逐像素保留，主体未被裁掉。1,761个绿优势混边/细节像素原样保留，不宣称正式抠图完成。两只靴的残余差异约5px，图1为唯一坐标依据，不将差异生成成滑步。
- **后续成功与下载**：首轮停止后再查询同一task一次，于UTC `2026-10-14T04:34:06.960219+00:00` 观察success，总查询7次、总创建仍1次。usage completion_tokens/total_tokens=87300/87300，cost.spend=0.48888（币种未注明）。同一成片于UTC `2026-10-14T04:36:06.721047+00:00` 下载完成，传输119.752秒；`C:/Users/steam/Videos/doubaoVideo/enemy1021_hitflash_v1.mp4`，1,951,465 bytes，SHA-256 `a36baeab0cf9fd2e276b859855fa89320fe6e790182b4c36abc08cf36fabf091`。MP4 ftyp/moov/mdat结构通过，PyAV逐帧完整解码通过；960×960/24fps/97帧/4.041667秒/H.264/无音轨。
- **请求与停点留档**：视频旁 `.request.json` / `.download.json`、`Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/job.json` / `runtime.json` / `inspection/decode_report.json` 已回读；创建哨兵 `create_attempted.json` 保持一次。首次解码检查因 `to_ndarray` 依赖缺失numpy失败，改用 `VideoFrame.to_image()` 完成全帧解码，没有安装包、重下载或新生成。首/末诊断帧仅验证媒体可读，不是生产选帧或动作验收。
- **动作验收与选帧**：用户明确“动作通过，请挑选有效帧”。原始97帧全部解码保存，候选六帧为零基f028/f034/f043/f046/f049/f052（1.167/1.417/1.792/1.917/2.042/2.167秒），覆盖干净受击姿态、余势、恢复早中晚和接近Idle；避开f018–f027可见火花与长时间近似保持。全部候选为原960×960 RGB PNG字节副本，原始解码RGB和视频重新解码RGB哈希核对一致；没有裁切、缩放、平移、改色、抠图或对齐。
- **候选素材入口**：`Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/selected_v1/`（`hitFlash1_f028.png`、`hitFlash2_f034.png`、`hitFlash3_f043.png`、`hitFlash4_f046.png`、`hitFlash5_f049.png`、`hitFlash6_f052.png`）；清单 `frames/selected_frames_v1.json`。预览 `frames/HITFLASH_SELECTED_6_REVIEW_v1.png` 与 `frames/HITFLASH_SELECTED_6_PREVIEW_v1.gif` 使用同一固定ROI，GIF100ms/帧仅评审帧序、不定义Clip。视频与六帧均已验收，`selected_frames_accepted_by_user=true`。
- **T80抠图授权与完成**：用户明确“已验收，请按照容差80处理抠图”。复用C盘已安装SVL和Python3.10.11/NumPy2.2.6/Pillow12.3.0，运行现有 `Locus/tools/svl-matting/svl_matte_batch.py`；Chroma逐帧自动取色、threshold80/softness16/halo1/半透明转不透明。无收费调用、新安装、平滑处理、重绘或额外全图调色。六张同名960×960 RGBA在 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/keyed_ui_t80/`；源帧哈希未变，最终输出哈希与SVL工作输出一致。
- **T80实测与限制**：六帧半透明0、透明RGB非零0、SVL幕布残留/封闭孔指标0；严格四连通Alpha诊断另识别f028盔顶与红缨间3717像素合法透明负空间，保持透明未填孔（诊断图与 `hitflash_matting_t80_connectivity_audit.json` 保留）。距可见轮廓4px内侧的核心RGB改动0。边缘去溢色改动553–714像素/帧，green_gt8仍280–359像素/帧，不声称严格零绿或所有RGB不变；用户已明确验收透明结果。
- **Unity接入与游戏验收**：新建 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_hitFlash1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_hitFlash6.png`，固定补边 `(74,91)`、1108×1108；新建 `Assets/Animations/Enemy_1021_HitFlash.anim`，30fps/0.3667s/非循环，六个受击键和末帧保持键，显式清空`ShieldVisual`。仅替换 `Assets/Animations/Enemy_1021.controller` 的HitFlash motion，原Exit Time 0.9/duration 0保留。静态导入、源像素、Prefab Controller、第一关enemyId 1021和109个保护文件哈希已核对；用户现明确游戏验收通过。报告 `Library/Locus/tmp/enemy1021_hitflash_deploy_v1/asset_verification.json` 已同步 `user_game_acceptance=accepted`。代理未进入Play Mode、未采样或自检；用户验收不代表代理运行自检。
- **后续未完成项**：当前剩余动作和机制统一见 `plan/enemy1021-art-production-handoff.md` §0.2；HitRight已完成接入，待游戏验收。
- **后续边界**：HitFlash四个阶段（视频/选帧/透明/游戏）均已验收完成；HitRight已接入、待游戏验收。不要重做已验收动作，不自动运行自检。剩余动作与机制清单见 `plan/enemy1021-art-production-handoff.md` §0.2。

### Enemy 1021 ShieldBreak 关键图 v1–v3（2026-10-14，v3姿态已验收）

- **阶段与边界**：当前仅制作第一张“刚刚脱手”关键图。v1被用户以“​​不像是被动脱手，姿态不合格”拒收；v2被用户以“受击方向反了，角色姿态方向反了，没有脱手感，角色右手没有伸张开而是蜷缩感”拒收。两张原图与请求/响应保留，不作为后续参考，结果JSON已标记拒收。用户随后明确v3“正确”，该脱手姿态已验收；后续授权制作无盾终态并已验收，当前只授权视频生成准备，未创建视频任务、制作透明帧或接入Unity。
- **v1/v2追溯**：原图分别为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_break_detach_keypose_v1.png`（1,090,827 bytes、SHA-256 `0fc572323d3bf18d11cbdf8507f8a8dcf7d710c5df94dcae7209e4cd76872e4a`）、同目录 `enemy1021_shield_break_detach_keypose_v2.png`（1,084,556 bytes、SHA-256 `5f62757c11390b8ce06b3da40a4ac8cf1c4f0fa9595b37e8e3a75a4db5f64be1`）。两次均单次POST/HTTP200/sunburst/max/high/1024×1024/opaque/PNG/n=1，cost与task id未返回。v1同时上传Idle与HitRight绿幕副本，v2只上传Idle。v1在G1副本技术量测后未等待副本单独视觉确认即上传，已向用户说明此流程偏差；不能把技术量测当作用户目视验收。
- **v3授权与规格**：用户审阅完整中英文正文、双语负面词、两张实际绿幕副本与参数后明确“发起生成”。一次 `POST /v1/images/edits`，`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、opaque、PNG、n=1；无自动重试。正文与英文负面词合并为实际prompt，SHA-256 `fb0a1bdb8607fbb3a7f8cf47773ff5946ac1cbeed343bb76fa9a4fac73148b41`。
- **v3实际参考**：`Library/Locus/tmp/enemy1021_shield_break_keypose_v1/reference_uploads/01_idle_identity_green_009F3E.png`（SHA-256 `c5c2be6203aa41afd90ff3059b748252e1b458e063855b1d429980aef6584106`）只负责身份、装备、绘制尺度与脚位；同目录 `02_hit_right_reaction_green_009F3E.png`（SHA-256 `1e03b5e7c329160d807f218ec5db1b5d6ad109d58603b159a9b2ea203443d732`）只负责画面右侧来力、上身向左受击与盾臂被动打开，明确不照抄仍握盾的手势。两图均1024×1024 RGB，四角64×64实测(0,159,62)、std0；原图保留，背景外未遮罩像素RGB改动0，无裁切、缩放或平移。
- **v3结果**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_break_detach_keypose_v3.png`，1,094,431 bytes、SHA-256 `b2ba2b253a64e84d11497c2ff6b1d61a9bb0a1ddd15cb207a180ed51ff7fb3bf`；PNG解码通过，1024×1024 RGB，Color Type2，符合本次不透明关键图请求。HTTP200，POST129.609秒、下载5.322秒；usage input/output/total=1927/8288/10215，cost/task id未返回，不推断费用。请求时间UTC `2026-10-14T14:23:50.390393+00:00`，下载校验完成UTC `2026-10-14T14:26:05.353883+00:00`。
- **v3颜色与姿态边界**：实际四角64×64背景中位依次(2,161,65)/(2,161,66)/(2,162,65)/(2,161,66)，相对目标(0,159,62)为Δ(2,2,3)/(2,2,4)/(2,3,3)/(2,2,4)，角区std约0.56–0.97；不是精确恒色。用户随后明确“正确”，v3姿态验收通过；实际绿底仍需视频前另存统一副本，未进行视频时序G2或抠图后G3，不声称主体配色或几何已锁定。原图已通过read附图；代理观察头盔位于腰带扣左侧、画面左手五指张开，不代替用户对被动脱手感的判断。
- **留档**：每张原图旁均有同名 `.request.json` / `.response.json` / `.result.json`，v3另有 `.submitted_once.json`；工程准备稿为 `Library/Locus/tmp/enemy1021_shield_break_keypose_v3/job.json`。本次未进入Play、未抠图、未改Unity资产。

### Enemy 1021 ShieldBreak 无盾终态与视频 v1（修订六帧已验收；抠图/分件v1已完成待验收，未接入）

- **终态图生成与验收**：用户批准一次 `sunburst/max/input_fidelity high/1024×1024/opaque/PNG/n=1` 编辑。成图 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_break_coward_target_keypose_v1.png`，1,011,668 bytes、SHA-256 `24e10b01a3e3be3c848f11a85198393fad3721c6869f221a31a3bf0387097f06`，RGB/ColorType2/解码通过，HTTP200。usage input/output/total=3194/7024/10218，cost/task id未返回；POST64.81秒、下载5.39秒，只提交一次。实际英文prompt SHA-256 `48f226b69481b29debe7ee0779ea8011fba07d6f63449dbe46b56e5eacdeded7`，同名请求/脱敏响应/结果留档；用户现明确“已验收”。
- **终态色键实测**：四角64×64中位(3,158,62)/(3,158,62)/(3,157,62)/(3,157,62)，相对(0,159,62)为ΔR+3/ΔG−1至−2/ΔB0；关键姿态验收不等于下游视频锁色或G3通过。
- **本次用户要求**：准备发起视频，特别指出参考图与视频绿底和光泽不同。已核对既有Idle原片97帧，源`idle1_f001.png`像素与真实f000一致；既有视频首/末空背景中位(0,172,57)/(0,168,56)，全段G中位162–172。输入统一色键不决定视频输出色。
- **最终G1候选**：在 `Library/Locus/tmp/enemy1021_shield_break_video_v1_preparation/upload_copies_final_v3/` 另存三张1200×1200 RGB(0,159,62)副本，原图不变，无重采样、无裁主体，保留区RGB改动0；旧草稿目录不上传。图1改用已验收Idle视频真实帧作为材质/金属高光权威，图2已验收脱手v3与图3已验收无盾终态只提供姿态。副本SHA/平移/残差见 `G1_final_input_preparation.json`，实际候选对照 `FINAL_G1_VIDEO_REFERENCES_REVIEW.png`，同中性底诊断 `MATERIAL_NEUTRAL_DIAGNOSTIC_REVIEW.png`；用户随后明确发起视频生成，这组三张已逐张上传HTTP200并用于下述唯一视频任务；原准备稿和诊断对照保留。
- **光泽诊断与未解决项**：一个头盔局部灰度配准NCC0.723/0.874，关键图匹配区亮度中位相对Idle约+34/+23；单局部且有姿态受光差异，不能推断全图光泽或校色量。未对已验收图自动调色。视频词声明图1独占材质、图2/3只提供动作，但这是软约束；该风险在本批审批与未来输出G2/G3报告列为待决项，不能只记color_issue结案。
- **视频创建及首次轮询（历史快照，最终下载与选帧以下条为准）**：完整双语正文/负面词/职责/参数在 `Library/Locus/tmp/enemy1021_shield_break_video_v1_preparation/VIDEO_GENERATION_REVIEW.md` 和 `job.json`，拟seedance-2-fast/6秒/720p/1:1/无音频无水印/seed=-1，3张普通reference_image。拟输出 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_break_v1_6s.mp4`。用户随后明确“发起视频生成”，已按原准备稿创建一次任务 `task_fea6963084c740be8ac0cd34c8369645`；三张上传和创建均HTTP200。accepted UTC `2026-10-14T16:35:32.160421+00:00`，实际英文Prompt SHA-256 `8418c452b06d3a29094db6b9c559ed3cc8e22194e53dd40118237613b458cfbf`。12次限时查询均generating，最后观察UTC `2026-10-14T16:37:25.386339+00:00`；已停止本地轮询，未取消远端任务，无本地视频/usage/cost，不推断费用。上传3次、创建1次、查询12次，未重试创建。请求留档 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_break_v1_6s.mp4.request.json`，运行记录 `Library/Locus/tmp/enemy1021_shield_break_video_v1_preparation/runtime.json`，一次创建哨兵 `create_attempted.json`；后续只查询/下载此task，不能重跑创建脚本。脱手后源视频拟盾自然左下落出画、人物完整入框；正式本体帧不带飞落盾，引擎接管仍未实现。未进入Play、未抠图、未改Unity。

- **最终下载**：仅查询原 task `task_fea6963084c740be8ac0cd34c8369645`，UTC `2026-10-15T03:28:57.576279+00:00` 观察到success，下载HTTP200，于 `2026-10-15T03:29:00.349823+00:00` 完成。成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_break_v1_6s.mp4`，2,779,848 bytes，SHA-256 `2ff04f2ee8d19c38af23831ec82f7b9fc3f7c883860ba4f0297269581e6fb5ef`；下载及容器校验记录在同路径 `.download.json`。本地解码读取为960×960/24fps/145帧/6.041667秒，正式G2背景统计和G3主体材质对照尚未完成，不据选帧验收宣称这些检查通过。未重新创建任务。
- **修订六帧已验收**：用户否定初版 `frame_000.png` 与 `frame_024.png`，指定在24–48帧寻找脱手前的关联动作；最终零基 **f028/f034/f048/f072/f108/f144**（1.166667/1.416667/2/3/4.5/6秒）已获用户明确“已验收”。目录 `C:/Users/steam/Pictures/doubaoVideo/enemy1021_shield_break_selected_6_frames_v2/`，文件依次 `01_frame_028.png`、`02_frame_034.png`、`03_frame_048.png`、`04_frame_072.png`、`05_frame_108.png`、`06_frame_144.png`；全为原960×960 RGB，未抠图、调色、缩放或部署。`selection_manifest.json` 已记录用户验收，编号预览 `preview_6_frames_v2.jpg` 已回读。前两帧为受击臂外开与张手松盾，后四帧保留原选择；原目录和33/35/37/39等调查帧仅供追溯，不能混作最终六帧。
- **用户分件建议与102现状核查（实施前快照，后续完成以下条为准）**：用户提出“盾牌脱手后单独分件，参考102敌人的做法”。已只读确认102的 `Assets/Animations/Enemy_102_CowardTransition.anim` 同步驱动根Sprite的五张Body与 `ShieldVisual` 的五张Shield（资源位于 `Assets/Sprites/Enemy/Enemy2/Coward/LostSplit/`），并带子物体局部位置曲线；没有创建独立掉落对象，Prefab盾层sortingOrder=1。1021可借鉴素材拆分，前两张保留本体内盾，从选中第3张f048起将飞盾从本体移除，f108左下残余盾也需清除；保持本体统一画布/锚点。正式引擎接管仍按既有Design的独立对象方案，具体分件与接线尚未实施；本次只做调查与验收留档，未改代码/Unity资产、未进入Play、未采样动画。

- **本地抠图与分件v1已完成并获用户验收**：用户明确“请按照需要抠图并拆件”后，使用既有本地SVL、逐帧自动取绿底（本批空角中位约RGB(0,141–142,58–59)），最终T79/softness16/halo1/半透明转不透明；未启用平滑或全图调色。T90初试会改动距边界4px的内部190–435px/帧等，未交付；T79内部仍有0–22px变化，已在Alpha不变的前提下恢复距边界4px内部的源RGB，边缘仅保留SVL去溢色。最终目录 `C:/Users/steam/Pictures/doubaoVideo/enemy1021_shield_break_matted_split_v1/`。
- **交付结构与接管信息**：`body_960/01_body_f028.png`、`02_body_f034.png`、`03_body_f048.png`、`04_body_f072.png`、`05_body_f108.png`、`06_body_f144.png` 全部960×960 RGBA、不缩放/不平移/不重绘；前两张仍含盾，第3–6张本体无盾。f048/f072完整盾分别拆为 `shield_parts/Enemy_1021_shieldDrop_f048.png`（147×254）及 `Enemy_1021_shieldDrop_f072.png`（205×226），各8px透明补边；前者为接管主素材，后者为后续转向参考。`shield_full_canvas_960/` 保存同源位置版；f108左下1236px残片已从人物层移除，只留 `trace/shield_remnant_960/05_shield_f108.png` 追溯，不当完整盾使用。
- **回读与核验**：源六PNG哈希未变，六帧Alpha仅0/255、透明RGB全0、内部源RGB改动0；人物/盾层无重叠，复合后与颜色保护的抠图全身层RGBA逐像素相同，裁剪盾重新定位亦相同。按底色距离≤12的残留0，不宣称严格零绿：复合图g−max(r,b)>8为75–116px/帧。严格4连通透明区检查识别f034头缨空隙380px/手盾空隙57px、f048头缨空隙330px、f072头缨空隙277px；已以原绿底/白底/Alpha三方附图确认是真实负空间，未填孔。报告 `split_report_v1.json`、`verification_v1.json` 与交付说明均在输出目录；工作脚本/原始T79/T90在 `Library/Locus/tmp/enemy1021_shield_break_matte_v1/`。
- **评审与边界**：`preview/BODY_6_WHITE.png` / `BODY_6_DARK.png` / `BODY_6_CHECKER.png`、独立盾 `SHIELD_PARTS_WHITE_DARK.png`、手盾细节及复合核对均已回读；同中性底 `IDLE_MATERIAL_NEUTRAL_COMPARISON.png` 只去除既有Idle部署补边(74,91)，同960画布比例呈现，源ShieldBreak人物相对Idle显小，本次未做尺寸/锚点对齐、全局校色或给出跨动作颜色一致性通过结论。没有调用远程生成，没有修改Unity资产/代码/场景、没有进入Play或采样动画。用户随后明确“已验收，请按需求接入”，透明与分件验收通过并授权接入。
- **ShieldBreak 接入已完成，待用户游戏验收（2026-10-15）**：接入只做到“接入＋配置＋静态核验”，**未进入 Play、未采样动画、未做对象池实测**，游戏表现待用户验收。
- **素材归一取值**：源六帧比已部署 Idle 小约 20%，按“单一全局缩放＋只钉脚底”处理：全局缩放 **1.20**、目标画布 1108、粘贴偏移 **(-22, 6)**（把双靴中点与基线均值对齐到已部署 Idle，再叠加既有 `(74,91)` 约定）。局部头盔配准得分很低（约 0.40），**不是精确标定**；六帧共用同一变换、未逐帧拉齐。取值与证据见 `Library/Locus/tmp/enemy1021_shield_break_deploy_v1/preparation_report_v2.json` 与同目录 `scale_registration.json`。
- **精灵（本次新增 7 张，未覆盖既有动作）**：`Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldBreak1.png` 至 `Enemy_1021_shieldBreak6.png` 与 `Enemy_1021_shieldDrop.png`；导入设置逐项镜像 Idle（Point / Uncompressed / 无 Mipmap / AlphaIsTransparency / 源 PPU 16 / pivot 中心）。逐张哈希见 `preparation_report_v2.json`。
- **Clip 与事件**：新建 `Assets/Animations/Enemy_1021_ShieldBreak.anim`，50fps、实际长度 **0.46s**（与 `breakAnimationDuration` 一致），六姿势键 0/0.06/0.12/0.22/0.32/0.40 秒 ＋ 末帧保持键 0.44 秒，`ShieldVisual` 0 秒 null 键；事件 0.12s `OnShieldReleaseFrame`（本体已无盾的那一帧生成独立盾）、0.46s `OnShieldBreakAnimationComplete`（收尾进入胆怯）。用事件而非墙钟计时，因为命中卡肉会临时禁用 Animator。**注意**：Unity 会按最后一个键＋一个采样间隔自动加长 PPtr clip（首次建成 0.48s，把保持键移到 0.44 才回到 0.46）。
- **胆怯兜底**：新建 `Assets/Animations/Enemy_1021_CowardIdle_Hold.anim`（0.5s 循环、静态复用破盾末帧、ShieldVisual null），替换 Controller 的 `CowardIdle` motion，避免破盾后立刻跳回 102 占位。**这不是新的待机动画，只是兜底**；无盾胆怯家族其余动作仍为 102 占位。
- **独立盾对象**：新建 `Assets/Prefabs/Effects/Enemy_1021_ShieldDrop.prefab` 与 `Assets/Scripts/Effects/ShieldDropEffect.cs`。脱手时只读取敌人姿态一次，之后不跟随敌人的移动/形变/死亡；参数 velocity (-2.3,-0.4)、gravity 13、angular -150、lifetime 0.85，到时自毁。
- **代码改动（4 个既有文件）**：`Assets/Scripts/Enemy/ShieldGuardEnemy.cs`（脱手/结束/打断/池复位＋Inspector 直接引用）、`Assets/Scripts/Enemy/Enemy.cs`（无盾形态表现路由、破盾期不抢 Walk，且仅对 1021 生效）、`Assets/Animations/Enemy_1021.controller`、`Assets/Resources/EnemyPrefabs/Enemy_1021.prefab`（清空旧 `ShieldVisual`、接好 Clip/盾 Prefab/脱手局部坐标 `(-20.0625, -3.84375, 0)`）。
- **静态核验已通过**：完整编译＋域重载完成；事件方法可解析、Clip 长度与键位正确、精灵引用完整、关卡仍含 1021、场景未 dirty、未进入 Play；受保护哈希核对显示只有上述 4 个目标文件变化，102 资产、其他已验收动作与关卡配置均未改。
- **核验留档**：`Library/Locus/tmp/enemy1021_shield_break_deploy_v1/asset_verification.json` 已补写（改为只输出原生数值，避开 Unity 类型序列化报错），记录 Clip 长度/键位、两个事件及其可解析性、Prefab 引用与脱手坐标、盾对象参数、旧层已清空、关卡仍含 1021、七张精灵导入设置与 GUID。**已知缺口**：无盾胆怯家族、持盾死亡、击飞 Rise/Fall 仍为占位；1.20 缩放与脱手时序的手感仍需游戏内确认。
- **接入流程已固化为工作流**：`skill/workflows/action-to-unity-deployment.md`（参数化步骤＋决策表＋不变量）与 `skill/workflows/character-action-pipeline-entry.md`（阶段入口清单）。

### Enemy 1021 CowardIdle 6 帧透明素材 v1（2026-10-17，T=80，**已获用户验收**）

- **用户决策**：采用 **6 帧**（否掉 4 帧）、抠图容差 **80**。
- **抠图**：`svl_matte_batch.py --label CowardIdle --tolerance 80 --sheet`；SVL 自动取键，实测底 `(0,159,63)`、coloured screen；softness 16 / halo 1 / 半透明转不透明，未开平滑。T=80 下幕布残留 0；扫描时 T=90 的绿残留更低（132 vs 246），但按用户指定值执行。
- **结果（逐帧）**：半透明 **0**、透明区非零 RGB **0**、幕布残留 **0**、孔 **0**；可见像素 59,085 / 59,253 / 60,536 / 61,328 / 60,548 / 60,253；距轮廓 ≥4px 内部 RGB 改动 f039 仅 2 px（maxΔ7），其余 0。保留可见区 green>8 共 214–317 px（约 0.35%–0.53%），不声称严格零绿。
- **交付**：`Library/Locus/tmp/enemy1021_coward_idle_video_v1/coward_idle_6frames_matted_v1/` 下六张 960×960 RGBA（与 `keyed_ui_t80/` 同名）+ 清单 `coward_idle_6frames_matted_v1.json`（含逐帧 SHA-256）。
- **帧序**：f039 → f045 → f059 → f069 → f076 → f078（步长 28,041–37,104，比值 1.32）。
- **评审**：`COWARD_IDLE_MATTED_6_REVIEW_v1.png`（上白下深）、`COWARD_IDLE_MATTED_6_CHECKER_v1.png`、`COWARD_IDLE_MATTED_6_PREVIEW_v1.gif`（6×150ms）。
- **边界**：对齐、1108 补边、Clip 创建、Unity 接入均不由本侧做；未改任何 Unity 资产、未进入 Play。
- **用户验收（2026-10-17）**：用户确认“已验收”，6 帧透明结果通过；遗留绿像素（每帧 214–317 px）与 f039 的 2 px 内部偏差均视为已接受。

### Enemy 1021 CowardIdle 循环帧候选（2026-10-17，用户选定 6 帧）

- **前置**：用户验收循环视频内容（`enemy1021_coward_idle_v1.mp4`，1,706,747 bytes，SHA-256 `84c7e29b…fd2f`，960×960/24fps/97 帧）并要求挑选 6 帧或 4 帧循环帧。
- **单周期定位**：呼吸信号用顶边 y（肩上抬高度）与主体像素。静息顶边 = 298（f000-f005、f034-f044、f085-f096）；峰值顶边 = 282（f016-f023、f063-f073）；一次完整呼吸约 **2 秒**，全片含两次呼吸。
- **关键发现**：视频**不是平滑渐变**，相邻帧差异呈“小→大→小”跳变，单帧步长可达 2–3 万像素，平台期连续帧几乎不变（≤344 px）。因此等时间间隔取帧会拿到重复姿态，必须先按姿态聚合再按相位选。
- **选帧方法**：按 ≥4000 px 阈值聚成 29 个姿态组并取代表帧；在该周期内穷举组合，目标为“相邻帧变化最大值/最小值”最小，且强制包含静息与峰值，排除重复的静息/平台帧。
- **候选 A（6 帧）**：`Library/Locus/tmp/enemy1021_coward_idle_video_v1/candidates_six/` — `cowardIdle01_f039`、`02_f045`、`03_f059`、`04_f069`、`05_f076`、`06_f078`；步长 28,041–37,104（比值 1.32）。
- **候选 B（4 帧）**：`.../candidates_four/` — `cowardIdle01_f045`、`02_f059`、`03_f069`、`04_f076`；步长 32,502–37,104（比值 **1.14**，更均匀）。
- **共同属性**：均 960×960 RGB、未抠图、与解码源帧逐字节一致（已校 SHA-256）、未缩放/平移；清单 `coward_idle_candidates.json`。
- **排除项**：两套均**不包含两个静息帧**（首尾都取静息会产生重复帧，步长比升到 3.19–17.96）。
- **评审**：`CANDIDATES_SIX_REVIEW.png`、`CANDIDATES_FOUR_REVIEW.png`；数据 `pose_groups.json`、`phase_candidates.json`、`chosen_frames.json`、`frame_metrics.json`。
- **边界**：未抠图、未组装 Clip、未接入 Unity、未进入 Play、未改任何 Unity 资产。

### Enemy 1021 CowardIdle 循环视频 v1（2026-10-17，已下载、解码并获用户验收）

- **来源**：`plan/enemy1021-art-production-handoff.md` §0.13 词，脚本逐字提取，未改写。
- **规格**：`seedance-2-fast` / 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1；两张普通 `reference_image`（未混 `first_frame`/`last_frame`）。
- **两张上传图**：图1 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`（SHA `3fd64c93…a6c0`）；图2 `Library/Locus/tmp/enemy1021_coward_idle_video_v1/02_coward_idle_peak_green_00B140_1024.png`（SHA `8b5355be…e736`，G1 副本，外部 977,318 px + 边缘 546 px 换色，主体内部 0）。两图四角均精确 `(0,177,64)`、max_dev 0。
- **Task**：`task_d9b8cc176ff54dd7a70be22663228710`；accepted UTC `2026-10-17T10:04:05+00:00`；create HTTP 200；两次上传 HTTP 200；**只创建一次**。usage completion_tokens=87300；`cost.spend=0.48888`。11 次轮询后于 `2026-10-17T10:07:49+00:00` 返回 `success`。
- **下载故障与处理**：首次下载报 `ModuleNotFoundError: No module named 'av'`（提交脚本误用了无 PyAV 的 SVL 解释器）。生成本身成功，**未重建任务**；改用带 PyAV 的解释器查询同一 task 下载：四段 Range 均 206、ETag 一致、全帧解码通过。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_idle_v1.mp4`，1,706,747 bytes、SHA-256 `84c7e29b13bd94a959d2abfd8dcc3c536aadc1ac0b0ff5b320b34163fe62fd2f`；960×960 / 24fps / 97 帧 / 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨。
- **客观量测**：首尾 f000 vs f096 差 **6,721 px**（主体约 10.6%），包围盒差 ≤1 px，中段 f000 vs f048 为 49,057 px；主体像素 63,176–65,788（摆幅 **4.13%**）；两周期同相位 f024 vs f072 差 22,842 px——两次呼吸不完全重复，做循环需按单周期截取；首帧片基 `(0,160,63)`。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_idle_video_v1/` 下 `COWARD_IDLE_DENSE_SHEET.png`、`COWARD_IDLE_FIRST_LAST.png`、`COWARD_IDLE_PREVIEW_v1.gif`、`loop_metrics.json`、`frames_clean_raw/`（97 帧）、`runtime.json`、`job.json`；脚本 `build_job.py`、`generate_video.py`、`download_existing.py` 均已执行，**不要重跑创建**。
- **边界**：未抠图、未组装 Clip、未接入 Unity、未进入 Play、未改任何 Unity 资产；本侧未写或改视频词、不验收画面。用户随后验收视频内容并要求挑选循环帧（见上一条）。

### Enemy 1021 胆怯受击峰值图 v4（2026-10-17，已提交一次；**验收不通过**）

- **来源**：`plan/enemy1021-art-production-handoff.md` §0.17 词，脚本逐字提取、校验哈希后提交，未改写。
- **参数**：`gpt-image-2.5-sunburst` / `POST https://api.muskapis.com/v1/images/edits` / quality=max / input_fidelity=high / 1024x1024 / opaque / png / n=1；单图输入 `image[]`。
- **上传图**：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`（四角精确 `(0,177,64)`，SHA-256 `3fd64c93…a6c0`）；未上传 102 受击图、v1/v2/v3 成片帧或持盾受击图。
- **词指纹**：英文正文 `a4eace35…2923`＋负面词 `709a4169…790c`，合并 `b2cbe6a2…cb8e`。
- **结果**：HTTP 200，POST 42.973 秒；usage input 333 / output 7,024 tokens；cost 与 task id 未返回，不推断。
- **产物**：`C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_keypose_v4.png`，991,457 bytes、SHA-256 `291cc23d363af756a64ce6d872f4681f42e4874119877e67181353613c59ec48`、1024×1024 RGB、完整解码。
- **客观量测**：主体框 450×445（源图 320×438）——宽比 **1.406**、高比 **1.016**、可见像素比 **1.137**；最小留白 275 px（26.9%）；四角中位 `(1,175,63)`、极差 ≤15（非精确 00B140，视频前需重新做 G1 副本）。
- **验收结果：不通过（2026-10-17）**：用户判定“**看着像摆超级英雄的 pose**”。宽大外张的轮廓来自那只大幅甩开的手，读成英雄姿势而非被打中。不得用作视频参考图，也不得抠图部署。
- **边界**：未写视频词、未提交视频、未抠图、未改任何 Unity 资产。

### Enemy 1021 胆怯受击视频 v4（2026-10-18，已提交一次、下载并全帧解码，待用户看片）

- **词来源**：用户批准的草稿 `Library/Locus/tmp/enemy1021_coward_hit_video_v4/draft_prompt.md`（本侧写的，用户审后指令“发起视频生成”）。
- **两张参考图（普通 reference_image）**：① 无盾待机绿幕 `01_coward_idle_peak_source_green_00B140_1024.png`（SHA `3fd64c93…a6c0`，四角精确 `(0,177,64)`）；② 已获用户认可的受击峰值 v8 的 G1 副本 `02_coward_hit_peak_v8_green_00B140_1024.png`（SHA `790d3f10…865e`，背景改 954,834 px、主体内改动 0）。
- **参数**：`seedance-2-fast` / 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1。
- **Task**：`task_3e4271451e904295ad5be8d673703526`，create HTTP 200，**只创建一次**；usage completion 87,300 tokens，`cost.spend=0.48888`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v4.mp4`，1,883,229 bytes、SHA-256 `b32093731a356e771581dfddf7427576d8e204b2582e06370699cf2fe09a73b3`；960×960 / 24fps / 97 帧 / 4.041667 秒 / 末帧 PTS 4.0 / H.264 / 无音轨；四段 Range 与全帧解码通过。
- **实测（逐帧主体框）**：f000–f016 待机 299×410；**f018 直接张到 525×441**（与已认可的 v8 在 960 画布上等宽）、底边从 704 升到 693（离地 11 px）；f018–f062 保持张开约 **1.85 秒**；f063–f069 收回，f069 回到 299×410；之后待机到 f096。
- **与词的对差（需用户判定）**：词要求峰值只保持 0.30–0.60 秒、1.10 秒前收回；实际保持到 2.6 秒才收回。选帧时可以自己压缩，不影响���用。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_hit_video_v4/COWARD_HIT_V4_DENSE_SHEET.png`（每 4 帧）、`contact_sheet.png`、`first_frame.png`、`last_frame.png`、`runtime.json`、`job.json`、`download_manifest.json`。
- **下一步**：交用户看片；通过再选帧→抠图→补边→接入 `CowardHit`。不通过就记原因、等新词，本侧不改词不自重发。

### Enemy 1021 胆怯受击峰值图 v8 · X 形四肢全开（2026-10-17，已提交一次，待用户验收）

- **用户反馈**：v7 “看着像跳起来而不是被电那种四肢完全张开的感觉”。
- **做法**：改用两张输入——图1 = 1021 无盾待机（身份/画风/正面/绿幕），图2 = **自家 1011 `Enemy_1011_dead3`**（四肢张开、腾空）仅作**张开程度**参考；另给了备选参考 `POSE_REF_1011_DEAD_STRIP.png`（六帧并排）。
- **图2 副本**：`Library/Locus/tmp/enemy1021_coward_hit_reference/pose_ref_1011_dead3_green_1024.png`（1108→1024 等比、绿幕、SHA `32e1277c…341a`）。
- **参数**：`gpt-image-2.5-sunburst` / `/v1/images/edits` / quality=max / input_fidelity=high / 1024x1024 / opaque / png / n=1。
- **结果**：HTTP 200，POST 65.561 秒；usage input 2,617 / output 7,024 tokens；cost 与 task id 未返回。
- **产物**：`C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_v8_spread.png`，998,991 bytes、SHA-256 `3f8b8607cbbae9345f84f03a0c526f904e7ed6f5ee08f9148873c1b93370be63`、1024×1024 RGB。
- **客观量测**：主体框 **560×469**（待机 320×438）——宽比 **1.75**（v7 1.21、v4 1.41、1011_dead3 自身参考 1.51）、高比 1.071、可见像素 93,474（1.31×）；底边 739，比待机 752 高 13 px（脚仍离地，但比 v7 的 20 px 少）。
- **对照**：`Library/Locus/tmp/enemy1021_coward_hit_peak_v8/V8_VS_IDLE_V7.png`、`v8_metrics.json`。
- **风险提示**：图2 是 1011 的动作图，可能带来身份/盔甲轻度串味，验收时需看“还是不是 1021”。
- **边界**：仅参考图；未写视频词、未提交视频、未抠图、未改任何 Unity 资产。

### Enemy 1021 胆怯受击峰值图 v7 · 在 v6 上大幅夸张（双臂外扬、双脚离地）（2026-10-17，已提交一次，待用户验收）

- **用户指令**：“双手应该外甩，像是被电一样，允许脚离地；受击动作应该更夸张些”。
- **做法**：不再换参考图，直接以 **v6 成品为唯一输入**做一次夸张化编辑（同一身份/画风/机位，避免风格与角色漂移）。
- **参数**：`gpt-image-2.5-sunburst` / `/v1/images/edits` / quality=max / input_fidelity=high / 1024x1024 / opaque / png / n=1。
- **结果**：HTTP 200，POST 66.773 秒；usage input 1,437 / output 7,024 tokens；cost 与 task id 未返回。
- **产物**：`C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_v7_exaggerated.png`，991,060 bytes、SHA-256 `90c945ee34ee65c4570d1ead71bc19588022791f51879b09db2bcd14e8df9522`、1024×1024 RGB。
- **客观量测（均以胆怯待机为基准）**：主体框 387×442（待机 320×438）——宽比 **1.209**（v6 是 0.944，v4 是 1.406）、高比 1.009；**主体底边 732，比待机的 752 高 20 px，即两只脚离地约 4.6% 身高**；可见像素 71,093≈待机。
- **对照**：`Library/Locus/tmp/enemy1021_coward_hit_peak_v7/V7_VS_IDLE_V6.png`（待机 / v6 / v7 并排）、`v7_metrics.json`。
- **边界**：仅参考图；动作素材本身不带特效（未画闪电/电流），未写视频词、未提交视频、未抠图、未改任何 Unity 资产。

### Enemy 1021 胆怯受击峰值图 v6 · 两张输入（自身待机 + 横版受击参考）（2026-10-17，已提交一次，待用户验收）

- **思路**：v5（从持盾 HitFlash 去盾）被指出“带盾受击的厚重感不适合胆怯”；改用横版游戏的受击帧当**姿势参考**，让模型把它译成正面。
- **图 1（身份/风格/机位）**：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`（SHA `3fd64c93…a6c0`）。
- **图 2（仅姿势参考，CC0）**：`NES Soldier Hurt`（OpenGameArt，作者 Chasersgaming，CC0；`https://opengameart.org/content/brawler-asset-character-soldier-nes`），16×32 单帧，20× 最近邻放大并置中于浅灰色 1024 画布 → `Library/Locus/tmp/enemy1021_coward_hit_reference/pose_ref_soldier_hurt_1024.png`（SHA `c3317313…afc8`）。同色包内还有 `Renegade_Hurt`（已下载备用）。
- **参数**：`gpt-image-2.5-sunburst` / `/v1/images/edits` / quality=max / input_fidelity=high / 1024x1024 / opaque / png / n=1；两张图以 `image[]` 上传。
- **结果**：HTTP 200，POST 52.48 秒；usage input 2,589 / output 7,024 tokens；cost 与 task id 未返回。
- **产物**：`C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_v6_front_from_side.png`，945,941 bytes、SHA-256 `aea80b619d5b03907f66671ab3391c587c7e3039871ef934ef035f6ff2100324`、1024×1024 RGB。
- **客观量测（相对自身待机）**：主体框 302×421（待机 320×438）——宽比 **0.944**、高比 **0.961**、可见像素比 **0.882**；重心左移 6.2 px、下移 6.0 px；最小留白 278 px（27%）；四角中位 `(2,178,69)`。即“向内收、变矮、变薄”，与 v4 的宽比 1.406 完全相反。
- **对照**：`Library/Locus/tmp/enemy1021_coward_hit_peak_v6/V6_VS_IDLE.png`、`v6_metrics.json`；参考姿势图 `HURT_REFERENCE_POSES_24x.png`。
- **边界**：仅参考图；未写视频词、未提交视频、未抠图、未改任何 Unity 资产。

### Enemy 1021 胆怯受击峰值图 v5 · 从已验收 HitFlash 去盾（2026-10-17，已提交一次，待用户验收）

- **动机**：v4 峰值图被否（像摆超级英雄 pose）。不再让模型凭空编姿态，改用用户已验收的正面受击帧 `Enemy_1021_hitFlash2`（源 `hitFlash2_f034`）作基准，只要求去掉盾。
- **输入副本**：`Library/Locus/tmp/enemy1021_coward_hit_reference/02_hitflash2_green_00B140_1024.png`（1024×1024 RGB，四角精确 `(0,177,64)`，SHA-256 `77fa4776…b3f4`）。
- **参数**：`gpt-image-2.5-sunburst` / `/v1/images/edits` / quality=max / input_fidelity=high / 1024x1024 / opaque / png / n=1；单图输入。
- **结果**：HTTP 200，POST 57.907 秒；usage input 1,422 / output 7,024 tokens；cost 与 task id 未返回。
- **产物**：`C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_v5_noshield.png`，995,900 bytes、SHA-256 `18a484325b99b6f3c1154232543870e51a33009ec24ea054c0ce6c45a45687ef`、1024×1024 RGB。
- **几何核验（证明姿态与尺寸未被改）**：输入 bbox 452×573（x 289–741）；输出 **390×573（x 351–741）**——高、顶边 239、底边 812、右边界 741 **完全一致**，只有左边界内收 62 px（盾被去掉）；可见像素 144,117→117,695（−26,422，约等于盾的面积）。四角中位 `(2,179,67)`（非精确 00B140，入视频前需重做 G1 副本）。
- **对照**：`Library/Locus/tmp/enemy1021_coward_hit_peak_v5/V5_VS_HITFLASH2.png`（输入/输出各 50%）、`v5_metrics.json`。
- **边界**：仅参考图；未写视频词、未提交视频、未抠图、未改任何 Unity 资产。

### Enemy 1021 胆怯受击 CowardHit 视频 v1 / v2（2026-10-17，**两版均验收不通过**）

- **v1（用户否决：完全没有受击感）**：按 §0.14 词提交一次，task `task_bae4d800be834f8cbbd9cf3ae93950a2`；成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v1.mp4`，1,913,306 bytes、960×960 / 24fps / 97 帧 / 4.041667 秒、cost 0.48888。抽帧显示 f012/f024 仍是待机护脸、f036 之后只是把手放低。**不再重跑 §0.14**。
- **v2（按 §0.15 重写；已提交一次、下载完成、完整解码通过；**用户验收不通过**）**：用户指令“根据文档发起生成视频”，按 §0.15 原文提交，未改词、未换图、未降参数。
- **参考图**：只上传 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`（SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`，1024×1024 RGB，四角精确 `(0,177,64)`），单张普通 `reference_image`；未上传 102 的受击图、v1 成片帧或已补边的 `Enemy_1021_shieldBreak6.png`。
- **词与指纹**：§0.15 英文正文 SHA-256 `c76eb24a956f6b2041dfd0b0f6e570d427fb3e2944bc3d816c8aa2404e8d7eb4` ＋ 负面词 `69fed04fbbe04def37a9fba2b2c01cf91e2e4eb666fd7f9fd6d25cde4a3c7ee6`，合并后 `3d29a8b5193c0638b75c42ce3bf0e7e58e3652910c5c03c291c44af7af09683c`；脚本从文档逐字提取后校验哈希再提交。
- **参数与任务**：`seedance-2-fast` / 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1；task `task_de9e6bcb777248eeb7c0fa5a257bee1e`，create HTTP 200，accepted UTC `2026-10-17T14:14:05+00:00`，**只创建一次**（`create_attempts=1`）；32 次轮询后 `success`；usage completion 87,300 tokens，`cost.spend=0.48888`（币种未注明）。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v2.mp4`，1,747,950 bytes、SHA-256 `0ceae089e47bd52bd8f7d775d5b5ccd081af7b772d9479f165b76a5858ebd664`；实测 960×960 / 24fps / 97 帧 / 容器 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨；四段 Range 均 206 并按 ETag 校验，MP4 结构 `ftyp/uuid/free/mdat/moov`，全帧解码通过。
- **检查材料（不是选帧）**：`Library/Locus/tmp/enemy1021_coward_hit_v2/` 的 `contact_sheet.png`（帧 0/24/48/72/96）、`inspection_frames/`、`first_frame.png`、`last_frame.png`、`job.json`、`runtime.json`、`download_manifest.json`；下载报告为视频旁 `.download.json`。脚本 `generate_video.py` 已执行，**不要重跑创建**。
- **边界与验收（最终）**：**验收不通过**。用户判定视频里“实际上做了两个语义完全不一致的受击动作，且切换的非常僵硬”，整支作废，不接受用抽帧裁剪补救。逐帧实测支持该判断：f016–f024 抱胸（主体框宽 299→264）、f025–f033 几乎静止（约 0.35 秒）、f033–f042 甩臂（框宽 265→399）、f042 之后静止——两段独立动作中间夹了一段停顿。
- **帧挑选尝试（已否，不得用于部署）**：按用户“只取前半部分、跳过开头 idle”的指示抽过 3 帧稳定姿态 `f024`（1.00s）/ `f036`（1.50s）/ `f044`（1.83s），文件 `Library/Locus/tmp/enemy1021_coward_hit_v2/selected_v1/`，对比图 `COWARD_HIT_3FRAME_REVIEW_v1.png`、预览 `COWARD_HIT_3FRAME_PREVIEW_v1.gif`、清单 `selected_frames_v1.json`、逐帧指标 `frame_metrics.json`。结论：三帧仍是从两段不同语义里各取一帧，选了也是“两个动作硬拼”；用户否掉整支视频。**这些帧不抠图、不接入。**
- **后续**：词由另一侧改写后重新出词；本侧不改词、不重发 §0.14/§0.15。未抠图、未对齐补边、未接入 Unity、未进入 Play、未改任何 Unity 资产。

### Enemy 1021 胆怯受击 CowardHit 视频 v3（2026-10-17，已提交一次、下载并完整解码；**验收不通过**）

- **来源**：`plan/enemy1021-art-production-handoff.md` §0.16 词，脚本从文档逐字提取、校验哈希后提交，未改写。
- **规格**：`seedance-2-fast` / 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1；单张普通 `reference_image`（无盾待机绿幕图，SHA `3fd64c93…a6c0`）。词指纹：英文正文 `9469e0fb…ad15`＋负面词 `4b7e2c7b…04ed`，合并 `723d9b85…576c`。
- **Task**：`task_f121b090d3c54b078ec70db42eda1bd9`，create HTTP 200，**只创建一次**；usage completion 87,300 tokens，`cost.spend=0.48888`。
- **下载故障与处理**：首次下载在分段读取时报 `TimeoutError`（任务已 success），**未重建任务**；改用带续传的分段下载器重试同一 task（段 0 从 81,920 字节续传，其余三段复用），拼接后全帧解码通过。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v3.mp4`，1,652,421 bytes、SHA-256 `bfd4eda1f126ef7697bd98b3e76f85a04fdbf5891bbf6a1494234c89e40ffcc3`；960×960 / 24fps / 97 帧 / 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨。
- **客观量测（供判断）**：f000–f016 idle（298×410）；**f017–f020 人物变高**（顶部 294→252、可见像素 62,757→67,865）；f021 跳到 266×394 并保持到 f053（约 1.35 秒）；**f054 单帧弹回 idle**（变化 58,260 px，无收势）。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_hit_v3/` 的 `COWARD_HIT_V3_DENSE_SHEET_f000_f048.png`、`keyframes/`、`contact_sheet.png`、`runtime.json`、`download_manifest.json`；脚本 `generate_video.py`、`download_existing.py` 均已执行，**不要重跑创建**。
- **验收结果：不通过（2026-10-17）**：用户判据——① 动作非常抽象；② 关键受击动作只是**毫无表情的捂胸**；③ **没有任何受击感**。产物仅作失败样本，不得选帧、抠图、接入；§0.16 词作废，待另一侧再改。
- **边界**：未选帧、未抠图、未补边、未接入 Unity、未进入 Play、未改任何 Unity 资产。

### Enemy 1021 CowardIdle 呼吸最高点关键帧 v1（2026-10-17，**已获用户验收**）

- **来源**：`plan/enemy1021-art-production-handoff.md` §0.12 词，脚本逐字提取，未改写。
- **规格**：`POST /v1/images/edits`；`gpt-image-2.5-sunburst` / `quality=max` / `input_fidelity=high` / `1024x1024` / `opaque` / `png` / `n=1`；英文正文 `8d85157a…0e36` + 22 条负面词 `ad092a98…1fdf` 合并为 `7cd71799…6fbb`。
- **上传副本（G1）**：源 `C:/Users/steam/Pictures/doubaoVideo/enemy1021_shield_break_selected_6_frames_v2/06_frame_144.png` 实测底 `(0,142,59)`，非 00B140。另存 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`：960 中心补边到 1024（偏移 32,32）、外部背景 849,750 px + 2px 边缘环 11 px 换为 `(0,177,64)`、四角精确 std 0、主体内部改动 0、角色未缩放；原帧未动。
- **结果**：HTTP 200、POST 68.859 秒；usage 1365/7024/8389；cost 与 task id 未返回。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_coward_idle_peak_keypose_v1.png`，953,009 bytes、SHA-256 `8b6656bcbe15ed016c94fcf44ad995f2d397d1f97702bf2c4eefc7bdbebb569d`、1024×1024 RGB。只提交一次，未重试。
- **客观量测（与上传副本同口径）**：包围盒源 `320×438` vs 生成 `320×430`；宽比 1.000、高比 0.982、像素比 0.984；底边差 −2 px、顶边差 +6 px；双靴质心 (−2.9,−0.8)、左侧手区 (−0.1,−2.3)、右侧胸前手区 (−4.0,+1.3)、红缨 (−12.8,+7.3)；最小留白 26.8%。
- **背景实测**：四角中位 `(2,178,67)` 附近、dev std 0.54–0.66，非精确 `(0,177,64)`。
- **边界**：未组装 4 帧循环、未做视频、未抠图、未接入 Unity、未进入 Play、未改任何 Unity 资产。对照材料 `UPLOAD_COPY_REVIEW.png`、`COWARD_PEAK_V1_VS_SOURCE.png`；脚本 `build_upload_copy.py` / `build_job.py` / `generate_coward_peak.py` 均已执行，**不要重跑创建**。
- **用户验收（2026-10-17）**：用户明确“已验收”，呼吸最高点关键帧通过；底色偏差（生成图四角约 `(2,178,67)`）视为已接受，不重发不改图。用户同时要求更新交接文档**让另一侧写提示词**：胆怯家族剩余项的生成词由写词侧起草并自行提交，本侧不写词。本侧接下来的职责仍是发起生成/取回产物/选帧/抠图并回写验收状态。

### Enemy 1021 持盾死亡：3 帧透明素材 v1（2026-10-17，T=80，**已获用户验收**）

- **用户决策**：采用 **3 帧**（否掉 5 帧）、抠图容差 **80**、开头 idle 帧无意义从 **f044** 起。
- **为何只能 3 帧**：用「平坦区差异」区分真实姿态与重编码噪声。f045 vs f048 = **0 px**；f049 vs f096 = 1,000 px（90.3% 边缘噪声）；而 f044 vs f046 = 141,696 px、f046 vs f051 = 134,141 px。即 f044…f096 只有 A2/B/C 三个真姿态。证据 `effective_pose_inventory.json`。
- **选定帧**：`dead01_f044`（1.833s）、`dead02_f046`（1.917s）、`dead03_f072`（3.000s）。
- **抠图**：`Locus/tools/svl-matting/svl_matte_batch.py --label Dead --tolerance 80 --sheet`；SVL 自动取键（实测底 `(1,157,60)`，coloured screen），softness 16 / halo 1 / 半透明转不透明，未开平滑。
- **结果（逐帧）**：半透明 **0**、透明区非零 RGB **0**、幕布残留 **0**、封闭孔 **0**；可见像素 156,451 / 153,627 / 136,853；距轮廓 ≥4px 内部 RGB 改动 0 px / 1 px / 6 px（>8Δ 仅 2 px）。保留可见区 `g−max(r,b)>8` 共 403/508/495 px（可见区 0.26%–0.37%），不声称严格零绿。
- **交付**：`Library/Locus/tmp/enemy1021_dead_video_v2/dead_3frames_matted_v1/` 下三张 960×960 RGBA（与 `keyed_ui_t80/` 同名副本一致）+ 清单 `dead_3frames_matted_v1.json`（含 SHA-256）。评审：`DEAD_MATTED_DELIVERY_REVIEW_v1.png`、`_3_WHITE/_3_DARK/_3_CHECKER`、`ZOOM_*`、`DEAD_MATTED_PREVIEW_v1_0p6s.gif`。
- **边界**：未对齐、未 1108 补边、未部署 Unity、未进入 Play、未改任何 Unity 资产。
- **用户验收（2026-10-17）**：用户明确“已验收”，三帧透明结果通过；遗留绿像素（green>8 共 403/508/495 px）视为已接受。用户同时明确**本侧不负责对齐、补边与接入**，这三项由另一侧执行；本侧交付到此结束。
- **接入侧部署（2026-10-17，已游戏验收）**：按固定偏移 `(74,91)` 零重采样补边到 1108（粘贴区逐像素差 0、画布外非零 0），新增 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_dead1..3.png`（导入设置与 `Enemy_1021_hitFlash1` 一致，Unity 实际 1024×1024 / Sprite PPU 14.787）与 `Assets/Animations/Enemy_1021_Dead.anim`（30fps、0.6s、Clamp、键位 0/0.2/0.4＋保持 0.5667、`ShieldVisual` null 键）；仅替换 1021 控制器 `Dead` 状态 motion，其余 14 状态/18 转场不变。核验记录 `Library/Locus/tmp/enemy1021_dead_deploy_v1/asset_verification.json`；未进入 Play、未采样动画；用户 2026-10-17 开游戏验收通过。

### Enemy 1021 持盾死亡：选中帧候选 v1（2026-10-17，视频已验收；5 帧已被 3 帧方案取代）

- **前置**：用户验收视频 v2（`C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.mp4`，2,326,004 bytes，SHA-256 `c2b444a3…9095`，960×960/24fps/97 帧），并授权开始选帧。
- **选帧前的客观分析（关键发现）**：97 帧全部字节唯一，但只存在 **5 个真正不同的姿态**，R态之间是 1 帧硬切而非渐变。判据：帧间真实变化 >24 通道差且 >4000 px 才计为新姿态；跳切点 `f033→f034`（143,480 px / 15.57%）、`f044→f045`（208,206 px / 22.59%）、`f048→f049`（205,966 px / 22.35%）。
- **姿态分组**：`f000-f033` 站立（bbox `[270,230,681,781]`，fill 0.617）；`f034-f036` 受击起手（`[206,214,795,781]`，fill 0.486）；`f037-f044` 张开中段（同 bbox，与上组差 15,766 px，头/躯干微调）；`f045-f048` 展开加强（`[154,217,813,760]`，fill 0.451）；`f049-f096` 终态（`[84,228,866,717]`，fill 0.379）。
- **选帧规则**：每组只取 1 帧，取组内对相邻帧变化最小的一帧（避开跳切边缘与压缩伪影），不机械等距采样。
- **候选 v1（5 帧）**：`dead01_f010.png`（0.417s，fill 0.617）、`dead02_f034.png`（1.417s，0.486）、`dead03_f044.png`（1.833s，0.486）、`dead04_f048.png`（2.000s，0.451）、`dead05_f051.png`（2.125s，0.379）。均在 `Library/Locus/tmp/enemy1021_dead_video_v2/candidates_v1/`，960×960 RGB，与解码源帧 SHA-256 逐一相同，未抠图/缩放/平移。
- **预览（不定义 Clip）**：`SELECTED_REVIEW_v1.png`、`SELECTED_PREVIEW_v1_0p6s.gif`（5帧×120ms）、`SELECTED_PREVIEW_ALT4_0p6s.gif`（4帧×150ms，去掉 f044）。
- **未决**：f044 与 f034 差异主要是头/躯干微调（11,442 px），是否保留由用户定；若求平滑过渡，需重新出视频（本侧不写词、不自行重发）。
- **边界**：**未抠图、未对齐、未补边、未部署 Unity、未进入 Play；未重发视频、未写词。** 数据与脚本在 `Library/Locus/tmp/enemy1021_dead_video_v2/`（`frame_metrics.json`、`pose_groups.json`、`group_analysis.json`、`selected_frames_v1.json`、`extract_and_measure.py`、`distinct_pose_check.py`、`group_analysis.py`、`select_frames.py`）；原始 97 帧在 `frames/clean_raw/`。

### Enemy 1021 持盾死亡：视频 v2（2026-10-17，已下载、解码并获用户验收）

- **发起依据**：用户更新交接后要求“根据交接文档发起视频生成，下载后立刻汇报”。词源为 `plan/enemy1021-art-production-handoff.md` §0.11，由脚本从文档逐字提取，未改写、未换图。
- **规格**：`seedance-2-fast` / 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1；两张普通 `reference_image`（未混 `first_frame`/`last_frame`）。
- **两张上传图**：图1 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`（SHA `d398a1eb…40de`，四角精确 `(0,177,64)`）；图2 `Library/Locus/tmp/enemy1021_dead_video_v2/02_dead_v3_green_00B140_1024.png`（SHA `16bfc410…83cf`，G1 副本，四角精确 `(0,177,64)`、std 0）。上传前两份哈希与文档记录逐一校对一致。
- **Task**：`task_215c88d7209f46bc8e5b36a528c225e2`；accepted UTC `2026-10-17T03:12:03.170732+00:00`；create HTTP 200 / 2.135 秒；上传 HTTP 200（4.76 / 5.72 秒）。
- **轮询**：创建后 12 次限时轮询均为 generating（前两次 queued）后停止本地等待；随后只查询同一任务，UTC `2026-10-17T03:16:07.205752+00:00` 观察到 `success`。usage completion_tokens=87300；`cost.spend=0.48888`（币种未注明）。未创建新任务、未重试。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.mp4`，2,326,004 bytes、SHA-256 `c2b444a358d246f2ac677a63bfdb6f0c2327e28ca96300836c9d1a2bf47e9095`；实测 960×960 / 24fps / 97 帧 / 容器 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨；四段 Range 均 206 且 ETag 校验一致，MP4 结构 `ftyp/uuid/free/mdat/moov`，全帧解码通过。
- **提交 prompt**：§0.11 英文正文 `f1d9ff62…0d4f` + 39 条负面词 `19bd7829…f9ce` 合并为 `086e7c9c…12a9`，负面词并入同一字段，无独立负面字段。
- **留档**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.request.json`、`enemy1021_dead_v2.download.json`；工作目录 `Library/Locus/tmp/enemy1021_dead_video_v2/`（`job.json`、`runtime.json`、`create_attempted.json`、`download_manifest.json`、`download_parts/`、`contact_sheet.png`、`first_frame.png`、`last_frame.png`、`inspection_frames/`、脚本 `build_job_v2.py` / `generate_dead_video_v2.py` / `query_existing_v2.py` / `poll_and_download_v2.py`）。脚本均已执行，**不要重跑创建**。
- **边界**：只发起、下载、解码与汇报。**未选帧（当时）、未抠图、未对齐、未部署 Unity、未进入 Play、未改任何 Unity 资产；本侧未写或改视频词、不验收画面。**检查帧仅文件诊断，不是选帧。用户随后验收本视频并授权选帧（见上一条）。

### Enemy 1021 持盾死亡：图2 v1/v2 历史 + 重做 v3（2026-10-16，v3 已获用户验收）

- **前置状态**：持盾死亡视频 v1（同时早些时候）已生成并**两轮验收不通过**（task `task_800f25278957424d9c5b911e75bfca2c`，`cost.spend=0.48888`；成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v1.mp4`，2,056,869 bytes、960×960/24fps/97帧）。用户第二轮反馈：“动作毫无张力，没有被击飞到底的感觉”“与1011的dead动画语义完全不符”“姿态僵硬像是被催眠倒下”。代理实测确认根因不在视频词而在目标图2：1011 已验收死亡填充率约 0.30、宽度为 Idle 的 1.45–1.52 倍，而成片末帧填充率反而从 0.620 升到 0.670。用户随后拍板“按 A 方向”= 重做图2。
- **图2 历史版本**：v1 `enemy1021_dead_airborne_keypose_v1.png`（940,772 bytes，SHA-256 `5a13710a…da60`，未通过：整身旋转 46.5°、盾区放大 1.331×/1.234×）；v2 `enemy1021_dead_airborne_keypose_v2.png`（1,072,848 bytes，SHA-256 `f6073b06…2ac9`，用户验收通过，但为“垂头失重、身形站桩”，主轴仅 28.4°——正是导致视频僵硬的来源）。两者均保留追溯，不再提交。
- **v3 生图规格**：`POST /v1/images/edits`；`gpt-image-2.5-sunburst` / `quality=max` / `input_fidelity=high` / `1024x1024` / `background=opaque` / `output_format=png` / `n=1`；唯一参考图1 `enemy1021_shield_hold_v3_green_v1.png`（SHA-256 `d398a1eb…40de`）；英文正文与 40 条负面词合并进同一 `prompt` 字段，未使用独立负面字段。已按 §0.10 词提交**一次**，无自动重试。
- **v3 结果**：HTTP 200、POST 108.071 秒；usage prompt/completion/total = 667/946/1613，`cost` 与 task id **未返回**（不推断费用）。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v3.png`，1,122,630 bytes、SHA-256 `dba1290c0b78b3e4df861fe63f53b2e0d83c973ba133a5587f87fe2f84669785`、1024×1024 RGB、ColorType2、解码通过。实际提交合并 prompt SHA-256 `eb06952c4725d02ed4d0c89d30766f4a91360409cfab1a5f17a1924b07f91140`（英文正文 `721e7814…587c` + 负面词 `e55a48fe…92ca`）。同名 `.request.json` / `.response.json` / `.result.json` / `.submitted_once.json` 已落盘。
- **v3 四角背景实测**：约 `(1.6, 181.1, 72.4)`，**不是**图1 的精确 `(0,177,64)`，与 v2 的 `(2,178,68)` 同类；若后续进视频，仍需按 G1 另存同色绿幕上传副本。
- **v3 客观测量（与 1011 同口径；仅辅助，不代替用户目检）**：全身宽高比 **1.60**（要求 >1.4）；填充率 **0.376**、张力 0.624（目标填充率 ≈0.30–0.35；1011 dead1/3/6 为 0.345/0.257/0.256）；主体分为 **2 个连通块**，身体块 `[318,244,924,765]` 与盾块 `[90,317,338,627]` 最小间距 **14 px**（脱手成立）；盾像素面积约为图1 的 **0.64**，面积换算线性尺度约 **0.80**（偏小而非放大）；身体块主轴 **79.8°**（落于目标 50°–80°；1011 dead3 为 71.5°、dead6 为 81.6°；v2 身体倾角为 28.5°）；四边最小留白 **8.8% / 9.7%**（左/右，未裁切但略低于词中 ≥10%）。
- **留档**：审计目录 `Library/Locus/tmp/enemy1021_dead_keypose_v3/`，含 `KEYPOSE_V3_SOURCE_COMPARE.png`（图1/v1/v2/v3 同画布 50% 并排）、`KEYPOSE_V3_VS_1011_DEAD.png`、`SHIELD_WINDOW_100PCT_COMPARE.png`、`HEAD_100PCT_COMPARE.png`，数据 `v3_metrics.json`、`v3_body_metrics.json`、`v3_shield_oriented.json`、`v3_gap_check.json`，脚本 `build_jobs_v3.py`、`generate_dead_keypose_v3.py`、`measure_v3.py`、`body_metrics_v3.py`、`shield_oriented_v3.py`、`gap_check_v3.py`。暖色木材分类受皮革/靴子同色干扰，已用窗口回避并在报告中标明为代理指标。
- **边界**：本次只生成关键姿态并做静态量测。未抠图、未对齐、未改任何 Unity 资产、未进入 Play 或运行自检；**未写新视频词、未提交视频**。图2 v3 通过用户目检后，视频才改用图1＋v3 作为两张普通 `reference_image` 并另行审批视频词。
- **用户验收**：用户于 2026-10-16 明确“**已验收该图片**”，v3 成为持盾死亡末帧锚点；代理量测出的三项达标边沿（左右留白 8.8%/9.7%、填充率 0.376、盾面积 0.64×）均视为已接受，不重发。下一步由负责写词的对话以图1＋v3 起草新死亡视频词（§0.8 旧词作废）；本侧不写词、不验收图。

### Enemy 1021 胆怯击飞 CowardLaunched 视频 v1（2026-10-18，已提交一次；**验收不通过**：凭空出现 1011 的剑）

- **授权**：用户 2026-10-18 指令“根据交接文档发起视频生成”，按 `plan/enemy1021-art-production-handoff.md` §0.18 原文只提交一次；词、参数、输出路径均未改。
- **图2 缺口与替代（用户已批准）**：§0.18 要求从 `enemy1011_launch_rise_v1.mp4` 重抽 1-based f056，但该视频与 `Library/Locus/tmp/sword_enemy_launch_v1/` 在本机都不存在（全盘搜索 mp4/帧目录为 0）。替代源为 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_rise4.png`——`plan/enemy1011-frame-provenance.csv` 登记为 f056 @2.292s、本地绿幕键、补边 (74,120)、逐像素一致。已另存 1024×1024 RGB 绿幕副本 `Library/Locus/tmp/enemy1021_coward_launched_video_v1/02_1011_rise4_f056_green_00B140_1024.png`（角色不缩放，主体 791×520 与源相同，四角精确 (0,177,64)，最小留边 116px，SHA-256 `8772afd16c48cb5b4d314d3a988b2e82e611cac32ac7c62197d6021bf9c25b3d`）。
- **图1**：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`，SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`，四角精确 (0,177,64)。
- **词指纹**：§0.18 英文正文 `f40787301c5595995a43140f37b8323723c319e143a94e39a66d20bf1e6a144a` ＋ 负面词 `abfbbffa069bd844ae24e074c834cf09642fa104316073ae4d3dbabbc457a6e9`，合并 `e7bc74b09957eb76e39afbddb0e14109c0cc3d3c88aed266c109353bf897bfc8`；脚本按文档逐字提取后先校验哈希再上传。
- **Task**：`task_99b97a3345fc4606a3df4aa11f796522`；create HTTP 200，两次上传均 200，`create_attempts=1`，无重试；usage completion 130,500，`cost.spend=0.7308`（币种未注明）。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launched_v1.mp4`，3,087,094 bytes、SHA-256 `1037d2bec320b830a4d0c72fc4b4489d5d73bc4daf1cd4fcfd1d1d10f3496afd`；960×960 / 24fps / 145 帧 / 容器 6.041667 秒 / 末帧 PTS 6.0 / H.264 / 无音轨；四段 Range 均 206 且 ETag 一致，全帧解码通过。
- **与词的客观对差**：① 动作在 f029（1.208s）才开始，f000–f028（0–1.17s）完全是待机姿态（298×410、底边 703），词要求 0.08–0.30s 起跳、0.55s 到位；② f033（1.375s）后姿态冻结到 f144（6.0s），相邻帧差异 ≤约 60 px，等于 4.6 秒稳定保持，符合“不下落、不翻滚、不二次受击”；③ 末帧包围盒 742×488、主轴距水平 4.6°（躯干接近水平），宽度为待机的 2.49 倍；④ 末帧最低带只有 57px 宽（x 606–662，y 699–723），上一带 24px 高却 412px 宽——该窄突出后续量测确认为**剑尖**（不是靴尖），见文末可复用教训。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_launched_video_v1/` 的 `LAUNCHED_V1_EARLY_f000_f025.png`、`LAUNCHED_V1_TRANSITION_f026_f038.png`（全片唯一的运动段）、`LAUNCHED_V1_FINAL_POSE_MARKED.png`、`LAUNCHED_V1_FINAL_LOWEST_ZOOM.png`、`LAUNCHED_V1_LATE_SAMPLES.png`、`objective_metrics.json`、`pose_detail.json`、`first_frame.png`、`last_frame.png`、`frames_all/`（145 张原帧）。
- **边界**：未选帧、未抠图、未对齐、未补边、未改任何 Unity 资产、未进入 Play Mode。脚本 `generate_video.py` 已执行，**不要重跑创建**。
- **验收结果（2026-10-18）：不通过**。用户原话要点：① 凭空出现了 1011 的剑；② 完全没有按 1021 的胆怯造型制作击飞。成片留作失败样本，不复用、不抽帧。
- **可复用教训**：带武器的姿态参考图会把武器复制进成片，负面词 `no sword` / `no weapon` 扣不住；武器在剪影上表现为“宽带之后突降的 45–60px 窄突出”。图2 用了 `Enemy_1011_rise4.png` 的绿幕副本（参考最后两档 60px→ 45px，x 646–705），成片末帧同样在 394px 之后降为 57px→46px（x 606–662）；正常待机底部是 248px 连续靴带。今后角色动画参考图必须是**无武器、无盾**的自身帧。证据 `Library/Locus/tmp/enemy1021_coward_launched_video_v1/fail_analysis_sword_protrusion.json`。

### Enemy 1021 胆怯击飞完整链（单图 / 10 秒）v2（2026-10-18，已提交一次、下载并全帧解码；源片与 16 帧均已验收）

- **依据**：`plan/enemy1021-coward-launch-full-cycle-10s-preparation.md` §6 + §8 逐字提交（正文 `6cfea968…6bc5`、负面 `68aad0e5…3507`、合并 `fccc7c2c…ec11`），唯一参考图 1021 无盾胆怯 Idle（`3fd64c93…a6c0`）；无 1011、无额外姿态图、无 first/last frame。
- **Task**：`task_78a8dfe06b3849be8cbd429c8964b706`；上传/创建均 HTTP 200，`create_attempts=1`；usage 216,900，`cost.spend=1.21464`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launch_fullcycle_v2_10s.mp4`，3,859,668 bytes、SHA-256 `cef48f8e…9cd7`；960×960 / 24fps / 241 帧 / 10.041667 秒 / 无音轨；四段 Range 与全帧解码通过。
- **客观实测**：241 帧中 **174 帧处于 3–10 帧长的静止平台**，另有 **33 次 ≥30k px 的单帧换图**（最大 107,112 px，f079→f080），典型为“静止 4 帧 → 一帧 71k–102k px 换图 → 再静止”；阶段弧线存在（idle→离地 1.50s→空中 2.5–3.7s→背部平躺 3.9–5.9s→起身 6.0–7.5s→idle 8.0–10.0s），首末帧尺寸与参考 idle 一致（299×408 / 298×412，底边 703）。
- **背景色**：四角均值约 `(1,160,61)`，与请求 `(0,177,64)` 不同（最大通道差 21）；后续抠图必须自动取键。主体均值色最大偏移 22.5。
- **结论**：用户看片后反馈“效果挺好啊”，本片转为**已认可的动作素材源片**，不再归为废片；停格结构改作选帧依据（取平台内部帧）。
- **抠图（2026-10-18，T=80）**：用户剔除起飞前待机与回 idle 两帧后剩 **16 帧**，段内重编为 rise1..5 / fall1..6 / getup1..5。SVL Chroma 自动取键（实测 `(2,158,59)`）、容差 80 / softness 16 / halo 1 / alpha-aware despill / 半透明转不透明（cutoff 128），无缩放/对齐/补边。结果：半透明 **0**、透明区非零 RGB **0**、幕布残留 **0**、内部改动 6 px（maxΔ7）；遗留 `g−max(r,b)>8` 为 197–524 px/帧（合计 5,380），属 SVL 去溢色边界，待白底/深底目视确认。交付 `Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/keyed_t80_v1/`（16 张 960×960 RGBA）；报告 `matte_report_v1.json`；评审 `MATTE_16_WHITE_v1.png`、`MATTE_16_DARK_v1.png`、`MATTE_16_CHECKER_v1.png` 与三个 `*_MATTED_PREVIEW_v1.gif`。未对齐、未补边、未建 Clip、未改 Unity。**用户 2026-10-18 已验收源片与 16 帧**；可复用经验见 `memory/enemy1021-coward-launch-recipe.md`。

### Enemy 1021 胆怯死亡 CowardDead 源片 v1（10 秒 / 单图）（2026-10-18，已提交一次、下载并全帧解码；选帧待验收）

- **依据**：`plan/enemy1021-coward-dead-10s-video-preparation.md` §4 + §6 逐字提交（正文 `1868d8f6…0b2b`、负面 `12fcd757…a741`、合并 `9930871c…d564`）；唯一参考图 1021 无盾胆怯 Idle（`3fd64c93…a6c0`），无其他图片/视频。
- **Task**：`task_64938fa46e21459eace6e2109afc62ce`；上传/创建均 HTTP 200，`create_attempts=1`；usage 216,900，`cost.spend=1.21464`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_dead_v1_10s.mp4`，4,864,912 bytes、SHA-256 `5c9d7355…d28a`；960×960 / 24fps / 241 帧 / 10.041667 秒 / 无音轨；四段 Range 与全帧解码通过。
- **结构实测**：f000–f047（0–1.96 秒）纯 idle（299–300×410、底边 703）；f048–f155（2.00–6.46 秒）逐帧连续变化（帧间中位数 3,284 px，最大 90,438）；f156–f240（6.50–10.00 秒）85 帧完全冻结（607×526、底边 787）。底边全程单调下降 703→787，无向上腾空。背景四角相对 `(0,177,64)` 偏 20.8，相对项目基 `(0,159,62)` ≤4.8；主体颜色漂移仅 5.4。
- **选帧（2026-10-18，待验收）**：跳过纯 idle 与冻结段重复，按真实不同姿态取 **8 帧**（dead1..8 = f049/f057/f072/f089/f104/f133/f146/f160），相邻帧差异 80,365–160,246 px。素材 `Library/Locus/tmp/enemy1021_coward_dead_video_v1_10s/selected_dead_v1/`（960×960 RGB、未抠图），清单 `selected_dead_v1.json`，评审 `DEAD_SELECTED_REVIEW_v1.png` / `DEAD_SELECTED_PREVIEW_v1.gif`。
- **未做**：未抠图、未补边、未建 Clip、未改 Unity；`generate_video.py` 已执行，不得重跑创建。

### Enemy109 骑手落马：连续受击后仰离鞍（2026-10-15；视频/12帧/透明版均已验收）

**当前交付状态：视频①、12帧选帧、T79透明版均已用户验收；未部署 Unity。下一步为视频②无骑手马左向逃跑。** 正式入口见 `plan/enemy109-rider-fall-mount-escape-handoff-20261015.md` §0.5。

- **背景**：用户将“人马一起倒地死亡”改为“骑手受击摔落，马存活、向画面左侧跑离并渐隐”。v1静态姿态被否决（“马匹没有惊慌感，角色倒地姿势应该后仰身而不是躺下”）；v2“马受惊＋骑手后仰失衡”与v3“倒地尾姿”均获验收。
- **视规则**：`seedance-2-fast`，6秒 / 720p / 1:1，无音频、无水印、seed=-1。三张**普通 `reference_image`**按“正常骑乘 → v2后仰 → v3倒地”顺序提交，未混用 `first_frame`/`last_frame`，无自创 `middle_frame` 或时间戳字段。普通参考不保证精确首尾帧。
- **输入准备**：三张派生上传副本统一1152×1152、RGB(0,159,62)，每边补64px；起始图1449、v2为25、v3为27个外轮廓绿边像素被中性化；原图未改。
- **Task ID**：`task_393dfe84427642639b163b4d499cfa26`；创建HTTP200、仅1次、无自动重试；服务端 `2026-10-15T09:05:23.879836+00:00` 返回 success。usage 130500/130500，`cost.spend=0.7308`（币种未返回）。
- **成片**：`C:/Users/Administrator/Videos/doubaoVideo/enemy109_rider_fall_v1_720p_6s.mp4`，3,810,739 bytes，SHA-256 `381b184bfeaf3064436c681b3f28b3e7222631170d3e362d4ac002066c01ac9d`；960×960、24fps、145帧、约6.04s、无音轨；容器与全量解码通过。用户已验收。
- **选帧**：`Enemy109ArtSource/03_SelectedFrames/RiderFall_12Frames_v1/raw/`，源帧号（1-based）`001,010,018,026,034,042,050,058,066,074,086,107`；尾帧f107（约4.417s），省略后续运动量降至2%–13%的重复保持段。12张均为真实解码帧，独立重解码像素一致；用户已验收。
- **拨图**：`.../keyed_ui_t79/`，12张960×960 RGBA。实输出绿幕为漂移绿`(0,148,59)`，按手册扫描T55/70/79/100后选 **T79**（被删主体16.6%、幕布残留0、绿>8 494），与109已验收区间及本角色已部署t79/t80一致；T100被删升至18.5%不取。配方：SVL Chroma / 逐帧auto-key / T79 / softness16 / halo1 / alpha-aware despill / 半透明转不透明；无平滑、无额外描边替色、无全图调色。同配方回归处理已部署 MountedIdle1 输出逐像素相同。用户已验收。
- **硬门**：半透明0、透明区隐藏RGB0、保护核心被删0、4px边缘Alpha前景0；白底绿优势>8为357–596，低于109 MountedIdle t79基线的1086–1458。
- **边界**：未部署Unity，未做1108补边、锚点对齐、Clip或死亡视觉路由；骑手与马的拆分属视频②。骑手落马死亡演出接入属独立授权项（`CavalryVisualController.LateUpdate()`在 `Dead` 时直接返回；`Enemy.Die()`自带弹起旋转，不能与已烘焙落马叠加）。

### Enemy 1051 黄巾弓手身份定妆图 v2（2026-10-18，三次提交 + 一次透明探针；**验收不通过**）

- **依据**：`plan/enemy1051-identity-generation-handoff.md` §6+§7；单图编辑底图为已认可的 v1（SHA `592262c1…b83f`）；`gpt-image-2.5-sunburst / 1024x1024 / quality=max / input_fidelity=high / background=transparent / png / n=1`，每次只提交一次、无重试。
- **三次结果（全为 RGB，背景是绘入的棋盘格）**：v2 1,181,804 bytes（16px 棋盘 254/208）；v2b 1,617,159 bytes（8–9px 棋盘 223/172，同词重生）；v2c 1,536,103 bytes（7–9px 棋盘 218/233，压黄+透明补句）。三次均 Color Type **2**、透明 0 px。
- **透明探针**：同路由同参数、极简去背短词 → **Color Type 6（RGBA）**，透明 650,444 px、半透明 398,132、alpha 0–254。证明本机路由支持 alpha，失败来自“长服装编辑词”。
- **用户验收**：“这两张图的士兵都黄了…头巾还是黄的，非常诡异”“验收不通过”。黄系高饱和像素从 4.80%（v2）降到 2.20%（v2c）仍不可接受。
- **待拍板**：头巾处理（缩小/改色/只留小段）、布料主色是否给低饱和色值区间、透明路线（二段式语义去背 vs 本地双键去背）、是否改用短词换透明度。未拍板前不提第 4 次。
- **文件**：`C:\Users\steam\Pictures\gptGen\enemy1051_identity_v2\`（v2/v2b/v2c + 探针，各带 `.request.json` / `.response.json` / `.status.json`）；脚本在 `Library/Locus/tmp/enemy1051_identity_v2/`。v1 仍是唯一获认可的身份图。

### Enemy 1051 黄巾弓手 v3 阶段A（2026-10-18，已提交一次；**已获用户验收，去背搁置**）

- **方案**：`plan/enemy1051-identity-generation-handoff.md` §4 阶段A + §5A；**不要求透明**，改用不透明纯绿制作底。
- **参数**：`gpt-image-2.5-sunburst / 1024x1024 / quality=max / input_fidelity=high / background=opaque / png / n=1`；唯一参考图 v1（SHA `592262c1…b83f`）；词 = §5A 正文 1,743 字符 + 负面 287 字符（合并 `f50145f6…0ef1`）。
- **结果**：HTTP 200、42.61 秒、usage in 442 / out 7,024；`C:\Users\steam\Pictures\gptGen\enemy1051_identity_v3\enemy1051_identity_stageA.png`，1,088,256 bytes、SHA-256 `553f30c6…88a0`、Color Type 2（RGB，符合阶段A预期）、alpha 全 255。
- **实测**：四角制作底中位数 `(1,163,67)/(2,163,66)/(1,163,66)/(1,163,66)`、std ≤1.03、边框游程 1024（无棋盘格），相对 `(0,159,62)` 偏差 ≤4；主体 528×776，留白 左27.54/上12.01/右20.90/下12.21%；黄系高饱和像素仅 **0.26%**（v2 4.80%、v2c 2.20%）。
- **验收**：用户“已验收，暂不进行去背”。阶段B（语义去背）**搁置**，输入必须为本阶段A 原图；去背/1108 补边/Clip/Prefab/动作/Unity 均未做。

### Enemy 1051 Idle 6 秒源片 + 6 帧循环帧（2026-10-18，已提交一次；**均获用户验收，去背暂缓**）

- **依据**：`plan/enemy1051-idle-6s-video-preparation.md` §5+§7 逐字提交，合并词 `8c5fff0c…0830` 与文档记录一致后才上传；输入为归一绿幕副本 `01_idle_v3A_green_009F3E_1024.png`（445,447 bytes、SHA `64a0f777…a4fc`、四角精确 `(0,159,62)`），**同一 URL 兼作 `first_frame` 与 `last_frame`**。
- **参数/Task**：`seedance-2-fast / 6s / 720p / 1:1 / 无音频无水印 / seed=-1`；`task_30fdb0d6265a44729d272a4d88c30268`，`create_attempts=1`，usage 130,500，`cost.spend=0.7308`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1051_idle_v1_6s.mp4`，2,993,431 bytes、SHA-256 `eb5f564d…e204`；960×960 / 24fps / 145 帧 / 6.041667 秒 / 无音轨，全帧解码通过。
- **实测**：四角均值首帧 `(0,152,55)`、末帧 `(0,150,56)`，相对上传键 `(0,159,62)` 最大通道偏差 17（平均 14.09）；帧间改动中位数 2,421 px，144 对里 20 对 <200 px；剪影面积 193,570–195,925（±0.6%）、高 726–738 —— **无整体缩放**；首尾差异 9,350 px < 半周期 13,144 px。
- **选帧（已验收）**：按主体顶边（静止 115–116 / 峰值 104）取一个周期 **f006→f084** 的 6 帧：`idle1 f003(116) / idle2 f012(110) / idle3 f021(106) / idle4 f030(104) / idle5 f045(108) / idle6 f060(115)`；相邻差异 68,656–124,948 px，**循环回接 idle6→idle1 仅 13,439 px**（全组最小）。
- **交付**：`Library/Locus/tmp/enemy1051_idle_video_v1_6s/selected_idle_6/`（`idle1..6.png`，960×960 RGB，未抠图）；清单 `selected_idle_6.json`；评审 `IDLE_SELECTED_6_REVIEW.png` / `IDLE_SELECTED_6_PREVIEW.gif`。
- **未做**：未抠图（用户“先不要去背”）、未补边 1108、未建 Clip、未改 Prefab/旧105、未进 Unity。

### Enemy 1051 射箭 10 秒视频 v1（2026-10-18，已提交一次；**验收不通过**）

- **依据**：`plan/enemy1051-attack-10s-video-preparation.md` §5+§7 逐字提交（正文 5,084 字符 + 负面 578 字符，合并 `b0ba9f80…d11b`）；唯一普通 `reference_image` = 归一绿幕身份副本 `01_idle_v3A_green_009F3E_1024.png`（SHA `64a0f777…a4fc`），未用首尾帧、未传旧 105 图。
- **参数/Task**：`seedance-2-fast / 10s / 720p / 1:1 / 无音频无水印 / seed=-1`；`task_f344527f846d42488ec7b7e7e1a76b7b`，`create_attempts=1`，usage 216,900，`cost.spend=1.21464`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1051_attack_v1_10s.mp4`，4,360,193 bytes、SHA-256 `1e01ce16…fa0766`；960×960 / 24fps / 241 帧 / 10.041667 秒 / 无音轨，全帧解码通过。
- **实测**：背景四角 `(0,147,62)`→`(0,146,63)`，相对键 `(0,159,62)` 最大偏差 14.1（无棋盘格）；宽度 494→806、高度 657–831、面积 ±23.4%，**极限帧上边留白仅 1.1%**（低于 ≥10% 安全边）；帧间改动中位数 1,414 px，61/240 对 <200 px；>60k px 变化集中在 f055–f057 与 f201–f203。
- **用户验收**：“箭矢拿反了”“射击姿态也与 105 完全不一致”“这版验收不通过”。归因：v1 词只写了`朝玩家/透视缩短`，未把箭尾锁定到弦、箭头锁定到镜头；且未锁定 105 的手位/弓位/躯干构图，与“不上传旧 105”矛盾。
- **待拍板（未拍板不提交第 2 次）**：是否允许上传旧 105 三图仅作姿态参考；箭矢朝向硬约束写法；满弓/松弦可读帧时长；10 秒是否缩短。
- **未做**：未选帧、未抠图、未补边、未建 Clip、未改旧 105/Prefab、未进 Unity。

### 1011 风格锁定刀兵 v5 换人视频（2026-10-24，已提交一次；**待用户验收**）

- **用途**：把用户提供的绿幕动画源片里的人物换成 v5 刀兵立绘，保留原动作与镜头。源片 `C:/Users/steam/Videos/doubaoVideo/2b1888f6388ed56b14da731eb0aeb229.mp4`，960×960 / 24fps / 97 帧 / 4.042 秒，实测键色 G 161–169。
- **输入职责**：`reference_video` = 源片（动作＋镜头）；`reference_image` = `C:/Users/steam/Pictures/gptGen/enemy1011_style_locked_dao_v5/enemy1011_style_locked_dao_v5.png`（SHA `8330ac18…09d6`，身份与服饰）。未用首尾帧。
- **参数/Task**：`seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1`；`task_106a6af73b374bbebad8ea94115088b8`，`create_attempts=1`，usage 173,700，`cost.spend=0.57321`（billing 条件 `has_video_input=true`、`720p`）。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v1_4s_720p.mp4`，2,082,134 bytes、SHA-256 `dcf13f43…fa82`；960×960 / 24fps / 97 帧 / 4.042 秒 / h264，无音轨，全帧解码通过。
- **实测（对照源片逐帧）**：帧数、分辨率、时长与源片一致。背景四角中位 G 由首帧 154 降到 132–134 并稳定，相对基准键 `(0,159,62)` 为 ΔG −5…−27（最差 f096），相对源片自身键 (0,161…169) 为 −27…−37。主体质心与源片平均差 18 px、最大 71 px（f049），首尾帧差 ≤8 px。**主体高度普遍比源片大 20–25%**（首帧 745 vs 593，f072 719 vs 579）。
- **待拍板**：① 背景 G 偏低 27–37 是否接受／改参考重生成／只做交付物侧处理（按本文档 G2 第 6 条升级，不得静默结案）；② 主体放大 20–25% 是否影响逐帧对齐。**已于 2026-10-24 裁决为两项一并修**，修法见 `plan/enemy1011-style-locked-dao-generation-handoff.md` §16（v2，A 方案仅提示词，词已备好待发起）。
- **复核诊断（2026-10-24，重要）**：① 成片脸部其实已换成 v5 露脸怒视，不是头盔无脸，问题是**嘴 97 帧全闭、脸像静态贴图**；② `Library/Locus/tmp/dao_v5_repl_video/frame_000/024/048/072/096.png` 与 `contact_sheet.png` **全部是源片帧**（与源片逐像素差 0.00，与成片差 62–78），此前据此评审会误判为“头盔无表情”，**评审必须从成片 mp4 重新抽帧**；③ 源片本身就是 16 姿态定格保持动画（相邻帧近重复对 50/96、平均 MAD 3.16，与成片 43/96、3.16 一致），“凑数跳帧”是源片继承而非模型偷懒；④ 脸只在举刀段可见（皮肤占比 f35 0.19），劈砍段 f49–f72 塌到 0.025 被头盔挡死，表情峰值只能落在举刀段顶刀点；⑤ 主体放大与脚底下移同源（源片主体高占比 0.680／底边 Y 776 → 成片 0.798／860），一条约束可同修。
- **接口坑（可复用）**：① `file.reachapi.ai/file/uploads` 前置 Cloudflare 会以 `HTTP 403 error code 1010` 拒绝 `Python-urllib` 的 UA，必须带浏览器 UA；② `GET /v1/tasks/{id}` 的 `data` 是**数组** `[{url, duration_seconds}]`，按 dict 解析会抛 `AttributeError: 'list' object has no attribute 'get'`，会把进行中的 generating 误判成状态未知。
- **未做**：未选帧、未抠图、未补边、未建 Clip、未进 Unity。

### 1011 v5 刀兵换人视频 v2（2026-10-24 14:39 已发起一次，**待用户目视验收**）

- **用途**：v1 脸部为静态贴图（嘴 97 帧全闭）。v2 为 **A 方案单变量对照**：模型/参数/两个输入文件全部与 v1 字节一致，**只换提示词**（新增举刀段表情阶梯 + 尺寸/脚底约束 + 背景键值写死）。方案与提交文本见 `plan/enemy1011-style-locked-dao-generation-handoff.md` §16。
- **输入职责**：`reference_video` = 源片 `2b1888f…mp4`（SHA `f624106031c57b…`，动作与镜头）；`reference_image` = v5 立绘（SHA `8330ac18…09d6`，身份与服装）。均与 v1 相同字节。
- **参数/Task**：`seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1`；`task_7ef0c06849df431a819e5b62038fab3f`，`create_attempts=1`，prompt 2951 字符 SHA `b21d8e54…6a32`，usage 173,700，`cost.spend=0.57321`（与 v1 同量级）。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v2_expr_4s_720p.mp4`，2,129,352 B、SHA-256 `e9fae4ec73234e83eea7ec535ac453f1570cff7190e604f281f546b65c177747`；960×960 / 24fps / 97 帧 / 4.042 s / h264，无音轨，全帧解码通过。v1 文件未动。
- **轮询**：首轮 12 次耗尽本地 120 s 预算仍 `generating`（本地预算耗尽 ≠ 服务端失败）；续查同一 task 第 5 次拿到 `success`（观察等待 44.61 s），下载 157.93 s，ftyp/box/SHA256 均通过后原子改名。
- **背景色 G2（本侧验收）**：**未修复**。f000 `(0,139,63)` ΔG −20、f024 `(3,135,64)` ΔG −24、f096 `(1,133,64)` ΔG −26；f024–f096 ΔG 范围 −26…−24，与 v1 的 −27…−24 **同向同量级**；四角 G std mean 1.43，无棋盘格。词里写死 `RGB(0,159,62)` + `flat and even` **不足以约束输出键值**，再次印证“输入已统一 ≠ 输出已统一”。按 G2 第 6 条升级为待用户决策项（接受／换参考重生成／只做交付物侧处理）。
- **参考（非本侧验收项）**：主体尺寸几乎未变——身高比 out/src 均值 1.182（v1 1.216）、主体底边 Y 偏差均值 +69.2px（v1 +68.5）。§16.3 第 3 段的尺寸约束本次同样未生效。
- **表情/动作/身份/尺寸由用户目视验收**，本侧不代为判定。已从成片 mp4 重抽 16 姿态槽位帧 `Library/Locus/tmp/dao_v5_repl_video/v2_slot_frames/`（`v2_attack01_f010.png`…`v2_attack16_f079.png`），对照图 `v2_face_16slots.png` / `v2_body_16slots.png`。
- **可复用坑（新增）**：① 本项目 v1 的 `frame_*.png` 与 `contact_sheet.png` **全是源片帧**（与源片逐像素差 0.00，与成片差 62–78），评审必须从成片 mp4 重抽；② 源片本身是 16 姿态定格保持动画（97 帧按 24fps 放长），相邻帧近重复对 50/96、平均 MAD 3.16，成片 43/96、3.16 一致——“凑数跳帧”是源片继承而非模型偷懒；③ 脸只在举刀段可见（皮肤占比 f35 0.19），劈砍段 f49–f72 塔到 0.025 被头盔挡死，表情峰值只能落在顶刀点。

### 1011 v5 刀兵换人视频 v3（2026-10-24 15:40 已发起一次，**待用户目视验收**）

- **用途**：v2 的怒吼过于突出（整口张开 + 劈砍全程保持）。v3 只改 P4 表情段（窄幅无声呲牙、只在顶刀点一瞬、劈砍段闭口），其余五段与参数、两个输入全部与 v2 字节一致。方案与文本见 `plan/enemy1011-style-locked-dao-generation-handoff.md` §17。
- **参数/Task**：`seedance-2-fast / 4s / 720p / 1:1 / 无音频无水印 / seed=-1`；`task_3dc63c400b6f4fefadae1c8615a8dcf3`，`create_attempts=1`，usage 173,700，`cost.spend=0.57321`（与 v1/v2 同量级，计费条件 `has_video_input=true`、`720p`）。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/dao_v5_motion_replacement_v3_snarl_4s_720p.mp4`，2,116,632 B、SHA-256 `dd79be1b99b775060f9cf38bcaab1ce559e120220a2fc06179f16a10681871b3`；960×960 / 24fps / 97 帧 / 4.042 s / h264，无音轨，ftyp/box/全帧解码通过。
- **轮询**：首轮 12 次（≤120 s）仍 `generating`；第二轮第 10 次拿到 `success`（共 22 次查询，观察等待 171.8 s），下载 173.93 s。再证“本地预算耗尽 ≠ 服务端失败”。
- **提交文本**：3,447 字符、SHA `bdee94b6…1175`。**新坑**：词文件是 CRLF（3,459 字节，字节哈希 `7b13d70a…7bbf`），直接按原始字节提交会得到 3,458 字符 / SHA `566938c3…ad7a`，与文档口径不符；必须先 CRLF→LF 并去掉末尾单个换行。
- **背景色 G2**：仍未修复。ΔG 全片 −28…−7（f000 `(0,152,60)`、f024 `(2,133,62)`、f096 `(2,131,64)`），与 v1 −27…−5、v2 −26…−19 同向同量级；四角 G std mean 1.50，无棋盘格。
- **开口代理指标（暗红像素，v1 基线）**：峰值 f41 **+5,081**（v2 +4,176，高 21.7%）；超阈窗口（Δ>300）v3 = f14–f49（35 帧），v2 = f29–f68（29 帧）；起手段 f0–f28 最大 |Δ| v3 = 1,603（f28），v2 = 501。即本次并未把嘴的权重降下来，反而峰值更高、窗口更长且提前。本侧只报数，不出合格结论。
- **经验**：同一句“收窄幅度”的自然语言约束不能假设模型会照做；v2→v3 的实测差异说明表情幅度对词敏感但方向不可控，需要按 §17.7 走升级路径而不是重采样。
- **未做**：未做正式选帧、抠图、补边、Clip、Unity 接入。可目视参考：`Library/Locus/tmp/dao_v5_repl_video/v3_slot_frames/`（16 槽位帧）、`v3_vs_v2_face_peak.png`（行 = v1/v2/v3，列 = f28/f35/f41/f47/f48）。

### 1011 v11 形象重做：idle 已部署、walk 三代实测（2026-10-24）

- **idle**：`task_7340f7c493d144a0a5cd5b5ad2d2c000`，`cost.spend=0.57321`，成片 `dao_v11_idle_v1_4s_720p.mp4` 2,075,774 B、SHA `c1d8adbc…7c78`，960×960 / 24fps / 97 帧。源片 `sword_enemy_v4_combat_idle_loop_v1_720p_4s.mp4` 为**灰底**（四角 `(45,44,49)`），词里要求换成绿。背景 G2：相对 `(0,159,62)` ΔG −21…−13（最差 f030 `(0,138,58)`），与 attack 的 −18 同向同量级；成片底色偏低不自动校色，交付侧按每帧实测键色抠图（T=79 / softness 16 / halo 1）规避。6 帧已覆写 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1..6.png`（`.meta` 未动），旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_idle_20261024/`；**用户已验收**。
- **walk v1**：`task_ddd396265a014eb3ac2e67fd48e5e586`，`cost.spend=0.57321`，成片 `dao_v11_walk_v1_4s_720p.mp4` 2,495,659 B、SHA `8fd620c2…0075`。源片 `sword_enemy_walk_front_v4.mp4` 自带绿底但 G≈160–172（非批次键 159）；实测节拍 **f0–9 静立 / f10–88 步态 / f89–96 回静立**，步幅约 10.6 帧（是跑不是走）。背景 G2：相对 `(0,138,59)` ΔG −15…−9（最差 f005 `(0,123,56)`）。**用户判定：面部毫无表情变化。**
- **walk v2**（v1 成片 + 身份图，词为“只改脸”）：`task_9054134cea304484b5d549d6b41b912c`，`cost.spend=0.57321`，成片 `dao_v11_walk_v2_face_4s_720p.mp4` 2,804,704 B、SHA `b534fb14…7c1b`。**结果：嘴在静立帧张开**（词里写了“下颌松开”），即增量指令被反向执行；背景更差，ΔG 最差 −23。
- **walk v3**（v2 成片、无身份图）：`task_354cd2e43052416f9f44554445fee52d`，`cost.spend=0.57321`，成片 `dao_v11_walk_v3_face_4s_720p.mp4` 2,998,103 B、SHA `a240ead9…9ce6`。**结果：嘴全程闭住，但脸仍无变化；第 6 帧刀位漂移。**
- **walk v4**（回到原始源片 + 原始身份图，单次提交，词里把表情写成跟随步幅的时间性表演）：`task_4581124685744599949ec7f2999e480e`，`cost.spend=0.57321`，成片 `dao_v11_walk_v4_4s_720p.mp4` 2,668,215 B、SHA `7bfa4a59…98cb`；960×960 / 24fps / 97 帧。**中段帧边缘锐度 3.91 / 4.14，回到源头水平以上**（v1 3.71 / v2 3.33 / v3 3.14），证明“一次生成”能保住画质。**用户判定：脸仍不如 v1，放弃表情路线。**
- **walk 最终采用 v1**：`dao_v11_walk_v1_4s_720p.mp4` 的 f22/f24/f26/f28/f30/f32 抠图缩放后覆写 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_walk1..6.png`，`.meta` 未动，旧帧备份 `Library/Locus/tmp/backup_Enemy1_1_walk_20261024/`，**用户已验收**。选帧法：按旧 walk 的脚底线轮廓 `875, 906, 884, 874, 905, 893` 与刀位 `345, 341, 222, 143, 132, 304` 反查同构帧，约束最小间隔 2 帧、跨度一个步幅（v1 步幅 10.5–11 帧）。缩放 k=0.92561，逐槽钉旧脚底线，新高 `620, 633, 622, 621, 626, 625` 对旧 `619, 638, 618, 616, 639, 615`。
- **踩坑：左/右半身分割测腿相位会被横扫的刀污染**（测到 691 的其实是刀尖不是脚）。改用脚底线（主体最低行）起伏 + 目视腿部放大图判相位。
- **功能损失（比清晰度更致命）**：v1 无表情 → v2 补不上；v2 张嘴 → v3 闭住嘴但仍无表情。**第二轮只能修改第一轮已画对的部分。**
- **新纪律**：一个动作只允许一次生成；禁止把上一轮成片再当 `reference_video` 叠加重塑。改脸/改底色/改动作一律回到**原始源片 + 原始身份图**重新提交一次。表情必须写进**第一次**提交并写成跟随源片节拍的时间性表演，「只改 X」类增量指令对视频模型无效。已写入 `skill/workflows/character-hit-animation-video-workflow.md` §13 与 `skill/reachapi-seedance-video-generation-v2.md`。
- **重编码源片有代价（实测）**：为统一底色把走路源片另存为平底副本（连通域保护背景、`crf=16`、色彩元数据与原片一致），但角色**内部**像素仍有 56–76% 变化 >2（最大差 27–34），全部来自 H.264 重编码而非染色。**结论：不要为了统一底色重编码源片**，底色靠词约束 + 交付侧按实测键色抠图处理。
- **底色请求值的经验**：词里写 `RGB(0,159,62)` 实测落到 G≈138–146；写 `RGB(0,138,59)` 实测落到 G≈123–129。输出普遍比请求值低 9–21，**请求值越高越接近批次键**。

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
