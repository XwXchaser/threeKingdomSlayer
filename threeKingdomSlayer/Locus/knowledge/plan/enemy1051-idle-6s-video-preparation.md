---
id: kd_44161e3d-abe3-4097-8909-3f3cb8fcf8ad
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 1051 Idle：6秒源片（enemy1051_idle_v1_6s.mp4）与6帧循环帧（idle1..6，顶边116→104→115）均已用户验收；静帧去背仍暂缓。未抠图/未补边/未建Clip。
---

# Enemy 1051：原地持弓 Idle / 6 秒循环视频生成方案

## 1. 当前目标与状态

用户已获得并验收1051新的轻装Idle形象，要求思考如何制作Idle动画、准备视频生成，建议源片6秒。本轮据此准备：**同一张已验收造型的绿幕副本作为首尾帧，6秒中做两个约3秒的原地呼吸周期。**

**状态（2026-10-18 更新）：6 秒 Idle 源片已生成并获用户验收（两个约 3 秒原地呼吸周期），6 帧循环帧（`idle1..6`）也已验收。用户明确“先不要去背”，静帧阶段 B 继续暂缓。** 方案、上传副本、执行记录与选帧依据分别在 §2–§8、§12、§13。

- 身份以 `plan/enemy1051-identity-generation-handoff.md` §15 已验收的v3阶段A原图为准，不再使用v1或v2/v2b/v2c。
- 不重新设计服装、颜色、围巾厚度或手套；已验收图的实际外观优先于早期减甲文字。
- 用户“暂不进行去背”继续有效：本次不恢复静帧阶段B，不制作透明素材。另存上传副本仅统一外部绿幕，仍为不透明RGB。
- 本次只完成视频准备。不调用收费生成，不选帧/抠图/对齐/补边，不改旧105或Unity资产，不进入Play。

## 2. 动作设计

### 主动作：持弓警戒中的温和呼吸

- 双脚接触、骨盆、腰带中心和整体阵位稳定。胸肩与上臂小幅呼吸，不用整个人上下平移、缩放或左右摆动代替呼吸。
- 吸气时上胸与肩线略抬，肘部有很小的跟随；呼气时自然回落。应看得出活着，但不是喘气、耸肩健身或全身充气。
- 画面左侧空手保持参考图的放松拳形、垂在同一侧；不抬手、不摸脸、不去箭壶取箭。
- 画面右侧持弓手始终握住同一点。弓作为同一件硬形物件随腕臂很轻微移动，**保持原有曲线与长度，不额外拉弯或恢复弓形**。弦始终完整、拉直并连接同样两端，不拉弦、不搭箭、不放箭、不弹响。
- 箭壶随躯干极微跟随，箭羽数量和身份不变，不跳出或独立弹动。巾尾与短红腰布只作很小的延迟随动，不加风场或把围巾变长。
- 头部保持相同方向，眼缝与面巾不变，不露脸、不转头，不让橙色眼光像灯一样周期闪亮。

### 节奏：两个宽松的3秒周期

| 时间 | 动作 |
|---|---|
| 0–1.5s | 从参考图的放松警戒姿态自然吸气，上胸/肩线轻微抬升 |
| 1.5–3s | 缓慢呼气，连续回到同一站姿和装备关系 |
| 3–4.5s | 重复同等幅度、同一节奏的吸气 |
| 4.5–6s | 呼气回到原姿态，运动趋势可继续下一次吸气，无跳变或额外停顿 |

这些是宽松相位提示，不要求模型机械踩点。6秒用来取得可选的呼吸周期，不等于游戏内Clip必须长6秒。静态首尾约束不能保证中间重复性或无缝，仍要观察动作与接缝。

动作边界只要求小幅局部关节运动，不指定大量像素或角度上限。身体轮廓允许因呼吸轻微变化，但头、手脚与弓等部件本身尺寸不能周期变大变小。

## 3. 已验收原图与实际上传副本

### 原始身份图（保留不变，不直接作为本次上传文件）

```text
C:\Users\steam\Pictures\gptGen\enemy1051_identity_v3\enemy1051_identity_stageA.png
```

已回读：RGB PNG / 1024×1024 / 1,088,256 bytes；SHA-256：

```text
553f30c66b2e91713670fdda8d69bf9d5869a2095a1c391fa3e694a2802288a0
```

源图四角各64×64实测RGB中位为 `(1,163,67)` / `(2,163,66)` / `(1,163,66)` / `(1,163,66)`，std最大约1.033；属于近似单色绿幕，不是精确恒色。上下留白约12%已随静帧获用户接受，不能按早期≥15%条款自动缩小重排。

### 唯一实际上传副本（待用户审阅）

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_idle_video_v1_6s\01_idle_v3A_green_009F3E_1024.png
```

| 项 | 回读核验 |
|---|---|
| 格式 / 尺寸 | RGB PNG / 1024×1024，仍不透明 |
| 文件大小 | 445,447 bytes |
| SHA-256 | `64a0f77765cc4a19fe35e1d0d6b1bca7c063883f97379e741c35cc0fd476a4fc` |
| 目标制作底 | RGB `(0,159,62)` / `#009F3E` |
| 四角ROI | 各64×64均精确 `(0,159,62)`，std=0，min=max对应色值 |
| 实际背景改动 | 813,287 px；背景归一蒙版共813,306 px，其中19 px原本已等于目标色 |
| 主体保护 | 对非背景像素向外保留2px保护区；保护区RGB改动0 |
| 几何 | 无缩放、无平移、无裁切、无重绘；非背景框 `(282,123)–(810,899)` 不变 |
| Alpha / 去背 | 未生成Alpha、未制作透明图、未调用语义去背 |

方法：只选择距实测键 `(1,163,66)` 欧氏色差≤25且 `g-max(r,b)≥60` 的饱和绿区；其余主体及外扩2px边缘一律保留。弓弦、箭羽、指缝和暗橄榄布料在保护区内。保留原始轮廓混色环，所以本副本不是去绿边成品，也不宣称主体外围每一像素都已换底。

对照图（仅供审阅，不得上传）：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_idle_video_v1_6s\UPLOAD_SOURCE_VS_COPY.png
```

左为原图，右为上传副本。实现与量测留档：同目录 `prepare_upload.py`、`upload_preparation.json`。脚本已执行，不要覆盖重跑；需要修订时另存版本。本轮已回读副本、对照图和报告，但用户尚未对副本作验收。

### 参考模式

- **只上传上述副本一次**，返回URL同时用于 `first_frame` 和 `last_frame`。
- 不使用普通 `reference_image`，不混用参考视频或额外图片；两端是同一文件、同一图像，不另做呼吸峰值图。
- 首尾参考图仅约束目标，不承诺逐像素匹配、无缝循环、弓弦稳定或输出色值恒定。

## 4. 视频参数与输出

| 项 | 值 |
|---|---|
| 模型 | `seedance-2-fast` |
| 时长 | **6 秒** |
| 分辨率 / 比例 | `720p` / `1:1` |
| 音频 / 水印 | `false` / `false` |
| seed | `-1` |
| 输入 | 同一张副本作 `first_frame` 和 `last_frame` |
| 背景 | 不透明绿幕 RGB `(0,159,62)`，不请求Alpha视频 |
| 次数 | 用户明确发起后创建一次，失败/超时/未知结果不自动重建 |

建议输出（目前不存在，尚未生成）：

```text
C:\Users\steam\Videos\doubaoVideo\enemy1051_idle_v1_6s.mp4
```

建议请求/轮询/下载留档目录：

```text
C:\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\enemy1051_idle_video_v1_6s\
```

该目录目前仅有本次本地上传准备文件，不代表已经发出请求。不要覆盖原图或重跑其他敌人的生成脚本；不虚构 `loop`、固定fps或透明视频参数。

## 5. 完整英文正文

```text
Create a 6-second in-place combat-ready idle loop for this exact 2D game-sprite archer. The first and last images are the SAME prepared identity image. He waits alertly in his formation slot, alive but not performing a gesture. Preserve the accepted light costume, dark brown-gray olive head wrap and face covering, gray-brown cloth, exposed arms and lower legs, small cloth accents, proportions and hard pixel-cluster rendering. Do not redesign or recolor him.

Perform two gentle breathing cycles, approximately three seconds each. During inhalation the upper chest and shoulder line lift slightly, and the elbows follow by a very small amount; during exhalation they settle naturally back to the starting relationship. Make the breathing visible but restrained, not panting, shrugging, whole-body bouncing or inflation. Keep both shoe contacts, the pelvis, belt center, drawing scale and central slot stable. Only local upper-body articulation changes.

The screen-left free hand remains in its existing relaxed fist near the thigh, with only tiny arm follow-through. It never reaches for the quiver or bowstring. The screen-right hand maintains the same grip on the same bow. The whole bow may follow the wrist minutely, but its existing curve, length and construction stay unchanged. The complete string remains straight and attached to the same two bow tips: no drawing, flexing, string plucking, vibration, nocking or shooting. Preserve the quiver and the existing number of fletchings behind the screen-left shoulder; no arrows appear, disappear or jump.

Keep the head facing the same way and the face covered, with the same narrow orange eye slit, no exposed facial features or pulsing eye glow. Short cloth tips and the small waist cloth may follow breathing with a tiny delay; no strong wind or independent flag-like motion.

Use approximately 0-1.5s inhale, 1.5-3s exhale, 3-4.5s inhale and 4.5-6s exhale. Return continuously to the original stance and equipment relationship with a smooth loop boundary, not a pose snap, crossfade, rewind or extra pause. These are soft timing guides; prioritize a consistent natural breathing rhythm.

Lock the front-view camera and square canvas. No zoom, pan, reframing, turn, step, foot slide, root movement, approach or retreat. Preserve the supplied framing and margins; keep the entire bow, string, arrow feathers, cloth tips, hands and shoes inside the canvas with at least ten percent clear space on every side. Do not resize the character to create motion. Keep the same flat chroma-green background RGB(0,159,62), stable subject colors and lighting. No floor, shadow, scenery, effects, additional equipment, text or UI.
```

## 6. 完整中文对照（供用户审阅，不提交）

```text
为这个确切的2D游戏精灵弓手制作一段6秒的原地战斗警戒Idle循环。首尾两张图是同一张准备好的身份图。他在阵位里保持警觉，活着但不主动表演动作。保留已验收的轻装、暗褐灰橄榄头巾与面巾、灰褐布衣、露出的手臂和小腿、小块布料点色、体型比例和硬边像素块画风，不重新设计或改色。

做两个温和呼吸周期，每个约3秒。吸气时上胸与肩线稍抬，手肘很轻微地跟随；呼气时自然回到原本关系。呼吸要看得出来但克制，不喘气、不耸肩、不全身弹跳或充气。双鞋接触点、骨盆、腰带中心、绘制尺度和中心阵位稳定，只改变上身局部关节。

画面左侧空手仍保持参考图里靠近大腿的放松拳形，只有极小手臂随动，不去箭壶取箭、不碰弓弦。画面右侧持弓手始终握着同一把弓的同一点。整把弓可以极微跟随手腕，但原有弓形曲线、长度和结构不变。完整弓弦保持拉直，并连接同样两个弓梢；不拉弓、不额外弯曲弓身、不拨弦、不振弦、不搭箭、不射箭。左肩后箭壶及现有箭羽数量不变，不凭空增减或弹出箭矢。

头部保持同一方向，面部继续遮住，暖橙窄眼缝不变，不出现独立五官，不把眼光做成周期闪灯。短巾尾与小腰布可以随呼吸有极小延迟，不加强风，不像旗帜一样独立大幅飘动。

大致节奏为0–1.5秒吸气、1.5–3秒呼气、3–4.5秒吸气、4.5–6秒呼气。连续回到原本的站姿与装备关系，循环接缝平顺，不换姿势硬切、不叠化、不倒放，也不额外停住。这些时间是柔性节奏提示，优先自然一致的呼吸韵律。

锁定正面机位和正方形画布。不推拉、摇移、重新构图、转身、迈步、滑脚、移动根部、靠近或远离镜头。保留上传图构图与留白，整把弓、弓弦、箭羽、巾尾、双手和鞋子始终完整在画面里，四边至少10%空白。不要通过缩放人物制造运动。背景保持同一平坦绿幕RGB(0,159,62)，主体颜色与光照稳定。没有地板、投影、场景、特效、新装备、文字或UI。
```

## 7. 英文负面词（并入正文，同一个text）

```text
Avoid aiming, drawing the bow, nocking, shooting, reaching for an arrow, changing the bow curve, changing grip, broken or vibrating string, missing or extra arrows, waving, head turns, exposed face, added armor, bright yellow costume, exaggerated breathing, body scaling, whole-body bobbing, steps, foot sliding, camera movement, pose snapping, rewind, crossfade, long static holds, strong wind, flicker, particles, floor, shadow, scenery, text and UI.
```

中文负面词对照：不要瞄准、拉弓、搭箭、射击、取箭、改变弓形或握持点、断弦或弦振动、箭矢增减、挥手、转头、露脸、新增护甲、亮黄衣服、夸张呼吸、身体缩放、整个人上下浮动、迈步、滑脚、镜头运动、换姿势硬切、倒放、叠化、长时间静止、强风、闪烁、粒子、地板、投影、场景、文字或UI。

## 8. 提交方式与权限

1. 向用户呈现本方案的完整中英文词、负面词、实际上传副本、6秒参数和输出路径。副本与规格未经确认前，不上传、不创建请求。
2. 用户明确批准后，核对本次副本SHA-256，只上传该文件一次，返回URL在首尾两处复用。不混普通参考图，不另上传原图或其他敌人帧。
3. 实际text只包含§5英文正文＋两个换行＋§7英文负面词，中文与文档说明不提交。使用 `input.content`，不用旧 `negative_prompt` 字段。
4. 创建仅一次；超时或结果未知先调查同一请求，不重复收费。拿到task后，查询/下载失败不重建task。
5. 按 `skill/reachapi-seedance-video-generation-v2.md` 有限轮询、断点下载、完整解码并回读本地文件。保存实际词、参数、参考图哈希、task、usage/cost和成片哈希，不保存凭据。
6. 下载后先交用户看视频，不自动选生产帧、抠图、补边、改Clip/Prefab或进入Play。静帧阶段B仍然暂缓，不因视频授权恢复语义去背。
7. 用户不通过时仅记录原因并停止；不自改词、换图、降规格或重试。10秒击飞源片的成功不要求把Idle也改成10秒，当前按用户6秒制作。

请求示意（尚未发出；URL和text由发起侧替换）：

```json
{
  "model": "seedance-2-fast",
  "input": {
    "content": [
      { "type": "text", "text": "<§5英文正文 + 两个换行 + §7英文负面词>" },
      { "type": "image_url", "role": "first_frame", "image_url": { "url": "<同一个上传URL>" } },
      { "type": "image_url", "role": "last_frame", "image_url": { "url": "<同一个上传URL>" } }
    ],
    "duration_seconds": 6,
    "resolution": "720p",
    "aspect_ratio": "1:1",
    "generate_audio": false,
    "seed": -1,
    "watermark": false
  }
}
```

## 9. 验收重点与后续边界

- **实际动作**：胸肩与手肘有可读的小幅呼吸，不只是布料纹理闪动，也不是把角色整体缩放上下拉伸；脚底/腰带稳定。
- **弓手特征**：持弓不等于准备射击，空手不取箭。弓弦完整、附着点稳定、弓形不弹动，箭羽数量/形状不跳变，细部不能融化。
- **当前身份**：参考图实际服装、面巾和深色配色不变；不根据历史词重画成v1或黄色v2，不露鼻嘴。
- **循环**：观察两个周期及最后约半秒，比较同相位的脚、肩、拳、弓梢和巾尾；首尾相似不等于无缝。既要看姿态也要看运动趋势，警惕末尾硬切原图或冻结。
- **技术**：记录真实分辨率、fps、帧数、容器长度和完整解码结果。720p是请求档位，不假定输出为720×720或保证24fps。
- **G2/G3颜色**：量测确认全程无主体的背景ROI，相对实际上传键 `(0,159,62)` 给范围、首尾和最差帧差值；主体用稳定材质区、必要时局部配准单独比较。背景漂移不等于主体必定变色，输入已归一也不代表输出恒色。偏差列为用户待决项，不自动校色/重生。
- **姿态平台**：若生成呈离散姿态平台，只报告结构供选帧，不自动判废；由用户看效果。后续被授权选帧时取平台内部干净姿态，不拿压缩噪声或混合帧凑数量。
- **帧数与速度**：视频认可后才选有效周期，按真实不同姿态决定4–6帧等候选，不预先锁死。6秒源片与最终Idle Clip时长分开决定，不能把6帧误当6秒或每张只播1/30秒。
- **锚点**：保留源画布与动作几何，不逐帧按包围盒缩放。已验收原图约12%上下留白，本方案采用保留构图且至少10%动作安全边，不自动缩图去追旧15%指标。

## 10. 可复制给发起侧

> 请读取 `Locus/knowledge/plan/enemy1051-idle-6s-video-preparation.md`。用户已要求将方案写入交接，将在本侧之外发起生产。目标是已验收v3阶段A形象的原地持弓Idle：6秒、两个约3秒温和呼吸周期，只动胸肩/肘部及极小巾尾随动，双脚与骨盆固定，不取箭、不拉弦、不射击。实际输入是文档§3的归一绿幕副本，不是v1/v2或原图；同一副本上传一次，作为first_frame与last_frame，不混reference_image。参数seedance-2-fast/6s/720p/1:1/无音频无水印/seed=-1，text为§5英文正文＋两个换行＋§7英文负面词。收到用户明确发起指令并确认副本和规格后，只创建一次任务；失败或超时不自行重建。下载解码后交用户看，不自动选帧、抠图、接入Unity，也不恢复静帧去背阶段B。

## 11. 本次交接记录（2026-10-18）

- 用户指令：“写入交接文档，我会在别处发起生产”。本侧只维护交接，不提交生成。
- 完整英文正文、中文对照和双语负面词在§5–7；上传文件、哈希和副本处理边界在§3；请求参数与结构在§4/§8，发起侧不需重新编词或重做参考图。
- 实际提交合并词为§5英文正文＋两个换行＋§7英文负面词，UTF-8 SHA-256：`8c5fff0cd38f3d654bbced03a85e46306b44f7ac08802ec511d11e8ae9910830`。
- 上传副本与对照图位于本机`Library/Locus/tmp/`，不随Git自动迁移。若在其他机器生产，先传递同一上传PNG并核对SHA-256；缺文件时停止索取，不拿截图、对照图、旧身份图或重新生成图替代。
- 尚未创建task、视频或收费请求记录；不得把上传副本准备或交接完成报告为视频生产完成。

## 12. 执行结果（2026-10-18，已提交一次、下载并全帧解码；待用户看片）

- **授权**：用户 2026-10-19“根据交接文档发起视频生成”。
- **提交**：`POST /v1/vids/create`，`seedance-2-fast / 6s / 720p / 1:1 / 无音频 / 无水印 / seed=-1`；上传副本 `01_idle_v3A_green_009F3E_1024.png`（445,447 bytes、SHA `64a0f777…a4fc`、四角精确 `(0,159,62)`）**只上传一次**，同一 URL 同时用作 `first_frame` 与 `last_frame`；未混普通参考图。
- **词指纹**：§5 正文（2,677 字符）+ §7 负面（452 字符），合并 `8c5fff0cd38f3d654bbced03a85e46306b44f7ac08802ec511d11e8ae9910830`，与 §11 记录一致后才上传。
- **Task**：`task_30fdb0d6265a44729d272a4d88c30268`；上传/创建均 HTTP 200，`create_attempts=1`，无重试；usage 130,500 tokens，`cost.spend=0.7308`。
- **成片**：`C:/Users/steam/Videos/doubaoVideo/enemy1051_idle_v1_6s.mp4`，2,993,431 bytes、SHA-256 `eb5f564d436fa668a87c288a76615088b35ed7192f5b0130bfc6a9af4ffee204`；实测 960×960 / 24fps / 145 帧 / 容器 6.041667 秒 / 末帧 PTS 6.0 / H.264 / 无音轨，全帧解码通过。

### 12.1 客观实测

- **背景漂移**：四角 16×16 均值首帧 `(0,151.9,54.9)` 一族、末帧 `(0,150.0,56.2)` 一族；相对上传键 `(0,159,62)` 的最大通道偏差 **17**（平均 14.09，最差帧 f004）。输出的绿比上传副本约暗 8–9，但四角一致、无棋盘格。**是否可接受由用户定；不自动校色。**
- **动作幅度**：相邻帧改动中位数 **2,421 px**、最大 88,392 px；144 个相邻对里有 **20 对 <200 px**（近似静止平台）。半周期（f000 vs f072）差异 13,144 px，约为剪影面积（~19.4 万 px）的 6.8% —— 属于小幅呼吸量级，非整体缩放。
- **尺度稳定**：剪影面积 193,570–195,925（±0.6%），高 726–738、宽 487–497 —— **没有整体缩放或上下拉伸**，符合方案要求。
- **循环接缝**：首帧 vs 末帧改动 **9,350 px**，小于半周期差异（13,144 px），说明首尾接近；但"首尾接近"不等于无缝，最终仍需用户看动。
- **材料**：`Library/Locus/tmp/enemy1051_idle_video_v1_6s/` 的 `contact_sheet.png`（帧 0/36/72/108/144）、`frames_all/`（145 张原帧）、`objective_metrics.json`、`runtime.json`、`download_manifest.json`、`submit_1051_idle_video.py`。

### 12.2 未做

未选帧、未抠图、未对齐补边、未建 Clip、未改 Prefab/旧105、未进 Unity、未进入 Play；静帧阶段B（静帧去背）仍按用户要求暂缓，不因本次视频授权而恢复。

## 13. 6 帧循环帧选取（2026-10-18，**已获用户验收**）

用户验收视频后要求“挑选 6 帧 idle 循环帧”。呼吸相位信号用**主体遮罩顶边**（肩线）＋剪影面积：

- 静止：顶边 **115–116**（平台 f000–f006、f057–f084、f132–f144）
- 峰值：顶边 **104**（平台 f024–f039、f099–f117）
- 一个完整周期约 **f006 → f084（≈3.2 秒）**

| 精灵 | 源帧 | 时间 | 顶边 | 尺寸 | 可见 px | 节拍 |
|---|---|---|---|---|---|---|
| idle1 | f003 | 0.12s | 116 | 496×726 | 193,890 | 静止（呼气末，肩线最低） |
| idle2 | f012 | 0.50s | 110 | 495×732 | 195,360 | 吸气早段 |
| idle3 | f021 | 0.88s | 106 | 489×736 | 195,889 | 吸气后段 |
| idle4 | f030 | 1.25s | 104 | 487×738 | 195,888 | 峰值（平台内部） |
| idle5 | f045 | 1.88s | 108 | 492×734 | 195,649 | 呼气早段 |
| idle6 | f060 | 2.50s | 115 | 496×727 | 194,138 | 呼气后段（接近静止） |

- **相位覆盖**：顶边 116 → 110 → 106 → 104 → 108 → 115，跨一个完整呼吸；只放一个静止姿态（结束回到静止），避免重复平台帧。
- **帧间差异**：相邻 68,656–124,948 px；**循环回接 idle6 → idle1 仅 13,439 px**（全组最小），说明首尾贴合。像素差被模型逐帧重绘放大，不作为幅度依据；幅度看顶边 12 px（≈身高 1.6%）与面积 +1%。
- **交付目录**：`Library/Locus/tmp/enemy1051_idle_video_v1_6s/selected_idle_6/`（`idle1..6.png`，960×960 RGB，未抠图、未缩放、未对齐）；清单 `selected_idle_6.json`（逐帧 SHA-256、顶边、包围盒、可见像素）。
- **评审材料**：同目录上层 `IDLE_SELECTED_6_REVIEW.png`、`IDLE_SELECTED_6_PREVIEW.gif`（120ms/帧，仅评审节奏，不定义 Clip 时长）。
- **未做**：未抠图、未补边、未建 Clip、未改 Prefab/旧105、未进 Unity；静帧去背（阶段B）仍暂缓。

### 13.1 验收与当前边界（2026-10-18）

- **用户验收**：“已验收，请先不要去背，更新交接文档” —— **6 帧循环帧验收通过**，视频此前已验收。
- **当前资产**：`selected_idle_6/idle1..6.png`（960×960 RGB，未抠图）为 1051 Idle 的正式候选帧；源片 `C:/Users/steam/Videos/doubaoVideo/enemy1051_idle_v1_6s.mp4`（SHA `eb5f564d…e204`）。
- **去背状态**：静帧阶段 B 与本次 Idle 帧的抠图**均暂缓**，用户明确“先不要去背”。恢复需另行授权；恢复时以这 6 张原帧为输入，不得改用视频重抽或换图。
- **仍未做**：抠图、1108 补边、Animator Clip、Prefab/旧 105 替换、Unity 接入与 Play 自检；均需单独授权。
- **可复用口径**：呼吸相位用主体顶边＋面积量测；6 帧按“静止→吸气早/后段→峰值→呼气早/后段”取，只留一个静止姿态；循环看 `idle6 → idle1` 差异（本次 13,439 px，全组最小）。
