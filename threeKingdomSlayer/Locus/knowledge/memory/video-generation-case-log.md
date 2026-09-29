---
id: kd_b36a0f8e-c80c-4114-824c-e74466d1d189
injectMode: inherit
summary: AI 视频/图像素材生产的一次性状态台账：Enemy2 受击动画与 Seedance 路线视频的 task id、费用、文件与验收结论，以及 FakeRoute 视频与节点部署状态。
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

### Seedance 路线视频

| 段 | task id | 最终文件 | 大小 | 结论 |
|---|---|---|---|---|
| N3→E3 | `task_db0f2676cf0541929b2bd94255623daf` | `C:/Users/steam/Videos/doubaoVideo/N3toE3_village_supply_yard_480p_4s.mp4` | 3,145,128 bytes | 用户验收 |
| E3→EW | `task_a553daccef564d35b66602bd9d4308ed` | `C:/Users/steam/Videos/doubaoVideo/E3toOuterWoods_480p_4s.mp4` | 2,715,939 bytes | 用户验收，随后部署 Unity |
| EW→J3 | —（用户裁剪拼接） | `Assets/RouteData/FakeStage01/Presentations/Videos/EWtoJ3.mp4` | — | 用户验收；Unity 读取约 7.40 秒；严格首尾帧版本因侧路瞬移被否决 |
| E12→N6 | —（用户提供视频） | `C:/Users/steam/Videos/doubaoVideo/E12toN6_turn_v1_480p_6s.mp4`；Unity副本：`Assets/RouteData/FakeStage01/Presentations/Videos/E12toN6_turn_v1_480p_6s.mp4` | 2,765,523 bytes（源文件） | 用户验收；已接入 `E12 → N6` 出口 `e12_to_n6`；`E12toN6_Turn.asset` 使用 Video、`holdLastFrameAsBackground=true`；N6 `battleBackground=None`，进入战斗继续使用视频尾帧；未改通用 Presenter 算法 |

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
