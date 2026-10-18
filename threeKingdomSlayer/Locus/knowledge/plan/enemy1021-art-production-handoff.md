---
id: kd_190da719-cb35-4537-9560-44d26329b07d
injectMode: inherit
injectAgents:
- unity
summary: 1021最新交接：胆怯死亡CowardDead的单图10秒视频方案已准备，尚未提交；仅用现有无盾Idle，致命冲击后全身展开松脱，保持无支撑死亡倒姿，不落地起身。胆怯击飞完整链源片与16帧透明素材已验收，补边和Unity接入未做。
aiEditMode: inherit
---

# Enemy 1021 美术与动画交接（持盾死亡已验收）

> **用途**：在新对话里直接读本文件即可开工，产出的目标是 **1021 盾兵的正式美术**（先定形象与 Idle，再按批次做动作）。
> **前提**：1021 的机制与代码已在最小可玩版落地（见 `design/enemy-1021-shield-guard.md`）；Idle、Walk、Attack、ShieldGuardHit、HitFlash、HitRight、ShieldBreak 与持盾死亡 Dead 已换为正式素材，其余动作仍复用102占位或缺失，详见§0.2。
> **本文件不定义机制**；机制、状态机、动画需求以 `design/enemy-1021-shield-guard.md` 与 `design/enemy-1021-animation.md` 为准。
> **当前进度（2026-10-18）**：Idle、Walk、Attack、ShieldGuardHit、HitFlash、持盾死亡、`CowardIdle`、`CowardHit` 已验收。**胆怯击飞完整链源片与 16 帧透明素材已验收**（rise1..5 / fall1..6 / getup1..5，T=80），补边 1108、3 个 Clip 与控制器接入尚未做，见 §0.19。**本轮已准备胆怯死亡 `CowardDead` 的单图 10 秒视频生成方案，尚未提交**；完整中英文词、参数、唯一参考图与发起边界见 §0.20 及 `plan/enemy1021-coward-dead-10s-video-preparation.md`。
> **后续部署边界（用户明确要求）**：只做到角色接入第一关并配置对应动画；部署完成后通知用户，由用户开游戏检查和验收。除非用户在对话中明确要求自检动画部署情况，否则不进入运行自检、不自动驱动动画或追加画面检查。该要求优先于下文历史检查流程，见 `memory/user-preference.md`。

---

## 0. 下一对话从这里继续

### 最新停点：胆怯死亡视频方案已准备（单图 10 秒，尚未提交）（2026-10-18）

本节优先于下文所有历史停点。用户在胆怯击飞完整链成功后，要求思考胆怯 `Dead`，随后明确“准备视频生成方案交接”。本轮只完成方案与文档，**没有调用生成、没有 task 或成片、没有新参考图与 Unity 改动**。发起侧入口为 **§0.20**。

已验收的胆怯击飞源片与 16 帧透明素材仍在 **§0.19**，不重做、不重抠，也不因新死亡任务自动部署。旧 §0.18（6 秒双图、只保持倒姿）已被取代且验收不通过，不再提交。

### 0.20 CowardDead · 单图 10 秒视频生成方案交接（已准备，尚未提交）

- **完整方案**：`Locus/knowledge/plan/enemy1021-coward-dead-10s-video-preparation.md`。§4 为英文正文、§5 为完整中文对照、§6 为双语负面词、§7 为请求结构；实际提交只合并 §4 英文正文与 §6 英文负面词。
- **动作内容**：胆怯双掌护身 → 一次致命重击打散防御 → 全身反冲、短暂离地并向后展开 → 头颈/肘腕/膝踝松脱 → 保持无支撑的死亡倒姿。**先有冲击张力、再变死重；不落地、不撑地、不起身、不回 Idle。** 大幅飞行、旋转和掉出屏幕由现有死亡代码完成。
- **唯一上传图**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`。已回读核验：RGB PNG / 1024×1024 / 172,046 bytes / SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`；四角各 64×64 中位 RGB `(0,177,64)`、std=0，原图未改。
- **参数**：`seedance-2-fast / 10s / 720p / 1:1 / 无音频无水印 / seed=-1`；只用单张普通 `reference_image`，不使用首尾帧模式。不准备峰值/尾姿图，不追加 1011、持盾死亡、击飞帧或其他参考图/视频。
- **宽松节奏**：0–0.6s 短 Idle，0.6–2s 致命冲击，2–4.5s 惯性展开，4.5–7s 松脱，7–10s 死亡终态保持。中间动作自由生成，不锁精确关节角度、身体轴角或包围框比。**10 秒是源片，不是玩法时长；不让致命一击变成数秒缓慢推挤。**
- **建议新输出**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_dead_v1_10s.mp4`；建议留档 `Library/Locus/tmp/enemy1021_coward_dead_video_v1_10s/`。准备时这两个路径均不存在，本轮没有生成。
- **验收边界**：观察冲击 → 展开 → 松脱是否有可用姿态；平台可用于选帧，不因“姿态平台 + 换图”结构自动判废，用户决定美术验收。完整解码、绿幕时间线与主体颜色另报；输入恒色不代表输出恒色。
- **授权边界**：用户须另行明确发起后，创建任务才执行一次；不自行重试、改词、换图或降规格。发起侧下载/解码后交用户看视频，不自动选帧、抠图、补边、建 Clip、接入或进入 Play。本轮只改两份交接/方案文档，未改资产与机制。
- **执行结果（2026-10-18，已提交一次、下载并全帧解码；待用户看片）**：task `task_64938fa46e21459eace6e2109afc62ce`，`create_attempts=1`，`cost.spend=1.21464`；成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_dead_v1_10s.mp4`（960×960 / 24fps / 241 帧 / 10.041667 秒 / 无音轨，SHA-256 `5c9d7355…d28a`）。客观：idle 到约 1.88 秒才起力；**f157–f240（6.54–10.00 秒）84 帧完全冻结**（607×526、底边 787）；底边全程单调下降 703→787，**没有向上腾空**，与“小幅离地”不一致，需用户判定；背景相对项目基色 `(0,159,62)` 偏差 ≤4.8，主体颜色漂移仅 5.4。未选帧、未抠图、未改 Unity。详见方案文档 §11。
- **已接入（2026-10-18，按用户“两组都接”）**：死亡 6 帧（T=80 抠图 + 面积对齐后）以偏移 `(74,91)` 补到 1108，导入为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_cowardDead1..6.png`；新建 `Assets/Animations/Enemy_1021_CowardDead.anim`（30fps，6 键 0/0.09/…/0.45，末尾 0.6 秒保持，length 0.633，loop off，含 `ShieldVisual` 清空键），已替换控制器 `CowardDead` 的 motion（原为 `Enemy_102_CowardDead` 占位）。**尺寸仍为当前对齐版，是否过头待用户在游戏里判断**；要改只需重跑 `align_size_v1.py` 后重导精灵，Clip 不用重建（键指向同一路径）。

### 0.19 CowardLaunched 完整链 · 单图 10 秒源片 + 16 帧透明素材（已验收）

> 用户已验收：“效果挺好”，并确认 18 帧选取（只看节拍），随后指定去掉起飞前待机与回 idle 两帧、按容差 80 抠图，最后验收 16 帧。

- **源片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launch_fullcycle_v2_10s.mp4`（960×960 / 24fps / 241 帧 / 10.041667 秒 / 无音轨，SHA-256 `cef48f8e…9cd7`）。方案与全部测量在 `plan/enemy1021-coward-launch-full-cycle-10s-preparation.md`。
- **一次性要点**：只上传 1021 无盾胆怯 Idle 一张参考图，词里给完整动作自由（Idle→挑飞→下落→落地→起身→Idle），`seedance-2-fast` / 10s / 720p / 1:1 / 无音频无水印 / seed -1；一次任务 `task_78a8dfe0…`，`cost.spend=1.21464`。
- **透明帧**：`Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/keyed_t80_v1/`，16 张 960×960 RGBA：`rise1..5`（f035/f040/f047/f067/f073）、`fall1..6`（f078/f083/f087/f091/f100/f142）、`getup1..5`（f144/f149/f155/f163/f188）；无缩放、无对齐、无补边。
- **抠图口径**：SVL Chroma 自动取键（本片实测 `(2,158,59)`）、容差 **80** / softness 16 / halo 1 / alpha-aware despill / 半透明转不透明（cutoff 128）。结果：半透明 0、透明区非零 RGB 0、幕布残留 0、内部改动 6 px；遗留 `g−max(r,b)>8` 197–524 px/帧，已随帧一并验收。
- **未做**：未补边 1108、未建 Clip、未改控制器/Prefab/场景、未进入 Play。按分工，补边与接入属接入侧；落地/起身目前控制器还有 `CowardIdle` 直连，拆 Clip 与状态接线需单独定。
- **已接入（2026-10-18，按用户“两组都接”）**：16 帧以固定偏移 `(74,91)` 零重采样补到 1108 后导入 `Assets/Sprites/Enemy/Enemy1021/`：`Enemy_1021_cowardLaunched1..5.png`、`Enemy_1021_cowardFall1..6.png`、`Enemy_1021_cowardGetup1..5.png`。新建 `Assets/Animations/Enemy_1021_CowardLaunched.anim`（30fps，rise1..5 于 0/0.12/0.24/0.36/0.48 秒，末尾在 2.0 秒再加一键保持，length 2.033，loop off，含 `ShieldVisual` 清空键），并已替换控制器 `CowardLaunched` 的 motion（原为 `Enemy_102_CowardLaunched` 占位）；出边仍为 `CowardLaunched → CowardIdle`（hasExitTime 1.0，2.03 秒，覆盖 `launchDuration = 2.0`）。Fall/Getup 精灵只导入，未接线。
- **导入尺寸口径**：与已有 1021 精灵一致——磁盘 PNG 为 1108，Unity 实际导入为 1024×1024（PPU 16、Point、FullRect、居中 pivot）；未单独改这份工程既有行为。
- **Fall / Getup 接线（2026-10-18，按 1011 结构）**：对比 `Enemy_1011.controller` 后再做，不凭记忆。
  - 1011 结构：`Idle→Launched_Rise`（代码 Play）→ `Launched_Fall`（hasExit 1.0、Fall 循环）→ 落地时代码 `Play(Getup)` → `Launched_Getup`→`Idle`（exit 1.0）；`Launched_Fall→Launched_Rise` 靠 `ReRise` 触发器。
  - 1021 实做：`Enemy_1021_CowardLaunched` 缩为 **0.6 秒**（5 键×0.12，去掉原 2.0 秒保持）；新建 `Enemy_1021_CowardFall`（6 键×0.1，**循环**）、`Enemy_1021_CowardGetup`（5 键×0.12，不循环）。
  - 控制器：新增状态 `CowardFall`、`CowardGetup`；新建 `CowardLaunched→CowardFall`（hasExit 1.0）、`CowardFall→CowardLaunched`（`ReRise` 触发器）、`CowardGetup→CowardIdle`（exit 1.0）；**删掉原有的 `CowardLaunched→CowardIdle` 直连**（否则 0.6 秒就回站立，Fall 永远看不到）。
  - 代码：`Enemy.cs` 的 `HasGetupAnimatorState()` 原来只认 `Launched_Getup`，现改为 `IsCoward ? "Base Layer.CowardGetup" : "Base Layer.Launched_Getup"`；否则胆怯落地会跳过起身直接回 Idle。已全量重编译通过（compiled / domain_reload completed）。
  - 未做：未进 Play Mode 自检，不验证实机节奏；`CowardFall` 在 2.0 秒飞行里会循环约 2.6 圈，是否重复感需要开游戏看（要改只需调 Fall 的 state speed 或帧间隔）。
- **Fall / Getup 尺寸对齐（2026-10-18）**：用户反馈这两段角色尺寸与其他不一致。实测同一支源片里人物本身在缩：升段均面 62,797、落段 52,934、起身段 53,468（皆为 960 空间），而已验收的 idle 约 63,000。按“与 idle/rise 同一尺度”修正，**每段一个常数**（帧内比例不动）：rise ×1.000（不动）、fall ×**1.089**、getup ×**1.084**；预乘 RGBa LANCZOS、以 alpha 质心为锚、cutoff 128 重新硬化，再按 `(74,91)` 补到 1108 覆盖原精灵（同路径，GUID 不变，三个 Clip 无需重建，已回读确认首键仍指向同一 sprite）。脚本 `Library/Locus/tmp/enemy1021_coward_launch_fullcycle_v2_10s/align_launch_sets.py`，报告 `align_launch_sets_report_v1.json`。
- **死亡组绝对尺度（已按用户“缩”执行）**：死亡 6 帧由 ~107,745 均面缩到 ~62,437（**×0.7647**，全组一个常数），与 idle/rise（~63,000）一致：对齐后尺寸 366–423 高、均面 62,348–62,505；预乘 RGBa LANCZOS、alpha 质心为锚、cutoff 128 硬化，再按 `(74,91)` 补到 1108 覆盖原精灵（同路径，GUID 不变，`Enemy_1021_CowardDead` 七个键已回读全部指向新图，长度 0.633 未变）。脚本 `Library/Locus/tmp/enemy1021_coward_dead_video_v1_10s/scale_dead_set.py`，报告 `scale_dead_set_report_v1.json`。
  - **需开游戏看的一点**：缩放后死亡帧底边为 651–692（960 空间），比 idle 脚线 703 高 11–52 px，静止终态可能微浮；如果看到明显悬空，需要单独做“底边对齐地面”而不只是缩放（会改变帧间相对高度，另定口径）。

### 0.18 CowardLaunched 视频 v1 · 一次被挑飞后保持（2026-10-18，已提交一次；**验收不通过**）

> 用户已否决自行生成的峰值图：“左侧脚姿态不正确导致看着像飞升”。~~不再生成峰值图，直接用已验收的 1011 击飞姿态作参考~~（**此行已作废：1011 帧带剑，v1 成片凭空长出剑，见下条验收结果**）。用户已同意：生成容器 6 秒；不分段；胆怯起身另做。

#### 运行时边界

- `CowardLaunched` 从挑飞一直播到落地。上升、下落和真正高度都由 `Enemy.UpdateLaunch()` 的重力与根节点 Y 位移完成，精灵不得在画布里模拟整段飞行。
- 控制器现有 `CowardLaunched → CowardIdle`，hasExitTime 1.0、exit 1.0、duration 0。因 clip 做成 6 秒，2 秒浮空结束前会提前切回站立。部署时 clip 做 **2.0 秒、Clamp、末帧保持**，覆盖 `launchDuration = 2.0`。
- 浮空再受击会重播该片段，所以前 0.55 秒必须是一次完整可读的被挑飞。
- 代码支持 `CowardGetup`，但当前控制器没有该状态，且 `HasGetupAnimatorState()` 只检查 `Launched_Getup`，所以现在落地直接回 `CowardIdle`。起身是下一支独立视频，不要写进本次词。
- 不要新增 `CowardFall`。下落期间继续保持本次的空中倒姿。

#### 两张实际上传图

| 图 | 路径 | 职责 |
|---|---|---|
| 图1 | `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png` | 1021 无盾身份、正面、绘制尺寸、起始站姿和绿幕。SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`，四角精确 `(0,177,64)` |
| 图2 | 从 `enemy1011_launch_rise_v1.mp4` 重抽的 **1-based f056**（对应已部署 `Enemy_1011_rise4`，2.292 秒）的 1024 绿幕上传副本 | 只负责身体接近水平、双腿朝后、双手向两侧的关系 |

两张都用普通 `reference_image`，不使用 `first_frame` / `last_frame`。

**禁止直接上传** `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_rise4.png`：它是 1108 白底透明部署图，并且带剑。本机已核对，原始视频与 `Library/Locus/tmp/sword_enemy_launch_v1/` 都不在。提交前必须回视频重抽 f056，另存 1024 同色绿幕副本：角色不缩放，只改外部背景为 RGB(0,177,64)，主体内部改动为 0，并回读四角色值与 SHA-256。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 6 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无 / 无 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launched_v1.mp4` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
This is a 2D game-sprite launch reaction, not a cinematic. Use both images as ordinary references. IMAGE 1 defines the unshielded soldier, his identity, front-facing view, drawing size and starting pose. IMAGE 2 defines ONLY the final body relationship: the torso is nearly horizontal, both legs trail backward, and both arms spread sideways. Do not copy IMAGE 2's character, armour, colours, sword, background or canvas.

The same unshielded soldier is launched once by an upward blow from below and slightly in front. He is alive, afraid and has no shield. This is one action, not a jump and not a death.

TIMING:
0.00-0.08s: hold IMAGE 1.
0.08-0.30s: the pelvis is carried up and backward. Both boots leave the ground together and the torso begins to tip backward.
0.30-0.55s: reach the IMAGE 2 relationship. The torso is nearly horizontal. Both knees are loose, both legs trail backward, and both boot toes point backward, never downward. The screen-left arm is thrown out and slightly upward, while the screen-right arm is thrown sideways near shoulder height. Both palms stay open. The head stays toward the camera and the red plume trails backward.
0.55-6.00s: hold that exact airborne pose. No falling, rolling, second hit, landing or return to standing.

The lowest part of the silhouette must never be only one downward boot toe. Keep the same soldier and drawing size as IMAGE 1. The game moves the root upward, so do not fly him high inside the canvas. Keep at least 12 percent empty margin on every side.

Locked camera, no zoom, pan, tilt or shake. Keep the same flat chroma-green background. No shield, weapon, sword, blood, sparks, smoke, dust, flash, speed lines, floor, shadow, scenery, text or UI.
```

中文对照（不提交）：

> 这是一段 2D 游戏精灵击飞，不是电影。两张图都只作普通参考。图1定 1021 无盾士兵的身份、正面、绘制尺寸和起始站姿。图2只定最终身体关系：躯干接近水平，双腿朝后，双手向两侧张开。不要复制图2的角色、盔甲、颜色、剑、背景或画布。
>
> 同一个无盾士兵被从下方略偏前方的一次向上冲击挑飞。他还活着，并且害怕、没有盾。这是一次动作，不是跳跃，也不是死亡。
>
> 节奏：0.00–0.08 秒保持图1；0.08–0.30 秒骨盆被向上向后带起，双脚同时离地，躯干开始后倒；0.30–0.55 秒到达图2的关系，躯干接近水平，双腿松弛并朝后，两只靴尖朝后而不是朝下；画面左侧手臂向外并略向上，右侧手臂向侧方、接近肩高，双手张开；头仍朝向镜头，红缨向后拖；0.55–6.00 秒完全保持该空中姿态，不下落、不翻滚、不再受击、不落地、不回到站立。
>
> 剪影最低处永远不能只剩一只朝下的靴尖。保持图1的角色与绘制尺寸。游戏会移动根节点，不要把人画到画布高处。四边至少留 12% 空边。镜头锁定，背景保持同一平面绿幕。无盾、武器、剑、血迹、火花、烟尘、闪光、速度线、地面、阴影、场景、文字和 UI。

负面词（并入同一 prompt）：

```text
no floating upright body, no standing in the air, no downward boot toes, no one boot hanging lowest, no raised arm pose, no hero pose, no jump, no tucked knees, no falling, no landing, no rolling, no second action, no return to standing, no death, no corpse, no copied character from image 2, no copied sword, no copied armour, no shield, no weapon, no enlarged character, no high flight inside the canvas, no side view, no spin, no camera movement, no floor, no shadow, no scenery, no blood, no sparks, no smoke, no dust, no flash, no speed lines, no text, no UI, no cropped limb, no body leaving the frame
```

#### 发起后

下载并完整解码，再交用户看视频。通过后只从 0.08–0.55 秒及其后的稳定保持段选 3–5 帧；抠图、补边和接入分别验收。部署时把峰值保持到 2.0 秒，不要把 Unity clip 做成 6 秒。

失败时不要自行改词重发。尤其不要接受“一只脚朝下垂着”或“整个人站在空中”的结果。

#### 执行结果（2026-10-18，已提交一次、下载并全帧解码；结果见下一条验收结果）

- **授权**：用户 2026-10-18 指令“根据交接文档发起视频生成”；本节词、参数、输出路径均未改，英文正文 `f4078730…144a` ＋ 负面词 `abfbbffa…a6e9`，合并 `e7bc74b0…bfc8`。
- **图2 偏离（已向用户说明并获批）**：本机**没有** `enemy1011_launch_rise_v1.mp4`，也没有 `Library/Locus/tmp/sword_enemy_launch_v1/`（全盘搜索 mp4 与帧目录均为 0），无法按本节“回视频重抽 f056”。经用户选择“用重建副本提交”，改用 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_rise4.png`（`plan/enemy1011-frame-provenance.csv` 登记为 f056 @2.292s、本地绿幕键、补边 (74,120)、逐像素一致）另存 1024 绿幕副本：角色不缩放（主体 791×520 与源相同）、四角精确 `(0,177,64)`、最小留边 116px、SHA-256 `8772afd1…5b3d`。图1 不变（`3fd64c93…a6c0`）。
- **Task**：`task_99b97a3345fc4606a3df4aa11f796522`；create HTTP 200，两次上传 200，`create_attempts=1`，无重试；cost `spend=0.7308`，completion 130,500 tokens。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launched_v1.mp4`，3,087,094 bytes、SHA-256 `1037d2be…abfd`，960×960 / 24fps / 145 帧 / 容器 6.041667 秒 / 末帧 PTS 6.0 / 无音轨，四段 Range 与全帧解码通过。
- **与词的客观对差**：① 动作自 f029（1.208s）才开始，f000–f028 完全是待机（298×410、底边 703），未按 0.08–0.30s 起跳；② f033（1.375s）后冻结到 f144（6.0s），相邻帧差 ≤约 60px，等于 4.6 秒稳定保持，符合“不下落、不翻滚、不二次受击”；③ 末帧包围盒 742×488、主轴距水平 4.6°（躯干接近水平）、宽度 2.49× 待机；④ 末帧最低带仅 57px 宽（x 606–662）而上一带 412px；后续量测确认这个窄突出就是**剑尖**，不是靴尖（见下一条）。
- **材料**：`Library/Locus/tmp/enemy1021_coward_launched_video_v1/` 的 `LAUNCHED_V1_EARLY_f000_f025.png`、`LAUNCHED_V1_TRANSITION_f026_f038.png`、`LAUNCHED_V1_FINAL_POSE_MARKED.png`、`LAUNCHED_V1_FINAL_LOWEST_ZOOM.png`、`objective_metrics.json`、`pose_detail.json`、`frames_all/`。
- **停点**：交用户看片。未选帧、未抠图、未改 Unity 资产；未自行改词重发。

#### 验收结果（2026-10-18）：**不通过**——凭空出现 1011 的剑，且不是 1021 胆怯造型

- **用户原话要点**：① 凭空出现了 1011 的剑；② 完全没有按 1021 的胆怯造型制作击飞。
- **根因（实测，不是推测）**：图2 是带剑的 1011 帧，武器随参考图被复制进来，负面词里的 `no sword` / `no weapon` 没能抵消。剪影底部的窄突出物就是剑：图2 参考的最后两档是 60px→45px（x 646–705），成片末帧同样在 394px 宽带之后突降为 57px→46px（x 606–662），形态一致；正常待机帧底部是 248px 连续靴带，没有这种窄突出。
- **结论**：本节自己写的“禁止直接上传 `Enemy_1011_rise4.png`”是对的。带武器的姿态参考不能靠负面词约束，必须换图，不能靠改词。
- **新约束（下一支视频必须遵守）**：图2 只能是**无武器、无盾**的 1021 姿态图（首选 1021 自己的帧），不得用任何 1011 rise/dead 帧；图1 继续用 `01_coward_idle_peak_source_green_00B140_1024.png`。
- **当前授权状态**：用户 2026-10-18 明确“更新交接文档即可不要制作”——本次到此为止，**不准备新参考图、不建新候选副本、不再提交任何生成**。图2 用哪一张（项目内其它击飞帧 / 只用图1 单图 / 由写词侧另行给图）尚未拍板；未拍板前不发起视频。
- **材料**：`Library/Locus/tmp/enemy1021_coward_launched_video_v1/fail_analysis_sword_protrusion.json`、`LAUNCHED_V1_FINAL_LOWEST_ZOOM.png`、`LAUNCHED_V1_FINAL_POSE_MARKED.png`。成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_launched_v1.mp4` 留作失败样本，不复用、不抽帧。

#### 本次不做

- 不再提交 `enemy1021_coward_launched_peak_keypose_v1.png` 那条生图路线。它已被用户否决：左侧脚朝下，读成飞升。
- 不做 `CowardGetup` 视频、选帧、抠图或控制器改动。
- 不进入 Play，不改机制。

### 历史停点：胆怯家族第一批已接入；按实机反馈修了尺寸与时长，并新增“挑飞破盾”（2026-10-18，待游戏复验）

以下内容保留为历史，不再优先于 §0.18。

- **已接入第一关（N1ART）**：`Enemy_1021_CowardIdle.anim`（6 帧、30fps、0.96s 循环）与 `Enemy_1021_CowardHit.anim`；控制器只改这两个 motion，其余 13 个状态与 17 条转场未变。
- **修正一：胆怯形象偏小**。实测（960 空间、脚底/中线）：持盾 Idle 高 549–569、脚底 780；胆怯 Idle 原为 401–419、脚底 702——**胆怯是均匀缩小版（×0.73）**，不是补边或坑位问题。已用**预乘 alpha 重采样**整体放大并锚到持盾 Idle 的画布位置：胆怯 Idle ×**1.363**、命中帧（1024 源）×**1.276**，目标脚底/中线 (475,780)。重测：胆怯 Idle 高 546–570、脚底 779–780、中线 ~475；命中帧（峰值）712×596、底边 762（仍离地）。
- **修正二：受击太快**。`CowardHit` 由 0.2666s / 4 键改为 **0.4s / 5 键**：待机 0 → 压缩 0.0833 → 峰值 0.1667 → 压缩 0.2667 → 待机 0.3667（去程/峰值/回程各留可读时间）。
- **修正三（机制）：挑飞也能拆盾**。用户指出“C4 与 Launch 挑飞也该能拆盾”。`ShieldGuardEnemy.ProcessHit` 新增：`damageType == DamageType.Launch` 时**不受侧向限制直接 `broke = true`**，本次全额结算；若真的挑起，`Launch()` 会经 `InterruptShieldBreakVisual` 立即完成破盾并走 `CowardLaunched`（无掉盾动画）。已热重载生效、语义检查通过。
- **仍待你验**：胆怯 Idle 的 6 帧节奏（按 0.16s 均分）、受击 0.4s 的手感、以及挑飞破盾时“盾直接消失”（不会生成掉盾对象）是否能接受。
- **留档**：`Library/Locus/tmp/enemy1021_coward_deploy_v1/asset_verification.json`（含 `revision_2026_10_18_size_and_timing`）、`rescale_report.json`。

#### 历史：CowardHit 视频 v4 提交与实测（2026-10-18）

- **路径回顾**：v4 峰值图被否（像摆超级英雄 pose）→ v5（从持盾 HitFlash 去盾）被认为“带盾的厚重感不适合胆怯” → v6（横版 CC0 受击帧作姿势参考）太小 → v7（在 v6 上放大）像跳 → **v8 X 形四肢全开，用户已认可**。
- **视频词由本侧编写**：用户审阅 `Library/Locus/tmp/enemy1021_coward_hit_video_v4/draft_prompt.md` 后指令“发起视频生成”；词借鉴已验收的死亡 v2 结构（两张普通参考 + 时间轴 + 只准一次动作 + 峰值停留 + 尺寸/镜头约束），并把 v2/v3/v4/v7 的失败模式写进负面词。
- **两张上传图**：图1 无盾待机绿幕（SHA `3fd64c93…a6c0`）；图2 为 v8 峰值的 G1 副本 `02_coward_hit_peak_v8_green_00B140_1024.png`（SHA `790d3f10…865e`，只改背景 954,834 px、主体内改动 0）。
- **Task**：`task_3e4271451e904295ad5be8d673703526`，create HTTP 200，**只创建一次**；cost 0.48888；成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v4.mp4`，1,883,229 bytes、SHA-256 `b32093731a356e771581dfddf7427576d8e204b2582e06370699cf2fe09a73b3`，960×960/24fps/97 帧/4.041667 秒，全帧解码通过。
- **实测**：f000–f016 待机 299×410；**f018 张到 525×441**（与已认可的 v8 在 960 画布上等宽）、底边 704→693（离地 11 px）；f018–f062 保持张开约 1.85 秒；f069 回到待机；之后待机到 f096。
- **与词的对差**：词要求峰值只保持 0.30–0.60 秒、1.10 秒前收回；实际保持到约 2.6 秒才收回。选帧时可自行压缩，不影响可用性。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_hit_video_v4/COWARD_HIT_V4_DENSE_SHEET.png`、`contact_sheet.png`、`runtime.json`、`download_manifest.json`。
- **下一步**：交用户看片。通过再选帧→抠图→补边→接入；不通过则记原因等新词。

#### 历史：v4 峰值图否决与之前尝试（2026-10-17）

- **用户否决（原话）**：**“不通过，看着像摆超级英雄的 pose”**。峰值图 `C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_keypose_v4.png` 作废，不得用作视频参考图。
- **实测对应**：主体框 450×445（源图 320×438）——高度几乎不变（1.016），但**宽度被拉到 1.406 倍**，多出来的全是往画面左侧甩开的那只手；可见像素 1.137 倍。宽大、外张、双肩不平的轮廓正好构成“英雄登场”的读感，而不是“被打中”。
- **需回避的方向（写给下一轮）**：不要再靠“把一只手大幅甩开”来表达受击；也不要双臂对称张开、不要站稳了摆姿势。受击图应该是**被动变形**：躯干被挤、头被带偏、四肢各自被冲量带跑而不对称，不是主动张开的英雄姿态。
- **本次提交记录（以免与其他会话的改稿冲突）**：§0.17 词按原文提交一次（HTTP 200，POST 42.973 秒，usage 333/7,024 tokens，cost 未返回）；产物 991,457 bytes、SHA-256 `291cc23d363af756a64ce6d872f4681f42e4874119877e67181353613c59ec48`、1024×1024 RGB、解码通过。对照材料 `Library/Locus/tmp/enemy1021_coward_hit_peak_v4/POSTURE_V4_VS_SOURCE.png`、`HEAD_ZOOM_V4_VS_SOURCE.png`、`v4_metrics.json`。
- **下一步**：词由另一侧重写（本侧不写词、不重发 §0.17）；新词到位后再发起一次。通过前不写视频词、不提交视频。

#### 历史：v3 否决与三次失败模式（2026-10-17）

- **用户否决（原话要点，以此为准）**：① **动作非常抽象**；② **角色关键受击动作居然是毫无表情的捂胸**；③ **没有任何受击感**。整支 v3 不通过，不接受抽帧补救。
- **实测对应**：f017–f020 先是整个人变高（顶部 +42 px）；f021 才压成 266×394 并一直保持到 f053（约 1.35 秒）——这 1.35 秒的“保持”看起来就是捂胸站着，不是受击；f054 又一帧硬弹回站姿。即：没有冲击瞬间、没有压缩后的回弹，只有一个静态捂胸姿势。
- **三次失败模式归纳（写词时必须逐条避开）**：v1 = 动作太小，抽帧仍像待机护脸；v2 = 两个语义不一致的受击动作＋中间停顿＋硬切；v3 = 只有“捂胸”一个静态姿势，没有受击感、没有表情。
- **下一步**：另一边按 §0.17 生成一张受击峰值图。不要重跑 §0.14、§0.15 或 §0.16。峰值图未经用户看过之前，不写视频词、不提交视频。
- **不受影响**：`CowardIdle` 的呼吸最高点图、循环视频、6 帧透明素材均已验收，不受本次影响。

#### 历史：v3 提交与下载（产物为失败样本，不复用）

- **授权与提交**：用户 2026-10-17 指令“已更新交接文档，请按照最新文档内容发起视频生成”；按 §0.16 原文提交（未改词、未换图、未降参数），脚本从文档逐字提取后先校验哈希再上传。
- **Task ID**：`task_f121b090d3c54b078ec70db42eda1bd9`；create HTTP 200；**只创建一次**（`create_attempts=1`）；usage completion 87,300 tokens，`cost.spend=0.48888`。
- **参考图**：只上传 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`（四角精确 `(0,177,64)`，SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`），单张普通 `reference_image`；未上传 102 受击图、v1/v2 成片帧，也未另做关键帧。
- **词指纹**：英文正文 `9469e0fb048870352ee5280038e582a9a0482a78704764b60558c77ce422ad15` ＋ 负面词 `4b7e2c7b55352be12c79019aedd8d5129c503354e221392a8abb3334894004ed`，合并 `723d9b859c6289331a24c3f928eb903964b01eaa288d4914a32ec8ddb95f576c`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v3.mp4`，1,652,421 bytes，SHA-256 `bfd4eda1f126ef7697bd98b3e76f85a04fdbf5891bbf6a1494234c89e40ffcc3`；960×960 / 24fps / 97 帧 / 容器 4.041667 秒 / 末帧 PTS 4.0 / H.264 / 无音轨；四段 Range 校验与全帧解码通过。
- **下载故障与处理（未重建任务）**：首次下载在分段读取时报 `TimeoutError`，任务本身已 `success`；用带续传的分段下载器重试同一 task，段 0 从 81,920 字节续传，其余三段直接复用，最终拼接后全帧解码通过。
- **客观量测（供用户判断，不代替看片）**：① 开场 idle 到 f016，bbox 298×410；② **f017–f020 反而变高**：顶部 294→252（高 +42 px）、可见像素 62,757→67,865，与“竖直压缩”相反；③ f021 再跳一次到 266×394（比 idle 矮 16 px），并在 f025–f053 保持这个压缩姿态（约 1.35 秒）；④ **f054 一帧内直接弹回 idle 姿态**（单帧变化 58,260 px），没有收势过程。
- **检查材料**：`Library/Locus/tmp/enemy1021_coward_hit_v3/` 的 `COWARD_HIT_V3_DENSE_SHEET_f000_f048.png`（0–48 帧隔帧）、`keyframes/f000|f017|f021|f030|f054.png`、`contact_sheet.png`、`first_frame.png`、`last_frame.png`、`runtime.json`、`download_manifest.json`、`generate_video.py`、`download_existing.py`（均已执行，不要重跑创建）。
- **下一步**：交用户看片。通过则选帧→抠图→补边→接入；不通过则等另一侧再改词，本侧不自改、不自重发。

本节视频已验收不通过。不要再提交本节，也不要抽帧补救。下一版先用 §0.17 生成一张受击峰值图。

### 0.17 CowardHit 受击峰值图 v4（2026-10-17，已提交一次；**验收不通过**，不要再提交）

> v1 到 v3 都只有一张待机图，模型只能猜受击。v3 猜成了捂胸，并保持了 1.35 秒。这次先只做峰值图，用户看过之前不做视频。

#### 上传

- 源姿态：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`。SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`，四角精确 `(0,177,64)`。
- 不上传 102 的受击图、v1/v2/v3 成片帧，也不上传持盾的 HitFlash 或 HitRight。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst` |
| 接口 | `POST https://api.muskapis.com/v1/images/edits` |
| quality / input_fidelity | max / high |
| size / background / format / n | 1024x1024 / opaque / png / 1 |
| 建议输出 | `C:/Users/steam/Pictures/gptGen/enemy1021_coward_hit_peak_keypose_v4.png` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
Edit the provided image into one still 2D game sprite. This is the single peak frame of a hit, not a video and not a defensive pose.

The same unshielded soldier has just been struck in the chest from straight ahead. The attacker stays outside the frame. Show the result of the blow, not a pose he chooses.

His head is knocked toward screen-right and the helmet tips that way. The orange eyes narrow and the brow drops, so the face reads as hit. One open hand is thrown outward to screen-left by the impact. The other hand stays open near the chest but does not clutch it. Both shoulders are uneven from the blow. Both knees bend and both boots stay planted.

Do not fold both hands together, do not cover the chest, and do not keep the calm guarding pose from the source. The result must be visibly different in the head, eyes, and one arm.

Keep the same soldier, front-facing body, drawing size, and flat chroma-green background RGB(0,177,64). No shield, weapon, blood, sparks, smoke, dust, flash, speed lines, floor, shadow, scenery, text, or UI. Keep at least 15 percent empty margin on every side.
```

中文对照（不提交）：

> 把提供的图改成一张 2D 游戏精灵静帧。这是受击的唯一峰值帧，不是视频，也不是防御姿势。
>
> 同一个无盾士兵刚被画面正前方的一击打在胸口。攻击者不入画。画的是被打之后的结果，不是他自己摆出来的姿势。
>
> 头被打向画面右侧，头盔也向那边偏。橙色眼睛收窄，眉部下压，脸上要能读出被打到。一只张开的手被冲击甩向画面左侧。另一只手仍张开并留在胸口附近，但不捂住胸口。双肩因冲击变得不平。双膝弯曲，双脚不离地。
>
> 不要把两只手合在一起，不要捂胸，也不要保持原图那种平静的护卫姿势。头、眼睛和一只手臂必须与原图明显不同。
>
> 保持同一个角色、正面身体、绘制尺寸和纯色绿幕 RGB(0,177,64)。无盾、武器、血迹、火花、烟尘、闪光、速度线、地面、阴影、场景、文字和 UI。四边至少留 15% 空边。

负面词（与英文词一起提交）：

```text
no hands clutched together, no covering the chest, no guarding pose, no calm face, no unchanged eyes, no shield, no weapon, no blood, no sparks, no smoke, no dust, no flash, no speed lines, no floor, no shadow, no scenery, no text, no UI, no enlarged character, no side-view redesign, no cropped limb, no body leaving the frame
```

#### 生成后

先把这一张图交给用户看。通过前不写视频词、不提交视频。若不通过，不自行改词重发。

执行结果（2026-10-17）：已按本节原样提交一次，HTTP 200，POST 42.973 秒，usage 333/7,024 tokens，cost 未返回；产物 `enemy1021_coward_hit_peak_keypose_v4.png`（991,457 bytes、SHA-256 `291cc23d…ec48`、1024×1024 RGB）。**验收结果：不通过**——用户：“看着像摆超级英雄的 pose”。主体框 450×445（源 320×438）、宽比 1.406、高比 1.016、可见像素比 1.137：宽度全靠一只手外甩，读成了英雄登场。**本节已执行且已作废，不要再提交第二次。**

#### 历史：v2 否决与帧挑选（2026-10-17）

- **用户否决（原话要点，以此为准）**：① **视频里实际上做了两个语义完全不一致的受击动作**；② **两段之间切换得非常僵硬**。因此整支 v2 不通过，不是“换几帧”能解决。
- **实测支持用户判断**：帧间运动量显示两段运动爆发中间夹着一段近似静止——f016–f024（双手收向胸口，主体框宽 299→264）、f025–f033（几乎不动，约 0.35 秒）、f033–f042（右臂甩开，框宽 265→399）、f042 之后静止。即模型把“抱胸”和“甩臂”做成了两下独立动作，而不是一次连贯受击。
- **帧挑选已尝试并被否**（记录仅为追溯，**不得用于部署**）：按用户“只取前半部分、跳过开场 idle”的指示，抽过 3 帧稳定姿态候选 `f024`（1.00s 抱胸）/ `f036`（1.50s 甩臂）/ `f044`（1.83s 收势），文件在 `Library/Locus/tmp/enemy1021_coward_hit_v2/selected_v1/`，对比图 `COWARD_HIT_3FRAME_REVIEW_v1.png`、预览 `COWARD_HIT_3FRAME_PREVIEW_v1.gif`、清单 `selected_frames_v1.json`。结论：三帧仍是从两段不同语义里各取一帧，选了也还是“两个动作硬拼”，用户已在看到该分析后否决整支视频。**这些帧不作为素材，不抠图、不接入。**
- **不受影响的部分**：`CowardIdle` 6 帧透明素材、`CowardIdle` 循环视频、呼吸最高点关键帧均已验收，照常推进；本次只有 `CowardHit` 这一项需要重做。
- **下一步**：另一边按 §0.16 发起一次。不要重跑 §0.14 或 §0.15，不要另做受击关键帧，也不要上传 102 的受击图。

#### 历史：v2 提交一次、下载与解码（产物为失败样本，不复用）

- **授权与提交**：用户 2026-10-17 指令“根据文档发起生成视频”，按 §0.15 原文提交（未改词、未换图、未降参数），脚本从文档逐字提取后先校验哈希再上传。
- **Task ID**：`task_de9e6bcb777248eeb7c0fa5a257bee1e`；create HTTP 200，accepted UTC `2026-10-17T14:14:05+00:00`；**只创建一次**（`create_attempts=1`），失败不重试。32 次轮询后 `success`。
- **上传与参考图**：只上传 §0.15 指定的 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`（1024×1024 RGB，四角精确 `(0,177,64)`，SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`），单张普通 `reference_image`；未上传 102 的受击图、v1 成片帧或已补边的 `Enemy_1021_shieldBreak6.png`。
- **参数**：`seedance-2-fast` / 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1。
- **实际提交词**：§0.15 英文正文（SHA-256 `c76eb24a956f6b2041dfd0b0f6e570d427fb3e2944bc3d816c8aa2404e8d7eb4`）＋ 负面词（`69fed04fbbe04def37a9fba2b2c01cf91e2e4eb666fd7f9fd6d25cde4a3c7ee6`）合并，合并后 `3d29a8b5193c0638b75c42ce3bf0e7e58e3652910c5c03c291c44af7af09683c`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v2.mp4`，1,747,950 bytes，SHA-256 `0ceae089e47bd52bd8f7d775d5b5ccd081af7b772d9479f165b76a5858ebd664`；实测 960×960 / 24fps / 97 帧 / 容器 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨；四段 Range 均 206 并按 ETag 校验，全帧解码通过。
- **费用**：usage completion 87,300 tokens，`cost.spend=0.48888`（币种未注明）。
- **检查材料（仅文件诊断，不是选帧）**：`Library/Locus/tmp/enemy1021_coward_hit_v2/` 的 `contact_sheet.png`（帧 0/24/48/72/96）、`COWARD_HIT_V2_DENSE_SHEET_f000_f036.png`（帧 0–36 隔帧共 19 格，用来看两拍受击）、`inspection_frames/`、`first_frame.png`、`last_frame.png`；下载报告为视频旁 `.download.json`。
- **下一步（旧计划，已作废）**：原本计划“通过则选帧→抠图→补边→接入”；现因整支视频被否，不再执行。

### 0.16 CowardHit 视频 v3 · 一次竖直压缩（2026-10-17，已提交一次并下载完成；**验收不通过**，不要再提交）

> v2 把 102 的两帧切换写成了视频里的两个姿势，所以成片停顿后再硬切。v3 改用已验收 1011 正面受击的方法：只有一次压缩，没有第二拍，也没有第二张姿态图。

#### 实际上传图

只上传这一张：

`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`

SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`，四角精确 `(0,177,64)`。它同时是受击前和受击后的站姿。

不要上传 102 的 `Enemy2_Coward_Hit_01.png`、`Enemy2_Coward_Hit_02.png`，不要上传 v1/v2 成片帧，也不要新做受击关键帧。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 4 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无 / 无 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v3.mp4` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
This is a 2D game-sprite hit reaction, not a cinematic. Use only the provided image. It is both the opening pose and the recovered pose. Do not use a second pose image.

The same unshielded soldier takes one light hit in the centre of his chest from straight ahead. The attacker stays outside the frame. He compresses once, then returns to the same standing pose. This is one impact, not two actions.

TIMING:
0.00-0.10s: hold the provided pose.
0.10-0.25s: the hit peaks. Both shoulders rise and pull inward, the neck shortens, and the helmet sinks toward the shoulders. Both knees bend, so the whole figure becomes shorter. The open hands only follow the shoulders inward. Do not create a separate chest-clutch pose or a later arm-throw pose.
0.25-0.90s: he releases and returns to the provided standing pose.
0.90-4.00s: hold that recovered pose. No second hit and no pause between two different hit poses.

The body stays vertical. He does not lean, turn, throw his head, or throw either arm outward. Both palms stay open. The screen-left hand remains near the face, and the screen-right hand remains in front of the chest. Both boots stay planted.

The figure must never become larger than in the provided image. No shield, weapon, blood, sparks, smoke, dust, flash, speed lines, floor, shadow, scenery, text, or UI. Keep at least 15 percent empty margin on every side. Locked camera, no zoom, pan, tilt, or shake. Keep the same flat chroma-green background.
```

中文对照（不提交）：

> 这是一段 2D 游戏精灵受击，不是电影。只使用提供的这一张图。它既是开头姿势，也是恢复后的姿势。不要使用第二张姿态图。
>
> 同一个无盾士兵被画面正前方的一记轻击打在胸口中央。攻击者不入画。他只压缩一次，然后回到同一站姿。这是一次受力，不是两个动作。
>
> 节奏：0.00–0.10 秒保持原姿势；0.10–0.25 秒达到受击峰值，双肩上提并内收，脖子缩短，头盔沉向肩膀，双膝弯曲，整个人变矮；张开的手只跟随肩膀向内，不单独做成抱胸姿势，也不要之后再甩臂；0.25–0.90 秒松开并回到原站姿；0.90–4.00 秒保持恢复后的姿势。没有第二下，也没有两个受击姿势之间的停顿。
>
> 身体保持竖直。不侧倾、不转身、不甩头、不把任何一只手臂向外甩。两只手掌保持张开。画面左侧的手仍在脸附近，画面右侧的手仍在胸前。双脚不离地。
>
> 人物任何时候都不得比原图更大。无盾、武器、血迹、火花、烟尘、闪光、速度线、地面、阴影、场景、文字和 UI。四边至少留 15% 空边。镜头锁定，背景保持同一平面绿幕。

负面词（并入同一 prompt）：

```text
no second pose, no second hit action, no pose snap, no pause between poses, no chest-clutch pose, no arm throw, no head throw, no body lean, no side view, no shield, no weapon, no enlarged character, no stretched limbs, no step, no airborne body, no death, no camera movement, no floor, no shadow, no scenery, no blood, no sparks, no smoke, no dust, no flash, no speed lines, no text, no UI, no cropped limb, no body leaving the frame
```

#### 发起后

下载并完整解码，再交用户看视频。如果仍出现两个姿势或中间停顿，不要自行改词重发。

执行结果（2026-10-17）：已按本节原样提交一次，task `task_f121b090d3c54b078ec70db42eda1bd9`（create HTTP 200，cost 0.48888）；成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v3.mp4`，1,652,421 bytes、SHA-256 `bfd4eda1…fcc3`、960×960/24fps/97 帧，全帧解码通过。**本节已执行，不要再提交第二次。** 实测：f017–f020 人物比原图更高（顶部 +42 px），f021 才压到 266×394 并保持到 f053，f054 一帧弹回原姿态。**用户验收：不通过**——动作抽象、关键受击帧是“毫无表情的捂胸”、没有受击感；本词作废，等另一侧再改。

#### 历史：CowardIdle 6 帧验收与 CowardHit v1 否决（2026-10-17）

- **用户验收**：用户于 2026-10-17 确认“已验收”，6 帧透明结果通过。代理报告里的遗留绿像素（每帧 214–317 px，约 0.35%–0.53%）与 f039 的 2 px 内部偏差均视为已接受。
- **用户决策（已执行）**：采用 **6 帧**（不用 4 帧）；抠图**容差 80**。
- **抠图配方**：SVL Chroma 自动取键（实测底 `(0,159,63)`、coloured screen）+ **T=80** + softness 16 + halo 1 + 半透明转不透明；**未开启平滑**。T=80 下幕布残留 0；扫描时 T=90 绿残留更低（132 vs 246），但按用户指定值执行。
- **交付目录（最终透明素材）**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_coward_idle_video_v1/coward_idle_6frames_matted_v1/`

| # | 文件 | 时间 | 可见px | 半透明 | 透明RGB非零 | green>8 | 内部RGB改动 |
|---|---|---|---|---|---|---|---|
| 1 | `cowardIdle01_f039.png` | 1.625s | 59,085 | 0 | 0 | 214 | 2 px（maxΔ7） |
| 2 | `cowardIdle02_f045.png` | 1.875s | 59,253 | 0 | 0 | 240 | 0 |
| 3 | `cowardIdle03_f059.png` | 2.458s | 60,536 | 0 | 0 | 223 | 0 |
| 4 | `cowardIdle04_f069.png` | 2.875s | 61,328 | 0 | 0 | 251 | 0 |
| 5 | `cowardIdle05_f076.png` | 3.167s | 60,548 | 0 | 0 | 311 | 0 |
| 6 | `cowardIdle06_f078.png` | 3.250s | 60,253 | 0 | 0 | 317 | 0 |

- **验收指标（逐帧实测）**：半透明像素 **0**；透明区非零 RGB **0**；幕布残留 **0**；封闭孔 **0**；距轮廓 ≥4px 的内部 RGB 改动 f039 仅 2 px（maxΔ7），其余帧 **0**。
- **遗留绿像素**：可见区 `g−max(r,b) > 8` 共 214–317 px（每帧约 0.35%–0.53%），属 SVL 去溢色的既有边界，白/深底目视无绿边；不声称严格零绿。
- **帧序与节奏**：f039 → f045 → f059 → f069 → f076 → f078，与候选清单一致；步长 28,041–37,104（比值 1.32）。帧序与时长由接入侧定，本侧不代定 Clip。
- **清单**：`coward_idle_6frames_matted_v1.json`（逐帧 SHA-256、可见像素、Alpha 指标、源 SHA）。
- **评审材料**：`COWARD_IDLE_MATTED_6_REVIEW_v1.png`（上白下深双底）、`COWARD_IDLE_MATTED_6_CHECKER_v1.png`、`COWARD_IDLE_MATTED_6_PREVIEW_v1.gif`（6 帧 ×150ms，仅评审，不定义 Clip）；原始 RGB 候选仍在 `candidates_six/`（未动），原始 RGBA 在 `keyed_ui_t80/`。
- **未做（且不在本侧范围）**：对齐、1108 补边、Clip 创建、Unity 接入均未由本侧执行；未改任何 Unity 资产、未进入 Play。
- **CowardHit v1 未通过**：成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v1.mp4`，960×960 / 24fps / 97 帧 / 4.04 秒。用户判定完全没有受击感。抽帧看，f012 与 f024 仍是待机护脸，f036 以后只是把手放低。不要重跑 §0.14。
- **下一次**：按 §0.15 发起一次。仍然不另做关键帧，也不上传 102 的受击图。

### 0.14 CowardHit 视频 · 交给另一方发起一次（2026-10-17，尚未提交）

> 不生成受击关键帧，也不上传第二张姿态图。只有一张无盾待机参考，动作由视频连续完成。

#### 实际上传图

| 图 | 上传路径 | 职责 |
|---|---|---|
| 图1 | `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png` | 受击前和受击后的同一个无盾待机。SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`。四角精确 `(0,177,64)` |

只用普通 `reference_image`，不使用 `first_frame` / `last_frame`。不要上传呼吸高点、CowardIdle 成片帧、持盾受击图或已补边的 `Enemy_1021_shieldBreak6.png`。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 4 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无 / 无 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v1.mp4` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
This is a 2D game-sprite hit reaction, not a cinematic. Use only the provided image. It shows the same unshielded soldier in his nervous standing pose. Do not use a second pose image. There is no target pose to snap to.

One invisible blow comes from straight ahead. The attacker stays outside the frame. The soldier is already afraid and has no shield. He is hit once, staggers, and settles back into the same standing pose. He is not killed and he is not launched.

TIMING:
0.00-0.15s: hold the provided standing pose.
0.15-0.35s: the blow lands immediately. His shoulders jerk, his head dips, and both open hands are knocked slightly apart. The first moving frame must already show the hit.
0.35-1.10s: the body rocks back toward centre and the hands settle. He returns to the same nervous standing pose by the end of this range.
1.10-4.00s: hold that recovered pose. No second hit.

KEEP: both palms stay open and toward the camera. The screen-left hand remains near the face, and the screen-right hand remains in front of the chest. Both boots stay planted. He stays front-facing. There is no shield and no weapon in either hand.

The reaction is larger than idle breathing, but it is still a short stagger. Do not enlarge, shrink, turn, or redraw him. Keep at least 15 percent empty margin on every side.

CAMERA AND BACKGROUND: locked camera, no zoom, pan, tilt, or shake. Keep the same flat chroma-green background. No floor, shadow, scenery, blood, sparks, smoke, dust, flash, speed lines, text, or UI.
```

中文对照（不提交）：

> 这是一段 2D 游戏精灵受击，不是电影。只使用提供的这一张图。它是同一个无盾士兵的紧张站姿。不要使用第二张姿态图，也没有需要硬扭过去的目标姿势。
>
> 一记隐形攻击来自画面正前方，攻击者不入画。士兵已经失去盾并且处于胆怯状态。他被打一次，晃动，然后回到同一站姿。他没有死，也没有被击飞。
>
> 节奏：0.00–0.15 秒保持提供的站姿；0.15–0.35 秒受击立刻发生，肩膀猛地一沉，头低下，两只张开的手被打得稍微分开，第一帧运动就必须已经是挨打；0.35–1.10 秒身体晃回中心，双手收回，结束时回到同一个紧张站姿；1.10–4.00 秒保持恢复后的姿态，不再挨第二下。
>
> 两只手掌继续张开并朝向镜头。画面左侧的手仍在脸附近，画面右侧的手仍在胸前。双脚不离地，人保持正面。两只手里都没有盾，也没有武器。
>
> 动作幅度要比待机呼吸大，但仍然只是一次短促晃动。不得放大、缩小、转身或重画角色。四边至少留 15% 空边。镜头锁定，背景保持同一平面绿幕。无地面、阴影、场景、血迹、火花、烟尘、闪光、速度线、文字和 UI。

负面词（并入同一 prompt）：

```text
no second pose image, no target pose, no pose snap, no shield, no weapon, no closed fist, no attack, no death, no launch, no airborne body, no second hit, no step, no body turn, no side view, no enlarged character, no stretched limbs, no camera movement, no floor, no shadow, no scenery, no blood, no sparks, no smoke, no dust, no flash, no speed lines, no text, no UI, no cropped limb, no body leaving the frame
```

#### 发起后

下载并完整解码，再交用户看视频。选帧、抠图、对齐和接入分别验收。

本节已用于生成 v1，结果没有受击感。不要再提交本节。下一次改用 §0.15。

### 0.15 CowardHit 视频 v2 · 按 102 胆怯受击重写（2026-10-17，已提交一次并下载完成；**验收不通过**，不要再提交）

> v1 的问题不是缺一张目标图，而是动作本身太小。102 的 `Enemy_102_CowardHit` 只有 0.26 秒、两张姿态：先双手抱在胸前，再把头和一只手臂甩开。v2 借用这个两拍读法，但不上传 102 的图，也不另做 1021 受击关键帧。

#### 实际上传图

只上传 §0.14 的同一张图：

`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`

SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`，四角精确 `(0,177,64)`。它只提供 1021 的身份和受击前站姿。

不要上传 `Assets/Sprites/Enemy/Enemy2/Coward/Enemy2_Coward_Hit_01.png`、`Enemy2_Coward_Hit_02.png`、1021 的持盾受击图，或 v1 成片帧。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 4 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无 / 无 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v2.mp4` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
This is a 2D game-sprite hit reaction, not a cinematic. Use only the provided image for the soldier's identity and starting pose. Do not add a second pose image and do not copy another character.

He is already unshielded and afraid. One heavy invisible blow comes from straight ahead. The attacker stays outside the frame. Show a clear two-part flinch, then let him settle. He is not killed and he is not launched.

TIMING:
0.00-0.10s: hold the provided standing pose for only a short instant.
0.10-0.30s: both open hands snap inward and clutch the chest. The shoulders rise, the neck shortens, and the helmet ducks. This first reaction must be obvious in one frame.
0.30-0.70s: the head is knocked toward screen-right. One arm is thrown outward to screen-left, while the other arm stays bent across the chest. The torso leans toward screen-right. Both boots stay planted, but both knees bend deeper.
0.70-1.30s: the thrown arm and head settle back toward the starting pose. Do not add a new hit.
1.30-4.00s: hold the recovered standing pose.

The middle pose must be visibly different from the provided image: one arm is out, the head is off centre, and the body is leaning. A small hand adjustment is not enough.

Keep the same soldier, front-facing camera, drawing size, and flat chroma-green background. No shield, weapon, blood, sparks, smoke, dust, flash, speed lines, floor, shadow, scenery, text, or UI. Keep at least 12 percent empty margin on every side. Locked camera, no zoom, pan, tilt, or shake.
```

中文对照（不提交）：

> 这是一段 2D 游戏精灵受击，不是电影。提供的图只负责 1021 的身份和起始站姿。不要添加第二张姿态图，也不要复制另一个角色。
>
> 他已经失去盾，并且处于胆怯状态。一记较重的隐形攻击来自画面正前方，攻击者不入画。先给出清楚的两拍受击，再让他恢复。他没有死，也没有被击飞。
>
> 节奏：0.00–0.10 秒只短暂保持原站姿；0.10–0.30 秒两只张开的手迅速收向胸口，双肩上提，脖子缩短，头盔低下，这一下必须单帧就能看出来；0.30–0.70 秒头被打向画面右侧，一只手臂甩向画面左侧，另一只手臂仍弯在胸前，上身向画面右侧倾斜，双脚不离地，但双膝弯得更深；0.70–1.30 秒甩出的手臂和头回到起始姿态，不再增加第二下攻击；1.30–4.00 秒保持恢复后的站姿。
>
> 中间姿态必须和原图明显不同：一只手在外面，头离开中线，身体有倾斜。只把手微调一点不够。
>
> 保持同一个角色、正面镜头、绘制尺寸和平面绿幕。无盾、武器、血迹、火花、烟尘、闪光、速度线、地面、阴影、场景、文字和 UI。四边至少留 12% 空边。镜头锁定。

负面词（并入同一 prompt）：

```text
no second pose image, no copied character, no tiny hand movement, no unchanged pose, no shield, no weapon, no attack, no death, no launch, no airborne body, no second hit, no step, no full side view, no enlarged character, no stretched limbs, no camera movement, no floor, no shadow, no scenery, no blood, no sparks, no smoke, no dust, no flash, no speed lines, no text, no UI, no cropped limb, no body leaving the frame
```

#### 发起后

下载并完整解码，再交用户看视频。若中间仍看不出抱胸和甩臂，不要自行改词重发。

执行结果（2026-10-17）：已按本节原样提交一次（task `task_de9e6bcb777248eeb7c0fa5a257bee1e`，create HTTP 200，accepted UTC `2026-10-17T14:14:05+00:00`，cost 0.48888），成片已下载并全帧解码通过：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_hit_v2.mp4`，1,747,950 bytes、SHA-256 `0ceae089…d664`、960×960/24fps/97 帧。**验收结果：不通过**——用户判定视频里“做了两个语义完全不一致的受击动作，且切换得非常僵硬”；本节词与成片均作废，**不要再提交本节**，等另一侧改写新词。

### 历史停点：CowardIdle 循环帧候选（6 帧/4 帧两套，已选 6 帧；2026-10-17）

- **用户验收**：用户于 2026-10-17 验收循环视频内容，并要求“挑选 6 帧或 4 帧循环帧”。
- **单周期定位（量化，不靠目测）**：用顶边 y（= 肩上抬高度）与主体像素做呼吸信号。静息顶边 = **298**（f000-f005、f034-f044、f085-f096）；呼吸最高点顶边 = **282**（f016-f023、f063-f073）。一次完整呼吸约 **2 秒**（静息→峰值→静息），全片含两次呼吸。
- **重要发现**：这支视频**不是平滑渐变**，而是由多次单帧步长约 2–3 万像素的离散姿态跳变拼成（相邻帧差异表见 `pose_groups.json`）。平台期（如 f016–f023、f034–f044）连续帧几乎不变（≤344 px），因此按“等时间间隔”取帧会拿到重复姿态。
- **选帧方法**：先按 ≥4000 px 阈值把帧聚成姿态组（29 组），取每组代表帧；再在该呼吸周期内穷举组合，以“相邻帧间变化最大值/最小值”最小为目标，且强制包含静息与峰值，避开重复的静息/平台帧。
- **候选 A：6 帧**，目录 `Library/Locus/tmp/enemy1021_coward_idle_video_v1/candidates_six/`

| # | 帧 | 时间 | 顶边 | 主体px | 到下一帧变化 |
|---|---|---|---|---|---|
| 1 | f039 | 1.625s | 298 | 63,239 | 30,589 |
| 2 | f045 | 1.875s | 296 | 63,767 | 36,488 |
| 3 | f059 | 2.458s | 288 | 64,989 | 33,850 |
| 4 | f069 | 2.875s | 282 | 65,815 | 37,104 |
| 5 | f076 | 3.167s | 288 | 65,023 | 28,041 |
| 6 | f078 | 3.250s | 285 | 64,609 | 30,381（回到 #1） |

- **候选 B：4 帧**，目录 `Library/Locus/tmp/enemy1021_coward_idle_video_v1/candidates_four/`

| # | 帧 | 时间 | 顶边 | 主体px | 到下一帧变化 |
|---|---|---|---|---|---|
| 1 | f045 | 1.875s | 296 | 63,767 | 36,488 |
| 2 | f059 | 2.458s | 288 | 64,989 | 33,850 |
| 3 | f069 | 2.875s | 282 | 65,815 | 37,104 |
| 4 | f076 | 3.167s | 288 | 65,023 | 32,502（回到 #1） |

- **两套的步长均匀度**：6 帧为 28,041–37,104（比值 **1.32**）；4 帧为 32,502–37,104（比值 **1.14**）——4 帧更均匀，但 6 帧多了静息起点 f039。
- **选帧边界**：两套均**未包含两个静息帧**（首尾都取静息会得到重复帧，步长比会升到 3.19–17.96），也均不含平台期重复帧。
- **文件属性**：两套都是 **960×960 RGB**（未抠图），与解码源帧**逐字节一致**（已校 SHA-256），未缩放/平移。清单 `coward_idle_candidates.json`。
- **评审材料**：`CANDIDATES_SIX_REVIEW.png`、`CANDIDATES_FOUR_REVIEW.png`（编号 + 步长）；数据 `pose_groups.json`、`phase_candidates.json`、`phase_even.json`、`chosen_frames.json`、`frame_metrics.json`；97 帧原片在 `frames_clean_raw/`。
- **已定**：用户选择 **6 帧**，抠图已完成（见顶部最新停点）；4 帧候选保留在 `candidates_four/` 仅作追溯，不抠图。
- **未做**：未抠图、未组装 Clip、未接入 Unity、未进入 Play。

### 历史停点：CowardIdle 循环视频 v1 已生成、下载并获用户验收（2026-10-17）

- **本次动作**：按 §0.13 已完稿词原样提交**一次** `POST /v1/vids/create`（英文正文 + 23 条负面词合并进同一 prompt 字段，两张普通 `reference_image`，未混 `first_frame`/`last_frame`）。未改词、未换图、未降参数。
- **Task ID**：`task_d9b8cc176ff54dd7a70be22663228710`；accepted UTC `2026-10-17T10:04:05+00:00`；create HTTP 200；两次上传均 HTTP 200；**只创建一次**（`create_attempts=1`）。
- **轮询与结果**：限时轮询 11 次，于 `2026-10-17T10:07:49+00:00` 返回 `success`；`usage.completion_tokens=87300`、`cost.spend=0.48888`（币种未注明）。
- **下载中断与修复（已解决，未重发任务）**：第一次下载阶段报 `ModuleNotFoundError: No module named 'av'`——提交脚本借用了 SVL 那个 Python（无 PyAV）。生成本身已成功，故**没有重建任务**，改用带 PyAV 的解释器查询同一 task 并下载：四段 Range 均 206、ETag 一致，全帧解码通过。
- **成片（已获用户验收）**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_idle_v1.mp4`，1,706,747 bytes、SHA-256 `84c7e29b13bd94a959d2abfd8dcc3c536aadc1ac0b0ff5b320b34163fe62fd2f`；实测 **960×960 / 24fps / 97 帧 / 容器时长 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨**。报告：同目录 `.download.json`。
- **实际提交内容**：§0.13 英文正文（SHA-256 `1991cb0a…06c7`）+ 23 条负面词（`c19fbd97…7228`）合并为 `21edfc10…4d76`。上传两图 SHA 与文档记录逐一校对一致（图1 `3fd64c93…a6c0`、图2 `8b5355be…e736`），均四角精确 `(0,177,64)`、max_dev 0。
- **循环闭合客观量测（仅辅助，不代替用户目检）**：首帧 f000 与末帧 f096 包围盒分别为 `[348,298,649,703]` 与 `[349,298,649,703]`，差 ≤1 px；主体像素 63,213 vs 63,178；首尾差异 **6,721 px**（占主体约 10.6%），而中段 f000 vs f048 为 49,057 px——说明首尾确实回到同一姿态，**但**不是逐像素相等，接缝仍由用户目检判定。
- **呼吸幅度**：主体像素随时间 63,176–65,788，摆幅 2,612 px（**4.13%**）；首尾两周期同相位 f024 vs f072 差 22,842 px，说明两次呼吸不完全重复——若要做循环需按单周期截取。
- **视频片基**：首帧四角中位 `(0,160,63)`，与上传 `(0,177,64)` 不一致（生成器不保上传色值，保色族）；要抠图时按既有流程另取容差。
- **检查材料（仅文件诊断，不是选帧）**：`Library/Locus/tmp/enemy1021_coward_idle_video_v1/` 下 `COWARD_IDLE_DENSE_SHEET.png`（每 8 帧）、`COWARD_IDLE_FIRST_LAST.png`（f000 vs f096 并排 1:1）、`COWARD_IDLE_PREVIEW_v1.gif`（全 97 帧，约 41ms/帧，仅评审，不定义 Clip）、`loop_metrics.json`、`frames_clean_raw/`（97 帧）、`runtime.json`、`job.json`、`download_manifest.json`。
- **未做**：未选帧、未抠图、未组装 Clip、未接入 Unity、未进入 Play。
- **当时状态**：用户已验收循环视频内容并要求挑选循环帧；本侧随后选出 6 帧与 4 帧两套候选，用户选定 6 帧。

### 历史停点：CowardIdle 循环视频词写入 §0.13（2026-10-17）

- **用户验收**：用户于 2026-10-17 明确“**已验收**”，呼吸最高点关键帧 v1 通过；重评分中的底色偏差（生成图四角约 `(2,178,67)`）视为已接受，**不重发、不改图**。
- **当时状态**：本节词与两张绿幕副本已就绪；本侧随后按该节提交一次并完成下载，用户已验收。

#### 已完成（已验收）的本项产物

- **本次动作**：按 §0.12 已完稿词原样提交**一次** `POST /v1/images/edits`（英文正文 + 22 条负面词合并进同一 prompt 字段），只上传一张源姿态副本。HTTP 200，POST 68.859 秒；usage input/output/total = 1365/7024/8389，cost 与 task id 未返回，不推断费用。未重发、未改词、未降参数。
- **产物（已验收）**：`C:/Users/steam/Pictures/gptGen/enemy1021_coward_idle_peak_keypose_v1.png`，953,009 bytes、SHA-256 `8b6656bcbe15ed016c94fcf44ad995f2d397d1f97702bf2c4eefc7bdbebb569d`、1024×1024 RGB、ColorType2、解码通过。同目录 `.request.json`（实际提交合并 prompt SHA-256 `7cd71799…6fbb`）、`.response.json`、`.result.json`、`.submitted_once.json`。
- **上传副本（G1）**：源帧 `06_frame_144.png` 实测底为 `(0,142,59)`（非 00B140），故另存 1024 上传副本 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`：960 画布中心补边到 1024（偏移 32,32）、外部背景与 2px 边缘环统一为 `(0,177,64)`、四角精确 `(0,177,64)` std 0、**主体内部改动 0 px**、**角色未缩放**；原帧未动。记录 `g1_upload_copy.json`。
- **客观量测（与上传副本同口径，仅辅助，不代替用户目检）**：主体包围盒源 `320×438`、生成 `320×430`；**宽比 1.000、高比 0.982、像素比 0.984**（词要求尺寸差 ≤5%，达标）；主体底边差 **−2 px**、顶边差 **+6 px**；双靴质心位移 `(−2.9, −0.8)`；画面左侧高举手指尖区质心位移 `(−0.1, −2.3)`、画面右侧胸前手区 `(−4.0, +1.3)`——均在词要求的“每只手 ≤ 约 20 px”内；红缨质心位移 `(−12.8, +7.3)`（词里本就要求“轻轻摆动”）；四边最小留白 **26.8%**（要求 ≥15%，达标）。
- **底色实测**：生成图四角中位 `(2,178,67)/(2,178,66)/(2,178,67)/(2,177,66)`、dev std 0.54–0.66，**不是**精确 `(0,177,64)`（与批量历史一致：生成器不保上传色值，但保色族）。若后续要抠图，按既有流程另取容差，不直接拿本图当透明素材。
- **对照材料（仅本地）**：`Library/Locus/tmp/enemy1021_coward_idle_peak_v1/` 下 `UPLOAD_COPY_REVIEW.png`（源帧 vs 上传副本）、`COWARD_PEAK_V1_VS_SOURCE.png`（同区域 1:1 并排）、`COWARD_PEAK_V1_SOURCE_2X.png`；脚本 `build_upload_copy.py`、`build_job.py`、`generate_coward_peak.py`；数据 `g1_upload_copy.json`、`job.json`。
- **未做**：未组装 4 帧循环、未做视频、未抠图、未接入 Unity、未进入 Play。按 §0.12，本图只是循环中间的呼吸高点，首尾仍用 `06_frame_144` 原始姿态。
- **写词侧接手时的最小上下文**：① 身份基准 = 破盾末帧 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldBreak6.png`（无盾、双手张开于脸侧与胸前）；② 本批已验收的呼吸高点图可作为 CowardIdle 循环的中间姿态参照；③ 四项读感与约束逐项见 `plan/enemy1021-coward-family-plan.md` §3.2 与 §4；④ 词写入本文件后由本侧发起生成。

### 历史停点：持盾死亡 `Enemy_1021_Dead` 已接入并获用户游戏验收（2026-10-17）

- **授权与范围**：用户明确“请按照交接内容接入 1021 敌人的 dead 动画”。范围只做到“补边＋Clip＋Controller 接线＋静态核验”，未进入 Play、未采样动画、未运行自检；用户随后于 2026-10-17 开游戏验收通过。
- **素材来源**：已验收透明帧 `Library/Locus/tmp/enemy1021_dead_video_v2/dead_3frames_matted_v1/`（`dead01_f044` / `dead02_f046` / `dead03_f072`，960×960 RGBA，T=80），逐帧 SHA-256 与清单 `dead_3frames_matted_v1.json` 一致。
- **补边与导入**：按固定偏移 `(74,91)` 零重采样补边到 1108×1108（全局缩放 1.0）；粘贴区与源帧逐像素差 **0**，画布外非零像素 **0**，半透明像素 **0**。新增 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_dead1..3.png`，导入设置与 `Enemy_1021_hitFlash1.png` **逐项一致**（Sprite/Single/PPU16/Point/Uncompressed/无 Mipmap/Tight/Center），打开设置后的 .meta 与参照 .meta 差 **0 行**；Unity 实际导入 1024×1024、Sprite PPU 14.787，与既有动作同口径。
- **Clip**：`Assets/Animations/Enemy_1021_Dead.anim`，30fps、0.6s、Clamp、非循环；精灵键 0/0.2/0.4＋末帧保持键 0.5667；`ShieldVisual` 显式 null 键；无 Transform 曲线、无 AnimationEvent。
- **接线**：仅替换 `Enemy_1021.controller` 的 `Dead` motion（`Enemy_102_Dead` → `Enemy_1021_Dead`）；其余 14 个状态 motion、18 条转场（含 AnyState→Dead `Dead` 触发）与 6 个 Trigger 参数均未变。代码入口未改：`Enemy.PlayDeadVisual()` 仍按名 `Play("Dead")`。Prefab、脚本、场景与关卡配置哈希不变（102 个保护文件里只有控制器变化）。
- **时长契约**：Dead 不由玩法字段计时，视觉段由 `Enemy.DeathBounceAndFall()`（0.3s 弹起＋0.8s 掉落）控制；3 帧在 0.6s 内均分为 0/0.2/0.4 并保持末帧，与 1011_Dead 的 0.6s 口径一致。
- **核验留档**：`Library/Locus/tmp/enemy1021_dead_deploy_v1/asset_verification.json`、`preparation_report.json`、`baseline_hashes.json`。
- **未验证**：无盾胆怯死亡（`CowardDead`）仍为 102 占位，不在本次范围。
- **验收结果**：用户于 2026-10-17 回答“已验收”，持盾死亡游戏表现通过；`Library/Locus/tmp/enemy1021_dead_deploy_v1/asset_verification.json` 已同步 `user_game_acceptance=accepted`、`game_acceptance_pending=false`。
- **当时状态**：持盾死亡已完成；`CowardIdle` 的第一张关键帧词已写入 §0.12。

### 0.12 CowardIdle 呼吸最高点 · 关键帧生成词（2026-10-17；产物 v1 已获用户验收，本节已执行完毕）

> 本节只交付一张静帧的生成词。它是胆怯待机循环的呼吸最高点，不是完整循环，也不是视频词。**本节词已于 2026-10-17 提交一次，产物见顶部最新停点；不要再提交第二次。**

#### 上传

- 源姿态：`C:/Users/steam/Pictures/doubaoVideo/enemy1021_shield_break_selected_6_frames_v2/06_frame_144.png`。这是已验收破盾视频的原始末帧，也是当前静态 `CowardIdle` 的姿态来源。
- 提交前另存 1024×1024 上传副本：角色不缩放，只把外部绿幕收成 RGB(0,177,64)。原帧不改。
- 不上传 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldBreak6.png`。它已经按 1.20 缩放并补边，不是生成参考。
- 不上传旧的 `enemy1021_shield_break_coward_target_keypose_v1.png`，也不上传持盾 Idle。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst` |
| 接口 | `POST https://api.muskapis.com/v1/images/edits` |
| quality / input_fidelity | max / high |
| size / background / format / n | 1024x1024 / opaque / png / 1 |
| 建议输出 | `C:/Users/steam/Pictures/gptGen/enemy1021_coward_idle_peak_keypose_v1.png` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
Edit the provided image into one still 2D game sprite. This is the highest point of a nervous breathing loop, not a new action and not a video. Keep the same unshielded soldier, the same front view, and the same planted stance.

Both open palms stay toward the camera. The screen-left hand stays high beside the face, and the screen-right hand stays in front of the chest. Fingers stay spread. There is no shield and no weapon in either hand.

Change only the breath: both shoulders rise and pull slightly inward, the neck shortens a little, and the helmet ducks by only a few pixels. The fingers open a little wider. The red plume shifts slightly. Both boots, both knees, and the root stay where they are. Each hand moves by no more than about 20 pixels. The full-body size stays within 5 percent of the source.

Do not enlarge, shrink, or stretch him. Do not turn the body, step, crouch lower, attack, or flinch. Flat chroma green RGB(0,177,64), no floor, shadow, scenery, effects, text, or UI. Keep at least 15 percent empty margin on every side.
```

中文对照（不提交）：

> 把提供的图改成一张静帧，是胆怯待机呼吸的最高点，不是新动作，也不是视频。同一个无盾士兵，同一个正面，同一套站姿。
>
> 两只手掌继续朝向镜头。画面左侧的手仍高举在脸侧，画面右侧的手仍挡在胸前，手指张开。两只手里都没有盾，也没有武器。
>
> 只改呼吸：双肩略上提并内收，脖子略缩短，头盔只低下几个像素。手指张得稍大一点，红缨轻轻摆动。双脚、双膝和身体根部保持原位。每只手的移动不超过约 20 像素，全身尺寸与原图相差不超过 5%。
>
> 不得放大、缩小或拉长，不得转身、迈步、再蹲低、攻击或做受击。背景为纯色绿幕 RGB(0,177,64)，无地面、阴影、场景、特效、文字和 UI。四边至少留 15% 空边。

负面词（与英文词一起提交）：

```text
no shield, no weapon, no closed fist, no lowered hands, no guard stance, no attack, no hit reaction, no step, no walk cycle, no deeper crouch, no enlarged character, no stretched limbs, no body turn, no side view, no floor, no shadow, no scenery, no effects, no text, no UI, no cropped limb, no body leaving the frame
```

#### 这张图之后做什么

生成后先交用户看单图。通过后，它只作为循环中间的呼吸高点；循环首尾仍用 `06_frame_144` 的原始姿态。不要把这一张图当成完整的 4 帧循环，也不要据此直接做视频。

#### 本次执行结果（2026-10-17，已提交并获用户验收）

| 项 | 值 |
|---|---|
| 产物 | `C:/Users/steam/Pictures/gptGen/enemy1021_coward_idle_peak_keypose_v1.png` |
| 大小 / SHA-256 | 953,009 bytes / `8b6656bc…569d` |
| HTTP / POST 耗时 | 200 / 68.859 秒 |
| usage | input/output/total = 1365/7024/8389 |
| cost / task id | 未返回（不推断费用） |
| 实际提交 prompt SHA-256 | `7cd71799…6fbb`（正文 `8d85157a…0e36` ＋ 负面词 `ad092a98…1fdf` 合并） |
| 上传副本 | `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png`，四角精确 `(0,177,64)` std 0，主体内部改动 0，角色未缩放 |
| 宽比 / 高比 / 像素比 | 1.000 / 0.982 / 0.984（要求尺寸差 ≤5%，达标） |
| 四边最小留白 | 26.8%（要求 ≥15%） |
| 生成图底色 | 四角中位约 `(2,178,67)`，非精确 `(0,177,64)` |

未组装循环、未做视频、未抠图、未接入 Unity。视频交接改用 §0.13。

### 0.13 CowardIdle 循环视频 · 已按本节提交一次、下载并获用户验收（2026-10-17）

> 本节只做胆怯待机循环。受击、击飞和死亡不在这次视频里。**本节已于 2026-10-17 原样提交一次，成片已下载、完整解码并获用户验收；不要再提交第二次。** 结果与文件见顶部最新停点。

#### 两张实际上传图

| 图 | 上传路径 | 职责 |
|---|---|---|
| 图1 | `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/01_coward_idle_peak_source_green_00B140_1024.png` | 循环首尾的无盾站姿。SHA-256 `3fd64c935cd7b5adf562d2729899430e2156120edf0575e969e2faf95146a6c0`。四角精确 `(0,177,64)` |
| 图2 | `Library/Locus/tmp/enemy1021_coward_idle_video_v1/02_coward_idle_peak_green_00B140_1024.png` | 呼吸最高点。SHA-256 `8b5355be81986999837da4d62427f12bcae492087637b94cb7e7b104ddaee736`。四角精确 `(0,177,64)`、std 0 |

两张都用普通 `reference_image`。不要上传呼吸高点原图，它的底色是 `(2,178,66)`。也不要上传已补边的 `Enemy_1021_shieldBreak6.png` 或持盾 Idle。

图2 只替换了外部背景 977,318 px 和混色边缘 546 px，主体内部改动 0，原图未动。记录见 `Library/Locus/tmp/enemy1021_coward_idle_video_v1/g1_peak_upload_copy.json`。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 4 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无 / 无 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_idle_v1.mp4` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
This is a seamless 2D game-sprite idle loop, not a cinematic. The same unshielded soldier stays planted and faces the camera. He is nervous after losing his shield. This is not an attack, not a hit, and not a walk.

Image 1 is both the first frame and the last frame. Image 2 is only the highest point of one breath. Use both as ordinary references. Keep the same flat chroma-green background in every frame.

LOOP:
0.00-0.20s: hold image 1.
0.20-1.00s: breathe in to image 2. Both shoulders rise and pull slightly inward, the neck shortens a little, and the red plume shifts slightly. Both open palms stay toward the camera. The screen-left hand stays high beside the face, and the screen-right hand stays in front of the chest.
1.00-1.80s: breathe out and return exactly to image 1.
1.80-4.00s: repeat that same small breath and return to image 1 again. The last frame must match the first frame.

LOCKED PARTS: both boots, both knees, and the body root stay planted. No step and no sideways drift. There is no shield and no weapon in either hand. Do not enlarge, shrink, turn, or redraw the character. Keep at least 15 percent empty margin on every side.

CAMERA AND BACKGROUND: locked camera, no zoom, pan, tilt, or shake. No floor, shadow, scenery, effects, text, or UI.
```

中文对照（不提交）：

> 这是一段可循环的 2D 游戏精灵待机，不是电影。同一个无盾士兵站在原地，正面朝向镜头。他失去盾后有些紧张。这不是攻击，不是受击，也不是走路。
>
> 图1 同时是首帧和末帧。图2 只是一次呼吸的最高点。两张图都只作普通参考。每一帧保持同一平面绿幕。
>
> 循环：0.00–0.20 秒保持图1；0.20–1.00 秒吸气到图2，双肩略上提并内收，脖子略缩短，红缨轻轻摆动；两只手掌继续朝向镜头，画面左侧的手仍在脸侧，画面右侧的手仍在胸前；1.00–1.80 秒呼气，精确回到图1；1.80–4.00 秒重复同一次小呼吸，再次回到图1。最后一帧必须和第一帧一致。
>
> 双脚、双膝和身体根部保持原位。不迈步，不横向漂移。两只手里都没有盾，也没有武器。不得放大、缩小、转身或重画角色。四边至少留 15% 空边。镜头锁定。无地面、阴影、场景、特效、文字和 UI。

负面词（并入同一 prompt）：

```text
no shield, no weapon, no closed fist, no lowered hands, no attack, no hit reaction, no step, no walk cycle, no sideways drift, no body turn, no side view, no enlarged character, no stretched limbs, no camera movement, no floor, no shadow, no scenery, no effects, no text, no UI, no cropped limb, no body leaving the frame, no pose change at the last frame
```

#### 发起后

下载并完整解码，再交用户看视频。选帧、抠图、对齐和接入分别验收，不在这一次里连续做完。

#### 本次执行结果（2026-10-17，已提交、下载并获用户验收）

| 项 | 值 |
|---|---|
| Task ID | `task_d9b8cc176ff54dd7a70be22663228710` |
| accepted UTC | `2026-10-17T10:04:05+00:00`（create HTTP 200） |
| 轮询 | 11 次后于 `2026-10-17T10:07:49+00:00` 返回 `success` |
| usage / cost | completion_tokens 87300；`cost.spend=0.48888`（币种未注明） |
| 成片 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_idle_v1.mp4` |
| 大小 / SHA-256 | 1,706,747 bytes / `84c7e29b13bd94a959d2abfd8dcc3c536aadc1ac0b0ff5b320b34163fe62fd2f` |
| 实测规格 | 960×960 / 24fps / 97 帧 / 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨 |
| 提交 prompt | 正文 `1991cb0a…06c7` + 负面词 `c19fbd97…7228` 合并为 `21edfc10…4d76` |
| 首尾闭合 | 首尾差异 6,721 px（主体约 10.6%）；包围盒差 ≤1 px；中段对照 49,057 px |
| 呼吸幅度 | 主体像素 63,176–65,788，摆幅 4.13%；两周期同相位差 22,842 px |
| 视频片基 | 首帧四角中位 `(0,160,63)` |

下载阶段曾因解释器缺 PyAV 中断一次；生成本身成功，故未重建任务，改用带 PyAV 的解释器查询同一 task 下载。未选帧、未抠图、未组装 Clip、未接入 Unity。

### 下一项（已排期）：胆怯家族四项 · CowardIdle 视频已生成（2026-10-17）

- **用户指令**：“先做胆怯家族的四项动画”，随后要求“更新交接文档让另一边写提示词”。因此**生成词的起草由写词侧负责**；本侧按词发起生成、取回产物、选帧、抠图并回写验收状态；**对齐/补边/接入不由本侧做**。
- **写词侧可以从这里接手**：
  - 规格与合约：`plan/enemy1021-coward-family-plan.md`（§3 身份/几何/抠图/命名；§3.2 四项读感；§4 时长与出边约束；§6 待拍板项）。
  - 身份基准：破盾末帧 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldBreak6.png`（无盾、双手张开于脸侧与胸前，当前 `CowardIdle` 兜底姿态），四项都要能从它自然过渡。
  - 已验收参照：CowardIdle 呼吸最高点 `C:/Users/steam/Pictures/gptGen/enemy1021_coward_idle_peak_keypose_v1.png`（1024×1024，四角约 `(2,178,67)`），可作循环中间姿态；循环首尾仍用 `06_frame_144` 原始姿态。
  - 上传边界与 G1：源帧底不是 00B140（实测 `(0,142,59)`），上传前必须另存 1024 同色副本；现成构建脚本 `Library/Locus/tmp/enemy1021_coward_idle_peak_v1/build_upload_copy.py`。
  - 不得上传：已部署的 `Enemy_1021_shieldBreak6.png`（已 1.20 缩放并补边，不是生成参考）、旧的 `enemy1021_shield_break_coward_target_keypose_v1.png`、持盾 Idle。
- **四项与现状**：`CowardIdle`（现为 `Enemy_1021_CowardIdle_Hold.anim` 静态兜底）、`CowardHit`（`Enemy_102_CowardHit`）、`CowardLaunched`（`Enemy_102_CowardLaunched`，用的是死亡帧）、`CowardDead`（`Enemy_102_CowardDead`）。四项状态在控制器里均已存在，接入只需替换 motion。
- **制作侧需先知道的三个约束**：① 胆怯形态**没有 Walk 动画**，被补齐推移时播的就是 `CowardIdle`，循环不能依赖横向位移；② `CowardHit` 的 clip 长度就是受击表现总时长（`CowardHit → CowardIdle` exit 1.0）；③ `CowardLaunched` 有 `→ CowardIdle` exit 1.0 出边，所以默认按**2.0s 长保持 clip**做（与 `launchDuration=2.0` 对齐），若要改成代码驱动需用户拍板。
- **素材未到时本侧不做什么**：不自造帧、不建空 Clip、不改控制器、不动机制；也不代写生成词。

### 历史停点：持盾死亡透明素材 v1（3 帧 / T=80）已获用户验收（2026-10-17）

本节优先于下文所有历史停点。

- **用户验收**：用户于 2026-10-17 明确“**已验收**”，3 帧透明结果（`dead_3frames_matted_v1/`）通过；重评分中的遗留绿像素（green>8 共 403/508/495 px）视为已接受，**不重抠、不改图**。
- **本侧任务边界（用户明确）：对齐、补边、接入均不由本侧负责**。透明素材验收即本侧交付终点；后续由负责接入的一侧按 `skill/workflows/action-to-unity-deployment.md` 做尺寸/锚点对齐、1108 补边、Clip 与第一关接入。本侧不要自动开工，也不要以“交接建议”的口吻替另一侧派活。
- **用户决策（供接入侧遵循）**：① 采用 **3 帧**（不用 5 帧）；② 抠图**容差 80**；③ 开头与 idle 一致的帧无意义，从 **f044 起**选帧。
- **为何只能是 3 帧（已量化）**：f044…f096 只有 **3 个真姿态**——A2（f044）、B（f045–f048）、C（f049–f096）。判据为“平坦区差异”（排除边缘重编码噪声）：f045 vs f048 = **0 px**；f049 vs f096 = 1,000 px（90.3% 为边缘噪声）；而 A2 vs B = 141,696 px、B vs C = 134,141 px。取第 4 帧只能重复某姿态。
- **选定帧**：`dead01_f044`（1.833s）、`dead02_f046`（1.917s）、`dead03_f072`（3.000s）。
- **抠图配方**：SVL Chroma 自动取键（实测底 `(1,157,60)`、coloured screen）+ **T=80** + softness 16 + halo 1 + 半透明转不透明；**未开启平滑**。
- **交付目录（最终透明素材）**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_dead_video_v2/dead_3frames_matted_v1/`，三张 960×960 RGBA：`dead01_f044.png`、`dead02_f046.png`、`dead03_f072.png`；清单 `dead_3frames_matted_v1.json`（含逐帧 SHA-256）。原始 RGBA 在 `keyed_ui_t80/`（同名）。
- **验收指标（逐帧实测）**：半透明像素 **0**；透明区非零 RGB **0**；幕布残留 **0**；封闭孔 **0**；可见像素 156,451 / 153,627 / 136,853；距轮廓 ≥4px 的内部 RGB 改动：f044 **0 px**、f046 1 px（ΔRGB≤12）、f072 6 px（其中 >8Δ 的仅 2 px）——即主体颜色基本原样。
- **遗留的绿像素**：可见区 `g−max(r,b) > 8` 仍有 403 / 508 / 495 px（占可见 0.26%–0.37%），属 SVL 去溢色的既有边界，白/深底目视无绿边；不声称严格零绿。
- **评审材料**：`DEAD_MATTED_DELIVERY_REVIEW_v1.png`（上白下深双底）、`DEAD_MATTED_3_WHITE.png`、`DEAD_MATTED_3_DARK.png`、`DEAD_MATTED_3_CHECKER.png`、`ZOOM_dead01_f044.png` / `ZOOM_dead02_f046.png` / `ZOOM_dead03_f072.png`（2× 细节）、`DEAD_MATTED_PREVIEW_v1_0p6s.gif`（3 帧 ×200ms，仅评审，不定义 Clip）。
- **接入侧向前需要的取值（供参考，不由本侧执行）**：三帧均为 960×960 RGBA、未挑放大缩小、未平移；若要沿用已验收动作的 1108 画布约定，固定偏移与导入参数见 `skill/workflows/action-to-unity-deployment.md` 与本文件 §4.3；帧序为 `dead01_f044` → `dead02_f046` → `dead03_f072`。
- **未做（且不在本侧范围）**：尺寸/锚点对齐、1108 补边、Clip 创建、Unity 部署均未由本侧执行；本侧未改任何 Unity 资产、未进入 Play。
- **本侧范围**：交付到透明素材为止；未做对齐、补边、Clip 与接入。

### 历史停点：持盾死亡选帧候选 v1（5 帧，已被 3 帧方案取代；2026-10-17）

> 本节为历史记录。用户随后定为 3 帧、从 f044 起、T=80，见顶部最新停点。下列 5 帧候选**不再采用**，仅保留追溯。

- **重要发现（需用户知悉）**：这支出片**只有 5 个真正不同的姿态，且姿态之间是 1 帧硬切、没有渐变过渡**。逐帧量测：97 帧全部字节唯一，但包围盒只落在 5 组；帧间变化集中在 `f033→f034`（15.57%）、`f044→f045`（22.59%）、`f048→f049`（22.35%）三处跳切。
- **姿态分组（按真实变化 ≥4000 px 划分）**：`f000–f033` 站立（34 帧）；`f034–f036` 受击起手（3 帧）；`f037–f044` 身体张开中段（8 帧）；`f045–f048` 展开加强（4 帧）；`f049–f096` 终态保持（48 帧）。
- **选帧规则**：每个姿态组只取**1 帧**，取该组内对相邻帧最稳定的一帧；不机械等距取帧。
- **当时的 5 帧候选（已废弃）**：

| # | 帧 | 时间 | 填充率 | 宽高比 | 说明 |
|---|---|---|---|---|---|
| 1 | f010 | 0.417s | 0.617 | 0.746 | 站立持盾，开头（后判定与 idle 重复、无意义） |
| 2 | f034 | 1.417s | 0.486 | 1.039 | 受击，身体开始张开 |
| 3 | f044 | 1.833s | 0.486 | 1.039 | 与 #2 近似（头部/躯干微调） |
| 4 | f048 | 2.000s | 0.451 | 1.213 | 展开加强、盾已离开手 |
| 5 | f051 | 2.125s | 0.379 | 1.598 | 终态，与已验收图2 同量级 |

- **交付目录（仅追溯）**：`Library/Locus/tmp/enemy1021_dead_video_v2/candidates_v1/`；评审表 `SELECTED_REVIEW_v1.png`、预览 `SELECTED_PREVIEW_v1_0p6s.gif`、`SELECTED_PREVIEW_ALT4_0p6s.gif`；原始 97 帧在 `frames/clean_raw/`。GIF 只用于评审，**不定义 Unity Clip**。
- **为何被取代**：用户指出开头帧与 idle 一致、无意义，且有效帧不足 5；量化确认 f044 之后只有 3 个真姿态。

### 历史停点：持盾死亡视频 v2 已提交、下载完成并获用户验收（2026-10-17）

- **本次动作**：按 §0.11 已完稿词原样提交**一次**（未改词、未换图、未降参数），两张普通 `reference_image`，无 `first_frame`/`last_frame`。
- **Task ID**：`task_215c88d7209f46bc8e5b36a528c225e2`；accepted UTC `2026-10-17T03:12:03.170732+00:00`，create HTTP 200 / 2.135 秒。两次上传均 HTTP 200（图1 4.76 秒、图2 5.72 秒）。
- **轮询与结果**：创建后 12 次限时轮询均 `generating`（queued×2 后转 generating）后停止本地等待；随后仅查询同一任务，UTC `2026-10-17T03:16:07.205752+00:00` 观察到 `success`。接口 `usage.completion_tokens=87300`、`cost.spend=0.48888`（币种未注明）。**未创建第二个任务、未重试。**
- **成片（已获用户验收）**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.mp4`，2,326,004 bytes、SHA-256 `c2b444a358d246f2ac677a63bfdb6f0c2327e28ca96300836c9d1a2bf47e9095`；实测 **960×960 / 24fps / 97 帧 / 容器时长 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨**；四段 Range 均 206 并按 ETag 校验，MP4 结构 `ftyp/uuid/free/mdat/moov` 与**全帧解码通过**。下载与解码报告：`C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.download.json`；请求台账：同目录 `enemy1021_dead_v2.request.json`。
- **实际提交内容**：§0.11 英文正文（SHA-256 `f1d9ff62…0d4f`）+ 39 条负面词（`19bd7829…f9ce`）合并进同一 prompt 字段（合并后 `086e7c9c…12a9`），无独立负面字段。上传图1 SHA `d398a1eb…40de`、图2 G1 副本 SHA `16bfc410…83cf`，与 §0.11 记录一致。
- **检查帧（仅文件诊断，不是选帧）**：`Library/Locus/tmp/enemy1021_dead_video_v2/contact_sheet.png` 取 0/24/48/72/96 帧；`first_frame.png`、`last_frame.png` 同目录。按纪律这不算动作验收，也不定义 Clip。
- **当时状态**：用户于 2026-10-17 验收本视频并授权开始选帧。
- **历史停点（同一任务的前一状态）**：图2 重做版 v3 已于 2026-10-16 获用户验收（`enemy1021_dead_airborne_keypose_v3.png`，SHA-256 `dba1290c…9785`）；三项达标边沿（留白 8.8%/9.7%、填充率 0.376、盾面积 0.64×）均已接受。

### 历史停点：图2 重做版 v3 已生成并获用户验收（2026-10-16）

- **用户验收**：用户于 2026-10-16 明确“**已验收该图片**”，即 §0.10 的图2 重做版 v3 通过。代理量测中的三项达标边沿（左右留白 8.8%/9.7%、填充率 0.376、盾面积 0.64×）**均视为已接受**，不因此重发、不改图。
- **本对话的任务边界（用户明确，2026-10-17 更正）**：本对话**不需要写生成词**（生图与视频词由另一方对话负责）；但**会出现选帧与抠图的工作要求**，这两类工作由本侧执行。图片与视频的验收由用户做；本侧不验收画面、不自行重发收费任务。
- **本次动作**：按 §0.10 已完稿词（英文正文与 40 条负面词合并，无独立负面字段）提交**一次** `POST /v1/images/edits`，只上传图1。HTTP 200，POST 108.071 秒；usage prompt/completion/total = 667/946/1613，cost 与 task id 未返回，不推断费用。未重发、未改词、未降参数；未改任何 Unity 资产、未进入 Play 或运行自检。
- **产物（已验收）**：`C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v3.png`，1,122,630 bytes、SHA-256 `dba1290c0b78b3e4df861fe63f53b2e0d83c973ba133a5587f87fe2f84669785`、1024×1024 RGB、ColorType2、解码通过。同目录保存 `.request.json`（实际提交合并 prompt SHA-256 `eb06952c…f140`，英文正文 `721e7814…587c`、负面词 `e55a48fe…92ca`）、`.response.json`、`.result.json`、`.submitted_once.json`。
- **背景实测**：四角 64×64 中位约 `(1.6, 181.1, 72.4)`，**不是**图1 的精确 `(0,177,64)`，与 v2 的 `(2,178,68)` 同类。若进入视频，仍按 §0.9 的 G1 规则另存同色绿幕上传副本并实测；不得直接拿本图当上传副本。
- **§0.10 四项通过标准（代理客观测量，不代替用户目检）**：① 全身宽高比 **1.60**（要求 >1.4，达标）；② 填充率 **0.376**（目标 ≈0.30–0.35，略高于上限，已验收）；③ 脱手成立——主体分成 **2 个连通块**，身体块 `[318,244,924,765]` 与盾块 `[90,317,338,627]` 之间最小距离 **14 px**，中间是背景；④ 部件未被放大——盾的像素面积约为图1 的 **0.64**，面积换算线性尺度约 **0.80**，即偏小而非膨胀。
- **与 1011 已验收死亡同口径对照**：v3 全身填充率 0.376 vs 1011 dead1/3/6 的 0.345/0.257/0.256；v3 主轴 87.7°（全身，含盾）而仅算身体块为 **79.8°**，落在 §0.10 要求的 50°–80° 内，与 1011 dead3 的 71.5°、dead6 的 81.6° 同量级。v2 的身体倾角是 28.5°。
- **对照材料（仅本地）**：`Library/Locus/tmp/enemy1021_dead_keypose_v3/` 下 `KEYPOSE_V3_SOURCE_COMPARE.png`（图1/v1/v2/v3 同画布 50% 并排）、`KEYPOSE_V3_VS_1011_DEAD.png`（1011 dead3 与 v3 并排）、`SHIELD_WINDOW_100PCT_COMPARE.png`（盾 100% 实像素）、`HEAD_100PCT_COMPARE.png`（头盔与红缨 100%）；数据 `v3_metrics.json`、`v3_body_metrics.json`、`v3_shield_oriented.json`、`v3_gap_check.json`；脚本 `build_jobs_v3.py`、`generate_dead_keypose_v3.py`、`measure_v3.py`、`body_metrics_v3.py`、`shield_oriented_v3.py`、`gap_check_v3.py`。
- **绿幕已处理**：v3 原图四角中位 `(2,181,72)`，不能直接上传。另存上传副本 `Library/Locus/tmp/enemy1021_dead_video_v2/02_dead_v3_green_00B140_1024.png`，四角精确 `(0,177,64)`、std 0，SHA-256 `16bfc41086e8a1f5674c6236f04e941b7f4bacd96072e23e6ac4f29e70a983cf`。只替换外部背景和 856 个混色边缘像素；主体内部改动 0；原图未动。记录见同目录 `g1_plate_unification.json`。
- **当时状态**：随后由发起侧按 §0.11 提交一次（task `task_215c88d7209f46bc8e5b36a528c225e2`），已下载并获用户验收；未重发。

### 历史停点：持盾死亡图2重做方案已写入 §0.10（2026-10-16）

- **当前方案**：重做图2。用户随后明确：**死亡动作里没有禁止盾脱手。** 新方案写入 §0.10，画面左侧那只手松开，木盾离开手并完整留在画面里。旧的“盾必须一直握着”是错误限制，不再使用。
- **当时状态**：后续按 §0.10 提交并最终产出已验收的 v3；本节词未再使用。
- **本次已完成**：按 §0.8 定稿词提交一次 `POST /v1/vids/create`（HTTP 200、task `task_800f25278957424d9c5b911e75bfca2c`），轮询 12 次仍 generating 后停止本地等待，随后单次查询返回 `success`（cost.spend=0.48888），四段 Range 下载并完整解码通过。**但两轮反馈均验收不通过。**本轮未重发、未改写词、未改参数、未改任何 Unity 资产。
- **用户否决理由（原话要点，以此为准；第二轮优先）**：① “结尾姿势不符合预期”；② “总体来说几乎没有什么动作”；③ **“动作毫无张力，没有被击飞到底的感觉”**；④ **“与1011的dead动画语义完全不符”**；⑤ **“姿态僵硬像是被催眠倒下”**。
- **根因（已定为重做图2 的依据）**：代理实测显示，**问题不在词而在目标图2**。1011 已验收死亡帧的填充率约 0.30（宽度扩到 Idle 的 1.45–1.52 倍，四肢张开）；而图2 v2 当年正是为治“旋转 45°/放大 1.33×”被要求“偏离竖直 ≤15°、包围盒变化 ≤10%”，它是一张**竖着、收着的**图。视频词又继承该限制并叠“末帧锁图2”，只能得到“原地小幅转身”。详解与实测见 §0.9。
- **新图2 的目标**：身体像 1011 dead 那样张开，参考量级为宽高比大于 1.4、填充率约 0.30–0.35，主轴偏离竖直约 50°–80°。张开来自甩开的四肢和脱手的盾，不靠放大头盔、胸甲、盾或靴。完整词见 §0.10。
- **成片产物（保留作失败样本，不得当已验收素材）**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v1.mp4`，2,056,869 bytes、SHA-256 `8e55e51942a095480de0d421071cfd32c938f7ba1e02b767d657cd0610937402`；实测 960×960 / 24fps / 97 帧 / 容器时长 4.04s / 无音频。下载报告 `enemy1021_dead_v1.download.json`；审计目录 `Library/Locus/tmp/enemy1021_dead_video_v1/`（`job.json`、`runtime.json`、`frame_metrics.json`）。
- **本次实际提交的 prompt**：§0.8 英文正文（SHA-256 `80a9d532…ac8d`）+ 负面词（SHA-256 `ef2db090…b9de`）合并提交，合并后 SHA-256 `eebc6855…f478`。两张普通 `reference_image`：图1（SHA `d398a1eb…40de`）+ 图2 v2 的统一底色上传副本（SHA `e3961b81…6419`）。
- **G1 处理记录（参考图底色不一致，已按 SOP 处理）**：图1 实测四角精确 `(0,177,64)`、std 0；而图2 v2 实测底色为约 `(2,178,68)`。因此按 Skill 的 G1 门槛另存了图2 的**同一色值绿幕上传副本**（背景归一为 `(0,177,64)`、内部像素改动 0、主体几何与位置不变），原图未动。副本 `Library/Locus/tmp/enemy1021_dead_video_v1/02_dead_v2_green_00B140_1024.png`；对照图 `G1_PLATE_UNIFICATION_REVIEW.png`；记录 `g1_plate_unification.json`。
- **G2 背景实测（达标）**：逐帧四角 64×64，G 范围 **158–160**，与批次基准键 `(0,159,62)` 最差差 **+1**；首帧 G=160、末帧 G=159。背景极稳定，不列为问题项。
- **图2 v2 已验收产物**：`C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v2.png`，1,072,848 bytes、SHA-256 `f6073b065876d7e50d95d3d26c065d25b675b3532badd71b2920d8d3c5012ac9`、1024×1024 RGB。四角背景约 (2,178,68)、std ≤1.0。同目录保留 `.request.json`、`.response.json`、`.result.json`、`.submitted_once.json`；实际提交合并 prompt SHA-256 `576f410dc78c198f85a8a570765dd4050b648ba48338e8b9d7aaab76902327b0`（英文正文 `d5b81695…4649` + 负面词 `593a940b…19e1`）。审计记录在 `Library/Locus/tmp/enemy1021_dead_keypose_v2/`。
- **v2 客观测量（与 v1 同口径，仅作辅助）**：主轴偏移 28.4°（v1 46.5°、源 1.4°）；包围盒 454×540（v1 574×578、源 439×587），宽比 1.034（v1 1.308）；盾木板宽比 1.040（v1 1.331）；最小边距 193 px（18.8%）。即**尺寸已收回、朝向大幅改善但未达 15° 目标**；用户知悉该测值仍验收。
- **图2 v1（失败样本，保留追溯、不得使用）**：`enemy1021_dead_airborne_keypose_v1.png`，940,772 bytes、SHA-256 `5a13710a50df8731cc0f11d4d9d9b735af19f155b3747f2ed64d10108826da60`。否决理由与 v2 修正过程见 §0.7。
- **视频词状态：作废，待重写**。§0.8 的视频词已随 v1 失败而作废（实测证实“旋转封死+末帧锁旧图2”导致僵硬解）；**在图2 重做版验收通过前不重写视频词、不提交视频**。新视频词需以重做后的图2 为末帧锚点重新起草并审批。
- **图1（身份与首帧）已定，不再讨论**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`；1024×1024 RGB、四角 RGB(0,177,64) 且 std 0。SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`。
- **用户否决理由（原话要点，以此为准）**：① **“姿态诡异像是角色飞升而非死亡”**；② **“角色同步朝向发生了严重的改变，完全看向了左边”**。
- **生成产物（保留作失败样本，不得当已验收素材使用）**：`C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v1.png`，940,772 bytes、SHA-256 `5a13710a50df8731cc0f11d4d9d9b735af19f155b3747f2ed64d10108826da60`、1024×1024 RGB。同目录保留 `.request.json`、`.response.json`、`.result.json`、`.submitted_once.json`；第一次生成的实际提交词 SHA-256 `631829e859fb90b564597276e1dd846affd195087d1f69d769235d4dfc385903`，实际 prompt 已回读校验。审计记录在 `Library/Locus/tmp/enemy1021_dead_keypose_v1/`。
- **客观测量（代理自己跑的，仅作辅助，不代替用户目检）**：主体主轴由图1 的 1.4°（近竖直）变为 46.5°，相对偏转 **45.1°**；盾的木板区由 429×466 变为 571×575，宽比 **1.331**、高比 **1.234**——两张图都证实了用户看出的“整身旋转”与“被放大”。背景与边距本身达标：众数 (2,177,66)、四角 std 0.58–0.93、最小边距 201 px（19.6%）。
- **本轮教训**：写了 `no whole-body rotation` 与“绘制尺寸与原图一致”，模型仍同时做了整身旋转与约 1.3× 放大。§0.6 的负面词与硬约束段不足以锁住**朝向**和**尺寸**；下一版需要把“身体近乎竖直、正面朝镜头”写成**可判读的正面硬指标**（例：主轴偏移不超过 15°），并把尺寸锁定换成可量化的包围盒比例（例：主体包围盒宽高与源图差异 ≤10%），而不是只说“不得放大”。
- **v2 词已按 §0.7 三处修正写完，尚未提交**：正文、中文对照与负面词都在 §0.7。用户批准后才提交一次；未批准前不生成、不改参数重发。
- **图1（身份与首帧）已定，不再讨论**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`；1024×1024 RGB、四角 RGB(0,177,64) 且 std 0。SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`。
  - 该文件即 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/upload_copies/01_idle_green_00B140.png`，也是 Idle / Walk / Attack 三支已验收视频 request 记录里实际使用的同一份参考图。用户选 A 是在“沿用四支已验收视频的同一文件、零风险”与“从已验收 Idle 成片 raw 帧重建、指纹更接近最近一次破盾验收”之间选了前者。
  - 用户已明确接受它轮廓环带浅灰底混色的既有特征（近灰混色 351 px，占轮廓环 11.7%）；这是该副本的已知限制，不因此重新生成或改图。
  - 本轮曾产出两份替代副本（`enemy1021_shield_hold_v3_green_v2.png`、`enemy1021_idle_green_v3_from_video_00B140_1200.png`），均**未被采用**，仅作追溯。不得自动用它们替代图1。
- **图2 当前状态：【已过期】以下描述的是 v2 时期的状态，现已作废。** v1 未通过后，图2 已按 §0.10 重做为 v3，**已生成并经用户验收**（见顶部最新停点）。不要再按本节旧描述提交 §0.6 旧词或 §0.7 的 v2 词；两者均不提交。新死亡视频词由另一方对话以图1＋v3 起草，本侧不写词、不提交视频。
- **死亡动作语义：【本节已被 §0.10 取代，旧条款不得再用】**旧描述要求 “木盾始终连在画面左侧那只手上，不掉落、不破裂” 并限定躯干不旋转；用户已明确纠正：“死亡动作没有禁止盾脱手”，且已接受更大旋转与展开。当前生效的语义、指标与提交边界全部以 §0.10 与顶部最新停点为准；本行以下的历史条款只能作为演进记录阅读。
- **参考图与上传边界**：只上传图1 与生成的图2，两张都用普通 `reference_image`，**不混 `first_frame`/`last_frame`**。1011 的死亡帧（`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_dead1..6.png`）**只在本地对照**，不上传；不得上传 109、102 占位素材或任何旧候选。
- **工作环境快照**：工作目录 `C:/threeKingdomSlayer/threeKingdomSlayer`，分支 `route-scroll-movement`（落后 origin 3 个提交）；最新 Unity 公告为 Edit Mode，Active Scene `Assets/Scenes/Battle.scene`。新对话先读最新公告与 `git status`，不要把快照当作持续有效状态。

### 最新停点：ShieldBreak 已接入第一关，待用户游戏验收（2026-10-15）

本节优先于下文所有历史停点。

- **用户授权与范围**：透明帧与分件经用户明确“已验收，请按需求接入”后接入。范围只做到“接入＋配置＋静态核验”：未进入 Play、未采样动画、未做对象池实测，游戏表现待用户验收。
- **素材归一取值**：源六帧比已部署 Idle 小约 20%，按“单一全局缩放＋只钉脚底”处理——全局缩放 **1.20**、目标画布 1108、粘贴偏移 **(-22, 6)**（双靴中点/基线均值对齐到已部署 Idle 后叠加既有 `(74,91)` 约定）。局部头盔配准得分仅约 0.40，**不是精确标定**；六帧共用同一变换，未逐帧拉齐。取值与证据：`Library/Locus/tmp/enemy1021_shield_break_deploy_v1/preparation_report_v2.json`、`scale_registration.json`、`HEAD_SCALE_DIAGNOSTIC.png`。
- **本次新增资产（7 精灵 + 2 Clip + 1 Prefab + 1 脚本）**：`Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldBreak1..6.png`、`Enemy_1021_shieldDrop.png`；`Assets/Animations/Enemy_1021_ShieldBreak.anim`（50fps、0.46s、六姿势键 0/0.06/0.12/0.22/0.32/0.40 ＋ 末键保持 0.44、`ShieldVisual` null 键）、`Assets/Animations/Enemy_1021_CowardIdle_Hold.anim`（0.5s 循环、静态复用末帧）；`Assets/Prefabs/Effects/Enemy_1021_ShieldDrop.prefab`；`Assets/Scripts/Effects/ShieldDropEffect.cs`。
- **事件与接管**：0.12s `OnShieldReleaseFrame` 生成独立盾、0.46s `OnShieldBreakAnimationComplete` 收尾进入胆怯；用事件而非墙钟计时（命中卡肉会临时禁用 Animator）。独立盾脱手时只读取敌人姿态一次，之后不跟随移动/形变，0.85s 后自毁。
- **代码改动（4 个既有文件）**：`Enemy.cs`、`ShieldGuardEnemy.cs`、`Enemy_1021.controller`、`Enemy_1021.prefab`（清空旧 `ShieldVisual`、接好引用与脱手局部坐标 `(-20.0625, -3.84375, 0)`）。破盾期不抢 Walk 的逻辑**仅对 1021 生效**，不影响 102 等其它单位。
- **静态核验结果**：完整编译＋域重载通过；事件方法可解析；Clip 长度与键位正确；精灵引用完整；关卡仍含 1021；场景未 dirty；未进入 Play。受保护哈希核对显示只有上述 4 个目标文件变化。
- **核验留档与遗留**：`Library/Locus/tmp/enemy1021_shield_break_deploy_v1/asset_verification.json` 已补写（只输出原生数值，避开 Unity 类型序列化报错），含 Clip 长度/键位、事件可解析性、Prefab 引用与脱手坐标、盾对象参数、旧层清空、关卡仍含 1021、七张精灵导入设置与 GUID。**未完成**：无盾胆怯家族（`CowardHit/Launched/Dead`）、持盾死亡、击飞 Rise/Fall 仍为 102 占位；1.20 缩放与脱手时序手感待游戏内确认。
- **接入流程已固化**：`skill/workflows/action-to-unity-deployment.md`（参数化步骤＋决策表＋不变量＋已知陷阱）与 `skill/workflows/character-action-pipeline-entry.md`（9 阶段入口清单）。下次做动作接入先读后者。

### 历史停点：HitFlash 游戏验收通过（2026-10-14）

- **新用户授权**：用户指定按HitFlash v1参考图准备视频，审阅完整中英文词、两张同色绿幕副本和4秒参数后明确“发起生成”。允许本次使用v1姿态，不重写此前v1–v4关键图未通过的历史；视频、六帧与T80透明结果均已通过用户验收；用户授权第一关接入后，现明确“已验收，更新交接文档并告诉我还有哪些动作未完成”，HitFlash游戏表现验收通过。当前仅同步文档/状态，不自动开始下一动作。
- **Task ID**：`task_16d49a77f64446e693cf170b55106a32`。seedance-2-fast / 4秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1；创建HTTP200，accepted UTC `2026-10-14T04:23:10.769696+00:00`，仅创建一次。
- **视频验收与文件**：用户已确认动作通过。成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_hitflash_v1.mp4`，1,951,465 bytes、SHA-256 `a36baeab0cf9fd2e276b859855fa89320fe6e790182b4c36abc08cf36fabf091`；MP4结构与PyAV全帧解码通过，960×960/24fps/97帧/4.041667秒/H.264/无音轨。Task `task_16d49a77f64446e693cf170b55106a32`，cost.spend=0.48888（币种未注明）。下载/解码报告见视频旁 `.download.json` 和 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/inspection/decode_report.json`。
- **六帧候选**：从原始97帧选出零基 `f028/f034/f043/f046/f049/f052`（1.167/1.417/1.792/1.917/2.042/2.167秒），角色全身原960×960 RGB画布保持不变；已绕开约f018–f027可见火花，避开长时间近似保持段，以两帧受击/余势、三帧恢复和一帧回到接近Idle构成候选。用户已验收六帧。接触表 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/HITFLASH_SELECTED_6_REVIEW_v1.png`，GIF预览 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/HITFLASH_SELECTED_6_PREVIEW_v1.gif`（固定公共ROI预览、每帧100ms，不定义Unity Clip）。
- **帧素材与证据**：原始97帧在 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/clean_raw/`，候选六帧在 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/selected_v1/`，清单 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/selected_frames_v1.json`。候选PNG逐文件字节哈希与原始解码帧一致，并重新与视频解码RGB比对；没有裁切、缩放、平移、抠图、对齐或重绘。原始帧全部保留；后续T80透明处理和固定补边接入见下文。
- **T80透明结果**：SVL Chroma逐帧自动取背景色、容差80/softness16/halo1/半透明转不透明，未启用平滑、重绘或额外全图调色。六张同名960×960 RGBA在 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/keyed_ui_t80/`，已获用户验收。六帧半透明0、透明RGB非零0，SVL幕布残留/封闭孔指标均0；严格四连通Alpha诊断另识别f028盔顶与红缨之间3717像素的合法透明负空间（515,251–599,335），保持透明、未自动填孔。距可见轮廓4px的核心RGB改动0。混色边缘553–714像素/帧发生去溢色变化，green_gt8仍280–359像素/帧，不声称严格零绿或所有RGB原样保留。源帧与输出哈希已回读。
- **透明评审入口**：白底/棋盘格 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/HITFLASH_MATTED_6_WHITE_CHECKER_t80_v1.png`；GIF `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/HITFLASH_MATTED_6_PREVIEW_t80_v1.gif`（公共ROI、100ms/帧，仅评审）。验证报告 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/hitflash_matting_t80_verification.json` 与 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/frames/hitflash_matting_t80_core_and_preview.json`。
- **两张实际上传图**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/upload_copies/01_idle_green_00B140.png` 与同目录 `02_hitflash_v1_green_00B140.png`，普通reference_image。两图1024×1024 RGB，四角空背景RGB(0,177,64)、std0。图1负责身份/绘制比例/根和双脚/首尾Idle，图2只负责v1局部受击姿态；v1副本仅换连通绿底并整图平移(+2,+9)px，无缩放重绘，原图保留，靴位残差约5px仍以图1为准。未上传1011动作图或其他版本/动作。
- **继续入口**：完整双语词与本地状态为 `Library/Locus/tmp/enemy1021_hitflash_video_v1_preparation/job.json` / `runtime.json`；持久请求 `C:/Users/steam/Videos/doubaoVideo/enemy1021_hitflash_v1.request.json`；一次性创建哨兵 `create_attempted.json` 已落盘。实际英文Prompt SHA-256 `44e1eb9950f515c9ec9c86a07716259cd78010088d46086a2b77276179bd17b8`。
- **接入结果**：六张部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_hitFlash1.png` 至 `Enemy_1021_hitFlash6.png`，按固定 `(74,91)` 零重采样补边到1108×1108；新建 `Assets/Animations/Enemy_1021_HitFlash.anim`，30fps、0.3667秒、非循环，根Sprite六键加末帧保持键，`ShieldVisual` 0秒null键。1021 Controller的HitFlash motion已替换，现有 `HitFlash → Idle` Exit Time 0.9、duration 0保持。Prefab仍引用1021 Controller，第一关配置仍含enemyId 1021。
- **游戏验收与留档**：用户已明确验收HitFlash游戏表现，`Library/Locus/tmp/enemy1021_hitflash_deploy_v1/preparation_report.json` 与 `Library/Locus/tmp/enemy1021_hitflash_deploy_v1/asset_verification.json` 已同步 `user_game_acceptance=accepted`、`game_acceptance_pending=false`。此前静态导入、引用、源像素保护和109个既有文件哈希核对通过；代理没有进入Play Mode、采样动画或运行自检，游戏验收依据是用户确认。接入阶段既有资产仅改1021 Controller的HitFlash motion，未改Prefab、第一关配置、场景或脚本；本次收尾只更新文档/状态。
- **当前边界**：Idle/Walk/Attack/ShieldGuardHit/HitFlash为已验收基线，不重做；HitRight已接入、游戏表现待用户验收。不自动处理剩余动作；新目标、制作优先级和起身/眩晕取舍由用户指定，默认不运行自检。

### 历史停点：HitFlash v4未通过，暂停制作（2026-10-13）

- **最新用户指令**：“这好像还不如V1版本，算了先写交接文档吧，我们在别的对话继续”。现在停止生成，保留全部候选与原始请求，等待下一对话指定目标；没有批准v5、HitFlash视频或任何受击动画部署。
- **既有游戏基线**：Idle / Walk / Attack / ShieldGuardHit已接入；本轮HitRight与HitFlash素材工作没有修改Prefab、Controller、Clip、脚本、数值或场景。文件系统核对未见 `Assets/Animations/Enemy_1021_HitFlash.anim` 或 `Assets/Animations/Enemy_1021_HitRight.anim`，不能把本地候选描述为已部署动画。
- **2026-10-13历史验收分层**（仅描述当时停点；HitFlash最终完成状态以本节最上方为准）：

| 动作 | 已完成 / 已验收 | 未完成 / 当前边界 |
|---|---|---|
| HitRight反侧受击 | v2姿态、视频动作和六帧选择已获用户认可；换绿与透明处理已落盘 | 最终透明结果未明确获验收；封闭灰色空隙仍有待确认；未对齐、补边、配置Clip或接入Unity，暂不部署 |
| HitFlash正面可读受击 | 四张参考图已生成、留档；1011实际正面受击帧已在本地查看 | v1–v4均未通过；没有正式动作基准、视频、生产帧、透明序列或Clip；此为10-13当时状态，10-14用户重新指定v1后已完成视频/选帧/T80透明与游戏验收 |

#### HitFlash v1–v4失败记录与素材入口

所有候选为1024×1024 RGB绿底PNG，每版只实际生成一次；`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、`background=opaque`、PNG、n=1。图像响应均未返回明确cost，不推断图像费用。以下为用户反馈，不把Prompt里的目标角度/位移当作成图实测。

| 版本与完整路径 | 验收反馈 / 不能复用的原因 |
|---|---|
| `C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v1.png` | 肩部没有清楚受力移动、缺轻微后仰，像主动后缩；未通过 |
| `C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v2.png` | 明显左右朝向，后仰不像受击；未通过 |
| `C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v3.png` | 肩部与双手姿态不对、受击感弱；未通过 |
| `C:/Users/steam/Pictures/gptGen/enemy1021_hit_front_keypose_v4.png` | 用户认为还不如v1，要求暂停并交接；未通过，不自动退回v1 |

- **唯一实际上传的身份图**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`。未上传任何1011动作图或之前被否决的HitFlash/HitRight动作图。不要因为本轮失败自行改变这个上传边界。
- **完整词与参数**：四张图旁分别保留同名 `.request.json`、`.response.json`、`.result.json`；工程内双语词和执行状态在 `Library/Locus/tmp/enemy1021_hit_front_keypose_v1/job.json`、`Library/Locus/tmp/enemy1021_hit_front_keypose_v2/job.json`、`Library/Locus/tmp/enemy1021_hit_front_keypose_v3/job.json`、`Library/Locus/tmp/enemy1021_hit_front_keypose_v4/job.json`。这些任务已执行，不重跑创建请求。
- **本地1011研究入口**：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` 与 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_hit1.png` 至 `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_hit5.png`；原词在 `memory/enemy1011-animation-prompt-archive.md` §2。v4本地肩肘手对照为 `Library/Locus/tmp/enemy1021_hit_front_keypose_v4/1011_FRONT_SHOULDER_ARM_REVIEW.png` 和 `Library/Locus/tmp/enemy1021_hit_front_keypose_v4/POSE_LANGUAGE_COMPARISON_v1.png`，仅用于研究，不是上传副本或获验收1021姿态。
- **最后尝试的语言，不是已验收规则**：v4要求双肩上耸内收、肩甲靠近头盔，双肘屈曲留在身体两侧，空手前臂收回腰侧，持盾肘屈曲后盾随动，膝盖轻屈吸收冲击。但产物依然未达到用户期待；下一对话不要把这段直接当作成功配方或只微调数字连续收费重试。此前“内收双手”“双肘略向外”“手腕落下/抬高”描述曾反复修订，继续时应重新对照实际图，不盲信文字总结。
- **要保留的目标与红线**：正面可读、没有左右转身/歪头；肩、肘、腕真正参与受力，身体是被冲击带动而非主动缩身/挺胸/防御；盾仍在画面左侧握持，不能把这个通用受击做成已验收ShieldGuardHit或HitRight大幅敞盾动作。固定根和脚底，不以全身缩放表现受力，无特效。
- **生成底不等于色值锁定**：v1–v4背景都是绿底，但四角中位色分别约(2,177,66)/(2,176,66)/(1,177,68)/(2,176,66)，并非严格RGB(0,177,64)。即使下一张姿态获认可，视频前仍须另存同色绿幕上传副本、测背景并保护主体颜色，再供用户审阅。
- **当时暂停结论**：2026-10-13没有通过的HitFlash姿态，当时不启动视频。2026-10-14用户新指定v1参考图并授权视频，后续视频、帧序、透明结果和游戏表现已通过；本段仅保留失败过程，不覆盖最终验收。

#### HitRight v2（历史）：视频/选帧已验收，当时透明候选未部署

- **动作读感已认可**：画面右侧来力、头部被甩偏、盾向画面左侧敞开，露出胸甲以表达没有挡住；v1因仍像持盾站立被替代。v2姿态图为 `C:/Users/steam/Pictures/gptGen/enemy1021_hit_right_keypose_v2.png`。
- **v2成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1021_hit_right_v2.mp4`，task `task_f1086e5874ee4ae7bf386a0bd0a1aea6`，seedance-2-fast/4秒/720p/1:1/无音频/无水印/seed=-1。实测960×960、24fps、97帧、4.041667秒；1,847,087 bytes，SHA-256 `adbe1efbd2539b17cbd15321fa1b74609cc47f0128515ab19c7bbb901bad1514`，完整解码通过。原任务cost.spend=0.48888（币种未知）；不要重新创建视频。
- **流程错误已明确**：代理此前上传浅灰副本并在视频词中指定灰底，导致已验收视频不是绿幕。用户将其指出为严重流程问题。以后角色视频先准备统一绿幕副本，常用RGB(0,177,64)，不能自动沿用定妆/关键图灰底；不要把用户确认动作或“发起生成”解释为免去绿幕准备。
- **六帧已验收**：零基f018/f024/f050/f052/f054/f066，时间0.750/1.000/2.083/2.167/2.250/2.750秒。原始候选目录：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/selected_v1/`。
- **文件名**：`hitRight1_f018.png`、`hitRight2_f024.png`、`hitRight3_f050.png`、`hitRight4_f052.png`、`hitRight5_f054.png`、`hitRight6_f066.png`；全部原960×960画布保留，未裁切、缩放、平移或对齐。
- **绿底中间版**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/green_recovered_v1/`，同名追加 `_green.png`，统一RGB(0,177,64)。先按外部连通灰底识别蒙版、保护内部高光、反混合，再合成绿底；染绿不是跳过背景分离的捷径。
- **当前透明候选**：`C:/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/keyed_green_t79_v1/`，六张与原候选同名的960×960 RGBA；SVL T79/softness16/halo1/半透明转不透明。本轮重新核对原片、源帧、绿底及透明候选哈希，均与留档一致。
- **已验证与未验证**：六张识别到的外部灰底残留0、半透明0、透明RGB非零0、报告列明源内部RGB变化0；仍保留了封闭近灰区域，可能有手指/红缨空隙或金属高光。没有完成逐个空隙语义确认，不能宣称全面清理。最终透明素材没有收到明确验收回复，后续“暂不部署”也不算透明验收。
- **检查与留档**：`Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/selected_frames_v1.json` 已为selected_frames_accepted_by_user=true；`Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/hitright_green_matting_report_v1.json` 仍matte_user_accepted=false。透明评审 `Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/HITRIGHT_GREEN_MATTED_6_REVIEW_v1.png`、细节 `Library/Locus/tmp/enemy1021_hit_right_video_v2/frames/HITRIGHT_GREEN_MATTED_DETAIL_v1.png`，其余源片/任务/下载留档在v2目录与视频旁JSON中。
- **边界**：不自动清孔、对齐、补边或部署；只有用户明确指定后再处理。严禁全局删除灰色以免吃掉金属高光；若继续局部修复，另存版本保留已验收源帧。批处理脚本已执行，报告写入曾因numpy.int64失败后由只读输出收尾脚本补齐，不能因此重跑生成或覆盖图片。

### ShieldGuardHit 六秒视频 v1（2026-10-11已完成基线，透明六帧v3已接入并获游戏验收）

- 用户认可K1 v4（“很好，根据这个v4参考图思考视频生成词”），审阅完整双语视频词后明确“时间改为6秒，发起生成”。已上传Idle定妆图和K1 v4，以两张普通 `reference_image` 创建一次视频任务，未混用首尾帧。
- **Task ID**：`task_e6757d35965a46c389e06bfd86a6d4ae`，创建HTTP200，accepted UTC `2026-10-11T13:25:25.591157+00:00`。规格 `seedance-2-fast` / 6秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1。
- 动作目标：约0.5秒接住来自画面左侧的攻击，盾向画面右侧压入，缩颈/耸肩、膝屈和空手稍晚跟随；经过v4承力姿态后盾向左回弹，颈肩释放、回Idle，保持到六秒结束。无特效、固定双脚/根位置/镜头。提示词保留此前0.25–0.35秒受力与恢复目标，用户只修改源视频总时长。
- 首轮12次查询全部 `generating`，随后查询现有任务于UTC `2026-10-11T13:38:48.417507+00:00` 返回 `success`；接口 usage `completion_tokens=130500`，cost `spend=0.7308`（币种未注明）。下载未创建新任务。
- 成片：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.mp4`，2,310,345 bytes，SHA-256 `cefcc521c18651cadc0777a29f44f23b5fb44ccd97ad76290eba2d78692d7e1f`。实测960×960、24fps、145帧、容器时长6.041667秒、末帧PTS 6.0秒、H.264、无音频；MP4 `ftyp/moov/mdat` 结构和全部帧解码通过。
- 下载报告：`C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.download.json`；四段Range均HTTP206/ETag和字节范围核验通过，下载耗时89.724秒。用户随后明确“已验收，请截取有效帧”，要求六帧；六帧选帧也已验收，用户明确要求“开始抠图”；本地抠图v3完成后，用户明确“已验收，请接入”，透明结果已验收并完成静态接入；用户随后明确“已验收，更新交接文档准备在其他对话继续任务”，ShieldGuardHit游戏表现验收通过。
- 六帧已截取：零基 **f023/f024/f026/f090/f093/f101**，源时间0.958/1.000/1.083/3.750/3.875/4.208秒。首帧已是承力姿态，顺序为初次接触→压入→缩颈峰值→回弹→颈肩释放→恢复Idle；跳过f027..f089的长时间近似保持和前后待机。
- 已验收源帧目录 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/selected_v1/`，文件 `guard1_f023.png` 至 `guard6_f101.png`；六图均960×960 RGB原始画布，与解码源帧字节哈希及重新解码像素一致，原始文件未改动。选择记录 `frames/selected_frames_v1.json` 已记录 `selected_frames_accepted_by_user=true`，145张原始帧保留于 `frames/clean_raw/`。
- 原选帧预览 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_SELECTED_6_REVIEW_v1.png` 与同目录 `GUARD_SELECTED_6_PREVIEW_v1.gif` 保留。**当前透明交付为v3**：`Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/keyed_gray_v3/`，六张与源帧同名的960×960 RGBA PNG；未裁切、缩放、平移、对齐或重绘。
- **抠图配方与核验**：现有本地SVL Chroma自动取底色，threshold14/softness16/halo1/半透明转不透明；灰底与甲胄近色，补充底色距离26的8连通外部背景与逐帧确认的真实空隙清理，保护内部金属高光。未使用生成式平滑、旧描边修复或远程生成。六图源/输出哈希回读一致，Alpha仅0/255、透明RGB全0、已识别背景及空隙泄漏0、距可见轮廓4px的内部RGB改动0；用户随后明确验收透明结果。核验报告 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/guard_matting_verification_gray_v3.json`。
- **透明结果预览**：白底/棋盘格 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_6_WHITE_CHECKER_v3.png`，原图/白底/深底细节 `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_SOURCE_WHITE_DARK_DETAILS_v3.png`，GIF `Library/Locus/tmp/enemy1021_shield_guard_video_v1_6s/frames/GUARD_MATTED_6_PREVIEW_v3.gif` 均已回读。预览使用固定ROI，GIF每帧80ms仅评审姿态顺序，不定义正式Clip。用户已验收透明结果并授权接入，按既有固定偏移补边部署，未做逐帧缩放或脚底拉齐。早期 `keyed_ui_t14_connectivity_v1/`、`keyed_gray_t14_v2/` 保留为试验，不作最终交付。
- 完整双语词、六秒参数、参考图职责/哈希和task ID在 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_guard_hit_v1_6s.request.json`，已回读核验；实际提交Prompt SHA-256 `fc83097a561aee4862ea902d516bb9ee94998d97f06d9213388e4057af87ddf4`。`generate_video.py`和`query_and_download_existing.py`均已执行，不要重跑或重新创建任务。六帧选帧、透明v3及Unity游戏表现均已获用户验收；不要重做已验收动作。
- **ShieldGuardHit部署**：透明六帧以Idle/Walk/Attack相同固定偏移 `(74,91)` 零重采样补边到1108×1108，保存为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldGuardHit1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_shieldGuardHit6.png`。导入设置与Idle一致：Single/PPU16/Point/无压缩/无Mipmaps/Tight/Center；Unity实际导入1024×1024、Sprite PPU14.7870035，与现有动作导入几何一致。原960透明帧像素和动作位置保留，无逐帧缩放、裁切或拉齐。
- **Clip与接线**：新建 `Assets/Animations/Enemy_1021_ShieldGuardHit.anim`，30fps、0.30秒非循环，六帧键位0/0.033333/0.066667/0.133333/0.2/0.266667秒。包含 `ShieldVisual` sprite null键，无Transform曲线或AnimationEvent；仅替换1021控制器的 `ShieldGuardHit` motion，保留exit time 1、duration 0回Idle转场。Prefab已引用该Controller和状态名，第一关已含1021，无需新增入口配置。
- **保存证据与验收边界**：`Library/Locus/tmp/enemy1021_shield_guard_deploy_v1/preparation_report.json` 与 `Library/Locus/tmp/enemy1021_shield_guard_deploy_v1/asset_verification.json` 已回读；六张补边PNG像素一致、六个Sprite引用完整，Clip/Controller重新导入核对保存成功。85个受保护既有文件哈希保持一致，既有资产仅修改1021控制器；未改Prefab、关卡、场景、脚本或其他动画。代理未进入Play Mode、未驱动动画采样或执行运行/画面自检；用户自行开游戏后已明确验收通过，部署留档已同步 `user_game_acceptance=accepted`、`game_acceptance_pending=false`。

### ShieldGuardHit 关键帧参考图制作历史（2026-10-11，K1 v4获认可；最终游戏验收见上节）

- 用户明确的格挡读感：角色右手持盾（画面左侧），小幅接住来自画面左侧的攻击，盾牌小幅震荡，角色不后仰；动作素材不允许特效。v1两图因角色姿态没有变化、仅增加盾牌特效而被用户否决。
- 按用户随后批准的修订动作语言重新生成v2：K1 承力压入、K2 释放回弹。唯一参考为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`；`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、PNG、浅灰底，每个姿态仅提交一次。
- K1 v2：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v2.png`，1,111,759 bytes，SHA-256 `6dd5400f33134dfbcdfcd0fc7f75fec670560a6ced628a41a24cac3da34e7772`。HTTP 200，usage input/output `1167/8596`，cost未返回；用户反馈盾牌位移有效、肢体变化不足，未通过。
- K2 v2：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_rebound_v2.png`，1,020,121 bytes，SHA-256 `49df3772aa0649236534f176c6f47a87415f8dfb2affda1c1b6b416308343b4e`。HTTP 200，usage input/output `831/1084`，cost未返回；随v2方案一并不作为验收动作依据。
- K1 v3：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v3.png`，965,738 bytes，SHA-256 `ec3613fc53e63c38f9ae10b5eabd33054fcbd73529726f9d90eed81ac31b1031`。使用Idle定妆图和K1 v2两张参考，HTTP 200，`quality=max`、`input_fidelity=high`，usage input/output `2662/7024`，cost未返回；每个姿态只提交一次。
- 用户反馈：K1 v2盾牌位移有效，但角色整体肢体姿态变化不足，至少须有朝画面右侧倾斜的承力反应，v2未通过。用户随后批准K1 v3双参考编辑Prompt：以骨盆为支点，上身向画面右侧侧倾约8°、头肩胸相对骨盆右移约25–35px，保持双脚和根位置固定、正面朝向、不后仰、无特效。8°和25–35px是获批请求目标，不能当作成图实测结果。
- K1 v3已回读，代理初检曾认为右倾读感偏弱；**用户随后明确反馈“其实已经很接近了，建议稍稍让其缩脖子表达受到冲击即可”**。当时保留v3姿态，不再加大右倾或重做盾位/四肢；该阶段K1尚未最终验收、K2暂缓。后续K1 v4已认可并用于已验收视频，最终游戏验收见上节。
- 完整双语Prompt与请求/响应/核验位于输出旁的 `.request.json`、`.response.json`、`.result.json`；v2生成脚本和配置位于 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v2/`。费用字段为空，不推断费用。
- K1 v3阶段仅有静态候选，当时尚未生成K2 v3、格挡视频、透明帧或Unity资产；后续完成情况见上节。Idle/v2/v3同画布对照为 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v3/K1_IDLE_V2_V3_REVIEW.png`：三图统一缩至50%预览，未分别裁切或对齐。v3完整请求、脱敏响应、核验在输出旁 `.request.json`、`.response.json`、`.result.json`；脚本和配置在 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v3/`，脚本已执行，不要重新运行。
- **K1 v4已生成**：以 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v3.png` 为唯一输入，仅请求颈肩微调；目标输出 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_guard_contact_v4.png`，994,135 bytes，SHA-256 `fe6d53654ee1f985f440f835a9e7d0bef35b264f8cbc5d42e0ea399f40ad875d`。HTTP 200，`quality=max`、`input_fidelity=high`，usage input/output `653/1023`，cost未返回；只提交一次。
- v4回读未见火花、木屑、震动线等特效，缩颈读感比v3明显；同时模型带动了头部、肩甲和盾牌/构图，变化比“只做颈肩局部微调”的目标更大。用户随后反馈“很好”，并要求以v4推导视频词，现已作为六秒视频的承力参考；这不是视频动作、透明帧或部署验收。K2未另行生成。
- v4原图及v3对照：`Library/Locus/tmp/enemy1021_shield_guard_keyframes_v4/K1_V3_V4_REVIEW.png`，上排为相同完整画布50%预览，下排为相同颈肩区域裁剪的100%对照；未做姿态对齐或像素修改。完整Prompt、请求、脱敏响应、核验位于输出旁 `.request.json`、`.response.json`、`.result.json`；配置与脚本位于 `Library/Locus/tmp/enemy1021_shield_guard_keyframes_v4/`，脚本已执行，不要重跑。

### 最新 Attack 任务（2026-10-11，已接入第一关并获用户验收）

- 用户审阅“稍稍侧身用盾牌撞击前方”的中英文提示词后明确“发起生成”；已提交一次 `seedance-2-fast` 4秒/720p/1:1、无音频无水印、seed=-1 视频任务。
- Task ID：`task_941e179a2fbd4ee080d0e7c66022469b`，accepted UTC `2026-10-11T06:29:01.914241+00:00`；首轮12次查询仍 `generating` 后停止。用户随后要求下载，本轮UTC `2026-10-11T06:43:56.565016+00:00` 观察到success，已下载同一任务，未重新生成；不要再运行创建脚本。
- 参考为已验收 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png`，同一图一次上传后用于首尾帧；允许躯干小幅侧转，镜头与脚底锚点固定。完整请求 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.request.json`；提交提示词SHA-256 `fdcb8bff319094b28bc169ea62c3e86fce30b15b28e0af8fb94f9107a014828b`。
- 运行记录：`Library/Locus/tmp/enemy1021_attack_video_v1/runtime.json`；下载报告 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.download.json`；分段与续传清单 `Library/Locus/tmp/enemy1021_attack_video_v1/download_manifest.json`。首次查询TLS超时后换curl成功；末段超时后只补缺失615,900 bytes完成下载，已验证远程对象/ETag/精确Range。
- 实际成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_attack_v1.mp4` 已落盘，2,476,908 bytes，SHA-256 `ea41b8a47b01b4d9ddea455222d5653f5d93184a4512b5c9ec99e97e0838b404`；960×960/24fps/97帧、4.041667秒、无音频，MP4容器及完整解码通过。下载完成UTC `2026-10-11T06:48:25.843527+00:00`；原任务 `cost.spend=0.48888`（币种未知）。检查接触表只是文件诊断，不是生产选帧。
- 用户已确认12帧候选并授权抠图和对齐；顺序为先选有效帧供用户验收，再抠图，最后统一尺寸/锚点。选中零基idx：000/024/032/036/040/048/056/064/072/080/088/096，原始97帧、12帧RGB候选与未对齐SVL透明版均保留。
- **当前交付使用v2**：12张960×960 RGBA在 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/aligned_v2_960/`。以可见头盔细节的实际匹配尺寸估算整体倍率，按双靴底锚点对齐到 `(484.25,780)`；待机参考帧不缩放，顶撞阶段整帧缩放约0.814–0.822，不按轮廓总高fit。v1为已被替代的中间版，后续不要从v1部署。
- **验收素材与证据**：接触表 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNED_12_WHITE_REVIEW_v2.png`，处理前后对照 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNMENT_BEFORE_AFTER_v2.png`，GIF `Library/Locus/tmp/enemy1021_attack_video_v1/frames/ATTACK_ALIGNMENT_BEFORE_AFTER_v2_preview.gif`（0.12秒/帧，仅评审节奏）。报告 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/alignment_report_v2.json` 和 `Library/Locus/tmp/enemy1021_attack_video_v1/frames/verification_report_v2.json`；12帧底边均780，重新测得双脚中点最大X误差1.75px，透明区RGB全0，Alpha0/255，边界完整。
- **素材验收与接入**：用户先明确“已验收，请接入然后等我验收”，已授权使用对齐v2；接入第一关后用户再次确认 Attack 已验收。十二帧按Idle相同固定偏移 `(74,91)` 零重采样补到1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_attack1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_attack12.png`。新建 `Assets/Animations/Enemy_1021_Attack.anim`，替换 `Assets/Animations/Enemy_1021.controller` 的Attack motion；已有第一关配置 `Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset` 包含1021，Prefab引用该Controller。
- **播放与接线记录**：Clip为30fps、2秒非循环，十二帧键位0/0.5/0.6667/0.75/0.8333/1/1.1667/1.3333/1.5/1.6667/1.8333/1.9667秒，`attack6` 接触姿态在1秒。额外包含 `ShieldVisual` 清空键；原Attack→Idle与受击转场保留。没有修改Prefab、攻击数值、脚本、命中帧声明或场景。
- **落盘证据与验收边界**：`Library/Locus/tmp/enemy1021_attack_deploy_v1/preparation_report.json` 与 `Library/Locus/tmp/enemy1021_attack_deploy_v1/asset_verification.json` 已回读，十二个root Sprite引用完整、源像素补边后保持一致、Clip/Controller保存完毕。用户已验收 Attack；本轮未进入Play Mode、未执行运行或画面自检。后续在其他对话继续1021敌人动画与机制完善。

### 0.0 最新 Walk 进度（2026-10-11，优先于下文早期Idle阶段快照）

- 用户已明确验收Walk视频与六帧，并要求“已验收，请开始抠图”；任务 `task_2d7741be5ad14839b33f12f061c6b606`，视频 `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_walk_v1.mp4`，960×960/24fps/97帧，SHA-256 `2d20f36dc068c73c0710971b69c8f1ea8c650e809f787921eee2baef636fbc58`。
- 已解码97张原始RGB帧至 `Library/Locus/tmp/enemy1021_walk_v1_frames/clean_raw/`，编号f001..f097为1基。下半身固定ROI在约11帧间隔重复，选中段完整周期六帧：**f040/f042/f044/f046/f048/f050**，排除首尾Idle与重复姿态。
- 候选目录 `Library/Locus/tmp/enemy1021_walk_v1_frames/selected_v1/`；选择/哈希清单 `Library/Locus/tmp/enemy1021_walk_v1_frames/selected_frames_v1.json`；编号接触表 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_CANDIDATES_v1_6frames.png`；循环GIF `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_CANDIDATES_v1_loop_0p6s.gif`。
- 原始候选960×960RGB、未裁切缩放去背，哈希与对应解码帧一致；接触表/GIF仅采用同一固定预览ROI。GIF每帧0.1s/循环0.6s，只是评审候选节奏。
- **透明六帧已完成并部署，用户已确认验收**：现有SVL本地Chroma自动取背景色，容差79/softness16/halo1/半透明转不透明；透明源文件在 `Library/Locus/tmp/enemy1021_walk_v1_frames/keyed_ui_t79/`，与源帧同名，960×960 RGBA。半透明/幕布残留/封闭孔均0；保留原跑步起伏，没有逐帧缩放、裁切或拉齐脚底。没有收费生成或新安装。
- 白底+棋盘格图 `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_6frames_white_alpha.png`；循环GIF `Library/Locus/tmp/enemy1021_walk_v1_frames/WALK_MATTED_v1_loop_0p6s.gif`；SVL指标报告 `Library/Locus/tmp/enemy1021_walk_v1_frames/svl_matte_walk_v1_report.json`；完整核验 `Library/Locus/tmp/enemy1021_walk_v1_frames/walk_matting_verification_v1.json`。
- **Walk 部署完成**：透明源帧按 Idle 相同固定偏移 `(74,91)` 零重采样补边至1108×1108，部署为 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_walk1.png` 至 `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_walk6.png`。`Assets/Animations/Enemy_1021_Walk.anim` 为30fps、0.6秒循环、六个唯一精灵，含 `ShieldVisual` 清空键；Controller 的 `Walk` motion 已替换，Prefab 与 Idle Clip 保持部署前字节一致。
- **留档**：`Library/Locus/tmp/enemy1021_walk_deploy_v1/preparation_report.json`、`Library/Locus/tmp/enemy1021_walk_deploy_v1/asset_verification.json`、`Library/Locus/tmp/enemy1021_walk_deploy_v1/runtime_verification.json`。用户提出新边界前已完成受控对象池实例测试（六帧、两次前进一排、回 Idle、同实例复用）；这不是自然波次调度回归。后续画面对照已停止，不追加自检。

### 0.1 当前任务与完成边界

- **继续目标**：为敌人 1021 制作正式美术，先完成定妆与 Idle，再分批制作其他动作；每套动作分别完成视频、选帧、透明处理、对齐与 Unity 部署，各阶段单独验收。
- **已完成**：用户明确说“idle形象已验收”；`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` 是当前唯一获验收的 1021 身份与持盾姿态锚点。不要重新设计体型、头盔、护甲或盾牌来贴合早期提示词数字。
- **已接入动作**：Idle四帧（0.96秒循环）、Walk六帧（0.6秒循环）、Attack十二帧（2秒非循环、1秒接触姿态）、ShieldGuardHit六帧（0.30秒非循环）、HitFlash六帧（0.3667秒非循环）、HitRight六帧（0.9秒非循环）、ShieldBreak六帧（0.46秒非循环，含独立盾接管）与持盾死亡三帧（0.6秒非循环、末帧保持）已接入第一关。Idle/Walk/Attack/ShieldGuardHit/HitFlash/Dead已获用户游戏验收；HitRight与ShieldBreak待游戏验收。已验收动作不重做；击飞与无盾胆怯家族仍未完成。
- **最新用户请求与授权**：透明帧与分件经用户“已验收，请按需求接入”后完成 ShieldBreak 接入（新增精灵/Clip/事件/独立盾对象/代码挂点）。之后用户要求先停下并转入工作流文档优化，未开始下一项动画、未改变机制、未进入运行自检。
- **最新工作环境快照**：工作目录 `C:/threeKingdomSlayer/threeKingdomSlayer`，分支 `route-scroll-movement`；最近 Unity 公告为 Edit Mode，Active Scene `Assets/Scenes/Battle.scene`。新对话先读取最新公告和 `git status`，不要把快照当作持续有效状态。

### 0.2 其他对话继续时的未完成项

本轮只更新交接，用户将在其他对话指定后续动画或机制目标。以下是已有方案中的剩余工作，不是本轮自动实施清单，也没有确定下一套动作的制作优先级。

| 方向 | 未完成内容 | 继续时的约束与入口 |
|---|---|---|
| 格挡附加反馈（可选） | 独立格挡特效/音效，尚未制作 | 正式 `ShieldGuardHit` 已接入并获游戏验收，不再列为未完成；动作素材保持无特效，独立反馈另按用户后续请求确定 |
| 破盾与掉盾 | **已完成，待用户游戏验收**：正式 `Enemy_1021_ShieldBreak.anim`、独立盾 Prefab、脱手事件接管与生命周期均已实现 | 若要调节奏：改 Clip 键位必须同步 `breakAnimationDuration`（两者当前都是 0.46s）；`CowardIdle` 目前只是静态兜底（`Enemy_1021_CowardIdle_Hold.anim`），不是正式待机 |
| 反侧受击 | `HitRight` 已接入第一关（用户自抠六帧） | 等待用户游戏验收；若要调整节奏需改 `Enemy_1021_HitRight.anim` 键位或 `HitRight → Idle` 的 exit time，不要重做已验收的动作视频/选帧 |
| 持盾死亡 | 正式 `Dead` | **已完成并获用户游戏验收（2026-10-17）**：`Enemy_1021_Dead.anim`（3帧、30fps、0.6s、末帧保持，键位 0/0.2/0.4＋保持 0.5667）；素材来自已验收 `dead_3frames_matted_v1/`，按 `(74,91)` 补边到 1108。已验收不重做 |
| 持盾击飞 | 正式 `Launched_Rise`、`Launched_Fall` | 起飞与下落仍用102占位；是否补起身见下行，不把死亡帧当击飞完成 |
| 无盾胆怯家族 | 正式 `CowardHit`、`CowardLaunched`、`CowardDead`；`CowardIdle` 目前只有静态兜底 | **已排期（2026-10-17），规格已写**：制作与部署口径见 `plan/enemy1021-coward-family-plan.md`。`CowardIdle` 暂用 `Enemy_1021_CowardIdle_Hold.anim`（破盾末帧静态循环）撑住无盾形象；其余三项仍复用102占位。无盾形象基准为破盾末帧 `Enemy_1021_shieldBreak6.png` |
| 起身/眩晕（待决定） | `Launched_Getup`、`CowardGetup`、专属眩晕表现 | 当前均未完成；是否补齐普通/胆怯起身及非Boss专属眩晕仍需用户拍板，不能自动作为必做清单 |
| 精确攻击 | 跟手瞄准、枪尖与命中点同源、盾区参数及可视化 | 设计已定但未实现；链路和边界见 `design/enemy-1021-shield-guard.md` §4.4 |
| 数据声明 | 独立 `breaksGuard` / `isPreciseAim` 字段 | 当前重攻击语义借用 `interruptsCavalryCharge`；后续解耦时保留骑兵行为和原资产数据，不维护招式白名单 |
| 机制完善与验收 | 减伤/破盾/胆怯手感、破盾期死亡或击飞、池复用、同列多1021等交互 | 见 `plan/enemy1021-minimal-playable-handoff.md` 与机制Design §12；Attack与ShieldGuardHit动作验收不代表这些机制全量验收 |

已完成的HitFlash和ShieldGuardHit不属于剩余项；持盾侧`HitLeft`已并入格挡，不另做，也不制作举盾/收盾切换。以上动作验收不表示独立掉盾、精确攻击或机制交互已完成。

#### 剩余动画素材：按 Animator 状态名对照（未制作，等用户指定）

| Animator状态名 | 1021当前素材 | 状态 |
|---|---|---|
| `Idle` | `Enemy_1021_Idle.anim` | 已接入、已游戏验收 |
| `Walk` | `Enemy_1021_Walk.anim` | 已接入、已游戏验收 |
| `Attack` | `Enemy_1021_Attack.anim` | 已接入、已游戏验收 |
| `ShieldGuardHit` | `Enemy_1021_ShieldGuardHit.anim` | 已接入、已游戏验收 |
| `HitFlash` | `Enemy_1021_HitFlash.anim` | 已接入、已游戏验收 |
| `HitRight` | `Enemy_1021_HitRight.anim` | 已接入、**等待用户游戏验收** |
| `ShieldBreak` | `Enemy_1021_ShieldBreak.anim` | 已接入、**等待用户游戏验收**（50fps、0.46s、0.12s 脱手事件＋0.46s 结束事件） |
| `Dead` | `Enemy_1021_Dead.anim` | 已接入、已游戏验收（3帧、30fps、0.6s、末帧保持） |
| `Launched_Rise` | 复用`Enemy_102_Launched.anim` | **未制作** |
| `Launched_Fall` | 复用`Enemy_102_Launched_Fall.anim` | **未制作** |
| `CowardIdle` | `Enemy_1021_CowardIdle_Hold.anim` | 待正式循环（静态兜底已能跑）；规格见 `plan/enemy1021-coward-family-plan.md` |
| `CowardHit` | 复用`Enemy_102_CowardHit.anim` | **未制作**；已排期，规格见 `plan/enemy1021-coward-family-plan.md` |
| `CowardLaunched` | 复用`Enemy_102_CowardLaunched.anim` | **未制作**（现用死亡帧，读感差）；已排期，注意其出边与 2.0s 保持方案 |
| `CowardDead` | 复用`Enemy_102_CowardDead.anim` | **未制作**；已排期，0.6s 末帧保持 |
| `Launched_Getup`、`CowardGetup` | 控制器中不存在 | **待用户决定**是否补起身 |
| `Stun` 专用表现 | 控制器中不存在 | **待用户决定**（非Boss通用缺口） |

盾掉落独立对象已实现（`Assets/Prefabs/Effects/Enemy_1021_ShieldDrop.prefab` ＋ `Assets/Scripts/Effects/ShieldDropEffect.cs`），由 `ShieldBreak` 的 0.12s 事件生成、0.85s 后自毁；仍待游戏内确认轨迹与层级表现。

继续前先读本节、`memory/user-preference.md` 及相关Design，再核对实际代码/资产。后续动画顺序固定为：有效帧先交用户验收 → 正常SVL抠图 → 尺寸/锚点对齐 → 按明确请求接入第一关和对应动画 → 通知用户开游戏验收。动作接入的完整步骤与不变量见 `skill/workflows/action-to-unity-deployment.md`，阶段入口见 `skill/workflows/character-action-pipeline-entry.md`。用户未明确要求时不进入运行自检。

### 0.3 本机素材与留档

| 文件 | 用途与当前状态 |
|---|---|
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png` | 已验收定妆原图；1024×1024、RGB、983,338 bytes；SHA-256 `446d6d558baa7d4a5d6e6fd9bf712c07b99c63a1c4f2e711cc0b28a7564427d0` |
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.request.json` | 已提交英文原词、参数、两图来源/处理/哈希；4,924 bytes；不是视频请求 |
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.response.json` | 原始 HTTP 成功响应、图片 URL 与 usage；403 bytes，未返回费用或 task id |
| `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` | 获批视频参考副本；1024×1024 RGB，344,540 bytes；SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`；同名 `.processing.json` 记录本地处理 |
| `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.request.json` | 已提交视频词、参数、参考图哈希与 task ID；5,976 bytes；已回读核验 |
| `Library/Locus/tmp/enemy1021_idle_video_v1/runtime.json` | 运行与限次查询记录；创建脚本在 `Library/Locus/tmp/create_enemy1021_idle_video_v1.py`，已执行，勿再次运行 |
| `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.mp4` | 已下载；2,000,007 bytes；SHA-256 `808593f9cbc5b9132ba7992f5ad62159f77be0ea6e4eb5c2feaa04b0c07fce2f`；960×960/24fps/97帧 |
| `Library/Locus/tmp/enemy1021_idle_v1_frames/keyed_ui_t79/` | 其他AI已完成的四帧透明素材；f001/f025/f049/f073；RGBA、半透明0、幕布残留0 |
| `Assets/Sprites/Enemy/Enemy1021/` | 已部署 `Enemy_1021_idle1..4.png`；1108画布固定偏移补边，保持现有1021之外资产不变 |
| `Assets/Animations/Enemy_1021_Idle.anim` | 已创建并接入Idle状态；4帧、30fps、0.96s循环，含旧ShieldVisual null键 |
| `Library/Locus/tmp/enemy1021_idle_deploy_v1/deploy_report.json` | 本次部署参数、哈希、Prefab与运行验证记录 |
| `Library/Locus/tmp/enemy1021_idle_deploy_v1/runtime_verification.json` | 自然生成、循环、旧盾清空和对象池复用的运行验证记录 |
| `C:/Users/steam/Videos/doubaoVideo/enemy1021_shield_idle_v1.download.json` | 下载与容器/完整解码报告；同任务，usage/cost已记录 |
| `Library/Locus/tmp/enemy1021_idle_video_v1/contact_sheet.png` | 0/24/48/72/96检查帧接触表，已回读；不是正式选帧或循环验收 |
| `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png` | 1011 主体画风与人物基底；后续动作以 1021 v3 为直接身份锚点 |
| `C:/Users/steam/Downloads/微信图片_20260928101134_12_223.png` | 用户盾牌构造/材质来源，仅用于追溯定妆，不作为新动画的姿态锚点 |
| `Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/` | 本次两张实际上传副本；当前存在且哈希匹配，可能被清理，重建方法见 §3.2/请求 JSON |

交接前已再次检查：上述 PNG 和两份 JSON 可读，原图与已记录哈希匹配；请求英文原词 SHA-256 为 `246e7766ee2b916fdc2070a91d62b9b9a691cdfc43c7f3e8e433ae5b67eec4be`。图片和 JSON 在本机用户目录，**未复制进项目、未成为 Unity 资产，也不随 Git 自动迁移到另一台机器**；换机器时先确认文件可用，不能因本机路径缺失自行重新收费生成。

### 0.4 已确认约束与本轮教训

- **人物**：1011 本人改剑为盾，共享体型、头盔和甲胄；保持 Q 化比例、严肃战斗感、冷铁灰/朱红/暖棕/暖金关系。不能把 1011 降为泛泛“风格参考”，再另画一个盾兵；不再上传 109 重装骑兵或早期失败候选定义本角色。
- **姿态**：角色右手对应画面左侧，盾已举在胸腹前方偏左，手/手指/前臂/握把藏在盾后；画面右侧左手保持空垂，头盔/面罩/眼光可读。“固定持盾”指不做举起/收回切换，不是禁止举盾应战。
- **盾与画风**：沿用已验收 v3 的厚木板、暗铁件、铆钉、硬边体块与阶梯轮廓，无参考图红黑纹章。不要为了显示握持而重新露出手指，不写固定两三色、2～3 像素铆钉或红色占比 3% 等无依据限制。
- **比例判断**：先与原图并排看头/肩/躯干/腿的关系；画布占比不等于头身比，低 Alpha 外围不能当主体尺寸。前面“4.5 头身”“原图 669px 所以输出 540px 比例错了”等推断已撤回，不作为制作要求。
- **完成状态以当前工程为准**：文档里的“未通过/未完成/暂停”只描述当时那次尝试；用户明确指定新方向后，同一动作可继续推进并最终验收，旧结论不得覆盖新验收。
- **生成纪律**：所有 GPT Image 请求固定 `quality=max`，编辑/融合另用 `input_fidelity=high`；参数不保证姿态或像素锁定。每次实质改词/参考/参数后，先在对话呈现完整中英文词、负面词、参考职责和规格供审；不要只给摘要或job路径。获批后仅提交一次，收费失败/超时也不自行重试。
- **角色绿幕门槛**：视频必须先准备同一明确色值的绿幕上传副本并量测，常用RGB(0,177,64)。生成图看上去绿不等于恒色；不能沿用浅灰底或仅在词中写绿底却不检查实际上传素材。保留原图，副本归一化只改背景，不能全图调色。
- **绿幕规则复盘（2026-10-16，修正2026-10-15的判断）**：实测七支已接入/已验收视频的 raw 帧，结论三条。① **生成器不保上传色值**：上传 `(0,177,64)` 的 Idle/Walk/Attack 成片落在 G 155–172，上传 `(0,159,62)` 的破盾落在 141–146；单帧内也不均（Idle 成片边框有 31 种颜色）。② **但它保色族**：绿幕上传出绿幕，灰底上传出灰底——GuardHit 与 HitRight v2 两支上传灰底，成片也真是灰底，事后必须本地分离。③ 因此规则本意是“**上传饱和单色幕布**”，不是“上传值等于某个常数”；写死数值锁不住任何东西，要保证的是“进的是绿幕”。
  - 据此修正：`enemy1021_shield_hold_v3_green_v1.png` **不构成违规**。它的绿底场是干净的——四角精确 `(0,177,64)`、std 0、全图非背景区**只有 1 个连通块**（即主体）、杂点 0；SHA 与已验收 IDLE/WALK/ATTACK 三份 request 记录里的参考图完全一致，并且等于 HitFlash 上传副本 `01_idle_green_00B140.png`。它就是四支已验收视频实际用的那一份。
  - 真正的差异在**轮廓环的来源**：已验收的 009F3E 破盾副本是拿“已绿幕的成片 raw 帧”做连通带替换再平移补边（主体 RGB 改动 0 像素），轮廓环绿向 94%；而 green_v1 从浅灰定妆图直换底，轮廓环绿向仅 5%、近灰混色 351 px（11.7%）。这是已知限制，用户已接受，不再改图。
  - 本轮曾据“轮廓带灰”误判 green_v1 违规并产出两份替代副本；用户于 2026-10-16 选定方案 A（沿用 green_v1）。两份替代副本**均未被采用**，不得自动替换。
- **工具结果纪律（2026-10-16）**：本次编辑交接文档首次连续三轮只写了“已更新”却从未发出 `edit` 调用，文档始终停在原状。完成性表述只能依据实际工具结果，改动后必须回读目标文件的 mtime/行数/关键词命中作为证据；详见 `memory/tool-result-verification.md`。
- **技术成功不等于动作成功**：HTTP200、PNG可读、Alpha合格或Prompt数字达到请求都不是用户验收；v1–v4关键图失败说明不把最后一版或“还不如v1”的比较自动设为新基准。用户后来另指定v1作视频参考属新的明确授权，不代表当时v1已通过。
- **盾生命周期**：持盾画在本体帧内；ShieldBreak 表现脱手后由引擎驱动独立盾对象。正式脱手帧、盾精灵接管、死亡/击飞打断与池回收仍待实现，不恢复旧分离持盾层路线。
- **范围保护**：机制、数值、现有状态名契约和102的资产保持既定方案。Idle已部署，Walk/Attack/ShieldGuardHit/HitFlash已接入并获用户游戏验收；后续只按用户指定继续未完成动画与机制，不重复制作已验收动作，不额外改全局画风文档或Skill。

### 0.5 工作树与后续未决项

- **工作树快照（本次文档编辑前）**：`git status --short --branch` 显示分支 `route-scroll-movement`；已修改 `Assets/Resources/EnemyPrefabs/Enemy_101.prefab`、`Assets/Resources/EnemyPrefabs/Enemy_109.prefab`、`Assets/RouteData/Stage01/N1_ArtPreview_Battle.asset`、`Locus/knowledge/memory/user-preference.md`、`Locus/knowledge/memory/video-generation-case-log.md`、`Locus/workspace-trees/default.json`；`Directory.Build.props` 已删除；未跟踪 `Assets/Library.meta` 与 `nul`。这是并行工作树状态，不归因于1021素材任务，不恢复/删除陌生改动。
- 本次交接文档修改会额外使两份1021 plan文档变更。旧交接“1021资产与Design未跟踪”的描述是历史快照，不再当作当前事实。下一对话重新运行git status保护用户与并行改动，本次未提交Git。
- Walk六张、Attack十二张、ShieldGuardHit六张与HitFlash六张Sprite及对应Clip的部署记录保留。HitFlash正式Sprite、Clip及.meta已在Assets，新文件尚未提交Git，1021 Controller的motion替换仍为工作树改动；HitRight素材仍在用户目录/Library，未部署。原片、候选与请求不要因清理任务丢弃。
- Idle已部署，Walk/Attack/ShieldGuardHit/HitFlash游戏验收通过。其他正式动作及独立掉盾对象接管等待用户指定；默认不运行自检。
- 更多动作与机制契约分别见 `design/enemy-1021-animation.md`、`design/enemy-1021-shield-guard.md`；详细生成台账见 `memory/video-generation-case-log.md` 的“Enemy 1021 持盾 Idle 定妆图 v3”。

可复制到新对话的开场指令：

> 请读取 `Locus/knowledge/plan/enemy1021-art-production-handoff.md` 顶部最新停点。现状：持盾死亡已游戏验收；CowardIdle 的呼吸最高点图、循环视频与 6 帧透明素材均已验收；`CowardHit` / `CowardLaunched` / `CowardDead` 仍为 102 占位、无素材。规格在 `plan/enemy1021-coward-family-plan.md` §3/§4。本侧不写生成词。

---

### 0.6 【历史】持盾死亡 图2 关键图 · 已定稿生图词（定稿于 2026-10-16；已据此生成 v1，未通过，见 §0.7）

> **本节用途与当前状态**：本节保存**已提交过一次的历史定稿词**（实际提交词 SHA-256 `631829e8…5903`），供修正时对照使用。
> **本节已不再是“直接执行”指令**：v1 已生成并**用户验收不通过**（见 §0.7）。**不要直接重跑本节旧词**；下一版必须先按 §0.7 的三处修正改完，把完整中英文词、负面词与参数重新贴进对话经用户批准，才能提交。
> 它不是视频词；图2 通过前不写视频词、不提交视频。

#### 这张图要干什么

图2 是**单张图像编辑请求**（不是视频）。输入=图1（已验收持盾 Idle），输出=同一个角色被一记来自画面正前方的隐形重击打死之后的**空中死重终态**。它的职责只有两个：① 给视频提供“末帧必须精确到达”的锚点；② 保住身份不被重设计。

**静帧只画终态，不画过程。** 1011 视频词里的“胸口上顶、脊柱短促后弓”发生在视频前段；若与“随后脊柱松开、变成死重”同时写进这一张图，模型会取平均姿态。因此本节英文词只锁松开后的空中尸体。后弓留给以后的视频词，不在本图里要求保持。

#### 必须锁定的身份（不得重设计）

以图1 为准，逐项保留：头盔与红缨、面罩与橙色眼光、红围巾、冷灰甲胄（含肩甲/裙甲/臂甲/腿甲）、暖棕皮靴、体型与头身比、粗重像素描边与硬块阴影、以及**画面左侧那面厚木盾**（模板、铁件、铆钉、硬边体块）。

不得改动：不得把人物换成剑兵；不得改盾的形制或换手；不得加重/减轻护甲；不得提高或降低像素密度。

#### 必须准确的死亡读点（这是本图的核心）

- **头**：受击瞬间向后并转向画面右侧，面罩离开镜头，**不再看向观者**；颈弯、下巴朝上并偏开。
- **躯干（本图只画终态）**：胸口已经落下，脊柱松开；整具身体是“空中死重”，没有支撑，也不保持后弓。后弓只存在于以后的视频前段，不画进这张静帧。
- **四肢**：双膝松折、小腿自然下垂拖后；画面右侧的**空手张开并被惯性甩出**。
- **盾**：画面左侧手臂也被甩开，**但木盾仍连在那只手上并随之前行**；握盾的手、前臂、握把继续藏在盾后。盾是**被打偏**，不是脱手、不是破碎，也不是第二面盾。
- **离地幅度**：只表现“靴子刚离开地面”，全身上移不超过自身站立高度的 **8%–12%**；大幅飞行与旋转由引擎在后续完成，**不在图内表现**。

#### 硬约束（必须逐条写进词里）

- 画布 1:1；角色完整入框，**四边至少 15% 空白**；任何部位不得贴边或被裁切。
- **不得整体放大**：头/躯干/四肢/盾的绘制尺寸与图1 一致，只改角度与位置；不得拉伸、不得膨胀轮廓。
- **不得整体旋转或翻滚**：不出现侧面、3/4 视角、镜像或整身翻转；动作发生在画面平面内，正面朝向镜头。
- 镜头锁定：不缩放、不平移、不旋转、不震动。
- 背景：纯色绿幕 RGB(0,177,64)，无渐变、无地面、无阴影、无场景、无道具。
- 无血迹、无伤口喷溅、无火花、无烟尘、无冲击闪光、无速度线、无动态模糊。
- 无第二个人、无飞入画面的武器、无文字、无 UI。
- 姿态必须与图1 **肉眼可辨地不同**；不得只是站姿微调。

#### 提交用英文词（v1 已提交过，用户验收不通过；不要再提交）

上一版草稿同时写了“胸口上顶、脊柱后弓”和“随后变成死重”，已废弃。本段是 v1 实际提交词，保留对照。v2 见 §0.7，未批准前也不要提交。

```text
Edit the provided image into one still 2D game sprite. This is the final held frame of a death animation, not a video and not a cinematic. Keep the same shield soldier: helmet and red plume, face opening, orange eyes, red scarf, cold-grey plate armour, warm-brown boots, body proportions, thick dark pixel outline, hard block shading and the same thick wooden shield on screen-left. Do not redesign him and do not turn him into a sword soldier.

He was just killed by one heavy invisible blow from straight ahead. The attacker and the weapon stay outside the frame. Draw only the resulting airborne corpse, after the arch has already released: dead weight hanging in the air, not the instant of impact.

HEAD: thrown back and toward screen-right. The neck is bent, the chin points up and away, and the face opening has rotated away so he no longer looks at the camera. The helmet rests back toward the screen-right shoulder.

TORSO: the chest has settled and the spine has uncurled. The body hangs limp, with no braced posture and no held back-arch.

LIMBS: both knees are loosely folded and the lower legs trail downward. The empty screen-right hand is open and flung outward by inertia. The screen-left arm is also flung outward, but the wooden shield stays attached to that hand and moves with it. The gripping hand, forearm and grip remain hidden behind the shield. The shield is only knocked aside: not dropped, not broken, not split and not duplicated. The red scarf trails with the thrown head.

LIFT: both boots have just left the ground. The whole figure rises by no more than 8 to 12 percent of its standing height and stays centred in the same slot as the source image. This is not a high launch and not a jump.

CONTAINMENT: 1:1 canvas. The complete figure, including plume, shield, both hands and both boots, stays fully inside the frame with at least 15 percent empty margin on every side. If a pose would touch an edge, make that pose more compact. Never crop it.

SCALE AND VIEW: the head, torso, limbs and shield keep the same drawing size as the source. Change only joint angles and positions. Do not enlarge, shrink, inflate or stretch the character. No whole-body rotation, spin, roll, mirror, side view or three-quarter view. The action stays in the picture plane, seen straight on. Locked camera: no zoom, pan, tilt or shake.

Background: flat chroma green RGB(0,177,64), no gradient, floor, ground shadow, scenery or props. No blood, wound, sparks, smoke, dust, impact flash, speed lines or motion blur. No second character, no flying weapon, no text and no UI.
```

中文对照（不提交，仅供审阅）：

> 把提供的图改成一张 2D 游戏精灵静帧，是死亡动画的终态保持帧，不是视频。身份全保留：头盔与红缨、面罩、橙眼、红围巾、冷灰甲、暖棕靴、体型、粗黑像素描边、硬块阴影，以及画面左侧那面厚木盾。不得重设计，不得改成剑兵。
>
> 他刚被画面正前方一记隐形重击打死，攻击者和武器不入画。只画冲击已经结束之后的空中尸体：死重悬在空中。不要画受击瞬间，也不要保持后弓。
>
> 头甩向后并偏向画面右侧，颈弯，下巴朝上偏开，面罩转离镜头，不再看玩家，头盔靠向画面右侧肩膀。胸口已经落下，脊柱松开，身体瘫挂，没有支撑，也没有保持后弓。双膝松折，小腿向下拖。画面右侧空手张开并被甩出。画面左侧手臂也甩开，但木盾仍连在那只手上一起动；握盾的手、前臂和握把继续藏在盾后。盾只是被打偏，不掉、不裂、不分成两块、不出现第二面盾。红围巾随甩头拖出。
>
> 双靴刚离地，全身上移不超过站立高度的 8%–12%，人留在原站位中心。不是高飞，也不是自己跳起。1:1 画布；红缨、盾、双手和双靴全部入框，四边至少 15% 空边，会贴边就改得更紧，不许裁切。绘制尺寸与原图一致，只改关节角度和位置。禁止整身旋转、翻滚、镜像、侧面和 3/4 视角。镜头锁定。纯色绿幕 RGB(0,177,64)，无地面、阴影、场景、血迹和特效。

#### 负面词（与英文词一起提交；已去掉旧草稿里会否定“空中尸体”的 `no dead corpse`）

```text
no voluntary jump, no attack, no guard pose, no braced stance, no held back-arch, no second hit, no attacker, no impact flash, no blood, no wound spray, no dust, no debris, no smoke, no speed lines, no shockwave, no camera movement, no motion blur, no fall to the ground, no landing, no crouch, no kneel, no lying-down death pose, no return to Idle, no standing recovery, no floor, no scenery, no ground shadow, no shadow, no text, no UI, no checkerboard, no extra character, no extra weapon, no dropped shield, no broken shield, no split shield, no second shield, no visible gripping hand, no exposed forearm, no sword, no cropped limb, no cropped shield, no body leaving the frame, no enlarged character, no whole-body rotation, no side view, no three-quarter view
```

#### 参数（不得自行降档）

| 项 | 值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst` |
| 接口 | `POST https://api.muskapis.com/v1/images/edits` |
| quality | **max** |
| input_fidelity | **high** |
| size | 1024x1024 |
| background | opaque |
| output_format | png |
| n | 1 |
| 参考图 | 仅图1（`image[]`，普通编辑输入） |

#### 发起一次生成（历史执行记录；下次不可直接照此重跑，先读 §0.7）

1. 本次已按本节词提交**一次**，产物与失败反馈见 §0.7。
2. **不得重跑本次旧词**。下一版先把 §0.7 要求的三处修正改完，重新贴完整中英文词与参数经用户批准，才提交一次。
3. 仍为**只提交一次**：失败、超时、空结果或观感不佳都**不自行重试**，也不改词、不降 `quality` / `input_fidelity` 后重发。
4. 只上传图1，字段为普通编辑输入 `image[]`。不上传 1011 死亡帧、109、102 占位或任何旧候选，不使用 `first_frame` / `last_frame`。
5. 视频以后另用两张普通 `reference_image`（图1 + 目检通过的图2），规格沿用 4 秒 / 720p / 1:1 / 无音频 / 无水印 / seed=-1。

#### 素材入口（写词者可能需要的上下文）

| 用途 | 路径 |
|---|---|
| 图1（身份与首帧，已定） | `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` |
| 1011 死亡帧（仅本地对照，**不得上传**） | `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_dead1..6.png` |
| 1011 死亡已验收完整原词 | `memory/enemy1011-animation-prompt-archive.md` §4 |
| 1011 死亡姿态复用经验 | `memory/enemy1011-animation-pose-reference.md` §3.6 |
| 写词五层结构与正面视角铁律 | `memory/hit-animation-video-pipeline-lessons.md` |

### 0.7 【历史】持盾死亡 图2 v1 否决记录与 v2 修正（2026-10-16；v2 已验收通过，见顶部最新停点）

> **本节用途**：保留 v1 否决原因与 v2 修正过程，供追溯。**当前状态：v2 已生成并经用户验收通过**（见顶部最新停点与下节）。本节已不是行动指令；随后的视频词见 §0.8，该视频已验收。

#### 用户反馈（原话要点，验收依据）

| # | 用户指出的问题 | 含义 |
|---|---|---|
| 1 | 姿态诡异，**像是角色飞升而非死亡** | 读成了被挑飞/升天，不是“被一记重击打死、悬空死重”；不是死亡语义 |
| 2 | 角色**同步朝向发生了严重改变，完全看向了左边** | 朝向偏了，不再正面朝镜头；这是硬性禁止项（§0.6 已写 `no side view / no whole-body rotation`，仍发生） |

#### 代理客观测量（与用户反馈互相印证）

| 指标 | 图1（源） | 图2 v1（生成） | 判定 |
|---|---|---|---|
| 主体主轴相对竖直偏移 | 1.4° | **46.5°** | 逆约束：整身旋转约 45° |
| 盾的木板区宽×高 | 429×466 | **571×575** | 逆约束：宽比 1.331、高比 1.234，被整体放大 |
| 红色区（头/缨）质心 x | 509（画布中心 512） | 528 | 向右偏移，但幅度不足以证明朝向正确 |
| 背景众数色 | (0,177,64) | (2,177,66) | 达标 |
| 四角 std | 0 | 0.58–0.93 | 达标 |
| 最小边距 | 191 px（18.7%） | 201 px（19.6%） | 达标（要求 ≥15%） |

结论：几何约束（背景、边距、完整入框、盾未脱落或分裂）基本达标；**失败的是姿态语义与朝向，加上尺寸漂移**。

#### v1 失败后的三条修正（已写入下方 v2 词）

1. **正面改成可判读指标**：躯干主轴偏离竖直不超过 15°，胸甲和双肩正对镜头，橙色眼光仍从面罩朝镜头可见。v1 把“头向后、面罩转离”做成了整身朝左，v2 不再要求面罩离开镜头。
2. **尺寸改成包围盒**：头盔、胸甲、盾和靴的绘制尺寸与图1 一致，主体包围盒相对图1 的 439×587 变化不超过 10%。v1 实测盾区放大到 1.33×/1.23×。
3. **死亡改成失重下坠，不写成上升**：双脚只离开地面一小段，身体没有被向上托起。头只是垂向画面右侧，不再写甩向后或仰天，避免再读成飞升。

#### v2 提交用英文词（已改完，未提交；用户批准前不要发）

```text
Edit the provided image into one still 2D game sprite: the final held frame of a death animation, not a video. Keep the same shield soldier and the same front-facing camera. Helmet, red plume, face opening, orange eyes, red scarf, cold-grey armour, warm-brown boots, body proportions, thick dark outline, hard block shading and the thick wooden shield on screen-left all stay the same. Do not redesign him and do not turn him into a sword soldier.

He has already been killed by one invisible blow from straight ahead. The attacker stays outside the frame. Draw the body after the blow, hanging as dead weight. He is not rising, not being lifted and not ascending. Both boots are only a short distance off the ground, and the body is about to drop, not fly upward.

FRONT VIEW, HIGHEST PRIORITY: the torso stays upright and square to the camera. Its long axis stays within 15 degrees of vertical. Both shoulders and the chest plate face the viewer. The orange eyes remain visible through the face opening and still point toward the camera. The head only slumps toward screen-right; it does not turn the whole body, and the helmet does not rotate into a left-facing or side view.

POSE: both shoulders drop, both knees fold loosely, and the lower legs hang down. The empty screen-right hand opens and hangs outward. The screen-left arm hangs slightly outward, but the wooden shield stays on that hand at the same size as the source. The gripping hand, forearm and grip remain hidden behind the shield. The shield is knocked a little aside, not dropped, not broken, not split and not duplicated. The red scarf hangs down with the slumped head.

SCALE: helmet, chest, shield and boots keep the same drawing size as the source image. The full-body bounding box stays within 10 percent of the source box, 439 pixels wide by 587 pixels tall. Do not enlarge, shrink, inflate or stretch the character. Change joint angles only.

PLACE: keep him centred in the same slot. 1:1 canvas, every part fully inside the frame, at least 15 percent empty margin on every side. No whole-body rotation, spin, roll, mirror, side view or three-quarter view. Locked camera: no zoom, pan, tilt or shake.

Background: flat chroma green RGB(0,177,64), no gradient, floor, shadow, scenery or props. No blood, wound, sparks, smoke, dust, flash, speed lines or motion blur. No second character, no flying weapon, no text and no UI.
```

中文对照（不提交，供审阅）：

> 把提供的图改成一张死亡终态静帧。同一个盾兵，同一个正面镜头。头盔、红缨、面罩、橙眼、红围巾、冷灰甲、暖棕靴、体型、粗描边、硬块阴影和画面左侧厚木盾都保持原样。不得重设计，不得改成剑兵。
>
> 他已经被画面正前方的一记隐形重击打死，攻击者不入画。只画打击结束后的失重身体。他没有继续上升，没有被托起，也不是飞升。双脚只离开地面一小段，身体正要往下掉。
>
> 正面是最高优先级：躯干保持竖直，正对镜头，长轴偏离竖直不超过 15°。双肩和胸甲朝向观者。橙色眼光仍从面罩里朝镜头可见。头只垂向画面右侧，不能带动整个人转向，头盔不能转成朝左或侧面。
>
> 双肩下沉，双膝松折，小腿下垂。画面右侧空手张开并垂向外。画面左侧手臂略向外垂，木盾仍在那只手上，尺寸与原图相同；握盾的手、前臂和握把继续藏在盾后。盾只是被打偏一点，不掉、不裂、不分成两块、不出现第二面盾。红围巾随垂头向下挂。
>
> 头盔、胸甲、盾和靴的绘制尺寸与原图一致。全身包围盒相对原图 439×587 的变化不超过 10%。只改关节角度，不放大、缩小、膨胀或拉长。人留在原站位中心，1:1 画布，全部入框，四边至少 15% 空边。禁止整身旋转、翻滚、镜像、侧面和 3/4 视角。镜头锁定。纯色绿幕 RGB(0,177,64)，无地面、阴影、场景、血迹和特效。

v2 负面词（与英文词一起提交）：

```text
no ascension, no rising pose, no upward launch, no being lifted, no triumphant pose, no floating hero, no voluntary jump, no attack, no guard pose, no braced stance, no held back-arch, no head thrown straight up, no face turned away, no torso turned to screen-left, no profile torso, no head turned away from camera, no whole-body rotation, no side view, no three-quarter view, no enlarged character, no enlarged shield, no second hit, no attacker, no impact flash, no blood, no wound spray, no dust, no debris, no smoke, no speed lines, no shockwave, no camera movement, no motion blur, no fall to the ground, no landing, no crouch, no kneel, no lying-down death pose, no return to Idle, no standing recovery, no floor, no scenery, no ground shadow, no shadow, no text, no UI, no checkerboard, no extra character, no extra weapon, no dropped shield, no broken shield, no split shield, no second shield, no visible gripping hand, no exposed forearm, no sword, no cropped limb, no cropped shield, no body leaving the frame
```

#### v2 参数与提交边界（批准后才执行）

| 项 | 值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst` |
| 接口 | `POST https://api.muskapis.com/v1/images/edits` |
| quality / input_fidelity | max / high |
| size / background / format / n | 1024x1024 / opaque / png / 1 |
| 参考 | 仅图1，普通 `image[]` |
| 建议输出 | `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v2.png` |

只上传图1。不上传 v1 失败图，也不上传 1011 死亡帧。只提交一次；失败或超时不自行重试，不改词、不降参数重发。生成后先交用户目检。通过前不写视频词、不提交视频。

#### 项目内可选对照材料（仅本地查看，**不得上传**）

| 用途 | 路径 |
|---|---|
| v1 生成图（失败样本） | `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v1.png` |
| v1 与图1 并排对照 | `Library/Locus/tmp/enemy1021_dead_keypose_v1/DEAD_KEYPOSE_V1_VS_IDLE_REVIEW.png` |
| v1 像素 2× 放大对照 | `Library/Locus/tmp/enemy1021_dead_keypose_v1/DEAD_KEYPOSE_V1_PIXEL_ZOOM.png` |
| 1011 死亡帧（终态参考） | `Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_dead1..6.png` |
| 1011 死亡已验收原词 | `memory/enemy1011-animation-prompt-archive.md` §4 |

#### 纪律

- 本次失败后**不自行重发、不改参数重发、不降档重发**；也不因为“还不如 v1”之类比较自动换回旧版。
- v2 词已写在本节，但写入文档不等于批准。提交前仍要把完整英文词、中文对照、负面词和参数贴进对话，用户批准后才提交一次。
- 不要重跑 §0.6 的 v1 词，也不要上传 v1 失败图当参考。
- 图2 未通过前不写视频词、不提交视频。

### 0.8 【历史】持盾死亡（Dead）视频生成词（2026-10-16；已据此生成 v1，未通过，见 §0.9）

> **本节用途**：保存 v1 的历史定稿词（实际提交词之英文正文 SHA-256 `80a9d532…ac8d` + 负面词 `ef2db090…b9de`）。
> **当前状态**：v1 已生成并**用户验收不通过**（见 §0.9）。**不要直接重跑本节旧词**；下一版先按 §0.9 三处修正改完，重新贴完整中英文词与参数经审批。
> **前置已就绪**：图1 与图2 v2 均已验收，可直接作为两张普通 `reference_image`。

#### 两张参考图的职责（硬规定）

| 图 | 路径 | 职责 |
|---|---|---|
| 图1 | `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` | **首帧**：身份、绘制比例、根与双脚、持盾站姿、开头 Idle |
| 图2 | `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v2.png` | **末帧**：必须精确到达的空中死重姿态 |

两张都用普通 `reference_image`，**不混 `first_frame`/`last_frame`**。不上传 v1 失败图、1011 死亡帧、109 或 102 占位。

#### 规格（沿用已验收视频的参数）

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 4 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无音频 / 无水印 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v1.mp4` |
| 提交次数 | **只提交一次**；失败、超时、结果不佳都不自行重试 |

若认为 4 秒不够容纳“受力 → 腾空 → 保持”，可在词里写清楚阶段节奏；**是否改成更长时长需先问用户**，不自行改规格。

#### 必须遵守的语义（来自已验收的图与设计，不得改写）

1. **这是一次死亡，不是击飞，也不是自己跳**。一记来自画面正前方的隐形重击；攻击者与武器不入画；全程只有一次打击。
2. **首帧就是图1 的持盾站姿**，**末帧必须精确到达图2**。
3. **整体朝向保持正面**。这是 v1 失败的主因（被读成飞升 + 整体转向画面左侧）：不得出现整身旋转、翻滚、侧面、3/4 视角或镜像；躯干长轴偏离竖直不宜超过约 15°；胸甲与双肩朝镜头，橙色眼光可见。
4. **不得上升、不得被托起**。双脚仅离地很小一段（对应图2 的 8%–12% 高度）；随后保持空中死重，**不落地、不回 Idle**。引擎会在之后接管世界的飞行与回收。
5. **盾始终握在画面左侧那只手上**：不掉落、不破裂、不分成两块、不出现第二面盾；握盾的手、前臂与握把始终藏在盾后。
6. **不得整体放大**：各部件绘制尺寸与图1 一致（图2 已收回到包围盒宽比 1.034），全程不得膨胀或拉伸。
7. **无特效**：无血迹、无伤口喷溅、无火花、无烟尘、无冲击闪光、无速度线、无动态模糊。
8. **镜头锁定**：不缩放、不平移、不旋转、不震动。背景保持同一平面绿幕，不出现地面、阴影或场景。
9. **防超框**：四边至少 8%–10% 空白；会贴边就把姿势做紧凑，不裁切（1011 的实测经验：动作量大的段必须带这条，否则“动作到位”会以出框为代价）。

#### 措辞陷阱（已踩过，必须避开）

- **不要写“后仰/向后”**：正面视角无法投影，会被做成左右倾斜或整体旋转。改用正面可见读点：双肩下沉、颈压缩、膝受力微屈、手臂惯性滞后。
- **不要写 `slightly wider` / “变宽·变厚”**：会被实现成整体放大。写死“任何时刻整体尺寸不大于图1”。
- **不要写“头向后仰天”**：v1 因此被读成飞升。改为“头垂向画面右侧”。
- **不要只靠负面词锁朝向**：v1 已写了 `no whole-body rotation` 仍发生转向；必须同时在正文给出**可判读的正面硬指标**。

#### 提交用英文正文（已写完，未批准前不要发）

时长保持 4 秒。动作发生在前 1.2 秒，后面只保持图2，方便选帧。不把“后仰、变宽、仰天”写进正文。

```text
This is a 2D game sprite animation, not a cinematic. One front-facing shield soldier is killed by a single invisible blow from straight ahead. The attacker and the weapon stay outside the frame. This is death, not a jump, not a launch and not a second attack.

Image 1 is the opening held pose: the same standing shield soldier, shield on screen-left, both boots planted. Image 2 is the final held pose, and the animation must arrive at that pose exactly: head slumped toward screen-right, shoulders dropped, knees loose, both boots only a short distance off the ground, shield still on the screen-left hand. Use both images only as ordinary reference images.

FRONT VIEW IN EVERY FRAME: the chest plate and both shoulders face the camera. The torso's long axis stays within 15 degrees of vertical. The orange eyes stay visible through the face opening. The head may slump toward screen-right, but it must not turn the torso, and the body must not rotate into a side view or a left-facing view.

TIMING, ONE HIT ONLY:
0.00-0.20s: hold image 1.
0.20-0.45s: the blow lands. Both shoulders drop, the neck shortens, the knees bend, and the empty screen-right hand opens. He does not wind up and he does not jump.
0.45-1.20s: both boots leave the ground by only 8 to 12 percent of his standing height. The body hangs as dead weight and settles into image 2. He is not rising and he is not being lifted.
1.20-4.00s: hold image 2, with only slight drift. No landing, no second hit and no return to the standing pose.

SHIELD: the wooden shield stays attached to the screen-left hand in every frame. The gripping hand, forearm and grip stay hidden behind it. The shield may shift a little with the arm, but it is not dropped, broken, split or duplicated.

SCALE AND FRAME: the helmet, chest, shield and boots keep the same drawing size as image 1 in every frame. Never enlarge, shrink, inflate or stretch the character. He stays centred in the same slot. Keep at least 10 percent empty margin on all four sides; if a pose would touch an edge, make that pose more compact instead of cropping it.

CAMERA AND BACKGROUND: locked front camera, no zoom, pan, tilt or shake. Keep the same flat chroma-green background, with no floor, shadow, scenery or props. No blood, wound, sparks, smoke, dust, flash, speed lines or motion blur. No second character, no flying weapon, no text and no UI.
```

中文对照（不提交）：

> 这是一段 2D 游戏精灵动画，不是电影。一个正面盾兵被画面正前方的一记隐形重击打死。攻击者和武器不入画。这是死亡，不是跳，不是击飞，也不是第二下攻击。
>
> 图1 是开头保持的持盾站姿，盾在画面左侧，双脚着地。图2 是必须精确到达的终态：头垂向画面右侧，双肩下沉，双膝松开，双脚只离开地面一小段，盾仍在画面左侧那只手上。两张图都只作普通参考图。
>
> 每一帧都保持正面：胸甲和双肩朝向镜头，躯干长轴偏离竖直不超过 15°，橙色眼光仍从面罩可见。头可以垂向画面右侧，但不能带动躯干转向，也不能转成侧面或朝左。
>
> 节奏只有一次打击：0.00–0.20 秒保持图1；0.20–0.45 秒受击，双肩下沉、颈缩短、膝弯曲、画面右侧空手张开，不蓄力、不自己跳；0.45–1.20 秒双脚离地，高度只有站立高度的 8%–12%，身体变成死重并落到图2，不继续上升、不被托起；1.20–4.00 秒保持图2，只允许轻微漂移，不落地、不再挨打、不回到站姿。
>
> 木盾每一帧都连在画面左侧那只手上。握盾的手、前臂和握把一直藏在盾后。盾可以随手臂小幅移动，但不掉、不裂、不分成两块、不出现第二面盾。
>
> 头盔、胸甲、盾和靴的绘制尺寸每一帧都与图1 一致，不放大、缩小、膨胀或拉长。人留在原站位中心。四边至少 10% 空边，会贴边就把姿势做紧，不许裁切。镜头锁定。背景保持同一平面绿幕，无地面、阴影、场景、血迹、特效、第二个人、飞入武器、文字和 UI。

负面词（并入同一 prompt，无独立负面字段）：

```text
no ascension, no rising pose, no upward launch, no being lifted, no triumphant pose, no voluntary jump, no second hit, no attack windup, no head thrown straight up, no face turned away, no torso turned to screen-left, no profile, no side view, no three-quarter view, no whole-body rotation, no spin, no roll, no mirror, no enlarged character, no inflated silhouette, no stretched limbs, no dropped shield, no broken shield, no split shield, no second shield, no visible gripping hand, no exposed forearm, no sword, no landing, no crouch, no kneel, no lying down, no return to idle, no standing recovery, no attacker, no blood, no wound, no sparks, no smoke, no dust, no flash, no speed lines, no motion blur, no camera movement, no floor, no shadow, no scenery, no text, no UI, no extra character, no flying weapon, no cropped limb, no body leaving the frame
```

自检：9 条语义都在正文里，包括一次死亡、首尾图职责、正面 15°、不大升、盾不脱手、尺寸锁定、无特效、镜头锁定和 10% 防超框。4 条措辞红线也避开了：没有写后仰、变宽、仰天，朝向不只放在负面词里。时长仍是 4 秒，没有自行加长。

#### 原任务书交付清单（本轮已完成，保留备查）

1. 英文正文：见上，分受力、离地、保持。
2. 中文对照：见上，不进请求。
3. 负面词：见上，并入同一 prompt。
4. 参考职责与参数：仍用本节前面的两张表。
5. 自检：见上。

#### 素材与上下文入口

| 用途 | 路径 |
|---|---|
| 图1（首帧，已验收） | `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` |
| 图2 v2（末帧，已验收） | `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v2.png` |
| 1011 死亡视频已验收原词（结构参照） | `memory/enemy1011-animation-prompt-archive.md` §4 |
| 写词五层结构与正面视角铁律 | `memory/hit-animation-video-pipeline-lessons.md` |
| 1011 死亡姿态与阶段职责 | `memory/enemy1011-animation-pose-reference.md` §3.6 |
| 本项目与另一侧交接说明 | §0.7（图2 从 v1 到 v2 的修正过程与失败教训） |

#### 本项目侧的提交纪律

- 拿到词后，**先把完整中英文正文、负面词、参考职责与参数贴给用户审批**；未批准不提交。
- 批准后**只提交一次**；失败或超时不自行重试，不改词或改参数重发。
- 提交前先确认两张图均为**同一色值的绿幕副本并实测背景**；不得用灰底图。
- 生成后先下载并完整解码，再把视频交用户目检；**选帧、抠图、对齐、接入分别验收**，不在本次一气做完。

### 0.9 【反馈记录】持盾死亡视频 v1 两轮验收不通过；已拍板方向 A（重做图2）（2026-10-16）

> **本节用途**：记录用户对视频 v1 的否决反馈与代理客观测量，并记录用户已拍板的方向 A（重做图2）。本节**不是**重发授权；图2 重做版的生图词由其他侧写，任何提交都需用户先审阅并批准完整中英文词与参数。

#### 用户反馈（原话要点，验收依据）

**第一轮（结构层）**

| # | 用户指出的问题 | 含义 |
|---|---|---|
| 1 | **结尾姿势不符合预期** | 末帧没有到达已验收图2 的终态 |
| 2 | **总体来说几乎没有什么动作** | 动作量太小、受击感不足 |

**第二轮（语义层，2026-10-16，本轮新增，优先级更高）**

| # | 用户原话要点 | 含义 |
|---|---|---|
| 3 | **动作毫无张力，没有被击飞到底的感觉** | 缺少“被力量抛出去”的暴力读感，没有力道 |
| 4 | **与1011 的 dead 动画语义完全不符** | 要的是 1011 那种被重击打得四肢张开、身体炸开的死亡语义 |
| 5 | **姿态僵硬，像是被催眠倒下** | 像自己缓慢软倒，不是被外力打死 |

> **以第二轮为准**：第一轮的“末帧不符图2 / 动作量小”是表层现象，第二轮指出的是**整个动作的语义错了**（软倒 vs 被打飞）。后续修正不能只在旧框架上补动作量，而要重立语义。

#### 代理客观测量：成片 vs 1011 已验收死亡（同口径）

用**填充率 = 主体像素 ÷ 包围盒面积**衡量“身体有没有被打开”：数值越低 = 四肢张得越开、越“散”。即“张力指标” = `1 − 填充率`。

| 素材 | 包围盒 w/h | 主轴 | 填充率 | 张力(1−fill) |
|---|---|---|---|---|
| 1011 Idle（站立） | 0.921 | — | 0.465 | 0.535 |
| **1011 dead3（已验收）** | **1.548** | 79.3° | **0.301** | **0.699** |
| 1011 dead1 | 1.060 | 41.8° | 0.403 | 0.597 |
| 1011 dead6 | 1.438 | 70.5° | 0.297 | 0.703 |
| **1021 成片首帧** | 0.746 | 1.8° | 0.620 | 0.380 |
| **1021 成片末帧 f097** | 1.085 | 86.2° | **0.670** | **0.330** |

判读：

- **1011 的死亡是“炸开”**：宽度扩到 Idle 的 **1.45–1.52 倍**，填充率从 0.465 掉到 **0.30**（张力 0.70）——四肢被甩开、身体展开、剑脱手。
- **1021 成片只是“转了个身”**：末帧填充率反而从首帧 0.620 **升到 0.670**（张力 0.33，连站立 Idle 都不如），宽度只从 412 变到 484。
- 即：成片把“受力”实现成了**原地旋转**，完全没有把身体打开。这与用户“僵硬像被催眠倒下”“毫无张力”的描述一致。

另：本次成片的末态姿势（f031 起）被模型拉成了接近 90° 的横躺（见 (a) 表），而镜头/躯干约束又要求不旋转，两个相反的限制叠加，最终得到“转了一半、四肢又没张开”的僵硬解。

#### 【根因与用户决策】目标图2 已被“约束成僵硬” → 已拍板方向 A：重做图2

- 图2 v1 因“整身旋转 45°、尺寸放大 1.33×”被否决，于是图2 v2 被要求 **躯干偏离竖直 ≤15°、包围盒变化 ≤10%**。
- 图2 v2 据此通过验收：主轴 28.4°、宽高比 0.841（竖高于宽）、填充率（对比 1011 dead3 的 0.301）明显更高——**它是一张“垂头失重但身形基本站桩”的图**。
- 视频词又把这个“不旋转、不放大”的双重限制继承下来，并叠加“末帧必须精确到达图2”。两者相加，模型只能做到“原地小幅转身”，无法表现 1011 的“四肢张开”。
- 这也是 2026-10-16 早些时候“防旋转”修正的反作用：当时为了治 v1 的“转向画面左侧”，把旋转封死了，代价是把死亡读感也封掉了。
- **【用户已拍板】2026-10-16，用户原话：“按 A 方向，不用你写提示词，更新交接文档即可。”** 即：**重做图2**，接受**更大旋转与更宽展开**，对齐 1011 的死亡语义；图2 重做版的生图词由**其他侧**写，不由本侧起草。

#### 【方向 A 的执行计划】（图2 重做，其他侧出词）

1. **其他侧先产出图2 重做版的完整生图词**（中英双语正文 + 负面词 + 参考职责 + 参数），目标量级参考 1011 已验收死亡帧：宽高比 **>1.4**、填充率 **≈0.30**、四肢张开、头甩向一侧。
2. **本侧只做两件事**：将收到的词写入本交接；经用户批准后**提交一次** `images/edits`。
3. **不再把“不旋转/不放大”写成硬约束**：这两条正是导致 v2 僵硬的来源；新词应允许明显旋转与更宽展开，仅保留“不越框、不重设计身份、盾不脱手”等必要红线。
4. **新图2 验收通过后**才重新起草死亡视频词（以新图2 为末帧锚点），旧视频词 §0.8 作废。
5. **新图2 的底色必须同样按 G1 处理**：生成后实测背景（本次 v2 生成底约为 `(2,178,68)`，与图1 的 `(0,177,64)` 不同值），上传前另存同色绿幕副本。

#### 【历史】第一轮曾提出的修正（已被方向 A 取代，仅存档）

原“四处必修”与第一轮“三处修正”都建立在“保留图2 v2、只调视频词”的框架上。实测已证明该框架无法产生 1011 的死亡语义（填充率反而比站立 Idle 还紧），因此以下不再作为行动依据：

1. ~~先定要不要重做图2~~ → **已定：重做**。
2. 把“张力/展开”写成可判读指标：末帧宽度不少于图1 的 1.3–1.5 倍、填充率 ≤0.35（1011 dead3 = 0.301）——**该指标仍适用，但目标是写进图2 的新生图词，而不是旧框架下的视频词**。
3. 把“被动受力”与“主动软倒”彻底分开（`no slow collapse`、`no hypnotic fall`、`no gentle sag`）——**同样归入图2 新词的语义词**。
4. 末帧锁图2 与“求张开”的矛盾：**重做图2 后自然解除**（新图2 本身就是张开终态）。

第一轮的三条（末帧竖向失重/提高受击量/避免先不动后旋转）均已作废：它们追求的是“竖向、不旋转”，与方向 A 相反。

#### 【历史】第一轮提出的三处修正（已被方向 A 取代，仅存档）

1. **末帧锁定到图2 的“竖向失重”，不写成旋转/躺倒**：实测末帧主轴 86.2° 而图2 是 28.4°。下一版必须在正文里把末态写成“**身体仍保持竖向、双肩下沉、头垂向一侧；严禁把身体旋转到水平或躺倒**”，并在负面词里补 `no lying flat`、`no horizontal body`、`no body rotated to 90 degrees`、`no falling sideways`。同时把图2 的宽高比（约 0.84，竖高于宽）作为可判读指标写进去。
2. **把受击动作量提上去，并把它写进前段**：实测受击窗口平均变化仅 1.33，占全片比重过低。下一版应显式写明受击是“**可见的、全身参与的一下**”（双肩明显下沉、头颈被压低、双臂被震开、盾被撞偏、双膝下沉），并给出“受击幅度在画面上要能被单帧看出”这种可判读要求；同时考虑把受击窗口从 0.20–0.45s 适度延长（或把首帧保持缩短），避免前段几乎静止。
3. **避免“先不动、后旋转”的平均解**：实测最大变化跑到 t=1.42s、累计 50% 在 t=1.29s，模型把动作集中到了中后段的整体旋转（即第 1 项的失败）。下一版要把“旋转”从可实现路径里拿掉（见第 1 项负面词），让变化只能落在关节与姿态上，自然落到前段。

#### 代理客观测量（与用户反馈互相印证）

**（a）末态不符已验收图2**（同口径，图2 折算到 960 画布）

| 指标 | 图2 v2（已验收目标） | 成片末帧 f097 | 判定 |
|---|---|---|---|
| 主体包围盒 | 454×540（→960 折算 426×506） | 484×446 | 宽比 1.137、高比 0.881 |
| 宽高比 w/h | **0.841**（竖高于宽） | **1.085**（宽高于竖） | 逆约束：比目标图更摊平、更横躺 |
| 主体主轴偏移 | 28.4° | **86.2°** | 几乎整身旋转到水平 |
| 边缘余量 | 193 px | 未超框 | 达标 |

判读：图2 是“头垂向一侧但身体基本竖着”的失重态；成片末帧把身体旋转到了接近 90°，读成躺倒/斜卧，不是已验收图2。

**（b）动作量太小**（逐帧帧间平均绝对差，0 = 完全静止）

| 时间段 | 帧间变化均值 | 最大 |
|---|---|---|
| 0.00–0.55s（词里写的“受击”窗口） | 1.33 | 4.98（f003） |
| 0.55s 之后 | 0.95 | 7.17（f035, t=1.42s） |
| 1.60s 之后（保持段） | ≈0.4–0.5 | — |

- **全片最大变化出现在 t=1.42s，不在受击窗口**；累计变化到 50% 的时刻是 **t=1.29s**。即真正的变化集中在中后段的身体旋转，而词里写在前 0.45s 的“受击”几乎没有量。
- 受击窗口内 13 帧平均仅 1.33，其中多帧接近静止（f008 0.28、f011–f013 0.29–0.38）；**看不出“双肩下沉、颈缩短、膝弯曲”那一下**。
- 末段 1.6s–4.0s 基本静止（≈0.4），与“保持末态”一致，但也因此全片显得没有内容。

结论：**背景与尺寸均达标，失败在“末帧姿态不符已验收图2”与“受击动作量不足”**。

#### 下一版必须修的三处（修完需用户批准才能发）【历史：见上方“四处”】

1. **末帧锁定到图2 的“竖向失重”，不写成旋转/躺倒**：实测末帧主轴 86.2° 而图2 是 28.4°。下一版必须在正文里把末态写成“**身体仍保持竖向、双肩下沉、头垂向一侧；严禁把身体旋转到水平或躺倒**”，并在负面词里补 `no lying flat`、`no horizontal body`、`no body rotated to 90 degrees`、`no falling sideways`。同时把图2 的宽高比（约 0.84，竖高于宽）作为可判读指标写进去。
2. **把受击动作量提上去，并把它写进前段**：实测受击窗口平均变化仅 1.33，占全片比重过低。下一版应显式写明受击是“**可见的、全身参与的一下**”（双肩明显下沉、头颈被压低、双臂被震开、盾被撞偏、双膝下沉），并给出“受击幅度在画面上要能被单帧看出”这种可判读要求；同时考虑把受击窗口从 0.20–0.45s 适度延长（或把首帧保持缩短），避免前段几乎静止。
3. **避免“先不动、后旋转”的平均解**：实测最大变化跑到 t=1.42s、累计 50% 在 t=1.29s，模型把动作集中到了中后段的整体旋转（即第 1 项的失败）。下一版要把“旋转”从可实现路径里拿掉（见第 1 项负面词），让变化只能落在关节与姿态上，自然落到前段。

#### 对方向 A 仍适用的约束

- **保留**：一次死亡；不重设计身份；四向不超框；无地面、阴影、场景和特效；不出现第二人、文字和 UI。
- **不再保留“盾不脱手”**：用户于 2026-10-16 明确纠正，死亡动作没有禁止盾脱手。该限制是代理自行加入的，已作废。新方案见 §0.10。
- **不再作为硬约束**：“躯干偏离竖直 ≤15°”“包围盒变化 ≤10%”——这两条正是 v2 僵硬的来源，方向 A 已明确接受更大旋转与更宽展开。
- **G1**：新图2 生成后同样要实测背景并另存同色绿幕上传副本（本次 v2 底为约 `(2,178,68)`，与图1 的 `(0,177,64)` 不同值），不得直接上传灰底或异色底。
- **图2 重做版通过新验收后**，旧的死亡视频词 §0.8 作废，视频词需以新图2 为末帧锚点重新起草并重新审批。

#### 项目内对照材料（仅本地查看，**不得上传**）

| 用途 | 路径 |
|---|---|
| 成片（失败样本） | `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v1.mp4` |
| 接触表（12 帧时间线） | `Library/Locus/tmp/enemy1021_dead_video_v1/DEAD_V1_CONTACT_SHEET.png` |
| 动作分镜条（6 帧） | `Library/Locus/tmp/enemy1021_dead_video_v1/DEAD_V1_MOTION_STRIP.png` |
| **末态 vs 已验收图2 对照** | `Library/Locus/tmp/enemy1021_dead_video_v1/END_POSE_VS_APPROVED_IMAGE2.png` |
| G1 底色归一对照 | `Library/Locus/tmp/enemy1021_dead_video_v1/G1_PLATE_UNIFICATION_REVIEW.png` |
| 逐帧姿态指标 | `Library/Locus/tmp/enemy1021_dead_video_v1/frame_metrics.json` |
| **成片 vs 1011 死亡语义对照** | `Library/Locus/tmp/enemy1021_dead_video_v1/SEMANTIC_VS_1011_DEAD.png` |
| 1011 死亡帧实测指标 | `Library/Locus/tmp/enemy1021_dead_video_v1/ref_1011_dead_metrics.json` |
| 成片逐帧姿态指标 | `Library/Locus/tmp/enemy1021_dead_video_v1/video_pose_metrics.json` |
| 解码帧 | `Library/Locus/tmp/enemy1021_dead_video_v1/frames/clean_raw/` |

#### 纪律

- 本次失败后**不自行重发**；服务端 `cost=0.48888` 已计费，不得自动重新生成。
- **图2 重做词已写入 §0.10，但写入不等于批准。** 用户批准后才提交一次。
- **新图2 未通过前：不重写视频词、不提交视频。**
- 在图2 未变的前提下，图1 与图2 可继续作为两张普通 `reference_image`；图2 重做后需重新确定两张参考图。

---

### 0.10 持盾死亡图2重做 · 生成方案（2026-10-16；已按本节提交一次，产物 v3 已获用户验收）

> 本节取代 §0.6 和 §0.7 的提交词。v1、v2 都不再提交。本节词已于 2026-10-16 提交一次，结果与量测见顶部最新停点；**不要重跑本次提交**。

#### 这张图要解决什么

视频 v1 被否决，是因为目标图2 v2 把人收成垂头站桩，视频只能原地转身。1011 已验收死亡是四肢甩开、武器脱手、身体变宽。1021 这次死亡按同一语义做。

用户已明确：死亡动作没有禁止盾脱手。画面左侧那只手松开，木盾离开手，完整留在画面里。破盾动画的独立掉盾是另一套状态，不拿来限制这张图。

#### 参考与参数

| 项 | 值 |
|---|---|
| 模型 | `gpt-image-2.5-sunburst` |
| 接口 | `POST https://api.muskapis.com/v1/images/edits` |
| quality / input_fidelity | max / high |
| size / background / format / n | 1024x1024 / opaque / png / 1 |
| 上传 | 只上传图1：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` |
| 不上传 | 图2 v1、图2 v2、死亡视频帧、1011 死亡帧 |
| 建议输出 | `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v3.png` |

1011 死亡帧只在本地对照，用来看张开幅度，不进请求。

#### 提交用英文词

```text
Edit the provided shield soldier into one still 2D game sprite: the final frame of a violent death, not a video. Keep his identity: the same helmet, red plume, face opening, orange eyes, red scarf, cold-grey armour, warm-brown boots, thick dark outline and hard block shading. Do not redesign him and do not turn him into a sword soldier.

One invisible blow from straight ahead has already killed him and flung his body open in the picture plane. The attacker stays outside the frame. This is the same kind of death as a soldier knocked off his feet: limbs spread, hands open, equipment leaving the hand. It is not a slow collapse and not an ascension.

BODY: rotate the whole figure about 50 to 80 degrees from vertical, inside the picture plane. Keep the chest readable from the front. Do not redraw him as a side-view character. The head slumps toward the upper side of the tilt. Both arms are thrown apart and both hands are open. Both legs are spread, not hanging together.

SHIELD: the wooden shield is knocked out of the screen-left hand. A clear gap of background separates that open hand from the shield. The shield stays whole, at the same drawing size as the source, and remains fully inside the frame. It is not broken, split, duplicated or turned into a sword.

SPREAD, NOT SCALE: the open pose makes the full-body bounding box wider than it is tall, with a width-to-height ratio above 1.4. The spread comes from separated limbs and the loose shield, not from enlarging parts. Helmet, chest, shield and boots keep the same drawing size as the source.

PLACE: centre the whole group. Keep at least 10 percent empty margin on every side. If the pose would touch an edge, tighten the spacing instead of cropping. He is airborne: no floor, no ground contact and no ground shadow.

Background: flat chroma green RGB(0,177,64), no gradient, scenery or props. No blood, wound, sparks, smoke, dust, flash, speed lines or motion blur. No second character, no text and no UI.
```

中文对照（不提交）：

> 把提供的盾兵改成一张暴力死亡终态静帧，不是视频。头盔、红缨、面罩、橙眼、红围巾、冷灰甲、暖棕靴、粗描边和硬块阴影保持原样。不得重设计，不得改成剑兵。
>
> 画面正前方的一记隐形重击已经把他打死，并把身体在画面平面内甩开。攻击者不入画。这是和剑兵死亡同一类的结果：四肢张开、手张开、装备离开手。不是缓慢软倒，也不是飞升。
>
> 整个人在画面平面内偏离竖直约 50°–80°。胸甲仍从正面可读，不要重画成侧面角色。头垂向倾斜后的上侧。双臂甩开，双手张开。双腿分开，不要并拢下垂。
>
> 木盾从画面左侧那只手脱手。张开的手和盾之间要有一块清楚的背景空隙。盾保持完整，绘制尺寸与原图一致，并且完整留在画面里。不裂、不分成两块、不出现第二面盾、不变成剑。
>
> 张开后的全身包围盒宽大于高，宽高比大于 1.4。宽度来自分开的四肢和脱手的盾，不来自放大部件。头盔、胸甲、盾和靴的绘制尺寸与原图一致。
>
> 整组画面居中，四边至少留 10% 空边。会贴边就收紧间距，不许裁切。人在空中，没有地面、没有接地、没有地面阴影。背景是纯色绿幕 RGB(0,177,64)。无血迹、特效、第二个人、文字和 UI。

负面词（与英文词一起提交）：

```text
no shield still held, no hand hidden behind the shield, no gripping hand, no slow collapse, no hypnotic fall, no gentle sag, no braced standing pose, no ascension, no rising pose, no upward launch, no triumphant pose, no lying on the ground, no floor, no ground shadow, no side-view redesign, no profile character, no enlarged shield, no enlarged body parts, no stretched limbs, no broken shield, no split shield, no second shield, no sword, no cropped shield, no shield leaving the frame, no cropped limb, no body leaving the frame, no attacker, no second character, no blood, no wound, no sparks, no smoke, no dust, no flash, no speed lines, no motion blur, no scenery, no text, no UI
```

#### 生成后先量这四项

| 项 | 通过标准 |
|---|---|
| 全身宽高比 | 大于 1.4 |
| 填充率 | 约 0.30–0.35 |
| 脱手 | 画面左侧的手张开，手和盾之间有背景空隙 |
| 部件尺寸 | 盾没有相对图1 明显放大 |

只看包围盒不够。盾被放大也会让包围盒变宽，这种情况判失败。

#### 提交边界

只提交一次。失败、超时或结果不佳都不自行重试，也不改词、不降参数重发。生成后先交用户看单图。通过前不写视频词、不提交视频。通过后，视频才改用图1和这张新图2作为两张普通参考图，并另行审批视频词。

#### 本次执行结果（2026-10-16，已提交并经用户验收）

| 项 | 值 |
|---|---|
| 产物 | `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v3.png` |
| 大小 / SHA-256 | 1,122,630 bytes / `dba1290c…9785` |
| HTTP / POST 耗时 | 200 / 108.071 秒 |
| usage | prompt/completion/total = 667/946/1613 |
| cost / task id | 未返回（不推断费用） |
| 实际提交 prompt SHA-256 | `eb06952c…f140`（正文 `721e7814…587c` ＋ 负面词 `e55a48fe…92ca` 合并） |
| 四角背景中位 | 约 `(1.6, 181.1, 72.4)`，非图1 的精确 `(0,177,64)` |

四项通过标准的实测、与 1011 的同口径对照，以及代理量测出的三项达标边沿，见顶部最新停点。**用户已验收本图**；三项边沿均已接受，不重发。视频改用 §0.11。

---

### 0.11 持盾死亡视频 v2 · 已按本节提交一次、下载并获用户验收（2026-10-17）

> §0.8 的旧视频词已作废，不要重跑。**本节词已于 2026-10-17 原样提交一次，成片已下载、完整解码并获用户验收；不要再提交第二次。** 结果与文件见顶部最新停点。

#### 两张实际上传图

| 图 | 上传路径 | 职责 |
|---|---|---|
| 图1 | `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3_green_v1.png` | 开头持盾站姿。SHA-256 `d398a1eb7cd436f8082c6cf7dd7901e59d4f888e757f48b1ac01823f4adf40de`。四角精确 `(0,177,64)` |
| 图2 | `Library/Locus/tmp/enemy1021_dead_video_v2/02_dead_v3_green_00B140_1024.png` | 必须到达的死亡终态。SHA-256 `16bfc41086e8a1f5674c6236f04e941b7f4bacd96072e23e6ac4f29e70a983cf`。四角精确 `(0,177,64)`、std 0 |

两张都用普通 `reference_image`，不使用 `first_frame` / `last_frame`。不要上传 v3 原图 `enemy1021_dead_airborne_keypose_v3.png`，它的底色是 `(2,181,72)`。也不要上传 v1、v2 或 1011 死亡帧。

#### 参数

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 / 分辨率 / 比例 | 4 秒 / 720p / 1:1 |
| 音频 / 水印 / seed | 无 / 无 / -1 |
| 建议输出 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.mp4` |
| 次数 | 只提交一次；失败或超时不自行重试 |

#### 提交用英文词

```text
This is a 2D game sprite animation, not a cinematic. One front-facing shield soldier is killed by a single invisible blow from straight ahead. The attacker stays outside the frame. This is a violent death: the body is flung open and the shield leaves his hand. It is not a slow collapse, not a jump and not an ascension.

Image 1 is the opening pose: the same soldier standing with the wooden shield held on screen-left and both boots planted. Image 2 is the final pose, and the animation must arrive at it exactly: the body is spread in the picture plane, both hands are open, and the wooden shield has left the screen-left hand with a visible gap. Both images are ordinary references. Keep the same flat chroma-green background in every frame.

TIMING, ONE HIT ONLY:
0.00-0.15s: hold image 1.
0.15-0.45s: the blow lands. Both shoulders drop, the screen-left hand opens, and the shield separates from that hand. The empty screen-right hand opens at the same time.
0.45-1.20s: the arms, legs and loose shield spread into image 2. The body tilts in the picture plane but is not redrawn as a side-view character. He is not rising and he is not being lifted.
1.20-4.00s: hold image 2, with only slight drift. No landing, no second hit and no return to the standing pose.

SHIELD: it starts in the screen-left hand and is knocked free during the hit. After separation, a clear background gap stays between the open hand and the shield. The shield remains whole, stays inside the frame, and is not broken, split or duplicated.

SCALE AND FRAME: helmet, chest, shield and boots keep the same drawing size as image 1. The wider final pose comes from separated limbs and the loose shield, not from enlarging parts. Keep the whole group centred, with at least 8 percent empty margin on every side. If a pose would touch an edge, tighten the spacing instead of cropping it.

CAMERA AND BACKGROUND: locked camera, no zoom, pan, tilt or shake. No floor, shadow, scenery or props. No blood, wound, sparks, smoke, dust, flash, speed lines or motion blur. No second character, no text and no UI.
```

中文对照（不提交）：

> 这是一段 2D 游戏精灵动画，不是电影。一个正面盾兵被画面正前方的一记隐形重击打死。攻击者不入画。这是暴力死亡：身体被甩开，盾离开手。不是缓慢软倒，不是跳，也不是飞升。
>
> 图1 是开头：同一个盾兵站着，木盾握在画面左侧，双脚着地。图2 是必须精确到达的终态：身体在画面平面内张开，双手张开，木盾已经离开画面左侧那只手，中间有可见空隙。两张图都只作普通参考。每一帧保持同一平面绿幕。
>
> 节奏只有一次打击：0.00–0.15 秒保持图1；0.15–0.45 秒受击，双肩下沉，画面左侧的手张开，盾离开这只手，画面右侧的空手同时张开；0.45–1.20 秒四肢和脱手的盾展开到图2，身体在画面平面内倾斜，但不重画成侧面角色，不继续上升、不被托起；1.20–4.00 秒保持图2，只允许轻微漂移，不落地、不再挨打、不回到站姿。
>
> 盾开始时在画面左侧手上，受击时脱手。分开后，张开的手和盾之间保持一块清楚的背景空隙。盾保持完整、留在画面内，不裂、不分成两块、不出现第二面盾。
>
> 头盔、胸甲、盾和靴的绘制尺寸与图1 一致。终态变宽来自分开的四肢和脱手的盾，不来自放大部件。整组画面居中，四边至少留 8% 空边。会贴边就收紧间距，不许裁切。镜头锁定。无地面、阴影、场景、血迹、特效、第二个人、文字和 UI。

负面词（并入同一 prompt）：

```text
no slow collapse, no hypnotic fall, no gentle sag, no ascension, no rising pose, no upward launch, no voluntary jump, no second hit, no shield still held at the end, no gripping hand at the end, no broken shield, no split shield, no second shield, no shield leaving the frame, no enlarged character, no enlarged shield, no stretched limbs, no side-view redesign, no landing, no lying on the ground, no return to idle, no attacker, no second character, no blood, no wound, no sparks, no smoke, no dust, no flash, no speed lines, no motion blur, no camera movement, no floor, no shadow, no scenery, no text, no UI, no cropped limb, no body leaving the frame
```

#### 发起后

下载并完整解码，再交用户看视频。选帧、抠图、对齐和接入分别验收，不在这一次里连续做完。

#### 本次执行结果（2026-10-17）

| 项 | 值 |
|---|---|
| Task ID | `task_215c88d7209f46bc8e5b36a528c225e2` |
| accepted UTC | `2026-10-17T03:12:03.170732+00:00`（create HTTP 200 / 2.135 秒） |
| 轮询 | 12 次限时轮询均 generating 后停止本地等待；单次查询于 `2026-10-17T03:16:07.205752+00:00` 返回 `success` |
| usage / cost | completion_tokens 87300；`cost.spend=0.48888`（币种未注明） |
| 成片 | `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.mp4` |
| 大小 / SHA-256 | 2,326,004 bytes / `c2b444a358d246f2ac677a63bfdb6f0c2327e28ca96300836c9d1a2bf47e9095` |
| 实测规格 | 960×960 / 24fps / 97 帧 / 4.041667 秒 / 末帧 PTS 4.0 秒 / H.264 / 无音轨 |
| 校验 | 四段 Range 均 206 且 ETag 一致；MP4 结构 `ftyp/uuid/free/mdat/moov`；全帧解码通过 |
| 提交 prompt | 正文 `f1d9ff62…0d4f` + 负面词 `19bd7829…f9ce` 合并为 `086e7c9c…12a9` |
| 报告 | `enemy1021_dead_v2.download.json`、`enemy1021_dead_v2.request.json`、`Library/Locus/tmp/enemy1021_dead_video_v2/runtime.json` |
| 检查帧 | `contact_sheet.png`（0/24/48/72/96）、`first_frame.png`、`last_frame.png`，仅文件诊断，不是选帧 |

未创建第二个任务、未重试。选帧、抠图、对齐、接入均未开始。

---

## 1. 制作目标与产出流程（当前完成状态优先看 §0）

按优先级交付，每步都需用户目检验收后才进入下一步：

| 阶段 | 交付物 | 说明 |
|---|---|---|
| ① 形象确定 | **1021 的 Idle 定妆图（单帧）** | **已验收**：`enemy1021_shield_hold_v3.png`；作为后续动作身份锚点 |
| ② Idle 动画 | 已部署4帧透明PNG与Idle Clip | 0.96秒循环；沿用现有素材，不按早期6帧建议重复制作 |
| ③ 其他动作 | 按 `design/enemy-1021-animation.md` §2/§3.2 的清单分批 | **Walk/Attack/ShieldGuardHit/HitFlash已完成并获游戏验收**；HitRight有未部署透明候选，ShieldBreak、Dead、击飞Rise/Fall与胆怯家族仍未完成；起身另列为待决定项 |
| ④ 可选 | 眩晕、起身（`Launched_Getup`/`CowardGetup`） | 主文档 §10 待拍板项 |

**不在本次范围**：机制/数值改动、控制器接线逻辑改动（状态名是契约，换 clip 不需要改代码）、102 的任何资产。

---

## 2. 开工前必读（按顺序）

| 文档 | 作用 |
|---|---|
| `design/enemy-1021-shield-guard.md` | **机制权威**：方向分类、固定减伤、破盾与胆怯、形态状态机 |
| `design/enemy-1021-animation.md` | **动画需求**：逐状态 × 动画矩阵、需新做的清单与规格、Animator 待补状态、时间对齐规则、盾的表现方案、验收清单 |
| `design/pixel-character-and-enemy-animation-spec.md` | 硬性规范：正面像素角色、完整在画布内、脚底锚点、动作语义（**注意 §4 的盾位与本敌人相反，见 §6.1**） |
| `design/art-style-guide.md` | **画风基准**：两张概念图 + 已验收 1011 六帧 Idle（§2.2、§3.1 体型与剪影、§3.2 材质） |
| `skill/workflows/character-hit-animation-video-workflow.md` | 视频生产 SOP（提示词 5 层结构、参考图分工、防超框、验收、部署） |
| `skill/workflows/image-asset-generation.md` | 图像生成/编辑/去背 SOP，含**尺寸与密度约定**与两级验收标准 |
| `skill/gpt-image-generation.md` | GPT Image 接口能力与**路由指纹**（决定能否拿到原生透明） |
| `skill/reachapi-seedance-video-generation-v2.md` | Seedance 视频接口与下载方法 |
| `skill/workflows/local-green-screen-cutout.md` | 原生透明不可用时的本地绿幕抠图兜底 SOP |
| `memory/hit-animation-video-pipeline-lessons.md` | **9 条实测教训**（提示词 5 层、正面视角三条铁律、收费纪律等） |
| `memory/video-generation-case-log.md` | 任务台账：每次的参数/费用/文件/结论（追加新任务用） |
| `memory/enemy1011-animation-prompt-archive.md`、`memory/enemy1011-animation-pose-reference.md` | 本机可直接读取的 1011 实际视频原词与姿态经验；Idle 参考主读档案 §1，改掉持剑/无盾等不适用条款，不直接整段照搬 |
| `plan/enemy1011-animation-deployment-handoff.md` | 已验收的**接入约定与基线**（画布、导入设置、锚点、命名、脚底/血条数值） |

用户流程纪律（来自教训文档 §9，必须遵守）：关键姿态参考图先给用户目检；提交前把提示词的**中文对照版**贴进对话；收费任务**不自行重试**；交付一律贴**完整路径**。

---

## 3. 形象确定的历史记录（当前部署状态以 §0 为准）

### 3.1 必须锁定的设计点

1. **盾在画面左侧**（敌人**右手**持盾）——这是机制的前提（`Left` = 力来自画面左 = 盾侧）。**注意这与 102 的占位美术相反**（102 的盾在画面右侧、即角色左手），详见 §6.1。
2. **一眼能与 102 区分**：1021 是正式盾兵、102 是实验品，二者会在同一波次并存。当前以已验收 v3 的盾位、木盾、1011 人物基底与持法区分；不再为区分兵种另换头盔、加重护甲或添加盾面纹章。
3. **用户已确认盾的制作方案**：持盾时盾画在角色本体帧内；ShieldBreak 动画表现角色脱手，之后由引擎驱动独立盾牌掉落。不制作 Animator 驱动的分离持盾层；Idle 保持已经举盾应战的姿势，不制作举起/收回盾的状态切换。
4. **人物基底已定**：直接以 1011 为基底，将持剑改为持盾；共享体型、头盔和甲胄设计，保留大头盔、宽肩、紧凑躯干、短腿及冷铁灰、朱红、暖金的绘制语言，不另行设计更重或更轻的装备。
5. **已验收 Idle 语义**：双脚站稳、正面应战；盾位于胸腹前方偏画面左侧，持盾手、手指、前臂和握把藏在盾后，头盔、面罩和眼光保持可读；另一只手（角色左手、画面右侧）保持原图**空垂**。允许轻微呼吸/缨带随动，不迈步、攻击、切换盾位或起尘。具体外观与盾形以已验收 v3 成图为准。

### 3.2 已验收定妆图与实际生成来源

- **已验收文件**：`C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`；1024×1024、RGB PNG、983,338 bytes、浅灰背景。该单帧形象已验收；后续去背、对齐与Unity部署已在§0阶段完成，本节仅保留定妆当时来源。
- **人物主参考**：`Assets/Sprites/Enemy/Enemy1_1/Enemy_1011_idle1.png`，负责同一角色身份、头身关系、装备、镜头角度、受光和像素体块；不是泛化的风格参考。实际上传副本合成到 `#E6E6E6`，保留原 1108×1108 画布，未去剑或缩放。
- **盾牌局部参考**：用户提供的 `C:/Users/steam/Downloads/微信图片_20260928101134_12_223.png`，裁剪区域 `(685,575,1024,1120)`（左、上、右、下），339×545；仅借用木板、铁件、铆钉的构造与材质，不继承红黑纹章、人物、握持姿态或场景。局部副本保留原图案，由提示词明确排除。
- **实际上传副本**：`Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/01_1011_idle1_lightgray.png` 与 `Library/Locus/tmp/enemy1021_shield_hold_v3_inputs/02_shield_material_detail.png`；临时目录可能被清理，来源与处理方式同时保存在请求 JSON。
- **生成记录**：`POST /v1/images/edits`、`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、`size=1024x1024`、PNG、`n=1`；只提交一次获批英文提示词，HTTP 200，无自动重试。完整原词和参数在同名 `.request.json`，原始响应在 `.response.json`；用量与哈希见 `memory/video-generation-case-log.md`。
- **当前外观锚点**：后续动作沿用 v3 的体型、头盔、甲胄、红缨/围巾、木盾和举盾遮挡关系；副手保持空垂。具体盾高、底部收窄和材质以用户验收成图为准，不为追求早期比例数字自动修图或再生成。
- **早期方案已被取代**：1011+109 的两图参考和 `enemy1021_idle_keypose_v1/v2.png`、`enemy1021_shield_hold_v1/v2.png` 均属于探索版本，未成为身份锚点。后续不继续上传 109 或失败候选来定义 1021。
- **验收边界**：本次仅确认单帧形象；Idle 视频、循环连贯性、透明帧、锚点对齐、导入参数与运行时表现分别验收。不得将 HTTP 200、PNG 可读或单帧通过写成动画/部署完成。

### 3.3 定妆图验收要点（同时满足才算过）

- 正面朝向、无侧身/3/4 视角；盾在**画面左侧**且持握关系清楚。
- 与 1011 同级的厚重度与像素密度；材质按 `art-style-guide.md` §3.2（金属冷灰蓝灰 + 锐利反光、红布大色块、木盾有板缝与铁件）。
- 缩小到游戏尺寸仍能读出"盾兵"；剪影不被细节塞满。
- 装备、配色、比例在后续动作里必须保持（动作参考图只改姿态，不重设计角色）。

---

## 4. Idle 视频与早期规格记录（实际已部署4帧，见 §0）

### 4.1 视频

- 参考/锚点：已验收v3原图仍是身份与姿态基准；首尾使用获批的独立绿幕副本，未覆盖原图。五层结构双语词已批准并提交，v1 task现为success，视频已下载并完整解码；成片待用户验收，不重新创建收费任务。
- **本次实际规格**：获批请求 `seedance-2-fast` / 4s / 720p / 1:1 / 无音频无水印 / seed=-1；1021 v1实测960×960、24fps、97帧、容器时长4.041667秒，末帧PTS=4.000秒，音频流0；API返回cost.spend=0.48888。这些有本次响应/解码证据，不是沿用1011历史值。
- **背景已确认**：已验收 v3 浅灰原图保留；本地另存 `#00B140` 绿幕副本作为 v1 的首尾参考，不涉及收费生图。背景纯度、画布和保留像素已检查，细浅灰边限制已告知用户；视频绿幕稳定性仍需本次成片检验。

### 4.2 取帧与抠图

- 取帧：先读取本次视频的实际尺寸、帧率与时长，再用 `ffmpeg` 解出 `clean_raw/f000.png…`；保留源视频的实际画布和背景，不标注、不裁切、不缩放，不预设输出一定是 960×960 或绿底。
- 选 6 帧：先确认视频中的完整、稳定呼吸周期，检查脚底、盾位、尺寸和首尾接缝；周期内可均匀取 6 个代表姿态，不能对整段视频机械等距取帧后假定无缝。1011 的 6 键/每键 0.1s/循环 0.6s 仅为接入参考，1021 最终节奏需按实际选帧和用户验收确定。
- 抠图：先发 **1 次探针**（`background=transparent` + `output_format=png`），用**路由指纹**判断能否拿到原生 alpha（G 类 = 原生 RGBA；B 类 = RGB 无 alpha，立即停手）；不可用则转本地绿幕键（用户已验收 A2 组合：反混合去污染 + 2px 环颜色替换）。
- 对齐：保留源视频帧的原始几何关系，禁止按各帧包围框或高度分别归一化。若 GPT 去背引入缩放/位移漂移，先测量与对应源帧的差异，再按 SOP 采用**单一全局缩放 + 逐帧脚底锚点对齐**并检查残差；不能用逐帧伸缩抹掉呼吸/关节变化或掩盖源视频漂移。

### 4.3 补边与导入（沿用 1011 已验收约定）

| 项 | 值 |
|---|---|
| 画布 | 补边到 **1108×1108**（角色保持原生像素、零重采样） |
| 脚底 | 自底 **210px**；脚心 x ≈ **549** |
| 补边方式 | **源画布居中补边**（不要"脚心锚到画布中心"，1011 因此整幅画偏移约 0.74 世界单位、看起来差一列） |
| 导入 | PPU **16**、Sprite Single、Center pivot **(554,554)**、Filter **Point**、Compression **Uncompressed**、Alpha Is Transparency、Mipmaps off、Mesh Tight |
| 命名/路径 | `Assets/Sprites/Enemy/Enemy1021/Enemy_1021_idle1..6.png` |
| Clip | `Assets/Animations/Enemy_1021_Idle.anim`（6 键 @0.1s、30fps、Loop 为 1011 基线，1021 键间隔与状态 speed 待验收确定）；**替换 controller 里 `Idle` 状态的 motion，不改代码** |

**Prefab 数值必须同步**（当前 1021 预制体是 102 的拷贝：scale 0.2、血条 yOffset 0、碰撞盒 local 2.787×7.149）：

- 若角色画到与 1011 同级高度（约 606px / 1108 画布）：`scale ≈ 3.15×16÷身高px ≈ 0.0832`；碰撞盒与血条可直接沿用 1011 的验收值（碰撞盒世界 0.502×1.287×0.036、中心 y 0.067；血条 `yOffset = 35.28`、世界高 2.934）。公式与细节见 `plan/enemy1011-animation-deployment-handoff.md` §7 与 `skill/workflows/image-asset-generation.md` 的尺寸密度约定。
- 这些数值**必须在新素材到位后实测复核**，不能照抄；改分辨率而不改 scale/碰撞盒/血条会出现"看起来变了一个兵"的错误。

---

## 5. 第三步：后续动作分批

每个动作按用户确认的顺序推进：**写五层中英文提示词 → 用户批准生成 → 视频交用户验收 → 选有效帧交用户验收 → 正常SVL抠图 → 尺寸/锚点对齐交用户验收 → 按明确请求补边并接入第一关 → 通知用户自行开游戏验收**。若另需生成关键姿态参考图，先单独交用户验收。未明确要求时不运行自检。

| 批次 | 动作 | 提示词/流程要点 |
|---|---|---|
| A | `Walk`、`Attack`（已验收）、`HitFlash`（已验收）、`HitRight`（已接入，待游戏验收） | Walk 要"向玩家前进"、**手臂摆动是主读点**（不能锁死手臂，1011 Walk v1 因此被否决）；Attack 按"起手→蓄力→命中→收势"，命中帧放中部；HitRight 是从画面右被打（全额侧） |
| A+ | **`ShieldGuardHit`（游戏验收通过）**、**`ShieldBreak`（待制作）** | 格挡沿用当前已验收六帧；掉盾见下方专项 |
| B | `Launched_Rise` / `Launched_Fall` / `Launched_Getup` | 击飞语义见规范 §7.KnockUp；Rise 非循环、Fall 循环、Getup 非循环 |
| C | 胆怯家族 `CowardIdle/Hit/Dead/Launched` | 可先沿用 102 的现有帧（已能读），有余力再重做；注意现有 `CowardLaunched` 用的是死亡帧，读感差 |
| D | 可选：眩晕、起身 | 非 Boss 目前全无眩晕表现，属通用缺口 |

### 5.1 `ShieldGuardHit`（格挡）专项

- **当前状态：六帧正式动作已接入并获用户游戏验收，不再重新设计或生成。** 角色右手持盾（画面左侧），接住左侧来力，盾向画面右侧小幅压入，上身轻微右倾、缩颈耸肩后回弹，双脚与根位置固定、无后仰；动作素材无特效。该动作同时服务盾侧与正面；不得用早期“禁止任何侧倾”描述推翻已验收姿态。
- 卡肉会禁用 Animator（格挡走 Light = 45ms），**首帧必须已经是"盾面接触"**，不能是渐变起手。
- 已部署时长0.30s，六帧、30fps非循环，末帧恢复站姿，出边exit time 1 → Idle；路径与键位见§0。
- 不需要左右两个变体（盾侧受击已并入该动作，理由见 `design/enemy-1021-animation.md` §7）。

### 5.2 `ShieldBreak`（站立掉盾）专项

- **长度必须等于 `breakAnimationDuration`（当前 0.46s）**；首帧要能从 Idle / 受击姿态接上，**末帧必须是"无盾踉跄/空手"站姿**（播完代码立刻切 `CowardIdle`）。
- 表现内容：盾脱手/被打飞 + 本体踉跄，**不要**用"被击飞"的动作语言（102 的 `CowardTransition` 就是被击飞语言，仅作占位）。
- 期间普通受击不播受击动画（代码抑制），所以这段**不需要受击变体帧**；但死亡/击飞会被 AnyState 切走，关键姿态不要放尾部。
- 用户已选择“帧内绘制盾 + 引擎驱动独立掉落”方案：视频中盾随本体动作直至脱手，脱手后正式本体帧不继续绘制飞落盾；掉落由引擎接管。本体帧与独立盾对象必须在脱手帧衔接，避免双盾、提前消失或重新出现。具体脱手帧和代码接管时机留待 ShieldBreak 制作/部署时确认，尚未实现。

---

## 6. 已知风险与坑（都踩过，逐条照做可避免）

### 6.1 盾位：与 102 相反（最容易做错的一条）

- 机制要求 **盾在画面左侧（敌人右手）**；而 `pixel-character-and-enemy-animation-spec.md` §4「Enemy2 盾兵基准」描述的 102 是"盾位于角色左侧，即**画面右侧**"，102 的掉盾 clip 里盾的位移也是朝 +x。
- 结论：**规范 §4 描述的是 102 的旧盾位，不能照抄**；1021 要刻意镜像到画面左侧。新对话若直接用 102 的盾帧只会得到反向的盾。
- 连带：盾会向画面左侧伸出，**横向占位可能超过列距 1.0 世界单位**（1011 的剑就有这个问题），这是美术固有事实；要么接受跨列读感，要么在美术上收窄盾的横向占位。

### 6.2 生成与抠图

- GPT Image 的 `background=transparent` **随后端路由变化**：同参数提交可能拿到 RGB + 画入的底 → 必须"先探针、按指纹判、不可用立刻停手"。
- GPT 去背可能逐帧改变角色缩放与位置，须与对应源帧比较；Idle 和受击均不按每帧高度分别归一化。采用单一全局缩放与逐帧脚底对齐后仍需量化残差，保留原始动作几何，不以处理后稳定代替源视频验收。
- 绿幕视频的本地抠图：边软/压缩幽灵会让包围盒虚胖；按 `local-green-screen-cutout.md` 的 SOP 走，并**与已验收基线比**（1px 环亮度比 idle 原生 alpha 基线约 0.47–0.61），不要用绝对值下判。
- 交付必须保留"对齐前/对齐后"两版候选与对照表，便于回溯。

### 6.3 视频提示词

- 必须分 5 层写（情景/动作语义/量化读点/硬约束/节奏），缺一层就会得到僵硬平均解。
- **正面视角下"后仰"不可投影**：要用正面可见读点（双肩上耸、脖子压缩、胸腔压扁、膝盖受力微屈、武器惯性滞后），并显式禁止身体纵轴左右倾斜。
- **不要写"变宽/变厚"**：会被实现成整体放大（"充气"）。写死"任何时刻整体尺寸不得大于 idle"。
- 动作量大的动作（Walk/击飞/掉盾）**必须带"绝对防超框"段**（四边 ≥8% 空白、碰边就做紧凑），否则"动作到位"会以出框为代价。
- 行进/位移类动作**手臂摆动是主读点**并给度数区间；武器只给方向红线，不锁角度。
- 节奏：受力/发力只占 0.05–0.15s，其余是回弹——静止关键图读不出冲击感是正常的，别指望它解决节奏。

### 6.4 部署与验证

以下运行检查注意事项仅在用户明确要求自检时使用；普通部署完成后通知用户自行验收，不自动执行。

- `EnemyPool` 按**文件名**解析 id，预制体必须叫 `Enemy_1021.prefab`（`Enemy_102_1.prefab` 会被解析成 102 撞号）。
- Play Mode 验证时必须筛**激活中**的实例（`FindObjectsOfType<Enemy>(true)` 会先命中池里未激活的克隆，`Animator.layerCount=0`、`Play` 完全无效，极易误判成接线错）。
- 命中卡肉会临时禁用 Animator，**不能在命中后 1 帧读动画状态**（会误判"动画没播"）。
- 当前 1021 Prefab 仍保留从 102 复制来的 `ShieldVisual` 占位层；正式方案已选帧内绘盾、脱手后引擎驱动独立盾对象。部署时须确认旧分离层和旧 clip 不会显示残盾，并实测独立掉落对象生命周期、敌人池复用及死亡/击飞打断。是否清理/复位旧节点按接线现状评估，不因旧债务而恢复分离持盾层生产。

---

## 7. 收费与交付纪律（复用既有约定）

- 任何收费任务**不自行重试**；服务端 `failed` 且 `cost=0` 只说明未计费，不等于已获授权。
- 提交前把提示词的**中文对照版**贴进对话；关键姿态参考图先给用户目检。
- 每个任务完成后把 **task id / 文件完整路径 / 字节数 / 费用 / 结论**追加到 `memory/video-generation-case-log.md` 的"任务台账"（一次性数据），流程性结论写进 `skill/workflows/` 对应 SOP。
- 中间产物放 `Library/Locus/tmp/enemy1021_*/`（可清理，不作为正式资产）；正式素材进 `Assets/Sprites/Enemy/Enemy1021/`。

---

## 8. 开工顺序建议（新对话照此推进）

1. **先读 §0 继续任务摘要与实际 v3 原图**，再按 §2 查当前阶段所需文档；起草 Idle 视频重点读画风、1011 实际 Idle 原词与视频 SOP，实施机制/部署时再核对对应契约和当前 Unity 引用。
2. **形象定妆已完成验收**：身份锚点为 `C:/Users/steam/Pictures/gptGen/enemy1021_shield_hold_v3.png`。人物以 1011 为基底改剑为盾，已举盾应战、持盾手藏于盾后、副手空垂；帧内绘盾与脱手后引擎掉落方案不变。生成来源见 §3.2，不再回到 1011+109 或早期失败候选。
3. **完成基线**：Idle四帧、Walk六帧、Attack十二帧、ShieldGuardHit六帧与HitFlash六帧已接入；Walk/Attack/ShieldGuardHit/HitFlash已由用户游戏验收。不要沿用早期阶段快照重做已完成动作。
4. **等用户指定目标，不自动开工**：新对话先读§0最新停点和§0.2剩余项；HitFlash已完成，HitRight已接入、待游戏验收。每次完整双语词和实际绿幕副本先审；有效帧先验收，再抠图，透明验收/对齐/接入分别处理。部署仍只接入第一关，未明确要求不运行自检。
5. 角色身份继续使用已验收v3，盾在画面左侧、帧内绘制；独立掉盾接管尚未完成，不能将Attack或ShieldGuardHit动作验收解释为破盾/胆怯等机制全量验收。
6. 每批完成后同步本交接与 `memory/video-generation-case-log.md`；Design文档中的规则继续作为约束，旧资产进度快照以本交接和当前工程为准。

---

## 9. 变更日志

- 2026-10-18 胆怯死亡视频方案交接：用户确认10秒胆怯击飞源片已有有效动作后，要求思考胆怯Dead并“准备视频生成方案交接”。新增 `plan/enemy1021-coward-dead-10s-video-preparation.md`：单张现有无盾Idle、10s/720p/1:1、完整中英文词与负面词、一次致命冲击→展开→松脱→无支撑死亡终态，不落地起身；参考文件已回读核验。主交接新增§0.20、更新顶部停点与过期summary，保留§0.19已验收击飞素材及全部历史。**本轮仅文档准备，未提交任何生成、未建task、未改图片/Unity资产、未进入Play或运行自检。**
- 初版：为 1021 形象与动画素材生产建立开工交接。汇总了必读文档清单、三步产出路线（形象定妆 → Idle 六帧 → 分批动作）、1011 已验收的接入约定与数值、1021 专属要点（盾位镜像、格挡/破盾两个专属动作的画法与时长契约）、六类已踩过的风险与收费纪律。
- 早期用户确认（参考输入已被本轮验收版取代）：A 型宽矩形木盾、空手副手、固定持盾姿势；盾在视频/本体帧内，脱手后由引擎驱动掉落。当时参考为 1011 与 109 Idle 原帧，未生图/视频；所有 GPT Image 请求统一 `quality=max`、编辑/融合 `input_fidelity=high`。
- 本轮 Idle 定妆验收：用户明确确认“idle形象已验收”；`enemy1021_shield_hold_v3.png` 成为身份与持盾姿态锚点。同步实际 1011 主参考 + 用户盾牌局部参考、已验收文件与阶段边界；Idle 动画、去背、对齐、Unity 部署和引擎掉盾尚未完成，本轮仅更新记录。
- 跨对话交接整理：新增 §0 的当前目标、已完成/未开始、授权边界、素材核验、1011 原词入口、下一步视频方案和工作树保护；纠正逐帧高度归一化矛盾，把背景与历史视频参数明确标为待审批/参考。当时未创建新生成任务、复制素材进 Assets、提交 Git 或修改 Unity 资产。
- 2026-10-10 Idle视频v1创建：本地背景染绿，用户批准参考图、双语词及参数后创建一次任务 `task_e169c149759b4216b75c879672389448`。首尾同图，4s/720p/1:1；初轮12次generating后停止自动轮询，当时无本地成片/usage/cost；保存请求与台账，未改Unity资产或去背。
- 2026-10-10 用户要求下载后：现有task success，4路Range下载83.808秒，2,000,007 bytes；容器及全部97帧解码通过，实际960×960/24fps/4.041667秒，cost.spend=0.48888。视频落盘哈希已回读核对，接触表保存0/24/48/72/96检查帧；当时待用户动画验收，未制作生产透明帧或部署。
- 2026-10-11 Attack交接收尾：Idle四帧、Walk六帧和Attack十二帧均已部署；用户确认Walk与Attack验收通过。Attack使用已验收对齐v2，2秒非循环、1秒接触姿态键。当时仅同步文档和验收留档，未改变Unity资产或启动运行自检。
- 2026-10-11 ShieldGuardHit交接收尾：用户逐阶段验收六秒视频、六帧选帧、透明v3后授权接入；六帧按现有固定偏移补边，创建0.30秒非循环Clip并接入1021控制器。用户随后明确游戏验收通过，要求在其他对话继续；已同步主交接、机制接续入口、素材台账与部署留档，移除正式ShieldGuardHit待办，保留其他未完成目标。本次仅更新文档/验收记录，未修改Unity资产、启动生成或执行自检。
- 2026-10-13 HitRight/HitFlash暂停交接：HitRight v2视频动作和六帧选择获用户验收，灰底补救后的绿底透明候选已落盘但透明验收仍为false，用户要求暂不部署；HitFlash v1–v4均未通过，v4“还不如v1”不代表v1重新获批。同步美术交接、机制入口、素材台账及v4 job/result状态；未生成新素材、未修改Unity、未运行自检。
- 2026-10-14 HitFlash完成交接：用户重新指定v1作为视频参考，逐阶段验收视频、六帧和T80透明结果后授权接入；六张按 `(74,91)` 补边，创建0.3667秒非循环Clip，替换1021 Controller的HitFlash motion。用户现已明确游戏验收通过；同步主交接、机制接续、台账和部署状态，清除当前清单中的HitFlash未完成项。当前仅更新文档/验收记录，保留历史，不改Unity资产或运行自检。
- 2026-10-14 HitRight接入：用户自行对已验收六帧完成抠图并明确“我已抠好那批图，你可以接入了”。六张按固定 `(74,91)` 零重采样补边为1108×1108，镜像Idle导入设置；新建 `Assets/Animations/Enemy_1021_HitRight.anim`（30fps、0.9秒、非循环，6帧+末保持键，`ShieldVisual`清空键），Controller新增 `HitRight` 状态与 `HitRight → Idle`（exit 0.9、duration 0），其余状态motion与Prefab/第一关配置未改；121个受保护文件哈希一致。补边仅做两项归一（半透明→不透明、全透明RGB→0），用户源图未动。HitRight游戏表现待用户验收，代理未进入Play/未自检。
- 2026-10-16 持盾死亡准备与绿幕规则复盘：用户指定开始做持盾死亡，要求先参考1011已验收死亡动作的出词方法。已对照1011死亡词（`memory/enemy1011-animation-prompt-archive.md` §4，6帧/0.6s非循环、腾空失力）完成差异分析，确定1021版只把“剑脱手”改为“盾仍握在画面左侧手上”，并保留1011的反超框、小幅离地、NO ROTATION、HEAD RULE等结构条款。为验证参考图合规性，实测七支视频的 raw 帧底色，修正了2026-10-15“green_v1违规”的判断：生成器不保上传色值但保色族，green_v1 的绿底场纯净、且就是四支已验收视频实际使用的同一份文件。用户据此选定**方案A**（沿用 `enemy1021_shield_hold_v3_green_v1.png` 作图1），并要求把图2 的生图任务书写入本文档以交给其他 AI 出词，§0.6 即为此交付。本轮**未发起任何收费生成**，未修改任何Unity资产，未进入Play或运行自检。
- 2026-10-16 图2 生图词定稿：用户审阅后要求把可直接提交的内容写入交接，供其他 AI 发起一次生成。定稿相对 §0.6 旧草稿的差异：静帧只画松开后的空中死重，不再同时要求“胸口上顶后弓”和“随后脊柱松开”；头向后落成甩向画面右侧；负面词删掉会否定目标的 `no dead corpse`，并补上禁后弓、禁露握盾手、禁剑、禁放大和禁侧面。仍**未发起收费生成**，未改 Unity 资产，未进入 Play 或运行自检。
- 2026-10-16 图2 v1 生成与验收不通过：用户要求按 §0.6 定稿词发起一次生成。已按批准的英文正文 + 负面词合并提交一次 `POST /v1/images/edits`（`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、opaque、PNG、n=1，仅上传图1；HTTP 200、168.75s、usage in/out 959/2058、cost 未返回）。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v1.png`（940,772 bytes、SHA-256 `5a13710a…da60`、1024×1024 RGB）实际落盘，图1 未改动。**用户随即验收不通过**：像角色飞升而非死亡，且整体朝向严重改变、完全看向画面左侧。代理客观测量印证了这两点：主体主轴相对竖直偏移由源图 1.4° 变为 46.5°，盾的木板区由 429×466 变为 571×575（1.331×/1.234× 被放大）；背景、边距、完整入框与盾的完整性本身达标。失败记录、三处必修与本地对照材料写入 §0.7。本轮**未重发、未改词、未降参数、未写视频词、未提交视频、未改任何 Unity 资产、未进入 Play 或运行自检**。已按批准的英文正文 + 负面词合并提交一次 `POST /v1/images/edits`（`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、opaque、PNG、n=1，仅上传图1；HTTP 200、168.75s、usage in/out 959/2058、cost 未返回）。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v1.png`（940,772 bytes、SHA-256 `5a13710a…da60`、1024×1024 RGB）实际落盘，图1 未改动。**用户随即验收不通过**：像角色飞升而非死亡，且整体朝向严重改变、完全看向画面左侧。代理客观测量印证了这两点：主体主轴相对竖直偏移由源图 1.4° 变为 46.5°，盾的木板区由 429×466 变为 571×575（1.331×/1.234× 被放大）；背景、边距、完整入框与盾的完整性本身达标。失败记录与本地对照材料写入 §0.7。当时未重发、未写视频词、未提交视频、未改 Unity 资产。
- 2026-10-16 图2 v2 词：用户要求按 v1 反馈修改提示词。v2 改为躯干竖直且偏离不超过 15°、橙眼仍朝镜头、头只垂向画面右侧；包围盒锁定在图1 的 439×587 上下 10%；死亡改为失重下坠，不再写后甩和仰天。完整英文词、中文对照和负面词写入 §0.7。**尚未提交**，未改 Unity 资产，未进入 Play 或运行自检。
- 2026-10-16 图2 v2 生成并验收通过：用户要求按新版词发起生成。已从文档精确提取 v2 英文正文（SHA `d5b81695…4649`）与负面词（SHA `593a940b…19e1`）合并提交一次 `POST /v1/images/edits`（`gpt-image-2.5-sunburst`、`quality=max`、`input_fidelity=high`、1024×1024、opaque、PNG、n=1，仅上传图1；HTTP 200、106.84s、usage in/out 856/1000、cost 未返回），实际提交合并 prompt SHA `576f410d…27b0`。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v2.png`（1,072,848 bytes、SHA-256 `f6073b06…2ac9`、1024×1024 RGB）实际落盘，图1 未改动。客观测量：主轴偏移由 v1 的 46.5° 降到 28.4°（未达 15° 目标）；包围盒宽比由 1.308 收到 1.034，盾木板宽比由 1.331 收到 1.040，尺寸已收回；最小边距 193 px（18.8%）。**用户于 2026-10-16 验收 v2 通过**。当时未写视频词、未提交视频、未改任何 Unity 资产；新增 §0.8 作为视频词任务书。
- 2026-10-16 死亡视频词：用户要求准备视频生成词。按已验收图2 v2 的姿态写 4 秒词：0.20 秒前保持图1，0.45 秒完成一次受击，1.20 秒前到达图2 并保持到 4 秒。正面、离地 8%–12%、盾不脱手、尺寸不放大和 10% 防超框都写进正文。不写后仰、变宽和仰天。英文正文、中文对照和负面词写入 §0.8。
- 2026-10-16 死亡视频 v1 生成（同日早些时候）：发现两张参考图底色不同值（图1 四角精确 `(0,177,64)` std 0；图2 v2 为约 `(2,178,68)`），按 G1 先另存图2 的同色绿幕上传副本（背景归一为 `(0,177,64)`、内部像素改动 0、主体几何不变；原图未动）。随后提交一次 `POST /v1/vids/create`（`seedance-2-fast` / 4s / 720p / 1:1 / 无音频 / 无水印 / seed=-1，两张普通 `reference_image`；HTTP 200，task `task_800f25278957424d9c5b911e75bfca2c`）。首轮 12 次轮询均 generating 后停止本地等待，之后单次查询返回 `success`，`cost.spend=0.48888`；四段 Range 下载完成（注：该解释器缺 PyAV，改用剪映自带 ffmpeg 完成完整解码验证），产物 `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v1.mp4`（2,056,869 bytes、SHA-256 `8e55e519…7402`、960×960 / 24fps / 97 帧 / 4.04s / 无音频）。背景 G 158–160、与基准差最差 +1，达标。**本轮未重发、未改词、未降参数、未改任何 Unity 资产、未进入 Play 或运行自检。**
- 2026-10-16 死亡视频 v1 两轮验收不通过：用户第一轮指出“结尾姿势不符预期、几乎没什么动作”；第二轮进一步给出语义层否决原话——“**动作毫无张力，没有被击飞到底的感觉**”“**与1011的dead动画语义完全不符**”“**姿态僵硬像是被催眠倒下**”。代理为此拿 1011 已验收死亡六帧做同口径实测：1011 dead 的填充率约 **0.30**（宽度扩到 Idle 的 **1.45–1.52 倍**，四肢真的张开），而 1021 成片末帧填充率反而由首帧 0.620 **升到 0.670**（比站立 Idle 还紧，张力 0.33 vs 1011 的 0.70），证实“毫无张力”属实。**本轮得到根因：问题不在视频词，而在目标图2**——图2 v2 当年为治“旋转 45°/放大 1.33×”被约束成“偏离竖直 ≤15°、包围盒变化 ≤10%”的**竖着收着**的姿态，视频词又继承该限制并叠加“末帧锁图2”，只能得到“原地小幅转身”的僵硬平均解。**结论：只改词改不出 1011 的死亡语义，须重做图2 并接受更大旋转/更宽展开——用户随后已拍板方向 A。**对照图 `SEMANTIC_VS_1011_DEAD.png`，指标 `ref_1011_dead_metrics.json` / `video_pose_metrics.json`；两轮反馈、测量与四处必修写入 §0.9。本轮**未重发、未改词、未生成图2、未改任何 Unity 资产、未进入 Play 或运行自检**。
- 2026-10-16 死亡方向拍板 A（重做图2）：用户原话“**按 A 方向，不用你写提示词，更新交接文档即可**”。当时记录为重做图2、接受更大旋转与更宽展开；生图词当时未写。文档同步了停点、§0.9 和开场指令。该轮只改文档，未生图、未提交视频。
- 2026-10-16 图2 重做方案：用户纠正“死亡动作没有禁止盾脱手”。新方案写入 §0.10：身体在画面平面内偏离竖直约 50°–80°，四肢张开，木盾离开画面左侧那只手并完整留在画面里；宽高比大于 1.4，填充率约 0.30–0.35。宽度来自张开和脱手，不来自放大部件。旧的“盾必须一直握着”“偏离竖直不超过 15°”“包围盒变化不超过 10%”都不再使用。英文词、中文对照、负面词和参数已写入。**该轮只写词未提交**，未改 Unity 资产，未进入 Play 或运行自检。
- 2026-10-16 图2 重做版 v3 生成（依据交接视频 v1 的失败反馈发起）：按 §0.10 词提交**一次** `POST /v1/images/edits`（仅上传图1，负面词并入同一 prompt），HTTP 200、POST 108.071 秒、usage 667/946/1613、cost 与 task id 未返回。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_dead_airborne_keypose_v3.png`，1,122,630 bytes、SHA-256 `dba1290c…9785`、1024×1024 RGB；实际提交合并 prompt SHA-256 `eb06952c…f140`（正文 `721e7814…587c` + 负面词 `e55a48fe…92ca`）。代理客观测量（与 1011 同口径）：全身宽高比 **1.60**（>1.4 达标）、填充率 **0.376**（目标 ≤0.35，略高）、主体分 **2 个连通块**（身体与盾最小间距 **14 px**，脱手成立）、盾像素面积约为图1 的 **0.64**（偏小而非放大）、身体块主轴 **79.8°**（落在 50°–80°；1011 dead3 为 71.5°、dead6 为 81.6°）、四边最小留白 **8.8%**（未裁切但略低于 ≥10%）。**未重发、未改词、未改任何 Unity 资产、未进入 Play 或运行自检；未提交视频、未写新视频词。**
- 2026-10-16 图2 重做版 v3 获用户验收：用户明确“**已验收该图片**”，v3 成为持盾死亡末帧锚点；三项达标边沿均视为已接受，不重发、不改图。该轮只改文档，未改 Unity 资产、未提交视频。
- 2026-10-16 视频 v2 准备：用户要求处理已验收图的绿幕，并准备视频生成。v3 原图四角实测 `(2,181,72)`，另存上传副本 `Library/Locus/tmp/enemy1021_dead_video_v2/02_dead_v3_green_00B140_1024.png`，四角精确 `(0,177,64)`、std 0，SHA-256 `16bfc410…83cf`。外部背景 883,920 px 与混色边缘 856 px 被替换，主体内部改动 0，原图未动。新视频词写入 §0.11：0.15 秒前保持图1，0.45 秒前盾脱手，1.20 秒前到达 v3 终态并保持到 4 秒。**尚未提交视频**，未改 Unity 资产，未进入 Play 或运行自检。
- 2026-10-17 死亡视频 v2 提交与下载（发起侧）：用户要求按交接文档发起视频并下载后立刻汇报。已按 §0.11 词原样提交**一次**（未改词、未换图），两张普通 `reference_image`，无首尾帧；上传与 create 均 HTTP 200，task `task_215c88d7209f46bc8e5b36a528c225e2`，accepted UTC `2026-10-17T03:12:03.170732+00:00`。12 次限时轮询均 generating 后停止本地等待，随后仅查询同一任务于 `2026-10-17T03:16:07.205752+00:00` 返回 `success`（completion_tokens 87300、`cost.spend=0.48888`）。成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_dead_v2.mp4`，2,326,004 bytes、SHA-256 `c2b444a3…9095`，实测 960×960/24fps/97帧/4.041667s/无音轨，四段 Range 均 206 且 ETag 一致，全帧解码通过。**未创建第二个任务、未重试、未选帧、未抠图、未对齐、未接入 Unity、未进入 Play；未写或改词。**
- 2026-10-17 死亡视频 v2 验收与选帧候选 v1：用户验收视频并授权开始选帧，同时更正任务边界——本对话不写生成词，但会承担选帧与抠图。已解出 97 帧并逐帧量测：97 帧字节全唯一，但只有 **5 个真正不同的姿态**（`f000-f033` 站立、`f034-f036` 受击起手、`f037-f044` 张开中段、`f045-f048` 展开加强、`f049-f096` 终态保持），且姿态间为 1 帧硬切（`f033→f034` 15.57%、`f044→f045` 22.59%、`f048→f049` 22.35%）。按“每组取组内最稳定 1 帧”选出候选 v1：f010/f034/f044/f048/f051，均 960×960 RGB，与解码源帧逐字节一致，未抠图/缩放/平移。交付目录 `Library/Locus/tmp/enemy1021_dead_video_v2/candidates_v1/`，评审表 `SELECTED_REVIEW_v1.png`，预览 GIF 两个（5 帧 120ms/帧、4 帧 150ms/帧）。**未抠图、未对齐、未部署 Unity、未进入 Play；未重发视频、未写词。** 待用户定帧数与去留。
- 2026-10-17 死亡透明素材 v1（3 帧 / T=80）：用户定 3 帧、容差 80、从 f044 起，并指出开头帧与 idle 一致无意义。先用“平坦区差异”证伪第 4 帧：f045 vs f048 = 0 px、f049 vs f096 = 1,000 px（90.3% 边缘噪声），而 f044 vs f046 = 141,696 px、f046 vs f051 = 134,141 px，即 f044 之后只有 A2/B/C 三个真姿态（`effective_pose_inventory.json`）。SVL Chroma 自动取键（底 `(1,157,60)`，coloured）+ T=80 + softness 16 + halo 1 + 半透明转不透明，未开平滑。产物 `Library/Locus/tmp/enemy1021_dead_video_v2/dead_3frames_matted_v1/`：`dead01_f044.png` / `dead02_f046.png` / `dead03_f072.png`，960×960 RGBA，半透明 0、透明区 RGB 全零、幕布残留 0、孔 0；距轮廓 ≥4px 内部 RGB 改动 0/1/6 px。保留可见区 green>8 共 403/508/495 px，不声称严格零绿。**未对齐、未补边、未部署 Unity、未进 Play；未重发视频、未写词。**该条当时为待验收状态，用户于同日后继消息验收通过，见下一条。
- 2026-10-17 死亡透明素材 v1 获用户验收：用户明确“已验收”，3 帧透明结果（`dead_3frames_matted_v1/`）通过；代理报告中的遗留绿像素（green>8 共 403/508/495 px）视为已接受，不重抠、不改图。用户同时明确本侧边界：**只更新交接文档，不负责对齐、补边与接入**——这三项由另一侧执行。本轮只改文档，未改任何 Unity 资产、未进入 Play、未重跑抠图、未重发视频。
- 2026-10-17 CowardIdle 关键帧词：用户要求准备胆怯待机的关键帧生成词，并放到交接文档，由另一侧生成。词写入 §0.12：源姿态是破盾原始末帧 `06_frame_144.png`，只画呼吸最高点；提交前另存精确绿幕副本，不上传已补边的 `Enemy_1021_shieldBreak6.png`。**尚未生成**，未改 Unity 资产，未进入 Play。
- 2026-10-17 CowardIdle 呼吸最高点关键帧 v1 生成：用户更新交接后要求“按文档发起图片生成”。按 §0.12 词原样提交一次 `POST /v1/images/edits`（英文正文 + 22 条负面词合并，只上传一张源姿态副本），HTTP 200、POST 68.859 秒、usage 1365/7024/8389、cost 与 task id 未返回。产物 `C:/Users/steam/Pictures/gptGen/enemy1021_coward_idle_peak_keypose_v1.png`（953,009 bytes、SHA-256 `8b6656bc…569d`、1024×1024 RGB）。源帧 `06_frame_144.png` 实测底 `(0,142,59)`，按 G1 另存 1024 上传副本（中心补边偏移 32,32、外部背景与 2px 边缘环统一 `(0,177,64)`、四角 std 0、主体内部改动 0、角色未缩放），原帧未动。客观量测：宽比 1.000 / 高比 0.982 / 像素比 0.984（要求 ≤5%）、最小留白 26.8%（要求 ≥15%）、双靴质心位移 (−2.9,−0.8)、左手区 (−0.1,−2.3)、右手区 (−4.0,+1.3)；生成图底色约 `(2,178,67)`。**未组装循环、未做视频、未抠图、未接入 Unity、未进入 Play；未重发、未改词。** 待用户验收。
- 2026-10-17 CowardIdle 呼吸最高点关键帧 v1 获用户验收并移交写词侧：用户明确“已验收”，呼吸最高点关键帧 v1 通过；底色偏差（生成图四角约 `(2,178,67)`）视为已接受，不重发不改图。用户同时要求“更新交接文档让另一边写提示词”，故胆怯家族剩余项（`CowardIdle` 循环 / `CowardHit` / `CowardLaunched` / `CowardDead`）的生成词由写词侧起草，本侧不写词、不代拟。文档已同步：顶部停点改为“已验收 → 其余关键帧的词待写词侧提交”，新增写词侧接手清单（规格所在文件、身份基准、已验收参照、G1 上传边界与不得上传项）；排期段从“等素材”改为“等写词侧出词”；§0.12 标记为执行完毕并附执行结果；开场指令改为指向写词侧。本轮只改文档，未改 Unity 资产、未进入 Play、未重跑生成。
- 2026-10-17 CowardIdle 视频交接：用户更新交接后要求准备视频生成交接。呼吸高点原图四角实测 `(2,178,66)`，另存上传副本 `Library/Locus/tmp/enemy1021_coward_idle_video_v1/02_coward_idle_peak_green_00B140_1024.png`，四角精确 `(0,177,64)`、std 0，SHA-256 `8b5355be…e736`。外部背景 977,318 px 与混色边缘 546 px 被替换，主体内部改动 0，原图未动。视频词写入 §0.13：图1 为循环首尾，图2 只是呼吸最高点；0.20–1.80 秒完成一次呼吸，之后重复并在末帧回到图1。**尚未提交视频**，未改 Unity 资产，未进入 Play。
- 2026-10-17 CowardIdle 循环视频 v1 提交与下载：用户更新交接后要求“按交接文档发起生成”。按 §0.13 词原样提交一次 `POST /v1/vids/create`（正文 + 23 条负面词合并，两张普通 reference_image），task `task_d9b8cc176ff54dd7a70be22663228710`，accepted UTC `2026-10-17T10:04:05+00:00`，create HTTP 200、两次上传均 200、只创建一次；11 次轮询后于 `10:07:49` 返回 `success`（completion_tokens 87300、`cost.spend=0.48888`）。下载阶段首次因提交脚本误用无 PyAV 的 SVL 解释器报 `ModuleNotFoundError: No module named 'av'`；生成已成功，因此**未重建任务**，改用带 PyAV 的解释器查询同一 task 下载，四段 Range 均 206、ETag 一致、全帧解码通过。成片 `C:/Users/steam/Videos/doubaoVideo/enemy1021_coward_idle_v1.mp4`，1,706,747 bytes、SHA-256 `84c7e29b…fd2f`，960×960/24fps/97帧/4.041667s/无音轨。客观量测：首尾差 6,721 px（主体约 10.6%）且包围盒差 ≤1 px；呼吸摆幅 4.13%；两周期同相位差 22,842 px（不完全重复，做循环需截单周期）；首帧片基 `(0,160,63)`。已同步停点、§0.13 标题与执行结果、排期段标题、summary 与台账。**未选帧、未抠图、未组装 Clip、未接入 Unity、未进入 Play；未重发、未改词。**
- 2026-10-17 CowardIdle 循环帧候选（6 帧/4 帧）：用户验收循环视频内容后要求挑选循环帧。量化定位单周期：静息顶边 298（f000-f005、f034-f044、f085-f096）、峰值顶边 282（f016-f023、f063-f073），一次呼吸约 2 秒。发现视频为离散姿态跳变而非平滑渐变（平台期连续帧差异 ≤344 px，跳变单步 2–3 万 px），故先按 ≥4000 px 聚成 29 个姿态组取代表帧，再穷举选帧使相邻变化最均匀并强制含静息与峰值。6 帧候选 f039/f045/f059/f069/f076/f078（步长比 1.32）落 `candidates_six/`；4 帧候选 f045/f059/f069/f076（步长比 1.14）落 `candidates_four/`；两套均 960×960 RGB、与解码源帧逐字节一致、未抠图。两套均不含两个静息帧（否则步长比升至 3.19–17.96）。**未抠图、未组装 Clip、未接入 Unity、未进入 Play。** 待用户在 6 帧与 4 帧之间选定。
- 2026-10-17 CowardIdle 6 帧透明素材 v1：用户选定 6 帧、容差 80。`svl_matte_batch.py --tolerance 80` 执行，实测底 `(0,159,63)`（coloured screen），T=80 下幕布残留 0（T=90 绿残留更低但按用户指定值）。六张 960×960 RGBA 落 `coward_idle_6frames_matted_v1/`：半透明 0、透明区非零 RGB 0、幕布残留 0、孔 0；可见像素 59,085–61,328；距轮廓 ≥4px 内部 RGB 改动仅 f039 2 px（maxΔ7），其余 0；保留可见区 green>8 共 214–317 px，不声称严格零绿。帧序 f039→f045→f059→f069→f076→f078（步长比 1.32），清单 `coward_idle_6frames_matted_v1.json` 记录逐帧 SHA-256。**对齐/补边/Clip/接入不由本侧做；未改 Unity 资产、未进 Play。** 待用户验收透明结果。
- 2026-10-17 CowardIdle 6 帧透明素材 v1 获用户验收：用户确认“已验收”，6 帧透明结果通过；遗留绿像素（每帧 214–317 px）与 f039 的 2 px 内部偏差视为已接受。按用户要求，本轮只更新交接文档并陈述现状，不写规划：已把“待用户验收”“下一步…”等过期或规划式表述改为现状陈述，包括文件头进度行、summary、CowardIdle 候选段、§0.11/§0.13 标题与引言、开场指令，以及若干历史停点的行动指令句式。素材未变、未重抠、未改 Unity 资产、未进入 Play。
- 2026-10-17 CowardHit 视频准备：用户指出不要先做受击关键帧，因为视频会僵硬地扭到关键帧姿势。因此不生成第二张图，只复用已有无盾待机上传副本 `01_coward_idle_peak_source_green_00B140_1024.png`。视频词写入 §0.14：0.15 秒前保持待机，0.35 秒前完成一次正面受击，1.10 秒前回到同一站姿并保持。**尚未提交视频**，未改 Unity 资产，未进入 Play。
- 2026-10-17 CowardHit v1 无受击感：用户查看 `enemy1021_coward_hit_v1.mp4` 后判定完全没有受击感。抽帧确认 f012、f024 仍是待机护脸，f036 以后只是把手放低。对照 102 的 `Enemy_102_CowardHit`：0.26 秒、两拍，先双手抱胸，再头和一只手臂甩开。v2 词写入 §0.15，仍只上传 1021 的一张待机图，不上传 102 受击图，也不另做关键帧。**尚未提交 v2**，未改 Unity 资产，未进入 Play。
- 2026-10-17 CowardHit v3 词：v2 被否，因为把 102 的两帧切换写成了视频中的抱胸和甩臂两段动作。v3 改用已验收 1011 正面受击的一次竖直压缩：肩上提内收、头沉、膝弯、人变矮，然后回到同一站姿。不甩臂、不侧倾、不另做关键帧。词写入 §0.16，上传图仍是 `01_coward_idle_peak_source_green_00B140_1024.png`。**尚未提交**，未改 Unity 资产，未进入 Play。
- 2026-10-17 CowardHit v4 峰值图：v3 成片被否，关键帧是毫无表情的捂胸，并保持约 1.35 秒。前三版都只给待机图，模型只能猜姿势。v4 先只生成一张受击峰值图：头向画面右侧偏、橙眼收窄、一只手被甩向画面左侧，明确禁止捂胸。词写入 §0.17。**尚未提交**；通过前不写视频词、不提交视频。
